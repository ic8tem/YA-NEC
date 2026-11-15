// YA-NEC 0.4.30 - antenne su PCB a 2,4 GHz (famiglia "2,4 GHz COMMERCIALE" dei Calcolatori), bozzetto 2 rivisto approvato da IC8TEM il
// 14/11/2026 22:15; disegno quotato stampabile 1:1 e vista 3D del modello NEC approvati alle 22:42 ("si va bene"), costruiti dalle 22:49.
// IFA, MIFA (IFA a meandro), dipolo stampato, Yagi su PCB, Big Wheel su PCB. Disegni nostri: nessuna figura copiata dalle note TI o da WA5VJB.
// Il NEC-2 non conosce il dielettrico: le lunghezze si calcolano con la permittivita' efficace (er + 1)/2 (pista sul bordo della scheda, meta'
// del campo nell'aria e meta' nel substrato) e il NEC risolve la geometria fisica a f x sqrt(eeff), che e' la stessa cosa che scalare tutte le
// misure. Il piano di massa e' una griglia di fili (regola dell'area uguale: raggio = maglia / 2 pi), la pista larga w e' un filo di raggio w/4.
// Prove con nec2c (prototipo pcb24/pcb.py, 14/11/2026): IFA FR4 H 3 mm, massa 40 x 25 mm, pista 0,5 mm -> L 23,4 mm, S 7,9 mm, 50 +j0 ohm;
// dipolo a 8 mm dalla massa: 34,25 mm, 41,6 ohm; Yagi 5 el.: radiatore 35,4 mm, 39,5 ohm; Big Wheel petali di 100 gradi: R 19,5 mm, 18,0 ohm.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading;

namespace YaNec.Engine
{
    public class Pcb24Spec
    {
        public int Tipo;                          // 0 IFA, 1 MIFA, 2 dipolo stampato, 3 Yagi su PCB, 4 Big Wheel su PCB
        public double FMin = 2400, FMax = 2483.5, Z0 = 50;
        public int Substrato;                     // 0 FR4, 1 Rogers 4003C, 2 libero (in aria), 3 altro
        public double Er = 4.4, Spessore = 1.6;   // mm
        public double PianoW = 40, PianoL = 25, Pista = 0.5;   // mm
        public double H = 3;                      // IFA / MIFA: altezza del braccio sopra il bordo della massa (mm)
        public double MeandroH = 3, MeandroPasso = 2;           // MIFA (mm)
        public bool PianoDipolo = true;
        public double GapDipolo = 8;              // mm
        public int ElementiYagi = 5;
        public double AperturaBw = 100;           // gradi di ogni petalo della Big Wheel
        public double Margine = 1.5;              // distanza del corto dal bordo sinistro della massa (mm)
        public double L, S;                       // risultato: braccio (IFA, MIFA: sviluppo), lunghezza (dipolo, radiatore Yagi), raggio (Big Wheel); presa (mm)
        public bool Accordata;
        public Pcb24Spec Clone() { return (Pcb24Spec)MemberwiseClone(); }
    }

    public class Pcb24Esito
    {
        public Complex Zc;
        public double RosMin, RosC, RosMax, Gmax, F2a = double.NaN, F2b = double.NaN;
        public List<double[]> Curva = new List<double[]>();     // f (MHz), ROS
        public double[] Correnti;                                // |I| per segmento, normalizzata a 1 (vista 3D)
        public string Nota = "";
    }

    /// <summary>Primitiva del disegno quotato, in mm (u verso destra, v verso l'alto).</summary>
    public class PcbPrim
    {
        public const int Scheda = 0, Massa = 1, Pista = 2, Linea = 3, Freccia = 4, Alim = 5, Foro = 6, Testo = 7;
        public int K;
        public double[] P;            // Scheda/Massa: u0 v0 u1 v1; Pista/Linea: u v u v ...; Freccia: tre punti; Alim/Foro: u v r; Testo: u v
        public double W;              // Pista: larghezza (mm); Testo: altezza (mm)
        public int Lato;              // Pista: 0 sopra, 1 sotto (Big Wheel)
        public bool Verticale;        // Testo ruotato di 90 gradi
        public string T = "";
    }

    public static class Pcb24
    {
        public const double C0 = 299792.458;    // mm * MHz
        public static readonly string[] Tipi = { "IFA", "MIFA", "dipolo", "Yagi su PCB", "Big Wheel su PCB" };
        static readonly CultureInfo IV = CultureInfo.InvariantCulture;
        static readonly CultureInfo IT = CultureInfo.GetCultureInfo("it-IT");

        public static double Fc(Pcb24Spec s) { return (s.FMin + s.FMax) / 2; }
        public static double Eeff(Pcb24Spec s) { return s.Substrato == 2 ? 1.0 : (s.Er + 1) / 2; }
        public static double LambdaG(Pcb24Spec s) { return C0 / Fc(s) / Math.Sqrt(Eeff(s)); }
        public static bool HaMassa(Pcb24Spec s) { return s.Tipo <= 1 || (s.Tipo == 2 && s.PianoDipolo); }

        public static void Substrato(Pcb24Spec s, int k)
        {
            s.Substrato = k;
            if (k == 0) s.Er = 4.4; else if (k == 1) s.Er = 3.55; else if (k == 2) s.Er = 1.0;
        }

