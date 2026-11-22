// YA-NEC 0.4.2 - sezione "Calcolo in tempo reale" delle schede dei Calcolatori (richiesta di IC8TEM del 13/11/2026, 14:58):
// in ogni scheda con un modello d'antenna, a ogni modifica dei dati, il NEC calcola in background solo il modello della scheda
// (il modello aperto non si usa) e mostra almeno: ROS minimo e i limiti a ROS 2,0, guadagno, F/B, fronte/lato;
// sopra i 28 MHz anche gli angoli a -3 dB e le distanze di stack DL6WU.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        /// <summary>Esito del calcolo in tempo reale (tutto in un passaggio, fuori dal thread dell'interfaccia).</summary>
        sealed class EsitoTempoReale
        {
            public double F, Z0, SwrMin = double.NaN, FSwrMin, Lim1 = double.NaN, Lim2 = double.NaN, Da, A;
            public bool PiuSorgenti;
            public RunResult R;
            public double FronteLato = double.NaN;
            // 1.0.9: aperture a -3 dB nello spazio libero, per le distanze di stack DL6WU (sopra un terreno l'apertura verticale e' quella
            // del lobo di riflessione, 1-2 gradi, e la formula dava 60-77 m)
            public double HpbwAzFs = double.NaN, HpbwElFs = double.NaN;
            public bool ConTerreno;
            public double[] BandeF, BandeRos;          // 1.5.6: ROS alle bande della scheda (Filari)
            public System.Numerics.Complex[] BandeZ;
        }

        static EsitoTempoReale CalcolaTempoReale(string nec, double z0, double da, double a, CancellationToken tok, double[] bande = null)
        {
            EsitoTempoReale es = new EsitoTempoReale { Z0 = z0 };
            NecModel m = NecModel.Parse(nec);
            m.Z0 = z0;
            double f = m.FreqMHz;
            es.F = f;
            SegmentGeometry g = m.BuildGeometry();
            es.R = Analysis.Run(m, g, f, 2.0, tok);
            es.PiuSorgenti = es.R.Sources.Count != 1;
            // fronte/lato: guadagno massimo meno il peggiore tra 60 e 120 gradi dalla direzione del massimo, sui due lati
            if (es.R.AzCut != null)
            {
                double lato = -999.99;
                for (int d = 60; d <= 120; d++)
                    foreach (int s in new[] { 1, -1 })
                    {
                        int k = (int)Math.Round(es.R.PhiMax + s * d);
                        k = ((k % 360) + 360) % 360;
                        lato = Math.Max(lato, es.R.AzCut[k]);
                    }
                es.FronteLato = lato <= -999 ? 99.9 : es.R.GmaxDbi - lato;
            }
            if (f > 28.0)
            {
                es.ConTerreno = m.Ground != null && m.Ground.Kind != GroundKind.FreeSpace;
                if (!es.ConTerreno) { es.HpbwAzFs = es.R.HpbwAz; es.HpbwElFs = es.R.HpbwEl; }
                else
                {
                    try
                    {
                        NecModel mfs = NecModel.Parse(nec);
                        mfs.Z0 = z0;
                        mfs.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
                        RunResult rfs = Analysis.Run(mfs, mfs.BuildGeometry(), f, 2.0, tok);
                        es.HpbwAzFs = rfs.HpbwAz; es.HpbwElFs = rfs.HpbwEl;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception) { }
                }
            }
            if (es.PiuSorgenti) return es;
            // ROS: sweep di sola impedenza attorno alla frequenza (+-10 %, poi rifinitura attorno al minimo)
            Func<double, double> ros = ff =>
            {
                tok.ThrowIfCancellationRequested();
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(ff, Analysis.EffectiveSources(m, g, ff), m.EffectiveLoads(), TerreniEdifici.Alla(m.Ground, ff), m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(ff));
                System.Numerics.Complex zz = e.Sources[0].Z;
                double gam = System.Numerics.Complex.Abs((zz - z0) / (zz + z0));
                return gam >= 0.999999 ? 999 : (1 + gam) / (1 - gam);
            };
            // 0.4.8 (registri 0.4.7: 1,6 s e 500 MB a calcolo): griglia di 21 punti calcolata in parallelo, poi il minimo e i due
            // limiti a ROS 2,0 cercati per bisezione invece che con una griglia fitta (circa 40 soluzioni invece di 62, stessi risultati
            // entro 1 kHz sui limiti)
            if (bande != null && bande.Length > 0)
            {
                es.BandeF = bande; es.BandeRos = new double[bande.Length]; es.BandeZ = new System.Numerics.Complex[bande.Length];
                Parallel.For(0, bande.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2), CancellationToken = tok }, i =>
                {
                    Nec2Engine e = new Nec2Engine(g);
                    e.Solve(bande[i], Analysis.EffectiveSources(m, g, bande[i]), m.EffectiveLoads(), TerreniEdifici.Alla(m.Ground, bande[i]), m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(bande[i]));
                    System.Numerics.Complex zz = e.Sources[0].Z;
                    es.BandeZ[i] = zz;
                    double gam = System.Numerics.Complex.Abs((zz - z0) / (zz + z0));
                    es.BandeRos[i] = gam >= 0.999999 ? 999 : (1 + gam) / (1 - gam);
                });
            }
            es.Da = double.IsNaN(da) ? f * 0.9 : da; es.A = double.IsNaN(a) ? f * 1.1 : a;   // banda della scheda (LPDA) o +-10 %
            const int NG = 20;
            double[] gf = new double[NG + 1], gs = new double[NG + 1];
            for (int i = 0; i <= NG; i++) gf[i] = es.Da + (es.A - es.Da) * i / NG;
            Parallel.For(0, NG + 1, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2), CancellationToken = tok }, i => { gs[i] = ros(gf[i]); });
            int im = 0;
            for (int i = 1; i <= NG; i++) if (gs[i] < gs[im]) im = i;
            // minimo: sezione aurea tra i due punti vicini della griglia
            double lo = gf[Math.Max(0, im - 1)], hi = gf[Math.Min(NG, im + 1)], fm = gf[im], sm = gs[im];
            double gr = (Math.Sqrt(5) - 1) / 2, x1 = hi - gr * (hi - lo), x2 = lo + gr * (hi - lo), r1 = ros(x1), r2 = ros(x2);
            for (int k = 0; k < 8; k++)
            {
                if (r1 < r2) { hi = x2; x2 = x1; r2 = r1; x1 = hi - gr * (hi - lo); r1 = ros(x1); }
                else { lo = x1; x1 = x2; r1 = r2; x2 = lo + gr * (hi - lo); r2 = ros(x2); }
            }
            if (r1 < sm) { sm = r1; fm = x1; }
            if (r2 < sm) { sm = r2; fm = x2; }
            es.SwrMin = sm; es.FSwrMin = fm;
            // limiti a ROS 2,0: primo incrocio sotto e sopra il minimo, poi bisezione (7 passi: 1/128 del passo della griglia)
            Func<double, double, double, double, double> bisez = (fa, sa, fb, sb) =>
            {
                for (int k = 0; k < 7; k++)
                {
                    double fc = (fa + fb) / 2, sc = ros(fc);
                    if ((sa - 2.0) * (sc - 2.0) <= 0) { fb = fc; sb = sc; } else { fa = fc; sa = sc; }
                }
                return fa + (sa - 2.0) / (sa - sb) * (fb - fa);
            };
            if (sm <= 2.0)
            {
                for (int i = im; i > 0; i--)
                    if (gs[i - 1] > 2.0 && gs[i] <= 2.0) { es.Lim1 = bisez(gf[i - 1], gs[i - 1], gf[i], gs[i]); break; }
                for (int i = im; i < NG; i++)
                    if (gs[i] <= 2.0 && gs[i + 1] > 2.0) { es.Lim2 = bisez(gf[i], gs[i], gf[i + 1], gs[i + 1]); break; }
            }
            return es;
        }

        /// <summary>Riquadro "Calcolo in tempo reale"; avvia(nec, z0, da, a) (da/a NaN = +-10 %) parte con 400 ms di attesa e annulla il calcolo precedente.</summary>
        Border TempoRealePanel(out Action<string, double, double, double> avvia)
        {
            Action<double[]> nessuna;
            return TempoRealePanel(out avvia, out nessuna);
        }

        Border TempoRealePanel(out Action<string, double, double, double> avvia, out Action<double[]> avviaBande)
        {
            double[] bandeAttese = null;
            avviaBande = b => bandeAttese = b;
            StackPanel box = new StackPanel { Spacing = 4 };
            StackPanel testa = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };
            testa.Children.Add(new TextBlock { Text = "Calcolo in tempo reale", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            TextBlock stato = new TextBlock { Text = "in attesa del modello", Foreground = Pal.B(Pal.Muted), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            testa.Children.Add(stato);
            box.Children.Add(testa);
            box.Children.Add(new TextBlock
            {
                Text = "Solo il modello di questa scheda (il modello aperto non si usa). ROS riferito a " + "Z0 del programma; sopra i 28 MHz anche angoli a -3 dB e distanze di stack DL6WU.",
                TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Foreground = Pal.B(Pal.Muted)
            });
            WrapPanel cards = new WrapPanel();
            box.Children.Add(cards);
            Border panel = Scheda(box);
            panel.Margin = new Thickness(0, 0, 0, 8);

            CancellationTokenSource cts = null;
            DispatcherTimer attesa = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            string necAtteso = null; double z0Atteso = 50, daAtteso = double.NaN, aAtteso = double.NaN;
            attesa.Tick += (s, e) =>
            {
                attesa.Stop();
                if (necAtteso == null) return;
                if (cts != null) cts.Cancel();
                cts = new CancellationTokenSource();
                CancellationToken tok = cts.Token;
                string nec = necAtteso; double z0 = z0Atteso, da = daAtteso, a = aAtteso; double[] bnd = bandeAttese;
                stato.Text = "calcolo...";
                DateTime t0 = DateTime.Now;
                Task.Run(() => { using (Prestazioni.Misura("Calcolatore: calcolo in tempo reale", null, false)) return CalcolaTempoReale(nec, z0, da, a, tok, bnd); }, tok).ContinueWith(t => Dispatcher.UIThread.Post(() =>
                {
                    if (tok.IsCancellationRequested) return;
                    if (t.IsFaulted) { stato.Text = "errore: " + t.Exception.GetBaseException().Message; return; }
                    EsitoTempoReale es = t.Result;
                    RunResult r = es.R;
                    cards.Children.Clear();
                    if (es.PiuSorgenti) cards.Children.Add(Riquadretto("ROS", "piu' sorgenti", "vedi le impedenze delle sorgenti", true));
                    else
                    {
                        cards.Children.Add(Riquadretto("ROS minimo (" + FN(es.Z0, 0) + " Ω)", FN(es.SwrMin, 2), Lang.F("a {0} MHz", FN(es.FSwrMin, 3)), true));
                        string lim = double.IsNaN(es.SwrMin) || es.SwrMin > 2.0 ? "ROS mai sotto 2"
                            : (double.IsNaN(es.Lim1) ? "sotto " + FN(es.Da, 3) : FN(es.Lim1, 3)) + " - " + (double.IsNaN(es.Lim2) ? "oltre " + FN(es.A, 3) : FN(es.Lim2, 3)) + " MHz";
                        cards.Children.Add(Riquadretto("Limiti a ROS 2,0", lim, Lang.F("cercati tra {0} e {1} MHz", FN(es.Da, 3), FN(es.A, 3)), true));
                    }
                    cards.Children.Add(Riquadretto("Guadagno", FN(r.GmaxDbi, 2) + " dBi", FN(r.GmaxDbi - 2.15, 2) + " dBd, elevazione " + FN(r.ElevationOfMax, 0) + "°", true));
                    cards.Children.Add(Riquadretto("F/B", FN(r.FrontBack, 1) + " dB", "F/R " + FN(r.FrontRear, 1) + " dB"));
                    cards.Children.Add(Riquadretto("Fronte/lato", es.FronteLato >= 99 ? "> 99 dB" : FN(es.FronteLato, 1) + " dB", "peggiore tra 60° e 120°"));
                    if (es.F > 28.0)
                    {
                        double lam = 299.792458 / es.F;
                        // 1.0.6: con un diagramma omnidirezionale l'angolo non esiste (NaN): si scrive "omni" invece di "NaN°"
                        bool okAz = !double.IsNaN(r.HpbwAz) && r.HpbwAz > 0, okEl = !double.IsNaN(r.HpbwEl) && r.HpbwEl > 0;
                        cards.Children.Add(Riquadretto("Angoli a -3 dB", (okAz ? FN(r.HpbwAz, 1) + "°" : "omni") + " / " + (okEl ? FN(r.HpbwEl, 1) + "°" : "-"),
                            okAz ? "azimut / elevazione" : "azimut / elevazione (omni: nessun calo di 3 dB in azimut)"));
                        bool okAzF = !double.IsNaN(es.HpbwAzFs) && es.HpbwAzFs > 0, okElF = !double.IsNaN(es.HpbwElFs) && es.HpbwElFs > 0;
                        string st = okAzF && okElF ? FN(StackDetect.Dl6wu(lam, es.HpbwAzFs), 2) + " / " + FN(StackDetect.Dl6wu(lam, es.HpbwElFs), 2) + " m" : "-";
                        cards.Children.Add(Riquadretto("Distanze di stack DL6WU", st, "affiancate / sovrapposte (punto di partenza)" + (es.ConTerreno ? ", aperture nello spazio libero" : "")));
                    }
                    if (es.BandeF != null)
                        for (int i = 0; i < es.BandeF.Length; i++)
                        {
                            int ib = Array.IndexOf(CalcFilari.BandeF, es.BandeF[i]);
                            string nome = ib >= 0 ? CalcFilari.BandeNomi[ib] : FN(es.BandeF[i], 3) + " MHz";
                            System.Numerics.Complex zb = es.BandeZ[i];
                            double rb = es.BandeRos[i];
                            cards.Children.Add(Riquadretto(nome + ", ROS (NEC)", rb >= 99 ? "> 99" : FN(rb, 2),
                                (rb <= 2 ? "✓ " : (rb <= 3 ? "accordatore interno, " : "accordatore esterno, ")) + "Z " + FN(zb.Real, 0) + (zb.Imaginary >= 0 ? " +j" : " -j") + FN(Math.Abs(zb.Imaginary), 0) + " Ω", rb <= 2));
                        }
                    stato.Text = Lang.F("aggiornato ({0} s) a {1} MHz", FN((DateTime.Now - t0).TotalSeconds, 1), FN(es.F, 3));
                }));
            };
            avvia = (nec, z0, da, a) =>
            {
                necAtteso = nec; z0Atteso = z0; daAtteso = da; aAtteso = a;
                if (nec == null) { cards.Children.Clear(); stato.Text = "questa scheda non crea un modello d'antenna"; panel.IsVisible = false; return; }
                panel.IsVisible = true;
                stato.Text = "in coda...";
                attesa.Stop(); attesa.Start();
            };
            return panel;
        }
    }
}
