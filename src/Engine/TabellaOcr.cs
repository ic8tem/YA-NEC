// YA-NEC 1.9.0 - Importa tabella da immagine (OCR): bozzetto approvato da IC8TEM il 18/11/2026 21:30
// (doc/bozzetti_approvati/Tabella_da_immagine_OCR_18-11-2026.html), costruito su sua richiesta del 28/11/2026 06:15
// («Compila e collauda immediatamente»). Legge le tabelle delle misure delle Yagi (e di quad, verticali e fili liberi) dagli articoli:
//  - sorgente: immagine, PDF (pagina scelta; con il testo vero si legge senza OCR), incolla, cattura dello schermo; raddrizzamento
//    automatico (profilo delle righe: l'angolo che rende le righe di testo piu' nette) o a mano; contrasto normale o forte (foto);
//  - OCR: Tesseract in modalita' blocco (psm 6: prova del 28/11 su una tabella di 10 righe ruotata di 1,6 gradi e ridotta al 60 %,
//    tutte le cifre giuste; la modalita' sparsa perdeva le cifre isolate), con i dati della lingua scelta tra otto (tessdata_fast);
//  - tabella: righe dalle posizioni verticali delle parole, celle dalle distanze tra le parole, colonne dalla sovrapposizione delle celle;
//    cifre incerte (O/0, l/1, I/1, S/5, B/8) corrette e lasciate gialle con il testo letto; righe con almeno due numeri = corpo;
//  - controlli (rossi, non bloccano): posizioni crescenti, lunghezze entro lambda/2 +/- 15 % (Yagi), direttori che si allungano.
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace YaNec.Engine
{
    public class CellaLetta
    {
        public string Testo = "";
        public bool Incerta;            // cifra corretta dall'OCR o letta con poca sicurezza
        public string Letto;            // testo letto dall'OCR prima della correzione
        public bool Rossa;              // controllo di plausibilita'
        public string Nota;
        public List<ParolaLetta> Fonti = new List<ParolaLetta>();
    }

    public class TabellaLetta
    {
        public string Titolo = "";
        public List<string> Intestazioni = new List<string>();
        public List<List<CellaLetta>> Righe = new List<List<CellaLetta>>();
        public int Unita;               // indice di TabellaOcr.Unita
        public bool Virgola;
        public int Colonne { get { int n = Intestazioni.Count; foreach (var r in Righe) n = Math.Max(n, r.Count); return n; } }
        public void Pareggia()
        {
            int n = Colonne;
            while (Intestazioni.Count < n) Intestazioni.Add("");
            foreach (var r in Righe) while (r.Count < n) r.Add(new CellaLetta());
        }
    }

    public class ElementoTab { public string Nome = ""; public double Pos, Lung, Diam; public double[] Fili; public int Segmenti; }

    public static class TabellaOcr
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        public static readonly string[] Unita = { "mm", "cm", "m", "pollici", "lambda" };
        public static readonly string[] Lingue = { "italiano", "inglese", "tedesco", "russo", "giapponese", "francese", "portoghese", "spagnolo" };
        public static readonly string[] CodiciLingua = { "ita", "eng", "deu", "rus", "jpn", "fra", "por", "spa" };
        public static readonly string[] Ruoli = { "ignora", "nome", "posizione sul boom", "distanza dal precedente", "lunghezza totale", "semielemento",
            "diametro", "raggio", "X1", "Y1", "Z1", "X2", "Y2", "Z2", "segmenti" };
        public const int RIgnora = 0, RNome = 1, RPos = 2, RDist = 3, RLung = 4, RSemi = 5, RDiam = 6, RRaggio = 7, RX1 = 8, RSeg = 14;
        public static readonly string[] Tipi = { "Yagi (elementi lungo il boom)", "Quad / loop", "Verticale con radiali", "Fili liberi (X1 Y1 Z1 X2 Y2 Z2)" };

        public static double MetriPer(int unita, double fMHz)
        {
            switch (unita) { case 0: return 0.001; case 1: return 0.01; case 2: return 1; case 3: return 0.0254; default: return 299.792458 / Math.Max(0.01, fMHz); }
        }

        // ------------------------------------------------------------------------------------------ numeri e cifre incerte
        /// <summary>Corregge le lettere confuse con le cifre (O/0, l/1, I/1, |/1, S/5, B/8) quando il resto e' un numero.</summary>
        public static string Cifre(string t, out bool corretta)
        {
            corretta = false;
            if (string.IsNullOrEmpty(t)) return t;
            string s = t.Trim().Replace('−', '-').Replace('–', '-');
            if (Regex.IsMatch(s, @"^-?[0-9.,]+$")) return s;
            if (Regex.IsMatch(s, @"^-?[0-9.,OolI|SB]+$") && Regex.IsMatch(s, @"[0-9]") || Regex.IsMatch(s, @"^[Oo]$"))
            {
                corretta = true;
                return s.Replace('O', '0').Replace('o', '0').Replace('l', '1').Replace('I', '1').Replace('|', '1').Replace('S', '5').Replace('B', '8');
            }
            // nomi degli elementi: DI = D1, DS = D5, D1O = D10
            var m = Regex.Match(s, @"^([A-Za-z]{1,2})([0-9OolI|S]+)$");
            if (m.Success && Regex.IsMatch(m.Groups[2].Value, "[OolI|S]") && m.Groups[1].Value.ToUpperInvariant().StartsWith("D"))
            {
                corretta = true;
                return m.Groups[1].Value + m.Groups[2].Value.Replace('O', '0').Replace('o', '0').Replace('l', '1').Replace('I', '1').Replace('|', '1').Replace('S', '5');
            }
            return s;
        }

        public static bool Valore(string t, bool virgola, out double v)
        {
            v = 0;
            if (string.IsNullOrWhiteSpace(t)) return false;
            string s = t.Trim().TrimEnd('"', '”', '″', '*').Replace(" ", "");
            s = Regex.Replace(s, "(mm|cm|m|in|λ)$", "");
            if (virgola) s = s.Replace(".", "").Replace(',', '.');
            else s = s.Replace(",", ".");
            // frazioni dei pollici: 35 15/16
            var fr = Regex.Match(t.Trim(), @"^(\d+)\s+(\d+)/(\d+)""?$");
            if (fr.Success) { v = double.Parse(fr.Groups[1].Value, CI) + double.Parse(fr.Groups[2].Value, CI) / double.Parse(fr.Groups[3].Value, CI); return true; }
            return double.TryParse(s, NumberStyles.Float, CI, out v);
        }

        // ------------------------------------------------------------------------------------------ riconoscimento
        class Riga { public double Cy; public List<ParolaLetta> P = new List<ParolaLetta>(); public List<CellaLetta> Celle = new List<CellaLetta>(); public List<double[]> Spazi = new List<double[]>(); }

        /// <summary>Tabella dalle parole di una pagina (OCR o testo del PDF).</summary>
        public static TabellaLetta Riconosci(PaginaLetta pg)
        {
            var t = new TabellaLetta();
            var parole = pg.Parole.Where(p => !string.IsNullOrWhiteSpace(p.Testo) && Regex.IsMatch(p.Testo, @"[0-9A-Za-z\u00C0-\u024F\u0400-\u04FF\u3040-\u30FF\u4E00-\u9FFFλØø]")).OrderBy(p => p.Cy).ToList();
            if (parole.Count == 0) return t;
            double hMed = parole.Select(p => p.H).OrderBy(h => h).ElementAt(parole.Count / 2);
            // righe
            var righe = new List<Riga>();
            foreach (var p in parole)
            {
                var r = righe.LastOrDefault(q => Math.Abs(q.Cy - p.Cy) < 0.5 * hMed);
                if (r == null) { r = new Riga { Cy = p.Cy }; righe.Add(r); }
                r.P.Add(p); r.Cy = r.P.Average(q => q.Cy);
            }
            // celle: parole vicine nella stessa riga (spazio minore di 0,9 altezze)
            foreach (var r in righe)
            {
                CellaLetta c = null; double xFine = 0, xIni = 0;
                foreach (var p in r.P.OrderBy(q => q.X))
                {
                    if (c == null || p.X - xFine > 0.9 * hMed)
                    {
                        if (c != null) r.Spazi.Add(new[] { xIni, xFine });
                        c = new CellaLetta(); r.Celle.Add(c); xIni = p.X;
                        c.Testo = p.Testo;
                    }
                    else c.Testo += " " + p.Testo;
                    c.Fonti.Add(p); xFine = Math.Max(xFine, p.X + p.W);
                    if (p.Conf < 70) c.Incerta = true;
                }
                if (c != null) r.Spazi.Add(new[] { xIni, xFine });
            }
            // corpo: la serie piu' lunga di righe con almeno due numeri
            Func<Riga, int> numeri = r => r.Celle.Count(c => { bool k; double v; return Valore(Cifre(c.Testo, out k), false, out v) || Valore(Cifre(c.Testo, out k), true, out v); });
            int best0 = -1, bestN = 0;
            for (int i = 0; i < righe.Count; i++)
            {
                if (numeri(righe[i]) < 2) continue;
                int j = i;
                while (j + 1 < righe.Count && numeri(righe[j + 1]) >= 2) j++;
                if (j - i + 1 > bestN) { bestN = j - i + 1; best0 = i; }
                i = j;
            }
            if (best0 < 0) return t;
            var corpo = righe.GetRange(best0, bestN);
            Riga intest = null;
            if (best0 > 0 && righe[best0 - 1].Celle.Count >= 2 && numeri(righe[best0 - 1]) < righe[best0 - 1].Celle.Count) intest = righe[best0 - 1];
            int titoloFino = (intest != null ? best0 - 1 : best0);
            t.Titolo = string.Join(" ", righe.Take(titoloFino).Skip(Math.Max(0, titoloFino - 2)).Select(r => string.Join(" ", r.P.OrderBy(p => p.X).Select(p => p.Testo)))).Trim();

            // colonne: intervalli orizzontali delle celle che si sovrappongono
            var tutte = new List<Riga>(corpo); if (intest != null) tutte.Insert(0, intest);
            var col = new List<double[]>();   // [sinistra, destra]
            foreach (var r in corpo)
                for (int k = 0; k < r.Celle.Count; k++)
                {
                    double a = r.Spazi[k][0], b = r.Spazi[k][1];
                    int ci = col.FindIndex(c => a < c[1] + 0.3 * hMed && b > c[0] - 0.3 * hMed);
                    if (ci < 0) col.Add(new[] { a, b }); else { col[ci][0] = Math.Min(col[ci][0], a); col[ci][1] = Math.Max(col[ci][1], b); }
                }
            // colonne che dopo l'allargamento si sovrappongono: una sola
            col = col.OrderBy(c => c[0]).ToList();
            for (int k = 1; k < col.Count; k++)
                if (col[k][0] < col[k - 1][1]) { col[k - 1][1] = Math.Max(col[k - 1][1], col[k][1]); col.RemoveAt(k); k--; }
            Func<double, double, int> colonna = (a, b) =>
            {
                int bi = 0; double bo = double.MinValue;
                for (int k = 0; k < col.Count; k++)
                {
                    double ov = Math.Min(b, col[k][1]) - Math.Max(a, col[k][0]);
                    if (ov > bo) { bo = ov; bi = k; }
                }
                return bi;
            };
            if (intest != null)
            {
                for (int k = 0; k < col.Count; k++) t.Intestazioni.Add("");
                for (int k = 0; k < intest.Celle.Count; k++)
                {
                    int ci = colonna(intest.Spazi[k][0], intest.Spazi[k][1]);
                    t.Intestazioni[ci] = (t.Intestazioni[ci] + " " + intest.Celle[k].Testo).Trim();
                }
            }
            foreach (var r in corpo)
            {
                var riga = new List<CellaLetta>();
                for (int k = 0; k < col.Count; k++) riga.Add(new CellaLetta());
                for (int k = 0; k < r.Celle.Count; k++)
                {
                    int ci = colonna(r.Spazi[k][0], r.Spazi[k][1]);
                    var c = r.Celle[k];
                    if (riga[ci].Testo.Length > 0) { riga[ci].Testo += " " + c.Testo; riga[ci].Fonti.AddRange(c.Fonti); riga[ci].Incerta |= c.Incerta; }
                    else riga[ci] = c;
                }
                t.Righe.Add(riga);
            }
            t.Pareggia();
            // cifre incerte
            foreach (var r in t.Righe)
                foreach (var c in r)
                {
                    bool k; string n = Cifre(c.Testo, out k);
                    if (k) { c.Letto = c.Testo; c.Testo = n; c.Incerta = true; }
                }
            // virgola o punto, unita' dal titolo e dalle intestazioni
            int virg = 0, punti = 0;
            foreach (var r in t.Righe) foreach (var c in r) { if (Regex.IsMatch(c.Testo, @"^\d+,\d{1,3}$")) virg++; if (Regex.IsMatch(c.Testo, @"^\d*\.\d+$")) punti++; }
            t.Virgola = virg > punti;
            t.Unita = UnitaDalTesto(t.Titolo + " " + string.Join(" ", t.Intestazioni), t);
            return t;
        }

        public static int UnitaDalTesto(string s, TabellaLetta t)
        {
            string l = " " + s.ToLowerInvariant() + " ";
            if (Regex.IsMatch(l, @"\bmm\b")) return 0;
            if (Regex.IsMatch(l, @"\bcm\b")) return 1;
            if (Regex.IsMatch(l, @"\binch|\binches\b|pollici|\bin\.|"" ?\)|\(in\)")) return 3;
            if (Regex.IsMatch(l, @"λ|lambda|\bwl\b|wavelength")) return 4;
            if (Regex.IsMatch(l, @"\(m\)|\bin m\b|metri\b|meters\b")) return 2;
            if (t != null && t.Righe.Any(r => r.Any(c => c.Testo.Contains("\"")))) return 3;
            return 0;
        }

        /// <summary>Ruoli delle colonne: dalle intestazioni, poi dai valori.</summary>
        public static List<int> RuoliProbabili(TabellaLetta t, double fMHz)
        {
            int n = t.Colonne;
            var r = Enumerable.Repeat(-1, n).ToList();
            for (int k = 0; k < n; k++)
            {
                string h = (k < t.Intestazioni.Count ? t.Intestazioni[k] : "").ToLowerInvariant().Trim();
                if (h.Length == 0) continue;
                if (Regex.IsMatch(h, @"^(x1|y1|z1|x2|y2|z2)\b")) { r[k] = RX1 + Array.IndexOf(new[] { "x1", "y1", "z1", "x2", "y2", "z2" }, h.Substring(0, 2)); continue; }
                if (Regex.IsMatch(h, @"seg")) { r[k] = RSeg; continue; }
                if (Regex.IsMatch(h, @"semi|half|1/2|meta")) { r[k] = RSemi; continue; }
                if (Regex.IsMatch(h, @"spac|spaz|dist|interasse|gap|abstand")) { r[k] = RDist; continue; }
                if (Regex.IsMatch(h, @"pos|boom|^x$|from|da r|lage")) { r[k] = RPos; continue; }
                if (Regex.IsMatch(h, @"lungh|leng|^l\b|^l\.|perim|länge|lange|largo|longueur")) { r[k] = RLung; continue; }
                if (Regex.IsMatch(h, @"raggio|radius|^r\b|^r\.")) { r[k] = RRaggio; continue; }
                if (Regex.IsMatch(h, @"ø|diam|^d$|^d\.|dia\b|tube|tubo|durchm")) { r[k] = RDiam; continue; }
                if (Regex.IsMatch(h, @"^(el|elem|elemento|element|n|nr|no|#|name|nome|tipo)")) { r[k] = RNome; continue; }
            }
            // dai valori: colonna di testo = nome; crescente da 0 = posizione; la piu' grande = lunghezza; la piu' piccola = diametro
            var num = new List<double[]>();
            for (int k = 0; k < n; k++)
            {
                var v = new List<double>();
                foreach (var riga in t.Righe) { double x; if (k < riga.Count && Valore(riga[k].Testo, t.Virgola, out x)) v.Add(x); }
                num.Add(v.ToArray());
            }
            for (int k = 0; k < n; k++)
                if (r[k] < 0 && num[k].Length < t.Righe.Count / 2.0) r[k] = r.Contains(RNome) ? RIgnora : RNome;
            var libere = Enumerable.Range(0, n).Where(k => r[k] < 0).ToList();
            Func<int, bool> crescente = k => num[k].Length >= 2 && num[k].Zip(num[k].Skip(1), (a, b) => b >= a).All(x => x);
            if (!r.Contains(RPos) && !r.Contains(RDist))
            {
                int k = libere.FirstOrDefault(c => crescente(c));
                if (libere.Any(c => crescente(c))) { r[k] = RPos; libere.Remove(k); }
            }
            if (!r.Contains(RLung) && !r.Contains(RSemi) && libere.Count > 0)
            {
                int k = libere.OrderByDescending(c => num[c].Length > 0 ? num[c].Average() : 0).First();
                r[k] = RLung; libere.Remove(k);
            }
            if (!r.Contains(RDiam) && !r.Contains(RRaggio) && libere.Count > 0)
            {
                int k = libere.OrderBy(c => num[c].Length > 0 ? num[c].Average() : double.MaxValue).First();
                r[k] = RDiam; libere.Remove(k);
            }
            for (int k = 0; k < n; k++) if (r[k] < 0) r[k] = RIgnora;
            return r;
        }

        /// <summary>Controlli di plausibilita' (rosso, non bloccano): posizioni crescenti, lunghezze entro lambda/2 +/- 15 %
        /// (solo Yagi, con la frequenza), direttori che si allungano. Restituisce le note.</summary>
        public static List<string> Controlla(TabellaLetta t, List<int> ruoli, double fMHz, int tipo)
        {
            var note = new List<string>();
            foreach (var r in t.Righe) foreach (var c in r) { c.Rossa = false; c.Nota = null; }
            int kp = ruoli.IndexOf(RPos), kl = ruoli.IndexOf(RLung), ks = ruoli.IndexOf(RSemi), kn = ruoli.IndexOf(RNome);
            double prec = double.NegativeInfinity;
            if (kp >= 0)
                for (int i = 0; i < t.Righe.Count; i++)
                {
                    double v; if (!Valore(t.Righe[i][kp].Testo, t.Virgola, out v)) continue;
                    if (v < prec) { t.Righe[i][kp].Rossa = true; t.Righe[i][kp].Nota = "posizione minore della precedente"; note.Add(Nome(t, i, kn) + ": posizione minore della precedente"); }
                    prec = v;
                }
            int kL = kl >= 0 ? kl : ks;
            if (kL >= 0 && tipo == 0)
            {
                double k = MetriPer(t.Unita, fMHz) * (kl >= 0 ? 1 : 2), mezza = 299.792458 / Math.Max(0.01, fMHz) / 2;
                double lPrec = double.NaN;
                for (int i = 0; i < t.Righe.Count; i++)
                {
                    double v; if (!Valore(t.Righe[i][kL].Testo, t.Virgola, out v)) continue;
                    double l = v * k;
                    var c = t.Righe[i][kL];
                    if (fMHz > 0 && (l < 0.85 * mezza || l > 1.15 * mezza)) { c.Rossa = true; c.Nota = "lontana da lambda/2 (" + (l / mezza * 100).ToString("0", CI) + " %): unita' o frequenza giuste?"; note.Add(Nome(t, i, kn) + ": lunghezza " + (l / mezza * 100).ToString("0", CI) + " % di lambda/2"); }
                    else if (i >= 3 && !double.IsNaN(lPrec) && l > lPrec + 1e-9) { c.Rossa = true; c.Nota = "nei direttori la lunghezza di solito cala andando avanti: se e' giusto si lascia cosi'"; note.Add(Nome(t, i, kn) + ": direttore piu' lungo del precedente (controllo, non errore)"); }
                    if (i >= 2) lPrec = l;
                }
            }
            return note;
        }

        static string Nome(TabellaLetta t, int i, int kn) { return kn >= 0 && t.Righe[i][kn].Testo.Length > 0 ? t.Righe[i][kn].Testo : "riga " + (i + 1); }

        // ------------------------------------------------------------------------------------------ elementi e modelli
        public static List<ElementoTab> Elementi(TabellaLetta t, List<int> ruoli, double fMHz, double diamPredef, out string errore)
        {
            string err = null;
            var l0 = ElementiInterni(t, ruoli, fMHz, diamPredef, e => { if (err == null) err = e; });
            errore = err;
            return l0;
        }

        static List<ElementoTab> ElementiInterni(TabellaLetta t, List<int> ruoli, double fMHz, double diamPredef, Action<string> segnala)
        {
            string errore = null;
            var l = new List<ElementoTab>();
            double k = MetriPer(t.Unita, fMHz);
            int kp = ruoli.IndexOf(RPos), kd = ruoli.IndexOf(RDist), kl = ruoli.IndexOf(RLung), ks = ruoli.IndexOf(RSemi), kD = ruoli.IndexOf(RDiam), kr = ruoli.IndexOf(RRaggio), kn = ruoli.IndexOf(RNome), kseg = ruoli.IndexOf(RSeg);
            bool fili = Enumerable.Range(RX1, 6).All(x => ruoli.Contains(x));
            double cum = 0;
            for (int i = 0; i < t.Righe.Count; i++)
            {
                var r = t.Righe[i];
                var e = new ElementoTab { Nome = kn >= 0 ? r[kn].Testo : "" };
                if (e.Nome.Length == 0) e.Nome = (i + 1).ToString(CI);
                double v;
                Func<int, string, double> leggi = (c, cosa) =>
                {
                    double x;
                    if (c < 0) return double.NaN;
                    if (!Valore(r[c].Testo, t.Virgola, out x)) { if (errore == null) errore = e.Nome + ": " + cosa + " non numerica (\"" + r[c].Testo + "\")"; return double.NaN; }
                    return x;
                };
                if (fili)
                {
                    e.Fili = new double[6];
                    for (int a = 0; a < 6; a++) e.Fili[a] = leggi(ruoli.IndexOf(RX1 + a), Ruoli[RX1 + a]) * k;
                }
                else
                {
                    if (kp >= 0) e.Pos = leggi(kp, "posizione") * k;
                    else if (kd >= 0) { v = leggi(kd, "distanza"); if (!double.IsNaN(v)) cum += v * k; e.Pos = cum; }
                    else if (errore == null) errore = "manca la colonna della posizione (o della distanza dal precedente)";
                    if (kl >= 0) e.Lung = leggi(kl, "lunghezza") * k;
                    else if (ks >= 0) e.Lung = 2 * leggi(ks, "semielemento") * k;
                    else if (errore == null) errore = "manca la colonna della lunghezza (o del semielemento)";
                }
                if (kD >= 0) e.Diam = leggi(kD, "diametro") * (t.Unita == 4 ? k : DiamPer(t.Unita));
                else if (kr >= 0) e.Diam = 2 * leggi(kr, "raggio") * (t.Unita == 4 ? k : DiamPer(t.Unita));
                else e.Diam = diamPredef;
                if (kseg >= 0) { v = leggi(kseg, "segmenti"); e.Segmenti = double.IsNaN(v) ? 0 : (int)v; }
                l.Add(e);
            }
            if (errore == null && l.Any(e => double.IsNaN(e.Pos) || double.IsNaN(e.Lung) || double.IsNaN(e.Diam) || e.Lung <= 0 && e.Fili == null)) errore = "ci sono celle vuote o non numeriche";
            if (errore != null) segnala(errore);
            return l;
        }

        // i diametri sono quasi sempre in mm anche quando le lunghezze sono in cm o in m; in pollici restano pollici
        static double DiamPer(int unita) { return unita == 3 ? 0.0254 : 0.001; }

        static int Seg(double l, double lam, bool dispari) { int n = Math.Max(dispari ? 3 : 1, (int)Math.Ceiling(l / (lam / 20))); if (dispari && n % 2 == 0) n++; return n; }

        static string Intestazione(string titolo, string fonte, TabellaLetta t, List<int> ruoli)
        {
            var sb = new StringBuilder();
            sb.AppendLine("CM " + (string.IsNullOrWhiteSpace(titolo) ? "Tabella importata" : titolo));
            sb.AppendLine("CM Importata con File > Importa tabella da immagine di YA-NEC; fonte: " + (fonte ?? ""));
            sb.AppendLine("CM Unita' della tabella: " + Unita[t.Unita] + "; colonne: " + string.Join(" | ", ruoli.Select(r => Ruoli[r])));
            sb.AppendLine("CM Tabella corretta:");
            foreach (var r in t.Righe) sb.AppendLine("CM   " + string.Join(" | ", r.Select(c => c.Testo)));
            return sb.ToString();
        }

        /// <summary>Modello NEC dalla tabella. tipo: 0 Yagi, 1 quad, 2 verticale con radiali, 3 fili liberi. alim = riga alimentata.</summary>
        public static string Nec(List<ElementoTab> el, int tipo, double fMHz, bool verticale, int alim, bool leeson, string titolo, string fonte, TabellaLetta t, List<int> ruoli)
        {
            double lam = 299.792458 / Math.Max(0.01, fMHz);
            var sb = new StringBuilder(Intestazione(titolo, fonte, t, ruoli));
            if (leeson) sb.AppendLine("CM YA-NEC LEESON=1");
            sb.AppendLine("CE");
            int tag = 0; string ex = null;
            Func<double, double, double, double, double, double, double, int, string> gw = (x1, y1, z1, x2, y2, z2, r, n) =>
                string.Format(CI, "GW {0} {1} {2:0.######} {3:0.######} {4:0.######} {5:0.######} {6:0.######} {7:0.######} {8:0.#######}", ++tag, n, x1, y1, z1, x2, y2, z2, r);
            double x0 = el.Count > 0 && tipo != 3 ? el.Min(e => e.Pos) : 0;
            if (tipo == 0)
            {
                for (int i = 0; i < el.Count; i++)
                {
                    var e = el[i]; double x = e.Pos - x0, h = e.Lung / 2; int n = e.Segmenti > 0 ? e.Segmenti | 1 : Seg(e.Lung, lam, true);
                    sb.AppendLine(verticale ? gw(x, 0, -h, x, 0, h, e.Diam / 2, n) : gw(x, -h, 0, x, h, 0, e.Diam / 2, n));
                    if (i == alim) ex = string.Format(CI, "EX 0 {0} {1} 0 1 0", tag, (n + 1) / 2);
                }
            }
            else if (tipo == 1)
            {
                // anelli quadrati nel piano YZ, lunghezza = perimetro; alimentazione al centro del lato basso (polarizzazione orizzontale)
                for (int i = 0; i < el.Count; i++)
                {
                    var e = el[i]; double x = e.Pos - x0, s = e.Lung / 4, a = s / 2, r = e.Diam / 2; int n = Seg(s, lam, true);
                    if (verticale)
                    {
                        sb.AppendLine(gw(x, -a, -a, x, -a, a, r, n)); if (i == alim) ex = string.Format(CI, "EX 0 {0} {1} 0 1 0", tag, (n + 1) / 2);
                        sb.AppendLine(gw(x, -a, a, x, a, a, r, n)); sb.AppendLine(gw(x, a, a, x, a, -a, r, n)); sb.AppendLine(gw(x, a, -a, x, -a, -a, r, n));
                    }
                    else
                    {
                        sb.AppendLine(gw(x, -a, -a, x, a, -a, r, n)); if (i == alim) ex = string.Format(CI, "EX 0 {0} {1} 0 1 0", tag, (n + 1) / 2);
                        sb.AppendLine(gw(x, a, -a, x, a, a, r, n)); sb.AppendLine(gw(x, a, a, x, -a, a, r, n)); sb.AppendLine(gw(x, -a, a, x, -a, -a, r, n));
                    }
                }
            }
            else if (tipo == 2)
            {
                // prima riga = radiatore verticale da z = 0 (sorgente alla base), le altre = radiali orizzontali a raggiera
                if (el.Count > 0)
                {
                    var rad = el[0]; int n = Seg(rad.Lung, lam, false);
                    sb.AppendLine(gw(0, 0, 0, 0, 0, rad.Lung, rad.Diam / 2, Math.Max(3, n)));
                    ex = string.Format(CI, "EX 0 {0} 1 0 1 0", tag);
                    int nr = el.Count - 1;
                    for (int i = 1; i < el.Count; i++)
                    {
                        double a = 2 * Math.PI * (i - 1) / nr, l = el[i].Lung;
                        sb.AppendLine(gw(0, 0, 0, l * Math.Cos(a), l * Math.Sin(a), 0, el[i].Diam / 2, Seg(l, lam, false)));
                    }
                }
            }
            else
            {
                for (int i = 0; i < el.Count; i++)
                {
                    var e = el[i]; var f = e.Fili;
                    double l = Math.Sqrt(Math.Pow(f[3] - f[0], 2) + Math.Pow(f[4] - f[1], 2) + Math.Pow(f[5] - f[2], 2));
                    int n = e.Segmenti > 0 ? e.Segmenti : Seg(l, lam, i == alim);
                    sb.AppendLine(gw(f[0], f[1], f[2], f[3], f[4], f[5], e.Diam / 2, n));
                    if (i == alim) ex = string.Format(CI, "EX 0 {0} {1} 0 1 0", tag, (n + 1) / 2);
                }
            }
            sb.AppendLine("GE 0");
            sb.AppendLine(string.Format(CI, "FR 0 1 0 0 {0:0.###} 0", fMHz));
            if (ex != null) sb.AppendLine(ex);
            sb.AppendLine("EN");
            return sb.ToString();
        }

        public static string Riassunto(List<ElementoTab> el, int tipo, double fMHz, string nec)
        {
            try
            {
                var m = NecModel.Parse(nec); var g = m.BuildGeometry();
                string boom = tipo <= 1 && el.Count > 1 ? ", boom " + ((el.Max(e => e.Pos) - el.Min(e => e.Pos))).ToString("0.00", CultureInfo.CurrentCulture) + " m" : "";
                return el.Count + (tipo == 3 ? " fili" : tipo == 2 ? " fili (radiatore e " + (el.Count - 1) + " radiali)" : " elementi") + boom + ", " + g.N + " segmenti.";
            }
            catch (Exception ex) { return "Modello non valido: " + ex.Message; }
        }

        // ------------------------------------------------------------------------------------------ confronto con i calcolatori
        /// <summary>Confronto colonna per colonna (posizione e lunghezza, mm) tra la tabella e un modello calcolato.</summary>
        public static List<string[]> Confronto(List<ElementoTab> el, string necCalcolato, bool perimetro)
        {
            var righe = new List<string[]>();
            var m = NecModel.Parse(necCalcolato);
            var mod = LetturaProgetto.ElementiModello(m.BuildGeometry());
            if (mod.Count == 0 || el.Count == 0) return righe;
            double p0 = el.Min(e => e.Pos), q0 = mod[0][0];
            for (int i = 0; i < el.Count; i++)
            {
                var e = el.OrderBy(q => q.Pos).ElementAt(i);
                if (i >= mod.Count) { righe.Add(new[] { e.Nome, Mm(e.Pos - p0), "-", "", Mm(e.Lung), "-", "" }); continue; }
                double pc = mod[i][0] - q0, lc = mod[i][1] * (perimetro ? 4 : 1);
                righe.Add(new[] { e.Nome, Mm(e.Pos - p0), Mm(pc), Diff(pc - (e.Pos - p0)), Mm(e.Lung), Mm(lc), Diff(lc - e.Lung) });
            }
            return righe;
        }
        static string Mm(double m) { return (m * 1000).ToString("0", CI); }
        static string Diff(double m) { return (m * 1000).ToString("+0;-0;0", CI); }

        // ------------------------------------------------------------------------------------------ immagine: raddrizza, contrasto, ritaglio
        /// <summary>Angolo (gradi) che rende piu' nette le righe di testo: profilo orizzontale con la varianza massima, da -6 a +6 gradi.</summary>
        public static double AngoloRaddrizza(byte[] png)
        {
            using (var b0 = SkiaSharp.SKBitmap.Decode(png))
            {
                if (b0 == null) return 0;
                double s = Math.Min(1.0, 900.0 / Math.Max(b0.Width, b0.Height));
                int w = Math.Max(1, (int)(b0.Width * s)), h = Math.Max(1, (int)(b0.Height * s));
                using (var b = b0.Resize(new SkiaSharp.SKImageInfo(w, h), SkiaSharp.SKFilterQuality.Medium))
                {
                    var px = b.Pixels;
                    var nere = new List<int[]>();
                    double soglia = 0; foreach (var c in px) soglia += (c.Red + c.Green + c.Blue) / 3.0; soglia = soglia / px.Length * 0.75;
                    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { var c = px[y * w + x]; if ((c.Red + c.Green + c.Blue) / 3.0 < soglia) nere.Add(new[] { x, y }); }
                    if (nere.Count < 50) return 0;
                    double migliore = 0, vMax = double.MinValue;
                    for (double a = -6; a <= 6.0001; a += 0.2)
                    {
                        double t = Math.Tan(a * Math.PI / 180);
                        var prof = new double[h + w];
                        foreach (var p in nere) { int yy = (int)Math.Round(p[1] - p[0] * t) + w / 2; if (yy >= 0 && yy < prof.Length) prof[yy]++; }
                        double m = prof.Average(), v = prof.Sum(q => (q - m) * (q - m));
                        if (v > vMax + 1e-9) { vMax = v; migliore = a; }
                    }
                    return Math.Round(migliore, 1);
                }
            }
        }

        /// <summary>Immagine ruotata di -angolo (raddrizzata), ritagliata (rettangolo in pixel dell'originale, dopo la rotazione) e,
        /// con "forte", con il contrasto stirato (foto): restituisce un PNG.</summary>
        public static byte[] Prepara(byte[] png, double angolo, bool forte, double[] ritaglio, bool linee = true)
        {
            using (var b0 = SkiaSharp.SKBitmap.Decode(png))
            {
                if (b0 == null) return png;
                int w = b0.Width, h = b0.Height;
                var info = new SkiaSharp.SKImageInfo(w, h, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul);
                using (var surf = SkiaSharp.SKSurface.Create(info))
                {
                    var c = surf.Canvas; c.Clear(SkiaSharp.SKColors.White);
                    c.Save(); c.RotateDegrees((float)(-angolo), w / 2f, h / 2f);
                    using (var p = new SkiaSharp.SKPaint { FilterQuality = SkiaSharp.SKFilterQuality.High, IsAntialias = true }) c.DrawBitmap(b0, 0, 0, p);
                    c.Restore();
                    using (var img = surf.Snapshot())
                    using (var bmp = SkiaSharp.SKBitmap.FromImage(img))
                    {
                        SkiaSharp.SKBitmap fin = bmp;
                        if (ritaglio != null)
                        {
                            var r = new SkiaSharp.SKRectI((int)Math.Max(0, ritaglio[0]), (int)Math.Max(0, ritaglio[1]), (int)Math.Min(w, ritaglio[2]), (int)Math.Min(h, ritaglio[3]));
                            if (r.Width > 10 && r.Height > 10) { var sub = new SkiaSharp.SKBitmap(); if (bmp.ExtractSubset(sub, r)) fin = sub.Copy(); }
                        }
                        if (linee) TogliLinee(fin);
                        if (forte)
                        {
                            var px = fin.Pixels;
                            var lum = px.Select(q => (q.Red * 299 + q.Green * 587 + q.Blue * 114) / 1000).OrderBy(q => q).ToArray();
                            int lo = lum[(int)(lum.Length * 0.02)], hi = lum[(int)(lum.Length * 0.6)];
                            if (hi <= lo + 10) hi = lo + 10;
                            for (int k = 0; k < px.Length; k++)
                            {
                                int g = (px[k].Red * 299 + px[k].Green * 587 + px[k].Blue * 114) / 1000;
                                byte v = (byte)Math.Max(0, Math.Min(255, (g - lo) * 255 / (hi - lo)));
                                px[k] = new SkiaSharp.SKColor(v, v, v, 255);
                            }
                            fin.Pixels = px;
                        }
                        using (var data = fin.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100)) { var o = data.ToArray(); if (fin != bmp) fin.Dispose(); return o; }
                    }
                }
            }
        }

        /// <summary>Toglie le linee della griglia (tratti orizzontali e verticali di nero piu' lunghi del 3 % della larghezza):
        /// con la griglia Tesseract legge le celle come un blocco unico (prova del 28/11 sulla tabella 1 della NBS TN 688, scansione
        /// a 300 dpi: 59 parole con la griglia, quasi tutti i numeri senza).</summary>
        public static void TogliLinee(SkiaSharp.SKBitmap b)
        {
            int w = b.Width, h = b.Height;
            var px = b.Pixels;
            var nero = new bool[w * h];
            for (int k = 0; k < px.Length; k++) nero[k] = (px[k].Red * 299 + px[k].Green * 587 + px[k].Blue * 114) / 1000 < 128;
            var togli = new bool[w * h];
            int L = Math.Max(25, (int)(0.03 * w));
            for (int y = 0; y < h; y++)
            {
                int x = 0;
                while (x < w)
                {
                    if (!nero[y * w + x]) { x++; continue; }
                    int s0 = x; while (x < w && nero[y * w + x]) x++;
                    if (x - s0 >= L) for (int q = s0; q < x; q++) togli[y * w + q] = true;
                }
            }
            for (int x = 0; x < w; x++)
            {
                int y = 0;
                while (y < h)
                {
                    if (!nero[y * w + x]) { y++; continue; }
                    int s0 = y; while (y < h && nero[y * w + x]) y++;
                    if (y - s0 >= L) for (int q = s0; q < y; q++) togli[q * w + x] = true;
                }
            }
            bool cambiato = false;
            for (int k = 0; k < px.Length; k++) if (togli[k]) { px[k] = SkiaSharp.SKColors.White; cambiato = true; }
            if (cambiato) b.Pixels = px;
        }

        /// <summary>Lingue di Tesseract disponibili in data/tessdata.</summary>
        public static bool LinguaPresente(string codice) { return File.Exists(Path.Combine(AppPaths.DataDir, "tessdata", codice + ".traineddata")); }

        // ------------------------------------------------------------------------------------------ autotest
        static readonly string[][] Prova =
        {
            new[] { "R", "0", "1032", "6" }, new[] { "DR", "410", "966", "6" }, new[] { "D1", "565", "941", "6" }, new[] { "D2", "890", "931", "6" },
            new[] { "D3", "1310", "922", "6" }, new[] { "D4", "1790", "914", "6" }, new[] { "D5", "2310", "908", "6" }, new[] { "D6", "2860", "902", "6" },
            new[] { "D7", "3420", "897", "6" }, new[] { "D8", "3990", "888", "6" }
        };

        static byte[] Disegna(double angolo, double scala)
        {
            int W = (int)(700 * scala), H = (int)(520 * scala);
            var info = new SkiaSharp.SKImageInfo(W, H);
            using (var surf = SkiaSharp.SKSurface.Create(info))
            using (var font = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.FromFamilyName("DejaVu Sans") ?? SkiaSharp.SKTypeface.Default, 16f))   // la scala la da' il canvas
            using (var paint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black, IsAntialias = true })
            {
                var c = surf.Canvas; c.Clear(SkiaSharp.SKColors.White);
                c.RotateDegrees((float)angolo, W / 2f, H / 2f);
                c.Scale((float)scala);
                c.DrawText("Tab. 4 - Yagi 10 elementi per 144 MHz (misure in mm)", 40, 40, font, paint);
                string[] h = { "Elem.", "Posiz.", "Lungh.", "Diam." }; float[] xs = { 40, 160, 300, 440 };
                for (int k = 0; k < 4; k++) c.DrawText(h[k], xs[k], 80, font, paint);
                for (int i = 0; i < Prova.Length; i++) for (int k = 0; k < 4; k++) c.DrawText(Prova[i][k], xs[k], 115 + i * 38, font, paint);
                if (Math.Abs(scala - 1.01) < 1e-6)   // griglia: linee orizzontali tra le righe e verticali tra le colonne
                {
                    using (var lp = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black, StrokeWidth = 2.5f })
                    {
                        for (int i = 0; i <= Prova.Length + 1; i++) c.DrawLine(30, 55 + i * 38 - (i == 0 ? 0 : 0), 520, 55 + i * 38, lp);
                        foreach (float x in new float[] { 30, 140, 280, 420, 520 }) c.DrawLine(x, 55, x, 55 + (Prova.Length + 1) * 38, lp);
                    }
                }
                using (var img = surf.Snapshot()) using (var d = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100)) return d.ToArray();
            }
        }

        static string Confronta(TabellaLetta t, out int giuste)
        {
            giuste = 0; int tot = 0;
            for (int i = 0; i < Prova.Length; i++)
                for (int k = 0; k < 4; k++)
                {
                    tot++;
                    if (i < t.Righe.Count && k < t.Righe[i].Count && t.Righe[i][k].Testo == Prova[i][k]) giuste++;
                }
            return giuste + " celle giuste su " + tot + ", " + t.Righe.Count + " righe x " + t.Colonne + " colonne";
        }

        /// <summary>Riga di comando (YA-NEC --importa-tabella file|prova [lingua]): tabella letta, per le prove.</summary>
        public static int RigaDiComando(string file, string lingua)
        {
            byte[] png = file == "prova" ? Disegna(1.6, 0.6) : File.ReadAllBytes(file);
            if (file == "prova") File.WriteAllBytes(Path.Combine(AppPaths.TempDir, "prova_tabella.png"), png);
            double a = AngoloRaddrizza(png);
            byte[] pr = Prepara(png, a, false, null);
            File.WriteAllBytes(Path.Combine(AppPaths.TempDir, "prova_tabella_dritta.png"), pr);
            string err; var pg = LetturaProgetto.Ocr(pr, file, out err, lingua ?? "eng", 6);
            if (pg == null) { Console.WriteLine(err); return 2; }
            Console.WriteLine("angolo " + a.ToString("0.0", CI) + ", " + pg.Parole.Count + " parole " + pg.Larghezza + "x" + pg.Altezza);
            foreach (var p in pg.Parole.OrderBy(q => q.Cy)) Console.Write(string.Format(CI, "{0:0},{1:0},{2:0}:{3} ", p.X, p.Y, p.H, p.Testo));
            Console.WriteLine();
            var t = Riconosci(pg);
            Console.WriteLine("Titolo: " + t.Titolo + " | unita' " + Unita[t.Unita] + " | " + string.Join(" | ", t.Intestazioni));
            foreach (var r in t.Righe) Console.WriteLine(string.Join(" | ", r.Select(c => c.Testo + (c.Incerta ? "?" : ""))));
            return 0;
        }

        public static List<Tuple<string, bool, string>> Prove()
        {
            var r = new List<Tuple<string, bool, string>>();
            // 1. riconoscimento dalle parole (come un PDF con il testo)
            var pg = new PaginaLetta { Larghezza = 700, Altezza = 520 };
            Action<string, double, double> parola = (s, x, y) => pg.Parole.Add(new ParolaLetta { Testo = s, X = x, Y = y - 12, W = 9 * s.Length, H = 14 });
            double xx = 40; foreach (var w in "Tab. 4 - Yagi 10 elementi per 144 MHz (misure in mm)".Split(' ')) { parola(w, xx, 40); xx += 9 * w.Length + 5; }
            string[] hh = { "Elem.", "Posiz.", "Lungh.", "Diam." }; double[] xs = { 40, 160, 300, 440 };
            for (int k = 0; k < 4; k++) parola(hh[k], xs[k], 80);
            for (int i = 0; i < Prova.Length; i++) for (int k = 0; k < 4; k++) parola(k == 0 && i == 2 ? "DI" : k == 1 && i == 4 ? "l310" : Prova[i][k], xs[k], 115 + i * 38);
            var t = Riconosci(pg);
            int g; string d = Confronta(t, out g);
            var ruoli = RuoliProbabili(t, 144.3);
            string sr = string.Join(",", ruoli.Select(q => Ruoli[q]));
            bool ok = g >= 39 && t.Unita == 0 && ruoli.SequenceEqual(new List<int> { RNome, RPos, RLung, RDiam }) && t.Righe[2][0].Incerta && t.Righe[2][0].Testo == "D1";
            r.Add(Tuple.Create("Importa tabella: riconoscimento di una tabella di prova (parole)", ok, d + "; colonne " + sr + "; unita' " + Unita[t.Unita] + "; DI letto D1 (giallo)"));
            // 2. controlli, elementi, modello e confronto con il calcolatore DL6WU (misure di prova, non di un articolo)
            var note = Controlla(t, ruoli, 144.3, 0);
            string err; var el = Elementi(t, ruoli, 144.3, 0.006, out err);
            bool ok2 = err == null && el.Count == 10 && Math.Abs(el[9].Pos - 3.99) < 1e-9 && Math.Abs(el[0].Diam - 0.006) < 1e-9;
            string d2 = err ?? (el.Count + " elementi, boom " + el[9].Pos.ToString("0.00", CI) + " m");
            try
            {
                string nec = Nec(el, 0, 144.3, false, 1, false, "prova", "autotest", t, ruoli);
                var m = NecModel.Parse(nec); var geo = m.BuildGeometry();
                ok2 = ok2 && m.Sources.Count == 1 && geo.N > 50;
                d2 += ", " + geo.N + " segmenti, sorgente sul DR; controlli: " + note.Count;
                var conf = Confronto(el, nec, false);
                ok2 = ok2 && conf.Count == 10 && conf.All(c => c[3] == "0" && c[6] == "0");
                d2 += "; confronto con se' stesso: " + conf.Count(c => c[3] == "0" && c[6] == "0") + " su 10 uguali";
            }
            catch (Exception ex) { ok2 = false; d2 += "; " + ex.Message; }
            r.Add(Tuple.Create("Importa tabella: modello NEC e confronto", ok2, d2));
            // 3. OCR di immagini della tabella: dritta, storta di 1,6 gradi e ridotta al 60 %
            if (LetturaProgetto.Tesseract() == null) { r.Add(Tuple.Create("Importa tabella: OCR (Tesseract non installato, prova saltata)", true, LetturaProgetto.ComeInstallare())); return r; }
            foreach (var caso in new[] { Tuple.Create(0.0, 1.0, "dritta"), Tuple.Create(1.6, 0.6, "storta di 1,6 gradi e al 60 %"), Tuple.Create(0.0, 1.01, "con la griglia") })
            {
                try
                {
                    byte[] png = Disegna(caso.Item1, caso.Item2);
                    double ang = AngoloRaddrizza(png);
                    byte[] pr = Prepara(png, ang, false, null);
                    string e2;
                    var po = LetturaProgetto.Ocr(pr, "prova", out e2, "eng", 6);
                    if (po == null) { r.Add(Tuple.Create("Importa tabella: OCR della tabella " + caso.Item3, false, e2)); continue; }
                    var to = Riconosci(po);
                    int go; string dd = Confronta(to, out go);
                    bool okA = Math.Abs(ang - caso.Item1) <= 0.4;
                    r.Add(Tuple.Create("Importa tabella: OCR della tabella " + caso.Item3, go >= 38 && okA, dd + "; raddrizzata di " + ang.ToString("0.0", CI) + " gradi"));
                }
                catch (Exception ex) { r.Add(Tuple.Create("Importa tabella: OCR della tabella " + caso.Item3, false, ex.Message)); }
            }
            return r;
        }
    }
}
