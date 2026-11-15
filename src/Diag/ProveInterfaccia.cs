// YA-NEC - prove automatiche dell'interfaccia (avvio con --prove-interfaccia, lanciato da compila.bat nella fase 6).
// Il programma si apre normalmente e fa da solo le prove che prima si facevano a mano: apre i modelli, preme i
// pulsanti delle variabili, scrive nella tabella dei fili, cambia linguetta, schema, banda e terreno, salva e riapre.
// Per ogni passo scrive atteso / ottenuto / esito in data/prove_interfaccia.txt e salva un'immagine della finestra
// in data/collaudo_immagini (le immagini mostrano com'era la finestra in quel momento). Alla fine si chiude da solo.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Avalonia.Threading;
using YaNec.Engine;
using Complex = System.Numerics.Complex;

namespace YaNec
{
    public partial class MainWindow
    {
        public static bool ProveAutomatiche;
        public static bool ProveSoloNovita;
        public static bool ProveSoloButternut;   // 0.4.17: --prove-butternut, fase 2D dopo che tutto il resto e' superato
        const int SegmentiProvaAParte = 3000;     // 0.4.17: oltre 3000 segmenti non si aprono qui; dalla 0.4.18 i 3 piu' pesanti si provano nella fase 2E (ModelliPesanti)   // 0.4.13: --solo-novita, solo le pagine e le funzioni cambiate dopo la 0.4.10
        public static int ProveFallite;
        // 0.4.14 (collaudo 0.4.10: finestra chiusa a mano durante la Butternut, esito "superate" con prove_interfaccia.txt mancante):
        // se le prove non arrivano in fondo il programma esce con il codice 10000 e compila.bat scrive INTERROTTE
        public static bool ProveFinite;

        StringBuilder prRep;
        string prFile = "prove_interfaccia.txt";   // 0.4.17: prove_butternut.txt nella fase 2D
        int prEccezioni, prFumetti, prFumettiVuoti, prPulsanti, prCaselle;
        string prUltimaAzione = "";
        readonly HashSet<Control> prProvati = new HashSet<Control>();
        StringBuilder prElencoFumetti = new StringBuilder();
        int prN, prOk, prErr, prImg;
        string prDirImg;

        void AvviaProveInterfaccia()
        {
            // 0.4.10: un'eccezione in un gestore (anche asincrono) premendo un pulsante non chiude il programma: diventa un ERRORE del collaudo
            Dispatcher.UIThread.UnhandledException += (s, e) =>
            {
                if (!ProveAutomatiche) return;
                prEccezioni++;
                PrPasso("eccezione nell'interfaccia" + (prUltimaAzione.Length > 0 ? " dopo: " + prUltimaAzione : ""), "nessuna eccezione", e.Exception.GetType().Name + ": " + e.Exception.Message, false);
                e.Handled = true;
            };
            Dispatcher.UIThread.Post(async () =>
            {
                try { if (ProveSoloButternut) await ProveButternut(); else if (ProveSoloNovita) await ProveNovita(); else await ProveInterfaccia(); }
                catch (Exception ex) { PrPasso("le prove si sono interrotte", "nessuna eccezione", ex.ToString(), false); }
                PrFine();
                ProveFallite = prErr;
                ProveFinite = true;
                Close();
            });
        }

        // ------------------------------------------------------------------------------------------------
        void PrRiga(string s) { prRep.AppendLine(s); }

        void PrPasso(string cosa, string atteso, string ottenuto, bool ok)
        {
            prN++;
            if (ok) prOk++; else prErr++;
            PrRiga(string.Format(CI, "  {0,3}. {1}", prN, cosa));
            if (!string.IsNullOrEmpty(atteso)) PrRiga("         atteso:   " + atteso);
            PrRiga("         ottenuto: " + ottenuto);
            PrRiga("         esito:    " + (ok ? "OK" : "ERRORE"));
            Registro.Scrivi("PROVA", prN + ". " + cosa + " -> " + (ok ? "OK" : "ERRORE: " + ottenuto));
            // 0.4.11 (collaudo 0.4.9 interrotto al modello 391: prove_interfaccia.txt non creato): il resoconto si salva ogni 20 passi,
            // cosi' resta anche se le prove si interrompono
            // 0.4.16: in modo "solo novita'" i passi sono pochi: si salva a ogni passo
            if (prN % 20 == 0 || ProveSoloNovita)
            {
                try { File.WriteAllText(Path.Combine(AppPaths.DataDir, prFile), prRep.ToString() + Environment.NewLine + "(resoconto parziale: prove ancora in corso o interrotte)" + Environment.NewLine, new UTF8Encoding(false)); } catch { }
            }
        }

        void PrTitolo(string t) { PrRiga(""); PrRiga(t); }

        void PrImmagine(string nome)
        {
            try
            {
                prImg++;
                PixelSize ps = new PixelSize(Math.Max(1, (int)Bounds.Width), Math.Max(1, (int)Bounds.Height));
                using (RenderTargetBitmap bmp = new RenderTargetBitmap(ps, new Avalonia.Vector(96, 96)))
                {
                    bmp.Render(this);
                    string f = Path.Combine(prDirImg, string.Format(CI, "{0:00}_{1}.png", prImg, nome));
                    bmp.Save(f);
                    PrRiga("         immagine: collaudo_immagini\\" + Path.GetFileName(f));
                }
            }
            catch (Exception ex) { PrRiga("         immagine non salvata: " + ex.Message); }
        }

        static async Task<bool> Attendi(Func<bool> cond, int ms = 20000)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (!cond())
            {
                if (sw.ElapsedMilliseconds > ms) return false;
                await Task.Delay(50);
            }
            await Task.Delay(200);   // lascia ridisegnare la finestra
            return true;
        }

        async Task<bool> AttendiCalcolo(int calcoliPrima, int sweepPrima)
        {
            bool a = await Attendi(() => nCalcoli > calcoliPrima);
            bool b = await Attendi(() => nSweep > sweepPrima);
            return a && b;
        }

        static string Zs(Complex z) { return string.Format(CI, "{0:0.000} {1:+0.000;-0.000}j ohm", z.Real, z.Imaginary); }

        static bool Vicino(Complex z, double r, double x, double tol = 0.01)
        {
            return Math.Abs(z.Real - r) <= tol && Math.Abs(z.Imaginary - x) <= tol;
        }

