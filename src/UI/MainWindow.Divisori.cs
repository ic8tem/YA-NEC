// YA-NEC 1.2.0 - Calcolatori > Divisori (bozzetto VHF 5): lambda/4 in tubo quadro, in tubo tondo, in cavo, Wilkinson, solo cavi di fase.
// Disegno quotato a schermo e in PDF 1:1 (fogli A4 orizzontali con sovrapposizione), ROS con le impedenze attive dello stack, cavi di fase.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SkiaSharp;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Control BuildDivisoriPanel()
        {
            string tipo = "quadro";
            var riga = MeccRiga();
            StackPanel sx = new StackPanel { Width = 262, Spacing = 2 };
            Fascione fT = new Fascione(); fT.Famiglia("TIPO");
            fT.Voce("quadro", "λ/4 in tubo quadro"); fT.Voce("tondo", "λ/4 in tubo tondo"); fT.Voce("cavo", "λ/4 in cavo (2 cavi 75 Ω in parallelo)");
            fT.Voce("wilk", "Wilkinson (λ/4 in cavo o in linea + resistenze)"); fT.Voce("fase", "Solo cavi di fase");
            sx.Children.Add(fT);
            sx.Children.Add(MeccFam("DATI"));
            TextBox tF = Tb(run != null ? FN(run.FreqMHz, 3) : "144,200", 80), tZa = Tb("50", 60), tZi = Tb("50", 60), tD = Tb("20", 60);
            ComboBox cN = new ComboBox { Width = 80, ItemsSource = new[] { "2", "3", "4", "6", "8" }, SelectedIndex = 2 };
            ComboBox cDie = new ComboBox { Width = 120, ItemsSource = new[] { "Aria", "PTFE (εr 2,1)" }, SelectedIndex = 0 };
            ComboBox cCon = new ComboBox { Width = 120, ItemsSource = Divisori.Connettori, SelectedIndex = 0 };
            sx.Children.Add(riga("Frequenza", tF, "MHz")); sx.Children.Add(riga("Uscite", cN, "")); sx.Children.Add(riga("Z antenne", tZa, "Ω")); sx.Children.Add(riga("Z ingresso", tZi, "Ω"));
            sx.Children.Add(riga("Tubo esterno (lato / Ø int.)", tD, "mm")); sx.Children.Add(riga("Dielettrico", cDie, "")); sx.Children.Add(riga("Connettori", cCon, ""));
            CheckBox cAcc = new CheckBox { Content = "Correggi accorciamento dei connettori", IsChecked = true, Margin = new Thickness(2, 0) };
            FumettoGiallo(cAcc, "Toglie dalla lunghezza la parte di linea che sta dentro i connettori.", "stima di 4 mm per estremo con i connettori N (3 BNC, 1,5 SMA): conviene tarare con il VNA, accorciando a passi di 1 mm.", "Divisori");
            CheckBox cStack = new CheckBox { Content = "Collega allo Stack (cavi reali)", IsChecked = true, Margin = new Thickness(2, 0) };
            ToolTip.SetTip(cStack, "Usa le impedenze attive delle antenne dello stack aperto (una sorgente per antenna) e le distanze per la lunghezza minima dei cavi di fase.");
            sx.Children.Add(cAcc); sx.Children.Add(cStack);
            sx.Children.Add(MeccFam("CAVI DI FASE"));
            List<Cable> cavi = new List<Cable>();
            try { cavi = Cables.Load(Path.Combine(AppPaths.DataDir, "cavi.csv")); } catch { }
            ComboBox cCavo = new ComboBox { Width = 130, ItemsSource = cavi.Select(c => c.Name).ToList(), SelectedIndex = cavi.Count > 0 ? Math.Max(0, cavi.FindIndex(c => c.Name.Contains("Ecoflex 10"))) : -1 };
            TextBox tMin = Tb("3,0", 60);
            sx.Children.Add(riga("Cavo", cCavo, "")); sx.Children.Add(riga("Lunghezza minima", tMin, "m"));
            CheckBox cTilt = new CheckBox { Content = "Fase sfalsata per inclinare il lobo (tilt elettrico)", Margin = new Thickness(2, 0) };
            TextBox tTilt = Tb("2", 50), tDv = Tb("2,0", 50);
            sx.Children.Add(cTilt); sx.Children.Add(riga("Tilt", tTilt, "° in su")); sx.Children.Add(riga("Passo verticale", tDv, "m"));
            Button bPdf = Btn("Disegno 1:1 (PDF)", true); bPdf.Margin = new Thickness(2, 12, 0, 0);
            sx.Children.Add(bPdf);

            Disegno dDis = new Disegno { Height = 330 };
            LineChart gRos = new LineChart { Height = 330, XLabel = "frequenza (MHz)", YLabel = "ROS", EmptyText = "" };
            StackPanel carte = new StackPanel { Spacing = 6 };
            Grid tab = new Grid();
            StackPanel avv = new StackPanel { Spacing = 4, Margin = new Thickness(0, 8, 0, 0) };
            StackPanel cR = new StackPanel(); cR.Children.Add(carte); cR.Children.Add(tab); cR.Children.Add(avv);
            Grid tabF = new Grid();
            TextBlock notaF = new TextBlock { FontSize = 12.5, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            StackPanel cF = new StackPanel(); cF.Children.Add(tabF); cF.Children.Add(notaF);
            Grid gr = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*"), RowDefinitions = new RowDefinitions("Auto,10,Auto") };
            Border p1 = Pannello("Disegno quotato (stampabile 1:1)", dDis), p2 = Pannello("Risultati", cR), p3 = Pannello("ROS all'ingresso (carichi = impedenze attive dello Stack)", gRos), p4 = Pannello("Cavi di fase", cF);
            Grid.SetColumn(p2, 2); Grid.SetRow(p3, 2); Grid.SetRow(p4, 2); Grid.SetColumn(p4, 2);
            foreach (Border b in new[] { p1, p2, p3, p4 }) gr.Children.Add(b);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*"), Margin = new Thickness(4) };
            g.Children.Add(new ScrollViewer { Content = sx }); Grid.SetColumn(gr, 2); g.Children.Add(gr);

            double f0 = 144.2, z0 = 25, dInt = 14.2, lAria = 519.7, lTaglio = 512, lato = 20; int n = 4; double er = 1;
            Func<string, string, Border> carta = (v, s) =>
            {
                StackPanel sp = new StackPanel { Spacing = 2 };
                sp.Children.Add(new TextBlock { Text = v, FontSize = 22, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Accent) });
                sp.Children.Add(new TextBlock { Text = s, FontSize = 12.5, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap });
                return new Border { Child = sp, Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6) };
            };
            dDis.Disegna = (ctx, rr) =>
            {
                if (tipo == "fase") { Draw.TextC(ctx, "solo cavi di fase: nessun trasformatore da costruire", rr.Width / 2, rr.Height / 2, 13, Pal.Muted); return; }
                double L = 70, R = rr.Width - 90, y = 70, h = 70;
                Draw.Text(ctx, "vista laterale (scala ridotta a schermo; in stampa 1:1)", L, 10, 12, Pal.Muted);
                Pen nero = new Pen(new SolidColorBrush(Color.Parse("#2B3A4A")), 2);
                if (tipo == "cavo" || tipo == "wilk")
                {
                    ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#1D5FB0")), 6), new Point(L, y + h / 2), new Point(R, y + h / 2));
                    Draw.Text(ctx, tipo == "wilk" ? n + " rami λ/4 da " + FN(z0, 1) + " Ω e " + n + " resistenze da " + FN(ParseNum(tZi.Text, 50), 0) + " Ω a stella" : "2 cavi da 75 Ω in parallelo = 37,5 Ω", L, y + h / 2 + 10, 12, Pal.Fore);
                }
                else
                {
                    ctx.DrawRectangle(null, nero, new Rect(L, y, R - L, h));
                    ctx.DrawRectangle(null, new Pen(Pal.B(Pal.Muted), 1), new Rect(L + 6, y + 6, R - L - 12, h - 12));
                    double dh = h * dInt / lato;
                    ctx.FillRectangle(new SolidColorBrush(Color.Parse("#C9D9EA")), new Rect(L, y + h / 2 - dh / 2, R - L, dh));
                }
                ctx.FillRectangle(new SolidColorBrush(Color.Parse("#7A8696")), new Rect(L - 28, y + h / 2 - 14, 28, 28));
                for (int i = 0; i < Math.Min(n, 6); i++) ctx.FillRectangle(new SolidColorBrush(Color.Parse("#7A8696")), new Rect(R - 6, y - 4 + i * (h + 8) / Math.Min(n, 6), 26, 18));
                Draw.Text(ctx, "IN (" + (cCon.SelectedIndex < 3 ? cCon.SelectedItem.ToString().Split(',')[0] : "-") + ")", L - 30, y + h + 6, 11.5, Pal.Fore);
                Draw.Text(ctx, n + " × OUT", R + 2, y - 26, 11.5, Pal.Fore);
                ctx.DrawLine(new Pen(Pal.B(Pal.Fore), 1), new Point(L, y + h + 30), new Point(R, y + h + 30));
                ctx.DrawLine(new Pen(Pal.B(Pal.Fore), 1), new Point(L, y + h + 24), new Point(L, y + h + 36)); ctx.DrawLine(new Pen(Pal.B(Pal.Fore), 1), new Point(R, y + h + 24), new Point(R, y + h + 36));
                Draw.TextC(ctx, FN(lTaglio, 0) + " mm (λ/4 = " + FN(lAria, 1) + " mm" + (cAcc.IsChecked == true && cCon.SelectedIndex < 3 ? " meno accorciamento connettori" : "") + ")", (L + R) / 2, y + h + 44, 12, Pal.Fore);
                if (tipo == "quadro" || tipo == "tondo")
                {
                    double s0 = 90, x0 = L, y0 = y + h + 66;
                    if (tipo == "quadro") ctx.DrawRectangle(null, nero, new Rect(x0, y0, s0, s0)); else ctx.DrawEllipse(null, nero, new Point(x0 + s0 / 2, y0 + s0 / 2), s0 / 2, s0 / 2);
                    ctx.DrawEllipse(new SolidColorBrush(Color.Parse("#C9D9EA")), new Pen(Pal.B(Pal.Fore), 1), new Point(x0 + s0 / 2, y0 + s0 / 2), s0 / 2 * dInt / lato, s0 / 2 * dInt / lato);
                    double dd = Math.Abs(Divisori.Z0Quadro(lato, dInt - 0.2, er) - z0);
                    Draw.Text(ctx, "sezione: tubo " + (tipo == "quadro" ? "quadro, lato" : "tondo, Ø") + " interno " + FN(lato, 1) + " mm", x0 + s0 + 16, y0 + 10, 12, Pal.Fore);
                    Draw.Text(ctx, "conduttore tondo d = " + FN(dInt, 1) + " mm", x0 + s0 + 16, y0 + 30, 12, Pal.Fore);
                    Draw.Text(ctx, "tolleranza su d: ± 0,2 mm (± " + FN(dd, 1) + " Ω)", x0 + s0 + 16, y0 + 50, 12, Pal.Fore);
                }
            };
            Action calcola = () =>
            {
                f0 = Math.Max(1, ParseNum(tF.Text, 144.2)); n = int.Parse(cN.SelectedItem.ToString()); er = cDie.SelectedIndex == 1 ? 2.1 : 1; lato = ParseNum(tD.Text, 20);
                double za = ParseNum(tZa.Text, 50), zi = ParseNum(tZi.Text, 50);
                double zcar = za / n;
                z0 = tipo == "cavo" ? 37.5 : tipo == "wilk" ? zi * Math.Sqrt(n) : Divisori.Z0Richiesta(zi, za, n);
                double vf = tipo == "cavo" || tipo == "wilk" ? (cCavo.SelectedIndex >= 0 && cCavo.SelectedIndex < cavi.Count ? cavi[cCavo.SelectedIndex].Vf : 0.66) : 1 / Math.Sqrt(er);
                dInt = tipo == "quadro" ? Divisori.DQuadro(z0, lato, er) : Divisori.DTondo(z0, lato, er);
                lAria = Divisori.QuartoMm(f0, 1);
                double lLinea = Divisori.QuartoMm(f0, vf);
                lTaglio = lLinea - (cAcc.IsChecked == true ? 2 * Divisori.AccorciamentoMm[Math.Max(0, cCon.SelectedIndex)] : 0);
                // carico: impedenze attive dello stack, se ci sono e se richiesto
                Complex zl = zcar; string fonteZ = "Z antenne / " + n;
                if (cStack.IsChecked == true && run != null && run.Sources.Count == n && Math.Abs(run.FreqMHz - f0) / f0 < 0.05)
                {
                    Complex y = Complex.Zero; foreach (var s in run.Sources) y += 1 / s.Z;
                    zl = 1 / y; fonteZ = "impedenze attive delle " + n + " antenne dello stack in parallelo";
                }
                double soglia = 1.1, fa, fb;
                if (tipo == "fase") { fa = fb = double.NaN; }
                else Divisori.Banda(zl, z0, zi, f0, soglia, out fa, out fb);
                double perdita = tipo == "quadro" || tipo == "tondo" ? Divisori.PerditaAl(f0, z0, tipo == "quadro" ? 1.0787 * lato : lato, dInt, lLinea) :
                    (cCavo.SelectedIndex >= 0 && cCavo.SelectedIndex < cavi.Count ? cavi[cCavo.SelectedIndex].AttenuationAt(f0) * lLinea / 100000 : double.NaN);
                carte.Children.Clear();
                Grid c4 = new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,*,8,*,8,*") };
                Border[] cc =
                {
                    carta(FN(z0, 1) + " Ω", tipo == "wilk" ? "Z0 dei rami (Zin · √N)" : "Z0 richiesta"),
                    carta(tipo == "quadro" || tipo == "tondo" ? FN(dInt, 1) + " mm" : FN(vf, 2), tipo == "quadro" || tipo == "tondo" ? "Ø conduttore interno" : "VF del cavo"),
                    carta(FN(lAria, 1), "λ/4 in aria, mm"),
                    carta("≈ " + FN(lTaglio, 0), "da tagliare, mm")
                };
                if (tipo != "fase") for (int i = 0; i < 4; i++) { Grid.SetColumn(cc[i], 2 * i); c4.Children.Add(cc[i]); }
                carte.Children.Add(c4);
                tab.Children.Clear(); tab.RowDefinitions.Clear(); tab.ColumnDefinitions = new ColumnDefinitions("*,Auto");
                int r0 = 0;
                Action<string, string, bool> rg3 = (a, b, h) =>
                {
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    Border b1 = new Border { Child = new TextBlock { Text = a, FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal }, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                    Border b2 = new Border { Child = new TextBlock { Text = b, FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal }, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                    Grid.SetRow(b1, r0); Grid.SetRow(b2, r0); Grid.SetColumn(b2, 1); tab.Children.Add(b1); tab.Children.Add(b2); r0++;
                };
                if (tipo != "fase")
                {
                    Action<string, string> rg = (a, b) => rg3(a, b, false);
                    rg3("Formula", "Valore", true);
                    rg("Z carico in parallelo", FN(za, 0) + " / " + n + " = " + FN(zcar, 1) + " Ω");
                    if (tipo == "wilk") { rg("Rami λ/4: Z0 = Zin · √N", FN(zi, 0) + " · √" + n + " = " + FN(z0, 1) + " Ω"); rg("Resistenze a stella", n + " × " + FN(zi, 0) + " Ω, ciascuna fino a P/" + n + " se un'antenna si stacca"); rg("Isolamento tra le uscite", "ideale a f0; cala lontano da f0 (rami λ/4)"); }
                    else rg("Z0 = √(Zin · Zcarico)", "√(" + FN(zi, 0) + " · " + FN(zcar, 1) + ") = " + FN(Divisori.Z0Richiesta(zi, za, n), 1) + " Ω" + (tipo == "cavo" ? " (2 × 75 Ω = 37,5 Ω)" : ""));
                    if (tipo == "quadro") rg("Quadro/tondo: Z0 = 138 · log(1,0787 · D/d)", "D/d = " + FN(lato / dInt, 3));
                    if (tipo == "tondo") rg("Coassiale: Z0 = 138 · log(D/d)", "D/d = " + FN(lato / dInt, 3));
                    rg("Banda con ROS < " + FN(soglia, 1), double.IsNaN(fa) ? "-" : FN(fa, 1) + " – " + FN(fb, 1) + " MHz");
                    rg("Perdita stimata" + (tipo == "quadro" || tipo == "tondo" ? " (Al, aria)" : " (cavo)"), double.IsNaN(perdita) ? "-" : FN(perdita, 3) + " dB");
                    rg("Carico usato", fonteZ);
                }
                avv.Children.Clear();
                if (n % 2 == 1 || n == 6) avv.Children.Add(AvvisoGiallo("Numero " + (n % 2 == 1 ? "dispari" : "non potenza di 2") + " di antenne (" + n + "): il divisore λ/4 a stella non da' una Z0 standard → il programma propone due trasformatori in cascata o il Wilkinson a " + n + " vie. Il Wilkinson isola le uscite: utile anche in 144 MHz quando un'antenna dello stack si guasta o si stacca."));
                if (tipo == "cavo" && Math.Abs(Divisori.Z0Richiesta(zi, za, n) - 37.5) > 3) avv.Children.Add(AvvisoGiallo("Con " + n + " uscite serve Z0 = " + FN(Divisori.Z0Richiesta(zi, za, n), 1) + " Ω: due cavi da 75 Ω in parallelo (37,5 Ω) vanno bene solo per 2 uscite da 50 Ω (35,4 Ω)."));
                // ROS in frequenza
                double[] fs = Enumerable.Range(0, 81).Select(i => f0 * (0.97 + 0.06 * i / 80)).ToArray();
                gRos.X = fs; gRos.HLines = new[] { soglia }; gRos.MarkX = f0; gRos.YMinFixed = 1;
                gRos.Series = tipo == "fase" ? new List<LineSeries>() : new List<LineSeries> { new LineSeries { Name = "ROS all'ingresso", Y = fs.Select(f => Analysis.Swr(Divisori.Zin(zl, z0, f0, f), zi)).ToArray(), Color = Color.Parse("#1D5FB0"), Width = 2.4, Decimals = 2 } };
                gRos.EmptyText = tipo == "fase" ? "solo cavi di fase" : ""; gRos.InvalidateVisual();
                // cavi di fase
                tabF.Children.Clear(); tabF.RowDefinitions.Clear(); tabF.ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto,Auto");
                r0 = 0;
                Action<string[], bool> rf = (v, h) =>
                {
                    tabF.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int j = 0; j < v.Length; j++)
                    {
                        Border b = new Border { Child = new TextBlock { Text = v[j], FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal, HorizontalAlignment = j < 2 ? HorizontalAlignment.Left : HorizontalAlignment.Right }, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                        Grid.SetColumn(b, j); Grid.SetRow(b, r0); tabF.Children.Add(b);
                    }
                    r0++;
                };
                rf(new[] { "Uscita", "Cavo", "VF", "Lungh. elettrica", "Fisica mm", "Perdita dB" }, true);
                if (cCavo.SelectedIndex >= 0 && cCavo.SelectedIndex < cavi.Count)
                {
                    Cable cv = cavi[cCavo.SelectedIndex];
                    double minimo = ParseNum(tMin.Text, 3) * 1000;
                    if (cStack.IsChecked == true && stackInfo != null)
                    {
                        double dv = double.IsNaN(stackInfo.SpacingV) ? 0 : stackInfo.SpacingV * (stackInfo.Rows - 1), dh = double.IsNaN(stackInfo.SpacingH) ? 0 : stackInfo.SpacingH * (stackInfo.Cols - 1);
                        minimo = Math.Max(minimo, (Math.Sqrt(dv * dv / 4 + dh * dh / 4) + 0.5) * 1000);
                    }
                    int righe = stackInfo != null ? stackInfo.Rows : 1, colonne = stackInfo != null ? stackInfo.Cols : n;
                    for (int i = 0; i < n; i++)
                    {
                        int rIdx = righe > 1 ? (i / Math.Max(1, colonne)) : 0;
                        double extra = cTilt.IsChecked == true && righe > 1 ? Divisori.ExtraTilt(f0, cv.Vf, ParseNum(tDv.Text, 2), ParseNum(tTilt.Text, 2)) * rIdx : 0;
                        var cf = Divisori.Fase(f0, cv.Vf, cv.AttenuationAt(f0), minimo, extra);
                        string nome = (i + 1) + (n == 4 && righe == 2 ? new[] { " (alto sx)", " (alto dx)", " (basso sx)", " (basso dx)" }[i] : "");
                        rf(new[] { nome, cv.Name, FN(cv.Vf, 2), cf.Mezze + " · λ/2" + (extra > 0 ? " + " + FN(extra, 0) + " mm" : ""), FN(cf.FisicaMm, 0), FN(cf.PerditaDb, 2) }, false);
                    }
                    double tolFase = 360 * 5 / (Divisori.C / f0 * cv.Vf);
                    notaF.Text = "Lunghezze uguali → fase uguale. Multipli di λ/2 → l'impedenza attiva si ripete al divisore. Tolleranza consigliata ± 5 mm (= ± " + FN(tolFase, 1) + "° di fase)." +
                        (cTilt.IsChecked == true ? " Tilt: ogni riga piu' in alto ha il cavo piu' lungo di " + FN(Divisori.ExtraTilt(f0, cv.Vf, ParseNum(tDv.Text, 2), ParseNum(tTilt.Text, 2)), 0) + " mm (lobo inclinato di " + FN(ParseNum(tTilt.Text, 2), 1) + "° verso l'alto); l'impedenza al divisore non si ripete piu' esattamente." : "");
                }
                dDis.InvalidateVisual();
            };
            Action agg = () => { try { calcola(); } catch (Exception ex) { Status("Divisori: " + ex.Message, true); } };
            fT.Selected += k => { tipo = k; agg(); };
            foreach (TextBox t in new[] { tF, tZa, tZi, tD, tMin, tTilt, tDv }) t.LostFocus += (s, e) => agg();
            foreach (ComboBox c in new[] { cN, cDie, cCon, cCavo }) c.SelectionChanged += (s, e) => agg();
            foreach (CheckBox c in new[] { cAcc, cStack, cTilt }) c.IsCheckedChanged += (s, e) => agg();
            bPdf.Click += async (s, e) =>
            {
                if (tipo == "fase") { Status("Solo cavi di fase: nessun disegno da stampare"); return; }
                try
                {
                    IStorageFile fl = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Disegno 1:1 del divisore (PDF)", SuggestedFileName = "Divisore " + n + " vie " + FN(f0, 1) + " MHz.pdf", DefaultExtension = "pdf", FileTypeChoices = new[] { new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } } });
                    if (fl == null) return;
                    using (Stream st = await fl.OpenWriteAsync()) DivisorePdf(st, tipo, f0, n, z0, lato, dInt, lTaglio, lAria);
                    Status("Disegno 1:1 del divisore: " + fl.Name);
                }
                catch (Exception ex) { Status("Disegno PDF: " + ex.Message, true); }
            };
            fT.Select("quadro", false);
            agg();
            return new ScrollViewer { Content = g };
        }

        /// <summary>Disegno 1:1 su fogli A4 orizzontali con 10 mm di sovrapposizione: vista laterale quotata e sezione.</summary>
        static void DivisorePdf(Stream s, string tipo, double f0, int n, double z0, double lato, double dInt, double lTaglio, double lAria)
        {
            const float PtMm = 72f / 25.4f, W = 297f, H = 210f, M = 10f, Sovr = 10f;
            float larg = (float)lTaglio + 60, passo = W - 2 * M - Sovr;
            int fogli = Math.Max(1, (int)Math.Ceiling((larg - Sovr) / passo));
            CultureInfo IT = CultureInfo.GetCultureInfo("it-IT");
            using (SKManagedWStream ws = new SKManagedWStream(s))
            using (SKDocument doc = SKDocument.CreatePdf(ws, new SKDocumentPdfMetadata { Title = "Divisore", Creator = "YA-NEC", Producer = "YA-NEC (SkiaSharp)" }))
            using (SKPaint linea = new SKPaint { Color = SKColors.Black, StrokeWidth = 0.4f, IsStroke = true, IsAntialias = true })
            using (SKPaint fine = new SKPaint { Color = new SKColor(0xFF606060), StrokeWidth = 0.2f, IsStroke = true, IsAntialias = true })
            using (SKPaint testo = new SKPaint { Color = SKColors.Black, TextSize = 3.2f, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default })
            {
                for (int k = 0; k < fogli; k++)
                {
                    SKCanvas c = doc.BeginPage(W * PtMm, H * PtMm);
                    c.Scale(PtMm);
                    c.DrawText(string.Format(IT, "YA-NEC {0} - divisore λ/4 a {1} vie, {2:0.000} MHz, Z0 {3:0.0} ohm - foglio {4} di {5} (scala 1:1, sovrapposizione {6:0} mm)", AppPaths.Version, n, f0, z0, k + 1, fogli, Sovr), M, M + 3, testo);
                    c.DrawLine(M, H - M - 2, M + 50, H - M - 2, linea);
                    c.DrawText("50 mm di controllo", M, H - M - 4, testo);
                    c.Save();
                    c.ClipRect(new SKRect(M, M + 6, W - M, H - M - 8));
                    c.Translate(M - k * passo, 0);
                    float x0 = 30, y0 = 50, hh = (float)(tipo == "quadro" || tipo == "tondo" ? lato + 4 : 8), L = (float)lTaglio;
                    c.DrawRect(x0, y0, L, hh, linea);
                    if (tipo == "quadro" || tipo == "tondo")
                    {
                        float di = (float)dInt;
                        c.DrawRect(x0, y0 + hh / 2 - di / 2, L, di, fine);
                        c.DrawRect(x0, y0 + 2, L, hh - 4, fine);
                    }
                    c.DrawLine(x0, y0 + hh + 12, x0 + L, y0 + hh + 12, linea);
                    c.DrawLine(x0, y0 + hh + 8, x0, y0 + hh + 16, linea); c.DrawLine(x0 + L, y0 + hh + 8, x0 + L, y0 + hh + 16, linea);
                    for (float t = 0; t <= L; t += 50) { c.DrawLine(x0 + t, y0 + hh + 12, x0 + t, y0 + hh + 14, fine); c.DrawText(string.Format(IT, "{0:0}", t), x0 + t + 0.5f, y0 + hh + 19, testo); }
                    c.DrawText(string.Format(IT, "{0:0.0} mm da tagliare (λ/4 in aria {1:0.0} mm, meno accorciamento dei connettori)", lTaglio, lAria), x0 + 5, y0 - 4, testo);
                    if (tipo == "quadro" || tipo == "tondo")
                    {
                        float sx0 = x0, sy0 = y0 + hh + 30, ls = (float)lato;
                        if (tipo == "quadro") c.DrawRect(sx0, sy0, ls, ls, linea); else c.DrawCircle(sx0 + ls / 2, sy0 + ls / 2, ls / 2, linea);
                        c.DrawCircle(sx0 + ls / 2, sy0 + ls / 2, (float)dInt / 2, linea);
                        c.DrawText(string.Format(IT, "sezione 1:1 - {0} interno {1:0.0} mm, conduttore tondo d = {2:0.0} mm (± 0,2 mm)", tipo == "quadro" ? "lato" : "Ø", lato, dInt), sx0 + ls + 6, sy0 + ls / 2, testo);
                    }
                    c.Restore();
                    doc.EndPage();
                }
                doc.Close();
            }
        }
    }
}
