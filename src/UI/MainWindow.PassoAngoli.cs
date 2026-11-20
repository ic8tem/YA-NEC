// YA-NEC 1.5.3 - passo degli angoli del diagramma 3D adattato al computer (approvato da IC8TEM il 20/11/2026 alle 12:36; calcolo in
// src/Engine/PassoAngoli.cs). Impostazioni > Diagrammi: pillole auto, 0,5, 1, 2, 5; una pillola si spegne se sul computer servirebbero piu'
// di 10 s per il modello aperto; in rosso quella al limite (prevista tra 1 e 10 s, «la segnalazione più vicina»); fumetto con il tempo previsto.
// La velocita' misurata e i tempi previsti vanno nel registro (IC8TEM: «lo riporti nel registro»).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        readonly List<Tuple<Button, double>> passoBtn = new List<Tuple<Button, double>>();
        double velocitaSalvata = double.NaN;
        NecModel passoRegistrato;

        void AvviaPassoAngoli()
        {
            PassoAngoli.Velocita = cfg.GetD("campo_lontano_velocita", double.NaN);
            PassoAngoli.Scelta = cfg.GetD("passo_angoli", 0);
            if (Array.IndexOf(PassoAngoli.Passi, PassoAngoli.Scelta) < 0) PassoAngoli.Scelta = 0;
            velocitaSalvata = PassoAngoli.Velocita;
            PassoAngoli.Salva = v =>
            {
                // in config.ini solo se cambia di piu' del 5 % (l'ottimizzatore e gli sweep non scrivono il file a ogni passo)
                if (!double.IsNaN(velocitaSalvata) && Math.Abs(v / velocitaSalvata - 1) < 0.05) return;
                velocitaSalvata = v;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { cfg.SetD("campo_lontano_velocita", v); AggiornaPassoAngoli(); });
            };
            if (double.IsNaN(PassoAngoli.Velocita) && !ProveAutomatiche)
                Task.Run(() =>
                {
                    // prima volta su questo computer: un dipolo di 21 segmenti a 1 grado (circa un decimo di secondo)
                    try
                    {
                        NecModel d = NecModel.Parse("GW 1 21 0 -0.5 0 0 0.5 0 0.001\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 145 0\nEN\n");
                        Analysis.Run(d, d.BuildGeometry(), 145, 1.0);
                        Registro.Scrivi("COMPUTER", "prova del campo lontano: " + PassoAngoli.Descrizione(0, false));
                    }
                    catch (Exception ex) { Registro.Scrivi("COMPUTER", "prova del campo lontano non riuscita: " + ex.Message); }
                });
        }

        int SegmentiAperti() { return geo != null ? geo.N : 0; }
        bool TerrenoAperto() { return model != null && model.Ground.Kind != GroundKind.FreeSpace; }

        /// <summary>Passo del diagramma 3D per il modello aperto (2 gradi nelle prove automatiche, per avere risultati sempre uguali).</summary>
        double PassoDiagramma()
        {
            if (ProveAutomatiche) return 2.0;
            return PassoAngoli.Passo(SegmentiAperti(), TerrenoAperto());
        }

        Control PilloleAngoli()
        {
            StackPanel sp = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 2 };
            passoBtn.Clear();
            double[] v = { 0, 0.5, 1, 2, 5 };
            foreach (double p in v)
            {
                double pp = p;
                Button b = Btn(p == 0 ? "auto" : p.ToString("0.#", CultureInfo.CurrentCulture), false);
                b.BorderThickness = new Thickness(1);
                b.BorderBrush = Pal.B(Pal.PillBorder);
                b.Click += (s, e) =>
                {
                    PassoAngoli.Scelta = pp;
                    cfg.SetD("passo_angoli", pp);
                    AggiornaPassoAngoli();
                    RegistraPassoAngoli(true);
                    if (run != null && cts != null) AffinaDiagrammaSeServe(run, cts.Token, true);
                };
                passoBtn.Add(Tuple.Create(b, p));
                sp.Children.Add(b);
            }
            AggiornaPassoAngoli();
            return sp;
        }

        void AggiornaPassoAngoli()
        {
            int n = SegmentiAperti();
            bool gnd = TerrenoAperto();
            double usato = PassoDiagramma();
            foreach (Tuple<Button, double> t in passoBtn)
            {
                Button b = t.Item1;
                double p = t.Item2;
                bool scelto = p == PassoAngoli.Scelta;
                StileScelta(b, scelto);
                double tp = p > 0 && n > 0 ? PassoAngoli.Previsto(n, p, gnd) : double.NaN;
                b.IsEnabled = p == 0 || double.IsNaN(tp) || tp <= PassoAngoli.Limite;
                bool rosso = !double.IsNaN(tp) && tp > PassoAngoli.Rosso && tp <= PassoAngoli.Limite;
                if (rosso)
                {
                    b.BorderBrush = Pal.B(Pal.Red);
                    if (!scelto) b.Foreground = Pal.B(Pal.Red);
                    else b.Background = Pal.B(Pal.Red);
                }
                else b.BorderBrush = Pal.B(Pal.PillBorder);
                string tip;
                if (p == 0) tip = "Il passo più fine che sul modello aperto costa meno di mezzo secondo: ora " + usato.ToString("0.#", CultureInfo.CurrentCulture) + " gradi.";
                else if (n == 0 || double.IsNaN(tp)) tip = "Tempo previsto: si sa dopo il primo calcolo.";
                else if (!b.IsEnabled) tip = "Su questo computer, per il modello aperto, servirebbero circa " + PassoAngoli.Tempo(tp) + ": oltre 10 s non si può scegliere.";
                else tip = "Tempo previsto per il modello aperto su questo computer: " + PassoAngoli.Tempo(tp) + (rosso ? " (lento: oltre 1 s)." : ".");
                ToolTip.SetTip(b, tip);
            }
        }

        /// <summary>Riga del registro con la velocita' del computer e i tempi previsti (una volta per modello, o quando cambia la scelta).</summary>
        void RegistraPassoAngoli(bool sempre = false)
        {
            if (!sempre && passoRegistrato == model) return;
            passoRegistrato = model;
            Registro.Scrivi("COMPUTER", "passo degli angoli: " + PassoAngoli.Descrizione(SegmentiAperti(), TerrenoAperto()));
        }
    }
}