        /// <summary>Valori di partenza: IFA L + H = lambda_g / 4; MIFA sviluppo 1,15 volte; dipolo e radiatore 0,47 lambda_g; Big Wheel petalo lungo 1 lambda_g.</summary>
        public static void Iniziale(Pcb24Spec s)
        {
            double l = LambdaG(s);
            if (s.Tipo == 0) { s.L = Math.Max(2, l / 4 - s.H); s.S = Math.Max(1.5, 0.15 * (s.L + s.H)); }
            else if (s.Tipo == 1) { s.L = Math.Max(4, l / 4 * 1.15 - s.H); s.S = Math.Max(1.5, 0.15 * l / 4); }
            else if (s.Tipo == 2 || s.Tipo == 3) { s.L = 0.47 * l; s.S = 0; }
            else { s.L = l / (2 + 2 * AperturaRad(s)); s.S = 0; }
            s.Accordata = false;
        }

        static double AperturaRad(Pcb24Spec s) { return Math.Max(20, Math.Min(110, s.AperturaBw)) * Math.PI / 360; }   // meta' apertura
        static double SegMax(Pcb24Spec s) { return Math.Min(LambdaG(s) / 25, 3.0); }

        // ---------------------------------------------------------------- fili (mm)
        public class Filo { public int N; public double[] A, B; public double R; }

        static int Gw(List<Filo> w, double[] a, double[] b, double r, int ns, double sm)
        {
            double L = Dist(a, b);
            int n = ns > 0 ? ns : Math.Max(1, (int)Math.Ceiling(L / sm - 1e-9));
            w.Add(new Filo { N = n, A = a, B = b, R = r });
            return w.Count;   // etichetta (1 = primo)
        }
        static double Dist(double[] a, double[] b) { double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2]; return Math.Sqrt(dx * dx + dy * dy + dz * dz); }
        static double[] P3(double x, double y, double z) { return new[] { x, y, z }; }

        /// <summary>Piano di massa a griglia nel piano XZ (y = 0), bordo alto a z = 0, larghezza W lungo x da 0 e altezza L verso il basso.
        /// I punti "speciali" del bordo (corto e presa) prendono il posto del nodo di griglia piu' vicino.</summary>
        static void Massa(Pcb24Spec s, List<Filo> w, double[] speciali)
        {
            double m = Math.Min(LambdaG(s) / 20, 3.0);
            int nx = Math.Max(2, (int)Math.Round(s.PianoW / m)), nz = Math.Max(1, (int)Math.Round(s.PianoL / m));
            double mx = s.PianoW / nx, mz = s.PianoL / nz, ra = Math.Min(mx, mz) / (2 * Math.PI);
            double[] xs = new double[nx + 1], top = new double[nx + 1];
            for (int i = 0; i <= nx; i++) { xs[i] = i * mx; top[i] = xs[i]; }
            bool[] usato = new bool[nx + 1];
            foreach (double p in speciali)
            {
                int best = -1;
                for (int i = 0; i <= nx; i++)
                    if (!usato[i] && (best < 0 || Math.Abs(xs[i] - p) < Math.Abs(xs[best] - p))) best = i;
                if (best >= 0) { usato[best] = true; top[best] = p; }
            }
            int[] idx = new int[nx + 1];
            for (int i = 0; i <= nx; i++) idx[i] = i;
            Array.Sort(idx, (a, b) => top[a].CompareTo(top[b]));
            for (int k = 0; k < nx; k++) Gw(w, P3(top[idx[k]], 0, 0), P3(top[idx[k + 1]], 0, 0), ra, 1, 0);
            for (int r = 1; r <= nz; r++)
                for (int i = 0; i < nx; i++) Gw(w, P3(xs[i], 0, -r * mz), P3(xs[i + 1], 0, -r * mz), ra, 1, 0);
            for (int i = 0; i <= nx; i++)
                for (int r = 0; r < nz; r++) Gw(w, P3(r == 0 ? top[i] : xs[i], 0, -r * mz), P3(xs[i], 0, -(r + 1) * mz), ra, 1, 0);
        }

        /// <summary>Percorso del braccio della MIFA nel piano (u, v): tratto diritto di 2 mm oltre la presa, poi meandri su e giu' troncati allo sviluppo L.</summary>
        public static List<double[]> PercorsoMifa(Pcb24Spec s)
        {
            double xs = s.Margine, xf = xs + s.S, rest = s.L - s.S, x = xf, z = s.H;
            List<double[]> pts = new List<double[]> { new[] { x, z } };
            List<KeyValuePair<char, double>> seq = new List<KeyValuePair<char, double>> { new KeyValuePair<char, double>('r', 2.0) };
            bool su = true; double tot = 2;
            while (tot < rest + 10) { seq.Add(new KeyValuePair<char, double>(su ? 'u' : 'd', s.MeandroH)); seq.Add(new KeyValuePair<char, double>('r', s.MeandroPasso)); tot += s.MeandroH + s.MeandroPasso; su = !su; }
            foreach (KeyValuePair<char, double> d in seq)
            {
                if (rest <= 1e-9) break;
                double v = Math.Min(d.Value, rest); rest -= v;
                if (d.Key == 'r') x += v; else if (d.Key == 'u') z += v; else z -= v;
                pts.Add(new[] { x, z });
            }
            return pts;
        }

        public static double[] YagiLunghezze(Pcb24Spec s)
        {
            double l = LambdaG(s);
            int n = Math.Max(3, Math.Min(12, s.ElementiYagi));
            double[] r = new double[n];
            r[0] = 0.50 * l; r[1] = s.L;
            for (int k = 2; k < n; k++) r[k] = (0.44 - 0.01 * (k - 2)) * l;
            return r;
        }
        public static double[] YagiPosizioni(Pcb24Spec s)
        {
            double l = LambdaG(s);
            int n = Math.Max(3, Math.Min(12, s.ElementiYagi));
            double[] z = new double[n];
            for (int k = 1; k < n; k++) z[k] = z[k - 1] + (k == 1 ? 0.20 : k == 2 ? 0.15 : 0.20) * l;
            return z;
        }

