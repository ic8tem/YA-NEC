// YA-NEC 0.4.30 - calcolatore della HEXBEAM 20-10 m (G3TXQ "broadband hexbeam") - richiesta di IC8TEM del 14/11/2026 17:59-18:04,
// via del 14/11/2026 21:54 ("si procedi"): sottolinguetta dei Calcolatori per chi compra crociera e canne e si costruisce la direttiva.
// Decisioni di IC8TEM: misure 20-10 m dal modello di JN1GLB (Makoto Kasahara, MMANA 2008, in data/modelli); riferimento 50 ohm alla base
// del calcolo, l'importante e' jX vicina a zero (impedenze piu' basse vanno bene); i dipoli si collegano a una alimentazione comune
// ("bacchetta"); il kit 30/40 m e' separato, con il suo balun (non e' in questa scheda).
// Geometria (dal modello di JN1GLB, NOTE_hexbeam.md): per ogni banda un esagono di raggio R alla quota z; l'elemento alimentato e' a "W"
// (dal centro al vertice a +-30 gradi, poi lungo il lato verso il vertice a +-90 gradi per la frazione td del lato), il riflettore e' a "U"
// (lato posteriore, i due lati fino ai vertici a +-90 gradi, poi una coda verso l'alimentato per la frazione tr del lato); tra la punta
// dell'alimentato e la coda del riflettore resta lo spazio del distanziatore (corda). Il lobo e' verso +x (dal riflettore all'alimentato).
// Collegamento delle bande: come G3TXQ (M0OXO, N4KC/K4KIO) l'alimentazione e' in alto (20 m) e i centri degli alimentati sono uniti da
// spezzoni di linea lungo il palo centrale; nel NEC sono linee di trasmissione (scheda TL) tra i segmenti centrali. Prova con nec2c
// (14/11/2026, doc/riferimenti/hexbeam): con le misure di JN1GLB e spezzoni di coassiale 50 ohm (VF 0,66) il ROS e' sotto 1,32 su tutte
// le bande; con una linea a due fili da 120-300 ohm le bande alte non si adattano (ROS 2-9). Il pulsante "Risonanza" qui accorda
// ogni banda a 50 + j0 ohm cambiando la lunghezza dell'alimentato (td) e la coda del riflettore (tr), cioe' lo spazio tra le punte:
// con nec2c 49,9-54,7 ohm e |X| sotto 1,2 ohm su tutte le bande in tre giri.
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public class CalcHexbeam : SimpleCalc
    {
        // JN1GLB: banda, centro banda MHz, quota z (m, livello del 6 m = 0), raggio delle punte (m), frazione dell'alimentato, frazione della coda
        static readonly string[] Nb = { "20 m", "17 m", "15 m", "12 m", "10 m" };
        static readonly double[] Fb = { 14.175, 18.118, 21.225, 24.940, 28.500 };
        static readonly double[] Zj = { 0.85, 0.55, 0.35, 0.20, 0.08 };
        static readonly double[] Rj = { 3.217, 2.529, 2.184, 1.845, 1.627 };
        static readonly double[] TdJ = { 0.7443, 0.7105, 0.7050, 0.7015, 0.6940 };
        static readonly double[] TrJ = { 0.1355, 0.1137, 0.1095, 0.1108, 0.1214 };
        const double Gap = 0.02;      // spazio tra le due meta' al centro (morsetti), segmento della sorgente e delle linee

        // valori accordati con il pulsante "Risonanza" (null = misure di JN1GLB); valgono finche' non cambiano i dati della scheda
        double[] td, tr;
        Complex[] zAcc;
        string firma;

        public CalcHexbeam()
        {
            Key = "hexbeam"; Title = "Hexbeam 20-10 m"; Source = "G3TXQ (broadband hexbeam), misure del modello MMANA di JN1GLB (2008); M0OXO, N4KC/K4KIO, MM0OPX";
            Params.Add(CalcParam.Scelta("b", "Banda nel NEC", 0, "La banda di cui il motore calcola impedenza e diagramma (il modello contiene sempre tutte e cinque le bande).", Nb));
            Params.Add(new CalcParam("R", "Raggio delle punte del 20 m", 3.217, "m", "Distanza dall'asse del palo al vertice dell'esagono del 20 m (JN1GLB 3,217 m). Le altre bande seguono le proporzioni di JN1GLB."));
            Params.Add(new CalcParam("d", "Diametro del filo", 1.0, "mm"));
            Params.Add(CalcParam.Scelta("col", "Collegamento tra le bande", 0, "Linea che unisce i centri degli alimentati lungo il palo centrale, dall'alimentazione in alto (20 m) fino al 10 m.", "Coassiale (G3TXQ)", "Bacchetta a due conduttori"));
            Params.Add(new CalcParam("z0", "Coassiale: impedenza", 50, "Ω"));
            Params.Add(new CalcParam("vf", "Coassiale: fattore di velocita'", 0.66, "", "0,66 polietilene pieno (RG-58, RG-213), 0,8-0,85 dielettrico espanso."));
            Params.Add(new CalcParam("sb", "Bacchetta: distanza tra i conduttori", 20, "mm", "Da asse ad asse."));
            Params.Add(new CalcParam("db", "Bacchetta: diametro dei conduttori", 6, "mm"));
            Params.Add(new CalcParam("h", "Quota del 20 m (0 = spazio libero)", 0, "m", "Con quota maggiore di 0 il calcolo NEC usa il terreno medio (er 13, sigma 0,005 S/m)."));
        }

        string Firma() { StringBuilder s = new StringBuilder(); foreach (CalcParam p in Params) if (p.Key != "b") s.Append(p.Key + "=" + p.Value + ";"); return s.ToString(); }

        double Z0Linea(out double vf)
        {
            if (I("col") == 0) { vf = Math.Max(0.3, Math.Min(1, P("vf"))); return Math.Max(10, P("z0")); }
            vf = 0.97;
            double s = Math.Max(P("sb"), P("db") * 1.01), d = Math.Max(0.1, P("db"));
            return 120 * Math.Log(s / d + Math.Sqrt(s / d * (s / d) - 1));     // 120 acosh(s/d)
        }

        string Mazzo(double[] tdv, double[] trv, int banda)
        {
            double k = P("R") / Rj[0], a = Math.Max(0.05, P("d")) / 2000, h = P("h"), vf;
            double z0 = Z0Linea(out vf);
            double seg = C0 / Fb[4] / 40;
            NecDeck d = new NecDeck(Title + " " + Nb[banda]);
            int[] cen = new int[5];
            double[] zq = new double[5];
            for (int i = 0; i < 5; i++)
            {
                double z = h + (Zj[i] - Zj[0]) * k, R = Rj[i] * k, c30 = R * Math.Cos(Math.PI / 6), s30 = R * Math.Sin(Math.PI / 6);
                zq[i] = z;
                cen[i] = d.W(1, 0, -Gap / 2, z, 0, Gap / 2, z, a);
                for (int sg = 1; sg >= -1; sg -= 2)
                {
                    double vx = c30, vy = sg * s30, px = vx + (0 - vx) * tdv[i], py = vy + (sg * R - vy) * tdv[i];
                    d.Ws(seg, 0, sg * Gap / 2, z, vx, vy, z, a, false, 3);
                    // 0.4.34 (collaudo 0.4.33, Risonanza: 15 m 60,4 +j5,5 e 10 m 57,9 ohm): i tratti che l'accordo allunga e accorcia hanno il numero
                    // di segmenti delle misure di JN1GLB; con ceil(l / seg) il numero saltava di uno durante la derivata e Newton si perdeva
                    d.W(Math.Max(2, (int)Math.Ceiling(R * TdJ[i] / seg)), vx, vy, z, px, py, z, a);
                }
                d.Ws(seg, -c30, -s30, z, -c30, s30, z, a, false, 3);
                for (int sg = 1; sg >= -1; sg -= 2)
                {
                    double ex = c30 * trv[i], ey = sg * R + (sg * s30 - sg * R) * trv[i];
                    d.Ws(seg, -c30, sg * s30, z, 0, sg * R, z, a, false, 3);
                    d.W(Math.Max(1, (int)Math.Ceiling(R * TrJ[i] / seg)), 0, sg * R, z, ex, ey, z, a);   // 0.4.34: segmenti fissi (vedi sopra)
                }
            }
            d.Ground(h > 0 ? 2 : 0);
            // linee lungo il palo: lunghezza fisica = dislivello; nel NEC la lunghezza della TL e' elettrica (fisica / VF)
            for (int i = 0; i < 4; i++) d.Tl(cen[i], 1, cen[i + 1], 1, z0, Math.Max(0.01, zq[i] - zq[i + 1]) / vf);
            d.Ex(cen[0], 1);
            return d.End(Fb[banda]);
        }

        Complex Zin(double[] tdv, double[] trv, int banda)
        {
            NecModel m = NecModel.Parse(Mazzo(tdv, trv, banda));
            SegmentGeometry g = m.BuildGeometry();
            Nec2Engine e = new Nec2Engine(g);
            double f = Fb[banda];
            e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            return e.Sources[0].Z;
        }

        /// <summary>Accordo a 50 + j0 ohm di ogni banda: Newton su (td, tr) per banda, tre giri (le bande si influenzano).</summary>
        public override double Tune(double fmhz)
        {
            double[] a = (double[])TdJ.Clone(), b = (double[])TrJ.Clone();
            // 0.4.31 (collaudo 0.4.30: 10 m a 40,9 +j5,0 ohm dopo tre giri): fino a 6 giri, finche' tutte le bande sono entro 1 ohm da 50 + j0
            for (int giro = 0; giro < 6; giro++)
            {
                if (giro >= 3)
                {
                    bool fatto = true;
                    for (int j = 0; j < 5; j++) { Complex zj = Zin(a, b, j); if (Math.Abs(zj.Real - 50) > 1 || Math.Abs(zj.Imaginary) > 1) fatto = false; }
                    if (fatto) break;
                }
                for (int i = 0; i < 5; i++)
                {
                    Func<double, double, Complex> F = (x, y) => { double[] ta = (double[])a.Clone(), tb = (double[])b.Clone(); ta[i] = x; tb[i] = y; return Zin(ta, tb, i) - 50; };
                    double x0 = a[i], y0 = b[i];
                    for (int it = 0; it < 12; it++)
                    {
                        Complex f0 = F(x0, y0);
                        if (Math.Abs(f0.Real) < 0.3 && Math.Abs(f0.Imaginary) < 0.3) break;
                        const double hh = 0.003;
                        Complex fx = (F(x0 + hh, y0) - f0) / hh, fy = (F(x0, y0 + hh) - f0) / hh;
                        double det = fx.Real * fy.Imaginary - fy.Real * fx.Imaginary;
                        if (Math.Abs(det) < 1e-9) break;
                        double dx = (-f0.Real * fy.Imaginary + fy.Real * f0.Imaginary) / det, dy = (-fx.Real * f0.Imaginary + fx.Imaginary * f0.Real) / det;
                        double s = 1;
                        while (Math.Max(Math.Abs(dx * s), Math.Abs(dy * s)) > 0.05) s /= 2;
                        // 0.4.36 (collaudo 0.4.34: 15 m a 54,4 +j2,9 e 10 m a 57,9 +j1,9 ohm): si tiene il passo solo se |Z - 50| scende,
                        // altrimenti lo si dimezza (fino a 6 volte); se nessun passo migliora, Newton si ferma li'
                        bool meglio = false;
                        for (int dim = 0; dim < 7 && !meglio; dim++, s /= 2)
                        {
                            double xn = Math.Max(0.4, Math.Min(0.85, x0 + dx * s));
                            double yn = Math.Max(0.02, Math.Min(0.95 - xn, y0 + dy * s));
                            if (Complex.Abs(F(xn, yn)) < Complex.Abs(f0)) { x0 = xn; y0 = yn; meglio = true; }
                        }
                        if (!meglio) break;
                    }
                    a[i] = x0; b[i] = y0;
                }
            }
            td = a; tr = b; firma = Firma();
            zAcc = new Complex[5];
            for (int i = 0; i < 5; i++) zAcc[i] = Zin(td, tr, i);
            return 1;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            if (firma != null && firma != Firma()) { td = tr = null; zAcc = null; firma = null; }
            bool acc = td != null;
            double[] tdv = acc ? td : TdJ, trv = acc ? tr : TrJ;
            int banda = Math.Max(0, Math.Min(4, I("b")));
            double k = P("R") / Rj[0], vf, z0 = Z0Linea(out vf);
            for (int i = 0; i < 5; i++)
            {
                double R = Rj[i] * k, alim = R + tdv[i] * R + Gap / 2, rifl = 3 * R + 2 * trv[i] * R, spazio = (1 - tdv[i] - trv[i]) * R;
                string sub = "riflettore " + Calcolatori.N(rifl, 3) + " m, tra le punte " + Calcolatori.N(spazio, 3) + " m, raggio " + Calcolatori.N(R, 3) + " m";
                if (acc) sub += ", Z " + Calcolatori.Cx(zAcc[i]);
                o.Card(Nb[i], "alim. 2 x " + Calcolatori.N(alim, 3) + " m", sub, i == banda);
            }
            double vs = (Zj[0] - Zj[4]) * k;
            o.Card("Collegamento", I("col") == 0 ? "coassiale " + Calcolatori.N(z0, 0) + " Ω" : "bacchetta " + Calcolatori.N(z0, 0) + " Ω",
                "spezzoni tra le quote: " + Calcolatori.N((Zj[0] - Zj[1]) * k, 2) + " / " + Calcolatori.N((Zj[1] - Zj[2]) * k, 2) + " / " + Calcolatori.N((Zj[2] - Zj[3]) * k, 2) + " / " + Calcolatori.N((Zj[3] - Zj[4]) * k, 2) + " m, VF " + Calcolatori.N(vf, 2));
            o.Card("Palo centrale", "dal 20 m al 10 m " + Calcolatori.N(vs, 2) + " m", "alimentazione in alto, al 20 m");
            if (acc) o.Oks.Add("misure accordate con il NEC a 50 + j0 Ω su ogni banda (pulsante Risonanza); cambiando un dato si torna alle misure di JN1GLB");
            else o.Oks.Add("misure del modello di JN1GLB (proporzioni scalate sul raggio del 20 m); il pulsante Risonanza le accorda a 50 + j0 Ω banda per banda");
            if (I("col") == 1 && z0 > 75)
                o.Warnings.Add("Bacchetta da " + Calcolatori.N(z0, 0) + " Ω: la distribuzione funziona con una linea di circa 50 Ω (G3TXQ usa spezzoni di coassiale). Con una linea ad alta impedenza le bande alte non si adattano (prova con nec2c: ROS da 2 a 9 con 120-300 Ω).");
            if (Math.Abs(k - 1) > 0.1) o.Warnings.Add("Raggio molto diverso da JN1GLB: le proporzioni restano quelle del suo modello, conviene accordare con il pulsante Risonanza.");
            o.Warnings.Add("Il kit 30/40 m e' separato, con il suo balun e la sua discesa: non e' in questo modello.");
            o.Formula = "Geometria G3TXQ dal modello di JN1GLB: per ogni banda un esagono di raggio R; alimentato a W = R + td R per lato (dal centro al vertice a 30 gradi e lungo il lato), " +
                "riflettore a U = 3 R + 2 tr R, spazio tra le punte (1 - td - tr) R. JN1GLB: td = 0,744 / 0,711 / 0,705 / 0,702 / 0,694, tr = 0,136 / 0,114 / 0,110 / 0,111 / 0,121. " +
                "Bande collegate al centro con spezzoni di linea lungo il palo (TL nel NEC, lunghezza elettrica = dislivello / VF), alimentazione in alto al 20 m (M0OXO, N4KC). " +
                "Bacchetta a due conduttori: Z0 = 120 acosh(s/d). Riferimento 50 Ω.";
            o.Nec = Mazzo(tdv, trv, banda);
            o.Plane = "xy";
            o.HideGround = true;
            double R0 = Rj[0] * k;
            o.Mark(0, R0 * 0.55, -R0 * 0.08, "lobo ->", 1);
            o.Mark(7, -R0 * Math.Cos(Math.PI / 6), -R0 * 1.12, "R 20 m " + Calcolatori.N(R0, 2), 3, 0, -R0 * 1.12);
            o.Chart = new CalcChart { Title = "Lunghezza di ogni meta' dell'alimentato per banda", XLabel = "MHz", YLabel = "m" };
            o.Chart.X = (double[])Fb.Clone(); o.Chart.Y = new double[5];
            for (int i = 0; i < 5; i++) o.Chart.Y[i] = Rj[i] * k * (1 + tdv[i]) + Gap / 2;
            o.Chart.MarkX = Fb[banda]; o.Chart.MarkText = Nb[banda];
            return o;
        }
    }
}
