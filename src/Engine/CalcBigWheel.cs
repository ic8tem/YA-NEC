// YA-NEC 1.0.17 - Calcolatori > Forme semplici > Big Wheel (bozzetto approvato da IC8TEM il 17/11/2026 18:08, con i tre dipoli).
// Big Wheel: tre petali di circa un'onda, ognuno alimentato ai due capi; i bracci vicino al mozzo stanno su due livelli (sotto = calza,
// sopra = centrale) e il braccio sopra di un petalo e quello sotto del petalo vicino fanno da linea di lambda/4 (lo "sfasamento" dei
// conduttori). Forma del petalo dalla dima di F1BKM: triangolo arrotondato, bracci a 100 gradi, curve di raggio rc, lato esterno
// diritto. Lo stub in parallelo al mozzo porta i circa 10 ohm a 50 ohm. Tre dipoli (W4RNL e WA1FXT, QST 3/2008): dipoli diritti a
// triangolo o ad arco su un cerchio, alimentati con correnti uguali (cavi a 50 ohm uguali), collegati in serie al mozzo (circa 150 ohm)
// e portati a 50 ohm con un tratto di lambda/4 (93 o 75 ohm). Cebik (antenneX 145): la serie si modella con correnti uguali.
// Fonti: W1IJD e W1FVY (QST 9/1961); A. Griffault F1BKM (Radio-REF 764, 2004); L.B. Cebik W4RNL e B. Cerreto WA1FXT (QST 3/2008);
// Cebik, antenneX 41 (sorgenti multiple e verso dei fili), 145 (linee in serie); Rothammel DM2ABK (stack a 5/8 lambda capovolto).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public class CalcBigWheel : SimpleCalc
    {
        double fPrec = double.NaN; int tipoPrec = -1;
        public double[] SweepF, SweepSwr, SweepG; public double Circolarita = double.NaN, Gorizz = double.NaN;
        public Complex Zmozzo = Complex.Zero;
        string firmaSweep;

        public CalcBigWheel()
        {
            Key = "bigwheel"; Title = "Big Wheel";
            Source = "W1IJD e W1FVY (QST 9/1961); A. Griffault F1BKM (Radio-REF 764, 2004); L.B. Cebik W4RNL e B. Cerreto WA1FXT (QST 3/2008); Cebik, antenneX 41 e 145; K. Rothammel DM2ABK";
            Params.Add(CalcParam.Scelta("tipo", "Tipo", 0, "Big Wheel: tre petali di un'onda alimentati ai due capi, con lo stub al mozzo. Tre dipoli (W4RNL, WA1FXT): dipoli quasi a 50 ohm, a triangolo o su un cerchio, collegati in serie al mozzo e adattati con un lambda/4.",
                "Big Wheel (W1IJD-W1FVY, F1BKM)", "Tre dipoli a triangolo (W4RNL)", "Tre dipoli a cerchio (WA1FXT)"));
            Params.Add(new CalcParam("f", "Frequenza", 145, "MHz", "Cambiandola, le misure in mm si riscalano."));
            Params.Add(new CalcParam("tubo", "Diametro del tubo", 10, "mm", "F1BKM: alluminio da 10 mm; QST 1961: 3/8\" (9,5 mm); Cebik: 1/2\" (12,7 mm)."));
            Func<bool> bw = () => I("tipo") == 0, td = () => I("tipo") > 0;
            Add(new CalcParam("lp", "Lunghezza sviluppata del petalo", 2050, "mm", "Dal mozzo al mozzo lungo il tubo, circa un'onda (F1BKM 2050 mm a 145 MHz; QST 80\" a 146 MHz)."), bw);
            Add(new CalcParam("ap", "Apertura del petalo", 100, "°", "Angolo tra i due bracci di un petalo (dima F1BKM: 100 gradi, quindi 20 gradi tra i bracci di petali vicini)."), bw);
            Add(new CalcParam("rc", "Raggio delle curve (asse)", 145, "mm", "Raggio delle due curve del petalo sull'asse del tubo (dima F1BKM: 140 mm interno)."), bw);
            Add(new CalcParam("dz", "Distanza tra i due livelli", 30, "mm", "Lo \"sfasamento\" dei conduttori: distanza verticale tra i bracci sotto (calza) e sopra (centrale). Stimata dalla pila di piastre di F1BKM: le fonti non la scrivono."), bw);
            Add(new CalcParam("r0", "Raggio del mozzo", 20, "mm", "Dove i bracci si attaccano ai triangoli di collegamento (F1BKM: triangoli da 52 mm di lato)."), bw);
            Add(new CalcParam("ls", "Stub: lunghezza sviluppata", 127, "mm", "Striscia piegata a U tra i due livelli, in parallelo al mozzo (QST 1961: 5\"; F1BKM 127 mm in spazio libero, 152 mm su un'auto o in stack)."), bw);
            Add(new CalcParam("lw", "Stub: larghezza", 20, "mm", "Striscia di alluminio (F1BKM 20 x 2 mm): nel NEC un filo di raggio larghezza/4."), bw);
            Add(new CalcParam("rd", "Raggio al punto di alimentazione", 391, "mm", "Distanza dal centro al punto di alimentazione di ogni dipolo (Cebik: 15,4\" a triangolo, 15,5\" a cerchio, a 146 MHz)."), td);
            Add(new CalcParam("ld", "Lunghezza del dipolo", 871, "mm", "Da punta a punta (a cerchio: lungo l'arco). Cebik: 34,3\" a triangolo, 31,4\" a cerchio, a 146 MHz con tubo da 1/2\"."), td);
            Add(new CalcParam("zq", "Tratto di lambda/4: impedenza", 93, "Ω", "Dal mozzo (circa 150 ohm in serie) a 50 ohm: RG-62 da 93 ohm (Cebik), oppure 75 ohm se l'impedenza e' sotto 150."), td);
            Params.Add(CalcParam.Scelta("mont", "Montaggio", 0, "Spazio libero, oppure a un'altezza dal suolo medio (Sommerfeld).", "spazio libero", "a un'altezza dal suolo"));
            CalcParam h = new CalcParam("h", "Altezza dal suolo", 6, "m"); h.Visibile = () => I("mont") == 1; Params.Add(h);
            Params.Add(CalcParam.Scelta("stack", "Stack", 0, "Due antenne una sopra l'altra, in fase: Big Wheel a 5/8 lambda (QST e F1BKM: una capovolta con mezz'onda di cavo in piu', nel modello in fase diretta), tre dipoli a 0,875 lambda (Cebik).",
                "1 antenna", "2 sovrapposte"));
            NotaFissa = "Ogni petalo e' un'onda intera alimentata ai due capi: i bracci vicino al mozzo sono linee di λ/4 a due livelli (blu sotto = calza, rosso sopra = centrale). Il braccio sopra di un petalo e quello sotto del petalo vicino corrono a 20 gradi: e' quella coppia a fare da linea. Lo stub in parallelo porta i circa 10 Ω a 50 Ω.";
        }
        void Add(CalcParam p, Func<bool> vis) { p.Visibile = vis; Params.Add(p); }

        public override string TuneLabel { get { return "Porta in adattamento (NEC)"; } }

        string Firma()
        {
            StringBuilder s = new StringBuilder();
            foreach (CalcParam p in Params) s.Append(p.Key).Append('=').Append(p.Value.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            return s.ToString();
        }

        /// <summary>Riscala le misure quando cambia la frequenza e mette le misure di Cebik quando si passa ai tre dipoli.</summary>
        void Aggiorna()
        {
            double f = Math.Max(1, P("f"));
            int t = I("tipo");
            if (tipoPrec >= 0 && t != tipoPrec && t > 0)
            {
                double k = 146.0 / f;
                if (t == 1) { Par("rd").Value = Math.Round(391.2 * k, 1); Par("ld").Value = Math.Round(871.2 * k, 1); }
                else { Par("rd").Value = Math.Round(393.7 * k, 1); Par("ld").Value = Math.Round(797.6 * k, 1); }
            }
            if (!double.IsNaN(fPrec) && Math.Abs(f - fPrec) > 1e-9)
            {
                double k = fPrec / f;
                foreach (string key in new[] { "lp", "rc", "ls", "rd", "ld" }) Par(key).Value = Math.Round(Par(key).Value * k, 1);
            }
            fPrec = f; tipoPrec = t;
        }

        // ---------------------------------------------------------------------------------------------
        // geometria del petalo (mm, piano locale: mozzo nell'origine, asse del petalo lungo +y)
        public static List<double[]> Petalo(double L, double apDeg, double rc, double r0, out double H)
        {
            double a = apDeg / 2 * Math.PI / 180;
            Func<double, List<double[]>> forma = hh =>
            {
                List<double[]> p = new List<double[]>();
                double wc = ((hh - rc) * Math.Sin(a) - rc) / Math.Cos(a);
                double tl = wc * Math.Sin(a) + (hh - rc) * Math.Cos(a);
                p.Add(new[] { -Math.Sin(a) * r0, Math.Cos(a) * r0 });
                p.Add(new[] { -Math.Sin(a) * tl, Math.Cos(a) * tl });
                double cy = hh - rc;
                double a0 = Math.Atan2(p[1][1] - cy, p[1][0] + wc); if (a0 < 0) a0 += 2 * Math.PI;
                int nk = 10;
                for (int k = 1; k <= nk; k++) { double ang = a0 - (a0 - Math.PI / 2) * k / nk; p.Add(new[] { -wc + rc * Math.Cos(ang), cy + rc * Math.Sin(ang) }); }
                double a1 = Math.PI - a0;
                for (int k = 0; k <= nk; k++) { double ang = Math.PI / 2 - (Math.PI / 2 - a1) * k / nk; p.Add(new[] { wc + rc * Math.Cos(ang), cy + rc * Math.Sin(ang) }); }
                p.Add(new[] { Math.Sin(a) * r0, Math.Cos(a) * r0 });
                return p;
            };
            Func<List<double[]>, double> lung = p => { double s = 0; for (int i = 1; i < p.Count; i++) s += Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2)); return s; };
            double lo = rc * (1 + 1 / Math.Sin(a)) + 1, hi = L;
            for (int it = 0; it < 60; it++) { double m = (lo + hi) / 2; if (lung(forma(m)) < L) lo = m; else hi = m; }
            H = (lo + hi) / 2;
            return forma(H);
        }

        static double[] Ruota(double x, double y, double th) { return new[] { x * Math.Cos(th) - y * Math.Sin(th), x * Math.Sin(th) + y * Math.Cos(th) }; }

        public override CalcOutput Compute()
        {
            Aggiorna();
            CalcOutput o = new CalcOutput();
            double f = Math.Max(1, P("f")), lam = C0 / f;
            int tipo = I("tipo"), mont = I("mont"); bool stack = I("stack") == 1;
            double a = Math.Max(0.5, P("tubo")) / 2000, seg = lam / 40;
            double z0 = mont == 1 ? Math.Max(0.5, P("h")) : 0;
            double passoStack = tipo == 0 ? 0.625 * lam : 0.875 * lam;
            NecDeck dk = new NecDeck(Title + " " + Calcolatori.N(f, 1) + " MHz", tipo > 0);
            List<int[]> sorg = new List<int[]>();
            double diam = 0;
            for (int bay = 0; bay < (stack ? 2 : 1); bay++)
            {
                double zb = z0 + bay * passoStack;
                if (tipo == 0) diam = BigWheel(o, dk, zb, a, seg, sorg, bay == 0);
                else diam = TreDipoli(o, dk, zb, a, seg, tipo == 2, sorg, bay == 0);
            }
            dk.Ground(mont == 1 ? 2 : 0);
            foreach (int[] s in sorg) dk.Ex(s[0], s[1]);
            o.Nec = dk.End(f);
            o.Plane = "xy";

            // ---- riquadri
            if (tipo == 0)
            {
                double H;
                Petalo(P("lp"), P("ap"), P("rc"), P("r0"), out H);
                o.Card("Diametro della ruota", Calcolatori.N(diam, 0) + " mm", Calcolatori.N(diam / 1000 / lam, 3) + " λ", true);
                o.Card("Mozzo - lato esterno", Calcolatori.N(H, 0) + " mm", "sull'asse del tubo (F1BKM: 535 mm)");
                o.Card("Petalo", Calcolatori.N(P("lp"), 0) + " mm", Calcolatori.N(P("lp") / 1000 / lam, 3) + " λ sviluppati");
                o.Card("Livelli", Calcolatori.N(P("dz"), 0) + " mm", "sotto calza (blu), sopra centrale (rosso)");
                o.Card("Stub", Calcolatori.N(P("ls"), 0) + " mm", "striscia da " + Calcolatori.N(P("lw"), 0) + " mm a U tra i livelli");
            }
            else
            {
                o.Card("Dipolo", Calcolatori.N(P("ld"), 0) + " mm", Calcolatori.N(P("ld") / 1000 / lam, 3) + " λ " + (tipo == 2 ? "lungo l'arco" : "diritto"), true);
                o.Card("Raggio al punto di alimentazione", Calcolatori.N(P("rd"), 0) + " mm", "ingombro " + Calcolatori.N(diam, 0) + " mm");
                double lq = lam / 4 * 1000;
                o.Card("Tratto di λ/4", Calcolatori.N(lq, 0) + " mm x VF", Calcolatori.N(P("zq"), 0) + " Ω (RG-62: VF 0,82-0,84 → " + Calcolatori.N(lq * 0.83, 0) + " mm)");
            }
            if (stack) o.Card("Stack", Calcolatori.N(passoStack * 1000, 0) + " mm", tipo == 0 ? "5/8 λ (QST, F1BKM)" : "0,875 λ (Cebik)");
            if (SweepF != null && firmaSweep == Firma())
            {
                int c = SweepF.Length / 2;
                o.Card(tipo == 0 ? "Impedenza al mozzo (NEC)" : "Impedenza in serie al mozzo", Calcolatori.N(Zmozzo.Real, 1) + (Zmozzo.Imaginary < 0 ? " -j" : " +j") + Calcolatori.N(Math.Abs(Zmozzo.Imaginary), 1) + " Ω",
                    tipo == 0 ? "con lo stub" : "dopo il λ/4: ROS " + Calcolatori.N(SweepSwr[c], 2), true);
                o.Card("Guadagno all'orizzonte (NEC)", Calcolatori.N(Gorizz, 2) + " dBi", "circolarita' +-" + Calcolatori.N(Circolarita / 2, 2) + " dB", true);
                double smax = 0; foreach (double v in SweepSwr) smax = Math.Max(smax, v);
                o.Card("ROS a 50 Ω in banda", "max " + Calcolatori.N(smax, 2), Calcolatori.N(SweepF[0], 1) + "-" + Calcolatori.N(SweepF[SweepF.Length - 1], 1) + " MHz");
                o.Chart = new CalcChart { Title = "ROS a 50 Ω (dopo \"Porta in adattamento\")", XLabel = "MHz", YLabel = "ROS", X = SweepF, Y = SweepSwr, MarkX = f, MarkText = Calcolatori.N(f, 1), YMin = 1 };
            }

            // ---- passi di costruzione (dettaglio del mozzo)
            if (tipo == 0)
            {
                o.Passi.Add("Piega i tre petali uguali sulla dima (triangolo arrotondato, bracci a " + Calcolatori.N(P("ap"), 0) + " gradi, curve di " + Calcolatori.N(P("rc"), 0) + " mm sull'asse).");
                o.Passi.Add("Ogni petalo ha un braccio sul livello di sotto e uno su quello di sopra: il cambio di livello sta a meta' del lato esterno (il manicotto di F1BKM).");
                o.Passi.Add("Monta i bracci di sotto sul triangolo di massa (calza) e quelli di sopra sul triangolo del centrale, a " + Calcolatori.N(P("dz"), 0) + " mm l'uno dall'altro: tutti i petali nello stesso verso.");
                o.Passi.Add("Il braccio di sopra di un petalo e quello di sotto del petalo vicino stanno a 20 gradi: insieme fanno la linea di λ/4 che porta l'alta impedenza del petalo al mozzo.");
                o.Passi.Add("Stub da " + Calcolatori.N(P("ls"), 0) + " mm piegato a U tra i due triangoli, in parallelo al connettore; si regola la lunghezza per il ROS (152 mm su un'auto o in stack, F1BKM).");
            }
            else
            {
                o.Passi.Add("Tre dipoli uguali a 120 gradi, con il punto di alimentazione a " + Calcolatori.N(P("rd"), 0) + " mm dal centro; il centrale del cavo dallo stesso lato su tutti e tre (Cebik: un dipolo rovesciato rende l'antenna direttiva).");
                o.Passi.Add("Tre cavi a 50 Ω della stessa lunghezza (qualsiasi) fino al mozzo, collegati in serie: circa 150 Ω.");
                o.Passi.Add("Dal mozzo un tratto di λ/4 di cavo da " + Calcolatori.N(P("zq"), 0) + " Ω (lunghezza per il fattore di velocita') verso la discesa a 50 Ω.");
            }

            // ---- avvisi
            if (tipo == 0)
            {
                o.Warnings.Add("La Big Wheel e' critica: lo stesso Cebik (QST 3/2008) avverte che il NEC calcola male le linee dove la corrente e' bassa e che piccole differenze di costruzione spostano l'impedenza. Lo stub si regola sempre sull'antenna vera.");
                if (P("dz") < 4 * P("tubo") / 2) o.Warnings.Add("Livelli molto vicini rispetto al tubo: la linea dei bracci ha un'impedenza molto bassa e il modello e' al limite del NEC-2 (fili paralleli a meno di qualche raggio).");
            }
            o.Oks.Add("Polarizzazione orizzontale e diagramma omnidirezionale: il guadagno all'orizzonte dipende dall'altezza e dallo stack, non dalla forma.");

            o.Formula = "Big Wheel: petalo a triangolo arrotondato (bracci a " + Calcolatori.N(P("ap"), 0) + " gradi dal mozzo, curve di raggio rc, lato esterno diritto); l'altezza si trova per bisezione dalla lunghezza sviluppata. " +
                "Bracci vicino al mozzo su due livelli distanti dz; sorgente verticale tra i due nodi del mozzo, stub a U in parallelo (raggio = larghezza/4). Tre dipoli: correnti uguali sui tre punti di alimentazione (cavi uguali), " +
                "Z in serie = somma delle tre impedenze, dopo il λ/4: Z = Zq^2 / Zserie (Cebik, antenneX 145).\n" +
                "Fonti a confronto. W1IJD e W1FVY (QST 9/1961): petali da 80\" (2032 mm, 0,99 λ a 146 MHz), tubo da 3/8\", ruota di 4 piedi; circa 30 Ω per petalo, 10 Ω in parallelo, stub da 5\" (127 mm) per 50 Ω, ROS 1,2 in banda, diagramma entro +-2 dB; stack a 5/8 λ con una baia capovolta e 1 λ di RG-11, +4 dB sul dipolo. " +
                "F1BKM (Radio-REF 764, 2004): petali da 2050 mm di tubo da 10 mm, dima a 100 gradi con curve di 140 mm interne e 535 mm dal mozzo al lato esterno; stub 127 mm (152 su un'auto o in stack), striscia da 20 x 2 mm; stack a 1300 mm con le linee da 75 Ω di λ/4 e 3λ/4 e una baia capovolta (metodo DM2ABK). " +
                "Somiglianze: stesso petalo di circa un'onda e stesso stub da 127 mm. Differenze: F1BKM dichiara circa 5 dB sui dipoli incrociati, il QST non da' un guadagno assoluto. " +
                "Cebik e Cerreto (QST 3/2008): la Big Wheel e' un anello unico con tre punti ad alta impedenza, critico da riprodurre; propongono tre dipoli a triangolo (34,3\" su 15,4\" di raggio a 146 MHz, tubo da 1/2\") o a cerchio (31,4\" su 15,5\"), 7,2 dBi a 20 piedi, circolarita' entro 0,3 dB, serie al mozzo circa 150 Ω e λ/4 di RG-62; stack a 0,875 λ. " +
                "OfficinaHF e F... (Maignan, dalla versione F1BKM): ruota di circa 1,2 m, teme vento e ghiaccio, nessuna misura nuova.";
            return o;
        }

        // ---- Big Wheel: restituisce il diametro (mm)
        double BigWheel(CalcOutput o, NecDeck dk, double zb, double a, double seg, List<int[]> sorg, bool disegna)
        {
            double H;
            List<double[]> pet = Petalo(P("lp"), P("ap"), P("rc"), Math.Max(2, P("r0")), out H);
            double dz = Math.Max(2, P("dz")) / 1000, zs = zb + dz / 2, zg = zb - dz / 2;
            int half = pet.Count / 2;   // il cambio di livello sta a meta' del lato esterno
            double rmax = 0;
            for (int k = 0; k < 3; k++)
            {
                double th = (90 + 120 * k) * Math.PI / 180 - Math.PI / 2;
                List<double[]> q = new List<double[]>();
                foreach (double[] p in pet) { double[] r = Ruota(p[0] / 1000, p[1] / 1000, th); q.Add(r); rmax = Math.Max(rmax, Math.Sqrt(r[0] * r[0] + r[1] * r[1])); }
                // braccio sinistro (verso antiorario) sopra, destro sotto
                double[] ini = q[0], fin = q[q.Count - 1];
                dk.W(1, 0, 0, zs, ini[0], ini[1], zs, a);
                for (int i = 1; i < q.Count; i++)
                {
                    double za = i - 1 < half ? zs : zg, zc = i < half ? zs : zg;
                    if (i - 1 == half - 1 && i == half) { za = zs; zc = zg; }
                    dk.Ws(seg, q[i - 1][0], q[i - 1][1], za, q[i][0], q[i][1], zc, a, false, 1);
                    if (disegna) o.Mark(2, q[i - 1][0], q[i - 1][1], null, zc > zb ? 5 : 0, q[i][0], q[i][1]);
                }
                dk.W(1, fin[0], fin[1], zg, 0, 0, zg, a);
                if (disegna) { o.Mark(2, 0, 0, null, 5, ini[0], ini[1]); o.Mark(2, fin[0], fin[1], null, 0, 0, 0); }
            }
            // sorgente verticale tra i due nodi del mozzo e stub a U in parallelo (verso l'asse del petalo 0)
            int ts = dk.W(1, 0, 0, zg, 0, 0, zs, a);
            sorg.Add(new[] { ts, 1 });
            double ls = Math.Max(dz * 1.2, P("ls") / 1000), rs = Math.Max(0.5, P("lw")) / 4000, l1 = (ls - dz) / 2;
            dk.Ws(seg, 0, 0, zs, 0, l1, zs, rs, false, 1);
            dk.W(1, 0, l1, zs, 0, l1, zg, rs);
            dk.Ws(seg, 0, l1, zg, 0, 0, zg, rs, false, 1);
            if (disegna) { o.Mark(2, 0, 0, "stub", 2, 0, l1); o.Mark(0, -0.12, -0.08, "rosso: sopra (centrale)", 5); o.Mark(0, -0.12, -0.13, "blu: sotto (calza)", 0); }
            return 2 * rmax * 1000;
        }

        // ---- tre dipoli: restituisce l'ingombro (mm)
        double TreDipoli(CalcOutput o, NecDeck dk, double zb, double a, double seg, bool cerchio, List<int[]> sorg, bool disegna)
        {
            double R = Math.Max(10, P("rd")) / 1000, L = Math.Max(20, P("ld")) / 1000, rmax = 0;
            for (int k = 0; k < 3; k++)
            {
                double th = (90 + 120 * k) * Math.PI / 180;
                if (!cerchio)
                {
                    // dipolo diritto perpendicolare al raggio, verso antiorario (stesso verso del filo per i tre: Cebik antenneX 41)
                    double cx = R * Math.Cos(th), cy = R * Math.Sin(th), ux = -Math.Sin(th), uy = Math.Cos(th);
                    double x1 = cx - ux * L / 2, y1 = cy - uy * L / 2, x2 = cx + ux * L / 2, y2 = cy + uy * L / 2;
                    int t = dk.Ws(seg, x1, y1, zb, x2, y2, zb, a, true, 3);
                    sorg.Add(new[] { t, (dk.LastN + 1) / 2 });
                    rmax = Math.Max(rmax, Math.Sqrt(x1 * x1 + y1 * y1));
                    if (disegna) o.Mark(2, x1, y1, null, 0, x2, y2);
                }
                else
                {
                    // arco di raggio R centrato sul centro, lungo L, alimentato a meta' (spezzate di 2 lati per meta')
                    double span = L / R, nl = 8;
                    List<double[]> pt = new List<double[]>();
                    for (int i = 0; i <= nl; i++) { double ang = th - span / 2 + span * i / nl; pt.Add(new[] { R * Math.Cos(ang), R * Math.Sin(ang) }); }
                    int tSorg = 0, sSorg = 0;
                    for (int i = 1; i < pt.Count; i++)
                    {
                        int t = dk.Ws(seg, pt[i - 1][0], pt[i - 1][1], zb, pt[i][0], pt[i][1], zb, a, false, 1);
                        if (i == nl / 2) { tSorg = t; sSorg = dk.LastN; }
                        if (disegna) o.Mark(2, pt[i - 1][0], pt[i - 1][1], null, 0, pt[i][0], pt[i][1]);
                    }
                    sorg.Add(new[] { tSorg, sSorg });
                    rmax = R;
                }
            }
            return 2 * rmax * 1000;
        }

        // ---------------------------------------------------------------------------------------------
        // adattamento e banda
        RunResult Calcola(double fmhz, out Complex zm)
        {
            CalcOutput o = Compute();
            NecModel m = NecModel.Parse(o.Nec);
            m.Z0 = 50;
            SegmentGeometry g = m.BuildGeometry();
            RunResult r = Analysis.Run(m, g, fmhz, 5.0);
            zm = ZMozzo(r, fmhz);
            return r;
        }

        /// <summary>Big Wheel: Z della sorgente; tre dipoli: serie delle tre (cavi a 50 ohm uguali, rapporto 1) e poi il lambda/4.</summary>
        Complex ZMozzo(RunResult r, double fmhz)
        {
            if (I("tipo") == 0 || r.Sources.Count < 3) return r.Zin;
            Complex s = Complex.Zero;
            int n = I("stack") == 1 ? 3 : r.Sources.Count;
            for (int i = 0; i < 3 && i < r.Sources.Count; i++) s += r.Sources[i].Z;
            return s;
        }

        public static double Ros(Complex z, double z0) { double g = ((z - z0) / (z + z0)).Magnitude; return (1 + g) / Math.Max(1e-9, 1 - g); }

        Complex ZIngresso(Complex zm)
        {
            if (I("tipo") == 0) return zm;
            double zq = Math.Max(1, P("zq"));
            return zq * zq / zm;
        }

        double Gamma(double fmhz)
        {
            Complex zm;
            CalcOutput o = Compute();
            NecModel m = NecModel.Parse(o.Nec);
            SegmentGeometry g = m.BuildGeometry();
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(fmhz, Analysis.EffectiveSources(m, g, fmhz), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fmhz));
            if (I("tipo") == 0) zm = e.Sources[0].Z;
            else { zm = Complex.Zero; for (int i = 0; i < 3 && i < e.Sources.Count; i++) zm += e.Sources[i].Z; }
            Complex zi = ZIngresso(zm);
            return ((zi - 50) / (zi + 50)).Magnitude;
        }

        double Sezione(CalcParam p, double lo, double hi, double f, int iter)
        {
            double gr = (Math.Sqrt(5) - 1) / 2, x1 = hi - gr * (hi - lo), x2 = lo + gr * (hi - lo);
            p.Value = x1; double f1 = Gamma(f); p.Value = x2; double f2 = Gamma(f);
            for (int it = 0; it < iter; it++)
            {
                if (f1 < f2) { hi = x2; x2 = x1; f2 = f1; x1 = hi - gr * (hi - lo); p.Value = x1; f1 = Gamma(f); }
                else { lo = x1; x1 = x2; f1 = f2; x2 = lo + gr * (hi - lo); p.Value = x2; f2 = Gamma(f); }
            }
            p.Value = Math.Round(f1 < f2 ? x1 : x2, 1);
            return Math.Min(f1, f2);
        }

        public override double Tune(double fmhz)
        {
            Compute();
            double f = Math.Max(1, P("f"));
            if (I("tipo") == 0)
            {
                // petalo (reattanza giusta per lo stub) e stub (annulla la suscettanza), alternati
                CalcParam lp = Par("lp"), ls = Par("ls");
                for (int giro = 0; giro < 3; giro++)
                {
                    double l0 = lp.Value, s0 = ls.Value;
                    Sezione(ls, Math.Max(P("dz") * 1.3, s0 * 0.5), s0 * 1.6, f, 9);
                    Sezione(lp, l0 * 0.96, l0 * 1.04, f, 9);
                }
            }
            else
            {
                // dipoli: lunghezza per il ROS dopo il lambda/4 (la reattanza in serie va a zero)
                CalcParam ld = Par("ld");
                double l0 = ld.Value;
                Sezione(ld, l0 * 0.93, l0 * 1.07, f, 12);
            }
            int n = 9;
            SweepF = new double[n]; SweepSwr = new double[n]; SweepG = new double[n];
            for (int i = 0; i < n; i++)
            {
                double fi = f * (0.98 + 0.04 * i / (n - 1));
                Complex zm; RunResult r = Calcola(fi, out zm);
                SweepF[i] = fi; SweepSwr[i] = Ros(ZIngresso(zm), 50); SweepG[i] = r.GmaxDbi;
                if (i == n / 2)
                {
                    Zmozzo = zm;
                    // guadagno all'orizzonte (spazio libero) o al massimo (con il suolo), circolarita' sull'azimut
                    double th = I("mont") == 1 ? r.ThetaMax : 90, gmin = 1e9, gmax = -1e9;
                    for (int ph = 0; ph < 360; ph += 3) { double gg = r.Engine.Gain(th, ph).GainTotal; gmin = Math.Min(gmin, gg); gmax = Math.Max(gmax, gg); }
                    Gorizz = gmax; Circolarita = gmax - gmin;
                }
            }
            firmaSweep = Firma();
            return Scale;
        }
    }
}
