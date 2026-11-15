// YA-NEC 0.4.2 - calcolatori per la polarizzazione circolare (satelliti): eggbeater e QFH.
// Richiesti da IC8TEM il 13/11/2026 ("la eggbeater e la qfh mi servono nel calcolatore delle antenne tipo lpda e simili").
//
// Eggbeater (due spire a onda intera incrociate a 90 gradi, linea di fase lambda/4):
//  - ON6WG / F5VIF, "Eggbeater Antenna VHF/UHF" parte 1: perimetro 1005/f piedi (306,3/f m), riflettore di almeno 8 radiali
//    lambda/4, base delle spire a 1/8 lambda dal riflettore, linea di fase RG-62 (93 ohm) lambda/4; K5OE (eggbeater per i satelliti);
//  - tamago di C. Hoover (github.com/charlieh0tel/tamago, GPL-3): spire accordate in quadratura con il NEC (vengono il 2,5-3,7 %
//    piu' lunghe della formula), maglia uniforme con la sorgente su un lato normale, distacco di 10 mm tra le spire nell'incrocio,
//    radiali 0,27 lambda, rapporto delle correnti = |Z della spira| / Z0 della linea di fase, avviso: l'impedenza della spira
//    dipende molto dalla distanza dal riflettore (62-143 ohm), il "100 ohm per spira" della letteratura e' incompleto.
//    Solo idee e numeri di confronto, riscritti da capo.
//
// QFH (quadrifilare a elica, due spire bifilari a mezzo giro, piccola e grande):
//  - J. Coppens, calcolatore QFH (jcoppens.com/ant/qfh/calc.en.php, qfhcalc.js GPL-2): correzione della lunghezza con il
//    diametro del conduttore (tabella deltal), spira grande 1,026 e piccola 0,975 della lunghezza corretta, correzione delle
//    curve 2R - pi R / 2, diametro D = L / (2 (1 + sqrt(1/k^2 + (N pi)^2))), altezza H = D / k;
//  - geometria come helix2nec (uuki.kapsi.fi/qha_simul.html) e QFH2nec (oceantrotter, GPL-3): bracci radiali in alto dal
//    punto di alimentazione, curve, elica, curve e braccio radiale in basso che attraversa il centro; le due spire in parallelo
//    sul punto di alimentazione (la grande induttiva, la piccola capacitiva: correnti in quadratura).
//    Prova con nec2c (scratchpad egg/qfh.py): 137,5 MHz, conduttore 8 mm, R 15 mm, k 0,44: Z = 30,7 +j0,1 ohm, destra, AR 0,95.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public class CalcEggbeater : SimpleCalc
    {
        public CalcEggbeater()
        {
            Key = "eggbeater"; Title = "Eggbeater"; CanTune = false;
            Source = "ON6WG/F5VIF \"Eggbeater Antenna VHF/UHF\" (perimetro 1005/f piedi, riflettore a radiali, 1/8 lambda, RG-62 lambda/4); K5OE; " +
                     "tamago di C. Hoover (GPL-3): accordo in quadratura con il NEC e avviso sull'impedenza delle spire";
            Params.Add(new CalcParam("f", "Frequenza", 145.9, "MHz"));
            Params.Add(new CalcParam("d", "Diametro del conduttore", 5, "mm"));
            Params.Add(CalcParam.Scelta("forma", "Forma delle spire", 0, null, "cerchio", "quadrato"));
            Params.Add(CalcParam.Scelta("per", "Perimetro", 0, "Quadratura: il NEC allunga le spire finche' le correnti delle due spire sono a 90 gradi (tamago). Formula: 1005/f piedi (F5VIF).", "quadratura (NEC)", "formula 1005/f"));
            Params.Add(CalcParam.Scelta("rif", "Riflettore", 0, null, "radiali", "piano perfetto", "nessuno"));
            Params.Add(new CalcParam("dist", "Base delle spire sopra il riflettore", 0.125, "lambda", "F5VIF: 1/8 lambda dalla base delle spire. Piu' in basso l'impedenza delle spire scende (tamago)."));
            Params.Add(new CalcParam("nr", "Numero dei radiali", 8, ""));
            Params.Add(new CalcParam("lr", "Lunghezza dei radiali", 0.27, "lambda"));
            Params.Add(new CalcParam("z0", "Linea di fase: impedenza", 93, "ohm", "RG-62 93 ohm (F5VIF); 2 x RG-58 bilanciati = 100 ohm"));
            Params.Add(new CalcParam("vf", "Linea di fase: fattore di velocita'", 0.84, "", "RG-62: 0,84 (Belden 9269); F5VIF usa 0,86"));
            Params.Add(CalcParam.Scelta("senso", "Polarizzazione", 0, null, "destra (RHCP)", "sinistra (LHCP)"));
            Params.Add(new CalcParam("off", "Distacco tra le spire nell'incrocio", 10, "mm"));
        }

        // perimetro di formula in metri: 1005/f piedi
        static double PerimetroFormula(double f) { return 1005.0 * 0.3048 / f; }

        // mazzo NEC: spira A nel piano XZ (alimentata), spira B nel piano YZ, linea lambda/4 tra le due sorgenti
        string Mazzo(double fac, bool incrocia, out int tagA, out int tagB, out double zc, out double lato, out int nseg)
        {
            double f = P("f"), lam = C0 / f, a = P("d") / 2000, off = P("off") / 1000;
            double per = PerimetroFormula(f) * fac;
            bool quad = I("forma") == 1;
            int rif = I("rif");
            double meta = quad ? per / 8 : per / (2 * Math.PI);     // meta' lato o raggio
            lato = quad ? per / 4 : 2 * meta;
            nseg = 4 * (int)Math.Round(per / (36 * a) / 4);
            nseg = Math.Max(24, Math.Min(72, nseg));
            zc = rif == 2 ? meta + 0.1 * lam : P("dist") * lam + meta + off / 2;
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            tagA = Spira(d, true, per, quad, zc - off / 2, a, nseg);
            tagB = Spira(d, false, per, quad, zc + off / 2, a, nseg);
            if (rif == 0)
            {
                int nr = Math.Max(3, I("nr"));
                double lr = P("lr") * lam;
                int ns = Math.Max(1, (int)Math.Round(P("lr") / 0.05));
                for (int i = 0; i < nr; i++)
                {
                    double az = 2 * Math.PI * i / nr;
                    d.W(ns, 0, 0, 0, lr * Math.Cos(az), lr * Math.Sin(az), 0, a);
                }
            }
            d.Ground(rif == 1 ? 1 : 0);
            d.Raw("EK");
            double z0 = P("z0");
            d.Tl(tagA, 1, tagB, 1, incrocia ? -z0 : z0, lam / 4);
            d.Ex(tagA, 1);
            return d.End(f);
        }

        // spira a maglia uniforme: ogni lato un filo da un segmento, il primo (in basso, al centro) porta la sorgente
        static int Spira(NecDeck d, bool pianoXZ, double per, bool quad, double zc, double a, int n)
        {
            double[] u = new double[n + 1], v = new double[n + 1];
            for (int k = 0; k <= n; k++)
            {
                double s = ((k % n) - 0.5) / n;      // frazione del perimetro, partendo da meta' del primo lato in basso
                if (quad) PuntoQuadrato(s, per, out u[k], out v[k]);
                else
                {
                    double r = per / (2 * Math.PI), an = -Math.PI / 2 + 2 * Math.PI * s;
                    u[k] = r * Math.Cos(an); v[k] = r * Math.Sin(an);
                }
            }
            int primo = 0;
            for (int k = 0; k < n; k++)
            {
                int t = pianoXZ ? d.W(1, u[k], 0, zc + v[k], u[k + 1], 0, zc + v[k + 1], a)
                                : d.W(1, 0, u[k], zc + v[k], 0, u[k + 1], zc + v[k + 1], a);
                if (k == 0) primo = t;
            }
            return primo;
        }

        // punto del quadrato di perimetro per alla frazione s (0 = centro del lato basso, verso antiorario)
        static void PuntoQuadrato(double s, double per, out double u, out double v)
        {
            s -= Math.Floor(s);
            double l = per / 4, h = l / 2, p = s * per;
            if (p < h) { u = p; v = -h; }
            else if (p < h + l) { u = h; v = -h + (p - h); }
            else if (p < h + 2 * l) { u = h - (p - h - l); v = h; }
            else if (p < h + 3 * l) { u = -h; v = h - (p - h - 2 * l); }
            else { u = -h + (p - h - 3 * l); v = -h; }
        }

        // soluzione: correnti delle due spire alla sorgente e campo allo zenit
        Nec2Engine Risolvi(string nec, int tagA, int tagB, out Complex ia, out Complex ib)
        {
            NecModel m = NecModel.Parse(nec);
            SegmentGeometry g = m.BuildGeometry();
            Nec2Engine e = new Nec2Engine(g);
            double f = P("f");
            e.Solve(f, m.Sources, m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.Networks);
            ia = e.SegmentCurrent(g.SegNo(tagA, 1) - 1);
            ib = e.SegmentCurrent(g.SegNo(tagB, 1) - 1);
            return e;
        }

        static double Fase(Complex ia, Complex ib) { return Math.Abs((ia / ib).Phase * 180 / Math.PI); }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), lam = C0 / f, z0 = P("z0"), vf = P("vf");
            int tA, tB, ns; double zc, lato;
            double fac = 1;
            bool accordo = I("per") == 0;
            string avvisoAccordo = null;
            Complex ia, ib;
            if (accordo)
            {
                try
                {
                    // secante sulla fase tra le correnti delle spire (la fase scende allungando le spire)
                    Func<double, double> g = x => { Risolvi(Mazzo(x, false, out tA, out tB, out zc, out lato, out ns), tA, tB, out ia, out ib); return Fase(ia, ib) - 90; };
                    double x0 = 1.0, x1 = 1.03, g0 = g(x0), g1 = g(x1);
                    for (int it = 0; it < 15 && Math.Abs(g1) > 0.3; it++)
                    {
                        if (g1 == g0) break;
                        double x2 = x1 - g1 * (x1 - x0) / (g1 - g0);
                        x2 = Math.Max(0.85, Math.Min(1.25, x2));
                        x0 = x1; g0 = g1; x1 = x2; g1 = g(x1);
                    }
                    if (Math.Abs(g1) > 2) avvisoAccordo = "Quadratura non raggiunta (scarto " + Calcolatori.N(g1, 1) + " gradi): misure di formula.";
                    else fac = x1;
                }
                catch (NecException ex) { avvisoAccordo = "Accordo in quadratura non riuscito (" + ex.Message + "): misure di formula."; }
            }
            // verso della polarizzazione: se esce quello sbagliato si incrocia la linea di fase (Z0 negativa nella scheda TL)
            bool incrocia = false;
            string nec = Mazzo(fac, false, out tA, out tB, out zc, out lato, out ns);
            Nec2Engine e = Risolvi(nec, tA, tB, out ia, out ib);
            FarFieldPoint zen = e.Gain(0, 0);
            int voluto = I("senso") == 0 ? 1 : 2;
            if (zen.Sense != 0 && zen.Sense != voluto)
            {
                incrocia = true;
                nec = Mazzo(fac, true, out tA, out tB, out zc, out lato, out ns);
                e = Risolvi(nec, tA, tB, out ia, out ib);
                zen = e.Gain(0, 0);
            }
            double per = PerimetroFormula(f) * fac;
            bool quad = I("forma") == 1;
            double rapporto = Complex.Abs(ib / ia);
            double zSpira = rapporto * z0;
            Complex zin = e.Sources[0].Z;
            double gam = Complex.Abs((zin - 50) / (zin + 50)), ros = (1 + gam) / (1 - gam);
            double ar = zen.AxialRatio, arDb = ar > 1e-6 ? 20 * Math.Log10(1 / ar) : 99;
            double gco = zen.GainTotal + 10 * Math.Log10((1 + ar) * (1 + ar) / (2 * (1 + ar * ar)));
            double baseSpire = zc - P("off") / 2000 - (quad ? per / 8 : per / (2 * Math.PI));

            o.Card("Perimetro di ogni spira", Calcolatori.N(per * 1000, 0) + " mm", accordo && avvisoAccordo == null ? "quadratura NEC: " + Calcolatori.N((fac - 1) * 100, 1) + " % sulla formula" : "1005/f piedi", true);
            o.Card(quad ? "Lato" : "Diametro", Calcolatori.N(lato * 1000, 0) + " mm", Calcolatori.N(ns, 0) + " lati nel modello");
            o.Card("Linea di fase", Calcolatori.N(lam / 4 * vf * 1000, 0) + " mm", "lambda/4 x " + Calcolatori.N(vf, 2) + (incrocia ? ", incrociata" : ""));
            o.Card("Impedenza d'ingresso", Calcolatori.N(zin.Real, 1) + (zin.Imaginary < 0 ? " -j" : " +j") + Calcolatori.N(Math.Abs(zin.Imaginary), 1) + " Ω", "ROS " + Calcolatori.N(ros, 2) + " su 50 Ω", true);
            o.Card("Allo zenit", Calcolatori.N(gco, 2) + " dBic", "rapporto assiale " + Calcolatori.N(arDb, 1) + " dB, " + (zen.Sense == 1 ? "destra" : zen.Sense == 2 ? "sinistra" : "lineare"));
            o.Card("Correnti delle spire", Calcolatori.N(rapporto, 2) + " : 1", "spira circa " + Calcolatori.N(zSpira, 0) + " Ω, linea " + Calcolatori.N(z0, 0) + " Ω");
            if (I("rif") != 2)
                o.Card("Quote", "centro " + Calcolatori.N(zc * 1000, 0) + " mm", "base delle spire " + Calcolatori.N(baseSpire * 1000, 0) + " mm sopra il riflettore");

            o.Formula = "Formule: perimetro 1005/f piedi (F5VIF), poi allungato col NEC finche' le correnti delle due spire sono in quadratura (tamago); " +
                        "linea di fase lambda/4 tra le due alimentazioni, lunghezza fisica = lambda/4 x VF; rapporto delle correnti = |Z spira| / Z0 della linea; " +
                        "guadagno circolare = guadagno totale x (1 + r)^2 / (2 (1 + r^2)), r = rapporto assiale.";
            o.Warnings.Add("L'impedenza di ogni spira dipende molto dalla distanza dal riflettore (tamago: da 62 a 143 ohm tra 0,03 e 0,12 lambda di distanza della base): " +
                           "il \"100 ohm per spira, 50 ohm in parallelo\" della letteratura vale solo per una certa altezza.");
            if (rapporto > 1.2 || rapporto < 0.83)
                o.Warnings.Add("Correnti sbilanciate (" + Calcolatori.N(rapporto, 2) + " : 1): per la polarizzazione circolare servirebbe una linea di fase da circa " +
                               Calcolatori.N(zSpira, 0) + " ohm, oppure abbassare il riflettore finche' la spira scende verso " + Calcolatori.N(z0, 0) + " ohm.");
            if (avvisoAccordo != null) o.Warnings.Add(avvisoAccordo);
            if (ros > 2) o.Warnings.Add("ROS sopra 2 su 50 ohm: serve un adattamento (F5VIF parte 2: balun 4:1 o bobina d'arresto).");
            o.Nec = nec;
            o.Plane = "xz";
            return o;
        }
    }

    public class CalcQfh : SimpleCalc
    {
        public CalcQfh()
        {
            Key = "qfh"; Title = "QFH (quadrifilare a elica)"; CanTune = false;
            Source = "J. Coppens, calcolatore QFH (jcoppens.com, qfhcalc.js); geometria come helix2nec (uuki.kapsi.fi) e QFH2nec (oceantrotter)";
            Params.Add(new CalcParam("f", "Frequenza", 137.5, "MHz"));
            Params.Add(new CalcParam("giri", "Giri dell'elica", 0.5, "", "Coppens: di solito mezzo giro"));
            Params.Add(new CalcParam("lg", "Lunghezza di una spira", 1, "lambda"));
            Params.Add(new CalcParam("k", "Rapporto larghezza / altezza", 0.44, "", "Coppens: 0,44 diagramma quasi emisferico; valori piu' bassi lo stringono"));
            Params.Add(new CalcParam("d", "Diametro del conduttore", 8, "mm"));
            Params.Add(new CalcParam("rc", "Raggio delle curve", 15, "mm"));
            Params.Add(new CalcParam("gap", "Distanza dei morsetti di alimentazione", 15, "mm"));
            Params.Add(CalcParam.Scelta("senso", "Polarizzazione", 0, null, "destra (RHCP)", "sinistra (LHCP)"));
        }

        // tabella di Coppens: correzione della lunghezza col diametro del conduttore (0-16 mm)
        static readonly double[] DeltaL = { 1.045, 1.053, 1.060, 1.064, 1.068, 1.070, 1.070, 1.071, 1.071, 1.070, 1.070, 1.070, 1.070, 1.069, 1.069, 1.068, 1.067 };
        static double Correzione(double dmm)
        {
            double d = Math.Max(0, Math.Min(15, dmm));
            int i = (int)d;
            return DeltaL[i] + (DeltaL[i + 1] - DeltaL[i]) * (d - i);
        }

        // dimensioni di Coppens in mm: D (diametro tra i centri dei conduttori) e H (altezza) di una spira di fattore c
        void Dimensioni(double c, out double D, out double H, out double totale)
        {
            double lam = 299792.458 / P("f"), R = P("rc"), k = P("k"), N = P("giri");
            double wl = P("lg") * lam * Correzione(P("d"));
            double curve = 2 * R - Math.PI * R / 2;
            totale = wl * c;
            D = 0.5 * (totale + 4 * curve) / (1 + Math.Sqrt(1 / (k * k) + Math.Pow(N * Math.PI, 2)));
            H = D / k;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), lam = C0 / f, R = P("rc") / 1000, a = P("d") / 2000, gap = P("gap") / 1000, N = P("giri");
            double Dg, Hg, Lg, Dp, Hp, Lp;
            Dimensioni(1.026, out Dg, out Hg, out Lg);
            Dimensioni(0.975, out Dp, out Hp, out Lp);
            Dg /= 1000; Hg /= 1000; Dp /= 1000; Hp /= 1000;
            if (R * 2 >= Math.Min(Dp, Hp)) { o.Warnings.Add("Raggio delle curve troppo grande per queste misure."); R = Math.Min(Dp, Hp) / 4; }
            double tw = I("senso") == 0 ? -N : N;      // QFH2nec: giri negativi = destra
            double seg = lam / 60;

            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            Elica(d, Dp, Hp, R, a, gap, tw, 0, true, seg);
            Elica(d, Dg, Hg, R, a, gap, tw, Math.PI / 2, false, seg);
            double c45 = Math.Cos(Math.PI / 4) * gap / 2;
            int tf = d.W(1, c45, c45, 0, -c45, -c45, 0, a);
            d.Ground(0);
            d.Ex(tf, 1);
            o.Nec = d.End(f);

            o.Card("Spira grande", "D " + Calcolatori.N(Dg * 1000, 1) + " mm, H " + Calcolatori.N(Hg * 1000, 1) + " mm", "lunghezza " + Calcolatori.N(Lg, 0) + " mm (1,026)", true);
            o.Card("Spira piccola", "D " + Calcolatori.N(Dp * 1000, 1) + " mm, H " + Calcolatori.N(Hp * 1000, 1) + " mm", "lunghezza " + Calcolatori.N(Lp, 0) + " mm (0,975)", true);
            o.Card("Diametri interni", Calcolatori.N((Dg - 2 * a) * 1000, 1) + " / " + Calcolatori.N((Dp - 2 * a) * 1000, 1) + " mm", "grande / piccola, tra i bordi interni del conduttore");
            o.Card("Correzione del conduttore", Calcolatori.N(Correzione(P("d")), 3), "tabella di Coppens per " + Calcolatori.N(P("d"), 1) + " mm");
            o.Card("Polarizzazione", I("senso") == 0 ? "destra (RHCP)" : "sinistra (LHCP)", "verso degli avvolgimenti visti dall'alto");
            o.Formula = "Formule di J. Coppens: L = n lambda x correzione del conduttore; spira grande 1,026 L, piccola 0,975 L, piu' 4 (2R - pi R / 2) per le curve; " +
                        "D = L / (2 (1 + sqrt(1/k^2 + (N pi)^2))), H = D / k. Le due spire sono in parallelo sui morsetti in alto: la grande e' induttiva, la piccola capacitiva, " +
                        "e le correnti vanno in quadratura senza linea di fase.";
            o.Warnings.Add("Il modello alimenta le due spire direttamente ai morsetti (balun ideale): nella realta' serve il balun a bobina d'arresto o \"infinito\" lungo il supporto.");
            o.Plane = "xz";
            return o;
        }

        // spira bifilare: due meta' opposte, ciascuna braccio radiale in alto, curva, elica, curva; braccio radiale in basso che le unisce
        static void Elica(NecDeck d, double D, double H, double R, double a, double gap, double tw, double th0, bool primaSpira, double seg)
        {
            double[] fx = new double[2], fy = new double[2], fz = new double[2];
            for (int lato = 0; lato < 2; lato++)
            {
                double sg = lato == 0 ? 1 : -1;
                double am = primaSpira ? th0 + Math.PI / 4 : th0 - Math.PI / 4;     // morsetti comuni alle due spire
                double x1 = sg * gap / 2 * Math.Cos(am), y1 = sg * gap / 2 * Math.Sin(am);
                double x = sg * (D / 2 - R) * Math.Cos(th0), y = sg * (D / 2 - R) * Math.Sin(th0), z = 0;
                d.Ws(seg, x1, y1, 0, x, y, 0, a, false, 2);
                for (int i = 1; i <= 5; i++)
                {
                    double px = x, py = y, pz = z, al = Math.PI / 2 * i / 5;
                    z = -R + R * Math.Cos(al);
                    double th = z / H * tw * 2 * Math.PI + th0, rr = D / 2 - R + R * Math.Sin(al);
                    x = sg * rr * Math.Cos(th); y = sg * rr * Math.Sin(th);
                    d.W(1, px, py, pz, x, y, z, a);
                }
                int nh = Math.Max(8, (int)Math.Ceiling(Math.Sqrt((H - 2 * R) * (H - 2 * R) + Math.Pow(tw * Math.PI * D, 2)) / seg));
                for (int i = 1; i <= nh; i++)
                {
                    double px = x, py = y, pz = z;
                    z = -R - (double)i / nh * (H - 2 * R);
                    double th = z / H * tw * 2 * Math.PI + th0;
                    x = sg * D / 2 * Math.Cos(th); y = sg * D / 2 * Math.Sin(th);
                    d.W(1, px, py, pz, x, y, z, a);
                }
                for (int i = 1; i <= 5; i++)
                {
                    double px = x, py = y, pz = z, al = Math.PI / 2 * i / 5;
                    z = -H + R - R * Math.Sin(al);
                    double th = z / H * tw * 2 * Math.PI + th0, rr = D / 2 - R + R * Math.Cos(al);
                    x = sg * rr * Math.Cos(th); y = sg * rr * Math.Sin(th);
                    d.W(1, px, py, pz, x, y, z, a);
                }
                fx[lato] = x; fy[lato] = y; fz[lato] = z;
            }
            d.Ws(seg, fx[0], fy[0], fz[0], fx[1], fy[1], fz[1], a, true, 3);
        }
    }
}
