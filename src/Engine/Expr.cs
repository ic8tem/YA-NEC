// YA-NEC - espressioni e variabili (schede SY), compatibili con 4nec2 e xnec2c:
// operatori + - * / ^ (potenza, associativa a destra), meno unario, parentesi;
// funzioni SIN COS TAN (gradi), ATN (gradi), SQR, EXP, LOG, LOG10, ABS, SGN, INT (arrotonda), FIX (tronca), MOD, MAX, MIN;
// estensione di YA-NEC: ODD(x) = intero dispari piu' vicino non inferiore (utile per i segmenti);
// costanti PI, MM, CM, IN, FT, PF, NF, UF, NH, UH, AWG_0 ... AWG_20 (diametri in metri come in xnec2c).
// Maiuscole e minuscole sono equivalenti. Un'unita' attaccata al numero vale come prodotto: 6MM = 6*MM.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public class SyVar
    {
        public string Name = "";
        public string Expr = "";
        public double Value;
        public string Error;
        public string Descr = "";   // cosa rappresenta (scritta dall'utente; nel file: CM YA-NEC VAR nome: descrizione)
        public SyVar Clone() { return (SyVar)MemberwiseClone(); }
    }

    public static class Expr
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        static readonly Dictionary<string, double> Constants = BuildConstants();

        static Dictionary<string, double> BuildConstants()
        {
            Dictionary<string, double> c = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "PI", Math.PI }, { "MM", 1.0e-3 }, { "CM", 1.0e-2 }, { "IN", 2.54e-2 }, { "FT", 0.3048 },
                { "PF", 1.0e-12 }, { "NF", 1.0e-9 }, { "UF", 1.0e-6 }, { "NH", 1.0e-9 }, { "UH", 1.0e-6 },
            };
            double[] awg = { 0.00412623, 0.00367411, 0.00327152, 0.00291338, 0.00259461, 0.00231013, 0.00205740, 0.00183261,
                             0.00163195, 0.00145288, 0.00129413, 0.00115189, 0.00102616, 0.00091440, 0.00081407, 0.00072517,
                             0.00064516, 0.00057531, 0.00051308, 0.00045720, 0.00040640 };
            for (int i = 0; i < awg.Length; i++) c["AWG_" + i] = awg[i];
            return c;
        }

        static readonly HashSet<string> Functions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SIN", "COS", "TAN", "ATN", "SQR", "EXP", "LOG", "LOG10", "ABS", "SGN", "INT", "FIX", "MOD", "MAX", "MIN", "ODD",
            // funzioni dei fogli di AutoEZ / MultiNEC (Excel), usate dagli importatori
            "SQRT", "CEILING", "FLOOR", "ROUND", "EVEN",
            // 0.4.7: funzioni di Excel in radianti per i .weq (gli importatori rinominano SIN -> XSIN ecc.), RADIANS, DEGREES, LN, COSH
            "XSIN", "XCOS", "XTAN", "XATAN", "XASIN", "XACOS", "RADIANS", "DEGREES", "COSH", "SINH",
            // 0.4.11 (collaudo 0.4.9: "LPDA by Tau+Sigma.weq" e "LPDA by Elems+Boom.weq" di AC6LA, "Funzione sconosciuta: IF"): IF, AND, OR, NOT
            // di Excel; le condizioni usano i confronti > < >= <= = <> (vero = 1, falso = 0; un numero diverso da 0 e' vero, come in Excel)
            "IF", "AND", "OR", "NOT"   // 0.4.8: niente "LN" (e' un nome di variabile nei modelli di 4nec2, 1elQuad.nec): i .weq lo traducono in LOG
        };

        /// <summary>Raggio in metri del filo AWG n (diametro 0,127 mm x 92^((36 - n)/39)).</summary>
        public static double RaggioAwg(double n) { return 0.127e-3 * Math.Pow(92, (36 - n) / 39.0) / 2; }

        public static bool IsReserved(string name) { return Constants.ContainsKey(name) || Functions.Contains(name); }

        public static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!(char.IsLetter(name[0]) || name[0] == '_')) return false;
            foreach (char ch in name) if (!(char.IsLetterOrDigit(ch) || ch == '_')) return false;
            // 0.4.8: un nome di funzione (MIN, MAX, LOG...) puo' essere anche una variabile, come in 4nec2 (TL-test.nec: "SY min = wl/999"):
            // seguito da "(" e' la funzione, altrimenti e' la variabile. Restano riservate solo le costanti (PI, MM, FT, AWG_n...).
            return !Constants.ContainsKey(name);
        }

        /// <summary>Vero se il testo e' un semplice numero (senza variabili ne' operatori).</summary>
        public static bool IsPlainNumber(string s)
        {
            double d;
            return double.TryParse((s ?? "").Trim(), NumberStyles.Float, CI, out d);
        }

        public static double Eval(string text, IDictionary<string, double> vars)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            double quick;
            if (double.TryParse(text.Trim(), NumberStyles.Float, CI, out quick)) return quick;
            Parser p = new Parser(text, vars);
            double v = p.ParseExpr();
            p.SkipWs();
            if (!p.End) throw new NecException("Espressione non valida vicino a '" + p.Rest + "' in '" + text + "'");
            if (double.IsNaN(v) || double.IsInfinity(v)) throw new NecException("L'espressione '" + text + "' non da' un numero valido.");
            return v;
        }

        class Parser
        {
            readonly string s;
            readonly IDictionary<string, double> vars;
            int i;

            public Parser(string text, IDictionary<string, double> v) { s = text; vars = v; }
            public bool End { get { return i >= s.Length; } }
            public string Rest { get { return i < s.Length ? s.Substring(i) : ""; } }
            public void SkipWs() { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

            bool Accept(char c)
            {
                SkipWs();
                if (i < s.Length && s[i] == c) { i++; return true; }
                return false;
            }

            // 0.4.10: confronti di Excel (livello piu' basso: A+1 > B*2 confronta le due somme)
            public double ParseExpr()
            {
                double v = ParseSum();
                SkipWs();
                if (i >= s.Length) return v;
                string op = null;
                if (s[i] == '<' && i + 1 < s.Length && (s[i + 1] == '=' || s[i + 1] == '>')) op = s.Substring(i, 2);
                else if (s[i] == '>' && i + 1 < s.Length && s[i + 1] == '=') op = ">=";
                else if (s[i] == '<' || s[i] == '>' || s[i] == '=') op = s[i].ToString();
                if (op == null) return v;
                i += op.Length;
                double w = ParseSum();
                bool r;
                switch (op)
                {
                    case "<": r = v < w; break;
                    case ">": r = v > w; break;
                    case "<=": r = v <= w; break;
                    case ">=": r = v >= w; break;
                    case "=": r = Math.Abs(v - w) <= 1e-12 * Math.Max(1, Math.Abs(v)); break;
                    default: r = Math.Abs(v - w) > 1e-12 * Math.Max(1, Math.Abs(v)); break;
                }
                return r ? 1 : 0;
            }

            double ParseSum()
            {
                double v = ParseTerm();
                while (true)
                {
                    if (Accept('+')) v += ParseTerm();
                    else if (Accept('-')) v -= ParseTerm();
                    else return v;
                }
            }

            double ParseTerm()
            {
                double v = ParseUnary();
                while (true)
                {
                    if (Accept('*')) v *= ParseUnary();
                    else if (Accept('/'))
                    {
                        double d = ParseUnary();
                        if (d == 0) throw new NecException("Divisione per zero in '" + s + "'");
                        v /= d;
                    }
                    else return v;
                }
            }

            // il meno unario ha precedenza minore della potenza: -2^2 = -4
            double ParseUnary()
            {
                if (Accept('-')) return -ParseUnary();
                if (Accept('+')) return ParseUnary();
                return ParsePower();
            }

            double ParsePower()
            {
                double b = ParsePrimary();
                if (Accept('^')) return Math.Pow(b, ParseUnary());
                return b;
            }

            double ParsePrimary()
            {
                SkipWs();
                if (i >= s.Length) throw new NecException("Espressione incompleta: '" + s + "'");
                char c = s[i];
                if (c == '(')
                {
                    i++;
                    double v = ParseExpr();
                    if (!Accept(')')) throw new NecException("Manca ')' in '" + s + "'");
                    return v;
                }
                if (c == '#' && i + 1 < s.Length && (char.IsDigit(s[i + 1]) || s[i + 1] == '.'))
                {
                    // calibro AWG come in 4nec2 e AO/NEC-Wires (K6STI): #12 = raggio del filo AWG 12 in metri.
                    // 0.4.8: anche calibri non interi (#.4, #2.5): la formula AWG e' continua (D2T.nec di 4nec2)
                    i++;
                    int st0 = i;
                    while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    double g;
                    if (!double.TryParse(s.Substring(st0, i - st0), NumberStyles.Float, CI, out g))
                        throw new NecException("Calibro AWG non valido in '" + s + "'");
                    return RaggioAwg(g);
                }
                if (char.IsDigit(c) || c == '.')
                {
                    int st = i;
                    while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
                    {
                        int save = i;
                        i++;
                        if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
                        if (i < s.Length && char.IsDigit(s[i])) { while (i < s.Length && char.IsDigit(s[i])) i++; }
                        else i = save;
                    }
                    double d;
                    string num = s.Substring(st, i - st);
                    if (!double.TryParse(num, NumberStyles.Float, CI, out d))
                        throw new NecException("Numero non valido in '" + s + "'");
                    // unita' attaccata al numero: 6MM = 6*MM, 2.5CM = 2.5*CM, 10PF = 10*PF
                    if (i < s.Length && (char.IsLetter(s[i]) || s[i] == '_'))
                    {
                        int st2 = i;
                        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                        string unit = s.Substring(st2, i - st2);
                        double u;
                        if (Constants.TryGetValue(unit, out u)) return d * u;
                        throw new NecException("Tra " + num + " e " + unit + " manca l'operatore: scrivere " + num + "*" + unit);
                    }
                    // unita' separata da uno spazio in fondo all'espressione, come nei file 4nec2: "SY L = 135 ft", "C = 57.1 pf"
                    {
                        int k = i;
                        while (k < s.Length && (s[k] == ' ' || s[k] == '\t')) k++;
                        if (k > i && k < s.Length && char.IsLetter(s[k]))
                        {
                            int k2 = k;
                            while (k2 < s.Length && (char.IsLetterOrDigit(s[k2]) || s[k2] == '_')) k2++;
                            double u2;
                            if (s.Substring(k2).Trim().Length == 0 && Constants.TryGetValue(s.Substring(k, k2 - k), out u2) && !s.Substring(k, k2 - k).Equals("PI", StringComparison.OrdinalIgnoreCase))
                            {
                                i = k2;
                                return d * u2;
                            }
                        }
                    }
                    return d;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int st = i;
                    while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    string name = s.Substring(st, i - st);
                    SkipWs();
                    if (i < s.Length && s[i] == '(')
                    {
                        i++;
                        List<double> args = new List<double>();
                        if (!Accept(')'))
                        {
                            args.Add(ParseExpr());
                            while (Accept(',')) args.Add(ParseExpr());
                            if (!Accept(')')) throw new NecException("Manca ')' dopo gli argomenti di " + name);
                        }
                        return Call(name, args);
                    }
                    double v;
                    if (vars != null && vars.TryGetValue(name, out v)) return v;
                    if (Constants.TryGetValue(name, out v)) return v;
                    throw new NecException("Variabile sconosciuta: " + name);
                }
                throw new NecException("Carattere inatteso '" + c + "' in '" + s + "'");
            }

            double Call(string name, List<double> a)
            {
                string n = name.ToUpperInvariant();
                if (n == "PI" && a.Count == 0) return Math.PI;   // PI() come in Excel
                if (!Functions.Contains(n)) throw new NecException("Funzione sconosciuta: " + name);
                if (n == "IF")
                {
                    if (a.Count < 2 || a.Count > 3) throw new NecException("La funzione IF vuole 2 o 3 argomenti.");
                    return a[0] != 0 ? a[1] : (a.Count > 2 ? a[2] : 0);
                }
                if (n == "AND" || n == "OR")
                {
                    if (a.Count < 1) throw new NecException("La funzione " + n + " vuole almeno un argomento.");
                    bool r = n == "AND";
                    foreach (double x in a) r = n == "AND" ? r && x != 0 : r || x != 0;
                    return r ? 1 : 0;
                }
                if (n == "NOT")
                {
                    if (a.Count != 1) throw new NecException("La funzione NOT vuole 1 argomento.");
                    return a[0] == 0 ? 1 : 0;
                }
                bool optional2 = n == "CEILING" || n == "FLOOR" || n == "ROUND";
                int need = (n == "MOD" || n == "MAX" || n == "MIN") ? 2 : 1;
                if (optional2 ? (a.Count < 1 || a.Count > 2) : a.Count != need)
                    throw new NecException("La funzione " + n + " vuole " + (optional2 ? "1 o 2" : need.ToString()) + " argomenti.");
                const double D2R = Math.PI / 180.0;
                switch (n)
                {
                    case "SIN": return Math.Sin(a[0] * D2R);
                    case "COS": return Math.Cos(a[0] * D2R);
                    case "TAN": return Math.Tan(a[0] * D2R);
                    case "ATN": return Math.Atan(a[0]) / D2R;
                    case "SQR":
                        if (a[0] < 0) throw new NecException("SQR di un numero negativo.");
                        return Math.Sqrt(a[0]);
                    case "EXP": return Math.Exp(a[0]);
                    case "LOG": return Math.Log(a[0]);
                    case "LOG10": return Math.Log10(a[0]);
                    case "ABS": return Math.Abs(a[0]);
                    case "SGN": return Math.Sign(a[0]);
                    case "INT": return Math.Round(a[0], MidpointRounding.AwayFromZero);
                    case "FIX": return Math.Truncate(a[0]);
                    case "MOD": return a[0] % a[1];
                    case "MAX": return Math.Max(a[0], a[1]);
                    case "MIN": return Math.Min(a[0], a[1]);
                    case "XSIN": return Math.Sin(a[0]);
                    case "XCOS": return Math.Cos(a[0]);
                    case "XTAN": return Math.Tan(a[0]);
                    case "XATAN": return Math.Atan(a[0]);
                    case "XASIN": return Math.Asin(a[0]);
                    case "XACOS": return Math.Acos(a[0]);
                    case "RADIANS": return a[0] * D2R;
                    case "DEGREES": return a[0] / D2R;
                    case "COSH": return Math.Cosh(a[0]);
                    case "SINH": return Math.Sinh(a[0]);
                    case "SQRT":
                        if (a[0] < 0) throw new NecException("SQRT di un numero negativo.");
                        return Math.Sqrt(a[0]);
                    case "CEILING":
                        {
                            double sg = a.Count > 1 ? a[1] : 1;
                            return sg == 0 ? 0 : Math.Ceiling(a[0] / sg - 1e-12) * sg;
                        }
                    case "FLOOR":
                        {
                            double sg = a.Count > 1 ? a[1] : 1;
                            return sg == 0 ? 0 : Math.Floor(a[0] / sg + 1e-12) * sg;
                        }
                    case "ROUND":
                        return Math.Round(a[0], a.Count > 1 ? (int)a[1] : 0, MidpointRounding.AwayFromZero);
                    case "EVEN":
                        {
                            double r = Math.Ceiling(Math.Abs(a[0]) - 1e-9);
                            if (Math.Abs(r % 2) > 0.5) r += 1;
                            return a[0] < 0 ? -r : r;
                        }
                    case "ODD":
                        {
                            double r = Math.Ceiling(a[0] - 1e-9);
                            if (Math.Abs(r % 2) < 0.5) r += 1;
                            return r;
                        }
                }
                throw new NecException("Funzione sconosciuta: " + name);
            }
        }

        /// <summary>
        /// Per le caselle della tabella fili e delle variabili: la virgola decimale (tastiera italiana) diventa punto.
        /// Le virgole dentro le parentesi restano, perche' separano gli argomenti di MAX(a,b), MIN e MOD.
        /// </summary>
        public static string NormalizeDecimal(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
            int depth = 0;
            foreach (char ch in s)
            {
                if (ch == '(') depth++;
                if (ch == ')') depth = Math.Max(0, depth - 1);
                sb.Append(ch == ',' && depth == 0 ? '.' : ch);
            }
            return sb.ToString();
        }

        /// <summary>Divide i campi di una scheda su spazi, tabulazioni e virgole, ma non dentro le parentesi.</summary>
        public static string[] SplitFields(string rest)
        {
            List<string> f = new List<string>();
            System.Text.StringBuilder cur = new System.Text.StringBuilder();
            int depth = 0;
            foreach (char ch in rest)
            {
                if (ch == '(') depth++;
                if (ch == ')') depth = Math.Max(0, depth - 1);
                bool sep = depth == 0 && (ch == ' ' || ch == '\t' || ch == ',');
                if (sep)
                {
                    if (cur.Length > 0) { f.Add(cur.ToString()); cur.Clear(); }
                    continue;
                }
                if (ch == ' ' || ch == '\t') continue;    // spazi dentro le parentesi ignorati
                cur.Append(ch);
            }
            if (cur.Length > 0) f.Add(cur.ToString());
            return f.ToArray();
        }

        /// <summary>
        /// 0.4.8: unita' scritta come campo a se', come nei file 4nec2 (2lsloper.nec: "GW 1 11 0 -68 ft X Y -68 ft Z #12"):
        /// il campo "ft" (o in, cm, mm, pf...) moltiplica il campo precedente invece di occupare un posto.
        /// </summary>
        public static string[] UnisciUnita(string[] f)
        {
            List<string> r = new List<string>();
            foreach (string x in f)
            {
                double u;
                if (r.Count > 0 && Constants.TryGetValue(x, out u) && !x.Equals("PI", StringComparison.OrdinalIgnoreCase)
                    && !x.StartsWith("AWG_", StringComparison.OrdinalIgnoreCase) && !r[r.Count - 1].EndsWith("%"))
                {
                    r[r.Count - 1] = "(" + r[r.Count - 1] + ")*" + x.ToUpperInvariant();
                    continue;
                }
                r.Add(x);
            }
            return r.ToArray();
        }

        /// <summary>Legge le definizioni di una scheda SY: "A=1, B=A*2" (virgole fuori dalle parentesi).</summary>
        public static List<KeyValuePair<string, string>> SplitDefinitions(string rest)
        {
            List<KeyValuePair<string, string>> r = new List<KeyValuePair<string, string>>();
            List<string> parts = new List<string>();
            System.Text.StringBuilder cur = new System.Text.StringBuilder();
            int depth = 0;
            foreach (char ch in rest)
            {
                if (ch == '(') depth++;
                if (ch == ')') depth = Math.Max(0, depth - 1);
                if (ch == ',' && depth == 0) { parts.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(ch);
            }
            parts.Add(cur.ToString());
            foreach (string p in parts)
            {
                string t = p.Trim();
                if (t.Length == 0) continue;
                int eq = t.IndexOf('=');
                if (eq <= 0) throw new NecException("Scheda SY: manca '=' in '" + t + "'");
                string name = t.Substring(0, eq).Trim();
                string ex = t.Substring(eq + 1).Trim();
                if (!IsValidName(name))
                {
                    // 0.4.8: 4nec2 (k4ero-4.nec, "SY R-2=18") accetta a sinistra un'espressione: la si passa con "=" davanti
                    // e chi legge la scheda controlla che valga gia' quanto scritto a destra.
                    if (name.Length > 1 && (char.IsLetter(name[0]) || name[0] == '_') && name.IndexOfAny(new[] { '-', '+', '*', '/', '^' }) > 0)
                    { r.Add(new KeyValuePair<string, string>("=" + name, ex)); continue; }
                    throw new NecException("Scheda SY: nome di variabile non valido o riservato: '" + name + "'");
                }
                r.Add(new KeyValuePair<string, string>(name, ex));
            }
            return r;
        }
    }
}
