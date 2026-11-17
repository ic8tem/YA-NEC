// YA-NEC 1.2.0 - opzione "giunzioni come il NEC-4" (proposta approvata da IC8TEM il 17/11/2026 21:59-22:01, spenta di partenza).
// Rifatta dal metodo descritto nel manuale di teoria del NEC-4 (G. J. Burke, LLNL UCRL-MA-109338, 1992, par. 3.4, eq. 3-42 e 3-43,
// e manuale d'uso NEC-4.2, cap. 7 e scheda JN), senza codice del NEC-4 (che non e' pubblico):
//  1) la carica di ogni filo a una giunzione non viene piu' dalla regola del NEC-2 (proporzionale a 1/(ln(2/ka) - 0,5772), Wu-King /
//     Schelkunoff) ma da un piccolo calcolo dei momenti per la giunzione: i fili sono prolungati all'infinito lontano dalla giunzione, la
//     carica su ognuno e' un mezzo triangolo lungo il primo segmento piu' una costante fino all'infinito (n = 2), e si impone che il
//     potenziale scalare sia lo stesso in tutti i punti di controllo (meta' e fine del primo segmento di ogni filo). La carica alla
//     giunzione (q1 + q2) x delta del filo prende il posto di aj nelle funzioni di base (contano solo i rapporti tra i fili);
//  2) nucleo sottile con la corrente sulla superficie del segmento sorgente e la condizione al contorno sull'asse del segmento di
//     osservazione (il contrario del NEC-2): nel campo tra due segmenti il raggio e' quello della sorgente.
// I cappucci alle estremita' dei fili (par. 4.4) non ci sono ancora: contano con segmenti lunghi meno di 2 volte il raggio.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace YaNec.Engine
{
    public sealed partial class Nec2Engine
    {
        /// <summary>1.2.0: giunzioni e nucleo come il NEC-4 (vedi l'intestazione di Nec2Engine.Giunzioni.cs).</summary>
        public bool GiunzioniNec4;
        Dictionary<int, double> qGiunz;      // chiave: segmento (0-based) * 2 + (0 estremo 1, 1 estremo 2) -> carica alla giunzione
        public int GiunzioniCalcolate { get; private set; }

        double AjLog(int s) { return 1.0 / (Math.Log(1.0 / (PI * bi[s])) - .577215664); }

        /// <summary>Fattore di carica del segmento s all'estremo piu' vicino al punto (px, py, pz), in lunghezze d'onda.</summary>
        double AjAt(int s, double px, double py, double pz)
        {
            if (qGiunz == null) return AjLog(s);
            double h = si[s] / 2;
            double d1 = Sq(x[s] - h * cab[s] - px) + Sq(y[s] - h * sab[s] - py) + Sq(z[s] - h * salp[s] - pz);
            double d2 = Sq(x[s] + h * cab[s] - px) + Sq(y[s] + h * sab[s] - py) + Sq(z[s] + h * salp[s] - pz);
            double q;
            if (qGiunz.TryGetValue(s * 2 + (d2 < d1 ? 1 : 0), out q)) return q;
            return AjLog(s);
        }
        static double Sq(double v) { return v * v; }
        void EstremoSeg(int s, int end, out double px, out double py, out double pz)
        {
            double h = end * si[s] / 2;
            px = x[s] + h * cab[s]; py = y[s] + h * sab[s]; pz = z[s] + h * salp[s];
        }

        /// <summary>Carica alle giunzioni con il calcolo dei momenti del NEC-4 (eq. 3-42/3-43, n = 2), per tutte le giunzioni di 2 o piu'
        /// segmenti che non siano due segmenti uguali in fila; le giunzioni con il terreno o con le superfici restano con la regola del NEC-2.</summary>
        void PreparaGiunzioniNec4()
        {
            qGiunz = null; GiunzioniCalcolate = 0;
            if (!GiunzioniNec4) return;
            Dictionary<string, List<int[]>> gruppi = new Dictionary<string, List<int[]>>();   // punto -> (segmento, estremo -1/+1)
            double tol = 1e-6;
            for (int s = 0; s < n; s++)
                for (int e = -1; e <= 1; e += 2)
                {
                    int ic = e < 0 ? icon1[s] : icon2[s];
                    if (ic == 0) continue;
                    double px, py, pz; EstremoSeg(s, e, out px, out py, out pz);
                    string k = Math.Round(px / tol).ToString() + "|" + Math.Round(py / tol) + "|" + Math.Round(pz / tol);
                    List<int[]> l; if (!gruppi.TryGetValue(k, out l)) { l = new List<int[]>(); gruppi[k] = l; }
                    l.Add(new[] { s, e, ic });
                }
            Dictionary<int, double> q = new Dictionary<int, double>();
            foreach (List<int[]> g in gruppi.Values)
            {
                if (g.Count < 2) continue;
                bool salta = false;
                foreach (int[] m in g) if (m[2] == m[0] + 1 || Math.Abs(m[2]) > PCHCON) salta = true;   // terreno o superficie
                if (salta) continue;
                if (g.Count == 2)
                {
                    int a = g[0][0], b = g[1][0];
                    double dot = cab[a] * cab[b] + sab[a] * sab[b] + salp[a] * salp[b];
                    if (Math.Abs(bi[a] / bi[b] - 1) < 1e-6 && Math.Abs(Math.Abs(dot) - 1) < 1e-9) continue;   // due segmenti uguali in fila: cariche uguali
                }
                double[] qq = CaricaGiunzione(g);
                if (qq == null) continue;
                // scala vicina a quella del NEC-2 (contano solo i rapporti)
                double ml = 0, mq = 0;
                for (int i = 0; i < g.Count; i++) { ml += AjLog(g[i][0]); mq += qq[i]; }
                double k = Math.Abs(mq) > 1e-30 ? ml / mq : 1;
                for (int i = 0; i < g.Count; i++) q[g[i][0] * 2 + (g[i][1] > 0 ? 1 : 0)] = qq[i] * k;
                GiunzioniCalcolate++;
            }
            qGiunz = q.Count > 0 ? q : null;
        }

        /// <summary>Soluzione dell'equazione 3-43 per una giunzione: carica di ogni filo alla giunzione (parte reale), o null.</summary>
        double[] CaricaGiunzione(List<int[]> g)
        {
            int m = g.Count;
            double[][] u = new double[m][]; double[] a = new double[m], dl = new double[m];
            for (int i = 0; i < m; i++)
            {
                int s = g[i][0], e = g[i][1];
                // direzione che si allontana dalla giunzione: se la giunzione e' all'estremo 1 il filo va verso +dir
                double sg = e < 0 ? 1 : -1;
                u[i] = new[] { sg * cab[s], sg * sab[s], sg * salp[s] };
                a[i] = bi[s]; dl[i] = si[s];
            }
            int nn = 2 * m;
            Complex[,] A = new Complex[nn, nn];
            Complex[] rhs = new Complex[nn];
            for (int i = 0; i < m; i++)
                for (int kp = 0; kp < 2; kp++)
                {
                    double sobs = kp == 0 ? dl[i] / 2 : dl[i];
                    int row = 2 * i + kp;
                    rhs[row] = Complex.One;
                    for (int l = 0; l < m; l++)
                    {
                        double c = u[i][0] * u[l][0] + u[i][1] * u[l][1] + u[i][2] * u[l][2];
                        A[row, 2 * l] = Potenziale(sobs, c, a[l], dl[l], true);
                        A[row, 2 * l + 1] = Potenziale(sobs, c, a[l], dl[l], false);
                    }
                }
            Complex[] sol = GaussC(A, rhs);
            if (sol == null) return null;
            double[] r = new double[m];
            for (int l = 0; l < m; l++) r[l] = ((sol[2 * l] + sol[2 * l + 1]) * dl[l]).Real;
            return r;
        }

        /// <summary>Potenziale (senza costanti) nel punto a distanza sobs dalla giunzione sull'asse di un filo, dovuto alla carica sul filo l
        /// (raggio al, primo segmento dl) con coseno c tra i due fili: triangolo (dl - s') su [0, dl] oppure costante dl su [0, infinito).</summary>
        static Complex Potenziale(double sobs, double c, double al, double dl, bool triangolo)
        {
            const double K = 2 * Math.PI;
            double s0 = Math.Max(0, sobs * c);
            double w = Math.Sqrt(Math.Max(0, sobs * sobs * (1 - c * c)) + al * al);
            Func<double, Complex> h = sp =>
            {
                double r2 = sobs * sobs + sp * sp - 2 * sobs * sp * c + al * al;
                double r = Math.Sqrt(Math.Max(r2, al * al));
                Complex gr = new Complex(Math.Cos(K * r), -Math.Sin(K * r)) / r;
                return (triangolo ? (dl - sp) : dl) * gr;
            };
            if (triangolo) return IntegraPicco(h, 0, dl, s0, w);
            double L = 3.0;   // lunghezze d'onda; oltre, coda con l'esponenziale integrale
            Complex v = IntegraPicco(h, 0, L, s0, w);
            // coda: R circa s' - sobs c, integrale di exp(-jkR)/R = E1(j k (L - sobs c))
            v += dl * E1Immaginario(K * (L - sobs * c));
            return v;
        }

        /// <summary>Integrale di h su [p, q] con il picco di larghezza w in s0: sostituzione s = s0 + w sinh(t) e Gauss-Legendre.</summary>
        static Complex IntegraPicco(Func<double, Complex> h, double p, double q, double s0, double w)
        {
            s0 = Math.Min(Math.Max(s0, p), q);
            Complex tot = Complex.Zero;
            foreach (double[] pz in new[] { new[] { p, s0 }, new[] { s0, q } })
            {
                if (pz[1] - pz[0] <= 1e-15) continue;
                double t0 = Asinh((pz[0] - s0) / w), t1 = Asinh((pz[1] - s0) / w);
                int npz = 8;
                for (int k = 0; k < npz; k++)
                {
                    double ta = t0 + (t1 - t0) * k / npz, tb = t0 + (t1 - t0) * (k + 1) / npz, tm = (ta + tb) / 2, tr = (tb - ta) / 2;
                    for (int j = 0; j < GLx.Length; j++)
                    {
                        double t = tm + tr * GLx[j];
                        double sp = s0 + w * Math.Sinh(t);
                        tot += h(sp) * (w * Math.Cosh(t) * GLw[j] * tr);
                    }
                }
            }
            return tot;
        }
        static double Asinh(double v) { return Math.Log(v + Math.Sqrt(v * v + 1)); }
        static readonly double[] GLx = { -0.9739065285171717, -0.8650633666889845, -0.6794095682990244, -0.4333953941292472, -0.1488743389816312, 0.1488743389816312, 0.4333953941292472, 0.6794095682990244, 0.8650633666889845, 0.9739065285171717 };
        static readonly double[] GLw = { 0.0666713443086881, 0.1494513491505806, 0.2190863625159820, 0.2692667193099963, 0.2955242247147529, 0.2955242247147529, 0.2692667193099963, 0.2190863625159820, 0.1494513491505806, 0.0666713443086881 };

        /// <summary>E1(j x) = -Ci(x) - j (pi/2 - Si(x)), x > 0 (serie, sufficiente per x fino a circa 25).</summary>
        static Complex E1Immaginario(double xx)
        {
            double si = 0, ci = 0, term = xx, x2 = xx * xx;
            // Si = sum (-1)^n x^(2n+1) / ((2n+1)(2n+1)!), Ci = gamma + ln x + sum (-1)^n x^(2n) / (2n (2n)!)
            double ts = xx, tc = 1;
            for (int nn = 0; nn < 60; nn++)
            {
                si += ts / (2 * nn + 1);
                ts *= -x2 / ((2 * nn + 2) * (2 * nn + 3));
                if (nn > 0) ci += tc / (2 * nn);
                tc *= -x2 / ((2 * nn + 1) * (2 * nn + 2));
            }
            ci += 0.5772156649015329 + Math.Log(xx);
            return new Complex(-ci, -(Math.PI / 2 - si));
        }

        static Complex[] GaussC(Complex[,] A, Complex[] b)
        {
            int nn = b.Length;
            Complex[,] M = (Complex[,])A.Clone(); Complex[] v = (Complex[])b.Clone();
            for (int c = 0; c < nn; c++)
            {
                int p = c; for (int r = c + 1; r < nn; r++) if (M[r, c].Magnitude > M[p, c].Magnitude) p = r;
                if (M[p, c].Magnitude < 1e-300) return null;
                if (p != c) { for (int k = 0; k < nn; k++) { Complex t = M[c, k]; M[c, k] = M[p, k]; M[p, k] = t; } Complex tv = v[c]; v[c] = v[p]; v[p] = tv; }
                for (int r = c + 1; r < nn; r++)
                {
                    Complex f = M[r, c] / M[c, c];
                    if (f == Complex.Zero) continue;
                    for (int k = c; k < nn; k++) M[r, k] -= f * M[c, k];
                    v[r] -= f * v[c];
                }
            }
            Complex[] xs = new Complex[nn];
            for (int r = nn - 1; r >= 0; r--)
            {
                Complex s = v[r];
                for (int k = r + 1; k < nn; k++) s -= M[r, k] * xs[k];
                xs[r] = s / M[r, r];
            }
            return xs;
        }
    }
}
