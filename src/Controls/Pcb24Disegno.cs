// YA-NEC 0.4.30 - disegno quotato delle antenne su PCB a 2,4 GHz (IC8TEM 14/11/2026 22:42, "si va bene"): scheda, piano di massa, pista,
// punto di alimentazione e corto, con le misure calcolate scritte sopra. Le primitive (in mm) vengono da Engine/Pcb24.Disegno: le stesse
// della stampa 1:1 in PDF (UI/Pcb24Pdf.cs) e del DXF, cosi' il disegno a video e la maschera stampata non possono essere diversi.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public class Pcb24Disegno : Control
    {
        public List<PcbPrim> Prim = new List<PcbPrim>();
        public string Legenda = "";

        public static readonly Color Fr4 = Color.FromRgb(0xE4, 0xEF, 0xD8), Fr4Bordo = Color.FromRgb(0x8F, 0xAF, 0x7F);
        public static readonly Color Rame = Color.FromRgb(0xB8, 0x6A, 0x2B), Sotto = Color.FromRgb(0x1F, 0x6F, 0xB2);

        public Pcb24Disegno() { MinHeight = 320; ClipToBounds = true; }

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.Panel), null, new Rect(0, 0, W, H));
            if (Prim == null || Prim.Count == 0) return;
            double[] e = Pcb24.Estensione(Prim);
            double bw = Math.Max(1, e[2] - e[0]), bh = Math.Max(1, e[3] - e[1]);
            double k = Math.Min((W - 40) / bw, (H - 56) / bh), ox = (W - bw * k) / 2, oy = 20 + (H - 56 - bh * k) / 2;
            Func<double, double, Point> T = (u, v) => new Point(ox + (u - e[0]) * k, oy + (e[3] - v) * k);
            // prima le superfici, poi le piste, poi le quote e i testi
            foreach (PcbPrim p in Prim)
            {
                if (p.K != PcbPrim.Scheda && p.K != PcbPrim.Massa) continue;
                Rect r = new Rect(T(p.P[0], p.P[3]), T(p.P[2], p.P[1]));
                if (p.K == PcbPrim.Scheda) ctx.DrawRectangle(Pal.B(Fr4), Pal.P(Fr4Bordo, 1.2), r, 3, 3);
                else ctx.DrawRectangle(Pal.B(Pal.A(90, Rame)), Pal.P(Rame, 1.2), r);
            }
            foreach (PcbPrim p in Prim)
            {
                if (p.K != PcbPrim.Pista) continue;
                Color c = p.Lato == 0 ? Rame : Sotto;
                IPen pen = p.Lato == 0 ? new Pen(Pal.B(c), Math.Max(1.5, p.W * k), lineCap: PenLineCap.Square, lineJoin: PenLineJoin.Miter)
                                       : new Pen(Pal.B(c), Math.Max(1.5, p.W * k), new DashStyle(new double[] { 2, 1.5 }, 0), PenLineCap.Flat, PenLineJoin.Miter);
                for (int i = 0; i + 3 < p.P.Length; i += 2) ctx.DrawLine(pen, T(p.P[i], p.P[i + 1]), T(p.P[i + 2], p.P[i + 3]));
            }
            foreach (PcbPrim p in Prim)
            {
                if (p.K == PcbPrim.Linea) ctx.DrawLine(Pal.P(Pal.Muted, 1), T(p.P[0], p.P[1]), T(p.P[2], p.P[3]));
                else if (p.K == PcbPrim.Freccia)
                {
                    StreamGeometry g = new StreamGeometry();
                    using (StreamGeometryContext gc = g.Open())
                    {
                        gc.BeginFigure(T(p.P[0], p.P[1]), true);
                        gc.LineTo(T(p.P[2], p.P[3])); gc.LineTo(T(p.P[4], p.P[5]));
                        gc.EndFigure(true);
                    }
                    ctx.DrawGeometry(Pal.B(Pal.Muted), null, g);
                }
                else if (p.K == PcbPrim.Alim) { double r = Math.Max(5, p.P[2] * k); ctx.DrawEllipse(Pal.B(Pal.Red), null, T(p.P[0], p.P[1]), r, r); }
                else if (p.K == PcbPrim.Foro) { double r = Math.Max(4, p.P[2] * k); ctx.DrawEllipse(Pal.B(Pal.Panel), Pal.P(Pal.Fore, 1.6), T(p.P[0], p.P[1]), r, r); }
            }
            foreach (PcbPrim p in Prim)
            {
                if (p.K != PcbPrim.Testo) continue;
                double sz = Math.Max(10.5, Math.Min(14, p.W * k * 0.9));
                Point c = T(p.P[0], p.P[1]);
                if (p.Verticale)
                {
                    using (ctx.PushTransform(Matrix.CreateRotation(-Math.PI / 2) * Matrix.CreateTranslation(c.X, c.Y)))
                        Draw.TextC(ctx, p.T, 0, 0, sz, Pal.Fore);
                }
                else Draw.TextC(ctx, p.T, c.X, c.Y, sz, Pal.Fore);
            }
            if (Legenda.Length > 0) Draw.Text(ctx, Legenda, 12, H - 24, 12.5, Pal.Muted);
        }
    }
}
