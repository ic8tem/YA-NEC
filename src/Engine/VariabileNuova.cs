// YA-NEC 1.5.1 - variabile nuova dal modello (bozzetto «Ottimizzatore: creare una variabile quando il modello non ne ha», approvato da
// IC8TEM il 19/11/2026 08:10, versione 2 con l'immagine in ocra) e «dove agisce la variabile» (bozzetto della linguetta Variabili, approvato
// alle 08:12). Una misura di un elemento diritto (lunghezza, posizione sul boom, raggio) o la quota di tutta l'antenna diventa una scheda SY
// e i campi delle schede GW diventano formule: la geometria resta la stessa (controllo a fine creazione).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace YaNec.Engine
{
    public static class VariabileNuova
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        public const int Lunghezza = 0, Posizione = 1, Raggio = 2, Altezza = 3;
        static readonly string[] Campi = { "tag", "segmenti", "X1", "Y1", "Z1", "X2", "Y2", "Z2", "raggio" };

        public class Proposta
        {
            public int Tag, Tipo;
            public string Nome = "", Elemento = "", Cosa = "";
            public double Valore;            // nell'unita' della variabile (m; mm per il raggio)
            public string Unita = "m";
            public NecModel Modello;         // modello con la variabile gia' messa (null se non si puo')
            public string Errore;
        }

        /// <summary>Elemento: gruppo di fili GW collegati tra loro (filo diritto, loop, elemento piegato).</summary>
        public class Gruppo
        {
            public List<int> Schede = new List<int>();   // indici in Geometry
            public List<int> Tag = new List<int>();
            public double[] Centro = new double[3];
            public double Misura;                        // somma delle lunghezze dei fili (lunghezza o perimetro)
            public bool Chiuso, Diritto, SuAsse;         // loop chiuso; un filo diritto; sta in un piano perpendicolare all'asse
            public double Pos;                           // coordinata lungo l'asse
            public string Nome = "";
            public int Primo { get { return Tag.Count > 0 ? Tag.Min() : 0; } }
        }

        static double[] A(GeomCard c) { return new[] { c.F1, c.F2, c.F3 }; }
        static double[] B(GeomCard c) { return new[] { c.F4, c.F5, c.F6 }; }
        static double Dist(double[] p, double[] q) { return Math.Sqrt((p[0] - q[0]) * (p[0] - q[0]) + (p[1] - q[1]) * (p[1] - q[1]) + (p[2] - q[2]) * (p[2] - q[2])); }

        /// <summary>Elementi del modello (solo schede GW) e asse dell'antenna (-1 se non c'e': un elemento solo o sparsi).</summary>
        public static List<Gruppo> Gruppi(NecModel m, out int asse)
        {
            List<int> gw = new List<int>();
            for (int i = 0; i < m.Geometry.Count; i++) if (m.Geometry[i].Id == "GW") gw.Add(i);
            double dim = 1e-9;
            foreach (int i in gw) dim = Math.Max(dim, Dist(A(m.Geometry[i]), B(m.Geometry[i])));
            double tol = Math.Max(1e-6, dim * 1e-5);
            int[] par = new int[gw.Count];
            for (int i = 0; i < par.Length; i++) par[i] = i;
            Func<int, int> radice = null; radice = x => par[x] == x ? x : (par[x] = radice(par[x]));
            for (int i = 0; i < gw.Count; i++)
                for (int j = i + 1; j < gw.Count; j++)
                {
                    GeomCard p = m.Geometry[gw[i]], q = m.Geometry[gw[j]];
                    if (Dist(A(p), A(q)) < tol || Dist(A(p), B(q)) < tol || Dist(B(p), A(q)) < tol || Dist(B(p), B(q)) < tol) par[radice(i)] = radice(j);
                }
            Dictionary<int, Gruppo> d = new Dictionary<int, Gruppo>();
            for (int i = 0; i < gw.Count; i++)
            {
                int r = radice(i);
                Gruppo gr; if (!d.TryGetValue(r, out gr)) d[r] = gr = new Gruppo();
                gr.Schede.Add(gw[i]);
                int t = m.Geometry[gw[i]].I1; if (!gr.Tag.Contains(t)) gr.Tag.Add(t);
            }
            List<Gruppo> l = d.Values.ToList();
            foreach (Gruppo gr in l)
            {
                double s = 0; double[] c = new double[3];
                List<double[]> estremi = new List<double[]>();
                foreach (int i in gr.Schede)
                {
                    GeomCard g = m.Geometry[i]; double li = Dist(A(g), B(g)); s += li;
                    for (int k = 0; k < 3; k++) c[k] += li * (A(g)[k] + B(g)[k]) / 2;
                    estremi.Add(A(g)); estremi.Add(B(g));
                }
                gr.Misura = s;
                for (int k = 0; k < 3; k++) gr.Centro[k] = s > 0 ? c[k] / s : 0;
                // chiuso: ogni estremo ne tocca un altro
                gr.Chiuso = gr.Schede.Count > 2 && estremi.All(p => estremi.Count(q => Dist(p, q) < tol) >= 2);
                gr.Diritto = gr.Schede.Count == 1;
            }
            asse = -1;
            if (l.Count >= 2)
            {
                double est = -1;
                for (int k = 0; k < 3; k++)
                {
                    double e = l.Max(x => x.Centro[k]) - l.Min(x => x.Centro[k]);
                    if (e > est) { est = e; asse = k; }
                }
                if (est < tol) asse = -1;
            }
            foreach (Gruppo gr in l)
            {
                gr.Pos = asse >= 0 ? gr.Centro[asse] : 0;
                if (asse >= 0)
                {
                    double mn = double.MaxValue, mx = double.MinValue;
                    foreach (int i in gr.Schede) foreach (double[] p in new[] { A(m.Geometry[i]), B(m.Geometry[i]) }) { mn = Math.Min(mn, p[asse]); mx = Math.Max(mx, p[asse]); }
                    gr.SuAsse = mx - mn < Math.Max(tol, 0.02 * gr.Misura);
                }
            }
            l = l.OrderBy(x => x.Pos).ThenBy(x => x.Primo).ToList();
            foreach (Gruppo gr in l)
            {
                List<int> t = gr.Tag.OrderBy(x => x).ToList();
                string fili = t.Count == 1 ? "filo " + t[0] : (t.Count == t[t.Count - 1] - t[0] + 1 ? "fili " + t[0] + "-" + t[t.Count - 1] : "fili " + string.Join(", ", t));
                gr.Nome = (gr.Diritto ? "Elemento diritto" : gr.Chiuso ? "Loop" : "Elemento piegato") + " (" + fili + ")";
            }
            return l;
        }

        static string F(double v)
        {
            if (Math.Abs(v) < 1e-12) v = 0;
            return Math.Round(v, 9).ToString("0.#########", CI);
        }

        static bool Numero(string s, out double v)
        {
            return double.TryParse((s ?? "").Trim(), NumberStyles.Float, CI, out v);
        }

        static string[] SrcDi(GeomCard g)
        {
            if (g.Src != null) return (string[])g.Src.Clone();
            return new[] { g.I1.ToString(CI), g.I2.ToString(CI), F(g.F1), F(g.F2), F(g.F3), F(g.F4), F(g.F5), F(g.F6), F(g.F7) };
        }

        // c + k*N senza spazi (k = 1/2 -> N/2)
        static string Lin(double c, double k, string n)
        {
            double ak = Math.Abs(k);
            string t = Math.Abs(ak - 0.5) < 1e-9 ? n + "/2" : Math.Abs(ak - 1) < 1e-9 ? n : F(ak) + "*" + n;
            bool neg = k < 0;
            if (Math.Abs(c) < 1e-12) return (neg ? "-" : "") + t;
            return F(c) + (neg ? "-" : "+") + t;
        }

        /// <summary>Nome libero (L3, P3, R3, H): con un suffisso se esiste gia'.</summary>
        public static string NomeLibero(NecModel m, string base0)
        {
            string n = base0; int k = 2;
            while (m.Variables.Exists(v => string.Equals(v.Name, n, StringComparison.OrdinalIgnoreCase)) || !Expr.IsValidName(n)) n = base0 + "_" + (k++);
            return n;
        }

        /// <summary>Prepara la variabile (senza toccare il modello aperto): modello nuovo, campi cambiati, valore. gruppo = indice in Gruppi().</summary>
        public static Proposta Prepara(NecModel m0, int gruppo, int tipo, string nome)
        {
            Proposta p = new Proposta { Tipo = tipo, Nome = nome };
            try
            {
                if (!Expr.IsValidName(nome)) throw new NecException("«" + nome + "» non e' un nome di variabile valido");
                if (m0.Variables.Exists(v => string.Equals(v.Name, nome, StringComparison.OrdinalIgnoreCase))) throw new NecException("la variabile " + nome + " esiste gia'");
                NecModel m = m0.Clone();
                if (tipo == Altezza)
                {
                    if (m.Geometry.Exists(c => c.Id != "GW" && c.Id != "GE" && c.Id != "GC"))
                        throw new NecException("la quota come variabile vale solo per i modelli fatti di schede GW");
                    int nf = 0;
                    foreach (GeomCard c in m.Geometry)
                    {
                        if (c.Id != "GW") continue;
                        string[] s = SrcDi(c);
                        foreach (int i in new[] { 4, 7 })
                        {
                            double v;
                            s[i] = Numero(s[i], out v) ? (Math.Abs(v) < 1e-12 ? nome : F(v) + "+" + nome) : "(" + s[i] + ")+" + nome;
                        }
                        c.Src = s; nf++;
                        p.Tag = c.I1;
                    }
                    p.Valore = 0; p.Elemento = "tutta l'antenna";
                    p.Cosa = "Z1 e Z2 di tutti i fili (" + nf + "): quota + " + nome;
                    m.DefineVariable(nome, "0");
                    SetDescr(m, nome, "quota aggiunta a tutta l'antenna (m)");
                }
                else
                {
                    int asse;
                    List<Gruppo> gs = Gruppi(m0, out asse);
                    if (gruppo < 0 || gruppo >= gs.Count) throw new NecException("elemento non trovato");
                    Gruppo gr = gs[gruppo];
                    p.Tag = gr.Primo; p.Elemento = gr.Nome;
                    List<string> cam = new List<string>();
                    string fili = gr.Tag.Count == 1 ? "filo " + gr.Tag[0] : "fili " + string.Join(", ", gr.Tag.OrderBy(x => x));
                    if (tipo == Lunghezza)
                    {
                        double S0 = gr.Misura;
                        if (S0 <= 0) throw new NecException("elemento di lunghezza nulla");
                        foreach (int i in gr.Schede)
                        {
                            GeomCard c = m.Geometry[i];
                            if (c.HasGc) throw new NecException("il filo " + c.I1 + " e' rastremato (GC): non ancora");
                            string[] s = SrcDi(c);
                            double[] q = { c.F1, c.F2, c.F3, c.F4, c.F5, c.F6 };
                            for (int k = 0; k < 6; k++)
                            {
                                double off = q[k] - gr.Centro[k % 3];
                                if (Math.Abs(off) < 1e-9) continue;
                                double v; if (!Numero(s[2 + k], out v)) throw new NecException("il filo " + c.I1 + " ha gia' una formula in " + Campi[2 + k]);
                                s[2 + k] = Lin(gr.Centro[k % 3], off / S0, nome);
                            }
                            c.Src = s;
                            if (cam.Count < 2) cam.Add("filo " + c.I1 + ": " + string.Join(", ", Enumerable.Range(2, 6).Where(k => s[k] != SrcDi(m0.Geometry[i])[k]).Select(k => Campi[k] + " = " + s[k])));
                        }
                        if (gr.Schede.Count > 2) cam.Add("…");
                        p.Valore = S0;
                        p.Cosa = string.Join("; ", cam);
                        m.DefineVariable(nome, F(S0));
                        SetDescr(m, nome, (gr.Diritto ? "lunghezza" : gr.Chiuso ? "perimetro" : "lunghezza sviluppata") + " (" + fili + ")");
                    }
                    else if (tipo == Posizione)
                    {
                        if (asse < 0 || !gr.SuAsse) throw new NecException("l'elemento non sta su un asse comune agli altri: posizione non definita");
                        double pc = gr.Pos;
                        foreach (int i in gr.Schede)
                        {
                            GeomCard c = m.Geometry[i];
                            string[] s = SrcDi(c);
                            double[] q = { c.F1, c.F2, c.F3, c.F4, c.F5, c.F6 };
                            foreach (int k in new[] { asse, asse + 3 })
                            {
                                double v; if (!Numero(s[2 + k], out v)) throw new NecException("il filo " + c.I1 + " ha gia' una formula in " + Campi[2 + k]);
                                double off = q[k] - pc;
                                s[2 + k] = Math.Abs(off) < 1e-9 ? nome : nome + (off > 0 ? "+" : "-") + F(Math.Abs(off));
                            }
                            c.Src = s;
                        }
                        p.Valore = pc;
                        p.Cosa = fili + ": " + Campi[2 + asse] + " e " + Campi[5 + asse] + " = " + nome;
                        m.DefineVariable(nome, F(pc));
                        SetDescr(m, nome, "posizione lungo l'asse " + "XYZ"[asse] + " (" + fili + ")");
                    }
                    else
                    {
                        double r0 = m.Geometry[gr.Schede[0]].F7;
                        foreach (int i in gr.Schede)
                        {
                            GeomCard c = m.Geometry[i];
                            if (c.HasGc || Math.Abs(c.F7 - r0) > 1e-9) throw new NecException("i fili dell'elemento hanno raggi diversi o rastremati: non ancora");
                            string[] s = SrcDi(c);
                            double v; if (!Numero(s[8], out v)) throw new NecException("il filo " + c.I1 + " ha gia' una formula nel raggio");
                            s[8] = nome + "*MM";
                            c.Src = s;
                        }
                        p.Valore = r0 * 1000; p.Unita = "mm";
                        p.Cosa = fili + ": raggio = " + nome + "*MM";
                        m.DefineVariable(nome, F(p.Valore));
                        SetDescr(m, nome, "raggio in mm (" + fili + ")");
                    }
                }
                List<string> err = m.Reevaluate();
                if (err.Count > 0) throw new NecException(err[0]);
                // controllo: la geometria non deve cambiare
                SegmentGeometry g0 = m0.BuildGeometry(), g1 = m.BuildGeometry();
                if (g0.N != g1.N) throw new NecException("il numero dei segmenti e' cambiato: variabile non creata");
                for (int i = 0; i < g0.N; i++)
                    if (Math.Abs(g0.X1[i] - g1.X1[i]) + Math.Abs(g0.Y1[i] - g1.Y1[i]) + Math.Abs(g0.Z1[i] - g1.Z1[i]) + Math.Abs(g0.X2[i] - g1.X2[i]) +
                        Math.Abs(g0.Y2[i] - g1.Y2[i]) + Math.Abs(g0.Z2[i] - g1.Z2[i]) + Math.Abs(g0.Bi[i] - g1.Bi[i]) > 1e-7)
                        throw new NecException("la geometria cambierebbe (segmento " + (i + 1) + "): variabile non creata");
                p.Modello = m;
            }
            catch (Exception ex) { p.Errore = ex.Message; }
            return p;
        }

        static void SetDescr(NecModel m, string nome, string d)
        {
            SyVar v = m.Variables.Find(q => string.Equals(q.Name, nome, StringComparison.OrdinalIgnoreCase));
            if (v != null && string.IsNullOrWhiteSpace(v.Descr)) v.Descr = d;
        }

        /// <summary>Dove agisce una variabile: fili (tag) che la usano direttamente, con i campi, e fili che la usano attraverso altre variabili.</summary>
        public static void DoveAgisce(NecModel m, string nome, out Dictionary<int, List<string>> diretti, out Dictionary<int, List<string>> indiretti, out List<string> tramite)
        {
            diretti = new Dictionary<int, List<string>>();
            indiretti = new Dictionary<int, List<string>>();
            tramite = new List<string>();
            // variabili che dipendono (anche a catena) da nome
            HashSet<string> dip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool cresce = true;
            while (cresce)
            {
                cresce = false;
                foreach (SyVar v in m.Variables)
                {
                    if (string.Equals(v.Name, nome, StringComparison.OrdinalIgnoreCase) || dip.Contains(v.Name)) continue;
                    if (NecModel.ContainsName(v.Expr, nome) || dip.Any(d => NecModel.ContainsName(v.Expr, d))) { dip.Add(v.Name); cresce = true; }
                }
            }
            tramite.AddRange(dip);
            foreach (GeomCard c in m.Geometry)
            {
                if (c.Id != "GW" || c.Src == null) continue;
                for (int k = 2; k < c.Src.Length && k < Campi.Length; k++)
                {
                    if (NecModel.ContainsName(c.Src[k], nome)) Agg(diretti, c.I1, Campi[k]);
                    else if (dip.Any(d => NecModel.ContainsName(c.Src[k], d))) Agg(indiretti, c.I1, Campi[k]);
                }
            }
        }

        static void Agg(Dictionary<int, List<string>> d, int tag, string campo)
        {
            List<string> l;
            if (!d.TryGetValue(tag, out l)) d[tag] = l = new List<string>();
            if (!l.Contains(campo)) l.Add(campo);
        }
    }
}
