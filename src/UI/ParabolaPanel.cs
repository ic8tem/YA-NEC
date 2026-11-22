// YA-NEC - sottolinguetta "Parabola primo fuoco" della linguetta Calcolatori.
// Dati a sinistra (raggruppati, con fumetti), a destra le pagine a pillola:
//   Riepilogo | Lobi | Meccanica | Centina | Bande ed EME
// Tutto si ricalcola mentre si scrive (ritardo 300 ms). Stile: colori del fascione approvato.
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    /// <summary>Tagli E e H del feed calcolati da YA-NEC sul modello aperto (angoli dalla direzione di massimo, dB).</summary>
    public class FeedNec
    {
        public double[] ThetaE, DbE, ThetaH, DbH;
        public double[] FaseE, FaseH;   // fase copolare in gradi (facoltativa): E = E_theta a phi 90, H = E_phi a phi 0
        public double[] Theta45, Db45, Fase45;   // piano a 45 gradi, facoltativo e solo da vedere
        public string Nome;
    }

    public class ParabolaPanel : UserControl
    {
        /// <summary>Diametro massimo del disco per il modello NEC a griglia di fili (0.4.20, scelta di IC8TEM del 14/11/2026 18:11).</summary>
        public const double DiscoNecMax_m = 1.2;

        /// <summary>Diametro massimo (a passi di 5 cm, non oltre DiscoNecMax_m) con cui il modello NEC del disco resta entro 12000 segmenti.</summary>
        public static double DiametroNecMassimo(double fMHz, double fsud, int ncent, double maglia)
        {
            double best = 0;
            for (double d = 0.05; d <= DiscoNecMax_m + 1e-9; d += 0.05)
                if (ParabolaNec.Segmenti(fMHz, d, fsud, ncent, maglia) <= 12000) best = d; else break;
            return best;
        }

        /// <summary>Impostato da MainWindow: restituisce i tagli del feed del modello aperto (null se non c'e' un calcolo).</summary>
        public Func<FeedNec> FornitoreFeedNec;
        /// <summary>0.4.20: apre nella finestra principale il modello NEC del disco a centine (testo del mazzo, nome del file).</summary>
        public Action<string, string> ApriNec;

        // colori (gli stessi del Fascione)
        // colori dello schema scelto di YA-NEC (Impostazioni > Aspetto), approvato da IC8TEM il 13/11/2026: con gli schemi scuri il pannello
        // segue lo schema (riapplicato all'integrazione della parabola offset, 14/11/2026)
        internal static Color CSfondo => Pal.Panel2;
        internal static Color CBordo => Pal.Border;
        internal static Color CBlu => Pal.Accent;
        internal static Color CBluTesto => Pal.Dark ? Pal.Accent : Pal.PillFore;
        internal static Color CFam => Pal.ChartText;
        internal static Color CGrigio => Pal.Muted;
        internal static readonly CultureInfo IT = new CultureInfo("it-IT");

        readonly ParabolaInput inp = new ParabolaInput();
        ParabolaResult res;
        readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        readonly List<Action> letture = new List<Action>();
        readonly Dictionary<string, Border> pillole = new Dictionary<string, Border>();
        string pagina = "riepilogo";
        ContentControl hostPagina;
        StackPanel pannelloAvvisi;
        TextBlock titoloRisultato, notaFeed;
        TextBox tbFreq;
        bool mostraSenzaBlocco = true;
        string nomeFeedNec;

        public ParabolaPanel()
        {
            timer.Tick += (s, e) => { timer.Stop(); Ricalcola(); };
            Content = Costruisci();
            Ricalcola();
        }

        /// <summary>Aggancio per YA-NEC: usa come illuminatore il diagramma calcolato col NEC sul modello del feed
        /// (angoli in gradi dall'asse, livelli in dB). Passare null per tornare al feed teorico cos^q.</summary>
        public void ImpostaFeedDaNec(double[] thetaDeg, double[] db, string nome)
        {
            inp.FeedPianiThetaDeg = null; inp.FeedPianoDbE = inp.FeedPianoDbH = inp.FeedPianoFaseE = inp.FeedPianoFaseH = null;
            inp.FeedPiano45Db = inp.FeedPiano45Fase = null;
            inp.FeedTabThetaDeg = thetaDeg; inp.FeedTabDb = db; nomeFeedNec = thetaDeg == null ? null : nome;
            Ricalcola();
        }

        /// <summary>Feed con i due piani E e H su griglia comune (gradi da 0), con la fase facoltativa (gradi).</summary>
        public void ImpostaFeedPiani(double[] thetaDeg, double[] dbE, double[] dbH, double[] faseE, double[] faseH, string nome)
        {
            inp.FeedTabThetaDeg = null; inp.FeedTabDb = null;
            if (thetaDeg == null) { inp.FeedPiano45Db = null; inp.FeedPiano45Fase = null; }
            inp.FeedPianiThetaDeg = thetaDeg; inp.FeedPianoDbE = dbE; inp.FeedPianoDbH = dbH;
            inp.FeedPianoFaseE = faseE; inp.FeedPianoFaseH = faseH;
            nomeFeedNec = thetaDeg == null ? null : nome;
            Ricalcola();
        }

        void UsaFeedNec()
        {
            FeedNec f = null;
            try { f = FornitoreFeedNec != null ? FornitoreFeedNec() : null; }
            catch (Exception ex) { pannelloAvvisi.Children.Insert(0, Avviso(2, "Diagramma del feed non disponibile: " + ex.Message)); return; }
            if (f == null || f.ThetaE == null || f.DbE == null)
            {
                pannelloAvvisi.Children.Insert(0, Avviso(1, "Nessun diagramma del feed: aprire il modello del feed e calcolarlo nella linguetta Diagrammi."));
                return;
            }
            double[] th, gE, gH, fE, fH;
            Parabola.PianiSuGriglia(f.ThetaE, f.DbE, f.FaseE, f.ThetaH, f.DbH, f.FaseH, out th, out gE, out gH, out fE, out fH);
            double[] g45 = null, f45 = null;
            if (f.Theta45 != null && f.Db45 != null)
            {
                double[] t45, gx, fx;
                Parabola.PianiSuGriglia(f.Theta45, f.Db45, f.Fase45, f.Theta45, f.Db45, f.Fase45, out t45, out g45, out gx, out f45, out fx);
            }
            inp.FeedPiano45Db = g45; inp.FeedPiano45Fase = f45;
            ImpostaFeedPiani(th, gE, gH, fE, fH, string.IsNullOrEmpty(f.Nome) ? "modello aperto" : f.Nome);
        }

        async System.Threading.Tasks.Task CaricaFeedCsv()
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top == null) return;
                var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Diagramma del feed: un file, oppure due file (piano E e piano H) come in W1GHZ",
                    AllowMultiple = true
                });
                if (files == null || files.Count == 0) return;
                var letti = new List<Parabola.FeedLetto>();
                var nomi = new List<string>();
                foreach (var file in files.Take(2))
                {
                    string testo;
                    using (var st = await file.OpenReadAsync())
                    using (var rd = new StreamReader(st)) testo = rd.ReadToEnd();
                    Parabola.FeedLetto fl; string err;
                    if (!Parabola.LeggiFeed(testo, Parabola.FormatoAuto, out fl, out err))
                    {
                        pannelloAvvisi.Children.Insert(0, Avviso(2, "File \"" + file.Name + "\" non valido: " + err));
                        return;
                    }
                    letti.Add(fl); nomi.Add(file.Name);
                }
                double[] th, gE, gH, fE, fH;
                string descr;
                bool unPiano = false;
                if (letti.Count == 2)
                {
                    // il file con "H" nel nome (es. DIPROD_H.DAT) e' il piano H, l'altro il piano E
                    int iH = NomePianoH(nomi[1]) || !NomePianoH(nomi[0]) ? 1 : 0, iE = 1 - iH;
                    Parabola.PianiSuGriglia(letti[iE].Theta, letti[iE].DbE, letti[iE].Fase, letti[iH].Theta, letti[iH].DbE, letti[iH].Fase, out th, out gE, out gH, out fE, out fH);
                    descr = "E = " + nomi[iE] + ", H = " + nomi[iH] + " (" + letti[iE].Descrizione + ")";
                }
                else
                {
                    Parabola.PianiSuGriglia(letti[0].Theta, letti[0].DbE, letti[0].Fase, letti[0].Theta, letti[0].DbH, letti[0].Fase, out th, out gE, out gH, out fE, out fH);
                    descr = nomi[0] + " (" + letti[0].Descrizione + ")";
                    unPiano = letti[0].Formato == Parabola.FormatoW1ghz;
                }
                inp.FeedPiano45Db = null; inp.FeedPiano45Fase = null;   // i file W1GHZ e CSV non hanno il piano a 45 gradi
                ImpostaFeedPiani(th, gE, gH, fE, fH, descr);
                if (unPiano)
                    pannelloAvvisi.Children.Insert(0, Avviso(1, "File W1GHZ con un solo piano: si usa lo stesso diagramma (e la stessa fase) per E e H. Per i due piani selezionare insieme i due file."));
                if (letti.Count == 2 && (letti[0].Fase == null) != (letti[1].Fase == null))
                    pannelloAvvisi.Children.Insert(0, Avviso(1, "Solo uno dei due file ha la fase: la fase non viene usata."));
            }
            catch (Exception ex) { pannelloAvvisi.Children.Insert(0, Avviso(2, "Lettura non riuscita: " + ex.Message)); }
        }

        static bool NomePianoH(string nome)
        {
            string n = System.IO.Path.GetFileNameWithoutExtension(nome ?? "").ToUpperInvariant();
            return n.EndsWith("_H") || n.EndsWith("-H") || n.EndsWith(" H") || n.Contains("HPLANE") || n.Contains("PIANO_H");
        }

        // ============================================================================================
        // costruzione
        // ============================================================================================
        Control Costruisci()
        {
            var root = new Grid { ColumnDefinitions = new ColumnDefinitions("340,*"), Background = new SolidColorBrush(CSfondo) };

            // ---------------- colonna dei dati
            var sx = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4, Margin = new Thickness(8, 8, 12, 8) };
            sx.Children.Add(new TextBlock { Text = "Parabola primo fuoco", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(CFam) });
            sx.Children.Add(Nota("Meccanica e prestazioni per 1296, 2320 e 5760 MHz; per 10368 con la verifica EME."));

            sx.Children.Add(Famiglia("Banda e geometria"));
            var cb = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 4) };
            var voci = new List<string>();
            for (int i = 0; i < Parabola.FreqBande.Length; i++) voci.Add(string.Format(IT, "{0} - {1:0.0} MHz", Parabola.NomiBande[i], Parabola.FreqBande[i]));
            voci.Add("Frequenza libera");
            cb.ItemsSource = voci; cb.SelectedIndex = 0;
            ToolTip.SetTip(cb, "Centri operativi EME/banda stretta. Con \"Frequenza libera\" si scrive la frequenza nella casella sotto.");
            sx.Children.Add(cb);
            tbFreq = Campo(sx, "Frequenza", "MHz", () => inp.FreqMHz, v => inp.FreqMHz = v, "Frequenza di calcolo. Si aggiorna scegliendo la banda; si puo' scrivere a mano.");
            cb.SelectionChanged += (s, e) =>
            {
                if (cb.SelectedIndex >= 0 && cb.SelectedIndex < Parabola.FreqBande.Length)
                    tbFreq.Text = Parabola.FreqBande[cb.SelectedIndex].ToString("0.0", IT);
            };
            Campo(sx, "Diametro D", "m", () => inp.Diametro_m, v => inp.Diametro_m = v, "Diametro dell'apertura (bordo del disco).");
            Campo(sx, "f/D", "", () => inp.FsuD, v => inp.FsuD = v, "Rapporto focale. 0,35-0,45 per i feed a tromba tipo VE4MA/septum; i dipoli con riflettore lavorano meglio con dischi profondi (circa 0,25-0,35, W1GHZ cap. 6).");

            sx.Children.Add(Famiglia("Illuminatore"));
            Campo(sx, "Taper al bordo", "dB", () => inp.TaperBordo_dB, v => inp.TaperBordo_dB = v, "Illuminazione al bordo rispetto al centro, compresa l'attenuazione spaziale. Il massimo d'efficienza e' intorno a 10 dB (W1GHZ cap. 4 e 6); piu' taper = lobi piu' bassi e meno rumore da terra, ma meno guadagno.");
            notaFeed = Nota(""); sx.Children.Add(notaFeed);
            var rigaFeed = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            var bNec = new Button { Content = "Feed dal modello NEC", Margin = new Thickness(0, 0, 6, 4) };
            ToolTip.SetTip(bNec, "Usa il diagramma calcolato da YA-NEC sul modello aperto (feed puntato lungo il suo asse): media in potenza dei piani E e H. Il taper qui sopra non si usa piu'.");
            bNec.Click += (s2, e2) => UsaFeedNec();
            var bCsv = new Button { Content = "Carica diagramma (CSV, W1GHZ)", Margin = new Thickness(0, 0, 6, 4) };
            ToolTip.SetTip(bCsv, "Due formati riconosciuti da soli. CSV di YA-NEC: angolo; dB  oppure  angolo; dB E; dB H (livelli). W1GHZ/DF3GJ: angolo, attenuazione in dB positiva, [fase]; commenti con ; // #. Per W1GHZ selezionare insieme i due file del piano E e del piano H.");
            bCsv.Click += async (s2, e2) => await CaricaFeedCsv();
            var bCos = new Button { Content = "Torna al cos^q", Margin = new Thickness(0, 0, 6, 4) };
            bCos.Click += (s2, e2) => ImpostaFeedDaNec(null, null, null);
            rigaFeed.Children.Add(bNec); rigaFeed.Children.Add(bCsv); rigaFeed.Children.Add(bCos);
            sx.Children.Add(rigaFeed);
            Spunta(sx, "Usa la fase del feed (se c'e')", () => inp.UsaFase, v => inp.UsaFase = v, "Con la fase (dal NEC o da un file W1GHZ a 3 colonne) si calcolano il centro di fase, la perdita per errore di fase e i centri dei piani E e H.");
            Spunta(sx, "Centro di fase automatico", () => inp.CentroFaseAuto, v => inp.CentroFaseAuto = v, "Spostamento lungo l'asse del feed che rende massimo il guadagno del disco (criterio di W1GHZ). Togliendo la spunta si usa il valore scritto sotto.");
            Campo(sx, "Centro di fase manuale", "mm", () => inp.CentroFaseManuale_mm, v => inp.CentroFaseManuale_mm = v, "Posizione del centro di fase rispetto all'origine del modello del feed, + verso il disco. Serve solo senza la spunta qui sopra.");
            Campo(sx, "Diametro del feed", "mm", () => inp.FeedDiametro_m * 1000, v => inp.FeedDiametro_m = v / 1000, "Diametro dell'ombra del feed (tromba, scatola dell'LNA): e' il blocco centrale.");
            Campo(sx, "Massa feed + LNA + rele'", "kg", () => inp.FeedMassa_kg, v => inp.FeedMassa_kg = v, "Massa sul fuoco: entra nel baricentro e nella flessione dei puntoni.");
            Campo(sx, "Perdite varie", "dB", () => inp.PerditeVarie_dB, v => inp.PerditeVarie_dB = v, "ROS del feed, errori di fase, polarizzazione incrociata, cavo. W1GHZ: un disco reale rende almeno il 15% meno del calcolo (0,7 dB).");

            sx.Children.Add(Famiglia("Puntoni del feed"));
            Campo(sx, "Numero di puntoni", "", () => inp.NPuntoni, v => inp.NPuntoni = (int)Math.Round(v), "3 o 4. Con 2 non c'e' rigidezza fuori dal loro piano.");
            Campo(sx, "Diametro tubo", "mm", () => inp.PuntoneDiam_mm, v => inp.PuntoneDiam_mm = v, "Tubo tondo in alluminio: entra nel blocco, nel vento e nella rigidezza.");
            Campo(sx, "Spessore tubo", "mm", () => inp.PuntoneSpess_mm, v => inp.PuntoneSpess_mm = v, "Spessore della parete del tubo dei puntoni del feed: serve per la rigidezza e il peso.");
            Campo(sx, "Attacco (frazione del raggio)", "", () => inp.PuntoneAttacco_frac, v => inp.PuntoneAttacco_frac = v, "0,6-0,7: lontano dal bordo, dove la flessione sposterebbe il feed fuori fuoco.");

            sx.Children.Add(Famiglia("Struttura e superficie"));
            Campo(sx, "Centine", "", () => inp.NCentine, v => inp.NCentine = (int)Math.Round(v), "Numero di centine radiali (12-16 a 1296, 16-24 a 2320 per dischi da 2-3 m).");
            Campo(sx, "Massa lineare centina", "kg/m", () => inp.CentinaMassaLin_kg_m, v => inp.CentinaMassaLin_kg_m = v, "Peso per metro del profilo delle centine: entra nel peso del disco, nel baricentro e nel contrappeso.");
            Campo(sx, "Centina vista dal vento", "mm", () => inp.CentinaFronte_mm, v => inp.CentinaFronte_mm = v, "Larghezza della centina vista con il disco di fronte al vento.");
            Campo(sx, "Anelli", "", () => inp.NAnelli, v => inp.NAnelli = (int)Math.Round(v), "Anelli concentrici equidistanti; l'ultimo e' il bordo.");
            Campo(sx, "Massa lineare anello", "kg/m", () => inp.AnelloMassaLin_kg_m, v => inp.AnelloMassaLin_kg_m = v, "Peso per metro dell'anello di bordo: entra nel peso del disco, nel baricentro e nel contrappeso.");
            Campo(sx, "Anello visto dal vento", "mm", () => inp.AnelloFronte_mm, v => inp.AnelloFronte_mm = v, "Larghezza dell'anello di bordo vista dal vento: entra nella spinta del vento.");
            // 0.4.20 (IC8TEM 14/11/2026 18:20, "arcarecci dritti"; 18:21 "cosi' abbiamo i pesi meglio distribuiti")
            Spunta(sx, "Arcarecci diritti dimensionati dal calcolo", () => inp.ArcarecciCalcolati, v => inp.ArcarecciCalcolati = v,
                "Ogni anello e' fatto di tratti di tubo diritti da centina a centina. Il calcolo sceglie per ogni anello il tubo piu' leggero che regge vento e peso " +
                "(trave appoggiata sulle due centine; tutta la rete a carico degli arcarecci, a favore di sicurezza) con freccia col vento fino a L/200 e freccia permanente fino a lambda/40; " +
                "massa e larghezza al vento di ogni anello entrano nella spinta, nel baricentro e nel contrappeso al loro raggio, al posto dei due valori scritti sopra. Tabella nella pagina Centina.");
            {
                var cbMat = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 4) };
                var nomiMat = new List<string>();
                foreach (var mt in Parabola.MaterialiArcarecci) nomiMat.Add(mt.Nome);
                cbMat.ItemsSource = nomiMat; cbMat.SelectedIndex = Math.Max(0, Math.Min(nomiMat.Count - 1, inp.ArcarecciMateriale));
                ToolTip.SetTip(cbMat, "Materiale degli arcarecci: modulo elastico, snervamento (o resistenza a flessione per la vetroresina) e densita'. Sicurezza 1,5 per i metalli, 3 per la vetroresina.");
                letture.Add(() => inp.ArcarecciMateriale = Math.Max(0, cbMat.SelectedIndex));
                cbMat.SelectionChanged += (s, e) => { timer.Stop(); timer.Start(); };
                sx.Children.Add(cbMat);
            }
            Campo(sx, "Raggio del mozzo (dima)", "mm", () => inp.DimaMozzo_mm, v => inp.DimaMozzo_mm = v, "La dima della centina parte da questo raggio (dove la centina si fissa al mozzo).");
            Campo(sx, "Altezza della centina (dima)", "mm", () => inp.DimaAltezza_mm, v => inp.DimaAltezza_mm = v, "Altezza della centina dietro la faccia su cui appoggia la rete: la dima disegna anche il dorso. 0 = solo il profilo.");
            Spunta(sx, "Dima su fogli A3 (altrimenti A4)", () => inp.DimaFoglioA3, v => inp.DimaFoglioA3 = v, "Formato dei fogli del PDF della dima: A3 verticale (area utile 277 x 400 mm, meno fogli da unire) o A4 verticale (190 x 277 mm).");
            Spunta(sx, "Dima su fogli orizzontali (altrimenti verticali)", () => inp.DimaFoglioOrizzontale, v => inp.DimaFoglioOrizzontale = v, "Fogli del PDF della dima in orizzontale: la centina e' lunga e bassa, cosi' spesso servono meno fogli.");
            Campo(sx, "Arcarecci: lato minimo del tubo", "mm", () => inp.ArcarecciLatoMin_mm, v => inp.ArcarecciLatoMin_mm = v, "Il calcolo non scende sotto questo diametro (o lato del quadro): un tubo troppo sottile non regge il montaggio della rete e falsa il peso. Di partenza 16 mm (scelta di IC8TEM).");
            Campo(sx, "Arcarecci: spessore minimo del tubo", "mm", () => inp.ArcarecciSpessMin_mm, v => inp.ArcarecciSpessMin_mm = v, "Spessore minimo della parete del tubo degli arcarecci. Di partenza 1,5 mm.");
            Spunta(sx, "Arcarecci in tubo quadro (altrimenti tondo)", () => inp.ArcarecciQuadri, v => inp.ArcarecciQuadri = v, "Il tubo quadro si fora e si fissa piu' facilmente alle centine; a parita' di peso e' un po' piu' rigido.");
            Campo(sx, "Mozzo + flangia", "kg", () => inp.MozzoMassa_kg, v => inp.MozzoMassa_kg = v, "Peso del mozzo centrale e della flangia di attacco: entra nel peso e nel baricentro.");
            Spunta(sx, "Superficie piena (lamiera)", () => inp.SuperficiePiena, v => inp.SuperficiePiena = v, "Pannelli pieni invece della rete: niente perdite di rete, ma vento come un disco pieno.");
            Campo(sx, "Foro della rete (luce)", "mm", () => inp.ReteForo_mm, v => inp.ReteForo_mm = v, "Luce netta della maglia quadra. Regola pratica: lambda/10 o meno (lambda/4 e' il limite in cui la rete smette di essere uno schermo).");
            Campo(sx, "Filo della rete", "mm", () => inp.ReteFilo_mm, v => inp.ReteFilo_mm = v, "Diametro del filo della rete: con il passo dei fori da' la trasmissione attraverso la rete (W1GHZ, Otoshi) e la superficie esposta al vento.");
            Campo(sx, "Massa superficiale", "kg/m2", () => inp.ReteMassaSup_kg_m2, v => inp.ReteMassaSup_kg_m2 = v, "Rete 0,85 kg/m2 circa; lamiera di alluminio 1,5 mm = 4,05 kg/m2.");
            Campo(sx, "Errore RMS della superficie", "mm", () => inp.ErroreRmsCostruttivo_mm, v => inp.ErroreRmsCostruttivo_mm = v, "Scostamento RMS dal paraboloide (centine, rete, montaggio). Ruze: lambda/20 costa 1,7 dB, lambda/40 0,4 dB.");
            Spunta(sx, "Aggiungi la sfaccettatura della rete piana", () => inp.IncludiSfaccettatura, v => inp.IncludiSfaccettatura = v, "Caso peggiore: rete tesa piatta tra due centine, senza sagomatura. Stima geometrica (Ruze) piu' il bordo a poligono tra le punte delle centine.");
            {
                // 0.4.20 (IC8TEM 14/11/2026 12:55, "centine: tutti e due"): il disco come griglia di fili per il NEC-2, ideale o sfaccettato
                // secondo la casella sopra; maglia lambda/10 ridotta a 0,087 lambda perche' nessun segmento superi lambda/10 (prova 7 con EZNEC)
                var esito = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = new SolidColorBrush(CFam) };
                var bN = new Button { Content = "Modello NEC del disco (griglia di fili)", Margin = new Thickness(0, 2, 0, 0) };
                ToolTip.SetTip(bN, "Crea il disco come griglia di fili (maglia circa lambda/10, centine, rete ideale o piana tra le centine secondo la casella sopra) " +
                    "con un dipolo e riflettore nel fuoco, e lo apre nella finestra principale. Verificato con EZNEC: disco da 1,2 m a 1296 MHz, scarto 0,06 %. " +
                    "Il calcolo cresce con il cubo dei segmenti: un disco da 1,2 m a 1296 MHz ha circa 7000 segmenti (0,8 GB, alcuni minuti). " +
                    "Limiti solo per il modello NEC: diametro fino a 1,2 m e al massimo 12000 segmenti, cioe' circa 7 lambda: 1296 MHz 1,2 m; 2320 MHz 0,9 m; 3400 MHz 0,6 m; 5760 MHz 0,35 m; 10368 MHz 0,2 m (f/D 0,42, 16 centine). " +
                    "Il calcolo numerico delle prestazioni della scheda non ha limiti di diametro.");
                bN.Click += (s, e) =>
                {
                    if (ApriNec == null) { esito.Text = "Apertura del modello non disponibile qui."; return; }
                    foreach (var l in letture) l();
                    const double maglia = 0.087;
                    // 0.4.20 (IC8TEM 14/11/2026 18:11, "120cm come disco simulato nel NEC, ma limite quasi infinito per il calcolo delle
                    // prestazioni nella sottolinguetta"): dopo la prova della parabola da 3 m (30566 segmenti, 16,5 GB, decine di minuti) il
                    // modello NEC del disco si crea solo fino a 1,2 m (verificato con EZNEC a 1296 MHz); il calcolo d'apertura non ha limiti.
                    if (inp.Diametro_m > DiscoNecMax_m + 1e-9)
                    {
                        esito.Text = string.Format(IT, "Modello NEC del disco solo fino a {0:0.##} m di diametro (qui {1:0.##} m): oltre, matrice e tempi non stanno su un PC " +
                            "(il disco da 3 m a 1296 MHz ha 30566 segmenti, 16,5 GB e decine di minuti). Le prestazioni di questo disco sono nel calcolo d'apertura qui a fianco, " +
                            "che vale per qualsiasi diametro e tiene conto delle centine.", DiscoNecMax_m, inp.Diametro_m);
                        return;
                    }
                    int n = ParabolaNec.Segmenti(inp.FreqMHz, inp.Diametro_m, inp.FsuD, inp.NCentine, maglia);
                    double gb = 16.0 * n * (double)n / 1e9;
                    if (n > 12000)
                    {
                        // 0.4.20 (IC8TEM 18:14, "proponi tu dei diametri massimi"): diametro massimo a questa frequenza, circa 7 lambda
                        // (1296 MHz 1,2 m per la scelta di IC8TEM; 2320 MHz 0,9 m; 3400 MHz 0,6 m; 5760 MHz 0,35 m; 10368 MHz 0,2 m)
                        double dmax = DiametroNecMassimo(inp.FreqMHz, inp.FsuD, inp.NCentine, maglia);
                        esito.Text = string.Format(IT, "Troppo grande per il NEC-2 su un PC: {0} segmenti, matrice di {1:0.0} GB (massimo 12000 segmenti). A {2:0.###} MHz il modello NEC " +
                            "si crea fino a {3:0.00} m di diametro; il calcolo d'apertura qui a fianco vale per qualsiasi diametro e tiene gia' conto delle centine (casella sopra).", n, gb, inp.FreqMHz, dmax);
                        return;
                    }
                    int ns;
                    string mazzo = ParabolaNec.Mazzo(inp.FreqMHz, inp.Diametro_m, inp.FsuD, inp.NCentine, maglia, inp.IncludiSfaccettatura, out ns);
                    esito.Text = string.Format(IT, "{0} segmenti, matrice di {1:0.00} GB: aperto nella finestra principale (calcolo alla frequenza centrale; lo sweep si avvia con Sweep).", ns, gb);
                    try { ApriNec?.Invoke(mazzo, string.Format(IT, "Parabola {0:0.##} m {1} centine {2}.nec", inp.Diametro_m, inp.NCentine, inp.IncludiSfaccettatura ? "sfaccettata" : "ideale")); }
                    catch (Exception ex) { esito.Text = "Apertura non riuscita: " + ex.Message; }
                };
                sx.Children.Add(bN);
                sx.Children.Add(esito);
            }

            sx.Children.Add(Famiglia("Bilanciamento e vento"));
            Campo(sx, "Asse di elevazione dietro il vertice", "m", () => inp.PivotDietroVertice_m, v => inp.PivotDietroVertice_m = v, "Distanza dell'asse di elevazione dietro il vertice del disco: decide il momento da bilanciare e il contrappeso.");
            Campo(sx, "Braccio del contrappeso", "m", () => inp.BraccioContrappeso_m, v => inp.BraccioContrappeso_m = v, "Distanza del contrappeso dall'asse di elevazione, verso il retro.");
            Campo(sx, "Diametro del contrappeso", "mm", () => inp.ContrappesoDiam_mm, v => inp.ContrappesoDiam_mm = v, "Per calcolare la lunghezza del cilindro in acciaio o piombo.");
            Campo(sx, "Raffica di progetto", "km/h", () => inp.Vento_kmh, v => inp.Vento_kmh = v, "Velocita' del vento per il calcolo della spinta sul disco, sui puntoni e sul supporto.");

            sx.Children.Add(Famiglia("Fuori fuoco e puntamento"));
            Campo(sx, "Feed spostato in asse", "mm", () => inp.FeedSpostAssiale_mm, v => inp.FeedSpostAssiale_mm = v, "Distanza del centro di fase dal fuoco lungo l'asse.");
            Campo(sx, "Feed spostato di lato", "mm", () => inp.FeedSpostLaterale_mm, v => inp.FeedSpostLaterale_mm = v, "Il fascio si sposta dalla parte opposta, di BDF x spostamento / F.");
            Campo(sx, "Errore di puntamento", "gradi", () => inp.ErrorePuntamento_deg, v => inp.ErrorePuntamento_deg = v, "Precisione reale del sistema (taratura, passo del rotore, giochi).");
            Campo(sx, "HPBW del corrispondente", "gradi", () => inp.HpbwCorrispondente_deg, v => inp.HpbwCorrispondente_deg = v, "0 = stesso disco all'altro capo (eco proprio).");
            Campo(sx, "Passo della tabella centina", "mm", () => inp.PassoProfilo_mm, v => inp.PassoProfilo_mm = v, "Distanza tra i punti della tabella della centina (profondita' a ogni passo dal centro): piu' piccolo, piu' righe da segnare.");

            var scroll = new ScrollViewer { Content = sx, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetColumn(scroll, 0);
            root.Children.Add(scroll);

            // ---------------- colonna dei risultati
            var dx = new DockPanel { Margin = new Thickness(0, 8, 8, 8) };
            var barra = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            foreach (var kv in new[] { ("riepilogo", "Riepilogo"), ("feed", "Feed"), ("lobi", "Lobi"), ("meccanica", "Meccanica"), ("centina", "Centina"), ("bande", "Bande ed EME"), ("fonti", "Fonti") })
                barra.Children.Add(Pillola(kv.Item1, kv.Item2));
            DockPanel.SetDock(barra, Dock.Top);
            dx.Children.Add(barra);
            titoloRisultato = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(CFam), Margin = new Thickness(2, 0, 0, 6), TextWrapping = TextWrapping.Wrap };
            DockPanel.SetDock(titoloRisultato, Dock.Top);
            dx.Children.Add(titoloRisultato);
            pannelloAvvisi = new StackPanel { Spacing = 3, Margin = new Thickness(0, 6, 0, 0) };
            var scAvv = new ScrollViewer { Content = pannelloAvvisi, MaxHeight = 170, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            DockPanel.SetDock(scAvv, Dock.Bottom);
            dx.Children.Add(scAvv);
            hostPagina = new ContentControl();
            dx.Children.Add(hostPagina);
            Grid.SetColumn(dx, 1);
            root.Children.Add(dx);
            Seleziona("riepilogo");
            return root;
        }

        TextBlock Famiglia(string t)
        {
            return new TextBlock { Text = t, FontSize = 15, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(CFam), Margin = new Thickness(0, 10, 0, 2) };
        }

        internal static TextBlock Nota(string t)
        {
            return new TextBlock { Text = t, FontSize = 12, Foreground = new SolidColorBrush(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
        }

        TextBox Campo(StackPanel sp, string etichetta, string unita, Func<double> get, Action<double> set, string fumetto)
        {
            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,90,46") };
            var lb = new TextBlock { Text = etichetta, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(CFam) };
            var tb = new TextBox { Text = Fmt(get()), HorizontalContentAlignment = HorizontalAlignment.Right, Margin = new Thickness(4, 0, 4, 0) };
            var un = new TextBlock { Text = unita, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Pal.Muted) };
            Grid.SetColumn(tb, 1); Grid.SetColumn(un, 2);
            g.Children.Add(lb); g.Children.Add(tb); g.Children.Add(un);
            if (!string.IsNullOrEmpty(fumetto)) { ToolTip.SetTip(lb, fumetto); ToolTip.SetTip(tb, fumetto); }
            letture.Add(() =>
            {
                double v;
                if (Leggi(tb.Text, out v)) { set(v); tb.Foreground = Pal.B(Pal.FieldFore); }
                else tb.Foreground = new SolidColorBrush(Pal.Red);
            });
            tb.TextChanged += (s, e) => { timer.Stop(); timer.Start(); };
            sp.Children.Add(g);
            return tb;
        }

        void Spunta(StackPanel sp, string testo, Func<bool> get, Action<bool> set, string fumetto)
        {
            var c = new CheckBox { Content = testo, IsChecked = get(), Foreground = new SolidColorBrush(CFam) };
            if (!string.IsNullOrEmpty(fumetto)) ToolTip.SetTip(c, fumetto);
            letture.Add(() => set(c.IsChecked == true));
            c.Click += (s, e) => { timer.Stop(); timer.Start(); };
            sp.Children.Add(c);
        }

        internal static string Fmt(double v) { return v.ToString("0.###", IT); }

        internal static bool Leggi(string s, out double v)
        {
            s = (s ?? "").Trim().Replace(" ", "").Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && !double.IsNaN(v) && !double.IsInfinity(v);
        }

        Border Pillola(string key, string testo)
        {
            var t = new TextBlock { Text = testo, FontSize = 14, Foreground = new SolidColorBrush(CBluTesto) };
            var b = new Border
            {
                Child = t, Background = Pal.B(Pal.Pill), BorderBrush = new SolidColorBrush(CBordo), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14), Padding = new Thickness(12, 5), Margin = new Thickness(0, 0, 6, 4), Cursor = new Cursor(StandardCursorType.Hand)
            };
            b.PointerPressed += (s, e) => { Fascione.TogliFuoco(b); Seleziona(key); };  // 1.2.0: fuoco tolto prima del cambio di pagina (registro 13/11 17:58)
            pillole[key] = b;
            return b;
        }

        /// <summary>Pagine a pillola, per il collaudo automatico (0.4.13): chiavi e apertura di una pagina.</summary>
        public IEnumerable<string> ChiaviPagine() { return new List<string>(pillole.Keys); }
        public void ApriPagina(string key) { Seleziona(key); }

        void Seleziona(string key)
        {
            pagina = key;
            foreach (var kv in pillole)
            {
                bool sel = kv.Key == key;
                kv.Value.Background = sel ? new SolidColorBrush(CBlu) : Pal.B(Pal.Pill);
                ((TextBlock)kv.Value.Child).Foreground = sel ? Pal.B(Pal.AccentFore) : new SolidColorBrush(CBluTesto);
            }
            MostraPagina();
        }

        // ============================================================================================
        // calcolo
        // ============================================================================================
        void Ricalcola()
        {
            foreach (var l in letture) l();
            try { res = Parabola.Calcola(inp); }
            catch (Exception ex) { res = new ParabolaResult { Errore = "Errore nel calcolo: " + ex.Message }; }
            if (notaFeed != null)
                notaFeed.Text = (inp.FeedTabThetaDeg != null || inp.FeedPianiThetaDeg != null)
                    ? string.Format(IT, "Illuminatore: diagramma \"{0}\" (il taper qui sopra non si usa). Taper risultante al bordo {1:0.0} dB.", nomeFeedNec, res != null ? res.TaperBordoTab_dB : double.NaN)
                      + (res != null && res.FaseUsata ? string.Format(IT, " Centro di fase {0:+0.0;-0.0} mm dall'origine del modello (E {1:+0.0;-0.0}, H {2:+0.0;-0.0}; stima a -10 dB {3:+0.0;-0.0}).", res.CentroFase_mm, res.CentroFaseE_mm, res.CentroFaseH_mm, res.CentroFaseStima10_mm) : "")
                    : (res != null && res.Errore == null ? string.Format(IT, "Feed cos^q con q = {0:0.00}: taper del feed {1:0.0} dB + attenuazione spaziale {2:0.0} dB; guadagno del feed {3:0.0} dBi.", res.Q, res.TaperFeed_dB, res.AttSpaziale_dB, res.GuadagnoFeed_dBi) : "")
                      + (res != null && res.Errore == null ? string.Format(IT, " Free-space taper (Baars 4.3, 20 log[1 + (D/4f)^2]): {0:0.00} dB, taper totale al bordo {1:0.0} dB.", res.TfBaars_dB, res.TaperBordoTot_dB) : "");
            AggiornaAvvisi();
            MostraPagina();
        }

        void AggiornaAvvisi()
        {
            if (pannelloAvvisi == null) return;
            pannelloAvvisi.Children.Clear();
            if (res == null) return;
            if (res.Errore != null) { pannelloAvvisi.Children.Add(Avviso(2, res.Errore)); return; }
            foreach (var a in res.Avvisi.OrderByDescending(x => x.Livello)) pannelloAvvisi.Children.Add(Avviso(a.Livello, a.Testo));
            if (res.Avvisi.Count == 0) pannelloAvvisi.Children.Add(Avviso(0, "Nessun avviso."));
        }

        internal static Control Avviso(int livello, string testo)
        {
            Color bg = livello == 2 ? Pal.A(Pal.Dark ? 70 : 30, Pal.Red) : livello == 1 ? Pal.WarnBg : Pal.AccentSoft;
            Color fg = livello == 2 ? Pal.Red : livello == 1 ? Pal.WarnText : Pal.Accent;
            string pre = livello == 2 ? "CRITICO  " : livello == 1 ? "ATTENZIONE  " : "NOTA  ";
            return new Border
            {
                Background = new SolidColorBrush(bg), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4),
                Child = new TextBlock { Text = pre + testo, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(fg), FontSize = 12.5 }
            };
        }

        void MostraPagina()
        {
            if (hostPagina == null || res == null) return;
            if (res.Errore != null) { hostPagina.Content = Carta(Nota(res.Errore)); titoloRisultato.Text = ""; return; }
            titoloRisultato.Text = string.Format(IT, "{0:0.0} MHz  -  D {1:0.00} m, f/D {2:0.00}  -  guadagno {3:0.0} dBi, efficienza {4:0}%, HPBW {5:0.00} gradi",
                inp.FreqMHz, inp.Diametro_m, inp.FsuD, res.Guadagno_dBi, res.EtaTotale * 100, res.Hpbw_deg);
            if (pagina == "feed" && res.Feed != null)
                titoloRisultato.Text = string.Format(IT, "{0:0.0} MHz  -  D {1:0.00} m, f/D {2:0.00}  -  feed: {3}  -  efficienza d'apertura del feed {4:0.0} %  (blocco, superficie e rete esclusi)",
                    inp.FreqMHz, inp.Diametro_m, inp.FsuD, string.IsNullOrEmpty(nomeFeedNec) ? res.Feed.Descrizione : nomeFeedNec, res.Feed.TotAttuale * 100);
            Control c;
            switch (pagina)
            {
                case "feed": c = PaginaFeed(); break;
                case "lobi": c = PaginaLobi(); break;
                case "meccanica": c = PaginaMeccanica(); break;
                case "centina": c = PaginaCentina(); break;
                case "bande": c = PaginaBande(); break;
                case "fonti": c = PaginaFonti(); break;
                default: c = PaginaRiepilogo(); break;
            }
            hostPagina.Content = new ScrollViewer { Content = c, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        // ============================================================================================
        // pagine
        // ============================================================================================
        internal static Border Carta(Control c, string titolo = null)
        {
            Control inner = c;
            if (titolo != null)
            {
                var sp = new StackPanel { Spacing = 4 };
                sp.Children.Add(new TextBlock { Text = titolo, FontWeight = FontWeight.Bold, FontSize = 14, Foreground = new SolidColorBrush(CFam) });
                sp.Children.Add(c);
                inner = sp;
            }
            return new Border { Background = Pal.B(Pal.Panel), BorderBrush = new SolidColorBrush(CBordo), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 8), Child = inner };
        }

        // tabella etichetta | valore | nota
        internal static Grid Tabella(IEnumerable<string[]> righe, string colonne = "Auto,Auto,*")
        {
            var g = new Grid { ColumnDefinitions = new ColumnDefinitions(colonne) };
            int r = 0;
            foreach (var riga in righe)
            {
                g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                for (int c = 0; c < riga.Length; c++)
                {
                    bool intest = riga.Length > 0 && riga[0] != null && riga[0].StartsWith("#");
                    string txt = c == 0 && intest ? riga[0].Substring(1) : riga[c];
                    var tb = new TextBlock
                    {
                        Text = txt ?? "", Margin = new Thickness(0, 2, 14, 2), TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(c == 1 && !intest ? Pal.Fore : CFam),
                        FontWeight = intest || c == 1 ? FontWeight.SemiBold : FontWeight.Normal,
                        HorizontalAlignment = c == 1 && !intest && colonne == "Auto,Auto,*" ? HorizontalAlignment.Right : HorizontalAlignment.Left
                    };
                    Grid.SetRow(tb, r); Grid.SetColumn(tb, c);
                    g.Children.Add(tb);
                }
                r++;
            }
            return g;
        }

        internal static string D1(double v, string f = "0.00") { return double.IsNaN(v) || double.IsInfinity(v) ? "-" : v.ToString(f, IT); }
        internal static string Db(double eta) { return D1(-10 * Math.Log10(Math.Max(eta, 1e-12)), "0.00") + " dB"; }
        internal static string Pc(double eta) { return D1(eta * 100, "0.0") + " %"; }

        Control PaginaRiepilogo()
        {
            var sp = new StackPanel();
            var r = res;
            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "#Voce", "Efficienza", "Perdita" },
                new[] { "Spillover (energia che esce dal bordo)", Pc(r.EtaSpill), Db(r.EtaSpill) },
                new[] { "Illuminazione (taper)", Pc(r.EtaTaper), Db(r.EtaTaper) },
                new[] { r.FaseUsata ? string.Format(IT, "Fase del feed (residuo max {0:0} gradi E, {1:0} gradi H)", r.FaseResiduaE_deg, r.FaseResiduaH_deg) : "Fase del feed (non disponibile)", Pc(r.EtaFase), Db(r.EtaFase) },
                new[] { r.FaseUsata && !double.IsNaN(r.EtaFaseLimite) ? string.Format(IT, "   limite inferiore di Balanis (1 - m^2/2)^2, m = {0:0} gradi", r.FaseScartoMax_deg) : "   limite inferiore di Balanis (serve la fase del feed)", r.FaseUsata && !double.IsNaN(r.EtaFaseLimite) ? Pc(r.EtaFaseLimite) : "-", r.FaseUsata && !double.IsNaN(r.EtaFaseLimite) ? Db(r.EtaFaseLimite) : "" },
                new[] { "Asimmetria dei piani E e H (polarizzazione incrociata)", Pc(r.EtaPol), Db(r.EtaPol) },
                new[] { string.Format(IT, "Blocco: feed {0:0.0}% + puntoni {1:0.0}% dell'apertura pesata (di cui {2:0.0}% onda sferica, Baars 4.36)", r.BloccoCentrale_frac * 100, r.BloccoPuntoni_frac * 100, r.BloccoSferico_frac * 100), Pc(r.EtaBlocco), Db(r.EtaBlocco) },
                new[] { string.Format(IT, "Superficie (Ruze, {0:0.0} mm RMS = lambda/{1:0})", r.SigmaTot_mm, r.Lambda_mm / Math.Max(1e-9, r.SigmaTot_mm)), Pc(r.EtaRuze), Db(r.EtaRuze) },
                new[] { inp.SuperficiePiena ? "Superficie piena" : string.Format(IT, "Rete {0:0.#} mm = lambda/{1:0.0} (W1GHZ, Otoshi)", inp.ReteForo_mm, r.Lambda_mm / inp.ReteForo_mm), Pc(r.EtaRete), Db(r.EtaRete) },
                new[] { "Perdite varie (feed, cavo, fase)", Pc(r.EtaVarie), Db(r.EtaVarie) },
                new[] { "#Totale", Pc(r.EtaTotale), Db(r.EtaTotale) },
            }, "*,Auto,Auto"), "Bilancio dell'efficienza"));

            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "Guadagno ideale (apertura uniforme)", D1(r.GuadagnoIdeale_dBi, "0.00") + " dBi", "" },
                new[] { "Guadagno calcolato", D1(r.Guadagno_dBi, "0.00") + " dBi", "area efficace " + D1(r.AreaEfficace_m2, "0.00") + " m2" },
                new[] { "Larghezza del fascio a -3 dB", D1(r.Hpbw_deg, "0.000") + " gradi", string.Format(IT, "= {0:0.000} lambda/D; apertura uniforme 58,4 lambda/D: {1:0.000} gradi", r.HpbwCoeff, r.Hpbw58_deg) },
                new[] { "Primo nullo", D1(r.PrimoNullo_deg, "0.000") + " gradi", "" },
                new[] { "Primo lobo laterale", D1(r.PrimoLobo_dB, "0.0") + " dB", string.Format(IT, "a {0:0.000} gradi; senza blocco {1:0.0} dB", r.PrimoLobo_deg, r.PrimoLoboSenzaBlocco_dB) },
                new[] { "Rumore dalla rete (terreno dietro)", D1(r.RumoreRete_K, "0.0") + " K", "stima: trasmissione della rete x 290 K" },
                new[] { "Rumore dallo spillover", D1(r.RumoreSpill_K, "0") + " K", "stima grezza (meta' dello spillover sul terreno); il calcolo vero e' TANT" },
                new[] { "Fuori fuoco assiale per -0,25 dB", "+/-" + D1(r.TolleranzaAssiale025_mm, "0.0") + " mm", string.Format(IT, "con lo spostamento impostato: {0:0.00} dB (Baars 4.22-4.23: apertura uniforme {1:0.00} dB, quadratica {2:0.00} dB)", r.PerditaAssiale_dB, r.DefocusBaarsUnif_dB, r.DefocusBaarsQuad_dB) },
                new[] { "Guadagno massimo con questa superficie", double.IsNaN(r.GuadagnoMax_dBi) ? "-" : D1(r.GuadagnoMax_dBi, "0.0") + " dBi", double.IsNaN(r.LambdaMax_mm) ? "superficie senza errore" : string.Format(IT, "a lambda = 4 pi sigma = {0:0} mm ({1:0} MHz), Balanis 15-67..71", r.LambdaMax_mm, r.FreqGmax_MHz) },
                new[] { "Beam Deviation Factor", D1(r.BDF, "0.000"), string.Format(IT, "spostamento laterale impostato -> fascio spostato di {0:0.000} gradi", r.Strabismo_deg) },
            }), "Prestazioni"));
            return sp;
        }

        Control PaginaFeed()
        {
            var a = res.Feed;
            if (a == null) return Carta(Nota("Analisi del feed non disponibile."));
            Color BLU = CBlu, ARANCIO = Pal.Orange, VERDE = Color.Parse("#2E7D32"), VIOLA = Color.Parse("#7B3FA0"),
                TEAL = Color.Parse("#00838F"), ROSSO = Pal.Red, GR = Pal.Muted, MARRONE = Pal.WarnText, VERDE45 = Color.Parse("#6A8F2A");
            string psiTxt = "angolo di bordo " + a.Psi0_deg.ToString("0.0", IT) + "°";

            // 1. ampiezza
            var g1 = new ParabolaGrafico { Height = 330, Titolo = "1. Diagramma del feed (ampiezza)", TitoloX = "angolo dall'asse del feed (gradi)", TitoloY = "dB",
                XMin = 0, XMax = 180, PassoX = 30, FormatoX = "0", YMin = -40, YMax = 2, PassoY = 5, LegendaPos = 1 };
            g1.FasciaX1 = 0; g1.FasciaX2 = a.Psi0_deg; g1.FasciaColore = Pal.Yellow; g1.AlfaFasciaX = 30;
            g1.Serie_.Add(new ParabolaGrafico.Serie { X = a.Th, Y = a.DbE, Colore = BLU, Nome = "piano E" });
            g1.Serie_.Add(new ParabolaGrafico.Serie { X = a.Th, Y = a.DbH, Colore = ARANCIO, Nome = "piano H" });
            if (a.Db45 != null) g1.Serie_.Add(new ParabolaGrafico.Serie { X = a.Th, Y = a.Db45, Colore = VERDE45, Spessore = 1.2, Tratteggio = true, Nome = "piano a 45°" });
            g1.LineeY.Add((-3.0, "", ROSSO));
            g1.LineeY.Add((-10.0, "−10 dB", GR));
            g1.LineeX.Add((a.Psi0_deg, "", MARRONE));
            g1.Testi.Add((a.Psi0_deg + 1.5, -37.0, psiTxt, MARRONE, false));
            if (!double.IsNaN(a.Theta3E)) g1.Marcatori.Add((a.Theta3E, -3.0, BLU, "−3 dB a " + a.Theta3E.ToString("0", IT) + "°", 'v'));
            if (!double.IsNaN(a.Theta3H)) g1.Marcatori.Add((a.Theta3H, -3.0, ARANCIO, "−3 dB a " + a.Theta3H.ToString("0", IT) + "°", 'v'));

            // 2. fase
            Control c2;
            if (a.FaseE != null)
            {
                var g2 = new ParabolaGrafico { Height = 330, Titolo = "2. Fase del feed (copolare)", TitoloX = "angolo dall'asse del feed (gradi)", TitoloY = "gradi",
                    XMin = 0, XMax = 180, PassoX = 30, FormatoX = "0", YMin = -60, YMax = 60, PassoY = 20, LegendaPos = 1 };
                g2.FasciaY1 = -22.5; g2.FasciaY2 = 22.5; g2.FasciaYColore = VERDE; g2.FasciaYTesto = "±22,5° (λ/16, W1GHZ)";
                g2.Serie_.Add(new ParabolaGrafico.Serie { X = a.Th, Y = a.FaseE, Colore = BLU, Nome = "piano E (corretto al centro di fase)" });
                g2.Serie_.Add(new ParabolaGrafico.Serie { X = a.Th, Y = a.FaseH, Colore = ARANCIO, Spessore = 1.4, Tratteggio = true, Nome = "piano H (corretto)" });
                if (a.Fase45 != null) g2.Serie_.Add(new ParabolaGrafico.Serie { X = a.Th, Y = a.Fase45, Colore = VERDE45, Spessore = 1.2, Tratteggio = true, Nome = "piano a 45° (corretto)" });
                g2.Serie_.Add(new ParabolaGrafico.Serie { X = a.Th, Y = a.FaseEOrigine, Colore = GR, Spessore = 1.0, Tratteggio = true, Nome = "piano E riferito all'origine del modello" });
                g2.LineeX.Add((a.Psi0_deg, "", MARRONE));
                g2.Testi.Add((178.0, 50.0, "residuo entro il bordo: E " + res.FaseResiduaE_deg.ToString("0.0", IT) + "°, H " + res.FaseResiduaH_deg.ToString("0.0", IT) + "°", CFam, true));
                c2 = g2;
            }
            else
            {
                c2 = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "2. Fase del feed (copolare)", FontWeight = FontWeight.Bold, FontSize = 14, Foreground = new SolidColorBrush(CFam), Margin = new Thickness(0, 0, 0, 8) },
                        Nota("Fase non disponibile: il feed in uso non la fornisce (" + a.Descrizione + "). Si ottiene dal modello NEC del feed o da un file W1GHZ a 3 colonne; senza fase il centro di fase si assume nell'origine del modello.")
                    }
                };
            }

            // 3. efficienza in funzione di f/D
            var g3 = new ParabolaGrafico { Height = 330, Titolo = "3. Efficienza in funzione di f/D (centro di fase fisso)", TitoloX = "f/D", TitoloY = "%",
                XMin = 0.25, XMax = 1.0, PassoX = 0.1, FormatoX = "0.0", YMin = 0, YMax = 100, PassoY = 20, LegendaPos = 2 };
            Func<double[], double[]> pc = v => v.Select(x => 100.0 * x).ToArray();
            g3.Serie_.Add(new ParabolaGrafico.Serie { X = a.Fds, Y = pc(a.Spill), Colore = ARANCIO, Spessore = 1.3, Nome = "spillover" });
            g3.Serie_.Add(new ParabolaGrafico.Serie { X = a.Fds, Y = pc(a.Taper), Colore = VERDE, Spessore = 1.3, Nome = "illuminazione" });
            g3.Serie_.Add(new ParabolaGrafico.Serie { X = a.Fds, Y = pc(a.Pol), Colore = VIOLA, Spessore = 1.3, Nome = "asimmetria E/H" });
            g3.Serie_.Add(new ParabolaGrafico.Serie { X = a.Fds, Y = pc(a.Fase), Colore = TEAL, Spessore = 1.3, Nome = "fase" });
            g3.Serie_.Add(new ParabolaGrafico.Serie { X = a.Fds, Y = pc(a.Tot), Colore = BLU, Spessore = 2.6, Nome = "totale del feed" });
            g3.Serie_.Add(new ParabolaGrafico.Serie { X = a.Fds, Y = pc(a.TotSenzaFase), Colore = BLU, Spessore = 1.2, Tratteggio = true, Nome = "totale senza fase" });
            g3.Marcatori.Add((a.FdsMax, 100.0 * a.TotMax, BLU, "massimo " + (100.0 * a.TotMax).ToString("0", IT) + "% a f/D " + a.FdsMax.ToString("0.000", IT), 'o'));
            g3.LineeX.Add((inp.FsuD, "", CFam));
            g3.Testi.Add((inp.FsuD + 0.005, 4.0, "il tuo disco", CFam, false));

            // 4. efficienza in funzione della posizione (mm e lambda)
            double ymax = Math.Ceiling(a.EffDz.Max() / 10.0) * 10.0 + 20.0;
            var g4 = new ParabolaGrafico { Height = 330, Titolo = "4. Efficienza in funzione della posizione del feed (f/D " + inp.FsuD.ToString("0.00", IT) + ")",
                TitoloX = "distanza del centro di fase dal fuoco, lungo l'asse (mm)", TitoloY = "%",
                XMin = a.Dz_mm[0], XMax = a.Dz_mm[a.Dz_mm.Length - 1], FormatoX = "0", YMin = 0, YMax = ymax, PassoY = 10,
                ScalaX2 = 1.0 / a.Lambda_mm, TitoloX2 = "λ", FormatoX2 = "0.00" };
            g4.Serie_.Add(new ParabolaGrafico.Serie { X = a.Dz_mm, Y = a.EffDz, Colore = BLU, Nome = "calcolata" });
            if (a.EffDzBaars != null) g4.Serie_.Add(new ParabolaGrafico.Serie { X = a.Dz_mm, Y = a.EffDzBaars, Colore = GR, Tratteggio = true, Spessore = 1.2, Nome = "Baars 4.22 (apertura uniforme)" });
            if (!double.IsNaN(a.TolMin_mm) && !double.IsNaN(a.TolMax_mm))
            {
                g4.FasciaX1 = a.TolMin_mm; g4.FasciaX2 = a.TolMax_mm; g4.FasciaColore = VERDE; g4.AlfaFasciaX = 26;
                g4.Testi.Add((a.TolMin_mm + 2, Math.Min(ymax - 5, a.EffDz.Max() * 1.18), "entro −0,25 dB: da " + a.TolMin_mm.ToString("+0;-0", IT) + " a " + a.TolMax_mm.ToString("+0;-0", IT) + " mm (" + (a.TolMin_mm / a.Lambda_mm).ToString("+0.00;-0.00", IT) + " / " + (a.TolMax_mm / a.Lambda_mm).ToString("+0.00;-0.00", IT) + " λ)", VERDE, false));
            }
            g4.LineeX.Add((0.0, "", ROSSO));
            g4.Testi.Add((3.0, 8.0, "centro di fase nel fuoco", ROSSO, false));
            if (a.ConFase && Math.Abs(a.CentroUsato_mm) > 0.5)
            {
                g4.LineeX.Add((-a.CentroUsato_mm, "", GR));
                g4.Testi.Add((-a.CentroUsato_mm + 3, 20.0, "origine del modello nel fuoco: " + a.PerditaOrigine_dB.ToString("0.00", IT) + " dB", GR, false));
            }

            var gr = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto") };
            Control[] cs = { Carta(g1), Carta(c2), Carta(g3), Carta(g4) };
            for (int i = 0; i < 4; i++)
            {
                cs[i].Margin = new Thickness(i % 2 == 0 ? 0 : 4, 0, i % 2 == 0 ? 4 : 0, 8);
                Grid.SetColumn(cs[i], i % 2); Grid.SetRow(cs[i], i / 2);
                gr.Children.Add(cs[i]);
            }
            return gr;
        }

        Control PaginaLobi()
        {
            var sp = new StackPanel();
            var r = res;
            var gr = new ParabolaGrafico { Height = 380, Titolo = "Diagramma dei lobi (simmetria circolare, piano qualsiasi)", TitoloX = "angolo dall'asse (gradi)", TitoloY = "dB" };
            double xm = r.ThetaDeg[r.ThetaDeg.Length - 1];
            gr.XMin = -xm; gr.XMax = xm; gr.YMin = -50; gr.YMax = 0; gr.PassoY = 10;
            gr.FormatoX = xm < 2 ? "0.00" : xm < 10 ? "0.0" : "0";
            gr.Serie_.Add(new ParabolaGrafico.Serie { X = r.ThetaDeg, Y = r.PattDb, Specchia = true, Nome = "con il blocco del feed" });
            if (mostraSenzaBlocco) gr.Serie_.Add(new ParabolaGrafico.Serie { X = r.ThetaDeg, Y = r.PattDbSenzaBlocco, Specchia = true, Tratteggio = true, Spessore = 1.2, Colore = CGrigio, Nome = "senza blocco" });
            if (r.PattErroreDb != null && r.PiccoErrore_dB > -50) gr.Serie_.Add(new ParabolaGrafico.Serie { X = r.ThetaDeg, Y = r.PattErroreDb, Specchia = true, Tratteggio = true, Spessore = 1.2, Colore = Pal.Orange, Nome = "fascio d'errore (Baars 4.44-4.46)" });
            var ros = Pal.Red;
            gr.LineeY.Add((-3.0, "-3 dB", ros));
            gr.LineeX.Add((-r.Hpbw_deg / 2, "", ros));
            gr.LineeX.Add((r.Hpbw_deg / 2, string.Format(IT, "HPBW {0:0.000} gradi", r.Hpbw_deg), ros));
            if (r.Hpbw_deg < 3)
            {
                gr.FasciaX1 = -Parabola.LunaDiametro_deg / 2; gr.FasciaX2 = Parabola.LunaDiametro_deg / 2; gr.FasciaTesto = "Luna";
            }
            sp.Children.Add(Carta(gr));
            var opz = new CheckBox { Content = "Mostra la curva senza blocco", IsChecked = mostraSenzaBlocco };
            opz.Click += (s, e) => { mostraSenzaBlocco = opz.IsChecked == true; MostraPagina(); };
            var bt = new Button { Content = "Esporta il diagramma (CSV)" };
            bt.Click += async (s, e) => await Salva("diagramma_parabola.csv", Parabola.DiagrammaCsv(res));
            var riga = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            riga.Children.Add(opz); riga.Children.Add(bt);
            sp.Children.Add(Carta(riga));
            double lD = r.Lambda_mm / 1000.0 / inp.Diametro_m;
            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "#Formula o fonte", "HPBW", "Nota" },
                new[] { "Calcolato (integrazione d'apertura)", D1(r.Hpbw_deg, "0.000") + " gradi", string.Format(IT, "{0:0.000} lambda/D con l'illuminazione impostata", r.HpbwCoeff) },
                new[] { "Apertura uniforme: 58,4 lambda/D", D1(r.Hpbw58_deg, "0.000") + " gradi", "Balanis tab. 12.2 (29,2 gradi / (a/lambda)); DF3GJ usa 58 lambda/D. Vale solo senza taper: sottostima il fascio reale" },
                new[] { "Taper -10 dB (tabelle)", D1(1.14 * lD * 180 / Math.PI, "0.000") + " - " + D1(1.17 * lD * 180 / Math.PI, "0.000") + " gradi", "1,14-1,17 lambda/D: Stutzman (via Sengupta 2021) e GMRT tab. 19.1" },
                new[] { string.Format(IT, "Baars 4.13 (taper {0:0.0} dB)", r.TaperBordoTot_dB), r.HpbwBaars_deg > 0 ? D1(r.HpbwBaars_deg, "0.000") + " gradi" : "-", r.HpbwBaars_deg > 0 ? string.Format(IT, "{0:0.000} lambda/D, distribuzione quadratica con t = {1:0.00} al bordo (feed + spazio)", r.HpbwBaarsCoeff, r.TBaars) : "fuori dall'intervallo della formula (0,1 < t < 1)" },
                new[] { "Primo lobo, Baars 4.14a (-24,6 + 7,37 t)", r.HpbwBaars_deg > 0 ? D1(r.PrimoLoboBaars_dB, "0.0") + " dB" : "-", string.Format(IT, "calcolato {0:0.0} dB (senza blocco {1:0.0} dB): la formula non conta il blocco", r.PrimoLobo_dB, r.PrimoLoboSenzaBlocco_dB) },
                new[] { "Fascio d'errore, Baars 4.44-4.46", r.HpbwErrore_deg > 0 ? D1(r.HpbwErrore_deg, "0.00") + " gradi" : "-", r.HpbwErrore_deg > 0 ? string.Format(IT, "1,06 lambda/c con c = {0:0.00} m (distanza tra le centine); potenza 1,1 (e^s^2 - 1) = {1:0.0000}, picco {2:0.0} dB", r.ErroreC_m, r.PotenzaErrore, r.PiccoErrore_dB) : "superficie senza errore" },
                new[] { "Regola pratica 70 lambda/D", D1(r.Hpbw70_deg, "0.000") + " gradi", "1,22 lambda/D: corrisponde a un taper forte (circa -14/-18 dB nelle tabelle)" },
                new[] { "Guadagno dal fascio, 41253 / HPBW^2", D1(r.GuadagnoDaFascio_dBi, "0.0") + " dBi", string.Format(IT, "W1GHZ app. 6A: e' una direttivita' ottimista; il calcolo da' {0:0.0} dBi", r.Guadagno_dBi) },
            }), "Larghezza del fascio: confronto tra formule e fonti"));
            sp.Children.Add(Carta(Nota("Il fascio dipende dal diametro e dall'illuminazione, non dalle perdite: un disco sovrailluminato ha il fascio piu' stretto ma rende meno (W1GHZ, app. 6A). "
                + "Per le tolleranze di puntamento in EME si usa l'HPBW calcolato; con 58 lambda/D le tolleranze risulterebbero piu' strette del necessario.")));
            sp.Children.Add(Carta(Nota("Metodo: integrazione d'apertura con feed cos^q (o diagramma NEC del feed) e attenuazione spaziale; il blocco del feed e' sottratto al centro. "
                + "I puntoni non sono nel diagramma (non hanno simmetria circolare): danno lobi a stella nei loro piani e sono contati solo nell'efficienza. "
                + "Gli errori di superficie abbassano il guadagno (Ruze) e spargono l'energia in un fondo largo: la curva tratteggiata arancio e' la stima di Baars (fascio d'errore gaussiano), non un calcolo. "
                + "Valido vicino all'asse (qualche decina di larghezze del fascio); spillover e lobi posteriori non sono nel diagramma.")));
            return sp;
        }

        Control PaginaMeccanica()
        {
            var sp = new StackPanel();
            var r = res;
            sp.Children.Add(Carta(new ParabolaSezione { Inp = inp, Res = r, Height = 340 }, "Sezione in scala"));
            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "Distanza focale F", D1(r.Fuoco_m * 1000, "0.0") + " mm", "dal vertice al fuoco (centro di fase del feed)" },
                new[] { "Profondita' al bordo", D1(r.Profondita_m * 1000, "0.0") + " mm", "D^2 / 16F" },
                new[] { "Angolo di bordo dal fuoco", D1(r.Psi0_deg, "0.0") + " gradi", "semiangolo che il feed deve illuminare" },
                new[] { "Area dell'apertura / della superficie", D1(r.AreaApertura_m2, "0.00") + " / " + D1(r.AreaSuperficie_m2, "0.00") + " m2", "" },
                new[] { "Lunghezza di una centina", D1(r.LunghezzaCentina_m * 1000, "0") + " mm", "lungo la curva, dal centro al bordo" },
            }), "Geometria"));
            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "#Parte", "Massa", "Baricentro (dal vertice)" },
                new[] { "Rete / superficie", D1(r.MassaRete_kg, "0.00") + " kg", D1(r.ZRete_m * 1000, "0") + " mm" },
                new[] { "Centine", D1(r.MassaCentine_kg, "0.00") + " kg", D1(r.ZCentine_m * 1000, "0") + " mm" },
                new[] { "Anelli", D1(r.MassaAnelli_kg, "0.00") + " kg", D1(r.ZAnelli_m * 1000, "0") + " mm" },
                new[] { "Mozzo + flangia", D1(inp.MozzoMassa_kg, "0.00") + " kg", "-50 mm" },
                new[] { "Puntoni", D1(r.PuntoniMassa_kg, "0.00") + " kg", D1(r.ZPuntoni_m * 1000, "0") + " mm" },
                new[] { "Feed + LNA + rele'", D1(inp.FeedMassa_kg, "0.00") + " kg", D1(r.Fuoco_m * 1000, "0") + " mm" },
                new[] { "#Totale davanti all'asse", D1(r.MassaTotale_kg, "0.00") + " kg", D1(r.Baricentro_m * 1000, "0") + " mm" },
            }, "*,Auto,Auto"), "Pesi e baricentro"));
            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "Momento sbilanciato sull'asse", D1(r.MomentoSbilanciato_kgm, "0.00") + " kg m", string.Format(IT, "= {0:0} N m all'elevazione 0", r.CoppiaSbilanciata_Nm) },
                new[] { "Contrappeso", D1(r.Contrappeso_kg, "0.0") + " kg", string.Format(IT, "a {0:0.00} m dall'asse", inp.BraccioContrappeso_m) },
                new[] { "Cilindro in acciaio", string.Format(IT, "diam. {0:0} x {1:0} mm", inp.ContrappesoDiam_mm, r.ContrappesoAcciaioL_mm), "7850 kg/m3" },
                new[] { "Cilindro in piombo", string.Format(IT, "diam. {0:0} x {1:0} mm", inp.ContrappesoDiam_mm, r.ContrappesoPiomboL_mm), "11340 kg/m3" },
                new[] { "Massa che ruota in elevazione", D1(r.MassaRotante_kg, "0.0") + " kg", "disco, feed e contrappeso" },
            }), "Bilanciamento sull'asse di elevazione"));
            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "Lunghezza di un puntone", D1(r.PuntoneLunghezza_m * 1000, "0") + " mm", string.Format(IT, "inclinato di {0:0.0} gradi sull'asse", r.PuntoneAngolo_deg) },
                new[] { "Rigidezza laterale del treppiede", D1(r.RigidezzaLaterale_N_mm, "0") + " N/mm", "puntoni incernierati, solo sforzo assiale" },
                new[] { "Abbassamento del feed a elevazione 0", D1(r.FlessioneFeed_mm, "0.000") + " mm", "minimo teorico: i giunti e la flessione del bordo danno di piu'" },
                new[] { "Spostamento del fascio dovuto", D1(r.StrabismoFlessione_deg, "0.0000") + " gradi", string.Format(IT, "BDF {0:0.000}", r.BDF) },
            }), "Puntoni del feed"));
            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "Pressione dinamica", D1(r.PressioneDinamica_Pa, "0") + " Pa", string.Format(IT, "{0:0} km/h, aria {1:0.000} kg/m3", inp.Vento_kmh, inp.DensitaAria) },
                new[] { "Solidita' della rete", inp.SuperficiePiena ? "100 %" : Pc(r.ReteSolidita), "frazione di area occupata dai fili" },
                new[] { "Coefficiente di perdita K (Idelchik)", inp.SuperficiePiena ? "-" : D1(r.ReteK, "0.000"), inp.SuperficiePiena ? "" : string.Format(IT, "Re del filo {0:0}", r.ReteRe) },
                new[] { "Cd della rete piana / a disco", D1(r.CdRetePiana, "0.000") + " / " + D1(r.CdReteDisco, "0.000"), "disco attuatore (Taylor), poi concavita' come il disco pieno (1,42 / 1,17)" },
                new[] { "Spinta: rete + centine, anelli, puntoni, feed", D1(r.ForzaVento_N, "0") + " N", string.Format(IT, "{0:0} kgf; Cd equivalente sull'apertura {1:0.00}", r.ForzaVento_N / Parabola.G0, r.CdEquivalente) },
                new[] { "Spinta con rete chiusa (ghiaccio, neve)", D1(r.ForzaVentoGhiaccio_N, "0") + " N", string.Format(IT, "{0:0} kgf; Cd 1,42", r.ForzaVentoGhiaccio_N / Parabola.G0) },
            }), "Vento (disco frontale; struttura senza schermatura reciproca = a favore di sicurezza)"));
            return sp;
        }

        Control PaginaCentina()
        {
            var sp = new StackPanel();
            var r = res;
            var righe = new List<string[]> { new[] { "#r (mm)", "z (mm)", "arco (mm)", "pendenza (gradi)" } };
            foreach (var p in r.Profilo)
                righe.Add(new[] { D1(p.R_m * 1000, "0.0"), D1(p.Z_m * 1000, "0.0"), D1(p.Arco_m * 1000, "0.0"), D1(p.Pendenza_deg, "0.00") });
            var bt = new Button { Content = "Esporta la centina (CSV)" };
            bt.Click += async (s, e) => await Salva("centina_parabola.csv", Parabola.ProfiloCsv(res));
            var bc = new Button { Content = "Copia la tabella" };
            bc.Click += async (s, e) =>
            {
                var top = TopLevel.GetTopLevel(this);
                if (top != null && top.Clipboard != null) await top.Clipboard.SetTextAsync(Parabola.ProfiloCsv(res, '\t'));
            };
            var riga = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 6) };
            riga.Children.Add(bt); riga.Children.Add(bc);
            var interno = new StackPanel();
            interno.Children.Add(riga);
            interno.Children.Add(Nota("z(r) = r^2 / 4F, misurata dal vertice verso il fuoco; l'arco serve per tagliare e forare la centina lungo la curva."));
            interno.Children.Add(Tabella(righe, "Auto,Auto,Auto,Auto"));
            sp.Children.Add(Carta(interno, "Profilo della centina"));

            var ra = new List<string[]> { new[] { "#Anello", "raggio (mm)", "quota z (mm)", "circonferenza (mm)" } };
            for (int i = 0; i < r.AnelliRaggio_m.Length; i++)
                ra.Add(new[] { (i + 1).ToString(), D1(r.AnelliRaggio_m[i] * 1000, "0"), D1(r.AnelliZ_m[i] * 1000, "0.0"), D1(r.AnelliCirc_m[i] * 1000, "0") });
            sp.Children.Add(Carta(Tabella(ra, "Auto,Auto,Auto,Auto"), "Anelli"));

            // 0.4.20: arcarecci diritti (IC8TEM 14/11/2026 18:20)
            if (r.Arcarecci != null && r.Arcarecci.Count > 0)
            {
                var mat = Parabola.MaterialiArcarecci[Math.Max(0, Math.Min(Parabola.MaterialiArcarecci.Length - 1, inp.ArcarecciMateriale))];
                var aa = new List<string[]> { new[] { "#Anello", "raggio (mm)", "pezzi x campata (mm)", "freccia corda (mm)", "tubo", "sigma / ammessa (MPa)", "freccia vento / max (mm)", "freccia peso / max (mm)", "massa (kg)", "esito" } };
                double tot = 0;
                foreach (var x in r.Arcarecci)
                {
                    tot += x.Massa_kg;
                    aa.Add(new[] { x.Anello.ToString(), D1(x.Raggio_m * 1000, "0"), x.Pezzi + " x " + D1(x.Campata_m * 1000, "0.0"), D1(x.FrecciaCorda_mm, "0.0"), x.Sezione.Nome,
                        D1(x.Sigma_MPa, "0") + " / " + D1(x.SigmaAmm_MPa, "0"), D1(x.FrecciaVento_mm, "0.00") + " / " + D1(x.FrecciaVentoMax_mm, "0.0"),
                        D1(x.FrecciaPermanente_mm, "0.00") + " / " + D1(x.FrecciaPermMax_mm, "0.0"), D1(x.Massa_kg, "0.00"), x.Ok ? (x.OkGhiaccio ? "OK" : "OK (ghiaccio: oltre lo snervamento)") : "NON BASTA" });
                }
                var inA = new StackPanel();
                var bA = new Button { Content = "Esporta l'elenco di taglio (CSV)" };
                bA.Click += async (s, e) => await Salva("arcarecci_parabola.csv", Parabola.ArcarecciCsv(res, inp));
                inA.Children.Add(bA);
                inA.Children.Add(Nota(string.Format(IT, "{0}; vento {1:0} km/h ({2:0} Pa), Cd della rete a disco {3:0.000}. Trave appoggiata sulle due centine, tutta la rete a carico degli arcarecci (a favore di sicurezza). " +
                    "Freccia della corda: di quanto la rete sul tratto diritto resta sotto il paraboloide a meta' campata (entra nella sfaccettatura). Massa totale degli arcarecci {4:0.00} kg{5}.",
                    mat.Nome, inp.Vento_kmh, r.PressioneDinamica_Pa, r.CdReteDisco, tot, inp.ArcarecciCalcolati ? ", usata nel peso e nel contrappeso" : " (non usata: casella spenta)")));
                inA.Children.Add(Tabella(aa, "Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,*"));
                sp.Children.Add(Carta(inA, "Arcarecci diritti tra le centine"));
            }

            // 0.4.20: dima della centina (IC8TEM 14/11/2026 18:20, "vorrei calcolare anche le dime delle costole")
            {
                var inD = new StackPanel { Spacing = 4 };
                var rigaD = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                var bPdf = new Button { Content = "Dima della centina in PDF (scala 1:1, fogli A4 o A3)" };
                var bDxf = new Button { Content = "Dima della centina in DXF (taglio laser, CNC)" };
                bPdf.Click += async (s, e) => await SalvaDati("dima_centina.pdf", "pdf", st => DimaCentinaPdf.Scrivi(st, inp, res));
                bDxf.Click += async (s, e) => await SalvaDati("dima_centina.dxf", "dxf", st =>
                {
                    // DXF in ASCII senza BOM (alcuni programmi CAD non leggono il BOM)
                    byte[] b = Encoding.ASCII.GetBytes(Parabola.DimaDxf(inp, res, inp.DimaMozzo_mm / 1000.0, inp.DimaAltezza_mm / 1000.0));
                    st.Write(b, 0, b.Length);
                });
                rigaD.Children.Add(bPdf); rigaD.Children.Add(bDxf);
                inD.Children.Add(rigaD);
                inD.Children.Add(Nota(string.Format(IT, "Faccia della centina (dove appoggia la rete) z = r^2 / 4F dal mozzo (r {0:0} mm) al bordo, dorso a {1:0} mm lungo la normale, " +
                    "tacche degli anelli (arcarecci), dell'attacco dei puntoni e ogni 100 mm di raggio. Il PDF va stampato al 100 % (senza \"adatta alla pagina\"): " +
                    "su ogni foglio c'e' un righello di 100 mm per controllare la scala, e i fogli si sovrappongono di 10 mm sulle crocette. Mozzo e altezza della centina si scrivono nei dati a sinistra.",
                    inp.DimaMozzo_mm, inp.DimaAltezza_mm)));
                sp.Children.Add(Carta(inD, "Dima della centina"));
            }
            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "Freccia della rete piana tra due centine, al bordo", D1(r.SfaccettaturaBordo_mm, "0.0") + " mm", string.Format(IT, "{0} centine", inp.NCentine) },
                new[] { "Errore RMS equivalente (pesato con l'illuminazione)", D1(r.SigmaFacet_mm, "0.0") + " mm", (inp.IncludiSfaccettatura ? "incluso nel calcolo" : "non incluso (rete sagomata)") + string.Format(IT, "; componente assiale (Baars 4.43), normale alla superficie {0:0.0} mm", r.SigmaFacetNormale_mm) },
                new[] { "Bordo a poligono tra le punte delle centine (0.4.20)", D1(10 * Math.Log10(r.EtaBordoCentine), "0.00") + " dB", inp.IncludiSfaccettatura ? string.Format(IT, "area del poligono di {0} lati / cerchio; verifica NEC: 1,2 m -0,13 dB, 3 m -0,34 dB in tutto", inp.NCentine) : "non incluso (rete sagomata)" },
                new[] { "Obiettivo lambda/20 / lambda/40", D1(r.Lambda_mm / 20, "0.0") + " / " + D1(r.Lambda_mm / 40, "0.0") + " mm", "" },
            }), "Sfaccettatura (stima geometrica)"));
            return sp;
        }

        Control PaginaBande()
        {
            var sp = new StackPanel();
            var righe = new List<string[]> { new[] { "#Banda", "G (dBi)", "eff.", "HPBW", "rete", "Ruze", "foro/lambda", "eco Luna", "punt. 0,5 dB", "giudizio" } };
            foreach (var b in res.Bande)
                righe.Add(new[] { b.Nome, D1(b.Guadagno_dBi, "0.0"), D1(b.Efficienza * 100, "0") + "%", D1(b.Hpbw_deg, "0.00") + " g", D1(b.PerditaRete_dB, "0.00"), D1(b.PerditaRuze_dB, "0.00"), D1(b.ForoSuLambda, "0.00"), "-" + D1(b.PerditaEco_dB, "0.0") + " dB", D1(b.Tolleranza05_deg, "0.00") + " g", b.Giudizio });
            sp.Children.Add(Carta(Tabella(righe, "Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,*"), "Lo stesso disco su tutte le bande (stesso taper, stesso blocco, stesso errore di superficie)"));

            var r = res;
            sp.Children.Add(Carta(Tabella(new[]
            {
                new[] { "Diametro della Luna", D1(Parabola.LunaDiametro_deg, "0.00") + " gradi", "da 0,49 a 0,56 secondo la distanza" },
                new[] { "Fascio / Luna", D1(r.Hpbw_deg / Parabola.LunaDiametro_deg, "0.00"), "sotto 1,5 la Luna non e' piu' una sorgente puntiforme" },
                new[] { "Frazione del fascio sulla Luna (fill)", Pc(r.FillLuna), "1 - 2^-(Dluna/HPBW)^2 (DF3GJ, UA3AVR)" },
                new[] { "Perdita sull'eco per fascio stretto", D1(r.PerditaEco_dB, "0.00") + " dB", "Luna a cappello e fasci gaussiani; da sommare al calcolo a sorgente puntiforme" },
                new[] { "Perdita col puntamento impostato", D1(r.PerditaPuntamento_dB, "0.00") + " dB", string.Format(IT, "12 (e/HPBW)^2, e = {0:0.000} gradi (Bob Atkins)", inp.ErrorePuntamento_deg) },
                new[] { "Errore ammesso per 0,5 / 1 dB", D1(r.Tolleranza05_deg, "0.000") + " / " + D1(r.Tolleranza1_deg, "0.000") + " gradi", "" },
            }), "EME alla frequenza scelta"));

            double[] dd, ee; double dOtt;
            Parabola.CurvaEco(inp, res, out dd, out ee, out dOtt);
            var gr = new ParabolaGrafico { Height = 300, Titolo = string.Format(IT, "Eco relativo in funzione del diametro a {0:0.0} MHz, errore di puntamento {1:0.00} gradi", inp.FreqMHz, inp.ErrorePuntamento_deg), TitoloX = "diametro (m)", TitoloY = "dB" };
            gr.XMin = dd[0]; gr.XMax = dd[dd.Length - 1]; gr.YMax = 0; gr.YMin = -30; gr.PassoY = 5; gr.FormatoX = "0.0";
            gr.Serie_.Add(new ParabolaGrafico.Serie { X = dd, Y = ee, Nome = inp.HpbwCorrispondente_deg > 0 ? "segnale al corrispondente (un solo guadagno)" : "eco proprio (stesso disco TX e RX)" });
            gr.LineeX.Add((inp.Diametro_m, "il tuo disco", Pal.ChartText));
            if (!double.IsNaN(dOtt)) gr.LineeX.Add((dOtt, string.Format(IT, "massimo {0:0.0} m", dOtt), Pal.Red));
            sp.Children.Add(Carta(gr));
            sp.Children.Add(Carta(Nota(double.IsNaN(dOtt)
                ? "Con questo errore di puntamento l'eco cresce ancora col diametro in tutto l'intervallo: la larghezza non e' eccessiva, a patto di mantenere la superficie entro lambda/20."
                : string.Format(IT, "Con un errore di puntamento di {0:0.00} gradi, oltre {1:0.0} m il guadagno in piu' viene mangiato dal puntamento e dal fascio piu' stretto della Luna: per andare oltre serve un puntamento migliore (rumore lunare, encoder piu' fini).", inp.ErrorePuntamento_deg, dOtt))));
            return sp;
        }

        Control PaginaFonti()
        {
            return Carta(Tabella(new[]
            {
                new[] { "#Argomento", "Fonte" },
                new[] { "Feed cos^q, spillover, efficienza", "W.L. Stutzman, G.A. Thiele, Antenna Theory and Design, eq. 9-233; S. Sengupta, Virginia Tech 2021 (A Fast Approximate Method)" },
                new[] { "Tabelle HPBW / lobi / taper (collaudo)", "GMRT/NCRA, Low Frequency Radio Astronomy, tab. 19.1; Baars, NRAO EDIR 57" },
                new[] { "Larghezza del fascio (HPBW)", "C.A. Balanis, Antenna Theory, tab. 12.2 (apertura circolare uniforme 29,2 gradi/(a/lambda)); GMRT tab. 19.1 e Stutzman/Sengupta (con taper); W1GHZ app. 6A (guadagno dal fascio 41253/HPBW^2, ottimista); DF3GJ (58 lambda/D); RF HamDesign (tabelle dei kit da 1,9 e 3 m)" },
                new[] { "Feed reali, taper ottimo ~10 dB, efficienza reale -15%", "P. Wade W1GHZ, Online Microwave Antenna Book, cap. 4 e 6 (FEEDPATT, PHASEPAT)" },
                new[] { "Blocco pesato con l'illuminazione, eta = (1-B)^2", "Galt Telescope, arXiv 1607.06436; Lamb & Olver 1986 (arXiv 1609.09376)" },
                new[] { "Puntoni (ombre)", "J. Ruze, Feed Support Blockage Loss, Microwave J. 1968; Borkowski & Maciejewski 1987" },
                new[] { "Errori di superficie", "J. Ruze, Antenna Tolerance Theory, Proc. IEEE 1966; Bob Atkins, Dish Evaluation; OE2IGL" },
                new[] { "Fuori fuoco assiale", "Ruze 1969; NRAO ALMA memo 479" },
                new[] { "Fase del feed, centro di fase, asimmetria E/H", "W1GHZ, Online Microwave Antenna Book, cap. 6.1 e 12 (PHASEPAT, DIPROD.NEC); P.-S. Kildal, Factorization of the feed efficiency of paraboloids and Cassegrain antennas, IEEE Trans. AP-33, 1985; DF3GJ (formato dei file)" },
                new[] { "Spostamento laterale (BDF)", "Y.T. Lo 1960; Ruze 1965" },
                new[] { "Rete: trasmissione", "T.Y. Otoshi, Noise Temperature Theory and Applications for Deep Space Communications Antenna Systems (Artech, 2008), pp. 271-276; W1GHZ, Mesh Reflector Loss Calculations (2014)" },
                new[] { "Rete: vento", "I.E. Idelchik, Handbook of Hydraulic Resistance, diagr. 8-6; teoria del disco attuatore (Taylor); Hoerner, Fluid-Dynamic Drag" },
                new[] { "Disco pieno al vento", "Hoerner; rfessentials (Cd 1,2-1,4); dati di costruttori 1,5-1,65" },
                new[] { "EME: puntamento, fascio e Luna", "Bob Atkins (off-axis gain loss); OE2IGL/DF3GJ EME link budget; DF3GJ Filling Factors; UA3AVR DUBUS 3/2016; DJ7FJ DUBUS 1/1995" },
                new[] { "Costruzione: dischi a centine", "OK1DFC (10 m primo fuoco, rete 12,5 mm; 8 m offset, rete 6 x 0,8 mm, 36 centine, vento di progetto 130 km/h); RF HamDesign (3 m: 12 centine 20 mm, 4 anelli, rete 6 mm fino a 6 GHz, mozzo CNC 10 mm)" },
                new[] { "Controlli della 1.0.14 (Baars)", "J.W.M. Baars, The Paraboloidal Reflector Antenna in Radio Astronomy and Communication, Springer 2007, cap. 4: free-space taper 4.3, quadratica 4.8, gaussiana 4.6, HPBW 4.13, primo lobo 4.14a, fuori fuoco 4.21-4.23, ombra sferica dei puntoni 4.36c-f, componente assiale dell'errore 4.43, fascio d'errore 4.44-4.46. Uguale al calcolo: free-space taper (= attenuazione spaziale) ed efficienze delle distribuzioni. Diverso: Baars usa distribuzioni ideali, qui si integra il feed vero; HPBW e lobo di Baars sono di confronto, non sostituiscono il calcolo" },
                new[] { "Controlli della 1.0.14 (Balanis)", "C.A. Balanis, Antenna Theory, 4a ed., cap. 15: efficienza dei feed cos^n (15-55, collaudo con le curve), limite inferiore dell'efficienza di fase (1 - m^2/2)^2, guadagno massimo a lambda = 4 pi sigma (15-67..71). Il limite di fase e' un minimo garantito: il valore calcolato sta sempre sopra" },
                new[] { "Feed a 10 GHz, differenze misurate", "PA0PLY (disco 3 m, f/D 0,3): guida aperta 13,5 dB contro choke 15,2 dB; choke avvitata -0,6 dB; cappuccio dell'LNB -0,5 dB. Solo riferimento: il diagramma di quei feed va calcolato col NEC" },
                new[] { "Costruzione: stressed dish", "Bob Atkins (dischi a centine tensionate, perdita tipica 1-2 dB; K2UYH)" },
            }, "Auto,*"), "Fonti (da riportare nei crediti di YA-NEC)");
        }

        // ============================================================================================
        async System.Threading.Tasks.Task SalvaDati(string nome, string est, Action<Stream> scrivi)
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top == null) return;
                var f = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Salva", SuggestedFileName = nome, DefaultExtension = est });
                if (f == null) return;
                using (var st = await f.OpenWriteAsync()) scrivi(st);
            }
            catch (Exception ex)
            {
                pannelloAvvisi.Children.Insert(0, Avviso(2, "Salvataggio non riuscito: " + ex.Message));
            }
        }

        async System.Threading.Tasks.Task Salva(string nome, string testo, string est = "csv")
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top == null) return;
                var f = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Salva", SuggestedFileName = nome, DefaultExtension = est });
                if (f == null) return;
                using (var st = await f.OpenWriteAsync())
                using (var w = new StreamWriter(st, new UTF8Encoding(true)))
                    w.Write(testo);
            }
            catch (Exception ex)
            {
                pannelloAvvisi.Children.Insert(0, Avviso(2, "Salvataggio non riuscito: " + ex.Message));
            }
        }
    }
}
