// YA-NEC 1.2.0 - Calcolatori > Strumenti > "Meccanica" (bozzetto 9 approvato da IC8TEM il 17/11/2026, alla YagiStress di K7NV, applicato
// alla lettera): a sinistra parte, vento e lega a pillole; al centro il disegno della meta' elemento con i tubi telescopici e la freccia e il
// grafico della sollecitazione / snervamento con la zona di cedimento; a destra i risultati, le caselle e l'avviso giallo.
// Vento: NTC 2018 + CNR-DT 207 / EC1 (pillola del bozzetto rinominata come chiesto nelle specifiche). Calcolo in Engine/Meccanica.cs.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Controls;
using YaNec.Engine;

namespace YaNec
{
    /// <summary>Superficie di disegno con una funzione di disegno a richiesta (disegni e grafici delle pagine della 1.2).</summary>
    public class Disegno : Control
    {
        public Action<DrawingContext, Rect> Disegna;
        public override void Render(DrawingContext ctx)
        {
            Rect r = new Rect(0, 0, Bounds.Width, Bounds.Height);
            ctx.DrawRectangle(Pal.B(Pal.ChartBg), null, r);
            try { Disegna?.Invoke(ctx, r); } catch { }
        }
    }

    public partial class MainWindow
    {
        Control BuildMeccanicaPanel()
        {
            string parte = "elementi", vento = "ntc";
            int lega = 0;
            Dictionary<int, List<double>> spessori = new Dictionary<int, List<double>>();
            NecModel prima = null;      // modello prima della freccia (per togliere la freccia)

            // ---------------- sinistra ----------------
            Fascione fParte = new Fascione(); fParte.Famiglia("PARTE");
            fParte.Voce("elementi", "Elementi"); fParte.Voce("boom", "Boom"); fParte.Voce("tirante", "Tirante del boom (truss)");
            fParte.Voce("mast", "Staffe e mast"); fParte.Voce("area", "Area al vento totale");
            Fascione fVento = new Fascione(); fVento.Famiglia("VENTO");
            fVento.Voce("ntc", "NTC 2018 + CNR-DT 207 / EC1"); fVento.Voce("mano", "Velocità a mano"); fVento.Voce("ghiaccio", "Con ghiaccio");
            Fascione fLega = new Fascione(); fLega.Famiglia("LEGA");
            for (int i = 0; i < Meccanica.Leghe.Length; i++) fLega.Voce("lega" + i, Meccanica.Leghe[i].Nome);
            ComboBox cZona = new ComboBox { Width = 200, FontSize = 12.5 };
            List<string> zn = new List<string>(); foreach (Meccanica.Zona z in Meccanica.Zone) zn.Add("Zona " + z.N + " (" + (z.Regioni.Length > 26 ? z.Regioni.Substring(0, 26) + "…" : z.Regioni) + ")");
            cZona.ItemsSource = zn; cZona.SelectedIndex = 8;
            ToolTip.SetTip(cZona, "Zone del vento di NTC 2018, tabella 3.3.I: Zona 1 Valle d'Aosta, Piemonte, Lombardia, Trentino-Alto Adige, Veneto, Friuli-Venezia Giulia (tranne Trieste); 2 Emilia-Romagna; " +
                "3 Toscana, Marche, Umbria, Lazio, Abruzzo, Molise, Puglia, Campania, Basilicata, Calabria (tranne Reggio C.); 4 Sicilia e Reggio C.; 5 e 6 Sardegna (est e ovest della retta Capo Teulada - Isola di Maddalena); " +
                "7 Liguria; 8 Trieste; 9 isole minori e mare aperto.");
            ComboBox cCat = new ComboBox { ItemsSource = Meccanica.NomiCategorie, SelectedIndex = 0, Width = 60 };
            ToolTip.SetTip(cCat, "Categoria di esposizione del sito (NTC 2018, tab. 3.3.II e fig. 3.3.2): dipende dalla classe di rugosita' del terreno (A centri urbani ... D aree prive di ostacoli, mare) e dalla distanza dalla costa. I = costa aperta o mare, V = centro di citta'.");
            TextBox tLoc = Tb("Capri", 110), tQuota = Tb("80", 50), tAlt = Tb("12", 50), tVmano = Tb("130", 50);
            StackPanel ventoIn = new StackPanel { Spacing = 2, Margin = new Thickness(4, 2, 0, 6) };
            Func<string, Control, string, StackPanel> rg = (a, c, b) => { StackPanel p = new StackPanel { Orientation = Orientation.Horizontal }; p.Children.Add(new TextBlock { Text = a, Width = 74, VerticalAlignment = VerticalAlignment.Center, FontSize = 12.5, Foreground = Pal.B(Pal.Muted) }); if (c.Parent is Panel vecchio) vecchio.Children.Remove(c); p.Children.Add(c); if (b != null) p.Children.Add(new TextBlock { Text = b, VerticalAlignment = VerticalAlignment.Center, FontSize = 12.5, Margin = new Thickness(3, 0) }); return p; };
            StackPanel sx = new StackPanel { Width = 222, Spacing = 2 };
            sx.Children.Add(fParte); sx.Children.Add(fVento); sx.Children.Add(ventoIn); sx.Children.Add(fLega);
            sx.Children.Add(Nota("Tubi presi dalla tabella Rastremazioni: le misure sono le stesse del modello NEC."));

            // ---------------- centro ----------------
            ComboBox cEl = new ComboBox { MinWidth = 150, FontSize = 12.5 };
            TextBlock titDis = new TextBlock { FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            DockPanel testa = new DockPanel();
            DockPanel.SetDock(cEl, Dock.Right); testa.Children.Add(cEl); testa.Children.Add(titDis);
            Disegno dis = new Disegno { Height = 190 };
            StackPanel datiParte = new StackPanel { Spacing = 2 };
            StackPanel corpoDis = new StackPanel { Spacing = 6 };
            corpoDis.Children.Add(testa); corpoDis.Children.Add(dis); corpoDis.Children.Add(datiParte);
            Border pDis = new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 0, 10), Child = corpoDis };
            Disegno graf = new Disegno { Height = 210 };
            Border pGraf = Pannello("Sollecitazione", graf);
            TextBlock titGraf = (TextBlock)((StackPanel)((StackPanel)pGraf.Child).Children[0]).Children[0];
            StackPanel cen = new StackPanel(); cen.Children.Add(pDis); cen.Children.Add(pGraf);

