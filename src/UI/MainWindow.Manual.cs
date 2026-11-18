// linguetta Documenti tecnici (1.4; prima Manuale): data/manuale/Manuale.md, Calcolo.md e Citazioni.md (gli stessi testi dei PDF in doc/), un capitolo alla volta
// con la grafica del documento di referenze. Formato del file (semplice):
//   # titolo   ## capitolo   ### paragrafo   righe di testo (**grassetto**, `codice`)
//   > NOTA: ...   > ATTENZIONE: ...   > FORMULA: ... (1.4)   [C12] rimando alla citazione (1.4)   ![didascalia](immagini/file.png)   tabelle | a | b | con riga |---|---|
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace YaNec
{
    public partial class MainWindow
    {
        internal readonly List<KeyValuePair<string, Control>> manualChapters = new List<KeyValuePair<string, Control>>();

        public static string ManualPath { get { return Path.Combine(AppPaths.DataDir, "manuale", "Manuale.md"); } }

        // 1.4 (bozzetto Documenti tecnici approvato da IC8TEM il 18/11/2026 14:54): la linguetta Manuale diventa Documenti tecnici
        // con tre documenti: manuale d'uso, manuale di calcolo e delle fonti, citazioni. Ognuno con il suo indice e il suo PDF.
        public static readonly string[] DocFile = { "Manuale.md", "Calcolo.md", "Citazioni.md" };
        public static readonly string[] DocTitolo = { "Manuale d'uso", "Manuale di calcolo e delle fonti", "Citazioni" };
        public static readonly string[] DocPdf = { "Manuale_d_uso.pdf", "Manuale_di_calcolo_e_delle_fonti.pdf", "Citazioni.pdf" };
        public static string DocPath(int d) { return Path.Combine(AppPaths.DataDir, "manuale", DocFile[d]); }

        Action<string> manualeVai;               // manuale d'uso, al paragrafo con questo titolo
        Action<int, string> documentoVai;        // documento d, al titolo o alla citazione (C12)
        internal int documentoAperto;
        internal readonly int[] capitoliDoc = new int[3];

        /// <summary>1.3.1: apre il manuale d'uso al paragrafo con questo titolo (senza i cancelletti).</summary>
        void ApriManuale(string titolo)
        {
            for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == "Documenti tecnici") { SelectTab(i); break; }
            documentoVai?.Invoke(0, titolo);
        }

        /// <summary>1.4: apre un documento tecnico (0 uso, 1 calcolo, 2 citazioni) al titolo o alla citazione.</summary>
        internal void ApriDocumento(int d, string chiave)
        {
            for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == "Documenti tecnici") { SelectTab(i); break; }
            documentoVai?.Invoke(d, chiave);
        }

        /// <summary>1.3.1: pulsantino "? Manuale" accanto al titolo di un riquadro.</summary>
        Button BtnManuale(string titolo)
        {
            Button b = new Button
            {
                Content = Lang.T("? Manuale"), FontSize = 12, Padding = new Thickness(9, 1), Margin = new Thickness(8, 0, 0, 0), CornerRadius = new CornerRadius(10),
                Background = Brushes.Transparent, BorderBrush = Pal.B(Pal.Accent), BorderThickness = new Thickness(1), Foreground = Pal.B(Pal.Accent), VerticalAlignment = VerticalAlignment.Center
            };
            ToolTip.SetTip(b, Lang.T("Apre la spiegazione nel manuale"));
            b.Click += (s, e) => ApriManuale(titolo);
            return b;
        }

        static Control CercaTag(Control c, string tag)
        {
            if (Equals(c.Tag, tag)) return c;
            if (c is Panel p) foreach (Control x in p.Children) { Control r = CercaTag(x, tag); if (r != null) return r; }
            if (c is Decorator dc && dc.Child is Control cc) return CercaTag(cc, tag);
            return null;
        }

        /// <summary>Un documento: indice a sinistra, un capitolo alla volta a destra. vai(chiave) porta al titolo o alla riga "| C12 |".</summary>
        Control BuildDocumento(int d, out Action<string> vai, out ListBox idxOut, out List<string> capitoliOut, out string[] righeOut)
        {
            List<string> capitoli = new List<string>();
            StackPanel body = new StackPanel { Spacing = 8, Margin = new Thickness(26, 18, 26, 30), MaxWidth = 960, HorizontalAlignment = HorizontalAlignment.Left };
            Border head = new Border
            {
                Background = Pal.B(Pal.Track), CornerRadius = new CornerRadius(10), Padding = new Thickness(18, 12), Margin = new Thickness(0, 0, 0, 6),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "YA-NEC " + AppPaths.Version + " - " + Lang.T(DocTitolo[d]), FontSize = 24, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.BarFore), TextWrapping = TextWrapping.Wrap },
                        new TextBlock { Text = "a cura di Costantino IC8TEM", Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 2, 0, 0) },
                    }
                }
            };
            string[] lines;
            string errore = null;
            string path = DocPath(d);
            try { lines = File.ReadAllLines(path); }
            catch (Exception ex) { errore = Lang.T(DocTitolo[d]) + ": file non trovato (" + path + "): " + ex.Message; lines = new string[0]; }
            righeOut = lines;
            string imgDir = Path.GetDirectoryName(path) ?? "";
            // YA-NEC 0.4.10: si impagina un capitolo alla volta, quando lo si apre, e si tiene in memoria
            List<int> inizi = new List<int>();
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("## ")) { inizi.Add(i); capitoli.Add(lines[i].Substring(3)); }
            Dictionary<int, Control> fatti = new Dictionary<int, Control>();
            ScrollViewer sv = new ScrollViewer { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
            ListBox idx = new ListBox { Background = Pal.B(Pal.Panel), Margin = new Thickness(0, 0, 6, 0) };
            Action<int> mostra = null;
            Func<int, Control> capitolo = k =>
            {
                Control pronto;
                if (fatti.TryGetValue(k, out pronto)) return pronto;
                StackPanel cb = new StackPanel { Spacing = 8 };
                int da = inizi[k], a = k + 1 < inizi.Count ? inizi[k + 1] : lines.Length;
                for (int i = da; i < a; i++)
                {
                    string l = lines[i];
                    if (l.StartsWith("# ")) continue;
                    if (l.StartsWith("## "))
                    {
                        TextBlock t = new TextBlock { Text = l.Substring(3), FontSize = 20, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Accent), TextWrapping = TextWrapping.Wrap };
                        cb.Children.Add(new Border { BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 2), Padding = new Thickness(0, 0, 0, 4), Margin = new Thickness(0, 6, 0, 2), Child = t });
                    }
                    else if (l.StartsWith("#### "))   // 1.3.1: paragrafi; Tag per il pulsante "? Manuale"
                        cb.Children.Add(new TextBlock { Text = l.Substring(5), Tag = l.Substring(5), FontSize = 14.5, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap });
                    else if (l.StartsWith("### "))
                        cb.Children.Add(new TextBlock { Tag = l.Substring(4), Text = l.Substring(4), FontSize = 16, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap });
                    else if (l.StartsWith("> NOTA: ")) cb.Children.Add(Box(l.Substring(8), Pal.Accent));
                    else if (l.StartsWith("> ATTENZIONE: ")) cb.Children.Add(Box(l.Substring(14), Pal.Orange));
                    else if (l.StartsWith("> FORMULA: "))   // 1.4: formule del manuale di calcolo, in un riquadro con il carattere a spaziatura fissa
                        cb.Children.Add(new Border
                        {
                            Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(14, 8), Margin = new Thickness(0, 2), HorizontalAlignment = HorizontalAlignment.Left,
                            Child = new SelectableTextBlock { Text = l.Substring(11), FontFamily = Pal.Mono, FontSize = 14, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap }
                        });
                    else if (l.StartsWith("!["))
                    {
                        Match m = Regex.Match(l, @"!\[(.*)\]\((.*)\)");
                        if (!m.Success) continue;
                        try
                        {
                            Bitmap bmp = new Bitmap(Path.Combine(imgDir, m.Groups[2].Value.Replace('/', Path.DirectorySeparatorChar)));
                            cb.Children.Add(new Border
                            {
                                BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ClipToBounds = true,
                                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0),
                                Child = new Image { Source = bmp, MaxWidth = 900, Stretch = Stretch.Uniform }
                            });
                        }
                        catch (Exception ex) { cb.Children.Add(Para("(immagine non trovata: " + m.Groups[2].Value + " - " + ex.Message + ")")); }
                        cb.Children.Add(new TextBlock { Text = m.Groups[1].Value, FontSize = 12, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
                    }
                    else if (l.StartsWith("|"))
                    {
                        List<string[]> rows = new List<string[]>();
                        while (i < a && lines[i].StartsWith("|"))
                        {
                            if (!Regex.IsMatch(lines[i], @"^\|(\s*:?-+:?\s*\|)+\s*$")) rows.Add(lines[i].Trim().Trim('|').Split('|'));
                            i++;
                        }
                        i--;
                        cb.Children.Add(Table(rows));
                    }
                    else if (l.Trim().Length > 0) cb.Children.Add(Para(l));
                }
                // in fondo: capitolo precedente e seguente
                WrapPanel nav = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
                if (k > 0)
                {
                    Button bp = Btn("< " + capitoli[k - 1], false);
                    bp.Margin = new Thickness(0, 0, 8, 0);
                    bp.Click += (s, e) => mostra(k - 1);
                    nav.Children.Add(bp);
                }
                if (k + 1 < inizi.Count)
                {
                    Button bs = Btn(capitoli[k + 1] + " >", true);
                    bs.Click += (s, e) => mostra(k + 1);
                    nav.Children.Add(bs);
                }
                cb.Children.Add(nav);
                fatti[k] = cb;
                return cb;
            };
            mostra = k =>
            {
                if (k < 0 || k >= inizi.Count) return;
                body.Children.Clear();
                body.Children.Add(head);
                body.Children.Add(capitolo(k));
                sv.Offset = new Vector(0, 0);
                if (idx.SelectedIndex != k) idx.SelectedIndex = k;
            };
            // 1.3.1: al paragrafo con questo titolo; 1.4: anche alla riga della citazione ("C12": riga che comincia con "| C12 |")
            vai = chiave =>
            {
                int li = Array.FindIndex(lines, x => x.StartsWith("#") && x.TrimStart('#').Trim() == chiave);
                if (li < 0) li = Array.FindIndex(lines, x => x.StartsWith("|") && x.Trim().Trim('|').Split('|')[0].Trim() == chiave);
                if (li < 0) li = Array.FindIndex(lines, x => x.IndexOf(chiave, StringComparison.OrdinalIgnoreCase) >= 0);
                if (li < 0) return;
                int k = inizi.FindLastIndex(x => x <= li);
                if (k < 0) return;
                mostra(k);
                // il titolo piu' vicino sopra la riga trovata (per la ricerca nel testo)
                string tag = chiave;
                if (!lines[li].StartsWith("#") && !lines[li].StartsWith("|"))
                {
                    tag = null;
                    for (int j = li; j > inizi[k]; j--) if (lines[j].StartsWith("### ") || lines[j].StartsWith("#### ")) { tag = lines[j].TrimStart('#').Trim(); break; }
                    if (tag == null) return;
                }
                Dispatcher.UIThread.Post(() =>
                {
                    Control x = fatti.TryGetValue(k, out Control cc) ? CercaTag(cc, tag) : null;
                    if (x == null) return;
                    // il paragrafo in cima alla pagina (BringIntoView lo lasciava in fondo)
                    Point? pt = x.TranslatePoint(new Point(0, 0), body);
                    if (pt.HasValue) sv.Offset = new Vector(0, Math.Max(0, pt.Value.Y - 8)); else x.BringIntoView();
                    if (x is Border bx) { IBrush vecchio = bx.Background; bx.Background = Pal.B(Pal.A(60, Pal.Yellow)); DispatcherTimer.RunOnce(() => bx.Background = vecchio, TimeSpan.FromSeconds(2)); }
                }, DispatcherPriority.Loaded);
            };
            foreach (string c in capitoli) idx.Items.Add(new ListBoxItem { Content = c, FontSize = 13, Padding = new Thickness(10, 5) });
            idx.SelectionChanged += (s, e) => { if (idx.SelectedIndex >= 0) mostra(idx.SelectedIndex); };
            body.Children.Add(head);
            if (errore != null) body.Children.Add(Para(errore));
            else if (inizi.Count > 0) mostra(0);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("250,*") };
            Control left = Riquadro(new DockPanel
            {
                Children =
                {
                    new TextBlock { Text = Lang.T("Indice"), FontWeight = FontWeight.SemiBold, Margin = new Thickness(12, 10, 0, 6), [DockPanel.DockProperty] = Dock.Top },
                    idx
                }
            });
            sv.Content = body;
            Control right = Riquadro(sv);
            right.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(right, 1);
            g.Children.Add(left);
            g.Children.Add(right);
            idxOut = idx;
            capitoliOut = capitoli;
            return g;
        }

        /// <summary>1.4: linguetta Documenti tecnici: tre pillole, Cerca in tutti i documenti, Apri il PDF.</summary>
        Control BuildManualPage()
        {
            manualChapters.Clear();
            Control[] viste = new Control[3];
            Action<string>[] vai = new Action<string>[3];
            ListBox[] indici = new ListBox[3];
            string[][] righe = new string[3][];
            List<string>[] capitoli = new List<string>[3];
            for (int d = 0; d < 3; d++)
            {
                viste[d] = BuildDocumento(d, out vai[d], out indici[d], out capitoli[d], out righe[d]);
                viste[d].IsVisible = d == 0;
                capitoliDoc[d] = capitoli[d].Count;
            }
            foreach (string c in capitoli[0]) manualChapters.Add(new KeyValuePair<string, Control>(c, null));
            manualeVai = t => vai[0](t);

            Grid area = new Grid();
            foreach (Control v in viste) area.Children.Add(v);
            // risultati della ricerca: sopra il documento, si chiudono con la x
            StackPanel risultati = new StackPanel { Spacing = 2 };
            Border boxRis = new Border
            {
                IsVisible = false, Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Accent), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6), Margin = new Thickness(0, 0, 0, 6), MaxHeight = 260,
                Child = new ScrollViewer { Content = risultati }
            };

            WrapPanel barra = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            Button[] pill = new Button[3];
            Action<int> scegli = d =>
            {
                documentoAperto = d;
                for (int i = 0; i < 3; i++)
                {
                    viste[i].IsVisible = i == d;
                    pill[i].Background = Pal.B(i == d ? Pal.Accent : Pal.Pill);
                    pill[i].Foreground = Pal.B(i == d ? Pal.AccentFore : Pal.PillFore);
                }
            };
            for (int d = 0; d < 3; d++)
            {
                int dd = d;
                pill[d] = new Button { Content = Lang.T(DocTitolo[d]), CornerRadius = new CornerRadius(14), Padding = new Thickness(14, 5), Margin = new Thickness(0, 0, 6, 0), BorderBrush = Pal.B(Pal.PillBorder), BorderThickness = new Thickness(1) };
                pill[d].Click += (s, e) => scegli(dd);
                barra.Children.Add(pill[d]);
            }
            TextBox cerca = new TextBox { Width = 240, Watermark = Lang.T("Cerca in tutti i documenti"), Margin = new Thickness(14, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
            Button bCerca = Btn(Lang.T("Cerca"), false);
            Button bPdf = Btn(Lang.T("Apri il PDF"), false);
            bPdf.Margin = new Thickness(14, 0, 0, 0);
            barra.Children.Add(cerca); barra.Children.Add(bCerca); barra.Children.Add(bPdf);

            Action faiRicerca = () =>
            {
                risultati.Children.Clear();
                string q = (cerca.Text ?? "").Trim();
                if (q.Length < 2) { boxRis.IsVisible = false; return; }
                DockPanel testa = new DockPanel();
                Button chiudi = new Button { Content = "x", Padding = new Thickness(8, 0), Background = Brushes.Transparent, [DockPanel.DockProperty] = Dock.Right };
                chiudi.Click += (s, e) => boxRis.IsVisible = false;
                testa.Children.Add(chiudi);
                TextBlock conta = new TextBlock { FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center };
                testa.Children.Add(conta);
                risultati.Children.Add(testa);
                int n = 0;
                for (int d = 0; d < 3 && n < 60; d++)
                {
                    string cap = "";
                    for (int i = 0; i < righe[d].Length && n < 60; i++)
                    {
                        string l = righe[d][i];
                        if (l.StartsWith("## ")) cap = l.Substring(3);
                        int p = l.IndexOf(q, StringComparison.OrdinalIgnoreCase);
                        if (p < 0 || l.StartsWith("# ")) continue;
                        string pezzo = l.Replace("**", "").Replace("`", "");
                        int pp = Math.Max(0, pezzo.IndexOf(q, StringComparison.OrdinalIgnoreCase) - 50);
                        pezzo = (pp > 0 ? "..." : "") + pezzo.Substring(pp, Math.Min(140, pezzo.Length - pp)) + (pezzo.Length - pp > 140 ? "..." : "");
                        int dd = d; string chiave = l.StartsWith("#") ? l.TrimStart('#').Trim() : l.StartsWith("|") ? l.Trim().Trim('|').Split('|')[0].Trim() : q;
                        if (!l.StartsWith("#") && !l.StartsWith("|")) chiave = l;
                        Button r = new Button
                        {
                            Background = Brushes.Transparent, Padding = new Thickness(4, 2), HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left,
                            Content = new TextBlock { TextWrapping = TextWrapping.Wrap, Inlines = new InlineCollection { new Run(Lang.T(DocTitolo[d]) + " › " + cap + ": ") { Foreground = Pal.B(Pal.Accent) }, new Run(pezzo) { Foreground = Pal.B(Pal.Fore) } } }
                        };
                        r.Click += (s, e) => { scegli(dd); vai[dd](chiave); };
                        risultati.Children.Add(r);
                        n++;
                    }
                }
                conta.Text = n == 0 ? Lang.F("Nessun risultato per «{0}»", q) : n >= 60 ? Lang.F("Primi {0} risultati per «{1}»", n, q) : Lang.F("{0} risultati per «{1}»", n, q);
                boxRis.IsVisible = true;
            };
            bCerca.Click += (s, e) => faiRicerca();
            cerca.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) faiRicerca(); };
            // il PDF del documento scelto: quello installato in data/manuale se c'e', altrimenti quello su GitHub
            bPdf.Click += (s, e) =>
            {
                string loc = Path.Combine(AppPaths.DataDir, "manuale", DocPdf[documentoAperto]);
                string url = File.Exists(loc) ? new Uri(loc).AbsoluteUri : "https://github.com/ic8tem/YA-NEC/blob/main/doc/" + DocPdf[documentoAperto];
                Registro.Scrivi("AZIONE", "Documenti tecnici: apri il PDF " + url);
                AboutWindow.OpenUrl(url);
            };
            documentoVai = (d, chiave) => { scegli(d); if (!string.IsNullOrEmpty(chiave)) vai[d](chiave); };
            citazioneVai = c => documentoVai(2, c);
            scegli(0);

            DockPanel dp = new DockPanel();
            DockPanel.SetDock(barra, Dock.Top);
            DockPanel.SetDock(boxRis, Dock.Top);
            dp.Children.Add(barra);
            dp.Children.Add(boxRis);
            dp.Children.Add(area);
            return dp;
        }

        Action<string> citazioneVai;

        // testo con **grassetto** e `codice`
        Control Para(string text)
        {
            SelectableTextBlock t = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), FontSize = 14, LineHeight = 21 };
            if (t.Inlines == null) t.Inlines = new InlineCollection();
            // 1.4: i rimandi [C12] aprono la citazione nel documento Citazioni
            foreach (Match m in Regex.Matches(text, @"\*\*(.+?)\*\*|`(.+?)`|\[(C\d+)\]|\[\[(Uso|Calcolo|Citazioni): ([^\]]+)\]\]|[^*`\[]+|[*`\[]"))
            {
                if (m.Groups[1].Success) t.Inlines.Add(new Run(m.Groups[1].Value) { FontWeight = FontWeight.Bold });
                else if (m.Groups[2].Success) t.Inlines.Add(new Run(m.Groups[2].Value) { FontFamily = Pal.Mono, FontSize = 13 });
                else if (m.Groups[3].Success)
                {
                    string c = m.Groups[3].Value;
                    TextBlock lk = new TextBlock { Text = "[" + c + "]", Foreground = Pal.B(Pal.Accent), FontSize = 13, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand), TextDecorations = TextDecorations.Underline };
                    ToolTip.SetTip(lk, Lang.F("Apre la citazione {0}", c));
                    lk.PointerPressed += (s, e) => citazioneVai?.Invoke(c);
                    t.Inlines.Add(new InlineUIContainer(lk) { BaselineAlignment = BaselineAlignment.TextBottom });
                }
                else if (m.Groups[4].Success)   // 1.4: [[Calcolo: 9.2 Matrice di accoppiamento]] apre il capitolo o il paragrafo dell'altro documento
                {
                    int dd = m.Groups[4].Value == "Uso" ? 0 : m.Groups[4].Value == "Calcolo" ? 1 : 2;
                    string tt = m.Groups[5].Value.Trim();
                    TextBlock lk = new TextBlock { Text = (dd == 1 ? Lang.T("Manuale di calcolo") + ", " : dd == 0 ? Lang.T("Manuale d'uso") + ", " : "") + tt, Foreground = Pal.B(Pal.Accent), FontSize = 14, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand), TextDecorations = TextDecorations.Underline, TextWrapping = TextWrapping.Wrap };
                    lk.PointerPressed += (s, e) => documentoVai?.Invoke(dd, tt);
                    t.Inlines.Add(new InlineUIContainer(lk) { BaselineAlignment = BaselineAlignment.TextBottom });
                }
                else t.Inlines.Add(new Run(m.Value));
            }
            return t;
        }

        Control Box(string text, Color c)
        {
            return new Border
            {
                Background = Pal.B(Pal.A(28, c)), BorderBrush = Pal.B(c), BorderThickness = new Thickness(4, 0, 0, 0),
                CornerRadius = new CornerRadius(4), Padding = new Thickness(12, 8), Margin = new Thickness(0, 4), Child = Para(text)
            };
        }

        Control Table(List<string[]> rows)
        {
            Grid g = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            int nc = 0;
            foreach (string[] r in rows) nc = Math.Max(nc, r.Length);
            for (int c = 0; c < nc; c++) g.ColumnDefinitions.Add(new ColumnDefinition(c == 0 && nc > 1 ? new GridLength(220) : new GridLength(1, GridUnitType.Star)));
            for (int r = 0; r < rows.Count; r++)
            {
                g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int c = 0; c < nc; c++)
                {
                    string txt = c < rows[r].Length ? rows[r][c].Trim() : "";
                    Control content = r == 0 ? new TextBlock { Text = txt, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore) } : Para(txt);
                    Border cell = new Border
                    {
                        BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(c == 0 ? 1 : 0, r == 0 ? 1 : 0, 1, 1), Padding = new Thickness(8, 5),
                        Background = r == 0 ? Pal.B(Pal.Panel2) : Brushes.Transparent, Child = content
                    };
                    if (c == 0 && r > 0) cell.Tag = txt;   // 1.4: per andare alla riga di una citazione (C12)
                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);
                    g.Children.Add(cell);
                }
            }
            return g;
        }
    }
}
