// YA-NEC - vista 3D della struttura: fili, correnti a colori, sorgenti, terreno, lobo 3D sovrapposto.
// Mouse: sinistro trascina = ruota; rotella = ingrandisce; destro trascina = sposta; doppio clic = vista iniziale.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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
        public string HeightSummary = "";
        public string GroundValues = "";
        // bozzetto four-square: etichette di ampiezza e fase sulle sorgenti e freccia del lobo
        public bool ShowPhases = true;
        public List<KeyValuePair<int, string>> SourceLabels = new List<KeyValuePair<int, string>>();
        public double LobeAz = double.NaN, LobeEl = double.NaN;       // scritto sul terreno neutro (personalizzato)
        public string Info = "";
        public int HighlightTag = -1;      // filo selezionato nella tabella (evidenziato)
        // barra Geometria (0.4.2, bozzetto approvato): fili scelti, anteprima tratteggiata arancione, centro della trasformazione
        public HashSet<int> SelectedTags = new HashSet<int>();
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
            if (!rotating && !panning) return;
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
                    ctx.DrawLine(Pal.P(Pal.A(120, Pal.Accent), 8), new Point(sx1, sy1), new Point(sx2, sy2));
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
                bool colori = ShowCurrents && CurrentColors && CurrentNorm != null;
                double spess = Geo.N <= 3000 ? 2.2 : Geo.N <= 8000 ? 1.4 : Geo.N <= 15000 ? 1.0 : 0.6;
                int nLiv = colori ? 16 : 1;
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
                    Color c = colori ? Pal.Heat((k + 0.5) / nLiv) : Pal.Fore;
                    ctx.DrawGeometry(null, Pal.P(c, spess), geo[k]);
                }
            }
            // estremi dei segmenti (piccole tacche) e sorgenti
            if (Geo.N < 3000)
                for (int i = 0; i < Geo.N; i++)
                {
                    Project(Geo.X1[i], Geo.Y1[i], Geo.Z1[i], cx, cy, sc, ox, oy, oz, out sx1, out sy1);
                    ctx.DrawEllipse(Pal.B(Pal.A(160, Pal.Muted)), null, new Point(sx1, sy1), 1.2, 1.2);
                }
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
            if (ShowCurrents && CurrentColors && CurrentNorm != null)
            {
                double bx = 10, by = H - 30, bw = 160;
                for (int k = 0; k < 40; k++)
                    ctx.DrawRectangle(Pal.B(Pal.Heat(k / 39.0)), null, new Rect(bx + k * bw / 40, by, bw / 40 + 0.5, 10));
                Draw.Text(ctx, "corrente: bassa", bx, by - 16, 11, Pal.ChartText);
                Draw.TextR(ctx, "alta", bx + bw, by - 16, 11, Pal.ChartText);
            }
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
            if (lv.Count == 1) return "antenna a " + Draw.N(lv[0], 2) + " m";
            List<string> t = new List<string>();
            foreach (double v in lv) t.Add(Draw.N(v, 2));
            return "quote " + string.Join(" / ", t) + " m";
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
