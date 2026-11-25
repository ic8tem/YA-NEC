// YA-NEC - linguetta "Calcolatori": fascione largo con le voci descritte e i calcolatori delle forme semplici
// secondo il bozzetto approvato alla lettera (dati con pillole, formule e fonti, pulsanti, disegno a destra,
// riquadri dei risultati con la verifica NEC, grafico di un parametro, avvisi), e il calcolatore della four-square
// con la grafica del bozzetto approvato (vista 3D con le fasi, tabella delle sorgenti con la Z risultante, diagrammi polari).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Polygon = Avalonia.Controls.Shapes.Polygon;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;
using Shape = Avalonia.Controls.Shapes.Shape;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Fascione fasCalc;
        ContentControl hostCalc;
        TextBlock calcTitolo;
        ContentControl calcCorpo;
        Control calcGriglia, calcRx;
        string calcFam = "HF";
        readonly Dictionary<string, Button> calcFamBtn = new Dictionary<string, Button>();
        readonly Dictionary<string, string> calcUltima = new Dictionary<string, string>();
        /// <summary>1.1.0: famiglie dei Calcolatori e loro voci. 1.5.2: nuovo ordine chiesto da IC8TEM il 19/11/2026 17:06
        /// (schema numerato 1-7 sull'albero delle linguette): HF, Antenne riceventi, VHF, Strumenti, Dischi e illuminatori, 2,4 GHz, Calcoli meccanici.</summary>
        // 1.6.0: «Suggerimenti» in testa (bozzetto «spunti dalla ricerca su GitHub», proposta 1, approvato da IC8TEM il 23/11/2026 14:27)
        public static readonly string[] CalcFamiglie = { "Suggerimenti", "HF", "Antenne riceventi", "VHF", "Strumenti", "Dischi e illuminatori", "2,4 GHz", "Calcoli meccanici" };
        Control calcMeccPagina;   // 1.5: la pagina dei Calcoli meccanici (prima linguetta principale) dentro i Calcolatori
        Control calcSugg;         // 1.6.0: pagina dei Suggerimenti
        string calcVarianteVoluta;
        static readonly Dictionary<string, string[]> CalcVoci = new Dictionary<string, string[]>
        {
            { "HF", new[] { "dipoli", "filari", "dipmb", "yagihf", "yagimb", "yagibib", "verticali", "vert43", "vertmb", "4sq", "delta", "looprett", "loopmag", "quad", "eh", "moxon", "hexbeam", "skywire", "nvis", "carichihf", "caricolin" } },   // 1.7.0: yagihf, yagibib, caricolin
            { "VHF", new[] { "hb9cv", "bigwheel", "hen", "lpda", "yagidl6wu", "quagi", "loopyagi", "boom", "divisori", "yagiincr", "eggbeater", "qfh", "discone" } },   // 1.5.6: Discone da HF a VHF (IC8TEM 21/11/2026)
            { "Antenne riceventi", new string[0] },
            { "Suggerimenti", new string[0] },
            { "Strumenti", new[] { "ridim", "tetto", "retelc", "forcina", "gamma", "stub", "rrad", "cavi", "toroidiT", "toroidiFT", "balun", "staffe" } },   // 1.7.0: gamma, stub
            { "Calcoli meccanici", new string[0] },
            { "Dischi e illuminatori", new[] { "parabola", "offset", "trombe", "biquad" } },
            { "2,4 GHz", new[] { "pcb24" } },
        };
        /// <summary>Famiglia di una voce dei Calcolatori (null se non c'e').</summary>
        public static string FamigliaDi(string voce)
        {
            foreach (KeyValuePair<string, string[]> kv in CalcVoci) if (Array.IndexOf(kv.Value, voce) >= 0) return kv.Key;
            return null;
        }
        readonly Dictionary<string, Control> calcPages = new Dictionary<string, Control>();
        List<SimpleCalc> simpleCalcs;

        Control BuildCalcolatoriPage()
        {
            simpleCalcs = SimpleCalc.All();
            try { CalcCavi.Lista = Cables.Load(Path.Combine(AppPaths.DataDir, "cavi.csv")); } catch { }
            calcPages.Clear();
            fasCalc = new Fascione();
            // 1.1.0 (bozzetto approvato da IC8TEM il 17/11/2026 19:17): le famiglie stanno in una fascia orizzontale sotto le linguette;
            // il fascione mostra solo le voci della famiglia scelta. "Forme semplici" diventa "HF"; la linguetta "Antenne riceventi" diventa
            // una famiglia; "Distanze di stack DL6WU" passa alla sopralinguetta Rumore/GT/Stack.
            calcTitolo = new TextBlock { Text = "HF", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(2, 0, 0, 6) };
            fasCalc.Children.Add(calcTitolo);
            fasCalc.Voce("dipoli", "Dipoli", false, null, "mezz'onda, V invertita, ripiegato, trappolato, OCF/Windom, G5RV, T2FD, banda larga, multibanda a rele'", true);
            fasCalc.Voce("filari", "Filari", false, null, "alimentate in punta: EFHW 49:1 / 64:1, con bobina, filo non risonante 9:1 / 4:1; contrappeso, choke; risonanze in armonica (EFLW)");   // 1.5.6 (bozzetto approvato da IC8TEM 21/11/2026 21:12)
            fasCalc.Voce("dipmb", "Dipolo multibanda", false, null, "bacchette parallele, manica aperta, radiatori XR-5");   // 0.4.30
            fasCalc.Voce("yagihf", "Yagi HF 2-3 elementi", false, null, "valori di partenza e guida di taratura (I4MFA)");   // 1.7.0
            fasCalc.Voce("yagimb", "Yagi multibanda", false, null, "HF e inizio VHF: radiatori multibanda + riflettore o direttore per banda");   // 0.4.30
            fasCalc.Voce("yagibib", "Yagi bibanda interlacciata", false, null, "due bande sullo stesso boom, una o due linee (W1JR)");   // 1.7.0
            fasCalc.Voce("verticali", "Verticali", false, null, "lambda/4 e 5/8, ground plane, radiali, caricate con bobina, J-pole, trappolata multibanda, L invertita / T, half-sloper", true);
            fasCalc.Voce("vert43", "Verticale non risonante (43 piedi)", false, null, "un-un 4:1, accordatore; perdite nel cavo, tensione alla base");   // 1.5.9 (bozzetto approvato da IC8TEM 22/11/2026 21:22)
            fasCalc.Voce("vertmb", "Verticale multibanda (DX Commander)", false, null, "elementi a ventaglio, terreno suo, verifica per banda");   // 0.4.30
            fasCalc.Voce("4sq", "Four-square", false, null, "quattro verticali sfasati (ON4UN)");
            fasCalc.Voce("delta", "Delta loop", false, null, "equilatera, schiacciata, angolo di alimentazione");
            fasCalc.Voce("looprett", "Loop rettangolare", false, null, "lati e alimentazione");
            fasCalc.Voce("loopmag", "Loop magnetica", false, null, "piccolo loop con condensatore");
            fasCalc.Voce("quad", "Quad", false, null, "1-2-3 elementi, multibanda (DK7ZB)", true);
            fasCalc.Voce("eh", "Antenna EH", false, null, "con avviso sulle prestazioni", true);
            fasCalc.Voce("moxon", "Moxon", false, null, "gia' approvato (Cebik / DK7ZB)");
            fasCalc.Voce("hexbeam", "Hexbeam 20-10 m", false, null, "G3TXQ, misure JN1GLB, accordo a 50 Ω");   // 0.4.30
            fasCalc.Voce("skywire", "Loop orizzontale multibanda", false, null, "Loop Skywire (W7FB, NT0Z): tutte le bande, scala o coassiale, perdita della discesa");   // 1.0.17
            fasCalc.Voce("nvis", "NVIS", false, null, "dipolo, V invertito, loop 1 λ; altezza ottima, schermo o 9 fili a terra (Cebik)");   // 1.5
            fasCalc.Voce("caricolin", "Carico lineare", false, null, "λ/4 e dipolo accorciato; confronto con bobina e cappello (IU5HIV, I5WHC)");   // 1.7.0
            fasCalc.Voce("carichihf", "Carichi HF", false, null, "trappole LC e coassiali, bobine con autorisonanza, linear loading, cappelli; Q, tensioni e perdite per banda");   // 1.2.0
            fasCalc.Voce("hb9cv", "HB9CV", false, null, "due elementi alimentati sfasati");
            fasCalc.Voce("bigwheel", "Big Wheel", false, null, "144-432 MHz, polarizzazione orizzontale, omnidirezionale; i due livelli e i tre dipoli");   // 1.0.17
            fasCalc.Voce("hen", "HEN (loop giapponese)", false, null, "loop a \"J\" di JA");
            fasCalc.Voce("lpda", "Log-periodica (LPDA)", false, null, "metodo Carrel, linea incrociata");
            fasCalc.Voce("yagidl6wu", "Yagi lunga DL6WU", false, null, "VHF e UHF: progetto DL6WU, quattro alimentazioni, ottimizzazione a passi");   // 0.5.0
            fasCalc.Voce("quagi", "Quagi e quad a piu' elementi", false, null, "N6NB e Cebik, ottimizzazione a passi");   // 0.5.0
            fasCalc.Voce("loopyagi", "Loop Yagi 1296 MHz", false, null, "tutta ad anelli: N6CA (ARRL) e G3JVL, ottimizzazione a passi");   // 0.5.0
            fasCalc.Voce("boom", "Correzione del boom", false, null, "DL6WU/G3SEK, SM5BSZ, DG7YBN");
            fasCalc.Voce("divisori", "Divisori e cavi di fase", false, null, "λ/4 in tubo quadro o tondo, in cavo, Wilkinson; cavi di fase, tilt; disegno 1:1");   // 1.2.0 (bozzetto VHF 5)
            fasCalc.Voce("yagiincr", "Yagi incrociate", false, null, "X o +, piani sfalsati di λ/4; H, V, RHCP, LHCP, X-pol; rapporto assiale e isolamento");   // 1.2.0 (bozzetto VHF 6)
            fasCalc.Voce("eggbeater", "Eggbeater", false, null, "due spire incrociate, riflettore a radiali (F5VIF, tamago)");
            fasCalc.Voce("qfh", "QFH (quadrifilare a elica)", false, null, "spira grande e piccola a mezzo giro (Coppens)");
            fasCalc.Voce("discone", "Discone", false, null, "disco, cono, angolo");   // 1.5.6: spostata da HF a VHF
            fasCalc.Voce("ridim", "Ridimensiona antenna", false, null, "ora in Variabili e ottimizzazione > Ridimensiona e ricentra");   // 1.5.3
            fasCalc.Voce("tetto", "Antenna su tetto (solaio)", false, null, "modello aperto: terreno vero sotto il fabbricato o solaio sotto l'antenna");   // 0.5.0
            fasCalc.Voce("retelc", "Rete LC di adattamento", false, null, "L, Pi, T, con i valori");
            fasCalc.Voce("forcina", "Forcina (beta / hairpin)", false, null, "adattamento simmetrico del pilotato a 50 Ω (Cebik)");   // 1.5
            fasCalc.Voce("gamma", "Gamma match", false, null, "barra, distanza, condensatore in serie (Healey)");   // 1.7.0
            fasCalc.Voce("stub", "Adattamento a stub", false, null, "stub singolo e doppio, carta di Smith (IW2FND)");   // 1.7.0
            fasCalc.Voce("rrad", "Resistenza di radiazione", false, null, "dipolo corto, monopolo, loop", true);
            fasCalc.Voce("cavi", "Cavi RG", false, null, "dal nostro abaco cavi");
            fasCalc.Voce("toroidiT", "Toroidi iron powder (T)", false, null, "AL, spire, banda", true);
            fasCalc.Voce("toroidiFT", "Toroidi ferrite (FT)", false, null, "AL, spire, impedenza", true);
            fasCalc.Voce("balun", "Balun / Un-Un", false, null, "rapporti e avvolgimenti", true);
            fasCalc.Voce("staffe", "Staffe di fissaggio", false, null, "piastre al boom: compensazione e modello (ARRL, Leeson)");   // 0.4.30
            // 1.2.0: la Meccanica alla YagiStress (bozzetto HF 9) andra' in Calcoli meccanici > Sollecitazioni elementi quando IC8TEM approva il bozzetto 13
            fasCalc.Voce("parabola", "Parabola primo fuoco", false, null, "1296 / 2320 / 5760 / 10368 MHz, meccanica, lobi, EME");
            fasCalc.Voce("offset", "Parabola offset", false, null, "geometria, dime, illuminazione asimmetrica, lobi H e V, EME");
            fasCalc.Voce("trombe", "Trombe e illuminatori", false, null, "coffee-can, VE4MA, Chaparral, W2IMU, conica, piramidale; modello NEC");   // 0.4.19
            fasCalc.Voce("biquad", "Biquad e doppia biquad", false, null, "1296-5760 MHz, riflettore pieno o a rete, feed per parabola");   // 1.0.16
            fasCalc.Voce("pcb24", "Antenne su PCB", false, null, "IFA, MIFA, dipolo, Yagi, Big Wheel; disegno 1:1");
            hostCalc = new ContentControl();
            fasCalc.Selected += k =>
            {
                Control c;
                if (!calcPages.TryGetValue(k, out c))
                {
                    c = k == "ridim" ? BuildRimandoRicentra() : k == "carichihf" ? BuildCarichiHfPanel() : k == "divisori" ? BuildDivisoriPanel() : k == "yagiincr" ? BuildYagiIncrociatePanel() : k == "meccanica" ? BuildMeccanicaPanel() : k == "lpda" ? BuildLpdaPanel() : k == "4sq" ? BuildFourSquarePanel() : k == "boom" ? BuildBoomPanel() : k == "staffe" ? BuildStaffePanel() : k == "vertmb" ? BuildVerticaleMbPanel() : k == "pcb24" ? BuildPcb24Panel() : k == "dipmb" ? BuildDipoloMbPanel() : k == "yagimb" ? BuildYagiMbPanel() : k == "parabola" ? CreaPannelloParabola() : k == "trombe" ? new TrombaPanel { ApriNec = ApriModelloCalcolatore } : k == "offset" ? CreaPannelloOffset() : BuildGruppoCalc(k);
                    calcPages[k] = c;
                }
                hostCalc.Content = c;
            };
            fasCalc.Selected += k => { string f = FamigliaDi(k); if (f == null) return; calcUltima[f] = k; if (f != calcFam && calcCorpo != null) ApriFamigliaCalc(f, k); };
            // il fascione puo' arrivare al 30 % (IC8TEM): qui il 26 %
            calcGriglia = Fascione.PaginaLarga(fasCalc, hostCalc, 26);
            // fascia delle famiglie: colore diverso dalle linguette, famiglia scelta in arancio (bozzetto approvato)
            WrapPanel fp = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (string fn in CalcFamiglie)
            {
                string nome = fn;
                Button b = new Button { Content = nome, Tag = "famiglia:" + nome, Padding = new Thickness(14, 4), Margin = new Thickness(2, 2), CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(1) };
                b.Click += (s2, e2) => ApriFamigliaCalc(nome);
                calcFamBtn[nome] = b;
                fp.Children.Add(b);
            }
            Border fascia = new Border { Background = new SolidColorBrush(Color.Parse("#2F4A66")), BorderBrush = Pal.B(Pal.Accent), BorderThickness = new Thickness(0, 2, 0, 0), Padding = new Thickness(8, 4), Child = fp };
            calcCorpo = new ContentControl();
            DockPanel dp = new DockPanel();
            DockPanel.SetDock(fascia, Dock.Top);
            dp.Children.Add(fascia);
            dp.Children.Add(calcCorpo);
            calcUltima["HF"] = "loopmag";
            ApriFamigliaCalc("HF");
            return dp;
        }

        /// <summary>1.1.0: apre una famiglia dei Calcolatori (e una voce, se data). "Antenne riceventi" mostra la pagina delle riceventi.</summary>
        public void ApriFamigliaCalc(string fam, string voce = null)
        {
            if (!CalcVoci.ContainsKey(fam)) return;
            calcFam = fam;
            foreach (KeyValuePair<string, Button> kv in calcFamBtn)
            {
                bool on = kv.Key == fam;
                kv.Value.Background = new SolidColorBrush(on ? Color.Parse("#E08A00") : Color.Parse("#2F4A66"));
                kv.Value.BorderBrush = new SolidColorBrush(on ? Color.Parse("#E08A00") : Color.Parse("#4A6787"));
                kv.Value.Foreground = new SolidColorBrush(Color.Parse(on ? "#FFFFFF" : "#E6EDF5"));
                kv.Value.FontWeight = on ? FontWeight.SemiBold : FontWeight.Normal;
            }
            if (fam == "Calcoli meccanici")
            {
                calcCorpo.Content = calcMeccPagina;
                // 1.5.1 (difetto trovato registrando la lezione 25): la pagina nasce all'avvio, prima del modello; aprendo la famiglia si
                // ricalcola la voce attiva, altrimenti il Vento restava senza gli elementi del modello finche' non si cliccava di nuovo
                ApriVoceMeccanica(voce ?? meccVoceAttiva);
                return;
            }
            if (fam == "Suggerimenti")
            {
                if (calcSugg == null) calcSugg = BuildSuggerimentiPage();
                calcCorpo.Content = calcSugg;
                return;
            }
            if (fam == "Antenne riceventi")
            {
                if (calcRx == null) calcRx = BuildRiceventiPage();
                calcCorpo.Content = calcRx;
                if (voce != null && fasRx != null) fasRx.Select(voce);
                return;
            }
            string[] l = CalcVoci[fam];
            foreach (Control c in fasCalc.Children)
                if (c is Border bv && bv.Tag is string k) bv.IsVisible = Array.IndexOf(l, k) >= 0;
            calcTitolo.Text = fam;
            calcCorpo.Content = calcGriglia;
            string scelta = voce;
            if (scelta == null || Array.IndexOf(l, scelta) < 0) { string u; scelta = calcUltima.TryGetValue(fam, out u) ? u : l[0]; }
            if (fasCalc.Current != scelta) fasCalc.Select(scelta);
        }

        // 1.5.6: pulsanti di rimando tra i Calcolatori (bozzetto Filari approvato da IC8TEM il 21/11/2026 21:12)
        string ritornoPer, ritornoVoce, ritornoTitolo;
        void ApriRimando(CalcRimando r, SimpleCalc da)
        {
            SimpleCalc dest = simpleCalcs != null ? simpleCalcs.Find(c => c.Key == r.Voce) : null;
            if (dest != null)
            {
                foreach (KeyValuePair<string, double> kv in r.Valori) { CalcParam p = dest.Par(kv.Key); if (p != null) p.Value = kv.Value; }
                dest.Scale = 1;
            }
            string voce = dest != null ? (dest.Group ?? dest.Key) : r.Voce;
            calcVarianteVoluta = dest != null && dest.Group != null ? dest.Key : null;   // 1.6.0: apre la variante giusta (V invertita nei Dipoli)
            if (da == null) { ritornoPer = null; }
            else { ritornoPer = voce; ritornoVoce = da.Group ?? da.Key; ritornoTitolo = da.Nome; }
            calcPages.Remove(voce);
            string fam = FamigliaDi(voce);
            if (fam == null) return;
            bool giaAperta = fasCalc != null && fasCalc.Current == voce;
            ApriFamigliaCalc(fam, voce);
            if (giaAperta) fasCalc.Select(voce);   // 1.6.0: la pagina si rifa' con i valori nuovi anche se la voce era gia' quella aperta
            Registro.Scrivi("AZIONE", "rimando dei Calcolatori: " + (da != null ? da.Key : "suggerimenti") + " -> " + voce);
        }

        // voce del fascione con piu' varianti: pillole "Tipo" in alto e il calcolatore sotto
        Control BuildGruppoCalc(string key)
        {
            List<SimpleCalc> l = simpleCalcs.FindAll(c => c.Group == key);
            if (l.Count == 0) return Pannello("Calcolatore", Nota("Non disponibile."));
            if (l.Count == 1) return BuildSimpleCalcPanel(l[0], null);
            ContentControl cc = new ContentControl();
            Dictionary<int, Control> cache = new Dictionary<int, Control>();
            int i0 = calcVarianteVoluta != null ? Math.Max(0, l.FindIndex(c => c.Key == calcVarianteVoluta)) : 0;   // 1.6.0
            calcVarianteVoluta = null;
            StackPanel tipo = Scelta(l.Select(c => c.Variant).ToArray(), i0, k => { });
            cache[i0] = BuildSimpleCalcPanel(l[i0], tipo);
            cc.Content = cache[i0];
            // le pillole "Tipo" stanno nella riga "Tipo" del pannello visibile: alla scelta si spostano nel pannello della variante
            for (int i = 0; i < tipo.Children.Count; i++)
            {
                Button bb = tipo.Children[i] as Button;
                if (bb == null) continue;
                int k = i;
                bb.Click += (s, e) =>
                {
                    Control x;
                    if (!cache.TryGetValue(k, out x)) { x = BuildSimpleCalcPanel(l[k], null); cache[k] = x; }
                    SpostaPillole(tipo, x);
                    cc.Content = x;
                };
            }
            return cc;
        }

        // le pillole della variante vanno nella riga "Tipo" del pannello visibile
        static void SpostaPillole(StackPanel tipo, Control pannello)
        {
            ContentControl posto = FindTag(pannello, "posto-tipo") as ContentControl;
            if (posto == null) return;
            if (tipo.Parent is ContentControl vecchio) vecchio.Content = null;
            posto.Content = tipo;
        }

        static Control FindTag(Control c, string tag)
        {
            if (c == null) return null;
            if (c.Tag as string == tag) return c;
            if (c is Panel p) foreach (Control x in p.Children) { Control r = FindTag(x, tag); if (r != null) return r; }
            if (c is Decorator d) return FindTag(d.Child as Control, tag);
            if (c is ContentControl cc) return FindTag(cc.Content as Control, tag);
            return null;
        }

        static TextBlock Muto(string s)
        {
            return new TextBlock { Text = s ?? "", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, MaxWidth = 340 };
        }

        Border Scheda(Control corpo)
        {
            return new Border
            {
                Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10), Margin = new Thickness(0, 0, 0, 10), Child = corpo
            };
        }

        // ------------------------------------------------------------------------------------------------
        // calcolatore delle forme semplici, impaginato come il bozzetto approvato
        // ------------------------------------------------------------------------------------------------
        Control BuildSimpleCalcPanel(SimpleCalc sc, StackPanel pilloleTipo)
        {
            CalcOutput first;
            sc.Model = model;
            try { first = sc.Run(); } catch { first = new CalcOutput(); }

            StackPanel root = new StackPanel();
            // 0.5.3 (bozzetto C approvato da IC8TEM il 15/11/2026 19:55): caselle su 2-3 colonne, formule in una riga che si apre, riquadri dei
            // risultati sotto i pulsanti nella stessa scheda, disegno ridotto a destra; per Cavi RG a destra il cambio d'impedenza, per il
            // Balun il disegno di costruzione con i passi
            bool conTrasf = sc.Key == "cavi", conBalun = sc.Key == "balun";
            Grid top = new Grid { ColumnDefinitions = new ColumnDefinitions(conTrasf ? "10*,12*" : (conBalun ? "10*,11*" : (sc is CalcStackDl6wu ? "10*,9*" : "*,330"))) };   // 1.3.1: stack, disegno largo a destra
            StackPanel left = new StackPanel { Spacing = 4 };
            TextBlock titoloCalc = new TextBlock { Text = sc.Title, FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center };
            if (sc is CalcStackDl6wu)   // 1.3.1 (bozzetto approvato da IC8TEM il 18/11/2026 13:04): pulsante "? Manuale"
                left.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6), Children = { titoloCalc, BtnManuale("Distanze di stack: accoppiamento e distanza migliore") } });
            else { titoloCalc.Margin = new Thickness(0, 0, 0, 6); left.Children.Add(titoloCalc); }
            // 1.5.6: aperta da un pulsante di rimando (Filari, OCF...): "‹ Torna a ..." riporta alla scheda di partenza
            if (ritornoPer != null && ritornoPer == (sc.Group ?? sc.Key) && ritornoVoce != null)
            {
                string rv = ritornoVoce;
                Button bTorna = Btn("‹ Torna a " + Lang.T(ritornoTitolo), false);   // 1.5.6: titolo tradotto (verifica traduzioni, IC8TEM 22/11/2026)
                bTorna.HorizontalAlignment = HorizontalAlignment.Left; bTorna.Margin = new Thickness(0, 0, 0, 6);
                bTorna.Click += (s0, e0) => { string fr = FamigliaDi(rv); if (fr != null) ApriFamigliaCalc(fr, rv); };
                left.Children.Insert(left.Children.Count - 1, bTorna);
            }
            if (pilloleTipo != null || !string.IsNullOrEmpty(sc.Variant))
            {
                ContentControl posto = new ContentControl { Tag = "posto-tipo", Content = pilloleTipo };
                left.Children.Add(RigaCalc("Tipo", posto));
            }

            List<Action> leggi = new List<Action>();
            List<Action> aggiornaHint = new List<Action>();
            Dictionary<string, TextBox> campi = new Dictionary<string, TextBox>();
            Dictionary<string, ComboBox> tendine = new Dictionary<string, ComboBox>();
            WrapPanel gruppo = null;   // caselle numeriche consecutive: celle che vanno a capo (3 per riga se c'e' posto)
            Action mostra = null;
            if (sc.PuoUsareModello)
            {
                // IC8TEM 13/11/2026 18:19: casella per scegliere se i dati vengono dal modello della finestra principale
                CheckBox cbMod = new CheckBox { Content = "Prendi i dati dal modello (finestra principale)", IsChecked = sc.UsaModello, Margin = new Thickness(0, 0, 0, 4) };
                ToolTip.SetTip(cbMod, "Accesa: frequenza (barra in alto), aperture a -3 dB e boom vengono dal calcolo del modello aperto e si aggiornano da soli. Spenta: valori scritti a mano.");
                cbMod.IsCheckedChanged += (s, e) => { sc.UsaModello = cbMod.IsChecked == true; if (mostra != null) mostra(); };
                left.Children.Add(cbMod);
            }
            // 1.0.11 (bozzetti approvati da IC8TEM il 16/11/2026 23:48): schede in sezioni numerate, caselle su 2 colonne allineate,
            // parametri che si vedono solo in un modo (dipolo trappolato), scelte sulla stessa riga, tessere con la figurina (delta loop)
            bool sezioni = sc.Params.Exists(x => x.Sezione != null);
            List<Action> visib = new List<Action>();
            Grid griglia = null; int cellaGriglia = 0;
            StackPanel ultimaRigaScelte = null;
            foreach (CalcParam p in sc.Params)
            {
                CalcParam pp = p;
                int nPrima = left.Children.Count;
                Control cellaSez = null, cellaGruppo = null;
                if (pp.Sezione != null)
                {
                    gruppo = null; griglia = null; ultimaRigaScelte = null;
                    TextBlock ts = new TextBlock { Text = pp.Sezione, FontWeight = FontWeight.Bold, FontSize = 13.5, Foreground = Pal.B(Pal.Accent), Margin = new Thickness(0, 8, 0, 2) };
                    left.Children.Add(ts);
                }
                if (pp.Figure != null && pp.Choices != null)
                {
                    gruppo = null; griglia = null;
                    left.Children.Add(TessereScelta(pp, () => { sc.Scale = 1; if (mostra != null) mostra(); }));
                }
                else if (pp.Choices != null && pp.Choices.Length > 0 && (pp.Choices.Length <= 4 || pp.Pillole))
                {
                    gruppo = null; griglia = null;
                    TextBlock h = Muto(pp.Hint);
                    StackPanel sp = Scelta(pp.Choices, (int)pp.Value, k => { pp.Value = k; sc.Scale = 1; if (mostra != null) mostra(); });
                    if (!string.IsNullOrEmpty(pp.Tip)) ToolTip.SetTip(sp, pp.Tip);
                    if (sezioni && pp.StessaRiga && ultimaRigaScelte != null)
                    {
                        ultimaRigaScelte.Children.Add(new TextBlock { Text = pp.Label + ":", VerticalAlignment = VerticalAlignment.Center, FontSize = 14, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(18, 0, 4, 0) });
                        ultimaRigaScelte.Children.Add(sp);
                        ultimaRigaScelte.Children.Add(h);
                        cellaSez = sp;
                    }
                    else if (sezioni && pp.Sezione != null && !pp.Pillole)
                    {
                        StackPanel riga = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 2) };
                        riga.Children.Add(sp); riga.Children.Add(h);
                        left.Children.Add(riga);
                        ultimaRigaScelte = riga;
                    }
                    else if (pp.Pillole)
                    {
                        // tante voci: pillole che vanno a capo
                        WrapPanel wp = new WrapPanel { Tag = "pillole-" + pp.Key };
                        List<Control> figli = new List<Control>();
                        foreach (Control x in sp.Children) figli.Add(x);
                        sp.Children.Clear();
                        foreach (Control x in figli) { x.Margin = new Thickness(0, 1, 4, 3); wp.Children.Add(x); }
                        if (!string.IsNullOrEmpty(pp.Tip)) ToolTip.SetTip(wp, pp.Tip);
                        DockPanel riga = RigaCalc(pp.Label);
                        riga.Children.Add(wp);
                        left.Children.Add(riga);
                    }
                    else
                    {
                        DockPanel rg = RigaCalc(pp.Label, sp, h);
                        left.Children.Add(rg);
                        ultimaRigaScelte = rg.Children[1] as StackPanel;
                    }
                    aggiornaHint.Add(() => h.Text = pp.Hint ?? "");
                }
                else if (pp.Choices != null)
                {
                    gruppo = null;
                    ComboBox cb = new ComboBox { MinWidth = 230 };
                    tendine[pp.Key] = cb;
                    Action carica = () =>
                    {
                        if (cb.ItemCount != pp.Choices.Length)
                        {
                            cb.ItemsSource = pp.Choices.ToList();
                            cb.SelectedIndex = Math.Max(0, Math.Min(pp.Choices.Length - 1, (int)pp.Value));
                        }
                    };
                    carica();
                    cb.SelectionChanged += (s, e) => { if (cb.SelectedIndex >= 0 && cb.SelectedIndex != (int)pp.Value) { pp.Value = cb.SelectedIndex; sc.Scale = 1; if (mostra != null) mostra(); } };
                    if (!string.IsNullOrEmpty(pp.Tip)) ToolTip.SetTip(cb, pp.Tip);
                    TextBlock h = Muto(pp.Hint);
                    left.Children.Add(RigaCalc(pp.Label, cb, h));
                    aggiornaHint.Add(() => { carica(); int iv = Math.Max(0, Math.Min(pp.Choices.Length - 1, (int)pp.Value)); if (cb.SelectedIndex != iv) cb.SelectedIndex = iv; h.Text = pp.Hint ?? ""; });
                }
                else
                {
                    TextBox t = Tb(pp.Value.ToString("0.####", CultureInfo.CurrentCulture), 84);
                    t.FontFamily = Pal.Mono;
                    campi[pp.Key] = t;
                    if (!string.IsNullOrEmpty(pp.Tip)) ToolTip.SetTip(t, pp.Tip);
                    TextBlock u = new TextBlock { Text = pp.Unit, VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(4, 0, 0, 0), MinWidth = 26 };
                    TextBlock h = new TextBlock { Text = pp.Hint ?? "", Foreground = Pal.B(Pal.Muted), FontSize = 11.5, TextWrapping = TextWrapping.Wrap, MaxWidth = 280, Margin = new Thickness(146, 0, 0, 0) };
                    if (sezioni)
                    {
                        // due colonne allineate: etichetta (con la parte tra parentesi in piccolo sotto), casella, unita'
                        if (griglia == null)
                        {
                            griglia = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), Margin = new Thickness(0, 2, 0, 2) };
                            left.Children.Add(griglia); cellaGriglia = 0;
                        }
                        Control cg = CellaSezione(pp.Label, pp.Tip, t, u, h);
                        if (cellaGriglia % 2 == 0) griglia.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                        Grid.SetRow(cg, cellaGriglia / 2); Grid.SetColumn(cg, cellaGriglia % 2);
                        griglia.Children.Add(cg);
                        cellaGriglia++;
                        cellaSez = cg;
                    }
                    else
                    {
                        if (gruppo == null) { gruppo = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) }; left.Children.Add(gruppo); }
                        Control cella = CellaCalc(pp.Label, pp.Tip, t, u, h);
                        gruppo.Children.Add(cella);
                        cellaGruppo = cella;   // 1.7.0: nelle schede senza sezioni si nasconde solo la cella, non tutto il gruppo
                    }
                    leggi.Add(() => { if (!t.IsReadOnly) pp.Value = ParseNum(t.Text, pp.Value); });
                    aggiornaHint.Add(() => { h.Text = pp.Hint ?? ""; h.IsVisible = !string.IsNullOrEmpty(h.Text); });
                    t.LostFocus += (s, e) =>
                    {
                        // il fuoco si perde anche quando la scheda viene tolta (cambio di calcolatore): il ridisegno si rimanda
                        // e si salta se il campo non e' piu' nella finestra (evita AvaloniaInternalException, registro 13/11 17:58)
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            if (TopLevel.GetTopLevel(t) == null || t.IsReadOnly) return;
                            double old = pp.Value; pp.Value = ParseNum(t.Text, pp.Value); if (old != pp.Value) sc.Scale = 1; if (mostra != null) mostra();
                        });
                    };
                    t.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter && !t.IsReadOnly) { pp.Value = ParseNum(t.Text, pp.Value); sc.Scale = 1; if (mostra != null) mostra(); } };
                }
                if (pp.Visibile != null)
                {
                    // tutto cio' che questo parametro ha aggiunto (titolo della sezione compreso) o la sua cella nella griglia
                    List<Control> suoi = new List<Control>();
                    if (cellaGruppo != null) suoi.Add(cellaGruppo);
                    else
                    {
                        for (int i = nPrima; i < left.Children.Count; i++) if (!(left.Children[i] is Grid gg && gg == griglia && cellaSez != null)) suoi.Add(left.Children[i]);
                        if (cellaSez != null) suoi.Add(cellaSez);
                    }
                    Func<bool> vis = pp.Visibile;
                    visib.Add(() => { bool v = vis(); foreach (Control c in suoi) c.IsVisible = v; });
                }
            }
            foreach (Action a in visib) a();
            // 1.0.15 (bozzetto approvato da IC8TEM il 17/11/2026 09:57): nota fissa e avviso sui dati sotto i parametri
            if (!string.IsNullOrEmpty(sc.NotaFissa))
            {
                left.Children.Add(new Border
                {
                    Background = Pal.B(Pal.WarnBg), BorderBrush = Pal.B(Pal.Yellow), BorderThickness = new Thickness(5, 1, 1, 1), CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 7), Margin = new Thickness(0, 6, 0, 2),
                    // l'icona a parte: il testo resta identico alla chiave e si traduce (Lang)
                    Child = new DockPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "ⓘ", Foreground = Pal.B(Pal.Fore), FontSize = 12.5, Margin = new Thickness(0, 0, 6, 0), [DockPanel.DockProperty] = Dock.Left },
                            new TextBlock { Text = sc.NotaFissa, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), FontSize = 12.5 }
                        }
                    }
                });
                gruppo = null;
            }
            TextBlock avvDatiTesto = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), FontSize = 12.5 };
            Border avvDati = new Border
            {
                Background = Pal.B(Pal.A(40, Pal.Orange)), BorderBrush = Pal.B(Pal.Orange), BorderThickness = new Thickness(5, 1, 1, 1), CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 7), Margin = new Thickness(0, 4, 0, 2), IsVisible = false, Child = avvDatiTesto
            };
            left.Children.Add(avvDati);
            // 0.4.30 (IC8TEM 14/11/2026 23:40): rastremazione dei tubi nei progetti HF non filari
            if (sc.Tubolare)
            {
                CheckBox cR = new CheckBox { Content = "Tubi rastremati", IsChecked = sc.Rastrema, VerticalAlignment = VerticalAlignment.Center };
                TextBox tR = Tb(sc.SchemaRastr, 260);
                tR.FontFamily = Pal.Mono;
                ToolTip.SetTip(tR, "Diametro in mm x lunghezza in m per lato, dal centro o dalla base verso la punta; l'ultimo tubo arriva alla punta. Esempio: 35x1; 30x1; 25x1; 20x1; 16x1; 12");
                Action leggiR = () => { sc.Rastrema = cR.IsChecked == true; sc.SchemaRastr = tR.Text; };
                cR.IsCheckedChanged += (s, e) => { leggiR(); sc.Scale = 1; if (mostra != null) mostra(); };
                tR.LostFocus += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => { if (TopLevel.GetTopLevel(tR) == null) return; leggiR(); sc.Scale = 1; if (mostra != null) mostra(); });
                leggi.Add(leggiR);
                gruppo = null;
                left.Children.Add(RigaCalc("Rastremazione (HF)", cR, tR));
                left.Children.Add(Nota("tubi telescopici del radiatore: diametro x lunghezza per lato, dal centro o dalla base; nel calcolo correzione di Leeson; \"Porta in risonanza\" tiene conto dei tubi"));
            }
            TextBlock formula = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 13, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 2, 0, 2), LineHeight = 20 };
            Expander espFormule = new Expander { Header = "Formule e fonti", Content = formula, IsExpanded = false, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 6, 0, 6) };
            ToolTip.SetTip(espFormule, "Clic per aprire le formule e le fonti del calcolatore.");
            left.Children.Add(espFormule);

            WrapPanel btns = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            Button bNec = Btn(sc.Key == "ridim" ? "Crea il modello scalato e verificalo" : "Crea il modello NEC e verificalo", true);
            Button bSalva = Btn("Salva il modello (.nec / .yanec)", false);
            Button bApri = Btn("Apri come modello", false);
            Button bTune = Btn("Porta in risonanza (NEC)", false);
            Button bCar = Btn("Carichi (C) ›", false);
            foreach (Button b in new[] { bNec, bSalva, bApri, bTune, bCar }) { b.Margin = new Thickness(0, 0, 8, 6); btns.Children.Add(b); }
            if (sc.Key == "filari")
            {
                // 1.5.8 (anteprima approvata da IC8TEM il 22/11/2026 16:46): abaco delle filari in una finestra
                Button bAbaco = Btn("Abaco ›", false);
                bAbaco.Margin = new Thickness(0, 0, 8, 6);
                ToolTip.SetTip(bAbaco, Lang.T("Tabelle di lunghezze e contrappesi calcolate con il NEC (filo orizzontale a 3 m): una riga si puo' copiare nella scheda."));
                bAbaco.Click += (s0, e0) => ApriAbacoFilari(sc);
                btns.Children.Add(bAbaco);
            }
            left.Children.Add(btns);
            // 1.5.6 (bozzetto Filari approvato da IC8TEM il 21/11/2026 21:12): pulsanti viola che aprono Balun, Toroidi FT, Cavi RG, Carichi HF gia' compilati
            WrapPanel rimandi = new WrapPanel { Margin = new Thickness(0, 0, 0, 2), IsVisible = false };
            left.Children.Add(rimandi);
            // 0.4.20 (IC8TEM 14/11/2026 16:24, "creare lo stack"): dalla scheda DL6WU si crea lo stack nel modello aperto con le distanze calcolate;
            // se il modello e' gia' uno stack si applicano le distanze alle copie che ci sono
            // 1.0.16 (bozzetto biquad approvato da IC8TEM il 17/11/2026 17:25): feed per la parabola e dima del filo a scala 1:1
            if (sc is CalcBiquad bq)
            {
                Button bFeed = Btn("Usa come feed della parabola", false), bDima = Btn("Dima 1:1 (PDF)", false);
                foreach (Button b in new[] { bFeed, bDima }) { b.Margin = new Thickness(0, 0, 8, 6); btns.Children.Add(b); }
                ToolTip.SetTip(bFeed, "Apre la biquad come modello nella finestra principale (elemento a z = 0, fascio verso +Z, polarizzazione lungo Y, come i feed delle trombe) e passa alla Parabola primo fuoco: li' il pulsante \"Feed dal modello NEC\" prende il diagramma calcolato.");
                ToolTip.SetTip(bDima, "Disegno del filo piegato a scala 1:1 su A4, con i punti di piega, il righello di 100 mm per controllare la stampa e la tabella delle misure.");
                bFeed.Click += (s, e) =>
                {
                    if (mostra != null) mostra();
                    CalcOutput ob = bq.Run();
                    if (ob == null || ob.Nec == null) return;
                    ApriModelloCalcolatore(ob.Nec, bq.Nome + " " + bq.P("f").ToString("0", CultureInfo.InvariantCulture) + " MHz.nec");
                    if (fasCalc != null) fasCalc.Select("parabola");
                    Status("Biquad aperta come modello: quando il calcolo e' finito, nella Parabola primo fuoco premi \"Feed dal modello NEC\".");
                };
                bDima.Click += async (s, e) =>
                {
                    if (mostra != null) mostra();
                    try
                    {
                        IStorageFile fl = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Salva la dima", SuggestedFileName = "biquad_" + bq.P("f").ToString("0", CultureInfo.InvariantCulture) + "_1a1.pdf", DefaultExtension = "pdf" });
                        if (fl == null) return;
                        using (Stream st = await fl.OpenWriteAsync()) BiquadPdf.Scrivi(st, bq);
                        Status("Salvato: " + fl.Name);
                    }
                    catch (Exception ex) { Status("Dima non salvata: " + ex.Message, true); }
                };
            }
            // 1.0.17 (bozzetto Big Wheel approvato da IC8TEM il 17/11/2026 18:08): dima di piegatura 1:1 e dettaglio del mozzo
            if (sc is CalcBigWheel bwh)
            {
                Button bDimaP = Btn("Dima di piegatura 1:1 (PDF)", false), bMozzo = Btn("Dettaglio del mozzo (PDF)", false);
                foreach (Button b in new[] { bDimaP, bMozzo }) { b.Margin = new Thickness(0, 0, 8, 6); btns.Children.Add(b); }
                ToolTip.SetTip(bDimaP, "Il petalo a scala 1:1 su fogli A4 da unire (sovrapposizione 10 mm, crocette ogni 50 mm, righello di 100 mm). Solo per la Big Wheel.");
                ToolTip.SetTip(bMozzo, "I due livelli dei bracci, i triangoli di collegamento, lo stub e il connettore, con le misure.");
                Func<string, Action<Stream>, Task> salvaPdf = async (nome, scrivi) =>
                {
                    if (mostra != null) mostra();
                    if (bwh.I("tipo") != 0) { Status("La dima e il mozzo servono alla Big Wheel: scegli il tipo Big Wheel.", true); return; }
                    try
                    {
                        IStorageFile fl = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Salva", SuggestedFileName = nome, DefaultExtension = "pdf" });
                        if (fl == null) return;
                        using (Stream st = await fl.OpenWriteAsync()) scrivi(st);
                        Status("Salvato: " + fl.Name);
                    }
                    catch (Exception ex) { Status("PDF non salvato: " + ex.Message, true); }
                };
                bDimaP.Click += async (s, e) => await salvaPdf("big_wheel_dima_1a1.pdf", st => BigWheelPdf.Dima(st, bwh));
                bMozzo.Click += async (s, e) => await salvaPdf("big_wheel_mozzo.pdf", st => BigWheelPdf.Mozzo(st, bwh));
            }
            if (sc is CalcStackDl6wu sdl)
            {
                Button bStack = Btn("Crea lo stack nel modello aperto", true);
                bStack.Margin = new Thickness(0, 0, 8, 6);
                ToolTip.SetTip(bStack, "Copia l'antenna del modello aperto in righe (una sopra l'altra) e colonne (affiancate, perpendicolari al lobo) con le distanze DL6WU della scheda; " +
                    "ogni antenna ha la sua sorgente, uguale in ampiezza e fase (cavi uguali e divisore ideale). Se il modello e' gia' uno stack, sposta le copie alle distanze DL6WU. " +
                    "Solo modelli fatti di fili GW. Il file su disco non cambia: salvalo se vuoi tenerlo.");
                bStack.Click += (s, e) =>
                {
                    if (mostra != null) mostra();
                    if (model == null) { Status("Apri prima un modello (l'antenna da mettere in stack).", true); return; }
                    if (sdl.UsaModello && !sdl.DaModello) { Status("Calcolo dell'antenna base in corso: riprova appena la scheda si aggiorna (servono le aperture e la direzione del lobo).", true); return; }
                    try
                    {
                        StackInfo esiste = StackDetect.Detect(model);
                        if (esiste != null)
                        {
                            StackDetect.Apply(model, esiste, sdl.UltimaDv, sdl.UltimaDh, double.NaN);
                            Status("Stack gia' presente (" + esiste.Rows + " x " + esiste.Cols + "): copie spostate alle distanze DL6WU.");
                            ApplicaERicalcola("stack DL6WU applicato");
                            return;
                        }
                        List<string> note = new List<string>();
                        NecModel nm = StackDetect.Crea(model, sdl.I("righe"), sdl.I("col"), sdl.UltimaDv, sdl.UltimaDh, sdl.UltimoPhiLobo, note);
                        string fn = model.FileName;
                        SetModel(nm, null);
                        model.FileName = string.IsNullOrEmpty(fn) ? "stack.nec" : Path.Combine(Path.GetDirectoryName(fn) ?? "", Path.GetFileNameWithoutExtension(fn) + " stack " + sdl.I("righe") + "x" + sdl.I("col") + ".nec");
                        Registro.Scrivi("AZIONE", "stack DL6WU creato: " + string.Join(" ", note));
                        int ti = -1;
                        for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == "Struttura 3D") ti = i;
                        if (ti >= 0) SelectTab(ti);
                        Status(string.Join(" ", note) + " Il file su disco non e' cambiato.");
                        Calculate(true);
                    }
                    catch (Exception ex) { Status("Stack non creato: " + ex.Message, true); }
                };
                btns.Children.Add(bStack);
            }

            WrapPanel cards = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            left.Children.Add(cards);
            CalcDrawing dr = new CalcDrawing { Height = conBalun ? 330 : (sc is CalcStackDl6wu ? 420 : 300), Margin = new Thickness(0) };   // 1.3.1: stack piu' alto
            Control destra = dr;
            Action<CalcOutput> mostraDestra = null;
            if (conTrasf) destra = CostruisciTrasformatore(sc, campi, tendine, () => { if (mostra != null) mostra(); }, out mostraDestra);
            else if (conBalun) destra = CostruisciDisegnoBalun(sc, dr, left, tendine, out mostraDestra, () => { if (mostra != null) mostra(); });
            Border colDestra = new Border { Child = destra, Margin = new Thickness(14, 0, 0, 0), Padding = new Thickness(14, 0, 0, 0), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1, 0, 0, 0) };
            Grid.SetColumn(left, 0);
            Grid.SetColumn(colDestra, 1);
            top.Children.Add(left);
            top.Children.Add(colDestra);
            root.Children.Add(Scheda(top));
            // 1.5.9 (bozzetto Verticale 43 piedi approvato da IC8TEM il 22/11/2026 21:22): tabella banda per banda a tutta larghezza
            StackPanel tabBox = new StackPanel();
            TextBlock tabTitolo = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
            Grid tabGriglia = new Grid();
            tabBox.Children.Add(tabTitolo);
            tabBox.Children.Add(new ScrollViewer { Content = tabGriglia, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
            Border tabPanel = Scheda(tabBox);
            tabPanel.IsVisible = false;
            root.Children.Add(tabPanel);
            Action<CalcOutput> mostraTabella = o0 =>
            {
                tabGriglia.Children.Clear(); tabGriglia.RowDefinitions.Clear(); tabGriglia.ColumnDefinitions.Clear();
                if (o0.Tabella == null || o0.Tabella.Count == 0) { tabPanel.IsVisible = false; return; }
                tabPanel.IsVisible = true;
                tabTitolo.Text = Lang.T(o0.TabellaTitolo ?? "");
                int nc = 0; foreach (string[] r in o0.Tabella) nc = Math.Max(nc, r.Length);
                for (int j = 0; j < nc; j++) tabGriglia.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                for (int i = 0; i < o0.Tabella.Count; i++)
                {
                    tabGriglia.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int j = 0; j < o0.Tabella[i].Length; j++)
                    {
                        string t = o0.Tabella[i][j] ?? "";
                        Color col = i == 0 ? Pal.Muted : t.StartsWith("✓") ? Pal.Green : t.StartsWith("⚠") ? Pal.Orange : Pal.Fore;
                        TextBlock tb = new TextBlock { Text = Lang.TC(t), FontSize = i == 0 ? 12.5 : 13, FontWeight = i == 0 || j == 0 ? FontWeight.SemiBold : FontWeight.Normal, Foreground = Pal.B(col) };
                        Border cell = new Border { Child = tb, Padding = new Thickness(7, 4, 10, 4), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) };
                        Grid.SetRow(cell, i); Grid.SetColumn(cell, j);
                        tabGriglia.Children.Add(cell);
                    }
                }
            };
            Action<string, double, double, double> avviaTempoReale;
            Action<double[]> avviaBande;
            root.Children.Add(TempoRealePanel(out avviaTempoReale, out avviaBande));   // 0.4.2: calcolo in tempo reale (IC8TEM 13/11/2026); 1.5.6: ROS per banda
            Border necCard = Riquadretto("Verifica NEC (modello creato)", "in attesa", "Z, G, efficienza dal motore");

            Grid bottom = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,10,2*") };
            LineChart chart = new LineChart { Height = 270, XLabel = "", EmptyText = "nessun grafico per questo calcolatore" };
            StackPanel chartBox = new StackPanel();
            TextBlock chartTitle = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
            chartBox.Children.Add(chartTitle);
            chartBox.Children.Add(chart);
            Border chartPanel = Scheda(chartBox);
            StackPanel avv = new StackPanel { Spacing = 4 };
            StackPanel avvBox = new StackPanel();
            avvBox.Children.Add(new TextBlock { Text = "Avvisi", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6) });
            avvBox.Children.Add(avv);
            Border avvPanel = Scheda(avvBox);
            Grid.SetColumn(chartPanel, 0);
            Grid.SetColumn(avvPanel, 2);
            bottom.Children.Add(chartPanel);
            bottom.Children.Add(avvPanel);
            root.Children.Add(bottom);

            CalcOutput last = first;
            Action<string, string> setNec = (v, sub) =>
            {
                StackPanel sp = (StackPanel)necCard.Child;
                ((TextBlock)sp.Children[1]).Text = v;
                ((TextBlock)sp.Children[2]).Text = sub;
            };

            mostra = () =>
            {
                foreach (Action a in visib) a();
                foreach (Action a in leggi) a();
                sc.Model = model;
                sc.MainFreqMHz = txtFreq != null ? ParseNum(txtFreq.Text, double.NaN) : double.NaN;
                try { using (Prestazioni.Misura("Calcolatore: formule (" + sc.Title + ")")) last = sc.Run(); }
                catch (Exception ex) { last = new CalcOutput(); last.Warnings.Add("Errore nel calcolo: " + ex.Message); }
                // parametri presi dal modello della finestra principale: caselle aggiornate e in sola lettura
                foreach (KeyValuePair<string, TextBox> kv in campi)
                {
                    double vm;
                    bool dalModello = sc.DalModello.TryGetValue(kv.Key, out vm);
                    if (dalModello) kv.Value.Text = vm.ToString(kv.Key == "f" ? "0.###" : "0.##", CultureInfo.CurrentCulture);
                    double vc = 0;
                    bool calcolato = !dalModello && sc.Calcolati.TryGetValue(kv.Key, out vc);
                    CalcParam cpar = sc.Par(kv.Key);
                    if (calcolato) kv.Value.Text = vc.ToString(kv.Key == "nel" ? "0" : "0.###", CultureInfo.CurrentCulture);
                    else if (kv.Value.IsReadOnly && !dalModello && cpar != null) kv.Value.Text = cpar.Value.ToString("0.####", CultureInfo.CurrentCulture);   // torna scrivibile: il valore dell'utente
                    kv.Value.IsReadOnly = dalModello || calcolato;
                    kv.Value.Opacity = dalModello ? 0.75 : calcolato ? 0.6 : 1.0;
                    kv.Value.FontStyle = calcolato ? FontStyle.Italic : FontStyle.Normal;
                    string tipo = cpar != null ? cpar.Tip : null;
                    ToolTip.SetTip(kv.Value, dalModello ? "Valore preso dal modello aperto nella finestra principale (si aggiorna da solo)." : calcolato ? "Valore ricavato dagli altri dati: non si scrive (vedi la nota sotto i dati)." : (string.IsNullOrEmpty(tipo) ? null : tipo));
                }
                foreach (Action a in aggiornaHint) a();
                avvDatiTesto.Text = string.IsNullOrEmpty(last.AvvisoDati) ? "" : "⚠ " + Lang.T(last.AvvisoDati);
                avvDati.IsVisible = !string.IsNullOrEmpty(last.AvvisoDati);
                formula.Text = last.Formula;
                cards.Children.Clear();
                foreach (CalcCard c in last.Cards) cards.Children.Add(Riquadretto(c.Title, c.Value, c.Sub, c.Hl));
                if (last.Nec != null)
                {
                    if (necCard.Parent is Panel pp0) pp0.Children.Remove(necCard);
                    cards.Children.Add(necCard);
                    setNec("in attesa", "Z, G, efficienza dal motore");
                }
                dr.Nec = last.Nec; dr.Out = last; dr.InvalidateVisual();
                if (mostraDestra != null) mostraDestra(last);
                mostraTabella(last);
                if (last.Chart != null && last.Chart.X != null)
                {
                    chartTitle.Text = last.Chart.Title + (string.IsNullOrEmpty(last.Chart.MarkText) ? "" : "  (" + last.Chart.MarkText + ")");
                    chart.X = last.Chart.X;
                    chart.XLabel = last.Chart.XLabel;
                    chart.YLabel = last.Chart.YLabel;
                    chart.Series = new List<LineSeries> { new LineSeries { Name = last.Chart.Nome1 ?? last.Chart.YLabel, Y = last.Chart.Y, Color = Pal.Accent } };
                    if (last.Chart.Y2 != null) chart.Series.Add(new LineSeries { Name = last.Chart.Nome2 ?? "", Y = last.Chart.Y2, Color = Pal.Orange });
                    chart.MarkX = last.Chart.MarkX;
                    chart.YMinFixed = last.Chart.YMin;
                    chartPanel.IsVisible = true;
                }
                else chartPanel.IsVisible = false;
                // 0.4.8 (IC8TEM 13/11/2026 23:41, antenna EH): senza grafico gli avvisi restavano nella colonna stretta a destra, tagliati
                // in fondo alla pagina: ora prendono tutta la larghezza
                Grid.SetColumn(avvPanel, chartPanel.IsVisible ? 2 : 0);
                Grid.SetColumnSpan(avvPanel, chartPanel.IsVisible ? 1 : 3);
                chart.InvalidateVisual();
                avv.Children.Clear();
                foreach (string w in last.Warnings) avv.Children.Add(new TextBlock { Text = "⚠ " + Lang.T(w), TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Orange), FontSize = 13.5 });
                foreach (string w in last.Oks) avv.Children.Add(new TextBlock { Text = "✓ " + w, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Green), FontSize = 13.5 });
                // 1.7.0 (bozzetto «mining riviste» U): le schede della linguetta Consigli che riguardano questo calcolatore
                foreach (Consiglio cns in Consigli.Per(sc.Key))
                    avv.Children.Add(new TextBlock { Text = Lang.T("Consiglio") + " · " + Lang.T(cns.Titolo) + ": " + Lang.T(cns.Testo) + " (" + cns.Fonte + ")", TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Violet), FontSize = 13.5 });
                if (avv.Children.Count == 0) avv.Children.Add(new TextBlock { Text = "nessun avviso", Foreground = Pal.B(Pal.Muted) });
                bool haNec = last.Nec != null;
                // 0.4.20 (IC8TEM 14/11/2026 16:01): fumetto giallo con gli avvisi del modello NEC che i pulsanti creano
                {
                    List<string> avMod = haNec ? AvvisiDelMazzo(last.Nec) : new List<string>();
                    FumettoConAvvisi(bNec, "Crea il modello NEC dai valori della scheda, lo calcola con il motore e lo apre nella Struttura 3D.", avMod);
                    FumettoConAvvisi(bApri, "Apre il modello NEC della scheda nella finestra principale.", avMod);
                    foreach (string w in avMod) avv.Children.Insert(0, new TextBlock { Text = "⚠ modello NEC: " + w, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Yellow), FontSize = 13.5 });
                    if (avMod.Count > 0 && avv.Children.Count > 1 && avv.Children[avv.Children.Count - 1] is TextBlock tbn && (tbn.Text == "nessun avviso" || tbn.Text == Lang.T("nessun avviso"))) avv.Children.RemoveAt(avv.Children.Count - 1);
                }
                rimandi.Children.Clear();
                foreach (CalcRimando rr in last.Rimandi)
                {
                    CalcRimando r0 = rr;
                    Button br = Btn(r0.Testo, false);
                    br.Foreground = Pal.B(Pal.Violet); br.BorderBrush = Pal.B(Pal.Violet); br.BorderThickness = new Thickness(1); br.Background = Pal.B(Pal.A(30, Pal.Violet));
                    br.Margin = new Thickness(0, 0, 8, 6);
                    if (!string.IsNullOrEmpty(r0.Tip)) ToolTip.SetTip(br, r0.Tip);
                    br.Click += (s1, e1) => ApriRimando(r0, sc);
                    rimandi.Children.Add(br);
                }
                rimandi.IsVisible = rimandi.Children.Count > 0;
                avviaBande(last.Bande);
                avviaTempoReale(last.Nec, !double.IsNaN(last.Z0Rif) ? last.Z0Rif : (model != null ? model.Z0 : 50), double.NaN, double.NaN);
                bNec.IsVisible = bSalva.IsVisible = bApri.IsVisible = haNec || sc.UsesModel;
                bTune.IsVisible = haNec && sc.CanTune;
                bTune.Content = sc.TuneLabel;
                bCar.IsVisible = haNec && last.Nec.Contains("\nLD ");
            };

            string necCreato = null;   // testo del modello creato con "Crea il modello NEC e verificalo" (se i valori cambiano, va ricreato)
            Action verifica = () =>
            {
                mostra();
                if (last == null || last.Nec == null) return;
                necCreato = last.Nec;
                CalcOutput o = last;
                setNec("calcolo...", "motore NEC-2");
                Task.Run(() =>
                {
                    using (Prestazioni.Misura("Calcolatore: verifica NEC (" + sc.Title + ")"))
                    {
                        NecModel m = NecModel.Parse(o.Nec);
                        return SimpleCalc.Solve(o, m.FreqMHz, !double.IsNaN(o.Z0Rif) ? o.Z0Rif : (model != null ? model.Z0 : 50));   // 1.5.6: Filari, ROS riferito a 50 x rapporto
                    }
                }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
                {
                    if (t.IsFaulted) { setNec("errore", t.Exception.GetBaseException().Message); return; }
                    RunResult r = t.Result;
                    string pol = "";
                    try
                    {
                        FarFieldPoint fp = r.Engine.Gain(r.ThetaMax, r.PhiMax);
                        pol = fp.GainVert > fp.GainHor + 3 ? ", pol. V" : (fp.GainHor > fp.GainVert + 3 ? ", pol. H" : ", pol. mista");
                    }
                    catch { }
                    string z = r.Sources.Count > 1 ? r.Sources.Count + " sorgenti" : FN(r.Zin.Real, 1) + (r.Zin.Imaginary >= 0 ? " +j" : " -j") + FN(Math.Abs(r.Zin.Imaginary), 1) + " Ω";
                    setNec(z, "G " + FN(r.GmaxDbi, 1) + " dBi, eff. " + FN(r.Efficiency, 0) + " %" + (r.Sources.Count == 1 ? ", ROS " + FN(r.Swr, 2) : "") + pol);
                }));
            };
            // richiesta IC8TEM 13/11/2026 18:10: il modello creato si apre anche nella finestra principale e si vede nella Struttura 3D
            Action creaEApri = () =>
            {
                verifica();
                if (last != null && last.Nec != null) ApriModelloCalcolatore(last.Nec, sc.Nome + ".nec");
            };
            bNec.Click += (s, e) => creaEApri();
            bApri.Click += (s, e) => { mostra(); if (last != null && last.Nec != null) ApriModelloCalcolatore(last.Nec, sc.Nome + ".nec"); };
            // richiesta IC8TEM 13/11/2026 18:01: salvataggio senza aver creato il modello -> promemoria, poi il modello si crea comunque e si salva
            Func<Task> salvaDaQui = async () =>
            {
                mostra();
                if (last == null || last.Nec == null) { Status("Questa scheda non produce un modello NEC da salvare.", true); return; }
                if (necCreato != last.Nec)
                {
                    await Promemoria("Prima crea il modello NEC",
                        "Non hai ancora premuto \"" + bNec.Content + "\".\n\nRicorda di creare prima il modello NEC dai valori della scheda: " +
                        "lo creo adesso, lo verifico con il motore e lo apro nella Struttura 3D, poi si apre la finestra per salvarlo.");
                    creaEApri();
                }
                await SalvaTestoModello(last.Nec, sc.Nome);
            };
            bSalva.Click += async (s, e) => await salvaDaQui();
            root.AttachedToVisualTree += (s, e) => salvaCalcolatoreCorrente = salvaDaQui;
            // calcolatori che dipendono dal modello aperto: si ricalcolano ogni volta che la scheda torna visibile
            if (sc is CalcStackDl6wu)
            {
                bool aVideo = false;
                root.AttachedToVisualTree += (s, e) => { aVideo = true; if (mostra != null) mostra(); };
                root.DetachedFromVisualTree += (s, e) => aVideo = false;
                // 0.4.20: il calcolo dell'antenna base gira in sottofondo; quando finisce la scheda si ridisegna (solo se e' a video)
                CalcStackDl6wu.Pronto += () => Dispatcher.UIThread.Post(() => { if (aVideo && mostra != null) mostra(); });
            }
            if (sc is CalcMoxon)
            {
                // 1.0.10: Moxon con E vincolata, il NEC ricalcola la forma in sottofondo; a calcolo finito la scheda si ridisegna
                bool aVideoM = false;
                root.AttachedToVisualTree += (s, e) => aVideoM = true;
                root.DetachedFromVisualTree += (s, e) => aVideoM = false;
                CalcMoxon.ProntoMoxon += () => Dispatcher.UIThread.Post(() => { if (aVideoM && mostra != null) mostra(); });
            }
            if (sc is CalcQuad)
            {
                // 1.5: Quad sopra i 30 MHz (metodo Automatico o NEC), il motore cerca la risonanza in sottofondo; a calcolo finito la scheda si ridisegna
                bool aVideoQ = false;
                root.AttachedToVisualTree += (s, e) => aVideoQ = true;
                root.DetachedFromVisualTree += (s, e) => aVideoQ = false;
                CalcQuad.ProntoQuad += () => Dispatcher.UIThread.Post(() => { if (aVideoQ && mostra != null) mostra(); });
            }
            root.DetachedFromVisualTree += (s, e) => { if (salvaCalcolatoreCorrente == salvaDaQui) salvaCalcolatoreCorrente = null; };
            bCar.Click += (s, e) =>
            {
                mostra();
                if (last == null || last.Nec == null) return;
                ApriModelloCalcolatore(last.Nec, sc.Nome + ".nec");
                try { bassoCorrente = "carichi"; MostraBasso("carichi"); } catch { }
            };
            bTune.Click += (s, e) =>
            {
                mostra();
                if (last == null || last.Nec == null) return;
                double f = NecModel.Parse(last.Nec).FreqMHz;
                setNec("risonanza...", "ricerca con il NEC");
                Task.Run(() => { using (Prestazioni.Misura("Calcolatore: risonanza NEC (" + sc.Title + ")")) return sc.Tune(f); }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
                {
                    if (t.IsFaulted) { setNec("errore", t.Exception.GetBaseException().Message); return; }
                    verifica();
                }));
            };
            mostra();
            return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        // scheda del calcolatore visibile: il pulsante "Salva .nec..." in alto, senza modello aperto, salva il suo modello
        Func<Task> salvaCalcolatoreCorrente;

        // finestrella di promemoria con un solo pulsante
        async Task Promemoria(string titolo, string testo)
        {
            Window w = new Window { Title = titolo, Width = 460, SizeToContent = SizeToContent.Height, CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel) };
            StackPanel sp = new StackPanel { Margin = new Thickness(18), Spacing = 14 };
            sp.Children.Add(new TextBlock { Text = titolo, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            sp.Children.Add(new TextBlock { Text = testo, TextWrapping = TextWrapping.Wrap, FontSize = 14, Foreground = Pal.B(Pal.Fore) });
            Button ok = Btn("Ho capito: crea e salva", true);
            ok.HorizontalAlignment = HorizontalAlignment.Right;
            ok.Click += (s, e) => w.Close();
            sp.Children.Add(ok);
            w.Content = sp;
            Registro.Scrivi("AZIONE", "promemoria: " + titolo);
            await w.ShowDialog(this);
        }

        // 0.5.3: cella "etichetta + casella + unita'" (con il suggerimento sotto) per le caselle su piu' colonne
        Control CellaCalc(string etichetta, string tip, TextBox t, TextBlock unita, TextBlock suggerimento)
        {
            StackPanel v = new StackPanel { Width = 300, Margin = new Thickness(0, 2, 14, 2) };
            StackPanel h = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock l = new TextBlock { Text = etichetta, Width = 142, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, FontSize = 13.5, Foreground = Pal.B(Pal.Muted) };
            if (!string.IsNullOrEmpty(tip)) ToolTip.SetTip(l, tip);
            h.Children.Add(l); h.Children.Add(t); h.Children.Add(unita);
            v.Children.Add(h);
            suggerimento.IsVisible = !string.IsNullOrEmpty(suggerimento.Text);
            v.Children.Add(suggerimento);
            return v;
        }

        // 1.0.11: cella delle schede in sezioni (bozzetto approvato il 16/11/2026 23:48): etichetta larga 200 con la parte tra parentesi
        // in piccolo sotto se l'etichetta e' lunga, casella, unita'
        Control CellaSezione(string etichetta, string tip, TextBox t, TextBlock unita, TextBlock suggerimento)
        {
            string princ = etichetta, sotto = null;
            int par = etichetta.IndexOf(" (");
            if (par > 0 && etichetta.EndsWith(")") && etichetta.Length > 26) { princ = etichetta.Substring(0, par); sotto = etichetta.Substring(par + 2, etichetta.Length - par - 3); }
            StackPanel v = new StackPanel { Margin = new Thickness(0, 3, 14, 3) };
            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("200,Auto,Auto") };
            StackPanel et = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            et.Children.Add(new TextBlock { Text = princ, TextWrapping = TextWrapping.Wrap, FontSize = 13.5, Foreground = Pal.B(Pal.Muted) });
            if (sotto != null) et.Children.Add(new TextBlock { Text = sotto, TextWrapping = TextWrapping.Wrap, FontSize = 11.5, Foreground = Pal.B(Pal.Muted) });
            if (!string.IsNullOrEmpty(tip)) ToolTip.SetTip(et, tip);
            Grid.SetColumn(t, 1); Grid.SetColumn(unita, 2);
            g.Children.Add(et); g.Children.Add(t); g.Children.Add(unita);
            v.Children.Add(g);
            suggerimento.Margin = new Thickness(208, 0, 0, 0);
            suggerimento.IsVisible = !string.IsNullOrEmpty(suggerimento.Text);
            v.Children.Add(suggerimento);
            return v;
        }

        // 1.0.11: scelta a tessere con la figurina (delta loop: dove va il balun), la scelta in arancio con il bordo blu
        Control TessereScelta(CalcParam pp, Action cambiato)
        {
            WrapPanel wp = new WrapPanel { Margin = new Thickness(0, 2, 0, 4) };
            List<Border> bs = new List<Border>();
            List<List<Shape>> figure = new List<List<Shape>>();
            Action stile = () =>
            {
                for (int j = 0; j < bs.Count; j++)
                {
                    bool on = j == (int)pp.Value;
                    bs[j].Background = Pal.B(on ? Pal.AccentSoft : Pal.Panel);
                    bs[j].BorderBrush = Pal.B(on ? Pal.Accent : Pal.PillBorder);
                    bs[j].BorderThickness = new Thickness(on ? 2 : 1);
                    foreach (Shape sh in figure[j])
                    {
                        if (sh is Polygon) sh.Stroke = Pal.B(on ? Pal.Orange : Pal.Muted);
                        else sh.Fill = Pal.B(on ? Pal.Orange : Pal.Muted);
                    }
                }
            };
            for (int i = 0; i < pp.Choices.Length; i++)
            {
                int k = i;
                double[] fg = pp.Figure.Length > i ? pp.Figure[i] : null;
                Canvas cv = new Canvas { Width = 44, Height = 34, HorizontalAlignment = HorizontalAlignment.Center };
                List<Shape> mie = new List<Shape>();
                if (fg != null && fg.Length >= 8)
                {
                    Polygon pg = new Polygon { StrokeThickness = 2 };
                    var pts = new List<Point>();
                    for (int q = 0; q + 3 < fg.Length; q += 2) pts.Add(new Point(fg[q] * 44, fg[q + 1] * 34));
                    pg.Points = pts;
                    Rectangle r = new Rectangle { Width = 8, Height = 8 };
                    Canvas.SetLeft(r, fg[fg.Length - 2] * 44 - 4); Canvas.SetTop(r, fg[fg.Length - 1] * 34 - 4);
                    cv.Children.Add(pg); cv.Children.Add(r);
                    mie.Add(pg); mie.Add(r);
                }
                figure.Add(mie);
                StackPanel sp = new StackPanel { Spacing = 2 };
                sp.Children.Add(cv);
                sp.Children.Add(new TextBlock { Text = pp.Choices[i], TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, FontSize = 13, Foreground = Pal.B(Pal.Fore) });
                if (pp.Sotto != null && pp.Sotto.Length > i)
                    sp.Children.Add(new TextBlock { Text = pp.Sotto[i], TextAlignment = TextAlignment.Center, FontSize = 11.5, Foreground = Pal.B(Pal.Muted) });
                Border b = new Border { Child = sp, Width = 132, Padding = new Thickness(6, 6), Margin = new Thickness(0, 0, 8, 6), CornerRadius = new CornerRadius(8), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
                if (!string.IsNullOrEmpty(pp.Tip)) ToolTip.SetTip(b, pp.Tip);
                b.PointerPressed += (s, e) => { pp.Value = k; stile(); cambiato(); };
                bs.Add(b);
                wp.Children.Add(b);
            }
            stile();
            return wp;
        }

        DockPanel RigaCalc(string etichetta, params Control[] c)
        {
            DockPanel d = new DockPanel { Margin = new Thickness(0, 2) };
            TextBlock l = new TextBlock { Text = etichetta, Width = 190, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, FontSize = 14, Foreground = Pal.B(Pal.Muted) };
            DockPanel.SetDock(l, Dock.Left);
            d.Children.Add(l);
            StackPanel h = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            foreach (Control x in c) h.Children.Add(x);
            d.Children.Add(h);
            return d;
        }

        // salva il testo di un modello in .nec (NEC2 universale) o .yanec (completo: stesso testo con le righe CM YA-NEC)
        async Task SalvaTestoModello(string nec, string nome)
        {
            try
            {
                IStorageFile f = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Salva il modello del calcolatore",
                    SuggestedFileName = nome + ".nec",
                    DefaultExtension = "nec",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("NEC2 (universale)") { Patterns = new[] { "*.nec" } },
                        new FilePickerFileType("YA-NEC (completo)") { Patterns = new[] { "*.yanec" } }
                    }
                });
                if (f == null) return;
                string path = f.TryGetLocalPath();
                if (path == null) { Status("Impossibile scrivere nel percorso scelto.", true); return; }
                string testo = nec;
                if (path.EndsWith(".yanec", StringComparison.OrdinalIgnoreCase)) testo = YanecFile.Testo(null, Path.GetFileNameWithoutExtension(path), nec);   // 0.4.30: formato .yanec 2
                File.WriteAllText(path, testo);
                Status("Modello salvato: " + path);
            }
            catch (Exception ex) { Status("Errore nel salvataggio: " + ex.Message, true); }
        }

        // ------------------------------------------------------------------------------------------------
        // four-square con la grafica del bozzetto approvato
        // ------------------------------------------------------------------------------------------------
        Control BuildFourSquarePanel()
        {
            SimpleCalc sc = simpleCalcs.Find(c => c.Key == "4sq");
            StackPanel root = new StackPanel();

            // dati in una riga
            WrapPanel dati = new WrapPanel();
            List<Action> leggi = new List<Action>();
            Action calcola = null;
            foreach (CalcParam p in sc.Params)
            {
                CalcParam pp = p;
                StackPanel cell = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 2, 18, 2) };
                cell.Children.Add(new TextBlock { Text = pp.Label, VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) });
                if (pp.Choices != null)
                    cell.Children.Add(Scelta(pp.Choices, (int)pp.Value, k => { pp.Value = k; if (calcola != null) calcola(); }));
                else
                {
                    TextBox t = Tb(pp.Value.ToString("0.####", CultureInfo.CurrentCulture), 70);
                    cell.Children.Add(t);
                    cell.Children.Add(new TextBlock { Text = pp.Unit, VerticalAlignment = VerticalAlignment.Center });
                    leggi.Add(() => pp.Value = ParseNum(t.Text, pp.Value));
                    t.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Enter && calcola != null) calcola(); };
                }
                dati.Children.Add(cell);
            }
            Button bCalc = Btn("Calcola", true), bApri = Btn("Apri come modello", false), bSalva = Btn("Salva il modello (.nec / .yanec)", false);
            foreach (Button b in new[] { bCalc, bApri, bSalva }) { b.Margin = new Thickness(0, 2, 8, 2); dati.Children.Add(b); }
            root.Children.Add(Scheda(dati));

            WrapPanel cards = new WrapPanel();
            root.Children.Add(cards);

            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("11*,10,9*") };
            Structure3DView v3 = new Structure3DView { Height = 560, ShowPattern = false, ShowGroundAndHeight = true, ShowPhases = true };
            StackPanel sx = new StackPanel();
            StackPanel chk = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, Margin = new Thickness(0, 0, 0, 6) };
            CheckBox cFasi = new CheckBox { Content = "Fasi delle sorgenti", IsChecked = true };
            CheckBox cLobo = new CheckBox { Content = "Lobo 3D", IsChecked = false };
            CheckBox cTer = new CheckBox { Content = "Terreno e quota", IsChecked = true };
            cFasi.IsCheckedChanged += (s, e) => { v3.ShowPhases = cFasi.IsChecked == true; v3.InvalidateVisual(); };
            cLobo.IsCheckedChanged += (s, e) => { v3.ShowPattern = cLobo.IsChecked == true; v3.InvalidateVisual(); };
            cTer.IsCheckedChanged += (s, e) => { v3.ShowGroundAndHeight = cTer.IsChecked == true; v3.InvalidateVisual(); };
            chk.Children.Add(cFasi); chk.Children.Add(cLobo); chk.Children.Add(cTer);
            sx.Children.Add(chk);
            sx.Children.Add(new Border { Child = v3, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ClipToBounds = true });
            Grid.SetColumn(sx, 0);
            g.Children.Add(sx);

            StackPanel dx = new StackPanel();
            StackPanel tab = new StackPanel { Spacing = 2 };
            StackPanel tabBox = new StackPanel();
            tabBox.Children.Add(new TextBlock { Text = "Sorgenti (Oggetti del modello) - alimentazione a corrente", FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6) });
            tabBox.Children.Add(tab);
            tabBox.Children.Add(Nota("Con il tipo \"corrente\" il programma calcola da solo le tensioni che danno esattamente queste correnti (matrice delle impedenze mutue). " +
                "L'elemento A puo' avere resistenza negativa: restituisce potenza alla rete di sfasamento."));
            dx.Children.Add(Scheda(tabBox));
            PolarPlot pAz = new PolarPlot { Height = 300, Rotate = 90, LinRange = 30 };
            PolarPlot pEl = new PolarPlot { Height = 300, Elevation = true, GroundHalf = true, LinRange = 30 };
            Grid pol = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
            Grid.SetColumn(pEl, 1);
            pol.Children.Add(pAz); pol.Children.Add(pEl);
            TextBlock polNote = Nota("");
            StackPanel polBox = new StackPanel();
            polBox.Children.Add(pol);
            polBox.Children.Add(polNote);
            dx.Children.Add(Scheda(polBox));
            Grid.SetColumn(dx, 2);
            g.Children.Add(dx);
            root.Children.Add(g);
            StackPanel avv = new StackPanel();
            root.Children.Add(avv);
            Action<string, double, double, double> avviaTempoReale4;
            root.Children.Add(TempoRealePanel(out avviaTempoReale4));   // 0.4.2: calcolo in tempo reale

            CalcOutput last = null;
            Func<string, string[], Control> rigaTab = (nome, celle) =>
            {
                StackPanel r = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                r.Children.Add(new TextBlock { Text = nome, Width = 26, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore) });
                double[] w = { 70, 80, 80, 90, 130 };
                for (int i = 0; i < celle.Length; i++)
                {
                    bool head = nome == "";
                    r.Children.Add(head ? (Control)new TextBlock { Text = celle[i], Width = w[i], Foreground = Pal.B(Pal.Muted), FontSize = 13 }
                        : Cella(celle[i], w[i], true, true));
                }
                return r;
            };

            calcola = () =>
            {
                foreach (Action a in leggi) a();
                last = sc.Run();
                avviaTempoReale4(last.Nec, model != null ? model.Z0 : 50, double.NaN, double.NaN);
                // 0.4.20 (IC8TEM 14/11/2026 16:01): fumetto giallo con gli avvisi del modello NEC
                List<string> av4 = AvvisiDelMazzo(last.Nec);
                FumettoConAvvisi(bApri, "Apre il modello NEC della four-square nella finestra principale.", av4);
                FumettoConAvvisi(bCalc, "Calcola le impedenze delle quattro verticali con le sorgenti a corrente.", av4);
                cards.Children.Clear();
                foreach (CalcCard c in last.Cards) cards.Children.Add(Riquadretto(c.Title, c.Value, c.Sub, c.Hl));
                avv.Children.Clear();
                foreach (string w in last.Warnings) avv.Children.Add(Nota("⚠ " + Lang.T(w), Pal.Orange));
                foreach (string w in av4) avv.Children.Add(Nota("⚠ modello NEC: " + w, Pal.Yellow));
                tab.Children.Clear();
                tab.Children.Add(rigaTab("", new[] { "Filo", "Ampiezza", "Fase", "Tipo", "Z risultante" }));
                tab.Children.Add(Nota("calcolo NEC in corso..."));
                CalcOutput o = last;
                Task.Run(() =>
                {
                    NecModel m = NecModel.Parse(o.Nec);
                    SegmentGeometry geo = m.BuildGeometry();
                    RunResult r = Analysis.Run(m, geo, m.FreqMHz, 2.0);
                    return Tuple.Create(m, geo, r);
                }).ContinueWith(t => Dispatcher.UIThread.Post(() =>
                {
                    tab.Children.RemoveAt(tab.Children.Count - 1);
                    if (t.IsFaulted) { tab.Children.Add(Nota("Errore NEC: " + t.Exception.GetBaseException().Message, Pal.Red)); return; }
                    NecModel m = t.Result.Item1;
                    SegmentGeometry geo = t.Result.Item2;
                    RunResult r = t.Result.Item3;
                    string[] fasi = { "0°", "-90°", "-90°", "-180°" };
                    v3.Geo = geo;
                    v3.Ground = m.Ground.Kind != GroundKind.FreeSpace;
                    v3.GroundLook = m.Ground.Kind == GroundKind.Perfect ? GroundPainter.ByIndex(0) : GroundPainter.ByIndex(1);
                    v3.SourceSegs = new HashSet<int>();
                    v3.SourceLabels = new List<KeyValuePair<int, string>>();
                    for (int i = 0; i < m.Sources.Count && i < 4; i++)
                    {
                        int sg = geo.SegNo(m.Sources[i].Tag, m.Sources[i].Seg) - 1;
                        v3.SourceSegs.Add(sg);
                        v3.SourceLabels.Add(new KeyValuePair<int, string>(sg, CalcFourSquare.Nomi[i] + " 1∠" + fasi[i]));
                        Complex z = i < r.Sources.Count ? r.Sources[i].Z : Complex.Zero;
                        tab.Children.Add(rigaTab(CalcFourSquare.Nomi[i], new[] { m.Sources[i].Tag.ToString(), "1 A", fasi[i], "corrente",
                            FN(z.Real, 1) + (z.Imaginary >= 0 ? " +j" : " -j") + FN(Math.Abs(z.Imaginary), 1) }));
                    }
                    double[] cn = new double[geo.N];
                    double mx = 0;
                    for (int i = 0; i < geo.N; i++) { cn[i] = Complex.Abs(r.Engine.SegmentCurrent(i)); mx = Math.Max(mx, cn[i]); }
                    if (mx > 0) for (int i = 0; i < geo.N; i++) cn[i] /= mx;
                    v3.CurrentNorm = cn;
                    v3.Pattern = r.Grid; v3.PatternMax = r.GmaxDbi;
                    v3.LobeAz = r.PhiMax; v3.LobeEl = r.ElevationOfMax;
                    v3.Info = Lang.F("{0} segmenti, {1} fili, {2}", geo.N, m.Geometry.Count, Lang.T(m.Ground.Kind == GroundKind.Perfect ? "terreno perfetto" : "terreno medio (εr 13, σ 0,005 S/m, Sommerfeld)"));
                    v3.InvalidateVisual();
                    Complex za = r.Sources.Count > 0 ? r.Sources[0].Z : Complex.Zero;
                    cards.Children.Insert(0, Riquadretto("Impedenza A (dietro)", FN(za.Real, 1) + (za.Imaginary >= 0 ? " +j" : " -j") + FN(Math.Abs(za.Imaginary), 1), "4 sorgenti: vedi tabella"));
                    cards.Children.Insert(1, Riquadretto("Guadagno massimo", FN(r.GmaxDbi, 2) + " dBi", m.Ground.Kind == GroundKind.Perfect ? "terreno perfetto" : "terreno medio", true));
                    cards.Children.Insert(2, Riquadretto("Direzione del massimo", "az " + FN(r.PhiMax, 1) + "°", "elevazione " + FN(r.ElevationOfMax, 0) + "°"));
                    cards.Children.Insert(3, Riquadretto("F/B", FN(r.FrontBack, 1) + " dB", "a " + FN(r.ElevationOfMax, 0) + "° di elevazione"));
                    cards.Children.Insert(4, Riquadretto("Apertura a -3 dB", FN(r.HpbwAz, 0) + "° / " + FN(r.HpbwEl, 0) + "°", "orizzontale / verticale"));
                    pAz.Traces = new List<PolarTrace> { new PolarTrace { Db = r.AzCut, Color = Pal.Accent, Name = "totale" } };
                    pAz.RefDb = r.GmaxDbi; pAz.MarkAngle = r.PhiMax; pAz.Title = Lang.F("Azimut a {0}° di elevazione", FN(r.ElevationOfMax, 0));
                    pEl.Traces = new List<PolarTrace> { new PolarTrace { Db = r.ElCut, Color = Pal.Orange, Name = "totale" } };
                    pEl.RefDb = r.GmaxDbi; pEl.MarkAngle = r.ElevationOfMax; pEl.Title = "Elevazione (piano az " + FN(r.PhiMax, 0) + "°)";
                    pAz.InvalidateVisual(); pEl.InvalidateVisual();
                    polNote.Text = "max " + FN(r.GmaxDbi, 2) + " dBi a " + FN(r.ElevationOfMax, 0) + "°, F/B " + FN(r.FrontBack, 1) + " dB";
                }));
            };
            bCalc.Click += (s, e) => calcola();
            bApri.Click += (s, e) => { if (last != null) ApriModelloCalcolatore(last.Nec, "Four-square.nec"); };
            bSalva.Click += async (s, e) => { if (last != null) await SalvaTestoModello(last.Nec, "Four-square"); };
            calcola();
            return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }
    }
}
