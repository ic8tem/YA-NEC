// YA-NEC 0.4.30 - sottolinguetta dei Calcolatori "Antenne su PCB" della famiglia "2,4 GHz COMMERCIALE", bozzetto 2 rivisto approvato da
// IC8TEM il 14/11/2026 22:15; immagini approvate alle 22:42 ("si va bene"): disegno quotato nostro per ogni antenna (IFA, MIFA, dipolo
// stampato, Yagi su PCB, Big Wheel), che si aggiorna a ogni dato e si stampa in scala 1:1 come le dime delle centine; accanto la stessa
// antenna nella vista 3D del modello NEC, con il piano di massa fatto a griglia di fili. Calcolo solo della scheda: il modello aperto non
// viene usato ne' toccato ("Apri come modello" a richiesta). Motore: Engine/Pcb24.cs; stampa: UI/Pcb24Pdf.cs.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Control BuildPcb24Panel()
        {
            Pcb24Spec spec = new Pcb24Spec();
            Pcb24.Iniziale(spec);
            Pcb24Esito esito = null;
            CancellationTokenSource cts = null;
            StackPanel root = new StackPanel();

            // ---------------- dati
            StackPanel sx = new StackPanel { Spacing = 3 };
            sx.Children.Add(new TextBlock { Text = "Antenne su PCB - 2,4 GHz", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            sx.Children.Add(Nota("Fonti dei progetti: TI AN043 (IFA, MIFA), DN007, DN034 / SWRA350 (Yagi); WA5VJB Kent Electronics (Yagi e Big Wheel su PCB). I disegni e le misure sono calcolati qui, non copiati."));
            Action aggiorna = null;
            StackPanel rIfa = null, rMifa = null, rDip = null, rYagi = null, rBw = null, rMassa = null;
            TextBox tFmin = Tb("2400", 70), tFmax = Tb("2483,5", 70), tZ0 = Tb("50", 50);
            TextBox tEr = Tb("4,4", 55), tSp = Tb("1,6", 55), tW = Tb("40", 55), tL = Tb("25", 55), tPista = Tb("0,5", 55);
            TextBox tH = Tb("3", 55), tHm = Tb("3", 55), tPm = Tb("2", 55), tGap = Tb("8", 55), tNel = Tb("5", 45), tAp = Tb("100", 55);
            CheckBox cPiano = new CheckBox { Content = "piano di massa vicino", IsChecked = true };
            Func<string, TextBlock> u = Lbl;
            sx.Children.Add(RigaCalc("Tipo", Scelta(new[] { "IFA", "MIFA", "dipolo", "Yagi su PCB", "Big Wheel su PCB" }, 0, k =>
            {
                spec.Tipo = k;
                if (k == 0 && Math.Abs(ParseNum(tH.Text, 3) - 2) < 1e-9) tH.Text = "3";
                if (k == 1 && Math.Abs(ParseNum(tH.Text, 3) - 3) < 1e-9) tH.Text = "2";   // MIFA: la prova con nec2c si adatta meglio con H 2 mm
                aggiorna();
            })));
            StackPanel rb = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rb.Children.Add(tFmin); rb.Children.Add(u("-")); rb.Children.Add(tFmax); rb.Children.Add(u("MHz   Z")); rb.Children.Add(tZ0); rb.Children.Add(u("Ω"));
            sx.Children.Add(RigaCalc("Banda", rb));
            StackPanel rs = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rs.Children.Add(Scelta(new[] { "FR4", "Rogers 4003C", "libero (in aria)", "altro" }, 0, k =>
            {
                Pcb24.Substrato(spec, k);
                if (k != 3) tEr.Text = Fmt(spec.Er, 2);
                aggiorna();
            }));
            sx.Children.Add(RigaCalc("Scheda", rs));
            StackPanel rs2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rs2.Children.Add(u("er")); rs2.Children.Add(tEr); rs2.Children.Add(u("spessore")); rs2.Children.Add(tSp); rs2.Children.Add(u("mm   pista")); rs2.Children.Add(tPista); rs2.Children.Add(u("mm"));
            sx.Children.Add(RigaCalc("", rs2));
            rMassa = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rMassa.Children.Add(tW); rMassa.Children.Add(u("x")); rMassa.Children.Add(tL); rMassa.Children.Add(u("mm (larghezza x altezza)"));
            DockPanel dMassa = RigaCalc("Piano di massa", rMassa);
            sx.Children.Add(dMassa);
            rIfa = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rIfa.Children.Add(tH); rIfa.Children.Add(u("mm sopra il bordo della massa"));
            DockPanel dIfa = RigaCalc("Altezza H", rIfa);
            sx.Children.Add(dIfa);
            rMifa = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rMifa.Children.Add(u("altezza")); rMifa.Children.Add(tHm); rMifa.Children.Add(u("mm   passo")); rMifa.Children.Add(tPm); rMifa.Children.Add(u("mm"));
            DockPanel dMifa = RigaCalc("Meandro", rMifa);
            sx.Children.Add(dMifa);
            rDip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rDip.Children.Add(cPiano); rDip.Children.Add(u("a")); rDip.Children.Add(tGap); rDip.Children.Add(u("mm dal bordo"));
            DockPanel dDip = RigaCalc("Dipolo", rDip);
            sx.Children.Add(dDip);
            rYagi = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rYagi.Children.Add(tNel); rYagi.Children.Add(u("elementi (riflettore, radiatore, direttori)"));
            DockPanel dYagi = RigaCalc("Yagi", rYagi);
            sx.Children.Add(dYagi);
            rBw = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rBw.Children.Add(tAp); rBw.Children.Add(u("gradi di apertura di ogni petalo (tre petali)"));
            DockPanel dBw = RigaCalc("Big Wheel", rBw);
            sx.Children.Add(dBw);
            sx.Children.Add(Nota("Nel NEC-2 la scheda non c'e': si usa la permittivita' efficace (er+1)/2 sulle lunghezze (il NEC risolve la geometria vera a f x √eeff) " +
                "e il piano di massa come griglia di fili (regola dell'area uguale, come NECBOL); la pista larga w e' un filo di raggio w/4. Il risultato e' indicativo: va misurato col VNA."));
            Grid top = new Grid { ColumnDefinitions = new ColumnDefinitions("*,14,*") };
            top.Children.Add(Scheda(sx));

            // ---------------- esito
            StackPanel dx = new StackPanel { Spacing = 3 };
            dx.Children.Add(new TextBlock { Text = "Misure e risultati", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            Grid gMis = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            dx.Children.Add(gMis);
            Grid gRis = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 6, 0, 0) };
            dx.Children.Add(gRis);
            TextBlock tNota = Nota("");
            dx.Children.Add(tNota);
            ProgressBar pb = new ProgressBar { Minimum = 0, Maximum = 1, Height = 6, Margin = new Thickness(0, 4), IsIndeterminate = false };
            TextBlock tAv = Nota("Calcolo della scheda: il NEC calcola solo questa antenna, con i dati di questa scheda; il modello generale aperto non viene usato ne' toccato.");
            dx.Children.Add(pb); dx.Children.Add(tAv);
            WrapPanel bt = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 6, 0, 0) };
            Button bCalc = Btn("Calcola questa antenna", true), bApri = Btn("Apri come modello", false), bPdf = Btn("Disegno in PDF (scala 1:1)", false), bDxf = Btn("DXF per fresare", false);
            CheckBox cSpec = new CheckBox { Content = "anche specchiato", IsChecked = true, Margin = new Thickness(0, 6, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            foreach (Button b in new[] { bApri, bPdf }) b.Margin = new Thickness(0, 6, 8, 0);
            bDxf.Margin = new Thickness(0, 6, 8, 0); bCalc.Margin = new Thickness(0, 6, 0, 0);
            bt.Children.Add(bApri); bt.Children.Add(bPdf); bt.Children.Add(cSpec); bt.Children.Add(bDxf); bt.Children.Add(bCalc);
            dx.Children.Add(bt);
            Grid.SetColumn(dx, 2);
            Border bDx = Scheda(dx);
            Grid.SetColumn(bDx, 2);
            top.Children.Add(bDx);
            root.Children.Add(top);

            // ---------------- disegno quotato e vista 3D, affiancati
            Grid gv = new Grid { ColumnDefinitions = new ColumnDefinitions("*,14,*") };
            StackPanel pd = new StackPanel();
            pd.Children.Add(new TextBlock { Text = "Disegno quotato (mm)", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 4) });
            Pcb24Disegno dis = new Pcb24Disegno { Height = 460 };
            pd.Children.Add(dis);
            pd.Children.Add(Nota("Si aggiorna a ogni dato. Con \"Disegno in PDF\" esce a scala 1:1 (righello di 100 mm per controllare la stampa) e, a richiesta, anche specchiato per il trasferimento del toner sul rame; il DXF ha i contorni delle piste per la fresa."));
            gv.Children.Add(Scheda(pd));
            StackPanel p3 = new StackPanel();
            p3.Children.Add(new TextBlock { Text = "Modello NEC (vista 3D)", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 4) });
            Structure3DView v3 = new Structure3DView { Height = 460, ShowPattern = false, ShowGroundAndHeight = false, ShowPhases = false };
            p3.Children.Add(new Border { Child = v3, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ClipToBounds = true });
            TextBlock n3 = Nota("");
            p3.Children.Add(n3);
            Border b3 = Scheda(p3);
            Grid.SetColumn(b3, 2);
            gv.Children.Add(b3);
            root.Children.Add(gv);

            // ---------------- proposte tipiche
            StackPanel pt = new StackPanel();
            pt.Children.Add(new TextBlock { Text = "Proposte tipiche a 2,4 GHz", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            pt.Children.Add(Nota("guadagni tipici di letteratura, indicativi: per le antenne di questa scheda il guadagno lo calcola il NEC"));
            string[][] prop =
            {
                new[] { "antenna", "guadagno", "uso" },
                new[] { "IFA / MIFA su PCB", "1-2 dBi (TI)", "moduli, sensori" },
                new[] { "dipolo stampato", "circa 2 dBi", "schede con alimentazione bilanciata" },
                new[] { "Yagi su PCB", "dal NEC", "direttiva compatta" },
                new[] { "Big Wheel su PCB (WA5VJB)", "dal NEC", "omnidirezionale, polarizzazione orizzontale" },
                new[] { "patch FR4 / in aria", "6-9 dBi", "punto-punto" },
                new[] { "biquad con riflettore", "10-11 dBi", "WiFi a distanza" },
                new[] { "cantenna Ø 90 mm", "9-11 dBi", "esperimenti" },
                new[] { "Yagi 16 elementi", "13-15 dBi", "collegamenti" },
                new[] { "collineare 4-8 elementi", "5-8 dBi", "access point" },
            };
            Grid gp = new Grid { ColumnDefinitions = new ColumnDefinitions("240,140,*") };
            for (int i = 0; i < prop.Length; i++)
            {
                gp.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int j = 0; j < 3; j++)
                {
                    TextBlock t = new TextBlock { Text = prop[i][j], FontSize = 13, Foreground = Pal.B(i == 0 ? Pal.Muted : Pal.Fore), Margin = new Thickness(0, 1) };
                    Grid.SetRow(t, i); Grid.SetColumn(t, j); gp.Children.Add(t);
                }
            }
            pt.Children.Add(gp);
            root.Children.Add(Scheda(pt));

            // ---------------- logica
            Action<Grid, List<string[]>> riempi = (g, righe) =>
            {
                g.Children.Clear(); g.RowDefinitions.Clear();
                for (int i = 0; i < righe.Count; i++)
                {
                    g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    TextBlock a = new TextBlock { Text = righe[i][0], FontSize = 13, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 1) };
                    TextBlock b = new TextBlock { Text = righe[i][1], FontSize = 13, FontFamily = Pal.Mono, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(12, 1, 0, 1) };
                    Grid.SetRow(a, i); Grid.SetRow(b, i); Grid.SetColumn(b, 1);
                    g.Children.Add(a); g.Children.Add(b);
                }
            };
            Action mostra = () =>
            {
                riempi(gMis, Pcb24.Misure(spec));
                double[] ing = Pcb24.Ingombro(spec);
                List<string[]> r = new List<string[]>();
                r.Add(new[] { "Ingombro dell'antenna", Fmt(ing[0], 1) + " x " + Fmt(ing[1], 1) + " mm" });
                if (esito == null)
                {
                    r.Add(new[] { "Misure", spec.Accordata ? "accordate" : "di partenza (da calcolare)" });
                    r.Add(new[] { "ROS (NEC) inizio / centro / fine", "da calcolare" });
                    r.Add(new[] { "Guadagno massimo", "da calcolare" });
                    r.Add(new[] { "Banda a ROS 2", "da calcolare" });
                    tNota.Text = "";
                }
                else
                {
                    r.Add(new[] { "Z al centro banda", Fmt(esito.Zc.Real, 1) + (esito.Zc.Imaginary < 0 ? " -j" : " +j") + Fmt(Math.Abs(esito.Zc.Imaginary), 1) + " Ω" });
                    r.Add(new[] { "ROS (NEC) " + Fmt(spec.FMin, 0) + " / " + Fmt(Pcb24.Fc(spec), 0) + " / " + Fmt(spec.FMax, 0) + " MHz", Fmt(esito.RosMin, 2) + " / " + Fmt(esito.RosC, 2) + " / " + Fmt(esito.RosMax, 2) });
                    r.Add(new[] { "Guadagno massimo", Fmt(esito.Gmax, 1) + " dBi" });
                    r.Add(new[] { "Banda a ROS 2", double.IsNaN(esito.F2a) ? "ROS sopra 2 al centro" : Fmt(esito.F2a, 0) + " - " + Fmt(esito.F2b, 0) + " MHz (" + Fmt(esito.F2b - esito.F2a, 0) + " MHz)" });
                    tNota.Text = esito.Nota;
                }
                riempi(gRis, r);
            };
            Action disegna = () =>
            {
                dis.Prim = Pcb24.Disegno(spec);
                dis.Legenda = "rame: pista e piano di massa · punto rosso: alimentazione · cerchio: corto a massa / foro · pista " + Fmt(spec.Pista, 2) + " mm";
                dis.InvalidateVisual();
                try
                {
                    NecModel m = Pcb24.Modello(spec, false);
                    SegmentGeometry g = m.BuildGeometry();
                    v3.Geo = g;
                    v3.Ground = false;
                    v3.SourceSegs = new HashSet<int>();
                    foreach (SourceSpec so in m.Sources) v3.SourceSegs.Add(g.SegNo(so.Tag, so.Seg) - 1);
                    v3.CurrentNorm = esito != null && esito.Correnti != null && esito.Correnti.Length == g.N ? esito.Correnti : null;
                    v3.InvalidateVisual();
                    n3.Text = m.Geometry.Count + " fili, " + g.N + " segmenti" + (Pcb24.HaMassa(spec) ? " (piano di massa a griglia di fili)" : "") + (esito != null ? "; colori = corrente al centro banda" : "") +
                        ". Trascina per ruotare, rotella per ingrandire.";
                }
                catch (Exception ex) { n3.Text = "Modello NEC: " + ex.Message; }
            };
            aggiorna = () =>
            {
                if (cts != null) { cts.Cancel(); cts = null; pb.Value = 0; }
                spec.FMin = ParseNum(tFmin.Text, 2400); spec.FMax = Math.Max(spec.FMin + 1, ParseNum(tFmax.Text, 2483.5));
                spec.Z0 = Math.Max(1, ParseNum(tZ0.Text, 50));
                if (spec.Substrato == 3) spec.Er = Math.Max(1, ParseNum(tEr.Text, 4.4));
                tEr.IsEnabled = spec.Substrato == 3;
                tSp.IsEnabled = spec.Substrato != 2;
                spec.Spessore = Math.Max(0.1, ParseNum(tSp.Text, 1.6));
                spec.PianoW = Math.Max(5, ParseNum(tW.Text, 40)); spec.PianoL = Math.Max(3, ParseNum(tL.Text, 25));
                spec.Pista = Math.Max(0.1, ParseNum(tPista.Text, 0.5));
                spec.H = Math.Max(0.5, ParseNum(tH.Text, 3));
                spec.MeandroH = Math.Max(0.5, ParseNum(tHm.Text, 3)); spec.MeandroPasso = Math.Max(0.5, ParseNum(tPm.Text, 2));
                spec.PianoDipolo = cPiano.IsChecked == true; spec.GapDipolo = Math.Max(1, ParseNum(tGap.Text, 8));
                spec.ElementiYagi = Math.Max(3, Math.Min(12, (int)Math.Round(ParseNum(tNel.Text, 5))));
                spec.AperturaBw = Math.Max(40, Math.Min(110, ParseNum(tAp.Text, 100)));
                dMassa.IsVisible = Pcb24.HaMassa(spec);
                dIfa.IsVisible = spec.Tipo <= 1; dMifa.IsVisible = spec.Tipo == 1; dDip.IsVisible = spec.Tipo == 2; dYagi.IsVisible = spec.Tipo == 3; dBw.IsVisible = spec.Tipo == 4;
                Pcb24.Iniziale(spec);
                esito = null;
                tAv.Text = "Misure di partenza: \"Calcola questa antenna\" accorda col NEC a " + Fmt(spec.Z0, 0) + " Ω e calcola ROS e guadagno.";
                mostra(); disegna();
            };
            foreach (TextBox t in new[] { tFmin, tFmax, tZ0, tEr, tSp, tW, tL, tPista, tH, tHm, tPm, tGap, tNel, tAp }) t.TextChanged += (s, e) => aggiorna();
            cPiano.IsCheckedChanged += (s, e) => aggiorna();

            bCalc.Click += async (s, e) =>
            {
                if (cts != null) cts.Cancel();
                cts = new CancellationTokenSource();
                CancellationToken tok = cts.Token;
                Pcb24Spec sp = spec.Clone();
                pb.IsIndeterminate = true;
                tAv.Text = "accordo in corso...";
                DateTime t0 = DateTime.Now;
                try
                {
                    Pcb24Esito es = await Task.Run(() =>
                    {
                        Pcb24.Accorda(sp, tok, m => Dispatcher.UIThread.Post(() => { if (!tok.IsCancellationRequested) tAv.Text = m; }));
                        return Pcb24.Esito(sp, tok, m => Dispatcher.UIThread.Post(() => { if (!tok.IsCancellationRequested) tAv.Text = m; }));
                    }, tok);
                    if (tok.IsCancellationRequested) return;
                    spec.L = sp.L; spec.S = sp.S; spec.Accordata = true;
                    esito = es;
                    pb.IsIndeterminate = false; pb.Value = 1;
                    tAv.Text = "Calcolo finito in " + Fmt((DateTime.Now - t0).TotalSeconds, 1) + " s: misure accordate a " + Fmt(spec.Z0, 0) + " +j0 Ω al centro banda (disegno aggiornato).";
                    mostra(); disegna();
                    Registro.Scrivi("CALCOLO", "antenna su PCB 2,4 GHz: " + Pcb24.Tipi[spec.Tipo] + ", L " + Fmt(spec.L, 2) + " mm, S " + Fmt(spec.S, 2) + " mm, ROS " + Fmt(es.RosC, 2));
                }
                catch (OperationCanceledException) { pb.IsIndeterminate = false; }
                catch (Exception ex) { pb.IsIndeterminate = false; tAv.Text = "Errore NEC: " + ex.Message; }
            };
            bApri.Click += (s, e) =>
            {
                try { ApriModelloCalcolatore(Pcb24.Modello(spec, true).ToNec(), Pcb24.Tipi[spec.Tipo].Replace(" ", "_") + "_2400.nec"); }
                catch (Exception ex) { Status("Antenna su PCB: " + ex.Message, true); }
            };
            Func<string, string, Action<Stream>, Task> salva = async (nome, est, scrivi) =>
            {
                try
                {
                    IStorageFile f = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Salva", SuggestedFileName = nome, DefaultExtension = est });
                    if (f == null) return;
                    using (Stream st = await f.OpenWriteAsync()) scrivi(st);
                    Status("Salvato: " + f.Name);
                }
                catch (Exception ex) { Status("Salvataggio non riuscito: " + ex.Message, true); }
            };
            bPdf.Click += async (s, e) =>
            {
                Pcb24Spec sp = spec.Clone(); Pcb24Esito es = esito; bool sp2 = cSpec.IsChecked == true;
                await salva(Pcb24.Tipi[sp.Tipo].Replace(" ", "_") + "_2400_1a1.pdf", "pdf", st => Pcb24Pdf.Scrivi(st, sp, es, sp2));
            };
            bDxf.Click += async (s, e) =>
            {
                Pcb24Spec sp = spec.Clone(); bool sp2 = cSpec.IsChecked == true;
                await salva(Pcb24.Tipi[sp.Tipo].Replace(" ", "_") + "_2400" + (sp2 ? "_specchiato" : "") + ".dxf", "dxf", st =>
                {
                    byte[] b = Encoding.ASCII.GetBytes(Pcb24.Dxf(sp, sp2));
                    st.Write(b, 0, b.Length);
                });
            };
            aggiorna();
            return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }
    }
}
