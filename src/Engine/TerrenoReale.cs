// YA-NEC 1.2.0 - Diagrammi > "Terreno reale" (bozzetti HF 1 rev. 3 e VHF 1 rev. 2, stesso motore), metodo alla HFTA (R. D. Straw N6BV,
// ARRL Antenna Book 24a ed. par. 14.3.4): il campo dell'antenna viene dalla soluzione NEC; il terreno aggiunge il raggio diretto (oscurato o
// diffratto dai rilievi), un raggio riflesso da ogni tratto del profilo che vede l'antenna e il cielo, e la diffrazione sul crinale piu'
// importante (lama di coltello, ITU-R P.526: approssimazione della UTD di Kouyoumjian e Pathak usata da HFTA; fumetto giallo).
// Riflessione su un tratto inclinato di sigma: il campo riflesso all'elevazione alfa e' quello dell'antenna specchiata nella retta del tratto,
// cioe' il campo diretto del NEC nella direzione beta = 2 sigma - alfa per il coefficiente di Fresnel all'angolo radente alfa - sigma, con la
// fase del punto immagine dell'origine (centro del modello, sul suolo). Sul terreno piatto questo e' esattamente il campo lontano del NEC sopra
// il suolo (stesso coefficiente rrh / rrv di ffld): e' il collaudo del bozzetto (curva reale = curva NEC entro 0,1 dB).
// Opzioni: mare (er 81, sigma 5 S/m) dove il profilo e' a quota 0, rugosita' del mare (fattore di Ament, onda di altezza efficace data),
// terra curva (raggio equivalente k = 4/3: abbassamento d^2 / (2 k R) e fattore di divergenza), diffrazione.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace YaNec.Engine
{
    public static class TerrenoReale
    {
        public class Opzioni
        {
            public double EpsTerra = 13, SigmaTerra = 0.005;
            public double EpsMare = 81, SigmaMare = 5;
            public bool Mare = true, Rugosita, Divergenza, Diffrazione = true;
            public double OndaM = 0.25;          // altezza efficace delle onde (deviazione standard, m) per la rugosita'
            public double AlzaM;                 // innalzamento dell'antenna rispetto al modello (scansione dell'altezza)
        }

        public class Esito
        {
            public double[] El;                  // gradi di elevazione 0..180 (oltre 90 = dietro, profilo opposto)
            public double[] Reale, Piatto, Libero;   // dBi
            public double OrizzonteAvanti, OrizzonteDietro;   // angolo dell'orizzonte (gradi)
            public double RiflessioneKm = double.NaN;         // punto di riflessione del primo lobo reale
            public bool RiflessioneMare;
        }

        static Complex Gamma(double psiRad, double eps, double sigma, double fMHz, bool vert)
        {
            double lam = 299.792458 / fMHz;
            Complex epsc = new Complex(eps, -sigma * lam * 59.96);
            Complex zr = 1 / Complex.Sqrt(epsc);
            double roz = Math.Sin(psiRad), thz = Math.Cos(psiRad);
            Complex zrsin = Complex.Sqrt(1.0 - zr * zr * thz * thz);
            if (vert) return -(roz - zr * zrsin) / (roz + zr * zrsin);
            return (zr * roz - zrsin) / (zr * roz + zrsin);
        }

        /// <summary>Fattore di diffrazione di Fresnel per la lama di coltello (ITU-R P.526): F(nu) complesso, |F| = 1 senza ostacolo.</summary>
        public static Complex Fresnel(double nu)
        {
            // F = (1 + j)/2 * integral da nu a infinito di exp(-j pi t^2 / 2) dt
            double c, s; FresnelCS(nu, out c, out s);
            return new Complex(0.5, 0.5) * new Complex(0.5 - c, -(0.5 - s));
        }

        /// <summary>Integrali di Fresnel C(x), S(x): serie di potenze fino a |x| = 3, poi sviluppo asintotico (errore sotto 1e-6).</summary>
        public static void FresnelCS(double x, out double c, out double s)
        {
            double ax = Math.Abs(x);
            if (ax <= 3)
            {
                // C = sum (-1)^n (pi/2)^(2n) x^(4n+1) / ((2n)! (4n+1)),  S = sum (-1)^n (pi/2)^(2n+1) x^(4n+3) / ((2n+1)! (4n+3))
                double z = Math.PI / 2 * ax * ax, t = ax, sc = 0, ss = 0;   // t = z^k x / k!
                for (int k = 0; k < 80; k++)
                {
                    double term = t / (2 * k + 1);
                    if ((k & 1) == 0) { if ((k & 2) == 0) sc += term; else sc -= term; }
                    else { if ((k & 2) == 0) ss += term; else ss -= term; }
                    t *= z / (k + 1);
                    if (t < 1e-17 && k > 10) break;
                }
                c = sc; s = ss;
            }
            else
            {
                double px2 = Math.PI * ax * ax, u = 1 / (px2 * px2 / (ax * ax * ax * ax) * ax * ax * ax * ax);   // 1/(pi x^2)^2
                double f = (1 - 3 * u + 105 * u * u) / (Math.PI * ax), g = (1 - 15 * u + 945 * u * u) / (Math.PI * px2 * ax);
                double a = px2 / 2, sn = Math.Sin(a), cs = Math.Cos(a);
                c = 0.5 + f * sn - g * cs; s = 0.5 - f * cs - g * sn;
            }
            if (x < 0) { c = -c; s = -s; }
        }

        /// <summary>Campo (theta, phi) diretto del NEC nella direzione di elevazione el (gradi, anche negativa) e azimut phi.</summary>
        public delegate void CampoDiretto(double elDeg, double phiDeg, out Complex eth, out Complex eph);

        /// <summary>
        /// Diagramma di elevazione sul profilo. diretto = campo diretto del NEC (senza il suolo), gcon = costante di guadagno del motore,
        /// fMHz, azimut avanti phi; pa / pd = profili avanti e dietro (distanze in m dalla base, quote in m s.l.m.); quotaAntenna = altezza del centro
        /// dell'antenna sul suolo (m); opt. piatto = guadagno del NEC sul terreno piatto per lo stesso elevazione (null = non calcolato).
        /// </summary>
        public static Esito Calcola(CampoDiretto diretto, double gcon, double fMHz, double phi, Dem.Profilo pa, Dem.Profilo pd, double quotaAntenna, Opzioni o, Func<double, double> piatto)
        {
            Esito r = new Esito();
            int n = 721;   // passo 0,25 gradi
            r.El = new double[n]; r.Reale = new double[n]; r.Piatto = new double[n]; r.Libero = new double[n];
            double lam = 299.792458 / fMHz, k = 2 * Math.PI / lam;
            double[] oriz = new double[2];
            for (int lato = 0; lato < 2; lato++)
            {
                Dem.Profilo p = lato == 0 ? pa : pd;
                if (p == null) continue;
                double phiL = lato == 0 ? phi : (phi + 180) % 360;
                // profilo relativo alla base dell'antenna, con la curvatura della terra se richiesta
                int m = p.D.Length;
                double[] X = new double[m], Z = new double[m];
                double kR = 4.0 / 3 * Dem.Rterra * 1000;
                for (int i = 0; i < m; i++)
                {
                    X[i] = p.D[i];
                    Z[i] = p.H[i] - p.QuotaBase - (o.Divergenza ? p.D[i] * p.D[i] / (2 * kR) : 0);
                }
                double za = quotaAntenna + o.AlzaM;   // centro dell'antenna sopra la base
                // orizzonte
                double om = -90;
                for (int i = 1; i < m; i++) om = Math.Max(om, Math.Atan2(Z[i] - za, X[i]) * 180 / Math.PI);
                oriz[lato] = om;
                for (int q = 0; q <= 360; q++)
                {
                    double el = q * 0.25;
                    int idx = lato == 0 ? q : 720 - q;
                    double a = el * Math.PI / 180;
                    double ca = Math.Cos(a), sa = Math.Sin(a);
                    // campo diretto, fase riferita all'origine del modello (sul suolo alla base)
                    Complex dth, dph; diretto(el, phiL, out dth, out dph);
                    Complex th = Complex.Zero, ph = Complex.Zero;
                    // raggio diretto: ostacolo piu' alto rispetto alla retta dall'antenna
                    double peggio = double.NegativeInfinity; int ip = -1;
                    for (int i = 1; i < m; i++)
                    {
                        double clear = Z[i] - (za + X[i] * Math.Tan(a));
                        if (el >= 89.9) clear = -1e9;
                        if (clear > peggio) { peggio = clear; ip = i; }
                    }
                    Complex fdir = Complex.One;
                    // solo un crinale vero (piu' alto dei punti vicini di almeno lambda/10) diffrange; il terreno piatto no
                    if (ip > 0)
                    {
                        int w = Math.Max(1, m / 200);
                        double vic = double.NegativeInfinity;
                        for (int j = Math.Max(0, ip - 5 * w); j <= Math.Min(m - 1, ip + 5 * w); j++) if (Math.Abs(j - ip) >= w) vic = Math.Max(vic, Z[j]);
                        if (ip == m - 1 || Z[ip] < vic + lam / 10) { if (peggio <= 0) ip = -1; }
                    }
                    if (ip > 0 && peggio > -1e8)
                    {
                        double d1 = X[ip] / Math.Max(ca, 1e-6);
                        double nu = peggio * ca * Math.Sqrt(2 / (lam * d1));
                        if (nu > -0.78 || peggio > 0)
                            fdir = o.Diffrazione ? Fresnel(nu) : (peggio > 0 ? Complex.Zero : Complex.One);
                    }
                    th += dth * fdir; ph += dph * fdir;
                    // raggi riflessi dai tratti del profilo
                    for (int i = 0; i + 1 < m; i++)
                    {
                        double x1 = X[i], z1 = Z[i], x2 = X[i + 1], z2 = Z[i + 1];
                        double sig = Math.Atan2(z2 - z1, x2 - x1);
                        double psi = a - sig;
                        if (psi <= 0) continue;
                        // immagine del centro dell'antenna A = (0, za) nella retta del tratto
                        double nx = -Math.Sin(sig), nz = Math.Cos(sig);
                        double dA = (0 - x1) * nx + (za - z1) * nz;
                        double ax = 0 - 2 * dA * nx, az = za - 2 * dA * nz;
                        // punto di riflessione: retta dall'immagine nella direzione alfa che incontra il tratto
                        double tx = x2 - x1, tz = z2 - z1;
                        double den = ca * tz - sa * tx;
                        if (Math.Abs(den) < 1e-12) continue;
                        double t = ((x1 - ax) * tz - (z1 - az) * tx) / den;
                        if (t <= 0) continue;
                        double px = ax + t * ca, pz = az + t * sa;
                        double u = ((px - x1) * tx + (pz - z1) * tz) / (tx * tx + tz * tz);
                        // 1.2.0: il tratto riflette solo la parte della prima zona di Fresnel che copre: fattore = integrale di Fresnel tra i suoi
                        // estremi (riferiti al punto speculare) diviso per l'integrale su tutto il piano. Il primo tratto continua sotto l'antenna e
                        // l'ultimo oltre il profilo; su un profilo piatto la somma dei fattori vale 1 (riflessione piena, come il NEC).
                        double lt = Math.Sqrt(tx * tx + tz * tz), r1 = t;
                        double kz = Math.Sin(psi) * Math.Sqrt(2 / (lam * Math.Max(r1, 1e-3)));
                        double nu1 = i == 0 ? double.NegativeInfinity : (0 - u) * lt * kz, nu2 = i + 2 == m ? double.PositiveInfinity : (1 - u) * lt * kz;
                        if (nu2 <= nu1) continue;
                        double c1, s1, c2, s2;
                        if (double.IsNegativeInfinity(nu1)) { c1 = -0.5; s1 = -0.5; } else FresnelCS(nu1, out c1, out s1);
                        if (double.IsPositiveInfinity(nu2)) { c2 = 0.5; s2 = 0.5; } else FresnelCS(nu2, out c2, out s2);
                        Complex zona = new Complex(c2 - c1, -(s2 - s1)) / new Complex(1, -1);
                        if (zona.Magnitude < 1e-4) continue;
                        // punto del tratto usato per la visibilita': il punto speculare riportato dentro il tratto
                        double uc = Math.Max(0, Math.Min(1, u));
                        px = x1 + uc * tx; pz = z1 + uc * tz;
                        if (px <= 0) px = Math.Max(1e-3, x2 * 1e-3);
                        // il tratto vede l'antenna e il cielo nella direzione alfa?
                        bool libero = true;
                        for (int j = 1; j < m && libero; j++)
                        {
                            if (X[j] < px - 1e-6) { double zl = za + (pz - za) * X[j] / px; if (Z[j] > zl + 0.01 && j != i && j != i + 1) libero = false; }
                            else if (X[j] > px + 1e-6) { double zl = pz + (X[j] - px) * Math.Tan(a); if (Z[j] > zl + 0.01 && j != i && j != i + 1) libero = false; }
                        }
                        if (!libero) continue;
                        bool mare = o.Mare && p.Mare[i] && p.Mare[i + 1];
                        double eps = mare ? o.EpsMare : o.EpsTerra, sgm = mare ? o.SigmaMare : o.SigmaTerra;
                        Complex gh = Gamma(psi, eps, sgm, fMHz, false), gv = Gamma(psi, eps, sgm, fMHz, true);
                        double fat = 1;
                        if (mare && o.Rugosita) { double g = 2 * Math.PI * o.OndaM * Math.Sin(psi) / lam; fat *= Math.Exp(-2 * g * g); }
                        if (o.Divergenza) fat /= Math.Sqrt(1 + 2 * px / (kR * Math.Max(Math.Tan(psi), 1e-4)));
                        // immagine dell'origine (0, 0) nella retta del tratto: fase del campo riflesso
                        double dO = (0 - x1) * nx + (0 - z1) * nz;
                        double ox = -2 * dO * nx, oz = -2 * dO * nz;
                        double fase = k * (ox * ca + oz * sa);
                        double beta = (2 * sig - a) * 180 / Math.PI;
                        Complex rth, rph; diretto(beta, phiL, out rth, out rph);
                        Complex e = new Complex(Math.Cos(fase), Math.Sin(fase)) * fat * zona;
                        // come ffld del NEC: componente orizzontale per rrh, verticale per -rrv
                        ph += rph * gh * e;
                        th += -rth * gv * e;
                        if (lato == 0 && q > 0 && double.IsNaN(r.RiflessioneKm) && el >= 0.5 && u >= 0 && u <= 1) { r.RiflessioneKm = px / 1000; r.RiflessioneMare = mare; }
                    }
                    double gl = gcon * ((th * Complex.Conjugate(th)).Real + (ph * Complex.Conjugate(ph)).Real);
                    double gf = gcon * ((dth * Complex.Conjugate(dth)).Real + (dph * Complex.Conjugate(dph)).Real);
                    r.El[idx] = lato == 0 ? el : 180 - el;
                    r.Reale[idx] = gl > 1e-20 ? 10 * Math.Log10(gl) : -99;
                    r.Libero[idx] = gf > 1e-20 ? 10 * Math.Log10(gf) : -99;
                    r.Piatto[idx] = piatto != null ? piatto(lato == 0 ? el : 180 - el) : double.NaN;
                }
            }
            r.OrizzonteAvanti = oriz[0]; r.OrizzonteDietro = oriz[1];
            return r;
        }
    }
}
