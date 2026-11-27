// YA-NEC - autotest della scheda "Trombe e illuminatori". Valori attesi da verifica/rif_trombe.py, rif_diagrammi.py,
// trascrizione_tromba.py e trascrizione_griglia.py.
#nullable disable
using System;
using System.Globalization;
using System.Linq;

namespace YaNec.Engine
{
    public static class TrombaSelfTest
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static int ok, ko;

        static void Prova(Action<string> log, string nome, double valore, double atteso, double toll)
        {
            bool b = !double.IsNaN(valore) && Math.Abs(valore - atteso) <= toll;
            if (b) ok++; else ko++;
            log(string.Format(CI, "{0} {1}: {2:0.######} (atteso {3:0.######} +/- {4:0.######})", b ? "OK  " : "ERR ", nome, valore, atteso, toll));
        }

        static double Quota(TrombaResult r, string inizio) { var q = r.Quote.FirstOrDefault(x => x.Nome.StartsWith(inizio)); return q == null ? double.NaN : q.Mm; }

        public static bool Esegui(Action<string> log)
        {
            ok = 0; ko = 0;
            log("=== Trombe e illuminatori: autotest ===");

            // 1. Bessel
            Prova(log, "J1(1)", Tromba.J1(1.0), 0.4400505857, 1e-6);
            Prova(log, "J1(5)", Tromba.J1(5.0), -0.3275791376, 1e-6);
            Prova(log, "J2(3)", Tromba.J2(3.0), 0.4860912606, 1e-6);

            // 2. apertura TE11 in fase: efficienza 0,8368
            Prova(log, "TE11: efficienza d'apertura", new Tromba.ApCircolare(0.5, 0, 0, 1.0, 0).Eta, 0.8368, 0.001);

            // 3. Balanis, esempio 13.6: 22,6 dBi a 11 GHz con WR90
            {
                double lam = Parabola.C0 / 11e9, chi, a1, b1, re, rh, pe, ph;
                Tromba.Balanis(22.6, 0.02286 / lam, 0.01016 / lam, out chi, out a1, out b1, out re, out rh, out pe, out ph);
                Prova(log, "Balanis: chi", chi, 11.1154, 1e-3);
                Prova(log, "Balanis: a1 (lambda)", a1, 6.0024, 1e-3);
                Prova(log, "Balanis: b1 (lambda)", b1, 4.7150, 1e-3);
                Prova(log, "Balanis: pe = ph (lambda)", pe, 10.0037, 1e-3);
                Prova(log, "Balanis: ph (lambda)", ph, 10.0037, 1e-3);
                Prova(log, "Balanis: guadagno integrato (dBi)", 10 * Math.Log10(new Tromba.ApRettangolare(a1, b1, re, rh).Guadagno), 22.623, 0.02);
            }

            // 4. W2IMU: sezione di fase come negli esempi di W1GHZ (2,8 e 3,52 lambda)
            Prova(log, "W2IMU: C per B = 1,63 lambda", Tromba.FaseW2imu_lambda(1.63), 2.782, 0.002);
            Prova(log, "W2IMU: C per B = 1,79 lambda", Tromba.FaseW2imu_lambda(1.79), 3.524, 0.002);

            // 5. progetti per f/D
            {
                var w = Tromba.Calcola(new TrombaInput { Tipo = TipoTromba.W2IMU, FreqMHz = 1296.1, FsuD = 0.55 }, false);
                Prova(log, "W2IMU 1296 f/D 0,55: bocca (lambda)", Quota(w, "Diametro della bocca") / w.Lambda_mm, 1.2502, 0.002);
                Prova(log, "W2IMU: -10 dB piano E (gradi)", w.Theta10E, 54.32, 0.1);
                Prova(log, "W2IMU: -10 dB piano H (gradi)", w.Theta10H, 54.38, 0.1);
                Prova(log, "W2IMU: guadagno (dBi)", w.Guadagno_dBi, 10.460, 0.02);
                // 1.7.3: sonda di partenza da W1GHZ (QEX 2001), ingresso 0,70 lambda: lambda guida 422,99 mm
                Prova(log, "W2IMU 1296: sonda dal fondo (mm)", Quota(w, "Sonda dal fondo"), 93.06, 0.05);
                Prova(log, "W2IMU 1296: lunghezza della sonda (mm)", Quota(w, "Lunghezza della sonda"), 50.89, 0.05);
                Prova(log, "W2IMU 1296: diametro della sonda (mm)", Quota(w, "Diametro della sonda"), 10.18, 0.05);
                var c = Tromba.Calcola(new TrombaInput { Tipo = TipoTromba.CoffeeCan, FreqMHz = 2320.1, FsuD = 0.38 }, false);
                Prova(log, "Coffee-can f/D 0,38: diametro (lambda)", Quota(c, "Diametro interno") / c.Lambda_mm, 0.7190, 0.002);
                var k = Tromba.Calcola(new TrombaInput { Tipo = TipoTromba.Conica, FreqMHz = 5760.1, FsuD = 0.8 }, false);
                Prova(log, "Conica f/D 0,8: bocca (lambda)", Quota(k, "Diametro della bocca") / k.Lambda_mm, 1.9460, 0.003);
                Prova(log, "Conica: guadagno (dBi)", k.Guadagno_dBi, 13.037, 0.03);
                var pg = Tromba.Calcola(new TrombaInput { Tipo = TipoTromba.Piramidale, Progetto = ProgettoTromba.PerGuadagno, Guadagno_dBi = 15.0, FreqMHz = 10368.1 }, false);
                Prova(log, "Piramidale 15 dBi WR90: a1 (lambda)", Quota(pg, "Bocca, lato H") / pg.Lambda_mm, 2.5836, 0.002);
                Prova(log, "Piramidale: b1 (lambda)", Quota(pg, "Bocca, lato E") / pg.Lambda_mm, 1.9036, 0.002);
                Prova(log, "Piramidale: svasatura (lambda)", Quota(pg, "Lunghezza della svasatura") / pg.Lambda_mm, 1.2572, 0.002);
                Prova(log, "Piramidale: guadagno integrato (dBi)", pg.Guadagno_dBi, 15.023, 0.02);
                Prova(log, "Piramidale: nodi NEC sciolti", pg.NecNodiSciolti, 0, 0);
            }

            // 6. griglia NEC
            {
                var v = Tromba.Calcola(new TrombaInput { Tipo = TipoTromba.SuperVE4MA, FreqMHz = 1296.1 }, false);
                Prova(log, "Super-VE4MA 1296: segmenti NEC", v.NecSegmenti, 4683, 0);
                Prova(log, "Super-VE4MA: nodi NEC sciolti", v.NecNodiSciolti, 0, 0);
                var ch = Tromba.Calcola(new TrombaInput { Tipo = TipoTromba.Chaparral, FreqMHz = 1296.1 }, false);
                Prova(log, "Chaparral: nodi NEC sciolti", ch.NecNodiSciolti, 0, 0);
                bool deck = v.Nec != null && v.Nec.Contains("GE 0") && v.Nec.Contains("RP 0 181 1 1000 0 90 1 0") && v.Nec.TrimEnd().EndsWith("EN");
                if (deck) ok++; else ko++;
                log((deck ? "OK  " : "ERR ") + " NEC: schede GE, RP (phi 90, 0, 45) ed EN presenti");
            }

            log(string.Format(CI, "=== Trombe: {0} prove superate, {1} fallite ===", ok, ko));
            return ko == 0;
        }
    }
}
