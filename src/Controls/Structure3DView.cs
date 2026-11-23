// YA-NEC - vista 3D della struttura: fili, correnti a colori, sorgenti, terreno, lobo 3D sovrapposto.
// Mouse: sinistro trascina = ruota; rotella = ingrandisce; destro trascina = sposta; doppio clic = vista iniziale.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System.Numerics;
using System.Runtime.InteropServices;
using YaNec.Engine;

namespace YaNec
{
    public class Structure3DView : Control
    {
        public SegmentGeometry Geo;
        public double[] CurrentNorm;       // 0..1 per segmento (null = colore unico)
        public HashSet<int> SourceSegs = new HashSet<int>();   // 0 = primo
        public bool Ground;
        public PatternGrid Pattern;
        public double PatternMax;
        public bool ShowCurrents = true, ShowPattern = true, ShowAxes = true;
        public bool ShowHalfPowerCone = true;   // cono a -3 dB sul lobo 3D (bozzetto approvato)
        // bozzetto approvato "terreno e quota": piano colorato secondo il tipo di terreno e quote dal suolo
        public bool ShowGroundAndHeight = true;
        public GroundStyle GroundLook;          // null = solo griglia
        // correnti disegnate come funzioni (curve) sopra ogni filo
        public bool CurrentCurves = false, CurrentColors = true;
        /// <summary>1.6.0 (bozzetto «spunti dalla ricerca su GitHub», proposta 4, approvato da IC8TEM il 23/11/2026 14:27): fili colorati
        /// secondo la fase della corrente rispetto all'alimentazione (ruota dei colori: rosso 0°, verde 120°, blu 240°); i segmenti con meno
        /// del 3 % della corrente massima restano grigi (fase non significativa). Idea da NEC-Workbench (GPL-3): nessun codice usato.</summary>
        public bool CurrentPhase;
        const int NFasi = 24;
        public static Color ColoreFase(double gradi)
        {
            double h = ((gradi % 360) + 360) % 360 / 60.0;
            int k = (int)Math.Floor(h); double f = h - k;
            double v = 0.92, p = 0.15, q = 0.15 + (v - 0.15) * (1 - f), t = 0.15 + (v - 0.15) * f;
            double r, g, b;
            switch (k % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }
            return Color.FromRgb((byte)(255 * r), (byte)(255 * g), (byte)(255 * b));
        }
        double FaseRiferimento()
        {
            if (CurrentC == null) return 0;
            foreach (int sg in SourceSegs) if (sg >= 0 && sg < CurrentC.Length && CurrentC[sg].Magnitude > 0) return CurrentC[sg].Phase * 180 / Math.PI;
            int im = 0;
            for (int i = 1; i < CurrentC.Length; i++) if (CurrentC[i].Magnitude > CurrentC[im].Magnitude) im = i;
            return CurrentC.Length > 0 ? CurrentC[im].Phase * 180 / Math.PI : 0;
        }
        // 1.5.9 (bozzetto «Struttura 3D › correnti animate e valori col mouse», approvato alla lettera da IC8TEM il 22/11/2026;
        // idea vista in AntennaSim di EA1FUO, GPL-3: nessun codice usato): correnti complesse del NEC per segmento (A, come la tabella
        // delle correnti), puntini animati e fumetto dei valori al passaggio del mouse
        public Complex[] CurrentC;
        public double AnimSpeed = 1;          // cursore «velocita'» (0,25 - 3)
        public bool HoverValues;              // casella «valori col mouse»
        public const int AnimMaxSeg = 6000;   // oltre, i puntini rallenterebbero troppo il disegno: restano i colori
        bool currentAnimate;
        public bool CurrentAnimate
        {
            get { return currentAnimate; }
            set { currentAnimate = value; AggiornaTimer(); InvalidateVisual(); }
        }
        static int pausaAnimazione;
        sealed class Ripresa : IDisposable { bool fatto; public void Dispose() { if (!fatto) { fatto = true; System.Threading.Interlocked.Decrement(ref pausaAnimazione); } } }
        /// <summary>Ferma i puntini durante calcolo, sweep e ottimizzatore (using (...) attorno al lavoro in sottofondo).</summary>
        public static IDisposable PausaAnimazione() { System.Threading.Interlocked.Increment(ref pausaAnimazione); return new Ripresa(); }
        DispatcherTimer animTimer;
        double animT;
        DateTime animUltimo = DateTime.UtcNow;
        Point? hoverAt;
        public string HeightSummary = "";
        public string GroundValues = "";
        // bozzetto four-square: etichette di ampiezza e fase sulle sorgenti e freccia del lobo
        public bool ShowPhases = true;
        public List<KeyValuePair<int, string>> SourceLabels = new List<KeyValuePair<int, string>>();
        public double LobeAz = double.NaN, LobeEl = double.NaN;       // scritto sul terreno neutro (personalizzato)
        public string Info = "";
        public int HighlightTag = -1;      // filo selezionato nella tabella (evidenziato)
        public Color? HighlightColor;      // 1.2.0 (IC8TEM 17/11/2026 23:46): colore velato del filo scelto, dalle Impostazioni (null = blu di prima)
        // barra Geometria (0.4.2, bozzetto approvato): fili scelti, anteprima tratteggiata arancione, centro della trasformazione
        public HashSet<int> SelectedTags = new HashSet<int>();
        // 1.5.1 (bozzetti approvati da IC8TEM il 19/11/2026 08:10 e 08:12): dove agisce una variabile, in ocra. Valore: campi toccati
        // (X1, Y1, ... raggio); pieno = la usa direttamente, tratteggiato = attraverso un'altra variabile
        public Dictionary<int, List<string>> OcraDiretti, OcraIndiretti;
        public static readonly Color Ocra = Color.Parse("#C98A12");
        // 1.5.2 (bozzetto «Struttura 3D: linee, carichi e reti nel disegno», approvato da IC8TEM il 19/11/2026 08:36)
        public class Oggetto
        {
            public int Tipo;            // 0 linea TL, 1 carico, 2 rete / trasformatore / rete L, 3 stub
            public int Seg1 = -1, Seg2 = -1;   // segmenti (0 = primo)
            public string Etichetta = "";
            public bool Incrociata;
            public int Simbolo;         // carichi: 0 R, 1 L, 2 C, 3 RLC serie, 4 LC parallelo, 5 R+jX
            public int Riga;            // riga nella sua tabella
        }
        public List<Oggetto> Oggetti = new List<Oggetto>();
        public HashSet<int> SegVirtuali = new HashSet<int>();
        public bool ShowObjects = true;
        public event Action<int, int> OggettoCliccato;   // tipo, riga
        readonly List<Tuple<Point, int, int>> zoneOggetti = new List<Tuple<Point, int, int>>();
        static readonly Color ColTl = Color.Parse("#8A3FFC"), ColCarico = Color.Parse("#C2410C"), ColRete = Color.Parse("#0F766E");
        public List<double[]> Preview;                  // segmenti x1 y1 z1 x2 y2 z2 dopo la trasformazione
        public double[] PreviewCenter;                  // punto arancione
        public string PreviewLabel = "", PreviewCenterText = "";
        /// <summary>Clic su un filo (senza trascinare): tag e tasto Ctrl.</summary>
        public event Action<int, bool> WireClicked;
        double lcx, lcy, lsc, lox, loy, loz;            // proiezione dell'ultimo fotogramma (per il clic)
        Point pressAt;
        bool pressCtrl;

        double az = -55, el = 22, zoom = 1, panX, panY;
        Point last;
        bool rotating, panning;

        public Structure3DView()
        {
            ClipToBounds = true;
        }

        void AggiornaTimer()
        {
            if (currentAnimate && animTimer == null)
            {
                animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
                animTimer.Tick += (s, e) =>
                {
                    DateTime ora = DateTime.UtcNow;
                    double dt = Math.Min(0.1, (ora - animUltimo).TotalSeconds);
                    animUltimo = ora;
                    if (!IsEffectivelyVisible || pausaAnimazione > 0 || RiduciAnimazioni() || !ShowCurrents || Geo == null || CurrentC == null || Geo.N > AnimMaxSeg) return;
                    animT += dt * AnimSpeed;
                    InvalidateVisual();
                };
                animTimer.Start();
            }
            else if (!currentAnimate && animTimer != null) { animTimer.Stop(); animTimer = null; }
        }

        // «Riduci animazioni» del sistema: su Windows l'opzione «Mostra animazioni in Windows» (SPI_GETCLIENTAREAANIMATION),
        // letta ogni 5 secondi; sugli altri sistemi Avalonia non la espone
        static bool riduciLetto; static bool riduciValore; static DateTime riduciQuando;
        [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint a, uint b, ref bool c, uint d);
        static bool RiduciAnimazioni()
        {
            if (!OperatingSystem.IsWindows()) return false;
            if (riduciLetto && (DateTime.UtcNow - riduciQuando).TotalSeconds < 5) return riduciValore;
            try { bool on = true; if (SystemParametersInfo(0x1042, 0, ref on, 0)) riduciValore = !on; } catch { riduciValore = false; }
            riduciLetto = true; riduciQuando = DateTime.UtcNow;
            return riduciValore;
        }

