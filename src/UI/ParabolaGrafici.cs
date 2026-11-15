// YA-NEC - Parabola primo fuoco: controlli di disegno (diagramma cartesiano e sezione meccanica del disco).
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    /// <summary>Grafico cartesiano semplice: piu' serie, linee di riferimento, fascia evidenziata.</summary>
    public class ParabolaGrafico : Control
    {
        public class Serie
        {
            public double[] X, Y;
            public Color Colore = Pal.Accent;
            public double Spessore = 2.0;
            public bool Tratteggio;
            public string Nome;
            public bool Specchia;   // disegna anche -X (diagramma simmetrico)
        }

        public readonly List<Serie> Serie_ = new List<Serie>();
        public readonly List<(double y, string testo, Color col)> LineeY = new List<(double, string, Color)>();
        public readonly List<(double x, string testo, Color col)> LineeX = new List<(double, string, Color)>();
        public double? FasciaX1, FasciaX2; public string FasciaTesto; public Color FasciaColore = Pal.Yellow;
        public string TitoloX = "", TitoloY = "", Titolo = "";
        public double XMin, XMax, YMin = -40, YMax = 0, PassoX = 0, PassoY = 10;
        public string FormatoX = "0.0";
        // aggiunte per la pagina "Feed"
        public readonly List<(double x, double y, Color col, string testo, char forma)> Marcatori = new List<(double, double, Color, string, char)>();
        public readonly List<(double x, double y, string testo, Color col, bool destra)> Testi = new List<(double, double, string, Color, bool)>();
        public double? FasciaY1, FasciaY2; public string FasciaYTesto; public Color FasciaYColore = Color.Parse("#2E7D32");
        public int LegendaPos;                 // 0 in alto a sinistra, 1 in basso a sinistra, 2 in alto a destra
        public double ScalaX2;                 // > 0: secondo asse X in alto (valore x * ScalaX2), es. mm -> lambda
        public string TitoloX2 = "", FormatoX2 = "0.00";
        public byte AlfaFasciaX = 70;

        static Color Griglia => Pal.ChartGrid;
        static Color Assi => Pal.ChartText;
        static Color Testo => Pal.ChartText;

        public ParabolaGrafico() { MinHeight = 260; ClipToBounds = true; }

        FormattedText Ft(string s, double size, Color c, bool bold = false)
        {
            return new FormattedText(s ?? "", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, bold ? FontWeight.Bold : FontWeight.Normal), size, new SolidColorBrush(c));
        }

        static double Passo(double span)
        {
            double raw = span / 8.0;
            double m = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double n = raw / m;
            return (n < 1.5 ? 1 : n < 3.5 ? 2 : n < 7.5 ? 5 : 10) * m;
        }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            if (W < 50 || H < 50) return;
            ctx.DrawRectangle(Pal.B(Pal.ChartBg), null, new Rect(0, 0, W, H));
            double l = 52, r = 14, t = (string.IsNullOrEmpty(Titolo) ? 12 : 30) + (ScalaX2 > 0 ? 18 : 0), b = 40;
            Rect pr = new Rect(l, t, Math.Max(10, W - l - r), Math.Max(10, H - t - b));
            if (!string.IsNullOrEmpty(Titolo)) ctx.DrawText(Ft(Titolo, 13, Testo, true), new Point(l, 6));
            if (XMax <= XMin) return;
            Func<double, double> px = x => pr.Left + (x - XMin) / (XMax - XMin) * pr.Width;
            Func<double, double> py = y => pr.Top + (YMax - Math.Max(YMin, Math.Min(YMax, y))) / (YMax - YMin) * pr.Height;

            // fascia (es. disco lunare)
            if (FasciaX1.HasValue && FasciaX2.HasValue)
            {
                var fc = FasciaColore; var fb = new SolidColorBrush(Color.FromArgb(AlfaFasciaX, fc.R, fc.G, fc.B));
                double x1 = Math.Max(pr.Left, px(FasciaX1.Value)), x2 = Math.Min(pr.Right, px(FasciaX2.Value));
                if (x2 > x1)
                {
                    ctx.DrawRectangle(fb, null, new Rect(x1, pr.Top, x2 - x1, pr.Height));
                    if (!string.IsNullOrEmpty(FasciaTesto)) ctx.DrawText(Ft(FasciaTesto, 11, Pal.WarnText), new Point(x2 + 3, pr.Top + 2));
                }
            }

            // fascia orizzontale (es. +/-22,5 gradi di fase)
            if (FasciaY1.HasValue && FasciaY2.HasValue)
            {
                var yc = FasciaYColore;
                double y1 = py(FasciaY2.Value), y2 = py(FasciaY1.Value);
                ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(22, yc.R, yc.G, yc.B)), null, new Rect(pr.Left, y1, pr.Width, Math.Max(0, y2 - y1)));
                var pyc = new Pen(new SolidColorBrush(yc), 0.8, new DashStyle(new double[] { 5, 4 }, 0));
                ctx.DrawLine(pyc, new Point(pr.Left, y1), new Point(pr.Right, y1));
                ctx.DrawLine(pyc, new Point(pr.Left, y2), new Point(pr.Right, y2));
                if (!string.IsNullOrEmpty(FasciaYTesto)) ctx.DrawText(Ft(FasciaYTesto, 11, yc), new Point(pr.Left + 6, y1 - 15));
            }

            // griglia
            var pg = new Pen(new SolidColorBrush(Griglia), 1);
            double sx = PassoX > 0 ? PassoX : Passo(XMax - XMin);
            for (double x = Math.Ceiling(XMin / sx) * sx; x <= XMax + 1e-9; x += sx)
            {
                double X = px(x);
                ctx.DrawLine(pg, new Point(X, pr.Top), new Point(X, pr.Bottom));
                var ft = Ft(x.ToString(FormatoX, CultureInfo.CurrentCulture), 11, Testo);
                ctx.DrawText(ft, new Point(X - ft.Width / 2, pr.Bottom + 3));
                if (ScalaX2 > 0)
                {
                    var f2 = Ft((x * ScalaX2).ToString(FormatoX2, CultureInfo.CurrentCulture), 10.5, Testo);
                    ctx.DrawText(f2, new Point(X - f2.Width / 2, pr.Top - 16));
                }
            }
            double sy = PassoY > 0 ? PassoY : Passo(YMax - YMin);
            for (double y = Math.Ceiling(YMin / sy) * sy; y <= YMax + 1e-9; y += sy)
            {
                double Y = py(y);
                ctx.DrawLine(pg, new Point(pr.Left, Y), new Point(pr.Right, Y));
                var ft = Ft(y.ToString("0.#", CultureInfo.CurrentCulture), 11, Testo);
                ctx.DrawText(ft, new Point(pr.Left - ft.Width - 4, Y - ft.Height / 2));
            }
            ctx.DrawRectangle(null, new Pen(new SolidColorBrush(Assi), 1), pr);
            var ftx = Ft(TitoloX, 11, Testo); ctx.DrawText(ftx, new Point(pr.Left + pr.Width / 2 - ftx.Width / 2, pr.Bottom + 20));
            ctx.DrawText(Ft(TitoloY, 11, Testo), new Point(4, Math.Max(0, pr.Top - 16)));
            if (ScalaX2 > 0 && !string.IsNullOrEmpty(TitoloX2))
            {
                var t2 = Ft(TitoloX2, 10.5, Testo);
                ctx.DrawText(t2, new Point(pr.Right - t2.Width, pr.Top - 31));
            }

            using (ctx.PushClip(pr))
            {
                var dash = new DashStyle(new double[] { 5, 4 }, 0);
                foreach (var ly in LineeY)
                {
                    var pen = new Pen(new SolidColorBrush(ly.col), 1.2, dash);
                    double Y = py(ly.y);
                    ctx.DrawLine(pen, new Point(pr.Left, Y), new Point(pr.Right, Y));
                    if (!string.IsNullOrEmpty(ly.testo)) ctx.DrawText(Ft(ly.testo, 11, ly.col), new Point(pr.Right - 4 - Ft(ly.testo, 11, ly.col).Width, Y - 16));
                }
                foreach (var lx in LineeX)
                {
                    var pen = new Pen(new SolidColorBrush(lx.col), 1.2, dash);
                    double X = px(lx.x);
                    ctx.DrawLine(pen, new Point(X, pr.Top), new Point(X, pr.Bottom));
                    if (!string.IsNullOrEmpty(lx.testo)) ctx.DrawText(Ft(lx.testo, 11, lx.col), new Point(X + 3, pr.Top + 14));
                }
                foreach (var s in Serie_)
                {
                    if (s.X == null || s.Y == null || s.X.Length < 2) continue;
                    var pen = new Pen(new SolidColorBrush(s.Colore), s.Spessore, s.Tratteggio ? new DashStyle(new double[] { 4, 3 }, 0) : null);
                    for (int lato = 0; lato < (s.Specchia ? 2 : 1); lato++)
                    {
                        double seg = lato == 0 ? 1 : -1;
                        var g = new StreamGeometry();
                        using (var c = g.Open())
                        {
                            c.BeginFigure(new Point(px(seg * s.X[0]), py(s.Y[0])), false);
                            for (int i = 1; i < s.X.Length; i++) c.LineTo(new Point(px(seg * s.X[i]), py(s.Y[i])));
                            c.EndFigure(false);
                        }
                        ctx.DrawGeometry(null, pen, g);
                    }
                }
                foreach (var m in Marcatori)
                {
                    var br = new SolidColorBrush(m.col);
                    var c0 = new Point(px(m.x), py(m.y));
                    if (m.forma == 'v')
                    {
                        var tri = new StreamGeometry();
                        using (var c = tri.Open())
                        {
                            c.BeginFigure(new Point(c0.X - 6, c0.Y - 9), true);
                            c.LineTo(new Point(c0.X + 6, c0.Y - 9)); c.LineTo(c0); c.EndFigure(true);
                        }
                        ctx.DrawGeometry(br, null, tri);
                    }
                    else ctx.DrawEllipse(br, null, c0, 4.5, 4.5);
                    if (!string.IsNullOrEmpty(m.testo)) ctx.DrawText(Ft(m.testo, 11, m.col), new Point(c0.X + 6, c0.Y - 26));
                }
                foreach (var tx in Testi)
                {
                    var ft = Ft(tx.testo, 11, tx.col);
                    double X = px(tx.x), Y = py(tx.y);
                    ctx.DrawText(ft, new Point(tx.destra ? X - ft.Width : X, Y - ft.Height / 2));
                }
            }

            // legenda
            int nLeg = 0;
            foreach (var s0 in Serie_) if (!string.IsNullOrEmpty(s0.Nome)) nLeg++;
            double lyp = LegendaPos == 1 ? pr.Bottom - 6 - 18 * nLeg : pr.Top + 4;
            double lxp = LegendaPos == 2 ? pr.Right - 246 : pr.Left;
            foreach (var s in Serie_)
            {
                if (string.IsNullOrEmpty(s.Nome)) continue;
                var pen = new Pen(new SolidColorBrush(s.Colore), s.Spessore, s.Tratteggio ? new DashStyle(new double[] { 4, 3 }, 0) : null);
                ctx.DrawRectangle(new SolidColorBrush(Pal.A(220, Pal.ChartBg)), null, new Rect(lxp + 6, lyp - 1, 236, 17));
                ctx.DrawLine(pen, new Point(lxp + 10, lyp + 8), new Point(lxp + 34, lyp + 8));
                ctx.DrawText(Ft(s.Nome, 11, Testo), new Point(lxp + 40, lyp));
                lyp += 18;
            }
        }
    }

    /// <summary>Sezione del disco in scala: profilo, fuoco, feed, puntoni, anelli, baricentro, asse di elevazione, contrappeso.</summary>
    public class ParabolaSezione : Control
    {
        public ParabolaInput Inp;
        public ParabolaResult Res;
        public ParabolaSezione() { MinHeight = 300; ClipToBounds = true; }

        FormattedText Ft(string s, double size, Color c)
        {
            return new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, size, new SolidColorBrush(c));
        }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.ChartBg), null, new Rect(0, 0, W, H));
            if (Inp == null || Res == null || Res.Errore != null || W < 80 || H < 80) return;
            double a = Inp.Diametro_m / 2, F = Res.Fuoco_m, Hd = Res.Profondita_m;
            double zMin = -(Inp.PivotDietroVertice_m + Inp.BraccioContrappeso_m) - 0.1, zMax = Math.Max(F, Hd) + 0.15;
            double rMax = a + 0.1;
            // asse del disco orizzontale: z verso destra, r verso l'alto
            double sc = Math.Min((W - 40) / (zMax - zMin), (H - 50) / (2 * rMax));
            double ox = 20 - zMin * sc, oy = H / 2;
            Func<double, double, Point> P = (z, r) => new Point(ox + z * sc, oy - r * sc);

            var blu = Pal.Accent; var gr = Pal.ChartText; var ar = Pal.Orange; var ro = Pal.Red;
            var pAsse = new Pen(new SolidColorBrush(Pal.Muted), 1, new DashStyle(new double[] { 6, 4 }, 0));
            ctx.DrawLine(pAsse, P(zMin, 0), P(zMax, 0));

            // profilo
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                int n = 80;
                for (int i = 0; i <= n; i++)
                {
                    double r = -a + 2 * a * i / n, z = r * r / (4 * F);
                    if (i == 0) c.BeginFigure(P(z, r), false); else c.LineTo(P(z, r));
                }
                c.EndFigure(false);
            }
            ctx.DrawGeometry(null, new Pen(new SolidColorBrush(blu), 3), g);

            // anelli
            if (Res.AnelliRaggio_m != null)
                foreach (var i in new[] { 1, -1 })
                    for (int k = 0; k < Res.AnelliRaggio_m.Length; k++)
                        ctx.DrawEllipse(new SolidColorBrush(gr), null, P(Res.AnelliZ_m[k], i * Res.AnelliRaggio_m[k]), 3, 3);

            // puntoni e feed
            double rs = Inp.PuntoneAttacco_frac * a, zs = rs * rs / (4 * F);
            var pPunt = new Pen(new SolidColorBrush(gr), 2);
            ctx.DrawLine(pPunt, P(zs, rs), P(F, 0));
            ctx.DrawLine(pPunt, P(zs, -rs), P(F, 0));
            double fr = Inp.FeedDiametro_m / 2;
            ctx.DrawRectangle(new SolidColorBrush(Pal.AccentSoft), new Pen(new SolidColorBrush(blu), 1.5), new Rect(P(F - 0.02, fr), P(F + 0.12, -fr)));
            ctx.DrawEllipse(new SolidColorBrush(ro), null, P(F, 0), 4, 4);
            ctx.DrawText(Ft(string.Format(CultureInfo.CurrentCulture, "fuoco F = {0:0} mm", F * 1000), 11, ro), P(F - 0.05, -fr - 0.05));

            // raggi dal fuoco al bordo (angolo di bordo)
            var pRag = new Pen(new SolidColorBrush(Pal.A(120, Pal.Red)), 1, new DashStyle(new double[] { 3, 3 }, 0));
            ctx.DrawLine(pRag, P(F, 0), P(Hd, a)); ctx.DrawLine(pRag, P(F, 0), P(Hd, -a));

            // mozzo, asse di elevazione, braccio e contrappeso
            ctx.DrawRectangle(new SolidColorBrush(gr), null, new Rect(P(-0.08, 0.08), P(0, -0.08)));
            double zp = -Inp.PivotDietroVertice_m;
            ctx.DrawEllipse(Pal.B(Pal.ChartBg), new Pen(new SolidColorBrush(Pal.Fore), 2), P(zp, 0), 6, 6);
            ctx.DrawText(Ft("asse elev.", 11, gr), P(zp - 0.12, -0.12));
            double zc = zp - Inp.BraccioContrappeso_m;
            ctx.DrawLine(new Pen(new SolidColorBrush(gr), 3), P(zp, 0), P(zc, 0));
            double cr = Math.Max(4, Math.Min(26, Math.Sqrt(Math.Max(0, double.IsNaN(Res.Contrappeso_kg) ? 0 : Res.Contrappeso_kg)) * 4));
            ctx.DrawEllipse(new SolidColorBrush(ar), null, P(zc, 0), cr, cr);
            ctx.DrawText(Ft(string.Format(CultureInfo.CurrentCulture, "contrappeso {0:0.0} kg", Res.Contrappeso_kg), 11, ar), new Point(P(zc, 0).X - 30, P(zc, 0).Y + cr + 4));

            // baricentro
            var cg = P(Res.Baricentro_m, 0);
            ctx.DrawEllipse(Pal.B(Pal.ChartBg), new Pen(new SolidColorBrush(Pal.Fore), 1.5), cg, 7, 7);
            ctx.DrawLine(new Pen(new SolidColorBrush(Pal.Fore), 1.5), new Point(cg.X - 7, cg.Y), new Point(cg.X + 7, cg.Y));
            ctx.DrawLine(new Pen(new SolidColorBrush(Pal.Fore), 1.5), new Point(cg.X, cg.Y - 7), new Point(cg.X, cg.Y + 7));
            ctx.DrawText(Ft(string.Format(CultureInfo.CurrentCulture, "baricentro +{0:0} mm", Res.Baricentro_m * 1000), 11, gr), new Point(cg.X + 9, cg.Y + 4));

            // quote
            ctx.DrawText(Ft(string.Format(CultureInfo.CurrentCulture, "D = {0:0} mm   profondita' = {1:0} mm   angolo di bordo = {2:0.0} gradi", Inp.Diametro_m * 1000, Hd * 1000, Res.Psi0_deg), 12, gr), new Point(10, 6));
        }
    }
}
