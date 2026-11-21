// YA-NEC 1.5.2 - bozzetto «Struttura 3D · terreno a scomparsa e barra di stato» (approvato da IC8TEM il 19/11/2026 19:41):
// pulsante «Mostra terreno» a sinistra della barra di stato con la legenda dei terreni in una tendina; ogni errore della barra
// di stato e' in grassetto e apre anche una finestra con OK (una sola alla volta: un errore nuovo aggiorna quella aperta).
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace YaNec
{
    public partial class MainWindow
    {
        Window finErrore;
        Popup popTerreno;
        TextBlock finErroreTesto;

        Control PulsanteTerreno()
        {
            Button b = new Button { Content = "Mostra terreno ▴", Padding = new Thickness(12, 2), Margin = new Thickness(0, 0, 10, 0), CornerRadius = new CornerRadius(12),
                FontSize = 12, Background = new SolidColorBrush(Color.Parse("#2F4A66")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.Parse("#4A6787")),
                BorderThickness = new Thickness(1), VerticalAlignment = VerticalAlignment.Center };
            ToolTip.SetTip(b, "Come viene disegnato ogni terreno (il colore segue il tipo scelto nel menu Terreno):");
            // Popup e non Flyout: il FlyoutPresenter limita la larghezza (la legenda veniva tagliata); niente chiusura al clic fuori
            // (il clic sul pulsante la riaprirebbe): si chiude con lo stesso pulsante o con Esc
            Popup pp = new Popup
            {
                Child = new Border { Padding = new Thickness(8), Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
                    Child = groundLegend ?? new GroundLegend { Width = 760 } },
                PlacementTarget = b, Placement = PlacementMode.TopEdgeAlignedLeft, IsLightDismissEnabled = false, VerticalOffset = -4
            };
            pp.Opened += (s, e) => { b.Content = "Nascondi terreno ▾"; b.Background = new SolidColorBrush(Color.Parse("#E08A00")); b.BorderBrush = b.Background; };
            pp.Closed += (s, e) => { b.Content = "Mostra terreno ▴"; b.Background = new SolidColorBrush(Color.Parse("#2F4A66")); b.BorderBrush = new SolidColorBrush(Color.Parse("#4A6787")); };
            b.Click += (s, e) => pp.IsOpen = !pp.IsOpen;
            AddHandler(KeyDownEvent, (s, e) => { if (e.Key == Key.Escape && pp.IsOpen) { pp.IsOpen = false; e.Handled = true; } }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            popTerreno = pp;
            StackPanel sb = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            sb.Children.Add(b); sb.Children.Add(pp);
            DockPanel.SetDock(sb, Dock.Left);
            return sb;
        }

        void FinestraErrore(string testo)
        {
            if (ProveAutomatiche) return;   // le prove automatiche leggono la barra di stato: niente finestre
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (finErrore != null) { finErroreTesto.Text = testo; finErrore.Activate(); return; }
                    finErroreTesto = new TextBlock { Text = testo, TextWrapping = TextWrapping.Wrap, MaxWidth = 460, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 14) };
                    Button ok = new Button { Content = "OK", MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right, HorizontalContentAlignment = HorizontalAlignment.Center,
                        Background = Pal.B(Pal.Accent), Foreground = Pal.B(Pal.AccentFore), IsDefault = true };
                    StackPanel sp = new StackPanel { Margin = new Thickness(18, 14) };
                    sp.Children.Add(new TextBlock { Text = "Errore", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#D23C3C")), Margin = new Thickness(0, 0, 0, 8) });
                    sp.Children.Add(finErroreTesto);
                    sp.Children.Add(ok);
                    Border cornice = new Border { Child = sp, BorderBrush = new SolidColorBrush(Color.Parse("#D23C3C")), BorderThickness = new Thickness(0, 4, 0, 0) };   // 1.5.2: striscia rossa in alto, si vede anche senza la cornice della finestra
                    Window w = new Window { Title = "YA-NEC", Content = cornice, SizeToContent = SizeToContent.WidthAndHeight, CanResize = false, MinWidth = 320,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel), ShowInTaskbar = false };
                    ok.Click += (s, e) => w.Close();
                    w.KeyDown += (s, e) => { if (e.Key == Key.Escape || e.Key == Key.Enter) w.Close(); };
                    w.Closed += (s, e) => { finErrore = null; finErroreTesto = null; };
                    finErrore = w;
                    w.Opened += (s, e) => { w.Activate(); ok.Focus(); };   // Invio o Esc chiudono subito
                    w.Show(this);
                    w.Activate();   // 1.5.3: la finestra prende il fuoco anche se si stava scrivendo in una tabella (Invio la chiude)
                }
                catch { finErrore = null; }
            });
        }
    }
}
