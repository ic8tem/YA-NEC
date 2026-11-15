// YA-NEC - Calcolatore "Parabola offset": geometria, centine, prestazioni, lobi, EME.
// Metodo: paraboloide madre z = (x^2+y^2)/4F con fuoco (0,0,F); apertura proiettata = cerchio di diametro D
// centrato in (0, yc), bordo inferiore y1 = yc - D/2 (W1GHZ cap. 5; Bob Atkins; OE5JFL "Ofs_calc_jfl").
//  - il bordo fisico e' un'ellisse piana: asse minore D, asse maggiore D / cos(inclinazione);
//  - il feed e' puntato sulla bisettrice degli angoli dei bordi visti dal fuoco (psi1, psi2) e deve illuminare
//    il semiangolo theta* = (psi2 - psi1)/2; f/D equivalente per il feed = 1 / (4 tan(theta*/2));
//  - campo d'apertura E = F_feed(theta') / rho (rho = distanza fuoco-superficie, dA = rho^2 dOmega):
//    efficienza = |integrale E dA|^2 / (A_ap * P_feed) = spillover * illuminazione * fase (integrazione 2D);
//  - diagrammi nei piani orizzontale e verticale dalla trasformata dell'apertura (somme separabili).
// Collaudo: il limite simmetrico (yc = 0) ridà il primo fuoco; la geometria degli 8 m di OK1DFC (Trenton 2024)
// ridà inclinazione 18,45 gradi e asse maggiore 8248 mm.
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace YaNec.Engine
{
    public class OffsetPuntoProfilo { public double S_m, X_m, Profondita_m; }

    public class OffsetResult
    {
        // geometria
        public double Lambda_mm, F_m, Y1_m, Y2_m, Yc_m, Psi1_deg, Psi2_deg, PsiO_deg, ThetaStar_deg, FsuDEquivalente;
        public double Inclinazione_deg, AsseMaggiore_m, AsseMinore_m, ProfonditaMax_m, ProfonditaMaxS_m, ProfonditaAssiale_m;
        public double DiametroMadre_m, FsuDMadre, FuocoBordoInf_m, FuocoBordoSup_m, FuocoCentro_m, AreaApertura_m2, AreaSuperficie_m2;
        public bool Ibrido;
        public List<OffsetPuntoProfilo> ProfiloVerticale = new List<OffsetPuntoProfilo>(), ProfiloOrizzontale = new List<OffsetPuntoProfilo>();

        // feed e illuminazione
        public double Q = double.NaN, MiraUsata_deg, MiraOttima_deg, EffMiraZero;
        public double IllumBordoInf_dB, IllumBordoSup_dB, IllumBordoLat_dB;
        public double DiametroFeedStimato_mm;
        public bool FaseUsata; public double CentroFase_mm = double.NaN;

        // efficienze e guadagno
        public double EtaSpill, EtaTaper, EtaFase = 1, EtaPol = 1, EtaBlocco = 1, EtaRuze, EtaRete, ReteTrasmessa, EtaVarie, EtaApertura, EtaTotale;
        public double GuadagnoIdeale_dBi, Guadagno_dBi, SigmaTot_mm;

        // diagrammi (angolo dall'asse del fascio = asse della parabola madre)
        public double[] ThetaDeg, PattH_dB, PattV_dB;
        public double HpbwH_deg, HpbwV_deg, PrimoLoboH_dB, PrimoLoboV_dB, HpbwCoeff;

        // EME
        public double Squint_deg, Hpbw_deg, FillLuna, PerditaEco_dB, PerditaPuntamento_dB, Tolleranza05_deg, Tolleranza1_deg;

        // meccanica
        public double MassaSuperficie_kg, AreaVento_m2, CdRete, ForzaVento_N, ForzaVentoGhiaccio_N;

        public List<ParabolaBanda> Bande = new List<ParabolaBanda>();
        public List<ParabolaAvviso> Avvisi = new List<ParabolaAvviso>();
        public string Errore;
    }

    public static partial class Parabola
    {
        /// <summary>Profondita' della superficie misurata lungo la normale al piano del bordo, nel punto (s, x)
        /// del piano del bordo (s lungo l'asse maggiore dal bordo inferiore, x lungo l'asse minore).</summary>
        static double ProfonditaOffset(double F, double y1, double z1, double ty, double tz, double s, double x)
        {
            // punto del piano del bordo e normale (0, ny, nz) verso il paraboloide
            double qy = y1 + s * ty, qz = z1 + s * tz;
            double ny = -tz, nz = ty;
            // (qz + u nz) = (x^2 + (qy + u ny)^2) / 4F  ->  a u^2 + b u + c = 0
            double a = ny * ny, b = 2.0 * qy * ny - 4.0 * F * nz, c = x * x + qy * qy - 4.0 * F * qz;
            double u;
            if (Math.Abs(a) < 1e-15) u = -c / b;
            else
            {
                double disc = Math.Max(0.0, b * b - 4.0 * a * c);
                double u1 = (-b + Math.Sqrt(disc)) / (2.0 * a), u2 = (-b - Math.Sqrt(disc)) / (2.0 * a);
                u = Math.Abs(u1) < Math.Abs(u2) ? u1 : u2;
            }
            return Math.Abs(u);
        }

        public static OffsetResult CalcolaOffset(ParabolaInput p) { return CalcolaOffset(p, true); }

        public static OffsetResult CalcolaOffset(ParabolaInput p, bool conBande)
        {
            var r = new OffsetResult();
            if (p.Diametro_m <= 0 || p.FreqMHz <= 0 || p.FsuD <= 0.1) { r.Errore = "Diametro, frequenza e f/D devono essere positivi (f/D oltre 0,1)."; return r; }
            double lam = C0 / (p.FreqMHz * 1e6), lamMm = lam * 1000.0, k = 2.0 * Math.PI / lam;
            double D = p.Diametro_m, R = D / 2.0, F = p.FsuD * D, y1 = p.OffsetBordoInferiore_m, y2 = y1 + D, yc = y1 + R;
            r.Lambda_mm = lamMm; r.F_m = F; r.Y1_m = y1; r.Y2_m = y2; r.Yc_m = yc;

            // ---------------- geometria
            double psi1 = 2.0 * Math.Atan(y1 / (2.0 * F)), psi2 = 2.0 * Math.Atan(y2 / (2.0 * F));
            double psiO = 0.5 * (psi1 + psi2), thS = 0.5 * (psi2 - psi1);
            r.Psi1_deg = psi1 * 180 / Math.PI; r.Psi2_deg = psi2 * 180 / Math.PI; r.PsiO_deg = psiO * 180 / Math.PI; r.ThetaStar_deg = thS * 180 / Math.PI;
            r.FsuDEquivalente = 1.0 / (4.0 * Math.Tan(thS / 2.0));
            double z1 = y1 * y1 / (4 * F), z2 = y2 * y2 / (4 * F), zc = yc * yc / (4 * F);
            r.Inclinazione_deg = Math.Atan((z2 - z1) / D) * 180 / Math.PI;
            r.AsseMinore_m = D; r.AsseMaggiore_m = Math.Sqrt(D * D + (z2 - z1) * (z2 - z1));
            double ty = D / r.AsseMaggiore_m, tz = (z2 - z1) / r.AsseMaggiore_m;
            r.DiametroMadre_m = 2.0 * Math.Max(Math.Abs(y1), Math.Abs(y2)); r.FsuDMadre = F / r.DiametroMadre_m;
            r.Ibrido = y1 < 0;
            r.FuocoBordoInf_m = Math.Sqrt(y1 * y1 + (z1 - F) * (z1 - F));
            r.FuocoBordoSup_m = Math.Sqrt(y2 * y2 + (z2 - F) * (z2 - F));
            r.FuocoCentro_m = Math.Sqrt(yc * yc + (zc - F) * (zc - F));
            double best = 0, bestS = 0;
            for (int i = 0; i <= 2000; i++)
            {
                double s = r.AsseMaggiore_m * i / 2000.0, dpt = ProfonditaOffset(F, y1, z1, ty, tz, s, 0.0);
                if (dpt > best) { best = dpt; bestS = s; }
            }
            r.ProfonditaMax_m = best; r.ProfonditaMaxS_m = bestS; r.ProfonditaAssiale_m = best / Math.Cos(r.Inclinazione_deg * Math.PI / 180.0);
            double passo = Math.Max(1.0, p.PassoProfilo_mm) / 1000.0;
            for (double s = 0; s <= r.AsseMaggiore_m + 1e-9; s += passo)
                r.ProfiloVerticale.Add(new OffsetPuntoProfilo { S_m = s, X_m = 0, Profondita_m = ProfonditaOffset(F, y1, z1, ty, tz, s, 0.0) });
            r.ProfiloVerticale.Add(new OffsetPuntoProfilo { S_m = r.AsseMaggiore_m, X_m = 0, Profondita_m = 0 });
            double sC = r.AsseMaggiore_m / 2.0;
            for (double x = -R; x <= R + 1e-9; x += passo)
                r.ProfiloOrizzontale.Add(new OffsetPuntoProfilo { S_m = sC, X_m = x, Profondita_m = ProfonditaOffset(F, y1, z1, ty, tz, sC, x) });

            // ---------------- griglia d'apertura (punto medio, maschera circolare)
            const int Ng = 240;
            double h = D / Ng, dA = h * h;
            var cx = new double[Ng];
            for (int i = 0; i < Ng; i++) cx[i] = (i + 0.5) * h - R;
            var celle = new List<int>();
            for (int i = 0; i < Ng; i++) for (int j = 0; j < Ng; j++) if (cx[i] * cx[i] + cx[j] * cx[j] <= R * R) celle.Add(i * Ng + j);
            int nc = celle.Count;
            double Aap = nc * dA; r.AreaApertura_m2 = Aap;
            var X = new double[nc]; var Y = new double[nc]; var RHO = new double[nc]; var VY = new double[nc]; var VZ = new double[nc];
            var bloccata = new bool[nc];
            double rb = Math.Max(0, p.FeedDiametro_m / 2.0), areaSup = 0;
            for (int c = 0; c < nc; c++)
            {
                int i = celle[c] / Ng, j = celle[c] % Ng;
                double x = cx[i], y = cx[j] + yc, z = (x * x + y * y) / (4 * F);
                X[c] = x; Y[c] = y; VY[c] = y; VZ[c] = z - F;
                RHO[c] = Math.Sqrt(x * x + y * y + (z - F) * (z - F));
                bloccata[c] = x * x + y * y < rb * rb;
                areaSup += Math.Sqrt(1.0 + (x * x + y * y) / (4 * F * F)) * dA;
            }
            r.AreaSuperficie_m2 = areaSup;

            // ---------------- feed (stesse sorgenti del primo fuoco)
            FeedPrep fp = PreparaPiani(p);
            bool tab = fp == null && HaFeedTab(p);
            double q = double.NaN;
            if (fp == null && !tab)
            {
                q = Math.Log(Math.Pow(10.0, -Math.Abs(p.TaperBordo_dB) / 20.0)) / Math.Log(Math.Cos(thS));
                if (double.IsNaN(q) || q < 0) q = 0;
                r.Q = q;
            }
            r.FaseUsata = fp != null && fp.ConFase;

            // coseno dell'angolo dall'asse del feed (puntato a psiO + delta) per ogni cella
            Func<double, double[]> cosTh = delta =>
            {
                double pa = psiO + delta, sa = Math.Sin(pa), ca = Math.Cos(pa);
                var ct = new double[nc];
                for (int c = 0; c < nc; c++) ct[c] = (VY[c] * sa - VZ[c] * ca) / RHO[c];
                return ct;
            };
            // potenza totale del feed sulla sfera e potenze nel cono theta* (per l'asimmetria E/H)
            int ns = 1800; double pTot = 0, pCono = 0, aCono = 0;
            for (int i = 0; i < ns; i++)
            {
                double th = (i + 0.5) * Math.PI / ns, sn = Math.Sin(th), td = th * 180 / Math.PI;
                Complex fe = ValutaFeed(p, fp, q, tab, td, 1, k, 0, true), fh = ValutaFeed(p, fp, q, tab, td, 2, k, 0, true);
                double pt = 0.5 * (fe.Magnitude * fe.Magnitude + fh.Magnitude * fh.Magnitude) * sn;
                pTot += pt;
                if (th <= thS) { pCono += pt; Complex A = 0.5 * (fe + fh); aCono += A.Magnitude * A.Magnitude * sn; }
            }
            pTot *= 2.0 * Math.PI * (Math.PI / ns);
            r.EtaPol = pCono > 0 ? aCono / pCono : 1.0;

            // campo d'apertura per mira delta e centro di fase d
            Func<double[], double, bool, Complex[]> campo = (ct, d, conBlocco) =>
            {
                var E = new Complex[nc];
                for (int c = 0; c < nc; c++)
                {
                    if (conBlocco && bloccata[c]) continue;
                    double cth = Math.Max(-1.0, Math.Min(1.0, ct[c]));
                    double td = Math.Acos(cth) * 180.0 / Math.PI;
                    E[c] = ValutaFeed(p, fp, q, tab, td, 0, k, d, true) / RHO[c];
                }
                return E;
            };
            Func<Complex[], double> efficienza = E =>
            {
                Complex s = Complex.Zero;
                for (int c = 0; c < nc; c++) s += E[c];
                s *= dA;
                return s.Magnitude * s.Magnitude / (Aap * pTot);
            };

            // centro di fase (solo con la fase): massimo dell'efficienza alla mira della bisettrice
            double dUsato = 0;
            if (r.FaseUsata)
            {
                var ct0 = cosTh(0.0);
                var E0 = campo(ct0, 0.0, false);
                Func<double, double> effD = d =>
                {
                    Complex s = Complex.Zero;
                    for (int c = 0; c < nc; c++) s += E0[c] * Complex.FromPolarCoordinates(1.0, -k * d * (ct0[c] - 1.0));
                    return s.Magnitude;
                };
                double bd = 0, bv = double.MinValue;
                for (double d = -2 * lam; d <= 2 * lam + 1e-12; d += lam / 50)
                {
                    double v = effD(d);
                    if (v > bv) { bv = v; bd = d; }
                }
                double lo = bd - lam / 50, hi = bd + lam / 50;
                for (int it = 0; it < 40; it++)
                {
                    double m1 = lo + (hi - lo) / 3, m2 = hi - (hi - lo) / 3;
                    if (effD(m1) < effD(m2)) lo = m1; else hi = m2;
                }
                dUsato = p.CentroFaseAuto ? 0.5 * (lo + hi) : p.CentroFaseManuale_mm / 1000.0;
                r.CentroFase_mm = dUsato * 1000.0;
            }

            // mira del feed: bisettrice, correzione manuale o ottimo (-10..+10 gradi)
            r.EffMiraZero = efficienza(campo(cosTh(0.0), dUsato, false));
            double bestDelta = 0, bestEff = r.EffMiraZero;
            for (double dd = -10.0; dd <= 10.0 + 1e-9; dd += 0.5)
            {
                double e = efficienza(campo(cosTh(dd * Math.PI / 180.0), dUsato, false));
                if (e > bestEff) { bestEff = e; bestDelta = dd; }
            }
            r.MiraOttima_deg = bestDelta;
            r.MiraUsata_deg = p.OffsetMiraOttima ? bestDelta : p.OffsetCorrezioneMira_deg;
            var ctU = cosTh(r.MiraUsata_deg * Math.PI / 180.0);
            var Eu = campo(ctU, dUsato, false);
            var Eb = campo(ctU, dUsato, true);

            // efficienze
            Complex I1 = Complex.Zero, I1b = Complex.Zero; double Ia = 0, I2 = 0;
            for (int c = 0; c < nc; c++) { I1 += Eu[c]; I1b += Eb[c]; double m = Eu[c].Magnitude; Ia += m; I2 += m * m; }
            I1 *= dA; I1b *= dA; Ia *= dA; I2 *= dA;
            r.EtaSpill = I2 / pTot;
            r.EtaTaper = Ia * Ia / (Aap * I2);
            r.EtaFase = Ia > 0 ? I1.Magnitude * I1.Magnitude / (Ia * Ia) : 1;
            r.EtaBlocco = I1.Magnitude > 0 ? (I1b.Magnitude * I1b.Magnitude) / (I1.Magnitude * I1.Magnitude) : 1;
            r.EtaApertura = r.EtaSpill * r.EtaTaper * r.EtaFase * r.EtaPol;
            r.SigmaTot_mm = p.ErroreRmsCostruttivo_mm;
            r.EtaRuze = Math.Exp(-Math.Pow(4.0 * Math.PI * r.SigmaTot_mm / lamMm, 2.0));
            if (p.SuperficiePiena) { r.ReteTrasmessa = 0; r.EtaRete = 1; }
            else { r.ReteTrasmessa = TrasmissioneRete(p.ReteForo_mm, p.ReteFilo_mm, lamMm); r.EtaRete = 1 - r.ReteTrasmessa; }
            r.EtaVarie = Math.Pow(10.0, -Math.Abs(p.PerditeVarie_dB) / 10.0);
            r.EtaTotale = r.EtaApertura * r.EtaBlocco * r.EtaRuze * r.EtaRete * r.EtaVarie;
            double gId = Math.Pow(Math.PI * D / lam, 2.0);
            r.GuadagnoIdeale_dBi = Db10(gId); r.Guadagno_dBi = Db10(gId * r.EtaTotale);

            // illuminazione ai bordi (feed + attenuazione spaziale) rispetto al centro dell'apertura
            Func<double, double, double> illum = (x, y) =>
            {
                double z = (x * x + y * y) / (4 * F), vz = z - F, rr = Math.Sqrt(x * x + y * y + vz * vz);
                double pa = psiO + r.MiraUsata_deg * Math.PI / 180.0;
                double cth = Math.Max(-1.0, Math.Min(1.0, (y * Math.Sin(pa) - vz * Math.Cos(pa)) / rr));
                return ValutaFeed(p, fp, q, tab, Math.Acos(cth) * 180 / Math.PI, 0, k, 0, false).Magnitude / rr;
            };
            double e0 = illum(0, yc);
            r.IllumBordoInf_dB = 20 * Math.Log10(Math.Max(1e-12, illum(0, y1)) / e0);
            r.IllumBordoSup_dB = 20 * Math.Log10(Math.Max(1e-12, illum(0, y2)) / e0);
            r.IllumBordoLat_dB = 20 * Math.Log10(Math.Max(1e-12, illum(R, yc)) / e0);
            // bocca indicativa di una tromba adatta: interpolazione lineare 1,31 lambda (W2IMU, W1GHZ 6.5) -> 1,88 lambda (tromba di W1GHZ 6.4-2)
            // tra f/D 0,55 e 0,8; solo un ordine di grandezza (OK2AQ usa una tromba piramidale di 1,45 x 1,17 lambda per f/D 0,8)
            r.DiametroFeedStimato_mm = Math.Max(1.0, Math.Min(2.5, 1.31 + (r.FsuDEquivalente - 0.55) * (1.88 - 1.31) / 0.25)) * lamMm;

            // ---------------- diagrammi orizzontale e verticale (somme separabili)
            var Gx = new Complex[Ng]; var Gy = new Complex[Ng];
            for (int c = 0; c < nc; c++) { int i = celle[c] / Ng, j = celle[c] % Ng; Gx[i] += Eb[c]; Gy[j] += Eb[c]; }
            int NT = 600; double thMax = Math.Min(Math.PI / 2, 12.0 * lam / D);
            r.ThetaDeg = new double[2 * NT + 1]; r.PattH_dB = new double[2 * NT + 1]; r.PattV_dB = new double[2 * NT + 1];
            var fH = new double[2 * NT + 1]; var fV = new double[2 * NT + 1];
            for (int t = 0; t <= 2 * NT; t++)
            {
                double th = thMax * (t - NT) / NT, st = Math.Sin(th), ob = (1 + Math.Cos(th)) / 2;
                Complex sh = Complex.Zero, sv = Complex.Zero;
                for (int i = 0; i < Ng; i++)
                {
                    sh += Gx[i] * Complex.FromPolarCoordinates(1.0, k * cx[i] * st);
                    sv += Gy[i] * Complex.FromPolarCoordinates(1.0, k * cx[i] * st);
                }
                r.ThetaDeg[t] = th * 180 / Math.PI; fH[t] = ob * sh.Magnitude; fV[t] = ob * sv.Magnitude;
            }
            for (int t = 0; t <= 2 * NT; t++)
            {
                r.PattH_dB[t] = 20 * Math.Log10(fH[t] / fH[NT] + 1e-15);
                r.PattV_dB[t] = 20 * Math.Log10(fV[t] / fV[NT] + 1e-15);
            }
            double hpd, hpsx, lbd, lbs;
            Mezzo(r.ThetaDeg, r.PattH_dB, NT, +1, out hpd, out lbd); Mezzo(r.ThetaDeg, r.PattH_dB, NT, -1, out hpsx, out lbs);
            r.HpbwH_deg = hpd + hpsx; r.PrimoLoboH_dB = Math.Max(lbd, lbs);
            Mezzo(r.ThetaDeg, r.PattV_dB, NT, +1, out hpd, out lbd); Mezzo(r.ThetaDeg, r.PattV_dB, NT, -1, out hpsx, out lbs);
            r.HpbwV_deg = hpd + hpsx; r.PrimoLoboV_dB = Math.Max(lbd, lbs);
            r.Hpbw_deg = 0.5 * (r.HpbwH_deg + r.HpbwV_deg);
            r.HpbwCoeff = (r.Hpbw_deg * Math.PI / 180.0) / (lam / D);

            // ---------------- EME
            double hOther = p.HpbwCorrispondente_deg > 0 ? p.HpbwCorrispondente_deg : r.Hpbw_deg;
            r.FillLuna = FillLuna(r.Hpbw_deg);
            r.PerditaEco_dB = -Db10(FattoreEcoLuna(r.Hpbw_deg, hOther));
            r.PerditaPuntamento_dB = PerditaPuntamento(p.ErrorePuntamento_deg, r.Hpbw_deg);
            r.Tolleranza05_deg = r.Hpbw_deg * Math.Sqrt(0.5 / 12.0);
            r.Tolleranza1_deg = r.Hpbw_deg * Math.Sqrt(1.0 / 12.0);
            // squint del fascio in polarizzazione circolare (Adatia e Rudge 1975): sin(squint) = lambda sin(psiO) / (4 pi F);
            // destra e sinistra deviano in versi opposti nel piano orizzontale (perpendicolare al piano di simmetria)
            r.Squint_deg = Math.Asin(Math.Min(1.0, lam * Math.Abs(Math.Sin(psiO)) / (4.0 * Math.PI * F))) * 180.0 / Math.PI;

            // ---------------- meccanica: superficie e vento sul piano del bordo
            r.MassaSuperficie_kg = r.AreaSuperficie_m2 * p.ReteMassaSup_kg_m2;
            r.AreaVento_m2 = Math.PI / 4.0 * r.AsseMinore_m * r.AsseMaggiore_m;
            double qd = 0.5 * p.DensitaAria * Math.Pow(p.Vento_kmh / 3.6, 2.0);
            if (p.SuperficiePiena) r.CdRete = CdDiscoPieno;
            else
            {
                double beta = Math.Pow(p.ReteForo_mm / (p.ReteForo_mm + p.ReteFilo_mm), 2.0);
                double K = 1.3 * (1.0 - beta) + Math.Pow(1.0 / beta - 1.0, 2.0);
                double cdp = K <= 4.0 ? K / Math.Pow(1.0 + K / 4.0, 2.0) : 1.0 + (CdPiastraPiana - 1.0) * (1.0 - 4.0 / K);
                r.CdRete = Math.Min(CdDiscoPieno, cdp * CdDiscoPieno / CdPiastraPiana);
            }
            r.ForzaVento_N = qd * r.AreaVento_m2 * r.CdRete;
            r.ForzaVentoGhiaccio_N = qd * r.AreaVento_m2 * CdDiscoPieno;

            // ---------------- stesso disco sulle quattro bande (stessa geometria, stesso feed relativo)
            if (conBande)
                for (int b = 0; b < FreqBande.Length; b++)
                {
                    double lb = C0 / (FreqBande[b] * 1e6), lbm = lb * 1000.0;
                    var bb = new ParabolaBanda { Nome = NomiBande[b], FreqMHz = FreqBande[b], Lambda_mm = lbm };
                    double tr = p.SuperficiePiena ? 0 : TrasmissioneRete(p.ReteForo_mm, p.ReteFilo_mm, lbm);
                    double eRuze = Math.Exp(-Math.Pow(4.0 * Math.PI * r.SigmaTot_mm / lbm, 2.0));
                    bb.PerditaRete_dB = -Db10(1 - tr); bb.PerditaRuze_dB = -Db10(eRuze);
                    bb.ForoSuLambda = p.SuperficiePiena ? 0 : p.ReteForo_mm / lbm;
                    bb.Efficienza = r.EtaApertura * r.EtaBlocco * eRuze * (1 - tr) * r.EtaVarie;
                    bb.Guadagno_dBi = Db10(Math.Pow(Math.PI * D / lb, 2.0) * bb.Efficienza);
                    bb.Hpbw_deg = r.HpbwCoeff * lb / D * 180.0 / Math.PI;
                    double ho = p.HpbwCorrispondente_deg > 0 ? p.HpbwCorrispondente_deg : bb.Hpbw_deg;
                    bb.FillLuna = FillLuna(bb.Hpbw_deg); bb.PerditaEco_dB = -Db10(FattoreEcoLuna(bb.Hpbw_deg, ho));
                    bb.Tolleranza05_deg = bb.Hpbw_deg * Math.Sqrt(0.5 / 12.0); bb.Tolleranza1_deg = bb.Hpbw_deg * Math.Sqrt(1.0 / 12.0);
                    bb.PerditaPuntamento_dB = PerditaPuntamento(p.ErrorePuntamento_deg, bb.Hpbw_deg);
                    var g = new List<string>();
                    if (D / lb < 10) g.Add("disco piccolo in lambda");
                    if (bb.ForoSuLambda > 0.1) g.Add("rete troppo larga");
                    if (r.SigmaTot_mm > lbm / 20.0) g.Add("superficie oltre lambda/20");
                    if (bb.Hpbw_deg < 1.0) g.Add("puntamento entro " + bb.Tolleranza05_deg.ToString("0.00", CI) + " gradi");
                    bb.Giudizio = g.Count == 0 ? "adatto" : string.Join("; ", g);
                    r.Bande.Add(bb);
                }

            AvvisaOffset(p, r);
            return r;
        }

        // meta' fascio a -3 dB e primo lobo su un lato (dir = +1 a destra, -1 a sinistra dello zero centrale)
        static void Mezzo(double[] th, double[] db, int i0, int dir, out double mezzo, out double lobo)
        {
            mezzo = double.NaN; lobo = double.NaN;
            int n = db.Length, i = i0;
            while (i + dir >= 0 && i + dir < n && db[i + dir] >= -3.0) i += dir;
            if (i + dir < 0 || i + dir >= n) return;
            int j = i + dir;
            double t3 = th[i] + (th[j] - th[i]) * (-3.0 - db[i]) / (db[j] - db[i]);
            mezzo = Math.Abs(t3 - th[i0]);
            int m = j;
            while (m + dir >= 0 && m + dir < n && db[m + dir] < db[m]) m += dir;          // fino al primo nullo
            while (m + dir >= 0 && m + dir < n && db[m + dir] >= db[m]) m += dir;         // fino al primo massimo
            lobo = db[m];
        }

        static void AvvisaOffset(ParabolaInput p, OffsetResult r)
        {
            var A = r.Avvisi; double lam = r.Lambda_mm;
            double rb = p.FeedDiametro_m / 2.0;
            if (r.Ibrido || r.Y1_m < rb)
                A.Add(new ParabolaAvviso(r.EtaBlocco < 0.97 ? 1 : 0, string.Format(CI, "Geometria ibrida: il bordo inferiore e' a {0:0} mm dall'asse e il feed (diametro {1:0} mm) {2} l'apertura: perdita per blocco {3:0.00} dB (OE5JFL: offset vero solo con il fuoco non piu' alto del bordo inferiore).",
                    r.Y1_m * 1000, p.FeedDiametro_m * 1000, r.EtaBlocco < 0.999 ? "copre in parte" : "sfiora", -Db10(r.EtaBlocco))));
            if (p.Diametro_m * 1000.0 / lam < 10.0)
                A.Add(new ParabolaAvviso(1, string.Format(CI, "Disco piccolo in lambda ({0:0.0} lambda): il feed per f/D equivalente {1:0.00} e' grande (bocca dual-mode circa {2:0} mm) rispetto al disco; a 1296 MHz l'offset si usa sui dischi grandi (OK1DFC 8 m) o con un feed allungato a tromba (OK2AQ, 1,8 m con f/D 0,8).",
                    p.Diametro_m * 1000.0 / lam, r.FsuDEquivalente, r.DiametroFeedStimato_mm)));
            if (Math.Abs(r.IllumBordoInf_dB - r.IllumBordoSup_dB) > 4.0)
                A.Add(new ParabolaAvviso(0, string.Format(CI, "Illuminazione asimmetrica: {0:0.0} dB al bordo inferiore, {1:0.0} dB al bordo superiore (attenuazione spaziale diversa). Puntamento migliore del feed: {2:+0.0;-0.0;0} gradi rispetto alla bisettrice (efficienza {3:0.0}% contro {4:0.0}%).",
                    r.IllumBordoInf_dB, r.IllumBordoSup_dB, r.MiraOttima_deg, 100 * r.EtaApertura, 100 * r.EffMiraZero * r.EtaPol)));
            if (!p.SuperficiePiena && p.ReteForo_mm / lam > 0.1)
                A.Add(new ParabolaAvviso(1, string.Format(CI, "Rete: foro {0:0.0} mm = lambda/{1:0.0}; la regola pratica e' lambda/10 o meno (perdita {2:0.00} dB). Attenzione ai dischi TV in rete forata.", p.ReteForo_mm, lam / p.ReteForo_mm, -Db10(r.EtaRete))));
            if (r.SigmaTot_mm > lam / 20.0)
                A.Add(new ParabolaAvviso(r.SigmaTot_mm > lam / 16.0 ? 2 : 1, string.Format(CI, "Superficie: {0:0.0} mm RMS = lambda/{1:0} -> perdita Ruze {2:0.0} dB; obiettivo lambda/20 = {3:0.0} mm.", r.SigmaTot_mm, lam / r.SigmaTot_mm, -Db10(r.EtaRuze), lam / 20.0)));
            if (r.Hpbw_deg < LunaDiametro_deg * 1.5)
                A.Add(new ParabolaAvviso(2, string.Format(CI, "EME: fascio {0:0.00} gradi contro una Luna di {1:0.00}: perdita sull'eco {2:0.0} dB rispetto alla sorgente puntiforme.", r.Hpbw_deg, LunaDiametro_deg, r.PerditaEco_dB)));
            if (r.Hpbw_deg < 1.0)
                A.Add(new ParabolaAvviso(2, string.Format(CI, "Puntamento: per perdere meno di 0,5 dB serve un errore entro {0:0.00} gradi (1 dB: {1:0.00}).", r.Tolleranza05_deg, r.Tolleranza1_deg)));
            if (r.Squint_deg > 0.05 * r.Hpbw_deg)
                A.Add(new ParabolaAvviso(r.Squint_deg > 0.1 * r.Hpbw_deg ? 1 : 0, string.Format(CI, "Polarizzazione circolare: il fascio si sposta di {0:0.000} gradi di lato, in versi opposti per RHCP e LHCP (Adatia-Rudge): tra trasmissione e ricezione EME sono {1:0.000} gradi, {2:0}% dell'HPBW. Con la polarizzazione lineare c'e' invece polarizzazione incrociata nel piano orizzontale (Chu-Turrin).",
                    r.Squint_deg, 2 * r.Squint_deg, 200 * r.Squint_deg / r.Hpbw_deg)));
            A.Add(new ParabolaAvviso(0, string.Format(CI, "Con il piano del bordo verticale il fascio punta {0:0.0} gradi sopra l'orizzonte: l'elevazione meccanica e' quella del fascio meno {0:0.0} gradi.", r.Inclinazione_deg)));
            if (r.FaseUsata)
                A.Add(new ParabolaAvviso(0, string.Format(CI, "Centro di fase {0:+0.0;-0.0} mm dall'origine del modello del feed (+ verso il disco): e' il punto da mettere nel fuoco.", r.CentroFase_mm)));
        }

        public static string ProfiliOffsetCsv(OffsetResult r, char sep = ';')
        {
            var it = new CultureInfo("it-IT");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("sezione" + sep + "s_mm" + sep + "x_mm" + sep + "profondita_mm");
            foreach (var pt in r.ProfiloVerticale) sb.AppendLine(string.Format(it, "verticale{3}{0:0.0}{3}{1:0.0}{3}{2:0.0}", pt.S_m * 1000, pt.X_m * 1000, pt.Profondita_m * 1000, sep));
            foreach (var pt in r.ProfiloOrizzontale) sb.AppendLine(string.Format(it, "orizzontale{3}{0:0.0}{3}{1:0.0}{3}{2:0.0}", pt.S_m * 1000, pt.X_m * 1000, pt.Profondita_m * 1000, sep));
            return sb.ToString();
        }
    }
}
