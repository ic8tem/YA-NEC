// YA-NEC 1.2.0 - finestra "Scegli la posizione dell'antenna" (bozzetto HF 01b): mappa a mattonelle (foto aerea Esri World Imagery,
// OpenStreetMap o sola griglia) con la griglia dei locatori Maidenhead da 6 a 12 caratteri. Clic = punto dell'antenna; trascina = sposta;
// rotella = zoom. Le mattonelle restano nella cartella data/mappe (cache, come chiede la politica d'uso delle mattonelle di OpenStreetMap).
// Ricerca delle localita' con Nominatim (OpenStreetMap), una richiesta per ricerca.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    /// <summary>Mappa a mattonelle Web Mercator con la griglia dei locatori.</summary>
    public class MappaLocatori : Control
    {
        public double Lat = 40.55, Lon = 14.24;   // centro
        public int Zoom = 13;
        public string Sfondo = "foto";            // foto, osm, griglia
        public int Livello = 8;                   // caratteri della griglia scelti
        public double SelLat = double.NaN, SelLon = double.NaN;
        public event Action<double, double> PuntoScelto;
        public string Cartella;

        readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();
        readonly HashSet<string> inCorso = new HashSet<string>();
        Point? premuto; Point ultimo; bool trascinato;

        public MappaLocatori() { ClipToBounds = true; Focusable = true; }

        static double MondoX(double lon, int z) { return (lon + 180) / 360 * 256 * Math.Pow(2, z); }
        static double MondoY(double lat, int z)
        {
            double s = Math.Sin(Math.Max(-85.05, Math.Min(85.05, lat)) * Math.PI / 180);
            return (0.5 - Math.Log((1 + s) / (1 - s)) / (4 * Math.PI)) * 256 * Math.Pow(2, z);
        }
        static double LonDa(double x, int z) { return x / (256 * Math.Pow(2, z)) * 360 - 180; }
        static double LatDa(double y, int z) { double n = Math.PI - 2 * Math.PI * y / (256 * Math.Pow(2, z)); return 180 / Math.PI * Math.Atan(Math.Sinh(n)); }

        Point ASchermo(double lat, double lon)
        {
            return new Point(MondoX(lon, Zoom) - MondoX(Lon, Zoom) + Bounds.Width / 2, MondoY(lat, Zoom) - MondoY(Lat, Zoom) + Bounds.Height / 2);
        }
        void DaSchermo(Point p, out double lat, out double lon)
        {
            double x = MondoX(Lon, Zoom) + p.X - Bounds.Width / 2, y = MondoY(Lat, Zoom) + p.Y - Bounds.Height / 2;
            lon = LonDa(x, Zoom); lat = LatDa(y, Zoom);
        }

        /// <summary>Passi in gradi (longitudine, latitudine) del quadrato a n caratteri.</summary>
        public static void Passo(int n, out double dlon, out double dlat)
        {
            dlon = 20; dlat = 10;
            for (int i = 2; i < n; i += 2) { int d = (i / 2) % 2 == 1 ? 10 : 24; dlon /= d; dlat /= d; }
        }

        double LarghezzaPx(int n)
        {
            double dlon, dlat; Passo(n, out dlon, out dlat);
            return MondoX(Lon + dlon, Zoom) - MondoX(Lon, Zoom);
        }

        /// <summary>Livello della griglia mostrato: quello scelto, piu' fitto con lo zoom (fino a 12), piu' rado se i quadrati sono troppo piccoli.</summary>
        public int LivelloMostrato()
        {
            int l = Livello;
            while (l < 12 && LarghezzaPx(l + 2) >= 60) l += 2;
            while (l > 4 && LarghezzaPx(l) < 24) l -= 2;
            return l;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e) { base.OnPointerPressed(e); premuto = ultimo = e.GetPosition(this); trascinato = false; e.Pointer.Capture(this); }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (premuto == null) return;
            Point p = e.GetPosition(this);
            if (!trascinato && Math.Abs(p.X - premuto.Value.X) + Math.Abs(p.Y - premuto.Value.Y) < 4) return;
            trascinato = true;
            double x = MondoX(Lon, Zoom) - (p.X - ultimo.X), y = MondoY(Lat, Zoom) - (p.Y - ultimo.Y);
            Lon = LonDa(x, Zoom); Lat = LatDa(y, Zoom);
            ultimo = p; InvalidateVisual();
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (premuto != null && !trascinato)
            {
                double la, lo; DaSchermo(e.GetPosition(this), out la, out lo);
                SelLat = la; SelLon = lo; InvalidateVisual();
                PuntoScelto?.Invoke(la, lo);
            }
            premuto = null; e.Pointer.Capture(null);
        }
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            Point p = e.GetPosition(this);
            double la, lo; DaSchermo(p, out la, out lo);
            int z = Math.Max(3, Math.Min(19, Zoom + (e.Delta.Y > 0 ? 1 : -1)));
            if (z == Zoom) return;
            Zoom = z;
            // il punto sotto il mouse resta fermo
            double x = MondoX(lo, Zoom) - (p.X - Bounds.Width / 2), y = MondoY(la, Zoom) - (p.Y - Bounds.Height / 2);
            Lon = LonDa(x, Zoom); Lat = LatDa(y, Zoom);
            InvalidateVisual(); e.Handled = true;
        }

        string Url(int z, int x, int y)
        {
            return Sfondo == "foto" ? "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/" + z + "/" + y + "/" + x
                : "https://tile.openstreetmap.org/" + z + "/" + x + "/" + y + ".png";
        }

        async void Carica(string chiave, int z, int x, int y)
        {
            if (!inCorso.Add(chiave)) return;
            try
            {
                string f = Path.Combine(Cartella ?? "data", "mappe", Sfondo, z.ToString(), x.ToString(), y + (Sfondo == "foto" ? ".jpg" : ".png"));
                byte[] b;
                if (File.Exists(f) && (DateTime.Now - File.GetLastWriteTime(f)).TotalDays < 60) b = await File.ReadAllBytesAsync(f);
                else
                {
                    b = await Dem.Http.GetByteArrayAsync(Url(z, x, y));
                    try { Directory.CreateDirectory(Path.GetDirectoryName(f)); await File.WriteAllBytesAsync(f, b); } catch { }
                }
                using (MemoryStream ms = new MemoryStream(b)) cache[chiave] = new Bitmap(ms);
                if (cache.Count > 400) { foreach (string k in cache.Keys.Take(100).ToList()) cache.Remove(k); }
                Dispatcher.UIThread.Post(InvalidateVisual);
            }
            catch { cache[chiave] = null; }
        }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.FillRectangle(new SolidColorBrush(Color.Parse(Sfondo == "griglia" ? "#F4F7FB" : "#C9D6E3")), new Rect(0, 0, W, H));
            double cx = MondoX(Lon, Zoom) - W / 2, cy = MondoY(Lat, Zoom) - H / 2;
            int n = 1 << Zoom;
            if (Sfondo != "griglia")
            {
                int x0 = (int)Math.Floor(cx / 256), x1 = (int)Math.Floor((cx + W) / 256), y0 = (int)Math.Floor(cy / 256), y1 = (int)Math.Floor((cy + H) / 256);
                for (int ty = Math.Max(0, y0); ty <= Math.Min(n - 1, y1); ty++)
                    for (int tx = x0; tx <= x1; tx++)
                    {
                        int txw = ((tx % n) + n) % n;
                        string k = Sfondo + "/" + Zoom + "/" + txw + "/" + ty;
                        Bitmap bm;
                        if (cache.TryGetValue(k, out bm)) { if (bm != null) ctx.DrawImage(bm, new Rect(tx * 256 - cx, ty * 256 - cy, 256, 256)); }
                        else Carica(k, Zoom, txw, ty);
                    }
            }
            // griglia dei locatori
            double laN, loW, laS, loE;
            DaSchermo(new Point(0, 0), out laN, out loW); DaSchermo(new Point(W, H), out laS, out loE);
            int liv = LivelloMostrato();
            Color lin = Sfondo == "foto" ? Color.Parse("#E6EEF8") : Color.Parse("#5B7A99");
            Action<int, double, bool> griglia = (lv, sp, etichette) =>
            {
                double dlon, dlat; Passo(lv, out dlon, out dlat);
                if ((loE - loW) / dlon > 400 || (laN - laS) / dlat > 400) return;
                Pen p = new Pen(new SolidColorBrush(Color.FromArgb(200, lin.R, lin.G, lin.B)), sp);
                for (double x = Math.Floor((loW + 180) / dlon) * dlon - 180; x <= loE + 1e-12; x += dlon) { Point a = ASchermo(laN, x), b = ASchermo(laS, x); ctx.DrawLine(p, new Point(a.X, 0), new Point(b.X, H)); }
                for (double y = Math.Floor((laS + 90) / dlat) * dlat - 90; y <= laN + 1e-12; y += dlat) { Point a = ASchermo(y, loW); ctx.DrawLine(p, new Point(0, a.Y), new Point(W, a.Y)); }
                if (!etichette) return;
                double wpx = LarghezzaPx(lv);
                if (wpx < 34) return;
                for (double x = Math.Floor((loW + 180) / dlon) * dlon - 180; x <= loE; x += dlon)
                    for (double y = Math.Floor((laS + 90) / dlat) * dlat - 90; y <= laN; y += dlat)
                    {
                        string l = Dem.Locatore(y + dlat / 2, x + dlon / 2, lv).ToUpperInvariant();
                        Point c = ASchermo(y + dlat / 2, x + dlon / 2);
                        Draw.TextC(ctx, l.Substring(Math.Max(0, lv - 4)), c.X, c.Y, 10.5, Sfondo == "foto" ? Colors.White : Color.Parse("#2B3A4A"));
                    }
            };
            griglia(liv, 1, true);
            if (liv > 6) griglia(6, 3, false);
            // nomi dei quadrati a 6 caratteri negli angoli
            double d6lon, d6lat; Passo(6, out d6lon, out d6lat);
            if (LarghezzaPx(6) > 120)
                for (double x = Math.Floor((loW + 180) / d6lon) * d6lon - 180; x <= loE; x += d6lon)
                    for (double y = Math.Floor((laS + 90) / d6lat) * d6lat - 90; y <= laN; y += d6lat)
                    {
                        Point a = ASchermo(y + d6lat, x);
                        Draw.Text(ctx, Dem.Locatore(y + d6lat / 2, x + d6lon / 2, 6).ToUpperInvariant(), a.X + 6, a.Y + 4, 15, Sfondo == "foto" ? Colors.White : Color.Parse("#16222E"), FontWeight.Bold);
                    }
            if (!double.IsNaN(SelLat))
            {
                // quadrato a 8 caratteri del punto in blu e punto rosso
                double d8lon, d8lat; Passo(8, out d8lon, out d8lat);
                double qx = Math.Floor((SelLon + 180) / d8lon) * d8lon - 180, qy = Math.Floor((SelLat + 90) / d8lat) * d8lat - 90;
                Point a = ASchermo(qy + d8lat, qx), b = ASchermo(qy, qx + d8lon);
                ctx.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#1D6FE0")), 2.4), new Rect(a, b));
                Point p = ASchermo(SelLat, SelLon);
                ctx.DrawEllipse(new SolidColorBrush(Color.Parse("#D0342C")), new Pen(Brushes.White, 2), p, 7, 7);
            }
            string attr = Sfondo == "foto" ? "Foto: Esri, Maxar, Earthstar Geographics e la comunita' GIS" : Sfondo == "osm" ? "© i contributori di OpenStreetMap" : "";
            if (attr.Length > 0)
            {
                FormattedText ft = Draw.Ft(attr, 10.5, Color.Parse("#16222E"));
                ctx.FillRectangle(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), new Rect(W - ft.Width - 10, H - ft.Height - 4, ft.Width + 10, ft.Height + 4));
                ctx.DrawText(ft, new Point(W - ft.Width - 5, H - ft.Height - 2));
            }
        }
    }

    public partial class MainWindow
    {
        /// <summary>Finestra della mappa: restituisce il locatore a 10 caratteri del punto scelto, oppure null se annullata.</summary>
        async Task<string> ScegliSullaMappa(string locIniz, double altezzaAntenna)
        {
            Window w = new Window { Title = "Scegli la posizione dell'antenna", Width = 1180, Height = 820, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Back) };
            MappaLocatori mappa = new MappaLocatori { Cartella = AppPaths.DataDir, MinHeight = 420 };
            double la0, lo0;
            if (Dem.DaLocatore(locIniz, out la0, out lo0)) { mappa.Lat = la0; mappa.Lon = lo0; mappa.SelLat = la0; mappa.SelLon = lo0; }
            string risultato = null;

            // barra in alto
            WrapPanel barra = new WrapPanel { Margin = new Thickness(12, 8) };
            Func<string, TextBlock> et = s => new TextBlock { Text = s, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 6, 0), Foreground = Pal.B(Pal.Fore) };
            Dictionary<string, Button> bSf = new Dictionary<string, Button>(), bGr = new Dictionary<string, Button>();
            Action pillole = () =>
            {
                foreach (var kv in bSf) StilePillola(kv.Value, kv.Key == mappa.Sfondo);
                foreach (var kv in bGr) StilePillola(kv.Value, kv.Key == mappa.Livello.ToString());
            };
            barra.Children.Add(et("Sfondo"));
            foreach (string[] v in new[] { new[] { "foto", "Foto aerea" }, new[] { "osm", "OpenStreetMap" }, new[] { "griglia", "Solo griglia" } })
            {
                Button b = new Button { Content = v[1], Margin = new Thickness(0, 0, 6, 0) }; string k = v[0];
                b.Click += (s, e) => { mappa.Sfondo = k; pillole(); mappa.InvalidateVisual(); };
                bSf[k] = b; barra.Children.Add(b);
            }
            barra.Children.Add(et("Griglia"));
            foreach (int n in new[] { 6, 8, 10 })
            {
                Button b = new Button { Content = n + " car.", Margin = new Thickness(0, 0, 6, 0) }; int k = n;
                b.Click += (s, e) => { mappa.Livello = k; pillole(); mappa.InvalidateVisual(); };
                bGr[n.ToString()] = b; barra.Children.Add(b);
            }
            barra.Children.Add(et("Cerca"));
            TextBox tCerca = Tb("", 170); barra.Children.Add(tCerca);
            Button bCentra = Btn("⌖ Centra sul locatore", false); bCentra.Margin = new Thickness(8, 0, 0, 0); barra.Children.Add(bCentra);
            pillole();

            // destra
            TextBlock vLoc = new TextBlock { FontSize = 22, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Accent) };
            TextBlock sLoc = new TextBlock { FontSize = 12, Foreground = Pal.B(Pal.Muted) };
            TextBlock vCoo = new TextBlock { FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) };
            TextBlock vQuo = new TextBlock { FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) };
            TextBlock tQuo = new TextBlock { FontSize = 12, Foreground = Pal.B(Pal.Muted), Text = "Quota del suolo (DEM)" };
            TextBlock sQuo = new TextBlock { FontSize = 12, Foreground = Pal.B(Pal.Muted), Text = "+ altezza dell'antenna sul suolo: " + FN(altezzaAntenna, 1) + " m" };
            Func<Control[], bool, Border> scheda = (cc, ev) =>
            {
                StackPanel sp = new StackPanel { Spacing = 2 }; foreach (Control c in cc) sp.Children.Add(c);
                return new Border { Child = sp, Background = Pal.B(ev ? Pal.AccentSoft : Pal.Panel), BorderBrush = Pal.B(ev ? Pal.Accent : Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8), Margin = new Thickness(0, 0, 0, 8) };
            };
            Disegno zoom8 = new Disegno { Height = 200, Width = 220 };
            TextBlock tZoom = new TextBlock { FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore) };
            StackPanel dx = new StackPanel { Width = 270 };
            dx.Children.Add(Pannello("Punto scelto", new StackPanel
            {
                Children =
                {
                    scheda(new Control[] { new TextBlock { Text = "Locatore", FontSize = 12, Foreground = Pal.B(Pal.Muted) }, vLoc, sLoc }, true),
                    scheda(new Control[] { new TextBlock { Text = "Coordinate", FontSize = 12, Foreground = Pal.B(Pal.Muted) }, vCoo }, false),
                    scheda(new Control[] { tQuo, vQuo, sQuo }, false)
                }
            }));
            StackPanel cz = new StackPanel { Spacing = 6 }; cz.Children.Add(tZoom); cz.Children.Add(zoom8);
            dx.Children.Add(new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Child = cz });

            // sotto la mappa: tabella dei lati
            Grid tab = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Thickness(0, 6, 0, 0) };
            Border avv = AvvisoGiallo("Con 6 caratteri il punto puo' sbagliare di km: per il profilo del terreno servono almeno 8 caratteri.");
            StackPanel sotto = new StackPanel { Spacing = 4 };
            sotto.Children.Add(new TextBlock { Text = "Clic sulla mappa = punto dell'antenna; trascina = sposta; rotella = zoom (la griglia passa da 6 a 8, 10, 12 caratteri). Bordo spesso = quadrato a 6 caratteri.", FontSize = 12, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap });
            sotto.Children.Add(tab); sotto.Children.Add(avv);

            Action aggiorna = null;
            CancellationTokenSource ctsQ = null;
            aggiorna = () =>
            {
                double la = mappa.SelLat, lo = mappa.SelLon;
                if (double.IsNaN(la)) { vLoc.Text = "-"; return; }
                string l10 = Dem.Locatore(la, lo, 10).ToUpperInvariant();
                double ew, ns; Dem.LatoQuadrato(10, la, out ew, out ns);
                vLoc.Text = l10; sLoc.Text = "10 caratteri · quadrato di " + FN(ew * 1000, 0) + " × " + FN(ns * 1000, 0) + " m";
                vCoo.Text = (la >= 0 ? FN(la, 4) + "° N" : FN(-la, 4) + "° S") + " · " + (lo >= 0 ? FN(lo, 4) + "° E" : FN(-lo, 4) + "° W");
                tZoom.Text = "Zoom su " + l10.Substring(0, 8) + " (in rosso: " + l10.Substring(8) + ")";
                zoom8.InvalidateVisual();
                // tabella dei lati alla latitudine del punto
                tab.Children.Clear(); tab.RowDefinitions.Clear();
                int r = 0;
                Action<string, string, bool, bool> riga = (a, b, h, bold) =>
                {
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int j = 0; j < 2; j++)
                    {
                        Border bb = new Border { Child = new TextBlock { Text = j == 0 ? a : b, FontWeight = h || bold ? FontWeight.SemiBold : FontWeight.Normal }, Padding = new Thickness(8, 4), Background = h ? Pal.B(Pal.Panel2) : Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 1, 1) };
                        Grid.SetColumn(bb, j); Grid.SetRow(bb, r); tab.Children.Add(bb);
                    }
                    r++;
                };
                riga("Caratteri", "Lato (a " + FN(Math.Abs(la), 1) + "° " + (la >= 0 ? "N" : "S") + ")", true, false);
                string l12 = Dem.Locatore(la, lo, 12).ToUpperInvariant();
                foreach (int n in new[] { 6, 8, 10, 12 })
                {
                    Dem.LatoQuadrato(n, la, out ew, out ns);
                    string lato = ew >= 1 ? FN(ew, 1) + " × " + FN(ns, 1) + " km" : ew >= 0.01 ? FN(ew * 1000, 0) + " × " + FN(ns * 1000, 0) + " m" : FN(ew * 1000, 1) + " × " + FN(ns * 1000, 1) + " m";
                    riga(n + "  " + l12.Substring(0, n), lato, false, n == 10);
                }
                // quota del suolo dal DEM
                ctsQ?.Cancel(); ctsQ = new CancellationTokenSource(); CancellationToken ct = ctsQ.Token;
                vQuo.Text = "…"; tQuo.Text = "Quota del suolo (DEM)";
                Task.Run(async () =>
                {
                    double q = double.NaN; string fonte = "";
                    try { Dem.Griglia g = await Dem.Tinitaly(la, lo, 0.3, ct); if (g != null) { q = g.Quota(la, lo); fonte = "DEM TINITALY"; } } catch { }
                    if (double.IsNaN(q)) try { Dem.Griglia g = await Dem.Srtm1(la, lo, ct); if (g != null) { q = g.Quota(la, lo); fonte = "SRTM1"; } } catch { }
                    return Tuple.Create(q, fonte);
                }, ct).ContinueWith(t =>
                {
                    if (ct.IsCancellationRequested || t.IsFaulted) return;
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (double.IsNaN(t.Result.Item1)) { vQuo.Text = "non disponibile"; tQuo.Text = "Quota del suolo (DEM: serve la rete al primo uso)"; }
                        else { vQuo.Text = FN(Math.Max(0, t.Result.Item1), 0) + " m s.l.m."; tQuo.Text = "Quota del suolo (" + t.Result.Item2 + ")"; }
                    });
                });
            };
            zoom8.Disegna = (ctx, rr) =>
            {
                if (double.IsNaN(mappa.SelLat)) return;
                double d8lon, d8lat, d10lon, d10lat; MappaLocatori.Passo(8, out d8lon, out d8lat); MappaLocatori.Passo(10, out d10lon, out d10lat);
                double qx = Math.Floor((mappa.SelLon + 180) / d8lon) * d8lon - 180, qy = Math.Floor((mappa.SelLat + 90) / d8lat) * d8lat - 90;
                int i = Math.Min(23, (int)((mappa.SelLon - qx) / d10lon)), j = Math.Min(23, (int)((mappa.SelLat - qy) / d10lat));
                double cw = (rr.Width - 2) / 24, ch = (rr.Height - 2) / 24;
                ctx.FillRectangle(new SolidColorBrush(Color.Parse("#DDEBC4")), rr);
                Pen p = new Pen(new SolidColorBrush(Color.Parse("#7FA06A")), 0.6);
                for (int k = 0; k <= 24; k++) { ctx.DrawLine(p, new Point(1 + k * cw, 1), new Point(1 + k * cw, rr.Height - 1)); ctx.DrawLine(p, new Point(1, 1 + k * ch), new Point(rr.Width - 1, 1 + k * ch)); }
                ctx.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#1D6FE0")), 1.5), new Rect(0.5, 0.5, rr.Width - 1, rr.Height - 1));
                ctx.FillRectangle(new SolidColorBrush(Color.Parse("#D0342C")), new Rect(1 + i * cw, 1 + (23 - j) * ch, cw, ch));
            };
            mappa.PuntoScelto += (la, lo) => aggiorna();
            bCentra.Click += (s, e) =>
            {
                double la, lo;
                if (!double.IsNaN(mappa.SelLat)) { mappa.Lat = mappa.SelLat; mappa.Lon = mappa.SelLon; }
                else if (Dem.DaLocatore(locIniz, out la, out lo)) { mappa.Lat = la; mappa.Lon = lo; }
                mappa.Zoom = Math.Max(mappa.Zoom, 14); mappa.InvalidateVisual();
            };
            tCerca.KeyUp += async (s, e) =>
            {
                if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(tCerca.Text)) return;
                try
                {
                    // un locatore va direttamente; altrimenti Nominatim di OpenStreetMap
                    double la, lo;
                    string q = tCerca.Text.Trim();
                    if (Dem.DaLocatore(q, out la, out lo)) { mappa.Lat = mappa.SelLat = la; mappa.Lon = mappa.SelLon = lo; mappa.InvalidateVisual(); aggiorna(); return; }
                    string url = "https://nominatim.openstreetmap.org/search?format=json&limit=1&q=" + Uri.EscapeDataString(q);
                    string js = await Dem.Http.GetStringAsync(url);
                    using (JsonDocument d = JsonDocument.Parse(js))
                    {
                        if (d.RootElement.GetArrayLength() == 0) { Status("Localita' non trovata: " + q, true); return; }
                        JsonElement r0 = d.RootElement[0];
                        mappa.Lat = double.Parse(r0.GetProperty("lat").GetString(), CultureInfo.InvariantCulture);
                        mappa.Lon = double.Parse(r0.GetProperty("lon").GetString(), CultureInfo.InvariantCulture);
                        mappa.Zoom = Math.Max(mappa.Zoom, 13); mappa.InvalidateVisual();
                        Status("Trovato: " + r0.GetProperty("display_name").GetString() + " (Nominatim, © OpenStreetMap)");
                    }
                }
                catch (Exception ex) { Status("Ricerca: " + ex.Message, true); }
            };

            Button bAnn = Btn("Annulla", false), bUsa = Btn("Usa questo punto", true);
            bAnn.Click += (s, e) => w.Close();
            bUsa.Click += (s, e) => { if (!double.IsNaN(mappa.SelLat)) risultato = Dem.Locatore(mappa.SelLat, mappa.SelLon, 10).ToUpperInvariant(); w.Close(); };
            Grid piede = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Margin = new Thickness(12, 8) };
            piede.Children.Add(bAnn); Grid.SetColumn(bUsa, 2); piede.Children.Add(bUsa);

            Border bMappa = new Border { Child = mappa, CornerRadius = new CornerRadius(6), ClipToBounds = true, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1) };
            Grid sx = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
            sx.Children.Add(bMappa); Grid.SetRow(sotto, 1); sx.Children.Add(sotto);
            Grid corpo = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,Auto"), Margin = new Thickness(12, 0) };
            corpo.Children.Add(new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Child = sx });
            Grid.SetColumn(dx, 2); corpo.Children.Add(dx);
            DockPanel dp = new DockPanel();
            DockPanel.SetDock(barra, Dock.Top); dp.Children.Add(barra);
            DockPanel.SetDock(piede, Dock.Bottom); dp.Children.Add(piede);
            dp.Children.Add(corpo);
            w.Content = dp;
            aggiorna();
            Registro.Scrivi("AZIONE", "terreno reale: mappa del locatore");
            await w.ShowDialog(this);
            return risultato;
        }

        static void StilePillola(Button b, bool on)
        {
            b.Background = new SolidColorBrush(on ? Fascione.SelezSfondo : Fascione.VoceSfondo);
            b.Foreground = new SolidColorBrush(on ? Fascione.SelezTesto : Fascione.VoceTesto);
            b.BorderBrush = new SolidColorBrush(Fascione.VoceBordo);
            b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(14);
            b.Padding = new Thickness(12, 4);
        }
    }
}
