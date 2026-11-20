// YA-NEC - disegno dei calcolatori delle forme semplici (bozzetto approvato alla lettera): i fili del modello NEC generato,
// visti nel piano scelto (o in quello piu' esteso), con il punto di alimentazione, il terreno e le annotazioni del calcolatore
// (condensatore, loop di accoppiamento, quote, resistenze, bobine, schemi delle reti).
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public class CalcDrawing : Control
    {
        public string Nec;
        public CalcOutput Out;
        public bool Mini;   // 0.5.3: miniatura (galleria dei balun): senza scritte

        struct Seg { public double X1, Y1, Z1, X2, Y2, Z2; public int Tag, N; }

        static Color Colore(int c)
        {
            switch (c)
            {
                case 1: return Pal.Orange;
                case 2: return Pal.Violet;
                case 3: return Pal.Muted;
                case 4: return Pal.Green;
                case 5: return Pal.Red;
                default: return Pal.Accent;
            }
        }

        public CalcDrawing() { ClipToBounds = true; MinHeight = 260; }

        // 0.4.8: schema dell'antenna EH dal disegno di IC8TEM (13/11/2026 23:41), alla lettera ma senza scritte e senza sfondo:
        // due cilindri, condensatore variabile tra il filo del cilindro alto e il cilindro basso, bobina da 16 spire dal cilindro
        // basso, bobina da 3 spire (rossa) e connettore SO239. Coordinate del disegno originale (245 x 310), scalate nel riquadro.
        void SchemaEh(DrawingContext ctx, double W, double H)
        {
            double k = Math.Min(W / 260.0, H / 320.0), ox = (W - 245 * k) / 2, oy = (H - 310 * k) / 2;
            Func<double, double, Point> Q = (x, y) => new Point(ox + x * k, oy + y * k);
            IPen filo = Pal.P(Pal.Fore, 1.6), cil = Pal.P(Pal.Accent, 2.2), rosso = Pal.P(Pal.Red, 1.8), grigio = Pal.P(Pal.Muted, 1.6);
            Action<double, double, double, double> cilindro = (x, y, w, h) =>
            {
                double ry = 9;
                ctx.DrawRectangle(Pal.B(Pal.A(40, Pal.Accent)), null, new Rect(Q(x, y), Q(x + w, y + h)));
                ctx.DrawEllipse(Pal.B(Pal.Panel), cil, Q(x + w / 2, y), w / 2 * k, ry * k);
                ctx.DrawLine(cil, Q(x, y), Q(x, y + h));
                ctx.DrawLine(cil, Q(x + w, y), Q(x + w, y + h));
                StreamGeometry g = new StreamGeometry();
                using (StreamGeometryContext gc = g.Open())
                {
                    gc.BeginFigure(Q(x, y + h), false);
                    gc.ArcTo(Q(x + w, y + h), new Size(w / 2 * k, ry * k), 0, false, SweepDirection.CounterClockwise);
                    gc.EndFigure(false);
                }
                ctx.DrawGeometry(null, cil, g);
            };
            Action<double, double, double, int, IPen> bobina = (x, y1, y2, n, pen) =>
            {
                double passo = (y2 - y1) / n;
                for (int i = 0; i < n; i++)
                {
                    StreamGeometry g = new StreamGeometry();
                    using (StreamGeometryContext gc = g.Open())
                    {
                        gc.BeginFigure(Q(x, y1 + i * passo), false);
                        gc.ArcTo(Q(x, y1 + (i + 1) * passo), new Size(6 * k, passo / 2 * k), 0, false, SweepDirection.CounterClockwise);
                        gc.EndFigure(false);
                    }
                    ctx.DrawGeometry(null, pen, g);
                }
            };
            cilindro(72, 22, 68, 46);    // cilindro alto
            cilindro(72, 98, 68, 50);    // cilindro basso
            // filo dal cilindro alto fino al connettore, a destra
            ctx.DrawLine(filo, Q(128, 77), Q(128, 98));
            ctx.DrawLine(filo, Q(128, 157), Q(128, 262));
            // condensatore variabile tra il filo del cilindro alto e il cilindro basso
            ctx.DrawLine(filo, Q(128, 86), Q(162, 86));
            ctx.DrawLine(filo, Q(162, 86), Q(162, 100));
            ctx.DrawLine(Pal.P(Pal.Fore, 3), Q(150, 100), Q(174, 100));
            ctx.DrawLine(Pal.P(Pal.Fore, 3), Q(150, 107), Q(174, 107));
            ctx.DrawLine(filo, Q(162, 107), Q(162, 121));
            ctx.DrawLine(filo, Q(162, 121), Q(140, 121));
            ctx.DrawLine(filo, Q(150, 114), Q(176, 92));
            ctx.DrawLine(filo, Q(176, 92), Q(170, 93));
            ctx.DrawLine(filo, Q(176, 92), Q(174, 98));
            // bobina da 16 spire dal centro del cilindro basso
            ctx.DrawLine(filo, Q(100, 157), Q(100, 165));
            bobina(100, 165, 235, 16, filo);
            // bobina da 3 spire (rossa) e collegamento al connettore
            bobina(100, 238, 262, 3, rosso);
            ctx.DrawLine(rosso, Q(100, 262), Q(128, 262));
            ctx.DrawLine(rosso, Q(128, 262), Q(160, 250));
            ctx.DrawLine(filo, Q(128, 262), Q(160, 272));
            // connettore SO239
            ctx.DrawRectangle(Pal.B(Pal.A(60, Pal.Muted)), grigio, new Rect(Q(160, 246), Q(166, 278)));
            ctx.DrawRectangle(Pal.B(Pal.A(40, Pal.Muted)), grigio, new Rect(Q(166, 252), Q(186, 272)));
            for (double x = 170; x < 186; x += 4) ctx.DrawLine(grigio, Q(x, 252), Q(x, 272));
        }

        // 0.5.3: disegno di costruzione del balun (bozzetto B approvato da IC8TEM il 15/11/2026 19:55): nucleo visto di fronte, ogni spira e' un
        // tratto radiale attraverso l'anello (un colore per filo), prese dell'autotrasformatore in arancio, primario dell'EFHW in nero con il
        // condensatore; ferrite grigio scuro, polvere di ferro rossa (-2) o gialla (-6).
        static readonly Color[] ColoriFili = { Color.Parse("#D22B2B"), Color.Parse("#202020"), Color.Parse("#1F6FE0"), Color.Parse("#9AA0A6") };
        void SchemaBalun(DrawingContext ctx, double W, double H, BalunSchema b)
        {
            if (b == null) return;
            int nuclei = b.Tipo == "guanella" ? 2 : 1;
            double testo = Mini ? 0 : 34;
            double R = Math.Min((W / nuclei) * 0.40, (H - testo) * 0.42), r = R * 0.55;
            Color corpo = b.Polvere ? (b.Nucleo.EndsWith("-6") ? Color.Parse("#D9B400") : Color.Parse("#B23A3A")) : Color.Parse("#3A3A3A");
            IBrush tb = Pal.B(Pal.Fore), mu = Pal.B(Pal.Muted);
            Typeface tf = new Typeface("Inter");
            Action<string, double, double, IBrush, double> scrivi = (t, x, y, br, sz) =>
            {
                if (Mini) return;
                FormattedText ft = new FormattedText(t, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, sz, br);
                ctx.DrawText(ft, new Point(x - ft.Width / 2, y));
            };
            // fili: (colore, spire, angolo iniziale, ampiezza, scostamento)
            Action<Point, double, double, double, double, Color, double> spire = (c, n, a0, amp, off, col, sp) =>
            {
                int vis = (int)Math.Min(n, Mini ? 14 : 28);
                IPen pen = new Pen(new SolidColorBrush(col), sp, lineCap: PenLineCap.Round);
                for (int i = 0; i < vis; i++)
                {
                    double a = (a0 + (vis > 1 ? amp * i / (vis - 1) : 0) + off) * Math.PI / 180;
                    ctx.DrawLine(pen, new Point(c.X + (r - 4) * Math.Cos(a), c.Y + (r - 4) * Math.Sin(a)), new Point(c.X + (R + 5) * Math.Cos(a), c.Y + (R + 5) * Math.Sin(a)));
                }
            };
            double sp0 = Math.Max(1.4, R / 26);
            for (int k = 0; k < nuclei; k++)
            {
                Point c = new Point(W / nuclei * (k + 0.5), (H - testo) / 2 + 2);
                ctx.DrawEllipse(new SolidColorBrush(corpo), null, c, R, R);
                ctx.DrawEllipse(Pal.B(Pal.Panel), null, c, r, r);
                double amp = 150, a0 = -165;
                switch (b.Tipo)
                {
                    case "bifilare":
                    case "ruthroff":
                        spire(c, b.Np, a0, amp, 0, ColoriFili[0], sp0); spire(c, b.Np, a0, amp, amp / Math.Max(2, b.Np) / 2.5, ColoriFili[1], sp0);
                        break;
                    case "guanella":
                        spire(c, b.Np, a0, amp, 0, ColoriFili[k == 0 ? 0 : 2], sp0); spire(c, b.Np, a0, amp, amp / Math.Max(2, b.Np) / 2.5, ColoriFili[k == 0 ? 1 : 3], sp0);
                        break;
                    case "trifilare":
                        for (int j = 0; j < 3; j++) spire(c, b.Np, a0, amp, j * amp / Math.Max(2, b.Np) / 3.2, ColoriFili[j], sp0);
                        break;
                    case "auto":
                        {
                            int tot = b.Prese[b.Prese.Length - 1];
                            spire(c, tot, a0, amp + 60, 0, ColoriFili[0], sp0);
                            int vis = (int)Math.Min(tot, Mini ? 14 : 28);
                            IPen pp = new Pen(new SolidColorBrush(Color.Parse("#E0A000")), sp0 * 1.6, lineCap: PenLineCap.Round);
                            for (int j = 0; j < b.Prese.Length - 1; j++)
                            {
                                double fr = (b.Prese[j] - 1) / Math.Max(1.0, tot - 1.0);
                                double a = (a0 + (amp + 60) * fr) * Math.PI / 180;
                                Point p1 = new Point(c.X + (R + 5) * Math.Cos(a), c.Y + (R + 5) * Math.Sin(a)), p2 = new Point(c.X + (R + 22) * Math.Cos(a), c.Y + (R + 22) * Math.Sin(a));
                                ctx.DrawLine(pp, p1, p2);
                                scrivi(b.Prese[j].ToString(CultureInfo.InvariantCulture), p2.X + 8 * Math.Cos(a), p2.Y + 10 * Math.Sin(a) - 7, tb, 11);
                            }
                            break;
                        }
                    case "efhw":
                        {
                            spire(c, b.Ns, a0, amp + 80, 0, ColoriFili[0], sp0);
                            double ampP = (amp + 80) * Math.Max(1, b.Np - 1) / Math.Max(1.0, Math.Min(b.Ns, Mini ? 14 : 28) - 1.0);
                            spire(c, b.Np, a0, ampP, (amp + 80) / Math.Max(2, Math.Min(b.Ns, Mini ? 14 : 28)) / 2.2, ColoriFili[1], sp0 * 1.3);
                            if (b.Cpf > 0 && !Mini)
                            {
                                Point q = new Point(c.X - R - 18, c.Y + R * 0.2);
                                IPen cp = Pal.P(Pal.Fore, 2.2);
                                ctx.DrawLine(cp, new Point(q.X - 8, q.Y), new Point(q.X + 8, q.Y));
                                ctx.DrawLine(cp, new Point(q.X - 8, q.Y + 6), new Point(q.X + 8, q.Y + 6));
                                scrivi(b.Cpf.ToString("0", CultureInfo.InvariantCulture) + " pF", q.X, q.Y + 10, tb, 11);
                            }
                            break;
                        }
                }
                if (!Mini)
                {
                    string sotto = b.Tipo == "efhw" ? b.Np + " : " + b.Ns + " spire" : (b.Tipo == "auto" ? string.Join(" / ", Array.ConvertAll(b.Prese, x => x.ToString(CultureInfo.InvariantCulture))) + " spire" :
                                   b.Np + " spire" + (b.Tipo == "trifilare" ? " trifilari" : (b.Tipo == "auto" ? "" : " bifilari")));
                    scrivi((nuclei == 2 ? "nucleo " + (k + 1) + ": " : "") + sotto, c.X, c.Y + R + 8, tb, 12.5);
                }
            }
            if (!Mini)
            {
                string fili = b.Tipo == "guanella" ? "rosso/nero e blu/grigio: le due coppie" : (b.Tipo == "trifilare" ? "rosso, nero, blu: i tre fili" : (b.Tipo == "auto" ? "arancio: le prese" : (b.Tipo == "efhw" ? "nero: primario sopra il secondario (rosso)" : "rosso e nero: la coppia")));
                scrivi(b.Nucleo + (b.Polvere ? " (polvere di ferro)" : " (ferrite)") + " · " + fili, W / 2, H - 16, mu, 11.5);
            }
        }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.Panel), null, new Rect(0, 0, W, H));
            if (Out == null) return;
            if (Out.Schema == "eh") { SchemaEh(ctx, W, H); return; }
            if (Out.Schema == "balun") { SchemaBalun(ctx, W, H, Out.Balun); return; }
            List<Seg> wires = new List<Seg>();
            int exTag = 0, exSeg = 0;
            bool ground = false;
            if (!string.IsNullOrEmpty(Nec) && !Out.HideWires)
            {
                foreach (string raw in Nec.Split('\n'))
                {
                    string l = raw.Trim();
                    if (l.Length < 2) continue;
                    string[] t = l.Substring(2).Replace(',', ' ').Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    Func<int, double> d = k => k < t.Length ? double.Parse(t[k], CultureInfo.InvariantCulture) : 0;
                    try
                    {
                        if (l.StartsWith("GW")) wires.Add(new Seg { Tag = (int)d(0), N = (int)d(1), X1 = d(2), Y1 = d(3), Z1 = d(4), X2 = d(5), Y2 = d(6), Z2 = d(7) });
                        else if (l.StartsWith("EX") && exTag == 0) { exTag = (int)d(1); exSeg = (int)d(2); }
                        else if (l.StartsWith("GN") && (int)d(0) != -1) ground = true;
                    }
                    catch { }
                }
            }
            if (Out.HideGround) ground = false;

            // piano della vista
            string plane = Out.Plane;
            if (string.IsNullOrEmpty(plane))
            {
                double ex = 0, ey = 0, ez = 0, mnx = 1e9, mxx = -1e9, mny = 1e9, mxy = -1e9, mnz = 1e9, mxz = -1e9;
                foreach (Seg s in wires)
                {
                    mnx = Math.Min(mnx, Math.Min(s.X1, s.X2)); mxx = Math.Max(mxx, Math.Max(s.X1, s.X2));
                    mny = Math.Min(mny, Math.Min(s.Y1, s.Y2)); mxy = Math.Max(mxy, Math.Max(s.Y1, s.Y2));
                    mnz = Math.Min(mnz, Math.Min(s.Z1, s.Z2)); mxz = Math.Max(mxz, Math.Max(s.Z1, s.Z2));
                }
                ex = mxx - mnx; ey = mxy - mny; ez = mxz - mnz;
                if (ez < 0.05 * Math.Max(ex, ey)) plane = "xy";
                else plane = ex >= ey ? "xz" : "yz";
            }
            Func<double, double, double, Point> uv = (x, y, z) => plane == "xy" ? new Point(x, y) : plane == "yz" ? new Point(y, z) : new Point(x, z);

            // estensione
            double u0 = 1e9, u1 = -1e9, v0 = 1e9, v1 = -1e9;
            Action<double, double> ext = (u, v) => { u0 = Math.Min(u0, u); u1 = Math.Max(u1, u); v0 = Math.Min(v0, v); v1 = Math.Max(v1, v); };
            foreach (Seg s in wires) { Point a = uv(s.X1, s.Y1, s.Z1), b = uv(s.X2, s.Y2, s.Z2); ext(a.X, a.Y); ext(b.X, b.Y); }
            foreach (CalcMark m in Out.Marks)
            {
                ext(m.X - m.R, m.Y - m.R); ext(m.X + m.R, m.Y + m.R);
                if (m.Kind == 2 || m.Kind == 7 || m.Kind == 8 || m.Kind == 3 || m.Kind == 5 || m.Kind == 6) ext(m.X2, m.Y2);
            }
            if (ground && plane != "xy") ext(u0, 0);
            if (u0 > u1) return;
            double du = Math.Max(u1 - u0, 1e-6), dv = Math.Max(v1 - v0, 1e-6);
            double margin = 46;
            double sc = Math.Min((W - 2 * margin) / du, (H - 2 * margin) / dv);
            if (dv < 1e-5) sc = (W - 2 * margin) / du;
            if (du < 1e-5) sc = (H - 2 * margin) / dv;
            double cx = W / 2 - (u0 + u1) / 2 * sc, cy = H / 2 + (v0 + v1) / 2 * sc;
            Func<double, double, Point> P = (u, v) => new Point(cx + u * sc, cy - v * sc);

            if (ground && plane != "xy")
            {
                double gy = P(0, 0).Y;
                ctx.DrawLine(Pal.P(Pal.Muted, 1.2), new Point(8, gy), new Point(W - 8, gy));
                for (double x = 12; x < W - 8; x += 12) ctx.DrawLine(Pal.P(Pal.A(120, Pal.Muted), 1), new Point(x, gy), new Point(x - 7, gy + 7));
            }

            IPen wp = Pal.P(Pal.Accent, 4);
            foreach (Seg s in wires)
            {
                Point a = uv(s.X1, s.Y1, s.Z1), b = uv(s.X2, s.Y2, s.Z2);
                ctx.DrawLine(wp, P(a.X, a.Y), P(b.X, b.Y));
            }
            // punto di alimentazione (scheda EX)
            foreach (Seg s in wires)
            {
                if (s.Tag != exTag || exTag == 0 || s.N < 1) continue;
                double t = (exSeg - 0.5) / s.N;
                Point a = uv(s.X1 + (s.X2 - s.X1) * t, s.Y1 + (s.Y2 - s.Y1) * t, s.Z1 + (s.Z2 - s.Z1) * t);
                Point q = P(a.X, a.Y);
                ctx.DrawRectangle(Pal.B(Pal.Orange), Pal.P(Colors.White, 1.5), new Rect(q.X - 6, q.Y - 6, 12, 12));
                break;
            }

            foreach (CalcMark m in Out.Marks)
            {
                Color c = Colore(m.Col);
                Point p = P(m.X, m.Y), p2 = P(m.X2, m.Y2);
                IPen pen = m.Dash ? Pal.P(c, 1.6, Pal.Dash) : Pal.P(c, 2.2);
                switch (m.Kind)
                {
                    case 0:
                        if (!string.IsNullOrEmpty(m.Text)) Draw.Text(ctx, Lang.T(m.Text), p.X, p.Y - 9, 12.5, c, FontWeight.SemiBold);
                        break;
                    case 1:
                        ctx.DrawEllipse(null, m.Dash ? Pal.P(c, 1.4, Pal.Dash) : Pal.P(c, 3), p, m.R * sc, m.R * sc);   // 1.3.1: cerchio tratteggiato (aperture dello stack)
                        if (!string.IsNullOrEmpty(m.Text)) Draw.Text(ctx, Lang.T(m.Text), p.X + m.R * sc + 6, p.Y - 9, 12.5, c, FontWeight.SemiBold);
                        break;
                    case 2:
                        ctx.DrawLine(pen, p, p2);
                        if (!string.IsNullOrEmpty(m.Text)) Draw.Text(ctx, Lang.T(m.Text), p2.X + 6, p2.Y - 9, 12.5, c);
                        break;
                    case 3:
                    case 5:
                    case 6:
                        {
                            // componente tra p e p2: condensatore (due piastre), resistenza (rettangolo), bobina (archi)
                            double dx = p2.X - p.X, dy = p2.Y - p.Y, L = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                            double ux = dx / L, uy = dy / L, nx = -uy, ny = ux;
                            Point mid = new Point((p.X + p2.X) / 2, (p.Y + p2.Y) / 2);
                            double half = Math.Min(14, L / 2);
                            Point a = new Point(mid.X - ux * half, mid.Y - uy * half), b = new Point(mid.X + ux * half, mid.Y + uy * half);
                            ctx.DrawLine(Pal.P(c, 2), p, a);
                            ctx.DrawLine(Pal.P(c, 2), b, p2);
                            if (m.Kind == 3)
                            {
                                Point a2 = new Point(mid.X - ux * 4, mid.Y - uy * 4), b2 = new Point(mid.X + ux * 4, mid.Y + uy * 4);
                                ctx.DrawLine(Pal.P(c, 2), a, a2); ctx.DrawLine(Pal.P(c, 2), b2, b);
                                ctx.DrawLine(Pal.P(c, 3), new Point(a2.X + nx * 12, a2.Y + ny * 12), new Point(a2.X - nx * 12, a2.Y - ny * 12));
                                ctx.DrawLine(Pal.P(c, 3), new Point(b2.X + nx * 12, b2.Y + ny * 12), new Point(b2.X - nx * 12, b2.Y - ny * 12));
                            }
                            else if (m.Kind == 5)
                            {
                                StreamGeometry g = new StreamGeometry();
                                using (StreamGeometryContext gc = g.Open())
                                {
                                    gc.BeginFigure(new Point(a.X + nx * 6, a.Y + ny * 6), true);
                                    gc.LineTo(new Point(b.X + nx * 6, b.Y + ny * 6));
                                    gc.LineTo(new Point(b.X - nx * 6, b.Y - ny * 6));
                                    gc.LineTo(new Point(a.X - nx * 6, a.Y - ny * 6));
                                    gc.EndFigure(true);
                                }
                                ctx.DrawGeometry(Pal.B(Pal.Panel), Pal.P(c, 2), g);
                            }
                            else
                            {
                                Point prev = a;
                                for (int k = 1; k <= 32; k++)
                                {
                                    double t = k / 32.0, ang = t * 4 * Math.PI;
                                    double off = Math.Abs(Math.Sin(ang / 2)) * 9;
                                    Point q = new Point(a.X + (b.X - a.X) * t + nx * off, a.Y + (b.Y - a.Y) * t + ny * off);
                                    ctx.DrawLine(Pal.P(c, 2), prev, q);
                                    prev = q;
                                }
                            }
                            if (!string.IsNullOrEmpty(m.Text)) Draw.Text(ctx, Lang.T(m.Text), mid.X + nx * 16 + 4, mid.Y + ny * 16 - 9, 12.5, c, FontWeight.SemiBold);
                        }
                        break;
                    case 4:
                        ctx.DrawRectangle(Pal.B(Pal.Orange), Pal.P(Colors.White, 1.5), new Rect(p.X - 6, p.Y - 6, 12, 12));
                        if (!string.IsNullOrEmpty(m.Text)) Draw.Text(ctx, Lang.T(m.Text), p.X + 10, p.Y - 9, 12.5, c);
                        break;
                    case 7:
                        {
                            IPen qp = Pal.P(Pal.Muted, 1);
                            ctx.DrawLine(qp, p, p2);
                            double dx = p2.X - p.X, dy = p2.Y - p.Y, L = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                            double nx = -dy / L * 5, ny = dx / L * 5;
                            ctx.DrawLine(qp, new Point(p.X + nx, p.Y + ny), new Point(p.X - nx, p.Y - ny));
                            ctx.DrawLine(qp, new Point(p2.X + nx, p2.Y + ny), new Point(p2.X - nx, p2.Y - ny));
                            if (!string.IsNullOrEmpty(m.Text)) Draw.TextC(ctx, Lang.T(m.Text), (p.X + p2.X) / 2 - dy / L * 12, (p.Y + p2.Y) / 2 + dx / L * 12, 12, Pal.Muted);
                        }
                        break;
                    case 8:
                        ctx.DrawRectangle(null, pen, new Rect(p, p2));
                        if (!string.IsNullOrEmpty(m.Text)) Draw.TextC(ctx, Lang.T(m.Text), (p.X + p2.X) / 2, (p.Y + p2.Y) / 2, 12.5, c, FontWeight.SemiBold);
                        break;
                }
            }
        }
    }
}
