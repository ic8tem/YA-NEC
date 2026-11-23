// YA-NEC 1.6.0 - Ottimizzatore > «Adattamento diretto a Z0» (bozzetto «spunti dalla ricerca su GitHub», proposta 2, approvato da IC8TEM
// il 23/11/2026 14:27). Riquadro in cima all'Ottimizzatore, visibile solo con una Yagi riconosciuta; calcolo in Engine/AdattaZ0.cs.
// «Adatta» calcola e applica il modello adattato (le misure diventano numeri, come Ridimensiona e ricentra); «Annulla» rimette il
// modello di prima.
using System;
using System.Collections.Generic;
using System.Globalization;
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
        Border adattaBox;
        TextBlock adattaPerche;
        NecModel adattaPrima;
        CancellationTokenSource adattaCts;

        Control PannelloAdattaZ0()
        {
            StackPanel sp = new StackPanel { Spacing = 4 };
            WrapPanel tit = new WrapPanel();
            tit.Children.Add(new TextBlock { Text = "Adattamento diretto a Z0", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(tit);
            sp.Children.Add(NotaT("Senza adattatore: si cambiano la lunghezza del radiatore e lunghezza e posizione del primo direttore finche' l'impedenza arriva a Z0. ",
                "Il resto della Yagi non cambia. Idea di AntAn (sozden, Apache-2.0)."));
            TextBox tZ0 = Tb(Fmt(model != null ? model.Z0 : 50, 1), 70);
            ComboBox cMuove = new ComboBox { ItemsSource = new[] { "radiatore (lunghezza) + D1 (lunghezza e posizione)", "solo il radiatore" }, SelectedIndex = 0, MinWidth = 300 };
            ComboBox cG = new ComboBox { ItemsSource = new[] { "non deve scendere piu' di 0,2 dB", "non deve scendere piu' di 0,5 dB", "libero" }, SelectedIndex = 0, MinWidth = 240 };
            ToolTip.SetTip(cG, "Calo di guadagno ammesso nella direzione del lobo: oltre, la prova e' penalizzata.");
            Func<string, Control, string, Control> riga = (et, c, um) =>
            {
                WrapPanel w = new WrapPanel { Margin = new Thickness(0, 1) };
                w.Children.Add(new TextBlock { Text = et, Width = 130, VerticalAlignment = VerticalAlignment.Center });
                w.Children.Add(c);
                if (!string.IsNullOrEmpty(um)) w.Children.Add(new TextBlock { Text = um, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) });
                return w;
            };
            sp.Children.Add(riga("Z0 della linea", tZ0, "Ω"));
            sp.Children.Add(riga("Cosa si muove", cMuove, ""));
            sp.Children.Add(riga("Guadagno", cG, ""));
            WrapPanel bb = new WrapPanel();
            Button bAd = Btn("Adatta a " + Fmt(model != null ? model.Z0 : 50, 0) + " Ω", true), bAnn = Btn("Annulla", false);
            bAnn.IsEnabled = false;
            tZ0.TextChanged += (s, e) => bAd.Content = Lang.F("Adatta a {0} Ω", Fmt(ParseNum(tZ0.Text, 50), 0));
            bb.Children.Add(bAd); bb.Children.Add(bAnn);
            sp.Children.Add(bb);
            ProgressBar pb = new ProgressBar { IsIndeterminate = true, Height = 4, IsVisible = false };
            sp.Children.Add(pb);
            StackPanel ris = new StackPanel { Spacing = 3 };
            sp.Children.Add(ris);
            bAd.Click += async (s, e) =>
            {
                if (model == null || ottInCorso) return;
                double z0 = ParseNum(tZ0.Text, model.Z0);
                if (!(z0 > 0)) { Status("Adattamento diretto: Z0 non valida.", true); return; }
                double f = ottFreq.Count > 0 ? ottFreq[Math.Max(0, Math.Min(ottFreq.Count - 1, ottFreqPrinc))] : model.FreqMHz;
                bool solo = cMuove.SelectedIndex == 1;
                double calo = cG.SelectedIndex == 0 ? 0.2 : cG.SelectedIndex == 1 ? 0.5 : double.PositiveInfinity;
                NecModel m0 = model;
                adattaCts = new CancellationTokenSource();
                CancellationToken tok = adattaCts.Token;
                bAd.IsEnabled = false; pb.IsVisible = true; ris.Children.Clear();
                try
                {
                    AdattaEsito r;
                    using (Prestazioni.Misura("Adattamento diretto a Z0"))
                    using (Structure3DView.PausaAnimazione())
                        r = await Task.Run(() => AdattaZ0.Calcola(m0, f, z0, solo, calo, tok));
                    if (model != m0) { Status("Adattamento diretto: il modello e' cambiato durante il calcolo, risultato scartato.", true); return; }
                    Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto") };
                    string[][] righe =
                    {
                        new[] { "", "Impedenza", "ROS", "Guadagno", "Avanti/indietro" },
                        new[] { "prima", ZTesto(r.ZPrima), Fmt(r.RosPrima, 2), Fmt(r.GPrima, 2) + " dBi", Fmt(r.FbPrima, 1) + " dB" },
                        new[] { "dopo", ZTesto(r.ZDopo), Fmt(r.RosDopo, 2), Fmt(r.GDopo, 2) + " dBi", Fmt(r.FbDopo, 1) + " dB" },
                    };
                    for (int i = 0; i < righe.Length; i++)
                    {
                        g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                        for (int j = 0; j < 5; j++)
                        {
                            TextBlock t = new TextBlock { Text = Lang.T(righe[i][j]), Margin = new Thickness(6, 2), FontFamily = i > 0 && j > 0 ? Pal.Mono : FontFamily.Default,
                                Foreground = Pal.B(i == 0 ? Pal.Muted : (i == 2 && j == 2 && r.RosDopo <= 1.2 ? Pal.Green : Pal.Fore)), FontWeight = i == 2 ? FontWeight.SemiBold : FontWeight.Normal };
                            Grid.SetRow(t, i); Grid.SetColumn(t, j); g.Children.Add(t);
                        }
                    }
                    ris.Children.Add(new ScrollViewer { Content = g, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto });
                    string var = solo ? Lang.F("Variazioni: radiatore {0} mm.", Segno(r.DRadMm)) :
                        Lang.F("Variazioni: radiatore {0} mm, D1 {1} mm e {2} mm {3}.", Segno(r.DRadMm), Segno(r.DD1Mm), Segno(Math.Abs(r.PD1Mm)).TrimStart('+'), Lang.T(r.PD1Mm < 0 ? "verso il radiatore" : "lontano dal radiatore"));
                    ris.Children.Add(Nota(var + " " + Lang.F("{0} calcoli.", r.Prove)));
                    foreach (string n in r.Note) ris.Children.Add(AvvisoGiallo(n));
                    adattaPrima = m0;
                    ottInAdatta = true;
                    SetModel(r.Modello, null);
                    ottInAdatta = false;
                    bAnn.IsEnabled = true;
                    Registro.Scrivi("AZIONE", string.Format(CultureInfo.InvariantCulture, "adattamento diretto a {0} ohm: ROS {1:0.00} -> {2:0.00}, radiatore {3:+0.0;-0.0} mm, D1 {4:+0.0;-0.0} mm, posizione {5:+0.0;-0.0} mm",
                        z0, r.RosPrima, r.RosDopo, r.DRadMm, r.DD1Mm, r.PD1Mm));
                    Status(Lang.F("Adattamento diretto applicato: ROS {0} → {1}. Il file su disco non è cambiato: salvalo se vuoi tenerlo.", Fmt(r.RosPrima, 2), Fmt(r.RosDopo, 2)));
                    Calculate(true);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { ris.Children.Add(AvvisoGiallo(ex.GetBaseException().Message)); Status("Adattamento diretto: " + ex.GetBaseException().Message, true); }
                finally { bAd.IsEnabled = true; pb.IsVisible = false; ottInAdatta = false; }
            };
            bAnn.Click += (s, e) =>
            {
                if (adattaPrima == null) return;
                ottInAdatta = true;
                SetModel(adattaPrima, null);
                ottInAdatta = false;
                adattaPrima = null;
                bAnn.IsEnabled = false;
                ris.Children.Clear();
                Registro.Scrivi("AZIONE", "adattamento diretto annullato");
                Status("Adattamento diretto annullato: il modello e' quello di prima.");
                Calculate(true);
            };
            adattaPerche = Nota("");
            adattaPerche.IsVisible = false;
            sp.Children.Add(adattaPerche);
            adattaBox = new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Accent), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 0, 10), Child = sp
            };
            AdattaZ0Visibile();
            return adattaBox;
        }

        bool ottInAdatta;

        /// <summary>Il riquadro si vede solo con una Yagi riconosciuta (radiatore diritto alimentato al centro, direttore).</summary>
        void AdattaZ0Visibile()
        {
            if (adattaBox == null) return;
            string perche;
            bool si = model != null && AdattaZ0.Riconosciuta(model, out perche);
            adattaBox.IsVisible = si || adattaPrima != null;
        }

        static string Segno(double v) { return (v >= 0 ? "+" : "−") + Math.Abs(v).ToString("0.0", CultureInfo.CurrentCulture); }
        string ZTesto(System.Numerics.Complex z) { return Fmt(z.Real, 1) + (z.Imaginary < 0 ? " − j" : " + j") + Fmt(Math.Abs(z.Imaginary), 1) + " Ω"; }
    }
}
