// YA-NEC 1.7.0 - nuovi calcolatori dal bozzetto «mining riviste» (approvato da IC8TEM il 25/11/2026 20:41), seconda parte:
// J dipolo multibanda a rele', K verticale trappolata multibanda, L Yagi bibanda interlacciata, O carico lineare, P filo risonante in armonica.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    // =============================================================================================
    // J · Dipolo multibanda a rele' (IK4PKK e IK4CIE, RR 9/2025)
    public class CalcDipoloRele : SimpleCalc
    {
        static readonly string[] Serie = { "40-20-15-10 m", "80-40-20-10 m", "20-17-15-12-10 m" };
        static readonly double[][] SerieF = { new[] { 28.5, 21.225, 14.175, 7.1 }, new[] { 28.5, 14.175, 7.1, 3.65 }, new[] { 28.5, 24.94, 21.225, 18.118, 14.175 } };
        public CalcDipoloRele()
        {
            Key = "dipolorele"; Title = "Dipolo multibanda a rele'"; Source = "IK4PKK e IK4CIE, RadioRivista 9/2025 (tratti a lunghezza piena uniti da rele' telecomandati)";
            Params.Add(CalcParam.Scelta("serie", "Bande", 0, null, Serie));
            Params.Add(CalcParam.Scelta("b", "Banda calcolata", 0, "Il modello NEC ha i rele' chiusi fino a questa banda e aperti oltre.", "1ª", "2ª", "3ª", "4ª", "5ª"));
            Params.Add(new CalcParam("h", "Altezza", 10, "m", "0 = spazio libero."));
            Params.Add(new CalcParam("d", "ø del filo", 2, "mm"));
            Params.Add(new CalcParam("bat", "Batteria", 7000, "mAh"));
            Params.Add(new CalcParam("ir", "Corrente di un rele' chiuso", 65, "mA"));
        }
        double[] Fr { get { return SerieF[Math.Max(0, Math.Min(SerieF.Length - 1, I("serie")))]; } }
        int B { get { return Math.Max(0, Math.Min(Fr.Length - 1, I("b"))); } }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double[] fr = Fr;
            int b = B;
            double f = fr[b], lam = C0 / f, a = P("d") / 2000, h = P("h");
            double[] pos = new double[fr.Length];
            for (int i = 0; i < fr.Length; i++) pos[i] = 142.65 / fr[i] / 2;
            pos[b] *= Scale;
            int chiusi = b;   // coppie di rele' chiusi (uno per braccio)
            for (int i = 0; i < fr.Length; i++)
                o.Card(C170.N(fr[i], 3) + " MHz", C170.N(pos[i], 2) + " m", i == fr.Length - 1 ? "punta del braccio" : "rele' a " + C170.N(pos[i], 2) + " m dal centro", i == b);
            double ore = chiusi == 0 ? double.PositiveInfinity : P("bat") / (P("ir") * 2 * chiusi);
            o.Card("Batteria", chiusi == 0 ? "rele' a riposo" : C170.N(ore, 0) + " ore", chiusi == 0 ? "banda piu' alta: nessun rele' chiuso" : 2 * chiusi + " rele' chiusi da " + C170.N(P("ir"), 0) + " mA");
            o.Formula = "Ogni banda ha il suo braccio a lunghezza piena (142,65/f in tutto): i rele' chiusi uniscono i tratti fino alla banda voluta, quelli aperti staccano il resto. " +
                "Confronto con il dipolo trappolato: qui ogni banda e' un dipolo intero (banda a ROS 2 piena, niente perdite delle trappole), in cambio servono i fili di comando e l'alimentazione dei rele'. " +
                "Fonte: IK4PKK e IK4CIE, RR 9/2025. Nel modello NEC un rele' aperto e' un tratto corto con una resistenza altissima; i fili di comando non ci sono.";
            o.Oks.Add("I fili di comando dei rele' vanno fatti passare lontano dal dipolo o con impedenze di blocco: possono risuonare e cambiare il ROS.");
            NecDeck dk = new NecDeck(Title + " " + C170.N(f, 3) + " MHz");
            double seg = Math.Min(lam / 50, C0 / fr[0] / 30), gap = 0.05;
            int tc = dk.W(1, 0, -seg / 2, h, 0, seg / 2, h, a);
            List<int> aperti = new List<int>();
            foreach (int s in new[] { 1, -1 })
            {
                double y = seg / 2;
                for (int i = 0; i < fr.Length; i++)
                {
                    double fine = pos[i] - (i < fr.Length - 1 ? gap / 2 : 0);
                    if (fine > y + 0.01) dk.Ws(seg, 0, s * y, h, 0, s * fine, h, a);
                    y = fine;
                    if (i < fr.Length - 1)
                    {
                        int tr = dk.W(1, 0, s * y, h, 0, s * (y + gap), h, a);
                        if (i >= b) aperti.Add(tr);
                        y += gap;
                    }
                }
            }
            dk.Ground(h > 0 ? 2 : 0);
            foreach (int t in aperti) dk.Ld(0, t, 1, 1, 1e7, 0, 0);
            dk.Ex(tc, 1);
            o.Nec = dk.End(f);
            o.Plane = "yz";
            return o;
        }
    }

    // =============================================================================================
    // K · Verticale trappolata multibanda λ/4 (IZ2IAM, RR 11/2025; bobina con cappello: idea da Grazioli, RKE 11/2025)
    public class CalcVertTrappolata : SimpleCalc
    {
        public override bool Tubolare { get { return true; } }
        static readonly string[] Serie = { "40-20-15-10 m", "80-40-20-15-10 m", "20-15-10 m" };
        static readonly double[][] SerieF = { new[] { 28.5, 21.225, 14.175, 7.1 }, new[] { 28.5, 21.225, 14.175, 7.1, 3.65 }, new[] { 28.5, 21.225, 14.175 } };
        static readonly double[] Cratio = { 60, 33, 20 };
        public CalcVertTrappolata()
        {
            Key = "verttrap"; Title = "Verticale trappolata multibanda"; Source = "IZ2IAM, RadioRivista 11/2025 (Cushcraft AV-5); bobina con cappello: idea da Grazioli, RadioKit Elettronica 11/2025 (il contrappeso brevettato non si riproduce); " +
                "modello a linea del monopolo (Schelkunoff: Z0 = 60 (ln(2h/a) - 1)); ON4UN (resistenza di terra)";
            Params.Add(CalcParam.Scelta("serie", "Bande", 0, null, Serie));
            Params.Add(new CalcParam("d", "ø dei tubi", 30, "mm"));
            Params.Add(CalcParam.Scelta("lc", "Trappole", 1, "C grande: trappole meno sensibili e con meno perdite in banda, ma piu' accorciamento. C piccola: bobine piu' grandi, banda piu' stretta sulle bande basse.", "C grande (60 pF)", "media (33 pF)", "C piccola (20 pF)"));
            Params.Add(new CalcParam("q", "Q delle trappole", 200, ""));
            Params.Add(CalcParam.Scelta("ult", "Banda piu' bassa", 0, "Bobina con cappello: un tratto corto con la bobina e un cappello di 4 raggi in cima; la bobina carica la banda bassa e fa da separazione.", "tratto pieno", "bobina con cappello"));
            Params.Add(new CalcParam("lt", "Tratto sopra la bobina", 1.2, "m") { Visibile = () => I("ult") == 1 });
            Params.Add(new CalcParam("lh", "Raggi del cappello (4)", 0.8, "m") { Visibile = () => I("ult") == 1 });
            Params.Add(new CalcParam("n", "Numero di radiali", 16, "").ConSezione("2 · Terra"));
            Params.Add(new CalcParam("lr", "Lunghezza dei radiali", 10, "m"));
        }
        public override string TuneLabel { get { return "Accorda le bande (NEC)"; } }

        double[] Fr { get { return SerieF[Math.Max(0, Math.Min(SerieF.Length - 1, I("serie")))]; } }
        double[] scale;
        string chiaveAcc;
        List<RunResult> risultati;
        string Chiave() { string k = ""; foreach (CalcParam p in Params) k += p.Value.ToString("R", CultureInfo.InvariantCulture) + ";"; return k; }

        /// <summary>Lunghezze dei tratti dal modello a linea (dal basso), reattanza della bobina (con cappello) e L delle trappole.</summary>
        void Progetto(out double[] trattiOut, out double[] ltOut, out double xBobOut)
        {
            double[] fr = Fr;
            int n = fr.Length;
            double a = P("d") / 2000, c = Cratio[Math.Max(0, Math.Min(2, I("lc")))] * 1e-12, k = 0.97;
            bool cap = I("ult") == 1;
            double[] tratti = new double[n], lt = new double[n - 1]; double xBob = 0;
            for (int i = 0; i < n - 1; i++) lt[i] = 1 / (Math.Pow(2 * Math.PI * fr[i] * 1e6, 2) * c);
            tratti[0] = k * C0 / fr[0] / 4;
            for (int j = 1; j < n; j++)
            {
                double f = fr[j], beta = 2 * Math.PI * f / C0 / k;
                double basso = 0; for (int i = 0; i < j; i++) basso += tratti[i];
                if (j == n - 1 && cap)
                {
                    // bobina sopra l'ultima trappola, poi il tratto corto e il cappello (lunghezza equivalente: tratto + raggi)
                    double w = 2 * Math.PI * f * 1e6, Ltop = lt[j - 1];
                    double z0 = C170.Z0Mono(basso + P("lt"), a), leq = Math.Max(0.1, P("lt")) + Math.Max(0, P("lh"));
                    Func<double, double> fx = xb =>
                    {
                        Complex z = -Complex.ImaginaryOne * z0 / Math.Tan(beta * leq) + Complex.ImaginaryOne * xb;
                        z += Complex.ImaginaryOne * (w * Ltop / (1 - w * w * Ltop * c));
                        for (int i = j - 1; i >= 0; i--)
                        {
                            z = C170.Linea(z, C170.Z0Mono(basso + 1, a), beta * tratti[i]);
                            if (i > 0) { double L = lt[i - 1]; z += Complex.ImaginaryOne * (w * L / (1 - w * w * L * c)); }
                        }
                        return z.Imaginary;
                    };
                    xBob = C170.Bisez(fx, 0, 20000);
                    tratti[j] = Math.Max(0.1, P("lt"));
                }
                else
                {
                    Func<double, double> fx = lj =>
                    {
                        double w = 2 * Math.PI * f * 1e6, L = lt[j - 1];
                        Complex z = -Complex.ImaginaryOne * C170.Z0Mono(basso + lj, a) / Math.Tan(beta * lj);
                        z += Complex.ImaginaryOne * (w * L / (1 - w * w * L * c));
                        for (int i = j - 1; i >= 0; i--)
                        {
                            z = C170.Linea(z, C170.Z0Mono(basso + lj, a), beta * tratti[i]);
                            if (i > 0) { double Li = lt[i - 1]; z += Complex.ImaginaryOne * (w * Li / (1 - w * w * Li * c)); }
                        }
                        return z.Imaginary;
                    };
                    tratti[j] = C170.Bisez(fx, 0.05, Math.PI / 2 / beta - 0.01);
                }
            }
            trattiOut = tratti; ltOut = lt; xBobOut = xBob;
        }

        string Mazzo(double f, double[] tratti, double[] lt, double xBob, double[] sc)
        {
            double[] fr = Fr;
            int n = fr.Length;
            double a = P("d") / 2000, c = Cratio[Math.Max(0, Math.Min(2, I("lc")))] * 1e-12, Q = Math.Max(20, P("q"));
            bool cap = I("ult") == 1;
            double lam = C0 / f, seg = Math.Min(C0 / fr[0] / 40, 0.4), gt = 0.12;
            NecDeck dk = new NecDeck(Title + " " + Serie[Math.Max(0, Math.Min(2, I("serie")))] + " a " + C170.N(f, 3) + " MHz");
            double z = 0;
            int tBase = 0;
            List<int> trap = new List<int>();
            int tBob = 0;
            for (int i = 0; i < n; i++)
            {
                double l = tratti[i] * (sc != null && i < sc.Length ? sc[i] : 1);
                if (i == n - 1 && cap)
                {
                    tBob = dk.W(1, 0, 0, z, 0, 0, z + gt, a); z += gt;
                    dk.Ws(seg, 0, 0, z, 0, 0, z + Math.Max(0.1, P("lt")), a, false, 2); z += Math.Max(0.1, P("lt"));
                    double lh = Math.Max(0.05, P("lh"));
                    dk.Ws(seg, 0, 0, z, lh, 0, z, 0.003); dk.Ws(seg, 0, 0, z, -lh, 0, z, 0.003); dk.Ws(seg, 0, 0, z, 0, lh, z, 0.003); dk.Ws(seg, 0, 0, z, 0, -lh, z, 0.003);
                    break;
                }
                int t = dk.Ws(seg, 0, 0, z, 0, 0, z + l, a, false, i == 0 ? 3 : 2);
                if (i == 0) tBase = t;
                z += l;
                if (i < n - 1) { trap.Add(dk.W(1, 0, 0, z, 0, 0, z + gt, a)); z += gt; }
            }
            // radiali sopra il terreno perfetto non servono: terra come resistenza alla base (ON4UN), dipende dalla banda
            double rg = CalcQuartoTerra.ResistenzaTerra(Math.Max(1, P("n")), Math.Max(0.05, P("lr") / lam));
            dk.Ground(1, true);
            for (int i = 0; i < trap.Count; i++)
            {
                double L = lt[i], w0 = 1 / Math.Sqrt(L * c);
                dk.Ld(1, trap[i], 1, 1, Q * w0 * L, L, c);
            }
            if (cap && tBob > 0 && xBob > 0)
            {
                double w = 2 * Math.PI * fr[n - 1] * 1e6, Lb = xBob / w * (sc != null && sc.Length >= n ? sc[n - 1] : 1);
                dk.Ld(0, tBob, 1, 1, w * Lb / Q, Lb, 0);
            }
            dk.Ld(0, tBase, 1, 1, rg, 0, 0);
            dk.Ex(tBase, 1);
            return dk.End(f);
        }

        public override double Tune(double fmhz)
        {
            double[] fr = Fr, tratti, lt; double xb;
            Progetto(out tratti, out lt, out xb);
            scale = new double[fr.Length];
            for (int i = 0; i < scale.Length; i++) scale[i] = 1;
            for (int i = 0; i < fr.Length; i++)
            {
                int ii = i;
                scale[ii] = C170.Risonanza(s => { double[] sc = (double[])scale.Clone(); sc[ii] = s; return Mazzo(fr[ii], tratti, lt, xb, sc); }, fr[ii]);
            }
            risultati = new List<RunResult>();
            foreach (double f in fr) risultati.Add(C170.Calcola(Mazzo(f, tratti, lt, xb, scale), f));
            chiaveAcc = Chiave();
            return 1;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double[] fr = Fr, tratti, lt; double xb;
            Progetto(out tratti, out lt, out xb);
            bool fresco = chiaveAcc == Chiave() && scale != null && scale.Length == fr.Length;
            double[] sc = fresco ? scale : null;
            double c = Cratio[Math.Max(0, Math.Min(2, I("lc")))];
            bool cap = I("ult") == 1;
            double tot = 0;
            o.TabellaTitolo = "Tratti, trappole e bande (dal basso)";
            o.Tabella = new List<string[]> { new[] { "Banda", "Tratto", "Trappola in cima", "Z (NEC)", "ROS su 50 Ω", "Efficienza (NEC)" } };
            for (int i = 0; i < fr.Length; i++)
            {
                double l = tratti[i] * (sc != null ? sc[i] : 1);
                tot += l;
                string tr = i < lt.Length ? Calcolatori.Henry(lt[i]) + " / " + C170.N(c, 0) + " pF (" + C170.N(fr[i], 2) + " MHz)" : (cap ? "bobina " + Calcolatori.Henry(xb / (2 * Math.PI * fr[i] * 1e6) * (sc != null ? sc[i] : 1)) + " + cappello" : "punta");
                string zz = "«Accorda le bande»", ros = "", ef = "";
                if (fresco && risultati != null && i < risultati.Count)
                {
                    RunResult r = risultati[i];
                    zz = C170.Z(r.Zin); ros = C170.N(Math.Min(C170.Ros(r.Zin, 50), 99), 2); ef = C170.N(r.Efficiency, 0) + " %";
                }
                o.Tabella.Add(new[] { C170.N(fr[i], 3) + " MHz", C170.N(l, 2) + " m", tr, zz, ros, ef });
            }
            o.Card("Altezza totale", C170.N(tot + 0.12 * (fr.Length - 1) + (cap ? 0.12 : 0), 2) + " m", "tubi da " + C170.N(P("d"), 0) + " mm", true);
            o.Card("Trappole", (fr.Length - 1) + (cap ? " + bobina" : ""), "C " + C170.N(c, 0) + " pF, Q " + C170.N(P("q"), 0));
            o.Formula = "Modello a linea (dal basso): il tratto 1 e' λ/4 della banda piu' alta con l'effetto di punta (0,97); ogni banda sotto aggiunge il suo tratto oltre la trappola, che sotto la sua risonanza " +
                "e' una bobina X = ωL/(1 - ω²LC). Z0 del monopolo 60 (ln(2h/a) - 1) (Schelkunoff). «Accorda le bande (NEC)» ritocca i tratti dalla banda alta alla bassa, poi calcola ogni banda con il NEC " +
                "(trappole come RLC paralleli con il loro Q, terra come resistenza alla base). Fonti: IZ2IAM, RR 11/2025; Grazioli, RKE 11/2025 (bobina con cappello).";
            o.Warnings.Add("Le trappole riducono banda e rendimento sulle bande basse: le perdite da Q della bobina pesano dove la trappola lavora da carico (I5WHC, RR 4/2025).");
            if (!fresco) o.Oks.Add("«Accorda le bande (NEC)» porta in risonanza ogni banda e riempie Z, ROS ed efficienza.");
            o.Nec = Mazzo(fr[fr.Length - 1], tratti, lt, xb, sc);
            o.Plane = "xz";
            return o;
        }
    }

    // =============================================================================================
    // L · Yagi bibanda interlacciata a una linea (W1JR, QST 2/2022: solo riassunto, misure non riprodotte)
    public class CalcYagiBibanda : SimpleCalc
    {
        public override bool Tubolare { get { return true; } }
        static readonly string[] Coppie = { "12 / 17 m", "10 / 15 m", "4 / 6 m" };
        static readonly double[][] CoppieF = { new[] { 24.94, 18.118 }, new[] { 28.5, 21.225 }, new[] { 70.2, 50.15 } };
        public CalcYagiBibanda()
        {
            Key = "yagibib"; Title = "Yagi bibanda interlacciata"; Source = "W1JR, QST 2/2022 (Yagi 12 e 17 m sullo stesso boom con una sola linea: solo riassunto, misure non riprodotte); valori di partenza come la Yagi HF (I4MFA)";
            Params.Add(CalcParam.Scelta("cp", "Bande", 0, null, Coppie));
            Params.Add(CalcParam.Scelta("el", "Elementi per banda", 1, null, "2 + 2", "3 + 3"));
            Params.Add(CalcParam.Scelta("ali", "Alimentazione", 1, "Una linea: alimentato il radiatore della banda bassa, quello della banda alta gli sta accanto (0,04 λ) e prende energia per accoppiamento. Due linee: una per banda, il radiatore non usato aperto.",
                "due linee", "una linea (radiatore alto accoppiato)"));
            Params.Add(CalcParam.Scelta("b", "Banda calcolata", 0, null, "alta", "bassa"));
            Params.Add(new CalcParam("d", "ø dei tubi", 25, "mm"));
            Params.Add(new CalcParam("h", "Altezza", 0, "m", "0 = spazio libero."));
        }
        double[] Fp { get { return CoppieF[Math.Max(0, Math.Min(2, I("cp")))]; } }

        // elementi: banda (0 alta, 1 bassa), x, lunghezza, radiatore?
        List<double[]> Elementi(double scala)
        {
            double fh = Fp[0], fl = Fp[1], lh = C0 / fh, ll = C0 / fl;
            bool tre = I("el") == 1, una = I("ali") == 1;
            int b = I("b") == 0 ? 0 : 1;
            List<double[]> e = new List<double[]>();
            Func<double, double, double> de = (f, s) => 149.9 / f * 0.96 * s;
            double sl = b == 1 ? scala : 1, sh = b == 0 ? scala : 1;
            // banda bassa: riflettore, radiatore a 0,15 λ, direttore a 0,325 λ
            e.Add(new[] { 1, 0, de(fl, 1) * 1.04, 0 });
            e.Add(new[] { 1, 0.15 * ll, de(fl, sl), 1 });
            if (tre) e.Add(new[] { 1, 0.325 * ll, de(fl, 1) * 0.94, 0 });
            // banda alta: radiatore 0,04 λ davanti a quello basso; riflettore 0,15 λ dietro, direttore 0,175 λ avanti
            double xe = 0.15 * ll + 0.04 * lh;
            e.Add(new[] { 0, xe - 0.15 * lh, de(fh, 1) * 1.04, 0 });
            e.Add(new[] { 0, xe, de(fh, sh), una ? 2 : 1 });
            if (tre) e.Add(new[] { 0, xe + 0.175 * lh, de(fh, 1) * 0.94, 0 });
            return e;
        }

        string Mazzo(double scala)
        {
            double f = Fp[I("b") == 0 ? 0 : 1], a = P("d") / 2000, h = P("h");
            bool una = I("ali") == 1;
            int b = I("b") == 0 ? 0 : 1;
            NecDeck dk = new NecDeck(Title + " " + Coppie[Math.Max(0, Math.Min(2, I("cp")))] + " a " + C170.N(f, 3) + " MHz");
            double seg = C0 / Fp[0] / 40;
            int tf = 0, nf = 0, toff = 0, noff = 0;
            foreach (double[] el in Elementi(scala))
            {
                int t = dk.Ws(seg, el[1], -el[2] / 2, h, el[1], el[2] / 2, h, a, true, 11), n = dk.LastN;
                if (el[3] == 1)
                {
                    // radiatore con la sua linea: alimentato se e' della banda calcolata (o della bassa con una linea sola)
                    bool alim = una ? (int)el[0] == 1 : (int)el[0] == b;
                    if (alim) { tf = t; nf = n; } else { toff = t; noff = n; }
                }
            }
            dk.Ground(h > 0 ? 2 : 0);
            if (!una && toff > 0) dk.Ld(0, toff, (noff + 1) / 2, (noff + 1) / 2, 1e7, 0, 0);
            dk.Ex(tf, (nf + 1) / 2);
            return dk.End(f);
        }

        public override string TuneLabel { get { return "Cerca il ROS piu' basso (NEC)"; } }

        /// <summary>Con una linea sola il radiatore accoppiato non porta a zero la reattanza in modo semplice: si cerca il fattore del radiatore
        /// della banda calcolata con il ROS piu' basso su 50 ohm.</summary>
        public override double Tune(double fmhz)
        {
            double f = Fp[I("b") == 0 ? 0 : 1], best = 1, rb = double.MaxValue;
            for (double sc = 0.85; sc <= 1.15; sc += 0.005)
            {
                double r = C170.Ros(C170.Zin(Mazzo(sc), f), 50);
                if (r < rb) { rb = r; best = sc; }
            }
            Scale = best;
            return Scale;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            List<double[]> el = Elementi(Scale);
            string[] nomeB = { Coppie[Math.Max(0, Math.Min(2, I("cp")))].Split('/')[0].Trim(), Coppie[Math.Max(0, Math.Min(2, I("cp")))].Split('/')[1].Trim() };
            o.TabellaTitolo = "Elementi sul boom";
            o.Tabella = new List<string[]> { new[] { "Elemento", "Banda", "Posizione", "Lunghezza" } };
            foreach (double[] e in el)
            {
                string nome = e[3] >= 1 ? (e[3] == 2 ? "radiatore accoppiato" : "radiatore") : (e[2] > 149.9 / Fp[(int)e[0]] * 0.97 ? "riflettore" : "direttore");
                o.Tabella.Add(new[] { nome, (int)e[0] == 0 ? nomeB[0] : nomeB[1], C170.N(e[1], 2) + " m", C170.N(e[2], 3) + " m" });
            }
            double boom = 0; foreach (double[] e in el) boom = Math.Max(boom, e[1]);
            o.Card("Boom", C170.N(boom, 2) + " m", el.Count + " elementi");
            o.Card("Banda calcolata", nomeB[I("b") == 0 ? 0 : 1], I("ali") == 1 ? "una sola linea sul radiatore della banda bassa" : "due linee, l'altro radiatore aperto", true);
            // interazione: coppie di elementi delle due bande vicine (entro 0,06 λ della banda alta) con lunghezze in rapporto vicino a 1/2, 2/3, 7/8
            double lh = C0 / Fp[0];
            List<string> av = new List<string>();
            foreach (double[] e1 in el)
                foreach (double[] e2 in el)
                {
                    if (e1[0] != 1 || e2[0] != 0 || Math.Abs(e1[1] - e2[1]) > 0.06 * lh) continue;
                    double r = e2[2] / e1[2];
                    foreach (double rr in new[] { 0.5, 2.0 / 3, 7.0 / 8 })
                        if (Math.Abs(r - rr) / rr < 0.04) av.Add("elementi a " + C170.N(e1[1], 2) + " e " + C170.N(e2[1], 2) + " m, rapporto " + C170.N(r, 3));
                }
            if (av.Count > 0) o.Warnings.Add("Interazione: " + string.Join("; ", av) + ". Un elemento della banda bassa vicino a uno della banda alta con lunghezze in rapporto vicino a 1/2, 2/3 o 7/8 sposta la risonanza e il F/B dell'altra banda (W1JR, QST 2/2022).");
            else o.Oks.Add("Nessuna coppia di elementi vicini in rapporto 1/2, 2/3 o 7/8.");
            o.Oks.Add("Elementi isolati dal boom (come W1JR): il boom metallico non e' nel modello.");
            o.Formula = "Valori di partenza: radiatore λ/2 - 4 %, riflettore +4 %, direttore -6 % per ogni banda (I4MFA); spaziature 0,15 e 0,175 λ; radiatore della banda alta 0,04 λ davanti a quello della bassa. " +
                "«Cerca il ROS piu' basso (NEC)» ritocca il radiatore della banda calcolata (con una linea sola, alla banda alta, quello accoppiato). Idea della Yagi bibanda a una linea: W1JR, QST 2/2022.";
            o.Nec = Mazzo(Scale);
            o.Plane = "xy";
            return o;
        }
    }

    // =============================================================================================
    // O · Carico lineare per λ/4 e dipolo accorciato (IU5HIV, RKE 1/2024) con il confronto delle perdite (S: I5WHC, RR 4/2025)
    public class CalcCaricoLineare : SimpleCalc
    {
        public override bool Tubolare { get { return true; } }
        public CalcCaricoLineare()
        {
            Key = "caricolin"; Title = "Carico lineare"; Source = "IU5HIV, RadioKit Elettronica 1/2024 (carico lineare per antenne a 1/4, il suo programma non e' incluso); modello a linea del monopolo (Schelkunoff); linea bifilare Z0 = 120 acosh(s/d); I5WHC, RR 4/2025 (efficienza)";
            Params.Add(CalcParam.Scelta("tipo", "Antenna", 0, null, "verticale λ/4", "dipolo accorciato"));
            Params.Add(new CalcParam("f", "Frequenza", 7.1, "MHz"));
            Params.Add(new CalcParam("acc", "Lunghezza fisica", 70, "%", "In % della lunghezza risonante senza carico (λ/4 per la verticale, il braccio per il dipolo)."));
            Params.Add(new CalcParam("d", "ø del conduttore", 20, "mm"));
            Params.Add(new CalcParam("dw", "ø del filo del carico", 2, "mm"));
            Params.Add(new CalcParam("s", "Distanza tra i due fili del carico", 0.15, "m", "IU5HIV: 10-20 cm."));
            Params.Add(new CalcParam("p", "Posizione del carico", 0, "%", "Dal punto di alimentazione: 0 = alla base (o al centro del dipolo), 50 = a meta'."));
            Params.Add(new CalcParam("q", "Q della bobina (confronto)", 200, ""));
            Params.Add(new CalcParam("h", "Altezza del dipolo", 10, "m") { Visibile = () => I("tipo") == 1 });
            Params.Add(new CalcParam("n", "Numero di radiali", 16, "") { Visibile = () => I("tipo") == 0 });
            Params.Add(new CalcParam("lr", "Lunghezza dei radiali", 0.25, "lambda") { Visibile = () => I("tipo") == 0 });
        }
        public override string TuneLabel { get { return "Porta in risonanza e confronta (NEC)"; } }
        bool Dip { get { return I("tipo") == 1; } }

        void Misure(out double H, out double p, out double xl, out double ls, out double z0s)
        {
            double f = P("f"), lam = C0 / f, a = P("d") / 2000, beta = 2 * Math.PI / lam / 0.97;
            H = 0.97 * lam / 4 * Math.Max(0.2, Math.Min(0.98, P("acc") / 100));
            p = H * Math.Max(0, Math.Min(0.9, P("p") / 100));
            double z0m = C170.Z0Mono(H, a), hh = H, pp = p;
            Func<double, double> fx = x =>
            {
                Complex z = -Complex.ImaginaryOne * z0m / Math.Tan(beta * (hh - pp)) + Complex.ImaginaryOne * x;
                return C170.Linea(z, z0m, beta * pp).Imaginary;
            };
            xl = C170.Bisez(fx, 0, 20000);
            double dw = P("dw") / 1000, s = Math.Max(P("s"), 2 * dw);
            z0s = 120 * Math.Log(s / dw + Math.Sqrt(s * s / (dw * dw) - 1));
            ls = Math.Atan(xl / z0s) / (2 * Math.PI / lam);
        }

        /// <summary>modo: 0 carico lineare, 1 bobina alla stessa posizione, 2 bobina alla base/centro, 3 cappello in cima/punte. k = fattore sull'elemento di carico.</summary>
        string Mazzo(int modo, double k)
        {
            double H, p, xl, ls, z0s;
            Misure(out H, out p, out xl, out ls, out z0s);
            double f = P("f"), lam = C0 / f, a = P("d") / 2000, aw = P("dw") / 2000, s = Math.Max(P("s"), 4 * aw), Q = Math.Max(20, P("q"));
            double w = 2 * Math.PI * f * 1e6, seg = lam / 60;
            bool dip = Dip;
            double h = dip ? Math.Max(1, P("h")) : 0;
            NecDeck dk = new NecDeck(Title + " " + C170.N(f, 3) + " MHz");
            int tf = 0;
            List<int[]> bob = new List<int[]>();
            double pp = modo == 2 ? 0 : p;
            foreach (int sg in dip ? new[] { 1, -1 } : new[] { 1 })
            {
                // asse lungo il quale si sviluppa il braccio: z per la verticale, y per il dipolo
                Func<double, double, double, double[]> P3 = (u, x, v) => dip ? new[] { x, sg * u, h + v } : new[] { x, v, u };
                double u0 = dip ? seg / 2 : 0;
                double up = Math.Max(u0 + 0.3, pp);
                if (dip && sg == 1) { tf = dk.W(1, 0, -seg / 2, h, 0, seg / 2, h, a); }
                if (up > u0 + 0.01)
                {
                    double[] q0 = P3(u0, 0, 0), q1 = P3(up, 0, 0);
                    int t0 = dk.Ws(seg, q0[0], q0[1], q0[2], q1[0], q1[1], q1[2], a, false, 1);
                    if (!dip) tf = t0;
                }
                double ucur = up;
                if (modo == 0)
                {
                    double l = ls * k;
                    double[] r1 = P3(ucur, 0, 0), r2 = P3(ucur, l, 0), r3 = P3(ucur + s, l, 0), r4 = P3(ucur + s, 0, 0);
                    dk.Ws(seg, r1[0], r1[1], r1[2], r2[0], r2[1], r2[2], aw, false, 2);
                    dk.W(1, r2[0], r2[1], r2[2], r3[0], r3[1], r3[2], aw);
                    dk.Ws(seg, r3[0], r3[1], r3[2], r4[0], r4[1], r4[2], aw, false, 2);
                    ucur += s;
                }
                else if (modo == 1 || modo == 2)
                {
                    double[] r1 = P3(ucur, 0, 0), r2 = P3(ucur + 0.1, 0, 0);
                    bob.Add(new[] { dk.W(1, r1[0], r1[1], r1[2], r2[0], r2[1], r2[2], a), 1 });
                    ucur += 0.1;
                }
                double[] e1 = P3(ucur, 0, 0), e2 = P3(H, 0, 0);
                if (H > ucur + 0.02) dk.Ws(seg, e1[0], e1[1], e1[2], e2[0], e2[1], e2[2], a, false, 2);
                if (modo == 3)
                {
                    double lh = Math.Max(0.05, 0.12 * lam * k);
                    foreach (double[] dd in new[] { new[] { 1.0, 0 }, new[] { -1.0, 0 }, new[] { 0, 1.0 }, new[] { 0, -1.0 } })
                    {
                        double[] c1 = P3(H, 0, 0);
                        double[] c2 = dip ? new[] { c1[0] + lh * dd[0], c1[1], c1[2] + lh * dd[1] } : new[] { c1[0] + lh * dd[0], c1[1] + lh * dd[1], c1[2] };
                        dk.Ws(seg, c1[0], c1[1], c1[2], c2[0], c2[1], c2[2], 0.002, false, 2);
                    }
                }
            }
            if (dip) dk.Ground(2);
            else dk.Ground(1, true);
            if (!dip) dk.Ld(0, tf, 1, 1, CalcQuartoTerra.ResistenzaTerra(Math.Max(1, P("n")), Math.Max(0.05, P("lr"))), 0, 0);
            double xb = modo == 2 ? XBase(H) : xl;
            foreach (int[] b in bob) { double L = xb * k / w; dk.Ld(0, b[0], 1, 1, w * L / Q, L, 0); }
            dk.Ld(5, 0, 0, 0, 5.8e7, 0, 0);
            dk.Ex(tf, 1);
            return dk.End(f);
        }

        /// <summary>Reattanza della bobina alla base per lo stesso radiatore (monopolo corto: X = -Z0 cot βH).</summary>
        double XBase(double H)
        {
            double lam = C0 / P("f"), a = P("d") / 2000, beta = 2 * Math.PI / lam / 0.97;
            return C170.Z0Mono(H, a) / Math.Tan(beta * H);
        }

        string chiaveConf;
        List<string[]> confronto;
        string Chiave() { string k = ""; foreach (CalcParam p in Params) k += p.Value.ToString("R", CultureInfo.InvariantCulture) + ";"; return k; }

        public override double Tune(double fmhz)
        {
            double f = P("f");
            confronto = new List<string[]> { new[] { "Carico (stessa lunghezza fisica)", "Efficienza", "Banda a ROS 2", "R in risonanza" } };
            string[] nomi = { "carico lineare", "bobina alla stessa posizione", Dip ? "bobina al centro" : "bobina alla base", Dip ? "cappelli alle punte" : "cappello in cima" };
            double kl = 1;
            for (int m = 0; m < 4; m++)
            {
                int mm = m;
                double k = C170.Risonanza(x => Mazzo(mm, x), f);
                if (m == 0) kl = k;
                string nec = Mazzo(m, k);
                RunResult r = C170.Calcola(nec, f);
                double bw = C170.BandaRos2(nec, f);
                confronto.Add(new[] { nomi[m], C170.N(r.Efficiency, 0) + " %", double.IsNaN(bw) ? "-" : C170.N(bw * 1000, 0) + " kHz", C170.N(r.Zin.Real, 1) + " Ω" });
            }
            chiaveConf = Chiave();
            Scale = kl;
            return Scale;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double H, p, xl, ls, z0s;
            Misure(out H, out p, out xl, out ls, out z0s);
            double f = P("f"), w = 2 * Math.PI * f * 1e6;
            bool dip = Dip;
            o.Card(dip ? "Braccio" : "Radiatore", C170.N(H, 2) + " m", C170.N(P("acc"), 0) + " % della lunghezza risonante", true);
            o.Card("Carico lineare", C170.N(ls * Scale, 2) + " m", "due fili a " + C170.N(P("s") * 100, 0) + " cm, chiusi in fondo (Z0 " + C170.N(z0s, 0) + " Ω); filo totale " + C170.N(2 * ls * Scale + P("s"), 2) + " m" + (Scale != 1 ? ", accordato col NEC" : ""), true);
            o.Card("Reattanza da aggiungere", "+j" + C170.N(xl, 0) + " Ω", "a " + C170.N(p, 2) + " m dal punto di alimentazione");
            o.Card("Bobina equivalente", Calcolatori.Henry(xl / w), "perdita " + C170.N(xl / Math.Max(20, P("q")), 2) + " Ω con Q " + C170.N(P("q"), 0));
            if (chiaveConf == Chiave() && confronto != null) { o.Tabella = confronto; o.TabellaTitolo = "Confronto con il NEC (stessa lunghezza fisica, ognuno portato in risonanza)"; }
            else o.Oks.Add("«Porta in risonanza e confronta (NEC)» accorda il carico lineare e confronta efficienza e banda con la bobina alla stessa posizione, la bobina " + (dip ? "al centro" : "alla base") + " e il cappello" + (dip ? " alle punte" : " in cima") + ".");
            o.Formula = "Modello a linea: sopra il carico il tratto aperto vale -j Z0 cot(β(H - p)), Z0 = 60 (ln(2H/a) - 1); il carico X porta a zero la reattanza alla base. Carico lineare = linea bifilare in corto, " +
                "X = Z0s tan(βl), Z0s = 120 acosh(s/d). Confronto: a parita' di lunghezza il carico verso la cima e il cappello rendono di piu' della bobina alla base (I5WHC, RR 4/2025). Idea del carico lineare: IU5HIV, RKE 1/2024.";
            if (p < 0.01 && !dip) o.Oks.Add("Carico alla base: e' la soluzione piu' comoda ma la corrente sul radiatore cala presto; spostandolo verso la cima l'efficienza sale (prova con la posizione).");
            o.Nec = Mazzo(0, Scale);
            o.Plane = dip ? "yz" : "xz";
            return o;
        }
    }

    // =============================================================================================
    // P · Filo alimentato all'estremita': bande risonanti in armonica (EFLW, RKE 1/2024)
    public class CalcEflw : SimpleCalc
    {
        static readonly string[] Serie = { "40-20-15-10 m", "80-40-20-15-10 m", "tutte le HF (80-10 m)" };
        static readonly int[][] SerieI = { new[] { 3, 5, 7, 9 }, new[] { 1, 3, 5, 7, 9 }, new[] { 1, 3, 4, 5, 6, 7, 8, 9 } };
        static readonly double[] BLo = { 1.81, 3.5, 5.3515, 7.0, 10.1, 14.0, 18.068, 21.0, 24.89, 28.0 };
        static readonly double[] BHi = { 2.0, 3.8, 5.3665, 7.2, 10.15, 14.35, 18.168, 21.45, 24.99, 29.7 };
        public CalcEflw()
        {
            Key = "eflw"; Title = "Filo risonante in armonica (EFLW)"; Source = "RadioKit Elettronica 1/2024 (EFLW: 21,12 m risonante su 40, 20, 15 e 10 m, fattore di accorciamento misurato sul posto); confronto con Dose (RK 6/2023) e l'EFHW Vibroplex (QST 2/2022)";
            Params.Add(new CalcParam("L", "Lunghezza del filo", 21.12, "m"));
            Params.Add(new CalcParam("k", "Fattore di accorciamento", 0.95, "", "Dipende da altezza, alberi, tetto e inclinazione: va misurato sul posto con il ROS (nell'articolo 0,93)."));
            Params.Add(CalcParam.Scelta("serie", "Bande scelte", 0, null, Serie));
            Params.Add(CalcParam.Scelta("tr", "Trasformatore", 0, null, "49:1", "64:1"));
            Params.Add(new CalcParam("h1", "Altezza del trasformatore", 1.5, "m"));
            Params.Add(new CalcParam("h2", "Altezza della punta", 10, "m"));
            Params.Add(new CalcParam("d", "ø del filo", 1, "mm"));
        }
        public override string TuneLabel { get { return "Lunghezza per le bande scelte"; } }
        int[] Sel { get { return SerieI[Math.Max(0, Math.Min(2, I("serie")))]; } }

        static double Scarto(double L, double k, int[] sel, out double peggiore)
        {
            peggiore = 0; double somma = 0;
            foreach (int i in sel)
            {
                double fc = CalcFilari.BandeF[i], f1 = C0 * k / (2 * L);
                int n = Math.Max(1, (int)Math.Round(fc / f1));
                double e = Math.Abs(n * f1 - fc) / fc;
                peggiore = Math.Max(peggiore, e); somma += e;
            }
            return somma;
        }

        public override double Tune(double fmhz)
        {
            double k = Math.Max(0.5, Math.Min(1, P("k"))), best = P("L"), bv = double.MaxValue;
            for (double L = 8; L <= 45; L += 0.01)
            {
                double pg;
                double sc = Scarto(L, k, Sel, out pg);
                double v = pg * 10 + sc;
                if (v < bv) { bv = v; best = L; }
            }
            Scale = best / Math.Max(0.1, P("L"));
            return Scale;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double L = Math.Max(1, P("L")) * Scale, k = Math.Max(0.5, Math.Min(1, P("k"))), f1 = C0 * k / (2 * L);
            int rapp = I("tr") == 1 ? 64 : 49;
            o.Card("Filo", C170.N(L, 2) + " m", Scale != 1 ? "lunghezza trovata per le bande scelte (scritta " + C170.N(P("L"), 2) + " m)" : "mezz'onda a " + C170.N(f1, 3) + " MHz con k " + C170.N(k, 2), true);
            o.TabellaTitolo = "Risonanze in armonica vicino alle bande scelte";
            o.Tabella = new List<string[]> { new[] { "Banda", "Mezze onde", "Risonanza piu' vicina", "Dal centro banda", "In banda?" } };
            List<double> bande = new List<double>();
            foreach (int i in Sel)
            {
                double fc = CalcFilari.BandeF[i];
                int n = Math.Max(1, (int)Math.Round(fc / f1));
                double fn = n * f1;
                bool dentro = fn >= BLo[i] && fn <= BHi[i];
                o.Tabella.Add(new[] { CalcFilari.BandeNomi[i], n.ToString(), C170.N(fn, 3) + " MHz", (fn >= fc ? "+" : "") + C170.N((fn - fc) * 1000, 0) + " kHz", dentro ? "✓ si'" : "⚠ fuori banda" });
                bande.Add(fc);
            }
            o.Bande = bande.ToArray();
            o.Formula = "Risonanze: f_n = n · c · k / (2 L) (n mezze onde, k fattore di accorciamento). «Lunghezza per le bande scelte» cerca la lunghezza che porta le armoniche piu' vicine ai centri banda insieme. " +
                "Il ROS di ogni banda lo da' il calcolo NEC (trasformatore ideale " + rapp + ":1, ROS riferito a " + C170.N(50.0 * rapp, 0) + " Ω). Fonte: EFLW, RKE 1/2024; confronto: Dose, RK 6/2023 (49:1 su FT-82-43) e l'EFHW Vibroplex (QST 2/2022).";
            o.Oks.Add("Il fattore di accorciamento dipende da altezza, alberi, tetto e inclinazione: va misurato sul posto con il ROS (esempio dell'articolo: 0,93).");
            o.Oks.Add("Per gli 80 m: bobina con un codino in punta (Filari, «con bobina»), oppure filo lungo il doppio.");
            o.Rimandi.Add(new CalcRimando { Testo = "Apri in Filari (EFHW)", Voce = "filari", Tip = "Trasformatore, nucleo, contrappeso e perdite nella scheda Filari." });
            // modello: trasformatore ideale (filo corto con la sorgente), filo dritto dalla quota h1 alla punta h2, contrappeso 0,05 λ verso il basso
            double h1 = Math.Max(0.5, P("h1")), h2 = Math.Max(0.5, P("h2")), a = P("d") / 2000;
            double dz = h2 - h1, dx = Math.Sqrt(Math.Max(0.01, L * L - dz * dz));
            double fmax = 0; foreach (double b in bande) fmax = Math.Max(fmax, b);
            double seg = Math.Min(C0 / Math.Max(fmax, 7) / 30, 0.5);
            NecDeck dk = new NecDeck(Title + " " + C170.N(L, 2) + " m");
            int tfeed = dk.W(1, 0, 0, h1 - 0.1, 0, 0, h1, a);
            dk.Ws(seg, 0, 0, h1, dx, 0, h2, a, false, 3);
            double cp = 0.05 * C0 / (bande.Count > 0 ? bande[0] : 7.1);
            double giu = Math.Min(cp, Math.Max(0, h1 - 0.1 - 0.3));
            if (giu > 0.05) dk.Ws(seg, 0, 0, h1 - 0.1, 0, 0, h1 - 0.1 - giu, a);
            if (cp - giu > 0.05) dk.Ws(seg, 0, 0, h1 - 0.1 - giu, -(cp - giu), 0, h1 - 0.1 - giu, a);
            dk.Ground(2);
            dk.Ex(tfeed, 1);
            o.Nec = "CM YA-NEC Z0=" + (50 * rapp).ToString(CultureInfo.InvariantCulture) + "\n" + dk.End(bande.Count > 0 ? bande[0] : 7.1);
            o.Z0Rif = 50 * rapp;
            o.Plane = "xz";
            return o;
        }
    }
}
