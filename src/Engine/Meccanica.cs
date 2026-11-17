// YA-NEC 1.2.0 - Calcolatori > Strumenti > "Meccanica" (bozzetto 9 approvato da IC8TEM il 17/11/2026, alla YagiStress di K7NV):
// elementi a tubi telescopici come travi a sbalzo dal boom, boom con il tirante, mast e staffe, area al vento e lista di taglio.
// Vento (proposta gia' approvata per il carico del vento): velocita' e pressione da NTC 2018 par. 3.3 (zona, categoria di esposizione,
// quota), coefficienti di forza da CNR-DT 207 R1/2018 ed EN 1991-1-4 par. 7.9 (cilindri) con il fattore di snellezza psi_lambda.
// TIA-222-H, ASCE 7-22, AS/NZS 1170.2 e AIJ sono solo citate nel manuale.
// Leghe (EN 755-2, tubi estrusi, Rp0,2 minimo): 6063-T6 170 MPa, 6061-T6 240 MPa, 6082-T6 250 MPa; E = 70 GPa, densita' 2700 kg/m3.
// Trave a sbalzo: momento e freccia per integrazione (formule chiuse di Roark per il collaudo). E' un predimensionamento.
using System;
using System.Collections.Generic;

namespace YaNec.Engine
{
    public static class Meccanica
    {
        public const double Rho = 1.25, E = 70e9, DensAl = 2700, DensGhiaccio = 900, G = 9.81;

        public class Lega { public string Nome; public double Fy; }
        public static readonly Lega[] Leghe =
        {
            new Lega { Nome = "6063-T6", Fy = 170e6 },
            new Lega { Nome = "6061-T6", Fy = 240e6 },
            new Lega { Nome = "6082-T6", Fy = 250e6 },
        };

        /// <summary>Zone di NTC 2018 tab. 3.3.I: vb,0 (m/s), a0 (m), ks.</summary>
        public class Zona { public int N; public string Regioni; public double Vb0, A0, Ks; }
        public static readonly Zona[] Zone =
        {
            new Zona { N = 1, Regioni = "Valle d'Aosta, Piemonte, Lombardia, Trentino-Alto Adige, Veneto, Friuli-Venezia Giulia (tranne Trieste)", Vb0 = 25, A0 = 1000, Ks = 0.40 },
            new Zona { N = 2, Regioni = "Emilia-Romagna", Vb0 = 25, A0 = 750, Ks = 0.45 },
            new Zona { N = 3, Regioni = "Toscana, Marche, Umbria, Lazio, Abruzzo, Molise, Puglia, Campania, Basilicata, Calabria (tranne Reggio Calabria)", Vb0 = 27, A0 = 500, Ks = 0.37 },
            new Zona { N = 4, Regioni = "Sicilia e provincia di Reggio Calabria", Vb0 = 28, A0 = 500, Ks = 0.36 },
            new Zona { N = 5, Regioni = "Sardegna (a oriente della retta tra Capo Teulada e l'Isola di Maddalena)", Vb0 = 28, A0 = 750, Ks = 0.40 },
            new Zona { N = 6, Regioni = "Sardegna (a occidente della retta tra Capo Teulada e l'Isola di Maddalena)", Vb0 = 28, A0 = 500, Ks = 0.36 },
            new Zona { N = 7, Regioni = "Liguria", Vb0 = 28, A0 = 1000, Ks = 0.54 },
            new Zona { N = 8, Regioni = "Provincia di Trieste", Vb0 = 30, A0 = 1500, Ks = 0.50 },
            new Zona { N = 9, Regioni = "Isole (tranne Sicilia e Sardegna) e mare aperto", Vb0 = 31, A0 = 500, Ks = 0.32 },
        };
        /// <summary>Categorie di esposizione NTC 2018 tab. 3.3.II: kr, z0 (m), zmin (m).</summary>
        public static readonly double[,] Categorie = { { 0.17, 0.01, 2 }, { 0.19, 0.05, 4 }, { 0.20, 0.10, 5 }, { 0.22, 0.30, 8 }, { 0.23, 0.70, 12 } };
        public static readonly string[] NomiCategorie = { "I", "II", "III", "IV", "V" };

