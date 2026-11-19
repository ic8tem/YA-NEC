// YA-NEC 1.5.2 - Ottimizzatore, riquadro «4. Misure al millimetro» (bozzetto v2 approvato da IC8TEM il 19/11/2026 14:37,
// doc/bozzetti_approvati/Ottimizzatore_misure_al_millimetro_19-11-2026.html). Motore: Engine/Millimetro.cs.
// Richiesta di IC8TEM del 19/11/2026 08:47: elementi gia' divisi in piu' fili (tubi telescopici, misure nate in pollici come
// -5,575300 m) riportati al millimetro, con una nuova verifica dell'antenna prima e dopo alle frequenze da rispettare.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        bool mmDiametri = true, mmSoloScelti;
        double mmSogliaZ = 2, mmSogliaG = 0.05, mmSogliaFb = 0.5;
        Millimetro.Esito mmEsito;
        NecModel mmBase;
        StackPanel mmRis;
        Button mmVerifica;

        Control MillimetroPannello()
        {
            StackPanel p = new StackPanel { Spacing = 6 };
            Func<string, Control[], WrapPanel> riga = (et, cc) =>
            {
                WrapPanel w = new WrapPanel { Margin = new Thickness(0, 1) };
                w.Children.Add(new TextBlock { Text = et, Width = 90, VerticalAlignment = VerticalAlignment.Center });
                foreach (Control c in cc) { c.Margin = new Thickness(0, 0, 8, 0); c.VerticalAlignment = VerticalAlignment.Center; w.Children.Add(c); }
                return w;
            };
            Func<string, TextBlock> muted = s => new TextBlock { Text = s, FontSize = 12, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, MaxWidth = 420 };

            Button bMm = Btn("al millimetro", true); StileScelta(bMm, true);
            p.Children.Add(riga("Coordinate", new Control[] { bMm }));

            Button bDia = Btn("diametri commerciali", false); StileScelta(bDia, mmDiametri);
            bDia.Click += (s, e) => { mmDiametri = !mmDiametri; StileScelta(bDia, mmDiametri); mmEsito = null; };
            p.Children.Add(riga("Diametri", new Control[] { bDia }));
            p.Children.Add(muted("il tubo commerciale piu' vicino (alluminio: 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 30 mm...); sotto i 6 mm il filo metrico o AWG piu' vicino. Il raggio negativo (riga delle Rastremazioni) resta com'e'."));

            Button bTutti = Btn("tutti", false), bScelti = Btn("solo quelli scelti nella tabella", false);
            Action stileFili = () => { StileScelta(bTutti, !mmSoloScelti); StileScelta(bScelti, mmSoloScelti); };
            bTutti.Click += (s, e) => { mmSoloScelti = false; stileFili(); mmEsito = null; };
            bScelti.Click += (s, e) => { mmSoloScelti = true; stileFili(); mmEsito = null; };
            stileFili();
            p.Children.Add(riga("Fili", new Control[] { bTutti, bScelti }));
            p.Children.Add(muted("i punti in comune tra due fili restano uniti (si arrotonda il punto, non il filo)"));

            WrapPanel cmd = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            mmVerifica = Btn("Arrotonda e verifica (NEC)", true);
            mmVerifica.Click += async (s, e) => await MmVerifica();
            Button bApp = Btn("Applica al modello", false);
            bApp.Click += (s, e) => MmApplica();
            Button bCopia = Btn("Copia la lista di taglio", false);
            bCopia.Click += async (s, e) =>
            {
                if (model == null) { Status("Apri prima un modello.", true); return; }
                NecModel m = mmEsito != null && ReferenceEquals(mmBase, model) ? mmEsito.Modello : model;
                try { if (Clipboard != null) await Clipboard.SetTextAsync(Millimetro.ListaTaglio(m)); Status("Lista di taglio copiata."); } catch { }
            };
            foreach (Button b in new[] { mmVerifica, bApp, bCopia }) { b.Margin = new Thickness(0, 0, 6, 4); cmd.Children.Add(b); }
            p.Children.Add(cmd);
            p.Children.Add(Nota("La verifica calcola il modello prima e dopo alle frequenze da rispettare (riquadro 1) e confronta. «Applica al modello» va nella storia di Annulla della barra Geometria."));
            mmRis = new StackPanel { Spacing = 4 };
            p.Children.Add(mmRis);
            return p;
        }

        HashSet<int> MmFiliScelti()
        {
            HashSet<int> t = new HashSet<int>();
            if (wireRows == null) return t;
            for (int i = 0; i < wireRows.Count; i++)
            {
                bool dentro = wireRows[i] == activeRow || (selFrom >= 0 && i >= Math.Min(selFrom, selTo) && i <= Math.Max(selFrom, selTo));
                if (dentro && wireRows[i].Card != null && wireRows[i].Card.Id == "GW") t.Add(wireRows[i].Card.I1);
            }
            return t;
        }

        static string MmZ(Complex z) { return FN(z.Real, 1) + " " + (z.Imaginary < 0 ? "−j" : "+j") + FN(Math.Abs(z.Imaginary), 1); }

        async Task MmVerifica()
        {
            if (model == null) { Status("Apri prima un modello.", true); return; }
            HashSet<int> scelti = null;
            if (mmSoloScelti)
            {
                scelti = MmFiliScelti();
                if (scelti.Count == 0) { Status("Scegli prima i fili nella tabella della Struttura 3D.", true); return; }
            }
            NecModel base0 = model;
            Millimetro.Esito es = Millimetro.Arrotonda(base0, mmDiametri, scelti);
            mmEsito = es; mmBase = base0;
            mmRis.Children.Clear();
            if (es.Cambi.Count == 0)
            {
                mmRis.Children.Add(Nota(es.Formule > 0 ? Lang.F("Niente da arrotondare: le misure sono gia' al millimetro ({0} campi sono formule o variabili e restano come sono).", es.Formule) : Lang.T("Niente da arrotondare: le misure sono gia' al millimetro.")));
                return;
            }
            List<double> freq = ottFreq.Count > 0 ? new List<double>(ottFreq) : new List<double> { base0.FreqMHz };
            mmVerifica.IsEnabled = false;
            Status("Misure al millimetro: " + "calcolo in corso…");
            try
            {
                List<string[]> righe = new List<string[]>
                {
                    new[] { Lang.T("Frequenza"), Lang.T("Z prima"), Lang.T("Z dopo"), Lang.T("ROS prima"), Lang.T("ROS dopo"), Lang.T("G dBi prima"), Lang.T("G dopo"), Lang.T("F/B prima"), Lang.T("F/B dopo"), Lang.T("esito") }
                };
                // il filo che sposta di piu' (per l'esito arancio)
                int filoMax = -1; double sMax = -1;
                foreach (KeyValuePair<int, double> kv in es.SpostamentoPerFilo) if (kv.Value > sMax) { sMax = kv.Value; filoMax = kv.Key; }
                List<int> fuori = new List<int>();
                foreach (double f in freq)
                {
                    RunResult r0 = await Task.Run(() => Analysis.Run(base0, base0.BuildGeometry(), f, 2.0));
                    RunResult r1 = await Task.Run(() => Analysis.Run(es.Modello, es.Modello.BuildGeometry(), f, 2.0));
                    double dz = r0.Zin.Magnitude > 0 ? (r1.Zin - r0.Zin).Magnitude / r0.Zin.Magnitude * 100 : 0;
                    double dg = r1.GmaxDbi - r0.GmaxDbi, dfb = r1.FrontBack - r0.FrontBack;
                    List<string> oltre = new List<string>();
                    if (dz > mmSogliaZ) oltre.Add("Z " + FN(dz, 1) + " %");
                    if (Math.Abs(dg) > mmSogliaG) oltre.Add("G " + (dg > 0 ? "+" : "−") + FN(Math.Abs(dg), 2) + " dB");
                    if (Math.Abs(dfb) > mmSogliaFb) oltre.Add("F/B " + (dfb > 0 ? "+" : "−") + FN(Math.Abs(dfb), 1) + " dB");
                    string esito = oltre.Count == 0 ? Lang.T("uguale") : string.Join(", ", oltre) + (filoMax > 0 ? " (" + Lang.T("filo") + " " + filoMax + ")" : "");
                    if (oltre.Count > 0) fuori.Add(righe.Count);
                    righe.Add(new[] { FN(f, 3) + " MHz", MmZ(r0.Zin), MmZ(r1.Zin), FN(r0.Swr, 2), FN(r1.Swr, 2), FN(r0.GmaxDbi, 2), FN(r1.GmaxDbi, 2), FN(r0.FrontBack, 1), FN(r1.FrontBack, 1), esito });
                }
                if (!ReferenceEquals(base0, model)) { mmEsito = null; Status("Il modello e' cambiato durante la verifica: ripeti «Arrotonda e verifica».", true); return; }
                Grid tab = Tabella(righe);
                // esito: verde se uguale, arancio oltre le soglie
                foreach (Control c in tab.Children)
                    if (c is Border b && Grid.GetColumn(b) == righe[0].Length - 1 && Grid.GetRow(b) > 0 && b.Child is TextBlock tb)
                    { tb.Foreground = Pal.B(fuori.Contains(Grid.GetRow(b)) ? Pal.Orange : Pal.Green); tb.FontWeight = FontWeight.SemiBold; }
                mmRis.Children.Add(new ScrollViewer { Content = tab, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled });
                TextBlock soglie = Nota(Lang.F("Soglie dell'esito: Z entro il {0} %, guadagno entro {1} dB, F/B entro {2} dB (si cambiano col tasto destro). Oltre: arancio, con il filo che sposta di piu'.", FN(mmSogliaZ, 2), FN(mmSogliaG, 2), FN(mmSogliaFb, 2)));
                soglie.ContextFlyout = MmFlyoutSoglie();
                tab.ContextFlyout = MmFlyoutSoglie();
                mmRis.Children.Add(soglie);

                // misure cambiate
                mmRis.Children.Add(new TextBlock { Text = Lang.T("Misure cambiate"), FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 8, 0, 2) });
                List<string[]> rc = new List<string[]> { new[] { Lang.T("Filo"), Lang.T("Campo"), Lang.T("Prima (m)"), Lang.T("Dopo (m)"), "Δ mm" } };
                // un giunto (stesso punto in due fili, stesso asse, stesso valore prima e dopo) e' una riga sola, come nel bozzetto: «1–2, Y2 / Y1 (giunto)»
                List<Millimetro.Cambio> righeC = new List<Millimetro.Cambio>();
                Dictionary<string, Millimetro.Cambio> giunti = new Dictionary<string, Millimetro.Cambio>();
                foreach (Millimetro.Cambio c0 in es.Cambi)
                {
                    if (c0.Diametro) { righeC.Add(c0); continue; }
                    string k = c0.Campo.Substring(0, 1) + "|" + c0.Prima.ToString("R", CultureInfo.InvariantCulture) + "|" + c0.Dopo.ToString("R", CultureInfo.InvariantCulture);
                    Millimetro.Cambio g0;
                    if (giunti.TryGetValue(k, out g0) && g0.Tag != c0.Tag && g0.Nota == "")
                    { g0.Nota = c0.Tag.ToString(CultureInfo.InvariantCulture); g0.Campo = g0.Campo + " / " + c0.Campo + " (" + Lang.T("giunto") + ")"; continue; }
                    Millimetro.Cambio cc = new Millimetro.Cambio { Tag = c0.Tag, Campo = c0.Campo, Prima = c0.Prima, Dopo = c0.Dopo };
                    giunti[k] = cc; righeC.Add(cc);
                }
                int nMostra = Math.Min(righeC.Count, 200);
                for (int i = 0; i < nMostra; i++)
                {
                    Millimetro.Cambio c = righeC[i];
                    if (c.Diametro)
                        rc.Add(new[] { c.Tag.ToString(CultureInfo.InvariantCulture), Lang.T("diametro (mm)"), FN(c.Prima, 3), FN(c.Dopo, 3) + " (" + Lang.T(c.Nota) + ")", (c.Dopo - c.Prima >= 0 ? "+" : "−") + FN(Math.Abs(c.Dopo - c.Prima), 3) });
                    else
                        rc.Add(new[] { c.Tag.ToString(CultureInfo.InvariantCulture) + (c.Nota != "" ? "–" + c.Nota : ""), c.Campo, c.Prima.ToString("0.000000", CultureInfo.CurrentCulture), c.Dopo.ToString("0.000", CultureInfo.CurrentCulture), (c.Dopo - c.Prima >= 0 ? "+" : "−") + FN(Math.Abs(c.Dopo - c.Prima) * 1000, 2) });
                }
                mmRis.Children.Add(new ScrollViewer { Content = Tabella(rc), HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled });
                string piu = righeC.Count > nMostra ? " " + Lang.F("(qui le prime {0})", nMostra) : "";
                mmRis.Children.Add(Nota(Lang.F("In tutto: {0} misure cambiate su {1} fili, spostamento massimo {2} mm, lunghezza totale degli elementi cambiata di {3} mm.",
                    es.Cambi.Count, es.Fili, FN(es.SpostamentoMaxMm, 2), (es.DeltaLunghezzaMm >= 0 ? "+" : "−") + FN(Math.Abs(es.DeltaLunghezzaMm), 1)) + piu +
                    (es.Formule > 0 ? " " + Lang.F("{0} campi sono formule o variabili e restano come sono.", es.Formule) : "")));
                // verifica della segmentazione dopo l'arrotondamento: solo gli avvisi nuovi
                List<string> w0 = new List<string>(), w1 = new List<string>();
                try { base0.BuildGeometry(w0); es.Modello.BuildGeometry(w1); } catch { }
                List<string> nuovi = w1.FindAll(x => !w0.Contains(x));
                if (nuovi.Count > 0) mmRis.Children.Add(AvvisoGiallo(Lang.T("Segmentazione dopo l'arrotondamento:") + " " + string.Join(" ", nuovi)));
                Registro.Scrivi("AZIONE", "misure al millimetro: " + es.Cambi.Count + " misure su " + es.Fili + " fili, spostamento massimo " + es.SpostamentoMaxMm.ToString("0.00", CultureInfo.InvariantCulture) + " mm");
                Status("Misure al millimetro: " + "calcolo finito");
            }
            catch (Exception ex) { mmEsito = null; Status("Misure al millimetro: " + ex.Message, true); }
            finally { mmVerifica.IsEnabled = true; }
        }

        FlyoutBase MmFlyoutSoglie()
        {
            StackPanel sp = new StackPanel { Spacing = 4, Margin = new Thickness(4) };
            TextBox tz = Tb(FN(mmSogliaZ, 2), 60), tg = Tb(FN(mmSogliaG, 2), 60), tf = Tb(FN(mmSogliaFb, 2), 60);
            Func<string, TextBox, string, Control> r = (et, t, um) =>
            {
                StackPanel w = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                w.Children.Add(new TextBlock { Text = et, Width = 110, VerticalAlignment = VerticalAlignment.Center }); w.Children.Add(t);
                w.Children.Add(new TextBlock { Text = um, VerticalAlignment = VerticalAlignment.Center }); return w;
            };
            sp.Children.Add(new TextBlock { Text = Lang.T("Soglie dell'esito"), FontWeight = FontWeight.Bold });
            sp.Children.Add(r(Lang.T("Impedenza"), tz, "%")); sp.Children.Add(r(Lang.T("Guadagno"), tg, "dB")); sp.Children.Add(r("F/B", tf, "dB"));
            Button ok = Btn("Usa queste soglie", true);
            Flyout fl = new Flyout { Content = sp };
            ok.Click += async (s, e) =>
            {
                mmSogliaZ = Math.Max(0, ParseNum(tz.Text, mmSogliaZ)); mmSogliaG = Math.Max(0, ParseNum(tg.Text, mmSogliaG)); mmSogliaFb = Math.Max(0, ParseNum(tf.Text, mmSogliaFb));
                fl.Hide();
                if (mmEsito != null) await MmVerifica();
            };
            sp.Children.Add(ok);
            return fl;
        }

        void MmApplica()
        {
            if (model == null) { Status("Apri prima un modello.", true); return; }
            if (mmEsito == null || !ReferenceEquals(mmBase, model))
            {
                HashSet<int> scelti = mmSoloScelti ? MmFiliScelti() : null;
                if (mmSoloScelti && scelti.Count == 0) { Status("Scegli prima i fili nella tabella della Struttura 3D.", true); return; }
                mmEsito = Millimetro.Arrotonda(model, mmDiametri, scelti); mmBase = model;
            }
            if (mmEsito.Cambi.Count == 0) { Status("Niente da arrotondare: le misure sono gia' al millimetro."); return; }
            int n = mmEsito.Cambi.Count;
            Registro.Scrivi("AZIONE", "misure al millimetro applicate al modello: " + n + " misure");
            GeoMetti(mmEsito.Modello, true);
            mmEsito = null; mmBase = null;
            if (mmRis != null) mmRis.Children.Clear();
            Status(Lang.F("Misure al millimetro applicate al modello ({0} misure). \"Annulla\" della barra Geometria torna indietro.", n));
        }
    }
}
