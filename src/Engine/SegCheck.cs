// YA-NEC - verifica della segmentazione con correzione automatica (bozzetto approvato).
// Regole (EZNEC, ARRL, manuale NEC-2 di Burke e Poggio): segmento <= lambda/10 (minimo) e <= lambda/20 (prudente);
// segmento/raggio >= 2 (minimo) e >= 4 (prudente, sotto conviene il kernel esteso); numero dispari di segmenti sui fili
// alimentati al centro; segmenti di lunghezza simile ai lati di sorgenti e porte delle linee e nelle giunzioni.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public class SegIssue
    {
        public bool Sel = true;
        public string Dove = "", Problema = "", Ora = "-", Minimo = "-", Prudente = "-", Fuori = "", Segmenti = "", SegDopo = "-";
        public int Card = -1;          // indice della scheda GW interessata (-1 = nessuna)
        public int NewN;               // segmenti proposti
    }

    public class SegOptions
    {
        public int Livello = 1;        // 0 minimo lambda/10, 1 prudente lambda/20, 2 segmenti per lambda
        public int PerLambda = 40;
        public bool SoloSegnalati = true, Dispari = true, Pareggia = true, Giunzioni = true, ProponiEK = false;
    }

    public static class SegCheck
    {
        static readonly CultureInfo CC = CultureInfo.CurrentCulture;
        static string M(double v) { return v.ToString(v < 1 ? "0.###" : "0.##", CC) + " m"; }

        static double Len(GeomCard c)
        {
            return Math.Sqrt((c.F4 - c.F1) * (c.F4 - c.F1) + (c.F5 - c.F2) * (c.F5 - c.F2) + (c.F6 - c.F3) * (c.F6 - c.F3));
        }

        static bool Vicino(double x1, double y1, double z1, double x2, double y2, double z2, double tol)
        {
            return Math.Abs(x1 - x2) < tol && Math.Abs(y1 - y2) < tol && Math.Abs(z1 - z2) < tol;
        }

        /// <summary>Lunghezza del segmento voluta per il livello scelto.</summary>
        public static double Obiettivo(double lam, SegOptions o)
        {
            return o.Livello == 0 ? lam / 10 : (o.Livello == 1 ? lam / 20 : lam / Math.Max(5, o.PerLambda));
        }

        static int TagCount(NecModel m, int tag)
        {
            int k = 0;
            foreach (GeomCard c in m.Geometry) if (c.Id == "GW" && c.I1 == tag) k++;
            return k;
        }

        /// <summary>Elenco dei problemi alla frequenza fmax (la piu' alta dello sweep) con i segmenti proposti.</summary>
        public static List<SegIssue> Check(NecModel m, double fmax, SegOptions o)
        {
            List<SegIssue> l = new List<SegIssue>();
            double lam = 299.792458 / fmax, tgt = Obiettivo(lam, o);
            List<GeomCard> g = m.Geometry;
            HashSet<int> centro = new HashSet<int>();
            foreach (SourceSpec s in m.Sources)
                for (int k = 0; k < g.Count; k++)
                    if (g[k].Id == "GW" && g[k].I1 == s.Tag && g[k].I2 > 0 && Math.Abs((s.Seg - 0.5) / g[k].I2 - 0.5) < 0.5 / g[k].I2 + 1e-9) centro.Add(k);

            for (int k = 0; k < g.Count; k++)
            {
                GeomCard c = g[k];
                if (c.Id != "GW" || c.I2 < 1) continue;
                string dove = "Filo " + c.I1;
                if (c.HasGc || c.F7 <= 0) { l.Add(new SegIssue { Dove = dove, Problema = "filo rastremato (GC): segmentazione non modificata", Fuori = "info", Sel = false, Card = -1, Segmenti = c.I2.ToString() }); continue; }
                double L = Len(c), seg = L / c.I2, r = c.F7;
                int nNew = Math.Max(c.I2, (int)Math.Ceiling(L / tgt - 1e-9));
                if (o.Dispari && centro.Contains(k) && nNew % 2 == 0) nNew++;
                bool lungo = seg > lam / 10 + 1e-12, lungoP = seg > tgt + 1e-12;
                if (lungo || lungoP)
                    l.Add(new SegIssue
                    {
                        Dove = dove, Problema = "segmento troppo lungo", Ora = M(seg), Minimo = M(lam / 10), Prudente = M(tgt),
                        Fuori = lungo ? "minimo" : (o.Livello == 2 ? "segmenti per lambda" : "prudente"), Card = k, NewN = nNew,
                        Segmenti = c.I2 + " → " + nNew, SegDopo = M(L / nNew)
                    });
                double sr = (L / Math.Max(nNew, c.I2)) / r;
                if (sr < 4)
                    l.Add(new SegIssue
                    {
                        Dove = dove, Problema = "segmento corto rispetto al raggio (" + (L / Math.Max(nNew, c.I2) / r).ToString("0.0", CC) + " volte)", Ora = (seg / r).ToString("0.0", CC),
                        Minimo = "2", Prudente = "4", Fuori = sr < 2 ? "minimo" : "prudente", Card = -1, Segmenti = o.ProponiEK ? "kernel esteso" : "-",
                        Sel = o.ProponiEK
                    });
                if (o.Dispari && centro.Contains(k) && c.I2 % 2 == 0)
                    l.Add(new SegIssue { Dove = dove, Problema = "alimentato al centro con un numero pari di segmenti", Ora = c.I2.ToString(), Minimo = "dispari", Prudente = "dispari", Fuori = "minimo", Card = k, NewN = c.I2 + 1, Segmenti = c.I2 + " → " + (c.I2 + 1) });
            }

            // segmenti vicini molto diversi: ai lati di sorgenti e porte delle linee (pareggia) e nelle giunzioni (rastremazione)
            for (int a = 0; a < g.Count; a++)
            {
                if (g[a].Id != "GW" || g[a].I2 < 1 || g[a].HasGc) continue;
                for (int b = a + 1; b < g.Count; b++)
                {
                    if (g[b].Id != "GW" || g[b].I2 < 1 || g[b].HasGc) continue;
                    GeomCard A = g[a], B = g[b];
                    double tol = 1e-4 * Math.Max(1, Math.Max(Len(A), Len(B)));
                    bool giunti = Vicino(A.F1, A.F2, A.F3, B.F1, B.F2, B.F3, tol) || Vicino(A.F1, A.F2, A.F3, B.F4, B.F5, B.F6, tol) ||
                                  Vicino(A.F4, A.F5, A.F6, B.F1, B.F2, B.F3, tol) || Vicino(A.F4, A.F5, A.F6, B.F4, B.F5, B.F6, tol);
                    if (!giunti) continue;
                    double sa = Len(A) / A.I2, sb = Len(B) / B.I2, ra = A.F7, rb = B.F7;
                    bool porta = false;
                    foreach (SourceSpec s in m.Sources) if (s.Tag == A.I1 || s.Tag == B.I1) porta = true;
                    foreach (NetworkSpec n in m.Networks) if (n.Tag1 == A.I1 || n.Tag1 == B.I1 || n.Tag2 == A.I1 || n.Tag2 == B.I1) porta = true;
                    double rap = Math.Max(sa, sb) / Math.Min(sa, sb);
                    if ((porta && o.Pareggia && (rap > 1.5 || Math.Abs(ra - rb) > 1e-9 * Math.Max(ra, rb) && rap > 1.2)) || (o.Giunzioni && rap > 3))
                    {
                        bool aGrosso = sa > sb;
                        GeomCard G = aGrosso ? A : B;
                        double fine = Math.Min(sa, sb);
                        int nNew = Math.Max(G.I2, (int)Math.Ceiling(Len(G) / (fine * (porta ? 1.25 : 2.0))));
                        l.Add(new SegIssue
                        {
                            Dove = (porta ? "Porta, fili " : "Giunzione, fili ") + A.I1 + "-" + B.I1, Problema = "segmenti vicini di lunghezza" + (Math.Abs(ra - rb) > 1e-12 ? " o diametro" : "") + " diversi",
                            Ora = M(Math.Max(sa, sb)) + " / " + M(fine), Prudente = porta ? "uguali" : "rapporto <= 2", Fuori = porta ? "prudente" : "prudente",
                            Card = g.IndexOf(G), NewN = nNew, Segmenti = porta ? "pareggia" : "rastrema", SegDopo = M(Len(G) / nNew)
                        });
                    }
                }
            }
            Geometria(m, l);
            return l;
        }

        // distanza minima tra due segmenti di retta 3D (punti piu' vicini)
        static double DistSeg(GeomCard A, GeomCard B)
        {
            double[] p = { A.F1, A.F2, A.F3 }, d1 = { A.F4 - A.F1, A.F5 - A.F2, A.F6 - A.F3 };
            double[] q = { B.F1, B.F2, B.F3 }, d2 = { B.F4 - B.F1, B.F5 - B.F2, B.F6 - B.F3 };
            double[] r = { p[0] - q[0], p[1] - q[1], p[2] - q[2] };
            Func<double[], double[], double> dot = (u, v) => u[0] * v[0] + u[1] * v[1] + u[2] * v[2];
            double a = dot(d1, d1), e = dot(d2, d2), f = dot(d2, r), c = dot(d1, r), b = dot(d1, d2), den = a * e - b * b;
            double sN = den > 1e-18 ? Math.Max(0, Math.Min(1, (b * f - c * e) / den)) : 0;
            double tN = e > 1e-18 ? (b * sN + f) / e : 0;
            if (tN < 0) { tN = 0; sN = a > 1e-18 ? Math.Max(0, Math.Min(1, -c / a)) : 0; }
            else if (tN > 1) { tN = 1; sN = a > 1e-18 ? Math.Max(0, Math.Min(1, (b - c) / a)) : 0; }
            double dx = p[0] + d1[0] * sN - q[0] - d2[0] * tN, dy = p[1] + d1[1] * sN - q[1] - d2[1] * tN, dz = p[2] + d1[2] * sN - q[2] - d2[2] * tN;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Controllo della geometria (0.4.2, approvato da IC8TEM il 13/11/2026; idee di N. Mladenov, nec.opt, e di antsim):
        /// estremi quasi coincidenti (sotto 1 cm) che il NEC non collega; fili non collegati con le superfici a meno di 5 mm o che si
        /// incrociano. Solo avvisi: la correzione resta all'utente.</summary>
        static void Geometria(NecModel m, List<SegIssue> l)
        {
            List<GeomCard> g = m.Geometry.FindAll(c => c.Id == "GW" && c.I2 > 0);
            if (g.Count > 400) return;   // con molti fili (griglie) il controllo a coppie diventa lento e poco utile
            for (int a = 0; a < g.Count; a++)
                for (int b = a + 1; b < g.Count; b++)
                {
                    GeomCard A = g[a], B = g[b];
                    double tol = 1e-4 * Math.Max(1, Math.Max(Len(A), Len(B)));
                    double[][] ea = { new[] { A.F1, A.F2, A.F3 }, new[] { A.F4, A.F5, A.F6 } }, eb = { new[] { B.F1, B.F2, B.F3 }, new[] { B.F4, B.F5, B.F6 } };
                    bool giunti = false; double quasi = double.MaxValue;
                    foreach (double[] u in ea)
                        foreach (double[] v in eb)
                        {
                            double d = Math.Sqrt((u[0] - v[0]) * (u[0] - v[0]) + (u[1] - v[1]) * (u[1] - v[1]) + (u[2] - v[2]) * (u[2] - v[2]));
                            if (d < tol) giunti = true; else quasi = Math.Min(quasi, d);
                        }
                    if (giunti) continue;
                    if (quasi < 0.01)
                    {
                        l.Add(new SegIssue { Dove = "Fili " + A.I1 + "-" + B.I1, Problema = "estremi quasi coincidenti ma non collegati (" + (quasi * 1000).ToString("0.#", CC) + " mm)", Ora = (quasi * 1000).ToString("0.#", CC) + " mm", Minimo = "0 o > 10 mm", Prudente = "-", Fuori = "minimo", Sel = false, Card = -1, Segmenti = "-" });
                        continue;
                    }
                    double ds = DistSeg(A, B), sup = ds - A.F7 - B.F7;
                    if (ds < tol)
                        l.Add(new SegIssue { Dove = "Fili " + A.I1 + "-" + B.I1, Problema = "fili che si incrociano senza giunzione", Ora = "0 mm", Minimo = "giunzione", Prudente = "-", Fuori = "minimo", Sel = false, Card = -1, Segmenti = "-" });
                    else if (sup < 0.005)
                        l.Add(new SegIssue { Dove = "Fili " + A.I1 + "-" + B.I1, Problema = "fili non collegati troppo vicini (superficie-superficie " + (sup * 1000).ToString("0.#", CC) + " mm)", Ora = (sup * 1000).ToString("0.#", CC) + " mm", Minimo = "5 mm", Prudente = "-", Fuori = sup <= 0 ? "minimo" : "prudente", Sel = false, Card = -1, Segmenti = "-" });
                }
        }

        /// <summary>Copia del modello con i segmenti corretti; sorgenti, carichi e linee spostati sugli stessi punti.</summary>
        public static NecModel Correct(NecModel src, double fmax, SegOptions o, List<SegIssue> scelti)
        {
            NecModel m = NecModel.Parse(src.ToNec());
            double lam = 299.792458 / fmax, tgt = Obiettivo(lam, o);
            Dictionary<int, int> nuovi = new Dictionary<int, int>();
            foreach (SegIssue i in scelti)
                if (i.Sel && i.Card >= 0)
                {
                    int v;
                    nuovi[i.Card] = nuovi.TryGetValue(i.Card, out v) ? Math.Max(v, i.NewN) : i.NewN;
                }
            if (!o.SoloSegnalati)
                for (int k = 0; k < m.Geometry.Count; k++)
                {
                    GeomCard c = m.Geometry[k];
                    if (c.Id != "GW" || c.I2 < 1 || c.HasGc) continue;
                    int n = Math.Max(c.I2, (int)Math.Ceiling(Len(c) / tgt - 1e-9));
                    int v;
                    nuovi[k] = nuovi.TryGetValue(k, out v) ? Math.Max(v, n) : n;
                }
            foreach (SegIssue i in scelti) if (i.Sel && o.ProponiEK && i.Segmenti == "kernel esteso") m.ExtendedKernel = true;

            // segmenti per tag prima e dopo (servono a spostare i riferimenti)
            Dictionary<int, int> prima = Conta(m.BuildGeometry());
            foreach (KeyValuePair<int, int> kv in nuovi)
            {
                GeomCard c = m.Geometry[kv.Key];
                if (TagCount(m, c.I1) > 1) continue;     // tag condiviso da piu' schede: non si tocca
                int n = kv.Value;
                bool centro = false;
                foreach (SourceSpec s in m.Sources) if (s.Tag == c.I1 && 2 * s.Seg - 1 == c.I2) centro = true;
                if (o.Dispari && centro && n % 2 == 0) n++;
                c.I2 = n;
                if (c.Src != null && c.Src.Length > 1) c.Src[1] = null;
            }
            Dictionary<int, int> dopo = Conta(m.BuildGeometry());
            Func<int, int, int> sposta = (tag, seg) =>
            {
                int a, b;
                if (!prima.TryGetValue(tag, out a) || !dopo.TryGetValue(tag, out b) || a == b || seg <= 0) return seg;
                if (a % 2 == 1 && b % 2 == 1 && seg == (a + 1) / 2) return (b + 1) / 2;
                int s = (int)Math.Round((seg - 0.5) * b / a + 0.5);
                return Math.Max(1, Math.Min(b, s));
            };
            foreach (SourceSpec s in m.Sources) s.Seg = sposta(s.Tag, s.Seg);
            foreach (LoadSpec ld in m.Loads)
            {
                int a, b;
                if (ld.Tag > 0 && prima.TryGetValue(ld.Tag, out a) && dopo.TryGetValue(ld.Tag, out b) && ld.From == 1 && ld.To == a) { ld.To = b; continue; }
                if (ld.From > 0) ld.From = sposta(ld.Tag, ld.From);
                if (ld.To > 0) ld.To = sposta(ld.Tag, ld.To);
            }
            foreach (NetworkSpec n in m.Networks) { n.Seg1 = sposta(n.Tag1, n.Seg1); n.Seg2 = sposta(n.Tag2, n.Seg2); }
            m.FileName = src.FileName;
            return m;
        }

        static Dictionary<int, int> Conta(SegmentGeometry g)
        {
            Dictionary<int, int> d = new Dictionary<int, int>();
            for (int i = 0; i < g.N; i++) { int v; d[g.Tag[i]] = d.TryGetValue(g.Tag[i], out v) ? v + 1 : 1; }
            return d;
        }

        public static int Segmenti(NecModel m) { try { return m.BuildGeometry().N; } catch { return 0; } }

        /// <summary>Testo del rapporto (pulsante "Copia il rapporto").</summary>
        public static string Rapporto(List<SegIssue> l, string titolo)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(titolo);
            sb.AppendLine("Dove\tProblema\tOra\tMinimo\tPrudente\tFuori dal\tSegmenti\tSegm. dopo");
            foreach (SegIssue i in l) sb.AppendLine(i.Dove + "\t" + i.Problema + "\t" + i.Ora + "\t" + i.Minimo + "\t" + i.Prudente + "\t" + i.Fuori + "\t" + i.Segmenti + "\t" + i.SegDopo);
            return sb.ToString();
        }
    }
}
