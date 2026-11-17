// YA-NEC 1.2.0 - tolleranze costruttive (bozzetto VHF 9): Monte Carlo con seme fisso (ripetibile) e sensibilita' un elemento alla volta.
// Gli elementi sono i fili diritti (un tag = un elemento); gli errori si applicano alla geometria a segmenti gia' costruita:
// lunghezza (simmetrica rispetto al centro), posizione sul boom (spostamento lungo l'asse del boom), diametro, errore della staffa
// (elemento fuori centro lungo il proprio asse). Ogni prova e' una soluzione NEC a tre frequenze (inizio, centro e fine della banda).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace YaNec.Engine
{
    public static class Tolleranze
    {
        public class Elemento
        {
            public int Tag; public string Nome = ""; public bool Alimentato;
            public double Pos, Lung, Diam;              // m
            public double[] Dir = new double[3];        // direzione del filo
            public double Sens = double.NaN;            // calo di F/B per mm di errore di lunghezza (dB/mm)
            public double TollAmmessa = double.NaN;     // mm
        }

        public class Opzioni
        {
            public double TolL = 1.0, TolP = 2.0, TolD = 0.1;   // mm
            public bool Gauss, Foratura = true, Staffa;
            public int Prove = 500;
            public double F1, F2;                              // MHz
            public int Seme = 12345;
            public double CaloFbMax = 1.0, RosMax = 1.3;
        }

        public class Prova { public double G, Fb, Ros; }

        public class Esito
        {
            public Prova Nominale;
            public List<Prova> Prove = new List<Prova>();
            public List<Elemento> Elementi = new List<Elemento>();
            public double G5, Fb5, Ros95;
            public string Critici = "";
        }

        /// <summary>Elementi del modello: tag con tutti i segmenti allineati; asse del boom = asse con la massima estensione dei centri.</summary>
        public static List<Elemento> Elementi(NecModel m, SegmentGeometry g, out int asse)
        {
            Dictionary<int, List<int>> perTag = new Dictionary<int, List<int>>();
            for (int i = 0; i < g.N; i++) { if (!perTag.ContainsKey(g.Tag[i])) perTag[g.Tag[i]] = new List<int>(); perTag[g.Tag[i]].Add(i); }
            List<Elemento> l = new List<Elemento>();
            List<double[]> centri = new List<double[]>();
            int sTag = m.Sources.Count > 0 ? m.Sources[0].Tag : -1;
            foreach (var kv in perTag)
            {
                List<int> s = kv.Value;
                double x1 = g.X1[s[0]], y1 = g.Y1[s[0]], z1 = g.Z1[s[0]], x2 = g.X2[s[s.Count - 1]], y2 = g.Y2[s[s.Count - 1]], z2 = g.Z2[s[s.Count - 1]];
                double dx = x2 - x1, dy = y2 - y1, dz = z2 - z1, L = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (L < 1e-6) continue;
                bool dritto = true; double somma = 0;
                foreach (int i in s)
                {
                    double ex = g.X2[i] - g.X1[i], ey = g.Y2[i] - g.Y1[i], ez = g.Z2[i] - g.Z1[i], li = Math.Sqrt(ex * ex + ey * ey + ez * ez);
                    somma += li;
                    if (li > 0 && (ex * dx + ey * dy + ez * dz) / (li * L) < 0.999) dritto = false;
                }
                if (!dritto || Math.Abs(somma - L) > 1e-6 * Math.Max(1, L)) continue;
                double r = 0; foreach (int i in s) r += g.Bi[i]; r /= s.Count;
                l.Add(new Elemento { Tag = kv.Key, Alimentato = kv.Key == sTag, Lung = L, Diam = 2 * r, Dir = new[] { dx / L, dy / L, dz / L } });
                centri.Add(new[] { (x1 + x2) / 2, (y1 + y2) / 2, (z1 + z2) / 2 });
            }
            asse = 0; double est = -1;
            for (int a = 0; a < 3; a++)
            {
                if (centri.Count == 0) break;
                double e = centri.Max(c => c[a]) - centri.Min(c => c[a]);
                if (e > est) { est = e; asse = a; }
            }
            for (int i = 0; i < l.Count; i++) l[i].Pos = centri[i][asse];
            l = l.OrderBy(e => e.Pos).ToList();
            // nomi: prima del radiatore R (o R1, R2), il radiatore DR, poi D1, D2...
            int iDr = l.FindIndex(e => e.Alimentato);
            if (iDr < 0) iDr = 0;
            double p0 = l.Count > 0 ? l[0].Pos : 0;
            // il boom va dal riflettore: se il radiatore e' oltre la meta', l'ordine va invertito (direttori verso le posizioni piu' basse)
            if (l.Count > 2 && iDr > l.Count / 2) { l.Reverse(); iDr = l.Count - 1 - iDr; p0 = l[0].Pos; foreach (Elemento e in l) e.Pos = p0 - e.Pos; }
            else foreach (Elemento e in l) e.Pos -= p0;
            for (int i = 0; i < l.Count; i++)
                l[i].Nome = i == iDr ? "DR" : i < iDr ? (iDr == 1 ? "R" : "R" + (i + 1)) : "D" + (i - iDr);
            return l;
        }

        static SegmentGeometry Copia(SegmentGeometry g)
        {
            SegmentGeometry c = (SegmentGeometry)typeof(SegmentGeometry).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(g, null);
            c.X1 = (double[])g.X1.Clone(); c.Y1 = (double[])g.Y1.Clone(); c.Z1 = (double[])g.Z1.Clone();
            c.X2 = (double[])g.X2.Clone(); c.Y2 = (double[])g.Y2.Clone(); c.Z2 = (double[])g.Z2.Clone();
            c.Bi = (double[])g.Bi.Clone();
            c.SymNop = 0; c.SymNp = 0;
            return c;
        }

        /// <summary>Applica a un elemento: dL allungamento totale, dP spostamento lungo il boom, dD variazione del diametro, dC spostamento lungo il filo (m).</summary>
        static void Applica(SegmentGeometry g, Elemento e, int asse, double dL, double dP, double dD, double dC)
        {
            double cx = 0, cy = 0, cz = 0; int n = 0;
            for (int i = 0; i < g.N; i++) if (g.Tag[i] == e.Tag) { cx += g.X1[i] + g.X2[i]; cy += g.Y1[i] + g.Y2[i]; cz += g.Z1[i] + g.Z2[i]; n += 2; }
            if (n == 0) return;
            cx /= n; cy /= n; cz /= n;
            double k = (e.Lung + dL) / e.Lung;
            double[] sp = { e.Dir[0] * dC, e.Dir[1] * dC, e.Dir[2] * dC };
            sp[asse] += dP;
            for (int i = 0; i < g.N; i++)
            {
                if (g.Tag[i] != e.Tag) continue;
                g.X1[i] = cx + (g.X1[i] - cx) * k + sp[0]; g.Y1[i] = cy + (g.Y1[i] - cy) * k + sp[1]; g.Z1[i] = cz + (g.Z1[i] - cz) * k + sp[2];
                g.X2[i] = cx + (g.X2[i] - cx) * k + sp[0]; g.Y2[i] = cy + (g.Y2[i] - cy) * k + sp[1]; g.Z2[i] = cz + (g.Z2[i] - cz) * k + sp[2];
                g.Bi[i] = Math.Max(1e-5, g.Bi[i] + dD / 2);
            }
        }

        static Prova Valuta(NecModel m, SegmentGeometry g, Opzioni o, double th, double ph, CancellationToken ct)
        {
            double fc = (o.F1 + o.F2) / 2;
            List<double> fs = o.F2 - o.F1 > 1e-6 ? new List<double> { o.F1, fc, o.F2 } : new List<double> { fc };
            List<SweepPoint> sp = Analysis.Sweep(m, g, fs, th, ph, null, ct);
            SweepPoint c = sp[fs.Count == 3 ? 1 : 0];
            return new Prova { G = c.GainFwd, Fb = c.FrontBack, Ros = sp.Where(p => p.Ok).Select(p => p.Swr).DefaultIfEmpty(double.NaN).Max() };
        }

        static double Gauss(Random r) { double u1 = 1 - r.NextDouble(), u2 = r.NextDouble(); return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2); }

        /// <summary>Errore casuale con tolleranza t: uniforme in [-t, t] oppure gaussiano con t = 2 sigma.</summary>
        static double Errore(Random r, double t, bool gauss) { return gauss ? Gauss(r) * t / 2 : (2 * r.NextDouble() - 1) * t; }

        /// <summary>Sensibilita' (dB di calo del F/B per mm) e tolleranza ammessa di ogni elemento. Il budget di calo del F/B e' diviso
        /// tra gli elementi come somma quadratica (errori indipendenti): ogni elemento puo' far calare il F/B di CaloFbMax / radice(N).</summary>
        public static void Sensibilita(NecModel m, SegmentGeometry g, List<Elemento> el, int asse, Opzioni o, double th, double ph, Prova nom, IProgress<int> prog, CancellationToken ct)
        {
            int nEl = Math.Max(1, el.Count(e => !e.Alimentato));
            double budget = o.CaloFbMax / Math.Sqrt(nEl);
            int k = 0;
            foreach (Elemento e in el)
            {
                ct.ThrowIfCancellationRequested();
                if (e.Alimentato) { e.Sens = double.NaN; e.TollAmmessa = double.NaN; prog?.Report(++k); continue; }
                Func<double, Prova> prova = dmm =>
                {
                    SegmentGeometry gp = Copia(g); Applica(gp, e, asse, dmm / 1000, 0, 0, 0);
                    return Valuta(m, gp, o, th, ph, ct);
                };
                Func<double, Tuple<double, double>> calo = dmm =>
                {
                    Prova a = prova(dmm), b = prova(-dmm);
                    return Tuple.Create(Math.Max(nom.Fb - a.Fb, nom.Fb - b.Fb), Math.Max(a.Ros, b.Ros));
                };
                var c1 = calo(1.0);
                e.Sens = Math.Max(0, c1.Item1);
                // tolleranza ammessa: bisezione tra 0,1 e 10 mm sul calo del F/B (quota dell'elemento) e sul ROS massimo
                // ROS: non oltre l'obiettivo; se il ROS nominale lo supera gia', conta solo l'aumento (al massimo 0,05)
                double rosLim = double.IsNaN(nom.Ros) ? o.RosMax : Math.Max(o.RosMax, nom.Ros + 0.05);
                Func<Tuple<double, double>, bool> ok = c => c.Item1 <= budget && (double.IsNaN(c.Item2) || c.Item2 <= rosLim);
                double lo = 0, hi = 10;
                if (ok(calo(hi))) e.TollAmmessa = hi;
                else
                {
                    for (int it = 0; it < 6; it++) { double mid = (lo + hi) / 2; if (ok(mid == 1.0 ? c1 : calo(mid))) lo = mid; else hi = mid; }
                    e.TollAmmessa = Math.Round(lo * 10) / 10;
                }
                prog?.Report(++k);
            }
        }

        public static Esito MonteCarlo(NecModel m, SegmentGeometry g, List<Elemento> el, int asse, Opzioni o, double th, double ph, IProgress<int> prog, CancellationToken ct)
        {
            Esito r = new Esito { Elementi = el };
            r.Nominale = Valuta(m, g, o, th, ph, ct);
            Random rnd = new Random(o.Seme);
            // gli errori di tutte le prove si estraggono prima, in ordine: il risultato non dipende dal parallelismo
            double[][] err = new double[o.Prove][];
            for (int p = 0; p < o.Prove; p++)
            {
                err[p] = new double[el.Count * 4];
                for (int i = 0; i < el.Count; i++)
                {
                    err[p][4 * i] = el[i].Alimentato ? 0 : Errore(rnd, o.TolL, o.Gauss);
                    err[p][4 * i + 1] = o.Foratura ? Errore(rnd, o.TolP, o.Gauss) : 0;
                    err[p][4 * i + 2] = Errore(rnd, o.TolD, o.Gauss);
                    err[p][4 * i + 3] = o.Staffa ? Errore(rnd, o.TolP, o.Gauss) : 0;
                }
            }
            Prova[] ris = new Prova[o.Prove];
            int fatte = 0;
            for (int p = 0; p < o.Prove; p++)
            {
                ct.ThrowIfCancellationRequested();
                SegmentGeometry gp = Copia(g);
                for (int i = 0; i < el.Count; i++) Applica(gp, el[i], asse, err[p][4 * i] / 1000, err[p][4 * i + 1] / 1000, err[p][4 * i + 2] / 1000, err[p][4 * i + 3] / 1000);
                ris[p] = Valuta(m, gp, o, th, ph, ct);
                prog?.Report(++fatte);
            }
            r.Prove = ris.ToList();
            Riassumi(r);
            return r;
        }

        public static void Riassumi(Esito r)
        {
            if (r.Prove.Count > 0)
            {
                List<double> g = r.Prove.Select(p => p.G).OrderBy(x => x).ToList(), fb = r.Prove.Select(p => p.Fb).OrderBy(x => x).ToList(), ros = r.Prove.Select(p => p.Ros).Where(x => !double.IsNaN(x)).OrderBy(x => x).ToList();
                r.G5 = g[(int)(0.05 * (g.Count - 1))]; r.Fb5 = fb[(int)(0.05 * (fb.Count - 1))];
                r.Ros95 = ros.Count > 0 ? ros[(int)(0.95 * (ros.Count - 1))] : double.NaN;
            }
            r.Critici = string.Join(", ", r.Elementi.Where(e => !double.IsNaN(e.Sens)).OrderByDescending(e => e.Sens).Take(2).Select(e => e.Nome));
        }
    }
}
