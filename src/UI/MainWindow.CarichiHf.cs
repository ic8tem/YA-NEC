// YA-NEC 1.2.0 - Calcolatori > HF > "Carichi HF" (bozzetto 7 approvato da IC8TEM il 17/11/2026, applicato alla lettera):
// a sinistra i tipi costruttivi a pillole e "Inserisci nel modello"; al centro i dati, il grafico R e X in frequenza con la banda da bloccare
// e la tabella per banda (Z, comportamento, tensione alla potenza scelta, potenza persa); a destra i risultati e gli avvisi gialli.
// Calcolo in Engine/CarichiHf.cs.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
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
        /// <summary>1.2.0: fumetto giallo delle approssimazioni (regola delle specifiche: ogni approssimazione ha il suo fumetto giallo).</summary>
        internal static void FumettoGiallo(Control c, string normale, string approssimazione, string capitolo = null)
        {
            StackPanel sp = new StackPanel { Spacing = 4, MaxWidth = 520 };
            if (!string.IsNullOrEmpty(normale)) sp.Children.Add(new TextBlock { Text = normale, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 40)) });
            sp.Children.Add(new TextBlock { Text = "⚠ APPROSSIMAZIONE: " + approssimazione, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(90, 60, 0)) });
            if (!string.IsNullOrEmpty(capitolo)) sp.Children.Add(new TextBlock { Text = "Spiegazione nel manuale, " + capitolo + ".", TextWrapping = TextWrapping.Wrap, FontSize = 11.5, Foreground = new SolidColorBrush(Color.FromRgb(90, 90, 90)) });
            ToolTip.SetTip(c, new Border { Background = new SolidColorBrush(Color.FromRgb(255, 236, 153)), BorderBrush = new SolidColorBrush(Color.FromRgb(214, 168, 0)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 6), Child = sp });
        }

        /// <summary>Riquadro giallo con il triangolo (avvisi dei bozzetti della 1.2).</summary>
        internal static Border AvvisoGiallo(string testo)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 243, 196)), BorderBrush = new SolidColorBrush(Color.FromRgb(214, 168, 0)), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 6), Margin = new Thickness(0, 6, 0, 0),
                Child = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(90, 60, 0)), FontSize = 12.5, Text = "⚠ " + testo }
            };
        }

        static TextBlock Et(string s) { return new TextBlock { Text = s, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 3, 0), Foreground = Pal.B(Pal.Fore) }; }

        Control BuildCarichiHfPanel()
        {
            CarichiHf.Dati d = new CarichiHf.Dati();
            List<Cable> cavi = new List<Cable>();
            foreach (Cable c in CalcCavi.Lista ?? new List<Cable>()) if (c.OdMm > 0) cavi.Add(c);
            d.Cavo = cavi.Find(c => c.Name.StartsWith("RG-58", StringComparison.Ordinal)) ?? (cavi.Count > 0 ? cavi[0] : null);
            string inserisci = "carico";

            // ---------------- sinistra: pillole ----------------
            Fascione fTipi = new Fascione();
            fTipi.Famiglia("CARICHI HF");
            string[] chiavi = { "lc", "coax", "bobina", "linear", "cappello", "misure" };
            for (int i = 0; i < chiavi.Length; i++) fTipi.Voce(chiavi[i], CarichiHf.Tipi[i]);
            Fascione fIns = new Fascione();
            fIns.Famiglia("INSERISCI NEL MODELLO");
            fIns.Voce("carico", "Come carico (Z in frequenza)");
            fIns.Voce("fili", "Come fili (linear loading, cappelli)");
            TextBox tDove = Tb("w1c", 80);
            ToolTip.SetTip(tDove, "Dove va il carico: w + numero del filo + b (inizio), c (centro), e (fine), oppure w4:7 (segmento 7 del filo 4). " +
                "Il cappello va all'estremo del filo (b o e), il linear loading nel punto scelto del filo.");
            Button bIns = Btn("Inserisci nel modello", true);
            StackPanel sx = new StackPanel { Width = 222, Spacing = 2 };
            sx.Children.Add(fTipi);
            sx.Children.Add(fIns);
            StackPanel rDove = new StackPanel { Orientation = Orientation.Horizontal };
            rDove.Children.Add(Et("Dove")); rDove.Children.Add(tDove);
            sx.Children.Add(rDove);
            sx.Children.Add(bIns);
            sx.Children.Add(Nota("Gia' approvati nel bozzetto Carichi: Q di L e C, f0, Z per banda, tensione e corrente sul componente. Qui si aggiungono i tipi costruttivi."));

            // ---------------- centro ----------------
            StackPanel datiCorpo = new StackPanel { Spacing = 4 };
            Border pDati = Pannello("Trappola coassiale", datiCorpo);
            TextBlock titDati = (TextBlock)((StackPanel)((StackPanel)pDati.Child).Children[0]).Children[0];
            LineChart grafico = new LineChart { Height = 270, XLabel = "frequenza", YLabel = "Ω", EmptyText = "" };
            Border pGraf = Pannello("Impedenza della trappola (Q in frequenza, perdite per effetto pelle)", grafico);
            TextBlock titGraf = (TextBlock)((StackPanel)((StackPanel)pGraf.Child).Children[0]).Children[0];
            Grid tab = new Grid();
            StackPanel cen = new StackPanel();
            cen.Children.Add(pDati); cen.Children.Add(pGraf);
            cen.Children.Add(new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8), Child = tab
            });

            // ---------------- destra ----------------
            StackPanel ris = new StackPanel { Spacing = 2 };
            Border pRis = Pannello("Risultati", ris);

            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,10,1.5*,10,*"), Margin = new Thickness(4) };
            g.Children.Add(sx);
            Grid.SetColumn(cen, 2); g.Children.Add(cen);
            Grid.SetColumn(pRis, 4); pRis.VerticalAlignment = VerticalAlignment.Top; g.Children.Add(pRis);

            // campi
            ComboBox cBlocca = new ComboBox(), cSotto = new ComboBox();
            List<string> hf = new List<string>();
            foreach (Band b in Bands.All) if (b.Operating > 1.5 && b.Operating < 30) hf.Add(b.Name);
            cBlocca.ItemsSource = hf; cSotto.ItemsSource = hf;
            cBlocca.SelectedItem = d.Blocca; cSotto.SelectedItem = d.Sotto;
            ComboBox cCavo = new ComboBox { ItemsSource = cavi, SelectedItem = d.Cavo, MinWidth = 110 };
            ComboBox cPasso = new ComboBox { ItemsSource = CarichiHf.Passi, SelectedIndex = 0 };
            ComboBox cColl = new ComboBox { ItemsSource = CarichiHf.Collegamenti, SelectedIndex = 0 };
            ToolTip.SetTip(cColl, "Collegamento standard della trappola coassiale: il centrale all'inizio dell'avvolgimento si salda alla calza alla fine; " +
                "i terminali della trappola sono la calza all'inizio e il centrale alla fine. La capacita' e' quella tra centrale e calza per tutta la lunghezza avvolta. " +
                "W8NX bassa impedenza (ARRL Antenna Book 24a ed., par. 10.2): l'uscita si prende sulle sole spire del centrale, meta' delle spire, e la trappola vale circa 1/4 dell'impedenza con la stessa risonanza; " +
                "il filo interno va saldato all'inizio dell'avvolgimento del centrale (invertendo i capi la risonanza scende di circa 50 kHz).");
            TextBox tForma = Tb("40", 56), tSpire = Tb("8,6", 56);
            TextBox tL = Tb("8,2", 60), tC = Tb("60", 60), tQL = Tb("200", 50), tQC = Tb("2000", 56);
            TextBox tDiam = Tb("50", 50), tFilo = Tb("2", 44), tPassoMm = Tb("4", 44), tSpB = Tb("20", 50), tF = Tb("3,650", 64), tPos = Tb("5", 50), tFiloEl = Tb("2", 44);
            TextBox tBraccio = Tb("12", 50), tPiega = Tb("2", 50), tDist = Tb("100", 50), tFiloLl = Tb("2", 44);
            TextBox tRaggi = Tb("4", 40), tRaggio = Tb("1", 50), tFiloCap = Tb("2", 44), tAlt = Tb("10", 50);
            CheckBox cAnello = new CheckBox { Content = "anello", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            TextBox tMisL = Tb("10", 56), tMisQ = Tb("250", 50), tMisF = Tb("3,650", 64), tMisF0 = Tb("40", 50);
            TextBox tPot = Tb("1", 34);
            Button bSpire = Btn("Calcola spire per f0", true);
            FumettoGiallo(tSpire, "Spire dell'avvolgimento (anche frazionarie).", "induttanza di Wheeler sul diametro medio (forma + cavo) e capacita' del cavo per la lunghezza avvolta: " +
                "la f0 reale si scosta di qualche per cento, misurala con il grid-dip o il VNA e correggi le spire.", "capitolo 15 (Carichi HF)");

            Func<string, Control[], WrapPanel> rigaW = null;
            rigaW = (s, cs) => { WrapPanel w = new WrapPanel(); foreach (Control c in cs) w.Children.Add(c); return w; };

            Action ricalcola = null;
            Action costruisciDati = () =>
            {
                datiCorpo.Children.Clear();
                switch (d.Tipo)
                {
                    case 0:
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("L"), tL, Et("µH  Q"), tQL, Et("C"), tC, Et("pF  Q"), tQC }));
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("Blocca"), cBlocca, Et("dipolo con"), cSotto, Et("Filo Ø"), tFiloEl, Et("mm"), bSpire }));
                        bSpire.Content = "Calcola C per f0";
                        break;
                    case 1:
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("Forma"), tForma, Et("mm"), Et("Cavo"), cCavo, Et("Spire"), tSpire, Et("Passo"), cPasso, Et("Collegamento") }));
                        datiCorpo.Children.Add(rigaW(null, new Control[] { cColl, Et("Blocca"), cBlocca, Et("dipolo con"), cSotto, Et("Filo Ø"), tFiloEl, Et("mm"), bSpire }));
                        break;
                    case 2:
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("Diametro"), tDiam, Et("mm  Filo Ø"), tFilo, Et("mm  Passo"), tPassoMm, Et("mm  Spire"), tSpB }));
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("Frequenza"), tF, Et("MHz  dal centro"), tPos, Et("m  Braccio"), tBraccio, Et("m  Filo dell'elemento Ø"), tFiloEl, Et("mm"), bSpire }));
                        bSpire.Content = "Calcola spire per la risonanza";
                        break;
                    case 3:
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("Piega"), tPiega, Et("m  Distanza tra i fili"), tDist, Et("mm  Filo Ø"), tFiloLl, Et("mm") }));
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("Frequenza"), tF, Et("MHz  dal centro"), tPos, Et("m  Braccio"), tBraccio, Et("m  Filo dell'elemento Ø"), tFiloEl, Et("mm"), bSpire }));
                        bSpire.Content = "Calcola la piega per la risonanza";
                        break;
                    case 4:
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("Raggi"), tRaggi, Et("lunghi"), tRaggio, Et("m  Filo Ø"), tFiloCap, Et("mm"), cAnello }));
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("Verticale alto"), tAlt, Et("m  Frequenza"), tF, Et("MHz  Filo Ø"), tFiloEl, Et("mm"), bSpire }));
                        bSpire.Content = "Calcola i raggi per la risonanza";
                        break;
                    default:
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("L misurata"), tMisL, Et("µH  Q"), tMisQ, Et("a"), tMisF, Et("MHz  Autorisonanza f0"), tMisF0, Et("MHz") }));
                        datiCorpo.Children.Add(rigaW(null, new Control[] { Et("Frequenza"), tF, Et("MHz  dal centro"), tPos, Et("m  Filo dell'elemento Ø"), tFiloEl, Et("mm") }));
                        break;
                }
                if (d.Tipo == 1) bSpire.Content = "Calcola spire per f0 = " + CarichiHf.Fmt((CarichiHf.Banda(d.Blocca) ?? CarichiHf.Banda("40 m")).Operating, 3);
                bSpire.IsVisible = d.Tipo != 5;
            };

            Action leggi = () =>
            {
                d.Blocca = cBlocca.SelectedItem as string ?? d.Blocca; d.Sotto = cSotto.SelectedItem as string ?? d.Sotto;
                d.Cavo = cCavo.SelectedItem as Cable ?? d.Cavo; d.Passo = Math.Max(0, cPasso.SelectedIndex); d.Collegamento = Math.Max(0, cColl.SelectedIndex);
                d.FormaMm = ParseNum(tForma.Text, d.FormaMm); d.Spire = Math.Max(0.5, ParseNum(tSpire.Text, d.Spire));
                d.LuH = Math.Max(1e-3, ParseNum(tL.Text, d.LuH)); d.CpF = Math.Max(0.1, ParseNum(tC.Text, d.CpF));
                d.QL = ParseNum(tQL.Text, d.QL); d.QC = ParseNum(tQC.Text, d.QC);
                if (d.QL <= 0) d.QL = double.PositiveInfinity; if (d.QC <= 0) d.QC = double.PositiveInfinity;
                d.DiamBobinaMm = Math.Max(1, ParseNum(tDiam.Text, d.DiamBobinaMm)); d.FiloMm = Math.Max(0.05, ParseNum(tFilo.Text, d.FiloMm));
                d.PassoMm = ParseNum(tPassoMm.Text, d.PassoMm); d.SpireBobina = Math.Max(0.5, ParseNum(tSpB.Text, d.SpireBobina));
                d.FLavoroMHz = Math.Max(0.1, ParseNum(tF.Text, d.FLavoroMHz)); d.BraccioM = Math.Max(0.1, ParseNum(tBraccio.Text, d.BraccioM)); d.PosizioneM = ParseNum(tPos.Text, d.PosizioneM);
                d.FiloElementoMm = Math.Max(0.1, ParseNum(tFiloEl.Text, d.FiloElementoMm));
                d.PiegaM = Math.Max(0.01, ParseNum(tPiega.Text, d.PiegaM)); d.DistanzaMm = Math.Max(1, ParseNum(tDist.Text, d.DistanzaMm)); d.FiloLlMm = Math.Max(0.1, ParseNum(tFiloLl.Text, d.FiloLlMm));
                d.Raggi = Math.Max(2, (int)Math.Round(ParseNum(tRaggi.Text, d.Raggi))); d.RaggioM = Math.Max(0.05, ParseNum(tRaggio.Text, d.RaggioM));
                d.FiloCapMm = Math.Max(0.1, ParseNum(tFiloCap.Text, d.FiloCapMm)); d.AltezzaM = Math.Max(0.5, ParseNum(tAlt.Text, d.AltezzaM)); d.Anello = cAnello.IsChecked == true;
                d.MisLuH = Math.Max(1e-3, ParseNum(tMisL.Text, d.MisLuH)); d.MisQ = Math.Max(1, ParseNum(tMisQ.Text, d.MisQ));
                d.MisFMHz = Math.Max(0.01, ParseNum(tMisF.Text, d.MisFMHz)); d.MisF0MHz = Math.Max(0.01, ParseNum(tMisF0.Text, d.MisF0MHz));
                d.PotenzaW = Math.Max(0.001, ParseNum(tPot.Text, 1)) * 1000;
            };

            CarichiHf.Ris r = null;
            Func<string, string, string, bool, Border> card = (t, v, s, ev) =>
            {
                Border b = Riquadretto(t, v, s, ev);
                b.HorizontalAlignment = HorizontalAlignment.Stretch; b.MinWidth = 0;
                if (ev) foreach (Control c in ((StackPanel)b.Child).Children) if (c is TextBlock tb && tb.FontSize > 20) tb.Foreground = Pal.B(Pal.Accent);
                return b;
            };
            Func<Complex, string> zs = z => CarichiHf.Fmt(z.Real, z.Real >= 100 ? 0 : 1) + (z.Imaginary >= 0 ? " + j " : " - j ") + CarichiHf.Fmt(Math.Abs(z.Imaginary), Math.Abs(z.Imaginary) >= 100 ? 0 : 1);

            ricalcola = () =>
            {
                leggi();
                try { r = CarichiHf.Calcola(d); }
                catch (Exception ex) { Status("Carichi HF: " + ex.Message, true); return; }
                bool trap = d.Tipo <= 1;
                Band bb = CarichiHf.Banda(d.Blocca), bs = CarichiHf.Banda(d.Sotto);
                string bn = d.Blocca.Replace(" m", ""), sn = d.Sotto.Replace(" m", "");
                titDati.Text = d.Tipo == 0 ? "Trappola LC per " + d.Blocca + " (dipolo " + bn + "/" + sn + ")"
                    : d.Tipo == 1 ? "Trappola coassiale per " + d.Blocca + " (dipolo " + bn + "/" + sn + ")"
                    : d.Tipo == 2 ? "Bobina di carico a " + CarichiHf.Fmt(d.FLavoroMHz, 3) + " MHz"
                    : d.Tipo == 3 ? "Linear loading a " + CarichiHf.Fmt(d.FLavoroMHz, 3) + " MHz"
                    : d.Tipo == 4 ? "Cappello capacitivo in punta al verticale" : "Bobina dalle misure";
                titGraf.Text = trap ? "Impedenza della trappola (Q in frequenza, perdite per effetto pelle)"
                    : d.Tipo == 4 ? "Reattanza del cappello in frequenza" : "Impedenza del carico (con l'autorisonanza e le perdite per effetto pelle)";
                // grafico
                double fc = trap ? r.F0 : d.FLavoroMHz * 1e6;
                double fa = trap ? fc * 0.71 : fc * 0.5, fz = trap ? fc * 1.42 : fc * 2;
                if (trap) { fa = Math.Floor(fa / 1e6) * 1e6; fz = Math.Ceiling(fz / 1e6) * 1e6; }
                int n = 400;
                double[] xs = new double[n], rr = new double[n], xx = new double[n];
                double clip = trap ? 6000 : 0;
                for (int i = 0; i < n; i++)
                {
                    double f = fa + (fz - fa) * i / (n - 1);
                    Complex z = r.Z(f);
                    xs[i] = f / 1e6; rr[i] = z.Real; xx[i] = z.Imaginary;
                    if (clip > 0) { rr[i] = Math.Max(-clip, Math.Min(clip, rr[i])); xx[i] = Math.Max(-clip, Math.Min(clip, xx[i])); }
                }
                grafico.X = xs;
                grafico.Series = new List<LineSeries>
                {
                    new LineSeries { Name = "X", Y = xx, Color = Color.Parse("#F08A24"), Decimals = 0 },
                    new LineSeries { Name = "R", Y = rr, Color = Color.Parse("#1D6FE0"), Decimals = 0 },
                };
                if (d.Tipo == 4) grafico.Series.RemoveAt(1);
                grafico.YMinFixed = clip > 0 ? -clip : double.NaN; grafico.YMaxFixed = clip > 0 ? clip : double.NaN;
                grafico.MarkX = trap ? (bb != null ? bb.Operating : double.NaN) : d.FLavoroMHz;
                grafico.MarkXLabel = trap ? CarichiHf.Fmt(grafico.MarkX, 3) + " MHz (banda da bloccare)" : CarichiHf.Fmt(d.FLavoroMHz, 3) + " MHz (lavoro)";
                grafico.MarkXColor = Color.Parse("#21A04A");
                grafico.YLabel = trap ? "impedenza della trappola" : "Ω";
                grafico.InvalidateVisual();
                // tabella
                tab.Children.Clear(); tab.RowDefinitions.Clear();
                tab.ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto,Auto");
                string[] hh = { "Banda", "f (MHz)", trap ? "Z trappola (Ω)" : "Z carico (Ω)", "Si comporta da", "V", "Potenza persa" };
                tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int j = 0; j < hh.Length; j++)
                {
                    Control hc;
                    if (j == 4)
                    {
                        StackPanel hp = new StackPanel { Orientation = Orientation.Horizontal };
                        hp.Children.Add(new TextBlock { Text = "V a ", FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
                        tPot.Margin = new Thickness(0); tPot.Padding = new Thickness(2, 0); tPot.MinHeight = 22; tPot.FontSize = 12.5;
                        if (tPot.Parent is Panel pp) pp.Children.Remove(tPot);
                        hp.Children.Add(tPot);
                        hp.Children.Add(new TextBlock { Text = " kW", FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
                        hc = hp;
                    }
                    else hc = new TextBlock { Text = hh[j], FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore) };
                    Border hb = new Border { Child = hc, Background = Pal.B(Pal.Panel2), Padding = new Thickness(6, 4), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 1, 1) };
                    Grid.SetColumn(hb, j); tab.Children.Add(hb);
                }
                FumettoGiallo(tPot, "Potenza del trasmettitore per le tensioni e le perdite della tabella.", trap
                    ? "banda bassa: corrente in ventre sqrt(P/73) e alla trappola I0 cos(beta a); banda bloccata: tensione alla punta del dipolo interno I0 Z0 divisa tra la trappola e la radiazione."
                    : "corrente in ventre sqrt(P/73) e al carico I0 cos(beta a), con a la distanza dal centro.", "capitolo 15 (Carichi HF)");
                int riga = 1;
                foreach (CarichiHf.RigaBanda rb in r.Righe)
                {
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    bool vRossa = false, pRossa = false;
                    double vl = d.Tipo == 1 ? CarichiHf.TensioneLavoro(d.Cavo != null ? d.Cavo.Name : "") : 0;
                    if (vl > 0 && rb.V > vl) vRossa = true;
                    if (rb.Perdita > 0.02 * d.PotenzaW) pRossa = true;
                    string[] v = { rb.Banda, CarichiHf.Fmt(rb.FMHz, 3), zs(rb.Z), rb.Comportamento, double.IsNaN(rb.V) ? "-" : CarichiHf.Fmt(rb.V / 1000, 1) + " kV", double.IsNaN(rb.Perdita) ? "-" : CarichiHf.Fmt(rb.Perdita, rb.Perdita < 10 ? 1 : 0) + " W" };
                    for (int j = 0; j < v.Length; j++)
                    {
                        TextBlock t = new TextBlock { Text = v[j], Foreground = Pal.B((j == 4 && vRossa) || (j == 5 && pRossa) ? Pal.Red : Pal.Fore), FontWeight = (j == 4 && vRossa) || (j == 5 && pRossa) ? FontWeight.Bold : FontWeight.Normal, TextWrapping = TextWrapping.NoWrap };
                        Border cb = new Border { Child = t, Padding = new Thickness(6, 4), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 1, 1) };
                        Grid.SetColumn(cb, j); Grid.SetRow(cb, riga); tab.Children.Add(cb);
                    }
                    riga++;
                }
                // risultati
                ris.Children.Clear();
                double fs = bs != null ? bs.Operating : 3.65;
                switch (d.Tipo)
                {
                    case 0:
                    case 1:
                        ris.Children.Add(card("Risonanza propria f0", CarichiHf.Fmt(r.F0 / 1e6, 2) + " MHz", "L " + CarichiHf.Fmt(r.L * 1e6, 1) + " µH · C " + CarichiHf.Fmt(r.C * 1e12, 0) + " pF" + (d.Tipo == 1 ? " (dal cavo)" : " (componenti)"), true));
                        Border cq = card("Q a f0", double.IsInfinity(r.Q0) ? "∞" : CarichiHf.Fmt(r.Q0, 0), (double.IsInfinity(r.QAltra) ? "Q ideale" : "cala a " + CarichiHf.Fmt(r.QAltra, 0) + " a " + CarichiHf.Fmt(fs, 1) + " MHz (effetto pelle)"), false);
                        if (d.Tipo == 1) FumettoGiallo(cq, "Q della trappola alla risonanza.", "resistenza della calza e del centrale per effetto pelle con un fattore 3 per la treccia e fino a 3 per la vicinanza delle spire, " +
                            "piu' la perdita del dielettrico (tangente 3e-4 PE, 2e-4 PTFE). Le trappole coassiali misurate stanno di solito tra Q 100 e 300.", "capitolo 15 (Carichi HF)");
                        ris.Children.Add(cq);
                        if (d.Tipo == 1) ris.Children.Add(card("Lunghezza di cavo", CarichiHf.Fmt(r.LungCavo, 2) + " m", "+ 10 cm per le saldature", false));
                        else ris.Children.Add(card("R in parallelo equivalente", double.IsInfinity(r.Rpar) || r.Rpar <= 0 ? "∞" : CarichiHf.Fmt(r.Rpar / 1000, 1) + " kΩ", "Q x w0 L: e' il carico LD che entra nel modello", false));
                        Border ce = card("Effetto sul dipolo " + d.Sotto, (r.Accorciamento >= 0 ? "+" : "") + CarichiHf.Fmt(r.Accorciamento, 1) + " m", "accorciamento per braccio dovuto alla trappola (interno " + CarichiHf.Fmt(r.Interno, 2) + " m, esterno " + CarichiHf.Fmt(r.Esterno, 2) + " m)", false);
                        FumettoGiallo(ce, "Braccio del dipolo alla banda bassa con la trappola, confrontato con il braccio a grandezza piena (71,3 / f).", "modello a linea del braccio caricato X = Z0 (cot(beta b) - tan(beta a)), Z0 = 60 (ln(2h/a) - 1), x 0,95 per l'effetto di estremita': " +
                            "il NEC con la trappola come carico da' la misura giusta.", "capitolo 15 (Carichi HF)");
                        ris.Children.Add(ce);
                        break;
                    case 2:
                    case 5:
                        ris.Children.Add(card(d.Tipo == 2 ? "Induttanza" : "Induttanza misurata", CarichiHf.Fmt(r.L * 1e6, 2) + " µH", d.Tipo == 2 ? "Wheeler, bobina lunga " + CarichiHf.Fmt(r.LungBobina * 1000, 0) + " mm, filo " + CarichiHf.Fmt(r.LungCavo, 2) + " m" : "dalla misura", true));
                        Border cs = card("Autorisonanza", CarichiHf.Fmt(r.Srf / 1e6, 1) + " MHz", "capacita' propria " + CarichiHf.Fmt(r.Cs * 1e12, 1) + " pF" + (d.Tipo == 2 ? " (Medhurst)" : " (dalla f0 misurata)"), false);
                        if (d.Tipo == 2) FumettoGiallo(cs, "Frequenza a cui la bobina risuona da sola con la sua capacita' propria.", "capacita' propria dalla tabella di Medhurst in funzione di lunghezza / diametro, valida per bobine a strato singolo lontane da masse metalliche.", "capitolo 15 (Carichi HF)");
                        ris.Children.Add(cs);
                        Border cq2 = card("Q a " + CarichiHf.Fmt(d.FLavoroMHz, 3) + " MHz", CarichiHf.Fmt(r.Q, 0), "reattanza " + CarichiHf.Fmt(r.X, 0) + " Ω con l'autorisonanza", false);
                        if (d.Tipo == 2) FumettoGiallo(cq2, "Q della bobina alla frequenza di lavoro.", "resistenza del filo per effetto pelle per un fattore di vicinanza 1 + 2 (d/p)^2 (d filo, p passo): Medhurst da' valori simili per bobine con lunghezza circa uguale al diametro.", "capitolo 15 (Carichi HF)");
                        ris.Children.Add(cq2);
                        ris.Children.Add(EffettoCard(card, r, d));
                        break;
                    case 3:
                        ris.Children.Add(card("Reattanza della piega", "+j " + CarichiHf.Fmt(r.X, 0) + " Ω", "come una bobina da " + CarichiHf.Fmt(r.Leq * 1e6, 2) + " µH a " + CarichiHf.Fmt(d.FLavoroMHz, 3) + " MHz", true));
                        ris.Children.Add(card("Z0 della linea", CarichiHf.Fmt(r.Z0Stub, 0) + " Ω", "120 acosh(s/d), fili a " + CarichiHf.Fmt(d.DistanzaMm, 0) + " mm", false));
                        ris.Children.Add(card("Q della piega", CarichiHf.Fmt(r.Q, 0), "filo usato " + CarichiHf.Fmt(r.LungCavo, 2) + " m", false));
                        ris.Children.Add(EffettoCard(card, r, d));
                        break;
                    case 4:
                        {
                            Border cc = card("Capacita' del cappello", CarichiHf.Fmt(r.Ccap * 1e12, 0) + " pF", d.Raggi + " raggi da " + CarichiHf.Fmt(d.RaggioM, 2) + " m" + (d.Anello ? " con anello" : ""), true);
                            FumettoGiallo(cc, "Capacita' del cappello verso lo spazio.", "somma dei raggi (filo sottile, 2 pi eps0 l / (ln(2l/a) - 1)) e dell'anello che satura verso il disco pieno (8 eps0 r): i raggi si fanno schermo a vicenda. Inserito come fili il NEC lo calcola esatto.", "capitolo 15 (Carichi HF)");
                            ris.Children.Add(cc);
                            ris.Children.Add(card("Reattanza a " + CarichiHf.Fmt(d.FLavoroMHz, 3) + " MHz", "-j " + CarichiHf.Fmt(-r.X, 0) + " Ω", "in punta al verticale", false));
                            ris.Children.Add(card("Altezza per la risonanza", CarichiHf.Fmt(r.AltezzaRis, 2) + " m", "tan(beta h) = 1 / (w C Z0), x 0,95", false));
                            ris.Children.Add(card("Effetto sul verticale", (r.Accorciamento >= 0 ? "+" : "") + CarichiHf.Fmt(r.Accorciamento, 2) + " m", "rispetto al quarto d'onda pieno (" + CarichiHf.Fmt(0.95 * 299.792458 / d.FLavoroMHz / 4, 2) + " m)", false));
                            break;
                        }
                }
                foreach (string a in r.Avvisi) ris.Children.Add(AvvisoGiallo(a));
                foreach (string o in r.Ok) ris.Children.Add(AvvisoGiallo(o));
            };

            Action cambiaTipo = () => { costruisciDati(); ricalcola(); };
            fTipi.Selected += k => { d.Tipo = Array.IndexOf(chiavi, k); cambiaTipo(); };
            fIns.Selected += k => { inserisci = k; };
            foreach (TextBox t in new[] { tBraccio, tForma, tSpire, tL, tC, tQL, tQC, tDiam, tFilo, tPassoMm, tSpB, tF, tPos, tFiloEl, tPiega, tDist, tFiloLl, tRaggi, tRaggio, tFiloCap, tAlt, tMisL, tMisQ, tMisF, tMisF0, tPot })
                t.LostFocus += (s, e) => ricalcola();
            foreach (ComboBox c in new[] { cBlocca, cSotto, cCavo, cPasso, cColl }) c.SelectionChanged += (s, e) => { if (datiCorpo.Children.Count > 0) { if (d.Tipo == 1) costruisciDati(); ricalcola(); } };
            cAnello.IsCheckedChanged += (s, e) => ricalcola();
            bSpire.Click += (s, e) =>
            {
                leggi();
                Band bb = CarichiHf.Banda(d.Blocca);
                double f0 = (bb != null ? bb.Operating : 7.1) * 1e6;
                if (d.Tipo == 1) { d.Spire = CarichiHf.SpirePerF0(d, f0); tSpire.Text = CarichiHf.Fmt(d.Spire, 2); }
                else if (d.Tipo == 0) { double C = 1 / (Math.Pow(2 * Math.PI * f0, 2) * d.LuH * 1e-6); tC.Text = CarichiHf.Fmt(C * 1e12, 1); }
                else
                {
                    // spire, piega o raggi che portano a zero la reattanza da compensare (braccio risonante alla frequenza di lavoro)
                    Func<double, double> err = x =>
                    {
                        CarichiHf.Dati t = d.Clone();
                        if (d.Tipo == 2) t.SpireBobina = x; else if (d.Tipo == 3) t.PiegaM = x; else t.RaggioM = x;
                        CarichiHf.Ris q = CarichiHf.Calcola(t);
                        return d.Tipo == 4 ? q.AltezzaRis - d.AltezzaM : double.IsNaN(q.Esterno) ? -1e9 : q.Interno + q.Esterno - d.BraccioM;
                    };
                    double lo = d.Tipo == 2 ? 0.5 : 0.02, hi = d.Tipo == 2 ? 300 : d.Tipo == 3 ? 299.792458 / d.FLavoroMHz * 0.24 : 30;
                    double elo = err(lo), ehi = err(hi);
                    if (Math.Sign(elo) == Math.Sign(ehi)) { Status("Carichi HF: nessuna soluzione nell'intervallo (" + CarichiHf.Fmt(lo, 2) + " - " + CarichiHf.Fmt(hi, 2) + ")", true); return; }
                    for (int i = 0; i < 70; i++) { double m = (lo + hi) / 2, em = err(m); if (Math.Sign(em) == Math.Sign(elo)) { lo = m; elo = em; } else hi = m; }
                    double x0 = (lo + hi) / 2;
                    if (d.Tipo == 2) tSpB.Text = CarichiHf.Fmt(x0, 1); else if (d.Tipo == 3) tPiega.Text = CarichiHf.Fmt(x0, 2); else tRaggio.Text = CarichiHf.Fmt(x0, 2);
                }
                ricalcola();
            };
            bIns.Click += (s, e) =>
            {
                ricalcola();
                if (model == null || r == null) { Status("Carichi HF: apri un modello nella finestra principale.", true); return; }
                int tag, seg; string err;
                if (!PulseToSeg(tDove.Text, out tag, out seg, out err)) { Status("Carichi HF: " + err, true); return; }
                double fl = d.Tipo <= 1 ? r.F0 : d.FLavoroMHz * 1e6;
                if (inserisci == "fili")
                {
                    if (d.Tipo != 3 && d.Tipo != 4) { Status("Come fili si inseriscono il linear loading e il cappello capacitivo: per gli altri scegli \"Come carico\".", true); return; }
                    List<string> note = new List<string>();
                    NecModel nm = CarichiHf.Fili(model, d, tag, seg, d.FLavoroMHz * 1e6, note);
                    if (nm == null) { Status("Carichi HF: " + string.Join(" ", note), true); return; }
                    nm.FileName = model.FileName;
                    GeoMetti(nm, true);
                    foreach (string nn in note) Registro.Scrivi("AZIONE", "carichi HF: " + nn);
                    Status("Carichi HF inseriti come fili (Annulla nella barra Geometria li toglie): " + string.Join(" ", note));
                }
                else
                {
                    LoadSpec ld = CarichiHf.Ld(r, fl, tag, seg);
                    if (ld == null) { Status("Il cappello capacitivo si inserisce come fili.", true); return; }
                    NecModel nm = model.Clone();
                    nm.Loads.Add(ld);
                    nm.UseLoads = true;
                    nm.FileName = model.FileName;
                    GeoMetti(nm, true);
                    ldRows = null;
                    Registro.Scrivi("AZIONE", "carichi HF: carico LD " + ld.Type + " sul filo " + tag + " segmento " + seg);
                    Status("Carico inserito nel modello (LD " + (ld.Type == 1 ? "RLC parallelo" : "RLC serie") + " sul filo " + tag + ", segmento " + seg + "; Annulla nella barra Geometria lo toglie).");
                }
            };
            fIns.Select("carico", false);
            fTipi.Select("coax", false);
            d.Tipo = 1;
            try { d.Spire = CarichiHf.SpirePerF0(d, 7.1e6); tSpire.Text = CarichiHf.Fmt(d.Spire, 2); } catch { }
            costruisciDati();
            ricalcola();
            return new ScrollViewer { Content = g, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }

        Border EffettoCard(Func<string, string, string, bool, Border> card, CarichiHf.Ris r, CarichiHf.Dati d)
        {
            Border ce = card("Effetto sul dipolo", (r.Accorciamento >= 0 ? "+" : "") + CarichiHf.Fmt(r.Accorciamento, 2) + " m",
                "per braccio: carico a " + CarichiHf.Fmt(r.Interno, 2) + " m dal centro, poi " + CarichiHf.Fmt(r.Esterno, 2) + " m fino alla punta", false);
            FumettoGiallo(ce, "Accorciamento del braccio rispetto al quarto d'onda pieno (71,3 / f) con il carico in quel punto.", "modello a linea del braccio caricato X = Z0 (cot(beta b) - tan(beta a)), x 0,95 per l'effetto di estremita'.", "capitolo 15 (Carichi HF)");
            return ce;
        }
    }
}
