// YA-NEC 0.4.20 - dima della centina in PDF a scala 1:1 su fogli A4 o A3 da unire (richiesta di IC8TEM del 14/11/2026 18:20, "vorrei calcolare
// anche le dime delle costole"; 18:21 "costole o centine chiamale uguali"). Scritta con SkiaSharp come SchedaPdf: nessun pacchetto in piu'.
// Ogni foglio: area utile 190 x 277 mm in A4, 277 x 400 mm in A3, sovrapposizione di 10 mm con i vicini (crocette di registro sulla griglia comune), righello di 100 mm
// per controllare la scala di stampa, etichetta riga/colonna. Il disegno: faccia z = r^2/4F (dove appoggia la rete), dorso parallelo,
// tacche degli anelli (arcarecci), dell'attacco dei puntoni e ogni 10 mm di raggio (piu' lunghe ogni 50 e 100 mm, con la quota).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SkiaSharp;
using YaNec.Engine;

namespace YaNec
{
    public static class DimaCentinaPdf
    {
        const float PtMm = 72f / 25.4f;
        const float Margine = 10f, Sovr = 10f;

        public static void Scrivi(Stream s, ParabolaInput p, ParabolaResult r)
        {
            CultureInfo IT = CultureInfo.GetCultureInfo("it-IT");
            // 0.4.20 (IC8TEM 14/11/2026 18:39, "volendo anche A3"): fogli A4 o A3 verticali
            float FoglioW = p.DimaFoglioA3 ? 297f : 210f, FoglioH = p.DimaFoglioA3 ? 420f : 297f;
            // 18:40, "anche fogli orizzontali": si scambiano larghezza e altezza
            if (p.DimaFoglioOrizzontale) { float t = FoglioW; FoglioW = FoglioH; FoglioH = t; }
            string formato = (p.DimaFoglioA3 ? "A3" : "A4") + (p.DimaFoglioOrizzontale ? " orizzontali" : " verticali");
            double hM = Math.Max(0, p.DimaAltezza_mm) / 1000.0, r0 = Math.Max(0, p.DimaMozzo_mm) / 1000.0;
            List<double[]> fa, dor;
            Parabola.ProfiloDima(p, r0, hM, 0.002, out fa, out dor);
            double F = p.FsuD * p.Diametro_m, a = p.Diametro_m / 2.0;

            // estensione del disegno in mm (X = raggio, Y = quota z verso il fuoco) con un bordo di 15 mm
            double xmin = double.MaxValue, xmax = double.MinValue, ymin = double.MaxValue, ymax = double.MinValue;
            foreach (List<double[]> l in new[] { fa, dor })
                foreach (double[] q in l) { xmin = Math.Min(xmin, q[0] * 1000); xmax = Math.Max(xmax, q[0] * 1000); ymin = Math.Min(ymin, q[1] * 1000); ymax = Math.Max(ymax, q[1] * 1000); }
            xmin -= 15; xmax += 15; ymin -= 25; ymax += 25;
            float uw = FoglioW - 2 * Margine, uh = FoglioH - 2 * Margine;      // area utile
            float pw = uw - Sovr, ph = uh - Sovr;                               // passo dei fogli
            int nc = Math.Max(1, (int)Math.Ceiling((xmax - xmin - Sovr) / pw)), nr = Math.Max(1, (int)Math.Ceiling((ymax - ymin - Sovr) / ph));

            // coordinate del disegno (mm) -> globali sulla griglia dei fogli: X da sinistra, Y dall'alto (la quota cresce verso l'alto)
            Func<double, float> gx = x => (float)(x - xmin);
            Func<double, float> gy = y => (float)(ymax - y);

            string titolo = string.Format(IT, "YA-NEC {0} - dima della centina: D {1:0} mm, f/D {2:0.000}, F {3:0.0} mm, mozzo r {4:0} mm, centina alta {5:0} mm",
                AppPaths.Version, p.Diametro_m * 1000, p.FsuD, F * 1000, r0 * 1000, hM * 1000);

            using (SKManagedWStream ws = new SKManagedWStream(s))
            using (SKDocument doc = SKDocument.CreatePdf(ws, new SKDocumentPdfMetadata { Title = "Dima della centina", Creator = "YA-NEC", Producer = "YA-NEC (SkiaSharp)" }))
            using (SKPaint faccia = new SKPaint { Color = SKColors.Black, StrokeWidth = 0.5f, IsStroke = true, IsAntialias = true })
            using (SKPaint dorso = new SKPaint { Color = new SKColor(0xFF404040), StrokeWidth = 0.35f, IsStroke = true, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 3f, 1.5f }, 0) })
            using (SKPaint tacca = new SKPaint { Color = SKColors.Black, StrokeWidth = 0.25f, IsStroke = true, IsAntialias = true })
            using (SKPaint rosso = new SKPaint { Color = new SKColor(0xFFC0392B), StrokeWidth = 0.4f, IsStroke = true, IsAntialias = true })
            using (SKPaint blu = new SKPaint { Color = new SKColor(0xFF1F6FB2), StrokeWidth = 0.4f, IsStroke = true, IsAntialias = true })
            using (SKPaint guida = new SKPaint { Color = new SKColor(0xFF9AA59F), StrokeWidth = 0.2f, IsStroke = true, IsAntialias = true })
            using (SKPaint testo = new SKPaint { Color = SKColors.Black, TextSize = 2.6f, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default })
            using (SKPaint testoR = new SKPaint { Color = new SKColor(0xFFC0392B), TextSize = 2.8f, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default })
            using (SKPaint testoB = new SKPaint { Color = new SKColor(0xFF1F6FB2), TextSize = 2.8f, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default })
            using (SKPaint piccolo = new SKPaint { Color = new SKColor(0xFF5D6862), TextSize = 2.3f, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default })
            {
                SKPath pf = new SKPath(), pd = new SKPath();
                for (int i = 0; i < fa.Count; i++) { if (i == 0) pf.MoveTo(gx(fa[i][0] * 1000), gy(fa[i][1] * 1000)); else pf.LineTo(gx(fa[i][0] * 1000), gy(fa[i][1] * 1000)); }
                for (int i = 0; i < dor.Count; i++) { if (i == 0) pd.MoveTo(gx(dor[i][0] * 1000), gy(dor[i][1] * 1000)); else pd.LineTo(gx(dor[i][0] * 1000), gy(dor[i][1] * 1000)); }
                if (dor.Count > 0)
                {
                    pd.MoveTo(gx(fa[0][0] * 1000), gy(fa[0][1] * 1000)); pd.LineTo(gx(dor[0][0] * 1000), gy(dor[0][1] * 1000));
                    pd.MoveTo(gx(fa[fa.Count - 1][0] * 1000), gy(fa[fa.Count - 1][1] * 1000)); pd.LineTo(gx(dor[dor.Count - 1][0] * 1000), gy(dor[dor.Count - 1][1] * 1000));
                }

                // tacca lungo la normale nel punto di raggio rr (mm): da "dentro" (verso il fuoco) a "fuori"
                Action<SKCanvas, double, double, double, SKPaint> taccaN = (c, rr, dentro, fuori, pnt) =>
                {
                    double rm = rr / 1000.0, zz = rm * rm / (4.0 * F), nx = rm / (2.0 * F), l = Math.Sqrt(nx * nx + 1.0);
                    double x1 = rr - dentro * nx / l, y1 = zz * 1000 + dentro / l, x2 = rr + fuori * nx / l, y2 = zz * 1000 - fuori / l;
                    c.DrawLine(gx(x1), gy(y1), gx(x2), gy(y2), pnt);
                };
                Func<double, SKPoint> sopra = rr => { double rm = rr / 1000.0, zz = rm * rm / (4.0 * F); return new SKPoint(gx(rr) - 1f, gy(zz * 1000 + 9)); };

                for (int ri = 0; ri < nr; ri++)
                    for (int ci = 0; ci < nc; ci++)
                    {
                        SKCanvas c = doc.BeginPage(FoglioW * PtMm, FoglioH * PtMm);
                        c.Scale(PtMm);
                        float ox = ci * pw, oy = ri * ph;           // origine globale del foglio
                        c.Save();
                        c.ClipRect(new SKRect(Margine, Margine, Margine + uw, Margine + uh));
                        c.Translate(Margine - ox, Margine - oy);
                        // griglia leggera ogni 10 mm del disegno (aiuta ad allineare) e crocette di registro sulla griglia dei fogli
                        for (double gxm = Math.Floor((ox) / 10) * 10; gxm <= ox + uw; gxm += 10) c.DrawLine((float)gxm, oy, (float)gxm, oy + uh, new SKPaint { Color = new SKColor(0xFFEDF1EE), StrokeWidth = 0.1f });
                        for (double gym = Math.Floor((oy) / 10) * 10; gym <= oy + uh; gym += 10) c.DrawLine(ox, (float)gym, ox + uw, (float)gym, new SKPaint { Color = new SKColor(0xFFEDF1EE), StrokeWidth = 0.1f });
                        for (int k = 0; k <= nc; k++)
                            for (int j = 0; j <= nr; j++)
                            {
                                float cx = k * pw + Sovr / 2f, cy = j * ph + Sovr / 2f;
                                c.DrawLine(cx - 4, cy, cx + 4, cy, guida); c.DrawLine(cx, cy - 4, cx, cy + 4, guida); c.DrawCircle(cx, cy, 2f, guida);
                            }
                        c.DrawPath(pf, faccia);
                        if (dor.Count > 0) c.DrawPath(pd, dorso);
                        // righello sul profilo: tacche ogni 10 mm di raggio, lunghe ogni 50 e 100 mm con la quota
                        for (double rr = Math.Ceiling(r0 * 100) * 10; rr <= a * 1000 + 1e-6; rr += 10)
                        {
                            bool cento = Math.Abs(rr % 100) < 1e-6, cinquanta = Math.Abs(rr % 50) < 1e-6;
                            taccaN(c, rr, cento ? 5 : cinquanta ? 3.5 : 2, 0, tacca);
                            if (cento)
                            {
                                double zz = rr / 1000.0 * rr / 1000.0 / (4.0 * F) * 1000.0;
                                c.DrawText(string.Format(IT, "r {0:0}  z {1:0.0}", rr, zz), gx(rr) + 0.8f, gy(zz + 6), testo);
                            }
                        }
                        foreach (double ra in r.AnelliRaggio_m)
                        {
                            if (ra * 1000 < r0 * 1000 - 1e-6) continue;
                            taccaN(c, ra * 1000, 8, hM * 1000 + 3, rosso);
                            SKPoint q = sopra(ra * 1000);
                            c.DrawText(string.Format(IT, "anello (arcareccio) r {0:0}", ra * 1000), q.X, q.Y - 3, testoR);
                        }
                        double rs = p.PuntoneAttacco_frac * a * 1000;
                        if (p.NPuntoni > 0 && rs >= r0 * 1000)
                        {
                            taccaN(c, rs, 12, hM * 1000 + 3, blu);
                            SKPoint q = sopra(rs);
                            c.DrawText(string.Format(IT, "attacco puntone r {0:0}", rs), q.X, q.Y - 7, testoB);
                        }
                        c.Restore();

                        // cornice dell'area utile, etichetta, righello di controllo della scala (fuori dal ritaglio)
                        c.DrawRect(new SKRect(Margine, Margine, Margine + uw, Margine + uh), guida);
                        c.DrawText(string.Format(IT, "foglio riga {0} colonna {1}  ({2} x {3} fogli; unire sovrapponendo le crocette, sovrapposizione {4:0} mm)", ri + 1, ci + 1, nr, nc, Sovr), Margine, Margine - 3f, piccolo);
                        c.DrawText(titolo, Margine, FoglioH - Margine + 5f, piccolo);
                        float bx = Margine + 4, by = FoglioH - Margine - 6;
                        using (SKPaint bianco = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill })
                            c.DrawRect(new SKRect(bx - 2, by - 6, bx + 104, by + 3), bianco);
                        c.DrawLine(bx, by, bx + 100, by, faccia);
                        for (int k = 0; k <= 10; k++) c.DrawLine(bx + 10 * k, by, bx + 10 * k, by - (k % 5 == 0 ? 3 : 1.6f), tacca);
                        c.DrawText("100 mm: stampa al 100 %, senza \"adatta alla pagina\"", bx, by - 3.6f, piccolo);
                        doc.EndPage();
                    }

