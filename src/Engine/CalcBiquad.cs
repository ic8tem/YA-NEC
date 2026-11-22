// YA-NEC 1.0.16 - Calcolatori > 2,4 GHz COMMERCIALE > Biquad e doppia biquad (bozzetto approvato da IC8TEM il 17/11/2026 17:25).
// Elemento a due (o quattro) quadrati di lato circa lambda/4 davanti a un riflettore pieno (lamiera, PCB) o a rete, con bordi rialzati facoltativi.
// Il modello NEC segue la convenzione dei feed di YA-NEC (come Trombe e illuminatori): elemento nel piano z = 0, fascio verso +Z,
// polarizzazione lungo Y (la fila dei quadrati), riflettore a z = -d; cosi' "Feed dal modello NEC" della parabola lo legge senza conversioni.
// Fonti: T. Marshall (BYTE 10/2001, trevormarshall.com), martybugs.net, W. Zwickel OE2TZL (QSP 10/93), W6PQL, IK1ZYW (2012),
// G6GVI e MW1FGQ, buildyourownantenna (2014, doppia biquad), nec2.org (riflettore a griglia 0,05-0,1 lambda), MathWorks (biquad Yagi),
// T.Y. Otoshi (rete), A. Rubinstein e altri 2005 (raggio dei fili della griglia).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YaNec.Engine
{
    public class CalcBiquad : SimpleCalc
    {
        public static readonly double[] Bande = { 1296.2, 2320.2, 2442, 3400.1, 5760.1 };
        double fPrec = double.NaN;
        // banda dopo "Porta in adattamento": ROS e guadagno
        public double[] SweepF, SweepSwr, SweepG, SweepFb;
        string firmaSweep;
        public int UltimiSegmenti;

        public CalcBiquad()
        {
            Key = "biquad"; Title = "Biquad e doppia biquad";
            Source = "T. Marshall (BYTE 10/2001); martybugs.net; W. Zwickel OE2TZL (QSP 10/93); W6PQL; IK1ZYW; G6GVI e MW1FGQ; buildyourownantenna (doppia biquad, 2014); nec2.org; T.Y. Otoshi";
            CalcParam b = CalcParam.Scelta("banda", "Banda", 2, "Banda di lavoro: la frequenza e tutte le misure in mm si riscalano. \"libera\" lascia scrivere la frequenza.",
                "1296", "2320", "2442 Wi-Fi", "3400", "5760", "libera");
            b.Pillole = true;
            Params.Add(b);
            Params.Add(new CalcParam("f", "Frequenza", 2442, "MHz", "Frequenza di progetto. Con una banda scelta e' grigia; con \"libera\" si scrive. Cambiandola, lato, distanza e riflettore si riscalano."));
            Params.Add(CalcParam.Scelta("tipo", "Tipo", 0, "Biquad: due quadrati (8 lati di filo). Doppia biquad: quattro quadrati in fila (16 lati), i fili si incrociano senza toccarsi tra il primo e il secondo quadrato di ogni lato.",
                "Biquad (2 quadrati)", "Doppia biquad (4)"));
            Params.Add(new CalcParam("lato", "Lato del quadrato", 30.5, "mm", "Da centro filo a centro filo, circa lambda/4 (Marshall 30,5 mm a 2,4 GHz)."));
            Params.Add(new CalcParam("filo", "Diametro del filo", 1.6, "mm", "Rame da 1,2-2 mm nelle fonti."));
            Params.Add(CalcParam.Scelta("iso", "Filo", 0, "Il NEC-2 non modella l'isolante: le misure calcolate sono quelle del filo nudo. Con il filo isolato l'elemento va accorciato (W6PQL: lato 0,238 lambda invece di 0,248, circa il 4 % in meno).",
                "nudo", "isolato"));
            Params.Add(new CalcParam("dist", "Distanza dal riflettore", 15, "mm", "Dal piano dei quadrati alla superficie del riflettore: e' la regolazione del ROS (0,1-0,13 lambda nelle fonti)."));
            Params.Add(CalcParam.Scelta("rifl", "Riflettore", 0, "Pieno: lamiera o PCB, nel NEC una griglia fitta (0,07 lambda, raggio dei fili con l'area uguale). Rete: la maglia vera con il suo filo.",
                "Pieno (lamiera, PCB)", "Rete"));
            Params.Add(new CalcParam("rw", "Larghezza del riflettore", 123, "mm", "Lato perpendicolare alla fila dei quadrati (Marshall: 123 mm da sola, 110 mm come feed di una parabola)."));
            Params.Add(new CalcParam("rh", "Altezza del riflettore", 123, "mm", "Lato lungo la fila dei quadrati (la polarizzazione). Doppia biquad: circa 2,3 lambda."));
            CalcParam mx = new CalcParam("mx", "Rete: maglia lungo la larghezza", 12.7, "mm", "Passo dei fili della rete (W6PQL e G6GVI: rete da 1/2 x 1 pollice).");
            CalcParam my = new CalcParam("my", "Rete: maglia lungo l'altezza", 25.4, "mm");
            CalcParam mf = new CalcParam("mf", "Rete: diametro del filo", 0.8, "mm");
            Func<bool> rete = () => I("rifl") == 1;
            mx.Visibile = rete; my.Visibile = rete; mf.Visibile = rete;
            Params.Add(mx); Params.Add(my); Params.Add(mf);
            Params.Add(CalcParam.Scelta("bordi", "Bordi rialzati", 0, "Bordi piegati in avanti (verso l'elemento). 2 lati: i due bordi perpendicolari alla polarizzazione (alto e basso della fila dei quadrati). Marshall: bordi di 30 mm, lobi posteriori circa 6 dB piu' bassi.",
                "nessuno", "2 lati", "4 lati"));
            CalcParam lip = new CalcParam("lip", "Altezza dei bordi", 30, "mm");
            lip.Visibile = () => I("bordi") > 0;
            Params.Add(lip);
            Params.Add(new CalcParam("z0", "Impedenza di riferimento", 50, "Ω", "Per il ROS (50 ohm con il coassiale direttamente sul punto di alimentazione, come in tutte le fonti)."));
            NotaFissa = "Il lato e' circa λ/4 da centro a centro del filo; la distanza dal riflettore (0,1-0,13 λ nelle fonti) e' la regolazione del ROS. Con la rete il riflettore e' una griglia di fili con la maglia vera (W1GHZ, Otoshi: maglia sotto λ/10).";
        }

        public override string TuneLabel { get { return "Porta in adattamento (NEC)"; } }

        string Firma()
        {
            StringBuilder s = new StringBuilder();
            foreach (CalcParam p in Params) s.Append(p.Key).Append('=').Append(p.Value.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            return s.Append("sc=").Append(Scale.ToString("R", CultureInfo.InvariantCulture)).ToString();
        }

        /// <summary>Frequenza e riscalatura delle misure quando cambia la frequenza.</summary>
        double Frequenza()
        {
            int b = I("banda");
            double f = b >= 0 && b < Bande.Length ? Bande[b] : Math.Max(100, P("f"));
            if (b >= 0 && b < Bande.Length) { Par("f").Value = f; Calcolati["f"] = f; }
            if (!double.IsNaN(fPrec) && Math.Abs(f - fPrec) > 1e-6)
            {
                double k = fPrec / f;
                foreach (string key in new[] { "lato", "dist", "rw", "rh", "lip" }) Par(key).Value = Math.Round(Par(key).Value * k, 2);
                Scale = 1;
            }
            fPrec = f;
            return f;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = Frequenza(), lam = C0 / f;                       // m
            bool doppia = I("tipo") == 1, rete = I("rifl") == 1;
            double s = Math.Max(1, P("lato")) / 1000 * Scale, a = Math.Max(0.1, P("filo")) / 2000, d = Math.Max(1, P("dist")) / 1000;
            double rw = Math.Max(10, P("rw")) / 1000, rh = Math.Max(10, P("rh")) / 1000;
            int bordi = I("bordi"); double lip = Math.Max(0, P("lip")) / 1000;
            double h = s / Math.Sqrt(2), g = Math.Max(0.0015, 4 * a), zc = Math.Max(0.0015, 4 * a);
            double seg = lam / 30;
            NecDeck dk = new NecDeck(Title + " " + Calcolatori.N(f, 1) + " MHz");
            // ---- elemento: nodi di alimentazione R (+x) e L (-x), fila dei quadrati lungo Y
            double xr = g / 2, xl = -g / 2;
            int tFeed = dk.W(1, xl, 0, 0, xr, 0, 0, a);
            double filo = 0;
            Action<double, double, double, double, double, double> w = (x1, y1, z1, x2, y2, z2) =>
            {
                dk.Ws(seg, x1, y1, z1, x2, y2, z2, a, false, 2);
                filo += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1) + (z2 - z1) * (z2 - z1));
            };
            foreach (double sy in new[] { 1.0, -1.0 })
            {
                if (!doppia)
                {
                    w(xr, 0, 0, h, sy * h, 0); w(h, sy * h, 0, 0, sy * 2 * h, 0); w(0, sy * 2 * h, 0, -h, sy * h, 0); w(-h, sy * h, 0, xl, 0, 0);
                }
                else
                {
                    // R -> destro del 1o -> (incrocio) -> sinistro del 2o -> punta -> destro del 2o -> (incrocio, ponte a z = zc) -> sinistro del 1o -> L
                    w(xr, 0, 0, h, sy * h, 0);
                    w(h, sy * h, 0, -h, sy * 3 * h, 0);
                    w(-h, sy * 3 * h, 0, 0, sy * 4 * h, 0);
                    w(0, sy * 4 * h, 0, h, sy * 3 * h, 0);
                    double dl = Math.Min(0.25, 2 * zc / (2 * Math.Sqrt(2) * h));     // meta' del ponte, in frazione della diagonale
                    double qx1 = h + (-2 * h) * (0.5 - dl), qy1 = sy * (3 * h + (-2 * h) * (0.5 - dl));
                    double qx2 = h + (-2 * h) * (0.5 + dl), qy2 = sy * (3 * h + (-2 * h) * (0.5 + dl));
                    w(h, sy * 3 * h, 0, qx1, qy1, zc); w(qx1, qy1, zc, qx2, qy2, zc); w(qx2, qy2, zc, -h, sy * h, 0);
                    w(-h, sy * h, 0, xl, 0, 0);
                }
            }
            filo += g;
            double estY = doppia ? 4 * h : 2 * h;
            // ---- riflettore a griglia, z = -d
            double pas = lam / 14, ag = 0;
            double px, py; bool reteVera = rete;
            if (rete)
            {
                px = Math.Max(0.5, P("mx")) / 1000; py = Math.Max(0.5, P("my")) / 1000; ag = Math.Max(0.05, P("mf")) / 2000;
                int stima = (int)(Math.Ceiling(rw / px) + 1) * (int)Math.Ceiling(rh / py) + (int)(Math.Ceiling(rh / py) + 1) * (int)Math.Ceiling(rw / px);
                if (stima > 1800) { reteVera = false; px = py = pas; }
            }
            else { px = py = pas; }
            int nx = Math.Max(2, (int)Math.Ceiling(rw / px)), ny = Math.Max(2, (int)Math.Ceiling(rh / py));
            double dx = rw / nx, dy = rh / ny;
            if (!reteVera) ag = Math.Min(dx, dy) / (2 * Math.PI);
            double zr = -d;
            for (int j = 0; j <= ny; j++) dk.W(nx, -rw / 2, -rh / 2 + j * dy, zr, rw / 2, -rh / 2 + j * dy, zr, ag);
            for (int i = 0; i <= nx; i++) dk.W(ny, -rw / 2 + i * dx, -rh / 2, zr, -rw / 2 + i * dx, rh / 2, zr, ag);
            // ---- bordi rialzati (verso +Z): 2 lati = y = +-rh/2 (perpendicolari alla polarizzazione), 4 lati = anche x = +-rw/2
            if (bordi > 0 && lip > 0.001)
            {
                int nz = Math.Max(1, (int)Math.Ceiling(lip / Math.Min(dx, dy)));
                double dz = lip / nz;
                foreach (double yy in new[] { -rh / 2, rh / 2 })
                {
                    for (int k = 1; k <= nz; k++) dk.W(nx, -rw / 2, yy, zr + k * dz, rw / 2, yy, zr + k * dz, ag);
                    for (int i = 0; i <= nx; i++) dk.W(nz, -rw / 2 + i * dx, yy, zr, -rw / 2 + i * dx, yy, zr + lip, ag);
                }
                if (bordi == 2)
                    foreach (double xx in new[] { -rw / 2, rw / 2 })
                    {
                        for (int k = 1; k <= nz; k++) dk.W(ny, xx, -rh / 2, zr + k * dz, xx, rh / 2, zr + k * dz, ag);
                        for (int j = 1; j < ny; j++) dk.W(nz, xx, -rh / 2 + j * dy, zr, xx, -rh / 2 + j * dy, zr + lip, ag);
                    }
            }
            dk.Ex(tFeed, 1);
            // tagli come i feed delle trombe: phi 90 (piano YZ), 0 (XZ), 45
            dk.Raw("RP 0 181 1 1000 0 90 1 0");
            dk.Raw("RP 0 181 1 1000 0 0 1 0");
            dk.Raw("RP 0 181 1 1000 0 45 1 0");
            o.Nec = dk.End(f);
            o.Plane = "xy";
            try { UltimiSegmenti = NecModel.Parse(o.Nec).BuildGeometry().N; } catch { UltimiSegmenti = 0; }

            // ---- riquadri e righe
            double latoMm = s * 1000;
            o.Card("Lato del quadrato", Calcolatori.N(latoMm, 1) + " mm", Calcolatori.N(s / lam, 3) + " λ, centro filo" + (Math.Abs(Scale - 1) > 1e-4 ? " (adattato dal NEC)" : ""), true);
            o.Card("Filo da tagliare", Calcolatori.N(filo * 1000, 0) + " mm", (doppia ? "16" : "8") + " lati + spazio al centro di " + Calcolatori.N(g * 1000, 1) + " mm");
            o.Card("Distanza dal riflettore", Calcolatori.N(d * 1000, 1) + " mm", Calcolatori.N(d / lam, 3) + " λ");
            o.Card("Riflettore", Calcolatori.N(rw * 1000, 0) + " x " + Calcolatori.N(rh * 1000, 0) + " mm", Calcolatori.N(rw / lam, 2) + " x " + Calcolatori.N(rh / lam, 2) + " λ");
            o.Card("Lunghezza d'onda", Calcolatori.N(lam * 1000, 1) + " mm", Calcolatori.N(f, 1) + " MHz");
            o.Card("Modello NEC", UltimiSegmenti + " segmenti", reteVera ? "rete con la maglia vera" : rete ? "rete come superficie piena (maglia troppo fitta da calcolare)" : "griglia 0,07 λ");
            o.Card("Elemento", Calcolatori.N(2 * h * 1000 + a * 2000, 0) + " x " + Calcolatori.N(estY * 2 * 1000 + a * 2000, 0) + " mm", "larghezza x altezza (lungo la polarizzazione)");

            // ---- banda dopo l'adattamento
            if (SweepF != null && firmaSweep == Firma())
            {
                o.Chart = new CalcChart { Title = "ROS a " + Calcolatori.N(P("z0"), 0) + " Ω in banda (dopo \"Porta in adattamento\")", XLabel = "MHz", YLabel = "ROS", X = SweepF, Y = SweepSwr, MarkX = f, MarkText = Calcolatori.N(f, 1), YMin = 1 };
                int c = SweepF.Length / 2;
                o.Card("F/B lungo l'asse (NEC)", Calcolatori.N(SweepFb[c], 1) + " dB", "bordi della banda " + Calcolatori.N(SweepFb[0], 1) + " e " + Calcolatori.N(SweepFb[SweepFb.Length - 1], 1) + " dB");
                o.Card("Guadagno (NEC)", Calcolatori.N(SweepG[c], 2) + " dBi", "bordi della banda " + Calcolatori.N(SweepG[0], 1) + " e " + Calcolatori.N(SweepG[SweepG.Length - 1], 1) + " dBi", true);
                double smax = 0; foreach (double v in SweepSwr) smax = Math.Max(smax, v);
                o.Card("ROS in banda", "max " + Calcolatori.N(smax, 2), Calcolatori.N(SweepF[0], 0) + "-" + Calcolatori.N(SweepF[SweepF.Length - 1], 0) + " MHz, centro " + Calcolatori.N(SweepSwr[c], 2), true);
            }

            // ---- avvisi
            if (s / lam < 0.22 || s / lam > 0.28) o.Warnings.Add("Lato di " + Calcolatori.N(s / lam, 3) + " λ: le fonti stanno fra 0,238 (W6PQL, filo isolato) e 0,264 λ (OE2TZL).");
            if (d / lam < 0.06 || d / lam > 0.16) o.Warnings.Add("Distanza dal riflettore di " + Calcolatori.N(d / lam, 3) + " λ: le fonti stanno fra 0,09 e 0,13 λ. IK1ZYW: da 1/8 a 1/16 λ si perdono circa 0,5 dB di guadagno e 3 dB di F/B.");
            if (rw < 0.7 * lam || rh < estY * 2 + 0.3 * lam) o.Warnings.Add("Riflettore piccolo: il guadagno scende e il lobo posteriore cresce (Marshall: 123 mm a 2,4 GHz, cioe' circa 1 λ, per la biquad da sola).");
            if (rete && reteVera && Math.Max(px, py) > lam / 10) o.Warnings.Add("Maglia della rete di " + Calcolatori.N(Math.Max(px, py) * 1000, 1) + " mm, sopra λ/10 (" + Calcolatori.N(lam * 100, 1) + " mm): parte della potenza passa dietro (W1GHZ, Otoshi). Il NEC la calcola con la maglia vera.");
            if (rete && !reteVera) o.Warnings.Add("Rete troppo fitta per il calcolo (oltre 1800 segmenti): nel NEC il riflettore e' una griglia di 0,07 λ, come se fosse pieno. Con maglie sotto λ/10 la differenza e' piccola.");
            if (I("iso") == 1) o.Warnings.Add("Filo isolato: il NEC-2 lo calcola come nudo. L'isolante accorcia l'elemento: W6PQL usa 0,238 λ di lato con il filo isolato contro 0,248 λ di Marshall con il filo nudo (circa il 4 % in meno).");
            if (UltimiSegmenti > 2500) o.Warnings.Add("Modello pesante (" + UltimiSegmenti + " segmenti): il calcolo richiede qualche secondo.");
            o.Oks.Add("Alimentazione diretta con il coassiale al centro, senza balun (Marshall, OE2TZL, W6PQL): il centrale va da un lato, la calza dall'altro.");

            o.Formula = "Geometria: quadrati di lato s (circa λ/4 da centro filo a centro filo) disposti a rombo lungo la polarizzazione, alimentati al centro tra due nodi distanti " +
                "max(1,5 mm; 4 raggi). Doppia biquad: quattro quadrati in fila; il filo si incrocia tra il primo e il secondo quadrato di ogni lato senza toccarsi (ponte di " +
                Calcolatori.N(zc * 1000, 1) + " mm nel modello). Riflettore: griglia di fili a z = -d, passo 0,07 λ e raggio con l'area uguale (2 pi a = passo, Rubinstein 2005) se pieno, " +
                "maglia e filo veri se a rete (W1GHZ, Otoshi: per ogni direzione dei fili passa (2 s v / λ)^2, v = ln[1 / (1 - e^(-pi d / s))]). Riferimento come i feed di YA-NEC: elemento a z = 0, fascio verso +Z, polarizzazione lungo Y. " +
                "\"Porta in adattamento (NEC)\" regola il lato per la risonanza e la distanza per il ROS minimo, poi calcola ROS e guadagno su +-5 % di banda.\n" +
                "Fonti a confronto (somiglianze: tutti usano un lato vicino a λ/4 e regolano il ROS con la distanza dal riflettore; differenze: lato e distanza): " +
                "T. Marshall (BYTE 10/2001): 2,4 GHz, lato 30,5 mm (0,248 λ), filo 1,2 mm, 15 mm (0,122 λ), riflettore 123 mm (110 mm come feed), bordi di 30 mm su due lati, 11 dBi. " +
                "martybugs.net: filo 1,6 mm, 15-17 mm, PCB 123 mm, 11-12 dBi, doppia biquad circa 13 dBi, apertura 50 gradi. " +
                "W. Zwickel OE2TZL (QSP 10/93): 23 cm, lato 61 mm (0,258 λ a 1270 MHz), filo 2 mm, 31 mm (0,131 λ), lamiera, 8 dBd, ROS sotto 1,6 in banda. " +
                "W6PQL: 23 cm, lato 55 mm (0,238 λ) con filo #12 isolato, 22 mm (0,095 λ), rete da 1/2 x 1 pollice di circa 20 x 25 cm, circa 9 dBi, 60 gradi, F/B oltre 25 dB. " +
                "IK1ZYW (2012, MMANA): da 1/8 λ a 15 mm (1/16 λ) circa -0,5 dB e -3 dB di F/B. " +
                "G6GVI (da MW1FGQ): doppia biquad, alluminio piu' rete da 1/2 x 1 pollice, circa 12,5 dBi stimati a 3,4 GHz. " +
                "buildyourownantenna (2014, 4nec2): doppia biquad a 2450 MHz, lato 30,9 mm, 11 mm, riflettore 132 x 282 mm con bordi di 28,5 mm, circa 14 dBi. " +
                "MathWorks (Antenna Toolbox): biquad Yagi a 2,4 GHz con 4 direttori biquad, solo grafici. " +
                "Le fonti dichiarano guadagni da 8 dBd a 14 dBi su riflettori diversi: il NEC della scheda li mette sullo stesso piano.";
            return o;
        }

        /// <summary>Percorso del filo in mm nel piano dell'elemento (x, y), dal nodo destro al sinistro passando per i quadrati di sopra,
        /// poi dal sinistro al destro per quelli di sotto; s = lato usato (mm), g = spazio al centro (mm).</summary>
        public List<double[]> Percorso(out double s, out double g)
        {
            s = Math.Max(1, P("lato")) * Scale;
            double a = Math.Max(0.1, P("filo")) / 2;
            g = Math.Max(1.5, 4 * a);
            double h = s / Math.Sqrt(2), xr = g / 2, xl = -g / 2;
            List<double[]> pt = new List<double[]>();
            if (I("tipo") == 0)
            {
                pt.Add(new[] { xr, 0.0 }); pt.Add(new[] { h, h }); pt.Add(new[] { 0.0, 2 * h }); pt.Add(new[] { -h, h }); pt.Add(new[] { xl, 0.0 });
                pt.Add(new[] { -h, -h }); pt.Add(new[] { 0.0, -2 * h }); pt.Add(new[] { h, -h }); pt.Add(new[] { xr, 0.0 });
            }
            else
            {
                pt.Add(new[] { xr, 0.0 }); pt.Add(new[] { h, h }); pt.Add(new[] { -h, 3 * h }); pt.Add(new[] { 0.0, 4 * h }); pt.Add(new[] { h, 3 * h }); pt.Add(new[] { -h, h }); pt.Add(new[] { xl, 0.0 });
                pt.Add(new[] { -h, -h }); pt.Add(new[] { h, -3 * h }); pt.Add(new[] { 0.0, -4 * h }); pt.Add(new[] { -h, -3 * h }); pt.Add(new[] { h, -h }); pt.Add(new[] { xr, 0.0 });
            }
            return pt;
        }

        // ---- "Porta in adattamento (NEC)": lato per la risonanza, distanza per il ROS minimo, poi la banda
        RunResult Calcola(double fmhz)
        {
            CalcOutput o = Compute();
            NecModel m = NecModel.Parse(o.Nec);
            m.Z0 = Math.Max(1, P("z0"));
            SegmentGeometry g = m.BuildGeometry();
            return Analysis.Run(m, g, fmhz, 5.0);
        }

        double Rosso(double fmhz)
        {
            CalcOutput o = Compute();
            NecModel m = NecModel.Parse(o.Nec);
            SegmentGeometry g = m.BuildGeometry();
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(fmhz, Analysis.EffectiveSources(m, g, fmhz), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fmhz));
            System.Numerics.Complex z = e.Sources[0].Z;
            double z0 = Math.Max(1, P("z0"));
            double gm = ((z - z0) / (z + z0)).Magnitude;
            return gm;
        }

        /// <summary>F/B lungo l'asse del feed: guadagno verso +Z (theta 0) contro -Z (theta 180). Il F/B generale di YA-NEC guarda l'orizzonte.</summary>
        public static double FronteRetro(RunResult r)
        {
            if (r == null || r.Engine == null) return double.NaN;
            return r.Engine.Gain(0, 0).GainTotal - r.Engine.Gain(180, 0).GainTotal;
        }

        public override double Tune(double fmhz)
        {
            double f = Frequenza(), lam = C0 / f;
            CalcParam pd = Par("dist");
            for (int giro = 0; giro < 2; giro++)
            {
                base.Tune(f);   // lato: reattanza nulla
                // distanza: sezione aurea tra 0,05 e 0,16 lambda
                double lo = 0.05 * lam * 1000, hi = 0.16 * lam * 1000, gr = (Math.Sqrt(5) - 1) / 2;
                double x1 = hi - gr * (hi - lo), x2 = lo + gr * (hi - lo);
                pd.Value = x1; double f1 = Rosso(f);
                pd.Value = x2; double f2 = Rosso(f);
                for (int it = 0; it < 10; it++)
                {
                    if (f1 < f2) { hi = x2; x2 = x1; f2 = f1; x1 = hi - gr * (hi - lo); pd.Value = x1; f1 = Rosso(f); }
                    else { lo = x1; x1 = x2; f1 = f2; x2 = lo + gr * (hi - lo); pd.Value = x2; f2 = Rosso(f); }
                }
                pd.Value = Math.Round(f1 < f2 ? x1 : x2, 2);
            }
            // banda +-5 %
            int n = 11;
            SweepF = new double[n]; SweepSwr = new double[n]; SweepG = new double[n]; SweepFb = new double[n];
            for (int i = 0; i < n; i++)
            {
                double fi = f * (0.95 + 0.1 * i / (n - 1));
                RunResult r = Calcola(fi);
                SweepF[i] = fi; SweepSwr[i] = r.Swr; SweepG[i] = r.GmaxDbi; SweepFb[i] = FronteRetro(r);
            }
            firmaSweep = Firma();
            return Scale;
        }
    }
}
