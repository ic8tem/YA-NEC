// YA-NEC - linguette "RDF" e "Rumore e G/T" (bozzetti approvati il 12/11/2026), calcolate dal diagramma 3D del motore.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        StackPanel rdfBody;
        LineChart rdfChart;

        Border Riquadretto(string titolo, string valore, string sotto, bool evidenziato = false)
        {
            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = titolo, FontSize = 12.5, Foreground = Pal.B(Pal.Muted) });
            sp.Children.Add(new TextBlock { Text = valore, FontSize = 22, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            sp.Children.Add(new TextBlock { Text = sotto, FontSize = 12, Foreground = Pal.B(Pal.Muted) });
            return new Border
            {
                Background = Pal.B(evidenziato ? Pal.AccentSoft : Pal.Panel), BorderBrush = Pal.B(evidenziato ? Pal.Accent : Pal.Border), BorderThickness = new Thickness(evidenziato ? 2 : 1),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8), Margin = new Thickness(3), MinWidth = 170, Child = sp
            };
        }

        // ------------------------------------------------------------------------------------------------
        // RDF
        // ------------------------------------------------------------------------------------------------
        Control BuildRdfPage()
        {
            rdfBody = new StackPanel();
            rdfChart = new LineChart { Title = "RDF lungo l'elevazione (azimut del massimo)", XLabel = "gradi di elevazione", Height = 260 };
            StackPanel all = new StackPanel { Margin = new Thickness(4) };
            all.Children.Add(rdfBody);
            all.Children.Add(Pannello("RDF e guadagno in funzione dell'elevazione", rdfChart));
            all.Children.Add(Pannello("Come si calcola", Nota(
                "Guadagno medio = potenza irradiata nell'emisfero superiore / 4 pi (con il terreno) oppure media su tutta la sfera in spazio libero, " +
                "come EZNEC e IV3PRK (convenzione approvata il 12/11/2026). RDF = guadagno nella direzione voluta - guadagno medio (dB). " +
                "Caso di prova approvato: verticale lambda/4 a 1,83 MHz con 4 radiali a 0,5 m e con 32 radiali a 5 cm su terreno medio di Sommerfeld: RDF 5,07 dB in " +
                "entrambi i casi (IV3PRK 5,06 / 5,07); YA-NEC con Sommerfeld (GN 2): 5,06 dB.", Pal.Fore)));
            MostraRdf();
            return new ScrollViewer { Content = all };
        }

        void MostraRdf()
        {
            if (rdfBody == null) return;
            rdfBody.Children.Clear();
            if (run == null || run.Grid == null) { rdfBody.Children.Add(Pannello("RDF", Nota("Calcola prima un modello."))); return; }
            bool gnd = run.Engine.HasGround;
            RdfResult r = NoiseGt.Rdf(run.Grid, run.ThetaMax, run.PhiMax, run.GmaxDbi, gnd);
            WrapPanel cards = new WrapPanel();
            cards.Children.Add(Riquadretto("Guadagno", FN(r.GainDbi, 2) + " dBi", "elevazione " + FN(90 - r.Theta, 1) + " gradi, azimut " + FN(r.Phi, 1)));
            cards.Children.Add(Riquadretto("Guadagno medio", FN(r.AvgGainDb, 2) + " dB", gnd ? "emisfero superiore / 4 pi" : "su tutta la sfera"));
            cards.Children.Add(Riquadretto("RDF", FN(r.Rdf, 2) + " dB", "direttivita' in ricezione", true));
            rdfBody.Children.Add(cards);
            // RDF lungo l'elevazione
            List<double> xs = new List<double>(), ys = new List<double>(), gs = new List<double>();
            for (int e = 0; e <= 90; e++)
            {
                double th = 90 - e;
                double gdb = run.Engine.Gain(th, run.PhiMax).GainTotal;
                xs.Add(e); gs.Add(gdb); ys.Add(gdb - r.AvgGainDb);
            }
            rdfChart.X = xs.ToArray();
            rdfChart.Series = new List<LineSeries> {
                new LineSeries { Name = "RDF dB", Y = ys.ToArray(), Color = Pal.Accent },
                new LineSeries { Name = "guadagno dBi", Y = gs.ToArray(), Color = Pal.Green, Dashed = true } };
            rdfChart.InvalidateVisual();
        }

        // ------------------------------------------------------------------------------------------------
        // Rumore e G/T (come TANT / XLGTa)
        // ------------------------------------------------------------------------------------------------
        TextBox gtSky, gtGround, gtElev, gtLoss, gtNf;
        ComboBox gtBanda, gtAmb;
        TextBlock gtTabT;

        // ITU-R P.372: Fa in dB sopra kT0 (T0 = 290 K); f in MHz
        static double TdaFa(double fa) { return 290 * Math.Pow(10, fa / 10); }
        static double Tgalattico(double f) { return TdaFa(52 - 23 * Math.Log10(f)); }
        static double Tartificiale(double f, int amb) { double[] c = { 67.2, 72.5, 76.8 }; return TdaFa(c[Math.Max(0, Math.Min(2, amb))] - 27.7 * Math.Log10(f)); }

        void ApplicaTemperature()
        {
            if (gtBanda == null || gtAmb == null) return;
            double f = gtBanda.SelectedIndex == 1 ? 28.4 : gtBanda.SelectedIndex == 2 ? 50.2 : gtBanda.SelectedIndex == 3 ? 144.3 : gtBanda.SelectedIndex == 4 ? 432.2 : ParseNum(txtFreq.Text, 144.3);
            System.Text.StringBuilder sb = new System.Text.StringBuilder("ITU-R P.372 a " + FN(f, 1) + " MHz: cielo (galattico) " + FN(Tgalattico(f), 0) + " K; terreno rurale " +
                FN(Tartificiale(f, 0), 0) + " K, residenziale " + FN(Tartificiale(f, 1), 0) + " K, citta' " + FN(Tartificiale(f, 2), 0) + " K.");
            // 0.5.3 (IC8TEM 15/11/2026 21:57, "da ve7bqh o da tant cerca la temperatura usata di riferimento"): riferimenti delle tabelle
            // G/T di VE7BQH. 144 MHz: vecchio (preimpostato in TANT) cielo 200 K, terreno 1000 K; nuovo cielo 290 K, terreno rurale 1600,
            // residenziale 5400, citta' 14 550 K (ITU-R P.372-14; yu7ef.com/ve7bqh2.htm). 432 MHz: cielo 20 K, terreno 350 K (DG7YBN,
            // appendice al manuale di TANT, Dubus 2/2012, tabella 4). 1296 MHz e oltre: 10 K e 290 K (manuale di TANT). Elevazione 30 gradi.
            bool b144 = f > 100 && f < 200, b432 = f > 400 && f < 460, b1296 = f >= 1000;
            if (b144) sb.Append(" VE7BQH 144: vecchio (TANT) cielo 200 K e terreno 1000 K; nuovo cielo 290 K, terreno rurale 1600, residenziale 5400, citta' 14 550 K.");
            if (b432) sb.Append(" VE7BQH 432 (DG7YBN): cielo 20 K, terreno 350 K.");
            if (b1296) sb.Append(" TANT da 1296 MHz: cielo 10 K, terreno 290 K.");
            gtTabT.Text = sb.ToString();
            int a = gtAmb.SelectedIndex;
            if (a <= 0) return;
            double tsky = Tgalattico(f), tg = Tartificiale(f, a >= 4 ? Math.Max(0, a - 5) : a - 1);
            if (a == 4)
            {
                // riferimento vecchio di VE7BQH, come TANT
                if (b144) { tsky = 200; tg = 1000; } else if (b432) { tsky = 20; tg = 350; } else if (b1296) { tsky = 10; tg = 290; }
            }
            else if (a >= 5)
            {
                // riferimento nuovo di VE7BQH (solo 144 MHz); sulle altre bande cielo galattico e terreno ITU dello stesso ambiente,
                // a 432 e da 1296 MHz il cielo freddo del riferimento
                double[] t144 = { 1600, 5400, 14550 };
                if (b144) { tsky = 290; tg = t144[a - 5]; }
                else if (b432) tsky = 20;
                else if (b1296) tsky = 10;
            }
            gtSky.Text = FN(tsky, 0);
            gtGround.Text = FN(tg, 0);
            CalcolaGt();
        }
        CheckBox gtKf;
        WrapPanel gtCards;
        LineChart gtChart;
        StackPanel gtTable;
        int gtStep = 2;   // dalla 0.4.2 (approvato da IC8TEM il 13/11/2026): riusa il diagramma a 2 gradi del calcolo, scarto sul G/T sotto 0,01 dB

        Control BuildGtPage()
        {
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,10,*") };
            StackPanel left = new StackPanel(), right = new StackPanel();
            StackPanel inp = new StackPanel { Spacing = 2 };
            // 0.5.3 (IC8TEM 15/11/2026 21:57): tolta la scelta "Cielo: mappa (galassia)", mai fatta; il cielo e' una temperatura costante, come in TANT
            gtSky = Tb("200", 70); gtGround = Tb("1000", 70);
            inp.Children.Add(Riga("T del cielo", gtSky, new TextBlock { Text = "K", VerticalAlignment = VerticalAlignment.Center }));
            inp.Children.Add(Riga("T del terreno", gtGround, new TextBlock { Text = "K", VerticalAlignment = VerticalAlignment.Center }));
            // temperature tipiche per banda e ambiente (ITU-R P.372: rumore galattico Fa = 52 - 23 log f; rumore artificiale
            // rurale / residenziale / citta' Fa = 67,2 / 72,5 / 76,8 - 27,7 log f; T = 290 * 10^(Fa/10)); a 144 e 432 anche il
            // riferimenti delle tabelle G/T di VE7BQH (vecchio come TANT e nuovo; vedi ApplicaTemperature)
            gtBanda = new ComboBox { ItemsSource = new[] { "dalla frequenza", "28 MHz", "50 MHz", "144 MHz", "432 MHz" }, SelectedIndex = 0, Width = 150 };
            gtAmb = new ComboBox { ItemsSource = new[] { "a mano", "rurale", "residenziale", "citta'", "VE7BQH vecchio (TANT)", "VE7BQH nuovo, rurale", "VE7BQH nuovo, residenziale", "VE7BQH nuovo, citta'" }, SelectedIndex = 0, Width = 230 };
            gtBanda.SelectionChanged += (s, e) => ApplicaTemperature();
            gtAmb.SelectionChanged += (s, e) => ApplicaTemperature();
            ToolTip.SetTip(gtAmb, "Rurale / residenziale / citta': cielo galattico medio e rumore artificiale dell'ambiente (ITU-R P.372). VE7BQH vecchio (preimpostato in TANT): 144 MHz cielo 200 K e terreno 1000 K; 432 MHz 20 K e 350 K; da 1296 MHz 10 K e 290 K. VE7BQH nuovo (144 MHz): cielo 290 K, terreno 1600 / 5400 / 14 550 K. Le tabelle G/T di VE7BQH sono calcolate a 30 gradi di elevazione.");
            inp.Children.Add(Riga("Temperature tipiche", gtBanda, gtAmb));
            gtTabT = new TextBlock { FontSize = 12, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, MaxWidth = 430 };
            inp.Children.Add(gtTabT);
            // 0.5.3 (IC8TEM 15/11/2026 20:22: "se non lo posso scegliere ... togli la scelta"): la riflessione del suolo non e' mai stata fatta;
            // resta solo il metodo di TANT (spazio libero, terreno come temperatura), detto in una riga
            inp.Children.Add(Riga("Terreno", new TextBlock { Text = "spazio libero, come TANT (il terreno conta solo come temperatura)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, MaxWidth = 330 }));
            gtElev = Tb("30", 60);
            inp.Children.Add(Riga("Elevazione", gtElev, new TextBlock { Text = "gradi (puntamento: Luna, satellite)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            inp.Children.Add(Riga("Diagramma 3D", Scelta(new[] { "1", "2", "5" }, 1, k => { gtStep = k == 0 ? 1 : (k == 1 ? 2 : 5); CalcolaGt(); }), new TextBlock { Text = "gradi, dal motore", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            inp.Children.Add(new TextBlock { Text = "Perdite e ricevitore", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 8, 0, 2), Foreground = Pal.B(Pal.Fore) });
            gtLoss = Tb("0,09", 60); gtNf = Tb("0,30", 60);
            inp.Children.Add(Riga("Perdite (linee, divisore)", gtLoss, new TextBlock { Text = "dB", VerticalAlignment = VerticalAlignment.Center }));
            inp.Children.Add(Riga("Preamplificatore NF", gtNf, new TextBlock { Text = "dB", VerticalAlignment = VerticalAlignment.Center }));
            gtKf = new CheckBox { Content = "Correzione KF2YN (guadagno medio del modello)", IsChecked = true };
            ToolTip.SetTip(gtKf, "Divide per il guadagno medio vero del modello invece che per 4 pi (KF2YN).");
            inp.Children.Add(gtKf);
            // 0.5.3 (IC8TEM 15/11/2026 20:17, bozzetto approvato alle 20:19): stack di 2 antenne o 2 x 2 per un G/T con senso, distanze
            // suggerite DL6WU dalle aperture dell'antenna; sotto i 28 MHz avviso e niente stack
            StackPanel stk = new StackPanel { Spacing = 2 };
            stk.Children.Add(new TextBlock { Text = "Stack", FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            stk.Children.Add(Riga("Antenne", Scelta(new[] { "singola", "2 affiancate", "2 sovrapposte", "2 x 2" }, 0, k => { gtStackModo = k; AggiornaStackGt(); CalcolaGt(); })));
            gtDv = Tb("", 70); gtDh = Tb("", 70); gtDiv = Tb("0,20", 60);
            gtSugV = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), FontSize = 12.5, TextWrapping = TextWrapping.Wrap, MaxWidth = 300 };
            gtSugH = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), FontSize = 12.5, TextWrapping = TextWrapping.Wrap, MaxWidth = 300 };
            stk.Children.Add(Riga("Distanza verticale", gtDv, new TextBlock { Text = "m", VerticalAlignment = VerticalAlignment.Center }, gtSugV));
            stk.Children.Add(Riga("Distanza orizzontale", gtDh, new TextBlock { Text = "m", VerticalAlignment = VerticalAlignment.Center }, gtSugH));
            stk.Children.Add(Riga("Perdita del divisore", gtDiv, new TextBlock { Text = "dB  divisore + cavi di fase", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            Button bSug = Btn("Usa le distanze suggerite", false), bCrea = Btn("Crea lo stack nel modello aperto", false);
            bSug.Click += (s, e) =>
            {
                if (!double.IsNaN(gtSugDv)) gtDv.Text = FN(gtSugDv, 3);
                if (!double.IsNaN(gtSugDh)) gtDh.Text = FN(gtSugDh, 3);
                CalcolaGt();
            };
            bCrea.Click += (s, e) => CreaStackDaGt();
            WrapPanel bstk = new WrapPanel();
            foreach (Button b in new[] { bSug, bCrea }) { b.Margin = new Thickness(0, 4, 8, 0); bstk.Children.Add(b); }
            stk.Children.Add(bstk);
            stk.Children.Add(new YaNec.Controls.TestoAdattato { Child = Nota("Calcolo con il NEC sullo stack completo (accoppiamento tra le antenne compreso), sempre in spazio libero come TANT, poi lo stesso integrale del rumore; la perdita del divisore si somma alle perdite.") });
            gtStackBox = new Border { Child = stk, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8), Margin = new Thickness(0, 8, 0, 6) };
            inp.Children.Add(gtStackBox);
            foreach (TextBox t in new[] { gtDv, gtDh, gtDiv }) t.LostFocus += (s, e) => { if (gtStackModo > 0) CalcolaGt(); };
            Button bc = Btn("Calcola", true);
            bc.Click += (s, e) => CalcolaGt();
            inp.Children.Add(bc);
            foreach (TextBox t in new[] { gtSky, gtGround, gtElev, gtLoss, gtNf }) t.LostFocus += (s, e) => CalcolaGt();
            gtKf.IsCheckedChanged += (s, e) => CalcolaGt();
            // il testo grigio va a capo alla larghezza dei dati (anche su tre righe) e non allarga il riquadro di sinistra
            inp.Children.Add(new YaNec.Controls.TestoAdattato { Child = Nota("Metodo: integrazione del diagramma 3D per angoli solidi finiti (somma = 4 pi esatta, come XLGTa di AC6LA), scansione dell'elevazione come TANT di YT1NT/VE3EA, " +
                "correzione KF2YN del guadagno medio. Tloss = 290 (1 - 10^(-L/10)); Tpreampli = 290 (10^(NF/10) - 1); G/T = G - 10 log T.") });
            left.Children.Add(Pannello("Dati di ingresso", inp));

            gtAvviso28 = new Border
            {
                IsVisible = false, Background = Pal.B(Pal.WarnBg), BorderBrush = Pal.B(Pal.Orange), BorderThickness = new Thickness(5, 0, 0, 0), CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 8), Margin = new Thickness(0, 0, 0, 8),
                Child = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), Text = "Sotto i 28 MHz il G/T non ha senso di paragone: il rumore esterno (atmosferico, artificiale, galattico) supera di molto quello dell'antenna e del ricevitore. Il calcolo si fa lo stesso, senza stack." }
            };
            right.Children.Add(gtAvviso28);
            gtCards = new WrapPanel();
            right.Children.Add(gtCards);
            gtChart = new LineChart { Title = "T antenna in funzione dell'elevazione", XLabel = "gradi", Height = 260 };
            right.Children.Add(Pannello("T antenna in funzione dell'elevazione", gtChart));
            gtTable = new StackPanel();
            right.Children.Add(Pannello("Da dove arriva il rumore", gtTable));
            Grid.SetColumn(left, 0); Grid.SetColumn(right, 2);
            g.Children.Add(left); g.Children.Add(right);
            return new ScrollViewer { Content = g };
        }

        int gtStackModo;
        TextBox gtDv, gtDh, gtDiv;
        TextBlock gtSugV, gtSugH;
        Border gtStackBox, gtAvviso28;
        double gtSugDv = double.NaN, gtSugDh = double.NaN;
        static readonly int[] GtRighe = { 1, 1, 2, 2 }, GtColonne = { 1, 2, 1, 2 };
        static readonly string[] GtNomi = { "singola", "2 affiancate", "2 sovrapposte", "stack 2 x 2" };

        void AggiornaStackGt()
        {
            if (gtDv == null) return;
            gtDv.IsEnabled = GtRighe[gtStackModo] > 1;
            gtDh.IsEnabled = GtColonne[gtStackModo] > 1;
            gtDiv.IsEnabled = gtStackModo > 0;
        }

        /// <summary>Distanze DL6WU dalle aperture a -3 dB: D = lambda / (2 sin(apertura/2)); verticale dall'elevazione, orizzontale dall'azimut.</summary>
        static double Dl6wu(double fMHz, double apertura)
        {
            if (double.IsNaN(apertura) || apertura <= 0 || apertura >= 180) return double.NaN;
            return 299.792458 / fMHz / (2 * Math.Sin(apertura / 2 * Math.PI / 180));
        }

        void CreaStackDaGt()
        {
            if (model == null || run == null) { Status("Calcola prima il modello (servono le aperture e la direzione del lobo).", true); return; }
            if (gtStackModo == 0) { Status("Scegli 2 affiancate, 2 sovrapposte o 2 x 2.", true); return; }
            try
            {
                double dv = ParseNum(gtDv.Text, gtSugDv), dh = ParseNum(gtDh.Text, gtSugDh);
                List<string> note = new List<string>();
                NecModel nm = StackDetect.Crea(model, GtRighe[gtStackModo], GtColonne[gtStackModo], dv, dh, run.PhiMax, note);
                string fn = model.FileName;
                SetModel(nm, null);
                model.FileName = string.IsNullOrEmpty(fn) ? "stack.nec" : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fn) ?? "", System.IO.Path.GetFileNameWithoutExtension(fn) + " stack " + GtRighe[gtStackModo] + "x" + GtColonne[gtStackModo] + ".nec");
                Registro.Scrivi("AZIONE", "stack creato da Rumore/GT/Stack: " + string.Join(" ", note));
                Status(string.Join(" ", note) + " Il file su disco non e' cambiato.");
                Calculate(true);
            }
            catch (Exception ex) { Status("Stack non creato: " + ex.Message, true); }
        }

        static PatternGrid GrigliaGt(RunResult r, int step)
        {
            if (step == (int)Math.Round(r.Grid.ThetaStep)) return r.Grid;
            PatternGrid pg = new PatternGrid { ThetaStep = step, PhiStep = step, NTheta = 180 / step + 1, NPhi = 360 / step };
            pg.Total = new double[pg.NTheta, pg.NPhi];
            // una riga di elevazione per processore (0.4.2): il campo lontano legge solo i dati della soluzione
            System.Threading.Tasks.Parallel.For(0, pg.NTheta, i =>
            {
                for (int j = 0; j < pg.NPhi; j++) pg.Total[i, j] = r.Engine.Gain(pg.ThetaAt(i), pg.PhiAt(j)).GainTotal;
            });
            return pg;
        }

        class GtCalcolo { public GtResult Singola, Stack; public double SugDv = double.NaN, SugDh = double.NaN, F, GstackIdeale; public string Nota; public bool GiaStack; }

        // 0.5.3 (IC8TEM 15/11/2026 20:22, "qua mi metti dei fumetti che mi spiegano per bene?"): fumetto su ogni riquadro dei risultati
        static readonly Dictionary<string, string> FumettiRiquadriGt = new Dictionary<string, string>
        {
            { "Guadagno", "Guadagno massimo dell'antenna (o dello stack) in spazio libero, in dBi: come fa TANT, il G/T si calcola senza il terreno sotto l'antenna; il terreno entra solo come temperatura (T del terreno)." },
            { "Guadagno di stack", "Quanto guadagna lo stack rispetto all'antenna singola. L'ideale e' +3 dB con 2 antenne e +6 dB con 4: si perde qualcosa per l'accoppiamento tra le antenne e se le distanze non sono quelle giuste." },
            { "T dal diagramma", "Temperatura di rumore che l'antenna raccoglie dal cielo e dal terreno: ogni direzione pesa quanto il guadagno dell'antenna in quella direzione (integrale del diagramma 3D per angoli solidi). Con l'antenna puntata all'elevazione scritta nei dati." },
            { "T delle perdite", "Rumore aggiunto dalle perdite: cavo e divisore (Tloss = 290 (1 - 10^(-L/10)), L in dB) e perdite dell'antenna stessa (efficienza sotto il 100 %). 0,1 dB di cavo valgono circa 6,6 K." },
            { "T antenna", "Temperatura di rumore all'uscita dell'antenna, prima del preamplificatore: T dal diagramma + T delle perdite. E' la T che VE7BQH usa nelle sue tabelle." },
            { "G/T antenna", "Figura di merito dell'antenna in ricezione: G/T = G - 10 log(T antenna), in dB/K. Piu' alto e' meglio. E' il numero delle tabelle G/T di VE7BQH (con le stesse temperature del cielo e del terreno si possono confrontare le antenne): +1 dB di G/T e' 1 dB in piu' di segnale/rumore." },
            { "T sistema", "Temperatura di rumore di tutto il ricevitore riportata all'antenna: T antenna + rumore del preamplificatore (Tpre = 290 (10^(NF/10) - 1), NF in dB). 0,3 dB di NF valgono circa 20,7 K." },
            { "G/T sistema", "Quello che si sente davvero: G/T con il rumore del preamplificatore compreso, G - 10 log(T sistema). Serve per confrontare stazioni intere (antenna + cavo + preampli), per esempio per l'EME o i satelliti." },
            { "Stack", "Il modello aperto e' gia' uno stack: si calcola cosi' com'e' e la parte Stack dei dati non si usa." },
            { "G/T", "Figura di merito in ricezione, guadagno diviso per la temperatura di rumore (in dB/K)." },
        };

        void FumettiGt()
        {
            foreach (Control c in gtCards.Children)
            {
                if (!(c is Border b) || !(b.Child is StackPanel sp) || sp.Children.Count == 0 || !(sp.Children[0] is TextBlock t)) continue;
                string tip;
                if (!FumettiRiquadriGt.TryGetValue(t.Text ?? "", out tip))
                    foreach (KeyValuePair<string, string> kv in FumettiRiquadriGt) if (Lang.T(kv.Key) == t.Text) tip = kv.Value;
                if (tip != null) ToolTip.SetTip(b, tip);
            }
        }

        void CalcolaGt()
        {
            if (gtCards == null) return;
            gtCards.Children.Clear();
            gtTable.Children.Clear();
            if (run == null) { gtCards.Children.Add(Riquadretto("G/T", "-", "calcola prima un modello")); return; }
            double tsky = ParseNum(gtSky.Text, 200), tg = ParseNum(gtGround.Text, 1000), el = ParseNum(gtElev.Text, 30);
            double loss = ParseNum(gtLoss.Text, 0), nf = ParseNum(gtNf.Text, 0);
            bool kf = gtKf.IsChecked == true;
            RunResult r0 = run;
            NecModel m0 = model;
            int step = gtStep;
            bool sotto28 = r0.FreqMHz < 28;
            if (gtAvviso28 != null) gtAvviso28.IsVisible = sotto28;
            if (gtStackBox != null) gtStackBox.IsEnabled = !sotto28;
            int modo = sotto28 ? 0 : gtStackModo;
            double dvTxt = gtDv != null ? ParseNum(gtDv.Text, double.NaN) : double.NaN, dhTxt = gtDh != null ? ParseNum(gtDh.Text, double.NaN) : double.NaN;
            double div = gtDiv != null ? Math.Max(0, ParseNum(gtDiv.Text, 0.2)) : 0.2;
            gtCards.Children.Add(Riquadretto("G/T", "...", modo > 0 ? "calcolo in corso: antenna singola e " + GtNomi[modo] + " (spazio libero, come TANT)" : "calcolo in corso (antenna in spazio libero, come TANT)"));
            gtChart.EmptyText = "calcolo in corso...";
            gtChart.InvalidateVisual();
            Task.Run(() =>
            {
                GtCalcolo c = new GtCalcolo { F = r0.FreqMHz };
                // come TANT: il G/T si calcola sempre con l'antenna in spazio libero (il terreno conta solo come temperatura)
                RunResult r = r0;
                NecModel mf = m0.Clone();
                mf.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
                mf.GroundFlag = 0;
                if (r0.Engine.HasGround || r0.Model.Ground.Kind != GroundKind.FreeSpace) r = Analysis.Run(mf, mf.BuildGeometry(), r0.FreqMHz, 2.0);
                c.SugDv = Dl6wu(r.FreqMHz, r.HpbwEl);
                c.SugDh = Dl6wu(r.FreqMHz, r.HpbwAz);
                using (Prestazioni.Misura("Rumore e G/T: integrazione", "elevazione " + el.ToString(System.Globalization.CultureInfo.InvariantCulture) + " gradi"))
                    c.Singola = NoiseGt.Gt(GrigliaGt(r, step), r.ThetaMax, r.PhiMax, r.GmaxDbi, el, tsky, tg, loss, nf, kf, r.Efficiency > 0 ? r.Efficiency : 100);
                if (modo > 0)
                {
                    if (StackDetect.Detect(mf) != null) { c.GiaStack = true; return c; }
                    double dv = double.IsNaN(dvTxt) || dvTxt <= 0 ? c.SugDv : dvTxt, dh = double.IsNaN(dhTxt) || dhTxt <= 0 ? c.SugDh : dhTxt;
                    List<string> note = new List<string>();
                    NecModel ms = StackDetect.Crea(mf, GtRighe[modo], GtColonne[modo], dv, dh, r.PhiMax, note);
                    RunResult rs;
                    using (Prestazioni.Misura("Rumore/GT/Stack: calcolo dello stack", GtNomi[modo]))
                        rs = Analysis.Run(ms, ms.BuildGeometry(), r0.FreqMHz, 2.0);
                    using (Prestazioni.Misura("Rumore/GT/Stack: integrazione dello stack", GtNomi[modo]))
                        c.Stack = NoiseGt.Gt(GrigliaGt(rs, step), rs.ThetaMax, rs.PhiMax, rs.GmaxDbi, el, tsky, tg, loss + div, nf, kf, rs.Efficiency > 0 ? rs.Efficiency : 100);
                    c.GstackIdeale = 10 * Math.Log10(GtRighe[modo] * GtColonne[modo]);
                    c.Nota = GtNomi[modo] + ": verticale " + (GtRighe[modo] > 1 ? FN(dv, 3) + " m" : "-") + ", orizzontale " + (GtColonne[modo] > 1 ? FN(dh, 3) + " m" : "-") + ", divisore " + FN(div, 2) + " dB";
                }
                return c;
            }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
            {
                gtCards.Children.Clear();
                if (t.IsFaulted) { gtCards.Children.Add(Riquadretto("G/T", "errore", t.Exception.GetBaseException().Message)); return; }
                GtCalcolo c = t.Result;
                GtResult x = c.Singola, y = c.Stack;
                gtSugDv = c.SugDv; gtSugDh = c.SugDh;
                if (gtSugV != null)
                {
                    gtSugV.Text = double.IsNaN(c.SugDv) ? "" : "suggerita " + FN(c.SugDv, 2) + " m (DL6WU, apertura V " + FN(run != null ? run.HpbwEl : double.NaN, 1) + "°)";
                    gtSugH.Text = double.IsNaN(c.SugDh) ? "" : "suggerita " + FN(c.SugDh, 2) + " m (DL6WU, apertura H " + FN(run != null ? run.HpbwAz : double.NaN, 1) + "°)";
                    if (string.IsNullOrWhiteSpace(gtDv.Text) && !double.IsNaN(c.SugDv)) gtDv.Text = FN(c.SugDv, 3);
                    if (string.IsNullOrWhiteSpace(gtDh.Text) && !double.IsNaN(c.SugDh)) gtDh.Text = FN(c.SugDh, 3);
                }
                if (y == null)
                {
                    gtCards.Children.Add(Riquadretto("Guadagno", FN(x.GainDbi, 2) + " dBi", "spazio libero"));
                    gtCards.Children.Add(Riquadretto("T dal diagramma", FN(x.Tpattern, 1) + " K", "a " + FN(el, 0) + " gradi di elevazione"));
                    gtCards.Children.Add(Riquadretto("T delle perdite", FN(x.Tloss, 1) + " K", "cavo " + FN(loss, 2) + " dB, antenna " + FN(x.EfficienzaPct, 1) + " %"));
                    gtCards.Children.Add(Riquadretto("T antenna", FN(x.Tant, 1) + " K", "diagramma + perdite"));
                    gtCards.Children.Add(Riquadretto("G/T antenna", FN(x.GtAnt, 2) + " dB", "come VE7BQH", true));
                    gtCards.Children.Add(Riquadretto("T sistema", FN(x.Tsys, 1) + " K", "con il preampli (" + FN(x.Tpre, 1) + " K)"));
                    gtCards.Children.Add(Riquadretto("G/T sistema", FN(x.GtSys, 2) + " dB", "quello che si sente", true));
                    if (c.GiaStack) gtCards.Children.Add(Riquadretto("Stack", "gia' nel modello", "il modello aperto e' gia' uno stack: si calcola com'e'"));
                }
                else
                {
                    gtCards.Children.Add(Riquadretto("Guadagno", FN(y.GainDbi, 2) + " dBi", "stack (singola " + FN(x.GainDbi, 2) + ")"));
                    gtCards.Children.Add(Riquadretto("Guadagno di stack", (y.GainDbi - x.GainDbi >= 0 ? "+" : "") + FN(y.GainDbi - x.GainDbi, 2) + " dB", "ideale +" + FN(c.GstackIdeale, 1)));
                    gtCards.Children.Add(Riquadretto("T antenna", FN(y.Tant, 1) + " K", "stack (singola " + FN(x.Tant, 1) + ")"));
                    gtCards.Children.Add(Riquadretto("G/T antenna", FN(y.GtAnt, 2) + " dB", "singola " + FN(x.GtAnt, 2), true));
                    gtCards.Children.Add(Riquadretto("T sistema", FN(y.Tsys, 1) + " K", "singola " + FN(x.Tsys, 1)));
                    gtCards.Children.Add(Riquadretto("G/T sistema", FN(y.GtSys, 2) + " dB", "singola " + FN(x.GtSys, 2), true));
                }
                FumettiGt();
                gtChart.X = x.CurveElev;
                List<LineSeries> ser = new List<LineSeries> { new LineSeries { Name = y == null ? "T antenna K" : "singola, K", Y = x.CurveTant, Color = Pal.Accent } };
                if (y != null && y.CurveTant != null && y.CurveTant.Length == x.CurveTant.Length) ser.Add(new LineSeries { Name = GtNomi[gtStackModo] + ", K", Y = y.CurveTant, Color = Pal.Orange, Dashed = true });
                gtChart.Series = ser;
                gtChart.MarkX = el;
                gtChart.InvalidateVisual();
                List<string[]> rows = new List<string[]>();
                if (y == null)
                {
                    rows.Add(new[] { "Zona", "Peso", "Contributo" });
                    rows.Add(new[] { "cielo (sopra l'orizzonte)", FN(100 * x.SkyFraction, 1) + " %", FN(x.SkyK, 1) + " K" });
                    rows.Add(new[] { "terreno (sotto l'orizzonte)", FN(100 * x.GroundFraction, 1) + " %", FN(x.GroundK, 1) + " K" });
                    rows.Add(new[] { "perdite", "-", FN(x.Tloss, 1) + " K" });
                    rows.Add(new[] { "totale", "", FN(x.Tant, 1) + " K" });
                }
                else
                {
                    rows.Add(new[] { "Zona", "singola peso", "singola K", "stack peso", "stack K" });
                    rows.Add(new[] { "cielo (sopra l'orizzonte)", FN(100 * x.SkyFraction, 1) + " %", FN(x.SkyK, 1), FN(100 * y.SkyFraction, 1) + " %", FN(y.SkyK, 1) });
                    rows.Add(new[] { "terreno (sotto l'orizzonte)", FN(100 * x.GroundFraction, 1) + " %", FN(x.GroundK, 1), FN(100 * y.GroundFraction, 1) + " %", FN(y.GroundK, 1) });
                    rows.Add(new[] { "perdite (+ divisore per lo stack)", "-", FN(x.Tloss, 1), "-", FN(y.Tloss, 1) });
                    rows.Add(new[] { "totale", "", FN(x.Tant, 1), "", FN(y.Tant, 1) });
                }
                gtTable.Children.Clear();
                int nc = rows[0].Length;
                Grid tg2 = new Grid { ColumnDefinitions = new ColumnDefinitions(nc == 3 ? "2*,*,*" : "2*,*,*,*,*") };
                for (int i = 0; i < rows.Count; i++)
                {
                    tg2.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int cc = 0; cc < nc; cc++)
                    {
                        TextBlock tb = new TextBlock { Text = rows[i][cc], Margin = new Thickness(4, 2), Foreground = Pal.B(Pal.Fore), FontWeight = (i == 0 || i == rows.Count - 1) ? FontWeight.Bold : FontWeight.Normal,
                            HorizontalAlignment = cc == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                        Grid.SetRow(tb, i); Grid.SetColumn(tb, cc);
                        tg2.Children.Add(tb);
                    }
                }
                gtTable.Children.Add(tg2);
                gtTable.Children.Add(Nota("Guadagno medio del modello = " + FN(x.AvgGain, 4) + (kf ? " (correzione KF2YN applicata)" : " (senza correzione)") + (y != null ? ". " + c.Nota + "; guadagno medio dello stack = " + FN(y.AvgGain, 4) : "")));
            }));
        }
    }
}
