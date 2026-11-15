// YA-NEC - scheda PDF semplice (A4) scritta con SkiaSharp, la stessa libreria grafica di Avalonia: nessun pacchetto in piu'.
// Usata dal calcolatore "Correzione del boom" (richiesta IC8TEM 13/11/2026 18:20): dati del boom e tabella degli elementi.
using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;

namespace YaNec
{
    public class SchedaPdf
    {
        const float W = 595f, H = 842f, M = 42f;       // A4 in punti, margine
        readonly List<Action<SKCanvas>> pagine = new List<Action<SKCanvas>>();
        readonly List<Action<SKCanvas>> corrente = new List<Action<SKCanvas>>();
        float y = M;
        readonly string titolo, piede;
        int nPagine;

        static SKTypeface Tf(bool bold, bool mono)
        {
            string fam = mono ? "Consolas" : "Arial";
            SKTypeface t = SKTypeface.FromFamilyName(fam, bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            if (t == null && mono) t = SKTypeface.FromFamilyName("DejaVu Sans Mono", bold ? SKFontStyle.Bold : SKFontStyle.Normal);
            return t ?? SKTypeface.Default;
        }

        static SKPaint P(float size, bool bold = false, bool mono = false, uint colore = 0xFF1D2420)
        {
            return new SKPaint { Typeface = Tf(bold, mono), TextSize = size, IsAntialias = true, Color = new SKColor(colore) };
        }

        public SchedaPdf(string titolo, string piede)
        {
            this.titolo = titolo; this.piede = piede;
            Intestazione();
        }

        void Intestazione()
        {
            string t = titolo;
            corrente.Add(c =>
            {
                using (SKPaint p = P(17, true)) c.DrawText(t, M, M + 14, p);
                using (SKPaint l = new SKPaint { Color = new SKColor(0xFF1F6F5C), StrokeWidth = 1.5f }) c.DrawLine(M, M + 22, W - M, M + 22, l);
            });
            y = M + 40;
        }

        void Spazio(float h)
        {
            if (y + h <= H - M - 24) return;
            NuovaPagina();
        }

        void NuovaPagina()
        {
            List<Action<SKCanvas>> pag = new List<Action<SKCanvas>>(corrente);
            pagine.Add(c => { foreach (Action<SKCanvas> a in pag) a(c); });
            corrente.Clear();
            Intestazione();
        }

        public void Sezione(string testo)
        {
            Spazio(30);
            float yy = y + 12;
            corrente.Add(c => { using (SKPaint p = P(12.5f, true, false, 0xFF1F6F5C)) c.DrawText(testo, M, yy, p); });
            y += 22;
        }

        /// <summary>Riga "etichetta: valore" su due colonne.</summary>
        public void Dato(string etichetta, string valore)
        {
            Spazio(16);
            float yy = y + 10;
            corrente.Add(c =>
            {
                using (SKPaint a = P(10, false, false, 0xFF5D6862)) c.DrawText(etichetta, M, yy, a);
                using (SKPaint b = P(10, true)) c.DrawText(valore, M + 190, yy, b);
            });
            y += 15;
        }

        /// <summary>Testo a capo automatico sulla larghezza della pagina.</summary>
        public void Testo(string testo, float size = 9f)
        {
            using (SKPaint mis = P(size))
            {
                List<string> righe = new List<string>();
                foreach (string par in testo.Split('\n'))
                {
                    string riga = "";
                    foreach (string w in par.Split(' '))
                    {
                        string prova = riga.Length == 0 ? w : riga + " " + w;
                        if (mis.MeasureText(prova) > W - 2 * M && riga.Length > 0) { righe.Add(riga); riga = w; }
                        else riga = prova;
                    }
                    righe.Add(riga);
                }
                foreach (string r in righe)
                {
                    Spazio(size + 4);
                    float yy = y + size;
                    string rr = r;
                    corrente.Add(c => { using (SKPaint p = P(size, false, false, 0xFF3A4440)) c.DrawText(rr, M, yy, p); });
                    y += size + 4;
                }
            }
            y += 4;
        }

        /// <summary>Tabella: la prima riga e' l'intestazione; colonne numeriche allineate a destra (tutte tranne la prima).</summary>
        public void Tabella(List<string[]> righe, float[] larghezze)
        {
            float tot = 0; foreach (float l in larghezze) tot += l;
            float k = (W - 2 * M) / tot;
            for (int i = 0; i < righe.Count; i++)
            {
                Spazio(17);
                if (i > 0 && y == M + 40)
                {
                    // nuova pagina: si ripete l'intestazione
                    Riga(righe[0], larghezze, k, true);
                }
                Riga(righe[i], larghezze, k, i == 0);
            }
            y += 6;
        }

        void Riga(string[] r, float[] larghezze, float k, bool testa)
        {
            float yy = y + 11;
            string[] rr = r;
            corrente.Add(c =>
            {
                if (testa) using (SKPaint f = new SKPaint { Color = new SKColor(0xFFE9ECE9) }) c.DrawRect(M, yy - 11, W - 2 * M, 16, f);
                float x = M;
                for (int j = 0; j < rr.Length && j < larghezze.Length; j++)
                {
                    float w = larghezze[j] * k;
                    using (SKPaint p = P(9.5f, testa, !testa && j > 0, testa ? 0xFF5D6862 : 0xFF1D2420))
                    {
                        string s = rr[j] ?? "";
                        if (j == 0) c.DrawText(s, x + 4, yy, p);
                        else c.DrawText(s, x + w - 4 - p.MeasureText(s), yy, p);
                    }
                    x += w;
                }
                using (SKPaint l = new SKPaint { Color = new SKColor(0xFFD5DBD6), StrokeWidth = 0.6f }) c.DrawLine(M, yy + 5, W - M, yy + 5, l);
            });
            y += 17;
        }

        /// <summary>Scrive il PDF nello stream (le pagine numerate "pagina n di N" con il piede).</summary>
        public void Scrivi(Stream s)
        {
            NuovaPagina();
            corrente.Clear();
            nPagine = pagine.Count;
            using (SKManagedWStream ws = new SKManagedWStream(s))
            using (SKDocument doc = SKDocument.CreatePdf(ws, new SKDocumentPdfMetadata { Title = titolo, Creator = "YA-NEC", Producer = "YA-NEC (SkiaSharp)" }))
            {
                for (int i = 0; i < pagine.Count; i++)
                {
                    SKCanvas c = doc.BeginPage(W, H);
                    pagine[i](c);
                    string pp = piede + "   -   pagina " + (i + 1) + " di " + nPagine;
                    using (SKPaint p = P(8, false, false, 0xFF5D6862)) c.DrawText(pp, M, H - M + 8, p);
                    doc.EndPage();
                }
                doc.Close();
            }
        }
    }
}
