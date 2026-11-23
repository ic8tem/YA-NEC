// YA-NEC - finestra "About": programma, versione, autore, contatti, licenza e crediti in breve.
// Regola dei pulsanti dei programmi di IC8TEM: con due pulsanti uno a sinistra e uno a destra.
using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace YaNec
{
    public class AboutWindow : Window
    {
        public AboutWindow()
        {
            Title = "About - " + AppPaths.AppName;
            Width = 600;
            SizeToContent = SizeToContent.Height;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Pal.B(Pal.Back);
            Foreground = Pal.B(Pal.Fore);
            FontSize = 13;

            StackPanel root = new StackPanel();

            // testata scura come la barra del titolo della finestra principale
            Border head = new Border { Background = Pal.B(Pal.Track), Padding = new Thickness(20, 16) };
            StackPanel hp = new StackPanel { Spacing = 2 };
            StackPanel hl = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            hl.Children.Add(new TextBlock { Text = "YA-NEC", FontSize = 28, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.BarFore) });
            hl.Children.Add(new TextBlock { Text = "versione " + AppPaths.Version, FontSize = 14, Foreground = Pal.B(Pal.BarMuted), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 5) });
            hp.Children.Add(hl);
            hp.Children.Add(new TextBlock { Text = "Simulatore d'antenne a fili NEC2 per Windows, Linux e Mac", FontSize = 13.5, Foreground = Pal.B(Pal.BarFore) });
            hp.Children.Add(new TextBlock { Text = AppPaths.Author, FontSize = 13.5, FontStyle = FontStyle.Italic, Foreground = Pal.B(Pal.BarMuted), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 6, 0, 0) });
            head.Child = hp;
            root.Children.Add(head);

            StackPanel body = new StackPanel { Margin = new Thickness(20, 14), Spacing = 10 };
            // 1.0 (IC8TEM, 15/11/2026): al posto della storia del programma, la frase scelta dall'autore
            body.Children.Add(new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10),
                Child = new SelectableTextBlock { Text = Frase, FontSize = 14.5, FontStyle = FontStyle.Italic, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center }
            });
            body.Children.Add(Section("AUTORE",
                "Costantino Cerrotta IC8TEM - Capri\n" +
                "ccerrotta@gmail.com  -  ic8tem@gmail.com\n" +
                "www.qrz.com/db/ic8tem"));
            body.Children.Add(Section("MOTORE DI CALCOLO",
                "NEC2 di G. J. Burke e A. J. Poggio (LLNL), tradotto in C da Neoklis Kyriazis 5B4AZ (nec2c) " +
                "e in C# per YA-NEC. Interfaccia di riferimento: xnec2c di 5B4AZ e Eric KJ7LNW.\n" +
                "I crediti completi sono nella linguetta Crediti."));
            body.Children.Add(Section("LICENZA",
                "GPL-3.0-or-later (GPL 3 o successiva), come nec2c e xnec2c: il codice sorgente accompagna il programma (file COPYING).\n" +
                "Il programma e' fornito cosi' com'e', senza garanzie: verificare sempre i risultati dei modelli."));

            // due pulsanti: uno a sinistra, uno a destra
            Grid buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Margin = new Thickness(0, 6, 0, 2) };
            Button kofi = MakeButton("Sostieni su Ko-fi", false);
            kofi.Click += (s, e) => OpenUrl("https://ko-fi.com/ic8tem");
            Button close = MakeButton("Chiudi", true);
            close.Click += (s, e) => Close();
            Grid.SetColumn(kofi, 0);
            Grid.SetColumn(close, 2);
            buttons.Children.Add(kofi);
            buttons.Children.Add(close);
            body.Children.Add(buttons);

            root.Children.Add(body);
            Content = root;
        }

        public const string Frase = "La storia di questo programma la farete voi: con i vostri progetti, con le vostre idee, con la vostra genialità, con le vostre scoperte e con le vostre certezze.";

        static Control Section(string title, string text)
        {
            StackPanel p = new StackPanel { Spacing = 3 };
            p.Children.Add(new TextBlock { Text = title, FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Muted) });
            p.Children.Add(new SelectableTextBlock { Text = text, FontSize = 13, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap });
            return new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8), Child = p
            };
        }

        static Button MakeButton(string text, bool accent)
        {
            Button b = new Button { Content = text, Padding = new Thickness(16, 5) };
            b.Classes.Add("pill");
            b.Background = Pal.B(accent ? Pal.Accent : Pal.Pill);
            b.Foreground = Pal.B(accent ? Pal.AccentFore : Pal.PillFore);
            b.BorderBrush = Pal.B(accent ? Pal.Accent : Pal.PillBorder);
            b.BorderThickness = new Thickness(1);
            if (accent) b.FontWeight = FontWeight.SemiBold;
            return b;
        }

        public static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }
    }
}
