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
        const double Gap = 0.02;
        // 1.0.10 (IC8TEM 16/11/2026 22:32 "devo poter calcolare anche ... la hex in 50 MHz con 2 o 3 elementi"): il 6 m del modello di
        // JN1GLB (6BandBBHEX, data/modelli), un livello sotto il 10 m (quota 0 contro 0,08 m): alimentato diritto 2 x 1,38 m, riflettore a U
        // (lato posteriore a x = -1,19, braccia fino a (-0,48; +-1,075)), direttore a U (lato a x = 0,66 da -1,06 a 1,06, punte fino a
        // (0,51; +-1,15)). "2 elementi" toglie il direttore. Prova con nec2c (scratchpad hex6/h6.py, coassiale 50 ohm VF 0,66, spazio
        // libero, 50,15 MHz): 2 el 44,0 +j6,5 ohm ROS 1,21, 6,50 dBi, F/B 14,6 dB; 3 el 33,0 +j11,3 ohm ROS 1,65, 6,96 dBi, F/B 19,6 dB.
        static readonly string[] NbTutte = { "20 m", "17 m", "15 m", "12 m", "10 m", "6 m", "30 m", "40 m" };
        const double F6 = 50.150, Z6 = 0.0, F30 = 10.125, F40 = 7.100;
        // 1.0.10 (IC8TEM 16/11/2026 22:47 "va parallelo al dipolo dei 20mt", 22:48 "pero' segue comunque la prosecuzione della geometria
        // dei bracci"): dipoli 30 e 40 m con la loro alimentazione (il kit ha il suo balun): ogni braccio parte dal centro, segue il W del
        // 20 m (centro -> vertice a 30 gradi -> vertice a 90 gradi) e prosegue lungo l'esagono (vertice a 150 gradi, poi verso il retro) finche'
        // la meta' del dipolo e' lunga quanto serve; quota sotto il 20 m (30 m a dz, 40 m a dz + 0,10 m). Quando si calcola un dipolo
        // l'alimentazione della hex e l'altro dipolo hanno un carico di 50 ohm (il coassiale verso la radio), e viceversa.
        // Prova con nec2c (scratchpad hex6/h34.py, R 3,217 m, dz 0,10 m, un dipolo per volta): 40 m meta' 10,36 m, 3,1 ohm, 2,97 dBi;
        // 30 m meta' 7,31 m, 5,9 ohm, 3,48 dBi: piegati attorno a un esagono di 6,4 m le punte tornano verso il retro e la resistenza
        // di radiazione e' di pochi ohm, quindi serve un adattamento (lo dice la scheda).
        double l30 = double.NaN, l40 = double.NaN;
        static readonly string[] Tr34 = { "1:1 (50 Ω)", "4:1 al contrario (12,5 Ω)", "9:1 al contrario (5,6 Ω)", "16:1 al contrario (3,1 Ω)" };
        static readonly double[] Tr34n = { 1, 4, 9, 16 };
        Complex z30 = Complex.Zero, z40 = Complex.Zero;
        static double FreqB(int b) { return b == 5 ? F6 : b == 6 ? F30 : b == 7 ? F40 : Fb[b]; }      // spazio tra le due meta' al centro (morsetti), segmento della sorgente e delle linee

        // valori accordati con il pulsante "Risonanza" (null = misure di JN1GLB); valgono finche' non cambiano i dati della scheda
        double[] td, tr;
        Complex[] zAcc;
        string firma;

        public CalcHexbeam()
        {
            Key = "hexbeam"; Title = "Hexbeam 20-10 m"; Source = "G3TXQ (broadband hexbeam), misure del modello MMANA di JN1GLB (2008); M0OXO, N4KC/K4KIO, MM0OPX";
            Params.Add(CalcParam.Scelta("b", "Banda nel NEC", 0, "La banda di cui il motore calcola impedenza e diagramma (il modello contiene tutte le bande scelte).", NbTutte));
            Params.Add(CalcParam.Scelta("d34", "Dipoli 30 / 40 m", 0, "Dipoli con la loro alimentazione, paralleli all'alimentato del 20 m e lungo i bracci dell'esagono.", "nessuno", "30 m", "40 m", "30 e 40 m"));
            Params.Add(new CalcParam("dz", "Dipoli: distanza sotto il 20 m", 0.10, "m", "Quota del dipolo dei 30 m sotto il 20 m; il 40 m sta 0,10 m piu' in basso."));
            // 1.0.10 (IC8TEM 16/11/2026 22:57 "puo' funzionare con un 1:4 al contrario?", 23:08 "i dipoli sono due separati"): un
            // trasformatore per ogni dipolo (ognuno ha la sua discesa); il ROS del dipolo e' riferito a 50 ohm / rapporto
            Params.Add(CalcParam.Scelta("t30", "Trasformatore del 30 m", 0, "Rapporto d'impedenza del balun/un-un del dipolo dei 30 m, verso l'antenna (4:1 al contrario = 50 -> 12,5 Ω).", Tr34));
            Params.Add(CalcParam.Scelta("t40", "Trasformatore del 40 m", 0, "Rapporto d'impedenza del balun/un-un del dipolo dei 40 m, verso l'antenna (9:1 al contrario = 50 -> 5,6 Ω).", Tr34));
            Params.Add(CalcParam.Scelta("e6", "6 m (50 MHz)", 0, "Il 6 m del modello di JN1GLB, sotto il 10 m: 2 elementi (riflettore e alimentato) o 3 elementi (con il direttore).", "nessuno", "2 elementi", "3 elementi"));
            Params.Add(new CalcParam("R", "Raggio delle punte del 20 m", 3.217, "m", "Distanza dall'asse del palo al vertice dell'esagono del 20 m (JN1GLB 3,217 m). Le altre bande seguono le proporzioni di JN1GLB."));
            Params.Add(new CalcParam("d", "Diametro del filo", 1.0, "mm"));
            Params.Add(CalcParam.Scelta("col", "Collegamento tra le bande", 0, "Linea che unisce i centri degli alimentati lungo il palo centrale, dall'alimentazione in alto (20 m) fino al 10 m.", "Coassiale (G3TXQ)", "Bacchetta a due conduttori"));
            Params.Add(new CalcParam("z0", "Coassiale: impedenza", 50, "Ω"));
            Params.Add(new CalcParam("vf", "Coassiale: fattore di velocita'", 0.66, "", "0,66 polietilene pieno (RG-58, RG-213), 0,8-0,85 dielettrico espanso."));
            Params.Add(new CalcParam("sb", "Bacchetta: distanza tra i conduttori", 20, "mm", "Da asse ad asse."));
            Params.Add(new CalcParam("db", "Bacchetta: diametro dei conduttori", 6, "mm"));
            Params.Add(new CalcParam("h", "Quota del 20 m (0 = spazio libero)", 0, "m", "Con quota maggiore di 0 il calcolo NEC usa il terreno medio (er 13, sigma 0,005 S/m)."));
        }

        string Firma() { StringBuilder s = new StringBuilder(); foreach (CalcParam p in Params) if (p.Key != "b" && p.Key != "t30" && p.Key != "t40") s.Append(p.Key + "=" + p.Value + ";"); return s.ToString(); }

        double Z0Linea(out double vf)
        {
            if (I("col") == 0) { vf = Math.Max(0.3, Math.Min(1, P("vf"))); return Math.Max(10, P("z0")); }
            vf = 0.97;
            double s = Math.Max(P("sb"), P("db") * 1.01), d = Math.Max(0.1, P("db"));
            return 120 * Math.Log(s / d + Math.Sqrt(s / d * (s / d) - 1));     // 120 acosh(s/d)
        }

        // lunghezze di meta' dipolo: accordate dal pulsante Risonanza, altrimenti quelle della prova con nec2c scalate sul raggio
        double L30(double k) { return !double.IsNaN(l30) ? l30 : 7.311 * k; }
        double L40(double k) { return !double.IsNaN(l40) ? l40 : 10.358 * k; }

        string Mazzo(double[] tdv, double[] trv, int banda)
        {
            double k = P("R") / Rj[0], a = Math.Max(0.05, P("d")) / 2000, h = P("h"), vf;
            double z0 = Z0Linea(out vf);
            double seg = C0 / Fb[4] / 40;
            int e6 = I("e6");
            NecDeck d = new NecDeck(Title + " " + NbTutte[banda]);
            int nb = e6 > 0 ? 6 : 5;
            int[] cen = new int[nb];
            double[] zq = new double[nb];
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
            if (e6 > 0)
            {
                // 1.0.10: 6 m di JN1GLB (proporzioni scalate come le altre bande)
                double z = h + (Z6 - Zj[0]) * k, seg6 = C0 / F6 / 40;
                zq[5] = z;
                cen[5] = d.W(1, 0, -Gap / 2, z, 0, Gap / 2, z, a);
                for (int sg = 1; sg >= -1; sg -= 2) d.Ws(seg6, 0, sg * Gap / 2, z, 0, sg * 1.38 * k, z, a, false, 3);
                d.Ws(seg6, -1.19 * k, -0.68 * k, z, -1.19 * k, 0.68 * k, z, a, false, 3);
                for (int sg = 1; sg >= -1; sg -= 2) d.Ws(seg6, -1.19 * k, sg * 0.68 * k, z, -0.48 * k, sg * 1.075 * k, z, a, false, 3);
                if (e6 == 2)
                {
                    d.Ws(seg6, 0.66 * k, -1.06 * k, z, 0.66 * k, 1.06 * k, z, a, false, 3);
                    for (int sg = 1; sg >= -1; sg -= 2) d.Ws(seg6, 0.66 * k, sg * 1.06 * k, z, 0.51 * k, sg * 1.15 * k, z, a, false, 1);
                }
            }
            // 1.0.10: dipoli 30 / 40 m lungo il W del 20 m e i bracci dell'esagono
            int d34 = I("d34"), cd30 = 0, cd40 = 0;
            double R20 = Rj[0] * k, zd = h - Math.Max(0.02, P("dz"));
            foreach (int bd in new[] { 6, 7 })
            {
                if (bd == 6 && d34 != 1 && d34 != 3) continue;
                if (bd == 7 && d34 != 2 && d34 != 3) continue;
                double Lh = bd == 6 ? L30(k) : L40(k), z = bd == 6 ? zd : zd - 0.10, segd = C0 / FreqB(bd) / 40;
                int cn = d.W(1, 0, -Gap / 2, z, 0, Gap / 2, z, a);
                if (bd == 6) cd30 = cn; else cd40 = cn;
                double c3 = R20 * Math.Cos(Math.PI / 6), s3 = R20 * Math.Sin(Math.PI / 6);
                for (int sg = 1; sg >= -1; sg -= 2)
                {
                    double[][] pt = { new[] { 0, sg * Gap / 2 }, new[] { c3, sg * s3 }, new[] { 0, sg * R20 }, new[] { -c3, sg * s3 }, new[] { -R20 + 0.05, 0.0 } };
                    double resto = Lh - Gap / 2;
                    for (int i = 0; i < pt.Length - 1 && resto > 1e-4; i++)
                    {
                        double[] p0 = pt[i], q = pt[i + 1];
                        double dd = Math.Sqrt((q[0] - p0[0]) * (q[0] - p0[0]) + (q[1] - p0[1]) * (q[1] - p0[1]));
                        if (dd > resto) { q = new[] { p0[0] + (q[0] - p0[0]) * resto / dd, p0[1] + (q[1] - p0[1]) * resto / dd }; dd = resto; }
                        d.Ws(segd, p0[0], p0[1], z, q[0], q[1], z, a, false, 2);
                        resto -= dd;
                    }
                }
            }
            d.Ground(h > 0 ? 2 : 0);
            // linee lungo il palo: lunghezza fisica = dislivello; nel NEC la lunghezza della TL e' elettrica (fisica / VF)
            for (int i = 0; i < nb - 1; i++) d.Tl(cen[i], 1, cen[i + 1], 1, z0, Math.Max(0.01, zq[i] - zq[i + 1]) / vf);
            if (banda == 6 && cd30 > 0 || banda == 7 && cd40 > 0)
            {
                d.Ld(0, cen[0], 1, 1, 50, 0, 0);
                if (banda == 6 && cd40 > 0) d.Ld(0, cd40, 1, 1, 50, 0, 0);
                if (banda == 7 && cd30 > 0) d.Ld(0, cd30, 1, 1, 50, 0, 0);
                d.Ex(banda == 6 ? cd30 : cd40, 1);
            }
            else
            {
                if (cd30 > 0) d.Ld(0, cd30, 1, 1, 50, 0, 0);
                if (cd40 > 0) d.Ld(0, cd40, 1, 1, 50, 0, 0);
                d.Ex(cen[0], 1);
            }
            return d.End(FreqB(banda));
        }

        Complex Zin(double[] tdv, double[] trv, int banda)
        {
            NecModel m = NecModel.Parse(Mazzo(tdv, trv, banda));
            SegmentGeometry g = m.BuildGeometry();
            Nec2Engine e = new Nec2Engine(g);
            double f = FreqB(banda);
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
            // 1.0.10: dipoli 30 / 40 m in risonanza (X = 0) cambiando la lunghezza, per bisezione
            double kk = P("R") / Rj[0];
            int d34 = I("d34");
            for (int giro = 0; giro < 2; giro++)
                foreach (int bd in new[] { 7, 6 })
                {
                    if (bd == 6 && d34 != 1 && d34 != 3 || bd == 7 && d34 != 2 && d34 != 3) continue;
                    double l0 = bd == 6 ? L30(kk) : L40(kk), lo = l0 * 0.85, hi = l0 * 1.12;
                    for (int it = 0; it < 22; it++)
                    {
                        double mid = (lo + hi) / 2;
                        if (bd == 6) l30 = mid; else l40 = mid;
                        if (Zin(td, tr, bd).Imaginary > 0) hi = mid; else lo = mid;
                    }
                }
            if (d34 == 1 || d34 == 3) z30 = Zin(td, tr, 6);
            if (d34 == 2 || d34 == 3) z40 = Zin(td, tr, 7);
            return 1;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            if (firma != null && firma != Firma()) { td = tr = null; zAcc = null; firma = null; l30 = l40 = double.NaN; z30 = z40 = Complex.Zero; }
            bool acc = td != null;
            double[] tdv = acc ? td : TdJ, trv = acc ? tr : TrJ;
            int banda = Math.Max(0, Math.Min(7, I("b")));
            int d34c = I("d34");
            if (banda == 6 && d34c != 1 && d34c != 3 || banda == 7 && d34c != 2 && d34c != 3) { banda = 0; o.Warnings.Add("Banda nel NEC " + NbTutte[I("b")] + " senza quel dipolo nel modello: sceglierlo nella riga \"Dipoli 30 / 40 m\"; il calcolo resta sul 20 m."); }
            if (banda == 5 && I("e6") == 0) { banda = 0; o.Warnings.Add("Banda nel NEC 6 m senza il 6 m nel modello: scegliere 2 o 3 elementi nella riga \"6 m (50 MHz)\"; il calcolo resta sul 20 m."); }
            double k = P("R") / Rj[0], vf, z0 = Z0Linea(out vf);
            for (int i = 0; i < 5; i++)
            {
                double R = Rj[i] * k, alim = R + tdv[i] * R + Gap / 2, rifl = 3 * R + 2 * trv[i] * R, spazio = (1 - tdv[i] - trv[i]) * R;
                string sub = "riflettore " + Calcolatori.N(rifl, 3) + " m, tra le punte " + Calcolatori.N(spazio, 3) + " m, raggio " + Calcolatori.N(R, 3) + " m";
                if (acc) sub += ", Z " + Calcolatori.Cx(zAcc[i]);
                o.Card(Nb[i], "alim. 2 x " + Calcolatori.N(alim, 3) + " m", sub, i == banda);
            }
            if (I("e6") > 0)
            {
                double k6 = P("R") / Rj[0];
                string s6 = "rifl. U " + Calcolatori.N((1.36 + 2 * Math.Sqrt(0.71 * 0.71 + 0.395 * 0.395)) * k6, 3) + " m";
                o.Card("6 m (JN1GLB)", (I("e6") == 2 ? "3 el" : "2 el") + ", alim. 2 x " + Calcolatori.N(1.38 * k6, 3) + " m",
                    s6 + ", " + Calcolatori.N((Zj[4] - Z6) * k6, 2) + " m sotto il 10 m" + (I("e6") == 2 ? ", direttore a U" : "") + "; nec2c: " + (I("e6") == 2 ? "33 +j11 Ω, 6,96 dBi, F/B 19,6 dB" : "44 +j6,5 Ω, 6,50 dBi, F/B 14,6 dB"), banda == 5);
                o.Oks.Add("6 m con le misure di JN1GLB (non accordate dal pulsante Risonanza, che lavora sulle bande 20-10 m)");
            }
            if (d34c > 0)
            {
                foreach (int bd in new[] { 6, 7 })
                {
                    if (bd == 6 && d34c != 1 && d34c != 3 || bd == 7 && d34c != 2 && d34c != 3) continue;
                    double lh = bd == 6 ? L30(k) : L40(k);
                    Complex zz = bd == 6 ? z30 : z40;
                    bool fatto = bd == 6 ? !double.IsNaN(l30) : !double.IsNaN(l40);
                    double zt = 50 / Tr34n[Math.Max(0, Math.Min(3, I(bd == 6 ? "t30" : "t40")))];
                    double gm = fatto ? ((zz - zt) / (zz + zt)).Magnitude : 0;
                    string ros = fatto ? ", ROS " + Calcolatori.N(gm >= 0.999 ? 999 : (1 + gm) / (1 - gm), 2) + " su " + Calcolatori.N(zt, 1) + " Ω" : "";
                    o.Card("Dipolo " + NbTutte[bd], "2 x " + Calcolatori.N(lh, 3) + " m",
                        (fatto ? "in risonanza, Z " + Calcolatori.Cx(zz) + ros : "lunghezza di partenza (pulsante Risonanza per accordarlo)") + "; lungo il W del 20 m e i bracci, " + Calcolatori.N(P("dz") + (bd == 7 ? 0.10 : 0), 2) + " m sotto il 20 m", banda == bd);
                }
                o.Warnings.Add("Dipoli 30 / 40 m piegati attorno all'esagono: le punte tornano verso il retro e la resistenza di radiazione e' di pochi ohm (prova con nec2c: 40 m 3 Ω, 30 m 6 Ω), quindi serve un adattamento: scegliere il trasformatore di ogni dipolo (prova con nec2c, rame 1 mm: 30 m 7,7 Ω, ROS 1,7 con il 4:1 al contrario; 40 m 5,1 Ω, ROS 2,5 con il 4:1 e 1,4 con il 9:1, banda di poche decine di kHz). Con resistenze cosi' basse il filo perde molto: 40 m circa meta' della potenza con 1 mm di rame, meglio un conduttore grosso.");
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
            if (d34c == 0) o.Warnings.Add("Il kit 30/40 m e' separato, con il suo balun e la sua discesa: si aggiunge con la riga \"Dipoli 30 / 40 m\".");
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
            o.Chart.MarkX = banda >= 5 ? (banda == 5 ? Fb[4] : Fb[0]) : Fb[banda]; o.Chart.MarkText = NbTutte[banda];
            return o;
        }
    }
}
