// YA-NEC 0.5.3 - Calcolatori > Cavi RG: "Cambio d'impedenza con un tratto di cavo" nella colonna destra (al posto del disegno vuoto).
// IC8TEM 15/11/2026 19:42: "dove ho messo asterisco mi devi mettere un calcolatore di cambio impedenza se interpongo un cavo. esempio: un delta
// loop se lo alimento a 100ohm interpongo un cavo da 75ohm in quarto d'onda. se ti prendi il database dei cavi da YAEC questo lo puoi proporre
// gia' adesso"; bozzetto approvato alle 19:44 ("approvato").
//  - dati: impedenza dell'antenna (R, X; anche "dal modello"), impedenza da ottenere, cavo del trasformatore (tutto l'elenco, anche bifilari),
//    cavi in parallelo, lunghezza lambda/4, 3/4, 5/4 o libera (in lambda); frequenza = quella della scheda a sinistra;
//  - riquadri: Z0 ideale, lunghezza da tagliare, Z ottenuta (con le perdite), ROS, perdita;
//  - tabella dei trasformatori possibili (per ogni Z0 e numero di cavi in parallelo il cavo con meno perdita), ordinata per ROS: clic = scelta;
//  - pulsanti: "Usa questo cavo nella scheda Cavi RG", "Metti la linea nel modello aperto (TL)", "Copia i valori".
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        static bool Bifilare(Cable c) { return c != null && (c.Dielectric ?? "").StartsWith("bifilare", StringComparison.OrdinalIgnoreCase); }

        Control CostruisciTrasformatore(SimpleCalc sc, Dictionary<string, TextBox> campi, Dictionary<string, ComboBox> tendine, Action ricalcola, out Action<CalcOutput> aggiornaDestra)
        {
            aggiornaDestra = null;
            List<Cable> lista = CalcCavi.Lista;
            StackPanel root = new StackPanel { Spacing = 4 };
            StackPanel tit = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            tit.Children.Add(new TextBlock { Text = "Cambio d'impedenza con un tratto di cavo", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            root.Children.Add(tit);
            if (lista.Count == 0) { root.Children.Add(Nota("Abaco dei cavi non trovato (data/cavi.csv).")); return root; }

            TextBox tR = Tb("100", 70), tX = Tb("0", 70), tZ = Tb("50", 70), tLib = Tb("0,25", 70);
            foreach (TextBox t in new[] { tR, tX, tZ, tLib }) t.FontFamily = Pal.Mono;
            Button bMod = Btn("dal modello", false);
            ToolTip.SetTip(bMod, "Prende R e X dell'ultimo calcolo del modello aperto (prima sorgente).");
            StackPanel rz = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            rz.Children.Add(Lbl("R")); rz.Children.Add(tR); rz.Children.Add(Lbl("X")); rz.Children.Add(tX); rz.Children.Add(Lbl("Ω")); rz.Children.Add(bMod);
            root.Children.Add(RigaCalc("Impedenza dell'antenna", rz));
            StackPanel zz = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            zz.Children.Add(tZ); zz.Children.Add(Lbl("Ω  (il cavo di discesa)"));
            root.Children.Add(RigaCalc("Impedenza da ottenere", zz));
            string[] nomi = new string[lista.Count];
            for (int i = 0; i < lista.Count; i++) nomi[i] = lista[i].Name + " · " + Fmt(lista[i].Z0, 0) + " Ω · VF " + Fmt(lista[i].Vf, 2);
            int iniz = Math.Max(0, lista.FindIndex(c => Math.Abs(c.Z0 - 75) < 1));
            ComboBox cCavo = new ComboBox { ItemsSource = nomi, SelectedIndex = iniz, MinWidth = 300, MaxWidth = 460 };
            root.Children.Add(RigaCalc("Cavo del trasformatore", cCavo));
            int npar = 1, modo = 0;
            Action ricalcolaT = null;
            StackPanel par = Scelta(new[] { "1 cavo", "2 in parallelo" }, 0, k => { npar = k + 1; if (ricalcolaT != null) ricalcolaT(); });
            root.Children.Add(RigaCalc("Cavi in parallelo", par));
            StackPanel lun = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            lun.Children.Add(Scelta(new[] { "λ/4", "3λ/4", "5λ/4", "libera" }, 0, k => { modo = k; tLib.IsEnabled = k == 3; if (ricalcolaT != null) ricalcolaT(); }));
            lun.Children.Add(tLib); lun.Children.Add(Lbl("λ"));
            tLib.IsEnabled = false;
            root.Children.Add(RigaCalc("Lunghezza elettrica", lun));
            TextBlock fNota = Nota("");
            root.Children.Add(fNota);

            WrapPanel cards = new WrapPanel { Margin = new Thickness(0, 6, 0, 4) };
            root.Children.Add(cards);
            Grid tab = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,60,1.3*,70,1.2*"), Margin = new Thickness(0, 4, 0, 0) };
            root.Children.Add(tab);
            WrapPanel bt = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            Button bUsa = Btn("Usa questo cavo nella scheda Cavi RG", true), bTl = Btn("Metti la linea nel modello aperto (TL)", false), bCopia = Btn("Copia i valori", false);
            foreach (Button b in new[] { bUsa, bTl, bCopia }) { b.Margin = new Thickness(0, 0, 8, 6); bt.Children.Add(b); }
            root.Children.Add(bt);
            root.Children.Add(Nota("Con la reattanza dell'antenna diversa da 0 la Z ottenuta e il ROS tengono conto anche di X. \"libera\": qualunque lunghezza (cosa si vede in fondo al cavo). " +
                "Se λ/4 e' troppo corto per arrivare al punto di alimentazione si sceglie 3λ/4 o 5λ/4: stessa Z, banda piu' stretta e piu' perdita. Le linee bifilari vanno usate con un balun. " +
                "Nel modello la linea e' una scheda TL del NEC (senza perdite): la perdita del cavo si legge qui."));

            double fUlt = 14.2, lunUlt = 0, z0Ult = 75; Complex zAntUlt = new Complex(100, 0);
            string testo = "";
            Func<int, Cable> cavo = i => lista[Math.Max(0, Math.Min(lista.Count - 1, i))];
            Func<double> lunL = () => modo == 3 ? Math.Max(0.001, ParseNum(tLib.Text, 0.25)) : new[] { 0.25, 0.75, 1.25 }[modo];
            ricalcolaT = () =>
            {
                double f = 14.2;
                CalcParam pf = sc.Params.Find(p => p.Key == "f");
                double fm;
                if (sc.DalModello != null && sc.DalModello.TryGetValue("f", out fm)) f = fm;
                else if (pf != null) f = pf.Value;
                if (f <= 0) f = 14.2;
                double r = Math.Max(0.01, ParseNum(tR.Text, 100)), x = ParseNum(tX.Text, 0), zt = Math.Max(0.1, ParseNum(tZ.Text, 50));
                Complex za = new Complex(r, x);
                Cable c = cavo(cCavo.SelectedIndex);
                double z0 = c.Z0 / npar, ll = lunL();
                double lam = LineCalc.Wavelength(f, c.Vf), len = ll * lam;
                double att = c.AttenuationAt(f);
                Complex zin = LineCalc.InputImpedance(za, z0, f, c.Vf, att, len);
                double ros = LineCalc.Swr(zin, zt), rosSenza = LineCalc.Swr(za, zt);
                double eff = LineCalc.PowerEfficiency(za, z0, f, c.Vf, att, len);
                double perdita = eff > 0 ? -10 * Math.Log10(eff) : double.PositiveInfinity;
                fUlt = f; lunUlt = len; z0Ult = z0; zAntUlt = za;
                fNota.Text = "Frequenza " + Fmt(f, 3) + " MHz (quella della scheda a sinistra). Cavo: " + c.Name + (Bifilare(c) ? " (linea bifilare: serve un balun)" : "") + ", " + c.Source + ".";
                cards.Children.Clear();
                cards.Children.Add(Riquadretto("Z0 ideale", Fmt(QuarterWaveMatch.IdealZ0(r, zt), 1) + " Ω", "√(" + Fmt(r, 0) + " · " + Fmt(zt, 0) + ")", true));
                cards.Children.Add(Riquadretto("Lunghezza da tagliare", Fmt(len, 3) + " m", Fmt(ll, 3) + " λ = " + Fmt(ll * LineCalc.C0 / f, 3) + " m × VF " + Fmt(c.Vf, 2)));
                cards.Children.Add(Riquadretto("Z ottenuta", Calcolatori.Cx(zin, 1), "con le perdite del cavo"));
                cards.Children.Add(Riquadretto("ROS su " + Fmt(zt, 0) + " Ω", Fmt(ros, 2), "senza: " + Fmt(rosSenza, 2), ros < 1.5));
                cards.Children.Add(Riquadretto("Perdita", Fmt(perdita, 2) + " dB", Fmt(len, 2) + " m" + (npar > 1 ? " (2 cavi)" : "")));
                // trasformatori possibili: per ogni (Z0, n) il cavo con meno perdita, ordinati per ROS
                Dictionary<string, Tuple<int, int, double, Complex, double>> migliori = new Dictionary<string, Tuple<int, int, double, Complex, double>>();
                for (int i = 0; i < lista.Count; i++)
                    for (int n = 1; n <= 2; n++)
                    {
                        Cable ci = lista[i];
                        double z0i = ci.Z0 / n, li = ll * LineCalc.Wavelength(f, ci.Vf), ai = ci.AttenuationAt(f);
                        Complex zi = LineCalc.InputImpedance(za, z0i, f, ci.Vf, ai, li);
                        double ri = LineCalc.Swr(zi, zt);
                        double ei = LineCalc.PowerEfficiency(za, z0i, f, ci.Vf, ai, li);
                        double pi = ei > 0 ? -10 * Math.Log10(ei) : 99;
                        string chiave = Fmt(ci.Z0, 0) + "|" + n + "|" + (Bifilare(ci) ? "b" : "c");
                        Tuple<int, int, double, Complex, double> v;
                        if (!migliori.TryGetValue(chiave, out v) || pi < v.Item5) migliori[chiave] = Tuple.Create(i, n, ri, zi, pi);
                    }
                List<Tuple<int, int, double, Complex, double>> el = new List<Tuple<int, int, double, Complex, double>>(migliori.Values);
                el.Sort((a, b) => a.Item3.CompareTo(b.Item3));
                tab.Children.Clear(); tab.RowDefinitions.Clear();
                string[] hh = { "Trasformatore possibile (clic per sceglierlo)", "Z0", "Z ottenuta", "ROS", "perdita" };
                tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int i = 0; i < hh.Length; i++) { TextBlock t = new TextBlock { Text = hh[i], FontSize = 12, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 0, 6, 2) }; Grid.SetColumn(t, i); tab.Children.Add(t); }
                int riga = 1;
                foreach (Tuple<int, int, double, Complex, double> v in el)
                {
                    if (riga > 7) break;
                    Cable ci = lista[v.Item1];
                    bool scelto = v.Item1 == cCavo.SelectedIndex && v.Item2 == npar;
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    string nome = (v.Item2 == 2 ? "due cavi da " : "un cavo da ") + Fmt(ci.Z0, 0) + " Ω" + (v.Item2 == 2 ? " in parallelo" : "") + " (" + ci.Name + (Bifilare(ci) ? ", bifilare" : "") + ")" + (riga == 1 ? "  · il migliore" : "");
                    string[] val = { nome, Fmt(ci.Z0 / v.Item2, 1), Calcolatori.Cx(v.Item4, 1), Fmt(v.Item3, 2), Fmt(v.Item5, 2) + " dB" };
                    int ii = v.Item1, nn = v.Item2;
                    for (int k = 0; k < val.Length; k++)
                    {
                        TextBlock t = new TextBlock { Text = val[k], FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 6, 1), FontFamily = k == 0 ? FontFamily.Default : Pal.Mono,
                            Foreground = Pal.B(scelto ? Pal.Accent : (v.Item3 < 1.5 ? Pal.Fore : Pal.Muted)), FontWeight = scelto ? FontWeight.SemiBold : FontWeight.Normal, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
                        t.PointerPressed += (s, e) =>
                        {
                            npar = nn;
                            if (par.Children.Count == 2) { StileScelta((Button)par.Children[0], nn == 1); StileScelta((Button)par.Children[1], nn == 2); }
                            if (cCavo.SelectedIndex == ii) ricalcolaT(); else cCavo.SelectedIndex = ii;
                        };
                        Grid.SetRow(t, riga); Grid.SetColumn(t, k); tab.Children.Add(t);
                    }
                    riga++;
                }
                testo = "Cambio d'impedenza con un tratto di cavo (YA-NEC)\nFrequenza: " + Fmt(f, 3) + " MHz\nAntenna: " + Calcolatori.Cx(za, 1) + "\nDa ottenere: " + Fmt(zt, 1) + " Ω\n" +
                        "Cavo: " + (npar > 1 ? "2 x " : "") + c.Name + " (Z0 " + Fmt(c.Z0, 0) + " Ω, VF " + Fmt(c.Vf, 2) + ")\nLunghezza: " + Fmt(ll, 3) + " λ = " + Fmt(len, 3) + " m\n" +
                        "Z ottenuta: " + Calcolatori.Cx(zin, 1) + "\nROS: " + Fmt(ros, 2) + " (senza: " + Fmt(rosSenza, 2) + ")\nPerdita: " + Fmt(perdita, 2) + " dB\nZ0 ideale: " + Fmt(QuarterWaveMatch.IdealZ0(r, zt), 1) + " Ω";
            };
            cCavo.SelectionChanged += (s, e) => ricalcolaT();
            foreach (TextBox t in new[] { tR, tX, tZ, tLib })
            {
                TextBox tt = t;
                t.LostFocus += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => { if (TopLevel.GetTopLevel(tt) != null) ricalcolaT(); });
                t.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter) ricalcolaT(); };
            }
            bMod.Click += (s, e) =>
            {
                if (run == null || model == null) { Status("Calcola prima il modello aperto: R e X vengono dalla sua prima sorgente.", true); return; }
                tR.Text = Fmt(run.Zin.Real, 2); tX.Text = Fmt(run.Zin.Imaginary, 2);
                ricalcolaT();
            };
            bUsa.Click += (s, e) =>
            {
                ricalcolaT();
                ComboBox cb;
                if (tendine.TryGetValue("c", out cb)) cb.SelectedIndex = cCavo.SelectedIndex;
                TextBox t;
                if (campi.TryGetValue("l", out t)) t.Text = Fmt(lunUlt, 3);
                if (campi.TryGetValue("rl", out t)) t.Text = Fmt(zAntUlt.Real, 2);
                if (campi.TryGetValue("xl", out t)) t.Text = Fmt(zAntUlt.Imaginary, 2);
                foreach (CalcParam p in sc.Params)
                {
                    if (p.Key == "c") p.Value = cCavo.SelectedIndex;
                    else if (p.Key == "l") p.Value = Math.Round(lunUlt, 3);
                    else if (p.Key == "rl") p.Value = zAntUlt.Real;
                    else if (p.Key == "xl") p.Value = zAntUlt.Imaginary;
                }
                ricalcola();
                Status("Scheda Cavi RG: cavo, lunghezza e carico presi dal cambio d'impedenza" + (npar > 1 ? " (la scheda considera un cavo solo: con 2 in parallelo vale la Z0 dimezzata)" : "") + ".");
            };
            bCopia.Click += async (s, e) => { ricalcolaT(); try { if (Clipboard != null) await Clipboard.SetTextAsync(testo); Status("Valori del cambio d'impedenza copiati."); } catch { } };
            bTl.Click += (s, e) =>
            {
                ricalcolaT();
                try
                {
                    Cable c = cavo(cCavo.SelectedIndex);
                    NecModel m = LineaNelModello(z0Ult, lunUlt / c.Vf, fUlt);
                    if (m == null) return;
                    Registro.Scrivi("AZIONE", "cambio d'impedenza: linea TL nel modello, Z0 " + Fmt(z0Ult, 1) + " ohm, " + Fmt(lunUlt, 3) + " m di " + c.Name);
                    GeoMetti(m, true);
                    Status("Linea messa nel modello: TL da " + Fmt(z0Ult, 1) + " Ω lunga " + Fmt(lunUlt, 3) + " m (" + c.Name + ", VF " + Fmt(c.Vf, 2) + "); la sorgente e' ora in fondo alla linea. \"Annulla\" torna indietro.");
                }
                catch (Exception ex) { Status("Linea nel modello: " + ex.Message, true); }
            };
            // 1.0.6: la colonna destra segue ogni ricalcolo della scheda a sinistra (prima restava sulla frequenza vecchia)
            aggiornaDestra = o => { try { ricalcolaT(); } catch (Exception ex) { Registro.Scrivi("ERRORE", "cambio d'impedenza: " + ex.Message); } };
            ricalcolaT();
            return root;
        }

        NecModel LineaNelModello(double z0, double lenNec, double fMHz)
        {
            if (model == null) { Status("Apri prima un modello.", true); return null; }
            string err;
            NecModel m = LineaAdattamento.Inserisci(model, z0, lenNec, fMHz, out err);
            if (m == null) Status(err, true);
            return m;
        }
    }
}
