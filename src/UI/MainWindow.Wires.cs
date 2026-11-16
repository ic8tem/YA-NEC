// YA-NEC - tabella dei fili sotto la Struttura 3D (stile MMANA): una riga per filo (scheda GW) con le coordinate
// degli estremi, il raggio in mm, i segmenti e l'alimentazione. Le celle accettano anche variabili ed espressioni.
// Le altre schede di geometria (GA, GH, GM, GR, GX, GS, fili rastremati) compaiono in sola lettura e restano al loro posto.
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        class WireRow
        {
            public GeomCard Card;        // scheda originale (null = riga nuova)
            public bool ReadOnly;
            public TextBox[] Cells;      // 0 tag, 1..6 coordinate, 7 raggio mm, 8 segmenti
            public string[] Shown;       // testo mostrato all'apertura (per capire cosa e' stato cambiato)
            public TextBlock Num;
            public Border NumBox;
            public TextBox Len;
            public Grid Row;
        }

        // 0.4.8 (14/11/2026: con un modello di 348 fili l'interfaccia restava ferma 7,1 s): la tabella e' virtualizzata,
        // si disegnano solo le righe visibili. Ogni riga sta in un ContentPresenter suo (contenitore di se stesso, mai riciclato),
        // cosi' le celle restano le stesse TextBox anche quando la riga esce e rientra dalla vista.
        class RigheVirtuali
        {
            public readonly Avalonia.Collections.AvaloniaList<Control> Voci = new Avalonia.Collections.AvaloniaList<Control>();
            readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, Avalonia.Controls.Presenters.ContentPresenter> involucri = new System.Runtime.CompilerServices.ConditionalWeakTable<Control, Avalonia.Controls.Presenters.ContentPresenter>();
            public readonly ItemsControl Lista;
            public RigheVirtuali()
            {
                Lista = new ItemsControl
                {
                    ItemsSource = Voci,
                    ItemsPanel = new Avalonia.Controls.Templates.FuncTemplate<Panel>(() => new VirtualizingStackPanel())
                };
            }
            Avalonia.Controls.Presenters.ContentPresenter Involucro(Control c)
            {
                Avalonia.Controls.Presenters.ContentPresenter cp;
                if (!involucri.TryGetValue(c, out cp)) { cp = new Avalonia.Controls.Presenters.ContentPresenter { Content = c }; involucri.Add(c, cp); }
                return cp;
            }
            public void Clear() { Voci.Clear(); }
            public void Add(Control c) { Voci.Add(Involucro(c)); }
            /// <summary>1.0.2: porta l'elenco delle righe a quello voluto senza svuotarlo. Svuotare e rimettere le stesse righe lasciava
            /// vuote quelle gia' disegnate (il pannello virtualizzato non ridisegna un contenitore che gli torna dopo Clear): con
            /// "Aggiungi filo" ripetuto si vedeva solo l'ultima riga.</summary>
            public void Allinea(List<Control> righe)
            {
                List<Avalonia.Controls.Presenters.ContentPresenter> voluti = new List<Avalonia.Controls.Presenters.ContentPresenter>();
                foreach (Control c in righe) voluti.Add(Involucro(c));
                HashSet<Control> tieni = new HashSet<Control>(voluti);
                for (int i = Voci.Count - 1; i >= 0; i--) if (!tieni.Contains(Voci[i])) Voci.RemoveAt(i);
                for (int i = 0; i < voluti.Count; i++)
                {
                    if (i < Voci.Count && ReferenceEquals(Voci[i], voluti[i])) continue;
                    int j = Voci.IndexOf(voluti[i]);
                    if (j > i) Voci.Move(j, i);
                    else Voci.Insert(i, voluti[i]);
                }
                while (Voci.Count > voluti.Count) Voci.RemoveAt(Voci.Count - 1);
            }
            /// <summary>Porta la riga nella parte visibile (la crea se serve), poi esegue l'azione (di solito il fuoco su una cella).</summary>
            public void Mostra(Control c, Action poi)
            {
                Avalonia.Controls.Presenters.ContentPresenter cp;
                if (c != null && involucri.TryGetValue(c, out cp))
                {
                    int i = Voci.IndexOf(cp);
                    if (i >= 0) { try { Lista.ScrollIntoView(i); } catch { } }
                }
                if (poi != null) Avalonia.Threading.Dispatcher.UIThread.Post(poi, Avalonia.Threading.DispatcherPriority.Loaded);
            }
        }

        RigheVirtuali wiresPanel;
        readonly List<WireRow> wireRows = new List<WireRow>();
        // colonne: N., Tag, X1, Y1, Z1, X2, Y2, Z2, Raggio, Segm., Lungh., pulsanti di riga (colonna "Alim." tolta su richiesta del 13/11/2026: le sorgenti si gestiscono nella tabella Sorgenti)
        static readonly string WireCols = "34,52,76,76,76,76,76,76,92,60,72,104";
        static readonly string[] WireHeads = { "", "Tag", "X1 (m)", "Y1 (m)", "Z1 (m)", "X2 (m)", "Y2 (m)", "Z2 (m)", "Raggio mm ⌀▾", "Segm.", "Lungh. m", "riga" };
        static readonly string[] CellNames = { "Tag", "X1", "Y1", "Z1", "X2", "Y2", "Z2", "Raggio", "Segm." };
        Border[] wireHeadCells;
        TextBlock lblWiresTitle, lblFxName;
        TextBox txtFx;
        WireRow activeRow;
        int activeCol = -1, selFrom = -1, selTo = -1;
        Maniglia manigliaFili;
        bool fxSync;

        Control BuildWiresTable()
        {
            DockPanel d = new DockPanel();
            // titolo, pulsanti e suggerimento
            WrapPanel bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            lblWiresTitle = new TextBlock { Text = "Fili", FontSize = 15, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), Foreground = Pal.B(Pal.Fore) };
            bar.Children.Add(lblWiresTitle);
            Button bAdd = Btn("Aggiungi filo", false);
            bAdd.Click += (s, e) => AddWireAfter(wireRows.Count - 1);
            Button bApply = Btn("Applica e ricalcola", true);
            bApply.Click += (s, e) => ApplyWires();
            bar.Children.Add(bAdd);
            bar.Children.Add(bApply);
            // 1.0.9: elemento simmetrico (MainWindow.Simmetrico.cs)
            Button bSim = Btn("Ins. elemento simmetrico", false);
            bSim.Margin = new Thickness(6, 0, 0, 0);
            ToolTip.SetTip(bSim, "Inserisce un filo diritto centrato sul boom: X, lunghezza totale, quota, raggio e segmenti; Y1 = -L/2, Y2 = +L/2.");
            bar.Children.Add(bSim);
            bar.Children.Add(new TextBlock
            {
                Text = "come in Excel: frecce e Tab, Invio applica, Ctrl+C / Ctrl+V anche su più righe, Canc svuota",
                Foreground = Pal.B(Pal.Muted), FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0), TextWrapping = TextWrapping.Wrap
            });
            DockPanel.SetDock(bar, Dock.Top);
            d.Children.Add(bar);
            Control pSim = BuildPannelloSimmetrico();
            DockPanel.SetDock(pSim, Dock.Top);
            d.Children.Add(pSim);
            bSim.Click += (s, e) => { if (!simPannello.IsVisible) { simPannello.IsVisible = true; if (simPrecompila != null) simPrecompila(); } else simPannello.IsVisible = false; };

            // barra della formula: "riga 3, Y1 | fx | valore"
            Grid fx = new Grid { ColumnDefinitions = new ColumnDefinitions("90,34,*,Auto"), Margin = new Thickness(0, 0, 0, 6) };
            Border fxBox = new Border { BorderBrush = new SolidColorBrush(XlGriglia), BorderThickness = new Thickness(1), Background = Brushes.White, Child = fx, Margin = new Thickness(0, 0, 0, 6) };
            fx.Margin = new Thickness(0);
            lblFxName = new TextBlock { Text = "", FontSize = 12.5, Foreground = new SolidColorBrush(Color.Parse("#444444")), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
            Border b0 = new Border { BorderBrush = new SolidColorBrush(XlGriglia), BorderThickness = new Thickness(0, 0, 1, 0), Child = lblFxName };
            Border b1 = new Border { BorderBrush = new SolidColorBrush(XlGriglia), BorderThickness = new Thickness(0, 0, 1, 0),
                Child = new TextBlock { Text = "fx", FontStyle = FontStyle.Italic, Foreground = new SolidColorBrush(Color.Parse("#888888")), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            txtFx = new TextBox { FontFamily = Pal.Mono };
            txtFx.Classes.Add("xl");
            txtFx.Classes.Add("l");
            txtFx.BorderThickness = new Thickness(0);
            txtFx.TextChanged += (s, e) =>
            {
                if (fxSync || activeRow == null || activeRow.Cells == null || activeCol < 0) return;
                fxSync = true;
                activeRow.Cells[activeCol].Text = txtFx.Text;
                fxSync = false;
            };
            txtFx.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) ApplyWires(); };
            TextBlock hint = new TextBlock { Text = "numero, variabile o formula", FontSize = 12, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
            Grid.SetColumn(b1, 1); Grid.SetColumn(txtFx, 2); Grid.SetColumn(hint, 3);
            fx.Children.Add(b0); fx.Children.Add(b1); fx.Children.Add(txtFx); fx.Children.Add(hint);
            DockPanel.SetDock(fxBox, Dock.Top);
            d.Children.Add(fxBox);

            // intestazioni di colonna (restano ferme quando la tabella scorre)
            Grid head = new Grid { ColumnDefinitions = new ColumnDefinitions(WireCols) };
            wireHeadCells = new Border[WireHeads.Length];
            for (int i = 0; i < WireHeads.Length; i++)
            {
                bool riga = i == WireHeads.Length - 1;
                Border hb = new Border
                {
                    Background = riga ? Brushes.Transparent : new SolidColorBrush(XlIntest),
                    BorderBrush = new SolidColorBrush(XlGriglia),
                    BorderThickness = riga ? new Thickness(0) : new Thickness(i == 0 ? 1 : 0, 1, 1, 1),
                    Padding = new Thickness(4, 3),
                    Child = new TextBlock { Text = WireHeads[i], FontSize = 12, Foreground = new SolidColorBrush(riga ? Color.Parse("#6A7686") : XlIntestTesto), TextAlignment = TextAlignment.Center }
                };
                if (i == 8)
                {
                    hb.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                    ToolTip.SetTip(hb, "Raggio da sezione: tondo, quadrato, rettangolo, piatto, U (formule Balanis, valore di Rothammel)");
                    hb.PointerPressed += (s, e) => ApriConvertitoreRaggio(hb);
                }
                Grid.SetColumn(hb, i);
                head.Children.Add(hb);
                wireHeadCells[i] = hb;
            }
            wiresPanel = new RigheVirtuali();
            DockPanel inner = new DockPanel();
            DockPanel.SetDock(head, Dock.Top);
            inner.Children.Add(head);
            inner.Children.Add(new ScrollViewer { Content = wiresPanel.Lista, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
            ScrollViewer hs = new ScrollViewer { Content = inner, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
            d.Children.Add(hs);
            // 1.0.11: maniglia di riempimento (come Excel); il riempimento si applica subito, Ctrl+Z lo toglie
            manigliaFili = new Maniglia(d, () => { List<TextBox[]> l = new List<TextBox[]>(); foreach (WireRow w in RigheModificabili()) l.Add(w.Cells); return l; },
                celle =>
                {
                    // 1.0.12: il riempimento va nella storia di Annulla della barra Geometria (Ctrl+Z o il pulsante)
                    NecModel prima = model;
                    ApplyWires();
                    if (model != null && prima != null && model != prima)
                    {
                        geoAnnulla.Add(prima);
                        if (geoAnnulla.Count > 30) geoAnnulla.RemoveAt(0);
                        geoRipeti.Clear();
                        GeoAggiornaAnnulla();
                    }
                },
                () =>
                {
                    if (selFrom < 0 || selFrom == selTo) return null;
                    List<WireRow> ed = RigheModificabili();
                    int a = ed.IndexOf(wireRows[Math.Min(selFrom, selTo)]), b = ed.IndexOf(wireRows[Math.Max(selFrom, selTo)]);
                    return a >= 0 && b >= a ? new[] { a, b } : null;
                },
                () => { if (geoAnnulla.Count == 0) return false; GeoAnnulla(true); Status("Riempimento annullato."); return true; });
            RefreshWiresTable();
            return new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8), Child = d
            };
        }

        // cella attiva: bordo blu (stile), intestazione della colonna e numero di riga evidenziati, barra della formula
        void AttivaCella(WireRow r, int col)
        {
            activeRow = r;
            activeCol = col;
            int n = 0;
            foreach (WireRow w in wireRows)
            {
                if (w.NumBox == null) continue;
                bool sel = w == r || (selFrom >= 0 && wireRows.IndexOf(w) >= Math.Min(selFrom, selTo) && wireRows.IndexOf(w) <= Math.Max(selFrom, selTo));
                w.NumBox.Background = new SolidColorBrush(sel ? XlSelSfondo : XlIntest);
                if (w.Num != null) w.Num.Foreground = new SolidColorBrush(sel ? XlSelTesto : Color.Parse("#666666"));
                n++;
            }
            if (wireHeadCells != null)
                for (int i = 0; i < wireHeadCells.Length - 1; i++)
                {
                    bool on = i == col + 1;
                    wireHeadCells[i].Background = new SolidColorBrush(on ? XlSelSfondo : XlIntest);
                    if (wireHeadCells[i].Child is TextBlock t)
                    {
                        t.Foreground = new SolidColorBrush(on ? XlSelTesto : XlIntestTesto);
                        t.FontWeight = on ? FontWeight.Bold : FontWeight.Normal;
                    }
                }
            if (lblFxName != null && r != null && r.Cells != null && col >= 0)
            {
                lblFxName.Text = "riga " + r.Num.Text + ", " + CellNames[col];
                fxSync = true;
                txtFx.Text = r.Cells[col].Text;
                fxSync = false;
            }
            HighlightWire(r);
        }

        List<WireRow> RigheModificabili() { return wireRows.FindAll(w => !w.ReadOnly); }

        // tasti come in Excel
        void TastoCella(WireRow r, int col, Avalonia.Input.KeyEventArgs e)
        {
            List<WireRow> ed = RigheModificabili();
            int i = ed.IndexOf(r);
            bool shift = (e.KeyModifiers & Avalonia.Input.KeyModifiers.Shift) != 0;
            bool ctrl = (e.KeyModifiers & Avalonia.Input.KeyModifiers.Control) != 0;
            if (e.Key == Avalonia.Input.Key.Enter) { ApplyWires(); e.Handled = true; return; }
            if (e.Key == Avalonia.Input.Key.Up || e.Key == Avalonia.Input.Key.Down)
            {
                int j = i + (e.Key == Avalonia.Input.Key.Up ? -1 : 1);
                if (j < 0 || j >= ed.Count) { e.Handled = true; return; }
                if (shift) { if (selFrom < 0) selFrom = wireRows.IndexOf(r); selTo = wireRows.IndexOf(ed[j]); }
                else { selFrom = selTo = -1; }
                WireRow dest = ed[j];
                wiresPanel.Mostra(dest.Row, () => dest.Cells[col].Focus());
                e.Handled = true;
                return;
            }
            TextBox tb = r.Cells[col];
            if (e.Key == Avalonia.Input.Key.Left && !shift && tb.CaretIndex == 0 && string.IsNullOrEmpty(tb.SelectedText) && col > 0) { r.Cells[col - 1].Focus(); e.Handled = true; return; }
            if (e.Key == Avalonia.Input.Key.Right && !shift && tb.CaretIndex >= (tb.Text ?? "").Length && col < 8) { r.Cells[col + 1].Focus(); e.Handled = true; return; }
            if (e.Key == Avalonia.Input.Key.Delete && !string.IsNullOrEmpty(tb.Text) && (tb.SelectedText ?? "") == tb.Text) { tb.Text = ""; e.Handled = true; return; }
            if (ctrl && e.Key == Avalonia.Input.Key.C && selFrom >= 0 && selFrom != selTo)
            {
                CopiaRighe();
                e.Handled = true;
                return;
            }
            if (ctrl && e.Key == Avalonia.Input.Key.V)
            {
                e.Handled = true;
                IncollaCelle(r, col);
            }
        }

        async void CopiaRighe()
        {
            int a = Math.Min(selFrom, selTo), b = Math.Max(selFrom, selTo);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int k = a; k <= b; k++)
            {
                WireRow w = wireRows[k];
                if (w.ReadOnly) continue;
                List<string> c = new List<string>();
                foreach (TextBox t in w.Cells) c.Add(t.Text ?? "");
                sb.Append(string.Join("\t", c)).Append("\n");
            }
            try { if (Clipboard != null) await Clipboard.SetTextAsync(sb.ToString()); Status("Copiate " + (b - a + 1) + " righe della tabella dei fili."); }
            catch (Exception ex) { Status("Copia non riuscita: " + ex.Message, true); }
        }

        // incolla: una cella sola come testo; piu' celle (tabulazioni) o piu' righe (a capo) come in Excel, aggiungendo righe
        async void IncollaCelle(WireRow r, int col)
        {
            string txt = null;
            try { if (Clipboard != null) txt = await Clipboard.GetTextAsync(); } catch { }
            if (string.IsNullOrEmpty(txt)) return;
            string[] lines = txt.Replace("\r", "").TrimEnd('\n').Split('\n');
            if (lines.Length == 1 && !lines[0].Contains("\t"))
            {
                TextBox tb = r.Cells[col];
                int c0 = tb.SelectionStart < tb.SelectionEnd ? tb.SelectionStart : tb.CaretIndex;
                int c1 = Math.Max(tb.SelectionStart, tb.SelectionEnd);
                string t0 = tb.Text ?? "";
                if (c1 < c0) c1 = c0;
                tb.Text = t0.Substring(0, Math.Min(c0, t0.Length)) + lines[0] + t0.Substring(Math.Min(c1, t0.Length));
                return;
            }
            List<WireRow> ed = RigheModificabili();
            int i = ed.IndexOf(r);
            foreach (string line in lines)
            {
                if (i >= ed.Count)
                {
                    AddWireAfter(wireRows.IndexOf(ed[ed.Count - 1]));
                    ed = RigheModificabili();
                }
                string[] parts = line.Split('\t');
                for (int k = 0; k < parts.Length && col + k < 9; k++) ed[i].Cells[col + k].Text = parts[k].Trim();
                i++;
            }
            Status("Incollate " + lines.Length + " righe: Invio o 'Applica e ricalcola' per calcolare.");
        }

        // ---------------------------------------------------------------------------------------------
        // convertitore "sezione -> raggio equivalente" (formule Balanis; valore di Rothammel = massima estensione / 2)
        void ApriConvertitoreRaggio(Control anchor)
        {
            WireRow r = activeRow != null && activeRow.Cells != null ? activeRow : RigheModificabili().Find(x => true);
            if (r == null) { Status("Nessun filo nella tabella.", true); return; }
            int forma = 1;
            StackPanel p = new StackPanel { Spacing = 6, Width = 330 };
            p.Children.Add(new TextBlock { Text = "Raggio da sezione (riga " + r.Num.Text + ") – formule Balanis", FontWeight = FontWeight.Bold });
            TextBox ta = Tb("10", 60), tb2 = Tb("20", 60);
            TextBlock la = new TextBlock { Text = "lato", VerticalAlignment = VerticalAlignment.Center }, lb = new TextBlock { Text = "b", VerticalAlignment = VerticalAlignment.Center };
            TextBlock res = new TextBlock { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold };
            TextBlock roth = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), FontSize = 12 };
            Button usa = Btn("Usa", true), ann = Btn("Annulla", false);
            double valore = double.NaN;
            Action calc = () =>
            {
                double a = ParseNum(ta.Text, 10), b = ParseNum(tb2.Text, 20), ae = double.NaN, est = double.NaN;
                string f = "";
                lb.IsVisible = tb2.IsVisible = forma == 2 || forma == 4;
                la.Text = forma == 0 ? "diametro" : (forma == 1 ? "lato" : (forma == 3 ? "larghezza" : "a"));
                if (forma == 0) { ae = a / 2; est = a; f = "raggio = d / 2 = " + FN(ae, 2) + " mm"; }
                else if (forma == 1) { ae = 0.59 * a; est = a; f = "raggio equivalente = 0,59 × " + FN(a, 2) + " = " + FN(ae, 2) + " mm"; }
                else if (forma == 2) { ae = 0.2 * (a + b); est = Math.Max(a, b); f = "raggio equivalente ≈ 0,2 (a + b) = " + FN(ae, 2) + " mm"; }
                else if (forma == 3) { ae = 0.25 * a; est = a; f = "raggio equivalente = 0,25 × larghezza = " + FN(ae, 2) + " mm"; }
                else { ae = double.NaN; est = Math.Max(a, b); f = "profilo U: calcolo numerico con la capacita' della sezione 2D (da validare, prossima versione)"; }
                valore = ae;
                res.Text = la.Text + " " + FN(a, 2) + " mm" + (tb2.IsVisible ? ", b " + FN(b, 2) + " mm" : "") + " → " + f;
                roth.Text = "Rothammel (massima estensione " + FN(est, 2) + " mm): raggio " + FN(est / 2, 2) + " mm" + (forma == 4 ? "" : " · profilo U: calcolo numerico");
                usa.Content = double.IsNaN(ae) ? "Usa" : "Usa " + FN(ae, 2);
                usa.IsEnabled = !double.IsNaN(ae);
            };
            p.Children.Add(Scelta(new[] { "tondo", "quadrato", "rettangolo", "piatto", "U" }, 1, k => { forma = k; calc(); }));
            StackPanel ins = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            ins.Children.Add(la); ins.Children.Add(ta); ins.Children.Add(lb); ins.Children.Add(tb2); ins.Children.Add(new TextBlock { Text = "mm", VerticalAlignment = VerticalAlignment.Center });
            p.Children.Add(ins);
            p.Children.Add(res);
            p.Children.Add(roth);
            StackPanel bb = new StackPanel { Orientation = Orientation.Horizontal };
            bb.Children.Add(usa); bb.Children.Add(ann);
            p.Children.Add(bb);
            ta.LostFocus += (s, e) => calc();
            tb2.LostFocus += (s, e) => calc();
            ta.KeyUp += (s, e) => calc();
            tb2.KeyUp += (s, e) => calc();
            calc();
            Flyout fl = new Flyout { Content = p };
            usa.Click += (s, e) =>
            {
                if (!double.IsNaN(valore)) { r.Cells[7].Text = valore.ToString("0.###", CultureInfo.InvariantCulture); AttivaCella(r, 7); }
                fl.Hide();
            };
            ann.Click += (s, e) => fl.Hide();
            fl.ShowAt(anchor);
        }

        // ---------------------------------------------------------------------------------------------
        static string FmtM(double v) { return v.ToString("0.######", CultureInfo.InvariantCulture); }

        internal static string RadiusToCell(string card)
        {
            string t = (card ?? "").Trim();
            double v;
            // raggio negativo intero = riga della tabella Rastremazioni (come MMANA): resta com'e'
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v < 0 && v == Math.Round(v)) return t;
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return (v * 1000.0).ToString("0.####", CultureInfo.InvariantCulture);
            if (t.EndsWith("*MM", StringComparison.OrdinalIgnoreCase)) return t.Substring(0, t.Length - 3);
            if (Expr.IsValidName(t)) return t + "/MM";   // R -> R/MM (valore in mm)
            return "(" + t + ")/MM";
        }

        static bool Balanced(string s)
        {
            int d = 0;
            foreach (char ch in s) { if (ch == '(') d++; if (ch == ')' && --d < 0) return false; }
            return d == 0;
        }

        internal static string RadiusFromCell(string cell)
        {
            string t = cell.Trim();
            // "(R)/MM" o "R/MM" mostrati dalla tabella per un raggio scritto con una variabile: si torna a "R"
            System.Text.RegularExpressions.Match back = System.Text.RegularExpressions.Regex.Match(t, @"^\(?(.+?)\)?/MM$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (back.Success && Balanced(back.Groups[1].Value)) return back.Groups[1].Value;
            double v;
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v < 0 && v == Math.Round(v)) return v.ToString("0", CultureInfo.InvariantCulture);
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return (v / 1000.0).ToString("0.##########", CultureInfo.InvariantCulture);
            bool simple = Expr.IsValidName(t) || (t.StartsWith("(") && t.EndsWith(")"));
            return simple ? t + "*MM" : "(" + t + ")*MM";
        }

        void RefreshWiresTable()
        {
            if (wiresPanel == null) return;
            wiresPanel.Clear();
            wireRows.Clear();
            if (model == null)
            {
                wiresPanel.Add(new TextBlock { Text = "Nessun modello aperto.", Foreground = Pal.B(Pal.Muted), Margin = new Thickness(4, 6) });
                RefreshWiresTitle();
                return;
            }
            tabellaFiliRidotta = model.Geometry.Count > SchedeTabellaFiliMax;
            if (tabellaFiliRidotta)
            {
                // 0.4.20 (registro di IC8TEM del 14/11/2026 17:48): la parabola da 30546 schede costruiva 30546 righe da 10 caselle,
                // 12 s e 2,5 GB solo per la tabella. Oltre SchedeTabellaFiliMax la tabella non si costruisce e non si applica
                // (cosi' non puo' riscrivere il modello con righe mancanti): i fili si modificano nella linguetta Modello NEC.
                activeRow = null; activeCol = -1; selFrom = selTo = -1;
                wiresPanel.Add(new TextBlock
                {
                    Text = "Modello con " + model.Geometry.Count.ToString(CultureInfo.InvariantCulture) + " schede di geometria: oltre " + SchedeTabellaFiliMax.ToString(CultureInfo.InvariantCulture) +
                           " la tabella dei fili non si costruisce (sarebbe lentissima e occuperebbe gigabyte di memoria).\nI fili si modificano nella linguetta Modello NEC; Struttura 3D e calcolo funzionano normalmente.",
                    Foreground = Pal.B(Pal.Muted), Margin = new Thickness(4, 6), TextWrapping = TextWrapping.Wrap
                });
                if (lblWiresTitle != null) lblWiresTitle.Text = "Fili: tabella non costruita (" + model.Geometry.Count.ToString(CultureInfo.InvariantCulture) + " schede)";
                return;
            }
            foreach (GeomCard g in model.Geometry) wireRows.Add(MakeWireRow(g));
            activeRow = null; activeCol = -1; selFrom = selTo = -1;
            RenumberWires();
        }

        /// <summary>Oltre questo numero di schede di geometria la tabella dei fili non si costruisce (0.4.20).</summary>
        const int SchedeTabellaFiliMax = 4000;
        bool tabellaFiliRidotta;

        bool TabellaFiliNonAttiva()
        {
            if (!tabellaFiliRidotta) return false;
            Status("Modello troppo grande per la tabella dei fili: modifica le schede nella linguetta Modello NEC.", true);
            return true;
        }

        void RefreshWiresTitle()
        {
            if (lblWiresTitle != null) lblWiresTitle.Text = "Fili " + wireRows.FindAll(w => !w.ReadOnly).Count;
        }

        WireRow MakeWireRow(GeomCard g)
        {
            WireRow r = new WireRow { Card = g };
            bool isGw = g == null || (g.Id == "GW" && !g.HasGc);
            r.ReadOnly = !isGw;
            r.Row = new Grid { ColumnDefinitions = new ColumnDefinitions(WireCols) };
            r.Num = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#666666")), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            r.NumBox = new Border { Background = new SolidColorBrush(XlIntest), BorderBrush = new SolidColorBrush(XlGriglia), BorderThickness = new Thickness(1, 0, 1, 1), Child = r.Num };
            r.Row.Children.Add(r.NumBox);

            if (r.ReadOnly)
            {
                string txt = g.Src != null ? g.Id + " " + string.Join(" ", g.Src)
                    : string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}", g.Id, g.I1, g.I2, FmtM(g.F1), FmtM(g.F2), FmtM(g.F3), FmtM(g.F4), FmtM(g.F5), FmtM(g.F6), FmtM(g.F7));
                if (g.HasGc) txt += "   (filo rastremato, con GC)";
                TextBox tb = new TextBox { Text = txt + "   - si modifica nella linguetta Modello NEC", FontFamily = Pal.Mono };
                tb.Classes.Add("xl"); tb.Classes.Add("ro"); tb.Classes.Add("l");
                Grid.SetColumn(tb, 1);
                Grid.SetColumnSpan(tb, 10);
                r.Row.Children.Add(tb);
                return r;
            }

            string[] src = new string[9];
            if (g == null)
            {
                int tag = NextTag();
                src = new[] { tag.ToString(CultureInfo.InvariantCulture), "11", "0", "0", "0", "0", "0", "1", "1" };
            }
            else if (g.Src != null)
            {
                src = new[] { g.Src[0], g.Src[1], g.Src[2], g.Src[3], g.Src[4], g.Src[5], g.Src[6], g.Src[7], RadiusToCell(g.Src[8]) };
            }
            else
            {
                src = new[] { g.I1.ToString(CultureInfo.InvariantCulture), g.I2.ToString(CultureInfo.InvariantCulture),
                              FmtM(g.F1), FmtM(g.F2), FmtM(g.F3), FmtM(g.F4), FmtM(g.F5), FmtM(g.F6), RadiusToCell(FmtM(g.F7)) };
            }
            // ordine delle celle a video: tag, X1..Z2, raggio, segmenti
            string[] shown = { src[0], src[2], src[3], src[4], src[5], src[6], src[7], src[8], src[1] };
            r.Shown = shown;
            r.Cells = new TextBox[9];
            for (int k = 0; k < 9; k++)
            {
                int col = k;
                TextBox tb = new TextBox { Text = shown[k], FontFamily = Pal.Mono };
                tb.Classes.Add("xl");
                tb.KeyDown += (s, e) => TastoCella(r, col, e);
                tb.GotFocus += (s, e) => { tb.SelectAll(); AttivaCella(r, col); if (col > 0) manigliaFili?.Attiva(tb); else manigliaFili?.Attiva(null); };
                // rotella sulla cella attiva: cambia il numero e ricalcola (0.4.2, idea di yagi_designer, approvata il 13/11/2026);
                // coordinate 1 mm, raggio 0,1 mm, segmenti 1 (Maiusc: x10)
                tb.PointerWheelChanged += (s, e) =>
                {
                    if (!tb.IsFocused || col == 0) return;
                    double passo = col <= 6 ? 0.001 : (col == 7 ? 0.1 : 1);
                    if ((e.KeyModifiers & Avalonia.Input.KeyModifiers.Shift) != 0) passo *= 10;
                    if (RotellaNumero(tb, (e.Delta.Y > 0 ? 1 : -1) * passo, col == 8, col == 7 ? 0.001 : (col == 8 ? 1 : double.NegativeInfinity)))
                    {
                        e.Handled = true;
                        RotellaRicalcola(ApplyWires);
                    }
                };
                tb.TextChanged += (s, e) =>
                {
                    if (activeRow == r && activeCol == col && txtFx != null && !fxSync) { fxSync = true; txtFx.Text = tb.Text; fxSync = false; }
                };
                Grid.SetColumn(tb, k + 1);
                r.Row.Children.Add(tb);
                r.Cells[k] = tb;
            }
            r.Len = new TextBox { FontFamily = Pal.Mono };
            r.Len.Classes.Add("xl"); r.Len.Classes.Add("ro");
            ToolTip.SetTip(r.Len, "Lunghezza del filo (calcolata, sola lettura)");
            if (g != null)
            {
                double dx = g.F4 - g.F1, dy = g.F5 - g.F2, dz = g.F6 - g.F3;
                r.Len.Text = Math.Sqrt(dx * dx + dy * dy + dz * dz).ToString("0.####", CultureInfo.CurrentCulture);
            }
            Grid.SetColumn(r.Len, 10);
            r.Row.Children.Add(r.Len);
            StackPanel btn = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 1, 0, 1) };
            Button add = BtnRiga("+", "aggiungi riga sotto"), cp = BtnRiga("⤓", "copia la riga di sopra"), del = BtnRiga("✕", "elimina", true);
            add.Click += (s, e) => AddWireAfter(wireRows.IndexOf(r));
            cp.Click += (s, e) =>
            {
                List<WireRow> ed = RigheModificabili();
                int i = ed.IndexOf(r);
                if (i <= 0) { Status("La prima riga non ha una riga sopra da copiare.", true); return; }
                for (int k = 1; k < 9; k++) r.Cells[k].Text = ed[i - 1].Cells[k].Text;
            };
            del.Click += (s, e) =>
            {
                wireRows.Remove(r);
                if (activeRow == r) { activeRow = null; activeCol = -1; }
                RenumberWires();
            };
            btn.Children.Add(add); btn.Children.Add(cp); btn.Children.Add(del);
            Grid.SetColumn(btn, 11);
            r.Row.Children.Add(btn);
            return r;
        }

        int NextTag()
        {
            int t = 0;
            if (model != null) foreach (GeomCard g in model.Geometry) t = Math.Max(t, g.I1);
            foreach (WireRow w in wireRows)
            {
                int v;
                if (w.Cells != null && int.TryParse(w.Cells[0].Text, out v)) t = Math.Max(t, v);
            }
            return t + 1;
        }

        void RenumberWires()
        {
            List<Control> righe = new List<Control>();
            int n = 0;
            foreach (WireRow r in wireRows)
            {
                if (!r.ReadOnly) n++;
                r.Num.Text = r.ReadOnly ? "" : n.ToString(CultureInfo.InvariantCulture);
                righe.Add(r.Row);
            }
            // riga "next": scrivendo qui si aggiunge un filo
            if (model != null)
            {
                Grid nx = new Grid { ColumnDefinitions = new ColumnDefinitions(WireCols) };
                nx.Children.Add(new Border { Background = new SolidColorBrush(XlIntest), BorderBrush = new SolidColorBrush(XlGriglia), BorderThickness = new Thickness(1, 0, 1, 1),
                    Child = new TextBlock { Text = (n + 1).ToString(CultureInfo.InvariantCulture), FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#666666")), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                TextBox next = new TextBox { Watermark = "next – scrivi qui per aggiungere un filo", FontFamily = Pal.Mono };
                next.Classes.Add("xl"); next.Classes.Add("next");
                next.TextChanged += (s, e) =>
                {
                    string t = next.Text ?? "";
                    if (t.Length == 0) return;
                    next.Text = "";
                    AddWireAfter(wireRows.Count - 1);
                    WireRow last = wireRows[wireRows.Count - 1];
                    if (last.Cells != null) { last.Cells[1].Text = t; wiresPanel.Mostra(last.Row, () => { last.Cells[1].Focus(); last.Cells[1].CaretIndex = t.Length; }); }
                };
                Grid.SetColumn(next, 1);
                Grid.SetColumnSpan(next, 10);
                nx.Children.Add(next);
                righe.Add(nx);
            }
            if (wireRows.Count == 0)
                righe.Add(new TextBlock { Text = "Nessun filo: \"Aggiungi filo\" o scrivi nella riga next.", Foreground = Pal.B(Pal.Muted), Margin = new Thickness(4, 6) });
            wiresPanel.Allinea(righe);
            RefreshWiresTitle();
        }

        async void NuovoEAggiungiFilo(int idx)
        {
            try
            {
                await NuovoModello();
                if (model != null) AddWireAfter(idx);
            }
            catch (Exception ex) { Registro.Scrivi("ERRORE", "Aggiungi filo senza modello: " + ex.Message); }
        }

        /// <summary>1.0.10: vero se il testo e' un conto di soli numeri (cifre, punto, + - * / e parentesi, almeno un operatore dopo il primo carattere).</summary>
        static bool SoloConto(string t)
        {
            if (string.IsNullOrEmpty(t)) return false;
            foreach (char ch in t) if (!(char.IsDigit(ch) || ch == '.' || ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '(' || ch == ')' || ch == 'e' || ch == 'E')) return false;
            for (int i = 1; i < t.Length; i++) if ("+-*/".IndexOf(t[i]) >= 0 && t[i - 1] != 'e' && t[i - 1] != 'E') return true;
            return t.StartsWith("(");
        }

        void AddWireAfter(int idx)
        {
            if (TabellaFiliNonAttiva()) return;
            // 1.0.8 (IC8TEM 16/11/2026 20:59): "Aggiungi filo" senza modello aperto equivale a "Nuovo" e poi aggiunge il filo
            if (model == null) { NuovoEAggiungiFilo(idx); return; }
            WireRow r = MakeWireRow(null);
            // il nuovo filo parte dall'estremo del filo precedente (comodo per disegnare una linea spezzata)
            WireRow prev = null;
            for (int k = Math.Min(idx, wireRows.Count - 1); k >= 0; k--) if (!wireRows[k].ReadOnly) { prev = wireRows[k]; break; }
            if (prev != null)
            {
                for (int k = 0; k < 3; k++) { r.Cells[1 + k].Text = prev.Cells[4 + k].Text; r.Cells[4 + k].Text = prev.Cells[4 + k].Text; }
                r.Cells[7].Text = prev.Cells[7].Text;
                r.Cells[8].Text = prev.Cells[8].Text;
            }
            wireRows.Insert(Math.Min(idx + 1, wireRows.Count), r);
            RenumberWires();
            wiresPanel.Mostra(r.Row, () => r.Cells[4].Focus());
        }

        Avalonia.Threading.DispatcherTimer rotellaTimer;
        Action rotellaAzione;

        /// <summary>Somma il passo al numero della casella (solo se e' un numero semplice); falso se c'e' una formula.</summary>
        static bool RotellaNumero(TextBox tb, double passo, bool intero, double minimo)
        {
            string t = Expr.NormalizeDecimal((tb.Text ?? "").Trim());
            double v;
            if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return false;
            v = Math.Max(minimo, v + passo);
            if (intero) v = Math.Round(v);
            if (Math.Abs(v) < 1e-12) v = 0;
            tb.Text = v.ToString(intero ? "0" : "0.######", CultureInfo.InvariantCulture);
            tb.CaretIndex = tb.Text.Length;
            return true;
        }

        // il ricalcolo parte mezzo secondo dopo l'ultimo scatto della rotella
        void RotellaRicalcola(Action a)
        {
            rotellaAzione = a;
            if (rotellaTimer == null)
            {
                rotellaTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                rotellaTimer.Tick += (s, e) => { rotellaTimer.Stop(); Action x = rotellaAzione; rotellaAzione = null; if (x != null) x(); };
            }
            rotellaTimer.Stop(); rotellaTimer.Start();
        }

        void HighlightWire(WireRow r)
        {
            if (view3d == null || r == null) return;
            int tag;
            view3d.HighlightTag = r.Cells != null && int.TryParse(r.Cells[0].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out tag) ? tag : (r.Card != null ? r.Card.I1 : -1);
            view3d.InvalidateVisual();
        }

        // ---------------------------------------------------------------------------------------------
        void ApplyWires()
        {
            if (TabellaFiliNonAttiva()) return;
            if (model == null) { Status("Apri prima un modello.", true); return; }
            NecModel m = model.Clone();
            m.Geometry = new List<GeomCard>();
            List<GeomCard> newWires = new List<GeomCard>();
            List<WireRow> newRows = new List<WireRow>();
            foreach (WireRow r in wireRows)
            {
                if (r.ReadOnly) { m.Geometry.Add(r.Card.Clone()); continue; }
                string[] c = new string[9];
                for (int k = 0; k < 9; k++)
                {
                    c[k] = Expr.NormalizeDecimal((r.Cells[k].Text ?? "").Replace(" ", "").Replace("\t", ""));
                    if (c[k].Length == 0) { Status("Filo " + r.Num.Text + ": c'e' una cella vuota.", true); return; }
                    // 1.0.10 (IC8TEM 16/11/2026 22:42 "mi devi permettere di fare le somme e sottrazioni"): come in Excel, un conto di soli
                    // numeri (1.42+.26, 2*0.75-0.1) diventa il suo risultato nella cella; le formule con le variabili restano scritte
                    if (k >= 1 && k <= 8 && SoloConto(c[k]))
                    {
                        try
                        {
                            double v = Expr.Eval(c[k], null);
                            c[k] = k == 8 ? Math.Round(v).ToString(CultureInfo.InvariantCulture) : FormatVal(v);
                            r.Cells[k].Text = c[k];
                        }
                        catch (Exception ex) { Status("Filo " + r.Num.Text + ": conto non valido (" + c[k] + "): " + ex.Message, true); return; }
                    }
                }
                string[] orig = r.Card != null ? r.Card.Src : null;
                // cella non toccata: si conserva il testo originale della scheda (variabili comprese)
                string radius = (orig != null && c[7] == r.Shown[7]) ? orig[8] : RadiusFromCell(c[7]);
                GeomCard g = r.Card != null ? r.Card.Clone() : new GeomCard { Id = "GW" };
                g.Id = "GW";
                g.HasGc = false;
                g.Src = new[] { c[0], c[8], c[1], c[2], c[3], c[4], c[5], c[6], radius };
                m.Geometry.Add(g);
                newWires.Add(g);
                newRows.Add(r);
            }
            List<string> err = m.Reevaluate();
            Registro.Scrivi("AZIONE", "tabella dei fili applicata: " + newWires.Count + " fili GW, " + (m.Geometry.Count - newWires.Count) + " altre schede");
            foreach (GeomCard gw in newWires)
                Registro.Scrivi("AZIONE", "  GW " + string.Join(" ", gw.Src) + string.Format(CultureInfo.InvariantCulture, "  ->  tag {0}, {1} segm., ({2:0.####} {3:0.####} {4:0.####}) - ({5:0.####} {6:0.####} {7:0.####}), raggio {8:0.######} m",
                    gw.I1, gw.I2, gw.F1, gw.F2, gw.F3, gw.F4, gw.F5, gw.F6, gw.F7));
            for (int i = 0; i < newWires.Count && err.Count == 0; i++)
            {
                GeomCard g = newWires[i];
                string n = "Filo " + newRows[i].Num.Text + ": ";
                double dx = g.F4 - g.F1, dy = g.F5 - g.F2, dz = g.F6 - g.F3;
                if (g.I1 < 0) err.Add(n + "il tag non puo' essere negativo.");
                if (g.I2 < 1) err.Add(n + "servono almeno 1 segmento.");
                if (g.F7 < 0 && g.F7 == Math.Round(g.F7))
                {
                    int nr = (int)Math.Round(-g.F7);
                    if (!m.Rastremazioni.Exists(q => q.N == nr)) err.Add(n + "raggio " + nr * -1 + ": la riga -" + nr + " non c'e' nella tabella Rastremazioni (Oggetti del modello).");
                }
                else if (g.F7 <= 0) err.Add(n + "il raggio deve essere maggiore di zero (oppure -N = riga N delle Rastremazioni).");
                if (dx * dx + dy * dy + dz * dz < 1e-18) err.Add(n + "i due estremi coincidono.");
            }
            if (err.Count > 0)
            {
                Status(err[0], true);
                lastError = string.Join("\n  ", err);
                UpdateNotes();
                return;
            }
            // le sorgenti restano quelle della tabella Sorgenti: se cambiano il tag o i segmenti del filo, seguono il filo
            for (int i = 0; i < newWires.Count; i++)
            {
                GeomCard g = newWires[i];
                WireRow r = newRows[i];
                int oldTag = r.Card != null ? r.Card.I1 : -1;
                int center = (g.I2 + 1) / 2;
                List<SourceSpec> mine = m.Sources.FindAll(q => q.Tag == oldTag && oldTag >= 0);
                if (mine.Count == 0) continue;
                int oldCenter = r.Card != null ? (r.Card.I2 + 1) / 2 : center;
                foreach (SourceSpec s in mine)
                {
                    s.Tag = g.I1;
                    if (s.Seg == oldCenter || s.Seg > g.I2) s.Seg = center;
                }
            }
            // sorgenti dei fili eliminati
            HashSet<GeomCard> kept = new HashSet<GeomCard>();
            foreach (WireRow r in newRows) if (r.Card != null) kept.Add(r.Card);
            foreach (GeomCard g in model.Geometry)
            {
                if (g.Id != "GW" || g.HasGc || kept.Contains(g)) continue;
                int t = g.I1;
                if (newWires.Exists(q => q.I1 == t)) continue;
                m.Sources.RemoveAll(q => q.Tag == t);
            }
            if (m.Sources.Count == 0) Status("Attenzione: nessuna sorgente (aggiungerla nella tabella Sorgenti).", true);
            model = m;
            mmWires = null;     // la tabella dei fili MMANA risegmentati non vale piu'

            lastError = null;
            if (txtNec != null) txtNec.Text = model.ToNec();
            RefreshWiresTable();
            RefreshVarsPanel();
            Calculate(true);
        }
    }
}
