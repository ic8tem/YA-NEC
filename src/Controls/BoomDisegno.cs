// YA-NEC 0.4.20 - disegno della correzione del boom (bozzetto approvato da IC8TEM il 14/11/2026 18:53, "perfetto applica"):
// boom orizzontale, elementi verticali alla loro posizione; blu pieno = misura fisica (con il boom), arancio tratteggiato = filo
// elettrico del NEC (senza boom). La differenza e' esagerata perche' si veda (pochi millimetri su un metro).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace YaNec
{
    public class BoomDisegno : Control
    {
        public class El { public string Nome; public double Pos_m, Fisico_mm, Elettrico_mm; }
        public List<El> Elementi = new List<El>();
        /// <summary>Vero nel verso "rilievo -> modello elettrico" (cambia solo la legenda).</summary>
        public bool Rilievo;

        public BoomDisegno() { MinHeight = 380; ClipToBounds = true; }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.Panel), null, new Rect(0, 0, W, H));
            if (Elementi.Count == 0)
            {
                Draw.TextC(ctx, "Apri un modello: qui si disegnano boom ed elementi", W / 2, H / 2, 13, Pal.Muted);
                return;
            }
            double pmin = double.MaxValue, pmax = double.MinValue, lmax = 0;
            foreach (El e in Elementi) { pmin = Math.Min(pmin, e.Pos_m); pmax = Math.Max(pmax, e.Pos_m); lmax = Math.Max(lmax, Math.Max(e.Fisico_mm, e.Elettrico_mm)); }
            double x0 = 40, x1 = W - 40, yc = (H - 70) / 2 + 10, hMax = (H - 90) / 2;
            double span = Math.Max(1e-9, pmax - pmin);
            double k = lmax > 0 ? hMax / (lmax / 2) : 1;      // px per mm (mezzo elemento)
            ctx.DrawRectangle(Pal.B(Color.FromRgb(185, 195, 211)), null, new Rect(x0 - 10, yc - 7, x1 - x0 + 20, 14));
            IPen blu = new Pen(Pal.B(Pal.Accent), 4.5), aran = new Pen(Pal.B(Pal.Orange), 2.6, new DashStyle(new double[] { 3, 2 }, 0));
            foreach (El e in Elementi)
            {
                double x = Elementi.Count == 1 ? (x0 + x1) / 2 : x0 + (e.Pos_m - pmin) / span * (x1 - x0);
                // differenza esagerata 12 volte, perche' sia visibile
                double hf = e.Fisico_mm / 2 * k, he = hf - (e.Fisico_mm - e.Elettrico_mm) / 2 * k * 12;
                ctx.DrawLine(blu, new Point(x, yc - hf), new Point(x, yc + hf));
                ctx.DrawLine(aran, new Point(x + 6, yc - he), new Point(x + 6, yc + he));
                Draw.TextC(ctx, e.Nome, x, yc + hf + 12, 11.5, Pal.Fore);
            }
            double yl = H - 52;
            Draw.Text(ctx, "differenza esagerata per vederla", x0 - 10, yl - 16, 11, Pal.Muted);
            if (Rilievo)
            {
                Draw.Text(ctx, "— rilievo (misure reali, con il boom)", x0 - 10, yl, 13, Pal.Accent);
                Draw.Text(ctx, "- - modello elettrico per il NEC (senza boom)", x0 - 10, yl + 18, 13, Pal.Orange);
            }
            else
            {
                Draw.Text(ctx, "- - progetto elettrico (fili NEC, senza boom)", x0 - 10, yl, 13, Pal.Orange);
                Draw.Text(ctx, "— elementi da realizzare (con il boom)", x0 - 10, yl + 18, 13, Pal.Accent);
            }
        }
    }
}
