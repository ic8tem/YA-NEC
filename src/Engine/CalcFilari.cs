// YA-NEC 1.5.6 - Calcolatori > HF > "Filari" (bozzetto approvato da IC8TEM il 21/11/2026 21:12, «esatto, compila e pubblica»):
// antenne filari alimentate in punta. EFHW multibanda, monobanda e con bobina (trasformatore 49:1 o 64:1, contrappeso di 0,05 lambda
// a filo o con la calza del coassiale fino al choke, condensatore sul primario) e filo non risonante con il 9:1 o il 4:1 (contrappeso
// tra il 5 e il 10 % del filo, regola di IC8TEM). Sei disposizioni: inclinata dal basso, inclinata dall'alto, verticale, L rovesciata,
// U rovesciata, tutto dritto (disegni di IC8TEM del 21/11/2026 21:08-21:09).
// Fonti: S. Yates AA5TB, "The End-Fed Half-Wave Antenna" (contrappeso 0,05 lambda); G. B. Gudlaugsson TF5NN, HF Antenna Impedance
// Visualizer v3 (CC BY 4.0: modello del filo come linea aperta con perdita pari alla radiazione, effetto d'estremita', verificato con
// NEC-2); VE3EED (lunghezze del filo non risonante); DG1JAN (condensatore 100 pF, perdite dei nuclei piccoli); W8JI e Owen Duffy
// (avvolgimento, perdite); L. I3RKE, "Conoscere le antenne End Fed" (calza del coassiale come parte dell'antenna); OfficinaHF
// (bobina della EFHW per gli 80 m, tensione in punta). Il trasformatore nel modello NEC e' ideale: il ROS e' riferito a 50 x rapporto.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace YaNec.Engine
{
    public class CalcFilari : SimpleCalc
    {
        public static readonly string[] Tipi = { "EFHW multibanda", "EFHW monobanda", "Filo non risonante" };   // 22/11/2026: la bobina e' una scelta dell'EFHW (IC8TEM)
        public static readonly string[] Disposizioni = { "inclinata dal basso", "inclinata dall'alto", "verticale", "L rovesciata", "U rovesciata", "tutto dritto" };
        static readonly string[] Isolanti = { "nudo", "isolato in PVC", "isolato in PE / PTFE" };
        static readonly double[] VfIsol = { 1.0, 0.97, 0.98 };
        static readonly double[] ErIsol = { 1.0, 3.5, 2.2 };
        static readonly double[] SpIsol = { 0, 0.8, 0.4 };   // spessore dell'isolante (mm)
        // bande radioamatoriali HF (centro della banda in regione 1)
        public static readonly string[] BandeNomi = { "160 m", "80 m", "60 m", "40 m", "30 m", "20 m", "17 m", "15 m", "12 m", "10 m" };
        public static readonly double[] BandeF = { 1.85, 3.65, 5.36, 7.1, 10.125, 14.175, 18.118, 21.225, 24.94, 28.5 };
        // VE3EED: lunghezze consigliate per il filo non risonante con il 9:1 (piedi)
        static readonly double[] Ve3eedFt = { 29, 35.5, 41, 58, 71, 84, 107, 119, 148 };

        public CalcFilari()
        {
            Key = "filari"; Title = "Filari"; Family = "HF";
            Source = "S. Yates AA5TB, The End-Fed Half-Wave Antenna (contrappeso 0,05 λ); G. B. Gudlaugsson TF5NN, HF Antenna Impedance Visualizer v3 (CC BY 4.0, modello del filo verificato con NEC-2); " +
                     "VE3EED (lunghezze del filo non risonante); DG1JAN, xOTA-Antenna (CC BY-SA 4.0: 100 pF, perdite dei nuclei); W8JI e O. Duffy (trasformatori); I3RKE, Conoscere le antenne End Fed (ARI Vittorio Veneto, 2020); OfficinaHF (bobina per gli 80 m, tensione in punta)";
            Func<bool> efhw = () => I("tipo") != 2, nonRis = () => I("tipo") == 2, bobina = () => I("tipo") != 2 && I("bob") == 1;
            Params.Add(CalcParam.Scelta("tipo", "Tipo", 0, "EFHW: mezz'onda alimentata in punta con il 49:1 o il 64:1, sulle armoniche (multibanda) o su una banda; la bobina di carico, se c'e', la allunga per una banda piu' bassa. Filo non risonante: lunghezza lontana dalle mezze onde, 9:1 o 4:1 e accordatore.",
                Tipi).ConSezione("1 · Antenna"));
            Params.Add(new CalcParam("f", "Banda piu' bassa (mezz'onda)", 7.1, "MHz", "La frequenza a cui il filo e' lungo mezz'onda. Con bobina: la banda del filo senza bobina (es. 7,1 MHz), la bobina aggiunge la banda sotto.") { Visibile = efhw });
            // 22/11/2026, IC8TEM («la bobina di carico puo' essere opzionale», scelta «Interruttore nell'EFHW»)
            Params.Add(Vis(CalcParam.Scelta("bob", "Bobina di carico", 0, "Senza bobina: solo il filo (es. 40-20-15-10 m). Con la bobina e una coda dopo di essa l'antenna lavora anche su una banda piu' bassa (es. +80 m, OfficinaHF); alle bande alte la bobina blocca la coda.", "no", "si'"), efhw));
            Params.Add(new CalcParam("fb", "Banda aggiunta dalla bobina", 3.65, "MHz") { Visibile = bobina });
            Params.Add(new CalcParam("coda", "Filo dopo la bobina", 2.4, "m", "OfficinaHF: 2,39 m per gli 80 m su 20 m di filo. Coda corta = bobina grande.") { Visibile = bobina });
            Params.Add(new CalcParam("fl", "Banda piu' bassa", 7.1, "MHz") { Visibile = nonRis });
            Params.Add(new CalcParam("L", "Lunghezza del filo", 16.2, "m", "VE3EED: lunghezze consigliate 8,8 - 10,8 - 12,5 - 17,7 - 21,6 - 25,6 - 32,6 - 36,3 - 45,1 m; lontano dalle mezze onde delle bande d'uso.") { Visibile = nonRis });
            Params.Add(Vis(CalcParam.Scelta("tr", "Trasformatore", 0, "49:1 per 2000-3000 Ω (spire 7:1); 64:1 per i fili alti e gli 80 m (spire 8:1).", "49:1", "64:1"), efhw));
            Params.Add(Vis(CalcParam.Scelta("trb", "Trasformatore", 0, "9:1 trifilare (filo lungo, 300-600 Ω) o 4:1 (fili piu' corti, 100-300 Ω). Serve l'accordatore.", "9:1", "4:1"), nonRis));
            Params.Add(Vis(CalcParam.Scelta("cp", "Contrappeso RF", 0, "Filo: 0,05 λ della banda piu' bassa (AA5TB), dal trasformatore verso il basso. Calza: fa da contrappeso il coassiale fino al choke, messo a 0,05 λ dal trasformatore. Nessuno: nessun filo e nessun choke; la corrente torna sulla calza del coassiale fino alla radio (49:1 e 64:1 si usano anche cosi', IC8TEM 22/11/2026).",
                "filo 0,05 λ", "calza del coassiale", "nessuno"), efhw));
            Params.Add(new CalcParam("coax", "Coassiale fino alla radio", 10, "m", "Senza contrappeso la calza del coassiale, senza choke, fa da contrappeso: la sua lunghezza cambia il ROS. Nel modello scende lungo il palo e poi va verso la radio a 0,3 m da terra.") { Visibile = () => I("tipo") != 2 && I("cp") == 2 });
            Params.Add(new CalcParam("cpp", "Contrappeso (% del filo)", 7.5, "%", "Con il 9:1 e il 4:1 il contrappeso va di solito tra il 5 e il 10 % del filo principale; l'abaco (pulsante Abaco) mostra il migliore per ogni lunghezza, a volte fino al 25-46 %.") { Visibile = nonRis });
            Params.Add(CalcParam.Scelta("iso", "Filo", 1, "L'isolante rallenta l'onda: il filo isolato e' piu' corto. Nel modello NEC l'isolante e' la scheda LD 7 (metodo di Cebik).", Isolanti));
            Params.Add(new CalcParam("d", "ø del filo", 1.0, "mm"));
            Params.Add(new CalcParam("pw", "Potenza", 100, "W"));
            Params.Add(new CalcParam("disp", "Disposizione", 0, "") { Choices = Disposizioni, Pillole = true, Sezione = "2 · Disposizione" });
            Func<int, Func<bool>> d = k => () => I("disp") == k;
            Params.Add(new CalcParam("ha", "Altezza del trasformatore", 1.5, "m") { Visibile = d(0), Sezione = "3 · Altezze" });
            Params.Add(new CalcParam("hb", "Altezza della punta", 10, "m") { Visibile = d(0) });
            Params.Add(new CalcParam("hc", "Altezza del trasformatore", 10, "m") { Visibile = d(1), Sezione = "3 · Altezze" });
            Params.Add(new CalcParam("hd", "Altezza della punta", 2, "m") { Visibile = d(1) });
            Params.Add(new CalcParam("he", "Altezza del trasformatore", 1.5, "m") { Visibile = d(2), Sezione = "3 · Altezze" });
            Params.Add(new CalcParam("hf", "Altezza del trasformatore", 1.5, "m") { Visibile = d(3), Sezione = "3 · Altezze" });
            Params.Add(new CalcParam("hg", "Altezza del tratto orizzontale", 10, "m") { Visibile = d(3) });
            Params.Add(new CalcParam("hh", "Altezza del trasformatore", 4, "m") { Visibile = d(4), Sezione = "3 · Altezze" });
            Params.Add(new CalcParam("hi", "Altezza del tratto orizzontale", 10, "m") { Visibile = d(4) });
            Params.Add(new CalcParam("hl", "Coda che scende", 3, "m") { Visibile = d(4) });
            Params.Add(new CalcParam("hm", "Altezza", 10, "m") { Visibile = d(5), Sezione = "3 · Altezze" });
            CanTune = true;
        }

        double L0Formula, uHStima;
        public override string TuneLabel { get { return I("tipo") != 2 && I("bob") == 1 ? "Accorda la bobina (NEC)" : "Accorda la lunghezza (NEC)"; } }

        /// <summary>EFHW: lunghezza (fattore di scala tra 0,8 e 1,2) con il ROS piu' basso attraverso il trasformatore (monobanda: alla banda;
        /// multibanda: media geometrica sulle bande armoniche),
        /// con il contrappeso e il terreno del modello. Non si cerca lo zero della reattanza: oltre la mezz'onda porterebbe ai 3/4 d'onda.</summary>
        public override double Tune(double fmhz)
        {
            // multibanda: media geometrica del ROS sulle bande armoniche della tabella (il compromesso tra le bande);
            // monobanda: il ROS alla banda della scheda
            bool multi = I("tipo") == 0;
            Func<double, double> ros = sc =>
            {
                Scale = sc;
                CalcOutput o = Compute();
                NecModel m = NecModel.Parse(o.Nec);
                double[] ff = multi && o.Bande != null && o.Bande.Length > 0 ? o.Bande : new[] { fmhz };
                double somma = 0;
                foreach (double fq in ff)
                {
                    SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, fq);
                    Nec2Engine e = new Nec2Engine(g);
                    e.Solve(fq, Analysis.EffectiveSources(m, g, fq), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fq));
                    somma += Math.Log(Math.Min(50, Ros(e.Sources[0].Z, double.IsNaN(o.Z0Rif) ? 2450 : o.Z0Rif)));
                }
                return Math.Exp(somma / ff.Length);
            };
            double best = 1, rb = double.MaxValue;
            if (I("tipo") != 2 && I("bob") == 1)
            {
                // 22/11/2026: con la bobina si cerca l'induttanza (fattore da 1/4 a 4 volte la stima) con il ROS piu' basso alla banda della bobina
                multi = false;
                double lb = 0, rlb = double.MaxValue;
                for (int k = 0; k <= 40; k++) { double l = Math.Log(0.25) + k * Math.Log(16) / 40, r = ros(Math.Exp(l)); if (r < rlb) { rlb = r; lb = l; } }
                double a0 = lb - Math.Log(16) / 40, b0 = lb + Math.Log(16) / 40, g0 = (Math.Sqrt(5) - 1) / 2;
                double c1 = b0 - g0 * (b0 - a0), c2 = a0 + g0 * (b0 - a0), q1 = ros(Math.Exp(c1)), q2 = ros(Math.Exp(c2));
                for (int k = 0; k < 14; k++)
                {
                    if (q1 < q2) { b0 = c2; c2 = c1; q2 = q1; c1 = b0 - g0 * (b0 - a0); q1 = ros(Math.Exp(c1)); }
                    else { a0 = c1; c1 = c2; q1 = q2; c2 = a0 + g0 * (b0 - a0); q2 = ros(Math.Exp(c2)); }
                }
                double sb = Math.Min(q1, q2) <= rlb ? Math.Exp(q1 < q2 ? c1 : c2) : Math.Exp(lb);
                Scale = sb;
                return sb;
            }
            for (double sc = 0.80; sc <= 1.2001; sc += 0.025) { double r = ros(sc); if (r < rb) { rb = r; best = sc; } }
            double lo = best - 0.025, hi = best + 0.025, gr = (Math.Sqrt(5) - 1) / 2;
            double x1 = hi - gr * (hi - lo), x2 = lo + gr * (hi - lo), r1 = ros(x1), r2 = ros(x2);
            for (int k = 0; k < 12; k++)
            {
                if (r1 < r2) { hi = x2; x2 = x1; r2 = r1; x1 = hi - gr * (hi - lo); r1 = ros(x1); }
                else { lo = x1; x1 = x2; r1 = r2; x2 = lo + gr * (hi - lo); r2 = ros(x2); }
            }
            double bs = r1 < r2 ? x1 : x2;
            if (Math.Min(r1, r2) > rb) bs = best;
            Scale = bs;
            return bs;
        }

        /// <summary>Fattore di velocita' del filo isolato con l'induttanza aggiunta di Cebik (la stessa del LD 7 nel modello NEC):
        /// dL = 2e-7 (er R/r)^(1/12) (1 - 1/er) ln(R/r) H/m contro L' = Z0 / c del filo, Z0 = 60 ln(0,22 lambda / r).</summary>
        static double VfIsolato(double fmhz, double r, double er, double R)
        {
            if (er <= 1 || R <= r) return 1;
            double dL = 2e-7 * Math.Pow(er * R / r, 1.0 / 12) * (1 - 1 / er) * Math.Log(R / r);
            double Lp = 60 * Math.Log(0.22 * (C0 / fmhz) / r) / 299792458.0;
            return Math.Sqrt(Lp / (Lp + dL));
        }

        static CalcParam Vis(CalcParam p, Func<bool> v) { p.Visibile = v; return p; }

        // ------------------------------------------------------------------------------------------------------------
        // modello TF5NN v3 (CC BY 4.0, G. B. Gudlaugsson): filo aperto contro una terra ideale, linea con perdita = radiazione
        const double EulerG = 0.5772156649015329;
        static void SiCi(double x, out double si, out double ci)
        {
            if (x <= 4)
            {
                si = 0; ci = EulerG + Math.Log(x); double term = x;
                for (int n = 1; n <= 40; n++)
                {
                    if (n > 1) term *= x / n;
                    if (n % 2 == 1) si += (((n - 1) / 2) % 2 == 0 ? 1 : -1) * term / n;
                    else ci += ((n / 2) % 2 == 0 ? 1 : -1) * term / n;
                }
                return;
            }
            double x2 = x * x;
            double f = (1 / x) * ((((x2 + 38.027264) * x2 + 265.187033) * x2 + 335.677320) * x2 + 38.102495) / ((((x2 + 40.021433) * x2 + 322.624911) * x2 + 570.236280) * x2 + 157.105423);
            double g = (1 / x2) * ((((x2 + 42.242855) * x2 + 302.757865) * x2 + 352.018498) * x2 + 21.821899) / ((((x2 + 48.196927) * x2 + 482.485984) * x2 + 1114.978885) * x2 + 449.690326);
            si = Math.PI / 2 - f * Math.Cos(x) - g * Math.Sin(x); ci = f * Math.Sin(x) - g * Math.Cos(x);
        }
        static double RradMonopolo(double th)
        {
            double kl = 2 * th, s1, c1, s2, c2;
            SiCi(kl, out s1, out c1); SiCi(2 * kl, out s2, out c2);
            return 30 * (EulerG + Math.Log(kl) - c1 + 0.5 * Math.Sin(kl) * (s2 - 2 * s1) + 0.5 * Math.Cos(kl) * (EulerG + Math.Log(kl / 2) + c2 - 2 * c1));
        }
        /// <summary>Effetto d'estremita' in mezze onde (TF5NN): 0,0464 - 0,003 ln(lambda/a), minimo 0,004.</summary>
        public static double Estremita(double fmhz, double raggio) { return Math.Max(0.004, 0.0464 - 0.0030 * Math.Log((C0 / fmhz) / raggio)); }
        /// <summary>Impedenza in punta di un filo lungo l (m) a f (MHz), raggio a (m), fattore di velocita' vf (TF5NN v3, R limitata a 5000 ohm).</summary>
        public static Complex ZFilo(double l, double fmhz, double a, double vf)
        {
            double lam = C0 / fmhz, z0 = 60 * Math.Log(0.22 * lam / a) * vf;
            double th = 2 * Math.PI * l / (lam * vf) + Estremita(fmhz, a) * Math.PI;
            double al = Atanh(Math.Min(0.99, Math.Max(1e-4, RradMonopolo(th) / z0)));
            double D = Math.Cosh(2 * al) - Math.Cos(2 * th);
            return new Complex(Math.Min(5000, z0 * Math.Sinh(2 * al) / D), -z0 * Math.Sin(2 * th) / D);
        }
        static double Atanh(double x) { return 0.5 * Math.Log((1 + x) / (1 - x)); }
        public static double Ros(Complex z, double z0)
        {
            double g = Complex.Abs((z - z0) / (z + z0));
            return g >= 0.999 ? 999 : (1 + g) / (1 - g);
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            int tipo = Math.Max(0, Math.Min(2, I("tipo"))), iso = Math.Max(0, Math.Min(2, I("iso"))), disp = Math.Max(0, Math.Min(5, I("disp")));
            bool nonRis = tipo == 2, conBob = !nonRis && I("bob") == 1;
            CanTune = !nonRis;   // con la bobina si accorda la bobina, senza la lunghezza
            double a = Math.Max(0.1, P("d")) / 2000, vf = VfIsol[iso], pw = Math.Max(0, P("pw"));
            double rapp = nonRis ? (I("trb") == 1 ? 4 : 9) : (I("tr") == 1 ? 64 : 49);
            string rappTxt = rapp.ToString("0", CultureInfo.InvariantCulture) + ":1";
            double z0rif = 50 * rapp;
            double fBassa = nonRis ? Math.Max(1, P("fl")) : Math.Max(1, P("f"));

            // ---- lunghezze
            double L, Lbob = 0, Lcoda = 0, uH = 0, fb = Math.Max(0.5, P("fb"));
            if (!nonRis)
            {
                if (iso > 0) vf = VfIsolato(fBassa, a, ErIsol[iso], a + SpIsol[iso] / 1000);
                L = (1 - Estremita(fBassa, a)) * (C0 / fBassa) * vf / 2;
                L0Formula = L;
                if (!conBob) L *= Scale;
                if (conBob)
                {
                    // bobina (OfficinaHF, 20 m di filo + bobina + coda per gli 80 m): reattanza che rende il tutto mezz'onda alla banda aggiunta,
                    // con il filo come linea di impedenza media Z0 (TF5NN): X = Z0 (cot t1 + cot t2), t1 = filo prima della bobina, t2 = coda
                    if (fb >= fBassa * 0.95) { o.Warnings.Add("La banda aggiunta dalla bobina deve stare sotto la banda del filo: bobina non calcolata."); fb = fBassa / 2; }
                    Lbob = L; Lcoda = Math.Max(0.3, P("coda"));
                    double lamB = C0 / fb, k = 2 * Math.PI / (lamB * vf), z0l = 60 * Math.Log(0.22 * lamB / a) * vf;
                    double t1 = k * Lbob, t2 = k * Lcoda + Estremita(fb, a) * Math.PI;
                    double x = z0l * (1 / Math.Tan(t1) + 1 / Math.Tan(t2));
                    uH = Math.Max(0, x / (2 * Math.PI * fb));
                    uHStima = uH;
                    uH *= Scale;   // «Accorda la bobina (NEC)»
                    L = Lbob + Lcoda;
                }
            }
            else L = Math.Max(1, P("L"));
            double lamBassa = C0 / (conBob ? fb : fBassa);
            bool nessuno = !nonRis && I("cp") == 2;   // 22/11/2026: EFHW senza contrappeso
            double cp = nonRis ? L * Math.Max(1, Math.Min(60, P("cpp"))) / 100 : (nessuno ? Math.Max(0.5, P("coax")) : 0.05 * lamBassa);
            bool calza = !nonRis && I("cp") == 1;

            // ---- bande in tabella
            List<int> bande = new List<int>();
            for (int i = 0; i < BandeF.Length; i++)
            {
                double fbnd = BandeF[i];
                if (tipo == 1) { if (Math.Abs(fbnd - fBassa) / fBassa < 0.08 || (conBob && Math.Abs(fbnd - fb) / fb < 0.06)) bande.Add(i); continue; }
                if (nonRis) { if (fbnd >= fBassa * 0.95) bande.Add(i); continue; }
                // EFHW: solo le bande vicine a un multiplo della mezz'onda (entro il 6 %), anche quella della bobina
                bool armonica = false;
                for (int n = 1; n <= 16; n++) if (Math.Abs(fbnd - n * fBassa) / fbnd < 0.06) armonica = true;
                if (conBob && Math.Abs(fbnd - fb) / fb < 0.06) armonica = true;
                if (armonica) bande.Add(i);
            }
            if (bande.Count == 0) bande.Add(Array.IndexOf(BandeF, 7.1));

            // ---- riquadri
            if (conBob)
            {
                o.Card("Filo fino alla bobina", Calcolatori.N(Lbob, 2) + " m", "mezz'onda a " + Calcolatori.N(fBassa, 3) + " MHz", true);
                o.Card("Bobina", Calcolatori.N(uH, 0) + " µH", Math.Abs(Scale - 1) > 1e-6 ? "accordata con il NEC (stima " + Calcolatori.N(uHStima, 0) + " µH)" : Calcolatori.N(fb, 2) + " MHz: stima da verificare con il NEC", true);
                o.Card("Coda dopo la bobina", Calcolatori.N(Lcoda, 2) + " m", Calcolatori.N(L, 2) + " m di filo in tutto");
            }
            else if (!nonRis) o.Card("Filo radiante", Calcolatori.N(L, 2) + " m", Math.Abs(Scale - 1) > 1e-6 ? "accordato con il NEC (formula " + Calcolatori.N(L0Formula, 2) + " m)" : "mezz'onda a " + Calcolatori.N(fBassa, 3) + " MHz, VF " + Calcolatori.N(vf, 3) + ", effetto d'estremita'", true);
            else o.Card("Filo", Calcolatori.N(L, 2) + " m", "non risonante, con il " + rappTxt + " e l'accordatore", true);
            if (calza) o.Card("Choke sul coassiale", Calcolatori.N(cp, 1) + " m", "dal trasformatore: la calza fino a li' e' il contrappeso (0,05 λ)");
            else if (nessuno) o.Card("Contrappeso RF", "calza " + Calcolatori.N(cp, 1) + " m", "coassiale fino alla radio, senza choke");
            else o.Card("Contrappeso RF", Calcolatori.N(cp, 2) + " m", nonRis ? Calcolatori.N(P("cpp"), 1) + " % del filo (5-10 %)" : "0,05 λ (AA5TB)");
            if (!nonRis)
            {
                bool alte = false; foreach (int i in bande) if (BandeF[i] > 20) alte = true;
                double cpf = rapp == 64 ? 150 : 100;
                o.Card("Condensatore sul primario", alte ? Calcolatori.N(cpf, 0) + " pF" : "non serve", alte ? "almeno 1 kV, migliora le bande alte" : "solo bande sotto i 20 MHz");
            }
            Complex zb = conBob ? ZFilo(Lbob, fBassa, a, vf) : ZFilo(L, fBassa, a, vf);
            double v = Math.Sqrt(pw * Complex.Abs(zb));
            o.Card("Tensione in punta", Calcolatori.N(v, 0) + " V", "efficaci a " + Calcolatori.N(pw, 0) + " W (" + Calcolatori.N(v * Math.Sqrt(2), 0) + " V di picco)");
            if (nessuno) o.Card("Choke sul coassiale", "nessuno", "la calza fa da contrappeso");
            else if (!calza) o.Card("Choke sul coassiale", "al trasformatore", "la corrente torna dal contrappeso");
            int nucleo = pw <= 15 ? 1 : (pw <= 120 ? 2 : 3);   // FT-140-43, FT-240-43, 2 x FT-240-43 (indici della scheda Balun: 1, 2, 3)
            string[] nomiNucleo = { "", "FT-140-43", "FT-240-43", "2 x FT-240-43" };
            o.Card("Nucleo del trasformatore", nomiNucleo[nucleo], nonRis ? (rapp == 9 ? "trifilare" : "bifilare") : (rapp == 64 ? "rapporto spire 8:1, spire in Balun / Un-Un" : "rapporto spire 7:1, spire in Balun / Un-Un"));

            // ---- tabella per banda (modello TF5NN, filo su terra ideale)
            {
                double lw = conBob ? Lbob : L;   // con la bobina: alle bande del filo la bobina blocca la coda (la banda della bobina la da' il NEC)
                foreach (int i in bande)
                {
                    if (conBob && Math.Abs(BandeF[i] - fb) / fb < 0.06) continue;
                    Complex z = ZFilo(lw, BandeF[i], a, vf);
                    double r = Ros(z / rapp, 50);
                    string esito = r <= 2 ? "✓ " : (r <= 3 ? "accordatore interno, " : "accordatore esterno, ");
                    o.Card(BandeNomi[i] + ", ROS con il " + rappTxt, Calcolatori.N(Math.Min(r, 99), 2), esito + "Z " + Calcolatori.N(z.Real, 0) + (z.Imaginary >= 0 ? " +j" : " -j") + Calcolatori.N(Math.Abs(z.Imaginary), 0) + " Ω (TF5NN)", r <= 2);
                }
            }
            if (nonRis)
            {
                // VE3EED: mezze onde da evitare
                List<string> vicine = new List<string>();
                foreach (int i in bande)
                {
                    double mezza = C0 / BandeF[i] * vf / 2;
                    for (int n = 1; n <= 12; n++)
                        if (Math.Abs(L - n * mezza) < 0.04 * n * mezza) { vicine.Add(BandeNomi[i]); break; }
                }
                if (vicine.Count > 0) o.Warnings.Add("Il filo e' vicino a un multiplo di mezz'onda su " + string.Join(", ", vicine) + ": li' l'impedenza e' altissima e il " + rappTxt + " non basta. Cambia lunghezza (VE3EED).");
                else o.Oks.Add("lunghezza lontana dalle mezze onde delle bande scelte (VE3EED)");
                string cons = "";
                foreach (double ft in Ve3eedFt) cons += (cons.Length > 0 ? " · " : "") + Calcolatori.N(ft * 0.3048, 1);
                o.Card("Lunghezze consigliate (VE3EED)", cons, "metri, per il 9:1");
            }

            // ---- grafico: |Z| in punta contro la lunghezza, prima e seconda banda
            {
                double lw = conBob ? Lbob : L;
                double l0 = Math.Max(1, lw * 0.4), l1 = lw * 1.6;
                int np = 120;
                o.Chart = new CalcChart { Title = "Impedenza in punta al variare della lunghezza (modello TF5NN)", XLabel = "m", YLabel = "Ω" };
                o.Chart.X = Calcolatori.Lin(l0, l1, np); o.Chart.Y = new double[np];
                List<int> bc = new List<int>();
                foreach (int i in bande) if (!conBob || Math.Abs(BandeF[i] - fb) / fb >= 0.06) bc.Add(i);
                if (bc.Count == 0) bc.Add(bande[0]);
                int b1 = bc[0], b2 = bc.Count > 1 ? bc[1] : -1;
                for (int i = 0; i < np; i++) o.Chart.Y[i] = Complex.Abs(ZFilo(o.Chart.X[i], BandeF[b1], a, vf));
                o.Chart.Nome1 = BandeNomi[b1];
                if (b2 >= 0)
                {
                    o.Chart.Y2 = new double[np];
                    for (int i = 0; i < np; i++) o.Chart.Y2[i] = Complex.Abs(ZFilo(o.Chart.X[i], BandeF[b2], a, vf));
                    o.Chart.Nome2 = BandeNomi[b2];
                }
                o.Chart.MarkX = lw; o.Chart.MarkText = Calcolatori.N(lw, 2) + " m; " + rappTxt + " = " + Calcolatori.N(z0rif, 0) + " Ω";
                o.Chart.YMin = 0;
            }

            // ---- modello NEC
            double fmax = fBassa; foreach (int i in bande) fmax = Math.Max(fmax, BandeF[i]);
            fmax = Math.Min(30, Math.Max(fmax, fBassa));
            double seg = Math.Min(C0 / fmax / 30, 0.5);
            double h1;
            List<double[]> pts = Percorso(disp, L, out h1, o);
            NecDeck dk = new NecDeck(Title + " " + Tipi[tipo] + " " + Calcolatori.N(fBassa, 3) + " MHz, " + Disposizioni[disp]);
            double gap = 0.1;
            // filo di alimentazione (il trasformatore ideale), poi il radiatore, poi il contrappeso verso il basso e verso la radio
            int tFeed = dk.W(1, 0, 0, h1 - gap, 0, 0, h1, a);
            List<int> tagRad = new List<int>();
            double fatto = 0, pBob = Lbob;
            int tagBob = 0, segBob = 0;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                double[] p = pts[i], q = pts[i + 1];
                double lt = Dist(p, q);
                if (lt < 1e-6) continue;
                if (conBob && pBob > fatto + 1e-6 && pBob < fatto + lt - 1e-6)
                {
                    // spezza il tratto alla bobina: segmento corto (0,1 m) con il carico
                    double u0 = (pBob - fatto - 0.05) / lt, u1 = (pBob - fatto + 0.05) / lt;
                    double[] m0 = Lerp(p, q, u0), m1 = Lerp(p, q, u1);
                    tagRad.Add(dk.Ws(seg, p[0], 0, p[1], m0[0], 0, m0[1], a));
                    tagBob = dk.W(1, m0[0], 0, m0[1], m1[0], 0, m1[1], a); segBob = 1;
                    tagRad.Add(dk.Ws(seg, m1[0], 0, m1[1], q[0], 0, q[1], a));
                }
                else tagRad.Add(dk.Ws(seg, p[0], 0, p[1], q[0], 0, q[1], a));
                fatto += lt;
            }
            // contrappeso: giu' lungo il palo fino a 0,3 m dal suolo, poi verso la radio (-x)
            double giu = Math.Min(cp, Math.Max(0, h1 - gap - 0.3));
            double zc = h1 - gap - giu;
            List<int> tagCp = new List<int>();
            if (giu > 0.05) tagCp.Add(dk.Ws(seg, 0, 0, h1 - gap, 0, 0, zc, a));
            if (cp - giu > 0.05) tagCp.Add(dk.Ws(seg, 0, 0, zc, -(cp - giu), 0, zc, a));
            if (iso > 0)
            {
                double R = a + SpIsol[iso] / 1000;
                foreach (int t in tagRad) dk.Raw("LD 7 " + t + " 0 0 " + ErIsol[iso].ToString(CultureInfo.InvariantCulture) + " " + R.ToString("0.######", CultureInfo.InvariantCulture));
                foreach (int t in tagCp) dk.Raw("LD 7 " + t + " 0 0 " + ErIsol[iso].ToString(CultureInfo.InvariantCulture) + " " + R.ToString("0.######", CultureInfo.InvariantCulture));
            }
            if (tagBob > 0) dk.Ld(0, tagBob, segBob, segBob, 0, uH * 1e-6, 0);   // bobina ideale (serie R-L-C: solo L)
            dk.Ground(2);
            dk.Ex(tFeed, 1);
            string nec = dk.End(conBob ? fb : fBassa);
            o.Nec = "CM YA-NEC Z0=" + z0rif.ToString("0", CultureInfo.InvariantCulture) + "\n" + nec;
            o.Z0Rif = z0rif;
            List<double> fb2 = new List<double>();
            if (conBob) fb2.Add(fb);
            foreach (int i in bande) if (!conBob || Math.Abs(BandeF[i] - fb) / fb > 0.06) fb2.Add(BandeF[i]);
            o.Bande = fb2.ToArray();
            o.Plane = "xz";

            // ---- annotazioni del disegno
            o.Mark(8, -0.4, h1 - 0.5, rappTxt, 0, 0.4, h1 + 0.3);
            if (calza) o.Mark(0, -cp, Math.Max(0.4, zc) + 0.6, "choke", 2);
            o.Mark(0, -Math.Max(1, cp * 0.6), zc - 0.9, calza || nessuno ? "calza " + Calcolatori.N(cp, 1) + " m" : "contrappeso " + Calcolatori.N(cp, 2) + " m", 1);
            double[] ult = pts[pts.Count - 1];
            o.Mark(0, ult[0], ult[1] + 0.6, "punta", 0);

            // ---- formule e avvisi
            string fz = "Formule: filo λ/2 = (1 - e) VF λ/2, e = effetto d'estremita' in mezze onde = 0,0464 - 0,003 ln(λ/a) (TF5NN, con le armoniche che cadono un po' corte); " +
                "contrappeso 0,05 λ della banda piu' bassa (AA5TB) o tra il 5 e il 10 % del filo con il 9:1 e il 4:1; tensione in punta V = √(P |Z|); impedenza per banda dal modello TF5NN v3 (filo come linea aperta con perdita uguale alla radiazione, su terra ideale; " +
                "rispetto al NEC-2 errore del 4 % su terra ideale e dell'11 % con il contrappeso). Nel modello NEC il trasformatore e' ideale: il ROS del calcolo in tempo reale e' riferito a 50 x " + rapp.ToString("0", CultureInfo.InvariantCulture) + " = " + Calcolatori.N(z0rif, 0) + " Ω.";
            if (conBob) fz += " Bobina: X = Z0 (cot θ1 + cot θ2), con θ1 il filo prima della bobina e θ2 la coda (piu' l'effetto d'estremita') alla banda aggiunta, Z0 = 60 ln(0,22 λ/a); OfficinaHF: 20,15 m + 110-120 µH + 2,39 m per gli 80 m.";
            o.Formula = fz + " Fonti: " + Source + ".";
            if (nessuno) o.Warnings.Add("Senza contrappeso la corrente torna sulla calza del coassiale fino alla radio: il coassiale irradia, porta RF in stazione e cambia il ROS con la sua lunghezza (I3RKE).");
            if (calza) o.Warnings.Add("Con la calza come contrappeso il coassiale fino al choke irradia: tienilo lontano dalla casa e non mettere il choke sul trasformatore (I3RKE).");
            if (!nonRis && pw > 50 && nucleo == 2) o.Warnings.Add("Con un solo FT-240-43 il funzionamento continuo (FT8, RTTY) va tenuto tra 30 e 50 W; 100 W in SSB.");
            if (pw > 15 && pw <= 120 && nucleo == 2 && !nonRis) o.Oks.Add("FT-140-43 e FT-114-43 sconsigliati sopra la QRP: perdite oltre il 20 % misurate (DG1JAN)");
            if (v > 400) o.Warnings.Add("In punta ci sono " + Calcolatori.N(v, 0) + " V efficaci: isolatore buono e filo lontano da persone e rami.");
            if (tipo == 0) o.Oks.Add("le bande armoniche cadono un po' piu' in basso: la tabella mostra il ROS che resta (TF5NN)");
            if (conBob) { if (Math.Abs(Scale - 1) < 1e-6) o.Warnings.Add("La bobina e' una stima (linea di trasmissione): il calcolo in tempo reale dice dove risuona davvero; «Accorda la bobina (NEC)» cerca l'induttanza giusta con il contrappeso e il terreno."); }
            return o;
        }

        static double Dist(double[] p, double[] q) { return Math.Sqrt((q[0] - p[0]) * (q[0] - p[0]) + (q[1] - p[1]) * (q[1] - p[1])); }
        static double[] Lerp(double[] p, double[] q, double u) { return new[] { p[0] + (q[0] - p[0]) * u, p[1] + (q[1] - p[1]) * u }; }

        /// <summary>Punti (x, z) del filo dal trasformatore alla punta, secondo la disposizione.</summary>
        List<double[]> Percorso(int disp, double L, out double h1, CalcOutput o)
        {
            List<double[]> p = new List<double[]>();
            double hMin = 0.3;
            switch (disp)
            {
                case 1:   // inclinata dall'alto
                case 0:   // inclinata dal basso
                    {
                        h1 = Math.Max(hMin + 0.2, P(disp == 0 ? "ha" : "hc"));
                        double h2 = Math.Max(hMin, P(disp == 0 ? "hb" : "hd")), dz = h2 - h1;
                        if (Math.Abs(dz) >= L) { dz = Math.Sign(dz) * L; o.Warnings.Add("Il filo e' piu' corto del dislivello: la punta resta a " + Calcolatori.N(h1 + dz, 1) + " m."); }
                        double dx = Math.Sqrt(Math.Max(0, L * L - dz * dz));
                        p.Add(new[] { 0.0, h1 }); p.Add(new[] { dx, Math.Max(hMin, h1 + dz) });
                        o.Card("Distanza a terra", Calcolatori.N(dx, 1) + " m", "tra il trasformatore e la punta");
                        break;
                    }
                case 2:
                    h1 = Math.Max(hMin + 0.2, P("he"));
                    p.Add(new[] { 0.0, h1 }); p.Add(new[] { 0.0, h1 + L });
                    o.Card("Punta", Calcolatori.N(h1 + L, 1) + " m", "altezza");
                    break;
                case 3:   // L rovesciata
                    {
                        h1 = Math.Max(hMin + 0.2, P("hf"));
                        double ho = Math.Max(h1, P("hg")), sale = Math.Min(L, ho - h1);
                        p.Add(new[] { 0.0, h1 }); p.Add(new[] { 0.0, h1 + sale });
                        if (L - sale > 0.01) p.Add(new[] { L - sale, h1 + sale });
                        o.Card("Tratti", Calcolatori.N(sale, 1) + " + " + Calcolatori.N(L - sale, 1) + " m", "sale, orizzontale");
                        break;
                    }
                case 4:   // U rovesciata
                    {
                        h1 = Math.Max(hMin + 0.2, P("hh"));
                        double ho = Math.Max(h1, P("hi")), sale = Math.Min(L, ho - h1);
                        double coda = Math.Max(0, Math.Min(P("hl"), ho - hMin));
                        double oriz = L - sale - coda;
                        if (oriz < 0.5) { coda = Math.Max(0, L - sale - 0.5); oriz = L - sale - coda; o.Warnings.Add("Salita e coda sono quasi tutto il filo: la coda e' stata accorciata a " + Calcolatori.N(coda, 1) + " m."); }
                        p.Add(new[] { 0.0, h1 }); p.Add(new[] { 0.0, h1 + sale });
                        if (oriz > 0.01) p.Add(new[] { oriz, h1 + sale });
                        if (coda > 0.01) p.Add(new[] { oriz, h1 + sale - coda });
                        o.Card("Tratti", Calcolatori.N(sale, 1) + " + " + Calcolatori.N(oriz, 1) + " + " + Calcolatori.N(coda, 1) + " m", "sale, orizzontale, coda");
                        break;
                    }
                default:  // tutto dritto
                    h1 = Math.Max(hMin + 0.2, P("hm"));
                    p.Add(new[] { 0.0, h1 }); p.Add(new[] { L, h1 });
                    break;
            }
            return p;
        }
    }
}
