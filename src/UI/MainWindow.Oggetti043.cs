// YA-NEC 0.4.3 - Oggetti inseriti: Rastremazioni (bozzetto approvato doc/bozzetto-rastremazioni.png, come il Taper wire set di MMANA),
// Trasformatori e Reti L / di sfasamento (bozzetto approvato doc/bozzetto-oggetti-inseriti.png; formule di Lahlum e ibrido di Collins).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        bool rastDiametro, rastPollici;
        int rastFiloSel = -1;

        static readonly CultureInfo IC = CultureInfo.InvariantCulture;

        string RastL(double l) { return l < 0 ? "resto" : FN(rastPollici ? l / 0.0254 : l, rastPollici ? 2 : 3); }
        string RastR(double r) { double v = rastDiametro ? 2 * r : r; return FN(rastPollici ? v / 0.0254 : v * 1000, rastPollici ? 3 : 2); }
        double LeggiRastL(string t, double def)
        {
            t = (t ?? "").Trim();
            if (t.Length == 0) return double.NaN;
            if (string.Equals(t, "resto", StringComparison.OrdinalIgnoreCase)) return -1;
            double v = ParseNum(t, def);
            return rastPollici ? v * 0.0254 : v;
        }
        double LeggiRastR(string t, double def)
        {
            t = (t ?? "").Trim();
            if (t.Length == 0) return double.NaN;
            double v = ParseNum(t, def);
            v = rastPollici ? v * 0.0254 : v / 1000;
            return rastDiametro ? v / 2 : v;
        }

        List<int> FiliConRastremazione(int n)
        {
            List<int> l = new List<int>();
            List<GeomCard> fl = FiliGw();
            for (int i = 0; i < fl.Count; i++) if (fl[i].F7 < 0 && (int)Math.Round(-fl[i].F7) == n) l.Add(i + 1);
            return l;
        }

        Control PannelloRastremazioni(bool compatto = false)
        {
            StackPanel body = new StackPanel { Spacing = 2 };
            StackPanel barra = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 6) };
            Button nuova = Btn("+ nuova", false), arrl = Btn("da tabella ARRL / costruttore...", false);
            barra.Children.Add(nuova); barra.Children.Add(arrl);
            barra.Children.Add(new TextBlock { Text = "valori in", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(12, 0, 0, 0) });
            barra.Children.Add(Scelta(new[] { "raggio", "diametro" }, rastDiametro ? 1 : 0, k => { rastDiametro = k == 1; RidisegnaTabelle("rastremazioni"); }));
            barra.Children.Add(Scelta(new[] { "mm / m", "pollici" }, rastPollici ? 1 : 0, k => { rastPollici = k == 1; RidisegnaTabelle("rastremazioni"); }));
            body.Children.Add(barra);

            int maxT = 5;
            foreach (Rastremazione r in model.Rastremazioni) maxT = Math.Max(maxT, r.L.Count);
            StackPanel hd = new StackPanel { Orientation = Orientation.Horizontal };
            hd.Children.Add(Intest("R", 40, true)); hd.Children.Add(Intest("Tipo", 60));
            string uL = rastPollici ? "\"" : "m", uR = rastPollici ? "\"" : "mm";
            for (int i = 1; i <= maxT; i++) { hd.Children.Add(Intest("L" + i + " (" + uL + ")", 64)); hd.Children.Add(Intest((rastDiametro ? "D" : "R") + i + " (" + uR + ")", 64)); }
            hd.Children.Add(Intest("usata da", 150)); hd.Children.Add(IntestRiga());
            body.Children.Add(hd);
            List<TextBox[]> celle = new List<TextBox[]>();
            List<ComboBox> tipi = new List<ComboBox>();
            for (int ri = 0; ri < model.Rastremazioni.Count; ri++)
            {
                Rastremazione r = model.Rastremazioni[ri];
                int idx = ri;
                StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(new Border { Width = 40, Child = new TextBlock { Text = "-" + r.N, Foreground = Pal.B(Pal.Violet), FontWeight = FontWeight.Bold, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                ComboBox cb = new ComboBox { Width = 60, ItemsSource = new[] { "<>", ">", "<" }, SelectedIndex = r.Tipo };
                tipi.Add(cb);
                row.Children.Add(cb);
                TextBox[] tb = new TextBox[2 * maxT];
                for (int i = 0; i < maxT; i++)
                {
                    tb[2 * i] = Cella(i < r.L.Count ? RastL(r.L[i]) : "", 64);
                    tb[2 * i + 1] = Cella(i < r.R.Count ? RastR(r.R[i]) : "", 64);
                    row.Children.Add(tb[2 * i]); row.Children.Add(tb[2 * i + 1]);
                }
                celle.Add(tb);
                List<int> usa = FiliConRastremazione(r.N);
                row.Children.Add(Cella(usa.Count == 0 ? "nessun filo" : "fili " + string.Join(", ", usa), 150, true, true));
                Button del = BtnRiga("✕", "togli la riga", true);
                del.Margin = new Thickness(10, 0, 0, 0);
                del.Click += (s, e) =>
                {
                    if (FiliConRastremazione(model.Rastremazioni[idx].N).Count > 0) { Status("La rastremazione -" + model.Rastremazioni[idx].N + " e' usata da qualche filo: cambiare prima il raggio di quei fili.", true); return; }
                    model.Rastremazioni.RemoveAt(idx); RidisegnaTabelle("rastremazioni");
                };
                row.Children.Add(del);
                body.Children.Add(row);
            }
            if (model.Rastremazioni.Count == 0)
                body.Children.Add(Nota("Nessuna rastremazione. \"+ nuova\" aggiunge una riga; poi nella tabella dei fili si scrive -1 (o -2...) nel raggio del filo.", Pal.Fore));
            body.Children.Add(Nota("Tipo: <> simmetrica dal centro (L1 = tratto centrale intero)   > dall'estremo E1   < dall'estremo E2  |  \"resto\" = fino alla punta (99999,9 nel file MMANA)."));

            Func<bool> leggi = () =>
            {
                for (int ri = 0; ri < model.Rastremazioni.Count; ri++)
                {
                    Rastremazione r = model.Rastremazioni[ri];
                    List<double> L = new List<double>(), R = new List<double>();
                    TextBox[] tb = celle[ri];
                    for (int i = 0; i < tb.Length / 2; i++)
                    {
                        double l = LeggiRastL(tb[2 * i].Text, double.NaN), rr = LeggiRastR(tb[2 * i + 1].Text, double.NaN);
                        if (double.IsNaN(l) && double.IsNaN(rr)) continue;
                        if (double.IsNaN(l) || double.IsNaN(rr) || rr <= 0 || l == 0) { Status("Rastremazione -" + r.N + ", tratto " + (i + 1) + ": servono lunghezza (o \"resto\") e " + (rastDiametro ? "diametro" : "raggio") + ".", true); return false; }
                        L.Add(l); R.Add(rr);
                    }
                    if (L.Count == 0) { Status("Rastremazione -" + r.N + ": nessun tratto.", true); return false; }
                    r.L = L; r.R = R; r.Tipo = Math.Max(0, tipi[ri].SelectedIndex);
                }
                return true;
            };
            nuova.Click += (s, e) =>
            {
                if (!leggi()) return;
                int n = 1;
                while (model.Rastremazioni.Exists(q => q.N == n)) n++;
                model.Rastremazioni.Add(new Rastremazione { N = n, L = new List<double> { 2.0, -1 }, R = new List<double> { 0.0125, 0.008 } });
                RidisegnaTabelle("rastremazioni");
            };
            arrl.Click += (s, e) =>
            {
                if (!leggi()) return;
                int n = 1;
                while (model.Rastremazioni.Exists(q => q.N == n)) n++;
                // esempio di costruttore: riflettore 20 m della Force 12 XR-5 (manuale XR-5; tubi 25,4 / 22 / 19 / 16 / 13 / 10 mm, mezzo elemento 5,55 m)
                model.Rastremazioni.Add(new Rastremazione { N = n, Tipo = 0, L = new List<double> { 3.66, 0.30, 0.84, 0.84, 0.84, -1 }, R = new List<double> { 0.0127, 0.011, 0.0095, 0.008, 0.0065, 0.005 } });
                Status("Aggiunta la rastremazione -" + n + " con i tubi del riflettore 20 m della XR-5 (esempio da modificare).");
                RidisegnaTabelle("rastremazioni");
            };

            // profilo del filo selezionato
            List<GeomCard> fl = FiliGw();
            List<int> conR = new List<int>();
            for (int i = 0; i < fl.Count; i++) if (fl[i].F7 < 0) conR.Add(i);
            if (rastFiloSel < 0 || rastFiloSel >= fl.Count || fl[rastFiloSel].F7 >= 0) rastFiloSel = conR.Count > 0 ? conR[0] : -1;
            StackPanel prof = new StackPanel { Spacing = 4 };
            if (rastFiloSel >= 0 && !compatto)
            {
                GeomCard g = fl[rastFiloSel];
                Rastremazione r = model.Rastremazioni.Find(q => q.N == (int)Math.Round(-g.F7));
                StackPanel sel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                sel.Children.Add(new TextBlock { Text = "Filo", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore) });
                ComboBox cf = new ComboBox { Width = 120 };
                List<string> nomi = new List<string>();
                foreach (int i in conR) nomi.Add("filo " + (i + 1) + " (" + FN(fl[i].F7, 0) + ")");
                cf.ItemsSource = nomi; cf.SelectedIndex = Math.Max(0, conR.IndexOf(rastFiloSel));
                cf.SelectionChanged += (s, e) => { if (cf.SelectedIndex >= 0) { rastFiloSel = conR[cf.SelectedIndex]; RidisegnaTabelle("rastremazioni"); } };
                sel.Children.Add(cf);
                sel.Children.Add(new TextBlock { Text = "profilo reale del tubo (diametri ingranditi x20)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) });
                prof.Children.Add(sel);
                if (r != null)
                {
                    double dx = g.F4 - g.F1, dy = g.F5 - g.F2, dz = g.F6 - g.F3, len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    List<string> note = new List<string>();
                    List<OggettiExtra.Tubo> tubi = null;
                    try { tubi = OggettiExtra.Tubi(r, len, Math.Max(1, g.I2), note, "Filo " + (rastFiloSel + 1)); } catch (NecException ex) { note.Add(ex.Message); }
                    if (tubi != null)
                    {
                        double W = 980, H = 120, sx = W / len, dmax = 0;
                        foreach (OggettiExtra.Tubo t in tubi) dmax = Math.Max(dmax, t.Raggio);
                        double sy = Math.Min(20 * sx, 45 / Math.Max(dmax, 1e-6));
                        Canvas cv = new Canvas { Width = W + 20, Height = H + 30 };
                        foreach (OggettiExtra.Tubo t in tubi)
                        {
                            double h = Math.Max(4, 2 * t.Raggio * sy), x0 = 10 + t.Da * sx, w = Math.Max(2, (t.A - t.Da) * sx);
                            Rectangle rc = new Rectangle { Width = w, Height = h, Fill = Pal.B(Pal.A(60, Pal.Accent)), Stroke = Pal.B(Pal.Accent), StrokeThickness = 1.2 };
                            Canvas.SetLeft(rc, x0); Canvas.SetTop(rc, H / 2 - h / 2);
                            cv.Children.Add(rc);
                            if (w > 70)
                            {
                                TextBlock tt = new TextBlock { Text = "Ø " + FN(2 * t.Raggio * 1000, 1) + " mm x " + FN(t.A - t.Da, 2) + " m (" + t.Seg + " seg.)", FontSize = 11.5, Foreground = Pal.B(Pal.Fore) };
                                Canvas.SetLeft(tt, x0 + 4); Canvas.SetTop(tt, H / 2 + h / 2 + 2);
                                cv.Children.Add(tt);
                            }
                        }
                        Line cl = new Line { StartPoint = new Point(10 + W / 2, 4), EndPoint = new Point(10 + W / 2, H - 4), Stroke = Pal.B(Pal.Red), StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 3, 3 } };
                        cv.Children.Add(cl);
                        TextBlock e1 = new TextBlock { Text = "E1", FontSize = 11, Foreground = Pal.B(Pal.Muted) }, e2 = new TextBlock { Text = "E2 (" + FN(len, 2) + " m)", FontSize = 11, Foreground = Pal.B(Pal.Muted) };
                        Canvas.SetLeft(e1, 10); Canvas.SetTop(e1, H + 8); Canvas.SetLeft(e2, W - 60); Canvas.SetTop(e2, H + 8);
                        cv.Children.Add(e1); cv.Children.Add(e2);
                        prof.Children.Add(new ScrollViewer { Content = cv, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto });
                    }
                    foreach (string n0 in note) prof.Children.Add(Nota("⚠ " + n0, Pal.Orange));
                }
            }

            Button ap = Btn("Applica e ricalcola", true);
            ap.Click += (s, e) => { if (leggi()) ApplicaERicalcola("rastremazioni"); };
            Border tab = PannelloTesta("Rastremazioni " + model.Rastremazioni.Count, ap, "tubi telescopici: nel raggio del filo -1, -2... come in MMANA", body);
            if (compatto) return tab;

            StackPanel all = new StackPanel();
            all.Children.Add(tab);
            if (prof.Children.Count > 0) all.Children.Add(Pannello("Filo " + (rastFiloSel + 1) + " con la rastremazione " + FN(fl[rastFiloSel].F7, 0), prof));
            Grid g2 = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*") };
            g2.Children.Add(Pannello("Come lo calcola il motore", Nota("Il filo resta uno nella tabella (comodo da modificare e da ottimizzare). Per il calcolo diventa un tubo per tratto, con lo stesso tag e lo stesso numero " +
                "di segmenti in totale, distribuiti in proporzione alle lunghezze (almeno uno per tubo): le sorgenti e i carichi restano sugli stessi segmenti. " +
                "Con la correzione Leeson attiva (Impostazioni, tubi telescopici) l'elemento entro il 15 % della risonanza e' sostituito da un tubo equivalente ricalcolato a ogni frequenza, " +
                "come in EZNEC; senza correzione il NEC-2 fa risuonare gli elementi a gradini circa l'1-2 % piu' in basso. Nel file .nec si scrivono i tubi veri (leggibili da tutti i programmi) " +
                "e una riga CM YA-NEC che a YA-NEC permette di rimettere insieme il filo.", Pal.Fore)));
            StackPanel str = new StackPanel { Spacing = 6 };
            Button bRis = Btn("Risonanza del filo", false), bUni = Btn("Unisci i tubi in linea", false), bDiv = Btn("Dividi in tubi", false), bPeso = Btn("Peso dei tubi", false);
            TextBlock esito = Nota("", Pal.Fore);
            WrapPanel wb = new WrapPanel();
            foreach (Button b in new[] { bRis, bUni, bDiv, bPeso }) { b.Margin = new Thickness(0, 0, 6, 4); wb.Children.Add(b); }
            str.Children.Add(wb);
            str.Children.Add(Nota("Risonanza: modello a linee del filo selezionato da solo (Schelkunoff, come la correzione Leeson). Unisci: i fili in linea di diametro diverso (da .ez, .weq, .nec) " +
                "diventano un filo con la rastremazione. Dividi: il contrario, per esportare verso programmi senza rastremazioni. Peso: tubi di alluminio con parete da 1,25 mm (2,7 g/cm3)."));
            str.Children.Add(esito);
            bRis.Click += (s, e) =>
            {
                if (rastFiloSel < 0) { esito.Text = "Nessun filo con rastremazione."; return; }
                GeomCard g = fl[rastFiloSel];
                Rastremazione r = model.Rastremazioni.Find(q => q.N == (int)Math.Round(-g.F7));
                if (r == null) return;
                double len = Math.Sqrt((g.F4 - g.F1) * (g.F4 - g.F1) + (g.F5 - g.F2) * (g.F5 - g.F2) + (g.F6 - g.F3) * (g.F6 - g.F3));
                double fr = OggettiExtra.RisonanzaLinee(r, len);
                esito.Text = double.IsNaN(fr) ? "Risonanza: solo per le rastremazioni simmetriche (<>)." :
                    "Filo " + (rastFiloSel + 1) + " (" + FN(len, 3) + " m): risonanza del modello a linee " + FN(fr, 3) + " MHz; un tubo uniforme della stessa lunghezza risuonerebbe a circa " + FN(299.792458 / (2 * len) * 0.975, 3) + " MHz.";
            };
            bDiv.Click += (s, e) =>
            {
                if (!leggi()) return;
                try
                {
                    model.Geometry = OggettiExtra.Espandi(model, model.Geometry);
                    model.Rastremazioni.Clear();
                    ApplicaERicalcola("rastremazioni divise in tubi");
                    Status("Fili rastremati divisi in tubi (stesso tag): la tabella Rastremazioni e' vuota.");
                }
                catch (NecException ex) { Status(ex.Message, true); }
            };
            bUni.Click += (s, e) =>
            {
                if (!leggi()) return;
                int fatti = UnisciTubi();
                if (fatti > 0) { ApplicaERicalcola("tubi uniti in rastremazioni"); Status(fatti + " elementi a tubi uniti in un filo con la rastremazione."); }
                else Status("Nessun elemento a tubi in linea (fili allineati e collegati con diametri diversi) da unire.");
            };
            bPeso.Click += (s, e) =>
            {
                double kg = 0;
                foreach (GeomCard g in OggettiExtra.Espandi(model, model.Geometry))
                {
                    if (g.Id != "GW") continue;
                    double len = Math.Sqrt((g.F4 - g.F1) * (g.F4 - g.F1) + (g.F5 - g.F2) * (g.F5 - g.F2) + (g.F6 - g.F3) * (g.F6 - g.F3));
                    double ro = Math.Abs(g.F7), ri = Math.Max(0, ro - 0.00125);
                    kg += Math.PI * (ro * ro - ri * ri) * len * 2700;
                }
                esito.Text = "Peso dei tubi di tutti i fili (alluminio, parete 1,25 mm): circa " + FN(kg, 2) + " kg (senza boom, staffe e sovrapposizioni dei tubi).";
            };
            g2.Children.Add(new Border { Child = Pannello("Strumenti", str) });
            Grid.SetColumn(g2.Children[1], 2);
            all.Children.Add(g2);
            return all;
        }

        /// <summary>Fili in linea con diametri diversi (catene trovate come per la correzione Leeson) -> un filo con la rastremazione.</summary>
        int UnisciTubi()
        {
            List<Leeson.Catena> cs = Leeson.Trova(model);
            int fatti = 0;
            HashSet<int> togli = new HashSet<int>();
            List<KeyValuePair<int, GeomCard>> nuovi = new List<KeyValuePair<int, GeomCard>>();
            foreach (Leeson.Catena c in cs)
            {
                if (c.Lato.Count < 2 || c.Schede.Count < 2) continue;
                if (c.Motivo != null && c.Motivo.StartsWith("fili non collineari", StringComparison.Ordinal)) continue;
                bool simmetrica = c.Motivo == null || !c.Motivo.StartsWith("elemento non simmetrico", StringComparison.Ordinal);
                if (!simmetrica) continue;
                // tubi lungo l'elemento, dal centro alla punta: L1 = tratto centrale intero
                Rastremazione r = new Rastremazione { Tipo = 0 };
                double prev = 0;
                for (int i = 0; i < c.Lato.Count; i++)
                {
                    double fine = c.Lato[i][0];
                    r.L.Add(i == 0 ? 2 * fine : fine - prev);
                    r.R.Add(c.Lato[i][1] / 2);
                    prev = fine;
                }
                r.L[r.L.Count - 1] = -1;
                Rastremazione uguale = model.Rastremazioni.Find(q => q.Tipo == 0 && q.L.Count == r.L.Count && Simili(q, r));
                if (uguale == null)
                {
                    int n = 1;
                    while (model.Rastremazioni.Exists(q => q.N == n)) n++;
                    r.N = n;
                    model.Rastremazioni.Add(r);
                    uguale = r;
                }
                // filo unico: estremi della catena, tag del filo centrale, segmenti sommati; sorgenti e carichi rinumerati sul nuovo tag
                List<GeomCard> g = model.Geometry;
                double H = c.Lato[c.Lato.Count - 1][0];
                double x1 = c.Cx - c.Ux * H, y1 = c.Cy - c.Uy * H, z1 = c.Cz - c.Uz * H, x2 = c.Cx + c.Ux * H, y2 = c.Cy + c.Uy * H, z2 = c.Cz + c.Uz * H;
                List<int> ord = new List<int>(c.Schede);
                Func<GeomCard, double> pos = q => Math.Min((q.F1 - x1) * c.Ux + (q.F2 - y1) * c.Uy + (q.F3 - z1) * c.Uz, (q.F4 - x1) * c.Ux + (q.F5 - y1) * c.Uy + (q.F6 - z1) * c.Uz);
                ord.Sort((a, b) => pos(g[a]).CompareTo(pos(g[b])));
                int tagC = g[ord[ord.Count / 2]].I1, nTot = 0;
                Dictionary<int, int> offset = new Dictionary<int, int>();
                Dictionary<int, bool> dritto = new Dictionary<int, bool>();
                foreach (int i in ord)
                {
                    GeomCard q = g[i];
                    offset[q.I1] = nTot;
                    dritto[q.I1] = (q.F4 - q.F1) * c.Ux + (q.F5 - q.F2) * c.Uy + (q.F6 - q.F3) * c.Uz > 0;
                    nTot += q.I2;
                }
                Func<int, int, int> nuovoSeg = (tag, seg) => { GeomCard q = g.Find(w => w.Id == "GW" && w.I1 == tag && c.Schede.Contains(g.IndexOf(w))); int s = dritto[tag] ? seg : q.I2 + 1 - seg; return offset[tag] + s; };
                foreach (SourceSpec s in model.Sources) if (offset.ContainsKey(s.Tag)) { s.Seg = nuovoSeg(s.Tag, s.Seg); s.Tag = tagC; }
                foreach (LoadSpec l in model.Loads) if (offset.ContainsKey(l.Tag) && l.From > 0) { l.From = nuovoSeg(l.Tag, l.From); l.To = nuovoSeg(l.Tag, Math.Max(1, l.To)); if (l.To < l.From) { int t = l.To; l.To = l.From; l.From = t; } l.Tag = tagC; }
                foreach (NetworkSpec n in model.Networks)
                {
                    if (offset.ContainsKey(n.Tag1)) { n.Seg1 = nuovoSeg(n.Tag1, n.Seg1); n.Tag1 = tagC; }
                    if (offset.ContainsKey(n.Tag2)) { n.Seg2 = nuovoSeg(n.Tag2, n.Seg2); n.Tag2 = tagC; }
                }
                GeomCard nw = new GeomCard { Id = "GW", I1 = tagC, I2 = nTot, F1 = x1, F2 = y1, F3 = z1, F4 = x2, F5 = y2, F6 = z2, F7 = -uguale.N };
                nw.Src = new[] { tagC.ToString(IC), nTot.ToString(IC), FN0(x1), FN0(y1), FN0(z1), FN0(x2), FN0(y2), FN0(z2), "-" + uguale.N };
                nuovi.Add(new KeyValuePair<int, GeomCard>(ord[0], nw));
                foreach (int i in c.Schede) togli.Add(i);
                fatti++;
            }
            if (fatti == 0) return 0;
            List<GeomCard> geo = new List<GeomCard>();
            for (int i = 0; i < model.Geometry.Count; i++)
            {
                foreach (KeyValuePair<int, GeomCard> kv in nuovi) if (kv.Key == i) geo.Add(kv.Value);
                if (!togli.Contains(i)) geo.Add(model.Geometry[i]);
            }
            model.Geometry = geo;
            return fatti;
        }

        static string FN0(double v) { return Math.Abs(v) < 1e-12 ? "0" : v.ToString("0.######", IC); }

        static bool Simili(Rastremazione a, Rastremazione b)
        {
            for (int i = 0; i < a.L.Count; i++)
            {
                if ((a.L[i] < 0) != (b.L[i] < 0)) return false;
                if (a.L[i] >= 0 && Math.Abs(a.L[i] - b.L[i]) > 1e-4) return false;
                if (Math.Abs(a.R[i] - b.R[i]) > 1e-6) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------------------------------------
        // Trasformatori
        // ------------------------------------------------------------------------------------------------
        class TrRow { public string P1 = "", P2 = ""; public double Z1 = 50, Z2 = 200; public bool Inv; }
        List<TrRow> trRows;
        NecModel trModel;

        Control PannelloTrasformatori()
        {
            if (trRows == null || trModel != model)
            {
                trModel = model;
                trRows = new List<TrRow>();
                foreach (Trasformatore t in model.Trasformatori) trRows.Add(new TrRow { P1 = SegToPulse(t.Tag1, t.Seg1), P2 = SegToPulse(t.Tag2, t.Seg2), Z1 = t.Z1, Z2 = t.Z2, Inv = t.Inv });
            }
            StackPanel body = new StackPanel { Spacing = 2 };
            StackPanel hd = new StackPanel { Orientation = Orientation.Horizontal };
            hd.Children.Add(Intest("", 30, true)); hd.Children.Add(Intest("Porta 1 (PULSE)", 100)); hd.Children.Add(Intest("Porta 2 (PULSE)", 100));
            hd.Children.Add(Intest("Z rel. 1 (ohm)", 90)); hd.Children.Add(Intest("Z rel. 2 (ohm)", 90)); hd.Children.Add(Intest("Inv.", 50)); hd.Children.Add(Intest("rapporto spire", 110)); hd.Children.Add(IntestRiga());
            body.Children.Add(hd);
            for (int i = 0; i < trRows.Count; i++)
            {
                int ri = i;
                TrRow tr = trRows[i];
                StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(NumCella((i + 1).ToString(IC)));
                TextBox p1 = Cella(tr.P1, 100, false, true), p2 = Cella(tr.P2, 100, false, true), z1 = Cella(FN(tr.Z1, 2), 90), z2 = Cella(FN(tr.Z2, 2), 90);
                CheckBox inv = new CheckBox { IsChecked = tr.Inv, Width = 50, HorizontalAlignment = HorizontalAlignment.Center };
                TextBox rap = Cella("1 : " + FN(Math.Sqrt(Math.Max(1e-9, tr.Z2 / Math.Max(1e-9, tr.Z1))), 3), 110, true);
                p1.LostFocus += (s, e) => tr.P1 = p1.Text;
                p2.LostFocus += (s, e) => tr.P2 = p2.Text;
                z1.LostFocus += (s, e) => { tr.Z1 = ParseNum(z1.Text, tr.Z1); rap.Text = "1 : " + FN(Math.Sqrt(Math.Max(1e-9, tr.Z2 / Math.Max(1e-9, tr.Z1))), 3); };
                z2.LostFocus += (s, e) => { tr.Z2 = ParseNum(z2.Text, tr.Z2); rap.Text = "1 : " + FN(Math.Sqrt(Math.Max(1e-9, tr.Z2 / Math.Max(1e-9, tr.Z1))), 3); };
                inv.IsCheckedChanged += (s, e) => tr.Inv = inv.IsChecked == true;
                row.Children.Add(p1); row.Children.Add(p2); row.Children.Add(z1); row.Children.Add(z2); row.Children.Add(inv); row.Children.Add(rap);
                Button del = BtnRiga("✕", "togli la riga", true);
                del.Margin = new Thickness(10, 0, 0, 0);
                del.Click += (s, e) => { trRows.RemoveAt(ri); RidisegnaTabelle("trasformatori"); };
                row.Children.Add(del);
                body.Children.Add(row);
            }
            StackPanel nx = new StackPanel { Orientation = Orientation.Horizontal };
            nx.Children.Add(NumCella((trRows.Count + 1).ToString(IC)));
            TextBox next = Cella("", 520, false, true);
            next.Classes.Add("next");
            next.Watermark = "next – scrivi qui la porta 1 per aggiungere un trasformatore (es. w9c)";
            next.LostFocus += (s, e) => { if (!string.IsNullOrWhiteSpace(next.Text)) { trRows.Add(new TrRow { P1 = next.Text.Trim() }); RidisegnaTabelle("trasformatori"); } };
            nx.Children.Add(next);
            body.Children.Add(nx);
            Button ap = Btn("Applica e ricalcola", true);
            ap.Click += (s, e) =>
            {
                List<Trasformatore> l = new List<Trasformatore>();
                foreach (TrRow r in trRows)
                {
                    int t1, s1, t2, s2; string err;
                    if (!PulseToSeg(r.P1, out t1, out s1, out err)) { Status("Trasformatore, porta 1 " + r.P1 + ": " + err, true); return; }
                    if (!PulseToSeg(r.P2, out t2, out s2, out err)) { Status("Trasformatore, porta 2 " + r.P2 + ": " + err, true); return; }
                    if (r.Z1 <= 0 || r.Z2 <= 0) { Status("Trasformatore " + r.P1 + "-" + r.P2 + ": le impedenze devono essere positive.", true); return; }
                    if (t1 == t2 && s1 == s2) { Status("Trasformatore " + r.P1 + ": le due porte devono essere su segmenti diversi.", true); return; }
                    l.Add(new Trasformatore { Tag1 = t1, Seg1 = s1, Tag2 = t2, Seg2 = s2, Z1 = r.Z1, Z2 = r.Z2, Inv = r.Inv });
                }
                model.Trasformatori = l;
                ApplicaERicalcola("trasformatori");
            };
            body.Children.Add(Nota("Trasformatore ideale tra due punti (come in EZNEC): Z rel. 1 e Z rel. 2 sono le impedenze che il trasformatore fa corrispondere (per esempio 600 e 100 ohm, rapporto spire 1 : 0,41); " +
                "Inv. = polarita' invertita. Nel calcolo diventa una scheda NT (reattanza magnetizzante 10000 volte Z1): per collegarlo a una linea si usa un filo corto lontano (\"filo virtuale\"). " +
                "Nel file .nec si scrive la scheda NT (letta da tutti i programmi) e la riga CM YA-NEC TRASF."));
            return PannelloTesta("Trasformatori " + trRows.Count, ap, "porta 1 · porta 2 · Z rel. 1 · Z rel. 2 · inv.", body);
        }

        // ------------------------------------------------------------------------------------------------
        // Reti L e di sfasamento
        // ------------------------------------------------------------------------------------------------
        class RlRow { public string P1 = "", P2 = ""; public double Rs, Ls, Cs, Rp, Lp, Cp; public int Lato = 2; }
        List<RlRow> rlRows;
        NecModel rlModel;

        Control PannelloRetiL()
        {
            if (rlRows == null || rlModel != model)
            {
                rlModel = model;
                rlRows = new List<RlRow>();
                foreach (ReteL q in model.RetiL)
                    rlRows.Add(new RlRow { P1 = SegToPulse(q.Tag1, q.Seg1), P2 = SegToPulse(q.Tag2, q.Seg2), Rs = q.Rs, Ls = q.Ls * 1e6, Cs = q.Cs * 1e12, Rp = q.Rp, Lp = q.Lp * 1e6, Cp = q.Cp * 1e12, Lato = q.LatoParallelo });
            }
            StackPanel all = new StackPanel();
            StackPanel body = new StackPanel { Spacing = 2 };
            StackPanel hd = new StackPanel { Orientation = Orientation.Horizontal };
            hd.Children.Add(Intest("", 30, true)); hd.Children.Add(Intest("Porta 1", 80)); hd.Children.Add(Intest("Porta 2", 80));
            hd.Children.Add(Intest("ramo", 80)); hd.Children.Add(Intest("R ohm", 70)); hd.Children.Add(Intest("L uH", 70)); hd.Children.Add(Intest("C pF (0 = no)", 80)); hd.Children.Add(Intest("parallelo su", 90)); hd.Children.Add(IntestRiga());
            body.Children.Add(hd);
            for (int i = 0; i < rlRows.Count; i++)
            {
                int ri = i;
                RlRow q = rlRows[i];
                for (int ramo = 0; ramo < 2; ramo++)
                {
                    bool serie = ramo == 0;
                    StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
                    row.Children.Add(NumCella(serie ? (i + 1).ToString(IC) : ""));
                    if (serie)
                    {
                        TextBox p1 = Cella(q.P1, 80, false, true), p2 = Cella(q.P2, 80, false, true);
                        p1.LostFocus += (s, e) => q.P1 = p1.Text; p2.LostFocus += (s, e) => q.P2 = p2.Text;
                        row.Children.Add(p1); row.Children.Add(p2);
                    }
                    else { row.Children.Add(Cella("", 80, true)); row.Children.Add(Cella("", 80, true)); }
                    row.Children.Add(Cella(serie ? "serie" : "parallelo", 80, true, true));
                    TextBox r = Cella(FN(serie ? q.Rs : q.Rp, 2), 70), l = Cella(FN(serie ? q.Ls : q.Lp, 3), 70), c = Cella(FN(serie ? q.Cs : q.Cp, 1), 80);
                    r.LostFocus += (s, e) => { double v = ParseNum(r.Text, 0); if (serie) q.Rs = v; else q.Rp = v; };
                    l.LostFocus += (s, e) => { double v = ParseNum(l.Text, 0); if (serie) q.Ls = v; else q.Lp = v; };
                    c.LostFocus += (s, e) => { double v = ParseNum(c.Text, 0); if (serie) q.Cs = v; else q.Cp = v; };
                    row.Children.Add(r); row.Children.Add(l); row.Children.Add(c);
                    if (!serie)
                    {
                        ComboBox lato = new ComboBox { Width = 90, ItemsSource = new[] { "porta 1", "porta 2" }, SelectedIndex = q.Lato == 1 ? 0 : 1 };
                        lato.SelectionChanged += (s, e) => q.Lato = lato.SelectedIndex == 0 ? 1 : 2;
                        row.Children.Add(lato);
                    }
                    else
                    {
                        row.Children.Add(new Border { Width = 90 });
                        Button del = BtnRiga("✕", "togli la rete", true);
                        del.Margin = new Thickness(10, 0, 0, 0);
                        del.Click += (s, e) => { rlRows.RemoveAt(ri); RidisegnaTabelle("retil"); };
                        row.Children.Add(del);
                    }
                    body.Children.Add(row);
                }
            }
            StackPanel nx = new StackPanel { Orientation = Orientation.Horizontal };
            nx.Children.Add(NumCella((rlRows.Count + 1).ToString(IC)));
            TextBox next = Cella("", 520, false, true);
            next.Classes.Add("next");
            next.Watermark = "next – scrivi qui la porta 1 per aggiungere una rete L (es. w9c)";
            next.LostFocus += (s, e) => { if (!string.IsNullOrWhiteSpace(next.Text)) { rlRows.Add(new RlRow { P1 = next.Text.Trim() }); RidisegnaTabelle("retil"); } };
            nx.Children.Add(next);
            body.Children.Add(nx);
            Button ap = Btn("Applica e ricalcola", true);
            ap.Click += (s, e) =>
            {
                List<ReteL> l = new List<ReteL>();
                foreach (RlRow r in rlRows)
                {
                    int t1, s1, t2, s2; string err;
                    if (!PulseToSeg(r.P1, out t1, out s1, out err)) { Status("Rete L, porta 1 " + r.P1 + ": " + err, true); return; }
                    if (!PulseToSeg(r.P2, out t2, out s2, out err)) { Status("Rete L, porta 2 " + r.P2 + ": " + err, true); return; }
                    l.Add(new ReteL { Tag1 = t1, Seg1 = s1, Tag2 = t2, Seg2 = s2, Rs = r.Rs, Ls = r.Ls * 1e-6, Cs = r.Cs * 1e-12, Rp = r.Rp, Lp = r.Lp * 1e-6, Cp = r.Cp * 1e-12, LatoParallelo = r.Lato });
                }
                model.RetiL = l;
                ApplicaERicalcola("reti L");
            };
            body.Children.Add(Nota("Due righe per rete: ramo serie tra le due porte e ramo parallelo sulla porta 1 o 2; in ogni ramo R, L e C sono in serie (L 0 = assente, C 0 = assente). " +
                "Il calcolo rifa' la rete a ogni frequenza (scheda NT); nel file .nec si scrive la NT alla frequenza del modello e la riga CM YA-NEC RETEL."));
            all.Children.Add(PannelloTesta("Reti L " + rlRows.Count, ap, "ramo serie e ramo parallelo (R, L, C)", body));

            // calcolatore delle reti di sfasamento (Lewallen/Lahlum) e dell'ibrido di Collins
            StackPanel calc = new StackPanel { Spacing = 3 };
            TextBox tf = Tb(FN(model.FreqMHz, 3), 80), tn = Tb("2", 50), tz0 = Tb("75", 60), tr = Tb("36", 60), tx = Tb("0", 60), tk = Tb("1", 60), tt = Tb("-90", 60);
            calc.Children.Add(Riga("Frequenza", tf, new TextBlock { Text = "MHz", VerticalAlignment = VerticalAlignment.Center }));
            calc.Children.Add(Riga("Elementi alimentati dalla rete (n)", tn, new TextBlock { Text = "Z0 delle linee lambda/4", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) }, tz0, new TextBlock { Text = "ohm", VerticalAlignment = VerticalAlignment.Center }));
            calc.Children.Add(Riga("Impedenza dell'elemento R + jX", tr, tx, new TextBlock { Text = "ohm", VerticalAlignment = VerticalAlignment.Center }));
            calc.Children.Add(Riga("Rapporto delle correnti k, sfasamento", tk, tt, new TextBlock { Text = "gradi (negativo = in ritardo)", VerticalAlignment = VerticalAlignment.Center }));
            TextBlock out1 = Nota("", Pal.Fore), out2 = Nota("", Pal.Fore);
            Button bc = Btn("Calcola la rete", true);
            bc.Click += (s, e) =>
            {
                double f = ParseNum(tf.Text, model.FreqMHz), z0 = ParseNum(tz0.Text, 75), r0 = ParseNum(tr.Text, 36), x0 = ParseNum(tx.Text, 0), k = ParseNum(tk.Text, 1), th = ParseNum(tt.Text, -90);
                int n = (int)Math.Max(1, Math.Round(ParseNum(tn.Text, 2)));
                double xs, xp;
                OggettiExtra.Lahlum(n, z0, r0, x0, k, th, out xs, out xp);
                out1.Text = "Rete L di Lewallen (formule di Lahlum, ON4UN eq. 11-3...11-8): ramo serie Xs = " + FN(xs, 2) + " ohm (" + OggettiExtra.Componente(xs, f) + "), ramo parallelo Xp = " + FN(xp, 2) +
                    " ohm (" + OggettiExtra.Componente(xp, f) + "). Verifica sulla tab. 11-1 di ON4UN (n 2, 75 ohm, 41 -j19,3, k 1, -90 gradi, 3,8 MHz): Xs 68,60, Xp -46,64.";
                double w = 2 * Math.PI * f * 1e6;
                out2.Text = "Ibrido di Collins (90 gradi, Z0 " + FN(z0, 0) + " ohm): XL = Z0 -> L = " + FN(z0 / w * 1e6, 2) + " uH; XC = 2 Z0 -> C = " + FN(1 / (w * 2 * z0) * 1e12, 0) + " pF; spire su T225-2 (AL 120): " +
                    FN(100 * Math.Sqrt(z0 / w * 1e6 / 120), 1) + ".";
            };
            calc.Children.Add(bc);
            calc.Children.Add(out1);
            calc.Children.Add(out2);
            calc.Children.Add(Nota("Fonti: R. Lewallen W7EL (rete L con le linee lambda/4 \"current forcing\"), F. Lahlum W1MK (formule), ON4UN Low-Band DXing cap. 11 (tab. 11-1...11-5), ARRL Antenna Book (ibrido di Collins). " +
                "I valori calcolati si inseriscono nella tabella sopra come ramo serie e ramo parallelo (parallelo sulla porta verso gli elementi)."));
            all.Children.Add(Pannello("Calcolatore delle reti di sfasamento", calc));
            return all;
        }
    }
}
