// YA-NEC 0.4.30 - editor dei radiatori multibanda, comune alle schede Dipolo multibanda e Yagi multibanda HF (IC8TEM 15/11/2026 00:06:
// "mi devi permettere sia di inserire le trappole, sia le rastremazioni, sia cambiare le bande, perche' potrei anche fare un inizio di
// yagi vhf"): modo, bande da 80 m a 70 cm, conduttore, bacchette, tubi rastremati, trappole, terreno. Lo schizzo (bozzetto 13 approvato
// il 15/11/2026 00:02) si riempie da qui per tutte e due le schede.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        class EditorRad
        {
            public StackPanel Vista;
            public Action Leggi;          // controlli -> spec (bande, misure, rastremazione, terreno)
            public Action Ricostruisci;   // righe delle trappole
            public TextBlock NotaModo;
        }

        static readonly string[] BandeEditor = { "80 m", "40 m", "30 m", "20 m", "17 m", "15 m", "12 m", "10 m", "6 m", "4 m", "2 m", "70 cm" };

        EditorRad CreaEditorRadiatori(DmSpec spec, Action cambiato, Action<string> avviso)
        {
            EditorRad ed = new EditorRad { Vista = new StackPanel { Spacing = 3 } };
            StackPanel sx = ed.Vista;
            ed.NotaModo = Nota("");
            // 1.0.10 (IC8TEM 16/11/2026 22:22 "mettimi la scelta delle bande su due righe"): griglia di 6 colonne su due righe
            // (80-15 m e 12 m-70 cm), allineate; prima una riga sola che usciva dalla scheda
            Grid wb = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto") };
            List<CheckBox> cb = new List<CheckBox>();
            for (int i = 0; i < BandeEditor.Length; i++)
            {
                string b = BandeEditor[i];
                CheckBox c = new CheckBox { Content = b, IsChecked = spec.Bande.Contains(b), Margin = new Thickness(0, 0, 14, 0), MinWidth = 70 };
                c.IsCheckedChanged += (s, e) => { cambiato(); if (ed.Ricostruisci != null) ed.Ricostruisci(); };
                Grid.SetColumn(c, i % 6); Grid.SetRow(c, i / 6);
                cb.Add(c); wb.Children.Add(c);
            }
            TextBox tD = Tb(Fmt(spec.Diametro * 1000, 1), 55), tDist = Tb(Fmt(spec.Distanza, 2), 55), tGap = Tb(Fmt(spec.Gap, 3), 55), tQ = Tb(Fmt(spec.Quota, 1), 55);
            int terreno = spec.Terreno ? 1 : 0;
            sx.Children.Add(RigaCalc("Modo", Scelta(DipoloMultibanda.Modi, spec.Modo, k =>
            {
                spec.Modo = k;
                if (k == 1 && ParseNum(tD.Text, 2) < 5) tD.Text = "20";
                if (k == 0 && ParseNum(tD.Text, 2) >= 5) tD.Text = "2";
                cambiato();
            })));
            sx.Children.Add(ed.NotaModo);
            DockPanel dBande = RigaCalc("Bande", wb);
            sx.Children.Add(dBande);
            StackPanel rc = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rc.Children.Add(tD); rc.Children.Add(Lbl("mm"));
            DockPanel dCond = RigaCalc("Diametro del conduttore", rc);
            sx.Children.Add(dCond);
            StackPanel rd = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rd.Children.Add(tDist); rd.Children.Add(Lbl("m tra un dipolo e l'altro (0,05-0,1 m con 4 dipoli HF)   morsetti")); rd.Children.Add(tGap); rd.Children.Add(Lbl("m"));
            DockPanel dDist = RigaCalc("Bacchette", rd);
            sx.Children.Add(dDist);
            CheckBox cRas = new CheckBox { Content = "Tubi rastremati", IsChecked = spec.Rastrema, VerticalAlignment = VerticalAlignment.Center };
            TextBox tRas = Tb(spec.SchemaRastr, 250);
            tRas.FontFamily = Pal.Mono;
            ToolTip.SetTip(tRas, "Diametro in mm x lunghezza in m per lato, dal centro verso la punta; l'ultimo tubo arriva alla punta. Al posto del diametro del conduttore; correzione di Leeson accesa.");
            DockPanel dRas = RigaCalc("Rastremazione", cRas, tRas);
            sx.Children.Add(dRas);
            StackPanel pTrap = new StackPanel { Spacing = 4 };
            Button bAddT = Btn("+ trappola", false), bSei = Btn("Proposta: 6 bande da 40/20/15/10 m", false);
            StackPanel rTb = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rTb.Children.Add(bAddT); rTb.Children.Add(bSei);
            StackPanel boxT = new StackPanel { Spacing = 4 };
            boxT.Children.Add(pTrap); boxT.Children.Add(rTb);
            boxT.Children.Add(Nota("Ogni trappola va in tutti e due i bracci del dipolo scelto, subito dopo la punta, con una coda per la banda in piu'. Sola bobina: risuona con la sua capacita' propria (Medhurst) " +
                "e va bene solo per bande circa all'ottava (20 -> 40 m); bobina di carico: induttanza accordata, non isola la coda; trappola LC: bobina + condensatore (25 pF di partenza), per bande vicine (15 -> 17 m, 10 -> 12 m). L'accordo trova le code (e la bobina di carico) insieme alle lunghezze."));
            DockPanel dTrap = RigaCalc("Trappole (bande in piu')", boxT);
            sx.Children.Add(dTrap);
            StackPanel rt = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rt.Children.Add(Scelta(new[] { "spazio libero", "terreno medio (Sommerfeld)" }, terreno, k => { terreno = k; cambiato(); }));
            rt.Children.Add(Lbl("quota")); rt.Children.Add(tQ); rt.Children.Add(Lbl("m"));
            sx.Children.Add(RigaCalc("Terreno", rt));

            ed.Leggi = () =>
            {
                spec.Terreno = terreno == 1; spec.Quota = Math.Max(0.5, ParseNum(tQ.Text, 10));
                spec.Diametro = Math.Max(0.1, ParseNum(tD.Text, 2)) / 1000;
                spec.Distanza = Math.Max(0.005, ParseNum(tDist.Text, 0.1)); spec.Gap = Math.Max(0.002, ParseNum(tGap.Text, 0.05));
                bool xr = spec.Modo == 2;
                dBande.IsVisible = !xr; dCond.IsVisible = !xr; dDist.IsVisible = spec.Modo == 0;
                dRas.IsVisible = !xr; dTrap.IsVisible = spec.Modo == 0;
                spec.Rastrema = cRas.IsChecked == true; spec.SchemaRastr = tRas.Text;
                tD.IsEnabled = !spec.Rastrema;
                if (!xr)
                {
                    spec.Bande = new List<string>();
                    foreach (CheckBox c in cb) if (c.IsChecked == true) spec.Bande.Add((string)c.Content);
                    if (spec.Bande.Count == 0) { spec.Bande.Add("20 m"); cb[Array.IndexOf(BandeEditor, "20 m")].IsChecked = true; }
                }
                ed.NotaModo.Text = spec.Modo == 0 ? "Un dipolo per banda, uno sotto l'altro, tutti sulla stessa alimentazione (il piu' lungo in alto)."
                    : spec.Modo == 1 ? "Si alimenta solo il dipolo della banda piu' bassa; gli altri sono parassiti vicini, nello stesso piano, alternati davanti e dietro (N6BT)."
                    : "I cinque radiatori della XR-5 (20-17-15-12-10 m) con i tubi di fabbrica: coassiale al 12 m, che eccita 10 e 15 m; linea da 450 ohm al 20 m, che eccita 15 e 17 m (T. Schiller N6BT). " +
                      "Senza riflettori le misure di fabbrica non vanno (nec2c: ROS 13 sul 17 m): l'accordo sposta punte, posizioni e linea.";
            };
            foreach (TextBox t in new[] { tD, tDist, tGap, tQ }) t.TextChanged += (s, e) => cambiato();
            cRas.IsCheckedChanged += (s, e) => cambiato();
            tRas.LostFocus += (s, e) => cambiato();
            ed.Ricostruisci = () =>
            {
                pTrap.Children.Clear();
                foreach (DmTrappola t0 in spec.Trappole)
                {
                    DmTrappola t = t0;
                    WrapPanel r = new WrapPanel();
                    List<string> nuove = new List<string>();
                    Band bd = DipoloMultibanda.Banda(t.Dipolo);
                    foreach (Band bb in Bands.All)
                        if (bb.Operating >= 1.8 && bb.Operating < 500 && !spec.Bande.Contains(bb.Name) && bd != null && bb.Operating < bd.Operating) nuove.Add(bb.Name);
                    ComboBox cDip = new ComboBox { ItemsSource = new List<string>(spec.Bande), SelectedItem = spec.Bande.Contains(t.Dipolo) ? t.Dipolo : null, MinWidth = 80, Margin = new Thickness(0, 0, 4, 0) };
                    ComboBox cNu = new ComboBox { ItemsSource = nuove, SelectedItem = nuove.Contains(t.Nuova) ? t.Nuova : null, MinWidth = 80, Margin = new Thickness(0, 0, 6, 0) };
                    cDip.SelectionChanged += (s, e) => { if (cDip.SelectedItem is string x && x != t.Dipolo) { t.Dipolo = x; ed.Ricostruisci(); cambiato(); } };
                    cNu.SelectionChanged += (s, e) => { if (cNu.SelectedItem is string x && x != t.Nuova) { t.Nuova = x; cambiato(); } };
                    TextBox tC = Tb(Fmt(t.Cpf, 0), 45), tQb = Tb(Fmt(t.Q, 0), 45), tSu = Tb(Fmt(t.Supporto * 1000, 0), 45), tFi = Tb(Fmt(t.Filo * 1000, 1), 45);
                    tC.IsVisible = t.Tipo == 2;
                    Action leggiT = () =>
                    {
                        if (t.Tipo == 2) t.Cpf = Math.Max(1, ParseNum(tC.Text, 25));
                        t.Q = Math.Max(10, ParseNum(tQb.Text, 150)); t.Supporto = Math.Max(5, ParseNum(tSu.Text, 50)) / 1000; t.Filo = Math.Max(0.2, ParseNum(tFi.Text, 1.5)) / 1000;
                        cambiato();
                    };
                    foreach (TextBox q in new[] { tC, tQb, tSu, tFi }) q.LostFocus += (s, e) => leggiT();
                    Button bTogli = Btn("togli", false);
                    bTogli.Click += (s, e) => { spec.Trappole.Remove(t); ed.Ricostruisci(); cambiato(); };
                    r.Children.Add(Lbl("dipolo")); r.Children.Add(cDip); r.Children.Add(Lbl("nuova banda")); r.Children.Add(cNu);
                    r.Children.Add(Scelta(new[] { "sola bobina", "bobina di carico", "trappola LC" }, t.Tipo, k => { t.Tipo = k; tC.IsVisible = k == 2; cambiato(); }));
                    r.Children.Add(Lbl(" C")); r.Children.Add(tC); r.Children.Add(Lbl("pF  Q")); r.Children.Add(tQb);
                    r.Children.Add(Lbl("supporto Ø")); r.Children.Add(tSu); r.Children.Add(Lbl("mm  filo Ø")); r.Children.Add(tFi); r.Children.Add(Lbl("mm"));
                    r.Children.Add(bTogli);
                    pTrap.Children.Add(new Border { Child = r, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 2) });
                }
            };
            bAddT.Click += (s, e) =>
            {
                string dip = spec.Bande.Count > 0 ? spec.Bande[spec.Bande.Count - 1] : "10 m";
                string nu = null;
                foreach (Band bb in Bands.All)
                    if (bb.Operating >= 1.8 && bb.Operating < DipoloMultibanda.Banda(dip).Operating && !spec.Bande.Contains(bb.Name) && !spec.Trappole.Exists(q => q.Nuova == bb.Name)) nu = bb.Name;
                if (nu == null) { avviso("Nessuna banda libera sotto il " + dip + "."); return; }
                spec.Trappole.Add(new DmTrappola { Dipolo = dip, Nuova = nu, Tipo = 2 });
                ed.Ricostruisci(); cambiato();
            };
            bSei.Click += (s, e) =>
            {
                string[] q4 = { "40 m", "20 m", "15 m", "10 m" };
                foreach (CheckBox c in cb) c.IsChecked = Array.IndexOf(q4, (string)c.Content) >= 0;
                tDist.Text = Fmt(0.1, 2);
                spec.Trappole.Clear();
                spec.Trappole.Add(new DmTrappola { Dipolo = "15 m", Nuova = "30 m", Tipo = 2 });
                spec.Trappole.Add(new DmTrappola { Dipolo = "10 m", Nuova = "17 m", Tipo = 2 });
                cambiato(); ed.Ricostruisci();
            };
            return ed;
        }

        /// <summary>Schizzo dei radiatori (e dei parassiti della Yagi): bacchette di fronte nel Dipolo multibanda, tutto dall'alto nella Yagi.</summary>
        void RiempiSchizzo(DipoloMbDisegno schizzo, DmSpec spec, NecModel m, Dictionary<int, string> tb, YmSpec y)
        {
            schizzo.Tratti.Clear(); schizzo.Etichette.Clear(); schizzo.Punti.Clear();
            bool vert = spec.Modo == 0 && y == null;
            schizzo.Boom = !vert;
            schizzo.Titolo = y != null ? "Yagi multibanda · vista dall'alto" : (spec.Modo + 1) + " · " + DipoloMultibanda.Modi[spec.Modo];
            schizzo.Sotto = y != null ? "radiatori (" + DipoloMultibanda.Modi[spec.Modo] + ") + un parassita per banda; lobo verso destra"
                : spec.Modo == 0 ? "vista di fronte: un dipolo per banda, stessa alimentazione" : spec.Modo == 1 ? "vista dall'alto: alimentato solo il " + spec.Bande[0] + ", parassiti vicini nello stesso piano"
                : "vista dall'alto, boom orizzontale: tubi di fabbrica, senza riflettori e direttore";
            int nRad = 0;
            foreach (GeomCard c in m.Geometry) if (c.Id == "GW") nRad = Math.Max(nRad, c.I1);
            HashSet<int> parassiti = new HashSet<int>();
            if (y != null) for (int i = 0; i < spec.Bande.Count; i++) parassiti.Add(nRad - i);
            Dictionary<string, double[]> fine = new Dictionary<string, double[]>();
            foreach (GeomCard c in m.Geometry)
            {
                if (c.Id != "GW") continue;
                string bnd; tb.TryGetValue(c.I1, out bnd);
                double ax = vert ? c.F2 : c.F1, ay = vert ? c.F3 : c.F2, bx = vert ? c.F5 : c.F4, by = vert ? c.F6 : c.F5;
                double lung = Math.Sqrt((c.F4 - c.F1) * (c.F4 - c.F1) + (c.F5 - c.F2) * (c.F5 - c.F2) + (c.F6 - c.F3) * (c.F6 - c.F3));
                bool trap = spec.Modo == 0 && bnd != null && !spec.Bande.Contains(bnd);
                bool par = parassiti.Contains(c.I1);
                int tipo = par ? 1 : trap ? (Math.Abs(lung - 0.1) < 1e-6 ? 2 : 1) : spec.Modo == 0 ? 0 : spec.Modo == 1 ? (bnd == spec.Bande[0] ? 0 : 1) : (bnd == "12 m" || bnd == "20 m" ? 0 : 1);
                schizzo.Tratti.Add(new DipoloMbDisegno.Tratto { X1 = ax, Y1 = ay, X2 = bx, Y2 = by, Tipo = tipo });
                if (bnd != null && tipo != 2)
                {
                    string chiave = par ? "P" + bnd : bnd;
                    double[] q = vert ? (bx >= ax ? new[] { bx, by } : new[] { ax, ay }) : (by >= ay ? new[] { bx, by } : new[] { ax, ay });
                    double[] f0;
                    if (!fine.TryGetValue(chiave, out f0) || (vert ? q[0] > f0[0] : q[1] > f0[1])) fine[chiave] = q;
                }
            }
            foreach (KeyValuePair<string, double[]> kv in fine)
            {
                bool par = kv.Key.StartsWith("P");
                string b = par ? kv.Key.Substring(1) : kv.Key;
                DmTrappola tr = !par && spec.Modo == 0 ? spec.Trappole.Find(q => q.Nuova == b) : null;
                string txt = par ? (y.Direttore[b] ? "dir. " : "rifl. ") + b + " " + Fmt(y.PL[b], 2) + " m a " + Fmt(y.PS[b] * DipoloMultibanda.Lambda(b), 2) + " m"
                    : tr != null ? b + ": " + (tr.Tipo == 0 ? "sola bobina " : tr.Tipo == 1 ? "bobina " : "LC ") + Fmt(tr.Luh, 2) + " uH" + (tr.Tipo == 1 ? "" : ", " + Fmt(tr.Cpf, 1) + " pF") + ", coda " + Fmt(tr.Coda, 2) + " m"
                    : spec.Modo == 2 ? b + " punte " + Fmt(spec.L.ContainsKey(b) ? spec.L[b] : 0, 3) + " m"
                    : b + " " + Fmt(spec.L.ContainsKey(b) ? spec.L[b] : 0, 2) + " m" + (spec.Modo == 1 && spec.S.ContainsKey(b) && spec.S[b] > 0 ? " a " + Fmt(spec.S[b], 3) + " m" : "");
                int tp = par || tr != null ? 1 : spec.Modo == 0 ? 0 : (spec.Modo == 1 ? (b == spec.Bande[0] ? 0 : 1) : (b == "12 m" || b == "20 m" ? 0 : 1));
                if (y != null && !par && spec.Modo == 0) continue;   // dall'alto le bacchette si sovrappongono: si scrivono solo i parassiti
                schizzo.Etichette.Add(new DipoloMbDisegno.Etichetta { X = kv.Value[0], Y = kv.Value[1], T = txt, Tipo = tp });
            }
            foreach (SourceSpec so in m.Sources)
                foreach (GeomCard c in m.Geometry)
                    if (c.Id == "GW" && c.I1 == so.Tag) { schizzo.Punti.Add(vert ? new[] { (c.F2 + c.F5) / 2, (c.F3 + c.F6) / 2 } : new[] { (c.F1 + c.F4) / 2, (c.F2 + c.F5) / 2 }); break; }
            if (spec.Modo == 2)
            {
                double x12 = DipoloMultibanda.XRadiatore(spec, "12 m"), x20 = DipoloMultibanda.XRadiatore(spec, "20 m");
                schizzo.Tratti.Add(new DipoloMbDisegno.Tratto { X1 = x20, Y1 = 0, X2 = x12, Y2 = 0, Tipo = 3 });
                schizzo.Etichette.Add(new DipoloMbDisegno.Etichetta { X = x12, Y = 0, T = "linea 450 Ω " + Fmt(spec.Linea, 3) + " m, coassiale al 12 m", Tipo = 3 });
            }
            schizzo.InvalidateVisual();
        }
    }
}
