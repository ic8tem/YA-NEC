// YA-NEC - carta di Smith dello sweep, normalizzata all'impedenza di riferimento Z0
using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace YaNec
{
    public class SmithChart : Control
    {
        public double Z0 = 50;
        public double[] F = new double[0];
        public Complex[] Z = new Complex[0];
        public double MarkF = double.NaN;
        public List<Tuple<Complex[], Color>> Altre = new List<Tuple<Complex[], Color>>();   // 1.2.0: curve aggiunte (misure del VNA)
        public List<Tuple<Complex[], Color>> Fissate = new List<Tuple<Complex[], Color>>(); // 1.6.0: risultati fissati (tratteggiati)
        public string Title = "Carta di Smith";
        int hover = -1;

        public SmithChart() { ClipToBounds = true; }

        Point Map(Complex g, Point c, double R) { return new Point(c.X + g.Real * R, c.Y - g.Imaginary * R); }

        void GetLayout(out Point c, out double R)
        {
            double W = Bounds.Width, H = Bounds.Height;
            R = Math.Max(20, Math.Min(W - 260, H - 70) / 2);
            c = new Point(30 + R, 40 + R);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            Point c; double R;
            GetLayout(out c, out R);
            Point p = e.GetPosition(this);
            int best = -1; double bd = 15 * 15;
            for (int i = 0; i < Z.Length; i++)
            {
                Point q = Map((Z[i] - Z0) / (Z[i] + Z0), c, R);
                double d = (q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y);
                if (d < bd) { bd = d; best = i; }
            }
            if (best != hover) { hover = best; InvalidateVisual(); }
        }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.ChartBg), null, new Rect(0, 0, W, H));
            Draw.Text(ctx, Lang.T(Title) + " (Z0 = " + Draw.N(Z0, 1) + " ohm)", 10, 6, 14, Pal.Fore, FontWeight.SemiBold);
            Point c; double R;
            GetLayout(out c, out R);
            IPen grid = Pal.P(Pal.ChartGrid, 1);
            IPen axis = Pal.P(Pal.ChartText, 1);
            EllipseGeometry clip = new EllipseGeometry(new Rect(c.X - R, c.Y - R, 2 * R, 2 * R));
            using (ctx.PushGeometryClip(clip))
            {
                // 1.7.0 (bozzetto «mining riviste» B, approvato da IC8TEM il 25/11/2026): velatura arancio leggera dove R < Z0
                // (fuori dal cerchio r = 1), per vedere da che lato sta il ROS
                Geometry fuori = new CombinedGeometry(GeometryCombineMode.Exclude, new EllipseGeometry(new Rect(c.X - R, c.Y - R, 2 * R, 2 * R)),
                    new EllipseGeometry(new Rect(c.X, c.Y - R / 2, R, R)));
                ctx.DrawGeometry(new SolidColorBrush(Color.FromArgb((byte)(Pal.Dark ? 34 : 26), Pal.Orange.R, Pal.Orange.G, Pal.Orange.B)), null, fuori);
                // cerchi di resistenza costante: centro (r/(1+r), 0), raggio 1/(1+r)
                foreach (double r in new[] { 0.2, 0.5, 1.0, 2.0, 5.0 })
                {
                    double cc = r / (1 + r), rr = 1 / (1 + r);
                    ctx.DrawEllipse(null, r == 1.0 ? Pal.P(Pal.ChartText, 1) : grid, new Point(c.X + cc * R, c.Y), rr * R, rr * R);
                }
                // archi di reattanza costante: centro (1, 1/x), raggio 1/|x|
                foreach (double x in new[] { 0.2, 0.5, 1.0, 2.0, 5.0, -0.2, -0.5, -1.0, -2.0, -5.0 })
                {
                    double rr = 1 / Math.Abs(x);
                    ctx.DrawEllipse(null, grid, new Point(c.X + R, c.Y - (1 / x) * R), rr * R, rr * R);
                }
                // cerchio di ROS 2
                double g2 = 1.0 / 3.0;
                ctx.DrawEllipse(null, Pal.P(Pal.A(170, Pal.Green), 1.2, Pal.Dash), c, g2 * R, g2 * R);
            }
            ctx.DrawEllipse(null, axis, c, R, R);
            ctx.DrawLine(grid, new Point(c.X - R, c.Y), new Point(c.X + R, c.Y));
            foreach (double r in new[] { 0.2, 0.5, 1.0, 2.0, 5.0 })
            {
                double xr = (r - 1) / (r + 1);
                Draw.TextC(ctx, Draw.N(r * Z0, 0), c.X + xr * R, c.Y + 9, 9.5, Pal.ChartText);
            }
            Draw.Text(ctx, "ROS 2", c.X + R / 3 + 3, c.Y - R / 3 - 4, 9.5, Pal.Green);
            Draw.Text(ctx, "R < Z0", c.X - R * 0.86, c.Y - R * 0.22, 9.5, Pal.Orange);

            foreach (Tuple<Complex[], Color> a in Fissate)
            {
                if (a.Item1 == null) continue;
                IPen pf = Pal.P(a.Item2, 1.8, Pal.Dash);
                using (ctx.PushGeometryClip(clip))
                    for (int i = 1; i < a.Item1.Length; i++)
                        ctx.DrawLine(pf, Map((a.Item1[i - 1] - Z0) / (a.Item1[i - 1] + Z0), c, R), Map((a.Item1[i] - Z0) / (a.Item1[i] + Z0), c, R));
            }
            foreach (Tuple<Complex[], Color> a in Altre)
            {
                if (a.Item1 == null) continue;
                IPen pa = Pal.P(a.Item2, 2);
                using (ctx.PushGeometryClip(clip))
                    for (int i = 1; i < a.Item1.Length; i++)
                        ctx.DrawLine(pa, Map((a.Item1[i - 1] - Z0) / (a.Item1[i - 1] + Z0), c, R), Map((a.Item1[i] - Z0) / (a.Item1[i] + Z0), c, R));
            }
            if (Z != null && Z.Length > 0)
            {
                IPen pen = Pal.P(Pal.Accent, 2.2);
                for (int i = 1; i < Z.Length; i++)
                {
                    Complex g0 = (Z[i - 1] - Z0) / (Z[i - 1] + Z0), g1 = (Z[i] - Z0) / (Z[i] + Z0);
                    ctx.DrawLine(pen, Map(g0, c, R), Map(g1, c, R));
                }
                // inizio, fine e frequenza di progetto
                ctx.DrawEllipse(Pal.B(Pal.Green), null, Map((Z[0] - Z0) / (Z[0] + Z0), c, R), 4, 4);
                ctx.DrawEllipse(Pal.B(Pal.Orange), null, Map((Z[Z.Length - 1] - Z0) / (Z[Z.Length - 1] + Z0), c, R), 4, 4);
                int im = -1;
                if (!double.IsNaN(MarkF))
                {
                    double bd = double.MaxValue;
                    for (int i = 0; i < F.Length; i++) if (Math.Abs(F[i] - MarkF) < bd) { bd = Math.Abs(F[i] - MarkF); im = i; }
                    if (im >= 0) ctx.DrawEllipse(Pal.B(Pal.Red), Pal.P(Pal.Fore, 1), Map((Z[im] - Z0) / (Z[im] + Z0), c, R), 5, 5);
                }
                // riquadro dei valori
                double bx = c.X + R + 24, by = 44;
                List<string> lines = new List<string>();
                lines.Add(Lang.F("verde: {0} MHz", Draw.N(F[0], 3)));   // 1.5.2: tradotti
                lines.Add(Lang.F("arancio: {0} MHz", Draw.N(F[F.Length - 1], 3)));
                int show = hover >= 0 ? hover : im;
                if (show >= 0 && show < Z.Length)
                {
                    Complex z = Z[show];
                    lines.Add("");
                    lines.Add(Lang.F(hover >= 0 ? "punto: {0} MHz" : "progetto: {0} MHz", Draw.N(F[show], 3)));
                    lines.Add("Z = " + Draw.N(z.Real, 2) + (z.Imaginary >= 0 ? " +j" : " -j") + Draw.N(Math.Abs(z.Imaginary), 2) + " ohm");
                    lines.Add(Lang.T("ROS") + " = " + Draw.N(YaNec.Engine.Analysis.Swr(z, Z0), 2));
                    Complex g = (z - Z0) / (z + Z0);
                    lines.Add("|rho| = " + Draw.N(Complex.Abs(g), 3) + "  RL = " + Draw.N(-20 * Math.Log10(Math.Max(1e-9, Complex.Abs(g))), 1) + " dB");
                    if (hover >= 0) ctx.DrawEllipse(null, Pal.P(Pal.Fore, 2), Map(g, c, R), 7, 7);
                }
                for (int i = 0; i < lines.Count; i++)
                    Draw.Text(ctx, lines[i], bx, by + i * 18, 12, i < 2 ? (i == 0 ? Pal.Green : Pal.Orange) : Pal.ChartText);
            }
        }
    }
}
