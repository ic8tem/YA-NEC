// YA-NEC - Calcolatore "Trombe e illuminatori": dimensionamento per banda, diagramma d'apertura, modello NEC a griglia.
// Tipi: coffee-can, VE4MA, Super-VE4MA, Chaparral, W2IMU dual-mode, tromba conica, tromba piramidale.
// Fonti delle quote: W1GHZ, Microwave Antenna Book cap. 6.3-6.5 e "VE4MA and Chaparral feeds with Septum Polarizers" (2006);
// Balanis, Antenna Theory, par. 13.4.3 (tromba piramidale a guadagno ottimo) e 13.6 (tromba conica).
// Diagrammi: integrazione dell'apertura (Huygens, fattore di Chu con beta/k del modo TE11, errore di fase quadratico).
//  - TE11 in guida circolare: Ey = J0(x) - J2(x) cos 2phi, Ex = J2(x) sin 2phi, x = 1,8412 rho/a;
//  - dual-mode: + beta (J0(x2) + J2(x2) cos 2phi), x2 = 3,8317 rho/a, beta per piani E e H uguali a -10 dB (Potter 1963);
//  - integrali in phi chiusi con le Bessel: piano H 2pi[A J0(u) - B J2(u)], piano E 2pi[A J0(u) + B J2(u)], u = k rho sin(theta).
// Collaudo: verifica/rif_trombe.py e verifica/rif_diagrammi.py (integrazione 2D diretta).
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public enum TipoTromba { CoffeeCan = 0, VE4MA = 1, SuperVE4MA = 2, Chaparral = 3, W2IMU = 4, Conica = 5, Piramidale = 6 }
    public enum ProgettoTromba { PerFsuD = 0, PerThetaOffset = 1, PerGuadagno = 2, MisureLibere = 3 }

    public class TrombaInput
    {
        public TipoTromba Tipo = TipoTromba.W2IMU;
        public ProgettoTromba Progetto = ProgettoTromba.PerFsuD;
        public double FreqMHz = 1296.1;
        public double FsuD = 0.55;                 // primo fuoco
        public double ThetaStar_deg = 38.8;        // offset: semiangolo dei bordi visti dal fuoco
        public double Taper_dB = 10.0;             // per f/D: taper totale al bordo; per offset: livello del solo feed a theta*
        public double Guadagno_dBi = 15.0;         // per guadagno (conica, piramidale)
        // misure libere (in lambda): bocca, lunghezza obliqua o assiale della svasatura
        public double Bocca_lambda = 1.31, BoccaB_lambda = 1.0, Svasatura_lambda = 3.0;
        public double Ingresso_lambda = 0.70;      // guida d'ingresso circolare (W2IMU, conica): solo TE11 tra 0,586 e 0,766 lambda
        public double GuidaA_mm = 0, GuidaB_mm = 0;  // piramidale: guida d'ingresso (0 = guida WR standard della banda)
        // modello NEC
        public double Maglia_lambda = 0.10;        // lato della maglia
        public bool Circolare = false;             // due dipoli incrociati a 90 gradi (polarizzatore ideale)
        public double DiametroParabola_m = 0;      // per l'efficienza sulla parabola (0 = 20 lambda)
    }

    public class TrombaQuota { public string Nome, Nota; public double Mm; public TrombaQuota(string n, double mm, string nota) { Nome = n; Mm = mm; Nota = nota; } }
    public class TrombaModo { public string Sezione, Modo; public double Taglio_MHz; public bool Propaga; }
    public class TrombaStazione { public double Z_m, A_m, B_m; }   // circolare: A = B = raggio; rettangolare: semilati

    public class TrombaResult
    {
        public double Lambda_mm, LambdaG_mm;
        public bool Rettangolare, Analitico;
        public List<TrombaQuota> Quote = new List<TrombaQuota>();
        public List<TrombaModo> Modi = new List<TrombaModo>();
        public List<TrombaStazione> Profilo = new List<TrombaStazione>();
        public List<double[]> Anelli = new List<double[]>();   // VE4MA/Chaparral: {z dietro, z davanti, r interno, r esterno}
        public double DiametroEsterno_m, Lunghezza_m;
        // diagramma (0..180 gradi, passo 1): null per VE4MA e Chaparral (solo dal NEC)
        public double[] Th, DbE, DbH, FaseE, FaseH;
        public double Theta3E, Theta3H, Theta10E, Theta10H, Guadagno_dBi, EtaApertura, Beta;
        public double LivelloObiettivo_dB, AngoloObiettivo_deg;
        // sulla parabola (dalla pagina Feed della parabola)
        public double FsuDUsata, EffAllaFsuD, FsuDOttima, EffOttima, CentroFase_mm = double.NaN;
        public double[] CurvaFds, CurvaEff;
        public double PubFsuD, PubEff, PubEffMisurata;   // valori pubblicati (VE4MA, Chaparral)
        // NEC
        public string Nec; public int NecSegmenti, NecFili, NecNodiSciolti; public double NecMemoria_MB, NecRaggioFilo_mm;
        public List<ParabolaAvviso> Avvisi = new List<ParabolaAvviso>();
        public string Errore;
    }

    public static class Tromba
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        public const double X11 = 1.8412, X01 = 2.4048, X21 = 3.0542, XM11 = 3.8317, X12 = 5.3314;
        public const double BetaDualMode = 0.487;   // TM11/TE11 nella normalizzazione Ey(0) = 1: piani E e H uguali (verifica/beta2.py)

        // guide WR standard per le quattro bande (a x b in mm)
        static readonly double[] FreqWR = { 1296.1, 2320.1, 5760.1, 10368.1 };
        static readonly string[] NomeWR = { "WR650", "WR430", "WR159", "WR90" };
        static readonly double[] AWR = { 165.10, 109.22, 40.386, 22.86 }, BWR = { 82.55, 54.61, 20.193, 10.16 };

        // ------------------------------------------------------------------ Bessel (Abramowitz-Stegun 9.4.1-9.4.6)
        public static double J0(double x) { return Parabola.BesselJ0(x); }
        public static double J1(double x)
        {
            double ax = Math.Abs(x), r;
            if (ax <= 3.0)
            {
                double y = (ax / 3.0) * (ax / 3.0);
                r = ax * (0.5 + y * (-0.56249985 + y * (0.21093573 + y * (-0.03954289 + y * (0.00443319 + y * (-0.00031761 + y * 0.00001109))))));
            }
            else
            {
                double z = 3.0 / ax;
                double f1 = 0.79788456 + z * (0.00000156 + z * (0.01659667 + z * (0.00017105 + z * (-0.00249511 + z * (0.00113653 + z * -0.00020033)))));
                double t1 = ax - 2.35619449 + z * (0.12499612 + z * (0.00005650 + z * (-0.00637879 + z * (0.00074348 + z * (0.00079824 + z * -0.00029166)))));
                r = f1 * Math.Cos(t1) / Math.Sqrt(ax);
            }
            return x < 0 ? -r : r;
        }
        public static double J2(double x) { return Math.Abs(x) < 1e-3 ? x * x / 8.0 : 2.0 * J1(x) / x - J0(x); }

        static double GuidaLambda(double lam, double lc) { double s = 1.0 - (lam / lc) * (lam / lc); return s > 0 ? lam / Math.Sqrt(s) : double.PositiveInfinity; }

        /// <summary>Lunghezza della sezione di fase della W2IMU (W1GHZ cap. 6.5): 3/4 di ciclo tra TE11 e TM11 nella bocca B (in lambda).</summary>
        public static double FaseW2imu_lambda(double B)
        {
            double g1 = GuidaLambda(1.0, 1.706 * B), g2 = GuidaLambda(1.0, 0.82 * B);
            return 0.75 / (1.0 / g1 - 1.0 / g2);
        }

        // ------------------------------------------------------------------ diagrammi d'apertura
        /// <summary>Apertura circolare di raggio a (lambda): modo 0 = TE11, 1 = dual-mode; l = lunghezza obliqua dal vertice
        /// (0 = senza errore di fase); bk = beta/k del TE11 (fattore di Chu). Restituisce (piano E, piano H) a theta.</summary>
        public class ApCircolare
        {
            readonly double a, l, bk, beta; readonly int n = 201;
            readonly double[] r, A, B, C; readonly Complex[] q;
            public double Guadagno, Eta;
            public ApCircolare(double a_l, double l_l, int modo, double bk_, double beta_)
            {
                a = a_l; l = l_l; bk = bk_; beta = modo == 1 ? beta_ : 0.0;
                r = new double[n]; A = new double[n]; B = new double[n]; C = new double[n]; q = new Complex[n];
                for (int i = 0; i < n; i++)
                {
                    r[i] = a * i / (n - 1);
                    double x1 = X11 * r[i] / a, x2 = XM11 * r[i] / a;
                    A[i] = J0(x1) + beta * J0(x2); B[i] = -J2(x1) + beta * J2(x2); C[i] = J2(x1) - beta * J2(x2);
                    q[i] = l > 0 ? Complex.FromPolarCoordinates(1.0, -Math.PI * r[i] * r[i] / l) : Complex.One;
                }
                Complex i0 = Complex.Zero; double p = 0;
                for (int i = 0; i < n; i++)
                {
                    double w = Simpson(i) * r[i];
                    i0 += 2 * Math.PI * A[i] * q[i] * w;
                    p += (2 * Math.PI * A[i] * A[i] + Math.PI * B[i] * B[i] + Math.PI * C[i] * C[i]) * w;
                }
                double h = a / (n - 1) / 3.0; i0 *= h; p *= h;
                Guadagno = 4 * Math.PI * i0.Magnitude * i0.Magnitude / p;
                Eta = Guadagno / Math.Pow(2 * Math.PI * a, 2);
            }
            int Simpson(int i) { return i == 0 || i == n - 1 ? 1 : (i % 2 == 1 ? 4 : 2); }
            public void Valuta(double thDeg, out Complex fe, out Complex fh)
            {
                double t = thDeg * Math.PI / 180.0, s = Math.Sin(t), c = Math.Cos(t);
                Complex se = Complex.Zero, sh = Complex.Zero;
                for (int i = 0; i < n; i++)
                {
                    double u = 2 * Math.PI * r[i] * s, j0 = J0(u), j2 = J2(u), w = Simpson(i) * r[i];
                    se += (A[i] * j0 + B[i] * j2) * q[i] * w;
                    sh += (A[i] * j0 - B[i] * j2) * q[i] * w;
                }
                double h = 2 * Math.PI * a / (n - 1) / 3.0;
                fe = se * h * (bk + c) / (1 + bk);
                fh = sh * h * (1 + bk * c) / (1 + bk);
            }
        }

        /// <summary>Apertura rettangolare a1 x b1 (lambda, a1 lungo x = piano H) con raggi rho_e, rho_h (0 = in fase).</summary>
        public class ApRettangolare
        {
            readonly double a1, b1, re, rh; const int n = 401;
            public double Guadagno, Eta;
            public ApRettangolare(double a1_, double b1_, double re_, double rh_)
            {
                a1 = a1_; b1 = b1_; re = re_; rh = rh_;
                Complex fe, fh; Integrali(0, out fe, out fh);
                Guadagno = 4 * Math.PI * Math.Pow(fe.Magnitude * fh.Magnitude, 2) / (b1 * a1 / 2.0);
                Eta = Guadagno / (4 * Math.PI * a1 * b1);
            }
            void Integrali(double s, out Complex ie, out Complex ih)
            {
                ie = Complex.Zero; ih = Complex.Zero;
                for (int i = 0; i < n; i++)
                {
                    int w = i == 0 || i == n - 1 ? 1 : (i % 2 == 1 ? 4 : 2);
                    double y = -b1 / 2 + b1 * i / (n - 1), x = -a1 / 2 + a1 * i / (n - 1);
                    double fy = (re > 0 ? -Math.PI * y * y / re : 0) + 2 * Math.PI * y * s;
                    double fx = (rh > 0 ? -Math.PI * x * x / rh : 0) + 2 * Math.PI * x * s;
                    ie += w * Complex.FromPolarCoordinates(1.0, fy);
                    ih += w * Math.Cos(Math.PI * x / a1) * Complex.FromPolarCoordinates(1.0, fx);
                }
                ie *= b1 / (n - 1) / 3.0; ih *= a1 / (n - 1) / 3.0;
            }
            public void Valuta(double thDeg, out Complex fe, out Complex fh)
            {
                double t = thDeg * Math.PI / 180.0, ob = (1 + Math.Cos(t)) / 2;
                Complex ie, ih; Integrali(Math.Sin(t), out ie, out ih);
                Complex ie0, ih0; Integrali(0, out ie0, out ih0);
                // piano E (yz): integrale in y per il valore in asse dell'altro; piano H (xz) viceversa
                fe = ob * ie * ih0; fh = ob * ih * ie0;
            }
        }

        // ------------------------------------------------------------------ progetto ottimo di Balanis (piramidale)
        public static void Balanis(double g0dBi, double a, double b, out double chi, out double a1, out double b1, out double re, out double rh, out double pe, out double ph)
        {
            double G0 = Math.Pow(10, g0dBi / 10);
            Func<double, double> F = x => Math.Pow(Math.Sqrt(2 * x) - b, 2) * (2 * x - 1)
                - Math.Pow(G0 / (2 * Math.PI) * Math.Sqrt(3 / (2 * Math.PI)) / Math.Sqrt(x) - a, 2) * (G0 * G0 / (6 * Math.Pow(Math.PI, 3)) / x - 1);
            chi = G0 / (2 * Math.PI * Math.Sqrt(2 * Math.PI));
            for (int it = 0; it < 60; it++)
            {
                double h = 1e-6 * chi, d = (F(chi + h) - F(chi - h)) / (2 * h);
                if (d == 0) break;
                double nx = chi - F(chi) / d;
                if (nx <= 0.5) nx = 0.5 * (chi + 0.5);
                if (Math.Abs(nx - chi) < 1e-12) { chi = nx; break; }
                chi = nx;
            }
            re = chi; rh = G0 * G0 / (8 * Math.Pow(Math.PI, 3)) / chi;
            a1 = Math.Sqrt(3 * rh); b1 = Math.Sqrt(2 * re);
            pe = (b1 - b) * Math.Sqrt(Math.Max(0, (re / b1) * (re / b1) - 0.25));
            ph = (a1 - a) * Math.Sqrt(Math.Max(0, (rh / a1) * (rh / a1) - 0.25));
        }

        // ------------------------------------------------------------------ livello medio dei piani E e H a theta (dB)
        static double Livello(Func<double, Complex[]> f, double th)
        {
            var z = f(0); var v = f(th);
            double p0 = 0.5 * (z[0].Magnitude * z[0].Magnitude + z[1].Magnitude * z[1].Magnitude);
            double p = 0.5 * (v[0].Magnitude * v[0].Magnitude + v[1].Magnitude * v[1].Magnitude);
            return 10 * Math.Log10(Math.Max(p / p0, 1e-30));
        }

        // primo cambio di segno su 60 passi, poi bisezione (evita gli incroci sui lobi secondari)
        static double Bisezione(Func<double, double> g, double lo, double hi, out bool fuori)
        {
            fuori = false;
            double a0 = lo, g0 = g(lo), best = lo, gBest = Math.Abs(g0);
            bool trovato = false;
            for (int i = 1; i <= 60; i++)
            {
                double x = lo + (hi - lo) * i / 60.0, gx = g(x);
                if (Math.Abs(gx) < gBest) { gBest = Math.Abs(gx); best = x; }
                if (g0 * gx <= 0) { hi = x; lo = a0; trovato = true; break; }
                a0 = x; g0 = gx;
            }
            if (!trovato) { fuori = true; return best; }
            double glo = g(lo);
            for (int i = 0; i < 50; i++)
            {
                double m = 0.5 * (lo + hi), gm = g(m);
                if (gm * glo > 0) { lo = m; glo = gm; } else hi = m;
            }
            return 0.5 * (lo + hi);
        }

        // ------------------------------------------------------------------ calcolo principale
        public static TrombaResult Calcola(TrombaInput p) { return Calcola(p, true); }

        public static TrombaResult Calcola(TrombaInput p, bool conParabola)
        {
            var r = new TrombaResult();
            if (p.FreqMHz <= 0) { r.Errore = "Frequenza non valida."; return r; }
            double lam = Parabola.C0 / (p.FreqMHz * 1e6), L = lam * 1000.0;   // L = lambda in mm
            r.Lambda_mm = L;

            // obiettivo d'illuminazione
            double fd = p.Progetto == ProgettoTromba.PerThetaOffset ? 1.0 / (4.0 * Math.Tan(p.ThetaStar_deg * Math.PI / 360.0)) : p.FsuD;
            double psi0 = p.Progetto == ProgettoTromba.PerThetaOffset ? p.ThetaStar_deg : 2 * Math.Atan(1.0 / (4.0 * fd)) * 180 / Math.PI;
            double spazio = 20 * Math.Log10(2.0 / (1 + Math.Cos(psi0 * Math.PI / 180)));
            double obiettivo = p.Progetto == ProgettoTromba.PerThetaOffset ? -Math.Abs(p.Taper_dB) : -(Math.Abs(p.Taper_dB) - spazio);
            r.LivelloObiettivo_dB = obiettivo; r.AngoloObiettivo_deg = psi0; r.FsuDUsata = fd;
            bool perIllum = p.Progetto == ProgettoTromba.PerFsuD || p.Progetto == ProgettoTromba.PerThetaOffset;

            Func<double, Complex[]> diag = null;
            double gain = double.NaN, eta = double.NaN;
            double maglia = Math.Max(0.05, Math.Min(0.25, p.Maglia_lambda));
            var nec = new NecGriglia(maglia * lam);
            double zBack = 0, dipZ = 0, dipH = 0; bool dipoloX = false;
            bool fuori = false;

            switch (p.Tipo)
            {
                case TipoTromba.CoffeeCan:
                case TipoTromba.VE4MA:
                case TipoTromba.SuperVE4MA:
                case TipoTromba.Chaparral:
                    {
                        double d;   // diametro in lambda
                        if (p.Tipo == TipoTromba.CoffeeCan)
                        {
                            Func<double, ApCircolare> ap = dd => new ApCircolare(dd / 2, 0, 0, Math.Sqrt(Math.Max(0.01, 1 - Math.Pow(X11 / (Math.PI * dd), 2))), 0);
                            if (perIllum)
                                d = Bisezione(dd => { var a = ap(dd); return Livello(t => Coppia(a, t), psi0) - obiettivo; }, 0.60, 0.95, out fuori);
                            else d = Math.Max(0.59, p.Bocca_lambda);
                            var aa = ap(d); diag = t => Coppia(aa, t); gain = 10 * Math.Log10(aa.Guadagno); eta = aa.Eta;
                            r.Analitico = true;
                        }
                        else
                        {
                            d = p.Tipo == TipoTromba.VE4MA ? 0.77 : 0.71;
                            r.PubFsuD = p.Tipo == TipoTromba.VE4MA ? 0.32 : p.Tipo == TipoTromba.SuperVE4MA ? 0.37 : 0.36;
                            r.PubEff = p.Tipo == TipoTromba.VE4MA ? 0.71 : p.Tipo == TipoTromba.SuperVE4MA ? 0.788 : 0.762;
                            r.PubEffMisurata = p.Tipo == TipoTromba.VE4MA ? 0.58 : p.Tipo == TipoTromba.SuperVE4MA ? 0.65 : 0.60;
                        }
                        double R = d / 2 * lam, lg = GuidaLambda(lam, Math.PI * d * lam / X11);
                        r.LambdaG_mm = lg * 1000;
                        double len = 1.25 * lg;
                        zBack = -len; dipZ = zBack + lg / 4; dipH = Math.Min(0.23 * lam, 0.8 * R);
                        r.Profilo.Add(new TrombaStazione { Z_m = zBack, A_m = R, B_m = R });
                        r.Profilo.Add(new TrombaStazione { Z_m = 0, A_m = R, B_m = R });
                        r.Quote.Add(new TrombaQuota("Diametro interno della guida", d * L, string.Format(CI, "{0:0.000} lambda", d)));
                        r.Quote.Add(new TrombaQuota("Lunghezza dal fondo alla bocca", len * 1000, "1,25 lambda guida (W1GHZ: oltre una lambda guida, non critica)"));
                        r.Quote.Add(new TrombaQuota("Sonda dal fondo", lg / 4 * 1000, "lambda guida / 4"));
                        r.Quote.Add(new TrombaQuota("Lunghezza della sonda (coassiale)", 0.23 * L, "circa lambda/4, da tarare per il ROS"));
                        r.DiametroEsterno_m = d * lam;
                        var mod = new List<double[]>();
                        if (p.Tipo == TipoTromba.VE4MA || p.Tipo == TipoTromba.SuperVE4MA)
                        {
                            double w = p.Tipo == TipoTromba.VE4MA ? 0.50 : 0.60, prof = p.Tipo == TipoTromba.VE4MA ? 0.50 : 0.45, dietro = 0.15;
                            mod.Add(new[] { -(dietro + prof) * lam, -dietro * lam, R, R + w * lam });
                            r.Quote.Add(new TrombaQuota("Anello (choke): larghezza radiale", w * L, string.Format(CI, "{0:0.00} lambda", w)));
                            r.Quote.Add(new TrombaQuota("Anello: profondita'", prof * L, string.Format(CI, "{0:0.00} lambda", prof)));
                            r.Quote.Add(new TrombaQuota("Anello: bordo dietro la bocca", dietro * L, "0,15 lambda"));
                            r.Quote.Add(new TrombaQuota("Diametro esterno dell'anello", (d + 2 * w) * L, "ombra sul disco"));
                            r.DiametroEsterno_m = (d + 2 * w) * lam;
                        }
                        else if (p.Tipo == TipoTromba.Chaparral)
                        {
                            double w = 0.20, prof = 0.33, dietro = 0.25;
                            for (int k = 0; k < 3; k++) mod.Add(new[] { -(dietro + prof) * lam, -dietro * lam, R + k * w * lam, R + (k + 1) * w * lam });
                            r.Quote.Add(new TrombaQuota("Tre anelli: larghezza di ciascuno", w * L, "0,20 lambda"));
                            r.Quote.Add(new TrombaQuota("Anelli: profondita'", prof * L, "0,33 lambda"));
                            r.Quote.Add(new TrombaQuota("Anelli: bordo dietro la bocca", dietro * L, "0,25 lambda"));
                            r.Quote.Add(new TrombaQuota("Diametro esterno", (d + 6 * w) * L, "ombra sul disco (1,91 lambda)"));
                            r.DiametroEsterno_m = (d + 6 * w) * lam;
                        }
                        r.Anelli = mod;
                        ModiCircolari(r, "guida", d * lam, lam);
                        nec.TuboCircolare(r.Profilo, true, mod);
                        break;
                    }
                case TipoTromba.W2IMU:
                    {
                        double B;
                        if (perIllum)
                        {
                            Func<double, ApCircolare> ap = bb => new ApCircolare(bb / 2, 0, 1, 1.0, BetaDualMode);
                            B = Bisezione(bb => { var a = ap(bb); return Livello(t => Coppia(a, t), psi0) - obiettivo; }, 1.15, 2.0, out fuori);
                        }
                        else B = p.Bocca_lambda;
                        double A = p.Ingresso_lambda;
                        var aw = new ApCircolare(B / 2, 0, 1, 1.0, BetaDualMode);
                        diag = t => Coppia(aw, t); gain = 10 * Math.Log10(aw.Guadagno); eta = aw.Eta; r.Analitico = true; r.Beta = BetaDualMode;
                        double alfa = 44.6 / B, C = FaseW2imu_lambda(B);
                        double lgA = GuidaLambda(lam, Math.PI * A * lam / X11);
                        r.LambdaG_mm = lgA * 1000;
                        double flare = (B - A) / 2 / Math.Tan(alfa * Math.PI / 180) * lam;
                        double zC = -C * lam, zF = zC - flare, zB = zF - 0.75 * lgA;
                        zBack = zB; dipZ = zB + lgA / 4; dipH = Math.Min(0.23 * lam, 0.8 * A / 2 * lam);
                        r.Profilo.Add(new TrombaStazione { Z_m = zB, A_m = A / 2 * lam, B_m = A / 2 * lam });
                        r.Profilo.Add(new TrombaStazione { Z_m = zF, A_m = A / 2 * lam, B_m = A / 2 * lam });
                        r.Profilo.Add(new TrombaStazione { Z_m = zC, A_m = B / 2 * lam, B_m = B / 2 * lam });
                        r.Profilo.Add(new TrombaStazione { Z_m = 0, A_m = B / 2 * lam, B_m = B / 2 * lam });
                        r.Quote.Add(new TrombaQuota("Diametro della bocca B", B * L, string.Format(CI, "{0:0.000} lambda (W1GHZ: B circa 2,35 f/D = {1:0.00})", B, 2.35 * fd)));
                        r.Quote.Add(new TrombaQuota("Diametro d'ingresso A", A * L, string.Format(CI, "{0:0.000} lambda: solo TE11", A)));
                        r.Quote.Add(new TrombaQuota("Semiangolo della svasatura", alfa, "gradi (non mm): 44,6 / B"));
                        r.Quote.Add(new TrombaQuota("Lunghezza della svasatura", flare * 1000, "lungo l'asse"));
                        r.Quote.Add(new TrombaQuota("Sezione di fase C", C * lam * 1000, string.Format(CI, "{0:0.000} lambda: TE11 e TM11 in fase alla bocca", C)));
                        r.Quote.Add(new TrombaQuota("Guida d'ingresso dal fondo alla svasatura", 0.75 * lgA * 1000, "0,75 lambda guida"));
                        r.Quote.Add(new TrombaQuota("Sonda dal fondo", lgA / 4 * 1000, "lambda guida / 4"));
                        r.Quote.Add(new TrombaQuota("Lunghezza totale", -zB * 1000, ""));
                        r.DiametroEsterno_m = B * lam;
                        ModiCircolari(r, "ingresso A", A * lam, lam);
                        ModiCircolari(r, "bocca B", B * lam, lam);
                        if (B < 1.22) r.Avvisi.Add(new ParabolaAvviso(2, string.Format(CI, "Bocca {0:0.00} lambda: sotto 1,22 lambda il TM11 non si propaga e la tromba non e' piu' dual-mode (W1GHZ).", B)));
                        if (B > 1.7) r.Avvisi.Add(new ParabolaAvviso(1, string.Format(CI, "Bocca {0:0.00} lambda: oltre 1,7 lambda si propaga anche il TE12 (Turrin ha usato 1,86 lambda).", B)));
                        if (A < 0.586 || A > 0.766) r.Avvisi.Add(new ParabolaAvviso(2, string.Format(CI, "Ingresso {0:0.000} lambda: deve stare tra 0,586 (taglio TE11) e 0,766 lambda (taglio TM01).", A)));
                        nec.TuboCircolare(r.Profilo, true, null);
                        break;
                    }
                case TipoTromba.Conica:
                    {
                        double A = p.Ingresso_lambda, d;
                        Func<double, double> lOtt = dd => dd * dd / 3.0;   // lunghezza obliqua ottima (Balanis 13.6)
                        Func<double, ApCircolare> ap = dd => new ApCircolare(dd / 2, lOtt(dd), 0, 1.0, 0);
                        double lObl;
                        if (perIllum) { d = Bisezione(dd => { var a = ap(dd); return Livello(t => Coppia(a, t), psi0) - obiettivo; }, 0.8, 8.0, out fuori); lObl = lOtt(d); }
                        else if (p.Progetto == ProgettoTromba.PerGuadagno) { d = Bisezione(dd => 10 * Math.Log10(ap(dd).Guadagno) - p.Guadagno_dBi, 0.8, 15.0, out fuori); lObl = lOtt(d); }
                        else { d = p.Bocca_lambda; lObl = Math.Max(d / 2 + 0.01, p.Svasatura_lambda); }
                        var ac = new ApCircolare(d / 2, lObl, 0, 1.0, 0);
                        diag = t => Coppia(ac, t); gain = 10 * Math.Log10(ac.Guadagno); eta = ac.Eta; r.Analitico = true;
                        double alfa = Math.Asin(Math.Min(1.0, d / 2 / lObl)), l0 = A / 2 / Math.Sin(alfa);
                        double cono = Math.Max(0, (lObl - l0) * Math.Cos(alfa)) * lam;
                        double lgA = GuidaLambda(lam, Math.PI * A * lam / X11);
                        r.LambdaG_mm = lgA * 1000;
                        double zF = -cono, zB = zF - 0.75 * lgA;
                        zBack = zB; dipZ = zB + lgA / 4; dipH = Math.Min(0.23 * lam, 0.8 * A / 2 * lam);
                        r.Profilo.Add(new TrombaStazione { Z_m = zB, A_m = A / 2 * lam, B_m = A / 2 * lam });
                        r.Profilo.Add(new TrombaStazione { Z_m = zF, A_m = A / 2 * lam, B_m = A / 2 * lam });
                        r.Profilo.Add(new TrombaStazione { Z_m = 0, A_m = d / 2 * lam, B_m = d / 2 * lam });
                        r.Quote.Add(new TrombaQuota("Diametro della bocca", d * L, string.Format(CI, "{0:0.000} lambda", d)));
                        r.Quote.Add(new TrombaQuota("Lunghezza obliqua dal vertice", lObl * L, p.Progetto == ProgettoTromba.MisureLibere ? "impostata" : "ottima: d^2 / (3 lambda)"));
                        r.Quote.Add(new TrombaQuota("Semiangolo del cono", alfa * 180 / Math.PI, "gradi (non mm)"));
                        r.Quote.Add(new TrombaQuota("Lunghezza del cono (assiale)", cono * 1000, ""));
                        r.Quote.Add(new TrombaQuota("Diametro d'ingresso", A * L, string.Format(CI, "{0:0.000} lambda", A)));
                        r.Quote.Add(new TrombaQuota("Guida d'ingresso dal fondo al cono", 0.75 * lgA * 1000, "0,75 lambda guida"));
                        r.Quote.Add(new TrombaQuota("Sonda dal fondo", lgA / 4 * 1000, "lambda guida / 4"));
                        r.DiametroEsterno_m = d * lam;
                        ModiCircolari(r, "ingresso", A * lam, lam);
                        if (cono <= 0) r.Avvisi.Add(new ParabolaAvviso(1, "Bocca piu' piccola dell'ingresso: la tromba si riduce a una guida aperta."));
                        nec.TuboCircolare(r.Profilo, true, null);
                        break;
                    }
                default:   // piramidale
                    {
                        r.Rettangolare = true;
                        double ga = p.GuidaA_mm, gb = p.GuidaB_mm; string nomeWR = "impostata";
                        if (ga <= 0 || gb <= 0)
                        {
                            int best = 0;
                            for (int i = 1; i < FreqWR.Length; i++) if (Math.Abs(Math.Log(FreqWR[i] / p.FreqMHz)) < Math.Abs(Math.Log(FreqWR[best] / p.FreqMHz))) best = i;
                            ga = AWR[best]; gb = BWR[best]; nomeWR = NomeWR[best];
                            if (Math.Abs(Math.Log(FreqWR[best] / p.FreqMHz)) > 0.2) { ga = 0.75 * L; gb = 0.375 * L; nomeWR = "0,75 x 0,375 lambda"; }
                        }
                        double a = ga / L, b = gb / L, a1, b1, re, rh, pp;
                        if (p.Progetto == ProgettoTromba.PerGuadagno)
                        {
                            double chi, pe, ph;
                            Balanis(p.Guadagno_dBi, a, b, out chi, out a1, out b1, out re, out rh, out pe, out ph);
                            pp = 0.5 * (pe + ph);
                        }
                        else if (perIllum)
                        {
                            // piano E (b1) e piano H (a1) a parte; prima con le relazioni dell'ottimo, poi con la stessa lunghezza pp
                            double pFix = 0;
                            Func<double, double> reDi = bb => pFix <= 0 ? bb * bb / 2.0 : bb * Math.Sqrt(Math.Pow(pFix / Math.Max(1e-6, bb - b), 2) + 0.25);
                            Func<double, double> rhDi = aa => pFix <= 0 ? aa * aa / 3.0 : aa * Math.Sqrt(Math.Pow(pFix / Math.Max(1e-6, aa - a), 2) + 0.25);
                            a1 = a; b1 = b;
                            for (int it = 0; it < 3; it++)
                            {
                                bool f1, f2;
                                b1 = Bisezione(bb => LivelloPiano(bb, reDi(bb), true, psi0) - obiettivo, Math.Max(b, 0.3), 8.0, out f1);
                                a1 = Bisezione(aa => LivelloPiano(aa, rhDi(aa), false, psi0) - obiettivo, Math.Max(a, 0.5), 8.0, out f2);
                                fuori = f1 || f2;
                                double reO = reDi(b1), rhO = rhDi(a1);
                                double pe = (b1 - b) * Math.Sqrt(Math.Max(0, (reO / b1) * (reO / b1) - 0.25));
                                double ph = (a1 - a) * Math.Sqrt(Math.Max(0, (rhO / a1) * (rhO / a1) - 0.25));
                                if (it == 0) pFix = Math.Max(pe, ph);
                            }
                            pp = pFix; re = reDi(b1); rh = rhDi(a1);
                        }
                        else
                        {
                            a1 = p.Bocca_lambda; b1 = p.BoccaB_lambda; pp = p.Svasatura_lambda;
                            re = b1 * Math.Sqrt(Math.Pow(pp / Math.Max(1e-6, b1 - b), 2) + 0.25);
                            rh = a1 * Math.Sqrt(Math.Pow(pp / Math.Max(1e-6, a1 - a), 2) + 0.25);
                        }
                        var arr = new ApRettangolare(a1, b1, re, rh);
                        diag = t => { Complex fe, fh; arr.Valuta(t, out fe, out fh); return new[] { fe, fh }; };
                        gain = 10 * Math.Log10(arr.Guadagno); eta = arr.Eta; r.Analitico = true;
                        double lg = GuidaLambda(lam, 2 * ga / 1000.0);
                        r.LambdaG_mm = lg * 1000;
                        double zF = -pp * lam, zB = zF - 0.75 * lg;
                        zBack = zB; dipZ = zB + lg / 4; dipH = Math.Min(0.23 * lam, 0.4 * gb / 1000.0);
                        r.Profilo.Add(new TrombaStazione { Z_m = zB, A_m = ga / 2000, B_m = gb / 2000 });
                        r.Profilo.Add(new TrombaStazione { Z_m = zF, A_m = ga / 2000, B_m = gb / 2000 });
                        r.Profilo.Add(new TrombaStazione { Z_m = 0, A_m = a1 * lam / 2, B_m = b1 * lam / 2 });
                        r.Quote.Add(new TrombaQuota("Bocca, lato H (a1, largo)", a1 * L, string.Format(CI, "{0:0.000} lambda", a1)));
                        r.Quote.Add(new TrombaQuota("Bocca, lato E (b1, stretto)", b1 * L, string.Format(CI, "{0:0.000} lambda", b1)));
                        r.Quote.Add(new TrombaQuota("Lunghezza della svasatura (assiale)", pp * L, "pe = ph: la tromba si puo' costruire"));
                        r.Quote.Add(new TrombaQuota("Raggio piano E (rho_e)", re * L, "dal vertice alla bocca"));
                        r.Quote.Add(new TrombaQuota("Raggio piano H (rho_h)", rh * L, ""));
                        r.Quote.Add(new TrombaQuota("Guida d'ingresso a x b", ga, string.Format(CI, "x {0:0.00} mm ({1})", gb, nomeWR)));
                        r.Quote.Add(new TrombaQuota("Guida dal fondo alla svasatura", 0.75 * lg * 1000, "0,75 lambda guida"));
                        r.Quote.Add(new TrombaQuota("Sonda dal fondo", lg / 4 * 1000, "lambda guida / 4"));
                        r.DiametroEsterno_m = Math.Sqrt(a1 * b1 * 4 / Math.PI) * lam;
                        double fc10 = Parabola.C0 / (2 * ga / 1000.0) / 1e6, fc20 = 2 * fc10, fc01 = Parabola.C0 / (2 * gb / 1000.0) / 1e6;
                        r.Modi.Add(new TrombaModo { Sezione = "guida", Modo = "TE10", Taglio_MHz = fc10, Propaga = p.FreqMHz > fc10 });
                        r.Modi.Add(new TrombaModo { Sezione = "guida", Modo = "TE20", Taglio_MHz = fc20, Propaga = p.FreqMHz > fc20 });
                        r.Modi.Add(new TrombaModo { Sezione = "guida", Modo = "TE01", Taglio_MHz = fc01, Propaga = p.FreqMHz > fc01 });
                        nec.TuboRettangolare(r.Profilo);
                        break;
                    }
            }
            if (p.Progetto == ProgettoTromba.PerGuadagno && p.Tipo != TipoTromba.Conica && p.Tipo != TipoTromba.Piramidale)
                r.Avvisi.Add(new ParabolaAvviso(1, "Il progetto per guadagno vale per la tromba conica e la piramidale: qui si usano le misure libere o quelle pubblicate."));
            if (fuori) r.Avvisi.Add(new ParabolaAvviso(1, string.Format(CI, "L'obiettivo ({0:0.0} dB a {1:0.0} gradi) e' fuori dal campo di questa tromba: si usa la misura al limite.", obiettivo, psi0)));

            // ---------------- diagramma 0..180 e parametri
            r.Guadagno_dBi = gain; r.EtaApertura = eta;
            if (diag != null)
            {
                r.Th = new double[181]; r.DbE = new double[181]; r.DbH = new double[181]; r.FaseE = new double[181]; r.FaseH = new double[181];
                var z = diag(0);
                for (int i = 0; i <= 180; i++)
                {
                    var v = diag(i);
                    r.Th[i] = i;
                    r.DbE[i] = Math.Max(-80, 20 * Math.Log10(v[0].Magnitude / z[0].Magnitude + 1e-12));
                    r.DbH[i] = Math.Max(-80, 20 * Math.Log10(v[1].Magnitude / z[1].Magnitude + 1e-12));
                    r.FaseE[i] = v[0].Phase * 180 / Math.PI; r.FaseH[i] = v[1].Phase * 180 / Math.PI;
                }
                r.Theta3E = 2 * Angolo(r.DbE, -3); r.Theta3H = 2 * Angolo(r.DbH, -3);
                r.Theta10E = Angolo(r.DbE, -10); r.Theta10H = Angolo(r.DbH, -10);
                if (conParabola)
                {
                    double Dp = p.DiametroParabola_m > 0 ? p.DiametroParabola_m : 20 * lam;
                    var rp = Parabola.Calcola(new ParabolaInput
                    {
                        FreqMHz = p.FreqMHz, Diametro_m = Dp, FsuD = Math.Max(0.2, Math.Min(1.2, fd)), FeedDiametro_m = r.DiametroEsterno_m,
                        FeedPianiThetaDeg = r.Th, FeedPianoDbE = r.DbE, FeedPianoDbH = r.DbH, FeedPianoFaseE = r.FaseE, FeedPianoFaseH = r.FaseH
                    }, true);   // con true: anche l'analisi della pagina Feed (curva in f/D)
                    if (rp.Errore == null && rp.Feed != null)
                    {
                        r.EffAllaFsuD = rp.Feed.TotAttuale; r.FsuDOttima = rp.Feed.FdsMax; r.EffOttima = rp.Feed.TotMax;
                        r.CurvaFds = rp.Feed.Fds; r.CurvaEff = rp.Feed.Tot;
                        if (rp.FaseUsata) r.CentroFase_mm = rp.CentroFase_mm;
                    }
                }
            }
            else r.Avvisi.Add(new ParabolaAvviso(0, string.Format(CI, "Per questa tromba non c'e' un diagramma in forma chiusa: si ricava dal NEC. Valori pubblicati da W1GHZ (HFSS, disco di 20 lambda): f/D migliore {0:0.00}, efficienza {1:0.0}% calcolata, {2:0}% misurata da WD5AGO a 2,3 GHz.",
                r.PubFsuD, r.PubEff * 100, r.PubEffMisurata * 100)));

            // ---------------- modello NEC
            dipoloX = p.Circolare;
            nec.Dipolo(dipZ, dipH, lam, dipoloX);
            r.Lunghezza_m = -zBack;
            r.NecSegmenti = nec.Segmenti; r.NecFili = nec.Fili; r.NecNodiSciolti = nec.NodiSciolti();
            r.NecMemoria_MB = 16.0 * nec.Segmenti * (double)nec.Segmenti / 1048576.0;
            r.NecRaggioFilo_mm = nec.Raggio * 1000;
            r.Nec = nec.Testo(p, r, lam);
            if (r.NecSegmenti > 6000) r.Avvisi.Add(new ParabolaAvviso(1, string.Format(CI, "Modello NEC pesante: {0} segmenti, matrice di {1:0} MB. Allargare la maglia (0,12-0,15 lambda) per una prima prova.", r.NecSegmenti, r.NecMemoria_MB)));
            if (r.NecNodiSciolti > 0) r.Avvisi.Add(new ParabolaAvviso(2, string.Format(CI, "Modello NEC: {0} nodi della griglia non collegati. Segnalare il caso.", r.NecNodiSciolti)));
            return r;
        }

        static Complex[] Coppia(ApCircolare a, double t) { Complex fe, fh; a.Valuta(t, out fe, out fh); return new[] { fe, fh }; }

        // livello (dB) di un solo piano della piramidale: E (true, lato b1) o H (false, lato a1)
        static double LivelloPiano(double lato, double rho, bool pianoE, double th)
        {
            var ar = pianoE ? new ApRettangolare(1.0, lato, rho, 0) : new ApRettangolare(lato, 1.0, 0, rho);
            Complex fe0, fh0, fe, fh;
            ar.Valuta(0, out fe0, out fh0); ar.Valuta(th, out fe, out fh);
            return pianoE ? 20 * Math.Log10(fe.Magnitude / fe0.Magnitude + 1e-15) : 20 * Math.Log10(fh.Magnitude / fh0.Magnitude + 1e-15);
        }

        static double Angolo(double[] db, double liv)
        {
            for (int i = 1; i < db.Length; i++)
                if (db[i] < liv) return (i - 1) + (liv - db[i - 1]) / (db[i] - db[i - 1]);
            return double.NaN;
        }

        static void ModiCircolari(TrombaResult r, string sezione, double diam_m, double lam)
        {
            double f0 = Parabola.C0 / lam / 1e6;
            foreach (var m in new[] { ("TE11", X11), ("TM01", X01), ("TE21", X21), ("TM11", XM11), ("TE12", X12) })
            {
                double fc = m.Item2 * Parabola.C0 / (Math.PI * diam_m) / 1e6;
                r.Modi.Add(new TrombaModo { Sezione = sezione, Modo = m.Item1, Taglio_MHz = fc, Propaga = f0 > fc });
            }
        }
    }

    // ================================================================== griglia di fili per il NEC-2
    /// <summary>Superfici come griglie di fili da un segmento ciascuno (W1GHZ cap. 12; 4nec2: maglia circa lambda/10).
    /// Raggio dei fili con la regola dell'area uguale (2 pi a = maglia), limitato a 1/4 del lato piu' corto.</summary>
    public class NecGriglia
    {
        readonly double maglia;
        readonly List<double[]> fili = new List<double[]>();          // x1 y1 z1 x2 y2 z2
        readonly List<double[]> dipoli = new List<double[]>();        // x1 y1 z1 x2 y2 z2 nseg
        readonly List<Complex> tensioni = new List<Complex>();
        public double Raggio;
        public NecGriglia(double maglia_m) { maglia = maglia_m; }
        public int Fili { get { return fili.Count + dipoli.Count; } }
        public int Segmenti { get { int s = fili.Count; foreach (var d in dipoli) s += (int)d[6]; return s; } }

        void Filo(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            if (Math.Abs(x1 - x2) + Math.Abs(y1 - y2) + Math.Abs(z1 - z2) < 1e-9) return;
            fili.Add(new[] { x1, y1, z1, x2, y2, z2 });
        }

        static int Multiplo8(int n) { return Math.Max(8, ((n + 7) / 8) * 8); }

        static List<double> Stazioni(List<TrombaStazione> s, double maglia, out List<double> ra, out List<double> rb)
        {
            var z = new List<double>(); ra = new List<double>(); rb = new List<double>();
            for (int i = 0; i < s.Count; i++)
            {
                if (i == 0) { z.Add(s[0].Z_m); ra.Add(s[0].A_m); rb.Add(s[0].B_m); continue; }
                double dz = s[i].Z_m - s[i - 1].Z_m;
                double dl = Math.Sqrt(dz * dz + Math.Pow(Math.Max(Math.Abs(s[i].A_m - s[i - 1].A_m), Math.Abs(s[i].B_m - s[i - 1].B_m)), 2));
                int n = Math.Max(1, (int)Math.Ceiling(dl / maglia - 1e-9));
                for (int k = 1; k <= n; k++)
                {
                    double t = (double)k / n;
                    if (k == n) { z.Add(s[i].Z_m); ra.Add(s[i].A_m); rb.Add(s[i].B_m); continue; }
                    z.Add(s[i - 1].Z_m + t * dz); ra.Add(s[i - 1].A_m + t * (s[i].A_m - s[i - 1].A_m)); rb.Add(s[i - 1].B_m + t * (s[i].B_m - s[i - 1].B_m));
                }
            }
            return z;
        }

        /// <summary>Tubo circolare dal profilo (fondo al primo punto, bocca all'ultimo), con il fondo chiuso e gli anelli choke.</summary>
        public void TuboCircolare(List<TrombaStazione> prof, bool fondo, List<double[]> anelli)
        {
            double rmax = 0; foreach (var s in prof) rmax = Math.Max(rmax, s.A_m);
            if (anelli != null) foreach (var a in anelli) rmax = Math.Max(rmax, a[3]);
            int M = Multiplo8((int)Math.Ceiling(2 * Math.PI * rmax / maglia));
            // stazioni del tubo, con quelle degli anelli inserite
            var prof2 = new List<TrombaStazione>(prof);
            if (anelli != null)
                foreach (var a in anelli)
                    foreach (double zz in new[] { a[0], a[1] })
                    {
                        bool c = false; foreach (var s in prof2) if (Math.Abs(s.Z_m - zz) < 1e-9) c = true;
                        if (!c) { double rr = prof[0].A_m; prof2.Add(new TrombaStazione { Z_m = zz, A_m = rr, B_m = rr }); }
                    }
            prof2.Sort((u, v) => u.Z_m.CompareTo(v.Z_m));
            List<double> ra, rb;
            var z = Stazioni(prof2, maglia, out ra, out rb);
            double latoMin = double.MaxValue;
            for (int i = 0; i < z.Count; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    double f1 = 2 * Math.PI * j / M, f2 = 2 * Math.PI * (j + 1) / M;
                    Filo(ra[i] * Math.Cos(f1), ra[i] * Math.Sin(f1), z[i], ra[i] * Math.Cos(f2), ra[i] * Math.Sin(f2), z[i]);
                    if (i > 0) Filo(ra[i - 1] * Math.Cos(f1), ra[i - 1] * Math.Sin(f1), z[i - 1], ra[i] * Math.Cos(f1), ra[i] * Math.Sin(f1), z[i]);
                }
                latoMin = Math.Min(latoMin, 2 * Math.PI * ra[i] / M);
            }
            if (fondo) latoMin = Math.Min(latoMin, Disco(z[0], ra[0], M));
            if (anelli != null)
            {
                // fondo anulare dietro + cilindro esterno di ciascun anello (le pareti interne sono il tubo o l'anello precedente)
                foreach (var a in anelli)
                {
                    int nr = Math.Max(1, (int)Math.Ceiling((a[3] - a[2]) / maglia - 1e-9));
                    for (int k = 1; k <= nr; k++)
                    {
                        double rr0 = a[2] + (a[3] - a[2]) * (k - 1) / nr, rr = a[2] + (a[3] - a[2]) * k / nr;
                        for (int j = 0; j < M; j++)
                        {
                            double f1 = 2 * Math.PI * j / M, f2 = 2 * Math.PI * (j + 1) / M;
                            Filo(rr * Math.Cos(f1), rr * Math.Sin(f1), a[0], rr * Math.Cos(f2), rr * Math.Sin(f2), a[0]);
                            Filo(rr0 * Math.Cos(f1), rr0 * Math.Sin(f1), a[0], rr * Math.Cos(f1), rr * Math.Sin(f1), a[0]);
                        }
                    }
                    // cilindro esterno da a[0] a a[1]
                    int nz = Math.Max(1, (int)Math.Ceiling((a[1] - a[0]) / maglia - 1e-9));
                    for (int k = 1; k <= nz; k++)
                    {
                        double z0 = a[0] + (a[1] - a[0]) * (k - 1) / nz, z1 = a[0] + (a[1] - a[0]) * k / nz;
                        for (int j = 0; j < M; j++)
                        {
                            double f1 = 2 * Math.PI * j / M, f2 = 2 * Math.PI * (j + 1) / M;
                            Filo(a[3] * Math.Cos(f1), a[3] * Math.Sin(f1), z1, a[3] * Math.Cos(f2), a[3] * Math.Sin(f2), z1);
                            Filo(a[3] * Math.Cos(f1), a[3] * Math.Sin(f1), z0, a[3] * Math.Cos(f1), a[3] * Math.Sin(f1), z1);
                        }
                    }
                }
            }
            Raggio = Math.Min(maglia / (2 * Math.PI), latoMin / 4);
        }

        // disco di fondo: anelli concentrici con i nodi dimezzati verso il centro, raggi fino al centro
        double Disco(double z, double R, int M)
        {
            int nr = Math.Max(1, (int)Math.Ceiling(R / maglia - 1e-9));
            var N = new int[nr + 1]; N[nr] = M;
            double latoMin = double.MaxValue;
            for (int j = nr - 1; j >= 1; j--)
            {
                int n = N[j + 1]; double rj = R * j / nr;
                while (n % 2 == 0 && n > 4 && 2 * Math.PI * rj / n < maglia / 2) n /= 2;
                N[j] = n;
            }
            for (int j = nr - 1; j >= 1; j--)
            {
                double rj = R * j / nr, rj1 = R * (j + 1) / nr;
                for (int i = 0; i < N[j]; i++)
                {
                    double f1 = 2 * Math.PI * i / N[j], f2 = 2 * Math.PI * (i + 1) / N[j];
                    Filo(rj * Math.Cos(f1), rj * Math.Sin(f1), z, rj * Math.Cos(f2), rj * Math.Sin(f2), z);
                    Filo(rj * Math.Cos(f1), rj * Math.Sin(f1), z, rj1 * Math.Cos(f1), rj1 * Math.Sin(f1), z);
                }
                latoMin = Math.Min(latoMin, 2 * Math.PI * rj / N[j]);
            }
            int n1 = nr >= 2 ? N[1] : Math.Min(M, 8);
            double r1 = R / nr;
            for (int i = 0; i < n1; i++)
            {
                double f = 2 * Math.PI * i / n1;
                Filo(0, 0, z, r1 * Math.Cos(f), r1 * Math.Sin(f), z);
            }
            latoMin = Math.Min(latoMin, r1);
            return latoMin;
        }

        /// <summary>Tubo rettangolare (A = semilato x, B = semilato y) con il fondo a griglia.</summary>
        public void TuboRettangolare(List<TrombaStazione> prof)
        {
            double amax = 0, bmax = 0; foreach (var s in prof) { amax = Math.Max(amax, s.A_m); bmax = Math.Max(bmax, s.B_m); }
            int nx = Math.Max(2, (int)Math.Ceiling(2 * amax / maglia - 1e-9)), ny = Math.Max(2, (int)Math.Ceiling(2 * bmax / maglia - 1e-9));
            // perimetro normalizzato: lato basso, destro, alto, sinistro
            var sx = new List<double>(); var sy = new List<double>();
            for (int i = 0; i < nx; i++) { sx.Add(-1 + 2.0 * i / nx); sy.Add(-1); }
            for (int i = 0; i < ny; i++) { sx.Add(1); sy.Add(-1 + 2.0 * i / ny); }
            for (int i = 0; i < nx; i++) { sx.Add(1 - 2.0 * i / nx); sy.Add(1); }
            for (int i = 0; i < ny; i++) { sx.Add(-1); sy.Add(1 - 2.0 * i / ny); }
            int M = sx.Count;
            List<double> ra, rb;
            var z = Stazioni(prof, maglia, out ra, out rb);
            double latoMin = double.MaxValue;
            for (int i = 0; i < z.Count; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    int j2 = (j + 1) % M;
                    Filo(sx[j] * ra[i], sy[j] * rb[i], z[i], sx[j2] * ra[i], sy[j2] * rb[i], z[i]);
                    if (i > 0) Filo(sx[j] * ra[i - 1], sy[j] * rb[i - 1], z[i - 1], sx[j] * ra[i], sy[j] * rb[i], z[i]);
                }
                latoMin = Math.Min(latoMin, Math.Min(2 * ra[i] / nx, 2 * rb[i] / ny));
            }
            // fondo: linee interne
            double A = ra[0], B = rb[0], z0 = z[0];
            for (int jy = 1; jy < ny; jy++)
                for (int ix = 0; ix < nx; ix++)
                    Filo(-A + 2 * A * ix / nx, -B + 2 * B * jy / ny, z0, -A + 2 * A * (ix + 1) / nx, -B + 2 * B * jy / ny, z0);
            for (int ix = 1; ix < nx; ix++)
                for (int jy = 0; jy < ny; jy++)
                    Filo(-A + 2 * A * ix / nx, -B + 2 * B * jy / ny, z0, -A + 2 * A * ix / nx, -B + 2 * B * (jy + 1) / ny, z0);
            Raggio = Math.Min(maglia / (2 * Math.PI), latoMin / 4);
        }

        /// <summary>Dipolo lungo Y (polarizzazione del piano E = phi 90) e, in circolare, un secondo lungo X 0,03 lambda piu' avanti, a -90 gradi.</summary>
        public void Dipolo(double z, double h, double lam, bool anche_x)
        {
            int ns = Math.Max(5, ((int)Math.Ceiling(2 * h / (lam / 40)) | 1));
            dipoli.Add(new[] { 0, -h, z, 0, h, z, ns }); tensioni.Add(new Complex(1, 0));
            if (anche_x) { double z2 = z + 0.03 * lam; dipoli.Add(new[] { -h, 0, z2, h, 0, z2, ns }); tensioni.Add(new Complex(0, -1)); }
        }

        /// <summary>Nodi della griglia toccati da un solo filo (estremi liberi): devono essere zero.</summary>
        public int NodiSciolti()
        {
            var cont = new Dictionary<string, int>();
            Func<double, double, double, string> k = (x, y, z) => string.Format(CultureInfo.InvariantCulture, "{0:0.000000}|{1:0.000000}|{2:0.000000}", x, y, z).Replace("-0.000000", "0.000000");
            foreach (var f in fili)
            {
                foreach (var key in new[] { k(f[0], f[1], f[2]), k(f[3], f[4], f[5]) })
                { int c; cont.TryGetValue(key, out c); cont[key] = c + 1; }
            }
            int n = 0; foreach (var kv in cont) if (kv.Value < 2) n++;
            return n;
        }

        public string Testo(TrombaInput p, TrombaResult r, double lam)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("CM YA-NEC - tromba " + p.Tipo + string.Format(ci, " per {0:0.0} MHz (lambda {1:0.00} mm)", p.FreqMHz, lam * 1000));
            sb.AppendLine("CM Bocca nel piano z = 0, la tromba irradia verso +Z; polarizzazione lungo Y: piano E = phi 90, piano H = phi 0");
            sb.AppendLine(string.Format(ci, "CM Griglia di fili, maglia {0:0.0} mm, raggio dei fili {1:0.000} mm; {2} segmenti", maglia * 1000, Raggio * 1000, Segmenti));
            sb.AppendLine("CM Eccitazione: dipolo nella guida a lambda guida / 4 dal fondo (polarizzatore ideale; la sonda reale va tarata a parte)");
            if (dipoli.Count > 1) sb.AppendLine("CM Circolare: secondo dipolo lungo X a -90 gradi; verificare il senso con RHCP/LHCP");
            foreach (var q in r.Quote) sb.AppendLine(string.Format(ci, "CM   {0}: {1:0.0}", q.Nome, q.Mm));
            sb.AppendLine("CE");
            int tag = 1;
            foreach (var f in fili)
                sb.AppendLine(string.Format(ci, "GW {0} 1 {1:0.000000} {2:0.000000} {3:0.000000} {4:0.000000} {5:0.000000} {6:0.000000} {7:0.000000}", tag++, f[0], f[1], f[2], f[3], f[4], f[5], Raggio));
            var tagDip = new List<int>();
            foreach (var d in dipoli)
            {
                tagDip.Add(tag);
                sb.AppendLine(string.Format(ci, "GW {0} {1} {2:0.000000} {3:0.000000} {4:0.000000} {5:0.000000} {6:0.000000} {7:0.000000} {8:0.000000}", tag++, (int)d[6], d[0], d[1], d[2], d[3], d[4], d[5], lam / 300));
            }
            sb.AppendLine("GE 0");
            sb.AppendLine(string.Format(ci, "FR 0 1 0 0 {0:0.000} 0", p.FreqMHz));
            for (int i = 0; i < dipoli.Count; i++)
                sb.AppendLine(string.Format(ci, "EX 0 {0} {1} 0 {2:0.000} {3:0.000}", tagDip[i], ((int)dipoli[i][6] + 1) / 2, tensioni[i].Real, tensioni[i].Imaginary));
            sb.AppendLine("RP 0 181 1 1000 0 90 1 0");
            sb.AppendLine("RP 0 181 1 1000 0 0 1 0");
            sb.AppendLine("RP 0 181 1 1000 0 45 1 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }
    }
}
