// YA-NEC - scheda Loop della barra Geometria, alla lettera dal bozzetto approvato doc/bozzetto-loop.png ("approvato cosi'
// com'e', costruisci", IC8TEM 13/11/2026): forma, misura, spire e passo, conduttore (tubo o fascia, rame o alluminio), piano e
// base, segmenti, accordo, alimentazione; "Esito anticipato" NEC contro formule; Chiudi, Sostituisci il modello, Aggiungi
// al modello. Motore: Engine/LoopGen.cs.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
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
        readonly LoopSpec loopSpec = new LoopSpec();
        TextBox lpMisura, lpSpire, lpPasso, lpCond, lpBase, lpSeg, lpCap, lpK, lpLati;
        TextBlock lpSegNota, lpCapNota;
        Grid lpEsito;
        TextBlock lpEsitoNota;
        CancellationTokenSource lpCts;
        DispatcherTimer lpTimer;
        NecModel lpFinale;
        LoopEsito lpUltimo;

        Control LoopPannello()
        {
            if (model != null && loopSpec.FreqMHz == 7.1 && model.FreqMHz > 0) loopSpec.FreqMHz = ParseNum(txtFreq.Text, model.FreqMHz);
            StackPanel p = new StackPanel { Spacing = 4 };
            p.Children.Add(new TextBlock { Text = "Loop", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 4) });

            StackPanel forma = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            lpLati = LpTb(loopSpec.Lati.ToString(CultureInfo.InvariantCulture), 60);
            lpLati.IsVisible = loopSpec.Forma == 3;
            forma.Children.Add(Scelta(new[] { "cerchio", "quadrato", "ottagono", "N lati" }, loopSpec.Forma, k => { loopSpec.Forma = k; lpLati.IsVisible = k == 3; LpCambia(); }));
            forma.Children.Add(lpLati);
            p.Children.Add(GeoRiga("Forma", forma));

            StackPanel mis = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            lpMisura = LpTb(FmtU(loopSpec.Misura, "m"), 80);
            lpSpire = LpTb(loopSpec.Spire.ToString(CultureInfo.InvariantCulture), 60);
            lpPasso = LpTb(loopSpec.Spire > 1 ? FmtU(loopSpec.Passo, "m") : "-", 80);
            mis.Children.Add(lpMisura); mis.Children.Add(Lbl("spire")); mis.Children.Add(lpSpire); mis.Children.Add(Lbl("passo")); mis.Children.Add(lpPasso);
            mis.Children.Add(Nota("(piu' spire: elica sul cilindro)"));
            p.Children.Add(GeoRiga("Diametro / lato", mis));

            StackPanel con = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            lpCond = LpTb(FmtU(loopSpec.Conduttore * 1000, "mm"), 80);
            con.Children.Add(lpCond);
            con.Children.Add(Scelta(new[] { "tubo", "fascia larga" }, loopSpec.Fascia ? 1 : 0, k => { loopSpec.Fascia = k == 1; LpCambia(); }));
            con.Children.Add(Scelta(new[] { "rame", "alluminio" }, loopSpec.Alluminio ? 1 : 0, k => { loopSpec.Alluminio = k == 1; LpCambia(); }));
            p.Children.Add(GeoRiga("Conduttore", con));

            StackPanel pi = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            lpBase = LpTb(FmtU(loopSpec.Base, "m"), 80);
            pi.Children.Add(Scelta(new[] { "verticale XZ", "verticale YZ", "orizzontale" }, loopSpec.Piano, k => { loopSpec.Piano = k; LpCambia(); }));
            pi.Children.Add(Lbl("base a")); pi.Children.Add(lpBase);
            p.Children.Add(GeoRiga("Piano e centro", pi));

            StackPanel sg = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            lpSeg = LpTb(loopSpec.Segmenti > 0 ? loopSpec.Segmenti.ToString(CultureInfo.InvariantCulture) : "auto", 90);
            lpSegNota = Nota("");
            lpSegNota.VerticalAlignment = VerticalAlignment.Center; lpSegNota.Margin = new Thickness(0);
            sg.Children.Add(lpSeg); sg.Children.Add(lpSegNota);
            p.Children.Add(GeoRiga("Segmenti", sg));

            StackPanel ac = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            lpCap = LpTb(loopSpec.CondensatorePf > 0 ? FmtU(loopSpec.CondensatorePf, "pF") : "auto", 90);
            lpCapNota = Nota("");
            lpCapNota.VerticalAlignment = VerticalAlignment.Center; lpCapNota.Margin = new Thickness(0);
            ac.Children.Add(Scelta(new[] { "condensatore", "nessuno" }, loopSpec.Condensatore ? 0 : 1, k => { loopSpec.Condensatore = k == 0; LpCambia(); }));
            ac.Children.Add(lpCap); ac.Children.Add(lpCapNota);
            p.Children.Add(GeoRiga("Accordo", ac));

            StackPanel al = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            lpK = LpTb(loopSpec.Rapporto.ToString("0.##", CultureInfo.CurrentCulture), 60);
            al.Children.Add(Scelta(new[] { "anello di Faraday", "gamma", "diretta", "nessuna (anello passivo)" }, loopSpec.Alimentazione, k => { loopSpec.Alimentazione = k; LpCambia(); }));
            al.Children.Add(Lbl("D/")); al.Children.Add(lpK);
            p.Children.Add(GeoRiga("Alimentazione", al));

            // esito anticipato
            StackPanel es = new StackPanel { Spacing = 2 };
            StackPanel eh = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            eh.Children.Add(new TextBlock { Text = "Esito anticipato", FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            eh.Children.Add(new TextBlock { Text = "(NEC-2 calcolato ora, confrontato con il calcolatore Loop magnetica)", Foreground = Pal.B(Pal.Muted), FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center });
            es.Children.Add(eh);
            lpEsito = new Grid { ColumnDefinitions = new ColumnDefinitions("170,*,110,70") };
            es.Children.Add(lpEsito);
            lpEsitoNota = Nota("");
            es.Children.Add(lpEsitoNota);
            p.Children.Add(GeoBox(es));
            p.Children.Add(GeoBox(new TextBlock
            {
                Text = "Il loop si scrive come fili GW (tabella dei fili, Geometria, Ottimizzatore); condensatore come carico LD 0, anello di accoppiamento come filo a parte. Q alto: lo sweep va fatto stretto (pochi kHz).",
                TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), FontSize = 13
            }));
            StackPanel bt = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
            Button chiudi = Btn("Chiudi", false);
            chiudi.Click += (s, e) => GeoApri(null);
            Button sost = Btn("Sostituisci il modello", false);
            sost.Click += (s, e) => LpApplica(true);
            Button agg = Btn("Aggiungi al modello", true);
            agg.Click += (s, e) => LpApplica(false);
            bt.Children.Add(chiudi); bt.Children.Add(sost); bt.Children.Add(agg);
            p.Children.Add(bt);
            LpCambia();
            return new ScrollViewer
            {
                Content = new Border { Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12), Child = p },
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };
        }

        static string FmtU(double v, string u) { return v.ToString("0.###", CultureInfo.CurrentCulture) + " " + u; }

        TextBox LpTb(string t, double w)
        {
            TextBox b = new TextBox { Text = t, Width = w, Margin = new Thickness(2), FontFamily = Pal.Mono };
            b.TextChanged += (s, e) => LpCambia();
            return b;
        }

        // legge una misura con unita' (es. "1 m", "22 mm", "201,1 pF"); l'unita' di default e' quella indicata
        static double LpLeggi(TextBox t, double def, double scalaDefault)
        {
            string s = (t?.Text ?? "").Trim();
            if (s.Length == 0 || s == "-" || s.Equals("auto", StringComparison.OrdinalIgnoreCase)) return def;
            string q = s.Replace(" ", "");
            bool conUnita = System.Text.RegularExpressions.Regex.IsMatch(q, @"[A-Za-z]$");
            if (q.EndsWith("m", StringComparison.OrdinalIgnoreCase) && !q.EndsWith("mm", StringComparison.OrdinalIgnoreCase) && !q.EndsWith("cm", StringComparison.OrdinalIgnoreCase)) { q = q.Substring(0, q.Length - 1); conUnita = false; scalaDefault = 1; }
            double v = Expr.Eval(Expr.NormalizeDecimal(q), null);
            return conUnita ? v : v * scalaDefault;
        }

        void LpCambia()
        {
            if (lpEsito == null || lpSeg == null) return;
            try
            {
                loopSpec.Misura = LpLeggi(lpMisura, 1, 1);
                loopSpec.Spire = Math.Max(1, (int)Math.Round(LpLeggi(lpSpire, 1, 1)));
                loopSpec.Passo = LpLeggi(lpPasso, 0, 1);
                loopSpec.Conduttore = LpLeggi(lpCond, 0.022, 1e-3);
                loopSpec.Base = LpLeggi(lpBase, 3, 1);
                loopSpec.Segmenti = (int)Math.Round(LpLeggi(lpSeg, 0, 1));
                loopSpec.CondensatorePf = LpLeggi(lpCap, 0, 1) * ((lpCap.Text ?? "").ToLowerInvariant().Contains("pf") ? 1e12 : 1);
                loopSpec.Rapporto = LpLeggi(lpK, 5, 1);
                loopSpec.Lati = Math.Max(3, (int)Math.Round(LpLeggi(lpLati, 6, 1)));
                loopSpec.FreqMHz = model != null ? ParseNum(txtFreq.Text, model.FreqMHz) : ParseNum(txtFreq.Text, 7.1);
                if (loopSpec.Misura <= 0 || loopSpec.Conduttore <= 0) throw new NecException("misura e conduttore devono essere maggiori di zero");
                int auto, max;
                LoopGen.Segmentazione(loopSpec, out auto, out max);
                lpSegNota.Text = "(" + (loopSpec.Segmenti > 0 ? "" : "auto: " + auto + "; ") + "min 2,5 diametri: max " + max + ")";
                lpSegNota.Foreground = Pal.B(loopSpec.Segmenti > max ? Pal.Red : Pal.Muted);
                lpCapNota.Text = loopSpec.Condensatore ? "risonanza a " + Fmt(loopSpec.FreqMHz, 3) + " MHz" : "";
                // anteprima nella vista 3D
                int tc, sc;
                NecModel prev = LoopGen.Costruisci(loopSpec, null, false, 0, true, out tc, out sc);
                SegmentGeometry g = prev.BuildGeometry();
                List<double[]> segs = new List<double[]>();
                for (int i = 0; i < g.N; i++) segs.Add(new[] { g.X1[i], g.Y1[i], g.Z1[i], g.X2[i], g.Y2[i], g.Z2[i] });
                if (view3d != null) { view3d.Preview = segs; view3d.PreviewCenter = null; view3d.PreviewLabel = ""; view3d.PreviewCenterText = ""; view3d.InvalidateVisual(); }
                lpEsitoNota.Text = "calcolo in corso...";
                lpEsitoNota.Foreground = Pal.B(Pal.Muted);
            }
            catch (Exception ex)
            {
                lpEsitoNota.Text = ex.Message;
                lpEsitoNota.Foreground = Pal.B(Pal.Red);
                return;
            }
            if (lpTimer == null)
            {
                lpTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
                lpTimer.Tick += (s, e) => { lpTimer.Stop(); LpCalcola(); };
            }
            lpTimer.Stop(); lpTimer.Start();
        }

        async void LpCalcola()
        {
            if (lpCts != null) lpCts.Cancel();
            lpCts = new CancellationTokenSource();
            CancellationToken tok = lpCts.Token;
            LoopSpec s = loopSpec.Clone();
            try
            {
                NecModel fin = null;
                LoopEsito r;
                using (Prestazioni.Misura("Loop: esito anticipato", Fmt(s.Misura, 2) + " m, " + Fmt(s.FreqMHz, 3) + " MHz"))
                    r = await Task.Run(() => { NecModel f; LoopEsito x = LoopGen.Esito(s, tok, out f); fin = f; return x; });
                if (tok.IsCancellationRequested) return;
                lpFinale = fin; lpUltimo = r;
                LpMostra(r, s);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { lpEsitoNota.Text = "Errore NEC: " + ex.Message; lpEsitoNota.Foreground = Pal.B(Pal.Red); }
        }

        void LpMostra(LoopEsito r, LoopSpec s)
        {
            lpEsito.Children.Clear();
            lpEsito.RowDefinitions.Clear();
            List<string[]> righe = new List<string[]>
            {
                new[] { "", "NEC (" + (s.Segmenti > 0 ? s.Segmenti : r.Segmenti) + (s.Forma == 0 ? " lati" : " segmenti") + ")", "formule", "scarto" },
                Riga3("L", Fmt(r.L * 1e6, 2) + " µH", Fmt(r.Lf * 1e6, 2) + " µH", r.L, r.Lf),
                Riga3("C di accordo", r.C > 0 ? Fmt(r.C * 1e12, 1) + " pF" : "-", Fmt(r.Cf * 1e12, 0) + " pF", r.C, r.Cf),
                Riga3("R di radiazione", LpOhm(r.Rr), LpOhm(r.Rrf), r.Rr, r.Rrf),
                Riga3("R di perdita (" + (s.Alluminio ? "alluminio" : "rame") + ")", LpOhm(r.Rp), LpOhm(r.Rpf), r.Rp, r.Rpf),
                new[] { "Efficienza / guadagno", Fmt(r.Eff, 1) + " % / " + Fmt(r.Gdbi, 2) + " dBi", Fmt(r.Efff, 0) + " %", Math.Abs(r.Eff - r.Efff) < 2 ? "ok" : Fmt(r.Eff - r.Efff, 1) },
                Riga3("Q / banda a -3 dB", Fmt(r.Q, 0) + " / " + Fmt(r.Banda / 1000, 1) + " kHz", Fmt(r.Qf, 0), r.Q, r.Qf),
            };
            string alim = s.Alimentazione == 0 ? "Faraday D/" + Fmt(s.Rapporto, 1) : s.Alimentazione == 1 ? "Gamma D/" + Fmt(s.Rapporto, 1) : s.Alimentazione == 2 ? "Diretta" : "Anello passivo";
            if (s.Alimentazione == 3)
                righe.Add(new[] { alim, "perimetro " + Fmt(r.PerimetroLambda, 3) + " λ, nessuna sorgente", "", "" });
            else
                righe.Add(new[] { alim, string.Format(CultureInfo.CurrentCulture, "Z {0:0.0} {1}j{2:0.0}, ROS {3:0.00} (50 Ω)", r.Zfinale.Real, r.Zfinale.Imaginary < 0 ? "-" : "+", Math.Abs(r.Zfinale.Imaginary), r.Ros), "", "" });
            for (int i = 0; i < righe.Count; i++)
            {
                lpEsito.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int c = 0; c < 4; c++)
                {
                    string t = righe[i][c];
                    bool sc = c == 3 && i > 0;
                    Color col = i == 0 ? Pal.Muted : sc ? (t == "ok" || t.EndsWith(" %") && Math.Abs(ParseNum(t.Replace(" %", "").Replace("+", ""), 99)) <= 5 ? Pal.Green : Pal.Orange) : Pal.Fore;
                    TextBlock tb = new TextBlock { Text = t, FontFamily = i == 0 ? FontFamily.Default : Pal.Mono, FontSize = 13, Foreground = Pal.B(col), Margin = new Thickness(4, 2), TextWrapping = TextWrapping.Wrap, FontWeight = sc ? FontWeight.SemiBold : FontWeight.Normal };
                    Grid.SetRow(tb, i); Grid.SetColumn(tb, c);
                    if (i == righe.Count - 1 && c == 1) Grid.SetColumnSpan(tb, 3);
                    lpEsito.Children.Add(tb);
                }
            }
            string nota = r.Nota;
            if (s.Alimentazione < 2 && r.Ros > 2)
                nota += (nota.Length > 0 ? " " : "") + "Per 50 Ω cambia D/" + Fmt(s.Rapporto, 1) + " (anello o asta piu' piccoli: D/8-D/10 nella prova con nec2c) o regola con l'ottimizzatore.";
            if (!double.IsNaN(r.ScartoL))
                nota += (nota.Length > 0 ? " " : "") + (Math.Max(r.ScartoL, r.ScartoRr) > 2
                    ? string.Format(CultureInfo.CurrentCulture, "Convergenza: con il doppio dei segmenti L cambia del {0:0.0} % e Rr del {1:0.0} %: aumenta i segmenti (entro la regola dei 2,5 diametri).", r.ScartoL, r.ScartoRr)
                    : string.Format(CultureInfo.CurrentCulture, "Convergenza verificata: con il doppio dei segmenti L e Rr cambiano meno del 2 % ({0:0.0} e {1:0.0} %).", r.ScartoL, r.ScartoRr));
            if (s.Segmenti > r.MaxSegmenti) nota += (nota.Length > 0 ? " " : "") + "Segmenti oltre la regola dei 2,5 diametri: il NEC-2 perde precisione.";
            lpEsitoNota.Text = nota;
            lpEsitoNota.Foreground = Pal.B(r.Ros > 2 && s.Alimentazione < 2 ? Pal.Orange : Pal.Muted);
        }

        static string LpOhm(double r) { return Math.Abs(r) < 1 ? Fmt(r * 1000, 2) + " mΩ" : Fmt(r, 2) + " Ω"; }

        static string[] Riga3(string n, string a, string b, double va, double vb)
        {
            string sc = vb != 0 && !double.IsNaN(va) ? ((va - vb) / vb * 100).ToString("+0;-0;0", CultureInfo.CurrentCulture) + " %" : "";
            return new[] { n, a, b, sc };
        }

        void LpApplica(bool sostituisci)
        {
            if (lpUltimo == null) { Status("Aspetta la fine del calcolo dell'esito.", true); return; }
            try
            {
                int tc, sc;
                LoopSpec s = loopSpec.Clone();
                if (s.Alimentazione == 3 && (sostituisci || model == null))
                    Status("Anello passivo: senza sorgente il modello da solo non si calcola; di solito si aggiunge a un modello che ha gia' il radiatore.", true);
                NecModel m = LoopGen.Costruisci(s, sostituisci || model == null ? null : model, true, s.Condensatore ? lpUltimo.C : 0, true, out tc, out sc);
                if (sostituisci || model == null)
                {
                    m.Comments.Clear();
                    m.Comments.Add(string.Format(CultureInfo.InvariantCulture, "Loop {0} m, conduttore {1} mm, {2} MHz (YA-NEC, barra Geometria, scheda Loop)", s.Misura, s.Conduttore * 1000, s.FreqMHz));
                    m.FileName = "loop.nec";
                }
                Registro.Scrivi("AZIONE", "geometria: loop " + (sostituisci ? "al posto del modello" : "aggiunto al modello") + string.Format(CultureInfo.InvariantCulture, ", C {0:0.000} pF", lpUltimo.C * 1e12));
                if (model == null) { SetModel(m, null); Calculate(true); }
                else GeoMetti(m, true);
                GeoApri(null);
                Status("Loop " + (sostituisci ? "al posto del modello" : "aggiunto al modello") + ". Con un Q cosi' alto lo sweep va fatto su pochi kHz.");
            }
            catch (Exception ex) { Status("Loop: " + ex.Message, true); }
        }
    }
}
