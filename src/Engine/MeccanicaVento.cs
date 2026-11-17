// YA-NEC 1.2.0 - Calcoli meccanici: Vento (bozzetto VHF 8) e Bilanciamento (bozzetto VHF 12). Predimensionamento.
// Pressione: NTC 2018 cap. 3.3 e Circolare 7/2019 (vb, qr, ce). Coefficienti di forza: CNR-DT 207 R1/2018 ed EN 1991-1-4
// (cf0 dei cilindri dal numero di Reynolds, §7.9.2; sezioni quadre §7.6; snellezza psi_lambda §7.13).
// Direzione del vento: principio del flusso trasversale (cross-flow, K5IU; ARRL Antenna Book, appendice sui carichi del vento):
// su un tubo conta solo la componente della velocita' perpendicolare al suo asse, quindi la forza va con sin^2 dell'angolo.
using System;
using System.Collections.Generic;
using System.Linq;

namespace YaNec.Engine
{
    public static class MeccanicaVento
    {
        public const double Nu = 15e-6;   // viscosita' cinematica dell'aria (m2/s)

        /// <summary>Zona di vento NTC 2018 proposta dalle coordinate: confini approssimati (riquadri delle isole minori, Sicilia,
        /// provincia di Reggio Calabria, Sardegna con la retta Capo Teulada - Isola di Maddalena, Trieste, Liguria, Emilia-Romagna, nord).
        /// Fuori dall'Italia restituisce 0 (zona da scegliere a mano).</summary>
        public static int ZonaNtc(double lat, double lon)
        {
            Func<double, double, double, double, bool> in_ = (a, b, c, d) => lat >= a && lat <= b && lon >= c && lon <= d;
            double[][] isole =
            {
                new[] { 40.53, 40.57, 14.19, 14.27 }, new[] { 40.69, 40.77, 13.85, 13.97 }, new[] { 40.74, 40.78, 13.99, 14.04 }, new[] { 40.78, 40.95, 12.93, 13.45 },
                new[] { 42.71, 42.89, 10.08, 10.44 }, new[] { 42.33, 42.39, 10.86, 10.94 }, new[] { 43.00, 43.07, 9.78, 9.85 }, new[] { 42.55, 42.61, 10.05, 10.12 },
                new[] { 38.35, 38.82, 14.33, 15.25 }, new[] { 37.90, 38.10, 11.90, 12.40 }, new[] { 38.68, 38.73, 13.15, 13.21 }, new[] { 36.72, 36.84, 11.92, 12.06 },
                new[] { 35.48, 35.90, 12.40, 12.90 }, new[] { 42.10, 42.15, 15.47, 15.53 }, new[] { 41.18, 41.32, 9.33, 9.52 }, new[] { 38.95, 39.20, 8.20, 8.40 }
            };
            foreach (double[] b in isole) if (in_(b[0], b[1], b[2], b[3])) return 9;
            if (lat < 35.4 || lat > 47.2 || lon < 6.5 || lon > 18.6) return 0;
            if (in_(36.6, 38.32, 12.4, 15.66)) return 4;                       // Sicilia
            if (in_(37.9, 38.6, 15.6, 16.35)) return 4;                        // provincia di Reggio Calabria
            if (in_(38.85, 41.27, 8.1, 9.85))                                  // Sardegna: lato della retta Capo Teulada - Isola di Maddalena
            {
                double x1 = 8.64, y1 = 38.866, x2 = 9.40, y2 = 41.217;
                double lato = (x2 - x1) * (lat - y1) - (y2 - y1) * (lon - x1);
                return lato < 0 ? 5 : 6;
            }
            if (in_(45.55, 45.82, 13.55, 13.95)) return 8;                     // Trieste
            if (in_(43.75, 44.55, 7.5, 10.0) && lat < 44.0 + 0.3 * Math.Max(0, lon - 7.5)) return 7;   // Liguria (fascia costiera)
            if (lat >= 44.0 && lat <= 45.1 && lon >= 9.2 && lon <= 12.8 && !(lon > 11.4 && lat > 44.95)) return 2;   // Emilia-Romagna
            if (lat > 44.6 || (lat > 44.0 && lon < 9.2)) return 1;            // nord
            return 3;
        }

        /// <summary>cf0 di un cilindro (EN 1991-1-4 eq. 7.19, fig. 7.28) per diametro b (m), velocita' v (m/s) e rugosita' k (m).</summary>
        public static double Cf0Cilindro(double b, double v, double k = 1.5e-6)
        {
            double re = Math.Max(1, b * v / Nu);
            if (re < 2e5) return 1.2;   // regime subcritico: CNR-DT 207 / EC1, cilindro liscio
            double a = 0.11 / Math.Pow(re / 1e6, 1.4);
            double c = 1.2 + 0.18 * Math.Log10(10 * k / b) / (1 + 0.4 * Math.Log10(re / 1e6));
            return Math.Min(1.2, Math.Max(a, c));
        }

