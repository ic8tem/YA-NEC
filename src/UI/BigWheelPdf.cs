// YA-NEC 1.0.17 - Big Wheel: dima di piegatura del petalo a scala 1:1 su piu' fogli A4 (sovrapposizione 10 mm, crocette di registro,
// righello di 100 mm) e dettaglio del mozzo (i due livelli, i triangoli, lo stub), bozzetto approvato da IC8TEM il 17/11/2026 18:08.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SkiaSharp;
using YaNec.Engine;

namespace YaNec
{
    public static class BigWheelPdf
    {
        const float PtMm = 72f / 25.4f;
        static SKTypeface tf;
        static SKPaint Testo(float h, SKColor c, bool b)
        {
            if (tf == null) tf = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default;
            return new SKPaint { Color = c, TextSize = h, IsAntialias = true, Typeface = b ? (SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? tf) : tf };
        }
        static SKPaint Tratto(float w, SKColor c) { return new SKPaint { Color = c, StrokeWidth = w, IsStroke = true, IsAntialias = true }; }
        static readonly SKColor Blu = new SKColor(0x1F, 0x6F, 0xB2), Rosso = new SKColor(0xC0, 0x39, 0x2B);

        /// <summary>Dima del petalo a scala 1:1 su fogli A4 verticali da unire (area utile 190 x 277 mm, 10 mm di sovrapposizione).</summary>
        public static void Dima(Stream st, CalcBigWheel bw)
        {
            CultureInfo IT = CultureInfo.GetCultureInfo("it-IT");
            double H;
            List<double[]> p = CalcBigWheel.Petalo(bw.P("lp"), bw.P("ap"), bw.P("rc"), bw.P("r0"), out H);
            double tubo = bw.P("tubo");
            double x0 = 1e9, x1 = -1e9, y0 = 1e9, y1 = -1e9;
            foreach (double[] q in p) { x0 = Math.Min(x0, q[0]); x1 = Math.Max(x1, q[0]); y0 = Math.Min(y0, q[1]); y1 = Math.Max(y1, q[1]); }
            y0 = Math.Min(y0, -25); x0 -= 15; x1 += 15; y1 += 15;
            const float UW = 190f, UH = 277f, OV = 10f, M = 10f;
            int nc = (int)Math.Ceiling((x1 - x0 - OV) / (UW - OV)), nr = (int)Math.Ceiling((y1 - y0 - OV) / (UH - OV));
            nc = Math.Max(1, nc); nr = Math.Max(1, nr);
            using (SKManagedWStream ws = new SKManagedWStream(st))
            using (SKDocument doc = SKDocument.CreatePdf(ws, new SKDocumentPdfMetadata { Title = "Dima del petalo della Big Wheel", Creator = "YA-NEC", Producer = "YA-NEC (SkiaSharp)" }))
            {
                for (int r = 0; r < nr; r++)
                    for (int c = 0; c < nc; c++)
                    {
                        SKCanvas cv = doc.BeginPage(210 * PtMm, 297 * PtMm);
                        cv.Scale(PtMm);
                        // origine del foglio nel piano del petalo (mm): x crescente a destra, y crescente in alto
                        double ox = x0 + c * (UW - OV), oy = y1 - r * (UH - OV);
                        Func<double, double, SKPoint> T = (u, v) => new SKPoint(M + (float)(u - ox), M + (float)(oy - v));
                        cv.Save();
                        cv.ClipRect(new SKRect(M, M, M + UW, M + UH));
                        using (SKPaint asse = Tratto((float)Math.Max(0.4, tubo), new SKColor(0xB8, 0x6A, 0x2B, 0x80)))
                        using (SKPaint lin = Tratto(0.3f, SKColors.Black))
                        {
                            for (int i = 1; i < p.Count; i++) { cv.DrawLine(T(p[i - 1][0], p[i - 1][1]), T(p[i][0], p[i][1]), asse); cv.DrawLine(T(p[i - 1][0], p[i - 1][1]), T(p[i][0], p[i][1]), lin); }
                            // mozzo e asse del petalo
                            using (SKPaint tr = Tratto(0.25f, SKColors.Gray)) { cv.DrawLine(T(0, -20), T(0, H + 10), tr); cv.DrawCircle(T(0, 0), 2f, tr); }
                        }
                        using (SKPaint t = Testo(3f, SKColors.Black, false))
                        {
                            cv.DrawText("mozzo", T(3, -6).X, T(3, -6).Y, t);
                            cv.DrawText("cambio di livello (meta' del lato esterno)", T(-60, H + 6).X, T(-60, H + 6).Y, t);
                        }
                        // crocette di registro ogni 50 mm del piano
                        using (SKPaint cr = Tratto(0.2f, SKColors.Gray))
                            for (double gx = Math.Floor(x0 / 50) * 50; gx <= x1; gx += 50)
                                for (double gy = Math.Floor(y0 / 50) * 50; gy <= y1; gy += 50)
                                { SKPoint q = T(gx, gy); cv.DrawLine(q.X - 2, q.Y, q.X + 2, q.Y, cr); cv.DrawLine(q.X, q.Y - 2, q.X, q.Y + 2, cr); }
                        cv.Restore();
                        using (SKPaint bordo = Tratto(0.2f, SKColors.LightGray)) cv.DrawRect(new SKRect(M, M, M + UW, M + UH), bordo);
                        using (SKPaint t = Testo(2.6f, SKColors.Black, false))
                        {
                            cv.DrawText("YA-NEC " + AppPaths.Version + " - Big Wheel " + bw.P("f").ToString("0.#", IT) + " MHz, dima del petalo 1:1 - foglio riga " + (r + 1) + " colonna " + (c + 1) + " di " + nr + " x " + nc +
                                " (sovrapposizione 10 mm, crocette ogni 50 mm)", M, 297 - 12, t);
                            cv.DrawText("Petalo " + bw.P("lp").ToString("0", IT) + " mm sviluppati, bracci a " + bw.P("ap").ToString("0", IT) + " gradi, curve di " + bw.P("rc").ToString("0", IT) +
                                " mm sull'asse, " + H.ToString("0", IT) + " mm dal mozzo al lato esterno. Tracciato = asse del tubo da " + tubo.ToString("0.#", IT) + " mm.", M, 297 - 8, t);
                        }
                        if (r == nr - 1 && c == 0)
                            using (SKPaint l = Tratto(0.3f, SKColors.Black))
                            using (SKPaint t = Testo(2.4f, SKColors.Black, false))
                            {
                                float yr = 297 - 18;
                                cv.DrawLine(M + 5, yr, M + 105, yr, l);
                                for (int i = 0; i <= 10; i++) cv.DrawLine(M + 5 + i * 10, yr, M + 5 + i * 10, yr - (i % 5 == 0 ? 3 : 1.8f), l);
                                cv.DrawText("100 mm: controlla questo righello (stampa al 100 %)", M + 108, yr, t);
                            }
                        doc.EndPage();
                    }
                doc.Close();
            }
        }

