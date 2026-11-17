// YA-NEC - sottolinguetta "Trombe e illuminatori" della linguetta Calcolatori (preparata da IC8TEM in un'altra chat, integrata nella 0.4.19;
// colori presi dal tema Pal come le altre pagine delle parabole).
// Dimensionamento per banda (1296 / 2320 / 5760 / 10368 MHz) di coffee-can, VE4MA, Super-VE4MA, Chaparral, W2IMU,
// tromba conica e piramidale; diagramma d'apertura, efficienza sulla parabola e modello NEC a griglia di fili.
//   Misure | Diagramma | Sulla parabola | Modello NEC | Fonti
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
    public class TrombaPanel : UserControl
    {
        /// <summary>Impostato da MainWindow: apre in YA-NEC il modello NEC generato (testo della scheda, nome suggerito).</summary>
        public Action<string, string> ApriNec;

        static readonly CultureInfo IT = P.IT;
        static readonly string[] NomiTipi = { "Coffee-can (guida circolare aperta)", "VE4MA (anello di Kumar)", "Super-VE4MA (W1GHZ 2006)", "Chaparral (tre anelli)", "W2IMU dual-mode", "Tromba conica", "Tromba piramidale" };
        static readonly string[] NomiProgetto = { "Per f/D (primo fuoco)", "Per theta* (offset)", "Per guadagno", "Misure libere" };

        readonly TrombaInput inp = new TrombaInput();
        TrombaResult res;
        readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        readonly List<Action> letture = new List<Action>();
        readonly Dictionary<string, Border> pillole = new Dictionary<string, Border>();
        string pagina = "misure";
        ContentControl hostPagina;
        StackPanel pannelloAvvisi;
        TextBlock titolo;
        TextBox tbFreq;

        public TrombaPanel()
        {
            timer.Tick += (s, e) => { timer.Stop(); Ricalcola(); };
            Content = Costruisci();
            Ricalcola();
        }

        Control Costruisci()
        {
            var root = new Grid { ColumnDefinitions = new ColumnDefinitions("340,*"), Background = new SolidColorBrush(P.CSfondo) };
            var sx = new StackPanel { Spacing = 4, Margin = new Thickness(8, 8, 12, 8) };
            sx.Children.Add(new TextBlock { Text = "Trombe e illuminatori", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(P.CFam) });
            sx.Children.Add(P.Nota("Misure per banda, diagramma d'apertura, efficienza sulla parabola e modello NEC da aprire in YA-NEC."));

            sx.Children.Add(Famiglia("Banda e tipo"));
            var cbB = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 4) };
            var voci = new List<string>();
            for (int i = 0; i < Parabola.FreqBande.Length; i++) voci.Add(string.Format(IT, "{0} - {1:0.0} MHz", Parabola.NomiBande[i], Parabola.FreqBande[i]));
            voci.Add("Frequenza libera");
            cbB.ItemsSource = voci; cbB.SelectedIndex = 0;
            sx.Children.Add(cbB);
            tbFreq = Campo(sx, "Frequenza", "MHz", () => inp.FreqMHz, v => inp.FreqMHz = v, "");
            cbB.SelectionChanged += (s, e) => { if (cbB.SelectedIndex >= 0 && cbB.SelectedIndex < Parabola.FreqBande.Length) tbFreq.Text = Parabola.FreqBande[cbB.SelectedIndex].ToString("0.0", IT); };
            var cbT = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, ItemsSource = NomiTipi, SelectedIndex = (int)inp.Tipo, Margin = new Thickness(0, 4, 0, 4) };
            cbT.SelectionChanged += (s, e) => { inp.Tipo = (TipoTromba)Math.Max(0, cbT.SelectedIndex); Ricalcola(); };
            sx.Children.Add(cbT);
            var cbP = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, ItemsSource = NomiProgetto, SelectedIndex = 0, Margin = new Thickness(0, 0, 0, 4) };
            ToolTip.SetTip(cbP, "Per f/D: la tromba si dimensiona per il taper totale al bordo di un primo fuoco. Per theta*: per il livello del solo feed ai bordi di un offset (vedi la sottolinguetta Parabola offset). Per guadagno: solo conica e piramidale (Balanis).");
            cbP.SelectionChanged += (s, e) => { inp.Progetto = (ProgettoTromba)Math.Max(0, cbP.SelectedIndex); Ricalcola(); };
            sx.Children.Add(cbP);

            sx.Children.Add(Famiglia("Obiettivo"));
            Campo(sx, "f/D della parabola", "", () => inp.FsuD, v => inp.FsuD = v, "Primo fuoco.");
            Campo(sx, "theta* dell'offset", "gradi", () => inp.ThetaStar_deg, v => inp.ThetaStar_deg = v, "Semiangolo dei bordi visti dal fuoco: si legge nella sottolinguetta Parabola offset.");
            Campo(sx, "Taper", "dB", () => inp.Taper_dB, v => inp.Taper_dB = v, "Per f/D: taper totale al bordo (feed + attenuazione spaziale), 10 dB di W1GHZ. Per theta*: livello del solo feed ai bordi.");
            Campo(sx, "Guadagno", "dBi", () => inp.Guadagno_dBi, v => inp.Guadagno_dBi = v, "Solo per il progetto per guadagno.");

            sx.Children.Add(Famiglia("Misure libere e guide"));
            Campo(sx, "Bocca (diametro o lato H)", "lambda", () => inp.Bocca_lambda, v => inp.Bocca_lambda = v, "");
            Campo(sx, "Bocca, lato E (piramidale)", "lambda", () => inp.BoccaB_lambda, v => inp.BoccaB_lambda = v, "");
            Campo(sx, "Svasatura", "lambda", () => inp.Svasatura_lambda, v => inp.Svasatura_lambda = v, "Conica: lunghezza obliqua dal vertice; piramidale: lunghezza assiale della svasatura.");
            Campo(sx, "Ingresso circolare", "lambda", () => inp.Ingresso_lambda, v => inp.Ingresso_lambda = v, "W2IMU e conica: tra 0,586 e 0,766 lambda passa solo il TE11.");
            Campo(sx, "Guida rettangolare a", "mm", () => inp.GuidaA_mm, v => inp.GuidaA_mm = v, "0 = guida WR standard della banda (WR650, WR430, WR159, WR90).");
            Campo(sx, "Guida rettangolare b", "mm", () => inp.GuidaB_mm, v => inp.GuidaB_mm = v, "");

            sx.Children.Add(Famiglia("Modello NEC e parabola"));
            Campo(sx, "Maglia della griglia", "lambda", () => inp.Maglia_lambda, v => inp.Maglia_lambda = v, "Circa lambda/10 (4nec2, W1GHZ). Piu' larga = calcolo piu' leggero e meno preciso.");
            Spunta(sx, "Polarizzazione circolare (due dipoli a 90 gradi)", () => inp.Circolare, v => inp.Circolare = v, "Polarizzatore ideale al posto del septum: il diagramma non cambia (W1GHZ), cambia solo la polarizzazione.");
            Campo(sx, "Diametro della parabola", "m", () => inp.DiametroParabola_m, v => inp.DiametroParabola_m = v, "Solo per l'efficienza; 0 = 20 lambda.");

            var scroll = new ScrollViewer { Content = sx, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            root.Children.Add(scroll);

            var dx = new DockPanel { Margin = new Thickness(0, 8, 8, 8) };
            var barra = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            foreach (var kv in new[] { ("misure", "Misure"), ("diagramma", "Diagramma"), ("parabola", "Sulla parabola"), ("nec", "Modello NEC"), ("fonti", "Fonti") })
                barra.Children.Add(Pillola(kv.Item1, kv.Item2));
            DockPanel.SetDock(barra, Dock.Top); dx.Children.Add(barra);
            titolo = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(P.CFam), Margin = new Thickness(2, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
            DockPanel.SetDock(titolo, Dock.Top); dx.Children.Add(titolo);
            pannelloAvvisi = new StackPanel { Spacing = 3, Margin = new Thickness(0, 6, 0, 0) };
            var scAvv = new ScrollViewer { Content = pannelloAvvisi, MaxHeight = 150, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            DockPanel.SetDock(scAvv, Dock.Bottom); dx.Children.Add(scAvv);
            hostPagina = new ContentControl(); dx.Children.Add(hostPagina);
            Grid.SetColumn(dx, 1); root.Children.Add(dx);
            Seleziona("misure");
            return root;
        }

        TextBlock Famiglia(string t) { return new TextBlock { Text = t, FontSize = 15, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(P.CFam), Margin = new Thickness(0, 10, 0, 2) }; }

        TextBox Campo(StackPanel sp, string etichetta, string unita, Func<double> get, Action<double> set, string fumetto)
        {
            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,90,52") };
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

        // 0.4.19: per le prove automatiche dell'interfaccia (come ParabolaPanel)
        public IEnumerable<string> ChiaviPagine() { return new List<string>(pillole.Keys); }
        public void ApriPagina(string key) { Seleziona(key); }
        public int SegmentiNec { get { return res == null ? 0 : res.NecSegmenti; } }

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

        void Ricalcola()
        {
            foreach (var l in letture) l();
            try { res = Tromba.Calcola(inp); }
            catch (Exception ex) { res = new TrombaResult { Errore = "Errore nel calcolo: " + ex.Message }; }
            pannelloAvvisi.Children.Clear();
            if (res.Errore != null) pannelloAvvisi.Children.Add(P.Avviso(2, res.Errore));
            else foreach (var a in res.Avvisi.OrderByDescending(x => x.Livello)) pannelloAvvisi.Children.Add(P.Avviso(a.Livello, a.Testo));
            MostraPagina();
        }

        void MostraPagina()
        {
            if (hostPagina == null || res == null) return;
            if (res.Errore != null) { hostPagina.Content = P.Carta(P.Nota(res.Errore)); titolo.Text = ""; return; }
            titolo.Text = string.Format(IT, "{0}  -  {1:0.0} MHz (lambda {2:0.0} mm)  -  {3}", NomiTipi[(int)inp.Tipo], inp.FreqMHz, res.Lambda_mm,
                res.Analitico ? string.Format(IT, "{0:0.0} dBi, -10 dB a {1:0} / {2:0} gradi (E / H)", res.Guadagno_dBi, res.Theta10E, res.Theta10H) : "diagramma dal NEC");
            Control c;
            switch (pagina)
            {
                case "diagramma": c = PaginaDiagramma(); break;
                case "parabola": c = PaginaParabola(); break;
                case "nec": c = PaginaNec(); break;
                case "fonti": c = PaginaFonti(); break;
                default: c = PaginaMisure(); break;
            }
            hostPagina.Content = new ScrollViewer { Content = c, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        static string D1(double v, string f = "0.0") { return P.D1(v, f); }

        Control PaginaMisure()
        {
            var r = res; var sp = new StackPanel();
            sp.Children.Add(P.Carta(new TrombaSezione { Res = r, Height = 300 }, r.Rettangolare ? "Sezione in scala (sopra il piano H, sotto il piano E)" : "Sezione in scala"));
            var righe = new List<string[]> { new[] { "#Quota", "Misura", "Nota" } };
            foreach (var q in r.Quote) righe.Add(new[] { q.Nome, q.Nome.StartsWith("Semiangolo") ? D1(q.Mm, "0.0") + " gradi" : D1(q.Mm, "0.0") + " mm", q.Nota });
            if (r.LambdaG_mm > 0) righe.Add(new[] { "Lunghezza d'onda in guida", D1(r.LambdaG_mm, "0.0") + " mm", "nella guida d'ingresso" });
            sp.Children.Add(P.Carta(P.Tabella(righe), "Misure interne"));
            var modi = new List<string[]> { new[] { "#Sezione", "Modo", "Taglio", "" } };
            foreach (var m in r.Modi) modi.Add(new[] { m.Sezione, m.Modo, D1(m.Taglio_MHz, "0") + " MHz", m.Propaga ? "si propaga" : "sotto il taglio" });
            sp.Children.Add(P.Carta(P.Tabella(modi, "Auto,Auto,Auto,*"), "Modi in guida"));
            sp.Children.Add(P.Carta(P.Nota("Le misure sono interne; lo spessore delle pareti si aggiunge fuori. La sonda coassiale va tarata per il ROS: nel modello NEC c'e' un dipolo ideale nello stesso punto.")));
            return sp;
        }

        Control PaginaDiagramma()
        {
            var r = res; var sp = new StackPanel();
            if (r.Th == null) { sp.Children.Add(P.Carta(P.Nota("Diagramma non disponibile in forma chiusa per questa tromba: salvare il modello NEC (pagina Modello NEC), calcolarlo in YA-NEC e caricarlo nella sottolinguetta Parabola con \"Feed dal modello NEC\"."))); return sp; }
            var ros = Pal.Red;
            var g = new ParabolaGrafico { Height = 380, Titolo = "Diagramma del feed (integrazione d'apertura)", TitoloX = "angolo dall'asse (gradi)", TitoloY = "dB", XMin = -180, XMax = 180, PassoX = 30, FormatoX = "0", YMin = -40, YMax = 2, PassoY = 5, LegendaPos = 1 };
            g.Serie_.Add(new ParabolaGrafico.Serie { X = r.Th, Y = r.DbE, Specchia = true, Nome = "piano E" });
            g.Serie_.Add(new ParabolaGrafico.Serie { X = r.Th, Y = r.DbH, Specchia = true, Colore = Pal.Orange, Nome = "piano H" });
            g.LineeY.Add((-3.0, "-3 dB", ros));
            g.LineeY.Add((-10.0, "-10 dB", Pal.WarnText));
            g.LineeX.Add((r.AngoloObiettivo_deg, string.Format(IT, "bordo {0:0.0} gradi", r.AngoloObiettivo_deg), Pal.Green));
            g.LineeX.Add((-r.AngoloObiettivo_deg, "", Pal.Green));
            g.Marcatori.Add((r.AngoloObiettivo_deg, r.LivelloObiettivo_dB, Pal.Green, string.Format(IT, "obiettivo {0:0.0} dB", r.LivelloObiettivo_dB), 'o'));
            sp.Children.Add(P.Carta(g));
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "#Grandezza", "Piano E", "Piano H" },
                new[] { "Larghezza a -3 dB", D1(r.Theta3E, "0.0") + " gradi", D1(r.Theta3H, "0.0") + " gradi" },
                new[] { "Angolo a -10 dB (dall'asse)", D1(r.Theta10E, "0.0") + " gradi", D1(r.Theta10H, "0.0") + " gradi" },
                new[] { "Guadagno / efficienza d'apertura", D1(r.Guadagno_dBi, "0.00") + " dBi", P.Pc(r.EtaApertura) },
            }, "Auto,Auto,Auto"), "Valori"));
            sp.Children.Add(P.Carta(P.Nota("Modello d'apertura: valido nel lobo principale, dove serve per illuminare il disco. Non vede le correnti esterne, il bordo e gli anelli: per i lobi posteriori, i feed con choke e il confronto finale serve il NEC. "
                + (inp.Tipo == TipoTromba.W2IMU ? string.Format(IT, "W2IMU: TM11 dosato a {0:0.000} (normalizzazione Ey(0) = 1) per avere i piani E e H uguali, come nel progetto di Potter.", r.Beta) : ""))));
            return sp;
        }

        Control PaginaParabola()
        {
            var r = res; var sp = new StackPanel();
            if (r.CurvaFds == null)
            {
                sp.Children.Add(P.Carta(P.Tabella(new[]
                {
                    new[] { "f/D migliore (W1GHZ, HFSS)", D1(r.PubFsuD, "0.00"), "" },
                    new[] { "Efficienza calcolata", P.Pc(r.PubEff), "disco di 20 lambda" },
                    new[] { "Efficienza misurata", P.Pc(r.PubEffMisurata), "WD5AGO a 2,3 GHz, f/D 0,37" },
                }), "Valori pubblicati"));
                return sp;
            }
            var g = new ParabolaGrafico { Height = 340, Titolo = "Efficienza d'apertura del feed in funzione di f/D", TitoloX = "f/D", TitoloY = "%", XMin = 0.25, XMax = 1.0, PassoX = 0.1, FormatoX = "0.0", YMin = 0, YMax = 100, PassoY = 20, LegendaPos = 2 };
            g.Serie_.Add(new ParabolaGrafico.Serie { X = r.CurvaFds, Y = r.CurvaEff.Select(v => v * 100).ToArray(), Nome = "spillover x illuminazione x fase x polarizzazione" });
            g.Marcatori.Add((r.FsuDUsata, r.EffAllaFsuD * 100, Pal.Green, string.Format(IT, "f/D {0:0.00}: {1:0.0}%", r.FsuDUsata, r.EffAllaFsuD * 100), 'o'));
            g.Marcatori.Add((r.FsuDOttima, r.EffOttima * 100, Pal.Red, string.Format(IT, "massimo {0:0.0}% a {1:0.00}", r.EffOttima * 100, r.FsuDOttima), 'v'));
            sp.Children.Add(P.Carta(g));
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "Efficienza alla f/D di progetto", P.Pc(r.EffAllaFsuD), string.Format(IT, "f/D {0:0.00}", r.FsuDUsata) },
                new[] { "f/D migliore", D1(r.FsuDOttima, "0.00"), P.Pc(r.EffOttima) },
                new[] { "Centro di fase", double.IsNaN(r.CentroFase_mm) ? "-" : D1(r.CentroFase_mm, "+0.0;-0.0") + " mm", "dalla bocca, + verso il disco (negativo = dentro la tromba)" },
                new[] { "Ombra sul disco", D1(r.DiametroEsterno_m * 1000, "0") + " mm", "diametro equivalente" },
            }), "Sulla parabola"));
            sp.Children.Add(P.Carta(P.Nota("Calcolo con lo stesso motore della sottolinguetta Parabola (pagina Feed): blocco, superficie e rete esclusi. W1GHZ: un disco reale rende almeno il 15% meno del calcolo.")));
            return sp;
        }

        Control PaginaNec()
        {
            var r = res; var sp = new StackPanel();
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "Segmenti", r.NecSegmenti.ToString(IT), string.Format(IT, "{0} fili; nodi non collegati: {1}", r.NecFili, r.NecNodiSciolti) },
                new[] { "Matrice", D1(r.NecMemoria_MB, "0") + " MB", "numeri complessi in doppia precisione (16 byte x segmenti^2)" },
                new[] { "Raggio dei fili", D1(r.NecRaggioFilo_mm, "0.000") + " mm", "area uguale (2 pi a = maglia), al massimo 1/4 del lato piu' corto" },
                new[] { "Eccitazione", inp.Circolare ? "due dipoli a 90 gradi" : "dipolo lungo Y", "a lambda guida / 4 dal fondo" },
                new[] { "Diagrammi", "phi 90, 0, 45", "piano E, piano H, piano a 45 gradi, theta da 0 a 180" },
            }), "Modello a griglia di fili"));
            var riga = new WrapPanel();
            var bS = new Button { Content = "Salva il file .nec", Margin = new Thickness(0, 0, 8, 4) };
            bS.Click += async (s, e) => await Salva(NomeFile(), res.Nec);
            var bA = new Button { Content = "Apri in YA-NEC", Margin = new Thickness(0, 0, 8, 4), IsEnabled = ApriNec != null };
            bA.Click += (s, e) => { try { ApriNec?.Invoke(res.Nec, NomeFile()); } catch (Exception ex) { pannelloAvvisi.Children.Insert(0, P.Avviso(2, "Apertura non riuscita: " + ex.Message)); } };
            riga.Children.Add(bS); riga.Children.Add(bA);
            sp.Children.Add(P.Carta(riga));
            var anteprima = string.Join("\n", (res.Nec ?? "").Split('\n').Take(40)) + "\n...";
            sp.Children.Add(P.Carta(new TextBlock { Text = anteprima, FontFamily = new FontFamily("Consolas, Menlo, monospace"), FontSize = 11.5, TextWrapping = TextWrapping.NoWrap }, "Inizio del file"));
            sp.Children.Add(P.Carta(P.Nota("Superfici come griglia di fili da un segmento per lato, come i modelli NEC2 di W1GHZ (coffee-can, W2IMU, trombe coniche) e di VA3RR. "
                + "Il septum non e' nel modello: il polarizzatore non cambia il diagramma (W1GHZ). Bocca nel piano z = 0: il centro di fase che YA-NEC calcola e' riferito alla bocca. "
                + "Il diagramma calcolato si porta nella sottolinguetta Parabola con \"Feed dal modello NEC\".")));
            return sp;
        }

        string NomeFile() { return string.Format(CultureInfo.InvariantCulture, "tromba_{0}_{1:0}MHz.nec", inp.Tipo.ToString().ToLowerInvariant(), inp.FreqMHz); }

        Control PaginaFonti()
        {
            var sp = new StackPanel();
            sp.Children.Add(P.Carta(P.Tabella(new[]
            {
                new[] { "#Fonte", "Che cosa si usa" },
                new[] { "W1GHZ, Microwave Antenna Book cap. 6.3", "coffee-can: diametro 0,6-0,9 lambda per f/D 0,25-0,4; lunghezza oltre una lambda guida; diagrammi NEC2" },
                new[] { "W1GHZ, cap. 6.5", "W2IMU: bocca B circa 2,35 f/D, tra 1,22 e 1,7 lambda; semiangolo 44,6/B; sezione di fase C da TE11 e TM11 (esempi 2,8 e 3,52 lambda, ripresi nell'autotest)" },
                new[] { "W1GHZ, VE4MA and Chaparral feeds with Septum Polarizers (2006), misure WD5AGO", "VE4MA 0,77 lambda e anello 0,5 x 0,5; Super-VE4MA 0,71 lambda e anello 0,6 x 0,45; Chaparral tre anelli 0,2 x 0,33; f/D migliori ed efficienze" },
                new[] { "W1GHZ, cap. 12", "modelli NEC2 dei feed (DIPROD.NEC), maglia e uso di FEEDPATT/PHASEPAT" },
                new[] { "Balanis, Antenna Theory, par. 13.4.3 e 13.6", "tromba piramidale a guadagno ottimo (esempio 13.6 nell'autotest); tromba conica ottima d = radice di 3 l lambda" },
                new[] { "Potter (1963)", "tromba dual-mode: TM11 dosato per piani E e H uguali" },
                new[] { "Silver / Chu", "fattore di obliquita' della guida aperta con beta/k del TE11" },
                new[] { "VA3RR", "W2IMU modellata in 4nec2 per un offset da 60 cm: stesso percorso NEC -> FEEDPATT/PHASEPAT" },
            }, "Auto,*")));
            sp.Children.Add(P.Carta(P.Nota("Similitudini: W1GHZ e Balanis partono dallo stesso modello d'apertura (campo del modo con errore di fase quadratico); W1GHZ lo verifica con NEC2 e HFSS. "
                + "Differenze: Balanis ottimizza il guadagno della tromba da sola, W1GHZ l'efficienza sulla parabola; per un feed la tromba ottima di Balanis e' di solito troppo corta e larga, per questo qui si dimensiona sull'illuminazione del disco. "
                + "La regola W1GHZ B = 2,35 f/D per la W2IMU e il calcolo d'apertura qui usato danno bocche entro il 3-4%.")));
            return sp;
        }

        async System.Threading.Tasks.Task Salva(string nome, string testo)
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top == null) return;
                var f = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = nome, DefaultExtension = "nec" });
                if (f == null) return;
                using (var st = await f.OpenWriteAsync()) using (var w = new StreamWriter(st)) w.Write(testo);
            }
            catch (Exception ex) { pannelloAvvisi.Children.Insert(0, P.Avviso(2, "Salvataggio non riuscito: " + ex.Message)); }
        }
    }

    /// <summary>Sezione della tromba in scala: profilo interno (circolare: raggio; piramidale: sopra il piano H, sotto il piano E),
    /// anelli choke e posizione del dipolo d'eccitazione.</summary>
    public class TrombaSezione : Control
    {
        public TrombaResult Res;
        public TrombaSezione() { MinHeight = 240; ClipToBounds = true; }

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            var r = Res; if (r == null || r.Profilo.Count < 2) return;
            double zmin = r.Profilo[0].Z_m, zmax = 0.05 * Math.Abs(zmin), rmax = 0;
            foreach (var s in r.Profilo) rmax = Math.Max(rmax, Math.Max(s.A_m, s.B_m));
            foreach (var a in r.Anelli) rmax = Math.Max(rmax, a[3]);
            rmax *= 1.1;
            double W = Bounds.Width, H = Bounds.Height, m = 16;
            double sc = Math.Min((W - 2 * m) / (zmax - zmin), (H - 2 * m) / (2 * rmax));
            double cy = H / 2;
            Func<double, double, Point> Pt = (z, y) => new Point(m + (z - zmin) * sc, cy - y * sc);
            var muro = new Pen(new SolidColorBrush(Pal.Accent), 2.5);
            var asse = new Pen(new SolidColorBrush(Pal.Muted), 1, new DashStyle(new double[] { 4, 3 }, 0));
            ctx.DrawLine(asse, Pt(zmin, 0), Pt(zmax, 0));
            for (int i = 1; i < r.Profilo.Count; i++)
            {
                var a = r.Profilo[i - 1]; var b = r.Profilo[i];
                ctx.DrawLine(muro, Pt(a.Z_m, a.A_m), Pt(b.Z_m, b.A_m));
                ctx.DrawLine(muro, Pt(a.Z_m, -(r.Rettangolare ? a.B_m : a.A_m)), Pt(b.Z_m, -(r.Rettangolare ? b.B_m : b.A_m)));
            }
            var p0 = r.Profilo[0];
            ctx.DrawLine(muro, Pt(p0.Z_m, p0.A_m), Pt(p0.Z_m, -(r.Rettangolare ? p0.B_m : p0.A_m)));
            foreach (var a in r.Anelli)
                foreach (double sgn in new[] { 1.0, -1.0 })
                {
                    ctx.DrawLine(muro, Pt(a[0], sgn * a[2]), Pt(a[0], sgn * a[3]));
                    ctx.DrawLine(muro, Pt(a[0], sgn * a[3]), Pt(a[1], sgn * a[3]));
                }
            // dipolo a lambda guida / 4 dal fondo
            if (r.LambdaG_mm > 0)
            {
                double zd = p0.Z_m + r.LambdaG_mm / 4000.0, h = Math.Min(0.23 * r.Lambda_mm / 1000.0, 0.8 * p0.A_m);
                ctx.DrawLine(new Pen(new SolidColorBrush(Pal.Red), 2), Pt(zd, h), Pt(zd, -h));
            }
            var ft = new FormattedText("bocca (z = 0) ->", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 11, Pal.B(Pal.Muted));
            ctx.DrawText(ft, new Point(Math.Max(m, Pt(0, 0).X - ft.Width - 4), 4));
        }
    }
}