        /// <summary>Velocita' di riferimento vb = vb,0 ca (m/s), periodo di ritorno 50 anni (cr = 1).</summary>
        public static double Vb(Zona z, double quotaSlm)
        {
            double ca = quotaSlm <= z.A0 ? 1 : 1 + z.Ks * (Math.Min(quotaSlm, 1500) / z.A0 - 1);
            return z.Vb0 * ca;
        }
        /// <summary>Coefficiente di esposizione ce(z) di NTC 2018 (3.3.7), ct = 1.</summary>
        public static double Ce(int cat, double z)
        {
            double kr = Categorie[cat, 0], z0 = Categorie[cat, 1], zmin = Categorie[cat, 2];
            double zz = Math.Max(z, zmin), l = Math.Log(zz / z0);
            return kr * kr * l * (7 + l);
        }
        /// <summary>Velocita' di picco al sito (m/s): vb sqrt(ce), cioe' la velocita' che da' la pressione di picco qr ce.</summary>
        public static double VentoSito(Zona z, int cat, double quotaSlm, double altezza) { return Vb(z, quotaSlm) * Math.Sqrt(Ce(cat, altezza)); }
        public static double Pressione(double v) { return 0.5 * Rho * v * v; }

        /// <summary>Fattore di snellezza psi_lambda (EN 1991-1-4 fig. 7.36, solidita' 1) per la snellezza efficace lambda.</summary>
        public static double PsiLambda(double lambda)
        {
            double[,] t = { { 1, 0.6 }, { 10, 0.7 }, { 70, 0.91 }, { 200, 1.0 } };
            if (lambda <= 1) return 0.6;
            if (lambda >= 200) return 1.0;
            int i = 1; while (lambda > t[i, 0]) i++;
            double a = Math.Log10(t[i - 1, 0]), b = Math.Log10(t[i, 0]);
            return t[i - 1, 1] + (t[i, 1] - t[i - 1, 1]) * (Math.Log10(lambda) - a) / (b - a);
        }
        /// <summary>Coefficiente di forza di un tubo (cilindro liscio sottocritico cf,0 = 1,2 per CNR-DT 207 / EN 1991-1-4) per psi_lambda.</summary>
        public static double Cf(double lunghezza, double diametro)
        {
            double lam = Math.Min(2 * lunghezza / Math.Max(diametro, 1e-4), 70);
            return 1.2 * PsiLambda(lam);
        }

        public class Tubo { public double Da, A, D, S; public double L { get { return A - Da; } } }

        /// <summary>Tubi dal centro (boom) alla punta da Staffe.Elemento.Lato (fine dal centro, diametro) e dagli spessori in m.</summary>
        public static List<Tubo> TubiElemento(List<double[]> lato, IList<double> spessori)
        {
            List<Tubo> l = new List<Tubo>();
            double da = 0;
            for (int i = 0; i < lato.Count; i++)
            {
                double d = lato[i][1];
                double s = spessori != null && i < spessori.Count && spessori[i] > 0 ? spessori[i] : SpessoreTipico(d);
                l.Add(new Tubo { Da = da, A = lato[i][0], D = d, S = Math.Min(s, d / 2) });
                da = lato[i][0];
            }
            return l;
        }
        /// <summary>Spessore tipico dei tubi telescopici (m) per il diametro (m), da modificare con le misure vere.</summary>
        public static double SpessoreTipico(double d) { double mm = d * 1000; return (mm >= 28 ? 2.0 : mm >= 25 ? 1.6 : mm >= 18 ? 1.2 : mm >= 10 ? 1.0 : 0.8) / 1000; }

