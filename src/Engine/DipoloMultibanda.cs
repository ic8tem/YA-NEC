// YA-NEC 0.4.30 - dipolo multibanda (sottolinguetta dei Calcolatori). Tre modi proposti da IC8TEM il 14/11/2026 21:26-21:27 ("oppure il
// dipolo come quello della force 12 come terza ipotesi", "e proponimi queste pure"), approvati alle 23:11; modello Force 12 scelto alle
// 23:18 ("xr5").
//   0 bacchette parallele: un dipolo per banda, uno sotto l'altro a distanza d, tutti sulla stessa alimentazione (i bracci di ogni lato
//     uniti da un filo verticale verso il morsetto del dipolo di sopra);
//   1 manica aperta (open sleeve): si alimenta solo il dipolo della banda piu' bassa; gli altri sono parassiti nello stesso piano,
//     paralleli, alternati davanti e dietro, a distanza s (N6BT);
//   2 radiatori della Force 12 XR-5: due open sleeve a 3 bande di T. Schiller N6BT (manuale XR-5, pag. 2): il coassiale va al radiatore
//     12 m, che eccita il 10 m davanti e il 15 m dietro; una linea "jacketed" da 450 ohm lo collega al radiatore 20 m, a 4" dal 15 m, che
//     eccita anche il 17 m dietro. Tubi e posizioni dal modello "Force12 XR-5 misure di fabbrica" (data/modelli), senza i riflettori e il
//     direttore. Senza riflettori le misure di fabbrica non vanno (prova con nec2c: ROS 13 sul 17 m, 12 sul 15 m): l'accordo sposta le
//     punte, le distanze del 17, 15 e 10 m e la lunghezza della linea.
// Accordo: discesa per coordinate con ricerca aurea su ogni variabile, obiettivo somma su tutte le bande di (ROS al centro banda - 1)^2:
// le bande si influenzano, quindi si accordano insieme. Prove con nec2c (prototipo scratchpad/dipmb, spazio libero):
//   bacchette 20/15/10 m, filo 2 mm, d 0,1 m: 20 m 10,26 m ROS 1,21; 15 m 6,87 m 1,03; 10 m 5,01 m 1,19 (con d 0,3 m il 10 m resta a
//     ROS 2,2: i fili verticali verso i morsetti, lunghi 0,6 m, fanno da stub); quattro dipoli (IC8TEM 23:32) 40/20/15/10 m con d 0,1 m:
//     ROS al centro 1,53 / 1,09 / 1,69 / 1,58; 20/17/15/10 m: 1,19 / 1,11 / 1,21 / 1,50; 20/15/12/10 m: 1,21 / 1,12 / 1,31 / 1,50
//   manica 20/15/10 m, tubo 20 mm: 20 m 10,11 m ROS 1,52; 15 m 6,81 m a 0,155 m ROS 1,17; 10 m 5,11 m a 0,109 m ROS 1,08
//   XR-5 senza riflettori, accordata: ROS al centro 1,39 / 1,21 / 1,06 / 1,45 / 1,05 (20/17/15/12/10 m), linea 0,44 m
// 0.4.30 (IC8TEM 23:40): trappole sulle bacchette parallele per bande in piu' (da 4 dipoli a 6 bande): il dipolo scelto riceve in ogni
// braccio, dopo la sua punta, una trappola lunga 10 cm e una coda. Tre tipi di accorciamento:
//   0 trappola a sola bobina (autorisonante): la capacita' e' quella propria della bobina (Medhurst, C = H D), il numero di spire si
//     cerca perche' la bobina risuoni sulla banda del dipolo: sopra la risonanza isola la coda, sotto la carica (LD 1 parallelo);
//   1 bobina di carico + coda (LD 0 serie, induttanza da accordare): non isola la coda, va bene solo per bande circa all'ottava;
//   2 trappola LC (bobina + condensatore scelto, 25 pF di partenza), risonante sulla banda del dipolo (LD 1 parallelo).
// Bobina: formula di Wheeler L = r^2 n^2 / (9 r + 10 l) (pollici, microhenry), spire accostate con passo 1,05 volte il filo; perdite
// dal Q (R serie = X / Q per la bobina di carico, R parallelo = Q X per le trappole).
// Prova con nec2c (scratchpad/dipmb/fan6b.py), bacchette 40/20/15/10 m d 0,1 m, trappole sul 15 m (-> 30 m) e sul 10 m (-> 17 m):
//   sola bobina (supporto 50 mm, filo 1,5 mm): ROS al centro 1,95 / 1,47 / 1,87 / 2,49 / 1,59 / 1,58 (40/30/20/17/15/10 m);
//   LC 25 pF: 1,18 / 1,12 / 1,03 / 1,14 / 1,06 / 1,10; code 2,66 m (30 m) e 1,00 m (17 m) per lato.
//   Da sola, la bobina autorisonante (pochi pF) isola bene solo bande all'ottava (20 m -> 40 m: ROS 1,11 / 1,10); per 15 -> 17 m o
//   10 -> 12 m la sua reattanza resta troppo alta e la coda non lavora (ROS 4-6): serve il condensatore.
// Rastremazione (23:40): bracci e parassiti a tubi telescopici con la schema di RastremazioneHf (modi 0 e 1; la XR-5 ha gia' i suoi).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;

