// linguetta Manuale: mostra data/manuale/Manuale.md (lo stesso testo di doc/Manuale_d_uso.pdf), un capitolo alla volta
// con la grafica del documento di referenze. Formato del file (semplice):
//   # titolo   ## capitolo   ### paragrafo   righe di testo (**grassetto**, `codice`)
//   > NOTA: ...   > ATTENZIONE: ...   ![didascalia](immagini/file.png)   tabelle | a | b | con riga |---|---|
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

namespace YaNec
{
    public partial class MainWindow
    {
        internal readonly List<KeyValuePair<string, Control>> manualChapters = new List<KeyValuePair<string, Control>>();

        public static string ManualPath { get { return Path.Combine(AppPaths.DataDir, "manuale", "Manuale.md"); } }

        Control BuildManualPage()
        {
            manualChapters.Clear();
            StackPanel body = new StackPanel { Spacing = 8, Margin = new Thickness(26, 18, 26, 30), MaxWidth = 960, HorizontalAlignment = HorizontalAlignment.Left };
            Border head = new Border
            {
                Background = Pal.B(Pal.Track), CornerRadius = new CornerRadius(10), Padding = new Thickness(18, 12), Margin = new Thickness(0, 0, 0, 6),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "YA-NEC " + AppPaths.Version + " - Manuale d'uso", FontSize = 24, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.BarFore) },
                        new TextBlock { Text = "a cura di Costantino IC8TEM", Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 2, 0, 0) },
                    }
                }
            };
            string[] lines;
            string errore = null;
            try { lines = File.ReadAllLines(ManualPath); }
            catch (Exception ex) { errore = "Manuale non trovato (" + ManualPath + "): " + ex.Message; lines = new string[0]; }
            string imgDir = Path.GetDirectoryName(ManualPath) ?? "";
            // YA-NEC 0.4.10 (registro del 14/11/2026 00:51: prima apertura della linguetta ferma 774 ms con tutto il
            // manuale impaginato insieme): si impagina un capitolo alla volta, quando lo si apre, e si tiene in memoria
            List<int> inizi = new List<int>();
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("## ")) { inizi.Add(i); manualChapters.Add(new KeyValuePair<string, Control>(lines[i].Substring(3), null)); }
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
                    else if (l.StartsWith("### "))
                        cb.Children.Add(new TextBlock { Text = l.Substring(4), FontSize = 16, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap });
                    else if (l.StartsWith("> NOTA: ")) cb.Children.Add(Box(l.Substring(8), Pal.Accent));
                    else if (l.StartsWith("> ATTENZIONE: ")) cb.Children.Add(Box(l.Substring(14), Pal.Orange));
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
                            if (!Regex.IsMatch(lines[i], @"^\|(-+\|)+\s*$")) rows.Add(lines[i].Trim().Trim('|').Split('|'));
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
                    Button bp = Btn("< " + manualChapters[k - 1].Key, false);
                    bp.Margin = new Thickness(0, 0, 8, 0);
                    bp.Click += (s, e) => mostra(k - 1);
                    nav.Children.Add(bp);
                }
                if (k + 1 < inizi.Count)
                {
                    Button bs = Btn(manualChapters[k + 1].Key + " >", true);
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
            foreach (KeyValuePair<string, Control> c in manualChapters) idx.Items.Add(new ListBoxItem { Content = c.Key, FontSize = 13, Padding = new Thickness(10, 5) });
            idx.SelectionChanged += (s, e) => { if (idx.SelectedIndex >= 0) mostra(idx.SelectedIndex); };
            body.Children.Add(head);
            if (errore != null) body.Children.Add(Para(errore));
            else if (inizi.Count > 0) mostra(0);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("250,*") };
            Control left = Riquadro(new DockPanel
            {
                Children =
                {
                    new TextBlock { Text = "Indice", FontWeight = FontWeight.SemiBold, Margin = new Thickness(12, 10, 0, 6), [DockPanel.DockProperty] = Dock.Top },
                    idx
                }
            });
            sv.Content = body;
            Control right = Riquadro(sv);
            right.Margin = new Thickness(6, 0, 0, 0);
            Grid.SetColumn(right, 1);
            g.Children.Add(left);
            g.Children.Add(right);
            return g;
        }

        // testo con **grassetto** e `codice`
        Control Para(string text)
        {
            SelectableTextBlock t = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), FontSize = 14, LineHeight = 21 };
            if (t.Inlines == null) t.Inlines = new InlineCollection();
            foreach (Match m in Regex.Matches(text, @"\*\*(.+?)\*\*|`(.+?)`|[^*`]+|[*`]"))
            {
                if (m.Groups[1].Success) t.Inlines.Add(new Run(m.Groups[1].Value) { FontWeight = FontWeight.Bold });
                else if (m.Groups[2].Success) t.Inlines.Add(new Run(m.Groups[2].Value) { FontFamily = Pal.Mono, FontSize = 13 });
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
                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);
                    g.Children.Add(cell);
                }
            }
            return g;
        }
    }
}