        public static double Modulo(double d, double s) { double di = d - 2 * s; return Math.PI * (Math.Pow(d, 4) - Math.Pow(di, 4)) / (32 * d); }
        public static double Inerzia(double d, double s) { double di = d - 2 * s; return Math.PI * (Math.Pow(d, 4) - Math.Pow(di, 4)) / 64; }
        public static double Massa(Tubo t) { double di = t.D - 2 * t.S; return DensAl * Math.PI * (t.D * t.D - di * di) / 4 * t.L; }

        public class Esito
        {
            public double[] X, Rapporto, Mw, Mg, Freccia;   // lungo l'elemento dal boom: sollecitazione / snervamento, momenti (N m), freccia (m)
            public double Max, XMax; public int TuboMax;
            public double Area, Massa, MassaGhiaccio;
        }

        /// <summary>Trave a sbalzo dal boom: vento v (m/s) perpendicolare, peso proprio e ghiaccio radiale t (m); spina = rinforzo interno ai giunti.</summary>
        public static Esito Elemento(List<Tubo> tubi, Lega lega, double v, double ghiaccio, bool spina, double cfFisso = double.NaN)
        {
            Esito r = new Esito();
            double Ltot = tubi[tubi.Count - 1].A;
            int n = 600;
            r.X = new double[n + 1]; r.Rapporto = new double[n + 1]; r.Mw = new double[n + 1]; r.Mg = new double[n + 1]; r.Freccia = new double[n + 1];
            double q = Pressione(v);
            double[] w = new double[n + 1], g = new double[n + 1], ei = new double[n + 1];
            Func<double, int> quale = x => { for (int i = 0; i < tubi.Count; i++) if (x <= tubi[i].A + 1e-12) return i; return tubi.Count - 1; };
            for (int k = 0; k <= n; k++)
            {
                double x = Ltot * k / n; r.X[k] = x;
                Tubo t = tubi[quale(Math.Min(Ltot, x + 1e-9))];
                double de = t.D + 2 * ghiaccio;
                double cf = double.IsNaN(cfFisso) ? Cf(2 * Ltot, de) : cfFisso;
                w[k] = q * cf * de;
                double di = t.D - 2 * t.S;
                g[k] = G * (DensAl * Math.PI * (t.D * t.D - di * di) / 4 + DensGhiaccio * Math.PI * (de * de - t.D * t.D) / 4);
                ei[k] = E * Inerzia(t.D, t.S);
            }
            foreach (Tubo t in tubi) { r.Area += (t.D + 2 * ghiaccio) * t.L; r.Massa += Massa(t); double de = t.D + 2 * ghiaccio; r.MassaGhiaccio += DensGhiaccio * Math.PI * (de * de - t.D * t.D) / 4 * t.L; }
            // taglio e momento dalla punta (V' = -w, M' = V)
            double dx = Ltot / n, Vw = 0, Vg = 0, Mw = 0, Mg = 0;
            r.Mw[n] = 0; r.Mg[n] = 0;
            for (int k = n - 1; k >= 0; k--)
            {
                double ww = (w[k] + w[k + 1]) / 2, gg = (g[k] + g[k + 1]) / 2;
                Mw += Vw * dx + ww * dx * dx / 2; Mg += Vg * dx + gg * dx * dx / 2;
                Vw += ww * dx; Vg += gg * dx;
                r.Mw[k] = Mw; r.Mg[k] = Mg;
            }
            r.Max = 0;
            for (int k = 0; k <= n; k++)
            {
                double x = r.X[k];
                int i = quale(Math.Min(Ltot, x + 1e-9));
                Tubo t = tubi[i];
                double sig = Math.Sqrt(r.Mw[k] * r.Mw[k] + r.Mg[k] * r.Mg[k]) / Modulo(t.D, t.S);
                // giunto senza spina: il tubo interno lavora sul bordo del tubo esterno (concentrazione, stima 1,25)
                if (!spina && i > 0 && x - t.Da < 0.05) sig *= 1.25;
                r.Rapporto[k] = sig / lega.Fy;
                if (r.Rapporto[k] > r.Max) { r.Max = r.Rapporto[k]; r.XMax = x; r.TuboMax = i; }
            }
            // freccia sotto il peso (e il ghiaccio): theta' = M/EI, y' = theta, incastro al boom
            double th = 0, y = 0; r.Freccia[0] = 0;
            for (int k = 1; k <= n; k++)
            {
                th += (r.Mg[k - 1] / ei[k - 1] + r.Mg[k] / ei[k]) / 2 * dx;
                y += th * dx;
                r.Freccia[k] = y;
            }
            return r;
        }

