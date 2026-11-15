// YA-NEC 0.5.1 - pulsante "Controlla il modello" (controllo z3 approvato da IC8TEM il 15/11/2026 11:35): tutti i controlli dei dati
// del modello aperto senza calcolare (Engine/ControlliDati.cs), in una finestra con gli errori prima e gli avvisi dopo, ognuno con
// "come correggere". Il modello non cambia. Spiegazioni nel manuale, capitolo 11 (Controllo dei dati).
using System;
using System.Collections.Generic;
using System.IO;
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
        async Task ApriControlloDati()
        {
            if (model == null) { Status("Apri prima un modello.", true); return; }
            double f = txtFreq != null ? ParseNum(txtFreq.Text, model.FreqMHz) : model.FreqMHz;
            List<ControlloDato> l;
            try { l = ControlliDati.Modello(model, f); }
            catch (Exception ex) { Status("Controllo dei dati interrotto: " + ex.Message, true); return; }
            string nome = string.IsNullOrEmpty(model.FileName) ? "modello" : Path.GetFileName(model.FileName);
            int ne = l.FindAll(x => x.Errore).Count, na = l.Count - ne;
            Registro.Scrivi("AZIONE", "controllo dei dati di " + nome + ": " + ne + " errori, " + na + " avvisi");
            foreach (ControlloDato d in l) Registro.Scrivi("CONTROLLI", d.ToString());

            Window w = new Window { Title = "Controllo dei dati - " + nome, Width = 760, Height = 560, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel) };
            DockPanel dp = new DockPanel { Margin = new Thickness(16) };
            StackPanel testa = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 10) };
            testa.Children.Add(new TextBlock { Text = "Controllo dei dati", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            testa.Children.Add(new TextBlock
            {
                Text = ne == 0 && na == 0 ? "Nessun problema nei controlli che si fanno senza calcolare (" + ControlliDati.Elenco.Length + " in tutto, quelli sui risultati dopo Calcola)."
                    : ne + " errori e " + na + " avvisi a " + f.ToString("0.###", System.Globalization.CultureInfo.CurrentCulture) + " MHz. Il modello non e' stato cambiato.",
                TextWrapping = TextWrapping.Wrap, FontSize = 14, Foreground = Pal.B(ne > 0 ? Pal.Red : (na > 0 ? Pal.Orange : Pal.Green))
            });
            DockPanel.SetDock(testa, Dock.Top);
            dp.Children.Add(testa);
            StackPanel piede = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            Button copia = Btn("Copia il testo", false);
            string rapporto = ControlliDati.Rapporto(l, nome);
            copia.Click += async (s, e) => { try { if (w.Clipboard != null) await w.Clipboard.SetTextAsync(rapporto); Status("Testo del controllo copiato."); } catch { } };
            Button chiudi = Btn("Chiudi", true);
            chiudi.Click += (s, e) => w.Close();
            piede.Children.Add(copia);
            piede.Children.Add(chiudi);
            DockPanel.SetDock(piede, Dock.Bottom);
            dp.Children.Add(piede);
            StackPanel righe = new StackPanel { Spacing = 8 };
            foreach (bool err in new[] { true, false })
                foreach (ControlloDato d in l)
                {
                    if (d.Errore != err) continue;
                    StackPanel r = new StackPanel { Spacing = 2 };
                    r.Children.Add(new TextBlock { Text = (err ? "ERRORE  [" : "AVVISO  [") + d.Codice + "]  " + d.Testo, TextWrapping = TextWrapping.Wrap, FontSize = 13.5, Foreground = Pal.B(err ? Pal.Red : Pal.WarnText) });
                    if (!string.IsNullOrEmpty(d.Rimedio)) r.Children.Add(new TextBlock { Text = "come correggere: " + d.Rimedio, TextWrapping = TextWrapping.Wrap, FontSize = 13, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(18, 0, 0, 0) });
                    righe.Children.Add(new Border { Background = Pal.B(err ? Pal.WarnBg : Pal.Panel), BorderBrush = Pal.B(err ? Pal.Red : Pal.Orange), BorderThickness = new Thickness(3, 0, 0, 0), Padding = new Thickness(8, 6), Child = r });
                }
            if (l.Count == 0) righe.Children.Add(new TextBlock { Text = "✓ nessun errore e nessun avviso", FontSize = 14, Foreground = Pal.B(Pal.Green) });
            righe.Children.Add(new TextBlock { Text = "Elenco completo dei controlli e spiegazioni: manuale, capitolo 11 (Controllo dei dati).", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 6, 0, 0) });
            dp.Children.Add(new ScrollViewer { Content = righe });
            w.Content = dp;
            await w.ShowDialog(this);
        }

        /// <summary>Controlli dei dati dopo un calcolo (modello e risultati), aggiunti agli avvisi del calcolo e scritti nel registro.</summary>
        static void ControlliDopoCalcolo(NecModel m, RunResult r)
        {
            try
            {
                foreach (ControlloDato d in ControlliDati.DopoCalcolo(m, r))
                {
                    string s = d.ToString();
                    if (!r.Warnings.Contains(s)) r.Warnings.Add(s);
                    Registro.Scrivi("CONTROLLI", s);
                }
            }
            catch { }
        }
    }
}
