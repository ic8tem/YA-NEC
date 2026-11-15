// YA-NEC 0.4.30 - disegno della verticale multibanda (bozzetto 10 approvato il 14/11/2026 22:41): palo grigio, elementi blu alla loro
// lunghezza con il nome della banda, alimentazione comune rossa, terreno e radiali arancio. Elementi spenti in grigio chiaro.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public class VerticaleDisegno : Control
    {
        public List<VmElemento> Elementi = new List<VmElemento>();
        public double AltezzaPalo = 9.0, LRad = 5;
        public int Radiali = 32;
        public bool Terreno = true;

        public VerticaleDisegno() { MinHeight = 420; ClipToBounds = true; }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.Panel), null, new Rect(0, 0, W, H));
            double yb = H - 70, hmax = AltezzaPalo + 1;
            foreach (VmElemento e in Elementi) hmax = Math.Max(hmax, Math.Min(e.L, AltezzaPalo) + 0.5);
            double k = (yb - 40) / hmax, xc = W / 2;
            if (Terreno)
            {
                ctx.DrawLine(new Pen(Pal.B(Color.FromRgb(140, 110, 60)), 3), new Point(xc - W * 0.4, yb), new Point(xc + W * 0.4, yb));
                double lr = Math.Min(W * 0.4, LRad * k);
                ctx.DrawLine(new Pen(Pal.B(Pal.Orange), 2), new Point(xc - lr, yb + 3), new Point(xc + lr, yb + 3));
                Draw.Text(ctx, "radiali sul terreno: " + Radiali + " x " + LRad.ToString("0.#") + " m", xc - W * 0.38, yb + 16, 13, Pal.Orange);
            }
            ctx.DrawLine(new Pen(Pal.B(Pal.Muted), 6), new Point(xc, yb), new Point(xc, yb - (AltezzaPalo + 1) * k));
            int n = 0, i = 0;
            foreach (VmElemento e in Elementi) if (!e.Palo) n++;
            foreach (VmElemento e in Elementi)
            {
                Color c = e.Attivo ? Pal.Accent : Pal.Border;
                if (e.Palo)
                {
                    double su = Math.Min(e.L, AltezzaPalo) * k;
                    ctx.DrawLine(new Pen(Pal.B(c), 3), new Point(xc + 4, yb), new Point(xc + 4, yb - su));
                    if (e.L > AltezzaPalo) ctx.DrawLine(new Pen(Pal.B(c), 3), new Point(xc + 10, yb - su), new Point(xc + 10, yb - su + (e.L - AltezzaPalo) * k));
                    Draw.Text(ctx, e.Banda, xc + 14, yb - su - 8, 13, c);
                    continue;
                }
                double dx = (i < (n + 1) / 2 ? -1 : 1) * (22 + 22 * (i < (n + 1) / 2 ? i : i - (n + 1) / 2));
                double x = xc + dx, h = e.L * k;
                ctx.DrawLine(new Pen(Pal.B(c), 3), new Point(x, yb), new Point(x, yb - h));
                Draw.TextC(ctx, e.Banda, x, yb - h - 12, 13, c);
                i++;
            }
            ctx.DrawEllipse(Pal.B(Pal.Red), null, new Point(xc, yb - 2), 6, 6);
            Draw.Text(ctx, "alimentazione comune", xc + 14, yb - 22, 13, Pal.Red);
        }
    }
}