namespace YaNec.Engine
{
    public class DmSpec
    {
        public int Modo;
        public List<string> Bande = new List<string> { "20 m", "15 m", "10 m" };
        public double Diametro = 0.002;           // m (modi 0 e 1)
        public double Distanza = 0.1, Gap = 0.05; // modo 0: distanza tra i dipoli, morsetti (m)
        public bool Terreno;                      // false = spazio libero; true = terreno reale (Sommerfeld) alla quota
        public double Quota = 10, Er = 13, Sigma = 0.005;
        public Dictionary<string, double> L = new Dictionary<string, double>();   // modi 0-1: lunghezza; modo 2: allungamento di ogni punta
        public Dictionary<string, double> S = new Dictionary<string, double>();   // modo 1: distanza dal radiatore; modo 2: spostamento lungo il boom
        public double Linea = 0.586154;           // modo 2: lunghezza elettrica della linea da 450 ohm (21" con VF 0,91)
        public NecModel Fabbrica;                 // modo 2
        public bool Rastrema;                     // modi 0 e 1: tubi telescopici
        public string SchemaRastr = RastremazioneHf.Predefinita;
        public List<DmTrappola> Trappole = new List<DmTrappola>();   // modo 0
        public DmSpec Clone()
        {
            DmSpec c = (DmSpec)MemberwiseClone();
            c.Bande = new List<string>(Bande); c.L = new Dictionary<string, double>(L); c.S = new Dictionary<string, double>(S);
            c.Trappole = Trappole.ConvertAll(t => t.Clone());
            return c;
        }
    }

    public class DmTrappola
    {
        public string Dipolo = "15 m", Nuova = "30 m";
        public int Tipo = 2;                      // 0 sola bobina autorisonante, 1 bobina di carico + coda, 2 trappola LC
        public double Luh = 5, Cpf = 25, Q = 150; // induttanza (uH), capacita' (pF), Q della bobina
        public double Coda = 1.0;                 // coda per lato (m)
        public double Supporto = 0.05, Filo = 0.0015;   // diametro del supporto e del filo della bobina (m)
        public double Spire, LungBobina, Cpropria;        // risultato (bobina)
        public DmTrappola Clone() { return (DmTrappola)MemberwiseClone(); }
    }

    public class DmBanda
    {
        public string Banda, Misure = "", Ruba = "";
        public Complex Z;
        public double RosLo, RosC, RosHi, G;
    }

    public static class DipoloMultibanda
    {
        static readonly CultureInfo IV = CultureInfo.InvariantCulture, IT = CultureInfo.GetCultureInfo("it-IT");
        public static readonly string[] Modi = { "bacchette parallele", "manica aperta (open sleeve)", "radiatori Force 12 XR-5" };
        public static readonly string[] BandeHf = { "40 m", "30 m", "20 m", "17 m", "15 m", "12 m", "10 m" };
        public static readonly string[] BandeXr5 = { "20 m", "17 m", "15 m", "12 m", "10 m" };
        // radiatori dell'XR-5 nel modello di fabbrica: posizione lungo il boom (m)
        static readonly Dictionary<string, double> XXr5 = new Dictionary<string, double> { { "17 m", 3.00355 }, { "20 m", 3.35915 }, { "15 m", 3.4798 }, { "12 m", 3.8989 }, { "10 m", 4.14655 } };
        public const string FileXr5 = "Force12 XR-5 misure di fabbrica.nec";

        /// <summary>Posizione lungo il boom (x, m) del radiatore della banda b.</summary>
        public static double XRadiatore(DmSpec s, string b)
        {
            if (s.Modo == 2) return XXr5[b] + (s.S.ContainsKey(b) ? s.S[b] : 0);
            if (s.Modo == 1) { int k = s.Bande.IndexOf(b); return k <= 0 ? 0 : s.S[b] * (k % 2 == 1 ? 1 : -1); }
            return 0;
        }
        /// <summary>Quota del radiatore della banda b (m).</summary>
        public static double ZRadiatore(DmSpec s, string b)
        {
            double h = s.Terreno ? s.Quota : 0;
            return s.Modo == 0 ? h - s.Bande.IndexOf(b) * s.Distanza : h;
        }
        public static double Lambda(string b) { return Lam(b); }

