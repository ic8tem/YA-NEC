// YA-NEC 0.5.3 - avviso quando nel menu Terreno si sceglie un solaio in calcestruzzo (IC8TEM 15/11/2026 19:48: "se io scelgo il solaio in
// calcestruzzo, fammi un popup di avviso che mi rimandi a questa scheda, così io posso calcolare bene secondo la scheda che hai prodotto";
// bozzetto A approvato alle 19:55). Pulsanti: "Apri Antenna su tetto" (linguetta Calcolatori, voce Antenna su tetto), "Continua con il
// solaio", "Annulla" (rimette il terreno di prima). Casella "Non mostrare piu'" (si riattiva da Impostazioni > Terreno).
using System;
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
        int terrenoPrecedente;
        bool avvisoSolaioAperto;

        async Task AvvisoSolaio(int precedente)
        {
            if (avvisoSolaioAperto || ProveAutomatiche) return;   // nelle prove automatiche dell'interfaccia la finestra bloccherebbe la prova
            avvisoSolaioAperto = true;
            try
            {
                string nome = cmbGround != null && cmbGround.SelectedItem != null ? cmbGround.SelectedItem.ToString() : "solaio in calcestruzzo";
                Window w = new Window { Title = "Solaio in calcestruzzo", Width = 640, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel), CanResize = false };
                StackPanel sp = new StackPanel { Margin = new Thickness(20, 16), Spacing = 10 };
                sp.Children.Add(new TextBlock { Text = "Solaio in calcestruzzo", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
                sp.Children.Add(new TextBlock { Text = "Hai scelto \"" + nome + "\" come terreno.", TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore) });
                sp.Children.Add(new Border
                {
                    Background = Pal.B(Pal.WarnBg), BorderBrush = Pal.B(Pal.Orange), BorderThickness = new Thickness(5, 0, 0, 0), CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 8),
                    Child = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), Text = "Il solaio va bene per un'antenna VHF/UHF a pochi metri sopra il tetto. In HF l'antenna \"vede\" il terreno vero sotto il fabbricato: si calcola meglio con il terreno alla quota totale (solaio + antenna)." }
                });
                sp.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), Text = "Il calcolatore \"Antenna su tetto (solaio)\" prepara il caso giusto: quota del solaio da terra, quota dell'antenna, caso HF o VHF/UHF, e crea il modello da verificare." });
                CheckBox mai = new CheckBox { Content = "Non mostrare piu' questo avviso (si riattiva da Impostazioni > Terreno)", Foreground = Pal.B(Pal.Fore) };
                sp.Children.Add(mai);
                WrapPanel bt = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
                Button bApri = Btn("Apri \"Antenna su tetto\"", true), bCont = Btn("Continua con il solaio", false), bAnn = Btn("Annulla (torna al terreno di prima)", false);
                foreach (Button b in new[] { bApri, bCont, bAnn }) { b.Margin = new Thickness(8, 0, 0, 0); bt.Children.Add(b); }
                sp.Children.Add(bt);
                w.Content = sp;
                int scelta = 1;
                bApri.Click += (s, e) => { scelta = 0; w.Close(); };
                bCont.Click += (s, e) => { scelta = 1; w.Close(); };
                bAnn.Click += (s, e) => { scelta = 2; w.Close(); };
                Registro.Scrivi("AZIONE", "avviso solaio: " + nome);
                await w.ShowDialog(this);
                if (mai.IsChecked == true) { cfg.Set("avviso_solaio", "0"); Status("Avviso del solaio spento (si riattiva da Impostazioni > Terreno)."); }
                Registro.Scrivi("AZIONE", "avviso solaio: " + (scelta == 0 ? "apri Antenna su tetto" : scelta == 1 ? "continua" : "annulla"));
                if (scelta == 2 && cmbGround != null && precedente >= 0 && precedente < cmbGround.ItemCount) cmbGround.SelectedIndex = precedente;
                else if (scelta == 0) ApriCalcolatore("tetto");
            }
            finally { avvisoSolaioAperto = false; }
        }

        /// <summary>Porta alla linguetta Calcolatori sulla voce indicata.</summary>
        void ApriCalcolatore(string voce)
        {
            int ti = -1;
            for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == "Calcolatori") ti = i;
            if (ti >= 0) SelectTab(ti);
            if (fasCalc != null) fasCalc.Select(voce);
        }
    }
}
