// YA-NEC 1.6.0 - oggetti di EZNEC che il NEC-2 non ha come schede (bozzetto «spunti dalla ricerca su GitHub», proposta 6, approvato da
// IC8TEM il 23/11/2026 14:27): carichi di Laplace e linee con perdite. Formule e struttura del file .ez da Ez2Nec di AG7T
// (src/nec/elements.rs e src/ez/read.rs, licenza Apache-2.0): rapporto di due polinomi in s = j omega con il termine s^0 per primo;
// linea con sole perdite nei conduttori (R serie = 2 alfa Z0, nessuna G), equazioni esatte della linea. Nessun codice copiato: le
// formule sono riscritte in C#. Nel calcolo diventano un carico LD 4 e una rete NT ricalcolati a ogni frequenza; nel file .nec restano
// come righe "CM YA-NEC LAPLACE" e "CM YA-NEC LINEAP" (gli altri programmi non le leggono).
using System;
using System.Globalization;
using System.Numerics;

namespace YaNec.Engine
{
    /// <summary>Carico di Laplace di EZNEC su un segmento: Z(s) = (a0 + a1 s + ... + a5 s^5) / (b0 + b1 s + ... + b5 s^5), s = j 2 pi f.</summary>
    public class CaricoLaplace
    {
        public int Tag, Seg;
        public double[] Num = new double[6], Den = new double[6];
        public CaricoLaplace Clone() { CaricoLaplace c = (CaricoLaplace)MemberwiseClone(); c.Num = (double[])Num.Clone(); c.Den = (double[])Den.Clone(); return c; }

        public Complex Z(double fMHz)
        {
            Complex s = new Complex(0, 2 * Math.PI * fMHz * 1e6);
            Complex n = Complex.Zero, d = Complex.Zero;
            for (int k = 5; k >= 0; k--) { n = n * s + Num[k]; d = d * s + Den[k]; }
            return Complex.Abs(d) < 1e-300 ? new Complex(1e9, 0) : n / d;
        }

        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        public static string Riga(CaricoLaplace c)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("YA-NEC LAPLACE " + c.Tag + " " + c.Seg);
            foreach (double v in c.Num) sb.Append(' ').Append(v.ToString("R", CI));
            foreach (double v in c.Den) sb.Append(' ').Append(v.ToString("R", CI));
            return sb.ToString();
        }

        public static CaricoLaplace Leggi(string t)
        {
            string[] f = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (f.Length < 14) return null;
            CaricoLaplace c = new CaricoLaplace { Tag = int.Parse(f[0], CI), Seg = int.Parse(f[1], CI) };
            for (int k = 0; k < 6; k++) { c.Num[k] = double.Parse(f[2 + k], CI); c.Den[k] = double.Parse(f[8 + k], CI); }
            return c;
        }
    }

    /// <summary>Linea di trasmissione con perdite tra due segmenti (EZNEC): rete NT con Y11 = Y22 = 1 / (Zc tanh(gamma l)),
    /// Y12 = -1 / (Zc sinh(gamma l)); Z0 negativa = linea incrociata (Y12 cambia segno).</summary>
    public class LineaPerdite
    {
        public int Tag1, Seg1, Tag2, Seg2;
        public double Z0 = 50, LenM, Vf = 1, DbPerM;
        public LineaPerdite Clone() { return (LineaPerdite)MemberwiseClone(); }

        /// <summary>Costante di propagazione (1/m) e impedenza caratteristica con le sole perdite nei conduttori (come EZNEC).</summary>
        public static void Costanti(double z0, double vf, double dbPerM, double fMHz, out Complex gamma, out Complex zc)
        {
            double w = 2 * Math.PI * fMHz * 1e6, v = 299.792458e6 * Math.Max(0.01, vf);
            double alfa = dbPerM / (20.0 / Math.Log(10));
            Complex serie = new Complex(2 * alfa * z0, w * z0 / v), parallelo = new Complex(0, w / (z0 * v));
            gamma = Complex.Sqrt(serie * parallelo);
            zc = Complex.Sqrt(serie / parallelo);
        }

        public NetworkSpec Nt(double fMHz)
        {
            Complex g, zc;
            Costanti(Math.Abs(Z0), Vf, DbPerM, fMHz, out g, out zc);
            Complex gl = g * Math.Max(1e-6, LenM);
            Complex y11 = Complex.Cosh(gl) / (zc * Complex.Sinh(gl)), y12 = -1 / (zc * Complex.Sinh(gl));
            if (Z0 < 0) y12 = -y12;
            return new NetworkSpec { IsTl = false, Tag1 = Tag1, Seg1 = Seg1, Tag2 = Tag2, Seg2 = Seg2, F1 = y11.Real, F2 = y11.Imaginary, F3 = y12.Real, F4 = y12.Imaginary, F5 = y11.Real, F6 = y11.Imaginary };
        }

        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        public static string Riga(LineaPerdite l)
        {
            return string.Format(CI, "YA-NEC LINEAP {0} {1} {2} {3} {4:R} {5:R} {6:R} {7:R}", l.Tag1, l.Seg1, l.Tag2, l.Seg2, l.Z0, l.LenM, l.Vf, l.DbPerM);
        }

        public static LineaPerdite Leggi(string t)
        {
            string[] f = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (f.Length < 8) return null;
            return new LineaPerdite { Tag1 = int.Parse(f[0], CI), Seg1 = int.Parse(f[1], CI), Tag2 = int.Parse(f[2], CI), Seg2 = int.Parse(f[3], CI),
                Z0 = double.Parse(f[4], CI), LenM = double.Parse(f[5], CI), Vf = double.Parse(f[6], CI), DbPerM = double.Parse(f[7], CI) };
        }
    }
}
