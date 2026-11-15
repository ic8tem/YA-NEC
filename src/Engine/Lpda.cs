// YA-NEC - calcolatore LPDA (log-periodica), metodo di Carrel con le curve corrette di Butson e Thompson (Balanis 11.4.3).
// alpha = atan[(1-tau)/(4 sigma)]; Bar = 1,1 + 7,7 (1-tau)^2 cot(alpha); Bs = B Bar; L = (lambda_max/4)(1 - 1/Bs) cot(alpha);
// N = 1 + ln Bs / ln(1/tau); Za = 120 [ln(l/d) - 2,25]; Z0 = Rin^2/(8 sigma' Za) + Rin sqrt[(Rin/(8 sigma' Za))^2 + 1], sigma' = sigma/sqrt(tau).
// sigma ottimo = 0,243 tau - 0,051. Linea a due tubi: Z0 = 120 acosh(s/d) -> s = d cosh(Z0/120). Stub in corto lambda_max/8.
// Modello NEC: elementi lungo Y, boom lungo X, il piu' lungo a X = 0 e il piu' corto in avanti (+X, azimut 0);
// linea incrociata tra elementi vicini (TL con Z0 negativa), sorgente sull'elemento piu' corto, stub in corto dietro il piu' lungo
// (dalla 0.4.7 come ammettenza in parallelo sull'elemento piu' lungo, riga CM YA-NEC STUB, senza filo fittizio).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YaNec.Engine
{
    public class LpdaDesign
    {
        public double Fmin, Fmax, Tau, Sigma, Rin = 50, DiamMm = 6;
        public bool Rastrema;                                       // 0.4.30: elementi a tubi telescopici (HF), schema di RastremazioneHf
        public string SchemaRastr = RastremazioneHf.Predefinita;
        public int DiamMode;            // 0 uguale, 1 tre gruppi, 2 l/d costante
        public double LineTubeMm = 10;
        public double StubMm = double.NaN;    // NaN = lambda_max/8
        // ricavati
        public double B, AlphaDeg, Bar, Bs, Nexact, Lboom, Za, SigmaP, Z0, LineSpacingMm, LambdaMax;
        public int N;
        public double[] Len, Pos, Diam;       // m
        public double[] UserLen;              // misure "Tue" (m), null = calcolate

        public static double SigmaOpt(double tau) { return 0.243 * tau - 0.051; }

        /// <summary>Tau dal guadagno voluto (dBi) lungo sigma ottimo: stima lineare tarata sull'esempio di Balanis
        /// (8 dB -> tau 0,865, sigma 0,157), circa 2,5 dB ogni 0,1 di tau. E' solo il punto di partenza: il valore vero lo da' il NEC.</summary>
        public static double TauFromGain(double gainDbi)
        {
            double t = 0.865 + (gainDbi - 8.0) / 25.0;
            return Math.Max(0.80, Math.Min(0.97, t));
        }

        public void Compute()
        {
            const double c = 299.792458;
            if (Fmin <= 0 || Fmax <= Fmin) throw new ArgumentException("La banda deve avere fmax maggiore di fmin.");
            if (Tau <= 0.5 || Tau >= 1) throw new ArgumentException("Tau deve stare tra 0,5 e 1.");
            if (Sigma <= 0) throw new ArgumentException("Sigma deve essere positiva.");
            B = Fmax / Fmin;
            double a = Math.Atan((1 - Tau) / (4 * Sigma));
            AlphaDeg = a * 180 / Math.PI;
            Bar = 1.1 + 7.7 * (1 - Tau) * (1 - Tau) / Math.Tan(a);
            Bs = B * Bar;
            LambdaMax = c / Fmin;
            Nexact = 1 + Math.Log(Bs) / Math.Log(1 / Tau);
            N = (int)Math.Ceiling(Nexact - 1e-9);
            Len = new double[N];
            Pos = new double[N];
            Diam = new double[N];
            Len[0] = LambdaMax / 2;
            for (int i = 1; i < N; i++) Len[i] = Len[i - 1] * Tau;
            if (UserLen != null && UserLen.Length == N)
                for (int i = 0; i < N; i++) if (UserLen[i] > 0) Len[i] = UserLen[i];
            Pos[0] = 0;
            for (int i = 1; i < N; i++) Pos[i] = Pos[i - 1] + 2 * Sigma * Len[i - 1];
            Lboom = LambdaMax / 4 * (1 - 1 / Bs) / Math.Tan(a);
            double d = DiamMm / 1000.0;
            double lmean = 0;
            foreach (double l in Len) lmean += l;
            lmean /= N;
            for (int i = 0; i < N; i++)
            {
                if (DiamMode == 2) Diam[i] = d * Len[i] / Len[0];                  // l/d costante (d = diametro dell'elemento piu' lungo)
                else if (DiamMode == 1) Diam[i] = d * (i < N / 3 ? 1.0 : (i < 2 * N / 3 ? 0.8 : 0.6));   // tre gruppi
                else Diam[i] = d;
            }
            Za = 120 * (Math.Log(lmean / d) - 2.25);
            SigmaP = Sigma / Math.Sqrt(Tau);
            double q = Rin / (8 * SigmaP * Za);
            Z0 = Rin * q + Rin * Math.Sqrt(q * q + 1);
            LineSpacingMm = LineTubeMm * Math.Cosh(Z0 / 120);
            if (double.IsNaN(StubMm) || StubMm <= 0) StubMm = LambdaMax / 8 * 1000;
        }

        static string F(double v) { return v != 0 && Math.Abs(v) < 1e-3 ? v.ToString("G9", CultureInfo.InvariantCulture) : v.ToString("0.######", CultureInfo.InvariantCulture); }   // valori piccoli (C in farad, L in henry) in notazione esponenziale

        /// <summary>Schede NEC. heightM = 0: spazio libero; altrimenti terreno (perfetto se epsr 0) e boom all'altezza data.</summary>
        public string ToNec(double fCalc, double heightM, GroundSpec ground, double z0ref = 50)
        {
            const double c = 299.792458;
            double lamMin = c / Fmax;
            double segMax = lamMin / 20;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "CM LPDA {0:0.###}-{1:0.###} MHz, tau {2:0.000}, sigma {3:0.000}, {4} elementi (metodo Carrel, calcolatore YA-NEC)", Fmin, Fmax, Tau, Sigma, N));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "CM linea di alimentazione incrociata Z0 = {0:0.0} ohm, stub in corto {1:0} mm", Z0, StubMm));
            sb.AppendLine("CM YA-NEC Z0=" + F(z0ref));
            // 0.4.7: lo stub in corto non e' piu' una linea verso un filo fittizio lontano (IC8TEM 13/11/2026 18:25), ma un'ammettenza
            // in parallelo sull'elemento piu' lungo, ricalcolata da YA-NEC a ogni frequenza (riga STUB); gli altri programmi leggono
            // l'ammettenza alla frequenza del file, scritta nella prima linea TL.
            int seg0 = 0;
            {
                int ns0 = (int)Math.Ceiling(Len[0] / segMax); if (ns0 < 5) ns0 = 5; if (ns0 % 2 == 0) ns0++;
                seg0 = (ns0 + 1) / 2;
            }
            Stub stub = new Stub { Tag = 1, Seg = seg0, Z0 = Z0, LenM = StubMm / 1000, Vf = 1, Corto = true };
            sb.AppendLine(Stub.Riga(stub).Replace("YA-NEC STUB", "CM YA-NEC STUB"));
            sb.AppendLine("CE");
            double h = heightM > 0 ? heightM : 0;
            int[] segs = new int[N];
            for (int i = 0; i < N; i++)
            {
                int ns = (int)Math.Ceiling(Len[i] / segMax);
                if (ns < 5) ns = 5;
                if (ns % 2 == 0) ns++;
                segs[i] = ns;
                sb.AppendLine("GW " + (i + 1) + " " + ns + " " + F(Pos[i]) + " " + F(-Len[i] / 2) + " " + F(h) + " " + F(Pos[i]) + " " + F(Len[i] / 2) + " " + F(h) + " " + F(Diam[i] / 2));
            }
            sb.AppendLine("GE 0");
            if (heightM > 0)
            {
                if (ground == null || ground.Kind == GroundKind.Perfect) sb.AppendLine("GN 1");
                else sb.AppendLine("GN 0 0 0 0 " + F(ground.Epsr) + " " + F(ground.Sigma));
            }
            System.Numerics.Complex ys = stub.Y(fCalc);
            for (int i = 0; i < N - 1; i++)
                sb.AppendLine("TL " + (i + 1) + " " + ((segs[i] + 1) / 2) + " " + (i + 2) + " " + ((segs[i + 1] + 1) / 2) + " " + F(-Z0) + " 0 " +
                    (i == 0 ? F(ys.Real) + " " + F(ys.Imaginary) : "0 0") + " 0 0");
            sb.AppendLine("EX 0 " + N + " " + ((segs[N - 1] + 1) / 2) + " 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(fCalc) + " 0");
            sb.AppendLine("EN");
            if (Rastrema)
            {
                NecModel m = NecModel.Parse(sb.ToString());
                if (RastremazioneHf.ApplicaIsolati(m, SchemaRastr, 1.0, null) > 0) return m.ToNec();
            }
            return sb.ToString();
        }
    }
}
