// YA-NEC 1.5.8 - G/T dello stack per la maschera VHF dell'ottimizzatore (bozzetto approvato da IC8TEM il 22/11/2026 17:03; dati di IC8TEM
// 17:04: temperature come VE7BQH, preamplificatore 20 dB, 2 dB di cavo). Stesso calcolo della linguetta Rumore/GT/Stack (NoiseGt, metodo di
// VE7BQH e TANT): antenna in spazio libero, stack di righe x colonne alle distanze DL6WU ricavate dalle aperture a -3 dB di questo passo,
// puntato all'elevazione scelta, cielo e terreno a temperatura uniforme, correzione KF2YN.
//
// Due modi:
//   veloce - diagramma NEC della singola antenna per il fattore di schiera (antenne uguali, in fase, senza accoppiamento):
//            G stack = G singola x |AF|^2 / N, AF = somma di exp(j k p . u). Il controllo con lo stack NEC completo (accoppiamento
//            compreso) lo fa l'ottimizzatore ogni 10 passi e alla fine, e corregge il G/T veloce della differenza.
//   NEC    - a ogni passo lo stack completo con StackDetect.Crea (lento: N volte i segmenti).
// Catena di ricezione: Tant (diagramma + perdite prima del preamplificatore: combinatore e conduttori dell'antenna); preamplificatore
// con NF e guadagno; cavo dopo il preamplificatore: T = 290 (10^(L/10) - 1), diviso il guadagno del preamplificatore (Friis).
// G/T sistema = G - 10 log10(Tant + Tpre + Tcavo / Gpre).
using System;
using System.Numerics;
using System.Threading;

namespace YaNec.Engine
{
    public class OttGtImpostazioni
    {
        public int Righe = 2, Colonne = 2;
        public double Elevazione = 30;
        public double Tcielo = 200, Tterreno = 1000;      // uniformi come VE7BQH (a 144 MHz; si cambiano con la frequenza)
        public double PerditePrima = 0.2;                 // combinatore e cavi di fase, dB (prima del preamplificatore)
        public double NfDb = 0.25, GuadagnoPreDb = 20, CavoDopoDb = 2;
        public bool Veloce = true;
        public int PassoGradi = 3;

        /// <summary>Temperature uniformi della linguetta Rumore/GT/Stack (TANT, VE7BQH) per la frequenza.</summary>
        public static void Temperature(double fMHz, out double cielo, out double terreno)
        {
            cielo = fMHz < 300 ? 200 : (fMHz < 1000 ? 20 : 10);
            terreno = fMHz < 300 ? 1000 : (fMHz < 1000 ? 350 : 290);
        }
    }

    public class OttGtRisultato
    {
        public double GtSys = double.NaN, GtAnt = double.NaN, Tant = double.NaN, Tsys = double.NaN, Gstack = double.NaN, Gsingola = double.NaN;
        public double GtSingola = double.NaN, Dv = double.NaN, Dh = double.NaN, HpbwEl = double.NaN, HpbwAz = double.NaN;
        public bool ConNec;
    }

    public static class OttGt
    {
        static double Lin(double db) { return db < -900 ? 0 : Math.Pow(10, db / 10); }

