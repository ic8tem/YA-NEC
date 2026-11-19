// YA-NEC - fase 1: linguetta Variabili (schede SY). Ogni variabile ha nome, espressione, valore calcolato,
// passo e pulsanti -/+ per regolarla al volo (solo se l'espressione e' un numero), come in xnec2c e AutoEZ.
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
        class VarRow
        {
            public TextBox Name, Expr, Step, Descr;
            public TextBlock Value, State;
        }

        StackPanel varsPanel;
        Structure3DView varView;
        SelectableTextBlock varInfo;
        string varScelta;
        int rotellaScatti;
        readonly List<VarRow> varRows = new List<VarRow>();

        const string VarsHelp =
            "Operatori: + - * / ^ e parentesi.  Funzioni: SIN, COS, TAN (in gradi), ATN (in gradi), SQR, EXP, LOG, LOG10, ABS, SGN, " +
            "INT (arrotonda), FIX (tronca), MOD, MAX, MIN, ODD (intero dispari: utile per i segmenti del filo alimentato).\n" +
            "Costanti: PI; unita' MM, CM, IN, FT; PF, NF, UF, NH, UH; diametri AWG_0 ... AWG_20.  Maiuscole e minuscole sono uguali.\n" +
            "Nelle schede di geometria scrivere le espressioni senza spazi, per esempio:  GW 1 NS 0 -L/2 0 0 L/2 0 R.\n" +
            "Le variabili restano nelle schede di geometria anche nel file salvato; nelle schede EX, LD, GN e FR vengono salvate come numeri.\n" +
            "Le schede SY sono le stesse di 4nec2 e xnec2c: i modelli con variabili si scambiano con quei programmi.";

        Control BuildVarsPage()
        {
            DockPanel d = new DockPanel();
            WrapPanel bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            Button bAdd = Btn("Aggiungi variabile", false);
            bAdd.Click += (s, e) => AddVarRow(new SyVar { Name = NewVarName(), Expr = "0" }, true);
            Button bApply = Btn("Applica e ricalcola", true);
            bApply.Click += (s, e) => ApplyVars();
            bar.Children.Add(bAdd);
            bar.Children.Add(bApply);
            bar.Children.Add(new TextBlock { Text = "Le variabili si usano nelle schede di geometria (linguetta Modello NEC) al posto dei numeri.", Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0) });
            DockPanel.SetDock(bar, Dock.Top);
            d.Children.Add(bar);

            Border help = new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8), Margin = new Thickness(0, 6, 0, 0),
                Child = new SelectableTextBlock { Text = VarsHelp, FontSize = 12, Foreground = Pal.B(Pal.ChartText), TextWrapping = TextWrapping.Wrap }
            };
            DockPanel.SetDock(help, Dock.Bottom);
            d.Children.Add(help);

            varsPanel = new StackPanel { Spacing = 4, Margin = new Thickness(10) };
            // 1.5.1 (bozzetto «Linguetta Variabili: dove agisce la variabile», approvato da IC8TEM il 19/11/2026 08:12): a destra la
            // struttura 3D con in ocra i fili su cui agisce la variabile scelta (clic sulla riga); clic sul titolo: nasconde il riquadro
            Grid gv = new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,0.8*") };
            Border sx = Riquadro(new ScrollViewer { Content = varsPanel, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto });
            Grid.SetColumn(sx, 0); gv.Children.Add(sx);
            varView = new Structure3DView { ShowPattern = false, ShowGroundAndHeight = false, ShowPhases = false, ShowCurrents = false, MinHeight = 260 };
            varInfo = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), FontSize = 12, Margin = new Thickness(0, 6, 0, 0) };
            Button tit = new Button { Content = "Struttura 3D: dove agisce la variabile ▾", Background = Brushes.Transparent, BorderThickness = new Thickness(0), FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore), Padding = new Thickness(0) };
            ToolTip.SetTip(tit, "Clic: nasconde o mostra la struttura");
            DockPanel dv = new DockPanel();
            DockPanel.SetDock(tit, Dock.Top); dv.Children.Add(tit);
            TextBlock leg = new TextBlock { Text = "ocra pieno: usa la variabile · ocra tratteggiato: la usa attraverso un'altra variabile", FontSize = 11, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap };
            DockPanel.SetDock(leg, Dock.Bottom); dv.Children.Add(leg);
            DockPanel.SetDock(varInfo, Dock.Bottom); dv.Children.Add(varInfo);
            dv.Children.Add(varView);
            Border sd = Riquadro(dv);
            Grid.SetColumn(sd, 2); gv.Children.Add(sd);
            tit.Click += (s, e) =>
            {
                bool vis = varView.IsVisible;
                varView.IsVisible = varInfo.IsVisible = leg.IsVisible = !vis;
                gv.ColumnDefinitions[2].Width = vis ? GridLength.Auto : new GridLength(0.8, GridUnitType.Star);
                tit.Content = vis ? "Struttura 3D ▸" : "Struttura 3D: dove agisce la variabile ▾";
            };
            d.Children.Add(gv);
            RefreshVarsPanel();
            return d;
        }

        string NewVarName()
        {
            for (int k = 1; ; k++)
            {
                string n = "V" + k;
                bool used = false;
                foreach (VarRow r in varRows) if (string.Equals(r.Name.Text, n, StringComparison.OrdinalIgnoreCase)) used = true;
                if (!used) return n;
            }
        }

        void RefreshVarsPanel()
        {
            if (varsPanel == null) return;
            varsPanel.Children.Clear();
            varRows.Clear();
            Grid head = VarGrid();
            string[] h = { "Nome", "Descrizione (cosa e' la variabile)", "Espressione", "Valore", "Passo", "", "", "", "Stato" };
            for (int i = 0; i < h.Length; i++)
            {
                TextBlock t = new TextBlock { Text = h[i], FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(4, 0) };
                Grid.SetColumn(t, i);
                head.Children.Add(t);
            }
            varsPanel.Children.Add(head);
            if (model == null || model.Variables.Count == 0)
            {
                varsPanel.Children.Add(new TextBlock
                {
                    Text = model == null ? "Nessun modello aperto." : "Il modello non ha variabili: \"Aggiungi variabile\" per crearne una.",
                    Foreground = Pal.B(Pal.Muted), Margin = new Thickness(4, 8)
                });
                // 1.5.2 (difetto visto preparando la lezione 1, IC8TEM «Si', correggi e gira»): senza variabili la struttura a destra
                // restava vuota con «Apri un modello»; ora si vede il modello, grigio, senza fili in ocra
                if (varView != null) VarMostraDove(null);
                return;
            }
            foreach (SyVar v in model.Variables) AddVarRow(v, false);
            if (varView != null) VarMostraDove(varScelta != null && model.Variables.Exists(q => string.Equals(q.Name, varScelta, StringComparison.OrdinalIgnoreCase)) ? varScelta : model.Variables[0].Name);
        }

        static Grid VarGrid()
        {
            return new Grid { ColumnDefinitions = new ColumnDefinitions("150,270,270,110,100,Auto,Auto,Auto,*") };
        }

        void AddVarRow(SyVar v, bool isNew)
        {
            if (varsPanel == null) return;
            if (isNew && varRows.Count == 0 && varsPanel.Children.Count > 1) varsPanel.Children.RemoveAt(1);   // toglie la riga "nessuna variabile"
            Grid g = VarGrid();
            VarRow r = new VarRow();
            r.Name = new TextBox { Text = v.Name, Margin = new Thickness(2, 2, 0, 2), FontFamily = Pal.Mono };
            r.Expr = new TextBox { Text = v.Expr, Margin = new Thickness(2, 2, 0, 2), FontFamily = Pal.Mono };
            // rotella sull'espressione attiva: come i pulsanti - e + con il passo della riga (0.4.2, approvata il 13/11/2026)
            r.Expr.PointerWheelChanged += (s, e) =>
            {
                if (!r.Expr.IsFocused) return;
                e.Handled = true;
                rotellaScatti += e.Delta.Y > 0 ? 1 : -1;
                RotellaRicalcola(() => { int n = rotellaScatti; rotellaScatti = 0; if (n != 0) StepVar(r, n); });
            };
            // descrizione: quella scritta dall'utente; se vuota, in grigio quella ricavata da dove e' usata la variabile
            r.Descr = new TextBox { Text = v.Descr ?? "", Margin = new Thickness(2), TextWrapping = TextWrapping.NoWrap };
            ToolTip.SetTip(r.Descr, "Cosa rappresenta la variabile. Se resta vuota, in grigio c'e' la descrizione ricavata da dove e' usata. Viene salvata nel file .nec.");
            r.Descr.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) ApplyVars(); };
            Control nameCell = WithMenu(r.Name, () => NameMenu(r), "Scegli una variabile tipica dall'elenco");
            Control exprCell = WithMenu(r.Expr, () => ExprMenu(r), "Inserisci una variabile, un'unita' o una funzione");
            r.Value = new TextBlock { Text = isNew ? "-" : FormatVal(v.Value), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0), FontFamily = Pal.Mono, Foreground = Pal.B(Pal.Fore) };
            double st = Math.Abs(v.Value) > 1e-12 ? NiceStep(Math.Abs(v.Value) * 0.01) : 0.001;
            r.Step = new TextBox { Text = st.ToString("0.#########", CultureInfo.CurrentCulture), Margin = new Thickness(2), FontFamily = Pal.Mono };
            ToolTip.SetTip(r.Step, "Passo dei pulsanti - e + nell'unita' del valore (metri per le lunghezze). Con 1000*MM e passo 0,01 si passa a 1010*MM.");
            Button minus = Btn("−", false), plus = Btn("+", false), del = Btn("✕", false);
            minus.MinWidth = plus.MinWidth = del.MinWidth = 36;
            ToolTip.SetTip(del, "Elimina la variabile");
            minus.Click += (s, e) => StepVar(r, -1);
            plus.Click += (s, e) => StepVar(r, +1);
            del.Click += (s, e) =>
            {
                varRows.Remove(r);
                varsPanel.Children.Remove(g);
                ApplyVars();
            };
            r.State = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0), FontSize = 12, TextWrapping = TextWrapping.Wrap };
            if (!isNew) SetRowState(r, v);
            r.Expr.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) ApplyVars(); };
            r.Name.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) ApplyVars(); };
            Control[] cells = { nameCell, r.Descr, exprCell, r.Value, r.Step, minus, plus, del, r.State };
            for (int i = 0; i < cells.Length; i++) { Grid.SetColumn(cells[i], i); g.Children.Add(cells[i]); }
            varRows.Add(r);
            varsPanel.Children.Add(g);
            // 1.5.1: clic sulla riga (o fuoco su una sua casella) = mostra dove agisce
            g.AddHandler(Avalonia.Input.InputElement.PointerPressedEvent, (s, e) => VarMostraDove(r.Name.Text), Avalonia.Interactivity.RoutingStrategies.Tunnel);
            r.Name.GotFocus += (s, e) => VarMostraDove(r.Name.Text);
            r.Expr.GotFocus += (s, e) => VarMostraDove(r.Name.Text);
        }

        /// <summary>1.5.1: struttura a destra con in ocra i fili su cui agisce la variabile.</summary>
        void VarMostraDove(string nome)
        {
            if (varView == null) return;
            varScelta = nome;
            varView.Geo = geo;
            varView.Ground = false;
            varView.OcraDiretti = varView.OcraIndiretti = null;
            if (model == null || geo == null || string.IsNullOrWhiteSpace(nome)) { varInfo.Text = model == null ? Lang.T("Nessun modello aperto.") : ""; varView.InvalidateVisual(); return; }
            Dictionary<int, List<string>> dir, ind; List<string> tramite;
            VariabileNuova.DoveAgisce(model, nome.Trim(), out dir, out ind, out tramite);
            varView.OcraDiretti = dir; varView.OcraIndiretti = ind;
            List<string> righe = new List<string>();
            foreach (KeyValuePair<int, List<string>> kv in dir) righe.Add(Lang.T("filo") + " " + kv.Key + ": " + string.Join(", ", kv.Value));
            string t = nome.Trim() + (righe.Count > 0 ? ": " + string.Join(" · ", righe) : ": " + Lang.T("non tocca la geometria (nessun filo la usa direttamente)"));
            if (ind.Count > 0) t += "\n" + Lang.F("attraverso {0}: fili {1}", string.Join(", ", tramite), string.Join(", ", ind.Keys));
            varInfo.Text = t;
            varView.InvalidateVisual();
        }

        void SetRowState(VarRow r, SyVar v)
        {
            if (v.Error != null)
            {
                r.State.Text = "errore: " + v.Error;
                r.State.Foreground = Pal.B(Pal.Red);
                r.Value.Text = "-";
                return;
            }
            bool used = model != null && model.UsesVariable(v.Name);
            r.State.Text = used ? "usata nel modello" : "non usata";
            r.State.Foreground = Pal.B(used ? Pal.Green : Pal.Muted);
            if (model != null && r.Descr != null) r.Descr.Watermark = model.DescribeUse(v.Name);
        }

        // casella con il pulsante a tendina a destra
        Control WithMenu(TextBox tb, Func<MenuFlyout> menu, string tip)
        {
            Button b = new Button
            {
                Content = "\u25BE", FontSize = 14, Padding = new Thickness(7, 0), Margin = new Thickness(0, 2, 2, 2), MinWidth = 28,
                Background = Pal.B(Pal.Field), Foreground = Pal.B(Pal.Accent), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 4, 4, 0), VerticalAlignment = VerticalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center
            };
            ToolTip.SetTip(b, tip);
            b.Click += (s, e) => { MenuFlyout f = menu(); f.ShowAt(b); };
            DockPanel d = new DockPanel();
            DockPanel.SetDock(b, Dock.Right);
            d.Children.Add(b);
            d.Children.Add(tb);
            return d;
        }

        // variabili tipiche delle antenne: nome, descrizione, espressione di partenza
        static readonly string[][] VarPresets =
        {
            new[] { "F", "frequenza di progetto (MHz)", "144.3" },
            new[] { "WL", "lunghezza d'onda in aria (m)", "299.792458/F" },
            new[] { "L", "lunghezza totale dell'elemento (m)", "1000MM" },
            new[] { "H", "meta' lunghezza dell'elemento (m)", "L/2" },
            new[] { "LR", "lunghezza del riflettore (m)", "1040MM" },
            new[] { "LD", "lunghezza del dipolo alimentato (m)", "990MM" },
            new[] { "L1", "lunghezza del primo direttore (m)", "940MM" },
            new[] { "XR", "posizione del riflettore sul boom (m)", "0" },
            new[] { "XD", "posizione del dipolo sul boom (m)", "380MM" },
            new[] { "X1", "posizione del primo direttore sul boom (m)", "580MM" },
            new[] { "SP", "spaziatura tra due elementi (m)", "200MM" },
            new[] { "D", "diametro del conduttore (m)", "4MM" },
            new[] { "R", "raggio del conduttore (m)", "D/2" },
            new[] { "Z", "altezza dal suolo (m)", "10" },
            new[] { "NS", "numero di segmenti (dispari sul filo alimentato)", "ODD(21)" },
            new[] { "G", "distanza dei morsetti di alimentazione (m)", "10MM" },
            new[] { "A", "angolo (gradi)", "30" },
        };

        MenuFlyout NameMenu(VarRow r)
        {
            MenuFlyout f = new MenuFlyout();
            foreach (string[] p in VarPresets)
            {
                string[] pp = p;
                MenuItem mi = new MenuItem { Header = pp[0] + "  -  " + pp[1] };
                mi.Click += (s, e) =>
                {
                    string n = pp[0];
                    int k = 2;
                    while (varRows.Exists(o => o != r && string.Equals(o.Name.Text, n, StringComparison.OrdinalIgnoreCase))) n = pp[0] + k++;
                    r.Name.Text = n;
                    if (string.IsNullOrWhiteSpace(r.Descr.Text)) r.Descr.Text = pp[1];
                    string ex = (r.Expr.Text ?? "").Trim();
                    if (ex.Length == 0 || ex == "0") r.Expr.Text = pp[2];
                    r.Name.Focus();
                };
                f.Items.Add(mi);
            }
            return f;
        }

        MenuFlyout ExprMenu(VarRow r)
        {
            MenuFlyout f = new MenuFlyout();
            MenuItem vars = new MenuItem { Header = "Variabili" };
            foreach (VarRow o in varRows)
            {
                if (o == r || string.IsNullOrWhiteSpace(o.Name.Text)) continue;
                string n = o.Name.Text.Trim();
                string d = string.IsNullOrWhiteSpace(o.Descr.Text) ? (o.Descr.Watermark ?? "") : o.Descr.Text;
                MenuItem mi = new MenuItem { Header = n + (d.Length > 0 ? "  -  " + d : "") };
                mi.Click += (s, e) => InsertAtCaret(r.Expr, n);
                vars.Items.Add(mi);
            }
            if (vars.Items.Count == 0) vars.Items.Add(new MenuItem { Header = "(nessun'altra variabile)", IsEnabled = false });
            f.Items.Add(vars);
            MenuItem units = new MenuItem { Header = "Unita' di misura" };
            foreach (string[] u in new[] { new[] { "MM", "millimetri" }, new[] { "CM", "centimetri" }, new[] { "IN", "pollici" }, new[] { "FT", "piedi" },
                                             new[] { "PF", "picofarad" }, new[] { "NF", "nanofarad" }, new[] { "UF", "microfarad" }, new[] { "NH", "nanohenry" }, new[] { "UH", "microhenry" } })
            {
                string[] uu = u;
                MenuItem mi = new MenuItem { Header = uu[0] + "  -  " + uu[1] + " (6" + uu[0] + " = 6*" + uu[0] + ")" };
                mi.Click += (s, e) => InsertAtCaret(r.Expr, uu[0]);
                units.Items.Add(mi);
            }
            f.Items.Add(units);
            MenuItem fun = new MenuItem { Header = "Funzioni" };
            foreach (string[] fn in new[] { new[] { "SIN(", "seno (gradi)" }, new[] { "COS(", "coseno (gradi)" }, new[] { "TAN(", "tangente (gradi)" }, new[] { "ATN(", "arcotangente (gradi)" },
                                              new[] { "SQR(", "radice quadrata" }, new[] { "ABS(", "valore assoluto" }, new[] { "INT(", "arrotonda all'intero" }, new[] { "FIX(", "tronca all'intero" },
                                              new[] { "ODD(", "intero dispari (segmenti)" }, new[] { "MAX(", "massimo di due valori" }, new[] { "MIN(", "minimo di due valori" },
                                              new[] { "MOD(", "resto della divisione" }, new[] { "LOG(", "logaritmo naturale" }, new[] { "LOG10(", "logaritmo decimale" }, new[] { "EXP(", "esponenziale" } })
            {
                string[] ff = fn;
                MenuItem mi = new MenuItem { Header = ff[0] + ")  -  " + ff[1] };
                mi.Click += (s, e) => InsertAtCaret(r.Expr, ff[0]);
                fun.Items.Add(mi);
            }
            f.Items.Add(fun);
            MenuItem pi = new MenuItem { Header = "PI  -  3,14159..." };
            pi.Click += (s, e) => InsertAtCaret(r.Expr, "PI");
            f.Items.Add(pi);
            return f;
        }

        static void InsertAtCaret(TextBox tb, string text)
        {
            string t = tb.Text ?? "";
            int c = Math.Max(0, Math.Min(tb.CaretIndex, t.Length));
            if (t == "0") { t = ""; c = 0; }
            tb.Text = t.Substring(0, c) + text + t.Substring(c);
            tb.CaretIndex = c + text.Length;
            tb.Focus();
        }

        static string FormatVal(double v)
        {
            return v.ToString("0.#########", CultureInfo.CurrentCulture);
        }

        static double NiceStep(double raw)
        {
            if (raw <= 0) return 0.001;
            double p = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double m = raw / p;
            return (m < 2 ? 1 : (m < 5 ? 2 : 5)) * p;
        }

        // -/+: solo se l'espressione e' un numero; altrimenti si modifica il testo
        void StepVar(VarRow r, int dir)
        {
            // il passo e' nell'unita' del valore calcolato (metri per le lunghezze)
            string ex = Expr.NormalizeDecimal((r.Expr.Text ?? "").Trim().Replace(" ", ""));
            double st = ParseNum(r.Step.Text, 0.001);
            CultureInfo ci = CultureInfo.InvariantCulture;
            if (Expr.IsPlainNumber(ex))
            {
                double v = double.Parse(ex, NumberStyles.Float, ci) + dir * st;
                if (Math.Abs(v) < 1e-15) v = 0;
                r.Expr.Text = v.ToString("0.##########", ci);
                ApplyVars();
                return;
            }
            // numero per unita' (1000*MM, 25*CM, 6MM): cambia il numero e lascia l'unita' come e' scritta
            System.Text.RegularExpressions.Match mt = System.Text.RegularExpressions.Regex.Match(ex, @"^([0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)(\*?)([A-Za-z_][A-Za-z0-9_]*)$");
            if (mt.Success && Expr.IsReserved(mt.Groups[3].Value))
            {
                double k = Expr.Eval(mt.Groups[3].Value, null);
                double n = double.Parse(mt.Groups[1].Value, NumberStyles.Float, ci) + dir * st / k;
                if (Math.Abs(n) < 1e-12) n = 0;
                r.Expr.Text = n.ToString("0.########", ci) + mt.Groups[2].Value + mt.Groups[3].Value;
                ApplyVars();
                return;
            }
            // espressione fatta solo di numeri e costanti (nessuna variabile): diventa un numero
            double c;
            try { c = Expr.Eval(ex, null); }
            catch (NecException)
            {
                Status("La variabile " + r.Name.Text + " dipende da altre variabili: si cambia regolando quelle, oppure modificando l'espressione.", true);
                return;
            }
            r.Expr.Text = (c + dir * st).ToString("0.##########", ci);
            ApplyVars();
        }

        void ApplyVars()
        {
            if (model == null) { Status("Apri prima un modello.", true); return; }
            List<SyVar> list = new List<SyVar>();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (VarRow r in varRows)
            {
                string n = (r.Name.Text ?? "").Trim();
                string ex = Expr.NormalizeDecimal((r.Expr.Text ?? "").Trim().Replace(" ", ""));
                if (!Expr.IsValidName(n)) { Status("Nome di variabile non valido o riservato: '" + n + "'", true); return; }
                if (!names.Add(n)) { Status("Variabile definita due volte: " + n, true); return; }
                if (ex.Length == 0) { Status("La variabile " + n + " non ha un'espressione.", true); return; }
                list.Add(new SyVar { Name = n, Expr = ex, Descr = (r.Descr.Text ?? "").Trim() });
            }
            model.Variables = list;
            List<string> err = model.Reevaluate();
            foreach (SyVar v in model.Variables)
                Registro.Scrivi("AZIONE", "variabile " + v.Name + " = " + v.Expr + (v.Error == null ? " -> " + v.Value.ToString("R", CultureInfo.InvariantCulture) : " -> errore: " + v.Error));
            for (int i = 0; i < varRows.Count && i < model.Variables.Count; i++)
            {
                SyVar v = model.Variables[i];
                varRows[i].Value.Text = v.Error == null ? FormatVal(v.Value) : "-";
                SetRowState(varRows[i], v);
            }
            if (txtNec != null) txtNec.Text = model.ToNec();
            if (err.Count > 0)
            {
                lastError = string.Join("\n  ", err);
                Status("Errore nelle variabili: " + err[0], true);
                UpdateNotes();
                return;
            }
            lastError = null;
            RefreshWiresTable();
            Calculate(true);
        }
    }
}
