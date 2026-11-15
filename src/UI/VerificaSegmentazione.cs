// YA-NEC - finestra "Verifica della segmentazione" con correzione automatica (bozzetto approvato,
// doc/bozzetti_approvati/YA-NEC-anteprima-verifica-segmentazione.png): tabella dei problemi con le caselle,
// correzione automatica (livello, fili, opzioni), riquadro prima/dopo con avvisi, segmenti, tempo, AGT e impedenza,
// pulsanti Anteprima, Applica e ricalcola, Annulla, Copia il rapporto.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public class VerificaSegmentazione : Window
    {
        readonly NecModel model;
        readonly double fmax;
        readonly SegOptions opt = new SegOptions();
        List<SegIssue> issues = new List<SegIssue>();
        readonly StackPanel tabella = new StackPanel();
        readonly TextBlock[,] pd = new TextBlock[5, 2];
        public NecModel Risultato;            // modello corretto (Applica)
        public bool Riapri;
        readonly CheckBox cAuto;
        static readonly string[] Col = { "", "Dove", "Problema", "Ora", "Minimo", "Prudente", "Fuori dal", "Segmenti", "Segm. dopo" };
        static readonly double[] W = { 34, 150, 330, 90, 80, 80, 100, 90, 90 };

        public static bool AutoApri;          // "apri questa finestra da sola prima di ogni calcolo se ci sono avvisi"

        static TextBlock T(string s, Color c, double size = 13.5, FontWeight w = FontWeight.Normal)
        {
            return new TextBlock { Text = s, Foreground = new SolidColorBrush(c), FontSize = size, FontWeight = w, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        }

        public VerificaSegmentazione(NecModel m, double fmaxMHz, string nome, Action<bool> autoCambiata)
        {
            model = m; fmax = fmaxMHz;
            Title = "Verifica della segmentazione";
            Width = 1180; Height = 720;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Pal.B(Pal.Back);
            double lam = 299.792458 / fmax;

            DockPanel root = new DockPanel();
            DockPanel head = new DockPanel { Background = new SolidColorBrush(Color.Parse("#1E293B")) };
            TextBlock ht = T("Verifica della segmentazione", Colors.White, 16, FontWeight.Bold);
            ht.Margin = new Thickness(16, 10);
            TextBlock hr = T(nome + " · controllo alla frequenza più alta dello sweep: " + fmax.ToString("0.0##", CultureInfo.CurrentCulture) + " MHz (λ = " + lam.ToString("0.00", CultureInfo.CurrentCulture) + " m)", Color.Parse("#CBD5E1"));
            hr.Margin = new Thickness(16, 10);
            DockPanel.SetDock(hr, Dock.Right);
            head.Children.Add(hr); head.Children.Add(ht);
            DockPanel.SetDock(head, Dock.Top);
            root.Children.Add(head);

            StackPanel body = new StackPanel { Margin = new Thickness(16, 10), Spacing = 10 };
            body.Children.Add(T("Regole: segmento ≤ λ/10 (minimo) e ≤ λ/20 (prudente); segmento/raggio ≥ 2 (minimo) e ≥ 4 (prudente); segmenti uguali ai lati di sorgenti e porte delle linee. Fonti: EZNEC, ARRL.", Pal.Muted));
            body.Children.Add(new Border { Child = tabella, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), Background = Pal.B(Pal.Panel) });

            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("63*,12,37*") };
            // correzione automatica
            StackPanel corr = new StackPanel { Spacing = 6 };
            corr.Children.Add(T("Correzione automatica", Pal.Fore, 15, FontWeight.Bold));
            corr.Children.Add(Riga("Livello da raggiungere", Pillole(new[] { "minimo λ/10", "prudente λ/20", "segmenti per λ: " + opt.PerLambda }, opt.Livello, k => { opt.Livello = k; Ricontrolla(); })));
            corr.Children.Add(Riga("Quali fili", Pillole(new[] { "solo quelli segnalati", "tutti" }, 0, k => { opt.SoloSegnalati = k == 0; Ricontrolla(); })));
            corr.Children.Add(Casella("numero dispari di segmenti sui fili alimentati al centro", true, v => opt.Dispari = v));
            corr.Children.Add(Casella("pareggia i segmenti ai lati di sorgenti e porte delle linee", true, v => opt.Pareggia = v));
            corr.Children.Add(Casella("segmenti più corti vicino alle giunzioni (rastremazione)", true, v => opt.Giunzioni = v));
            corr.Children.Add(Casella("se il rapporto segmento/raggio non basta: proponi il kernel esteso", false, v => opt.ProponiEK = v));
            Border bc = Box(corr);
            g.Children.Add(bc);
            // prima e dopo
            StackPanel pdBox = new StackPanel();
            pdBox.Children.Add(T("Prima e dopo", Pal.Fore, 15, FontWeight.Bold));
            Grid pg = new Grid { ColumnDefinitions = new ColumnDefinitions("110,*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"), Margin = new Thickness(0, 6, 0, 0) };
            string[] etich = { "Avvisi", "Segmenti totali", "Tempo stimato", "AGT", "Impedenza" };
            Action<int, int, Control> metti = (r, c, x) => { Border b = new Border { Child = x, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0.5), Padding = new Thickness(6, 3), Background = r == 0 ? Pal.B(Pal.Back) : null }; Grid.SetRow(b, r); Grid.SetColumn(b, c); pg.Children.Add(b); };
            metti(0, 1, T("prima", Pal.Muted)); metti(0, 2, T("dopo", Pal.Muted));
            for (int i = 0; i < 5; i++)
            {
                metti(i + 1, 0, T(etich[i], Pal.Fore));
                for (int j = 0; j < 2; j++) { pd[i, j] = T("-", Pal.Fore); pd[i, j].HorizontalAlignment = HorizontalAlignment.Right; metti(i + 1, j + 1, pd[i, j]); }
            }
            pdBox.Children.Add(pg);
            Border bp = Box(pdBox);
            Grid.SetColumn(bp, 2);
            g.Children.Add(bp);
            body.Children.Add(g);

            DockPanel puls = new DockPanel();
            StackPanel pb = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            Button bAnt = Pul("Anteprima", false), bApp = Pul("Applica e ricalcola", true), bAnn = Pul("Annulla", false), bCopia = Pul("Copia il rapporto", false);
            pb.Children.Add(bAnt); pb.Children.Add(bApp); pb.Children.Add(bAnn); pb.Children.Add(bCopia);
            cAuto = new CheckBox { Content = "apri questa finestra da sola prima di ogni calcolo se ci sono avvisi", IsChecked = AutoApri, Foreground = Pal.B(Pal.Muted) };
            cAuto.IsCheckedChanged += (s, e) => { AutoApri = cAuto.IsChecked == true; if (autoCambiata != null) autoCambiata(AutoApri); };
            DockPanel.SetDock(cAuto, Dock.Right);
            puls.Children.Add(cAuto);
            puls.Children.Add(pb);
            body.Children.Add(puls);
            root.Children.Add(new ScrollViewer { Content = body });
            Content = root;

            bAnt.Click += (s, e) => Anteprima(false);
            bApp.Click += (s, e) => Anteprima(true);
            bAnn.Click += (s, e) => Close();
            bCopia.Click += async (s, e) =>
            {
                try { if (Clipboard != null) await Clipboard.SetTextAsync(SegCheck.Rapporto(issues, Title + " - " + hr.Text)); } catch { }
            };
            Ricontrolla();
            pd[1, 0].Text = SegCheck.Segmenti(model).ToString();
        }

        void Ricontrolla()
        {
            issues = SegCheck.Check(model, fmax, opt);
            tabella.Children.Clear();
            Grid h = RigaTab();
            for (int i = 0; i < Col.Length; i++) Cell(h, i, T(Col[i], Pal.Muted, 13));
            h.Background = Pal.B(Pal.Back);
            tabella.Children.Add(h);
            foreach (SegIssue it in issues)
            {
                SegIssue x = it;
                Grid r = RigaTab();
                CheckBox cb = new CheckBox { IsChecked = x.Sel, HorizontalAlignment = HorizontalAlignment.Center };
                cb.IsCheckedChanged += (s, e) => x.Sel = cb.IsChecked == true;
                Cell(r, 0, cb);
                Cell(r, 1, T(x.Dove, Pal.Fore));
                Cell(r, 2, T(x.Problema, Pal.Fore));
                Cell(r, 3, T(x.Ora, Pal.Fore));
                Cell(r, 4, T(x.Minimo, Pal.Fore));
                Cell(r, 5, T(x.Prudente, Pal.Fore));
                bool min = x.Fuori == "minimo";
                Border tag = new Border
                {
                    Background = new SolidColorBrush(Color.Parse(min ? "#FDE2E1" : (x.Fuori == "info" ? "#E8EEF7" : "#FFF0DC"))), CornerRadius = new CornerRadius(8), Padding = new Thickness(7, 1),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Child = T(x.Fuori, Color.Parse(min ? "#B42318" : (x.Fuori == "info" ? "#425066" : "#B26A00")), 12.5)
                };
                Cell(r, 6, tag);
                TextBlock sg = T(x.Segmenti, Pal.Fore);
                if (x.Segmenti.Contains("→")) { sg.Inlines = new Avalonia.Controls.Documents.InlineCollection(); string[] p = x.Segmenti.Split('→'); sg.Inlines.Add(new Avalonia.Controls.Documents.Run(p[0] + "→ ")); sg.Inlines.Add(new Avalonia.Controls.Documents.Run(p[1].Trim()) { FontWeight = FontWeight.Bold }); }
                Cell(r, 7, sg);
                Cell(r, 8, T(x.SegDopo, Pal.Fore));
                tabella.Children.Add(r);
            }
            if (issues.Count == 0)
            {
                Grid r = RigaTab();
                Cell(r, 2, T("✓ nessun avviso: la segmentazione rispetta il livello scelto", Pal.Green));
                tabella.Children.Add(r);
            }
            pd[0, 0].Text = issues.FindAll(i => i.Fuori != "info").Count.ToString();
        }

        Grid RigaTab()
        {
            Grid r = new Grid { MinHeight = 26 };
            foreach (double w in W) r.ColumnDefinitions.Add(new ColumnDefinition(w, GridUnitType.Pixel));
            r.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            return r;
        }

        static void Cell(Grid r, int c, Control x)
        {
            Border b = new Border { Child = x, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, c < 8 ? 1 : 0, 1), Padding = new Thickness(6, 2) };
            Grid.SetColumn(b, c);
            r.Children.Add(b);
        }

        // calcola prima e dopo (AGT, tempo, impedenza) e, con applica = true, chiude restituendo il modello corretto
        void Anteprima(bool applica)
        {
            NecModel dopo;
            try { dopo = SegCheck.Correct(model, fmax, opt, issues); }
            catch (Exception ex) { pd[4, 1].Text = "errore: " + ex.Message; return; }
            if (applica) { Risultato = dopo; Close(); return; }
            int nDopo = SegCheck.Segmenti(dopo);
            pd[1, 1].Text = nDopo.ToString();
            int avvDopo = SegCheck.Check(dopo, fmax, opt).FindAll(i => i.Fuori != "info" && i.Card >= 0).Count;
            pd[0, 1].Text = avvDopo.ToString();
            pd[0, 1].Foreground = Pal.B(avvDopo == 0 ? Pal.Green : Pal.Orange);
            pd[0, 1].FontWeight = FontWeight.Bold;
            for (int j = 0; j < 2; j++) pd[2, j].Text = pd[3, j].Text = "calcolo...";
            pd[4, 0].Text = "calcolo...";
            NecModel prima = model;
            double f = fmax;
            Task.Run(() =>
            {
                Stopwatch w1 = Stopwatch.StartNew();
                RunResult a = Analysis.Run(prima, prima.BuildGeometry(), f, 5.0);
                double t1 = w1.Elapsed.TotalSeconds;
                Stopwatch w2 = Stopwatch.StartNew();
                RunResult b = Analysis.Run(dopo, dopo.BuildGeometry(), f, 5.0);
                double t2 = w2.Elapsed.TotalSeconds;
                return Tuple.Create(a, t1, b, t2);
            }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
            {
                if (t.IsFaulted) { pd[4, 0].Text = "errore: " + t.Exception.GetBaseException().Message; return; }
                CultureInfo cc = CultureInfo.CurrentCulture;
                RunResult a = t.Result.Item1, b = t.Result.Item3;
                pd[2, 0].Text = t.Result.Item2.ToString("0.0", cc) + " s";
                pd[2, 1].Text = t.Result.Item4.ToString("0.0", cc) + " s";
                pd[3, 0].Text = a.AgtDb.ToString("+0.00;−0.00", cc) + " dB";
                pd[3, 1].Text = b.AgtDb.ToString("+0.00;−0.00", cc) + " dB";
                pd[3, 1].Foreground = Pal.B(Math.Abs(b.AgtDb) <= 0.2 ? Pal.Green : Pal.Orange);
                pd[3, 1].FontWeight = FontWeight.Bold;
                Func<Complex, string> z = v => v.Real.ToString("0.0", cc) + (v.Imaginary >= 0 ? " +j" : " −j") + Math.Abs(v.Imaginary).ToString("0.0", cc);
                pd[4, 0].Text = z(a.Zin);
                pd[4, 1].Text = z(b.Zin);
            }));
        }

        static Border Box(Control c)
        {
            return new Border { Child = c, Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 10) };
        }

        static DockPanel Riga(string et, Control c)
        {
            DockPanel d = new DockPanel();
            TextBlock l = T(et, Pal.Fore, 14);
            l.Width = 240;
            DockPanel.SetDock(l, Dock.Left);
            d.Children.Add(l); d.Children.Add(c);
            return d;
        }

        static CheckBox Casella(string t, bool on, Action<bool> cambia)
        {
            CheckBox c = new CheckBox { Content = t, IsChecked = on };
            c.IsCheckedChanged += (s, e) => cambia(c.IsChecked == true);
            return c;
        }

        static Button Pul(string t, bool primo)
        {
            Button b = new Button { Content = t, CornerRadius = new CornerRadius(16), Padding = new Thickness(16, 6), FontWeight = primo ? FontWeight.Bold : FontWeight.Normal };
            if (primo) { b.Background = Pal.B(Pal.Accent); b.Foreground = Brushes.White; }
            else { b.Background = Pal.B(Pal.Panel); b.Foreground = Pal.B(Pal.Accent); b.BorderBrush = Pal.B(Pal.Accent); b.BorderThickness = new Thickness(1); }
            return b;
        }

        static StackPanel Pillole(string[] voci, int sel, Action<int> scelta)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            List<Button> bs = new List<Button>();
            Action<int> stile = k =>
            {
                for (int i = 0; i < bs.Count; i++)
                {
                    bool on = i == k;
                    bs[i].Background = Pal.B(on ? Pal.Accent : Pal.Panel);
                    bs[i].Foreground = on ? Brushes.White : Pal.B(Pal.Accent);
                }
            };
            for (int i = 0; i < voci.Length; i++)
            {
                int k = i;
                Button b = new Button { Content = voci[i], CornerRadius = new CornerRadius(14), Padding = new Thickness(10, 3), BorderBrush = Pal.B(Pal.Accent), BorderThickness = new Thickness(1) };
                b.Click += (s, e) => { stile(k); scelta(k); };
                bs.Add(b);
                sp.Children.Add(b);
            }
            stile(sel);
            return sp;
        }
    }
}
