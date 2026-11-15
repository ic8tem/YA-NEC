// YA-NEC - importatori dei formati di Brian Beezley K6STI (approvati da IC8TEM il 13/11/2026):
//  - .YAG di YO (Yagi Optimizer): titolo, quota facoltativa ("Height h"), tre frequenze, "N elements, <unita'>", diametri dei
//    tratti (in mm, dal centro verso la punta), poi per ogni elemento la posizione sul boom e la lunghezza di ogni tratto della
//    META' elemento; l'ultimo tratto arriva alla punta. Elemento alimentato: il secondo (convenzione di YO: riflettore, radiatore,
//    direttori). Le righe dopo gli elementi sono note libere.
//  - .ANT di AO / NEC-Wires (formato descritto in ANTFILE.DOC, K6STI 1995): titolo, terreno ("Free Space [symmetric]" oppure
//    terreno con "N zones" e "er sigma(mS/m)"), frequenza con unita', "N [materiale] wires, <unita'>", simboli "nome = valore",
//    fili "segmenti x1 y1 z1 x2 y2 z2 diametro" (unita' attaccate ai numeri: ' " ft in m cm mm, calibri #AWG), sorgenti
//    ("current" = a corrente), carichi RLC / impedenza, linee di trasmissione (Z=, D=, S=, L=, V=, untwisted, short end1/end2).
// Il modello si costruisce come testo NEC e si legge con NecModel.Parse (stessa strada degli altri importatori).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace YaNec.Engine
{
    public static class K6stiImport
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        const double C0 = 299.792458;
        static string F(double v) { return v.ToString("0.#########", CI); }

        static double Unita(string u)
        {
            u = (u ?? "").Trim().ToLowerInvariant();
            if (u.StartsWith("mi")) return 1e-3;          // millimeters
            if (u.StartsWith("mm")) return 1e-3;
            if (u.StartsWith("ce") || u == "cm") return 1e-2;
            if (u.StartsWith("in") || u == "\"") return 0.0254;
            if (u.StartsWith("fe") || u.StartsWith("fo") || u == "ft" || u == "'") return 0.3048;
            if (u.StartsWith("me") || u == "m") return 1.0;
            throw new NecException("Unita' di misura non riconosciuta: " + u);
        }

        // ------------------------------------------------------------------------------------------ .YAG
        public static NecModel LoadYag(string path) { string y = YanecFile.Sostituto(path); if (y != null) return YanecFile.Carica(y); return ParseYag(File.ReadAllLines(path), Path.GetFileNameWithoutExtension(path)); }

        public static NecModel ParseYag(string[] righe, string nome = "YO")
        {
            List<string> L = new List<string>();
            foreach (string r in righe) L.Add(r.TrimEnd());
            int i = 0;
            string titolo = L.Count > 0 ? L[0].Trim() : nome;
            i = 1;
            double quota = 0;
            if (i < L.Count && L[i].TrimStart().StartsWith("Height", StringComparison.OrdinalIgnoreCase))
            {
                quota = double.Parse(L[i].Trim().Substring(6).Trim().Split(' ')[0], CI);
                i++;
            }
            // frequenze: una sola ("18.125   MHz", Hy-Gain 173BA) o quattro (minima, centro, massima, progetto) seguite da "MHz"
            List<double> fr = new List<double>();
            foreach (string t in L[i++].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                double vv;
                if (double.TryParse(t, NumberStyles.Float, CI, out vv)) fr.Add(vv);
            }
            if (fr.Count == 0) throw new NecException("File .YAG: manca la riga della frequenza.");
            double fc = fr.Count >= 2 ? fr[1] : fr[0];
            Match m = Regex.Match(L[i++], @"(\d+)\s+elements?\s*,\s*(\w+)", RegexOptions.IgnoreCase);
            if (!m.Success) throw new NecException("File .YAG: manca la riga \"N elements, unita'\".");
            int ne = int.Parse(m.Groups[1].Value, CI);
            double u = Unita(m.Groups[2].Value);
            quota *= u;
            List<double> diam = new List<double>();
            string rigaDiam = L[i++];
            foreach (Match md in Regex.Matches(rigaDiam, @"([\d.]+)\s*mm", RegexOptions.IgnoreCase)) diam.Add(double.Parse(md.Groups[1].Value, CI) / 1000);
            // file .YAG dell'ARRL Antenna Book (zipyagi.zip, N6BV): diametri senza "mm", nell'unita' della riga degli elementi
            if (diam.Count == 0)
                foreach (Match md in Regex.Matches(rigaDiam, @"(?<![\w.])(\d+\.?\d*|\.\d+)(?![\w.])")) diam.Add(double.Parse(md.Groups[1].Value, CI) * u);
            if (diam.Count == 0) throw new NecException("File .YAG: mancano i diametri dei tratti.");
            double lam = C0 / fc, seg = lam / 40;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM " + titolo.Replace("\t", " "));
            sb.AppendLine("CM importato da YO (K6STI) .YAG: " + F(fc) + " MHz, " + ne + " elementi, alimentato il secondo");
            sb.AppendLine("CE");
            int tag = 0, tagFeed = 0, segFeed = 0;
            double xPrima = double.NegativeInfinity;
            // 0.4.8: riga dei diametri che comincia con "Spacing" (ARRL 506-12H.YAG): la prima colonna degli elementi e' la distanza
            // dal precedente, non la posizione sul boom
            bool incrementale = rigaDiam.TrimStart().StartsWith("Spacing", StringComparison.OrdinalIgnoreCase);
            for (int e = 0; e < ne; e++)
            {
                if (i >= L.Count) throw new NecException("File .YAG: il file finisce prima dell'elemento " + (e + 1) + ".");
                string riga = L[i++];
                string[] v = riga.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (v.Length == 0) { e--; continue; }
                double x = double.Parse(v[0], CI) * u;
                if (incrementale && e > 0) x += xPrima;
                // riga di nuovi diametri tra gli elementi (Hy-Gain 66DX): molto rientrata, oppure "posizione" che non cresce
                int rientro = riga.Length - riga.TrimStart().Length;
                if (rientro >= 6 || (!incrementale && e > 0 && x <= xPrima))
                {
                    diam.Clear();
                    foreach (string t in v) diam.Add(double.Parse(t, CI) * u);
                    e--;
                    continue;
                }
                xPrima = x;
                List<double> tratti = new List<double>();
                for (int k = 1; k < v.Length; k++) tratti.Add(double.Parse(v[k], CI) * u);
                if (tratti.Count == 0) throw new NecException("File .YAG: elemento " + (e + 1) + " senza lunghezza.");
                // con un solo diametro c'e' un solo tratto (meta' lunghezza); con piu' diametri un tratto per diametro
                List<double[]> parti = new List<double[]>();    // fine (dal centro), raggio
                double y = 0;
                for (int k = 0; k < tratti.Count; k++) { y += tratti[k]; parti.Add(new[] { y, diam[Math.Min(k, diam.Count - 1)] / 2 }); }
                // tratto centrale intero, poi i tratti simmetrici
                double y0 = parti[0][0];
                int nc = Math.Max(3, (int)Math.Ceiling(2 * y0 / seg)); if (nc % 2 == 0) nc++;
                for (int k = parti.Count - 1; k >= 1; k--)
                    sb.AppendLine(string.Format(CI, "GW {0} {1} {2} {3} {4} {2} {5} {4} {6}", ++tag, Math.Max(1, (int)Math.Ceiling((parti[k][0] - parti[k - 1][0]) / seg)), F(x), F(-parti[k][0]), F(quota), F(-parti[k - 1][0]), F(parti[k][1])));
                sb.AppendLine(string.Format(CI, "GW {0} {1} {2} {3} {4} {2} {5} {4} {6}", ++tag, nc, F(x), F(-y0), F(quota), F(y0), F(parti[0][1])));
                if (e == Math.Min(1, ne - 1)) { tagFeed = tag; segFeed = (nc + 1) / 2; }
                for (int k = 1; k < parti.Count; k++)
                    sb.AppendLine(string.Format(CI, "GW {0} {1} {2} {3} {4} {2} {5} {4} {6}", ++tag, Math.Max(1, (int)Math.Ceiling((parti[k][0] - parti[k - 1][0]) / seg)), F(x), F(parti[k - 1][0]), F(quota), F(parti[k][0]), F(parti[k][1])));
            }
            sb.AppendLine("GE " + (quota > 0 ? "0" : "0"));
            if (quota > 0) sb.AppendLine("GN 2 0 0 0 13 0.005");
            sb.AppendLine("EX 0 " + tagFeed + " " + segFeed + " 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(fc) + " 0");
            sb.AppendLine("EN");
            NecModel mod = NecModel.Parse(sb.ToString());
            mod.SourceFormat = "YO";
            mod.Warnings.Add("Importato da YO (.YAG): alimentato il secondo elemento (convenzione di YO), " + (quota > 0 ? "quota " + F(quota) + " m su terreno medio." : "spazio libero."));
            List<string> note = new List<string>();
            for (; i < L.Count; i++) if (L[i].Trim().Length > 0) note.Add(L[i].Trim());
            if (note.Count > 0) mod.Warnings.Add("Note del file .YAG: " + string.Join(" | ", note));
            if (diam.Count > 1)
                mod.Warnings.Add("Elementi a tubi telescopici: per un risultato come YO, EZNEC e ARRL attiva la correzione Leeson (Impostazioni > Tubi telescopici). " +
                                 "Senza correzione il NEC-2 sbaglia di molto (ARRL Antenna Book 24a, tab. 8.6).");
            return mod;
        }

        // ------------------------------------------------------------------------------------------ .ANT (AO / NEC-Wires)
        public static NecModel LoadAnt(string path) { string y = YanecFile.Sostituto(path); if (y != null) return YanecFile.Carica(y); return ParseAnt(File.ReadAllLines(path)); }

        static string Pulisci(string r) { int q = r.IndexOf(';'); return (q >= 0 ? r.Substring(0, q) : r).Trim(); }

        // 0.5.0 (IC8TEM 15/11/2026 14:34: "trattiamo i files di k6sti? se si allora si devono poter aprire"): il lettore .ANT copre
        // tutto ANTFILE.DOC (K6STI 1995): simboli con espressioni (+ - * /, parentesi, SIN COS in gradi, SQR, unita' attaccate ai
        // numeri, calibri #AWG), simboli preceduti dal segno meno, simboli anche per sorgenti, carichi e linee, Shift e Rotate,
        // carichi di Laplace, condensatore con perdite (Q senza L), resistivita' / conducibilita' IACS / permeabilita', materiale
        // del singolo filo, modo simmetrico con la sola meta' +Y del modello.

        /// <summary>Simbolo (nomi sensibili alle maiuscole come in NEC-Wires; se manca, si accetta lo stesso nome con altre maiuscole).</summary>
        static bool Simbolo(Dictionary<string, double> sym, string nome, out double v)
        {
            if (sym.TryGetValue(nome, out v)) return true;
            foreach (KeyValuePair<string, double> kv in sym) if (string.Equals(kv.Key, nome, StringComparison.OrdinalIgnoreCase)) { v = kv.Value; return true; }
            v = 0;
            return false;
        }

        /// <summary>Numero con unita' facoltativa attaccata (' " ft in m cm mm), calibro #AWG o simbolo (anche con il segno meno).</summary>
        static double Dim(string t, double u, Dictionary<string, double> sym, bool diametro = false)
        {
            t = t.Trim();
            if (t.StartsWith("#")) return 2 * Expr.RaggioAwg(int.Parse(t.Substring(1), CI));
            double s;
            if (Simbolo(sym, t, out s)) return s * u;
            if ((t.StartsWith("-") || t.StartsWith("+")) && Simbolo(sym, t.Substring(1), out s)) return (t[0] == '-' ? -s : s) * u;
            Match m = Regex.Match(t, @"^([-+]?[\d.]+(?:[eE][-+]?\d+)?)(.*)$");
            if (!m.Success) throw new NecException("Valore non valido nel file .ANT: " + t);
            double v = double.Parse(m.Groups[1].Value, CI);
            string un = m.Groups[2].Value.Trim();
            return un.Length == 0 ? v * u : v * Unita(un);
        }

        /// <summary>Valore di un'espressione di NEC-Wires nelle unita' del file (un numero con unita' attaccata si converte; #AWG = diametro).</summary>
        static double Valuta(string e, double u, Dictionary<string, double> sym)
        {
            List<string> tk = new List<string>();
            foreach (Match m in Regex.Matches(e, @"#\d+|\d*\.?\d+(?:[eE][-+]?\d+)?(?:'|""|ft|in|mm|cm|m)?(?![A-Za-z])|[A-Za-z_]\w*|[-+*/()]"))
                tk.Add(m.Value);
            int pos = 0;
            Func<double> somma = null, fattore = null;
            fattore = () =>
            {
                if (pos >= tk.Count) throw new NecException("File .ANT: espressione incompleta: " + e);
                string t = tk[pos++];
                if (t == "-") return -fattore();
                if (t == "+") return fattore();
                if (t == "(") { double v = somma(); if (pos < tk.Count && tk[pos] == ")") pos++; return v; }
                if (t.StartsWith("#")) return 2 * Expr.RaggioAwg(int.Parse(t.Substring(1), CI)) / u;
                string tu = t.ToUpperInvariant();
                if ((tu == "SIN" || tu == "COS" || tu == "SQR") && pos < tk.Count && tk[pos] == "(")
                {
                    double a = fattore();
                    return tu == "SIN" ? Math.Sin(a * Math.PI / 180) : tu == "COS" ? Math.Cos(a * Math.PI / 180) : Math.Sqrt(a);
                }
                Match n = Regex.Match(t, @"^(\d*\.?\d+(?:[eE][-+]?\d+)?)(.*)$");
                if (n.Success && (char.IsDigit(t[0]) || t[0] == '.'))
                {
                    double v = double.Parse(n.Groups[1].Value, CI);
                    return n.Groups[2].Value.Length == 0 ? v : v * Unita(n.Groups[2].Value) / u;
                }
                double sv;
                if (Simbolo(sym, t, out sv)) return sv;
                throw new NecException("File .ANT: simbolo non definito: " + t);
            };
            Func<double> prodotto = () =>
            {
                double v = fattore();
                while (pos < tk.Count && (tk[pos] == "*" || tk[pos] == "/")) { string o = tk[pos++]; double w = fattore(); v = o == "*" ? v * w : v / w; }
                return v;
            };
            somma = () =>
            {
                double v = prodotto();
                while (pos < tk.Count && (tk[pos] == "+" || tk[pos] == "-")) { string o = tk[pos++]; double w = prodotto(); v = o == "+" ? v + w : v - w; }
                return v;
            };
            double r = somma();
            if (pos < tk.Count) throw new NecException("File .ANT: espressione non valida: " + e);
            return r;
        }

        /// <summary>Righe "nome = espressione": definisce i simboli e avanza.</summary>
        static void Simboli(List<string> L, ref int i, double u, Dictionary<string, double> sym)
        {
            while (i < L.Count && Regex.IsMatch(L[i], @"^[A-Za-z_]\w*\s*=") && !Regex.IsMatch(L[i], @"^wire\b", RegexOptions.IgnoreCase))
            {
                int q = L[i].IndexOf('=');
                sym[L[i].Substring(0, q).Trim()] = Valuta(L[i].Substring(q + 1), u, sym);
                i++;
            }
        }

        /// <summary>Sostituisce i simboli in una riga di sorgente, carico o linea con il loro valore (le chiavi come Z= restano).</summary>
        static string ConSimboli(string r, Dictionary<string, double> sym)
        {
            if (sym.Count == 0) return r;
            return Regex.Replace(r, @"(?<![\w.])(-?)([A-Za-z_]\w*)(?![\w=])", m =>
            {
                double v;
                if (!Simbolo(sym, m.Groups[2].Value, out v) || Regex.IsMatch(m.Groups[2].Value, @"^(wire|center|end1|end2|untwisted|short|current|pf|nf|uf|f|nh|uh|mh|h|ohms?)$", RegexOptions.IgnoreCase)) return m.Value;
                return F(m.Groups[1].Value == "-" ? -v : v);
            });
        }

        /// <summary>Conducibilita' (S/m) da una frase di materiale di NEC-Wires; 0 = conduttore perfetto. mu = permeabilita' relativa.</summary>
        static double Materiale(string t, out double mu)
        {
            mu = 1;
            t = (t ?? "").Trim().ToLowerInvariant();
            Match mp = Regex.Match(t, @"\bper\w*\s+([\d.]+(?:e[-+]?\d+)?)");
            if (mp.Success) mu = double.Parse(mp.Groups[1].Value, CI);
            Match mr = Regex.Match(t, @"\bres\w*\s+([\d.]+(?:e[-+]?\d+)?)");
            if (mr.Success) return 1 / double.Parse(mr.Groups[1].Value, CI);
            Match mc = Regex.Match(t, @"\bconductivity\s+([\d.]+(?:e[-+]?\d+)?)");
            if (mc.Success) return double.Parse(mc.Groups[1].Value, CI) / 100 / 1.7241e-8;   // IACS: % del rame ricotto (1,7241e-8 ohm m)
            return t.StartsWith("copper") ? 1 / 1.7241e-8 : t.StartsWith("alum") ? 1 / 2.655e-8 : t.StartsWith("silver") ? 1 / 1.59e-8 :
                   t.StartsWith("6063") ? 1 / 3.25e-8 : t.StartsWith("6061") ? 1 / 4.01e-8 : t.StartsWith("brass") ? 1 / 6.4e-8 :
                   t.StartsWith("phosphor") ? 1 / 1.1e-7 : t.StartsWith("steel") ? 1 / 7.2e-7 : 0;
        }

        static double[] Ruota(double[] p, char asse, double gradi)
        {
            double a = gradi * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
            double x = p[0], y = p[1], z = p[2];
            // ANTFILE.DOC: positivo intorno a X = +Z verso +Y; intorno a Y = +X verso +Z; intorno a Z = +Y verso +X
            if (asse == 'x') return new[] { x, y * c + z * s, -y * s + z * c };
            if (asse == 'y') return new[] { x * c - z * s, y, z * c + x * s };
            return new[] { x * c + y * s, -x * s + y * c, z };
        }

        class Posto { public int[] P; public string Fmt; public double Re, Im; public bool Sorgente; }

        public static NecModel ParseAnt(string[] righe)
        {
            List<string> L = new List<string>();
            foreach (string r in righe) { string p = Pulisci(r); if (p.Length > 0) L.Add(p); }
            if (L.Count < 4) throw new NecException("File .ANT: troppo corto (servono titolo, terreno, frequenza e fili).");
            int i = 0;
            Func<int, string> Riga = k => k < L.Count ? L[k] : "";
            string titolo = L[i++];
            string terreno = L[i++];
            bool libero = terreno.StartsWith("free space", StringComparison.OrdinalIgnoreCase);
            bool simmetrico = libero && terreno.IndexOf("sym", StringComparison.OrdinalIgnoreCase) >= 0;
            double er = 13, sigma = 0.005;
            bool perfetto = false;
            List<string> avvisi = new List<string>();
            Match mz = Regex.Match(Riga(i), @"^(\d+)\s+zones?", RegexOptions.IgnoreCase);
            if (!libero && mz.Success)
            {
                i++;
                int nz = int.Parse(mz.Groups[1].Value, CI);
                if (nz == 0) perfetto = true;
                else
                {
                    string[] z = Riga(i++).Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    er = double.Parse(z[0], CI); sigma = double.Parse(z[1], CI) / 1000;
                    if (nz > 1) { avvisi.Add("Piu' zone di terreno (funzione di AO): usata solo la prima."); i += nz - 1; }
                }
            }
            Match mf = Regex.Match(Riga(i++), @"([\d.]+)\s*(k|m|g)?hz", RegexOptions.IgnoreCase);
            if (!mf.Success) throw new NecException("File .ANT: manca la frequenza (es. \"14.2 MHz\").");
            double f = double.Parse(mf.Groups[1].Value, CI);
            string pf = mf.Groups[2].Value.ToLowerInvariant();
            if (pf == "k") f /= 1000; else if (pf == "g") f *= 1000;
            Match mw = Regex.Match(Riga(i++), @"^(\d+)\s+(.*?)wires?\s*,\s*(\S+)", RegexOptions.IgnoreCase);
            if (!mw.Success) throw new NecException("File .ANT: manca la riga \"N wires, unita'\".");
            int nw = int.Parse(mw.Groups[1].Value, CI);
            double u = Unita(mw.Groups[3].Value);
            double muFilo;
            double sigFilo = Materiale(mw.Groups[2].Value, out muFilo);
            Dictionary<string, double> sym = new Dictionary<string, double>(StringComparer.Ordinal);
            Simboli(L, ref i, u, sym);
            double lam = C0 / f, seg = lam / 40;
            List<double[]> fili = new List<double[]>();
            List<int> nseg = new List<int>();
            List<double> sigma1 = new List<double>(), mu1 = new List<double>();
            // Shift e Rotate: elenco nell'ordine del file; si applicano dall'ultimo al primo (dal filo verso l'esterno). Uno Shift o un
            // Rotate sullo stesso asse di uno gia' attivo ne cambia il valore; "Shift End" e "Rotate End" li tolgono tutti.
            List<Tuple<char, char, double>> trasf = new List<Tuple<char, char, double>>();
            Action<char, char, double> metti = (tipo, asse, val) =>
            {
                int j = trasf.FindIndex(t => t.Item1 == tipo && t.Item2 == asse);
                if (j >= 0) trasf[j] = Tuple.Create(tipo, asse, val); else trasf.Add(Tuple.Create(tipo, asse, val));
            };
            bool spostati = false;
            for (int k = 0; k < nw; )
            {
                if (i >= L.Count) throw new NecException("File .ANT: il file finisce prima del filo " + (k + 1) + ".");
                string r0 = L[i];
                Match mt0 = Regex.Match(r0, @"^(shift|rotate)\b(.*)$", RegexOptions.IgnoreCase);
                if (mt0.Success)
                {
                    i++; spostati = true;
                    char tipo = char.ToLowerInvariant(mt0.Groups[1].Value[0]) == 's' ? 's' : 'r';
                    string resto = mt0.Groups[2].Value.Trim();
                    if (resto.StartsWith("end", StringComparison.OrdinalIgnoreCase)) { trasf.RemoveAll(t => t.Item1 == tipo); continue; }
                    string[] it = resto.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j + 1 < it.Length; j += 2)
                    {
                        char asse = char.ToLowerInvariant(it[j][0]);
                        if (asse != 'x' && asse != 'y' && asse != 'z') throw new NecException("File .ANT: asse non valido in " + r0);
                        double val;
                        if (tipo == 's') val = Dim(it[j + 1], u, sym);
                        else if (!double.TryParse(it[j + 1], NumberStyles.Float, CI, out val))
                        {
                            string nome = it[j + 1].TrimStart('-', '+');
                            if (!Simbolo(sym, nome, out val)) throw new NecException("File .ANT: valore non valido in " + r0);
                            if (it[j + 1].StartsWith("-")) val = -val;
                        }
                        metti(tipo, asse, val);
                    }
                    continue;
                }
                if (Regex.IsMatch(r0, @"^[A-Za-z_]\w*\s*=")) { Simboli(L, ref i, u, sym); continue; }
                i++;
                string[] v = r0.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (v.Length < 8) throw new NecException("File .ANT: filo " + (k + 1) + " incompleto.");
                double[] w = new double[7];
                for (int j = 0; j < 6; j++) w[j] = Dim(v[1 + j], u, sym);
                w[6] = Dim(v[7], u, sym, true) / 2;
                double[] pa = { w[0], w[1], w[2] }, pb = { w[3], w[4], w[5] };
                for (int j = trasf.Count - 1; j >= 0; j--)
                {
                    Tuple<char, char, double> t = trasf[j];
                    if (t.Item1 == 's') { int ax = t.Item2 - 'x'; pa[ax] += t.Item3; pb[ax] += t.Item3; }
                    else { pa = Ruota(pa, t.Item2, t.Item3); pb = Ruota(pb, t.Item2, t.Item3); }
                }
                w = new[] { pa[0], pa[1], pa[2], pb[0], pb[1], pb[2], w[6] };
                int ns;
                double nsv;
                if (!int.TryParse(v[0], NumberStyles.Integer, CI, out ns))
                {
                    if (!Simbolo(sym, v[0].TrimStart('-'), out nsv)) throw new NecException("File .ANT: numero di segmenti non valido: " + v[0]);
                    ns = (int)Math.Round(v[0].StartsWith("-") ? -nsv : nsv);
                }
                double len = Math.Sqrt(Math.Pow(w[3] - w[0], 2) + Math.Pow(w[4] - w[1], 2) + Math.Pow(w[5] - w[2], 2));
                int auto = Math.Max(1, (int)Math.Ceiling(len / seg));
                nseg.Add(ns > 0 ? Math.Max(ns, auto) : (ns < 0 ? -ns : auto));
                fili.Add(w);
                // materiale del singolo filo (frase dopo il diametro)
                double mu = muFilo, sg = sigFilo;
                if (v.Length > 8) sg = Materiale(string.Join(" ", v, 8, v.Length - 8), out mu);
                sigma1.Add(sg); mu1.Add(mu);
                k++;
            }
            if (spostati) avvisi.Add("Shift e Rotate di AO applicati ai fili (ANTFILE.DOC: dal filo verso l'esterno, nell'ordine inverso del file).");
            // permeabilita': il NEC-2 (LD 5) non la prevede; con l'effetto pelle la stessa resistenza superficiale si ottiene con sigma / mu
            for (int k = 0; k < fili.Count; k++) if (mu1[k] != 1 && sigma1[k] > 0) sigma1[k] /= mu1[k];
            if (mu1.Exists(x => x != 1)) avvisi.Add("Permeabilita' del materiale (ferro, acciaio): il NEC-2 non la prevede; usata la conducibilita' divisa per la permeabilita' (stessa resistenza per effetto pelle).");

            // modo simmetrico di AO: se il file da' solo la meta' +Y, l'altra meta' e' l'immagine speculare (piano X-Z)
            bool soloMeta = false;
            List<int> immagine = new List<int>();          // per ogni filo: indice del filo immagine, -1 se nessuno, -2 se unito all'immagine
            List<int> sulPiano = new List<int>();          // fili uniti: estremo originale (1 o 2) che stava sul piano dell'immagine
            if (simmetrico)
            {
                bool negativi = fili.Exists(w => w[1] < -1e-9 || w[4] < -1e-9);
                if (negativi) avvisi.Add("Modo simmetrico di AO (\"Free Space Symmetric\") con il modello intero nel file: calcolato intero, come lo descrive il file.");
                else
                {
                    soloMeta = true;
                    int n0 = fili.Count;
                    for (int k = 0; k < n0; k++)
                    {
                        double[] w = fili[k];
                        bool tocca1 = Math.Abs(w[1]) < 1e-9, tocca2 = Math.Abs(w[4]) < 1e-9;
                        bool lungoY = Math.Abs(w[3] - w[0]) < 1e-9 && Math.Abs(w[5] - w[2]) < 1e-9;
                        sulPiano.Add(tocca1 ? 1 : (tocca2 ? 2 : 0));
                        if (tocca1 && tocca2) { immagine.Add(-1); continue; }                 // filo nel piano dell'immagine
                        if (lungoY && (tocca1 || tocca2))
                        {
                            // filo lungo Y che parte dal piano: si unisce alla sua immagine in un filo solo, segmenti dispari (centro sul piano)
                            fili[k] = tocca1 ? new[] { w[3], -w[4], w[5], w[3], w[4], w[5], w[6] } : new[] { w[0], w[1], w[2], w[0], -w[1], w[2], w[6] };
                            nseg[k] = 2 * nseg[k] + 1;
                            immagine.Add(-2);
                            continue;
                        }
                        fili.Add(new[] { w[0], -w[1], w[2], w[3], -w[4], w[5], w[6] });
                        nseg.Add(nseg[k]); sigma1.Add(sigma1[k]); mu1.Add(mu1[k]);
                        immagine.Add(fili.Count - 1);
                    }
                    avvisi.Add("Modo simmetrico di AO (\"Free Space Symmetric\") con la sola meta' +Y nel file: l'altra meta' e' l'immagine speculare sul piano X-Z, " +
                        "con la corrente immagine come in AO (riflessa e poi invertita); i fili lungo Y che partono dal piano sono uniti alla loro immagine.");
                }
            }
            // sorgenti, carichi, linee: si identificano con "Wire n, center | end1 | end2"
            Func<string, int[]> posto = t =>
            {
                Match q = Regex.Match(t, @"wire\s*(\d+)\s*,?\s*(center|end1|end2)", RegexOptions.IgnoreCase);
                if (!q.Success) throw new NecException("File .ANT: posizione non riconosciuta: " + t);
                int w = int.Parse(q.Groups[1].Value, CI);
                if (w < 1 || w > nw) throw new NecException("File .ANT: il filo " + w + " non esiste (" + nw + " fili).");
                string dove = q.Groups[2].Value.ToLowerInvariant();
                if (dove == "center" && nseg[w - 1] % 2 == 0 && !(soloMeta && immagine[w - 1] == -2)) nseg[w - 1]++;
                return new[] { w, dove == "center" ? 0 : (dove == "end1" ? 1 : 2) };
            };
            List<Posto> posti = new List<Posto>();
            bool correnti = false;
            Match ms = Regex.Match(Riga(i), @"^(\d+)\s+(current\s+)?sources?", RegexOptions.IgnoreCase);
            if (ms.Success)
            {
                correnti = L[i].IndexOf("current", StringComparison.OrdinalIgnoreCase) >= 0;
                i++;
                Simboli(L, ref i, u, sym);
                for (int k = 0; k < int.Parse(ms.Groups[1].Value, CI); k++)
                {
                    string r = ConSimboli(Riga(i++), sym);
                    int[] p = posto(r);
                    string resto = Regex.Replace(r, @"wire\s*\d+\s*,?\s*(center|end1|end2)", "", RegexOptions.IgnoreCase);
                    string[] v = resto.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    double amp = v.Length > 0 ? double.Parse(v[0], CI) : 1, fase = v.Length > 1 ? double.Parse(v[1], CI) : 0;
                    posti.Add(new Posto { P = p, Fmt = "EX 0 {T} {S} 0 {R} {I}", Re = amp * Math.Cos(fase * Math.PI / 180), Im = amp * Math.Sin(fase * Math.PI / 180), Sorgente = true });
                }
            }
            Match ml = Regex.Match(Riga(i), @"^(\d+)\s+loads?", RegexOptions.IgnoreCase);
            if (ml.Success)
            {
                i++;
                Simboli(L, ref i, u, sym);
                int n = int.Parse(ml.Groups[1].Value, CI);
                string modo = "rlc";
                double w0 = 2 * Math.PI * f * 1e6;
                for (int k = 0; k < n; k++)
                {
                    if (i < L.Count && !L[i].StartsWith("wire", StringComparison.OrdinalIgnoreCase))
                    {
                        modo = L[i].StartsWith("laplace", StringComparison.OrdinalIgnoreCase) ? "laplace" : "impedenza";
                        i++;
                    }
                    string r = ConSimboli(Riga(i++), sym);
                    int[] p = posto(r);
                    string resto = Regex.Replace(r, @"wire\s*\d+\s*,?\s*(center|end1|end2)", "", RegexOptions.IgnoreCase);
                    if (modo == "laplace")
                    {
                        // numeratore e denominatore in s (da s^0), ohm, henry e farad: Z(j w) alla frequenza del modello
                        Func<string, System.Numerics.Complex> poli = riga =>
                        {
                            string[] c = ConSimboli(riga, sym).Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            System.Numerics.Complex sum = 0, sp = 1, sj = new System.Numerics.Complex(0, w0);
                            foreach (string x in c) { sum += double.Parse(x, NumberStyles.Float, CI) * sp; sp *= sj; }
                            return sum;
                        };
                        System.Numerics.Complex num = poli(Riga(i++)), den = poli(Riga(i++));
                        if (den == System.Numerics.Complex.Zero) throw new NecException("File .ANT: carico di Laplace con denominatore nullo a " + F(f) + " MHz.");
                        System.Numerics.Complex z = num / den;
                        posti.Add(new Posto { P = p, Fmt = "LD 4 {T} {S} {S} {R} {I}", Re = z.Real, Im = z.Imaginary });
                        avvisi.Add("Carico di Laplace di AO sul filo " + p[0] + ": scritto come impedenza " + F(Math.Round(z.Real, 4)) + " " + (z.Imaginary < 0 ? "-" : "+") + "j" + F(Math.Round(Math.Abs(z.Imaginary), 4)) +
                            " ohm a " + F(f) + " MHz (vale solo a questa frequenza; il NEC-2 non ha i carichi di Laplace).");
                        continue;
                    }
                    if (modo == "impedenza")
                    {
                        string[] v = resto.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        posti.Add(new Posto { P = p, Fmt = "LD 4 {T} {S} {S} {R} {I}", Re = double.Parse(v[0], CI), Im = v.Length > 1 ? double.Parse(v[1], CI) : 0 });
                        continue;
                    }
                    double R = 0, Lh = 0, Cf = 0, Q = 0;
                    foreach (Match c in Regex.Matches(resto, @"([\d.]+(?:[eE][-+]?\d+)?)\s*(pf|nf|uf|f|nh|uh|mh|h|ohms?)\b|q\s*=\s*([\d.]+)", RegexOptions.IgnoreCase))
                    {
                        if (c.Groups[3].Success) { Q = double.Parse(c.Groups[3].Value, CI); continue; }
                        double val = double.Parse(c.Groups[1].Value, CI);
                        string un = c.Groups[2].Value.ToLowerInvariant();
                        if (un.StartsWith("ohm")) R = val;
                        else if (un == "pf") Cf = val * 1e-12; else if (un == "nf") Cf = val * 1e-9; else if (un == "uf") Cf = val * 1e-6; else if (un == "f") Cf = val;
                        else if (un == "nh") Lh = val * 1e-9; else if (un == "uh") Lh = val * 1e-6; else if (un == "mh") Lh = val * 1e-3; else if (un == "h") Lh = val;
                    }
                    if (Q > 0 && Lh > 0) R = w0 * Lh / Q;
                    if (Q > 0 && Lh == 0 && Cf > 0)
                    {
                        // condensatore con perdite (ANTFILE.DOC: senza L, Q da' la R in parallelo): R = Q / (w C) in parallelo a C (LD 1)
                        posti.Add(new Posto { P = p, Fmt = "LD 1 {T} {S} {S} " + F(Q / (w0 * Cf)) + " 0 " + F(Cf) });
                        continue;
                    }
                    if (Cf > 0 && (Lh > 0 || R > 0))
                    {
                        // trappola: R in serie a L, il tutto in parallelo a C -> impedenza alla frequenza del modello (LD 4)
                        System.Numerics.Complex zrl = new System.Numerics.Complex(R, w0 * Lh), zc = new System.Numerics.Complex(0, -1 / (w0 * Cf));
                        System.Numerics.Complex z = zrl * zc / (zrl + zc);
                        posti.Add(new Posto { P = p, Fmt = "LD 4 {T} {S} {S} {R} {I}", Re = z.Real, Im = z.Imaginary });
                        avvisi.Add("Trappola RLC di AO sul filo " + p[0] + ": scritta come impedenza a " + F(f) + " MHz (vale solo a questa frequenza).");
                    }
                    else posti.Add(new Posto { P = p, Fmt = "LD 0 {T} {S} {S} " + F(R) + " " + F(Lh) + " " + F(Cf) });
                }
            }
            Match mt = Regex.Match(Riga(i), @"^(\d+)\s+(transmission\s+)?lines?", RegexOptions.IgnoreCase);
            List<Tuple<int[], int[], string>> linee = new List<Tuple<int[], int[], string>>();
            if (mt.Success)
            {
                i++;
                Simboli(L, ref i, u, sym);
                for (int k = 0; k < int.Parse(mt.Groups[1].Value, CI); k++)
                {
                    string r = ConSimboli(Riga(i++), sym);
                    MatchCollection ws = Regex.Matches(r, @"wire\s*(\d+)\s*,?\s*(center|end1|end2)", RegexOptions.IgnoreCase);
                    if (ws.Count < 2) throw new NecException("File .ANT: linea senza i due estremi: " + r);
                    int[] a = posto(ws[0].Value), b = posto(ws[1].Value);
                    Func<string, double> par = (k2) => { Match q = Regex.Match(r, @"\b" + k2 + @"\s*=\s*(\S+)", RegexOptions.IgnoreCase); return q.Success ? Dim(q.Groups[1].Value, u, sym) : double.NaN; };
                    double z0 = double.NaN;
                    Match qz = Regex.Match(r, @"\bZ\s*=\s*([\d.]+)", RegexOptions.IgnoreCase);
                    if (qz.Success) z0 = double.Parse(qz.Groups[1].Value, CI);
                    else
                    {
                        double d = par("D"), s = par("S");
                        if (double.IsNaN(d) || double.IsNaN(s)) throw new NecException("File .ANT: linea senza Z= o D= e S=: " + r);
                        z0 = 120 * Math.Log(2 * s / d + Math.Sqrt(4 * s * s / (d * d) - 1));   // 120 acosh(s/d) per i due conduttori
                    }
                    double len = par("L");
                    Match qv = Regex.Match(r, @"\bV\s*=\s*([\d.]+)", RegexOptions.IgnoreCase);
                    double vf = qv.Success ? double.Parse(qv.Groups[1].Value, CI) : 1;
                    bool diritta = r.IndexOf("untwisted", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (Regex.IsMatch(r, @"short", RegexOptions.IgnoreCase)) avvisi.Add("Linea in corto di AO (stub): il corto va modellato con il filo indicato nel file.");
                    linee.Add(Tuple.Create(a, b, string.Format(CI, "{0} {1}", F(diritta ? z0 : -z0), double.IsNaN(len) ? "0" : F(len / vf))));
                }
            }
            // segmento di una posizione; per i fili uniti all'immagine (simmetrico, meta' +Y) il filo va dall'immagine all'originale
            // (o dall'originale all'immagine): l'estremo che stava sul piano diventa il segmento centrale
            Func<int[], int> segFinale = p =>
            {
                int n = nseg[p[0] - 1];
                if (soloMeta && immagine[p[0] - 1] == -2)
                {
                    int c = (n + 1) / 2, h = (n - 1) / 2;
                    double[] w = fili[p[0] - 1];
                    bool versoPiu = w[4] > w[1];
                    if (p[1] == 0) return versoPiu ? c + (h + 1) / 2 : c - (h + 1) / 2;
                    if (p[1] == sulPiano[p[0] - 1]) return c;
                    return versoPiu ? n : 1;
                }
                return p[1] == 0 ? (n + 1) / 2 : (p[1] == 1 ? 1 : n);
            };
            // scrittura NEC
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM " + titolo);
            sb.AppendLine("CM importato da AO / NEC-Wires (K6STI) .ANT");
            if (correnti) sb.AppendLine("CM YA-NEC CORRENTI=1");
            sb.AppendLine("CE");
            for (int k = 0; k < fili.Count; k++)
            {
                double[] w = fili[k];
                sb.AppendLine(string.Format(CI, "GW {0} {1} {2} {3} {4} {5} {6} {7} {8}", k + 1, nseg[k], F(w[0]), F(w[1]), F(w[2]), F(w[3]), F(w[4]), F(w[5]), F(w[6])));
            }
            bool aTerra = false;
            foreach (double[] w in fili) if (Math.Abs(w[2]) < 1e-9 || Math.Abs(w[5]) < 1e-9) aTerra = true;
            sb.AppendLine(libero ? "GE 0" : (aTerra ? "GE 1" : "GE 0"));
            if (!libero) sb.AppendLine(perfetto ? "GN 1" : string.Format(CI, "GN 0 0 0 0 {0} {1}", F(er), F(sigma)));
            // conducibilita' dei fili: una scheda per tutti se e' la stessa, altrimenti filo per filo
            bool uguali = sigma1.TrueForAll(x => Math.Abs(x - sigma1[0]) < 1e-6 * Math.Max(1, sigma1[0]));
            if (uguali) { if (sigma1.Count > 0 && sigma1[0] > 0) sb.AppendLine("LD 5 0 0 0 " + F(sigma1[0])); }
            else for (int k = 0; k < fili.Count; k++) if (sigma1[k] > 0) sb.AppendLine(string.Format(CI, "LD 5 {0} 0 0 {1}", k + 1, F(sigma1[k])));
            Func<Posto, int, int, double, string> riga = (q, tag, sg, segno) => q.Fmt.Replace("{T}", tag.ToString(CI)).Replace("{S}", sg.ToString(CI))
                .Replace("{R}", F(segno * q.Re)).Replace("{I}", F(segno * q.Im));
            foreach (Posto q in posti)
            {
                sb.AppendLine(riga(q, q.P[0], segFinale(q.P), 1));
                if (!soloMeta) continue;
                int im = immagine[q.P[0] - 1];
                if (im >= 0)
                {
                    // immagine sul filo speculare: stesso segmento; sorgente con il segno cambiato (corrente riflessa e invertita, ANTFILE.DOC)
                    if (q.P[1] != 0 && Math.Abs(fili[q.P[0] - 1][q.P[1] == 1 ? 1 : 4]) < 1e-9)
                    {
                        avvisi.Add("Modo simmetrico: sorgente o carico sul filo " + q.P[0] + " proprio sul piano dell'immagine; messo solo sul filo originale (in AO sono due segmenti corti ai due lati).");
                        continue;
                    }
                    sb.AppendLine(riga(q, im + 1, segFinale(q.P), q.Sorgente ? -1 : 1));
                }
                else if (im == -2 && segFinale(q.P) != (nseg[q.P[0] - 1] + 1) / 2)
                {
                    // filo unito alla sua immagine: posizione speculare sullo stesso filo, corrente in fase (filo parallelo a Y)
                    sb.AppendLine(riga(q, q.P[0], nseg[q.P[0] - 1] + 1 - segFinale(q.P), 1));
                }
            }
            foreach (Tuple<int[], int[], string> t in linee)
            {
                sb.AppendLine(string.Format(CI, "TL {0} {1} {2} {3} {4} 0 0 0 0", t.Item1[0], segFinale(t.Item1), t.Item2[0], segFinale(t.Item2), t.Item3));
                if (soloMeta)
                {
                    int ia = immagine[t.Item1[0] - 1], ib = immagine[t.Item2[0] - 1];
                    if (ia >= 0 && ib >= 0) sb.AppendLine(string.Format(CI, "TL {0} {1} {2} {3} {4} 0 0 0 0", ia + 1, segFinale(t.Item1), ib + 1, segFinale(t.Item2), t.Item3));
                    else avvisi.Add("Modo simmetrico: la linea tra i fili " + t.Item1[0] + " e " + t.Item2[0] + " tocca il piano dell'immagine; l'immagine della linea non e' stata aggiunta.");
                }
            }
            sb.AppendLine("FR 0 1 0 0 " + F(f) + " 0");
            sb.AppendLine("EN");
            NecModel mod = NecModel.Parse(sb.ToString());
            mod.SourceFormat = "AO";
            foreach (string a in avvisi) mod.Warnings.Add(a);
            if (!libero && !perfetto) mod.Warnings.Add("Terreno di AO: er " + F(er) + ", sigma " + F(sigma) + " S/m (coefficiente di riflessione, come il NEC-2 di NEC-Wires).");
            return mod;
        }
    }
}
