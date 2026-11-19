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
        TextBlock finErroreTesto;

        Control PulsanteTerreno()
        {
            Button b = new Button { Content = "Mostra terreno ▴", Padding = new Thickness(12, 2), Margin = new Thickness(0, 0, 10, 0), CornerRadius = new CornerRadius(12),
                FontSize = 12, Background = new SolidColorBrush(Color.Parse("#2F4A66")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.Parse("#4A6787")),
                BorderThickness = new Thickness(1), VerticalAlignment = VerticalAlignment.Center };
            ToolTip.SetTip(b, "Come viene disegnato ogni terreno (il colore segue il tipo scelto nel menu Terreno):");
            Flyout fl = new Flyout
            {
                Content = new Border { Padding = new Thickness(4), Child = groundLegend ?? new GroundLegend { Width = 760 } },
                Placement = PlacementMode.TopEdgeAlignedLeft
            };
            fl.Opened += (s, e) => { b.Content = "Nascondi terreno ▾"; b.Background = new SolidColorBrush(Color.Parse("#E08A00")); b.BorderBrush = b.Background; };
            fl.Closed += (s, e) => { b.Content = "Mostra terreno ▴"; b.Background = new SolidColorBrush(Color.Parse("#2F4A66")); b.BorderBrush = new SolidColorBrush(Color.Parse("#4A6787")); };
            b.Click += (s, e) => { if (fl.IsOpen) fl.Hide(); else fl.ShowAt(b); };
            DockPanel.SetDock(b, Dock.Left);
            return b;
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
                    Window w = new Window { Title = "YA-NEC", Content = sp, SizeToContent = SizeToContent.WidthAndHeight, CanResize = false, MinWidth = 320,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel), ShowInTaskbar = false };
                    ok.Click += (s, e) => w.Close();
                    w.KeyDown += (s, e) => { if (e.Key == Key.Escape || e.Key == Key.Enter) w.Close(); };
                    w.Closed += (s, e) => { finErrore = null; finErroreTesto = null; };
                    finErrore = w;
                    w.Show(this);
                }
                catch { finErrore = null; }
            });
        }
    }
}
