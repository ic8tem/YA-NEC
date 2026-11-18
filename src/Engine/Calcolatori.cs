// YA-NEC - calcolatori delle forme semplici e strumenti (bozzetto approvato alla lettera il 12/11/2026).
// Ogni calcolatore: dati (con pillole), formule con le fonti, riquadri dei risultati, disegno, grafico di un parametro,
// avvisi, e il modello NEC con le misure calcolate che il motore verifica.
// Fonti delle formule (citate anche nei Crediti):
//  - ARRL Antenna Book: dipolo 468/f piedi, verticale 234/f, loop 1005/f, quad 1030/975, tabella dei radiali (Brown, Lewis, Epstein),
//    rapporto del dipolo ripiegato (1 + ln(2s/d1)/ln(2s/d2))^2, reti L, Pi e T;
//  - W3DZZ (trappole 7,05 MHz, 60 pF / 8,2 uH), G5RV (L. Varney, 102 piedi + 34 piedi di linea aperta), T2FD (Countryman, W3HH),
//    OCF/Windom (alimentazione a 1/3, balun 4:1);
//  - W5QJR e AA5TB (loop magnetica: Rr = 31171 (A/lambda^2)^2, Rs = sqrt(pi f mu0 / sigma), L = mu0 (D/2)(ln(8D/d) - 2));
//  - DK7ZB (HB9CV: riflettore 150/f, radiatore 139/f, spaziatura lambda/8, 4,15 dBd); L.B. Cebik W4RNL / MOXGEN (Moxon);
//  - hamuniverse.com "Hentenna" (lati lambda/2, larghezza lambda/6, alimentazione a lambda/10 dal basso, 2,5-3 dBd);
//  - Kraus/Balanis (resistenza di radiazione: 20 pi^2 (l/lambda)^2, 40 pi^2 (h/lambda)^2 = 395 (h/lambda)^2, 31171 (N A/lambda^2)^2);
//  - Amidon (AL dei toroidi: iron powder in uH/100 spire, ferrite in mH/1000 spire, +-25 %); linea monopolo Z0 = 60 (ln(2h/a) - 1).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    /// <summary>Piccolo costruttore di modelli NEC per i calcolatori (le etichette dei fili crescono da sole).</summary>
    public class NecDeck
    {
        readonly StringBuilder sb = new StringBuilder();
        readonly StringBuilder tail = new StringBuilder();
        public int Tag;
        static string F(double v) { return v != 0 && Math.Abs(v) < 1e-3 ? v.ToString("G9", CultureInfo.InvariantCulture) : v.ToString("0.######", CultureInfo.InvariantCulture); }   // valori piccoli (C in farad, L in henry) in notazione esponenziale
        public NecDeck(string titolo, bool correnti = false)
        {
            sb.AppendLine("CM " + titolo + " (calcolatore YA-NEC)");
            if (correnti) sb.AppendLine("CM YA-NEC CORRENTI=1");
            sb.AppendLine("CE");
        }
        /// <summary>Filo; restituisce l'etichetta. n = numero di segmenti (minimo 1).</summary>
        public int W(int n, double x1, double y1, double z1, double x2, double y2, double z2, double r)
        {
            Tag++;
            sb.AppendLine("GW " + Tag + " " + Math.Max(1, n) + " " + F(x1) + " " + F(y1) + " " + F(z1) + " " + F(x2) + " " + F(y2) + " " + F(z2) + " " + F(r));
            return Tag;
        }
        /// <summary>Filo con segmenti di lunghezza circa seg (dispari se dispari = true).</summary>
        public int Ws(double seg, double x1, double y1, double z1, double x2, double y2, double z2, double r, bool dispari = false, int min = 1)
        {
            double l = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1) + (z2 - z1) * (z2 - z1));
            int n = Math.Max(min, (int)Math.Ceiling(l / seg));
            if (dispari && n % 2 == 0) n++;
            LastN = n;
            return W(n, x1, y1, z1, x2, y2, z2, r);
        }
        public int LastN = 1;
        /// <summary>Terreno: 0 spazio libero, 1 perfetto, 2 reale medio (Sommerfeld). ge1 = fili che toccano il suolo.</summary>
        public void Ground(int tipo, bool ge1 = false)
        {
            sb.AppendLine(tipo == 0 ? "GE 0" : (ge1 ? "GE 1" : "GE 0"));
            if (tipo == 1) sb.AppendLine("GN 1");
            else if (tipo == 2) sb.AppendLine("GN 2 0 0 0 13 0.005");
        }
        public void Ex(int tag, int seg, double vr = 1, double vi = 0) { tail.AppendLine("EX 0 " + tag + " " + seg + " 0 " + F(vr) + " " + F(vi)); }
        public void Ld(int tipo, int tag, int s1, int s2, double a, double b, double c) { tail.AppendLine("LD " + tipo + " " + tag + " " + s1 + " " + s2 + " " + F(a) + " " + F(b) + " " + F(c)); }
        public void Tl(int t1, int s1, int t2, int s2, double z0, double len) { tail.AppendLine("TL " + t1 + " " + s1 + " " + t2 + " " + s2 + " " + F(z0) + " " + F(len) + " 0 0 0 0"); }
        public void Raw(string s) { tail.AppendLine(s); }
        public string End(double f)
        {
            return sb.ToString() + tail.ToString() + "FR 0 1 0 0 " + F(f) + " 0\nEN\n";
        }
    }

    public static class Calcolatori
    {
        public static List<SimpleCalc> Tutti()
        {
            List<SimpleCalc> l = new List<SimpleCalc>();
            Action<SimpleCalc, string, string> add = (c, g, v) => { c.Group = g; c.Variant = v; l.Add(c); };
            add(new CalcDipolo(), "dipoli", "mezz'onda");
            add(new CalcVInvertita(), "dipoli", "V invertita");
            add(new CalcRipiegato(), "dipoli", "ripiegato");
            add(new CalcTrappolato(), "dipoli", "trappolato");
            add(new CalcOcf(), "dipoli", "OCF/Windom");
            add(new CalcG5rv(), "dipoli", "G5RV");
            add(new CalcT2fd(), "dipoli", "T2FD");
            add(new CalcQuartoTerra(), "verticali", "lambda/4 e radiali");
            add(new CalcCinqueOttavi(), "verticali", "5/8");
            add(new CalcGroundPlane(), "verticali", "ground plane");
            add(new CalcCaricata(), "verticali", "caricata con bobina");
            add(new CalcJpole(), "verticali", "J-pole");
            add(new CalcLoopMagnetica(), "loopmag", null);
            add(new CalcHb9cv(), "hb9cv", null);
            add(new CalcQuad(), "quad", "1-2-3 elementi");
            add(new CalcQuadMulti(), "quad", "multibanda");
            add(new CalcEh(), "eh", null);
            add(new CalcMoxon(), "moxon", null);
            add(new CalcHexbeam(), "hexbeam", null);   // 0.4.30 (IC8TEM 14/11/2026 21:54)
            add(new CalcDelta(), "delta", null);
            add(new CalcLoopRett(), "looprett", null);
            add(new CalcSkywire(), "skywire", null);   // 1.0.17 (bozzetto 3 approvato da IC8TEM 17/11/2026 18:28)
            add(new CalcHen(), "hen", null);
            add(new CalcDiscone(), "discone", null);
            add(new CalcFourSquare(), "4sq", null);
            add(new CalcEggbeater(), "eggbeater", null);
            add(new CalcQfh(), "qfh", null);
            add(new CalcYagiDl6wu(), "yagidl6wu", null);   // 0.5.0 (IC8TEM 15/11/2026 15:02)
            add(new CalcQuagi(), "quagi", null);           // 0.5.0
            add(new CalcLoopYagi(), "loopyagi", null);     // 0.5.0 (IC8TEM 15/11/2026 15:42)
            add(new CalcStackDl6wu(), "stackdl6wu", null);
            add(new CalcBiquad(), "biquad", null);
            add(new CalcBigWheel(), "bigwheel", null);   // 1.0.17 (bozzetto approvato da IC8TEM 17/11/2026 18:08)   // 1.0.16 (bozzetto approvato da IC8TEM 17/11/2026 17:25)
            add(new CalcRidimensiona(), "ridim", null);
            add(new CalcTetto(), "tetto", null);   // 0.5.0 (IC8TEM 15/11/2026 15:32, proposta B)
            add(new CalcReteLC(), "retelc", null);
            add(new CalcRr(), "rrad", null);
            add(new CalcCavi(), "cavi", null);
            add(new CalcToroidiT(), "toroidiT", null);
            add(new CalcToroidiFT(), "toroidiFT", null);
            add(new CalcBalun(), "balun", null);
            return l;
        }

        // formato con la virgola (cultura corrente)
        internal static string N(double v, int d) { return double.IsNaN(v) || double.IsInfinity(v) ? "-" : v.ToString("F" + d, CultureInfo.CurrentCulture); }
        internal static string Ohm(double v)
        {
            double a = Math.Abs(v);
            if (a < 1) return N(v * 1000, a < 0.001 ? 2 : 1) + " mΩ";
            if (a >= 1000) return N(v / 1000, 2) + " kΩ";
            return N(v, a < 10 ? 2 : 1) + " Ω";
        }
        internal static string Henry(double h)
        {
            double a = Math.Abs(h);
            if (a < 1e-6) return N(h * 1e9, 0) + " nH";
            if (a < 1e-3) return N(h * 1e6, a < 1e-5 ? 2 : 1) + " µH";
            return N(h * 1e3, 2) + " mH";
        }
        internal static string Farad(double c)
        {
            double a = Math.Abs(c);
            if (a < 1e-9) return N(c * 1e12, a < 1e-11 ? 1 : 0) + " pF";
            if (a < 1e-6) return N(c * 1e9, 2) + " nF";
            return N(c * 1e6, 2) + " µF";
        }
        internal static string Cx(Complex z, int d = 1) { return N(z.Real, d) + (z.Imaginary >= 0 ? " + j" : " - j") + N(Math.Abs(z.Imaginary), d) + " Ω"; }
        internal static double Z0Monopolo(double h, double a) { return 60 * (Math.Log(2 * h / a) - 1); }
        internal static double[] Lin(double a, double b, int n) { double[] x = new double[n]; for (int i = 0; i < n; i++) x[i] = a + (b - a) * i / (n - 1); return x; }
    }

    // =============================================================================================
    // DIPOLI
    // =============================================================================================
    public class CalcRipiegato : SimpleCalc
    {
        public CalcRipiegato()
        {
            Key = "ripiegato"; Title = "Dipolo ripiegato"; Source = "ARRL Antenna Book (rapporto di trasformazione del dipolo ripiegato)";
            Params.Add(new CalcParam("f", "Frequenza", 14.2, "MHz"));
            Params.Add(new CalcParam("d1", "Diametro del conduttore alimentato", 2, "mm"));
            Params.Add(new CalcParam("d2", "Diametro dell'altro conduttore", 2, "mm"));
            Params.Add(new CalcParam("s", "Distanza tra i conduttori (centro-centro)", 100, "mm"));
            Params.Add(new CalcParam("h", "Quota (0 = spazio libero)", 10, "m"));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), d1 = P("d1") / 1000, d2 = P("d2") / 1000, s = Math.Max(P("s") / 1000, (d1 + d2)), h = P("h"), lam = C0 / f;
            double L = 142.6 / f * Scale * 0.985;
            double k = Math.Pow(1 + Math.Log(2 * s / d1) / Math.Log(2 * s / d2), 2);
            o.Card("Lunghezza", Calcolatori.N(L, 3) + " m", "ogni braccio " + Calcolatori.N(L / 2, 3) + " m", true);
            o.Card("Rapporto di salita", Calcolatori.N(k, 2) + " : 1", "conduttori uguali: 4 : 1");
            o.Card("Impedenza attesa", Calcolatori.N(73 * k, 0) + " Ω", "73 Ω x rapporto (spazio libero)");
            o.Card("Linea consigliata", 73 * k > 200 ? "300 Ω o balun 4:1" : "balun 1:1 / 2:1", "");
            o.Formula = "Formule: L = 142,6/f x 0,985 (il ripiegato e' un poco piu' corto); rapporto = (1 + ln(2s/d1) / ln(2s/d2))^2; Z = 73 x rapporto. Fonte: ARRL Antenna Book.";
            Hint("s", "= " + Calcolatori.N(s / lam * 1000, 1) + " millesimi di lambda");
            if (s > lam / 50) o.Warnings.Add("Distanza oltre lambda/50: il ripiegato si comporta come una linea non piu' trascurabile.");
            else o.Oks.Add("distanza piccola rispetto a lambda: formula del rapporto valida");
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 50, z = h;
            int t1 = d.Ws(seg, 0, -L / 2, z, 0, L / 2, z, d1 / 2, true, 21);
            int n1 = d.LastN;
            d.W(n1, 0, -L / 2, z + s, 0, L / 2, z + s, d2 / 2);
            d.W(1, 0, -L / 2, z, 0, -L / 2, z + s, d1 / 2);
            d.W(1, 0, L / 2, z, 0, L / 2, z + s, d1 / 2);
            d.Ground(h > 0 ? 2 : 0);
            d.Ex(t1, (n1 + 1) / 2);
            o.Nec = d.End(f);
            o.Plane = "yz";
            o.Mark(7, -L / 2, z - s - L * 0.04, "L = " + Calcolatori.N(L, 2) + " m", 3, L / 2, z - s - L * 0.04);
            o.Mark(0, L / 2 + L * 0.02, z + s, "s = " + Calcolatori.N(s * 1000, 0) + " mm", 3);
            o.Chart = new CalcChart { Title = "Rapporto di salita al variare del diametro dell'altro conduttore", XLabel = "mm", YLabel = "rapporto" };
            o.Chart.X = Calcolatori.Lin(0.5, 20, 40);
            o.Chart.Y = new double[40];
            for (int i = 0; i < 40; i++) o.Chart.Y[i] = Math.Pow(1 + Math.Log(2 * s / d1) / Math.Log(2 * s / (o.Chart.X[i] / 1000)), 2);
            o.Chart.MarkX = d2 * 1000; o.Chart.MarkText = Calcolatori.N(k, 2);
            return o;
        }
    }

    public class CalcTrappolato : SimpleCalc
    {
        bool Singola { get { return I("modo") == 1; } }
        public CalcTrappolato()
        {
            Key = "trappolato"; Title = "Dipolo trappolato (tipo W3DZZ)"; Source = "W3DZZ (trappole a 7,05 MHz, 60 pF / 8,2 uH); modello a linea del monopolo caricato; C. L. Buchanan W3DZZ, \"The Multimatch Antenna System\", QST marzo 1955 pp. 22-23; " +
                "bobina di carico: ARRL Antenna Book, antenne corte caricate (stesso modello a linea)";
            // 1.0.11 (IC8TEM 16/11/2026 23:37, bozzetto approvato alle 23:48): modo a singola banda con la bobina di carico, la sua posizione
            // in % del braccio e la lunghezza del braccio scelte da chi progetta
            Params.Add(CalcParam.Scelta("modo", "Modo di calcolo", 0, "Due bande: trappole LC parallele (W3DZZ). Singola banda: dipolo accorciato con una bobina per braccio.",
                "due bande (trappole W3DZZ)", "singola banda (bobina di carico)").ConSezione("1 · Modo di calcolo"));
            Func<bool> due = () => !Singola, una = () => Singola;
            Params.Add(new CalcParam("f1", "Banda alta (risonanza delle trappole)", 7.05, "MHz") { Sezione = "2 · Trappole", Visibile = due });
            Params.Add(new CalcParam("f2", "Banda bassa", 3.75, "MHz") { Visibile = due });
            Params.Add(new CalcParam("c", "Condensatore della trappola", 60, "pF") { Visibile = due });
            Params.Add(new CalcParam("q", "Q della trappola", 150, "") { Visibile = due });
            Params.Add(new CalcParam("fs", "Frequenza", 7.1, "MHz") { Sezione = "2 · Braccio e bobina", Visibile = una });
            Params.Add(new CalcParam("A", "Lunghezza del braccio (centro → punta)", 6.5, "m", "Meta' del dipolo, dal centro alla punta. Deve essere piu' corta di un quarto d'onda: la bobina allunga elettricamente il braccio.") { Visibile = una });
            Params.Add(new CalcParam("pb", "Posizione della bobina (% del braccio dal centro)", 50, "%", "0 % = al centro (bobina piu' piccola, meno efficienza), verso 100 % = verso la punta (bobina sempre piu' grande).") { Visibile = una });
            Params.Add(new CalcParam("qb", "Q della bobina", 200, "", "Perdita della bobina: R = X / Q. Bobine in aria ben fatte 200-400.") { Visibile = una });
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm") { Sezione = "3 · Filo e quota" });
            Params.Add(new CalcParam("h", "Quota (0 = spazio libero)", 15, "m"));
        }
        public override CalcOutput Compute() { return Singola ? ComputeSingola() : ComputeDue(); }

        /// <summary>Reattanza della bobina di carico a una posizione del braccio (modello a linea): X = Z0 (cot(beta b) - tan(beta a)),
        /// a = tratto interno, b = tratto esterno. Z0 del tratto esterno 60 (ln(b/r) - 1): con questa scelta la stima resta entro circa il 15 %
        /// dal NEC tra il 20 e il 70 % del braccio (prove del 16/11/2026); il valore esatto lo da' "Porta in risonanza (NEC)".</summary>
        public static double ReattanzaCarico(double fMHz, double braccio, double frazione, double raggio)
        {
            double beta = 2 * Math.PI * fMHz / C0, ai = braccio * frazione, bo = braccio - ai;
            double z0 = 60 * (Math.Log(Math.Max(bo, 10 * raggio) / raggio) - 1);
            return z0 * (1 / Math.Tan(beta * bo) - Math.Tan(beta * ai));
        }

        CalcOutput ComputeSingola()
        {
            CalcOutput o = new CalcOutput();
            double f = P("fs"), A = Math.Max(0.1, P("A")), pc = Math.Max(1, Math.Min(95, P("pb"))) / 100, Q = Math.Max(10, P("qb")), a = P("d") / 2000, h = P("h");
            double w = 2 * Math.PI * f * 1e6, lam = C0 / f;
            double X = ReattanzaCarico(f, A, pc, a);
            bool serve = X > 0;
            double L = Math.Max(1e-9, X / w) * Scale;
            double inner = A * pc, outer = A - inner, piena = 142.6 / f;
            o.Card("Bobina di carico", serve ? Calcolatori.Henry(L) : "non serve", serve ? "+j" + Calcolatori.N(w * L, 0) + " Ω a " + Calcolatori.N(f, 3) + " MHz" : "braccio gia' lungo un quarto d'onda", true);
            o.Card("Tratto interno", Calcolatori.N(inner, 2) + " m", "centro → bobina (" + Calcolatori.N(pc * 100, 0) + " %)");
            o.Card("Tratto esterno", Calcolatori.N(outer, 2) + " m", "bobina → punta");
            o.Card("Lunghezza totale", Calcolatori.N(2 * A, 2) + " m", "contro " + Calcolatori.N(piena, 1) + " m a grandezza piena (" + (2 * A < piena ? "−" : "+") + Calcolatori.N(Math.Abs(1 - 2 * A / piena) * 100, 0) + " %)");
            o.Formula = "Formule (modello a linea del braccio caricato, lo stesso della W3DZZ risolto per la bobina): X = Z0 (cot(beta b) - tan(beta a)), " +
                "a = tratto interno = p x braccio, b = tratto esterno, beta = 2 pi / lambda, Z0 = 60 (ln(b/r) - 1); L = X / (2 pi f); perdita R = X / Q. " +
                "La stima di Z0 e' tarata sul NEC (entro circa il 15 % tra il 20 e il 70 % del braccio): \"Porta in risonanza (NEC)\" ritocca la bobina, non le lunghezze. " +
                "Fonti: ARRL Antenna Book (antenne corte caricate); Buchanan W3DZZ, QST 1955 (stesso modello a linea, li' risolto per il tratto esterno).";
            if (!serve) o.Warnings.Add("Il braccio (" + Calcolatori.N(A, 2) + " m) e' gia' lungo almeno un quarto d'onda (" + Calcolatori.N(lam / 4, 2) + " m): la bobina non serve, accorcia il braccio.");
            if (pc > 0.8) o.Warnings.Add("Bobina oltre l'80 % del braccio: serve una bobina molto grande e la stima a linea perde precisione; usa \"Porta in risonanza (NEC)\".");
            if (serve && 2 * A < piena * 0.4) o.Warnings.Add("Dipolo piu' corto del 40 % della grandezza piena: banda stretta e resistenza di radiazione bassa, le perdite della bobina contano molto.");
            NecDeck d = new NecDeck("Dipolo caricato " + Calcolatori.N(f, 3) + " MHz, bobina al " + Calcolatori.N(pc * 100, 0) + " %");
            double seg = lam / 60;
            int tc = d.W(1, 0, -seg / 2, h, 0, seg / 2, h, a);
            int ti1 = d.Ws(seg, 0, seg / 2, h, 0, Math.Max(inner, seg), h, a, false, 3);
            int ni = d.LastN;
            d.Ws(seg, 0, Math.Max(inner, seg), h, 0, A, h, a, false, 3);
            int ti2 = d.Ws(seg, 0, -seg / 2, h, 0, -Math.Max(inner, seg), h, a, false, 3);
            d.Ws(seg, 0, -Math.Max(inner, seg), h, 0, -A, h, a, false, 3);
            d.Ground(h > 0 ? 2 : 0);
            if (serve)
            {
                d.Ld(0, ti1, ni, ni, w * L / Q, L, 0);
                d.Ld(0, ti2, ni, ni, w * L / Q, L, 0);
            }
            d.Ex(tc, 1);
            o.Nec = d.End(f);
            o.Plane = "yz";
            double q = A * 0.12;
            o.Mark(6, inner - A * 0.05, h, null, 2, inner + A * 0.05, h);
            o.Mark(6, -inner - A * 0.05, h, null, 2, -inner + A * 0.05, h);
            o.Mark(0, inner - A * 0.05, h + q * 0.9, "bobina", 2);
            o.Mark(0, -inner - A * 0.05, h + q * 0.9, "bobina", 2);
            o.Mark(7, 0, h - q, Calcolatori.N(inner, 2) + " m", 3, inner, h - q);
            o.Mark(7, inner, h - q, Calcolatori.N(outer, 2) + " m", 3, A, h - q);
            o.Mark(7, 0, h - 2.2 * q, "braccio " + Calcolatori.N(A, 2) + " m, bobina al " + Calcolatori.N(pc * 100, 0) + " %", 3, A, h - 2.2 * q);
            o.HideGround = true;   // 1.0.12: senza il terreno il dipolo occupa tutto il disegno e le quote non si sovrappongono
            return o;
        }

        CalcOutput ComputeDue()
        {
            CalcOutput o = new CalcOutput();
            double f1 = P("f1"), f2 = P("f2"), C = P("c") * 1e-12, Q = Math.Max(10, P("q")), a = P("d") / 2000, h = P("h");
            double w1 = 2 * Math.PI * f1 * 1e6, w2 = 2 * Math.PI * f2 * 1e6;
            double L = 1 / (w1 * w1 * C);
            double inner = 71.3 / f1;                        // lambda/4 alla banda alta, fino alla trappola
            double X = w2 * L / (1 - Math.Pow(f2 / f1, 2));   // reattanza (induttiva) della trappola alla banda bassa
            double lam2 = C0 / f2, beta = 2 * Math.PI / lam2;
            double Z0 = Calcolatori.Z0Monopolo(inner + 7, a);
            double outer = Math.Atan(Z0 / (X + Z0 * Math.Tan(beta * inner))) / beta * 0.95 * Scale;
            o.Card("Trappola", Calcolatori.Henry(L) + " / " + Calcolatori.Farad(C), "risuona a " + Calcolatori.N(f1, 3) + " MHz", true);
            o.Card("Tratto interno", Calcolatori.N(inner, 2) + " m", "per braccio, centro -> trappola");
            o.Card("Tratto esterno", Calcolatori.N(outer, 2) + " m", "per braccio, trappola -> punta");
            o.Card("Lunghezza totale", Calcolatori.N(2 * (inner + outer), 2) + " m", "contro " + Calcolatori.N(2 * 71.3 / f2, 1) + " m senza trappole");
            o.Card("Reattanza della trappola", "+j" + Calcolatori.N(X, 0) + " Ω", "a " + Calcolatori.N(f2, 3) + " MHz (bobina di carico)");
            o.Formula = "Formule: L = 1/((2 pi f1)^2 C); interno = 71,3/f1; trappola alla banda bassa X = w L / (1 - (f2/f1)^2); esterno b da cot(beta b) = (X + Z0 tan(beta a)) / Z0 " +
                "con Z0 = 60 (ln(2h/a) - 1). Riferimento W3DZZ: 60 pF e 8,2 uH a 7,05 MHz, 2 x 10,1 m + 2 x 6,7 m. Il NEC (trappole come carichi RLC paralleli) verifica le due bande.";
            if (f2 >= f1) o.Warnings.Add("La banda bassa deve essere sotto quella delle trappole.");
            o.Oks.Add("W3DZZ (7,05 MHz, 60 pF): L calcolata " + Calcolatori.Henry(1 / Math.Pow(2 * Math.PI * 7.05e6, 2) / 60e-12) + ", W3DZZ 8,2 uH");
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f1, 3) + "/" + Calcolatori.N(f2, 3) + " MHz");
            double seg = C0 / f1 / 60;
            int tc = d.W(1, 0, -seg / 2, h, 0, seg / 2, h, a);
            int ti1 = d.Ws(seg, 0, seg / 2, h, 0, inner, h, a, false, 5);
            int ni = d.LastN;
            int to1 = d.Ws(seg, 0, inner, h, 0, inner + outer, h, a, false, 3);
            int ti2 = d.Ws(seg, 0, -seg / 2, h, 0, -inner, h, a, false, 5);
            int to2 = d.Ws(seg, 0, -inner, h, 0, -inner - outer, h, a, false, 3);
            d.Ground(h > 0 ? 2 : 0);
            double rp = Q * w1 * L;
            d.Ld(1, ti1, ni, ni, rp, L, C);
            d.Ld(1, ti2, ni, ni, rp, L, C);
            d.Ex(tc, 1);
            o.Nec = d.End(f2);
            o.Plane = "yz";
            o.Mark(0, inner, h + (inner + outer) * 0.08, "trappola", 2);
            o.Mark(0, -inner, h + (inner + outer) * 0.08, "trappola", 2);
            o.Mark(7, 0, h - (inner + outer) * 0.12, Calcolatori.N(inner, 2) + " m", 3, inner, h - (inner + outer) * 0.12);
            o.Mark(7, inner, h - (inner + outer) * 0.12, Calcolatori.N(outer, 2) + " m", 3, inner + outer, h - (inner + outer) * 0.12);
            o.Warnings.Add("Il modello NEC e' calcolato alla banda bassa (" + Calcolatori.N(f2, 3) + " MHz): per la banda alta cambia la frequenza nella barra in alto dopo averlo aperto.");
            return o;
        }
    }

    public class CalcOcf : SimpleCalc
    {
        public CalcOcf()
        {
            Key = "ocf"; Title = "Dipolo OCF / Windom"; Source = "OCF (alimentazione a 1/3, balun 4:1); Windom originale con filo singolo; L. G. Windom W8GZ, \"Notes on Ethereal Adornments\", QST set. 1929 pp. 19-22; F. Spillner DJ2KY, \"Die FD4-Windom-Antenne\", QRV dic. 1971 pp. 13-20; Belrose e Bouliane, \"The Off-Center-Fed Dipole Revisited\", QST ago. 1990 pp. 28-34";
            Params.Add(new CalcParam("f", "Frequenza piu' bassa", 3.6, "MHz"));
            Params.Add(new CalcParam("p", "Punto di alimentazione (frazione)", 0.33, "", "0,33 = un terzo dalla punta: impedenza 200-300 ohm, balun 4:1"));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(new CalcParam("h", "Quota", 12, "m"));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), p = Math.Max(0.1, Math.Min(0.5, P("p"))), a = P("d") / 2000, h = P("h"), lam = C0 / f;
            double L = 142.6 / f * Scale;
            o.Card("Lunghezza totale", Calcolatori.N(L, 2) + " m", "come il dipolo a mezz'onda", true);
            o.Card("Braccio corto", Calcolatori.N(L * p, 2) + " m", Calcolatori.N(p * 100, 0) + " %");
            o.Card("Braccio lungo", Calcolatori.N(L * (1 - p), 2) + " m", Calcolatori.N((1 - p) * 100, 0) + " %");
            o.Card("Impedenza attesa", p < 0.4 ? "200-300 Ω" : "70-150 Ω", p < 0.4 ? "balun 4:1 in corrente" : "balun 2:1");
            o.Card("Bande armoniche", Calcolatori.N(2 * f, 1) + ", " + Calcolatori.N(4 * f, 1) + ", " + Calcolatori.N(8 * f, 1) + " MHz", "con il punto a 1/3 (non la 3a armonica)");
            o.Formula = "Formule: L = 142,6/f; punto di alimentazione a 1/3 della lunghezza: impedenza circa 200-300 ohm sulle armoniche pari, balun 4:1. Il NEC verifica Z sulla banda base.";
            o.Warnings.Add("La discesa dell'OCF irradia se manca un buon balun in corrente (o un'impedenza di modo comune) sotto il punto di alimentazione.");
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 80;
            int n = Math.Max(31, (int)Math.Ceiling(L / seg));
            int t = d.W(n, 0, -L * p, h, 0, L * (1 - p), h, a);
            d.Ground(h > 0 ? 2 : 0);
            d.Ex(t, Math.Max(1, (int)Math.Round(n * p + 0.5)));
            o.Nec = d.End(f);
            o.Plane = "yz";
            o.Mark(7, -L * p, h - L * 0.06, Calcolatori.N(L * p, 2) + " m", 3, 0, h - L * 0.06);
            o.Mark(7, 0, h - L * 0.06, Calcolatori.N(L * (1 - p), 2) + " m", 3, L * (1 - p), h - L * 0.06);
            return o;
        }
    }

    public class CalcG5rv : SimpleCalc
    {
        public CalcG5rv()
        {
            Key = "g5rv"; Title = "G5RV"; Source = "L. Varney G5RV (RSGB Bulletin 1966): 102 piedi (31,1 m) + 34 piedi (10,36 m) di linea aperta";
            Params.Add(new CalcParam("f", "Frequenza di progetto (3/2 lambda)", 14.15, "MHz"));
            Params.Add(CalcParam.Scelta("linea", "Linea di adattamento", 0, null, "aperta 600 Ω", "scaletta 450 Ω", "piattina 300 Ω"));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(new CalcParam("h", "Quota", 12, "m"));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, h = P("h"), lam = C0 / f;
            int li = I("linea");
            double[] z0 = { 600, 450, 300 }, vf = { 0.97, 0.90, 0.82 };
            double top = 31.1 * 14.15 / f * Scale, match = 10.36 / 0.97 * vf[li] * 14.15 / f;
            o.Card("Parte orizzontale", Calcolatori.N(top, 2) + " m", "ogni braccio " + Calcolatori.N(top / 2, 2) + " m", true);
            o.Card("Linea di adattamento", Calcolatori.N(match, 2) + " m", z0[li] + " Ω, VF " + Calcolatori.N(vf[li], 2));
            o.Card("Lunghezze d'onda", Calcolatori.N(top / lam, 2) + " lambda", "a " + Calcolatori.N(f, 2) + " MHz");
            o.Card("Poi", "coassiale 50 Ω", "con un balun 1:1 o un'impedenza di modo comune");
            o.Formula = "Formule: parte orizzontale 31,1 m x 14,15/f; linea = mezz'onda elettrica a 14,15 MHz (10,36 m x VF/0,97). Il NEC calcola la parte orizzontale con la linea (scheda TL) fino al coassiale.";
            o.Warnings.Add("Fuori dai 20 m la G5RV vuole quasi sempre l'accordatore: e' un'antenna multibanda di compromesso.");
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 60;
            int n = Math.Max(41, (int)Math.Ceiling(top / seg)) | 1;
            int t = d.W(n, 0, -top / 2, h, 0, top / 2, h, a);
            int tx = d.W(1, 0, 0, Math.Max(0.5, h - match), 0, 0.1, Math.Max(0.5, h - match), a);
            d.Ground(h > 0 ? 2 : 0);
            d.Tl(t, (n + 1) / 2, tx, 1, z0[li], match / vf[li]);
            d.Ex(tx, 1);
            o.Nec = d.End(f);
            o.Plane = "yz";
            o.Mark(2, 0, h, "linea " + Calcolatori.N(match, 2) + " m", 1, 0, Math.Max(0.5, h - match), 0, true);
            o.Mark(7, -top / 2, h + top * 0.05, Calcolatori.N(top, 2) + " m", 3, top / 2, h + top * 0.05);
            return o;
        }
    }

    public class CalcT2fd : SimpleCalc
    {
        public CalcT2fd()
        {
            Key = "t2fd"; Title = "T2FD (dipolo ripiegato terminato)"; Source = "G. Countryman W3HH (QST 1949); B&W";
            Params.Add(new CalcParam("f", "Frequenza piu' bassa", 3.0, "MHz"));
            Params.Add(new CalcParam("r", "Resistenza di terminazione", 450, "Ω"));
            Params.Add(new CalcParam("pw", "Potenza del trasmettitore", 100, "W"));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(new CalcParam("h", "Quota del centro", 12, "m"));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), R = P("r"), a = P("d") / 2000, h = P("h"), lam = C0 / f;
            double L = lam / 3 * Scale, s = lam / 100;
            o.Card("Lunghezza", Calcolatori.N(L, 2) + " m", "lambda/3 alla frequenza piu' bassa", true);
            o.Card("Distanza tra i fili", Calcolatori.N(s, 2) + " m", "lambda/100");
            o.Card("Resistenza", Calcolatori.N(R, 0) + " Ω", "antinduttiva, almeno " + Calcolatori.N(P("pw") / 3, 0) + " W");
            o.Card("Alimentazione", "circa 300 Ω", "balun 4:1 su 75 Ω (B&W: 800 Ω, 16:1)");
            o.Card("Banda", Calcolatori.N(f, 1) + "-" + Calcolatori.N(6 * f, 0) + " MHz", "circa 1 : 6 (esempio 3-18 MHz, 33 m)");
            o.Formula = "Formule: L = lambda/3 alla frequenza piu' bassa (3 MHz -> 33 m), distanza lambda/100 (1 m), resistenza 400-480 ohm al centro del filo opposto, potenza della resistenza almeno 1/3 del trasmettitore. Fonte W3HH, B&W.";
            o.Warnings.Add("Una parte importante della potenza finisce nella resistenza, soprattutto alle frequenze basse: e' il prezzo della banda larga.");
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 80;
            int n = Math.Max(41, (int)Math.Ceiling(L / seg)) | 1;
            int t1 = d.W(n, 0, -L / 2, h, 0, L / 2, h, a);
            int t2 = d.W(n, 0, -L / 2, h + s, 0, L / 2, h + s, a);
            d.W(3, 0, -L / 2, h, 0, -L / 2, h + s, a);
            d.W(3, 0, L / 2, h, 0, L / 2, h + s, a);
            d.Ground(h > 0 ? 2 : 0);
            d.Ld(4, t2, (n + 1) / 2, (n + 1) / 2, R, 0, 0);
            d.Ex(t1, (n + 1) / 2);
            o.Nec = d.End(f);
            o.Plane = "yz";
            o.Mark(5, -L * 0.05, h + s, "R " + Calcolatori.N(R, 0) + " Ω", 5, L * 0.05, h + s);
            return o;
        }
    }

    // =============================================================================================
    // VERTICALI
    // =============================================================================================
    public class CalcQuartoTerra : SimpleCalc
    {
        public override bool Tubolare { get { return true; } }   // 0.4.30: tubi rastremabili (HF)
        // ON4UN, Low-Band DXing cap. 9, Tab. 9-1: resistenza equivalente di terra (ohm) dei radiali interrati
        static readonly double[] Nr = { 2, 15, 30, 60, 120 }, Lr = { 0.15, 0.25, 0.40 };
        static readonly double[,] Rg =
        {
            { 28.6, 15.3, 14.8, 11.6, 11.6 },   // 0,15 lambda
            { 28.1, 15.1, 12.2, 7.9, 6.9 },     // 0,25 lambda
            { 27.0, 13.1, 7.2, 5.2, 0.1 },      // 0,40 lambda
        };
        // Tab. 9-3: lunghezza ottima secondo il numero di radiali
        static readonly double[] OptN = { 4, 12, 24, 48, 96, 120 }, OptL = { 0.10, 0.15, 0.25, 0.35, 0.45, 0.50 };
        public CalcQuartoTerra()
        {
            Key = "quarto"; Title = "Verticale lambda/4 con radiali a terra"; Source = "ARRL Antenna Book (radiatore 234/f piedi); ON4UN Low-Band DXing cap. 9 (Tab. 9-1, 9-3; misure DJ8WL); Sevick come confronto";
            Params.Add(new CalcParam("f", "Frequenza", 7.1, "MHz"));
            Params.Add(new CalcParam("d", "Diametro del radiatore", 30, "mm"));
            Params.Add(new CalcParam("n", "Numero di radiali", 16, ""));
            Params.Add(new CalcParam("lr", "Lunghezza dei radiali", 0.25, "lambda"));
        }
        static double Interp(double[] xs, double x, Func<int, double> y)
        {
            if (x <= xs[0]) return y(0);
            for (int k = 0; k < xs.Length - 1; k++)
                if (x <= xs[k + 1]) return y(k) + (y(k + 1) - y(k)) * (x - xs[k]) / (xs[k + 1] - xs[k]);
            return y(xs.Length - 1);
        }
        public static double ResistenzaTerra(double n, double lr)
        {
            Func<int, double> riga = r => Interp(Nr, n, c => Rg[r, c]);
            return Math.Max(0, Interp(Lr, lr, riga));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, lam = C0 / f, n = Math.Max(1, P("n")), lr = Math.Max(0.05, P("lr"));
            double h = 71.3 / f * Scale;
            double rg = ResistenzaTerra(n, lr), rr = 36, eff = rr / (rr + rg);
            double sevick = n >= 16 ? 9 * 16 / n * (n >= 64 ? 0.5 : 1) : (n >= 8 ? 18 - (n - 8) * 9 / 8.0 : 29 - (n - 4) * 11 / 4.0);
            double lOtt = Interp(OptN, n, k => OptL[k]);
            o.Card("Radiatore", Calcolatori.N(h, 2) + " m", "71,3 / f", true);
            o.Card("Resistenza di terra", Calcolatori.N(rg, 1) + " Ω", "ON4UN Tab. 9-1 (" + Calcolatori.N(n, 0) + " x " + Calcolatori.N(lr, 2) + " λ)");
            o.Card("Efficienza", Calcolatori.N(eff * 100, 0) + " %", Calcolatori.N(10 * Math.Log10(eff), 1) + " dB", true);
            o.Card("Impedenza", Calcolatori.N(rr + rg, 0) + " Ω", "36 Ω + terra");
            o.Card("Lunghezza ottima", Calcolatori.N(lOtt, 2) + " λ = " + Calcolatori.N(lOtt * lam, 1) + " m", "ON4UN Tab. 9-3 per " + Calcolatori.N(n, 0) + " radiali");
            o.Card("Filo totale", Calcolatori.N(n * lr * lam, 0) + " m", "Sevick: circa " + Calcolatori.N(Math.Max(0, sevick), 0) + " Ω di terra");
            o.Formula = "Formule: radiatore 71,3/f; resistenza di terra dalla Tab. 9-1 di ON4UN (radiali interrati, numero e lunghezza, interpolata): " +
                "2 radiali circa 28 Ω, 15 circa 15 Ω, 60 x 0,25 λ 7,9 Ω, 120 x 0,4 λ 0,1 Ω; efficienza = 36 / (36 + Rterra). Tab. 9-3: lunghezza ottima 0,10 λ (4 radiali) ... 0,50 λ (120). " +
                "Confronto: Sevick (ARRL) circa 29 / 18 / 9 / 0 Ω con 4 / 8 / 16 / infiniti radiali. La Tab. 9-1 viene dalle misure di Brown, Lewis ed Epstein (Proc. IRE 1937, circa 3 MHz, un solo terreno, Fig. 25): " +
                "verificata alla fonte, a 88 gradi coincide entro 0,2 Ω per 15 e 60 radiali, incertezza 2-4 Ω. Avvertenza (N6MW, 2016): la parte teorica di Brown " +
                "(rapporto tra corrente nel terreno e nei radiali) non concorda con i calcoli moderni; le misure restano valide. Il NEC-2 non tratta i radiali interrati (serve il NEC-4): " +
                "il modello usa il terreno perfetto con la resistenza di terra in serie.";
            o.Warnings.Add("DJ8WL (misure in ON4UN): con 8 radiali la verticale mostra \"50 Ω perfetti\" ma l'efficienza e' sotto il 50 %: 50 Ω non vuol dire efficiente.");
            if (lr > lOtt * 1.3) o.Warnings.Add("Radiali piu' lunghi dell'ottimo per questo numero (Tab. 9-3): conviene metterne di piu' e piu' corti.");
            o.Chart = new CalcChart { Title = "Efficienza al variare del numero di radiali (" + Calcolatori.N(lr, 2) + " λ, ON4UN Tab. 9-1)", XLabel = "radiali", YLabel = "%" };
            o.Chart.X = Calcolatori.Lin(2, 120, 60); o.Chart.Y = new double[60];
            for (int i = 0; i < 60; i++) o.Chart.Y[i] = 100 * rr / (rr + ResistenzaTerra(o.Chart.X[i], lr));
            o.Chart.MarkX = Math.Min(120, n); o.Chart.MarkText = Calcolatori.N(eff * 100, 0) + " %";
            o.Chart.YMin = 0;
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            int t = d.Ws(lam / 60, 0, 0, 0, 0, 0, h, a, false, 11);
            d.Ground(1, true);      // terreno perfetto + resistenza di terra della tabella (come il "MININEC ground" di EZNEC)
            d.Ld(4, t, 1, 1, rg, 0, 0);
            d.Ex(t, 1);
            o.Nec = d.End(f);
            o.Plane = "xz";
            for (int i = -2; i <= 2; i++) if (i != 0) o.Mark(2, 0, 0, null, 3, i * lr * lam / 2, 0, 0, true);
            o.Mark(0, lr * lam * 0.3, -h * 0.06, Calcolatori.N(n, 0) + " radiali da " + Calcolatori.N(lr * lam, 1) + " m", 3);
            return o;
        }
    }

    public class CalcCinqueOttavi : SimpleCalc
    {
        public override bool Tubolare { get { return true; } }   // 0.4.30: tubi rastremabili (HF)
        public CalcCinqueOttavi()
        {
            Key = "58"; Title = "Verticale 5/8 lambda"; Source = "ARRL Antenna Book (5/8 lambda con bobina alla base); Z0 del monopolo 60 (ln(2h/a) - 1); Z0 del monopolo da C. E. Smith e E. M. Johnson, Proc. IRE ott. 1947";
            Params.Add(new CalcParam("f", "Frequenza", 145, "MHz"));
            Params.Add(new CalcParam("d", "Diametro dello stilo", 4, "mm"));
            Params.Add(new CalcParam("q", "Q della bobina", 200, ""));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, lam = C0 / f, Q = Math.Max(20, P("q"));
            double h = 0.625 * lam * 0.95;
            double z0 = Calcolatori.Z0Monopolo(h, a), bh = 2 * Math.PI * h / lam;
            double X = z0 / Math.Tan(bh) * Scale;   // reattanza capacitiva da compensare (cot positivo tra 180 e 270 gradi)
            double L = Math.Abs(X) / (2 * Math.PI * f * 1e6);
            o.Card("Stilo", Calcolatori.N(h, 3) + " m", "0,625 lambda x 0,95", true);
            o.Card("Bobina alla base", Calcolatori.Henry(L), "+j" + Calcolatori.N(Math.Abs(X), 0) + " Ω (stima, poi il NEC)");
            o.Card("Guadagno atteso", "circa 3 dB", "sul lambda/4, a bassa elevazione");
            o.Card("Piano di terra", "radiali lambda/4", "o tetto dell'auto");
            o.Formula = "Formule: stilo 0,625 lambda x 0,95; reattanza da compensare con il modello a linea: X = Z0 cot(beta h), Z0 = 60 (ln(2h/a) - 1). " +
                "La linea e' solo una stima vicino alla mezz'onda: il pulsante del NEC cerca la bobina che annulla la reattanza (risonanza).";
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            int t = d.Ws(lam / 60, 0, 0, 0, 0, 0, h, a, false, 15);
            d.Ground(1, true);
            d.Ld(0, t, 1, 1, 2 * Math.PI * f * 1e6 * L / Q, L, 0);
            d.Ex(t, 1);
            o.Nec = d.End(f);
            o.Plane = "xz";
            o.Mark(6, 0, 0, "bobina " + Calcolatori.Henry(L), 2, 0, h * 0.06);
            o.Warnings.Add("Il modello usa il terreno perfetto (piano di massa ideale): con radiali corti o un tetto piccolo il risultato cambia.");
            return o;
        }
    }

    public class CalcCaricata : SimpleCalc
    {
        public CalcCaricata()
        {
            Key = "caricata"; Title = "Verticale accorciata con bobina"; Source = "linea del monopolo Z0 = 60 (ln(2h/a) - 1), X = Z0 cot(beta h); Rr = 160 pi^2 (heff/lambda)^2; C. E. Smith e E. M. Johnson, \"Performance of Short Antennas\", Proc. IRE ott. 1947 (Z0 = 60 (ln(h/r) - 1), X = Z0 cot h); R. Severns N6LF, \"Some Ideas for Short 160 Meter Verticals\", QEX mag./giu. 2013 (Q della bobina ed efficienza)";
            Params.Add(new CalcParam("f", "Frequenza", 3.6, "MHz"));
            Params.Add(new CalcParam("h", "Altezza dello stilo", 6, "m"));
            Params.Add(new CalcParam("d", "Diametro", 25, "mm"));
            Params.Add(CalcParam.Scelta("pos", "Posizione della bobina", 0, null, "alla base", "a meta'", "a 3/4"));
            Params.Add(new CalcParam("q", "Q della bobina", 200, ""));
            Params.Add(new CalcParam("rg", "Resistenza di terra", 10, "Ω", "Radiali: 2-5 ohm con molti radiali, 10-20 ohm con pochi."));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), h = P("h"), a = P("d") / 2000, lam = C0 / f, Q = Math.Max(20, P("q")), rg = Math.Max(0, P("rg"));
            double hc = new[] { 0.0, 0.5, 0.75 }[Math.Max(0, Math.Min(2, I("pos")))] * h;
            double z0 = Calcolatori.Z0Monopolo(h, a), b = 2 * Math.PI / lam;
            double X = z0 * (1 / Math.Tan(b * (h - hc)) - Math.Tan(b * hc)) * Scale;
            double w = 2 * Math.PI * f * 1e6, L = X / w, rc = X / Q;
            double heff = (h + hc) / 2, rr = 160 * Math.PI * Math.PI * Math.Pow(heff / lam, 2);
            // con la bobina in alto la corrente nella bobina e' minore di quella alla base: perdite pesate con cos^2
            double ic = hc > 0 ? Math.Cos(b * hc) : 1;
            double rcb = rc * ic * ic;
            double eff = rr / (rr + rcb + rg);
            o.Card("Bobina", Calcolatori.Henry(L), "+j" + Calcolatori.N(X, 0) + " Ω", true);
            o.Card("Resistenza di radiazione", Calcolatori.Ohm(rr), "altezza efficace " + Calcolatori.N(heff, 2) + " m");
            o.Card("Perdita nella bobina", Calcolatori.Ohm(rcb), "Q " + Calcolatori.N(Q, 0));
            o.Card("Efficienza", Calcolatori.N(eff * 100, 0) + " %", Calcolatori.N(10 * Math.Log10(eff), 1) + " dB", true);
            o.Card("Impedenza alla base", Calcolatori.Ohm(rr + rcb + rg), "da adattare a 50 Ω");
            Hint("h", "= " + Calcolatori.N(h / lam, 3) + " lambda (lambda/4 = " + Calcolatori.N(71.3 / f, 1) + " m)");
            o.Formula = "Formule: Z0 = 60 (ln(2h/a) - 1); bobina X = Z0 [cot(beta (h - hc)) - tan(beta hc)] (bobina a quota hc); Rr = 160 pi^2 (heff/lambda)^2 con heff = (h + hc)/2 " +
                "(base: Rr = 395 (h/lambda)^2); perdita della bobina X/Q; efficienza Rr / (Rr + Rbobina + Rterra).";
            o.Chart = new CalcChart { Title = "Efficienza al variare del Q della bobina", XLabel = "Q", YLabel = "%" };
            o.Chart.X = Calcolatori.Lin(50, 600, 23); o.Chart.Y = new double[23];
            for (int i = 0; i < 23; i++) o.Chart.Y[i] = 100 * rr / (rr + X / o.Chart.X[i] * ic * ic + rg);
            o.Chart.MarkX = Q; o.Chart.MarkText = Calcolatori.N(eff * 100, 0) + " %";
            if (h >= lam / 4) o.Warnings.Add("Lo stilo e' gia' lungo lambda/4 o piu': la bobina non serve.");
            if (hc > 0) o.Oks.Add("bobina in alto: corrente piu' uniforme nel tratto basso, Rr maggiore che con la bobina alla base");
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = Math.Min(lam / 60, h / 21);
            int n = Math.Max(21, (int)Math.Ceiling(h / seg));
            int t = d.W(n, 0, 0, 0, 0, 0, h, a);
            d.Ground(1, true);      // terreno perfetto + resistenza di terra in serie (radiali non modellabili nel NEC-2)
            int sc = Math.Max(1, (int)Math.Round(hc / h * n + 0.5));
            d.Ld(0, t, sc, sc, rc, L, 0);
            d.Ld(4, t, 1, 1, rg, 0, 0);
            d.Ex(t, 1);
            o.Nec = d.End(f);
            o.Plane = "xz";
            o.Mark(6, 0, Math.Max(0, hc - h * 0.04), "bobina " + Calcolatori.Henry(L), 2, 0, hc + h * 0.04);
            return o;
        }
    }

    public class CalcJpole : SimpleCalc
    {
        public CalcJpole()
        {
            Key = "jpole"; Title = "J-pole"; Source = "ARRL Antenna Book (J-pole: 3/4 lambda + 1/4 lambda, presa a circa 0,02 lambda); Cebik W4RNL, Some J-Poles That I Have Known e What Is a Slim Jim?";
            Params.Add(new CalcParam("f", "Frequenza", 145, "MHz"));
            Params.Add(new CalcParam("d", "Diametro dei tubi", 12, "mm"));
            Params.Add(new CalcParam("s", "Distanza tra i tubi", 40, "mm"));
            Params.Add(new CalcParam("t", "Presa del cavo dal fondo", 32, "mm", "Prova nec2c a 145 MHz (tubi 12 mm a 40 mm): 30 mm -> 45 + j23 ohm, 35 mm -> 60 + j24 ohm. Slim Jim: 50 mm -> 48 + j7 ohm."));
            // 1.5 (proposta j2 e bozzetto Calcolatori 1.5 approvati da IC8TEM il 18/11/2026): Slim Jim, Cebik W4RNL "What Is a Slim Jim?"
            Params.Insert(0, CalcParam.Scelta("tipo", "Tipo", 0, "La Slim Jim chiude in alto il radiatore con un filo ripiegato verso il basso, fino a poco sopra lo stub.", "J-pole", "Slim Jim (filo ripiegato)"));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, s = P("s") / 1000, tp = P("t") / 1000, lam = C0 / f;
            double Ll = 0.75 * lam * 0.95 * Scale, Lc = 0.25 * lam * 0.95 * Scale;
            o.Card("Elemento lungo", Calcolatori.N(Ll * 100, 1) + " cm", "3/4 lambda x 0,95", true);
            o.Card("Elemento corto", Calcolatori.N(Lc * 100, 1) + " cm", "1/4 lambda x 0,95 (stub)");
            o.Card("Distanza", Calcolatori.N(s * 1000, 0) + " mm", "circa 0,02 lambda = " + Calcolatori.N(0.02 * lam * 1000, 0) + " mm");
            o.Card("Presa del cavo", Calcolatori.N(tp * 1000, 0) + " mm", "dal fondo, da regolare per il ROS");
            o.Formula = "Formule: elemento lungo 0,75 lambda x 0,95, stub 0,25 lambda x 0,95, distanza circa 0,02 lambda, presa a circa 0,02 lambda dal ponticello: si sposta per il minimo ROS (il NEC lo mostra).";
            o.Warnings.Add("Senza un'impedenza di modo comune sul cavo la J-pole irradia anche dalla calza: il lobo si alza.");
            bool slim = I("tipo") == 1;
            double gap = 0.012 * lam;
            if (slim)
            {
                o.Card("Filo ripiegato", Calcolatori.N((Ll - Lc - gap) * 100, 1) + " cm", "dalla cima fino a " + Calcolatori.N(gap * 1000, 0) + " mm sopra lo stub (0,012 lambda)");
                o.Formula += " Slim Jim: il radiatore si chiude in cima e torna giu' parallelo, a " + Calcolatori.N(s * 1000, 0) + " mm, fino a 0,012 lambda sopra lo stub; la presa va piu' in alto (prova nec2c a 145 MHz, tubi 12 mm a 40 mm: 50 mm -> 48 + j7 ohm).";
            }
            o.Oks.Add("Cebik W4RNL: guadagno circa 2,5 dBi in spazio libero (J-pole standard: 2,46 dBi dopo il test AGT); la Slim Jim non guadagna di piu': le versioni a due fili stanno entro 0,08 dB, la migliore +0,23 dB");
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 80;
            int tb = d.W(1, 0, 0, 0, s, 0, 0, a);
            int tl1 = d.Ws(seg, 0, 0, 0, 0, 0, tp, a);
            int tl2 = d.Ws(seg, 0, 0, tp, 0, 0, Ll, a, false, 5);
            int tc1 = d.Ws(seg, s, 0, 0, s, 0, tp, a);
            int tc2 = d.Ws(seg, s, 0, tp, s, 0, Lc, a, false, 3);
            int tf = d.W(1, 0, 0, tp, s, 0, tp, a);
            if (slim)
            {
                d.W(1, 0, 0, Ll, s, 0, Ll, a);
                d.Ws(seg, s, 0, Ll, s, 0, Lc + gap, a, false, 5);
            }
            d.Ground(0);
            d.Ex(tf, 1);
            o.Nec = d.End(f);
            o.Plane = "xz";
            o.Mark(0, s * 1.6, tp, "presa " + Calcolatori.N(tp * 1000, 0) + " mm", 1);
            return o;
        }
    }

    // =============================================================================================
    // DIRETTIVE SEMPLICI
    // =============================================================================================
    public class CalcHb9cv : SimpleCalc
    {
        public CalcHb9cv()
        {
            Key = "hb9cv"; Title = "HB9CV"; Source = "DK7ZB (HB9CV: riflettore 150/f, radiatore 139/f, spaziatura lambda/8, 4,15 dBd, F/B oltre 20 dB)";
            Params.Add(new CalcParam("f", "Frequenza", 145, "MHz"));
            Params.Add(new CalcParam("d", "Diametro degli elementi", 8, "mm"));
            Params.Add(new CalcParam("h", "Quota (0 = spazio libero)", 0, "m"));
            CanTune = false;
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, h = P("h"), lam = C0 / f;
            double lr = 150 / f, ld = 139 / f, sp = lam / 8;
            o.Card("Elemento posteriore", Calcolatori.N(lr * 100, 1) + " cm", "150 / f", true);
            o.Card("Elemento anteriore", Calcolatori.N(ld * 100, 1) + " cm", "139 / f (verso il lobo)");
            o.Card("Spaziatura", Calcolatori.N(sp * 100, 1) + " cm", "lambda/8");
            o.Card("Linea di fase", "incrociata", "l'anteriore in ritardo di 135 gradi (180 - 45)");
            o.Card("Guadagno atteso", "4,15 dBd", "F/B oltre 20 dB (DK7ZB)");
            o.Formula = "Formule (DK7ZB): elemento posteriore 150/f m, anteriore 139/f m, spaziatura lambda/8; i due elementi sono alimentati con correnti uguali, l'anteriore in ritardo di 135 gradi " +
                "(linea incrociata: 180 gradi meno i 45 della spaziatura) (gamma di adattamento da circa 15 pF). Il NEC usa due sorgenti a corrente con lo sfasamento ideale.";
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz", true);
            double seg = lam / 50, z = h;
            int t1 = d.Ws(seg, -sp / 2, -lr / 2, z, -sp / 2, lr / 2, z, a, true, 11);
            int n1 = d.LastN;
            int t2 = d.Ws(seg, sp / 2, -ld / 2, z, sp / 2, ld / 2, z, a, true, 11);
            int n2 = d.LastN;
            d.Ground(h > 0 ? 2 : 0);
            Complex i2 = Complex.FromPolarCoordinates(1, -135 * Math.PI / 180);
            d.Ex(t1, (n1 + 1) / 2, 1, 0);
            d.Ex(t2, (n2 + 1) / 2, i2.Real, i2.Imaginary);
            o.Nec = d.End(f);
            o.CurrentDrive = true;
            o.WantedCurrents = new[] { Complex.One, i2 };
            o.Plane = "xy";
            o.HideGround = true;
            o.Mark(0, sp, 0, "lobo ->", 1);
            o.Mark(7, -sp / 2, -lr / 2 - lam * 0.03, Calcolatori.N(sp * 100, 1) + " cm", 3, sp / 2, -lr / 2 - lam * 0.03);
            return o;
        }
    }

    public class CalcQuad : SimpleCalc
    {
        // 1.5 (proposta q1 approvata da IC8TEM il 18/11/2026 19:23, bozzetto Calcolatori 1.5 approvato alle 19:30):
        // metodo del perimetro. Cebik W4RNL, Cubical Quad Notes vol. 2, cap. 2: perimetro della spira risonante in funzione di
        // D1 = log10(d/lambda), polinomio di 4 grado (programma GW-Basic, righe 190-250), valido per d tra 1E-6 e 1E-2 lambda.
        // IC8TEM: "si ma solo per HF, per VHF usi altro metodo": sopra i 30 MHz (Automatico) il motore cerca la risonanza
        // sul modello vero partendo da Cebik. Riflettore e direttore restano nei rapporti ARRL (1030/1005 e 975/1005).
        public static event Action ProntoQuad;
        static readonly object nlLock = new object();
        static readonly Dictionary<string, double> nlCache = new Dictionary<string, double>();
        static readonly HashSet<string> nlInCorso = new HashSet<string>();
        bool soloCebik;   // istanza interna della ricerca NEC: niente ricerca annidata

        public CalcQuad()
        {
            Key = "quad"; Title = "Quad"; Source = "ARRL Antenna Book: perimetri 1005/f (radiatore), 1030/f (riflettore), 975/f (direttore) piedi; Cebik W4RNL, Cubical Quad Notes vol. 2 (perimetro in funzione del diametro)";
            Params.Add(new CalcParam("f", "Frequenza", 14.2, "MHz"));
            Params.Add(CalcParam.Scelta("n", "Elementi", 1, null, "1", "2", "3"));
            Params.Add(new CalcParam("s", "Spaziatura", 0.15, "lambda"));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(new CalcParam("h", "Quota del centro (0 = spazio libero)", 0, "m"));
            Params.Add(CalcParam.Scelta("met", "Metodo", 0, "Automatico: Cebik fino a 30 MHz; sopra i 30 MHz il motore cerca la risonanza sul modello vero, partendo da Cebik.",
                "Automatico", "ARRL (1005/f)", "Cebik (dipende dal diametro)", "NEC: risonanza cercata dal motore"));
        }

        /// <summary>Cebik, Cubical Quad Notes vol. 2: perimetro della spira quadrata risonante (in lambda) e resistenza alla risonanza (ohm).</summary>
        public static double[] Cebik(double dSuLambda)
        {
            double d1 = Math.Log10(dSuLambda);
            double l1 = 0.0001386272922 * Math.Pow(d1, 4) + 0.002677872933 * Math.Pow(d1, 3) + 0.01955406057 * d1 * d1 + 0.06540532144 * d1 + 0.2164538128;
            double l2 = 0.3131933 * Math.Pow(d1, 4) + 5.694978593 * Math.Pow(d1, 3) + 38.65184274 * d1 * d1 + 118.05237 * d1 + 262.9477727;
            return new[] { l1 * 8, l2, d1 };
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, h = P("h"), lam = C0 / f, sp = P("s") * lam;
            int ne = I("n") + 1, met = I("met");
            if (met == 0) met = f <= 30 ? 2 : 3;
            if (soloCebik) met = 2;
            double pArrl = 306.3 / f;
            double[] cb = Cebik(P("d") / 1000 / lam);
            double pCeb = cb[0] * lam, base = met == 1 ? pArrl : pCeb;
            string metodo = met == 1 ? "ARRL" : (met == 2 ? "Cebik" : "Cebik, poi NEC");
            if (met == 3)
            {
                string key = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:R}|{1:R}|{2}|{3:R}|{4:R}", f, P("d"), ne, P("s"), h);
                double sc; bool parti = false, pronto;
                lock (nlLock) { pronto = nlCache.TryGetValue(key, out sc); if (!pronto && !nlInCorso.Contains(key)) { nlInCorso.Add(key); parti = true; } }
                if (parti)
                {
                    CalcQuad q = new CalcQuad { soloCebik = true };
                    foreach (CalcParam cp in Params) foreach (CalcParam qp in q.Params) if (qp.Key == cp.Key) qp.Value = cp.Value;
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        double r = double.NaN;
                        try { r = q.Tune(f); } catch (Exception) { }
                        lock (nlLock) { nlInCorso.Remove(key); if (!double.IsNaN(r) && r > 0.8 && r < 1.2) nlCache[key] = r; }
                        Action ev = ProntoQuad; if (ev != null) ev();
                    });
                }
                if (pronto) { base = pCeb * sc; o.Oks.Add("perimetro trovato dal motore NEC (risonanza della spira alimentata): " + Calcolatori.N((sc - 1) * 100, 1) + " % rispetto a Cebik"); }
                else o.Oks.Add("ricerca della risonanza con il NEC in corso (qualche secondo); intanto il perimetro di Cebik");
            }
            double pd = base * Scale, pr = pd * 1030.0 / 1005.0, pdir = pd * 975.0 / 1005.0;
            o.Card("Radiatore", Calcolatori.N(pd, 2) + " m", "lato " + Calcolatori.N(pd / 4, 3) + " m (" + metodo + ")", true);
            if (ne >= 2) o.Card("Riflettore", Calcolatori.N(pr, 2) + " m", "lato " + Calcolatori.N(pr / 4, 3) + " m");
            if (ne >= 3) o.Card("Direttore", Calcolatori.N(pdir, 2) + " m", "lato " + Calcolatori.N(pdir / 4, 3) + " m");
            o.Card("Spaziatura", Calcolatori.N(sp, 2) + " m", Calcolatori.N(P("s"), 2) + " lambda");
            o.Card("Impedenza tipica", ne == 1 ? "circa " + Calcolatori.N(cb[1], 0) + " Ω" : (ne == 2 ? "50-75 Ω" : "40-60 Ω"), ne == 1 ? "Cebik; quarto d'onda da 75 Ω" : "");
            o.Add("Perimetro ARRL (1005/f)", Calcolatori.N(pArrl, 3) + " m");
            o.Add("Perimetro Cebik", Calcolatori.N(pCeb, 3) + " m (" + (pCeb >= pArrl ? "+" : "") + Calcolatori.N((pCeb / pArrl - 1) * 100, 1) + " %)");
            o.Formula = "Formule: ARRL, perimetro del radiatore 306,3/f m, riflettore 313,6/f m, direttore 297,2/f m. Cebik (Cubical Quad Notes vol. 2): D1 = log10(d/lambda), " +
                "perimetro = 8 x (0,0001386 D1^4 + 0,0026779 D1^3 + 0,0195541 D1^2 + 0,0654053 D1 + 0,2164538) lambda, valido per d tra 1E-6 e 1E-2 lambda; " +
                "riflettore e direttore nei rapporti ARRL. Automatico: Cebik fino a 30 MHz, sopra il motore NEC cerca la risonanza. Spaziatura 0,14-0,2 lambda; alimentazione al centro del lato basso = polarizzazione orizzontale.";
            if (cb[2] < -6 || cb[2] > -2) o.Warnings.Add("Diametro fuori dall'intervallo delle formule di Cebik (da 1E-6 a 1E-2 lambda).");
            if (met == 2 && f > 30) o.Warnings.Add("Sopra i 30 MHz la regressione di Cebik e' usata fuori dal campo per cui la si propone qui (fili in HF): meglio Automatico o NEC.");
            // 1.5 (proposta q2): Cebik, Cubical Quad Notes vol. 3, conclusioni
            o.Formula += " Cebik (Quad Notes vol. 3): oltre circa 4 elementi la quad perde il vantaggio di guadagno su una Yagi con lo stesso boom.";
            NecDeck d = new NecDeck(Title + " " + ne + " el. " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 40, zc = h;
            int feedTag = 0, feedSeg = 0;
            double[] per = ne == 1 ? new[] { pd } : ne == 2 ? new[] { pr, pd } : new[] { pr, pd, pdir };
            for (int e = 0; e < per.Length; e++)
            {
                double x = (e - (per.Length - 1) / 2.0) * sp, l = per[e] / 4;
                int tb = d.Ws(seg, x, -l / 2, zc - l / 2, x, l / 2, zc - l / 2, a, true, 5);
                int nb = d.LastN;
                d.Ws(seg, x, l / 2, zc - l / 2, x, l / 2, zc + l / 2, a, false, 5);
                d.Ws(seg, x, l / 2, zc + l / 2, x, -l / 2, zc + l / 2, a, false, 5);
                d.Ws(seg, x, -l / 2, zc + l / 2, x, -l / 2, zc - l / 2, a, false, 5);
                if (per[e] == pd) { feedTag = tb; feedSeg = (nb + 1) / 2; }
            }
            d.Ground(h > 0 ? 2 : 0);
            d.Ex(feedTag, feedSeg);
            o.Nec = d.End(f);
            o.Plane = ne == 1 ? "yz" : "xz";
            if (h > 0 && h < pd / 8 + 1) o.Warnings.Add("Il lato basso e' troppo vicino al suolo: alzare il centro.");
            return o;
        }
    }

    public class CalcQuadMulti : SimpleCalc
    {
        static readonly double[] Fb = { 14.175, 18.118, 21.225, 24.94, 28.5 };
        static readonly string[] Nb = { "20 m", "17 m", "15 m", "12 m", "10 m" };
        public CalcQuadMulti()
        {
            Key = "quadmulti"; Title = "Quad multibanda 2 el."; Source = "ARRL Antenna Book (perimetri per banda); impostazione degli anelli concentrici come DK7ZB";
            Params.Add(CalcParam.Scelta("b", "Banda alimentata nel NEC", 0, null, "20 m", "15 m", "10 m"));
            Params.Add(new CalcParam("s", "Boom (spaziatura unica)", 2.5, "m"));
            Params.Add(new CalcParam("d", "Diametro del filo", 1.5, "mm"));
            CanTune = false;
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double sp = P("s"), a = P("d") / 2000;
            int bsel = new[] { 0, 2, 4 }[Math.Max(0, Math.Min(2, I("b")))];
            for (int i = 0; i < Fb.Length; i++)
            {
                double f = Fb[i], lam = C0 / f;
                o.Card(Nb[i], "rad. " + Calcolatori.N(306.3 / f, 2) + " m", "rifl. " + Calcolatori.N(313.6 / f, 2) + " m, boom " + Calcolatori.N(sp / lam, 3) + " lambda", i == bsel);
            }
            o.Formula = "Formule: per ogni banda radiatore 306,3/f e riflettore 313,6/f (ARRL), anelli concentrici sulle stesse crociere e un solo boom: la spaziatura in lambda cambia da banda a banda. " +
                "Le interazioni tra gli anelli spostano le risonanze: il NEC le calcola con tutti gli anelli presenti (anche quelli non alimentati).";
            o.Warnings.Add("Su un boom unico la spaziatura e' ottima solo per una banda; DK7ZB e gli altri progettisti ritoccano i perimetri con il calcolo.");
            NecDeck d = new NecDeck(Title + " 20-10 m");
            int feedTag = 0, feedSeg = 0;
            for (int i = 0; i < Fb.Length; i++)
            {
                double f = Fb[i], seg = C0 / f / 30;
                for (int e = 0; e < 2; e++)
                {
                    double x = e == 0 ? -sp / 2 : sp / 2, l = (e == 0 ? 313.6 : 306.3) / f / 4;
                    // quadrato a rombo (diamante) come sulle crociere: alimentazione al vertice basso
                    double r = l / Math.Sqrt(2);
                    int t1 = d.Ws(seg, x, 0, -r, x, r, 0, a, false, 3);
                    d.Ws(seg, x, r, 0, x, 0, r, a, false, 3);
                    d.Ws(seg, x, 0, r, x, -r, 0, a, false, 3);
                    d.Ws(seg, x, -r, 0, x, 0, -r, a, false, 3);
                    if (e == 1 && i == bsel) { feedTag = t1; feedSeg = 1; }
                }
            }
            d.Ground(0);
            d.Ex(feedTag, feedSeg);
            o.Nec = d.End(Fb[bsel]);
            o.Plane = "yz";
            return o;
        }
    }

    public class CalcEh : SimpleCalc
    {
        public CalcEh()
        {
            Key = "eh"; Title = "Antenna EH"; Source = "T. Hart W5QJR (antenna EH); verifica con la resistenza di radiazione del dipolo corto 20 pi^2 (l/lambda)^2";
            Params.Add(new CalcParam("f", "Frequenza", 7.1, "MHz"));
            Params.Add(new CalcParam("l", "Lunghezza totale dei due cilindri", 1.0, "m"));
            Params.Add(new CalcParam("d", "Diametro dei cilindri", 80, "mm"));
            Params.Add(new CalcParam("q", "Q della rete di accordo", 200, ""));
            CanTune = false;
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), l = P("l"), a = P("d") / 2000, lam = C0 / f, Q = Math.Max(20, P("q"));
            double rr = 20 * Math.PI * Math.PI * Math.Pow(l / lam, 2);
            // dipolo corto: reattanza capacitiva X = -120 (ln(l/(2a)) - 1) / tan(pi l / lambda)
            double xc = 120 * (Math.Log(l / (2 * a)) - 1) / Math.Tan(Math.PI * l / lam);
            double rl = xc / Q, eff = rr / (rr + rl);
            o.Card("Resistenza di radiazione", Calcolatori.Ohm(rr), "dipolo corto " + Calcolatori.N(l / lam, 3) + " lambda", true);
            o.Card("Reattanza", "-j" + Calcolatori.N(xc, 0) + " Ω", "da compensare con la bobina");
            o.Card("Perdita della rete", Calcolatori.Ohm(rl), "Q " + Calcolatori.N(Q, 0));
            o.Card("Efficienza", Calcolatori.N(eff * 100, 1) + " %", Calcolatori.N(10 * Math.Log10(eff), 1) + " dB", true);
            o.Formula = "Formule: Rr = 20 pi^2 (l/lambda)^2; reattanza del dipolo corto X = -120 (ln(l/2a) - 1) / tan(pi l/lambda); perdita della bobina X/Q; efficienza Rr/(Rr + Rperdita).";
            o.Warnings.Add("Le prestazioni dichiarate per l'antenna EH (\"come un dipolo\") non sono confermate dalle misure indipendenti: e' un dipolo corto, e il NEC lo mostra. " +
                "Spesso irradia soprattutto il cavo coassiale.");
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            int n = 11;
            int t = d.W(n, 0, 0, -l / 2, 0, 0, l / 2, a);
            d.Ground(0);
            double L = xc / (2 * Math.PI * f * 1e6);
            d.Ld(0, t, 6, 6, rl, L, 0);
            d.Ex(t, 6);
            o.Nec = d.End(f);
            o.Plane = "xz";
            o.Schema = "eh";      // disegno: schema dell'antenna EH (due cilindri, condensatore variabile, bobine, SO239), senza scritte
            return o;
        }
    }

    public class CalcMoxon : SimpleCalc
    {
        public CalcMoxon()
        {
            Key = "moxon"; Title = "Moxon"; Source = "L.B. Cebik W4RNL, MOXGEN (formule in funzione di d/lambda); tabella DK7ZB";
            Params.Add(new CalcParam("f", "Frequenza", 28.5, "MHz"));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(new CalcParam("h", "Quota (0 = spazio libero)", 0, "m"));
            // 0.4.8 (IC8TEM 13/11/2026 23:35): forma modificabile, per inserire il Moxon in un progetto piu' grande (Yagi bibanda);
            // 0 = misura di MOXGEN. Il calcolo in tempo reale si rifa' a ogni modifica.
            Params.Add(new CalcParam("A", "A larghezza (0 = MOXGEN)", 0, "m"));
            Params.Add(new CalcParam("B", "B coda del radiatore (0 = MOXGEN)", 0, "m"));
            Params.Add(new CalcParam("C", "C spazio tra le code (0 = MOXGEN)", 0, "m"));
            Params.Add(new CalcParam("D", "D coda del riflettore (0 = MOXGEN)", 0, "m"));
            // 1.0.10 (IC8TEM 16/11/2026 22:36 "mi devi permettere che io fissi la lettera E perche' sono vincolato e cosi' mi ricalcoli
            // il moxon in diretta"; disegno con E e la casella "fissa A-E"): profondita' E vincolata; B, C, D (e A se non e' fissa) ricalcolati
            // con il NEC in sottofondo per il F/B migliore con ROS sotto 1,5 su 50 ohm.
            Params.Add(new CalcParam("E", "E profondita' (0 = MOXGEN)", 0, "m", "Profondita' vincolata (dal radiatore al riflettore). Con E scritta il programma ricalcola B, C e D (e A, se non e' fissa) con il NEC."));
            Params.Add(CalcParam.Scelta("fae", "Fissa A ed E", 0, "Si': A (scritta o di MOXGEN) ed E restano fisse e il NEC ricalcola solo B, C e D.", "no", "si'"));
        }

        // ---- 1.0.10: adattamento con E (e A) vincolate, in sottofondo ----
        public static event Action ProntoMoxon;
        static readonly object fitLock = new object();
        static readonly Dictionary<string, double[]> fitCache = new Dictionary<string, double[]>();
        static readonly HashSet<string> fitInCorso = new HashSet<string>();

        static double[] ProvaNec(double f, double a, double A, double B, double C, double D)
        {
            double lam = C0 / f, E = B + C + D, seg = lam / 60;
            NecDeck d = new NecDeck("Moxon vincolato");
            int td = d.Ws(seg, 0, -A / 2, 0, 0, A / 2, 0, a, true, 21);
            int nd = d.LastN;
            d.Ws(seg, 0, A / 2, 0, -B, A / 2, 0, a, false, 3);
            d.Ws(seg, 0, -A / 2, 0, -B, -A / 2, 0, a, false, 3);
            d.Ws(seg, -E, -A / 2, 0, -E, A / 2, 0, a, true, 21);
            d.Ws(seg, -E, A / 2, 0, -E + D, A / 2, 0, a, false, 3);
            d.Ws(seg, -E, -A / 2, 0, -E + D, -A / 2, 0, a, false, 3);
            d.Ground(0);
            d.Ex(td, (nd + 1) / 2);
            NecModel m = NecModel.Parse(d.End(f));
            SegmentGeometry g = m.BuildGeometry();
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            double ga = e.GainLinear(90, 0), gb = e.GainLinear(90, 180);
            System.Numerics.Complex z = e.Sources[0].Z;
            return new[] { z.Real, z.Imaginary, 10 * Math.Log10(Math.Max(ga, 1e-12)), 10 * Math.Log10(Math.Max(ga, 1e-12) / Math.Max(gb, 1e-12)) };
        }

        /// <summary>A, B, C, D, R, X, G, F/B per E (e A) vincolate: griglia di partenza su A (se libera), B/E e C/E, poi sezione aurea
        /// variabile per variabile. Obiettivo: F/B verso 45 dB, ROS su 50 ohm sotto 1,5, guadagno. Prova con nec2c (scratchpad hex6/mox.py,
        /// 28,5 MHz, tubo 20 mm): E 1,0 m con A libera A 4,12 B 0,34 C 0,11 D 0,55 m, 30 -j2,7 ohm, 6,21 dBi, F/B 26,8 dB; E 1,7 m
        /// A 3,60 m, F/B 35,7 dB; E 1,404 (MOXGEN) con A fissa ritrova la forma di MOXGEN (F/B 45,6 dB).</summary>
        static double[] Adatta(double f, double a, double E, double Afix, double A0)
        {
            double lam = C0 / f;
            Func<double, double, double, double> J = (A, B, C) =>
            {
                double D = E - B - C;
                if (Math.Min(B, Math.Min(C, D)) < 0.004 * lam || 2 * B >= A || 2 * D >= A) return 1e9;
                double[] r = ProvaNec(f, a, A, B, C, D);
                System.Numerics.Complex z = new System.Numerics.Complex(r[0], r[1]);
                double gm = ((z - 50) / (z + 50)).Magnitude, swr = gm >= 0.999 ? 999 : (1 + gm) / (1 - gm);
                return Math.Max(0, swr - 1.5) * 2 + Math.Max(0, 45 - r[3]) / 10 - r[2] / 20;
            };
            bool libera = double.IsNaN(Afix);
            double best = double.MaxValue, bA = libera ? A0 : Afix, bB = 0.36 * E, bC = 0.12 * E;
            foreach (double fa in libera ? new[] { 0.95, 1.0, 1.05, 1.1, 1.15 } : new[] { 1.0 })
                for (int bi = 0; bi < 9; bi++)
                    for (int ci = 0; ci < 7; ci++)
                    {
                        double A = (libera ? A0 : Afix) * fa, B = E * (0.15 + 0.05 * bi), C = E * (0.04 + 0.04 * ci), j = J(A, B, C);
                        if (j < best) { best = j; bA = A; bB = B; bC = C; }
                    }
            for (int giro = 0; giro < 4; giro++)
            {
                double A1 = bA, B1 = bB, C1 = bC;
                if (libera) bA = Aurea(x => J(x, B1, C1), 0.95 * A1, 1.05 * A1);
                double A2 = bA;
                bB = Aurea(x => J(A2, x, C1), 0.85 * B1, Math.Min(1.15 * B1, E - C1 - 0.01));
                double B2 = bB;
                bC = Aurea(x => J(A2, B2, x), 0.75 * C1, Math.Min(1.25 * C1, E - B2 - 0.01));
            }
            double[] fin = ProvaNec(f, a, bA, bB, bC, E - bB - bC);
            return new[] { bA, bB, bC, E - bB - bC, fin[0], fin[1], fin[2], fin[3] };
        }

        static double Aurea(Func<double, double> F, double lo, double hi)
        {
            if (hi <= lo) return lo;
            double gr = (Math.Sqrt(5) - 1) / 2, x1 = hi - gr * (hi - lo), x2 = lo + gr * (hi - lo), f1 = F(x1), f2 = F(x2);
            for (int i = 0; i < 10; i++)
            {
                if (f1 < f2) { hi = x2; x2 = x1; f2 = f1; x1 = hi - gr * (hi - lo); f1 = F(x1); }
                else { lo = x1; x1 = x2; f1 = f2; x2 = lo + gr * (hi - lo); f2 = F(x2); }
            }
            return (lo + hi) / 2;
        }
        internal static double[] Dimensioni(double f, double dmm)
        {
            double lam = C0 / f, x = Math.Log10(dmm / 1000 / lam);
            double A = -0.0008571428571 * x * x - 0.009571428571 * x + 0.3398571429;
            double B = -0.002142857143 * x * x - 0.02035714286 * x + 0.008285714286;
            double C = 0.001809523381 * x * x + 0.01780952381 * x + 0.05164285714;
            double D = 0.001 * x + 0.07178571429;
            return new[] { A * lam, B * lam, C * lam, D * lam, (B + C + D) * lam, x };
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), dmm = P("d"), h = P("h"), lam = C0 / f, a = dmm / 2000;
            double[] m = Dimensioni(f, dmm);
            double A = m[0] * Scale, B = m[1] * Scale, Cg = m[2], D = m[3] * Scale;
            bool[] man = { P("A") > 0, P("B") > 0, P("C") > 0, P("D") > 0 };
            if (man[0]) A = P("A");
            if (man[1]) B = P("B");
            if (man[2]) Cg = P("C");
            if (man[3]) D = P("D");
            double E = B + Cg + D;
            // 1.0.10: E vincolata (e A, con "Fissa A ed E")
            bool fae = I("fae") == 1, vincolo = P("E") > 0 || fae;
            double[] fit = null;
            if (vincolo)
            {
                double Ev = P("E") > 0 ? P("E") : m[4], Afix = fae ? (P("A") > 0 ? P("A") : m[0]) : double.NaN;
                string key = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:R}|{1:R}|{2:R}|{3:R}", f, dmm, Ev, Afix);
                bool parti = false;
                lock (fitLock) { if (!fitCache.TryGetValue(key, out fit) && !fitInCorso.Contains(key)) { fitInCorso.Add(key); parti = true; } }
                if (parti)
                {
                    double ff = f, aa = a, A0 = m[0];
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        double[] rr = null;
                        try { rr = Adatta(ff, aa, Ev, Afix, A0); } catch (Exception) { }
                        lock (fitLock) { fitInCorso.Remove(key); if (rr != null) fitCache[key] = rr; }
                        Action pr = ProntoMoxon; if (pr != null) pr();
                    });
                }
                if (fit != null) { A = fit[0]; B = fit[1]; Cg = fit[2]; D = fit[3]; E = Ev; man = new[] { !double.IsNaN(Afix), false, false, false }; }
                else
                {
                    // in attesa del NEC: forma di MOXGEN scalata sulla profondita' vincolata
                    double s = Ev / m[4];
                    if (!double.IsNaN(Afix)) A = Afix;
                    B = m[1] * s; Cg = m[2] * s; D = m[3] * s; E = Ev;
                    o.Oks.Add("E vincolata: calcolo NEC di B, C e D in corso (qualche secondo); intanto la forma di MOXGEN scalata sulla profondita'");
                }
                if (man[1] || man[2] || man[3] || (P("A") > 0 && !fae)) o.Warnings.Add("Con E vincolata le caselle B, C e D (e A, se non e' fissa) non si usano: le ricalcola il NEC.");
            }
            Func<int, double, string> sub = (k, v) => man[k] ? "a mano (MOXGEN " + Calcolatori.N(v, 3) + " m)" : "MOXGEN";
            o.Card("A (larghezza)", Calcolatori.N(A, 3) + " m", sub(0, m[0]), true);
            o.Card("B (coda del radiatore)", Calcolatori.N(B, 3) + " m", sub(1, m[1]));
            o.Card("C (spazio tra le code)", Calcolatori.N(Cg, 3) + " m", sub(2, m[2]));
            o.Card("D (coda del riflettore)", Calcolatori.N(D, 3) + " m", sub(3, m[3]));
            if (man[0] || man[1] || man[2] || man[3]) o.Oks.Add("forma modificata a mano: il calcolo in tempo reale e la verifica NEC usano le misure scritte");
            if (2 * B >= A || 2 * D >= A) o.Warnings.Add("Le code sono piu' lunghe di meta' della larghezza A: le punte si toccano o si incrociano.");
            o.Card("E (profondita')", Calcolatori.N(E, 3) + " m", vincolo ? (fae ? "fissa, con A fissa" : "fissa (vincolo)") : "B + C + D", vincolo);
            if (fit != null)
            {
                o.Card("Forma ricalcolata (NEC)", Calcolatori.N(fit[7], 1) + " dB F/B", Calcolatori.N(fit[4], 1) + (fit[5] < 0 ? " -j" : " +j") + Calcolatori.N(Math.Abs(fit[5]), 1) + " Ω, " + Calcolatori.N(fit[6], 2) + " dBi nello spazio libero (MOXGEN " + Calcolatori.N(m[4], 3) + " m di profondita')");
                if (fit[7] < 10) o.Warnings.Add("Con questi vincoli il Moxon non funziona (F/B " + Calcolatori.N(fit[7], 1) + " dB): la profondita' e' troppo diversa da quella di MOXGEN (" + Calcolatori.N(m[4], 3) + " m)" + (fae ? "; provare a liberare A." : "."));
            }
            o.Formula = "MOXGEN (Cebik): x = log10(d/lambda); A = -0,000857 x^2 - 0,00957 x + 0,33986; B = -0,00214 x^2 - 0,02036 x + 0,00829; C = 0,00181 x^2 + 0,01781 x + 0,05164; " +
                "D = 0,001 x + 0,07179; E = B + C + D (in lambda, valide per x tra -5 e -2). Verifica DK7ZB 10 m: A 3,825 contro 3,86 m.";
            if (m[5] < -5 || m[5] > -2) o.Warnings.Add("Diametro fuori dall'intervallo delle formule (log10(d/lambda) tra -5 e -2).");
            else o.Oks.Add("diametro nell'intervallo delle formule MOXGEN");
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 60, z = h;
            int td = d.Ws(seg, 0, -A / 2, z, 0, A / 2, z, a, true, 21);
            int nd = d.LastN;
            d.Ws(seg, 0, A / 2, z, -B, A / 2, z, a, false, 3);
            d.Ws(seg, 0, -A / 2, z, -B, -A / 2, z, a, false, 3);
            d.Ws(seg, -E, -A / 2, z, -E, A / 2, z, a, true, 21);
            d.Ws(seg, -E, A / 2, z, -E + D, A / 2, z, a, false, 3);
            d.Ws(seg, -E, -A / 2, z, -E + D, -A / 2, z, a, false, 3);
            d.Ground(h > 0 ? 2 : 0);
            d.Ex(td, (nd + 1) / 2);
            o.Nec = d.End(f);
            o.Plane = "xy";
            o.HideGround = true;
            // 0.4.8 (IC8TEM 13/11/2026 23:35): la quota A stava sul lato del radiatore, sopra la scritta del lobo: ora sul lato del riflettore
            o.Mark(7, -E - lam * 0.03, -A / 2, "A " + Calcolatori.N(A, 2), 3, -E - lam * 0.03, A / 2);
            o.Mark(7, -E, A / 2 + lam * 0.03, "E " + Calcolatori.N(E, 2), 3, 0, A / 2 + lam * 0.03);
            o.Mark(0, lam * 0.02, -A * 0.12, "lobo ->", 1);
            o.Chart = new CalcChart { Title = "Larghezza A al variare del diametro del filo", XLabel = "mm", YLabel = "m" };
            o.Chart.X = Calcolatori.Lin(0.5, 30, 40); o.Chart.Y = new double[40];
            for (int i = 0; i < 40; i++) o.Chart.Y[i] = Dimensioni(f, o.Chart.X[i])[0];
            o.Chart.MarkX = dmm; o.Chart.MarkText = Calcolatori.N(A, 2) + " m";
            return o;
        }
    }

    // =============================================================================================
    // LOOP
    // =============================================================================================
    public class CalcLoopRett : SimpleCalc
    {
        public CalcLoopRett()
        {
            Key = "looprett"; Title = "Loop rettangolare"; Source = "ARRL Antenna Book (perimetro 1005/f piedi = 306,3/f m)";
            Params.Add(new CalcParam("f", "Frequenza", 14.2, "MHz"));
            Params.Add(CalcParam.Scelta("r", "Forma (altezza : larghezza)", 0, null, "quadrato", "2 : 1 (alto)", "1 : 2 (largo)"));
            Params.Add(CalcParam.Scelta("ali", "Alimentazione", 0, null, "centro in basso", "centro di un lato"));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(new CalcParam("h", "Quota del lato basso (0 = spazio libero)", 0, "m"));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, h = P("h"), lam = C0 / f;
            int r = I("r");
            double ratio = r == 0 ? 1 : (r == 1 ? 2 : 0.5);   // altezza / larghezza
            double per = 306.3 / f * Scale, w = per / (2 * (1 + ratio)), hh = w * ratio;
            bool lato = I("ali") == 1;
            o.Card("Perimetro", Calcolatori.N(per, 2) + " m", "306,3 / f", true);
            o.Card("Larghezza", Calcolatori.N(w, 2) + " m", "");
            o.Card("Altezza", Calcolatori.N(hh, 2) + " m", "");
            o.Card("Polarizzazione", lato ? "verticale" : "orizzontale", lato ? "alimentata su un lato" : "alimentata in basso");
            o.Card("Impedenza tipica", ratio > 1 ? "circa 75-100 Ω" : (ratio < 1 ? "oltre 150 Ω" : "circa 120 Ω"), "il NEC la calcola");
            o.Formula = "Formule: perimetro 306,3/f m (ARRL); a parita' di perimetro il loop piu' alto che largo abbassa l'impedenza; la polarizzazione segue il lato alimentato.";
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 50, z0 = h > 0 ? h : 0;
            int tb = d.Ws(seg, 0, -w / 2, z0, 0, w / 2, z0, a, true, 5);
            int nb = d.LastN;
            int tr = d.Ws(seg, 0, w / 2, z0, 0, w / 2, z0 + hh, a, true, 5);
            int nr = d.LastN;
            d.Ws(seg, 0, w / 2, z0 + hh, 0, -w / 2, z0 + hh, a, true, 5);
            d.Ws(seg, 0, -w / 2, z0 + hh, 0, -w / 2, z0, a, true, 5);
            d.Ground(h > 0 ? 2 : 0);
            if (lato) d.Ex(tr, (nr + 1) / 2); else d.Ex(tb, (nb + 1) / 2);
            o.Nec = d.End(f);
            o.Plane = "yz";
            return o;
        }
    }

    public class CalcHen : SimpleCalc
    {
        public CalcHen()
        {
            Key = "hen"; Title = "HEN (Hentenna)"; Source = "hamuniverse.com, \"Hentenna\" (JA): lati lambda/2, larghezza lambda/6, alimentazione a lambda/10 dal basso, 2,5-3 dBd; Y. Someya JE1DEU (1972, Sagami club, e.as76.net)";
            Params.Add(new CalcParam("f", "Frequenza", 50.15, "MHz"));
            Params.Add(new CalcParam("d", "Diametro", 4, "mm"));
            Params.Add(new CalcParam("p", "Barra di alimentazione dal basso", 0.1, "lambda"));
            Params.Add(new CalcParam("h", "Quota del lato basso (0 = spazio libero)", 0, "m"));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, h = P("h"), lam = C0 / f;
            double H = lam / 2 * Scale, W = lam / 6 * Scale, pf = P("p") * lam;
            o.Card("Lati verticali", Calcolatori.N(H, 3) + " m", "lambda/2", true);
            o.Card("Larghezza", Calcolatori.N(W, 3) + " m", "lambda/6");
            o.Card("Barra di alimentazione", Calcolatori.N(pf, 3) + " m dal basso", Calcolatori.N(P("p"), 2) + " lambda");
            o.Card("Guadagno dichiarato", "2,5-3 dBd", "polarizzazione verticale");
            o.Formula = "Formule (hamuniverse, Hentenna di origine giapponese): lati lambda/2, alto e basso lambda/6, barra di alimentazione a circa lambda/10 dal basso; " +
                "si regola spostando insieme i due punti della barra. Le misure \"vengono un po' lunghe\": il NEC lo verifica.";
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 50, z0 = h;
            d.Ws(seg, 0, -W / 2, z0, 0, W / 2, z0, a, false, 3);
            d.Ws(seg, 0, W / 2, z0, 0, W / 2, z0 + pf, a, false, 2);
            d.Ws(seg, 0, W / 2, z0 + pf, 0, W / 2, z0 + H, a, false, 5);
            d.Ws(seg, 0, W / 2, z0 + H, 0, -W / 2, z0 + H, a, false, 3);
            d.Ws(seg, 0, -W / 2, z0 + H, 0, -W / 2, z0 + pf, a, false, 5);
            d.Ws(seg, 0, -W / 2, z0 + pf, 0, -W / 2, z0, a, false, 2);
            int tf = d.Ws(seg, 0, -W / 2, z0 + pf, 0, W / 2, z0 + pf, a, true, 3);
            int nf = d.LastN;
            d.Ground(h > 0 ? 2 : 0);
            d.Ex(tf, (nf + 1) / 2);
            o.Nec = d.End(f);
            o.Plane = "yz";
            return o;
        }
    }

    public class CalcDiscone : SimpleCalc
    {
        public CalcDiscone()
        {
            Key = "discone"; Title = "Discone"; Source = "ARRL Antenna Book / Kandoian: cono lambda/4 alla frequenza minima, disco 0,7 x lambda/4, angolo 25-40 gradi";
            Params.Add(new CalcParam("f", "Frequenza minima", 50, "MHz"));
            Params.Add(new CalcParam("ang", "Semiapertura del cono", 30, "gradi"));
            Params.Add(new CalcParam("n", "Fili del cono e del disco", 8, ""));
            Params.Add(new CalcParam("d", "Diametro dei fili", 3, "mm"));
            CanTune = false;
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, lam = C0 / f, th = Math.Max(15, Math.Min(45, P("ang"))) * Math.PI / 180;
            int nw = Math.Max(4, Math.Min(16, I("n")));
            double sl = lam / 4, disc = 0.7 * lam / 4, top = 0.02 * lam, gap = top * 0.3;
            o.Card("Lato del cono", Calcolatori.N(sl, 3) + " m", "lambda/4 alla frequenza minima", true);
            o.Card("Diametro del disco", Calcolatori.N(disc, 3) + " m", "0,7 x lambda/4");
            o.Card("Cima del cono", Calcolatori.N(top * 100, 1) + " cm", "spazio disco-cono " + Calcolatori.N(gap * 100, 1) + " cm");
            o.Card("Banda", Calcolatori.N(f, 0) + "-" + Calcolatori.N(10 * f, 0) + " MHz", "circa 1 : 10 con ROS sotto 2");
            o.Formula = "Formule: lato del cono lambda/4 alla frequenza piu' bassa, disco 0,7 lambda/4, semiapertura 25-40 gradi (piu' larga = banda piu' ampia), spazio tra disco e cono circa 0,3 volte il diametro della cima.";
            NecDeck d = new NecDeck(Title + " da " + Calcolatori.N(f, 1) + " MHz");
            double seg = lam / 40, zf = sl * Math.Cos(th) + 0.1;
            int tf = d.W(1, 0, 0, zf, 0, 0, zf + gap, a);
            for (int i = 0; i < nw; i++)
            {
                double ph = 2 * Math.PI * i / nw;
                d.Ws(seg, 0, 0, zf + gap, disc / 2 * Math.Cos(ph), disc / 2 * Math.Sin(ph), zf + gap, a, false, 3);
                d.Ws(seg, 0, 0, zf, sl * Math.Sin(th) * Math.Cos(ph), sl * Math.Sin(th) * Math.Sin(ph), zf - sl * Math.Cos(th), a, false, 5);
            }
            d.Ground(0);
            d.Ex(tf, 1);
            o.Nec = d.End(f * 1.1);
            o.Plane = "xz";
            o.Warnings.Add("Il modello NEC a fili e' calcolato a " + Calcolatori.N(f * 1.1, 1) + " MHz (poco sopra la minima): per la banda intera usa lo sweep della linguetta Frequenza.");
            return o;
        }
    }

    // =============================================================================================
    // STRUMENTI
    // =============================================================================================
    public class CalcRidimensiona : SimpleCalc
    {
        public CalcRidimensiona()
        {
            Key = "ridim"; Title = "Ridimensiona antenna"; Source = "scala delle misure: k = f vecchia / f nuova (scheda GS del NEC)";
            Params.Add(new CalcParam("f1", "Frequenza del modello", 144.3, "MHz"));
            Params.Add(new CalcParam("f2", "Nuova frequenza", 50.15, "MHz"));
            Params.Add(CalcParam.Scelta("r", "Diametri", 0, null, "in scala", "uguali"));
            UsesModel = true; CanTune = false;
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double k = P("f1") / Math.Max(1e-6, P("f2"));
            o.Card("Fattore di scala", Calcolatori.N(k, 4), "f vecchia / f nuova", true);
            o.Card("Modello", Model == null ? "nessuno aperto" : (string.IsNullOrEmpty(Model.FileName) ? "modello aperto" : Model.FileName), Model == null ? "" : Model.Geometry.Count + " schede di geometria");
            o.Formula = "Il modello aperto viene copiato con tutte le misure moltiplicate per k (scheda GS): con \"diametri uguali\" i raggi restano quelli di prima. " +
                "Bobine e condensatori dei carichi vanno ricalcolati (L e C scalano con k).";
            if (Model == null) o.Warnings.Add("Apri prima un modello: lo strumento lavora sul modello aperto.");
            else
            {
                NecModel m = Scala(Model, k, I("r") == 0);
                m.FreqMHz = P("f2");
                o.Nec = m.ToNec();
                if (Model.Loads.Count > 0) o.Warnings.Add("Il modello ha " + Model.Loads.Count + " carichi: i valori di L e C non sono stati scalati.");
            }
            return o;
        }

        public static NecModel Scala(NecModel src, double k, bool raggi)
        {
            NecModel m = NecModel.Parse(src.ToNec());
            foreach (GeomCard c in m.Geometry)
            {
                c.Src = null;
                if (c.Id == "GW")
                {
                    c.F1 *= k; c.F2 *= k; c.F3 *= k; c.F4 *= k; c.F5 *= k; c.F6 *= k;
                    if (raggi) { c.F7 *= k; c.GcRad1 *= k; c.GcRad2 *= k; }
                }
                else if (c.Id == "GA") { c.F1 *= k; if (raggi) c.F4 *= k; }
                else if (c.Id == "GH") { c.F1 *= k; c.F2 *= k; c.F3 *= k; c.F4 *= k; c.F5 *= k; c.F6 *= k; if (raggi) c.F7 *= k; }
                else if (c.Id == "GM") { c.F4 *= k; c.F5 *= k; c.F6 *= k; }
            }
            m.Variables.Clear();
            m.FileName = string.IsNullOrEmpty(src.FileName) ? "scalato.nec" : System.IO.Path.GetFileNameWithoutExtension(src.FileName) + " scalato.nec";
            return m;
        }
    }

    public class CalcReteLC : SimpleCalc
    {
        public CalcReteLC()
        {
            Key = "retelc"; Title = "Rete LC di adattamento"; Source = "ARRL Antenna Book / Handbook (reti L, Pi e T senza perdite)";
            Params.Add(CalcParam.Scelta("tipo", "Rete", 0, null, "L", "Pi", "T"));
            Params.Add(new CalcParam("f", "Frequenza", 7.1, "MHz"));
            Params.Add(new CalcParam("rl", "Carico: resistenza", 12, "Ω"));
            Params.Add(new CalcParam("xl", "Carico: reattanza", -30, "Ω"));
            Params.Add(new CalcParam("r0", "Impedenza del trasmettitore", 50, "Ω"));
            Params.Add(new CalcParam("q", "Q della rete (Pi e T)", 3, ""));
            CanTune = false;
        }
        static Complex Pa2(Complex a, Complex b) { return a * b / (a + b); }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            int tipo = I("tipo");
            double f = P("f"), w = 2 * Math.PI * f * 1e6, rl = Math.Max(0.01, P("rl")), xl = P("xl"), r0 = Math.Max(0.01, P("r0")), Q = Math.Max(0.5, P("q"));
            Complex zl = new Complex(rl, xl);
            Func<double, string> comp = x => x >= 0 ? "L " + Calcolatori.Henry(x / w) : "C " + Calcolatori.Farad(-1 / (w * x));
            Complex zin;
            o.HideWires = true; o.Plane = "xy";
            if (tipo == 0)
            {
                // L: se il carico (in parallelo) e' maggiore di r0 il parallelo va dal lato del carico, altrimenti dal lato del trasmettitore
                double gl = rl / (rl * rl + xl * xl);
                if (rl < r0)
                {
                    double q = Math.Sqrt(r0 / rl - 1);
                    double xs = q * rl - xl, xp = -r0 / q;   // serie verso il carico, parallelo verso il trasmettitore (passa-basso)
                    zin = Pa2(new Complex(0, xp), zl + new Complex(0, xs));
                    o.Card("Serie (lato antenna)", comp(xs), "X = " + Calcolatori.N(xs, 1) + " Ω", true);
                    o.Card("Parallelo (lato TX)", comp(xp), "X = " + Calcolatori.N(xp, 1) + " Ω", true);
                    o.Card("Q della rete", Calcolatori.N(q, 2), "fissato da r0/R");
                    Schema(o, comp(xp), comp(xs), false);
                }
                else
                {
                    double rp = 1 / gl, bp = -xl / (rl * rl + xl * xl);
                    double q = Math.Sqrt(rp / r0 - 1);
                    double xp2 = rp / q;                         // parallelo totale voluto lato carico
                    double bneed = 1 / xp2 - bp;                 // suscettanza da aggiungere (passa-basso: condensatore)
                    double xpar = -1 / bneed, xs = q * r0;
                    zin = new Complex(0, xs) + Pa2(new Complex(0, xpar), zl);
                    o.Card("Parallelo (lato antenna)", comp(xpar), "X = " + Calcolatori.N(xpar, 1) + " Ω", true);
                    o.Card("Serie (lato TX)", comp(xs), "X = " + Calcolatori.N(xs, 1) + " Ω", true);
                    o.Card("Q della rete", Calcolatori.N(q, 2), "fissato da R/r0");
                    Schema(o, comp(xs), comp(xpar), true);
                }
            }
            else
            {
                // Pi e T con il Q scelto (la reattanza del carico si assorbe nell'elemento dal lato del carico)
                if (tipo == 1)
                {
                    double rp = (rl * rl + xl * xl) / rl, xpl = (rl * rl + xl * xl) / (xl == 0 ? 1e-30 : xl);
                    double rv = Math.Max(rp, r0) / (Q * Q + 1);
                    double q1 = Math.Sqrt(Math.Max(0, r0 / rv - 1)), q2 = Math.Sqrt(Math.Max(0, rp / rv - 1));
                    double x1 = -r0 / q1, x2tot = -rp / q2;
                    double x2 = xl == 0 ? x2tot : 1 / (1 / x2tot - 1 / xpl);
                    double xs = rv * (q1 + q2);
                    zin = Pa2(new Complex(0, x1), new Complex(0, xs) + Pa2(new Complex(0, x2), zl));
                    o.Card("C lato TX", comp(x1), "X = " + Calcolatori.N(x1, 1) + " Ω", true);
                    o.Card("L in serie", comp(xs), "X = " + Calcolatori.N(xs, 1) + " Ω", true);
                    o.Card("C lato antenna", comp(x2), "X = " + Calcolatori.N(x2, 1) + " Ω", true);
                    Schema(o, comp(x1), comp(xs), false, comp(x2));
                }
                else
                {
                    double rv = Math.Min(rl, r0) * (Q * Q + 1);
                    double q1 = Math.Sqrt(Math.Max(0, rv / r0 - 1)), q2 = Math.Sqrt(Math.Max(0, rv / rl - 1));
                    double x1 = q1 * r0, x2 = q2 * rl - xl, xp = -rv / (q1 + q2);
                    zin = new Complex(0, x1) + Pa2(new Complex(0, xp), new Complex(0, x2) + zl);
                    o.Card("L lato TX", comp(x1), "X = " + Calcolatori.N(x1, 1) + " Ω", true);
                    o.Card("C in parallelo", comp(xp), "X = " + Calcolatori.N(xp, 1) + " Ω", true);
                    o.Card("L lato antenna", comp(x2), "X = " + Calcolatori.N(x2, 1) + " Ω", true);
                    Schema(o, comp(x1), comp(xp), true, comp(x2));
                }
            }
            double g = Complex.Abs((zin - r0) / (zin + r0));
            o.Card("Verifica: Z vista dal TX", Calcolatori.Cx(zin, 2), "ROS " + Calcolatori.N((1 + g) / Math.Max(1e-9, 1 - g), 3));
            if (g < 0.01) o.Oks.Add("la rete calcolata riporta il carico a " + Calcolatori.N(r0, 0) + " Ω (verifica con i numeri complessi)");
            else o.Warnings.Add("La verifica non torna (ROS " + Calcolatori.N((1 + g) / Math.Max(1e-9, 1 - g), 2) + "): per Pi e T alza il Q sopra il minimo richiesto da R.");
            o.Formula = "Formule: rete L con Q = sqrt(Rmaggiore/Rminore - 1), Xserie = Q Rminore, Xparallelo = Rmaggiore/Q; Pi con resistenza virtuale Rv = Rmaggiore/(Q^2 + 1); " +
                "T con Rv = Rminore (Q^2 + 1). La reattanza del carico si assorbe nell'elemento accanto. La verifica rifa' il conto con i numeri complessi.";
            return o;
        }

        // schema a componenti: TX a sinistra, antenna a destra
        static void Schema(CalcOutput o, string a, string b, bool serieAlTx, string c = null)
        {
            o.Mark(0, -0.2, 0.5, "TX", 3);
            o.Mark(0, 3.1, 0.5, "antenna", 3);
            o.Mark(2, 0, 0, null, 3, 3, 0);
            o.Mark(2, 0, 1, null, 3, 0.4, 1);
            if (c == null)
            {
                if (serieAlTx) { o.Mark(a.StartsWith("L") ? 6 : 3, 0.4, 1, a, 2, 1.6, 1); o.Mark(2, 1.6, 1, null, 3, 3, 1); o.Mark(b.StartsWith("L") ? 6 : 3, 2.2, 1, b, 2, 2.2, 0); }
                else { o.Mark(a.StartsWith("L") ? 6 : 3, 0.9, 1, a, 2, 0.9, 0); o.Mark(2, 0.4, 1, null, 3, 1.4, 1); o.Mark(b.StartsWith("L") ? 6 : 3, 1.4, 1, b, 2, 2.6, 1); o.Mark(2, 2.6, 1, null, 3, 3, 1); }
            }
            else
            {
                if (!serieAlTx)
                {
                    o.Mark(a.StartsWith("L") ? 6 : 3, 0.8, 1, a, 2, 0.8, 0);
                    o.Mark(2, 0.4, 1, null, 3, 1.1, 1);
                    o.Mark(b.StartsWith("L") ? 6 : 3, 1.1, 1, b, 2, 2.0, 1);
                    o.Mark(2, 2.0, 1, null, 3, 3, 1);
                    o.Mark(c.StartsWith("L") ? 6 : 3, 2.4, 1, c, 2, 2.4, 0);
                }
                else
                {
                    o.Mark(a.StartsWith("L") ? 6 : 3, 0.4, 1, a, 2, 1.3, 1);
                    o.Mark(b.StartsWith("L") ? 6 : 3, 1.5, 1, b, 2, 1.5, 0);
                    o.Mark(2, 1.3, 1, null, 3, 1.7, 1);
                    o.Mark(c.StartsWith("L") ? 6 : 3, 1.7, 1, c, 2, 2.6, 1);
                    o.Mark(2, 2.6, 1, null, 3, 3, 1);
                }
            }
        }
    }

    public class CalcRr : SimpleCalc
    {
        public CalcRr()
        {
            Key = "rrad"; Title = "Resistenza di radiazione"; Source = "Kraus, Balanis: dipolo corto 20 pi^2 (l/lambda)^2, monopolo corto 40 pi^2 (h/lambda)^2, loop piccolo 31171 (N A/lambda^2)^2";
            Params.Add(CalcParam.Scelta("tipo", "Antenna", 0, null, "dipolo corto", "monopolo corto", "loop piccolo"));
            Params.Add(new CalcParam("f", "Frequenza", 3.6, "MHz"));
            Params.Add(new CalcParam("l", "Lunghezza / altezza / diametro", 10, "m"));
            Params.Add(new CalcParam("n", "Spire (loop)", 1, ""));
            Params.Add(new CalcParam("rp", "Resistenza di perdita", 2, "Ω"));
            CanTune = false;
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            int tipo = I("tipo");
            double f = P("f"), l = P("l"), lam = C0 / f, rp = Math.Max(0, P("rp")), N = Math.Max(1, P("n"));
            double rr;
            if (tipo == 0) rr = 20 * Math.PI * Math.PI * Math.Pow(l / lam, 2);
            else if (tipo == 1) rr = 40 * Math.PI * Math.PI * Math.Pow(l / lam, 2);
            else { double A = Math.PI * l * l / 4; rr = 31171 * Math.Pow(N * A / (lam * lam), 2); }
            double eff = rr / (rr + rp);
            o.Card("Resistenza di radiazione", Calcolatori.Ohm(rr), Calcolatori.N(l / lam, 4) + " lambda", true);
            o.Card("Efficienza", Calcolatori.N(eff * 100, 1) + " %", Calcolatori.N(10 * Math.Log10(eff), 1) + " dB con " + Calcolatori.Ohm(rp) + " di perdita", true);
            o.Formula = "Formule (corrente triangolare per dipolo e monopolo corti, uniforme nel loop): dipolo Rr = 20 pi^2 (l/lambda)^2; monopolo Rr = 40 pi^2 (h/lambda)^2 = 395 (h/lambda)^2; " +
                "loop Rr = 31171 (N A / lambda^2)^2 con A = pi D^2/4. Valide per l, h e la circonferenza molto piu' piccole di lambda.";
            bool fuori = tipo == 2 ? Math.PI * l > lam / 4 : (tipo == 0 ? l > lam / 5 : l > lam / 10);
            if (fuori) o.Warnings.Add("Antenna non piu' \"corta\": la formula sottostima o sovrastima, usa il NEC.");
            else o.Oks.Add("antenna corta rispetto a lambda: formula valida");
            o.Chart = new CalcChart { Title = "Resistenza di radiazione al variare della misura", XLabel = "m", YLabel = "Ω" };
            o.Chart.X = Calcolatori.Lin(l * 0.25, l * 2, 30); o.Chart.Y = new double[30];
            for (int i = 0; i < 30; i++)
            {
                double x = o.Chart.X[i];
                o.Chart.Y[i] = tipo == 0 ? 20 * Math.PI * Math.PI * Math.Pow(x / lam, 2) : tipo == 1 ? 40 * Math.PI * Math.PI * Math.Pow(x / lam, 2) : 31171 * Math.Pow(N * Math.PI * x * x / 4 / (lam * lam), 2);
            }
            o.Chart.MarkX = l; o.Chart.MarkText = Calcolatori.Ohm(rr);
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            if (tipo == 2)
            {
                double R = l / 2;
                int first = 0;
                for (int k = 0; k < 16; k++)
                {
                    double a1 = 2 * Math.PI * k / 16, a2 = 2 * Math.PI * (k + 1) / 16;
                    int t = d.W(1, 0, R * Math.Cos(a1), R * Math.Sin(a1), 0, R * Math.Cos(a2), R * Math.Sin(a2), 0.005);
                    if (k == 0) first = t;
                }
                d.Ground(0); d.Ex(first, 1);
            }
            else
            {
                int t = d.W(21, 0, 0, tipo == 1 ? 0 : -l / 2, 0, 0, tipo == 1 ? l : l / 2, 0.005);
                d.Ground(tipo == 1 ? 1 : 0, tipo == 1);
                d.Ex(t, tipo == 1 ? 1 : 11);
            }
            o.Nec = d.End(f);
            return o;
        }
    }

    public class CalcCavi : SimpleCalc
    {
        public static List<Cable> Lista = new List<Cable>();
        public CalcCavi()
        {
            Key = "cavi"; Title = "Cavi RG"; Source = "abaco dei cavi di YA-NEC (data/cavi.csv, perdite da catalogo); trasformazione dell'impedenza lungo la linea con perdite";
            Params.Add(new CalcParam("c", "Cavo", 0, "") { Choices = new string[0] });
            Params.Add(new CalcParam("f", "Frequenza", 144.3, "MHz"));
            Params.Add(new CalcParam("l", "Lunghezza", 20, "m"));
            Params.Add(new CalcParam("rl", "Carico: resistenza", 50, "Ω"));
            Params.Add(new CalcParam("xl", "Carico: reattanza", 0, "Ω"));
            CanTune = false;
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            CalcParam pc = Par("c");
            string[] nomi = new string[Lista.Count];
            for (int i = 0; i < Lista.Count; i++) nomi[i] = Lista[i].Name;
            pc.Choices = nomi;
            if (Lista.Count == 0) { o.Warnings.Add("Abaco dei cavi non trovato (data/cavi.csv)."); return o; }
            Cable c = Lista[Math.Max(0, Math.Min(Lista.Count - 1, I("c")))];
            double f = P("f"), len = P("l");
            Complex zl = new Complex(P("rl"), P("xl"));
            double att = c.AttenuationAt(f);
            Complex zin = LineCalc.InputImpedance(zl, c.Z0, f, c.Vf, att, len);
            double eff = LineCalc.PowerEfficiency(zl, c.Z0, f, c.Vf, att, len);
            o.Card("Perdita (adattato)", Calcolatori.N(att * len / 100, 2) + " dB", Calcolatori.N(att, 2) + " dB/100 m", true);
            o.Card("Efficienza con questo carico", Calcolatori.N(eff * 100, 1) + " %", Calcolatori.N(-10 * Math.Log10(Math.Max(1e-9, eff)), 2) + " dB");
            o.Card("Z all'ingresso", Calcolatori.Cx(zin, 1), "ROS al TX " + Calcolatori.N(LineCalc.Swr(zin, c.Z0), 2));
            o.Card("Lunghezza elettrica", Calcolatori.N(len / LineCalc.Wavelength(f, c.Vf), 3) + " lambda", "VF " + Calcolatori.N(c.Vf, 2) + ", lambda nel cavo " + Calcolatori.N(LineCalc.Wavelength(f, c.Vf), 3) + " m");
            o.Card("Cavo", c.Z0 + " Ω" + (c.OdMm > 0 ? ", ø " + Calcolatori.N(c.OdMm, 1) + " mm" : ", linea bifilare"), c.Dielectric + " (" + c.Source + ")");
            o.Formula = "Formule: perdita interpolata in scala logaritmica tra 50, 144, 432 e 1296 MHz; Zin = Z0 (ZL + Z0 tanh(gamma l)) / (Z0 + ZL tanh(gamma l)); efficienza = potenza al carico / potenza in ingresso.";
            o.Chart = new CalcChart { Title = "Perdita di " + Calcolatori.N(len, 0) + " m di " + c.Name + " al variare della frequenza", XLabel = "MHz", YLabel = "dB" };
            o.Chart.X = Calcolatori.Lin(10, 1300, 60); o.Chart.Y = new double[60];
            for (int i = 0; i < 60; i++) o.Chart.Y[i] = c.AttenuationAt(o.Chart.X[i]) * len / 100;
            o.Chart.MarkX = f; o.Chart.MarkText = Calcolatori.N(att * len / 100, 2) + " dB";
            o.HideWires = true;
            return o;
        }
    }

    public class CalcToroidiT : SimpleCalc
    {
        static readonly string[] Taglie = { "T-37", "T-50", "T-68", "T-80", "T-106", "T-130", "T-157", "T-200" };
        static readonly double[][] AL =   // uH per 100 spire (Amidon, ok1dxd)
        {
            new double[] { 40, 49, 57, 55, 135, 110, 140, 120 },     // -2 rosso
            new double[] { 30, 46, 47, 45, 116, 96, 115, 100 },      // -6 giallo
            new double[] { 25, 31, 32, 32, double.NaN, double.NaN, double.NaN, double.NaN },   // -10 nero
        };
        static readonly string[] Mix = { "-2 (rosso) 0,25-10 MHz", "-6 (giallo) 3-40 MHz", "-10 (nero) 10-100 MHz" };
        public CalcToroidiT()
        {
            Key = "toroidiT"; Title = "Toroidi iron powder (T)"; Source = "Amidon (AL in uH/100 spire, tabella di ok1dxd): N = 100 sqrt(L/AL)";
            Params.Add(CalcParam.Scelta("m", "Miscela", 0, null, "-2 rosso", "-6 giallo", "-10 nero"));
            Params.Add(new CalcParam("t", "Nucleo", 2, "") { Choices = Taglie });
            Params.Add(new CalcParam("l", "Induttanza voluta", 2.45, "µH"));
            Params.Add(new CalcParam("f", "Frequenza di lavoro", 7.1, "MHz"));
            CanTune = false;
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            int m = Math.Max(0, Math.Min(2, I("m"))), t = Math.Max(0, Math.Min(Taglie.Length - 1, I("t")));
            double al = AL[m][t], L = P("l"), f = P("f");
            o.HideWires = true;
            if (double.IsNaN(al)) { o.Warnings.Add("Il nucleo " + Taglie[t] + " non esiste nella miscela -10 della tabella: scegli T-37...T-80."); return o; }
            double N = 100 * Math.Sqrt(L / al);
            int Nr = (int)Math.Round(N);
            double Lr = al * Nr * Nr / 10000;
            o.Card("Spire", Nr.ToString(), "esatte " + Calcolatori.N(N, 1), true);
            o.Card("Induttanza con " + Nr + " spire", Calcolatori.N(Lr, 2) + " µH", "AL " + Calcolatori.N(al, 0) + " µH/100 spire");
            o.Card("Reattanza", Calcolatori.N(2 * Math.PI * f * Lr, 0) + " Ω", "a " + Calcolatori.N(f, 2) + " MHz");
            o.Card("Miscela", Mix[m], "");
            o.Formula = "Formule: L (uH) = AL N^2 / 10000; N = 100 sqrt(L/AL); AL da Amidon (tolleranza circa 5 %). Le spire si contano come passaggi nel foro.";
            if (f < new[] { 0.25, 3, 10 }[m] || f > new[] { 10, 40, 100 }[m]) o.Warnings.Add("Frequenza fuori dalla banda consigliata per la miscela " + Mix[m] + ".");
            o.Chart = new CalcChart { Title = "Induttanza al variare delle spire (" + Taglie[t] + Mix[m].Substring(0, 3) + ")", XLabel = "spire", YLabel = "µH" };
            o.Chart.X = Calcolatori.Lin(1, Math.Max(10, 2 * N), 40); o.Chart.Y = new double[40];
            for (int i = 0; i < 40; i++) o.Chart.Y[i] = al * o.Chart.X[i] * o.Chart.X[i] / 10000;
            o.Chart.MarkX = Nr; o.Chart.MarkText = Calcolatori.N(Lr, 2) + " µH";
            return o;
        }
    }

    public class CalcToroidiFT : SimpleCalc
    {
        static readonly string[] Taglie = { "FT-37", "FT-50", "FT-82", "FT-114", "FT-140", "FT-240" };
        static readonly double[][] AL =   // mH per 1000 spire = nH/spira^2 (amidoncorp.com, +-25 %)
        {
            new double[] { 350, 440, 470, 510, 885, 1075 },   // 43
            new double[] { 55, 69, 75, 80, 140, 170 },         // 61
        };
        public CalcToroidiFT()
        {
            Key = "toroidiFT"; Title = "Toroidi in ferrite (FT)"; Source = "Amidon (amidoncorp.com: AL in mH/1000 spire, +-25 %): N = 1000 sqrt(L/AL)";
            Params.Add(CalcParam.Scelta("m", "Miscela", 0, null, "43 (1-50 MHz, choke)", "61 (trasformatori fino a 100 MHz)"));
            Params.Add(new CalcParam("t", "Nucleo", 5, "") { Choices = Taglie });
            Params.Add(new CalcParam("n", "Spire", 12, ""));
            Params.Add(new CalcParam("f", "Frequenza", 7.1, "MHz"));
            CanTune = false;
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            int m = Math.Max(0, Math.Min(1, I("m"))), t = Math.Max(0, Math.Min(Taglie.Length - 1, I("t")));
            double al = AL[m][t], N = Math.Max(1, P("n")), f = P("f");
            double L = al * N * N * 1e-9;          // H
            double xl = 2 * Math.PI * f * 1e6 * L;
            o.Card("Induttanza", Calcolatori.Henry(L), "AL " + al + " nH/spira² (" + Taglie[t] + "-" + (m == 0 ? "43" : "61") + ")", true);
            o.Card("Reattanza a " + Calcolatori.N(f, 1) + " MHz", Calcolatori.Ohm(xl), "valore di bassa frequenza (u')");
            o.Card("Come choke", xl > 1000 ? "buono" : (xl > 500 ? "sufficiente" : "scarso"), "obiettivo: oltre 1000 Ω di modo comune");
            o.Card("Spire per 500 Ω / 1000 Ω", Calcolatori.N(Math.Sqrt(500 / (2 * Math.PI * f * 1e6 * al * 1e-9)), 0) + " / " + Calcolatori.N(Math.Sqrt(1000 / (2 * Math.PI * f * 1e6 * al * 1e-9)), 0), "a " + Calcolatori.N(f, 1) + " MHz");
            o.Formula = "Formule: L = AL N^2 (AL in nH/spira^2 = mH/1000 spire); X = 2 pi f L. Sopra pochi MHz la ferrite 43 diventa resistiva (u''): l'impedenza vera del choke e' dello stesso ordine ma va misurata.";
            o.Warnings.Add("Il valore di AL ha tolleranza +-25 % (Amidon).");
            o.Chart = new CalcChart { Title = "Reattanza al variare delle spire a " + Calcolatori.N(f, 1) + " MHz", XLabel = "spire", YLabel = "Ω" };
            o.Chart.X = Calcolatori.Lin(1, 20, 20); o.Chart.Y = new double[20];
            for (int i = 0; i < 20; i++) o.Chart.Y[i] = 2 * Math.PI * f * 1e6 * al * 1e-9 * o.Chart.X[i] * o.Chart.X[i];
            o.Chart.MarkX = N; o.Chart.MarkText = Calcolatori.Ohm(xl);
            o.HideWires = true;
            return o;
        }
    }

    // CalcBalun: dalla 0.5.3 in Engine/CalcBalun.cs
}
