// YA-NEC 1.2.0 - Diagrammi > "Terreno reale" (bozzetti HF 1 rev. 3 e VHF 1 rev. 2, stesso motore e stessa sottolinguetta).
// Metodo come HFTA di N6BV (ARRL Antenna Book): il campo diretto viene dal NEC (correnti risolte), il terreno aggiunge i raggi riflessi
// da ogni tratto del profilo e la diffrazione sui crinali (Engine/TerrenoReale.cs). Profilo da DEM (TINITALY 1.1 / SRTM1), da file
// di HorizonProfiler o piatto. In HF: direzioni di traffico e scala angolare doppia 0-45 gradi; in VHF: vista intera 0-180 e zoom 0-15.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Action terrAggiorna;
        double terrMediana = double.NaN;   // angolo di arrivo mediano (statistiche ARRL) per la linguetta Ricezione (RDF): NaN finche' mancano le statistiche
        TerrenoReale.Esito terrEsito;      // ultimo calcolo (per la sovrapposizione alla linguetta Verticale)
        bool terrSovrapponi;

        static readonly Color TerrBlu = Color.Parse("#1D6FE0"), TerrArancio = Color.Parse("#E08A00"), TerrGrigio = Color.Parse("#9AA5B4"),
            TerrSuolo = Color.Parse("#D8C8A0"), TerrSuoloBordo = Color.Parse("#8B6B3E"), TerrMare = Color.Parse("#BFDDF2"), TerrRosso = Color.Parse("#D0342C"),
            QuotaOcra = Color.Parse("#C77A10");

        /// <summary>Traccia del lobo sul terreno reale per il diagramma della linguetta Verticale (un valore per grado di elevazione 0..180).</summary>
        PolarTrace TerrTraccia()
        {
            if (terrEsito == null) return null;
            double[] db = new double[361];
            for (int i = 0; i < 361; i++) db[i] = -999;
            for (int i = 0; i <= 180; i++)
            {
                int idx = Math.Min(terrEsito.El.Length - 1, i * 4);
                double v = terrEsito.Reale[idx];
                db[i] = v < -90 ? -999 : v;
            }
            return new PolarTrace { Db = db, Color = TerrArancio, Width = 2, Dashed = false, Name = "terreno reale" };
        }

        static string FileTerreno { get { return Path.Combine(AppPaths.DataDir, "terreno.txt"); } }

        Control BuildTerrenoRealePage()
        {
            string prof = "dem", modoAz = "traffico", vista = "polare";
            int dirTr = 0;
            Dem.Profilo profFile = null;
            double lat = double.NaN, lon = double.NaN, quotaSito = double.NaN;
            // 1.5.2: quota del sito scritta a mano (NaN = quella del DEM), legata al locatore per cui e' stata scritta
            double quotaMano = double.NaN, ultimaQuotaDem = double.NaN; string locQuota = null;
            CancellationTokenSource cts = null;
            List<Tuple<double, double>> rosa = null;
            Dem.Profilo ultimoProf = null; TerrenoReale.Esito ultimo = null; double ultimaH = 0, ultimoAz = 0, ultimaF = 0;
            string titDir = "";
            NecModel modelloPrima = null; bool? vhfPrima = null;
            double hMigliore = double.NaN; List<Tuple<double, double>> scansione = null;

            string locIniz = "JN70CN82WE";
            try
            {
                if (File.Exists(FileTerreno))
                {
                    string[] s0 = File.ReadAllText(FileTerreno).Trim().Split(';');
                    if (s0[0].Trim().Length >= 4) locIniz = s0[0].Trim();
                    double q0; if (s0.Length > 1 && double.TryParse(s0[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out q0)) quotaMano = q0;
                }
            }
            catch { }
            locQuota = locIniz.ToUpperInvariant();

            // ---------------- sinistra ----------------
            StackPanel sx = new StackPanel { Width = 236, Spacing = 2 };
            Func<string, TextBlock> fam = s => new TextBlock { Text = s, FontWeight = FontWeight.Bold, FontSize = 13, Foreground = new SolidColorBrush(Fascione.FamigliaTesto), Margin = new Thickness(2, 10, 0, 4) };
            sx.Children.Add(fam("POSIZIONE DELL'ANTENNA"));
            TextBox tLoc = Tb(locIniz, 120);
            TextBlock spunta = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            StackPanel rLoc = new StackPanel { Orientation = Orientation.Horizontal };
            rLoc.Children.Add(new TextBlock { Text = "Locatore", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 6, 0) });
            rLoc.Children.Add(tLoc); rLoc.Children.Add(spunta);
            sx.Children.Add(rLoc);
            sx.Children.Add(new TextBlock { Text = "da 8 a 12 caratteri", FontSize = 11.5, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(2, 0, 0, 0) });
            TextBlock infoLoc = new TextBlock { FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 2, 0, 4) };
            sx.Children.Add(infoLoc);
            Border avv8 = AvvisoGiallo("Con 6 caratteri il punto puo' sbagliare di km: per il profilo del terreno servono almeno 8 caratteri.");
            sx.Children.Add(avv8);
            Button bMappa = Btn("Scegli sulla mappa", true); bMappa.HorizontalAlignment = HorizontalAlignment.Stretch;
            sx.Children.Add(bMappa);

            Fascione fProf = new Fascione(); fProf.Famiglia("PROFILO DEL TERRENO");
            fProf.Voce("file", "Da HorizonProfiler (.csv / .hpr)"); fProf.Voce("dem", "DEM TINITALY / SRTM1"); fProf.Voce("piatto", "Terreno piatto (confronto)");
            sx.Children.Add(fProf);

            sx.Children.Add(fam("SITO"));
            TextBox tH = Tb("10", 60), tAz = Tb("315", 60), tDmax = Tb("5", 60);
            Func<string, Control, string, Grid> rigaSito = (et, c, um) =>
            {
                Grid g0 = new Grid { ColumnDefinitions = new ColumnDefinitions("110,Auto,Auto"), Margin = new Thickness(2, 1) };
                g0.Children.Add(new TextBlock { Text = et, VerticalAlignment = VerticalAlignment.Center });
                Grid.SetColumn(c, 1); g0.Children.Add(c);
                TextBlock u = new TextBlock { Text = um, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };
                Grid.SetColumn(u, 2); g0.Children.Add(u);
                return g0;
            };
            // 1.5.2 (bozzetto approvato da IC8TEM il 19/11/2026): la quota del sito si puo' scrivere a mano quando il DEM sbaglia
            TextBox tQuota = Tb(double.IsNaN(quotaMano) ? "" : FN(quotaMano, 0), 60);
            sx.Children.Add(rigaSito("Quota sito", tQuota, "m s.l.m."));
            ToolTip.SetTip(tQuota, "Quota del suolo sotto l'antenna. Vuoto = valore del DEM (grigio). Scrivi la quota vera se il DEM sbaglia: il profilo parte da qui, il resto del terreno resta quello del DEM. ↺ DEM torna al valore del DEM.");
            TextBlock notaQuota = new TextBlock { FontSize = 11.5, Foreground = new SolidColorBrush(QuotaOcra), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            Button bQuotaDem = Btn("↺ DEM", false); bQuotaDem.Padding = new Thickness(8, 1); bQuotaDem.FontSize = 12;
            ToolTip.SetTip(bQuotaDem, "Torna al valore del DEM");
            Grid rigaNotaQuota = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(2, 0, 2, 2) };
            rigaNotaQuota.Children.Add(notaQuota); Grid.SetColumn(bQuotaDem, 1); rigaNotaQuota.Children.Add(bQuotaDem);
            sx.Children.Add(rigaNotaQuota);
            Action aggiornaQuota = () =>
            {
                // con il profilo da HorizonProfiler vale la quota del file: il campo la mostra in grigio e non si scrive
                bool daFile = prof == "file";
                bool mano = !double.IsNaN(quotaMano) && !daFile;
                double qv = daFile ? (profFile != null ? profFile.QuotaBase : double.NaN) : quotaSito;
                tQuota.IsEnabled = !daFile;
                tQuota.Watermark = double.IsNaN(qv) ? "-" : FN(qv, 0);
                if (mano) tQuota.BorderBrush = new SolidColorBrush(QuotaOcra); else tQuota.ClearValue(TextBox.BorderBrushProperty);
                tQuota.FontWeight = mano ? FontWeight.Bold : FontWeight.Normal;
                rigaNotaQuota.IsVisible = mano;
                notaQuota.Text = !mano ? "" : double.IsNaN(quotaSito) ? Lang.T("scritta a mano") :
                    Lang.F("scritta a mano: DEM {0} m ({1} m)", FN(quotaSito, 0), (quotaMano - quotaSito >= 0 ? "+" : "") + FN(quotaMano - quotaSito, 0));
            };
            aggiornaQuota();
            sx.Children.Add(rigaSito("Altezza antenna", tH, "m"));
            ToolTip.SetTip(tH, "Altezza del centro dell'antenna sul suolo. Se diversa da quella del modello, il diagramma del NEC viene alzato o abbassato con lo stesso andamento delle correnti.");
            sx.Children.Add(rigaSito("Distanza max", tDmax, "km"));

            Fascione fAz = new Fascione(); fAz.Famiglia("AZIMUT");
            fAz.Voce("traffico", "Per direzione di traffico"); fAz.Voce("libero", "Azimut libero");
            sx.Children.Add(fAz);
            Grid rigaAz = rigaSito("Azimut", tAz, "°");
            sx.Children.Add(rigaAz);

            Fascione fDir = new Fascione(); fDir.Famiglia("DIREZIONE DI TRAFFICO");
            for (int i = 0; i < Dem.DirezioniTraffico.Length; i++) { Border bv = fDir.Voce("d" + i, Dem.DirezioniTraffico[i].Nome); if (bv.Child is TextBlock tv0) tv0.FontSize = 13; bv.Padding = new Thickness(10, 3); }
            sx.Children.Add(fDir);
            sx.Children.Add(new TextBlock { Text = "Azimut e distanze ricalcolati a ogni cambio di locatore (ortodromia, sfera di 6371 km).", FontSize = 11.5, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 2, 0, 0) });

            sx.Children.Add(fam("SUPERFICI"));
            ComboBox cTerra = new ComboBox { Width = 205 };
            List<string> gl = new List<string>(); foreach (Tuple<string, double, double> g0 in Grounds) gl.Add(g0.Item1 + " (ε " + FN(g0.Item2, 0) + ", σ " + FN(g0.Item3, 4) + ")");
            cTerra.ItemsSource = gl; cTerra.SelectedIndex = 0;
            ComboBox cMare = new ComboBox { Width = 205, ItemsSource = new[] { "Acqua salata (εr 81, σ 5 S/m)", "Acqua dolce (εr 80, σ 0,003 S/m)" }, SelectedIndex = 0 };
            Func<string, Control, StackPanel> eti = (s, c) => { StackPanel p = new StackPanel { Margin = new Thickness(2, 1) }; p.Children.Add(new TextBlock { Text = s }); p.Children.Add(c); return p; };
            sx.Children.Add(eti("Terra", cTerra)); sx.Children.Add(eti("Mare", cMare));
            CheckBox cSuMare = new CheckBox { Content = "Superficie del mare", IsChecked = true };
            CheckBox cRug = new CheckBox { Content = "Rugosità mare (stato 2–3)", IsChecked = false };
            CheckBox cDiv = new CheckBox { Content = "Divergenza (terra curva, k = 4/3)", IsChecked = true };
            CheckBox cDif = new CheckBox { Content = "Diffrazione sui crinali", IsChecked = true };
            CheckBox cRosa = new CheckBox { Content = "Tutti gli azimut (rosa 0–360°)" };
            FumettoGiallo(cRug, "Riduce il coefficiente di riflessione del mare con il fattore di Ament per onde di 0,25 m (deviazione standard).", "modello statistico di superficie rugosa (Ament 1953): vale per angoli radenti e mare uniforme.", "Terreno reale");
            FumettoGiallo(cDiv, "Abbassa il profilo con la curvatura della terra (raggio efficace 4/3 per la rifrazione normale) e riduce i raggi riflessi lontani con il fattore di divergenza.", "atmosfera standard (k = 4/3); con condotti o inversioni la curvatura apparente cambia.", "Terreno reale");
            FumettoGiallo(cDif, "Il crinale piu' alto tra antenna e direzione d'uscita attenua il raggio diretto come uno spigolo (integrale di Fresnel), come in HFTA.", "spigolo singolo a lama di coltello: crinali arrotondati o piu' crinali in fila sono approssimati con il piu' alto.", "Terreno reale");
            FumettoGiallo(cRosa, "Ripete il calcolo ogni 10 gradi di azimut (solo con il DEM) e mostra la figura di merito sulla rosa, con l'antenna rivolta verso ogni azimut.", "con l'antenna sempre rivolta verso l'azimut del profilo: vale per un'antenna rotativa.", "Terreno reale");
            foreach (CheckBox c in new[] { cSuMare, cRug, cDiv, cDif, cRosa }) { c.Margin = new Thickness(2, 0); sx.Children.Add(c); }

            Fascione fVista = new Fascione(); fVista.Famiglia("VISTA");
            fVista.Voce("polare", "Polare (come Verticale)"); fVista.Voce("cartesiana", "Cartesiana dBi / gradi");
            sx.Children.Add(fVista);
            ComboBox cZoom = new ComboBox { Width = 120, ItemsSource = new[] { "0–45° (×2)", "0–30° (×3)", "0–15° (×6)", "0–90°", "0–180° intero" }, SelectedIndex = 0 };
            StackPanel rZ = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 2) };
            rZ.Children.Add(new TextBlock { Text = "Zoom angolare", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) }); rZ.Children.Add(cZoom);
            sx.Children.Add(rZ);
            CheckBox cSovr = new CheckBox { Content = "Sovrapponi alla linguetta Verticale", Margin = new Thickness(2, 0) };
            sx.Children.Add(cSovr);
            Button bCalc = Btn("Calcola", true), bCsv = Btn("Esporta CSV", false);
            StackPanel rB = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(2, 8, 0, 0) };
            rB.Children.Add(bCalc); rB.Children.Add(bCsv);
            sx.Children.Add(rB);

            // ---------------- centro ----------------
            TextBlock titProf = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            Disegno dProf = new Disegno { Height = 205 };
            StackPanel cProf = new StackPanel { Spacing = 4 }; cProf.Children.Add(titProf); cProf.Children.Add(dProf);
            Border pProf = new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 0, 10), Child = cProf };
            TextBlock titLobo = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            Disegno dLobo = new Disegno { Height = 380 };
            LineChart gCart = new LineChart { Height = 380, XLabel = "elevazione (gradi)", YLabel = "dBi", EmptyText = "premi Calcola", IsVisible = false };
            Disegno dRosa = new Disegno { Height = 320, IsVisible = false };
            StackPanel cLobo = new StackPanel { Spacing = 4 }; cLobo.Children.Add(titLobo); cLobo.Children.Add(dLobo); cLobo.Children.Add(gCart); cLobo.Children.Add(dRosa);
            Border pLobo = new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Child = cLobo };
            StackPanel cen = new StackPanel(); cen.Children.Add(pProf); cen.Children.Add(pLobo);

            // ---------------- destra ----------------
            StackPanel carte = new StackPanel { Spacing = 6 };
            Grid tab = new Grid();
            StackPanel righe = new StackPanel { Spacing = 1, Margin = new Thickness(0, 6, 0, 0) };
            StackPanel cTab = new StackPanel(); cTab.Children.Add(tab); cTab.Children.Add(righe);
            CheckBox cScan = new CheckBox { Content = "Scansione dell'altezza dell'antenna" };
            FumettoGiallo(cScan, "Ripete il calcolo con l'antenna da 6 a 30 m (HF) o da 2 a 30 m (VHF) e indica l'altezza con la figura di merito migliore sulla rotta.", "le correnti restano quelle del modello calcolato: l'effetto dell'altezza sull'impedenza e sulle correnti non e' considerato (come in HFTA).", "Terreno reale");
            CheckBox cStat = new CheckBox { Content = "Statistiche angoli ARRL (banda e regione)", IsEnabled = false };
            ToolTip.SetTip(cStat, "Servono le tabelle degli angoli di arrivo dell'ARRL Antenna Book (file .PRN per banda e regione): appena disponibili si attiva e mostra gli spicchi arancioni.");
            StackPanel opz = new StackPanel { Spacing = 2 }; opz.Children.Add(cScan); opz.Children.Add(cStat);
            StackPanel avvisi = new StackPanel { Spacing = 4, Margin = new Thickness(0, 6, 0, 0) };
            opz.Children.Add(avvisi);
            StackPanel dx = new StackPanel();
            dx.Children.Add(Pannello("Risultati (stile HFTA)", carte));
            dx.Children.Add(Pannello("Guadagno in elevazione", cTab));
            dx.Children.Add(Pannello("Opzioni", opz));

            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,10,1.5*,10,*"), Margin = new Thickness(4) };
            ScrollViewer svSx = new ScrollViewer { Content = sx };
            g.Children.Add(svSx);
            Grid.SetColumn(cen, 2); g.Children.Add(cen);
            Grid.SetColumn(dx, 4); g.Children.Add(dx);

            // ---------------- logica ----------------
            Func<bool> aggiornaLocatore = () =>
            {
                string s = (tLoc.Text ?? "").Trim().ToUpperInvariant();
                double la, lo;
                bool ok = Dem.DaLocatore(s, out la, out lo);
                spunta.Text = ok ? "✓" : "✗";
                spunta.Foreground = new SolidColorBrush(ok ? Color.Parse("#21A04A") : TerrRosso);
                avv8.IsVisible = ok && s.Length < 8;
                if (!ok) { infoLoc.Text = "locatore non valido"; lat = lon = double.NaN; return false; }
                lat = la; lon = lo;
                infoLoc.Text = (la >= 0 ? FN(la, 4) + "° N" : FN(-la, 4) + "° S") + " · " + (lo >= 0 ? FN(lo, 4) + "° E" : FN(-lo, 4) + "° W") +
                    (double.IsNaN(quotaSito) ? "" : " · quota DEM " + FN(quotaSito, 0) + " m");
                for (int i = 0; i < Dem.DirezioniTraffico.Length; i++)
                {
                    Dem.Direzione d = Dem.DirezioniTraffico[i]; double az, km;
                    Dem.Rotta(la, lo, d.Lat, d.Lon, out az, out km);
                    fDir.SetCount("d" + i, Lang.T(d.Nome) + " · " + FN(az, 0) + "° · " + FN(Math.Round(km / 10) * 10, 0) + " km");
                }
                if (locQuota != s) { locQuota = s; if (!double.IsNaN(quotaMano)) { quotaMano = double.NaN; tQuota.Text = ""; aggiornaQuota(); } }
                try { File.WriteAllText(FileTerreno, s + (double.IsNaN(quotaMano) ? "" : ";" + quotaMano.ToString("0.#", CultureInfo.InvariantCulture))); } catch { }
                return true;
            };
            Func<double> azimutScelto = () =>
            {
                if (modoAz == "traffico" && !double.IsNaN(lat))
                {
                    Dem.Direzione d = Dem.DirezioniTraffico[dirTr]; double az, km;
                    Dem.Rotta(lat, lon, d.Lat, d.Lon, out az, out km);
                    return az;
                }
                return ((ParseNum(tAz.Text, 0) % 360) + 360) % 360;
            };
            Func<double, string> nomeDir = az =>
            {
                string[] p = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
                return p[(int)Math.Round(az / 45) % 8];
            };

            // altezza del centro dell'antenna nel modello: segmento della prima sorgente, altrimenti media dei segmenti
            Func<double> quotaModello = () =>
            {
                if (run == null || run.Geometry == null) return 0;
                SegmentGeometry sg = run.Geometry;
                if (run.Sources.Count > 0) { int s = run.Sources[0].Segment - 1; if (s >= 0 && s < sg.N) return sg.CenterZ(s); }
                double z = 0; for (int i = 0; i < sg.N; i++) z += sg.CenterZ(i);
                return sg.N > 0 ? z / sg.N : 0;
            };

            Func<int, double> zoomMax = i => i == 0 ? 45 : i == 1 ? 30 : i == 2 ? 15 : i == 3 ? 90 : 180;

            // lobo: indice del primo massimo (sopra 0,25 gradi) e apertura a -3 dB
            Func<double[], double[], Tuple<int, double, double, int>> primoLobo = (el, gv) =>
            {
                int n = Math.Min(361, gv.Length);
                int im = -1;
                for (int i = 1; i + 1 < n; i++) if (gv[i] > -90 && gv[i] >= gv[i - 1] && gv[i] >= gv[i + 1]) { im = i; break; }
                if (im < 0) return Tuple.Create(-1, double.NaN, double.NaN, -1);
                double soglia = gv[im] - 3;
                int a = im; while (a > 0 && gv[a] > soglia) a--;
                int b = im; while (b < n - 1 && gv[b] > soglia) b++;
                int imin = -1;
                for (int i = im + 1; i + 1 < n; i++) if (gv[i] <= gv[i - 1] && gv[i] <= gv[i + 1]) { imin = i; break; }
                return Tuple.Create(im, el[a], el[b], imin);
            };
            Func<double[], double[], double, double> valoreA = (el, gv, e) =>
            {
                int i = (int)Math.Round(e * 4); i = Math.Max(0, Math.Min(gv.Length - 1, i));
                return gv[i];
            };
            // figura di merito senza statistiche: media della potenza sul settore di elevazione utile (HF 1-15 gradi, VHF 0,5-5) rispetto al piatto
            Func<TerrenoReale.Esito, double, double> figuraMerito = (r, f) =>
            {
                double a = f < 30 ? 1 : 0.5, b = f < 30 ? 15 : 5, sr = 0, sp = 0;
                for (int i = (int)(a * 4); i <= (int)(b * 4); i++) { sr += Math.Pow(10, r.Reale[i] / 10); sp += Math.Pow(10, r.Piatto[i] / 10); }
                return sp > 0 && sr > 0 ? 10 * Math.Log10(sr / sp) : double.NaN;
            };

            // ---------------- disegno del profilo ----------------
            dProf.Disegna = (ctx, rr) =>
            {
                Dem.Profilo p = ultimoProf;
                if (p == null || p.D.Length < 2) { Draw.TextC(ctx, run == null ? "Calcola prima il modello (pulsante Calcola in alto)" : "premi Calcola", rr.Width / 2, rr.Height / 2, 13, Pal.Muted); return; }
                double L = 52, R = rr.Width - 16, T = 14, B = rr.Height - 36;
                double xmax = p.D[p.D.Length - 1] / 1000;
                double zmin = 0, zmax = p.QuotaBase + ultimaH;
                foreach (double h in p.H) zmax = Math.Max(zmax, h);
                zmax = Math.Max(10, Math.Ceiling(zmax * 1.15 / 10) * 10);
                Func<double, double> X = d => L + (R - L) * d / xmax;
                Func<double, double> Y = z => B - (B - T) * (z - zmin) / (zmax - zmin);
                Pen griglia = new Pen(Pal.B(Pal.ChartGrid), 1);
                double passoZ = zmax <= 60 ? 10 : zmax <= 150 ? 25 : zmax <= 400 ? 50 : zmax <= 1000 ? 100 : 250;
                for (double z = 0; z <= zmax + 1e-6; z += passoZ) { ctx.DrawLine(griglia, new Point(L, Y(z)), new Point(R, Y(z))); Draw.TextR(ctx, FN(z, 0), L - 5, Y(z) - 7, 11, Pal.Muted); }
                double passoX = xmax <= 5 ? 1 : xmax <= 12 ? 2 : xmax <= 30 ? 5 : 10;
                for (double x = 0; x <= xmax + 1e-6; x += passoX) { ctx.DrawLine(griglia, new Point(X(x), T), new Point(X(x), B)); Draw.TextC(ctx, FN(x, 0), X(x), B + 10, 11, Pal.Muted); }
                Draw.TextC(ctx, "distanza dall'antenna (km)", (L + R) / 2, B + 26, 11.5, Pal.Muted);
                // suolo
                StreamGeometry sg = new StreamGeometry();
                using (StreamGeometryContext c = sg.Open())
                {
                    c.BeginFigure(new Point(X(0), Y(0)), true);
                    for (int i = 0; i < p.D.Length; i++) c.LineTo(new Point(X(p.D[i] / 1000), Y(p.H[i])));
                    c.LineTo(new Point(X(xmax), Y(0)));
                    c.EndFigure(true);
                }
                ctx.DrawGeometry(new SolidColorBrush(TerrSuolo), new Pen(new SolidColorBrush(TerrSuoloBordo), 1.4), sg);
                // mare
                for (int i = 0; i + 1 < p.D.Length; i++)
                    if (p.Mare[i]) ctx.FillRectangle(new SolidColorBrush(TerrMare), new Rect(X(p.D[i] / 1000), Y(0) - 7, Math.Max(1, X(p.D[i + 1] / 1000) - X(p.D[i] / 1000)), 7));
                // antenna
                double za = p.QuotaBase + ultimaH;
                ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#2B3440")), 2.4), new Point(X(0) + 1, Y(p.QuotaBase)), new Point(X(0) + 1, Y(za)));
                ctx.DrawEllipse(new SolidColorBrush(TerrRosso), null, new Point(X(0) + 1, Y(za)), 4.5, 4.5);
                Draw.Text(ctx, Lang.F("antenna {0} m sul suolo ({1} m s.l.m.)", FN(ultimaH, 1), FN(za, 0)), X(0) + 10, Y(za) - 18, 11.5, Pal.Fore);
                if (prof != "file" && !double.IsNaN(quotaMano))
                {
                    if (!double.IsNaN(quotaSito)) ctx.DrawLine(new Pen(new SolidColorBrush(QuotaOcra), 1.2, new DashStyle(new double[] { 2, 2 }, 0)), new Point(X(0), Y(quotaSito)), new Point(X(0) + 14, Y(quotaSito)));
                    Draw.Text(ctx, double.IsNaN(quotaSito) ? Lang.F("quota sito a mano {0} m", FN(quotaMano, 0)) : Lang.F("quota sito a mano {0} m (DEM {1} m)", FN(quotaMano, 0), FN(quotaSito, 0)),
                        X(0) + 18, B - 16, 11, QuotaOcra);
                }
                // orizzonte
                if (ultimo != null)
                {
                    double o = ultimo.OrizzonteAvanti;
                    if (o > -5)
                    {
                        double xo = xmax, zo = za + xo * 1000 * Math.Tan(o * Math.PI / 180);
                        Pen pd = new Pen(new SolidColorBrush(TerrSuoloBordo), 1.2, new DashStyle(new double[] { 2, 3 }, 0));
                        ctx.DrawLine(pd, new Point(X(0), Y(za)), new Point(X(xo), Y(Math.Min(zmax, zo))));
                        Draw.TextR(ctx, Lang.T("linea d'orizzonte") + " " + FN(o, 2) + "°", R - 4, Y(Math.Min(zmax, zo)) - 16, 11.5, TerrSuoloBordo);
                    }
                    if (!double.IsNaN(ultimo.RiflessioneKm) && ultimo.RiflessioneKm < xmax)
                    {
                        double xr = ultimo.RiflessioneKm; int ir = Math.Min(p.H.Length - 1, (int)Math.Round(xr * 1000 / Math.Max(1, p.D[1] - p.D[0])));
                        double zr = p.H[ir];
                        Pen po = new Pen(new SolidColorBrush(TerrArancio), 1.3, new DashStyle(new double[] { 4, 3 }, 0));
                        ctx.DrawLine(po, new Point(X(0), Y(za)), new Point(X(xr), Y(zr)));
                        ctx.DrawLine(po, new Point(X(xr), Y(zr)), new Point(X(Math.Min(xmax, 2 * xr)), Y(Math.Min(zmax, zr + (za - p.QuotaBase)))));
                        Draw.Text(ctx, "riflessione " + (ultimo.RiflessioneMare ? "sul mare" : "sul terreno") + " (" + FN(xr, 1) + " km)", X(xr) + 4, Y(zr) - 18, 11.5, TerrArancio);
                    }
                }
                if (p.Mare.Any(m => m)) Draw.TextR(ctx, "mare (riflessione con εr " + (cMare.SelectedIndex == 0 ? "81, σ 5 S/m" : "80, σ 0,003 S/m") + ")", R - 4, Y(0) - 24, 11.5, TerrBlu);
                // asse y
                FormattedText fy = Draw.Ft("quota s.l.m. (m)", 11.5, Pal.Muted);
                using (ctx.PushTransform(Matrix.CreateRotation(-Math.PI / 2) * Matrix.CreateTranslation(12, (T + B) / 2 + fy.Width / 2))) ctx.DrawText(fy, new Point(0, 0));
            };

            // ---------------- disegno del lobo (polare) ----------------
            // quarto (o mezzo) cerchio: el visualizzato = el * 90 / zmax; raggio lineare in dBi su 30 dB
            Action<DrawingContext, Rect, TerrenoReale.Esito, double, bool, double> disegnaPolare = (ctx, rr, r, zm, intero, gtop) =>
            {
                double gbot = gtop - 30;
                bool mezzo = intero;   // 0-180: mezzo cerchio con il centro in basso al centro
                double rad = mezzo ? Math.Min(rr.Width / 2 - 28, rr.Height - 46) : Math.Min(rr.Width - 60, rr.Height - 46);
                Point c0 = mezzo ? new Point(rr.X + rr.Width / 2, rr.Y + rr.Height - 26) : new Point(rr.X + 30, rr.Y + rr.Height - 26);
                double scala = mezzo ? 1 : 90 / zm;
                Func<double, double, Point> P = (e, gdb) =>
                {
                    double rho = rad * Math.Max(0, Math.Min(1, (gdb - gbot) / (gtop - gbot)));
                    double t = e * scala * Math.PI / 180;
                    return new Point(c0.X + rho * Math.Cos(t), c0.Y - rho * Math.Sin(t));
                };
                Pen gp = new Pen(Pal.B(Pal.ChartGrid), 1);
                double amax = mezzo ? 180 : 90;
                // orizzonte ostruito
                if (r != null)
                {
                    foreach (int lato in new[] { 0, 1 })
                    {
                        double o = lato == 0 ? r.OrizzonteAvanti : r.OrizzonteDietro;
                        if (o <= 0 || (lato == 1 && !mezzo)) continue;
                        double e0 = lato == 0 ? 0 : 180 - o, e1 = lato == 0 ? Math.Min(o, zm) : 180;
                        StreamGeometry s = new StreamGeometry();
                        using (StreamGeometryContext c = s.Open())
                        {
                            c.BeginFigure(c0, true);
                            for (double e = e0; e <= e1 + 1e-9; e += Math.Max(0.05, (e1 - e0) / 40)) c.LineTo(P(e, gtop));
                            c.EndFigure(true);
                        }
                        ctx.DrawGeometry(new SolidColorBrush(Pal.A(90, TerrSuolo)), null, s);
                    }
                }
                foreach (double gdb in new[] { gtop, gtop - 10, gtop - 20 })
                {
                    double rho = rad * (gdb - gbot) / (gtop - gbot);
                    StreamGeometry s = new StreamGeometry();
                    using (StreamGeometryContext c = s.Open())
                    {
                        bool ap = false;
                        for (double a = 0; a <= amax + 1e-9; a += 2) { Point q = new Point(c0.X + rho * Math.Cos(a * Math.PI / 180), c0.Y - rho * Math.Sin(a * Math.PI / 180)); if (!ap) { c.BeginFigure(q, false); ap = true; } else c.LineTo(q); }
                    }
                    ctx.DrawGeometry(null, gp, s);
                    Draw.TextC(ctx, FN(gdb, 0) + " dBi", c0.X + rho, c0.Y + 12, 10.5, Pal.Muted);
                    if (mezzo) Draw.TextC(ctx, FN(gdb, 0), c0.X - rho, c0.Y + 12, 10.5, Pal.Muted);
                }
                // raggi angolari
                double[] tacche = mezzo ? new double[] { 0, 10, 20, 30, 45, 60, 90, 120, 150, 180 } :
                    zm <= 15 ? new double[] { 0, 1, 2, 3, 4, 5, 6, 8, 10, 12, 15 } : zm <= 30 ? new double[] { 0, 2, 5, 8, 10, 15, 20, 25, 30 } : zm <= 45 ? new double[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45 } : new double[] { 0, 10, 20, 30, 45, 60, 75, 90 };
                foreach (double e in tacche)
                {
                    double t = e * scala * Math.PI / 180;
                    ctx.DrawLine(gp, c0, new Point(c0.X + rad * Math.Cos(t), c0.Y - rad * Math.Sin(t)));
                    Draw.TextC(ctx, FN(e, 0) + "°", c0.X + (rad + 14) * Math.Cos(t), c0.Y - (rad + 12) * Math.Sin(t), 10.5, Pal.Muted);
                }
                ctx.DrawLine(new Pen(Pal.B(Pal.Muted), 1.2), new Point(mezzo ? c0.X - rad : c0.X, c0.Y), new Point(c0.X + rad, c0.Y));
                if (r == null) return;
                Action<double[], Color, double, bool> curva = (v, col, w, tr) =>
                {
                    StreamGeometry s = new StreamGeometry();
                    using (StreamGeometryContext c = s.Open())
                    {
                        bool ap = false;
                        for (int i = 0; i < r.El.Length; i++)
                        {
                            double e = r.El[i];
                            if (!mezzo && e > zm + 1e-9) break;
                            if (double.IsNaN(v[i])) { ap = false; continue; }
                            Point q = P(e, v[i]);
                            if (!ap) { c.BeginFigure(q, false); ap = true; } else c.LineTo(q);
                        }
                    }
                    ctx.DrawGeometry(null, new Pen(new SolidColorBrush(col), w, tr ? new DashStyle(new double[] { 5, 3 }, 0) : null), s);
                };
                curva(r.Libero, TerrGrigio, 1.4, false);
                curva(r.Piatto, TerrArancio, 1.8, true);
                curva(r.Reale, TerrBlu, 2.4, false);
                var lb = primoLobo(r.El, r.Reale);
                if (lb.Item1 > 0)
                {
                    Pen p3 = new Pen(new SolidColorBrush(TerrBlu), 1.1, new DashStyle(new double[] { 4, 3 }, 0));
                    foreach (double e in new[] { lb.Item2, lb.Item3 }) if (mezzo || e <= zm) ctx.DrawLine(p3, c0, P(e, gtop));
                    Point pm = P(r.El[lb.Item1], r.Reale[lb.Item1]);
                    ctx.DrawEllipse(new SolidColorBrush(TerrBlu), null, pm, 3.5, 3.5);
                }
            };

            Action ridisegnaLobo = () =>
            {
                TerrenoReale.Esito r = ultimo;
                bool cart = vista == "cartesiana";
                dLobo.IsVisible = !cart; gCart.IsVisible = cart;
                int zi = Math.Max(0, cZoom.SelectedIndex);
                double zm = zoomMax(zi);
                if (cart)
                {
                    gCart.Series = new List<LineSeries>();
                    if (r != null)
                    {
                        int n = Math.Min(r.El.Length, (int)(zm * 4) + 1);
                        gCart.X = r.El.Take(n).ToArray();
                        gCart.Series.Add(new LineSeries { Name = "profilo reale", Y = r.Reale.Take(n).Select(v => v < -90 ? double.NaN : v).ToArray(), Color = TerrBlu, Width = 2.4, Decimals = 1 });
                        gCart.Series.Add(new LineSeries { Name = "terreno piatto", Y = r.Piatto.Take(n).Select(v => v < -90 ? double.NaN : v).ToArray(), Color = TerrArancio, Width = 1.8, Dashed = true, Decimals = 1 });
                        gCart.Series.Add(new LineSeries { Name = "spazio libero", Y = r.Libero.Take(n).Select(v => v < -90 ? double.NaN : v).ToArray(), Color = TerrGrigio, Width = 1.4, Decimals = 1 });
                    }
                    gCart.InvalidateVisual();
                }
                dLobo.InvalidateVisual();
            };
            dLobo.Disegna = (ctx, rr) =>
            {
                TerrenoReale.Esito r = ultimo;
                int zi = Math.Max(0, cZoom.SelectedIndex);
                double zm = zoomMax(zi);
                double gtop = 10;
                if (r != null) { double m = Math.Max(r.Reale.Max(), r.Piatto.Where(v => !double.IsNaN(v)).DefaultIfEmpty(-99).Max()); gtop = Math.Ceiling((m + 1) / 5) * 5; }
                // legenda
                double ly = rr.Y + 4;
                Action<Color, bool, string, double> voce = (col, tr, s, x) =>
                {
                    ctx.DrawLine(new Pen(new SolidColorBrush(col), 3, tr ? new DashStyle(new double[] { 2, 1 }, 0) : null), new Point(x, ly + 8), new Point(x + 18, ly + 8));
                    Draw.Text(ctx, s, x + 23, ly, 11.5, Pal.Fore);
                };
                voce(TerrBlu, false, "profilo reale", rr.X + 4); voce(TerrArancio, true, "terreno piatto (NEC)", rr.X + 120); voce(TerrGrigio, false, "spazio libero", rr.X + 280);
                ctx.FillRectangle(new SolidColorBrush(Pal.A(90, TerrSuolo)), new Rect(rr.X + 390, ly + 3, 18, 10));
                Draw.Text(ctx, "orizzonte ostruito", rr.X + 413, ly, 11.5, Pal.Fore);
                Rect corpo = new Rect(rr.X, rr.Y + 26, rr.Width, rr.Height - 26);
                bool vhf = ultimaF >= 30 || (r == null && run != null && run.FreqMHz >= 30);
                if (zi == 4) disegnaPolare(ctx, corpo, r, 180, true, gtop);
                else if (vhf)
                {
                    Rect a = new Rect(corpo.X, corpo.Y + 20, corpo.Width * 0.48, corpo.Height - 20), b = new Rect(corpo.X + corpo.Width * 0.5, corpo.Y + 20, corpo.Width * 0.5, corpo.Height - 20);
                    Draw.Text(ctx, "Intero 0–180° (come la linguetta Verticale)", a.X + 4, corpo.Y, 11.5, Pal.Fore, FontWeight.SemiBold);
                    Draw.TextR(ctx, "Zoom 0–" + FN(zm, 0) + "° (scala angolare ×" + FN(90 / zm, 0) + ")", b.Right - 4, corpo.Y, 11.5, Pal.Fore, FontWeight.SemiBold);
                    disegnaPolare(ctx, a, r, 180, true, gtop);
                    disegnaPolare(ctx, b, r, zm, false, gtop);
                    Draw.Text(ctx, "← dietro", a.X + 4, a.Bottom - 12, 11, Pal.Muted); Draw.TextR(ctx, "avanti →", a.Right - 4, a.Bottom - 12, 11, Pal.Muted);
                }
                else
                {
                    Rect a = new Rect(corpo.X, corpo.Y, corpo.Width * 0.62, corpo.Height);
                    disegnaPolare(ctx, a, r, zm, zm >= 180, gtop);
                    if (r != null)
                    {
                        var lb = primoLobo(r.El, r.Reale); var lp = primoLobo(r.El, r.Piatto);
                        double x = corpo.X + corpo.Width * 0.64, y = corpo.Y + 40;
                        Action<string, Color, FontWeight> rg = (s, col, w) => { Draw.Text(ctx, s, x, y, 12, col, w); y += 19; };
                        rg(Lang.T("scala angolare") + " " + (zm < 90 ? "×" + FN(90 / zm, 0) + " (0–" + FN(zm, 0) + "°)" : "normale"), Pal.Muted, FontWeight.Normal);
                        rg(titDir + " · " + FN(ultimaF, 3) + " MHz", Pal.Fore, FontWeight.SemiBold);
                        if (lb.Item1 > 0) rg(Lang.T("primo lobo reale:") + " " + FN(r.El[lb.Item1], 1) + "°", Pal.Fore, FontWeight.Normal);
                        if (lp.Item1 > 0) rg(Lang.T("primo lobo piatto:") + " " + FN(r.El[lp.Item1], 1) + "°", Pal.Fore, FontWeight.Normal);
                        if (lb.Item1 > 0) rg("-3 dB da " + FN(lb.Item2, 1) + "° a " + FN(lb.Item3, 1) + "°", TerrBlu, FontWeight.Normal);
                        double fino = double.NaN;
                        for (int i = 1; i < r.El.Length && r.El[i] <= 45; i++) if (r.Reale[i] > r.Piatto[i]) fino = r.El[i]; else if (!double.IsNaN(fino)) break;
                        if (!double.IsNaN(fino)) rg(Lang.T("lobo reale sopra il piatto fino a") + " " + FN(fino, 1) + "°", Color.Parse("#21A04A"), FontWeight.Normal);
                        rg("statistiche degli angoli di arrivo: non caricate", Pal.Muted, FontWeight.Normal);
                    }
                }
            };

            // ---------------- rosa della figura di merito ----------------
            dRosa.Disegna = (ctx, rr) =>
            {
                if (rosa == null || rosa.Count == 0) { Draw.TextC(ctx, "rosa 0–360°: calcolo in corso…", rr.Width / 2, rr.Height / 2, 12, Pal.Muted); return; }
                Point c0 = new Point(rr.Width / 2, rr.Height / 2 + 6);
                double rad = Math.Min(rr.Width, rr.Height) / 2 - 30;
                double vmin = Math.Floor(Math.Min(-3, rosa.Min(t => t.Item2)) / 3) * 3, vmax = Math.Ceiling(Math.Max(3, rosa.Max(t => t.Item2)) / 3) * 3;
                Func<double, double, Point> P = (az, v) => { double rho = rad * (v - vmin) / (vmax - vmin); double t = az * Math.PI / 180; return new Point(c0.X + rho * Math.Sin(t), c0.Y - rho * Math.Cos(t)); };
                Pen gp = new Pen(Pal.B(Pal.ChartGrid), 1);
                for (double v = vmin; v <= vmax + 1e-9; v += 3)
                {
                    double rho = rad * (v - vmin) / (vmax - vmin);
                    ctx.DrawEllipse(null, Math.Abs(v) < 1e-9 ? new Pen(Pal.B(Pal.Muted), 1.4) : gp, c0, rho, rho);
                    Draw.Text(ctx, (v > 0 ? "+" : "") + FN(v, 0) + " dB", c0.X + 3, c0.Y - rho - 13, 10, Pal.Muted);
                }
                for (int a = 0; a < 360; a += 30)
                {
                    Point q = P(a, vmax); ctx.DrawLine(gp, c0, q);
                    Draw.TextC(ctx, a + "°", c0.X + (rad + 16) * Math.Sin(a * Math.PI / 180), c0.Y - (rad + 14) * Math.Cos(a * Math.PI / 180), 10.5, Pal.Muted);
                }
                StreamGeometry s = new StreamGeometry();
                using (StreamGeometryContext c = s.Open())
                {
                    c.BeginFigure(P(rosa[0].Item1, rosa[0].Item2), true);
                    foreach (var t in rosa.Skip(1)) c.LineTo(P(t.Item1, t.Item2));
                    c.EndFigure(true);
                }
                ctx.DrawGeometry(new SolidColorBrush(Pal.A(50, TerrBlu)), new Pen(new SolidColorBrush(TerrBlu), 2), s);
                Draw.Text(ctx, "figura di merito rispetto al terreno piatto, per azimut (antenna rivolta verso ogni azimut)", 4, 2, 11.5, Pal.Muted);
            };

            Func<string, string, string, bool, Border> carta = (t, v, s, ev) =>
            {
                Border b = Riquadretto(t, v, s, ev); b.HorizontalAlignment = HorizontalAlignment.Stretch; b.MinWidth = 0;
                if (ev) foreach (Control c in ((StackPanel)b.Child).Children) if (c is TextBlock tb && tb.FontSize > 20) tb.Foreground = Pal.B(Pal.Accent);
                return b;
            };

            // ---------------- calcolo ----------------
            Func<double, double, double, CancellationToken, Task<Tuple<Dem.Profilo, Dem.Profilo>>> profili = async (az, dmax, passo, ct) =>
            {
                if (prof == "piatto" || (prof == "dem" && double.IsNaN(lat)))
                {
                    int n = (int)(dmax * 1000 / passo) + 1;
                    Dem.Profilo pp = new Dem.Profilo { D = new double[n], H = new double[n], Mare = new bool[n], Fonte = "piatto" };
                    double qp = !double.IsNaN(quotaMano) ? quotaMano : double.IsNaN(quotaSito) ? 0 : quotaSito;
                    for (int i = 0; i < n; i++) { pp.D[i] = i * passo; pp.H[i] = qp; }
                    pp.QuotaBase = pp.H[0];
                    return Tuple.Create(pp, pp);
                }
                if (prof == "file")
                {
                    if (profFile == null) throw new InvalidOperationException("Scegli il file del profilo (clic su \"Da HorizonProfiler\").");
                    int n = (int)(dmax * 1000 / passo) + 1;
                    Dem.Profilo pd0 = new Dem.Profilo { D = new double[n], H = new double[n], Mare = new bool[n], Fonte = "piatto" };
                    for (int i = 0; i < n; i++) { pd0.D[i] = i * passo; pd0.H[i] = profFile.QuotaBase; }
                    pd0.QuotaBase = profFile.QuotaBase;
                    return Tuple.Create(profFile, pd0);
                }
                // DEM: TINITALY (in Italia) e mattonelle SRTM1 lungo le due direzioni
                List<Dem.Griglia> gr = new List<Dem.Griglia>();
                try { Dem.Griglia ti = await Dem.Tinitaly(lat, lon, dmax + 0.5, ct); if (ti != null) gr.Add(ti); }
                catch (Exception ex) when (!(ex is OperationCanceledException)) { Registro.Scrivi("AVVISO", "TINITALY non disponibile: " + ex.Message); }
                HashSet<string> tile = new HashSet<string>();
                foreach (double a in new[] { az, (az + 180) % 360 })
                    for (double d = 0; d <= dmax + 1e-9; d += Math.Max(0.5, dmax / 20))
                    {
                        double la, lo; Dem.Destinazione(lat, lon, a, d, out la, out lo);
                        string k = Math.Floor(la) + "," + Math.Floor(lo);
                        if (tile.Add(k))
                        {
                            try { Dem.Griglia s = await Dem.Srtm1(la, lo, ct); if (s != null) gr.Add(s); }
                            catch (Exception ex) when (!(ex is OperationCanceledException)) { Registro.Scrivi("AVVISO", "SRTM1 non disponibile: " + ex.Message); }
                        }
                    }
                if (gr.Count == 0) throw new IOException("DEM non disponibile: serve la rete al primo uso per scaricare TINITALY o SRTM1 (poi restano nella cartella data/dem).");
                Dem.Profilo pa = Dem.Campiona(lat, lon, az, dmax, passo, gr);
                Dem.Profilo pdd = Dem.Campiona(lat, lon, (az + 180) % 360, dmax, passo, gr);
                if (pa.Fonte.Length > 0) ultimaQuotaDem = pa.QuotaBase;
                // 1.5.2: quota del sito scritta a mano: il primo punto dei due profili prende quella quota, il resto resta il DEM
                if (!double.IsNaN(quotaMano))
                    foreach (Dem.Profilo pq in new[] { pa, pdd }) { pq.H[0] = Math.Max(0, quotaMano); pq.Mare[0] = false; pq.QuotaBase = pq.H[0]; }
                return Tuple.Create(pa, pdd);
            };

            terrAggiorna = async () =>
            {
                cts?.Cancel(); cts = new CancellationTokenSource(); CancellationToken ct = cts.Token;
                aggiornaLocatore(); aggiornaQuota();
                carte.Children.Clear(); avvisi.Children.Clear();
                if (run == null || run.Engine == null)
                {
                    titProf.Text = "Profilo del terreno"; titLobo.Text = "Lobo di elevazione";
                    carte.Children.Add(Nota("Calcola prima il modello (pulsante Calcola in alto): il diagramma dell'antenna viene dal NEC."));
                    ultimo = null; ultimoProf = null; dProf.InvalidateVisual(); ridisegnaLobo();
                    return;
                }
                double f = run.FreqMHz, az = azimutScelto();
                // nuovo modello o passaggio HF <-> VHF: valori iniziali della banda e altezza dal modello
                if (!ReferenceEquals(modelloPrima, run.Model))
                {
                    bool vhfOra = f >= 30;
                    if (vhfPrima != vhfOra) { vhfPrima = vhfOra; cZoom.SelectedIndex = vhfOra ? 2 : 0; tDmax.Text = vhfOra ? "20" : "5"; }
                    modelloPrima = run.Model;
                    double zq = quotaModello(); tH.Text = zq > 0.5 ? FN(zq, 1) : "10";
                }
                double dmax = Math.Max(0.5, Math.Min(100, ParseNum(tDmax.Text, f < 30 ? 5 : 20)));
                double hUser = Math.Max(0.1, ParseNum(tH.Text, 10));
                double zMod = quotaModello();
                double phiAnt = run.PhiMax;
                bool suoloReale = model.Ground.Kind != GroundKind.FreeSpace;
                int gi = Math.Max(0, cTerra.SelectedIndex);
                TerrenoReale.Opzioni op = new TerrenoReale.Opzioni
                {
                    EpsTerra = Grounds[gi].Item2, SigmaTerra = Grounds[gi].Item3,
                    EpsMare = cMare.SelectedIndex == 0 ? 81 : 80, SigmaMare = cMare.SelectedIndex == 0 ? 5 : 0.003,
                    Mare = cSuMare.IsChecked == true, Rugosita = cRug.IsChecked == true, Divergenza = cDiv.IsChecked == true, Diffrazione = cDif.IsChecked == true
                };
                titDir = modoAz == "traffico" && !double.IsNaN(lat) ? Lang.T(Dem.DirezioniTraffico[dirTr].Nome) + " (" + FN(az, 0) + "°)" : "azimut " + FN(az, 0) + "° (" + nomeDir(az) + ")";
                string fonte = prof == "file" ? "da file" : prof == "piatto" ? "piatto" : "dal DEM";
                titProf.Text = Lang.T("Profilo verso") + " " + titDir + ", " + fonte + " · calcolo in corso…";
                titLobo.Text = Lang.T("Lobo verticale e angoli di arrivo a") + " " + FN(az, 0) + "° · " + FN(f, 3) + " MHz";
                Nec2Engine eng = run.Engine;
                double gcon = eng.GainConstant, lam = 299.792458 / f, kk = 2 * Math.PI / lam;
                bool scan = cScan.IsChecked == true, fareRosa = cRosa.IsChecked == true && prof == "dem" && !double.IsNaN(lat);
                dRosa.IsVisible = fareRosa; rosa = null;
                // 1.5.2: avviso subito nella barra in basso e Calcola disattivato finche' il calcolo non finisce
                bCalc.IsEnabled = false;
                // la barra in basso solo con la pagina davanti: dopo un calcolo del modello il ricalcolo del terreno in sottofondo non deve coprire «Calcolato a ...»
                bool aVista = TopLevel.GetTopLevel(g) != null && g.IsEffectivelyVisible;
                if (aVista) Status("Terreno reale: " + "calcolo in corso…");
                bool riuscito = false;
                try
                {
                    double passo = Math.Max(1, dmax * 1000 / 400);
                    var pr = await profili(az, dmax, passo, ct);
                    if (ct.IsCancellationRequested) return;
                    if (prof == "dem" && pr.Item1.Fonte.Length > 0) { quotaSito = ultimaQuotaDem; aggiornaLocatore(); }
                    aggiornaQuota();
                    Func<double, TerrenoReale.CampoDiretto> campo = sposta => (double el, double ph, out Complex a, out Complex b) =>
                    {
                        eng.FfldDiretto((90 - el) * Math.PI / 180, ph * Math.PI / 180, out a, out b);
                        if (Math.Abs(sposta) > 1e-6) { Complex fz = Complex.FromPolarCoordinates(1, kk * sposta * Math.Sin(el * Math.PI / 180)); a *= fz; b *= fz; }
                    };
                    Func<double, Dem.Profilo, Dem.Profilo, TerrenoReale.Esito> calcola = (h, pa, pd) =>
                    {
                        double sposta = h - zMod;
                        TerrenoReale.Opzioni o2 = new TerrenoReale.Opzioni { EpsTerra = op.EpsTerra, SigmaTerra = op.SigmaTerra, EpsMare = op.EpsMare, SigmaMare = op.SigmaMare, Mare = op.Mare, Rugosita = op.Rugosita, Divergenza = op.Divergenza, Diffrazione = op.Diffrazione };
                        Func<double, double> piattoNec = null;
                        if (suoloReale && model.Ground.Kind == GroundKind.Real && Math.Abs(sposta) < 0.05 && Math.Abs(model.Ground.Epsr - op.EpsTerra) < 1e-6 && Math.Abs(model.Ground.Sigma - op.SigmaTerra) < 1e-9)
                            piattoNec = e => e <= 90 ? eng.Gain(90 - e, phiAnt).GainTotal : eng.Gain(e - 90, (phiAnt + 180) % 360).GainTotal;
                        TerrenoReale.Esito r = TerrenoReale.Calcola(campo(sposta), gcon, f, phiAnt, pa, pd, h, o2, piattoNec);
                        if (piattoNec == null)
                        {
                            // terreno piatto con lo stesso metodo (stesso suolo, senza mare e senza curvatura)
                            int n = pa.D.Length;
                            Dem.Profilo fl = new Dem.Profilo { D = (double[])pa.D.Clone(), H = new double[n], Mare = new bool[n], Fonte = "piatto" };
                            TerrenoReale.Opzioni of = new TerrenoReale.Opzioni { EpsTerra = op.EpsTerra, SigmaTerra = op.SigmaTerra, Mare = false, Divergenza = false, Diffrazione = false };
                            TerrenoReale.Esito rp = TerrenoReale.Calcola(campo(sposta), gcon, f, phiAnt, fl, pd == null ? null : fl, h, of, null);
                            r.Piatto = rp.Reale;
                        }
                        return r;
                    };
                    var esito = await Task.Run(() => calcola(hUser, pr.Item1, pr.Item2), ct);
                    if (ct.IsCancellationRequested) return;
                    ultimo = esito; ultimoProf = pr.Item1; ultimaH = hUser; ultimoAz = az; ultimaF = f;
                    terrEsito = esito;
                    titProf.Text = Lang.T("Profilo verso") + " " + titDir + ", " + (prof == "dem" ? pr.Item1.Fonte : prof == "file" ? "da " + pr.Item1.Fonte : "piatto") + (prof != "file" && !double.IsNaN(quotaMano) ? " · " + Lang.T("quota sito a mano") : "");
                    dProf.InvalidateVisual(); ridisegnaLobo();
                    // risultati
                    double fom = figuraMerito(esito, f);
                    Border cF = carta("Figura di merito sulla rotta", (fom >= 0 ? "+" : "") + FN(fom, 1) + " dB", "rispetto alla stessa antenna su terreno piatto", true);
                    FumettoGiallo(cF, "Differenza tra il lobo sul profilo reale e il lobo sul terreno piatto, mediata sugli angoli utili.",
                        "senza le statistiche ARRL degli angoli di arrivo la media e' uniforme su " + (f < 30 ? "1–15°" : "0,5–5°") + " di elevazione; HFTA la pesa con la percentuale dei tempi di apertura.", "Terreno reale");
                    carte.Children.Add(cF);
                    carte.Children.Add(carta(f < 30 ? "Guadagno a 5° / 10°" : "Guadagno a 1° / 3°", FN(valoreA(esito.El, esito.Reale, f < 30 ? 5 : 1), 1) + " / " + FN(valoreA(esito.El, esito.Reale, f < 30 ? 10 : 3), 1) + " dBi",
                        Lang.T("piatto:") + " " + FN(valoreA(esito.El, esito.Piatto, f < 30 ? 5 : 1), 1) + " / " + FN(valoreA(esito.El, esito.Piatto, f < 30 ? 10 : 3), 1) + " dBi", false));
                    var lb = primoLobo(esito.El, esito.Reale); var lp = primoLobo(esito.El, esito.Piatto);
                    carte.Children.Add(carta("Primo massimo", lb.Item1 > 0 ? FN(esito.El[lb.Item1], 1) + "°" : "-", Lang.T("piatto:") + " " + (lp.Item1 > 0 ? FN(esito.El[lp.Item1], 1) + "°" : "-"), false));
                    carte.Children.Add(carta("Angoli coperti a -3 dB", lb.Item1 > 0 ? FN(lb.Item2, 1) + "° – " + FN(lb.Item3, 1) + "°" : "-", "primo lobo sul profilo reale", false));
                    Border cAlt = carta("Altezza migliore per la rotta", scan ? "calcolo…" : "-", scan ? "scansione " + (f < 30 ? "6–30" : "2–30") + " m" : "attiva la scansione (sotto)", false);
                    carte.Children.Add(cAlt);
                    if (Math.Abs(hUser - zMod) > 0.05)
                        avvisi.Children.Add(AvvisoGiallo("Antenna a " + FN(hUser, 1) + " m invece dei " + FN(zMod, 1) + " m del modello: il diagramma del NEC e' spostato in quota con le stesse correnti (l'impedenza e l'accoppiamento col suolo non cambiano)."));
                    if (!suoloReale)
                        avvisi.Children.Add(AvvisoGiallo("Modello in spazio libero: le correnti sono quelle senza suolo, come il diagramma libero usato da HFTA. Per tenere conto del suolo vicino calcola il modello con il terreno reale."));
                    avvisi.Children.Add(AvvisoGiallo("Il profilo vale per un solo azimut: per un'antenna rotativa servono piu' direzioni (casella \"Tutti gli azimut\")."));
                    avvisi.Children.Add(new Border { Background = Pal.B(Pal.WarnBg), BorderBrush = new SolidColorBrush(Color.Parse("#E5C76B")), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 6),
                        Child = new TextBlock { Text = "Metodo come HFTA (N6BV): raggio diretto + raggi riflessi su ogni tratto del profilo + diffrazione sui crinali. Il diagramma dell'antenna viene dal NEC, il terreno lo aggiunge questo calcolo.", TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.WarnText) } });
                    // tabella
                    tab.Children.Clear(); tab.RowDefinitions.Clear();
                    tab.ColumnDefinitions = new ColumnDefinitions("*,*,*,*");
                    int rr = 0;
                    Action<string[], bool, Color?> riga = (v, h, colD) =>
                    {
                        tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                        for (int j = 0; j < v.Length; j++)
                        {
                            TextBlock t = new TextBlock { Text = v[j], FontWeight = h || (j == 3 && colD.HasValue) ? FontWeight.SemiBold : FontWeight.Normal, HorizontalAlignment = j == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                            if (j == 3 && colD.HasValue) t.Foreground = new SolidColorBrush(colD.Value);
                            Border b = new Border { Child = t, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                            Grid.SetColumn(b, j); Grid.SetRow(b, rr); tab.Children.Add(b);
                        }
                        rr++;
                    };
                    riga(new[] { "Elev.", "Piatto", "Reale", "Δ" }, true, null);
                    foreach (double e in new[] { 0.5, 1, 2, 3, 5, 10 })
                    {
                        double vp = valoreA(esito.El, esito.Piatto, e), vr = valoreA(esito.El, esito.Reale, e), d = vr - vp;
                        riga(new[] { FN(e, 1) + "°", FN(vp, 1), FN(vr, 1), (d >= 0 ? "+" : "") + FN(d, 1) }, false, Math.Abs(d) < 1 ? (Color?)Pal.Fore : d > 0 ? Color.Parse("#21A04A") : TerrRosso);
                    }
                    righe.Children.Clear();
                    Action<string, string> rl = (a, b) =>
                    {
                        TextBlock t = new TextBlock { TextWrapping = TextWrapping.Wrap };
                        t.Inlines.Add(new Avalonia.Controls.Documents.Run { Text = a + ": ", FontWeight = FontWeight.Bold });
                        t.Inlines.Add(new Avalonia.Controls.Documents.Run { Text = b });
                        righe.Children.Add(t);
                    };
                    rl("Primo lobo", (lb.Item1 > 0 ? FN(esito.El[lb.Item1], 1) + "°" : "-") + " (piatto " + (lp.Item1 > 0 ? FN(esito.El[lp.Item1], 1) + "°" : "-") + ")");
                    rl("Primo minimo", lb.Item4 > 0 ? FN(esito.El[lb.Item4], 1) + "°" : "-");
                    rl("Punto di riflessione", double.IsNaN(esito.RiflessioneKm) ? "-" : FN(esito.RiflessioneKm, 1) + " km, " + (esito.RiflessioneMare ? "sul mare" : "sul terreno"));
                    rl("Orizzonte", FN(esito.OrizzonteAvanti, 2) + "°" + (esito.OrizzonteAvanti > 0 ? " (rilievi)" : ""));
                    righe.Children.Add(new TextBlock { Text = f < 30 ? "Uso: rotte DX in HF (angoli bassi 1–15°)." : "Uso: tropo e MS (0–5°), EME con la Luna bassa (guadagno di terra all'alba/tramonto lunare).", FontSize = 12, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
                    Registro.Scrivi("AZIONE", "terreno reale: " + titDir + ", " + FN(f, 3) + " MHz, FOM " + FN(fom, 2) + " dB");
                    // scansione dell'altezza
                    if (scan)
                    {
                        double h0 = f < 30 ? 6 : 2, h1 = 30, dh = f < 30 ? 2 : 1;
                        var sc = await Task.Run(() =>
                        {
                            List<Tuple<double, double>> l = new List<Tuple<double, double>>();
                            for (double h = h0; h <= h1 + 1e-9; h += dh) { ct.ThrowIfCancellationRequested(); l.Add(Tuple.Create(h, figuraMerito(calcola(h, pr.Item1, null), f))); }
                            return l;
                        }, ct);
                        if (ct.IsCancellationRequested) return;
                        scansione = sc;
                        // la figura di merito della scansione confronta ogni altezza con il piatto alla stessa altezza; per l'altezza migliore conta il guadagno
                        var gsc = await Task.Run(() =>
                        {
                            double best = double.NegativeInfinity, hb = double.NaN;
                            for (double h = h0; h <= h1 + 1e-9; h += dh)
                            {
                                ct.ThrowIfCancellationRequested();
                                TerrenoReale.Esito e2 = calcola(h, pr.Item1, null);
                                double a = f < 30 ? 1 : 0.5, b = f < 30 ? 15 : 5, s = 0; int n = 0;
                                for (int i = (int)(a * 4); i <= (int)(b * 4); i++) { s += Math.Pow(10, e2.Reale[i] / 10); n++; }
                                double v = 10 * Math.Log10(s / n);
                                if (v > best) { best = v; hb = h; }
                            }
                            return hb;
                        }, ct);
                        if (ct.IsCancellationRequested) return;
                        hMigliore = gsc;
                        int ic = carte.Children.IndexOf(cAlt);
                        Border nuovo = carta("Altezza migliore per la rotta", FN(hMigliore, 0) + " m", "scansione " + FN(h0, 0) + "–" + FN(h1, 0) + " m (guadagno medio su " + (f < 30 ? "1–15°" : "0,5–5°") + ")", false);
                        FumettoGiallo(nuovo, "Altezza con il guadagno medio piu' alto sugli angoli utili della rotta.", "correnti del modello calcolato spostate in quota: impedenza e accoppiamento col suolo restano quelli del modello.", "Terreno reale");
                        if (ic >= 0) carte.Children[ic] = nuovo;
                    }
                    // rosa
                    if (fareRosa)
                    {
                        dRosa.InvalidateVisual();
                        List<Tuple<double, double>> l = new List<Tuple<double, double>>();
                        for (int a = 0; a < 360; a += 10)
                        {
                            var p2 = await profili(a, dmax, passo, ct);
                            if (ct.IsCancellationRequested) return;
                            double v = await Task.Run(() => figuraMerito(calcola(hUser, p2.Item1, null), f), ct);
                            l.Add(Tuple.Create((double)a, v));
                            rosa = new List<Tuple<double, double>>(l);
                            dRosa.InvalidateVisual();
                        }
                    }
                    if (terrSovrapponi) ShowPatterns();
                    riuscito = true;
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    titProf.Text = "Profilo: " + ex.Message;
                    carte.Children.Add(AvvisoGiallo(ex.Message));
                    Status("Terreno reale: " + ex.Message, true);
                }
                finally
                {
                    // se un calcolo piu' recente ha annullato questo, il pulsante lo riattiva quello
                    if (!ct.IsCancellationRequested)
                    {
                        bCalc.IsEnabled = true;
                        if (riuscito && aVista) Status("Terreno reale: " + "calcolo finito");
                    }
                }
            };

            // ---------------- eventi ----------------
            fProf.Selected += async k =>
            {
                prof = k;
                if (k == "file")
                {
                    try
                    {
                        IReadOnlyList<IStorageFile> fl = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                        {
                            Title = "Profilo del terreno (HorizonProfiler)",
                            AllowMultiple = false,
                            FileTypeFilter = new[] { new FilePickerFileType("Profili (csv, hpr, txt)") { Patterns = new[] { "*.csv", "*.CSV", "*.hpr", "*.HPR", "*.txt", "*.TXT" } } }
                        });
                        if (fl != null && fl.Count > 0)
                        {
                            string pth = fl[0].TryGetLocalPath();
                            profFile = Dem.DaFile(pth);
                            Status("Profilo caricato: " + Path.GetFileName(pth) + " (" + profFile.D.Length + " punti)");
                        }
                    }
                    catch (Exception ex) { Status("Profilo: " + ex.Message, true); }
                }
                terrAggiorna();
            };
            fAz.Selected += k => { modoAz = k; rigaAz.IsVisible = k == "libero"; fDir.IsVisible = k == "traffico"; terrAggiorna(); };
            fDir.Selected += k => { dirTr = int.Parse(k.Substring(1)); terrAggiorna(); };
            fVista.Selected += k => { vista = k; ridisegnaLobo(); };
            cZoom.SelectionChanged += (s, e) => ridisegnaLobo();
            tLoc.LostFocus += (s, e) => { quotaSito = double.NaN; terrAggiorna(); };
            Action leggiQuota = () =>
            {
                string q = (tQuota.Text ?? "").Trim();
                double v = q.Length == 0 ? double.NaN : ParseNum(q, double.NaN);
                if (!double.IsNaN(v) && (v < -50 || v > 9000)) v = double.NaN;
                if (v.Equals(quotaMano) || (double.IsNaN(v) && double.IsNaN(quotaMano))) return;
                quotaMano = v; if (double.IsNaN(v)) tQuota.Text = "";
                aggiornaQuota(); terrAggiorna();
            };
            tQuota.LostFocus += (s, e) => leggiQuota();
            tQuota.KeyUp += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) leggiQuota(); };
            bQuotaDem.Click += (s, e) => { tQuota.Text = ""; leggiQuota(); };
            tLoc.KeyUp += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) { quotaSito = double.NaN; terrAggiorna(); } else aggiornaLocatore(); };
            cSovr.IsCheckedChanged += (s, e) => { terrSovrapponi = cSovr.IsChecked == true; ShowPatterns(); };
            bCalc.Click += (s, e) => terrAggiorna();
            // 1.3.0 (prova del videocorso, 18/11/2026): caselle, menu e campi del sito ricalcolano da soli, come la scelta della direzione
            // (prima la scansione dell'altezza e le superfici restavano spente finche' non si cambiava direzione)
            foreach (CheckBox c in new[] { cScan, cSuMare, cRug, cDiv, cDif, cRosa }) c.IsCheckedChanged += (s, e) => { if (terrAggiorna != null) terrAggiorna(); };
            foreach (ComboBox c in new[] { cTerra, cMare }) c.SelectionChanged += (s, e) => { if (terrAggiorna != null) terrAggiorna(); };
            foreach (TextBox t in new[] { tH, tAz, tDmax })
            {
                t.LostFocus += (s, e) => { if (terrAggiorna != null) terrAggiorna(); };
                t.KeyUp += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter && terrAggiorna != null) terrAggiorna(); };
            }
            bMappa.Click += async (s, e) =>
            {
                string nuovo = await ScegliSullaMappa((tLoc.Text ?? "").Trim().ToUpperInvariant(), ParseNum(tH.Text, 10));
                if (!string.IsNullOrEmpty(nuovo)) { tLoc.Text = nuovo; quotaSito = double.NaN; terrAggiorna(); }
            };
            bCsv.Click += async (s, e) =>
            {
                if (ultimo == null) { Status("Calcola prima il terreno reale", true); return; }
                try
                {
                    IStorageFile fl = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Esporta il lobo sul terreno reale", SuggestedFileName = "terreno_reale_" + FN(ultimoAz, 0) + ".csv", DefaultExtension = "csv",
                        FileTypeChoices = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } }
                    });
                    if (fl == null) return;
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("# YA-NEC terreno reale; " + titDir + "; " + ultimaF.ToString("0.000", CI) + " MHz; antenna " + ultimaH.ToString("0.0", CI) + " m" +
                        (prof != "file" && !double.IsNaN(quotaMano) ? "; quota sito a mano " + quotaMano.ToString("0", CI) + " m" + (double.IsNaN(quotaSito) ? "" : " (DEM " + quotaSito.ToString("0", CI) + " m)") : ""));
                    sb.AppendLine("elevazione_gradi;reale_dBi;piatto_dBi;spazio_libero_dBi");
                    for (int i = 0; i < ultimo.El.Length; i++)
                        sb.AppendLine(ultimo.El[i].ToString("0.00", CI) + ";" + ultimo.Reale[i].ToString("0.00", CI) + ";" + ultimo.Piatto[i].ToString("0.00", CI) + ";" + ultimo.Libero[i].ToString("0.00", CI));
                    using (Stream st = await fl.OpenWriteAsync()) using (StreamWriter w = new StreamWriter(st)) w.Write(sb.ToString());
                    Status("Lobo esportato: " + fl.Name);
                }
                catch (Exception ex) { Status("Esporta CSV: " + ex.Message, true); }
            };

            // valori iniziali: in VHF zoom 0-15 e 20 km; altezza dal modello
            if (model != null && model.Ground.Kind == GroundKind.Real)
                for (int i = 0; i < Grounds.Length; i++) if (Math.Abs(Grounds[i].Item2 - model.Ground.Epsr) < 1e-9 && Math.Abs(Grounds[i].Item3 - model.Ground.Sigma) < 1e-12) { cTerra.SelectedIndex = i; break; }
            fProf.Select("dem", false); fAz.Select("traffico", false); fDir.Select("d0", false); fVista.Select("polare", false);
            rigaAz.IsVisible = false;
            aggiornaLocatore();
            return new ScrollViewer { Content = g };
        }
    }
}
