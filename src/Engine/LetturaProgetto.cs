// YA-NEC 1.8.0 - "Leggi progetto": legge il progetto meccanico di una direttiva (Yagi) da un PDF o da un'immagine
// (bozzetto approvato da IC8TEM il 28/11/2026 00:55, «Ok bozzetto compila e pubblica»).
//  - PDF con il testo dentro: parole e posizioni dal PDF (UglyToad.PdfPig, Apache 2.0), numeri esatti, senza OCR;
//  - immagini (anche quelle dentro il PDF, come i disegni dei costruttori): OCR con Tesseract (Apache 2.0), lanciato come programma
//    a parte: su Windows quello nella cartella "tesseract" del pacchetto, su Linux e Mac quello del sistema; dati "eng" (tessdata_fast)
//    in data/tessdata. L'immagine si ingrandisce 3 volte in grigio prima dell'OCR (prova sul disegno XR-5: piu' numeri letti).
// Riconoscimento (disegni come quelli di Force 12, Hy-Gain, Cushcraft, Optibeam): la posizione sul boom sta in una colonna a destra,
// sopra ogni tubo i diametri e sotto le lunghezze dei tratti visibili, dalla punta verso il centro; le righe di manicotti e
// giunzioni (liner, splice, inside...) e i numeri tra parentesi non contano. Tutto cio' che non torna si segna "da controllare":
// la tabella si corregge a mano prima di creare il modello. Il file letto resta dell'utente: YA-NEC non lo copia ne' lo distribuisce.
#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace YaNec.Engine
{
    public class ParolaLetta
    {
        public string Testo; public double X, Y, W, H, Conf = 100;
        public double Cx { get { return X + W / 2; } }
        public double Cy { get { return Y + H / 2; } }
    }

    /// <summary>Una pagina di testo del PDF o un'immagine (del PDF o aperta da sola): parole con il riquadro, in punti o pixel.</summary>
    public class PaginaLetta
    {
        public int Pagina;                  // numero di pagina del PDF (0 = immagine aperta da sola)
        public string Descrizione = "";
        public bool DaOcr;                  // true: immagine da leggere con l'OCR
        public byte[] Png;                  // immagine (solo per DaOcr)
        public double Larghezza, Altezza;
        public List<ParolaLetta> Parole = new List<ParolaLetta>();
        public int NumeriInPollici;         // per scegliere la pagina giusta
        public double Copertura;            // immagine del PDF: parte della pagina che copre (0-1)
    }

    public class SpezzoneLetto { public double Diam, Lung; }

    public class ElementoLetto
    {
        public string Nome = "";
        public double Pos;
        public List<SpezzoneLetto> Spezzoni = new List<SpezzoneLetto>();   // dal centro verso la punta (meta' elemento)
        public bool Alimentato;
        public List<string> Dubbi = new List<string>();
        public List<ParolaLetta> Fonti = new List<ParolaLetta>();
        public bool DubbioPos;
        public double MezzaLunghezza { get { double s = 0; foreach (var t in Spezzoni) s += t.Lung; return s; } }
    }

    public static class LetturaProgetto
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static readonly Regex ReNum = new Regex(@"^(\d+\.?\d*|\.\d+)(""|in|mm|cm)?$", RegexOptions.Compiled);
        static readonly string[] Parole_Escluse = { "liner", "splice", "inside", "boom", "bracket", "u-bolt", "mount", "note", "feed", "copyright", "spacer", "jacket", "wires", "lugs", "turning", "weight", "windload", "wind", "power" };

        public static string[] Unita = { "pollici", "mm", "cm", "m" };
        public static double MetriPer(int unita) { return unita == 0 ? 0.0254 : unita == 1 ? 0.001 : unita == 2 ? 0.01 : 1.0; }

        // ------------------------------------------------------------------------------------------ numeri
        static string Pulisci(string t)
        {
            if (t == null) return "";
            t = t.Trim().Replace('\u201D', '"').Replace('\u201C', '"').Replace('\u2033', '"').Replace("''", "\"").Replace(',', '.');
            t = t.TrimEnd('|', ';', ':', '_', '!');
            int k0 = 0; while (k0 < t.Length && "{[|_£€$'`~«»>".IndexOf(t[k0]) >= 0) k0++;
            t = t.Substring(k0);
            return t;
        }

        /// <summary>Numero con unita' facoltativa (36.5", .375", 0.", 120mm). pollici = c'era il segno ".</summary>
        public static bool Numero(string testo, out double v, out bool pollici)
        {
            v = 0; pollici = false;
            string t = Pulisci(testo);
            if (t.StartsWith("(")) return false;
            var m = ReNum.Match(t);
            if (!m.Success) return false;
            if (!double.TryParse(m.Groups[1].Value, NumberStyles.Float, CI, out v)) return false;
            pollici = m.Groups[2].Value == "\"" || m.Groups[2].Value == "in";
            return true;
        }

        // ------------------------------------------------------------------------------------------ riconoscimento
        class Riga { public double Cy, H; public List<ParolaLetta> P = new List<ParolaLetta>(); public string Testo { get { return string.Join(" ", P.OrderBy(q => q.X).Select(q => q.Testo)); } } }

        static List<Riga> Righe(IEnumerable<ParolaLetta> parole)
        {
            var lista = parole.Where(p => !string.IsNullOrWhiteSpace(p.Testo)).OrderBy(p => p.Cy).ToList();
            var righe = new List<Riga>();
            double hMed = lista.Count > 0 ? lista.Select(p => p.H).OrderBy(h => h).ElementAt(lista.Count / 2) : 10;
            foreach (var p in lista)
            {
                Riga r = righe.LastOrDefault(q => Math.Abs(q.Cy - p.Cy) < 0.45 * hMed);
                if (r == null) { r = new Riga { Cy = p.Cy, H = p.H }; righe.Add(r); }
                r.P.Add(p); r.Cy = r.P.Average(q => q.Cy);
            }
            return righe;
        }

        /// <summary>Unita' piu' probabile: pollici se la maggior parte dei numeri ha il segno ".</summary>
        public static int UnitaProbabile(PaginaLetta pg)
        {
            int con = 0, senza = 0;
            foreach (var p in pg.Parole) { double v; bool pol; if (Numero(p.Testo, out v, out pol)) { if (pol) con++; else senza++; } }
            return con >= senza && con > 0 ? 0 : 1;
        }

        /// <summary>Riconosce gli elementi della pagina. unita: 0 pollici, 1 mm, 2 cm, 3 m (solo per i controlli sui diametri).</summary>
        public static List<ElementoLetto> Analizza(PaginaLetta pg, int unita)
        {
            var el = new List<ElementoLetto>();
            double W = pg.Larghezza, H = pg.Altezza;
            if (W <= 0 || H <= 0 || pg.Parole.Count == 0) return el;
            double diamMax = unita == 0 ? 2.5 : unita == 1 ? 65 : unita == 2 ? 6.5 : 0.065;

            // 1. punti di riferimento degli elementi: i numeri della colonna di destra (posizione sul boom) e le righe dei titoli
            //    ("20 Mtr Reflector, Element #1"): un titolo senza posizione leggibile da' un elemento con la posizione da scrivere
            var posTok = new List<ParolaLetta>();
            foreach (var p in pg.Parole)
            {
                double v; bool pol;
                if (p.Cx > 0.84 * W && Numero(p.Testo, out v, out pol)) posTok.Add(p);
            }
            var titoli = Righe(pg.Parole.Where(p => { double vv; bool pp; return p.X >= 0.6 * W && Regex.IsMatch(p.Testo, "[A-Za-z#0-9]") && !(p.Cx > 0.84 * W && Numero(p.Testo, out vv, out pp)); }))
                .Where(r => Regex.IsMatch(r.Testo, @"element|elem\.|#\s*\d|refl|driv|direct|radiat|rifl|dirett|\bDE\b|\bDR\b|\bD\d", RegexOptions.IgnoreCase)
                         && !Parole_Escluse.Any(x => r.Testo.ToLowerInvariant().Contains(x)) && r.P.Count <= 8).ToList();
            double tolY = 0.015 * H;
            var anc = new List<Tuple<double, ParolaLetta, Riga>>();   // y, posizione, titolo
            foreach (var t in titoli)
            {
                var pt = posTok.Where(q => Math.Abs(q.Cy - t.Cy) < tolY).OrderBy(q => Math.Abs(q.Cy - t.Cy)).FirstOrDefault();
                anc.Add(Tuple.Create(pt != null ? pt.Cy : t.Cy, pt, t));
            }
            foreach (var q in posTok)
                if (!anc.Any(a => a.Item2 == q) && (titoli.Count < 3 || anc.Any(a => Math.Abs(a.Item1 - q.Cy) < 2 * tolY)) && !anc.Any(a => Math.Abs(a.Item1 - q.Cy) < tolY))
                    anc.Add(Tuple.Create(q.Cy, q, (Riga)null));
            anc = anc.OrderBy(a => a.Item1).ToList();
            if (anc.Count == 0) return el;

            // righe del disegno, senza la colonna di destra ne' i titoli; le righe di manicotti e note con al massimo due numeri non contano
            var righe = Righe(pg.Parole.Where(p => p.Cx <= 0.84 * W && !titoli.Any(t => t.P.Contains(p))));
            righe = righe.Where(r =>
            {
                string t = r.Testo.ToLowerInvariant();
                if (!Parole_Escluse.Any(x => t.Contains(x))) return true;
                double vv; bool pp;
                return r.P.Count(q => Numero(q.Testo, out vv, out pp)) > 2;
            }).ToList();

            for (int i = 0; i < anc.Count; i++)
            {
                var e = new ElementoLetto();
                double v; bool pol;
                var pt = anc[i].Item2;
                if (pt != null && Numero(pt.Testo, out v, out pol))
                {
                    e.Pos = v; e.Fonti.Add(pt);
                    if (pt.Conf < 70) { e.DubbioPos = true; e.Dubbi.Add("posizione letta male (OCR " + pt.Conf.ToString("0", CI) + " %)"); }
                }
                else { e.Pos = double.NaN; e.DubbioPos = true; e.Dubbi.Add("posizione non letta: scriverla"); }
                double y0 = anc[i].Item1;
                double su = i > 0 ? (anc[i - 1].Item1 + y0) / 2 : y0 - 0.06 * H, giu = i + 1 < anc.Count ? (anc[i + 1].Item1 + y0) / 2 : y0 + 0.06 * H;
                if (anc[i].Item3 != null)
                {
                    var parole = anc[i].Item3.P.OrderBy(p => p.X).Select(p => p.Testo).ToList();
                    while (parole.Count > 0 && !Regex.IsMatch(parole[0], @"^[A-Za-z0-9#]")) parole.RemoveAt(0);
                    e.Nome = string.Join(" ", parole).Trim().TrimEnd(',');
                }
                if (e.Nome.Length == 0) e.Nome = "elemento " + (i + 1);

                // 2. righe con numeri nella fascia dell'elemento: la prima con diametri, la seguente con le lunghezze
                var fascia = righe.Where(r => r.Cy > su && r.Cy <= giu).OrderBy(r => r.Cy).ToList();
                Func<Riga, List<KeyValuePair<ParolaLetta, double>>> numeri = r =>
                {
                    var l = new List<KeyValuePair<ParolaLetta, double>>();
                    var ord = r.P.OrderBy(p => p.X).ToList();
                    for (int k = 0; k < ord.Count; k++)
                    {
                        double vv; bool pp;
                        if (!Numero(ord[k].Testo, out vv, out pp)) continue;
                        bool perX = (k + 1 < ord.Count && Regex.IsMatch(ord[k + 1].Testo, "^[x×X*]$")) || (k > 0 && Regex.IsMatch(ord[k - 1].Testo, "^[x×X*]$"));
                        if (perX) continue;                                  // manicotto .875" x 72"
                        if (k > 0 && ord[k - 1].Testo.TrimEnd().EndsWith("(")) continue;
                        if (unita == 0 && !pp)
                        {
                            string tt = Pulisci(ord[k].Testo);
                            if (!tt.Contains(".") && !Regex.IsMatch(tt, @"^\d{3}$")) continue;   // 20 di "20 Mtr", segni del disegno
                        }
                        l.Add(new KeyValuePair<ParolaLetta, double>(ord[k], vv));
                    }
                    return l;
                };
                int iD = -1;
                for (int k = 0; k < fascia.Count; k++)
                {
                    var n = numeri(fascia[k]);
                    if (n.Count == 0) continue;
                    // diametri: valori piccoli (o 375" / 500" dove l'OCR ha perso il punto)
                    int piccoli = n.Count(q => Diametro(q.Key.Testo, q.Value, unita) <= diamMax);
                    if (piccoli >= Math.Max(1, (n.Count + 1) / 2)) { iD = k; break; }
                }
                if (iD < 0) { e.Dubbi.Add("diametri non trovati"); el.Add(e); continue; }
                var dNum = numeri(fascia[iD]).Select(q => new KeyValuePair<ParolaLetta, double>(q.Key, Diametro(q.Key.Testo, q.Value, unita))).Where(q => q.Value <= diamMax).ToList();
                List<KeyValuePair<ParolaLetta, double>> lNum = null;
                for (int k = iD + 1; k < fascia.Count; k++) { var n = numeri(fascia[k]); if (n.Count > 0) { lNum = n; break; } }
                if (lNum == null) { e.Dubbi.Add("lunghezze non trovate"); el.Add(e); continue; }

                // 3. accoppiamento per x: a ogni diametro la lunghezza piu' vicina in orizzontale
                var usate = new HashSet<ParolaLetta>();
                var coppie = new List<Tuple<double, SpezzoneLetto>>();
                foreach (var d in dNum.OrderBy(q => q.Key.Cx))
                {
                    KeyValuePair<ParolaLetta, double> best = default(KeyValuePair<ParolaLetta, double>); double bd = double.MaxValue;
                    foreach (var l in lNum) { if (usate.Contains(l.Key)) continue; double dx = Math.Abs(l.Key.Cx - d.Key.Cx); if (dx < bd) { bd = dx; best = l; } }
                    if (best.Key == null || bd > 0.09 * W) { e.Dubbi.Add("diametro " + Fmt(d.Value) + " senza lunghezza"); continue; }
                    usate.Add(best.Key);
                    coppie.Add(Tuple.Create(d.Key.Cx, new SpezzoneLetto { Diam = d.Value, Lung = best.Value }));
                    e.Fonti.Add(d.Key); e.Fonti.Add(best.Key);
                    if (d.Key.Conf < 70 || best.Key.Conf < 70) e.Dubbi.Add("tratto " + Fmt(d.Value) + " × " + Fmt(best.Value) + " letto male");
                }
                if (lNum.Count(l => !usate.Contains(l.Key)) > 0) e.Dubbi.Add("lunghezze in piu': " + string.Join(" ", lNum.Where(l => !usate.Contains(l.Key)).Select(l => Fmt(l.Value))));
                // dal centro alla punta: le quote stanno da un lato del boom; il centro e' il lato verso il mezzo della pagina
                double media = coppie.Count > 0 ? coppie.Average(c => c.Item1) : 0;
                var ordinate = media < 0.5 * W ? coppie.OrderByDescending(c => c.Item1) : coppie.OrderBy(c => c.Item1);
                e.Spezzoni = ordinate.Select(c => c.Item2).ToList();
                Controlla(e);
                el.Add(e);
            }
            // alimentato: il primo radiatore
            var rad = el.FirstOrDefault(x => Regex.IsMatch(x.Nome, @"driv|radiat|dipol|alim|\bDE\b", RegexOptions.IgnoreCase));
            if (rad != null) rad.Alimentato = true;
            ControllaPosizioni(el);
            return el;
        }

        // diametro: con l'OCR il punto iniziale si perde spesso (375" invece di .375")
        static double Diametro(string testo, double v, int unita)
        {
            string t = Pulisci(testo);
            if (unita == 0 && !t.Contains(".") && v >= 100 && v < 2000 && Regex.IsMatch(t, @"^\d{3}""?$")) return v / 1000.0;
            return v;
        }

        public static void Controlla(ElementoLetto e)
        {
            e.Dubbi.RemoveAll(d => d.StartsWith("diametri che crescono"));
            for (int k = 1; k < e.Spezzoni.Count; k++)
                if (e.Spezzoni[k].Diam > e.Spezzoni[k - 1].Diam + 1e-9) { e.Dubbi.Add("diametri che crescono verso la punta"); break; }
            if (e.Spezzoni.Count == 0 && !e.Dubbi.Any()) e.Dubbi.Add("nessun tratto");
        }

        public static void ControllaPosizioni(List<ElementoLetto> el)
        {
            for (int i = 1; i < el.Count; i++)
                if (!double.IsNaN(el[i].Pos) && !double.IsNaN(el[i - 1].Pos) && el[i].Pos <= el[i - 1].Pos && !el[i].DubbioPos) { el[i].DubbioPos = true; el[i].Dubbi.Add("posizione non crescente lungo il boom"); }
        }

        public static string Fmt(double v) { return v.ToString("0.###", CI); }

        /// <summary>Tratti scritti come "1×72 · .875×12 · .75×33" (dal centro alla punta).</summary>
        public static string TestoSpezzoni(List<SpezzoneLetto> s) { return string.Join(" · ", s.Select(t => Fmt(t.Diam) + "×" + Fmt(t.Lung))); }

        public static bool LeggiSpezzoni(string testo, out List<SpezzoneLetto> s, out string errore)
        {
            s = new List<SpezzoneLetto>(); errore = null;
            foreach (Match m in Regex.Matches(testo ?? "", @"(\d*\.?\d+)\s*[x×X*]\s*(\d*\.?\d+)"))
                s.Add(new SpezzoneLetto { Diam = double.Parse(m.Groups[1].Value, CI), Lung = double.Parse(m.Groups[2].Value, CI) });
            if (s.Count == 0) { errore = "scrivere i tratti come diametro×lunghezza, dal centro verso la punta (per esempio 1×72 · .875×12)"; return false; }
            return true;
        }

        // ------------------------------------------------------------------------------------------ modello NEC
        /// <summary>Modello NEC: boom lungo X, elementi lungo Y a z = 0 (spazio libero), un filo per tratto; il tratto centrale
        /// e' un filo unico con un numero dispari di segmenti (sorgente al centro degli elementi alimentati).</summary>
        public static string CreaNec(List<ElementoLetto> el, int unita, double fMHz, string titolo, string fonte)
        {
            double k = MetriPer(unita), lam = 299.792458 / Math.Max(0.1, fMHz), seg = lam / 40;
            var sb = new StringBuilder();
            sb.AppendLine("CM " + (titolo ?? "Progetto letto"));
            sb.AppendLine("CM Letto con Calcolatori > Strumenti > Leggi progetto di YA-NEC da: " + (fonte ?? ""));
            sb.AppendLine("CM Misure del costruttore (" + Unita[unita] + ") controllate a mano; boom lungo X, elementi lungo Y, spazio libero");
            sb.AppendLine("CM Segmenti di circa lambda/40 a " + fMHz.ToString("0.###", CI) + " MHz");
            sb.AppendLine("CE");
            int tag = 0; var ex = new List<string>();
            double x0 = el.Count > 0 ? el.Min(e => e.Pos) : 0;
            foreach (var e in el.OrderBy(q => q.Pos))
            {
                if (e.Spezzoni.Count == 0) continue;
                double x = (e.Pos - x0) * k;
                double l0 = e.Spezzoni[0].Lung * k;
                int n0 = Math.Max(1, (int)Math.Ceiling(2 * l0 / seg)); if (n0 % 2 == 0) n0++;
                if (e.Alimentato && n0 < 3) n0 = 3;
                tag++;
                sb.AppendLine(string.Format(CI, "GW {0} {1} {2:0.######} {3:0.######} 0 {2:0.######} {4:0.######} 0 {5:0.#######}", tag, n0, x, -l0, l0, e.Spezzoni[0].Diam * k / 2));
                if (e.Alimentato) ex.Add(string.Format(CI, "EX 0 {0} {1} 0 1 0", tag, (n0 + 1) / 2));
                double a = l0;
                for (int s = 1; s < e.Spezzoni.Count; s++)
                {
                    double l = e.Spezzoni[s].Lung * k, r = e.Spezzoni[s].Diam * k / 2;
                    int n = Math.Max(1, (int)Math.Ceiling(l / seg));
                    tag++; sb.AppendLine(string.Format(CI, "GW {0} {1} {2:0.######} {3:0.######} 0 {2:0.######} {4:0.######} 0 {5:0.#######}", tag, n, x, a, a + l, r));
                    tag++; sb.AppendLine(string.Format(CI, "GW {0} {1} {2:0.######} {3:0.######} 0 {2:0.######} {4:0.######} 0 {5:0.#######}", tag, n, x, -a, -a - l, r));
                    a += l;
                }
            }
            sb.AppendLine("GE 0");
            sb.AppendLine(string.Format(CI, "FR 0 1 0 0 {0:0.###} 0", fMHz));
            if (ex.Count == 0) sb.AppendLine("CM nessun elemento alimentato: scegliere l'elemento alimentato in YA-NEC");
            foreach (var s in ex) sb.AppendLine(s);
            sb.AppendLine("EN");
            return sb.ToString();
        }

        /// <summary>Frequenza piu' alta nominata nel testo (20 Mtr, 10 m, 14.2 MHz...), per la segmentazione.</summary>
        public static double FrequenzaDalTesto(IEnumerable<string> testi)
        {
            double f = 0;
            string t = string.Join(" ", testi);
            foreach (Match m in Regex.Matches(t, @"\b(160|80|60|40|30|20|17|15|12|10|6)\s*(m|mtr|meter|metri|mt)\b", RegexOptions.IgnoreCase))
            {
                int b = int.Parse(m.Groups[1].Value, CI);
                double fb = b == 160 ? 1.9 : b == 80 ? 3.7 : b == 60 ? 5.36 : b == 40 ? 7.15 : b == 30 ? 10.13 : b == 20 ? 14.2 : b == 17 ? 18.12 : b == 15 ? 21.2 : b == 12 ? 24.94 : b == 10 ? 29.0 : 51;
                f = Math.Max(f, fb);
            }
            foreach (Match m in Regex.Matches(t, @"(\d+(?:\.\d+)?)\s*MHz", RegexOptions.IgnoreCase))
            {
                double v; if (double.TryParse(m.Groups[1].Value, NumberStyles.Float, CI, out v) && v > 1 && v < 500) f = Math.Max(f, v);
            }
            return f;
        }

        // ------------------------------------------------------------------------------------------ PDF
        /// <summary>Pagine di testo (con almeno un numero) e immagini grandi del PDF.</summary>
        public static List<PaginaLetta> LeggiPdf(string percorso)
        {
            var out_ = new List<PaginaLetta>();
            using (var doc = UglyToad.PdfPig.PdfDocument.Open(percorso))
            {
                foreach (var pg in doc.GetPages())
                {
                    var pt = new PaginaLetta { Pagina = pg.Number, Larghezza = pg.Width, Altezza = pg.Height, Descrizione = "pagina " + pg.Number + " - testo" };
                    foreach (var w in pg.GetWords())
                    {
                        var b = w.BoundingBox;
                        pt.Parole.Add(new ParolaLetta { Testo = w.Text, X = b.Left, Y = pg.Height - b.Top, W = b.Width, H = Math.Max(1, b.Height) });
                    }
                    pt.NumeriInPollici = Pollici(pt);
                    if (pt.NumeriInPollici > 0 || pt.Parole.Count(p => { double v; bool pol; return Numero(p.Testo, out v, out pol); }) >= 8) out_.Add(pt);
                    int ni = 0;
                    foreach (var im in pg.GetImages())
                    {
                        ni++;
                        var r = im.Bounds;
                        if (r.Width * r.Height < 0.15 * pg.Width * pg.Height) continue;   // loghi e foto piccole
                        byte[] png = null;
                        try { byte[] p2; if (im.TryGetPng(out p2)) png = p2; } catch { }
                        if (png == null) { try { png = im.RawBytes.ToArray(); } catch { } }
                        if (png == null || png.Length < 100) continue;
                        out_.Add(new PaginaLetta { Pagina = pg.Number, DaOcr = true, Png = png, Copertura = r.Width * r.Height / (pg.Width * pg.Height), Descrizione = "pagina " + pg.Number + " - immagine " + ni + " (" + im.WidthInSamples + "×" + im.HeightInSamples + ")" });
                    }
                }
            }
            return out_;
        }

        /// <summary>Pagina da proporre: la prima immagine che copre almeno meta' pagina (il disegno), poi la prima immagine, poi il testo con piu' misure.</summary>
        public static int Proposta(List<PaginaLetta> l)
        {
            int k = l.FindIndex(q => q.DaOcr && (q.Copertura >= 0.5 || q.Pagina == 0));
            if (k < 0) k = l.FindIndex(q => q.DaOcr);
            if (k < 0 && l.Count > 0) k = l.Select((q, i) => new { q, i }).OrderByDescending(x => x.q.NumeriInPollici).First().i;
            return k;
        }

        static int Pollici(PaginaLetta p) { int n = 0; foreach (var w in p.Parole) { double v; bool pol; if (Numero(w.Testo, out v, out pol) && pol) n++; } return n; }

        // ------------------------------------------------------------------------------------------ OCR (Tesseract)
        /// <summary>Programma tesseract: nella cartella "tesseract" di YA-NEC (Windows) o nel sistema; null se manca.</summary>
        public static string Tesseract()
        {
            bool win = Path.DirectorySeparatorChar == '\\';
            string exe = win ? "tesseract.exe" : "tesseract";
            var dove = new List<string> { Path.Combine(AppContext.BaseDirectory, "tesseract", exe) };
            if (win)
            {
                dove.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tesseract-OCR", exe));
                dove.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Tesseract-OCR", exe));
            }
            else { dove.Add("/opt/homebrew/bin/tesseract"); dove.Add("/usr/local/bin/tesseract"); dove.Add("/usr/bin/tesseract"); }
            foreach (string d in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)) if (d.Length > 0) dove.Add(Path.Combine(d, exe));
            foreach (string d in dove) { try { if (File.Exists(d)) return d; } catch { } }
            return null;
        }

        public static string ComeInstallare()
        {
            if (Path.DirectorySeparatorChar == '\\') return "Su Windows l'OCR e' nella cartella \"tesseract\" dei pacchetti x64 e ARM64; con quello a 32 bit installare Tesseract (UB Mannheim) in C:\\Program Files\\Tesseract-OCR.";
            if (File.Exists("/System/Library/CoreServices/SystemVersion.plist")) return "Su Mac: brew install tesseract";
            return "Su Linux: sudo apt install tesseract-ocr (Debian, Ubuntu, Raspberry Pi OS), sudo dnf install tesseract (Fedora), sudo pacman -S tesseract tesseract-data-eng (Arch)";
        }

        /// <summary>OCR di un'immagine (PNG o JPEG): parole in pixel dell'immagine originale. scala = ingrandimento prima dell'OCR.</summary>
        public static PaginaLetta Ocr(byte[] immagine, string descrizione, out string errore)
        {
            errore = null;
            string exe = Tesseract();
            if (exe == null) { errore = "OCR non disponibile: Tesseract non trovato. " + ComeInstallare(); return null; }
            string dir = Path.Combine(AppPaths.TempDir, "ocr_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(dir);
            try
            {
                int w0, h0;
                PaginaLetta pg = null;
                using (var bmp = SkiaSharp.SKBitmap.Decode(immagine))
                {
                    if (bmp == null) { errore = "immagine non leggibile"; return null; }
                    w0 = bmp.Width; h0 = bmp.Height;
                    pg = new PaginaLetta { DaOcr = true, Png = immagine, Larghezza = w0, Altezza = h0, Descrizione = descrizione };
                    // tre letture (prova sul disegno XR-5: 85 numeri con la prima, 97 con tutte e tre): 3 volte, 4 volte, 4 volte con piu' contrasto
                    double s0 = w0 < 1600 ? 3.0 : w0 < 3000 ? 1.5 : 1.0;
                    var passi = new[] { Tuple.Create(s0, false), Tuple.Create(s0 * 4 / 3, false), Tuple.Create(s0 * 4 / 3, true) };
                    int n = 0;
                    foreach (var pa in passi)
                    {
                        n++;
                        string png = Path.Combine(dir, "pagina" + n + ".png");
                        ScriviGrigio(bmp, pa.Item1, pa.Item2, png);
                        string err;
                        var parole = Tesseract(exe, png, Path.Combine(dir, "uscita" + n), pa.Item1, out err);
                        if (parole == null) { if (n == 1) { errore = err; return null; } continue; }
                        Unisci(pg.Parole, parole);
                    }
                }
                pg.NumeriInPollici = Pollici(pg);
                return pg;
            }
            catch (Exception ex) { errore = "OCR non riuscito: " + ex.Message; return null; }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        // immagine ingrandita in grigio (e con piu' contrasto: 20-80 % portato a 0-100 %)
        static void ScriviGrigio(SkiaSharp.SKBitmap bmp, double scala, bool contrasto, string png)
        {
            var info = new SkiaSharp.SKImageInfo((int)(bmp.Width * scala), (int)(bmp.Height * scala), SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Premul);
            using (var surf = SkiaSharp.SKSurface.Create(info))
            {
                var c = surf.Canvas;
                c.Clear(SkiaSharp.SKColors.White);
                using (var paint = new SkiaSharp.SKPaint { FilterQuality = SkiaSharp.SKFilterQuality.High, IsAntialias = true })
                {
                    paint.ColorFilter = SkiaSharp.SKColorFilter.CreateColorMatrix(new float[] { 0.299f, 0.587f, 0.114f, 0, 0, 0.299f, 0.587f, 0.114f, 0, 0, 0.299f, 0.587f, 0.114f, 0, 0, 0, 0, 0, 1, 0 });
                    c.DrawBitmap(bmp, new SkiaSharp.SKRect(0, 0, info.Width, info.Height), paint);
                }
                using (var img = surf.Snapshot())
                {
                    SkiaSharp.SKData data;
                    if (contrasto)
                    {
                        using (var b2 = SkiaSharp.SKBitmap.FromImage(img))
                        {
                            var px = b2.Pixels;
                            for (int k = 0; k < px.Length; k++)
                            {
                                int g = px[k].Red;
                                int v = (int)Math.Round((g - 51) * 255.0 / 153.0);
                                byte bb = (byte)Math.Max(0, Math.Min(255, v));
                                px[k] = new SkiaSharp.SKColor(bb, bb, bb, 255);
                            }
                            b2.Pixels = px;
                            data = b2.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        }
                    }
                    else data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    using (data) using (var fs = File.Create(png)) data.SaveTo(fs);
                }
            }
        }

        static List<ParolaLetta> Tesseract(string exe, string png, string uscita, double scala, out string errore)
        {
            errore = null;
            var psi = new ProcessStartInfo { FileName = exe, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
            psi.ArgumentList.Add(png); psi.ArgumentList.Add(uscita);
            string td = Path.Combine(AppPaths.DataDir, "tessdata");
            if (File.Exists(Path.Combine(td, "eng.traineddata"))) { psi.ArgumentList.Add("--tessdata-dir"); psi.ArgumentList.Add(td); }
            psi.ArgumentList.Add("-l"); psi.ArgumentList.Add("eng");
            psi.ArgumentList.Add("--psm"); psi.ArgumentList.Add("11");
            psi.ArgumentList.Add("-c"); psi.ArgumentList.Add("tessedit_create_tsv=1");   // senza il file di configurazione "tsv"
            string err;
            using (var pr = Process.Start(psi))
            {
                var tErr = pr.StandardError.ReadToEndAsync(); pr.StandardOutput.ReadToEnd();
                if (!pr.WaitForExit(180000)) { try { pr.Kill(); } catch { } errore = "OCR fermato dopo 3 minuti"; return null; }
                err = tErr.Result;
            }
            string tsv = uscita + ".tsv";
            if (!File.Exists(tsv)) { errore = "OCR non riuscito: " + (err ?? "").Trim(); return null; }
            var l = new List<ParolaLetta>();
            foreach (string riga in File.ReadAllLines(tsv).Skip(1))
            {
                var c = riga.Split('\t');
                if (c.Length < 12 || c[0] != "5" || string.IsNullOrWhiteSpace(c[11])) continue;
                double x = double.Parse(c[6], CI), t = double.Parse(c[7], CI), ww = double.Parse(c[8], CI), hh = double.Parse(c[9], CI), cf;
                double.TryParse(c[10], NumberStyles.Float, CI, out cf);
                l.Add(new ParolaLetta { Testo = c[11], X = x / scala, Y = t / scala, W = ww / scala, H = hh / scala, Conf = cf });
            }
            return l;
        }

        // unisce le parole di un'altra lettura: dove due parole si sovrappongono resta quella letta con piu' sicurezza
        static void Unisci(List<ParolaLetta> tutte, List<ParolaLetta> nuove)
        {
            foreach (var p in nuove)
            {
                int k = tutte.FindIndex(q => Math.Abs(q.Cx - p.Cx) < 0.5 * Math.Max(q.W, p.W) && Math.Abs(q.Cy - p.Cy) < 0.6 * Math.Max(q.H, p.H));
                if (k < 0) tutte.Add(p);
                else if (p.Conf > tutte[k].Conf + 5) tutte[k] = p;
            }
        }

        // ------------------------------------------------------------------------------------------ confronto
        /// <summary>Elementi di un modello a segmenti: segmenti raggruppati per posizione sul boom (asse perpendicolare agli elementi),
        /// lunghezza da punta a punta. Posizioni e lunghezze in metri.</summary>
        public static List<double[]> ElementiModello(SegmentGeometry g)
        {
            var r = new List<double[]>();
            if (g == null || g.N == 0) return r;
            double[] somma = new double[3];
            for (int i = 0; i < g.N; i++) { somma[0] += Math.Abs(g.X2[i] - g.X1[i]); somma[1] += Math.Abs(g.Y2[i] - g.Y1[i]); somma[2] += Math.Abs(g.Z2[i] - g.Z1[i]); }
            int ae = somma[0] >= somma[1] && somma[0] >= somma[2] ? 0 : somma[1] >= somma[2] ? 1 : 2;
            Func<int, int, double> mid = (i, a) => a == 0 ? (g.X1[i] + g.X2[i]) / 2 : a == 1 ? (g.Y1[i] + g.Y2[i]) / 2 : (g.Z1[i] + g.Z2[i]) / 2;
            Func<int, int, double> c1 = (i, a) => a == 0 ? g.X1[i] : a == 1 ? g.Y1[i] : g.Z1[i];
            Func<int, int, double> c2 = (i, a) => a == 0 ? g.X2[i] : a == 1 ? g.Y2[i] : g.Z2[i];
            int ab = -1; double est = -1;
            for (int a = 0; a < 3; a++)
            {
                if (a == ae) continue;
                double mn = double.MaxValue, mx = double.MinValue;
                for (int i = 0; i < g.N; i++) { double m = mid(i, a); mn = Math.Min(mn, m); mx = Math.Max(mx, m); }
                if (mx - mn > est) { est = mx - mn; ab = a; }
            }
            var gruppi = new SortedDictionary<long, List<int>>();
            for (int i = 0; i < g.N; i++)
            {
                if (Math.Abs(c2(i, ae) - c1(i, ae)) < 0.5 * Math.Abs(c2(i, ab) - c1(i, ab))) continue;   // segmenti lungo il boom
                long key = (long)Math.Round(mid(i, ab) / 0.005);
                List<int> l; if (!gruppi.TryGetValue(key, out l)) { l = new List<int>(); gruppi[key] = l; }
                l.Add(i);
            }
            foreach (var kv in gruppi)
            {
                double mn = double.MaxValue, mx = double.MinValue, p = 0;
                foreach (int i in kv.Value) { mn = Math.Min(mn, Math.Min(c1(i, ae), c2(i, ae))); mx = Math.Max(mx, Math.Max(c1(i, ae), c2(i, ae))); p += mid(i, ab); }
                r.Add(new[] { p / kv.Value.Count, mx - mn });
            }
            r.Sort((a, b) => a[0].CompareTo(b[0]));
            return r;
        }

        /// <summary>Confronto con gli elementi del modello aperto: differenza di posizione (dal primo elemento) e di lunghezza totale.</summary>
        public static List<string[]> Confronta(List<ElementoLetto> el, int unita, SegmentGeometry g)
        {
            var righe = new List<string[]>();
            var modello = ElementiModello(g);
            if (modello.Count == 0 || el.Count == 0) return righe;
            double k = MetriPer(unita);
            double p0l = el.Min(e => e.Pos) * k, p0m = modello[0][0];
            if (Math.Abs(modello[modello.Count - 1][0] - p0m) > 0 && el.Count > 1)
            {
                // il modello puo' avere il boom nel verso opposto
                double lungL = (el.Max(e => e.Pos) - el.Min(e => e.Pos)) * k;
                if (modello.Count > 1 && Math.Abs(modello[0][1] - 2 * el.OrderBy(e => e.Pos).First().MezzaLunghezza * k) > Math.Abs(modello[modello.Count - 1][1] - 2 * el.OrderBy(e => e.Pos).First().MezzaLunghezza * k))
                {
                    double pf = modello[modello.Count - 1][0];
                    modello = modello.Select(q => new[] { pf - q[0], q[1] }).OrderBy(q => q[0]).ToList();
                    p0m = modello[0][0];
                }
            }
            var usati = new HashSet<int>();
            foreach (var e in el.OrderBy(q => q.Pos))
            {
                double p = e.Pos * k - p0l;
                int best = -1; double bd = double.MaxValue;
                for (int i = 0; i < modello.Count; i++) { if (usati.Contains(i)) continue; double d = Math.Abs(modello[i][0] - p0m - p); if (d < bd) { bd = d; best = i; } }
                if (best < 0) { righe.Add(new[] { e.Nome, "-", "-", "nessun elemento del modello" }); continue; }
                usati.Add(best);
                double dp = (modello[best][0] - p0m - p) * 1000, dl = (modello[best][1] - 2 * e.MezzaLunghezza * k) * 1000;
                righe.Add(new[] { e.Nome, string.Format(CI, "{0:+0;-0;0} mm", dp), string.Format(CI, "{0:+0;-0;0} mm", dl), Math.Abs(dp) <= 5 && Math.Abs(dl) <= 10 ? "uguale" : "diverso" });
            }
            return righe;
        }

        // ------------------------------------------------------------------------------------------ autotest
        /// <summary>Prove per l'autotest: disegno di prova inventato (tre elementi, misure in pollici) come parole e, se c'e' Tesseract,
        /// come immagine letta con l'OCR. Nessun disegno di costruttori: i numeri sono di fantasia.</summary>
        public static List<Tuple<string, bool, string>> Prove()
        {
            var r = new List<Tuple<string, bool, string>>();
            // [testo, x, y] su una pagina 800 x 1000: diametri sopra, lunghezze sotto, posizione a destra
            var righe = new List<object[]>
            {
                new object[] { "Riflettore, Element #1", 520, 100 }, new object[] { "0.\"", 720, 100 },
                new object[] { ".250\"", 60, 88 }, new object[] { "375\"", 160, 88 }, new object[] { ".500\"", 260, 88 },
                new object[] { "30\"", 60, 112 }, new object[] { "40\"", 160, 112 }, new object[] { "48\"", 260, 112 },
                new object[] { ".375\" x 48\" inner liner (24\" each side)", 150, 140 },
                new object[] { "Driver, Element #2", 520, 240 }, new object[] { "44.5\"", 720, 240 },
                new object[] { ".250\"", 80, 228 }, new object[] { ".375\"", 170, 228 }, new object[] { ".500\"", 260, 228 },
                new object[] { "26.25\"", 80, 252 }, new object[] { "36\"", 170, 252 }, new object[] { "48\"", 260, 252 },
                new object[] { "Director, Element #3", 520, 380 }, new object[] { "97\"", 720, 380 },
                new object[] { ".375\"", 120, 368 }, new object[] { ".500\"", 260, 368 },
                new object[] { "50.5\"", 120, 392 }, new object[] { "48\"", 260, 392 },
            };
            var pg = new PaginaLetta { Larghezza = 800, Altezza = 1000 };
            foreach (var t in righe)
            {
                double x = Convert.ToDouble(t[1], CI);
                foreach (string w in ((string)t[0]).Split(' '))
                {
                    double lw = 7.5 * w.Length;
                    pg.Parole.Add(new ParolaLetta { Testo = w, X = x, Y = Convert.ToDouble(t[2], CI) - 7, W = lw, H = 14 });
                    x += lw + 6;
                }
            }
            var el = Analizza(pg, 0);
            string pos = string.Join(" ", el.Select(e => Fmt(e.Pos)));
            string t1 = el.Count > 0 ? TestoSpezzoni(el[0].Spezzoni) : "";
            bool ok1 = el.Count == 3 && pos == "0 44.5 97" && t1 == "0.5×48 · 0.375×40 · 0.25×30" && el[1].Alimentato && el.All(e => e.Dubbi.Count == 0);
            r.Add(Tuple.Create("Leggi progetto: riconoscimento di un disegno di prova", ok1, "posizioni " + pos + "; elemento 1 " + t1 + (el.Count > 1 && el[1].Alimentato ? "; alimentato il 2" : "; alimentato sbagliato") + "; dubbi " + el.Sum(e => e.Dubbi.Count)));
            // modello NEC
            bool ok2 = false; string d2 = "";
            try
            {
                var m = NecModel.Parse(CreaNec(el, 0, 29, "prova", "autotest"));
                var g = m.BuildGeometry();
                ok2 = g.N > 0 && m.Sources.Count == 1;
                d2 = g.N + " segmenti, " + m.Sources.Count + " sorgente";
                var conf = Confronta(el, 0, g);
                ok2 = ok2 && conf.Count == 3 && conf.All(c => c[3] == "uguale");
                d2 += "; confronto con se' stesso: " + conf.Count(c => c[3] == "uguale") + " su " + conf.Count + " uguali";
            }
            catch (Exception ex) { d2 = ex.Message; }
            r.Add(Tuple.Create("Leggi progetto: modello NEC dalla tabella", ok2, d2));
            // OCR della stessa pagina disegnata
            string exe = Tesseract();
            if (exe == null) { r.Add(Tuple.Create("Leggi progetto: OCR (Tesseract non installato, prova saltata)", true, ComeInstallare())); return r; }
            try
            {
                byte[] png;
                var info = new SkiaSharp.SKImageInfo(800, 1000);
                using (var surf = SkiaSharp.SKSurface.Create(info))
                using (var font = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.FromFamilyName("DejaVu Sans") ?? SkiaSharp.SKTypeface.Default, 15))
                using (var paint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black, IsAntialias = true })
                {
                    surf.Canvas.Clear(SkiaSharp.SKColors.White);
                    foreach (var t in righe) surf.Canvas.DrawText((string)t[0], Convert.ToSingle(t[1], CI), Convert.ToSingle(t[2], CI) + 5, font, paint);
                    using (var img = surf.Snapshot()) using (var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100)) png = data.ToArray();
                }
                string err;
                var po = Ocr(png, "prova", out err);
                if (po == null) { r.Add(Tuple.Create("Leggi progetto: OCR di un disegno di prova", false, err)); return r; }
                var eo = Analizza(po, 0);
                string poso = string.Join(" ", eo.Select(e => Fmt(e.Pos)));
                int tratti = eo.Sum(e => e.Spezzoni.Count);
                bool ok3 = poso == "0 44.5 97" && tratti >= 6;
                r.Add(Tuple.Create("Leggi progetto: OCR di un disegno di prova", ok3, "posizioni " + poso + ", " + tratti + " tratti su 8, " + po.Parole.Count + " parole lette (" + Path.GetFileName(exe) + ")"));
            }
            catch (Exception ex) { r.Add(Tuple.Create("Leggi progetto: OCR di un disegno di prova", false, ex.Message)); }
            return r;
        }

        /// <summary>Riga di comando (YA-NEC --leggi-progetto file [pagina]): pagine trovate ed elementi letti, per le prove.</summary>
        public static int RigaDiComando(string file, string scelta)
        {
            var l = file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? LeggiPdf(file) : new List<PaginaLetta> { new PaginaLetta { DaOcr = true, Png = File.ReadAllBytes(file), Descrizione = Path.GetFileName(file) } };
            for (int i = 0; i < l.Count; i++) Console.WriteLine(i + ": " + l[i].Descrizione + (l[i].DaOcr ? "" : ", " + l[i].NumeriInPollici + " misure in pollici"));
            int n; if (!int.TryParse(scelta ?? "", out n)) n = Proposta(l);
            if (n < 0 || n >= l.Count) return 1;
            var pg = l[n]; string err = null;
            if (pg.DaOcr) pg = Ocr(pg.Png, pg.Descrizione, out err);
            if (pg == null) { Console.WriteLine(err); return 2; }
            int u = UnitaProbabile(pg);
            Console.WriteLine("Pagina " + pg.Descrizione + ": " + pg.Parole.Count + " parole, unita' " + Unita[u] + ", frequenza " + FrequenzaDalTesto(pg.Parole.Select(q => q.Testo)));
            foreach (var e in Analizza(pg, u))
                Console.WriteLine(string.Format(CI, "{0,-34} {1,8}  {2}{3}", e.Nome, Fmt(e.Pos), TestoSpezzoni(e.Spezzoni), e.Dubbi.Count > 0 ? "   [" + string.Join("; ", e.Dubbi) + "]" : "") + (e.Alimentato ? "  (alimentato)" : ""));
            return 0;
        }
    }
}