            // ---------------- destra ----------------
            StackPanel ris = new StackPanel { Spacing = 2 };
            Border pRis = Pannello("Risultati (alla YagiStress, K7NV)", ris);
            CheckBox cFreccia = new CheckBox { Content = "Applica la freccia al modello NEC" };
            TextBlock tEff = new TextBlock { FontSize = 12.5, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(22, 0, 0, 2), TextWrapping = TextWrapping.Wrap };
            CheckBox cGh = new CheckBox { Content = "Ghiaccio radiale", VerticalAlignment = VerticalAlignment.Center };
            TextBox tGh = Tb("10", 56);
            CheckBox cSpina = new CheckBox { Content = "Spina interna nei giunti", IsChecked = true };
            FumettoGiallo(cSpina, "Spina (tubo o tondo) dentro il giunto telescopico.", "senza spina il tubo interno lavora sul bordo di quello esterno e si conta un fattore 1,25 sulla sollecitazione nei primi 5 cm: valore di stima, le fascette e le fessure cambiano molto il risultato.", "capitolo 15 (Meccanica)");
            StackPanel ghR = new StackPanel { Orientation = Orientation.Horizontal };
            ghR.Children.Add(cGh); ghR.Children.Add(tGh); ghR.Children.Add(new TextBlock { Text = "mm", VerticalAlignment = VerticalAlignment.Center });
            StackPanel casel = new StackPanel { Spacing = 2 };
            casel.Children.Add(cFreccia); casel.Children.Add(tEff); casel.Children.Add(ghR); casel.Children.Add(cSpina);
            casel.Children.Add(AvvisoGiallo("Calcolo di predimensionamento: per l'installazione vale il progetto di un tecnico abilitato."));
            Border pCas = new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Child = casel };
            StackPanel dx = new StackPanel(); dx.Children.Add(pRis); dx.Children.Add(pCas);

            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,10,1.5*,10,*"), Margin = new Thickness(4) };
            g.Children.Add(sx);
            Grid.SetColumn(cen, 2); g.Children.Add(cen);
            Grid.SetColumn(dx, 4); g.Children.Add(dx);

            // campi delle parti
            TextBox tSpess = Tb("", 220), tBoomD = Tb("50", 50), tBoomS = Tb("2", 40), tBoomL = Tb("", 56), tMastPos = Tb("", 56);
            TextBox tTirH = Tb("1", 50), tTirA = Tb("70", 44), tMastD = Tb("48", 50), tMastS = Tb("3", 40), tSbalzo = Tb("1", 50);
            ComboBox cAcc = new ComboBox { ItemsSource = new[] { "S235 (235 MPa)", "S275 (275 MPa)", "S355 (355 MPa)" }, SelectedIndex = 0 };
            ToolTip.SetTip(tSpess, "Spessori dei tubi dal boom alla punta (mm), separati da punto e virgola. Vuoto = spessori tipici per diametro (28 mm e oltre 2,0; 25 1,6; 18-22 1,2; 10-16 1,0).");

            List<Staffe.Elemento> elems = new List<Staffe.Elemento>();
            double fMod = 14.15;
            Action ricalcola = null;
            Func<double> vSito = () =>
            {
                Meccanica.Zona z = Meccanica.Zone[Math.Max(0, cZona.SelectedIndex)];
                double v = vento == "mano" ? ParseNum(tVmano.Text, 130) / 3.6 : Meccanica.VentoSito(z, Math.Max(0, cCat.SelectedIndex), ParseNum(tQuota.Text, 0), ParseNum(tAlt.Text, 10));
                return vento == "ghiaccio" ? v * 0.6 : v;
            };
            Func<double> ghiaccio = () => (cGh.IsChecked == true || vento == "ghiaccio") ? Math.Max(0, ParseNum(tGh.Text, 10)) / 1000 : 0;
            Func<int, List<Meccanica.Tubo>> tubiDi = i =>
            {
                List<double> sp; spessori.TryGetValue(i, out sp);
                return Meccanica.TubiElemento(elems[i].Lato, sp);
            };
            Action costruisciVento = () =>
            {
                foreach (Control vecchia in ventoIn.Children) (vecchia as Panel)?.Children.Clear();
                ventoIn.Children.Clear();   // 1.7.4: rg toglie le caselle dalla riga vecchia (prima: eccezione e chiusura alla seconda costruzione)
                if (vento == "mano") ventoIn.Children.Add(rg("Velocità", tVmano, "km/h"));
                else
                {
                    ventoIn.Children.Add(cZona);
                    ventoIn.Children.Add(rg("Categoria", cCat, null));
                    ventoIn.Children.Add(rg("Quota s.l.m.", tQuota, "m"));
                    ventoIn.Children.Add(rg("Antenna a", tAlt, "m dal suolo"));
                    ventoIn.Children.Add(rg("Località", tLoc, null));
                    if (vento == "ghiaccio") ventoIn.Children.Add(Nota("Con ghiaccio: vento ridotto a 0,6 volte e ghiaccio radiale della casella a destra (combinazione semplificata ISO 12494)."));
                }
            };
            Action leggiElementi = () =>
            {
                NecModel mb = prima ?? model;
                elems = mb != null ? Staffe.Trova(mb, mb.FreqMHz) : new List<Staffe.Elemento>();
                fMod = model != null ? model.FreqMHz : 14.15;
                elems.RemoveAll(e => Math.Abs(e.Uz) > 0.3);
                if (elems.Count > 1)
                {
                    Staffe.Elemento a = elems[0], z = elems[elems.Count - 1];
                    double ax = z.Cx - a.Cx, ay = z.Cy - a.Cy, az = z.Cz - a.Cz;
                    elems.Sort((p, q) => (p.Cx * ax + p.Cy * ay + p.Cz * az).CompareTo(q.Cx * ax + q.Cy * ay + q.Cz * az));
                }
                int ir = elems.FindIndex(e => e.Sorgente);
                int nd = 0;
                for (int i = 0; i < elems.Count; i++)
                    elems[i].Nome = elems[i].Sorgente ? "radiatore" : ir >= 0 && i < ir ? "riflettore" : "direttore " + (++nd);
                List<string> nomi = new List<string>(); foreach (Staffe.Elemento e in elems) nomi.Add(e.Nome);
                int sel = cEl.SelectedIndex;
                cEl.ItemsSource = nomi;
                cEl.SelectedIndex = elems.Count == 0 ? -1 : sel >= 0 && sel < elems.Count ? sel : 0;
            };

