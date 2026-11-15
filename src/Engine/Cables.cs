// YA-NEC - abaco dei cavi (file data/cavi.csv) e calcolo delle linee di accordo:
// trasformatore a lambda/4 (anche con piu' cavi in parallelo, come l'adattamento DK7ZB delle Yagi a 28 ohm),
// impedenza vista all'ingresso di una linea con perdite, stub aperti e in corto.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace YaNec.Engine
{
    public class Cable
    {
        public static readonly double[] RefMHz = { 50.15, 144.3, 432.2, 1296.2 };
        public string Name = "", Dielectric = "", Source = "";
        public double Z0 = 50, Vf = 0.66, OdMm;
        public double[] AttDb100m = new double[4];   // perdita dB/100 m alle frequenze RefMHz

        /// <summary>Perdita in dB/100 m alla frequenza f, interpolata in scala logaritmica tra i valori dell'abaco.</summary>
        public double AttenuationAt(double fMHz)
        {
            if (fMHz <= 0) return 0;
            int a, b;
            if (fMHz <= RefMHz[0]) { a = 0; b = 1; }
            else if (fMHz >= RefMHz[3]) { a = 2; b = 3; }
            else { a = 0; while (a < 2 && fMHz > RefMHz[a + 1]) a++; b = a + 1; }
            double ya = AttDb100m[a], yb = AttDb100m[b];
            if (ya <= 0 || yb <= 0) return Math.Max(0, ya + (yb - ya) * (fMHz - RefMHz[a]) / (RefMHz[b] - RefMHz[a]));
            double s = Math.Log(yb / ya) / Math.Log(RefMHz[b] / RefMHz[a]);
            return ya * Math.Pow(fMHz / RefMHz[a], s);
        }

        public override string ToString() { return Name; }
    }

    public static class Cables
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        /// <summary>Legge data/cavi.csv: nome;Z0;VF;dB/100m a 50;144;432;1296 MHz;diametro mm;dielettrico;fonte.</summary>
        public static List<Cable> Load(string path)
        {
            List<Cable> l = new List<Cable>();
            if (!File.Exists(path)) return l;
            foreach (string raw in File.ReadAllLines(path))
            {
                string s = raw.Trim();
                if (s.Length == 0 || s.StartsWith("#")) continue;
                string[] f = s.Split(';');
                if (f.Length < 7) continue;
                try
                {
                    Cable c = new Cable { Name = f[0].Trim() };
                    c.Z0 = double.Parse(f[1], CI);
                    c.Vf = double.Parse(f[2], CI);
                    for (int k = 0; k < 4; k++) c.AttDb100m[k] = double.Parse(f[3 + k], CI);
                    if (f.Length > 7) double.TryParse(f[7], NumberStyles.Float, CI, out c.OdMm);
                    if (f.Length > 8) c.Dielectric = f[8].Trim();
                    if (f.Length > 9) c.Source = f[9].Trim();
                    l.Add(c);
                }
                catch (FormatException) { }
            }
            return l;
        }
    }

    public static class LineCalc
    {
        public const double C0 = 299.792458;   // velocita' della luce in m/us (lambda in m = C0 / f MHz)

        public static double Wavelength(double fMHz, double vf) { return C0 / fMHz * vf; }

        /// <summary>Costante di propagazione gamma = alfa + j beta (1/m) di una linea.</summary>
        public static Complex Gamma(double fMHz, double vf, double lossDb100m)
        {
            double alpha = lossDb100m / 100.0 / (20.0 * Math.Log10(Math.E));   // Np/m
            double beta = 2 * Math.PI / Wavelength(fMHz, vf);
            return new Complex(alpha, beta);
        }

        /// <summary>Impedenza all'ingresso di una linea lunga len (m) chiusa su zl.</summary>
        public static Complex InputImpedance(Complex zl, double z0, double fMHz, double vf, double lossDb100m, double len)
        {
            // forma con cosh e sinh: resta valida anche a lambda/4 esatto di una linea senza perdite,
            // dove tanh(gamma*l) e' infinito e la forma con la tangente da' NaN
            Complex gl = Gamma(fMHz, vf, lossDb100m) * len;
            Complex ch = Complex.Cosh(gl), sh = Complex.Sinh(gl);
            return z0 * (zl * ch + z0 * sh) / (z0 * ch + zl * sh);
        }

        /// <summary>Stub: linea aperta (open = true) o in corto all'estremo lontano.</summary>
        public static Complex StubImpedance(bool open, double z0, double fMHz, double vf, double lossDb100m, double len)
        {
            Complex gl = Gamma(fMHz, vf, lossDb100m) * len;
            Complex ch = Complex.Cosh(gl), sh = Complex.Sinh(gl);
            return open ? z0 * ch / sh : z0 * sh / ch;
        }

        /// <summary>Potenza che arriva al carico rispetto a quella entrata nella linea (0..1), con il disadattamento.</summary>
        public static double PowerEfficiency(Complex zl, double z0, double fMHz, double vf, double lossDb100m, double len)
        {
            Complex g = Gamma(fMHz, vf, lossDb100m);
            Complex rho = (zl - z0) / (zl + z0);
            double al = g.Real * len;
            double r2 = rho.Magnitude * rho.Magnitude;
            double num = 1 - r2;
            double den = Math.Exp(2 * al) - r2 * Math.Exp(-2 * al);
            return den <= 0 ? 0 : num / den;
        }

        public static double Swr(Complex z, double z0)
        {
            double r = ((z - z0) / (z + z0)).Magnitude;
            return r >= 1 ? double.PositiveInfinity : (1 + r) / (1 - r);
        }
    }

    /// <summary>Trasformatore a lambda/4 fatto con n cavi uguali in parallelo (n = 2 con 75 ohm: adattamento DK7ZB per Yagi a 28 ohm).</summary>
    public class QuarterWaveMatch
    {
        public Cable Cable;
        public int Parallel = 1;          // cavi in parallelo
        public int OddMultiple = 1;       // 1 = lambda/4, 3 = 3/4 lambda, ...
        public double Z0Eq { get { return Cable.Z0 / Math.Max(1, Parallel); } }

        public double PhysicalLength(double fDesignMHz) { return LineCalc.Wavelength(fDesignMHz, Cable.Vf) / 4.0 * OddMultiple; }

        /// <summary>Impedenza vista dal lato del cavo di discesa alla frequenza f, con la linea tagliata per fDesign.</summary>
        public Complex InputImpedance(Complex zAnt, double fMHz, double fDesignMHz)
        {
            return LineCalc.InputImpedance(zAnt, Z0Eq, fMHz, Cable.Vf, Cable.AttenuationAt(fMHz), PhysicalLength(fDesignMHz));
        }

        public double LossDb(Complex zAnt, double fMHz, double fDesignMHz)
        {
            double eff = LineCalc.PowerEfficiency(zAnt, Z0Eq, fMHz, Cable.Vf, Cable.AttenuationAt(fMHz), PhysicalLength(fDesignMHz));
            return eff > 0 ? -10 * Math.Log10(eff) : double.PositiveInfinity;
        }

        /// <summary>Z0 ideale del trasformatore per portare zAnt (parte reale) a zSys.</summary>
        public static double IdealZ0(double rAnt, double zSys) { return Math.Sqrt(rAnt * zSys); }

        /// <summary>Le combinazioni dell'abaco (n cavi uguali in parallelo, n = 1..4) ordinate per ROS sul sistema.</summary>
        public static List<KeyValuePair<QuarterWaveMatch, double>> Suggest(IEnumerable<Cable> cables, Complex zAnt, double zSys, double fMHz)
        {
            List<KeyValuePair<QuarterWaveMatch, double>> r = new List<KeyValuePair<QuarterWaveMatch, double>>();
            foreach (Cable c in cables)
                for (int n = 1; n <= 4; n++)
                {
                    QuarterWaveMatch m = new QuarterWaveMatch { Cable = c, Parallel = n };
                    double swr = LineCalc.Swr(m.InputImpedance(zAnt, fMHz, fMHz), zSys);
                    r.Add(new KeyValuePair<QuarterWaveMatch, double>(m, swr));
                }
            r.Sort((a, b) => a.Value.CompareTo(b.Value));
            return r;
        }
    }
}
