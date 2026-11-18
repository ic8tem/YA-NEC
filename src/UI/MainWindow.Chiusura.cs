// YA-NEC 1.4 - finestra di chiusura come in LIVETROPOVHF (richiesta di IC8TEM del 18/11/2026 14:47, "inseriamo anche questo"):
// icona, domanda, ringraziamento in corsivo, barra grigia con Si' e No. Invio = Si', Esc = No.
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace YaNec
{
    public partial class MainWindow
    {
        async Task<bool> ChiediChiusura()
        {
            Window w = new Window
            {
                Title = "Chiusura del programma", Width = 480, SizeToContent = SizeToContent.Height, CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel), ShowInTaskbar = false, Icon = Icon
            };
            bool si = false;
            DockPanel top = new DockPanel { Margin = new Thickness(20, 20, 20, 16) };
            try
            {
                Bitmap img = new Bitmap(Avalonia.Platform.AssetLoader.Open(new System.Uri("avares://YA-NEC/assets/ya-nec-256.png")));
                Border pic = new Border { Width = 64, Height = 64, CornerRadius = new CornerRadius(10), ClipToBounds = true, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Top, Child = new Image { Source = img, Stretch = Stretch.UniformToFill } };
                DockPanel.SetDock(pic, Dock.Left);
                top.Children.Add(pic);
            }
            catch { }
            TextBlock q = new TextBlock { Text = "Vuoi chiudere YA-NEC?", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            TextBlock grazie = new TextBlock { Text = "Grazie per avermi usato: spero di esserti stato utile.", FontSize = 14, FontStyle = FontStyle.Italic, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            top.Children.Add(new StackPanel { Children = { q, grazie }, VerticalAlignment = VerticalAlignment.Center });
            Button bSi = Btn("Sì", true), bNo = Btn("No", false);
            bSi.MinWidth = 90; bNo.MinWidth = 90;
            bSi.Click += (s, e) => { si = true; w.Close(); };
            bNo.Click += (s, e) => w.Close();
            Border barra = new Border
            {
                Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(16, 10),
                Child = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right, Children = { bSi, bNo } }
            };
            w.Content = new StackPanel { Children = { top, barra } };
            w.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Escape) w.Close(); if (e.Key == Avalonia.Input.Key.Enter) { si = true; w.Close(); } };
            await w.ShowDialog(this);
            Registro.Scrivi("AZIONE", "chiusura del programma: " + (si ? "si'" : "no"));
            return si;
        }
    }
}
