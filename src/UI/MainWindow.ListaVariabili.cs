// YA-NEC 1.0.10 - lista a comparsa delle variabili nella barra Geometria (IC8TEM 16/11/2026 22:39 "help a comparsa per la lista delle
// variabili", bozzetto approvato alle 22:42): nome, formula, valore, descrizione o uso; ricerca; clic sul nome = lo scrive nella cella
// attiva della tabella dei fili (al cursore); collegamento alla linguetta Variabili.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        void MostraVariabili(Control dove)
        {
            WireRow riga = activeRow; int col = activeCol;
            StackPanel sp = new StackPanel { Spacing = 6, Width = 620 };
            TextBlock tit = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore) };
            tit.Inlines.Add(new Avalonia.Controls.Documents.Run { Text = "Variabili del modello", FontWeight = FontWeight.Bold });
            tit.Inlines.Add(new Avalonia.Controls.Documents.Run { Text = " - clic su un nome: lo scrive nella cella attiva della tabella dei fili", Foreground = Pal.B(Pal.Muted) });
            sp.Children.Add(tit);
            TextBox cerca = new TextBox { Watermark = "cerca...", Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
            sp.Children.Add(cerca);
            Grid tab = new Grid { ColumnDefinitions = new ColumnDefinitions("90,150,90,*") };
            ScrollViewer sv = new ScrollViewer { Content = tab, MaxHeight = 380 };
            sp.Children.Add(sv);
            Flyout fl = new Flyout { Content = new Border { Child = sp, Padding = new Thickness(4) }, Placement = PlacementMode.BottomEdgeAlignedLeft };
            Action disegna = () =>
            {
                tab.Children.Clear(); tab.RowDefinitions.Clear();
                string[] hh = { "Nome", "Formula", "Valore", "Descrizione / dove si usa" };
                tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int i = 0; i < 4; i++) { TextBlock t = new TextBlock { Text = hh[i], FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 8, 2), HorizontalAlignment = i == 2 ? HorizontalAlignment.Right : HorizontalAlignment.Left }; Grid.SetColumn(t, i); tab.Children.Add(t); }
                List<SyVar> vv = model != null ? model.Variables : new List<SyVar>();
                string q = (cerca.Text ?? "").Trim();
                int r = 1;
                foreach (SyVar v in vv)
                {
                    string uso = string.IsNullOrWhiteSpace(v.Descr) ? (model.DescribeUse(v.Name) ?? "") : v.Descr;
                    if (q.Length > 0 && v.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 && uso.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    string nome = v.Name;
                    TextBlock n = new TextBlock { Text = nome, FontFamily = Pal.Mono, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Accent), Cursor = new Cursor(StandardCursorType.Hand), Margin = new Thickness(0, 1, 8, 1) };
                    ToolTip.SetTip(n, "Scrive " + nome + " nella cella attiva della tabella dei fili");
                    n.PointerPressed += (s, e) =>
                    {
                        if (riga == null || riga.Cells == null || col < 0 || col >= riga.Cells.Length || riga.ReadOnly) { Status("Seleziona prima una cella della tabella dei fili, poi il nome della variabile.", true); return; }
                        TextBox tb = riga.Cells[col];
                        string t0 = tb.Text ?? "";
                        int c = Math.Max(0, Math.Min(t0.Length, tb.CaretIndex));
                        tb.Text = t0.Substring(0, c) + nome + t0.Substring(c);
                        tb.CaretIndex = c + nome.Length;
                        fl.Hide();
                        tb.Focus();
                    };
                    string[] val = { v.Expr, v.Error != null ? "errore" : FormatVal(v.Value), uso };
                    Grid.SetRow(n, r); tab.Children.Add(n);
                    for (int i = 0; i < 3; i++)
                    {
                        TextBlock t = new TextBlock { Text = val[i], FontFamily = i < 2 ? Pal.Mono : FontFamily.Default, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(i == 1 && v.Error != null ? Pal.Orange : Pal.Fore), Margin = new Thickness(0, 1, 8, 1), HorizontalAlignment = i == 1 ? HorizontalAlignment.Right : HorizontalAlignment.Left };
                        Grid.SetRow(t, r); Grid.SetColumn(t, i + 1); tab.Children.Add(t);
                    }
                    r++;
                }
                if (r == 1)
                {
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    TextBlock t = new TextBlock { Text = vv.Count == 0 ? "Nessuna variabile: si creano nella linguetta Variabili." : "Nessuna variabile corrisponde alla ricerca.", Foreground = Pal.B(Pal.Muted) };
                    Grid.SetRow(t, 1); Grid.SetColumnSpan(t, 4); tab.Children.Add(t);
                }
            };
            cerca.TextChanged += (s, e) => disegna();
            Button vai = Btn("Apri la linguetta Variabili →", false);
            vai.HorizontalAlignment = HorizontalAlignment.Left;
            vai.Click += (s, e) => { fl.Hide(); SelectTab(10); };
            sp.Children.Add(vai);
            disegna();
            fl.ShowAt(dove);
        }
    }
}
