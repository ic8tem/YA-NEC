// YA-NEC 1.7.1 - bozzetto «quote cliccabili e distanza di stack in diretta» approvato da IC8TEM il 26/11/2026 06:17.
// 1) Struttura 3D: clic su una quota (9,00 m), su «h = ...» o su «Δ = ...» della linea di quota: si scrive il valore nuovo.
//    h o la quota del livello piu' basso spostano tutta l'antenna; Δ o la quota di un livello piu' alto spostano quel livello e
//    quelli sopra. Il modello si ricalcola e «Annulla» della barra Geometria torna indietro.
// 2) Diagrammi: se il modello e' uno stack di antenne uguali (stesso riconoscimento della scheda Stack), riquadro in alto a destra
//    con la distanza (− / + / cursore): i diagrammi si ricalcolano in diretta, il lobo di partenza resta tratteggiato (risultato fissato).
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        // ------------------------------------------------------------------------------------------------
        // 1) quote cliccabili
        // ------------------------------------------------------------------------------------------------
        void QuotaCliccata3D(int k, bool intervallo, double valore, Point pt)
        {
            if (model == null) return;
            List<double> lv = new List<double>(view3d.LivelliQuote);
            if (k < 1 || k >= lv.Count) return;
            string titolo = intervallo ? (k == 1 ? Lang.T("h: quota dal suolo") : Lang.F("Δ tra i livelli {0} e {1}", k - 1, k)) : Lang.F("Quota del livello {0}", k);
            string spiega = k == 1 ? Lang.T("sposta tutta l'antenna") : Lang.T("sposta questo livello e quelli sopra");
            TextBox tb = new TextBox { Text = Fmt(valore, 2), Width = 90, HorizontalContentAlignment = HorizontalAlignment.Right };
            TextBlock err = new TextBlock { Foreground = Pal.B(Pal.Red), FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxWidth = 260, IsVisible = false };
            StackPanel riga = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            riga.Children.Add(tb); riga.Children.Add(new TextBlock { Text = "m", VerticalAlignment = VerticalAlignment.Center });
            StackPanel sp = new StackPanel { Spacing = 4, Margin = new Thickness(4) };
            sp.Children.Add(new TextBlock { Text = titolo, FontWeight = FontWeight.SemiBold });
            sp.Children.Add(riga);
            sp.Children.Add(new TextBlock { Text = spiega + " · " + Lang.T("Invio applica, Esc chiude"), FontSize = 11.5, Foreground = Pal.B(Pal.Muted) });
            sp.Children.Add(err);
            Flyout fl = new Flyout { Content = sp, Placement = PlacementMode.Pointer };
            tb.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { fl.Hide(); e.Handled = true; return; }
                if (e.Key != Key.Enter) return;
                e.Handled = true;
                double nuovo = ParseNum(tb.Text, double.NaN);
                if (double.IsNaN(nuovo)) { err.Text = Lang.T("Scrivi un numero."); err.IsVisible = true; return; }
                double dz = nuovo - valore;
                if (Math.Abs(dz) < 1e-6) { fl.Hide(); return; }
                string msg;
                NecModel m = SpostaLivelli(model, lv, k, dz, out msg);
                if (m == null) { err.Text = msg; err.IsVisible = true; return; }
                fl.Hide();
                Registro.Scrivi("AZIONE", string.Format(CultureInfo.InvariantCulture, "struttura 3D: {0} livello {1} da {2:0.###} a {3:0.###} m", intervallo ? "intervallo" : "quota", k, valore, nuovo));
                GeoMetti(m, true);
                Status(Lang.F("{0}: {1} → {2} m. «Annulla» della barra Geometria torna indietro.", titolo, Fmt(valore, 2), Fmt(nuovo, 2)));
            };
            fl.Opened += (s, e) => Dispatcher.UIThread.Post(() => { tb.Focus(); tb.SelectAll(); });
            fl.ShowAt(view3d, true);
        }

        /// <summary>Sposta in verticale di dz i fili del livello k e di quelli sopra (k = 1: tutta l'antenna sopra il suolo).
        /// null se non si puo' (un filo attraversa la quota di taglio, schede diverse da GW, fili sotto il suolo).</summary>
        static NecModel SpostaLivelli(NecModel m0, List<double> lv, int k, double dz, out string msg)
        {
            msg = null;
            if (!TrasformaGeometria.SoloFili(m0)) { msg = Lang.T("Nel modello ci sono archi, eliche o simmetrie (GA, GH, GM, GR, GX): sposta con la barra Geometria."); return null; }
            SegmentGeometry g = m0.BuildGeometry();
            double taglio = k == 1 ? 1e-3 : (lv[k - 1] + lv[k]) / 2;
            Dictionary<int, double[]> fili = new Dictionary<int, double[]>();   // tag -> zmin, zmax
            for (int i = 0; i < g.N; i++)
            {
                double a = Math.Min(g.Z1[i], g.Z2[i]), b = Math.Max(g.Z1[i], g.Z2[i]);
                double[] q;
                if (!fili.TryGetValue(g.Tag[i], out q)) fili[g.Tag[i]] = new[] { a, b };
                else { q[0] = Math.Min(q[0], a); q[1] = Math.Max(q[1], b); }
            }
            HashSet<int> tags = new HashSet<int>();
            double zminSpostati = double.MaxValue;
            foreach (KeyValuePair<int, double[]> f in fili)
            {
                if (f.Value[0] >= taglio) { tags.Add(f.Key); zminSpostati = Math.Min(zminSpostati, f.Value[0]); }
                else if (f.Value[1] > taglio + 1e-6 && k > 1) { msg = Lang.F("Il filo {0} attraversa la quota di taglio ({1} m): sposta con la barra Geometria.", f.Key, Fmt(taglio, 2)); return null; }
            }
            if (tags.Count == 0) { msg = Lang.T("Nessun filo da spostare a questa quota."); return null; }
            bool suolo = m0.Ground != null && m0.Ground.Kind != GroundKind.FreeSpace;
            if (suolo && zminSpostati + dz <= 0) { msg = Lang.T("Con questo valore i fili andrebbero sotto il suolo."); return null; }
            if (k > 1 && zminSpostati + dz <= lv[k - 1] + 1e-3) { msg = Lang.T("Con questo valore il livello scenderebbe sotto quello di prima."); return null; }
            try { return TrasformaGeometria.Applica(m0, TrasformaGeometria.Sposta(0, 0, dz), tags); }
            catch (Exception ex) { msg = ex.Message; return null; }
        }

        // ------------------------------------------------------------------------------------------------
        // 2) distanza di stack in diretta nei Diagrammi
        // ------------------------------------------------------------------------------------------------
        Border stackBox;
        StackPanel stackRighe;
        TextBlock stackEsito;
        NecModel stackBase, stackAtteso;     // modello di partenza dello stack e ultimo modello messo dal riquadro
        StackInfo stackDiaInfo;
        double stackDv, stackDh;
        DispatcherTimer stackTimer;
        bool stackInAggiorna;

        Control RiquadroStackDiagrammi()
        {
            stackRighe = new StackPanel { Spacing = 6 };
            stackEsito = new TextBlock { FontSize = 12, Foreground = Pal.B(Pal.Fore) };
            StackPanel sp = new StackPanel { Spacing = 4 };
            sp.Children.Add(stackRighe);
            sp.Children.Add(stackEsito);
            sp.Children.Add(new TextBlock { Text = "tratteggio: lobo con la distanza di partenza", FontSize = 11, Foreground = Pal.B(Pal.Muted) });
            stackBox = new Border
            {
                Child = sp, Padding = new Thickness(10, 8), Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 6, 10, 0),
                Width = 330, IsVisible = false
            };
            stackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            stackTimer.Tick += (s, e) => { stackTimer.Stop(); StackApplica(); };
            return stackBox;
        }

        /// <summary>Dopo ogni calcolo: mostra o nasconde il riquadro e scrive guadagno, F/B ed elevazione.</summary>
        void AggiornaRiquadroStack()
        {
            if (stackBox == null || stackInAggiorna) return;
            if (model == null) { stackBox.IsVisible = false; return; }
            if (model != stackAtteso)
            {
                // modello nuovo (aperto, modificato altrove): nuovo riconoscimento
                StackInfo si = null;
                try { si = StackDetect.Detect(model); } catch { si = null; }
                if (si == null || si.Copies < 2 || (si.Rows < 2 && si.Cols < 2)) { stackBox.IsVisible = false; stackBase = null; stackDiaInfo = null; return; }
                stackBase = model; stackDiaInfo = si; stackAtteso = model;
                stackDv = si.SpacingV; stackDh = si.SpacingH;
                CostruisciRigheStack();
            }
            stackBox.IsVisible = true;
            if (run != null && run.FreqMHz > 0)
                stackEsito.Text = Lang.F("{0} dBi · F/B {1} dB · el {2}°", Fmt(run.GmaxDbi, 2), Fmt(double.IsNaN(run.FrontBack) || double.IsInfinity(run.FrontBack) ? 0 : run.FrontBack, 1), Fmt(run.ElevationOfMax, 1));
        }

        void CostruisciRigheStack()
        {
            stackRighe.Children.Clear();
            StackInfo si = stackDiaInfo;
            string pol = si.HorizontalPol ? Lang.T("orizzontale") : Lang.T("verticale");
            stackRighe.Children.Add(new TextBlock { Text = Lang.F("Stack {0} × {1} (antenne uguali, {2})", si.Rows, si.Cols, pol), FontWeight = FontWeight.SemiBold, FontSize = 13, TextWrapping = TextWrapping.Wrap });
            double lam = model != null && model.FreqMHz > 0 ? 299.792458 / model.FreqMHz : double.NaN;
            if (si.Rows > 1) stackRighe.Children.Add(RigaStack(true, stackDv, lam));
            if (si.Cols > 1) stackRighe.Children.Add(RigaStack(false, stackDh, lam));
        }

        Control RigaStack(bool verticale, double d0, double lam)
        {
            StackPanel sp = new StackPanel { Spacing = 2 };
            TextBox tb = new TextBox { Text = Fmt(d0, 2), Width = 72, HorizontalContentAlignment = HorizontalAlignment.Right };
            Button bm = Btn("−", false), bp = Btn("+", false);
            bm.Padding = bp.Padding = new Thickness(9, 2);
            double min = Math.Max(0.05, Math.Round(d0 * 0.4, 2)), max = Math.Round(d0 * 2.2, 2);
            Slider sl = new Slider { Minimum = min, Maximum = max, Value = d0, SmallChange = 0.01, LargeChange = 0.1 };
            TextBlock lab = new TextBlock { FontSize = 11, Foreground = Pal.B(Pal.Muted) };
            Action<double> scrivi = v => { lab.Text = (double.IsNaN(lam) ? "" : Fmt(v / lam, 2) + " λ · ") + Lang.F("partenza {0} m", Fmt(d0, 2)); };
            scrivi(d0);
            bool interno = false;
            Action<double, bool> metti = (v, daCursore) =>
            {
                v = Math.Max(0.02, v);
                interno = true;
                if (!daCursore) sl.Value = Math.Max(sl.Minimum, Math.Min(sl.Maximum, v));
                tb.Text = Fmt(v, 2);
                interno = false;
                scrivi(v);
                if (verticale) stackDv = v; else stackDh = v;
                stackTimer.Stop(); stackTimer.Start();
            };
            double passo = Math.Max(0.01, Math.Round(d0 * 0.02, 2));
            bm.Click += (s, e) => metti(ParseNum(tb.Text, d0) - passo, false);
            bp.Click += (s, e) => metti(ParseNum(tb.Text, d0) + passo, false);
            tb.KeyDown += (s, e) => { if (e.Key == Key.Enter) { e.Handled = true; metti(ParseNum(tb.Text, d0), false); } };
            sl.PropertyChanged += (s, e) => { if (e.Property == Slider.ValueProperty && !interno) metti(Math.Round(sl.Value, 2), true); };
            StackPanel r1 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            r1.Children.Add(new TextBlock { Text = verticale ? Lang.T("distanza verticale") : Lang.T("distanza orizzontale"), VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), Width = 120, TextWrapping = TextWrapping.Wrap });
            r1.Children.Add(bm); r1.Children.Add(tb); r1.Children.Add(bp);
            r1.Children.Add(new TextBlock { Text = "m", VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(r1); sp.Children.Add(sl); sp.Children.Add(lab);
            return sp;
        }

        void StackApplica()
        {
            if (stackBase == null || stackDiaInfo == null) return;
            try
            {
                bool primo = model == stackBase;
                if (primo && run != null) FissaRisultato();   // il lobo di partenza resta tratteggiato
                NecModel m = stackBase.Clone();
                StackDetect.Apply(m, stackDiaInfo, stackDiaInfo.Rows > 1 ? stackDv : double.NaN, stackDiaInfo.Cols > 1 ? stackDh : double.NaN, double.NaN);
                List<string> err = m.Reevaluate();
                if (err.Count > 0) { Status("Stack: " + err[0], true); return; }
                stackAtteso = m;
                Registro.Scrivi("AZIONE", string.Format(CultureInfo.InvariantCulture, "diagrammi: distanza di stack verticale {0:0.###} m, orizzontale {1:0.###} m", stackDv, stackDh));
                stackInAggiorna = true;
                try { GeoMetti(m, primo); }
                finally { stackInAggiorna = false; }
                Status(Lang.F("Distanza di stack cambiata: {0}. «Annulla» della barra Geometria torna al modello di partenza.", stackDiaInfo.Rows > 1 ? Fmt(stackDv, 2) + " m" : Fmt(stackDh, 2) + " m"));
            }
            catch (Exception ex) { Status("Stack: " + ex.Message, true); }
        }
    }
}