        /// <summary>Vento (m/s) a cui l'elemento arriva allo snervamento nel primo punto.</summary>
        public static double Sopravvivenza(List<Tubo> tubi, Lega lega, double ghiaccio, bool spina)
        {
            double lo = 0, hi = 150;
            if (Elemento(tubi, lega, 0, ghiaccio, spina).Max >= 1) return 0;
            if (Elemento(tubi, lega, hi, ghiaccio, spina).Max < 1) return hi;
            for (int i = 0; i < 50; i++) { double m = (lo + hi) / 2; if (Elemento(tubi, lega, m, ghiaccio, spina).Max >= 1) hi = m; else lo = m; }
            return (lo + hi) / 2;
        }

        static readonly double[] Diametri = { 6, 8, 10, 12, 13, 14, 15, 16, 18, 19, 20, 22, 25, 28, 30, 32, 35, 38, 40, 45, 50, 60 };
        static readonly double[] Spessori = { 1.0, 1.2, 1.5, 1.6, 2.0, 2.5, 3.0 };

        /// <summary>Proposta di rinforzo: il tubo del primo cedimento sostituito dal piu' leggero (diametro uguale o maggiore, spessore commerciale)
        /// che porta la sopravvivenza almeno al vento voluto con il 20 % di margine. null se non serve o non si trova.</summary>
        public static Tubo Rinforzo(List<Tubo> tubi, Lega lega, double ghiaccio, bool spina, double vVoluto, out double vNuovo, out double dMassa, out int quale)
        {
            vNuovo = 0; dMassa = 0;
            Esito e = Elemento(tubi, lega, vVoluto * 1.2, ghiaccio, spina);
            quale = e.TuboMax;
            if (e.Max < 1) return null;
            Tubo t0 = tubi[quale], best = null; double mBest = double.MaxValue;
            foreach (double dmm in Diametri)
            {
                double d = dmm / 1000;
                if (d < t0.D - 1e-6 || d > t0.D + 0.012) continue;
                foreach (double smm in Spessori)
                {
                    double s = smm / 1000;
                    if (s >= d / 2) continue;
                    List<Tubo> prova = new List<Tubo>(tubi);
                    Tubo nt = new Tubo { Da = t0.Da, A = t0.A, D = d, S = s };
                    prova[quale] = nt;
                    double m = Massa(nt);
                    if (m >= mBest) continue;
                    if (Elemento(prova, lega, vVoluto * 1.2, ghiaccio, spina).Max < 1) { best = nt; mBest = m; }
                }
            }
            if (best != null)
            {
                List<Tubo> p2 = new List<Tubo>(tubi); p2[quale] = best;
                vNuovo = Sopravvivenza(p2, lega, ghiaccio, spina);
                dMassa = 2 * (Massa(best) - Massa(t0));
            }
            return best;
        }

        // ------------------------------------------------------------------------------------------------
        // boom (trave sul mast con i pesi degli elementi), tirante, mast
        // ------------------------------------------------------------------------------------------------
        public class Boom
        {
            public double Lunghezza = 6, D = 0.050, S = 0.002, PosMast = 3;   // posizione del mast dall'inizio del boom (m)
            public List<double[]> Elementi = new List<double[]>();             // (posizione lungo il boom, massa kg, area m2)
            public double AltezzaTirante = 0;                                     // 0 = senza tirante
            public double AttaccoTirante = 0.7;                                   // frazione del mezzo boom dove si attacca il tirante
        }
        public class EsitoBoom
        {
            public double[] X, Rapporto; public double Max, XMax, MomentoV, MomentoH, Massa, Tiro, Squilibrio, PosEquilibrio, Area;
        }

