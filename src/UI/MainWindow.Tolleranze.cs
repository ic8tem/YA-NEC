// YA-NEC 1.2.0 - Diagrammi > "Tolleranze" (bozzetto VHF 9): Monte Carlo con seme fisso o sensibilita' rapida, istogramma del F/B,
// sensibilita' per elemento e lista di taglio con la tolleranza ammessa calcolata dagli obiettivi (calo massimo del F/B, ROS massimo).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        Action tollAggiorna;

        Control BuildTolleranzePage()
        {
            string metodo = "mc";
            Tolleranze.Esito esito = null;
            CancellationTokenSource cts = null;

            // ---------------- sinistra ----------------
            StackPanel sx = new StackPanel { Width = 262, Spacing = 2 };
            Fascione fMet = new Fascione(); fMet.Famiglia("METODO");
            fMet.Voce("mc", "Monte Carlo"); fMet.Voce("sens", "Sensibilità rapida (un elemento alla volta)");
            sx.Children.Add(fMet);
            Func<string, TextBlock> fam = s => new TextBlock { Text = s, FontWeight = FontWeight.Bold, FontSize = 13, Foreground = new SolidColorBrush(Fascione.FamigliaTesto), Margin = new Thickness(2, 10, 0, 4) };
            Func<string, Control, string, Grid> riga = (et, c, um) =>
            {
                Grid g0 = new Grid { ColumnDefinitions = new ColumnDefinitions("100,Auto,Auto"), Margin = new Thickness(2, 1) };
                g0.Children.Add(new TextBlock { Text = et, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
                Grid.SetColumn(c, 1); g0.Children.Add(c);
                TextBlock u = new TextBlock { Text = um, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };
                Grid.SetColumn(u, 2); g0.Children.Add(u);
                return g0;
            };
            sx.Children.Add(fam("TOLLERANZE DI COSTRUZIONE"));
            TextBox tL = Tb("1,0", 70), tP = Tb("2,0", 70), tD = Tb("0,1", 70);
            sx.Children.Add(riga("Lunghezza elementi", tL, "± mm")); sx.Children.Add(riga("Posizione sul boom", tP, "± mm")); sx.Children.Add(riga("Diametro", tD, "± mm"));
            ComboBox cDist = new ComboBox { ItemsSource = new[] { "Uniforme", "Gaussiana (t = 2σ)" }, SelectedIndex = 0, Width = 140 };
            sx.Children.Add(riga("Distribuzione", cDist, ""));
            CheckBox cFor = new CheckBox { Content = "Includi foratura del boom (posizione)", IsChecked = true, Margin = new Thickness(2, 0) };
            CheckBox cSta = new CheckBox { Content = "Includi errore della staffa (isolatori)", Margin = new Thickness(2, 0) };
            FumettoGiallo(cSta, "Elemento montato fuori centro sulla staffa: spostamento lungo il proprio asse, con la tolleranza della posizione.", "la staffa reale cambia anche la lunghezza elettrica vicino al boom: per quella usa la linguetta Correzione del boom.", "Tolleranze");
            sx.Children.Add(cFor); sx.Children.Add(cSta);
            sx.Children.Add(fam("PROVE"));
            TextBox tN = Tb("500", 70), tF = Tb("", 100);
            ComboBox cSeme = new ComboBox { ItemsSource = new[] { "fisso (ripetibile)", "nuovo a ogni avvio" }, SelectedIndex = 0, Width = 140 };
            sx.Children.Add(riga("Numero prove", tN, "")); sx.Children.Add(riga("Frequenze", tF, "MHz")); sx.Children.Add(riga("Seme casuale", cSeme, ""));
            sx.Children.Add(fam("OBIETTIVO LISTA DI TAGLIO"));
            TextBox tCalo = Tb("1,0", 70), tRos = Tb("1,3", 70);
            sx.Children.Add(riga("Calo F/B max", tCalo, "dB")); sx.Children.Add(riga("ROS max", tRos, ""));
            Button bAvvia = Btn("Avvia (≈ 500 calcoli NEC)", true); bAvvia.Margin = new Thickness(2, 12, 0, 0);
            Button bFerma = Btn("Ferma", false); bFerma.Margin = new Thickness(2, 6, 0, 0); bFerma.IsVisible = false;
            ProgressBar barra = new ProgressBar { Height = 6, Margin = new Thickness(2, 6, 0, 0), IsVisible = false };
            sx.Children.Add(bAvvia); sx.Children.Add(bFerma); sx.Children.Add(barra);

            // ---------------- destra ----------------
            Grid carte = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*,10,*,10,*") };
            Disegno dIsto = new Disegno { Height = 300 }, dSens = new Disegno { Height = 300 };
            TextBlock titIsto = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) };
            StackPanel cI = new StackPanel { Spacing = 4 }; cI.Children.Add(titIsto); cI.Children.Add(dIsto);
            StackPanel cS = new StackPanel { Spacing = 4 }; cS.Children.Add(new TextBlock { Text = "Sensibilità per elemento (calo di F/B per 1 mm di errore)", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) }); cS.Children.Add(dSens);
            Func<Control, Border> riq = c => new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Child = c };
            Grid due = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*"), Margin = new Thickness(0, 10, 0, 10) };
            Border b1 = riq(cI), b2 = riq(cS); Grid.SetColumn(b2, 2); due.Children.Add(b1); due.Children.Add(b2);
            Grid lista = new Grid();
            Button bPdf = Btn("Stampa lista di taglio", false), bCsv = Btn("Esporta CSV", false);
            WrapPanel piede = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            piede.Children.Add(bPdf); bCsv.Margin = new Thickness(6, 0, 10, 0); piede.Children.Add(bCsv);
            piede.Children.Add(new TextBlock { Text = "Gli elementi vicini al radiatore sono di solito i più critici; quelli in fondo al boom tollerano errori più grandi.", Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
            StackPanel cL = new StackPanel(); cL.Children.Add(new TextBlock { Text = "Lista di taglio con tolleranza ammessa", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6) });
            cL.Children.Add(lista); cL.Children.Add(piede);
            StackPanel avvisi = new StackPanel { Spacing = 4, Margin = new Thickness(0, 8, 0, 0) };
            cL.Children.Add(avvisi);
            StackPanel dx = new StackPanel();
            dx.Children.Add(riq(carte)); dx.Children.Add(due); dx.Children.Add(riq(cL));

            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*"), Margin = new Thickness(4) };
            g.Children.Add(sx); Grid.SetColumn(dx, 2); g.Children.Add(dx);

            Func<string, string, bool, Border> carta = (v, s, ev) =>
            {
                StackPanel sp = new StackPanel { Spacing = 2 };
                sp.Children.Add(new TextBlock { Text = v, FontSize = 24, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Accent) });
                sp.Children.Add(new TextBlock { Text = s, FontSize = 12.5, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap });
                return new Border { Child = sp, Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8) };
            };

            // ---------------- disegni ----------------
            dIsto.Disegna = (ctx, rr) =>
            {
                if (esito == null || esito.Prove.Count == 0) { Draw.TextC(ctx, metodo == "mc" ? "premi Avvia" : "l'istogramma serve al Monte Carlo", rr.Width / 2, rr.Height / 2, 13, Pal.Muted); return; }
                List<double> fb = esito.Prove.Select(p => p.Fb).Where(x => !double.IsNaN(x)).ToList();
                double a = Math.Floor(Math.Min(fb.Min(), esito.Nominale.Fb) - 1), b = Math.Ceiling(Math.Max(fb.Max(), esito.Nominale.Fb) + 1);
                int nb = 28; double w = (b - a) / nb;
                int[] h = new int[nb]; foreach (double v in fb) h[Math.Min(nb - 1, (int)((v - a) / w))]++;
                double hm = Math.Max(1, h.Max());
                double L = 46, R = rr.Width - 12, T = 14, B = rr.Height - 38;
                Pen gp = new Pen(Pal.B(Pal.ChartGrid), 1);
                for (int k = 0; k <= 4; k++) { double y = B - (B - T) * k / 4; ctx.DrawLine(gp, new Point(L, y), new Point(R, y)); Draw.TextR(ctx, FN(hm * k / 4, 0), L - 5, y - 7, 11, Pal.Muted); }
                for (int i = 0; i < nb; i++)
                {
                    double x0 = L + (R - L) * i / nb, x1 = L + (R - L) * (i + 1) / nb, y = B - (B - T) * h[i] / hm;
                    ctx.DrawRectangle(new SolidColorBrush(Color.Parse("#8DB4E8")), new Pen(new SolidColorBrush(Color.Parse("#1D5FB0")), 1), new Rect(x0 + 1, y, x1 - x0 - 2, B - y));
                }
                double passo = b - a > 20 ? 4 : 2;
                for (double v = Math.Ceiling(a / passo) * passo; v <= b; v += passo) Draw.TextC(ctx, FN(v, 0), L + (R - L) * (v - a) / (b - a), B + 10, 11, Pal.Muted);
                Draw.TextC(ctx, "F/B (dB)", (L + R) / 2, B + 26, 11.5, Pal.Muted);
                Action<double, Color, string> linea = (v, c, s) =>
                {
                    double x = L + (R - L) * (v - a) / (b - a);
                    ctx.DrawLine(new Pen(new SolidColorBrush(c), 1.6, new DashStyle(new double[] { 4, 3 }, 0)), new Point(x, T), new Point(x, B));
                    Draw.Text(ctx, s, x + 3, T, 11.5, c);
                };
                linea(esito.Fb5, Color.Parse("#D0342C"), "5% peggiore");
                linea(esito.Nominale.Fb, Color.Parse("#21A04A"), "nominale");
            };
            dSens.Disegna = (ctx, rr) =>
            {
                List<Tolleranze.Elemento> el = esito?.Elementi.Where(e => !e.Alimentato && !double.IsNaN(e.Sens)).ToList();
                if (el == null || el.Count == 0) { Draw.TextC(ctx, "premi Avvia", rr.Width / 2, rr.Height / 2, 13, Pal.Muted); return; }
                double sm = Math.Max(0.1, el.Max(e => e.Sens)), top = Math.Ceiling(sm * 2) / 2;
                double L = 40, R = rr.Width - 10, T = 12, B = rr.Height - 40;
                Pen gp = new Pen(Pal.B(Pal.ChartGrid), 1);
                for (int k = 0; k <= 4; k++) { double y = B - (B - T) * k / 4; ctx.DrawLine(gp, new Point(L, y), new Point(R, y)); Draw.TextR(ctx, FN(top * k / 4, 1), L - 5, y - 7, 11, Pal.Muted); }
                double tolL = ParseNum(tL.Text, 1);
                HashSet<string> critici = new HashSet<string>(el.OrderByDescending(e => e.Sens).Take(2).Select(e => e.Nome));
                double bw = (R - L) / el.Count;
                for (int i = 0; i < el.Count; i++)
                {
                    double y = B - (B - T) * el[i].Sens / top;
                    bool cr = critici.Contains(el[i].Nome) || (!double.IsNaN(el[i].TollAmmessa) && el[i].TollAmmessa < tolL);
                    ctx.FillRectangle(new SolidColorBrush(Color.Parse(cr ? "#D0342C" : "#1D5FB0")), new Rect(L + i * bw + bw * 0.15, y, bw * 0.7, B - y));
                    Draw.TextC(ctx, el[i].Nome, L + i * bw + bw / 2, B + 10, 10.5, Pal.Muted);
                }
                Draw.TextC(ctx, "elemento", (L + R) / 2, B + 26, 11.5, Pal.Muted);
            };

            Action mostra = () =>
            {
                carte.Children.Clear();
                if (esito == null) { dIsto.InvalidateVisual(); dSens.InvalidateVisual(); return; }
                Tolleranze.Prova n = esito.Nominale;
                bool mc = esito.Prove.Count > 0;
                string fc = FN(ParseNum(tF.Text.Split('–', '-').Last(), run?.FreqMHz ?? 0), 1);
                Border[] c =
                {
                    carta(FN(mc ? esito.G5 : n.G, 1) + " dBi", "guadagno: nominale " + FN(n.G, 1) + (mc ? " · 5% peggiore " + FN(esito.G5, 1) : ""), true),
                    carta(FN(mc ? esito.Fb5 : n.Fb, 1) + " dB", "F/B: nominale " + FN(n.Fb, 1) + (mc ? " · 5% peggiore " + FN(esito.Fb5, 1) : ""), true),
                    carta(FN(mc ? esito.Ros95 : n.Ros, 2), mc ? "ROS nella banda: 95% delle prove sotto " + FN(esito.Ros95, 2) : "ROS nominale (peggiore nella banda)", true),
                    carta(esito.Critici.Length > 0 ? esito.Critici : "-", "elementi più critici (vedi sensibilità)", true)
                };
                for (int i = 0; i < 4; i++) { Grid.SetColumn(c[i], 2 * i); carte.Children.Add(c[i]); }
                titIsto.Text = mc ? "Distribuzione del F/B sulle " + esito.Prove.Count + " prove" : "Distribuzione del F/B";
                // lista di taglio a due colonne
                lista.Children.Clear(); lista.RowDefinitions.Clear();
                lista.ColumnDefinitions = new ColumnDefinitions("Auto,*,*,*,Auto,20,Auto,*,*,*,Auto");
                List<Tolleranze.Elemento> el = esito.Elementi;
                int meta = (el.Count + 1) / 2;
                double tolL = ParseNum(tL.Text, 1);
                Action<int, int, string, bool, Color?> cella = (r, col, s, h, colr) =>
                {
                    TextBlock t = new TextBlock { Text = s, FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal, HorizontalAlignment = col % 6 == 0 || col % 6 == 4 ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                    if (colr.HasValue) t.Foreground = new SolidColorBrush(colr.Value);
                    Border b = new Border { Child = t, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                    Grid.SetRow(b, r); Grid.SetColumn(b, col); lista.Children.Add(b);
                };
                for (int r = 0; r <= meta; r++) lista.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                foreach (int off in new[] { 0, 6 })
                {
                    string[] tit = { "Elem.", "Posizione mm", "Lunghezza mm", "Tolleranza ammessa", "Nota" };
                    for (int j = 0; j < 5; j++) cella(0, off + j, tit[j], true, null);
                }
                for (int i = 0; i < el.Count; i++)
                {
                    int r = 1 + i % meta, off = i < meta ? 0 : 6;
                    Tolleranze.Elemento e = el[i];
                    bool cr = !e.Alimentato && !double.IsNaN(e.TollAmmessa) && e.TollAmmessa < tolL;
                    Color? rosso = cr ? (Color?)Color.Parse("#D0342C") : null;
                    cella(r, off, e.Nome, false, null);
                    cella(r, off + 1, FN(e.Pos * 1000, 0), false, null);
                    cella(r, off + 2, e.Alimentato ? "—" : FN(e.Lung * 1000, 0), false, null);
                    cella(r, off + 3, e.Alimentato ? "—" : double.IsNaN(e.TollAmmessa) ? "" : "± " + FN(e.TollAmmessa, 1) + (e.TollAmmessa >= 10 ? "+" : ""), cr, rosso);
                    cella(r, off + 4, e.Alimentato ? "radiatore" : cr ? "critico" : "", cr, rosso);
                }
                avvisi.Children.Clear();
                avvisi.Children.Add(AvvisoGiallo("Tolleranza ammessa: errore di lunghezza (in piu' o in meno) che da solo fa calare il F/B di " + FN(ParseNum(tCalo.Text, 1) / Math.Sqrt(Math.Max(1, el.Count(e => !e.Alimentato))), 2) +
                    " dB (il calo massimo diviso per la radice del numero degli elementi: errori indipendenti) e non porta il ROS sopra " + FN(Math.Max(ParseNum(tRos.Text, 1.3), double.IsNaN(esito.Nominale.Ros) ? 0 : esito.Nominale.Ros + 0.05), 2) + (esito.Nominale.Ros > ParseNum(tRos.Text, 1.3) ? " (il ROS nominale supera gia' l'obiettivo: conta solo l'aumento)" : "") + ". In rosso gli elementi che chiedono meno della tolleranza di costruzione."));
                dIsto.InvalidateVisual(); dSens.InvalidateVisual();
            };

            tollAggiorna = () =>
            {
                if (run != null && string.IsNullOrWhiteSpace(tF.Text))
                {
                    double f = run.FreqMHz, d = f >= 30 ? Math.Max(0.1, Math.Round(f * 0.0014 * 10) / 10) : Math.Round(f * 0.007 * 100) / 100;
                    tF.Text = FN(f - d, f >= 30 ? 1 : 2) + "–" + FN(f + d, f >= 30 ? 1 : 2);
                }
                mostra();
            };
            fMet.Selected += k => { metodo = k; bAvvia.Content = k == "mc" ? "Avvia (≈ " + FN(ParseNum(tN.Text, 500), 0) + " calcoli NEC)" : "Avvia (sensibilità)"; mostra(); };
            tN.LostFocus += (s, e) => { if (metodo == "mc") bAvvia.Content = "Avvia (≈ " + FN(ParseNum(tN.Text, 500), 0) + " calcoli NEC)"; };
            bFerma.Click += (s, e) => cts?.Cancel();
            bAvvia.Click += async (s, e) =>
            {
                if (run == null) { Status("Calcola prima il modello", true); return; }
                tollAggiorna();
                string[] fp = (tF.Text ?? "").Split(new[] { '–', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
                double f1 = fp.Length > 0 ? ParseNum(fp[0], run.FreqMHz) : run.FreqMHz, f2 = fp.Length > 1 ? ParseNum(fp[1], f1) : f1;
                Tolleranze.Opzioni o = new Tolleranze.Opzioni
                {
                    TolL = ParseNum(tL.Text, 1), TolP = ParseNum(tP.Text, 2), TolD = ParseNum(tD.Text, 0.1), Gauss = cDist.SelectedIndex == 1,
                    Foratura = cFor.IsChecked == true, Staffa = cSta.IsChecked == true, Prove = Math.Max(10, Math.Min(5000, (int)ParseNum(tN.Text, 500))),
                    F1 = Math.Min(f1, f2), F2 = Math.Max(f1, f2), Seme = cSeme.SelectedIndex == 0 ? 12345 : Environment.TickCount, CaloFbMax = ParseNum(tCalo.Text, 1), RosMax = ParseNum(tRos.Text, 1.3)
                };
                NecModel m = model; SegmentGeometry geo = run.Geometry;
                double th = run.ThetaMax, ph = run.PhiMax;
                int asse; List<Tolleranze.Elemento> el = Tolleranze.Elementi(m, geo, out asse);
                if (el.Count < 2) { Status("Tolleranze: servono almeno due elementi diritti (un filo per elemento)", true); return; }
                cts = new CancellationTokenSource(); CancellationToken ct = cts.Token;
                bAvvia.IsEnabled = false; bFerma.IsVisible = true; barra.IsVisible = true;
                int tot = el.Count + (metodo == "mc" ? o.Prove : 0);
                barra.Maximum = tot; barra.Value = 0;
                int base0 = 0;
                Progress<int> pr = new Progress<int>(v => barra.Value = base0 + v);
                try
                {
                    Tolleranze.Esito r = await Task.Run(() =>
                    {
                        Tolleranze.Esito r0;
                        if (metodo == "mc") r0 = Tolleranze.MonteCarlo(m, geo, el, asse, o, th, ph, pr, ct);
                        else
                        {
                            r0 = new Tolleranze.Esito { Elementi = el };
                            r0.Nominale = Tolleranze.MonteCarlo(m, geo, el, asse, new Tolleranze.Opzioni { Prove = 0, F1 = o.F1, F2 = o.F2 }, th, ph, null, ct).Nominale;
                        }
                        base0 = metodo == "mc" ? o.Prove : 0;
                        Tolleranze.Sensibilita(m, geo, el, asse, o, th, ph, r0.Nominale, pr, ct);
                        Tolleranze.Riassumi(r0);
                        return r0;
                    }, ct);
                    esito = r;
                    Registro.Scrivi("AZIONE", "tolleranze: " + (metodo == "mc" ? o.Prove + " prove Monte Carlo" : "sensibilita'") + ", F/B nominale " + FN(r.Nominale.Fb, 1) + " dB, 5% peggiore " + FN(r.Fb5, 1));
                    mostra();
                }
                catch (OperationCanceledException) { Status("Tolleranze: fermato"); }
                catch (Exception ex) { Status("Tolleranze: " + ex.Message, true); }
                finally { bAvvia.IsEnabled = true; bFerma.IsVisible = false; barra.IsVisible = false; }
            };
            Func<List<string[]>> righeLista = () =>
            {
                List<string[]> l = new List<string[]> { new[] { "Elem.", "Posizione mm", "Lunghezza mm", "Diametro mm", "Tolleranza ammessa mm", "Nota" } };
                double tolL = ParseNum(tL.Text, 1);
                foreach (Tolleranze.Elemento e in esito.Elementi)
                    l.Add(new[] { e.Nome, FN(e.Pos * 1000, 0), e.Alimentato ? "-" : FN(e.Lung * 1000, 0), FN(e.Diam * 1000, 1), e.Alimentato || double.IsNaN(e.TollAmmessa) ? "-" : "± " + FN(e.TollAmmessa, 1),
                        e.Alimentato ? "radiatore" : e.TollAmmessa < tolL ? "critico" : "" });
                return l;
            };
            bCsv.Click += async (s, e) =>
            {
                if (esito == null) { Status("Avvia prima il calcolo", true); return; }
                try
                {
                    IStorageFile fl = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Esporta la lista di taglio", SuggestedFileName = "lista_di_taglio.csv", DefaultExtension = "csv", FileTypeChoices = new[] { new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } } } });
                    if (fl == null) return;
                    using (Stream st = await fl.OpenWriteAsync()) using (StreamWriter w = new StreamWriter(st)) foreach (string[] r in righeLista()) w.WriteLine(string.Join(";", r));
                    Status("Lista di taglio esportata: " + fl.Name);
                }
                catch (Exception ex) { Status("Esporta CSV: " + ex.Message, true); }
            };
            bPdf.Click += async (s, e) =>
            {
                if (esito == null) { Status("Avvia prima il calcolo", true); return; }
                try
                {
                    string nomeMod = string.IsNullOrEmpty(model.FileName) ? "" : Path.GetFileNameWithoutExtension(model.FileName);
                    SchedaPdf pdf = new SchedaPdf("Lista di taglio" + (nomeMod.Length > 0 ? " - " + nomeMod : ""), "YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture));
                    pdf.Sezione("Obiettivi");
                    pdf.Dato("Frequenze", tF.Text + " MHz");
                    pdf.Dato("Calo massimo del F/B", tCalo.Text + " dB");
                    pdf.Dato("ROS massimo", tRos.Text);
                    pdf.Dato("Tolleranze di costruzione", "lunghezza ± " + tL.Text + " mm, posizione ± " + tP.Text + " mm, diametro ± " + tD.Text + " mm");
                    pdf.Sezione("Elementi");
                    pdf.Tabella(righeLista(), new float[] { 40, 80, 80, 70, 110, 70 });
                    IStorageFile fl = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Stampa la lista di taglio (PDF)", SuggestedFileName = "Lista di taglio" + (nomeMod.Length > 0 ? " " + nomeMod : "") + ".pdf", DefaultExtension = "pdf", FileTypeChoices = new[] { new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } } });
                    if (fl == null) return;
                    using (Stream st = await fl.OpenWriteAsync()) pdf.Scrivi(st);
                    Status("Lista di taglio in PDF: " + fl.Name);
                }
                catch (Exception ex) { Status("Lista di taglio: " + ex.Message, true); }
            };
            fMet.Select("mc", false);
            tollAggiorna();
            return new ScrollViewer { Content = g };
        }
    }
}
