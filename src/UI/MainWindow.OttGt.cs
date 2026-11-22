// YA-NEC 1.5.8 - Variabili e ottimizzazione: maschera VHF «ROS + G/T» (bozzetto approvato da IC8TEM il 22/11/2026 17:03, «ti approvo il
// bozzetto»; dati di IC8TEM 17:04: temperature come VE7BQH, preamplificatore 20 dB, 2 dB di cavo). Pannello sotto gli obiettivi: stack,
// elevazione, temperature, catena di ricezione, modo di calcolo, e dopo l'ottimizzazione la tabella prima / dopo. Il calcolo e' in
// Engine/OttGt.cs. Etichette sopra le pillole e pillole che vanno a capo (IC8TEM 17:03: «attenzione alla compilazione», bozzetto stretto).
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        static readonly string[] OttGtStackNomi = { "2 x 2", "2 affiancate", "2 sovrapposte", "singola" };
        static readonly int[] OttGtRighe = { 2, 1, 2, 1 }, OttGtColonne = { 2, 2, 1, 1 };
        int ottGtStack;
        bool ottGtVeloce = true;
        TextBox ottGtElev, ottGtNf, ottGtGpre, ottGtCavo, ottGtComb;
        TextBlock ottGtTemp;
        StackPanel ottGtPrimaDopo;
        Button ottGtMaschera;

        double OttFreqPrincipale() { return ottFreq.Count > 0 ? ottFreq[Math.Max(0, Math.Min(ottFreq.Count - 1, ottFreqPrinc))] : (model != null && model.Frequencies().Count > 0 ? model.Frequencies()[0] : 144); }

        /// <summary>Pillole a scelta singola che vanno a capo quando la colonna e' stretta.</summary>
        WrapPanel OttGtPillole(string[] voci, Func<int> sel, Action<int> cambia)
        {
            WrapPanel w = new WrapPanel { Orientation = Orientation.Horizontal };
            List<Button> bs = new List<Button>();
            for (int i = 0; i < voci.Length; i++)
            {
                int k = i;
                Button b = Btn(voci[i], false);
                StileScelta(b, k == sel());
                b.Click += (s, e) => { cambia(k); for (int j = 0; j < bs.Count; j++) StileScelta(bs[j], j == sel()); };
                bs.Add(b); w.Children.Add(b);
            }
            return w;
        }

        Control OttGtRiga(string etichetta, params Control[] c)
        {
            StackPanel sp = new StackPanel { Spacing = 1, Margin = new Thickness(0, 3, 0, 0) };
            sp.Children.Add(new TextBlock { Text = etichetta, FontSize = 12.5, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Muted) });
            WrapPanel w = new WrapPanel();
            foreach (Control x in c) w.Children.Add(x);
            sp.Children.Add(w);
            return sp;
        }

        TextBlock OttGtTesto(string s) { return new TextBlock { Text = s, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(3, 0, 8, 0) }; }

        Control OttGtPannello()
        {
            StackPanel p = new StackPanel { Spacing = 2 };
            ottGtMaschera = Btn("Maschera VHF: ROS + G/T", true);
            ottGtMaschera.Background = Pal.B(Pal.Violet);
            ToolTip.SetTip(ottGtMaschera, "Da 50 MHz in su: ROS con soglia 1,3 e G/T dello stack al massimo; guadagno, F/B e gli altri obiettivi a zero (il G/T li contiene gia').");
            ottGtMaschera.Click += (s, e) => OttGtApplicaMaschera();
            p.Children.Add(ottGtMaschera);
            p.Children.Add(OttGtRiga("Stack", OttGtPillole(OttGtStackNomi, () => ottGtStack, k => ottGtStack = k)));
            p.Children.Add(OttGtRiga("Distanze", OttGtTesto("DL6WU dalle aperture a -3 dB, ricalcolate a ogni passo")));
            ottGtElev = Tb("30", 60);
            p.Children.Add(OttGtRiga("Elevazione", ottGtElev, OttGtTesto("gradi (VE7BQH)")));
            ottGtTemp = OttGtTesto("");
            p.Children.Add(OttGtRiga("Temperature", ottGtTemp));
            ottGtNf = Tb("0,25", 55); ottGtGpre = Tb("20", 55); ottGtCavo = Tb("2", 55); ottGtComb = Tb("0,2", 55);
            p.Children.Add(OttGtRiga("Preamplificatore", OttGtTesto("NF"), ottGtNf, OttGtTesto("dB, guadagno"), ottGtGpre, OttGtTesto("dB")));
            p.Children.Add(OttGtRiga("Perdite", OttGtTesto("combinatore prima del preamp"), ottGtComb, OttGtTesto("dB, cavo dopo il preamp"), ottGtCavo, OttGtTesto("dB")));
            p.Children.Add(OttGtRiga("Calcolo", OttGtPillole(new[] { "veloce + controllo NEC", "solo NEC dello stack" }, () => ottGtVeloce ? 0 : 1, k => ottGtVeloce = k == 0)));
            p.Children.Add(Nota("Veloce: a ogni passo il diagramma della singola antenna per il fattore di schiera. Ogni 10 passi e alla fine si calcola lo stack completo con il NEC, " +
                                "accoppiamento compreso, e il G/T veloce si corregge della differenza. Solo NEC: lo stack completo a ogni passo (molto piu' lento)."));
            p.Children.Add(Nota("Come la linguetta Rumore/GT/Stack (VE7BQH, TANT, correzione KF2YN): antenna in spazio libero, cielo e terreno a temperatura uniforme. " +
                                "Il G/T e' quello di sistema: T antenna + preamplificatore + cavo diviso il guadagno del preamplificatore."));
            ottGtPrimaDopo = new StackPanel { Spacing = 2, Margin = new Thickness(0, 6, 0, 0) };
            p.Children.Add(ottGtPrimaDopo);
            OttGtAggiornaTesti();
            Border b = Pannello("Maschera VHF: ROS + G/T", p);
            b.BorderBrush = Pal.B(Pal.Violet);
            b.BorderThickness = new Thickness(1.5);
            return b;
        }

        void OttGtAggiornaTesti()
        {
            if (ottGtTemp == null || ottGtMaschera == null) return;
            double f = OttFreqPrincipale(), tc, tt;
            OttGtImpostazioni.Temperature(f, out tc, out tt);
            ottGtTemp.Text = Lang.F("uniformi come VE7BQH: cielo {0} K, terreno {1} K (a {2} MHz)", Fmt(tc, 0), Fmt(tt, 0), Fmt(f, 3));
            ottGtMaschera.IsEnabled = f >= 50;
        }

        void OttGtApplicaMaschera()
        {
            if (OttFreqPrincipale() < 50) { Status("La maschera VHF e' per le antenne da 50 MHz in su.", true); return; }
            foreach (OttRigaObiettivo r in ottOb)
            {
                switch (r.Chiave)
                {
                    case "ros": r.Peso.Value = 60; r.Soglia.Text = "≤ 1,3"; break;
                    case "gt": r.Peso.Value = 100; r.Soglia.Text = "max"; break;
                    default: r.Peso.Value = 0; break;
                }
            }
            OttGtAggiornaTesti();
            Status("Maschera VHF: ROS (soglia 1,3) e G/T dello stack; gli altri pesi a zero. Scegli le variabili e premi Avvia.");
        }

        void OttGtLeggi(Ottimizzatore o)
        {
            double f = OttFreqPrincipale(), tc, tt;
            OttGtImpostazioni.Temperature(f, out tc, out tt);
            o.Gt = new OttGtImpostazioni
            {
                Righe = OttGtRighe[ottGtStack], Colonne = OttGtColonne[ottGtStack], Veloce = ottGtVeloce,
                Elevazione = ParseNum(ottGtElev.Text, 30), Tcielo = tc, Tterreno = tt,
                NfDb = Math.Max(0, ParseNum(ottGtNf.Text, 0.25)), GuadagnoPreDb = ParseNum(ottGtGpre.Text, 20),
                CavoDopoDb = Math.Max(0, ParseNum(ottGtCavo.Text, 2)), PerditePrima = Math.Max(0, ParseNum(ottGtComb.Text, 0.2))
            };
        }

        /// <summary>Scheda in alto e tabella prima / dopo del pannello.</summary>
        void OttGtMostra()
        {
            if (ottGtPrimaDopo == null) return;
            ottGtPrimaDopo.Children.Clear();
            OttPasso b = ott != null ? (ottScelto ?? ott.Migliore) : null, p0 = ott != null ? ott.Iniziale : null;
            bool usato = ott != null && ott.Obiettivi.Exists(q => q.Chiave == "gt" && q.Peso > 0);
            if (!usato || b == null || p0 == null || b.Gt == null || p0.Gt == null)
            {
                OttScheda("gt", "-", usato ? " " : Lang.T("obiettivo non usato"));
                return;
            }
            string nome = OttGtStackNomi[Math.Max(0, Math.Min(3, ott.Gt.Righe == 2 && ott.Gt.Colonne == 2 ? 0 : ott.Gt.Colonne == 2 ? 1 : ott.Gt.Righe == 2 ? 2 : 3))];
            double gtB = !double.IsNaN(b.GtNec) ? b.GtNec : b.GtVoto, gt0 = !double.IsNaN(p0.GtNec) ? p0.GtNec : p0.GtVoto;
            ottCard["gt"][0].Text = Lang.F("G/T stack {0} a {1}°", Lang.T(nome), Fmt(ott.Gt.Elevazione, 0));
            OttScheda("gt", Fmt(gtB, 2) + " dB/K", Lang.F("partenza {0} ({1}{2} dB)", Fmt(gt0, 2), gtB >= gt0 ? "+" : "", Fmt(gtB - gt0, 2)), gtB > gt0 + 0.005 ? Pal.Green : (Color?)null);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
            string[][] righe =
            {
                new[] { "", "prima", "dopo" },
                new[] { Lang.F("G/T stack {0} a {1}° (sistema)", Lang.T(nome), Fmt(ott.Gt.Elevazione, 0)), Fmt(gt0, 2), Fmt(gtB, 2) },
                new[] { "G/T antenna (come VE7BQH)", Fmt(p0.Gt.GtAnt, 2), Fmt(b.Gt.GtAnt, 2) },
                new[] { "G/T singola", Fmt(p0.Gt.GtSingola, 2), Fmt(b.Gt.GtSingola, 2) },
                new[] { "T antenna (stack), K", Fmt(p0.Gt.Tant, 0), Fmt(b.Gt.Tant, 0) },
                new[] { "Guadagno stack, dBi", Fmt(p0.Gt.Gstack, 2), Fmt(b.Gt.Gstack, 2) },
                new[] { "Guadagno singola, dBi", Fmt(p0.Gt.Gsingola, 2), Fmt(b.Gt.Gsingola, 2) },
                new[] { "ROS peggiore", Fmt(p0.RosPeggiore, 2), Fmt(b.RosPeggiore, 2) },
                new[] { "Distanze DL6WU V x H, m", OttGtDist(p0.Gt), OttGtDist(b.Gt) },
            };
            for (int r = 0; r < righe.Length; r++)
            {
                g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int c = 0; c < 3; c++)
                {
                    TextBlock t = new TextBlock { Text = c == 0 || r == 0 ? Lang.T(righe[r][c]) : righe[r][c], Margin = new Thickness(4, 1, 8, 1), TextWrapping = c == 0 ? TextWrapping.Wrap : TextWrapping.NoWrap,
                        FontWeight = r == 0 ? FontWeight.SemiBold : FontWeight.Normal, Foreground = Pal.B(r == 0 ? Pal.Muted : Pal.Fore), HorizontalAlignment = c == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                    Grid.SetRow(t, r); Grid.SetColumn(t, c); g.Children.Add(t);
                }
            }
            ottGtPrimaDopo.Children.Add(new TextBlock { Text = "Prima / dopo", FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            ottGtPrimaDopo.Children.Add(g);
            ottGtPrimaDopo.Children.Add(Nota(Lang.F("G/T con lo stack NEC completo (accoppiamento compreso); controlli NEC durante l'ottimizzazione: {0}.", ott.GtControlli)));
        }

        string OttGtDist(OttGtRisultato r)
        {
            string v = r.Dv > 0 ? Fmt(r.Dv, 2) : "-", h = r.Dh > 0 ? Fmt(r.Dh, 2) : "-";
            return v + " x " + h;
        }
    }
}
