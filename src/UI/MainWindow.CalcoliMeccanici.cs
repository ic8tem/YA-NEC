// YA-NEC 1.2.0 - linguetta principale "Calcoli meccanici" (bozzetti VHF 8 e 12): Vento e Bilanciamento; Boom e tirante, Palo e plinto e
// Sollecitazioni elementi (bozzetto 13, da approvare) restano visibili ma spente. Tutte le forze in N e kgf (scelta "Mostra").
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        static readonly string[] MeccVoci = { "Vento", "Bilanciamento", "Boom e tirante", "Palo e plinto", "Sollecitazioni elementi (HF)" };
        ContentControl meccCorpo;
        readonly Dictionary<string, Button> meccBtn = new Dictionary<string, Button>();
        readonly Dictionary<string, Control> meccPagine = new Dictionary<string, Control>();
        int meccUnita;                 // 0 = N e kgf affiancati, 1 = solo N, 2 = solo kgf
        double meccQ = double.NaN;     // pressione di progetto dell'ultimo calcolo del Vento (Pa), per il Bilanciamento
        Action meccAggiornaVento, meccAggiornaBil;
        string meccVoceAttiva = "Vento";   // 1.5.1: voce mostrata (per ricalcolarla quando si riapre la famiglia)

        /// <summary>Forza in N e/o kgf secondo la scelta "Mostra".</summary>
        string UF(double n, int dec = 0)
        {
            string a = FN(n, dec) + " N", b = FN(n / 9.80665, n / 9.80665 < 100 ? 1 : 0) + " kgf";
            return meccUnita == 1 ? a : meccUnita == 2 ? b : a + " · " + b;
        }
        string UM(double nm)
        {
            string a = Math.Abs(nm) >= 1000 ? FN(nm / 1000, 1) + " kN·m" : FN(nm, 0) + " N·m", b = FN(nm / 9.80665, Math.Abs(nm / 9.80665) < 100 ? 1 : 0) + " kgf·m";
            return meccUnita == 1 ? a : meccUnita == 2 ? b : a + " · " + b;
        }

        Control BuildCalcoliMeccaniciPage()
        {
            WrapPanel fascia = new WrapPanel { Margin = new Thickness(8, 6) };
            foreach (string v in MeccVoci)
            {
                // 1.5: Sollecitazioni elementi accesa (bozzetto 13 approvato il 18/11/2026 21:28: e' la pagina Meccanica alla YagiStress);
                // Boom e tirante approvato ma ancora da fare; Palo e plinto lo prepara IC8TEM
                bool futura = v != "Vento" && v != "Bilanciamento" && v != "Sollecitazioni elementi (HF)";
                Button b = new Button { Content = v, Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(14, 4), CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(1), IsEnabled = !futura };
                if (futura) ToolTip.SetTip(b, v == "Palo e plinto" ? "La prepara IC8TEM: non ancora attiva." : "Approvata, in preparazione: non ancora attiva in questa versione.");
                string k = v; b.Click += (s, e) => ApriVoceMeccanica(k);
                meccBtn[v] = b; fascia.Children.Add(b);
            }
            Border bf = new Border { Background = new SolidColorBrush(Color.Parse("#2F4A66")), Child = fascia };
            meccCorpo = new ContentControl();
            DockPanel d = new DockPanel();
            DockPanel.SetDock(bf, Dock.Top); d.Children.Add(bf); d.Children.Add(meccCorpo);
            ApriVoceMeccanica("Vento");
            return d;
        }

        public void ApriVoceMeccanica(string v)
        {
            foreach (KeyValuePair<string, Button> kv in meccBtn)
            {
                bool on = kv.Key == v;
                kv.Value.Background = new SolidColorBrush(on ? Color.Parse("#E08A00") : Color.Parse("#2F4A66"));
                kv.Value.BorderBrush = new SolidColorBrush(on ? Color.Parse("#E08A00") : Color.Parse("#4A6787"));
                kv.Value.Foreground = new SolidColorBrush(Color.Parse(kv.Value.IsEnabled ? (on ? "#FFFFFF" : "#E6EDF5") : "#8193A8"));
                kv.Value.FontWeight = on ? FontWeight.SemiBold : FontWeight.Normal;
            }
            Control c;
            if (!meccPagine.TryGetValue(v, out c)) { c = v == "Vento" ? BuildVentoPage() : v == "Bilanciamento" ? BuildBilanciamentoPage() : v == "Sollecitazioni elementi (HF)" ? BuildMeccanicaPanel() : new TextBlock(); meccPagine[v] = c; }
            meccCorpo.Content = c;
            meccVoceAttiva = v;
            Registro.Scrivi("AZIONE", "calcoli meccanici: " + v);
            if (v == "Vento") meccAggiornaVento?.Invoke(); else if (v == "Bilanciamento") meccAggiornaBil?.Invoke();
        }

        // elementi del modello (un tag diritto = un elemento) con posizione lungo il boom dal riflettore
        List<Tolleranze.Elemento> MeccElementi()
        {
            if (run == null || run.Geometry == null) return new List<Tolleranze.Elemento>();
            int asse; return Tolleranze.Elementi(model, run.Geometry, out asse);
        }

        static readonly string[] MeccBoomTipi = { "Al quadro 20×2 mm", "Al quadro 25×2 mm", "Al quadro 30×2 mm", "Al quadro 40×2 mm", "Al tondo 25×1,5 mm", "Al tondo 30×2 mm", "Al tondo 40×2 mm", "Al tondo 50×2 mm" };
        static void BoomDati(int i, out double lato, out double sp, out bool quadro)
        {
            double[] l = { 0.020, 0.025, 0.030, 0.040, 0.025, 0.030, 0.040, 0.050 }, s = { 0.002, 0.002, 0.002, 0.002, 0.0015, 0.002, 0.002, 0.002 };
            i = Math.Max(0, Math.Min(7, i)); lato = l[i]; sp = s[i]; quadro = i < 4;
        }

        Func<string, Control, string, Grid> MeccRiga()
        {
            return (et, c, um) =>
            {
                Grid g0 = new Grid { ColumnDefinitions = new ColumnDefinitions("118,Auto,Auto"), Margin = new Thickness(2, 1) };
                g0.Children.Add(new TextBlock { Text = et, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap });
                Grid.SetColumn(c, 1); g0.Children.Add(c);
                TextBlock u = new TextBlock { Text = um, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };
                Grid.SetColumn(u, 2); g0.Children.Add(u);
                return g0;
            };
        }
        static TextBlock MeccFam(string s) { return new TextBlock { Text = s, FontWeight = FontWeight.Bold, FontSize = 13, Foreground = new SolidColorBrush(Fascione.FamigliaTesto), Margin = new Thickness(2, 10, 0, 4) }; }
        Border MeccCarta(string v, string v2, string s)
        {
            StackPanel sp = new StackPanel { Spacing = 2 };
            TextBlock t = new TextBlock { FontSize = 24, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Accent), TextWrapping = TextWrapping.Wrap };
            t.Inlines.Add(new Avalonia.Controls.Documents.Run { Text = v });
            if (!string.IsNullOrEmpty(v2)) t.Inlines.Add(new Avalonia.Controls.Documents.Run { Text = " · " + v2, FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore) });
            sp.Children.Add(t);
            sp.Children.Add(new TextBlock { Text = s, FontSize = 12.5, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap });
            return new Border { Child = sp, Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8) };
        }
        void MeccSeparaUnita(string s, out string a, out string b) { int i = s.IndexOf(" · "); a = i < 0 ? s : s.Substring(0, i); b = i < 0 ? "" : s.Substring(i + 3); }

        // ------------------------------------------------------------------------------------------------
        // Vento (bozzetto VHF 8)
        // ------------------------------------------------------------------------------------------------
        Control BuildVentoPage()
        {
            var riga = MeccRiga();
            StackPanel sx = new StackPanel { Width = 262, Spacing = 2 };
            Fascione fN = new Fascione(); fN.Famiglia("NORMA");
            fN.Voce("ntc", "NTC 2018 + CNR-DT 207 / EC1"); fN.Voce("tia", "TIA-222-H (testo non disponibile)", true); fN.Voce("asce", "ASCE 7-22 · AS/NZS 1170.2 · AIJ", true);
            sx.Children.Add(fN);
            sx.Children.Add(MeccFam("SITO"));
            string locIniz = "JN70CN";
            try { if (File.Exists(FileTerreno)) locIniz = File.ReadAllText(FileTerreno).Trim(); } catch { }
            TextBox tLoc = Tb(locIniz, 120);
            ComboBox cZona = new ComboBox { Width = 120, ItemsSource = Meccanica.Zone.Select(z => z.N + " – " + z.Regioni).ToList(), SelectedIndex = 2 };
            TextBox tQs = Tb("62", 60), tTr = Tb("50", 60), tCt = Tb("1,00", 60);
            ComboBox cCat = new ComboBox { Width = 120, ItemsSource = Meccanica.NomiCategorie.Select(c => "categoria " + c).ToList(), SelectedIndex = 1 };
            sx.Children.Add(riga("Locatore", tLoc, "")); sx.Children.Add(riga("Zona NTC", cZona, "")); sx.Children.Add(riga("Quota sito as", tQs, "m"));
            sx.Children.Add(riga("Periodo ritorno", tTr, "anni")); sx.Children.Add(riga("Categoria esposiz.", cCat, "")); sx.Children.Add(riga("Coeff. topografia", tCt, ""));
            TextBlock notaZona = new TextBlock { FontSize = 11.5, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 0, 0, 0) };
            sx.Children.Add(notaZona);
            sx.Children.Add(MeccFam("STRUTTURA"));
            TextBox tH = Tb("12,0", 60), tPalo = Tb("2,5", 60), tDp = Tb("50", 60);
            ComboBox cBoom = new ComboBox { Width = 120, ItemsSource = MeccBoomTipi, SelectedIndex = 1 };
            sx.Children.Add(riga("Antenna da terra", tH, "m")); sx.Children.Add(riga("Palo sopra rotore", tPalo, "m")); sx.Children.Add(riga("Ø palo", tDp, "mm")); sx.Children.Add(riga("Boom", cBoom, ""));
            CheckBox cMod = new CheckBox { Content = "Usa elementi e boom del modello", IsChecked = true }, cTel = new CheckBox { Content = "Includi telaio, palo, cavi", IsChecked = true }, cScan = new CheckBox { Content = "Scansione direzione del vento", IsChecked = true };
            foreach (CheckBox c in new[] { cMod, cTel, cScan }) { c.Margin = new Thickness(2, 0); sx.Children.Add(c); }
            sx.Children.Add(MeccFam("UNITÀ"));
            ComboBox cU = new ComboBox { Width = 140, ItemsSource = new[] { "N e kgf affiancati", "solo N", "solo kgf" }, SelectedIndex = meccUnita };
            sx.Children.Add(riga("Mostra", cU, ""));
            sx.Children.Add(new TextBlock { Text = "1 kgf = 9,80665 N", FontSize = 12, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(2, 2, 0, 0) });
            Button bCalc = Btn("Calcola", true), bPdf = Btn("Relazione PDF", false);
            StackPanel rb = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(2, 12, 0, 0) }; rb.Children.Add(bCalc); rb.Children.Add(bPdf);
            sx.Children.Add(rb);

            Grid press = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*,10,*,10,*") };
            Grid tab = new Grid();
            TextBlock notaTab = new TextBlock { FontSize = 12, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            StackPanel cT = new StackPanel(); cT.Children.Add(tab); cT.Children.Add(notaTab);
            LineChart gDir = new LineChart { Height = 330, XLabel = "direzione del vento rispetto al boom (°)", YLabel = "forza (N)", Y2Label = "N·m", EmptyText = "premi Calcola" };
            Grid due = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*"), Margin = new Thickness(0, 10, 0, 10) };
            Border b1 = Pannello("Forze per parte (vento lungo il boom: elementi di fianco)", cT), b2 = Pannello("Forza e momento in funzione della direzione del vento", gDir);
            Grid.SetColumn(b2, 2); due.Children.Add(b1); due.Children.Add(b2);
            Grid fondo = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*,10,*,10,*") };
            StackPanel dx = new StackPanel();
            dx.Children.Add(Pannello("Pressione di progetto", press)); dx.Children.Add(due); dx.Children.Add(fondo);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*"), Margin = new Thickness(4) };
            g.Children.Add(new ScrollViewer { Content = sx }); Grid.SetColumn(dx, 2); g.Children.Add(dx);

            MeccanicaVento.EsitoVento ultimo = null;
            Action zonaDaLoc = () =>
            {
                double la, lo;
                if (!Dem.DaLocatore(tLoc.Text, out la, out lo)) { notaZona.Text = "locatore non valido: zona da scegliere a mano"; return; }
                int z = MeccanicaVento.ZonaNtc(la, lo);
                if (z == 0) { notaZona.Text = "fuori dall'Italia: zona e vb da scegliere a mano (NTC vale in Italia)"; return; }
                cZona.SelectedIndex = z - 1;
                notaZona.Text = "zona " + z + " proposta dal locatore (confini approssimati: verifica sulla tabella 3.3.I delle NTC)";
            };
            Action calcola = () =>
            {
                meccUnita = Math.Max(0, cU.SelectedIndex);
                Meccanica.Zona z = Meccanica.Zone[Math.Max(0, cZona.SelectedIndex)];
                double tr = Math.Max(5, ParseNum(tTr.Text, 50));
                double cr = 0.75 * Math.Sqrt(1 - 0.2 * Math.Log(-Math.Log(1 - 1 / tr)));   // NTC 2018 (3.3.2)
                double hAnt = ParseNum(tH.Text, 12), lPalo = ParseNum(tPalo.Text, 2.5), dPalo = ParseNum(tDp.Text, 50) / 1000;
                double vbStima = Meccanica.Vb(z, ParseNum(tQs.Text, 0)) * cr;
                List<MeccanicaVento.Parte> parti = new List<MeccanicaVento.Parte>();
                List<Tolleranze.Elemento> el = cMod.IsChecked == true ? MeccElementi() : new List<Tolleranze.Elemento>();
                int copie = stackInfo != null ? stackInfo.Copies : 1;
                double xMin = el.Count > 0 ? el.Min(e => e.Pos) : 0, xMax = el.Count > 0 ? el.Max(e => e.Pos) : 2;
                double lBoom = xMax - xMin + 0.10, xRot = (xMin + xMax) / 2;
                // staffa del rotore al baricentro del peso (elementi pieni + boom), come la posizione bilanciata del Bilanciamento
                if (el.Count > 0)
                {
                    double lt0, st0; bool q0; BoomDati(cBoom.SelectedIndex, out lt0, out st0, out q0);
                    double mb = (q0 ? 4 * lt0 * st0 : Math.PI * lt0 * st0) * lBoom, sm = mb, sx0 = mb * (xMin + xMax) / 2;
                    foreach (var e in el) { double me = e.Diam * e.Diam * Math.PI / 4 * e.Lung; sm += me; sx0 += me * e.Pos; }
                    xRot = sx0 / sm;
                }
                if (el.Count > 0)
                {
                    double a = el.Sum(e => e.Lung * e.Diam), dm = el.Average(e => e.Diam), lm = el.Average(e => e.Lung);
                    double cf0 = MeccanicaVento.Cf0Cilindro(dm, vbStima), psi = Meccanica.PsiLambda(Math.Min(70, 2 * lm / 2 / Math.Max(dm, 1e-4)));
                    parti.Add(new MeccanicaVento.Parte { Nome = "Elementi (" + (copie > 1 ? copie + " × " + el.Count / copie : el.Count.ToString()) + ")", Area = a, Cf0 = cf0, Psi = psi, Asse = 0, X = el.Sum(e => e.Pos * e.Lung * e.Diam) / Math.Max(a, 1e-9) });
                }
                double lato, sp; bool quadro; BoomDati(cBoom.SelectedIndex, out lato, out sp, out quadro);
                parti.Add(new MeccanicaVento.Parte { Nome = "Boom (" + copie + ")", Area = copie * lato * lBoom, Cf0 = quadro ? 2.1 : MeccanicaVento.Cf0Cilindro(lato, vbStima), Psi = Meccanica.PsiLambda(Math.Min(70, lBoom / lato)), Asse = 1, X = (xMin + xMax) / 2, Quadro = quadro ? 1 : 0 });
                if (cTel.IsChecked == true)
                {
                    if (stackInfo != null && stackInfo.Copies > 1)
                    {
                        double alt = (double.IsNaN(stackInfo.SpacingV) ? 0 : stackInfo.SpacingV * (stackInfo.Rows - 1)) + 0.6, larg = double.IsNaN(stackInfo.SpacingH) ? 0 : stackInfo.SpacingH * (stackInfo.Cols - 1) + 0.2;
                        parti.Add(new MeccanicaVento.Parte { Nome = "Telaio a H", Area = 2 * alt * 0.040 + larg * 0.050, Cf0 = MeccanicaVento.Cf0Cilindro(0.04, vbStima), Psi = 0.8, Asse = 2, X = xRot });
                    }
                    parti.Add(new MeccanicaVento.Parte { Nome = "Palo sopra rotore", Area = lPalo * dPalo, Cf0 = MeccanicaVento.Cf0Cilindro(dPalo, vbStima), Psi = Meccanica.PsiLambda(Math.Min(70, 2 * lPalo / dPalo)), Asse = 2, X = xRot });
                    parti.Add(new MeccanicaVento.Parte { Nome = "Cavi e divisore", Area = copie * 2.0 * 0.0075 + (copie > 1 ? 0.02 : 0), Cf0 = 1.2, Psi = 0.8, Asse = 2, X = xRot });
                }
                foreach (var p in parti) p.Cf = 0;
                MeccanicaVento.EsitoVento r = MeccanicaVento.Calcola(z, Math.Max(0, cCat.SelectedIndex), ParseNum(tQs.Text, 0), hAnt + lPalo, ParseNum(tCt.Text, 1), parti, xRot, lPalo);
                // periodo di ritorno: cr sulla velocita', cr^2 sulla pressione
                r.Vb *= cr; r.Qr *= cr * cr; r.Q *= cr * cr; r.FMax *= cr * cr; r.MFlettente *= cr * cr; r.TMax *= cr * cr;
                for (int i = 0; i < r.Forza.Length; i++) { r.Forza[i] *= cr * cr; r.Torcente[i] *= cr * cr; }
                ultimo = r; meccQ = r.Q;
                // pressione
                press.Children.Clear();
                string a1, a2;
                Border[] pc =
                {
                    MeccCarta(FN(r.Vb, 1) + " m/s", "", "vb = vb,0 · ca · cr (zona " + z.N + ", Tr " + FN(tr, 0) + " anni: cr " + FN(cr, 3) + ")"),
                    MeccCarta(FN(r.Qr, 0) + " N/m²", FN(r.Qr / 9.80665, 1) + " kgf/m²", "qr = ½ · ρ · vr² (ρ = 1,25 kg/m³)"),
                    MeccCarta(FN(r.Ce, 2), "", "ce alla quota z = " + FN(hAnt + lPalo, 1) + " m (categoria " + Meccanica.NomiCategorie[Math.Max(0, cCat.SelectedIndex)] + ")"),
                    MeccCarta(FN(r.Q, 0) + " N/m²", FN(r.Q / 9.80665, 1) + " kgf/m²", "q = qr · ce · ct")
                };
                for (int i = 0; i < 4; i++) { Grid.SetColumn(pc[i], 2 * i); press.Children.Add(pc[i]); }
                // tabella delle parti (vento lungo il boom)
                tab.Children.Clear(); tab.RowDefinitions.Clear();
                tab.ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto,Auto,Auto,Auto");
                int rr = 0;
                Action<string[], bool> rg = (v, h) =>
                {
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int j = 0; j < v.Length; j++)
                    {
                        Border b = new Border { Child = new TextBlock { Text = v[j], FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal, HorizontalAlignment = j == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right }, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                        Grid.SetColumn(b, j); Grid.SetRow(b, rr); tab.Children.Add(b);
                    }
                    rr++;
                };
                rg(new[] { "Parte", "Area m²", "cf0", "ψλ", "cf", "F (N)", "F (kgf)" }, true);
                double fTot = 0, aTot = 0;
                foreach (MeccanicaVento.Parte p in r.Parti)
                {
                    double[] f = MeccanicaVento.ForzaXY(p, r.Q, 0);
                    double fv = Math.Sqrt(f[0] * f[0] + f[1] * f[1]);
                    fTot += fv; aTot += p.Area;
                    rg(new[] { p.Nome, FN(p.Area, 2), FN(p.Cf0, 2) + (p.Quadro > 0 ? "*" : ""), FN(p.Psi, 2), FN(p.Cf, 2), fv > 0.5 ? FN(fv, 0) : "—", fv > 0.5 ? FN(fv / 9.80665, 1) : "—" }, false);
                }
                rg(new[] { "Totale", FN(aTot, 2), "", "", "", FN(fTot, 0), FN(fTot / 9.80665, 1) }, true);
                notaTab.Text = "* boom quadro: coefficiente da §7.6 EC1; conta solo col vento lungo gli elementi. cf0 dei tubi tondi dal numero di Reynolds (EC1 §7.9.2, CNR-DT 207 App. G); ψλ dalla snellezza (EC1 §7.13). " +
                    "Con il vento di traverso gli elementi non spingono (flusso trasversale, K5IU): la forza su un tubo va con il quadrato della componente del vento perpendicolare al suo asse.";
                // grafico
                gDir.X = r.Dir; gDir.Series = new List<LineSeries>();
                if (cScan.IsChecked == true)
                {
                    gDir.Series.Add(new LineSeries { Name = "forza totale (N)", Y = r.Forza, Color = Color.Parse("#1D5FB0"), Width = 2.4, Decimals = 0 });
                    gDir.Series.Add(new LineSeries { Name = "momento torcente sul rotore (N·m)", Y = r.Torcente, Color = Color.Parse("#D0342C"), Width = 2.2, Right = true, Decimals = 1 });
                }
                gDir.InvalidateVisual();
                // risultati
                fondo.Children.Clear();
                MeccSeparaUnita(UF(r.FMax), out a1, out a2); Border f1 = MeccCarta(a1, a2, "forza massima (vento a " + FN(r.DirMax, 0) + "° dal boom)");
                MeccSeparaUnita(UM(r.MFlettente), out a1, out a2); Border f2 = MeccCarta(a1, a2, "momento flettente alla staffa del rotore");
                MeccSeparaUnita(UM(r.TMax), out a1, out a2); Border f3 = MeccCarta(a1, a2, "momento torcente sul rotore con la staffa al baricentro del peso (" + FN(xRot - xMin, 2) + " m dal riflettore; vedi Bilanciamento)");
                Border f4 = AvvisoGiallo("Predimensionamento: non sostituisce la verifica di un tecnico per palo, traliccio e plinto. Il momento alla base passera' alla sottolinguetta Palo e plinto.");
                Border[] fc = { f1, f2, f3, f4 };
                for (int i = 0; i < 4; i++) { Grid.SetColumn(fc[i], 2 * i); fondo.Children.Add(fc[i]); }
                Registro.Scrivi("AZIONE", "vento: q " + FN(r.Q, 0) + " Pa, forza massima " + FN(r.FMax, 0) + " N");
            };
            meccAggiornaVento = () => { try { calcola(); } catch (Exception ex) { Status("Vento: " + ex.Message, true); } };
            tLoc.LostFocus += (s, e) => { zonaDaLoc(); calcola(); };
            bCalc.Click += (s, e) => meccAggiornaVento();
            cU.SelectionChanged += (s, e) => meccAggiornaVento();
            bPdf.Click += async (s, e) =>
            {
                if (ultimo == null) return;
                try
                {
                    SchedaPdf pdf = new SchedaPdf("Carico del vento (predimensionamento)", "YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture));
                    pdf.Sezione("Sito e norma");
                    pdf.Dato("Norma", "NTC 2018 + Circolare 7/2019; coefficienti CNR-DT 207 R1/2018 ed EN 1991-1-4");
                    pdf.Dato("Locatore / zona", tLoc.Text + " / zona " + Meccanica.Zone[Math.Max(0, cZona.SelectedIndex)].N);
                    pdf.Dato("vb", FN(ultimo.Vb, 1) + " m/s"); pdf.Dato("qr", FN(ultimo.Qr, 0) + " N/m²"); pdf.Dato("ce", FN(ultimo.Ce, 2)); pdf.Dato("q", FN(ultimo.Q, 0) + " N/m² (" + FN(ultimo.Q / 9.80665, 1) + " kgf/m²)");
                    pdf.Sezione("Forze per parte (vento lungo il boom)");
                    List<string[]> t = new List<string[]> { new[] { "Parte", "Area m2", "cf0", "psi", "cf", "F (N)", "F (kgf)" } };
                    foreach (var p in ultimo.Parti) { double[] f = MeccanicaVento.ForzaXY(p, ultimo.Q, 0); double fv = Math.Sqrt(f[0] * f[0] + f[1] * f[1]); t.Add(new[] { p.Nome, FN(p.Area, 2), FN(p.Cf0, 2), FN(p.Psi, 2), FN(p.Cf, 2), FN(fv, 0), FN(fv / 9.80665, 1) }); }
                    pdf.Tabella(t, new float[] { 150, 55, 45, 45, 45, 60, 60 });
                    pdf.Sezione("Risultati");
                    pdf.Dato("Forza massima", FN(ultimo.FMax, 0) + " N (" + FN(ultimo.FMax / 9.80665, 1) + " kgf), vento a " + FN(ultimo.DirMax, 0) + " gradi dal boom");
                    pdf.Dato("Momento flettente alla staffa del rotore", FN(ultimo.MFlettente, 0) + " N m (" + FN(ultimo.MFlettente / 9.80665, 1) + " kgf m)");
                    pdf.Dato("Momento torcente sul rotore", FN(ultimo.TMax, 0) + " N m (" + FN(ultimo.TMax / 9.80665, 1) + " kgf m)");
                    pdf.Testo("Predimensionamento: non sostituisce la verifica di un tecnico abilitato per palo, traliccio e plinto.");
                    IStorageFile fl = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Relazione del vento (PDF)", SuggestedFileName = "Vento.pdf", DefaultExtension = "pdf", FileTypeChoices = new[] { new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } } });
                    if (fl == null) return;
                    using (Stream st = await fl.OpenWriteAsync()) pdf.Scrivi(st);
                    Status("Relazione del vento: " + fl.Name);
                }
                catch (Exception ex) { Status("Relazione PDF: " + ex.Message, true); }
            };
            fN.Select("ntc", false);
            zonaDaLoc();
            return new ScrollViewer { Content = g };
        }

        // ------------------------------------------------------------------------------------------------
        // Bilanciamento (bozzetto VHF 12)
        // ------------------------------------------------------------------------------------------------
        Control BuildBilanciamentoPage()
        {
            var riga = MeccRiga();
            int modo = 0;
            StackPanel sx = new StackPanel { Width = 262, Spacing = 2 };
            Fascione fM = new Fascione(); fM.Famiglia("DA BILANCIARE");
            fM.Voce("0", "Peso e vento insieme"); fM.Voce("1", "Solo peso"); fM.Voce("2", "Solo vento (momento torcente)");
            sx.Children.Add(fM);
            sx.Children.Add(MeccFam("MASSE (DAL MODELLO)"));
            ComboBox cMat = new ComboBox { Width = 130, ItemsSource = new[] { "Al tondino pieno", "Al tubo (spessore tipico)" }, SelectedIndex = 0 };
            ComboBox cBoom = new ComboBox { Width = 130, ItemsSource = MeccBoomTipi, SelectedIndex = 1 };
            TextBox tSt = Tb("35", 60), tDip = Tb("0,40", 60), tCavo = Tb("0,15", 60);
            sx.Children.Add(riga("Materiale elementi", cMat, "")); sx.Children.Add(riga("Boom", cBoom, "")); sx.Children.Add(riga("Staffe, isolatori", tSt, "g cad."));
            sx.Children.Add(riga("Dipolo + scatola", tDip, "kg")); sx.Children.Add(riga("Cavo fino al palo", tCavo, "kg/m"));
            sx.Children.Add(MeccFam("STAFFA AL PALO"));
            TextBox tPos = Tb("", 70);
            sx.Children.Add(riga("Posizione attuale", tPos, "m dal riflettore"));
            CheckBox cContr = new CheckBox { Content = "Contrappeso", Margin = new Thickness(2, 0) };
            TextBox tCm = Tb("0", 50), tCd = Tb("0", 50);
            StackPanel rc = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 }; rc.Children.Add(tCm); rc.Children.Add(new TextBlock { Text = "kg a", VerticalAlignment = VerticalAlignment.Center }); rc.Children.Add(tCd);
            sx.Children.Add(cContr); sx.Children.Add(riga("Massa / distanza", rc, "m"));
            CheckBox cVento = new CheckBox { Content = "Vento dalla linguetta Vento", IsChecked = true, Margin = new Thickness(2, 0) };
            ToolTip.SetTip(cVento, "Usa la pressione di progetto dell'ultimo calcolo della sottolinguetta Vento; senza quel calcolo usa 600 N/m².");
            sx.Children.Add(cVento);
            sx.Children.Add(MeccFam("UNITÀ"));
            ComboBox cU = new ComboBox { Width = 140, ItemsSource = new[] { "N e kgf affiancati", "solo N", "solo kgf" }, SelectedIndex = meccUnita };
            sx.Children.Add(riga("Mostra", cU, ""));
            Button bCalc = Btn("Calcola", true), bSposta = Btn("Sposta staffa nel modello", false);
            ToolTip.SetTip(bSposta, "Sposta tutto il modello lungo il boom in modo che la staffa consigliata cada nell'origine (dove il palo e il telaio dello stack si attaccano). Si annulla con Annulla della Struttura 3D.");
            StackPanel rb = new StackPanel { Spacing = 6, Margin = new Thickness(2, 12, 0, 0) }; rb.Children.Add(bCalc); rb.Children.Add(bSposta);
            sx.Children.Add(rb);

            Disegno dVista = new Disegno { Height = 300 };
            LineChart gM = new LineChart { Height = 330, XLabel = "posizione della staffa dal riflettore (m)", YLabel = "N·m", EmptyText = "premi Calcola" };
            Grid tab = new Grid();
            TextBlock notaTab = new TextBlock { FontSize = 12, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            StackPanel cT = new StackPanel(); cT.Children.Add(tab); cT.Children.Add(notaTab);
            Grid due = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*"), Margin = new Thickness(0, 10, 0, 10) };
            Border b1 = Pannello("Momenti in funzione della posizione della staffa", gM), b2 = Pannello("Masse e aree per parte", cT);
            Grid.SetColumn(b2, 2); due.Children.Add(b1); due.Children.Add(b2);
            Grid fondo = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*,10,*,10,*") };
            StackPanel dx = new StackPanel();
            dx.Children.Add(Pannello("Vista laterale: baricentro, centro di spinta e staffa", dVista)); dx.Children.Add(due); dx.Children.Add(fondo);
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,12,*"), Margin = new Thickness(4) };
            g.Children.Add(new ScrollViewer { Content = sx }); Grid.SetColumn(dx, 2); g.Children.Add(dx);

            List<MeccanicaVento.Massa> masse = null; MeccanicaVento.EsitoBil es = null; List<Tolleranze.Elemento> el = null;
            double xAtt = double.NaN, lBoom = 0, x0Boom = 0;
            dVista.Disegna = (ctx, rr) =>
            {
                if (es == null || el == null || el.Count == 0) { Draw.TextC(ctx, run == null ? "Calcola prima il modello" : "premi Calcola", rr.Width / 2, rr.Height / 2, 13, Pal.Muted); return; }
                double L = 40, R = rr.Width - 40, y = rr.Height * 0.45;
                double a = x0Boom, b = x0Boom + lBoom;
                Func<double, double> X = x => L + (R - L) * (x - a) / (b - a);
                for (int m = (int)Math.Ceiling(a); m <= b; m++) { ctx.DrawLine(new Pen(Pal.B(Pal.Muted), 1), new Point(X(m), 26), new Point(X(m), 34)); Draw.TextC(ctx, m + " m", X(m), 16, 11, Pal.Muted); }
                ctx.FillRectangle(new SolidColorBrush(Color.Parse("#2B3A4A")), new Rect(X(a), y - 3, X(b) - X(a), 6));
                foreach (var e in el) ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#1D5FB0")), 3), new Point(X(e.Pos), y - 9), new Point(X(e.Pos), y + 9));
                var dr = el.FirstOrDefault(e => e.Alimentato); if (dr != null) ctx.FillRectangle(new SolidColorBrush(Color.Parse("#F2C94C")), new Rect(X(dr.Pos) - 8, y - 14, 16, 28));
                Draw.Text(ctx, "riflettore", X(el[0].Pos) - 4, y - 32, 11.5, Pal.Muted); Draw.TextR(ctx, "ultimo direttore", X(el[el.Count - 1].Pos) + 4, y - 32, 11.5, Pal.Muted);
                if (!double.IsNaN(xAtt)) { ctx.FillRectangle(new SolidColorBrush(Color.Parse("#8B3A2E")), new Rect(X(xAtt) - 6, y + 4, 12, rr.Height - y - 20)); Draw.Text(ctx, "staffa attuale " + FN(xAtt, 2) + " m", X(xAtt) - 150, rr.Height - 24, 12, Color.Parse("#8B3A2E")); }
                Pen pv = new Pen(new SolidColorBrush(Color.Parse("#D0342C")), 1.6, new DashStyle(new double[] { 3, 2 }, 0));
                ctx.DrawLine(pv, new Point(X(es.Xa), 52), new Point(X(es.Xa), y)); Draw.Text(ctx, "centro di spinta del vento " + FN(es.Xa, 2) + " m", X(es.Xa) + 8, 46, 12, Color.Parse("#D0342C"));
                ctx.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#1D5FB0")), 2.2), new Point(X(es.Xg), y), new Point(X(es.Xg), y + 60));
                Draw.Text(ctx, "baricentro " + FN(es.Xg, 2) + " m · " + UF(es.Kg * Meccanica.G, 1), X(es.Xg) + 8, y + 50, 12, Color.Parse("#1D5FB0"));
                ctx.DrawEllipse(null, new Pen(new SolidColorBrush(Color.Parse("#21A04A")), 2), new Point(X(es.XCons), y), 7, 7);
                Draw.Text(ctx, "consigliata " + FN(es.XCons, 2) + " m", X(es.XCons) + 8, y + 16, 12, Color.Parse("#21A04A"));
            };
            Action calcola = () =>
            {
                meccUnita = Math.Max(0, cU.SelectedIndex);
                el = MeccElementi();
                if (el.Count == 0) { Status("Bilanciamento: calcola prima il modello (servono gli elementi)", true); return; }
                if (stackInfo != null && stackInfo.Copies > 1) { int n1 = el.Count / stackInfo.Copies; el = el.GroupBy(e => Math.Round(e.Pos, 3)).Select(gq => gq.First()).OrderBy(e => e.Pos).ToList(); }
                double rho = Meccanica.DensAl;
                double mEl = 0, xEl = 0, aEl = 0, xaEl = 0;
                foreach (var e in el)
                {
                    double d = e.Diam, s = cMat.SelectedIndex == 0 ? d / 2 : Meccanica.SpessoreTipico(d), di = Math.Max(0, d - 2 * s);
                    double m = rho * Math.PI * (d * d - di * di) / 4 * e.Lung;
                    mEl += m; xEl += m * e.Pos; aEl += d * e.Lung; xaEl += d * e.Lung * e.Pos;
                }
                double lato, spb; bool quadro; BoomDati(cBoom.SelectedIndex, out lato, out spb, out quadro);
                x0Boom = el[0].Pos - 0.05; lBoom = el[el.Count - 1].Pos - el[0].Pos + 0.10;
                double aSez = quadro ? 4 * lato * spb - 4 * spb * spb : Math.PI * (lato * lato - Math.Pow(lato - 2 * spb, 2)) / 4;
                double mBoom = rho * aSez * lBoom, xBoom = x0Boom + lBoom / 2;
                double mSt = ParseNum(tSt.Text, 35) / 1000 * el.Count;
                var dr = el.FirstOrDefault(e => e.Alimentato) ?? el[Math.Min(1, el.Count - 1)];
                if (double.IsNaN(xAtt) || string.IsNullOrWhiteSpace(tPos.Text)) { xAtt = Math.Round((x0Boom + lBoom / 2) * 100) / 100; tPos.Text = FN(xAtt, 2); }
                xAtt = ParseNum(tPos.Text, xAtt);
                double lCavo = Math.Abs(xAtt - dr.Pos), mCavo = ParseNum(tCavo.Text, 0.15) * lCavo;
                masse = new List<MeccanicaVento.Massa>
                {
                    new MeccanicaVento.Massa { Nome = "Elementi (" + el.Count + ")", Kg = mEl, X = xEl / mEl, Area = aEl, XArea = xaEl / aEl, ContaVento = false },
                    new MeccanicaVento.Massa { Nome = "Boom " + FN(lBoom, 1) + " m", Kg = mBoom, X = xBoom, Area = lato * lBoom, XArea = xBoom },
                    new MeccanicaVento.Massa { Nome = "Staffe e isolatori", Kg = mSt, X = el.Average(e => e.Pos), Area = 0.0006 * el.Count, XArea = el.Average(e => e.Pos) },
                    new MeccanicaVento.Massa { Nome = "Dipolo + scatola", Kg = ParseNum(tDip.Text, 0.4), X = dr.Pos, Area = 0.012, XArea = dr.Pos },
                    new MeccanicaVento.Massa { Nome = "Cavo sul boom", Kg = mCavo, X = (dr.Pos + xAtt) / 2, Area = 0.0075 * lCavo, XArea = (dr.Pos + xAtt) / 2 }
                };
                if (cContr.IsChecked == true && ParseNum(tCm.Text, 0) > 0) masse.Add(new MeccanicaVento.Massa { Nome = "Contrappeso", Kg = ParseNum(tCm.Text, 0), X = ParseNum(tCd.Text, 0), Area = 0, XArea = ParseNum(tCd.Text, 0) });
                double q = cVento.IsChecked == true && !double.IsNaN(meccQ) ? meccQ : 600, cf = quadro ? 2.1 : 1.2;
                es = MeccanicaVento.Bilancia(masse, q, cf, xAtt, modo);
                // tabella
                tab.Children.Clear(); tab.RowDefinitions.Clear();
                tab.ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto,Auto");
                int rr = 0;
                Action<string[], bool> rg = (v, h) =>
                {
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int j = 0; j < v.Length; j++)
                    {
                        Border b = new Border { Child = new TextBlock { Text = v[j], FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal, HorizontalAlignment = j == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right }, Padding = new Thickness(8, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                        Grid.SetColumn(b, j); Grid.SetRow(b, rr); tab.Children.Add(b);
                    }
                    rr++;
                };
                rg(new[] { "Parte", "Massa kg", "Baricentro m", "Area vento m²", "Centro area m" }, true);
                foreach (var m in masse) rg(new[] { m.Nome, FN(m.Kg, 2), FN(m.X, 2), FN(m.Area, 3) + (m.ContaVento ? "" : "*"), FN(m.XArea, 2) }, false);
                rg(new[] { "Totale", FN(es.Kg, 2) + " · " + FN(es.Kg * Meccanica.G, 1) + " N", FN(es.Xg, 2), FN(es.Area, 3), FN(es.Xa, 2) }, true);
                notaTab.Text = "Peso: " + FN(es.Kg, 2) + " kgf. Centro area = punto dove il vento laterale non fa ruotare l'antenna. * Gli elementi non contano per il torcente: con il vento di traverso sono paralleli al vento (flusso trasversale, K5IU). Pressione del vento " + FN(q, 0) + " N/m²" + (cVento.IsChecked == true && double.IsNaN(meccQ) ? " (predefinita: calcola il Vento per usare quella del sito)" : "") + ".";
                // grafico dei momenti
                int n = 41; double[] xs = new double[n], mp = new double[n], mv = new double[n];
                for (int i = 0; i < n; i++)
                {
                    xs[i] = x0Boom + lBoom * i / (n - 1);
                    mp[i] = es.Kg * Meccanica.G * (es.Xg - xs[i]);
                    mv[i] = q * cf * es.Area * (es.Xa - xs[i]);
                }
                gM.X = xs; gM.MarkX = xAtt; gM.MarkXLabel = "attuale"; gM.MarkXColor = Color.Parse("#8B3A2E");
                gM.HLines = new double[] { 0 };
                gM.Series = new List<LineSeries>
                {
                    new LineSeries { Name = "momento del peso sul morsetto (N·m)", Y = mp, Color = Color.Parse("#1D5FB0"), Width = 2.4, Decimals = 1 },
                    new LineSeries { Name = "momento torcente del vento sul rotore (N·m)", Y = mv, Color = Color.Parse("#D0342C"), Width = 2.2, Decimals = 1 }
                };
                gM.InvalidateVisual(); dVista.InvalidateVisual();
                fondo.Children.Clear();
                string a1, a2;
                MeccSeparaUnita(UM(Math.Abs(es.TAttuale)), out a1, out a2);
                Border[] fc =
                {
                    MeccCarta(FN(es.Xg, 2) + " m", "", "bilanciamento del peso (baricentro)"),
                    MeccCarta(FN(es.Xa, 2) + " m", "", "bilanciamento del vento (centro area)"),
                    MeccCarta(FN(es.XCons, 2) + " m", "", "posizione consigliata della staffa (" + (modo == 0 ? "compromesso" : modo == 1 ? "solo peso" : "solo vento") + ")"),
                    MeccCarta(a1, a2, "torcente sul rotore con la staffa attuale (" + FN(xAtt, 2) + " m) e vento di progetto → " + UM(Math.Abs(es.TCons)) + " spostandola")
                };
                for (int i = 0; i < 4; i++) { Grid.SetColumn(fc[i], 2 * i); fondo.Children.Add(fc[i]); }
                Registro.Scrivi("AZIONE", "bilanciamento: baricentro " + FN(es.Xg, 2) + " m, centro area " + FN(es.Xa, 2) + " m");
            };
            meccAggiornaBil = () => { try { calcola(); } catch (Exception ex) { Status("Bilanciamento: " + ex.Message, true); } };
            fM.Selected += k => { modo = int.Parse(k); meccAggiornaBil(); };
            bCalc.Click += (s, e) => meccAggiornaBil();
            cU.SelectionChanged += (s, e) => meccAggiornaBil();
            bSposta.Click += (s, e) =>
            {
                if (es == null || run == null) return;
                int asse; Tolleranze.Elementi(model, run.Geometry, out asse);
                // posizione dal riflettore -> coordinata del modello: il primo elemento e' a Pos 0
                List<Tolleranze.Elemento> e0 = MeccElementi();
                double[] c = TrasformaGeometria.Centro(run.Geometry);
                double coordRif = double.NaN;
                SegmentGeometry sg = run.Geometry;
                for (int i = 0; i < sg.N; i++) if (sg.Tag[i] == e0[0].Tag) { coordRif = asse == 0 ? sg.CenterX(i) : asse == 1 ? sg.CenterY(i) : sg.CenterZ(i); break; }
                if (double.IsNaN(coordRif)) return;
                // il verso del boom: se l'ultimo elemento ha coordinata minore, le posizioni crescono al contrario
                double coordUlt = double.NaN;
                for (int i = 0; i < sg.N; i++) if (sg.Tag[i] == e0[e0.Count - 1].Tag) { coordUlt = asse == 0 ? sg.CenterX(i) : asse == 1 ? sg.CenterY(i) : sg.CenterZ(i); break; }
                double verso = coordUlt >= coordRif ? 1 : -1;
                double target = coordRif + verso * es.XCons;
                double[] d = { 0, 0, 0 }; d[asse] = -target;
                NecModel m2 = TrasformaGeometria.Applica(model, TrasformaGeometria.Sposta(d[0], d[1], d[2]), null);
                GeoMetti(m2, true);
                Status("Modello spostato: la staffa consigliata (" + FN(es.XCons, 2) + " m dal riflettore) e' ora nell'origine.");
            };
            fM.Select("0", false);
            return new ScrollViewer { Content = g };
        }
    }
}