        public static Band Banda(string n) { return Bands.All.Find(b => b.Name == n); }
        static double Lam(string b) { return 299.792458 / Banda(b).Operating; }

        /// <summary>Bande dei dipoli piu' quelle aggiunte dalle trappole (modo 0), dalla piu' bassa.</summary>
        public static List<string> TutteBande(DmSpec s)
        {
            List<string> r = new List<string>(s.Bande);
            if (s.Modo == 0) foreach (DmTrappola t in s.Trappole) if (!r.Contains(t.Nuova) && Banda(t.Nuova) != null && s.Bande.Contains(t.Dipolo)) r.Add(t.Nuova);
            r.Sort((a, b) => Banda(a).Operating.CompareTo(Banda(b).Operating));
            return r;
        }

        // H di Medhurst in funzione di lunghezza / diametro della bobina (C propria = H x D in cm, pF)
        static readonly double[,] Medhurst = { { 0.1, 0.96 }, { 0.2, 0.79 }, { 0.4, 0.64 }, { 0.6, 0.56 }, { 0.8, 0.50 }, { 1.0, 0.46 }, { 1.5, 0.46 }, { 2, 0.50 }, { 3, 0.61 }, { 4, 0.72 }, { 5, 0.81 }, { 7, 1.01 }, { 10, 1.32 } };
        static double HMedhurst(double x)
        {
            int n = Medhurst.GetLength(0);
            if (x <= Medhurst[0, 0]) return Medhurst[0, 1];
            for (int i = 1; i < n; i++)
                if (x <= Medhurst[i, 0]) return Medhurst[i - 1, 1] + (Medhurst[i, 1] - Medhurst[i - 1, 1]) * (x - Medhurst[i - 1, 0]) / (Medhurst[i, 0] - Medhurst[i - 1, 0]);
            return Medhurst[n - 1, 1];
        }
        /// <summary>Bobina di n spire accostate: induttanza (H, Wheeler), capacita' propria (F, Medhurst), lunghezza (m).</summary>
        static void Bobina(double n, double D, double filo, out double L, out double Cs, out double lung)
        {
            lung = n * filo * 1.05;
            double r = D / 2 / 0.0254, l = lung / 0.0254;
            L = r * r * n * n / (9 * r + 10 * l) * 1e-6;
            Cs = HMedhurst(lung / D) * D * 100 * 1e-12;
        }
        static double SpirePer(Func<double, bool> troppe)
        {
            double lo = 0.5, hi = 5000;
            for (int i = 0; i < 80; i++) { double m = (lo + hi) / 2; if (troppe(m)) hi = m; else lo = m; }
            return (lo + hi) / 2;
        }
        /// <summary>Induttanza e capacita' della trappola, spire e lunghezza della bobina.</summary>
        public static void Dimensiona(DmTrappola t)
        {
            double f0 = Banda(t.Dipolo).Operating * 1e6, w0 = 2 * Math.PI * f0, L, Cs, lung;
            if (t.Tipo == 0)
            {
                t.Spire = SpirePer(n => { double l1, c1, g1; Bobina(n, t.Supporto, t.Filo, out l1, out c1, out g1); return 1 / (2 * Math.PI * Math.Sqrt(l1 * c1)) < f0; });
                Bobina(t.Spire, t.Supporto, t.Filo, out L, out Cs, out lung);
                t.Luh = L * 1e6; t.Cpf = Cs * 1e12;
            }
            else
            {
                if (t.Tipo == 2) t.Luh = 1e6 / (w0 * w0 * Math.Max(1, t.Cpf) * 1e-12);
                double voluta = t.Luh * 1e-6;
                t.Spire = SpirePer(n => { double l1, c1, g1; Bobina(n, t.Supporto, t.Filo, out l1, out c1, out g1); return l1 > voluta; });
                Bobina(t.Spire, t.Supporto, t.Filo, out L, out Cs, out lung);
            }
            t.LungBobina = lung; t.Cpropria = Cs * 1e12;
        }

        public static void Ordina(DmSpec s)
        {
            s.Bande.Sort((a, b) => Banda(a).Operating.CompareTo(Banda(b).Operating));
        }

