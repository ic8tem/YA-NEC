// YA-NEC - linguetta Ottimizzatore (fase 5), alla lettera dal bozzetto approvato doc/bozzetto-ottimizzatore.png:
// 1. Obiettivi e pesi (cursori e soglie), frequenze da rispettare; 2. Variabili con minimo, massimo e passo; 3. Registro dei
// passi (clic = riprendi); a destra Andamento, comandi e Diagramma partenza / migliore; in alto le schede dello stato.
// Motore: Engine/Ottimizzatore.cs. Iniziato il 13/11/2026 su indicazione di IC8TEM ("si procedi").
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        class OttRigaObiettivo { public string Chiave; public Slider Peso; public TextBlock Valore; public TextBox Soglia; }
        class OttRigaVar { public string Nome; public CheckBox Usa; public TextBox Ora, Min, Max, Passo; public bool Formula; public string Expr, Note; }

        readonly List<OttRigaObiettivo> ottOb = new List<OttRigaObiettivo>();
        readonly List<OttRigaVar> ottVar = new List<OttRigaVar>();
        readonly List<double> ottFreq = new List<double>();
        readonly HashSet<string> ottTolte = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int ottFreqPrinc;
        WrapPanel ottFreqPanel;
        StackPanel ottVarPanel, ottRegPanel;
        LineChart ottChart;
        PolarPlot ottPolar;
        WrapPanel ottCards, resCards;
        readonly Dictionary<string, TextBlock[]> ottCard = new Dictionary<string, TextBlock[]>();
        bool ottBoom, ottSimplesso;
        Button ottMetodo, ottBoomBtn, ottAvvia, ottFerma;
        NecModel ottModelloVars;
        Ottimizzatore ott;
        CancellationTokenSource ottCts;
        OttPasso ottScelto;
        RunResult ottRunPartenza, ottRunScelto;
        DateTime ottT0;
        readonly List<double> ottSerieMigliore = new List<double>(), ottSeriePasso = new List<double>();
        DispatcherTimer ottTimer;
        bool ottInCorso;

        static readonly string[][] OttObiettivi =
        {
            new[] { "jx", "jX (risonanza)", "20", "-" },
            new[] { "ros", "ROS", "60", "≤ 1,3" },
            new[] { "g", "Guadagno", "80", "max" },
            new[] { "fb", "F/B", "70", "≥ 22 dB" },
            new[] { "el", "Elevazione", "0", "-" },
            new[] { "z", "Impedenza voluta", "40", "Z0 +j0" },
            new[] { "i", "Corrente in un punto", "0", "-" },
        };

        // ------------------------------------------------------------------------------------------------
        // schede in alto (al posto dei risultati mentre la linguetta e' aperta, come nel bozzetto)
        // ------------------------------------------------------------------------------------------------
        WrapPanel CreaSchedeOttimizzatore()
        {
            WrapPanel p = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 4), IsVisible = false };
            foreach (string[] k in new[] { new[] { "stato", "Stato" }, new[] { "voto", "Voto migliore" }, new[] { "g", "Guadagno" }, new[] { "fb", "F/B" }, new[] { "ros", "ROS" }, new[] { "z", "Impedenza" } })
            {
                TextBlock l = new TextBlock { Text = k[1], FontSize = 11, Foreground = Pal.B(Pal.Muted) };
                TextBlock v = new TextBlock { Text = "-", FontSize = 18, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore) };
                TextBlock s = new TextBlock { Text = " ", FontSize = 11, Foreground = Pal.B(Pal.Muted) };
                ottCard[k[0]] = new[] { l, v, s };
                StackPanel sp = new StackPanel { Spacing = 1 };
                sp.Children.Add(l); sp.Children.Add(v); sp.Children.Add(s);
                p.Children.Add(new Border
                {
                    Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10), Padding = new Thickness(9, 4), Margin = new Thickness(2), MinWidth = 120, Child = sp
                });
            }
            ottCards = p;
            return p;
        }

        void OttScheda(string k, string v, string s, Color? c = null)
        {
            TextBlock[] t;
            if (!ottCard.TryGetValue(k, out t)) return;
            t[1].Text = v; t[2].Text = string.IsNullOrEmpty(s) ? " " : s; t[1].Foreground = Pal.B(c ?? Pal.Fore);
        }

        // ------------------------------------------------------------------------------------------------
        // pagina
        // ------------------------------------------------------------------------------------------------
        Control BuildOttimizzatorePage()
        {
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,4*,3*") };
            // la finestra si ricostruisce col cambio di schema: righe e controlli si rifanno da capo
            ottOb.Clear();
            ottVar.Clear();
            ottModelloVars = null;

            // ---- 1. Obiettivi e pesi ----
            StackPanel ob = new StackPanel { Spacing = 2 };
            Grid hh = new Grid { ColumnDefinitions = new ColumnDefinitions("150,*,40,130") };
            TextBlock hp = new TextBlock { Text = "peso: a sinistra = ignora, a destra = conta di piu'", FontSize = 12, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap };
            TextBlock hs = new TextBlock { Text = "soglia", FontSize = 12, Foreground = Pal.B(Pal.Muted), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom };
            Grid.SetColumn(hp, 1); Grid.SetColumn(hs, 3);
            hh.Children.Add(hp); hh.Children.Add(hs);
            ob.Children.Add(hh);
            foreach (string[] o in OttObiettivi)
            {
                OttRigaObiettivo r = new OttRigaObiettivo { Chiave = o[0] };
                Grid rg = new Grid { ColumnDefinitions = new ColumnDefinitions("150,*,40,130"), Margin = new Thickness(0, 2) };
                TextBlock l = new TextBlock { Text = o[1], FontSize = 14, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
                r.Peso = new Slider { Minimum = 0, Maximum = 100, Value = double.Parse(o[2], CultureInfo.InvariantCulture), SmallChange = 5, LargeChange = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0) };
                r.Valore = new TextBlock { Text = o[2], VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 6, 0) };
                r.Soglia = new TextBox { Text = o[3], HorizontalContentAlignment = HorizontalAlignment.Center, Margin = new Thickness(2), FontFamily = Pal.Mono };
                OttRigaObiettivo rr = r;
                r.Peso.PropertyChanged += (s, e) => { if (e.Property == RangeBase.ValueProperty) rr.Valore.Text = Math.Round(rr.Peso.Value).ToString(CultureInfo.InvariantCulture); };
                ToolTip.SetTip(r.Soglia, SuggerimentoSoglia(o[0]));
                Grid.SetColumn(r.Peso, 1); Grid.SetColumn(r.Valore, 2); Grid.SetColumn(r.Soglia, 3);
                rg.Children.Add(l); rg.Children.Add(r.Peso); rg.Children.Add(r.Valore); rg.Children.Add(r.Soglia);
                ob.Children.Add(rg);
                ottOb.Add(r);
            }
            ob.Children.Add(Nota("Raggiunta la soglia, quell'obiettivo smette di pesare e l'ottimizzatore pensa agli altri (come \"F/B almeno 22 dB, poi il resto\")."));
            ob.Children.Add(new TextBlock { Text = "Frequenze da rispettare", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 8, 0, 2) });
            ottFreqPanel = new WrapPanel();
            ob.Children.Add(ottFreqPanel);
            ob.Children.Add(Nota("Il voto e' la somma sulle frequenze: tiene buona l'antenna anche ai bordi della banda. Si possono mettere bande diverse (antenne multibanda). " +
                                 "Clic su una frequenza: diventa la principale (in grassetto, quella delle schede in alto); clic destro: la toglie."));
            Control c1 = new ScrollViewer { Content = Pannello("1. Obiettivi e pesi", ob), HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };

            // ---- 2. Variabili ----
            StackPanel vp = new StackPanel { Spacing = 4 };
            ottVarPanel = new StackPanel { Spacing = 2 };
            vp.Children.Add(ottVarPanel);
            vp.Children.Add(Nota("Le variabili vengono dalla linguetta Variabili (lunghezze, spaziature, altezza, carichi, fasi delle sorgenti, distanza di stack...). " +
                                 "Una variabile scritta come formula (LDE = LR*0,985) segue le altre da sola: e' il \"collegamento\" di MMANA, gia' coperto dalle formule. " +
                                 "Ordine = ordine di prova: la piu' efficace in alto."));
            WrapPanel vb = new WrapPanel();
            Button bAgg = Btn("+ aggiungi variabile", false);
            bAgg.Click += (s, e) => OttMenuAggiungi(bAgg);
            Button bTutti = Btn("tutti gli elementi", false);
            ToolTip.SetTip(bTutti, "Mette nell'elenco tutte le variabili numeriche del modello (con minimo e massimo al ±3 %)");
            bTutti.Click += (s, e) => { OttCaricaVariabili(true); };
            ottBoomBtn = Btn("☐ mantieni lunghezza del boom", false);
            ToolTip.SetTip(ottBoomBtn, "Scarta le prove che allungano o accorciano il boom (estensione degli elementi lungo il boom)");
            ottBoomBtn.Click += (s, e) => { ottBoom = !ottBoom; ottBoomBtn.Content = (ottBoom ? "☑" : "☐") + " mantieni lunghezza del boom"; };
            ottMetodo = Btn("metodo: una variabile alla volta ▾", false);
            ottMetodo.Click += (s, e) =>
            {
                MenuFlyout f = new MenuFlyout();
                MenuItem a = new MenuItem { Header = "una variabile alla volta (come MMANA: passo che si dimezza)" };
                a.Click += (s2, e2) => { ottSimplesso = false; ottMetodo.Content = "metodo: una variabile alla volta ▾"; };
                MenuItem b = new MenuItem { Header = "simplesso di Nelder e Mead (tutte le variabili insieme)" };
                b.Click += (s2, e2) => { ottSimplesso = true; ottMetodo.Content = "metodo: simplesso (Nelder-Mead) ▾"; };
                f.Items.Add(a); f.Items.Add(b);
                f.ShowAt(ottMetodo);
            };
            vb.Children.Add(bAgg); vb.Children.Add(bTutti); vb.Children.Add(ottBoomBtn); vb.Children.Add(ottMetodo);
            vp.Children.Add(vb);

            StackPanel rp = new StackPanel { Spacing = 2 };
            ottRegPanel = new StackPanel { Spacing = 0 };
            rp.Children.Add(ottRegPanel);
            rp.Children.Add(Nota("Il voto migliore non e' sempre quello che preferisci: qui tieni gli ultimi 128 passi, li confronti e scegli tu (per esempio un passo con guadagno piu' alto e F/B un po' piu' basso)."));
            StackPanel c2s = new StackPanel();
            c2s.Children.Add(Pannello("2. Variabili che l'ottimizzatore puo' cambiare", vp));
            c2s.Children.Add(Pannello("3. Registro dei passi - clic su una riga per riprenderla", rp));
            Control c2 = new ScrollViewer { Content = c2s, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Margin = new Thickness(8, 0) };

            // ---- destra: andamento, comandi, diagramma ----
            StackPanel c3s = new StackPanel();
            ottChart = new LineChart { Title = "", XLabel = "passo", YLabel = "voto", Height = 260, EmptyText = "Avvia l'ottimizzatore per vedere l'andamento del voto" };
            c3s.Children.Add(Pannello("Andamento", Riquadro(ottChart)));
            StackPanel cmd = new StackPanel { Spacing = 6 };
            WrapPanel cb = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
            ottAvvia = Btn("▶ Avvia", true);
            ottAvvia.Click += (s, e) => OttAvvia();
            ottFerma = Btn("■ Ferma", true);
            ottFerma.Background = Pal.B(Pal.Red);
            ottFerma.IsEnabled = false;
            ottFerma.Click += (s, e) => { if (ottCts != null) ottCts.Cancel(); };
            Button bApp = Btn("Applica al modello", false);
            bApp.Click += (s, e) => OttApplica();
            cb.Children.Add(ottAvvia); cb.Children.Add(ottFerma); cb.Children.Add(bApp);
            cmd.Children.Add(cb);
            Button bAnn = Btn("Annulla tutto", false);
            bAnn.HorizontalAlignment = HorizontalAlignment.Center;
            bAnn.Click += (s, e) => OttAnnulla();
            cmd.Children.Add(bAnn);
            TextBlock na = Nota("\"Applica\" copia i valori nella linguetta Variabili; il modello di partenza resta salvabile a parte.");
            na.TextAlignment = TextAlignment.Center;
            cmd.Children.Add(na);
            c3s.Children.Add(new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 0, 10), Child = cmd
            });
            ottPolar = new PolarPlot { Title = "Diagramma: partenza / migliore", Height = 340 };
            c3s.Children.Add(Riquadro(ottPolar));
            Control c3 = new ScrollViewer { Content = c3s, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };

            Grid.SetColumn(c2, 1); Grid.SetColumn(c3, 2);
            g.Children.Add(c1); g.Children.Add(c2); g.Children.Add(c3);
            OttStatoSchede();
            return g;
        }

        static string SuggerimentoSoglia(string k)
        {
            switch (k)
            {
                case "jx": return "Reattanza accettata in ohm, es. \"≤ 5\" (\"-\" = nessuna soglia: piu' vicino a zero e' meglio)";
                case "ros": return "ROS accettato, es. \"≤ 1,3\": sotto la soglia l'obiettivo e' raggiunto";
                case "g": return "\"max\" = piu' guadagno possibile; oppure un valore in dBi, es. \"≥ 13,5\"";
                case "fb": return "F/B accettato in dB, es. \"≥ 22 dB\"";
                case "el": return "Elevazione voluta del massimo in gradi, es. \"15\" (si calcola il diagramma completo alla frequenza principale)";
                case "z": return "Impedenza voluta, es. \"28 +j0\" o \"Z0 +j0\" (Z0 della barra in alto)";
                case "i": return "Punto in cui la corrente deve essere minima, notazione MMANA: w4c (filo 4, centro), w4b, w4e, w4:7";
            }
            return "";
        }

        // ------------------------------------------------------------------------------------------------
        // sincronizzazione con il modello
        // ------------------------------------------------------------------------------------------------
        void OttSincronizza()
        {
            if (ottInCorso) return;
            if (model != ottModelloVars)
            {
                ottModelloVars = model;
                OttCaricaVariabili(false);
                OttFrequenzePredefinite();
                ott = null; ottScelto = null; ottRunScelto = null; ottRunPartenza = run;
                ottSerieMigliore.Clear(); ottSeriePasso.Clear();
                OttRegistro();
                OttGrafici();
            }
            OttStatoSchede();
        }

        void OttFrequenzePredefinite()
        {
            ottFreq.Clear();
            if (model == null) { OttFrequenze(); return; }
            double f0 = ParseNum(txtFreq.Text, model.FreqMHz), span = ParseNum(txtSpan.Text, 0) / 1000.0;
            if (span > 0) { ottFreq.Add(f0 - span / 2 * 0.75); ottFreq.Add(f0); ottFreq.Add(f0 + span / 2 * 0.75); ottFreqPrinc = 1; }
            else { ottFreq.Add(f0); ottFreqPrinc = 0; }
            OttFrequenze();
        }

        void OttFrequenze()
        {
            if (ottFreqPanel == null) return;
            ottFreqPanel.Children.Clear();
            for (int i = 0; i < ottFreq.Count; i++)
            {
                int k = i;
                Button b = Btn(Fmt(ottFreq[i], 3) + " MHz", false);
                if (i == ottFreqPrinc) b.FontWeight = FontWeight.Bold;
                b.Click += (s, e) => { ottFreqPrinc = k; OttFrequenze(); };
                b.PointerReleased += (s, e) =>
                {
                    if (e.InitialPressMouseButton != MouseButton.Right || ottFreq.Count <= 1) return;
                    ottFreq.RemoveAt(k);
                    if (ottFreqPrinc >= ottFreq.Count) ottFreqPrinc = ottFreq.Count - 1;
                    OttFrequenze();
                };
                ToolTip.SetTip(b, "Clic: frequenza principale. Clic destro: toglie la frequenza.");
                ottFreqPanel.Children.Add(b);
            }
            Button agg = Btn("+ aggiungi", false);
            agg.Click += (s, e) =>
            {
                TextBox tb = new TextBox { Width = 110, Watermark = "MHz", Margin = new Thickness(3) };
                tb.KeyDown += (s2, e2) =>
                {
                    if (e2.Key != Key.Enter) return;
                    double f = ParseNum(tb.Text, -1);
                    if (f > 0) { ottFreq.Add(f); ottFreq.Sort(); }
                    OttFrequenze();
                };
                ottFreqPanel.Children.Remove(agg);
                ottFreqPanel.Children.Add(tb);
                tb.Focus();
            };
            ottFreqPanel.Children.Add(agg);
        }

        static bool OttNumerica(string expr, out double v)
        {
            v = 0;
            try { v = Expr.Eval(Expr.NormalizeDecimal((expr ?? "").Replace(" ", "")), null); return true; }
            catch (Exception) { return false; }
        }

        void OttCaricaVariabili(bool tutte)
        {
            if (ottVarPanel == null) return;
            List<OttRigaVar> vecchie = new List<OttRigaVar>(ottVar);
            ottVar.Clear();
            if (tutte) ottTolte.Clear();
            if (model != null)
                foreach (SyVar v in model.Variables)
                {
                    if (v.Error != null || ottTolte.Contains(v.Name)) continue;
                    OttRigaVar vecchia = vecchie.Find(q => string.Equals(q.Nome, v.Name, StringComparison.OrdinalIgnoreCase));
                    if (!tutte && vecchia != null) { ottVar.Add(vecchia); continue; }
                    double val;
                    bool num = OttNumerica(v.Expr, out val);
                    string descr = string.IsNullOrWhiteSpace(v.Descr) ? model.DescribeUse(v.Name) : v.Descr;
                    bool altezza = Regex.IsMatch(v.Name + " " + descr, @"\b(H|ALT\w*|QUOTA|HGT|altezza)\b", RegexOptions.IgnoreCase);
                    OttRigaVar r = new OttRigaVar { Nome = v.Name, Formula = !num, Expr = v.Expr, Note = descr ?? "" };
                    r.Usa = new CheckBox { IsChecked = num ? !altezza : true, IsEnabled = num, VerticalAlignment = VerticalAlignment.Center };
                    r.Ora = OttCella(num ? FormatVal(v.Value) : "= " + v.Expr, num);
                    double passo = NiceStep(Math.Max(Math.Abs(v.Value) * 0.001, 1e-4));
                    r.Min = OttCella(num ? FormatVal(v.Value - Math.Abs(v.Value) * 0.03) : "", num);
                    r.Max = OttCella(num ? FormatVal(v.Value + Math.Abs(v.Value) * 0.03) : "", num);
                    r.Passo = OttCella(num ? (passo >= 1e-3 && passo < 1 ? FormatVal(passo * 1000) + " mm" : FormatVal(passo)) : "-", num);
                    if (num && altezza) { r.Min.Text = "-"; r.Max.Text = "-"; r.Passo.Text = "-"; }
                    if (!num)
                    {
                        string dip = model.Variables.Find(q => q != v && NecModel.ContainsName(v.Expr, q.Name))?.Name;
                        if (dip != null) r.Note = (r.Note.Length > 0 ? r.Note + " " : "") + "(segue " + dip + ")";
                    }
                    ottVar.Add(r);
                }
            OttDisegnaVariabili();
        }

        TextBox OttCella(string t, bool attiva)
        {
            return new TextBox { Text = t, IsEnabled = attiva, Margin = new Thickness(2), FontFamily = Pal.Mono, MinWidth = 70 };
        }

        void OttDisegnaVariabili()
        {
            ottVarPanel.Children.Clear();
            Grid h = OttGrigliaVar();
            string[] tt = { "", "Variabile", "Valore ora", "Minimo", "Massimo", "Passo", "Note" };
            for (int i = 0; i < tt.Length; i++)
            {
                TextBlock t = new TextBlock { Text = tt[i], FontSize = 12.5, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(4, 0) };
                Grid.SetColumn(t, i); h.Children.Add(t);
            }
            ottVarPanel.Children.Add(h);
            if (ottVar.Count == 0)
            {
                ottVarPanel.Children.Add(Nota("Il modello non ha variabili: scrivile nella linguetta Variabili (schede SY) e usale nelle schede GW."));
                return;
            }
            foreach (OttRigaVar r in ottVar)
            {
                Grid g = OttGrigliaVar();
                TextBox nome = new TextBox { Text = r.Nome, IsReadOnly = true, FontFamily = Pal.Mono, Margin = new Thickness(2) };
                TextBlock note = new TextBlock { Text = r.Note, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 0) };
                Control[] c = { r.Usa, nome, r.Ora, r.Min, r.Max, r.Passo, note };
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i].Parent is Panel pp) pp.Children.Remove(c[i]);
                    Grid.SetColumn(c[i], i); g.Children.Add(c[i]);
                }
                OttRigaVar rr = r;
                ContextMenu cm = new ContextMenu();
                MenuItem su = new MenuItem { Header = "Sposta in alto (provata prima)" };
                su.Click += (s, e) => { int k = ottVar.IndexOf(rr); if (k > 0) { ottVar.RemoveAt(k); ottVar.Insert(k - 1, rr); OttDisegnaVariabili(); } };
                MenuItem tg = new MenuItem { Header = "Togli dall'elenco" };
                tg.Click += (s, e) => { ottVar.Remove(rr); ottTolte.Add(rr.Nome); OttDisegnaVariabili(); };
                cm.Items.Add(su); cm.Items.Add(tg);
                nome.ContextMenu = cm;
                ToolTip.SetTip(nome, "Clic destro: sposta in alto o togli");
                ottVarPanel.Children.Add(g);
            }
        }

        static Grid OttGrigliaVar() { return new Grid { ColumnDefinitions = new ColumnDefinitions("30,70,110,95,95,80,*") }; }

        void OttMenuAggiungi(Control dove)
        {
            MenuFlyout f = new MenuFlyout();
            if (model != null)
                foreach (SyVar v in model.Variables)
                {
                    if (v.Error != null || ottVar.Exists(q => string.Equals(q.Nome, v.Name, StringComparison.OrdinalIgnoreCase))) continue;
                    SyVar vv = v;
                    MenuItem mi = new MenuItem { Header = v.Name + (string.IsNullOrWhiteSpace(v.Descr) ? "" : "  -  " + v.Descr) };
                    mi.Click += (s, e) => { ottTolte.Remove(vv.Name); OttCaricaVariabili(false); };
                    f.Items.Add(mi);
                }
            if (f.Items.Count == 0) f.Items.Add(new MenuItem { Header = "(tutte le variabili sono gia' nell'elenco; nuove variabili nella linguetta Variabili)", IsEnabled = false });
            f.ShowAt(dove);
        }

        // ------------------------------------------------------------------------------------------------
        // lettura dei comandi
        // ------------------------------------------------------------------------------------------------
        static double OttNumero(string t, double def)
        {
            Match m = Regex.Match(Expr.NormalizeDecimal((t ?? "").Replace(",", ".")), @"[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?");
            return m.Success ? double.Parse(m.Value, CultureInfo.InvariantCulture) : def;
        }

        static double OttLunghezza(string t, double def)
        {
            string s = (t ?? "").Trim();
            if (s.Length == 0 || s == "-") return def;
            try { return Ottimizzatore.Valuta(Regex.Replace(s, @"\s+", "")); }
            catch (Exception) { return OttNumero(s, def); }
        }

        bool OttLeggi(Ottimizzatore o, out string err)
        {
            err = null;
            foreach (OttRigaObiettivo r in ottOb)
            {
                OttObiettivo x = new OttObiettivo { Chiave = r.Chiave, Peso = Math.Round(r.Peso.Value) };
                string s = (r.Soglia.Text ?? "").Trim();
                bool vuota = s.Length == 0 || s == "-" || s.Equals("max", StringComparison.OrdinalIgnoreCase);
                if (r.Chiave == "z")
                {
                    string t = s.Replace("Z0", Fmt(model.Z0, 2)).Replace("z0", Fmt(model.Z0, 2)).Replace(" ", "").Replace(",", ".");
                    Match m = Regex.Match(t, @"^([-+]?\d*\.?\d+)(?:([-+])j(\d*\.?\d+))?$");
                    if (x.Peso > 0 && !m.Success) { err = "Impedenza voluta: scrivere per esempio 28 +j0"; return false; }
                    if (m.Success)
                        x.Zvoluta = new Complex(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                            m.Groups[3].Success ? (m.Groups[2].Value == "-" ? -1 : 1) * double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0);
                }
                else if (r.Chiave == "i")
                {
                    if (x.Peso > 0)
                    {
                        int tag, seg; string e2;
                        if (!PulseToSeg(s, out tag, out seg, out e2)) { err = "Corrente in un punto: " + e2; return false; }
                        x.Tag = tag; x.Seg = seg;
                    }
                }
                else if (!vuota)
                {
                    x.HaSoglia = true;
                    x.Soglia = OttNumero(s, 0);
                }
                if (r.Chiave == "el" && x.Peso > 0 && !x.HaSoglia) { err = "Elevazione: scrivere nella soglia l'elevazione voluta in gradi"; return false; }
                o.Obiettivi.Add(x);
            }
            foreach (OttRigaVar r in ottVar)
            {
                if (r.Formula || r.Usa.IsChecked != true) continue;
                double ora = OttLunghezza(r.Ora.Text, double.NaN), mn = OttLunghezza(r.Min.Text, double.NaN), mx = OttLunghezza(r.Max.Text, double.NaN), ps = OttLunghezza(r.Passo.Text, double.NaN);
                if (double.IsNaN(ora) || double.IsNaN(mn) || double.IsNaN(mx) || double.IsNaN(ps) || ps <= 0 || mx <= mn)
                { err = "Variabile " + r.Nome + ": controlla valore, minimo, massimo e passo (passo > 0, massimo > minimo)"; return false; }
                o.Variabili.Add(new OttVariabile { Nome = r.Nome, Valore = ora, Min = mn, Max = mx, Passo = ps });
            }
            o.Frequenze.AddRange(ottFreq);
            o.Principale = ottFreqPrinc;
            o.MantieniBoom = ottBoom;
            o.Simplesso = ottSimplesso;
            return true;
        }

        // ------------------------------------------------------------------------------------------------
        // comandi
        // ------------------------------------------------------------------------------------------------
        async void OttAvvia()
        {
            if (model == null) { Status("Apri prima un modello.", true); return; }
            if (ottInCorso) return;
            Ottimizzatore o = new Ottimizzatore();
            // si riparte dal passo ripreso, se c'e', altrimenti dal modello
            NecModel partenza = model.Clone();
            string err;
            if (!OttLeggi(o, out err)) { Status(err, true); return; }
            if (ottScelto != null && ott != null && ottScelto.Valori != null)
            {
                List<OttVariabile> attPrima = ott.Variabili.FindAll(q => q.Attiva && q.Max > q.Min);
                for (int i = 0; i < o.Variabili.Count; i++)
                {
                    int j = attPrima.FindIndex(q => string.Equals(q.Nome, o.Variabili[i].Nome, StringComparison.OrdinalIgnoreCase));
                    if (j >= 0 && j < ottScelto.Valori.Length) o.Variabili[i].Valore = ottScelto.Valori[j];
                }
            }
            o.Partenza = partenza;
            ott = o;
            ottScelto = null;
            ottSerieMigliore.Clear(); ottSeriePasso.Clear();
            ottCts = new CancellationTokenSource();
            CancellationToken tok = ottCts.Token;
            ottInCorso = true;
            ottAvvia.IsEnabled = false; ottFerma.IsEnabled = true;
            ottT0 = DateTime.Now;
            Registro.Scrivi("AZIONE", "ottimizzatore avviato: " + o.Variabili.Count + " variabili, " + o.Frequenze.Count + " frequenze, metodo " + (o.Simplesso ? "simplesso" : "una variabile alla volta"));
            o.PassoFatto += (p, migliore) => { lock (ottSeriePasso) { ottSeriePasso.Add(p.Valido ? p.Voto : double.NaN); ottSerieMigliore.Add(o.Migliore != null ? o.Migliore.Voto : (p.Valido ? p.Voto : double.NaN)); } };
            if (ottTimer == null)
            {
                ottTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                ottTimer.Tick += (s, e) => OttAggiorna(false);
            }
            ottTimer.Start();
            string esito = "finito";
            try
            {
                using (Prestazioni.Misura("Ottimizzatore", o.Variabili.Count + " variabili, " + o.Frequenze.Count + " frequenze"))
                    await Task.Run(() => o.Esegui(tok));
                if (tok.IsCancellationRequested) esito = "fermato";
            }
            catch (OperationCanceledException) { esito = "fermato"; }
            catch (Exception ex) { esito = "errore"; Status("Ottimizzatore: " + ex.Message, true); }
            ottTimer.Stop();
            ottInCorso = false;
            ottAvvia.IsEnabled = true; ottFerma.IsEnabled = false;
            ottScelto = o.Migliore;
            OttAggiorna(true, esito);
            if (o.Migliore != null && o.Iniziale != null)
                Registro.Scrivi("CALCOLO", string.Format(CultureInfo.InvariantCulture, "ottimizzatore {0}: {1} passi, voto {2:0.0} -> {3:0.0}", esito, o.Passi, o.Iniziale.Voto, o.Migliore.Voto));
            await OttDiagrammi();
        }

        void OttAggiorna(bool fine, string esito = null)
        {
            if (ott == null) return;
            lock (ottSeriePasso)
            {
                ottChart.X = new double[ottSeriePasso.Count];
                for (int i = 0; i < ottSeriePasso.Count; i++) ottChart.X[i] = i;
                ottChart.Series.Clear();
                ottChart.Series.Add(new LineSeries { Name = "voto del passo", Y = ottSeriePasso.ToArray(), Color = Pal.A(110, Pal.Accent), Width = 1, Decimals = 1 });
                ottChart.Series.Add(new LineSeries { Name = "voto migliore", Y = ottSerieMigliore.ToArray(), Color = Pal.Accent, Width = 2.2, Decimals = 1 });
            }
            ottChart.MarkX = ottScelto != null && ott.Migliore != null && ottScelto != ott.Migliore ? ottScelto.N : double.NaN;
            ottChart.InvalidateVisual();
            OttStatoSchede(esito);
            OttRegistro();
        }

        void OttStatoSchede(string esito = null)
        {
            if (ottCards == null) return;
            if (ott == null || ott.Iniziale == null)
            {
                OttScheda("stato", ottInCorso ? "in preparazione" : "pronto", ottInCorso ? "calcolo del modello di partenza" : "premi Avvia", ottInCorso ? Pal.Green : (Color?)null);
                foreach (string k in new[] { "voto", "g", "fb", "ros", "z" }) OttScheda(k, "-", " ");
                return;
            }
            TimeSpan t = DateTime.Now - ottT0;
            string tempo = t.TotalMinutes >= 1 ? string.Format(CultureInfo.CurrentCulture, "{0} min {1} s", (int)t.TotalMinutes, t.Seconds) : string.Format(CultureInfo.CurrentCulture, "{0} s", (int)t.TotalSeconds);
            OttScheda("stato", ottInCorso ? "in corso" : (esito ?? "fermo"), "passo " + ott.Passi + " - " + tempo, ottInCorso ? Pal.Green : (Color?)null);
            OttPasso b = ottScelto ?? ott.Migliore, p0 = ott.Iniziale;
            if (b == null) return;
            bool ripreso = ottScelto != null && ottScelto != ott.Migliore;
            ottCard["voto"][0].Text = ripreso ? "Voto del passo " + b.N : "Voto migliore";
            OttScheda("voto", Fmt(b.Voto, 1), "partenza " + Fmt(p0.Voto, 1));
            OttScheda("g", Fmt(b.G, 2) + " dBi", "partenza " + Fmt(p0.G, 2));
            OttScheda("fb", Fmt(b.FB, 1) + " dB", "partenza " + Fmt(p0.FB, 1));
            ottCard["ros"][0].Text = "ROS (" + Fmt(model != null ? model.Z0 : 50, 1) + " ohm)";
            double ros = Analysis.Swr(b.Z, model != null ? model.Z0 : 50);
            OttScheda("ros", Fmt(ros, 2), "peggiore in banda " + Fmt(b.RosPeggiore, 2), ros <= 1.5 ? Pal.Green : (Color?)null);
            OttRigaObiettivo zo = ottOb.Find(q => q.Chiave == "z");
            OttScheda("z", Fmt(b.Z.Real, 1) + " " + (b.Z.Imaginary < 0 ? "-" : "+") + "j" + Fmt(Math.Abs(b.Z.Imaginary), 1), zo != null && zo.Peso.Value > 0 ? "obiettivo " + zo.Soglia.Text : " ");
        }

        void OttRegistro()
        {
            if (ottRegPanel == null) return;
            ottRegPanel.Children.Clear();
            if (ott == null || ott.Iniziale == null) { ottRegPanel.Children.Add(Nota("Ancora nessun passo.")); return; }
            List<OttVariabile> att = ott.Variabili.FindAll(q => q.Attiva && q.Max > q.Min);
            int nv = Math.Min(att.Count, 6);
            string col = "60";
            for (int i = 0; i < nv; i++) col += ",*";
            col += ",62,52,52,52";
            Func<Grid> riga = () => new Grid { ColumnDefinitions = new ColumnDefinitions(col) };
            Grid h = riga();
            List<string> tt = new List<string> { "Passo" };
            for (int i = 0; i < nv; i++) tt.Add(att[i].Nome);
            tt.AddRange(new[] { "G dBi", "F/B", "ROS", "Voto" });
            for (int i = 0; i < tt.Count; i++) { TextBlock t = new TextBlock { Text = tt[i], FontSize = 12.5, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(4, 2) }; Grid.SetColumn(t, i); h.Children.Add(t); }
            ottRegPanel.Children.Add(h);
            List<OttPasso> lista;
            lock (ott.Registro) lista = new List<OttPasso>(ott.Registro);
            foreach (OttPasso p in lista)
            {
                Grid r = riga();
                bool migl = p == ott.Migliore, sc = p == ottScelto;
                List<string> v = new List<string> { p.N.ToString(CultureInfo.InvariantCulture) };
                for (int i = 0; i < nv; i++) v.Add(FormatVal(Math.Round(p.Valori[i], 6)));
                v.Add(Fmt(p.G, 2)); v.Add(Fmt(p.FB, 1)); v.Add(Fmt(Analysis.Swr(p.Z, model != null ? model.Z0 : 50), 2)); v.Add(Fmt(p.Voto, 1));
                for (int i = 0; i < v.Count; i++)
                {
                    TextBlock t = new TextBlock { Text = v[i], FontFamily = Pal.Mono, FontSize = 12.5, Margin = new Thickness(4, 3), Foreground = Pal.B(Pal.Fore), FontWeight = migl ? FontWeight.Bold : FontWeight.Normal };
                    Grid.SetColumn(t, i); r.Children.Add(t);
                }
                Border b = new Border { Child = r, Background = sc ? Pal.B(Pal.AccentSoft) : Brushes.Transparent, Cursor = new Cursor(StandardCursorType.Hand) };
                OttPasso pp = p;
                b.PointerPressed += async (s, e) =>
                {
                    ottScelto = pp;
                    Registro.Scrivi("AZIONE", "ottimizzatore: ripreso il passo " + pp.N);
                    OttAggiorna(false);
                    await OttDiagrammi();
                };
                ottRegPanel.Children.Add(b);
            }
            if (nv < att.Count) ottRegPanel.Children.Add(Nota("Nel registro le prime 6 variabili; tutte vengono applicate."));
        }

        async Task OttDiagrammi()
        {
            if (ott == null || ott.Iniziale == null) return;
            OttPasso b = ottScelto ?? ott.Migliore;
            double f = ott.Frequenze[ott.Principale];
            try
            {
                Ottimizzatore o = ott;
                RunResult r0 = await Task.Run(() => { NecModel m0 = o.Modello(o.Iniziale.Valori); return Analysis.Run(m0, m0.BuildGeometry(), f, 2.0); });
                RunResult r1 = await Task.Run(() => { NecModel m1 = o.Modello(b.Valori); return Analysis.Run(m1, m1.BuildGeometry(), f, 2.0); });
                ottRunPartenza = r0; ottRunScelto = r1;
            }
            catch (Exception ex) { Status("Diagramma dell'ottimizzatore: " + ex.Message, true); return; }
            OttGrafici();
        }

        void OttGrafici()
        {
            if (ottPolar == null) return;
            ottPolar.Traces.Clear();
            List<string> note = new List<string>();
            double rif = -999;
            if (ottRunPartenza != null && ottRunPartenza.AzCut != null)
            {
                ottPolar.Traces.Add(new PolarTrace { Db = ottRunPartenza.AzCut, Color = Pal.Muted, Dashed = true, Name = "partenza" });
                note.Add(string.Format(CultureInfo.CurrentCulture, "- - partenza: {0:0.00} dBi, F/B {1:0.0} dB", ottRunPartenza.GmaxDbi, Nz(ottRunPartenza.FrontBack)));
                rif = Math.Max(rif, ottRunPartenza.GmaxDbi);
            }
            if (ottRunScelto != null && ottRunScelto.AzCut != null && ott != null)
            {
                OttPasso b = ottScelto ?? ott.Migliore;
                ottPolar.Traces.Add(new PolarTrace { Db = ottRunScelto.AzCut, Color = Pal.Accent, Name = "migliore" });
                note.Add(string.Format(CultureInfo.CurrentCulture, "— passo {0}: {1:0.00} dBi, F/B {2:0.0} dB", b.N, ottRunScelto.GmaxDbi, Nz(ottRunScelto.FrontBack)));
                rif = Math.Max(rif, ottRunScelto.GmaxDbi);
            }
            ottPolar.RefDb = rif > -900 ? rif : 0;
            ottPolar.Notes = note.ToArray();
            ottPolar.InvalidateVisual();
        }

        void OttApplica()
        {
            if (ott == null || model == null) { Status("Nessun risultato dell'ottimizzatore da applicare.", true); return; }
            OttPasso b = ottScelto ?? ott.Migliore;
            if (b == null || b.Valori == null) return;
            List<OttVariabile> att = ott.Variabili.FindAll(q => q.Attiva && q.Max > q.Min);
            for (int i = 0; i < att.Count && i < b.Valori.Length; i++)
            {
                VarRow r = varRows.Find(q => string.Equals((q.Name.Text ?? "").Trim(), att[i].Nome, StringComparison.OrdinalIgnoreCase));
                if (r == null) continue;
                r.Expr.Text = OttTestoValore(r.Expr.Text, b.Valori[i]);
            }
            Registro.Scrivi("AZIONE", "ottimizzatore: applicato il passo " + b.N + " (voto " + b.Voto.ToString("0.0", CultureInfo.InvariantCulture) + ")");
            ottModelloVars = null;   // le variabili nuove diventano il valore ora
            ApplyVars();
            ottModelloVars = model;
            OttCaricaVariabili(true);
            Status("Valori del passo " + b.N + " copiati nella linguetta Variabili. Per tenere anche il modello di partenza salvalo prima con un altro nome.");
        }

        // mantiene la forma scritta dall'utente: 1018MM resta in millimetri, 1.018 resta in metri
        static string OttTestoValore(string vecchio, double v)
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            string ex = Expr.NormalizeDecimal((vecchio ?? "").Trim().Replace(" ", ""));
            Match mt = Regex.Match(ex, @"^([0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)(\*?)([A-Za-z_][A-Za-z0-9_]*)$");
            if (mt.Success && Expr.IsReserved(mt.Groups[3].Value))
            {
                double k = Expr.Eval(mt.Groups[3].Value, null);
                return (v / k).ToString("0.######", ci) + mt.Groups[2].Value + mt.Groups[3].Value;
            }
            return v.ToString("0.#########", ci);
        }

        void OttAnnulla()
        {
            if (ottCts != null) ottCts.Cancel();
            ott = null; ottScelto = null; ottRunScelto = null; ottRunPartenza = run;
            lock (ottSeriePasso) { ottSeriePasso.Clear(); ottSerieMigliore.Clear(); }
            ottChart.Series.Clear(); ottChart.X = new double[0]; ottChart.MarkX = double.NaN; ottChart.InvalidateVisual();
            OttRegistro(); OttGrafici(); OttStatoSchede();
            Status("Ottimizzazione annullata: il modello e' quello di partenza.");
        }
    }
}
