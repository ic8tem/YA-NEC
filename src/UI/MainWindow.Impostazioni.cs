// YA-NEC - linguetta "Impostazioni" (bozzetto approvato): opzioni di calcolo a caselle, in tre colonne.
// Le voci della barra in alto (Z0, Terreno, Fili, Kernel, Segmenti per lambda) sono le stesse di questa linguetta:
// cambiarle qui le cambia anche li' (e viceversa). Le caselle delle funzioni non ancora nel motore sono in grigio,
// con il fumetto che dice in quale versione arrivano.
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        CheckBox impEk, impCurr, impLoads;
        NumericUpDown impZ0, impSegL;
        ComboBox impGround, impMat;
        bool impSync;

        Control RigaImp(string etichetta, Control destra, string nota = null, string fumetto = null)
        {
            DockPanel d = new DockPanel { Margin = new Thickness(0, 2) };
            if (destra != null)
            {
                StackPanel r = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Right };
                if (!string.IsNullOrEmpty(nota)) r.Children.Add(new TextBlock { Text = nota, FontSize = 11.5, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 170 });
                r.Children.Add(destra);
                DockPanel.SetDock(r, Dock.Right);
                d.Children.Add(r);
            }
            TextBlock l = new TextBlock { Text = etichetta, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore) };
            d.Children.Add(l);
            if (!string.IsNullOrEmpty(fumetto)) ToolTip.SetTip(d, fumetto);
            return d;
        }

        TextBlock lblAggiornamento;

        CheckBox Casella(string testo, bool val, bool attiva, string fumetto)
        {
            CheckBox c = new CheckBox { Content = testo, IsChecked = val, IsEnabled = attiva, Margin = new Thickness(0, 1) };
            // 1.4.2 (IC8TEM 18/11/2026 18:55: "mica ho capito perche' alcune di queste opzioni sono grigie"): il fumetto dice quale dei tre casi e'
            ToolTip.SetTip(c, (attiva ? "" : val ? "Sempre attiva: non si spegne. " : "Non ancora disponibile: arriva in una prossima versione. ") + fumetto);
            return c;
        }

        Control SceltaFutura(string[] voci, int sel, string fumetto)
        {
            StackPanel sp = Scelta(voci, sel, k => { });
            for (int i = 0; i < sp.Children.Count; i++) if (i != sel) sp.Children[i].IsEnabled = false;
            ToolTip.SetTip(sp, "Scelta fissa: oggi c'e' solo la voce evidenziata. " + fumetto);
            return sp;
        }

        Control BuildImpostazioniPage()
        {
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*,10,*") };
            StackPanel c1 = new StackPanel(), c2 = new StackPanel(), c3 = new StackPanel();

            // ---- Motore ----
            StackPanel mot = new StackPanel();
            mot.Children.Add(RigaImp("Calcolo", SceltaFutura(new[] { "NEC-2 (nec2c)", "NEC-2 doppia precisione" }, 1, "Il motore e' il NEC-2 di nec2c tradotto in C# in doppia precisione (verificato con l'autotest)."), null));
            impEk = new CheckBox { Content = "Kernel esteso (EK)", Margin = new Thickness(0, 1) };
            ToolTip.SetTip(impEk, "Kernel esteso del NEC-2: per fili grossi e distanze sotto 2 raggi (come la casella della barra in alto).");
            impEk.IsCheckedChanged += (s, e) => { if (!impSync && chkEk != null) chkEk.IsChecked = impEk.IsChecked; };
            mot.Children.Add(RigaImp("", new TextBlock { Text = "fili grossi, distanze < 2 raggi", FontSize = 11.5, Foreground = Pal.B(Pal.Muted) }));
            mot.Children.Insert(mot.Children.Count - 1, impEk);
            mot.Children.Add(RigaImp("Processori usati", new TextBlock { Text = Engine.Processori.Calcolo + " su " + Environment.ProcessorCount + (Engine.Processori.Calcolo < Environment.ProcessorCount ? " (uno resta alla finestra)" : ""), VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore) }, null,
                "Dalla 0.4.20 il calcolo (matrice, fattorizzazione, sweep, diagramma) lascia libero un processore logico per la finestra, " +
                "cosi' il programma risponde anche mentre calcola un modello grande. Con 1 o 2 processori si usano tutti; il collaudo senza finestra li usa tutti."));
            {
                // 0.4.20 (approvato da IC8TEM il 14/11/2026): calcolo automatico quando si apre un modello
                StackPanel scAuto = Scelta(new[] { "sempre", "solo modelli leggeri", "mai" }, (int)Math.Round(cfg.GetD("calc_auto", 1)), k =>
                {
                    cfg.SetD("calc_auto", k);
                    Registro.Scrivi("IMPOSTAZIONI", "calcolo automatico all'apertura: " + (k == 0 ? "sempre" : k == 1 ? "solo modelli leggeri" : "mai"));
                });
                ToolTip.SetTip(scAuto, "Cosa succede quando apri un modello o lo cambi. \"sempre\": calcolo alla frequenza centrale e subito dopo lo sweep (come fino alla 0.4.19). " +
                    "\"solo modelli leggeri\" (normale): oltre " + SegmentiModelloGrande + " segmenti, o se il calcolo dura piu' di 2 s, lo sweep non parte da solo: premi Sweep; " +
                    "il diagramma 3D si fa prima a 5 gradi e poi a 2 gradi in sottofondo. \"mai\": all'apertura si disegna solo la struttura, il calcolo parte con Calcola.");
                mot.Children.Add(RigaImp("Calcolo automatico all'apertura", scAuto));
            }
            {
                // 0.4.30 (IC8TEM 14/11/2026 22:13): velocita' della luce del motore
                StackPanel scC = Scelta(new[] { "299,792458 (come EZNEC)", "299,8 (come nec2c)" }, Engine.Nec2Engine.Cvel == Engine.Nec2Engine.CVEL_NEC2 ? 1 : 0, k =>
                {
                    if (Engine.Nec2Engine.CvelBloccata) return;
                    cfg.SetD("cvel_nec2c", k);
                    Engine.Nec2Engine.Cvel = k == 1 ? Engine.Nec2Engine.CVEL_NEC2 : Engine.Nec2Engine.CVEL_FISICA;
                    Registro.Scrivi("IMPOSTAZIONI", "velocita' della luce del motore " + (k == 1 ? "299,8 (nec2c)" : "299,792458 (EZNEC)"));
                    Calculate(true);
                });
                ToolTip.SetTip(scC, "Il NEC-2 originale e nec2c usano c = 299,8 m/us, EZNEC il valore esatto 299,792458. La differenza (2,5 parti su 100000) sposta l'impedenza " +
                    "di circa lo 0,15 % e, sulle loop magnetiche ad alto Q, anche del 2-10 %. Normale: come EZNEC. Il collaudo usa 299,8 per i confronti con nec2c.");
                mot.Children.Add(RigaImp("Velocita' della luce (m/us)", scC));
            }
            CheckBox impRapido = Casella("Sweep rapido (MBPE adattivo, Burke-Miller)", Analysis.SweepRapido, true,
                "Calcola un punto su quattro e infittisce da solo dove la curva cambia in fretta; gli altri punti si stimano con funzioni razionali. " +
                "Verificato con nec2c: ROS entro 0,01 %, guadagno e F/B entro 0,07 dB. Togli la spunta per calcolare ogni punto.");
            impRapido.IsCheckedChanged += (s, e) => { Analysis.SweepRapido = impRapido.IsChecked == true; Registro.Scrivi("IMPOSTAZIONI", "sweep rapido " + (Analysis.SweepRapido ? "si" : "no")); };
            mot.Children.Add(impRapido);
            impCurr = new CheckBox { Content = "Sorgenti a corrente in questo modello (schiere fasate)", Margin = new Thickness(0, 1) };
            ToolTip.SetTip(impCurr, "Come EZNEC e ARRL: i valori delle sorgenti sono correnti e il programma ricava le tensioni a ogni frequenza (V = Z I). Si imposta anche negli Struttura 3D > Oggetti del modello > Sorgenti.");
            impCurr.IsCheckedChanged += (s, e) => { if (!impSync && model != null) { model.CurrentSources = impCurr.IsChecked == true; ApplicaERicalcola("sorgenti a corrente"); } };
            mot.Children.Add(impCurr);
            mot.Children.Add(Casella("Schiere fasate: proponi sorgenti a corrente", false, false, "Avviso automatico quando ci sono piu' sorgenti sfasate (EZNEC, ARRL)."));
            impZ0 = Num(1, 10000, 50, 1, 1, 70);
            impZ0.ValueChanged += (s, e) => { if (!impSync && numZ0 != null && impZ0.Value.HasValue) numZ0.Value = impZ0.Value; };
            mot.Children.Add(RigaImp("Riferimento ROS (Z0)", impZ0, null, "Impedenza di riferimento per il ROS e la carta di Smith (come Z0 nella barra in alto)."));
            mot.Children.Add(RigaImp("Guadagno mostrato in", SceltaFutura(new[] { "dBi", "dBd", "tutti e due" }, 0, "I riquadri mostrano gia' dBi e sotto dBd; la scelta arriva con la prossima versione.")));
            c1.Children.Add(Pannello("Motore", mot));

            // ---- Segmentazione ----
            StackPanel seg = new StackPanel();
            impSegL = Num(5, 400, 40, 0, 1, 60);
            impSegL.ValueChanged += (s, e) => { if (!impSync && numSegL != null && impSegL.Value.HasValue) numSegL.Value = impSegL.Value; };
            seg.Children.Add(RigaImp("Segmenti per lambda", impSegL, null, "Risegmentazione dei modelli MMANA (come nella barra in alto)."));
            {
                Button bVs = Btn("Verifica della segmentazione...", false);
                bVs.Click += async (s, e) => await ApriVerificaSegmentazione();
                seg.Children.Add(RigaImp("Controllo", bVs, null, "Apre la finestra della verifica con la correzione automatica (anche dalla barra in alto)."));
                Button bCd = Btn("Controlla il modello...", false);
                bCd.Click += async (s, e) => await ApriControlloDati();
                seg.Children.Add(RigaImp("Controllo dei dati", bCd, null, "Tutti i controlli dei dati senza calcolare (anche dalla barra in alto, pulsante Controlla). Manuale, capitolo 11."));
            }
            seg.Children.Add(Casella("Segmenti piu' corti vicino alle giunzioni (come EZNEC)", true, false, "La fa la finestra Verifica della segmentazione (pulsante sopra)."));
            seg.Children.Add(Casella("Rispetta il rapporto segmento/raggio (livello prudente)", true, false, "Controllato sempre dalla verifica della segmentazione e negli Avvisi (8 con il kernel normale, 2 con il kernel esteso)."));
            seg.Children.Add(Casella("Rastrematura stile MMANA all'importazione (no: ricalcolo NEC)", false, false, "I file MMANA si risegmentano sempre per il NEC-2 (manuale, capitolo 10)."));
            seg.Children.Add(RigaImp("Diametro di sezioni non tonde", SceltaFutura(new[] { "raggio equivalente (Balanis)", "massima estensione (Rothammel)" }, 0, "Convertitore del raggio nella tabella dei fili: prossima versione.")));
            {
                StackPanel scLee = Scelta(new[] { "nessuna", "correzione Leeson", "Leeson + confronto senza" }, model != null && model.Leeson ? (Analysis.ConfrontoLeeson ? 2 : 1) : 0, k =>
                {
                    if (model == null) return;
                    model.Leeson = k >= 1;
                    Analysis.ConfrontoLeeson = k == 2;   // seconda soluzione senza correzione solo a richiesta (0.4.2)
                    if (txtNec != null) txtNec.Text = model.ToNec();
                    Registro.Scrivi("AZIONE", "correzione Leeson " + (model.Leeson ? "attiva" : "spenta"));
                    Calculate(true);
                });
                ToolTip.SetTip(scLee, "Elementi a diametri a gradini (tubi telescopici): per il calcolo diventano un elemento di diametro uniforme con lunghezza corretta, " +
                    "ricalcolata a ogni frequenza, come la Stepped Diameter Correction di EZNEC. Solo elementi diritti e simmetrici, con sorgenti/carichi/linee al centro, " +
                    "entro il 15 % della loro risonanza; gli Avvisi dicono quali elementi sono stati corretti. La scelta si salva nel file (riga CM YA-NEC LEESON=1). " +
                    "\"Leeson + confronto senza\" calcola anche l'impedenza senza correzione (negli Avvisi): e' una seconda soluzione, il calcolo dura il doppio.");
                seg.Children.Add(RigaImp("Tubi telescopici", scLee));
            }
            c1.Children.Add(Pannello("Segmentazione", seg));

            // ---- Convergenza ----
            StackPanel conv = new StackPanel();
            conv.Children.Add(Casella("AGT (test del guadagno medio) a ogni calcolo", true, false, "Riquadro AGT a ogni calcolo (vale in spazio libero e su terreno perfetto); la prova completa e' negli Avvisi (test AGT)."));
            conv.Children.Add(Casella("Prova di convergenza: rifai con segmenti raddoppiati (avviso se dG > 0,2 dB o dZ > 2%)", true, false, "Non ancora automatica: si fa a mano con il test AGT negli Avvisi (ARRL 24a, 4nec2)."));
            conv.Children.Add(Casella("Controlli geometrici prima del calcolo (incroci, fili vicini, sorgenti, radiali)", true, false, "Le sei regole del Geometry Check di EZNEC, a ogni calcolo, nella linguetta Avvisi."));
            conv.Children.Add(Casella("Spia guadagno Yagi (WA2PHW): avviso se > +0,5 dB sull'atteso", true, false, "In un'altra forma: il controllo r5 dei Controlli dei dati segnala il guadagno sopra il massimo fisico per la lunghezza del boom."));
            conv.Children.Add(Casella("Stack: avviso se lobi della singola > -15 dB (DL6WU)", false, false, "I lobi si leggono nella tabella delle distanze di stack."));
            c1.Children.Add(Pannello("Convergenza e controlli", conv));

            // ---- Terreno ----
            StackPanel ter = new StackPanel();
            {
                StackPanel scTer = Scelta(new[] { "coefficiente di riflessione (GN 0, veloce)", "Sommerfeld (GN 2, esatto)" }, cfg.GetD("terreno_sommerfeld", 0) > 0.5 ? 1 : 0, k =>
                {
                    cfg.SetD("terreno_sommerfeld", k);
                    if (model != null && model.Ground != null && model.Ground.Kind == GroundKind.Real)
                    {
                        model.Ground.Sommerfeld = k == 1;
                        if (txtNec != null) txtNec.Text = model.ToNec();
                        Calculate(true);
                    }
                });
                ToolTip.SetTip(scTer, "Terreno reale: il coefficiente di riflessione (GN 0) va bene per antenne alte piu' di circa 0,2 lambda; per fili bassi, radiali sopraelevati, " +
                    "Beverage ed EWE serve Sommerfeld (GN 2, come EZNEC High Accuracy). Il motore e' quello di nec2c, verificato con l'autotest.");
                ter.Children.Add(RigaImp("Modello", scTer));
            }
            impGround = new ComboBox { Width = 230 };
            impGround.SelectionChanged += (s, e) => { if (!impSync && cmbGround != null && impGround.SelectedIndex >= 0) cmbGround.SelectedIndex = impGround.SelectedIndex; };
            ter.Children.Add(RigaImp("Tipo", impGround, null, "Come il menu Terreno della barra in alto (costante dielettrica; conducibilita' S/m)."));
            {
                // 0.5.3: avviso quando nel menu Terreno si sceglie un solaio in calcestruzzo (bozzetto A approvato il 15/11/2026 19:55)
                CheckBox chkSol = Casella("Avviso quando scelgo un solaio in calcestruzzo", cfg.Get("avviso_solaio", "1") != "0", true,
                    "Finestra che spiega quando usare il solaio e porta al calcolatore Antenna su tetto. Si spegne anche dalla finestra stessa.");
                chkSol.Click += (s, e) => cfg.Set("avviso_solaio", chkSol.IsChecked == true ? "1" : "0");
                ter.Children.Add(chkSol);
            }
            ter.Children.Add(Casella("Secondo terreno (solo campo lontano)", false, false, "Distanza e tipo del secondo terreno: approvato per la 1.5 (Terreno reale 2)."));
            ter.Children.Add(new TextBlock { Text = "Campo lontano a bassa elevazione (sotto 5 gradi)", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 6, 0, 2), Foreground = Pal.B(Pal.Fore) });
            ter.Children.Add(Casella("Terreno/mare lontano per le basse elevazioni (solo diagramma)", true, false, "Si usa in Diagrammi > Terreno reale: riflessione sul mare con fattore di divergenza e rugosita'."));
            ter.Children.Add(Casella("Fattore di divergenza D = [1 + 2 d1 d2 / (a d sin psi)]^-1/2 (raggio 4/3)", true, false, "Curvatura terrestre, in Diagrammi > Terreno reale."));
            ter.Children.Add(Casella("Rugosita' R = R0 exp[-2 (k h sin psi)^2] (onda del mare)", true, false, "Altezza rms dell'onda e quota dell'antenna sul mare, in Diagrammi > Terreno reale."));
            c2.Children.Add(Pannello("Terreno", ter));

            // ---- Diagrammi ----
            StackPanel dia = new StackPanel();
            dia.Children.Add(RigaImp("Passo degli angoli", SceltaFutura(new[] { "0,5", "1", "2", "5" }, 2, "Il diagramma 3D si calcola a 2 gradi con l'affinamento del massimo.")));
            dia.Children.Add(RigaImp("Polarizzazione", SceltaFutura(new[] { "totale", "oriz.", "vert.", "circ. destra", "circ. sinistra" }, 0, "Totale, verticale e orizzontale sono nella linguetta Diagrammi; le circolari arrivano dopo.")));
            dia.Children.Add(RigaImp("F/R: settore posteriore", new TextBlock { Text = "180 gradi (90-270)", Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center }, "come EZNEC", "Il settore di 120 gradi come MMANA arriva con la prossima versione."));
            dia.Children.Add(Casella("Mostra l'angolo a -3 dB (triangolo nei diagrammi 2D)", true, true, "Gia' attivo dalla 0.4.0: raggi tratteggiati, arco e valore. Il cono nel 3D arriva dopo."));
            dia.Children.Add(Casella("Campo vicino (schede NE/NH)", false, false, "Per la linguetta Campi EM."));
            c2.Children.Add(Pannello("Diagrammi e campo lontano", dia));

            // ---- Struttura 3D (1.2.0, IC8TEM 17/11/2026 23:46) ----
            StackPanel s3d = new StackPanel();
            ComboBox cColF = new ComboBox { ItemsSource = ColoriFiloNomi, SelectedIndex = Math.Max(0, Math.Min(ColoriFiloNomi.Length - 1, (int)cfg.GetD("filo_scelto_colore", 0))), MinWidth = 120 };
            ComboBox cVelF = new ComboBox { ItemsSource = VelaturaNomi, SelectedIndex = Math.Max(0, Math.Min(VelaturaNomi.Length - 1, (int)cfg.GetD("filo_scelto_velatura", 1))), MinWidth = 120 };
            cColF.SelectionChanged += (s, e) => { cfg.Set("filo_scelto_colore", Math.Max(0, cColF.SelectedIndex).ToString(CultureInfo.InvariantCulture)); ApplicaColoreFiloScelto(); };
            cVelF.SelectionChanged += (s, e) => { cfg.Set("filo_scelto_velatura", Math.Max(0, cVelF.SelectedIndex).ToString(CultureInfo.InvariantCulture)); ApplicaColoreFiloScelto(); };
            s3d.Children.Add(RigaImp("Colore del filo scelto", cColF, null, "Clic su un filo nella vista 3D (o su una riga della tabella dei fili): il filo si sottolinea con questo colore, velato, e la sua riga si evidenzia."));
            s3d.Children.Add(RigaImp("Velatura", cVelF, null, "Quanto e' coprente il colore: leggera, media o forte. Sulla riga della tabella il velo e' piu' leggero, per leggere i numeri."));
            c2.Children.Add(Pannello("Struttura 3D", s3d));

            // ---- Stack ----
            StackPanel stk = new StackPanel();
            stk.Children.Add(RigaImp("Metodo", SceltaFutura(new[] { "completo NEC", "automatico", "fattore di schiera" }, 0, "Per ora sempre NEC completo (accoppiamento esatto). Metodo ibrido EEP approvato per gli stack grandi.")));
            stk.Children.Add(RigaImp("Formula DL6WU", SceltaFutura(new[] { "lambda/(2 sin a/2)", "57,3 lambda/a" }, 0, "a = apertura dell'antenna base in spazio libero: stack verticale -> apertura verticale; affiancato -> orizzontale (si scambiano con la polarizzazione).")));
            stk.Children.Add(Casella("Mostra l'impedenza attiva di ogni antenna", true, false, "Nella tabella dello stack e negli Oggetti del modello (telaio dello stack, divisori)."));
            stk.Children.Add(Casella("Confronto NEC completo / fattore di schiera sugli stack piccoli", false, false, ""));
            c3.Children.Add(Pannello("Stack e schiere", stk));

            // ---- Perdite ----
            StackPanel per = new StackPanel();
            impMat = new ComboBox { Width = 200 };
            impMat.SelectionChanged += (s, e) => { if (!impSync && cmbMat != null && impMat.SelectedIndex >= 0) cmbMat.SelectedIndex = impMat.SelectedIndex; };
            per.Children.Add(RigaImp("Conduttori", impMat, null, "Materiale dei fili (come il menu Fili della barra in alto)."));
            impLoads = new CheckBox { Content = "Usa i carichi del modello", Margin = new Thickness(0, 1) };
            impLoads.IsCheckedChanged += (s, e) => { if (!impSync && model != null) { model.UseLoads = impLoads.IsChecked == true; ApplicaERicalcola("usa i carichi"); } };
            per.Children.Add(impLoads);
            per.Children.Add(Casella("Perdite dei cavi negli Oggetti del modello", true, false, "Le linee TL del NEC-2 sono senza perdite; le perdite si calcolano negli Oggetti Linea e accordatore e Cavo e balun."));
            per.Children.Add(Casella("Verticali: perdita nel terreno sempre calcolata", true, false, "Con Sommerfeld e i radiali."));
            c3.Children.Add(Pannello("Perdite", per));

            // ---- Dove valgono ----
            StackPanel dv = new StackPanel();
            dv.Children.Add(RigaImp("Queste impostazioni", SceltaFutura(new[] { "solo questo modello", "predefinite per tutti" }, 0, "Z0, terreno e materiale si salvano nel modello (.nec) e in config.ini.")));
            StackPanel bb = new StackPanel { Orientation = Orientation.Horizontal };
            Button bAp = Btn("Applica e ricalcola", true);
            bAp.Click += (s, e) => { if (model != null) Calculate(true); };
            Button bSave = Btn("Salva come predefinite", false);
            bSave.Click += (s, e) =>
            {
                try
                {
                    cfg.SetD("z0_predefinita", (double)(numZ0.Value ?? 50));
                    cfg.SetD("segmenti_lambda", (double)(numSegL.Value ?? 40));
                    Status("Impostazioni salvate come predefinite in config.ini.");
                }
                catch (Exception ex) { Status("Salvataggio non riuscito: " + ex.Message, true); }
            };
            Button bRes = Btn("Ripristina", false);
            bRes.Click += (s, e) => { numZ0.Value = 50; numSegL.Value = 40; chkEk.IsChecked = false; Status("Valori di base ripristinati (Z0 50 ohm, 40 segmenti per lambda, kernel normale)."); };
            bb.Children.Add(bAp); bb.Children.Add(bSave); bb.Children.Add(bRes);
            dv.Children.Add(bb);
            dv.Children.Add(new Border { Background = Pal.B(Pal.OkBg), BorderBrush = Pal.B(Pal.Green), BorderThickness = new Thickness(3, 0, 0, 0), Padding = new Thickness(8, 4),
                Child = new TextBlock { Text = "Ogni casella ha il suo fumetto con la spiegazione e la fonte. In grigio: spuntata = sempre attiva (non si spegne); non spuntata = non ancora disponibile; scelte a pillole = scelta fissa. Manuale, capitolo 21.", TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore) } });
            c3.Children.Add(Pannello("Dove valgono", dv));

            // ---- Aggiornamento (0.4.15, richiesta di IC8TEM del 14/11/2026 08:59, confermata alle 09:03) ----
            StackPanel ag = new StackPanel();
            string motivoPacchetto;
            bool pacchetto = Aggiornamento.DaPacchetto(out motivoPacchetto);
            CheckBox chkAgg = Casella("Aggiornamento automatico dal GitHub (ogni " + Aggiornamento.IntervalloMinuti + " minuti)", Aggiornamento.Attivo && !pacchetto, !pacchetto,
                "Controlla ogni 3 minuti (la prima volta 10 secondi dopo l'avvio) l'ultima versione pubblicata sul GitHub; se e' piu' nuova la scarica in silenzio, ne verifica l'impronta SHA-256 " +
                "e la installa: entra in funzione al prossimo avvio, una finestra lo avvisa e il programma si riavvia da solo dopo 10 secondi, riaprendo il modello (copia della versione precedente in data/aggiornamenti). Non tocca config.ini ne' i modelli. " +
                "Spento durante il collaudo. Non vale per i pacchetti .deb e .rpm, che si aggiornano con il gestore dei pacchetti.");
            chkAgg.Click += (s, e) => { cfg.Set("aggiornamento_auto", chkAgg.IsChecked == true ? "1" : "0"); if (chkAgg.IsChecked == true) Aggiornamento.Avvia(cfg); else Aggiornamento.Ferma(); };
            ag.Children.Add(chkAgg);
            TextBox tbRepo = new TextBox { Text = Aggiornamento.Repository, Watermark = "utente/repository", Width = 200, IsEnabled = !pacchetto };
            ToolTip.SetTip(tbRepo, "Repository GitHub da cui si aggiorna il programma, scritto come utente/nome (per esempio IC8TEM/YA-NEC). Invio per salvarlo in config.ini.");
            tbRepo.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) { cfg.Set("aggiornamento_repo", (tbRepo.Text ?? "").Trim()); Status("Repository dell'aggiornamento: " + (tbRepo.Text ?? "").Trim()); } };
            ag.Children.Add(RigaImp("Repository", tbRepo));
            // 1.5 (bozzetto approvato da IC8TEM il 18/11/2026 19:35): "Forza l'aggiornamento" e "Reinstalla la versione attuale"
            Button bOra = Btn("Forza l'aggiornamento", true);
            ToolTip.SetTip(bOra, "Controlla subito su GitHub senza aspettare i 3 minuti. Se c'e' una versione nuova la scarica, avvisa e riavvia come l'aggiornamento automatico (10 secondi, il modello si riapre). Funziona anche con l'aggiornamento automatico spento.");
            bOra.IsEnabled = !pacchetto;
            CheckBox chkReinst = new CheckBox { Content = "Reinstalla la versione attuale", IsChecked = false, IsEnabled = !pacchetto, Margin = new Thickness(0, 2, 0, 0) };
            ToolTip.SetTip(chkReinst, "Riscarica e reinstalla anche se la versione e' la stessa: utile se un file del programma si e' rovinato.");
            bOra.Click += async (s, e) => { cfg.Set("aggiornamento_repo", (tbRepo.Text ?? "").Trim()); await Aggiornamento.Controlla(true, chkReinst.IsChecked == true); };
            ag.Children.Add(bOra);
            ag.Children.Add(chkReinst);
            lblAggiornamento = new TextBlock { Text = "Stato: " + (pacchetto ? "spento: " + motivoPacchetto : Aggiornamento.Stato), TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), FontSize = 12, Margin = new Thickness(0, 4, 0, 0) };
            ag.Children.Add(lblAggiornamento);
            c3.Children.Add(Pannello("Aggiornamento", ag));

            Grid.SetColumn(c1, 0); Grid.SetColumn(c2, 2); Grid.SetColumn(c3, 4);
            g.Children.Add(c1); g.Children.Add(c2); g.Children.Add(c3);
            SincronizzaImpostazioni();
            return new ScrollViewer { Content = g };
        }

        Control Futuro(Control c) { c.IsEnabled = false; return c; }

        // riporta nella linguetta i valori della barra in alto e del modello
        void SincronizzaImpostazioni()
        {
            if (impZ0 == null) return;
            impSync = true;
            try
            {
                if (numZ0 != null) impZ0.Value = numZ0.Value;
                if (numSegL != null) impSegL.Value = numSegL.Value;
                if (chkEk != null) impEk.IsChecked = chkEk.IsChecked;
                if (cmbGround != null) { impGround.ItemsSource = cmbGround.ItemsSource; impGround.SelectedIndex = cmbGround.SelectedIndex; }
                if (cmbMat != null) { impMat.ItemsSource = cmbMat.ItemsSource; impMat.SelectedIndex = cmbMat.SelectedIndex; }
                impCurr.IsChecked = model != null && model.CurrentSources;
                impLoads.IsChecked = model == null || model.UseLoads;
                impCurr.IsEnabled = impLoads.IsEnabled = model != null;
            }
            finally { impSync = false; }
        }
    }
}