            // boom dal modello: asse lungo i centri degli elementi
            Func<Meccanica.Boom> boomDi = () =>
            {
                Meccanica.Boom b = new Meccanica.Boom { D = ParseNum(tBoomD.Text, 50) / 1000, S = ParseNum(tBoomS.Text, 2) / 1000 };
                if (elems.Count >= 2)
                {
                    Staffe.Elemento a = elems[0], z = elems[elems.Count - 1];
                    double ux = z.Cx - a.Cx, uy = z.Cy - a.Cy, uz = z.Cz - a.Cz, l = Math.Sqrt(ux * ux + uy * uy + uz * uz);
                    double sbalzo = 0.05;
                    b.Lunghezza = ParseNum(tBoomL.Text, l + 2 * sbalzo);
                    double ofs = (b.Lunghezza - l) / 2;
                    double gh = ghiaccio();
                    for (int i = 0; i < elems.Count; i++)
                    {
                        Staffe.Elemento e = elems[i];
                        double p = ((e.Cx - a.Cx) * ux + (e.Cy - a.Cy) * uy + (e.Cz - a.Cz) * uz) / l + ofs;
                        Meccanica.Esito es = Meccanica.Elemento(tubiDi(i), Meccanica.Leghe[lega], 0, gh, true);
                        b.Elementi.Add(new[] { p, 2 * (es.Massa + es.MassaGhiaccio), 2 * es.Area });
                    }
                    double sa = 0, sax = 0; foreach (double[] e in b.Elementi) { sa += e[2]; sax += e[2] * e[0]; }
                    b.PosMast = ParseNum(tMastPos.Text, sa > 0 ? sax / sa : b.Lunghezza / 2);
                }
                else { b.Lunghezza = ParseNum(tBoomL.Text, 6); b.PosMast = ParseNum(tMastPos.Text, b.Lunghezza / 2); }
                if (parte == "tirante") { b.AltezzaTirante = Math.Max(0.05, ParseNum(tTirH.Text, 1)); b.AttaccoTirante = Math.Max(0.05, Math.Min(1, ParseNum(tTirA.Text, 70) / 100)); }
                return b;
            };

            Action costruisciParte = () =>
            {
                foreach (Control vecchia in datiParte.Children) (vecchia as Panel)?.Children.Clear();   // 1.7.4: le caselle restano le stesse: si staccano dalla riga vecchia
                datiParte.Children.Clear();
                WrapPanel w = new WrapPanel();
                if (parte == "elementi") { w.Children.Add(Et("Spessori dei tubi dal boom (mm)")); w.Children.Add(tSpess); }
                else if (parte == "boom" || parte == "tirante")
                {
                    w.Children.Add(Et("Boom Ø")); w.Children.Add(tBoomD); w.Children.Add(Et("× s")); w.Children.Add(tBoomS); w.Children.Add(Et("mm  lungo")); w.Children.Add(tBoomL);
                    w.Children.Add(Et("m  mast a")); w.Children.Add(tMastPos); w.Children.Add(Et("m"));
                    if (parte == "tirante") { w.Children.Add(Et("montante")); w.Children.Add(tTirH); w.Children.Add(Et("m sopra il boom, attacco al")); w.Children.Add(tTirA); w.Children.Add(Et("% del mezzo boom")); }
                }
                else if (parte == "mast")
                {
                    w.Children.Add(Et("Mast Ø")); w.Children.Add(tMastD); w.Children.Add(Et("× s")); w.Children.Add(tMastS); w.Children.Add(Et("mm, acciaio")); w.Children.Add(cAcc);
                    w.Children.Add(Et("sbalzo sopra il cuscinetto")); w.Children.Add(tSbalzo); w.Children.Add(Et("m"));
                }
                datiParte.Children.Add(w);
                cEl.IsVisible = parte == "elementi";
                graf.IsVisible = parte != "area" && parte != "mast";
                pGraf.IsVisible = graf.IsVisible;
            };

            Func<string, string, string, bool, Border> card = (t, v, s, ev) =>
            {
                Border b = Riquadretto(t, v, s, ev);
                b.HorizontalAlignment = HorizontalAlignment.Stretch; b.MinWidth = 0;
                if (ev) foreach (Control c in ((StackPanel)b.Child).Children) if (c is TextBlock tb && tb.FontSize > 20) tb.Foreground = Pal.B(Pal.Accent);
                return b;
            };
            Func<double, string> kmh = v => CarichiHf.Fmt(v * 3.6, 0) + " km/h";
            Func<double, string> mm = v => CarichiHf.Fmt(v * 1000, v * 1000 < 10 ? 1 : 0);
            Func<Meccanica.Tubo, string> nomeTubo = t => "Ø" + mm(t.D) + "×" + CarichiHf.Fmt(t.S * 1000, 1);