                // ultimo foglio: legenda e dati
                SKCanvas cl = doc.BeginPage(FoglioW * PtMm, FoglioH * PtMm);
                cl.Scale(PtMm);
                using (SKPaint tit = new SKPaint { Color = SKColors.Black, TextSize = 5f, IsAntialias = true, FakeBoldText = true, Typeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default })
                using (SKPaint tx = new SKPaint { Color = SKColors.Black, TextSize = 3.4f, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default })
                {
                    float y = 25;
                    cl.DrawText("Dima della centina - legenda", 15, y, tit); y += 10;
                    string[] righe =
                    {
                        string.Format(IT, "Disco: D {0:0} mm, f/D {1:0.000}, fuoco F {2:0.0} mm, profondita' {3:0.0} mm; {4} centine, {5} anelli.", p.Diametro_m * 1000, p.FsuD, F * 1000, r.Profondita_m * 1000, p.NCentine, p.NAnelli),
                        string.Format(IT, "Linea nera piena: faccia della centina, z = r^2 / 4F, dal mozzo (r {0:0} mm) al bordo (r {1:0} mm). Qui appoggia la rete.", r0 * 1000, a * 1000),
                        hM > 0 ? string.Format(IT, "Linea tratteggiata: dorso della centina a {0:0} mm dalla faccia, misurati lungo la normale (centina di altezza costante).", hM * 1000) : "Altezza della centina 0: solo il profilo della faccia.",
                        "Tacche nere: ogni 10 mm di raggio (lunghe ogni 50 mm, con r e z ogni 100 mm). Le misure sono orizzontali dall'asse del disco.",
                        "Tacche rosse: anelli (arcarecci diritti) - fori o staffe per i tratti di tubo tra le centine.",
                        p.NPuntoni > 0 ? string.Format(IT, "Tacca blu: attacco dei puntoni del feed a r {0:0} mm ({1} puntoni).", p.PuntoneAttacco_frac * a * 1000, p.NPuntoni) : "Nessun puntone.",
                        string.Format(IT, "Fogli: {0} righe x {1} colonne di {3}; sovrapposizione {2:0} mm; crocette di registro comuni ai fogli vicini.", nr, nc, Sovr, formato),
                        "Stampa al 100 % (nessun adattamento alla pagina) e controlla il righello di 100 mm in basso su ogni foglio prima di tagliare.",
                        "La lunghezza lungo la curva (per tagliare un tubo prima di curvarlo) e' nella tabella del profilo della centina (colonna arco).",
                    };
                    foreach (string t in righe)
                    {
                        // a capo semplice a circa 95 caratteri
                        string resto = t;
                        while (resto.Length > 0)
                        {
                            int n = resto.Length <= 95 ? resto.Length : Math.Max(1, resto.LastIndexOf(' ', 95));
                            cl.DrawText(resto.Substring(0, n).Trim(), 15, y, tx); y += 5.2f;
                            resto = resto.Substring(n).TrimStart();
                        }
                        y += 2f;
                    }
                    cl.DrawText(titolo, 15, FoglioH - 12, tx);
                }
                doc.EndPage();
                doc.Close();
            }
        }
    }
}