        /// <summary>Valori di partenza: dipoli 0,48 lambda; parassiti a 0,1 m; XR-5 come di fabbrica.</summary>
        public static void Iniziale(DmSpec s)
        {
            s.L.Clear(); s.S.Clear();
            if (s.Modo == 2)
            {
                s.Bande = new List<string>(BandeXr5);
                foreach (string b in BandeXr5) { s.L[b] = 0; s.S[b] = 0; }
                s.Linea = 0.586154;
                if (s.Fabbrica == null)
                {
                    string f = Path.Combine(AppPaths.ModelsDir, FileXr5);
                    if (!YanecFile.Esiste(f)) throw new NecException("manca il modello di fabbrica dell'XR-5 (" + f + ")");
                    s.Fabbrica = NecModel.Load(f);
                }
                return;
            }
            Ordina(s);
            for (int k = 0; k < s.Bande.Count; k++) { s.L[s.Bande[k]] = 0.48 * Lam(s.Bande[k]); s.S[s.Bande[k]] = k == 0 ? 0 : 0.1; }
            foreach (DmTrappola t in s.Trappole) { Dimensiona(t); t.Coda = 0.06 * Lam(t.Nuova); }
        }

        static double FMax(DmSpec s) { double m = 0; foreach (string b in s.Bande) m = Math.Max(m, Banda(b).High); return m; }

        /// <summary>Modello NEC alla frequenza f; tagBanda: banda di ogni filo.</summary>
        public static NecModel Modello(DmSpec s, double f, out Dictionary<int, string> tagBanda)
        {
            Dictionary<int, string> bandaDi = new Dictionary<int, string>();   // 0.4.31: copia locale, un parametro out non si usa nella lambda (CS1628)
            tagBanda = bandaDi;
            StringBuilder sb = new StringBuilder();
            sb.Append("CM YA-NEC - dipolo multibanda, " + Modi[s.Modo] + ": " + string.Join(", ", s.Bande) + "\n");
            if (s.Modo == 2) sb.Append("CM radiatori della Force 12 XR-5 (misure di fabbrica, senza riflettori e direttore), linea 450 ohm\nCM YA-NEC LEESON=1\n");
            sb.Append("CE\n");
            double h = s.Terreno ? s.Quota : 0, seg = 299.792458 / FMax(s) / 20, r = s.Diametro / 2;
            int tag = 0, ts = 1, ss = 1;
            List<string> tl = new List<string>();
            Action<int, double, double, double, double, double, double, double, string> gw = (n, x1, y1, z1, x2, y2, z2, rr, b) =>
            {
                tag++;
                sb.Append(string.Format(IV, "GW {0} {1} {2:0.#####} {3:0.#####} {4:0.#####} {5:0.#####} {6:0.#####} {7:0.#####} {8:0.######}\n", tag, n, x1, y1, z1, x2, y2, z2, rr));
                bandaDi[tag] = b;
            };
            List<string> ld = new List<string>();
            bool ras = s.Rastrema && s.Modo != 2;
            double rc = ras ? RastremazioneHf.DiametroCentro(s.SchemaRastr) / 2 : r, rp = ras ? RastremazioneHf.DiametroPunta(s.SchemaRastr) / 2 : r;
            if (s.Modo == 0)
            {
                double g = s.Gap;
                for (int k = 0; k < s.Bande.Count; k++)
                {
                    string b = s.Bande[k];
                    double L = s.L[b], z = h - k * s.Distanza;
                    int n = Math.Max(3, (int)Math.Ceiling((L / 2 - g / 2) / seg));
                    // con la rastremazione i bracci partono dal centro: sinistro "<" (riga -2, centro in E2), destro ">" (riga -1, centro in E1)
                    gw(n, 0, -L / 2, z, 0, -g / 2, z, ras ? -2 : r, b);
                    gw(n, 0, g / 2, z, 0, L / 2, z, ras ? -1 : r, b);
                    if (k == 0) { gw(1, 0, -g / 2, z, 0, g / 2, z, rc, b); ts = tag; ss = 1; }
                    else
                    {
                        int nb = Math.Max(1, (int)Math.Ceiling(s.Distanza / seg));
                        gw(nb, 0, -g / 2, z, 0, -g / 2, z + s.Distanza, rc, b);
                        gw(nb, 0, g / 2, z, 0, g / 2, z + s.Distanza, rc, b);
                    }
                    foreach (DmTrappola t in s.Trappole)
                    {
                        if (t.Dipolo != b || Banda(t.Nuova) == null) continue;
                        double fn = Banda(t.Nuova).Operating * 1e6, f0 = Banda(b).Operating * 1e6, Lh = t.Luh * 1e-6;
                        foreach (int sg in new[] { -1, 1 })
                        {
                            double y0 = sg * L / 2, y1 = y0 + sg * 0.1;
                            gw(1, 0, y0, z, 0, y1, z, rp, t.Nuova);
                            if (t.Tipo == 1) ld.Add(string.Format(IV, "LD 0 {0} 1 1 {1:G6} {2:G6} 0", tag, 2 * Math.PI * fn * Lh / Math.Max(1, t.Q), Lh));
                            else ld.Add(string.Format(IV, "LD 1 {0} 1 1 {1:G6} {2:G6} {3:G6}", tag, Math.Max(1, t.Q) * 2 * Math.PI * f0 * Lh, Lh, t.Cpf * 1e-12));
                            int nt = Math.Max(1, (int)Math.Ceiling(t.Coda / seg));
                            gw(nt, 0, y1, z, 0, y1 + sg * t.Coda, z, rp, t.Nuova);
                        }
                    }
                }
            }
            else if (s.Modo == 1)
            {
                for (int k = 0; k < s.Bande.Count; k++)
                {
                    string b = s.Bande[k];
                    double L = s.L[b], x = k == 0 ? 0 : s.S[b] * (k % 2 == 1 ? 1 : -1);
                    int n = Math.Max(3, (int)Math.Ceiling(L / seg)); if (n % 2 == 0) n++;
                    gw(n, x, -L / 2, h, x, L / 2, h, ras ? -3 : r, b);
                    if (k == 0) { ts = tag; ss = (n + 1) / 2; }
                }
            }
            else
            {
                if (s.Fabbrica == null) Iniziale(s);
                int t12 = 0, t20 = 0;
                foreach (string b in BandeXr5)
                {
                    List<GeomCard> el = s.Fabbrica.Geometry.FindAll(c => c.Id == "GW" && Math.Abs(c.F1 - XXr5[b]) < 1e-4 && Math.Abs(c.F4 - XXr5[b]) < 1e-4);
                    double ymax = 0;
                    foreach (GeomCard c in el) ymax = Math.Max(ymax, Math.Max(Math.Abs(c.F2), Math.Abs(c.F5)));
                    double dl = s.L.ContainsKey(b) ? s.L[b] : 0, x = XXr5[b] + (s.S.ContainsKey(b) ? s.S[b] : 0);
                    foreach (GeomCard c in el)
                    {
                        double y1 = c.F2, y2 = c.F5;
                        if (Math.Abs(Math.Abs(y1) - ymax) < 1e-6) y1 += Math.Sign(y1) * dl;
                        if (Math.Abs(Math.Abs(y2) - ymax) < 1e-6) y2 += Math.Sign(y2) * dl;
                        gw(c.I2, x, y1, c.F3 + h, x, y2, c.F6 + h, c.F7, b);
                        // il tubo centrale (attraversa y = 0) dei due radiatori divisi: 12 m alimentato, 20 m collegato dalla linea
                        if (y1 < 0 && y2 > 0 && b == "12 m") t12 = tag;
                        if (y1 < 0 && y2 > 0 && b == "20 m") t20 = tag;
                    }
                }
                ts = t12; ss = 6;
                tl.Add(string.Format(IV, "TL {0} 6 {1} 8 450 {2:0.######} 0 0 0 0", t12, t20, s.Linea));
            }
            sb.Append(s.Terreno ? "GE 1\n" : "GE 0\n");
            if (s.Terreno) sb.Append(string.Format(IV, "GN 2 0 0 0 {0} {1}\n", s.Er, s.Sigma));
            foreach (string t in ld) sb.Append(t + "\n");
            foreach (string t in tl) sb.Append(t + "\n");
            sb.Append(string.Format(IV, "EX 0 {0} {1} 0 1 0\nFR 0 1 0 0 {2:0.####} 0\nEN\n", ts, ss, f));
            NecModel m = NecModel.Parse(sb.ToString());
            if (s.Modo == 2) m.Leeson = true;
            if (ras)
            {
                m.Rastremazioni.Add(RastremazioneHf.Riga(s.SchemaRastr, 1, 1));
                m.Rastremazioni.Add(RastremazioneHf.Riga(s.SchemaRastr, 2, 2));
                m.Rastremazioni.Add(RastremazioneHf.Riga(s.SchemaRastr, 3, 0));
                m.Leeson = true;
            }
            return m;
        }

