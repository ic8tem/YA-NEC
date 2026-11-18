// YA-NEC - linguetta "Oggetti inseriti" (fascione di sinistra, stile approvato): Sorgenti e Carichi con la notazione
// MMANA (w4c = filo 4 al centro, b inizio, e fine; anche w4:7 = segmento 7), Linee di trasmissione (TL) e Stack.
// Ogni tabella ha la riga "next" per aggiungere e i tre pulsanti di riga: + (nuova riga sotto), copia la riga sopra, x (togli).
// Stack: copie identiche riconosciute da sole; distanze DL6WU con le aperture dell'antenna base in spazio libero.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Fascione fasOggetti;
        ContentControl hostOggetti;
        StackInfo stackInfo;
        RunResult stackBaseRun;
        NecModel stackBaseFor;

        // ------------------------------------------------------------------------------------------------
        // piccoli costruttori comuni alle nuove linguette
        // ------------------------------------------------------------------------------------------------
        Border Pannello(string titolo, Control corpo, string sottotitolo = null)
        {
            StackPanel sp = new StackPanel { Spacing = 6 };
            StackPanel head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            head.Children.Add(new TextBlock { Text = titolo, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            if (!string.IsNullOrEmpty(sottotitolo))
                head.Children.Add(new TextBlock { Text = sottotitolo, FontSize = 12.5, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Bottom });
            sp.Children.Add(head);
            sp.Children.Add(corpo);
            return new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 0, 10), Child = sp
            };
        }

        DockPanel Riga(string etichetta, params Control[] c)
        {
            DockPanel d = new DockPanel { Margin = new Thickness(0, 1) };
            TextBlock l = new TextBlock { Text = etichetta, Width = 250, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore) };
            DockPanel.SetDock(l, Dock.Left);
            d.Children.Add(l);
            StackPanel h = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            foreach (Control x in c) h.Children.Add(x);
            d.Children.Add(h);
            return d;
        }

        // 1.3.0: avviso legato al risultato di un calcolo, da togliere al calcolo successivo
        static TextBlock Ris(TextBlock t) { t.Tag = "ris"; return t; }
        TextBlock Nota(string s, Color? c = null)
        {
            return new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Foreground = Pal.B(c ?? Pal.Muted), Margin = new Thickness(0, 4, 0, 0) };
        }

        static void StileScelta(Button b, bool on)
        {
            b.Background = Pal.B(on ? Pal.Accent : Pal.Pill);
            b.Foreground = Pal.B(on ? Pal.AccentFore : Pal.PillFore);
            b.FontWeight = on ? FontWeight.SemiBold : FontWeight.Normal;
        }

        // gruppo di pulsanti a pillola a scelta singola
        StackPanel Scelta(string[] voci, int sel, Action<int> cambia)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
            List<Button> bs = new List<Button>();
            for (int i = 0; i < voci.Length; i++)
            {
                int k = i;
                Button b = Btn(voci[i], false);
                b.BorderThickness = new Thickness(1);
                b.BorderBrush = Pal.B(Pal.PillBorder);
                StileScelta(b, i == sel);
                b.Click += (s, e) =>
                {
                    for (int j = 0; j < bs.Count; j++) StileScelta(bs[j], j == k);
                    cambia(k);
                };
                bs.Add(b);
                sp.Children.Add(b);
            }
            return sp;
        }

        // ---- celle in stile Excel (bozzetto approvato "Struttura divisa": griglia #D4D4D4, intestazioni grigie, cella attiva blu) ----
        static readonly Color XlGriglia = Color.Parse("#D4D4D4"), XlIntest = Color.Parse("#F3F3F3"), XlIntestTesto = Color.Parse("#444444");
        static readonly Color XlSelSfondo = Color.Parse("#DBE7FB"), XlSelTesto = Color.Parse("#1D4FA8");

        Button BtnRiga(string t, string tip, bool rosso = false)
        {
            Button b = new Button { Content = t };
            b.Classes.Add("rb");
            if (rosso) b.Classes.Add("del");
            ToolTip.SetTip(b, tip);
            return b;
        }

        static TextBox Cella(string v, double w, bool ro = false, bool sinistra = false)
        {
            TextBox t = new TextBox { Text = v, Width = w, FontFamily = Pal.Mono };
            t.Classes.Add("xl");
            if (ro) t.Classes.Add("ro");
            if (sinistra) t.Classes.Add("l");
            if (!ro) t.GotFocus += (s, e) => t.SelectAll();
            return t;
        }

        // intestazione di colonna: cella grigia con il bordo della griglia
        static Border Intest(string s, double w, bool prima = false)
        {
            return new Border
            {
                Width = w, Background = new SolidColorBrush(XlIntest), BorderBrush = new SolidColorBrush(XlGriglia),
                BorderThickness = new Thickness(prima ? 1 : 0, 1, 1, 1), Padding = new Thickness(6, 3),
                Child = new TextBlock { Text = s, FontSize = 12, Foreground = new SolidColorBrush(XlIntestTesto), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap }
            };
        }

        // intestazione vuota sopra i pulsanti di riga
        static TextBlock IntestRiga()
        {
            return new TextBlock { Text = "riga", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#6A7686")), Margin = new Thickness(10, 4, 0, 0) };
        }

        // numero di riga (colonna grigia a sinistra)
        static Border NumCella(string n, bool sel = false)
        {
            return new Border
            {
                Width = 30, Background = new SolidColorBrush(sel ? XlSelSfondo : XlIntest), BorderBrush = new SolidColorBrush(XlGriglia),
                BorderThickness = new Thickness(1, 0, 1, 1),
                Child = new TextBlock { Text = n, FontSize = 12, Foreground = new SolidColorBrush(sel ? XlSelTesto : Color.Parse("#666666")), TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };
        }

        // riquadro con titolo, pulsante "Applica e ricalcola" e suggerimento nella stessa riga (come nel bozzetto)
        Border PannelloTesta(string titolo, Button applica, string suggerimento, Control corpo)
        {
            StackPanel sp = new StackPanel { Spacing = 6 };
            WrapPanel head = new WrapPanel();
            head.Children.Add(new TextBlock { Text = titolo, FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            if (applica != null) head.Children.Add(applica);
            if (!string.IsNullOrEmpty(suggerimento))
                head.Children.Add(new TextBlock { Text = suggerimento, FontSize = 12.5, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0), TextWrapping = TextWrapping.Wrap });
            sp.Children.Add(head);
            sp.Children.Add(corpo);
            return new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8), Margin = new Thickness(0, 0, 0, 10), Child = sp
            };
        }

        // tabella in basso a destra nella Struttura 3D. 0.5.3 (IC8TEM 15/11/2026 20:15, "se sposti tutto questo nella zona in basso a destra
        // nella schermata principale andiamo meglio"; bozzetto approvato alle 20:19): tutti gli oggetti del modello con una fila di pillole
        // (Sorgenti, Carichi, Linee, Stack, Rastremazioni, Trasformatori, Reti L e di sfasamento, Balun) e la tabella scelta sotto;
        // la linguetta "Oggetti inseriti" non c'e' piu'
        double bassoLarghezza = 600;
        void MostraBasso(string k)
        {
            if (hostBasso == null) return;
            bassoCorrente = k;
            StackPanel sp = new StackPanel { Spacing = 6 };
            if (model == null) { hostBasso.Content = PannelloTesta("Oggetti del modello", null, "apri un modello", new TextBlock()); return; }
            string[][] voci = {
                new[] { "sorgenti", Lang.T("Sorgenti") + " " + model.Sources.Count },
                new[] { "carichi", Lang.T("Carichi") + " " + model.Loads.Count },
                new[] { "linee", Lang.T("Linee") + " " + model.Networks.Count },
                new[] { "stack", "Stack " + (stackInfo != null ? stackInfo.Copies.ToString(CultureInfo.InvariantCulture) : "0") },
                new[] { "rastremazioni", Lang.T("Rastremazioni") + " " + model.Rastremazioni.Count },
                new[] { "trasformatori", Lang.T("Trasformatori") + " " + model.Trasformatori.Count },
                new[] { "retil", Lang.T("Reti L e di sfasamento") + " " + model.RetiL.Count },
                new[] { "cavobalun", "Cavo e balun" } };
            string[] fumetti = {
                "Sorgenti di tensione o di corrente con la notazione MMANA (w4c).",
                "Carichi LC (trappole), R+jX e RLC serie, come in MMANA.",
                "Linee di trasmissione tra due punti (scheda TL del NEC), anche incrociate.",
                "Copie identiche della stessa antenna: distanze DL6WU dalle aperture dell'antenna base.",
                "Tabella dei tubi telescopici come il Taper wire set di MMANA.",
                "Trasformatori ideali tra due punti (come in EZNEC).",
                "Reti L (ramo serie e parallelo) e calcolatore Lewallen/Lahlum, ibrido di Collins.",
                "Calza del cavo e palo come fili NEC; confronto dei balun da 1,8 a 1296 MHz (1.2)." };
            StackPanel tit = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            tit.Children.Add(new TextBlock { Text = "Oggetti del modello", FontWeight = FontWeight.Bold, FontSize = 14, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center });
            tit.Children.Add(new TextBlock { Text = "un clic sceglie la tabella qui sotto", FontSize = 12.5, Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(tit);
            WrapPanel pil = new WrapPanel();
            for (int i = 0; i < voci.Length; i++)
            {
                string key = voci[i][0];
                Button b = Btn(voci[i][1], false);
                b.BorderThickness = new Thickness(1);
                b.BorderBrush = Pal.B(Pal.PillBorder);
                StileScelta(b, key == k);
                b.Margin = new Thickness(0, 0, 5, 5);
                ToolTip.SetTip(b, fumetti[i]);
                b.Click += (s, e) => MostraBasso(key);
                pil.Children.Add(b);
            }
            sp.Children.Add(pil);
            Control c;
            if (k == "carichi") c = PannelloCarichi(true);
            else if (k == "linee") { StackPanel lp = new StackPanel { Spacing = 8 }; lp.Children.Add(PannelloLinee(true)); lp.Children.Add(PannelloLineaAccordatore()); c = lp; }   // 1.2.0: bozzetto 11 B
            else if (k == "rastremazioni") c = PannelloRastremazioni(true);
            else if (k == "stack") c = PannelloStack();
            else if (k == "trasformatori") c = PannelloTrasformatori();
            else if (k == "retil") c = PannelloRetiL();
            else if (k == "cavobalun") c = PannelloApriGrande("Cavo e balun", "Calza del cavo e palo come fili NEC; confronto dei balun (corrente di modo comune, F/B, F/R medio, perdita, potenza a 1 kW) da 1,8 a 1296 MHz.", () => PannelloCavoBalun());
            else c = PannelloSorgenti(true);
            if (c is Border bc) bc.Margin = new Thickness(0);
            sp.Children.Add(c);
            hostBasso.Content = sp;
            LimitaNote(sp, bassoLarghezza);
        }

        /// <summary>Le note che vanno a capo restano dentro la zona in basso a destra (lo scorrimento orizzontale serve solo alle tabelle).</summary>
        static void LimitaNote(Control c, double w)
        {
            if (c == null || w <= 50) return;
            if (c is TextBlock t) { if (t.TextWrapping == TextWrapping.Wrap) t.MaxWidth = w - 30; return; }
            if (c is Panel p) { foreach (Control x in p.Children) LimitaNote(x, w); return; }
            if (c is Decorator d) { LimitaNote(d.Child as Control, w); return; }
            if (c is ContentControl cc) LimitaNote(cc.Content as Control, w);
        }

        // le tabelle possono essere aperte in due posti (Oggetti inseriti e Struttura 3D): si ridisegnano entrambi
        // 1.0.2: il ridisegno parte dopo l'evento in corso. Scrivendo nella riga "next" e cliccando subito "Applica e ricalcola", la
        // perdita del fuoco ridisegnava la tabella tra la pressione e il rilascio del mouse: il pulsante veniva sostituito e il primo
        // clic andava perso. Ora la riga entra subito nei dati, la tabella si ridisegna un attimo dopo e il clic arriva al pulsante.
        readonly HashSet<string> ridisegniInAttesa = new HashSet<string>();
        bool puntatoreGiu;
        void RidisegnaTabelle(string k)
        {
            if (!ridisegniInAttesa.Add(k)) return;
            DispatcherTimer tm = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            int giri = 0;
            tm.Tick += (s, e) =>
            {
                // aspetta il rilascio del tasto del mouse (al massimo 3 s)
                if (puntatoreGiu && ++giri < 100) return;
                tm.Stop();
                ridisegniInAttesa.Remove(k);
                if (fasOggetti != null && fasOggetti.Current == k) MostraOggetto(k);
                if (bassoCorrente == k) MostraBasso(k);
            };
            tm.Start();
        }

        static string FN(double v, int dec = 3) { return v.ToString("0." + new string('#', dec), CultureInfo.CurrentCulture); }

        // ------------------------------------------------------------------------------------------------
        // notazione MMANA dei punti sui fili
        // ------------------------------------------------------------------------------------------------
        List<GeomCard> FiliGw() { return model == null ? new List<GeomCard>() : model.Geometry.FindAll(g => g.Id == "GW"); }

        /// <summary>"w4c" -> tag e segmento NEC; null se non valido. forceOdd: con "c" il filo deve avere segmenti dispari.</summary>
        bool PulseToSeg(string p, out int tag, out int seg, out string err, bool forceOdd = false)
        {
            tag = 0; seg = 0; err = null;
            Match m = Regex.Match((p ?? "").Trim().ToLowerInvariant(), @"^w(\d+)\s*([bce]|:\s*\d+)?$");
            if (!m.Success) { err = "scrivere w + numero del filo + b, c, e (es. w4c) oppure w4:7"; return false; }
            int w = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            List<GeomCard> fl = FiliGw();
            if (w < 1 || w > fl.Count) { err = "il filo " + w + " non esiste (fili: " + fl.Count + ")"; return false; }
            GeomCard g = fl[w - 1];
            string pos = m.Groups[2].Success ? m.Groups[2].Value : "c";
            if (pos == "c" && forceOdd && g.I2 % 2 == 0)
            {
                g.I2++;
                if (g.Src != null && g.Src.Length > 1) g.Src[1] = g.I2.ToString(CultureInfo.InvariantCulture);
            }
            tag = g.I1;
            if (pos == "b") seg = 1;
            else if (pos == "e") seg = g.I2;
            else if (pos == "c") seg = (g.I2 + 1) / 2;
            else
            {
                seg = int.Parse(pos.Substring(1).Trim(), CultureInfo.InvariantCulture);
                if (seg < 1 || seg > g.I2) { err = "il filo " + w + " ha " + g.I2 + " segmenti"; return false; }
            }
            return true;
        }

        string SegToPulse(int tag, int seg)
        {
            List<GeomCard> fl = FiliGw();
            for (int i = 0; i < fl.Count; i++)
            {
                if (fl[i].I1 != tag) continue;
                int n = fl[i].I2;
                if (seg == 1 && n > 1) return "w" + (i + 1) + "b";
                if (seg == n && n > 1) return "w" + (i + 1) + "e";
                if (n % 2 == 1 && seg == (n + 1) / 2) return "w" + (i + 1) + "c";
                if (seg == 1) return "w" + (i + 1) + "c";
                return "w" + (i + 1) + ":" + seg;
            }
            return "tag" + tag + ":" + seg;
        }

        string SegDescr(string pulse)
        {
            int tag, seg; string err;
            List<GeomCard> fl = FiliGw();
            if (!PulseToSeg(pulse, out tag, out seg, out err)) return err ?? "";
            for (int i = 0; i < fl.Count; i++) if (fl[i].I1 == tag) return "filo " + (i + 1) + ", seg. " + seg + " di " + fl[i].I2;
            return "";
        }

        void ApplicaERicalcola(string cosa)
        {
            if (model == null) return;
            Registro.Scrivi("AZIONE", "oggetti inseriti: " + cosa);
            if (txtNec != null) txtNec.Text = model.ToNec();
            RefreshWiresTable();
            Calculate(true);
        }

        // ------------------------------------------------------------------------------------------------
        Control BuildOggettiPage()
        {
            fasOggetti = new Fascione();
            fasOggetti.Famiglia("Oggetti del modello");
            fasOggetti.Voce("sorgenti", "Sorgenti", false, "Sorgenti di tensione o di corrente con la notazione MMANA (w4c).");
            fasOggetti.Voce("carichi", "Carichi", false, "Carichi LC (trappole), R+jX e RLC serie, come in MMANA.");
            fasOggetti.Voce("linee", "Linee", false, "Linee di trasmissione tra due punti (scheda TL del NEC), anche incrociate.");
            fasOggetti.Voce("stack", "Stack", false, "Copie identiche della stessa antenna: distanze DL6WU dalle aperture dell'antenna base.");
            fasOggetti.Voce("rastremazioni", "Rastremazioni (tubi telescopici)", false, "Tabella dei tubi telescopici come il Taper wire set di MMANA: nel raggio del filo -1, -2...");
            fasOggetti.Voce("trasformatori", "Trasformatori", false, "Trasformatori ideali tra due punti (come in EZNEC).");
            fasOggetti.Voce("retil", "Reti L e di sfasamento", false, "Reti L (ramo serie e parallelo) e calcolatore Lewallen/Lahlum, ibrido di Collins.");
            fasOggetti.Voce("cavobalun", "Cavo e balun", false, "Calza del cavo e palo come fili NEC; confronto dei balun da 1,8 a 1296 MHz (corrente di modo comune, F/B, perdita).");   // 1.2.0: bozzetti VHF 10 rev. 2 e HF 5
            hostOggetti = new ContentControl();
            fasOggetti.Selected += k => MostraOggetto(k);
            DockPanel d = Fascione.Pagina(fasOggetti, hostOggetti);
            fasOggetti.Select("sorgenti");
            return d;
        }

        void MostraOggetto(string k)
        {
            if (hostOggetti == null) return;
            Control c;
            if (model == null) c = Pannello("Oggetti del modello", Nota("Apri prima un modello."));
            else if (k == "carichi") c = PannelloCarichi();
            else if (k == "linee") { StackPanel lp = new StackPanel(); lp.Children.Add(PannelloLinee()); lp.Children.Add(PannelloLineaAccordatore()); c = lp; }   // 1.2.0: bozzetto 11 B
            else if (k == "stack") c = PannelloStack();
            else if (k == "rastremazioni") c = PannelloRastremazioni();
            else if (k == "trasformatori") c = PannelloTrasformatori();
            else if (k == "retil") c = PannelloRetiL();
            else c = PannelloSorgenti();
            hostOggetti.Content = new ScrollViewer { Content = c, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        }

        void AggiornaOggetti()
        {
            MostraBasso(bassoCorrente);
            if (fasOggetti == null) return;
            if (model != null)
            {
                fasOggetti.SetCount("sorgenti", Lang.T("Sorgenti") + "   " + model.Sources.Count);
                fasOggetti.SetCount("carichi", Lang.T("Carichi") + "   " + model.Loads.Count);
                fasOggetti.SetCount("linee", Lang.T("Linee") + "   " + model.Networks.Count);
                fasOggetti.SetCount("rastremazioni", Lang.T("Rastremazioni (tubi telescopici)") + "   " + model.Rastremazioni.Count);
                fasOggetti.SetCount("trasformatori", Lang.T("Trasformatori") + "   " + model.Trasformatori.Count);
                fasOggetti.SetCount("retil", Lang.T("Reti L e di sfasamento") + "   " + model.RetiL.Count);
                fasOggetti.SetCount("stack", "Stack   " + (stackInfo != null ? stackInfo.Copies.ToString(CultureInfo.InvariantCulture) : "0"));
            }

        }

        // ------------------------------------------------------------------------------------------------
        // Sorgenti
        // ------------------------------------------------------------------------------------------------
        class SrcRow { public string Pulse = "", Tipo = "V"; public double Mag = 1, Ph; }
        List<SrcRow> srcRows;

        Control PannelloSorgenti(bool compatto = false)
        {
            if (srcRows == null || srcModel != model)
            {
                srcModel = model;
                srcRows = new List<SrcRow>();
                foreach (SourceSpec s in model.Sources)
                    srcRows.Add(new SrcRow { Pulse = SegToPulse(s.Tag, s.Seg), Mag = s.V.Magnitude, Ph = s.V.Phase * 180 / Math.PI, Tipo = model.CurrentSources ? "I" : "V" });
            }
            StackPanel body = new StackPanel { Spacing = 2 };
            StackPanel hd = new StackPanel { Orientation = Orientation.Horizontal };
            hd.Children.Add(Intest("", 30, true)); hd.Children.Add(Intest("PULSE", 110)); hd.Children.Add(Intest(model.CurrentSources ? "Corr. A" : "Volt. V", 90));
            hd.Children.Add(Intest("Phase dg", 90)); hd.Children.Add(Intest("Tipo", 60)); hd.Children.Add(Intest("→ segmento NEC", 210));
            bool colZ = model.CurrentSources || model.Sources.Count > 2;     // schiere: impedenza risultante di ogni elemento (bozzetto four-square)
            if (colZ) hd.Children.Add(Intest("Z risultante", 130));
            hd.Children.Add(IntestRiga());
            body.Children.Add(hd);
            List<TextBox[]> cells = new List<TextBox[]>();
            Maniglia man = new Maniglia(body, () => cells, null);     // 1.0.11: maniglia di riempimento
            for (int i = 0; i < srcRows.Count; i++)
            {
                int r = i;
                SrcRow sr = srcRows[i];
                StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(NumCella((i + 1).ToString(CultureInfo.InvariantCulture)));
                TextBox p = Cella(sr.Pulse, 110, false, true), v = Cella(FN(sr.Mag, 3), 90), ph = Cella(FN(sr.Ph, 1), 90);
                TextBox ty = Cella(sr.Tipo, 60);
                ty.TextAlignment = TextAlignment.Center;
                ToolTip.SetTip(ty, "V = sorgente di tensione, I = sorgente di corrente (come MMANA). Con una I tutte le sorgenti diventano a corrente.");
                ty.LostFocus += (s, e) => { string t = (ty.Text ?? "").Trim().ToUpperInvariant(); sr.Tipo = t == "I" ? "I" : "V"; ty.Text = sr.Tipo; };
                TextBox seg = Cella(SegDescr(sr.Pulse), 210, true, true);
                p.LostFocus += (s, e) => { sr.Pulse = p.Text; seg.Text = SegDescr(sr.Pulse); };
                v.LostFocus += (s, e) => { sr.Mag = ParseNum(v.Text, sr.Mag); };
                ph.LostFocus += (s, e) => { sr.Ph = ParseNum(ph.Text, sr.Ph); };
                row.Children.Add(p); row.Children.Add(v); row.Children.Add(ph); row.Children.Add(ty); row.Children.Add(seg);
                cells.Add(new[] { p, v, ph, ty });
                foreach (TextBox tm in new[] { p, v, ph, ty }) { TextBox tmm = tm; tmm.GotFocus += (s, e) => man.Attiva(tmm); }
                if (colZ)
                {
                    string zt = "";
                    int tg, sg; string er;
                    if (run != null && geo != null && PulseToSeg(sr.Pulse, out tg, out sg, out er))
                    {
                        try
                        {
                            int abs = geo.SegNo(tg, sg);
                            SourceResult q = run.Sources.Find(x => x.Segment == abs);
                            if (q != null) zt = FN(q.Z.Real, 1) + (q.Z.Imaginary >= 0 ? " +j" : " -j") + FN(Math.Abs(q.Z.Imaginary), 1);
                        }
                        catch { }
                    }
                    row.Children.Add(Cella(zt, 130, true));
                }
                Button add = BtnRiga("+", "nuova riga sotto"), cp = BtnRiga("⤓", "copia la riga sopra"), del = BtnRiga("✕", "togli la riga", true);
                add.Click += (s, e) => { srcRows.Insert(r + 1, new SrcRow { Pulse = sr.Pulse }); RidisegnaTabelle("sorgenti"); };
                cp.Click += (s, e) => { if (r > 0) { SrcRow a = srcRows[r - 1]; sr.Pulse = a.Pulse; sr.Mag = a.Mag; sr.Ph = a.Ph; sr.Tipo = a.Tipo; RidisegnaTabelle("sorgenti"); } };
                del.Click += (s, e) => { srcRows.RemoveAt(r); RidisegnaTabelle("sorgenti"); };
                add.Margin = new Thickness(10, 0, 2, 0);
                row.Children.Add(add); row.Children.Add(cp); row.Children.Add(del);
                body.Children.Add(row);
            }
            // riga "next"
            StackPanel nx = new StackPanel { Orientation = Orientation.Horizontal };
            nx.Children.Add(NumCella((srcRows.Count + 1).ToString(CultureInfo.InvariantCulture)));
            TextBox next = Cella("", 560, false, true);
            next.Classes.Add("next");
            next.Watermark = "next – scrivi qui per aggiungere (es. w3c)";
            next.LostFocus += (s, e) => { if (!string.IsNullOrWhiteSpace(next.Text)) { srcRows.Add(new SrcRow { Pulse = next.Text.Trim() }); next.Text = ""; RidisegnaTabelle("sorgenti"); } };
            nx.Children.Add(next);
            body.Children.Add(nx);

            CheckBox chkI = new CheckBox { Content = "Sorgenti a corrente (schiere fasate): il programma ricava le tensioni", IsChecked = model.CurrentSources, Margin = new Thickness(0, 6, 0, 0) };
            ToolTip.SetTip(chkI, "Tipo I di MMANA: i valori della tabella sono correnti (A e gradi); le tensioni che le producono si calcolano a ogni frequenza (V = Z I).");
            body.Children.Add(chkI);
            Button ap = Btn("Applica e ricalcola", true);
            ap.Click += (s, e) =>
            {
                List<SourceSpec> l = new List<SourceSpec>();
                foreach (SrcRow sr in srcRows)
                {
                    int tag, seg; string err;
                    if (!PulseToSeg(sr.Pulse, out tag, out seg, out err, true)) { Status("Sorgente " + sr.Pulse + ": " + err, true); return; }
                    l.Add(new SourceSpec { Tag = tag, Seg = seg, V = Complex.FromPolarCoordinates(sr.Mag, sr.Ph * Math.PI / 180),
                        Qds = model.Sources.Exists(q0 => q0.Tag == tag && q0.Seg == seg && q0.Qds) });   // EX 5 conservata
                }
                if (l.Count == 0) { Status("Serve almeno una sorgente.", true); return; }
                model.Sources = l;
                model.CurrentSources = chkI.IsChecked == true || srcRows.Exists(x => x.Tipo == "I");
                ApplicaERicalcola("sorgenti");
            };
            if (!compatto)
                body.Children.Add(Nota("PULSE come in MMANA: w + numero del filo + b inizio, c centro, e fine (es. w4c); w4:7 = segmento 7. La colonna grigia mostra il segmento NEC calcolato; " +
                    "sui fili con la sorgente al centro i segmenti diventano dispari da soli. Con le sorgenti a corrente i valori sono correnti (schiere fasate, four-square)."));
            return PannelloTesta(Lang.T("Sorgenti") + " " + srcRows.Count, ap, "PULSE come in MMANA: w + filo + b/c/e (w14c); colonna grigia = segmento NEC", body);
        }
        NecModel srcModel, ldModel, tlModel;

        // ------------------------------------------------------------------------------------------------
        // Carichi
        // ------------------------------------------------------------------------------------------------
        class LoadRow { public string Pulse = "", Type = "LC"; public double A, B, Q, F; public LoadSpec Orig; }
        List<LoadRow> ldRows;
        static readonly string[] LoadTypes = { "LC", "R+jX", "RLC serie", "S (Laplace)" };

        Control PannelloCarichi(bool compatto = false)
        {
            double f0 = ParseNum(txtFreq.Text, model.FreqMHz);
            if (ldRows == null || ldModel != model)
            {
                ldModel = model;
                ldRows = new List<LoadRow>();
                foreach (LoadSpec l in model.Loads)
                {
                    LoadRow r = new LoadRow { Pulse = SegToPulse(l.Tag, l.From), Orig = l };
                    if (l.Type == 1)
                    {
                        r.Type = "LC"; r.A = l.F2 * 1e6; r.B = l.F3 * 1e12;
                        if (l.F2 > 0 && l.F3 > 0) r.F = 1 / (2 * Math.PI * Math.Sqrt(l.F2 * l.F3)) / 1e6;
                        double fq = r.F > 0 ? r.F : f0;
                        if (l.F1 > 0 && l.F2 > 0) r.Q = l.F1 / (2 * Math.PI * fq * 1e6 * l.F2);
                    }
                    else if (l.Type == 4) { r.Type = "R+jX"; r.A = l.F1; r.B = l.F2; }
                    else if (l.Type == 0) { r.Type = "RLC serie"; r.A = l.F1; r.B = l.F2 * 1e6; r.Q = l.F3 * 1e12; }
                    else r.Type = "LD " + l.Type;
                    ldRows.Add(r);
                }
            }
            StackPanel body = new StackPanel { Spacing = 2 };
            StackPanel hd = new StackPanel { Orientation = Orientation.Horizontal };
            hd.Children.Add(Intest("", 30, true)); hd.Children.Add(Intest("PULSE", 80)); hd.Children.Add(Intest("Type", 110));
            hd.Children.Add(Intest("L uH / R / A0", 90)); hd.Children.Add(Intest("C pF / jX / B0", 90)); hd.Children.Add(Intest("Q / C pF (serie)", 90));
            hd.Children.Add(Intest("F MHz", 70)); hd.Children.Add(Intest("-> segmento NEC", 170)); hd.Children.Add(IntestRiga());
            body.Children.Add(hd);
            List<TextBox[]> cellsL = new List<TextBox[]>();
            Maniglia manL = new Maniglia(body, () => cellsL, null);     // 1.0.11: maniglia di riempimento
            for (int i = 0; i < ldRows.Count; i++)
            {
                int ri = i;
                LoadRow lr = ldRows[i];
                StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(NumCella((i + 1).ToString(CultureInfo.InvariantCulture)));
                TextBox p = Cella(lr.Pulse, 80, false, true);
                ComboBox ty = new ComboBox { ItemsSource = LoadTypes, Width = 110, FontSize = 12.5 };
                int ti = Array.IndexOf(LoadTypes, lr.Type);
                ty.SelectedIndex = ti;
                if (ti < 0) { ty.IsEnabled = false; ToolTip.SetTip(ty, "Carico NEC " + lr.Type + ": si conserva com'e'."); }
                TextBox a = Cella(FN(lr.A, 4), 90), b = Cella(FN(lr.B, 4), 90), q = Cella(lr.Q > 0 ? FN(lr.Q, 2) : "", 90), f = Cella(lr.F > 0 ? FN(lr.F, 4) : "", 70);
                TextBox seg = Cella(SegDescr(lr.Pulse), 170, true, true);
                p.LostFocus += (s, e) => { lr.Pulse = p.Text; seg.Text = SegDescr(lr.Pulse); };
                ty.SelectionChanged += (s, e) => { if (ty.SelectedIndex >= 0) lr.Type = LoadTypes[ty.SelectedIndex]; };
                a.LostFocus += (s, e) =>
                {
                    double old = lr.A;
                    lr.A = ParseNum(a.Text, lr.A);
                    // come MMANA: con la F indicata, cambiando L si ricalcola C per tenere la trappola a quella frequenza
                    if (lr.Type == "LC" && lr.F > 0 && lr.A > 0 && Math.Abs(old - lr.A) > 1e-12)
                    {
                        double w = 2 * Math.PI * lr.F * 1e6;
                        lr.B = 1 / (w * w * lr.A * 1e-6) * 1e12;
                        b.Text = FN(lr.B, 4);
                    }
                };
                b.LostFocus += (s, e) => { lr.B = ParseNum(b.Text, lr.B); };
                q.LostFocus += (s, e) => { lr.Q = ParseNum(q.Text, 0); };
                f.LostFocus += (s, e) => { lr.F = ParseNum(f.Text, 0); };
                row.Children.Add(p); row.Children.Add(ty); row.Children.Add(a); row.Children.Add(b); row.Children.Add(q); row.Children.Add(f); row.Children.Add(seg);
                cellsL.Add(new[] { p, null, a, b, q, f });
                foreach (TextBox tm in new[] { p, a, b, q, f }) { TextBox tmm = tm; tmm.GotFocus += (s, e) => manL.Attiva(tmm); }
                Button add = BtnRiga("+", "nuova riga sotto"), cp = BtnRiga("⤓", "copia la riga sopra"), del = BtnRiga("✕", "togli la riga", true);
                add.Click += (s, e) => { ldRows.Insert(ri + 1, new LoadRow { Pulse = lr.Pulse }); RidisegnaTabelle("carichi"); };
                cp.Click += (s, e) => { if (ri > 0) { LoadRow o = ldRows[ri - 1]; lr.Pulse = o.Pulse; lr.Type = o.Type; lr.A = o.A; lr.B = o.B; lr.Q = o.Q; lr.F = o.F; lr.Orig = null; RidisegnaTabelle("carichi"); } };
                del.Click += (s, e) => { ldRows.RemoveAt(ri); RidisegnaTabelle("carichi"); };
                add.Margin = new Thickness(10, 0, 2, 0);
                row.Children.Add(add); row.Children.Add(cp); row.Children.Add(del);
                body.Children.Add(row);
            }
            StackPanel nx = new StackPanel { Orientation = Orientation.Horizontal };
            nx.Children.Add(NumCella((ldRows.Count + 1).ToString(CultureInfo.InvariantCulture)));
            TextBox next = Cella("", 530, false, true);
            next.Classes.Add("next");
            next.Watermark = "next – scrivi qui per aggiungere (es. w2c)";
            next.LostFocus += (s, e) => { if (!string.IsNullOrWhiteSpace(next.Text)) { ldRows.Add(new LoadRow { Pulse = next.Text.Trim() }); next.Text = ""; RidisegnaTabelle("carichi"); } };
            nx.Children.Add(next);
            body.Children.Add(nx);
            CheckBox use = new CheckBox { Content = "Usa i carichi", IsChecked = model.UseLoads, Margin = new Thickness(0, 6, 0, 0) };
            body.Children.Add(use);
            Button ap = Btn("Applica e ricalcola", true);
            ap.Click += (s, e) =>
            {
                List<LoadSpec> l = new List<LoadSpec>();
                foreach (LoadRow r in ldRows)
                {
                    int tag, seg; string err;
                    if (!PulseToSeg(r.Pulse, out tag, out seg, out err)) { Status("Carico " + r.Pulse + ": " + err, true); return; }
                    if (r.Type == "LC")
                    {
                        double L = r.A * 1e-6, C = r.B * 1e-12;
                        if (r.F > 0 && L > 0 && C <= 0) { double w = 2 * Math.PI * r.F * 1e6; C = 1 / (w * w * L); }
                        double fq = r.F > 0 ? r.F : f0;
                        double R = r.Q > 0 && L > 0 ? r.Q * 2 * Math.PI * fq * 1e6 * L : 0;     // perdita della bobina come R in parallelo (Q * wL)
                        l.Add(new LoadSpec { Type = 1, Tag = tag, From = seg, To = seg, F1 = R, F2 = L, F3 = C });
                    }
                    else if (r.Type == "R+jX") l.Add(new LoadSpec { Type = 4, Tag = tag, From = seg, To = seg, F1 = r.A, F2 = r.B });
                    else if (r.Type == "RLC serie") l.Add(new LoadSpec { Type = 0, Tag = tag, From = seg, To = seg, F1 = r.A, F2 = r.B * 1e-6, F3 = r.Q * 1e-12 });
                    else if (r.Type.StartsWith("S")) { Status("Il carico S (Laplace) non e' ancora nel motore: riga " + r.Pulse + " non applicata.", true); return; }
                    else if (r.Orig != null) { LoadSpec o = r.Orig.Clone(); l.Add(o); }
                }
                model.Loads = l;
                model.UseLoads = use.IsChecked == true;
                ApplicaERicalcola("carichi");
            };
            if (!compatto)
                body.Children.Add(Nota("Type come in MMANA: LC = trappola L e C in parallelo (L in uH, C in pF, Q della bobina, F = frequenza di risonanza; con la F indicata, cambiando L il programma ricalcola C); " +
                "R+jX = impedenza fissa (ohm); RLC serie = R ohm, L uH, C pF in serie. S (Laplace) nella prossima versione. Il carico sta al centro del segmento indicato."));
            return PannelloTesta(Lang.T("Carichi") + " " + ldRows.Count, ap, "(L - uH; C - pF; R/jX - ohm) LC = trappola, R+jX, RLC serie", body);
        }

        // ------------------------------------------------------------------------------------------------
        // Linee di trasmissione (TL)
        // ------------------------------------------------------------------------------------------------
        class TlRow { public string P1 = "", P2 = ""; public double Z0 = 50, Len; public bool Cross; public NetworkSpec Orig; }
        List<TlRow> tlRows;

        Control PannelloLinee(bool compatto = false)
        {
            if (tlRows == null || tlModel != model)
            {
                tlModel = model;
                tlRows = new List<TlRow>();
                foreach (NetworkSpec n in model.Networks)
                    tlRows.Add(new TlRow { P1 = SegToPulse(n.Tag1, n.Seg1), P2 = SegToPulse(n.Tag2, n.Seg2), Z0 = Math.Abs(n.F1), Len = n.F2, Cross = n.F1 < 0, Orig = n.IsTl ? null : n });
            }
            StackPanel body = new StackPanel { Spacing = 2 };
            StackPanel hd = new StackPanel { Orientation = Orientation.Horizontal };
            hd.Children.Add(Intest("", 30, true)); hd.Children.Add(Intest("Da (PULSE)", 80)); hd.Children.Add(Intest("A (PULSE)", 80));
            hd.Children.Add(Intest("Z0 ohm", 70)); hd.Children.Add(Intest("Lungh. m (0 = distanza)", 110)); hd.Children.Add(Intest("Incrociata", 80));
            hd.Children.Add(Intest("distanza tra i punti", 130)); hd.Children.Add(IntestRiga());
            body.Children.Add(hd);
            List<TextBox[]> cellsT = new List<TextBox[]>();
            Maniglia manT = new Maniglia(body, () => cellsT, null);     // 1.0.11: maniglia di riempimento
            for (int i = 0; i < tlRows.Count; i++)
            {
                int ri = i;
                TlRow tr = tlRows[i];
                StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(NumCella((i + 1).ToString(CultureInfo.InvariantCulture)));
                TextBox p1 = Cella(tr.P1, 80, false, true), p2 = Cella(tr.P2, 80, false, true), z = Cella(FN(tr.Z0, 2), 70), le = Cella(FN(tr.Len, 4), 110);
                CheckBox cr = new CheckBox { IsChecked = tr.Cross, Width = 80, HorizontalAlignment = HorizontalAlignment.Center };
                TextBox dist = Cella(DistanzaPunti(tr.P1, tr.P2), 130, true);
                p1.LostFocus += (s, e) => { tr.P1 = p1.Text; dist.Text = DistanzaPunti(tr.P1, tr.P2); };
                p2.LostFocus += (s, e) => { tr.P2 = p2.Text; dist.Text = DistanzaPunti(tr.P1, tr.P2); };
                z.LostFocus += (s, e) => { tr.Z0 = ParseNum(z.Text, tr.Z0); };
                le.LostFocus += (s, e) => { tr.Len = ParseNum(le.Text, tr.Len); };
                cr.IsCheckedChanged += (s, e) => { tr.Cross = cr.IsChecked == true; };
                if (tr.Orig != null) { foreach (Control c in new Control[] { z, le, cr }) c.IsEnabled = false; ToolTip.SetTip(z, "Rete NT: si conserva com'e'."); }
                row.Children.Add(p1); row.Children.Add(p2); row.Children.Add(z); row.Children.Add(le); row.Children.Add(cr); row.Children.Add(dist);
                cellsT.Add(new[] { p1, p2, z, le });
                foreach (TextBox tm in new[] { p1, p2, z, le }) { TextBox tmm = tm; tmm.GotFocus += (s, e) => manT.Attiva(tmm); }
                Button add = BtnRiga("+", "nuova riga sotto"), cp = BtnRiga("⤓", "copia la riga sopra"), del = BtnRiga("✕", "togli la riga", true);
                add.Click += (s, e) => { tlRows.Insert(ri + 1, new TlRow { P1 = tr.P2 }); RidisegnaTabelle("linee"); };
                cp.Click += (s, e) => { if (ri > 0) { TlRow o = tlRows[ri - 1]; tr.Z0 = o.Z0; tr.Len = o.Len; tr.Cross = o.Cross; RidisegnaTabelle("linee"); } };
                del.Click += (s, e) => { tlRows.RemoveAt(ri); RidisegnaTabelle("linee"); };
                add.Margin = new Thickness(10, 0, 2, 0);
                row.Children.Add(add); row.Children.Add(cp); row.Children.Add(del);
                body.Children.Add(row);
            }
            StackPanel nx = new StackPanel { Orientation = Orientation.Horizontal };
            nx.Children.Add(NumCella((tlRows.Count + 1).ToString(CultureInfo.InvariantCulture)));
            TextBox next = Cella("", 550, false, true);
            next.Classes.Add("next");
            next.Watermark = "next – scrivi qui il primo punto per aggiungere una linea (es. w1c)";
            next.LostFocus += (s, e) => { if (!string.IsNullOrWhiteSpace(next.Text)) { tlRows.Add(new TlRow { P1 = next.Text.Trim() }); next.Text = ""; RidisegnaTabelle("linee"); } };
            nx.Children.Add(next);
            body.Children.Add(nx);
            Button ap = Btn("Applica e ricalcola", true);
            ap.Click += (s, e) =>
            {
                List<NetworkSpec> l = new List<NetworkSpec>();
                foreach (TlRow r in tlRows)
                {
                    if (r.Orig != null) { l.Add(r.Orig.Clone()); continue; }
                    int t1, s1, t2, s2; string err;
                    if (!PulseToSeg(r.P1, out t1, out s1, out err)) { Status("Linea, primo punto " + r.P1 + ": " + err, true); return; }
                    if (!PulseToSeg(r.P2, out t2, out s2, out err)) { Status("Linea, secondo punto " + r.P2 + ": " + err, true); return; }
                    if (r.Z0 <= 0) { Status("Linea " + r.P1 + "-" + r.P2 + ": Z0 deve essere positiva (per incrociarla usare la casella).", true); return; }
                    l.Add(new NetworkSpec { IsTl = true, Tag1 = t1, Seg1 = s1, Tag2 = t2, Seg2 = s2, F1 = r.Cross ? -r.Z0 : r.Z0, F2 = r.Len });
                }
                model.Networks = l;
                ApplicaERicalcola("linee");
            };
            if (!compatto)
                body.Children.Add(Nota("Linee ideali senza perdite (scheda TL del NEC-2): lunghezza 0 = distanza tra i centri dei due segmenti; 'Incrociata' = Z0 negativa (linea a fase invertita, come nelle LPDA). " +
                "Una linea piu' corta della distanza tra i punti non e' fisica: il programma la calcola ma conviene controllare. Perdite, fattore di velocita' e catalogo dei cavi nella prossima versione."));
            return PannelloTesta(Lang.T("Linee") + " " + tlRows.Count, ap, "linee ideali TL tra due punti; lunghezza 0 = distanza; Incrociata = Z0 negativa", body);
        }

        string DistanzaPunti(string a, string b)
        {
            if (geo == null) return "";
            int t1, s1, t2, s2; string err;
            if (!PulseToSeg(a, out t1, out s1, out err) || !PulseToSeg(b, out t2, out s2, out err)) return "";
            try
            {
                int i = geo.SegNo(t1, s1) - 1, j = geo.SegNo(t2, s2) - 1;
                double dx = geo.CenterX(i) - geo.CenterX(j), dy = geo.CenterY(i) - geo.CenterY(j), dz = geo.CenterZ(i) - geo.CenterZ(j);
                return FN(Math.Sqrt(dx * dx + dy * dy + dz * dz), 4) + " m";
            }
            catch { return ""; }
        }

        // ------------------------------------------------------------------------------------------------
        // Stack
        // ------------------------------------------------------------------------------------------------
        void AggiornaRiconoscimentoStack()
        {
            try { stackInfo = StackDetect.Detect(model); } catch { stackInfo = null; }
            if (btnStackAvviso != null)
            {
                btnStackAvviso.IsVisible = stackInfo != null;
                if (stackInfo != null) btnStackAvviso.Content = "Stack di " + stackInfo.Copies + " antenne: distanze...";
            }
            AggiornaOggetti();
        }

        void ApriStack()
        {
            // 0.5.3: gli oggetti sono in basso a destra nella Struttura 3D
            SelectTab(0);
            MostraBasso("stack");
        }

        Control PannelloStack()
        {
            StackPanel body = new StackPanel { Spacing = 4 };
            if (stackInfo == null)
            {
                body.Children.Add(Nota("Nel modello non ci sono copie identiche della stessa antenna. Il riconoscimento cerca fili elencati a gruppi uguali, " +
                    "ognuno uguale al primo ma spostato (stessi raggi e segmenti), come nei file di stack di MMANA, AutoEZ ed EZNEC.", Pal.Fore));
                body.Children.Add(Nota("Per creare uno stack da un'antenna sola: Rumore/GT/Stack > Distanze di stack, righe e colonne, pulsante \"Crea lo stack nel modello aperto\" (dalla 0.4.20)."));
                Button vai = Btn("Vai a Distanze di stack", false);
                vai.Click += (s, e) =>
                {
                    ApriRumoreGt("stack");   // 1.1.0: Distanze di stack sta in Rumore/GT/Stack
                };
                body.Children.Add(vai);
                return Pannello("Stack", body);
            }
            StackInfo si = stackInfo;
            double f = ParseNum(txtFreq.Text, model.FreqMHz), lam = 299.792458 / f;
            body.Children.Add(Nota("Riconosciute " + si.Describe() + ". Antenna base: fili 1-" + si.WiresPerCopy + "; polarizzazione " + (si.HorizontalPol ? "orizzontale" : "verticale") + ".", Pal.Fore));
            // aperture dell'antenna base in spazio libero (calcolate dal programma)
            TextBlock ape = Nota("Calcolo dell'antenna base in spazio libero...", Pal.Fore);
            body.Children.Add(ape);
            TextBox tv = Tb(double.IsNaN(si.SpacingV) ? "" : FN(si.SpacingV, 3), 90);
            TextBox th = Tb(double.IsNaN(si.SpacingH) ? "" : FN(si.SpacingH, 3), 90);
            TextBox tq = Tb(FN(si.BaseHeight, 2), 90);
            TextBlock lv = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) };
            TextBlock lh = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) };
            Action upd = () =>
            {
                double v = ParseNum(tv.Text, double.NaN), h = ParseNum(th.Text, double.NaN);
                lv.Text = double.IsNaN(v) ? "" : "m = " + FN(v / lam, 2) + " lambda";
                lh.Text = double.IsNaN(h) ? "" : "m = " + FN(h / lam, 2) + " lambda";
            };
            tv.LostFocus += (s, e) => upd();
            th.LostFocus += (s, e) => upd();
            upd();
            Button bv = Btn("DL6WU ...", false), bh = Btn("DL6WU ...", false);
            bv.IsEnabled = bh.IsEnabled = false;
            body.Children.Add(Riga("Righe (in verticale) x colonne", new TextBlock { Text = si.Rows + " x " + si.Cols + " = " + si.Copies + " antenne", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore) }));
            if (si.Rows > 1) body.Children.Add(Riga("Spaziatura verticale", tv, lv, bv));
            if (si.Cols > 1) body.Children.Add(Riga("Spaziatura orizzontale", th, lh, bh));
            body.Children.Add(Riga("Altezza dal suolo della riga in basso", tq, new TextBlock { Text = "m (al boom)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            Button ap = Btn("Applica e ricalcola", true);
            ap.Click += (s, e) =>
            {
                double v = ParseNum(tv.Text, double.NaN), h = ParseNum(th.Text, double.NaN), q = ParseNum(tq.Text, double.NaN);
                if ((!double.IsNaN(v) && v <= 0) || (!double.IsNaN(h) && h <= 0)) { Status("Le spaziature devono essere positive.", true); return; }
                StackDetect.Apply(model, si, v, h, model.Ground.Kind == GroundKind.FreeSpace ? double.NaN : q);
                Status("Stack ridisposto: verticale " + (double.IsNaN(v) ? "-" : FN(v, 3)) + " m, orizzontale " + (double.IsNaN(h) ? "-" : FN(h, 3)) + " m.");
                // 0.5.1: controllo k4 (spaziatura sotto meta' della DL6WU dell'antenna base, se gia' calcolata)
                if (stackBaseRun != null && stackBaseFor == model)
                {
                    List<ControlloDato> k4 = ControlliDati.Stack(v, h, StackDetect.Dl6wu(lam, stackBaseRun.HpbwEl), StackDetect.Dl6wu(lam, stackBaseRun.HpbwAz));
                    if (k4.Count > 0) { Status("⚠ " + k4[0].ToString(), true); foreach (ControlloDato cd in k4) Registro.Scrivi("CONTROLLI", cd.ToString()); }
                }
                ApplicaERicalcola("stack");
            };
            body.Children.Add(ap);
            body.Children.Add(Nota("Correzione approvata: la formula DL6WU D = lambda / (2 sin(a/2)) si applica con le aperture dell'antenna base in spazio libero calcolate dal programma. " +
                "Polarizzazione orizzontale: stack verticale -> apertura verticale, affiancate -> apertura orizzontale; polarizzazione verticale: i due piani si scambiano. " +
                "Con il MoM (Yagi 15 el. di Balanis) a circa 2 lambda i lobi secondari sono al minimo; oltre, il guadagno sale di qualche decimo ma i lobi peggiorano di 4-5 dB. " +
                "Le impedenze di ogni antenna nello stack sono 'impedenze attive' (riquadro Impedenza, una sorgente alla volta nella linguetta Avvisi)."));
            if (model.Ground.Kind == GroundKind.FreeSpace) body.Children.Add(Nota("Spazio libero: l'altezza dal suolo non si applica.", Pal.Orange));

            // calcolo delle aperture in background (una volta per modello e frequenza)
            NecModel mm = model;
            Task.Run(() =>
            {
                if (stackBaseRun != null && stackBaseFor == mm && Math.Abs(stackBaseRun.FreqMHz - f) < 1e-9) return stackBaseRun;
                NecModel b = StackDetect.BaseAntenna(mm, si);
                SegmentGeometry g = b.BuildGeometry();
                return Analysis.Run(b, g, f, 2.0);
            }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
            {
                if (t.IsFaulted) { ape.Text = "Antenna base: errore nel calcolo (" + t.Exception.GetBaseException().Message + ")"; return; }
                RunResult r = t.Result;
                stackBaseRun = r; stackBaseFor = mm;
                // aperture: piano orizzontale (azimut) e verticale (elevazione) dell'antenna base in spazio libero
                double ah = r.HpbwAz, av = r.HpbwEl;
                double dh = StackDetect.Dl6wu(lam, ah), dv = StackDetect.Dl6wu(lam, av);
                ape.Text = "Antenna base in spazio libero a " + FN(f, 3) + " MHz: " + FN(r.GmaxDbi, 2) + " dBi, apertura orizzontale " + FN(ah, 1) + " gradi, verticale " + FN(av, 1) +
                    " gradi -> DL6WU: sovrapposte " + FN(dv, 2) + " m (" + FN(dv / lam, 2) + " lambda), affiancate " + FN(dh, 2) + " m (" + FN(dh / lam, 2) + " lambda).";
                bv.Content = "DL6WU " + FN(dv, 2) + " m"; bh.Content = "DL6WU " + FN(dh, 2) + " m";
                bv.IsEnabled = !double.IsNaN(dv); bh.IsEnabled = !double.IsNaN(dh);
                bv.Click += (s, e) => { tv.Text = FN(dv, 3); upd(); };
                bh.Click += (s, e) => { th.Text = FN(dh, 3); upd(); };
            }));
            StackPanel tutto = new StackPanel { Spacing = 10 };
            tutto.Children.Add(Pannello("Stack", body, "l'antenna base resta una sola: le copie si spostano insieme"));
            tutto.Children.Add(PannelloApriGrande("Telaio a H e palo nello stack", "Montanti, traversa, palo e calze dei cavi di fase come fili NEC: confronto senza / con telaio (guadagno, F/B, F/R medio, lobi, Z del divisore, Tant e G/T).", () => PannelloTelaio(si)));   // 1.2.0: bozzetto VHF 4
            return tutto;
        }
    }
}