        public void ResetView()
        {
            az = -55; el = 22; zoom = 1; panX = 0; panY = 0;
            InvalidateVisual();
        }

        public void SetView(double azDeg, double elDeg)
        {
            az = azDeg; el = elDeg;
            InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            PointerPoint pp = e.GetCurrentPoint(this);
            last = pp.Position;
            pressAt = pp.Position;
            pressCtrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
            if (e.ClickCount == 2) { ResetView(); return; }
            if (pp.Properties.IsLeftButtonPressed) rotating = true;
            else if (pp.Properties.IsRightButtonPressed || pp.Properties.IsMiddleButtonPressed) panning = true;
            e.Pointer.Capture(this);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (!rotating && !panning)
            {
                if (HoverValues) { hoverAt = e.GetPosition(this); InvalidateVisual(); }
                return;
            }
            hoverAt = null;
            Point p = e.GetPosition(this);
            double dx = p.X - last.X, dy = p.Y - last.Y;
            last = p;
            if (rotating)
            {
                az -= dx * 0.5;
                el += dy * 0.5;
                el = Math.Max(-90, Math.Min(90, el));
            }
            else
            {
                panX += dx; panY += dy;
            }
            InvalidateVisual();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            Point rp = e.GetPosition(this);
            if (rotating && OggettoCliccato != null && Math.Abs(rp.X - pressAt.X) + Math.Abs(rp.Y - pressAt.Y) < 4)
            {
                foreach (Tuple<Point, int, int> z in zoneOggetti)
                    if (Math.Abs(z.Item1.X - rp.X) < 14 && Math.Abs(z.Item1.Y - rp.Y) < 14) { OggettoCliccato(z.Item2, z.Item3); rotating = panning = false; e.Pointer.Capture(null); return; }
            }
            if (rotating && WireClicked != null && Geo != null && Math.Abs(rp.X - pressAt.X) + Math.Abs(rp.Y - pressAt.Y) < 4)
            {
                // filo piu' vicino al punto cliccato (entro 7 pixel)
                double best = 7; int tag = -1;
                for (int i = 0; i < Geo.N; i++)
                {
                    double ax, ay, bx, by;
                    Project(Geo.X1[i], Geo.Y1[i], Geo.Z1[i], lcx, lcy, lsc, lox, loy, loz, out ax, out ay);
                    Project(Geo.X2[i], Geo.Y2[i], Geo.Z2[i], lcx, lcy, lsc, lox, loy, loz, out bx, out by);
                    double vx = bx - ax, vy = by - ay, l2 = vx * vx + vy * vy;
                    double t = l2 > 0 ? Math.Max(0, Math.Min(1, ((rp.X - ax) * vx + (rp.Y - ay) * vy) / l2)) : 0;
                    double dx = ax + t * vx - rp.X, dy = ay + t * vy - rp.Y, d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < best) { best = d; tag = Geo.Tag[i]; }
                }
                if (tag >= 0) WireClicked(tag, pressCtrl);
            }
            rotating = panning = false;
            e.Pointer.Capture(null);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (hoverAt != null) { hoverAt = null; InvalidateVisual(); }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            zoom *= e.Delta.Y > 0 ? 1.15 : 1 / 1.15;
            zoom = Math.Max(0.1, Math.Min(50, zoom));
            InvalidateVisual();
            e.Handled = true;
        }

        // proiezione: u orizzontale sullo schermo, v verticale
        void Project(double x, double y, double z, double cx, double cy, double sc, double ox, double oy, double oz, out double sx, out double sy)
        {
            double a = az * Math.PI / 180, b = el * Math.PI / 180;
            x -= ox; y -= oy; z -= oz;
            double u = -x * Math.Sin(a) + y * Math.Cos(a);
            double w = x * Math.Cos(a) + y * Math.Sin(a);
            double v = z * Math.Cos(b) - w * Math.Sin(b);
            sx = cx + sc * u;
            sy = cy - sc * v;
        }

        public override void Render(DrawingContext ctx)
        {
            // 0.4.20: ritaglio esplicito sul rettangolo della vista, oltre a ClipToBounds (nella schermata del 14/11 17:49 le linee uscivano sopra la finestra)
            using (ctx.PushClip(new Rect(0, 0, Bounds.Width, Bounds.Height)))
            using (Prestazioni.Misura("Disegno della struttura 3D (un fotogramma)", null, false)) Disegna(ctx);
        }

