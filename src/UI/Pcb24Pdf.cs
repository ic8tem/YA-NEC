// YA-NEC 0.4.30 - disegno quotato delle antenne su PCB a 2,4 GHz in PDF a scala 1:1 (IC8TEM 14/11/2026 22:42: "si può stampare in scala 1:1
// come le dime delle centine, così diventa una maschera per incidere o fresare la scheda"). Scritto con SkiaSharp come DimaCentinaPdf.
// Pagina 1: disegno diritto con le quote, righello di 100 mm per controllare la scala, tabella delle misure. Pagina 2 (a richiesta): lo
// stesso disegno specchiato, per il trasferimento del toner sul rame. Foglio A4 verticale, A3 se il disegno non ci sta.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SkiaSharp;
using YaNec.Engine;

namespace YaNec
{
    public static class Pcb24Pdf
    {
        const float PtMm = 72f / 25.4f;

        public static void Scrivi(Stream s, Pcb24Spec sp, Pcb24Esito es, bool specchiato)
        {
            CultureInfo IT = CultureInfo.GetCultureInfo("it-IT");
            List<PcbPrim> d = Pcb24.Disegno(sp);
            double[] e = Pcb24.Estensione(d);
            double bw = e[2] - e[0] + 8, bh = e[3] - e[1] + 8;
            bool a3 = bw > 180 || bh > 170;
            float FW = a3 ? 297f : 210f, FH = a3 ? 420f : 297f;
            if (bw > FW - 30 || bh > FH - 120) throw new InvalidOperationException("il disegno non entra in un foglio A3 a scala 1:1");
            List<string[]> mis = Pcb24.Misure(sp);
            string titolo = "YA-NEC " + AppPaths.Version + " - " + Pcb24.Tipi[Math.Max(0, Math.Min(4, sp.Tipo))] + " " +
                sp.FMin.ToString("0.#", IT) + "-" + sp.FMax.ToString("0.#", IT) + " MHz, " +
                (sp.Substrato == 2 ? "in aria" : "er " + sp.Er.ToString("0.##", IT) + ", spessore " + sp.Spessore.ToString("0.##", IT) + " mm") + " - scala 1:1, misure in mm";
            Typeface();
            using (SKManagedWStream ws = new SKManagedWStream(s))
            using (SKDocument doc = SKDocument.CreatePdf(ws, new SKDocumentPdfMetadata { Title = "Antenna su PCB 2,4 GHz", Creator = "YA-NEC", Producer = "YA-NEC (SkiaSharp)" }))
            {
                for (int pag = 0; pag < (specchiato ? 2 : 1); pag++)
                {
                    SKCanvas c = doc.BeginPage(FW * PtMm, FH * PtMm);
                    c.Scale(PtMm);
                    using (SKPaint t = Testo(3.2f, SKColors.Black, false))
                        c.DrawText(titolo + (pag == 1 ? " - SPECCHIATO (trasferimento del toner)" : ""), 15, 15, t);
                    // area del disegno: da x = 15 mm, y = 22 mm
                    float x0 = 15 + 4, y0 = 22 + 4;
                    c.Save();
                    if (pag == 1) { c.Translate(x0 * 2 + (float)(e[2] - e[0]), 0); c.Scale(-1, 1); }
                    Disegna(c, d, e, x0, y0);
                    c.Restore();
                    // righello di 100 mm e note
                    float yr = y0 + (float)(e[3] - e[1]) + 14;
                    using (SKPaint l = Tratto(0.3f, SKColors.Black))
                    using (SKPaint t = Testo(2.4f, SKColors.Black, false))
                    {
                        c.DrawLine(15, yr, 115, yr, l);
                        for (int i = 0; i <= 10; i++) c.DrawLine(15 + i * 10, yr, 15 + i * 10, yr - (i % 5 == 0 ? 3 : 1.8f), l);
                        c.DrawText("100 mm: controlla questo righello prima di incidere (stampa al 100 %, nessun adattamento alla pagina)", 15, yr + 4.5f, t);
                    }
                    // tabella delle misure e dell'esito
                    float yt = yr + 12;
                    using (SKPaint t = Testo(2.8f, SKColors.Black, false))
                    using (SKPaint tb = Testo(2.8f, SKColors.Black, true))
                    {
                        c.DrawText("Misure (asse delle piste; il NEC usa la permittivita' efficace (er + 1)/2)", 15, yt, tb); yt += 5;
                        foreach (string[] r in mis) { c.DrawText(r[0], 18, yt, t); c.DrawText(r[1], 95, yt, t); yt += 4.4f; }
                        if (es != null)
                        {
                            yt += 2;
                            c.DrawText("Esito NEC-2 (indicativo: va misurato col VNA)", 15, yt, tb); yt += 5;
                            c.DrawText(string.Format(IT, "Z al centro banda {0:0.0} {1} j{2:0.0} ohm, ROS {3:0.00} / {4:0.00} / {5:0.00} (inizio / centro / fine), guadagno massimo {6:0.0} dBi",
                                es.Zc.Real, es.Zc.Imaginary < 0 ? "-" : "+", Math.Abs(es.Zc.Imaginary), es.RosMin, es.RosC, es.RosMax, es.Gmax), 18, yt, t); yt += 4.4f;
                            if (!double.IsNaN(es.F2a)) { c.DrawText(string.Format(IT, "banda a ROS 2: {0:0} - {1:0} MHz", es.F2a, es.F2b), 18, yt, t); yt += 4.4f; }
                        }
                        else { c.DrawText("Misure di partenza: premere \"Calcola questa antenna\" per l'accordo col NEC.", 18, yt, t); yt += 4.4f; }
                        c.DrawText("Legenda: rame della pista e del piano di massa; punto rosso = alimentazione; cerchio = corto a massa o foro passante.", 15, yt + 3, t);
                    }
                    doc.EndPage();
                }
                doc.Close();
            }
        }