            // grafico della sollecitazione: % dello snervamento lungo x, zona di cedimento oltre il 100 %
            Action<DrawingContext, Rect, double[], double[], string, double, string, double> disegnaSoll = (ctx, r, xs, rap, xlab, xMax, etMax, ymaxMin) =>
            {
                double ml = 46, mr = 30, mt = 10, mb = 34;
                Rect pr = new Rect(ml, mt, Math.Max(10, r.Width - ml - mr), Math.Max(10, r.Height - mt - mb));
                double ymax = 140; foreach (double v in rap) ymax = Math.Max(ymax, Math.Ceiling(v * 100 / 20) * 20 + 20);
                double x0 = xs[0], x1 = xs[xs.Length - 1]; if (x1 <= x0) x1 = x0 + 1;
                Func<double, double> PX = v => pr.Left + (v - x0) / (x1 - x0) * pr.Width;
                Func<double, double> PY = v => pr.Bottom - v / ymax * pr.Height;
                ctx.DrawRectangle(Pal.B(Color.FromArgb(70, 239, 68, 68)), null, new Rect(pr.Left, PY(ymax), pr.Width, PY(100) - PY(ymax)));
                Draw.TextR(ctx, "zona di cedimento", pr.Right - 6, PY(ymax) + 12, 11.5, Pal.Red);
                foreach (double v in new[] { 0.0, 50, 100, ymax })
                {
                    ctx.DrawLine(Pal.P(Pal.ChartGrid, 1), new Point(pr.Left, PY(v)), new Point(pr.Right, PY(v)));
                    Draw.TextR(ctx, CarichiHf.Fmt(v, 0) + (v == ymax ? " %" : ""), pr.Left - 4, PY(v) - 7, 10.5, Pal.ChartText);
                }
                double st = Draw.NiceStep(x1 - x0, 6);
                for (double v = Math.Ceiling(x0 / st) * st; v <= x1 + 1e-9; v += st)
                    Draw.TextC(ctx, CarichiHf.Fmt(v, st < 1 ? 1 : 0) + (v + st > x1 + 1e-9 ? " m" : ""), PX(v), pr.Bottom + 9, 10.5, Pal.ChartText);
                Draw.TextC(ctx, xlab, pr.Left + pr.Width / 2, pr.Bottom + 24, 11, Pal.ChartText);
                ctx.DrawLine(Pal.P(Pal.ChartText, 1), new Point(pr.Left, pr.Top), new Point(pr.Left, pr.Bottom));
                ctx.DrawLine(Pal.P(Pal.ChartText, 1), new Point(pr.Left, pr.Bottom), new Point(pr.Right, pr.Bottom));
                using (ctx.PushTransform(Matrix.CreateTranslation(12, pr.Top + pr.Height / 2) * Matrix.Identity))
                using (ctx.PushTransform(Matrix.CreateRotation(-Math.PI / 2)))
                    Draw.TextC(ctx, "sollecitazione / snervamento", 0, 0, 10.5, Pal.ChartText);
                IPen pen = Pal.P(Color.Parse("#1D6FE0"), 2);
                for (int i = 1; i < xs.Length; i++)
                    ctx.DrawLine(pen, new Point(PX(xs[i - 1]), PY(Math.Min(ymax, rap[i - 1] * 100))), new Point(PX(xs[i]), PY(Math.Min(ymax, rap[i] * 100))));
                if (!string.IsNullOrEmpty(etMax)) Draw.Text(ctx, etMax, PX(xMax) + 14, PY(Math.Min(ymax, ymaxMin * 100)) - 14, 11.5, Color.Parse("#1D6FE0"));
            };

