// YA-NEC - finestra "Abaco delle filari" della scheda Calcolatori > HF > Filari (anteprima approvata da IC8TEM il 22/11/2026 16:46,
// «si senza pubblicare»). Tabelle calcolate con «YA-NEC --abaco-filari» (Diag/AbacoFilari.cs) e salvate in data/abaco_filari.tsv:
// filo nudo da 1 mm orizzontale a 3 m, contrappeso orizzontale opposto, trasformatore ideale, terreno medio e perfetto.
// «Usa questa riga nella scheda» scrive i valori nella scheda; poi il calcolo resta libero (IC8TEM 16:30: «permetti di fare calcolo libero»).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        /// <summary>Sezioni del file dell'abaco: chiave "terreno|titolo" -> righe (la prima e' l'intestazione).</summary>
        static Dictionary<string, List<string[]>> LeggiAbaco()
        {
            Dictionary<string, List<string[]>> d = new Dictionary<string, List<string[]>>();
            string p = Path.Combine(AppPaths.DataDir, "abaco_filari.tsv");
            if (!File.Exists(p)) return d;
            string cur = null;
            foreach (string r in File.ReadAllLines(p))
            {
                if (r.StartsWith("## ", StringComparison.Ordinal)) { cur = r.Substring(3).Trim(); d[cur] = new List<string[]>(); continue; }
                if (cur == null || r.Trim().Length == 0 || r.StartsWith("#", StringComparison.Ordinal)) continue;
                d[cur].Add(r.Split('\t'));
            }
            return d;
        }

        static IBrush ColoreRos(string v)
        {
            if (string.IsNullOrEmpty(v)) return Brushes.Transparent;
            double x;
            if (v.StartsWith(">", StringComparison.Ordinal)) x = 999;
            else if (!double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out x)) return Brushes.Transparent;
            Color c = x <= 1.5 ? Pal.Green : x <= 2 ? Pal.Green : x <= 3 ? Pal.Yellow : x <= 10 ? Pal.Orange : Pal.Red;
            return Pal.B(Pal.A(x <= 1.5 ? 110 : x <= 2 ? 60 : 80, c));
        }

        void ApriAbacoFilari(SimpleCalc sc)
        {
            Dictionary<string, List<string[]>> dati = LeggiAbaco();
            if (dati.Count == 0) { Status("Abaco delle filari non trovato (data/abaco_filari.tsv).", true); return; }
            Window w = new Window { Title = Lang.T("Abaco delle filari") + " - YA-NEC", Width = Math.Max(1180, Math.Min(1560, Bounds.Width - 40)), Height = 760, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel) };
            DockPanel root = new DockPanel { Margin = new Thickness(14) };
            StackPanel testa = new StackPanel { Spacing = 6 };
            DockPanel.SetDock(testa, Dock.Top);
            testa.Children.Add(new TextBlock { Text = "Abaco delle filari", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            testa.Children.Add(new TextBlock { Text = "Filo nudo da 1 mm orizzontale a 3 m, contrappeso orizzontale dalla parte opposta alla stessa altezza, trasformatore ideale (ROS su 50 x rapporto). Calcolato con il NEC di YA-NEC. Con altre altezze, disposizioni o fili conta il calcolo della scheda.",
                TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Foreground = Pal.B(Pal.Muted) });
            WrapPanel p1 = new WrapPanel(), p2 = new WrapPanel(), p3 = new WrapPanel();
            testa.Children.Add(p1); testa.Children.Add(p2); testa.Children.Add(p3);
            StackPanel piede = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
            DockPanel.SetDock(piede, Dock.Bottom);
            TextBlock nota = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 8, 0, 0) };
            DockPanel.SetDock(nota, Dock.Bottom);
            Button usa = Btn("Usa questa riga nella scheda", true), chiudi = Btn("Chiudi", false);
            usa.IsEnabled = false;
            piede.Children.Add(usa); piede.Children.Add(chiudi);
            root.Children.Add(testa); root.Children.Add(piede); root.Children.Add(nota);
            ScrollViewer sv = new ScrollViewer { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, Margin = new Thickness(0, 8, 0, 0) };
            root.Children.Add(sv);
            w.Content = root;

            string tab = "49", terreno = "medio", contr = "con";
            string[] scelta = null; string sezScelta = null;
            Action ridisegna = null;
            Func<string, bool, Action, Button> pill = (t, on, click) =>
            {
                Button b = Btn(t, on);
                b.Click += (s, e) => click();
                return b;
            };
            Action pillole = () =>
            {
                p1.Children.Clear(); p2.Children.Clear(); p3.Children.Clear();
                foreach (string[] t in new[] { new[] { "49", "EFHW 49:1" }, new[] { "64", "EFHW 64:1" }, new[] { "9m", "9:1, lunghezze migliori" }, new[] { "9v", "9:1, lunghezze VE3EED" } })
                { string k = t[0]; p1.Children.Add(pill(t[1], tab == k, () => { tab = k; ridisegna(); })); }
                foreach (string[] t in new[] { new[] { "medio", "terreno medio" }, new[] { "perfetto", "terreno perfetto" } })
                { string k = t[0]; p2.Children.Add(pill(t[1], terreno == k, () => { terreno = k; ridisegna(); })); }
                if (tab == "49" || tab == "64")
                    foreach (string[] t in new[] { new[] { "con", "contrappeso 0,05 λ" }, new[] { "senza", "senza contrappeso" } })
                    { string k = t[0]; p3.Children.Add(pill(t[1], contr == k, () => { contr = k; ridisegna(); })); }
            };
            ridisegna = () =>
            {
                pillole();
                scelta = null; usa.IsEnabled = false;
                string titolo = null;
                foreach (string k in dati.Keys)
                {
                    if (!k.StartsWith("[" + terreno + "]", StringComparison.Ordinal)) continue;
                    if ((tab == "49" || tab == "64") && k.Contains("EFHW " + tab + ":1") && k.Contains(contr == "senza" ? "senza contrappeso" : "0,05 lambda")) titolo = k;
                    if (tab == "9m" && k.Contains("9:1 scansione")) titolo = k;
                    if (tab == "9v" && k.Contains("9:1 (Z0")) titolo = k;
                }
                sezScelta = titolo;
                if (titolo == null) { sv.Content = new TextBlock { Text = "Tabella non presente nel file dell'abaco.", Foreground = Pal.B(Pal.Muted) }; return; }
                List<string[]> righe = new List<string[]>(dati[titolo]);
                bool efhw = tab == "49" || tab == "64";
                string[] intest = righe[0];
                righe.RemoveAt(0);
                righe.RemoveAll(r => r.Length < 4);   // righe "MIGLIORE"
                if (tab == "9m") righe.Sort((a, b) => { int c = int.Parse(b[3], CultureInfo.InvariantCulture).CompareTo(int.Parse(a[3], CultureInfo.InvariantCulture)); return c != 0 ? c : double.Parse(a[0], CultureInfo.InvariantCulture).CompareTo(double.Parse(b[0], CultureInfo.InvariantCulture)); });
                int primaBanda = 4;
                Grid g = new Grid();
                for (int c = 0; c < intest.Length; c++) g.ColumnDefinitions.Add(new ColumnDefinition(c < primaBanda ? GridLength.Auto : new GridLength(62)));
                for (int r = 0; r <= righe.Count; r++) g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int c = 0; c < intest.Length; c++)
                {
                    TextBlock h = new TextBlock { Text = Lang.T(intest[c]), FontWeight = FontWeight.SemiBold, FontSize = 12, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(6, 2), HorizontalAlignment = c < primaBanda ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                    Grid.SetColumn(h, c); g.Children.Add(h);
                }
                List<Border> sfondi = new List<Border>();
                for (int r = 0; r < righe.Count; r++)
                {
                    string[] riga = righe[r];
                    Border sf = new Border { Background = Brushes.Transparent, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1), Cursor = new Cursor(StandardCursorType.Hand) };
                    Grid.SetRow(sf, r + 1); Grid.SetColumnSpan(sf, intest.Length); g.Children.Add(sf); sfondi.Add(sf);
                    for (int c = 0; c < intest.Length && c < riga.Length; c++)
                    {
                        string v = riga[c];
                        string vis = efhw ? (c >= 2 ? v.Replace('.', ',') : Lang.TC(v)) : (c == 2 ? Lang.T(v) : v.Replace('.', ','));
                        Border cella = new Border { Background = c >= primaBanda ? ColoreRos(v) : Brushes.Transparent, Margin = new Thickness(1), IsHitTestVisible = false };
                        cella.Child = new TextBlock { Text = vis, FontSize = 12.5, TextWrapping = efhw && c == 1 ? TextWrapping.Wrap : TextWrapping.NoWrap, MaxWidth = efhw && c == 1 ? 290 : double.PositiveInfinity, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(6, 2), HorizontalAlignment = c < primaBanda ? HorizontalAlignment.Left : HorizontalAlignment.Right, FontFamily = c >= 2 ? new FontFamily("Consolas, Menlo, monospace") : FontFamily.Default };
                        Grid.SetRow(cella, r + 1); Grid.SetColumn(cella, c); g.Children.Add(cella);
                    }
                    string[] r0 = riga; Border b0 = sf;
                    sf.PointerPressed += (s, e) =>
                    {
                        foreach (Border x in sfondi) x.Background = Brushes.Transparent;
                        b0.Background = Pal.B(Pal.A(70, Pal.Accent));
                        scelta = r0; usa.IsEnabled = true;
                    };
                }
                sv.Content = g;
                nota.Text = Lang.T(efhw ? (terreno == "medio" ? "Terreno medio: la mezz'onda accordata per una banda da' ROS 1,0-1,7 su quella banda; la multibanda cede sulla fondamentale e guadagna sulle armoniche; il quarto d'onda supera 99 alla banda piu' bassa." :
                        "Terreno perfetto: a 3 m il filo vede la propria immagine in controfase; sulle bande basse il ROS resta alto anche alla mezz'onda. La scheda calcola sempre con il terreno medio.")
                    : "9:1, dagli 80 ai 10 m: contrappesi fissi (0-10 m) e in percentuale del filo; il migliore ha piu' bande con ROS fino a 3 e, a parita', la media dei ROS piu' bassa.");
                if (efhw && contr == "senza") nota.Text += " " + Lang.T("Senza contrappeso fa da contrappeso la calza di 10 m di coassiale senza choke: i ROS bassi sulle bande alte vengono dalla calza che irradia e porta RF in stazione.");
            };
            usa.Click += (s, e) =>
            {
                if (scelta == null) return;
                bool efhw = tab == "49" || tab == "64";
                Action<string, double> set = (k, v) => { CalcParam p = sc.Par(k); if (p != null) p.Value = v; };
                set("iso", 0); set("d", 1); set("disp", 5); set("hm", 3); set("bob", 0); set("pw", Math.Max(5, sc.P("pw")));
                if (efhw)
                {
                    int ib = Array.IndexOf(CalcFilari.BandeNomi, scelta[0]);
                    double f = ib >= 0 ? CalcFilari.BandeF[ib] : 7.1;
                    double L = double.Parse(scelta[2], CultureInfo.InvariantCulture);
                    set("tipo", scelta[1].Contains("multibanda") ? 0 : 1);
                    set("f", f); set("tr", tab == "64" ? 1 : 0); set("cp", contr == "senza" ? 2 : 0);
                    if (contr == "senza") set("coax", double.Parse(scelta[3], CultureInfo.InvariantCulture));
                    double l0 = (1 - CalcFilari.Estremita(f, 0.0005)) * (299.792458 / f) / 2;
                    sc.Scale = L / l0;
                }
                else
                {
                    double L = double.Parse(scelta[0], CultureInfo.InvariantCulture), cp = double.Parse(scelta[1], CultureInfo.InvariantCulture);
                    set("tipo", 2); set("fl", 3.65); set("L", L); set("trb", 0); set("cpp", Math.Max(1, Math.Round(cp / L * 100, 1)));
                    sc.Scale = 1;
                }
                calcPages.Remove("filari");
                Control pag = BuildGruppoCalc("filari");
                calcPages["filari"] = pag;
                if (fasCalc.Current == "filari") hostCalc.Content = pag;
                Registro.Scrivi("AZIONE", "abaco delle filari: riga " + string.Join(" ", scelta) + " nella scheda");
                Status(Lang.T("Riga dell'abaco nella scheda Filari: ora il calcolo e' libero, si puo' cambiare tutto."));
                w.Close();
            };
            chiudi.Click += (s, e) => w.Close();
            ridisegna();
            Registro.Scrivi("AZIONE", "apre l'abaco delle filari");
            w.Show(this);
        }
    }
}