        /// <summary>Dettaglio del mozzo: vista dall'alto dei bracci sui due livelli e vista di lato con i triangoli, lo stub e il connettore.</summary>
        public static void Mozzo(Stream st, CalcBigWheel bw)
        {
            CultureInfo IT = CultureInfo.GetCultureInfo("it-IT");
            double ap = bw.P("ap"), r0 = bw.P("r0"), dz = bw.P("dz"), ls = bw.P("ls"), lw = bw.P("lw"), tubo = bw.P("tubo");
            using (SKManagedWStream ws = new SKManagedWStream(st))
            using (SKDocument doc = SKDocument.CreatePdf(ws, new SKDocumentPdfMetadata { Title = "Mozzo della Big Wheel", Creator = "YA-NEC", Producer = "YA-NEC (SkiaSharp)" }))
            {
                SKCanvas c = doc.BeginPage(210 * PtMm, 297 * PtMm);
                c.Scale(PtMm);
                using (SKPaint t = Testo(3.4f, SKColors.Black, true)) c.DrawText("YA-NEC " + AppPaths.Version + " - Big Wheel " + bw.P("f").ToString("0.#", IT) + " MHz: mozzo e livelli (misure in mm)", 15, 15, t);
                // vista dall'alto, scala 2:1, centro a (105, 85)
                float cx = 105, cy = 85, k = 2f, lb = 35;
                using (SKPaint t = Testo(3f, SKColors.Black, false)) c.DrawText("Vista dall'alto (scala 2:1): bracci rossi sopra (centrale), blu sotto (calza)", 15, 25, t);
                for (int i = 0; i < 3; i++)
                {
                    double th = (90 + 120 * i) * Math.PI / 180;
                    double au = th + ap / 2 * Math.PI / 180, ad = th - ap / 2 * Math.PI / 180;
                    using (SKPaint su = Tratto((float)(tubo * k * 0.4), Rosso))
                        c.DrawLine(cx + (float)(Math.Cos(au) * r0 * k), cy - (float)(Math.Sin(au) * r0 * k), cx + (float)(Math.Cos(au) * (r0 + lb) * k), cy - (float)(Math.Sin(au) * (r0 + lb) * k), su);
                    using (SKPaint gi = Tratto((float)(tubo * k * 0.4), Blu))
                        c.DrawLine(cx + (float)(Math.Cos(ad) * r0 * k), cy - (float)(Math.Sin(ad) * r0 * k), cx + (float)(Math.Cos(ad) * (r0 + lb) * k), cy - (float)(Math.Sin(ad) * (r0 + lb) * k), gi);
                }
                using (SKPaint tri = Tratto(0.5f, SKColors.Gray))
                {
                    SKPath pa = new SKPath();
                    for (int i = 0; i < 3; i++) { double th = (90 + 120 * i) * Math.PI / 180; float px = cx + (float)(Math.Cos(th + Math.PI / 3) * r0 * 1.15 * k), py = cy - (float)(Math.Sin(th + Math.PI / 3) * r0 * 1.15 * k); if (i == 0) pa.MoveTo(px, py); else pa.LineTo(px, py); }
                    pa.Close(); c.DrawPath(pa, tri);
                }
                using (SKPaint t = Testo(2.8f, SKColors.Black, false))
                {
                    c.DrawText(ap.ToString("0", IT) + " gradi tra i bracci di un petalo; " + (120 - ap).ToString("0", IT) + " gradi tra il braccio rosso di un petalo e il blu del vicino (la linea di λ/4).", 15, 150, t);
                    c.DrawText("Bracci fissati ai triangoli a " + r0.ToString("0", IT) + " mm dal centro; tutti i petali nello stesso verso.", 15, 155, t);
                }
                // vista di lato, scala 2:1
                float sy = 200, sx = 60;
                using (SKPaint t = Testo(3f, SKColors.Black, false)) c.DrawText("Vista di lato (scala 2:1)", 15, 170, t);
                using (SKPaint ros = Tratto(3f, Rosso)) c.DrawLine(sx, sy, sx + 90, sy, ros);
                using (SKPaint blu = Tratto(3f, Blu)) c.DrawLine(sx, sy + (float)(dz * k), sx + 90, sy + (float)(dz * k), blu);
                using (SKPaint q = Tratto(0.3f, SKColors.Black))
                using (SKPaint t = Testo(2.8f, SKColors.Black, false))
                {
                    c.DrawLine(sx - 8, sy, sx - 8, sy + (float)(dz * k), q);
                    c.DrawText(dz.ToString("0", IT) + " mm", sx - 30, sy + (float)(dz * k / 2) + 1, t);
                    c.DrawText("triangolo di sopra: centrale del connettore", sx + 95, sy + 1, t);
                    c.DrawText("triangolo di sotto: calza (massa, staffa del palo)", sx + 95, sy + (float)(dz * k) + 1, t);
                }
                double l1 = Math.Max(1, (ls - dz) / 2);
                using (SKPaint vi = Tratto((float)Math.Max(1, lw * k * 0.15), new SKColor(0x8E, 0x44, 0xAD)))
                {
                    float xs = sx + 40;
                    c.DrawLine(xs, sy, xs, sy - (float)(l1 * k * 0.5), vi);
                    c.DrawLine(xs, sy - (float)(l1 * k * 0.5), xs + 12, sy - (float)(l1 * k * 0.5), vi);
                    c.DrawLine(xs + 12, sy - (float)(l1 * k * 0.5), xs + 12, sy + (float)(dz * k), vi);
                }
                using (SKPaint t = Testo(2.8f, SKColors.Black, false))
                {
                    c.DrawText("stub a U: " + ls.ToString("0", IT) + " mm sviluppati, striscia da " + lw.ToString("0", IT) + " mm (schema, non in scala)", sx + 20, sy - (float)(l1 * k * 0.5) - 3, t);
                    float yt = sy + (float)(dz * k) + 20;
                    c.DrawText("1. Connettore sotto il triangolo di massa, il centrale passa al triangolo di sopra.", 15, yt, t);
                    c.DrawText("2. Lo stub collega i due triangoli in parallelo al connettore: si regola la lunghezza per il ROS (F1BKM: 152 mm su un'auto o in stack).", 15, yt + 5, t);
                    c.DrawText("3. Isolanti tra i livelli (F1BKM: pila di piastre di polistirene da 5 mm): la distanza dz va tenuta uguale per tutti i bracci.", 15, yt + 10, t);
                    c.DrawText("4. Il cambio di livello di ogni petalo sta a meta' del lato esterno (manicotto di F1BKM).", 15, yt + 15, t);
                }
                doc.EndPage();
                doc.Close();
            }
        }
    }
}