        /// <summary>Esportazione in .nec per costruirci una Yagi multibanda HF (IC8TEM 14/11/2026 23:33): il sistema di radiatori accordato con
        /// il boom sull'asse X (gli elementi lungo Y), le etichette dei fili di ogni banda e le misure nelle righe CM; riflettori dietro (x
        /// negativo) e direttori davanti (x positivo) si aggiungono nel modello. FR su tutte le bande del dipolo, dalla piu' bassa.</summary>
        public static string EsportaYagi(DmSpec s, List<DmBanda> esito)
        {
            if (s.Modo != 2) Ordina(s);
            Dictionary<int, string> tb;
            NecModel m = Modello(s, Banda(s.Bande[0]).Operating, out tb);
            StringBuilder c = new StringBuilder();
            c.Append("CM YA-NEC " + AppPaths.Version + " - radiatore multibanda per Yagi HF (Calcolatori > Dipolo multibanda), " + Modi[s.Modo] + "\n");
            c.Append("CM boom = asse X, elementi lungo Y: riflettori dietro (x < 0), direttori davanti (x > 0); " + (s.Terreno ? string.Format(IT, "terreno medio, quota {0:0.##} m", s.Quota) : "spazio libero") + "\n");
            foreach (string b in TutteBande(s))
            {
                List<string> tags = new List<string>();
                foreach (KeyValuePair<int, string> kv in tb) if (kv.Value == b) tags.Add(kv.Key.ToString(IV));
                DmTrappola tr = s.Modo == 0 ? s.Trappole.Find(q => q.Nuova == b && s.Bande.Contains(q.Dipolo)) : null;
                string mis = tr != null ? DescriviTrappola(tr) : s.Modo == 2 ? string.Format(IT, "punte {0:+0.000;-0.000} m, spostamento {1:+0.000;-0.000} m", s.L[b], s.S[b])
                                         : string.Format(IT, "lunghezza {0:0.000} m", s.L[b]) + (s.Modo == 1 && s.S[b] > 0 ? string.Format(IT, ", a {0:0.000} m dal radiatore", s.S[b]) : "");
                DmBanda d = esito != null ? esito.Find(x => x.Banda == b) : null;
                c.Append("CM " + b + " (" + Banda(b).Operating.ToString("0.###", IT) + " MHz): fili " + string.Join(" ", tags) + "; " + mis +
                         (d != null ? string.Format(IT, "; ROS al centro {0:0.00} da solo", d.RosC) : "; misure non accordate") + "\n");
            }
            if (s.Modo == 2) c.Append(string.Format(IT, "CM linea da 450 ohm tra 12 m e 20 m: {0:0.000} m elettrici\n", s.Linea));
            c.Append("CM con riflettori e direttori i radiatori vanno riaccordati (Ottimizzatore): le bande si influenzano\n");
            return c.ToString() + m.ToNec();
        }

