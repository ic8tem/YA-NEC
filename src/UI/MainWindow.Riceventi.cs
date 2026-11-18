// YA-NEC - linguetta "Antenne riceventi" (bozzetto doc/bozzetto-antenne-riceventi.png, richiesta IC8TEM 13/11/2026):
// dimensionamento e RDF delle antenne riceventi per l'HF (Beverage, BOG, EWE, DHDL, loop magnetico RX) e la verticale
// di riferimento. L'RDF si calcola solo qui: per le antenne trasmittenti non ha senso.
using System;
using System.Collections.Generic;
using System.Globalization;
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
        Fascione fasRx;
        ContentControl hostRx;
        readonly Dictionary<string, Control> rxPages = new Dictionary<string, Control>();

        Control BuildRiceventiPage()
        {
            rxPages.Clear();
            fasRx = new Fascione();
            fasRx.Famiglia("Fili a terra / bassi");
            fasRx.Voce("beverage", "Beverage", false, null, "filo lungo terminato, 1-2 lambda");
            fasRx.Voce("bog", "BOG", false, null, "Beverage On Ground, filo isolato a terra");
            fasRx.Voce("dipbasso", "Dipolo basso", false, null, "riferimento a lobo alto");
            fasRx.Famiglia("Piccoli loop terminati");
            fasRx.Voce("ewe", "EWE", false, null, "WA2WVL, due verticali corte + filo");
            fasRx.Voce("flag", "Flag", false, null, "rettangolo terminato (W7IUV, IV3PRK)");
            fasRx.Voce("pennant", "Pennant", false, null, "triangolo terminato (K6SE, WA1ION, IV3PRK)");
            fasRx.Voce("k9ay", "K9AY", false, null, "loop a terra con resistenza");
            fasRx.Voce("dhdl", "DHDL", false, null, "doppio mezzo delta loop (AA7JV, misure IV3PRK)");
            fasRx.Voce("rombo", "Rombo ripiegato", true, null, "rombo terminato e ripiegato");
            fasRx.Voce("looprx", "Loop magnetico RX", false, null, "schermato, accordato o a larga banda");
            fasRx.Famiglia("Schiere");
            fasRx.Voce("4sqrx", "4-square RX", false, null, "verticali corte attive");
            fasRx.Voce("hiz", "Hi-Z 2-8 elementi", false, null, "cardioide, triangolo, cerchio");
            fasRx.Voce("wf", "Waller Flag", false, null, "due Flag end-fire con linee di fase (NX4D, N4IS, IV3PRK)");
            fasRx.Voce("dhdl2", "Due DHDL broadside", false, null, "affiancate e in fase (IV3PRK 2013)");
            fasRx.Famiglia("Riferimento");
            fasRx.Voce("vert", "Verticale TX", false, null, "lambda/4 con radiali");
            hostRx = new ContentControl();
            fasRx.Selected += k =>
            {
                Control c;
                if (!rxPages.TryGetValue(k, out c)) { c = BuildRxPanel(k); rxPages[k] = c; }
                hostRx.Content = c;
            };
            Grid g = Fascione.PaginaLarga(fasRx, hostRx, 22);
            fasRx.Select("beverage");
            return g;
        }

        internal class RxDefault { public double L, H, R, Rrod, H2, D, Vf, Ang; }

        internal static RxDefault RxDefaults(string k, int banda)
        {
            double sc = banda == 0 ? 1 : (banda == 1 ? 0.5 : 0.25);
            switch (k)
            {
                case "beverage": return new RxDefault { L = 175 * sc, H = 2.5, R = 500, Rrod = 20, Vf = 0.85, Ang = 20 };
                // 0.4.18: quota del filo nel modello NEC = lambda/1000 (limite del terreno di Sommerfeld), vedi RxAntennas.Bog
                case "bog": return new RxDefault { L = 68 * sc, H = RxAntennas.QuotaBog(banda == 0 ? 1.83 : banda == 1 ? 3.6 : 7.05), R = 250, Rrod = 20, Vf = 0.65, Ang = 20 };
                case "ewe": return new RxDefault { L = 9.1 * sc, H = 3.05 * sc, R = 850, Rrod = 20 };
                case "dhdl": return new RxDefault { L = 20 * sc, H = 6.5 * sc, R = 1300, Rrod = 20, H2 = 2 };
                case "dhdl2": return new RxDefault { L = 20 * sc, H = 6.5 * sc, R = 1300, Rrod = 20, H2 = 2, D = 90 * sc };
                case "flag": return new RxDefault { L = 8.84 * sc, H = 4.28 * sc, R = 937, H2 = 8 };
                case "pennant": return new RxDefault { L = 8.84 * sc, H = 4.27 * sc, R = 950, H2 = 8 };
                case "wf": return new RxDefault { L = 4.27 * sc, H = 2.0 * sc, R = 600, H2 = 11, D = 7.73 * sc };
                case "looprx": return new RxDefault { D = 1.0, H2 = 1.5, R = 0 };
                // 1.3.0 (IC8TEM 18/11/2026 00:41 "dovremmo abilitare le parti spente"): voci previste nel bozzetto approvato
                case "dipbasso": return new RxDefault { L = 77.8 * sc, H = 15 * sc };
                case "k9ay": return new RxDefault { L = 9.2, H = 7.6, H2 = 1.0, R = 390 };
                case "4sqrx": return new RxDefault { L = 20.5 * sc, H = 7.6 };
                case "hiz": return new RxDefault { L = 20.5 * sc, H = 7.6, D = 2 };
                default: return new RxDefault { };
            }
        }

        Control BuildRxPanel(string k)
        {
            string titolo = k == "beverage" ? "Beverage" : k == "bog" ? "BOG (Beverage On Ground)" : k == "ewe" ? "EWE (WA2WVL)" : k == "dhdl" ? "DHDL (AA7JV, IV3PRK)" : k == "looprx" ? "Loop magnetico RX" :
                k == "flag" ? "Flag (K6SE, W7IUV)" : k == "dipbasso" ? "Dipolo basso (riferimento)" : k == "k9ay" ? "K9AY (K9AY, Hi-Z)" : k == "4sqrx" ? "4-square in ricezione" : k == "hiz" ? "Schiera Hi-Z di verticali corte" : k == "pennant" ? "Pennant (K6SE, WA1ION)" : k == "wf" ? "Waller Flag (NX4D, N4IS)" : k == "dhdl2" ? "Due DHDL broadside (IV3PRK)" : "Verticale TX di riferimento";
            bool anello = k == "flag" || k == "wf" || k == "pennant", dh = k == "dhdl" || k == "dhdl2";
            bool schiera = k == "4sqrx" || k == "hiz";   // 1.3.0: verticali corte attive in schiera
            int nEl = 2;                                  // 1.3.0: Hi-Z, 2 (cardioide), 3 (triangolo) o 8 (cerchio)
            TextBox tPh = Tb("", 60);
            int banda = 0;
            double[] fb = { 1.83, 3.6, 7.05 };
            RxDefault d0 = RxDefaults(k, 0);
            TextBox tf = Tb("1,830", 80), tL = Tb(FN(d0.L, 1), 70), tH = Tb(FN(d0.H, 2), 60), tR = Tb(FN(d0.R, 0), 70), tRod = Tb(FN(d0.Rrod, 0), 60);
            TextBox tDir = Tb("0", 60), tH2 = Tb(FN(d0.H2, 2), 60), tD = Tb(FN(d0.D, 2), 60), tVf = Tb(FN(d0.Vf, 2), 60), tAng = Tb(FN(d0.Ang, 0), 60);
            TextBox tT1 = Tb("4", 60), tT2 = Tb("3,25", 60), tZl = Tb("100", 60), tRat = Tb("6", 60), tVfl = Tb("0,66", 60);
            TextBlock lLam = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) };
            ComboBox cTer = new ComboBox { Width = 230 };
            List<string> gl = new List<string>();
            foreach (Tuple<string, double, double> g in Grounds) gl.Add(g.Item1 + " " + FN(g.Item2, 0) + " / " + FN(g.Item3, 4));
            cTer.ItemsSource = gl;
            cTer.SelectedIndex = 0;
            Button bOtt = Btn("ottimizza per F/B", false);
            Button bCalc = Btn("Crea il modello NEC e calcola l'RDF", true), bApri = Btn("Apri come modello", false);
            StackPanel cards = new StackPanel { Orientation = Orientation.Horizontal };
            WrapPanel cardsW = new WrapPanel();
            PolarPlot pEl = new PolarPlot { Elevation = true, GroundHalf = true, ArrlScale = false, LinRange = 30, Title = "elevazione" };
            PolarPlot pAz = new PolarPlot { ArrlScale = false, LinRange = 30, Title = "azimut" };
            BarChart bars = new BarChart { Height = 360 };
            StackPanel formule = new StackPanel { Spacing = 2 };
            StackPanel avv = new StackPanel();

            StackPanel inp = new StackPanel { Spacing = 2 };
            Action leggiBanda = null;
            inp.Children.Add(Riga("Banda", Scelta(new[] { "160 m", "80 m", "40 m" }, 0, b =>
            {
                banda = b;
                tf.Text = FN(fb[b], 3);
                RxDefault d = RxDefaults(k, b);
                tL.Text = FN(d.L, 2); tH.Text = FN(d.H, 2); tR.Text = FN(d.R, 0); tH2.Text = FN(d.H2, 2);
                if (k == "wf" || k == "dhdl2") tD.Text = FN(d.D, 2);
                if (k == "hiz") { double lb = 299.792458 / fb[b]; tL.Text = FN(nEl == 2 ? lb / 8 : nEl == 3 ? lb / 6 : 0.37 * lb, 1); }
                if (schiera) tPh.Text = k == "hiz" && nEl == 8 ? "220" : "";
                if (leggiBanda != null) leggiBanda();
            }), tf, new TextBlock { Text = "MHz", VerticalAlignment = VerticalAlignment.Center }));
            bool filo = k == "beverage" || k == "bog";
            if (k == "hiz") inp.Children.Add(Riga("Elementi", Scelta(new[] { "2 (cardioide)", "3 (triangolo)", "8 (cerchio)" }, 0, b =>
            {
                nEl = b == 0 ? 2 : b == 1 ? 3 : 8;
                double lam0 = 299.792458 / ParseNum(tf.Text, 1.83);
                tL.Text = FN(nEl == 2 ? lam0 / 8 : nEl == 3 ? lam0 / 6 : 0.37 * lam0, 1);
                tPh.Text = nEl == 8 ? "220" : "";   // 8 in cerchio: con pi + beta d (file a 0,34 lambda) il lobo non va avanti
                if (leggiBanda != null) leggiBanda();
            })));
            Func<string, TextBlock> um = t => new TextBlock { Text = t, VerticalAlignment = VerticalAlignment.Center };
            if (k != "looprx" && k != "vert") inp.Children.Add(Riga(dh ? "Lunghezza totale (fili orizzontali 3 + 4)" : k == "pennant" ? "Lunghezza (filo alto)" : anello ? "Larghezza dell'anello" :
                k == "k9ay" ? "Base del loop" : k == "4sqrx" ? "Lato del quadrato" : k == "hiz" ? "Distanza, lato o diametro" : "Lunghezza", tL, um("m"), lLam));
            if (k == "beverage" || k == "ewe" || dh || anello || k == "dipbasso" || k == "k9ay" || schiera)
                inp.Children.Add(Riga(dh ? "Verticali alle estremita' (fili 1 e 6)" : k == "pennant" ? "Altezza del lato verticale" : anello ? "Altezza dell'anello" :
                    k == "k9ay" ? "Apice (altezza del palo)" : schiera ? "Altezza delle verticali" : "Altezza del filo", tH, um("m")));
            if (k == "k9ay") inp.Children.Add(Riga("Base del loop dal suolo", tH2, um("m")));
            if (schiera) bOtt.Content = "ottimizza la fase (RDF)";
            if (schiera) inp.Children.Add(Riga("Fase tra le file", tPh, um("gradi (vuota = nullo posteriore, π + βd)"), bOtt));
            TextBlock lQuotaBog = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Orange), TextWrapping = TextWrapping.Wrap, MaxWidth = 380 };
            if (k == "bog") inp.Children.Add(Riga("Quota del filo nel modello NEC", tH, um("m"), lQuotaBog));
            if (dh || anello || k == "looprx") inp.Children.Add(Riga(dh ? "Base (fili orizzontali) dal suolo" : k == "pennant" ? "Centro del triangolo dal suolo" : anello ? "Centro dell'anello dal suolo" : "Base della loop dal suolo", tH2, um("m")));
            if (k == "looprx" || k == "wf" || k == "dhdl2") inp.Children.Add(Riga(k == "looprx" ? "Diametro" : k == "wf" ? "Distanza tra i centri degli anelli" : "Distanza tra le due DHDL", tD, um("m")));
            if (k == "wf")
            {
                inp.Children.Add(Riga("Linea all'anello davanti (TL1)", tT1, um("m"), new TextBlock { Text = "dietro, incrociata (TL2)", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) }, tT2, um("m")));
                inp.Children.Add(Riga("Linea: Z0 / VF", tZl, um("ohm"), tVfl, new TextBlock { Text = "trasformatori", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) }, tRat, um(": 1 (impedenze, 600/100 = 6)")));
            }
            if (filo)
            {
                inp.Children.Add(Riga("Fattore di velocita' sul filo", tVf, new TextBlock { Text = filo && k == "bog" ? "(BOG 0,5-0,77 misurato; N4DJ)" : "(0,85-0,90 a 2 m; N4DJ)", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
                inp.Children.Add(Riga("Angolo di arrivo voluto", tAng, new TextBlock { Text = "gradi", VerticalAlignment = VerticalAlignment.Center }));
            }
            if (k != "looprx" && k != "vert" && k != "dipbasso" && !schiera) inp.Children.Add(Riga("Terminazione", tR, new TextBlock { Text = "ohm", VerticalAlignment = VerticalAlignment.Center }, bOtt));
            if (k != "vert") inp.Children.Add(Riga("Direzione", tDir, new TextBlock { Text = "gradi di azimut", VerticalAlignment = VerticalAlignment.Center }));
            inp.Children.Add(Riga("Terreno", cTer, new TextBlock { Text = "Sommerfeld (GN 2), come NEC-2 con il terreno esatto", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) }));
            if (k != "looprx" && k != "vert" && k != "dipbasso" && k != "k9ay" && !schiera) inp.Children.Add(Riga("Picchetti di terra", tRod, new TextBlock { Text = "ohm", VerticalAlignment = VerticalAlignment.Center }));
            inp.Children.Add(formule);
            StackPanel bb = new StackPanel { Orientation = Orientation.Horizontal };
            bb.Children.Add(bCalc); bb.Children.Add(bApri);   // 0.4.18: anche il BOG, con il modello approssimato
            inp.Children.Add(bb);
            inp.Children.Add(Nota(k == "bog"
                ? "⚠ APPROSSIMAZIONE. Il BOG e' un filo isolato appoggiato al suolo: il NEC-2 non tratta fili sul terreno, interrati o isolati (serve il NEC-4). " +
                  "YA-NEC lo calcola come filo nudo alla quota minima che il terreno di Sommerfeld accetta, lambda/1000 (16 cm a 160 m, 8 cm a 80 m, 4 cm a 40 m). " +
                  "Lobo, direzione, RDF e lunghezza migliore sono indicativi; l'impedenza e le perdite del modello NON sono quelle del BOG reale (200-300 ohm misurati, N4DJ e K1FZ): " +
                  "la terminazione si regola sul campo. Sotto lambda/1000 il NEC-2 esce dal suo campo di validita' (impedenza che cresce senza limite). Spiegazione completa nel manuale, capitolo 11 Terreni, \"Antenne a terra: il BOG e i fili vicini al suolo\"."
                : "Crea il modello NEC (fili, discese, carico, sorgente) e lo calcola con il terreno di Sommerfeld: diagramma 3D a passo 5 gradi, poi guadagno medio e RDF. Verifica con nec2c GN 2: Beverage 175 m RDF 10,55 dB (IV3PRK 10,81), verticale di riferimento 5,06 dB (IV3PRK 5,06-5,07)."));

            Func<RxParams> leggi = () =>
            {
                RxParams p = new RxParams
                {
                    F = ParseNum(tf.Text, fb[banda]), L = ParseNum(tL.Text, d0.L), H = ParseNum(tH.Text, d0.H), Rterm = ParseNum(tR.Text, d0.R),
                    Rrod = ParseNum(tRod.Text, 20), Dir = ParseNum(tDir.Text, 0), H2 = ParseNum(tH2.Text, d0.H2), D = ParseNum(tD.Text, d0.D),
                    Vf = ParseNum(tVf.Text, d0.Vf), Angle = ParseNum(tAng.Text, d0.Ang),
                    Tl1 = ParseNum(tT1.Text, 4), Tl2 = ParseNum(tT2.Text, 3.25), Zl = ParseNum(tZl.Text, 100), Ratio = Math.Max(0.01, ParseNum(tRat.Text, 6)), VfLinea = Math.Max(0.05, ParseNum(tVfl.Text, 0.66))
                };
                if (schiera && !string.IsNullOrWhiteSpace(tPh.Text)) p.Fase = ParseNum(tPh.Text, double.NaN);
                int gi = Math.Max(0, cTer.SelectedIndex);
                p.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = Grounds[gi].Item2, Sigma = Grounds[gi].Item3 };
                return p;
            };
            Func<RxParams, string> deck = p => k == "beverage" ? RxAntennas.Beverage(p) : k == "bog" ? RxAntennas.Bog(p) : k == "ewe" ? RxAntennas.Ewe(p) : k == "dhdl" ? RxAntennas.Dhdl(p) : k == "looprx" ? RxAntennas.LoopRx(p) :
                k == "flag" ? RxAntennas.Flag(p) : k == "pennant" ? RxAntennas.Pennant(p) : k == "wf" ? RxAntennas.WallerFlag(p) : k == "dhdl2" ? RxAntennas.DhdlBroadside(p) :
                k == "dipbasso" ? RxAntennas.DipoloBasso(p) : k == "k9ay" ? RdfRicezione.K9ay(p, true, p.H, p.L / 2, p.H2) : k == "4sqrx" ? RdfRicezione.FourSquareRx(p, false, p.L, p.H) :
                k == "hiz" ? (nEl == 2 ? RdfRicezione.Cardioide2(p, false, p.L, p.H) : nEl == 3 ? RdfRicezione.Triangolo3(p, false, p.L, p.H) : RdfRicezione.Cerchio8(p, false, p.L, p.H)) : RxAntennas.VerticaleRef(p);
            Action<string, string, string, bool> card = (t, v, s, hl) => cardsW.Children.Add(Riquadretto(t, v, s, hl));

            Action formuleAgg = () =>
            {
                RxParams p = leggi();
                double lam = 299.792458 / p.F;
                formule.Children.Clear();
                avv.Children.Clear();
                lLam.Text = "= " + FN(p.L / lam, 2) + " lambda";
                if (k == "bog")
                {
                    double lim = lam / 1000;
                    lQuotaBog.Text = p.H < lim - 1e-6
                        ? "⚠ sotto lambda/1000 = " + FN(lim, 3) + " m: fuori dal campo del NEC-2 (impedenza non attendibile)"
                        : "lambda/1000 = " + FN(lim, 3) + " m (minimo del NEC-2); il BOG reale sta sul suolo";
                }
                List<string> righe = new List<string>();
                if (filo)
                {
                    double lo = RxAntennas.LengthForAngle(lam, p.Vf, p.Angle);
                    righe.Add("Lunghezza per la massima risposta a " + FN(p.Angle, 0) + " gradi: L = lambda / (4 (1/VF - cos theta)) = " + FN(lo, 1) + " m (" + FN(lo / lam, 2) + " lambda)");
                    if (k == "beverage")
                    {
                        double z0 = RxAntennas.WireOverGroundZ0(Math.Max(0.1, p.H), p.F > 0 ? 0.0016 : 0.0016);
                        righe.Add("Impedenza del filo sul terreno Z0 = 138 log10(4h/d) = " + FN(z0, 0) + " ohm -> terminazione circa " + FN(z0, 0) + " ohm, trasformatore " + (z0 > 550 ? "9:1" : "7,56:1 o 9:1") + " su 50-75 ohm");
                    }
                    else righe.Add("Impedenza tipica del BOG 200-300 ohm: terminazione 160-300 ohm (picchetti in serie), trasformatore da 4:1 a 9:1 (N4DJ)");
                }
                if (k == "ewe") righe.Add("EWE di Floyd Koontz WA2WVL (QST, febbraio 1995): a 160 m 9,1 m x 3 m, terminazione circa 850 ohm, trasformatore 16:1; il lobo si cerca con il NEC (la terminazione si regola per il F/B)");
                if (dh) righe.Add("DHDL di AA7JV (TX3A) e N4IS con le misure di IV3PRK (2013): verticali 6,5 m alle estremita', orizzontali 10 + 10 m a 2 m dal suolo, fili inclinati che si incrociano al centro senza toccarsi; " +
                    "carico 1250-1300 ohm (massimo F/B), Z circa 900 ohm: trasformatore 12:1 su 75 ohm (2:7 spire su BN73-202), 9:1 su 100 ohm in doppino (2:6). TX3A: -28 dB, F/B 33 dB, RDF 10 dB. YA-NEC (Sommerfeld, 13; 5 mS/m): -31,9 dBi, RDF 10,0 dB." +
                    (k == "dhdl2" ? " Due DHDL affiancate in fase con linee uguali: IV3PRK RDF 12,7 dB a 90 m (apertura 52 gradi, lobo pulito), 13,7 oltre 120 m con lobi laterali; YA-NEC 12,6 dB a 90 m." : ""));
                if (k == "pennant") righe.Add("Pennant di K6SE (pagine di WA1ION, dati IV3PRK): triangolo 8,84 x 4,27 m (29 x 14 piedi), resistenza circa 950 ohm al centro del lato verticale, trasformatore sulla punta in alto; il lobo va verso la punta. Rispetto alla Flag ha un filo in meno e circa 6 dB di guadagno in meno, con RDF quasi uguale (IV3PRK 7,8 dB; nec2c 7,6 dB). Il carico si regola per il F/B (pulsante \"ottimizza\").");
                if (k == "flag") righe.Add("Flag di K6SE (rotativa di W7IUV, ricostruita da IV3PRK nel 2007): 8,84 x 4,28 m, resistenza 937 ohm, trasformatore 3+12 spire su BN73-202, choke 12 spire di RG58 su due FT140-J. Guadagno circa -30 dB, RDF 7,8-8,0 dB (IV3PRK con la torre TX disaccordata). Il carico si regola per il F/B (pulsante \"ottimizza\").");
                if (k == "wf") righe.Add("Waller Flag (NX4D, N4IS; modello IV3PRK 2007): due anelli 4,27 x 2 m nello stesso piano, carichi 600 ohm, trasformatori 600/100, linee da 100 ohm (due RG58, VF 0,66) TL1 4 m all'anello davanti e TL2 3-7 m incrociata a quello dietro. " +
                    "IV3PRK: -58 dB, RDF 11,84 dB con TL2 = 3 m; allungando TL2 sale il F/B e scende l'RDF. Servono due preampli (segnale circa -55 dB). Costruzione IV3PRK: carichi 580/605 ohm, linee 7,20 / 6,70 m con i choke (+2,60 m elettrici).");
                if (k == "looprx")
                {
                    double A = Math.PI * p.D * p.D / 4, rr = 31171 * A * A / Math.Pow(lam, 4), he = 2 * Math.PI * A / lam;
                    righe.Add("Loop piccolo: Rr = 31171 A^2/lambda^4 = " + FN(rr * 1000, 3) + " mohm; altezza efficace h = 2 pi A / lambda = " + FN(he * 100, 1) + " cm (segnale proporzionale ad A)");
                }
                if (k == "dipbasso") righe.Add("Dipolo orizzontale basso: a meno di lambda/4 dal suolo il lobo va verso l'alto. E' il riferimento \"a lobo alto\" della graduatoria di IV3PRK (RDF 7,3 dB): si confronta con le riceventi a lobo basso.");
                if (k == "k9ay") righe.Add("K9AY di Gary Breed K9AY (QST, settembre 1997): due loop verticali a 90 gradi sullo stesso palo, resistenza di terminazione (390 ohm tipici, si regola per il F/B), trasformatore 9:1. Qui un loop solo, rivolto verso la direzione scelta, chiuso sopra il suolo come nella pagina Ricezione (RDF). Hi-Z: RDF 7,2 dB.");
                if (k == "4sqrx") righe.Add("4-square in ricezione: quattro verticali corte attive ai vertici di un quadrato di lato lambda/8, fuoco lungo la diagonale. Fase vuota = nullo posteriore esatto (pi + beta d tra le file): da' la direttivita' massima teorica; le reti di fase reali (ON4UN, IV3PRK) usano fasi diverse e danno un RDF piu' basso (IV3PRK 9,4 dB). Il pulsante cerca la fase con l'RDF migliore.");
                if (k == "hiz") righe.Add("Schiere di verticali corte attive come quelle di Hi-Z Antennas: 2 elementi in end-fire (cardioide, lambda/8), 3 a triangolo (lato lambda/6, un vertice verso la direzione scelta), 8 in cerchio (diametro 0,37 lambda: si usano le quattro verso la direzione e quella opposta). Hi-Z: 8 in cerchio RDF 13,4 dB con il suo combinatore; il modello semplice a due file resta piu' basso. Il pulsante cerca la fase con l'RDF migliore.");
                if (k == "vert") righe.Add("Verticale lambda/4 (71,3/f) con 4 radiali sopraelevati a 0,5 m: e' il riferimento della graduatoria (IV3PRK: RDF 5,06-5,07 dB con Sommerfeld).");
                foreach (string r in righe) formule.Children.Add(Nota(r, Pal.Fore));
                // 0.4.20 (IC8TEM 14/11/2026 16:01): fumetti gialli con gli avvisi e le approssimazioni del modello NEC
                List<string> avR = AvvisiDelMazzo(deck(p));
                if (schiera) avv.Children.Add(Nota("⚠ Le verticali partono dal terreno reale (GE 1 con il terreno di Sommerfeld): con il NEC-2 un filo collegato al terreno reale da' un'impedenza che dipende dal segmento della sorgente (guida del NEC-2, EZNEC). Diagramma e RDF sono indicativi; il collegamento con radiali rialzate e' in programma per tutte le riceventi.", Pal.Orange));
                string appr = k == "bog"
                    ? "il BOG reale e' un filo isolato appoggiato al suolo; il NEC-2 non tratta fili sul terreno, interrati o isolati e lo calcola come filo nudo alla quota lambda/1000 (" + FN(lam / 1000, 3) + " m). Lobo, direzione e RDF sono indicativi; impedenza e perdite non sono quelle del BOG reale (200-300 ohm misurati)."
                    : null;
                FumettoConAvvisi(bCalc, "Crea il modello NEC (fili, discese, carico, sorgente), lo calcola con il terreno di Sommerfeld e lo apre nella Struttura 3D.", avR, appr);
                FumettoConAvvisi(bApri, "Apre il modello NEC della scheda nella finestra principale.", avR, appr);
                if (k == "bog") FumettoConAvvisi(tH, "Quota del filo nel modello NEC (il BOG reale sta sul suolo).", null, appr);
            };
            leggiBanda = formuleAgg;
            foreach (TextBox t in new[] { tf, tL, tH, tR, tVf, tAng, tH2, tD, tT1, tT2, tZl, tRat, tVfl, tPh }) t.LostFocus += (s, e) => formuleAgg();

            Action<RxParams> calcola = null;
            calcola = p =>
            {
                cardsW.Children.Clear();
                // 1.3.0: gli avvisi del calcolo precedente si tolgono (prima restavano e si accumulavano)
                for (int ia = avv.Children.Count - 1; ia >= 0; ia--) if (Equals(avv.Children[ia].Tag, "ris")) avv.Children.RemoveAt(ia);
                card("Calcolo NEC", "in corso...", "diagramma 3D a passo 5°", false);
                bCalc.IsEnabled = bOtt.IsEnabled = false;
                string nec = deck(p);
                Task.Run(() =>
                {
                    NecModel m = NecModel.Parse(nec);
                    m.Z0 = 50;
                    RunResult r = Analysis.Run(m, m.BuildGeometry(), p.F, 5.0);
                    RdfResult rd = NoiseGt.Rdf(r.Grid, r.ThetaMax, r.PhiMax, r.GmaxDbi, true);
                    double[] bogZ0 = null;
                    if (k == "bog")
                    {
                        // 0.4.18: impedenza caratteristica del filo del modello (aperto / in corto), da confrontare con i 200-300 ohm misurati
                        System.Numerics.Complex za, zc;
                        System.Numerics.Complex z0 = RxAntennas.Z0Modello(p, RxAntennas.Bog, out za, out zc);
                        bogZ0 = new[] { z0.Real, z0.Imaginary, System.Numerics.Complex.Abs(za - zc) / Math.Max(1e-9, System.Numerics.Complex.Abs(zc)) };
                    }
                    return Tuple.Create(r, rd, bogZ0);
                }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
                {
                    bCalc.IsEnabled = bOtt.IsEnabled = true;
                    cardsW.Children.Clear();
                    if (t.IsFaulted) { card("Calcolo NEC", "errore", t.Exception.GetBaseException().Message, false); return; }
                    RunResult r = t.Result.Item1;
                    RdfResult rd = t.Result.Item2;
                    double lit = k == "bog" ? 9.0 : k == "beverage" ? 10.81 : k == "vert" ? 5.07 : k == "dhdl" ? 10.0 : k == "dhdl2" ? 12.7 : k == "flag" ? 7.9 : k == "pennant" ? 7.8 : k == "wf" ? 11.84 : k == "ewe" ? 7.7 :
                        k == "dipbasso" ? 7.3 : k == "k9ay" ? 7.2 : k == "4sqrx" ? 9.4 : k == "hiz" && nEl == 8 ? 13.4 : double.NaN;
                    string litS = k == "bog" ? "IV3PRK (BOG 63 m): 9,0" : k == "beverage" ? "IV3PRK (175 m): 10,81" : k == "vert" ? "IV3PRK: 5,07" : k == "dhdl" ? "IV3PRK/TX3A: 10" : k == "dhdl2" ? "IV3PRK (90 m): 12,7" : k == "flag" ? "IV3PRK: 7,8-8,0" : k == "pennant" ? "IV3PRK: 7,8" : k == "wf" ? "IV3PRK (TL2 3 m): 11,84" : k == "ewe" ? "graduatoria: 7,7" :
                        k == "dipbasso" ? "IV3PRK: 7,3" : k == "k9ay" ? "Hi-Z: 7,2" : k == "4sqrx" ? "IV3PRK: 9,4" : k == "hiz" && nEl == 8 ? "Hi-Z: 13,4" : "";
                    card("Guadagno", FN(r.GmaxDbi, 1) + " dBi", "a " + FN(r.ElevationOfMax, 0) + "°", false);
                    card("Guadagno medio", FN(rd.AvgGainDb, 1) + " dB", "su tutte le direzioni", false);
                    card("RDF", FN(rd.Rdf, 1) + " dB", litS, true);
                    card("DMF", FN(rd.Dmf, 1) + " dB", "ON4UN: G a 20° - media posteriore (non confrontare con l'RDF)", false);
                    card("F/B", FN(r.FrontBack, 1) + " dB", "avanti / dietro", false);
                    card("Apertura", FN(r.HpbwAz, 0) + "°", "orizzontale a -3 dB", false);
                    double zr = Math.Sqrt(r.Zin.Real * r.Zin.Real + r.Zin.Imaginary * r.Zin.Imaginary);
                    int[] rap = { 1, 4, 9, 16, 18 };
                    int best = 1;
                    foreach (int q in rap) if (Math.Abs(Math.Log(zr / (75.0 * q))) < Math.Abs(Math.Log(zr / (75.0 * best)))) best = q;
                    if (k == "wf")
                    {
                        double rt = Math.Max(0.01, ParseNum(tRat.Text, 6));
                        card("Z sul lato delle linee", FN(r.Zin.Real / rt, 0) + (r.Zin.Imaginary >= 0 ? " +j" : " -j") + FN(Math.Abs(r.Zin.Imaginary / rt), 0), "al punto comune, dopo i trasformatori", false);
                    }
                    else card("Z alla sorgente", FN(r.Zin.Real, 0) + (r.Zin.Imaginary >= 0 ? " +j" : " -j") + FN(Math.Abs(r.Zin.Imaginary), 0), best == 1 ? "diretta su 75 ohm" : "trasformatore " + best + ":1 su 75 ohm", false);
                    if (k == "bog")
                    {
                        double[] bz = t.Result.Item3;
                        if (bz != null)
                        {
                            double z0r = bz[0], z0i = bz[1], diff = bz[2];
                            card("Z0 del filo nel modello", FN(z0r, 0) + (z0i >= 0 ? " +j" : " -j") + FN(Math.Abs(z0i), 0), "radice(Z aperto x Z corto); BOG reale 200-300 ohm", false);
                            if (diff < 0.05)
                                avv.Children.Add(Ris(Nota("⚠ Z aperto e Z in corto differiscono meno del 5%: nel modello l'onda si spegne prima dell'estremo lontano, la terminazione non conta e il F/B non si regola. " +
                                    "E' il segno che il filo e' troppo vicino al terreno per il NEC-2: alzare la quota verso lambda/200 per vedere l'andamento.", Pal.Orange)));
                        }
                        avv.Children.Add(Ris(Nota("⚠ Modello approssimato (NEC-2): filo nudo a " + FN(p.H, 2) + " m invece che isolato sul suolo. Lobo, direzione e RDF indicativi; impedenza, perdite e guadagno assoluto no.", Pal.Orange)));
                    }
                    card("Rispetto alla verticale", (rd.Rdf - 5.07 >= 0 ? "+" : "") + FN(rd.Rdf - 5.07, 1) + " dB S/N", "preampli consigliato " + (r.GmaxDbi < -20 ? "+20 dB" : (r.GmaxDbi < -10 ? "+12 dB" : "non serve")), false);
                    pEl.Traces = new List<PolarTrace> { new PolarTrace { Db = r.ElCut, Color = Pal.Accent, Name = "totale" } };
                    pEl.RefDb = r.GmaxDbi; pEl.MarkAngle = r.ElevationOfMax; pEl.Title = "elevazione";
                    pAz.Traces = new List<PolarTrace> { new PolarTrace { Db = r.AzCut, Color = Pal.Accent, Name = "totale" } };
                    pAz.RefDb = r.GmaxDbi; pAz.MarkAngle = r.PhiMax; pAz.Title = Lang.F("azimut a {0}°", FN(r.ElevationOfMax, 0));
                    pEl.InvalidateVisual(); pAz.InvalidateVisual();
                    List<BarItem> it = new List<BarItem>();
                    bool messo = false;
                    foreach (Tuple<string, double> gq in RxAntennas.Graduatoria)
                    {
                        if (!messo && rd.Rdf < gq.Item2) { it.Add(new BarItem(titolo + " (YA-NEC)", rd.Rdf, Pal.Orange)); messo = true; }
                        it.Add(new BarItem(gq.Item1, gq.Item2, Color.Parse("#98A6BC")));
                    }
                    if (!messo) it.Add(new BarItem(titolo + " (YA-NEC)", rd.Rdf, Pal.Orange));
                    bars.Items = it;
                    bars.InvalidateVisual();
                    if (!double.IsNaN(lit) && Math.Abs(rd.Rdf - lit) > 1.0)
                        avv.Children.Add(Ris(Nota(Lang.F("⚠ RDF {0} dB contro {1} della fonte: controlla dimensioni, terminazione e terreno (la fonte puo' usare un terreno o una geometria diversi).", FN(rd.Rdf, 1), FN(lit, 2)), Pal.Orange)));
                }));
            };
            // 13/11/2026 18:10: il modello creato si vede anche nella Struttura 3D (le schede RDF restano qui, calcolate in sottofondo)
            bCalc.Click += (s, e) => { formuleAgg(); RxParams p = leggi(); calcola(p); ApriModelloCalcolatore(deck(p), Lang.T(titolo) + ".nec"); };
            bApri.Click += (s, e) => { RxParams p = leggi(); ApriModelloCalcolatore(deck(p), Lang.T(titolo) + ".nec"); };
            bOtt.Click += (s, e) =>
            {
                if (schiera)
                {
                    // 1.3.0: per le schiere di verticali la fase tra le file con l'RDF migliore (lobo in avanti, entro 45 gradi dalla direzione)
                    RxParams b0 = leggi();
                    bOtt.IsEnabled = false;
                    cardsW.Children.Clear();
                    card("Ottimizzazione", "in corso...", "fase tra le file da 120 a 300 gradi per l'RDF", false);
                    Task.Run(() =>
                    {
                        double bestF = double.NaN, bestR = double.MinValue;
                        for (double ph = 120; ph <= 300; ph += 10)
                        {
                            RxParams q = b0.Copia(); q.Fase = ph;
                            NecModel m = NecModel.Parse(deck(q));
                            RunResult r = Analysis.Run(m, m.BuildGeometry(), q.F, 10.0);
                            double dphi = Math.Abs(((r.PhiMax - q.Dir) % 360 + 540) % 360 - 180);
                            if (dphi > 45) continue;
                            RdfResult rd = NoiseGt.Rdf(r.Grid, r.ThetaMax, r.PhiMax, r.GmaxDbi, true);
                            if (rd.Rdf > bestR) { bestR = rd.Rdf; bestF = ph; }
                        }
                        return Tuple.Create(bestF, bestR);
                    }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
                    {
                        bOtt.IsEnabled = true;
                        if (t.IsFaulted || double.IsNaN(t.Result.Item1))
                        {
                            cardsW.Children.Clear();
                            card("Ottimizzazione", "errore", t.IsFaulted ? t.Exception.GetBaseException().Message : "nessuna fase con il lobo in avanti", false);
                            return;
                        }
                        tPh.Text = FN(t.Result.Item1, 0);
                        Status(Lang.F("Fase tra le file per l'RDF migliore: {0} gradi (RDF {1} dB). Ricalcolo...", FN(t.Result.Item1, 0), FN(t.Result.Item2, 1)));
                        calcola(leggi());
                    }));
                    return;
                }
                RxParams p0 = leggi();
                bOtt.IsEnabled = false;
                cardsW.Children.Clear();
                card("Ottimizzazione", "in corso...", k == "bog" ? "terminazione da 100 a 4000 ohm per il F/B" : "terminazione da 200 a 1500 ohm per il F/B", false);
                // 1.0.4: i parametri si leggono qui, nel thread dell'interfaccia; leggi() dentro Task.Run toccava le caselle da un altro
                // thread e l'ottimizzazione finiva subito con "Call from invalid thread"
                double rStart = p0.Rterm;
                Task.Run(() =>
                {
                    double bestR = rStart, bestFb = double.MinValue;
                    double rIni = k == "bog" ? 100 : 200, rFin = k == "bog" ? 4000 : 1500, rPas = k == "bog" ? 100 : 50;   // 0.4.18: il modello del BOG ha Z0 alta
                    for (double rr = rIni; rr <= rFin; rr += rPas)
                    {
                        RxParams q = p0;
                        q.Rterm = rr;
                        NecModel m = NecModel.Parse(deck(q));
                        RunResult r = Analysis.Run(m, m.BuildGeometry(), q.F, 10.0);
                        if (r.FrontBack > bestFb) { bestFb = r.FrontBack; bestR = rr; }
                    }
                    return Tuple.Create(bestR, bestFb);
                }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
                {
                    bOtt.IsEnabled = true;
                    if (t.IsFaulted) { cardsW.Children.Clear(); card("Ottimizzazione", "errore", t.Exception.GetBaseException().Message, false); return; }
                    tR.Text = FN(t.Result.Item1, 0);
                    Status("Terminazione per il massimo F/B: " + FN(t.Result.Item1, 0) + " ohm (F/B " + FN(t.Result.Item2, 1) + " dB). Ricalcolo...");
                    calcola(leggi());
                }));
            };
            formuleAgg();

            // impaginazione come nel bozzetto: dati e diagrammi sopra, riquadri, graduatoria e spiegazione sotto
            Grid top = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*") };
            Border pi = Pannello(titolo, inp);
            Grid diag = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Height = 300 };
            Grid.SetColumn(pAz, 1);
            diag.Children.Add(pEl); diag.Children.Add(pAz);
            Border pd = Pannello("Diagrammi", diag);
            Grid.SetColumn(pd, 2);
            top.Children.Add(pi); top.Children.Add(pd);
            StackPanel all = new StackPanel();
            all.Children.Add(top);
            all.Children.Add(cardsW);
            all.Children.Add(avv);
            Grid bottom = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,10,2*") };
            Border pb = Pannello("Graduatoria RDF (160 m) - in arancione il calcolo YA-NEC", bars);
            Border pc = Pannello("Cos'e' l'RDF", Nota("RDF = guadagno nella direzione voluta - guadagno medio su tutte le direzioni (W8JI; prima chiamato SNIF, anni '60). " +
                "Misura di quanto l'antenna migliora il rapporto segnale/rumore se il rumore arriva uniforme da ogni direzione; il guadagno assoluto non conta (con un preampli si recupera). " +
                "Media come EZNEC: emisfero superiore diviso per tutta la sfera (4 pi). Con questa convenzione i calcoli tornano con IV3PRK e W8JI/Hi-Z (verticale 5,07 contro 5,06 e 4,9; " +
                "Beverage 350 m 13,5 contro 13,66). Limite: il rumore reale non e' uniforme (linee elettriche, QRM locale): per questo contano anche F/B e la forma del lobo. " +
                "Per le antenne trasmittenti l'RDF non ha senso: per questo si calcola solo in questa linguetta. " +
                "DMF (Directivity Merit Figure, ON4UN): guadagno a 20 gradi di elevazione nella direzione avanti meno il guadagno medio della semisfera posteriore (azimut oltre 90 gradi, " +
                "sopra il terreno, con il peso del coseno per l'area): dice quanto l'antenna scarta il rumore che arriva da dietro. ON4UN avverte di non confrontare DMF e RDF tra loro. Fonti: W8JI e W8WWV (RDF), ON4UN Low-Band DXing cap. 7 (DMF, Tab. 7-22, 7-24, 7-25), IV3PRK, IK0XBX.", Pal.Fore));
            Grid.SetColumn(pc, 2);
            bottom.Children.Add(pb); bottom.Children.Add(pc);
            all.Children.Add(bottom);
            List<BarItem> it0 = new List<BarItem>();
            foreach (Tuple<string, double> gq in RxAntennas.Graduatoria) it0.Add(new BarItem(gq.Item1, gq.Item2, Color.Parse("#98A6BC")));
            bars.Items = it0;
            return new ScrollViewer { Content = all };
        }
    }
}
