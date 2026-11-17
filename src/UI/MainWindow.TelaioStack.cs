// YA-NEC 1.2.0 - Oggetti inseriti > Stack: telaio a H e palo nel calcolo (bozzetto VHF 4).
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
        Control PannelloTelaio(StackInfo si)
        {
            var riga = MeccRiga();
            StackPanel sx = new StackPanel { Width = 262, Spacing = 2 };
            sx.Children.Add(MeccFam("TELAIO"));
            CheckBox cInc = new CheckBox { Content = "Includi telaio nel calcolo", IsChecked = true, FontWeight = FontWeight.SemiBold, Margin = new Thickness(2, 0) };
            ComboBox cTipo = new ComboBox { Width = 130, ItemsSource = TelaioStack.Tipi, SelectedIndex = 0 };
            ComboBox cMat = new ComboBox { Width = 130, ItemsSource = TelaioStack.Materiali, SelectedIndex = 0 };
            ToolTip.SetTip(cMat, "La vetroresina non conduce: il telaio non entra nel NEC, restano solo le calze dei cavi di fase.");
            CheckBox cPalo = new CheckBox { Content = "Includi palo sotto il telaio", IsChecked = true }, cCavi = new CheckBox { Content = "Cavi di fase lungo i montanti", IsChecked = true }, cRot = new CheckBox { Content = "Rotore e controventi", IsEnabled = false };
            ToolTip.SetTip(cRot, "Prevista: rotore e controventi come fili (servono le misure del traliccio).");
            sx.Children.Add(cInc); sx.Children.Add(riga("Tipo", cTipo, "")); sx.Children.Add(riga("Materiale", cMat, ""));
            foreach (CheckBox c in new[] { cPalo, cCavi, cRot }) { c.Margin = new Thickness(2, 0); sx.Children.Add(c); }
            Button bCalc = Btn("Calcola confronto", true); bCalc.Margin = new Thickness(2, 10, 0, 0);
            sx.Children.Add(bCalc);

            Disegno dV = new Disegno { Height = 420 };
            Grid tPezzi = new Grid();
            StackPanel cV = new StackPanel(); cV.Children.Add(dV); cV.Children.Add(new TextBlock { Text = "rosso = parti del telaio modellate come fili NEC · tratteggio = calza dei cavi di fase", FontSize = 12, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 4) }); cV.Children.Add(tPezzi);
            Grid tab = new Grid();
            StackPanel cT = new StackPanel(); cT.Children.Add(tab);
            cT.Children.Add(AvvisoGiallo(si.HorizontalPol ? "Polarizzazione orizzontale: i montanti verticali sono poco accoppiati; la traversa orizzontale e i cavi di fase no." : "Polarizzazione verticale: i montanti e il palo sono paralleli agli elementi e pesano di piu'; la traversa orizzontale poco."));
            PolarPlot pol = new PolarPlot { ArrlScale = false, LinRange = 40, Height = 340, ShowHalfPower = false, CursorsOn = false, ShowNulls = false };
            StackPanel dxr = new StackPanel(); dxr.Children.Add(Pannello("Confronto senza / con telaio", cT)); dxr.Children.Add(Pannello("Diagramma orizzontale sovrapposto", pol));
            Grid corpo = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*") };
            corpo.Children.Add(Pannello("Vista frontale (dal davanti dell'antenna)", cV)); Grid.SetColumn(dxr, 2); corpo.Children.Add(dxr);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*") };
            g.Children.Add(sx); Grid.SetColumn(corpo, 2); g.Children.Add(corpo);

            TelaioStack.Esito ultimo = null;
            dV.Disegna = (ctx, rr) =>
            {
                if (ultimo == null) { Draw.TextC(ctx, "premi Calcola confronto", rr.Width / 2, rr.Height / 2, 13, Pal.Muted); return; }
                double[] u = { si.HorizDirX, si.HorizDirY, 0 };
                Func<double[], double> U = p => p[0] * u[0] + p[1] * u[1];
                var pts = ultimo.Disegno.SelectMany(d => new[] { d[0], d[1] }).ToList();
                double umin = Math.Min(pts.Min(U), ultimo.Fissaggi.Min(U) - ultimo.LElemento / 2), umax = Math.Max(pts.Max(U), ultimo.Fissaggi.Max(U) + ultimo.LElemento / 2), zmin = pts.Min(p => p[2]), zmax = pts.Max(p => p[2]);
                double sc = Math.Min((rr.Width - 60) / Math.Max(0.1, umax - umin), (rr.Height - 30) / Math.Max(0.1, zmax - zmin));
                Func<double, double, Point> P = (uu, z) => new Point(30 + (uu - umin) * sc, rr.Height - 15 - (z - zmin) * sc);
                // antenne: elemento piu' lungo (orizzontale o verticale) con il cerchio del boom nel punto di fissaggio
                foreach (double[] fx in ultimo.Fissaggi)
                {
                    double l2 = ultimo.LElemento / 2;
                    Point pa = si.HorizontalPol ? P(U(fx) - l2, fx[2]) : P(U(fx), fx[2] - l2), pb = si.HorizontalPol ? P(U(fx) + l2, fx[2]) : P(U(fx), fx[2] + l2);
                    ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#1D5FB0")), 3), pa, pb);
                }
                Color[] col = { Color.Parse("#D0342C"), Color.Parse("#D0342C"), Color.Parse("#8B3A2E"), Color.Parse("#7A8696") };
                foreach (var d in ultimo.Disegno)
                {
                    int t = (int)d[2][0];
                    Pen p = t == 3 ? new Pen(new SolidColorBrush(Color.Parse("#555F6B")), 1.4, new DashStyle(new double[] { 2, 2 }, 0)) : new Pen(new SolidColorBrush(col[t]), t == 2 ? 9 : 6);
                    ctx.DrawLine(p, P(U(d[0]), d[0][2]), P(U(d[1]), d[1][2]));
                }
                foreach (double[] fx in ultimo.Fissaggi) ctx.DrawEllipse(Brushes.White, new Pen(new SolidColorBrush(Color.Parse("#1D5FB0")), 2), P(U(fx), fx[2]), 7, 7);
            };
            Action calcola = async () =>
            {
                if (run == null) { Status("Stack: calcola prima il modello", true); return; }
                TelaioStack.Opzioni o = new TelaioStack.Opzioni { Tipo = cTipo.SelectedIndex, Materiale = cMat.SelectedIndex, Palo = cPalo.IsChecked == true, Cavi = cCavi.IsChecked == true };
                NecModel m0 = model; SegmentGeometry g0 = run.Geometry; double f = run.FreqMHz, th = run.ThetaMax, ph = run.PhiMax;
                bool inc = cInc.IsChecked == true;
                bCalc.IsEnabled = false; Status("Stack: calcolo senza e con telaio...");
                try
                {
                    var res = await Task.Run(() =>
                    {
                        TelaioStack.Esito e = TelaioStack.Costruisci(m0, g0, si, f, o);
                        var a = TelaioStack.Valuta(m0, f, th, ph, si.HorizontalPol, CancellationToken.None);
                        var b = inc ? TelaioStack.Valuta(e.Modello, f, th, ph, si.HorizontalPol, CancellationToken.None) : a;
                        return Tuple.Create(e, a, b);
                    });
                    ultimo = res.Item1;
                    var s0 = res.Item2; var s1 = res.Item3;
                    // pezzi
                    tPezzi.Children.Clear(); tPezzi.RowDefinitions.Clear(); tPezzi.ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto");
                    int r0 = 0;
                    Action<Grid, string[], bool, Color?> rg = (gt, v, h, cc) =>
                    {
                        gt.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                        for (int j = 0; j < v.Length; j++)
                        {
                            TextBlock t = new TextBlock { Text = v[j], FontWeight = h || (cc.HasValue && j == v.Length - 1) ? FontWeight.SemiBold : FontWeight.Normal, HorizontalAlignment = j == 0 || (gt == tPezzi && j == 3) ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                            if (cc.HasValue && j == v.Length - 1) t.Foreground = new SolidColorBrush(cc.Value);
                            Border b = new Border { Child = t, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                            Grid.SetColumn(b, j); Grid.SetRow(b, gt.RowDefinitions.Count - 1); gt.Children.Add(b);
                        }
                    };
                    rg(tPezzi, new[] { "Pezzo", "Ø mm", "Lungh. m", "Collegato" }, true, null);
                    foreach (var p in ultimo.Pezzi) rg(tPezzi, new[] { p.Nome, FN(p.DiamMm, 0), (p.Quanti > 1 && p.Nome.StartsWith("Cavi") ? p.Quanti + " × " : "") + FN(p.LungM, 2), p.Collegato }, false, null);
                    tab.Children.Clear(); tab.RowDefinitions.Clear(); tab.ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto");
                    Color rosso = Color.Parse("#D0342C");
                    Func<double, bool, Color?> peggio = (d, piuEMeglio) => (piuEMeglio ? d < -1 : d > 1) ? rosso : (Color?)null;
                    rg(tab, new[] { "", "Senza telaio", "Con telaio", "Δ" }, true, null);
                    rg(tab, new[] { "Guadagno", FN(s0.G, 1) + " dBi", FN(s1.G, 1) + " dBi", FN(s1.G - s0.G, 1) }, false, peggio(s1.G - s0.G, true));
                    rg(tab, new[] { "F/B", FN(s0.Fb, 1) + " dB", FN(s1.Fb, 1) + " dB", FN(s1.Fb - s0.Fb, 1) }, false, peggio(s1.Fb - s0.Fb, true));
                    rg(tab, new[] { "F/R medio", FN(s0.FrMedio, 1) + " dB", FN(s1.FrMedio, 1) + " dB", FN(s1.FrMedio - s0.FrMedio, 1) }, false, peggio(s1.FrMedio - s0.FrMedio, true));
                    rg(tab, new[] { "Lobo laterale peggiore (piano E)", FN(s0.Lobo, 1) + " dB", FN(s1.Lobo, 1) + " dB", (s1.Lobo - s0.Lobo >= 0 ? "+" : "") + FN(s1.Lobo - s0.Lobo, 1) }, false, peggio(s1.Lobo - s0.Lobo, false));
                    Func<Complex, string> zs = z => FN(z.Real, 1) + (z.Imaginary >= 0 ? " + j" : " − j") + FN(Math.Abs(z.Imaginary), 1);
                    rg(tab, new[] { "Z ingresso divisore", zs(s0.Zdiv), zs(s1.Zdiv), "" }, false, null);
                    if (!double.IsNaN(s0.Tant))
                    {
                        rg(tab, new[] { "Tant (elev. 10°, cielo 200 K, suolo 290 K)", FN(s0.Tant, 0) + " K", FN(s1.Tant, 0) + " K", (s1.Tant - s0.Tant >= 0 ? "+" : "") + FN(s1.Tant - s0.Tant, 0) + " K" }, false, s1.Tant - s0.Tant > 10 ? rosso : (Color?)null);
                        rg(tab, new[] { "G/T", FN(s0.Gt, 1) + " dB/K", FN(s1.Gt, 1) + " dB/K", FN(s1.Gt - s0.Gt, 1) }, false, peggio(s1.Gt - s0.Gt, true));
                    }
                    pol.Traces.Clear(); pol.RefDb = Math.Max(s0.Az.Max(), s1.Az.Max());
                    pol.Traces.Add(new PolarTrace { Db = s0.Az, Color = Color.Parse("#1D5FB0"), Width = 2.2, Name = "senza telaio" });
                    pol.Traces.Add(new PolarTrace { Db = s1.Az, Color = Color.Parse("#D0342C"), Width = 2.2, Name = "con telaio" });
                    pol.InvalidateVisual(); dV.InvalidateVisual();
                    Status("Stack: confronto senza / con telaio fatto");
                    Registro.Scrivi("AZIONE", "telaio dello stack: " + TelaioStack.Tipi[o.Tipo] + ", F/B " + FN(s0.Fb, 1) + " -> " + FN(s1.Fb, 1));
                }
                catch (Exception ex) { Status("Telaio: " + ex.Message, true); }
                finally { bCalc.IsEnabled = true; }
            };
            bCalc.Click += (s, e) => calcola();
            return Pannello("Telaio a H e palo nello stack", g);
        }
    }
}