        /// <summary>Fili in mm della geometria fisica; tag e segmento della sorgente.</summary>
        public static List<Filo> Fili(Pcb24Spec s, out int tagSorg, out int segSorg)
        {
            List<Filo> w = new List<Filo>();
            double a = s.Pista / 4, sm = SegMax(s);
            tagSorg = 1; segSorg = 1;
            if (s.Tipo <= 1)
            {
                double xs = s.Margine, xf = xs + s.S, H = s.H;
                Gw(w, P3(xs, 0, 0), P3(xs, 0, H), a, 0, sm);
                tagSorg = Gw(w, P3(xf, 0, 0), P3(xf, 0, H), a, 0, sm); segSorg = 1;
                Gw(w, P3(xs, 0, H), P3(xf, 0, H), a, 0, sm);
                if (s.Tipo == 0) Gw(w, P3(xf, 0, H), P3(xs + s.L, 0, H), a, 0, sm);
                else
                {
                    List<double[]> p = PercorsoMifa(s);
                    for (int i = 0; i + 1 < p.Count; i++) Gw(w, P3(p[i][0], 0, p[i][1]), P3(p[i + 1][0], 0, p[i + 1][1]), a, 0, sm);
                }
                Massa(s, w, new[] { xs, xf });
            }
            else if (s.Tipo == 2)
            {
                double xc = s.PianoDipolo ? s.PianoW / 2 : 0, z = s.PianoDipolo ? s.GapDipolo : 0;
                int n = Math.Max(3, (int)Math.Ceiling(s.L / sm)); if (n % 2 == 0) n++;
                tagSorg = Gw(w, P3(xc - s.L / 2, 0, z), P3(xc + s.L / 2, 0, z), a, n, sm); segSorg = (n + 1) / 2;
                if (s.PianoDipolo) Massa(s, w, new double[0]);
            }
            else if (s.Tipo == 3)
            {
                double[] ls = YagiLunghezze(s), zs = YagiPosizioni(s);
                for (int k = 0; k < ls.Length; k++)
                {
                    int n = Math.Max(3, (int)Math.Ceiling(ls[k] / sm)); if (n % 2 == 0) n++;
                    int t = Gw(w, P3(-ls[k] / 2, 0, zs[k]), P3(ls[k] / 2, 0, zs[k]), a, n, sm);
                    if (k == 1) { tagSorg = t; segSorg = (n + 1) / 2; }
                }
            }
            else
            {
                double R = s.L, al = AperturaRad(s), t = s.Substrato == 2 ? 1.6 : Math.Max(0.4, s.Spessore);
                tagSorg = Gw(w, P3(0, 0, 0), P3(0, 0, t), a, 1, sm); segSorg = 1;
                for (int i = 0; i < 3; i++)
                {
                    double th = (90 + 120 * i) * Math.PI / 180;
                    double[] p1 = { R * Math.Cos(th - al), R * Math.Sin(th - al) };
                    Gw(w, P3(0, 0, t), P3(p1[0], p1[1], t), a, 0, sm);
                    int na = Math.Max(3, (int)Math.Ceiling(2 * al * R / sm));
                    double[] prev = p1;
                    for (int k = 1; k <= na; k++)
                    {
                        double an = th - al + 2 * al * k / na;
                        double[] q = { R * Math.Cos(an), R * Math.Sin(an) };
                        Gw(w, P3(prev[0], prev[1], t), P3(q[0], q[1], t), a, 1, sm);
                        prev = q;
                    }
                    Gw(w, P3(prev[0], prev[1], t), P3(prev[0], prev[1], 0), a, 1, sm);
                    Gw(w, P3(prev[0], prev[1], 0), P3(0, 0, 0), a, 0, sm);
                }
            }
            return w;
        }

        /// <summary>Modello NEC. aria = false: geometria fisica (da risolvere a f x sqrt(eeff)); aria = true: geometria equivalente in aria
        /// (misure x sqrt(eeff)) alla frequenza vera, per "Apri come modello" e per EZNEC.</summary>
        public static NecModel Modello(Pcb24Spec s, bool aria)
        {
            int ts, ss;
            List<Filo> w = Fili(s, out ts, out ss);
            double k = 1e-3 * (aria ? Math.Sqrt(Eeff(s)) : 1.0);
            StringBuilder sb = new StringBuilder();
            sb.Append("CM YA-NEC - " + Tipi[Math.Max(0, Math.Min(4, s.Tipo))] + " a 2,4 GHz (Calcolatori > 2,4 GHz commerciale)\n");
            sb.Append(string.Format(IT, "CM scheda er {0:0.##}, eeff (er+1)/2 = {1:0.###}, lambda_g {2:0.00} mm\n", s.Substrato == 2 ? 1.0 : s.Er, Eeff(s), LambdaG(s)));
            if (aria) sb.Append(string.Format(IT, "CM geometria equivalente in aria: misure della scheda x {0:0.####} (sqrt eeff)\n", Math.Sqrt(Eeff(s))));
            else sb.Append(string.Format(IT, "CM geometria fisica: va calcolata a f x {0:0.####} (sqrt eeff)\n", Math.Sqrt(Eeff(s))));
            sb.Append("CE\n");
            for (int i = 0; i < w.Count; i++)
            {
                Filo f = w[i];
                sb.Append(string.Format(IV, "GW {0} {1} {2:R} {3:R} {4:R} {5:R} {6:R} {7:R} {8:R}\n", i + 1, f.N,
                    f.A[0] * k, f.A[1] * k, f.A[2] * k, f.B[0] * k, f.B[1] * k, f.B[2] * k, f.R * k));
            }
            sb.Append("GE 0\n");
            sb.Append(string.Format(IV, "EX 0 {0} {1} 0 1 0\n", ts, ss));
            double f0 = aria ? Fc(s) : Fc(s) * Math.Sqrt(Eeff(s));
            sb.Append(string.Format(IV, "FR 0 1 0 0 {0:R} 0\nEN\n", f0));
            NecModel m = NecModel.Parse(sb.ToString());
            m.Z0 = s.Z0;
            return m;
        }

