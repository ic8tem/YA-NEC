// YA-NEC - calcolatore "Correzione del boom" (bozzetto approvato il 12/11/2026, doc/bozzetto-boom.png).
// Formula DL6WU con il fit di G3SEK: C = (25,195 B - 229 B^2) x diametro del boom, B = diametro del boom in lunghezze d'onda,
// valida fino a B = 0,055; boom quadro x 1,18 (SM5BSZ); passante isolato = meta'; montaggio sopra il boom con staffa
// conduttiva circa 0,65 (DG7YBN on-boom); sopra il boom isolato: correzione trascurabile.
// Verso: "dal disegno (-)" toglie la correzione dalle misure del disegno per avere il modello NEC senza boom;
// "costruire (+)" la aggiunge alle misure del modello per avere quelle da tagliare.
// 0.4.7 (IC8TEM 13/11/2026 18:20): niente "Applica al modello": il pulsante crea una scheda PDF con i dati del boom e degli elementi.
// 0.4.8 (IC8TEM 14/11/2026 00:12): correzione applicabile in negativo. Con "dal disegno (-)" il pulsante "Crea il modello elettrico (-)"
// fa una copia del modello (rilievo di un'antenna reale) con gli elementi scelti accorciati della correzione, meta' per lato e attorno
// al centro dell'elemento: e' il modello elettrico da calcolare al NEC. Il modello rilevato non cambia (la copia si apre con un nome nuovo).
// 0.4.20 (bozzetto approvato da IC8TEM il 14/11/2026 18:53, "perfetto applica"): i due versi con i nomi del bozzetto,
// "progetto NEC -> elementi da realizzare" (tabella filo NEC / correzione / da realizzare / per lato) e "rilievo -> modello elettrico
// per il NEC" (tabella rilievo / correzione / elettrico / differenza, confronto del rilievo calcolato com'e' con il modello elettrico:
// risonanza, ROS, guadagno, F/R); percorso a tre riquadri, disegno di boom ed elementi, pulsanti e fumetto giallo per ogni verso.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Platform.Storage;
using System.Numerics;
using System.Text;
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
        public static double CorrezioneBoom(double fMHz, double boomMm, bool quadro, int montaggio, double distMm)
        {
            double lam = 299.792458 / fMHz * 1000;
            double d = boomMm * (quadro ? 1.18 : 1.0), B = d / lam;
            double c = (25.195 * B - 229 * B * B) * d;
            switch (montaggio)
            {
                case 1: return c / 2;                                     // passante isolato
                case 2: return distMm > 0 && distMm < d / 2 ? c * 0.25 * (1 - distMm / (d / 2)) : 0;   // sopra il boom isolato
                case 3: return c * 0.65;                                  // sopra il boom, staffa conduttiva (DG7YBN)
                default: return c;                                        // passante conduttivo
            }
        }

        class ElBoom { public int Card; public string Nome; public double Lung, Pos, Corr; public bool Sorgente; }

        Control BuildBoomPanel()
        {
            StackPanel root = new StackPanel();
            // 0.5.3 (IC8TEM 15/11/2026 20:00, "prova a riformattare"): i dati in una scheda larga in alto, caselle e scelte su piu' colonne,
            // formule in una riga che si apre; sotto riquadri, grafico + confronto, disegno + tabella a tutta larghezza (prima la colonna
            // dei dati occupava mezza pagina e spingeva in basso le informazioni utili)
            StackPanel top = new StackPanel();
            StackPanel dati = new StackPanel { Spacing = 4 };
            dati.Children.Add(new TextBlock { Text = "Dati", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            double f = model != null ? model.FreqMHz : 144.3;
            TextBox tF = Tb(f.ToString("0.000", CultureInfo.CurrentCulture), 96), tB = Tb("40", 96), tE = Tb("6", 96), tDist = Tb("10", 60);
            TextBlock hB = new TextBlock { Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center };
            int quadro = 0, montaggio = 0, verso = 0, applica = 0, metodo = 0;
            Action ricalcola = null;
            WrapPanel celle = new WrapPanel();
            foreach (TextBox t0 in new[] { tF, tB, tE }) t0.Width = 84;
            hB.FontSize = 11.5; hB.Margin = new Thickness(146, 0, 0, 0);
            celle.Children.Add(CellaCalc("Frequenza", null, tF, new TextBlock { Text = "MHz", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }, new TextBlock()));
            celle.Children.Add(CellaCalc("Diametro boom", null, tB, new TextBlock { Text = "mm", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }, hB));
            celle.Children.Add(CellaCalc("Diametro elementi", null, tE, new TextBlock { Text = "mm", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }, new TextBlock()));
            dati.Children.Add(celle);
            WrapPanel scelte = new WrapPanel();
            Func<string, Control, Control> coppia = (etq, ctl) =>
            {
                StackPanel h0 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 18, 2) };
                h0.Children.Add(new TextBlock { Text = etq, Width = 90, VerticalAlignment = VerticalAlignment.Center, FontSize = 13.5, Foreground = Pal.B(Pal.Muted) });
                h0.Children.Add(ctl);
                return h0;
            };
            scelte.Children.Add(coppia("Boom", Scelta(new[] { "tondo", "quadro (x1,18 SM5BSZ)" }, 0, k => { quadro = k; ricalcola(); })));
            dati.Children.Add(scelte);
            dati.Children.Add(new TextBlock { Text = "Montaggio", Foreground = Pal.B(Pal.Muted), FontSize = 13.5, Margin = new Thickness(0, 4, 0, 0) });
            WrapPanel montP = new WrapPanel();
            dati.Children.Add(montP);
            string[] montNomiCorti = { "passanti (conduttivi)", "passanti isolati", "sopra il boom, isolati", "sopra il boom, staffa" };
            string[] mont = { "passante, a contatto (conduttivo)", "passante, isolato (bussole, rivetti nylon)", "sopra il boom, isolato: distanza", "sopra il boom, staffa conduttiva" };
            for (int i = 0; i < 4; i++)
            {
                int k = i;
                RadioButton rb = new RadioButton { Content = mont[i], GroupName = "boommont", IsChecked = i == 0, Margin = new Thickness(18, 0, 0, 0), MinWidth = 330 };
                rb.IsCheckedChanged += (s, e) => { if (rb.IsChecked == true) { montaggio = k; ricalcola(); } };
                if (i == 2)
                {
                    StackPanel h = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                    h.Children.Add(rb); h.Children.Add(tDist); h.Children.Add(new TextBlock { Text = "mm", VerticalAlignment = VerticalAlignment.Center });
                    montP.Children.Add(h);
                }
                else montP.Children.Add(rb);
            }
            ComboBox cMet = new ComboBox { ItemsSource = new[] { "DL6WU / G3SEK (formula)" }, SelectedIndex = 0, MinWidth = 260 };
            ToolTip.SetTip(cMet, "anche: tabella YU7EF, SM5BSZ (BC.exe, calo verso le estremita' del boom), DG7YBN (on-boom): nella tabella di confronto; la tabella YU7EF completa non e' ancora nel programma.");
            ComboBox cApp = new ComboBox { ItemsSource = new[] { "direttori (davanti al radiatore)", "tutti tranne il radiatore", "tutti gli elementi" }, SelectedIndex = 0, MinWidth = 260 };
            WrapPanel scelte2 = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            scelte2.Children.Add(coppia("Metodo", cMet));
            scelte2.Children.Add(coppia("Applica a", cApp));
            dati.Children.Add(scelte2);
            StackPanel versoP = Scelta(new[] { "progetto NEC → elementi da realizzare", "rilievo → modello elettrico per il NEC" }, 0, k => { verso = k; ricalcola(); });
            WrapPanel versoW = new WrapPanel();
            List<Control> vfigli = new List<Control>();
            foreach (Control vx in versoP.Children) vfigli.Add(vx);
            versoP.Children.Clear();
            foreach (Control vx in vfigli) { vx.Margin = new Thickness(0, 1, 4, 2); versoW.Children.Add(vx); }
            DockPanel rigaVerso = RigaCalc("Verso");
            rigaVerso.Children.Add(versoW);
            dati.Children.Add(rigaVerso);
            CheckBox cCalo = new CheckBox { Content = "calo verso le estremita' del boom (SM5BSZ)", IsEnabled = false };
            ToolTip.SetTip(cCalo, "In arrivo: serve la curva di SM5BSZ (BC.exe) del calo della correzione verso le estremita' del boom.");
            dati.Children.Add(cCalo);
            dati.Children.Add(new Expander { Header = "Formule e fonti", HorizontalAlignment = HorizontalAlignment.Stretch, Content = Nota("Formule (B = boom in lunghezze d'onda, C = frazione del diametro del boom): passante conduttivo C = 25,195 B - 229 B²; passante isolato la meta'; " +
                "valide fino a B = 0,055 lambda (DL6WU, fit di G3SEK). Boom quadro: lato x 1,18 (SM5BSZ). Staffa conduttiva sopra il boom: circa 0,65 (DG7YBN on-boom). " +
                "Non tengono conto del diametro degli elementi, della distanza dalle estremita' del boom ne' della segmentazione del modello (DG7YBN). Anche: tabella YU7EF, SM5BSZ (BC.exe, calo verso le estremita' del boom), DG7YBN (on-boom), nella tabella di confronto.") });
            Button bApp = Btn("Scheda PDF da costruire", true), bCopia = Btn("Copia la tabella", false), bElet = Btn("Crea il modello elettrico per il NEC", true);
            Button bCsv = Btn("Esporta CSV", false), bRil = Btn("Calcola il rilievo com'e'", false), bConf = Btn("Confronta i due", false);
            foreach (Button b0 in new[] { bApp, bCopia, bElet, bCsv, bRil, bConf }) b0.Margin = new Thickness(0, 6, 8, 0);
            Border pDati = Scheda(dati);
            top.Children.Add(pDati);

            StackPanel dx = new StackPanel();
            WrapPanel cards = new WrapPanel();
            dx.Children.Add(cards);
            Grid mid = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,12,2*") };
            LineChart ch = new LineChart { Height = 300, XLabel = "diametro del boom (mm)", YLabel = "mm", EmptyText = "" };
            StackPanel chBox = new StackPanel();
            chBox.Children.Add(new TextBlock { Text = "Correzione in funzione del boom (DL6WU / G3SEK)", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6) });
            chBox.Children.Add(ch);
            TextBlock chNota = Nota("");
            chBox.Children.Add(chNota);
            mid.Children.Add(Scheda(chBox));
            // confronto tra le fonti (valori del bozzetto approvato)
            StackPanel conf = new StackPanel();
            conf.Children.Add(new TextBlock { Text = "Confronto tra le fonti (isolato passante)", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6) });
            string[][] fonti =
            {
                new[] { "Fonte", "Boom", "144", "432" },
                new[] { "DL6WU/G3SEK (YU7EF)", "25 tondo", "3,7", "7,6" },
                new[] { "VE7BQH", "25,4", "3,47", "7,00" },
                new[] { "K1FO", "25,4", "-", "8,12" },
                new[] { "I0JXX (elem. 5 mm)", "25", "2,50", "7,7" },
                new[] { "DJ9BV (quadro)", "25x25", "3,0", "6,0" },
                new[] { "DG7YBN on-boom", "20x20", "3,9", "7,5" },
            };
            foreach (string[] r in fonti)
            {
                Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,72,46,46"), Margin = new Thickness(0, 3) };
                for (int i = 0; i < 4; i++)
                {
                    TextBlock t = new TextBlock { Text = r[i], FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 6, 0), Foreground = Pal.B(r == fonti[0] ? Pal.Muted : Pal.Fore), FontFamily = i > 0 && r != fonti[0] ? Pal.Mono : FontFamily.Default };
                    Grid.SetColumn(t, i); g.Children.Add(t);
                }
                conf.Children.Add(new Border { Child = g, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) });
            }
            conf.Children.Add(Nota("Somiglianze: tutte entro 1-1,5 mm a 144 MHz. Differenze: diametro dei fori, isolanti, elementi; per DG7YBN il valore cala verso le estremita' del boom (SM5BSZ)."));
            Border pConf = Scheda(conf);
            Grid.SetColumn(pConf, 2);
            mid.Children.Add(pConf);
            dx.Children.Add(mid);
            // 0.4.20: disegno a sinistra, tabella del verso scelto a destra (bozzetto approvato)
            Grid gTab = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,12,3*") };
            BoomDisegno disegno = new BoomDisegno { Height = 420 };
            gTab.Children.Add(Scheda(disegno));
            StackPanel tab = new StackPanel();
            TextBlock tabTit = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
            WrapPanel percorso = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            StackPanel righe = new StackPanel();
            WrapPanel cardsV = new WrapPanel { Margin = new Thickness(0, 8, 0, 4) };
            TextBlock testoV = Nota("");
            Border gialloV = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 243, 196)), BorderBrush = new SolidColorBrush(Color.FromRgb(214, 168, 0)), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 6), Margin = new Thickness(0, 8, 0, 0),
                Child = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(90, 60, 0)), FontSize = 12.5,
                    Text = "⚠ La correzione vale per elementi diritti che attraversano il boom; per elementi isolati o su staffa vale la frazione indicata (passante isolato meta', staffa 0,65). " +
                           "Il file del rilievo non viene sovrascritto: il modello elettrico si apre come file nuovo (\"... elettrico ....nec\")." }
            };
            WrapPanel bottoniV = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
            tab.Children.Add(tabTit); tab.Children.Add(percorso); tab.Children.Add(righe); tab.Children.Add(cardsV); tab.Children.Add(testoV); tab.Children.Add(gialloV); tab.Children.Add(bottoniV);
            Border pTab = Scheda(tab);
            Grid.SetColumn(pTab, 2);
            gTab.Children.Add(pTab);
            dx.Children.Add(gTab);
            // risultati del confronto (verso rilievo): rilievo com'e' e modello elettrico
            string confKey = null;
            double[] confRil = null, confEl = null;   // risonanza MHz, ROS, Gmax dBi, F/R dB
            top.Children.Add(dx);
            root.Children.Add(top);

            List<ElBoom> elems = new List<ElBoom>();
            double corr = 0;
            Func<List<ElBoom>> trovaElementi = () =>
            {
                List<ElBoom> l = new List<ElBoom>();
                if (model == null) return l;
                double lam = 299.792458 / Math.Max(0.1, ParseNum(tF.Text, f));
                // elementi = fili diritti lunghi almeno lambda/4, ordinati lungo l'asse del boom (direzione dei centri)
                for (int i = 0; i < model.Geometry.Count; i++)
                {
                    GeomCard c = model.Geometry[i];
                    if (c.Id != "GW") continue;
                    double L = Math.Sqrt((c.F4 - c.F1) * (c.F4 - c.F1) + (c.F5 - c.F2) * (c.F5 - c.F2) + (c.F6 - c.F3) * (c.F6 - c.F3));
                    if (L < lam / 4) continue;
                    ElBoom e = new ElBoom { Card = i, Lung = L };
                    foreach (SourceSpec s in model.Sources) if (s.Tag == c.I1) e.Sorgente = true;
                    l.Add(e);
                }
                if (l.Count < 2) return l;
                GeomCard a = model.Geometry[l[0].Card], z = model.Geometry[l[l.Count - 1].Card];
                double ax = (z.F1 + z.F4) / 2 - (a.F1 + a.F4) / 2, ay = (z.F2 + z.F5) / 2 - (a.F2 + a.F5) / 2, az = (z.F3 + z.F6) / 2 - (a.F3 + a.F6) / 2;
                double al = Math.Max(1e-9, Math.Sqrt(ax * ax + ay * ay + az * az));
                foreach (ElBoom e in l)
                {
                    GeomCard c = model.Geometry[e.Card];
                    e.Pos = (((c.F1 + c.F4) / 2) * ax + ((c.F2 + c.F5) / 2) * ay + ((c.F3 + c.F6) / 2) * az) / al;
                }
                l.Sort((p, q) => p.Pos.CompareTo(q.Pos));
                int ir = l.FindIndex(e => e.Sorgente);
                // il verso del boom va dal riflettore (prima del radiatore) ai direttori
                if (ir >= 0 && ir > l.Count / 2) { l.Reverse(); ir = l.Count - 1 - ir; }
                int nd = 0, nr = 0;
                for (int i = 0; i < l.Count; i++)
                {
                    ElBoom e = l[i];
                    if (e.Sorgente) e.Nome = "radiatore";
                    else if (ir >= 0 && i < ir) e.Nome = "riflettore" + (ir > 1 ? " " + (++nr) : "");
                    else e.Nome = "D" + (++nd);
                }
                return l;
            };

            ricalcola = () =>
            {
                double fr = ParseNum(tF.Text, f), db = ParseNum(tB.Text, 40), dist = ParseNum(tDist.Text, 10);
                double lam = 299.792458 / Math.Max(0.1, fr) * 1000;
                hB.Text = "= " + (db * (quadro == 1 ? 1.18 : 1) / lam).ToString("0.0000", CultureInfo.CurrentCulture) + " lambda";
                corr = CorrezioneBoom(fr, db, quadro == 1, montaggio, dist);
                double cIso = CorrezioneBoom(fr, db, quadro == 1, 1, dist);
                cards.Children.Clear();
                cards.Children.Add(Riquadretto("Correzione (formula)", FN(corr, 1) + " mm", "su tutto l'elemento", true));
                cards.Children.Add(Riquadretto("Tabella YU7EF", "-", "tabella non ancora nel programma"));
                cards.Children.Add(Riquadretto("Per lato", FN(corr / 2, 1) + " mm", "ogni meta' elemento"));
                cards.Children.Add(Riquadretto("Se isolato", FN(cIso, 1) + " mm", "meta' della passante conduttiva"));
                cards.Children.Add(Riquadretto("Limite della formula", FN(0.055 * lam / (quadro == 1 ? 1.18 : 1), 0) + " mm", "boom fino a 0,055 lambda" + (db * (quadro == 1 ? 1.18 : 1) / lam > 0.055 ? " - SUPERATO" : "")));

                // grafico 10-50 mm a 144 e 432 MHz
                double[] xs = Calcolatori.Lin(10, 50, 41);
                double[] y144c = new double[41], y144i = new double[41], y432c = new double[41], y432i = new double[41];
                for (int i = 0; i < 41; i++)
                {
                    y144c[i] = CorrezioneBoom(144.3, xs[i], quadro == 1, 0, 0); y144i[i] = y144c[i] / 2;
                    y432c[i] = CorrezioneBoom(432.2, xs[i], quadro == 1, 0, 0); y432i[i] = y432c[i] / 2;
                }
                ch.X = xs;
                ch.Series = new List<LineSeries>
                {
                    new LineSeries { Name = "432 cond.", Y = y432c, Color = Pal.Orange },
                    new LineSeries { Name = "432 isol.", Y = y432i, Color = Pal.Orange, Dashed = true },
                    new LineSeries { Name = "144 cond.", Y = y144c, Color = Pal.Accent },
                    new LineSeries { Name = "144 isol.", Y = y144i, Color = Pal.Accent, Dashed = true },
                };
                ch.MarkX = db; ch.YMinFixed = 0;
                ch.InvalidateVisual();
                double lim432 = 0.055 * 299.792458 / 432.2 * 1000 / (quadro == 1 ? 1.18 : 1);
                chNota.Text = "cond. = passante conduttivo, isol. = passante isolato (tratteggio). Linea verticale: il boom scelto (" + FN(db, 0) + " mm, " + FN(corr, 1) + " mm a " + FN(fr, 3) + " MHz). La formula vale fino a 0,055 lambda: " +
                    FN(lim432, 0) + " mm a 432 MHz, " + FN(0.055 * 299.792458 / 144.3 * 1000 / (quadro == 1 ? 1.18 : 1), 0) + " mm a 144 MHz." +
                    (db * (quadro == 1 ? 1.18 : 1) / lam > 0.055 ? "  ⚠ Il boom supera 0,055 lambda: la formula non vale." : "");

                // tabella degli elementi (0.4.20: colonne del bozzetto approvato)
                elems = trovaElementi();
                applica = cApp.SelectedIndex;
                righe.Children.Clear();
                tabTit.Text = model == null ? "Elementi: apri un modello" : "Elementi di " + (string.IsNullOrEmpty(model.FileName) ? "modello" : Path.GetFileName(model.FileName)) +
                    (verso == 1 ? ": dal rilievo scritto nel NEC al modello elettrico" : ": dal progetto elettrico agli elementi da realizzare");
                percorso.Children.Clear();
                string[][] passi = verso == 1
                    ? new[] { new[] { "Rilievo nel NEC", "misure reali scritte nei fili (con boom)" }, new[] { "- correzione del boom", "al contrario" }, new[] { "Modello elettrico", "idoneo alla simulazione" } }
                    : new[] { new[] { "Fili del modello NEC", "progetto elettrico, senza boom" }, new[] { "+ correzione del boom", "DL6WU / G3SEK" }, new[] { "Elementi da realizzare", "misure da tagliare" } };
                for (int q = 0; q < 3; q++)
                {
                    StackPanel ps = new StackPanel();
                    ps.Children.Add(new TextBlock { Text = passi[q][0], FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore), HorizontalAlignment = HorizontalAlignment.Center });
                    ps.Children.Add(new TextBlock { Text = passi[q][1], FontSize = 11.5, Foreground = Pal.B(Pal.Muted), HorizontalAlignment = HorizontalAlignment.Center });
                    percorso.Children.Add(new Border { Child = ps, Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 5), MinWidth = 150 });
                    if (q < 2) percorso.Children.Add(new TextBlock { Text = "➜", FontSize = 18, Foreground = Pal.B(Pal.Accent), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0) });
                }
                string cols = "1.3*,1.2*,1.5*,1.4*,1*,1.3*";
                Grid h = new Grid { ColumnDefinitions = new ColumnDefinitions(cols) };
                string[] hh = verso == 1 ? new[] { "elemento", "rilievo nel NEC (mm)", "correzione (mm)", "elettrico per il NEC (mm)", "differenza", "nota" }
                                         : new[] { "elemento", "filo NEC (mm)", "correzione (mm)", "da realizzare (mm)", "per lato (mm)", "nota" };
                for (int i = 0; i < hh.Length; i++) { TextBlock t = new TextBlock { Text = hh[i], Foreground = Pal.B(Pal.Muted), FontSize = 12, TextWrapping = TextWrapping.Wrap }; Grid.SetColumn(t, i); h.Children.Add(t); }
                righe.Children.Add(h);
                int irad = elems.FindIndex(e => e.Sorgente);
                List<BoomDisegno.El> dis = new List<BoomDisegno.El>();
                for (int i = 0; i < elems.Count; i++)
                {
                    ElBoom e = elems[i];
                    bool si = applica == 2 || (applica == 1 && !e.Sorgente) || (applica == 0 && irad >= 0 && i > irad);
                    e.Corr = si ? corr : 0;
                    double mis = e.Lung * 1000, dc = si ? (verso == 1 ? -corr : corr) : 0, dopo = mis + dc;
                    string cTxt = si ? (dc > 0 ? "+" : "") + FN(dc, 1) : (e.Sorgente ? "0 (radiatore)" : "0 (non scelto)");
                    string nota = e.Sorgente ? "radiatore: isolato dal boom" : (si ? "" : "non scelto in \"Applica a\"");
                    string[] v = verso == 1
                        ? new[] { e.Nome, FN(mis, 1), cTxt, FN(dopo, 1), si ? FN(dc / mis * 100, 2) + " %" : "0", nota }
                        : new[] { e.Nome, FN(mis, 1), cTxt, FN(dopo, 1), FN(dopo / 2, 1), nota };
                    Grid r = new Grid { ColumnDefinitions = new ColumnDefinitions(cols), Margin = new Thickness(0, 2) };
                    for (int j = 0; j < v.Length; j++)
                    {
                        TextBlock t = new TextBlock { Text = v[j], Foreground = Pal.B(Pal.Fore), FontFamily = j > 0 && j < 5 ? Pal.Mono : FontFamily.Default, TextWrapping = j == 5 ? TextWrapping.Wrap : TextWrapping.NoWrap, FontSize = j == 5 ? 12 : 13 };
                        if (j == 3) { t.FontWeight = FontWeight.Bold; Border hb = new Border { Background = Pal.B(Pal.AccentSoft), Child = t, Padding = new Thickness(4, 0) }; Grid.SetColumn(hb, j); r.Children.Add(hb); }
                        else { Grid.SetColumn(t, j); r.Children.Add(t); }
                    }
                    righe.Children.Add(new Border { Child = r, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) });
                    // disegno: fisico (con boom) ed elettrico (senza)
                    double fis = verso == 1 ? mis : dopo, ele = verso == 1 ? dopo : mis;
                    dis.Add(new BoomDisegno.El { Nome = e.Nome, Pos_m = e.Pos, Fisico_mm = fis, Elettrico_mm = ele });
                }
                disegno.Elementi = dis; disegno.Rilievo = verso == 1; disegno.InvalidateVisual();

                // riquadri, testo, fumetto giallo e pulsanti del verso
                cardsV.Children.Clear();
                double dEqC = ParseNum(tB.Text, 40) * (quadro == 1 ? 1.18 : 1), Bl = dEqC / lam;
                if (verso == 0)
                {
                    cardsV.Children.Add(Riquadretto("Il modello NEC resta", "elettrico", "non si modifica: e' il progetto", true));
                    cardsV.Children.Add(Riquadretto("Correzione", "+" + FN(corr, 1) + " mm", "boom " + (quadro == 1 ? "quadro" : "tondo") + " " + FN(ParseNum(tB.Text, 40), 0) + ", " + montNomiCorti[montaggio]));
                    cardsV.Children.Add(Riquadretto("Validita'", Bl <= 0.055 ? "rispettata" : "SUPERATA", "boom " + Bl.ToString("0.000", CultureInfo.CurrentCulture) + " lambda " + (Bl <= 0.055 ? "<" : ">") + " 0,055"));
                    testoV.Text = "Si parte dal modello NEC aperto, che e' il progetto elettrico (fili senza boom). La tabella aggiunge la correzione del boom e da' le misure degli elementi da tagliare; il modello non cambia.";
                    gialloV.IsVisible = false;
                }
                else
                {
                    string key = model == null ? "" : model.ToNec() + "|" + FN(corr, 3) + "|" + applica;
                    bool valido = confKey != null && confKey == key && confRil != null && confEl != null;
                    Func<double[], string, string> rz = (a, cosa) => a == null ? "-" : (double.IsNaN(a[0]) ? "fuori +-10 %" : FN(a[0], 2) + " MHz");
                    cardsV.Children.Add(Riquadretto("Rilievo calcolato cosi' com'e'", valido ? rz(confRil, "") : "-", valido ? "risonanza; ROS " + FN(confRil[1], 2) + " a " + FN(fr, 3) + (confRil[1] > 1.5 ? " (non conforme)" : "") : "premi Confronta i due"));
                    cardsV.Children.Add(Riquadretto("Modello elettrico", valido ? rz(confEl, "") : "-", valido ? "risonanza; ROS " + FN(confEl[1], 2) + " a " + FN(fr, 3) : "premi Confronta i due", true));
                    cardsV.Children.Add(Riquadretto("Guadagno / F/R", valido ? FN(confEl[2], 1) + " dBi" : "-", valido ? FN(confEl[3], 1) + " dB (prima " + FN(confRil[2], 1) + " / " + FN(confRil[3], 1) + ")" : "elettrico, e prima col rilievo"));
                    testoV.Text = "Si parte da un'antenna gia' costruita o comprata: le misure reali (rilievo) si scrivono nei fili del modello NEC. Cosi' com'e' il calcolo non e' conforme, perche' il NEC non vede il boom. " +
                        "Il pulsante toglie la correzione del boom elemento per elemento e crea il modello elettrico idoneo alla simulazione; il rilievo resta in un file a parte.";
                    gialloV.IsVisible = true;
                }
                bottoniV.Children.Clear();
                foreach (Button b0 in verso == 1 ? new[] { bRil, bConf, bElet } : new[] { bCopia, bCsv, bApp }) bottoniV.Children.Add(b0);
                bApp.IsEnabled = true;
                bElet.IsEnabled = verso == 1 && model != null && elems.Exists(x => x.Corr > 0);
                bConf.IsEnabled = bElet.IsEnabled; bRil.IsEnabled = model != null; bCsv.IsEnabled = elems.Count > 0;
                ToolTip.SetTip(bConf, "Calcola il rilievo cosi' com'e' e il modello elettrico: risonanza (ricerca entro +-10 %), ROS su Z0 = " + FN(model != null ? model.Z0 : 50, 0) + " ohm, guadagno e F/R. Il modello aperto non cambia.");
                ToolTip.SetTip(bRil, "Calcola il modello aperto (il rilievo) cosi' com'e', senza correzione.");
                // fumetto giallo (bozzetto approvato): quando vale la correzione intera e quando una frazione
                {
                    StackPanel fs = new StackPanel { Spacing = 4, MaxWidth = 520 };
                    fs.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                        Text = "Rilievo di un'antenna reale: crea una copia del modello con gli elementi scelti accorciati della correzione (meta' per lato, attorno al centro dell'elemento) e la apre nella Struttura 3D per il calcolo. Il modello rilevato non cambia." });
                    fs.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(90, 60, 0)),
                        Text = "⚠ La correzione vale per elementi diritti che attraversano il boom; isolati o su staffa vale la frazione indicata (passante isolato meta', staffa 0,65). Formula DL6WU/G3SEK valida fino a 0,055 lambda (manuale, Correzione del boom)." });
                    ToolTip.SetTip(bElet, new Border { Background = new SolidColorBrush(Color.FromRgb(255, 236, 153)), BorderBrush = new SolidColorBrush(Color.FromRgb(214, 168, 0)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 6), Child = fs });
                }
                ToolTip.SetTip(bApp, "Crea una scheda PDF con i dati del boom (diametro, forma, montaggio, correzione) e la tabella degli elementi del modello aperto " +
                    "(posizione sul boom, misura, correzione, misura corretta e mezzo elemento per lato). Il modello non cambia.");
            };
            foreach (TextBox t in new[] { tF, tB, tE, tDist })
            {
                t.LostFocus += (s, e) => ricalcola();
                t.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) ricalcola(); };
            }
            cApp.SelectionChanged += (s, e) => ricalcola();
            // richiesta IC8TEM 13/11/2026 18:20: la correzione non si applica al modello; si crea una scheda PDF con boom ed elementi
            string[] montNomi = mont;
            bApp.Click += async (s, e) =>
            {
                ricalcola();
                double fr = ParseNum(tF.Text, f), db = ParseNum(tB.Text, 40), de = ParseNum(tE.Text, 6), dist = ParseNum(tDist.Text, 10);
                double lam = 299.792458 / Math.Max(0.1, fr) * 1000;
                double dEq = db * (quadro == 1 ? 1.18 : 1);
                string nomeMod = model == null ? "" : (string.IsNullOrEmpty(model.FileName) ? "modello" : Path.GetFileNameWithoutExtension(model.FileName));
                SchedaPdf pdf = new SchedaPdf("Correzione del boom" + (nomeMod.Length > 0 ? " - " + nomeMod : ""),
                    "YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture) + " - Costantino Cerrotta IC8TEM");
                pdf.Sezione("Boom");
                pdf.Dato("Frequenza", FN(fr, 3) + " MHz (lambda " + FN(lam, 1) + " mm)");
                pdf.Dato("Forma del boom", quadro == 1 ? "quadro (lato x 1,18, SM5BSZ)" : "tondo");
                pdf.Dato("Diametro / lato del boom", FN(db, 1) + " mm" + (quadro == 1 ? " (equivalente " + FN(dEq, 1) + " mm)" : "") + " = " + (dEq / lam).ToString("0.0000", CultureInfo.CurrentCulture) + " lambda");
                pdf.Dato("Montaggio degli elementi", montNomi[montaggio] + (montaggio == 2 ? " " + FN(dist, 0) + " mm" : ""));
                pdf.Dato("Metodo", "DL6WU / G3SEK (formula)");
                pdf.Dato("Correzione su tutto l'elemento", FN(corr, 1) + " mm (per lato " + FN(corr / 2, 1) + " mm)");
                pdf.Dato("Se il montaggio fosse isolato", FN(CorrezioneBoom(fr, db, quadro == 1, 1, dist), 1) + " mm");
                pdf.Dato("Validita' della formula", "boom fino a 0,055 lambda = " + FN(0.055 * lam / (quadro == 1 ? 1.18 : 1), 0) + " mm" + (dEq / lam > 0.055 ? "  (SUPERATA)" : "  (rispettata)"));
                pdf.Dato("Verso", verso == 1 ? "rilievo -> modello elettrico per il NEC (correzione tolta)" : "progetto NEC -> elementi da realizzare (correzione aggiunta)");
                pdf.Dato("Applicata a", cApp.SelectedItem?.ToString() ?? "");
                pdf.Sezione("Elementi");
                pdf.Dato("Diametro degli elementi", FN(de, 1) + " mm");
                if (elems.Count == 0) pdf.Testo(model == null ? "Nessun modello aperto: la tabella degli elementi si riempie aprendo un modello nella finestra principale." : "Nel modello aperto non ci sono elementi diritti lunghi almeno lambda/4.");
                else
                {
                    List<string[]> tabPdf = new List<string[]>();
                    tabPdf.Add(verso == 1 ? new[] { "Elemento", "Posizione (mm)", "Disegno (mm)", "Correzione (mm)", "Nel calcolo (mm)", "Per lato (mm)" }
                                       : new[] { "Elemento", "Posizione (mm)", "Calcolo (mm)", "Correzione (mm)", "Da costruire (mm)", "Per lato (mm)" });
                    double p0 = elems[0].Pos;
                    foreach (ElBoom el in elems)
                    {
                        double dc = verso == 1 ? -el.Corr : el.Corr, dopo = el.Lung * 1000 + dc;
                        tabPdf.Add(new[] { el.Nome, FN(Math.Abs(el.Pos - p0) * 1000, 1), FN(el.Lung * 1000, 1), el.Corr > 0 ? FN(dc, 1) : (el.Sorgente ? "0 (radiatore)" : "0"), FN(dopo, 1), FN(dopo / 2, 1) });
                    }
                    pdf.Tabella(tabPdf, new[] { 1.3f, 1.1f, 1.1f, 1.2f, 1.25f, 1.0f });
                    pdf.Testo("Posizione: distanza lungo il boom dal primo elemento (riflettore). Per lato: meta' della misura corretta, dal centro del boom.");
                }
                pdf.Sezione("Formule e fonti");
                pdf.Testo("Passante conduttivo: C = (25,195 B - 229 B^2) x diametro del boom, B = diametro del boom in lunghezze d'onda (DL6WU, fit di G3SEK), valida fino a B = 0,055. " +
                    "Passante isolato: meta'. Boom quadro: lato x 1,18 (SM5BSZ). Sopra il boom con staffa conduttiva: circa 0,65 (DG7YBN on-boom). " +
                    "La formula non tiene conto del diametro degli elementi, della distanza dalle estremita' del boom ne' della segmentazione del modello (DG7YBN). " +
                    "Confronto a 144 MHz con boom da 25 mm isolato passante: DL6WU/G3SEK 3,7 mm, VE7BQH 3,47, I0JXX 2,50, DJ9BV (quadro) 3,0, DG7YBN on-boom 3,9: tutte entro 1-1,5 mm.");
                try
                {
                    IStorageFile fl = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Crea la scheda PDF del boom",
                        SuggestedFileName = "Boom" + (nomeMod.Length > 0 ? " " + nomeMod : "") + ".pdf",
                        DefaultExtension = "pdf",
                        FileTypeChoices = new[] { new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } }
                    });
                    if (fl == null) return;
                    using (Stream st = await fl.OpenWriteAsync()) pdf.Scrivi(st);
                    Registro.Scrivi("AZIONE", "scheda PDF del boom: " + fl.Name);
                    Status("Scheda PDF del boom creata: " + fl.Name);
                }
                catch (Exception ex) { Status("Errore nella scheda PDF: " + ex.Message, true); }
            };
            // modello elettrico dal rilievo: copia del modello con gli elementi scelti accorciati della correzione, meta' per lato
            Func<NecModel, int[], NecModel> creaElettrico = (sorg, conta) =>
            {
                NecModel m = sorg.Clone();
                int n = 0;
                foreach (ElBoom el in elems)
                {
                    if (el.Corr <= 0) continue;
                    GeomCard g = m.Geometry[el.Card];
                    double L = Math.Sqrt((g.F4 - g.F1) * (g.F4 - g.F1) + (g.F5 - g.F2) * (g.F5 - g.F2) + (g.F6 - g.F3) * (g.F6 - g.F3));
                    double dl = el.Corr / 1000.0;       // mm -> unita' del modello (metri, come nella tabella)
                    if (L <= dl) continue;
                    double k = (dl / 2) / L;            // meta' per lato
                    double ux = (g.F4 - g.F1) * k, uy = (g.F5 - g.F2) * k, uz = (g.F6 - g.F3) * k;
                    g.F1 += ux; g.F2 += uy; g.F3 += uz; g.F4 -= ux; g.F5 -= uy; g.F6 -= uz;
                    string[] src = g.Src != null ? (string[])g.Src.Clone() : new string[9];
                    if (g.Src == null) { src[0] = g.I1.ToString(CultureInfo.InvariantCulture); src[1] = g.I2.ToString(CultureInfo.InvariantCulture); src[8] = g.F7.ToString("R", CultureInfo.InvariantCulture); }
                    double[] v = { g.F1, g.F2, g.F3, g.F4, g.F5, g.F6 };
                    for (int j = 0; j < 6; j++) src[2 + j] = Math.Round(v[j], 7).ToString("0.#######", CultureInfo.InvariantCulture);
                    g.Src = src;
                    n++;
                }
                List<string> err = m.Reevaluate();
                if (err.Count > 0) throw new NecException(err[0]);
                if (conta != null && conta.Length > 0) conta[0] = n;
                return m;
            };
            bElet.Click += (s, e) =>
            {
                ricalcola();
                if (model == null || verso != 1) return;
                try
                {
                    int[] cn = new int[1];
                    NecModel m = creaElettrico(model, cn);
                    int n = cn[0];
                    string nome = string.IsNullOrEmpty(model.FileName) ? "modello" : Path.GetFileNameWithoutExtension(model.FileName);
                    m.Comments.Add("YA-NEC correzione del boom (-): " + FN(corr, 1) + " mm tolti a " + n + " elementi (boom " + FN(ParseNum(tB.Text, 40), 1) +
                        " mm, " + montNomi[montaggio] + ", DL6WU/G3SEK)");
                    m.FileName = nome + " (elettrico, boom -" + FN(corr, 1) + " mm).nec";
                    SetModel(m, null);
                    Registro.Scrivi("AZIONE", "correzione del boom in negativo: " + n + " elementi accorciati di " + FN(corr, 1) + " mm -> " + m.FileName);
                    int ti = -1;
                    for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == "Struttura 3D") ti = i;
                    if (ti >= 0) SelectTab(ti);
                    Status("Modello elettrico creato (" + n + " elementi accorciati di " + FN(corr, 1) + " mm): " + m.FileName + ". Salvalo con Salva con nome.");
                    Calculate(true);
                }
                catch (Exception ex) { Status("Errore nel modello elettrico: " + ex.Message, true); }
            };
            bRil.Click += (s, e) =>
            {
                if (model == null) return;
                Registro.Scrivi("AZIONE", "correzione del boom: calcolo del rilievo cosi' com'e'");
                Status("Calcolo del modello aperto cosi' com'e' (rilievo, senza correzione del boom).");
                Calculate(true);
            };
            // confronto: risonanza (X = 0 entro +-10 %, secanti), ROS su Z0 del modello, guadagno e F/R a 5 gradi
            Func<NecModel, double, double[]> misura = (m, fr) =>
            {
                Func<double, Complex> zin = fq =>
                {
                    SegmentGeometry g = m.BuildGeometry();
                    Nec2Engine en = new Nec2Engine(g);
                    en.Solve(fq, Analysis.EffectiveSources(m, g, fq), m.EffectiveLoads(), TerreniEdifici.Alla(m.Ground, fq), m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(fq));
                    return en.Sources[0].Z;
                };
                double fa = fr, fb = fr * 1.01, xa = zin(fa).Imaginary, xb = zin(fb).Imaginary, ris = double.NaN;
                for (int it = 0; it < 12; it++)
                {
                    if (Math.Abs(xb - xa) < 1e-12) break;
                    double fc = fb - xb * (fb - fa) / (xb - xa);
                    if (double.IsNaN(fc) || fc < fr * 0.9 || fc > fr * 1.1) break;
                    fa = fb; xa = xb; fb = fc; xb = zin(fb).Imaginary;
                    if (Math.Abs(xb) < 0.01) { ris = fb; break; }
                }
                if (double.IsNaN(ris) && Math.Abs(xb) < 1.0 && fb >= fr * 0.9 && fb <= fr * 1.1) ris = fb;
                SegmentGeometry g0 = m.BuildGeometry();
                RunResult r = Analysis.Run(m, g0, fr, 5.0);
                double z0 = m.Z0 > 0 ? m.Z0 : 50;
                double gam = Complex.Abs((r.Zin - z0) / (r.Zin + z0)), swr = gam < 0.999 ? (1 + gam) / (1 - gam) : 99;
                return new[] { ris, swr, r.GmaxDbi, r.FrontRear };
            };
            bConf.Click += async (s, e) =>
            {
                ricalcola();
                if (model == null || verso != 1) return;
                try
                {
                    double fr = ParseNum(tF.Text, f);
                    NecModel ril = model.Clone(), el = creaElettrico(model, null);
                    string key = model.ToNec() + "|" + FN(corr, 3) + "|" + applica;
                    bConf.IsEnabled = false;
                    Status("Confronto: calcolo del rilievo cosi' com'e' e del modello elettrico...");
                    double[] a = await Task.Run(() => misura(ril, fr));
                    double[] b = await Task.Run(() => misura(el, fr));
                    confRil = a; confEl = b; confKey = key;
                    Registro.Scrivi("CALCOLO", string.Format(CultureInfo.InvariantCulture, "correzione del boom, confronto: rilievo risonanza {0:0.###} MHz ROS {1:0.00} G {2:0.00} F/R {3:0.0}; elettrico {4:0.###} MHz ROS {5:0.00} G {6:0.00} F/R {7:0.0}",
                        a[0], a[1], a[2], a[3], b[0], b[1], b[2], b[3]));
                    Status("Confronto fatto: rilievo e modello elettrico calcolati (il modello aperto non e' cambiato).");
                }
                catch (Exception ex) { Status("Confronto non riuscito: " + ex.Message, true); }
                finally { ricalcola(); }
            };
            bCsv.Click += async (s, e) =>
            {
                ricalcola();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(verso == 1 ? "elemento;rilievo_nel_NEC_mm;correzione_mm;elettrico_per_il_NEC_mm;differenza_%" : "elemento;filo_NEC_mm;correzione_mm;da_realizzare_mm;per_lato_mm");
                CultureInfo ci = CultureInfo.InvariantCulture;
                foreach (ElBoom el in elems)
                {
                    double mis = el.Lung * 1000, dc = verso == 1 ? -el.Corr : el.Corr, dopo = mis + dc;
                    sb.AppendLine(el.Nome + ";" + mis.ToString("0.0", ci) + ";" + dc.ToString("0.0", ci) + ";" + dopo.ToString("0.0", ci) + ";" + (verso == 1 ? (dc / mis * 100).ToString("0.00", ci) : (dopo / 2).ToString("0.0", ci)));
                }
                try
                {
                    IStorageFile fl = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Esporta la tabella del boom", SuggestedFileName = "Boom elementi.csv", DefaultExtension = "csv",
                        FileTypeChoices = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } }
                    });
                    if (fl == null) return;
                    using (Stream st = await fl.OpenWriteAsync()) using (StreamWriter w = new StreamWriter(st, new UTF8Encoding(true))) w.Write(sb.ToString());
                    Registro.Scrivi("AZIONE", "tabella del boom esportata: " + fl.Name);
                    Status("Tabella del boom esportata: " + fl.Name);
                }
                catch (Exception ex) { Status("Esportazione non riuscita: " + ex.Message, true); }
            };
            bCopia.Click += async (s, e) =>
            {
                StringBuilder sb = new StringBuilder("Elemento\tMisura (mm)\tCorrezione (mm)\tDopo (mm)\n");
                foreach (ElBoom el in elems) sb.AppendLine(el.Nome + "\t" + FN(el.Lung * 1000, 1) + "\t" + FN(verso == 1 ? -el.Corr : el.Corr, 1) + "\t" + FN(el.Lung * 1000 + (verso == 1 ? -el.Corr : el.Corr), 1));
                try { if (Clipboard != null) await Clipboard.SetTextAsync(sb.ToString()); Status("Tabella del boom copiata."); } catch { }
            };
            ricalcola();
            root.AttachedToVisualTree += (s, e) => ricalcola();   // il modello puo' essere cambiato nel frattempo
            return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }
    }
}
