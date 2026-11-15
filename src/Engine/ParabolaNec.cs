// YA-NEC 0.4.20 - parabola a centine come griglia di fili per il NEC-2 (scelta di IC8TEM del 14/11/2026 12:55: "centine: tutti e due",
// effetto nell'apertura e modello NEC del disco). Stesso generatore usato per le prove 5 e 7 con EZNEC (parabola_centine.py):
// parabola 1,2 m a maglia lambda/10 con segmenti fino a lambda/10: EZNEC 63,793 +j45,632 ohm, 21,72 dBi; nec2c 63,775 +j45,598, 21,72 dBi.
// Disco con il vertice nell'origine e l'asse lungo +Z, fuoco a z = F. Anelli a passo costante lungo il profilo (circa la maglia); su ogni
// anello M nodi (multiplo del numero di centine, lato <= maglia); raggi tra anelli vicini (ogni nodo esterno al nodo interno piu' vicino in
// angolo). Le centine sono i raggi agli angoli 2 pi k / N. Sfaccettata: la rete e' tesa piana tra due centine vicine (punto sulla corda tra
// le centine allo stesso raggio), come una rete vera senza sagomatura; ideale: i nodi stanno sul paraboloide.
// Raggio dei fili con la regola dell'area uguale, 2 pi a = lato della maglia (A. Rubinstein, F. Rachidi, M. Rubinstein, "On wire-grid
// representation of solid metallic surfaces", IEEE Trans. EMC 47, 2005), al massimo 1/4 del filo piu' corto. Superfici a griglia di fili
// nel NEC-2: P. Wade W1GHZ, Online Microwave Antenna Book cap. 12; A. Voors, Parab50.nec di 4nec2.
// Illuminatore: dipolo lambda/2 lungo Y nel fuoco con un riflettore 0,25 lambda dietro (verso +Z), che irradia verso il disco.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YaNec.Engine
{
    public static class ParabolaNec
    {
        const double C0 = 299.792458;
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        /// <summary>Numero di segmenti del modello senza costruirlo (per avvisare prima dei modelli enormi).</summary>
        public static int Segmenti(double fMHz, double d, double fsud, int ncent, double magliaLambda)
        {
            int n;
            Fili(fMHz, d, fsud, ncent, magliaLambda, false, out n);
            return n;
        }

        static List<double[]> Fili(double fMHz, double d, double fsud, int ncent, double magliaLambda, bool sfacc, out int nseg)
        {
            double lam = C0 / fMHz, F = fsud * d, R = d / 2, mg = magliaLambda * lam;
            ncent = Math.Max(3, ncent);
            Func<double, double> z = r => r * r / (4 * F);
            List<double> rs = new List<double> { 0.0 };
            double rr = 0;
            while (rr < R - 1e-9)
            {
                double dzdr = rr / (2 * F), dr = mg / Math.Sqrt(1 + dzdr * dzdr);
                rr = Math.Min(R, rr + dr);
                rs.Add(rr);
            }
            if (rs.Count > 2 && rs[rs.Count - 1] - rs[rs.Count - 2] < 0.3 * mg) rs.RemoveAt(rs.Count - 2);
            Func<double, int> M = r =>
            {
                if (r == 0) return 1;
                int m = ncent * Math.Max(1, (int)Math.Ceiling(2 * Math.PI * r / (ncent * mg) - 1e-9));
                while (m > 4 && m % 2 == 0 && 2 * Math.PI * r / m < mg / 2) m /= 2;
                return m;
            };
            Func<double, int, int, double[]> nodo = (r, j, m) =>
            {
                double phi = 2 * Math.PI * j / m;
                if (!sfacc || m < ncent) return new[] { r * Math.Cos(phi), r * Math.Sin(phi), z(r) };
                double dc = 2 * Math.PI / ncent;
                int k = (int)Math.Floor(phi / dc + 1e-12);
                double t = (phi - k * dc) / dc, a1 = k * dc, a2 = (k + 1) * dc;
                double x1 = r * Math.Cos(a1), y1 = r * Math.Sin(a1), x2 = r * Math.Cos(a2), y2 = r * Math.Sin(a2);
                return new[] { x1 + t * (x2 - x1), y1 + t * (y2 - y1), z(r) };
            };
            List<double[]> fili = new List<double[]>();   // x1 y1 z1 x2 y2 z2 centina(0/1)
            int[] ms = new int[rs.Count];
            for (int i = 0; i < rs.Count; i++) ms[i] = M(rs[i]);
            for (int i = 1; i < rs.Count; i++)
            {
                double r = rs[i];
                int m = ms[i], mi = ms[i - 1];
                for (int j = 0; j < m; j++)
                {
                    double[] a = nodo(r, j, m), b = nodo(r, (j + 1) % m, m);
                    fili.Add(new[] { a[0], a[1], a[2], b[0], b[1], b[2], 0 });
                }
                for (int j = 0; j < m; j++)
                {
                    double phi = 2 * Math.PI * j / m;
                    int ji = mi == 1 ? 0 : ((int)Math.Round(phi / (2 * Math.PI) * mi, MidpointRounding.ToEven)) % mi;
                    bool cent = m >= ncent && ((long)j * ncent) % m == 0;
                    double[] a = mi > 1 ? nodo(rs[i - 1], ji, mi) : new[] { 0.0, 0.0, 0.0 };
                    double[] b = nodo(r, j, m);
                    fili.Add(new[] { a[0], a[1], a[2], b[0], b[1], b[2], cent ? 1 : 0 });
                }
            }
            nseg = fili.Count + 2 * 11;
            return fili;
        }

        /// <summary>Mazzo NEC-2 della parabola a centine (spazio libero, alimentazione al centro del dipolo del feed).</summary>
        public static string Mazzo(double fMHz, double d, double fsud, int ncent, double magliaLambda, bool sfacc, out int nseg)
        {
            double lam = C0 / fMHz, F = fsud * d, mg = magliaLambda * lam;
            List<double[]> fili = Fili(fMHz, d, fsud, ncent, magliaLambda, sfacc, out nseg);
            double lmin = double.MaxValue;
            foreach (double[] w in fili)
            {
                double l = Math.Sqrt((w[3] - w[0]) * (w[3] - w[0]) + (w[4] - w[1]) * (w[4] - w[1]) + (w[5] - w[2]) * (w[5] - w[2]));
                if (l > 0) lmin = Math.Min(lmin, l);
            }
            double rr = Math.Min(mg / (2 * Math.PI), lmin / 4);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(CI, "CM YA-NEC - parabola a centine come griglia di fili: D {0:0.00} m, f/D {1:0.00}, {2} centine, {3:0.###} MHz", d, fsud, ncent, fMHz));
            sb.AppendLine(string.Format(CI, "CM maglia {0:0.###} lambda ({1:0.0} mm), rete {2}; {3} segmenti", magliaLambda, mg * 1000, sfacc ? "PIANA tra le centine (sfaccettata)" : "sul paraboloide (ideale)", nseg));
            sb.AppendLine(string.Format(CI, "CM vertice nell'origine, asse +Z, fuoco a z = {0:0.0000} m; dipolo lambda/2 lungo Y nel fuoco, riflettore 0,25 lambda dietro", F));
            sb.AppendLine(string.Format(CI, "CM raggio dei fili {0:0.00} mm (area uguale, Rubinstein 2005); centine con lo stesso raggio della rete", rr * 1000));
            sb.AppendLine("CE");
            int t = 1;
            foreach (double[] w in fili)
                sb.AppendLine(string.Format(CI, "GW {0} 1 {1:0.######} {2:0.######} {3:0.######} {4:0.######} {5:0.######} {6:0.######} {7:0.#######}", t++, w[0], w[1], w[2], w[3], w[4], w[5], rr));
            double h = 0.235 * lam, zr = F + 0.25 * lam, hr = 0.25 * lam;
            int tdip = t;
            sb.AppendLine(string.Format(CI, "GW {0} 11 0 {1:0.######} {2:0.######} 0 {3:0.######} {2:0.######} {4:0.#######}", t++, -h, F, h, lam / 300));
            sb.AppendLine(string.Format(CI, "GW {0} 11 0 {1:0.######} {2:0.######} 0 {3:0.######} {2:0.######} {4:0.#######}", t++, -hr, zr, hr, lam / 300));
            sb.AppendLine("GE 0");
            sb.AppendLine(string.Format(CI, "EX 0 {0} 6 0 1 0", tdip));
            sb.AppendLine(string.Format(CI, "FR 0 1 0 0 {0:0.###} 0", fMHz));
            sb.AppendLine("EN");
            return sb.ToString();
        }
    }
}
