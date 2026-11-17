// YA-NEC 1.2.0 - fascia della linguetta Diagrammi (come la fascia delle famiglie dei Calcolatori): Diagrammi, Tabelle (bozzetto HF 11 A),
// Terreno reale (bozzetti HF 1 rev. 3 e VHF 1 rev. 2, stesso motore), Ricezione (RDF) (bozzetto HF 3), Tolleranze (bozzetto VHF 9).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        public static readonly string[] DiagVoci = { "Diagrammi", "Tabelle", "Terreno reale", "Ricezione (RDF)", "Tolleranze" };
        readonly Dictionary<string, Button> diagBtn = new Dictionary<string, Button>();
        readonly Dictionary<string, Control> diagPagine = new Dictionary<string, Control>();
        ContentControl diagCorpo;
        string diagVoce = "Diagrammi";

        Control DiagrammiConFascia(Control diagrammi)
        {
            diagPagine["Diagrammi"] = diagrammi;
            WrapPanel fp = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (string v in DiagVoci)
            {
                string nome = v;
                Button b = new Button { Content = nome, Tag = "diagrammi:" + nome, Padding = new Thickness(14, 4), Margin = new Thickness(2, 2), CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(1) };
                b.Click += (s, e) => ApriVoceDiagrammi(nome);
                diagBtn[nome] = b;
                fp.Children.Add(b);
            }
            Border fascia = new Border { Background = new SolidColorBrush(Color.Parse("#2F4A66")), BorderBrush = Pal.B(Pal.Accent), BorderThickness = new Thickness(0, 2, 0, 0), Padding = new Thickness(8, 4), Child = fp };
            diagCorpo = new ContentControl { Margin = new Thickness(0, 6, 0, 0) };
            DockPanel d = new DockPanel();
            DockPanel.SetDock(fascia, Dock.Top);
            d.Children.Add(fascia);
            d.Children.Add(diagCorpo);
            ApriVoceDiagrammi("Diagrammi");
            return d;
        }

        public void ApriVoceDiagrammi(string v)
        {
            diagVoce = v;
            foreach (KeyValuePair<string, Button> kv in diagBtn)
            {
                bool on = kv.Key == v;
                kv.Value.Background = new SolidColorBrush(on ? Color.Parse("#E08A00") : Color.Parse("#2F4A66"));
                kv.Value.BorderBrush = new SolidColorBrush(on ? Color.Parse("#E08A00") : Color.Parse("#4A6787"));
                kv.Value.Foreground = new SolidColorBrush(Color.Parse(on ? "#FFFFFF" : "#E6EDF5"));
                kv.Value.FontWeight = on ? FontWeight.SemiBold : FontWeight.Normal;
            }
            Control c;
            if (!diagPagine.TryGetValue(v, out c))
            {
                c = v == "Tabelle" ? BuildTabellePage() : v == "Ricezione (RDF)" ? BuildRicezioneRdfPage() : v == "Terreno reale" ? BuildTerrenoRealePage() : v == "Tolleranze" ? BuildTolleranzePage() : new TextBlock();
                diagPagine[v] = c;
            }
            if (diagCorpo != null) diagCorpo.Content = c;
            Registro.Scrivi("AZIONE", "diagrammi: " + v);
            AggiornaDiagrammi12();
        }

        /// <summary>Aggiorna la voce aperta della fascia dei Diagrammi dopo un calcolo o uno sweep.</summary>
        void AggiornaDiagrammi12()
        {
            try
            {
                if (diagVoce == "Tabelle") MostraTabelle();
                else if (diagVoce == "Ricezione (RDF)" && rdfAggiorna != null) rdfAggiorna(false);
                else if (diagVoce == "Terreno reale" && terrAggiorna != null) terrAggiorna();
                else if (diagVoce == "Tolleranze" && tollAggiorna != null) tollAggiorna();
            }
            catch (Exception ex) { Status("Diagrammi: " + ex.Message, true); }
        }

        // ------------------------------------------------------------------------------------------------
        // Tabelle: F/B e F/R su tutta la banda (bozzetto HF 11 A)
        // ------------------------------------------------------------------------------------------------
        LineChart tabFbFr;
        Grid tabSweep;
        TextBlock tabNota;

        Control BuildTabellePage()
        {
            tabFbFr = new LineChart { Height = 300, XLabel = "frequenza (MHz)", YLabel = "dB", EmptyText = "Fai uno sweep (pulsante Sweep in alto) per vedere F/B e F/R su tutta la banda" };
            tabSweep = new Grid();
            tabNota = Nota("F/R = guadagno avanti meno media della potenza sull'emisfero posteriore (90-270° di azimut, a tutte le elevazioni sopra 0°). Entra anche negli obiettivi dell'ottimizzatore.");
            StackPanel sp = new StackPanel { Margin = new Thickness(4) };
            StackPanel c1 = new StackPanel(); c1.Children.Add(tabFbFr); c1.Children.Add(tabNota);
            sp.Children.Add(Pannello("Tabelle: F/B e F/R su tutta la banda", c1));
            sp.Children.Add(Pannello("Valori dello sweep", tabSweep));
            MostraTabelle();
            return new ScrollViewer { Content = sp };
        }

        void MostraTabelle()
        {
            if (tabFbFr == null) return;
            List<SweepPoint> ok = sweep != null ? sweep.Where(p => p.Ok).ToList() : new List<SweepPoint>();
            tabFbFr.X = ok.Select(p => p.F).ToArray();
            tabFbFr.MarkX = run != null ? run.FreqMHz : double.NaN;
            tabFbFr.Series = new List<LineSeries>();
            if (ok.Count > 1)
            {
                tabFbFr.Series.Add(new LineSeries { Name = "F/B (un solo punto a 180°)", Y = ok.Select(p => p.FrontBack).ToArray(), Color = Color.Parse("#F08A24"), Width = 2, Decimals = 1 });
                tabFbFr.Series.Add(new LineSeries { Name = "F/R medio (emisfero posteriore)", Y = ok.Select(p => p.FrontRearMedio).ToArray(), Color = Color.Parse("#1D6FE0"), Width = 2.4, Decimals = 1 });
            }
            tabFbFr.InvalidateVisual();
            tabSweep.Children.Clear(); tabSweep.RowDefinitions.Clear();
            tabSweep.ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto");
            string[] hh = { "f (MHz)", "Z (Ω)", "ROS", "guadagno (dBi)", "F/B (dB)", "F/R peggiore (dB)", "F/R medio (dB)" };
            int r = 0;
            Action<string[], bool> riga = (v, h) =>
            {
                tabSweep.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int j = 0; j < v.Length; j++)
                {
                    Border b = new Border { Child = new TextBlock { Text = v[j], FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal, FontFamily = h ? FontFamily.Default : Pal.Mono }, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 1, 1) };
                    Grid.SetColumn(b, j); Grid.SetRow(b, r); tabSweep.Children.Add(b);
                }
                r++;
            };
            riga(hh, true);
            int passo = Math.Max(1, ok.Count / 60);
            for (int i = 0; i < ok.Count; i += passo)
            {
                SweepPoint p = ok[i];
                riga(new[] { FN(p.F, 4), FN(p.Z.Real, 1) + (p.Z.Imaginary >= 0 ? " + j " : " - j ") + FN(Math.Abs(p.Z.Imaginary), 1), FN(p.Swr, 2), FN(p.GainFwd, 2), FN(p.FrontBack, 1), FN(p.FrontRear, 1), double.IsNaN(p.FrontRearMedio) ? "-" : FN(p.FrontRearMedio, 1) }, false);
            }
            if (ok.Count == 0) riga(new[] { "-", "-", "-", "-", "-", "-", "-" }, false);
        }

        // ------------------------------------------------------------------------------------------------
        // Ricezione (RDF) (bozzetto HF 3)
        // ------------------------------------------------------------------------------------------------
        Action<bool> rdfAggiorna;

        Control BuildRicezioneRdfPage()
        {
            string ant = "beverage", rum = "iso";
            Fascione fAnt = new Fascione(); fAnt.Famiglia("ANTENNE RICEVENTI");
            string[][] antenne = { new[] { "beverage", "Beverage" }, new[] { "bog", "Beverage a terra (BOG)" }, new[] { "k9ay", "K9AY" }, new[] { "flag", "Flag / Pennant" }, new[] { "ewe", "EWE" },
                new[] { "4sq", "4-square in ricezione" }, new[] { "8c", "8 verticali in cerchio" }, new[] { "modello", "Modello aperto" } };
            foreach (string[] a in antenne) fAnt.Voce(a[0], a[1]);
            Fascione fRum = new Fascione(); fRum.Famiglia("RUMORE");
            fRum.Voce("iso", "Isotropo"); fRum.Voce("piano", "Dal piano (ITU-R P.372)"); fRum.Voce("locale", "Sorgente locale (azimut)");
            StackPanel sx = new StackPanel { Width = 222, Spacing = 2 };
            sx.Children.Add(fAnt); sx.Children.Add(fRum);
            sx.Children.Add(Nota("RDF = guadagno nella direzione voluta meno guadagno medio su tutto il semispazio: non dipende dalle perdite, a differenza del guadagno."));

            PolarPlot pol = new PolarPlot { ArrlScale = false, LinRange = 40, Height = 340, ShowHalfPower = true, CursorsOn = false, ShowNulls = false };
            TextBlock titPol = new TextBlock { FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) };
            TextBlock notaPol = new TextBlock { FontSize = 12.5, Foreground = Pal.B(Pal.Muted) };
            StackPanel cPol = new StackPanel { Spacing = 4 }; cPol.Children.Add(titPol); cPol.Children.Add(pol); cPol.Children.Add(notaPol);
            Border pPol = new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 0, 10), Child = cPol };
            TextBox tAz = Tb("0", 60), tEl = Tb("20", 50), tF = Tb("1,830", 70), tB = Tb("500", 60), tNf = Tb("15", 44), tPre = Tb("10", 44), tAzL = Tb("90", 50);
            ComboBox cAmb = new ComboBox { ItemsSource = RdfRicezione.Ambienti, SelectedIndex = 0 };
            ComboBox cTer = new ComboBox { Width = 190 };
            List<string> gl = new List<string>(); foreach (Tuple<string, double, double> g0 in Grounds) gl.Add(g0.Item1 + " " + FN(g0.Item2, 0) + " / " + FN(g0.Item3, 4));
            cTer.ItemsSource = gl; cTer.SelectedIndex = 0;
            CheckBox cRif = new CheckBox { Content = "Mostra anche il riferimento (verticale λ/4)", IsChecked = true };
            CheckBox cMed = new CheckBox { Content = "Mediana sugli angoli di arrivo (linguetta Terreno reale)" };
            ToolTip.SetTip(cMed, "Usa come elevazione voluta l'angolo di arrivo mediano della linguetta Terreno reale (statistiche per banda e regione). Senza le statistiche ARRL caricate resta l'elevazione scritta qui.");
            StackPanel dir = new StackPanel { Spacing = 2 };
            WrapPanel w1 = new WrapPanel(); w1.Children.Add(new TextBlock { Text = "Direzione voluta", FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center }); w1.Children.Add(Et("az")); w1.Children.Add(tAz); w1.Children.Add(Et("el")); w1.Children.Add(tEl); w1.Children.Add(Et("f")); w1.Children.Add(tF); w1.Children.Add(Et("MHz"));
            WrapPanel w2 = new WrapPanel(); w2.Children.Add(Et("Banda")); w2.Children.Add(tB); w2.Children.Add(Et("Hz  rumore")); w2.Children.Add(cAmb); w2.Children.Add(Et("NF ricevitore")); w2.Children.Add(tNf); w2.Children.Add(Et("dB  preamp")); w2.Children.Add(tPre); w2.Children.Add(Et("dB"));
            WrapPanel w3 = new WrapPanel(); w3.Children.Add(Et("Terreno")); w3.Children.Add(cTer); w3.Children.Add(Et("sorgente locale az")); w3.Children.Add(tAzL);
            dir.Children.Add(w1); dir.Children.Add(cRif); dir.Children.Add(cMed); dir.Children.Add(w2); dir.Children.Add(w3);
            Border pDir = new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Child = dir };
            StackPanel cen = new StackPanel(); cen.Children.Add(pPol); cen.Children.Add(pDir);

            StackPanel cifre = new StackPanel { Spacing = 2 }, basta = new StackPanel { Spacing = 2 };
            Grid r1 = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*") };
            Border pc1 = Pannello("Cifre di merito in ricezione", cifre), pc2 = Pannello("Basta il segnale?", basta);
            Grid.SetColumn(pc2, 2); r1.Children.Add(pc1); r1.Children.Add(pc2);
            LineChart gF = new LineChart { Height = 210, XLabel = "frequenza (MHz)", YLabel = "RDF", EmptyText = "calcolo in corso…" };
            Grid tab = new Grid();
            StackPanel dx = new StackPanel();
            dx.Children.Add(r1);
            dx.Children.Add(Pannello("RDF in funzione della frequenza", gF));
            dx.Children.Add(new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8), Child = tab });
            StackPanel avvisi = new StackPanel();
            dx.Children.Add(avvisi);

            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,10,0.9*,10,1.4*"), Margin = new Thickness(4) };
            g.Children.Add(sx);
            Grid.SetColumn(cen, 2); g.Children.Add(cen);
            Grid.SetColumn(dx, 4); g.Children.Add(dx);

            CancellationTokenSource cts = null;
            Func<string, string, string, bool, Border> card = (t, v, s, ev) =>
            {
                Border b = Riquadretto(t, v, s, ev); b.HorizontalAlignment = HorizontalAlignment.Stretch; b.MinWidth = 0;
                if (ev) foreach (Control c in ((StackPanel)b.Child).Children) if (c is TextBlock tb && tb.FontSize > 20) tb.Foreground = Pal.B(Pal.Accent);
                return b;
            };
            Func<string, double, double, GroundSpec, bool, string> deck = (k, f, az, gr, inv) =>
            {
                int banda = f < 2.5 ? 0 : f < 5 ? 1 : 2;
                RxDefault d0 = RxDefaults(k == "8c" || k == "4sq" || k == "k9ay" ? "beverage" : k, banda);
                RxParams p = new RxParams { F = f, Dir = inv ? az + 180 : az, Ground = gr, L = d0.L, H = d0.H, Rterm = d0.R, Rrod = d0.Rrod > 0 ? d0.Rrod : 20, H2 = d0.H2, D = d0.D, Vf = d0.Vf > 0 ? d0.Vf : 0.85, Angle = d0.Ang > 0 ? d0.Ang : 20 };
                switch (k)
                {
                    case "beverage": return RxAntennas.Beverage(p);
                    case "bog": return RxAntennas.Bog(p);
                    case "flag": return RxAntennas.Flag(p);
                    case "ewe": return RxAntennas.Ewe(p);
                    case "k9ay": p.Rterm = 390; p.Dir = az; return RdfRicezione.K9ay(p, inv);
                    case "4sq": p.Dir = az; return RdfRicezione.FourSquareRx(p, inv);
                    case "8c": p.Dir = az; return RdfRicezione.Cerchio8(p, inv);
                    default: p.Dir = 0; return RxAntennas.VerticaleRef(p);
                }
            };
            // risolve l'antenna k a f verso az; se il dietro esce piu' forte del davanti, la gira (sorgente e terminazione scambiate)
            Func<string, double, double, double, GroundSpec, CancellationToken, Nec2Engine> risolvi = (k, f, az, el, gr, ct) =>
            {
                Nec2Engine best = null;
                foreach (bool inv in new[] { false, true })
                {
                    ct.ThrowIfCancellationRequested();
                    NecModel m = NecModel.Parse(deck(k, f, az, gr, inv));
                    RunResult rr = Analysis.Run(m, m.BuildGeometry(), f, 10, ct);
                    double av = rr.Engine.Gain(90 - el, az).GainTotal, di = rr.Engine.Gain(90 - el, (az + 180) % 360).GainTotal;
                    if (k == "vert" || av >= di) return rr.Engine;
                    best = rr.Engine;
                }
                return best;
            };

            rdfAggiorna = async (forza) =>
            {
                cts?.Cancel(); cts = new CancellationTokenSource(); CancellationToken ct = cts.Token;
                double az = ParseNum(tAz.Text, 0), el = ParseNum(tEl.Text, 20), f = Math.Max(0.1, ParseNum(tF.Text, 1.83));
                if (cMed.IsChecked == true && !double.IsNaN(terrMediana)) el = terrMediana;
                int amb = Math.Max(0, cAmb.SelectedIndex), rumore = rum == "piano" ? 1 : rum == "locale" ? 2 : 0;
                double bw = ParseNum(tB.Text, 500), nf = ParseNum(tNf.Text, 15), pre = ParseNum(tPre.Text, 10), azL = ParseNum(tAzL.Text, 90);
                int gi = Math.Max(0, cTer.SelectedIndex);
                GroundSpec gr = new GroundSpec { Kind = GroundKind.Real, Epsr = Grounds[gi].Item2, Sigma = Grounds[gi].Item3 };
                string k = ant;
                if (k == "modello" && run == null) { titPol.Text = "Calcola prima il modello aperto"; return; }
                if (k == "modello") f = run.FreqMHz;
                string nomeAnt = Array.Find(antenne, a => a[0] == k)[1];
                titPol.Text = "Azimut a " + FN(el, 0) + "° · " + FN(f, 3) + " MHz";
                notaPol.Text = "calcolo in corso…";
                try
                {
                    var res = await Task.Run(() =>
                    {
                        Nec2Engine e = k == "modello" ? run.Engine : risolvi(k, f, az, el, gr, ct);
                        RdfRicezione.Esito es = RdfRicezione.Calcola(e, f, az, el, rumore, azL, bw, amb, nf, pre);
                        Nec2Engine ev = risolvi("vert", f, 0, el, gr, ct);
                        RdfRicezione.Esito er = RdfRicezione.Calcola(ev, f, 0, el, 0, 0, bw, amb, nf, pre);
                        return Tuple.Create(es, er);
                    }, ct);
                    if (ct.IsCancellationRequested) return;
                    RdfRicezione.Esito r = res.Item1, rv = res.Item2;
                    pol.Traces.Clear();
                    double[] a = r.Az.Select(x => x - r.AzMax).ToArray();
                    pol.RefDb = 0;
                    pol.Traces.Add(new PolarTrace { Db = a, Color = Color.Parse("#1D6FE0"), Width = 2.4, Name = nomeAnt });
                    if (cRif.IsChecked == true) pol.Traces.Add(new PolarTrace { Db = rv.Az.Select(x => x - rv.AzMax).ToArray(), Color = Pal.Muted, Width = 1.6, Dashed = true, Name = "verticale λ/4" });
                    pol.MarkAngle = az;
                    pol.Title = "";
                    pol.InvalidateVisual();
                    notaPol.Text = "Elevazione " + FN(el, 0) + "° · dB relativi · arancio: -3 dB";
                    cifre.Children.Clear(); basta.Children.Clear(); avvisi.Children.Clear();
                    cifre.Children.Add(card("RDF", FN(r.Rdf, 1) + " dB", (r.Rdf - rv.Rdf >= 0 ? "+" : "") + FN(r.Rdf - rv.Rdf, 1) + " dB sulla verticale λ/4", true));
                    Border cd = card("DMF (direttività media sul fronte)", FN(r.Dmf, 1) + " dB", "settore ±30° az, 10–40° el", false);
                    FumettoGiallo(cd, "Media del guadagno nel settore avanti meno la media sull'emisfero posteriore.", "ON4UN definisce il DMF con il solo guadagno a 20 gradi; qui si usa la media sul settore del bozzetto, quindi i valori non sono confrontabili uno a uno con quelli del libro.", "capitolo 7 (Ricezione RDF)");
                    cifre.Children.Add(cd);
                    cifre.Children.Add(card("Guadagno", FN(r.G, 1) + " dBi", r.G < -30 ? "molto basso: serve un preamplificatore" : r.G < -10 ? "basso ma accettabile: vedi soglia" : "come un'antenna trasmittente", false));
                    basta.Children.Add(card("F/B · F/R", FN(r.FB, 0) + " / " + FN(r.FRmedio, 0) + " dB", "F/R medio sull'emisfero posteriore", false));
                    Border cn = card("Rumore d'antenna ai morsetti", FN(r.PnDbm, 0) + " dBm", FN(bw, 0) + " Hz, rumore " + RdfRicezione.Ambienti[amb] + (rumore == 0 ? "" : "; RDF con questo rumore " + FN(r.RdfRumore, 1) + " dB"), false);
                    FumettoGiallo(cn, "Potenza di rumore esterno ai morsetti: -174 dBm/Hz + Fa (ITU-R P.372) + 10 log B + guadagno medio pesato sulla provenienza del rumore.",
                        rumore == 1 ? "rumore dal piano con peso exp(-el/10 gradi) sull'elevazione: rappresenta il rumore artificiale lontano che arriva basso; la distribuzione vera dipende dal sito." :
                        rumore == 2 ? "sorgente puntiforme a 5 gradi di elevazione con la stessa potenza del fondo isotropo." : "rumore isotropo sopra il suolo; il suolo non aggiunge rumore.", "capitolo 7 (Ricezione RDF)");
                    basta.Children.Add(cn);
                    basta.Children.Add(card("Margine sul ricevitore", (r.Margine >= 0 ? "+" : "") + FN(r.Margine, 0) + " dB", "con preamp " + FN(pre, 0) + " dB; sotto 6 dB: avviso", false));
                    if (r.Margine < 6) avvisi.Children.Add(AvvisoGiallo("Il rumore d'antenna supera il rumore del ricevitore di meno di 6 dB: il ricevitore limita la sensibilita'. Serve piu' preamplificazione o un ricevitore con NF piu' bassa."));
                    if (k == "k9ay" || k == "4sq" || k == "8c")
                        avvisi.Children.Add(AvvisoGiallo("Modello semplificato di " + nomeAnt + (k == "k9ay" ? " (un loop a delta chiuso in basso, 390 ohm)" : " (verticali corte alimentate con la fase del nullo posteriore, amplificatori ideali)") + ": l'RDF e' indicativo, in tabella i valori di letteratura."));
                    // tabella di confronto
                    tab.Children.Clear(); tab.RowDefinitions.Clear();
                    tab.ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto,Auto");
                    int rr = 0;
                    Action<string[], bool, bool> riga = (v, h, gras) =>
                    {
                        tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                        for (int j = 0; j < v.Length; j++)
                        {
                            Border b = new Border { Child = new TextBlock { Text = v[j], FontWeight = h || gras ? FontWeight.SemiBold : FontWeight.Normal }, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 1, 1) };
                            Grid.SetColumn(b, j); Grid.SetRow(b, rr); tab.Children.Add(b);
                        }
                        rr++;
                    };
                    riga(new[] { "Antenna", "RDF 1,8", "RDF 3,5", "Guadagno 1,8", "Spazio richiesto" }, true, false);
                    foreach (string[] lt in RdfRicezione.Letteratura) riga(lt, false, false);
                    riga(new[] { nomeAnt + " (questo calcolo, " + FN(f, 3) + " MHz)", FN(r.Rdf, 1), "", FN(r.G, 1) + " dBi", "" }, false, true);
                    // RDF in frequenza (in fondo, piu' lento)
                    double[] fs = f < 8 ? new[] { 1.83, 2.5, 3.6, 5.0, 7.05 } : new[] { f * 0.7, f * 0.85, f, f * 1.15, f * 1.3 };
                    gF.X = new double[0]; gF.Series = new List<LineSeries>(); gF.EmptyText = "calcolo in corso…"; gF.InvalidateVisual();
                    var curve = await Task.Run(() =>
                    {
                        List<double[]> l = new List<double[]>();
                        foreach (string kk in new[] { k, "k9ay", "vert" })
                        {
                            double[] y = new double[fs.Length];
                            for (int i = 0; i < fs.Length; i++)
                            {
                                if (kk == "modello") { y[i] = double.NaN; continue; }
                                Nec2Engine e = risolvi(kk, fs[i], kk == "vert" ? 0 : az, el, gr, ct);
                                y[i] = RdfRicezione.Calcola(e, fs[i], kk == "vert" ? 0 : az, el, 0, 0, bw, amb, nf, pre).Rdf;
                            }
                            l.Add(y);
                        }
                        return l;
                    }, ct);
                    if (ct.IsCancellationRequested) return;
                    gF.X = fs;
                    gF.Series = new List<LineSeries>
                    {
                        new LineSeries { Name = nomeAnt, Y = curve[0], Color = Color.Parse("#1D6FE0"), Width = 2.4, Decimals = 1 },
                        new LineSeries { Name = "K9AY", Y = curve[1], Color = Color.Parse("#21A04A"), Width = 2, Decimals = 1 },
                        new LineSeries { Name = "verticale λ/4", Y = curve[2], Color = Pal.Muted, Width = 1.6, Dashed = true, Decimals = 1 },
                    };
                    if (k == "k9ay") gF.Series.RemoveAt(1);
                    gF.InvalidateVisual();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { notaPol.Text = "Errore: " + ex.Message; }
            };
            fAnt.Selected += k => { ant = k; rdfAggiorna(true); };
            fRum.Selected += k => { rum = k; rdfAggiorna(true); };
            foreach (TextBox t in new[] { tAz, tEl, tF, tB, tNf, tPre, tAzL }) t.LostFocus += (s, e) => rdfAggiorna(true);
            foreach (ComboBox c in new[] { cAmb, cTer }) c.SelectionChanged += (s, e) => rdfAggiorna(true);
            cRif.IsCheckedChanged += (s, e) => rdfAggiorna(true);
            cMed.IsCheckedChanged += (s, e) => rdfAggiorna(true);
            fAnt.Select("beverage", false); fRum.Select("iso", false);
            rdfAggiorna(true);
            return new ScrollViewer { Content = g, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }
    }
}
