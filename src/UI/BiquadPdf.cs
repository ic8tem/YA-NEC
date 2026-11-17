// YA-NEC 1.0.16 - dima del filo della biquad (o doppia biquad) a scala 1:1 in PDF, come le dime delle antenne su PCB e delle centine
// (bozzetto approvato da IC8TEM il 17/11/2026 17:25, pulsante "Dima 1:1 (PDF)"). Foglio A4 verticale, A3 se il disegno non ci sta.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SkiaSharp;
using YaNec.Engine;

namespace YaNec
{
    public static class BiquadPdf
    {
        const float PtMm = 72f / 25.4f;
        static SKTypeface tf;
        static SKPaint Testo(float h, SKColor c, bool b)
        {
            if (tf == null) tf = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default;
            return new SKPaint { Color = c, TextSize = h, IsAntialias = true, Typeface = b ? (SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? tf) : tf };
        }
        static SKPaint Tratto(float w, SKColor c) { return new SKPaint { Color = c, StrokeWidth = w, IsStroke = true, IsAntialias = true }; }

        public static void Scrivi(Stream st, CalcBiquad bq)
        {
            CultureInfo IT = CultureInfo.GetCultureInfo("it-IT");
            double s, g;
            List<double[]> pt = bq.Percorso(out s, out g);
            double x0 = 1e9, x1 = -1e9, y0 = 1e9, y1 = -1e9;
            foreach (double[] p in pt) { x0 = Math.Min(x0, p[0]); x1 = Math.Max(x1, p[0]); y0 = Math.Min(y0, p[1]); y1 = Math.Max(y1, p[1]); }
            double bw = x1 - x0 + 30, bh = y1 - y0 + 30;
            bool a3 = bw > 180 || bh > 160;
            float FW = a3 ? 297f : 210f, FH = a3 ? 420f : 297f;
            if (bw > FW - 30 || bh > FH - 110) throw new InvalidOperationException("il disegno non entra in un foglio A3 a scala 1:1");
            double filoD = bq.P("filo"), f = bq.P("f");
            double lung = 0;
            for (int i = 1; i < pt.Count; i++) lung += Math.Sqrt(Math.Pow(pt[i][0] - pt[i - 1][0], 2) + Math.Pow(pt[i][1] - pt[i - 1][1], 2));
            using (SKManagedWStream ws = new SKManagedWStream(st))
            using (SKDocument doc = SKDocument.CreatePdf(ws, new SKDocumentPdfMetadata { Title = "Dima della biquad", Creator = "YA-NEC", Producer = "YA-NEC (SkiaSharp)" }))
            {
                SKCanvas c = doc.BeginPage(FW * PtMm, FH * PtMm);
                c.Scale(PtMm);
                using (SKPaint t = Testo(3.2f, SKColors.Black, false))
                    c.DrawText("YA-NEC " + AppPaths.Version + " - " + (bq.I("tipo") == 1 ? "doppia biquad" : "biquad") + " " + f.ToString("0.#", IT) + " MHz - scala 1:1, misure in mm, centro del filo", 15, 15, t);
                float ox = 15 + 15 + (float)(-x0), oy = 25 + 15 + (float)y1;
                Func<double, double, SKPoint> T = (u, v) => new SKPoint(ox + (float)u, oy - (float)v);
                using (SKPaint rame = Tratto((float)Math.Max(0.3, filoD), new SKColor(0xB8, 0x6A, 0x2B)))
                    for (int i = 1; i < pt.Count; i++) c.DrawLine(T(pt[i - 1][0], pt[i - 1][1]), T(pt[i][0], pt[i][1]), rame);
                using (SKPaint pieg = new SKPaint { Color = new SKColor(0x1F, 0x6F, 0xB2), IsAntialias = true })
                using (SKPaint t = Testo(2.4f, SKColors.Black, false))
                {
                    for (int i = 1; i < pt.Count - 1; i++)
                    {
                        if (Math.Abs(pt[i][1]) < 1e-6) continue;   // nodi di alimentazione
                        SKPoint q = T(pt[i][0], pt[i][1]);
                        c.DrawCircle(q, 0.9f, pieg);
                        c.DrawText("P" + i, q.X + 1.5f, q.Y - 1f, t);
                    }
                }
                using (SKPaint rosso = new SKPaint { Color = SKColors.Red, IsAntialias = true })
                using (SKPaint t = Testo(2.6f, SKColors.Red, false))
                {
                    c.DrawCircle(T(g / 2, 0), 0.8f, rosso); c.DrawCircle(T(-g / 2, 0), 0.8f, rosso);
                    c.DrawText("centrale", T(g / 2, 0).X + 1.5f, T(g / 2, 0).Y + 3.5f, t);
                    c.DrawText("calza", T(-g / 2, 0).X - 9f, T(-g / 2, 0).Y + 3.5f, t);
                }
                // righello di 100 mm
                float yr = oy + (float)(-y0) + 15;
                using (SKPaint l = Tratto(0.3f, SKColors.Black))
                using (SKPaint t = Testo(2.4f, SKColors.Black, false))
                {
                    c.DrawLine(15, yr, 115, yr, l);
                    for (int i = 0; i <= 10; i++) c.DrawLine(15 + i * 10, yr, 15 + i * 10, yr - (i % 5 == 0 ? 3 : 1.8f), l);
                    c.DrawText("100 mm: controlla questo righello (stampa al 100 %, nessun adattamento alla pagina)", 15, yr + 4.5f, t);
                }
                // misure
                float yt = yr + 12;
                string[][] mis =
                {
                    new[] { "Lato del quadrato (centro filo)", s.ToString("0.0", IT) + " mm" },
                    new[] { "Diametro del filo", filoD.ToString("0.0#", IT) + " mm" },
                    new[] { "Lunghezza del filo (lungo l'asse, senza la parte da saldare)", lung.ToString("0", IT) + " mm" },
                    new[] { "Spazio tra i due nodi di alimentazione", g.ToString("0.0", IT) + " mm" },
                    new[] { "Distanza dal riflettore", bq.P("dist").ToString("0.0", IT) + " mm" },
                    new[] { "Riflettore", bq.P("rw").ToString("0", IT) + " x " + bq.P("rh").ToString("0", IT) + " mm" + (bq.I("bordi") > 0 ? ", bordi di " + bq.P("lip").ToString("0", IT) + " mm" : "") },
                };
                using (SKPaint t = Testo(2.8f, SKColors.Black, false))
                using (SKPaint tb = Testo(2.8f, SKColors.Black, true))
                {
                    foreach (string[] r in mis) { c.DrawText(r[0], 15, yt, t); c.DrawText(r[1], 125, yt, tb); yt += 4.6f; }
                    yt += 2;
                    c.DrawText("Pieghe a 90 gradi nei punti P. Nodi rossi: il centrale del coassiale da un lato, la calza dall'altro (nessun balun).", 15, yt, t);
                    if (bq.I("tipo") == 1) c.DrawText("Doppia biquad: dove le diagonali si incrociano i fili non si toccano (circa 1,5 mm di distanza).", 15, yt + 4.6f, t);
                }
                doc.EndPage();
                doc.Close();
            }
        }
    }
}
