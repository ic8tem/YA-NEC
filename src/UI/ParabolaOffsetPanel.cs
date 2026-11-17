// YA-NEC - sottolinguetta "Parabola offset" della linguetta Calcolatori.
// Stessa impostazione della "Parabola primo fuoco": dati a sinistra con i fumetti, pagine a pillola a destra
//   Riepilogo | Lobi | Geometria | Bande ed EME | Fonti
// Il feed puo' essere il cos^q teorico, il modello NEC aperto (stesso aggancio FornitoreFeedNec) o un file.
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using YaNec.Engine;
using P = YaNec.ParabolaPanel;

namespace YaNec
{
    public class ParabolaOffsetPanel : UserControl
    {
        /// <summary>Impostato da MainWindow (lo stesso delegato della parabola primo fuoco).</summary>
        public Func<FeedNec> FornitoreFeedNec;

        static readonly CultureInfo IT = P.IT;
        readonly ParabolaInput inp = new ParabolaInput { Diametro_m = 1.0, FsuD = 0.6, FreqMHz = 10368.1, OffsetBordoInferiore_m = 0.05, FeedDiametro_m = 0.06, ReteForo_mm = 2.0, ReteFilo_mm = 0.5, ErroreRmsCostruttivo_mm = 0.7, PerditeVarie_dB = 0.5 };
        OffsetResult res;
        readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        readonly List<Action> letture = new List<Action>();
        readonly Dictionary<string, Border> pillole = new Dictionary<string, Border>();
        string pagina = "riepilogo", nomeFeed;
        ContentControl hostPagina;
        StackPanel pannelloAvvisi;
        TextBlock titoloRisultato, notaFeed;
        TextBox tbFreq;

        public ParabolaOffsetPanel()
        {
            timer.Tick += (s, e) => { timer.Stop(); Ricalcola(); };
            Content = Costruisci();
            Ricalcola();
        }

        /// <summary>Feed con i due piani E e H su griglia comune (gradi da 0), fase facoltativa; null = cos^q teorico.</summary>
        public void ImpostaFeedPiani(double[] thetaDeg, double[] dbE, double[] dbH, double[] faseE, double[] faseH, string nome)
        {
            inp.FeedTabThetaDeg = null; inp.FeedTabDb = null; inp.FeedPiano45Db = null; inp.FeedPiano45Fase = null;
            inp.FeedPianiThetaDeg = thetaDeg; inp.FeedPianoDbE = dbE; inp.FeedPianoDbH = dbH;
            inp.FeedPianoFaseE = faseE; inp.FeedPianoFaseH = faseH;
            nomeFeed = thetaDeg == null ? null : nome;
            Ricalcola();
        }

        void UsaFeedNec()
        {
            FeedNec f = null;
            try { f = FornitoreFeedNec != null ? FornitoreFeedNec() : null; }
            catch (Exception ex) { pannelloAvvisi.Children.Insert(0, P.Avviso(2, "Diagramma del feed non disponibile: " + ex.Message)); return; }
            if (f == null || f.ThetaE == null || f.DbE == null)
            {
                pannelloAvvisi.Children.Insert(0, P.Avviso(1, "Nessun diagramma del feed: aprire il modello del feed e calcolarlo nella linguetta Diagrammi."));
                return;
            }
            double[] th, gE, gH, fE, fH;
            Parabola.PianiSuGriglia(f.ThetaE, f.DbE, f.FaseE, f.ThetaH, f.DbH, f.FaseH, out th, out gE, out gH, out fE, out fH);
            ImpostaFeedPiani(th, gE, gH, fE, fH, string.IsNullOrEmpty(f.Nome) ? "modello aperto" : f.Nome);
        }

