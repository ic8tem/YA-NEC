// YA-NEC 0.4.30 - scheda "Elica" della barra Geometria, dal bozzetto 1 rivisto approvato da IC8TEM il 14/11/2026 22:40: modo assiale
// (Kraus) / normale (corta) / libera, frequenza, spire, verso, circonferenza e passo in lambda, rastremazione (taper_factor di NECBOL),
// conduttore, fili per spira, riflettore (radiali / perfetto / nessuno), piano e base; "Esito anticipato" NEC contro Kraus/Emerson;
// calcolo in tempo reale a ogni modifica; Chiudi, Aggiungi al modello, Calcola (sostituisce il modello aperto; Annulla lo riporta).
// Motore: Engine/HelixGen.cs.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        readonly HelixSpec elSpec = new HelixSpec();
        TextBox elF, elSpire, elCirc, elPasso, elTaper, elCond, elFili, elDr, elRad, elBase;
        Grid elEsito;
        TextBlock elNota, elTempo;
        CancellationTokenSource elCts;
        DispatcherTimer elTimer;
        HelixEsito elUltimo;
        bool elSync;

        Control ElicaPannello()
        {
            StackPanel p = new StackPanel { Spacing = 4 };
            StackPanel tit = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            tit.Children.Add(new TextBlock { Text = "Elica", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            tit.Children.Add(new TextBlock { Text = "(geometria come NECBOL di G1OJS, MIT; formule Kraus / Emerson)", Foreground = Pal.B(Pal.Muted), FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
            p.Children.Add(tit);
            Func<string, double, TextBox> tb = (t, w) => { TextBox b = new TextBox { Text = t, Width = w, Margin = new Thickness(2), FontFamily = Pal.Mono }; b.TextChanged += (s, e) => ElCambia(); return b; };
            CultureInfo cc = CultureInfo.CurrentCulture;
            StackPanel modi = new StackPanel { Orientation = Orientation.Horizontal };
            modi.Children.Add(Scelta(new[] { "assiale (Kraus)", "normale (corta)", "libera" }, elSpec.Modo, k =>
            {
                HelixGen.Modo(elSpec, k);
                elSync = true;
                elCirc.Text = elSpec.Circ.ToString("0.00", cc); elPasso.Text = elSpec.Passo.ToString("0.00", cc); elFili.Text = elSpec.FiliPerSpira.ToString(cc);
                elSync = false;
                ElCambia();
            }));
            p.Children.Add(GeoRiga("Modo", modi));
            StackPanel fr = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            elF = tb(elSpec.FreqMHz.ToString("0.###", cc), 80); elSpire = tb(elSpec.Spire.ToString(cc), 60);
            fr.Children.Add(elF); fr.Children.Add(Lbl("MHz  spire")); fr.Children.Add(elSpire);
            fr.Children.Add(Scelta(new[] { "destra", "sinistra" }, elSpec.Destra ? 0 : 1, k => { elSpec.Destra = k == 0; ElCambia(); }));
            p.Children.Add(GeoRiga("Frequenza", fr));
            StackPanel ci = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            elCirc = tb(elSpec.Circ.ToString("0.00", cc), 70); elPasso = tb(elSpec.Passo.ToString("0.00", cc), 70);
            ci.Children.Add(elCirc); ci.Children.Add(Lbl("λ (Kraus 0,75-1,33)  passo")); ci.Children.Add(elPasso); ci.Children.Add(Lbl("λ (12-14°)"));
            p.Children.Add(GeoRiga("Circonferenza", ci));
            StackPanel ta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            elTaper = tb(elSpec.Rastremazione.ToString("0.00", cc), 70);
            ta.Children.Add(elTaper); ta.Children.Add(Lbl("raggio in cima / in basso (NECBOL taper_factor)"));
            p.Children.Add(GeoRiga("Rastremazione", ta));
            StackPanel co = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            elCond = tb((elSpec.Conduttore * 1000).ToString("0.##", cc), 60); elFili = tb(elSpec.FiliPerSpira.ToString(cc), 60);
            co.Children.Add(elCond); co.Children.Add(Lbl("mm  fili per spira")); co.Children.Add(elFili);
            p.Children.Add(GeoRiga("Conduttore", co));
            StackPanel ri = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            elDr = tb(elSpec.DiamRifl.ToString("0.0", cc), 60); elRad = tb(elSpec.Radiali.ToString(cc), 50);
            ri.Children.Add(Scelta(new[] { "radiali", "perfetto", "nessuno" }, elSpec.Riflettore, k => { elSpec.Riflettore = k; ElCambia(); }));
            ri.Children.Add(Lbl("Ø")); ri.Children.Add(elDr); ri.Children.Add(Lbl("λ  radiali")); ri.Children.Add(elRad);
            p.Children.Add(GeoRiga("Riflettore", ri));
            StackPanel pb = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            elBase = tb(elSpec.Base.ToString("0.###", cc), 70);
            pb.Children.Add(Scelta(new[] { "asse Z", "asse X" }, elSpec.AsseX ? 1 : 0, k => { elSpec.AsseX = k == 1; ElCambia(); }));
            pb.Children.Add(Lbl("base a")); pb.Children.Add(elBase); pb.Children.Add(Lbl("m"));
            p.Children.Add(GeoRiga("Piano e base", pb));

            StackPanel es = new StackPanel { Spacing = 2 };
            StackPanel eh = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            eh.Children.Add(new TextBlock { Text = "Esito anticipato", FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            eh.Children.Add(new TextBlock { Text = "(NEC-2 calcolato ora, banda +-4,6 %)", Foreground = Pal.B(Pal.Muted), FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center });
            es.Children.Add(eh);
            elEsito = new Grid { ColumnDefinitions = new ColumnDefinitions("170,*,*,*") };
            es.Children.Add(elEsito);
            elNota = Nota("");
            es.Children.Add(elNota);
            p.Children.Add(GeoBox(es));
            StackPanel tr = new StackPanel();
            tr.Children.Add(new TextBlock { Text = "Calcolo in tempo reale", FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            tr.Children.Add(new TextBlock { Text = "ogni modifica di un dato ricalcola subito: Z, guadagno, rapporto assiale e ROS si aggiornano qui.", TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), FontSize = 13 });
            elTempo = Nota("");
            tr.Children.Add(elTempo);
            p.Children.Add(GeoBox(tr));
            StackPanel bt = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
            Button chiudi = Btn("Chiudi", false), agg = Btn("Aggiungi al modello", false), calc = Btn("Calcola (sostituisce il modello aperto)", true);
            chiudi.Click += (s, e) => GeoApri(null);
            agg.Click += (s, e) => ElApplica(false);
            calc.Click += (s, e) => ElApplica(true);
            bt.Children.Add(chiudi); bt.Children.Add(agg); bt.Children.Add(calc);
            p.Children.Add(bt);
            p.Children.Add(new TextBlock { Text = "\"Calcola\" mette l'elica al posto del modello aperto e la calcola; Annulla nella barra Geometria riporta il modello di prima.", TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), FontSize = 12.5 });
            ElCambia();
            return new ScrollViewer
            {
                Content = new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12), Child = p },
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };
        }

        void ElCambia()
        {
            if (elSync || elEsito == null || elBase == null) return;
            try
            {
                elSpec.FreqMHz = ParseNum(elF.Text, 435);
                elSpec.Spire = Math.Max(1, (int)Math.Round(ParseNum(elSpire.Text, 10)));
                elSpec.Circ = ParseNum(elCirc.Text, 1); elSpec.Passo = ParseNum(elPasso.Text, 0.23);
                elSpec.Rastremazione = ParseNum(elTaper.Text, 1);
                elSpec.Conduttore = ParseNum(elCond.Text, 4) / 1000;
                elSpec.FiliPerSpira = Math.Max(6, (int)Math.Round(ParseNum(elFili.Text, 24)));
                elSpec.DiamRifl = ParseNum(elDr.Text, 0.8); elSpec.Radiali = Math.Max(4, (int)Math.Round(ParseNum(elRad.Text, 16)));
                elSpec.Base = ParseNum(elBase.Text, 0);
                if (elSpec.FreqMHz <= 0 || elSpec.Circ <= 0 || elSpec.Passo <= 0 || elSpec.Conduttore <= 0) throw new NecException("frequenza, circonferenza, passo e conduttore devono essere maggiori di zero");
                int ts, ss;
                NecModel prev = HelixGen.Costruisci(elSpec, null, out ts, out ss);
                SegmentGeometry g = prev.BuildGeometry();
                List<double[]> segs = new List<double[]>();
                for (int i = 0; i < g.N; i++) segs.Add(new[] { g.X1[i], g.Y1[i], g.Z1[i], g.X2[i], g.Y2[i], g.Z2[i] });
                double lam = 299.792458 / elSpec.FreqMHz, D = elSpec.Circ * lam / Math.PI * 1000, S = elSpec.Passo * lam * 1000;
                if (view3d != null)
                {
                    view3d.Preview = segs; view3d.PreviewCenter = null;
                    view3d.PreviewLabel = string.Format(CultureInfo.CurrentCulture, "D {0:0} mm, passo {1:0} mm ({2:0.0}°), {3} spire, filo {4:0.#} mm", D, S, Math.Atan(S / (Math.PI * D)) * 180 / Math.PI, elSpec.Spire, elSpec.Conduttore * 1000);
                    view3d.PreviewCenterText = ""; view3d.InvalidateVisual();
                }
                elNota.Text = "calcolo in corso..."; elNota.Foreground = Pal.B(Pal.Muted);
            }
            catch (Exception ex) { elNota.Text = ex.Message; elNota.Foreground = Pal.B(Pal.Red); return; }
            if (elTimer == null)
            {
                elTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                elTimer.Tick += (s, e) => { elTimer.Stop(); ElCalcola(); };
            }
            elTimer.Stop(); elTimer.Start();
        }

        async void ElCalcola()
        {
            if (elCts != null) elCts.Cancel();
            elCts = new CancellationTokenSource();
            CancellationToken tok = elCts.Token;
            HelixSpec s = elSpec.Clone();
            DateTime t0 = DateTime.Now;
            try
            {
                HelixEsito r;
                using (Prestazioni.Misura("Elica: esito anticipato", Fmt(s.FreqMHz, 3) + " MHz, " + s.Spire + " spire"))
                    r = await Task.Run(() => { NecModel m; return HelixGen.Esito(s, tok, out m); });
                if (tok.IsCancellationRequested) return;
                elUltimo = r;
                ElMostra(r, s);
                elTempo.Text = "● aggiornato in " + Fmt((DateTime.Now - t0).TotalSeconds, 1) + " s · ROS su 50 Ω " + Fmt(r.Swr, 2);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { elNota.Text = "Errore NEC: " + ex.Message; elNota.Foreground = Pal.B(Pal.Red); }
        }

        void ElMostra(HelixEsito r, HelixSpec s)
        {
            elEsito.Children.Clear(); elEsito.RowDefinitions.Clear();
            Func<System.Numerics.Complex, string> zs = z => Fmt(z.Real, 1) + (z.Imaginary < 0 ? " -j" : " +j") + Fmt(Math.Abs(z.Imaginary), 1) + " Ω";
            List<string[]> righe = new List<string[]>
            {
                new[] { "", "NEC", "Kraus", "nota" },
                new[] { "Z d'ingresso", zs(r.Z), Fmt(r.Zkraus, 0) + " Ω", "Kraus: 140 C/λ; il NEC dipende dal tratto di alimentazione" },
                new[] { "Guadagno lungo l'asse", Fmt(r.GdbicNec, 1) + " dBic", Fmt(r.GdbicKraus, 1) + " dBic", "Emerson " + Fmt(r.GdbicEmerson, 1) + " dBic" },
                new[] { "Rapporto assiale", Fmt(r.ArNec, 2) + " (" + Fmt(r.ArDbNec, 1) + " dB)", "(2N+1)/2N = " + Fmt(r.ArKraus, 2), r.DestraNec ? "destra" : "sinistra" },
                new[] { "Banda " + Fmt(s.FreqMHz * 0.954, 0) + "-" + Fmt(s.FreqMHz * 1.046, 0) + " MHz", "Z " + Fmt(r.Zbassa.Real, 0) + " / " + Fmt(r.Zalta.Real, 0) + " Ω", Fmt(r.Fbassa, 0) + "-" + Fmt(r.Falta, 0) + " MHz", "apertura Kraus " + Fmt(r.HpbwKraus, 0) + "°" },
            };
            for (int i = 0; i < righe.Count; i++)
            {
                elEsito.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int c = 0; c < 4; c++)
                {
                    TextBlock t = new TextBlock { Text = righe[i][c], FontFamily = i == 0 || c == 3 ? FontFamily.Default : Pal.Mono, FontSize = 13, Foreground = Pal.B(i == 0 ? Pal.Muted : c == 3 ? Pal.Orange : Pal.Fore), Margin = new Thickness(4, 2), TextWrapping = TextWrapping.Wrap };
                    Grid.SetRow(t, i); Grid.SetColumn(t, c); elEsito.Children.Add(t);
                }
            }
            string nota = r.Nota;
            if (s.Riflettore == 0) nota += (nota.Length > 0 ? " " : "") + "Con i radiali il riflettore non e' pieno: il guadagno cala rispetto al piano perfetto (prova con nec2c a 435 MHz: 7,8 contro 11,0 dBi con 16 radiali da 0,8 λ).";
            if (s.Riflettore == 1 && s.AsseX) nota += (nota.Length > 0 ? " " : "") + "Il riflettore perfetto e' il terreno: vale solo con l'asse Z.";
            nota += (nota.Length > 0 ? " " : "") + "Adattamento a 50 Ω: lamina a λ/4 o ultima mezza spira avvicinata al piano.";
            elNota.Text = nota; elNota.Foreground = Pal.B(Pal.Muted);
        }

        void ElApplica(bool sostituisci)
        {
            try
            {
                int ts, ss;
                HelixSpec s = elSpec.Clone();
                NecModel m = HelixGen.Costruisci(s, sostituisci || model == null ? null : model, out ts, out ss);
                if (sostituisci || model == null)
                {
                    m.Comments.Clear();
                    m.Comments.Add(string.Format(CultureInfo.InvariantCulture, "Elica {0} spire, C {1} lambda, passo {2} lambda, {3} MHz (YA-NEC, barra Geometria, scheda Elica)", s.Spire, s.Circ, s.Passo, s.FreqMHz));
                    m.FileName = "elica.nec";
                }
                Registro.Scrivi("AZIONE", "geometria: elica " + (sostituisci ? "al posto del modello" : "aggiunta al modello"));
                if (model == null) { SetModel(m, null); Calculate(true); }
                else GeoMetti(m, true);
                GeoApri(null);
                Status("Elica " + (sostituisci ? "al posto del modello (Annulla nella barra Geometria riporta quello di prima)" : "aggiunta al modello") + ".");
            }
            catch (Exception ex) { Status("Elica: " + ex.Message, true); }
        }
    }
}