        static Nec2Engine Risolvi(NecModel m, double fNec)
        {
            SegmentGeometry g = m.BuildGeometry();
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(fNec, Analysis.EffectiveSources(m, g, fNec), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fNec));
            return e;
        }

        /// <summary>Impedenza alla frequenza vera f (MHz).</summary>
        public static Complex Z(Pcb24Spec s, double f, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Risolvi(Modello(s, false), f * Math.Sqrt(Eeff(s))).Sources[0].Z;
        }

        static double Gamma(Complex z, double z0) { return Complex.Abs((z - z0) / (z + z0)); }

        /// <summary>Accordo a Z0 + j0 al centro banda. IFA e MIFA: braccio L e presa S (griglia 9 x 7 nella zona dell'IFA, L fino a 1,8 lambda_g/4 e
        /// S fino a L/2, poi Newton smorzato che accetta solo passi che migliorano il ROS). Gli altri: secante sulla lunghezza per X = 0.</summary>
        public static void Accorda(Pcb24Spec s, CancellationToken ct, Action<string> avanzamento = null)
        {
            double f = Fc(s), z0 = s.Z0, q = LambdaG(s) / 4;
            const double LMAX = 1.8, SMAX = 0.5;
            if (s.Tipo <= 1)
            {
                double bg = double.MaxValue, bL = s.L, bS = s.S;
                for (int i = 0; i < 9; i++)
                {
                    double L = q * (0.6 + (LMAX - 0.6) * i / 8);
                    for (int j = 0; j < 7; j++)
                    {
                        s.L = L; s.S = 0.5 + (SMAX * L - 0.5) * j / 6;
                        double g = Gamma(Z(s, f, ct), z0);
                        if (g < bg) { bg = g; bL = s.L; bS = s.S; }
                    }
                    if (avanzamento != null) avanzamento("ricerca iniziale " + (i + 1) + "/9");
                }
                s.L = bL; s.S = bS;
                for (int it = 0; it < 20; it++)
                {
                    Complex z = Z(s, f, ct);
                    if (Math.Abs(z.Real - z0) < 0.5 && Math.Abs(z.Imaginary) < 0.5) break;
                    const double d = 0.1;
                    s.S += d; Complex zs = Z(s, f, ct); s.S -= d;
                    s.L += d; Complex zl = Z(s, f, ct); s.L -= d;
                    double j00 = (zs.Real - z.Real) / d, j01 = (zl.Real - z.Real) / d, j10 = (zs.Imaginary - z.Imaginary) / d, j11 = (zl.Imaginary - z.Imaginary) / d;
                    double det = j00 * j11 - j01 * j10;
                    if (Math.Abs(det) < 1e-12) break;
                    double r0 = z0 - z.Real, r1 = -z.Imaginary;
                    double dS = (r0 * j11 - j01 * r1) / det, dL = (j00 * r1 - j10 * r0) / det;
                    double g0 = Gamma(z, z0), sc = Math.Min(1, 0.1 * q / Math.Max(Math.Max(Math.Abs(dS), Math.Abs(dL)), 1e-9));
                    double oS = s.S, oL = s.L;
                    bool ok = false;
                    for (int k = 0; k < 6 && !ok; k++)
                    {
                        double nL = Math.Min(Math.Max(oL + dL * sc, 0.5 * q), LMAX * q), nS = Math.Min(Math.Max(0.5, oS + dS * sc), SMAX * nL);
                        s.S = nS; s.L = nL;
                        if (Gamma(Z(s, f, ct), z0) < g0) ok = true; else sc /= 2;
                    }
                    if (!ok) { s.S = oS; s.L = oL; break; }
                    if (avanzamento != null) avanzamento("accordo, passo " + (it + 1));
                }
            }
            else
            {
                double a = s.L, xa = Z(s, f, ct).Imaginary, b = a * 0.97;
                s.L = b; double xb = Z(s, f, ct).Imaginary;
                for (int it = 0; it < 15; it++)
                {
                    if (Math.Abs(xb) < 0.3 || Math.Abs(xb - xa) < 1e-9) break;
                    double c = b - xb * (b - a) / (xb - xa);
                    c = Math.Min(Math.Max(c, 0.7 * a), 1.3 * a);
                    a = b; xa = xb; b = c; s.L = b; xb = Z(s, f, ct).Imaginary;
                    if (avanzamento != null) avanzamento("accordo, passo " + (it + 1));
                }
                s.L = b;
            }
            s.Accordata = true;
        }

        /// <summary>ROS sulla banda (fmin - 100 ... fmax + 100 MHz a passi di 10 MHz), banda a ROS 2, guadagno massimo al centro banda.</summary>
        public static Pcb24Esito Esito(Pcb24Spec s, CancellationToken ct, Action<string> avanzamento = null)
        {
            Pcb24Esito r = new Pcb24Esito();
            double fc = Fc(s);
            NecModel m = Modello(s, false);
            RunResult rr = Analysis.Run(m, m.BuildGeometry(), fc * Math.Sqrt(Eeff(s)), 5.0, ct);
            r.Zc = rr.Zin; r.Gmax = rr.GmaxDbi; r.RosC = Analysis.Swr(r.Zc, s.Z0);
            int ns = rr.Engine.SegmentCount;
            r.Correnti = new double[ns];
            double cmax = 0;
            for (int i = 0; i < ns; i++) { r.Correnti[i] = Complex.Abs(rr.Engine.SegmentCurrent(i)); cmax = Math.Max(cmax, r.Correnti[i]); }
            if (cmax > 0) for (int i = 0; i < ns; i++) r.Correnti[i] /= cmax;
            r.RosMin = Analysis.Swr(Z(s, s.FMin, ct), s.Z0);
            r.RosMax = Analysis.Swr(Z(s, s.FMax, ct), s.Z0);
            double f0 = Math.Floor((s.FMin - 100) / 10) * 10, f1 = s.FMax + 100;
            for (double f = f0; f <= f1 + 1e-9; f += 10)
            {
                r.Curva.Add(new[] { f, Analysis.Swr(Z(s, f, ct), s.Z0) });
                if (avanzamento != null) avanzamento(string.Format(IT, "ROS a {0:0} MHz", f));
            }
            // banda a ROS 2 attorno al centro (interpolazione lineare)
            int ic = 0;
            for (int i = 0; i < r.Curva.Count; i++) if (Math.Abs(r.Curva[i][0] - fc) < Math.Abs(r.Curva[ic][0] - fc)) ic = i;
            if (r.Curva[ic][1] <= 2)
            {
                int a = ic, b = ic;
                while (a > 0 && r.Curva[a - 1][1] <= 2) a--;
                while (b < r.Curva.Count - 1 && r.Curva[b + 1][1] <= 2) b++;
                r.F2a = a > 0 ? Interp(r.Curva[a - 1], r.Curva[a]) : r.Curva[0][0];
                r.F2b = b < r.Curva.Count - 1 ? Interp(r.Curva[b], r.Curva[b + 1]) : r.Curva[r.Curva.Count - 1][0];
            }
            List<string> n = new List<string>();
            if (r.RosC > 1.5 && s.Tipo <= 1) n.Add("Con questa altezza H la presa non porta a 50 ohm: abbassare H (la prova con nec2c da' 50 +j0 con H 3 mm) o cambiare il piano di massa.");
            if (s.Tipo >= 2 && r.RosC > 1.5)
                n.Add(string.Format(IT, "R = {0:0.0} ohm: serve un adattamento, per esempio un tratto di linea lambda/4 da {1:0.0} ohm (sqrt(R x Z0)).", r.Zc.Real, Math.Sqrt(Math.Max(0.1, r.Zc.Real) * s.Z0)));
            if (s.Tipo == 2 || s.Tipo == 3) n.Add("Alimentazione bilanciata: con il coassiale serve un balun.");
            if (s.Tipo <= 1 && s.Margine + Ingombro(s)[0] > s.PianoW + 0.01) n.Add("Il braccio esce dalla larghezza del piano di massa.");
            r.Nota = string.Join(" ", n);
            return r;
        }

        static double Interp(double[] a, double[] b) { return Math.Abs(b[1] - a[1]) < 1e-12 ? a[0] : a[0] + (2 - a[1]) * (b[0] - a[0]) / (b[1] - a[1]); }

        /// <summary>Ingombro dell'antenna (senza il piano di massa) in mm: larghezza, altezza.</summary>
        public static double[] Ingombro(Pcb24Spec s)
        {
            if (s.Tipo == 0) return new[] { s.L, s.H };
            if (s.Tipo == 1)
            {
                double u0 = s.Margine, u1 = s.Margine, v1 = s.H;
                foreach (double[] p in PercorsoMifa(s)) { u1 = Math.Max(u1, p[0]); v1 = Math.Max(v1, p[1]); }
                return new[] { u1 - u0, v1 };
            }
            if (s.Tipo == 2) return new[] { s.L, 0 };
            if (s.Tipo == 3)
            {
                double[] l = YagiLunghezze(s), z = YagiPosizioni(s);
                double mx = 0; foreach (double x in l) mx = Math.Max(mx, x);
                return new[] { mx, z[z.Length - 1] };
            }
            return new[] { 2 * s.L, 2 * s.L };
        }

        /// <summary>Misure da scrivere nella scheda e nel disegno (nome, valore in mm).</summary>
        public static List<string[]> Misure(Pcb24Spec s)
        {
            List<string[]> r = new List<string[]>();
            Func<double, string> mm = v => v.ToString("0.00", IT) + " mm";
            r.Add(new[] { "lambda_g (eeff " + Eeff(s).ToString("0.###", IT) + ")", mm(LambdaG(s)) });
            if (s.Tipo <= 1)
            {
                r.Add(new[] { "H altezza sopra la massa", mm(s.H) });
                r.Add(new[] { s.Tipo == 0 ? "L braccio dal corto" : "L sviluppo del braccio dal corto", mm(s.L) });
                r.Add(new[] { "S presa dal corto (asse-asse)", mm(s.S) });
                if (s.Tipo == 1) { r.Add(new[] { "meandro: altezza / passo", mm(s.MeandroH) + " / " + mm(s.MeandroPasso) }); }
                r.Add(new[] { "piano di massa", mm(s.PianoW) + " x " + mm(s.PianoL) });
            }
            else if (s.Tipo == 2)
            {
                r.Add(new[] { "lunghezza totale", mm(s.L) });
                r.Add(new[] { "ogni braccio (gap 1 mm al centro)", mm((s.L - 1) / 2) });
                if (s.PianoDipolo) r.Add(new[] { "distanza dal piano di massa", mm(s.GapDipolo) });
            }
            else if (s.Tipo == 3)
            {
                double[] l = YagiLunghezze(s), z = YagiPosizioni(s);
                for (int k = 0; k < l.Length; k++)
                    r.Add(new[] { (k == 0 ? "riflettore" : k == 1 ? "radiatore" : "direttore " + (k - 1)), mm(l[k]) + (k > 0 ? "  a " + mm(z[k]) : "  a 0") });
            }
            else
            {
                r.Add(new[] { "R raggio dei petali", mm(s.L) });
                r.Add(new[] { "apertura di ogni petalo", s.AperturaBw.ToString("0", IT) + " gradi" });
                r.Add(new[] { "sviluppo di ogni petalo", mm(2 * s.L + 2 * AperturaRad(s) * s.L) });
                r.Add(new[] { "spessore (lato sopra / sotto)", mm(s.Substrato == 2 ? 1.6 : s.Spessore) });
            }
            r.Add(new[] { "larghezza della pista", mm(s.Pista) });
            return r;
        }

        // ---------------------------------------------------------------- disegno quotato
        static void Quota(List<PcbPrim> d, double u1, double v1, double u2, double v2, double off, string t)
        {
            double du = u2 - u1, dv = v2 - v1, L = Math.Sqrt(du * du + dv * dv);
            if (L < 1e-6) return;
            double eu = du / L, ev = dv / L, nu = -ev, nv = eu;
            double a1 = u1 + nu * off, b1 = v1 + nv * off, a2 = u2 + nu * off, b2 = v2 + nv * off;
            double sg = off >= 0 ? 1 : -1;
            d.Add(new PcbPrim { K = PcbPrim.Linea, P = new[] { u1 + nu * sg * 0.5, v1 + nv * sg * 0.5, a1 + nu * sg * 1.0, b1 + nv * sg * 1.0 } });
            d.Add(new PcbPrim { K = PcbPrim.Linea, P = new[] { u2 + nu * sg * 0.5, v2 + nv * sg * 0.5, a2 + nu * sg * 1.0, b2 + nv * sg * 1.0 } });
            d.Add(new PcbPrim { K = PcbPrim.Linea, P = new[] { a1, b1, a2, b2 } });
            double fa = Math.Min(1.2, L / 3), fw = fa * 0.35;
            d.Add(new PcbPrim { K = PcbPrim.Freccia, P = new[] { a1, b1, a1 + eu * fa + nu * fw, b1 + ev * fa + nv * fw, a1 + eu * fa - nu * fw, b1 + ev * fa - nv * fw } });
            d.Add(new PcbPrim { K = PcbPrim.Freccia, P = new[] { a2, b2, a2 - eu * fa + nu * fw, b2 - ev * fa + nv * fw, a2 - eu * fa - nu * fw, b2 - ev * fa - nv * fw } });
            bool vert = Math.Abs(dv) > Math.Abs(du);
            double tu = (a1 + a2) / 2 + nu * sg * 1.6, tv = (b1 + b2) / 2 + nv * sg * 1.6;
            d.Add(new PcbPrim { K = PcbPrim.Testo, P = new[] { tu, tv }, W = 1.8, T = t, Verticale = vert });
        }

        static string Mm(double v) { return v.ToString("0.0#", IT); }

        /// <summary>Disegno quotato in mm (u, v). IFA, MIFA e dipolo: piano della scheda XZ; Yagi: XZ con il boom verso l'alto; Big Wheel: XY.</summary>
        public static List<PcbPrim> Disegno(Pcb24Spec s)
        {
            List<PcbPrim> d = new List<PcbPrim>();
            bool sub = s.Substrato != 2;
            double w = s.Pista;
            if (s.Tipo <= 1)
            {
                double[] ing = Ingombro(s);
                double vTop = ing[1] + 3, uR = Math.Max(s.PianoW, s.Margine + ing[0] + 2);
                if (sub) d.Add(new PcbPrim { K = PcbPrim.Scheda, P = new[] { 0, -s.PianoL, uR, vTop } });
                d.Add(new PcbPrim { K = PcbPrim.Massa, P = new[] { 0, -s.PianoL, s.PianoW, 0 } });
                double xs = s.Margine, xf = xs + s.S, H = s.H;
                d.Add(new PcbPrim { K = PcbPrim.Pista, P = new[] { xs, 0, xs, H }, W = w });
                d.Add(new PcbPrim { K = PcbPrim.Pista, P = new[] { xf, 0, xf, H }, W = w });
                List<double> pl = new List<double> { xs, H };
                if (s.Tipo == 0) { pl.Add(xs + s.L); pl.Add(H); }
                else foreach (double[] p in PercorsoMifa(s)) { pl.Add(p[0]); pl.Add(p[1]); }
                d.Add(new PcbPrim { K = PcbPrim.Pista, P = pl.ToArray(), W = w });
                d.Add(new PcbPrim { K = PcbPrim.Foro, P = new[] { xs, 0, Math.Max(0.3, w * 0.6) } });
                d.Add(new PcbPrim { K = PcbPrim.Alim, P = new[] { xf, 0, Math.Max(0.4, w * 0.8) } });
                Quota(d, xs, 0, xs, H, 2.5, "H " + Mm(H));
                Quota(d, xs, H, xf, H, ing[1] - H + 2.5, "S " + Mm(s.S));
                if (s.Tipo == 0) Quota(d, xs, H, xs + s.L, H, ing[1] - H + 6.5, "L " + Mm(s.L));
                else
                {
                    Quota(d, xs, H, xs + ing[0], H, ing[1] - H + 6.5, "ingombro " + Mm(ing[0]) + " (sviluppo L " + Mm(s.L) + ")");
                    Quota(d, xs + ing[0], H, xs + ing[0], H + s.MeandroH, -3, "hm " + Mm(s.MeandroH));
                }
                Quota(d, 0, -s.PianoL, s.PianoW, -s.PianoL, -4, "massa " + Mm(s.PianoW));
                Quota(d, s.PianoW, -s.PianoL, s.PianoW, 0, -4, Mm(s.PianoL));
                d.Add(new PcbPrim { K = PcbPrim.Testo, P = new[] { s.PianoW / 2, -s.PianoL / 2 }, W = 2.2, T = "piano di massa (anche sotto)" });
                d.Add(new PcbPrim { K = PcbPrim.Testo, P = new[] { xf + 3.5, -2.5 }, W = 1.6, T = "alimentazione" });
                d.Add(new PcbPrim { K = PcbPrim.Testo, P = new[] { xs + 1.2, -5.0 }, W = 1.6, T = "corto a massa" });
            }
            else if (s.Tipo == 2)
            {
                double xc = s.PianoDipolo ? s.PianoW / 2 : s.L / 2 + 3, z = s.PianoDipolo ? s.GapDipolo : 0, g = 0.5;
                double v0 = s.PianoDipolo ? -s.PianoL : -6, u1 = Math.Max(s.PianoDipolo ? s.PianoW : 0, xc + s.L / 2 + 3);
                double u0 = Math.Min(0, xc - s.L / 2 - 3);
                if (sub) d.Add(new PcbPrim { K = PcbPrim.Scheda, P = new[] { u0, v0, u1, z + 4 } });
                if (s.PianoDipolo) d.Add(new PcbPrim { K = PcbPrim.Massa, P = new[] { 0, -s.PianoL, s.PianoW, 0 } });
                d.Add(new PcbPrim { K = PcbPrim.Pista, P = new[] { xc - s.L / 2, z, xc - g, z }, W = w });
                d.Add(new PcbPrim { K = PcbPrim.Pista, P = new[] { xc + g, z, xc + s.L / 2, z }, W = w });
                d.Add(new PcbPrim { K = PcbPrim.Alim, P = new[] { xc, z, Math.Max(0.4, w * 0.8) } });
                Quota(d, xc - s.L / 2, z, xc + s.L / 2, z, 2.5, "L " + Mm(s.L));
                if (s.PianoDipolo)
                {
                    Quota(d, xc + s.L / 2 + 1, 0, xc + s.L / 2 + 1, z, -2.5, Mm(s.GapDipolo));
                    Quota(d, 0, -s.PianoL, s.PianoW, -s.PianoL, -4, "massa " + Mm(s.PianoW));
                    Quota(d, s.PianoW, -s.PianoL, s.PianoW, 0, -4, Mm(s.PianoL));
                    d.Add(new PcbPrim { K = PcbPrim.Testo, P = new[] { s.PianoW / 2, -s.PianoL / 2 }, W = 2.2, T = "piano di massa" });
                }
                d.Add(new PcbPrim { K = PcbPrim.Testo, P = new[] { xc, z - 2.2 }, W = 1.6, T = "alimentazione (gap 1 mm, balun)" });
            }
            else if (s.Tipo == 3)
            {
                double[] l = YagiLunghezze(s), z = YagiPosizioni(s);
                double mx = 0; foreach (double x in l) mx = Math.Max(mx, x);
                if (sub) d.Add(new PcbPrim { K = PcbPrim.Scheda, P = new[] { -mx / 2 - 3, -4, mx / 2 + 3, z[z.Length - 1] + 4 } });
                for (int k = 0; k < l.Length; k++)
                {
                    if (k == 1)
                    {
                        d.Add(new PcbPrim { K = PcbPrim.Pista, P = new[] { -l[k] / 2, z[k], -0.5, z[k] }, W = w });
                        d.Add(new PcbPrim { K = PcbPrim.Pista, P = new[] { 0.5, z[k], l[k] / 2, z[k] }, W = w });
                        d.Add(new PcbPrim { K = PcbPrim.Alim, P = new[] { 0, z[k], Math.Max(0.4, w * 0.8) } });
                    }
                    else d.Add(new PcbPrim { K = PcbPrim.Pista, P = new[] { -l[k] / 2, z[k], l[k] / 2, z[k] }, W = w });
                    string nome = k == 0 ? "R" : k == 1 ? "A" : "D" + (k - 1);
                    d.Add(new PcbPrim { K = PcbPrim.Testo, P = new[] { l[k] / 2 + 1.5, z[k] }, W = 1.7, T = nome + " " + Mm(l[k]) });
                    if (k > 0) Quota(d, -mx / 2 - 1, z[k - 1], -mx / 2 - 1, z[k], 1.5, Mm(z[k] - z[k - 1]));
                }
                d.Add(new PcbPrim { K = PcbPrim.Testo, P = new[] { 0, z[z.Length - 1] + 2.5 }, W = 1.6, T = "direzione del lobo" });
            }
            else
            {
                double R = s.L, al = AperturaRad(s);
                if (sub) d.Add(new PcbPrim { K = PcbPrim.Scheda, P = new[] { -R - 3, -R - 3, R + 3, R + 3 } });
                for (int i = 0; i < 3; i++)
                {
                    double th = (90 + 120 * i) * Math.PI / 180;
                    List<double> top = new List<double> { 0, 0 };
                    int na = 24;
                    for (int k = 0; k <= na; k++) { double an = th - al + 2 * al * k / na; top.Add(R * Math.Cos(an)); top.Add(R * Math.Sin(an)); }
                    d.Add(new PcbPrim { K = PcbPrim.Pista, P = top.ToArray(), W = w, Lato = 0 });
                    double ue = R * Math.Cos(th + al), ve = R * Math.Sin(th + al);
                    d.Add(new PcbPrim { K = PcbPrim.Pista, P = new[] { ue, ve, 0, 0 }, W = w, Lato = 1 });
                    d.Add(new PcbPrim { K = PcbPrim.Foro, P = new[] { ue, ve, Math.Max(0.3, w * 0.6) } });
                }
                d.Add(new PcbPrim { K = PcbPrim.Alim, P = new[] { 0, 0, Math.Max(0.5, w) } });
                double tq = (90 - 60) * Math.PI / 180;
                Quota(d, 0, 0, R * Math.Cos(tq), R * Math.Sin(tq), 0, "R " + Mm(R));
                d.Add(new PcbPrim { K = PcbPrim.Testo, P = new[] { 0, -R - 1.8 }, W = 1.6, T = "rame: pieno = lato sopra, tratteggiato = lato sotto; fori ai capi" });
            }
            return d;
        }

        /// <summary>Estensione del disegno in mm: umin, vmin, umax, vmax.</summary>
        public static double[] Estensione(List<PcbPrim> d)
        {
            double[] e = { double.MaxValue, double.MaxValue, double.MinValue, double.MinValue };
            Action<double, double> add = (u, v) => { e[0] = Math.Min(e[0], u); e[1] = Math.Min(e[1], v); e[2] = Math.Max(e[2], u); e[3] = Math.Max(e[3], v); };
            foreach (PcbPrim p in d)
            {
                if (p.K == PcbPrim.Testo) { add(p.P[0], p.P[1]); continue; }
                if (p.K == PcbPrim.Alim || p.K == PcbPrim.Foro) { add(p.P[0] - p.P[2], p.P[1] - p.P[2]); add(p.P[0] + p.P[2], p.P[1] + p.P[2]); continue; }
                for (int i = 0; i + 1 < p.P.Length; i += 2) add(p.P[i], p.P[i + 1]);
            }
            if (e[0] > e[2]) return new double[] { 0, 0, 10, 10 };
            return e;
        }

        // ---------------------------------------------------------------- DXF (R12, solo LINE e CIRCLE, in mm)
        public static string Dxf(Pcb24Spec s, bool specchiato)
        {
            List<PcbPrim> d = Disegno(s);
            StringBuilder b = new StringBuilder();
            double sg = specchiato ? -1 : 1;
            Action<string, double, double, double, double> linea = (lay, x1, y1, x2, y2) =>
                b.Append(string.Format(IV, "0\nLINE\n8\n{0}\n10\n{1:0.####}\n20\n{2:0.####}\n30\n0\n11\n{3:0.####}\n21\n{4:0.####}\n31\n0\n", lay, x1 * sg, y1, x2 * sg, y2));
            Action<string, double, double, double> cerchio = (lay, x, y, r) =>
                b.Append(string.Format(IV, "0\nCIRCLE\n8\n{0}\n10\n{1:0.####}\n20\n{2:0.####}\n30\n0\n40\n{3:0.####}\n", lay, x * sg, y, r));
            b.Append("999\nYA-NEC " + Tipi[Math.Max(0, Math.Min(4, s.Tipo))] + " 2,4 GHz - unita' mm, scala 1:1" + (specchiato ? ", specchiato" : "") + "\n");
            b.Append("0\nSECTION\n2\nENTITIES\n");
            foreach (PcbPrim p in d)
            {
                if (p.K == PcbPrim.Scheda || p.K == PcbPrim.Massa)
                {
                    string lay = p.K == PcbPrim.Scheda ? "SCHEDA" : "MASSA";
                    linea(lay, p.P[0], p.P[1], p.P[2], p.P[1]); linea(lay, p.P[2], p.P[1], p.P[2], p.P[3]);
                    linea(lay, p.P[2], p.P[3], p.P[0], p.P[3]); linea(lay, p.P[0], p.P[3], p.P[0], p.P[1]);
                }
                else if (p.K == PcbPrim.Pista)
                {
                    string lay = p.Lato == 0 ? "PISTA" : "PISTA_SOTTO", asse = p.Lato == 0 ? "ASSE" : "ASSE_SOTTO";
                    double h = p.W / 2;
                    for (int i = 0; i + 3 < p.P.Length; i += 2)
                    {
                        double x1 = p.P[i], y1 = p.P[i + 1], x2 = p.P[i + 2], y2 = p.P[i + 3], L = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                        if (L < 1e-9) continue;
                        double nx = -(y2 - y1) / L * h, ny = (x2 - x1) / L * h;
                        linea(asse, x1, y1, x2, y2);
                        linea(lay, x1 + nx, y1 + ny, x2 + nx, y2 + ny); linea(lay, x1 - nx, y1 - ny, x2 - nx, y2 - ny);
                        linea(lay, x1 + nx, y1 + ny, x1 - nx, y1 - ny); linea(lay, x2 + nx, y2 + ny, x2 - nx, y2 - ny);
                    }
                }
                else if (p.K == PcbPrim.Foro) cerchio("FORI", p.P[0], p.P[1], p.P[2]);
                else if (p.K == PcbPrim.Alim) cerchio("ALIMENTAZIONE", p.P[0], p.P[1], p.P[2]);
            }
            b.Append("0\nENDSEC\n0\nEOF\n");
            return b.ToString();
        }
    }
}