            ricalcola = () =>
            {
                ris.Children.Clear();
                Meccanica.Lega lg = Meccanica.Leghe[lega];
                double v = vSito(), gh = ghiaccio();
                bool spina = cSpina.IsChecked == true;
                Meccanica.Zona zz = Meccanica.Zone[Math.Max(0, cZona.SelectedIndex)];
                string sito = vento == "mano" ? "a mano" : (string.IsNullOrWhiteSpace(tLoc.Text) ? "zona " + zz.N : tLoc.Text.Trim()) + ", cat. " + Meccanica.NomiCategorie[Math.Max(0, cCat.SelectedIndex)] + ", " + CarichiHf.Fmt(ParseNum(tQuota.Text, 0), 0) + " m s.l.m.";
                if (vento == "ghiaccio") sito += ", con ghiaccio";
                string banda = model != null ? (CarichiHf.BandaDi(model.FreqMHz * 1e6) ?? new Band("", 0, 0, 0)).Name : "";
                if (model == null || elems.Count == 0)
                {
                    titDis.Text = model == null ? "Apri un modello nella finestra principale" : "Nel modello non ci sono elementi orizzontali diritti";
                    dis.Disegna = null; graf.Disegna = null; dis.InvalidateVisual(); graf.InvalidateVisual();
                    ris.Children.Add(card("Vento di progetto (" + sito + ")", kmh(v), "pressione " + CarichiHf.Fmt(Meccanica.Pressione(v), 0) + " Pa", true));
                    return;
                }
                if (parte == "elementi")
                {
                    int ie = Math.Max(0, cEl.SelectedIndex);
                    List<Meccanica.Tubo> tubi = tubiDi(ie);
                    tSpess.Watermark = string.Join("; ", tubi.ConvertAll(t => CarichiHf.Fmt(t.S * 1000, 1)));
                    Meccanica.Esito es = Meccanica.Elemento(tubi, lg, v, gh, spina);
                    double vs = Meccanica.Sopravvivenza(tubi, lg, gh, spina);
                    Meccanica.Esito ecs = Meccanica.Elemento(tubi, lg, vs, gh, spina);
                    double L = tubi[tubi.Count - 1].A;
                    titDis.Text = "Elemento " + elems[ie].Nome + " " + banda + " · metà (" + CarichiHf.Fmt(L, 1) + " m) · " + tubi.Count + (tubi.Count == 1 ? " tubo" : " tubi telescopici");
                    double fr = es.Freccia[es.Freccia.Length - 1];
                    dis.Disegna = (ctx, r) =>
                    {
                        double x0 = 30, x1 = r.Width - 40, y0 = 34;
                        double k = (x1 - x0) / L, dmax = 0; foreach (Meccanica.Tubo t in tubi) dmax = Math.Max(dmax, t.D);
                        double ks = 22 / Math.Max(dmax, 1e-3);
                        ctx.DrawRectangle(Pal.B(Color.Parse("#1F2937")), null, new Rect(x0 - 10, y0 - 6, 10, ks * dmax + 12 + 12));
                        Draw.Text(ctx, "boom", x0 - 14, y0 - 22, 11, Pal.Fore);
                        foreach (Meccanica.Tubo t in tubi)
                        {
                            Rect tr = new Rect(x0 + t.Da * k, y0 + (ks * dmax - ks * t.D) / 2 + 6, t.L * k, ks * t.D);
                            ctx.DrawRectangle(Pal.B(Color.Parse("#CBD5E1")), Pal.P(Color.Parse("#475569"), 1), tr);
                            Draw.TextC(ctx, nomeTubo(t), tr.X + tr.Width / 2, tr.Bottom + 9, 10.5, Pal.ChartText);
                        }
                        double yl = y0 + ks * dmax + 48;
                        ctx.DrawLine(Pal.P(Pal.A(160, Pal.Muted), 1, Pal.Dash), new Point(x0, yl), new Point(x1, yl));
                        double sc = Math.Min(60 / Math.Max(Math.Abs(fr), 1e-4), 1500);
                        IPen pen = Pal.P(Color.Parse("#1D6FE0"), 2.2);
                        for (int i = 1; i < es.X.Length; i += 3)
                            ctx.DrawLine(pen, new Point(x0 + es.X[Math.Max(0, i - 3)] * k, yl + es.Freccia[Math.Max(0, i - 3)] * sc), new Point(x0 + es.X[i] * k, yl + es.Freccia[i] * sc));
                        Draw.TextR(ctx, "freccia in punta " + CarichiHf.Fmt(fr * 1000, 0) + " mm (peso proprio + ghiaccio " + CarichiHf.Fmt(gh * 1000, 0) + " mm)", x1, yl + fr * sc + 4, 11, Color.Parse("#1D6FE0"));
                    };
                    titGraf.Text = "Sollecitazione a " + kmh(v) + ", vento perpendicolare all'elemento";
                    graf.Disegna = (ctx, r) => disegnaSoll(ctx, r, es.X, es.Rapporto, "distanza dal boom", es.XMax, CarichiHf.Fmt(es.Max * 100, 0) + " % " + (es.XMax < 0.05 ? "al boom" : "a " + CarichiHf.Fmt(es.XMax, 2) + " m"), es.Max);
                    Meccanica.Tubo tc = tubi[ecs.TuboMax];
                    Border c1 = card("Vento di sopravvivenza", kmh(vs), "primo cedimento: tubo Ø" + mm(tc.D) + (ecs.XMax < 0.05 ? " al boom" : " a " + CarichiHf.Fmt(ecs.XMax, 2) + " m dal boom"), true);
                    FumettoGiallo(c1, "Vento perpendicolare all'elemento a cui il primo punto arriva allo snervamento (peso proprio e ghiaccio compresi).", "coefficiente di forza 1,2 psi_lambda (cilindro liscio sottocritico, CNR-DT 207 / EN 1991-1-4), pressione 0,5 rho v^2 con rho 1,25 kg/m3, nessun coefficiente di sicurezza: e' il vento di rottura, non quello di progetto.", "capitolo 15 (Meccanica)");
                    ris.Children.Add(c1);
                    double marg = (vs / Math.Max(v, 0.1) - 1) * 100;
                    Border c2 = card("Vento di progetto (" + sito + ")", kmh(v), "", false);
                    StackPanel c2p = (StackPanel)c2.Child; TextBlock sot = (TextBlock)c2p.Children[2];
                    sot.Inlines = new Avalonia.Controls.Documents.InlineCollection();
                    sot.Inlines.Add(new Avalonia.Controls.Documents.Run("margine " + CarichiHf.Fmt(marg, 0) + " %: "));
                    sot.Inlines.Add(new Avalonia.Controls.Documents.Run(marg < 10 ? "scarso" : marg < 25 ? "sufficiente" : "buono") { Foreground = Pal.B(marg < 10 ? Pal.Red : marg < 25 ? Pal.Orange : Pal.Green), FontWeight = FontWeight.SemiBold });
                    if (vento != "mano") FumettoGiallo(c2, "Velocita' di picco al sito: vb sqrt(ce), con vb = vb,0 ca (NTC 2018, periodo di ritorno 50 anni) e ce(z) della categoria di esposizione all'altezza dell'antenna.", "ct = 1 (terreno pianeggiante) e cd = 1; sui crinali e sulle scarpate il coefficiente di topografia ct alza il vento.", "capitolo 15 (Meccanica)");
                    ris.Children.Add(c2);
                    // area e peso dell'antenna (lista di taglio)
                    double area = 0, massa = 0;
                    for (int i = 0; i < elems.Count; i++) { Meccanica.Esito ei = Meccanica.Elemento(tubiDi(i), lg, 0, gh, spina); area += 2 * ei.Area; massa += 2 * (ei.Massa + ei.MassaGhiaccio); }
                    Meccanica.Boom bm = boomDi();
                    Meccanica.EsitoBoom eb = Meccanica.CalcolaBoom(bm, lg, v, gh);
                    area += eb.Area; massa += eb.Massa;
                    ris.Children.Add(card("Area al vento dell'antenna", CarichiHf.Fmt(area, 2) + " m²", "peso " + CarichiHf.Fmt(massa, 1) + " kg (lista di taglio)", false));
                    double vN, dM; int q;
                    Meccanica.Tubo rf = Meccanica.Rinforzo(tubi, lg, gh, spina, v, out vN, out dM, out q);
                    if (rf != null) ris.Children.Add(card("Proposta", nomeTubo(rf) + (rf.Da < 1e-6 ? " al boom" : " (tubo " + (q + 1) + ")"), "sopravvivenza " + kmh(vN) + ", " + (dM >= 0 ? "+" : "") + CarichiHf.Fmt(dM, 1) + " kg", false));
                    else ris.Children.Add(card("Proposta", Meccanica.Elemento(tubi, lg, v * 1.2, gh, spina).Max < 1 ? "nessun rinforzo" : "tubo piu' grosso", Meccanica.Elemento(tubi, lg, v * 1.2, gh, spina).Max < 1 ? "regge il vento di progetto con il 20 % di margine" : "nessun tubo commerciale fino a +12 mm basta: rivedi la rastremazione", false));
                }
                else if (parte == "boom" || parte == "tirante")
                {
                    Meccanica.Boom bm = boomDi();
                    if (string.IsNullOrWhiteSpace(tBoomL.Text)) tBoomL.Watermark = CarichiHf.Fmt(bm.Lunghezza, 2);
                    if (string.IsNullOrWhiteSpace(tMastPos.Text)) tMastPos.Watermark = CarichiHf.Fmt(bm.PosMast, 2);
                    Meccanica.EsitoBoom eb = Meccanica.CalcolaBoom(bm, lg, v, gh);
                    double lo = 0, hi = 150;
                    for (int i = 0; i < 50; i++) { double m = (lo + hi) / 2; if (Meccanica.CalcolaBoom(bm, lg, m, gh).Max >= 1) hi = m; else lo = m; }
                    double vs = Meccanica.CalcolaBoom(bm, lg, 0, gh).Max >= 1 ? 0 : (lo + hi) / 2;
                    titDis.Text = (parte == "tirante" ? "Boom con il tirante" : "Boom") + " Ø" + mm(bm.D) + "×" + CarichiHf.Fmt(bm.S * 1000, 1) + " · " + CarichiHf.Fmt(bm.Lunghezza, 2) + " m · " + elems.Count + " elementi";
                    dis.Disegna = (ctx, r) =>
                    {
                        double x0 = 30, x1 = r.Width - 30, yb = 110, k = (x1 - x0) / bm.Lunghezza;
                        ctx.DrawRectangle(Pal.B(Color.Parse("#CBD5E1")), Pal.P(Color.Parse("#475569"), 1), new Rect(x0, yb - 6, bm.Lunghezza * k, 12));
                        foreach (double[] e in bm.Elementi) { ctx.DrawLine(Pal.P(Pal.Fore, 2), new Point(x0 + e[0] * k, yb - 16), new Point(x0 + e[0] * k, yb + 16)); Draw.TextC(ctx, CarichiHf.Fmt(e[1], 1) + " kg", x0 + e[0] * k, yb + 26, 10.5, Pal.ChartText); }
                        double xm = x0 + bm.PosMast * k;
                        ctx.DrawRectangle(Pal.B(Color.Parse("#1F2937")), null, new Rect(xm - 4, yb + 6, 8, r.Height - yb - 6));
                        Draw.Text(ctx, "mast", xm + 8, r.Height - 18, 11, Pal.Fore);
                        if (bm.AltezzaTirante > 0)
                        {
                            double hpx = Math.Min(90, bm.AltezzaTirante * k);
                            Point top = new Point(xm, yb - 6 - hpx);
                            ctx.DrawLine(Pal.P(Pal.Fore, 3), new Point(xm, yb - 6), top);
                            double aL = bm.AttaccoTirante * bm.PosMast * k, aR = bm.AttaccoTirante * (bm.Lunghezza - bm.PosMast) * k;
                            ctx.DrawLine(Pal.P(Pal.Orange, 1.5), top, new Point(xm - aL, yb - 6));
                            ctx.DrawLine(Pal.P(Pal.Orange, 1.5), top, new Point(xm + aR, yb - 6));
                        }
                        Draw.Text(ctx, "equilibrio delle aree al vento: " + CarichiHf.Fmt(eb.PosEquilibrio, 2) + " m", x0, 8, 11, Color.Parse("#1D6FE0"));
                        ctx.DrawLine(Pal.P(Color.Parse("#1D6FE0"), 1, Pal.Dash), new Point(x0 + eb.PosEquilibrio * k, 24), new Point(x0 + eb.PosEquilibrio * k, yb - 8));
                    };
                    titGraf.Text = "Sollecitazione del boom a " + kmh(v) + ", vento perpendicolare al boom, pesi degli elementi";
                    graf.Disegna = (ctx, r) => disegnaSoll(ctx, r, eb.X, eb.Rapporto, "distanza dall'inizio del boom", eb.XMax, CarichiHf.Fmt(eb.Max * 100, 0) + " %", eb.Max);
                    ris.Children.Add(card("Vento di sopravvivenza del boom", kmh(vs), "punto piu' sollecitato a " + CarichiHf.Fmt(eb.XMax, 2) + " m", true));
                    ris.Children.Add(card("Vento di progetto (" + sito + ")", kmh(v), "margine " + CarichiHf.Fmt((vs / Math.Max(v, 0.1) - 1) * 100, 0) + " %", false));
                    ris.Children.Add(card("Momenti al mast", CarichiHf.Fmt(eb.MomentoV, 0) + " N m", "verticale (pesi); orizzontale " + CarichiHf.Fmt(eb.MomentoH, 0) + " N m (vento)", false));
                    if (parte == "tirante") ris.Children.Add(card("Tiro del tirante", CarichiHf.Fmt(eb.Tiro, 0) + " N", "per lato, teso per annullare il momento verticale al mast", false));
                    else ris.Children.Add(card("Peso del boom", CarichiHf.Fmt(eb.Massa, 1) + " kg", "elementi " + CarichiHf.Fmt(SommaMasse(bm), 1) + " kg", false));
                }
                else if (parte == "mast")
                {
                    Meccanica.Boom bm = boomDi();
                    Meccanica.EsitoBoom eb = Meccanica.CalcolaBoom(bm, lg, v, gh);
                    double area = eb.Area; foreach (double[] e in bm.Elementi) area += e[2];
                    double q = Meccanica.Pressione(v), F = q * 1.2 * area;
                    double fy = cAcc.SelectedIndex == 2 ? 355e6 : cAcc.SelectedIndex == 1 ? 275e6 : 235e6;
                    double sig = Meccanica.TensioneMast(F, Math.Max(0.05, ParseNum(tSbalzo.Text, 1)), ParseNum(tMastD.Text, 48) / 1000, ParseNum(tMastS.Text, 3) / 1000);
                    titDis.Text = "Staffe e mast · forza del vento " + CarichiHf.Fmt(F, 0) + " N a " + kmh(v);
                    dis.Disegna = (ctx, r) =>
                    {
                        double xm = r.Width / 2, top = 30, bot = r.Height - 20, sb = Math.Max(0.05, ParseNum(tSbalzo.Text, 1));
                        ctx.DrawRectangle(Pal.B(Color.Parse("#1F2937")), null, new Rect(xm - 5, top, 10, bot - top));
                        ctx.DrawRectangle(Pal.B(Color.Parse("#CBD5E1")), Pal.P(Color.Parse("#475569"), 1), new Rect(xm - 140, top - 6, 280, 12));
                        double yc = top + Math.Min(bot - top - 20, 110);
                        ctx.DrawRectangle(Pal.B(Pal.Orange), null, new Rect(xm - 18, yc, 36, 14));
                        Draw.Text(ctx, "cuscinetto / rotore (sbalzo " + CarichiHf.Fmt(sb, 2) + " m)", xm + 24, yc, 11, Pal.Fore);
                        ctx.DrawLine(Pal.P(Pal.Red, 2), new Point(xm - 120, top + 40), new Point(xm - 20, top + 40));
                        Draw.Text(ctx, "F = " + CarichiHf.Fmt(F, 0) + " N", xm - 120, top + 22, 11, Pal.Red);
                    };
                    Border c1 = card("Tensione nel mast al cuscinetto", CarichiHf.Fmt(sig / 1e6, 0) + " MPa", CarichiHf.Fmt(sig / fy * 100, 0) + " % dello snervamento dell'acciaio", true);
                    ris.Children.Add(c1);
                    ris.Children.Add(card("Momento al cuscinetto", CarichiHf.Fmt(F * Math.Max(0.05, ParseNum(tSbalzo.Text, 1)), 0) + " N m", "forza del vento x sbalzo", false));
                    ris.Children.Add(card("Coppia sulla staffa boom-mast", CarichiHf.Fmt(eb.Squilibrio, 0) + " N m", "squilibrio delle aree al vento; equilibrio a " + CarichiHf.Fmt(eb.PosEquilibrio, 2) + " m dall'inizio del boom", false));
                    ris.Children.Add(card("Area al vento (per il rotore)", CarichiHf.Fmt(area, 2) + " m²", CarichiHf.Fmt(area * 10.764, 1) + " ft²", false));
                    if (sig > fy) ris.Children.Add(AvvisoGiallo("Il mast supera lo snervamento: mast piu' grosso o sbalzo piu' corto."));
                }
                else
                {
                    // area al vento totale e lista di taglio
                    titDis.Text = "Area al vento totale e lista di taglio";
                    Grid tab = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto,Auto") };
                    string[] hh = { "parte", "tubo", "lunghezza (m)", "pezzi", "peso (kg)" };
                    int rr = 0;
                    Action<string[], bool> riga = (vv, h) =>
                    {
                        tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                        for (int j = 0; j < vv.Length; j++)
                        {
                            Border cb = new Border { Child = new TextBlock { Text = vv[j], FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal }, Padding = new Thickness(6, 3), Background = h ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 1, 1) };
                            Grid.SetColumn(cb, j); Grid.SetRow(cb, rr); tab.Children.Add(cb);
                        }
                        rr++;
                    };
                    riga(hh, true);
                    double area = 0, massa = 0;
                    for (int i = 0; i < elems.Count; i++)
                        foreach (Meccanica.Tubo t in tubiDi(i))
                        {
                            double m = Meccanica.Massa(t) * 2;
                            massa += m; area += 2 * (t.D + 2 * gh) * t.L;
                            riga(new[] { elems[i].Nome, nomeTubo(t), CarichiHf.Fmt(t.L, 3), "2", CarichiHf.Fmt(m, 2) }, false);
                        }
                    Meccanica.Boom bm = boomDi();
                    Meccanica.EsitoBoom eb = Meccanica.CalcolaBoom(bm, lg, v, gh);
                    area += eb.Area; massa += eb.Massa;
                    riga(new[] { "boom", "Ø" + mm(bm.D) + "×" + CarichiHf.Fmt(bm.S * 1000, 1), CarichiHf.Fmt(bm.Lunghezza, 3), "1", CarichiHf.Fmt(eb.Massa, 2) }, false);
                    foreach (Control vecchia in datiParte.Children) (vecchia as Panel)?.Children.Clear();
                    datiParte.Children.Clear(); datiParte.Children.Add(tab);
                    dis.Disegna = null; dis.Height = 0;
                    double F = Meccanica.Pressione(v) * 1.2 * area;
                    ris.Children.Add(card("Area al vento totale", CarichiHf.Fmt(area, 2) + " m²", CarichiHf.Fmt(area * 10.764, 1) + " ft² (per la scheda del rotore)", true));
                    ris.Children.Add(card("Peso", CarichiHf.Fmt(massa, 1) + " kg", "tubi di lega " + lg.Nome + (gh > 0 ? ", ghiaccio escluso" : ""), false));
                    ris.Children.Add(card("Forza del vento di progetto", CarichiHf.Fmt(F, 0) + " N", "a " + kmh(v) + " (" + sito + ")", false));
                }
                if (parte != "area") dis.Height = 190;
                dis.InvalidateVisual(); graf.InvalidateVisual();
            };

            Action tutto = () => { costruisciVento(); costruisciParte(); ricalcola(); };
            fParte.Selected += k => { parte = k; tutto(); };
            fVento.Selected += k => { vento = k; tutto(); };
            fLega.Selected += k => { lega = int.Parse(k.Substring(4), CultureInfo.InvariantCulture); ricalcola(); };
            cEl.SelectionChanged += (s, e) => { int i = cEl.SelectedIndex; tSpess.Text = i >= 0 && spessori.ContainsKey(i) ? string.Join("; ", spessori[i].ConvertAll(x => CarichiHf.Fmt(x * 1000, 1))) : ""; ricalcola(); };
            tSpess.LostFocus += (s, e) =>
            {
                int i = cEl.SelectedIndex; if (i < 0) return;
                List<double> l = new List<double>();
                foreach (string p in (tSpess.Text ?? "").Split(';')) { double x = ParseNum(p, -1); if (x > 0) l.Add(x / 1000); }
                if (l.Count > 0) spessori[i] = l; else spessori.Remove(i);
                ricalcola();
            };
            foreach (TextBox t in new[] { tLoc, tQuota, tAlt, tVmano, tGh, tBoomD, tBoomS, tBoomL, tMastPos, tTirH, tTirA, tMastD, tMastS, tSbalzo }) t.LostFocus += (s, e) => ricalcola();
            foreach (ComboBox c in new[] { cZona, cCat, cAcc }) c.SelectionChanged += (s, e) => ricalcola();
            cGh.IsCheckedChanged += (s, e) => ricalcola();
            cSpina.IsCheckedChanged += (s, e) => ricalcola();
            cFreccia.IsCheckedChanged += async (s, e) =>
            {
                if (model == null || elems.Count == 0) { cFreccia.IsChecked = false; return; }
                if (cFreccia.IsChecked == true)
                {
                    prima = model;
                    double gh = ghiaccio();
                    Dictionary<Staffe.Elemento, Meccanica.Esito> fr = new Dictionary<Staffe.Elemento, Meccanica.Esito>();
                    for (int i = 0; i < elems.Count; i++) fr[elems[i]] = Meccanica.Elemento(tubiDi(i), Meccanica.Leghe[lega], 0, gh, true);
                    List<string> note = new List<string>();
                    NecModel nm = Meccanica.ApplicaFreccia(model, elems, (el, x) =>
                    {
                        Meccanica.Esito es = fr[el]; double[] xs = es.X;
                        if (x >= xs[xs.Length - 1]) return es.Freccia[xs.Length - 1];
                        int k = (int)(x / xs[xs.Length - 1] * (xs.Length - 1));
                        double u = (x - xs[k]) / Math.Max(1e-12, xs[k + 1] - xs[k]);
                        return es.Freccia[k] + (es.Freccia[k + 1] - es.Freccia[k]) * u;
                    }, note);
                    nm.FileName = model.FileName;
                    tEff.Text = "effetto: calcolo in corso…";
                    NecModel m0 = model; double f = model.FreqMHz;
                    string eff;
                    try
                    {
                        eff = await Task.Run(() =>
                        {
                            RunResult a0 = Analysis.Run(m0, m0.BuildGeometry(), f, 5), a1 = Analysis.Run(nm, nm.BuildGeometry(), f, 5);
                            RunResult b0 = Analysis.Run(m0, m0.BuildGeometry(), f * 1.005, 5);
                            double dxdf = (b0.Zin.Imaginary - a0.Zin.Imaginary) / (f * 0.005 * 1000);   // ohm / kHz
                            double df = Math.Abs(dxdf) > 1e-9 ? -(a1.Zin.Imaginary - a0.Zin.Imaginary) / dxdf : 0;
                            return "effetto: risonanza " + (df >= 0 ? "+" : "") + CarichiHf.Fmt(df, 0) + " kHz, guadagno " + (a1.GmaxDbi - a0.GmaxDbi >= 0 ? "+" : "") + CarichiHf.Fmt(a1.GmaxDbi - a0.GmaxDbi, 2) + " dB";
                        });
                    }
                    catch (Exception ex) { eff = "effetto non calcolato: " + ex.Message; }
                    tEff.Text = eff;
                    GeoMetti(nm, true);
                    Status("Freccia applicata al modello (Annulla nella barra Geometria la toglie). " + eff + (note.Count > 0 ? " " + string.Join(" ", note) : ""));
                }
                else if (prima != null)
                {
                    GeoMetti(prima, true); prima = null; tEff.Text = "";
                    leggiElementi(); ricalcola();
                    Status("Freccia tolta dal modello.");
                }
            };
            g.AttachedToVisualTree += (s, e) => { leggiElementi(); tutto(); };
            fParte.Select("elementi", false); fVento.Select("ntc", false); fLega.Select("lega0", false);
            leggiElementi();
            tutto();
            return new ScrollViewer { Content = g, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        }

        static double SommaMasse(Meccanica.Boom b) { double m = 0; foreach (double[] e in b.Elementi) m += e[1]; return m; }
    }
}
