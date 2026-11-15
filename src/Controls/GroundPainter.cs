// YA-NEC - disegno del terreno secondo il tipo scelto (bozzetto approvato "terreno e quota"):
// colore di fondo e trama (puntini, trattini, erba, onde, mattoni, griglia) per ogni terreno tipico,
// usati sia sul piano z = 0 della Struttura 3D sia nei campioni della legenda sotto la vista.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace YaNec
{
    public enum GroundTexture { Grid, Dots, Dashes, Grass, Waves, Bricks, Plain }

    public class GroundStyle
    {
        public string Key, Name;
        public Color Base, Mark;
        public GroundTexture Texture;
        public GroundStyle(string key, string name, Color b, Color m, GroundTexture t) { Key = key; Name = name; Base = b; Mark = m; Texture = t; }
    }

    public static class GroundPainter
    {
        // stesso ordine dei terreni tipici della tendina Terreno (dopo "Perfetto")
        public static readonly GroundStyle[] Styles =
        {
            new GroundStyle("perfetto",   "Perfetto",     Pal.Rgb(142, 157, 181), Pal.Rgb(93, 111, 140),  GroundTexture.Grid),
            new GroundStyle("medio",      "Medio",        Pal.Rgb(201, 167, 124), Pal.Rgb(138, 106, 69),  GroundTexture.Dots),
            new GroundStyle("povero",     "Povero",       Pal.Rgb(216, 188, 146), Pal.Rgb(156, 123, 82),  GroundTexture.Dots),
            new GroundStyle("citta",      "Citta'",       Pal.Rgb(169, 169, 169), Pal.Rgb(112, 112, 112), GroundTexture.Bricks),
            new GroundStyle("sabbia",     "Sabbia secca", Pal.Rgb(235, 217, 164), Pal.Rgb(184, 158, 94),  GroundTexture.Dashes),
            new GroundStyle("pascolo",    "Pascolo",      Pal.Rgb(156, 203, 108), Pal.Rgb(78, 138, 44),   GroundTexture.Grass),
            new GroundStyle("palude",     "Palude",       Pal.Rgb(78, 127, 82),   Pal.Rgb(47, 90, 51),    GroundTexture.Waves),
            new GroundStyle("fertile",    "Fertile",      Pal.Rgb(139, 94, 60),   Pal.Rgb(90, 58, 34),    GroundTexture.Dots),
            new GroundStyle("moltobuono", "Molto buono",  Pal.Rgb(107, 156, 74),  Pal.Rgb(62, 107, 42),   GroundTexture.Grass),
            new GroundStyle("acquadolce", "Acqua dolce",  Pal.Rgb(140, 196, 238), Pal.Rgb(236, 246, 255), GroundTexture.Waves),
            new GroundStyle("mare",       "Mare",         Pal.Rgb(59, 124, 196),  Pal.Rgb(207, 228, 247), GroundTexture.Waves),
            // terreni di 4nec2 (0.4.2): stesso ordine della tabella Grounds di MainWindow
            new GroundStyle("acquadolce10", "Acqua dolce 10°", Pal.Rgb(150, 202, 240), Pal.Rgb(236, 246, 255), GroundTexture.Waves),
            new GroundStyle("acquadolce20", "Acqua dolce 20°", Pal.Rgb(130, 190, 236), Pal.Rgb(236, 246, 255), GroundTexture.Waves),
            new GroundStyle("mare10",     "Mare 10°",     Pal.Rgb(66, 132, 204),  Pal.Rgb(207, 228, 247), GroundTexture.Waves),
            new GroundStyle("mare20",     "Mare 20°",     Pal.Rgb(52, 116, 188),  Pal.Rgb(207, 228, 247), GroundTexture.Waves),
            new GroundStyle("ghiacciomare", "Ghiaccio marino", Pal.Rgb(214, 232, 242), Pal.Rgb(150, 182, 204), GroundTexture.Plain),
            new GroundStyle("ghiacciopolo", "Ghiaccio polare", Pal.Rgb(226, 238, 246), Pal.Rgb(160, 190, 210), GroundTexture.Plain),
            new GroundStyle("calotta",    "Calotta polare", Pal.Rgb(240, 246, 250), Pal.Rgb(176, 198, 214), GroundTexture.Plain),
            new GroundStyle("artica",     "Terra artica", Pal.Rgb(190, 196, 186), Pal.Rgb(130, 138, 126), GroundTexture.Dots),
            new GroundStyle("citta1",     "Citta' ind.",  Pal.Rgb(160, 160, 164), Pal.Rgb(104, 104, 108), GroundTexture.Bricks),
            new GroundStyle("citta2",     "Citta' ind. media", Pal.Rgb(176, 172, 168), Pal.Rgb(116, 112, 108), GroundTexture.Bricks),
            new GroundStyle("citta3",     "Citta' ind. max", Pal.Rgb(150, 150, 156), Pal.Rgb(96, 96, 102), GroundTexture.Bricks),
        };

        // terreno personalizzato o non riconosciuto: colore neutro, i valori si scrivono sul campione
        public static readonly GroundStyle Neutral = new GroundStyle("neutro", "Personalizzato", Pal.Rgb(200, 200, 200), Pal.Rgb(150, 150, 150), GroundTexture.Plain);

        public static GroundStyle ByIndex(int i) { return i >= 0 && i < Styles.Length ? Styles[i] : Neutral; }

        // generatore deterministico: la trama non "balla" quando si ruota la vista
        static double Hash(int i, int k)
        {
            unchecked
            {
                uint h = (uint)(i * 374761393 + k * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (double)0x1000000;
            }
        }

        // ---------------- campione piatto (legenda) ----------------
        public static void Swatch(DrawingContext ctx, Rect r, GroundStyle st, string text = null)
        {
            using (ctx.PushClip(r))
            {
                ctx.DrawRectangle(Pal.B(st.Base), null, r);
                IPen pen = Pal.P(st.Mark, 1.2);
                IBrush br = Pal.B(st.Mark);
                switch (st.Texture)
                {
                    case GroundTexture.Grid:
                        for (double x = r.X + 6; x < r.Right; x += 9) ctx.DrawLine(pen, new Point(x, r.Y), new Point(x, r.Bottom));
                        for (double y = r.Y + 5; y < r.Bottom; y += 9) ctx.DrawLine(pen, new Point(r.X, y), new Point(r.Right, y));
                        break;
                    case GroundTexture.Bricks:
                        {
                            int row = 0;
                            for (double y = r.Y; y < r.Bottom; y += 12, row++)
                            {
                                ctx.DrawLine(pen, new Point(r.X, y), new Point(r.Right, y));
                                for (double x = r.X + (row % 2 == 0 ? 10 : 22); x < r.Right; x += 24) ctx.DrawLine(pen, new Point(x, y), new Point(x, y + 12));
                            }
                        }
                        break;
                    case GroundTexture.Waves:
                        for (double y = r.Y + 6; y < r.Bottom; y += 9)
                        {
                            Point p0 = new Point(r.X, y);
                            for (double x = r.X; x <= r.Right; x += 3)
                            {
                                Point p1 = new Point(x, y + 2.2 * Math.Sin((x - r.X) / 5.0));
                                ctx.DrawLine(pen, p0, p1);
                                p0 = p1;
                            }
                        }
                        break;
                    case GroundTexture.Plain:
                        break;
                    default:
                        int n = (int)(r.Width * r.Height / 70);
                        for (int i = 0; i < n; i++)
                        {
                            double x = r.X + Hash(i, 1) * r.Width, y = r.Y + Hash(i, 2) * r.Height;
                            if (st.Texture == GroundTexture.Dots) ctx.DrawEllipse(br, null, new Point(x, y), 1 + Hash(i, 3), 1 + Hash(i, 3) * 0.8);
                            else if (st.Texture == GroundTexture.Dashes) ctx.DrawLine(pen, new Point(x, y), new Point(x + 4, y));
                            else if (i % 3 == 0) ctx.DrawLine(Pal.P(st.Mark, 1.6), new Point(x, y), new Point(x, y - 7));
                        }
                        break;
                }
                if (!string.IsNullOrEmpty(text)) Draw.TextC(ctx, text, r.X + r.Width / 2, r.Y + r.Height / 2, 11, Pal.Rgb(60, 60, 60), FontWeight.SemiBold);
            }
            ctx.DrawRectangle(null, Pal.P(Pal.Border, 1), r);
        }

        // ---------------- piano z = 0 nella vista 3D ----------------
        // proj: (x, y) del piano -> punto sullo schermo. Il piano va da (x0, y0) a (x1, y1).
        public static void Plane(DrawingContext ctx, Func<double, double, Point> proj, double x0, double y0, double x1, double y1, double step, GroundStyle st)
        {
            StreamGeometry g = new StreamGeometry();
            using (StreamGeometryContext gc = g.Open())
            {
                gc.BeginFigure(proj(x0, y0), true);
                gc.LineTo(proj(x1, y0));
                gc.LineTo(proj(x1, y1));
                gc.LineTo(proj(x0, y1));
                gc.EndFigure(true);
            }
            ctx.DrawGeometry(Pal.B(Pal.A(215, st.Base)), null, g);
            IPen grid = Pal.P(Pal.A(90, st.Mark), 1);
            for (double x = x0; x <= x1 + 1e-9; x += step) ctx.DrawLine(grid, proj(x, y0), proj(x, y1));
            for (double y = y0; y <= y1 + 1e-9; y += step) ctx.DrawLine(grid, proj(x0, y), proj(x1, y));

            double w = x1 - x0, h = y1 - y0;
            IPen pen = Pal.P(Pal.A(200, st.Mark), 1.1);
            IBrush br = Pal.B(Pal.A(200, st.Mark));
            switch (st.Texture)
            {
                case GroundTexture.Grid:
                    {
                        IPen fine = Pal.P(Pal.A(150, st.Mark), 0.8);
                        for (double x = x0; x <= x1; x += step / 4) ctx.DrawLine(fine, proj(x, y0), proj(x, y1));
                        for (double y = y0; y <= y1; y += step / 4) ctx.DrawLine(fine, proj(x0, y), proj(x1, y));
                    }
                    break;
                case GroundTexture.Bricks:
                    {
                        double bh = step / 3, bw = step / 1.5;
                        int row = 0;
                        for (double y = y0; y < y1; y += bh, row++)
                        {
                            ctx.DrawLine(pen, proj(x0, y), proj(x1, y));
                            for (double x = x0 + (row % 2 == 0 ? 0 : bw / 2); x < x1; x += bw) ctx.DrawLine(pen, proj(x, y), proj(x, Math.Min(y1, y + bh)));
                        }
                    }
                    break;
                case GroundTexture.Waves:
                    {
                        double dy = step / 4, amp = step / 25, lam = step / 3;
                        for (double y = y0 + dy / 2; y < y1; y += dy)
                        {
                            Point p0 = proj(x0, y);
                            for (double x = x0; x <= x1; x += lam / 6)
                            {
                                Point p1 = proj(x, y + amp * Math.Sin(2 * Math.PI * (x - x0) / lam));
                                ctx.DrawLine(pen, p0, p1);
                                p0 = p1;
                            }
                        }
                    }
                    break;
                case GroundTexture.Plain:
                    break;
                default:
                    {
                        int n = 700;
                        for (int i = 0; i < n; i++)
                        {
                            double x = x0 + Hash(i, 1) * w, y = y0 + Hash(i, 2) * h;
                            Point p = proj(x, y);
                            if (st.Texture == GroundTexture.Dots) ctx.DrawEllipse(br, null, p, 1.1 + Hash(i, 3) * 1.3, 0.9 + Hash(i, 3) * 0.9);
                            else if (st.Texture == GroundTexture.Dashes) ctx.DrawLine(pen, p, proj(x + step / 12, y));
                            else if (i % 2 == 0) ctx.DrawLine(Pal.P(Pal.A(220, st.Mark), 1.4), p, new Point(p.X, p.Y - 5));
                        }
                    }
                    break;
            }
        }
    }

    // riga di campioni sotto la vista 3D: "Come viene disegnato ogni terreno"
    public class GroundLegend : Control
    {
        public string SelectedKey = "";
        public string CustomText = "";

        public GroundLegend() { Height = 30 + (GroundPainter.Styles.Length + 1 + 5) / 6 * 42; }   // dalla 0.4.2 piu' righe (terreni di 4nec2)

        // due righe da sei campioni (bozzetto approvato "Struttura divisa")
        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width;
            ctx.DrawRectangle(Pal.B(Pal.ChartBg), null, new Rect(0, 0, W, Bounds.Height));
            ctx.DrawLine(Pal.P(Pal.Border, 1), new Point(0, 0.5), new Point(W, 0.5));
            Draw.Text(ctx, "Come viene disegnato ogni terreno (il colore segue il tipo scelto nel menu Terreno):", 10, 6, 12.5, Pal.ChartText, FontWeight.SemiBold);
            List<GroundStyle> all = new List<GroundStyle>(GroundPainter.Styles);
            all.Add(GroundPainter.Neutral);
            int perRow = 6;
            double gap = 8, sw = Math.Max(40, (W - 20 - gap * (perRow - 1)) / perRow);
            for (int k = 0; k < all.Count; k++)
            {
                GroundStyle st = all[k];
                double x = 10 + (k % perRow) * (sw + gap), y = 26 + (k / perRow) * 42;
                Rect r = new Rect(x, y, sw, 20);
                GroundPainter.Swatch(ctx, r, st, st == GroundPainter.Neutral ? "εr σ" : null);
                bool on = st.Key == SelectedKey;
                if (on) ctx.DrawRectangle(null, Pal.P(Pal.Accent, 2.5), r.Inflate(2));
                Draw.Text(ctx, st.Name, x, y + 22, 11, on ? Pal.Accent : Pal.ChartText, on ? FontWeight.Bold : FontWeight.Normal);
            }
        }
    }
}