        public class Parte
        {
            public string Nome = "";
            public double Area;        // m2, proiettata perpendicolarmente all'asse dei tubi
            public double Cf0, Psi, Cf;
            public int Asse;           // 0 = tubi perpendicolari al boom (elementi), 1 = lungo il boom, 2 = verticali (palo, telaio)
            public double X;           // centro dell'area lungo il boom (m dal riflettore), per il momento torcente
            public double Quadro;      // 1 = sezione quadra (conta solo col vento perpendicolare al suo asse)
        }

        public class EsitoVento
        {
            public double Vb, Qr, Ce, Q;
            public List<Parte> Parti = new List<Parte>();
            public double[] Dir, Forza, Torcente;   // direzione del vento rispetto al boom 0..180 a passi di 5 gradi
            public double FMax, DirMax, MFlettente, TMax;
        }

        /// <summary>Forza (N) su una parte con il vento a theta gradi dal boom (componente perpendicolare all'asse dei tubi, al quadrato).</summary>
        public static double[] ForzaXY(Parte p, double q, double thetaDeg)
        {
            double t = thetaDeg * Math.PI / 180, c = Math.Cos(t), s = Math.Sin(t);
            double f0 = q * p.Cf * p.Area;
            if (p.Asse == 0) return new[] { f0 * c * Math.Abs(c), 0 };      // elementi (asse y): spinta lungo il boom
            if (p.Asse == 1) return new[] { 0, f0 * s * Math.Abs(s) };      // boom (asse x): spinta laterale
            return new[] { f0 * c, f0 * s };                                 // tubi verticali: sempre di fronte al vento
        }

        public static EsitoVento Calcola(Meccanica.Zona z, int cat, double quotaSlm, double altezza, double ct, List<Parte> parti, double xRotore, double bracci)
        {
            EsitoVento r = new EsitoVento();
            r.Vb = Meccanica.Vb(z, quotaSlm);
            r.Qr = Meccanica.Pressione(r.Vb);
            r.Ce = Meccanica.Ce(cat, altezza);
            r.Q = r.Qr * r.Ce * ct;
            double vPicco = Math.Sqrt(2 * r.Q / Meccanica.Rho);
            foreach (Parte p in parti) if (p.Cf <= 0) p.Cf = p.Cf0 * p.Psi;
            r.Parti = parti;
            int n = 37;
            r.Dir = new double[n]; r.Forza = new double[n]; r.Torcente = new double[n];
            for (int i = 0; i < n; i++)
            {
                double th = i * 5, fx = 0, fy = 0, tz = 0;
                foreach (Parte p in parti)
                {
                    double[] f = ForzaXY(p, r.Q, th);
                    fx += f[0]; fy += f[1];
                    if (p.Asse == 1 || p.Asse == 2) tz += f[1] * (p.X - xRotore);
                }
                r.Dir[i] = th; r.Forza[i] = Math.Sqrt(fx * fx + fy * fy); r.Torcente[i] = tz;
                if (r.Forza[i] > r.FMax) { r.FMax = r.Forza[i]; r.DirMax = th; }
                r.TMax = Math.Max(r.TMax, Math.Abs(tz));
            }
            r.MFlettente = r.FMax * bracci;
            return r;
        }

        // ------------------------------------------------------------------------------------------------
        // Bilanciamento
        // ------------------------------------------------------------------------------------------------
        public class Massa { public string Nome = ""; public double Kg, X, Area, XArea; public bool ContaVento = true; }

        public class EsitoBil
        {
            public double Kg, Xg, Area, Xa, XCons, TAttuale, TCons, MPesoAttuale;
        }

        /// <summary>Baricentro, centro dell'area laterale (solo le parti con ContaVento: gli elementi, per il flusso trasversale, non fanno
        /// ruotare l'antenna con il vento laterale), posizione di compromesso (media pesata: peso = 1, vento = pesoVento) e torcenti.</summary>
        public static EsitoBil Bilancia(List<Massa> m, double q, double cf, double xAttuale, int modo)
        {
            EsitoBil r = new EsitoBil();
            r.Kg = m.Sum(a => a.Kg);
            r.Xg = r.Kg > 0 ? m.Sum(a => a.Kg * a.X) / r.Kg : 0;
            List<Massa> v = m.Where(a => a.ContaVento && a.Area > 0).ToList();
            r.Area = v.Sum(a => a.Area);
            r.Xa = r.Area > 0 ? v.Sum(a => a.Area * a.XArea) / r.Area : r.Xg;
            r.XCons = modo == 1 ? r.Xg : modo == 2 ? r.Xa : (r.Xg + r.Xa) / 2;
            r.TAttuale = q * cf * r.Area * (r.Xa - xAttuale);
            r.TCons = q * cf * r.Area * (r.Xa - r.XCons);
            r.MPesoAttuale = r.Kg * Meccanica.G * (r.Xg - xAttuale);
            return r;
        }
    }
}
