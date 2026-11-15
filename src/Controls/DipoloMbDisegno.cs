// YA-NEC 0.4.30 - schizzo del Dipolo multibanda, dal bozzetto 13 approvato da IC8TEM il 15/11/2026 00:02: bacchette di fronte (un dipolo
// per banda, uno sotto l'altro, trappole e code), manica aperta e radiatori XR-5 visti dall'alto (boom orizzontale). Linea piena blu =
// alimentati, tratteggio arancio = parassiti / eccitati per vicinanza o code delle trappole, rosso = alimentazione, linea e trappole.
// Le misure scritte sono quelle del modello NEC della scheda (accordate dopo "Accorda e verifica"). Non in scala tra i due assi.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace YaNec
{
    public class DipoloMbDisegno : Control
    {
        public class Tratto { public double X1, Y1, X2, Y2; public int Tipo; }   // 0 alimentato, 1 parassita/coda, 2 trappola, 3 linea
        public class Etichetta { public double X, Y; public string T; public int Tipo; }
        public List<Tratto> Tratti = new List<Tratto>();
        public List<Etichetta> Etichette = new List<Etichetta>();
        public List<double[]> Punti = new List<double[]>();   // alimentazione
        public string Titolo = "", Sotto = "";
        public bool Boom;

        public DipoloMbDisegno() { MinHeight = 300; ClipToBounds = true; }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.Panel), null, new Rect(0, 0, W, H));
            Draw.Text(ctx, Titolo, 12, 8, 15, Pal.Fore, FontWeight.SemiBold);
            Draw.Text(ctx, Sotto, 12, 30, 12.5, Pal.Muted);
            if (Tratti.Count == 0) return;
            double x0 = double.MaxValue, x1 = double.MinValue, y0 = double.MaxValue, y1 = double.MinValue;
            foreach (Tratto t in Tratti) { x0 = Math.Min(x0, Math.Min(t.X1, t.X2)); x1 = Math.Max(x1, Math.Max(t.X1, t.X2)); y0 = Math.Min(y0, Math.Min(t.Y1, t.Y2)); y1 = Math.Max(y1, Math.Max(t.Y1, t.Y2)); }
            double bw = Math.Max(1e-6, x1 - x0), bh = Math.Max(1e-6, y1 - y0);
            double L = 30, R = W - 150, T = 70, B = H - 40;
            // assi in scala diversa (schizzo): ognuno riempie il suo spazio, con un minimo per le distanze piccole
            double kx = (R - L) / bw, ky = (B - T) / bh;
            Func<double, double, Point> P = (x, y) => new Point(L + (x - x0) * kx, B - (y - y0) * ky);
            if (Boom) ctx.DrawLine(new Pen(Pal.B(Color.FromRgb(0xB9, 0xC3, 0xD1)), 8), new Point(L - 10, P(0, 0).Y), new Point(R + 10, P(0, 0).Y));
            foreach (Tratto t in Tratti)
            {
                Color c = t.Tipo == 0 ? Pal.Accent : t.Tipo == 1 ? Pal.Orange : Pal.Red;
                IPen pen = t.Tipo == 1 ? new Pen(Pal.B(c), 3, new DashStyle(new double[] { 3, 2 }, 0)) : new Pen(Pal.B(c), t.Tipo == 2 ? 7 : 3);
                ctx.DrawLine(pen, P(t.X1, t.Y1), P(t.X2, t.Y2));
            }
            foreach (double[] p in Punti) ctx.DrawEllipse(Pal.B(Pal.Red), null, P(p[0], p[1]), 6, 6);
            foreach (Etichetta e in Etichette)
            {
                Point q = P(e.X, e.Y);
                Draw.Text(ctx, e.T, Math.Min(q.X + 6, W - 145), q.Y - 9, 12.5, e.Tipo == 0 ? Pal.Accent : e.Tipo == 1 ? Pal.Orange : Pal.Red);
            }
            Draw.Text(ctx, "— alimentati    - - parassiti / code    ■ trappole    ● alimentazione (non in scala)", 12, H - 22, 12, Pal.Muted);
        }
    }
}
