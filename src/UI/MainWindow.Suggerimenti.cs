// YA-NEC 1.6.0 - Calcolatori > Suggerimenti (bozzetto «spunti dalla ricerca su GitHub», proposta 1, approvato da IC8TEM il 23/11/2026
// 14:27): a sinistra «Cosa ti serve», a destra «Antenne possibili» ordinate secondo quello che conta, con «apri» che porta nel
// calcolatore con questi valori. Calcolo in Engine/Suggerimenti.cs.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        CancellationTokenSource suggCts;

        Control BuildSuggerimentiPage()
        {
            StackPanel sx = new StackPanel { Spacing = 4, Width = 380 };
            sx.Children.Add(new TextBlock { Text = "Cosa ti serve", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            double f0 = model != null ? model.FreqMHz : 14.175;
            TextBox tF = Tb(Fmt(f0, 3), 110), tH = Tb("10", 110), tIng = Tb("12", 110);
            ComboBox cTer = new ComboBox { ItemsSource = Suggerimenti.Terreni, SelectedIndex = 0, Width = 230 };
            ComboBox cOb = new ComboBox { ItemsSource = Suggerimenti.Obiettivi, SelectedIndex = 0, Width = 230 };
            ComboBox cPol = new ComboBox { ItemsSource = Suggerimenti.Polarizzazioni, SelectedIndex = 2, Width = 230 };
            ToolTip.SetTip(tH, "Altezza del centro dell'antenna (delle verticali: della base). La V invertita usa l'altezza del vertice, il ground plane quella della base; la verticale λ/4 con radiali sta a terra; la NVIS sceglie da sola l'altezza (0,175 λ).");
            ToolTip.SetTip(tIng, "Lato piu' lungo dell'antenna vista dall'alto (radiali compresi). 0 = nessun limite.");
            Func<string, Control, string, Control> riga = (et, c, um) =>
            {
                Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("130,*,Auto"), Margin = new Thickness(0, 1) };
                TextBlock l = new TextBlock { Text = et, VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore) };
                Grid.SetColumn(c, 1); c.HorizontalAlignment = HorizontalAlignment.Left;
                g.Children.Add(l); g.Children.Add(c);
                if (!string.IsNullOrEmpty(um)) { TextBlock u = new TextBlock { Text = um, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }; Grid.SetColumn(u, 2); g.Children.Add(u); }
                return g;
            };
            sx.Children.Add(riga("Frequenza", tF, "MHz"));
            sx.Children.Add(riga("Altezza dal suolo", tH, "m"));
            sx.Children.Add(riga("Terreno", cTer, ""));
            sx.Children.Add(riga("Ingombro massimo", tIng, "m"));
            sx.Children.Add(riga("Cosa conta", cOb, ""));
            sx.Children.Add(riga("Polarizzazione", cPol, ""));
            Button bCalc = Btn("Calcola tutte", true);
            bCalc.Margin = new Thickness(0, 8, 0, 0);
            Button bFerma = Btn("Ferma", false);
            bFerma.Margin = new Thickness(6, 8, 0, 0);
            bFerma.IsEnabled = false;
            WrapPanel bw = new WrapPanel();
            bw.Children.Add(bCalc); bw.Children.Add(bFerma);
            sx.Children.Add(bw);
            ProgressBar pb = new ProgressBar { Height = 6, IsVisible = false, Margin = new Thickness(0, 4, 0, 0) };
            sx.Children.Add(pb);
            sx.Children.Add(NotaT("Ogni antenna usa il suo calcolatore con i valori di partenza, portata all'altezza e sul terreno scelti, e un calcolo NEC (in parallelo). ",
                "Idea di AntAn di sozden (Apache-2.0): nessun codice copiato."));

            StackPanel dx = new StackPanel { Spacing = 4 };
            dx.Children.Add(new TextBlock { Text = "Antenne possibili", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            TextBlock sotto = Nota("Premi «Calcola tutte»: le antenne adatte alla frequenza si calcolano e si ordinano secondo quello che conta.");
            dx.Children.Add(sotto);
            ContentControl tab = new ContentControl();
            dx.Children.Add(new ScrollViewer { Content = tab, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
            StackPanel note = new StackPanel { Spacing = 2 };
            dx.Children.Add(note);

            bFerma.Click += (s, e) => { if (suggCts != null) suggCts.Cancel(); };
            bCalc.Click += async (s, e) =>
            {
                SuggRichiesta q = new SuggRichiesta
                {
                    F = ParseNum(tF.Text, 14.175), H = ParseNum(tH.Text, 10), IngombroMax = ParseNum(tIng.Text, 0),
                    Terreno = Math.Max(0, cTer.SelectedIndex), Obiettivo = Math.Max(0, cOb.SelectedIndex), Pol = Math.Max(0, cPol.SelectedIndex)
                };
                if (!(q.F >= 1.5 && q.F <= 6000)) { Status("Suggerimenti: frequenza tra 1,5 e 6000 MHz.", true); return; }
                if (q.H < 0) q.H = 0;
                suggCts = new CancellationTokenSource();
                CancellationToken tok = suggCts.Token;
                bCalc.IsEnabled = false; bFerma.IsEnabled = true; pb.IsVisible = true; pb.Value = 0; pb.Maximum = 14;
                Progress<int> pr = new Progress<int>(v => pb.Value = v);
                DateTime t0 = DateTime.Now;
                try
                {
                    List<SuggRiga> r;
                    using (Prestazioni.Misura("Suggerimenti di antenna"))
                    using (Structure3DView.PausaAnimazione())
                        r = await Task.Run(() => Suggerimenti.Calcola(q, pr, tok));
                    tab.Content = TabellaSuggerimenti(r, q);
                    sotto.Text = Lang.F("{0} antenne calcolate in {1} s, ordinate per «{2}». Le righe in grigio non rispettano ingombro o polarizzazione.", r.Count,
                        ((DateTime.Now - t0).TotalSeconds).ToString("0", CultureInfo.CurrentCulture), Lang.T(Suggerimenti.Obiettivi[q.Obiettivo]));
                    note.Children.Clear();
                    note.Children.Add(Nota("«apri» porta nel calcolatore con questi valori; da lì «Apri come modello» come sempre. Valori indicativi di partenza: ogni calcolatore ha poi la sua «Porta in risonanza» o ottimizzazione."));
                    Registro.Scrivi("AZIONE", string.Format(CultureInfo.InvariantCulture, "suggerimenti: {0:0.###} MHz, {1} m, {2} antenne", q.F, q.H, r.Count));
                }
                catch (OperationCanceledException) { sotto.Text = Lang.T("Fermato."); }
                catch (Exception ex) { Status("Suggerimenti: " + ex.GetBaseException().Message, true); }
                finally { bCalc.IsEnabled = true; bFerma.IsEnabled = false; pb.IsVisible = false; }
            };

            Grid g0 = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,16,*") };
            Control a = Pannello("Suggerimenti", sx), b = Pannello("Risultato", dx);
            Grid.SetColumn(b, 2);
            g0.Children.Add(a); g0.Children.Add(b);
            return new ScrollViewer { Content = new Border { Padding = new Thickness(8), Child = g0 }, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }

        Control TabellaSuggerimenti(List<SuggRiga> l, SuggRichiesta q)
        {
            Grid g = new Grid();
            string[] h = { "Antenna", "Guadagno", "Avanti/indietro", "Angolo", "ROS", "Ingombro", "" };
            for (int c = 0; c < h.Length; c++) g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (int c = 0; c < h.Length; c++)
            {
                TextBlock t = new TextBlock { Text = c == 4 ? Lang.T("ROS") + " (Z0)" : Lang.T(h[c]), Foreground = Pal.B(Pal.Muted), FontWeight = FontWeight.SemiBold, Margin = new Thickness(8, 4), HorizontalAlignment = c == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                Grid.SetColumn(t, c); g.Children.Add(t);
            }
            int r = 1;
            foreach (SuggRiga x in l)
            {
                g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                bool grigio = x.Errore != null || x.Fuori || x.Nota != null;
                Color col = grigio ? Pal.Muted : Pal.Fore;
                Action<int, string, bool> cella = (c, testo, dx) =>
                {
                    TextBlock t = new TextBlock { Text = testo, Foreground = Pal.B(col), Margin = new Thickness(8, 3), HorizontalAlignment = dx ? HorizontalAlignment.Right : HorizontalAlignment.Left, FontFamily = dx ? Pal.Mono : FontFamily.Default, VerticalAlignment = VerticalAlignment.Center };
                    Lang.NonTradurre(t);
                    Grid.SetRow(t, r); Grid.SetColumn(t, c); g.Children.Add(t);
                };
                cella(0, Lang.T(x.Nome), false);
                if (x.Errore != null)
                {
                    TextBlock t = new TextBlock { Text = x.Errore, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(8, 3), TextWrapping = TextWrapping.Wrap, MaxWidth = 520 };
                    Grid.SetRow(t, r); Grid.SetColumn(t, 1); Grid.SetColumnSpan(t, 5); g.Children.Add(t);
                }
                else
                {
                    cella(1, Fmt(x.G, 1) + " dBi", true);
                    cella(2, Fmt(double.IsNaN(x.Fb) ? 0 : x.Fb, 1) + " dB", true);
                    cella(3, q.Obiettivo == 3 ? Lang.F("zenit {0} dBi", Fmt(x.Zenit, 1)) : Fmt(x.El, 0) + "°", true);
                    cella(4, Fmt(x.Ros, 2) + (Math.Abs(x.Z0 - 50) > 0.5 ? " (" + Fmt(x.Z0, 0) + " Ω)" : ""), true);
                    ToolTip.SetTip(g.Children[g.Children.Count - 1], "Z = " + Fmt(x.Z.Real, 1) + (x.Z.Imaginary < 0 ? " − j" : " + j") + Fmt(Math.Abs(x.Z.Imaginary), 1) + " Ω");
                    string ing = x.Profondo > 0.15 ? Fmt(x.Largo, 1) + " × " + Fmt(x.Profondo, 1) + " m" : Fmt(x.Largo, 1) + " m";
                    if (x.Alto > Math.Max(1, x.Largo * 0.8)) ing += Lang.F(", alta {0} m", Fmt(x.Alto, 1));
                    if (x.Fuori) ing += " — " + Lang.T("fuori dall'ingombro");
                    else if (x.Nota != null) ing += " — " + Lang.T(x.Nota);
                    cella(5, ing, true);
                    Button bA = Btn("apri", false);
                    bA.Margin = new Thickness(8, 1);
                    SuggRiga xx = x;
                    bA.Click += (s, e) =>
                    {
                        CalcRimando rr = new CalcRimando { Voce = xx.Chiave };
                        foreach (KeyValuePair<string, double> kv in xx.Valori) rr.Valori[kv.Key] = kv.Value;
                        ApriRimando(rr, null);
                        Status(Lang.F("{0}: calcolatore aperto con i valori dei suggerimenti.", Lang.T(xx.Nome)));
                    };
                    Grid.SetRow(bA, r); Grid.SetColumn(bA, 6); g.Children.Add(bA);
                }
                Border sep = new Border { BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1), IsHitTestVisible = false };
                Grid.SetRow(sep, r); Grid.SetColumnSpan(sep, h.Length); g.Children.Insert(0, sep);
                r++;
            }
            return g;
        }
    }
}