        static Nec2Engine Risolvi(NecModel m, SegmentGeometry g, double f)
        {
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            return e;
        }

        public static double Ros(DmSpec s, double f, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Dictionary<int, string> tb;
            NecModel m = Modello(s, f, out tb);
            return Analysis.Swr(Risolvi(m, Analysis.GeometriaDiCalcolo(m, f), f).Sources[0].Z, 50);   // 0.4.31: con Leeson come la verifica
        }

        static double Obiettivo(DmSpec s, CancellationToken ct)
        {
            double o = 0;
            foreach (string b in TutteBande(s)) { double r = Ros(s, Banda(b).Operating, ct); o += (r - 1) * (r - 1); }
            // 1.0.7: penalita' se due elementi della manica aperta sono piu' vicini della distanza minima (il NEC li calcolerebbe male)
            if (s.Modo == 1)
            {
                double dm = DistanzaMinima(s), dd = DistanzaPiuVicina(s);
                if (dd < dm) o += 100 * Math.Pow((dm - dd) / dm, 2) + 1;
            }
            return o;
        }

        /// <summary>1.0.7 (IC8TEM 16/11/2026 19:12): distanza minima tra gli assi di due elementi della manica aperta: 3 diametri
        /// (almeno 2 cm). Sotto, il NEC-2 non e' affidabile (tubi quasi a contatto: con 20 mm a 2,1 cm dava 10,6 dBi in 15 m).</summary>
        public static double DistanzaMinima(DmSpec s) { return Math.Max(0.02, 3 * s.Diametro); }

        /// <summary>Distanza piu' piccola tra gli assi di due elementi della manica aperta (m).</summary>
        public static double DistanzaPiuVicina(DmSpec s)
        {
            double d = double.MaxValue;
            for (int i = 0; i < s.Bande.Count; i++)
                for (int j = i + 1; j < s.Bande.Count; j++)
                {
                    if (!s.S.ContainsKey(s.Bande[i]) && i > 0 || !s.S.ContainsKey(s.Bande[j])) continue;
                    d = Math.Min(d, Math.Abs(XRadiatore(s, s.Bande[i]) - XRadiatore(s, s.Bande[j])));
                }
            return d;
        }

        public static double Aurea(Func<double, double> F, double a, double c, int it)
        {
            double gr = (Math.Sqrt(5) - 1) / 2, x1 = c - gr * (c - a), x2 = a + gr * (c - a), f1 = F(x1), f2 = F(x2);
            for (int i = 0; i < it; i++)
            {
                if (f1 < f2) { c = x2; x2 = x1; f2 = f1; x1 = c - gr * (c - a); f1 = F(x1); }
                else { a = x1; x1 = x2; f1 = f2; x2 = a + gr * (c - a); f2 = F(x2); }
            }
            return (a + c) / 2;
        }

