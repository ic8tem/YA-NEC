// YA-NEC - diagramma polare (azimut o elevazione) con scala logaritmica ARRL o lineare in dB
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace YaNec
{
    public class PolarTrace
    {
        public double[] Db;          // un valore per grado (azimut: 0..359; elevazione: 0..360 avvolto)
        public Color Color;
        public double Width = 2;
        public bool Dashed;
        public string Name = "";
    }

    public class PolarPlot : Control
    {
        public string Title = "";
        public bool Elevation;          // false = azimut (0 gradi a destra, antiorario come phi del NEC)
        public bool GroundHalf;         // elevazione con terreno: solo la meta' superiore
        public bool ArrlScale = true;
        public double LinRange = 40;           // scala lineare: dB al centro (40, oppure 30 nei calcolatori)
        public double RefDb;            // valore che corrisponde al cerchio esterno
        public List<PolarTrace> Traces = new List<PolarTrace>();
        public string[] Notes = new string[0];
        public double MarkAngle = double.NaN;   // direzione del massimo
        public double Rotate;                   // gradi aggiunti a tutti gli angoli (90 = avanti in alto, per i calcolatori)
        public bool ShowHalfPower = true;
        public double TextScale = 1;            // 0.5.3: grandezza di tutte le scritte del diagramma (cursore "Caratteri" dei Diagrammi, 100-200 %)       // apertura a -3 dB: due raggi tratteggiati, arco e valore (il "triangolo")

        public PolarPlot() { ClipToBounds = true; }

        double Radius(double db, double R)
        {
            double d = RefDb - db;      // attenuazione rispetto al riferimento (>= 0)
            if (d < 0) d = 0;
            if (ArrlScale) return R * Math.Pow(0.89, d / 2.0);     // ARRL: 0,89 ogni 2 dB
            return R * Math.Max(0, 1 - d / LinRange);               // lineare: -LinRange dB al centro
        }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.ChartBg), null, new Rect(0, 0, W, H));
            double ks = Math.Max(0.5, TextScale);
            double top = 12 + 18 * ks, bottom = 14 + 16 * ks + 16 * ks * Math.Max(1, Notes.Length);
            double cx = W / 2, cy;
            double R;
            if (Elevation && GroundHalf)
            {
                R = Math.Min(W / 2 - 18 - 14 * ks, H - top - bottom - 20);
                cy = top + 10 + R;
            }
            else
            {
                R = Math.Min(W / 2 - 18 - 14 * ks, (H - top - bottom) / 2 - 6 - 8 * ks);
                cy = top + 10 + R + 4;
            }
            if (R < 30) return;
            Draw.Text(ctx, Title, 10, 6, 14 * ks, Pal.Fore, FontWeight.SemiBold);
            Point c = new Point(cx, cy);
            IPen grid = Pal.P(Pal.ChartGrid, 1);
            IPen grid2 = Pal.P(Pal.ChartGrid, 1, Pal.Dot);

            // cerchi
            double[] rings = ArrlScale ? new double[] { 0, 3, 6, 10, 20, 30 } : (LinRange <= 30 ? new double[] { 0, 10, 20 } : new double[] { 0, 10, 20, 30 });
            foreach (double d in rings)
            {
                double r = Radius(RefDb - d, R);
                if (Elevation && GroundHalf) DrawHalfCircle(ctx, c, r, d == 0 ? grid : grid2);
                else ctx.DrawEllipse(null, d == 0 ? grid : grid2, c, r, r);
                Draw.Text(ctx, d == 0 ? "0" : "-" + d.ToString("0"), cx + r * Math.Cos(1.309) + 2, cy - r * Math.Sin(1.309) - 13 * ks, 10 * ks, Pal.ChartText);   // lungo il raggio a 75 gradi, lontano dalle scritte degli assi
            }
            // raggi ogni 30 gradi
            for (int a = 0; a < 360; a += 30)
            {
                if (Elevation && GroundHalf && a > 180) continue;
                double t = (a + Rotate) * Math.PI / 180;
                Point p = new Point(cx + R * Math.Cos(t), cy - R * Math.Sin(t));
                ctx.DrawLine(grid2, c, p);
                string lab = Elevation ? (a <= 90 ? a.ToString() : (a <= 180 ? (180 - a).ToString() : (a - 360).ToString())) : a.ToString();
                if (Elevation && a > 180) lab = "-" + (a <= 270 ? (a - 180) : (360 - a)).ToString();
                Draw.TextC(ctx, lab + "°", cx + (R + 6 + 10 * ks) * Math.Cos(t), cy - (R + 4 + 8 * ks) * Math.Sin(t), 10 * ks, Pal.ChartText);
            }
            if (Elevation && GroundHalf) ctx.DrawLine(Pal.P(Pal.Green, 1.5), new Point(cx - R - 6, cy), new Point(cx + R + 6, cy));

            // tracce
            foreach (PolarTrace tr in Traces)
            {
                if (tr.Db == null || tr.Db.Length < 2) continue;
                IPen pen = tr.Dashed ? Pal.P(tr.Color, tr.Width, Pal.Dash) : Pal.P(tr.Color, tr.Width);
                int nn = tr.Db.Length;
                StreamGeometry sg = new StreamGeometry();
                using (StreamGeometryContext g = sg.Open())
                {
                    bool open = false;
                    int count = Elevation ? (GroundHalf ? 180 : 360) : 360;
                    for (int k = 0; k <= count; k++)
                    {
                        int idx = k % nn;
                        double v = tr.Db[idx];
                        // come nel bozzetto approvato: i nulli (-999,99 dB) portano la traccia al centro, senza interruzioni
                        double r = v < -900 ? 0 : Radius(v, R);
                        double t = ((Elevation ? (k <= 180 ? k : k - 360) : k) + Rotate) * Math.PI / 180;
                        Point p = new Point(cx + r * Math.Cos(t), cy - r * Math.Sin(t));
                        if (!open) { g.BeginFigure(p, false); open = true; }
                        else g.LineTo(p);
                    }
                    if (open) g.EndFigure(false);
                }
                ctx.DrawGeometry(null, pen, sg);
            }
            // apertura a -3 dB sulla prima traccia
            if (ShowHalfPower && Traces.Count > 0 && Traces[0].Db != null && Traces[0].Db.Length >= 36)
                DrawHalfPower(ctx, c, R, Traces[0]);
            // direzione del massimo
            if (!double.IsNaN(MarkAngle))
            {
                double t = (MarkAngle + Rotate) * Math.PI / 180;
                ctx.DrawLine(Pal.P(Pal.A(180, Pal.Red), 1, Pal.Dash), c, new Point(cx + R * Math.Cos(t), cy - R * Math.Sin(t)));
            }
            // legenda e note
            double ly = H - bottom + 4;
            foreach (string s in Notes)
            {
                Draw.Text(ctx, s, 12, ly, 12 * ks, Pal.ChartText);
                ly += 16 * ks;
            }
            double lx = W - 12;
            for (int i = Traces.Count - 1; i >= 0; i--)
            {
                PolarTrace tr = Traces[i];
                if (string.IsNullOrEmpty(tr.Name)) continue;
                double ls = Draw.LegendScale;
                FormattedText ft = Draw.Ft(tr.Name, 11.5 * ls, Pal.ChartText);
                lx -= ft.Width;
                ctx.DrawText(ft, new Point(lx, 8));
                double legY = 8 + ft.Height / 2;
                ctx.DrawLine(tr.Dashed ? Pal.P(tr.Color, 2, Pal.Dash) : Pal.P(tr.Color, 2), new Point(lx - 22 * ls, legY), new Point(lx - 4, legY));
                lx -= 34 * ls;
            }
            Draw.TextR(ctx, ArrlScale ? "scala log ARRL, dB rispetto al massimo" : "scala lineare in dB (" + LinRange.ToString("0") + " dB)", W - 12, H - 8 - 12 * ks, 10.5 * ks, Pal.Muted);
        }

        void DrawHalfPower(DrawingContext ctx, Point c, double R, PolarTrace tr)
        {
            double[] d = tr.Db;
            int nn = d.Length;
            int count = Elevation ? (GroundHalf ? 181 : nn) : nn;
            count = Math.Min(count, nn);
            int m = 0;
            for (int k = 1; k < count; k++) if (d[k] > d[m]) m = k;
            double lim = d[m] - 3;
            bool wrap = !(Elevation && GroundHalf);
            // cerca il punto a -3 dB andando nei due versi, con interpolazione lineare
            Func<int, double> find = dir =>
            {
                int prev = m;
                for (int s = 1; s < count; s++)
                {
                    int k = m + dir * s;
                    if (wrap) k = ((k % nn) + nn) % nn;
                    else if (k < 0 || k >= count) return double.NaN;
                    if (d[k] < lim)
                    {
                        double f = (d[prev] - lim) / Math.Max(1e-9, d[prev] - d[k]);
                        return m + dir * (s - 1 + f);
                    }
                    prev = k;
                }
                return double.NaN;
            };
            double a = find(-1), b = find(+1);
            if (double.IsNaN(a) || double.IsNaN(b)) return;
            double width = b - a;
            if (width <= 0 || width >= 359) return;
            double r = Radius(lim, R);
            Color col = Pal.A(220, tr.Color);
            IPen pen = Pal.P(col, 1.2, Pal.Dash);
            // gli indici sono gradi del grafico (anche oltre 360 o negativi: seno e coseno non cambiano)
            double ta = (a + Rotate) * Math.PI / 180, tb = (b + Rotate) * Math.PI / 180;
            Point pa = new Point(c.X + r * Math.Cos(ta), c.Y - r * Math.Sin(ta));
            Point pb = new Point(c.X + r * Math.Cos(tb), c.Y - r * Math.Sin(tb));
            ctx.DrawLine(pen, c, pa);
            ctx.DrawLine(pen, c, pb);
            // arco tra i due raggi, a meta' raggio, con il valore
            double ra = Math.Max(18, r * 0.45);
            StreamGeometry sg = new StreamGeometry();
            using (StreamGeometryContext g = sg.Open())
            {
                int steps = Math.Max(4, (int)(width / 3));
                for (int i = 0; i <= steps; i++)
                {
                    double t = ta + (tb - ta) * i / steps;
                    Point p = new Point(c.X + ra * Math.Cos(t), c.Y - ra * Math.Sin(t));
                    if (i == 0) g.BeginFigure(p, false); else g.LineTo(p);
                }
                g.EndFigure(false);
            }
            ctx.DrawGeometry(null, Pal.P(col, 1.2), sg);
            double tm = (ta + tb) / 2;
            double rl = ra + 14;
            Draw.TextC(ctx, "-3 dB: " + Draw.N(width, 1) + "°", c.X + rl * Math.Cos(tm), c.Y - rl * Math.Sin(tm), 11 * Math.Max(0.5, TextScale), col, FontWeight.SemiBold);
        }

        static void DrawHalfCircle(DrawingContext ctx, Point c, double r, IPen pen)
        {
            StreamGeometry sg = new StreamGeometry();
            using (StreamGeometryContext g = sg.Open())
            {
                g.BeginFigure(new Point(c.X + r, c.Y), false);
                for (int k = 1; k <= 90; k++)
                {
                    double t = k * Math.PI / 90;
                    g.LineTo(new Point(c.X + r * Math.Cos(t), c.Y - r * Math.Sin(t)));
                }
                g.EndFigure(false);
            }
            ctx.DrawGeometry(null, pen, sg);
        }
    }
}
