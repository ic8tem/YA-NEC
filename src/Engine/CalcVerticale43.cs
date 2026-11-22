// YA-NEC 1.5.9 - Calcolatori > HF > "Verticale non risonante (43 piedi)" (bozzetto approvato da IC8TEM il 22/11/2026 21:22).
// Verticale di lunghezza fissa (di solito 13,1 m = 43 piedi) che non risuona su nessuna banda, con un-un 4:1 alla base e accordatore in
// stazione, accordatore alla base oppure alimentazione diretta. Per ogni banda: Z alla base, ROS sul coassiale, perdita nel coassiale con
// il ROS (linea con perdite, come Cavi RG), perdita dell'accordatore (stima di Linea aperta + accordatore), efficienza, guadagno e angolo
// sul terreno reale, guadagno al trasmettitore, tensione alla base.
// Senza il NEC: impedenza dal modello TF5NN v3 (monopolo su terra ideale) piu' la resistenza di terra. Con «Calcola le bande (NEC)»:
// impedenza, guadagno, angolo ed efficienza dal motore NEC-2 con il terreno di Sommerfeld; radiali interrati come resistenza di terra
// in serie (ON4UN Tab. 9-1, il NEC-2 non tratta i fili interrati), radiali rialzati come fili veri.
// Fonte: P. Salas AD5X, «The 43-Foot Vertical» (presentazione di N0CU, 2016): numeri citati per il confronto, non riprodotti.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace YaNec.Engine
{
    public class CalcVerticale43 : SimpleCalc
    {
        public static readonly string[] Adattamenti = { "un-un 4:1 + accordatore in stazione", "accordatore alla base", "diretta (1:1) + accordatore in stazione" };
        public static readonly string[] CaviNomi = { "RG-58", "RG-8X", "RG-213", "Aircell 7", "H-2000 Flex", "Ecoflex 10", "LMR-400" };
        static readonly string[] CaviCerca = { "RG-58", "RG-8X", "RG-213", "Aircell 7", "H-2000", "Ecoflex 10", "LMR-400" };
        static readonly string[] TerreniNomi = { "povero", "medio", "buono", "mare" };
        static readonly double[][] Terreni = { new[] { 5, 0.001 }, new[] { 13, 0.005 }, new[] { 20, 0.0303 }, new[] { 81, 5.0 } };
        static readonly string[] BandeDa = { "160-10 m", "80-10 m", "60-10 m", "40-10 m" };
        const double Qu = 150;   // Q delle bobine dell'accordatore (stima)

        // AD5X (via N0CU 2016), guadagno e angolo su terreno reale: solo per il confronto
        static readonly Dictionary<int, string> Ad5x = new Dictionary<int, string>
        {
            { 0, "-8 dBi a 23°" }, { 1, "-2 dBi a 29°" }, { 3, "0 dBi a 25°" }, { 5, "1 dBi a 16°" }, { 7, "4 dBi a 37°" }, { 9, "5 dBi a 57°" }
        };

        /// <summary>Risultato del NEC per banda (pulsante «Calcola le bande (NEC)»).</summary>
        class RigaNec { public double F; public Complex Z; public double G, El, Eff; }
        List<RigaNec> nec;
        string firma;

        /// <summary>Per il rimando a Cavi RG: indice del cavo in CalcCavi.Lista e carico visto dal cavo alla banda piu' bassa.</summary>
        public int IndiceCavo = -1;
        public Complex CaricoCavo = new Complex(50, 0);

        public override string TuneLabel { get { return "Calcola le bande (NEC)"; } }

        public CalcVerticale43()
        {
            Key = "vert43"; Title = "Verticale non risonante (43 piedi)"; Family = "HF";
            Source = "P. Salas AD5X, The 43-Foot Vertical (presentazione di N0CU, 2016: Rr, ROS con il 4:1, perdite nel coassiale, guadagni e angoli, tensione alla base); " +
                     "G. B. Gudlaugsson TF5NN, HF Antenna Impedance Visualizer v3 (CC BY 4.0, impedenza del monopolo); ON4UN, Low-Band DXing cap. 9 (resistenza di terra dei radiali interrati, Tab. 9-1); " +
                     "abaco dei cavi di YA-NEC (perdite da catalogo); accordatore come in Linea aperta + accordatore (Q della bobina)";
            Func<bool> interrati = () => I("rad") == 0;
            Params.Add(new CalcParam("h", "Altezza della verticale", 13.1, "m", "43 piedi = 13,1 m (AD5X). Si usano anche 26 piedi (7,9 m), 31 piedi (9,4 m) e 50 piedi (15,2 m).").ConSezione("1 · Antenna"));
            Params.Add(new CalcParam("d", "ø del tubo (medio)", 35, "mm", "Diametro medio dei tubi telescopici."));
            Params.Add(CalcParam.Scelta("bnd", "Bande", 0, "Le bande della tabella: dalla piu' bassa scelta fino ai 10 m.", BandeDa));
            Params.Add(CalcParam.Scelta("ad", "Adattamento alla base", 0, "Un-un 4:1 alla base e accordatore in stazione (la soluzione piu' comune); accordatore automatico alla base (il coassiale lavora adattato); oppure coassiale collegato direttamente alla base.", Adattamenti));
            Params.Add(new CalcParam("lc", "Coassiale fino alla stazione", 30, "m"));
            Params.Add(new CalcParam("cv", "Tipo di coassiale", 2, "") { Choices = CaviNomi, StessaRiga = false });
            Params.Add(new CalcParam("pw", "Potenza", 100, "W"));
            Params.Add(new CalcParam("nr", "Numero di radiali", 16, "", "AD5X: piu' radiali possibile; con pochi radiali l'efficienza sulle bande basse crolla.").ConSezione("2 · Terra e radiali"));
            Params.Add(new CalcParam("lr", "Lunghezza dei radiali", 10, "m"));
            Params.Add(CalcParam.Scelta("rad", "Radiali", 0, "Interrati o appoggiati: il NEC-2 non li tratta, la loro perdita entra come resistenza di terra in serie alla base. Rialzati: fili veri nel modello NEC, sopra il terreno di Sommerfeld.", "interrati", "rialzati"));
            Params.Add(new CalcParam("hr", "Altezza dei radiali rialzati", 2.5, "m") { Visibile = () => I("rad") == 1 });
            Params.Add(new CalcParam("terr", "Terreno", 1, "") { Choices = TerreniNomi, Pillole = true });
            Params.Add(Vis(CalcParam.Scelta("rgm", "Resistenza di terra", 0, "Dai radiali con la Tab. 9-1 di ON4UN (numero e lunghezza in λ, banda per banda), oppure un valore fisso come AD5X (10 Ω).", "dai radiali (ON4UN)", "valore fisso"), interrati));
            Params.Add(new CalcParam("rg", "Rg fissa", 10, "Ω") { Visibile = () => I("rad") == 0 && I("rgm") == 1 });
            CanTune = true;
        }

        static CalcParam Vis(CalcParam p, Func<bool> v) { p.Visibile = v; return p; }

        string Firma()
        {
            string s = "";
            foreach (string k in new[] { "h", "d", "bnd", "nr", "lr", "rad", "hr", "terr", "rgm", "rg" }) s += k + "=" + P(k).ToString("R", CultureInfo.InvariantCulture) + ";";
            return s;
        }

        List<int> Bande()
        {
            List<int> l = new List<int>();
            int da = new[] { 0, 1, 2, 3 }[Math.Max(0, Math.Min(3, I("bnd")))];
            for (int i = da; i < CalcFilari.BandeF.Length; i++) l.Add(i);
            return l;
        }

        /// <summary>Resistenza di terra alla frequenza f (radiali interrati); 0 con i radiali rialzati.</summary>
        double Rg(double f)
        {
            if (I("rad") == 1) return 0;
            if (I("rgm") == 1) return Math.Max(0, P("rg"));
            return CalcQuartoTerra.ResistenzaTerra(Math.Max(1, P("nr")), Math.Max(0.01, P("lr")) / (C0 / f));
        }

        Cable Cavo()
        {
            int k = Math.Max(0, Math.Min(CaviNomi.Length - 1, I("cv")));
            IndiceCavo = -1;
            if (CalcCavi.Lista != null)
                for (int i = 0; i < CalcCavi.Lista.Count; i++)
                    if (CalcCavi.Lista[i].Name.StartsWith(CaviCerca[k])) { IndiceCavo = i; return CalcCavi.Lista[i]; }
            // abaco non trovato: RG-213 da catalogo
            Cable c = new Cable { Name = "RG-213/U", Z0 = 50, Vf = 0.66 };
            c.AttDb100m = new[] { 4.31, 8.21, 15.0, 26.0 };
            return c;
        }

        /// <summary>Modello NEC: verticale alla frequenza f (resistenza di terra di quella banda), segmenti per la banda piu' alta.</summary>
        string Modello(double f, double fmax)
        {
            double h = Math.Max(1, P("h")), a = Math.Max(5, P("d")) / 2000, seg = Math.Min(C0 / fmax / 25, 0.6);
            double[] t = Terreni[Math.Max(0, Math.Min(3, I("terr")))];
            NecDeck dk = new NecDeck(Title + ", " + Calcolatori.N(h, 2) + " m, " + Adattamenti[Math.Max(0, Math.Min(2, I("ad")))]);
            bool alti = I("rad") == 1;
            int nr = Math.Max(0, Math.Min(64, (int)Math.Round(P("nr"))));
            if (!alti)
            {
                int tv = dk.Ws(seg, 0, 0, 0, 0, 0, h, a, false, 9);
                dk.Raw("GE 1");
                dk.Raw("GN 2 0 0 0 " + t[0].ToString(CultureInfo.InvariantCulture) + " " + t[1].ToString(CultureInfo.InvariantCulture));
                double rg = Rg(f);
                if (rg > 0) dk.Ld(4, tv, 1, 1, rg, 0, 0);
                dk.Ex(tv, 1);
            }
            else
            {
                double hr = Math.Max(0.3, P("hr")), lr = Math.Max(0.5, P("lr"));
                int tv = dk.Ws(seg, 0, 0, hr, 0, 0, hr + h, a, false, 9);
                double ar = 0.001;
                for (int i = 0; i < Math.Max(1, nr); i++)
                {
                    double ang = 2 * Math.PI * i / Math.Max(1, nr);
                    dk.Ws(seg, 0, 0, hr, lr * Math.Cos(ang), lr * Math.Sin(ang), hr, ar);
                }
                dk.Raw("GE 0");
                dk.Raw("GN 2 0 0 0 " + t[0].ToString(CultureInfo.InvariantCulture) + " " + t[1].ToString(CultureInfo.InvariantCulture));
                dk.Ex(tv, 1);
            }
            // NecDeck mette GE/GN in coda: li sposta prima delle schede di controllo
            string s = dk.End(f);
            return SpostaTerreno(s);
        }

        /// <summary>Le righe GE e GN scritte con Raw finiscono tra le schede di controllo: le porta subito dopo la geometria (prima di LD/EX).</summary>
        static string SpostaTerreno(string nec)
        {
            List<string> geo = new List<string>(), terra = new List<string>(), resto = new List<string>();
            foreach (string r in nec.Replace("\r", "").Split('\n'))
            {
                if (r.Length == 0) continue;
                if (r.StartsWith("GE") || r.StartsWith("GN")) terra.Add(r);
                else if (r.StartsWith("CM") || r.StartsWith("CE") || r.StartsWith("GW")) geo.Add(r);
                else resto.Add(r);
            }
            // GE prima di GN
            terra.Sort((x, y) => x.StartsWith("GE") == y.StartsWith("GE") ? 0 : (x.StartsWith("GE") ? -1 : 1));
            List<string> tutto = new List<string>(geo);
            tutto.AddRange(terra); tutto.AddRange(resto);
            return string.Join("\n", tutto) + "\n";
        }

        public override double Tune(double fmhz)
        {
            List<int> bande = Bande();
            double fmax = CalcFilari.BandeF[bande[bande.Count - 1]];
            List<RigaNec> l = new List<RigaNec>();
            foreach (int i in bande)
            {
                double f = CalcFilari.BandeF[i];
                NecModel m = NecModel.Parse(Modello(f, fmax));
                RunResult r = Analysis.Run(m, m.BuildGeometry(), f, 3.0);
                l.Add(new RigaNec { F = f, Z = r.Zin, G = r.GmaxDbi, El = r.ElevationOfMax, Eff = r.Efficiency > 0 ? r.Efficiency / 100 : 1 });
            }
            nec = l;
            firma = Firma();
            return 1;
        }

        /// <summary>Una banda: perdite nel cavo e nell'accordatore, ROS sul cavo, potenza all'antenna.</summary>
        void Catena(Complex z, double f, Cable c, out double rosCavo, out double dbCavo, out double dbAcc, out Complex carico)
        {
            int ad = Math.Max(0, Math.Min(2, I("ad")));
            double len = Math.Max(0, P("lc")), att = c.AttenuationAt(f);
            carico = ad == 0 ? z / 4 : (ad == 1 ? new Complex(50, 0) : z);
            rosCavo = LineCalc.Swr(carico, 50);
            double effCavo = LineCalc.PowerEfficiency(carico, c.Z0, f, c.Vf, att, len);
            dbCavo = -10 * Math.Log10(Math.Max(1e-9, effCavo));
            if (ad == 1) dbAcc = LineaAccordatore.PerditaAccordatore(z, 50, 1, Qu);   // rete L alla base
            else
            {
                Complex zin = LineCalc.InputImpedance(carico, c.Z0, f, c.Vf, att, len);
                dbAcc = rosCavo <= 1.5 ? 0 : LineaAccordatore.PerditaAccordatore(zin, 50, 0, Qu);   // T in stazione
            }
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            if (firma != null && firma != Firma()) { nec = null; firma = null; }
            List<int> bande = Bande();
            double h = Math.Max(1, P("h")), a = Math.Max(5, P("d")) / 2000, pw = Math.Max(0, P("pw"));
            int ad = Math.Max(0, Math.Min(2, I("ad")));
            Cable c = Cavo();
            bool alti = I("rad") == 1;
            double fmax = CalcFilari.BandeF[bande[bande.Count - 1]], fl = CalcFilari.BandeF[bande[0]];
            CanTune = true;

            // ---- tabella per banda
            o.TabellaTitolo = nec != null ? "Banda per banda (NEC-2, terreno di Sommerfeld" + (alti ? ", radiali rialzati" : ", radiali come resistenza di terra") + ")"
                                          : "Banda per banda (stima TF5NN + resistenza di terra; «Calcola le bande (NEC)» per guadagno e angolo)";
            o.Tabella = new List<string[]>();
            o.Tabella.Add(new[] { "Banda", "Z alla base (Ω)", "ROS sul cavo", "Perdita nel cavo", "Perdita accordatore", "Efficienza", "Guadagno, angolo", "Al trasmettitore", "AD5X", "Esito" });
            double bestG = double.NaN; string bestNome = null, bestTxt = null;
            double zbR = double.NaN, zbX = double.NaN, effB = double.NaN, vB = double.NaN, cavoB = double.NaN, rosB = double.NaN, rgB = Rg(fl);
            List<string> altiAngoli = new List<string>(), perditeForti = new List<string>();
            foreach (int i in bande)
            {
                double f = CalcFilari.BandeF[i], rg = Rg(f);
                RigaNec rn = nec != null ? nec.Find(x => Math.Abs(x.F - f) < 1e-6) : null;
                Complex z; double eff, g = double.NaN, el = double.NaN;
                if (rn != null) { z = rn.Z; eff = rn.Eff; g = rn.G; el = rn.El; }
                else
                {
                    Complex zm = CalcFilari.ZFilo(h, f, a, 1.0);
                    z = zm + rg;
                    eff = alti ? double.NaN : zm.Real / Math.Max(1e-6, zm.Real + rg);
                }
                double ros, dbC, dbA; Complex carico;
                Catena(z, f, c, out ros, out dbC, out dbA, out carico);
                double pAnt = pw * Math.Pow(10, -(dbC + dbA) / 10);
                double v = Math.Sqrt(pAnt / Math.Max(0.1, z.Real)) * Complex.Abs(z);
                double gtx = g - dbC - dbA;
                if (i == bande[0])
                {
                    zbR = z.Real; zbX = z.Imaginary; effB = eff; vB = v; cavoB = dbC; rosB = ros; CaricoCavo = carico;
                }
                string esito;
                double perd = dbC + dbA;
                if (perd > 3) { esito = "⚠ accordatore alla base"; perditeForti.Add(CalcFilari.BandeNomi[i]); }
                else if (!double.IsNaN(el) && el > 35) { esito = "angolo alto"; altiAngoli.Add(CalcFilari.BandeNomi[i]); }
                else if (perd > 1) esito = "accettabile";
                else if (!double.IsNaN(el) && el <= 20) esito = "✓ ottima per il DX";
                else esito = "✓";
                if (ad == 1 && dbA > 3) esito = "⚠ accordatore con perdite";
                if (!double.IsNaN(gtx) && el <= 30 && (double.IsNaN(bestG) || gtx > bestG)) { bestG = gtx; bestNome = CalcFilari.BandeNomi[i]; bestTxt = Calcolatori.N(g, 1) + " dBi a " + Calcolatori.N(el, 0) + "°"; }
                string ad5 = Ad5x.ContainsKey(i) ? Ad5x[i] : "-";
                o.Tabella.Add(new[]
                {
                    CalcFilari.BandeNomi[i],
                    Calcolatori.N(z.Real, z.Real < 10 ? 1 : 0) + (z.Imaginary >= 0 ? " +j" : " −j") + Calcolatori.N(Math.Abs(z.Imaginary), 0),
                    ros >= 99 ? "> 99" : Calcolatori.N(ros, ros < 10 ? 2 : 0),
                    Calcolatori.N(dbC, 1) + " dB",
                    Calcolatori.N(dbA, 1) + " dB",
                    double.IsNaN(eff) ? "nel guadagno" : Calcolatori.N(eff * 100, 0) + " %" + (rg > 0 ? " (Rg " + Calcolatori.N(rg, 1) + ")" : ""),
                    double.IsNaN(g) ? "dal NEC" : Calcolatori.N(g, 1) + " dBi a " + Calcolatori.N(el, 0) + "°",
                    double.IsNaN(gtx) ? "-" + Calcolatori.N(perd, 1) + " dB" : Calcolatori.N(gtx, 1) + " dBi",
                    ad5,
                    esito
                });
            }

            // ---- riquadri
            string nomeB = CalcFilari.BandeNomi[bande[0]];
            o.Card("Banda migliore", bestNome ?? "dal NEC", bestNome != null ? bestTxt + ", al TX " + Calcolatori.N(bestG, 1) + " dBi (angolo fino a 30°)" : "«Calcola le bande (NEC)»", true);
            o.Card("Z alla base sui " + nomeB, Calcolatori.N(zbR, zbR < 10 ? 1 : 0) + (zbX >= 0 ? " +j" : " −j") + Calcolatori.N(Math.Abs(zbX), 0) + " Ω", nec != null ? "dal NEC, con la resistenza di terra" : "stima TF5NN + Rg " + Calcolatori.N(rgB, 1) + " Ω");
            o.Card("Efficienza sui " + nomeB, double.IsNaN(effB) ? "nel guadagno" : Calcolatori.N(effB * 100, 0) + " %", alti ? "radiali rialzati: le perdite nel terreno sono nel guadagno del NEC" : "Rr / (Rr + Rg), Rg " + Calcolatori.N(rgB, 1) + " Ω", !double.IsNaN(effB) && effB < 0.3);
            o.Card("Tensione alla base", Calcolatori.N(vB / 1000, 1) + " kV", "efficaci a " + Calcolatori.N(pw, 0) + " W sui " + nomeB + " (" + Calcolatori.N(vB * Math.Sqrt(2) / 1000, 1) + " kV di picco)", vB > 2000);
            o.Card("Perdita nel cavo, " + nomeB, Calcolatori.N(cavoB, 1) + " dB", ad == 1 ? "cavo adattato (accordatore alla base)" : "ROS " + (rosB >= 99 ? "> 99" : Calcolatori.N(rosB, 0)) + " sul cavo" + (ad == 0 ? " dopo il 4:1" : ""), cavoB > 3);
            o.Card("Coassiale", Calcolatori.N(P("lc"), 0) + " m di " + c.Name, Calcolatori.N(c.AttenuationAt(fl) * P("lc") / 100, 2) + " dB adattato sui " + nomeB + ", " + Calcolatori.N(c.AttenuationAt(28.5) * P("lc") / 100, 2) + " dB sui 10 m");

            // ---- grafico: perdita cavo + accordatore da 1,8 a 30 MHz (stima TF5NN), adattamento scelto e alternativa
            {
                int np = 120;
                o.Chart = new CalcChart { Title = "Perdite nel cavo e nell'accordatore (stima TF5NN + Rg)", XLabel = "MHz", YLabel = "dB" };
                o.Chart.X = Calcolatori.Lin(1.8, 30, np); o.Chart.Y = new double[np]; o.Chart.Y2 = new double[np];
                int alt = ad == 1 ? 0 : 1;
                for (int k = 0; k < np; k++)
                {
                    double f = o.Chart.X[k];
                    Complex z = CalcFilari.ZFilo(h, f, a, 1.0) + Rg(f);
                    double r1, c1, a1, r2, c2, a2; Complex q;
                    Catena(z, f, c, out r1, out c1, out a1, out q);
                    o.Chart.Y[k] = Math.Min(30, c1 + a1);
                    int sv = (int)Par("ad").Value; Par("ad").Value = alt;
                    Catena(z, f, c, out r2, out c2, out a2, out q);
                    Par("ad").Value = sv;
                    o.Chart.Y2[k] = Math.Min(30, c2 + a2);
                }
                o.Chart.Nome1 = Adattamenti[ad];
                o.Chart.Nome2 = Adattamenti[alt];
                o.Chart.MarkX = fl; o.Chart.MarkText = nomeB;
                o.Chart.YMin = 0;
            }

            // ---- modello NEC (banda piu' bassa) e calcolo in tempo reale con il ROS alle bande
            o.Nec = "CM YA-NEC Z0=" + (ad == 0 ? "200" : "50") + "\n" + Modello(fl, fmax);
            o.Z0Rif = ad == 0 ? 200 : 50;
            List<double> fb = new List<double>();
            foreach (int i in bande) fb.Add(CalcFilari.BandeF[i]);
            o.Bande = fb.ToArray();
            o.Plane = "xz";
            double z0 = alti ? Math.Max(0.3, P("hr")) : 0;
            if (ad != 2) o.Mark(8, -1.6, z0 + 0.2, ad == 0 ? "4:1" : "ATU", 0, -0.4, z0 + 1.4);
            o.Mark(7, 1.2, z0, Calcolatori.N(h, 1) + " m", 0, 1.2, z0 + h);
            o.Mark(0, -Math.Max(3, h * 0.35), z0 + 2.2, Calcolatori.N(P("lc"), 0) + " m " + c.Name.Split(' ')[0], 1);
            if (vB > 2000) o.Mark(0, 1.6, z0 + 0.5, "≈ " + Calcolatori.N(vB / 1000, 1) + " kV alla base", 2);

            // ---- avvisi
            if (perditeForti.Count > 0 && ad != 1)
                o.Warnings.Add("Perdite oltre 3 dB (" + string.Join(", ", perditeForti) + "): l'accordatore in stazione accorda il cavo, non l'antenna: si perdono piu' di 3 dB nel cavo e nell'accordatore. Serve l'accordatore (o una rete L) alla base (AD5X).");
            if (vB > 2000) o.Warnings.Add("Alla base ci sono circa " + Calcolatori.N(vB / 1000, 1) + " kV efficaci a " + Calcolatori.N(pw, 0) + " W: condensatori e rele' adatti, antenna fuori dalla portata delle persone.");
            if (!alti && !double.IsNaN(effB) && effB < 0.3) o.Warnings.Add("Efficienza bassa sulla banda piu' bassa (" + nomeB + "): la verticale corta: la verticale corta vive dei radiali (piu' radiali, o radiali rialzati).");
            if (altiAngoli.Count > 0) o.Warnings.Add("Lobo alto (" + string.Join(", ", altiAngoli) + "): il lobo principale e' alto (piu' di 35°): la verticale e' lunga 5/8 λ o piu' e il DX perde rispetto a una λ/4.");
            if (!alti && I("rgm") == 0 && Math.Max(0.01, P("lr")) / (C0 / fl) < 0.15) o.Warnings.Add("Radiali corti rispetto alla banda piu' bassa (meno di 0,15 λ): la Tab. 9-1 di ON4UN non scende sotto, la resistenza di terra vera e' piu' alta.");
            if (I("terr") == 0 && P("nr") < 8) o.Warnings.Add("Terreno povero e pochi radiali: l'efficienza crolla e l'angolo sale, la verticale non e' migliore di un dipolo basso.");
            if (nec == null) o.Oks.Add("«Calcola le bande (NEC)» mette nella tabella impedenza, guadagno, angolo ed efficienza del NEC con il terreno e i radiali scelti");
            if (ad == 0) o.Oks.Add("con il 4:1 il ROS sul cavo e' riferito a 200 Ω alla base (un-un ideale)");
            o.Formula = "Formule: impedenza alla base dal modello TF5NN v3 (monopolo su terra ideale) piu' Rg, oppure dal NEC-2; Rg dei radiali interrati dalla Tab. 9-1 di ON4UN (numero e lunghezza in λ, banda per banda) o fissa; " +
                "un-un 4:1 ideale (carico del cavo Z/4); perdita nel cavo con il carico vero (linea con perdite, gamma = alfa + j beta, come Cavi RG); accordatore: reattanza annullata da una bobina con Q " + Calcolatori.N(Qu, 0) +
                ", poi rete di Q caricato √(Rmax/Rmin - 1) (T in stazione, L alla base, come Linea aperta + accordatore); tensione alla base V = |Z| √(P/R) con la potenza che arriva all'antenna; al trasmettitore = guadagno NEC - perdite. " +
                "AD5X: Rr 3-4 Ω e circa -j600 Ω sui 160 m, 65 Ω sui 40 m, ROS 324:1 sui 160 e 41:1 sugli 80 m con il 4:1 e Rg 10 Ω, circa 13 kV alla base a 1500 W sui 160 m. Fonti: " + Source + ".";
            return o;
        }
    }
}
