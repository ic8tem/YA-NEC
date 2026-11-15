// YA-NEC 0.4.30 - sottolinguetta dei Calcolatori "Verticale multibanda (DX Commander)", bozzetto 10 rivisto approvato da IC8TEM il
// 14/11/2026 22:41: modello dal file (dxcommander_A) o nuovo, tabella banda / elemento / lunghezza / distanza dal palo / attivo,
// terreno della scheda (come la finestra o suo, sempre Sommerfeld), radiali N x L alla quota; calcolo in background indipendente dal
// modello aperto (il modello generale non viene toccato), verifica per banda; "Apri come modello" e "Verifica tutte le bande".
// Motore: Engine/VerticaleMultibanda.cs.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        Control BuildVerticaleMbPanel()
        {
            VmSpec spec = new VmSpec();
            string fFile = Path.Combine(AppPaths.DataDir, "archivio", "Verticali multibanda", "dxcommander_A.nec");
            try { if (YanecFile.Esiste(fFile)) spec.File = NecModel.Load(fFile); } catch { }
            spec.DalFile = spec.File != null;
            spec.El = spec.DalFile ? VerticaleMultibanda.ElementiDalFile(spec.File) : VerticaleMultibanda.ElementiNuovi();
            CancellationTokenSource cts = null;

            Grid top = new Grid { ColumnDefinitions = new ColumnDefinitions("*,14,*") };
            VerticaleDisegno dis = new VerticaleDisegno { Height = 640 };
            top.Children.Add(Scheda(dis));
            StackPanel dx = new StackPanel { Spacing = 5 };
            StackPanel tit = new StackPanel();
            tit.Children.Add(new TextBlock { Text = "Verticale multibanda tipo DX Commander", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            tit.Children.Add(new TextBlock { Text = "(linguetta sperimentale di verifica, richiesta del 12/11/2026)", FontSize = 12.5, Foreground = Pal.B(Pal.Muted) });
            dx.Children.Add(tit);
            StackPanel tab = new StackPanel();
            Action ridisegna = null, ricostruisciTabella = null;
            dx.Children.Add(RigaCalc("Modello", Scelta(new[] { "DX Commander A (modello in archivio)", "nuovo" }, spec.DalFile ? 0 : 1, k =>
            {
                spec.DalFile = k == 0 && spec.File != null;
                spec.El = spec.DalFile ? VerticaleMultibanda.ElementiDalFile(spec.File) : VerticaleMultibanda.ElementiNuovi();
                ricostruisciTabella(); ridisegna();
            })));
            dx.Children.Add(tab);
            // terreno della scheda
            string[] nomiT = new string[Grounds.Length + 2];
            nomiT[0] = "Perfetto";
            for (int i = 0; i < Grounds.Length; i++) nomiT[i + 1] = Grounds[i].Item1;
            nomiT[nomiT.Length - 1] = "Personalizzato";
            ComboBox cT = new ComboBox { ItemsSource = nomiT, SelectedIndex = 1, MinWidth = 170 };
            TextBox tEr = Tb("13", 60), tSig = Tb("0,005", 70);
            int modoT = 1;
            // 0.5.3 (IC8TEM 15/11/2026 19:51 "qua ci sono problemi di visualizzazione"): le righe vanno a capo dentro la scheda
            WrapPanel rt = new WrapPanel { Orientation = Orientation.Horizontal };
            Control sceltaT = Scelta(new[] { "come la finestra", "suo" }, 1, k => { modoT = k; ridisegna(); });
            StackPanel rtEr = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rtEr.Children.Add(Lbl("er")); rtEr.Children.Add(tEr); rtEr.Children.Add(Lbl("σ")); rtEr.Children.Add(tSig);
            foreach (Control c in new Control[] { sceltaT, cT, rtEr }) { c.Margin = new Thickness(0, 2, 8, 2); rt.Children.Add(c); }
            dx.Children.Add(RigaCalc("Terreno di questa scheda", rt));
            dx.Children.Add(Nota("elenco completo: perfetto, terreni tipici (sabbia secca ... acqua di mare), personalizzato; sempre Sommerfeld"));
            TextBox tNr = Tb("32", 50), tLr = Tb("5", 50), tQ = Tb("0,05", 60);
            StackPanel rr = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rr.Children.Add(tNr); rr.Children.Add(Lbl("x")); rr.Children.Add(tLr); rr.Children.Add(Lbl("m  quota")); rr.Children.Add(tQ); rr.Children.Add(Lbl("m"));
            dx.Children.Add(RigaCalc("Radiali", rr));
            dx.Children.Add(Nota("radiali: numero x lunghezza, alla quota scritta (negativa = interrati, 0 = sul terreno, positiva = sopraelevati)"));
            // pulsanti subito sotto i dati (prima restavano tagliati in fondo alla scheda)
            WrapPanel bt = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
            Button bApri = Btn("Apri come modello", false), bVer = Btn("Verifica tutte le bande", true);
            bApri.Margin = new Thickness(0, 6, 8, 0); bVer.Margin = new Thickness(0, 6, 0, 0);
            bt.Children.Add(bApri); bt.Children.Add(bVer);
            dx.Children.Add(bt);
            // calcolo in background
            ProgressBar pb = new ProgressBar { Minimum = 0, Maximum = 1, Height = 8, Margin = new Thickness(0, 4) };
            TextBlock tAv = Nota("");
            StackPanel bg = new StackPanel();
            bg.Children.Add(new TextBlock { Text = "Calcolo in background", FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            bg.Children.Add(Nota("indipendente dal modello aperto: usa solo i dati di questa scheda e il suo terreno; il modello generale non viene toccato"));
            bg.Children.Add(pb); bg.Children.Add(tAv);
            dx.Children.Add(GeoBox(bg));
            Grid ver = new Grid { ColumnDefinitions = new ColumnDefinitions("0.8*,0.9*,0.9*,1.1*,0.9*,1.6*") };
            StackPanel vb = new StackPanel();
            vb.Children.Add(new TextBlock { Text = "Verifica per banda", FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            vb.Children.Add(Nota("uno sweep di 41 punti per banda (banda +-3 %), con tutti gli elementi presenti: l'interazione tra gli elementi e' nel calcolo"));
            vb.Children.Add(ver);
            TextBlock vNota = Nota("Segnala le bande dove un elemento di un'altra banda prende corrente (come la nota 18 del manuale per le Yagi multibanda): accorciare o allungare un elemento per volta.");
            vb.Children.Add(vNota);
            dx.Children.Add(GeoBox(vb));
            Grid.SetColumn(dx, 2);
            top.Children.Add(Scheda(dx));

            Func<VmSpec> leggi = () =>
            {
                VmSpec s = spec;
                if (modoT == 0 && model != null) s.Terreno = model.Ground.Clone();
                else
                {
                    int t = cT.SelectedIndex;
                    if (t == 0) s.Terreno = new GroundSpec { Kind = GroundKind.Perfect };
                    else
                    {
                        double er = ParseNum(tEr.Text, 13), sg = ParseNum(tSig.Text, 0.005);
                        if (t >= 1 && t <= Grounds.Length) { er = Grounds[t - 1].Item2; sg = Grounds[t - 1].Item3; }
                        s.Terreno = new GroundSpec { Kind = GroundKind.Real, Epsr = er, Sigma = sg, Sommerfeld = true };
                    }
                }
                if (s.Terreno.Kind == GroundKind.Real) s.Terreno.Sommerfeld = true;
                s.Radiali = Math.Max(0, (int)Math.Round(ParseNum(tNr.Text, 32)));
                s.LRad = Math.Max(0.1, ParseNum(tLr.Text, 5));
                s.Quota = ParseNum(tQ.Text, 0.05);
                return s;
            };
            ridisegna = () =>
            {
                VmSpec s = leggi();
                tEr.IsEnabled = tSig.IsEnabled = cT.SelectedIndex == nomiT.Length - 1 && modoT == 1;
                if (cT.SelectedIndex >= 1 && cT.SelectedIndex <= Grounds.Length) { tEr.Text = Fmt(Grounds[cT.SelectedIndex - 1].Item2, 1); tSig.Text = Fmt(Grounds[cT.SelectedIndex - 1].Item3, 4); }
                dis.Elementi = s.El; dis.AltezzaPalo = s.AltezzaPalo; dis.LRad = s.LRad; dis.Radiali = s.Radiali; dis.Terreno = s.Terreno.Kind != GroundKind.FreeSpace;
                dis.InvalidateVisual();
            };
            ricostruisciTabella = () =>
            {
                tab.Children.Clear();
                string cols = "0.8*,0.9*,1.6*,1.6*,0.6*";
                Grid h = new Grid { ColumnDefinitions = new ColumnDefinitions(cols) };
                string[] hh = { "banda", "elemento", "lunghezza (m)", "distanza dal palo (m)", "attivo" };
                for (int i = 0; i < hh.Length; i++) { TextBlock t = new TextBlock { Text = hh[i], Foreground = Pal.B(Pal.Muted), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 6, 0) }; Grid.SetColumn(t, i); h.Children.Add(t); }
                tab.Children.Add(h);
                foreach (VmElemento e in spec.El)
                {
                    VmElemento el = e;
                    Grid r = new Grid { ColumnDefinitions = new ColumnDefinitions(cols), Margin = new Thickness(0, 1) };
                    Control[] cs = new Control[5];
                    cs[0] = new TextBlock { Text = e.Banda, FontFamily = Pal.Mono, VerticalAlignment = VerticalAlignment.Center };
                    cs[1] = new TextBlock { Text = e.Palo ? "palo" : "filo", VerticalAlignment = VerticalAlignment.Center };
                    if (spec.DalFile)
                    {
                        cs[2] = new TextBlock { Text = Fmt(e.L, 2) + " (dal modello)", FontFamily = Pal.Mono, VerticalAlignment = VerticalAlignment.Center };
                        cs[3] = new TextBlock { Text = e.Palo ? "-" : Fmt(e.Dist, 2) + " (dal modello)", FontFamily = Pal.Mono, VerticalAlignment = VerticalAlignment.Center };
                    }
                    else
                    {
                        TextBox tl = Tb(Fmt(e.L, 3), 80), td = Tb(Fmt(e.Dist, 3), 80);
                        tl.TextChanged += (s, ev) => { el.L = Math.Max(0.1, ParseNum(tl.Text, el.L)); ridisegna(); };
                        td.TextChanged += (s, ev) => { el.Dist = Math.Max(0, ParseNum(td.Text, el.Dist)); };
                        cs[2] = tl; cs[3] = e.Palo ? (Control)new TextBlock { Text = "-" } : td;
                    }
                    CheckBox ca = new CheckBox { IsChecked = e.Attivo };
                    ca.IsCheckedChanged += (s, ev) => { el.Attivo = ca.IsChecked == true; ridisegna(); };
                    cs[4] = ca;
                    for (int i = 0; i < 5; i++) { Grid.SetColumn(cs[i], i); r.Children.Add(cs[i]); }
                    tab.Children.Add(new Border { Child = r, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) });
                }
            };
            Action<List<VmBanda>> mostraVer = lista =>
            {
                ver.Children.Clear(); ver.RowDefinitions.Clear();
                string[] hh = { "banda", "ROS min", "a MHz", "guadagno a 10°", "efficienza", "elemento che \"ruba\"" };
                ver.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int i = 0; i < hh.Length; i++) { TextBlock t = new TextBlock { Text = hh[i], Foreground = Pal.B(Pal.Muted), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 6, 2) }; Grid.SetColumn(t, i); ver.Children.Add(t); }
                int riga = 1;
                foreach (VmBanda b in lista)
                {
                    ver.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    bool fatto = !double.IsNaN(b.Eff);
                    string[] v = { b.Banda, double.IsNaN(b.RosMin) ? "..." : Fmt(b.RosMin, 2), double.IsNaN(b.RosMin) ? "..." : Fmt(b.FRos, 3), fatto ? Fmt(b.G10, 2) + " dBi" : "...", fatto ? Fmt(b.Eff, 0) + " %" : "...", fatto ? b.Ruba : "..." };
                    for (int i = 0; i < v.Length; i++)
                    {
                        TextBlock t = new TextBlock { Text = v[i], FontFamily = i < 5 ? Pal.Mono : FontFamily.Default, FontSize = 13, Foreground = Pal.B(i == 5 && fatto && b.Ruba != "nessuno" ? Pal.Orange : Pal.Fore), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 6, 1) };
                        Grid.SetRow(t, riga); Grid.SetColumn(t, i); ver.Children.Add(t);
                    }
                    riga++;
                }
            };
            foreach (TextBox t in new[] { tEr, tSig, tNr, tLr, tQ }) t.TextChanged += (s, e) => ridisegna();
            cT.SelectionChanged += (s, e) => ridisegna();
            bApri.Click += (s, e) =>
            {
                try
                {
                    VmSpec sp = leggi();
                    VmElemento primo = sp.El.Find(x => x.Attivo);
                    Band b = primo != null ? Bands.All.Find(x => x.Name == primo.Banda) : null;
                    List<string> note = new List<string>();
                    NecModel m = VerticaleMultibanda.Modello(sp, b != null ? b.Operating : 7.1, note);
                    ApriModelloCalcolatore(m.ToNec(), "verticale multibanda.nec");
                    foreach (string n in note) Status(n);
                }
                catch (Exception ex) { Status("Verticale multibanda: " + ex.Message, true); }
            };
            bVer.Click += async (s, e) =>
            {
                if (cts != null) cts.Cancel();
                cts = new CancellationTokenSource();
                CancellationToken tok = cts.Token;
                VmSpec sp = leggi();
                List<VmElemento> att = sp.El.FindAll(x => x.Attivo && Bands.All.Exists(b => b.Name == x.Banda));
                List<VmBanda> lista = new List<VmBanda>();
                foreach (VmElemento x in att) lista.Add(new VmBanda { Banda = x.Banda });
                mostraVer(lista);
                pb.Value = 0;
                try
                {
                    List<string> note = new List<string>();
                    VerticaleMultibanda.Modello(sp, 7.1, note);   // costruisce i fili (etichette degli elementi) e gli avvisi
                    if (note.Count > 0) tAv.Text = string.Join(" ", note);
                    for (int i = 0; i < att.Count; i++)
                    {
                        int ii = i;
                        VmBanda r = await Task.Run(() => VerticaleMultibanda.Verifica(sp, att[ii], av => Dispatcher.UIThread.Post(() =>
                        {
                            pb.Value = (ii + av.Fatti / (double)av.Totale) / att.Count;
                            tAv.Text = att[ii].Banda + ": " + av.Fatti + " punti su " + av.Totale + (ii + 1 < att.Count ? " · poi " + att[ii + 1].Banda : "") + " (si puo' continuare a lavorare nelle altre linguette)";
                        }), tok));
                        lista[i] = r;
                        mostraVer(lista);
                    }
                    pb.Value = 1;
                    tAv.Text = "Verifica finita: " + att.Count + " bande.";
                    Registro.Scrivi("CALCOLO", "verticale multibanda: verifica di " + att.Count + " bande");
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { tAv.Text = "Errore: " + ex.Message; }
            };
            ricostruisciTabella(); ridisegna();
            mostraVer(new List<VmBanda>());
            return new ScrollViewer { Content = top, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }
    }
}
