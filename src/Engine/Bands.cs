// YA-NEC - bande radioamatoriali da 136 kHz a 10 GHz (limiti IARU Regione 1), approvate da IC8TEM il 12/11/2026.
// Centro aritmetico = meta' dei limiti; centro operativo = frequenza dove si lavora il DX (frequenza di progetto in VHF e oltre).
using System;
using System.Collections.Generic;

namespace YaNec.Engine
{
    public class Band
    {
        public string Name;
        public double Low, High, Operating;   // MHz
        public double Arithmetic { get { return (Low + High) / 2; } }
        public bool Contains(double f) { return f >= Low - 1e-9 && f <= High + 1e-9; }
        public Band(string n, double lo, double hi, double op) { Name = n; Low = lo; High = hi; Operating = op; }

        /// <summary>Span di sweep consigliato (kHz) attorno al centro f: simmetrico e dentro la banda.</summary>
        public double SpanKHz(double f)
        {
            double half = Math.Min(f - Low, High - f);
            if (half <= 0) half = (High - Low) / 2;
            return 2 * half * 1000.0;
        }

        public override string ToString() { return Name; }
    }

    public static class Bands
    {
        public static readonly List<Band> All = new List<Band>
        {
            new Band("2200 m", 0.1357, 0.1378, 0.13675),
            new Band("630 m", 0.472, 0.479, 0.4755),
            new Band("160 m", 1.810, 2.000, 1.830),
            new Band("80 m", 3.500, 3.800, 3.650),
            new Band("60 m", 5.3515, 5.3665, 5.359),
            new Band("40 m", 7.000, 7.200, 7.100),
            new Band("30 m", 10.100, 10.150, 10.125),
            new Band("20 m", 14.000, 14.350, 14.175),
            new Band("17 m", 18.068, 18.168, 18.118),
            new Band("15 m", 21.000, 21.450, 21.225),
            new Band("12 m", 24.890, 24.990, 24.940),
            new Band("10 m", 28.000, 29.700, 28.500),
            new Band("8 m", 40.660, 40.700, 40.680),
            new Band("6 m", 50.000, 52.000, 50.150),
            new Band("4 m", 70.000, 70.500, 70.200),
            new Band("2 m", 144.000, 146.000, 144.300),
            new Band("70 cm", 430.000, 440.000, 432.200),
            new Band("23 cm", 1240.000, 1300.000, 1296.200),
            new Band("13 cm", 2300.000, 2450.000, 2320.200),
            new Band("9 cm", 3400.000, 3475.000, 3400.100),
            new Band("6 cm", 5650.000, 5850.000, 5760.100),
            new Band("3 cm", 10000.000, 10500.000, 10368.100),
        };

        public static int IndexOf(double f)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].Contains(f)) return i;
            return -1;
        }
    }
}
