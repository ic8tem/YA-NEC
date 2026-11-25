// YA-NEC 1.7.0 - linguetta «Consigli» (bozzetto «mining riviste» U, approvato da IC8TEM il 25/11/2026 20:41): schede brevi con
// titolo, testo, fonte e pulsanti «apri» verso il calcolatore che mostra i numeri. Le schede sono in Engine/Consigli.cs e compaiono
// anche in fondo ai calcolatori che riguardano.
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Control BuildConsigliPage()
        {
            StackPanel sp = new StackPanel { Spacing = 10, Margin = new Thickness(4) };
            sp.Children.Add(new TextBlock { Text = Lang.T("Consigli pratici"), FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            sp.Children.Add(Nota(Lang.T("Schede brevi riassunte dalle fonti citate (Citazioni nei Documenti tecnici). Il pulsante apre il calcolatore che mostra i numeri; le stesse schede compaiono in fondo a quei calcolatori.")));
            WrapPanel wp = new WrapPanel();
            foreach (Consiglio c in Consigli.Tutti)
            {
                Consiglio cc = c;
                StackPanel corpo = new StackPanel { Spacing = 6 };
                corpo.Children.Add(new TextBlock { Text = Lang.T(cc.Testo), TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), FontSize = 13.5 });
                corpo.Children.Add(new TextBlock { Text = Lang.T("Fonte") + ": " + cc.Fonte, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), FontSize = 12 });
                WrapPanel bb = new WrapPanel();
                for (int i = 0; i < cc.Voci.Length; i++)
                {
                    string voce = cc.Voci[i], nome = i < cc.VociNomi.Length ? cc.VociNomi[i] : voce;
                    Button b = Btn(Lang.T("apri") + ": " + Lang.T(nome), i == 0);
                    b.Margin = new Thickness(0, 0, 8, 4);
                    b.Click += (s, e) => ApriConsiglio(voce);
                    bb.Children.Add(b);
                }
                corpo.Children.Add(bb);
                Border card = new Border
                {
                    Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Violet), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 10, 10), Width = 460,
                    Child = new StackPanel { Spacing = 6, Children = { new TextBlock { Text = Lang.T(cc.Titolo), FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Violet), TextWrapping = TextWrapping.Wrap }, corpo } }
                };
                wp.Children.Add(card);
            }
            sp.Children.Add(wp);
            return new ScrollViewer { Content = sp, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        void ApriConsiglio(string voce)
        {
            Registro.Scrivi("AZIONE", "consiglio: apri " + voce);
            if (voce == "*frequenza") { ApriLinguetta("Parametri"); return; }
            if (!ApriLinguetta("Calcolatori")) return;
            ApriRimando(new CalcRimando { Voce = voce, Testo = "" }, null);
        }
    }
}
