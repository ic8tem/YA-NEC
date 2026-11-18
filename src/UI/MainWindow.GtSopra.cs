// YA-NEC 1.1.0 - sopralinguetta "Rumore/GT/Stack" (bozzetti approvati da IC8TEM il 17/11/2026 19:17): fascione a sinistra con tre voci:
// "Rumore e G/T" (la pagina di prima, identica), "Rumore dal cielo vero" (NUOVO: mappa del cielo, QTH, ora, Luna; calcolo di skynoise di
// G8ONH portato in C#; parte come VE7BQH: 30 gradi, stack 2 x 2 a distanza DL6WU, spazio libero; due G/T affiancati) e "Distanze di stack"
// (la scheda di prima dai Calcolatori, con sotto l'accoppiamento e la distanza migliore col NEC, VHF/UHF in spazio libero e HF sopra il terreno).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Fascione fasGt;
        ContentControl hostGt;
        readonly Dictionary<string, Control> gtPagine = new Dictionary<string, Control>();

        Control BuildGtSopra()
        {
            fasGt = new Fascione();
            fasGt.Children.Add(new TextBlock { Text = "Rumore, G/T e stack", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(2, 0, 0, 2) });
            fasGt.Famiglia("RICEZIONE");
            fasGt.Voce("gt", "Rumore e G/T", false, null, "come prima: TANT, VE7BQH, KF2YN, stack");
            fasGt.Voce("cielo", "Rumore dal cielo vero", false, null, "NUOVO · mappa del cielo, QTH, ora, Luna (skynoise, G8ONH)");
            fasGt.Voce("stack", "Distanze di stack", false, null, "VHF/UHF (DL6WU) e HF (sopra il terreno), accoppiamento, distanza migliore");
            hostGt = new ContentControl();
            gtPagine["gt"] = BuildGtPage();
            fasGt.Selected += k =>
            {
                Control c;
                if (!gtPagine.TryGetValue(k, out c))
                {
                    c = k == "cielo" ? BuildCieloPage() : BuildStackStudioPage();
                    gtPagine[k] = c;
                }
                hostGt.Content = c;
                if (k == "gt") CalcolaGt();
            };
            Grid page = Fascione.PaginaLarga(fasGt, hostGt, 20, "rumoregt");
            fasGt.Select("gt", false);
            hostGt.Content = gtPagine["gt"];
            return page;
        }

        /// <summary>Apre la sopralinguetta Rumore/GT/Stack sulla voce data.</summary>
        public void ApriRumoreGt(string voce)
        {
            for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == "Rumore/GT/Stack") { SelectTab(i); break; }
            if (fasGt != null) fasGt.Select(voce);
        }

        // =====================================================================================================
        // Rumore dal cielo vero
        // =====================================================================================================
        TextBox cvLoc, cvData, cvAz, cvEl, cvTg, cvTsky, cvTgu, cvDv, cvDh;
        int cvPunt, cvStack = 3, cvDist, cvMappa, cvAmb = 0;
        WrapPanel cvCards;
        LineChart cvChart;
        StackPanel cvTable;
        TextBlock cvStato;
        CancellationTokenSource cvCts;

        Control BuildCieloPage()
        {
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,10,*") };
            StackPanel left = new StackPanel(), right = new StackPanel();
            StackPanel inp = new StackPanel { Spacing = 2 };
            inp.Children.Add(Riga("Mappa del cielo", Scelta(new[] { "dalla frequenza", "144 MHz", "432 MHz" }, 0, k => cvMappa = k)));
            cvLoc = Tb(cfg.Get("cielo_qth", ""), 90);
            ToolTip.SetTip(cvLoc, "Locator (JN70CN) oppure latitudine e longitudine in gradi (40,55 14,21). Si salva in config.ini.");
            inp.Children.Add(Riga("QTH", cvLoc, new TextBlock { Text = "locator, oppure latitudine e longitudine", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            cvData = Tb(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture), 130);
            Button ora = Btn("adesso", false);
            ora.Click += (s, e) => cvData.Text = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            inp.Children.Add(Riga("Data e ora (UTC)", cvData, ora));
            cvAz = Tb("270", 55); cvEl = Tb("30", 55);
            inp.Children.Add(Riga("Puntamento", Scelta(new[] { "azimut ed elevazione", "Luna", "cielo piu' freddo" }, 0, k => { cvPunt = k; cvAz.IsEnabled = k == 0; cvEl.IsEnabled = k != 1; })));
            inp.Children.Add(Riga("Azimut / elevazione", cvAz, cvEl, new TextBlock { Text = "gradi · 30° come VE7BQH", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            inp.Children.Add(Riga("Antenne", Scelta(new[] { "singola", "2 affiancate", "2 sovrapposte", "2 x 2" }, 3, k => cvStack = k), new TextBlock { Text = "come VE7BQH (stack di 4)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            cvDv = Tb("", 60); cvDh = Tb("", 60);
            inp.Children.Add(Riga("Distanze", Scelta(new[] { "DL6WU", "proposta (Distanze di stack)", "a mano" }, 0, k => { cvDist = k; cvDv.IsEnabled = k == 2; cvDh.IsEnabled = k == 2; }), cvDv, cvDh, new TextBlock { Text = "m (V, H)", VerticalAlignment = VerticalAlignment.Center }));
            cvDv.IsEnabled = cvDh.IsEnabled = false;
            cvTsky = Tb("200", 55); cvTgu = Tb("1000", 55);
            inp.Children.Add(Riga("Temperature uniformi (VE7BQH)", cvTsky, cvTgu, Scelta(new[] { "TANT", "rurale", "residenziale", "citta'" }, 0, k =>
            {
                cvAmb = k;
                double f = run != null ? run.FreqMHz : 144;
                if (k == 0) { cvTsky.Text = f < 300 ? "200" : (f < 1000 ? "20" : "10"); cvTgu.Text = f < 300 ? "1000" : (f < 1000 ? "350" : "290"); }
                else { cvTsky.Text = "290"; cvTgu.Text = k == 1 ? "1600" : (k == 2 ? "5400" : "14550"); }
            })));
            cvTg = Tb("290", 55);
            inp.Children.Add(Riga("T del terreno (cielo vero)", cvTg, new TextBlock { Text = "K sotto l'orizzonte (skynoise: 290 K)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            Button calc = Btn("Calcola", true), giorno = Btn("T antenna nelle 24 ore", false), copia = Btn("Copia", false);
            calc.Click += (s, e) => CalcolaCielo(false);
            giorno.Click += (s, e) => CalcolaCielo(true);
            copia.Click += async (s, e) => { try { if (Clipboard != null) await Clipboard.SetTextAsync(TestoCielo()); Status("Risultati del cielo vero copiati."); } catch { } };
            WrapPanel bb = new WrapPanel();
            foreach (Button b in new[] { calc, giorno, copia }) { b.Margin = new Thickness(0, 6, 8, 0); bb.Children.Add(b); }
            inp.Children.Add(bb);
            cvStato = new TextBlock { Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
            inp.Children.Add(cvStato);
            Border cred = new Border
            {
                Background = Pal.B(Pal.WarnBg), BorderBrush = Pal.B(Pal.Orange), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 8), Margin = new Thickness(0, 10, 0, 0),
                Child = new TestoAdattato
                {
                    Child = new TextBlock
                    {
                        TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), FontSize = 12.5,
                        Text = "Autori e fonti (licenza MIT, nota completa in Crediti)\n" +
                               "Calcolo da skynoise 0.2.2 di John C. Sager G8ONH (2025-2026, licenza MIT), portato in C# in YA-NEC.\n" +
                               "Mappe del cielo: C. G. T. Haslam et al., rilievo a 408 MHz (A&AS 47, 1982); rielaborazione di M. Remazeilles et al. (MNRAS 451, 2015), dal server LAMBDA della NASA; mappe a 144 e 432 MHz preparate da G8ONH.\n" +
                               "Tempo siderale: G. H. Kaplan, USNO Circular 179 (2005). Scala in frequenza: T = (fmappa / f)^2,56 T (G8ONH). Luna: J. Meeus, Astronomical Algorithms, cap. 47.\n" +
                               "Confronto: L. Edwards VE7BQH (tabelle G/T a 30 gradi, stack di 4 a distanza DL6WU, temperature uniformi); TANT di YT1NT e VE3EA (manuale DG7YBN); correzione KF2YN."
                    }
                }
            };
            inp.Children.Add(cred);
            left.Children.Add(Pannello("Dati di ingresso", inp));
            cvCards = new WrapPanel();
            right.Children.Add(cvCards);
            cvChart = new LineChart { Title = "T antenna nelle 24 ore (puntamento fisso)", XLabel = "ore UTC", Height = 260, EmptyText = "premi \"T antenna nelle 24 ore\"" };
            right.Children.Add(Pannello("T antenna nelle 24 ore", cvChart));
            cvTable = new StackPanel();
            right.Children.Add(Pannello("Da dove arriva il rumore", cvTable));
            Grid.SetColumn(left, 0); Grid.SetColumn(right, 2);
            g.Children.Add(left); g.Children.Add(right);
            return new ScrollViewer { Content = g };
        }

        class CieloCalcolo
        {
            public GtResult Uniforme; public CieloRisultato Vero; public double GtVero, TantVero, Gain, Az, El, F, Lat, Lon;
            public double[] Ore, Tore; public double GtMin = double.NaN, GtMax = double.NaN; public string Nota = "", Mappa = "";
        }
        CieloCalcolo cvUltimo;

        static bool LeggiQth(string s, out double lat, out double lon)
        {
            if (CieloVero.DaLocator(s, out lat, out lon)) return true;
            string[] p = (s ?? "").Replace(';', ' ').Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 2)
            {
                lat = ParseNum(p[0], double.NaN); lon = ParseNum(p[1], double.NaN);
                return !double.IsNaN(lat) && !double.IsNaN(lon) && Math.Abs(lat) <= 90 && Math.Abs(lon) <= 180;
            }
            return false;
        }

        void CalcolaCielo(bool giorno)
        {
            if (run == null || model == null) { cvStato.Text = "Calcola prima un modello."; return; }
            double lat, lon;
            if (!LeggiQth(cvLoc.Text, out lat, out lon)) { cvStato.Text = "QTH non valido: scrivi il locator (es. JN70CN) oppure latitudine e longitudine."; return; }
            cfg.Set("cielo_qth", cvLoc.Text.Trim());
            DateTime t;
            if (!DateTime.TryParseExact(cvData.Text.Trim(), new[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy H:mm", "yyyy-MM-dd HH:mm", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out t))
            { cvStato.Text = "Data e ora non valide (gg/mm/aaaa hh:mm, UTC)."; return; }
            t = DateTime.SpecifyKind(t, DateTimeKind.Utc);
            double az = ParseNum(cvAz.Text, 270), el = ParseNum(cvEl.Text, 30), tg = ParseNum(cvTg.Text, 290), tsu = ParseNum(cvTsky.Text, 200), tgu = ParseNum(cvTgu.Text, 1000);
            double loss = gtLoss != null ? ParseNum(gtLoss.Text, 0) : 0, nf = gtNf != null ? ParseNum(gtNf.Text, 0) : 0;
            int punt = cvPunt, stk = cvStack, dist = cvDist, mp = cvMappa;
            double dvm = ParseNum(cvDv.Text, double.NaN), dhm = ParseNum(cvDh.Text, double.NaN);
            StackStudioRisultato prop = ssUltimo;
            RunResult r0 = run; NecModel m0 = model;
            if (cvCts != null) cvCts.Cancel();
            cvCts = new CancellationTokenSource();
            CancellationToken ct = cvCts.Token;
            cvStato.Text = "calcolo in corso (spazio libero come VE7BQH" + (stk > 0 ? ", " + GtNomi[stk] : "") + ")...";
            Task.Run(() =>
            {
                CieloCalcolo c = new CieloCalcolo { F = r0.FreqMHz, Lat = lat, Lon = lon };
                NecModel mf = m0.Clone();
                mf.Ground = new GroundSpec { Kind = GroundKind.FreeSpace }; mf.GroundFlag = 0;
                RunResult r = Analysis.Run(mf, mf.BuildGeometry(), r0.FreqMHz, 2.0, ct);
                RunResult ra = r;
                if (stk > 0 && StackDetect.Detect(mf) == null)
                {
                    double dv = Dl6wu(r.FreqMHz, r.HpbwEl), dh = Dl6wu(r.FreqMHz, r.HpbwAz);
                    if (dist == 1 && prop != null && prop.Proposta != null) { dv = prop.Proposta.Dv > 0 ? prop.Proposta.Dv : dv; dh = prop.Proposta.Dh > 0 ? prop.Proposta.Dh : dh; }
                    if (dist == 2) { if (!double.IsNaN(dvm) && dvm > 0) dv = dvm; if (!double.IsNaN(dhm) && dhm > 0) dh = dhm; }
                    NecModel ms = StackDetect.Crea(mf, GtRighe[stk], GtColonne[stk], dv, dh, r.PhiMax, null);
                    ra = Analysis.Run(ms, ms.BuildGeometry(), r0.FreqMHz, 2.0, ct);
                    c.Nota = GtNomi[stk] + ": verticale " + (GtRighe[stk] > 1 ? FN(dv, 3) + " m" : "-") + ", orizzontale " + (GtColonne[stk] > 1 ? FN(dh, 3) + " m" : "-");
                }
                else c.Nota = "antenna singola";
                PatternGrid pg = GrigliaGt(ra, 2);
                double thF = r.ThetaMax, phF = r.PhiMax;
                c.Gain = ra.Engine.Gain(thF, phF).GainTotal;
                MappaCielo mappa = CieloVero.Mappa(Path.Combine(AppPaths.DataDir, "cielo"), mp == 1 ? 144 : (mp == 2 ? 432 : r0.FreqMHz));
                c.Mappa = mappa.Origine + (Math.Abs(mappa.FreqMHz - r0.FreqMHz) > 0.5 ? " scalata a " + FN(r0.FreqMHz, 1) + " MHz" : "");
                if (punt == 1)
                {
                    CieloVero.Luna(t, lat, lon, out az, out el);
                    if (el <= 0) throw new Exception("La Luna e' sotto l'orizzonte a quell'ora (elevazione " + FN(el, 1) + " gradi).");
                }
                else if (punt == 2)
                {
                    double best = double.MaxValue, ba = 0;
                    for (int a = 0; a < 360; a += 10)
                    {
                        ct.ThrowIfCancellationRequested();
                        CieloRisultato x = CieloVero.Tpattern(pg, thF, phF, mappa, r0.FreqMHz, lat, lon, t, a, el, tg);
                        if (x.Tpattern < best) { best = x.Tpattern; ba = a; }
                    }
                    az = ba;
                }
                c.Az = az; c.El = el;
                c.Uniforme = NoiseGt.Gt(pg, thF, phF, c.Gain, el, tsu, tgu, loss, nf, true, ra.Efficiency > 0 ? ra.Efficiency : 100);
                c.Vero = CieloVero.Tpattern(pg, thF, phF, mappa, r0.FreqMHz, lat, lon, t, az, el, tg);
                double tl = c.Uniforme.Tloss;
                c.TantVero = c.Vero.Tpattern + tl;
                c.GtVero = c.Gain - 10 * Math.Log10(c.TantVero);
                if (giorno)
                {
                    List<double> o = new List<double>(), tt = new List<double>();
                    double gmin = double.MaxValue, gmax = double.MinValue;
                    DateTime d0 = new DateTime(t.Year, t.Month, t.Day, 0, 0, 0, DateTimeKind.Utc);
                    for (int i = 0; i <= 48; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        DateTime ti = d0.AddMinutes(30 * i);
                        double azi = az, eli = el;
                        if (punt == 1) { CieloVero.Luna(ti, lat, lon, out azi, out eli); if (eli <= 0) continue; }
                        CieloRisultato x = CieloVero.Tpattern(pg, thF, phF, mappa, r0.FreqMHz, lat, lon, ti, azi, eli, tg);
                        double ta = x.Tpattern + tl, gtv = c.Gain - 10 * Math.Log10(ta);
                        o.Add(i * 0.5); tt.Add(ta);
                        gmin = Math.Min(gmin, gtv); gmax = Math.Max(gmax, gtv);
                    }
                    c.Ore = o.ToArray(); c.Tore = tt.ToArray(); c.GtMin = gmin; c.GtMax = gmax;
                }
                return c;
            }, ct).ContinueWith(task => Dispatcher.UIThread.Post(() =>
            {
                if (task.IsCanceled) return;
                if (task.IsFaulted) { cvStato.Text = "Errore: " + task.Exception.GetBaseException().Message; return; }
                CieloCalcolo c = task.Result;
                cvUltimo = c;
                cvStato.Text = "Puntamento azimut " + FN(c.Az, 1) + "°, elevazione " + FN(c.El, 1) + "°; " + c.Nota + "; mappa " + c.Mappa + ".";
                cvCards.Children.Clear();
                Border b1 = Riquadretto("G/T come VE7BQH", FN(c.Uniforme.GtAnt, 2) + " dB/K", "temperature uniformi: si confronta con le tabelle", true);
                Border b2 = Riquadretto("G/T con il cielo vero", FN(c.GtVero, 2) + " dB/K", cvData.Text + " UTC, " + FN(c.Az, 0) + "° / " + FN(c.El, 0) + "°", true);
                ToolTip.SetTip(b1, "Lo stesso calcolo di Rumore e G/T: cielo e terreno a temperatura uniforme (VE7BQH, TANT). Non dipende ne' dal QTH ne' dall'ora.");
                ToolTip.SetTip(b2, "Ogni direzione sopra l'orizzonte prende la temperatura della mappa del cielo in quel punto, all'ora e dal QTH dati (skynoise, G8ONH).");
                cvCards.Children.Add(b1); cvCards.Children.Add(b2);
                cvCards.Children.Add(Riquadretto("T antenna (cielo vero)", FN(c.TantVero, 1) + " K", "uniforme " + FN(c.Uniforme.Tant, 1) + " K"));
                cvCards.Children.Add(Riquadretto("Differenza dal cielo uniforme", (c.GtVero - c.Uniforme.GtAnt >= 0 ? "+" : "") + FN(c.GtVero - c.Uniforme.GtAnt, 2) + " dB", "peso della Via Lattea qui e ora"));
                cvCards.Children.Add(Riquadretto("Guadagno", FN(c.Gain, 2) + " dBi", c.Nota));
                if (!double.IsNaN(c.GtMin)) cvCards.Children.Add(Riquadretto("Nelle 24 ore (cielo vero)", FN(c.GtMin, 2) + " / " + FN(c.GtMax, 2) + " dB/K", "minimo / massimo"));
                cvCards.Children.Add(Riquadretto("Centro del lobo nel cielo", "RA " + FN(c.Vero.RaBs, 1) + "°, Dec " + FN(c.Vero.DecBs, 1) + "°", "tempo siderale locale " + FN(c.Vero.Lmst / 15, 2) + " h"));
                cvTable.Children.Clear();
                foreach (string[] rr in new[]
                {
                    new[] { "Cielo (fuori dalla Via Lattea)", FN(c.Vero.CieloK, 1) + " K" },
                    new[] { "Via Lattea (zone sopra 1000 K a 144 MHz)", FN(c.Vero.ViaLatteaK, 1) + " K" },
                    new[] { "Terreno", FN(c.Vero.TerrenoK, 1) + " K" },
                    new[] { "Perdite", FN(c.Uniforme.Tloss, 1) + " K" },
                })
                {
                    DockPanel dp = new DockPanel { Margin = new Thickness(0, 2) };
                    TextBlock v = new TextBlock { Text = rr[1], FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore) };
                    DockPanel.SetDock(v, Dock.Right);
                    dp.Children.Add(v);
                    dp.Children.Add(new TextBlock { Text = rr[0], Foreground = Pal.B(Pal.Fore) });
                    cvTable.Children.Add(dp);
                }
                if (c.F < 50 || c.F > 1300) cvTable.Children.Add(Nota("Fuori da 50-1300 MHz la scala della mappa con l'esponente 2,56 non basta (sotto: assorbimento ionosferico e rumore artificiale; sopra: fondo cosmico e atmosfera): il valore e' solo indicativo.", Pal.Orange));
                if (c.Ore != null)
                {
                    cvChart.X = c.Ore;
                    cvChart.Series = new List<LineSeries> { new LineSeries { Name = "T antenna K", Y = c.Tore, Color = Pal.Accent, Decimals = 0 } };
                    cvChart.InvalidateVisual();
                }
            }));
        }

        string TestoCielo()
        {
            CieloCalcolo c = cvUltimo;
            if (c == null) return "";
            StringBuilder s = new StringBuilder();
            s.AppendLine("YA-NEC - Rumore dal cielo vero (skynoise, G8ONH)");
            s.AppendLine("Frequenza " + FN(c.F, 3) + " MHz, QTH " + FN(c.Lat, 3) + " " + FN(c.Lon, 3) + ", " + cvData.Text + " UTC, azimut " + FN(c.Az, 1) + ", elevazione " + FN(c.El, 1));
            s.AppendLine(c.Nota + "; mappa " + c.Mappa);
            s.AppendLine("G/T come VE7BQH: " + FN(c.Uniforme.GtAnt, 2) + " dB/K; G/T con il cielo vero: " + FN(c.GtVero, 2) + " dB/K; T antenna " + FN(c.TantVero, 1) + " K");
            if (!double.IsNaN(c.GtMin)) s.AppendLine("Nelle 24 ore: minimo " + FN(c.GtMin, 2) + ", massimo " + FN(c.GtMax, 2) + " dB/K");
            return s.ToString();
        }

        // =====================================================================================================
        // Distanze di stack: scheda di prima + accoppiamento e distanza migliore (NEC)
        // =====================================================================================================
        int ssModo, ssCrit = 3, ssN = 2;
        TextBox ssKmin, ssKmax, ssPasso, ssRosMax, ssZ0, ssSoglia, ssH, ssDmin, ssDmax, ssAmin, ssAmax;
        StackPanel ssVhfBox, ssHfBox, ssRis;
        TextBlock ssStato;
        StackStudioRisultato ssUltimo;
        CancellationTokenSource ssCts;
        CalcStackDl6wu ssCalc;

        Control BuildStackStudioPage()
        {
            StackPanel all = new StackPanel { Spacing = 8 };
            ssCalc = simpleCalcs.Find(c => c.Key == "stackdl6wu") as CalcStackDl6wu;
            if (ssCalc != null)
            {
                Control p = BuildSimpleCalcPanel(ssCalc, null);
                if (p is ScrollViewer sv && sv.Content is Control inner) { sv.Content = null; all.Children.Add(inner); }
                else all.Children.Add(p);
            }
            StackPanel sez = new StackPanel { Spacing = 3 };
            sez.Children.Add(Riga("Banda dello stack", Scelta(new[] { "VHF/UHF (spazio libero, DL6WU)", "HF (sopra il terreno)" }, 0, k => { ssModo = k; ssVhfBox.IsVisible = k == 0; ssHfBox.IsVisible = k == 1; })));
            sez.Children.Add(new TestoAdattato { Child = Nota("VHF/UHF: per ogni distanza (fattore x la distanza DL6WU) lo stack si calcola col NEC: guadagno, lobo laterale piu' alto nel piano dello stack, G/T (temperature di Rumore e G/T) e accoppiamento tra le porte. HF: la formula DL6WU non vale (le antenne stanno sopra il terreno e conta l'angolo di partenza): lo stack si sceglie sugli angoli di elevazione voluti.") });
            // VHF/UHF
            ssVhfBox = new StackPanel { Spacing = 3 };
            ssKmin = Tb("0,5", 50); ssKmax = Tb("2,5", 50); ssZ0 = Tb(model != null ? FN(model.Z0, 0) : "50", 55); ssSoglia = Tb("-13", 50);   // 1.3.2: di partenza fino a 2,5 (IC8TEM 18/11/2026 14:48)
            ssPasso = Tb("0,05", 50);   // 1.3.1 (bozzetto approvato da IC8TEM il 18/11/2026 13:04): passo scelto, di partenza 0,05
            ssVhfBox.Children.Add(Riga("Distanze da provare", ssKmin, new TextBlock { Text = "-", VerticalAlignment = VerticalAlignment.Center }, ssKmax, new TextBlock { Text = "x la distanza DL6WU, a passi di", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }, ssPasso));
            ssVhfBox.Children.Add(Riga("Impedenza di riferimento", ssZ0, new TextBlock { Text = "Ω (barra in alto)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            ssVhfBox.Children.Add(Riga("Criterio della distanza migliore", Scelta(new[] { "guadagno massimo", "lobi laterali piu' bassi", "G/T migliore", "guadagno con lobi sotto" }, 3, k => ssCrit = k), ssSoglia, new TextBlock { Text = "dB", VerticalAlignment = VerticalAlignment.Center }));
            ssRosMax = Tb("1,5", 50);   // 1.3.2 (IC8TEM 18/11/2026 14:47): oltre questo ROS attivo la distanza e' "da non scegliere"
            ssVhfBox.Children.Add(Riga("ROS attivo massimo", ssRosMax, new TextBlock { Text = "oltre: \"ROS attivo alto: da non scegliere\" (escluse dalla scelta proposta)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, MaxWidth = 520 }));
            ssVhfBox.Children.Add(Riga("G/T (da Rumore e G/T)", new TextBlock { Text = "temperature del cielo e del terreno ed elevazione della voce Rumore e G/T", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, MaxWidth = 420 }));
            Button bv = Btn("Calcola accoppiamento e distanze (NEC)", true), bu = Btn("Usa la distanza proposta per lo stack", false), bc = Btn("Copia", false);
            bv.Click += (s, e) => StudioStack(false);
            bu.Click += (s, e) => UsaPropostaStack();
            bc.Click += async (s, e) => { try { if (Clipboard != null) await Clipboard.SetTextAsync(TestoStack()); Status("Tabella dello stack copiata."); } catch { } };
            WrapPanel w1 = new WrapPanel();
            foreach (Button b in new[] { bv, bu, bc }) { b.Margin = new Thickness(0, 4, 8, 0); w1.Children.Add(b); }
            ssVhfBox.Children.Add(w1);
            // HF
            ssHfBox = new StackPanel { Spacing = 3, IsVisible = false };
            ssH = Tb("15", 50); ssDmin = Tb("0,5", 50); ssDmax = Tb("1,25", 50); ssAmin = Tb("5", 45); ssAmax = Tb("15", 45);
            ssHfBox.Children.Add(Riga("Antenne una sopra l'altra", Scelta(new[] { "2", "3" }, 0, k => ssN = k + 2), new TextBlock { Text = "antenna base: il modello aperto", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            ssHfBox.Children.Add(Riga("Quota dell'antenna piu' bassa", ssH, new TextBlock { Text = "m", VerticalAlignment = VerticalAlignment.Center }));
            ssHfBox.Children.Add(Riga("Distanze da provare", ssDmin, new TextBlock { Text = "-", VerticalAlignment = VerticalAlignment.Center }, ssDmax, new TextBlock { Text = "λ, a passi di 0,125", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            ssHfBox.Children.Add(Riga("Terreno", new TextBlock { Text = "quello del modello (se e' in spazio libero: suolo medio 13 / 0,005, Sommerfeld)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, MaxWidth = 420 }));
            ssHfBox.Children.Add(Riga("Angoli di partenza voluti", ssAmin, new TextBlock { Text = "-", VerticalAlignment = VerticalAlignment.Center }, ssAmax, new TextBlock { Text = "gradi", VerticalAlignment = VerticalAlignment.Center }));
            Button bh = Btn("Calcola lo stack HF (NEC)", true), bhc = Btn("Crea lo stack nel modello aperto", false), bhx = Btn("Copia", false);
            bh.Click += (s, e) => StudioStack(true);
            bhc.Click += (s, e) => UsaPropostaStack();
            bhx.Click += async (s, e) => { try { if (Clipboard != null) await Clipboard.SetTextAsync(TestoStack()); Status("Tabella dello stack copiata."); } catch { } };
            WrapPanel w2 = new WrapPanel();
            foreach (Button b in new[] { bh, bhc, bhx }) { b.Margin = new Thickness(0, 4, 8, 0); w2.Children.Add(b); }
            ssHfBox.Children.Add(w2);
            sez.Children.Add(ssVhfBox);
            sez.Children.Add(ssHfBox);
            ssStato = new TextBlock { Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap };
            sez.Children.Add(ssStato);
            ssRis = new StackPanel { Spacing = 6 };
            sez.Children.Add(ssRis);
            sez.Children.Add(new TestoAdattato { Child = Nota("Fonti: DL6WU (formula); ARRL Antenna Book 24a ed. (limiti della formula, stack di Yagi HF e angoli di elevazione); L. Edwards VE7BQH (G/T come criterio per gli stack EME); R. D. Straw N6BV (HFTA, stack e terreno); ON4UN, Low-Band DXing; L. B. Cebik W4RNL; nec2-array di YO3IIU (matrice di accoppiamento in ricezione, GPL-2 o successive); G. Burke e A. Poggio, manuale NEC-2 (scheda CP).") });
            all.Children.Add(Pannello("Accoppiamento e distanza migliore (NEC)", sez, null, "Distanze di stack: accoppiamento e distanza migliore"));
            return new ScrollViewer { Content = all, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        void StudioStack(bool hf)
        {
            if (model == null || run == null) { ssStato.Text = "Calcola prima un modello."; return; }
            if (StackDetect.Detect(model) != null) { ssStato.Text = "Il modello aperto e' gia' uno stack: apri l'antenna singola."; return; }
            NecModel m0 = model; double f = run.FreqMHz;
            int righe = ssCalc != null ? Math.Max(1, Math.Min(8, ssCalc.I("righe"))) : 2, col = ssCalc != null ? Math.Max(1, Math.Min(8, ssCalc.I("col"))) : 1;
            if (!hf && righe * col < 2) { ssStato.Text = "Servono almeno 2 antenne (righe x colonne nella scheda sopra)."; return; }
            double kmin = ParseNum(ssKmin.Text, 0.5), kmax = ParseNum(ssKmax.Text, 2.5), passo = Math.Max(0.01, ParseNum(ssPasso.Text, 0.05)), rosMax = Math.Max(1.01, ParseNum(ssRosMax.Text, 1.5)), z0 = ParseNum(ssZ0.Text, m0.Z0), soglia = ParseNum(ssSoglia.Text, -13);
            double tsky = gtSky != null ? ParseNum(gtSky.Text, 200) : 200, tg = gtGround != null ? ParseNum(gtGround.Text, 1000) : 1000, el = gtElev != null ? ParseNum(gtElev.Text, 30) : 30;
            double h = ParseNum(ssH.Text, 15), dmin = ParseNum(ssDmin.Text, 0.5), dmax = ParseNum(ssDmax.Text, 1.25), amin = ParseNum(ssAmin.Text, 5), amax = ParseNum(ssAmax.Text, 15);
            int crit = ssCrit, n = ssN;
            if (ssCts != null) ssCts.Cancel();
            ssCts = new CancellationTokenSource();
            CancellationToken ct = ssCts.Token;
            ssStato.Text = hf ? "calcolo dello stack HF sopra il terreno in corso..." : "calcolo di " + (righe * col) + " antenne a tutte le distanze in corso (una soluzione NEC per porta)...";
            ssRis.Children.Clear();
            Task.Run(() => hf ? StackStudio.Hf(m0, f, n, h, Math.Min(dmin, dmax), Math.Max(dmin, dmax), Math.Min(amin, amax), Math.Max(amin, amax), ct)
                              : StackStudio.Vhf(m0, f, righe, col, Math.Min(kmin, kmax), Math.Max(kmin, kmax), z0, crit, soglia, tsky, tg, el, 2, ct, passo, rosMax), ct)
                .ContinueWith(t => Dispatcher.UIThread.Post(() =>
                {
                    if (t.IsCanceled) return;
                    if (t.IsFaulted) { ssStato.Text = "Errore: " + t.Exception.GetBaseException().Message; return; }
                    ssUltimo = t.Result;
                    ssUltimo.QuotaBase = hf ? h : double.NaN;
                    ssStato.Text = "Criterio: " + ssUltimo.Criterio + ".";
                    if (hf) MostraStackHf(ssUltimo, righe); else MostraStackVhf(ssUltimo, righe, col, z0);
                }));
        }

        Grid Tabella(List<string[]> righe, int evid = -1, int evid2 = -1)
        {
            Grid g = new Grid();
            int nc = righe[0].Length;
            for (int c = 0; c < nc; c++) g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            for (int r = 0; r < righe.Count; r++)
            {
                g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int c = 0; c < nc; c++)
                {
                    Border b = new Border
                    {
                        BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0.5), Padding = new Thickness(7, 3),
                        Background = Pal.B(r == 0 ? Pal.Panel2 : (r == evid ? Pal.A(60, Pal.Green) : (r == evid2 ? Pal.A(45, Pal.Violet) : Pal.Panel))),
                        Child = new TextBlock { Text = righe[r][c], FontWeight = r == 0 || r == evid ? FontWeight.SemiBold : FontWeight.Normal, Foreground = Pal.B(Pal.Fore), HorizontalAlignment = c == nc - 1 ? HorizontalAlignment.Left : HorizontalAlignment.Right }
                    };
                    Grid.SetRow(b, r); Grid.SetColumn(b, c);
                    g.Children.Add(b);
                }
            }
            return g;
        }

        void MostraStackVhf(StackStudioRisultato r, int righe, int col, double z0)
        {
            ssRis.Children.Clear();
            StackPunto p = r.Proposta, d = r.Dl6wu ?? r.Punti[0];
            WrapPanel cards = new WrapPanel();
            string dist = (righe > 1 ? "V " + FN(p.Dv, 3) + " m (" + FN(p.Dv / r.Lambda, 2) + " λ)" : "") + (righe > 1 && col > 1 ? ", " : "") + (col > 1 ? "H " + FN(p.Dh, 3) + " m (" + FN(p.Dh / r.Lambda, 2) + " λ)" : "");
            cards.Children.Add(Riquadretto("Distanza proposta", FN(p.K, 2) + " x DL6WU", dist, true));
            cards.Children.Add(Riquadretto("Stack alla distanza proposta", FN(p.G, 2) + " dBi", "lobi " + FN(p.Sll, 1) + " dB, G/T " + FN(p.Gt, 2) + " dB/K"));
            cards.Children.Add(Riquadretto("Alla distanza DL6WU", FN(d.G, 2) + " dBi", "lobi " + FN(d.Sll, 1) + " dB, G/T " + FN(d.Gt, 2) + " dB/K"));
            StackPunto smax = r.Punti.OrderByDescending(x => x.S21).First();
            cards.Children.Add(Riquadretto("Accoppiamento piu' forte", FN(smax.S21, 1) + " dB", "a " + FN(smax.K, 2) + " x DL6WU" + (smax.S21 > -20 ? ": sposta le impedenze" : "")));
            cards.Children.Add(Riquadretto("Antenna singola", FN(r.GSingola, 2) + " dBi", "lobo " + FN(r.SllSingola, 1) + " dB, G/T " + FN(r.GtSingola, 2) + " dB/K"));
            ssRis.Children.Add(cards);
            // matrice alla distanza proposta
            int np = r.SdB.GetLength(0);
            List<string[]> mt = new List<string[]>();
            string[] h0 = new string[np + 1]; h0[0] = "|S| dB a " + FN(z0, 0) + " Ω";
            for (int c = 0; c < np; c++) h0[c + 1] = "porta " + (c + 1);
            mt.Add(h0);
            for (int a = 0; a < np; a++)
            {
                string[] rr = new string[np + 1]; rr[0] = "porta " + (a + 1);
                for (int c = 0; c < np; c++) rr[c + 1] = FN(r.SdB[a, c], 1);
                mt.Add(rr);
            }
            List<string[]> zt = new List<string[]> { new[] { "Z (Ω)", "propria", "mutua con la porta 2" } };
            zt.Add(new[] { "porta 1", FN(r.Z[0, 0].Real, 1) + (r.Z[0, 0].Imaginary < 0 ? " -j" : " +j") + FN(Math.Abs(r.Z[0, 0].Imaginary), 1), np > 1 ? FN(r.Z[0, 1].Real, 1) + (r.Z[0, 1].Imaginary < 0 ? " -j" : " +j") + FN(Math.Abs(r.Z[0, 1].Imaginary), 1) : "-" });
            WrapPanel wm = new WrapPanel();
            Control m1 = Tabella(mt); m1.Margin = new Thickness(0, 0, 14, 6);
            wm.Children.Add(m1); wm.Children.Add(Tabella(zt));
            ssRis.Children.Add(Pannello("Matrice di accoppiamento alla distanza proposta", wm, "diagonale: disadattamento di ogni antenna con le altre chiuse su Z0; fuori diagonale: potenza che passa da una porta all'altra", "Distanze di stack: accoppiamento e distanza migliore"));
            // tabella delle distanze (1.3.1, bozzetto approvato da IC8TEM il 18/11/2026 13:04: riga della singola, F/R e variazione del F/R)
            List<string[]> rows = new List<string[]> { new[] { "x DL6WU", "distanza", "", "|Sij| max dB", "ROS attivo", "G dBi", "G realizzato", "sulla singola", "lobo laterale dB", "F/R dB", "ΔF/R", "G/T dB/K", "" } };
            rows.Add(new[] { Lang.T("singola"), "-", "", "-", FN(r.RosSingola, 2), FN(r.GSingola, 2), FN(r.GSingola - r.PerditaSingola, 2), "0", FN(r.SllSingola, 1), FN(r.FrSingola, 1), "0", FN(r.GtSingola, 2), Lang.T("riferimento") });
            int ie = -1, id = -1;
            for (int i = 0; i < r.Punti.Count; i++)
            {
                StackPunto q = r.Punti[i];
                List<string> tag = new List<string>();
                if (q == r.Dl6wu) { tag.Add("DL6WU"); id = i + 2; }
                if (q == r.GMax) tag.Add(Lang.T("guadagno max"));
                if (q == r.SllMin) tag.Add(Lang.T("lobi piu' bassi"));
                if (q == r.Punti.OrderByDescending(x => x.Fr).First()) tag.Add(Lang.T("F/R max"));
                if (q == r.GtMax) tag.Add(Lang.T("G/T max"));
                if (q == r.Proposta) { tag.Add(Lang.T("scelta proposta")); ie = i + 2; }
                if (q == r.RifMin) tag.Add(Lang.T("riflessi minimi"));
                if (q.Escluso) tag.Add(Lang.T("ROS attivo alto: da non scegliere"));
                double dd = righe > 1 ? q.Dv : q.Dh;
                rows.Add(new[] { FN(q.K, 2), FN(dd, 3) + " m", FN(dd / r.Lambda, 2) + " λ", FN(q.S21, 1), FN(q.RosAtt, 2), FN(q.G, 2), FN(q.Greal, 2), FN(q.G - r.GSingola, 2), FN(q.Sll, 1), FN(q.Fr, 1), FN(q.Fr - r.FrSingola, 1), FN(q.Gt, 2), string.Join(", ", tag) });
            }
            LineChart ch = new LineChart { Title = "", XLabel = Lang.T("distanza / distanza DL6WU"), YLabel = "dBi", Y2Label = "dB", Height = 260 };
            ch.X = r.Punti.Select(q => q.K).ToArray();
            ch.Series = new List<LineSeries>
            {
                new LineSeries { Name = Lang.T("guadagno dello stack (dBi)"), Y = r.Punti.Select(q => q.G).ToArray(), Color = Pal.Accent },
                new LineSeries { Name = Lang.T("lobo laterale piu' alto (dB)"), Y = r.Punti.Select(q => q.Sll).ToArray(), Color = Pal.Orange, Right = true, Decimals = 1 },
            };
            ch.MarkX = p.K;
            // grafico rispetto alla singola: a sinistra l'aumento di guadagno da 0 in su, a destra la variazione del F/R rovesciata
            // (0 in alto: dove lo stack migliora il F/R la curva scende); G/T solo con la casella
            int nAnt = righe * col;
            double ideale = 10 * Math.Log10(Math.Max(1, nAnt));
            double[] dfrInv = r.Punti.Select(q => -(q.Fr - r.FrSingola)).ToArray();
            LineChart ch2 = new LineChart { Title = "", XLabel = Lang.T("distanza / distanza DL6WU"), YLabel = "ΔG dB", Y2Label = "ΔF/R dB", Height = 280 };
            ch2.X = ch.X;
            ch2.YMinFixed = 0; ch2.YMaxFixed = Math.Ceiling(Math.Max(ideale, r.Punti.Max(q => q.G - r.GSingola)) + 1);
            ch2.Y2MaxFixed = Math.Max(1.5, Math.Ceiling(dfrInv.Max() + 0.5)); ch2.Y2MinFixed = Math.Floor(Math.Min(-1, dfrInv.Min()) - 0.5);
            ch2.MarkX = p.K;
            CheckBox cbGt = new CheckBox { Content = Lang.T("mostra anche il G/T (variazione sulla singola, asse di sinistra)"), IsChecked = false, Margin = new Thickness(0, 4, 0, 0) };
            Action serie2 = () =>
            {
                ch2.Series = new List<LineSeries>
                {
                    new LineSeries { Name = Lang.T("ΔG sulla singola"), Y = r.Punti.Select(q => q.G - r.GSingola).ToArray(), Color = Pal.Accent },
                    new LineSeries { Name = Lang.F("{0} antenne ideali", nAnt) + " +" + FN(ideale, 2) + " dB", Y = r.Punti.Select(q => ideale).ToArray(), Color = Pal.Muted, Dashed = true },
                    new LineSeries { Name = Lang.T("ΔF/R (0 in alto)"), Y = dfrInv, Color = Pal.Orange, Right = true, Decimals = 1 },
                };
                if (cbGt.IsChecked == true) ch2.Series.Add(new LineSeries { Name = Lang.T("ΔG/T"), Y = r.Punti.Select(q => q.Gt - r.GtSingola).ToArray(), Color = Pal.Green });
                ch2.InvalidateVisual();
            };
            serie2();
            cbGt.IsCheckedChanged += (s2, e2) => serie2();
            StackPanel grafici = new StackPanel { Spacing = 10 };
            // titoli fuori dai grafici: dentro si sovrapponevano alla legenda
            grafici.Children.Add(new TextBlock { Text = Lang.T("Guadagno e lobo laterale al variare della distanza"), FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore) });
            grafici.Children.Add(ch);
            grafici.Children.Add(new TextBlock { Text = Lang.T("Rispetto alla singola antenna"), FontSize = 14, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore) });
            grafici.Children.Add(ch2);
            grafici.Children.Add(cbGt);
            grafici.Children.Add(new TextBlock { Text = Lang.T("Asse di sinistra: G stack - G singola, da 0 in su. Asse di destra: F/R dello stack - F/R della singola, rovesciato, con lo 0 in alto: dove lo stack migliora il F/R la curva scende, e il punto piu' basso e' il miglioramento massimo. Il G/T resta nella tabella."), TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Pal.B(Pal.Muted) });
            Grid tg = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*") };
            Control tb = Tabella(rows, ie, id);
            Grid.SetColumn(tb, 0); Grid.SetColumn(grafici, 2);
            tg.Children.Add(tb); tg.Children.Add(grafici);
            ssRis.Children.Add(Pannello("Distanze provate", tg, null, "Distanze di stack: accoppiamento e distanza migliore"));
        }

        void MostraStackHf(StackStudioRisultato r, int righe)
        {
            ssRis.Children.Clear();
            StackPunto p = r.Proposta;
            WrapPanel cards = new WrapPanel();
            cards.Children.Add(Riquadretto("Distanza proposta", FN(p.Dv, 2) + " m = " + FN(p.K, 3) + " λ", "sopra a " + FN(r.QuotaBase + p.Dv * (ssN - 1), 1) + " m · " + r.Criterio, true));
            int ib = 0; for (int i = 1; i < r.ElBip.Length; i++) if (r.ElBip[i] > r.ElBip[ib]) ib = i;
            cards.Children.Add(Riquadretto("Stack, tutte in fase (BIP)", FN(r.ElBip[ib], 2) + " dBi a " + ib + "°", "a 5°: " + FN(r.ElBip[5], 1) + " dBi, a 15°: " + FN(r.ElBip[15], 1) + " dBi"));
            double gs = r.GSingola;
            cards.Children.Add(Riquadretto("Rispetto alla singola a " + FN(r.QuotaBase, 1) + " m", (r.ElBip[ib] - gs >= 0 ? "+" : "") + FN(r.ElBip[ib] - gs, 2) + " dB", "al massimo"));
            ssRis.Children.Add(cards);
            foreach (string nn in r.Note) ssRis.Children.Add(Nota(nn, Pal.Orange));
            List<string[]> rows = new List<string[]> { new[] { "distanza", "", "quota sopra", "G max", "a", "5°", "10°", "15°", "20°", "media voluta" } };
            int ie = -1;
            for (int i = 0; i < r.Punti.Count; i++)
            {
                StackPunto q = r.Punti[i];
                if (q == p) ie = i + 1;
                rows.Add(new[] { FN(q.K, 3) + " λ", FN(q.Dv, 2) + " m", FN(r.QuotaBase + q.Dv * (ssN - 1), 1) + " m", FN(q.G, 2), FN(q.SllAng, 0) + "°", FN(q.GAng[0], 1), FN(q.GAng[1], 1), FN(q.GAng[2], 1), FN(q.GAng[3], 1), FN(q.Gmedia, 2) });
            }
            LineChart ch = new LineChart { Title = "Elevazione alla distanza proposta (dBi sul terreno)", XLabel = "elevazione (gradi)", YLabel = "dBi", Height = 280, YMinFixed = -10 };
            int lim = 46;
            ch.X = r.ElAng.Take(lim).ToArray();
            ch.Series = new List<LineSeries>
            {
                new LineSeries { Name = "tutte in fase (BIP)", Y = r.ElBip.Take(lim).ToArray(), Color = Pal.Accent, Width = 2.5 },
                new LineSeries { Name = "solo sopra", Y = r.ElSopra.Take(lim).ToArray(), Color = Pal.Violet, Dashed = true },
                new LineSeries { Name = "solo sotto", Y = r.ElSotto.Take(lim).ToArray(), Color = Pal.Green, Dashed = true },
            };
            if (r.ElBop != null) ch.Series.Add(new LineSeries { Name = "controfase (BOP)", Y = r.ElBop.Take(lim).ToArray(), Color = Pal.Orange, Dashed = true });
            Grid tg = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*") };
            Control tb = Tabella(rows, ie);
            Grid.SetColumn(tb, 0); Grid.SetColumn(ch, 2);
            tg.Children.Add(tb); tg.Children.Add(ch);
            ssRis.Children.Add(Pannello("Distanze provate e diagramma in elevazione", tg));
        }

        void UsaPropostaStack()
        {
            StackStudioRisultato r = ssUltimo;
            if (r == null || r.Proposta == null || model == null || run == null) { ssStato.Text = "Calcola prima le distanze."; return; }
            try
            {
                bool hf = !double.IsNaN(r.QuotaBase);
                int righe = hf ? ssN : (ssCalc != null ? ssCalc.I("righe") : 2), col = hf ? 1 : (ssCalc != null ? ssCalc.I("col") : 1);
                NecModel b = model;
                if (hf)
                {
                    double zmin = TrasformaGeometria.ZMin(b.BuildGeometry());
                    b = TrasformaGeometria.Applica(b, TrasformaGeometria.Sposta(0, 0, r.QuotaBase - zmin), null);
                    if (b.Ground == null || b.Ground.Kind == GroundKind.FreeSpace) { b.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005, Sommerfeld = true, Name = "medio" }; b.GroundFlag = 1; }
                }
                List<string> note = new List<string>();
                NecModel nm = StackDetect.Crea(b, righe, col, r.Proposta.Dv, r.Proposta.Dh, run.PhiMax, note);
                string fn = model.FileName;
                SetModel(nm, null);
                model.FileName = string.IsNullOrEmpty(fn) ? "stack.nec" : Path.Combine(Path.GetDirectoryName(fn) ?? "", Path.GetFileNameWithoutExtension(fn) + " stack " + righe + "x" + col + ".nec");
                Registro.Scrivi("AZIONE", "stack creato con la distanza proposta: " + string.Join(" ", note));
                Status(string.Join(" ", note) + " Il file su disco non e' cambiato.");
                Calculate(true);
            }
            catch (Exception ex) { ssStato.Text = "Stack non creato: " + ex.Message; }
        }

        string TestoStack()
        {
            StackStudioRisultato r = ssUltimo;
            if (r == null) return "";
            StringBuilder s = new StringBuilder();
            s.AppendLine("YA-NEC - Distanze di stack (" + r.Criterio + ")");
            if (double.IsNaN(r.QuotaBase))
            {
                s.AppendLine("x DL6WU\tV m\tH m\t|Sij| max dB\tG dBi\tlobo dB\tG/T dB/K");
                foreach (StackPunto q in r.Punti) s.AppendLine(string.Join("\t", FN(q.K, 2), FN(q.Dv, 3), FN(q.Dh, 3), FN(q.S21, 1), FN(q.G, 2), FN(q.Sll, 1), FN(q.Gt, 2)));
            }
            else
            {
                s.AppendLine("lambda\tm\tG max dBi\ta gradi\tmedia voluta dBi");
                foreach (StackPunto q in r.Punti) s.AppendLine(string.Join("\t", FN(q.K, 3), FN(q.Dv, 2), FN(q.G, 2), FN(q.SllAng, 0), FN(q.Gmedia, 2)));
            }
            s.AppendLine("Proposta: " + FN(r.Proposta.K, 3) + (double.IsNaN(r.QuotaBase) ? " x DL6WU" : " lambda"));
            return s.ToString();
        }
    }
}