        int IndiceLinguetta(string nome)
        {
            for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == nome) return i;
            return -1;
        }

        VarRow RigaVar(string nome) { return varRows.Find(r => string.Equals(r.Name.Text, nome, StringComparison.OrdinalIgnoreCase)); }

        async Task<bool> Apri(string file)
        {
            int c = nCalcoli, s = nSweep;
            LoadModelFile(Path.Combine(AppPaths.ModelsDir, file));
            return await AttendiCalcolo(c, s);
        }

        async Task<bool> Applica(Action a)
        {
            int c = nCalcoli, s = nSweep;
            a();
            return await AttendiCalcolo(c, s);
        }

        // ------------------------------------------------------------------------------------------------
        // 0.4.13 (richiesta di IC8TEM del 14/11/2026 08:25: "per il collaudo della prossima 0.4.13 inseriamo solo le modifiche che hai
        // effettuato dopo la 0.4.10"): feed dal modello NEC con la fase, pagine Parabola primo fuoco (con la pagina Feed) e Parabola
        // offset con fumetti, pulsanti e caselle, manuale un capitolo alla volta, LPDA di AC6LA con IF(), modello oltre 3000 segmenti.
        // ------------------------------------------------------------------------------------------------
        // 0.4.17 (richiesta di IC8TEM del 14/11/2026 09:12: "il collaudo della butternut lo fai solo dopo che tutti i collaudi sono passati.
        // riapri ya-nec e carichi la butternut"): YA-NEC si riapre e carica la Butternut con 64 radiali (7877 segmenti): calcolo, sweep di un
        // punto, tempi e memoria. Resoconto in data/prove_butternut.txt.
        async Task ProveButternut()
        {
            prRep = new StringBuilder();
            prDirImg = Path.Combine(AppPaths.DataDir, "collaudo_immagini");
            try { Directory.CreateDirectory(prDirImg); } catch { }
            prFile = "prove_butternut.txt";
            PrRiga("PROVA DELLA BUTTERNUT CON 64 RADIALI, MODELLO CORRETTO NELLA 0.4.18 (fase 2D) - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            PrRiga("Si esegue solo dopo che compilazione, autotest, collaudo e prove dell'interfaccia sono superati. Dura alcuni minuti: non toccare mouse e tastiera.");
            PrRiga(new string('=', 100));
            await Task.Delay(800);
            long memPrima = GC.GetTotalMemory(false);
            Stopwatch sw = Stopwatch.StartNew();
            await ProvaTuttiIModelli(new List<string> { "Modelli IC8TEM (convertiti in NEC)/80_40_Butternut_64Radials.nec" });
            long memDopo = Environment.WorkingSet;
            // 0.4.18 (scelta di IC8TEM del 14/11/2026 09:20, "correggi il modello"): Sommerfeld, radiali a 10 cm, mozzo di 8 raggi,
            // 1453 segmenti invece di 7877; dalla 0.4.20 1431, con i segmenti di almeno lambda/1000 come vuole EZNEC. Impedenza confrontata con nec2c sullo stesso mazzo.
            if (run != null && run.Engine != null)
            {
                System.Numerics.Complex zAtt = new System.Numerics.Complex(17.565, 25.176);
                double rel = System.Numerics.Complex.Abs(run.Zin - zAtt) / System.Numerics.Complex.Abs(zAtt);
                PrPasso("Butternut corretta: impedenza come nec2c", "17,565 +j25,176 ohm a 7,15 MHz (nec2c, Sommerfeld) entro l'1%, parte reale positiva",
                    string.Format(CI, "{0:0.000} {1} j{2:0.000} ohm (scarto {3:0.000}%), {4} segmenti, Gmax {5:0.00} dBi", run.Zin.Real, run.Zin.Imaginary < 0 ? "-" : "+",
                        Math.Abs(run.Zin.Imaginary), 100 * rel, run.Engine.SegmentCount, run.GmaxDbi), rel < 0.01 && run.Zin.Real > 0);
            }
            else PrPasso("Butternut corretta: impedenza come nec2c", "modello calcolato", "nessun calcolo", false);
            PrPasso("Butternut con 64 radiali: tempo e memoria", "calcolata, memoria del programma sotto un quarto della RAM del computer",
                string.Format(CI, "{0:0} s, memoria del programma {1:0} MB, RAM del computer {2:0} MB", sw.Elapsed.TotalSeconds, memDopo / 1048576.0, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1048576.0),
                memDopo < GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 4 + 2L * 1024 * 1024 * 1024);
            PrImmagine("2D_butternut");
        }

        async Task ProveNovita()
        {
            prRep = new StringBuilder();
            prDirImg = Path.Combine(AppPaths.DataDir, "collaudo_immagini");
            try
            {
                Directory.CreateDirectory(prDirImg);
                foreach (string f in Directory.GetFiles(prDirImg, "*.png")) File.Delete(f);
            }
            catch { }
            PrRiga("PROVE AUTOMATICHE DELL'INTERFACCIA - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - SOLO LE NOVITA' DOPO LA 0.4.10");
            PrRiga("Non toccare mouse e tastiera finche' la finestra non si chiude (compila.bat /completo per le prove complete).");
            PrRiga(new string('=', 100));
            await Task.Delay(800);

            // N1. feed dal modello NEC: dipolo lambda/2 con riflettore a 0,24 lambda a 1296,1 MHz (come DIPROD.NEC di W1GHZ), fascio verso +X
            PrTitolo("N1. Feed dal modello NEC con la fase (dipolo + riflettore a 1296,1 MHz)");
            string feedFile = Path.Combine(AppPaths.DataDir, "prova_feed_dipolo_riflettore.nec");
            try
            {
                File.WriteAllText(feedFile, "CM feed di prova del collaudo 0.4.13: dipolo lambda/2 con riflettore a 0,24 lambda (come DIPROD.NEC di W1GHZ)\nCE\n" +
                    "GW 1 21 0 -0.0578 0 0 0.0578 0 0.001\nGW 2 21 -0.0555 -0.0578 0 -0.0555 0.0578 0 0.001\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 1296.1 0\nEN\n");
            }
            catch (Exception ex) { PrPasso("modello del feed scritto in data", "scritto", ex.Message, false); }
            Func<Task<bool>> apriFeed = async () => { int c0 = nCalcoli, s0 = nSweep; LoadModelFile(feedFile); return await AttendiCalcolo(c0, s0); };
            bool okF = await apriFeed();
            PrPasso("apertura e calcolo del feed di prova", "calcolato, massimo verso +X", okF && run != null ? string.Format(CI, "Z {0}, {1:0.00} dBi a theta {2:0} phi {3:0}", Zs(run.Zin), run.GmaxDbi, run.ThetaMax, run.PhiMax) : "non calcolato",
                okF && run != null && Math.Abs(run.ThetaMax - 90) < 3 && (run.PhiMax < 3 || run.PhiMax > 357));
            try
            {
                FeedNec fn = FeedDelModello();
                bool ok = fn != null && fn.DbE != null && fn.DbE.Length == 181 && fn.FaseE != null && fn.FaseH != null && fn.Db45 != null && fn.Fase45 != null;
                string det = fn == null ? "nessun feed" : string.Format(CI, "{0} punti; E: {1:0.0} dBi a 0, {2:0.0} a 60 gradi; H: {3:0.0} a 60 gradi; fase E a 0/60 gradi {4:0.0}/{5:0.0}; piano a 45 gradi {6}",
                    fn.DbE.Length, fn.DbE[0], fn.DbE[60], fn.DbH[60], fn.FaseE[0], fn.FaseE[60], fn.Db45 != null ? "presente" : "assente");
                bool fin = ok;
                if (ok) for (int k = 0; k < 181; k++) if (double.IsNaN(fn.FaseE[k]) || double.IsNaN(fn.FaseH[k]) || double.IsNaN(fn.DbE[k])) fin = false;
                PrPasso("tagli E, H e 45 gradi con la fase (FeedDelModello)", "181 punti per piano, fase presente e numerica, massimo all'asse", det, fin && fn.DbE[0] >= fn.DbE[60]);
            }
            catch (Exception ex) { PrPasso("tagli E, H e 45 gradi con la fase", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }

            // N2. pagine delle parabole: Feed dal modello NEC premuto con il feed aperto, poi fumetti, pulsanti e caselle
            int tc = IndiceLinguetta("Calcolatori");
            foreach (string[] voce in new[] { new[] { "parabola", "Parabola primo fuoco" }, new[] { "offset", "Parabola offset" } })
            {
                PrTitolo("N2. Calcolatori > " + voce[1]);
                await apriFeed();
                try
                {
                    SelectTab(tc);
                    await Task.Delay(400);
                    fasCalc.Select(voce[0]);
                    await Task.Delay(1500);
                    PrPasso(voce[1] + ": la pagina si apre", "senza errori", page != null ? "aperta" : "pagina vuota", page != null);
                    Button bNec = page.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => TestoDi(b.Content).Trim() == "Feed dal modello NEC");
                    if (bNec == null) PrPasso(voce[1] + ": pulsante \"Feed dal modello NEC\"", "presente", "non trovato", false);
                    else
                    {
                        prUltimaAzione = "Feed dal modello NEC (" + voce[1] + ")";
                        bNec.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await Task.Delay(1500);
                        string avvisi = string.Join(" | ", page.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t.StartsWith("CRITICO")).Take(3));
                        PrPasso(voce[1] + ": Feed dal modello NEC con il feed di prova aperto", "nessun avviso CRITICO", avvisi.Length == 0 ? "feed caricato" : avvisi, avvisi.Length == 0);
                        PrImmagine("N2_" + voce[0] + "_feed_nec");
                    }
                    // ogni pagina a pillola (per il primo fuoco anche la pagina Feed del bozzetto 13): immagine, fumetti, pulsanti e caselle
                    Control pann = (Control)page.GetVisualDescendants().OfType<ParabolaPanel>().FirstOrDefault() ?? page.GetVisualDescendants().OfType<ParabolaOffsetPanel>().FirstOrDefault();
                    List<string> chiavi = pann is ParabolaPanel pp1 ? pp1.ChiaviPagine().ToList() : pann is ParabolaOffsetPanel po1 ? po1.ChiaviPagine().ToList() : new List<string>();
                    PrPasso(voce[1] + ": pagine a pillola", voce[0] == "parabola" ? "7 pagine, con Feed" : "5 pagine", chiavi.Count + " pagine: " + string.Join(", ", chiavi),
                        voce[0] == "parabola" ? chiavi.Count == 7 && chiavi.Contains("feed") : chiavi.Count == 5);
                    foreach (string ch in chiavi)
                    {
                        try
                        {
                            if (pann is ParabolaPanel pp2) pp2.ApriPagina(ch); else if (pann is ParabolaOffsetPanel po2) po2.ApriPagina(ch);
                            await Task.Delay(1200);
                            PrImmagine("N2_" + voce[0] + "_" + ch);
                            await ProvaFumetti(page, voce[1] + " > " + ch);
                            await ProvaFunzioni(page, voce[1] + " > " + ch);
                        }
                        catch (Exception ex) { PrPasso(voce[1] + " > " + ch, "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
                    }
                    await ProvaPagina("Calcolatori > " + voce[1]);
                }
                catch (Exception ex) { PrPasso(voce[1], "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            }

            // N3. manuale un capitolo alla volta
            PrTitolo("N3. Manuale un capitolo alla volta");
            int tm = IndiceLinguetta("Manuale");
            Stopwatch swm = Stopwatch.StartNew();
            if (tm >= 0) SelectTab(tm);
            await Task.Delay(100);
            long msM = swm.ElapsedMilliseconds;
            await Task.Delay(700);
            PrPasso("linguetta Manuale", "indice con almeno 20 capitoli, un capitolo alla volta", manualChapters.Count + " capitoli, risposta in " + msM + " ms", manualChapters.Count >= 20);
            PrImmagine("N3_manuale");
            await ProvaFumetti(page, "Manuale");

            // N4. modelli toccati dalle novita'
            SelectTab(0);
            await Task.Delay(300);
            // 0.4.16 (collaudo 0.4.14): la Butternut da 7877 segmenti (4 minuti per fattorizzazione, piu' di 10 minuti in tutto) resta solo
            // nel collaudo /completo; qui la memoria dello sweep e' provata nella procedura 2B "Novita' dopo la 0.4.10"
            await ProvaTuttiIModelli(new List<string> { "AutoEZ (esempi AC6LA)/LPDA by Tau+Sigma.weq", "AutoEZ (esempi AC6LA)/LPDA by Elems+Boom.weq" });

            // N5. 0.4.18 (richiesta di IC8TEM del 14/11/2026 09:17): BOG calcolato con il modello approssimato (filo a lambda/1000, Sommerfeld)
            PrTitolo("N5. Antenne riceventi > BOG con il modello approssimato (0.4.18)");
            try
            {
                SelectTab(IndiceLinguetta("Antenne riceventi"));
                await Task.Delay(400);
                fasRx.Select("bog");
                await Task.Delay(800);
                Control pb;
                rxPages.TryGetValue("bog", out pb);
                Button bc = pb == null ? null : pb.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => TestoDi(b.Content).Trim().StartsWith("Crea il modello NEC", StringComparison.Ordinal));
                PrPasso("BOG: pulsante \"Crea il modello NEC e calcola l'RDF\"", "presente dalla 0.4.18", bc != null ? "presente" : "assente", bc != null);
                string avvApprox = pb == null ? "" : string.Join(" ", pb.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").Where(t => t.Contains("APPROSSIMAZIONE")).Take(1));
                PrPasso("BOG: avvertenza nella finestra", "testo con APPROSSIMAZIONE e lambda/1000", avvApprox.Length > 0 ? "presente" : "assente", avvApprox.Contains("lambda/1000"));
                if (bc != null)
                {
                    prUltimaAzione = "BOG: Crea il modello NEC e calcola l'RDF";
                    Stopwatch swb = Stopwatch.StartNew();
                    bc.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    bool fatto = await Attendi(() => pb.GetVisualDescendants().OfType<TextBlock>().Any(t => (t.Text ?? "").StartsWith("Z0 del filo nel modello", StringComparison.Ordinal)), 120000);
                    // 0.4.19 (collaudo 0.4.18): la nota si cerca a parte; prima era tagliata via dai primi 6 testi in dB e il passo dava un falso ERRORE
                    await Task.Delay(300);
                    List<string> testi = pb.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
                    string rdf = string.Join(" | ", testi.Where(t => t.EndsWith(" dB", StringComparison.Ordinal)).Take(4));
                    string z0bog = testi.SkipWhile(t => !t.StartsWith("Z0 del filo nel modello", StringComparison.Ordinal)).Skip(1).FirstOrDefault() ?? "-";
                    bool nota = testi.Any(t => t.Contains("Modello approssimato"));
                    PrPasso("BOG: calcolo con Sommerfeld a lambda/1000", "riquadri RDF e Z0 del modello, nota \"Modello approssimato\"",
                        (fatto ? "calcolato in " + swb.ElapsedMilliseconds + " ms: " : "non finito in 120 s: ") + rdf + " | Z0 del modello " + z0bog + " | nota " + (nota ? "presente" : "assente"), fatto && nota);
                    PrImmagine("N5_bog");
                }
                await ProvaFumetti(pb ?? page, "Antenne riceventi > BOG");
            }
            catch (Exception ex) { PrPasso("BOG con il modello approssimato", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }

            // N6. 0.4.19: Calcolatori > Trombe e illuminatori (scheda di IC8TEM del 14/11/2026): coffee-can a 1296 MHz, modello NEC aperto
            // in YA-NEC e calcolato, poi i tagli passano alla parabola con "Feed dal modello NEC" (bocca a z = 0, fascio verso +Z)
            PrTitolo("N6. Calcolatori > Trombe e illuminatori (0.4.19): coffee-can, modello NEC, feed alla parabola");
            try
            {
                SelectTab(IndiceLinguetta("Calcolatori"));
                await Task.Delay(400);
                fasCalc.Select("trombe");
                await Task.Delay(1500);
                TrombaPanel tp = page.GetVisualDescendants().OfType<TrombaPanel>().FirstOrDefault();
                PrPasso("Trombe e illuminatori: la pagina si apre", "pannello presente", tp != null ? "aperta" : "assente", tp != null);
                if (tp != null)
                {
                    ComboBox cbT = tp.GetVisualDescendants().OfType<ComboBox>().FirstOrDefault(c => c.ItemsSource is string[] a && a.Length == 7 && a[0].StartsWith("Coffee-can", StringComparison.Ordinal));
                    if (cbT != null) { cbT.SelectedIndex = 0; await Task.Delay(1500); }
                    PrPasso("tipo coffee-can", "scelto dalla casella dei tipi", cbT != null ? "scelto, " + tp.SegmentiNec + " segmenti nel modello NEC" : "casella non trovata", cbT != null && tp.SegmentiNec > 0);
                    List<string> chiavi = tp.ChiaviPagine().ToList();
                    PrPasso("pagine a pillola", "5 pagine: misure, diagramma, parabola, nec, fonti", chiavi.Count + ": " + string.Join(", ", chiavi), chiavi.Count == 5 && chiavi.Contains("nec"));
                    foreach (string ch in chiavi)
                    {
                        tp.ApriPagina(ch);
                        await Task.Delay(500);
                        PrImmagine("N6_trombe_" + ch);
                    }
                    await ProvaFumetti(tp, "Calcolatori > Trombe e illuminatori");
                    tp.ApriPagina("nec");
                    await Task.Delay(500);
                    Button bA = tp.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => TestoDi(b.Content).Trim() == "Apri in YA-NEC");
                    if (bA == null || !bA.IsEnabled) PrPasso("pulsante \"Apri in YA-NEC\"", "presente e attivo", bA == null ? "non trovato" : "disattivato", false);
                    else
                    {
                        int c0t = nCalcoli;
                        int segAttesi = tp.SegmentiNec;
                        prUltimaAzione = "Trombe: Apri in YA-NEC (coffee-can)";
                        Stopwatch swt = Stopwatch.StartNew();
                        bA.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        bool fatto = await Attendi(() => nCalcoli > c0t && run != null, 300000);
                        PrPasso("modello NEC della tromba aperto e calcolato", segAttesi + " segmenti, calcolo senza errori",
                            fatto ? string.Format(CI, "{0} segmenti, Z {1}, Gmax {2:0.00} dBi a theta {3:0} gradi, {4:0.0} s", run.Engine.SegmentCount, Zs(run.Zin), run.GmaxDbi, run.ThetaMax, swt.Elapsed.TotalSeconds) : "non calcolato: " + ultimoStato,
                            fatto && run.Engine.SegmentCount == segAttesi);
                        if (fatto)
                        {
                            PrImmagine("N6_trombe_modello_3D");
                            FeedNec fn = FeedDelModello();
                            bool okTagli = fn != null && fn.DbE != null && fn.DbE.Length == 181 && fn.DbE[0] >= fn.DbE[60] && fn.DbH[0] >= fn.DbH[60];
                            PrPasso("tagli della tromba per la parabola (Feed dal modello NEC)", "massimo sull'asse della bocca (verso +Z), piani E e H da 181 punti",
                                fn == null ? "nessun taglio" : string.Format(CI, "E: 0 gradi {0:0.0} dB, 60 gradi {1:0.0} dB; H: 0 gradi {2:0.0} dB, 60 gradi {3:0.0} dB; massimo a theta {4:0}",
                                    fn.DbE[0], fn.DbE[60], fn.DbH[0], fn.DbH[60], run.ThetaMax), okTagli && run.ThetaMax <= 10);
                        }
                    }
                }
            }
            catch (Exception ex) { PrPasso("Trombe e illuminatori", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }

            PrPasso("riepilogo dei fumetti", "ognuno con il suo testo", string.Format(CI, "{0} fumetti mostrati, {1} vuoti", prFumetti, prFumettiVuoti), prFumettiVuoti == 0);
            PrPasso("riepilogo delle funzioni", "nessuna eccezione", string.Format(CI, "{0} pulsanti premuti, {1} caselle cambiate e rimesse, {2} eccezioni", prPulsanti, prCaselle, prEccezioni), prEccezioni == 0);
            try { File.WriteAllText(Path.Combine(AppPaths.DataDir, "fumetti.txt"), prElencoFumetti.ToString(), new UTF8Encoding(false)); } catch { }
            try { File.Delete(feedFile); } catch { }
        }

        async Task ProveInterfaccia()
        {
            prRep = new StringBuilder();
            prDirImg = Path.Combine(AppPaths.DataDir, "collaudo_immagini");
            try
            {
                Directory.CreateDirectory(prDirImg);
                foreach (string f in Directory.GetFiles(prDirImg, "*.png")) File.Delete(f);
            }
            catch { }
            PrRiga("PROVE AUTOMATICHE DELL'INTERFACCIA - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            PrRiga("Il programma esegue da solo le prove che prima si facevano a mano. Non toccare mouse e tastiera finche' la finestra non si chiude.");
            PrRiga(new string('=', 100));
            await Task.Delay(800);

            // 1 - modello con variabili
            PrTitolo("A. Apertura del modello con variabili");
            bool ok = await Apri("Dipolo 144 con variabili.nec");
            PrPasso("Esempi -> \"Dipolo 144 con variabili\", calcolo e sweep", "Z = 74,982 +12,480j ohm, 2,14 dBi",
                run == null ? "nessun calcolo" : Zs(run.Zin) + string.Format(CI, ", {0:0.00} dBi", run.GmaxDbi),
                ok && run != null && Vicino(run.Zin, 74.982, 12.480) && Math.Abs(run.GmaxDbi - 2.14) < 0.01);
            SelectTab(IndiceLinguetta("Struttura 3D"));
            await Task.Delay(300);
            PrImmagine("struttura_dipolo");

            // 2 - linguetta Variabili
            PrTitolo("B. Linguetta Variabili");
            SelectTab(IndiceLinguetta("Variabili"));
            await Task.Delay(300);
            string vals = "";
            foreach (VarRow r in varRows) vals += r.Name.Text + "=" + r.Value.Text + "  ";
            PrPasso("variabili presenti con il loro valore", "L=1 H=0,5 D=0,004 R=0,002 NS=21", vals.Trim(),
                varRows.Count == 5 && RigaVar("NS") != null && RigaVar("NS").Value.Text == "21");
            VarRow rh = RigaVar("H");
            string wm = rh != null ? (rh.Descr.Text.Length > 0 ? rh.Descr.Text : rh.Descr.Watermark) : "";
            PrPasso("colonna Descrizione di H", "meta' lunghezza, estremi del filo (m)  (scritta nel file di esempio)", wm ?? "", (wm ?? "").StartsWith("meta' lunghezza"));
            PrImmagine("variabili");

            // 3 - pulsanti + e - su L
            VarRow rl = RigaVar("L");
            Complex z0 = run.Zin;
            ok = await Applica(() => StepVar(rl, +1));
            PrPasso("+ su L (1000*MM, passo 0,01)", "espressione 1010*MM, H = 0,505, reattanza piu' alta",
                RigaVar("L").Expr.Text + ", H = " + RigaVar("H").Value.Text + ", Z = " + Zs(run.Zin),
                ok && RigaVar("L").Expr.Text == "1010*MM" && run.Zin.Imaginary > z0.Imaginary);
            ok = await Applica(() => StepVar(RigaVar("L"), -1));
            PrPasso("- su L", "di nuovo 1000*MM e Z = 74,982 +12,480j", RigaVar("L").Expr.Text + ", Z = " + Zs(run.Zin),
                ok && RigaVar("L").Expr.Text == "1000*MM" && Vicino(run.Zin, 74.982, 12.480));

            // 4 - + su H (formula)
            int cH = nCalcoli;
            StepVar(RigaVar("H"), +1);
            await Task.Delay(400);
            PrPasso("+ su H (L/2, dipende da L)", "espressione invariata, messaggio di errore, nessun ricalcolo",
                RigaVar("H").Expr.Text + " / \"" + ultimoStato + "\"", RigaVar("H").Expr.Text == "L/2" && ultimoStatoErrore && nCalcoli == cH);

            // 5 - D = 6*MM, anche con la virgola
            ok = await Applica(() => { RigaVar("D").Expr.Text = "6*MM"; ApplyVars(); });
            PrPasso("D = 6*MM e Invio", "R = 0,003, Z = 76,452 +16,276j", "R = " + RigaVar("R").Value.Text + ", Z = " + Zs(run.Zin),
                ok && Math.Abs(model.Variables.Find(v => v.Name == "R").Value - 0.003) < 1e-12 && Vicino(run.Zin, 76.452, 16.276));
            ok = await Applica(() => { RigaVar("D").Expr.Text = "6,5MM"; ApplyVars(); });
            PrPasso("D = 6,5MM (virgola decimale, unita' attaccata)", "D = 0,0065", "D = " + model.Variables.Find(v => v.Name == "D").Value.ToString("0.######", CI),
                ok && Math.Abs(model.Variables.Find(v => v.Name == "D").Value - 0.0065) < 1e-12);
            ok = await Applica(() => { RigaVar("D").Expr.Text = "6*MM"; ApplyVars(); });

            // 6 - errore voluto
            int cE = nCalcoli;
            RigaVar("NS").Expr.Text = "ODD(X)";
            ApplyVars();
            await Task.Delay(400);
            PrPasso("NS = ODD(X) con X inesistente", "stato \"errore: Variabile sconosciuta: X\", nessun ricalcolo",
                RigaVar("NS").State.Text, RigaVar("NS").State.Text.Contains("X") && nCalcoli == cE);
            PrImmagine("variabili_errore");
            ok = await Applica(() => { RigaVar("NS").Expr.Text = "ODD(20)"; ApplyVars(); });
            PrPasso("NS = ODD(20) di nuovo", "ricalcolo, Z = 76,452 +16,276j", Zs(run.Zin), ok && Vicino(run.Zin, 76.452, 16.276));

            // 7 - tabella dei fili: evidenziazione
            PrTitolo("C. Tabella dei fili (Struttura 3D)");
            SelectTab(IndiceLinguetta("Struttura 3D"));
            await Task.Delay(300);
            HighlightWire(wireRows.Find(w => !w.ReadOnly));
            await Task.Delay(200);
            PrPasso("clic in una cella del filo 1", "filo 1 evidenziato nella vista 3D", "filo evidenziato: tag " + view3d.HighlightTag, view3d.HighlightTag == 1);

            // 8 - aggiungi filo con la virgola decimale
            AddWireAfter(wireRows.Count - 1);
            WireRow nw = wireRows[wireRows.Count - 1];
            string[] celle = { "2", "-0,42", "-0,53", "0", "-0,42", "0,53", "0", "2", "21" };
            for (int k = 0; k < 9; k++) nw.Cells[k].Text = celle[k];
            ok = await Applica(ApplyWires);
            int nGw = model.Geometry.FindAll(g => g.Id == "GW").Count;
            PrPasso("Aggiungi filo: riflettore -0,42 / +-0,53, raggio 2 mm, 21 segmenti (scritto con la virgola)", "2 fili, guadagno > 4 dBi, F/B > 3 dB",
                string.Format(CI, "{0} fili, Z = {1}, {2:0.00} dBi, F/B {3:0.00} dB", nGw, Zs(run.Zin), run.GmaxDbi, run.FrontBack),
                ok && nGw == 2 && run.GmaxDbi > 4 && run.FrontBack > 3);
            PrImmagine("struttura_due_fili");

            // 8b - filo copiato da uno con raggio R
            AddWireAfter(0);
            WireRow cp = wireRows[1];
            string rCella = cp.Cells[7].Text;
            for (int k = 0; k < 9; k++) if (k != 7) cp.Cells[k].Text = new[] { "3", "0,3", "-0,45", "0", "0,3", "0,45", "0", "", "21" }[k];
            ok = await Applica(ApplyWires);
            GeomCard g3 = model.Geometry.Find(g => g.I1 == 3);
            PrPasso("filo nuovo copiato dal filo 1 (raggio scritto come variabile R)", "nella scheda il raggio resta R",
                "cella \"" + rCella + "\" -> scheda \"" + (g3 != null && g3.Src != null ? g3.Src[8] : "?") + "\"", ok && g3 != null && g3.Src != null && g3.Src[8] == "R");

            // 9 - elimina i fili aggiunti
            ok = await Applica(() =>
            {
                wireRows.RemoveAll(w => !w.ReadOnly && w.Card != null && w.Card.I1 != 1);
                RenumberWires();
                ApplyWires();
            });
            PrPasso("elimina i fili aggiunti e Applica", "si torna al dipolo: Z = 76,452 +16,276j", Zs(run.Zin), ok && Vicino(run.Zin, 76.452, 16.276));

            // 10 - Modello NEC
            PrTitolo("D. Modello NEC, salvataggio e riapertura");
            SelectTab(IndiceLinguetta("NEC"));
            await Task.Delay(300);
            string nec = txtNec != null ? (txtNec.Text ?? "") : "";
            PrPasso("linguetta Modello NEC", "righe SY e \"GW 1 NS 0 -H 0 0 H 0 R\"", nec.Contains("SY ") && nec.Contains("GW 1 NS 0 -H 0 0 H 0 R") ? "presenti" : "mancanti",
                nec.Contains("SY ") && nec.Contains("GW 1 NS 0 -H 0 0 H 0 R"));
            PrImmagine("modello_nec");

            // 11 - salva e riapri
            string fs = Path.Combine(AppPaths.ModelsDir, "prova_salvataggio.nec");
            Complex zPrima = run.Zin;
            File.WriteAllText(fs, model.ToNec());
            ok = await Apri("prova_salvataggio.nec");
            PrPasso("salva e riapri", "5 variabili, stessa impedenza " + Zs(zPrima), model.Variables.Count + " variabili, " + Zs(run.Zin),
                ok && model.Variables.Count == 5 && Vicino(run.Zin, zPrima.Real, zPrima.Imaginary, 1e-6));
            try { File.Delete(fs); } catch { }

            // 12 - MMANA
            PrTitolo("E. Modello MMANA");
            ok = await Apri("10el DK7ZB 28 ohm (MMANA).maa");
            int nf = wireRows.FindAll(w => !w.ReadOnly).Count;
            PrPasso("Esempi -> 10el DK7ZB 28 ohm (MMANA)", "Z = 28,104 -6,132j, 10 fili in tabella", Zs(run.Zin) + ", " + nf + " fili",
                ok && Vicino(run.Zin, 28.104, -6.132) && nf == 10);
            SelectTab(IndiceLinguetta("Struttura 3D"));
            await Task.Delay(300);
            PrImmagine("struttura_dk7zb");
            foreach (string t in new[] { "Diagrammi", "Frequenza", "Smith", "Correnti", "Avvisi" })
            {
                int ti = IndiceLinguetta(t);
                if (ti < 0) { PrPasso("linguetta " + t, "presente", "mancante", false); continue; }
                SelectTab(ti);
                await Task.Delay(400);
                PrImmagine(t.ToLowerInvariant());
            }
            // calcolatori delle forme semplici (bozzetto approvato alla lettera), four-square, antenne riceventi
            {
                int tc = IndiceLinguetta("Calcolatori");
                if (tc >= 0)
                {
                    SelectTab(tc);
                    foreach (string k in new[] { "loopmag", "dipoli", "delta", "moxon", "retelc", "4sq" })
                    {
                        fasCalc.Select(k);
                        await Task.Delay(k == "4sq" ? 2500 : 500);
                        PrImmagine("calcolatore_" + k);
                    }
                    PrPasso("calcolatori delle forme semplici e four-square", "pannelli aperti senza eccezioni", "loop magnetica, dipoli, delta, Moxon, rete LC, four-square", true);
                }
                int tr = IndiceLinguetta("Antenne riceventi");
                if (tr >= 0) { SelectTab(tr); await Task.Delay(500); PrImmagine("antenne_riceventi"); }
            }

            // 13 - banda, terreno, schemi
            PrTitolo("F. Banda e terreno");
            int bi = Bands.IndexOf(432.2);
            cmbBand.SelectedIndex = bi + 1;
            await Task.Delay(300);
            PrPasso("Banda 70 cm (centro operativo)", "frequenza 432,200 MHz", txtFreq.Text, ParseNum(txtFreq.Text, 0) == 432.2);
            cmbBand.SelectedIndex = Bands.IndexOf(144.3) + 1;
            await Task.Delay(300);
            ok = await Applica(() => Calculate(true));
            // la Yagi MMANA e' a quota 0: con il terreno il programma deve rifiutare e spiegare perche'
            int cG = nCalcoli;
            cmbGround.SelectedIndex = 3;
            await Task.Delay(600);
            PrPasso("Terreno Medio sulla Yagi a quota 0", "nessun calcolo e messaggio che spiega di alzare l'antenna",
                "\"" + ultimoStato + "\"", nCalcoli == cG && ultimoStatoErrore && ultimoStato.Contains("sopra lo zero"));
            ok = await Applica(() => cmbGround.SelectedIndex = 0);
            // dipolo a 10 m su terreno medio (come t3 dell'autotest)
            ok = await Apri("Dipolo 20 m a 10 m su terreno medio.nec");
            PrPasso("dipolo 20 m a 10 m su terreno medio", "Z = 74,235 +10,679j (come nec2c)", Zs(run.Zin) + ", terreno " + model.Ground.Kind,
                ok && model.Ground.Kind == GroundKind.Real && Vicino(run.Zin, 74.235, 10.679));
            SelectTab(IndiceLinguetta("Struttura 3D"));
            await Task.Delay(300);
            PrImmagine("struttura_terreno");

            PrTitolo("G. Importazione AutoEZ");
            ok = await Apri(Path.Combine(AppPaths.DataDir, "prove", "fissi", "16el I0JXX mod IC8TEM (AutoEZ).weq"));   // 0.4.30: tolto dagli esempi, resta per il collaudo
            PrPasso("apri 16el I0JXX (AutoEZ .weq)", "32 fili, Z = 32,349 +8,549j", wireRows.FindAll(w => !w.ReadOnly).Count + " fili, " + Zs(run.Zin),
                ok && wireRows.FindAll(w => !w.ReadOnly).Count == 32 && Vicino(run.Zin, 32.349, 8.549, 0.06));
            PrImmagine("weq_16el");

            PrTitolo("H. Manuale");
            SelectTab(IndiceLinguetta("Manuale"));
            await Task.Delay(800);
            PrPasso("linguetta Manuale", "indice con 16 capitoli e testo con le figure", manualChapters.Count + " capitoli", manualChapters.Count >= 16);
            PrImmagine("manuale");

            PrTitolo("I. Schemi di colori");
            for (int sc = 0; sc < Pal.SchemaNames.Length; sc++)
            {
                cmbTema.SelectedIndex = sc;
                await Task.Delay(700);
                PrPasso("schema di colori \"" + Pal.SchemaNames[sc] + "\"", "la finestra si ridisegna", "schema " + Pal.Schema, Pal.Schema == sc);
                SelectTab(IndiceLinguetta("Diagrammi"));
                await Task.Delay(400);
                PrImmagine("schema_" + sc);
            }
            cmbTema.SelectedIndex = 0;
            await Task.Delay(500);

            // 0.4.10: i pulsanti e le pillole delle Impostazioni cambiano scelte globali (sweep rapido, confronto Leeson, Sommerfeld):
            // dopo la prova delle pagine si rimettono com'erano, cosi' i modelli si calcolano come sempre
            bool rapPrima = Analysis.SweepRapido, confPrima = Analysis.ConfrontoLeeson;
            double sommPrima = cfg.GetD("terreno_sommerfeld", 0);
            await ProvaTutteLeLinguette();
            Analysis.SweepRapido = rapPrima; Analysis.ConfrontoLeeson = confPrima; cfg.SetD("terreno_sommerfeld", sommPrima);
            SelectTab(0);
            await Task.Delay(300);
            await ProvaTuttiIModelli();
        }

        // 0.4.5 (richiesta di IC8TEM del 13/11/2026 16:06): ogni modello d'esempio e dell'archivio (data/archivio) si apre nel programma come
        // farebbe l'utente (calcolo e sweep), con il controllo di congruita': il risultato dell'interfaccia deve essere uguale al calcolo
        // diretto del motore sullo stesso modello, e il modello MMANA originale uguale alla sua conversione in .nec (cartelle "originali" e
        // "convertiti in NEC"). Dalla 0.4.10 nessun modello si salta.
        async Task ProvaTuttiIModelli(List<string> soloQuesti = null)
        {
            PrTitolo(soloQuesti == null ? "M. Tutti i modelli aperti nel programma, con il controllo di congruita'" : "M. Modelli toccati dalle novita', aperti nel programma con il controllo di congruita'");
            List<string> files = new List<string>();
            string[] ext = { ".yanec", ".nec", ".maa", ".weq", ".ez", ".yag", ".ant" };
            Action<string> agg = dir =>
            {
                if (!Directory.Exists(dir)) return;
                List<string> l = new List<string>(Directory.GetFiles(dir, "*", SearchOption.AllDirectories));
                l.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (string f in l) if (Array.IndexOf(ext, Path.GetExtension(f).ToLowerInvariant()) >= 0) files.Add(f);
            };
            string ar = Path.Combine(AppPaths.DataDir, "archivio");
            if (!Directory.Exists(ar)) ar = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "data", "archivio"));
            if (soloQuesti != null) { foreach (string q in soloQuesti) { string f = Path.Combine(ar, q); if (YanecFile.Esiste(f)) files.Add(f); else PrPasso("modello " + q, "nell'archivio", "mancante", false); } }
            else { agg(AppPaths.ModelsDir); agg(ar); }
            int nOk = 0, nErr = 0, nSalt = 0, nIncongr = 0, nNonCalc = 0;
            // 0.4.8: funzioni che YA-NEC non ha ancora (superfici, fili virtuali di EZNEC, celle nascoste di AutoEZ) e file difettosi
            // (frequenza 0 in butternut-new.maa): non sono guasti, si contano a parte come nel collaudo completo
            Func<string, bool> nonCalcolabile = msg => msg != null && (msg.Contains("non sono ancora supportate") || msg.Contains("non e' supportata") ||
                msg.Contains("non ha sorgenti") || msg.Contains(".NGF") || msg.Contains("non contiene schede di geometria") || msg.Contains("Frequenza non valida nel file"));
            Stopwatch tot = Stopwatch.StartNew();
            string puntiPrima = txtNpts.Text;
            foreach (string f in files)
            {
                string nome = f.StartsWith(ar, StringComparison.OrdinalIgnoreCase) ? "archivio/" + Path.GetRelativePath(ar, f).Replace('\\', '/') : Path.GetFileName(f);
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    // segmenti prima di aprirlo (per saltare i modelli troppo lunghi)
                    NecModel prova = ApriPerProva(f);
                    int nseg = prova.BuildGeometry(new List<string>()).N;
                    // 0.4.10 (richiesta di IC8TEM del 14/11/2026 00:59): tutti i modelli, nessuno saltato. Lo sweep del collaudo ha 11 punti
                    // (3 oltre 1500 segmenti): la correttezza dello sweep e' gia' provata nelle procedure; qui conta che il modello si apra e si calcoli
                    // oltre 3000 segmenti (Butternut con 64 radiali, 7877 segmenti: alcuni minuti per calcolo) uno sweep di un punto e niente
                    // ricalcolo di congruenza; oltre 12000 la matrice supera 2 GB di memoria e il modello si segnala senza aprirlo
                    if (nseg > 12000) { nSalt++; PrRiga("         non aperto (" + nseg + " segmenti: la matrice del NEC-2 supera 2 GB di memoria): " + nome); continue; }
                    // 0.4.17: oltre 3000 segmenti il modello si prova a parte, per ultimo (fase 2D, solo se tutto il resto e' superato)
                    if (soloQuesti == null && !ProveSoloButternut && nseg > SegmentiProvaAParte) { nSalt++; PrRiga("         non aperto qui (" + nseg + " segmenti): i 3 modelli piu' pesanti si provano nella fase 2E, senza finestra: " + nome); continue; }
                    bool enorme = nseg > 3000;
                    // 0.4.19: imposto nello sweep (la casella si riscrive quando il modello si apre: nella 0.4.17 la Butternut faceva 41 punti)
                    PuntiSweepForzati = enorme ? 2 : nseg > 1500 ? 3 : 11;
                    int attesa = enorme ? 1800000 : nseg > 1500 ? 600000 : 120000;
                    int c0 = nCalcoli, s0 = nSweep;
                    LoadModelFile(f);
                    bool ok = await Attendi(() => nCalcoli > c0 || (ultimoStatoErrore && ultimoStato.Length > 0), attesa);
                    bool okCalc = nCalcoli > c0 && run != null && model != null;
                    if (okCalc) await Attendi(() => nSweep > s0, attesa);
                    if (!okCalc && nonCalcolabile(ultimoStato)) { nNonCalc++; PrRiga("         non calcolabile (funzione non ancora in YA-NEC o file difettoso): " + nome + ": " + ultimoStato); continue; }
                    if (!okCalc) { nErr++; PrPasso("modello " + nome, "si apre e si calcola", "non calcolato: " + ultimoStato, false); continue; }
                    RunResult r = run;
                    bool finito = !double.IsNaN(r.Zin.Real) && !double.IsInfinity(r.Zin.Real) && !double.IsNaN(r.GmaxDbi);
                    // congruita' 1: stesso modello calcolato direttamente dal motore alla stessa frequenza
                    NecModel md = model.Clone();
                    RunResult rd = enorme ? r : Analysis.Run(md, md.BuildGeometry(), r.FreqMHz, 5.0);
                    bool cong = Complex.Abs(rd.Zin - r.Zin) <= 1e-6 * Math.Max(1, Complex.Abs(r.Zin)) + 1e-6;
                    string extra = "";
                    // congruita' 2: MMANA originale contro la sua conversione in .nec
                    if (!enorme && f.IndexOf("(originali)", StringComparison.OrdinalIgnoreCase) >= 0 && Path.GetExtension(f).ToLowerInvariant() == ".maa")
                    {
                        string nec = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(f)), "Modelli IC8TEM (convertiti in NEC)", Path.GetFileNameWithoutExtension(f) + ".nec");
                        if (File.Exists(nec))
                        {
                            NecModel mc = NecModel.Load(nec);
                            RunResult rc = Analysis.Run(mc, mc.BuildGeometry(), r.FreqMHz, 5.0);
                            // 0.4.8: si confrontano due calcoli fatti nello stesso modo (rd = motore sul modello aperto, griglia 5 gradi), non il
                            // risultato dell'interfaccia (griglia piu' fitta): il Gmax di r dipende dal passo della griglia (dl6mi-moxon, collaudo 0.4.7)
                            bool ugu = Complex.Abs(rc.Zin - rd.Zin) <= 0.02 * Math.Max(1, Complex.Abs(rd.Zin)) && Math.Abs(rc.GmaxDbi - rd.GmaxDbi) <= 0.1;
                            extra = string.Format(CI, "; .nec convertito {0}{1}", Zs(rc.Zin), ugu ? " (uguale)" : " (DIVERSO)");
                            if (!ugu) cong = false;
                        }
                    }
                    if (!cong) nIncongr++;
                    bool okTutto = finito && cong;
                    if (okTutto) nOk++; else nErr++;
                    string riga = string.Format(CI, "{0} segm., {1:0.###} MHz, Z {2}, ROS {3:0.00}, {4:0.00} dBi, F/B {5:0.0} dB, AGT {6:0.00}, {7:0.0} s{8}",
                        r.Geometry.N, r.FreqMHz, Zs(r.Zin), r.Swr, r.GmaxDbi, r.FrontBack, r.AgtDb, sw.Elapsed.TotalSeconds, extra);
                    PrPasso("modello " + nome, "si apre, si calcola, interfaccia = motore" + (extra.Length > 0 ? ", MMANA = .nec convertito" : ""), riga, okTutto);
                }
                catch (Exception ex)
                {
                    if (nonCalcolabile(ex.Message)) { nNonCalc++; PrRiga("         non calcolabile (funzione non ancora in YA-NEC o file difettoso): " + nome + ": " + ex.Message); continue; }
                    nErr++; PrPasso("modello " + nome, "si apre e si calcola", ex.GetType().Name + ": " + ex.Message, false);
                }
            }
            txtNpts.Text = puntiPrima;
            PuntiSweepForzati = 0;
            PrPasso("riepilogo dei modelli", files.Count + " modelli, tutti aperti", string.Format(CI, "{0} OK, {1} con errore ({2} incongruenti), {3} non calcolabili (funzioni non ancora in YA-NEC o file difettosi), {4} rimandati alla fase 2D o non aperti, {5:0} s", nOk, nErr, nIncongr, nNonCalc, nSalt, tot.Elapsed.TotalSeconds), nErr == 0);
        }

        NecModel ApriPerProva(string f)
        {
            List<MmanaWireInfo> w;
            string e = Path.GetExtension(f).ToLowerInvariant();
            if (e == ".maa") return MmanaImport.Load(f, (double)(numSegL.Value ?? 40), out w);
            if (e == ".weq") return EzImport.LoadWeq(f);
            if (e == ".ez") return EzImport.LoadEz(f);
            if (e == ".yag") return K6stiImport.LoadYag(f);
            if (e == ".ant") return K6stiImport.LoadAnt(f);
            return NecModel.Load(f);
        }

        // 0.4.5 (richiesta di IC8TEM del 13/11/2026 16:02): ogni linguetta e ogni sottolinguetta (voci dei fascioni) si apre con un
        // modello caricato; per ognuna: nessuna eccezione, pagina presente, stato senza "errore", immagine. Anche senza modello.
        async Task ProvaTutteLeLinguette()
        {
            PrTitolo("L. Tutte le linguette e le sottolinguette: fumetti, pulsanti e caselle di ogni pagina");
            await ProvaFumetti(this, "barra dei comandi e linguette");
            bool okA = await Apri("10el DK7ZB 28 ohm (MMANA).maa");
            PrPasso("modello per la prova delle linguette", "10el DK7ZB calcolato", okA ? "calcolato" : "non calcolato", okA);
            Dictionary<string, Fascione> sotto = new Dictionary<string, Fascione>
            {
                { "Calcolatori", fasCalc }, { "Antenne riceventi", fasRx }
            };
            int nTab = 0, nSub = 0, errTab = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                string nome = tabs[i].Tag as string ?? ("linguetta " + i);
                string esito = "";
                bool ok = true;
                try
                {
                    SelectTab(i);
                    await Task.Delay(nome == "Manuale" || nome == "Autotest" ? 900 : 450);
                    ok = page != null && page.Content != null;
                    if (!ok) esito = "pagina vuota";
                    if (ultimoStatoErrore && ultimoStato.IndexOf("errore", StringComparison.OrdinalIgnoreCase) >= 0) { ok = false; esito = "stato: " + ultimoStato; }
                }
                catch (Exception ex) { ok = false; esito = ex.GetType().Name + ": " + ex.Message; }
                nTab++;
                if (!ok) errTab++;
                PrPasso("linguetta \"" + nome + "\"", "si apre senza errori", ok ? "aperta" : esito, ok);
                PrImmagine("L_" + Pulito(nome));
                await ProvaPagina(nome);
                Fascione f;
                if (!sotto.TryGetValue(nome, out f) || f == null) continue;
                foreach (string k in f.Chiavi())
                {
                    string es = "";
                    bool ok2 = true;
                    try
                    {
                        f.Select(k);
                        await Task.Delay(nome == "Calcolatori" ? 1200 : 600);   // i calcolatori fanno il calcolo in tempo reale
                        if (ultimoStatoErrore && ultimoStato.IndexOf("errore", StringComparison.OrdinalIgnoreCase) >= 0) { ok2 = false; es = "stato: " + ultimoStato; }
                    }
                    catch (Exception ex) { ok2 = false; es = ex.GetType().Name + ": " + ex.Message; }
                    nSub++;
                    if (!ok2) errTab++;
                    PrPasso("  " + nome + " > " + f.Testo(k).Trim(), "si apre senza errori", ok2 ? "aperta" : es, ok2);
                    PrImmagine("L_" + Pulito(nome) + "_" + Pulito(k));
                    await ProvaPagina(nome + " > " + f.Testo(k).Trim());
                }
            }
            // senza modello: le linguette non devono rompersi (si chiude il modello aprendo un file inesistente non serve: si prova con model nullo)
            PrPasso("riepilogo linguette", "tutte aperte senza errori", string.Format(CI, "{0} linguette e {1} sottolinguette, {2} con errore", nTab, nSub, errTab), errTab == 0);
            PrPasso("riepilogo dei fumetti", "ognuno con il suo testo", string.Format(CI, "{0} fumetti mostrati, {1} vuoti", prFumetti, prFumettiVuoti), prFumettiVuoti == 0);
            PrPasso("riepilogo delle funzioni", "nessuna eccezione", string.Format(CI, "{0} pulsanti premuti, {1} caselle cambiate e rimesse, {2} eccezioni", prPulsanti, prCaselle, prEccezioni), prEccezioni == 0);
            try { File.WriteAllText(Path.Combine(AppPaths.DataDir, "fumetti.txt"), prElencoFumetti.ToString(), new UTF8Encoding(false)); } catch { }
        }

        // ------------------------------------------------------------------------------------------------
        // 0.4.10 (richiesta di IC8TEM del 14/11/2026 00:59): in ogni pagina e sottopagina si mostrano tutti i fumetti, si premono
        // tutti i pulsanti (tranne quelli che aprono finestre di file o siti) e si cambiano tutte le caselle (poi si rimettono).
        // Ogni controllo si prova una volta sola anche se compare in piu' pagine. Dopo ogni linguetta si riapre il modello di prova.
        // ------------------------------------------------------------------------------------------------
        static string TestoDi(object o)
        {
            if (o == null) return "";
            if (o is string st) return st;
            if (o is TextBlock tb) return tb.Text ?? "";
            if (o is Control c)
            {
                StringBuilder b = new StringBuilder();
                foreach (TextBlock t in c.GetVisualDescendants().OfType<TextBlock>()) { if (b.Length > 0) b.Append(' '); b.Append(t.Text); }
                if (b.Length == 0 && c is ContentControl cc) return TestoDi(cc.Content);
                return b.ToString();
            }
            return o.ToString();
        }

        static string Breve(string t) { t = (t ?? "").Replace("\r", " ").Replace("\n", " ").Trim(); return t.Length > 70 ? t.Substring(0, 70) + "..." : t; }

        async Task ProvaFumetti(Control radice, string dove)
        {
            List<Control> con = radice.GetVisualDescendants().OfType<Control>().Where(c => ToolTip.GetTip(c) != null && c.IsEffectivelyVisible).ToList();
            prElencoFumetti.AppendLine("== " + dove);
            foreach (Control c in con)
            {
                if (!prProvati.Add(c)) continue;
                string testo = TestoDi(ToolTip.GetTip(c)).Trim();
                string cosa = c is ContentControl cc ? Breve(TestoDi(cc.Content)) : c.GetType().Name;
                try { ToolTip.SetIsOpen(c, true); await Task.Delay(40); ToolTip.SetIsOpen(c, false); }
                catch (Exception ex) { testo += "  [non mostrato: " + ex.Message + "]"; }
                prFumetti++;
                if (testo.Length == 0) { prFumettiVuoti++; PrPasso("fumetto di \"" + cosa + "\" (" + dove + ")", "testo presente", "vuoto", false); }
                prElencoFumetti.AppendLine("  " + cosa + ": " + testo);
            }
        }

        // pulsanti che aprono finestre di file, siti o il programma di posta: non si premono nel collaudo automatico
        static readonly string[] NonPremere = { "salva", "pdf", "esporta", "sfoglia", "cartella", "ko-fi", "importa", "carica", "scegli", "esci", "chiudi", "sito", "github", "qrz", "e-mail", "about", "copia la", "copia negli", "apri modello", "apri il file", "feed dal modello nec", "da file", "csv", "scheda", "immagine", "png", "stampa" };

        async Task ProvaFunzioni(Control radice, string dove)
        {
            List<Button> pul = radice.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible && b.IsEffectivelyEnabled).ToList();
            int n = 0;
            foreach (Button b in pul)
            {
                if (n >= 150) break;
                if (prProvati.Contains(b)) continue;
                string t = Breve(TestoDi(b.Content));
                string tl = t.ToLowerInvariant();
                if (t.EndsWith("...") || NonPremere.Any(x => tl.Contains(x))) { prProvati.Add(b); continue; }
                if (!b.IsEffectivelyVisible || !b.IsEffectivelyEnabled || b.GetVisualRoot() == null) continue;
                prProvati.Add(b);
                n++; prPulsanti++;
                prUltimaAzione = "pulsante \"" + t + "\" (" + dove + ")";
                try
                {
                    b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    if (b.Command != null && b.Command.CanExecute(b.CommandParameter)) b.Command.Execute(b.CommandParameter);
                    await Task.Delay(250);
                    // un'ottimizzazione o un calcolo lungo partito dal pulsante si ferma subito
                    foreach (Button f in radice.GetVisualDescendants().OfType<Button>().Where(x => TestoDi(x.Content).Trim() == "Ferma" && x.IsEffectivelyEnabled).ToList())
                    { f.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(150); }
                }
                catch (Exception ex) { PrPasso(prUltimaAzione, "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
                // le finestre aperte dal pulsante (verifica della segmentazione, menu) si chiudono
                foreach (Window w in OwnedWindows.ToList()) { try { w.Close(); } catch { } }
            }
            List<CheckBox> cas = radice.GetVisualDescendants().OfType<CheckBox>().Where(c => c.IsEffectivelyVisible && c.IsEffectivelyEnabled).ToList();
            foreach (CheckBox c in cas)
            {
                if (!prProvati.Add(c) || c.GetVisualRoot() == null) continue;
                prCaselle++;
                string t = Breve(TestoDi(c.Content));
                prUltimaAzione = "casella \"" + t + "\" (" + dove + ")";
                try
                {
                    bool? prima = c.IsChecked;
                    c.IsChecked = !(prima == true);
                    await Task.Delay(200);
                    c.IsChecked = prima;
                    await Task.Delay(150);
                }
                catch (Exception ex) { PrPasso(prUltimaAzione, "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            }
            prUltimaAzione = "";
            if (n > 0 || cas.Count > 0) PrRiga(string.Format(CI, "         {0}: {1} pulsanti premuti, {2} caselle provate", dove, n, cas.Count));
        }

        async Task ProvaPagina(string dove)
        {
            if (page == null) return;
            int linguetta = curTab;
            try
            {
                await ProvaFumetti(page, dove);
                await ProvaFunzioni(page, dove);
            }
            catch (Exception ex) { PrPasso("prova della pagina " + dove, "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // il modello di prova si riapre: i pulsanti possono averlo cambiato
            if (model == null || !string.Equals(Path.GetFileName(model.FileName ?? ""), "10el DK7ZB 28 ohm (MMANA).maa", StringComparison.OrdinalIgnoreCase) || prPulsanti > 0)
                await Apri("10el DK7ZB 28 ohm (MMANA).maa");
            // "Apri come modello" e simili portano alla Struttura 3D: si torna alla linguetta in prova
            if (curTab != linguetta) { SelectTab(linguetta); await Task.Delay(300); }
        }

        static string Pulito(string s)
        {
            StringBuilder b = new StringBuilder();
            foreach (char ch in s.ToLowerInvariant()) b.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            return b.ToString().Trim('_');
        }

        void PrFine()
        {
            PrRiga("");
            PrRiga(new string('=', 100));
            PrRiga(string.Format(CI, (ProveSoloButternut ? "PROVA DELLA BUTTERNUT" : "PROVE DELL'INTERFACCIA") + ": {0} passi, {1} OK, {2} ERRORE.  Immagini: {3} in data\\collaudo_immagini.", prN, prOk, prErr, prImg));
            PrRiga(string.Format(CI, "Fumetti mostrati: {0} ({1} vuoti, elenco in data\\fumetti.txt); pulsanti premuti: {2}; caselle provate: {3}; eccezioni: {4}.", prFumetti, prFumettiVuoti, prPulsanti, prCaselle, prEccezioni));
            PrRiga(prErr == 0 ? "PROVE SUPERATE." : "PROVE NON SUPERATE: mandare collaudo_completo.txt e le immagini.");
            try { File.WriteAllText(Path.Combine(AppPaths.DataDir, prFile), prRep.ToString(), new UTF8Encoding(false)); } catch { }
        }
    }
}
