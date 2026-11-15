// YA-NEC 0.4.30 - sottolinguetta dei Calcolatori "Dipolo multibanda": bacchette parallele, manica aperta (open sleeve), radiatori della
// Force 12 XR-5 (tre modi proposti da IC8TEM il 14/11/2026 21:26-21:27, approvati alle 23:11, XR-5 scelta alle 23:18). Calcolo solo
// della scheda: il modello aperto non viene toccato ("Apri come modello" a richiesta). Motore: Engine/DipoloMultibanda.cs.
using System;
using System.Collections.Generic;
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
        // 0.4.30: ultimi radiatori della scheda Dipolo multibanda, per la scheda Yagi multibanda HF ("Prendi dal Dipolo multibanda")
        DmSpec dmCondiviso;

        Control BuildDipoloMbPanel()
        {
            DmSpec spec = new DmSpec();
            DipoloMultibanda.Iniziale(spec);
            CancellationTokenSource cts = null;
            List<DmBanda> esito = null;
            StackPanel root = new StackPanel();
            Grid top = new Grid { ColumnDefinitions = new ColumnDefinitions("*,14,*") };
            StackPanel sx = new StackPanel { Spacing = 3 };
            sx.Children.Add(new TextBlock { Text = "Dipolo multibanda", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            Action aggiorna = null;
            TextBlock tAv = Nota("");
            EditorRad ed = CreaEditorRadiatori(spec, () => { if (aggiorna != null) aggiorna(); }, m => tAv.Text = m);
            sx.Children.Add(ed.Vista);
            sx.Children.Add(Nota("L'accordo cerca insieme tutte le misure (lunghezze; distanze dei parassiti; per l'XR-5 punte, posizioni del 17, 15 e 10 m e linea) " +
                "per il ROS piu' basso al centro di ogni banda su 50 ohm: le bande si influenzano, quindi non si accordano una per volta. Il modello aperto non viene toccato."));
            ProgressBar pb = new ProgressBar { Minimum = 0, Maximum = 1, Height = 6, Margin = new Thickness(0, 4) };
            sx.Children.Add(pb); sx.Children.Add(tAv);
            WrapPanel bt = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
            Button bApri = Btn("Apri come modello", false), bYagi = Btn("Esporta .nec per Yagi HF", false), bVer = Btn("Verifica", false), bAcc = Btn("Accorda e verifica", true);
            foreach (Button b in new[] { bApri, bYagi, bVer }) b.Margin = new Thickness(0, 6, 8, 0);
            bAcc.Margin = new Thickness(0, 6, 0, 0);
            bt.Children.Add(bApri); bt.Children.Add(bYagi); bt.Children.Add(bVer); bt.Children.Add(bAcc);
            sx.Children.Add(bt);
            top.Children.Add(Scheda(sx));

            StackPanel dx = new StackPanel();
            // 0.4.30: schizzo del modo scelto con le misure (bozzetto 13 approvato il 15/11/2026 00:02)
            DipoloMbDisegno schizzo = new DipoloMbDisegno { Height = 360, Margin = new Thickness(0, 0, 0, 8) };
            dx.Children.Add(new Border { Child = schizzo, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ClipToBounds = true, Margin = new Thickness(0, 0, 0, 8) });
            dx.Children.Add(new TextBlock { Text = "Modello NEC (vista 3D)", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 4) });
            Structure3DView v3 = new Structure3DView { Height = 420, ShowPattern = false, ShowGroundAndHeight = false, ShowPhases = false };
            dx.Children.Add(new Border { Child = v3, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ClipToBounds = true });
            TextBlock n3 = Nota("");
            dx.Children.Add(n3);
            Border bdx = Scheda(dx);
            Grid.SetColumn(bdx, 2);
            top.Children.Add(bdx);
            root.Children.Add(top);

            StackPanel pr = new StackPanel();
            pr.Children.Add(new TextBlock { Text = "Risultati per banda", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            Grid gr = new Grid { ColumnDefinitions = new ColumnDefinitions("60,2*,150,170,80,*") };
            pr.Children.Add(gr);
            pr.Children.Add(Nota("ROS su 50 ohm a inizio / centro / fine banda; guadagno massimo al centro; \"ruba\" = elemento di un'altra banda con piu' del 30 % della corrente dell'elemento della banda."));
            root.Children.Add(Scheda(pr));

            Action mostra = () =>
            {
                gr.Children.Clear(); gr.RowDefinitions.Clear();
                string[] hh = { "banda", "misure", "Z al centro", "ROS inizio / centro / fine", "guadagno", "ruba" };
                gr.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int i = 0; i < hh.Length; i++) { TextBlock t = new TextBlock { Text = hh[i], FontSize = 12, Foreground = Pal.B(Pal.Muted) }; Grid.SetColumn(t, i); gr.Children.Add(t); }
                int riga = 1;
                foreach (string b in DipoloMultibanda.TutteBande(spec))
                {
                    DmBanda d = esito != null ? esito.Find(x => x.Banda == b) : null;
                    DmTrappola trb = spec.Modo == 0 ? spec.Trappole.Find(q => q.Nuova == b && spec.Bande.Contains(q.Dipolo)) : null;
                    string mis = trb != null ? DipoloMultibanda.DescriviTrappola(trb) : spec.Modo == 2 ? (spec.L.ContainsKey(b) ? string.Format("punte {0}, spost. {1} m", Fmt(spec.L[b], 3), Fmt(spec.S[b], 3)) : "")
                                                : (spec.L.ContainsKey(b) ? Fmt(spec.L[b], 3) + " m" + (spec.Modo == 1 && spec.S[b] > 0 ? " a " + Fmt(spec.S[b], 3) + " m" : "") : "");
                    string[] v = { b, mis, d == null ? "..." : Fmt(d.Z.Real, 1) + (d.Z.Imaginary < 0 ? " -j" : " +j") + Fmt(Math.Abs(d.Z.Imaginary), 1),
                                   d == null ? "..." : Fmt(d.RosLo, 2) + " / " + Fmt(d.RosC, 2) + " / " + Fmt(d.RosHi, 2), d == null ? "..." : Fmt(d.G, 1) + " dBi", d == null ? "..." : d.Ruba };
                    gr.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int i = 0; i < v.Length; i++)
                    {
                        TextBlock t = new TextBlock { Text = v[i], FontSize = 13, FontFamily = i < 5 ? Pal.Mono : FontFamily.Default, TextWrapping = TextWrapping.Wrap,
                            Foreground = Pal.B(i == 3 && d != null && d.RosC > 2 || i == 5 && d != null && d.Ruba != "nessuno" ? Pal.Orange : Pal.Fore) };
                        Grid.SetRow(t, riga); Grid.SetColumn(t, i); gr.Children.Add(t);
                    }
                    riga++;
                }
            };
            Action disegna = () =>
            {
                try
                {
                    Dictionary<int, string> tb;
                    NecModel m = DipoloMultibanda.Modello(spec, DipoloMultibanda.Banda(spec.Bande[0]).Operating, out tb);
                    SegmentGeometry g = m.BuildGeometry();
                    RiempiSchizzo(schizzo, spec, m, tb, null);
                    v3.Geo = g; v3.Ground = spec.Terreno; v3.CurrentNorm = null;
                    v3.SourceSegs = new HashSet<int>();
                    foreach (SourceSpec so in m.Sources) v3.SourceSegs.Add(g.SegNo(so.Tag, so.Seg) - 1);
                    v3.InvalidateVisual();
                    n3.Text = m.Geometry.Count + " fili, " + g.N + " segmenti. Trascina per ruotare, rotella per ingrandire.";
                    // 0.5.1: controlli k3, s5, s6 sulle trappole
                    foreach (ControlloDato cd in ControlliDati.DipoloMb(spec)) n3.Text += "\n⚠ " + cd.ToString();
                }
                catch (Exception ex) { n3.Text = "Modello NEC: " + ex.Message; }
            };
            aggiorna = () =>
            {
                if (cts != null) { cts.Cancel(); cts = null; }
                pb.Value = 0;
                ed.Leggi();
                try { DipoloMultibanda.Iniziale(spec); }
                catch (Exception ex) { tAv.Text = ex.Message; }
                esito = null;
                tAv.Text = "Misure di partenza: \"Accorda e verifica\" accorda tutte le bande insieme.";
                mostra(); disegna();
                dmCondiviso = spec.Clone();
            };

            Func<bool, Task> esegui = async accorda =>
            {
                if (cts != null) cts.Cancel();
                cts = new CancellationTokenSource();
                CancellationToken tok = cts.Token;
                DmSpec sp = spec.Clone();
                pb.IsIndeterminate = true;
                DateTime t0 = DateTime.Now;
                try
                {
                    List<DmBanda> r = await Task.Run(() =>
                    {
                        if (accorda) DipoloMultibanda.Accorda(sp, tok, m => Dispatcher.UIThread.Post(() => { if (!tok.IsCancellationRequested) tAv.Text = m; }));
                        Dispatcher.UIThread.Post(() => { if (!tok.IsCancellationRequested) tAv.Text = "verifica delle bande..."; });
                        return DipoloMultibanda.Verifica(sp, tok);
                    }, tok);
                    if (tok.IsCancellationRequested) return;
                    DipoloMultibanda.CopiaAccordo(sp, spec); spec.Bande = sp.Bande;
                    esito = r;
                    dmCondiviso = spec.Clone();
                    pb.IsIndeterminate = false; pb.Value = 1;
                    tAv.Text = (accorda ? "Accordo e verifica" : "Verifica") + " finiti in " + Fmt((DateTime.Now - t0).TotalSeconds, 0) + " s" + (spec.Modo == 2 ? string.Format(", linea {0} m", Fmt(spec.Linea, 3)) : "") + ".";
                    mostra(); disegna();
                    Registro.Scrivi("CALCOLO", "dipolo multibanda " + DipoloMultibanda.Modi[spec.Modo] + ": " + (accorda ? "accordo e " : "") + "verifica di " + r.Count + " bande");
                }
                catch (OperationCanceledException) { pb.IsIndeterminate = false; }
                catch (Exception ex) { pb.IsIndeterminate = false; tAv.Text = "Errore NEC: " + ex.Message; }
            };
            bAcc.Click += async (s, e) => await esegui(true);
            bVer.Click += async (s, e) => await esegui(false);
            bApri.Click += (s, e) =>
            {
                try
                {
                    Dictionary<int, string> tb;
                    ApriModelloCalcolatore(DipoloMultibanda.Modello(spec, DipoloMultibanda.Banda(spec.Bande[0]).Operating, out tb).ToNec(), "dipolo multibanda.nec");
                }
                catch (Exception ex) { Status("Dipolo multibanda: " + ex.Message, true); }
            };
            bYagi.Click += async (s, e) =>
            {
                try { await SalvaTestoModello(DipoloMultibanda.EsportaYagi(spec, esito), "radiatore multibanda " + string.Join("-", spec.Bande).Replace(" m", "")); }
                catch (Exception ex) { Status("Dipolo multibanda: " + ex.Message, true); }
            };
            ed.Ricostruisci();
            aggiorna();
            return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }
    }
}
