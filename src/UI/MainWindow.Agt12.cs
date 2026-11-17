// YA-NEC 1.2.0 - Avvisi: test del guadagno medio (AGT) fatto dal programma (bozzetto HF 11 D approvato da IC8TEM il 17/11/2026).
using System;
using System.Collections.Generic;
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
        Grid agtTab;
        TextBlock agtStato;
        CheckBox agtCorreggi;
        double agtUltimoDb = double.NaN;     // AGT in spazio libero senza perdite (dB) dell'ultimo test, per il guadagno corretto
        NecModel agtModello;
        CancellationTokenSource agtCts;

        Control BuildAgtPanel()
        {
            agtTab = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
            agtStato = Nota("");
            agtCorreggi = new CheckBox { Content = "Mostra il guadagno corretto con l'AGT (scheda Guadagno massimo)" };
            agtCorreggi.IsCheckedChanged += (s, e) => ShowCards();
            Button b = Btn("Esegui il test", true);
            b.Click += (s, e) => EseguiAgt();
            StackPanel sp = new StackPanel { Spacing = 4 };
            sp.Children.Add(agtTab);
            sp.Children.Add(Nota("Il programma lo calcola da sé togliendo perdite e terreno reale (metodo Burke/Lewallen); oltre ±0,2 dB avvisa e propone la verifica della segmentazione. " +
                "Il guadagno può essere mostrato corretto con l'AGT (già previsto nell'ottimizzatore)."));
            WrapPanel w = new WrapPanel(); w.Children.Add(b); w.Children.Add(agtCorreggi);
            sp.Children.Add(w); sp.Children.Add(agtStato);
            MostraAgt(null);
            return Pannello("Test del guadagno medio (AGT)", sp);
        }

        void MostraAgt(List<AgtTest.Riga> l)
        {
            if (agtTab == null) return;
            agtTab.Children.Clear(); agtTab.RowDefinitions.Clear();
            int r = 0;
            Action<string, string, string, Color?, bool> riga = (a, v, esito, col, h) =>
            {
                agtTab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                string[] c = { a, v, esito };
                for (int j = 0; j < 3; j++)
                {
                    TextBlock t = new TextBlock { Text = c[j], FontWeight = h || j == 2 ? FontWeight.SemiBold : FontWeight.Normal };
                    if (j == 2 && col.HasValue) t.Foreground = Pal.B(col.Value);
                    Border bd = new Border { Child = t, Padding = new Thickness(8, 4), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 1, 1) };
                    Grid.SetColumn(bd, j); Grid.SetRow(bd, r); agtTab.Children.Add(bd);
                }
                r++;
            };
            riga("Controllo prima/dopo il calcolo", "Valore", "Esito", null, true);
            if (l == null) { riga("(premi \"Esegui il test\"; si fa da solo all'apertura degli Avvisi con i modelli fino a 1500 segmenti)", "-", "-", null, false); return; }
            foreach (AgtTest.Riga x in l)
            {
                if (x.Errore != null) { riga(x.Nome, "errore", x.Errore, Pal.Red, false); continue; }
                riga(x.Nome, FN(x.Valore, 3) + " (" + (x.Db >= 0 ? "+" : "") + FN(x.Db, 2) + " dB)", x.Buono ? "buono" : "sospetto", x.Buono ? Pal.Green : Pal.Red, false);
            }
        }

        async void EseguiAgt()
        {
            if (model == null || agtTab == null) return;
            agtCts?.Cancel(); agtCts = new CancellationTokenSource();
            CancellationToken ct = agtCts.Token;
            NecModel m = model;
            agtStato.Text = "calcolo in corso…";
            try
            {
                List<AgtTest.Riga> l = await Task.Run(() => AgtTest.Esegui(m, ct), ct);
                if (ct.IsCancellationRequested || m != model) return;
                agtModello = m;
                agtUltimoDb = l.Count > 0 && l[0].Errore == null ? l[0].Db : double.NaN;
                MostraAgt(l);
                bool male = l.Exists(x => x.Errore == null && !x.Buono);
                agtStato.Text = male ? "⚠ Uno o piu' controlli oltre ±0,2 dB: verifica la segmentazione (Struttura 3D > Segmentazione... e Verifica segmentazione)." : "Tutti i controlli entro ±0,2 dB.";
                agtStato.Foreground = Pal.B(male ? Pal.Red : Pal.Green);
                Registro.Scrivi("CALCOLO", "test AGT: " + string.Join("; ", l.ConvertAll(x => x.Nome + " " + FN(x.Valore, 3))));
                ShowCards();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { agtStato.Text = "Test AGT non riuscito: " + ex.Message; }
        }

        /// <summary>Guadagno da mostrare: corretto con l'AGT se la casella e' accesa e il test e' del modello aperto.</summary>
        double GuadagnoMostrato(double g)
        {
            if (agtCorreggi != null && agtCorreggi.IsChecked == true && agtModello == model && !double.IsNaN(agtUltimoDb)) return g - agtUltimoDb;
            return g;
        }
    }
}
