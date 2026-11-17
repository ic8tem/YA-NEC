// YA-NEC 1.2.0 - Oggetti inseriti > Cavo e balun (bozzetto VHF 10 rev. 2, scheda unica HF-UHF che comprende il bozzetto HF 5).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Control PannelloCavoBalun()
        {
            var riga = MeccRiga();
            StackPanel sx = new StackPanel { Width = 262, Spacing = 2 };
            sx.Children.Add(MeccFam("BANDA"));
            TextBox tF = Tb(run != null ? FN(run.FreqMHz, 3) : FN(model.FreqMHz, 3), 90);
            sx.Children.Add(riga("Frequenza", tF, "MHz"));
            sx.Children.Add(new TextBlock { Text = "da 1,8 MHz a 1296 MHz: l'elenco dei balun cambia con la banda", FontSize = 11.5, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 0) });
            sx.Children.Add(MeccFam("LINEA DI DISCESA"));
            ComboBox cTipo = new ComboBox { Width = 120, ItemsSource = new[] { "Coassiale", "Linea aperta (HF)" }, SelectedIndex = 0 };
            ToolTip.SetTip(cTipo, "Con la linea aperta bilanciata non c'e' calza: il modo comune nasce solo dagli squilibri. La scheda calcola il caso coassiale; per la linea aperta usa Linee (TL) con l'accordatore.");
            TextBox tBoom = Tb("0,5", 60), tPalo = Tb("3,0", 60), tSt = Tb("0", 60);
            sx.Children.Add(riga("Tipo", cTipo, "")); sx.Children.Add(riga("Lungo il boom", tBoom, "m → palo")); sx.Children.Add(riga("Discesa sul palo", tPalo, "m")); sx.Children.Add(riga("Fino alla stazione", tSt, "m"));
            CheckBox cMassa = new CheckBox { Content = "Calza a massa sul palo / traliccio", IsChecked = true }, cTerra = new CheckBox { Content = "Messa a terra all'ingresso stazione", IsChecked = true };
            foreach (CheckBox c in new[] { cMassa, cTerra }) { c.Margin = new Thickness(2, 0); sx.Children.Add(c); }
            sx.Children.Add(MeccFam("BALUN DA CONFRONTARE"));
            Dictionary<string, CheckBox> cB = new Dictionary<string, CheckBox>();
            foreach (var t in CavoBalun.Tipi) { CheckBox c = new CheckBox { Content = t.Nome, IsChecked = t.Chiave == "nessuno" || t.Chiave == "mezzaonda" || t.Chiave == "manicotto" || t.Chiave == "ferriti", Margin = new Thickness(2, 0) }; cB[t.Chiave] = c; sx.Children.Add(c); }
            FumettoGiallo(cB["choke"], "8 spire di RG-213 su 110 mm: induttanza di Wheeler, capacita' propria di Medhurst.", "Q 50 e capacita' propria stimata: l'impedenza vera del choke va misurata.", "Cavo e balun");
            FumettoGiallo(cB["toroide"], "Toroide 1:1 in ferrite come RLC parallelo a Q basso.", "5 kOhm al picco (10 MHz con il mix 31, 25 MHz con il mix 43): valori tipici di 12 spire su 2 x FT-240.", "Cavo e balun");
            FumettoGiallo(cB["mezzaonda"], "λ/2 coassiale 4:1: alimentazione bilanciata, calze unite.", "modo comune residuo come 600 ohm risonanti a f0 (Q 3): dipende dal montaggio.", "Cavo e balun");
            FumettoGiallo(cB["manicotto"], "Manicotto λ/4 (sleeve): linea λ/4 in corto tra calza e manicotto, alta impedenza a f0.", "Z0 del manicotto 50 ohm e 0,08 dB di perdita sul quarto d'onda.", "Cavo e balun");
            FumettoGiallo(cB["ferriti"], "10 perline in ferrite sulla calza.", "40 ohm per perlina a 144 MHz (mix 61), 100 ohm sopra 30 MHz con il mix 43: valori tipici dei cataloghi.", "Cavo e balun");
            ComboBox cMostra = new ComboBox { Width = 140 };
            sx.Children.Add(riga("Caso mostrato", cMostra, ""));
            Button bCalc = Btn("Calcola il confronto", true); bCalc.Margin = new Thickness(2, 12, 0, 0);
            ProgressBar barra = new ProgressBar { Height = 6, Margin = new Thickness(2, 6, 0, 0), IsVisible = false };
            sx.Children.Add(bCalc); sx.Children.Add(barra);

            Disegno dV = new Disegno { Height = 520 };
            Grid tab = new Grid();
            StackPanel avv = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            StackPanel cT = new StackPanel(); cT.Children.Add(tab); cT.Children.Add(avv);
            LineChart gC = new LineChart { Height = 320, XLabel = "distanza dal punto di alimentazione (m)", YLabel = "corrente calza (%)", EmptyText = "premi Calcola il confronto" };
            StackPanel dxr = new StackPanel(); dxr.Children.Add(Pannello("Confronto dei balun", cT)); dxr.Children.Add(Pannello("Corrente lungo la calza, dal dipolo verso il basso", gC));
            Grid corpo = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,1.3*") };
            corpo.Children.Add(Pannello("Vista laterale: corrente sulla calza", dV)); Grid.SetColumn(dxr, 2); corpo.Children.Add(dxr);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*") };
            g.Children.Add(sx); Grid.SetColumn(corpo, 2); g.Children.Add(corpo);

            List<CavoBalun.Esito> esiti = new List<CavoBalun.Esito>();
            NecModel conCalza = null; List<int> tagsCalza = null; SegmentGeometry gCalza = null;
            Action aggiornaBalun = () =>
            {
                double f = ParseNum(tF.Text, 144);
                foreach (var t in CavoBalun.Tipi)
                {
                    bool attivo = f < 30 ? t.Hf : t.Vhf;
                    cB[t.Chiave].IsEnabled = attivo;
                    if (!attivo) cB[t.Chiave].IsChecked = false;
                }
            };
            Color[] col = { Color.Parse("#D0342C"), Color.Parse("#7A5C2E"), Color.Parse("#8E44AD"), Color.Parse("#1D5FB0"), Color.Parse("#21A04A"), Color.Parse("#E08A00"), Color.Parse("#16A3B5") };
            dV.Disegna = (ctx, rr) =>
            {
                if (run == null || run.Geometry == null) { Draw.TextC(ctx, "Calcola prima il modello", rr.Width / 2, rr.Height / 2, 13, Pal.Muted); return; }
                // proiezione laterale: asse del boom in orizzontale, z in verticale
                SegmentGeometry sg = gCalza ?? run.Geometry;
                int asse; Tolleranze.Elementi(model, run.Geometry, out asse);
                Func<int, double, double, double, double> hx = (i, x, y, z) => asse == 0 ? x : asse == 1 ? y : x;
                double xmin = double.MaxValue, xmax = double.MinValue, zmin = double.MaxValue, zmax = double.MinValue;
                for (int i = 0; i < sg.N; i++)
                {
                    foreach (var p in new[] { new[] { sg.X1[i], sg.Y1[i], sg.Z1[i] }, new[] { sg.X2[i], sg.Y2[i], sg.Z2[i] } })
                    { double h = p[asse == 2 ? 0 : asse]; xmin = Math.Min(xmin, h); xmax = Math.Max(xmax, h); zmin = Math.Min(zmin, p[2]); zmax = Math.Max(zmax, p[2]); }
                }
                double sc = Math.Min((rr.Width - 60) / Math.Max(0.1, xmax - xmin), (rr.Height - 80) / Math.Max(0.1, zmax - zmin));
                Func<double, double, Point> P = (h, z) => new Point(30 + (h - xmin) * sc, rr.Height - 50 - (z - zmin) * sc);
                CavoBalun.Esito es = esiti.FirstOrDefault(e => e.Nome == (cMostra.SelectedItem as string)) ?? esiti.FirstOrDefault();
                double cmax = es != null && es.Corr.Length > 0 ? Math.Max(1, es.Corr.Max()) : 1;
                int k = 0;
                for (int i = 0; i < sg.N; i++)
                {
                    double[] a = { sg.X1[i], sg.Y1[i], sg.Z1[i] }, b = { sg.X2[i], sg.Y2[i], sg.Z2[i] };
                    int ia = asse == 2 ? 0 : asse;
                    bool calza = tagsCalza != null && tagsCalza.Contains(sg.Tag[i]);
                    Color c0 = Color.Parse("#1D5FB0"); double w = 2;
                    if (calza && es != null && k < es.Corr.Length) { double t = Math.Min(1, es.Corr[k] / cmax); c0 = Color.FromRgb((byte)(70 + 170 * t), (byte)(110 - 60 * t), (byte)(220 - 180 * t)); w = 5; k++; }
                    else if (calza) { c0 = Color.Parse("#9AA5B4"); w = 4; }
                    else if (gCalza != null && sg.Tag[i] > model.Geometry.Max(q => q.I1) && !calza) { c0 = Color.Parse("#8B3A2E"); w = 6; }
                    ctx.DrawLine(new Pen(new SolidColorBrush(c0), w), P(a[ia], a[2]), P(b[ia], b[2]));
                }
                if (es != null)
                {
                    for (int i = 0; i < 100; i++) { double t = i / 99.0; ctx.FillRectangle(new SolidColorBrush(Color.FromRgb((byte)(70 + 170 * t), (byte)(110 - 60 * t), (byte)(220 - 180 * t))), new Rect(30 + i * 1.9, rr.Height - 34, 2, 12)); }
                    Draw.Text(ctx, "0 %", 30, rr.Height - 50, 11, Pal.Muted); Draw.TextR(ctx, FN(cmax, 0) + " %", 30 + 190, rr.Height - 50, 11, Pal.Muted);
                    Draw.Text(ctx, "caso mostrato: " + es.Nome, 30, rr.Height - 18, 11.5, Pal.Muted);
                }
            };
            Action calcola = async () =>
            {
                if (run == null || model.Sources.Count != 1) { Status("Cavo e balun: calcola prima il modello (una sola sorgente)", true); return; }
                double f = ParseNum(tF.Text, run.FreqMHz), f0 = f;
                CavoBalun.Percorso p = new CavoBalun.Percorso { LungoBoom = ParseNum(tBoom.Text, 0.5), DiscesaPalo = ParseNum(tPalo.Text, 3), FinoStazione = ParseNum(tSt.Text, 0), CalzaAMassa = cMassa.IsChecked == true, TerraStazione = cTerra.IsChecked == true };
                List<string> scelti = CavoBalun.Tipi.Where(t => cB[t.Chiave].IsChecked == true && cB[t.Chiave].IsEnabled).Select(t => t.Chiave).ToList();
                if (scelti.Count == 0) return;
                NecModel m0 = model; SegmentGeometry g0 = run.Geometry; double th = run.ThetaMax, ph = run.PhiMax;
                bCalc.IsEnabled = false; barra.IsVisible = true; barra.Maximum = scelti.Count * 4; barra.Value = 0;
                Progress<int> pr = new Progress<int>(v => barra.Value = v);
                try
                {
                    var res = await Task.Run(() =>
                    {
                        List<CavoBalun.Esito> l = new List<CavoBalun.Esito>(); int n = 0;
                        foreach (string k in scelti)
                        {
                            CavoBalun.Esito e = CavoBalun.Valuta(m0, g0, f, f0, p, k, th, ph, CancellationToken.None);
                            ((IProgress<int>)pr).Report(++n);
                            // banda con calza < 5 %: tre frequenze per lato (+-1, 2, 4 %)
                            if (k != "nessuno")
                            {
                                double da = f, a = f;
                                foreach (double d in new[] { 0.01, 0.02, 0.04 }) { if (CavoBalun.Valuta(m0, g0, f * (1 - d), f0, p, k, th, ph, CancellationToken.None).CorrPerc < 5) da = f * (1 - d); else break; }
                                ((IProgress<int>)pr).Report(++n);
                                foreach (double d in new[] { 0.01, 0.02, 0.04 }) { if (CavoBalun.Valuta(m0, g0, f * (1 + d), f0, p, k, th, ph, CancellationToken.None).CorrPerc < 5) a = f * (1 + d); else break; }
                                if (e.CorrPerc < 5) { e.BandaDa = da; e.BandaA = a; }
                            }
                            n += 2; ((IProgress<int>)pr).Report(n);
                            l.Add(e);
                        }
                        List<int> tg; NecModel mc = CavoBalun.ConCalza(m0, g0, f, p, "nessuno", f0, out tg);
                        return Tuple.Create(l, mc, tg);
                    });
                    esiti = res.Item1; conCalza = res.Item2; tagsCalza = res.Item3; gCalza = conCalza.BuildGeometry();
                    cMostra.ItemsSource = esiti.Select(e => e.Nome).ToList(); cMostra.SelectedIndex = 0;
                    // tabella
                    tab.Children.Clear(); tab.RowDefinitions.Clear();
                    tab.ColumnDefinitions = new ColumnDefinitions("*" + string.Concat(Enumerable.Repeat(",Auto", esiti.Count)));
                    int r0 = 0;
                    Action<string, Func<CavoBalun.Esito, string>, Func<CavoBalun.Esito, Color?>> rg = (et, val, colr) =>
                    {
                        tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                        Border b0 = new Border { Child = new TextBlock { Text = et, FontWeight = r0 == 0 ? FontWeight.SemiBold : FontWeight.Normal }, Padding = new Thickness(8, 3), Background = r0 == 0 ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                        Grid.SetRow(b0, r0); tab.Children.Add(b0);
                        for (int j = 0; j < esiti.Count; j++)
                        {
                            Color? cc = colr?.Invoke(esiti[j]);
                            TextBlock t = new TextBlock { Text = val(esiti[j]), HorizontalAlignment = HorizontalAlignment.Right, FontWeight = r0 == 0 || cc.HasValue ? FontWeight.SemiBold : FontWeight.Normal };
                            if (cc.HasValue) t.Foreground = new SolidColorBrush(cc.Value);
                            Border b = new Border { Child = t, Padding = new Thickness(8, 3), Background = r0 == 0 ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                            Grid.SetRow(b, r0); Grid.SetColumn(b, j + 1); tab.Children.Add(b);
                        }
                        r0++;
                    };
                    Color rosso = Color.Parse("#D0342C"), verde = Color.Parse("#21A04A");
                    double minC = esiti.Min(e => e.CorrPerc), maxFb = esiti.Max(e => e.Fb);
                    rg("", e => e.Nome.Split('(')[0].Trim(), null);
                    rg("Corrente sulla calza (% del dipolo)", e => FN(e.CorrPerc, 0) + " %", e => e.CorrPerc > 10 ? rosso : Math.Abs(e.CorrPerc - minC) < 1e-9 ? verde : (Color?)null);
                    rg("F/B", e => FN(e.Fb, 1) + " dB", e => Math.Abs(e.Fb - maxFb) < 1e-9 ? verde : e.Fb < maxFb - 3 ? rosso : (Color?)null);
                    rg("F/R medio", e => FN(e.FrMedio, 1) + " dB", null);
                    rg("Guadagno", e => FN(e.G, 1) + " dBi", null);
                    rg("Z / ROS", e => FN(e.Z.Real, 0) + (e.Z.Imaginary >= 0 ? " + j" : " − j") + FN(Math.Abs(e.Z.Imaginary), 0) + " / " + FN(e.Ros, 2), null);
                    rg("Perdita nel balun", e => e.Chiave == "nessuno" ? "—" : FN(e.PerditaDb, 2) + " dB", null);
                    rg("Potenza dissipata nel balun a 1 kW", e => e.Chiave == "nessuno" ? "—" : FN(e.WattA1kW, 0) + " W", e => e.WattA1kW > 20 ? rosso : (Color?)null);
                    rg("Banda con calza < 5 %", e => e.Chiave == "nessuno" ? "—" : double.IsNaN(e.BandaDa) ? "no" : (e.BandaA - e.BandaDa >= 0.079 * f ? "larga" : FN(e.BandaDa, 1) + "–" + FN(e.BandaA, 1)), e => !double.IsNaN(e.BandaDa) && e.BandaA - e.BandaDa >= 0.079 * f ? verde : (Color?)null);
                    avv.Children.Clear();
                    avv.Children.Add(AvvisoGiallo("Le voci in grigio sono balun dell'altra banda e si attivano scegliendo la frequenza. In HF conta anche la calza fino alla stazione (messa a terra), in VHF/UHF il tratto sul boom e sul palo. La calza e' un filo attaccato a un lato del punto di alimentazione, come nella realta' senza balun; il balun e' un'impedenza di modo comune sul primo tratto."));
                    gC.X = esiti[0].Dist; gC.Series = new List<LineSeries>();
                    for (int j = 0; j < esiti.Count; j++) gC.Series.Add(new LineSeries { Name = esiti[j].Nome.Split('(')[0].Trim(), Y = esiti[j].Corr, Color = col[j % col.Length], Width = 2.2, Decimals = 1 });
                    gC.InvalidateVisual(); dV.InvalidateVisual();
                    Registro.Scrivi("AZIONE", "cavo e balun: " + string.Join(", ", esiti.Select(e => e.Chiave + " " + FN(e.CorrPerc, 0) + "%")));
                }
                catch (Exception ex) { Status("Cavo e balun: " + ex.Message, true); }
                finally { bCalc.IsEnabled = true; barra.IsVisible = false; }
            };
            bCalc.Click += (s, e) => calcola();
            tF.LostFocus += (s, e) => aggiornaBalun();
            cMostra.SelectionChanged += (s, e) => dV.InvalidateVisual();
            aggiornaBalun();
            return g;
        }
    }
}
