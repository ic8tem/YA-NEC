// YA-NEC 0.4.30 - sottolinguetta dei Calcolatori "Staffe di fissaggio" (richiesta di IC8TEM del 14/11/2026 22:31: "questa mi serve come
// scheda di calcolo aggiuntiva (sottolinguetta di calcolatori): la modellazione proveniente da qua viene aggiunta al modello a richiesta
// dell'utente, non cancella il modello 3d caricato esistente"). Calcolo in Engine/Staffe.cs (ARRL 24a ed. cap. 8, Leeson cap. 9).
// Il pulsante modifica il modello aperto (le piastre si aggiungono agli elementi scelti, le punte si allungano se richiesto): il modello
// resta quello caricato, con la storia di Annulla / Ripeti della barra Geometria.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Control BuildStaffePanel()
        {
            StackPanel root = new StackPanel();
            Grid top = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,14,*") };
            StackPanel dati = new StackPanel { Spacing = 5, Width = 500 };
            dati.Children.Add(new TextBlock { Text = "Dati della piastra", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            TextBox tF = Tb(model != null ? model.FreqMHz.ToString("0.000", CultureInfo.CurrentCulture) : "14,175", 96);
            TextBox tL = Tb("100", 80), tW = Tb("50", 80), tS = Tb("5", 80);
            Func<string, TextBlock> u = s => new TextBlock { Text = s, VerticalAlignment = VerticalAlignment.Center };
            dati.Children.Add(RigaCalc("Frequenza", tF, u("MHz")));
            dati.Children.Add(RigaCalc("Lunghezza lungo l'elemento", tL, u("mm")));
            dati.Children.Add(RigaCalc("Larghezza", tW, u("mm")));
            dati.Children.Add(RigaCalc("Spessore (piastra + morsetti)", tS, u("mm")));
            ComboBox cApp = new ComboBox { ItemsSource = new[] { "tutti gli elementi", "tutti tranne il radiatore", "solo il radiatore" }, SelectedIndex = 1, MinWidth = 240 };
            dati.Children.Add(RigaCalc("Applica a", cApp));
            CheckBox cComp = new CheckBox { Content = "allunga le punte della compensazione", IsChecked = true };
            ToolTip.SetTip(cComp, "Con la spunta, quando aggiungi le piastre al modello le punte di ogni elemento si allungano della compensazione calcolata (meta' per lato), " +
                "cosi' la risonanza torna quella senza piastra. Senza spunta si aggiunge solo la piastra: si vede di quanto la staffa sposta l'antenna.");
            dati.Children.Add(cComp);
            dati.Children.Add(new YaNec.Controls.TestoAdattato { Child = Nota("La piastra al centro dell'elemento e' un tratto corto e grosso: un gradino in piu' del tubo. Diametro equivalente della sezione " +
                "rettangolare W x t: 2 x 0,2235 (W + t) (Balanis). Compensazione: allungamento delle punte che riporta la risonanza del modello a linee di Schelkunoff " +
                "(lo stesso della correzione di Leeson) a quella senza piastra. Fonti: ARRL Antenna Book 24a ed. cap. 8, D. Leeson W6NL cap. 9.") });
            Button bAgg = Btn("Aggiungi le piastre al modello", true), bCopia = Btn("Copia la tabella", false);
            bAgg.Margin = new Thickness(0, 6, 8, 0); bCopia.Margin = new Thickness(0, 6, 8, 0);
            WrapPanel bott = new WrapPanel();
            bott.Children.Add(bAgg); bott.Children.Add(bCopia);
            dati.Children.Add(bott);
            dati.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 243, 196)), BorderBrush = new SolidColorBrush(Color.FromRgb(214, 168, 0)), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 6), Margin = new Thickness(0, 8, 0, 0),
                Child = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(90, 60, 0)), FontSize = 12.5,
                    Text = "⚠ Le piastre si aggiungono al modello aperto, che resta quello caricato (non si apre un modello nuovo): Annulla nella barra Geometria toglie la modifica. " +
                           "Elementi diritti lunghi almeno lambda/4; il filo centrale si divide in tubo, piastra e tubo, e sorgente e carichi del centro passano sulla piastra. " +
                           "Con tubi a gradini conviene accendere la correzione di Leeson (Impostazioni > Tubi telescopici): la piastra diventa un gradino in piu'." }
            });
            top.Children.Add(Scheda(dati));

            StackPanel dx = new StackPanel();
            WrapPanel cards = new WrapPanel();
            dx.Children.Add(cards);
            StackPanel tab = new StackPanel();
            TextBlock tabTit = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
            StackPanel righe = new StackPanel();
            tab.Children.Add(tabTit); tab.Children.Add(righe);
            dx.Children.Add(Scheda(tab));
            Grid.SetColumn(dx, 2);
            top.Children.Add(dx);
            root.Children.Add(top);

            List<Staffe.Elemento> elems = new List<Staffe.Elemento>();
            double lp = 0.1, dp = 0;
            Action ricalcola = () =>
            {
                double f = ParseNum(tF.Text, model != null ? model.FreqMHz : 14.175);
                lp = Math.Max(0, ParseNum(tL.Text, 100)) / 1000;
                dp = Staffe.DiametroPiastra(ParseNum(tW.Text, 50) / 1000, ParseNum(tS.Text, 5) / 1000);
                elems = model != null ? Staffe.Trova(model, f) : new List<Staffe.Elemento>();
                // ordine lungo il boom e nomi
                if (elems.Count > 1)
                {
                    Staffe.Elemento a = elems[0], z = elems[elems.Count - 1];
                    double ax = z.Cx - a.Cx, ay = z.Cy - a.Cy, az = z.Cz - a.Cz;
                    elems.Sort((p, q) => (p.Cx * ax + p.Cy * ay + p.Cz * az).CompareTo(q.Cx * ax + q.Cy * ay + q.Cz * az));
                }
                for (int i = 0; i < elems.Count; i++) elems[i].Nome = "elemento " + (i + 1) + (elems[i].Sorgente ? " (radiatore)" : "");
                foreach (Staffe.Elemento e in elems) Staffe.Compensa(e, lp, dp);
                cards.Children.Clear();
                cards.Children.Add(Riquadretto("Diametro equivalente", FN(dp * 1000, 1) + " mm", "2 x 0,2235 (W + t)", true));
                cards.Children.Add(Riquadretto("Elementi", elems.Count.ToString(CultureInfo.CurrentCulture), model == null ? "apri un modello" : "diritti, almeno lambda/4"));
                double dm = 0; int nm = 0;
                foreach (Staffe.Elemento e in elems) if (e.Delta > 0) { dm += e.Delta; nm++; }
                cards.Children.Add(Riquadretto("Compensazione media", nm > 0 ? FN(dm / nm * 1000, 1) + " mm" : "-", "per lato, allungando le punte"));
                tabTit.Text = model == null ? "Elementi: apri un modello nella finestra principale" : "Elementi di " + (string.IsNullOrEmpty(model.FileName) ? "modello" : Path.GetFileName(model.FileName));
                righe.Children.Clear();
                string cols = "1.6*,1*,1*,1.1*,1.1*,1.1*,1.6*";
                string[] hh = { "elemento", "mezza lunghezza (mm)", "tubo centrale (mm)", "risonanza senza (MHz)", "con piastra (MHz)", "allungamento per lato (mm)", "nota" };
                Grid h = new Grid { ColumnDefinitions = new ColumnDefinitions(cols) };
                for (int i = 0; i < hh.Length; i++) { TextBlock t = new TextBlock { Text = hh[i], Foreground = Pal.B(Pal.Muted), FontSize = 12, TextWrapping = TextWrapping.Wrap }; Grid.SetColumn(t, i); h.Children.Add(t); }
                righe.Children.Add(h);
                int app = cApp.SelectedIndex;
                foreach (Staffe.Elemento e in elems)
                {
                    bool si = app == 0 || (app == 1 && !e.Sorgente) || (app == 2 && e.Sorgente);
                    string nota = e.Motivo ?? (dp <= e.Lato[0][1] ? "piastra non piu' grossa del tubo" : (si ? "" : "non scelto in \"Applica a\""));
                    string[] v = { e.Nome, FN(e.H * 1000, 1), FN(e.Lato[0][1] * 1000, 1), FN(e.Fs0, 3), FN(e.Fs1, 3), FN(e.Delta * 1000, 1), nota };
                    Grid r = new Grid { ColumnDefinitions = new ColumnDefinitions(cols), Margin = new Thickness(0, 2) };
                    for (int j = 0; j < v.Length; j++)
                    {
                        TextBlock t = new TextBlock { Text = v[j], Foreground = Pal.B(si ? Pal.Fore : Pal.Muted), FontFamily = j > 0 && j < 6 ? Pal.Mono : FontFamily.Default, TextWrapping = j == 6 ? TextWrapping.Wrap : TextWrapping.NoWrap, FontSize = j == 6 ? 12 : 13 };
                        if (j == 5) t.FontWeight = FontWeight.Bold;
                        Grid.SetColumn(t, j); r.Children.Add(t);
                    }
                    righe.Children.Add(new Border { Child = r, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) });
                }
                if (elems.Count == 0) righe.Children.Add(Nota(model == null ? "Nessun modello aperto." : "Nel modello aperto non ci sono elementi diritti lunghi almeno lambda/4."));
                bAgg.IsEnabled = model != null && elems.Count > 0;
                bCopia.IsEnabled = elems.Count > 0;
            };
            foreach (TextBox t in new[] { tF, tL, tW, tS }) t.TextChanged += (s, e) => ricalcola();
            cApp.SelectionChanged += (s, e) => ricalcola();
            root.AttachedToVisualTree += (s, e) => { if (model != null) tF.Text = model.FreqMHz.ToString("0.000", CultureInfo.CurrentCulture); ricalcola(); };
            bAgg.Click += (s, e) =>
            {
                ricalcola();
                if (model == null || elems.Count == 0) return;
                int app = cApp.SelectedIndex;
                List<Staffe.Elemento> scelti = elems.FindAll(x => app == 0 || (app == 1 && !x.Sorgente) || (app == 2 && x.Sorgente));
                List<string> note = new List<string>();
                try
                {
                    NecModel nuovo = Staffe.Aggiungi(model, scelti, lp, dp, cComp.IsChecked == true, note);
                    nuovo.FileName = model.FileName;
                    GeoMetti(nuovo, true);
                    foreach (string n in note) Registro.Scrivi("AZIONE", "staffe: " + n);
                    Status("Piastre aggiunte al modello aperto (" + scelti.Count + " elementi; Annulla nella barra Geometria le toglie). " + (note.Count > 0 ? note[0] : ""));
                }
                catch (Exception ex) { Status("Piastre non aggiunte: " + ex.Message, true); }
                ricalcola();
            };
            bCopia.Click += async (s, e) =>
            {
                StringBuilder sb = new StringBuilder("elemento\tmezza lunghezza mm\ttubo centrale mm\trisonanza senza MHz\tcon piastra MHz\tallungamento per lato mm\n");
                foreach (Staffe.Elemento x in elems) sb.AppendLine(string.Join("\t", x.Nome, FN(x.H * 1000, 1), FN(x.Lato[0][1] * 1000, 1), FN(x.Fs0, 3), FN(x.Fs1, 3), FN(x.Delta * 1000, 1)));
                try { if (Clipboard != null) await Clipboard.SetTextAsync(sb.ToString()); Status("Tabella delle staffe copiata."); } catch { }
            };
            ricalcola();
            return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }
    }
}
