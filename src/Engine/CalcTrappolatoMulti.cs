// YA-NEC 1.10.0 - Dipolo trappolato multibanda (2-5 bande), bozzetto approvato da IC8TEM il 28/11/2026 («Compila e pubblica tutto»).
// Idea della scheda: il calcolatore di dipoli trappolati di SP3RL (dx10antenne.pl), citato nei Crediti; formule e codice sono di YA-NEC:
// modello a linea del braccio caricato (lo stesso del Dipolo trappolato W3DZZ e della Verticale trappolata), risolto banda per banda
// dalla piu' alta alla piu' bassa: ogni trappola risuona alla banda appena piu' alta del tratto che la segue e sotto la sua risonanza
// e' una bobina X = wL / (1 - (f/ft)^2). "Accorda le bande (NEC)" ritocca i tratti con il motore, poi calcola Z, ROS, banda a ROS 2 e
// perdite delle trappole per ogni banda e sceglie il balun (1:1 ... 9:1) con il ROS medio piu' basso.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace YaNec.Engine
{
    public class CalcTrappolatoMulti : SimpleCalc
    {
        // bande proposte (60 m esclusa: canalizzata e con limiti di potenza diversi da paese a paese)
        public static readonly string[] BandeN = { "80 m", "40 m", "30 m", "20 m", "17 m", "15 m", "12 m", "10 m" };
        public static readonly double[] BandeF = { 3.65, 7.1, 10.125, 14.175, 18.118, 21.225, 24.94, 28.5 };
        public static readonly double[] RapportiBalun = { 1, 2, 4, 6, 9 };
        public static readonly string[] RapportiN = { "1:1", "2:1", "4:1", "6:1", "9:1" };
        static readonly double[] E12 = { 10, 12, 15, 18, 22, 27, 33, 39, 47, 56, 68, 82, 100, 120, 150, 180, 220 };
        const double XTrappola = 250;   // C automatica: reattanza di circa 250 ohm alla risonanza della trappola (scelta di compromesso, vedi Formula)

        public CalcTrappolatoMulti()
        {
            Key = "trapmulti"; Title = "Dipolo trappolato multibanda (2-5 bande)";
            Source = "idea della scheda: calcolatore di dipoli trappolati di SP3RL (dx10antenne.pl); modello a linea del braccio caricato come il Dipolo trappolato " +
                "(C. L. Buchanan W3DZZ, QST marzo 1955) e la Verticale trappolata; Z0 del monopolo 60 (ln(2h/a) - 1) (Schelkunoff); perdite delle trappole: I5WHC, RR 4/2025";
            Params.Add(new CalcParam("bande", "Bande", 0, "", "Da 2 a 5 bande. Ogni trappola risuona alla banda appena piu' alta del tratto che la segue.")
            { Choices = BandeN, Multipla = true, Value = Maschera(1, 3, 5, 7) }.ConSezione("1 · Bande"));
            Params.Add(new CalcParam("bc", "Banda calcolata", 0, "", "Banda del modello NEC creato e del disegno; se non e' tra quelle scelte si usa la piu' bassa.")
            { Choices = BandeN, Pillole = true, Value = 1 });
            Params.Add(CalcParam.Scelta("cs", "Condensatore delle trappole", 0, "Automatico: per ogni trappola il valore della serie E12 con reattanza vicina a 250 Ω alla sua risonanza. " +
                "Scelto: lo stesso C per tutte le trappole.", "automatico", "scelto").ConSezione("2 · Trappole"));
            Params.Add(new CalcParam("c", "C delle trappole", 47, "pF") { Visibile = () => I("cs") == 1 });
            Params.Add(new CalcParam("q", "Q delle trappole", 150, "", "Perdita della trappola: R parallela = Q ω L. Trappole in aria ben fatte 150-250, coassiali 100-150."));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm").ConSezione("3 · Filo, quota e terreno"));
            Params.Add(new CalcParam("h", "Quota", 12, "m", "0 = spazio libero."));
            Params.Add(CalcParam.Scelta("ter", "Terreno", 0, "Reale medio: Sommerfeld, εr 13, σ 0,005 S/m.", "reale medio", "perfetto", "spazio libero"));
            CanTune = true;
        }

        public override string TuneLabel { get { return "Accorda le bande (NEC)"; } }

        static double Maschera(params int[] idx) { int m = 0; foreach (int i in idx) m |= 1 << i; return m; }

        /// <summary>Frequenze delle bande scelte dalla piu' alta alla piu' bassa (al massimo 5).</summary>
        public double[] Fr
        {
            get
            {
                int m = I("bande");
                List<double> l = new List<double>();
                for (int i = BandeF.Length - 1; i >= 0; i--) if ((m & (1 << i)) != 0) l.Add(BandeF[i]);
                if (l.Count > 5) l.RemoveRange(5, l.Count - 5);
                if (l.Count == 0) l.Add(7.1);
                return l.ToArray();
            }
        }
        static string NomeBanda(double f) { int i = Array.IndexOf(BandeF, f); return i >= 0 ? BandeN[i] : Calcolatori.N(f, 3) + " MHz"; }

        double[] scale;           // fattore NEC di ogni tratto (null = formule)
        string chiaveAcc;
        List<double[]> esiti;     // per banda: R, X, perdita trappole dB, banda ROS 2 (kHz, NaN = non raggiunta), ROS
        public int BalunScelto;   // indice in RapportiBalun (per il rimando a Strumenti > Balun)
        string Chiave() { string k = ""; foreach (CalcParam p in Params) if (p.Key != "bc") k += p.Value.ToString("R", CultureInfo.InvariantCulture) + ";"; return k; }

        /// <summary>C (farad) della trappola che risuona a ft.</summary>
        double Ctrap(double ft)
        {
            if (I("cs") == 1) return Math.Max(1, P("c")) * 1e-12;
            double c = 1 / (2 * Math.PI * ft * 1e6 * XTrappola) * 1e12, best = E12[0];
            foreach (double v in E12) if (Math.Abs(Math.Log(v / c)) < Math.Abs(Math.Log(best / c))) best = v;
            return best * 1e-12;
        }

        static double Xtrap(double f, double L, double C)
        {
            double w = 2 * Math.PI * f * 1e6;
            return w * L / (1 - w * w * L * C);
        }

        /// <summary>Tratti di un braccio (dal centro alla punta) con il modello a linea; lt, ct = L e C delle trappole (n - 1).</summary>
        public void Progetto(out double[] tratti, out double[] lt, out double[] ct) { ProgettoSu(Fr, out tratti, out lt, out ct); }

        void ProgettoSu(double[] fr, out double[] tratti, out double[] lt, out double[] ct)
        {
            int n = fr.Length;
            double a = Math.Max(0.1, P("d")) / 2000, k = 0.95;   // 0,95: effetto d'estremita' del filo (71,3/f invece di 75/f)
            tratti = new double[n]; lt = new double[Math.Max(0, n - 1)]; ct = new double[Math.Max(0, n - 1)];
            for (int i = 0; i < n - 1; i++) { ct[i] = Ctrap(fr[i]); lt[i] = 1 / (Math.Pow(2 * Math.PI * fr[i] * 1e6, 2) * ct[i]); }
            tratti[0] = k * C0 / fr[0] / 4;
            for (int j = 1; j < n; j++)
            {
                double f = fr[j], beta = 2 * Math.PI * f / C0 / k;
                double interno = 0; for (int i = 0; i < j; i++) interno += tratti[i];
                double[] tr = tratti, l0 = lt, c0 = ct;
                int jj = j;
                Func<double, double> fx = lj =>
                {
                    double z0 = C170.Z0Mono(interno + lj, a);
                    Complex z = -Complex.ImaginaryOne * z0 / Math.Tan(beta * lj);                 // tratto j aperto in punta
                    z += Complex.ImaginaryOne * Xtrap(f, l0[jj - 1], c0[jj - 1]);                   // trappola tra il tratto j-1 e il tratto j
                    for (int i = jj - 1; i >= 0; i--)
                    {
                        z = C170.Linea(z, z0, beta * tr[i]);
                        if (i > 0) z += Complex.ImaginaryOne * Xtrap(f, l0[i - 1], c0[i - 1]);
                    }
                    return z.Imaginary;
                };
                // prima risonanza serie: la reattanza passa da negativa a positiva crescendo il tratto (la bisezione semplice puo' cadere su
                // un polo, dove la reattanza salta da +infinito a -infinito: succede con bande vicine, es. 12 e 10 m)
                double hi = Math.PI / 2 / beta - 0.01, x0 = 0.02, v0 = fx(x0), trovato = double.NaN;
                for (int st = 1; st <= 400; st++)
                {
                    double x = 0.02 + (hi - 0.02) * st / 400, v = fx(x);
                    if (v0 < 0 && v >= 0) { trovato = C170.Bisez(fx, x0, x); break; }
                    x0 = x; v0 = v;
                }
                tratti[j] = double.IsNaN(trovato) ? C170.Bisez(fx, 0.05, hi) : trovato;
            }
        }

        /// <summary>Modello NEC a una frequenza (sc = fattori dei tratti, null = formule).</summary>
        string Mazzo(double f, double[] tratti, double[] lt, double[] ct, double[] sc, double z0rif)
        {
            double[] fr = Fr;
            int n = fr.Length;
            double a = Math.Max(0.1, P("d")) / 2000, Q = Math.Max(10, P("q")), h = Math.Max(0, P("h"));
            int ter = I("ter");
            if (h <= 0) ter = 2;
            double seg = Math.Min(C0 / fr[0] / 40, 0.5);
            NecDeck dk = new NecDeck(Title + " " + Elenco(fr) + " a " + Calcolatori.N(f, 3) + " MHz");
            int tc = dk.W(1, 0, -seg / 2, h, 0, seg / 2, h, a);
            List<int[]> trap = new List<int[]>();   // tag, segmento, indice trappola
            for (int lato = -1; lato <= 1; lato += 2)
            {
                double y = seg / 2, fine = 0;
                for (int i = 0; i < n; i++)
                {
                    fine += tratti[i] * (sc != null && i < sc.Length ? sc[i] : 1);
                    double y2 = Math.Max(fine, y + seg);
                    int t = dk.Ws(seg, 0, lato * y, h, 0, lato * y2, h, a, false, 2);
                    if (i < n - 1) trap.Add(new[] { t, dk.LastN, i });
                    y = y2;
                }
            }
            dk.Ground(ter == 0 ? 2 : ter == 1 ? 1 : 0);
            foreach (int[] t in trap)
            {
                double L = lt[t[2]], C = ct[t[2]], w0 = 1 / Math.Sqrt(L * C);
                dk.Ld(1, t[0], t[1], t[1], Q * w0 * L, L, C);
            }
            dk.Ex(tc, 1);
            string nec = dk.End(f);
            return Math.Abs(z0rif - 50) > 0.5 ? "CM YA-NEC Z0=" + z0rif.ToString("0", CultureInfo.InvariantCulture) + "\n" + nec : nec;
        }

        static string Elenco(double[] fr)
        {
            List<string> s = new List<string>();
            for (int i = fr.Length - 1; i >= 0; i--) s.Add(NomeBanda(fr[i]).Replace(" m", ""));
            return string.Join("-", s) + " m";
        }

        /// <summary>Accorda i tratti dalla banda alta alla bassa con il NEC, poi calcola ogni banda e sceglie il balun.</summary>
        public override double Tune(double fmhz)
        {
            double[] fr = Fr, tratti, lt, ct;
            Progetto(out tratti, out lt, out ct);
            double[] s = new double[fr.Length];
            for (int i = 0; i < s.Length; i++) s[i] = 1;
            // due passate: i tratti esterni spostano di poco le bande alte (la trappola non e' un circuito aperto perfetto), la seconda le rimette
            for (int passata = 0; passata < 2; passata++)
                for (int i = 0; i < fr.Length; i++)
                {
                    int ii = i;
                    s[ii] = C170.Risonanza(x => { double[] sc = (double[])s.Clone(); sc[ii] = x; return Mazzo(fr[ii], tratti, lt, ct, sc, 50); }, fr[ii], s[ii]);
                    s[ii] = Math.Max(0.6, Math.Min(1.4, s[ii]));
                }
            scale = s;
            // Z al centro di ogni banda, poi il balun con la media geometrica del ROS piu' bassa
            Complex[] zb = new Complex[fr.Length];
            for (int i = 0; i < fr.Length; i++) zb[i] = C170.Zin(Mazzo(fr[i], tratti, lt, ct, s, 50), fr[i]);
            double migliore = double.MaxValue; int bi = 0;
            for (int r = 0; r < RapportiBalun.Length; r++)
            {
                double somma = 0;
                foreach (Complex z in zb) somma += Math.Log(Math.Min(50, C170.Ros(z, 50 * RapportiBalun[r])));
                if (somma < migliore - 1e-9) { migliore = somma; bi = r; }
            }
            BalunScelto = bi;
            double z0 = 50 * RapportiBalun[bi];
            esiti = new List<double[]>();
            for (int i = 0; i < fr.Length; i++)
            {
                string nec = Mazzo(fr[i], tratti, lt, ct, s, z0);
                double perdita = double.NaN;
                try
                {
                    NecModel m = NecModel.Parse(nec);
                    SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, fr[i]);
                    Nec2Engine e = new Nec2Engine(g);
                    e.Solve(fr[i], Analysis.EffectiveSources(m, g, fr[i]), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fr[i]));
                    if (e.InputPower > 0 && e.InputPower > e.StructureLoss) perdita = 10 * Math.Log10(e.InputPower / (e.InputPower - e.StructureLoss));
                }
                catch { }
                esiti.Add(new[] { zb[i].Real, zb[i].Imaginary, perdita, BandaRos2(nec, fr[i], z0), C170.Ros(zb[i], z0) });
            }
            chiaveAcc = Chiave();
            return 1;
        }

        /// <summary>Larghezza (kHz) in cui il ROS riferito a z0 resta sotto 2 intorno a f: passi dello 0,25 % fino al 6 %, poi interpolazione.
        /// NaN se al centro della banda il ROS e' gia' sopra 2.</summary>
        static double BandaRos2(string nec, double f, double z0)
        {
            Func<double, double> ros = x => C170.Ros(C170.Zin(nec, x), z0);
            double r0 = ros(f);
            if (r0 >= 2) return double.NaN;
            double[] lati = new double[2];
            for (int d = 0; d < 2; d++)
            {
                int seg = d == 0 ? 1 : -1;
                double fp = f, rp = r0, bordo = f * (1 + seg * 0.06);
                for (int k = 1; k <= 24; k++)
                {
                    double fx = f * (1 + seg * 0.0025 * k), rx = ros(fx);
                    if (rx >= 2) { bordo = fp + (fx - fp) * (2 - rp) / (rx - rp); break; }
                    fp = fx; rp = rx;
                }
                lati[d] = Math.Abs(bordo - f);
            }
            return (lati[0] + lati[1]) * 1000;
        }

        /// <summary>Prove dell'autotest: W3DZZ col modello a linea (60 pF a 7,05 MHz, tratto esterno 6,7 m), radice giusta con bande vicine,
        /// accordo NEC del 40-20-15-10 m (ogni banda in risonanza, ROS col balun scelto).</summary>
        public static List<Tuple<string, bool, string>> Prove()
        {
            var r = new List<Tuple<string, bool, string>>();
            CultureInfo ci = CultureInfo.InvariantCulture;
            // 1) W3DZZ: con le sole bande 40 e 80 m spostate a 7,05 / 3,75 MHz il tratto esterno deve tornare 6,7 m (W3DZZ) entro il 5 %
            {
                CalcTrappolatoMulti c = new CalcTrappolatoMulti();
                c.Par("cs").Value = 1; c.Par("c").Value = 60;
                double[] t, l, k;
                c.ProgettoSu(new[] { 7.05, 3.75 }, out t, out l, out k);
                bool ok = Math.Abs(t[1] - 6.7) / 6.7 < 0.05 && Math.Abs(l[0] * 1e6 - 8.5) < 0.3;
                r.Add(Tuple.Create("Trappolato multibanda: W3DZZ col modello a linea", ok, string.Format(ci, "interno {0:0.00} m, esterno {1:0.00} m (W3DZZ 10,1 + 6,7 m), L {2:0.00} uH (W3DZZ 8,2; 8,49 con 60 pF esatti)", t[0], t[1], l[0] * 1e6)));
            }
            // 2) bande vicine 12 e 10 m: il tratto dopo la trappola dei 10 m deve essere corto (prima risonanza, non il polo)
            {
                CalcTrappolatoMulti c = new CalcTrappolatoMulti();
                double[] t, l, k;
                c.ProgettoSu(new[] { 28.5, 24.94 }, out t, out l, out k);
                bool ok = t[1] > 0.05 && t[1] < 1.0;
                r.Add(Tuple.Create("Trappolato multibanda: bande vicine (12/10 m)", ok, string.Format(ci, "tratto dei 12 m dopo la trappola {0:0.00} m (atteso sotto 1 m)", t[1])));
            }
            // 3) 40-20-15-10 m (predefinito) accordato col NEC
            {
                CalcTrappolatoMulti c = new CalcTrappolatoMulti();
                c.Tune(7.1);
                double[] fr = c.Fr;
                bool ok = c.esiti != null && c.esiti.Count == fr.Length;
                List<string> det = new List<string>();
                if (ok)
                    for (int i = 0; i < fr.Length; i++)
                    {
                        double[] e = c.esiti[i];
                        if (Math.Abs(e[1]) > 5 || e[4] > 2.5) ok = false;
                        det.Add(string.Format(ci, "{0} {1:0.0}{2}j{3:0.0} ROS {4:0.00}", NomeBanda(fr[i]), e[0], e[1] < 0 ? "-" : "+", Math.Abs(e[1]), e[4]));
                    }
                CalcOutput o = c.Compute();
                ok = ok && o.Tabella != null && o.Tabella.Count == fr.Length && o.Tabella2 != null && o.Tabella2.Count == fr.Length + 1 && !string.IsNullOrEmpty(o.Nec);
                r.Add(Tuple.Create("Trappolato multibanda: 40-20-15-10 m accordato col NEC", ok, "balun " + RapportiN[c.BalunScelto] + "; " + string.Join("; ", det)));
            }
            return r;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double[] fr = Fr, tratti, lt, ct;
            int scelte = 0; { int m = I("bande"); for (int i = 0; i < BandeF.Length; i++) if ((m & (1 << i)) != 0) scelte++; }
            Progetto(out tratti, out lt, out ct);
            bool fresco = chiaveAcc == Chiave() && scale != null && scale.Length == fr.Length && esiti != null;
            double[] sc = fresco ? scale : null;
            int n = fr.Length;
            double[] l = new double[n], pos = new double[n];
            double braccio = 0;
            for (int i = 0; i < n; i++) { l[i] = tratti[i] * (sc != null ? sc[i] : 1); braccio += l[i]; pos[i] = braccio; }
            double z0 = fresco ? 50 * RapportiBalun[BalunScelto] : 50;

            o.Card("Lunghezza totale", Calcolatori.N(2 * braccio, 2) + " m", "braccio " + Calcolatori.N(braccio, 2) + " m, contro " + Calcolatori.N(2 * 71.3 / fr[n - 1], 1) + " m del dipolo senza trappole a " + NomeBanda(fr[n - 1]), true);
            o.Card("Trappole", n > 1 ? (2 * (n - 1)) + " (" + (n - 1) + " per braccio)" : "nessuna", n > 1 ? "Q " + Calcolatori.N(P("q"), 0) + (I("cs") == 0 ? ", C automatico" : ", C " + Calcolatori.N(P("c"), 0) + " pF") : "una sola banda: dipolo semplice");
            o.Card("Balun consigliato", fresco ? RapportiN[BalunScelto] : "«Accorda le bande»", fresco ? "ROS medio piu' basso sulle " + n + " bande (riferito a " + Calcolatori.N(z0, 0) + " Ω)" : "serve la Z del NEC su ogni banda", fresco);
            if (fresco)
            {
                double peggio = 1; string bp = "";
                for (int i = 0; i < n; i++) if (esiti[i][4] > peggio) { peggio = esiti[i][4]; bp = NomeBanda(fr[i]); }
                o.Card("ROS peggiore", Calcolatori.N(Math.Min(peggio, 99), 2), "a " + bp + " con il balun " + RapportiN[BalunScelto]);
            }

            // tabella delle trappole (dal centro)
            if (n > 1)
            {
                o.TabellaTitolo = "Trappole (per braccio, dal centro)";
                o.Tabella = new List<string[]> { new[] { "Trappola", "Risonanza", "C", "L", "Distanza dal centro", "Reattanza alle bande sotto" } };
                for (int i = 0; i < n - 1; i++)
                {
                    List<string> xs = new List<string>();
                    for (int j = i + 1; j < n; j++) xs.Add(NomeBanda(fr[j]) + " +j" + Calcolatori.N(Xtrap(fr[j], lt[i], ct[i]), 0));
                    o.Tabella.Add(new[] { "T" + (i + 1), Calcolatori.N(fr[i], 3) + " MHz (" + NomeBanda(fr[i]) + ")", Calcolatori.N(ct[i] * 1e12, 0) + " pF", Calcolatori.Henry(lt[i]),
                        Calcolatori.N(pos[i], 2) + " m", string.Join(", ", xs) });
                }
            }
            // tabella delle bande
            o.Tabella2Titolo = "Bande" + (fresco ? " (NEC, balun " + RapportiN[BalunScelto] + ")" : "");
            o.Tabella2 = new List<string[]> { new[] { "Banda", "Tratto", "Fino a", "Z (NEC)", "ROS", "Banda ROS < 2", "Perdite trappole" } };
            for (int i = 0; i < n; i++)
            {
                string zz = "«Accorda le bande»", ros = "", bw = "", pt = "";
                if (fresco)
                {
                    double[] e = esiti[i];
                    zz = C170.Z(new Complex(e[0], e[1]));
                    ros = (e[4] <= 2 ? "✓ " : "⚠ ") + Calcolatori.N(Math.Min(e[4], 99), 2);
                    bw = double.IsNaN(e[3]) ? "⚠ ROS > 2 al centro" : Calcolatori.N(e[3], 0) + " kHz";
                    pt = double.IsNaN(e[2]) ? "" : Calcolatori.N(e[2], 2) + " dB";
                }
                else if (i > 0)
                {
                    // stima delle perdite (I5WHC): trappole interne come bobine con R = X / Q, corrente cos(beta x) riferita al centro
                    double beta = 2 * Math.PI * fr[i] / C0, rr = 73, rl = 0;
                    for (int t = 0; t < i; t++) { double x = Xtrap(fr[i], lt[t], ct[t]), ic = Math.Cos(beta * pos[t]); rl += 2 * x / Math.Max(10, P("q")) * ic * ic; }
                    pt = "circa " + Calcolatori.N(10 * Math.Log10((rr + rl) / rr), 2) + " dB";
                }
                o.Tabella2.Add(new[] { NomeBanda(fr[i]) + " (" + Calcolatori.N(fr[i], 3) + " MHz)", Calcolatori.N(l[i], 2) + " m", i < n - 1 ? "T" + (i + 1) : "punta", zz, ros, bw, pt });
            }

            o.Formula = "Modello a linea del braccio (dal centro): il tratto 1 e' λ/4 della banda piu' alta con l'effetto d'estremita' (0,95); per ogni banda sotto si cerca il tratto oltre la trappola " +
                "che annulla la reattanza al centro, con le trappole interne come bobine X = ωL / (1 - (f/ft)²) e Z0 = 60 (ln(2h/a) - 1). Trappola: L = 1 / ((2π ft)² C); C automatico = valore E12 vicino a " +
                "1 / (2π ft · 250 Ω) (compromesso: C piu' grande accorcia meno e perde meno sulle bande basse, ma serve una bobina piccola e critica; W3DZZ usava 376 Ω). " +
                "«Accorda le bande (NEC)» ritocca i tratti dalla banda alta alla bassa (trappole come RLC paralleli con R = Q ω L), poi calcola Z, ROS, banda a ROS 2 e perdite delle trappole " +
                "e sceglie il balun 1:1 ... 9:1 con la media geometrica del ROS piu' bassa. Idea della scheda: SP3RL (dx10antenne.pl); W3DZZ, QST 1955; I5WHC, RR 4/2025 (perdite).";
            if (scelte > 5) o.Warnings.Add("Scelte piu' di 5 bande: si usano le 5 piu' alte.");
            if (scelte < 2) o.Warnings.Add("Scegli almeno 2 bande: con una sola banda e' un dipolo semplice (Dipoli > mezz'onda).");
            for (int i = 0; i + 1 < n; i++)
                if (fr[i] / fr[i + 1] < 1.3) o.Warnings.Add(NomeBanda(fr[i]) + " e " + NomeBanda(fr[i + 1]) + " sono vicine: il tratto tra le due trappole viene corto e la banda della piu' bassa stretta.");
            for (int i = 1; i < n; i++)
                if (l[i] < 0.3) o.Warnings.Add("Tratto " + (i + 1) + " (" + NomeBanda(fr[i]) + ") molto corto (" + Calcolatori.N(l[i], 2) + " m): prova un C piu' grande nelle trappole.");
            o.Warnings.Add("Le trappole riducono banda e rendimento sulle bande basse: la perdita da Q pesa dove la trappola lavora da bobina di carico (I5WHC, RR 4/2025).");
            if (!fresco) o.Oks.Add("«Accorda le bande (NEC)» porta in risonanza ogni banda, sceglie il balun e riempie Z, ROS, banda a ROS 2 e perdite.");
            else o.Oks.Add("Bande accordate con il NEC: fattori dei tratti " + string.Join(" / ", Array.ConvertAll(scale, x => Calcolatori.N(x, 3))) + ".");

            // banda calcolata
            double fc = BandeF[Math.Max(0, Math.Min(BandeF.Length - 1, I("bc")))];
            if (Array.IndexOf(fr, fc) < 0) fc = fr[n - 1];
            Hint("bc", Array.IndexOf(fr, BandeF[Math.Max(0, Math.Min(BandeF.Length - 1, I("bc")))]) < 0 ? "non scelta: " + NomeBanda(fc) : "");
            o.Nec = Mazzo(fc, tratti, lt, ct, sc, z0);
            o.Z0Rif = z0;
            o.Bande = (double[])fr.Clone();

            // disegno: trappole su tutti e due i bracci, nomi e quote solo sul braccio destro, sfalsati su tre righe (tratti corti vicini)
            double h = Math.Max(0, P("h")), q = Math.Max(0.4, braccio * 0.16);
            o.Plane = "yz";
            o.HideGround = true;
            double prima = 0;
            for (int i = 0; i < n; i++)
            {
                double yq = h - q * (1 + (i % 3) * 0.85);
                o.Mark(7, prima, yq, Calcolatori.N(l[i], 2) + " m", 3, pos[i], yq);
                if (i < n - 1)
                {
                    foreach (int lato in new[] { -1, 1 }) o.Mark(6, lato * pos[i] - braccio * 0.03, h, null, 2, lato * pos[i] + braccio * 0.03, h);
                    o.Mark(0, pos[i] - braccio * 0.02, h + q * (0.7 + (i % 3) * 0.75), "T" + (i + 1), 2);
                }
                prima = pos[i];
            }
            o.Mark(7, -braccio, h - q * 3.7, "totale " + Calcolatori.N(2 * braccio, 2) + " m", 3, braccio, h - q * 3.7);
            return o;
        }
    }
}
