// YA-NEC 0.4.30 - sottolinguetta dei Calcolatori "Yagi multibanda HF": i radiatori della scheda Dipolo multibanda piu' un parassita per
// banda, riflettore o direttore a scelta banda per banda (IC8TEM 14/11/2026 23:33, 23:36 "si come nuova scheda", 23:37 "sia una
// configurazione riflettori-dipoli, sia una configurazione dipoli-direttori"). Calcolo solo della scheda; "Apri come modello" ed
// "Esporta .nec" a richiesta. Motore: Engine/YagiMultibanda.cs.
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
        Control BuildYagiMbPanel()
        {
            YmSpec y = new YmSpec();
            DipoloMultibanda.Iniziale(y.Rad);
            YagiMultibanda.Iniziale(y);
            CancellationTokenSource cts = null;
            List<YmBanda> esito = null;
            StackPanel root = new StackPanel();
            Grid top = new Grid { ColumnDefinitions = new ColumnDefinitions("*,14,*") };
            StackPanel sx = new StackPanel { Spacing = 3 };
            sx.Children.Add(new TextBlock { Text = "Yagi multibanda", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            sx.Children.Add(Nota("Due elementi per banda: radiatori multibanda (bacchette con trappole e tubi rastremati, manica aperta o radiatori XR-5; bande da 80 a 4 m) e, per ogni banda, un riflettore (dietro) o un direttore (davanti). Lobo verso x positivo."));
            StackPanel tabConf = new StackPanel { Spacing = 2 };
            Action aggiorna = null, costruisciConf = null;
            TextBlock tAv = Nota("");
            // 0.4.30 (IC8TEM 15/11/2026 00:06): radiatori modificabili qui (bande, trappole, rastremazioni), come nel Dipolo multibanda
            EditorRad ed = CreaEditorRadiatori(y.Rad, () => { if (aggiorna != null) aggiorna(); if (costruisciConf != null) costruisciConf(); }, m => tAv.Text = m);
            // 1.0.10 (bozzetto approvato il 16/11/2026 22:36): sezioni 1 Radiatori e 2 Trappole dall'editor, poi 3 Parassiti e 4 Accordo
            sx.Children.Add(ed.Vista);
            StackPanel s3 = new StackPanel { Spacing = 3 };
            StackPanel tutti = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            Button bR = Btn("tutti riflettore-dipolo", false), bD = Btn("tutti dipolo-direttore", false);
            tutti.Children.Add(bR); tutti.Children.Add(bD);
            s3.Children.Add(tabConf); s3.Children.Add(tutti);
            TextBox tDiam = Tb("20", 55), tFb = Tb("20", 55), tPeso = Tb("0,002", 70);
            StackPanel rp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rp.Children.Add(Lbl("ø")); rp.Children.Add(tDiam); rp.Children.Add(Lbl("mm"));
            s3.Children.Add(RigaCalc("Tubo dei parassiti", rp));
            StackPanel s4 = new StackPanel { Spacing = 3 };
            StackPanel ro = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            ro.Children.Add(Lbl("F/B")); ro.Children.Add(tFb); ro.Children.Add(Lbl("dB  peso")); ro.Children.Add(tPeso);
            StackPanel ro2 = new StackPanel { Spacing = 1 };
            ro2.Children.Add(ro);
            ro2.Children.Add(Nota("Somma su tutte le bande di (ROS al centro - 1)^2 piu' peso x (F/B obiettivo - F/B)^2 quando il F/B e' sotto l'obiettivo; peso 0 = solo ROS. " +
                "Partenza: riflettore 0,51 lambda a 0,15 lambda; direttore 0,45 lambda a 0,10 lambda."));
            s4.Children.Add(RigaCalc("Obiettivo", ro2));
            // spaziatura consigliata dei parassiti (IC8TEM 15/11/2026 00:06: "devi anche citare quale sia la migliore spaziatura consigliata:
            // 0,1 0,12 0,2 lambda (fai il mining del manuale arrl, e citi)"); fonti in doc/Spaziatura_parassiti_ARRL.md
            StackPanel rsp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            double[] fisse = { 0, 0.10, 0.12, 0.15, 0.20 };
            rsp.Children.Add(Scelta(new[] { "accordata", "0,10 λ", "0,12 λ", "0,15 λ", "0,20 λ" }, 0, k => { y.PSFissa = fisse[k]; if (aggiorna != null) aggiorna(); }));
            s3.Children.Add(RigaCalc("Distanza", rsp));
            StackPanel spz = new StackPanel { Spacing = 2 };
            spz.Children.Add(Nota("Riflettore: 0,15-0,20 λ (0,2 λ allarga banda e F/B; sotto 0,15 λ la resistenza scende e la banda si stringe). Direttore: 0,10-0,12 λ. " +
                "Cosa dicono le fonti, con similitudini e differenze:"));
            spz.Children.Add(Nota("• ARRL Antenna Book 24ª ed. (2019), cap. 11 §11.3.3: una 2 elementi da' circa 4 dB sul dipolo e un F/R di circa 10 dB; nelle Yagi ottimizzate da 4 elementi in su riflettore, radiatore e primo direttore stanno in 0,15-0,20 λ di boom (W2PV: 0,15 λ costante tra tutti). Parla di Yagi ottimizzate, non di 2 elementi."));
            spz.Children.Add(Nota("• ARRL 19ª ed., cap. 10 (Log-Yag): riflettore a 0,085 λ dietro, direttore a 0,13-0,15 λ davanti; il guadagno quasi non cambia con il riflettore tra 0,08 e 0,25 λ. Concorda sul direttore piu' vicino del riflettore; il radiatore pero' e' una cella log-periodica."));
            spz.Children.Add(Nota("• ARRL 19ª ed., cap. 8 (end-fire): con due elementi alimentati in opposizione il guadagno massimo e' intorno a 0,1 λ, in pratica 1/8 e 1/4 λ; spaziature piu' strette danno poco guadagno in piu' e restringono molto la banda. Riguarda elementi alimentati, non parassiti, ma spiega perche' sotto 0,1 λ non conviene."));
            spz.Children.Add(Nota("• ARRL 19ª ed., cap. 18 (Yagi 2 m di W9PE): tre elementi a 0,2 λ, circa 7 dBi e F/B oltre 15 dB."));
            spz.Children.Add(Nota("• Accordo di YA-NEC con nec2c (bacchette 20/15/10 m): riflettori a 0,185 / 0,197 / 0,222 λ, direttori a 0,095 / 0,116 / 0,114 λ: dentro gli intervalli delle fonti."));
            s3.Children.Add(new Expander { Header = "Spaziatura consigliata (ARRL Antenna Book) e fonti", Content = spz, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 4, 0, 0) });
            sx.Children.Add(SezioneCalc("3 · Parassiti", s3));
            ProgressBar pb = new ProgressBar { Minimum = 0, Maximum = 1, Height = 6, Margin = new Thickness(0, 4) };
            s4.Children.Add(pb); s4.Children.Add(tAv);
            WrapPanel bt = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Left };
            Button bApri = Btn("Apri come modello", false), bEsp = Btn("Esporta .nec", false), bVer = Btn("Verifica", false), bAcc = Btn("Accorda e verifica", true);
            foreach (Button b in new[] { bApri, bEsp, bVer }) b.Margin = new Thickness(0, 6, 8, 0);
            bAcc.Margin = new Thickness(0, 6, 0, 0);
            bt.Children.Add(bApri); bt.Children.Add(bEsp); bt.Children.Add(bVer); bt.Children.Add(bAcc);
            s4.Children.Add(bt);
            sx.Children.Add(SezioneCalc("4 · Accordo", s4));
            top.Children.Add(Scheda(sx));

            StackPanel dx = new StackPanel();
            DipoloMbDisegno schizzo = new DipoloMbDisegno { Height = 380 };
            dx.Children.Add(new Border { Child = schizzo, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ClipToBounds = true, Margin = new Thickness(0, 0, 0, 8) });
            dx.Children.Add(new TextBlock { Text = "Modello NEC (vista 3D)", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 4) });
            Structure3DView v3 = new Structure3DView { Height = 440, ShowPattern = false, ShowGroundAndHeight = false, ShowPhases = false };
            dx.Children.Add(new Border { Child = v3, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ClipToBounds = true });
            TextBlock n3 = Nota("");
            dx.Children.Add(n3);
            Border bdx = Scheda(dx);
            Grid.SetColumn(bdx, 2);
            top.Children.Add(bdx);
            root.Children.Add(top);

            StackPanel pr = new StackPanel();
            pr.Children.Add(new TextBlock { Text = "Risultati per banda", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            Grid gr = new Grid { ColumnDefinitions = new ColumnDefinitions("60,*,140,170,80,80,*") };
            pr.Children.Add(gr);
            pr.Children.Add(Nota("ROS su 50 ohm a inizio / centro / fine banda; guadagno massimo e F/B al centro (" + "orizzonte in spazio libero, 20 gradi di elevazione sul terreno); \"ruba\" come nel Dipolo multibanda."));
            root.Children.Add(Scheda(pr));

            Action mostra = () =>
            {
                gr.Children.Clear(); gr.RowDefinitions.Clear();
                string[] hh = { "banda", "parassita", "Z al centro", "ROS inizio / centro / fine", "guadagno", "F/B", "ruba" };
                gr.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int i = 0; i < hh.Length; i++) { TextBlock t = new TextBlock { Text = hh[i], FontSize = 12, Foreground = Pal.B(Pal.Muted) }; Grid.SetColumn(t, i); gr.Children.Add(t); }
                int riga = 1;
                foreach (string b in DipoloMultibanda.TutteBande(y.Rad))
                {
                    YmBanda d = esito != null ? esito.Find(x => x.Banda == b) : null;
                    string par = !y.PL.ContainsKey(b) ? "trappola (senza parassita)" : (y.Direttore[b] ? "direttore " : "riflettore ") + Fmt(y.PL[b], 3) + " m a " + Fmt(y.PS[b] * DipoloMultibanda.Lambda(b), 3) + " m";
                    string[] v = { b, par, d == null ? "..." : Fmt(d.Z.Real, 1) + (d.Z.Imaginary < 0 ? " -j" : " +j") + Fmt(Math.Abs(d.Z.Imaginary), 1),
                                   d == null ? "..." : Fmt(d.RosLo, 2) + " / " + Fmt(d.RosC, 2) + " / " + Fmt(d.RosHi, 2), d == null ? "..." : Fmt(d.G, 1) + " dBi",
                                   d == null ? "..." : Fmt(d.Fb, 1) + " dB", d == null ? "..." : d.Ruba };
                    gr.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int i = 0; i < v.Length; i++)
                    {
                        TextBlock t = new TextBlock { Text = v[i], FontSize = 13, FontFamily = i < 6 ? Pal.Mono : FontFamily.Default, TextWrapping = TextWrapping.Wrap,
                            Foreground = Pal.B(i == 3 && d != null && d.RosC > 2 || i == 6 && d != null && d.Ruba != "nessuno" ? Pal.Orange : Pal.Fore) };
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
                    NecModel m = YagiMultibanda.Modello(y, DipoloMultibanda.Banda(y.Rad.Bande[0]).Operating, out tb);
                    SegmentGeometry g = m.BuildGeometry();
                    RiempiSchizzo(schizzo, y.Rad, m, tb, y);
                    v3.Geo = g; v3.Ground = y.Rad.Terreno; v3.CurrentNorm = null;
                    v3.SourceSegs = new HashSet<int>();
                    foreach (SourceSpec so in m.Sources) v3.SourceSegs.Add(g.SegNo(so.Tag, so.Seg) - 1);
                    v3.InvalidateVisual();
                    n3.Text = m.Geometry.Count + " fili, " + g.N + " segmenti. Trascina per ruotare, rotella per ingrandire.";
                }
                catch (Exception ex) { n3.Text = "Modello NEC: " + ex.Message; }
            };
            costruisciConf = () =>
            {
                tabConf.Children.Clear();
                foreach (string b0 in y.Rad.Bande)
                {
                    string b = b0;
                    StackPanel r = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                    r.Children.Add(new TextBlock { Text = b, Width = 50, FontWeight = FontWeight.Bold, FontSize = 15, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center });
                    r.Children.Add(Scelta(new[] { "riflettore-dipolo", "dipolo-direttore" }, y.Direttore[b] ? 1 : 0, k => { y.Direttore[b] = k == 1; aggiorna(); }));
                    tabConf.Children.Add(r);
                }
            };
            aggiorna = () =>
            {
                if (cts != null) { cts.Cancel(); cts = null; }
                pb.Value = 0;
                y.Diametro = Math.Max(1, ParseNum(tDiam.Text, 20)) / 1000;
                y.FbObiettivo = ParseNum(tFb.Text, 20); y.Peso = Math.Max(0, ParseNum(tPeso.Text, 0.002));
                ed.Leggi();
                try { DipoloMultibanda.Iniziale(y.Rad); } catch (Exception ex) { tAv.Text = ex.Message; }
                foreach (string b in y.Rad.Bande) if (!y.Direttore.ContainsKey(b)) y.Direttore[b] = false;
                YagiMultibanda.Iniziale(y);
                esito = null;
                tAv.Text = "Misure di partenza: \"Accorda e verifica\" accorda radiatori e parassiti insieme.";
                mostra(); disegna();
            };
            foreach (TextBox t in new[] { tDiam, tFb, tPeso }) t.TextChanged += (s, e) => aggiorna();
            bR.Click += (s, e) => { foreach (string b in y.Rad.Bande) y.Direttore[b] = false; costruisciConf(); aggiorna(); };
            bD.Click += (s, e) => { foreach (string b in y.Rad.Bande) y.Direttore[b] = true; costruisciConf(); aggiorna(); };
            Func<bool, Task> esegui = async accorda =>
            {
                if (cts != null) cts.Cancel();
                cts = new CancellationTokenSource();
                CancellationToken tok = cts.Token;
                YmSpec sp = y.Clone();
                pb.IsIndeterminate = true;
                DateTime t0 = DateTime.Now;
                try
                {
                    List<YmBanda> r = await Task.Run(() =>
                    {
                        if (accorda) YagiMultibanda.Accorda(sp, tok, m => Dispatcher.UIThread.Post(() => { if (!tok.IsCancellationRequested) tAv.Text = m; }));
                        Dispatcher.UIThread.Post(() => { if (!tok.IsCancellationRequested) tAv.Text = "verifica delle bande..."; });
                        return YagiMultibanda.Verifica(sp, tok);
                    }, tok);
                    if (tok.IsCancellationRequested) return;
                    DipoloMultibanda.CopiaAccordo(sp.Rad, y.Rad); y.PL = sp.PL; y.PS = sp.PS;
                    esito = r;
                    pb.IsIndeterminate = false; pb.Value = 1;
                    tAv.Text = (accorda ? "Accordo e verifica" : "Verifica") + " finiti in " + Fmt((DateTime.Now - t0).TotalSeconds, 0) + " s.";
                    mostra(); disegna();
                    Registro.Scrivi("CALCOLO", "Yagi multibanda HF: " + (accorda ? "accordo e " : "") + "verifica di " + r.Count + " bande");
                }
                catch (OperationCanceledException) { pb.IsIndeterminate = false; }
                catch (Exception ex) { pb.IsIndeterminate = false; tAv.Text = "Errore NEC: " + ex.Message; }
            };
            bAcc.Click += async (s, e) => await esegui(true);
            bVer.Click += async (s, e) => await esegui(false);
            bApri.Click += (s, e) =>
            {
                try { ApriModelloCalcolatore(YagiMultibanda.Esporta(y, esito), "yagi multibanda HF.nec"); }
                catch (Exception ex) { Status("Yagi multibanda: " + ex.Message, true); }
            };
            bEsp.Click += async (s, e) =>
            {
                try { await SalvaTestoModello(YagiMultibanda.Esporta(y, esito), "yagi multibanda " + string.Join("-", y.Rad.Bande).Replace(" m", "")); }
                catch (Exception ex) { Status("Yagi multibanda: " + ex.Message, true); }
            };
            foreach (string b in y.Rad.Bande) if (!y.Direttore.ContainsKey(b)) y.Direttore[b] = false;
            ed.Ricostruisci(); aggiorna(); costruisciConf();
            return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }
    }
}
