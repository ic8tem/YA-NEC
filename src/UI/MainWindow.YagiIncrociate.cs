// YA-NEC 1.2.0 - Calcolatori > Yagi incrociate (bozzetto VHF 6): due Yagi uguali (il modello aperto) ruotate di +-45 gradi (X) o 0/90 (+)
// attorno al boom, sullo stesso piano o sfalsate di lambda/4 lungo il boom; commutazione H, V, RHCP, LHCP, X-pol. Per ogni modo il programma
// prova le quattro fasi della rete (0, 90, 180, 270 gradi sul piano B) e tiene quella che da' la componente voluta piu' forte in asse.
// Componenti circolari (convenzione IEEE, e^jwt): E_R = (Eth + j Eph)/radice 2, E_L = (Eth - j Eph)/radice 2.
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
        Control BuildYagiIncrociatePanel()
        {
            string modo = "rhcp";
            var riga = MeccRiga();
            StackPanel sx = new StackPanel { Width = 262, Spacing = 2 };
            sx.Children.Add(MeccFam("GEOMETRIA"));
            ComboBox cBase = new ComboBox { Width = 130, ItemsSource = new[] { "Modello aperto" }, SelectedIndex = 0 };
            ComboBox cDisp = new ComboBox { Width = 130, ItemsSource = new[] { "X (45°)", "+ (H/V)" }, SelectedIndex = 0 };
            ComboBox cSf = new ComboBox { Width = 130, ItemsSource = new[] { "λ/4 sul boom", "stesso piano" }, SelectedIndex = 0 };
            ComboBox cBoom = new ComboBox { Width = 130, ItemsSource = new[] { "Metallico, isolati", "Non metallico" }, SelectedIndex = 0 };
            ToolTip.SetTip(cBoom, "Il boom non entra nel modello NEC: con elementi isolati dal boom metallico l'effetto e' piccolo (correzione del boom nei Calcolatori).");
            sx.Children.Add(riga("Yagi di base", cBase, "")); sx.Children.Add(riga("Disposizione", cDisp, "")); sx.Children.Add(riga("Sfasamento piani", cSf, "")); sx.Children.Add(riga("Boom", cBoom, ""));
            Fascione fM = new Fascione(); fM.Famiglia("COMMUTAZIONE");
            fM.Voce("h", "Orizzontale"); fM.Voce("v", "Verticale"); fM.Voce("rhcp", "Circolare destra (RHCP)"); fM.Voce("lhcp", "Circolare sinistra (LHCP)"); fM.Voce("xpol", "X-pol (2 ricevitori, EME)");
            sx.Children.Add(fM);
            sx.Children.Add(MeccFam("RETE DI FASE"));
            TextBox tZr = Tb("75", 60);
            sx.Children.Add(riga("Cavo di ritardo λ/4", tZr, "Ω"));
            CheckBox cReali = new CheckBox { Content = "Includi relè e cavi reali", IsChecked = true, Margin = new Thickness(2, 0) };
            FumettoGiallo(cReali, "Aggiunge la perdita del cavo di ritardo (VF 0,66, 10 dB/100 m a 144 MHz, scalata con la radice della frequenza) e 0,1 dB per il relè.", "valori tipici di un cavo da 75 ohm e di un relè coassiale; il programma non conosce i tuoi.", "Yagi incrociate");
            sx.Children.Add(cReali);
            Button bCalc = Btn("Calcola", true); bCalc.Margin = new Thickness(2, 12, 0, 0);
            sx.Children.Add(bCalc);
            sx.Children.Add(Nota("La stessa rete di fase serve anche per un turnstile (dipoli incrociati a 90°)."));

            Disegno dVista = new Disegno { Height = 360 };
            Grid tab = new Grid();
            StackPanel cV = new StackPanel(); cV.Children.Add(dVista); cV.Children.Add(tab);
            PolarPlot pol = new PolarPlot { ArrlScale = false, LinRange = 40, Height = 330, ShowHalfPower = false, CursorsOn = false, ShowNulls = false };
            LineChart gAr = new LineChart { Height = 330, XLabel = "angolo dall'asse (°)", YLabel = "AR (dB)", EmptyText = "premi Calcola" };
            LineChart gF = new LineChart { Height = 300, XLabel = "frequenza (MHz)", YLabel = "AR (dB) / ROS", EmptyText = "premi Calcola" };
            Border pV = Pannello("Vista frontale", cV), pP = Pannello("Componenti co-polare / cross-polare", pol), pA = Pannello("Rapporto assiale nel lobo", gAr), pF = Pannello("Rapporto assiale e ROS in frequenza", gF);
            Grid destra = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*"), RowDefinitions = new RowDefinitions("Auto,10,Auto") };
            Grid.SetColumn(pA, 2); Grid.SetRow(pF, 2); Grid.SetColumnSpan(pF, 3);
            destra.Children.Add(pP); destra.Children.Add(pA); destra.Children.Add(pF);
            Grid corpo = new Grid { ColumnDefinitions = new ColumnDefinitions("1*,10,2*") };
            corpo.Children.Add(pV); Grid.SetColumn(destra, 2); corpo.Children.Add(destra);
            StackPanel avv = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            StackPanel dx = new StackPanel(); dx.Children.Add(corpo); dx.Children.Add(avv);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*"), Margin = new Thickness(4) };
            g.Children.Add(new ScrollViewer { Content = sx }); Grid.SetColumn(dx, 2); g.Children.Add(dx);

            CancellationTokenSource cts = null;
            List<Tolleranze.Elemento> elBase = null;
            dVista.Disegna = (ctx, rr) =>
            {
                double cx = rr.Width / 2, cy = 130, l = 110;
                bool x = cDisp.SelectedIndex == 0;
                double aA = x ? 45 : 0, aB = x ? -45 : 90;
                Action<double, Color, string> braccio = (a, col, nome) =>
                {
                    double t = a * Math.PI / 180;
                    ctx.DrawLine(new Pen(new SolidColorBrush(col), 4), new Point(cx - l * Math.Cos(t), cy + l * Math.Sin(t)), new Point(cx + l * Math.Cos(t), cy - l * Math.Sin(t)));
                    Draw.Text(ctx, nome, cx + l * Math.Cos(t) + 6, cy - l * Math.Sin(t) - 8, 12, col);
                };
                braccio(aA, Color.Parse("#1D5FB0"), "piano A"); braccio(aB, Color.Parse("#21A04A"), "piano B");
                ctx.DrawEllipse(Brushes.White, new Pen(new SolidColorBrush(Color.Parse("#2B3A4A")), 2), new Point(cx, cy), 16, 16);
                Draw.TextC(ctx, "vista dal davanti — boom al centro", cx, cy + l + 26, 12, Pal.Fore);
                double y = cy + l + 56, x0 = 30, x1 = rr.Width - 30;
                ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#2B3A4A")), 4), new Point(x0, y), new Point(x1, y));
                int n = elBase != null ? elBase.Count : 12;
                double off = cSf.SelectedIndex == 0 ? (x1 - x0) / (n * 2.0) * 0.5 : 0;
                for (int i = 0; i < n; i++)
                {
                    double xi = x0 + 10 + (x1 - x0 - 30) * i / Math.Max(1, n - 1);
                    ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#1D5FB0")), 2.4), new Point(xi, y - 14), new Point(xi, y + 14));
                    ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#21A04A")), 2.4), new Point(xi + off + (off == 0 ? 3 : 0), y - 14), new Point(xi + off + (off == 0 ? 3 : 0), y + 14));
                }
                Draw.TextC(ctx, cSf.SelectedIndex == 0 ? "piani sfalsati di λ/4 lungo il boom" : "piani sullo stesso punto del boom (ritardo con il cavo)", cx, y + 30, 12, Pal.Fore);
            };

            Action calcola = async () =>
            {
                if (run == null || run.Geometry == null) { Status("Yagi incrociate: calcola prima il modello aperto (una Yagi)", true); return; }
                int asse; elBase = Tolleranze.Elementi(model, run.Geometry, out asse);
                var dr = elBase.FirstOrDefault(e => e.Alimentato);
                if (elBase.Count < 2 || dr == null || model.Sources.Count != 1) { Status("Yagi incrociate: serve una Yagi con una sola sorgente", true); return; }
                cts?.Cancel(); cts = new CancellationTokenSource(); CancellationToken ct = cts.Token;
                double f0 = run.FreqMHz, lam = 299.792458 / f0;
                bool x = cDisp.SelectedIndex == 0, sfal = cSf.SelectedIndex == 0;
                double thF = run.ThetaMax, phF = run.PhiMax;
                NecModel m0 = model; string modoC = modo; double zr = ParseNum(tZr.Text, 75); bool reali = cReali.IsChecked == true;
                bCalc.IsEnabled = false; Status("Yagi incrociate: calcolo in corso...");
                try
                {
                    var res = await Task.Run(() =>
                    {
                        double[] c = TrasformaGeometria.Centro(run.Geometry);
                        // piano A ruotato, piano B copia ruotata ancora di -90 (X) o +90 (+) e spostata di lambda/4 in avanti
                        NecModel mA = TrasformaGeometria.Applica(m0, TrasformaGeometria.Ruota(asse, x ? 45 : 0, c), null);
                        int maxTag = mA.Geometry.Max(q => q.I1);
                        List<int> tagsBase = mA.Geometry.Where(q => q.Id == "GW").Select(q => q.I1).ToList();
                        NecModel mB = TrasformaGeometria.Copia(mA, TrasformaGeometria.Ruota(asse, x ? -90 : 90, c), null, 1);
                        HashSet<int> nuovi = new HashSet<int>(Enumerable.Range(maxTag + 1, tagsBase.Count));
                        // verso del boom (dal riflettore ai direttori) lungo l'asse
                        double verso = Math.Cos(phF * Math.PI / 180) * (asse == 0 ? 1 : 0) + Math.Sin(phF * Math.PI / 180) * (asse == 1 ? 1 : 0);
                        if (Math.Abs(verso) < 0.5) verso = 1; else verso = Math.Sign(verso);
                        if (sfal) { double[] d = { 0, 0, 0 }; d[asse] = verso * lam / 4; mB = TrasformaGeometria.Applica(mB, TrasformaGeometria.Sposta(d[0], d[1], d[2]), nuovi); }
                        int tagB = maxTag + 1 + tagsBase.IndexOf(m0.Sources[0].Tag);
                        SourceSpec sA = m0.Sources[0].Clone();
                        Func<Complex, Complex, NecModel> conSorgenti = (va, vb) =>
                        {
                            NecModel m = mB.Clone();
                            m.Sources.Clear();
                            if (va != Complex.Zero) { SourceSpec a = sA.Clone(); a.V = va; m.Sources.Add(a); }
                            if (vb != Complex.Zero) { SourceSpec b = sA.Clone(); b.Tag = tagB; b.V = vb; m.Sources.Add(b); }
                            if (m.Sources.Count == 0) { SourceSpec a = sA.Clone(); m.Sources.Add(a); }
                            return m;
                        };
                        Func<Nec2Engine, double, double, Complex[]> comp = (e, th, ph) =>
                        {
                            Complex et, ep; e.Ffld(th * Math.PI / 180, ph * Math.PI / 180, out et, out ep);
                            double s2 = Math.Sqrt(2);
                            return new[] { (et + Complex.ImaginaryOne * ep) / s2, (et - Complex.ImaginaryOne * ep) / s2, -et, ep };   // R, L, verticale, orizzontale
                        };
                        int iCo = modoC == "rhcp" ? 0 : modoC == "lhcp" ? 1 : modoC == "v" ? 2 : 3, iX = modoC == "rhcp" ? 1 : modoC == "lhcp" ? 0 : modoC == "v" ? 3 : 2;
                        // fase del piano B: la migliore delle quattro per il modo voluto
                        double faseB = 0; RunResult best = null; double bv = -1;
                        if (modoC == "xpol") { best = Analysis.Run(conSorgenti(1, 0), conSorgenti(1, 0).BuildGeometry(), f0, 10, ct); }
                        else
                            foreach (double fz in new double[] { 0, 90, 180, 270 })
                            {
                                ct.ThrowIfCancellationRequested();
                                NecModel m = conSorgenti(1, Complex.FromPolarCoordinates(1, fz * Math.PI / 180));
                                RunResult r = Analysis.Run(m, m.BuildGeometry(), f0, 10, ct);
                                double v = comp(r.Engine, thF, phF)[iCo].Magnitude;
                                if (v > bv) { bv = v; best = r; faseB = fz; }
                            }
                        Nec2Engine en = best.Engine; double gc = en.GainConstant;
                        Func<Complex, double> db = q => { double gg = gc * (q * Complex.Conjugate(q)).Real; return gg > 1e-30 ? 10 * Math.Log10(gg) : -99; };
                        // taglio di azimut nel piano del massimo
                        double[] co = new double[360], cr = new double[360];
                        for (int a = 0; a < 360; a++) { var q = comp(en, thF, a); co[a] = db(q[modoC == "xpol" ? 3 : iCo]); cr[a] = db(q[modoC == "xpol" ? 2 : iX]); }
                        // rapporto assiale nel lobo (-30..30 gradi)
                        double[] ang = Enumerable.Range(0, 61).Select(i => -30.0 + i).ToArray();
                        Func<Complex[], double> ar = q => { double r_ = q[0].Magnitude, l_ = q[1].Magnitude; double d = Math.Abs(r_ - l_); return d < 1e-12 ? 60 : Math.Min(60, 20 * Math.Log10((r_ + l_) / d)); };
                        double[] arA = ang.Select(a => ar(comp(en, thF, phF + a))).ToArray();
                        double gco = db(comp(en, thF, phF)[modoC == "xpol" ? 3 : iCo]), gx = db(comp(en, thF, phF)[modoC == "xpol" ? 2 : iX]);
                        // -3 dB del co-polare
                        int k3 = 0; while (k3 < 30 && co[(int)((phF + k3 + 360) % 360)] > gco - 3) k3++;
                        double ar3 = (arA[30 + Math.Min(30, k3)] + arA[30 - Math.Min(30, k3)]) / 2;
                        // isolamento: solo il piano A alimentato, il B in corto alla sorgente -> Y11, Y21 -> S21 su 50 ohm
                        NecModel mi = conSorgenti(1, 0); SegmentGeometry gi = mi.BuildGeometry();
                        RunResult ri = Analysis.Run(mi, gi, f0, 10, ct);
                        int segB = -1, cnt = 0; for (int s = 0; s < gi.N; s++) if (gi.Tag[s] == tagB && ++cnt == sA.Seg) segB = s;
                        int segA = -1; cnt = 0; for (int s = 0; s < gi.N; s++) if (gi.Tag[s] == sA.Tag && ++cnt == sA.Seg) segA = s;
                        Complex y11 = segA >= 0 ? ri.Engine.SegmentCurrent(segA) : Complex.Zero, y21 = segB >= 0 ? ri.Engine.SegmentCurrent(segB) : Complex.Zero;
                        double z0 = 50; Complex den = (1 + y11 * z0) * (1 + y11 * z0) - (y21 * z0) * (y21 * z0);
                        double iso = -20 * Math.Log10(Math.Max(1e-9, (2 * y21 * z0 / den).Magnitude));
                        // Z all'ingresso: Za in parallelo con Zb portata al giunto dal cavo di ritardo (lambda/4 per 90 / 270, lambda/2 aggiunta per 180)
                        Func<RunResult, double, double, Complex> zIn = (r, f, fz) =>
                        {
                            if (r.Sources.Count < 2) return r.Sources[0].Z;
                            Complex za = r.Sources[0].Z, zb = r.Sources[1].Z;
                            double len = (fz % 180 == 90 ? 0.25 : 0) * 299.792458 / f0 * 0.66 + (fz >= 180 ? 0.5 * 299.792458 / f0 * 0.66 : 0);
                            double loss = reali ? 10 * Math.Sqrt(f / 144.0) : 0;
                            Complex zbt = len > 0 ? LineCalc.InputImpedance(zb, fz % 180 == 90 ? zr : 50, f, 0.66, loss, len) : zb;
                            return 1 / (1 / za + 1 / zbt);
                        };
                        Complex zi = zIn(best, f0, faseB);
                        // frequenza: 7 punti +-1 %
                        double[] fs = Enumerable.Range(0, 7).Select(i => f0 * (0.99 + 0.02 * i / 6)).ToArray();
                        double[] arF = new double[7], rosF = new double[7];
                        for (int i = 0; i < 7; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            NecModel m = modoC == "xpol" ? conSorgenti(1, 0) : conSorgenti(1, Complex.FromPolarCoordinates(1, faseB * Math.PI / 180));
                            RunResult r = Analysis.Run(m, m.BuildGeometry(), fs[i], 15, ct);
                            arF[i] = ar(comp(r.Engine, thF, phF)); rosF[i] = Analysis.Swr(zIn(r, fs[i], faseB), 50);
                        }
                        return new { co, cr, ang, arA, gco, gx, ar3, iso, zi, fs, arF, rosF, faseB, arAsse = ar(comp(en, thF, phF)) };
                    }, ct);
                    if (ct.IsCancellationRequested) return;
                    string nomeCo = modo == "rhcp" ? "RHCP" : modo == "lhcp" ? "LHCP" : modo == "v" ? "verticale" : modo == "h" ? "orizzontale" : "piano A (orizz.)";
                    string nomeX = modo == "rhcp" ? "LHCP" : modo == "lhcp" ? "RHCP" : modo == "v" ? "orizzontale" : modo == "h" ? "verticale" : "piano B (vert.)";
                    pol.Traces.Clear(); pol.RefDb = res.co.Max();
                    pol.Traces.Add(new PolarTrace { Db = res.co, Color = Color.Parse("#1D5FB0"), Width = 2.4, Name = nomeCo });
                    pol.Traces.Add(new PolarTrace { Db = res.cr, Color = Color.Parse("#D0342C"), Width = 2, Name = nomeX });
                    pol.InvalidateVisual();
                    gAr.X = res.ang; gAr.Series = new List<LineSeries> { new LineSeries { Name = "AR", Y = res.arA, Color = Color.Parse("#1D5FB0"), Width = 2.4, Decimals = 1 } };
                    gAr.YMinFixed = 0; gAr.YMaxFixed = Math.Min(20, Math.Max(6, Math.Ceiling(res.arA.Max()))); gAr.InvalidateVisual();
                    gF.X = res.fs; gF.MarkX = run.FreqMHz;
                    gF.Series = new List<LineSeries> { new LineSeries { Name = "AR in asse (dB)", Y = res.arF.Select(v => Math.Min(v, 20)).ToArray(), Color = Color.Parse("#1D5FB0"), Width = 2.4, Decimals = 2 }, new LineSeries { Name = "ROS", Y = res.rosF, Color = Color.Parse("#21A04A"), Width = 2.2, Decimals = 2 } };
                    gF.YMinFixed = 0; gF.InvalidateVisual();
                    // tabella
                    tab.Children.Clear(); tab.RowDefinitions.Clear(); tab.ColumnDefinitions = new ColumnDefinitions("*,Auto");
                    int r0 = 0;
                    Action<string, string, bool, Color?> rg = (a, b, h, col) =>
                    {
                        tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                        TextBlock tb = new TextBlock { Text = b, FontWeight = h || col.HasValue ? FontWeight.SemiBold : FontWeight.Normal, HorizontalAlignment = HorizontalAlignment.Right }; if (col.HasValue) tb.Foreground = new SolidColorBrush(col.Value);
                        Border b1 = new Border { Child = new TextBlock { Text = a, TextWrapping = TextWrapping.Wrap, FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal }, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                        Border b2 = new Border { Child = tb, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                        Grid.SetRow(b1, r0); Grid.SetRow(b2, r0); Grid.SetColumn(b2, 1); tab.Children.Add(b1); tab.Children.Add(b2); r0++;
                    };
                    bool circ = modo == "rhcp" || modo == "lhcp";
                    rg("Risultato (" + nomeCo + ")", "", true, null);
                    rg("Guadagno", FN(res.gco, 1) + (circ ? " dBic" : " dBi"), false, null);
                    if (circ) { rg("Rapporto assiale in asse", FN(res.arAsse, 1) + " dB", false, res.arAsse < 1.5 ? (Color?)Color.Parse("#21A04A") : null); rg("Rapporto assiale a −3 dB", FN(res.ar3, 1) + " dB", false, null); }
                    rg("Rifiuto " + nomeX + " in asse", res.gco - res.gx > 60 ? "> 60 dB" : FN(res.gco - res.gx, 1) + " dB", false, null);
                    rg("Z ingresso", FN(res.zi.Real, 1) + (res.zi.Imaginary >= 0 ? " + j" : " − j") + FN(Math.Abs(res.zi.Imaginary), 1), false, null);
                    rg("Isolamento tra i due piani", res.iso > 60 ? "> 60 dB" : FN(res.iso, 0) + " dB", false, null);
                    rg("Fase della rete sul piano B", FN(res.faseB, 0) + "°" + (res.faseB % 180 == 90 ? " (cavo di ritardo λ/4)" : res.faseB == 180 ? " (λ/2 in piu')" : ""), false, null);
                    avv.Children.Clear();
                    if (res.iso > 60 || res.gco - res.gx > 60) avv.Children.Add(AvvisoGiallo("Piani perfettamente ortogonali e simmetrici: nel modello l'accoppiamento e la polarizzazione opposta sono quasi nulli. Nella realta' staffe, cavi e piccole asimmetrie li limitano a circa 25-35 dB."));
                    if (reali) avv.Children.Add(AvvisoGiallo("Perdita del cavo di ritardo e del relè incluse in modo approssimato (" + FN(10 * Math.Sqrt(run.FreqMHz / 144.0), 1) + " dB/100 m, 0,1 dB di relè): non cambiano il diagramma, solo la Z e il ROS."));
                    dVista.InvalidateVisual();
                    Status("Yagi incrociate: " + nomeCo + ", " + FN(res.gco, 1) + (circ ? " dBic" : " dBi"));
                    Registro.Scrivi("AZIONE", "yagi incrociate: " + nomeCo + ", fase B " + FN(res.faseB, 0) + ", AR " + FN(res.arAsse, 2) + " dB");
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Status("Yagi incrociate: " + ex.Message, true); }
                finally { bCalc.IsEnabled = true; }
            };
            fM.Selected += k => { modo = k; calcola(); };
            bCalc.Click += (s, e) => calcola();
            cDisp.SelectionChanged += (s, e) => dVista.InvalidateVisual(); cSf.SelectionChanged += (s, e) => dVista.InvalidateVisual();
            fM.Select("rhcp", false);
            return new ScrollViewer { Content = g };
        }
    }
}
