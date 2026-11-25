// YA-NEC 1.7.0 - nuovi calcolatori dal bozzetto «mining riviste» (approvato da IC8TEM il 25/11/2026 20:41).
// D: dipolo a banda larga con risonatore coassiale, L invertita / T per i 160 m, half-sloper sul traliccio, gamma match;
// G: adattamento a stub singolo e doppio; I: Yagi HF 2-3 elementi con la guida di taratura; J: dipolo multibanda a rele';
// K: verticale trappolata multibanda; L: Yagi bibanda interlacciata; O: carico lineare; P: filo risonante in armonica (EFLW).
// Fonti (solo riassunti, nessuna tabella copiata): ARRL Handbook cap. 20; VE2CV (half-sloper); D. J. Healey, QST 4/1969 (gamma match);
// IW2FND, RR 10/2023 (stub); I4MFA, RR 9/2025 (Yagi HF); IK4PKK e IK4CIE, RR 9/2025 (dipolo a rele'); IZ2IAM, RR 11/2025 (AV-5) e
// Grazioli, RKE 11/2025 (bobina con cappello); W1JR, QST 2/2022 (Yagi 12/17 m); IU5HIV, RKE 1/2024 (carico lineare); EFLW, RKE 1/2024;
// I5WHC, RR 4/2025 (efficienza); Schelkunoff-Friis (impedenza media del monopolo 60 (ln(2h/a) - 1)).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    /// <summary>Funzioni comuni dei calcolatori 1.7.0.</summary>
    static class C170
    {
        public const double C0 = 299.792458;
        public static string N(double v, int d) { return Calcolatori.N(v, d); }
        public static string Z(Complex z) { return N(z.Real, 1) + (z.Imaginary < 0 ? " -j" : " +j") + N(Math.Abs(z.Imaginary), 1) + " Ω"; }
        public static double Ros(Complex z, double z0)
        {
            Complex g = (z - z0) / (z + z0);
            double m = Complex.Abs(g);
            return m >= 0.999 ? 999 : (1 + m) / (1 - m);
        }
        /// <summary>Impedenza media del monopolo (Schelkunoff): Z0 = 60 (ln(2h/a) - 1).</summary>
        public static double Z0Mono(double h, double a) { return 60 * (Math.Log(2 * Math.Max(h, 4 * a) / a) - 1); }
        /// <summary>Linea: impedenza d'ingresso di un tratto lungo l (rad) chiuso su zl.</summary>
        public static Complex Linea(Complex zl, double z0, double bl)
        {
            Complex t = Math.Tan(bl);
            return z0 * (zl + Complex.ImaginaryOne * z0 * t) / (z0 + Complex.ImaginaryOne * zl * t);
        }
        public static double Bisez(Func<double, double> f, double lo, double hi, int n = 80)
        {
            double flo = f(lo);
            for (int i = 0; i < n; i++)
            {
                double m = (lo + hi) / 2, fm = f(m);
                if ((fm > 0) == (flo > 0)) { lo = m; flo = fm; } else hi = m;
            }
            return (lo + hi) / 2;
        }
        /// <summary>Calcolo NEC di un mazzo alla frequenza f (Z0 per il ROS).</summary>
        public static RunResult Calcola(string nec, double f, double z0 = 50)
        {
            NecModel m = NecModel.Parse(nec);
            m.Z0 = z0;
            return Analysis.Run(m, m.BuildGeometry(), f, 2.0);
        }
        /// <summary>Reattanza in ingresso (prima sorgente) senza il diagramma.</summary>
        public static Complex Zin(string nec, double f)
        {
            NecModel m = NecModel.Parse(nec);
            SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, f);
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            return e.Sources[0].Z;
        }
        /// <summary>Secante sul fattore s finche' la reattanza della prima sorgente va a zero (passi limitati al 5 %).</summary>
        public static double Risonanza(Func<double, string> mazzo, double f, double s0 = 1)
        {
            Func<double, double> x = s => Zin(mazzo(s), f).Imaginary;
            double a = s0, b = s0 * 1.02, fa = x(a), fb = x(b);
            for (int it = 0; it < 25 && Math.Abs(fb) > 0.1 && fb != fa; it++)
            {
                double c = Math.Max(b * 0.95, Math.Min(b * 1.05, b - fb * (b - a) / (fb - fa)));
                a = b; fa = fb; b = c; fb = x(b);
            }
            return b;
        }
        /// <summary>Banda a ROS 2 (riferito a R in risonanza) dalla pendenza della reattanza: Q = f/(2R) dX/df, B = f (S-1)/(Q √S).</summary>
        public static double BandaRos2(string nec, double f)
        {
            Complex z = Zin(nec, f), z1 = Zin(nec, f * 1.002), z2 = Zin(nec, f * 0.998);
            double dxdf = (z1.Imaginary - z2.Imaginary) / (0.004 * f);
            double q = f / (2 * Math.Max(0.01, z.Real)) * dxdf;
            return q > 0 ? f / q / Math.Sqrt(2) : double.NaN;
        }
        public static readonly string[] BandeHfN = { "160 m", "80 m", "60 m", "40 m", "30 m", "20 m", "17 m", "15 m", "12 m", "10 m", "6 m" };
        public static readonly double[] BandeHfF = { 1.85, 3.65, 5.36, 7.1, 10.125, 14.175, 18.118, 21.225, 24.94, 28.5, 50.15 };
        public static Band Banda(double f) { return Bands.All.Find(b => b.Contains(f)); }
    }

    // =============================================================================================
    // D1 · Dipolo a banda larga con risonatore coassiale (stub λ/4 in corto in parallelo + trasformatore λ/4)
    public class CalcBandaLarga : SimpleCalc
    {
        public CalcBandaLarga()
        {
            Key = "bandalarga"; Title = "Dipolo a banda larga con risonatore coassiale"; Source = "ARRL Handbook cap. 20 (dipolo a banda larga con risonatore coassiale, solo l'idea); doppia sintonia: stub λ/4 in corto in parallelo al dipolo e trasformatore λ/4 (formule classiche delle linee)";
            Params.Add(new CalcParam("f", "Frequenza centrale", 3.65, "MHz"));
            Params.Add(new CalcParam("h", "Altezza", 15, "m", "0 = spazio libero."));
            Params.Add(new CalcParam("d", "ø del filo", 2, "mm"));
            Params.Add(new CalcParam("vf", "Fattore di velocita' del coassiale", 0.66, ""));
            Params.Add(new CalcParam("zs", "Impedenza del risonatore", 0, "Ω", "0 = la migliore calcolata. Impedenze sotto 50 Ω: piu' coassiali uguali in parallelo (due da 50 = 25 Ω)."));
            Params.Add(new CalcParam("zt", "Impedenza del trasformatore", 0, "Ω", "0 = la migliore calcolata (tra 25 e 100 Ω)."));
        }

        // modello RLC serie del dipolo vicino alla risonanza: Z = R (1 + j 2 Q d), d = (f - f0)/f0
        static double RosRlc(double d, double ra, double qa, double zs, double zt, bool conRis)
        {
            Complex za = new Complex(ra, ra * 2 * qa * d);
            if (!conRis) return C170.Ros(za, 50);
            double bl = Math.PI / 2 * (1 + d);
            Complex ys = -Complex.ImaginaryOne / (zs * Math.Tan(bl));   // stub in corto: Y = -j cot(βl) / Zs
            Complex zp = 1 / (1 / za + ys);
            return C170.Ros(C170.Linea(zp, zt, bl), 50);
        }
        static double Banda(double ra, double qa, double zs, double zt, bool conRis)
        {
            double hi = 0, lo = 0;
            for (double d = 0; d < 0.3; d += 0.001) { if (RosRlc(d, ra, qa, zs, zt, conRis) > 2) break; hi = d; }
            for (double d = 0; d > -0.3; d -= 0.001) { if (RosRlc(d, ra, qa, zs, zt, conRis) > 2) break; lo = d; }
            return hi - lo;
        }

        // «Porta in risonanza (NEC)»: risonanza del dipolo da solo, poi R e Q del dipolo vero (quota e terreno) per scegliere risonatore e trasformatore
        bool soloDipolo;
        double raNec = double.NaN, qaNec = double.NaN;
        string chiaveNec;
        string Chiave() { string k = ""; foreach (CalcParam p in Params) k += p.Value.ToString("R", CultureInfo.InvariantCulture) + ";"; return k; }
        public override double Tune(double fmhz)
        {
            double f = P("f");
            soloDipolo = true;
            try
            {
                double s0 = Scale;
                Scale = C170.Risonanza(sc => { Scale = sc; return Compute().Nec; }, f, s0);
                string nec = Compute().Nec;
                Complex z = C170.Zin(nec, f), z1 = C170.Zin(nec, f * 1.002), z2 = C170.Zin(nec, f * 0.998);
                raNec = Math.Max(1, z.Real);
                qaNec = Math.Max(1, f / (2 * raNec) * (z1.Imaginary - z2.Imaginary) / (0.004 * f));
                chiaveNec = Chiave();
            }
            finally { soloDipolo = false; }
            return Scale;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), lam = C0 / f, a = P("d") / 2000, h = P("h"), vf = Math.Max(0.1, Math.Min(1, P("vf")));
            double L = 142.65 / f * Scale;
            bool daNec = chiaveNec == Chiave() && !double.IsNaN(raNec);
            double ra = daNec ? raNec : 73, qa = daNec ? qaNec : Math.PI / 4 * 120 * (Math.Log(L / a) - 1) / ra;
            double zs = P("zs"), zt = P("zt");
            if (zs <= 0 || zt <= 0)
            {
                double best = -1, bs = 50, bt = 50;
                for (double s = 3; s <= 150; s *= 1.12)
                    for (double t = 25; t <= 100; t += 5)
                    {
                        double ss = zs > 0 ? zs : s, tt = zt > 0 ? zt : t;
                        double b = Banda(ra, qa, ss, tt, true);
                        if (b > best) { best = b; bs = ss; bt = tt; }
                    }
                zs = bs; zt = bt;
            }
            double b0 = Banda(ra, qa, zs, zt, false), b1 = Banda(ra, qa, zs, zt, true);
            o.Card("Dipolo", C170.N(L, 2) + " m", "142,65 / f" + (Scale != 1 ? ", accordato col NEC" : ""), true);
            o.Card("Risonatore (stub in corto)", C170.N(zs, 0) + " Ω, " + C170.N(vf * lam / 4, 2) + " m", "λ/4 con VF " + C170.N(vf, 2) + ", in parallelo ai morsetti del dipolo", true);
            o.Card("Trasformatore λ/4", C170.N(zt, 0) + " Ω, " + C170.N(vf * lam / 4, 2) + " m", "tra il dipolo e il cavo da 50 Ω");
            o.Card("Banda a ROS 2 (stima)", C170.N(b1 * f * 1000, 0) + " kHz", "dipolo semplice " + C170.N(b0 * f * 1000, 0) + " kHz (Q del dipolo " + C170.N(qa, 1) + ")");
            o.Card("Dipolo in risonanza", C170.N(ra, 1) + " Ω, Q " + C170.N(qa, 1), daNec ? "dal NEC (quota e terreno)" : "stima: 73 Ω; «Porta in risonanza (NEC)» li misura");
            if (zs < 45) o.Oks.Add("Risonatore da " + C170.N(zs, 0) + " Ω: si ottiene con " + Math.Max(2, (int)Math.Round(50 / zs)) + " coassiali da 50 Ω in parallelo (stessa lunghezza).");
            o.Formula = "Formule: dipolo 142,65/f; vicino alla risonanza il dipolo e' un RLC serie, Z = R (1 + j2Qδ), Q = (π/4) 120 (ln(L/a) - 1) / R (Schelkunoff). " +
                "Lo stub λ/4 in corto in parallelo ha suscettanza opposta a quella del dipolo e allarga la banda; il trasformatore λ/4 riporta a 50 Ω. " +
                "Impedenze scelte cercando la banda a ROS 2 piu' larga sul modello RLC; il NEC verifica col dipolo vero (stub e trasformatore come linee). Idea: ARRL Handbook cap. 20.";
            o.Chart = new CalcChart { Title = "ROS stimato contro la frequenza", XLabel = "MHz", YLabel = "ROS", YMin = 1, YMax = 4, Nome1 = "con il risonatore", Nome2 = "dipolo semplice" };
            o.Chart.X = Calcolatori.Lin(f * 0.9, f * 1.1, 81); o.Chart.Y = new double[81]; o.Chart.Y2 = new double[81];
            for (int i = 0; i < 81; i++)
            {
                double d = o.Chart.X[i] / f - 1;
                o.Chart.Y[i] = Math.Min(4, RosRlc(d, ra, qa, zs, zt, true)); o.Chart.Y2[i] = Math.Min(4, RosRlc(d, ra, qa, zs, zt, false));
            }
            o.Chart.MarkX = f;
            // modello: dipolo lungo y, filo di alimentazione lontano (1 segmento) con la sorgente, linea TL del trasformatore e stub sul centro
            NecDeck dk = new NecDeck(Title + " " + C170.N(f, 3) + " MHz");
            double seg = lam / 50, z = h > 0 ? h : 0;
            int nd = Odd(L / seg, 11);
            int td = dk.W(nd, 0, -L / 2, z, 0, L / 2, z, a);
            if (soloDipolo)
            {
                dk.Ground(h > 0 ? 2 : 0);
                dk.Ex(td, (nd + 1) / 2);
                o.Nec = dk.End(f);
                o.Plane = "yz";
                return o;
            }
            int tf = dk.W(1, lam, -0.1, Math.Max(z, 1), lam, 0.1, Math.Max(z, 1), 0.001);
            dk.Ground(h > 0 ? 2 : 0);
            dk.Tl(tf, 1, td, (nd + 1) / 2, zt, lam / 4);
            dk.Ex(tf, 1);
            o.Nec = string.Format(CultureInfo.InvariantCulture, "CM YA-NEC STUB {0} {1} {2:R} {3:R} {4:R} corto\n", td, (nd + 1) / 2, zs, vf * lam / 4, vf) + dk.End(f);
            o.Oks.Add("Nel modello NEC la sorgente sta su un filo corto lontano dall'antenna e la linea TL (lunghezza elettrica λ/4) fa da trasformatore; lo stub e' in parallelo ai morsetti del dipolo.");
            o.Plane = "yz";
            return o;
        }
    }

    // =============================================================================================
    // D2 · L invertita e T per i 160 m
    public class CalcLT160 : SimpleCalc
    {
        public CalcLT160()
        {
            Key = "lt160"; Title = "L invertita e T per i 160 m"; Source = "ARRL Handbook cap. 20 (antenne a L invertita e a T per i 160 m, solo l'idea); ON4UN, Low-Band DXing (resistenza di terra, Tab. 9-1)";
            Params.Add(new CalcParam("f", "Frequenza", 1.83, "MHz"));
            Params.Add(CalcParam.Scelta("tipo", "Forma", 0, "L invertita: verticale e un tratto orizzontale. T: verticale con due bracci uguali in cima (cappello), i bracci non irradiano.", "L invertita", "T"));
            Params.Add(new CalcParam("h", "Altezza del tratto verticale", 18, "m"));
            Params.Add(new CalcParam("d", "ø del filo", 2, "mm"));
            Params.Add(new CalcParam("n", "Numero di radiali", 32, ""));
            Params.Add(new CalcParam("lr", "Lunghezza dei radiali", 0.25, "lambda"));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), lam = C0 / f, a = P("d") / 2000, h = Math.Max(1, P("h"));
            bool t = I("tipo") == 1;
            double resto = Math.Max(0.02 * lam, 0.97 * lam / 4 - h);
            double braccio = (t ? 0.62 * resto : resto) * Scale;
            double rg = CalcQuartoTerra.ResistenzaTerra(Math.Max(1, P("n")), Math.Max(0.05, P("lr")));
            double rrMax = 160 * Math.PI * Math.PI * Math.Pow(h / lam, 2);
            double rr = t ? rrMax * 0.9 : Math.Min(36, rrMax * 0.75);
            double eff = rr / (rr + rg);
            o.Card("Tratto verticale", C170.N(h, 1) + " m", C170.N(h / lam, 3) + " λ", true);
            o.Card(t ? "Bracci in cima (ognuno)" : "Tratto orizzontale", C170.N(braccio, 2) + " m", Scale != 1 ? "accordato col NEC" : "stima, poi «Porta in risonanza»", true);
            o.Card("Resistenza di terra", C170.N(rg, 1) + " Ω", "ON4UN Tab. 9-1: " + C170.N(P("n"), 0) + " radiali da " + C170.N(P("lr"), 2) + " λ");
            o.Card("Efficienza (stima)", C170.N(eff * 100, 0) + " %", "Rr circa " + C170.N(rr, 0) + " Ω contro " + C170.N(rg, 0) + " Ω di terra");
            o.Formula = "Formule: tratto orizzontale circa 0,97 λ/4 meno la verticale (L), bracci della T circa 0,62 di quella misura; Rr fino a 160 π² (h/λ)² con corrente quasi uniforme sulla verticale " +
                "(la T ci va vicino, la L meno); terra dalla Tab. 9-1 di ON4UN. Nel modello NEC terreno perfetto e la resistenza di terra come carico alla base (come la verticale λ/4).";
            if (!t) o.Oks.Add("L invertita: il tratto orizzontale irradia anche in polarizzazione orizzontale ad angoli alti; con la T i due bracci si annullano.");
            if (h < lam / 8) o.Warnings.Add("Verticale sotto λ/8: resistenza di radiazione bassa, la terra pesa molto sull'efficienza (I5WHC, RR 4/2025).");
            NecDeck dk = new NecDeck(Title + " " + (t ? "T" : "L") + " " + C170.N(f, 3) + " MHz");
            double seg = lam / 60;
            int tv = dk.Ws(seg, 0, 0, 0, 0, 0, h, a, false, 5);
            if (t) { dk.Ws(seg, 0, 0, h, 0, braccio, h, a, false, 3); dk.Ws(seg, 0, 0, h, 0, -braccio, h, a, false, 3); }
            else dk.Ws(seg, 0, 0, h, 0, braccio, h, a, false, 3);
            dk.Ground(1, true);
            dk.Ld(0, tv, 1, 1, rg, 0, 0);
            dk.Ex(tv, 1);
            o.Nec = dk.End(f);
            o.Plane = "yz";
            return o;
        }
    }

    // =============================================================================================
    // D3 · Half-sloper sul traliccio (VE2CV)
    public class CalcHalfSloper : SimpleCalc
    {
        public CalcHalfSloper()
        {
            Key = "halfsloper"; Title = "Half-sloper sul traliccio"; Source = "VE2CV (ARRL Handbook cap. 20: half-sloper sul traliccio con la Yagi in cima, solo l'idea)";
            Params.Add(new CalcParam("f", "Frequenza", 3.65, "MHz"));
            Params.Add(new CalcParam("H", "Altezza del traliccio", 18, "m"));
            Params.Add(new CalcParam("dt", "Diametro equivalente del traliccio", 300, "mm", "Un traliccio a sezione triangolare di lato s vale circa un tubo di diametro 0,7 s."));
            Params.Add(new CalcParam("hf", "Quota dell'alimentazione", 16, "m", "Sul traliccio, di solito poco sotto la Yagi."));
            Params.Add(new CalcParam("ang", "Inclinazione del filo", 45, "gradi", "Angolo del filo sull'orizzontale."));
            Params.Add(new CalcParam("lh", "Yagi in cima: lunghezza degli elementi", 10, "m", "La Yagi fa da cappello capacitivo: nel modello due fili incrociati lunghi cosi'. 0 = traliccio senza Yagi."));
            Params.Add(new CalcParam("d", "ø del filo", 2, "mm"));
        }
        public override string TuneLabel { get { return "Cerca il ROS piu' basso (NEC)"; } }

        /// <summary>L'impedenza dell'half-sloper dipende da traliccio e carico in cima: si cerca la lunghezza del filo con il ROS piu' basso su 50 ohm
        /// (con la risonanza X = 0 si puo' finire su un punto ad alta impedenza).</summary>
        public override double Tune(double fmhz)
        {
            double f = P("f"), best = 1, rb = double.MaxValue;
            for (double sc = 0.5; sc <= 1.6; sc += 0.02)
            {
                Scale = sc;
                double r = C170.Ros(C170.Zin(Compute().Nec, f), 50);
                if (r < rb) { rb = r; best = sc; }
            }
            Scale = best;
            return best;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), lam = C0 / f, H = Math.Max(3, P("H")), hf = Math.Max(1, Math.Min(H - 0.5, P("hf"))), ang = Math.Max(10, Math.Min(80, P("ang"))) * Math.PI / 180;
            double a = P("d") / 2000, at = P("dt") / 2000, lh = Math.Max(0, P("lh"));
            double ls = 0.95 * lam / 4 * Scale;
            double gap = 0.4;
            double zEnd = hf - ls * Math.Sin(ang), xEnd = gap + ls * Math.Cos(ang);
            if (zEnd < 0.5)
            {
                ls = (hf - 0.5) / Math.Sin(ang);
                zEnd = 0.5; xEnd = gap + ls * Math.Cos(ang);
                o.Warnings.Add("Il filo toccherebbe terra: accorciato a " + C170.N(ls, 1) + " m. Alza l'alimentazione o riduci l'inclinazione.");
            }
            o.Card("Filo inclinato", C170.N(ls, 2) + " m", Scale != 1 ? "ROS piu' basso trovato col NEC" : "0,95 λ/4, poi «Cerca il ROS piu' basso»", true);
            o.Card("Ancoraggio", C170.N(xEnd, 1) + " m dal traliccio", "a " + C170.N(zEnd, 1) + " m da terra");
            o.Card("Alimentazione", C170.N(hf, 1) + " m", "coassiale: calza al traliccio, centrale al filo");
            o.Formula = "Half-sloper (VE2CV): il filo inclinato lungo circa λ/4 e il traliccio con la Yagi in cima formano insieme l'antenna; l'impedenza dipende molto da altezza del traliccio, " +
                "carico in cima e quota dell'alimentazione (di solito 25-100 Ω): la da' il NEC. Terreno perfetto sotto il traliccio a terra.";
            o.Oks.Add("Il diagramma ha un massimo debole verso il lato del filo e polarizzazione mista: e' un'antenna per le bande basse quando il traliccio c'e' gia'.");
            NecDeck dk = new NecDeck(Title + " " + C170.N(f, 3) + " MHz");
            double seg = lam / 50;
            dk.Ws(seg, 0, 0, 0, 0, 0, hf, at, false, 3);
            int tt = dk.Ws(seg, 0, 0, hf, 0, 0, H, at, false, 2);
            if (lh > 0) { dk.Ws(seg, 0, 0, H, lh / 2, 0, H, 0.015); dk.Ws(seg, 0, 0, H, -lh / 2, 0, H, 0.015); dk.Ws(seg, 0, 0, H, 0, lh / 2, H, 0.015); dk.Ws(seg, 0, 0, H, 0, -lh / 2, H, 0.015); }
            int tf = dk.W(1, 0, 0, hf, gap, 0, hf, a);
            dk.Ws(seg, gap, 0, hf, xEnd, 0, zEnd, a, false, 3);
            dk.Ground(1, true);
            dk.Ex(tf, 1);
            o.Nec = dk.End(f);
            o.Plane = "xz";
            if (lh > 0) o.Oks.Add("La Yagi in cima e' modellata come due fili incrociati: con il NEC conta la capacita' verso il cielo, non i dettagli della Yagi.");
            return o;
        }
    }

    // =============================================================================================
    // D4 · Gamma match (Healey, QST 4/1969)
    public class CalcGammaMatch : SimpleCalc
    {
        public CalcGammaMatch()
        {
            Key = "gamma"; Title = "Gamma match"; Source = "D. J. Healey W3PVZ, \"An Examination of the Gamma Match\", QST 4/1969 (formule classiche); ARRL Handbook cap. 20";
            Params.Add(new CalcParam("f", "Frequenza", 14.175, "MHz"));
            Params.Add(new CalcParam("L", "Lunghezza dell'elemento", 0, "m", "0 = mezz'onda risonante (142,65/f) come dipolo; per il radiatore di una Yagi scrivi la sua lunghezza."));
            Params.Add(new CalcParam("D", "ø dell'elemento", 25, "mm"));
            Params.Add(new CalcParam("dg", "ø della barra del gamma", 8, "mm"));
            Params.Add(new CalcParam("s", "Distanza barra-elemento (asse-asse)", 100, "mm"));
            Params.Add(new CalcParam("ra", "R al centro dell'elemento", 73, "Ω", "73 Ω per il dipolo da solo; per il radiatore di una Yagi 15-35 Ω (dal NEC del modello completo)."));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            CanTune = true;
            double f = P("f"), lam = C0 / f, D = P("D") / 1000, d = P("dg") / 1000, s = Math.Max(P("s") / 1000, (D + d) / 2 * 1.05);
            double L = P("L") > 0 ? P("L") : 142.65 / f;
            double ra = Math.Max(1, P("ra"));
            double alfa = Math.Log(2 * s / d) / Math.Log(2 * s / D);   // fattore di divisione della corrente (Healey)
            double k = (1 + alfa) * (1 + alfa);
            double z0 = 60 * Acosh((4 * s * s - d * d - D * D) / (2 * d * D));
            double beta = 2 * Math.PI / lam;
            Func<double, Complex> zin = lg =>
            {
                Complex zt = k * ra / 2, zs = Complex.ImaginaryOne * z0 * Math.Tan(beta * lg);
                return zt * zs / (zt + zs);
            };
            double lgMax = lam / 8;
            double lg = double.NaN;
            if (zin(lgMax).Real >= 50) lg = C170.Bisez(x => zin(x).Real - 50, 0.002, lgMax);
            bool formulaOk = !double.IsNaN(lg);
            if (!formulaOk) lg = lam / 20;   // partenza per la ricerca con il NEC
            lg *= Scale;
            Complex z = zin(lg);
            double w = 2 * Math.PI * f * 1e6, cs = z.Imaginary > 0 ? 1 / (w * z.Imaginary) : double.NaN;
            if (Scale != 1 && chiaveC == Chiave() && !double.IsNaN(cNec)) cs = cNec;   // condensatore trovato con il NEC
            o.Card("Rapporto di salita", C170.N(k, 2), "(1 + α)², α = " + C170.N(alfa, 2));
            o.Card("Z0 barra-elemento", C170.N(z0, 0) + " Ω", "linea a fili diversi");
            if (!formulaOk && Scale == 1) o.Warnings.Add("Con questa R e queste misure il gamma non arriva a 50 Ω: avvicina la barra o usane una piu' sottile (rapporto di salita piu' alto).");
            {
                o.Card("Lunghezza del gamma", C170.N(lg * 100, 1) + " cm", "dal centro al ponticello" + (Scale != 1 ? " (accordato col NEC)" : ""), true);
                o.Card("Condensatore in serie", C170.N(cs * 1e12, 0) + " pF", "annulla +j" + C170.N(z.Imaginary, 0) + " Ω", true);
            }
            o.Formula = "Formule (Healey, QST 4/1969): α = ln(2s/d) / ln(2s/D), rapporto (1 + α)²; Z0 = 60 acosh((4s² - d² - D²)/(2dD)); la meta' dell'antenna vista attraverso il rapporto, (1+α)² R/2, " +
                "e' in parallelo al tratto di linea in corto j Z0 tan(βl): la lunghezza l porta la parte reale a 50 Ω, il condensatore in serie toglie la reattanza. Il NEC verifica sul dipolo con il gamma vero.";
            // modello: elemento lungo y spezzato al centro e al ponticello; barra sopra l'elemento (z = s); sorgente e condensatore sul filo verticale al centro
            if (lg > 0.02)
            {
                NecDeck dk = new NecDeck(Title + " " + C170.N(f, 3) + " MHz");
                double seg = lam / 50;
                dk.Ws(seg, 0, -L / 2, 0, 0, 0, 0, D / 2, false, 3);
                dk.Ws(seg, 0, 0, 0, 0, lg, 0, D / 2, false, 2);
                dk.Ws(seg, 0, lg, 0, 0, L / 2, 0, D / 2, false, 3);
                int tfeed = dk.W(1, 0, 0, 0, 0, 0, s, d / 2);
                dk.Ws(seg, 0, 0, s, 0, lg, s, d / 2, false, 2);
                dk.W(1, 0, lg, s, 0, lg, 0, d / 2);
                dk.Ground(0);
                if (!double.IsNaN(cs) && !senzaC) dk.Ld(0, tfeed, 1, 1, 0, 0, cs);
                dk.Ex(tfeed, 1);
                o.Nec = dk.End(f);
                o.Plane = "yz";
            }
            else CanTune = false;
            return o;
        }
        static double Acosh(double x) { return Math.Log(x + Math.Sqrt(Math.Max(0, x * x - 1))); }

        public override string TuneLabel { get { return "Accorda gamma e condensatore (NEC)"; } }
        double cNec = double.NaN; string chiaveC;
        string Chiave() { string k = ""; foreach (CalcParam p in Params) k += p.Value.ToString("R", CultureInfo.InvariantCulture) + ";"; return k; }
        bool senzaC;

        /// <summary>Si cerca la lunghezza del gamma che porta R a 50 ohm nel modello NEC senza condensatore (X induttiva), poi C = 1/(ωX).</summary>
        public override double Tune(double fmhz)
        {
            double f = P("f"), w = 2 * Math.PI * f * 1e6, best = 1, eb = double.MaxValue, xb = double.NaN;
            senzaC = true;
            try
            {
                for (double sc = 0.3; sc <= 3.0; sc += 0.025)
                {
                    Scale = sc;
                    CalcOutput o = Compute();
                    if (o.Nec == null) continue;
                    Complex z = C170.Zin(o.Nec, f);
                    double e = Math.Abs(z.Real - 50) + (z.Imaginary > 0 ? 0 : 1000);
                    if (e < eb) { eb = e; best = sc; xb = z.Imaginary; }
                }
            }
            finally { senzaC = false; }
            Scale = best;
            cNec = xb > 0 ? 1 / (w * xb) : double.NaN;
            chiaveC = Chiave();
            return best;
        }
    }

    // =============================================================================================
    // G · Adattamento a stub singolo e doppio (formule classiche delle linee; IW2FND, RR 10/2023)
    public class CalcStub : SimpleCalc
    {
        public CalcStub()
        {
            Key = "stub"; Title = "Adattamento a stub"; Source = "IW2FND, RadioRivista 10/2023; formule classiche delle linee (stub singolo e doppio)";
            Params.Add(new CalcParam("r", "Carico: R", 25, "Ω"));
            Params.Add(new CalcParam("x", "Carico: X", -30, "Ω"));
            Params.Add(new CalcParam("z0", "Z0 della linea", 50, "Ω"));
            Params.Add(new CalcParam("vf", "Fattore di velocita'", 0.66, ""));
            Params.Add(new CalcParam("f", "Frequenza", 14.175, "MHz"));
            Params.Add(CalcParam.Scelta("tipo", "Stub", 0, "In corto: piu' facile da fare e da regolare; aperto: va isolato in punta.", "in corto", "aperto"));
            Params.Add(CalcParam.Scelta("modo", "Adattamento", 0, null, "stub singolo", "stub doppio"));
            CalcParam psp = CalcParam.Scelta("sp", "Distanza tra gli stub", 0, null, "λ/8", "3λ/8");
            psp.Visibile = () => I("modo") == 1;
            Params.Add(psp);
            CanTune = false;
            PuoUsareModello = true;
        }

        static Complex YLinea(Complex yl, double y0, double bl)
        {
            // ammettenze: stessa formula della linea con le ammettenze
            Complex t = Math.Tan(bl);
            return y0 * (yl + Complex.ImaginaryOne * y0 * t) / (y0 + Complex.ImaginaryOne * yl * t);
        }
        double LunghStub(double b, double y0, bool corto)
        {
            // suscettanza voluta b (S): corto Y = -j y0 cot(βl); aperto Y = j y0 tan(βl)
            double bl = corto ? Math.Atan2(-y0, b) : Math.Atan2(b, y0);
            if (bl < 0) bl += Math.PI;
            return bl / (2 * Math.PI);
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double z0 = Math.Max(1, P("z0")), y0 = 1 / z0, vf = Math.Max(0.1, Math.Min(1, P("vf"))), f = P("f"), lam = C0 / f * vf;
            Complex zl = new Complex(P("r"), P("x"));
            string orig = "valori scritti";
            if (UsaModello && Model != null && Model.Geometry.Count > 0 && Model.Sources.Count > 0)
            {
                try
                {
                    double fm = double.IsNaN(MainFreqMHz) || MainFreqMHz <= 0 ? Model.FreqMHz : MainFreqMHz;
                    SegmentGeometry g = Model.BuildGeometry();
                    if (g.N <= 800)
                    {
                        Nec2Engine e = new Nec2Engine(g);
                        e.Solve(fm, Analysis.EffectiveSources(Model, g, fm), Model.EffectiveLoads(), Model.Ground, Model.ExtendedKernel, Model.Rkh, Model.EffectiveNetworks(fm));
                        zl = e.Sources[0].Z; f = fm; lam = C0 / f * vf;
                        DalModello["r"] = zl.Real; DalModello["x"] = zl.Imaginary; DalModello["f"] = f;
                        orig = "modello aperto a " + C170.N(fm, 3) + " MHz";
                    }
                    else o.Oks.Add("Modello aperto con piu' di 800 segmenti: carico dai valori scritti.");
                }
                catch (Exception ex) { o.Warnings.Add("Modello aperto non calcolabile (" + ex.Message + "): carico dai valori scritti."); }
            }
            bool corto = I("tipo") == 0;
            Complex yl = 1 / zl;
            o.Card("Carico", C170.Z(zl), orig + ", ROS " + C170.N(Math.Min(C170.Ros(zl, z0), 99), 2));
            List<double[]> percorsi = new List<double[]>();   // per il disegno: d (λ) dal carico e l (λ) dello stub
            o.TabellaTitolo = "Soluzioni";
            if (I("modo") == 0)
            {
                // stub singolo: distanza d dove Re(y) = y0, poi lo stub annulla la suscettanza
                o.Tabella = new List<string[]> { new[] { "Soluzione", "Distanza dal carico", "Lunghezza dello stub", "Suscettanza da annullare" } };
                Func<double, double> g = d => YLinea(yl, y0, 2 * Math.PI * d).Real - y0;
                int nsol = 0;
                double prev = g(0);
                for (int i = 1; i <= 2000 && nsol < 2; i++)
                {
                    double d1 = i * 0.5 / 2000, gv = g(d1);
                    if ((gv > 0) != (prev > 0))
                    {
                        double d = C170.Bisez(g, d1 - 0.5 / 2000, d1);
                        double b = YLinea(yl, y0, 2 * Math.PI * d).Imaginary, ls = LunghStub(-b, y0, corto);
                        nsol++;
                        o.Tabella.Add(new[] { nsol.ToString(), C170.N(d, 4) + " λ = " + C170.N(d * lam, 3) + " m", C170.N(ls, 4) + " λ = " + C170.N(ls * lam, 3) + " m", C170.N(b * 1000, 2) + " mS" });
                        percorsi.Add(new[] { d, ls });
                    }
                    prev = gv;
                }
                if (nsol == 0) o.Tabella.Add(new[] { "-", "carico gia' adattato", "", "" });
            }
            else
            {
                // stub doppio: primo stub sul carico, secondo a distanza fissa; vietata la zona g > y0 / sin²(βd)
                double ds = I("sp") == 0 ? 0.125 : 0.375, bd = 2 * Math.PI * ds;
                o.Tabella = new List<string[]> { new[] { "Soluzione", "Stub sul carico", "Stub a " + (I("sp") == 0 ? "λ/8" : "3λ/8") + " (" + C170.N(ds * lam, 3) + " m)", "" } };
                double gmax = y0 / Math.Pow(Math.Sin(bd), 2);
                if (yl.Real > gmax)
                {
                    o.Tabella.Add(new[] { "-", "non adattabile", "", "" });
                    o.Warnings.Add("Il carico cade nella zona che lo stub doppio con questa distanza non puo' adattare (G > Y0/sin²(βd)): sposta il primo stub di λ/8 lungo la linea o usa lo stub singolo.");
                }
                else
                {
                    Func<double, double> g = b1 => YLinea(yl + Complex.ImaginaryOne * b1, y0, bd).Real - y0;
                    int nsol = 0;
                    double lo = -20 * y0, hi = 20 * y0, step = (hi - lo) / 4000, prev = g(lo);
                    for (int i = 1; i <= 4000 && nsol < 2; i++)
                    {
                        double b = lo + i * step, gv = g(b);
                        if ((gv > 0) != (prev > 0))
                        {
                            double b1 = C170.Bisez(g, b - step, b);
                            double b2 = -YLinea(yl + Complex.ImaginaryOne * b1, y0, bd).Imaginary;
                            double l1 = LunghStub(b1, y0, corto), l2 = LunghStub(b2, y0, corto);
                            nsol++;
                            o.Tabella.Add(new[] { nsol.ToString(), C170.N(l1, 4) + " λ = " + C170.N(l1 * lam, 3) + " m", C170.N(l2, 4) + " λ = " + C170.N(l2 * lam, 3) + " m", "" });
                        }
                        prev = gv;
                    }
                }
            }
            o.Formula = "Linea: y(d) = Y0 (yL + jY0 tan βd)/(Y0 + j yL tan βd). Stub singolo: alla distanza d dove Re(y) = Y0 lo stub annulla la suscettanza (in corto: Y = -jY0 cot βl; aperto: Y = jY0 tan βl). " +
                "Stub doppio: il primo stub sul carico porta y sul cerchio che, dopo la distanza fissa, cade su Re(y) = Y0; zona non adattabile G > Y0/sin²(βd). Lunghezze fisiche con il fattore di velocita'. Fonte: IW2FND, RR 10/2023.";
            DisegnoSmith(o, zl, z0, percorsi);
            o.Nec = null;
            return o;
        }

        // carta di Smith con il percorso: carico -> (cerchio di ROS costante) -> punto dello stub -> (cerchio g = 1) -> centro
        static void DisegnoSmith(CalcOutput o, Complex zl, double z0, List<double[]> percorsi)
        {
            o.Plane = "xz"; o.HideWires = true;
            double R = 1;
            o.Mark(1, 0, 0, null, 3, 0, 0, R);
            o.Mark(1, 0.5, 0, null, 3, 0, 0, 0.5, true);     // r = 1
            o.Mark(1, -0.5, 0, null, 2, 0, 0, 0.5, true);    // g = 1 (ammettenze)
            o.Mark(2, -R, 0, null, 3, R, 0);
            Complex gl = (zl - z0) / (zl + z0);
            o.Mark(1, gl.Real, gl.Imaginary, null, 5, 0, 0, 0.03);
            o.Mark(0, gl.Real + 0.05, gl.Imaginary + 0.05, "carico", 5);
            o.Mark(1, 0, 0, null, 4, 0, 0, 0.025);
            o.Mark(0, 0.04, -0.09, "adattato", 4);
            int col = 1;
            foreach (double[] p in percorsi)
            {
                // rotazione verso il generatore: Γ(d) = Γ_L e^{-j 2 β d}
                Complex prevP = gl;
                int n = 40;
                for (int i = 1; i <= n; i++)
                {
                    Complex gi = gl * Complex.FromPolarCoordinates(1, -4 * Math.PI * p[0] * i / n);
                    o.Mark(2, prevP.Real, prevP.Imaginary, null, col, gi.Real, gi.Imaginary);
                    prevP = gi;
                }
                o.Mark(2, prevP.Real, prevP.Imaginary, null, col, 0, 0, 0, true);
                o.Mark(1, prevP.Real, prevP.Imaginary, null, col, 0, 0, 0.02);
                col = col == 1 ? 2 : 1;
            }
            o.Mark(0, -R, -1.12, "Γ: carico (rosso), percorso lungo la linea (arancio, viola), stub (tratteggio) fino al centro", 3);
        }
    }

    // =============================================================================================
    // I · Yagi HF 2-3 elementi con la guida di taratura (I4MFA, RR 9/2025; confronto W4RNL)
    public class CalcYagiHf : SimpleCalc
    {
        public override bool Tubolare { get { return true; } }
        public CalcYagiHf()
        {
            Key = "yagihf"; Title = "Yagi HF 2-3 elementi"; Source = "I4MFA, RadioRivista 9/2025 (valori di partenza e regole di taratura); L. B. Cebik W4RNL (spaziature 0,150/0,175 λ, confronto)";
            Params.Add(new CalcParam("f", "Frequenza", 14.175, "MHz"));
            Params.Add(CalcParam.Scelta("tipo", "Elementi", 2, "2 el. con riflettore: circa 6 dBi, F/B 10 dB, 50 Ω. 2 el. con direttore: circa 6,5 dBi, F/B 15 dB, 25 Ω, banda stretta. 3 el.: radiatore = λ/2 - 4 %, riflettore +4 %, direttore -6 % (I4MFA).",
                "2 el. riflettore", "2 el. direttore", "3 el."));
            Params.Add(new CalcParam("d", "ø dei tubi", 25, "mm"));
            Params.Add(new CalcParam("h", "Altezza", 0, "m", "0 = spazio libero."));
            Params.Add(new CalcParam("sr", "Spaziatura del riflettore", 0.15, "lambda", "I4MFA: 0,165 λ nel 2 el.; W4RNL: 0,150 λ nel 3 el.") { Visibile = () => I("tipo") != 1 });
            Params.Add(new CalcParam("sd", "Spaziatura del direttore", 0.175, "lambda", "I4MFA: 0,09 λ nel 2 el. con direttore; W4RNL: 0,175 λ nel 3 el.") { Visibile = () => I("tipo") != 0 });
            Params.Add(new CalcParam("kr", "Ritocco del riflettore", 0, "%").ConSezione("2 · Taratura"));
            Params.Add(new CalcParam("ke", "Ritocco del radiatore", 0, "%"));
            Params.Add(new CalcParam("kd", "Ritocco del direttore", 0, "%"));
        }
        public override string TuneLabel { get { return "Guida di taratura (NEC)"; } }

        string chiaveGuida;
        List<string[]> guida;

        string Chiave() { string k = ""; foreach (CalcParam p in Params) k += p.Value.ToString("R", CultureInfo.InvariantCulture) + ";"; return k; }

        void Misure(out double lr, out double le, out double ld, out double xr, out double xd, double dr = 0, double de = 0, double dd = 0)
        {
            double f = P("f"), lam = C0 / f, c = 149.9 / f;
            int t = I("tipo");
            le = c * 0.96 * (1 + (P("ke") + de) / 100);
            lr = le * 1.04 * (1 + (P("kr") + dr) / 100);
            ld = le * 0.94 * (1 + (P("kd") + dd) / 100);
            xr = t != 1 ? -P("sr") * lam : double.NaN;
            xd = t != 0 ? P("sd") * lam : double.NaN;
        }

        string Mazzo(double dr, double de, double dd, double f)
        {
            double lr, le, ld, xr, xd;
            Misure(out lr, out le, out ld, out xr, out xd, dr, de, dd);
            double a = P("d") / 2000, h = P("h"), seg = C0 / f / 40;
            NecDeck dk = new NecDeck(Title);
            int te = dk.Ws(seg, 0, -le / 2, h, 0, le / 2, h, a, true, 11), ne = dk.LastN;
            if (!double.IsNaN(xr)) dk.Ws(seg, xr, -lr / 2, h, xr, lr / 2, h, a, true, 11);
            if (!double.IsNaN(xd)) dk.Ws(seg, xd, -ld / 2, h, xd, ld / 2, h, a, true, 11);
            dk.Ground(h > 0 ? 2 : 0);
            dk.Ex(te, (ne + 1) / 2);
            return dk.End(f);
        }

        /// <summary>Risonanza (X = 0) vicino a f e F/B alla frequenza f.</summary>
        static void Leggi(Func<double, string> mazzo, double f, out double fr, out double fb, out double r)
        {
            Complex z0 = C170.Zin(mazzo(f), f), z1 = C170.Zin(mazzo(f * 1.005), f * 1.005);
            double dx = (z1.Imaginary - z0.Imaginary) / (0.005 * f);
            fr = dx > 0 ? f - z0.Imaginary / dx : double.NaN;
            RunResult rr = C170.Calcola(mazzo(f), f);
            fb = rr.FrontBack; r = rr.Zin.Real;
        }

        public override double Tune(double fmhz)
        {
            double f = P("f");
            guida = new List<string[]> { new[] { "Modifica (+1 %)", "Risonanza", "F/B a " + C170.N(f, 3) + " MHz", "Effetto" } };
            double fr0, fb0, r0;
            Leggi(ff => Mazzo(0, 0, 0, ff), f, out fr0, out fb0, out r0);
            guida.Add(new[] { "nessuna (punto di partenza)", C170.N(fr0, 3) + " MHz", C170.N(fb0, 1) + " dB", "R " + C170.N(r0, 0) + " Ω" });
            int t = I("tipo");
            foreach (int k in new[] { 0, 1, 2 })
            {
                if ((k == 0 && t == 1) || (k == 2 && t == 0)) continue;
                double fr, fb, r;
                Leggi(ff => Mazzo(k == 0 ? 1 : 0, k == 1 ? 1 : 0, k == 2 ? 1 : 0, ff), f, out fr, out fb, out r);
                string el = k == 0 ? "riflettore" : k == 1 ? "radiatore" : "direttore";
                string eff = (Math.Abs(fr - fr0) > 0.002 * f ? "sposta la risonanza di " + C170.N((fr - fr0) * 1000, 0) + " kHz" : "risonanza quasi ferma") + ", " +
                             (Math.Abs(fb - fb0) > 1 ? "F/B " + (fb > fb0 ? "+" : "") + C170.N(fb - fb0, 1) + " dB" : "F/B quasi fermo");
                guida.Add(new[] { el + " +1 %", C170.N(fr, 3) + " MHz", C170.N(fb, 1) + " dB", eff });
            }
            chiaveGuida = Chiave();
            return Scale;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), lam = C0 / f;
            double lr, le, ld, xr, xd;
            Misure(out lr, out le, out ld, out xr, out xd);
            int t = I("tipo");
            o.Card("Radiatore", C170.N(le, 3) + " m", "λ/2 - 4 %" + (P("ke") != 0 ? " " + (P("ke") > 0 ? "+" : "") + C170.N(P("ke"), 1) + " %" : ""), true);
            if (t != 1) o.Card("Riflettore", C170.N(lr, 3) + " m", "radiatore + 4 %, a " + C170.N(-xr, 2) + " m");
            if (t != 0) o.Card("Direttore", C170.N(ld, 3) + " m", "radiatore - 6 %, a " + C170.N(xd, 2) + " m");
            double boom = (t != 1 ? -xr : 0) + (t != 0 ? xd : 0);
            o.Card("Boom", C170.N(boom, 2) + " m", C170.N(boom / lam, 3) + " λ");
            string att = t == 0 ? "circa 6 dBi, F/B 10 dB, 50 Ω" : t == 1 ? "circa 6,5 dBi, F/B 15 dB, 25 Ω, banda stretta" : "circa 8 dBi, F/B oltre 20 dB, 20-30 Ω";
            o.Card("Attesi (I4MFA)", att, "il NEC verifica");
            o.Formula = "Valori di partenza (I4MFA, RR 9/2025): radiatore λ/2 - 4 %, riflettore radiatore + 4 %, direttore radiatore - 6 %; 2 el. con riflettore a 0,165 λ, con direttore a 0,09 λ. " +
                "Regole di taratura: il riflettore sposta il massimo del F/B, il radiatore la risonanza, il direttore tutti e due. Confronto W4RNL: 3 el. con spaziature 0,150 e 0,175 λ.";
            if (chiaveGuida == Chiave() && guida != null) { o.Tabella = guida; o.TabellaTitolo = "Guida di taratura: effetto di ogni elemento allungato dell'1 % (NEC)"; }
            else o.Oks.Add("«Guida di taratura (NEC)» calcola l'effetto di ogni elemento allungato dell'1 %: il riflettore sposta il massimo del F/B, il radiatore la risonanza, il direttore tutti e due (I4MFA).");
            o.Nec = Mazzo(0, 0, 0, f);
            o.Plane = "xy";
            return o;
        }
    }
}
