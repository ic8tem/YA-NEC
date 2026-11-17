// YA-NEC 1.2.0 - Oggetti inseriti > Linee: "Linea aperta + accordatore" alla TLW (bozzetto HF 11 B approvato da IC8TEM il 17/11/2026).
// Per ogni banda: Z al punto di alimentazione (dal NEC), ROS sulla linea, perdita nella linea con perdite (equazioni del telegrafista con
// gamma = alfa + j beta, come TLW di N6BV / ARRL), Z all'accordatore, perdita nell'accordatore (tipo e Q della bobina), perdita totale.
// Accordatore (stima, fumetto giallo): la reattanza si annulla con un elemento in serie (bobina con il suo Q o condensatore quasi senza
// perdite), poi la R si porta a 50 ohm con una rete di Q caricato Qn = sqrt(Rmax/Rmin - 1); perdita relativa Qn k / Qu con k = 1 per la
// rete L e 2 per T e Pi-greco (due sezioni). Avviso quando l'accordatore vede meno di 20 ohm.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace YaNec.Engine
{
    public static class LineaAccordatore
    {
        public static readonly string[] Tipi = { "T (passa-alto)", "L", "Pi-greco" };

        public class Riga { public double F; public Complex Zant, Zacc; public double RosLinea, PerditaLinea, PerditaAcc, Totale; public bool Basso; public string Errore; }

        /// <summary>Trasforma il carico zl lungo la linea (z0, VF, attenuazione dB/m alla f) e da' la Z d'ingresso e la perdita (dB).</summary>
        public static Complex Trasforma(Complex zl, double z0, double vf, double dbPerM, double fMHz, double len, out double perditaDb)
        {
            double beta = 2 * Math.PI * fMHz * 1e6 / (299792458.0 * vf), alfa = dbPerM / 8.685889638;
            Complex g = new Complex(alfa, beta) * len;
            Complex ch = Complex.Cosh(g), sh = Complex.Sinh(g);
            Complex vl = 1, il = 1 / zl;
            Complex vi = vl * ch + il * z0 * sh, ii = vl / z0 * sh + il * ch;
            double pl = (vl * Complex.Conjugate(il)).Real, pi = (vi * Complex.Conjugate(ii)).Real;
            perditaDb = pl > 0 && pi > 0 ? 10 * Math.Log10(pi / pl) : double.NaN;
            return vi / ii;
        }

        /// <summary>Perdita (dB) dell'accordatore che porta z a r0 (vedi l'intestazione del file).</summary>
        public static double PerditaAccordatore(Complex z, double r0, int tipo, double qu)
        {
            double R = Math.Max(z.Real, 0.01), X = z.Imaginary;
            double f1 = X < 0 ? Math.Abs(X) / qu / R : Math.Abs(X) / 1000 / R;
            double qn = Math.Sqrt(Math.Max(R, r0) / Math.Min(R, r0) - 1);
            double k = tipo == 1 ? 1 : 2;
            double f2 = (qn * k + (tipo == 1 ? 0 : 1)) / qu;
            return 10 * Math.Log10((1 + f1) * (1 + f2));
        }

        public static double Ros(Complex z, double z0)
        {
            double m = ((z - z0) / (z + z0)).Magnitude;
            return m >= 0.999999 ? 999 : (1 + m) / (1 - m);
        }
    }
}
