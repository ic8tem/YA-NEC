// YA-NEC 1.2.0 - misure del VNA (bozzetto VHF 11): lettura dei file Touchstone .s1p (RI, MA, DB; Hz, kHz, MHz, GHz) e dei CSV
// (NanoVNA-Saver e simili: frequenza, parte reale e immaginaria di S11), de-embedding del cavo tra VNA e antenna (linea con perdite)
// e confronto con il calcolo: frequenza del ROS minimo, ROS minimo, banda sotto una soglia, Z a una frequenza, scarto in %.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

namespace YaNec.Engine
{
    public static class MisureVna
    {
        public class Misura
        {
            public string Nome = "", Formato = "";
            public double Z0 = 50;
            public double[] F = new double[0];   // MHz
            public Complex[] Z = new Complex[0]; // ohm, cosi' come misurata (al connettore del VNA)
        }

        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        public static Misura Leggi(string file)
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            return ext == ".csv" || ext == ".txt" ? LeggiCsv(File.ReadAllLines(file), Path.GetFileName(file)) : LeggiTouchstone(File.ReadAllLines(file), Path.GetFileName(file));
        }

        public static Misura LeggiTouchstone(string[] righe, string nome)
        {
            double mult = 1e9; string fmt = "MA", par = "S"; double z0 = 50;
            List<double> f = new List<double>(); List<Complex> g = new List<Complex>();
            foreach (string r0 in righe)
            {
                string r = r0; int bang = r.IndexOf('!'); if (bang >= 0) r = r.Substring(0, bang);
                r = r.Trim(); if (r.Length == 0) continue;
                if (r.StartsWith("#"))
                {
                    string[] t = r.Substring(1).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < t.Length; i++)
                    {
                        string u = t[i].ToUpperInvariant();
                        if (u == "HZ") mult = 1; else if (u == "KHZ") mult = 1e3; else if (u == "MHZ") mult = 1e6; else if (u == "GHZ") mult = 1e9;
                        else if (u == "RI" || u == "MA" || u == "DB") fmt = u;
                        else if (u == "S" || u == "Z" || u == "Y") par = u;
                        else if (u == "R" && i + 1 < t.Length) double.TryParse(t[i + 1], NumberStyles.Float, CI, out z0);
                    }
                    continue;
                }
                string[] c = r.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (c.Length < 3) continue;
                double a, b, fr;
                if (!double.TryParse(c[0], NumberStyles.Float, CI, out fr) || !double.TryParse(c[1], NumberStyles.Float, CI, out a) || !double.TryParse(c[2], NumberStyles.Float, CI, out b)) continue;
                Complex v = fmt == "RI" ? new Complex(a, b) : fmt == "MA" ? Complex.FromPolarCoordinates(a, b * Math.PI / 180) : Complex.FromPolarCoordinates(Math.Pow(10, a / 20), b * Math.PI / 180);
                f.Add(fr * mult / 1e6);
                g.Add(par == "Z" ? v * z0 : par == "Y" ? 1 / (v / z0) : z0 * (1 + v) / (1 - v));
            }
            if (f.Count < 2) throw new IOException("Il file Touchstone non contiene punti di S11.");
            return new Misura { Nome = nome, Formato = fmt + " · Z0 " + z0.ToString("0", CI) + " Ω", Z0 = z0, F = f.ToArray(), Z = g.ToArray() };
        }

        public static Misura LeggiCsv(string[] righe, string nome, double z0 = 50)
        {
            List<double> f = new List<double>(); List<Complex> z = new List<Complex>();
            foreach (string r0 in righe)
            {
                string r = r0.Trim(); if (r.Length == 0 || r.StartsWith("#")) continue;
                char sep = r.Contains(';') ? ';' : r.Contains('\t') ? '\t' : ',';
                string[] c = r.Split(sep);
                if (c.Length < 3) continue;
                double fr, a, b;
                if (!double.TryParse(c[0].Trim(), NumberStyles.Float, CI, out fr) || !double.TryParse(c[1].Trim(), NumberStyles.Float, CI, out a) || !double.TryParse(c[2].Trim(), NumberStyles.Float, CI, out b)) continue;
                Complex gm = new Complex(a, b);
                f.Add(fr > 1e5 ? fr / 1e6 : fr);   // in Hz (NanoVNA-Saver) o gia' in MHz
                z.Add(z0 * (1 + gm) / (1 - gm));
            }
            if (f.Count < 2) throw new IOException("Il CSV non contiene righe frequenza, Re(S11), Im(S11).");
            return new Misura { Nome = nome, Formato = "CSV RI · Z0 " + z0.ToString("0", CI) + " Ω", Z0 = z0, F = f.ToArray(), Z = z.ToArray() };
        }

        /// <summary>Toglie il cavo: Z all'antenna dalla Z misurata all'ingresso di una linea (Z0, VF, perdita dB/100 m, lunghezza m).</summary>
        public static Complex[] TogliCavo(double[] f, Complex[] zm, double z0, double vf, Func<double, double> perdita, double len)
        {
            Complex[] r = new Complex[zm.Length];
            for (int i = 0; i < zm.Length; i++)
            {
                Complex gl = LineCalc.Gamma(f[i], vf, perdita(f[i])) * len;
                Complex ch = Complex.Cosh(gl), sh = Complex.Sinh(gl);   // inversa di LineCalc.InputImpedance
                r[i] = z0 * (zm[i] * ch - z0 * sh) / (z0 * ch - zm[i] * sh);
            }
            return r;
        }

        public class Riassunto { public double FMin = double.NaN, RosMin = double.NaN, BandaDa = double.NaN, BandaA = double.NaN; public Complex ZaF; }

        public static Riassunto Riassumi(double[] f, Complex[] z, double z0, double soglia, double fRif)
        {
            Riassunto r = new Riassunto();
            if (f.Length == 0) return r;
            double[] ros = z.Select(q => Analysis.Swr(q, z0)).ToArray();
            int im = 0; for (int i = 1; i < ros.Length; i++) if (ros[i] < ros[im]) im = i;
            // minimo affinato con la parabola sui tre punti vicini
            r.FMin = f[im]; r.RosMin = ros[im];
            if (im > 0 && im < ros.Length - 1)
            {
                double y0 = ros[im - 1], y1 = ros[im], y2 = ros[im + 1], den = y0 - 2 * y1 + y2;
                if (den > 1e-12) { double d = 0.5 * (y0 - y2) / den; r.FMin = f[im] + d * (f[im + 1] - f[im - 1]) / 2; r.RosMin = y1 - 0.25 * (y0 - y2) * d; }
            }
            int a = im, b = im;
            while (a > 0 && ros[a - 1] < soglia) a--;
            while (b < ros.Length - 1 && ros[b + 1] < soglia) b++;
            if (ros[im] < soglia) { r.BandaDa = f[a]; r.BandaA = f[b]; }
            int k = 0; for (int i = 1; i < f.Length; i++) if (Math.Abs(f[i] - fRif) < Math.Abs(f[k] - fRif)) k = i;
            r.ZaF = z[k];
            return r;
        }
    }
}