        /// <summary>Boom: pesi degli elementi (verticale) e vento perpendicolare al boom (orizzontale). Il tirante toglie il momento verticale
        /// tra il mast e l'attacco (la parte oltre l'attacco resta a sbalzo).</summary>
        public static EsitoBoom CalcolaBoom(Boom b, Lega lega, double v, double ghiaccio)
        {
            EsitoBoom r = new EsitoBoom();
            int n = 400;
            r.X = new double[n + 1]; r.Rapporto = new double[n + 1];
            double q = Pressione(v), de = b.D + 2 * ghiaccio, cf = Cf(b.Lunghezza, de);
            double di = b.D - 2 * b.S;
            double gb = G * (DensAl * Math.PI * (b.D * b.D - di * di) / 4 + DensGhiaccio * Math.PI * (de * de - b.D * b.D) / 4), wb = q * cf * de;
            r.Massa = DensAl * Math.PI * (b.D * b.D - di * di) / 4 * b.Lunghezza;
            r.Area = de * b.Lunghezza;
            double W = Modulo(b.D, b.S);
            // momento in x dai carichi tra x e l'estremo dalla stessa parte rispetto al mast (verticale: pesi; orizzontale: vento sul boom)
            Func<double, double[]> M = x =>
            {
                bool sx = x < b.PosMast;
                double l = sx ? x : b.Lunghezza - x;
                double mv = gb * l * l / 2, mh = wb * l * l / 2;
                foreach (double[] e in b.Elementi)
                    if ((sx && e[0] < x) || (!sx && e[0] > x)) mv += G * e[1] * Math.Abs(e[0] - x);
                return new[] { mv, mh };
            };
            // tirante: reazione R all'attacco (distanza a dal mast) tesa per annullare il momento verticale al mast: R = M0 / a,
            // tra il mast e l'attacco M(d) = M0(d) - R (a - d)
            double[] rT = new double[2], aT = new double[2], m0T = new double[2];
            for (int lato = 0; lato < 2; lato++)
            {
                double half = lato == 0 ? b.PosMast : b.Lunghezza - b.PosMast;
                aT[lato] = b.AttaccoTirante * half;
                m0T[lato] = M(lato == 0 ? b.PosMast - 1e-9 : b.PosMast + 1e-9)[0];
                rT[lato] = b.AltezzaTirante > 0 && aT[lato] > 0 ? m0T[lato] / aT[lato] : 0;
            }
            double mvMax = 0, mhMax = 0;
            for (int k = 0; k <= n; k++)
            {
                double x = b.Lunghezza * k / n; r.X[k] = x;
                double[] m = M(x);
                double mv = m[0];
                int lato = x < b.PosMast ? 0 : 1;
                double d = Math.Abs(x - b.PosMast);
                if (rT[lato] > 0 && d < aT[lato]) mv = Math.Abs(mv - rT[lato] * (aT[lato] - d));
                double sig = Math.Sqrt(mv * mv + m[1] * m[1]) / W;
                r.Rapporto[k] = sig / lega.Fy;
                if (r.Rapporto[k] > r.Max) { r.Max = r.Rapporto[k]; r.XMax = x; }
                mvMax = Math.Max(mvMax, mv); mhMax = Math.Max(mhMax, m[1]);
            }
            r.MomentoV = mvMax; r.MomentoH = mhMax;
            if (b.AltezzaTirante > 0)
            {
                int lt = rT[0] >= rT[1] ? 0 : 1;
                double th = Math.Atan2(b.AltezzaTirante, Math.Max(aT[lt], 1e-6));
                r.Tiro = rT[lt] / Math.Sin(th);
            }
            // squilibrio: momento torcente del vento sul mast (aree degli elementi e del boom ai due lati) e posizione di equilibrio
            double sa = 0, sax = 0;
            foreach (double[] e in b.Elementi) { sa += e[2]; sax += e[2] * e[0]; }
            sa += r.Area; sax += r.Area * b.Lunghezza / 2;
            r.PosEquilibrio = sa > 0 ? sax / sa : b.PosMast;
            r.Squilibrio = q * 1.2 * Math.Abs(sax - sa * b.PosMast);
            return r;
        }