        async System.Threading.Tasks.Task CaricaFeedFile()
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top == null) return;
                var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Diagramma del feed (W1GHZ/DF3GJ o CSV di YA-NEC; due file = piani E e H)", AllowMultiple = true });
                if (files == null || files.Count == 0) return;
                var letti = new List<Parabola.FeedLetto>(); var nomi = new List<string>();
                foreach (var file in files.Take(2))
                {
                    string testo;
                    using (var st = await file.OpenReadAsync()) using (var rd = new StreamReader(st)) testo = rd.ReadToEnd();
                    Parabola.FeedLetto fl; string err;
                    if (!Parabola.LeggiFeed(testo, Parabola.FormatoAuto, out fl, out err)) { pannelloAvvisi.Children.Insert(0, P.Avviso(2, "File \"" + file.Name + "\" non valido: " + err)); return; }
                    letti.Add(fl); nomi.Add(file.Name);
                }
                double[] th, gE, gH, fE, fH;
                if (letti.Count == 2)
                {
                    int iH = nomi[1].ToUpperInvariant().Contains("H") ? 1 : 0, iE = 1 - iH;
                    Parabola.PianiSuGriglia(letti[iE].Theta, letti[iE].DbE, letti[iE].Fase, letti[iH].Theta, letti[iH].DbE, letti[iH].Fase, out th, out gE, out gH, out fE, out fH);
                    ImpostaFeedPiani(th, gE, gH, fE, fH, "E = " + nomi[iE] + ", H = " + nomi[iH]);
                }
                else
                {
                    Parabola.PianiSuGriglia(letti[0].Theta, letti[0].DbE, letti[0].Fase, letti[0].Theta, letti[0].DbH, letti[0].Fase, out th, out gE, out gH, out fE, out fH);
                    ImpostaFeedPiani(th, gE, gH, fE, fH, nomi[0]);
                }
            }
            catch (Exception ex) { pannelloAvvisi.Children.Insert(0, P.Avviso(2, "Lettura non riuscita: " + ex.Message)); }
        }

        // ============================================================================================
        Control Costruisci()
        {
            var root = new Grid { ColumnDefinitions = new ColumnDefinitions("340,*"), Background = new SolidColorBrush(P.CSfondo) };
            var sx = new StackPanel { Spacing = 4, Margin = new Thickness(8, 8, 12, 8) };
            sx.Children.Add(new TextBlock { Text = "Parabola offset", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(P.CFam) });
            sx.Children.Add(P.Nota("Spicchio di paraboloide con il feed fuori dal fascio: niente blocco, ma feed piu' direttivo e illuminazione asimmetrica. Rende sui dischi piccoli a 5760 e 10368 MHz e sui dischi molto grandi a 1296 (OK1DFC, OE5JFL)."));

            sx.Children.Add(Famiglia("Banda e geometria"));
            var cb = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 4) };
            var voci = new List<string>();
            for (int i = 0; i < Parabola.FreqBande.Length; i++) voci.Add(string.Format(IT, "{0} - {1:0.0} MHz", Parabola.NomiBande[i], Parabola.FreqBande[i]));
            voci.Add("Frequenza libera");
            cb.ItemsSource = voci; cb.SelectedIndex = 3;
            sx.Children.Add(cb);
            tbFreq = Campo(sx, "Frequenza", "MHz", () => inp.FreqMHz, v => inp.FreqMHz = v, "Frequenza di calcolo.");
            cb.SelectionChanged += (s, e) => { if (cb.SelectedIndex >= 0 && cb.SelectedIndex < Parabola.FreqBande.Length) tbFreq.Text = Parabola.FreqBande[cb.SelectedIndex].ToString("0.0", IT); };
            Campo(sx, "Apertura D (asse minore)", "m", () => inp.Diametro_m, v => inp.Diametro_m = v, "Diametro dell'apertura proiettata, cioe' la larghezza del disco (asse minore dell'ellisse del bordo).");
            Campo(sx, "F/D (fuoco su D)", "", () => inp.FsuD, v => inp.FsuD = v, "Distanza focale della parabola madre divisa per l'apertura D. Per i dischi TV offset: F = (asse maggiore)^2 x D / (16 x profondita' x asse maggiore) circa; meglio misurare la profondita' e provare finche' la pagina Geometria ridà le misure.");
            Campo(sx, "Bordo inferiore sopra l'asse", "mm", () => inp.OffsetBordoInferiore_m * 1000, v => inp.OffsetBordoInferiore_m = v / 1000, "Quota del bordo inferiore rispetto all'asse della parabola madre. Positivo = offset vero (il feed non copre l'apertura); negativo = geometria ibrida tipo OK1DFC; -D/2 = primo fuoco.");

            sx.Children.Add(Famiglia("Illuminatore"));
            Campo(sx, "Feed a theta*", "dB", () => inp.TaperBordo_dB, v => inp.TaperBordo_dB = v, "Attenuazione del solo feed al semiangolo theta* che vede i bordi (senza l'attenuazione spaziale, che nell'offset e' diversa in alto e in basso). 10 dB e' il punto di partenza di W1GHZ.");
            notaFeed = P.Nota(""); sx.Children.Add(notaFeed);
            var riga = new WrapPanel();
            var bNec = new Button { Content = "Feed dal modello NEC", Margin = new Thickness(0, 0, 6, 4) };
            ToolTip.SetTip(bNec, "Usa i tagli E e H (e la fase, se c'e') calcolati da YA-NEC sul modello aperto.");
            bNec.Click += (s, e) => UsaFeedNec();
            var bFile = new Button { Content = "Feed da file", Margin = new Thickness(0, 0, 6, 4) };
            bFile.Click += async (s, e) => await CaricaFeedFile();
            var bCos = new Button { Content = "Feed teorico", Margin = new Thickness(0, 0, 6, 4) };
            bCos.Click += (s, e) => ImpostaFeedPiani(null, null, null, null, null, null);
            riga.Children.Add(bNec); riga.Children.Add(bFile); riga.Children.Add(bCos);
            sx.Children.Add(riga);
            Spunta(sx, "Puntamento del feed ottimo", () => inp.OffsetMiraOttima, v => inp.OffsetMiraOttima = v, "Cerca l'angolo del feed (da -10 a +10 gradi rispetto alla bisettrice dei bordi) che da' l'efficienza massima: compensa l'attenuazione spaziale piu' forte verso il bordo superiore.");
            Campo(sx, "Correzione manuale del puntamento", "gradi", () => inp.OffsetCorrezioneMira_deg, v => inp.OffsetCorrezioneMira_deg = v, "Usata senza la spunta qui sopra: + verso il bordo superiore.");
            Spunta(sx, "Centro di fase calcolato", () => inp.CentroFaseAuto, v => inp.CentroFaseAuto = v, "Solo con la fase del feed (modello NEC o file con la fase).");
            Campo(sx, "Centro di fase manuale", "mm", () => inp.CentroFaseManuale_mm, v => inp.CentroFaseManuale_mm = v, "+ verso il disco.");
            Campo(sx, "Diametro del feed", "mm", () => inp.FeedDiametro_m * 1000, v => inp.FeedDiametro_m = v / 1000, "Conta solo se il feed entra nel fascio (geometria ibrida o bordo inferiore troppo basso).");
            Campo(sx, "Perdite varie", "dB", () => inp.PerditeVarie_dB, v => inp.PerditeVarie_dB = v, "ROS, cavo, polarizzazione incrociata, supporto del feed.");

            sx.Children.Add(Famiglia("Superficie e ambiente"));
            Spunta(sx, "Superficie piena (lamiera)", () => inp.SuperficiePiena, v => inp.SuperficiePiena = v, "Pannelli pieni invece della rete: niente perdite di rete, ma vento come un disco pieno.");
            Campo(sx, "Foro della rete (luce)", "mm", () => inp.ReteForo_mm, v => inp.ReteForo_mm = v, "lambda/10 o meno.");
            Campo(sx, "Filo della rete", "mm", () => inp.ReteFilo_mm, v => inp.ReteFilo_mm = v, "Diametro del filo della rete: con il passo dei fori da' la trasmissione attraverso la rete (Mumford) e la superficie esposta al vento.");
            Campo(sx, "Massa superficiale", "kg/m2", () => inp.ReteMassaSup_kg_m2, v => inp.ReteMassaSup_kg_m2 = v, "Lamiera d'acciaio da 0,6 mm (disco TV) circa 4,7 kg/m2; alluminio 1 mm 2,7 kg/m2.");
            Campo(sx, "Errore RMS della superficie", "mm", () => inp.ErroreRmsCostruttivo_mm, v => inp.ErroreRmsCostruttivo_mm = v, "I dischi TV stampati stanno di solito sotto il millimetro; il vero limite e' la deformazione dei supporti.");
            Campo(sx, "Raffica di progetto", "km/h", () => inp.Vento_kmh, v => inp.Vento_kmh = v, "Velocita' del vento per il calcolo della spinta sul disco e sul supporto.");
            Campo(sx, "Errore di puntamento", "gradi", () => inp.ErrorePuntamento_deg, v => inp.ErrorePuntamento_deg = v, "Errore di puntamento previsto: la perdita vale 12 (errore / apertura a -3 dB)^2 dB e entra nel bilancio EME.");
            Campo(sx, "HPBW del corrispondente", "gradi", () => inp.HpbwCorrispondente_deg, v => inp.HpbwCorrispondente_deg = v, "0 = stesso disco all'altro capo.");
            Campo(sx, "Passo dei profili", "mm", () => inp.PassoProfilo_mm, v => inp.PassoProfilo_mm = v, "Passo delle tabelle di profondita' (dima di controllo).");

            var scroll = new ScrollViewer { Content = sx, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            root.Children.Add(scroll);

            var dx = new DockPanel { Margin = new Thickness(0, 8, 8, 8) };
            var barra = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            foreach (var kv in new[] { ("riepilogo", "Riepilogo"), ("lobi", "Lobi"), ("geometria", "Geometria"), ("bande", "Bande ed EME"), ("fonti", "Fonti") })
                barra.Children.Add(Pillola(kv.Item1, kv.Item2));
            DockPanel.SetDock(barra, Dock.Top); dx.Children.Add(barra);
            titoloRisultato = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(P.CFam), Margin = new Thickness(2, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
            DockPanel.SetDock(titoloRisultato, Dock.Top); dx.Children.Add(titoloRisultato);
            pannelloAvvisi = new StackPanel { Spacing = 3, Margin = new Thickness(0, 6, 0, 0) };
            var scAvv = new ScrollViewer { Content = pannelloAvvisi, MaxHeight = 170, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            DockPanel.SetDock(scAvv, Dock.Bottom); dx.Children.Add(scAvv);
            hostPagina = new ContentControl(); dx.Children.Add(hostPagina);
            Grid.SetColumn(dx, 1); root.Children.Add(dx);
            Seleziona("riepilogo");
            return root;
        }

        TextBlock Famiglia(string t) { return new TextBlock { Text = t, FontSize = 15, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(P.CFam), Margin = new Thickness(0, 10, 0, 2) }; }

        TextBox Campo(StackPanel sp, string etichetta, string unita, Func<double> get, Action<double> set, string fumetto)
        {
            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,90,46") };
            var lb = new TextBlock { Text = etichetta, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(P.CFam) };
            var tb = new TextBox { Text = P.Fmt(get()), HorizontalContentAlignment = HorizontalAlignment.Right, Margin = new Thickness(4, 0, 4, 0) };
            var un = new TextBlock { Text = unita, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Pal.Muted) };
            Grid.SetColumn(tb, 1); Grid.SetColumn(un, 2);
            g.Children.Add(lb); g.Children.Add(tb); g.Children.Add(un);
            if (!string.IsNullOrEmpty(fumetto)) { ToolTip.SetTip(lb, fumetto); ToolTip.SetTip(tb, fumetto); }
            letture.Add(() =>
            {
                double v;
                if (P.Leggi(tb.Text, out v)) { set(v); tb.Foreground = Pal.B(Pal.FieldFore); }
                else tb.Foreground = new SolidColorBrush(Pal.Red);
            });
            tb.TextChanged += (s, e) => { timer.Stop(); timer.Start(); };
            sp.Children.Add(g);
            return tb;
        }

        void Spunta(StackPanel sp, string testo, Func<bool> get, Action<bool> set, string fumetto)
        {
            var c = new CheckBox { Content = testo, IsChecked = get(), Foreground = new SolidColorBrush(P.CFam) };
            if (!string.IsNullOrEmpty(fumetto)) ToolTip.SetTip(c, fumetto);
            letture.Add(() => set(c.IsChecked == true));
            c.Click += (s, e) => { timer.Stop(); timer.Start(); };
            sp.Children.Add(c);
        }

        Border Pillola(string key, string testo)
        {
            var t = new TextBlock { Text = testo, FontSize = 14, Foreground = new SolidColorBrush(P.CBluTesto) };
            var b = new Border { Child = t, Background = Pal.B(Pal.Pill), BorderBrush = new SolidColorBrush(P.CBordo), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(12, 5), Margin = new Thickness(0, 0, 6, 4), Cursor = new Cursor(StandardCursorType.Hand) };
            b.PointerPressed += (s, e) => { Fascione.TogliFuoco(b); Seleziona(key); };  // 1.2.0: fuoco tolto prima del cambio di pagina (registro 13/11 17:58)
            pillole[key] = b;
            return b;
        }

        /// <summary>Pagine a pillola, per il collaudo automatico (0.4.13): chiavi e apertura di una pagina.</summary>
        public IEnumerable<string> ChiaviPagine() { return new List<string>(pillole.Keys); }
        public void ApriPagina(string key) { Seleziona(key); }

        void Seleziona(string key)
        {
            pagina = key;
            foreach (var kv in pillole)
            {
                bool sel = kv.Key == key;
                kv.Value.Background = sel ? new SolidColorBrush(P.CBlu) : Pal.B(Pal.Pill);
                ((TextBlock)kv.Value.Child).Foreground = sel ? Pal.B(Pal.AccentFore) : new SolidColorBrush(P.CBluTesto);
            }
            MostraPagina();
        }

        // ============================================================================================
        void Ricalcola()
        {
            foreach (var l in letture) l();
            try { res = Parabola.CalcolaOffset(inp); }
            catch (Exception ex) { res = new OffsetResult { Errore = "Errore nel calcolo: " + ex.Message }; }
            if (notaFeed != null && res != null && res.Errore == null)
                notaFeed.Text = inp.FeedPianiThetaDeg != null
                    ? string.Format(IT, "Illuminatore: \"{0}\" (il valore qui sopra non si usa).", nomeFeed) + (res.FaseUsata ? string.Format(IT, " Centro di fase {0:+0.0;-0.0} mm.", res.CentroFase_mm) : "")
                    : string.Format(IT, "Feed cos^q con q = {0:0.00} per theta* = {1:0.0} gradi (f/D equivalente {2:0.00}); bocca indicativa di una tromba adatta {3:0} mm.", res.Q, res.ThetaStar_deg, res.FsuDEquivalente, res.DiametroFeedStimato_mm);
            pannelloAvvisi.Children.Clear();
            if (res.Errore != null) pannelloAvvisi.Children.Add(P.Avviso(2, res.Errore));
            else foreach (var a in res.Avvisi.OrderByDescending(x => x.Livello)) pannelloAvvisi.Children.Add(P.Avviso(a.Livello, a.Testo));
            MostraPagina();
        }

        void MostraPagina()
        {
            if (hostPagina == null || res == null) return;
            if (res.Errore != null) { hostPagina.Content = P.Carta(P.Nota(res.Errore)); titoloRisultato.Text = ""; return; }
            titoloRisultato.Text = string.Format(IT, "{0:0.0} MHz  -  offset D {1:0.00} x {2:0.00} m  -  guadagno {3:0.0} dBi, efficienza {4:0}%, HPBW {5:0.00} gradi",
                inp.FreqMHz, res.AsseMinore_m, res.AsseMaggiore_m, res.Guadagno_dBi, res.EtaTotale * 100, res.Hpbw_deg);
            Control c;
            switch (pagina)
            {
                case "lobi": c = PaginaLobi(); break;
                case "geometria": c = PaginaGeometria(); break;
                case "bande": c = PaginaBande(); break;
                case "fonti": c = PaginaFonti(); break;
                default: c = PaginaRiepilogo(); break;
            }
            hostPagina.Content = new ScrollViewer { Content = c, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        static string D1(double v, string f = "0.00") { return P.D1(v, f); }

        Control PaginaRiepilogo()
        {
            var r = res; var sp = new StackPanel();
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "Guadagno ideale (pi D / lambda)^2", D1(r.GuadagnoIdeale_dBi, "0.00") + " dBi", "apertura proiettata uniforme" },
                new[] { "Spillover", P.Pc(r.EtaSpill), P.Db(r.EtaSpill) },
                new[] { "Illuminazione (taper)", P.Pc(r.EtaTaper), P.Db(r.EtaTaper) },
                new[] { "Fase", P.Pc(r.EtaFase), r.FaseUsata ? P.Db(r.EtaFase) : "senza la fase del feed" },
                new[] { "Polarizzazione (E/H diversi)", P.Pc(r.EtaPol), P.Db(r.EtaPol) },
                new[] { "Efficienza d'apertura", P.Pc(r.EtaApertura), P.Db(r.EtaApertura) },
                new[] { "Blocco del feed", P.Pc(r.EtaBlocco), r.EtaBlocco > 0.9999 ? "nessuno: il feed e' fuori dal fascio" : P.Db(r.EtaBlocco) },
                new[] { "Superficie (Ruze)", P.Pc(r.EtaRuze), P.Db(r.EtaRuze) },
                new[] { "Rete", P.Pc(r.EtaRete), P.Db(r.EtaRete) },
                new[] { "Perdite varie", P.Pc(r.EtaVarie), P.Db(r.EtaVarie) },
                new[] { "#Efficienza totale", P.Pc(r.EtaTotale), D1(r.Guadagno_dBi, "0.00") + " dBi" },
            }), "Guadagno"));
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "Bordo inferiore", D1(r.IllumBordoInf_dB, "0.0") + " dB", "rispetto al centro dell'apertura (feed + attenuazione spaziale)" },
                new[] { "Bordo superiore", D1(r.IllumBordoSup_dB, "0.0") + " dB", "il bordo superiore e' piu' lontano dal fuoco" },
                new[] { "Bordi laterali", D1(r.IllumBordoLat_dB, "0.0") + " dB", "" },
                new[] { "Puntamento del feed", D1(r.MiraUsata_deg, "+0.0;-0.0;0") + " gradi", string.Format(IT, "dalla bisettrice ({0:0.0} gradi dall'asse della parabola madre); ottimo {1:+0.0;-0.0;0}", r.PsiO_deg, r.MiraOttima_deg) },
                new[] { "Semiangolo da illuminare theta*", D1(r.ThetaStar_deg, "0.0") + " gradi", string.Format(IT, "come un primo fuoco con f/D {0:0.00}", r.FsuDEquivalente) },
            }), "Illuminazione"));
            sp.Children.Add(P.Carta(P.Nota("L'efficienza e' integrata su tutta l'apertura (griglia 240 x 240) con il campo F(theta')/rho del feed: spillover, illuminazione e fase escono dallo stesso integrale. "
                + "Mancano la polarizzazione incrociata dell'offset con polarizzazione lineare (Chu-Turrin) e la diffrazione dal bordo e dal supporto del feed.")));
            return sp;
        }

        Control PaginaLobi()
        {
            var r = res; var sp = new StackPanel();
            var ros = Pal.Red;
            double xm = r.ThetaDeg[r.ThetaDeg.Length - 1];
            var gr = new ParabolaGrafico { Height = 380, Titolo = "Diagramma nei piani orizzontale e verticale", TitoloX = "angolo dall'asse del fascio (gradi)", TitoloY = "dB" };
            gr.XMin = -xm; gr.XMax = xm; gr.YMin = -50; gr.YMax = 0; gr.PassoY = 10; gr.FormatoX = xm < 2 ? "0.00" : xm < 10 ? "0.0" : "0";
            gr.Serie_.Add(new ParabolaGrafico.Serie { X = r.ThetaDeg, Y = r.PattH_dB, Nome = "orizzontale (piano di simmetria perpendicolare)" });
            gr.Serie_.Add(new ParabolaGrafico.Serie { X = r.ThetaDeg, Y = r.PattV_dB, Colore = Pal.Orange, Spessore = 1.5, Nome = "verticale (piano di simmetria; + verso il bordo superiore)" });
            gr.LineeY.Add((-3.0, "-3 dB", ros));
            gr.LineeX.Add((-r.Hpbw_deg / 2, "", ros));
            gr.LineeX.Add((r.Hpbw_deg / 2, string.Format(IT, "HPBW {0:0.000} gradi", r.Hpbw_deg), ros));
            if (r.Hpbw_deg < 3) { gr.FasciaX1 = -Parabola.LunaDiametro_deg / 2; gr.FasciaX2 = Parabola.LunaDiametro_deg / 2; gr.FasciaTesto = "Luna"; }
            sp.Children.Add(P.Carta(gr));
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "#Piano", "HPBW", "Primo lobo" },
                new[] { "Orizzontale", D1(r.HpbwH_deg, "0.000") + " gradi", D1(r.PrimoLoboH_dB, "0.0") + " dB" },
                new[] { "Verticale", D1(r.HpbwV_deg, "0.000") + " gradi", D1(r.PrimoLoboV_dB, "0.0") + " dB" },
                new[] { "Media (usata per l'EME)", D1(r.Hpbw_deg, "0.000") + " gradi", string.Format(IT, "{0:0.000} lambda/D", r.HpbwCoeff) },
            }, "Auto,Auto,Auto"), "Larghezza del fascio"));
            sp.Children.Add(P.Carta(P.Nota("Il piano verticale e' asimmetrico perche' l'illuminazione e' piu' forte verso il bordo inferiore (piu' vicino al fuoco): i lobi dalla parte del bordo inferiore sono piu' alti. "
                + "Gli angoli sono misurati dall'asse della parabola madre, che e' la direzione del fascio; il bordo del disco e' inclinato rispetto a questo asse (pagina Geometria).")));
            return sp;
        }

        Control PaginaGeometria()
        {
            var r = res; var sp = new StackPanel();
            sp.Children.Add(P.Carta(new OffsetSezione { Res = r, Height = 340 }, "Sezione verticale in scala"));
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "Asse minore (larghezza)", D1(r.AsseMinore_m * 1000, "0") + " mm", "= apertura D" },
                new[] { "Asse maggiore (altezza)", D1(r.AsseMaggiore_m * 1000, "0") + " mm", "D / cos(inclinazione)" },
                new[] { "Inclinazione del bordo", D1(r.Inclinazione_deg, "0.00") + " gradi", "angolo tra il piano del bordo e la verticale con il fascio orizzontale" },
                new[] { "Profondita' massima", D1(r.ProfonditaMax_m * 1000, "0.0") + " mm", string.Format(IT, "perpendicolare al piano del bordo, a {0:0} mm dal bordo inferiore lungo l'asse maggiore", r.ProfonditaMaxS_m * 1000) },
                new[] { "Distanza focale F", D1(r.F_m * 1000, "0.0") + " mm", "della parabola madre" },
                new[] { "Fuoco - bordo inferiore", D1(r.FuocoBordoInf_m * 1000, "0") + " mm", "misure di controllo col metro" },
                new[] { "Fuoco - bordo superiore", D1(r.FuocoBordoSup_m * 1000, "0") + " mm", "" },
                new[] { "Fuoco - centro del disco", D1(r.FuocoCentro_m * 1000, "0") + " mm", "" },
                new[] { "Angoli dei bordi dal fuoco", D1(r.Psi1_deg, "0.0") + " / " + D1(r.Psi2_deg, "0.0") + " gradi", string.Format(IT, "bisettrice {0:0.0} gradi dall'asse", r.PsiO_deg) },
                new[] { "Parabola madre", D1(r.DiametroMadre_m * 1000, "0") + " mm", string.Format(IT, "f/D {0:0.000}{1}", r.FsuDMadre, r.Ibrido ? "; geometria ibrida (il disco contiene il vertice)" : "") },
                new[] { "Superficie", D1(r.AreaSuperficie_m2, "0.000") + " m2", string.Format(IT, "massa {0:0.0} kg", r.MassaSuperficie_kg) },
                new[] { "Vento sul piano del bordo", D1(r.ForzaVento_N, "0") + " N", string.Format(IT, "Cd {0:0.00}; con la rete piena di ghiaccio {1:0} N", r.CdRete, r.ForzaVentoGhiaccio_N) },
            }), "Misure"));
            var righe = new List<string[]> { new[] { "#s dal bordo inferiore", "Profondita'", "" } };
            foreach (var pt in r.ProfiloVerticale) righe.Add(new[] { D1(pt.S_m * 1000, "0") + " mm", D1(pt.Profondita_m * 1000, "0.0") + " mm", "" });
            sp.Children.Add(P.Carta(P.Tabella(righe), "Dima verticale (lungo l'asse maggiore, misure dal piano del bordo)"));
            var righe2 = new List<string[]> { new[] { "#x dal centro", "Profondita'", "" } };
            foreach (var pt in r.ProfiloOrizzontale) righe2.Add(new[] { D1(pt.X_m * 1000, "0") + " mm", D1(pt.Profondita_m * 1000, "0.0") + " mm", "" });
            sp.Children.Add(P.Carta(P.Tabella(righe2), "Dima orizzontale (lungo l'asse minore, a meta' dell'asse maggiore)"));
            var bt = new Button { Content = "Esporta i profili (CSV)" };
            bt.Click += async (s, e) => await Salva("profili_offset.csv", Parabola.ProfiliOffsetCsv(res));
            sp.Children.Add(P.Carta(bt));
            return sp;
        }

        Control PaginaBande()
        {
            var r = res; var sp = new StackPanel();
            var righe = new List<string[]> { new[] { "#Banda", "Guadagno", "Efficienza", "HPBW", "Eco Luna", "Puntamento 0,5 dB", "Giudizio" } };
            foreach (var b in r.Bande)
                righe.Add(new[] { b.Nome, D1(b.Guadagno_dBi, "0.0") + " dBi", P.Pc(b.Efficienza), D1(b.Hpbw_deg, "0.00") + " gradi", "-" + D1(b.PerditaEco_dB, "0.0") + " dB", D1(b.Tolleranza05_deg, "0.00") + " gradi", b.Giudizio });
            sp.Children.Add(P.Carta(P.Tabella(righe, "Auto,Auto,Auto,Auto,Auto,Auto,*"), "Lo stesso disco e lo stesso feed (in lambda) sulle quattro bande"));
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "Riempimento della Luna", P.Pc(r.FillLuna), "frazione del fascio occupata dalla Luna" },
                new[] { "Perdita sull'eco", D1(r.PerditaEco_dB, "0.0") + " dB", "rispetto alla sorgente puntiforme" },
                new[] { "Perdita di puntamento", D1(r.PerditaPuntamento_dB, "0.00") + " dB", "12 (errore / HPBW)^2" },
                new[] { "Squint in circolare", D1(r.Squint_deg, "0.000") + " gradi", string.Format(IT, "RHCP e LHCP in versi opposti: {0:0.000} gradi tra TX e RX", 2 * r.Squint_deg) },
            }), "EME alla frequenza di calcolo"));
            return sp;
        }

        Control PaginaFonti()
        {
            var sp = new StackPanel();
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "#Fonte", "Che cosa si usa" },
                new[] { "W1GHZ, Microwave Antenna Book, cap. 5 e 6", "geometria offset (angoli dei bordi, bisettrice, f/D equivalente), illuminazione asimmetrica, trombe dual-mode" },
                new[] { "OE5JFL, Ofs_calc_jfl", "calcolatore offset per radioamatori: misure del disco e posizione del feed; offset vero solo con il feed fuori dall'apertura" },
                new[] { "Bob Atkins, offset dish", "f/D e geometria dei dischi TV offset misurando assi e profondita'" },
                new[] { "OK2AQ, septum allungato per f/D 0,8", "offset da 1,8 m a 1296 MHz con un septum OK1DFC prolungato da una tromba: l'offset si usa anche a 23 cm" },
                new[] { "VA3RR, feed dual-mode al NEC2", "tromba W2IMU modellata con 4nec2 per un offset da 60 cm, efficienza e centro di fase con FEEDPATT e PHASEPAT: e' il percorso di YA-NEC" },
                new[] { "OK1DFC, offset da 8 m (Trenton 2024)", "caso reale a 1296 MHz in geometria ibrida: collaudo dell'inclinazione e dell'asse maggiore" },
                new[] { "Rudge e Adatia, Proc. IEEE 1978", "rassegna sulle antenne offset: efficienza, polarizzazione incrociata, squint in circolare" },
                new[] { "Adatia e Rudge, Electronics Letters 1975", "spostamento del fascio in polarizzazione circolare: sin(squint) = lambda sin(psiO) / (4 pi F)" },
                new[] { "Chu e Turrin, IEEE Trans. AP 1973", "polarizzazione incrociata dell'offset in polarizzazione lineare (non calcolata qui)" },
                new[] { "Kildal, Foundations of Antennas", "scomposizione dell'efficienza (spillover, illuminazione, fase, polarizzazione) e campo d'apertura" },
            }, "Auto,*")));
            sp.Children.Add(P.Carta(P.Nota("Similitudini: W1GHZ, OE5JFL e Atkins descrivono la stessa geometria (sezione del paraboloide madre, feed sulla bisettrice dei bordi). "
                + "Differenze: W1GHZ calcola l'efficienza con il diagramma del feed; OE5JFL e Atkins danno solo la geometria. OK1DFC usa un disco che contiene il vertice (ibrido) per non avere un feed enorme a 1296 MHz. "
                + "Per l'efficienza si integra il campo sull'apertura come Kildal. Per la polarizzazione, Rudge-Adatia e Chu-Turrin trattano gli effetti che qui sono solo segnalati (lo squint e' calcolato, la polarizzazione incrociata no).")));
            return sp;
        }

        async System.Threading.Tasks.Task Salva(string nome, string testo)
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top == null) return;
                var f = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = nome });
                if (f == null) return;
                using (var st = await f.OpenWriteAsync()) using (var w = new StreamWriter(st)) w.Write(testo);
            }
            catch (Exception ex) { pannelloAvvisi.Children.Insert(0, P.Avviso(2, "Salvataggio non riuscito: " + ex.Message)); }
        }
    }

    /// <summary>Sezione verticale in scala: parabola madre, spicchio offset, fuoco, asse del feed e raggi ai bordi.</summary>
    public class OffsetSezione : Control
    {
        public OffsetResult Res;
        public OffsetSezione() { MinHeight = 300; ClipToBounds = true; }

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            var r = Res; if (r == null || r.Errore != null) return;
            double F = r.F_m, y1 = r.Y1_m, y2 = r.Y2_m;
            // estensione: da min(y1, 0) a y2 in y; da 0 a max(F, z2) in z
            double ymin = Math.Min(y1, -0.05 * (y2 - y1)), ymax = y2 * 1.05, zmin = -0.05 * F, zmax = Math.Max(F, y2 * y2 / (4 * F)) * 1.1;
            double W = Bounds.Width, H = Bounds.Height, m = 20;
            double sc = Math.Min((W - 2 * m) / (zmax - zmin), (H - 2 * m) / (ymax - ymin));
            Func<double, double, Point> P_ = (z, y) => new Point(m + (z - zmin) * sc, H - m - (y - ymin) * sc);
            var grigio = new Pen(new SolidColorBrush(Pal.Muted), 1, new DashStyle(new double[] { 4, 3 }, 0));
            var blu = new Pen(new SolidColorBrush(Pal.Accent), 3);
            var ros = new Pen(new SolidColorBrush(Pal.Red), 1);
            var ver = new Pen(new SolidColorBrush(Color.Parse("#2E7D32")), 1.5);
            // asse della parabola madre
            ctx.DrawLine(grigio, P_(zmin, 0), P_(zmax, 0));
            // parabola madre (tratteggio) e spicchio (pieno)
            Point? prec = null;
            for (int i = 0; i <= 200; i++)
            {
                double y = ymin + (ymax - ymin) * i / 200.0, z = y * y / (4 * F);
                var p = P_(z, y); if (prec != null) ctx.DrawLine(grigio, prec.Value, p); prec = p;
            }
            prec = null;
            for (int i = 0; i <= 200; i++)
            {
                double y = y1 + (y2 - y1) * i / 200.0, z = y * y / (4 * F);
                var p = P_(z, y); if (prec != null) ctx.DrawLine(blu, prec.Value, p); prec = p;
            }
            // piano del bordo, fuoco, raggi ai bordi e asse del feed
            var pf = P_(F, 0);
            ctx.DrawLine(ver, P_(y1 * y1 / (4 * F), y1), P_(y2 * y2 / (4 * F), y2));
            ctx.DrawLine(ros, pf, P_(y1 * y1 / (4 * F), y1));
            ctx.DrawLine(ros, pf, P_(y2 * y2 / (4 * F), y2));
            double pa = (r.PsiO_deg + r.MiraUsata_deg) * Math.PI / 180.0, L = 0.6 * Math.Sqrt(F * F + r.Yc_m * r.Yc_m);
            ctx.DrawLine(new Pen(new SolidColorBrush(Pal.Orange), 2), pf, P_(F - L * Math.Cos(pa), L * Math.Sin(pa)));
            ctx.DrawEllipse(new SolidColorBrush(Pal.Red), null, pf, 4, 4);
            var ft = new FormattedText("fuoco", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 11, Pal.B(Pal.Muted));
            ctx.DrawText(ft, new Point(pf.X + 6, pf.Y + 2));
            var fl = new FormattedText("fascio verso destra (asse della parabola madre)", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 11, Pal.B(Pal.Muted));
            ctx.DrawText(fl, new Point(m, 4));
        }
    }
}