        static SKTypeface tf;
        static void Typeface() { if (tf == null) tf = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default; }
        static SKPaint Testo(float h, SKColor c, bool grassetto)
        {
            return new SKPaint { Color = c, TextSize = h, IsAntialias = true, Typeface = grassetto ? (SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? tf) : tf };
        }
        static SKPaint Tratto(float w, SKColor c) { return new SKPaint { Color = c, StrokeWidth = w, IsStroke = true, IsAntialias = true }; }

        static void Disegna(SKCanvas c, List<PcbPrim> d, double[] e, float x0, float y0)
        {
            Func<double, double, SKPoint> T = (u, v) => new SKPoint(x0 + (float)(u - e[0]), y0 + (float)(e[3] - v));
            SKColor rame = new SKColor(0xB8, 0x6A, 0x2B), sotto = new SKColor(0x1F, 0x6F, 0xB2), fr4 = new SKColor(0xE4, 0xEF, 0xD8), fr4b = new SKColor(0x8F, 0xAF, 0x7F);
            foreach (PcbPrim p in d)
            {
                if (p.K != PcbPrim.Scheda && p.K != PcbPrim.Massa) continue;
                SKPoint a = T(p.P[0], p.P[3]), b = T(p.P[2], p.P[1]);
                SKRect r = new SKRect(a.X, a.Y, b.X, b.Y);
                if (p.K == PcbPrim.Scheda)
                {
                    using (SKPaint f = new SKPaint { Color = fr4, IsAntialias = true }) c.DrawRect(r, f);
                    using (SKPaint l = Tratto(0.25f, fr4b)) c.DrawRect(r, l);
                }
                else
                {
                    using (SKPaint f = new SKPaint { Color = rame.WithAlpha(120), IsAntialias = true }) c.DrawRect(r, f);
                    using (SKPaint l = Tratto(0.25f, rame)) c.DrawRect(r, l);
                }
            }
            foreach (PcbPrim p in d)
            {
                if (p.K != PcbPrim.Pista) continue;
                using (SKPaint l = new SKPaint { Color = p.Lato == 0 ? rame : sotto, StrokeWidth = (float)p.W, IsStroke = true, IsAntialias = true, StrokeCap = SKStrokeCap.Square, StrokeJoin = SKStrokeJoin.Miter })
                {
                    if (p.Lato == 1) l.PathEffect = SKPathEffect.CreateDash(new[] { 1.2f, 0.8f }, 0);
                    SKPath path = new SKPath();
                    path.MoveTo(T(p.P[0], p.P[1]));
                    for (int i = 2; i + 1 < p.P.Length; i += 2) path.LineTo(T(p.P[i], p.P[i + 1]));
                    c.DrawPath(path, l);
                }
            }
            using (SKPaint q = Tratto(0.15f, new SKColor(0x40, 0x40, 0x40)))
            using (SKPaint qf = new SKPaint { Color = new SKColor(0x40, 0x40, 0x40), IsAntialias = true })
            using (SKPaint rosso = new SKPaint { Color = new SKColor(0xC0, 0x39, 0x2B), IsAntialias = true })
            using (SKPaint foro = Tratto(0.2f, SKColors.Black))
            using (SKPaint bianco = new SKPaint { Color = SKColors.White, IsAntialias = true })
                foreach (PcbPrim p in d)
                {
                    if (p.K == PcbPrim.Linea) c.DrawLine(T(p.P[0], p.P[1]), T(p.P[2], p.P[3]), q);
                    else if (p.K == PcbPrim.Freccia)
                    {
                        SKPath tri = new SKPath();
                        tri.MoveTo(T(p.P[0], p.P[1])); tri.LineTo(T(p.P[2], p.P[3])); tri.LineTo(T(p.P[4], p.P[5])); tri.Close();
                        c.DrawPath(tri, qf);
                    }
                    else if (p.K == PcbPrim.Alim) c.DrawCircle(T(p.P[0], p.P[1]), (float)p.P[2], rosso);
                    else if (p.K == PcbPrim.Foro) { c.DrawCircle(T(p.P[0], p.P[1]), (float)p.P[2], bianco); c.DrawCircle(T(p.P[0], p.P[1]), (float)p.P[2], foro); }
                }
            foreach (PcbPrim p in d)
            {
                if (p.K != PcbPrim.Testo) continue;
                using (SKPaint t = Testo((float)Math.Max(1.4, Math.Min(2.4, p.W)), SKColors.Black, false))
                {
                    t.TextAlign = SKTextAlign.Center;
                    SKPoint pt = T(p.P[0], p.P[1]);
                    c.Save();
                    c.Translate(pt.X, pt.Y);
                    if (p.Verticale) c.RotateDegrees(-90);
                    c.DrawText(p.T, 0, t.TextSize * 0.35f, t);
                    c.Restore();
                }
            }
        }
    }
}
