// YA-NEC 1.5 - calcolatori nuovi del bozzetto "Calcolatori 1.5" (approvato da IC8TEM il 18/11/2026 19:30):
//  - Forcina (beta / hairpin match), proposta y1: Cebik W4RNL, "Antenna Options" n. 1-3 (QEX), Cubical Quad Notes vol. 1;
//  - NVIS, proposte n1 e n2: Cebik W4RNL, "Antenna Options" n. 12-13 (QEX); N6BV, "What's the Deal About NVIS?", QST dic. 2005.
// GPL-3.0.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace YaNec.Engine
{
    /// <summary>Forcina: adattamento a L con il pilotato reso capacitivo (serie) e la forcina induttiva in parallelo al punto di alimentazione.</summary>
    public class CalcForcina : SimpleCalc
    {
        public CalcForcina()
        {
            Key = "forcina"; Title = "Forcina (beta / hairpin match)"; Family = "Strumenti";
            Source = "Cebik W4RNL, Antenna Options n. 1-3 (QEX); Cubical Quad Notes vol. 1";
            Params.Add(new CalcParam("f", "Frequenza", 14.15, "MHz"));
            Params.Add(new CalcParam("r", "R del pilotato", 25, "Ω", "Resistenza del pilotato alla risonanza, dal modello o dal calcolo NEC."));
            Params.Add(new CalcParam("x", "X del pilotato", 0, "Ω", "Reattanza del pilotato adesso: se e' positiva il pilotato va accorciato di piu'."));
            Params.Add(new CalcParam("z0", "Impedenza del cavo", 50, "Ω"));
            Params.Add(new CalcParam("df", "ø dei conduttori della forcina", 6, "mm"));
            Params.Add(new CalcParam("sf", "Interasse della forcina", 50, "mm"));
            Params.Add(new CalcParam("lp", "Lunghezza del pilotato", 10.0, "m", "Serve solo per stimare l'accorciamento; la verifica la fa il NEC."));
            Params.Add(new CalcParam("dp", "ø del pilotato", 25, "mm"));
            CanTune = false;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), r = P("r"), x = P("x"), z0 = P("z0"), df = P("df") / 1000, sf = P("sf") / 1000, lp = P("lp"), dp = P("dp") / 1000;
            double lam = C0 / f, beta = 2 * Math.PI / lam;
            o.Formula = "Formule: Xc = √(R·(Z0 − R)) (reattanza capacitiva del pilotato), XL = Z0·√(R/(Z0 − R)) (forcina in parallelo); " +
                "Z0 della forcina = 120·arccosh(interasse/ø); lunghezza = arctan(XL/Z0f)/β (linea in corto, in aria). Accorciamento stimato con la reattanza del dipolo vicino alla risonanza: " +
                "X ≈ 42,5 − Za·cot(βL/2), Za = 120·(ln(L/ø) − 1). La verifica finale la fa il motore NEC sul modello con la forcina.";
            if (r <= 0 || z0 <= 0 || r >= z0)
            {
                o.Warnings.Add("La forcina funziona solo con R del pilotato minore dell'impedenza del cavo: per R maggiore usa il gamma o un trasformatore.");
                return o;
            }
            if (sf <= df) { o.Warnings.Add("L'interasse della forcina deve essere maggiore del diametro dei conduttori."); return o; }
            double xc = Math.Sqrt(r * (z0 - r)), xl = z0 * Math.Sqrt(r / (z0 - r));
            double z0f = 120 * Acosh(sf / df);
            double lf = Math.Atan(xl / z0f) / beta;
            double za = 120 * (Math.Log(lp / dp) - 1), dx = -xc - x;
            // X(L) ≈ 42,5 − Za·cot(βL/2): vicino a βL/2 = π/2 la pendenza dX/d(βL/2) vale circa Za
            double dl = dx / za / (Math.PI / 2);
            o.Card("Reattanza voluta del pilotato", "−j" + Calcolatori.N(xc, 1) + " Ω", "capacitiva", true);
            o.Card("Reattanza della forcina", "+j" + Calcolatori.N(xl, 1) + " Ω", "induttiva, in parallelo");
            o.Card("Z0 della forcina", Calcolatori.N(z0f, 0) + " Ω", "linea a due fili");
            o.Card("Lunghezza della forcina", Calcolatori.N(lf * 1000, 0) + " mm", Calcolatori.N(lf / lam, 4) + " lambda", true);
            o.Card("Accorciamento del pilotato", Calcolatori.N(dl * 100, 1) + " %", "stima: " + Calcolatori.N(lp * dl * 1000, 0) + " mm in tutto, poi verifica NEC");
            o.Add("R del pilotato", Calcolatori.N(r, 1) + " Ω");
            o.Add("X da aggiungere al pilotato", Calcolatori.N(dx, 1) + " Ω");
            if (lf / lam > 0.2) o.Warnings.Add("Forcina lunga (oltre 0,2 lambda): conviene stringere l'interasse o usare conduttori piu' grossi.");
            if (Math.Abs(dl) > 0.08) o.Warnings.Add("Accorciamento oltre l'8%: la stima lineare non e' affidabile, verifica con il NEC.");
            o.Oks.Add("La forcina e' simmetrica: non sbilancia il diagramma come il gamma. Serve comunque un balun o un choke al punto di alimentazione.");
            return o;
        }

        static double Acosh(double v) { return Math.Log(v + Math.Sqrt(v * v - 1)); }
    }

    /// <summary>NVIS: dipolo, V invertito o loop orizzontale di 1 lambda vicino al suolo, con schermo, 9 fili o un filo riflettore a terra.</summary>
    public class CalcNvis : SimpleCalc
    {
        static readonly string[] TerrNomi = { "medio", "molto povero", "molto buono", "mare" };
        static readonly double[,] Terr = { { 13, 0.005 }, { 3, 0.001 }, { 20, 0.0303 }, { 81, 5 } };
        double hScansione = double.NaN;   // altezza trovata dalla scansione (m)
        string chiaveScansione = "";
        bool inScansione;      // dati per cui vale la scansione: se cambiano, si torna all'altezza scritta
        string Chiave() { return P("tipo") + "|" + P("f") + "|" + P("h") + "|" + P("ter") + "|" + P("sotto") + "|" + P("d"); }

        public CalcNvis()
        {
            Key = "nvis"; Title = "NVIS"; Family = "HF";
            Source = "Cebik W4RNL, Antenna Options n. 12-13 (QEX); N6BV, What's the Deal About NVIS?, QST dic. 2005";
            Params.Add(CalcParam.Scelta("tipo", "Antenna", 0, null, "Dipolo", "V invertito", "Loop orizzontale di 1 λ"));
            Params.Add(new CalcParam("f", "Frequenza", 3.6, "MHz"));
            Params.Add(new CalcParam("h", "Altezza (0 = 0,175 λ)", 0, "m", "Cebik: il massimo verso l'alto si ha a circa 0,175 λ; bene tra 0,125 e 0,225 λ."));
            Params.Add(CalcParam.Scelta("ter", "Terreno", 0, null, TerrNomi));
            Params.Add(CalcParam.Scelta("sotto", "Sotto l'antenna", 0, "Schermo: 1 λ x 1 λ a maglie di 0,1 λ, filo da 1\" (come Cebik); 9 fili: a 0,1 λ, poco piu' lunghi del dipolo; tutto a 0,001 λ dal suolo.",
                "niente", "schermo 1 λ x 1 λ", "9 fili a 0,1 λ", "un filo riflettore"));
            Params.Add(new CalcParam("d", "ø del filo", 2, "mm"));
        }

        public override string TuneLabel { get { return "Altezza ottima (scansione)"; } }

        /// <summary>Scansione dell'altezza tra 0,1 e 0,25 lambda: tiene quella con il guadagno allo zenit piu' alto.</summary>
        public override double Tune(double fmhz)
        {
            double lam = C0 / P("f"), best = double.NegativeInfinity, hb = 0.175 * lam;
            double salvato = hScansione;
            inScansione = true;
            for (double k = 0.10; k <= 0.2501; k += 0.025)
            {
                hScansione = k * lam;
                CalcOutput o = Compute();
                NecModel m = NecModel.Parse(o.Nec);
                SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, fmhz);
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(fmhz, Analysis.EffectiveSources(m, g, fmhz), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fmhz));
                double gz = e.GainLinear(0, 0);
                if (gz > best) { best = gz; hb = hScansione; }
            }
            inScansione = false;
            hScansione = double.IsNaN(hb) ? salvato : hb;
            chiaveScansione = Chiave();
            return 1;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            int tipo = I("tipo"), ter = Math.Max(0, Math.Min(3, I("ter"))), sotto = I("sotto");
            double f = P("f"), lam = C0 / f, a = P("d") / 2000;
            if (!double.IsNaN(hScansione) && chiaveScansione != Chiave() && !inScansione) hScansione = double.NaN;
            double h = !double.IsNaN(hScansione) ? hScansione : (P("h") > 0 ? P("h") : 0.175 * lam);
            double hl = h / lam, seg = lam / 50, hs = 0.001 * lam;
            o.Card("Altezza", Calcolatori.N(h, 1) + " m", Calcolatori.N(hl, 3) + " λ" + (!double.IsNaN(hScansione) ? " (scansione NEC)" : ""), true);
            o.Card("Zona buona", Calcolatori.N(0.125 * lam, 1) + " – " + Calcolatori.N(0.225 * lam, 1) + " m", "0,125 – 0,225 λ (Cebik)");
            NecDeck d = new NecDeck(Title + " " + Params[0].Choices[tipo] + " " + Calcolatori.N(f, 3) + " MHz, " + TerrNomi[ter]);
            double L;
            int feedTag, feedSeg;
            if (tipo == 2)
            {
                // loop quadrato di 1 lambda (perimetro ARRL 306,3/f), alimentato al centro di un lato
                L = 306.3 / f; double s = L / 4;
                feedTag = d.Ws(seg, -s / 2, -s / 2, h, s / 2, -s / 2, h, a, true, 5); feedSeg = (d.LastN + 1) / 2;
                d.Ws(seg, s / 2, -s / 2, h, s / 2, s / 2, h, a, false, 5);
                d.Ws(seg, s / 2, s / 2, h, -s / 2, s / 2, h, a, false, 5);
                d.Ws(seg, -s / 2, s / 2, h, -s / 2, -s / 2, h, a, false, 5);
                o.Card("Loop", Calcolatori.N(L, 2) + " m", "lato " + Calcolatori.N(s, 2) + " m");
            }
            else if (tipo == 1)
            {
                // V invertito a 90 gradi tra i bracci, estremi a meta' altezza (non sotto 0,05 lambda)
                L = 142.6 / f * 1.03; double br = L / 2, hEst = Math.Max(0.05 * lam, h - br * Math.Cos(Math.PI / 4)), dx = Math.Sqrt(Math.Max(0, br * br - (h - hEst) * (h - hEst)));
                int nf = 3;
                feedTag = d.W(nf, -seg * 0.5, 0, h, seg * 0.5, 0, h, a); feedSeg = 2;
                d.Ws(seg, seg * 0.5, 0, h, dx, 0, hEst, a, false, 5);
                d.Ws(seg, -seg * 0.5, 0, h, -dx, 0, hEst, a, false, 5);
                o.Card("V invertito", Calcolatori.N(L, 2) + " m", "estremi a " + Calcolatori.N(hEst, 1) + " m");
                if (hEst < 0.06 * lam) o.Warnings.Add("Estremi molto bassi: attenzione a persone e animali (tensioni alte alle punte).");
            }
            else
            {
                L = 142.6 / f;
                feedTag = d.Ws(seg, -L / 2, 0, h, L / 2, 0, h, a, true, 11); feedSeg = (d.LastN + 1) / 2;
                o.Card("Dipolo", Calcolatori.N(L, 2) + " m", "142,6/f");
            }
            double rs = 0.0127;   // 1" di diametro, come Cebik
            if (sotto == 1)
            {
                int n = 11; double lato = lam, passo = lato / (n - 1);
                for (int i = 0; i < n; i++)
                {
                    double c = -lato / 2 + i * passo;
                    d.Ws(passo, -lato / 2, c, hs, lato / 2, c, hs, rs, false, n - 1);
                    d.Ws(passo, c, -lato / 2, hs, c, lato / 2, hs, rs, false, n - 1);
                }
                o.Add("Schermo", "1 λ x 1 λ (" + Calcolatori.N(lam, 1) + " m), maglie da 0,1 λ, filo da 1\", a 0,001 λ dal suolo");
            }
            else if (sotto == 2)
            {
                double lw = Math.Max(L, tipo == 2 ? L / 4 : L) * 1.05;
                for (int i = -4; i <= 4; i++) d.Ws(seg, -lw / 2, i * 0.1 * lam, hs, lw / 2, i * 0.1 * lam, hs, 0.00081, false, 5);   // #14 AWG
                o.Add("9 fili", "#14 AWG a 0,1 λ, lunghi " + Calcolatori.N(lw, 1) + " m, a 0,001 λ dal suolo");
            }
            else if (sotto == 3)
            {
                double lw = L * 1.05;
                d.Ws(seg, -lw / 2, 0, hs, lw / 2, 0, hs, 0.00081, false, 5);
                o.Add("Filo riflettore", "un filo da " + Calcolatori.N(lw, 1) + " m a 0,001 λ dal suolo");
            }
            d.Ground(0);
            d.Raw("GN 2 0 0 0 " + Terr[ter, 0].ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + Terr[ter, 1].ToString(System.Globalization.CultureInfo.InvariantCulture));
            d.Ex(feedTag, feedSeg);
            o.Nec = d.End(f);
            o.Plane = "xz";
            o.Formula = "Cebik (NEC-4, Sommerfeld): il guadagno verso l'alto e' massimo a circa 0,175 λ; tra 0,125 e 0,225 λ va bene e l'altezza regola la forma del lobo " +
                "(alzando, si allarga di piu' il lato del dipolo). Sotto 0,1 λ il guadagno cala: l'antenna bassissima non guadagna, era un errore dei vecchi programmi con il terreno semplificato. " +
                "Lo schermo (o 9 fili) rende quasi 2 dB su terreno molto povero e circa 0,2 dB su terreno molto buono; un filo riflettore singolo serve poco. " +
                "NVIS direttiva (Cebik, n. 13): meno guadagno, non di piu' (un elemento inclinato o un riflettore lasco); per l'ALE servono antenne a banda larghissima.";
            if (hl < 0.1) o.Warnings.Add("Sotto 0,1 λ il guadagno verso l'alto cala (Cebik): l'antenna bassissima non guadagna.");
            else if (hl > 0.25) o.Warnings.Add("Sopra 0,25 λ il lobo comincia ad abbassarsi: meno adatto all'NVIS.");
            if (sotto == 3) o.Warnings.Add("Il filo riflettore singolo da' un miglioramento molto piccolo (Cebik): meglio lo schermo o i 9 fili.");
            if (sotto > 0) o.Oks.Add("fili a 0,001 λ dal suolo con il terreno di Sommerfeld, come nei modelli di Cebik (girano anche con il NEC-2)");
            return o;
        }
    }
}
