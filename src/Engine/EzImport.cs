// YA-NEC - importazione dei modelli AutoEZ / MultiNEC (.weq, testo a colonne) ed EZNEC (.ez, binario).
// I due formati non sono documentati: la lettura e' ricavata dai file di prova (12/11/2026) ed e' prudente:
// tutto quello che non e' riconosciuto con certezza viene segnalato negli avvisi, mai indovinato in silenzio.
// Segmentazione: quella del file (anche le formule di AutoEZ, come =ODD(WireLen / WL * 20)).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace YaNec.Engine
{
    public static class EzImport
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        static double UnitToMeters(string u, double fMHz)
        {
            switch ((u ?? "").Trim().ToLowerInvariant())
            {
                case "m": return 1;
                case "mm": return 0.001;
                case "cm": return 0.01;
                case "ft": return 0.3048;
                case "in": return 0.0254;
                case "wl": return 299.792458 / fMHz;
            }
            return double.NaN;
        }

        static int SegOf(int segs, double pct)
        {
            int s = (int)Math.Floor(pct / 100.0 * segs) + 1;
            return Math.Max(1, Math.Min(segs, s));
        }

        static void AddWire(NecModel m, int tag, int segs, double[] p, double radius)
        {
            m.Geometry.Add(new GeomCard { Id = "GW", I1 = tag, I2 = segs, F1 = p[0], F2 = p[1], F3 = p[2], F4 = p[3], F5 = p[4], F6 = p[5], F7 = radius });
        }

        // =============================================================================================
        // AutoEZ / MultiNEC .weq
        // riga 1: MultiNEC <tab> frequenza MHz <tab> unita' delle coordinate <tab> unita' del diametro ...
        // righe dei fili: unita' (m, mm, ft, in, wl) <tab> X1 Y1 Z1 X2 Y2 Z2 diametro segmenti (numeri o formule Excel).
        // Una riga senza unita' nella prima colonna usa l'unita' della prima riga del file (confermato da IC8TEM:
        // nel file di prova i due blocchi sono due antenne una sopra l'altra).
        // Sorgenti: filo <tab> % da E1 <tab> ampiezza <tab> fase <tab> V/I.
        // Nel foglio la riga N del file corrisponde alla riga N-14 di Excel (verificato sui riferimenti $B$11...).
        // =============================================================================================
        const int WeqRowOffset = 14;

        public static NecModel LoadWeq(string path)
        {
            string y = YanecFile.Sostituto(path);           // 0.4.30: .yanec
            if (y != null) return YanecFile.Carica(y);
            NecModel m = ParseWeq(File.ReadAllLines(path, Encoding.GetEncoding("ISO-8859-1")));
            m.FileName = path;
            return m;
        }

        // 0.4.7: le funzioni trigonometriche di Excel lavorano in radianti, quelle di YA-NEC (come 4nec2) in gradi
        static string ExcelTrig(string e)
        {
            e = Regex.Replace(e, @"(?<![A-Za-z_])LOG\s*\(", "LOG10(", RegexOptions.IgnoreCase);   // in Excel LOG e' in base 10
            e = Regex.Replace(e, @"(?<![A-Za-z_])LN\s*\(", "LOG(", RegexOptions.IgnoreCase);      // e LN e' il logaritmo naturale (LOG di YA-NEC)
            return Regex.Replace(e, @"(?<![A-Za-z_])(SIN|COS|TAN|ATAN|ASIN|ACOS)\s*\(", mt => "X" + mt.Groups[1].Value.ToUpperInvariant() + "(", RegexOptions.IgnoreCase);
        }

        // 0.4.7: diametro AWG di EZNEC/AutoEZ ("#12"), in metri: d = 0,005 x 92^((36 - n)/39) pollici
        static double Awg(string s)
        {
            int n;
            if (!s.StartsWith("#") || !int.TryParse(s.Substring(1).Trim(), NumberStyles.Integer, CI, out n)) return double.NaN;
            return 0.005 * Math.Pow(92, (36 - n) / 39.0) * 0.0254;
        }

        // 0.4.7: condizione di un IF di Excel con le variabili di AutoEZ (>, <, >=, <=, =, <>): vera o falsa
        static bool CondizioneExcel(string cond, Dictionary<string, double> vars)
        {
            Match m = Regex.Match(cond, @"^(.+?)(>=|<=|<>|>|<|=)(.+)$");
            if (!m.Success) throw new NecException("condizione non capita: " + cond);
            Func<string, double> ev = t => Expr.Eval(Regex.Replace(ExcelTrig(t), @"\b([A-Z])\b(?!\s*\()", mt => { double vv; return vars.TryGetValue(mt.Groups[1].Value, out vv) ? "(" + vv.ToString("R", CI) + ")" : mt.Value; }), null);
            double a = ev(m.Groups[1].Value), b = ev(m.Groups[3].Value);
            switch (m.Groups[2].Value)
            {
                case ">": return a > b;
                case "<": return a < b;
                case ">=": return a >= b;
                case "<=": return a <= b;
                case "=": return Math.Abs(a - b) < 1e-12;
                default: return Math.Abs(a - b) >= 1e-12;
            }
        }

        /// <summary>1.5: condizione di Excel con AND(...), OR(...), NOT(...) e confronti; "Freq" = frequenza di calcolo (chiave FREQ).</summary>
        static bool CondizioneComposta(string cond, Dictionary<string, double> vars)
        {
            cond = cond.Trim();
            Match f = Regex.Match(cond, @"^(AND|OR|NOT)\s*\((.*)\)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (f.Success)
            {
                List<string> parti = new List<string>(); int liv = 0, ini = 0; string corpo = f.Groups[2].Value;
                for (int i = 0; i < corpo.Length; i++)
                {
                    if (corpo[i] == '(') liv++; else if (corpo[i] == ')') liv--;
                    else if (corpo[i] == ',' && liv == 0) { parti.Add(corpo.Substring(ini, i - ini)); ini = i + 1; }
                }
                parti.Add(corpo.Substring(ini));
                string op = f.Groups[1].Value.ToUpperInvariant();
                if (op == "NOT") return !CondizioneComposta(parti[0], vars);
                bool and = op == "AND";
                foreach (string p in parti) { bool v = CondizioneComposta(p, vars); if (and && !v) return false; if (!and && v) return true; }
                return and;
            }
            double fr;
            string c2 = vars.TryGetValue("FREQ", out fr) ? Regex.Replace(cond, @"\bFreq\b", "(" + fr.ToString("R", CI) + ")", RegexOptions.IgnoreCase) : cond;
            return CondizioneExcel(c2, vars);
        }

        /// <summary>1.5 (prova .weq di IC8TEM, 18/11/2026: OCF Dipole e Fan Dipole di AC6LA): funzioni di testo di Excel usate da AutoEZ
        /// per le variabili scritte come "45d" (angolo) invece che come numero. IF(ISNUMBER(X), a, b) sceglie il ramo guardando se la
        /// variabile X e' un numero; SUBSTITUTE(X, "d", "") e VALUE(...) ricavano il numero dal testo della variabile.</summary>
        static string ExcelTesto(string e, Dictionary<string, string> testo, Dictionary<string, double> vars)
        {
            e = e.Replace("\"\"", "\"");
            // TLParms(cella, Zo, VF, k1, k2, k3, ..., "Ro"|"VF"|"dB", ...) di AutoEZ (linee coassiali e bifilari con le perdite): YA-NEC
            // usa Zo e VF nominali (la scheda TL e' senza perdite); "dB" = 0
            e = Regex.Replace(e, @"TLParms\s*\(([^()]*)\)", mt =>
            {
                string[] a = mt.Groups[1].Value.Split(',');
                string q = "";
                foreach (string x in a) if (x.Trim().StartsWith("\"")) q = x.Trim().Trim('"');
                if (a.Length < 3) return mt.Value;
                if (q.Equals("Ro", StringComparison.OrdinalIgnoreCase)) return "(" + a[1].Trim() + ")";
                if (q.Equals("VF", StringComparison.OrdinalIgnoreCase)) return "(" + a[2].Trim() + ")";
                return "(0)";
            }, RegexOptions.IgnoreCase);
            for (int giro = 0; giro < 20; giro++)
            {
                Match mi = Regex.Match(e, @"IF\s*\(\s*ISNUMBER\s*\(\s*([A-Z]{1,2})\s*\)\s*,", RegexOptions.IgnoreCase);
                if (!mi.Success) break;
                List<string> parti = new List<string>(); int liv = 0, ini = mi.Index + mi.Length, fine = -1;
                for (int i = ini; i < e.Length; i++)
                {
                    char ch = e[i];
                    if (ch == '"') { int j = e.IndexOf('"', i + 1); if (j < 0) break; i = j; continue; }
                    if (ch == '(') liv++;
                    else if (ch == ')') { if (liv == 0) { parti.Add(e.Substring(ini, i - ini)); fine = i; break; } liv--; }
                    else if (ch == ',' && liv == 0) { parti.Add(e.Substring(ini, i - ini)); ini = i + 1; }
                }
                if (fine < 0 || parti.Count < 1) break;
                bool numero = vars.ContainsKey(mi.Groups[1].Value.ToUpperInvariant());
                string ramo = numero ? parti[0] : (parti.Count > 1 ? parti[1] : "0");
                e = e.Substring(0, mi.Index) + "(" + ramo.Trim() + ")" + e.Substring(fine + 1);
            }
            e = Regex.Replace(e, @"SUBSTITUTE\s*\(\s*([A-Z]{1,2})\s*,\s*""([^""]*)""\s*,\s*""([^""]*)""\s*\)", mt =>
            {
                string n = mt.Groups[1].Value.ToUpperInvariant(), t;
                double v;
                if (vars.TryGetValue(n, out v)) return "(" + v.ToString("R", CI) + ")";
                if (!testo.TryGetValue(n, out t)) throw new NecException("SUBSTITUTE su " + n + ": variabile vuota");
                t = mt.Groups[2].Value.Length > 0 ? t.Replace(mt.Groups[2].Value, mt.Groups[3].Value) : t;
                if (!double.TryParse(t.Trim(), NumberStyles.Float, CI, out v)) throw new NecException("SUBSTITUTE su " + n + ": \"" + t + "\" non e' un numero");
                return "(" + v.ToString("R", CI) + ")";
            }, RegexOptions.IgnoreCase);
            return Regex.Replace(e, @"(?<![A-Za-z_])VALUE\s*\(", "(", RegexOptions.IgnoreCase);
        }

        /// <summary>Nome della colonna di Excel (1 = A, 27 = AA, 59 = BG).</summary>
        static string Colonna(int n)
        {
            string r = "";
            while (n > 0) { int k = (n - 1) % 26; r = (char)('A' + k) + r; n = (n - 1) / 26; }
            return r;
        }

        /// <summary>1.5 (prova .weq di IC8TEM, 18/11/2026): colonna «mark out» di AutoEZ ("x" o =IF(...)) delle tabelle di sorgenti, carichi e
        /// linee. Nel .weq e' la 13a colonna (indice 12): la 0.4.7 leggeva la 14a, vuota, e i "x" di 2el-30-40 e le formule delle quad
        /// pentabanda di AC6LA non venivano mai letti. Si guarda la 13a e, se vuota, la 14a.</summary>
        static string Marca(string[] c)
        {
            string t = Cell(c, 12);
            if (t.Length == 0) t = Cell(c, 13);
            return t.Equals("x", StringComparison.OrdinalIgnoreCase) || t.StartsWith("=") ? t : "";
        }

        static string Cell(string[] c, int i)
        {
            if (i >= c.Length) return "";
            string s = c[i].Trim();
            if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2) s = s.Substring(1, s.Length - 2);
            if (s.StartsWith("_")) s = s.Substring(1);
            return s.Trim();
        }

        // Variabili di AutoEZ: nel .weq ci sono solo valore e commento (righe 2, 3, ... prima della tabella dei fili),
        // senza la lettera. Le lettere di AutoEZ sono A..Z tranne C e R (nomi riservati di Excel) e le variabili vuote
        // non vengono scritte. Regola (0.3.5): se le lettere usate nelle formule sono tante quante le righe delle
        // variabili, si assegnano in ordine alfabetico; altrimenti A, B, D, E, ... in ordine, con un avviso.
        // 0.4.8 (collaudo 0.4.7, 13/11/2026 21:25): le variabili di AutoEZ stanno nelle righe 2-23 del file e la lettera dipende dalla RIGA,
        // non dall'ordine: A, B, D, E, G, H, I, J, K, L, M, N, O, P, Q, S, T, U, V, W, X, Y (niente C, F, R, Z; dalla 1.5 niente C, F, R, W: vedi sotto). Ricavato dai file di AC6LA:
        // "LPDA by Tau+Sigma" usa E = tau (riga 5) e G = sigma (riga 6); "Vee With Formulas" usa A (riga 2), H (riga 7) e L (riga 11);
        // le righe vuote non spostano le lettere. Le variabili scritte come formula si risolvono in piu' passate (una puo' usare l'altra);
        // UnitsFact = metri per unita' delle coordinate.
        // 1.5 (prova .weq di IC8TEM, 18/11/2026): anche la W manca. OCF Dipole di AC6LA usa V per la riga 20 e X per la riga 21, Fan Dipole
        // Parallel Y per la riga 22 e Fan Dipole Splayed Z per la riga 23; il blocco seguente del foglio comincia da AA. Prima le righe
        // 21-23 diventavano W, X, Y (una lettera indietro).
        const string LettereWeq = "ABDEGHIJKLMNOPQSTUVXYZ";

        static Dictionary<string, double> WeqVariables(string[][] rows, NecModel m, double unitaM)
        {
            Dictionary<string, double> vars = new Dictionary<string, double>();
            List<int> attese = new List<int>();
            string[] nomi = new string[LettereWeq.Length], formule = new string[LettereWeq.Length];
            for (int i = 0; i < LettereWeq.Length && i + 1 < rows.Length; i++)
            {
                string[] c = rows[i + 1];
                string v = Cell(c, 0);
                nomi[i] = Cell(c, 1);
                if (v.Length == 0) continue;
                double d;
                string lt = LettereWeq[i].ToString();
                if (double.TryParse(v, NumberStyles.Float, CI, out d)) vars[lt] = d;
                else if (v.StartsWith("=")) { formule[i] = v.Substring(1); attese.Add(i); }
            }
            Func<string, string> sostituisci = e =>
            {
                e = ExcelTrig(e);
                e = Regex.Replace(e, @"\bUnitsFact\b", "(" + unitaM.ToString("R", CI) + ")");
                // 1.5 (z2, IC8TEM 18/11/2026 23:01): nei file di MultiNEC le variabili usano WL, la lunghezza d'onda nell'unita' delle coordinate
                // (es. RightAngleDelta: E = WL*A / (2 + 2*SQRT(2)), G = WL*B); prima la variabile restava non definita e il file era rifiutato
                if (m.FreqMHz > 0 && unitaM > 0) e = Regex.Replace(e, @"\bWL\b", "(" + (299.792458 / m.FreqMHz / unitaM).ToString("R", CI) + ")");
                return Regex.Replace(e, @"\b([A-Z])\b(?!\s*\()", mt => { double vv; return vars.TryGetValue(mt.Groups[1].Value, out vv) ? "(" + vv.ToString("R", CI) + ")" : mt.Value; });
            };
            for (int giro = 0; giro < LettereWeq.Length && attese.Count > 0; giro++)
            {
                bool fatto = false;
                foreach (int i in attese.ToArray())
                {
                    try { vars[LettereWeq[i].ToString()] = Expr.Eval(sostituisci(formule[i]), null); attese.Remove(i); fatto = true; }
                    catch (Exception) { }
                }
                if (!fatto) break;
            }
            foreach (int i in attese)
                m.Notes.Add("File .weq: la variabile " + LettereWeq[i] + (nomi[i].Length > 0 ? " (" + nomi[i] + ")" : "") + " e' la formula \"=" + formule[i] +
                            "\" che YA-NEC non sa calcolare (testo, celle di altri fogli): non definita.");
            if (vars.Count == 0) return vars;
            string list = "";
            for (int i = 0; i < LettereWeq.Length; i++)
            {
                double v;
                if (!vars.TryGetValue(LettereWeq[i].ToString(), out v)) continue;
                list += (list.Length > 0 ? ", " : "") + LettereWeq[i] + " = " + v.ToString("0.####", CI) + (nomi[i] != null && nomi[i].Length > 0 && !nomi[i].StartsWith("=") ? " (" + nomi[i] + ")" : "");
            }
            m.Notes.Add("Variabili di AutoEZ: " + list);
            return vars;
        }

        public static NecModel ParseWeq(string[] lines)
        {
            NecModel m = new NecModel { SourceFormat = "AutoEZ" };
            if (lines.Length == 0) throw new NecException("File .weq vuoto.");
            string[] h = lines[0].Split('\t');
            double f;
            if (!double.TryParse(Cell(h, 1), NumberStyles.Float, CI, out f) || f <= 0)
                throw new NecException("File .weq: frequenza non trovata nella prima riga.");
            m.FreqMHz = f;
            double cu = UnitToMeters(Cell(h, 2), f), du = UnitToMeters(Cell(h, 3), f);
            if (double.IsNaN(cu)) { cu = 1; m.Warnings.Add("Unita' delle coordinate \"" + Cell(h, 2) + "\" non riconosciuta: usati i metri."); }
            if (double.IsNaN(du)) { du = 0.001; m.Warnings.Add("Unita' del diametro \"" + Cell(h, 3) + "\" non riconosciuta: usati i millimetri."); }
            m.Comments.Add("Importato da AutoEZ/MultiNEC (" + Cell(h, 0) + "), " + f.ToString("0.###", CI) + " MHz, coordinate in " + Cell(h, 2) + ", diametri in " + Cell(h, 3));

            string[][] rows = new string[lines.Length][];
            for (int i = 0; i < lines.Length; i++) rows[i] = lines[i].Split('\t');
            Dictionary<string, double> cache = new Dictionary<string, double>();
            Dictionary<string, double> vars = WeqVariables(rows, m, cu);
            // testo delle variabili (per SUBSTITUTE: "45d" = angolo in gradi)
            Dictionary<string, string> testoVar = new Dictionary<string, string>();
            for (int i = 0; i < LettereWeq.Length && i + 1 < rows.Length; i++) { string t = Cell(rows[i + 1], 0); if (t.Length > 0 && !t.StartsWith("=")) testoVar[LettereWeq[i].ToString()] = t; }
            // 1.5: il foglio Variables di AutoEZ e' una griglia di blocchi di 22 righe (righe 2-23 del file): blocco k con il valore nella
            // colonna 10k e il commento nella 10k+1. Il blocco 0 sono le lettere A..Y (WeqVariables); dal blocco 1 i nomi seguono le colonne
            // di Excel: riga n del blocco k = colonna 3 + 22k + n (blocco 1 da AA, blocco 2 da AW, blocco 3 da BS). Dentro i blocchi stanno
            // anche le celle calcolate («Calculated values. Do not modify»). Ricavato da OCF Dipole e Fan Dipole di AC6LA (prova di IC8TEM,
            // 18/11/2026): AC = rotazione dell'elemento 3, AG = lunghezza dell'elemento 4, BG = altezza del centro (=J), BV = centro dell'elemento 4.
            {
                Dictionary<string, string> nascoste = new Dictionary<string, string>();
                for (int li = 1; li <= 22 && li < rows.Length; li++)
                    for (int k = 1; 10 * k < rows[li].Length; k++)
                    {
                        string t = Cell(rows[li], 10 * k);
                        if (t.Length == 0) continue;
                        string nome = Colonna(3 + 22 * k + li + 1);
                        double dd;
                        if (t.StartsWith("=") || double.TryParse(t, NumberStyles.Float, CI, out dd)) nascoste[nome] = t;
                        else testoVar[nome] = t;
                    }
                List<string> attese = new List<string>(nascoste.Keys);
                for (int giro = 0; giro < 40 && attese.Count > 0; giro++)
                {
                    bool fatto = false;
                    foreach (string n in attese.ToArray())
                    {
                        try
                        {
                            string t = nascoste[n];
                            double dd;
                            if (!double.TryParse(t, NumberStyles.Float, CI, out dd))
                            {
                                string e = ExcelTrig(ExcelTesto(t.Substring(1), testoVar, vars));
                                e = Regex.Replace(e, @"\bUnitsFact\b", "(" + cu.ToString("R", CI) + ")");
                                e = Regex.Replace(e, @"\b([A-Z]{1,2})\b(?!\s*\()", mt => { double vv; return vars.TryGetValue(mt.Groups[1].Value, out vv) ? "(" + vv.ToString("R", CI) + ")" : mt.Value; });
                                dd = Expr.Eval(e, null);
                            }
                            vars[n] = dd; attese.Remove(n); fatto = true;
                        }
                        catch (Exception) { }
                    }
                    if (!fatto) break;
                }
                // le formule che scrivono testo (titoli «Element 3 (NOT IN USE)») non sono numeri: non si segnalano
                attese.RemoveAll(n => nascoste[n].Contains("\"\"") && nascoste[n].IndexOf("SUBSTITUTE", StringComparison.OrdinalIgnoreCase) < 0);
                if (nascoste.Count - attese.Count > 0)
                    m.Notes.Add("File .weq: " + (nascoste.Count - attese.Count) + " variabili e celle calcolate di AutoEZ oltre la prima colonna del foglio Variables (AA, AB, ... BG, ...) ricostruite" +
                        (attese.Count > 0 ? "; non calcolate: " + string.Join(", ", attese) : "") + ".");
            }
            HashSet<string> busy = new HashSet<string>();
            double wl = 299.792458 / f;   // in metri

            // valore di una cella (riga e colonna di Excel), con le formule risolte
            Func<int, int, double> value = null;
            Func<string, int, double> evalText = null;
            value = (xlRow, col) =>
            {
                string key = xlRow + ":" + col;
                double v;
                if (cache.TryGetValue(key, out v)) return v;
                if (!busy.Add(key)) throw new NecException("riferimento circolare nella cella " + (char)('A' + col) + xlRow);
                int li = xlRow + WeqRowOffset - 1;
                if (li < 0 || li >= rows.Length) throw new NecException("la cella " + (char)('A' + col) + xlRow + " non e' nel file");
                v = evalText(Cell(rows[li], col), xlRow);
                busy.Remove(key);
                cache[key] = v;
                return v;
            };
            evalText = (s, xlRow) =>
            {
                if (s.Length == 0) throw new NecException("cella vuota");
                double d;
                if (double.TryParse(s, NumberStyles.Float, CI, out d)) return d;
                if (!s.StartsWith("=")) throw new NecException("valore non numerico \"" + s + "\"");
                string e = ExcelTrig(ExcelTesto(s.Substring(1), testoVar, vars));
                // 1.5 (z2): WL di MultiNEC/AutoEZ = lunghezza d'onda nell'unita' delle coordinate (Example3 di MultiNEC la usa nei fili)
                e = Regex.Replace(e, @"\bWL\b", "(" + (wl / cu).ToString("R", CI) + ")");
                // Excel: INT tronca verso il basso (in YA-NEC INT arrotonda), SQRT esiste gia'
                e = Regex.Replace(e, @"\bINT\s*\(", "FLOOR(", RegexOptions.IgnoreCase);
                e = Regex.Replace(e, @"\$?([A-Z])\$?(\d+)\b", mt =>
                {
                    int col = mt.Groups[1].Value[0] - 'A';
                    int r = int.Parse(mt.Groups[2].Value, CI);
                    return "(" + value(r, col).ToString("R", CI) + ")";
                });
                // variabili di AutoEZ (una lettera sola, senza numero di riga) e celle nascoste (due lettere, 1.5)
                e = Regex.Replace(e, @"\b([A-Z]{1,2})\b(?!\s*\()", mt =>
                {
                    double vv;
                    return vars.TryGetValue(mt.Groups[1].Value, out vv) ? "(" + vv.ToString("R", CI) + ")" : mt.Value;
                });
                return Expr.Eval(e, null);
            };

            List<int> wireSegs = new List<int>();
            int ignored = 0, esclusi = 0, tag = 0;
            for (int li = 1; li < rows.Length; li++)
            {
                string[] c = rows[li];
                if (c.Length < 9) continue;
                // riga di un filo: colonne B..H numeriche o formule
                bool wireRow = Cell(c, 1).Length > 0;
                for (int k = 1; k <= 8 && wireRow; k++)
                {
                    string s = Cell(c, k);
                    double d;
                    if (!(s.StartsWith("=") || double.TryParse(s, NumberStyles.Float, CI, out d) || (k == 7 && !double.IsNaN(Awg(s))))) wireRow = false;
                }
                if (!wireRow) continue;
                string unit = Cell(c, 0);
                // prima colonna: "x" = filo escluso in AutoEZ; =IF(condizione, "", "X") = filo escluso se la condizione e' falsa (LPDA di AC6LA)
                if (unit.StartsWith("="))
                {
                    Match mi = Regex.Match(unit, @"^=\s*IF\s*\((.+?),\s*""+\s*,\s*""+X""+\s*\)\s*$", RegexOptions.IgnoreCase);
                    if (mi.Success)
                    {
                        bool incluso;
                        try { incluso = CondizioneExcel(mi.Groups[1].Value, vars); }
                        catch (NecException) { incluso = true; }
                        if (!incluso) { esclusi++; continue; }
                        unit = "";
                    }
                }
                if (unit.Equals("x", StringComparison.OrdinalIgnoreCase)) { esclusi++; continue; }
                double um = unit.Length == 0 ? cu : UnitToMeters(unit, f);
                if (double.IsNaN(um)) { ignored++; continue; }
                int xlRow = li + 1 - WeqRowOffset;
                double[] p = new double[6];
                double dia;
                try
                {
                    for (int k = 0; k < 6; k++) p[k] = value(xlRow, k + 1) * um;
                    double awg = Awg(Cell(c, 7));
                    dia = double.IsNaN(awg) ? value(xlRow, 7) * du : awg;
                }
                catch (NecException ex)
                {
                    // celle nascoste del foglio Variables (BG, BH...) o nomi di AutoEZ (WireLen, VF...): non si possono ricostruire dal .weq
                    Match mv = Regex.Match(ex.Message, @"Variabile sconosciuta: ([A-Za-z]{2,}\w*)");
                    if (mv.Success) throw new NecException("File .weq, riga " + (li + 1) + ": la formula usa \"" + mv.Groups[1].Value + "\", una cella nascosta o un nome interno di AutoEZ: queste formule non sono ancora supportate in YA-NEC.");
                    throw new NecException("File .weq, riga " + (li + 1) + ": " + ex.Message);
                }
                double len = Math.Sqrt((p[3] - p[0]) * (p[3] - p[0]) + (p[4] - p[1]) * (p[4] - p[1]) + (p[5] - p[2]) * (p[5] - p[2]));
                string sg = Cell(c, 8);
                int segs;
                double sd;
                if (double.TryParse(sg, NumberStyles.Float, CI, out sd)) segs = (int)Math.Round(sd);
                else
                {
                    // formula dei segmenti di AutoEZ: WireLen e WL nella stessa unita' (il rapporto non dipende dall'unita')
                    string e = sg.TrimStart('=');
                    e = Regex.Replace(e, @"\bWireLen\b", "(" + len.ToString("R", CI) + ")", RegexOptions.IgnoreCase);
                    e = Regex.Replace(e, @"\bWL\b", "(" + wl.ToString("R", CI) + ")", RegexOptions.IgnoreCase);
                    e = Regex.Replace(e, @"\bINT\s*\(", "FLOOR(", RegexOptions.IgnoreCase);
                    e = ExcelTrig(e);
                    e = Regex.Replace(e, @"\b([A-Z])\b(?!\s*\()", mt => { double vv; return vars.TryGetValue(mt.Groups[1].Value, out vv) ? "(" + vv.ToString("R", CI) + ")" : mt.Value; });
                    try { segs = (int)Math.Round(Expr.Eval(e, null)); }
                    catch (NecException ex) { throw new NecException("File .weq, riga " + (li + 1) + ", segmenti \"" + sg + "\": " + ex.Message); }
                }
                if (segs < 1) segs = 1;
                if (dia <= 0) throw new NecException("File .weq, riga " + (li + 1) + ": diametro nullo.");
                tag++;
                AddWire(m, tag, segs, p, dia / 2);
                wireSegs.Add(segs);
            }
            if (tag == 0) throw new NecException("Nel file .weq non sono stati trovati fili.");
            int fisici = tag;
            // 1.5 (prova .weq di IC8TEM, 18/11/2026: 4Square TL, W5TX, TFD di AC6LA): fili virtuali di EZNEC (V1, V2...), punti di
            // collegamento senza struttura per sorgenti, linee e trasformatori. Ogni filo virtuale diventa un filo di 1 segmento (lungo
            // 0,02 lambda, sopra l'antenna e fuori dal suo ingombro) con 1 Mohm di reattanza in serie: la porta e' praticamente aperta
            // (errore sotto 1/10000 sulle ammettenze delle linee) e il filo non porta corrente, quindi non irradia e non accoppia.
            Dictionary<string, int> virtuali = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            {
                SortedSet<int> nv = new SortedSet<int>();
                foreach (string[] c in rows)
                    foreach (int col in new[] { 0, 2, 26, 28 })
                    {
                        Match mv = Regex.Match(Cell(c, col), @"^V(\d+)$", RegexOptions.IgnoreCase);
                        if (mv.Success) nv.Add(int.Parse(mv.Groups[1].Value, CI));
                    }
                if (nv.Count > 0)
                {
                    double xmax = double.MinValue, zmax = double.MinValue, ymin = double.MaxValue, ymax = double.MinValue;
                    foreach (GeomCard g in m.Geometry)
                    {
                        xmax = Math.Max(xmax, Math.Max(g.F1, g.F4)); zmax = Math.Max(zmax, Math.Max(g.F3, g.F6));
                        ymin = Math.Min(ymin, Math.Min(g.F2, g.F5)); ymax = Math.Max(ymax, Math.Max(g.F2, g.F5));
                    }
                    double lv = 0.02 * wl, yc = (ymin + ymax) / 2, zc = Math.Max(zmax + 0.1 * wl, 0.1 * wl);
                    int k = 0;
                    foreach (int n in nv)
                    {
                        double x = xmax + 0.1 * wl + 0.05 * wl * k++;
                        tag++;
                        AddWire(m, tag, 1, new[] { x, yc - lv / 2, zc, x, yc + lv / 2, zc }, lv / 200);
                        wireSegs.Add(1);
                        virtuali["V" + n] = tag;
                        m.Loads.Add(new LoadSpec { Type = 4, Tag = tag, From = 1, To = 1, F1 = 0, F2 = 1e6 });
                    }
                    m.Notes.Add("File .weq: " + nv.Count + " fili virtuali di EZNEC (" + string.Join(", ", virtuali.Keys) + ") come fili " + (fisici + 1) + "-" + tag +
                        " di 1 segmento sopra l'antenna, con 1 Mohm di reattanza in serie: la porta resta aperta e il filo non irradia (non spegnere \"Usa i carichi\").");
                }
            }
            // filo (numero o filo virtuale V1, V2...) di una cella; 0 = nessuno
            Func<string, int> filo = t =>
            {
                int tv;
                if (virtuali.TryGetValue(t.Trim(), out tv)) return tv;
                double dd;
                if (double.TryParse(t, NumberStyles.Float, CI, out dd)) return (int)Math.Round(dd);
                return 0;
            };
            if (ignored > 0)
                m.Warnings.Add(ignored + " righe di fili con un'unita' sconosciuta nella prima colonna non sono state importate.");
            if (esclusi > 0)
                m.Notes.Add(esclusi + " fili segnati come esclusi in AutoEZ (\"x\" nella prima colonna) non sono stati importati, come fa AutoEZ.");

            int nCorr = 0, srcEsclusi = 0, nMark = 0;
            // sorgenti: filo, %, ampiezza, fase, tipo
            for (int li = 1; li < rows.Length; li++)
            {
                string[] c = rows[li];
                string typ = Cell(c, 4).ToUpperInvariant();
                if (typ != "V" && typ != "I" && typ != "SV" && typ != "SI") continue;
                int w;
                double pct, amp, ph;
                // 0.4.16 (collaudo 0.4.14: LPDA di AC6LA, sorgente "=S" sull'ultimo elemento): le celle della sorgente possono essere formule
                Func<int, double> cella = col =>
                {
                    string t = Cell(c, col);
                    double dd;
                    if (double.TryParse(t, NumberStyles.Float, CI, out dd)) return dd;
                    if (t.StartsWith("=")) { try { return evalText(t, li + 1 - WeqRowOffset); } catch (Exception) { } }
                    return double.NaN;
                };
                bool sv = virtuali.ContainsKey(Cell(c, 0));
                double wd = sv ? virtuali[Cell(c, 0)] : cella(0);
                pct = sv && Cell(c, 1).Length == 0 ? 50 : cella(1); amp = cella(2); ph = cella(3);
                if (double.IsNaN(wd) || double.IsNaN(pct) || double.IsNaN(amp) || double.IsNaN(ph)) continue;
                w = (int)Math.Round(wd);
                if (Marca(c).Equals("x", StringComparison.OrdinalIgnoreCase)) { srcEsclusi++; continue; }   // sorgente esclusa in AutoEZ
                // 1.5 (prova .weq di IC8TEM, 18/11/2026: quad pentabanda di AC6LA): =IF(condizione su Freq, "", "X") = sorgente accesa solo dove la
                // condizione e' vera; prima era ignorata e le cinque sorgenti restavano accese insieme
                Func<double, bool> attivaA = null; string attivaTesto = null;
                {
                    Match mo = Regex.Match(Marca(c), @"^=\s*IF\s*\((.+),\s*""+\s*,\s*""+X""+\s*\)\s*$", RegexOptions.IgnoreCase);
                    if (mo.Success)
                    {
                        string cond = mo.Groups[1].Value;
                        Dictionary<string, double> vv = new Dictionary<string, double>(vars);
                        attivaTesto = cond;
                        attivaA = fq => { vv["FREQ"] = fq; return CondizioneComposta(cond, vv); };
                        try { attivaA(m.FreqMHz); } catch (Exception) { attivaA = null; m.Warnings.Add("Condizione della sorgente non capita (\"" + cond + "\"): sorgente sempre accesa."); }
                    }
                }
                if (w < 1 || w > wireSegs.Count) { m.Warnings.Add("Sorgente sul filo " + w + " inesistente: ignorata."); continue; }
                double a = ph * Math.PI / 180;
                m.Sources.Add(new SourceSpec { Tag = w, Seg = SegOf(wireSegs[w - 1], pct), V = new Complex(amp * Math.Cos(a), amp * Math.Sin(a)), AttivaA = attivaA, AttivaTesto = attivaTesto });
                if (attivaA != null) nMark++;
                if (typ == "I" || typ == "SI") nCorr++;
            }
            // frequenze di prova di AutoEZ ("_Freq (MHz)" in fondo): se sono diverse da quella del modello EZNEC si calcola alla prima
            // (file di SQ9OUB del 13/11/2026: modello EZNEC a 432,5 MHz, sorgente sul radiatore dei 2 m, prove a 143,9 e 144 MHz)
            for (int li = 0; li + 1 < rows.Length; li++)
            {
                if (Cell(rows[li], 0) != "Freq (MHz)") continue;
                List<double> fp = new List<double>();
                for (int k = li + 1; k < rows.Length; k++)
                {
                    double ff;
                    string t = Cell(rows[k], 0);
                    if (rows[k].Length > 0 && rows[k][0].Trim().StartsWith("_") && double.TryParse(t, NumberStyles.Float, CI, out ff) && ff > 0) fp.Add(ff); else break;
                }
                if (fp.Count > 0 && Math.Abs(fp[0] - m.FreqMHz) > 1e-6 * m.FreqMHz)
                {
                    m.Warnings.Add("File .weq: il modello EZNEC e' a " + m.FreqMHz.ToString("0.###", CI) + " MHz ma le frequenze di prova di AutoEZ sono " +
                        string.Join(", ", fp.ConvertAll(q => q.ToString("0.###", CI))) + " MHz: calcolato a " + fp[0].ToString("0.###", CI) + " MHz (si cambia nella barra in alto).");
                    m.FreqMHz = fp[0];
                }
                break;
            }
            // 0.4.4: sorgenti di corrente di EZNEC (I) come "Sorgenti a corrente" di YA-NEC, come nella lettura dei .ez
            if (nCorr > 0 && nCorr == m.Sources.Count) m.CurrentSources = true;
            else if (nCorr > 0) m.Warnings.Add("File .weq: sorgenti di corrente e di tensione insieme: YA-NEC non le mescola, tutte calcolate come sorgenti di tensione.");
            if (srcEsclusi > 0) m.Notes.Add(srcEsclusi + " sorgenti segnate come escluse in AutoEZ (\"x\") non sono state importate.");
            if (nMark > 0) m.Notes.Add(nMark + " sorgenti accese solo in certe bande (formule «mark out» di AutoEZ): a ogni frequenza si calcolano solo quelle accese; le altre sono filo continuo, come in EZNEC.");
            if (m.Sources.Count == 0) m.Warnings.Add("Nel file .weq non e' stata trovata nessuna sorgente.");
            else if (m.Sources.Count == 1 && fisici > 1)
            {
                // antenne ripetute (stack) con una sola sorgente: le altre risultano passive
                List<double> zs = new List<double>();
                foreach (GeomCard g in m.Geometry) { if (g.I1 > fisici) continue; double z = Math.Round((g.F3 + g.F6) / 2, 3); if (!zs.Contains(z)) zs.Add(z); }
                if (zs.Count > 1)
                    m.Warnings.Add("Il modello ha fili a " + zs.Count + " quote diverse (antenne sovrapposte?) ma una sola sorgente: le antenne senza sorgente sono calcolate come passive.");
            }
            // 0.4.3: carichi e linee. Nei file di AutoEZ le sorgenti, i carichi e le linee sono tre blocchi di 30 righe che finiscono
            // alla riga "Controls" (16el di IC8TEM, esempi 4Square TL, Fan Dipole, Diamond Quad).
            int ci = -1;
            for (int li = 0; li < rows.Length; li++) if (Cell(rows[li], 0) == "Controls") { ci = li; break; }
            int nCar = 0, nLin = 0, carEsclusi = 0;
            if (ci >= 60)
            {
                Func<int, int, double> num = (li, col) =>
                {
                    string t = Cell(rows[li], col);
                    double dd;
                    if (double.TryParse(t, NumberStyles.Float, CI, out dd)) return dd;
                    return value(li + 1 - WeqRowOffset, col);
                };
                for (int li = ci - 60; li < ci - 30; li++)
                {
                    string[] c = rows[li];
                    int w;
                    if (!int.TryParse(Cell(c, 0), NumberStyles.Integer, CI, out w) || w < 1 || w > wireSegs.Count) continue;
                    try
                    {
                        // 0.4.7 (registro 13/11 17:54, 2el-30-40.weq): "x" nella colonna N = carico escluso in AutoEZ; celle vuote = 0.
                        // Colonna G compilata (Ser/Par) = carico RLC di EZNEC: R ohm, L uH, C pF (vuoto = assente); altrimenti R + jX con
                        // il collegamento della colonna H. Le bobine "indut" di 2el-30-40 (13,4 uH) confermano L in microhenry.
                        if (Marca(c).Equals("x", StringComparison.OrdinalIgnoreCase)) { carEsclusi++; continue; }
                        Func<int, double> nv = col => Cell(c, col).Length > 0 ? num(li, col) : 0;
                        double pct = num(li, 1), r = nv(2);
                        string rlc = Cell(c, 6), tipo = Cell(c, 7);
                        int sg = SegOf(wireSegs[w - 1], pct);
                        if (rlc.Length > 0 || Cell(c, 4).Length > 0)
                        {
                            bool par = rlc.StartsWith("Par", StringComparison.OrdinalIgnoreCase);
                            double L = nv(3) * 1e-6, C = nv(4) * 1e-12;
                            m.Loads.Add(new LoadSpec { Type = par ? 1 : 0, Tag = w, From = sg, To = sg, F1 = r, F2 = L, F3 = C });
                        }
                        else
                        {
                            double x = nv(3);
                            if (tipo.StartsWith("Par", StringComparison.OrdinalIgnoreCase))
                            {
                                Complex zp = r == 0 ? new Complex(0, x) : x == 0 ? new Complex(r, 0) : 1 / (1 / new Complex(r, 0) + 1 / new Complex(0, x));
                                m.Loads.Add(new LoadSpec { Type = 4, Tag = w, From = sg, To = sg, F1 = zp.Real, F2 = zp.Imaginary });
                            }
                            else m.Loads.Add(new LoadSpec { Type = 4, Tag = w, From = sg, To = sg, F1 = r, F2 = x });
                        }
                        nCar++;
                    }
                    catch (Exception ex) { m.Warnings.Add("Carico alla riga " + (li + 1) + " del .weq non letto: " + ex.Message); }
                }
                // 0.4.16 (LPDA di AC6LA): punto di un filo alla percentuale data, in metri (per la lunghezza "Actual dist" di AutoEZ)
                Func<int, double, double[]> punto = (tg, pc) =>
                {
                    GeomCard gw = m.Geometry[tg - 1];
                    double u = Math.Max(0, Math.Min(100, pc)) / 100;
                    return new[] { gw.F1 + (gw.F4 - gw.F1) * u, gw.F2 + (gw.F5 - gw.F2) * u, gw.F3 + (gw.F6 - gw.F3) * u };
                };
                Func<int, double, int, double, double> distanza = (ta, pa, tb, pb) =>
                {
                    double[] a = punto(ta, pa), b = punto(tb, pb);
                    return Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]) + (a[2] - b[2]) * (a[2] - b[2]));
                };
                Func<string, int, double> numOFormula = (t, li2) =>
                {
                    double dd;
                    if (double.TryParse(t, NumberStyles.Float, CI, out dd)) return dd;
                    if (t.StartsWith("=")) return evalText(t, li2 + 1 - WeqRowOffset);
                    throw new NecException("valore non numerico \"" + t + "\"");
                };
                NetworkSpec ultimaTl = null;
                double ultimaP1 = 50, ultimaP2 = 50, ultimaVf = 1;
                for (int li = ci - 30; li < ci; li++)
                {
                    string[] c = rows[li];
                    // "LPDA repeat to <S>" di AutoEZ: la linea precedente (filo n -> n+1) si ripete fino al filo S, sempre tra fili vicini
                    if (Cell(c, 0).IndexOf("LPDA repeat to", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Match mr = Regex.Match(Cell(c, 0), @"&\s*([A-Z])\b");
                        double sv;
                        if (ultimaTl != null && mr.Success && vars.TryGetValue(mr.Groups[1].Value, out sv) && ultimaTl.Tag2 == ultimaTl.Tag1 + 1)
                        {
                            int fino = Math.Min((int)Math.Round(sv), wireSegs.Count), nRip = 0;
                            for (int k = ultimaTl.Tag2; k < fino; k++)
                            {
                                double d = distanza(k, ultimaP1, k + 1, ultimaP2);
                                m.Networks.Add(new NetworkSpec { IsTl = true, Tag1 = k, Seg1 = SegOf(wireSegs[k - 1], ultimaP1), Tag2 = k + 1, Seg2 = SegOf(wireSegs[k], ultimaP2),
                                    F1 = ultimaTl.F1, F2 = ultimaVf > 0 ? d / ultimaVf : d });
                                nLin++; nRip++;
                            }
                            m.Notes.Add("File .weq: \"LPDA repeat to " + fino + "\" di AutoEZ: la linea tra i fili " + ultimaTl.Tag1 + " e " + ultimaTl.Tag2 + " ripetuta " + nRip + " volte fino al filo " + fino + ".");
                        }
                        else m.Warnings.Add("Riga " + (li + 1) + " del .weq: \"LPDA repeat\" di AutoEZ non applicato (manca la linea tra due fili vicini o il numero di fili).");
                        continue;
                    }
                    int w1, w2;
                    double w1d;
                    // 1.5: stub con "Open"/"Short" nella prima colonna e il filo nella terza (quad pentabanda di AC6LA): lo stub di YA-NEC si
                    // appoggia a una linea TL sullo stesso segmento; qui non ce n'e', quindi resta fuori con un avviso
                    if (Cell(c, 0).Equals("Open", StringComparison.OrdinalIgnoreCase) || Cell(c, 0).Equals("Short", StringComparison.OrdinalIgnoreCase))
                    {
                        m.Warnings.Add("Riga " + (li + 1) + " del .weq: stub " + (Cell(c, 0).StartsWith("O", StringComparison.OrdinalIgnoreCase) ? "aperto" : "in corto") + " sul filo " + Cell(c, 2) +
                            (Marca(c).Length > 0 ? " (acceso solo in certe bande)" : "") + " senza una linea sullo stesso segmento: non importato (YA-NEC non ha ancora gli stub a una porta).");
                        continue;
                    }
                    if (virtuali.ContainsKey(Cell(c, 0))) w1d = virtuali[Cell(c, 0)];
                    else try { w1d = numOFormula(Cell(c, 0), li); } catch (Exception) { continue; }
                    w1 = (int)Math.Round(w1d);
                    if (w1 < 1 || w1 > wireSegs.Count) continue;
                    // colonna N: "x" o =IF(condizione, "", "X") = linea esclusa in AutoEZ
                    string esc = Marca(c);
                    if (esc.Equals("x", StringComparison.OrdinalIgnoreCase)) continue;
                    Match mi = Regex.Match(esc, @"^=\s*IF\s*\((.+?),\s*""+\s*,\s*""+X""+\s*\)\s*$", RegexOptions.IgnoreCase);
                    if (mi.Success) { bool inc; try { inc = CondizioneExcel(mi.Groups[1].Value, vars); } catch (NecException) { inc = true; } if (!inc) continue; }
                    // stub in corto o aperto ("Short" / "Open" nella colonna del secondo filo): ammettenza in parallelo, come nella LPDA di YA-NEC
                    string fine = Cell(c, 2);
                    if (fine.Equals("Short", StringComparison.OrdinalIgnoreCase) || fine.Equals("Open", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            double p1s = numOFormula(Cell(c, 1), li), lens = numOFormula(Cell(c, 4), li), z0s = numOFormula(Cell(c, 5), li);
                            double vfs = Cell(c, 6).Length > 0 ? numOFormula(Cell(c, 6), li) : 1;
                            m.Stubs.Add(new Stub { Tag = w1, Seg = SegOf(wireSegs[w1 - 1], p1s), Z0 = Math.Abs(z0s), LenM = lens * cu, Vf = vfs > 0 ? vfs : 1, Corto = fine.StartsWith("S", StringComparison.OrdinalIgnoreCase) });
                            nLin++;
                        }
                        catch (Exception ex) { m.Warnings.Add("Stub alla riga " + (li + 1) + " del .weq non letto: " + ex.Message); }
                        continue;
                    }
                    w2 = filo(fine);
                    if (w2 < 1 || w2 > wireSegs.Count)
                    {
                        m.Warnings.Add("Linea alla riga " + (li + 1) + " del .weq verso \"" + fine + "\" (filo virtuale): non importata.");
                        continue;
                    }
                    try
                    {
                        double p1 = Cell(c, 1).Length == 0 && w1 > fisici ? 50 : num(li, 1), p2 = Cell(c, 3).Length == 0 && w2 > fisici ? 50 : num(li, 3);
                        double z0 = num(li, 5), vf = Cell(c, 6).Length > 0 ? num(li, 6) : 1;
                        // "Actual dist" di AutoEZ: lunghezza = distanza vera tra i due punti di collegamento
                        bool vera = Cell(c, 4).StartsWith("Actual", StringComparison.OrdinalIgnoreCase);
                        double lenM = vera ? distanza(w1, p1, w2, p2) : num(li, 4) * cu;
                        bool incr = Cell(c, 7).StartsWith("R", StringComparison.OrdinalIgnoreCase);
                        NetworkSpec ns = new NetworkSpec { IsTl = true, Tag1 = w1, Seg1 = SegOf(wireSegs[w1 - 1], p1), Tag2 = w2, Seg2 = SegOf(wireSegs[w2 - 1], p2),
                            F1 = incr ? -Math.Abs(z0) : Math.Abs(z0), F2 = lenM > 0 && vf > 0 ? lenM / vf : 0 };
                        m.Networks.Add(ns);
                        ultimaTl = ns; ultimaP1 = p1; ultimaP2 = p2; ultimaVf = vf;
                        if (Cell(c, 8).Length > 0 && Cell(c, 8).IndexOf("TLParms", StringComparison.OrdinalIgnoreCase) < 0 && num(li, 8) != 0) m.Warnings.Add("Linea alla riga " + (li + 1) + " del .weq: perdita non usata (la scheda TL e' senza perdite).");
                        nLin++;
                    }
                    catch (Exception ex) { m.Warnings.Add("Linea alla riga " + (li + 1) + " del .weq non letta: " + ex.Message); }
                }
            }
            int nTr = 0;
            if (ci >= 90)
            {
                // 1.5: tabella dei trasformatori di EZNEC (colonne AA..AG del blocco delle sorgenti): porta 1 filo e %, porta 2 filo e %,
                // Rel Z della porta 1 e della porta 2, N/R (TFD di AC6LA: V1 -> filo 1 al 50 %, 1 : S; Complete Feed System: V2 -> V3, 25 : 100)
                for (int li = ci - 90; li < ci - 60; li++)
                {
                    string[] c = rows[li];
                    int ta = filo(Cell(c, 26)), tb = filo(Cell(c, 28));
                    if (ta < 1 || tb < 1 || ta > wireSegs.Count || tb > wireSegs.Count) continue;
                    try
                    {
                        int riga = li;
                        Func<int, double, double> pc = (col, def) =>
                        {
                            string tx = Cell(c, col);
                            double dd;
                            if (tx.Length == 0) return def;
                            if (double.TryParse(tx, NumberStyles.Float, CI, out dd)) return dd;
                            return value(riga + 1 - WeqRowOffset, col);
                        };
                        double pa = pc(27, 50), pb = pc(29, 50), z1 = pc(30, 1), z2 = pc(31, 1);
                        if (z1 <= 0 || z2 <= 0) throw new NecException("Rel Z non positiva");
                        m.Trasformatori.Add(new Trasformatore { Tag1 = ta, Seg1 = SegOf(wireSegs[ta - 1], pa), Tag2 = tb, Seg2 = SegOf(wireSegs[tb - 1], pb),
                            Z1 = 50, Z2 = 50 * z2 / z1, Inv = Cell(c, 32).StartsWith("R", StringComparison.OrdinalIgnoreCase) });
                        nTr++;
                    }
                    catch (Exception ex) { m.Warnings.Add("Trasformatore alla riga " + (li + 1) + " del .weq non letto: " + ex.Message); }
                }
                // tabella delle reti L di EZNEC (stesse colonne nel blocco dei carichi): con AutoEZ le scrive Zmatch con formule del foglio
                // Calculate, che non e' nel .weq
                for (int li = ci - 60; li < ci - 30; li++)
                    if (filo(Cell(rows[li], 26)) > 0)
                        throw new NecException("File .weq: rete di adattamento (tabella «L Networks» di EZNEC, riga " + (li + 1) + ") scritta da AutoEZ con formule del foglio Calculate, che non e' nel .weq: non importabile.");
            }
            if (nTr > 0) m.Notes.Add("Dal .weq: " + nTr + " trasformatori ideali di EZNEC (rapporto delle impedenze dalla Rel Z delle due porte).");
            if (lines.Length > 0 && Array.Exists(lines, l => l.IndexOf("TLParms", StringComparison.OrdinalIgnoreCase) >= 0))
                m.Notes.Add("File .weq: le linee con TLParms di AutoEZ (cavi con perdite) usano Zo e VF nominali e nessuna perdita: la scheda TL di NEC e' senza perdite.");
            if (nCar + nLin > 0) m.Notes.Add("Dal .weq: " + nCar + " carichi e " + nLin + " linee.");
            if (carEsclusi > 0) m.Notes.Add(carEsclusi + " carichi segnati come esclusi in AutoEZ (\"x\") non sono stati importati.");
            m.Warnings.Add("Il terreno del file .weq non e' nel file (AutoEZ usa quello del modello EZNEC): controllare il menu Terreno.");
            m.Notes.Add("Modello importato da AutoEZ (.weq) con la segmentazione del file.");
            return m;
        }

        // =============================================================================================
        // EZNEC .ez (binario, little endian). Struttura ricavata dal file di prova:
        //   5: frequenza MHz (float)
        //   0: numero dei fili (int16)  48: di nuovo il numero dei fili, 50: numero delle sorgenti (verificato su 40 file, 0.3.5)
        //   18..47: titolo
        //   186: tabella a righe di 170 byte; riga k = filo k + sorgente k:
        //     +0  X1 Y1 Z1 X2 Y2 Z2 diametro (7 float, in metri anche se il modello e' in piedi)
        //     +28 FF FF oppure il calibro AWG   +30 segmenti (int16)
        //     +32 filo della sorgente (int16, 0 = nessuna)  +34 % da E1  +38 ampiezza  +42 fase (float)  +46 tipo ('V'/'I')
        // =============================================================================================
        const int EzTable = 186, EzRow = 170, EzGap = 324;

        /// <summary>1.6.0: blocchi in coda al .ez (Ez2Nec read.rs): dopo le righe e 324 byte, (tag int16, lunghezza int16 con i 4 byte
        /// della testa) fino alla fine. Restituisce per tag l'inizio e la fine dei dati.</summary>
        static Dictionary<int, int[]> EzBlocchi(byte[] d, int nRec)
        {
            Dictionary<int, int[]> r = new Dictionary<int, int[]>();
            int pos = EzTable + EzRow * nRec + EzGap;
            while (pos + 4 <= d.Length)
            {
                int tag = (ushort)BitConverter.ToInt16(d, pos), len = (ushort)BitConverter.ToInt16(d, pos + 2);
                if (len < 4 || pos + len > d.Length) break;
                if (!r.ContainsKey(tag)) r[tag] = new[] { pos + 4, pos + len };
                pos += len;
            }
            return r;
        }

        /// <summary>1.6.0: reti L di EZNEC dal blocco 0x11 (struttura di Ez2Nec read.rs, read_l_networks): porta 1 = ramo in serie tra le
        /// porte, porta 2 = ramo in parallelo sulla porta 2; R, L, C e frequenza della R per porta; S/P = elementi in serie o in parallelo.</summary>
        static int EzRetiL(byte[] d, Dictionary<int, int[]> blocchi, List<int> segs, NecModel m)
        {
            int[] b;
            if (!blocchi.TryGetValue(0x11, out b) || b[1] - b[0] < 11 || d[b[0]] != 0 || d[b[0] + 1] != 0 || d[b[0] + 2] != 2) return 0;
            int s = b[0], n = BitConverter.ToInt32(d, s + 3);
            if (n <= 0 || n != BitConverter.ToInt32(d, s + 7) || n > 500) return 0;
            int elems = 2 * n + 2, wiresAt = 11, pctsAt = wiresAt + 4 * elems, zAt = pctsAt + 4 * elems + 12, n3At = zAt + 16 * n, blocksAt = n3At + 36, connAt = blocksAt + 32 * n + 2;
            if (s + connAt + 2 * n > b[1]) return 0;
            int fatte = 0;
            for (int k = 0; k < n; k++)
            {
                int[] w = new int[2]; double[] pc = new double[2]; bool[] par = new bool[2];
                for (int p = 0; p < 2; p++)
                {
                    int e = 2 * k + p + 1;
                    w[p] = BitConverter.ToInt32(d, s + wiresAt + 4 * e);
                    pc[p] = BitConverter.ToSingle(d, s + pctsAt + 4 * e);
                    par[p] = d[s + connAt + 2 * k + p] == (byte)'P';
                }
                if (w[0] < 1 || w[0] > segs.Count || w[1] < 1 || w[1] > segs.Count) { m.Warnings.Add("Rete L " + (k + 1) + " di EZNEC su un filo inesistente: ignorata."); continue; }
                Func<int, double> blk = j => BitConverter.ToSingle(d, s + blocksAt + 32 * k + 4 * j);
                m.RetiL.Add(new ReteL
                {
                    Tag1 = w[0], Seg1 = SegOf(segs[w[0] - 1], pc[0]), Tag2 = w[1], Seg2 = SegOf(segs[w[1] - 1], pc[1]),
                    Rs = blk(0), Rp = blk(1), Ls = blk(2), Lp = blk(3), Cs = blk(4), Cp = blk(5), RfS = blk(6), RfP = blk(7),
                    ParS = par[0], ParP = par[1], LatoParallelo = 2, Eznec = true
                });
                fatte++;
            }
            return fatte;
        }

        public static NecModel LoadEz(string path)
        {
            string y = YanecFile.Sostituto(path);           // 0.4.30: .yanec
            if (y != null) return YanecFile.Carica(y);
            NecModel m = ParseEz(File.ReadAllBytes(path));
            m.FileName = path;
            return m;
        }

        public static NecModel ParseEz(byte[] d)
        {
            // 0.4.3: struttura di Ez2Nec (AG7T, src/ez/read.rs; approvato il 13/11/2026): conteggi in testata (righe a 0x00,
            // mezzi del terreno 0x02, fili 0x30, sorgenti 0x32, carichi 0x34, linee 0x78), byte di formato 5 a 0x40,
            // righe di 170 byte con filo +0x00, sorgente +0x20, carico +0x2F (RLC +0x85), linea +0x6D; terreno 0x36 (F/P/R),
            // modello 0x7B (M = MININEC, H = High Accuracy), mezzo 1 a 0xAA (sigma, er), resistivita' dei fili 0x62.
            if (d.Length < EzTable + EzRow || d[0x40] != 5 && BitConverter.ToInt16(d, 48) != BitConverter.ToInt16(d, 0))
                throw new NecException("Formato .ez non riconosciuto (versione di EZNEC diversa da quelle note). " +
                                       "Da EZNEC: File > Save As NEC (.nec), poi aprire il file .nec.");
            int nRec = BitConverter.ToInt16(d, 0);
            int nWires = BitConverter.ToInt16(d, 0x30);
            int nSrc = BitConverter.ToInt16(d, 0x32);
            int nLoad = d[0x40] == 5 ? BitConverter.ToInt16(d, 0x34) : 0;
            int nTl = d[0x40] == 5 ? BitConverter.ToInt16(d, 0x78) : 0;
            if (nRec < 1 || nWires < 1 || nWires > nRec || EzTable + EzRow * nRec > d.Length)
                throw new NecException("File .ez: numero di fili o di righe non valido (" + nWires + " fili, " + nRec + " righe).");
            NecModel m = new NecModel { SourceFormat = "EZNEC" };
            double f = BitConverter.ToSingle(d, 5);
            if (!(f > 0.001 && f < 1e6)) throw new NecException("File .ez: frequenza non valida (" + f.ToString(CI) + ").");
            m.FreqMHz = Math.Round(f, 4);
            string title = Encoding.GetEncoding("ISO-8859-1").GetString(d, 0x12, 30).Trim('\0', ' ');
            if (title.Length > 0) m.Comments.Add(title);
            m.Comments.Add("Importato da EZNEC (.ez)");
            List<int> segs = new List<int>();
            for (int k = 0; k < nWires; k++)
            {
                int st = EzTable + EzRow * k;
                int ns = BitConverter.ToInt16(d, st + 30);
                if (ns < 1) throw new NecException("File .ez, filo " + (k + 1) + ": " + ns + " segmenti.");
                double[] p = new double[6];
                for (int j = 0; j < 6; j++) p[j] = BitConverter.ToSingle(d, st + 4 * j);
                double dia = BitConverter.ToSingle(d, st + 24);
                if (!(dia > 0 && dia < 10)) throw new NecException("File .ez, filo " + (k + 1) + ": diametro non valido.");
                AddWire(m, k + 1, ns, p, dia / 2);
                segs.Add(ns);
            }
            Func<int, string, bool> filoOk = (w, cosa) =>
            {
                if (w >= 1 && w <= segs.Count) return true;
                m.Warnings.Add(cosa + " sul filo " + w + " inesistente: ignorato.");
                return false;
            };
            for (int k = 0; k < Math.Min(nSrc, nRec); k++)
            {
                int st = EzTable + EzRow * k;
                int w = BitConverter.ToInt16(d, st + 32);
                if (!filoOk(w, "Sorgente")) continue;
                double pct = BitConverter.ToSingle(d, st + 34), amp = BitConverter.ToSingle(d, st + 38), ph = BitConverter.ToSingle(d, st + 42);
                char typ = (char)d[st + 46];
                double a = ph * Math.PI / 180;
                Complex v = new Complex(amp * Math.Cos(a), amp * Math.Sin(a));
                int n = segs[w - 1];
                if (typ == 'W' || typ == 'J')
                {
                    // sorgente divisa (split) di EZNEC: alimenta i due segmenti ai lati della giunzione piu' vicina (Ez2Nec, rules.rs)
                    int jn = Math.Max(0, Math.Min(n, (int)Math.Floor(pct * n / 100.0 + 0.5)));
                    if (jn >= 1 && jn < n) { m.Sources.Add(new SourceSpec { Tag = w, Seg = jn, V = v / 2 }); m.Sources.Add(new SourceSpec { Tag = w, Seg = jn + 1, V = v / 2 }); }
                    else m.Sources.Add(new SourceSpec { Tag = w, Seg = SegOf(n, pct), V = v });
                }
                else m.Sources.Add(new SourceSpec { Tag = w, Seg = SegOf(n, pct), V = v });
                if (typ == 'I' || typ == 'J') m.CurrentSources = true;
            }
            // carichi: tutti dello stesso tipo (0x75: Z = R+jX, R = RLC, L = Laplace)
            char tipo = (char)d[0x75];
            int nLap = 0, nStub = 0, nPerd = 0, nRetiL = 0;
            // 1.6.0: blocchi in coda al file (Ez2Nec read.rs): perdite delle linee (0x0e) e reti L (0x11)
            Dictionary<int, int[]> blocchi = EzBlocchi(d, nRec);
            double[] perdite = new double[Math.Max(0, nTl)];
            int[] bp;
            if (blocchi.TryGetValue(0x0e, out bp) && bp[1] - bp[0] >= 7 && d[bp[0] + 2] == 1)
            {
                int nr = BitConverter.ToInt32(d, bp[0] + 3);
                for (int k = 0; k < Math.Min(nr, perdite.Length); k++)
                    if (bp[0] + 7 + 8 * k + 8 <= bp[1]) perdite[k] = BitConverter.ToSingle(d, bp[0] + 7 + 8 * k);
            }
            for (int k = 0; k < Math.Min(nLoad, nRec); k++)
            {
                int r = EzTable + EzRow * k;
                int w = BitConverter.ToInt16(d, r + 0x2F);
                if (!filoOk(w, "Carico")) continue;
                int seg = SegOf(segs[w - 1], BitConverter.ToSingle(d, r + 0x31));
                if (tipo == 'R')
                {
                    char forma = (char)d[r + 0x86];
                    double rr = BitConverter.ToSingle(d, r + 0x87), l = BitConverter.ToSingle(d, r + 0x8B), c = BitConverter.ToSingle(d, r + 0x8F);
                    if (forma == 'T') m.Warnings.Add("Carico trappola (T) di EZNEC sul filo " + w + ": calcolato come RLC parallelo.");
                    m.Loads.Add(new LoadSpec { Type = forma == 'S' ? 0 : 1, Tag = w, From = seg, To = seg, F1 = rr, F2 = l, F3 = c });
                }
                else if (tipo == 'L')
                {
                    // 1.6.0 (proposta 6, Ez2Nec read.rs: coefficienti a +0x3D, sei del numeratore e sei del denominatore, s^0 per primo)
                    CaricoLaplace cl = new CaricoLaplace { Tag = w, Seg = seg };
                    for (int j = 0; j < 6; j++) { cl.Num[j] = BitConverter.ToSingle(d, r + 0x3D + 4 * j); cl.Den[j] = BitConverter.ToSingle(d, r + 0x3D + 24 + 4 * j); }
                    m.Laplace.Add(cl);
                    nLap++;
                }
                else m.Loads.Add(new LoadSpec { Type = 4, Tag = w, From = seg, To = seg, F1 = BitConverter.ToSingle(d, r + 0x35), F2 = BitConverter.ToSingle(d, r + 0x39) });
            }
            for (int k = 0; k < Math.Min(nTl, nRec); k++)
            {
                int r = EzTable + EzRow * k + 0x6D;
                int wa = BitConverter.ToInt16(d, r), wb = BitConverter.ToInt16(d, r + 6);
                double pa = BitConverter.ToSingle(d, r + 2), pb = BitConverter.ToSingle(d, r + 8), z0 = BitConverter.ToSingle(d, r + 12), len = BitConverter.ToSingle(d, r + 16), vf = BitConverter.ToSingle(d, r + 20);
                if (!filoOk(wa, "Linea")) continue;
                double perd = k < perdite.Length && perdite[k] > 0 && perdite[k] < 10 ? perdite[k] : 0;
                if (wb < 0)
                {
                    // 1.6.0 (proposta 6): stub in corto (-1) o aperto (-2) di EZNEC. Ez2Nec lo scrive come rete con un filo fittizio per
                    // l'altro capo; in YA-NEC e' uno stub sul segmento: in parallelo a una linea che parte da li', altrimenti a una porta
                    // (la sua impedenza entra in serie nel filo). Lunghezza negativa = gradi elettrici.
                    if (len == 0 || !(vf > 0)) { m.Warnings.Add("Stub sul filo " + wa + " senza lunghezza o fattore di velocita': non importato."); continue; }
                    double lfis = len > 0 ? len : -len / 360.0 * 299.792458 / m.FreqMHz * vf;
                    m.Stubs.Add(new Stub { Tag = wa, Seg = SegOf(segs[wa - 1], pa), Z0 = Math.Abs(z0), LenM = lfis, Vf = vf, Corto = wb == -1, DbPerM = perd });
                    nStub++;
                    continue;
                }
                if (!filoOk(wb, "Linea")) continue;
                if (perd > 0)
                {
                    // 1.6.0: linea con perdite: rete NT ricalcolata a ogni frequenza (come Ez2Nec); senza lunghezza = distanza tra i punti
                    double lfis = len > 0 ? len : len < 0 ? -len / 360.0 * 299.792458 / m.FreqMHz * vf : 0;
                    if (lfis == 0)
                    {
                        GeomCard ga = m.Geometry[wa - 1], gb = m.Geometry[wb - 1];
                        Func<GeomCard, double, double[]> pt = (gw, pc) => { double u = Math.Max(0, Math.Min(100, pc)) / 100; return new[] { gw.F1 + (gw.F4 - gw.F1) * u, gw.F2 + (gw.F5 - gw.F2) * u, gw.F3 + (gw.F6 - gw.F3) * u }; };
                        double[] qa = pt(ga, pa), qb = pt(gb, pb);
                        lfis = Math.Sqrt((qa[0] - qb[0]) * (qa[0] - qb[0]) + (qa[1] - qb[1]) * (qa[1] - qb[1]) + (qa[2] - qb[2]) * (qa[2] - qb[2]));
                    }
                    m.LineePerdite.Add(new LineaPerdite { Tag1 = wa, Seg1 = SegOf(segs[wa - 1], pa), Tag2 = wb, Seg2 = SegOf(segs[wb - 1], pb), Z0 = z0, LenM = lfis, Vf = vf > 0 ? vf : 1, DbPerM = perd });
                    nPerd++;
                    continue;
                }
                double lnec = len > 0 && vf > 0 ? len / vf : 0;    // nel NEC la lunghezza della TL e' elettrica
                if (len < 0)
                {
                    // lunghezza negativa: gradi elettrici (negli esempi ARRL di EZNEC, cardioide: -80,56 e -154,53)
                    lnec = -len / 360.0 * 299.792458 / m.FreqMHz;
                    m.Warnings.Add("Linea dal filo " + wa + ": lunghezza " + (-len).ToString("0.##", CI) + " gradi elettrici a " + m.FreqMHz.ToString("0.###", CI) + " MHz (" + lnec.ToString("0.###", CI) + " m nel NEC).");
                }
                else if (len == 0 && Math.Abs(vf - 1) > 1e-6) m.Warnings.Add("Linea dal filo " + wa + " senza lunghezza e con VF " + vf.ToString("0.##", CI) + ": usata la distanza tra i segmenti con VF 1.");
                m.Networks.Add(new NetworkSpec { IsTl = true, Tag1 = wa, Seg1 = SegOf(segs[wa - 1], pa), Tag2 = wb, Seg2 = SegOf(segs[wb - 1], pb), F1 = z0, F2 = lnec });
            }
            if (d[0x40] == 5 && BitConverter.ToInt32(d, 0x9C) > 0)
            {
                nRetiL = EzRetiL(d, blocchi, segs, m);
                if (nRetiL < BitConverter.ToInt32(d, 0x9C)) m.Warnings.Add("Reti L di EZNEC: importate " + nRetiL + " su " + BitConverter.ToInt32(d, 0x9C) + " (blocco 0x11 non letto per intero).");
            }
            if (nLap + nStub + nPerd + nRetiL > 0)
                m.Notes.Add("Dal .ez (lettura di Ez2Nec, AG7T): " + nLap + " carichi di Laplace, " + nStub + " stub, " + nPerd + " linee con perdite, " + nRetiL +
                    " reti L; YA-NEC li ricalcola a ogni frequenza.");
            // terreno
            char gnd = (char)d[0x36];
            if (gnd == 'P') { m.Ground.Kind = GroundKind.Perfect; m.GroundFlag = 1; }
            else if (gnd == 'R')
            {
                m.Ground.Kind = GroundKind.Real; m.GroundFlag = 1;
                double sg = BitConverter.ToSingle(d, 0xAA), er = BitConverter.ToSingle(d, 0xAE);
                if (sg > 0 && er > 0) { m.Ground.Sigma = sg; m.Ground.Epsr = er; }
                m.Ground.Sommerfeld = d[0x7B] == 'H';
                // 0.4.20 (prova 6 con EZNEC, 14/11/2026): il terreno reale "MININEC" di EZNEC calcola correnti e impedenza come su terreno
                // perfetto e usa er e sigma solo per il diagramma (32 modelli su 32 tornano entro lo 0,5 % cosi'). In YA-NEC e' il GN 3
                // ("MiniNec ground" di 4nec2), gia' presente: prima della 0.4.20 l'import lo trattava come GN 0 (coefficiente di riflessione)
                // e l'impedenza differiva da EZNEC fino al 20 % (dipolo a 10 m: 74,2 +j10,7 contro 75,9 +j2,7 ohm).
                m.Ground.MiniNec = !m.Ground.Sommerfeld;
                if (m.Ground.MiniNec) m.Warnings.Add("Terreno reale \"MININEC\" di EZNEC: impedenza calcolata come su terreno perfetto, diagramma con er e sigma (GN 3, come EZNEC). " +
                    "Per l'impedenza su terreno reale scegliere Sommerfeld (GN 2, in EZNEC \"High Accuracy\").");
                if (BitConverter.ToInt16(d, 2) > 1) m.Warnings.Add("Terreno con piu' mezzi in EZNEC: importato solo il primo.");
            }
            double rho = BitConverter.ToSingle(d, 0x62);
            if (rho > 0 && rho < 1) m.Loads.Add(new LoadSpec { Type = 5, Tag = 0, F1 = 1 / rho });
            if (m.Sources.Count == 0) m.Warnings.Add("Nel file .ez non e' stata trovata nessuna sorgente.");
            m.Warnings.Add("File .ez: lettura con la struttura di Ez2Nec (formato non documentato); unita' in metri. Controllare terreno e carichi.");
            m.Notes.Add("Modello importato da EZNEC (.ez) con la segmentazione del file.");
            // 0.4.22 (collaudo del 14/11/2026 20:39, fase 2J): sui 202 modelli d'archivio con diametri a gradini (Yagi a tubi telescopici
            // dell'ARRL, Hy-Gain/K6STI...) YA-NEC che leggeva il .ez senza correzione differiva da EZNEC del 42 % (mediana), con la
            // correzione di Leeson del 3 %: EZNEC li calcola con la sua Stepped Diameter Correction. Il .ez si legge quindi con Leeson acceso
            // quando il modello ha catene di tubi a gradini.
            try
            {
                if (Leeson.Trova(m).Count > 0)
                {
                    m.Leeson = true;
                    m.Warnings.Add("Diametri a gradini: EZNEC li calcola con la sua Stepped Diameter Correction; YA-NEC applica la correzione di Leeson " +
                        "(Impostazioni > Tubi telescopici). I due metodi danno risultati vicini ma non identici (nel collaudo circa il 3 %).");
                }
            }
            catch { }
            return m;
        }
    }
}