        /// <summary>G/T dello stack del modello m (geometria g) alla frequenza f. nec = stack completo con il NEC invece del fattore di schiera.</summary>
        public static OttGtRisultato Calcola(NecModel m, SegmentGeometry g, double f, OttGtImpostazioni s, bool nec, CancellationToken ct)
        {
            OttGtRisultato res = new OttGtRisultato { ConNec = nec };
            NecModel mf = m.Clone();
            mf.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
            mf.GroundFlag = 0;
            RunResult r = Analysis.Run(mf, g, f, s.PassoGradi, ct);
            ct.ThrowIfCancellationRequested();
            double lambda = 299.792458 / f;
            res.HpbwEl = r.HpbwEl; res.HpbwAz = r.HpbwAz;
            int righe = Math.Max(1, s.Righe), colonne = Math.Max(1, s.Colonne), n = righe * colonne;
            res.Dv = righe > 1 ? StackDetect.Dl6wu(lambda, r.HpbwEl) : 0;
            res.Dh = colonne > 1 ? StackDetect.Dl6wu(lambda, r.HpbwAz) : 0;
            if ((righe > 1 && double.IsNaN(res.Dv)) || (colonne > 1 && double.IsNaN(res.Dh))) throw new NecException("apertura a -3 dB non trovata: niente distanza DL6WU");
            double thF = r.ThetaMax, phF = r.PhiMax;
            double eff = r.Efficiency > 0 ? r.Efficiency : 100;
            res.Gsingola = r.Engine.Gain(thF, phF).GainTotal;
            GtResult singola = NoiseGt.Gt(r.Grid, thF, phF, res.Gsingola, s.Elevazione, s.Tcielo, s.Tterreno, 0, s.NfDb, true, eff);
            res.GtSingola = singola.GtAnt;
            PatternGrid gs;
            double gst, effs = eff;
            if (n == 1) { gs = r.Grid; gst = res.Gsingola; }
            else if (nec)
            {
                NecModel ms = StackDetect.Crea(mf, righe, colonne, res.Dv, res.Dh, phF, null);
                RunResult rs = Analysis.Run(ms, ms.BuildGeometry(), f, s.PassoGradi, ct);
                gs = rs.Grid;
                gst = rs.Engine.Gain(thF, phF).GainTotal;
                effs = rs.Efficiency > 0 ? rs.Efficiency : 100;
            }
            else
            {
                // fattore di schiera: righe in verticale (z), colonne in orizzontale perpendicolari al lobo, come StackDetect.Crea
                double k = 2 * Math.PI / lambda, ph = phF * Math.PI / 180, hx = -Math.Sin(ph), hy = Math.Cos(ph);
                double[] px = new double[n], py = new double[n], pz = new double[n];
                int q = 0;
                for (int a = 0; a < righe; a++)
                    for (int b = 0; b < colonne; b++, q++)
                    {
                        double off = colonne > 1 ? (b - (colonne - 1) / 2.0) * res.Dh : 0;
                        px[q] = off * hx; py[q] = off * hy; pz[q] = righe > 1 ? (a - (righe - 1) / 2.0) * res.Dv : 0;
                    }
                Func<double, double, double> af2 = (thd, phd) =>
                {
                    double t = thd * Math.PI / 180, p = phd * Math.PI / 180;
                    double ux = Math.Sin(t) * Math.Cos(p), uy = Math.Sin(t) * Math.Sin(p), uz = Math.Cos(t);
                    Complex sum = Complex.Zero;
                    for (int i = 0; i < n; i++) sum += Complex.FromPolarCoordinates(1, k * (px[i] * ux + py[i] * uy + pz[i] * uz));
                    return sum.Magnitude * sum.Magnitude / n;
                };
                PatternGrid src = r.Grid;
                gs = new PatternGrid { ThetaStep = src.ThetaStep, PhiStep = src.PhiStep, NTheta = src.NTheta, NPhi = src.NPhi, Total = new double[src.NTheta, src.NPhi] };
                for (int i = 0; i < src.NTheta; i++)
                    for (int j = 0; j < src.NPhi; j++)
                    {
                        double gl = Lin(src.Total[i, j]) * af2(src.ThetaAt(i), src.PhiAt(j));
                        gs.Total[i, j] = gl > 0 ? 10 * Math.Log10(gl) : -999.99;
                    }
                gst = res.Gsingola + 10 * Math.Log10(Math.Max(1e-12, af2(thF, phF)));
            }
            res.Gstack = gst;
            GtResult x = NoiseGt.Gt(gs, thF, phF, gst, s.Elevazione, s.Tcielo, s.Tterreno, s.PerditePrima, s.NfDb, true, effs);
            res.Tant = x.Tant;
            res.GtAnt = x.GtAnt;
            double tcavo = 290 * (Math.Pow(10, s.CavoDopoDb / 10) - 1);
            res.Tsys = x.Tant + x.Tpre + tcavo / Math.Pow(10, s.GuadagnoPreDb / 10);
            res.GtSys = gst - 10 * Math.Log10(Math.Max(1e-9, res.Tsys));
            return res;
        }
    }
}