        /// <summary>Mast d'acciaio a sbalzo sopra il cuscinetto: tensione (Pa) per la forza del vento F (N) a una distanza h (m).</summary>
        public static double TensioneMast(double forza, double sbalzo, double d, double s) { return forza * sbalzo / Modulo(d, s); }

        /// <summary>
        /// Copia del modello con gli elementi orizzontali piegati sotto il peso: ogni filo dell'elemento (anche rastremato) diventa una catena di
        /// fili da un segmento con la stessa etichetta, gli estremi abbassati della freccia alla loro distanza dal centro. Segmenti, sorgenti e
        /// carichi restano dove sono. freccia(e, x) = abbassamento (m) a distanza x dal centro dell'elemento e.
        /// </summary>
        public static NecModel ApplicaFreccia(NecModel m0, List<Staffe.Elemento> elems, Func<Staffe.Elemento, double, double> freccia, List<string> note)
        {
            NecModel m = m0.Clone();
            Dictionary<int, List<GeomCard>> sost = new Dictionary<int, List<GeomCard>>();
            foreach (Staffe.Elemento e in elems)
            {
                if (Math.Abs(e.Uz) > 0.3) { note.Add((e.Nome ?? "elemento") + ": non orizzontale, lasciato diritto."); continue; }
                foreach (int ic in e.Schede)
                {
                    GeomCard g = m.Geometry[ic];
                    List<GeomCard> pezzi = OggettiExtra.Espandi(m, new List<GeomCard> { g });
                    List<GeomCard> nuovi = new List<GeomCard>();
                    foreach (GeomCard p in pezzi)
                    {
                        int ns = Math.Max(1, p.I2);
                        for (int k = 0; k < ns; k++)
                        {
                            double a = (double)k / ns, b = (double)(k + 1) / ns;
                            double[] p1 = { p.F1 + (p.F4 - p.F1) * a, p.F2 + (p.F5 - p.F2) * a, p.F3 + (p.F6 - p.F3) * a };
                            double[] p2 = { p.F1 + (p.F4 - p.F1) * b, p.F2 + (p.F5 - p.F2) * b, p.F3 + (p.F6 - p.F3) * b };
                            Func<double[], double> dist = q => Math.Abs((q[0] - e.Cx) * e.Ux + (q[1] - e.Cy) * e.Uy + (q[2] - e.Cz) * e.Uz);
                            p1[2] -= freccia(e, dist(p1)); p2[2] -= freccia(e, dist(p2));
                            nuovi.Add(new GeomCard { Id = "GW", I1 = p.I1, I2 = 1, F1 = p1[0], F2 = p1[1], F3 = p1[2], F4 = p2[0], F5 = p2[1], F6 = p2[2], F7 = p.F7 });
                        }
                    }
                    sost[ic] = nuovi;
                }
            }
            List<GeomCard> geo = new List<GeomCard>();
            for (int i = 0; i < m.Geometry.Count; i++) if (sost.ContainsKey(i)) geo.AddRange(sost[i]); else geo.Add(m.Geometry[i]);
            m.Geometry = geo;
            if (!m.Comments.Exists(x => x.StartsWith("Meccanica:", StringComparison.Ordinal))) m.Comments.Add("Meccanica: elementi piegati dalla freccia sotto il peso (YA-NEC)");
            return m;
        }

        /// <summary>Freccia in punta di una trave a sbalzo a sezione costante con carico uniforme w (N/m): w L^4 / (8 E I) (Roark).</summary>
        public static double FrecciaChiusa(double w, double L, double ei) { return w * Math.Pow(L, 4) / (8 * ei); }
    }
}
