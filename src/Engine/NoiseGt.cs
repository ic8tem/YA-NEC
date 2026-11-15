// YA-NEC - RDF e G/T dal diagramma 3D calcolato dal motore.
// RDF (convenzione approvata il 12/11/2026, come EZNEC e IV3PRK): guadagno medio = potenza dell'emisfero superiore / 4 pi
// (con il terreno), oppure media su tutta la sfera in spazio libero; RDF = guadagno nella direzione voluta - guadagno medio (dB).
// G/T (come TANT di YT1NT/VE3EA e XLGTa di AC6LA): l'antenna in spazio libero viene puntata all'elevazione scelta; ogni
// direzione vede il cielo (sopra l'orizzonte) o il terreno (sotto); Tpattern = somma di T G dOmega / somma di G dOmega
// (correzione KF2YN: si divide per il guadagno medio vero del modello) oppure / 4 pi senza correzione.
// Tloss = 290 (1 - 10^(-L/10)); Tant = Tpattern + Tloss; Tpreampli = 290 (10^(NF/10) - 1); G/T = G - 10 log10(T).
using System;
using System.Collections.Generic;

namespace YaNec.Engine
{
    public class RdfResult
    {
        public double GainDbi, AvgGainDb, Rdf, Theta, Phi;
        public double BackAvgDb, Dmf, Gain20Dbi;   // DMF di ON4UN: guadagno a 20 gradi avanti meno il guadagno medio della semisfera posteriore (non confrontabile con l'RDF)
        public bool UpperHemisphere;
    }

    public class GtResult
    {
        public double GainDbi, Tpattern, Tloss, Tant, Tpre, Tsys, GtAnt, GtSys, AvgGain, EfficienzaPct = 100;
        public double SkyFraction, GroundFraction, SkyK, GroundK;
        public double[] CurveElev, CurveTant;
    }

    public static class NoiseGt
    {
        static double CellWeight(PatternGrid g, int i, double thMaxDeg)
        {
            double step = g.ThetaStep * Math.PI / 180.0, dph = g.PhiStep * Math.PI / 180.0;
            double th = g.ThetaAt(i) * Math.PI / 180.0, thMax = thMaxDeg * Math.PI / 180.0;
            double a0 = Math.Max(0, th - step / 2), b0 = Math.Min(thMax, th + step / 2);
            return (Math.Cos(a0) - Math.Cos(b0)) * dph;
        }

        static double Lin(double db) { return db < -900 ? 0 : Math.Pow(10, db / 10); }

        public static RdfResult Rdf(PatternGrid g, double thetaFwd, double phiFwd, double gainFwdDbi, bool hasGround)
        {
            double thMax = hasGround ? 90 : 180, sum = 0;
            for (int i = 0; i < g.NTheta; i++)
            {
                double w = CellWeight(g, i, thMax);
                for (int j = 0; j < g.NPhi; j++) sum += Lin(g.Total[i, j]) * w;
            }
            double avg = sum / (4 * Math.PI);
            RdfResult r = new RdfResult { GainDbi = gainFwdDbi, Theta = thetaFwd, Phi = phiFwd, UpperHemisphere = hasGround };
            r.AvgGainDb = avg > 0 ? 10 * Math.Log10(avg) : -99;
            r.Rdf = gainFwdDbi - r.AvgGainDb;
            // DMF (ON4UN, Low-Band DXing cap. 7): media del guadagno sulla meta' posteriore (azimut oltre 90 gradi dalla direzione avanti),
            // sopra il terreno; con lo spazio libero si usa lo stesso emisfero superiore
            double sb = 0, wb = 0;
            for (int i = 0; i < g.NTheta; i++)
            {
                if (g.ThetaAt(i) > 90 + 1e-9) continue;
                double w = CellWeight(g, i, 90);
                for (int j = 0; j < g.NPhi; j++)
                {
                    double d = Math.Abs(((g.PhiAt(j) - phiFwd) % 360 + 540) % 360 - 180);
                    if (d < 90) continue;
                    sb += Lin(g.Total[i, j]) * w; wb += w;
                }
            }
            r.BackAvgDb = sb > 0 && wb > 0 ? 10 * Math.Log10(sb / wb) : -99;
            // ON4UN: guadagno a 20 gradi di elevazione nella direzione avanti (theta = 70), dalla griglia
            int it20 = Math.Max(0, Math.Min(g.NTheta - 1, (int)Math.Round(70.0 / g.ThetaStep)));
            int jf = ((int)Math.Round(phiFwd / g.PhiStep) % g.NPhi + g.NPhi) % g.NPhi;
            r.Gain20Dbi = g.Total[it20, jf];
            r.Dmf = r.Gain20Dbi - r.BackAvgDb;
            return r;
        }

