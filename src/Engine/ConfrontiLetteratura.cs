// YA-NEC 1.0.17 - confronti con casi pubblicati (IC8TEM 17/11/2026 18:08 "si procedi"): Yagi a 6 elementi di MathWorks (Antenna Toolbox,
// "Direct Search Based Optimization of Six-Element Yagi-Uda Antenna": 165 MHz, tubo da 12,7 mm, dipolo ripiegato a 300 ohm; scheda tecnica
// 10,5 dBi, F/B 16 dB, aperture 54 e 63 gradi; misure ottimizzate pubblicate) come prova del nostro ottimizzatore, ed elica di MathWorks
// ("Helical Antenna Design": 1,65 GHz, raggio 28 mm, passo 11,2 gradi, 17,5 spire, specifica 13 +- 1,5 dBi) come prova della scheda elica.
// Le righe dell'autotest dicono somiglianze e differenze; le soglie sono larghe (modelli diversi: MathWorks usa strisce, YA-NEC fili).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace YaNec.Engine
{
    public static class ConfrontiLetteratura
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        const double C0 = 299.792458;

        /// <summary>Modello della Yagi MathWorks con variabili SY (metri). Elementi lungo X, boom lungo Z, fascio verso +Z.</summary>
        public static string YagiMathWorks(double[] v)
        {
            // v: LR, LD, L1, L2, L3, L4, SR, S1, S2, S3, S4, FS
            StringBuilder s = new StringBuilder();
            s.AppendLine("CM Yagi 6 elementi MathWorks (Antenna Toolbox), 165 MHz, dipolo ripiegato a 300 ohm");
            s.AppendLine("CE");
            string[] n = { "LR", "LD", "L1", "L2", "L3", "L4", "SR", "S1", "S2", "S3", "S4", "FS" };
            for (int i = 0; i < n.Length; i++) s.AppendLine("SY " + n[i] + "=" + v[i].ToString("R", CI));
            string a = "0.00635";
            s.AppendLine("GW 1 21 -LR/2 0 0 LR/2 0 0 " + a);
            s.AppendLine("GW 2 21 -LD/2 0 SR LD/2 0 SR " + a);
            s.AppendLine("GW 3 21 -LD/2 FS SR LD/2 FS SR " + a);
            s.AppendLine("GW 4 1 -LD/2 0 SR -LD/2 FS SR " + a);
            s.AppendLine("GW 5 1 LD/2 0 SR LD/2 FS SR " + a);
            s.AppendLine("GW 6 21 -L1/2 0 SR+S1 L1/2 0 SR+S1 " + a);
            s.AppendLine("GW 7 21 -L2/2 0 SR+S1+S2 L2/2 0 SR+S1+S2 " + a);
            s.AppendLine("GW 8 21 -L3/2 0 SR+S1+S2+S3 L3/2 0 SR+S1+S2+S3 " + a);
            s.AppendLine("GW 9 21 -L4/2 0 SR+S1+S2+S3+S4 L4/2 0 SR+S1+S2+S3+S4 " + a);
            s.AppendLine("GE 0");
            s.AppendLine("EX 0 2 11 0 1 0");
            s.AppendLine("FR 0 1 0 0 165 0");
            s.AppendLine("EN");
            return s.ToString();
        }

        static string Riga(string nome, RunResult r)
        {
            return string.Format(CI, "{0}: G {1:0.00} dBi, F/B {2:0.0} dB, Z {3:0.0} {4:+0.0;-0.0;+0.0}j (ROS a 300 ohm {5:0.00}), aperture {6:0} e {7:0} gradi",
                nome, r.GmaxDbi, r.FrontBack, r.Zin.Real, r.Zin.Imaginary, r.Swr, Math.Min(r.HpbwAz, r.HpbwEl), Math.Max(r.HpbwAz, r.HpbwEl));
        }

        public static void Esegui(Action<string> log, List<string> errori)
        {
            double lam = C0 / 165;
            // 1) misure ottimizzate pubblicate da MathWorks, calcolate col nostro NEC
            double[] mw = { 0.90846, 0.84596, 0.72096, 0.65846, 0.72096, 0.65846, 0.42008, 0.36338, 0.23838, 0.51673, 0.53236, 0.016118 };
            NecModel m = NecModel.Parse(YagiMathWorks(mw)); m.Z0 = 300;
            RunResult r = Analysis.Run(m, m.BuildGeometry(), 165, 2.0);
            string riga = Riga("Yagi MathWorks, misure ottimizzate da MathWorks (pubblicato: 10,59 dBi, F/B 15,6 dB, aperture 50 e 62 gradi)", r);
            bool ok = Math.Abs(r.GmaxDbi - 10.59) < 0.8;
            log((ok ? "OK   " : "ERR  ") + riga);
            if (!ok) errori.Add(riga);
            // 2) il nostro ottimizzatore dalla stessa partenza (lunghezze 0,5 lambda, spaziature 0,3 e 0,25 lambda) e con gli stessi limiti
            double[] p0 = { 0.5 * lam, 0.5 * lam, 0.5 * lam, 0.5 * lam, 0.5 * lam, 0.5 * lam, 0.3 * lam, 0.25 * lam, 0.25 * lam, 0.25 * lam, 0.25 * lam, lam / 60 };
            double[] mn = { 0.3, 0.45, 0.3, 0.3, 0.3, 0.3, 0.05, 0.05, 0.05, 0.05, 0.05, 0.004 }, mx = { 0.9, 0.6, 0.7, 0.7, 0.7, 0.7, 0.35, 0.2, 0.2, 0.3, 0.3, 0.009 };
            string[] nomi = { "LR", "LD", "L1", "L2", "L3", "L4", "SR", "S1", "S2", "S3", "S4", "FS" };
            Ottimizzatore o = new Ottimizzatore { Partenza = NecModel.Parse(YagiMathWorks(p0)), Simplesso = true, MaxPassi = 500, Principale = 1 };
            o.Partenza.Z0 = 300;
            for (int i = 0; i < nomi.Length; i++) o.Variabili.Add(new OttVariabile { Nome = nomi[i], Attiva = true, Valore = p0[i], Min = mn[i] * lam, Max = mx[i] * lam, Passo = 0.002 * lam });
            o.Frequenze.AddRange(new[] { 160.875, 165.0, 169.125 });
            o.Obiettivi.Add(new OttObiettivo { Chiave = "g", Peso = 60 });
            o.Obiettivi.Add(new OttObiettivo { Chiave = "fb", Peso = 40, HaSoglia = true, Soglia = 15 });
            o.Obiettivi.Add(new OttObiettivo { Chiave = "ros", Peso = 50, HaSoglia = true, Soglia = 1.92 });
            try
            {
                o.Esegui(CancellationToken.None);
                NecModel best = o.Modello(o.Migliore.Valori); best.Z0 = 300;
                RunResult rb = Analysis.Run(best, best.BuildGeometry(), 165, 2.0);
                log("INFO " + Riga("Yagi MathWorks, nostro ottimizzatore (simplesso, " + o.Passi + " passi, stessa partenza e stessi limiti)", rb));
                StringBuilder mis = new StringBuilder();
                for (int i = 0; i < nomi.Length; i++) mis.Append(nomi[i]).Append(' ').Append(o.Migliore.Valori[i].ToString("0.0000", CI)).Append(i < nomi.Length - 1 ? ", " : "");
                log("INFO misure trovate (m): " + mis + " | MathWorks: LR 0,9085, LD 0,8460, L1 0,7210, L2 0,6585, L3 0,7210, L4 0,6585, SR 0,4201, S1 0,3634, S2 0,2384, S3 0,5167, S4 0,5324, FS 0,0161");
            }
            catch (Exception ex) { log("INFO ottimizzatore sulla Yagi MathWorks non concluso: " + ex.Message); }

            // 3) elica MathWorks
            double lh = C0 / 1650, R = 0.028, sp = 2 * Math.PI * R * Math.Tan(11.2 * Math.PI / 180);
            HelixSpec hs = new HelixSpec { Modo = 2, FreqMHz = 1650, Spire = 18, Circ = 2 * Math.PI * R / lh, Passo = sp / lh, Conduttore = 0.0006, Riflettore = 0, DiamRifl = 0.6 / lh, Radiali = 16, Gap = 0.003 };
            try
            {
                NecModel hm;
                HelixEsito he = HelixGen.Esito(hs, CancellationToken.None, out hm);
                string rh = string.Format(CI, "Elica MathWorks (1,65 GHz, raggio 28 mm, passo 11,2 gradi = {0:0.0} mm, 18 spire invece di 17,5, piano di 600 mm a radiali): NEC {1:0.00} dBic, Kraus {2:0.00}, Emerson {3:0.00}; specifica MathWorks 13 +- 1,5 dBi",
                    sp * 1000, he.GdbicNec, he.GdbicKraus, he.GdbicEmerson);
                bool okh = he.GdbicNec > 11.5 && he.GdbicNec < 14.5;
                log((okh ? "OK   " : "ERR  ") + rh);
                if (!okh) errori.Add(rh);
            }
            catch (Exception ex) { errori.Add("elica MathWorks: " + ex.Message); }
        }
    }
}