        void Disegna(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.ChartBg), null, new Rect(0, 0, W, H));
            if (Geo == null || Geo.N == 0)
            {
                Draw.TextC(ctx, "Apri un modello (.nec o .maa) per vedere la struttura", W / 2, H / 2, 15, Pal.Muted);
                return;
            }
            double minx = 1e30, miny = 1e30, minz = 1e30, maxx = -1e30, maxy = -1e30, maxz = -1e30;
            for (int i = 0; i < Geo.N; i++)
            {
                minx = Math.Min(minx, Math.Min(Geo.X1[i], Geo.X2[i])); maxx = Math.Max(maxx, Math.Max(Geo.X1[i], Geo.X2[i]));
                miny = Math.Min(miny, Math.Min(Geo.Y1[i], Geo.Y2[i])); maxy = Math.Max(maxy, Math.Max(Geo.Y1[i], Geo.Y2[i]));
                minz = Math.Min(minz, Math.Min(Geo.Z1[i], Geo.Z2[i])); maxz = Math.Max(maxz, Math.Max(Geo.Z1[i], Geo.Z2[i]));
            }
            if (Geo.M > 0 && Geo.PX != null)
                for (int k = 0; k < Geo.M; k++)
                {
                    minx = Math.Min(minx, Geo.PX[k]); maxx = Math.Max(maxx, Geo.PX[k]);
                    miny = Math.Min(miny, Geo.PY[k]); maxy = Math.Max(maxy, Geo.PY[k]);
                    minz = Math.Min(minz, Geo.PZ[k]); maxz = Math.Max(maxz, Geo.PZ[k]);
                }
            List<double> levels = HeightLevels();
            bool quote = Ground && ShowGroundAndHeight && levels.Count > 0;
            HeightSummary = HeightText(levels);
            if (quote) minz = Math.Min(minz, 0);
            double ox = (minx + maxx) / 2, oy = (miny + maxy) / 2, oz = (minz + maxz) / 2;
            double R = 0.5 * Math.Sqrt((maxx - minx) * (maxx - minx) + (maxy - miny) * (maxy - miny) + (maxz - minz) * (maxz - minz));
            if (R <= 0) R = 1;
            double sc = 0.42 * Math.Min(W, H) / R * zoom;
            double cx = W / 2 + panX, cy = H / 2 + panY;
            double sx1, sy1, sx2, sy2;
            lcx = cx; lcy = cy; lsc = sc; lox = ox; loy = oy; loz = oz;

            // terreno: piano z = 0 colorato secondo il tipo (oppure solo la griglia)
            if (Ground)
            {
                double gr = R * 1.4, step = Draw.NiceStep(2 * gr, 10);
                double g0x = Math.Floor((ox - gr) / step) * step, g0y = Math.Floor((oy - gr) / step) * step;
                double g1x = Math.Ceiling((ox + gr) / step) * step, g1y = Math.Ceiling((oy + gr) / step) * step;
                if (ShowGroundAndHeight && GroundLook != null)
                {
                    double ccx = cx, ccy = cy, csc = sc, cox = ox, coy = oy, coz = oz;
                    Func<double, double, Point> proj = (px, py) =>
                    {
                        double qx, qy;
                        Project(px, py, 0, ccx, ccy, csc, cox, coy, coz, out qx, out qy);
                        return new Point(qx, qy);
                    };
                    GroundPainter.Plane(ctx, proj, g0x, g0y, g1x, g1y, step, GroundLook);
                    if (GroundLook == GroundPainter.Neutral && !string.IsNullOrEmpty(GroundValues))
                    {
                        Point pc = proj((g0x + g1x) / 2, g0y + (g1y - g0y) * 0.15);
                        Draw.TextC(ctx, GroundValues, pc.X, pc.Y, 13, Pal.Rgb(70, 70, 70), FontWeight.SemiBold);
                    }
                }
                else
                {
                    IPen gp = Pal.P(Pal.A(120, Pal.Green), 1);
                    for (double gx = g0x; gx <= g1x + 1e-9; gx += step)
                    {
                        Project(gx, g0y, 0, cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                        Project(gx, g1y, 0, cx, cy, sc, ox, oy, oz, out sx2, out sy2);
                        ctx.DrawLine(gp, new Point(sx1, sy1), new Point(sx2, sy2));
                    }
                    for (double gy = g0y; gy <= g1y + 1e-9; gy += step)
                    {
                        Project(g0x, gy, 0, cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                        Project(g1x, gy, 0, cx, cy, sc, ox, oy, oz, out sx2, out sy2);
                        ctx.DrawLine(gp, new Point(sx1, sy1), new Point(sx2, sy2));
                    }
                }
            }

            // assi
            if (ShowAxes)
            {
                double L = R * 0.35;
                string[] nm = { "X", "Y", "Z" };
                Color[] cc = { Pal.Rgb(220, 60, 60), Pal.Rgb(40, 170, 70), Pal.Rgb(60, 110, 230) };
                double[,] d = { { L, 0, 0 }, { 0, L, 0 }, { 0, 0, L } };
                Project(0, 0, 0, cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                for (int k = 0; k < 3; k++)
                {
                    Project(d[k, 0], d[k, 1], d[k, 2], cx, cy, sc, ox, oy, oz, out sx2, out sy2);
                    ctx.DrawLine(Pal.P(cc[k], 1.5), new Point(sx1, sy1), new Point(sx2, sy2));
                    Draw.TextC(ctx, nm[k], sx2 + (sx2 - sx1) * 0.08, sy2 + (sy2 - sy1) * 0.08, 12, cc[k], FontWeight.Bold);
                }
            }

            // lobo 3D
            if (ShowPattern && Pattern != null)
            {
                double rp = R * 0.95;
                IPen pp = Pal.P(Pal.A(110, Pal.Orange), 0.8);
                Func<int, int, Point> P3 = (ti, pj) =>
                {
                    double g = Pattern.Total[ti, pj % Pattern.NPhi];
                    double rr = Math.Max(0, (g - PatternMax + 30) / 30.0) * rp;
                    double th = Pattern.ThetaAt(ti) * Math.PI / 180, ph = Pattern.PhiAt(pj % Pattern.NPhi) * Math.PI / 180;
                    double px = ox + rr * Math.Sin(th) * Math.Cos(ph), py = oy + rr * Math.Sin(th) * Math.Sin(ph), pz = oz + rr * Math.Cos(th);
                    double qx, qy;
                    Project(px, py, pz, cx, cy, sc, ox, oy, oz, out qx, out qy);
                    return new Point(qx, qy);
                };
                int every = Math.Max(1, (int)Math.Round(10.0 / Pattern.PhiStep));
                for (int ip = 0; ip < Pattern.NPhi; ip += every)
                    for (int it = 0; it < Pattern.NTheta - 1; it++)
                        ctx.DrawLine(pp, P3(it, ip), P3(it + 1, ip));
                int everyT = Math.Max(1, (int)Math.Round(10.0 / Pattern.ThetaStep));
                for (int it = everyT; it < Pattern.NTheta; it += everyT)
                    for (int ip = 0; ip < Pattern.NPhi; ip++)
                        ctx.DrawLine(pp, P3(it, ip), P3(it, ip + 1));

                // cono a -3 dB (bozzetto approvato): bordo del lobo principale a meta' potenza, tratteggiato, con i raggi dal centro
                if (ShowHalfPowerCone)
                {
                    double thr = PatternMax - 3, r3 = (27.0 / 30.0) * rp;
                    IPen cp = Pal.P(Pal.A(230, Pal.Red), 1.6, Pal.Dash);
                    Func<double, double, Point> PD = (thDeg, phDeg) =>
                    {
                        double th = thDeg * Math.PI / 180, ph = phDeg * Math.PI / 180, qx, qy;
                        Project(ox + r3 * Math.Sin(th) * Math.Cos(ph), oy + r3 * Math.Sin(th) * Math.Sin(ph), oz + r3 * Math.Cos(th), cx, cy, sc, ox, oy, oz, out qx, out qy);
                        return new Point(qx, qy);
                    };
                    // per ogni azimut: le due elevazioni dove il guadagno passa per -3 dB (lobo che contiene il massimo)
                    List<Point> lo = new List<Point>(), hi = new List<Point>();
                    List<double> phis = new List<double>();
                    for (int ip = 0; ip < Pattern.NPhi; ip++)
                    {
                        double a1 = double.NaN, a2 = double.NaN;
                        for (int it = 0; it < Pattern.NTheta - 1; it++)
                        {
                            double g1 = Pattern.Total[it, ip], g2 = Pattern.Total[it + 1, ip];
                            if ((g1 < thr) == (g2 < thr)) continue;
                            double t = (thr - g1) / (g2 - g1), thc = Pattern.ThetaAt(it) + t * Pattern.ThetaStep;
                            if (g1 < thr && double.IsNaN(a1)) a1 = thc;
                            else if (g1 >= thr) a2 = thc;
                        }
                        if (double.IsNaN(a1) && double.IsNaN(a2)) continue;
                        if (double.IsNaN(a1)) a1 = 0;
                        if (double.IsNaN(a2)) a2 = Pattern.ThetaAt(Pattern.NTheta - 1);
                        lo.Add(PD(a1, Pattern.PhiAt(ip))); hi.Add(PD(a2, Pattern.PhiAt(ip))); phis.Add(Pattern.PhiAt(ip));
                    }
                    Point o0 = PD(0, 0);
                    double qox, qoy;
                    Project(ox, oy, oz, cx, cy, sc, ox, oy, oz, out qox, out qoy);
                    Point org = new Point(qox, qoy);
                    for (int k = 0; k + 1 < lo.Count; k++)
                    {
                        if (Math.Abs(phis[k + 1] - phis[k]) > Pattern.PhiStep * 1.5) continue;
                        ctx.DrawLine(cp, lo[k], lo[k + 1]);
                        ctx.DrawLine(cp, hi[k], hi[k + 1]);
                    }
                    int passo = Math.Max(1, lo.Count / 4);
                    for (int k = 0; k < lo.Count; k += passo) { ctx.DrawLine(cp, org, lo[k]); ctx.DrawLine(cp, org, hi[k]); }
                    if (lo.Count > 0) Draw.Text(ctx, "-3 dB", hi[0].X + 4, hi[0].Y - 8, 12, Pal.Red, FontWeight.SemiBold);
                }
            }

            // filo selezionato nella tabella: alone sotto il filo
            if (HighlightTag >= 0)
                for (int i = 0; i < Geo.N; i++)
                {
                    if (Geo.Tag[i] != HighlightTag) continue;
                    Project(Geo.X1[i], Geo.Y1[i], Geo.Z1[i], cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                    Project(Geo.X2[i], Geo.Y2[i], Geo.Z2[i], cx, cy, sc, ox, oy, oz, out sx2, out sy2);
                    ctx.DrawLine(Pal.P(HighlightColor ?? Pal.A(120, Pal.Accent), HighlightColor.HasValue ? 11 : 8), new Point(sx1, sy1), new Point(sx2, sy2));
                }
            // 1.5.1: dove agisce la variabile (ocra)
            if (OcraDiretti != null || OcraIndiretti != null)
            {
                Pen pd = new Pen(new SolidColorBrush(Ocra), 7) { LineCap = PenLineCap.Round };
                Pen pi = new Pen(new SolidColorBrush(Ocra), 5) { DashStyle = new DashStyle(new double[] { 2, 1.5 }, 0) };
                Pen pf = new Pen(new SolidColorBrush(Ocra), 2.5) { LineCap = PenLineCap.Round };
                Dictionary<int, int[]> primoUltimo = new Dictionary<int, int[]>();
                for (int i = 0; i < Geo.N; i++)
                {
                    int t = Geo.Tag[i];
                    bool d = OcraDiretti != null && OcraDiretti.ContainsKey(t), ind = !d && OcraIndiretti != null && OcraIndiretti.ContainsKey(t);
                    if (!d && !ind) continue;
                    Project(Geo.X1[i], Geo.Y1[i], Geo.Z1[i], cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                    Project(Geo.X2[i], Geo.Y2[i], Geo.Z2[i], cx, cy, sc, ox, oy, oz, out sx2, out sy2);
                    ctx.DrawLine(d ? pd : pi, new Point(sx1, sy1), new Point(sx2, sy2));
                    if (d) { int[] pu; if (!primoUltimo.TryGetValue(t, out pu)) primoUltimo[t] = new[] { i, i }; else pu[1] = i; }
                }
                // frecce: lunghezza (alle punte, lungo il filo), posizione o quota (lungo l'asse della coordinata)
                foreach (KeyValuePair<int, int[]> kv in primoUltimo)
                {
                    List<string> campi = OcraDiretti[kv.Key];
                    int a = kv.Value[0], b = kv.Value[1];
                    double ex = Geo.X2[b] - Geo.X1[a], ey = Geo.Y2[b] - Geo.Y1[a], ez = Geo.Z2[b] - Geo.Z1[a], L = Math.Sqrt(ex * ex + ey * ey + ez * ez);
                    if (L < 1e-9) continue;
                    double[] dir = { ex / L, ey / L, ez / L };
                    double ax, ay, bx, by;
                    Project(Geo.X1[a], Geo.Y1[a], Geo.Z1[a], cx, cy, sc, ox, oy, oz, out ax, out ay);
                    Project(Geo.X2[b], Geo.Y2[b], Geo.Z2[b], cx, cy, sc, ox, oy, oz, out bx, out by);
                    foreach (string c in campi)
                    {
                        if (c.Length != 2) continue;
                        int k = "XYZ".IndexOf(c[0]); if (k < 0) continue;
                        if (Math.Abs(dir[k]) > 0.7)
                        {
                            // lunghezza: prolunga le due punte
                            double ux = bx - ax, uy = by - ay, ul = Math.Sqrt(ux * ux + uy * uy); if (ul < 1) continue;
                            ux /= ul; uy /= ul;
                            ctx.DrawLine(pf, new Point(bx, by), new Point(bx + 12 * ux, by + 12 * uy));
                            ctx.DrawLine(pf, new Point(ax, ay), new Point(ax - 12 * ux, ay - 12 * uy));
                        }
                        else
                        {
                            double px0, py0, px1, py1;
                            Project(Geo.X2[b], Geo.Y2[b], Geo.Z2[b], cx, cy, sc, ox, oy, oz, out px0, out py0);
                            double[] q = { Geo.X2[b], Geo.Y2[b], Geo.Z2[b] }; q[k] += 0.05 * Math.Max(L, 0.1);
                            Project(q[0], q[1], q[2], cx, cy, sc, ox, oy, oz, out px1, out py1);
                            double vx = px1 - px0, vy = py1 - py0, vl = Math.Sqrt(vx * vx + vy * vy); if (vl < 0.5) continue;
                            vx /= vl; vy /= vl;
                            Point p0 = new Point(px0 + 6 * vx, py0 + 6 * vy), p1 = new Point(px0 + 22 * vx, py0 + 22 * vy);
                            ctx.DrawLine(pf, p0, p1);
                            ctx.DrawLine(pf, p1, new Point(p1.X - 6 * vx + 4 * vy, p1.Y - 6 * vy - 4 * vx));
                            ctx.DrawLine(pf, p1, new Point(p1.X - 6 * vx - 4 * vy, p1.Y - 6 * vy + 4 * vx));
                        }
                        break;
                    }
                }
            }
            // fili scelti nella barra Geometria: alone blu
            if (SelectedTags.Count > 0)
                for (int i = 0; i < Geo.N; i++)
                {
                    if (!SelectedTags.Contains(Geo.Tag[i])) continue;
                    Project(Geo.X1[i], Geo.Y1[i], Geo.Z1[i], cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                    Project(Geo.X2[i], Geo.Y2[i], Geo.Z2[i], cx, cy, sc, ox, oy, oz, out sx2, out sy2);
                    ctx.DrawLine(Pal.P(Pal.A(150, Pal.Accent), 9), new Point(sx1, sy1), new Point(sx2, sy2));
                }
            // fili. 0.4.20 (schermata di IC8TEM del 14/11/2026 17:49, parabola da 30566 segmenti): con decine di migliaia di DrawLine, ognuna
            // con la sua penna, la scheda grafica disegnava linee spurie fuori dalla vista e il disco diventava una macchia nera. Ora i fili
            // si raccolgono in poche geometrie (una per colore, 16 livelli di corrente) disegnate una volta sola, e lo spessore scende con
            // il numero dei segmenti (2,2 px fino a 3000, poi fino a 0,6 px). I punti fuori da un margine ampio della vista si scartano.
            {
                bool fasi = ShowCurrents && CurrentPhase && CurrentC != null && CurrentNorm != null && CurrentC.Length == Geo.N && CurrentNorm.Length == Geo.N;
                bool colori = !fasi && ShowCurrents && (CurrentColors || currentAnimate) && CurrentNorm != null;
                double spess = Geo.N <= 3000 ? 2.2 : Geo.N <= 8000 ? 1.4 : Geo.N <= 15000 ? 1.0 : 0.6;
                int nLiv = colori ? 16 : fasi ? NFasi + 1 : 1;
                double fRif = fasi ? FaseRiferimento() : 0;
                StreamGeometry[] geo = new StreamGeometry[nLiv];
                StreamGeometryContext[] gc = new StreamGeometryContext[nLiv];
                bool[] usato = new bool[nLiv];
                double lim = 4 * Math.Max(W, H) + 1000;
                try
                {
                    for (int i = 0; i < Geo.N; i++)
                    {
                        Project(Geo.X1[i], Geo.Y1[i], Geo.Z1[i], cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                        Project(Geo.X2[i], Geo.Y2[i], Geo.Z2[i], cx, cy, sc, ox, oy, oz, out sx2, out sy2);
                        if (double.IsNaN(sx1) || double.IsNaN(sy1) || double.IsNaN(sx2) || double.IsNaN(sy2)) continue;
                        if (Math.Abs(sx1 - W / 2) > lim || Math.Abs(sy1 - H / 2) > lim || Math.Abs(sx2 - W / 2) > lim || Math.Abs(sy2 - H / 2) > lim) continue;
                        int k = 0;
                        if (colori) k = i < CurrentNorm.Length ? Math.Max(0, Math.Min(nLiv - 1, (int)(CurrentNorm[i] * nLiv))) : 0;
                        else if (fasi)
                        {
                            if (CurrentNorm[i] < 0.03) k = NFasi;
                            else
                            {
                                double fg = (CurrentC[i].Phase * 180 / Math.PI - fRif + 720) % 360;
                                k = (int)Math.Round(fg / (360.0 / NFasi)) % NFasi;
                            }
                        }
                        if (gc[k] == null) { geo[k] = new StreamGeometry(); gc[k] = geo[k].Open(); }
                        gc[k].BeginFigure(new Point(sx1, sy1), false);
                        gc[k].LineTo(new Point(sx2, sy2));
                        gc[k].EndFigure(false);
                        usato[k] = true;
                    }
                }
                finally
                {
                    for (int k = 0; k < nLiv; k++) if (gc[k] != null) gc[k].Dispose();
                }
                for (int k = 0; k < nLiv; k++)
                {
                    if (!usato[k]) continue;
                    Color c = colori ? Pal.Heat((k + 0.5) / nLiv) : fasi ? (k == NFasi ? Pal.A(140, Pal.Muted) : ColoreFase(k * 360.0 / NFasi)) : Pal.Fore;
                    ctx.DrawGeometry(null, Pal.P(c, fasi && k < NFasi ? spess + 0.8 : spess), geo[k]);
                }
            }
            // 1.5.9: superfici (patch SP/SM/SC): quadratini di lato pari alla radice dell'area, nel piano t1-t2, con la normale
            if (Geo.M > 0 && Geo.PX != null)
            {
                IBrush fp = Pal.B(Pal.A(70, Pal.Cyan));
                IPen bp = Pal.P(Pal.A(170, Pal.Cyan), 0.8);
                for (int k = 0; k < Geo.M; k++)
                {
                    double h = 0.5 * Math.Sqrt(Math.Abs(Geo.PArea[k]));
                    double[] sx = { 1, -1, -1, 1 }, sy = { 1, 1, -1, -1 };
                    StreamGeometry pg = new StreamGeometry();
                    using (StreamGeometryContext pc = pg.Open())
                    {
                        for (int c = 0; c < 4; c++)
                        {
                            double qx = Geo.PX[k] + h * (sx[c] * Geo.T1X[k] + sy[c] * Geo.T2X[k]);
                            double qy = Geo.PY[k] + h * (sx[c] * Geo.T1Y[k] + sy[c] * Geo.T2Y[k]);
                            double qz = Geo.PZ[k] + h * (sx[c] * Geo.T1Z[k] + sy[c] * Geo.T2Z[k]);
                            double ux, uy;
                            Project(qx, qy, qz, cx, cy, sc, ox, oy, oz, out ux, out uy);
                            if (c == 0) pc.BeginFigure(new Point(ux, uy), true); else pc.LineTo(new Point(ux, uy));
                        }
                        pc.EndFigure(true);
                    }
                    ctx.DrawGeometry(fp, bp, pg);
                }
            }

            // estremi dei segmenti (piccole tacche) e sorgenti
            if (Geo.N < 3000)
                for (int i = 0; i < Geo.N; i++)
                {
                    Project(Geo.X1[i], Geo.Y1[i], Geo.Z1[i], cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                    ctx.DrawEllipse(Pal.B(Pal.A(160, Pal.Muted)), null, new Point(sx1, sy1), 1.2, 1.2);
                }
            if (ShowObjects) DisegnaOggetti(ctx, cx, cy, sc, ox, oy, oz);
            foreach (int s in SourceSegs)
            {
                if (s < 0 || s >= Geo.N) continue;
                Project(Geo.CenterX(s), Geo.CenterY(s), Geo.CenterZ(s), cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                ctx.DrawEllipse(Pal.B(Pal.Red), Pal.P(Pal.Fore, 1), new Point(sx1, sy1), 5, 5);
            }

            // anteprima della barra Geometria: tratteggio arancione, centro e angolo
            if (Preview != null)
            {
                IPen pa = Pal.P(Pal.Orange, 2.4, Pal.Dash);
                foreach (double[] q in Preview)
                {
                    Project(q[0], q[1], q[2], cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                    Project(q[3], q[4], q[5], cx, cy, sc, ox, oy, oz, out sx2, out sy2);
                    ctx.DrawLine(pa, new Point(sx1, sy1), new Point(sx2, sy2));
                }
                if (PreviewCenter != null)
                {
                    Project(PreviewCenter[0], PreviewCenter[1], PreviewCenter[2], cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                    ctx.DrawEllipse(Pal.B(Pal.Orange), null, new Point(sx1, sy1), 5, 5);
                    if (!string.IsNullOrEmpty(PreviewLabel)) Draw.Text(ctx, PreviewLabel, sx1 - 14, sy1 + 30, 15, Pal.Orange, FontWeight.Bold);
                }
                if (!string.IsNullOrEmpty(PreviewCenterText))
                {
                    ctx.DrawEllipse(Pal.B(Pal.Orange), null, new Point(W - 260, H - 18), 5, 5);
                    Draw.Text(ctx, PreviewCenterText, W - 250, H - 27, 13, Pal.Orange);
                }
            }

            // 1.5.9: puntini che scorrono lungo i fili (piu' fitti e veloci dove la corrente e' forte, sfasati con la fase)
            if (ShowCurrents && currentAnimate && CurrentNorm != null && CurrentC != null && CurrentNorm.Length == Geo.N && CurrentC.Length == Geo.N)
            {
                if (Geo.N <= AnimMaxSeg) DisegnaPuntini(ctx, cx, cy, sc, ox, oy, oz, W, H);
                else Draw.Text(ctx, Lang.F("animazione spenta oltre {0} segmenti", AnimMaxSeg.ToString()), 10, 26, 11.5, Pal.Muted);
            }

            // correnti come funzioni sopra i fili
            if (ShowCurrents && CurrentCurves && CurrentNorm != null && CurrentNorm.Length == Geo.N)
                DrawCurrentCurves(ctx, cx, cy, sc, ox, oy, oz, R);

            // quote altimetriche dal suolo
            if (quote) DrawHeights(ctx, levels, cx, cy, sc, ox, oy, oz, R, minx, miny);

            // fasi delle sorgenti ed direzione del lobo
            if (ShowPhases) DrawPhasesAndLobe(ctx, cx, cy, sc, ox, oy, oz, R);

            // legenda
            Draw.Text(ctx, Info + (Ground && HeightSummary.Length > 0 ? ", " + HeightSummary : ""), 10, 8, 12.5, Pal.ChartText);
            Draw.TextR(ctx, "trascina: ruota  |  rotella: zoom  |  tasto destro: sposta  |  doppio clic: vista iniziale", W - 10, H - 22, 11, Pal.Muted);
            if (HoverValues && hoverAt != null) DisegnaFumetto(ctx, hoverAt.Value, cx, cy, sc, ox, oy, oz, R, W, H);
            if (ShowCurrents && CurrentPhase && CurrentC != null && CurrentNorm != null)
            {
                // 1.6.0: scala circolare della fase in basso a sinistra
                double rx = 78, ry = H - 56, r1 = 30, r2 = 18;
                for (int k = 0; k < 36; k++)
                {
                    double a0 = (k * 10 - 90) * Math.PI / 180, a1 = ((k + 1) * 10 - 90) * Math.PI / 180;
                    StreamGeometry sg = new StreamGeometry();
                    using (StreamGeometryContext c = sg.Open())
                    {
                        c.BeginFigure(new Point(rx + r1 * Math.Cos(a0), ry + r1 * Math.Sin(a0)), true);
                        c.LineTo(new Point(rx + r1 * Math.Cos(a1), ry + r1 * Math.Sin(a1)));
                        c.LineTo(new Point(rx + r2 * Math.Cos(a1), ry + r2 * Math.Sin(a1)));
                        c.LineTo(new Point(rx + r2 * Math.Cos(a0), ry + r2 * Math.Sin(a0)));
                        c.EndFigure(true);
                    }
                    ctx.DrawGeometry(Pal.B(ColoreFase(k * 10 + 5)), null, sg);
                }
                Draw.TextC(ctx, "0°", rx, ry - r1 - 15, 10.5, Pal.ChartText);
                Draw.Text(ctx, "120°", rx + r1 * 0.87 + 2, ry + r1 * 0.5 - 4, 10.5, Pal.ChartText);
                Draw.TextR(ctx, "240°", rx - r1 * 0.87 - 2, ry + r1 * 0.5 - 4, 10.5, Pal.ChartText);
                Draw.Text(ctx, Lang.T("fase della corrente rispetto all'alimentazione"), rx + r1 + 34, ry - 7, 11, Pal.ChartText);
            }
            else if (ShowCurrents && (CurrentColors || currentAnimate) && CurrentNorm != null)
            {
                double bx = 10, by = H - 30, bw = 160;
                for (int k = 0; k < 40; k++)
                    ctx.DrawRectangle(Pal.B(Pal.Heat(k / 39.0)), null, new Rect(bx + k * bw / 40, by, bw / 40 + 0.5, 10));
                Draw.Text(ctx, "corrente: bassa", bx, by - 16, 11, Pal.ChartText);
                Draw.TextR(ctx, "alta", bx + bw, by - 16, 11, Pal.ChartText);
            }
        }
        // ------------------------------------------------------------------------------------------------
        // 1.5.9: puntini animati. Per ogni segmento: numero di puntini secondo la lunghezza sullo schermo e la corrente (passo da 40 px
        // a corrente bassa a 12 px al massimo), posizione = frazione(j/n + t * velocita' * |I| + fase/360); colore e misura come «a colori».
        void DisegnaPuntini(DrawingContext ctx, double cx, double cy, double sc, double ox, double oy, double oz, double W, double H)
        {
            int totale = 0;
            const int maxPuntini = 5000;
            IBrush[] pennelli = new IBrush[16];
            for (int i = 0; i < Geo.N && totale < maxPuntini; i++)
            {
                double a = CurrentNorm[i];
                if (a < 0.08) continue;
                double x1, y1, x2, y2;
                Project(Geo.X1[i], Geo.Y1[i], Geo.Z1[i], cx, cy, sc, ox, oy, oz, out x1, out y1);
                Project(Geo.X2[i], Geo.Y2[i], Geo.Z2[i], cx, cy, sc, ox, oy, oz, out x2, out y2);
                if (double.IsNaN(x1) || double.IsNaN(x2)) continue;
                if ((x1 < -20 && x2 < -20) || (x1 > W + 20 && x2 > W + 20) || (y1 < -20 && y2 < -20) || (y1 > H + 20 && y2 > H + 20)) continue;
                double lpx = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                double passo = 40 - 28 * a;
                int n = Math.Max(1, (int)Math.Round(lpx / passo));
                if (lpx < 3 && (i % 3) != 0) continue;   // segmenti cortissimi sullo schermo: uno su tre
                double fase = Math.Atan2(CurrentC[i].Imaginary, CurrentC[i].Real) / (2 * Math.PI);
                int k = Math.Max(0, Math.Min(15, (int)(a * 16)));
                if (pennelli[k] == null) pennelli[k] = Pal.B(Pal.Heat((k + 0.5) / 16));
                double rr = 1.3 + 2.0 * a;
                for (int j = 0; j < n; j++)
                {
                    double u = j / (double)n + animT * 0.6 * a + fase;
                    u -= Math.Floor(u);
                    ctx.DrawEllipse(pennelli[k], null, new Point(x1 + u * (x2 - x1), y1 + u * (y2 - y1)), rr, rr);
                    totale++;
                }
            }
        }

        // 1.5.9: fumetto dei valori. Filo: tag, segmento nel filo e numero NEC, |I| in mA e dB dal massimo, fase, diametro e lunghezza
        // del segmento. Lobo 3D: azimut, elevazione, guadagno in dBi e dB dal massimo. Nessun calcolo nuovo.
        void DisegnaFumetto(DrawingContext ctx, Point m, double cx, double cy, double sc, double ox, double oy, double oz, double R, double W, double H)
        {
            List<string> righe = null;
            double best = 8; int seg = -1;
            for (int i = 0; i < Geo.N; i++)
            {
                double ax, ay, bx, by;
                Project(Geo.X1[i], Geo.Y1[i], Geo.Z1[i], cx, cy, sc, ox, oy, oz, out ax, out ay);
                Project(Geo.X2[i], Geo.Y2[i], Geo.Z2[i], cx, cy, sc, ox, oy, oz, out bx, out by);
                double vx = bx - ax, vy = by - ay, l2 = vx * vx + vy * vy;
                double t = l2 > 0 ? Math.Max(0, Math.Min(1, ((m.X - ax) * vx + (m.Y - ay) * vy) / l2)) : 0;
                double dx = ax + t * vx - m.X, dy = ay + t * vy - m.Y, d = Math.Sqrt(dx * dx + dy * dy);
                if (d < best) { best = d; seg = i; }
            }
            if (seg >= 0)
            {
                int nelFilo = 0;
                for (int i = 0; i <= seg; i++) if (Geo.Tag[i] == Geo.Tag[seg]) nelFilo++;
                righe = new List<string> { Lang.F("Filo {0}, segmento {1} (n. {2} del NEC)", Geo.Tag[seg].ToString(), nelFilo.ToString(), (seg + 1).ToString()) };
                if (CurrentC != null && seg < CurrentC.Length)
                {
                    double mx = 0;
                    foreach (Complex c in CurrentC) mx = Math.Max(mx, c.Magnitude);
                    double ia = CurrentC[seg].Magnitude, db = mx > 0 && ia > 0 ? 20 * Math.Log10(ia / mx) : double.NegativeInfinity;
                    righe.Add("|I| " + Draw.N(ia * 1000, ia * 1000 >= 10 ? 1 : 2) + " mA (" + (double.IsInfinity(db) ? "-∞" : Draw.N(db, 1)) + " dB)");
                    righe.Add(Lang.F("fase {0}°", Draw.N(CurrentC[seg].Phase * 180 / Math.PI, 1)));
                }
                righe.Add(Lang.F("ø {0} mm · segmento {1} mm", Draw.N(Geo.Bi[seg] * 2000, 2), Draw.N(Geo.Length(seg) * 1000, 1)));
            }
            else if (ShowPattern && Pattern != null)
            {
                double rp = R * 0.95, bestL = 10; int bt = -1, bp = -1;
                for (int it = 0; it < Pattern.NTheta; it++)
                    for (int ip = 0; ip < Pattern.NPhi; ip++)
                    {
                        double g = Pattern.Total[it, ip];
                        double rr = Math.Max(0, (g - PatternMax + 30) / 30.0) * rp;
                        if (rr <= 0) continue;
                        double th = Pattern.ThetaAt(it) * Math.PI / 180, ph = Pattern.PhiAt(ip) * Math.PI / 180, qx, qy;
                        Project(ox + rr * Math.Sin(th) * Math.Cos(ph), oy + rr * Math.Sin(th) * Math.Sin(ph), oz + rr * Math.Cos(th), cx, cy, sc, ox, oy, oz, out qx, out qy);
                        double d = Math.Sqrt((qx - m.X) * (qx - m.X) + (qy - m.Y) * (qy - m.Y));
                        if (d < bestL) { bestL = d; bt = it; bp = ip; }
                    }
                if (bt >= 0)
                {
                    double g = Pattern.Total[bt, bp];
                    righe = new List<string>
                    {
                        Lang.T("Lobo 3D"),
                        Lang.F("azimut {0}° · elevazione {1}°", Draw.N(Pattern.PhiAt(bp), 0), Draw.N(90 - Pattern.ThetaAt(bt), 0)),
                        Lang.F("guadagno {0} dBi ({1} dB dal massimo)", Draw.N(g, 2), Draw.N(g - PatternMax, 1))
                    };
                }
            }
            if (righe == null) return;
            double bw = 0;
            foreach (string l in righe) bw = Math.Max(bw, Draw.Ft(l, 12, Pal.Fore).Width);
            double bh = righe.Count * 17 + 10, x = m.X + 14, y = m.Y - bh - 8;
            if (x + bw + 18 > W) x = m.X - bw - 32;
            if (y < 4) y = m.Y + 18;
            Color fondo = Pal.Dark ? Pal.Rgb(0x2c, 0x28, 0x14) : Pal.Rgb(0xff, 0xfb, 0xe6), bordo = Pal.Dark ? Pal.Rgb(0x6f, 0x64, 0x30) : Pal.Rgb(0xd9, 0xc6, 0x6b);
            ctx.DrawRectangle(Pal.B(fondo), Pal.P(bordo, 1), new Rect(x, y, bw + 18, bh), 6, 6);
            for (int i = 0; i < righe.Count; i++)
                Draw.Text(ctx, righe[i], x + 9, y + 5 + i * 17, 12, Pal.Fore, i == 0 ? FontWeight.SemiBold : FontWeight.Normal);
        }

        // ------------------------------------------------------------------------------------------------
        // livelli dell'antenna sopra il suolo: si raggruppano le quote dei segmenti; un gruppo sottile (antenna orizzontale)
        // da' una sola quota, un gruppo alto (verticale) da' la base e la cima. Piu' di 10 livelli: solo il minimo e il massimo.
        List<double> HeightLevels()
        {
            List<double> outL = new List<double>();
            if (Geo == null || Geo.N == 0) return outL;
            List<double[]> iv = new List<double[]>();
            double top = 0;
            for (int i = 0; i < Geo.N; i++)
            {
                double a = Math.Min(Geo.Z1[i], Geo.Z2[i]), b = Math.Max(Geo.Z1[i], Geo.Z2[i]);
                iv.Add(new[] { a, b });
                top = Math.Max(top, b);
            }
            if (top <= 1e-6) return outL;
            iv.Sort((p, q) => p[0].CompareTo(q[0]));
            double tol = Math.Max(0.02 * top, 0.01);
            List<double[]> cl = new List<double[]>();
            foreach (double[] p in iv)
            {
                if (cl.Count > 0 && p[0] <= cl[cl.Count - 1][1] + tol) cl[cl.Count - 1][1] = Math.Max(cl[cl.Count - 1][1], p[1]);
                else cl.Add(new[] { p[0], p[1] });
            }
            foreach (double[] c in cl)
            {
                if (c[1] - c[0] <= 0.1 * top) outL.Add((c[0] + c[1]) / 2);
                else { outL.Add(c[0]); outL.Add(c[1]); }
            }
            outL.RemoveAll(v => v < 1e-3);
            outL.Sort();
            if (outL.Count > 10) outL = new List<double> { outL[0], outL[outL.Count - 1] };
            return outL;
        }

        static string HeightText(List<double> lv)
        {
            if (lv.Count == 0) return "";
            if (lv.Count == 1) return Lang.F("antenna a {0} m", Draw.N(lv[0], 2));
            List<string> t = new List<string>();
            foreach (double v in lv) t.Add(Draw.N(v, 2));
            return Lang.F("quote {0} m", string.Join(" / ", t));
        }

        void Arrow(DrawingContext ctx, Point tip, bool up, Color c)
        {
            double d = up ? 1 : -1;
            StreamGeometry g = new StreamGeometry();
            using (StreamGeometryContext gc = g.Open())
            {
                gc.BeginFigure(tip, true);
                gc.LineTo(new Point(tip.X - 4.5, tip.Y + 9 * d));
                gc.LineTo(new Point(tip.X + 4.5, tip.Y + 9 * d));
                gc.EndFigure(true);
            }
            ctx.DrawGeometry(Pal.B(c), null, g);
        }

        // linea di quota verticale a sinistra della struttura: tacca a 0 m e a ogni livello, h dal suolo e distanza tra i livelli
        void DrawHeights(DrawingContext ctx, List<double> lv, double cx, double cy, double sc, double ox, double oy, double oz, double R, double minx, double miny)
        {
            Color c = Pal.ChartText;
            IPen pen = Pal.P(c, 1.6);
            IPen dash = Pal.P(Pal.Muted, 1, Pal.Dash);
            double xd = minx - 0.12 * R, yd = miny - 0.12 * R;
            List<double> all = new List<double> { 0 };
            all.AddRange(lv);
            Point[] p = new Point[all.Count];
            for (int k = 0; k < all.Count; k++)
            {
                double qx, qy;
                Project(xd, yd, all[k], cx, cy, sc, ox, oy, oz, out qx, out qy);
                p[k] = new Point(qx, qy);
            }
            ctx.DrawLine(pen, p[0], p[p.Length - 1]);
            for (int k = 0; k < p.Length; k++)
            {
                ctx.DrawLine(pen, new Point(p[k].X - 9, p[k].Y), new Point(p[k].X + 9, p[k].Y));
                Draw.TextR(ctx, Draw.N(all[k], 2) + " m", p[k].X - 12, p[k].Y - 9, 12, c);
                if (k == 0) continue;
                // freccette alle estremita' dell'intervallo
                bool upw = p[k].Y < p[k - 1].Y;
                if (Math.Abs(p[k].Y - p[k - 1].Y) > 22)
                {
                    Arrow(ctx, p[k], upw, c);
                    Arrow(ctx, p[k - 1], !upw, c);
                }
                string lab = (k == 1 ? "h = " : "Δ = ") + Draw.N(all[k] - all[k - 1], 2) + " m";
                double my = (p[k].Y + p[k - 1].Y) / 2, mx = (p[k].X + p[k - 1].X) / 2;
                if (Math.Abs(p[k].Y - p[k - 1].Y) > 16) Draw.TextR(ctx, lab, mx - 10, my - 9, 12.5, c, FontWeight.Bold);
                // tratteggio dalla tacca al punto della struttura piu' vicino a quella quota
                double best = 1e30, bx = 0, by = 0, bz = 0;
                for (int i = 0; i < Geo.N; i++)
                {
                    double zc = Geo.CenterZ(i);
                    if (Math.Abs(zc - all[k]) > Math.Max(0.05 * all[k], 0.05)) continue;
                    double dx = Geo.CenterX(i) - xd, dy = Geo.CenterY(i) - yd;
                    double d2 = dx * dx + dy * dy;
                    if (d2 < best) { best = d2; bx = Geo.CenterX(i); by = Geo.CenterY(i); bz = zc; }
                }
                if (best < 1e29)
                {
                    double qx, qy;
                    Project(bx, by, bz, cx, cy, sc, ox, oy, oz, out qx, out qy);
                    ctx.DrawLine(dash, new Point(p[k].X + 9, p[k].Y), new Point(qx, qy));
                }
            }
        }

        // correnti come curve: per ogni filo (segmenti consecutivi con lo stesso tag) la curva |I| sopra il filo,
        // zero agli estremi liberi; i fili orizzontali hanno la curva verso l'alto, quelli verticali verso l'esterno
        void DrawCurrentCurves(DrawingContext ctx, double cx, double cy, double sc, double ox, double oy, double oz, double R)
        {
            int n = Geo.N;
            List<int[]> runs = new List<int[]>();
            int st = 0;
            for (int i = 1; i <= n; i++)
            {
                bool brk = i == n || Geo.Tag[i] != Geo.Tag[i - 1] ||
                    Math.Abs(Geo.X1[i] - Geo.X2[i - 1]) + Math.Abs(Geo.Y1[i] - Geo.Y2[i - 1]) + Math.Abs(Geo.Z1[i] - Geo.Z2[i - 1]) > 1e-6;
                if (brk) { runs.Add(new[] { st, i - 1 }); st = i; }
            }
            double longest = 0;
            foreach (int[] r in runs)
            {
                double L = 0;
                for (int i = r[0]; i <= r[1]; i++) L += Geo.Length(i);
                longest = Math.Max(longest, L);
            }
            double amp = Math.Min(0.35 * longest, 0.25 * R);
            if (amp <= 0) return;
            double tolc = 1e-4 * Math.Max(1e-3, R);
            Func<double, double, double, int, int, bool> joined = (x, y, z, a, b) =>
            {
                for (int j = 0; j < n; j++)
                {
                    if (j >= a && j <= b) continue;
                    if (Math.Abs(Geo.X1[j] - x) + Math.Abs(Geo.Y1[j] - y) + Math.Abs(Geo.Z1[j] - z) < tolc) return true;
                    if (Math.Abs(Geo.X2[j] - x) + Math.Abs(Geo.Y2[j] - y) + Math.Abs(Geo.Z2[j] - z) < tolc) return true;
                }
                return false;
            };
            IPen curve = Pal.P(Pal.Red, 1.7);
            IPen hatch = Pal.P(Pal.A(70, Pal.Red), 1);
            foreach (int[] r in runs)
            {
                int a = r[0], b = r[1];
                double dx = Geo.X2[b] - Geo.X1[a], dy = Geo.Y2[b] - Geo.Y1[a], dz = Geo.Z2[b] - Geo.Z1[a];
                double dl = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (dl <= 0) continue;
                dx /= dl; dy /= dl; dz /= dl;
                // direzione della curva: verticale tolta la componente lungo il filo
                double vx = -dz * dx, vy = -dz * dy, vz = 1 - dz * dz;
                double vl = Math.Sqrt(vx * vx + vy * vy + vz * vz);
                if (vl < 0.3)
                {
                    double mx = (Geo.X1[a] + Geo.X2[b]) / 2 - ox, my = (Geo.Y1[a] + Geo.Y2[b]) / 2 - oy;
                    double ml = Math.Sqrt(mx * mx + my * my);
                    if (ml < 1e-6) { mx = 1; my = 0; ml = 1; }
                    vx = mx / ml; vy = my / ml; vz = 0;
                }
                else { vx /= vl; vy /= vl; vz /= vl; }
                List<double[]> pts = new List<double[]>();   // x, y, z, valore
                double v0 = joined(Geo.X1[a], Geo.Y1[a], Geo.Z1[a], a, b) ? CurrentNorm[a] : 0;
                pts.Add(new[] { Geo.X1[a], Geo.Y1[a], Geo.Z1[a], v0 });
                for (int i = a; i <= b; i++) pts.Add(new[] { Geo.CenterX(i), Geo.CenterY(i), Geo.CenterZ(i), CurrentNorm[i] });
                double v1 = joined(Geo.X2[b], Geo.Y2[b], Geo.Z2[b], a, b) ? CurrentNorm[b] : 0;
                pts.Add(new[] { Geo.X2[b], Geo.Y2[b], Geo.Z2[b], v1 });
                Point prev = new Point();
                for (int k = 0; k < pts.Count; k++)
                {
                    double[] q = pts[k];
                    double h = amp * q[3];
                    double wx, wy, px, py;
                    Project(q[0], q[1], q[2], cx, cy, sc, ox, oy, oz, out wx, out wy);
                    Project(q[0] + vx * h, q[1] + vy * h, q[2] + vz * h, cx, cy, sc, ox, oy, oz, out px, out py);
                    Point pc = new Point(px, py);
                    if (k > 0 && k < pts.Count - 1) ctx.DrawLine(hatch, new Point(wx, wy), pc);
                    if (k > 0) ctx.DrawLine(curve, prev, pc);
                    prev = pc;
                }
            }
        }
        // 1.5.2: linee TL (arco viola tratteggiato), carichi (riquadro arancio col simbolo), reti/trasformatori/stub (riquadro verde acqua),
        // fili virtuali di EZNEC (grigi). Le etichette si nascondono oltre 40 oggetti.
        void DisegnaOggetti(DrawingContext ctx, double cx, double cy, double sc, double ox, double oy, double oz)
        {
            zoneOggetti.Clear();
            Func<int, Point> centro = sg => { double px, py; Project(Geo.CenterX(sg), Geo.CenterY(sg), Geo.CenterZ(sg), cx, cy, sc, ox, oy, oz, out px, out py); return new Point(px, py); };
            Func<int, Point> dirSeg = sg =>
            {
                double ax, ay, bx, by;
                Project(Geo.X1[sg], Geo.Y1[sg], Geo.Z1[sg], cx, cy, sc, ox, oy, oz, out ax, out ay);
                Project(Geo.X2[sg], Geo.Y2[sg], Geo.Z2[sg], cx, cy, sc, ox, oy, oz, out bx, out by);
                double l = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
                return l < 1e-6 ? new Point(1, 0) : new Point((bx - ax) / l, (by - ay) / l);
            };
            // fili virtuali: grigi e sottili, con la sigla
            foreach (int sg in SegVirtuali)
            {
                if (sg < 0 || sg >= Geo.N) continue;
                double ax, ay, bx, by;
                Project(Geo.X1[sg], Geo.Y1[sg], Geo.Z1[sg], cx, cy, sc, ox, oy, oz, out ax, out ay);
                Project(Geo.X2[sg], Geo.Y2[sg], Geo.Z2[sg], cx, cy, sc, ox, oy, oz, out bx, out by);
                ctx.DrawLine(Pal.P(Pal.Panel, 4), new Point(ax, ay), new Point(bx, by));
                ctx.DrawLine(Pal.P(Pal.Muted, 1.2), new Point(ax, ay), new Point(bx, by));
            }
            if (Oggetti == null || Oggetti.Count == 0) return;
            bool etichette = Oggetti.Count <= 40;
            IBrush fondo = Pal.B(Pal.Panel);
            foreach (Oggetto o in Oggetti)
            {
                if (o.Seg1 < 0 || o.Seg1 >= Geo.N) continue;
                Point p1 = centro(o.Seg1);
                bool due = o.Seg2 >= 0 && o.Seg2 < Geo.N && o.Seg2 != o.Seg1;
                Point p2 = due ? centro(o.Seg2) : p1;
                if (o.Tipo == 0)
                {
                    // arco tra i due segmenti, gonfio verso l'alto dello schermo
                    double dx = p2.X - p1.X, dy = p2.Y - p1.Y, d = Math.Sqrt(dx * dx + dy * dy);
                    double h = Math.Max(18, 0.35 * d);
                    Point c1 = new Point(p1.X + dx * 0.15, Math.Min(p1.Y, p2.Y) - h), c2 = new Point(p2.X - dx * 0.15, Math.Min(p1.Y, p2.Y) - h);
                    StreamGeometry g = new StreamGeometry();
                    using (StreamGeometryContext gc = g.Open()) { gc.BeginFigure(p1, false); gc.CubicBezierTo(c1, c2, p2); gc.EndFigure(false); }
                    Pen pt = new Pen(new SolidColorBrush(ColTl), 3) { DashStyle = new DashStyle(new double[] { 3, 1.7 }, 0) };
                    ctx.DrawGeometry(null, pt, g);
                    ctx.DrawEllipse(new SolidColorBrush(ColTl), null, p1, 3.5, 3.5);
                    ctx.DrawEllipse(new SolidColorBrush(ColTl), null, p2, 3.5, 3.5);
                    // punto medio della curva di Bezier (t = 0,5)
                    Point pm = new Point(0.125 * p1.X + 0.375 * c1.X + 0.375 * c2.X + 0.125 * p2.X, 0.125 * p1.Y + 0.375 * c1.Y + 0.375 * c2.Y + 0.125 * p2.Y);
                    zoneOggetti.Add(Tuple.Create(pm, 0, o.Riga));
                    if (etichette) Etichetta(ctx, o.Etichetta + (o.Incrociata ? " ✕" : ""), pm, ColTl, fondo);
                }
                else if (o.Tipo == 1)
                {
                    Point u = dirSeg(o.Seg1);
                    double ang = Math.Atan2(u.Y, u.X);
                    using (ctx.PushTransform(Matrix.CreateRotation(ang) * Matrix.CreateTranslation(p1.X, p1.Y)))
                    {
                        IPen pc = new Pen(new SolidColorBrush(ColCarico), 2);
                        IPen ps = new Pen(new SolidColorBrush(ColCarico), 1.6);
                        ctx.DrawRectangle(fondo, pc, new Rect(-15, -9, 30, 18), 3, 3);
                        Simbolo(ctx, ps, o.Simbolo);
                    }
                    zoneOggetti.Add(Tuple.Create(p1, 1, o.Riga));
                    if (etichette) Draw.Text(ctx, o.Etichetta, p1.X + 18, p1.Y - 24, 12, ColCarico, FontWeight.SemiBold);
                }
                else
                {
                    IPen pr = new Pen(new SolidColorBrush(ColRete), 2);
                    if (due) ctx.DrawLine(new Pen(new SolidColorBrush(ColRete), 1.2) { DashStyle = new DashStyle(new double[] { 2, 2 }, 0) }, p1, p2);
                    Point pq = due ? new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2) : p1;
                    ctx.DrawRectangle(fondo, pr, new Rect(pq.X - 13, pq.Y - 13, 26, 26), 4, 4);
                    if (o.Tipo == 3) ctx.DrawLine(pr, new Point(pq.X - 6, pq.Y + 6), new Point(pq.X + 6, pq.Y - 6));
                    else { ctx.DrawEllipse(null, new Pen(new SolidColorBrush(ColRete), 1.6), new Point(pq.X - 4, pq.Y), 5, 5); ctx.DrawEllipse(null, new Pen(new SolidColorBrush(ColRete), 1.6), new Point(pq.X + 4, pq.Y), 5, 5); }
                    zoneOggetti.Add(Tuple.Create(pq, o.Tipo, o.Riga));
                    if (etichette) Draw.Text(ctx, o.Etichetta, pq.X + 17, pq.Y - 20, 12, ColRete, FontWeight.SemiBold);
                }
            }
        }

        static void Etichetta(DrawingContext ctx, string t, Point p, Color c, IBrush fondo)
        {
            double w = 8 + 7.2 * t.Length;
            ctx.DrawRectangle(fondo, new Pen(new SolidColorBrush(c), 1), new Rect(p.X - w / 2, p.Y - 11, w, 20), 4, 4);
            Draw.TextC(ctx, t, p.X, p.Y - 9, 12, c, FontWeight.SemiBold);
        }

        static void Simbolo(DrawingContext ctx, IPen p, int k)
        {
            if (k == 0 || k == 5)
            {
                // resistenza (zig-zag); R+jX: zig-zag e una barra
                ctx.DrawLine(p, new Point(-11, 0), new Point(-8, -4)); ctx.DrawLine(p, new Point(-8, -4), new Point(-4, 4)); ctx.DrawLine(p, new Point(-4, 4), new Point(0, -4));
                ctx.DrawLine(p, new Point(0, -4), new Point(4, 4)); ctx.DrawLine(p, new Point(4, 4), new Point(8, -4)); ctx.DrawLine(p, new Point(8, -4), new Point(11, 0));
                if (k == 5) ctx.DrawLine(p, new Point(-11, 6), new Point(11, 6));
            }
            else if (k == 1)
            {
                for (int i = 0; i < 4; i++) ctx.DrawEllipse(null, p, new Point(-7.5 + 5 * i, 0), 2.5, 4);
            }
            else if (k == 2)
            {
                ctx.DrawLine(p, new Point(-11, 0), new Point(-2, 0)); ctx.DrawLine(p, new Point(2, 0), new Point(11, 0));
                ctx.DrawLine(p, new Point(-2, -6), new Point(-2, 6)); ctx.DrawLine(p, new Point(2, -6), new Point(2, 6));
            }
            else
            {
                // RLC serie (k 3) o LC parallelo (k 4): bobina e condensatore
                for (int i = 0; i < 3; i++) ctx.DrawEllipse(null, p, new Point(-9 + 4 * i, 0), 2, 3.5);
                ctx.DrawLine(p, new Point(5, -5), new Point(5, 5)); ctx.DrawLine(p, new Point(8, -5), new Point(8, 5));
                if (k == 4) { ctx.DrawLine(p, new Point(-12, -7), new Point(12, -7)); }
            }
        }

        void DrawPhasesAndLobe(DrawingContext ctx, double cx, double cy, double sc, double ox, double oy, double oz, double R)
        {
            foreach (KeyValuePair<int, string> kv in SourceLabels)
            {
                int s = kv.Key;
                if (s < 0 || s >= Geo.N) continue;
                int tag = Geo.Tag[s];
                double zt = double.MinValue, xs = Geo.CenterX(s), ys = Geo.CenterY(s);
                for (int i = 0; i < Geo.N; i++) if (Geo.Tag[i] == tag) zt = Math.Max(zt, Math.Max(Geo.Z1[i], Geo.Z2[i]));
                double px, py;
                Project(xs, ys, zt, cx, cy, sc, ox, oy, oz, out px, out py);
                Draw.TextC(ctx, kv.Value, px, py - 22, 13, Pal.ChartText, FontWeight.Bold);
            }
            if (double.IsNaN(LobeAz) || double.IsNaN(LobeEl) || SourceLabels.Count == 0) return;
            double a = LobeAz * Math.PI / 180, e = LobeEl * Math.PI / 180, L = R * 0.9;
            double x0, y0, x1, y1;
            Project(ox, oy, oz, cx, cy, sc, ox, oy, oz, out x0, out y0);
            Project(ox + L * Math.Cos(e) * Math.Cos(a), oy + L * Math.Cos(e) * Math.Sin(a), oz + L * Math.Sin(e), cx, cy, sc, ox, oy, oz, out x1, out y1);
            IPen pen = Pal.P(Pal.Red, 3);
            double dx = x1 - x0, dy = y1 - y0, dl = Math.Sqrt(dx * dx + dy * dy);
            if (dl < 10) return;
            double ux = dx / dl, uy = dy / dl;
            Point start = new Point(x0 + ux * dl * 0.35, y0 + uy * dl * 0.35), tip = new Point(x1, y1);
            ctx.DrawLine(pen, start, tip);
            StreamGeometry g = new StreamGeometry();
            using (StreamGeometryContext gc = g.Open())
            {
                gc.BeginFigure(tip, true);
                gc.LineTo(new Point(tip.X - ux * 16 - uy * 7, tip.Y - uy * 16 + ux * 7));
                gc.LineTo(new Point(tip.X - ux * 16 + uy * 7, tip.Y - uy * 16 - ux * 7));
                gc.EndFigure(true);
            }
            ctx.DrawGeometry(Pal.B(Pal.Red), null, g);
            Draw.Text(ctx, "lobo (az " + Draw.N(LobeAz, 0) + "°, el " + Draw.N(LobeEl, 0) + "°)", (start.X + tip.X) / 2, (start.Y + tip.Y) / 2 + 10, 13, Pal.Red, FontWeight.SemiBold);
        }
    }
}
