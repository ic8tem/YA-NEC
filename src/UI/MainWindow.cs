// YA-NEC - finestra principale (interfaccia costruita in codice, come YA-MS)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow : Window
    {
        readonly Config cfg;

        // stato
        NecModel model;
        SegmentGeometry geo;
        RunResult run;
        List<SweepPoint> sweep;
        List<MmanaWireInfo> mmWires;
        List<string> geoNotes = new List<string>();
        CancellationTokenSource cts;
        string lastError;

        // controlli
        Structure3DView view3d;
        ContentControl hostBasso;
        string bassoCorrente = "sorgenti";
        TextBox txtEps, txtSig;
        TextBlock lblEps, lblSig;
        GroundLegend groundLegend;
        Button btnStackAvviso;
        PolarPlot polAz, polEl;
        LineChart chSwr, chZ, chGain, chFb, chEff;
        SmithChart smith;
        TextBox txtFreq, txtSpan, txtNpts, txtNec;
        ComboBox cmbBand, cmbCenter;
        NumericUpDown numZ0, numSegL;
        ComboBox cmbGround, cmbMat, cmbTema, cmbPol, cmbScale, cmbEsempi, cmbLingua;
        CheckBox chkEk, chkCur, chkPat;
        TextBlock lblStatus, lblFile;
        ProgressBar prog;
        ContentControl page;
        readonly List<ToggleButton> tabs = new List<ToggleButton>();
        readonly List<Control> pages = new List<Control>();
        int curTab;
        readonly Dictionary<string, TextBlock[]> cards = new Dictionary<string, TextBlock[]>();
        SelectableTextBlock txtCurrents, txtNotes, txtTest;
        bool building;
        // contatori e ultimo messaggio, usati dalle prove automatiche dell'interfaccia
        int nCalcoli, nSweep;
        string ultimoStato = "";
        bool ultimoStatoErrore;

        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        // terreni tipici (valori standard ITU-R P.527 / ARRL Antenna Book): nome, costante dielettrica, conducibilita' S/m
        static readonly Tuple<string, double, double>[] Grounds =
        {
            Tuple.Create("Medio", 13.0, 0.005),
            Tuple.Create("Povero", 13.0, 0.002),
            Tuple.Create("Molto povero / citta'", 3.0, 0.001),
            Tuple.Create("Sabbioso, secco", 10.0, 0.002),
            Tuple.Create("Pascolo", 13.0, 0.006),
            Tuple.Create("Paludoso", 12.0, 0.0075),
            Tuple.Create("Fertile", 14.0, 0.01),
            Tuple.Create("Molto buono", 20.0, 0.0303),
            Tuple.Create("Acqua dolce", 80.0, 0.001),
            Tuple.Create("Acqua di mare", 81.0, 5.0),
            // dalla 0.4.2: tabella dei terreni di 4nec2 (data/Ground.txt, A. Voors; fonte: tabella dei suoli di pe2bz.philpem.me.uk)
            Tuple.Create("Acqua dolce 10 °C (fino a 100 MHz)", 84.0, 0.001),
            Tuple.Create("Acqua dolce 20 °C (fino a 100 MHz)", 80.0, 0.005),
            Tuple.Create("Mare 10 °C (fino a 1 GHz)", 80.0, 4.0),
            Tuple.Create("Mare 20 °C (fino a 1 GHz)", 73.0, 4.0),
            Tuple.Create("Ghiaccio marino", 4.0, 0.001),
            Tuple.Create("Ghiaccio polare", 3.0, 0.0003),
            Tuple.Create("Calotta polare", 1.0, 0.0001),
            Tuple.Create("Terra artica", 3.0, 0.0005),
            Tuple.Create("Citta' industriale", 3.0, 0.0001),
            Tuple.Create("Citta' industriale, attenuazione media", 5.0, 0.001),
            Tuple.Create("Citta' industriale, attenuazione massima", 3.0, 0.0004),
            // 0.5.0 (IC8TEM 15/11/2026 15:32): solai in calcestruzzo; er e sigma si ricalcolano a ogni frequenza (Engine/TerreniEdifici.cs),
            // i valori qui sono solo segnaposto (a 144 MHz e a 7 MHz) e non servono a riconoscere il terreno
            Tuple.Create(TerreniEdifici.Nomi[1], 5.24, 0.0101),
            Tuple.Create(TerreniEdifici.Nomi[2], 20.0, 0.000093),
        };
        const int PrimoSolaio = 21;   // indice in Grounds del primo solaio (i due solai sono gli ultimi)

        static readonly Tuple<string, double>[] Materials =
        {
            Tuple.Create("Senza perdite", 0.0),
            Tuple.Create("Rame", 5.8e7),
            Tuple.Create("Alluminio puro", 3.5e7),
            Tuple.Create("Alluminio 6061-T6", 2.5e7),
            Tuple.Create("Alluminio 6063-T832", 3.08e7),   // dalla 0.4.2: data/Conduc.txt di 4nec2
            Tuple.Create("Ottone", 1.5e7),
            Tuple.Create("Acciaio inox", 1.4e6),
        };

        public MainWindow(Config c)
        {
            cfg = c;
            // 0.4.30: velocita' della luce del motore (Impostazioni > Motore); 0 = 299,792458 come EZNEC (normale), 1 = 299,8 come nec2c
            if (!Engine.Nec2Engine.CvelBloccata) Engine.Nec2Engine.Cvel = c.GetD("cvel_nec2c", 0) > 0.5 ? Engine.Nec2Engine.CVEL_NEC2 : Engine.Nec2Engine.CVEL_FISICA;
            Title = AppPaths.Title;
            // 1.0.5: icona della finestra (bozzetto C approvato da IC8TEM il 16/11/2026 16:02)
            try { Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://YA-NEC/assets/ya-nec-256.png"))); }
            catch (Exception ex) { Registro.Scrivi("ERRORE", "icona della finestra: " + ex.Message); }
            Width = 1320;
            Height = 900;
            MinWidth = 900;
            MinHeight = 560;
            BuildUi();
            // 1.0.2: stato del tasto del mouse, per non ridisegnare le tabelle degli Oggetti a meta' di un clic (RidisegnaTabelle)
            AddHandler(PointerPressedEvent, (s, e) => puntatoreGiu = true, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
            AddHandler(PointerReleasedEvent, (s, e) => puntatoreGiu = false, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
            // 0.5.3 (IC8TEM 15/11/2026 21:24, "devi pensare pure alle correzioni se lo schermo è piccolo, tipo 768p"): la finestra non
            // supera lo schermo (1366 x 768: si apre ingrandita) e sotto gli 820 punti di altezza passa al formato compatto
            Opened += (s, e) => AdattaAlloSchermo();
            SizeChanged += (s, e) => ApplicaCompatto(e.NewSize.Height < 820);
            Opened += (s, e) =>
            {
                CopyExamples();
                Interfaccia.AvviaGuardia();
                Registro.Scrivi("COMPUTER", "disegno dell'interfaccia: " + Prestazioni.Disegno());
                if (ProveAutomatiche) AvviaProveInterfaccia();
                // 0.4.15: aggiornamento automatico dal GitHub ogni 30 minuti (spento durante le prove)
                string agg = Aggiornamento.PulisciDopoAvvio();
                if (!ProveAutomatiche)
                {
                    Aggiornamento.Cambiato += st => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (lblAggiornamento != null) lblAggiornamento.Text = "Stato: " + st;
                        if (st.StartsWith("versione ") && st.Contains("installata")) Status(st);
                    });
                    Aggiornamento.Avvia(cfg);
                    if (agg != null) Status(agg.Substring(0, 1).ToUpperInvariant() + agg.Substring(1) + ".");
                }
            };
            // 0.5.3: alla chiusura, avviso se il modello non e' salvato (la chiusura si ferma, chiede, poi riprende)
            Closing += async (s, e) =>
            {
                if (!chiusuraConfermata && ModelloNonSalvato() && !ProveAutomatiche)
                {
                    e.Cancel = true;
                    if (await ChiediSeNonSalvato("Cosa faccio prima di chiudere YA-NEC?", "chiudi")) { chiusuraConfermata = true; Close(); }
                    return;
                }
                Aggiornamento.Ferma();
            };
        }

        // ------------------------------------------------------------------------------------------------
        // costruzione dell'interfaccia (si ripete al cambio dello schema di colori)
        // ------------------------------------------------------------------------------------------------
        void BuildUi()
        {
            building = true;
            string fr = txtFreq != null ? txtFreq.Text : "";
            string spanTxt = txtSpan != null ? txtSpan.Text : "", np = txtNpts != null ? txtNpts.Text : "41";
            int bandSel = cmbBand != null ? cmbBand.SelectedIndex : 0;
            tabs.Clear(); pages.Clear(); cards.Clear();
            Background = Pal.B(Pal.Back);
            Foreground = Pal.B(Pal.Fore);
            FontSize = 13;   // corpo del testo del bozzetto approvato (12/11/2026)

            Grid root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,*,Auto") };

            // --- barra del titolo ---
            Border top = new Border { Background = Pal.B(Pal.Track), Padding = new Thickness(14, 8) };
            DockPanel tp = new DockPanel();
            StackPanel tl = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            tl.Children.Add(new TextBlock { Text = "YA-NEC", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.BarFore), VerticalAlignment = VerticalAlignment.Center });
            // 0.5.3 (IC8TEM 15/11/2026 22:30, "si puo' inserire la scritta su due righe? YA-NEC sta bene cosi'"): sottotitolo su due righe accanto a YA-NEC
            StackPanel sott = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 0 };
            sott.Children.Add(new TextBlock { Text = "simulatore d'antenne NEC2", FontSize = 12, LineHeight = 15, Foreground = Pal.B(Pal.BarMuted) });
            sott.Children.Add(new TextBlock { Text = AppPaths.Author, FontSize = 12, LineHeight = 15, Foreground = Pal.B(Pal.BarMuted) });
            tl.Children.Add(sott);
            lblFile = new TextBlock { Text = model != null ? Path.GetFileName(model.FileName) : "nessun modello", FontSize = 13, Foreground = Pal.B(Pal.BarFore), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            DockPanel.SetDock(lblFile, Dock.Right);
            Button bAbout = Btn("About", false);
            bAbout.Padding = new Thickness(12, 2);
            bAbout.Margin = new Thickness(10, 0, 0, 0);
            bAbout.Click += async (s, e) => await new AboutWindow().ShowDialog(this);
            DockPanel.SetDock(bAbout, Dock.Right);
            tp.Children.Add(bAbout);
            // 0.5.3 (IC8TEM 15/11/2026 20:20, "la scelta della lingua e della colorazione li metti a sinistra del pulsante about"; 21:03
            // "come ip-trx-ctrl"): Colori = i sei temi di IP-TRX-CTRL (prima era "Schema" nella barra dei comandi), Lingua = le 36 lingue
            // dei programmi di IC8TEM; tutte e due ridisegnano subito la finestra e si salvano in config.ini
            cmbTema = new ComboBox { ItemsSource = Pal.SchemaNames, SelectedIndex = Pal.Schema, Width = 170, MaxDropDownHeight = 560, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            ToolTip.SetTip(cmbTema, "Colori della finestra: i 18 schemi comuni ai programmi di IC8TEM (Chiaro, Notte, Nero, otto pastelli, Scia meteorica, i temi di IP-TRX-CTRL, Blu, Viola, Verde). Si salva in config.ini.");
            cmbTema.SelectionChanged += (s, e) =>
            {
                if (building || cmbTema.SelectedIndex < 0 || cmbTema.SelectedIndex == Pal.Schema) return;
                Registro.Scrivi("AZIONE", "schema dei colori: " + Pal.SchemaNames[cmbTema.SelectedIndex]);
                cfg.SetD("tema", cmbTema.SelectedIndex);
                Pal.Load(cmbTema.SelectedIndex);
                BuildUi();
                RefreshAll();
            };
            string[] nomiLingue = new string[Lang.Languages.Length];
            for (int i = 0; i < nomiLingue.Length; i++) nomiLingue[i] = Lang.Languages[i][1];
            cmbLingua = new ComboBox { ItemsSource = nomiLingue, SelectedIndex = Math.Max(0, Lang.IndexOf(Lang.Code)), Width = 150, Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, MaxDropDownHeight = 520 };
            ToolTip.SetTip(cmbLingua, "Lingua dell'interfaccia (37 lingue). Le scritte non ancora tradotte restano in italiano. Si salva in config.ini.");
            cmbLingua.SelectionChanged += (s, e) =>
            {
                if (building || cmbLingua.SelectedIndex < 0) return;
                string code = Lang.Languages[cmbLingua.SelectedIndex][0];
                if (code == Lang.Code) return;
                Registro.Scrivi("AZIONE", "lingua: " + code);
                cfg.Set("lingua", code);
                Lang.Load(code);
                Lang.ApplyCulture();
                BuildUi();
                RefreshAll();
            };
            StackPanel linCol = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
            linCol.Children.Add(new TextBlock { Text = "Lingua", FontSize = 12, Foreground = Pal.B(Pal.BarMuted), VerticalAlignment = VerticalAlignment.Center });
            linCol.Children.Add(cmbLingua);
            linCol.Children.Add(new TextBlock { Text = "Colori", FontSize = 12, Foreground = Pal.B(Pal.BarMuted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) });
            linCol.Children.Add(cmbTema);
            DockPanel.SetDock(linCol, Dock.Right);
            tp.Children.Add(linCol);
            // cursore della grandezza delle didascalie dei grafici (90-250 %), in alto a destra
            StackPanel leg = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            double legv = Math.Max(90, Math.Min(250, cfg.GetD("didascalie", 100)));
            Draw.LegendScale = legv / 100.0;
            TextBlock legT = new TextBlock { Text = "Didascalie " + legv.ToString("0", CI) + " %", FontSize = 12, Foreground = Pal.B(Pal.BarMuted), VerticalAlignment = VerticalAlignment.Center, Width = 110 };
            Slider legS = new Slider { Minimum = 90, Maximum = 250, Value = legv, Width = 130, VerticalAlignment = VerticalAlignment.Center, SmallChange = 10, LargeChange = 20 };
            ToolTip.SetTip(legS, "Grandezza delle didascalie (legende) di tutti i grafici, dal 90 al 250 %. Si salva in config.ini.");
            legS.PropertyChanged += (s, e) =>
            {
                if (e.Property != Slider.ValueProperty) return;
                double v = Math.Round(legS.Value / 10) * 10;
                Draw.LegendScale = v / 100.0;
                legT.Text = "Didascalie " + v.ToString("0", CI) + " %";
                cfg.SetD("didascalie", v);
                foreach (Control c in new Control[] { chSwr, chZ, chGain, chFb, chEff, polAz, polEl, smith }) if (c != null) c.InvalidateVisual();
            };
            leg.Children.Add(legT);
            leg.Children.Add(legS);
            DockPanel.SetDock(leg, Dock.Right);
            tp.Children.Add(leg);
            tp.Children.Add(lblFile);
            tp.Children.Add(tl);
            top.Child = tp;
            Grid.SetRow(top, 0);
            root.Children.Add(top);

            // --- comandi ---
            Border cmdB = new Border { Background = Pal.B(Pal.Panel2), Padding = new Thickness(8, 4), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
            WrapPanel cmd = new WrapPanel { Orientation = Orientation.Horizontal };
            // 0.5.3 (IC8TEM 15/11/2026 20:26, bozzetto approvato alle 20:27): "Nuovo" azzera il modello, con l'avviso se non e' salvato
            Button bNuovo = Btn("Nuovo", false);
            ToolTip.SetTip(bNuovo, "Comincia un modello vuoto (nessun filo, spazio libero, frequenza della barra). Se il modello aperto ha modifiche non salvate chiede prima se salvarlo.");
            bNuovo.Click += async (s, e) => await NuovoModello();
            cmd.Children.Add(bNuovo);
            Button bOpen = Btn("Apri modello...", false);
            bOpen.Click += async (s, e) => await OpenModelDialog();
            Button bSave = Btn("Salva .nec...", false);
            bSave.Click += async (s, e) =>
            {
                // nessun modello aperto ma una scheda dei Calcolatori visibile: si salva il modello della scheda (con il promemoria)
                if ((model == null || model.Geometry.Count == 0) && salvaCalcolatoreCorrente != null) await salvaCalcolatoreCorrente();
                else await SaveModelDialog();
            };
            cmd.Children.Add(bOpen);
            cmd.Children.Add(bSave);
            cmbEsempi = new ComboBox { Width = 190, Margin = new Thickness(4, 3), VerticalAlignment = VerticalAlignment.Center, PlaceholderText = "Esempi..." };
            cmbEsempi.ItemsSource = ExampleNames();
            cmbEsempi.SelectionChanged += async (s, e) =>
            {
                if (building || cmbEsempi.SelectedItem == null) return;
                string f = Path.Combine(AppPaths.ModelsDir, cmbEsempi.SelectedItem.ToString());
                // 0.5.3: avviso se il modello aperto non e' salvato; con Annulla la tendina torna vuota e il modello resta
                if (!await ChiediSeNonSalvato("Cosa faccio prima di aprire l'esempio?", "apri"))
                {
                    bool old = building; building = true; cmbEsempi.SelectedItem = null; building = old;
                    return;
                }
                LoadModelFile(f);
            };
            cmd.Children.Add(cmbEsempi);
            cmd.Children.Add(Sep());
            // banda: "Libera" lascia la frequenza a scelta dell'utente; una banda imposta il centro scelto e lo span
            List<string> bl = new List<string> { "Libera" };
            foreach (Band bd in Bands.All) bl.Add(bd.Name);
            cmbBand = new ComboBox { ItemsSource = bl, SelectedIndex = Math.Max(0, bandSel), Width = 100, Margin = new Thickness(4, 3), VerticalAlignment = VerticalAlignment.Center };
            cmbBand.SelectionChanged += (s, e) => { if (!building) ApplyBand(); };
            cmbCenter = new ComboBox { ItemsSource = new[] { "operativo", "aritmetico" }, SelectedIndex = (int)cfg.GetD("centro_banda", 0), Width = 120, Margin = new Thickness(4, 3), VerticalAlignment = VerticalAlignment.Center };
            cmbCenter.SelectionChanged += (s, e) =>
            {
                if (building) return;
                cfg.SetD("centro_banda", cmbCenter.SelectedIndex);
                ApplyBand();
            };
            txtFreq = Tb(fr, 80);
            txtFreq.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) Calculate(); };
            Button bCalc = Btn("Calcola", true);
            bCalc.Click += (s, e) => Calculate();
            cmd.Children.Add(bCalc);
            txtSpan = Tb(spanTxt, 70);
            ToolTip.SetTip(txtSpan, "Ampiezza dello sweep in kHz, centrata sulla frequenza di calcolo");
            txtNpts = Tb(np, 44);
            Button bSweep = Btn("Sweep", true);
            bSweep.Click += (s, e) => DoSweep();
            cmd.Children.Add(bSweep);
            Button bSeg = Btn("Segmentazione...", false);
            ToolTip.SetTip(bSeg, "Verifica della segmentazione alla frequenza piu' alta dello sweep, con correzione automatica (bozzetto approvato).");
            bSeg.Click += async (s, e) => await ApriVerificaSegmentazione();
            cmd.Children.Add(bSeg);
            // 0.5.1: controllo dei dati senza calcolare (controllo z3 approvato da IC8TEM il 15/11/2026 11:35)
            Button bCtl = Btn("Controlla...", false);
            ToolTip.SetTip(bCtl, "Controlla il modello senza calcolare: frequenza, fili, terreno, sorgenti, carichi, linee, unita' di misura e variabili (manuale, capitolo 11).");
            bCtl.Click += async (s, e) => await ApriControlloDati();
            cmd.Children.Add(bCtl);
            cmd.Children.Add(Sep());
            numZ0 = Num(1, 10000, model != null ? (decimal)model.Z0 : 50, 1, 1, 70);
            numZ0.ValueChanged += (s, e) => { if (!building && model != null) { model.Z0 = (double)(numZ0.Value ?? 50); Registro.Scrivi("AZIONE", "Z0 = " + model.Z0 + " ohm"); RefreshAfterZ0(); } };
            cmd.Children.Add(Lbl("Terreno"));
            List<string> gl = new List<string> { "Spazio libero", "Perfetto", "Dal modello" };
            foreach (Tuple<string, double, double> g in Grounds) gl.Add(g.Item1 + string.Format(CultureInfo.CurrentCulture, " ({0:0.##}; {1:0.####})", g.Item2, g.Item3));
            gl.Add("Personalizzato");
            cmbGround = new ComboBox { ItemsSource = gl, Width = 200, Margin = new Thickness(4, 3), VerticalAlignment = VerticalAlignment.Center };
            cmbGround.SelectionChanged += (s, e) => { if (!building) ApplyGroundChoice(); };
            cmd.Children.Add(cmbGround);
            CostruisciQuota(cmd);   // 0.5.3: quota dell'antenna sul terreno (bozzetto approvato il 15/11/2026 19:40)
            // terreno personalizzato (approvato il 12/11/2026): caselle er e sigma
            lblEps = Lbl("εr");
            txtEps = Tb(cfg.GetD("terreno_er", 13).ToString("0.###", CultureInfo.CurrentCulture), 50);
            lblSig = Lbl("σ S/m");
            txtSig = Tb(cfg.GetD("terreno_sigma", 0.005).ToString("0.#####", CultureInfo.CurrentCulture), 70);
            ToolTip.SetTip(txtEps, "Costante dielettrica relativa del terreno (1-100). Modificabile solo con 'Personalizzato'; con i terreni tipici e 'Dal modello' mostra i valori usati.");
            ToolTip.SetTip(txtSig, "Conducibilita' del terreno in S/m (0-10; acqua di mare 5). Modificabile solo con 'Personalizzato'.");
            txtEps.LostFocus += (s, e) => { if (!building && cmbGround.SelectedIndex == 3 + Grounds.Length) ApplyGroundChoice(); };
            txtSig.LostFocus += (s, e) => { if (!building && cmbGround.SelectedIndex == 3 + Grounds.Length) ApplyGroundChoice(); };
            lblEps.IsVisible = txtEps.IsVisible = lblSig.IsVisible = txtSig.IsVisible = false;
            cmd.Children.Add(lblEps);
            cmd.Children.Add(txtEps);
            cmd.Children.Add(lblSig);
            cmd.Children.Add(txtSig);
            cmd.Children.Add(Lbl("Fili"));
            List<string> ml = new List<string>();
            foreach (Tuple<string, double> m in Materials) ml.Add(m.Item1);
            ml.Add("Dal modello");
            cmbMat = new ComboBox { ItemsSource = ml, Width = 160, Margin = new Thickness(4, 3), VerticalAlignment = VerticalAlignment.Center };
            cmbMat.SelectionChanged += (s, e) => { if (!building) ApplyMaterialChoice(); };
            cmd.Children.Add(cmbMat);
            chkEk = new CheckBox { Content = "Kernel esteso (EK)", Margin = new Thickness(6, 3), VerticalAlignment = VerticalAlignment.Center };
            chkEk.IsCheckedChanged += (s, e) => { if (!building && model != null) { model.ExtendedKernel = chkEk.IsChecked == true; Registro.Scrivi("AZIONE", "kernel esteso: " + model.ExtendedKernel); Calculate(); } };
            cmd.Children.Add(chkEk);
            cmd.Children.Add(Sep());
            cmd.Children.Add(Lbl("Segmenti per λ (MMANA)"));
            numSegL = Num(10, 200, (decimal)cfg.GetD("segmenti_lambda", 40), 0, 2, 60);
            numSegL.ValueChanged += (s, e) => { if (!building) cfg.SetD("segmenti_lambda", (double)(numSegL.Value ?? 40)); };
            cmd.Children.Add(numSegL);

            // spiegazioni nei fumetti (si aprono tenendo il mouse sopra)
            ToolTip.SetTip(cmbCenter,
                "Centro della banda scelta:\n" +
                "- operativo: la frequenza dove si lavora davvero (per esempio 144,300 MHz in 2 m, 432,200 in 70 cm),\n" +
                "  cioe' la frequenza di progetto delle antenne per DX, EME e meteor scatter;\n" +
                "- aritmetico: la meta' esatta tra i limiti IARU della banda (per esempio 145,000 MHz in 2 m),\n" +
                "  utile per le antenne che devono coprire tutta la banda (FM, ripetitori, satelliti).\n" +
                "Lo span dello sweep viene impostato per restare dentro la banda.");
            ToolTip.SetTip(chkEk,
                "Kernel esteso del NEC2 (scheda EK).\n" +
                "Il NEC2 normale tratta ogni filo come una linea sottile: va bene finche' il segmento e' almeno\n" +
                "8 volte piu' lungo del raggio. Con fili grossi o segmenti corti (tubi spessi in UHF, elementi vicini,\n" +
                "segmenti fitti) il kernel esteso usa una formula piu' esatta e il risultato e' piu' affidabile.\n" +
                "Quando serve: rapporto segmento/raggio fra 2 e 8 circa (sotto 2 nessuno dei due e' affidabile).\n" +
                "Attenzione: non va usato con fili che si incontrano ad angolo o con diametri diversi alla giunzione.\n" +
                "Il kernel esteso viene salvato nel file .nec come scheda EK.");
            ToolTip.SetTip(numZ0, "Impedenza di riferimento per il ROS (50 ohm il cavo, 28 ohm le Yagi DK7ZB, 75 ohm, ...).");
            ToolTip.SetTip(cmbGround, "Terreno sotto l'antenna: spazio libero, terreno perfetto o uno dei terreni tipici (costante dielettrica; conducibilita' S/m).\nCon il terreno l'antenna deve stare sopra la quota 0.");
            ToolTip.SetTip(cmbMat, "Materiale dei fili: aggiunge le perdite ohmiche (rame, alluminio, ottone, acciaio inox).");
            ToolTip.SetTip(numSegL, "Segmenti per lunghezza d'onda usati quando si apre un modello MMANA (che va risegmentato per il NEC2).");

            // ogni etichetta resta sulla stessa riga della sua casella quando la barra va a capo
            List<Control> items = new List<Control>(cmd.Children);
            cmd.Children.Clear();
            StackPanel grp = null;
            foreach (Control it in items)
            {
                if (it is TextBlock)
                {
                    grp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    cmd.Children.Add(grp);
                    grp.Children.Add(it);
                    continue;
                }
                bool sep = it is Border && it.Width == 1;
                if (grp != null && !sep) { grp.Children.Add(it); continue; }
                grp = null;
                cmd.Children.Add(it);
            }
            // 0.5.3: sotto la barra il riquadro arancione della quota (nascosto finche' non serve)
            StackPanel cmdSp = new StackPanel();
            cmdSp.Children.Add(cmd);
            cmdSp.Children.Add(CostruisciAvvisoQuota());
            cmdB.Child = cmdSp;
            Grid.SetRow(cmdB, 1);
            root.Children.Add(cmdB);

            // --- riquadri dei risultati ---
            WrapPanel res = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 4) };
            foreach (string[] k in new[] {
                new[] { "z", "Impedenza (ohm)" }, new[] { "swr", "ROS" }, new[] { "g", "Guadagno massimo" }, new[] { "dir", "Direzione del massimo" },
                new[] { "fb", "F/B" }, new[] { "fr", "F/R" }, new[] { "bw", "Apertura a -3 dB" } })
                res.Children.Add(Card(k[0], k[1]));
            // 0.5.0 (bozzetto approvato da IC8TEM il 15/11/2026 14:44): Z0 al posto dell'AGT, frequenza e sweep in un quadro a scomparsa,
            // AGT tra gli altri dati con efficienza e segmenti; Calcola e Sweep restano anche nella barra (IC8TEM 14:45)
            res.Children.Add(Z0Card());
            res.Children.Add(FreqCard());
            res.Children.Add(MoreCard());
            Grid.SetRow(res, 2);
            root.Children.Add(res);
            resCards = res;
            // schede dell'ottimizzatore nello stesso posto (visibili con la sua linguetta, come nel bozzetto)
            WrapPanel oc = CreaSchedeOttimizzatore();
            Grid.SetRow(oc, 2);
            root.Children.Add(oc);

            // --- linguette ---
            Border tabB = new Border { Background = Pal.B(Pal.Track), Padding = new Thickness(8, 4) };
            WrapPanel tabP = new WrapPanel { Orientation = Orientation.Horizontal };
            // 0.5.3 (IC8TEM 15/11/2026 20:19): ordine voluto "struttura 3d, frequenza, diagrammi, smith, rumore/gt/stack, correnti, nec, ottimizzatore,
            // calcolatori", poi le altre in fondo; "Oggetti inseriti" non e' piu' una linguetta (le tabelle sono in basso a destra nella Struttura 3D)
            string[] names = { "Struttura 3D", "Frequenza", "Diagrammi", "Smith", "Rumore/GT/Stack", "Correnti", "NEC", "Ottimizzatore", "Calcolatori", "Variabili", "Avvisi", "Impostazioni", "Manuale", "Crediti", "Registro", "Autotest" };
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                ToggleButton t = new ToggleButton { Content = names[i], Tag = names[i] };   // 0.5.3: Tag = nome italiano (il testo si traduce)
                t.Classes.Add("tab");
                t.Margin = new Thickness(2, 1);
                t.Foreground = Pal.B(Pal.BarFore);
                t.Background = Pal.B(Pal.Track);
                t.Click += (s, e) => SelectTab(idx);
                tabs.Add(t);
                tabP.Children.Add(t);
            }
            tabB.Child = tabP;
            Grid.SetRow(tabB, 3);
            root.Children.Add(tabB);

            // le pagine si costruiscono nell'ordine di prima (alcune usano quello che le precedenti preparano), poi si mettono nell'ordine delle linguette
            Dictionary<string, Control> pag = new Dictionary<string, Control>();
            pag["Struttura 3D"] = BuildStructurePage();
            BuildOggettiPage();   // 0.5.3: il fascione degli oggetti resta (tabelle, conteggi, prove) ma non e' piu' una linguetta
            pag["Calcolatori"] = BuildCalcolatoriPage();
            pag["Diagrammi"] = BuildPatternPage();
            pag["Rumore/GT/Stack"] = BuildGtSopra();   // 1.1.0: sopralinguetta con tre voci
            // 1.1.0: "Antenne riceventi" e' una famiglia dei Calcolatori (la pagina si costruisce alla prima apertura)
            pag["Frequenza"] = BuildFreqPage();
            pag["Smith"] = BuildSmithPage();
            pag["Correnti"] = BuildCurrentsPage();
            pag["NEC"] = BuildNecPage();
            pag["Variabili"] = BuildVarsPage();
            pag["Ottimizzatore"] = BuildOttimizzatorePage();
            pag["Impostazioni"] = BuildImpostazioniPage();
            pag["Avvisi"] = BuildNotesPage();
            pag["Autotest"] = BuildTestPage();
            pag["Manuale"] = BuildManualPage();
            pag["Crediti"] = BuildCreditsPage();
            pag["Registro"] = BuildRegistroPage();
            foreach (string nm in names) pages.Add(pag[nm]);
            page = new ContentControl { Margin = new Thickness(6) };
            Grid.SetRow(page, 4);
            root.Children.Add(page);

            // --- barra di stato ---
            Border st = new Border { Background = Pal.B(Pal.Track), Padding = new Thickness(10, 4) };
            DockPanel sp = new DockPanel();
            prog = new ProgressBar { Width = 220, Minimum = 0, Maximum = 100, IsVisible = false, Margin = new Thickness(8, 0, 0, 0) };
            DockPanel.SetDock(prog, Dock.Right);
            sp.Children.Add(prog);
            lblStatus = new TextBlock { Text = "Pronto. Apri un modello NEC (.nec) o MMANA (.maa), oppure scegli un esempio.", Foreground = Pal.B(Pal.BarFore), FontSize = 12.5, TextTrimming = TextTrimming.CharacterEllipsis };
            sp.Children.Add(lblStatus);
            st.Child = sp;
            Grid.SetRow(st, 5);
            root.Children.Add(st);

            Content = root;
            SelectTab(curTab);
            SyncToolbar();
            building = false;
        }

        void SelectTab(int i)
        {
            if (i != curTab && i >= 0 && i < tabs.Count) Registro.Scrivi("AZIONE", "linguetta: " + tabs[i].Content);
            curTab = i;
            for (int k = 0; k < tabs.Count; k++)
            {
                bool on = k == i;
                tabs[k].IsChecked = on;
                tabs[k].Background = Pal.B(on ? Pal.Accent : Pal.Track);
                tabs[k].Foreground = Pal.B(on ? Pal.AccentFore : Pal.BarFore);
                tabs[k].FontWeight = on ? FontWeight.SemiBold : FontWeight.Normal;
            }
            if (page != null && i >= 0 && i < pages.Count) page.Content = pages[i];
            bool ottAperto = i >= 0 && i < tabs.Count && (tabs[i].Tag as string) == "Ottimizzatore";
            if (resCards != null) resCards.IsVisible = !ottAperto;
            if (ottCards != null) ottCards.IsVisible = ottAperto;
            AggiornaLinguetta();
        }

        // le linguette nuove si aggiornano quando si aprono (e dopo ogni calcolo, se sono aperte)
        void AggiornaLinguetta()
        {
            if (curTab < 0 || curTab >= tabs.Count) return;
            if (run != null && cts != null) AffinaDiagrammaSeServe(run, cts.Token);   // 0.4.20: lobo a 2 gradi quando si apre la linguetta
            string n = tabs[curTab].Tag as string;
            try
            {
                if (n == "Impostazioni") SincronizzaImpostazioni();
                else if (n == "Rumore/GT/Stack") { if (fasGt == null || fasGt.Current == "gt") CalcolaGt(); }
                else if (n == "Registro") AggiornaRegistro();
                else if (n == "Ottimizzatore") OttSincronizza();
                else if (n == "Calcolatori" && lpdaPrimoCalcolo != null && fasCalc != null && fasCalc.Current == "lpda")
                {
                    Action a = lpdaPrimoCalcolo;
                    lpdaPrimoCalcolo = null;
                    a();
                }
            }
            catch (Exception ex) { Status("Errore nella linguetta " + n + ": " + ex.Message, true); }
        }

        // ------------------------------------------------------------------------------------------------
        // pagine
        // ------------------------------------------------------------------------------------------------
        Control BuildStructurePage()
        {
            DockPanel d = new DockPanel();
            WrapPanel bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            // correnti: casella si'/no e, se si', scelta a pallino tra colori e sola curva (richiesta IC8TEM 13/11/2026 17:56)
            chkCur = new CheckBox { Content = "Correnti", IsChecked = true, Margin = new Thickness(4, 0) };
            RadioButton rbColori = new RadioButton { Content = "a colori", GroupName = "corr3d", IsChecked = true, Margin = new Thickness(2, 0) };
            RadioButton rbCurva = new RadioButton { Content = "solo curva", GroupName = "corr3d", IsChecked = false, Margin = new Thickness(2, 0, 8, 0) };
            chkCur.IsCheckedChanged += (s, e) =>
            {
                bool on = chkCur.IsChecked == true;
                view3d.ShowCurrents = on; rbColori.IsEnabled = on; rbCurva.IsEnabled = on;
                view3d.InvalidateVisual();
            };
            rbColori.IsCheckedChanged += (s, e) =>
            {
                bool col = rbColori.IsChecked == true;
                view3d.CurrentColors = col; view3d.CurrentCurves = !col; view3d.InvalidateVisual();
            };
            chkPat = new CheckBox { Content = "Lobo 3D", IsChecked = true, Margin = new Thickness(4, 0) };
            chkPat.IsCheckedChanged += (s, e) => { view3d.ShowPattern = chkPat.IsChecked == true; view3d.InvalidateVisual(); };
            CheckBox chkQuota = new CheckBox { Content = "Terreno e quota", IsChecked = true, Margin = new Thickness(4, 0) };
            chkQuota.IsCheckedChanged += (s, e) =>
            {
                view3d.ShowGroundAndHeight = chkQuota.IsChecked == true;
                if (groundLegend != null) groundLegend.IsVisible = chkQuota.IsChecked == true;
                view3d.InvalidateVisual();
            };
            CheckBox chkFasi = new CheckBox { Content = "Fasi delle sorgenti", IsChecked = true, Margin = new Thickness(4, 0) };
            chkFasi.IsCheckedChanged += (s, e) => { view3d.ShowPhases = chkFasi.IsChecked == true; view3d.InvalidateVisual(); };
            ToolTip.SetTip(chkFasi, "Con piu' sorgenti (schiere, four-square): ampiezza e fase scritte su ogni elemento e freccia del lobo.");
            ToolTip.SetTip(chkCur, "Mostra o nasconde la corrente sui fili.");
            ToolTip.SetTip(rbColori, "Fili colorati secondo la corrente (blu bassa, rosso alta), con la scala in basso.");
            ToolTip.SetTip(rbCurva, "Fili nel colore normale con sopra la distribuzione della corrente come curva (zero agli estremi liberi).");
            ToolTip.SetTip(chkQuota, "Terreno disegnato secondo il tipo scelto nel menu Terreno e quote dell'antenna dal suolo (0 m).");
            bar.Children.Add(chkCur);
            bar.Children.Add(rbColori);
            bar.Children.Add(rbCurva);
            bar.Children.Add(chkPat);
            bar.Children.Add(chkQuota);
            bar.Children.Add(chkFasi);
            btnStackAvviso = Btn("Stack riconosciuto: distanze...", true);
            btnStackAvviso.IsVisible = false;
            btnStackAvviso.Click += (s, e) => ApriStack();
            ToolTip.SetTip(btnStackAvviso, "Nel modello ci sono copie identiche della stessa antenna, solo spostate: apre la linguetta delle distanze dello stack (DL6WU con le aperture dell'antenna base).");
            bar.Children.Add(Lbl("Vista:"));
            foreach (string[] v in new[] { new[] { "3D", "-55", "22" }, new[] { "dall'alto", "-90", "90" }, new[] { "di lato", "-90", "0" }, new[] { "di fronte", "0", "0" } })
            {
                double a = double.Parse(v[1], CI), b = double.Parse(v[2], CI);
                Button bt = Btn(v[0], false);
                bt.Click += (s, e) => view3d.SetView(a, b);
                bar.Children.Add(bt);
            }
            bar.Children.Add(btnStackAvviso);
            DockPanel.SetDock(bar, Dock.Top);
            d.Children.Add(bar);
            // barra Geometria (bozzetto approvato, 0.4.2): sopra la barra della vista
            Control barGeo = CreaBarraGeometria();
            DockPanel.SetDock(barGeo, Dock.Top);
            d.Children.Insert(0, barGeo);
            view3d = new Structure3DView();
            view3d.WireClicked += (tag, ctrl) => GeoFiloCliccato(tag, ctrl);
            // bozzetto approvato "Struttura divisa": a sinistra il modello disegnato, a destra le tabelle in stile Excel
            // (Fili sopra, Sorgenti sotto); le due righe di separazione si trascinano
            Grid split3d = new Grid { ColumnDefinitions = new ColumnDefinitions("46*,10,54*") };
            groundLegend = new GroundLegend();
            DockPanel dock3d = new DockPanel();
            DockPanel.SetDock(groundLegend, Dock.Bottom);
            dock3d.Children.Add(groundLegend);
            dock3d.Children.Add(view3d);
            Control pane3d = Riquadro(dock3d);
            Grid.SetColumn(pane3d, 0);
            split3d.Children.Add(pane3d);
            GridSplitter vs = new GridSplitter { Background = Brushes.Transparent, ResizeDirection = GridResizeDirection.Columns };
            Grid.SetColumn(vs, 1);
            split3d.Children.Add(vs);
            Border vgrip = new Border { Width = 3, Height = 60, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.Parse("#B9C4D4")), IsHitTestVisible = false, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(vgrip, 1);
            split3d.Children.Add(vgrip);
            Grid right = new Grid { RowDefinitions = new RowDefinitions("19*,10,10*") };
            Control tblFili = BuildWiresTable();
            right.Children.Add(tblFili);
            GridSplitter hs = new GridSplitter { Background = Brushes.Transparent, ResizeDirection = GridResizeDirection.Rows };
            Grid.SetRow(hs, 1);
            right.Children.Add(hs);
            Border hgrip = new Border { Width = 60, Height = 3, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.Parse("#B9C4D4")), IsHitTestVisible = false, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetRow(hgrip, 1);
            right.Children.Add(hgrip);
            hostBasso = new ContentControl();
            ScrollViewer bassoSv = new ScrollViewer { Content = hostBasso, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
            bassoSv.SizeChanged += (s, e) =>
            {
                if (Math.Abs(e.NewSize.Width - bassoLarghezza) < 20) return;
                bassoLarghezza = e.NewSize.Width;
                LimitaNote(hostBasso.Content as Control, bassoLarghezza);
            };
            Grid.SetRow(bassoSv, 2);
            right.Children.Add(bassoSv);
            struttTabelle = right;
            struttDestraHost = new ContentControl { Content = right };
            geoScheda = null;
            Grid.SetColumn(struttDestraHost, 2);
            split3d.Children.Add(struttDestraHost);
            ToolTip.SetTip(vs, "La riga verticale (e quella tra Fili e Sorgenti) si trascina: modello e tabelle si allargano a scelta.");
            bassoCorrente = "sorgenti";
            d.Children.Add(split3d);
            return d;
        }

        Control BuildPatternPage()
        {
            DockPanel d = new DockPanel();
            WrapPanel bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            bar.Children.Add(Lbl("Tracce"));
            cmbPol = new ComboBox { ItemsSource = new[] { "Totale", "Totale + verticale + orizzontale" }, SelectedIndex = 0, Width = 240, Margin = new Thickness(4, 0) };
            cmbPol.SelectionChanged += (s, e) => { if (!building) ShowPatterns(); };
            bar.Children.Add(cmbPol);
            bar.Children.Add(Lbl("Scala"));
            cmbScale = new ComboBox { ItemsSource = new[] { "Logaritmica ARRL", "Lineare in dB" }, SelectedIndex = 0, Width = 170, Margin = new Thickness(4, 0) };
            cmbScale.SelectionChanged += (s, e) => { if (!building) ShowPatterns(); };
            bar.Children.Add(cmbScale);
            // 0.5.3 (IC8TEM 15/11/2026 20:12-20:13, "scala dei caratteri a scelta e possibilita' di girare il lobo azimut di 90 gradi",
            // sul bozzetto: "no slide 100-200% sui caratteri e tickbox per 90 gradi"): cursore della grandezza delle scritte dei due
            // diagrammi e casella che ruota l'azimut di 90 gradi (0 gradi in alto); tutti e due salvati in config.ini
            double carV = Math.Max(100, Math.Min(200, cfg.GetD("diagrammi_caratteri", 100)));
            TextBlock carT = new TextBlock { Text = "Caratteri " + carV.ToString("0", CI) + " %", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(18, 0, 4, 0), Width = 104 };
            Slider carS = new Slider { Minimum = 100, Maximum = 200, Value = carV, Width = 160, VerticalAlignment = VerticalAlignment.Center, SmallChange = 5, LargeChange = 25 };
            ToolTip.SetTip(carS, "Grandezza di tutte le scritte dei due diagrammi (titolo, gradi, anelli in dB, valori sotto, apertura a -3 dB), dal 100 al 200 %. Si salva in config.ini.");
            CheckBox cRuota = new CheckBox { Content = "Azimut ruotato di 90° (0° in alto)", IsChecked = cfg.Get("diagrammi_azimut_90", "0") == "1", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(18, 0, 0, 0) };
            ToolTip.SetTip(cRuota, "Accesa: il diagramma azimutale e' ruotato di 90 gradi, 0 gradi in alto e il lobo che punta in su; i gradi restano quelli del NEC (antiorari: 90 gradi a sinistra). Spenta: 0 gradi a destra come nel NEC. Si salva in config.ini.");
            bar.Children.Add(carT); bar.Children.Add(carS); bar.Children.Add(cRuota);
            DockPanel.SetDock(bar, Dock.Top);
            d.Children.Add(bar);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
            polAz = new PolarPlot { Title = "Azimut", TextScale = carV / 100.0, Rotate = cRuota.IsChecked == true ? 90 : 0 };
            polEl = new PolarPlot { Title = "Elevazione", Elevation = true, TextScale = carV / 100.0 };
            carS.PropertyChanged += (s, e) =>
            {
                if (e.Property != Slider.ValueProperty) return;
                double v = Math.Round(carS.Value / 5) * 5;
                carT.Text = "Caratteri " + v.ToString("0", CI) + " %";
                cfg.SetD("diagrammi_caratteri", v);
                polAz.TextScale = polEl.TextScale = v / 100.0;
                polAz.InvalidateVisual(); polEl.InvalidateVisual();
            };
            cRuota.IsCheckedChanged += (s, e) =>
            {
                cfg.Set("diagrammi_azimut_90", cRuota.IsChecked == true ? "1" : "0");
                polAz.Rotate = cRuota.IsChecked == true ? 90 : 0;
                Registro.Scrivi("AZIONE", "diagrammi: azimut " + (cRuota.IsChecked == true ? "ruotato di 90 gradi" : "0 gradi a destra"));
                polAz.InvalidateVisual();
            };
            Control a1 = Riquadro(polAz), a2 = Riquadro(polEl);
            Grid.SetColumn(a2, 1);
            a1.Margin = new Thickness(0, 0, 3, 0);
            a2.Margin = new Thickness(3, 0, 0, 0);
            g.Children.Add(a1);
            g.Children.Add(a2);
            d.Children.Add(g);
            return d;
        }

        Control BuildFreqPage()
        {
            // 0.4.40 (bozzetto di IC8TEM del 15/11/2026 14:32-14:33, "correggi secondo le modifiche al bozzetto"): griglia 2 x 2,
            // ROS | guadagno sopra, impedenza | F/B e F/R sotto; l'efficienza (0.4.2) resta su tutta la larghezza in fondo.
            Grid g = new Grid { RowDefinitions = new RowDefinitions("*,*,0.7*"), ColumnDefinitions = new ColumnDefinitions("*,*") };
            chSwr = new LineChart { Title = "ROS", YLabel = "ROS" };
            chGain = new LineChart { Title = "Guadagno", YLabel = "dBi" };
            chZ = new LineChart { Title = "Impedenza", YLabel = "ohm" };
            chFb = new LineChart { Title = "Rapporti F/B e F/R", YLabel = "dB" };
            // efficienza di radiazione in funzione della frequenza (0.4.2, idea di sa6mwa, approvata da IC8TEM il 13/11/2026)
            chEff = new LineChart { Title = "Efficienza di radiazione (potenza irradiata / potenza in ingresso)", YLabel = "%" };
            Control[] cc = { Riquadro(chSwr), Riquadro(chGain), Riquadro(chZ), Riquadro(chFb), Riquadro(chEff) };
            for (int i = 0; i < cc.Length; i++)
            {
                int r = i / 2, c = i % 2;
                Grid.SetRow(cc[i], r);
                Grid.SetColumn(cc[i], c);
                if (i == 4) Grid.SetColumnSpan(cc[i], 2);
                cc[i].Margin = new Thickness(c == 0 ? 0 : 3, r == 0 ? 0 : 3, c == 0 && i != 4 ? 3 : 0, r == 2 ? 0 : 3);
                g.Children.Add(cc[i]);
            }
            return g;
        }

        Control BuildSmithPage()
        {
            smith = new SmithChart();
            return Riquadro(smith);
        }

        Control BuildCurrentsPage()
        {
            txtCurrents = new SelectableTextBlock { FontFamily = Pal.Mono, FontSize = 12.5, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(10) };
            return Riquadro(new ScrollViewer { Content = txtCurrents, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto });
        }

        Control BuildNecPage()
        {
            DockPanel d = new DockPanel();
            WrapPanel bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            Button bApply = Btn("Applica e ricalcola", true);
            bApply.Click += (s, e) => ApplyNecText();
            Button bReset = Btn("Ripristina dal modello", false);
            bReset.Click += (s, e) => { if (model != null) txtNec.Text = model.ToNec(); };
            bar.Children.Add(bApply);
            bar.Children.Add(bReset);
            bar.Children.Add(new TextBlock { Text = "Schede NEC2 del modello: si possono modificare qui (GW, GA, GH, GM, GR, GS, GX, GC, GE, EX, LD, GN, FR, EK, KH).", Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0) });
            DockPanel.SetDock(bar, Dock.Top);
            d.Children.Add(bar);
            txtNec = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap, FontFamily = Pal.Mono, FontSize = 13, Text = model != null ? model.ToNec() : "" };
            ScrollViewer.SetHorizontalScrollBarVisibility(txtNec, ScrollBarVisibility.Auto);
            ScrollViewer.SetVerticalScrollBarVisibility(txtNec, ScrollBarVisibility.Auto);
            d.Children.Add(txtNec);
            return d;
        }

        Control BuildNotesPage()
        {
            txtNotes = new SelectableTextBlock { FontSize = 13, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(12), TextWrapping = TextWrapping.Wrap };
            return Riquadro(new ScrollViewer { Content = txtNotes });
        }

        Control BuildTestPage()
        {
            DockPanel d = new DockPanel();
            WrapPanel bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            Button b = Btn("Esegui l'autotest del motore", true);
            b.Click += (s, e) => RunSelfTest();
            bar.Children.Add(b);
            bar.Children.Add(new TextBlock { Text = "Confronta il motore C# con i valori calcolati da nec2c sugli stessi modelli (impedenza, guadagno, efficienza, importazione MMANA).", Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0), TextWrapping = TextWrapping.Wrap });
            DockPanel.SetDock(bar, Dock.Top);
            d.Children.Add(bar);
            txtTest = new SelectableTextBlock { FontFamily = Pal.Mono, FontSize = 12.5, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(10), TextWrapping = TextWrapping.Wrap };
            d.Children.Add(Riquadro(new ScrollViewer { Content = txtTest }));
            return d;
        }

        // linguetta Crediti: i colleghi e i progetti da cui deriva YA-NEC, poi le note sulla versione
        Control BuildCreditsPage()
        {
            StackPanel p = new StackPanel { Margin = new Thickness(14), Spacing = 6 };
            p.Children.Add(new TextBlock { Text = "Crediti e ringraziamenti", FontSize = 16, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore) });
            p.Children.Add(new TextBlock { Text = Credits.Intro, FontSize = 13, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
            WrapPanel cards = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (CreditEntry c in Credits.Entries)
            {
                StackPanel cp = new StackPanel { Spacing = 2 };
                cp.Children.Add(new TextBlock { Text = c.Title, FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap });
                cp.Children.Add(new TextBlock { Text = c.Who, FontSize = 12.5, Foreground = Pal.B(Pal.Accent), TextWrapping = TextWrapping.Wrap });
                cp.Children.Add(new TextBlock { Text = c.What, FontSize = 12.5, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
                string foot = (string.IsNullOrEmpty(c.Link) ? "" : c.Link + "   ") + "licenza: " + c.License;
                cp.Children.Add(new SelectableTextBlock { Text = foot, FontSize = 11, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) });
                cards.Children.Add(new Border
                {
                    Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8), Margin = new Thickness(3), Width = 400, Child = cp
                });
            }
            p.Children.Add(cards);
            SelectableTextBlock t = new SelectableTextBlock { FontSize = 12.5, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 10, 0, 0) };
            t.Text =
                "NOTE SU QUESTA VERSIONE (fase 0)\n" +
                "  Il motore e' verificato con l'autotest sugli stessi modelli calcolati con nec2c. Corretto un errore di nec2c\n" +
                "  nell'impedenza interna dei fili (routine zint, carichi LD 5 sotto x = 110), riportando le formule del NEC2 originale.\n" +
                "  Calcola: fili dritti, archi ed eliche (GW, GA, GH), copie e simmetrie (GM, GR, GX), scala (GS), fili rastremati (GC);\n" +
                "  kernel normale ed esteso (EK); spazio libero, terreno perfetto, terreno reale col coefficiente di riflessione;\n" +
                "  carichi LD 0-5 e materiale dei fili; sorgenti di tensione (EX 0); modelli MMANA con risegmentazione NEC.\n" +
                "  " + MmanaImport.ImportNote + "\n" +
                "  EZNEC e' un marchio registrato di Roy W. Lewallen.\n" +
                "  Impostazioni ed esempi: " + AppPaths.DataDir;
            p.Children.Add(t);
            return Riquadro(new ScrollViewer { Content = p });
        }

        // ------------------------------------------------------------------------------------------------
        // piccoli costruttori di controlli
        // ------------------------------------------------------------------------------------------------
        Border Riquadro(Control c)
        {
            return new Border { Background = Pal.B(Pal.ChartBg), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ClipToBounds = true, Child = c };
        }

        Button Btn(string text, bool accent)
        {
            Button b = new Button { Content = text, Margin = new Thickness(3, 3), VerticalAlignment = VerticalAlignment.Center };
            b.Classes.Add("pill");
            if (accent)
            {
                b.Background = Pal.B(Pal.Accent);
                b.Foreground = Pal.B(Pal.AccentFore);
                b.FontWeight = FontWeight.SemiBold;
            }
            else
            {
                b.Background = Pal.B(Pal.Pill);
                b.Foreground = Pal.B(Pal.PillFore);
                b.BorderBrush = Pal.B(Pal.PillBorder);
                b.BorderThickness = new Thickness(1);
            }
            return b;
        }

        TextBlock Lbl(string s)
        {
            return new TextBlock { Text = s, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 3, 0) };
        }

        Control Sep()
        {
            return new Border { Width = 1, Height = 24, Background = Pal.B(Pal.Border), Margin = new Thickness(8, 0), VerticalAlignment = VerticalAlignment.Center };
        }

        TextBox Tb(string val, double w)
        {
            return new TextBox { Text = val, Width = w, Margin = new Thickness(3, 3), VerticalAlignment = VerticalAlignment.Center };
        }

        NumericUpDown Num(decimal min, decimal max, decimal val, int dec, decimal inc, double w)
        {
            NumericUpDown n = new NumericUpDown
            {
                Minimum = min, Maximum = max, Increment = inc,
                FormatString = "F" + dec.ToString(CI),
                ClipValueToMinMax = true,
                Width = w + 70,
                Margin = new Thickness(3, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            n.Value = Math.Max(min, Math.Min(max, val));
            return n;
        }

        bool compatto;

        void AdattaAlloSchermo()
        {
            try
            {
                Avalonia.Platform.Screen sc = Screens.ScreenFromWindow(this) ?? Screens.Primary;
                if (sc == null) return;
                double k = sc.Scaling > 0 ? sc.Scaling : 1;
                double w = sc.WorkingArea.Width / k, h = sc.WorkingArea.Height / k;
                Registro.Scrivi("COMPUTER", string.Format(CI, "schermo utile {0:0} x {1:0} punti (scala {2:0.##})", w, h, k));
                if (h < 860 || w < 1340) { WindowState = WindowState.Maximized; return; }
                if (Height > h - 20) Height = h - 20;
                if (Width > w - 20) Width = w - 20;
            }
            catch { }
        }

        // formato compatto per gli schermi bassi: riquadri dei risultati senza la riga sotto e con il valore piu' piccolo
        void ApplicaCompatto(bool si)
        {
            if (si == compatto) return;
            compatto = si;
            foreach (TextBlock[] t in cards.Values)
            {
                if (t == null || t.Length < 3) continue;
                t[1].FontSize = si ? 15 : 18;
                t[2].IsVisible = !si;
            }
            Registro.Scrivi("AZIONE", "formato " + (si ? "compatto (finestra bassa)" : "normale"));
        }

        Border Card(string key, string label)
        {
            TextBlock l = new TextBlock { Text = label, FontSize = 11, Foreground = Pal.B(Pal.Muted) };
            TextBlock v = new TextBlock { Text = "-", FontSize = compatto ? 15 : 18, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore) };
            TextBlock s = new TextBlock { Text = " ", FontSize = 11, Foreground = Pal.B(Pal.Muted), IsVisible = !compatto };
            cards[key] = new[] { l, v, s };
            StackPanel p = new StackPanel { Spacing = 1 };
            p.Children.Add(l); p.Children.Add(v); p.Children.Add(s);
            return new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(9, 4), Margin = new Thickness(2), MinWidth = 100, Child = p
            };
        }

        // 0.5.0: riquadro della Z0 per il ROS (casella con le frecce), al posto dell'AGT
        Control Z0Card()
        {
            StackPanel p = new StackPanel { Spacing = 1 };
            p.Children.Add(new TextBlock { Text = "Z0 (ohm) per il ROS", FontSize = 11, Foreground = Pal.B(Pal.Muted) });
            numZ0.Width = 130;
            numZ0.Margin = new Thickness(0, 2, 0, 0);
            p.Children.Add(numZ0);
            return new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(9, 4), Margin = new Thickness(2), MinWidth = 100, Child = p
            };
        }

        // 0.5.0: casella di testo con le frecce su e giu' (passo calcolato dal valore)
        Control ConFrecce(TextBox tb, Func<double, double> passo, double min, double max, int dec, Action dopo)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            tb.Margin = new Thickness(0);
            sp.Children.Add(tb);
            foreach (int verso in new[] { 1, -1 })
            {
                int v0 = verso;
                Button b = new Button { Content = verso > 0 ? "\u25b2" : "\u25bc", Padding = new Thickness(7, 2), Margin = new Thickness(2, 0, 0, 0), VerticalAlignment = VerticalAlignment.Stretch };
                b.Click += (s, e) =>
                {
                    double x = ParseNum(tb.Text, min);
                    x = Math.Max(min, Math.Min(max, x + v0 * passo(x)));
                    tb.Text = Fmt(x, dec);
                    if (dopo != null) dopo();
                };
                sp.Children.Add(b);
            }
            return sp;
        }

        // 0.5.0: quadro a scomparsa "Frequenza e sweep": banda, centro, frequenza, span e punti con le frecce, Calcola e Sweep
        Control FreqCard()
        {
            TextBlock sum = new TextBlock { FontSize = 11, Foreground = Pal.B(Pal.Muted) };
            Action riassunto = () => sum.Text = (txtFreq.Text ?? "") + " MHz \u00b7 " + (txtSpan.Text ?? "") + " kHz \u00b7 " + (txtNpts.Text ?? "") + " punti";
            foreach (TextBox t in new[] { txtFreq, txtSpan, txtNpts }) t.TextChanged += (s, e) => riassunto();
            riassunto();
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto"), Margin = new Thickness(6) };
            Action<int, string, Control> riga = (r, t, c) =>
            {
                TextBlock l = new TextBlock { Text = t, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 12, 4) };
                Grid.SetRow(l, r); Grid.SetColumn(l, 0); g.Children.Add(l);
                c.Margin = new Thickness(0, 3); Grid.SetRow(c, r); Grid.SetColumn(c, 1); g.Children.Add(c);
            };
            TextBlock tit = new TextBlock { Text = "Frequenza e sweep", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 4) };
            Grid.SetRow(tit, 0); Grid.SetColumnSpan(tit, 2); g.Children.Add(tit);
            cmbBand.Width = 170; cmbCenter.Width = 170; txtFreq.Width = 110; txtSpan.Width = 110; txtNpts.Width = 110;
            riga(1, "Banda", cmbBand);
            riga(2, "Centro", cmbCenter);
            // passi: frequenza 1/1000 del valore arrotondato (14,175 -> 0,01; 144,3 -> 0,1), span 1/10, punti 2 (restano dispari)
            riga(3, "Frequenza MHz", ConFrecce(txtFreq, x => Math.Pow(10, Math.Floor(Math.Log10(Math.Max(1e-6, x))) - 3), 0.001, 1e6, 4, null));
            riga(4, "Span kHz", ConFrecce(txtSpan, x => Math.Pow(10, Math.Floor(Math.Log10(Math.Max(1, x))) - 1), 0, 1e7, 1, null));
            riga(5, "Punti", ConFrecce(txtNpts, x => 2, 3, 1001, 0, null));
            StackPanel bt = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            Button bC = Btn("Calcola", true); bC.Click += (s, e) => Calculate();
            Button bS = Btn("Sweep", true); bS.Click += (s, e) => DoSweep();
            bt.Children.Add(bC); bt.Children.Add(bS);
            Grid.SetRow(bt, 6); Grid.SetColumnSpan(bt, 2); g.Children.Add(bt);
            StackPanel face = new StackPanel { Spacing = 1 };
            face.Children.Add(new TextBlock { Text = "Frequenza e sweep", FontSize = 11, Foreground = Pal.B(Pal.Muted) });
            face.Children.Add(new TextBlock { Text = "\u25be", FontSize = 18, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Accent) });
            face.Children.Add(sum);
            Button b = new Button
            {
                Content = face,
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(9, 4), Margin = new Thickness(2), MinWidth = 100,
                VerticalAlignment = VerticalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left
            };
            Flyout fl = new Flyout { Content = g, Placement = PlacementMode.BottomEdgeAlignedLeft };
            b.Flyout = fl;
            fl.Opened += (s, e) => b.BorderBrush = Pal.B(Pal.Accent);
            fl.Closed += (s, e) => b.BorderBrush = Pal.B(Pal.Border);
            ToolTip.SetTip(b, "Banda, centro, frequenza di calcolo, span e punti dello sweep (caselle e frecce)");
            return b;
        }

        // riquadro "Altri dati": tendina a scomparsa con Efficienza e Segmenti (richiesta del 12/11/2026)
        Control MoreCard()
        {
            StackPanel inner = new StackPanel { Spacing = 2, Margin = new Thickness(4) };
            inner.Children.Add(Card("agt", "AGT (test di convergenza)"));
            inner.Children.Add(Card("eff", "Efficienza"));
            inner.Children.Add(Card("seg", "Segmenti"));
            StackPanel face = new StackPanel { Spacing = 1 };
            face.Children.Add(new TextBlock { Text = "Altri dati", FontSize = 11, Foreground = Pal.B(Pal.Muted) });
            face.Children.Add(new TextBlock { Text = "\u25be", FontSize = 18, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Accent) });
            face.Children.Add(new TextBlock { Text = "AGT, efficienza, segmenti", FontSize = 11, Foreground = Pal.B(Pal.Muted) });
            Button b = new Button
            {
                Content = face,
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(9, 4), Margin = new Thickness(2), MinWidth = 100,
                VerticalAlignment = VerticalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left
            };
            Flyout fl = new Flyout { Content = inner, Placement = PlacementMode.BottomEdgeAlignedRight };
            b.Flyout = fl;
            // bordo blu mentre la tendina e' aperta, come nel bozzetto
            fl.Opened += (s, e) => b.BorderBrush = Pal.B(Pal.Accent);
            fl.Closed += (s, e) => b.BorderBrush = Pal.B(Pal.Border);
            ToolTip.SetTip(b, "Mostra o nasconde AGT, efficienza e numero di segmenti");
            return b;
        }

        void SetCard(string key, string value, string sub, Color? color = null)
        {
            TextBlock[] t;
            if (!cards.TryGetValue(key, out t)) return;
            t[1].Text = value;
            t[2].Text = string.IsNullOrEmpty(sub) ? " " : sub;
            t[1].Foreground = Pal.B(color ?? Pal.Fore);
        }

        void Status(string s, bool error = false)
        {
            Registro.Scrivi(error ? "ERRORE" : "STATO", s);
            ultimoStato = s;
            ultimoStatoErrore = error;
            if (lblStatus == null) return;
            lblStatus.Text = s;
            lblStatus.Foreground = Pal.B(error ? Pal.Rgb(255, 140, 120) : Pal.BarFore);
        }
    }
}
