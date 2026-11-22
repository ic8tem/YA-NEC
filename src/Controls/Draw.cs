// YA-NEC - piccoli aiuti per il disegno (testi, numeri)
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace YaNec
{
    public static class Draw
    {
        // grandezza delle didascalie (legende) dei grafici: 0,9 ... 2,5 (cursore in alto a destra, salvato in config.ini)
        public static double LegendScale = 1.0;

        public static FormattedText Ft(string s, double size, Color c, FontWeight w = FontWeight.Normal)
        {
            // 0.5.3: le scritte dei disegni passano dalla traduzione (testo italiano = chiave)
            return new FormattedText(Lang.TC(s ?? ""),   // 1.5.6: anche i testi composti (es. «contrappeso 2,11 m»)
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, w), size, new ImmutableSolidColorBrush(c));
        }

        public static void Text(DrawingContext ctx, string s, double x, double y, double size, Color c, FontWeight w = FontWeight.Normal)
        {
            ctx.DrawText(Ft(s, size, c, w), new Point(x, y));
        }

        // testo centrato su (cx, cy)
        public static void TextC(DrawingContext ctx, string s, double cx, double cy, double size, Color c, FontWeight w = FontWeight.Normal)
        {
            FormattedText ft = Ft(s, size, c, w);
            ctx.DrawText(ft, new Point(cx - ft.Width / 2, cy - ft.Height / 2));
        }

        // testo allineato a destra che finisce in x
        public static void TextR(DrawingContext ctx, string s, double x, double y, double size, Color c, FontWeight w = FontWeight.Normal)
        {
            FormattedText ft = Ft(s, size, c, w);
            ctx.DrawText(ft, new Point(x - ft.Width, y));
        }

        public static string N(double v, int dec)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "-";
            return v.ToString("F" + dec, CultureInfo.CurrentCulture);
        }

        // passo "rotondo" per le griglie
        public static double NiceStep(double range, int target)
        {
            if (range <= 0) return 1;
            double raw = range / Math.Max(1, target);
            double p = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double[] k = { 1, 2, 2.5, 5, 10 };
            foreach (double x in k) if (x * p >= raw) return x * p;
            return 10 * p;
        }
    }
}
