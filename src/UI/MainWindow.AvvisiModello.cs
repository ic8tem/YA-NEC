// YA-NEC 0.4.20 - fumetti gialli di avviso sui pulsanti che creano i modelli NEC dei calcolatori.
// Richiesta di IC8TEM del 14/11/2026 16:01: "chiaro che per i fili sul terreno ci sono le approssimazioni, e non dimenticare di inserire
// fumetti con avviso in giallo, oltre che relazionare nei manuali". Il fumetto mostra gli avvisi del calcolo (TerrenoVicino: fili vicini al
// terreno, segmenti fuori misura, giunzioni affollate, contatti fuori giunzione) del modello che il pulsante crea, su fondo giallo.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        /// <summary>Avvisi del calcolo per il mazzo NEC di un calcolatore (vuoto se non ce ne sono o se il mazzo non si legge).</summary>
        internal static List<string> AvvisiDelMazzo(string nec)
        {
            List<string> av = new List<string>();
            if (string.IsNullOrEmpty(nec)) return av;
            try
            {
                NecModel m = NecModel.Parse(nec);
                SegmentGeometry g = m.BuildGeometry(new List<string>());
                if (g.N <= 4000) av.AddRange(TerrenoVicino.Controlla(g, m.Ground, m.FreqMHz));
            }
            catch { }
            return av;
        }

        /// <summary>Fumetto normale, oppure giallo con gli avvisi del modello se ce ne sono.</summary>
        internal static void FumettoConAvvisi(Control c, string normale, IList<string> avvisi, string approssimazione = null)
        {
            if (c == null) return;
            if ((avvisi == null || avvisi.Count == 0) && string.IsNullOrEmpty(approssimazione)) { ToolTip.SetTip(c, normale); return; }
            StackPanel sp = new StackPanel { Spacing = 4, MaxWidth = 520 };
            if (!string.IsNullOrEmpty(normale)) sp.Children.Add(new TextBlock { Text = normale, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 40)) });
            if (!string.IsNullOrEmpty(approssimazione))
                sp.Children.Add(new TextBlock { Text = "⚠ APPROSSIMAZIONE: " + approssimazione, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(90, 60, 0)) });
            if (avvisi != null)
                foreach (string a in avvisi)
                    sp.Children.Add(new TextBlock { Text = "⚠ " + a, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(90, 60, 0)) });
            sp.Children.Add(new TextBlock { Text = "Spiegazione nel manuale, capitolo 11 (Terreni: limiti del NEC-2 e controlli geometrici).", TextWrapping = TextWrapping.Wrap, FontSize = 11.5, Foreground = new SolidColorBrush(Color.FromRgb(90, 90, 90)) });
            ToolTip.SetTip(c, new Border { Background = new SolidColorBrush(Color.FromRgb(255, 236, 153)), BorderBrush = new SolidColorBrush(Color.FromRgb(214, 168, 0)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 6), Child = sp });
        }
    }
}
