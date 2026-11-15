// YA-NEC - linguetta "Registro" (dalla 0.4.2, richiesta di IC8TEM del 13/11/2026): dove sta il registro della sessione,
// le ultime righe, il punteggio delle funzioni (quanto pesano sulla lentezza) e il pulsante per aprire la cartella.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace YaNec
{
    public partial class MainWindow
    {
        TextBlock regPercorso;
        SelectableTextBlock regPunteggio, regRighe;

        Control BuildRegistroPage()
        {
            DockPanel d = new DockPanel();
            StackPanel top = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 6) };
            regPercorso = new TextBlock { FontSize = 13, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            top.Children.Add(regPercorso);
            WrapPanel bar = new WrapPanel();
            Button bApri = Btn("Apri la cartella dei registri", true), bAgg = Btn("Aggiorna", false), bCopia = Btn("Copia il punteggio", false);
            bApri.Click += (s, e) => ApriCartellaRegistri();
            bAgg.Click += (s, e) => AggiornaRegistro();
            bCopia.Click += async (s, e) =>
            {
                try { if (Clipboard != null) await Clipboard.SetTextAsync(Prestazioni.Punteggio()); Status("Punteggio delle funzioni copiato."); } catch { }
            };
            foreach (Button b in new[] { bApri, bAgg, bCopia }) { b.Margin = new Thickness(0, 0, 6, 0); bar.Children.Add(b); }
            top.Children.Add(bar);
            top.Children.Add(Nota("Il registro e' sempre attivo: un file di testo per sessione nella cartella Registri accanto al programma, con ogni azione, i tempi di risposta, " +
                "la durata delle funzioni, CPU, GPU, RAM e disco. Per segnalare una lentezza basta mandare il file della sessione. " +
                "Punteggio: quota del tempo proprio di ogni funzione sul tempo misurato (0-100); risposte oltre 0,1 s si percepiscono, oltre 1 s sono lente (Nielsen)."));
            DockPanel.SetDock(top, Dock.Top);
            d.Children.Add(top);

            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*") };
            regPunteggio = new SelectableTextBlock { FontFamily = Pal.Mono, FontSize = 12, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(8) };
            regRighe = new SelectableTextBlock { FontFamily = Pal.Mono, FontSize = 12, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(8) };
            // 0.4.20 (schermata di IC8TEM del 14/11/2026 17:55: "il registro non ha le barre di scorrimento"): Pannello mette il corpo in
            // uno StackPanel, che da' altezza infinita e quindi niente scorrimento verticale. Qui intestazione in alto in un DockPanel e
            // lo ScrollViewer riempie il resto, con le barre sempre visibili (orizzontale e verticale).
            regScorriPunteggio = Scorrevole(regPunteggio);
            regScorriRighe = Scorrevole(regRighe);
            Border a = PannelloScorrevole("Punteggio delle funzioni (questa sessione)", regScorriPunteggio);
            Border b2 = PannelloScorrevole("Ultime righe del registro", regScorriRighe);
            Grid.SetColumn(a, 0); Grid.SetColumn(b2, 2);
            g.Children.Add(a); g.Children.Add(b2);
            d.Children.Add(g);
            return d;
        }

        ScrollViewer regScorriPunteggio, regScorriRighe;

        static ScrollViewer Scorrevole(Control c)
        {
            return new ScrollViewer
            {
                Content = c,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
                AllowAutoHide = false
            };
        }

        Border PannelloScorrevole(string titolo, ScrollViewer corpo)
        {
            DockPanel dp = new DockPanel();
            TextBlock head = new TextBlock { Text = titolo, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6) };
            DockPanel.SetDock(head, Dock.Top);
            dp.Children.Add(head);
            dp.Children.Add(corpo);
            return new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 0, 10), Child = dp
            };
        }

        void AggiornaRegistro()
        {
            if (regPercorso == null) return;
            string f = Registro.File_;
            regPercorso.Text = f == null ? "Registro non attivo (cartella non scrivibile)." : "Registro di questa sessione: " + f;
            regPunteggio.Text = Prestazioni.Punteggio() + Environment.NewLine + "Risorse adesso: " + Prestazioni.Risorse();
            List<string> ult = new List<string>();
            try
            {
                if (f != null)
                    using (FileStream fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (StreamReader r = new StreamReader(fs, Encoding.UTF8))
                    {
                        string l;
                        while ((l = r.ReadLine()) != null) { ult.Add(l); if (ult.Count > 300) ult.RemoveAt(0); }
                    }
            }
            catch (Exception ex) { ult.Add("Registro non leggibile: " + ex.Message); }
            regRighe.Text = string.Join(Environment.NewLine, ult);
            // le righe piu' recenti sono in fondo: si scorre alla fine
            if (regScorriRighe != null) Avalonia.Threading.Dispatcher.UIThread.Post(() => { try { regScorriRighe.ScrollToEnd(); } catch { } }, Avalonia.Threading.DispatcherPriority.Loaded);
        }

        void ApriCartellaRegistri()
        {
            string d = Registro.Cartella;
            if (d == null) { Status("La cartella dei registri non esiste.", true); return; }
            try
            {
                if (OperatingSystem.IsWindows()) Process.Start(new ProcessStartInfo("explorer.exe", "\"" + d + "\"") { UseShellExecute = false });
                else if (OperatingSystem.IsMacOS()) Process.Start("open", "\"" + d + "\"");
                else Process.Start("xdg-open", "\"" + d + "\"");
                Registro.Scrivi("AZIONE", "aperta la cartella dei registri");
            }
            catch (Exception ex) { Status("Cartella dei registri: " + d + " (non aperta: " + ex.Message + ")", true); }
        }
    }
}