        /// <summary>Accordo di tutte le bande insieme (discesa per coordinate, 4 giri). Si tiene il nuovo valore solo se migliora.</summary>
        public static void Accorda(DmSpec s, CancellationToken ct, Action<string> avanzamento = null)
        {
            if (s.Modo != 2) Ordina(s);
            // 0.4.31 (collaudo 0.4.30: 6 bande a sola bobina, 17 m ROS 3,08): prima i dipoli senza trappole, poi le trappole sui dipoli
            // gia' accordati, come nella prova con nec2c (dipmb/fan6b.py parte dalle lunghezze accordate delle 4 bande)
            if (s.Modo == 0 && s.Trappole.Count > 0)
            {
                DmSpec base0 = s.Clone();
                base0.Trappole.Clear();
                Accorda(base0, ct, avanzamento);
                s.L = new Dictionary<string, double>(base0.L);
                s.S = new Dictionary<string, double>(base0.S);
            }
            List<Tuple<string, Func<double>, Action<double>, double, double>> v = Variabili(s);
            // code delle trappole: prima una griglia (la discesa da sola si ferma lontano), poi la discesa con tutto il resto
            if (s.Modo == 0)
                foreach (DmTrappola t in s.Trappole)
                {
                    if (!s.Bande.Contains(t.Dipolo) || Banda(t.Nuova) == null) continue;
                    double ln = Lam(t.Nuova), bo = double.MaxValue, bc = t.Coda;
                    foreach (double x in new[] { 0.01, 0.02, 0.04, 0.06, 0.09, 0.12, 0.16, 0.2, 0.25 })
                    {
                        t.Coda = x * ln;
                        double o = Obiettivo(s, ct);
                        if (o < bo) { bo = o; bc = t.Coda; }
                    }
                    t.Coda = bc;
                    if (avanzamento != null) avanzamento("griglia della coda " + t.Nuova);
                }
            double best = Obiettivo(s, ct);
            for (int giro = 0; giro < 4; giro++)
            {
                foreach (Tuple<string, Func<double>, Action<double>, double, double> t in v)
                {
                    double vecchio = t.Item2();
                    double nuovo = Aurea(x => { t.Item3(x); double o = Obiettivo(s, ct); t.Item3(vecchio); return o; }, t.Item4, t.Item5, 12);
                    t.Item3(nuovo);
                    double o2 = Obiettivo(s, ct);
                    if (o2 < best) best = o2; else t.Item3(vecchio);
                    if (avanzamento != null) avanzamento(string.Format(IT, "giro {0}/4, {1}: somma (ROS-1)^2 = {2:0.000}", giro + 1, t.Item1, best));
                }
            }
            foreach (DmTrappola t in s.Trappole) if (t.Tipo == 1) Dimensiona(t);   // spire per l'induttanza trovata
        }

        /// <summary>Riporta le misure accordate (lunghezze, distanze, linea, code e bobine) da una copia allo spec originale.</summary>
        public static void CopiaAccordo(DmSpec da, DmSpec a)
        {
            a.L = new Dictionary<string, double>(da.L); a.S = new Dictionary<string, double>(da.S); a.Linea = da.Linea;
            for (int i = 0; i < a.Trappole.Count && i < da.Trappole.Count; i++)
            {
                DmTrappola x = da.Trappole[i], y = a.Trappole[i];
                y.Coda = x.Coda; y.Luh = x.Luh; y.Cpf = x.Cpf; y.Spire = x.Spire; y.LungBobina = x.LungBobina; y.Cpropria = x.Cpropria;
            }
        }

        public static string DescriviTrappola(DmTrappola t)
        {
            string tipo = t.Tipo == 0 ? "sola bobina autorisonante" : t.Tipo == 1 ? "bobina di carico" : "trappola LC";
            string b = string.Format(IT, "{0:0.0} spire su {1:0} mm, lunga {2:0} mm", t.Spire, t.Supporto * 1000, t.LungBobina * 1000);
            return tipo + " sul " + t.Dipolo + string.Format(IT, ": {0:0.00} uH", t.Luh) + (t.Tipo == 1 ? "" : string.Format(IT, " // {0:0.0} pF", t.Cpf)) + " (" + b + "), coda " + string.Format(IT, "{0:0.000} m per lato", t.Coda);
        }

