// YA-NEC - barra Geometria nella linguetta Struttura 3D (bozzetto approvato doc/bozzetto-geometria.png, "approvato procedi"
// di IC8TEM del 13/11/2026): Sposta, Ruota, Scala, Specchia, Copia; rapidi -90 / +90 gradi Z, orizzontale <-> verticale,
// appoggia a terra, centra; Annulla e Ripeti. Il pannello della scheda prende il posto delle tabelle a destra della vista 3D,
// con l'anteprima tratteggiata arancione. Motore: Engine/TrasformaGeometria.cs.
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        ContentControl struttDestraHost;
        Control struttTabelle;
        string geoScheda;
        readonly Dictionary<string, Button> geoBottoni = new Dictionary<string, Button>();
        readonly HashSet<int> geoSel = new HashSet<int>();
        readonly List<NecModel> geoAnnulla = new List<NecModel>(), geoRipeti = new List<NecModel>();
        Button geoBtnAnnulla, geoBtnRipeti;

        // stato del pannello aperto
        bool geoSoloScelti, geoAnteprima = true, geoComeVar, geoOrario, geoRaggi, geoTieni = true;
        int geoAsse = 2, geoCentro = 1, geoPiano = 1, geoEstremo;
        TextBox geoA, geoB, geoC, geoAng, geoPx, geoPy, geoPz, geoFilo, geoN;
        TextBlock geoCambia, geoNotaSel;

        Control CreaBarraGeometria()
        {
            WrapPanel bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            bar.Children.Add(new TextBlock { Text = "Geometria", FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 6, 0) });
            geoBottoni.Clear();
            foreach (string s in new[] { "Sposta", "Ruota", "Scala", "Specchia", "Copia", "Loop", "Elica" })
            {
                string k = s;
                Button b = Btn(s, false);
                b.Click += (o, e) => GeoApri(geoScheda == k ? null : k);
                geoBottoni[k] = b;
                bar.Children.Add(b);
            }
            bar.Children.Add(new Border { Width = 1, Height = 22, Background = Pal.B(Pal.Border), Margin = new Thickness(6, 0) });
            bar.Children.Add(Lbl("rapidi"));
            Button m90 = Btn("↺ -90° Z", false), p90 = Btn("↻ +90° Z", false), ov = Btn("orizzontale ↔ verticale", false), at = Btn("appoggia a terra", false), ce = Btn("centra", false);
            ToolTip.SetTip(m90, "Ruota tutta l'antenna di 90 gradi in senso orario attorno all'asse Z passante per il suo centro");
            ToolTip.SetTip(p90, "Ruota tutta l'antenna di 90 gradi in senso antiorario attorno all'asse Z passante per il suo centro");
            ToolTip.SetTip(ov, "Ruota di 90 gradi attorno al boom (o all'asse orizzontale perpendicolare al filo piu' lungo): cambia la polarizzazione");
            ToolTip.SetTip(at, "Sposta in verticale l'antenna finche' il punto piu' basso tocca z = 0");
            ToolTip.SetTip(ce, "Porta il centro dell'antenna su X = 0, Y = 0 (la quota non cambia)");
            m90.Click += (s, e) => GeoRapido(g => TrasformaGeometria.Ruota(2, -90, TrasformaGeometria.Centro(g)), "-90 gradi Z");
            p90.Click += (s, e) => GeoRapido(g => TrasformaGeometria.Ruota(2, 90, TrasformaGeometria.Centro(g)), "+90 gradi Z");
            ov.Click += (s, e) => GeoRapido(g => TrasformaGeometria.Ruota(TrasformaGeometria.AsseOrizzVert(model, g), 90, TrasformaGeometria.Centro(g)), "orizzontale <-> verticale");
            at.Click += (s, e) => GeoRapido(g => TrasformaGeometria.Sposta(0, 0, -TrasformaGeometria.ZMin(g)), "appoggia a terra");
            ce.Click += (s, e) => GeoRapido(g => { double[] c = TrasformaGeometria.Centro(g); return TrasformaGeometria.Sposta(-c[0], -c[1], 0); }, "centra");
            bar.Children.Add(m90); bar.Children.Add(p90); bar.Children.Add(ov); bar.Children.Add(at); bar.Children.Add(ce);
            bar.Children.Add(new Border { Width = 1, Height = 22, Background = Pal.B(Pal.Border), Margin = new Thickness(6, 0) });
            geoBtnAnnulla = Btn("↶ Annulla", false);
            geoBtnRipeti = Btn("↷ Ripeti", false);
            geoBtnAnnulla.Click += (s, e) => GeoAnnulla(true);
            geoBtnRipeti.Click += (s, e) => GeoAnnulla(false);
            bar.Children.Add(geoBtnAnnulla); bar.Children.Add(geoBtnRipeti);
            GeoAggiornaAnnulla();
            return bar;
        }

        void GeoAggiornaAnnulla()
        {
            if (geoBtnAnnulla != null) geoBtnAnnulla.IsEnabled = geoAnnulla.Count > 0;
            if (geoBtnRipeti != null) geoBtnRipeti.IsEnabled = geoRipeti.Count > 0;
        }

        void GeoSvuotaStoria() { geoAnnulla.Clear(); geoRipeti.Clear(); GeoAggiornaAnnulla(); }

        void GeoAnnulla(bool indietro)
        {
            List<NecModel> da = indietro ? geoAnnulla : geoRipeti, a = indietro ? geoRipeti : geoAnnulla;
            if (da.Count == 0 || model == null) return;
            a.Add(model);
            NecModel m = da[da.Count - 1];
            da.RemoveAt(da.Count - 1);
            Registro.Scrivi("AZIONE", "geometria: " + (indietro ? "annulla" : "ripeti"));
            GeoMetti(m, false);
        }

        // nuovo modello dalla barra: storia, tabelle, variabili e ricalcolo
        void GeoMetti(NecModel m, bool storia)
        {
            if (storia)
            {
                geoAnnulla.Add(model);
                if (geoAnnulla.Count > 30) geoAnnulla.RemoveAt(0);
                geoRipeti.Clear();
            }
            model = m;
            mmWires = null;
            lastError = null;
            if (txtNec != null) txtNec.Text = model.ToNec();
            RefreshWiresTable();
            RefreshVarsPanel();
            GeoAggiornaAnnulla();
            if (geoScheda != null) GeoAnteprima();
            Calculate(true);
        }

        void GeoRapido(Func<SegmentGeometry, Affine> fa, string nome)
        {
            if (model == null) { Status("Apri prima un modello.", true); return; }
            try
            {
                SegmentGeometry g = model.BuildGeometry();
                Affine a = fa(g);
                NecModel m = TrasformaGeometria.Applica(model, a, null);
                Registro.Scrivi("AZIONE", "geometria, rapido: " + nome + " (" + a.Descrizione + ")");
                GeoMetti(m, true);
                Status("Geometria: " + nome + ". \"Annulla\" torna indietro.");
            }
            catch (Exception ex) { Status("Geometria: " + ex.Message, true); }
        }

        // ------------------------------------------------------------------------------------------------
        // pannello della scheda
        // ------------------------------------------------------------------------------------------------
        void GeoApri(string scheda)
        {
            geoScheda = scheda;
            foreach (KeyValuePair<string, Button> kv in geoBottoni) StileScelta(kv.Value, kv.Key == scheda);
            if (struttDestraHost == null) return;
            if (scheda == null)
            {
                struttDestraHost.Content = struttTabelle;
                if (view3d != null) { view3d.SelectedTags = new HashSet<int>(); view3d.Preview = null; view3d.PreviewCenter = null; view3d.PreviewCenterText = ""; view3d.PreviewLabel = ""; view3d.InvalidateVisual(); }
                return;
            }
            // la selezione parte dai fili scelti nella tabella
            if (geoSel.Count == 0 && activeRow != null)
            {
                for (int i = 0; i < wireRows.Count; i++)
                {
                    bool dentro = wireRows[i] == activeRow || (selFrom >= 0 && i >= Math.Min(selFrom, selTo) && i <= Math.Max(selFrom, selTo));
                    if (dentro && wireRows[i].Card != null && wireRows[i].Card.Id == "GW") geoSel.Add(wireRows[i].Card.I1);
                }
                if (geoSel.Count > 0) geoSoloScelti = true;
            }
            Registro.Scrivi("AZIONE", "geometria: scheda " + scheda);
            if (scheda == "Loop") { struttDestraHost.Content = LoopPannello(); return; }   // bozzetto approvato doc/bozzetto-loop.png
            if (scheda == "Elica") { struttDestraHost.Content = ElicaPannello(); return; }   // 0.4.30: bozzetto 1 approvato il 14/11/2026 22:40
            struttDestraHost.Content = GeoPannello(scheda);
            GeoAnteprima();
        }

        void GeoFiloCliccato(int tag, bool ctrl)
        {
            if (geoScheda == null) return;
            if (!ctrl) { bool solo = geoSel.Count == 1 && geoSel.Contains(tag); geoSel.Clear(); if (!solo) geoSel.Add(tag); }
            else if (!geoSel.Remove(tag)) geoSel.Add(tag);
            if (geoSel.Count > 0 && !geoSoloScelti) { geoSoloScelti = true; struttDestraHost.Content = GeoPannello(geoScheda); }
            GeoAnteprima();
        }

        TextBox GeoTb(string t, double w) { TextBox b = new TextBox { Text = t, Width = w, Margin = new Thickness(2), FontFamily = Pal.Mono }; b.TextChanged += (s, e) => GeoAnteprima(); return b; }

        Control GeoRiga(string etichetta, Control c)
        {
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("185,*"), Margin = new Thickness(0, 3) };
            TextBlock l = new TextBlock { Text = etichetta, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, FontSize = 14 };
            Grid.SetColumn(c, 1);
            g.Children.Add(l); g.Children.Add(c);
            return g;
        }

        Control GeoPannello(string scheda)
        {
            StackPanel p = new StackPanel { Spacing = 4 };
            p.Children.Add(new TextBlock { Text = scheda, FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 4) });
            p.Children.Add(GeoRiga("Applica a", Scelta(new[] { "tutta l'antenna", "fili selezionati" }, geoSoloScelti ? 1 : 0, k => { geoSoloScelti = k == 1; GeoAnteprima(); })));
            geoNotaSel = Nota("selezione: clic sui fili nella vista 3D o nella tabella dei fili (Ctrl per piu' fili)");
            geoNotaSel.Margin = new Thickness(185, 0, 0, 4);
            p.Children.Add(geoNotaSel);

            switch (scheda)
            {
                case "Sposta":
                    {
                        StackPanel r = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                        geoA = GeoTb("0", 90); geoB = GeoTb("0", 90); geoC = GeoTb("0", 90);
                        r.Children.Add(Lbl("X")); r.Children.Add(geoA); r.Children.Add(Lbl("Y")); r.Children.Add(geoB); r.Children.Add(Lbl("Z")); r.Children.Add(geoC);
                        p.Children.Add(GeoRiga("Spostamento (m)", r));
                        p.Children.Add(Nota("Anche con le unita': 10cm, 6in, 2ft."));
                        break;
                    }
                case "Ruota":
                    {
                        p.Children.Add(GeoRiga("Asse", Scelta(new[] { "X", "Y", "Z" }, geoAsse, k => { geoAsse = k; GeoAnteprima(); })));
                        StackPanel r = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                        geoAng = GeoTb("90", 110);
                        r.Children.Add(geoAng); r.Children.Add(Lbl("gradi"));
                        r.Children.Add(Scelta(new[] { "antiorario", "orario" }, geoOrario ? 1 : 0, k => { geoOrario = k == 1; GeoAnteprima(); }));
                        p.Children.Add(GeoRiga("Angolo", r));
                        GeoCentro(p, "Centro di rotazione");
                        break;
                    }
                case "Scala":
                    {
                        geoA = GeoTb("1", 110);
                        StackPanel r = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                        r.Children.Add(geoA);
                        r.Children.Add(Nota("es. 1,02 oppure 144,3/145 (frequenza vecchia / nuova)"));
                        p.Children.Add(GeoRiga("Fattore", r));
                        CheckBox rg = new CheckBox { Content = "Scala anche i raggi dei fili", IsChecked = geoRaggi };
                        rg.IsCheckedChanged += (s, e) => { geoRaggi = rg.IsChecked == true; GeoAnteprima(); };
                        p.Children.Add(rg);
                        GeoCentro(p, "Centro della scala");
                        break;
                    }
                case "Specchia":
                    {
                        p.Children.Add(GeoRiga("Piano", Scelta(new[] { "YZ (cambia X)", "XZ (cambia Y)", "XY (cambia Z)" }, geoPiano, k => { geoPiano = k; GeoAnteprima(); })));
                        GeoCentro(p, "Il piano passa per");
                        CheckBox tn = new CheckBox { Content = "Tieni anche l'originale (copia speculare, come GX ma scritta in fili GW)", IsChecked = geoTieni };
                        tn.IsCheckedChanged += (s, e) => { geoTieni = tn.IsChecked == true; GeoAnteprima(); };
                        p.Children.Add(tn);
                        break;
                    }
                case "Copia":
                    {
                        geoN = GeoTb("1", 70);
                        p.Children.Add(GeoRiga("Numero di copie", geoN));
                        StackPanel r = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                        geoA = GeoTb("0", 80); geoB = GeoTb("0", 80); geoC = GeoTb("0", 80);
                        r.Children.Add(Lbl("X")); r.Children.Add(geoA); r.Children.Add(Lbl("Y")); r.Children.Add(geoB); r.Children.Add(Lbl("Z")); r.Children.Add(geoC);
                        p.Children.Add(GeoRiga("Spostamento a ogni copia (m)", r));
                        StackPanel r2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                        geoAng = GeoTb("0", 90);
                        r2.Children.Add(geoAng); r2.Children.Add(Lbl("gradi attorno all'asse"));
                        r2.Children.Add(Scelta(new[] { "X", "Y", "Z" }, geoAsse, k => { geoAsse = k; GeoAnteprima(); }));
                        p.Children.Add(GeoRiga("Rotazione a ogni copia", r2));
                        GeoCentro(p, "Centro della rotazione");
                        p.Children.Add(Nota("Le copie hanno tag nuovi dopo l'ultimo; le sorgenti restano solo sui fili di partenza (stack: zona Oggetti del modello della Struttura 3D)."));
                        break;
                    }
            }

            CheckBox an = new CheckBox { Content = "Anteprima nella vista 3D", IsChecked = geoAnteprima, Margin = new Thickness(0, 6, 0, 0) };
            an.IsCheckedChanged += (s, e) => { geoAnteprima = an.IsChecked == true; GeoAnteprima(); };
            p.Children.Add(an);
            if (scheda == "Ruota")
            {
                StackPanel v = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                CheckBox cv = new CheckBox { Content = "Angolo come variabile", IsChecked = geoComeVar };
                cv.IsCheckedChanged += (s, e) => { geoComeVar = cv.IsChecked == true; GeoAnteprima(); };
                v.Children.Add(cv);
                v.Children.Add(new Border { Background = Pal.B(Pal.AccentSoft), CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 1), VerticalAlignment = VerticalAlignment.Center, Child = new TextBlock { Text = GeoNomeVar(), Foreground = Pal.B(Pal.Accent), FontSize = 12.5 } });
                v.Children.Add(new TextBlock { Text = "(per sweep e ottimizzatore)", Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center });
                p.Children.Add(v);
            }
            StackPanel cb = new StackPanel { Spacing = 2 };
            cb.Children.Add(new TextBlock { Text = scheda == "Ruota" ? "Cosa cambia nella tabella dei fili (con \"Angolo come variabile\" le coordinate diventano formule con " + GeoNomeVar() + "):" : "Cosa cambia nella tabella dei fili:", TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), FontSize = 13 });
            geoCambia = new TextBlock { FontFamily = Pal.Mono, FontSize = 13, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            cb.Children.Add(geoCambia);
            p.Children.Add(GeoBox(cb));
            p.Children.Add(GeoBox(new TextBlock
            {
                Text = scheda == "Copia"
                    ? "Le copie sono fili GW nuovi, senza sorgenti: per uno stack alimentato usa la zona Oggetti del modello (Struttura 3D, in basso a destra), Stack."
                    : "Sorgenti, carichi, linee e rastremazioni restano agganciati a filo e percentuale: seguono l'antenna senza modifiche." +
                      (scheda == "Ruota" ? " Il diagramma ruota di conseguenza: con 90 gradi attorno a Z il massimo passa da 0 a 90 gradi di azimut." : ""),
                TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), FontSize = 13
            }));
            StackPanel bt = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
            Button chiudi = Btn("Chiudi", false);
            chiudi.Click += (s, e) => GeoApri(null);
            Button ap = Btn(new Dictionary<string, string> { { "Sposta", "Applica spostamento" }, { "Ruota", "Applica rotazione" }, { "Scala", "Applica scala" }, { "Specchia", "Applica specchio" }, { "Copia", "Applica copia" } }[scheda], true);
            ap.Click += (s, e) => GeoApplica();
            bt.Children.Add(chiudi); bt.Children.Add(ap);
            p.Children.Add(bt);
            return new ScrollViewer
            {
                Content = new Border
                {
                    Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12), Child = p
                },
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };
        }

        Border GeoBox(Control c)
        {
            return new Border { Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 8), Margin = new Thickness(0, 6, 0, 0), Child = c };
        }

        void GeoCentro(StackPanel p, string titolo)
        {
            p.Children.Add(new TextBlock { Text = titolo, Foreground = Pal.B(Pal.Muted), FontSize = 14, Margin = new Thickness(0, 6, 0, 2) });
            string gruppo = "geoc" + Guid.NewGuid().ToString("N");
            double[] c = GeoCentroAntenna();
            RadioButton r0 = new RadioButton { GroupName = gruppo, Content = "origine (0, 0, 0)", IsChecked = geoCentro == 0 };
            RadioButton r1 = new RadioButton { GroupName = gruppo, IsChecked = geoCentro == 1 };
            StackPanel c1 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            c1.Children.Add(new TextBlock { Text = "centro dell'antenna", VerticalAlignment = VerticalAlignment.Center });
            c1.Children.Add(new TextBlock { Text = string.Format(CultureInfo.CurrentCulture, "({0:0.##}; {1:0.##}; {2:0.##})", c[0], c[1], c[2]), FontFamily = Pal.Mono, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center });
            r1.Content = c1;
            RadioButton r2 = new RadioButton { GroupName = gruppo, IsChecked = geoCentro == 2 };
            StackPanel c2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            geoPx = GeoTb("0", 80); geoPy = GeoTb("0", 80); geoPz = GeoTb("0", 80);
            c2.Children.Add(new TextBlock { Text = "punto: X", VerticalAlignment = VerticalAlignment.Center }); c2.Children.Add(geoPx);
            c2.Children.Add(new TextBlock { Text = "Y", VerticalAlignment = VerticalAlignment.Center }); c2.Children.Add(geoPy);
            c2.Children.Add(new TextBlock { Text = "Z", VerticalAlignment = VerticalAlignment.Center }); c2.Children.Add(geoPz);
            r2.Content = c2;
            RadioButton r3 = new RadioButton { GroupName = gruppo, IsChecked = geoCentro == 3 };
            StackPanel c3 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            geoFilo = GeoTb("1", 70);
            c3.Children.Add(new TextBlock { Text = "estremo di un filo: filo", VerticalAlignment = VerticalAlignment.Center }); c3.Children.Add(geoFilo);
            c3.Children.Add(Scelta(new[] { "E1", "E2" }, geoEstremo, k => { geoEstremo = k; GeoAnteprima(); }));
            r3.Content = c3;
            RadioButton[] rr = { r0, r1, r2, r3 };
            for (int i = 0; i < rr.Length; i++)
            {
                int k = i;
                rr[i].IsCheckedChanged += (s, e) => { if (rr[k].IsChecked == true) { geoCentro = k; GeoAnteprima(); } };
                rr[i].Margin = new Thickness(16, 1, 0, 1);
                p.Children.Add(rr[i]);
            }
        }

        double[] GeoCentroAntenna()
        {
            try { return model == null ? new double[3] : TrasformaGeometria.Centro(model.BuildGeometry()); }
            catch (Exception) { return new double[3]; }
        }

        string GeoNomeVar()
        {
            if (model == null) return "ANG";
            string n = "ANG"; int k = 2;
            while (model.Variables.Exists(v => string.Equals(v.Name, n, StringComparison.OrdinalIgnoreCase))) n = "ANG" + k++;
            return n;
        }

        static double GeoNum(TextBox t, double def)
        {
            string s = Expr.NormalizeDecimal((t?.Text ?? "").Trim().Replace(" ", ""));
            if (s.Length == 0) return def;
            return Expr.Eval(s, null);
        }

        double[] GeoCentroScelto()
        {
            switch (geoCentro)
            {
                case 0: return new double[3];
                case 2: return new[] { GeoNum(geoPx, 0), GeoNum(geoPy, 0), GeoNum(geoPz, 0) };
                case 3:
                    {
                        List<GeomCard> fl = FiliGw();
                        int w = (int)GeoNum(geoFilo, 1);
                        if (w < 1 || w > fl.Count) throw new NecException("il filo " + w + " non esiste (fili GW: " + fl.Count + ")");
                        GeomCard g = fl[w - 1];
                        return geoEstremo == 0 ? new[] { g.F1, g.F2, g.F3 } : new[] { g.F4, g.F5, g.F6 };
                    }
                default: return GeoCentroAntenna();
            }
        }

        // modello trasformato secondo il pannello (null se c'e' un errore, scritto nel riquadro "Cosa cambia")
        NecModel GeoCalcola(out HashSet<int> toccati, out double[] centro, out string etichetta)
        {
            toccati = null; centro = null; etichetta = "";
            if (model == null) throw new NecException("Apri prima un modello.");
            HashSet<int> tags = geoSoloScelti ? new HashSet<int>(geoSel) : null;
            if (tags != null && tags.Count == 0) throw new NecException("Nessun filo selezionato: clic su un filo nella vista 3D (Ctrl per piu' fili).");
            NecModel m0 = model;
            switch (geoScheda)
            {
                case "Sposta":
                    return TrasformaGeometria.Applica(m0, TrasformaGeometria.Sposta(GeoNum(geoA, 0), GeoNum(geoB, 0), GeoNum(geoC, 0)), tags);
                case "Ruota":
                    {
                        double ang = GeoNum(geoAng, 0) * (geoOrario ? -1 : 1);
                        centro = GeoCentroScelto();
                        etichetta = (ang >= 0 ? "+" : "") + ang.ToString("0.##", CultureInfo.CurrentCulture) + "°";
                        string var = null;
                        if (geoComeVar)
                        {
                            m0 = model.Clone();
                            var = GeoNomeVar();
                            m0.Variables.Add(new SyVar { Name = var, Expr = ang.ToString("0.######", CultureInfo.InvariantCulture), Descr = "angolo di rotazione attorno a " + "XYZ"[geoAsse] + " (gradi)" });
                            List<string> e = m0.Reevaluate();
                            if (e.Count > 0) throw new NecException(e[0]);
                        }
                        return TrasformaGeometria.Applica(m0, TrasformaGeometria.Ruota(geoAsse, ang, centro, var), tags);
                    }
                case "Scala":
                    {
                        double k = GeoNum(geoA, 1);
                        if (k <= 0) throw new NecException("il fattore deve essere maggiore di zero");
                        centro = GeoCentroScelto();
                        return TrasformaGeometria.Applica(m0, TrasformaGeometria.Scala(k, centro), tags, geoRaggi, k);
                    }
                case "Specchia":
                    {
                        centro = GeoCentroScelto();
                        Affine a = TrasformaGeometria.Specchia(geoPiano, centro);
                        if (geoTieni)
                        {
                            NecModel r = TrasformaGeometria.CopiaSpeculare(m0, a, tags);
                            toccati = GeoTagNuovi(m0, r);
                            return r;
                        }
                        return TrasformaGeometria.Applica(m0, a, tags);
                    }
                case "Copia":
                    {
                        int n = (int)Math.Round(GeoNum(geoN, 1));
                        if (n < 1 || n > 200) throw new NecException("numero di copie da 1 a 200");
                        double ang = GeoNum(geoAng, 0);
                        Affine a = Math.Abs(ang) > 1e-12 ? TrasformaGeometria.Ruota(geoAsse, ang, GeoCentroScelto()) : new Affine();
                        a.T = new[] { GeoNum(geoA, 0), GeoNum(geoB, 0), GeoNum(geoC, 0) };
                        NecModel r = TrasformaGeometria.Copia(m0, a, tags, n);
                        toccati = GeoTagNuovi(m0, r);
                        return r;
                    }
            }
            throw new NecException("operazione sconosciuta");
        }

        static HashSet<int> GeoTagNuovi(NecModel prima, NecModel dopo)
        {
            HashSet<int> v = new HashSet<int>(), n = new HashSet<int>();
            foreach (GeomCard g in prima.Geometry) v.Add(g.I1);
            foreach (GeomCard g in dopo.Geometry) if (g.Id == "GW" && !v.Contains(g.I1)) n.Add(g.I1);
            return n;
        }

        void GeoAnteprima()
        {
            if (view3d == null || geoScheda == null || geoScheda == "Loop" || geoScheda == "Elica") return;
            view3d.SelectedTags = new HashSet<int>(geoSel);
            if (geoNotaSel != null)
                geoNotaSel.Text = "selezione: clic sui fili nella vista 3D o nella tabella dei fili (Ctrl per piu' fili)" + (geoSel.Count > 0 ? " - scelti: tag " + string.Join(", ", geoSel) : "");
            try
            {
                HashSet<int> toccati; double[] centro; string et;
                NecModel m = GeoCalcola(out toccati, out centro, out et);
                if (toccati == null) toccati = geoSoloScelti ? new HashSet<int>(geoSel) : null;
                SegmentGeometry g = m.BuildGeometry();
                List<double[]> segs = new List<double[]>();
                for (int i = 0; i < g.N; i++)
                    if (toccati == null || toccati.Contains(g.Tag[i])) segs.Add(new[] { g.X1[i], g.Y1[i], g.Z1[i], g.X2[i], g.Y2[i], g.Z2[i] });
                view3d.Preview = geoAnteprima ? segs : null;
                view3d.PreviewCenter = geoAnteprima ? centro : null;
                view3d.PreviewLabel = geoAnteprima ? et : "";
                view3d.PreviewCenterText = geoAnteprima && centro != null ? GeoTestoCentro() : "";
                // primo filo toccato: estremo E1 prima e dopo
                GeomCard dopo = m.Geometry.Find(q => q.Id == "GW" && (toccati == null || toccati.Contains(q.I1)));
                GeomCard prima = dopo == null ? null : model.Geometry.Find(q => q.Id == "GW" && q.I1 == dopo.I1);
                List<GeomCard> fl = m.Geometry.FindAll(q => q.Id == "GW");
                if (dopo != null && geoCambia != null)
                {
                    int num = fl.IndexOf(dopo) + 1;
                    string pr = prima == null ? "(filo nuovo)" : string.Format(CultureInfo.CurrentCulture, "X {0,7:0.000}  Y {1,7:0.000}  Z {2,7:0.000}", prima.F1, prima.F2, prima.F3);
                    string dp = string.Format(CultureInfo.CurrentCulture, "X {0,7:0.000}  Y {1,7:0.000}  Z {2,7:0.000}", dopo.F1, dopo.F2, dopo.F3);
                    string fx = dopo.Src != null && Array.Exists(new[] { dopo.Src[2], dopo.Src[3], dopo.Src[4] }, q => !GeoNumero(q))
                        ? "\nformule: " + dopo.Src[2] + "  " + dopo.Src[3] + "  " + dopo.Src[4] : "";
                    geoCambia.Text = "filo " + num + ", estremo E1\nprima: " + pr + "\ndopo:  " + dp + fx;
                    geoCambia.Foreground = Pal.B(Pal.Fore);
                }
                else if (geoCambia != null) { geoCambia.Text = "(nessun filo GW toccato: l'operazione si scrive con una scheda GM in fondo al modello)"; geoCambia.Foreground = Pal.B(Pal.Fore); }
            }
            catch (Exception ex)
            {
                view3d.Preview = null; view3d.PreviewCenter = null; view3d.PreviewLabel = ""; view3d.PreviewCenterText = "";
                if (geoCambia != null) { geoCambia.Text = ex.Message; geoCambia.Foreground = Pal.B(Pal.Red); }
            }
            view3d.InvalidateVisual();
        }

        static bool GeoNumero(string s) { double v; return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v); }

        string GeoTestoCentro()
        {
            switch (geoCentro)
            {
                case 0: return "centro = origine";
                case 2: return "centro = punto scelto";
                case 3: return "centro = estremo del filo " + (geoFilo?.Text ?? "");
                default: return (geoScheda == "Ruota" ? "centro di rotazione" : "centro") + " = centro dell'antenna";
            }
        }

        void GeoApplica()
        {
            try
            {
                HashSet<int> t; double[] c; string et;
                NecModel m = GeoCalcola(out t, out c, out et);
                Registro.Scrivi("AZIONE", "geometria: " + geoScheda + (geoSoloScelti ? " (fili " + string.Join(",", geoSel) + ")" : " (tutta l'antenna)"));
                GeoMetti(m, true);
                Status("Geometria: " + geoScheda.ToLowerInvariant() + " applicata. \"Annulla\" torna indietro.");
            }
            catch (Exception ex) { Status("Geometria: " + ex.Message, true); }
        }
    }
}
