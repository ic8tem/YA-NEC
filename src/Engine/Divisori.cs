// YA-NEC 1.2.0 - divisori di potenza e cavi di fase (bozzetto VHF 5).
// Trasformatore lambda/4: Z0 = radice(Zin Zcarico), Zcarico = Zantenna / N. Linea in tubo quadro con conduttore tondo:
// Z0 = 138 log10(1,0787 D/d) / radice(er) (forma approssimata per la linea quadra, ARRL / DL6WU); in tubo tondo Z0 = 138 log10(D/d) / radice(er).
// Wilkinson a N vie: rami lambda/4 da Z0 radice(N) e resistenze Z0 a stella. Cavi di fase: multipli di lambda/2 (l'impedenza si ripete).
using System;
using System.Collections.Generic;
using System.Numerics;

namespace YaNec.Engine
{
    public static class Divisori
    {
        public const double C = 299792.458;   // mm * MHz
        public static readonly string[] Connettori = { "N, femmina", "BNC", "SMA", "senza connettori" };
        public static readonly double[] AccorciamentoMm = { 4.0, 3.0, 1.5, 0 };   // per estremo (stima: parte della linea dentro il connettore)

        public static double Z0Richiesta(double zin, double zant, int n) { return Math.Sqrt(zin * zant / Math.Max(1, n)); }
        public static double DQuadro(double z0, double latoInterno, double er) { return 1.0787 * latoInterno / Math.Pow(10, z0 * Math.Sqrt(er) / 138); }
        public static double DTondo(double z0, double dInterno, double er) { return dInterno / Math.Pow(10, z0 * Math.Sqrt(er) / 138); }
        public static double Z0Quadro(double lato, double d, double er) { return 138 * Math.Log10(1.0787 * lato / d) / Math.Sqrt(er); }
        public static double QuartoMm(double fMHz, double vf) { return C / fMHz / 4 * vf; }

        /// <summary>Impedenza all'ingresso del trasformatore lambda/4 (progettato a f0) chiuso su zl, alla frequenza f; linea senza perdite.</summary>
        public static Complex Zin(Complex zl, double z0, double f0, double f)
        {
            double bl = Math.PI / 2 * f / f0;
            Complex t = new Complex(0, Math.Tan(bl));
            return z0 * (zl + z0 * t) / (z0 + zl * t);
        }

        /// <summary>Perdita (dB) di una linea coassiale d'alluminio in aria: conduttori con resistenza superficiale Rs, lunghezza in mm.</summary>
        public static double PerditaAl(double fMHz, double z0, double dEstEq, double dInt, double lenMm)
        {
            double rs = Math.Sqrt(Math.PI * fMHz * 1e6 * 4e-7 * Math.PI / 3.5e7);
            double a = rs / (2 * z0) * (1 / (Math.PI * dInt / 1000) + 1 / (Math.PI * dEstEq / 1000));   // Np/m
            return 8.686 * a * lenMm / 1000;
        }

        public class CavoFase { public string Uscita = ""; public int Mezze; public double FisicaMm, PerditaDb, ExtraMm; }

        /// <summary>Cavi di fase: il multiplo di lambda/2 piu' piccolo che supera la lunghezza minima; extra = allungamento per il tilt (mm).</summary>
        public static CavoFase Fase(double fMHz, double vf, double lossDb100, double minimaMm, double extraMm)
        {
            double mezza = C / fMHz / 2 * vf;
            int n = Math.Max(1, (int)Math.Ceiling(minimaMm / mezza));
            double l = n * mezza + extraMm;
            return new CavoFase { Mezze = n, FisicaMm = l, PerditaDb = lossDb100 * l / 100000, ExtraMm = extraMm };
        }

        /// <summary>Allungamento (mm di cavo) di una riga dello stack per inclinare il lobo di tilt gradi: fase = 360 dv sin(tilt) / lambda.</summary>
        public static double ExtraTilt(double fMHz, double vf, double dvM, double tiltDeg) { return dvM * 1000 * Math.Sin(tiltDeg * Math.PI / 180) * vf; }

        /// <summary>Banda (MHz) in cui il ROS all'ingresso resta sotto la soglia, con il carico zl (costante) e la Z di sistema zs.</summary>
        public static void Banda(Complex zl, double z0, double zs, double f0, double soglia, out double fa, out double fb)
        {
            fa = fb = double.NaN;
            for (double f = f0; f > f0 * 0.5; f -= f0 * 0.0005) { if (Analysis.Swr(Zin(zl, z0, f0, f), zs) >= soglia) break; fa = f; }
            for (double f = f0; f < f0 * 1.5; f += f0 * 0.0005) { if (Analysis.Swr(Zin(zl, z0, f0, f), zs) >= soglia) break; fb = f; }
        }
    }
}