        /// <summary>Variabili dell'accordo dei radiatori: (nome, leggi, scrivi, minimo, massimo). Le usa anche la Yagi multibanda.</summary>
        public static List<Tuple<string, Func<double>, Action<double>, double, double>> Variabili(DmSpec s)
        {
            List<Tuple<string, Func<double>, Action<double>, double, double>> v = new List<Tuple<string, Func<double>, Action<double>, double, double>>();
            foreach (string b0 in s.Bande)
            {
                string b = b0;
                double lam = Lam(b);
                if (s.Modo == 2) v.Add(Tuple.Create<string, Func<double>, Action<double>, double, double>("punte " + b, () => s.L[b], x => s.L[b] = x, -0.6, 0.6));
                else v.Add(Tuple.Create<string, Func<double>, Action<double>, double, double>("lunghezza " + b, () => s.L[b], x => s.L[b] = x, 0.40 * lam, 0.56 * lam));
            }
            if (s.Modo == 0)
                foreach (DmTrappola t0 in s.Trappole)
                {
                    DmTrappola t = t0;
                    if (!s.Bande.Contains(t.Dipolo) || Banda(t.Nuova) == null) continue;
                    double ln = Lam(t.Nuova);
                    v.Add(Tuple.Create<string, Func<double>, Action<double>, double, double>("coda " + t.Nuova, () => t.Coda, x => t.Coda = x, 0.005 * ln, 0.30 * ln));
                    if (t.Tipo == 1) v.Add(Tuple.Create<string, Func<double>, Action<double>, double, double>("bobina " + t.Nuova, () => t.Luh, x => t.Luh = x, 0.2, 60));
                }
            if (s.Modo == 1)
                for (int k = 1; k < s.Bande.Count; k++)
                {
                    string b = s.Bande[k];
                    v.Add(Tuple.Create<string, Func<double>, Action<double>, double, double>("distanza " + b, () => s.S[b], x => s.S[b] = x, DistanzaMinima(s), Math.Max(0.3, 0.03 * Lam(b))));
                }
            if (s.Modo == 2)
            {
                double[][] lim = { new[] { -0.6, 0.6 }, new[] { -0.08, 0.35 }, new[] { -0.15, 0.6 } };
                string[] mob = { "17 m", "15 m", "10 m" };
                for (int i = 0; i < 3; i++)
                {
                    string b = mob[i];
                    v.Add(Tuple.Create<string, Func<double>, Action<double>, double, double>("posizione " + b, () => s.S[b], x => s.S[b] = x, lim[i][0], lim[i][1]));
                }
                v.Add(Tuple.Create<string, Func<double>, Action<double>, double, double>("linea", () => s.Linea, x => s.Linea = x, 0.2, 2.0));
            }
            return v;
        }

        /// <summary>Per ogni banda: Z e ROS al centro, ROS ai bordi, guadagno massimo, elemento di un'altra banda che prende corrente.</summary>
        public static List<DmBanda> Verifica(DmSpec s, CancellationToken ct)
        {
            List<DmBanda> r = new List<DmBanda>();
            foreach (string b in TutteBande(s))
            {
                Band bb = Banda(b);
                Dictionary<int, string> tb;
                NecModel m = Modello(s, bb.Operating, out tb);
                SegmentGeometry g = m.BuildGeometry();
                RunResult rr = Analysis.Run(m, g, bb.Operating, 5.0, ct);
                DmBanda d = new DmBanda { Banda = b, Z = rr.Zin, RosC = Analysis.Swr(rr.Zin, 50), G = rr.GmaxDbi };
                d.RosLo = Ros(s, bb.Low, ct); d.RosHi = Ros(s, bb.High, ct);
                DmTrappola tr = s.Modo == 0 ? s.Trappole.Find(q => q.Nuova == b && s.Bande.Contains(q.Dipolo)) : null;
                if (tr != null) d.Misure = DescriviTrappola(tr);
                else if (s.Modo == 2) d.Misure = string.Format(IT, "punte {0:+0.000;-0.000} m, spostamento {1:+0.000;-0.000} m", s.L[b], s.S[b]);
                else d.Misure = string.Format(IT, "{0:0.000} m", s.L[b]) + (s.Modo == 1 && s.S[b] > 0 ? string.Format(IT, " a {0:0.000} m", s.S[b]) : "");
                Dictionary<string, double> imax = new Dictionary<string, double>();
                for (int i = 0; i < g.N; i++)
                {
                    string eb;
                    if (!tb.TryGetValue(g.Tag[i], out eb)) continue;
                    double a = Complex.Abs(rr.Engine.SegmentCurrent(i)), v;
                    imax.TryGetValue(eb, out v); imax[eb] = Math.Max(v, a);
                }
                double mio; imax.TryGetValue(b, out mio);
                string ladro = null; double peggio = 0;
                foreach (KeyValuePair<string, double> kv in imax) if (kv.Key != b && kv.Value > peggio) { peggio = kv.Value; ladro = kv.Key; }
                d.Ruba = ladro != null && mio > 0 && peggio > 0.3 * mio ? ladro + string.Format(IT, " ({0:0} % della corrente)", peggio / mio * 100) : "nessuno";
                r.Add(d);
            }
            return r;
        }
    }
}
