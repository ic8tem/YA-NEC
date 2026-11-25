// YA-NEC - grafico a linee in funzione della frequenza, con asse destro facoltativo e lettura dei valori al passaggio del mouse
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace YaNec
{
    public class LineSeries
    {
        public string Name = "";
        public double[] Y;
        public Color Color;
        public bool Right;
        public double Width = 2;
        public bool Dashed;
        public int Decimals = 2;
    }

    public class LineChart : Control
    {
        public string Title = "", XLabel = "MHz", YLabel = "", Y2Label = "";
        public double[] X = new double[0];
        public List<LineSeries> Series = new List<LineSeries>();
        public double YMinFixed = double.NaN, YMaxFixed = double.NaN;
        public double Y2MinFixed = double.NaN, Y2MaxFixed = double.NaN;
        public double MarkX = double.NaN;
        public double MarkX2 = double.NaN;    // 1.7.0: seconda linea verticale tratteggiata (es. cella dello stack uguale all'area efficace)
        public string MarkX2Label;
        public string MarkXLabel;            // 1.2.0: scritta accanto alla linea verticale (es. banda da bloccare)
        public Color? MarkXColor;
        public double[] HLines = new double[0];
        public string EmptyText = "Esegui uno sweep in frequenza per vedere il grafico";   // linee orizzontali di riferimento (asse sinistro)
        double hoverX = double.NaN;

        public LineChart() { ClipToBounds = true; }
        // 1.5.2 (bozzetto «Linguetta Frequenza: bande radioamatoriali ombreggiate», approvato da IC8TEM il 19/11/2026 08:44, con la scelta
        // del colore): fascia dietro griglia e curve dove cadono le bande (Bands.All), nome della banda in alto
        public bool MostraBande;
        public static int ColoreBande;   // 0 verde salvia, 1 ambra, 2 azzurro ghiaccio, 3 nessuna (Impostazioni)
        static readonly string[][] TintaBande = { new[] { "#2F9E6B", "#5FD39B" }, new[] { "#E0A100", "#FFC94D" }, new[] { "#3AA0D8", "#7CC6F0" } };
        public static Color? ColoreBanda()
        {
            if (ColoreBande < 0 || ColoreBande > 2) return null;
            return Color.Parse(TintaBande[ColoreBande][Pal.Dark ? 1 : 0]);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            hoverX = e.GetPosition(this).X;
            InvalidateVisual();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            hoverX = double.NaN;
            InvalidateVisual();
        }

        static void Range(IEnumerable<LineSeries> ss, bool right, out double lo, out double hi)
        {
            lo = double.MaxValue; hi = double.MinValue;
            foreach (LineSeries s in ss)
            {
                if (s.Right != right || s.Y == null) continue;
                foreach (double v in s.Y)
                {
                    if (double.IsNaN(v) || double.IsInfinity(v)) continue;
                    lo = Math.Min(lo, v); hi = Math.Max(hi, v);
                }
            }
            if (lo > hi) { lo = 0; hi = 1; }
            if (hi - lo < 1e-9) { hi += 0.5; lo -= 0.5; }
        }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.ChartBg), null, new Rect(0, 0, W, H));
            Draw.Text(ctx, Title, 10, 6, 14, Pal.Fore, FontWeight.SemiBold);
            if (X == null || X.Length < 2)
            {
                Draw.TextC(ctx, EmptyText, W / 2, H / 2, 13, Pal.Muted);
                return;
            }
            bool any2 = Series.Exists(sr => sr.Right);
            double ml = 62, mr = any2 ? 62 : 20, mt = 34, mb = 40;
            Rect pr = new Rect(ml, mt, Math.Max(10, W - ml - mr), Math.Max(10, H - mt - mb));
            double xmin = X[0], xmax = X[X.Length - 1];
            if (xmax <= xmin) xmax = xmin + 1;
            double ylo, yhi, y2lo, y2hi;
            Range(Series, false, out ylo, out yhi);
            Range(Series, true, out y2lo, out y2hi);
            if (!double.IsNaN(YMinFixed)) ylo = YMinFixed;
            if (!double.IsNaN(YMaxFixed)) yhi = YMaxFixed;
            if (!double.IsNaN(Y2MinFixed)) y2lo = Y2MinFixed;
            if (!double.IsNaN(Y2MaxFixed)) y2hi = Y2MaxFixed;
            double ys = Draw.NiceStep(yhi - ylo, 6);
            ylo = Math.Floor(ylo / ys) * ys; yhi = Math.Ceiling(yhi / ys) * ys;
            if (yhi <= ylo) yhi = ylo + ys;
            double y2s = Draw.NiceStep(y2hi - y2lo, 6);
            y2lo = Math.Floor(y2lo / y2s) * y2s; y2hi = Math.Ceiling(y2hi / y2s) * y2s;
            if (y2hi <= y2lo) y2hi = y2lo + y2s;

            Func<double, double> PX = q => pr.Left + (q - xmin) / (xmax - xmin) * pr.Width;
            Func<double, double> PY = q => pr.Bottom - (q - ylo) / (yhi - ylo) * pr.Height;
            Func<double, double> PY2 = q => pr.Bottom - (q - y2lo) / (y2hi - y2lo) * pr.Height;
            IPen grid = Pal.P(Pal.ChartGrid, 1);
            Color? cb = MostraBande ? ColoreBanda() : null;
            if (cb.HasValue && XLabel == "MHz")
            {
                byte af = (byte)(Pal.Dark ? 36 : 31), ab = (byte)(Pal.Dark ? 102 : 90);
                foreach (YaNec.Engine.Band b in YaNec.Engine.Bands.All)
                {
                    if (b.High < xmin || b.Low > xmax) continue;
                    double xa = PX(Math.Max(b.Low, xmin)), xb = Math.Max(PX(Math.Min(b.High, xmax)), xa + 3);
                    ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(af, cb.Value.R, cb.Value.G, cb.Value.B)), null, new Rect(xa, pr.Top, xb - xa, pr.Height));
                    IPen pb = new Pen(new SolidColorBrush(Color.FromArgb(ab, cb.Value.R, cb.Value.G, cb.Value.B)), 1);
                    if (b.Low >= xmin) ctx.DrawLine(pb, new Point(xa, pr.Top), new Point(xa, pr.Bottom));
                    if (b.High <= xmax) ctx.DrawLine(pb, new Point(xb, pr.Top), new Point(xb, pr.Bottom));
                    if (xb - xa >= 14 || (xmax - xmin) < 3 * (b.High - b.Low) + 1)
                        Draw.TextC(ctx, b.Name, (xa + xb) / 2, pr.Top + 2, 9.5, cb.Value, FontWeight.SemiBold);
                }
            }

            for (double v = ylo; v <= yhi + ys * 0.01; v += ys)
            {
                double y = PY(v);
                ctx.DrawLine(grid, new Point(pr.Left, y), new Point(pr.Right, y));
                Draw.TextR(ctx, Draw.N(v, ys < 1 ? (ys < 0.1 ? 2 : 1) : 0), pr.Left - 6, y - 8, 10.5, Pal.ChartText);
            }
            if (any2)
                for (double v = y2lo; v <= y2hi + y2s * 0.01; v += y2s)
                    Draw.Text(ctx, Draw.N(v, y2s < 1 ? (y2s < 0.1 ? 2 : 1) : 0), pr.Right + 6, PY2(v) - 8, 10.5, Pal.ChartText);
            double xs = Draw.NiceStep(xmax - xmin, 8);
            for (double v = Math.Ceiling(xmin / xs) * xs; v <= xmax + xs * 0.001; v += xs)
            {
                double x = PX(v);
                ctx.DrawLine(grid, new Point(x, pr.Top), new Point(x, pr.Bottom));
                Draw.TextC(ctx, Draw.N(v, xs < 0.1 ? 2 : (xs < 1 ? 1 : 0)), x, pr.Bottom + 10, 10.5, Pal.ChartText);
            }
            Draw.TextC(ctx, XLabel, pr.Left + pr.Width / 2, pr.Bottom + 27, 11, Pal.ChartText);
            Draw.Text(ctx, YLabel, 6, mt - 18, 11, Pal.ChartText);
            if (any2) Draw.TextR(ctx, Y2Label, W - 6, mt - 18, 11, Pal.ChartText);
            ctx.DrawRectangle(null, Pal.P(Pal.ChartText, 1), pr);

            foreach (double h in HLines)
            {
                if (h < ylo || h > yhi) continue;
                ctx.DrawLine(Pal.P(Pal.A(160, Pal.Red), 1, Pal.Dash), new Point(pr.Left, PY(h)), new Point(pr.Right, PY(h)));
            }
            if (!double.IsNaN(MarkX2) && MarkX2 >= xmin && MarkX2 <= xmax)
            {
                ctx.DrawLine(Pal.P(Pal.A(200, Pal.Orange), 1.2, Pal.Dash), new Point(PX(MarkX2), pr.Top), new Point(PX(MarkX2), pr.Bottom));
                if (!string.IsNullOrEmpty(MarkX2Label)) Draw.Text(ctx, MarkX2Label, PX(MarkX2) + 6, pr.Bottom - 22, 11, Pal.Orange);
            }
            if (!double.IsNaN(MarkX) && MarkX >= xmin && MarkX <= xmax)
            {
                ctx.DrawLine(Pal.P(MarkXColor ?? Pal.A(150, Pal.Muted), 1, Pal.Dash), new Point(PX(MarkX), pr.Top), new Point(PX(MarkX), pr.Bottom));
                if (!string.IsNullOrEmpty(MarkXLabel)) Draw.Text(ctx, MarkXLabel, PX(MarkX) + 6, pr.Top + 28, 11, MarkXColor ?? Pal.Muted);
            }

            using (ctx.PushClip(pr))
            {
                foreach (LineSeries s in Series)
                {
                    if (s.Y == null) continue;
                    IPen pen = s.Dashed ? Pal.P(s.Color, s.Width, Pal.Dash) : Pal.P(s.Color, s.Width);
                    Point? prev = null;
                    for (int i = 0; i < X.Length && i < s.Y.Length; i++)
                    {
                        double v = s.Y[i];
                        if (double.IsNaN(v) || double.IsInfinity(v)) { prev = null; continue; }
                        Point p = new Point(PX(X[i]), s.Right ? PY2(v) : PY(v));
                        if (prev.HasValue) ctx.DrawLine(pen, prev.Value, p);
                        prev = p;
                    }
                }
            }

            // legenda
            double lx = pr.Right - 4;
            for (int i = Series.Count - 1; i >= 0; i--)
            {
                LineSeries s = Series[i];
                double ls = Draw.LegendScale;
                FormattedText ft = Draw.Ft(s.Name + (s.Right ? " (dx)" : ""), 11.5 * ls, Pal.ChartText);
                lx -= ft.Width;
                ctx.DrawText(ft, new Point(lx, 8));
                double legY = 8 + ft.Height / 2;
                ctx.DrawLine(s.Dashed ? Pal.P(s.Color, 2, Pal.Dash) : Pal.P(s.Color, 2.5), new Point(lx - 20 * ls, legY), new Point(lx - 4, legY));
                lx -= 30 * ls;
            }

            // lettura al passaggio del mouse
            if (!double.IsNaN(hoverX) && hoverX >= pr.Left && hoverX <= pr.Right)
            {
                double xv = xmin + (hoverX - pr.Left) / pr.Width * (xmax - xmin);
                int best = 0;
                for (int i = 1; i < X.Length; i++) if (Math.Abs(X[i] - xv) < Math.Abs(X[best] - xv)) best = i;
                double xx = PX(X[best]);
                ctx.DrawLine(Pal.P(Pal.A(200, Pal.Fore), 1), new Point(xx, pr.Top), new Point(xx, pr.Bottom));
                List<string> lines = new List<string>();
                lines.Add(Draw.N(X[best], XLabel == "MHz" ? 3 : (Math.Abs(X[best] - Math.Round(X[best])) < 1e-9 ? 0 : 2)) + " " + Lang.T(XLabel));   // 1.5.2: fumetto tradotto; 1.5.8: i passi senza decimali («39.000 steps»)
                foreach (LineSeries s in Series)
                    if (s.Y != null && best < s.Y.Length) lines.Add(Lang.T(s.Name) + ": " + Draw.N(s.Y[best], s.Decimals));
                double bw = 0;
                foreach (string l in lines) bw = Math.Max(bw, Draw.Ft(l, 11.5, Pal.Fore).Width);
                double bx = xx + 10, by = pr.Top + 8, bh = lines.Count * 16 + 8;
                if (bx + bw + 16 > pr.Right) bx = xx - bw - 26;
                ctx.DrawRectangle(Pal.B(Pal.A(235, Pal.Panel)), Pal.P(Pal.Border, 1), new Rect(bx, by, bw + 16, bh), 6, 6);
                for (int i = 0; i < lines.Count; i++)
                    Draw.Text(ctx, lines[i], bx + 8, by + 4 + i * 16, 11.5, i == 0 ? Pal.Fore : Pal.ChartText, i == 0 ? FontWeight.SemiBold : FontWeight.Normal);
            }
        }
    }
}
