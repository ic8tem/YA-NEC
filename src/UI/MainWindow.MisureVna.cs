// YA-NEC 1.2.0 - Diagrammi > "Misure VNA" (bozzetto VHF 11): ROS e carta di Smith del calcolo con le misure del VNA (.s1p o CSV),
// cavo tolto (de-embedding) e grezzo, tabella di confronto e proposta di correzione (il programma non cambia nulla da solo).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Action vnaAggiorna;

        Control BuildMisureVnaPage()
        {
            List<MisureVna.Misura> misure = new List<MisureVna.Misura>();
            double[] fCalc = null; Complex[] zCalc = null; NecModel calcPer = null; double fa = 0, fb = 0;
            CancellationTokenSource cts = null;
            Color[] colori = { Color.Parse("#D0342C"), Color.Parse("#21A04A"), Color.Parse("#8E44AD"), Color.Parse("#E08A00") };

            StackPanel sx = new StackPanel { Width = 262, Spacing = 2 };
            Fascione fT = new Fascione(); fT.Famiglia("MISURA");
            fT.Voce("s1p", "File Touchstone .s1p"); fT.Voce("csv", "CSV (NanoVNA-Saver e simili)");
            sx.Children.Add(fT);
            var riga = MeccRiga();
            TextBlock tFile = new TextBlock { Text = "-", TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Width = 130 };
            TextBlock tFmt = new TextBlock { Text = "-", VerticalAlignment = VerticalAlignment.Center }, tPunti = new TextBlock { Text = "-", VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Width = 130 };
            sx.Children.Add(riga("File", tFile, "")); sx.Children.Add(riga("Formato letto", tFmt, "")); sx.Children.Add(riga("Punti", tPunti, ""));
            sx.Children.Add(MeccFam("CAVO TRA VNA E ANTENNA"));
            CheckBox cDe = new CheckBox { Content = "Togli il cavo dalla misura (de-embedding)", IsChecked = false, Margin = new Thickness(2, 0) };
            sx.Children.Add(cDe);
            List<Cable> cavi = new List<Cable>();
            try { cavi = Cables.Load(Path.Combine(AppPaths.DataDir, "cavi.csv")); } catch { }
            ComboBox cCavo = new ComboBox { Width = 130, ItemsSource = cavi.Select(c => c.Name).ToList(), SelectedIndex = cavi.Count > 0 ? Math.Max(0, cavi.FindIndex(c => c.Name.Contains("Ecoflex 10"))) : -1 };
            TextBox tL = Tb("0", 70), tVf = Tb("", 70);
            sx.Children.Add(riga("Cavo", cCavo, "")); sx.Children.Add(riga("Lunghezza", tL, "m")); sx.Children.Add(riga("VF (vuoto = tabella)", tVf, ""));
            sx.Children.Add(MeccFam("MOSTRA"));
            CheckBox cRos = new CheckBox { Content = "ROS", IsChecked = true }, cSm = new CheckBox { Content = "Carta di Smith", IsChecked = true }, cRx = new CheckBox { Content = "R e X separate" }, cRl = new CheckBox { Content = "Perdita di ritorno (dB)" };
            foreach (CheckBox c in new[] { cRos, cSm, cRx, cRl }) { c.Margin = new Thickness(2, 0); sx.Children.Add(c); }
            TextBox tSoglia = Tb("1,5", 50);
            sx.Children.Add(riga("Banda con ROS <", tSoglia, ""));
            Button bApri = Btn("Apri misura", true), bAgg = Btn("Aggiungi altra", false);
            StackPanel rb = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(2, 12, 0, 0) }; rb.Children.Add(bApri); rb.Children.Add(bAgg);
            sx.Children.Add(rb);

            LineChart gRos = new LineChart { Height = 380, XLabel = "frequenza (MHz)", YLabel = "ROS", EmptyText = "apri una misura del VNA (.s1p o CSV)" };
            LineChart gRx = new LineChart { Height = 260, XLabel = "frequenza (MHz)", YLabel = "ohm", EmptyText = "", IsVisible = false };
            StackPanel cG = new StackPanel(); cG.Children.Add(gRos); cG.Children.Add(gRx);
            SmithChart sm = new SmithChart { Height = 420, Title = "Carta di Smith" };
            Grid due = new Grid { ColumnDefinitions = new ColumnDefinitions("1.3*,10,*"), Margin = new Thickness(0, 0, 0, 10) };
            Border b1 = Pannello("ROS: calcolato e misurato", cG), b2 = Pannello("Carta di Smith", sm);
            Grid.SetColumn(b2, 2); due.Children.Add(b1); due.Children.Add(b2);
            Grid tab = new Grid();
            TextBlock interp = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            StackPanel cT = new StackPanel(); cT.Children.Add(tab); cT.Children.Add(interp);
            StackPanel dx = new StackPanel(); dx.Children.Add(due); dx.Children.Add(Pannello("Confronto", cT));
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*"), Margin = new Thickness(4) };
            g.Children.Add(new ScrollViewer { Content = sx }); Grid.SetColumn(dx, 2); g.Children.Add(dx);

            Func<MisureVna.Misura, Complex[]> zAntenna = m =>
            {
                if (cDe.IsChecked != true || cCavo.SelectedIndex < 0 || cCavo.SelectedIndex >= cavi.Count) return m.Z;
                Cable c = cavi[cCavo.SelectedIndex];
                double vf = ParseNum(tVf.Text, c.Vf);
                return MisureVna.TogliCavo(m.F, m.Z, c.Z0, vf, f => c.AttenuationAt(f), ParseNum(tL.Text, 0));
            };
            Action mostra = () =>
            {
                double z0 = misure.Count > 0 ? misure[0].Z0 : 50, soglia = Math.Max(1.01, ParseNum(tSoglia.Text, 1.5));
                List<double> xs = new List<double>();
                if (fCalc != null) xs.AddRange(fCalc);
                foreach (var m in misure) xs.AddRange(m.F);
                double[] X = xs.Distinct().OrderBy(v => v).ToArray();
                Func<double[], Complex[], double[]> sul = (ff, zz) =>
                {
                    double[] y = new double[X.Length];
                    for (int i = 0; i < X.Length; i++)
                    {
                        double x = X[i];
                        if (ff == null || ff.Length < 2 || x < ff[0] - 1e-9 || x > ff[ff.Length - 1] + 1e-9) { y[i] = double.NaN; continue; }
                        int k = Array.BinarySearch(ff, x); if (k < 0) k = ~k; k = Math.Max(1, Math.Min(ff.Length - 1, k));
                        double t = (x - ff[k - 1]) / (ff[k] - ff[k - 1]);
                        Complex z = zz[k - 1] + (zz[k] - zz[k - 1]) * t;
                        y[i] = cRl.IsChecked == true ? -20 * Math.Log10(Math.Max(1e-9, Complex.Abs((z - z0) / (z + z0)))) : Analysis.Swr(z, z0);
                    }
                    return y;
                };
                gRos.YLabel = cRl.IsChecked == true ? "perdita di ritorno (dB)" : "ROS";
                gRos.X = X; gRos.Series = new List<LineSeries>();
                gRos.YMaxFixed = cRl.IsChecked == true ? double.NaN : 3.2; gRos.YMinFixed = cRl.IsChecked == true ? double.NaN : 1;
                if (fCalc != null) gRos.Series.Add(new LineSeries { Name = "calcolato (NEC)", Y = sul(fCalc, zCalc), Color = Color.Parse("#1D5FB0"), Width = 2.4, Decimals = 2 });
                for (int i = 0; i < misure.Count; i++)
                {
                    var m = misure[i]; Color col = colori[i % colori.Length];
                    if (cDe.IsChecked == true)
                    {
                        gRos.Series.Add(new LineSeries { Name = "misurato, cavo tolto" + (misure.Count > 1 ? " (" + m.Nome + ")" : ""), Y = sul(m.F, zAntenna(m)), Color = col, Width = 2.2, Decimals = 2 });
                        gRos.Series.Add(new LineSeries { Name = "misurato grezzo (con cavo)", Y = sul(m.F, m.Z), Color = Pal.A(110, col), Width = 1.6, Decimals = 2 });
                    }
                    else gRos.Series.Add(new LineSeries { Name = "misurato" + (misure.Count > 1 ? " (" + m.Nome + ")" : ""), Y = sul(m.F, m.Z), Color = col, Width = 2.2, Decimals = 2 });
                }
                gRos.MarkX = run != null ? run.FreqMHz : double.NaN;
                gRos.IsVisible = cRos.IsChecked == true || cRl.IsChecked == true; gRos.InvalidateVisual();
                // R e X
                gRx.IsVisible = cRx.IsChecked == true;
                if (gRx.IsVisible)
                {
                    gRx.X = X; gRx.Series = new List<LineSeries>();
                    Func<double[], Complex[], bool, double[]> comp = (ff, zz, im) => X.Select(x =>
                    {
                        if (ff == null || ff.Length < 2 || x < ff[0] || x > ff[ff.Length - 1]) return double.NaN;
                        int k = Array.BinarySearch(ff, x); if (k < 0) k = ~k; k = Math.Max(1, Math.Min(ff.Length - 1, k));
                        Complex z = zz[k - 1] + (zz[k] - zz[k - 1]) * ((x - ff[k - 1]) / (ff[k] - ff[k - 1]));
                        return im ? z.Imaginary : z.Real;
                    }).ToArray();
                    if (fCalc != null) { gRx.Series.Add(new LineSeries { Name = "R calcolata", Y = comp(fCalc, zCalc, false), Color = Color.Parse("#1D5FB0"), Width = 2 }); gRx.Series.Add(new LineSeries { Name = "X calcolata", Y = comp(fCalc, zCalc, true), Color = Color.Parse("#1D5FB0"), Width = 1.6, Dashed = true }); }
                    if (misure.Count > 0) { var m = misure[0]; Complex[] za = zAntenna(m); gRx.Series.Add(new LineSeries { Name = "R misurata", Y = comp(m.F, za, false), Color = colori[0], Width = 2 }); gRx.Series.Add(new LineSeries { Name = "X misurata", Y = comp(m.F, za, true), Color = colori[0], Width = 1.6, Dashed = true }); }
                    gRx.InvalidateVisual();
                }
                // Smith
                b2.IsVisible = cSm.IsChecked == true;
                sm.Z0 = z0; sm.F = fCalc ?? new double[0]; sm.Z = zCalc ?? new Complex[0]; sm.MarkF = run != null ? run.FreqMHz : double.NaN;
                sm.Altre = misure.Select((m, i) => Tuple.Create(zAntenna(m), colori[i % colori.Length])).ToList();
                sm.InvalidateVisual();
                // confronto con la prima misura
                tab.Children.Clear(); tab.RowDefinitions.Clear(); interp.Inlines.Clear();
                if (misure.Count == 0 || fCalc == null) return;
                double fr = run != null ? run.FreqMHz : (fCalc[0] + fCalc[fCalc.Length - 1]) / 2;
                var rc = MisureVna.Riassumi(fCalc, zCalc, z0, soglia, fr);
                var m0 = misure[0];
                var rm = MisureVna.Riassumi(m0.F, zAntenna(m0), z0, soglia, fr);
                double sc = (rm.FMin - rc.FMin) / rc.FMin * 100;
                tab.ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto");
                int rr = 0;
                Action<string[], bool, Color?> rg = (v, h, col) =>
                {
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int j = 0; j < v.Length; j++)
                    {
                        TextBlock t = new TextBlock { Text = v[j], FontWeight = h || (j == 3 && col.HasValue) ? FontWeight.SemiBold : FontWeight.Normal, HorizontalAlignment = j == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                        if (j == 3 && col.HasValue) t.Foreground = new SolidColorBrush(col.Value);
                        Border b = new Border { Child = t, Padding = new Thickness(10, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                        Grid.SetColumn(b, j); Grid.SetRow(b, rr); tab.Children.Add(b);
                    }
                    rr++;
                };
                Func<Complex, string> zs = z => FN(z.Real, 0) + (z.Imaginary >= 0 ? " + j" : " − j") + FN(Math.Abs(z.Imaginary), 0);
                rg(new[] { "", "Calcolato", "Misurato", "Scarto" }, true, null);
                rg(new[] { "Frequenza del ROS minimo", FN(rc.FMin, 3) + " MHz", FN(rm.FMin, 3) + " MHz", (sc >= 0 ? "+" : "") + FN(sc, 2) + " %" }, false, Math.Abs(sc) > 0.3 ? (Color?)Color.Parse("#D0342C") : null);
                rg(new[] { "ROS minimo", FN(rc.RosMin, 2), FN(rm.RosMin, 2), "" }, false, null);
                rg(new[] { "Banda con ROS < " + FN(soglia, 2), double.IsNaN(rc.BandaDa) ? "-" : FN(rc.BandaDa, 2) + "–" + FN(rc.BandaA, 2), double.IsNaN(rm.BandaDa) ? "-" : FN(rm.BandaDa, 2) + "–" + FN(rm.BandaA, 2), "" }, false, null);
                rg(new[] { "Z a " + FN(fr, 3) + " MHz", zs(rc.ZaF), zs(rm.ZaF), "" }, false, null);
                // interpretazione e proposta
                bool uniforme = !double.IsNaN(rc.BandaDa) && !double.IsNaN(rm.BandaDa) && Math.Abs(((rm.BandaDa - rc.BandaDa) / rc.BandaDa - (rm.BandaA - rc.BandaA) / rc.BandaA) * 100) < Math.Max(0.15, Math.Abs(sc) / 2);
                string testo = "Interpretazione: la misura e' spostata " + (sc < 0 ? "in basso" : "in alto") + " di " + FN(Math.Abs(sc), 2) + " %" + (uniforme ? ", uguale su tutta la banda → probabile effetto di boom/staffe o dielettrico non modellato" : ", in modo non uniforme → controlla adattamento, balun e cavo (la Z misurata ha una forma diversa)") + ". ";
                string prop = "";
                List<Tolleranze.Elemento> el = MeccElementi();
                var dr = el.FirstOrDefault(e => e.Alimentato);
                if (Math.Abs(sc) < 0.05) prop = "nessuna correzione: calcolo e misura coincidono.";
                else if (dr != null) prop = (sc < 0 ? "accorciare" : "allungare") + " il radiatore di " + FN(Math.Abs(sc) / 100 * dr.Lung * 1000 / 2, 1) + " mm per lato, oppure attivare la correzione del boom (Calcolatori > Correzione del boom) e ricalcolare. Il programma non cambia nulla da solo.";
                else prop = "scalare il modello del " + FN(-sc, 2) + " % (Struttura 3D > Scala) e ricalcolare. Il programma non cambia nulla da solo.";
                if (rm.RosMin > rc.RosMin + 0.15) testo += "Il ROS minimo misurato e' piu' alto del calcolato: la parte resistiva non torna (adattamento, perdite del balun o del cavo). ";
                interp.Inlines.Add(new Avalonia.Controls.Documents.Run { Text = testo });
                interp.Inlines.Add(new Avalonia.Controls.Documents.Run { Text = "Proposta: ", FontWeight = FontWeight.Bold });
                interp.Inlines.Add(new Avalonia.Controls.Documents.Run { Text = prop });
            };
            Func<Task> calcolaSulCampo = async () =>
            {
                if (run == null || misure.Count == 0) { mostra(); return; }
                double a = misure.Min(m => m.F[0]), b = misure.Max(m => m.F[m.F.Length - 1]);
                if (zCalc != null && calcPer == model && Math.Abs(a - fa) < 1e-9 && Math.Abs(b - fb) < 1e-9) { mostra(); return; }
                cts?.Cancel(); cts = new CancellationTokenSource(); CancellationToken ct = cts.Token;
                List<double> fs = new List<double>(); for (int i = 0; i <= 60; i++) fs.Add(a + (b - a) * i / 60);
                NecModel m0 = model; SegmentGeometry geo = run.Geometry; double th = run.ThetaMax, ph = run.PhiMax;
                gRos.EmptyText = "calcolo NEC sul campo della misura…";
                try
                {
                    var sw = await Task.Run(() => Analysis.Sweep(m0, geo, fs, th, ph, null, ct), ct);
                    var ok = sw.Where(p => p.Ok).ToList();
                    fCalc = ok.Select(p => p.F).ToArray(); zCalc = ok.Select(p => p.Z).ToArray(); calcPer = m0; fa = a; fb = b;
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { Status("Misure VNA: " + ex.Message, true); }
                mostra();
            };
            Func<bool, Task> apri = async aggiungi =>
            {
                try
                {
                    bool csv = fT.Current == "csv";
                    var fl = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Misura del VNA", AllowMultiple = false,
                        FileTypeFilter = new[] { csv ? new FilePickerFileType("CSV") { Patterns = new[] { "*.csv", "*.CSV", "*.txt" } } : new FilePickerFileType("Touchstone") { Patterns = new[] { "*.s1p", "*.S1P" } }, new FilePickerFileType("Tutti") { Patterns = new[] { "*" } } }
                    });
                    if (fl == null || fl.Count == 0) return;
                    MisureVna.Misura m = MisureVna.Leggi(fl[0].TryGetLocalPath());
                    if (!aggiungi) misure.Clear();
                    misure.Add(m);
                    tFile.Text = m.Nome; tFmt.Text = m.Formato; tPunti.Text = m.F.Length + " · " + FN(m.F[0], 1) + "–" + FN(m.F[m.F.Length - 1], 1) + " MHz";
                    Registro.Scrivi("AZIONE", "misura del VNA: " + m.Nome + ", " + m.F.Length + " punti");
                    await calcolaSulCampo();
                }
                catch (Exception ex) { Status("Misura del VNA: " + ex.Message, true); }
            };
            vnaAggiorna = () => { zCalc = null; _ = calcolaSulCampo(); };
            bApri.Click += async (s, e) => await apri(false);
            bAgg.Click += async (s, e) => await apri(true);
            foreach (CheckBox c in new[] { cDe, cRos, cSm, cRx, cRl }) c.IsCheckedChanged += (s, e) => mostra();
            cCavo.SelectionChanged += (s, e) => mostra();
            tL.LostFocus += (s, e) => mostra(); tVf.LostFocus += (s, e) => mostra(); tSoglia.LostFocus += (s, e) => mostra();
            fT.Select("s1p", false);
            return new ScrollViewer { Content = g };
        }
    }
}