        /// <summary>Il diagramma deve coprire tutta la sfera (modello in spazio libero).</summary>
        public static GtResult Gt(PatternGrid g, double thetaFwd, double phiFwd, double gainDbi, double elevDeg,
                                  double tsky, double tground, double lossDb, double nfDb, bool kf2yn, double efficienzaPct = 100)
        {
            GtResult r = new GtResult { GainDbi = gainDbi };
            r.Tpattern = Tpattern(g, thetaFwd, phiFwd, elevDeg, tsky, tground, kf2yn, out r.SkyFraction, out r.GroundFraction, out r.AvgGain);
            r.SkyK = r.SkyFraction * tsky;
            r.GroundK = r.GroundFraction * tground;
            // perdite: quelle del cavo (lossDb) e, dalla 0.4.2, quelle dei conduttori dell'antenna (efficienza del NEC), come TANT,
            // VE7BQH e YU7EF: Tloss = 290 (1 - eta 10^(-L/10)). Trovato con la EF0213 di YU7EF (Ta 223,6 K con l'alluminio).
            double eta = Math.Max(0, Math.Min(1, efficienzaPct / 100));
            r.EfficienzaPct = 100 * eta;
            r.Tloss = 290 * (1 - eta * Math.Pow(10, -lossDb / 10));
            r.Tant = r.Tpattern + r.Tloss;
            r.Tpre = 290 * (Math.Pow(10, nfDb / 10) - 1);
            r.Tsys = r.Tant + r.Tpre;
            r.GtAnt = gainDbi - 10 * Math.Log10(Math.Max(1e-9, r.Tant));
            r.GtSys = gainDbi - 10 * Math.Log10(Math.Max(1e-9, r.Tsys));
            List<double> ce = new List<double>(), ct = new List<double>();
            for (int e = 0; e <= 90; e += 5)
            {
                double fs, fg, av;
                ce.Add(e);
                ct.Add(Tpattern(g, thetaFwd, phiFwd, e, tsky, tground, kf2yn, out fs, out fg, out av) + r.Tloss);
            }
            r.CurveElev = ce.ToArray();
            r.CurveTant = ct.ToArray();
            return r;
        }

        static double Tpattern(PatternGrid g, double thetaFwd, double phiFwd, double elevDeg, double tsky, double tground, bool kf2yn,
                               out double skyFrac, out double groundFrac, out double avgGain)
        {
            double tf = thetaFwd * Math.PI / 180, pf = phiFwd * Math.PI / 180, el = elevDeg * Math.PI / 180;
            double fx = Math.Sin(tf) * Math.Cos(pf), fy = Math.Sin(tf) * Math.Sin(pf), fz = Math.Cos(tf);
            // "su" dell'antenna: verticale tolta la componente lungo la direzione del lobo
            double ux = -fz * fx, uy = -fz * fy, uz = 1 - fz * fz;
            double ul = Math.Sqrt(ux * ux + uy * uy + uz * uz);
            if (ul < 1e-6) { ux = 1; uy = 0; uz = 0; ul = 1; }
            ux /= ul; uy /= ul; uz /= ul;
            double sum = 0, sumT = 0, sky = 0, gnd = 0;
            for (int i = 0; i < g.NTheta; i++)
            {
                double w = CellWeight(g, i, 180);
                double th = g.ThetaAt(i) * Math.PI / 180;
                for (int j = 0; j < g.NPhi; j++)
                {
                    double ph = g.PhiAt(j) * Math.PI / 180;
                    double x = Math.Sin(th) * Math.Cos(ph), y = Math.Sin(th) * Math.Sin(ph), z = Math.Cos(th);
                    double cf = x * fx + y * fy + z * fz, cu = x * ux + y * uy + z * uz;
                    double zrot = cf * Math.Sin(el) + cu * Math.Cos(el);     // quota della direzione dopo aver alzato il lobo
                    double gw = Lin(g.Total[i, j]) * w;
                    sum += gw;
                    // celle sull'orizzonte (zrot = 0, la riga theta 90 a elevazione 0): meta' cielo e meta' terreno, come l'integrazione
                    // continua di TANT (a 0 gradi un diagramma simmetrico da' esattamente (Tcielo + Tterreno) / 2); prima andavano tutte al cielo
                    if (Math.Abs(zrot) < 1e-9) { sumT += gw * (tsky + tground) / 2; sky += gw / 2; gnd += gw / 2; }
                    else if (zrot > 0) { sumT += gw * tsky; sky += gw; }
                    else { sumT += gw * tground; gnd += gw; }
                }
            }
            avgGain = sum / (4 * Math.PI);
            double norm = kf2yn ? sum : 4 * Math.PI;
            skyFrac = norm > 0 ? sky / norm : 0;
            groundFrac = norm > 0 ? gnd / norm : 0;
            return norm > 0 ? sumT / norm : 0;
        }
    }
}
