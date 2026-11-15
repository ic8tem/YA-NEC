// YA-NEC - linguetta "Calcolatori" con il fascione di sinistra (stile approvato alla lettera).
// LPDA (bozzetto approvato alla lettera: metodo Carrel, Balanis 11.4.3, con la linea incrociata nel modello NEC) e i
// calcolatori delle antenne semplici (dipolo, V invertita, ground plane, delta loop, four-square, loop magnetica,
// distanze di stack DL6WU). Le voci in grigio sono previste dai bozzetti approvati ma non ancora attive.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
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

        // carica nel programma il modello NEC generato da un calcolatore
        void ApriModelloCalcolatore(string nec, string nome)
        {
            try
            {
                NecModel m = NecModel.Parse(nec);
                m.FileName = nome;
                SetModel(m, null);
                int ti = -1;
                for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == "Struttura 3D") ti = i;
                if (ti >= 0) SelectTab(ti);
                Status("Modello del calcolatore aperto: " + nome);
                CalcolaAllApertura();
            }
            catch (Exception ex) { Status("Errore nel modello del calcolatore: " + ex.Message, true); }
        }

        // ------------------------------------------------------------------------------------------------
        // LPDA (bozzetto approvato alla lettera)
        // ------------------------------------------------------------------------------------------------
        LpdaDesign lpda;
        Action lpdaPrimoCalcolo;
        int lpdaGround;          // 0 spazio libero, 1 terreno medio all'altezza data

        Control BuildLpdaPanel()
        {
            lpda = new LpdaDesign { Fmin = 144, Fmax = 432, Tau = 0.90, Sigma = 0.168, Rin = 50, DiamMm = 6, LineTubeMm = 10 };
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*") };
            StackPanel left = new StackPanel(), right = new StackPanel();

            // --- impostazione del calcolo ---
            StackPanel set = new StackPanel { Spacing = 2 };
            TextBox tf1 = Tb("144", 70), tf2 = Tb("432", 70);
            TextBlock lb = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) };
            set.Children.Add(Riga("Banda", tf1, new TextBlock { Text = "-", VerticalAlignment = VerticalAlignment.Center }, tf2, new TextBlock { Text = "MHz", VerticalAlignment = VerticalAlignment.Center }, lb));
            int parte = 0;
            TextBox tg = Tb("8,9", 60);
            tg.IsVisible = false;
            TextBox tt = Tb("0,900", 80), ts = Tb("0,168", 80), tr = Tb("50", 60), td = Tb("6", 60), tl = Tb("10", 60), tstub = Tb("", 70);
            StackPanel sParte = Scelta(new[] { "tau e sigma", "guadagno voluto" }, 0, k => { parte = k; tg.IsVisible = k == 1; });
            set.Children.Add(Riga("Parte da", sParte, tg, new TextBlock { Text = "", VerticalAlignment = VerticalAlignment.Center }));
            ToolTip.SetTip(tg, "Guadagno voluto in dBi: tau si ricava lungo sigma ottimo (stima tarata sull'esempio di Balanis, 8 dB -> tau 0,865); il valore vero lo da' il NEC.");
            set.Children.Add(Riga("Fattore di scala tau", tt));
            Button bso = Btn("sigma ottimo = 0,243 tau - 0,051", false);
            set.Children.Add(Riga("Spaziatura relativa sigma", ts, bso));
            set.Children.Add(Riga("Impedenza voluta Rin", tr, new TextBlock { Text = "ohm", VerticalAlignment = VerticalAlignment.Center }));
            int dmode = 0;
            set.Children.Add(Riga("Diametro elementi", td, new TextBlock { Text = "mm", VerticalAlignment = VerticalAlignment.Center },
                Scelta(new[] { "uguale", "3 gruppi", "l/d costante" }, 0, k => { dmode = k; })));
            // 0.4.30 (IC8TEM 14/11/2026 23:40): elementi a tubi telescopici per le LPDA HF
            CheckBox cRas = new CheckBox { Content = "Tubi rastremati (HF)", IsChecked = false, VerticalAlignment = VerticalAlignment.Center };
            TextBox tRas = Tb(RastremazioneHf.Predefinita, 250);
            ToolTip.SetTip(tRas, "Diametro in mm x lunghezza in m per lato dal centro; l'ultimo tubo arriva alla punta. Al posto dei diametri sopra, per gli elementi lunghi almeno 1 m; correzione di Leeson accesa.");
            set.Children.Add(Riga("Rastremazione", cRas, tRas));
            TextBlock lline = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore) };
            set.Children.Add(Riga("Linea di alimentazione (2 tubi)", new TextBlock { Text = "tubi", VerticalAlignment = VerticalAlignment.Center }, tl, lline));
            set.Children.Add(Riga("Chiusura dietro l'elemento piu' lungo", new TextBlock { Text = "stub in corto", VerticalAlignment = VerticalAlignment.Center }, tstub, new TextBlock { Text = "mm (lambda max / 8)", VerticalAlignment = VerticalAlignment.Center }));
            TextBlock deriv = Nota("");
            set.Children.Add(deriv);
            left.Children.Add(Pannello("Impostazione del calcolo (metodo Carrel, Balanis 11.4.3)", set));

            // --- misure ---
            StackPanel mis = new StackPanel { Spacing = 1 };
            StackPanel misBody = new StackPanel();
            mis.Children.Add(misBody);
            StackPanel mb = new StackPanel { Orientation = Orientation.Horizontal };
            Button bRip = Btn("Riporta ai calcolati", false), bOtt = Btn("Ottimizza...", false), bApri = Btn("Apri come modello", true), bExp = Btn("Esporta tabella", false);
            bOtt.IsEnabled = false;
            ToolTip.SetTip(bOtt, "Ottimizzatore (fase 5, bozzetto approvato): prossima versione.");
            mb.Children.Add(bRip); mb.Children.Add(bOtt); mb.Children.Add(bApri); mb.Children.Add(bExp);
            mis.Children.Add(mb);
            left.Children.Add(Pannello("Misure (mm)", mis, "\"Tua\" in arancione se diversa"));

            // --- risultati NEC ---
            StackPanel nr = new StackPanel { Spacing = 2 };
            TextBox th = Tb("10", 50);
            StackPanel gsel = Scelta(new[] { "spazio libero", "terreno medio a h (m)" }, 0, k => { lpdaGround = k; });
            StackPanel gl = new StackPanel { Orientation = Orientation.Horizontal };
            gl.Children.Add(gsel); gl.Children.Add(th);
            Button bNec = Btn("Calcola con il NEC", true);
            gl.Children.Add(bNec);
            nr.Children.Add(gl);
            Grid tab = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,*,*,1.4*") };
            nr.Children.Add(tab);
            right.Children.Add(Pannello("Risultati NEC", nr));
            LineChart chS = new LineChart { Title = "ROS", Height = 130, XLabel = "MHz" };
            LineChart chG = new LineChart { Title = "Guadagno dBi", Height = 130, XLabel = "MHz" };
            LineChart chF = new LineChart { Title = "F/B dB", Height = 130, XLabel = "MHz" };
            StackPanel ch = new StackPanel { Spacing = 4 };
            ch.Children.Add(chS); ch.Children.Add(chG); ch.Children.Add(chF);
            right.Children.Add(Pannello("ROS, guadagno e F/B in banda", ch));
            Grid pol = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Height = 300 };
            PolarPlot p1 = new PolarPlot { ArrlScale = false, LinRange = 30, Rotate = 90 }, p2 = new PolarPlot { ArrlScale = false, LinRange = 30, Rotate = 90 };
            Grid.SetColumn(p2, 1);
            pol.Children.Add(p1); pol.Children.Add(p2);
            StackPanel polBox = new StackPanel();
            polBox.Children.Add(pol);
            StackPanel lpWarn = new StackPanel();
            polBox.Children.Add(lpWarn);
            right.Children.Add(Pannello("Diagrammi agli estremi della banda", polBox, "orizzontale (piano E) pieno, verticale (piano H) tratteggiato, avanti in alto"));

            Action leggi = () =>
            {
                lpda.Fmin = ParseNum(tf1.Text, lpda.Fmin); lpda.Fmax = ParseNum(tf2.Text, lpda.Fmax);
                if (parte == 1)
                {
                    lpda.Tau = LpdaDesign.TauFromGain(ParseNum(tg.Text, 8.9));
                    lpda.Sigma = LpdaDesign.SigmaOpt(lpda.Tau);
                    tt.Text = FN(lpda.Tau, 3); ts.Text = FN(lpda.Sigma, 3);
                }
                else { lpda.Tau = ParseNum(tt.Text, lpda.Tau); lpda.Sigma = ParseNum(ts.Text, lpda.Sigma); }
                lpda.Rin = ParseNum(tr.Text, lpda.Rin); lpda.DiamMm = ParseNum(td.Text, lpda.DiamMm); lpda.DiamMode = dmode;
                lpda.LineTubeMm = ParseNum(tl.Text, lpda.LineTubeMm);
                lpda.Rastrema = cRas.IsChecked == true; lpda.SchemaRastr = tRas.Text;
                lpda.StubMm = ParseNum(tstub.Text, double.NaN);
            };
            Action mostra = null;
            mostra = () =>
            {
                leggi();
                int oldN = lpda.N;
                try { lpda.Compute(); }
                catch (Exception ex) { deriv.Text = "Errore: " + ex.Message; deriv.Foreground = Pal.B(Pal.Red); return; }
                if (lpda.UserLen != null && lpda.UserLen.Length != lpda.N) lpda.UserLen = null;
                deriv.Foreground = Pal.B(Pal.Muted);
                lb.Text = "B = " + FN(lpda.B, 2);
                tstub.Text = FN(lpda.StubMm, 0);
                lline.Text = "mm -> Z0 = " + FN(lpda.Z0, 1) + " ohm -> interasse " + FN(lpda.LineSpacingMm, 1) + " mm";
                deriv.Text = "Ricavati: alfa = " + FN(lpda.AlphaDeg, 2) + " gradi - Bar = " + FN(lpda.Bar, 3) + " - Bs = " + FN(lpda.Bs, 2) + " - N = " + FN(lpda.Nexact, 1) + " -> " + lpda.N +
                    " elementi - L = " + FN(lpda.Lboom * 1000, 0) + " mm (formula), boom " + FN(lpda.Pos[lpda.N - 1] * 1000, 0) + " mm - Za = " + FN(lpda.Za, 0) + " ohm - sigma' = " + FN(lpda.SigmaP, 3);
                // 0.5.1: controllo k1 (tau e sigma di Carrel)
                List<ControlloDato> k1 = ControlliDati.Lpda(lpda);
                foreach (ControlloDato d in k1) deriv.Text += "\n⚠ " + d.ToString();
                if (k1.Count > 0) deriv.Foreground = Pal.B(Pal.Orange);
                // tabella delle misure
                misBody.Children.Clear();
                StackPanel hd = new StackPanel { Orientation = Orientation.Horizontal };
                hd.Children.Add(Intest("El.", 40)); hd.Children.Add(Intest("Calcolata", 90)); hd.Children.Add(Intest("Tua", 90)); hd.Children.Add(Intest("Posizione", 90)); hd.Children.Add(Intest("Passo", 80)); hd.Children.Add(Intest("diam. mm", 70));
                misBody.Children.Add(hd);
                double lmax = 299.792458 / lpda.Fmin / 2;
                for (int i = 0; i < lpda.N; i++)
                {
                    int ii = i;
                    double calc = lmax * Math.Pow(lpda.Tau, i);
                    StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
                    row.Children.Add(new TextBlock { Text = (i + 1).ToString(CultureInfo.InvariantCulture), Width = 40, VerticalAlignment = VerticalAlignment.Center });
                    row.Children.Add(Cella(FN(calc * 1000, 0), 90, true));
                    TextBox tu = Cella(FN(lpda.Len[i] * 1000, 0), 90);
                    if (Math.Abs(lpda.Len[i] - calc) > 5e-4) tu.Foreground = Pal.B(Pal.Orange);
                    tu.LostFocus += (s, e) =>
                    {
                        double v = ParseNum(tu.Text, double.NaN) / 1000;
                        if (double.IsNaN(v) || v <= 0) return;
                        if (lpda.UserLen == null || lpda.UserLen.Length != lpda.N) lpda.UserLen = new double[lpda.N];
                        lpda.UserLen[ii] = v;
                        mostra();
                    };
                    row.Children.Add(tu);
                    row.Children.Add(Cella(FN(lpda.Pos[i] * 1000, 0), 90, true));
                    row.Children.Add(Cella(i < lpda.N - 1 ? FN((lpda.Pos[i + 1] - lpda.Pos[i]) * 1000, 0) : "0", 80, true));
                    row.Children.Add(Cella(FN(lpda.Diam[i] * 1000, 1), 70, true));
                    misBody.Children.Add(row);
                }
            };
            bso.Click += (s, e) => { double t = ParseNum(tt.Text, 0.9); ts.Text = FN(LpdaDesign.SigmaOpt(t), 3); mostra(); };
            bRip.Click += (s, e) => { lpda.UserLen = null; mostra(); };
            foreach (TextBox t in new[] { tf1, tf2, tg, tt, ts, tr, td, tl, tstub }) t.LostFocus += (s, e) => mostra();
            bApri.Click += (s, e) =>
            {
                mostra();
                double h = lpdaGround == 1 ? ParseNum(th.Text, 10) : 0;
                ApriModelloCalcolatore(lpda.ToNec((lpda.Fmin + lpda.Fmax) / 2, h, new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005 }, 50),
                    "LPDA " + FN(lpda.Fmin, 0) + "-" + FN(lpda.Fmax, 0) + " MHz (calcolatore).nec");
            };
            bExp.Click += (s, e) =>
            {
                mostra();
                try
                {
                    StringBuilder sb = new StringBuilder("elemento;calcolata_mm;tua_mm;posizione_mm;passo_mm;diametro_mm\n");
                    double lmax = 299.792458 / lpda.Fmin / 2;
                    for (int i = 0; i < lpda.N; i++)
                        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0};{1:0};{2:0};{3:0};{4:0};{5:0.0}", i + 1, lmax * Math.Pow(lpda.Tau, i) * 1000, lpda.Len[i] * 1000, lpda.Pos[i] * 1000,
                            i < lpda.N - 1 ? (lpda.Pos[i + 1] - lpda.Pos[i]) * 1000 : 0, lpda.Diam[i] * 1000));
                    string p = Path.Combine(AppPaths.DataDir, "LPDA_tabella.csv");
                    File.WriteAllText(p, sb.ToString());
                    Status("Tabella esportata in " + p);
                }
                catch (Exception ex) { Status("Esportazione non riuscita: " + ex.Message, true); }
            };
            bNec.Click += (s, e) =>
            {
                mostra();
                double h = lpdaGround == 1 ? ParseNum(th.Text, 10) : 0;
                CalcolaLpdaNec(h, tab, chS, chG, chF, p1, p2, lpWarn, bNec);
            };
            // 0.4.2: calcolo in tempo reale (IC8TEM 13/11/2026): modello della LPDA al centro della banda, ROS cercato su tutta la banda
            Action<string, double, double, double> avviaTempoReale;
            Border tempoReale = TempoRealePanel(out avviaTempoReale);
            right.Children.Insert(0, tempoReale);
            Action mostraBase = mostra;
            mostra = () =>
            {
                mostraBase();
                try
                {
                    double fc = (lpda.Fmin + lpda.Fmax) / 2;
                    avviaTempoReale(lpda.ToNec(fc, 0, new GroundSpec { Kind = GroundKind.FreeSpace }, lpda.Rin), lpda.Rin, lpda.Fmin * 0.95, lpda.Fmax * 1.05);
                }
                catch { }
            };
            mostra();
            Grid.SetColumn(left, 0);
            Grid.SetColumn(right, 2);
            g.Children.Add(left);
            g.Children.Add(right);
            // primo calcolo NEC quando si apre la linguetta (non all'avvio del programma)
            lpdaPrimoCalcolo = () => CalcolaLpdaNec(0, tab, chS, chG, chF, p1, p2, lpWarn, bNec);
            return new ScrollViewer { Content = g };
        }

        void CalcolaLpdaNec(double h, Grid tab, LineChart chS, LineChart chG, LineChart chF, PolarPlot p1, PolarPlot p2, StackPanel lpWarn, Button bNec)
        {
            LpdaDesign d = lpda;
            GroundSpec gs = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005 };
            double z0 = 50;
            bNec.IsEnabled = false;
            tab.Children.Clear();
            tab.RowDefinitions.Clear();
            tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            TextBlock wait = Nota("Calcolo NEC in corso (" + d.N + " elementi, linea incrociata)...", Pal.Fore);
            tab.Children.Add(wait);
            Task.Run(() =>
            {
                string nec = d.ToNec(d.Fmin, h, gs, z0);
                NecModel m = NecModel.Parse(nec);
                m.Z0 = z0;
                SegmentGeometry g = m.BuildGeometry();
                RunResult r1 = Analysis.Run(m, g, d.Fmin, 2.0);
                RunResult r2 = Analysis.Run(m, g, d.Fmax, 2.0);
                List<double> fs = new List<double>();
                int np = 41;
                for (int i = 0; i < np; i++) fs.Add(d.Fmin + (d.Fmax - d.Fmin) * i / (np - 1));
                List<SweepPoint> sw = Analysis.Sweep(m, g, fs, r1.ThetaMax, 0, null, System.Threading.CancellationToken.None);
                // tagli orizzontale e verticale con l'avanti in alto
                double[][] cuts = new double[4][];
                RunResult[] rr = { r1, r2 };
                for (int k = 0; k < 2; k++)
                {
                    double[] hor = new double[360], ver = new double[360];
                    double thf = rr[k].ThetaMax;
                    for (int a = 0; a < 360; a++)
                    {
                        hor[a] = rr[k].Engine.Gain(thf, a).GainTotal;
                        double el = a * Math.PI / 180;
                        double x = Math.Cos(el), z = Math.Sin(el);
                        double theta = Math.Acos(Math.Max(-1, Math.Min(1, z))) * 180 / Math.PI, phi = x >= 0 ? 0 : 180;
                        if (h > 0 && theta > 90) ver[a] = -999.99;
                        else ver[a] = rr[k].Engine.Gain(theta, phi).GainTotal;
                    }
                    cuts[2 * k] = hor; cuts[2 * k + 1] = ver;
                }
                return Tuple.Create(r1, r2, sw, cuts);
            }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
            {
                bNec.IsEnabled = true;
                tab.Children.Clear();
                tab.RowDefinitions.Clear();
                if (t.IsFaulted) { tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); tab.Children.Add(Nota("Errore NEC: " + t.Exception.GetBaseException().Message, Pal.Red)); return; }
                RunResult r1 = t.Result.Item1, r2 = t.Result.Item2;
                List<SweepPoint> sw = t.Result.Item3;
                double[][] cuts = t.Result.Item4;
                List<SweepPoint> ok = sw.FindAll(x => x.Ok);
                double maxSwr = ok.Count > 0 ? ok.Max(x => x.Swr) : double.NaN;
                double gmin = ok.Count > 0 ? ok.Min(x => x.GainFwd) : double.NaN, gmax = ok.Count > 0 ? ok.Max(x => x.GainFwd) : double.NaN;
                SweepPoint fbMin = ok.Count > 0 ? ok.OrderBy(x => x.FrontBack).First() : null;
                string[][] rows = {
                    new[] { "", FN(d.Fmin, 0) + " MHz", FN(d.Fmax, 0) + " MHz", "banda" },
                    new[] { "Impedenza", ZcalcTxt(r1.Zin), ZcalcTxt(r2.Zin), "" },
                    new[] { "ROS su " + FN(z0, 0) + " ohm", FN(r1.Swr, 2), FN(r2.Swr, 2), "max " + FN(maxSwr, 2) },
                    new[] { "Guadagno", FN(r1.GmaxDbi, 1) + " dBi", FN(r2.GmaxDbi, 1) + " dBi", FN(gmin, 1) + " - " + FN(gmax, 1) + " dBi" },
                    new[] { "F/B", FN(r1.FrontBack, 0) + " dB", FN(r2.FrontBack, 0) + " dB", fbMin != null ? "min " + FN(fbMin.FrontBack, 0) + " dB a " + FN(fbMin.F, 0) + " MHz" : "" },
                    new[] { "Apertura -3 dB (oriz./vert.)", FN(r1.HpbwAz, 0) + " / " + FN(r1.HpbwEl, 0) + " gradi", FN(r2.HpbwAz, 0) + " / " + FN(r2.HpbwEl, 0) + " gradi", "" } };
                for (int i = 0; i < rows.Length; i++)
                {
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int c = 0; c < 4; c++)
                    {
                        TextBlock tb = new TextBlock { Text = rows[i][c], Margin = new Thickness(4, 2), Foreground = Pal.B(Pal.Fore), FontWeight = i == 0 ? FontWeight.Bold : FontWeight.Normal,
                            HorizontalAlignment = c == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right, FontFamily = c > 0 && i > 0 ? Pal.Mono : FontFamily.Default };
                        Grid.SetRow(tb, i); Grid.SetColumn(tb, c);
                        tab.Children.Add(tb);
                    }
                }
                double[] xs = sw.Select(p => p.F).ToArray();
                chS.X = xs; chS.Series = new List<LineSeries> { new LineSeries { Name = "ROS", Y = sw.Select(p => p.Ok ? p.Swr : double.NaN).ToArray(), Color = Pal.Accent } };
                chG.X = xs; chG.Series = new List<LineSeries> { new LineSeries { Name = "Guadagno dBi", Y = sw.Select(p => p.Ok ? p.GainFwd : double.NaN).ToArray(), Color = Pal.Green } };
                chF.X = xs; chF.Series = new List<LineSeries> { new LineSeries { Name = "F/B dB", Y = sw.Select(p => p.Ok ? p.FrontBack : double.NaN).ToArray(), Color = Pal.Orange } };
                chS.InvalidateVisual(); chG.InvalidateVisual(); chF.InvalidateVisual();
                PolarPlot[] pp = { p1, p2 };
                RunResult[] rr = { r1, r2 };
                for (int k = 0; k < 2; k++)
                {
                    pp[k].Title = FN(rr[k].FreqMHz, 0) + " MHz (avanti in alto, scala 30 dB)";
                    double mx = Math.Max(cuts[2 * k].Max(), cuts[2 * k + 1].Max());
                    pp[k].RefDb = mx;
                    pp[k].Traces = new List<PolarTrace> {
                        new PolarTrace { Db = cuts[2 * k], Color = Pal.Accent, Name = "orizzontale" },
                        new PolarTrace { Db = cuts[2 * k + 1], Color = Pal.Orange, Dashed = true, Name = "verticale" } };
                    pp[k].InvalidateVisual();
                }
                lpWarn.Children.Clear();
                List<string> w = new List<string>();
                w.Add("Curve di Carrel originali 1-2 dB troppo ottimiste: usate quelle corrette (Butson-Thompson). Conta il NEC.");
                if (fbMin != null && fbMin.FrontBack < 15) w.Add("F/B scende a " + FN(fbMin.FrontBack, 0) + " dB intorno a " + FN(fbMin.F, 0) + " MHz: provare tau piu' alto o 'Ottimizza...'.");
                if (!double.IsNaN(maxSwr) && maxSwr > 2) w.Add("ROS oltre 2 in banda (max " + FN(maxSwr, 2) + "): controllare Z0 della linea e lo stub.");
                foreach (string s in w)
                    lpWarn.Children.Add(new Border { Background = Pal.B(Pal.WarnBg), BorderBrush = Pal.B(Pal.Orange), BorderThickness = new Thickness(3, 0, 0, 0), Padding = new Thickness(8, 6), Margin = new Thickness(0, 4, 0, 0),
                        Child = new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.WarnText) } });
            }));
        }

        static string ZcalcTxt(Complex z) { return FN(z.Real, 1) + (z.Imaginary >= 0 ? " +j" : " -j") + FN(Math.Abs(z.Imaginary), 1); }
    }
}
