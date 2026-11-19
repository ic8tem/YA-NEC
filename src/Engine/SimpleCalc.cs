// YA-NEC - calcolatori delle antenne semplici (dipolo, V invertita, verticale con radiali, delta loop, four-square,
// loop magnetica, distanze di stack DL6WU). Ogni calcolatore da' le misure di formula e il modello NEC; il NEC puo'
// poi portare l'antenna in risonanza (zero della reattanza con pendenza positiva, mai accanto a un polo).
// Fonti delle formule: ARRL Antenna Book (468/f piedi, 234/f, 1005/f), ON4UN (four-square, correnti 0/-90/-90/-180),
// DL6WU (stack), formule classiche della loop magnetica (Rr = 320 pi^4 A^2 / lambda^4, L = mu0 R [ln(8D/d) - 2]).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public class CalcParam
    {
        public string Key, Label, Unit, Tip;
        public double Value;
        public string[] Choices;                 // scelta a pillole (fino a 4 voci) o a tendina: Value = indice
        public string Hint;                      // testo grigio dopo l'unita' (aggiornato da Compute)
        public bool Pillole;                     // 0.5.3: scelta sempre a pillole (anche oltre 4 voci, vanno a capo)
        // 1.0.11 (bozzetti approvati da IC8TEM il 16/11/2026 23:48): schede in sezioni numerate
        public string Sezione;                   // titolo della sezione che si apre con questo parametro ("2 · Braccio e bobina")
        public Func<bool> Visibile;              // null = sempre; la sezione si vede se si vede il suo primo parametro
        public bool StessaRiga;                  // scelta a pillole sulla stessa riga della scelta precedente
        public double[][] Figure;                // scelta a tessere con figurina: per voce x,y del poligono (0..1) e per ultimo x,y del balun
        public string[] Sotto;                   // riga piccola sotto la voce della tessera
        public CalcParam(string key, string label, double value, string unit, string tip = null) { Key = key; Label = label; Value = value; Unit = unit; Tip = tip; }
        public static CalcParam Scelta(string key, string label, int sel, string tip, params string[] voci)
        {
            return new CalcParam(key, label, sel, "", tip) { Choices = voci };
        }
        public CalcParam ConSezione(string titolo) { Sezione = titolo; return this; }
    }

    /// <summary>Riquadro dei risultati (bozzetto approvato dei calcolatori delle forme semplici).</summary>
    public class CalcCard { public string Title, Value, Sub; public bool Hl; }

    /// <summary>Annotazione del disegno nel piano della vista (u, v in metri): 0 testo, 1 cerchio, 2 linea, 3 condensatore,
    /// 4 punto di alimentazione, 5 resistenza, 6 bobina, 7 quota (linea con testo), 8 rettangolo. Colore: 0 blu, 1 arancio, 2 viola, 3 grigio, 4 verde, 5 rosso.</summary>
    public class CalcMark
    {
        public int Kind, Col;
        public double X, Y, X2, Y2, R;
        public string Text;
        public bool Dash;
    }

    /// <summary>Grafico di un parametro (efficienza contro diametro, ROS contro frequenza...).</summary>
    public class CalcChart
    {
        public string Title = "", XLabel = "", YLabel = "";
        public double[] X, Y;
        public double MarkX = double.NaN;
        public string MarkText;
        public double YMin = double.NaN, YMax = double.NaN;
        // 1.5 (bozzetto AEH a3, approvato 18/11/2026 23:16): seconda curva facoltativa (es. R e X del dipolo ripiegato)
        public double[] Y2;
        public string Nome1, Nome2;
    }

    public class CalcOutput
    {
        public List<string[]> Lines = new List<string[]>();     // etichetta, valore (anche esportate nel manuale)
        public List<string> Warnings = new List<string>();       // righe con il triangolo
        public List<string> Oks = new List<string>();            // righe con la spunta
        public List<CalcCard> Cards = new List<CalcCard>();
        public List<CalcMark> Marks = new List<CalcMark>();
        public string Formula = "";                              // formule e fonti, sotto i dati
        public string Plane = "";                                // vista del disegno: "xz", "yz", "xy" ("" = automatica)
        public bool HideWires;                                   // disegno solo con le annotazioni (schemi)
        public bool HideGround;
        public string Schema;                                    // 0.4.8: disegno fisso al posto dei fili ("eh": schema dell'antenna EH, bozzetto IC8TEM 13/11/2026 23:41)
        public CalcChart Chart;
        public BalunSchema Balun;                                // 0.5.3: disegno di costruzione del balun (Calcolatori > Balun / Un-Un)
        public List<string> Passi = new List<string>();          // 0.5.3: passi di costruzione mostrati sotto il disegno
        public void Card(string t, string v, string s = "", bool hl = false) { Cards.Add(new CalcCard { Title = t, Value = v, Sub = s, Hl = hl }); }
        public void Mark(int kind, double x, double y, string text = null, int col = 0, double x2 = 0, double y2 = 0, double r = 0, bool dash = false)
        {
            Marks.Add(new CalcMark { Kind = kind, X = x, Y = y, Text = text, Col = col, X2 = x2, Y2 = y2, R = r, Dash = dash });
        }
        public string Nec;                                       // null = solo formule
        public bool CurrentDrive;                                // sorgenti da ricavare dalle correnti volute
        public Complex[] WantedCurrents;
        public double TuneScale = 1;                             // fattore applicato alla lunghezza da "Risonanza"
        public void Add(string label, string value) { Lines.Add(new[] { label, value }); }
        public string AvvisoDati;                                // 1.0.15: riquadro arancio sotto i dati (bozzetto DL6WU approvato da IC8TEM 17/11/2026 09:57)
    }

    public abstract class SimpleCalc
    {
        public string Key, Title, Family, Source;
        public string Group, Variant;           // voce del fascione e nome della variante (pillole "Tipo")
        public bool UsesModel;                  // strumenti che lavorano sul modello aperto (Ridimensiona)
        public NecModel Model;                  // modello aperto, passato dall'interfaccia
        public double MainFreqMHz = double.NaN; // frequenza della barra in alto (finestra principale)
        public Dictionary<string, double> DalModello = new Dictionary<string, double>();
        // 1.0.15 (bozzetto approvato da IC8TEM il 17/11/2026 09:57): campi ricavati dagli altri dati (grigi, non si scrivono, mostrano il valore)
        public Dictionary<string, double> Calcolati = new Dictionary<string, double>();
        public string NotaFissa;                // nota sempre visibile sotto i dati (fumetto fisso)
        public bool PuoUsareModello;            // la scheda sa prendere i dati dal modello aperto (casella "Prendi i dati dal modello")
        public bool UsaModello = true;          // stato della casella (IC8TEM 13/11/2026 18:19)
        public string Nome { get { return string.IsNullOrEmpty(Variant) ? Title : Variant; } }
        public List<CalcParam> Params = new List<CalcParam>();
        public bool CanTune = true;
        /// <summary>0.5.0: testo del pulsante che chiama Tune (le Yagi DL6WU e le Quagi ottimizzano invece di portare in risonanza).</summary>
        public virtual string TuneLabel { get { return "Porta in risonanza (NEC)"; } }
        public double Scale = 1;     // fattore di lunghezza trovato dalla risonanza NEC (1 = formula)
        // 0.4.30 (IC8TEM 14/11/2026 23:40): rastremazione dei tubi nei progetti HF non filari (Engine/RastremazioneHf.cs)
        public virtual bool Tubolare { get { return false; } }
        public bool Rastrema;
        public string SchemaRastr = RastremazioneHf.Predefinita;
        protected void Rastremare(CalcOutput o)
        {
            if (!Rastrema || !Tubolare || string.IsNullOrEmpty(o.Nec)) return;
            List<string> note = new List<string>();
            try { o.Nec = RastremazioneHf.ApplicaTesto(o.Nec, SchemaRastr, note); foreach (string n in note) o.Oks.Add(n); }
            catch (NecException ex) { o.Warnings.Add("Rastremazione non applicata: " + ex.Message); }
        }

        protected static readonly CultureInfo CC = CultureInfo.CurrentCulture;
        protected static string F(double v) { return v != 0 && Math.Abs(v) < 1e-3 ? v.ToString("G9", CultureInfo.InvariantCulture) : v.ToString("0.######", CultureInfo.InvariantCulture); }   // valori piccoli (C in farad, L in henry) in notazione esponenziale
        protected static string N(double v, int d) { return v.ToString("F" + d, CC); }
        protected const double C0 = 299.792458;

        public double P(string key)
        {
            foreach (CalcParam p in Params) if (p.Key == key) return p.Value;
            return 0;
        }

        public abstract CalcOutput Compute();

        protected static int Odd(double n, int min)
        {
            int k = (int)Math.Ceiling(n);
            if (k < min) k = min;
            if (k % 2 == 0) k++;
            return k;
        }

        protected static void Ground(StringBuilder sb, double h)
        {
            sb.AppendLine("GE 0");
            if (h > 0) sb.AppendLine("GN 2 0 0 0 13 0.005");   // Sommerfeld: esatto anche per antenne basse
        }

        protected static string Head(string title, double f)
        {
            return "CM " + title + " (calcolatore YA-NEC)\nCE\n";
        }

        public static List<SimpleCalc> All() { return Calcolatori.Tutti(); }

        public int I(string key) { return (int)Math.Round(P(key)); }
        public CalcParam Par(string key) { foreach (CalcParam p in Params) if (p.Key == key) return p; return null; }
        protected void Hint(string key, string text) { CalcParam p = Par(key); if (p != null) p.Hint = text; }

        // riempie i riquadri dalle righe quando un calcolatore non li prepara da se'
        public CalcOutput Run()
        {
            Calcolati.Clear();
            CalcOutput o = Compute();
            Rastremare(o);
            if (o.Cards.Count == 0) foreach (string[] l in o.Lines) o.Card(l[0], l[1]);
            if (string.IsNullOrEmpty(o.Formula)) o.Formula = "Fonti: " + Source + ".";
            // 0.5.1: controlli dei dati (parametri e modello NEC del calcolatore), come avvisi della scheda
            try { ControlliDati.AggiungiACalcolatore(this, o); } catch { }
            return o;
        }

        // ---------------------------------------------------------------------------------------------
        // soluzione NEC del modello di un calcolatore (con le sorgenti ricavate dalle correnti, se servono)
        public static RunResult Solve(CalcOutput o, double fmhz, double z0 = 50)
        {
            NecModel m = NecModel.Parse(o.Nec);
            m.Z0 = z0;
            SegmentGeometry g = m.BuildGeometry();
            return Analysis.Run(m, g, fmhz, 2.0);     // le sorgenti a corrente (CM YA-NEC CORRENTI=1) le risolve Analysis
        }

        /// <summary>Sorgenti a corrente: tensioni che danno le correnti volute (V = Z I, Z = inversa delle ammettenze tra le porte).</summary>
        public static void SetCurrentDrive(NecModel m, SegmentGeometry g, double fmhz, Complex[] want)
        {
            int n = m.Sources.Count;
            if (want.Length != n) throw new NecException("Numero di correnti diverso dal numero di sorgenti.");
            Complex[,] y = new Complex[n, n];
            for (int j = 0; j < n; j++)
            {
                List<SourceSpec> src = new List<SourceSpec>();
                for (int k = 0; k < n; k++)
                {
                    SourceSpec s = m.Sources[k].Clone();
                    s.V = k == j ? Complex.One : new Complex(1e-15, 0);    // le altre porte praticamente in corto
                    src.Add(s);
                }
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(fmhz, src, m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fmhz));
                for (int k = 0; k < n; k++) y[k, j] = e.Sources[k].I;
            }
            // risolve Y V = I
            Complex[,] a = new Complex[n, n + 1];
            for (int r = 0; r < n; r++) { for (int c = 0; c < n; c++) a[r, c] = y[r, c]; a[r, n] = want[r]; }
            for (int c = 0; c < n; c++)
            {
                int piv = c;
                for (int r = c + 1; r < n; r++) if (Complex.Abs(a[r, c]) > Complex.Abs(a[piv, c])) piv = r;
                if (piv != c) for (int k = 0; k <= n; k++) { Complex t = a[c, k]; a[c, k] = a[piv, k]; a[piv, k] = t; }
                for (int r = 0; r < n; r++)
                {
                    if (r == c) continue;
                    Complex f = a[r, c] / a[c, c];
                    for (int k = c; k <= n; k++) a[r, k] -= f * a[c, k];
                }
            }
            for (int r = 0; r < n; r++) m.Sources[r].V = a[r, n] / a[r, r];
        }

        /// <summary>Risonanza: fattore di lunghezza che annulla la reattanza della prima sorgente (secante, pendenza positiva).</summary>
        public virtual double Tune(double fmhz)
        {
            Func<double, double> x = sc =>
            {
                Scale = sc;
                CalcOutput o = Compute();
                Rastremare(o);
                NecModel m = NecModel.Parse(o.Nec);
                SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, fmhz);   // 0.4.31: con Leeson (tubi telescopici) come il calcolo finale
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(fmhz, Analysis.EffectiveSources(m, g, fmhz), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fmhz));
                return e.Sources[0].Z.Imaginary;
            };
            double s0 = Scale, s1 = Scale * 1.02;
            double f0 = x(s0), f1 = x(s1);
            for (int it = 0; it < 25; it++)
            {
                if (Math.Abs(f1) < 0.05) break;
                if (f1 == f0) break;
                double s2 = s1 - f1 * (s1 - s0) / (f1 - f0);
                // la reattanza cresce con la lunghezza vicino alla risonanza giusta: passi limitati al 5 %
                s2 = Math.Max(s1 * 0.95, Math.Min(s1 * 1.05, s2));
                s0 = s1; f0 = f1; s1 = s2; f1 = x(s1);
            }
            Scale = s1;
            return s1;
        }
    }

    // ---------------------------------------------------------------------------------------------
    public class CalcDipolo : SimpleCalc
    {
        public CalcDipolo()
        {
            Key = "dipolo"; Title = "Dipolo a mezz'onda"; Family = "Filari"; Source = "ARRL Antenna Book: L = 468/f piedi = 142,6/f m";
            Params.Add(new CalcParam("f", "Frequenza", 14.2, "MHz"));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(new CalcParam("h", "Altezza dal suolo (0 = spazio libero)", 10, "m", "Con altezza maggiore di 0 il calcolo NEC usa il terreno medio (er 13, sigma 0,005 S/m)."));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), d = P("d") / 1000, h = P("h"), lam = C0 / f;
            double L = 142.6 / f * Scale;
            o.Add("Lunghezza totale (formula)", N(142.6 / f, 3) + " m");
            if (Scale != 1) o.Add("Lunghezza in risonanza (NEC)", N(L, 3) + " m");
            o.Add("Ogni braccio", N(L / 2, 3) + " m");
            o.Add("Mezz'onda nello spazio libero", N(lam / 2, 3) + " m");
            o.Add("Fattore di accorciamento", N(L / (lam / 2), 3));
            o.Add("Impedenza teorica (spazio libero)", "circa 73 ohm");
            if (h > 0 && h < lam / 8) o.Warnings.Add("Altezza sotto lambda/8: impedenza bassa e molte perdite nel terreno.");
            int ns = Odd(L / (lam / 50), 21);
            StringBuilder sb = new StringBuilder(Head(Title + " " + N(f, 3) + " MHz", f));
            sb.AppendLine("GW 1 " + ns + " 0 " + F(-L / 2) + " " + F(h) + " 0 " + F(L / 2) + " " + F(h) + " " + F(d / 2));
            Ground(sb, h);
            sb.AppendLine("EX 0 1 " + ((ns + 1) / 2) + " 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(f) + " 0");
            sb.AppendLine("EN");
            o.Nec = sb.ToString();
            return o;
        }
    }

    public class CalcVInvertita : SimpleCalc
    {
        public CalcVInvertita()
        {
            Key = "vinv"; Title = "Dipolo a V invertita"; Family = "Filari"; Source = "ARRL Antenna Book; lunghezza di partenza 142,6/f m, poi risonanza NEC";
            Params.Add(new CalcParam("f", "Frequenza", 7.1, "MHz"));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(new CalcParam("h", "Altezza del vertice", 12, "m"));
            Params.Add(new CalcParam("a", "Angolo tra i bracci", 120, "gradi"));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), d = P("d") / 1000, h = P("h"), ang = P("a") * Math.PI / 180, lam = C0 / f;
            double L = 142.6 / f * Scale, leg = L / 2;
            double seg = lam / 50;
            double sa = Math.Sin(ang / 2), ca = Math.Cos(ang / 2);
            double zend = h - leg * ca;
            o.Add("Lunghezza totale", N(L, 3) + " m");
            o.Add("Ogni braccio", N(leg, 3) + " m");
            o.Add("Quota delle punte", N(zend, 2) + " m");
            o.Add("Distanza tra le punte", N(2 * leg * sa, 2) + " m");
            if (zend < 1) o.Warnings.Add("Le punte sono sotto 1 m dal suolo: alzare il vertice o aprire l'angolo.");
            if (zend <= 0) zend = 0.1;
            // filo corto di alimentazione al vertice (un segmento), poi i due bracci
            double s = Math.Min(seg, leg / 5);
            int ns = Odd((leg - s / 2) / seg, 9);
            StringBuilder sb = new StringBuilder(Head(Title + " " + N(f, 3) + " MHz", f));
            double y0 = s / 2;
            sb.AppendLine("GW 1 1 0 " + F(-y0) + " " + F(h) + " 0 " + F(y0) + " " + F(h) + " " + F(d / 2));
            double ly = (leg - y0) * sa, lz = (leg - y0) * ca;
            sb.AppendLine("GW 2 " + ns + " 0 " + F(y0) + " " + F(h) + " 0 " + F(y0 + ly) + " " + F(h - lz) + " " + F(d / 2));
            sb.AppendLine("GW 3 " + ns + " 0 " + F(-y0) + " " + F(h) + " 0 " + F(-y0 - ly) + " " + F(h - lz) + " " + F(d / 2));
            Ground(sb, h);
            sb.AppendLine("EX 0 1 1 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(f) + " 0");
            sb.AppendLine("EN");
            o.Nec = sb.ToString();
            return o;
        }
    }

    public class CalcGroundPlane : SimpleCalc
    {
        public override bool Tubolare { get { return true; } }   // 0.4.30: tubi rastremabili (HF)
        public CalcGroundPlane()
        {
            Key = "gp"; Title = "Verticale lambda/4 con radiali (ground plane)"; Family = "Verticali"; Source = "ARRL Antenna Book: radiatore 234/f piedi = 71,3/f m, radiali 246/f piedi = 75/f m";
            Params.Add(new CalcParam("f", "Frequenza", 14.2, "MHz"));
            Params.Add(new CalcParam("d", "Diametro del radiatore", 20, "mm"));
            Params.Add(new CalcParam("n", "Numero di radiali", 4, ""));
            Params.Add(new CalcParam("a", "Inclinazione dei radiali verso il basso", 30, "gradi"));
            Params.Add(new CalcParam("h", "Altezza della base (0 = spazio libero)", 0, "m", "Con altezza maggiore di 0 il calcolo NEC usa il terreno medio (er 13, sigma 0,005 S/m)."));
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), d = P("d") / 1000, h = P("h"), lam = C0 / f;
            int nr = Math.Max(2, Math.Min(32, (int)Math.Round(P("n"))));
            double a = P("a") * Math.PI / 180;
            double Lr = 71.3 / f * Scale, Lrad = 75.0 / f;
            o.Add("Radiatore", N(Lr, 3) + " m");
            o.Add("Radiali (ciascuno)", N(Lrad, 3) + " m");
            o.Add("Impedenza tipica", P("a") < 10 ? "circa 36 ohm (radiali orizzontali)" : (P("a") < 35 ? "circa 45 ohm" : "circa 50 ohm"));
            double zb = h > 0 ? h : Lrad * Math.Sin(a) + 0.5;
            if (h > 0 && h < Lrad * Math.Sin(a) + 0.2) o.Warnings.Add("Con questa inclinazione i radiali toccano il suolo: alzare la base.");
            double seg = lam / 50;
            int ns = Odd(Lr / seg, 11), nsr = Odd(Lrad / seg, 11);
            StringBuilder sb = new StringBuilder(Head(Title + " " + N(f, 3) + " MHz", f));
            sb.AppendLine("GW 1 " + ns + " 0 0 " + F(zb) + " 0 0 " + F(zb + Lr) + " " + F(d / 2));
            for (int i = 0; i < nr; i++)
            {
                double ph = 2 * Math.PI * i / nr;
                double x = Lrad * Math.Cos(a) * Math.Cos(ph), y = Lrad * Math.Cos(a) * Math.Sin(ph), z = zb - Lrad * Math.Sin(a);
                sb.AppendLine("GW " + (i + 2) + " " + nsr + " 0 0 " + F(zb) + " " + F(x) + " " + F(y) + " " + F(z) + " 0.001");
            }
            Ground(sb, h);
            sb.AppendLine("EX 0 1 1 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(f) + " 0");
            sb.AppendLine("EN");
            o.Nec = sb.ToString();
            if (h <= 0) o.Warnings.Add("Spazio libero: la base e' posta comunque sopra la punta dei radiali, il terreno non c'e'.");
            return o;
        }
    }

    public class CalcDelta : SimpleCalc
    {
        public CalcDelta()
        {
            Key = "delta"; Title = "Delta loop"; Family = "Filari"; Source = "ARRL Antenna Book: perimetro 1005/f piedi = 306,3/f m; posizioni di alimentazione e polarizzazione (ARRL Antenna Book, Cebik W4RNL)";
            // 1.0.11 (bozzetto approvato da IC8TEM il 16/11/2026 23:48): 1 Dimensioni, 2 Forma (forma e vertice sulla stessa riga),
            // 3 Balun / alimentazione a tessere con la figurina (prima le figurine stavano nel disegno e si sovrapponevano)
            Params.Add(new CalcParam("f", "Frequenza", 14.2, "MHz") { Sezione = "1 · Dimensioni" });
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(new CalcParam("h", "Quota del punto piu' basso (0 = spazio libero)", 8, "m"));
            // 0.4.8 (IC8TEM 13/11/2026 23:53): altezza del triangolo fissata, con qualsiasi forma, vertice e alimentazione
            Params.Add(new CalcParam("H", "Altezza del triangolo (0 = dalla forma)", 0, "m", "Distanza tra il lato orizzontale e il vertice. Se e' data, la forma si ricava dal perimetro e da questa altezza."));
            Params.Add(CalcParam.Scelta("forma", "Forma", 0, null, "equilatera", "schiacciata", "allungata").ConSezione("2 · Forma"));
            CalcParam pv = CalcParam.Scelta("vert", "vertice", 0, null, "in alto", "in basso");
            pv.StessaRiga = true;
            Params.Add(pv);
            CalcParam pa = CalcParam.Scelta("ali", "Balun / alimentazione", 0, "Polarizzazione: orizzontale al centro del lato orizzontale o al vertice; verticale sul lato inclinato a lambda/4 dal centro del lato orizzontale; mista all'angolo.",
                "centro del lato orizzontale", "al vertice", "all'angolo", "lato a λ/4").ConSezione("3 · Balun / alimentazione (dà la polarizzazione)");
            double[] tri = { 0.1, 0.9, 0.5, 0.1, 0.9, 0.9 };
            Func<double, double, double[]> fig = (x, y) => new[] { tri[0], tri[1], tri[2], tri[3], tri[4], tri[5], x, y };
            pa.Figure = new[] { fig(0.5, 0.9), fig(0.5, 0.1), fig(0.1, 0.9), fig(0.72, 0.52) };
            pa.Sotto = new[] { "orizzontale", "orizzontale", "mista", "verticale" };
            Params.Add(pa);
        }
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), a = P("d") / 2000, h = P("h"), lam = C0 / f, Hfiss = P("H");
            int forma = I("forma"), ali = I("ali");
            bool giu = I("vert") == 1;
            double ang = new[] { 60.0, 90.0, 40.0 }[Math.Max(0, Math.Min(2, forma))] * Math.PI / 180;
            double Pm = 306.3 / f * Scale;
            double sl = Pm / (2 + 2 * Math.Sin(ang / 2)), b = 2 * sl * Math.Sin(ang / 2), ht = sl * Math.Cos(ang / 2);
            bool altFissa = false;
            if (Hfiss > 0)
            {
                // triangolo isoscele di perimetro P e altezza H: 2 s + b = P, s^2 = H^2 + (b/2)^2  ->  b = P/2 - 2 H^2 / P
                double bb = Pm / 2 - 2 * Hfiss * Hfiss / Pm;
                if (bb > 0.05 * Pm)
                {
                    b = bb; sl = (Pm - b) / 2; ht = Hfiss; ang = 2 * Math.Asin(Math.Min(1, b / (2 * sl)));
                    altFissa = true;
                }
                else o.Warnings.Add("Altezza " + Calcolatori.N(Hfiss, 2) + " m troppo grande per il perimetro di " + Calcolatori.N(Pm, 2) + " m (massimo circa " + Calcolatori.N(Math.Sqrt(0.45) * Pm / 2, 2) + " m): usata la forma scelta.");
            }
            // percorso: dal centro del lato orizzontale verso destra, lato destro fino al vertice, lato sinistro, ritorno
            double sFeed = ali == 0 ? 0 : ali == 1 ? b / 2 + sl : ali == 2 ? b / 2 : lam / 4;
            string pol = ali == 0 || ali == 1 ? "orizzontale" : ali == 2 ? "mista (diagonale)" : "verticale";
            o.Card("Perimetro", Calcolatori.N(Pm, 2) + " m", "306,3 / f", true);
            o.Card("Lato orizzontale", Calcolatori.N(b, 2) + " m", "");
            o.Card("Lati inclinati", Calcolatori.N(sl, 2) + " m", "angolo al vertice " + Calcolatori.N(ang * 180 / Math.PI, 0) + " gradi");
            o.Card("Altezza del triangolo", Calcolatori.N(ht, 2) + " m", (altFissa ? "fissata; " : "") + (giu ? "vertice in basso" : "vertice in alto"), altFissa);
            if (altFissa) o.Oks.Add("altezza fissata: la forma (angolo al vertice " + Calcolatori.N(ang * 180 / Math.PI, 0) + " gradi) viene dal perimetro e dall'altezza, i pulsanti della forma non contano");
            o.Card("Polarizzazione attesa", pol, "il NEC la conferma (V/H)", true);
            o.Card("Impedenza tipica", ali == 3 ? "circa 100-150 Ω" : (forma == 1 ? "circa 70-100 Ω" : "circa 100-120 Ω"), "quarto d'onda da 75 Ω o balun 2:1");
            o.Formula = "Formule: perimetro 306,3/f m (ARRL); triangolo isoscele con angolo al vertice a: lato inclinato = P/(2 + 2 sin(a/2)), lato orizzontale = 2 s sin(a/2). " +
                "Polarizzazione (ARRL, Cebik): balun al centro del lato orizzontale o al vertice -> orizzontale; balun sul lato inclinato a lambda/4 dal centro del lato orizzontale -> verticale; all'angolo -> mista.";
            double zb = giu ? h + ht : h, za = giu ? h : h + ht;     // quota del lato orizzontale e del vertice
            if (h <= 0) { zb = giu ? ht : 0; za = giu ? 0 : ht; }
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(f, 3) + " MHz");
            double seg = lam / 60;
            int nb = Math.Max(11, (int)Math.Ceiling(b / seg)) | 1, nl = Math.Max(9, (int)Math.Ceiling(sl / seg));
            int t1 = d.W(nb, 0, -b / 2, zb, 0, b / 2, zb, a);
            int t2 = d.W(nl, 0, b / 2, zb, 0, 0, za, a);
            int t3 = d.W(nl, 0, 0, za, 0, -b / 2, zb, a);
            d.Ground(h > 0 ? 2 : 0);
            // segmento della sorgente lungo il percorso
            double sp = ((sFeed % Pm) + Pm) % Pm;
            int tag, sg;
            if (sp < b / 2) { tag = t1; sg = (int)Math.Floor((b / 2 + sp) / b * nb) + 1; }
            else if (sp < b / 2 + sl) { tag = t2; sg = (int)Math.Floor((sp - b / 2) / sl * nl) + 1; }
            else if (sp < b / 2 + 2 * sl) { tag = t3; sg = (int)Math.Floor((sp - b / 2 - sl) / sl * nl) + 1; }
            else { tag = t1; sg = (int)Math.Floor((sp - b / 2 - 2 * sl) / b * nb) + 1; }
            if (tag == t2 && ali == 1) sg = nl;          // al vertice: ultimo segmento del lato destro
            if (tag != t1 && ali == 2) { tag = t2; sg = 1; }
            sg = Math.Max(1, Math.Min(tag == t1 ? nb : nl, sg));
            d.Ex(tag, sg);
            o.Nec = d.End(f);
            o.Plane = "yz";
            // freccia della polarizzazione
            double cxm = b * 0.75, cym = (zb + za) / 2;
            if (pol == "orizzontale") o.Mark(2, cxm - b * 0.12, cym, "E", 1, cxm + b * 0.12, cym);
            else if (pol == "verticale") o.Mark(2, cxm, cym - ht * 0.15, "E", 1, cxm, cym + ht * 0.15);
            else o.Mark(2, cxm - b * 0.1, cym - ht * 0.1, "E", 1, cxm + b * 0.1, cym + ht * 0.1);
            // 1.0.11: didascalia sotto il triangolo, su due righe (prima a destra del balun, tagliata dal bordo); figurine passate ai pulsanti
            double sotto = Math.Min(zb, za) - ht * 0.18;
            o.Mark(0, -b / 2, sotto, "balun: " + Par("ali").Choices[ali], 1);
            o.Mark(0, -b / 2, sotto - ht * 0.16, "polarizzazione " + pol, 3);
            // quota H dell'altezza del triangolo, a sinistra (disegno di IC8TEM 13/11/2026 23:53)
            o.Mark(7, -b / 2 - b * 0.12, Math.Min(zb, za), "H " + Calcolatori.N(ht, 2), 3, -b / 2 - b * 0.12, Math.Max(zb, za));
            o.HideGround = true;
            if (ali == 3) o.Oks.Add("balun a lambda/4 (" + Calcolatori.N(lam / 4, 2) + " m) dal centro del lato orizzontale: massimo di corrente sui lati inclinati, polarizzazione verticale");
            if (h > 0 && h < 2) o.Warnings.Add("Il punto piu' basso e' sotto 2 m: perdite nel terreno e rischio di contatto.");
            return o;
        }
    }

    public class CalcFourSquare : SimpleCalc
    {
        public override bool Tubolare { get { return true; } }   // 0.4.30: tubi rastremabili (HF)
        public CalcFourSquare()
        {
            Key = "4sq"; Title = "Four-square"; Family = "Verticali"; Source = "ON4UN Low-Band DXing: lato lambda/4, correnti 0 / -90 / -90 / -180 gradi";
            Params.Add(new CalcParam("f", "Frequenza", 7.1, "MHz"));
            Params.Add(new CalcParam("d", "Diametro dei verticali", 30, "mm"));
            Params.Add(new CalcParam("l", "Lato del quadrato", 0.25, "lambda"));
            Params.Add(CalcParam.Scelta("dir", "Direzione del lobo", 0, null, "0°", "90°", "180°", "270°"));
            Params.Add(new CalcParam("zb", "Quota dei radiali (0 = terreno perfetto)", 2.5, "m"));
            Params.Add(new CalcParam("nr", "Radiali per verticale", 4, ""));
            CanTune = false;
        }
        public static readonly string[] Nomi = { "A", "B", "C", "D" };
        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            double f = P("f"), d = P("d") / 1000, lam = C0 / f, zb = Math.Max(0, P("zb"));
            double side = P("l") * lam, hv = 71.3 / f, lr = 75.0 / f;
            double r = side / Math.Sqrt(2);
            int dir = Math.Max(0, Math.Min(3, I("dir"))), nr = Math.Max(2, Math.Min(16, I("nr")));
            o.Card("Lato del quadrato", Calcolatori.N(side, 2) + " m", Calcolatori.N(P("l"), 2) + " lambda", true);
            o.Card("Verticali", Calcolatori.N(hv, 2) + " m", "71,3 / f");
            if (zb > 0) o.Card("Radiali", nr + " x " + Calcolatori.N(lr, 2) + " m", "a " + Calcolatori.N(zb, 1) + " m");
            o.Card("Correnti", "0 / -90 / -90 / -180°", "A dietro, B e C ai lati, D davanti");
            o.Formula = "Formule (ON4UN): verticali lambda/4 ai vertici di un quadrato di lato lambda/4, correnti uguali sfasate 0 (dietro), -90 (lati), -180 gradi (davanti). " +
                "Sorgenti a corrente: il programma ricava le tensioni (V = Z I) e mostra le impedenze risultanti, diverse per ogni verticale.";
            o.Warnings.Add("Le impedenze dei quattro verticali sono diverse e l'elemento A puo' avere resistenza negativa: la rete di sfasamento deve tenerne conto (ON4UN).");
            double seg = lam / 50;
            NecDeck dk = new NecDeck("Four-square " + Calcolatori.N(f, 3) + " MHz" + (zb > 0 ? " con radiali a " + Calcolatori.N(zb, 1) + " m" : ""), true);
            double[][] pos = { new[] { -r, 0.0 }, new[] { 0.0, r }, new[] { 0.0, -r }, new[] { r, 0.0 } };
            double th = dir * Math.PI / 2;
            int[] tags = new int[4];
            for (int i = 0; i < 4; i++)
            {
                double x = pos[i][0] * Math.Cos(th) - pos[i][1] * Math.Sin(th), y = pos[i][0] * Math.Sin(th) + pos[i][1] * Math.Cos(th);
                tags[i] = dk.Ws(seg, x, y, zb, x, y, zb + hv, d / 2, false, 15);
                if (zb > 0)
                    for (int k = 0; k < nr; k++)
                    {
                        // 0.4.20 (collaudo 0.4.20, fase 2H: EZNEC ferma il modello per Geometry Error): i radiali a 45 gradi lunghi quanto il
                        // lato arrivavano sulla base del verticale vicino e si sovrapponevano ai suoi. Ora vanno verso l'esterno, a ventaglio di
                        // 135 gradi centrato sulla direzione dal centro del quadrato al verticale (come il modello "Four-square 7 MHz con radiali"):
                        // il ventaglio dei vicini non si incrocia (i radiali estremi si incontrerebbero solo oltre 1,3 volte la loro lunghezza).
                        double ph0 = Math.Atan2(y, x);
                        double ph = ph0 + (nr > 1 ? (k - (nr - 1) / 2.0) * (135.0 / (nr - 1)) * Math.PI / 180.0 : 0);
                        dk.Ws(seg, x, y, zb, x + lr * Math.Cos(ph), y + lr * Math.Sin(ph), zb, 0.001, false, 9);
                    }
            }
            if (zb > 0) dk.Ground(2); else dk.Ground(1, true);
            string[] ex = { "1 0", "0 -1", "0 -1", "-1 0" };
            for (int i = 0; i < 4; i++) dk.Raw("EX 0 " + tags[i] + " 1 0 " + ex[i]);
            o.Nec = dk.End(f);
            o.CurrentDrive = true;
            o.WantedCurrents = new[] { Complex.One, Complex.FromPolarCoordinates(1, -Math.PI / 2), Complex.FromPolarCoordinates(1, -Math.PI / 2), Complex.FromPolarCoordinates(1, -Math.PI) };
            return o;
        }
    }

    public class CalcLoopMagnetica : SimpleCalc
    {
        double cScaledFormula(double cap) { return cap * Scale; }
        static readonly double[] Sigma = { 5.8e7, 3.5e7, 1.5e7 };
        static readonly string[] Mat = { "tubo di rame", "tubo di alluminio", "tubo di ottone" };
        public CalcLoopMagnetica()
        {
            Key = "loopmag"; Title = "Loop magnetica (piccolo loop trasmittente)"; Family = "Loop"; Source = "ARRL Antenna Book, W5QJR, AA5TB";
            Params.Add(new CalcParam("f", "Frequenza", 7.1, "MHz"));
            Params.Add(CalcParam.Scelta("forma", "Forma", 0, null, "cerchio", "ottagono", "quadrato"));
            Params.Add(new CalcParam("D", "Diametro", 1.0, "m", "Per il quadrato: il lato; per l'ottagono: il diametro tra due vertici opposti."));
            Params.Add(new CalcParam("mat", "Conduttore", 0, "") { Choices = Mat });
            Params.Add(new CalcParam("d", "ø del conduttore", 22, "mm"));
            // 1.5 (proposta l1 e bozzetto Calcolatori 1.5 approvati da IC8TEM il 18/11/2026; fattore di prossimita' di Smith non usato, risposta NO
            // delle 21:22): piu' spire con il solo effetto pelle e l'avviso sulla prossimita'
            Params.Add(new CalcParam("n", "Numero di spire", 1, "", "1 = una spira (come prima). Con piu' spire Rr cresce con N², la perdita per effetto pelle con N."));
            Params.Add(new CalcParam("passo", "Passo tra le spire (asse-asse)", 66, "mm", "Si usa con 2 o piu' spire: sotto 2 diametri del conduttore la perdita reale cresce per l'effetto di prossimita' (non calcolato)."));
            Params.Add(new CalcParam("p", "Potenza", 100, "W"));
            Params.Add(new CalcParam("h", "Quota", 3, "m", "Quota del centro per il calcolo con il terreno (0 = spazio libero)."));
            Params.Add(CalcParam.Scelta("acc", "Accoppiamento", 0, null, "loop di Faraday D/5", "gamma", "capacitivo (C1 serie, C2 parallelo)"));   // 1.5 a7: terza voce
            // 1.0.7 (IC8TEM 16/11/2026 19:54 "procedi"): "Accorda il condensatore (NEC)" cerca il C che porta la loop in risonanza nel modello
            // NEC (prima: CanTune = false e il C di formula lasciava la loop fuori risonanza, ROS 533 sul loop di accoppiamento)
        }

        public override string TuneLabel { get { return "Accorda il condensatore (NEC)"; } }

        /// <summary>1.0.7: fattore del condensatore che porta in risonanza la loop principale: massimo della resistenza vista dal loop di
        /// accoppiamento. Con Q di migliaia la risonanza e' larga circa 2/Q in C: prima una scansione fitta (passo 0,02 %) su +-4 %, poi la
        /// sezione aurea attorno al massimo.</summary>
        public override double Tune(double fmhz)
        {
            Func<double, double> rin = sc =>
            {
                Scale = sc;
                CalcOutput o = Compute();
                NecModel m = NecModel.Parse(o.Nec);
                SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, fmhz);
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(fmhz, Analysis.EffectiveSources(m, g, fmhz), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fmhz));
                return e.Sources[0].Z.Real;
            };
            double best = 1, rb = double.MinValue;
            for (int k = -200; k <= 200; k++)
            {
                double sc = 1 + k * 0.0002, r = rin(sc);
                if (r > rb) { rb = r; best = sc; }
            }
            double a = best - 0.0002, c = best + 0.0002, gr = (Math.Sqrt(5) - 1) / 2;
            double x1 = c - gr * (c - a), x2 = a + gr * (c - a), f1 = rin(x1), f2 = rin(x2);
            for (int it = 0; it < 30; it++)
            {
                if (f1 > f2) { c = x2; x2 = x1; f2 = f1; x1 = c - gr * (c - a); f1 = rin(x1); }
                else { a = x1; x1 = x2; f1 = f2; x2 = a + gr * (c - a); f2 = rin(x2); }
            }
            Scale = (a + c) / 2;
            return Scale;
        }

        const double Z0Rif = 50;   // 1.5 (a7): impedenza della linea per l'accoppiamento capacitivo

        static void Forma(int forma, double D, out double A, out double per, out double Deq)
        {
            if (forma == 1) { double s = D * Math.Sin(Math.PI / 8); per = 8 * s; A = 2 * Math.Sqrt(2) * D * D / 4; }
            else if (forma == 2) { per = 4 * D; A = D * D; }
            else { per = Math.PI * D; A = Math.PI * D * D / 4; }
            Deq = Math.Sqrt(4 * A / Math.PI);
        }

        static double Eff(int forma, double D, double dt, double f, double sigma, int nsp = 1)
        {
            double A, per, Deq;
            Forma(forma, D, out A, out per, out Deq);
            double lam = C0 / (f / 1e6);
            double rr = 31171 * Math.Pow(nsp * A / (lam * lam), 2);
            double rs = Math.Sqrt(Math.PI * f * 4e-7 * Math.PI / sigma);
            double rl = nsp * rs * per / (Math.PI * dt);
            return rr / (rr + rl);
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            int forma = I("forma"), mat = Math.Max(0, Math.Min(2, I("mat")));
            double D = P("D"), dt = P("d") / 1000, f = P("f") * 1e6, pw = P("p"), h = P("h");
            double lam = C0 / P("f");
            double A, per, Deq;
            Forma(forma, D, out A, out per, out Deq);
            int nsp = Math.Max(1, Math.Min(20, I("n")));
            double rr = 31171 * Math.Pow(nsp * A / (lam * lam), 2);
            double mu0 = 4e-7 * Math.PI, rs = Math.Sqrt(Math.PI * f * mu0 / Sigma[mat]);
            double rl = nsp * rs * per / (Math.PI * dt);
            double L = nsp * nsp * (forma == 2 ? 2 * mu0 * D / Math.PI * (Math.Log(D / (dt / 2)) - 0.774) : mu0 * Deq / 2 * (Math.Log(8 * Deq / dt) - 2));
            double w = 2 * Math.PI * f, xl = w * L;
            double cap = 1 / (w * w * L);
            double rt = rr + rl, q = xl / rt, ql = q / 2;
            double bw = f / ql;
            double i = Math.Sqrt(pw / rt), v = i * xl;
            double eff = rr / rt;
            Band band = Bands.All.Find(b0 => b0.Contains(P("f")));
            Hint("f", band != null ? Calcolatori.N(band.Low, 3) + "-" + Calcolatori.N(band.High, 3) : "");
            Hint("D", "circonferenza " + Calcolatori.N(per, 2) + " m = " + Calcolatori.N(per / lam, 3) + " lambda");
            Hint("h", "(per il calcolo con il terreno)");
            double dc = D / 5;
            Hint("acc", I("acc") == 0 ? "anello da " + Calcolatori.N(dc, 2) + " m" : I("acc") == 1 ? "presa a gamma sul lato basso" : "due condensatori sul lato basso");
            o.Card("Resistenza di radiazione", Calcolatori.Ohm(rr), Calcolatori.N(rr, 5) + " ohm");
            o.Card("Resistenza di perdita", Calcolatori.Ohm(rl), Mat[mat].Replace("tubo di ", "") + ", effetto pelle" + (nsp > 1 ? " (" + nsp + " spire)" : ""));
            if (nsp > 1)
            {
                double pas = P("passo");
                o.Warnings.Add("Con " + nsp + " spire la perdita e' solo quella dell'effetto pelle: l'effetto di prossimita' tra le spire vicine non e' calcolato, quindi il rendimento vero e' piu' basso (ARRL Antenna Book 15ª: 8 spire, 10 % calcolato, 3 % reale)." +
                    (pas > 0 && pas < 2 * P("d") ? " Passo di " + Calcolatori.N(pas, 0) + " mm, sotto 2 diametri del conduttore: allarga il passo." : ""));
                o.Oks.Add("Induttanza con N² (spire vicine) e modello NEC a una spira: il NEC qui verifica solo la geometria della spira singola.");
            }
            o.Card("Efficienza", Calcolatori.N(eff * 100, 0) + " %", Calcolatori.N(10 * Math.Log10(eff), 1) + " dB", true);
            o.Card("Guadagno", Calcolatori.N(10 * Math.Log10(eff) + 1.76, 1) + " dBi", "spazio libero");
            o.Card("Induttanza / C", Calcolatori.Henry(L).Replace("µH", "uH"), "C di accordo " + Calcolatori.Farad(cap));
            o.Card("Q / banda", Calcolatori.N(Math.Round(q / 100) * 100, 0) + " / " + Calcolatori.N(bw / 1000, 1) + " kHz", "Q caricato " + Calcolatori.N(Math.Round(ql / 10) * 10, 0));
            o.Card("Corrente / tensione su C", Calcolatori.N(i, 0) + " A / " + Calcolatori.N(v / 1000, 1) + " kV", "con " + Calcolatori.N(pw, 0) + " W");
            o.Formula = "Formule: Rr = 31171 (N·A)²/λ⁴ (N spire); Rperdita = C·Rs/(πd), Rs = √(πfμ0/σ); L = μ0 (D/2)(ln(8D/d) - 2); Q = X/R; banda = f/Q caricato. Fonti: ARRL Antenna Book, " +
                "W5QJR, AA5TB. Ogni calcolatore crea il modello NEC con le misure calcolate (qui il condensatore come carico C) e lo verifica con il motore: accanto ai " +
                "valori di formula compaiono quelli del calcolo NEC, e il modello si puo' aprire e salvare.";
            if (I("acc") == 2)
            {
                // 1.5 (a7, bozzetto AEH approvato 18/11/2026 23:16): accoppiamento capacitivo, AEH 4a ed. eq. 5-20 e 5-21 (Smith): C2 in parallelo
                // all'ingresso da 50 ohm, C1 in serie alla loop (fa anche da condensatore d'accordo)
                double r0 = Z0Rif, qm = Math.Sqrt(Math.Max(0, r0 / rt - 1));
                double c2 = qm / (w * r0), x1 = xl - Math.Sqrt(Math.Max(0, rt * (r0 - rt))), c1 = x1 > 0 ? 1 / (w * x1) : double.NaN;
                o.Card("C1 in serie con la loop", double.IsNaN(c1) ? "-" : Calcolatori.Farad(c1), Calcolatori.N(i * x1 / 1000, 1) + " kV con " + Calcolatori.N(pw, 0) + " W", true);
                o.Card("C2 in parallelo all'ingresso", Calcolatori.Farad(c2), Calcolatori.N(Math.Sqrt(pw * r0), 0) + " V", true);
                o.Formula += " Accoppiamento capacitivo (AEH 4a ed., eq. 5-20 e 5-21): C2 = sqrt(R0/R - 1)/(w R0), C1 = 1/(w (wL - sqrt(R (R0 - R)))).";
                o.Oks.Add("Accoppiamento capacitivo: il modello NEC ha la sorgente sul lato basso con C1 in serie; C2 non e' nel modello, la Z a 50 Ω si ottiene mettendo C2 in parallelo alla Z calcolata.");
            }
            if (Scale != 1) o.Card("C accordato (NEC)", Calcolatori.Farad(cScaledFormula(cap)), "risonanza nel modello, " + Calcolatori.N((Scale - 1) * 100, 2) + " % dalla formula");
            if (v > 1000) o.Warnings.Add(Calcolatori.N(v / 1000, 1) + " kV sul condensatore: serve un variabile a vuoto o con spaziatura adeguata.");
            if (bw < 10000) o.Warnings.Add("banda di " + Calcolatori.N(bw / 1000, 1) + " kHz: va riaccordato a ogni spostamento.");
            if (per < lam / 4) o.Oks.Add("circonferenza sotto 0,25 lambda: formule del piccolo loop valide.");
            else o.Warnings.Add("circonferenza oltre 0,25 lambda: le formule del piccolo loop non valgono piu', conta il NEC.");
            o.Oks.Add("Le giunzioni saldate o a vite aggiungono perdite (qui non contate).");
            o.Oks.Add("Verifica NEC: con il C di formula la loop e' vicina alla risonanza ma, con Q di migliaia, pochi decimi di pF spostano Z, come nella realta': \"Accorda il condensatore (NEC)\" trova il C esatto del modello (dalla 1.0.7).");

            o.Chart = new CalcChart { Title = "Efficienza al variare del diametro (" + Calcolatori.N(P("f"), 1) + " MHz, tubo " + Calcolatori.N(P("d"), 0) + " mm)", XLabel = "m", YLabel = "%" };
            o.Chart.X = Calcolatori.Lin(0.5, 2.5, 41); o.Chart.Y = new double[41];
            for (int k = 0; k < 41; k++) o.Chart.Y[k] = 100 * Eff(forma, o.Chart.X[k], dt, f, Sigma[mat], nsp);
            o.Chart.MarkX = D; o.Chart.MarkText = Calcolatori.N(D, 1) + " m: " + Calcolatori.N(eff * 100, 0) + " %";
            o.Chart.YMin = 0;

            // modello NEC: la loop (16 lati, 8 per l'ottagono, 4 per il quadrato) con il condensatore in alto e il loop di accoppiamento
            int ns = forma == 1 ? 8 : (forma == 2 ? 4 : 16);
            int perLato = forma == 0 ? 1 : (forma == 1 ? 2 : 4);
            double R = forma == 2 ? D / Math.Sqrt(2) : D / 2;
            double zc = h > 0 ? h : 0;
            double cScaled = cap * Scale;
            NecDeck dk = new NecDeck("Loop magnetica " + Calcolatori.N(P("f"), 3) + " MHz, " + Mat[mat] + " " + Calcolatori.N(P("d"), 0) + " mm");
            double off = forma == 2 ? Math.PI / 4 : (forma == 1 ? Math.PI / 8 : Math.PI / ns);
            int capTag = 0, gamTag = 0;
            for (int k = 0; k < ns; k++)
            {
                double a1 = -Math.PI / 2 + off + 2 * Math.PI * k / ns, a2 = a1 + 2 * Math.PI / ns;
                int t = dk.W(perLato, R * Math.Cos(a1), 0, zc + R * Math.Sin(a1), R * Math.Cos(a2), 0, zc + R * Math.Sin(a2), dt / 2);
                // lato in alto (attorno a +pi/2): condensatore
                double am = (a1 + a2) / 2;
                if (Math.Abs(Math.Sin(am) - 1) < 1e-6 || (capTag == 0 && Math.Sin(am) > Math.Sin(Math.PI / 2 - Math.PI / ns) - 1e-6)) capTag = t;
                if (gamTag == 0 && Math.Sin(am) < -Math.Sin(Math.PI / 2 - Math.PI / ns) + 1e-6) gamTag = t;
            }
            int feedTag;
            int feedSeg = 1;
            if (I("acc") == 0)
            {
                // loop di Faraday di diametro D/5, dentro la loop principale, appoggiato in basso
                double rc = dc / 2, zcc = zc - R * Math.Cos(Math.PI / ns) + rc + 0.02 * D;
                int first = 0;
                for (int k = 0; k < 8; k++)
                {
                    double a1 = -Math.PI / 2 + Math.PI / 8 + 2 * Math.PI * k / 8, a2 = a1 + 2 * Math.PI / 8;
                    int t = dk.W(1, rc * Math.Cos(a1), 0, zcc + rc * Math.Sin(a1), rc * Math.Cos(a2), 0, zcc + rc * Math.Sin(a2), 0.001);
                    if (k == 7) first = t;
                }
                feedTag = first;
                o.Mark(1, 0, zcc, "loop di accoppiamento " + Calcolatori.N(dc, 2) + " m", 1, 0, 0, rc);
                o.Mark(2, 0, zcc - rc, "coassiale 50 ohm", 3, 0, zcc - rc - R * 0.35);
            }
            else if (I("acc") == 2)
            {
                // 1.5 (a7): capacitivo, sorgente sul lato basso; C1 (= condensatore d'accordo) sullo stesso segmento, niente C in alto
                feedTag = gamTag == 0 ? 1 : gamTag;
                o.Mark(3, -R * 0.18, zc - R * 1.08, "C1 serie, C2 parallelo", 1, R * 0.18, zc - R * 1.08);
            }
            else
            {
                // gamma: presa sul lato basso (sorgente nel segmento del lato basso, stima)
                feedTag = gamTag == 0 ? 1 : gamTag;
                o.Mark(2, 0, zc - R, "gamma (stima)", 1, R * 0.4, zc - R * 0.8);
            }
            dk.Ground(h > 0 ? 2 : 0);
            if (I("acc") == 2)
            {
                double xc1 = xl - Math.Sqrt(Math.Max(0, rt * (Z0Rif - rt)));
                dk.Ld(0, feedTag, 1, 1, 0, 0, xc1 > 0 ? Scale / (w * xc1) : cScaled);
            }
            else dk.Ld(0, capTag == 0 ? 1 : capTag, 1, 1, 0, 0, cScaled);
            // 1.0.7: il materiale scelto entra nel modello (LD 5 su tutti i fili): prima i fili erano senza perdite e la verifica NEC dava
            // efficienza 100 % e 5,6 dBi contro il 16 % delle formule
            dk.Ld(5, 0, 0, 0, Sigma[mat], 0, 0);
            dk.Ex(feedTag, feedSeg);
            o.Nec = dk.End(P("f"));
            o.Plane = "xz";
            o.HideGround = true;
            // 1.5.1: con l'accoppiamento capacitivo non c'e' il condensatore in alto (C1 sta sul lato basso): niente etichetta in alto
            if (I("acc") != 2)
                o.Mark(3, -R * 0.18, zc + R * 1.0, "C " + Calcolatori.N(cap * 1e12, 0) + " pF, " + Calcolatori.N(v / 1000, 1) + " kV", 2, R * 0.18, zc + R * 1.0);
            o.Mark(0, -R * 1.55, zc, "ø " + Calcolatori.N(D, 2) + " m", 0);
            return o;
        }
    }

    public class CalcStackDl6wu : SimpleCalc
    {
        public CalcStackDl6wu()
        {
            Key = "stackdl6wu"; Title = "Distanze di stack DL6WU"; Family = "Strumenti"; Source = "DL6WU: D = lambda / (2 sin(apertura/2))";
            Params.Add(new CalcParam("f", "Frequenza", 144.3, "MHz"));
            Params.Add(new CalcParam("ah", "Apertura orizzontale a -3 dB", 30.9, "gradi"));
            Params.Add(new CalcParam("av", "Apertura verticale a -3 dB", 33.1, "gradi"));
            Params.Add(new CalcParam("boom", "Boom dell'antenna", 2.0, "lambda", "Per i limiti di validita' della formula (ARRL 24a ed.)."));
            // 0.4.20 (IC8TEM 14/11/2026 16:24): lo stack si crea da qui, con le distanze DL6WU
            Params.Add(new CalcParam("righe", "Antenne in verticale (righe)", 2, "", "Per il pulsante \"Crea lo stack nel modello aperto\": antenne una sopra l'altra, a passo DL6WU sovrapposte."));
            Params.Add(new CalcParam("col", "Antenne affiancate (colonne)", 1, "", "Per il pulsante \"Crea lo stack nel modello aperto\": antenne una accanto all'altra, a passo DL6WU affiancate, perpendicolari al lobo."));
            CanTune = false;
            PuoUsareModello = true;
        }
        // antenna base in spazio libero, calcolata una volta per modello e frequenza
        static readonly object cacheLock = new object();
        static string cacheKey;
        static RunResult cacheRun;
        static double cacheBoom;
        static bool cacheStack;

        // 0.4.20 (IC8TEM 14/11/2026 16:22: "mi devi permettere di non staccare se clicco sulla scheda calcolatori di dl6wu"): il calcolo
        // dell'antenna base girava dentro Compute, cioe' nel thread della finestra: con un modello grande la finestra restava ferma finche'
        // non finiva. Ora parte in sottofondo; intanto la scheda mostra i valori delle caselle e "calcolo in corso", e quando il calcolo
        // finisce l'evento Pronto fa ridisegnare la scheda con i valori del modello.
        public static event Action Pronto;
        /// <summary>Ultime distanze DL6WU calcolate (m) e azimut del lobo dell'antenna base (gradi, 0 se non c'e' un modello).</summary>
        public double UltimaDv = double.NaN, UltimaDh = double.NaN, UltimoPhiLobo;
        public bool DaModello;   // vero se l'ultimo calcolo ha preso aperture e lobo dal modello aperto
        static string inCorso;

        static bool InCache(NecModel m, double f, out RunResult r, out bool stack, out double boom, out string key)
        {
            key = m.ToNec() + "|" + f.ToString("R", CultureInfo.InvariantCulture);
            lock (cacheLock)
            {
                if (key == cacheKey) { r = cacheRun; stack = cacheStack; boom = cacheBoom; return true; }
            }
            r = null; stack = false; boom = 0;
            return false;
        }

        static void AvviaInSottofondo(NecModel m, double f, string key)
        {
            lock (cacheLock)
            {
                if (inCorso == key) return;
                inCorso = key;
            }
            NecModel copia = m.Clone();
            System.Threading.Tasks.Task.Run(() =>
            {
                try { bool st; double bm; BaseRun(copia, f, out st, out bm); }
                catch (Exception ex) { lock (cacheLock) { erroreKey = key; erroreTesto = ex.Message; } }
                finally { lock (cacheLock) { if (inCorso == key) inCorso = null; } }
                Action a = Pronto;
                if (a != null) a();
            });
        }
        static string erroreKey, erroreTesto;

        static RunResult BaseRun(NecModel m, double f, out bool stack, out double boom)
        {
            string key = m.ToNec() + "|" + f.ToString("R", CultureInfo.InvariantCulture);
            lock (cacheLock)
            {
                if (key == cacheKey) { stack = cacheStack; boom = cacheBoom; return cacheRun; }
            }
            StackInfo si = StackDetect.Detect(m);
            NecModel b = si != null ? StackDetect.BaseAntenna(m, si) : m.Clone();
            b.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
            SegmentGeometry g = b.BuildGeometry();
            RunResult r = Analysis.Run(b, g, f, 2.0);
            double bm = Ottimizzatore.Boom(b, g);
            lock (cacheLock) { cacheKey = key; cacheRun = r; cacheBoom = bm; cacheStack = si != null; }
            stack = si != null; boom = bm;
            return r;
        }

        // 13/11/2026 18:17 (IC8TEM): non e' un calcolo indipendente; con un modello aperto frequenza, aperture e boom vengono dal modello
        // della finestra principale (antenna base dello stack, se c'e', sempre in spazio libero come vuole la formula).
        /// <summary>1.3.1 (bozzetto approvato da IC8TEM il 18/11/2026 13:04): lo stack visto da davanti, con palo, telaio H, antenne
        /// (elemento e boom di punta), quote verticale e orizzontale e aperture a -3 dB (con DL6WU si toccano). Polarizzazione dal modello.</summary>
        void DisegnoStack(CalcOutput o, double lam, int nr, int nc, double dv, double dh)
        {
            bool vert = false;
            if (UsaModello && Model != null)
            {
                double sz = 0, sxy = 0;
                foreach (GeomCard g in Model.Geometry) if (g.Id == "GW") { sz += Math.Abs(g.F6 - g.F3); sxy += Math.Abs(g.F4 - g.F1) + Math.Abs(g.F5 - g.F2); }
                vert = sz > sxy;
            }
            o.Plane = "yz"; o.HideWires = true;
            double h = 0.24 * lam, rb = 0.025 * lam;
            double y0 = -(nc - 1) / 2.0 * dh, z0 = (nr - 1) / 2.0 * dv;
            double ra = Math.Min(nr > 1 ? dv : double.MaxValue, nc > 1 ? dh : double.MaxValue) / 2;
            if (double.IsInfinity(ra) || ra > 1e6) ra = Math.Max(dv, dh) / 2;
            double ytop = z0 + ra + 0.2 * lam, ybot = -z0 - ra - 0.1 * lam;
            // palo e telaio (il palo finisce sopra la quota orizzontale, cosi' la scritta resta libera)
            o.Mark(2, 0, ybot, null, 3, 0, ytop);
            if (nc > 1)
            {
                o.Mark(2, y0, 0, null, 3, -y0, 0);
                for (int j = 0; j < nc; j++) o.Mark(2, y0 + j * dh, -z0 - 0.15 * lam, null, 3, y0 + j * dh, z0 + 0.15 * lam);
            }
            int n = 0;
            for (int i = 0; i < nr; i++)
                for (int j = 0; j < nc; j++)
                {
                    double y = y0 + j * dh, z = z0 - i * dv;
                    o.Mark(1, y, z, null, 1, 0, 0, ra, true);
                    if (vert) o.Mark(2, y, z - h, null, 0, y, z + h); else o.Mark(2, y - h, z, null, 0, y + h, z);
                    o.Mark(1, y, z, null, 0, 0, 0, rb);
                    o.Mark(0, y + 0.12 * lam, z + 0.42 * ra, "A" + (++n), 0);
                }
            // quote: verticale a destra (testo a destra della linea), orizzontale sotto
            double qx = -y0 + ra + 0.3 * lam;
            if (nr > 1)
            {
                o.Mark(7, qx, z0, "", 3, qx, z0 - dv);
                o.Mark(0, qx + 0.15 * lam, z0 - dv / 2 + 0.05 * lam, N(dv, 3) + " m = " + N(dv / lam, 2) + " λ", 3);
            }
            if (nc > 1) o.Mark(7, y0, ybot - 0.25 * lam, N(dh, 3) + " m = " + N(dh / lam, 2) + " λ", 3, y0 + dh, ybot - 0.25 * lam);
            // spazio a destra per la scritta della quota verticale
            o.Mark(0, qx + 2.2 * lam, z0, "", 3);
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            DalModello.Clear();
            DaModello = false;
            double f = P("f"), ah = P("ah"), av = P("av"), bl = P("boom");
            string origine = null;
            if (UsaModello && Model != null && Model.Geometry.Count > 0)
            {
                try
                {
                    double fm = double.IsNaN(MainFreqMHz) || MainFreqMHz <= 0 ? Model.FreqMHz : MainFreqMHz;
                    bool stack; double boomM; RunResult r; string key;
                    if (!InCache(Model, fm, out r, out stack, out boomM, out key))
                    {
                        string err = null;
                        lock (cacheLock) { if (erroreKey == key) err = erroreTesto; }
                        if (err != null) throw new Exception(err);
                        AvviaInSottofondo(Model, fm, key);
                        o.Oks.Add("calcolo dell'antenna base del modello aperto in spazio libero in corso (in sottofondo): intanto i valori delle caselle; la scheda si aggiorna da sola");
                        r = null;
                    }
                    if (r == null) { }
                    else if (r.HpbwAz > 0 && r.HpbwEl > 0)
                    {
                        UltimoPhiLobo = r.PhiMax; DaModello = true;
                        f = fm; ah = r.HpbwAz; av = r.HpbwEl; bl = boomM / (C0 / fm);
                        DalModello["f"] = f; DalModello["ah"] = ah; DalModello["av"] = av; DalModello["boom"] = bl;
                        origine = (stack ? "antenna base dello stack aperto" : "modello aperto") + (string.IsNullOrEmpty(Model.FileName) ? "" : " (" + System.IO.Path.GetFileName(Model.FileName) + ")") +
                                  ", calcolato in spazio libero a " + N(fm, 3) + " MHz: " + N(r.GmaxDbi, 2) + " dBi, boom " + N(boomM, 3) + " m";
                    }
                    else o.Warnings.Add("Il modello aperto non ha un lobo principale con le due aperture a -3 dB: uso i valori scritti nelle caselle.");
                }
                catch (Exception ex) { o.Warnings.Add("Modello aperto non calcolabile in spazio libero (" + ex.Message + "): uso i valori scritti nelle caselle."); }
            }
            double lam = C0 / f;
            double dh = StackDetect.Dl6wu(lam, ah), dv = StackDetect.Dl6wu(lam, av);
            UltimaDv = dv; UltimaDh = dh;
            o.Add("Lunghezza d'onda", N(lam, 3) + " m");
            int nr = Math.Max(1, Math.Min(8, I("righe"))), nc = Math.Max(1, Math.Min(8, I("col")));
            // 1.3.1 (bozzetto approvato da IC8TEM il 18/11/2026 13:04): schede piu' strette (le distanze stanno nel disegno a destra)
            o.Add("Antenne", nr + " × " + nc + " = " + (nr * nc));
            o.Add("Affiancate (usa l'apertura orizzontale)", N(dh, 3) + " m = " + N(dh / lam, 2) + " lambda");
            o.Add("Sovrapposte (usa l'apertura verticale)", N(dv, 3) + " m = " + N(dv / lam, 2) + " lambda");
            DisegnoStack(o, lam, nr, nc, dv, dh);
            o.Formula = "Formula DL6WU: D = lambda / (2 sin(B/2)), B = apertura a -3 dB nel piano dello stack (ARRL 24a ed., Rothammel cap. 24, DG7YBN). " +
                "Limiti (ARRL 24a ed.): approssimata; vale nel piano E per boom oltre circa 0,7 lambda e nel piano H oltre 2 lambda; \"piu' sicura\" con boom di almeno 2 lambda.";
            if (origine != null) o.Oks.Add("valori presi dal " + origine + " (frequenza dalla barra in alto; le caselle si aggiornano da sole)");
            if (bl < 0.7) o.Warnings.Add("Boom sotto 0,7 lambda: la formula DL6WU non vale nemmeno nel piano E (ARRL 24a ed.). Verifica la distanza con il calcolo dello stack.");
            else if (bl < 2) o.Warnings.Add("Boom tra 0,7 e 2 lambda: formula valida nel piano E, non nel piano H (ARRL 24a ed.). Verifica con il calcolo dello stack.");
            else o.Oks.Add("boom di almeno 2 lambda: la formula DL6WU e' nel campo \"piu' sicuro\" (ARRL 24a ed.)");
            if (origine == null && !UsaModello)
                o.Oks.Add("casella \"Prendi i dati dal modello\" spenta: valori scritti a mano");
            else if (origine == null)
                o.Warnings.Add("Nessun modello aperto: valori scritti a mano. Aprendo un modello nella finestra principale frequenza, aperture e boom si prendono dal suo calcolo.");
            return o;
        }
    }
}
