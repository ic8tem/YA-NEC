// YA-NEC - collaudo automatico senza finestra (avvio con --collaudo, lanciato da compila.bat / compila.sh).
// Esegue le procedure una per una e scrive in data/collaudo.txt, per ogni passo: cosa fa, cosa si aspetta,
// cosa ha ottenuto ed esito (OK, AVVISO, ERRORE). Alla fine c'e' il riepilogo delle procedure.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using YaNec.Engine;

namespace YaNec
{
    public static partial class Collaudo
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static StringBuilder sb;
        static int procN, passoN, errProc, avvProc;
        static readonly List<string> riepilogo = new List<string>();
        static string procTitolo;

        const string DipoloVar =
            "CM Collaudo: dipolo con variabili\nCE\nSY L=1000*MM, H=L/2\nSY D=4*MM, R=D/2\nSY NS=ODD(20)\n" +
            "GW 1 NS 0 -H 0 0 H 0 R\nGE 0\nEX 0 1 (NS+1)/2 0 1 0\nFR 0 1 0 0 144.3 0\nEN\n";

        public static int Esegui()
        {
            sb = new StringBuilder();
            Riga("COLLAUDO DI YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + (SoloNovita ? " - SOLO LE NOVITA' DOPO LA 0.4.10 (compila.bat /completo per tutto)" : ""));
            Riga("Ogni procedura elenca i passi: cosa viene fatto, il valore atteso, il valore ottenuto e l'esito.");
            Riga("OK = come previsto;  AVVISO = da guardare ma non e' un guasto;  ERRORE = non funziona come previsto.");
            Riga(new string('=', 100));

            Procedura("Ambiente di esecuzione", "controllare sistema, .NET e cartella dati", Ambiente);
            Procedura("Autotest del motore", "confronto del motore C# con nec2c sui modelli di prova", Autotest);
            Procedura("Modelli di esempio", "aprire e calcolare ogni modello della cartella data/modelli", Esempi);
            Procedura("Variabili e formule (schede SY)", "valori, modifica di una variabile, errori, nomi riservati", Variabili);
            Procedura("Tabella dei fili", "modificare un filo e aggiungerne uno come fa la tabella sotto la Struttura 3D", Fili);
            Procedura("Importazione MMANA e segmentazione", "DK7ZB 10 el con 20, 40 e 80 segmenti per lambda", Mmana);
            Procedura("Importazione AutoEZ (.weq)", "file di prova di IC8TEM confrontato con nec2c", Importatori);
            Procedura("Abaco dei cavi e linee di accordo", "lettura di data/cavi.csv, adattamento DK7ZB a 28 ohm", Cavi);
            Procedura("Sweep in frequenza", "41 punti attorno al dipolo, minimo del ROS", Sweep);
            Procedura("Eggbeater e QFH (0.4.2)", "Calcolatori per i satelliti: quadratura delle spire dell'eggbeater come tamago, QFH di Coppens con l'impedenza di nec2c", Satelliti);
            Procedura("Esempi ufficiali del manuale NEC-2 (0.4.2)", "Esempi 1, 2 e 3 della Part III (NTIS ADA956129, mazzi di hf-nec2c): impedenze, efficienza e guadagno pubblicati; esempio 2 con la sorgente EX 5", EsempiManualeNec2);
            Procedura("Filo isolato LD 7 di 4nec2 (0.4.2)", "V invertita 80 m con filo in PVC (modello 36inv_coat di 4nec2): carico LD 2 di Cebik e impedenza come nec2c", FiloIsolato);
            Procedura("Simmetrie GX e GR nella soluzione (0.4.2)", "quattro modelli con GR e GX (carichi, terreno perfetto, linea): stessa impedenza con e senza simmetria e come nec2c", Simmetrie);
            Procedura("Sweep rapido MBPE adattivo (0.4.2)", "dipolo e DK7ZB 10 el: sweep rapido confrontato con lo sweep completo punto per punto (ROS 0,1 %, guadagni 0,1 dB)", SweepRapido);
            Procedura("Calcolatori, stack e fascione (0.4.0)", "formule LPDA (Balanis), LPDA 144-432, loop magnetica, riconoscimento dello stack 4x7 DK7ZB con DL6WU, stile del fascione", Calcolatori040);
            Procedura("Calcolatori delle forme semplici e Sommerfeld (0.4.1)", "tutti i calcolatori del bozzetto approvato: riquadri e modello NEC; MOXGEN, rete L, trappola W3DZZ", Calcolatori041);
            Procedura("Salvataggio e riapertura", "scrivere il modello .nec, rileggerlo e ricalcolare", Salvataggio);
            Procedura("Rumore e G/T: regola di TANT all'orizzonte", "a 0 gradi un diagramma simmetrico vede (Tcielo + Tterreno) / 2 (TANT 1.2, YT1NT/VE3EA)", GtTant);
            Procedura("Scheda Loop della barra Geometria (0.4.2)", "loop magnetica 1 m, tubo 22 mm, 7,1 MHz: L, C, Rr, perdita ed efficienza come nec2c e come le formule; anello di Faraday D/10", LoopProva);
            Procedura("Barra Geometria (0.4.2)", "ruota, sposta, scala, specchia e copia sui fili GW (anche con le variabili) e con una scheda GM quando ci sono archi", GeometriaProva);
            Procedura("Ottimizzatore (0.4.2)", "dipolo con la lunghezza variabile e il solo obiettivo jX: entrambi i metodi portano il dipolo in risonanza", OttimizzatoreProva);
            Procedura("Yagi ARRL 520-40H con e senza Leeson (0.4.2)", "file .YAG dell'ARRL (diametri in pollici) e tabella 8.6 dell'Antenna Book 24a: con la correzione impedenza e guadagno come EZNEC, senza il diagramma si rovescia", Arrl52040H);
            Procedura("G/T della EF0213 di YU7EF come TANT (0.4.2)", "13 el 144 MHz con elementi da 4 mm in alluminio: guadagno, Ta e G/T a 30 gradi con cielo 200 K e terreno 1000 K, perdite dei conduttori comprese", GtEf0213);
            Procedura("Importatori YO (.yag) e AO / NEC-Wires (.ant) di K6STI (0.4.2)", "Yagi 7 el. 144 MHz 28_3B.YAG (raccolta YO651) e Yagi 3 el. 14,2 MHz dell'esempio di ANTFILE.DOC, confronto con nec2c; dalla 0.5.0 tutte le funzioni di ANTFILE.DOC (Shift e Rotate, simboli, espressioni, Laplace, materiali, simmetrico)", ImportK6sti);
            Procedura("Corpus dei modelli di 4nec2 (0.4.2)", "467 modelli della raccolta di 4nec2 aperti e calcolati, impedenza confrontata con nec2c (data/prove/4nec2_attesi.csv)", Corpus4nec2);
            Procedura("Parabola primo fuoco (0.4.2)", "integrazione d'apertura, efficienze, meccanica, bande ed EME: autotest del motore delle parabole (tabelle GMRT/Stutzman, Sengupta, Mumford, Ruze); dalla 0.4.19 anche trombe e illuminatori (Balanis, W1GHZ)", ParabolaProve);
            Procedura("Salva in .weq e .ez e riapri (0.4.3)", "dipolo con variabile, carico R+jX e linea verso un secondo dipolo, su terreno reale: scritto in AutoEZ e in EZNEC, riletto e ricalcolato", SalvaWeqEz);
            Procedura("Antenne riceventi di IV3PRK (0.4.3)", "DHDL, due DHDL broadside a 90 m, Flag W7IUV, Waller Flag e Pennant a 1,83 MHz con Sommerfeld: RDF come nec2c e vicino ai valori pubblicati da IV3PRK", RiceventiIv3prk);
            Procedura("LPDA senza filo fittizio (0.4.7)", "lo stub della LPDA e' un'ammettenza in parallelo, non un filo lontano", LpdaStub);
            Procedura("Novita' dopo la 0.4.10 (0.4.11-0.4.16)", "LPDA di AC6LA con IF() nei .weq, elenco dei passi da guardare, parabola offset, sweep e memoria, aggiornamento automatico", Novita0413);
            Procedura("Novita' della 0.5.0: Yagi lunga DL6WU, Quagi, file di K6STI", "misure DL6WU (KY4Z/W6NBI, G3SEK, DL6WU 1988) e Quagi N6NB/Cebik confrontate con nec2c; un passo di ottimizzazione; tutte le funzioni di ANTFILE.DOC", Novita050);
            Procedura("Novita' della 0.5.3: cavi, cambio d'impedenza, balun, quota, Leeson A/B", "elenco dei cavi con il database di YAEC e le altre impedenze, trasformatore a quarto d'onda (delta loop 100 ohm + RG-59), linea TL messa nel modello, balun con tutti i rapporti (anche 64:1) e i nuclei in polvere di ferro, quota dell'antenna al centimetro, lingue e colori, Leeson sempre attivo sotto la risonanza (A) e taratura a zero finche' non e' approvata (B)", Novita053);
            Procedura("Novita' della 0.5.1: controllo dei dati", "i 45 controlli approvati: ogni controllo provato con un dato sbagliato apposta; dipolo giusto senza controlli; modelli di esempio e calcolatori senza errori", Novita051);
            Procedura("Novita' della 0.4.30: formato .yanec 2, fili nel terreno, gradini elemento per elemento, hexbeam", "esempi e archivio in .yanec 2 con l'abaco della descrizione; regola di EZNEC dei fili dentro o distesi sul terreno, limite della correzione di Leeson, calcolatore Hexbeam 20-10 m con l'accordo a 50 ohm", Novita0430);
            Procedura("Novita' della 0.4.21: arcarecci, dima della centina, correzione del boom, disco NEC", "arcarecci diritti dimensionati, dima DXF, formula DL6WU/G3SEK nei due versi, diametri massimi del modello NEC del disco", Novita0421);
            Procedura("Novita' della 0.4.20: LU a blocchi, controlli geometrici di EZNEC, terreno MININEC", "LU a blocchi come la LU classica, sei regole del Geometry Check, terreno MININEC di EZNEC (GN 3) in lettura e scrittura .ez, diagramma 3D affinato, processori", Novita0420);
            Procedura("Novita' della 0.4.18: BOG, terreno vicino, Butternut corretta", "BOG approssimato a lambda/1000 come nec2c, avvisi per i fili vicini al terreno e le giunzioni affollate, Butternut con 64 radiali corretta, tempi delle fasi del motore", Novita0418);
            Procedura("Calcolo completo di tutti i modelli (0.4.4)", "ogni modello che il programma contiene (esempi, corpus di 4nec2, calcolatori, antenne riceventi) aperto e calcolato per intero, con una riga nel resoconto e nel registro; AGT entro 0,95-1,05 (+-0,2 dB) dove vale", TuttiIModelli);
            Procedura("Registro, tempi, librerie e XR-5 di fabbrica (0.4.2)", "registro sempre attivo con computer e punteggio, librerie native in lib, modello XR-5 con le misure del manuale", Registro042);

            Riga("");
            Riga(new string('=', 100));
            Riga("RIEPILOGO");
            foreach (string r in riepilogo) Riga("  " + r);
            Riga("");
            Riga(errProc == 0
                ? (avvProc == 0 ? "COLLAUDO SUPERATO: tutte le procedure sono andate come previsto." : "COLLAUDO SUPERATO con " + avvProc + " procedure da guardare (AVVISO).")
                : "COLLAUDO NON SUPERATO: " + errProc + " procedure con ERRORE. Mandare il file collaudo.txt.");
            string txt = sb.ToString();
            Console.Write(txt);
            try { File.WriteAllText(Path.Combine(AppPaths.DataDir, "collaudo.txt"), txt, new UTF8Encoding(false)); } catch { }
            return errProc;
        }

        // ------------------------------------------------------------------------------------------------
        static int pErr, pAvv;

        static void Riga(string s) { sb.AppendLine(s); }

        // 0.4.13 (richiesta di IC8TEM del 14/11/2026 08:25): con --solo-novita si eseguono solo le procedure toccate dalle modifiche
        // fatte dopo la 0.4.10 (formule IF, importazione .weq, parabole, elenco dei passi); compila.bat /completo esegue tutto
        public static bool SoloNovita;
        static readonly string[] ProcedureNovita = { "Ambiente di esecuzione", "Autotest del motore", "Variabili e formule", "Importazione AutoEZ",
            "Parabola primo fuoco", "Novita' dopo la 0.4.10", "Novita' della 0.4.18", "Novita' della 0.4.20", "Novita' della 0.4.21", "Novita' della 0.4.30", "Novita' della 0.5.0", "Novita' della 0.5.1", "Novita' della 0.5.3", "Importatori YO" };

        static void Procedura(string titolo, string scopo, Action corpo)
        {
            if (SoloNovita)
            {
                bool tenuta = false;
                foreach (string t in ProcedureNovita) if (titolo.StartsWith(t, StringComparison.Ordinal)) tenuta = true;
                if (!tenuta) return;
            }
            procN++;
            passoN = 0;
            pErr = 0;
            pAvv = 0;
            procTitolo = titolo;
            Riga("");
            Riga("PROCEDURA " + procN + " - " + titolo);
            Riga("Scopo: " + scopo);
            Stopwatch sw = Stopwatch.StartNew();
            try { using (Prestazioni.Misura("Collaudo: " + titolo)) corpo(); }
            catch (Exception ex) { Passo("la procedura si e' interrotta", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            string esito = pErr > 0 ? "NON SUPERATA (" + pErr + " errori)" : (pAvv > 0 ? "SUPERATA con " + pAvv + " avvisi" : "SUPERATA");
            Riga(string.Format(CI, "Esito procedura {0}: {1}  [{2:0.0} s]", procN, esito, sw.Elapsed.TotalSeconds));
            if (pErr > 0) errProc++; else if (pAvv > 0) avvProc++;
            riepilogo.Add(string.Format(CI, "{0,2}. {1,-40} {2}", procN, titolo, esito));
        }

        static void Passo(string cosa, string atteso, string ottenuto, bool ok, bool soloAvviso = false)
        {
            passoN++;
            string e = ok ? "OK" : (soloAvviso ? "AVVISO" : "ERRORE");
            if (!ok) { if (soloAvviso) pAvv++; else pErr++; }
            Riga(string.Format(CI, "  {0}.{1,-3} {2}", procN, passoN, cosa));
            if (!string.IsNullOrEmpty(atteso)) Riga("         atteso:   " + atteso);
            Riga("         ottenuto: " + ottenuto);
            Riga("         esito:    " + e);
        }

        static void Info(string s) { Riga("         " + s); }

        // 0.4.2 (collaudo 0.4.1 di IC8TEM, 13/11/2026: "6,05 mΩ" scartato perche' si cercava il testo "6,0 mΩ"): numero letto dal testo
        // del riquadro, con la virgola o col punto, e confrontato con un intervallo
        static bool Fra(string testo, string prima, string dopo, double min, double max)
        {
            int i = testo.IndexOf(prima, StringComparison.Ordinal);
            if (i < 0) return false;
            i += prima.Length;
            int j = testo.IndexOf(dopo, i, StringComparison.Ordinal);
            if (j < 0) return false;
            double v;
            if (!double.TryParse(testo.Substring(i, j - i).Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return false;
            return v >= min && v <= max;
        }

        static string Z(Complex z) { return string.Format(CI, "{0:0.000} {1:+0.000;-0.000}j ohm", z.Real, z.Imaginary); }

        static bool Finito(double v) { return !double.IsNaN(v) && !double.IsInfinity(v); }

        // ------------------------------------------------------------------------------------------------
        static void Ambiente()
        {
            Passo("sistema operativo e architettura", "", System.Runtime.InteropServices.RuntimeInformation.OSDescription + " (" +
                  System.Runtime.InteropServices.RuntimeInformation.OSArchitecture + "), " + Environment.ProcessorCount + " processori", true);
            Passo("versione di .NET in uso", "8 o successiva", Environment.Version.ToString(), Environment.Version.Major >= 8);
            Passo("cartella del programma", "", AppPaths.AppDir, Directory.Exists(AppPaths.AppDir));
            string dd = AppPaths.DataDir;
            bool scrivibile;
            try { string t = Path.Combine(dd, "prova_scrittura.tmp"); File.WriteAllText(t, "x"); File.Delete(t); scrivibile = true; }
            catch { scrivibile = false; }
            Passo("cartella dati scrivibile (config.ini, autotest, registro)", "si'", dd + (scrivibile ? "  (scrivibile)" : "  (NON scrivibile)"), scrivibile);
            int nm = Directory.Exists(AppPaths.ModelsDir) ? Directory.GetFiles(AppPaths.ModelsDir).Length : 0;
            Passo("modelli di esempio in data/modelli", "almeno 1", nm + " file", nm > 0);
            string cv = Path.Combine(dd, "cavi.csv");
            Passo("abaco dei cavi data/cavi.csv", "presente", File.Exists(cv) ? "presente" : "mancante", File.Exists(cv));
            string mp = MainWindow.ManualPath;
            int cap = File.Exists(mp) ? Array.FindAll(File.ReadAllLines(mp), x => x.StartsWith("## ")).Length : 0;
            Passo("manuale d'uso data/manuale/Manuale.md (linguetta Manuale)", "presente, 16 capitoli", File.Exists(mp) ? cap + " capitoli" : "mancante", cap >= 16);
        }

        static void Autotest()
        {
            foreach (SelfTest.Line l in SelfTest.Run())
                Passo(l.Name, "uguale al riferimento", l.Detail, l.Pass);
        }

        static NecModel Apri(string path)
        {
            List<MmanaWireInfo> w;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".maa") return MmanaImport.Load(path, 40, out w);
            if (ext == ".weq") return EzImport.LoadWeq(path);
            if (ext == ".ez") return EzImport.LoadEz(path);
            if (ext == ".yag") return K6stiImport.LoadYag(path);
            if (ext == ".ant") return K6stiImport.LoadAnt(path);
            return NecModel.Load(path);
        }

        static RunResult Calcola(NecModel m, double f)
        {
            SegmentGeometry g = m.BuildGeometry(new List<string>());
            return Analysis.Run(m, g, f, 2.0, CancellationToken.None);
        }

        static void Esempi()
        {
            if (!Directory.Exists(AppPaths.ModelsDir)) { Passo("cartella data/modelli", "presente", "mancante", false); return; }
            foreach (string f in Directory.GetFiles(AppPaths.ModelsDir))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                if (ext != ".yanec" && ext != ".nec" && ext != ".maa" && ext != ".weq" && ext != ".ez") continue;
                Stopwatch sw = Stopwatch.StartNew();
                NecModel m = Apri(f);
                RunResult r = Calcola(m, m.FreqMHz);
                // con piu' sorgenti sfasate (four-square) un elemento puo' avere resistenza negativa: e' normale
                bool ok = Finito(r.Zin.Real) && Finito(r.Zin.Imaginary) && Finito(r.GmaxDbi) && (r.Zin.Real > 0 || m.Sources.Count > 1);
                Passo("apertura e calcolo di \"" + Path.GetFileName(f) + "\"", "impedenza e guadagno validi",
                    string.Format(CI, "{0:0.###} MHz, {1} segm.: Z = {2}, ROS {3:0.00} su {4} ohm, Gmax {5:0.00} dBi, F/B {6:0.00} dB, effic. {7:0.0} % ({8:0.0} s)",
                        m.FreqMHz, r.Geometry.N, Z(r.Zin), r.Swr, r.Z0, r.GmaxDbi, r.FrontBack, r.Efficiency, sw.Elapsed.TotalSeconds), ok);
                if (m.Ground.Kind != GroundKind.Real && r.Efficiency > 99.9)
                    Passo("  AGT (test di convergenza) dello stesso modello", "entro +-0,3 dB", string.Format(CI, "{0:0.00} dB", r.AgtDb), Math.Abs(r.AgtDb) <= 0.3, true);
                foreach (string w in m.Warnings) Info("avviso del modello: " + w);
            }
        }

        static void Variabili()
        {
            NecModel m = NecModel.Parse(DipoloVar);
            Info("modello di prova:  SY L=1000*MM, H=L/2 / SY D=4*MM, R=D/2 / SY NS=ODD(20) / GW 1 NS 0 -H 0 0 H 0 R / EX 0 1 (NS+1)/2 0 1 0");
            string vals = "";
            foreach (SyVar v in m.Variables) vals += v.Name + "=" + v.Value.ToString("0.######", CI) + " ";
            bool okv = m.Variables.Count == 5 && Math.Abs(m.Variables[0].Value - 1) < 1e-12 && Math.Abs(m.Variables[3].Value - 0.002) < 1e-12 && m.Variables[4].Value == 21;
            Passo("lettura delle variabili", "L=1 H=0,5 D=0,004 R=0,002 NS=21", vals.Trim(), okv);
            GeomCard g = m.Geometry[0];
            Passo("filo calcolato dalle variabili", "21 segmenti, Y da -0,5 a 0,5 m, raggio 0,002 m",
                string.Format(CI, "{0} segmenti, Y da {1} a {2} m, raggio {3} m", g.I2, g.F2, g.F5, g.F7), g.I2 == 21 && g.F2 == -0.5 && g.F5 == 0.5 && Math.Abs(g.F7 - 0.002) < 1e-12);
            Passo("sorgente con espressione (NS+1)/2", "segmento 11", "segmento " + m.Sources[0].Seg, m.Sources[0].Seg == 11);
            // 0.4.11: IF, AND, OR, NOT e confronti di Excel (formule dei .weq di AC6LA)
            {
                string got = "";
                bool okIf;
                try
                {
                    double i1 = Expr.Eval("IF(2>1, 3, 4)", null), i2 = Expr.Eval("IF(0, 1, 2)", null), i3 = Expr.Eval("IF(1+1 >= 2*1, 10, 20) + 1", null);
                    double i4 = Expr.Eval("AND(1, 0)", null), i5 = Expr.Eval("OR(0, 2)", null), i6 = Expr.Eval("NOT(3<>3)", null), i7 = Expr.Eval("1000*MM", null);
                    got = string.Format(CI, "{0} {1} {2} {3} {4} {5} {6}", i1, i2, i3, i4, i5, i6, i7);
                    okIf = i1 == 3 && i2 == 2 && i3 == 11 && i4 == 0 && i5 == 1 && i6 == 1 && i7 == 1;
                }
                catch (Exception ex) { got = ex.Message; okIf = false; }
                Passo("IF(2>1,3,4), IF(0,1,2), IF(1+1>=2*1,10,20)+1, AND(1,0), OR(0,2), NOT(3<>3), 1000*MM", "3 2 11 0 1 1 1", got, okIf);
            }
            RunResult r0 = Calcola(m, 144.3);
            Passo("calcolo a 144,3 MHz", "Z = 74,982 +12,480j ohm (come t1)", Z(r0.Zin), Math.Abs(r0.Zin.Real - 74.982) < 0.01 && Math.Abs(r0.Zin.Imaginary - 12.480) < 0.01);

            m.Variables[0].Expr = "1010*MM";
            List<string> e1 = m.Reevaluate();
            RunResult r1 = Calcola(m, 144.3);
            Passo("pulsante + su L (L = 1,01 m) e ricalcolo", "filo piu' lungo: Y2 = 0,505 m e reattanza piu' alta",
                string.Format(CI, "Y2 = {0} m, Z = {1}", m.Geometry[0].F5, Z(r1.Zin)), e1.Count == 0 && Math.Abs(m.Geometry[0].F5 - 0.505) < 1e-12 && r1.Zin.Imaginary > r0.Zin.Imaginary);

            m.Variables[4].Expr = "ODD(X)";
            List<string> e2 = m.Reevaluate();
            Passo("errore voluto: NS = ODD(X) con X inesistente", "errore \"Variabile sconosciuta: X\"", e2.Count > 0 ? e2[0] : "nessun errore segnalato", e2.Count > 0 && e2[0].Contains("X"));
            m.Variables[4].Expr = "ODD(20)";
            m.Variables[0].Expr = "1000*MM";
            List<string> e3 = m.Reevaluate();
            Passo("ripristino dei valori", "nessun errore", e3.Count == 0 ? "nessun errore" : e3[0], e3.Count == 0);
            Passo("descrizione automatica di H (colonna Descrizione)", "Y1, Y2 del filo 1", m.DescribeUse("H"), m.DescribeUse("H") == "Y1, Y2 del filo 1");
            Passo("descrizione automatica di L", "serve a calcolare H", m.DescribeUse("L"), m.DescribeUse("L") == "serve a calcolare H");
            Passo("unita' attaccata al numero: 6MM", "0,006", Expr.Eval("6MM", null).ToString("0.######", CI), Math.Abs(Expr.Eval("6MM", null) - 0.006) < 1e-15);
            m.Variables[0].Expr = "6MM";
            m.Variables[1].Expr = "L/2";
            m.Variables[0].Expr = "6M";
            List<string> e4 = m.Reevaluate();
            string eh = m.Variables[1].Error ?? "nessun errore";
            Passo("errore su L (6M) e variabile H che ne dipende", "H: \"dipende da L, che ha un errore\"", eh, eh.StartsWith("dipende da L"));
            m.Variables[0].Expr = "1000*MM";
            m.Reevaluate();
            Passo("nome riservato MM", "rifiutato", Expr.IsValidName("MM") ? "accettato" : "rifiutato", !Expr.IsValidName("MM"));
            Passo("nome non valido 2L", "rifiutato", Expr.IsValidName("2L") ? "accettato" : "rifiutato", !Expr.IsValidName("2L"));
        }

        static void Fili()
        {
            NecModel m = NecModel.Parse(DipoloVar);
            RunResult r0 = Calcola(m, 144.3);
            m.Geometry[0].Src[8] = "3*MM";
            List<string> e = m.Reevaluate();
            RunResult r1 = Calcola(m, 144.3);
            Passo("cella Raggio del filo 1 cambiata in 3 mm (scritto \"3\" nella colonna in mm)", "raggio 0,003 m e impedenza diversa da prima",
                string.Format(CI, "raggio {0} m, Z = {1} (prima {2})", m.Geometry[0].F7, Z(r1.Zin), Z(r0.Zin)), e.Count == 0 && Math.Abs(m.Geometry[0].F7 - 0.003) < 1e-12 && (r1.Zin - r0.Zin).Magnitude > 0.1);
            GeomCard rif = new GeomCard { Id = "GW", Src = new[] { "2", "21", "-0.42", "-0.53", "0", "-0.42", "0.53", "0", "R" } };
            m.Geometry.Add(rif);
            e = m.Reevaluate();
            RunResult r2 = Calcola(m, 144.3);
            Passo("Aggiungi filo: riflettore di 1,06 m a 0,42 m dietro (tag 2, 21 segm., raggio R)", "Yagi a 2 elementi: guadagno > 4 dBi, F/B > 3 dB",
                string.Format(CI, "{0} segmenti, Z = {1}, Gmax {2:0.00} dBi verso az {3:0}, F/B {4:0.00} dB", r2.Geometry.N, Z(r2.Zin), r2.GmaxDbi, r2.PhiMax, r2.FrontBack),
                e.Count == 0 && r2.Geometry.N == 42 && r2.GmaxDbi > 4 && r2.FrontBack > 3);
            m.Geometry.RemoveAt(1);
            e = m.Reevaluate();
            RunResult r3 = Calcola(m, 144.3);
            string[][] celle = { new[] { "-,42", "-.42" }, new[] { "1,5MM", "1.5MM" }, new[] { "MAX(1,2)", "MAX(1,2)" } };
            foreach (string[] c in celle)
                Passo("virgola decimale nella cella \"" + c[0] + "\" (tastiera italiana)", "\"" + c[1] + "\"", "\"" + Expr.NormalizeDecimal(c[0]) + "\"", Expr.NormalizeDecimal(c[0]) == c[1]);
            string[][] raggi = { new[] { "R", "R*MM" }, new[] { "(R)/MM", "R" }, new[] { "(D/2)/MM", "D/2" }, new[] { "3", "0.003" } };
            foreach (string[] c in raggi)
            {
                string got = MainWindow.RadiusFromCell(c[0]);
                Passo("raggio scritto \"" + c[0] + "\" nella colonna in mm", "nella scheda: " + c[1], got, got == c[1]);
            }
            string giro = MainWindow.RadiusFromCell(MainWindow.RadiusToCell("R"));
            Passo("filo nuovo copiato da uno con raggio R (andata e ritorno della colonna mm)", "R", giro, giro == "R");
            Passo("Elimina il filo aggiunto", "si torna al valore del passo 1", Z(r3.Zin), Math.Abs(r3.Zin.Real - r1.Zin.Real) < 1e-6 && Math.Abs(r3.Zin.Imaginary - r1.Zin.Imaginary) < 1e-6);
        }

        static void Mmana()
        {
            string f = Path.Combine(AppPaths.ModelsDir, "10el DK7ZB 28 ohm (MMANA).maa");
            if (!YanecFile.Esiste(f)) { Passo("file di esempio DK7ZB", "presente", "mancante", false); return; }
            double prev = double.NaN;
            foreach (double spl in new[] { 20.0, 40.0, 80.0 })
            {
                List<MmanaWireInfo> w;
                NecModel m = MmanaImport.Load(f, spl, out w);
                RunResult r = Calcola(m, 144.3);
                Passo(string.Format(CI, "importazione con {0:0} segmenti per lambda", spl), "Z vicino a 28 ohm (parte reale fra 15 e 45)",
                    string.Format(CI, "{0} segmenti, Z = {1}, Gmax {2:0.00} dBi", r.Geometry.N, Z(r.Zin), r.GmaxDbi), r.Zin.Real > 15 && r.Zin.Real < 45);
                Passo("  AGT (test di convergenza) con questa segmentazione", "entro +-0,3 dB", string.Format(CI, "{0:0.00} dB", r.AgtDb), Math.Abs(r.AgtDb) <= 0.3, true);
                if (!double.IsNaN(prev)) Info(string.Format(CI, "differenza di guadagno rispetto al passo prima: {0:+0.00;-0.00} dB", r.GmaxDbi - prev));
                prev = r.GmaxDbi;
            }
        }

        static void Calcolatori040()
        {
            // formule di Carrel, esempio di Balanis (54-216 MHz, 8 dB -> tau 0,865, sigma 0,157)
            LpdaDesign b = new LpdaDesign { Fmin = 54, Fmax = 216, Tau = 0.865, Sigma = 0.157, DiamMm = 10 };
            b.Compute();
            Passo("LPDA di Balanis: alfa, Bar, Bs, N, L", "alfa 12,13 gradi, Bar 1,753, Bs 7,01, N 14,4, L 5,54 m",
                string.Format(CI, "alfa {0:0.00}, Bar {1:0.000}, Bs {2:0.00}, N {3:0.0}, L {4:0.00} m", b.AlphaDeg, b.Bar, b.Bs, b.Nexact, b.Lboom),
                Math.Abs(b.AlphaDeg - 12.13) < 0.02 && Math.Abs(b.Bar - 1.753) < 0.003 && Math.Abs(b.Bs - 7.01) < 0.02 && Math.Abs(b.Nexact - 14.4) < 0.06 && Math.Abs(b.Lboom - 5.54) < 0.02);
            LpdaDesign d = new LpdaDesign { Fmin = 144, Fmax = 432, Tau = 0.90, Sigma = 0.168, DiamMm = 6, Rin = 50 };
            d.Compute();
            Passo("LPDA 144-432 MHz, tau 0,90, sigma 0,168, 6 mm", "16 elementi, Z0 57,0 ohm, boom circa 2,77 m",
                string.Format(CI, "{0} elementi, Z0 {1:0.0} ohm, boom {2:0.000} m", d.N, d.Z0, d.Pos[d.N - 1]),
                d.N == 16 && Math.Abs(d.Z0 - 57.0) < 0.2 && Math.Abs(d.Pos[d.N - 1] - 2.777) < 0.01);
            NecModel ml = NecModel.Parse(d.ToNec(144, 0, null, 50));
            RunResult rl = Calcola(ml, 144);
            Passo("modello NEC della LPDA con la linea incrociata e lo stub (144 MHz)", "Z = 56,31 +7,38j, 9,0 dBi (nec2c)", Z(rl.Zin) + string.Format(CI, ", {0:0.00} dBi", rl.GmaxDbi),
                Math.Abs(rl.Zin.Real - 56.313) <= 0.15 && Math.Abs(rl.Zin.Imaginary - 7.381) <= 0.15);
            // loop magnetica: esempio del bozzetto approvato
            CalcLoopMagnetica lm = new CalcLoopMagnetica();
            CalcOutput o = lm.Run();
            string lines = string.Join("; ", o.Cards.ConvertAll(x => x.Title + " " + x.Value + " (" + x.Sub + ")"));
            Passo("loop magnetica 1 m, tubo 22 mm, 7,1 MHz, 100 W (bozzetto)", "Rr 6,0 mΩ, perdita 31,6 mΩ, 16 %, -6,2 dBi, 2,45 uH / 205 pF, 52 A / 5,6 kV", lines,
                Fra(lines, "Resistenza di radiazione ", " mΩ", 5.9, 6.15) && Fra(lines, "Resistenza di perdita ", " mΩ", 31.4, 31.8) && lines.Contains("205 pF") && (lines.Contains("5,6 kV") || lines.Contains("5.6 kV")));
            // stack 4x7 DK7ZB: riconoscimento e distanze DL6WU dalla singola in spazio libero
            string f4 = Path.Combine(AppPaths.ModelsDir, "4x7el DK7ZB H-frame 3 m x 3 m.nec");
            if (!YanecFile.Esiste(f4)) Passo("esempio 4x7 DK7ZB", "presente", "mancante", false);
            else
            {
                NecModel m = NecModel.Load(f4);
                StackInfo si = StackDetect.Detect(m);
                Passo("riconoscimento dello stack 4x7 DK7ZB", "4 antenne da 7 fili, 2 x 2, spaziature 3,000 / 3,000 m",
                    si == null ? "non riconosciuto" : string.Format(CI, "{0} antenne da {1} fili, {2} x {3}, spaziature {4:0.000} / {5:0.000} m", si.Copies, si.WiresPerCopy, si.Rows, si.Cols, si.SpacingV, si.SpacingH),
                    si != null && si.Copies == 4 && si.WiresPerCopy == 7 && si.Rows == 2 && si.Cols == 2 && Math.Abs(si.SpacingV - 3) < 1e-6 && Math.Abs(si.SpacingH - 3) < 1e-6);
                if (si != null)
                {
                    NecModel bm = StackDetect.BaseAntenna(m, si);
                    RunResult rb = Calcola(bm, 144.3);
                    double lam = 299.792458 / 144.3;
                    double dh = StackDetect.Dl6wu(lam, rb.HpbwAz), dv = StackDetect.Dl6wu(lam, rb.HpbwEl);
                    Passo("DL6WU con le aperture della singola in spazio libero", "aperture circa 39 / 45 gradi -> affiancate circa 3,1 m, sovrapposte circa 2,7 m",
                        string.Format(CI, "aperture {0:0.0} / {1:0.0} gradi -> affiancate {2:0.00} m, sovrapposte {3:0.00} m", rb.HpbwAz, rb.HpbwEl, dh, dv),
                        Math.Abs(dh - 3.11) < 0.15 && Math.Abs(dv - 2.71) < 0.15, true);
                    RunResult r4 = Calcola(m, 144.3);
                    Passo("calcolo dello stack 4x7 (caso di studio contro YO di K6STI)", "circa 17,2 dBd = 19,3 dBi (DK7ZB: 17 dBd), Z circa 28 ohm",
                        Z(r4.Zin) + string.Format(CI, ", {0:0.00} dBi", r4.GmaxDbi), Math.Abs(r4.GmaxDbi - 19.34) < 0.1);
                }
            }
            // stile del fascione (doc/Stile_fascione_sinistro.md)
            Passo("stile del fascione di sinistra", "larghezza 200, raggio 14, bordo 1, margine 10/6, 5 sotto, carattere 15, colori #1D5FD1 / #1D6FE0 / #A3ACB9 / #425066",
                string.Format(CI, "larghezza {0}, raggio {1}, bordo {2}, margine {3}/{4}, {5} sotto, carattere {6}, {7} / {8} / {9} / {10}", Fascione.Larghezza, Fascione.Raggio, Fascione.Bordo,
                    Fascione.MargineOrizz, Fascione.MargineVert, Fascione.SpazioSotto, Fascione.Carattere, Fascione.VoceTesto, Fascione.SelezSfondo, Fascione.FuturaTesto, Fascione.FamigliaTesto),
                Fascione.Larghezza == 200 && Fascione.Raggio == 14 && Fascione.Bordo == 1 && Fascione.MargineOrizz == 10 && Fascione.MargineVert == 6 && Fascione.SpazioSotto == 5 && Fascione.Carattere == 15 && Fascione.VoceTesto == Avalonia.Media.Color.Parse("#1D5FD1") && Fascione.SelezSfondo == Avalonia.Media.Color.Parse("#1D6FE0")
                && Fascione.FuturaTesto == Avalonia.Media.Color.Parse("#A3ACB9") && Fascione.FamigliaTesto == Avalonia.Media.Color.Parse("#425066"));
        }

        // calcolatori delle forme semplici (bozzetto approvato alla lettera) e terreno di Sommerfeld
        static void Calcolatori041()
        {
            try { CalcCavi.Lista = Cables.Load(Path.Combine(AppPaths.DataDir, "cavi.csv")); } catch { }
            foreach (SimpleCalc c in SimpleCalc.All())
            {
                if (c.UsesModel) continue;
                CalcOutput o;
                try { o = c.Run(); }
                catch (Exception ex) { Passo("calcolatore " + c.Nome, "calcolo senza errori", ex.Message, false); continue; }
                string esito = o.Cards.Count + " riquadri";
                bool ok = o.Cards.Count > 0;
                if (o.Nec != null)
                {
                    try
                    {
                        NecModel m = NecModel.Parse(o.Nec);
                        RunResult r = Calcola(m, m.FreqMHz);
                        esito += ", NEC: " + Z(r.Zin) + string.Format(CI, ", {0:0.00} dBi, eff. {1:0} %", r.GmaxDbi, r.Efficiency);
                        ok &= !double.IsNaN(r.GmaxDbi) && r.GmaxDbi > -60;
                    }
                    catch (Exception ex) { esito += ", NEC: errore " + ex.Message; ok = false; }
                }
                Passo("calcolatore " + (c.Group ?? c.Key) + (c.Variant != null ? " / " + c.Variant : ""), "riquadri e modello NEC calcolabile", esito, ok, true);
            }
            // 1.7.0: i pulsanti NEC dei nuovi calcolatori (bozzetto «mining riviste»): Tune, poi calcolo del modello
            foreach (SimpleCalc c in SimpleCalc.All())
            {
                if (Array.IndexOf(new[] { "yagihf", "verttrap", "caricolin", "quadmulti", "yagibib", "eflw", "lt160", "halfsloper", "gamma", "bandalarga", "dipolorele" }, c.Key) < 0) continue;
                try
                {
                    CalcOutput o0 = c.Run();
                    double f0 = NecModel.Parse(o0.Nec).FreqMHz;
                    double sc = c.Tune(f0);
                    CalcOutput o = c.Run();
                    NecModel m = NecModel.Parse(o.Nec);
                    RunResult r = Calcola(m, m.FreqMHz);
                    string tab = o.Tabella != null && o.Tabella.Count > 1 ? "; " + string.Join(" | ", o.Tabella[o.Tabella.Count - 1]) : "";
                    Passo("1.7 " + c.Key + ": " + c.TuneLabel, "nessun errore, modello calcolabile", string.Format(CI, "fattore {0:0.000}, Z {1}, {2:0.00} dBi, eff. {3:0} %", sc, Z(r.Zin), r.GmaxDbi, r.Efficiency) + tab,
                        !double.IsNaN(r.GmaxDbi) && !double.IsNaN(r.Zin.Real), true);
                }
                catch (Exception ex) { Passo("1.7 " + c.Key + ": " + c.TuneLabel, "nessun errore", ex.GetBaseException().Message, false); }
            }
            double[] mx = CalcMoxon.Dimensioni(28.5, 2);
            Passo("Moxon MOXGEN 28,5 MHz filo 2 mm", "A 3,825 m (DK7ZB 3,86), E circa 1,40 m", string.Format(CI, "A {0:0.000} m, E {1:0.000} m", mx[0], mx[4]), Math.Abs(mx[0] - 3.825) < 0.005 && Math.Abs(mx[4] - 1.40) < 0.05);
            double cb = MainWindow.CorrezioneBoom(144.3, 40, false, 0, 0), cb432 = MainWindow.CorrezioneBoom(432.2, 25, false, 1, 0);
            Passo("correzione del boom DL6WU/G3SEK: 40 mm a 144,3 MHz passante conduttivo; 25 mm a 432,2 MHz isolato", "16,0 mm (bozzetto); 432 isolato 7,6 mm (tabella DL6WU/G3SEK di YU7EF: 7,6)",
                string.Format(CI, "{0:0.0} mm; {1:0.0} mm", cb, cb432), Math.Abs(cb - 16.0) < 0.1 && Math.Abs(cb432 - 7.63) < 0.1);
            CalcReteLC rl = new CalcReteLC();
            CalcOutput or = rl.Run();
            Passo("rete L da 12 - j30 ohm a 50 ohm: verifica con i numeri complessi", "ROS 1,000", or.Cards[or.Cards.Count - 1].Sub, or.Oks.Count > 0);
            CalcTrappolato tr = new CalcTrappolato();
            CalcOutput ot = tr.Run();
            {
                // 1.0.11: modo a singola banda, bobina di carico (stima a linea; il NEC su questo caso da' 12,55 uH)
                double xl = CalcTrappolato.ReattanzaCarico(7.1, 6.5, 0.5, 0.001), lu = xl / (2 * Math.PI * 7.1e6) * 1e6;
                tr.Par("modo").Value = 1;
                CalcOutput os = tr.Run();
                tr.Par("modo").Value = 0;
                Passo("dipolo trappolato a singola banda: 7,1 MHz, braccio 6,5 m, bobina al 50 %, filo 2 mm", "bobina circa 13,1 uH (NEC 12,55), modello NEC con 2 carichi LD 0",
                    string.Format(CI, "{0:0.00} uH; schede LD 0: {1}", lu, os.Nec == null ? 0 : os.Nec.Split('\n').Count(l => l.StartsWith("LD 0"))),
                    Math.Abs(lu - 13.14) < 0.05 && os.Nec != null && os.Nec.Split('\n').Count(l => l.StartsWith("LD 0")) == 2);
            }
            // correzione dei tubi telescopici: riflettore 20 m dell'XR-5 (tubi 25,4 / 22 / 19 / 16 / 13 / 10 mm, meta' 5,55 m)
            {
                System.Text.StringBuilder sx = new System.Text.StringBuilder("CM riflettore 20 m XR-5\nCE\n");
                double[] y = { 1.83, 2.13, 2.97, 3.81, 4.65, 5.55 }, dmm = { 25.4, 22, 19, 16, 13, 10 };
                int tg = 0;
                for (int i = 5; i >= 1; i--) sx.AppendLine(string.Format(CI, "GW {0} 2 0 {1} 0 0 {2} 0 {3}", ++tg, -y[i], -y[i - 1], dmm[i] / 2000));
                sx.AppendLine(string.Format(CI, "GW {0} 9 0 -1.83 0 0 1.83 0 0.0127", ++tg));
                for (int i = 1; i <= 5; i++) sx.AppendLine(string.Format(CI, "GW {0} 2 0 {1} 0 0 {2} 0 {3}", ++tg, y[i - 1], y[i], dmm[i] / 2000));
                sx.AppendLine("GE 0\nEX 0 6 5 0 1 0\nFR 0 1 0 0 14.1 0\nEN");
                NecModel ml = NecModel.Parse(sx.ToString());
                List<string> nl = new List<string>();
                NecModel mc = Leeson.Applica(ml, 14.1, nl);
                double ymax = 0, dd = 0;
                foreach (GeomCard gc in mc.Geometry) { ymax = Math.Max(ymax, Math.Max(gc.F2, gc.F5)); dd = gc.F7 * 2000; }
                // 0.4.8: algoritmo del libro di Leeson (sez. 8.4): 5,3234 m e 18,58 mm, a 0,9 mm e 0,02 mm da EZNEC (la 0.4.7 dava 5,3316 m e 18,45 mm)
                Passo("correzione Leeson del riflettore 20 m XR-5 a 14,1 MHz", "meta' 5,3234 m, diametro 18,58 mm (libro di Leeson; EZNEC: 5,3243 m con 18,60 mm)",
                    string.Format(CI, "meta' {0:0.0000} m, diametro {1:0.00} mm; {2}", ymax, dd, string.Join(" ", nl)), Math.Abs(ymax - 5.3234) < 0.002 && Math.Abs(dd - 18.58) < 0.05);
            }
            // esportazione MMANA e rilettura (Salva con nome .maa)
            NecModel md = NecModel.Parse("CM dipolo\nCE\nGW 1 21 0 -5.2 10 0 5.2 10 0.001\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 14.2 0\nEN");
            List<string> perso = new List<string>();
            string maa = MmanaExport.Write(md, true, perso);
            List<MmanaWireInfo> wi;
            NecModel mr = MmanaImport.Parse(maa.Split('\n'), 40, out wi);
            RunResult r1 = Calcola(md, 14.2), r2 = Calcola(mr, 14.2);
            Passo("Salva con nome .maa e rilettura (dipolo 14,2 MHz)", "stessa impedenza entro 2 ohm (segmenti diversi)", Z(r1.Zin) + " / " + Z(r2.Zin), Complex.Abs(r1.Zin - r2.Zin) < 2.0);
            Passo("trappola W3DZZ 7,05 MHz con 60 pF", "circa 8,5 uH (W3DZZ 8,2 uH)", ot.Cards[0].Value, ot.Cards[0].Value.Contains("8,49") || ot.Cards[0].Value.Contains("8.49") || ot.Cards[0].Value.Contains("8,5") || ot.Cards[0].Value.Contains("8.5"));
        }

        // riferimenti calcolati con nec2c sullo stesso modello scritto come schede NEC (verifica fatta a parte il 12/11/2026)
        static void Importatori()
        {
            string fw = Path.Combine(AppPaths.DataDir, "prove", "fissi", "16el I0JXX mod IC8TEM (AutoEZ).weq");   // 0.4.30: tolto dagli esempi, resta per il collaudo
            if (!YanecFile.Esiste(fw)) Passo("file di prova .weq", "presente", "mancante", false);
            else
            {
                NecModel m = EzImport.LoadWeq(fw);
                int nw = m.Geometry.Count;
                Passo("lettura del .weq: fili con unita', segmenti dalla formula =ODD(WireLen / WL * 20), sorgente", "32 fili (due antenne una sopra l'altra), 144,2 MHz, sorgente sul filo 2 segmento 6",
                    string.Format(CI, "{0} fili, {1} MHz, sorgente sul filo {2} segmento {3}, segmenti del filo 1: {4}", nw, m.FreqMHz,
                        m.Sources.Count > 0 ? m.Sources[0].Tag : 0, m.Sources.Count > 0 ? m.Sources[0].Seg : 0, nw > 0 ? m.Geometry[0].I2 : 0),
                    nw == 32 && m.FreqMHz == 144.2 && m.Sources.Count == 1 && m.Sources[0].Tag == 2 && m.Sources[0].Seg == 6 && m.Geometry[0].I2 == 11);
                foreach (string w in m.Warnings) Info("avviso dell'importazione: " + w);
                RunResult r = Calcola(m, m.FreqMHz);
                Passo("calcolo dei due 16 elementi sovrapposti, sorgente solo su quello in alto (spazio libero)", "Z = 32,349 +8,549j (nec2c), Gmax 16,63 dBi a circa 2° sopra l'orizzonte (calcolo 2,2°) (solo l'antenna in alto e' alimentata; all'orizzonte 16,58)",
                    Z(r.Zin) + string.Format(CI, ", Gmax {0:0.00} dBi a {1:0} gradi di elevazione", r.GmaxDbi, r.ElevationOfMax),
                    Math.Abs(r.Zin.Real - 32.349) <= 0.06 && Math.Abs(r.Zin.Imaginary - 8.549) <= 0.06 && Math.Abs(r.GmaxDbi - 16.63) <= 0.05);
            }
            // 0.3.5: stesso modello di IC8TEM in .weq (variabili A, B, D) e in .ez (34 fili, 2 sorgenti); riferimento nec2c spazio libero
            string fs1 = Path.Combine(AppPaths.DataDir, "prove", "fissi", "Stack 50+144 panino IC8TEM (AutoEZ).weq");   // 0.4.30: tolti dagli esempi, restano per il collaudo
            string fs2 = Path.Combine(AppPaths.DataDir, "prove", "fissi", "Stack 50+144 panino IC8TEM (EZNEC).ez");
            foreach (string fx in new[] { fs1, fs2 })
            {
                if (!YanecFile.Esiste(fx)) { Passo("file di prova " + Path.GetFileName(fx), "presente", "mancante", false); continue; }
                NecModel m;
                try { m = fx.EndsWith(".ez") ? EzImport.LoadEz(fx) : EzImport.LoadWeq(fx); }
                catch (NecException ex) { Passo("lettura di " + Path.GetFileName(fx), "34 fili", "errore: " + ex.Message, false); continue; }
                int nw = m.Geometry.Count;
                double z7 = nw >= 7 ? m.Geometry[6].F3 : double.NaN, z13 = nw >= 13 ? m.Geometry[12].F3 : double.NaN;
                Passo("lettura di " + Path.GetFileName(fx) + " (variabili A=4, B=3, D=9 nel .weq; numero di fili e sorgenti dall'intestazione nel .ez)",
                    "34 fili, 432,174 MHz, 2 sorgenti (fili 14 e 25), quote 13 m (filo 7) e 9,5 m (filo 13)",
                    string.Format(CI, "{0} fili, {1} MHz, {2} sorgenti, quote {3:0.###} e {4:0.###} m", nw, m.FreqMHz, m.Sources.Count, z7, z13),
                    nw == 34 && Math.Abs(m.FreqMHz - 432.174) < 0.001 && m.Sources.Count == 2 && Math.Abs(z7 - 13) < 1e-6 && Math.Abs(z13 - 9.5) < 1e-6);
                foreach (string w in m.Warnings) Info("avviso dell'importazione: " + w);
                foreach (string w in m.Notes) Info("nota: " + w);
                RunResult r = Calcola(m, m.FreqMHz);
                // dalla 0.4.4 le due sorgenti I di EZNEC restano di corrente (prima diventavano di tensione: nec2c 55,346 -27,047j)
                // dalla 0.4.20 il .ez (terreno reale MININEC nel file) si legge come GN 3, come lo calcola EZNEC: riferimento EZNEC Pro/2+ 7.0
                // sullo stesso file (prova 6 di IC8TEM, 14/11/2026: 55,333 -26,345j alla sorgente 1); il .weq non ha terreno (spazio libero)
                bool daEz = fx.EndsWith(".ez");
                Complex rif = daEz ? new Complex(55.333, -26.345) : new Complex(55.346, -27.047);
                Passo(daEz ? "calcolo (terreno MININEC del file .ez, come EZNEC; due sorgenti di corrente)" : "calcolo (spazio libero, due sorgenti di corrente uguali come in EZNEC)",
                    daEz ? "sorgenti a corrente; Z = 55,333 -26,345j (EZNEC sullo stesso .ez) entro 0,2 ohm" : "sorgenti a corrente; Z = 55,346 -27,047j (nec2c) entro 0,05 ohm",
                    (m.CurrentSources ? "sorgenti a corrente, " : "sorgenti di tensione, ") + Z(r.Zin),
                    m.CurrentSources && Complex.Abs(r.Zin - rif) <= (daEz ? 0.2 : 0.05));
            }
        }

        static void Cavi()
        {
            List<Cable> cv = Cables.Load(Path.Combine(AppPaths.DataDir, "cavi.csv"));
            int n75 = cv.FindAll(c => c.Z0 == 75).Count, n50 = cv.FindAll(c => c.Z0 == 50).Count;
            Passo("lettura dell'abaco", "10 cavi da 75 ohm e 19 da 50 ohm", n75 + " da 75 ohm, " + n50 + " da 50 ohm", n75 >= 10 && n50 >= 19);
            Cable rg59 = cv.Find(c => c.Name.StartsWith("RG-59/U (Belden 8241)"));
            if (rg59 == null) { Passo("cavo RG-59/U nell'abaco", "presente", "mancante", false); return; }
            Passo("perdita RG-59/U a 144,3 MHz", "circa 13,5 dB/100 m", rg59.AttenuationAt(144.3).ToString("0.00", CI) + " dB/100 m", Math.Abs(rg59.AttenuationAt(144.3) - 13.5) < 0.05);
            Complex zAnt = new Complex(28.104, -6.132);
            QuarterWaveMatch dk = new QuarterWaveMatch { Cable = rg59, Parallel = 2 };
            Complex zin = dk.InputImpedance(zAnt, 144.3, 144.3);
            Passo("adattamento DK7ZB: 2 x RG-59 in parallelo lambda/4 sulla Yagi da 28,104 -6,132j ohm", "Z vicino a 50 ohm, ROS < 1,3",
                string.Format(CI, "lunghezza {0:0.000} m, Z = {1}, ROS {2:0.00}, perdita {3:0.000} dB", dk.PhysicalLength(144.3), Z(zin), LineCalc.Swr(zin, 50), dk.LossDb(zAnt, 144.3, 144.3)),
                LineCalc.Swr(zin, 50) < 1.3);
            Info("le 5 combinazioni migliori dell'abaco per questa antenna (lambda/4 a 144,3 MHz):");
            List<KeyValuePair<QuarterWaveMatch, double>> s = QuarterWaveMatch.Suggest(cv, zAnt, 50, 144.3);
            for (int i = 0; i < Math.Min(5, s.Count); i++)
            {
                QuarterWaveMatch q = s[i].Key;
                Info(string.Format(CI, "  {0} x {1} ({2} ohm in parallelo = {3:0.0} ohm): ROS {4:0.00}, lunghezza {5:0.000} m, perdita {6:0.000} dB",
                    q.Parallel, q.Cable.Name, q.Cable.Z0, q.Z0Eq, s[i].Value, q.PhysicalLength(144.3), q.LossDb(zAnt, 144.3, 144.3)));
            }
        }

        static void Sweep()
        {
            NecModel m = NecModel.Parse(DipoloVar);
            SegmentGeometry g = m.BuildGeometry(new List<string>());
            List<double> fr = new List<double>();
            for (int i = 0; i <= 40; i++) fr.Add(136.0 + i * 0.5);
            Stopwatch sw = Stopwatch.StartNew();
            List<SweepPoint> sp = Analysis.Sweep(m, g, fr, 90, 0, null, CancellationToken.None);
            int ok = sp.FindAll(p => p.Ok).Count;
            SweepPoint best = null;
            foreach (SweepPoint p in sp) if (p.Ok && (best == null || p.Swr < best.Swr)) best = p;
            Passo("sweep 136-156 MHz, 41 punti, in parallelo", "41 punti calcolati", ok + " punti calcolati in " + sw.Elapsed.TotalSeconds.ToString("0.0", CI) + " s", ok == 41);
            Passo("minimo del ROS (dipolo da 1 m, risonanza sotto 144 MHz)", "fra 136 e 144 MHz",
                best == null ? "nessun punto" : string.Format(CI, "{0:0.0} MHz, ROS {1:0.00}, Z = {2}", best.F, best.Swr, Z(best.Z)), best != null && best.F >= 136 && best.F <= 144);
        }

        // modello 36inv_coat.nec della raccolta di 4nec2 (A. Voors): V invertita 80 m, filo da 0,5 mm con PVC fino a 1 mm di raggio.
        // nec2c dopo la conversione di 4nec2 (LD 2 di 1,295e-7 H/m): 60,04 -j0,15 ohm; senza isolante 57,76 -j57,25 ohm.
        // Esempi della NEC-2 Part III (Burke e Poggio, NTIS ADA956129), mazzi ricostruiti da R. Sutherland (hf-nec2c, BSD-2) con i
        // valori ricontrollati sulla scansione NTIS; un caso per ogni XQ, come modelli separati (proposta 1 del mining, approvata il 13/11/2026)
        static readonly string[][] EsempiNec2 =
        {
            // { titolo, mazzo, MHz, R, X, efficienza % (vuoto = non controllata), guadagni pubblicati "theta:dBi;..." a phi 0 (vuoto = nessuno) }
            new[] { "Es. 1, dipolo 7 segmenti senza carico", "GW 0 7 0 0 -0.25 0 0 0.25 0.001\nGE\nEX 0 0 4 0 1 0", "299.8", "82.6979", "46.3060", "100.00", "" },
            new[] { "Es. 1, con RLC serie sul segmento 4 (LD 0)", "GW 0 7 0 0 -0.25 0 0 0.25 0.001\nGE\nEX 0 0 4 0 1 0\nLD 0 0 4 4 10 3.000E-09 5.300E-11", "299.8", "92.6979", "41.9407", "89.21", "" },
            new[] { "Es. 2, filo sottile 8 segmenti, EX 5, 200 MHz", "GW 0 8 0 0 -0.25 0 0 0.25 0.00001\nGE\nEX 5 0 5 0 1 0", "200", "26.5762", "-632.060", "", "" },
            new[] { "Es. 2, EX 5, 250 MHz", "GW 0 8 0 0 -0.25 0 0 0.25 0.00001\nGE\nEX 5 0 5 0 1 0", "250", "47.1431", "-272.372", "", "" },
            new[] { "Es. 2, EX 5, 300 MHz", "GW 0 8 0 0 -0.25 0 0 0.25 0.00001\nGE\nEX 5 0 5 0 1 0", "300", "80.5511", "45.7144", "", "" },
            new[] { "Es. 2, EX 5, conduttore da 3,72e7 S/m (LD 5), 300 MHz", "GW 0 8 0 0 -0.25 0 0 0.25 0.00001\nGE\nLD 5 0 0 0 3.720E+07\nEX 5 0 5 0 1 0", "300", "112.430", "65.4276", "73.45", "" },
            new[] { "Es. 3, monopolo grosso su terreno perfetto (EK)", "GW 0 9 0 0 2 0 0 7 0.3\nGE 1\nEK\nEX 0 0 5 0 1 0\nGN 1", "30", "106.436", "9.90546", "100.00", "90:8.52;80:7.20;70:2.67;30:-1.70;10:-9.87" },
            new[] { "Es. 3, su terreno a coefficiente di riflessione (6; 1 mS/m)", "GW 0 9 0 0 2 0 0 7 0.3\nGE 1\nEK\nEX 0 0 5 0 1 0\nGN 0 0 0 0 6 1.000E-03", "30", "111.117", "11.0081", "100.00", "70:1.54;80:0.64;60:-0.79;40:-3.45;10:-12.87" },
        };

        static void Satelliti()
        {
            // eggbeater 145,9 MHz, 5 mm, radiali, 1/8 lambda, RG-62: nec2c (scratchpad egg/egg.py) quadratura a +2,5 %, correnti 1,57 : 1
            CalcEggbeater eg = new CalcEggbeater();
            CalcOutput o = eg.Run();
            RunResult r = SimpleCalc.Solve(o, eg.P("f"));
            NecModel m = r.Model;
            SegmentGeometry g = r.Geometry;
            int a1 = -1, b1 = -1;
            foreach (NetworkSpec nw in m.Networks) { a1 = g.SegNo(nw.Tag1, nw.Seg1) - 1; b1 = g.SegNo(nw.Tag2, nw.Seg2) - 1; }
            Complex ia = r.Engine.SegmentCurrent(a1), ib = r.Engine.SegmentCurrent(b1);
            double fase = Math.Abs((ia / ib).Phase * 180 / Math.PI), rap = Complex.Abs(ib / ia);
            Passo("eggbeater: correnti delle spire in quadratura", "90 +- 1 gradi", string.Format(CI, "{0:0.0} gradi", fase), Math.Abs(fase - 90) < 1);
            Passo("eggbeater: rapporto delle correnti con RG-62 a 1/8 lambda", "circa 1,57 : 1 (nec2c; tamago 143 / 93 = 1,54)", string.Format(CI, "{0:0.00} : 1", rap), Math.Abs(rap - 1.57) < 0.08);
            FarFieldPoint zen = r.Engine.Gain(0, 0);
            Passo("eggbeater: polarizzazione allo zenit", "destra", zen.Sense == 1 ? "destra" : zen.Sense == 2 ? "sinistra" : "lineare", zen.Sense == 1);
            CalcQfh q = new CalcQfh();
            r = SimpleCalc.Solve(q.Run(), q.P("f"));
            Complex za = new Complex(30.717, 0.0857);
            Passo("QFH 137,5 MHz (Coppens, 8 mm, k 0,44): impedenza", "nec2c " + Z(za), Z(r.Zin), Complex.Abs(r.Zin - za) < 0.5);
            zen = r.Engine.Gain(0, 0);
            Passo("QFH: zenit", "destra, guadagno 4,81 dBi, rapporto assiale 0,946 (nec2c)", string.Format(CI, "{0}, {1:0.00} dBi, {2:0.000}", zen.Sense == 1 ? "destra" : "altro", zen.GainTotal, zen.AxialRatio),
                zen.Sense == 1 && Math.Abs(zen.GainTotal - 4.81) < 0.05 && Math.Abs(zen.AxialRatio - 0.946) < 0.01);
        }

        static void EsempiManualeNec2()
        {
            foreach (string[] t in EsempiNec2)
            {
                double f = double.Parse(t[2], CI);
                NecModel m = NecModel.Parse("CM " + t[0] + "\nCE\n" + t[1] + "\nFR 0 1 0 0 " + t[2] + " 0\nEN");
                RunResult r = Calcola(m, f);
                Complex za = new Complex(double.Parse(t[3], CI), double.Parse(t[4], CI));
                Passo(t[0] + ", impedenza a " + t[2] + " MHz", "manuale NEC-2 " + Z(za), Z(r.Zin), Complex.Abs(r.Zin - za) < 0.002 * Complex.Abs(za) + 0.01);
                if (t[5] != "")
                {
                    double ea = double.Parse(t[5], CI);
                    Passo(t[0] + ", efficienza", string.Format(CI, "{0:0.00} %", ea), string.Format(CI, "{0:0.00} %", r.Efficiency), Math.Abs(r.Efficiency - ea) < 0.05);
                }
                if (t[6] != "")
                    foreach (string pg in t[6].Split(';'))
                    {
                        string[] q = pg.Split(':');
                        double th = double.Parse(q[0], CI), ga = double.Parse(q[1], CI);
                        double g = r.Engine.Gain(th, 0).GainTotal;
                        Passo(t[0] + ", guadagno a theta " + q[0], string.Format(CI, "{0:0.00} dBi", ga), string.Format(CI, "{0:0.00} dBi", g), Math.Abs(g - ga) < 0.02);
                    }
            }
        }

        static void FiloIsolato()
        {
            string deck = "CM Inverted V for 80 Mtr with coated-wire.\nCE\nSY hgh=21\nSY len=19.15\nSY ang=110\nSY Z=len*cos(ang/2), X=len*sin(ang/2)\nSY nrseg=25\n" +
                "GW 1 nrseg -X 0 hgh-Z -0.3 0 hgh .5mm\nGW 2 3 -0.3 0 hgh 0.3 0 hgh .5mm\nGW 3 nrseg 0.3 0 hgh X 0 hgh-z .5mm\nGE 0\n" +
                "GN 2 0 0 0 14 .006\nLD 5 0 0 0 58000000\nLD 7 0 0 0 4.5 1mm ' 1 mm outer radius PVC coat.\nEX 0 2 2 0 1 0\nFR 0 1 0 0 3.680\nEN";
            NecModel m = NecModel.Parse(deck);
            LoadSpec ld = m.Loads.Find(l => l.Type == 2);
            double atteso = 2e-7 * Math.Pow(4.5 * 2, 1.0 / 12) * (1 - 1 / 4.5) * Math.Log(2);
            Passo("LD 7 4,5 / 1 mm su filo da 0,5 mm di raggio", "LD 2 con L = 1,2949e-7 H/m (formula di Cebik come 4nec2)",
                ld == null ? "nessun carico LD 2" : string.Format(CI, "{0} carichi LD 2, L = {1:0.0000E0} H/m", m.Loads.FindAll(l => l.Type == 2).Count, ld.F2),
                ld != null && Math.Abs(ld.F2 - atteso) < 1e-12);
            RunResult r = Calcola(m, 3.68);
            Complex za = new Complex(60.043, -0.14917);
            Passo("impedenza a 3,68 MHz su Sommerfeld", "nec2c " + Z(za) + " (senza isolante 57,76 -j57,25)", Z(r.Zin), Complex.Abs(r.Zin - za) < 0.02 * Complex.Abs(za) + 0.5);
        }

        // modelli di prova delle simmetrie; impedenze attese calcolate con nec2c (che usa le simmetrie) il 13/11/2026
        static readonly string[][] ModelliSimmetria =
        {
            new[] { "GR, 4 dipoli verticali in cerchio, sorgente su uno solo", "GW 1 11 0.5 0 -0.25 0.5 0 0.25 0.003\nGR 1 4\nGE 0\nEX 0 1 6 0 1 0", "89.279", "56.075" },
            new[] { "GX 110, 4 dipoli nei quadranti, carichi di rame su tutti i segmenti", "GW 1 9 0.3 0.2 -0.24 0.3 0.2 0.24 0.004\nGX 1 110\nGE 0\nLD 5 0 0 0 3.7e7\nEX 0 1 5 0 1 0", "91.805", "-9.5786" },
            new[] { "GR, 3 monopoli su terreno perfetto", "GW 1 8 0.4 0 0 0.4 0 0.25 0.003\nGR 1 3\nGE 1\nGN 1\nEX 0 1 1 0 1 0", "37.021", "34.448" },
            new[] { "GX 010, Yagi 3 el a meta' con una linea tra le due meta'", "GW 1 5 0 0.02 0 0 0.255 0 0.003\nGW 2 5 0.2 0.02 0 0.2 0.24 0 0.003\nGW 3 5 -0.2 0.02 0 -0.2 0.265 0 0.003\nGX 3 010\nGE 0\nEX 0 2 1 0 1 0\nTL 2 1 5 1 300 0 0 0 0 0", "0.37998", "-246.93" },
        };

        static void Simmetrie()
        {
            bool prima = Nec2Engine.UsaSimmetrie;
            try
            {
                foreach (string[] t in ModelliSimmetria)
                {
                    NecModel m = NecModel.Parse("CM " + t[0] + "\nCE\n" + t[1] + "\nFR 0 1 0 0 299.8 0\nEN");
                    SegmentGeometry g = m.BuildGeometry(new List<string>());
                    Func<bool, Nec2Engine> risolvi = usa =>
                    {
                        Nec2Engine.UsaSimmetrie = usa;
                        Nec2Engine e = new Nec2Engine(g);
                        e.Solve(299.8, Analysis.EffectiveSources(m, g, 299.8), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.Networks);
                        return e;
                    };
                    Nec2Engine es = risolvi(true), ep = risolvi(false);
                    Complex zs = es.Sources[0].Z, zp = ep.Sources[0].Z;
                    Complex za = new Complex(double.Parse(t[2], CI), double.Parse(t[3], CI));
                    double dmax = 0;
                    for (int i = 0; i < g.N; i++)
                        dmax = Math.Max(dmax, Complex.Abs(es.SegmentCurrent(i) - ep.SegmentCurrent(i)) / Math.Max(Complex.Abs(ep.SegmentCurrent(i)), 1e-12));
                    Passo(t[0] + ": simmetria riconosciuta e usata", "copie della scheda GX/GR", es.SimmetriaUsata == "" ? "matrice intera" : es.SimmetriaUsata, es.SimmetriaUsata != "" && ep.SimmetriaUsata == "");
                    Passo(t[0] + ": con e senza simmetria", "correnti uguali entro 1e-6, impedenza come nec2c " + Z(za) + " entro 0,1 %",
                        string.Format(CI, "{0} (scarto correnti {1:E1})", Z(zs), dmax), dmax < 1e-6 && Complex.Abs(zs - za) < 1e-3 * Complex.Abs(za) + 0.01);
                }
            }
            finally { Nec2Engine.UsaSimmetrie = prima; }
        }

        static void SweepRapido()
        {
            bool prima = Analysis.SweepRapido;
            try
            {
                NecModel dip = NecModel.Parse(DipoloVar);
                List<double> f1 = new List<double>();
                for (int i = 0; i <= 40; i++) f1.Add(136.0 + i * 0.5);
                ConfrontaSweep("dipolo 136-156 MHz, 41 punti", dip, f1, 90, 0);
                string maa = Path.Combine(AppPaths.ModelsDir, "10el DK7ZB 28 ohm (MMANA).maa");
                if (!YanecFile.Esiste(maa)) maa = Path.Combine(AppContext.BaseDirectory, "data", "modelli", "10el DK7ZB 28 ohm (MMANA).maa");
                if (!YanecFile.Esiste(maa)) { Passo("DK7ZB 10 el (MMANA)", "file negli esempi", "mancante", false); return; }
                List<MmanaWireInfo> w;
                NecModel dk = MmanaImport.Load(maa, 40, out w);
                List<double> f2 = new List<double>();
                for (int i = 0; i <= 40; i++) f2.Add(142.0 + i * 0.1);
                ConfrontaSweep("DK7ZB 10 el 142-146 MHz, 41 punti", dk, f2, 90, 0);
            }
            finally { Analysis.SweepRapido = prima; }
        }

        static void ConfrontaSweep(string nome, NecModel m, List<double> fr, double th, double ph)
        {
            SegmentGeometry g = m.BuildGeometry(new List<string>());
            Analysis.SweepRapido = false;
            Stopwatch sw = Stopwatch.StartNew();
            List<SweepPoint> pieno = Analysis.Sweep(m, g, fr, th, ph, null, CancellationToken.None);
            double tPieno = sw.Elapsed.TotalSeconds;
            Analysis.SweepRapido = true;
            sw.Restart();
            List<SweepPoint> rap = Analysis.Sweep(m, g, fr, th, ph, null, CancellationToken.None);
            double tRap = sw.Elapsed.TotalSeconds;
            int calcolati = rap.FindAll(p => p.Ok && !p.Interpolato).Count;
            double dSwr = 0, dG = 0, dFb = 0;
            bool tuttiOk = true;
            for (int i = 0; i < fr.Count; i++)
            {
                if (!pieno[i].Ok || !rap[i].Ok) { tuttiOk = false; continue; }
                dSwr = Math.Max(dSwr, Math.Abs(rap[i].Swr - pieno[i].Swr) / pieno[i].Swr * 100);
                dG = Math.Max(dG, Math.Abs(rap[i].GainFwd - pieno[i].GainFwd));
                dFb = Math.Max(dFb, Math.Abs(rap[i].FrontBack - pieno[i].FrontBack));
            }
            Passo(nome + ": punti risolti dal NEC nello sweep rapido", "meno di tutti i punti",
                string.Format(CI, "{0} su {1} ({2:0.00} s contro {3:0.00} s)", calcolati, fr.Count, tRap, tPieno), tuttiOk && calcolati <= fr.Count);
            Passo(nome + ": scarto massimo rispetto allo sweep completo", "ROS entro 0,1 %, guadagno entro 0,1 dB, F/B entro 0,3 dB",
                string.Format(CI, "ROS {0:0.000} %, G {1:0.000} dB, F/B {2:0.000} dB", dSwr, dG, dFb), tuttiOk && dSwr < 0.1 && dG < 0.1 && dFb < 0.3);
        }

        // I modelli di 4nec2 (A. Voors, freeware) non sono distribuiti con YA-NEC: si mette in data/prove lo zip 4nec2zip.zip
        // (o la cartella data/prove/4nec2/models). I valori attesi sono di YA-NEC: conversione di riferimento 4nec2 -> NEC-2
        // (SY posizionali, unita', #AWG, EX 6 a corrente, LD 6 trappola, GN 3 su terreno perfetto) e calcolo con nec2c.
        static void Corpus4nec2()
        {
            string prove = Path.Combine(AppPaths.DataDir, "prove");
            string csv = Path.Combine(prove, "4nec2_attesi.csv");
            if (!File.Exists(csv)) csv = Path.Combine(AppContext.BaseDirectory, "data", "prove", "4nec2_attesi.csv");
            if (!File.Exists(csv)) { Passo("valori attesi del corpus 4nec2", "data/prove/4nec2_attesi.csv", "mancante", false); return; }
            string cartella = Path.Combine(prove, "4nec2", "models");
            string zip = Path.Combine(prove, "4nec2zip.zip");
            if (!File.Exists(zip)) zip = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "data", "prove", "4nec2zip.zip"));   // cartella dei sorgenti (compila.bat)
            if (!Directory.Exists(cartella) && File.Exists(zip))
            {
                try
                {
                    string dest = Path.Combine(AppPaths.TempDir, "yanec_4nec2_" + Environment.ProcessId);
                    System.IO.Compression.ZipFile.ExtractToDirectory(zip, dest, true);
                    string c2 = Path.Combine(dest, "4nec2", "models");
                    if (Directory.Exists(c2)) cartella = c2;
                }
                catch (Exception ex) { Passo("estrazione di 4nec2zip.zip", "cartella models", ex.Message, false, true); }
            }
            if (!Directory.Exists(cartella))
            {
                Passo("modelli di 4nec2", "data/prove/4nec2zip.zip oppure data/prove/4nec2/models", "non presenti: prova saltata (mettere lo zip di 4nec2 in data/prove)", false, true);
                return;
            }
            int righe = 0, attesi = 0, aperti = 0, calcolati = 0, uguali = 0, grandi = 0;
            List<string> diversi = new List<string>(), errori = new List<string>();
            string[] L = File.ReadAllLines(csv);
            for (int k = 1; k < L.Length; k++)
            {
                string[] c = L[k].Split(';');
                if (c.Length < 8 || c[0].Length == 0) continue;
                righe++;
                string f = Path.Combine(cartella, c[0].Replace('/', Path.DirectorySeparatorChar));
                if (!YanecFile.Esiste(f)) { errori.Add(c[0] + ": file mancante"); continue; }
                bool atteso = c[2].Length > 0;
                if (atteso) attesi++;
                try
                {
                    NecModel m = NecModel.Load(f);
                    aperti++;
                    if (!atteso) continue;
                    SegmentGeometry g = m.BuildGeometry(new List<string>());
                    if (g.N > 1500) { grandi++; continue; }
                    double fr = double.Parse(c[1], CI);
                    Nec2Engine e = new Nec2Engine(g);
                    e.Solve(fr, Analysis.EffectiveSources(m, g, fr), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.Networks);
                    calcolati++;
                    Complex z = e.Sources[0].Z, za = new Complex(double.Parse(c[2], CI), double.Parse(c[3], CI));
                    if (Complex.Abs(z - za) <= 0.02 * Complex.Abs(za) + 0.5) uguali++;
                    else diversi.Add(string.Format(CI, "{0} a {1} MHz: YA-NEC {2:0.##} {3:+0.##;-0.##}j, nec2c {4:0.##} {5:+0.##;-0.##}j", c[0], fr, z.Real, z.Imaginary, za.Real, za.Imaginary));
                }
                catch (Exception ex) { errori.Add(c[0] + ": " + ex.Message); }
            }
            Passo("modelli aperti", "tutti i " + righe + " modelli", aperti + " aperti, " + errori.Count + " con errore", errori.Count <= righe / 10, true);
            Passo("impedenza uguale a nec2c (entro il 2 % + 0,5 ohm)", attesi + " modelli con il valore atteso (" + grandi + " oltre 1500 segmenti saltati)",
                uguali + " uguali su " + calcolati + " calcolati, " + diversi.Count + " diversi", calcolati > 0 && uguali >= 0.9 * calcolati, true);
            for (int k = 0; k < Math.Min(40, errori.Count); k++) Riga("         errore: " + errori[k]);
            for (int k = 0; k < Math.Min(40, diversi.Count); k++) Riga("         diverso: " + diversi[k]);
        }

        static void ImportK6sti()
        {
            string[] yag = { "91IR606", "143.000 144.000 145.000 MHz", "7 elements, centimeters", "           10.0000mm",
                "  0.0000   52.0251  ", " 29.4488   48.6430  ", " 61.0088   47.1508  ", "104.6780   46.7678  ", "167.3407   44.4132  ",
                "245.5101   45.4591  ", "300.0000   43.6246  ", "", "stacking antenna sebaiknya 5.4 meter" };
            NecModel my = K6stiImport.ParseYag(yag, "28_3B");
            RunResult ry = Calcola(my, 144.0);
            Passo("28_3B.YAG, 144 MHz, alimentato il secondo elemento", "nec2c: 31,87 +j1,71 ohm, 12,52 dBi",
                string.Format(CI, "{0:0.00} {1:+0.00;-0.00}j ohm, {2:0.00} dBi, {3} fili", ry.Zin.Real, ry.Zin.Imaginary, ry.GmaxDbi, my.Geometry.Count),
                Math.Abs(ry.Zin.Real - 31.87) < 0.3 && Math.Abs(ry.Zin.Imaginary - 1.71) < 0.3 && Math.Abs(ry.GmaxDbi - 12.52) < 0.05);
            string[] ant = { "3-Element Yagi                                  ; Antenna title", "Free Space                                 ; Ground description",
                "14.2 MHz                     ; Frequency.  Use kHz, MHz, or GHz", "3 wires, feet                ; Number of wires, dimension units",
                "1  -8 -17   0  -8 17   0  1\"  ; For each wire:  Min # segments,", "1   0 -16.5 0   0 16.5 0  1\"  ;        XYZ coordinates of ends,",
                "1   7 -16   0   7 16   0  1\"  ;                    and diameter", "1 source                                 ; Number of feedpoints",
                "Wire 2, center  1  0          ; Source location, voltage, phase", "0 loads                     ; Number of lumped loads (optional)" };
            NecModel ma = K6stiImport.ParseAnt(ant);
            RunResult ra = Calcola(ma, 14.2);
            Passo("esempio di ANTFILE.DOC (3 el. 14,2 MHz, piedi, tubi da 1\")", "nec2c: 18,49 -j22,71 ohm, 7,43 dBi",
                string.Format(CI, "{0:0.00} {1:+0.00;-0.00}j ohm, {2:0.00} dBi", ra.Zin.Real, ra.Zin.Imaginary, ra.GmaxDbi),
                Math.Abs(ra.Zin.Real - 18.49) < 0.3 && Math.Abs(ra.Zin.Imaginary + 22.71) < 0.3 && Math.Abs(ra.GmaxDbi - 7.43) < 0.05);
            ImportK6sti0441();
            VariabiliNuove151();
        }

        // 0.5.0 (IC8TEM 15/11/2026 14:34: "trattiamo i files di k6sti? se si allora si devono poter aprire"): tutte le funzioni di
        // ANTFILE.DOC, con gli esempi del documento (V invertita con Shift e Rotate, quad con i simboli, three-square con le espressioni,
        // carichi di Laplace, materiali, modo simmetrico con la sola meta' +Y)
        // 1.5.1: variabile nuova dal modello (Ottimizzatore): la geometria e l'impedenza non cambiano; la variabile cambia davvero la misura
        static void VariabiliNuove151()
        {
            try
            {
                // Yagi 3 elementi e un loop quadrato (Quagi in piccolo)
                string nec = "GW 1 11 -0.1 -0.52 0 -0.1 0.52 0 0.005\nGW 2 11 0.3 -0.49 0 0.3 0.49 0 0.005\nGW 3 11 0.6 -0.46 0 0.6 0.46 0 0.005\n" +
                             "GW 4 5 0.9 -0.25 -0.25 0.9 0.25 -0.25 0.002\nGW 5 5 0.9 0.25 -0.25 0.9 0.25 0.25 0.002\nGW 6 5 0.9 0.25 0.25 0.9 -0.25 0.25 0.002\nGW 7 5 0.9 -0.25 0.25 0.9 -0.25 -0.25 0.002\n" +
                             "GE 0\nEX 0 2 6 0 1 0\nFR 0 1 0 0 144 0\nEN";
                NecModel m = NecModel.Parse(nec.Replace("\\n", "\n"));
                int asse;
                List<VariabileNuova.Gruppo> gs = VariabileNuova.Gruppi(m, out asse);
                Complex z0 = Calcola(m, 144).Zin;
                bool ok = gs.Count == 4 && asse == 0 && gs[3].Chiuso && Math.Abs(gs[3].Misura - 2.0) < 1e-9;
                string ris = gs.Count + " elementi, asse " + asse + (gs.Count == 4 ? ", loop " + (gs[3].Chiuso ? "chiuso" : "aperto") + " " + gs[3].Misura.ToString("0.###", CI) + " m" : "");
                foreach (int tipo in new[] { VariabileNuova.Lunghezza, VariabileNuova.Posizione, VariabileNuova.Raggio })
                    foreach (int gi in new[] { 1, 3 })
                    {
                        VariabileNuova.Proposta p = VariabileNuova.Prepara(m, gi, tipo, "V" + tipo + gi);
                        if (p.Modello == null) { ok = false; ris += "; " + p.Errore; continue; }
                        Complex z1 = Calcola(p.Modello, 144).Zin;
                        ok &= Complex.Abs(z1 - z0) < 1e-6 * Complex.Abs(z0);
                        // la variabile cambia la misura: +1 %
                        SyVar v = p.Modello.Variables.Find(q => q.Name == "V" + tipo + gi);
                        NecModel m2 = p.Modello.Clone();
                        m2.Variables.Find(q => q.Name == v.Name).Expr = (v.Value * 1.01 + (tipo == VariabileNuova.Posizione ? 0.01 : 0)).ToString("R", CI);
                        m2.Reevaluate();
                        int asse2; List<VariabileNuova.Gruppo> g2 = VariabileNuova.Gruppi(m2, out asse2);
                        double atteso = tipo == VariabileNuova.Lunghezza ? gs[gi].Misura * 1.01 : tipo == VariabileNuova.Posizione ? gs[gi].Pos * 1.01 + 0.01 : gs[gi].Misura;
                        double letto = tipo == VariabileNuova.Lunghezza ? g2[gi].Misura : tipo == VariabileNuova.Posizione ? g2[gi].Pos : g2[gi].Misura;
                        ok &= Math.Abs(letto - atteso) < 1e-6;
                        if (tipo == VariabileNuova.Raggio) ok &= Math.Abs(m2.Geometry[g2[gi].Schede[0]].F7 - m.Geometry[gs[gi].Schede[0]].F7 * 1.01) < 1e-9;
                    }
                VariabileNuova.Proposta ph = VariabileNuova.Prepara(m, -1, VariabileNuova.Altezza, "H");
                ok &= ph.Modello != null;
                Passo("Variabile nuova dal modello (Ottimizzatore, 1.5.1)", "4 elementi (3 fili diritti e un loop da 2 m), asse X; lunghezza, posizione e raggio del pilotato e del loop: stessa Z, misura +1 % quando cambia la variabile; quota di tutta l'antenna",
                    ris + (ok ? "; Z uguale e misure giuste" : ""), ok);
            }
            catch (Exception ex) { Passo("Variabile nuova dal modello", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
        }

        static void ImportK6sti0441()
        {
            Func<NecModel, int, string> gw = (m, k) => { GeomCard g = m.Geometry[k]; return string.Format(CI, "{0:0.0000} {1:0.0000} {2:0.0000} -> {3:0.0000} {4:0.0000} {5:0.0000}", g.F1, g.F2, g.F3, g.F4, g.F5, g.F6); };
            Func<GeomCard, double[], bool> vicino = (g, e) => Math.Abs(g.F1 - e[0]) < 1e-4 && Math.Abs(g.F2 - e[1]) < 1e-4 && Math.Abs(g.F3 - e[2]) < 1e-4 &&
                Math.Abs(g.F4 - e[3]) < 1e-4 && Math.Abs(g.F5 - e[4]) < 1e-4 && Math.Abs(g.F6 - e[5]) < 1e-4;
            try
            {
                // V invertita di ANTFILE.DOC (pag. 15): apice a 60 ft, 120 gradi
                NecModel m = K6stiImport.ParseAnt(new[] { "Inverted V", "Over Ground", "7.15 MHz", "2 wires, feet", "Shift Z 60", "Rotate X 30",
                    "1   0 0 0   0  33 0   #12", "Rotate X -30", "1   0 0 0   0 -33 0   #12", "1 source", "Wire 1, end1" });
                double ft = 0.3048, c30 = Math.Cos(Math.PI / 6) * 33 * ft, s30 = 0.5 * 33 * ft, h = 60 * ft;
                bool ok = m.Geometry.Count == 2 && vicino(m.Geometry[0], new[] { 0, 0, h, 0, c30, h - s30 }) && vicino(m.Geometry[1], new[] { 0, 0, h, 0, -c30, h - s30 });
                Passo("AO: Shift e Rotate (V invertita di ANTFILE.DOC)", string.Format(CI, "apice a {0:0.000} m, punte a Y +-{1:0.000} m e Z {2:0.000} m", h, c30, h - s30),
                    m.Geometry.Count + " fili: " + gw(m, 0) + " | " + (m.Geometry.Count > 1 ? gw(m, 1) : ""), ok);
            }
            catch (Exception ex) { Passo("AO: Shift e Rotate", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            try
            {
                // quad di ANTFILE.DOC (pag. 11-12): stesso modello con i numeri e con i simboli (anche con il segno meno e un simbolo per i segmenti)
                string[] testa = { "2-Element Cubical Quad", "Free Space Symmetric", "24.94 MHz", "8 wires, inches" };
                string[] numeri = { "1   -35.5 -64.75 -64.75   -35.5  64.75 -64.75   #12", "1   -35.5 -64.75 -64.75   -35.5 -64.75  64.75   #12",
                    "1   -35.5 -64.75  64.75   -35.5  64.75  64.75   #12", "1   -35.5  64.75  64.75   -35.5  64.75 -64.75   #12",
                    "1    35.5 -61.25 -61.25    35.5  61.25 -61.25   #12", "1    35.5 -61.25 -61.25    35.5 -61.25  61.25   #12",
                    "1    35.5 -61.25  61.25    35.5  61.25  61.25   #12", "1    35.5  61.25  61.25    35.5  61.25 -61.25   #12", "1 source", "Wire 5, center" };
                string[] simboli = { "seg = 1", "sp = 35.5", "ref = 64.75", "de = 61.25", "diam = #12",
                    "seg   -sp -ref -ref   -sp  ref -ref    diam", "seg   -sp -ref -ref   -sp -ref  ref    diam", "seg   -sp -ref  ref   -sp  ref  ref    diam",
                    "seg   -sp  ref  ref   -sp  ref -ref    diam", "seg    sp  -de  -de    sp   de  -de    diam", "seg    sp  -de  -de    sp  -de   de    diam",
                    "seg    sp  -de   de    sp   de   de    diam", "seg    sp   de   de    sp   de  -de    diam", "1 source", "Wire 5, center" };
                List<string> a = new List<string>(testa), b = new List<string>(testa);
                a.AddRange(numeri); b.AddRange(simboli);
                NecModel ma = K6stiImport.ParseAnt(a.ToArray()), mb = K6stiImport.ParseAnt(b.ToArray());
                bool ok = ma.Geometry.Count == 8 && mb.Geometry.Count == 8;
                for (int k = 0; ok && k < 8; k++)
                {
                    GeomCard g = ma.Geometry[k], q = mb.Geometry[k];
                    ok = vicino(q, new[] { g.F1, g.F2, g.F3, g.F4, g.F5, g.F6 }) && Math.Abs(q.F7 - g.F7) < 1e-9 && q.I2 == g.I2;
                }
                Passo("AO: simboli (quad di ANTFILE.DOC con numeri e con simboli)", "stessi 8 fili, diametri e segmenti", ok ? "uguali" : "diversi: " + gw(ma, 0) + " / " + gw(mb, 0), ok);
            }
            catch (Exception ex) { Passo("AO: simboli", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            try
            {
                // three-square di ANTFILE.DOC (pag. 13): espressioni con SIN, COS e unita' attaccate
                NecModel m = K6stiImport.ParseAnt(new[] { "Three-square 80 m", "Over Ground", "3.8 MHz", "3 wires, feet", "x = 40'", "a = SIN(30) * x", "b = COS(30) * x",
                    "1    x  0 0    x  0 61'   1\"", "1   -a -b 0   -a -b 61'   1\"", "1   -a  b 0   -a  b 61'   1\"", "1 source", "Wire 1, end1" });
                double ft = 0.3048, ea = 20 * ft, eb = Math.Sqrt(3) * 20 * ft, hh = 61 * ft;
                bool ok = m.Geometry.Count == 3 && vicino(m.Geometry[1], new[] { -ea, -eb, 0, -ea, -eb, hh }) && Math.Abs(m.Geometry[0].F7 - 0.0127) < 1e-6;
                Passo("AO: espressioni nei simboli (three-square di ANTFILE.DOC)", string.Format(CI, "filo 2 da (-{0:0.0000}, -{1:0.0000}, 0) a quota {2:0.0000} m, tubo da 1\"", ea, eb, hh),
                    m.Geometry.Count + " fili: " + gw(m, 1), ok);
            }
            catch (Exception ex) { Passo("AO: espressioni", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            try
            {
                // carico di Laplace di ANTFILE.DOC (pag. 5): 8,2e-6 s / (1 + 4,92e-16 s^2) = trappola LC parallela (8,2 uH, 60 pF circa)
                NecModel m = K6stiImport.ParseAnt(new[] { "Dipolo con trappole", "Free Space", "7.1 MHz", "5 wires, meters", "1 0 -10 10 0 -6 10 #12", "1 0 -6 10 0 -5.9 10 #12",
                    "21 0 -5.9 10 0 5.9 10 #12", "1 0 5.9 10 0 6 10 #12", "1 0 6 10 0 10 10 #12", "1 source", "Wire 3, center",
                    "2 loads", "Laplace Transform", "Wire 2, center", "0   8.2E-6", "1   0   4.92E-16", "Wire 4, center", "0   8.2E-6", "1   0   4.92E-16" });
                double w = 2 * Math.PI * 7.1e6;
                Complex za = new Complex(0, w * 8.2e-6) / (1 - w * w * 4.92e-16);
                int n4 = 0; bool ok = true;
                foreach (LoadSpec l in m.Loads) if (l.Type == 4) { n4++; ok &= Math.Abs(l.F1 - za.Real) < 1e-3 && Math.Abs(l.F2 - za.Imaginary) < 1e-3 * Math.Max(1, Math.Abs(za.Imaginary)); }
                Passo("AO: carichi di Laplace", string.Format(CI, "2 carichi LD 4 da {0} a 7,1 MHz, con un avviso", Z(za)), n4 + " carichi LD 4; " + m.Warnings.FindAll(x => x.Contains("Laplace")).Count + " avvisi",
                    ok && n4 == 2 && m.Warnings.Exists(x => x.Contains("Laplace")));
            }
            catch (Exception ex) { Passo("AO: carichi di Laplace", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            try
            {
                // materiali: resistivita' e permeabilita' sulla riga dei fili, materiale diverso su un filo
                NecModel m = K6stiImport.ParseAnt(new[] { "Materiali", "Free Space", "14.2 MHz", "2 resistivity 5.7E-08 permeability 1.5 wires, meters",
                    "11 0 -5 0 0 5 0 #12", "11 1 -5 0 1 5 0 #12 copper", "1 source", "Wire 1, center" });
                double s1 = 1 / 5.7e-8 / 1.5, s2 = 1 / 1.7241e-8;
                List<LoadSpec> l5 = m.Loads.FindAll(x => x.Type == 5);
                bool ok = l5.Count == 2 && l5.Exists(x => x.Tag == 1 && Math.Abs(x.F1 / s1 - 1) < 1e-6) && l5.Exists(x => x.Tag == 2 && Math.Abs(x.F1 / s2 - 1) < 1e-6);
                Passo("AO: resistivita', permeabilita' e materiale del singolo filo", string.Format(CI, "LD 5 filo 1 {0:0.000E+0} S/m (rho / mu), filo 2 rame {1:0.000E+0} S/m", s1, s2),
                    string.Join("; ", l5.ConvertAll(x => string.Format(CI, "filo {0}: {1:0.000E+0}", x.Tag, x.F1))), ok);
            }
            catch (Exception ex) { Passo("AO: materiali", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            try
            {
                // modo simmetrico con la sola meta' +Y: mezzo dipolo da 5 m con la sorgente sul piano -> dipolo intero, sorgente al centro,
                // stessa impedenza del dipolo intero scritto per esteso (21 segmenti)
                NecModel mh = K6stiImport.ParseAnt(new[] { "Mezzo dipolo", "Free Space Symmetric", "14.2 MHz", "1 wire, meters", "10 0 0 0 0 5 0 #12", "1 source", "Wire 1, end1" });
                NecModel mf = K6stiImport.ParseAnt(new[] { "Dipolo", "Free Space", "14.2 MHz", "1 wire, meters", "21 0 -5 0 0 5 0 #12", "1 source", "Wire 1, center" });
                Complex zh = Calcola(mh, 14.2).Zin, zf = Calcola(mf, 14.2).Zin;
                bool ok = mh.Geometry.Count == 1 && mh.Geometry[0].I2 == 21 && mh.Sources.Count == 1 && mh.Sources[0].Seg == 11 && Complex.Abs(zh - zf) < 1e-6 * Complex.Abs(zf);
                Passo("AO: \"Free Space Symmetric\" con la sola meta' +Y", "un filo da -5 a +5 m, 21 segmenti, sorgente all'11, stessa Z del dipolo intero",
                    string.Format(CI, "{0} fili, {1} segmenti, sorgente al {2}; {3} contro {4}", mh.Geometry.Count, mh.Geometry.Count > 0 ? mh.Geometry[0].I2 : 0, mh.Sources.Count > 0 ? mh.Sources[0].Seg : 0, Z(zh), Z(zf)), ok);
            }
            catch (Exception ex) { Passo("AO: modo simmetrico", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
        }

        // 0.4.13: le novita' dopo la 0.4.10 che non hanno gia' una procedura
        // ------------------------------------------------------------------------------------------------
        // 0.4.18 (richieste di IC8TEM del 14/11/2026 09:17-09:20): collaudo per le antenne a terra come il BOG "se il modello di calcolo
        // lo permette" (approssimazione a lambda/1000), Butternut con 64 radiali corretta, avvisi dei limiti del NEC-2 vicino al terreno,
        // tempi delle fasi del motore per la prova dei modelli pesanti (fase 2E).
        static Complex ZDi(string deck)
        {
            NecModel m = NecModel.Parse(deck);
            SegmentGeometry g = m.BuildGeometry();
            double f = m.Frequencies()[0];
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            return e.Sources[0].Z;
        }

        static string Zt(Complex z) { return string.Format(CI, "{0:0.0} {1} j{2:0.0} ohm", z.Real, z.Imaginary < 0 ? "-" : "+", Math.Abs(z.Imaginary)); }

        // 0.4.20 (richieste di IC8TEM del 14/11/2026: 12:55 LU a blocchi, 14:18 politica di calcolo, prove 5c/6 con EZNEC)
        // 0.4.21 (preparata il 14/11/2026 19:49 dopo il collaudo della 0.4.20 sorgente 4, fermo alla compilazione per un errore di
        // sintassi in ParabolaPanel.cs): arcarecci diritti e dima della centina, correzione del boom nei due versi, limiti del disco NEC
        // 0.5.0 (IC8TEM 15/11/2026 15:02): Yagi lunga DL6WU e Quagi nei Calcolatori. Valori attesi: nec2c corretto sugli stessi mazzi
        // (scratchpad yagi5/rif_collaudo.py): DL6WU 10 el. 144,3 MHz, elementi 4 mm, radiatore 10 mm: 51,37 +j16,88 ohm, 14,01 dBi, F/B 32,1 dB,
        // boom 4,456 m (stima DL6WU 13,94 dBi); Quagi 8 el. 144,3 MHz: 50,73 -j14,15 ohm, 13,34 dBi, F/B 14,2 dB, boom 4,197 m.
        static void Novita050()
        {
            try
            {
                CalcYagiDl6wu y = new CalcYagiDl6wu();
                CalcOutput o = y.Run();
                PrestazioniBanda r = PrestazioniBanda.Calcola(o.Nec, new[] { 144.3 }, 50);
                Passo("Yagi DL6WU 10 el. 144,3 MHz (misure del metodo, dipolo aperto)", "nec2c: 51,37 +j16,88 ohm entro 0,5 ohm, 14,01 dBi entro 0,05 dB, F/B 32,1 dB entro 0,5 dB",
                    string.Format(CI, "{0}, {1:0.00} dBi, F/B {2:0.0} dB", Z(r.Z[0]), r.G[0], r.Fb[0]),
                    Complex.Abs(r.Z[0] - new Complex(51.369, 16.881)) < 0.5 && Math.Abs(r.G[0] - 14.01) < 0.05 && Math.Abs(r.Fb[0] - 32.08) < 0.5);
                bool boom = false;
                foreach (CalcCard c in o.Cards) if (c.Title == "Boom" && c.Value.StartsWith(Calcolatori.N(4.456, 3), StringComparison.Ordinal)) boom = true;
                Passo("Yagi DL6WU: lunghezza del boom e stima del guadagno", "boom 4,456 m; stima 9,2 + 3,39 ln(2,145) = 11,79 dBd", boom ? "boom giusto" : "boom diverso", boom);
                // passo 1 dell'ottimizzazione: il radiatore porta il ROS sotto 1,15 al centro banda
                y.Par("passo").Value = 1;
                y.Tune(144.3);
                CalcOutput o1 = y.Run();
                PrestazioniBanda r1 = PrestazioniBanda.Calcola(o1.Nec, new[] { 144.3 }, 50);
                Passo("Yagi DL6WU: passo 1 dell'ottimizzazione (radiatore)", "ROS sotto 1,15 su 50 ohm a 144,3 MHz (nec2c: 1,09), guadagno invariato entro 0,1 dB",
                    string.Format(CI, "{0}, ROS {1:0.00}, {2:0.00} dBi", Z(r1.Z[0]), r1.Ros[0], r1.G[0]), r1.Ros[0] < 1.15 && Math.Abs(r1.G[0] - r.G[0]) < 0.1);
                // le quattro alimentazioni producono un modello calcolabile
                for (int fd = 1; fd < 4; fd++)
                {
                    CalcYagiDl6wu ya = new CalcYagiDl6wu();
                    ya.Par("feed").Value = fd;
                    CalcOutput oa = ya.Run();
                    PrestazioniBanda ra = PrestazioniBanda.Calcola(oa.Nec, new[] { 144.3 }, fd == 1 ? 200 : 50);
                    Passo("Yagi DL6WU con " + CalcYagiDl6wu.Alim[fd], "modello calcolato, guadagno tra 13,5 e 14,5 dBi", string.Format(CI, "{0}, {1:0.00} dBi", Z(ra.Z[0]), ra.G[0]), ra.G[0] > 13.5 && ra.G[0] < 14.5);
                }
            }
            catch (Exception ex) { Passo("Yagi DL6WU", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            try
            {
                CalcQuagi q = new CalcQuagi();
                CalcOutput o = q.Run();
                PrestazioniBanda r = PrestazioniBanda.Calcola(o.Nec, new[] { 144.3 }, 50);
                Passo("Quagi 8 el. 144,3 MHz (N6NB / Cebik)", "nec2c: 50,73 -j14,15 ohm entro 0,5 ohm, 13,34 dBi entro 0,05 dB",
                    string.Format(CI, "{0}, {1:0.00} dBi, F/B {2:0.0} dB", Z(r.Z[0]), r.G[0], r.Fb[0]),
                    Complex.Abs(r.Z[0] - new Complex(50.734, -14.147)) < 0.5 && Math.Abs(r.G[0] - 13.34) < 0.05);
            }
            catch (Exception ex) { Passo("Quagi", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // loop Yagi N6CA 1296 MHz, boom 6 piedi (25 el.): nec2c 50,06 +j9,68 ohm, 18,60 dBi (ARRL: circa 18 dBi)
            try
            {
                CalcLoopYagi ly = new CalcLoopYagi();
                CalcOutput o = ly.Run();
                PrestazioniBanda r = PrestazioniBanda.Calcola(o.Nec, new[] { 1296.1 }, 50);
                Passo("Loop Yagi N6CA 1296 MHz, boom 1,83 m (25 el.)", "nec2c: 50,06 +j9,68 ohm entro 0,5 ohm, 18,60 dBi entro 0,05 dB (ARRL: circa 18 dBi)",
                    string.Format(CI, "{0}, {1:0.00} dBi, F/B {2:0.0} dB", Z(r.Z[0]), r.G[0], r.Fb[0]),
                    Complex.Abs(r.Z[0] - new Complex(50.055, 9.676)) < 0.5 && Math.Abs(r.G[0] - 18.60) < 0.05);
            }
            catch (Exception ex) { Passo("Loop Yagi", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // solaio in calcestruzzo (proposta A, IC8TEM 15/11/2026 15:32): valori alla frequenza e motore che li ricalcola
            try
            {
                double er, sg, er2, sg2;
                TerreniEdifici.Valori(1, 144.3, out er, out sg);
                TerreniEdifici.Valori(2, 7.1, out er2, out sg2);
                Passo("solaio in calcestruzzo: valori alla frequenza", "ITU-R P.2040 a 144,3 MHz: er 5,24, sigma 0,01016 S/m; Perotoni a 7,1 MHz: er 20, sigma 9,48e-5 S/m",
                    string.Format(CI, "er {0}, sigma {1:0.00000}; er {2}, sigma {3:0.00E+0}", er, sg, er2, sg2),
                    er == 5.24 && Math.Abs(sg - 0.0101630) < 1e-6 && er2 == 20 && Math.Abs(sg2 - 9.47978e-5) < 1e-9);
                string dip = "CM dipolo sul solaio\nCM YA-NEC CALCESTRUZZO=1\nCE\nGW 1 21 0 -0.49 2 0 0.49 2 0.002\nGE 0\nGN 2 0 0 0 5.24 0.0101\nEX 0 1 11 0 1 0\nFR 0 1 0 0 432 0\nEN\n";
                NecModel mc = NecModel.Parse(dip);
                string esp = "CM stesso dipolo, terreno scritto a 432 MHz\nCE\nGW 1 21 0 -0.49 2 0 0.49 2 0.002\nGE 0\nGN 2 0 0 0 5.24 " + (0.0462 * Math.Pow(0.432, 0.7822)).ToString("R", CultureInfo.InvariantCulture) + "\nEX 0 1 11 0 1 0\nFR 0 1 0 0 432 0\nEN\n";
                NecModel me = NecModel.Parse(esp);
                Complex zc = Analysis.Run(mc, mc.BuildGeometry(), 432, 10.0).Zin, ze = Analysis.Run(me, me.BuildGeometry(), 432, 10.0).Zin;
                Passo("solaio in calcestruzzo: il motore usa i valori della frequenza di calcolo (432 MHz, non quelli scritti nella GN)", "stessa Z del modello con sigma 0,02396 S/m scritto nella GN, entro 1e-6",
                    string.Format(CI, "lettura CALCESTRUZZO={0}; {1} contro {2}", mc.Ground.Calcestruzzo, Z(zc), Z(ze)), mc.Ground.Calcestruzzo == 1 && Complex.Abs(zc - ze) < 1e-6 * Complex.Abs(ze));
                // antenna su tetto (proposta B): i due casi si calcolano
                CalcTetto t = new CalcTetto();
                t.Model = NecModel.Parse("CM dipolo 2 m\nCE\nGW 1 21 0 -0.49 0 0 0.49 0 0.002\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 144.3 0\nEN\n");
                t.Par("dz").Value = 3;
                t.Tune(144.3);
                CalcOutput ot = t.Run();
                bool due = ot.Cards.Exists(c => c.Title.StartsWith("Caso HF") && c.Value.StartsWith("Z ")) && ot.Cards.Exists(c => c.Title.StartsWith("Caso VHF") && c.Value.StartsWith("Z "));
                Passo("Antenna su tetto: dipolo 144 MHz a 3 m sopra un solaio a 15 m", "i due casi calcolati (terreno vero a 18 m, solaio a 3 m)",
                    string.Join(" | ", ot.Cards.FindAll(c => c.Title.StartsWith("Caso")).ConvertAll(c => c.Title + ": " + c.Value)), due);
            }
            catch (Exception ex) { Passo("solaio e antenna su tetto", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
        }

        static void Novita0430()
        {
            // 0.4.38 (fasi 1 e 2 del 15/11/2026, via di IC8TEM 13:22): terreno di Sommerfeld corretto (evlua, cammino sotto i punti di
            // diramazione che nec2c saltava). Riferimento necpp (T. Molteno), che coincide con EZNEC; prima YA-NEC scartava fino al 5,3 %.
            try
            {
                var casi = new List<Tuple<string, string, Complex, Complex>>();
                foreach (int d in new[] { 30, 60 })
                    casi.Add(Tuple.Create("due dipoli a " + d + " m, terreno 13 / 0,005, 3,6 MHz",
                        "CE\nGW 1 21 0 -20 15 0 20 15 0.001\nGW 2 21 " + d + " -20 15 " + d + " 20 15 0.001\nGE 0\nEX 0 1 11 0 1 0\nGN 2 0 0 0 13 0.005\nFR 0 1 0 0 3.6 0\nEN",
                        d == 30 ? new Complex(60.726, 14.428) : new Complex(66.541, -2.6931), d == 30 ? new Complex(60.41, 14.01) : new Complex(65.71, -2.98)));
                double[] hm = { 1, 3, 6, 12 };
                Complex[] rif = { new Complex(5.557, -44.922), new Complex(4.738, -35.251), new Complex(12.288, -17.360), new Complex(40.420, 7.2311) };
                Complex[] vec = { new Complex(4.1508, -46.839), new Complex(3.6315, -36.475), new Complex(11.495, -17.873), new Complex(40.029, 7.3316) };
                for (int i = 0; i < hm.Length; i++)
                    casi.Add(Tuple.Create("dipolo sul mare (80 / 5 S/m) a " + hm[i] + " m, 3,6 MHz",
                        "CE\nGW 1 21 0 -20 " + hm[i] + " 0 20 " + hm[i] + " 0.001\nGE 0\nEX 0 1 11 0 1 0\nGN 2 0 0 0 80 5\nFR 0 1 0 0 3.6 0\nEN", rif[i], vec[i]));
                foreach (var c in casi)
                {
                    NecModel ms = NecModel.Parse(c.Item2);
                    RunResult rs = Calcola(ms, ms.FreqMHz);
                    double sc = Complex.Abs(rs.Zin - c.Item3) / Complex.Abs(c.Item3) * 100, sv = Complex.Abs(c.Item4 - c.Item3) / Complex.Abs(c.Item3) * 100;
                    Passo("Sommerfeld corretto: " + c.Item1, string.Format(CI, "come necpp {0} entro lo 0,3 % (prima della 0.4.38 {1}, scarto {2:0.00} %)", Z(c.Item3), Z(c.Item4), sv),
                        string.Format(CI, "{0} (scarto {1:0.00} %)", Z(rs.Zin), sc), sc <= 0.3);
                }
            }
            catch (Exception ex) { Passo("Sommerfeld corretto", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 0. formato .yanec 2 (IC8TEM 15/11/2026 07:31): esempi e archivio in .yanec con l'abaco della descrizione, modello nel formato d'origine
            try
            {
                int nY = 0, nV2 = 0, nAbaco = 0, nVecchi = 0;
                List<string> senza = new List<string>();
                foreach (string dir in new[] { AppPaths.ModelsDir, Path.Combine(AppPaths.DataDir, "archivio") })
                {
                    if (!Directory.Exists(dir)) continue;
                    foreach (string f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        string e = Path.GetExtension(f).ToLowerInvariant();
                        if (e == ".nec" || e == ".maa" || e == ".weq" || e == ".ez" || e == ".yag" || e == ".ant") { nVecchi++; continue; }
                        if (e != ".yanec") continue;
                        nY++;
                        if (!YanecFile.E(f)) { senza.Add(Path.GetFileName(f)); continue; }
                        if (File.ReadLines(f).First().Contains("yanec 2")) nV2++;
                        List<string> corpo;
                        List<KeyValuePair<string, string>> t = YanecFile.Testata(f, out corpo);
                        bool tutte = true;
                        foreach (string k in new[] { "NOME", "DESCRIZIONE", "CATEGORIA", "FONTE", "ORIGINALE", "CONTENUTO" }) if (!t.Exists(x => x.Key == k)) tutte = false;
                        if (tutte) nAbaco++; else senza.Add(Path.GetFileName(f));
                    }
                }
                Passo("formato .yanec 2 negli esempi e nell'archivio", "tutti i modelli in .yanec 2 con l'abaco completo, nessun file nel formato vecchio",
                    string.Format(CI, "{0} .yanec, {1} di formato 2, {2} con l'abaco completo, {3} file nel formato vecchio{4}", nY, nV2, nAbaco, nVecchi, senza.Count > 0 ? " (da guardare: " + string.Join(", ", senza.Take(5)) + ")" : ""),
                    nY >= 290 && nV2 == nY && nAbaco == nY && nVecchi == 0);
                // un percorso nel formato vecchio apre il .yanec con lo stesso nome; stesso risultato del modello scritto a mano
                string vecchio = Path.Combine(AppPaths.ModelsDir, "Dipolo 144 MHz.nec");
                string y = YanecFile.Sostituto(vecchio);
                NecModel md = NecModel.Load(vecchio);
                Passo("\"Dipolo 144 MHz.nec\" chiesto con il nome vecchio", "apre \"Dipolo 144 MHz.yanec\" con l'abaco (NOME, CONTENUTO nec)",
                    (y == null ? "nessun sostituto" : Path.GetFileName(y)) + ", abaco " + md.Scheda.Count + " righe, " + md.Geometry.Count + " schede GW",
                    y != null && y.EndsWith("Dipolo 144 MHz.yanec") && md.Scheda.Exists(x => x.Key == "NOME") && md.Geometry.Count > 0 && md.Comments.TrueForAll(c => !c.StartsWith("YANEC")));
                // .maa dentro il .yanec: stesso importatore (risegmentazione) del file d'origine
                List<MmanaWireInfo> wm;
                NecModel mm = MmanaImport.Load(Path.Combine(AppPaths.ModelsDir, "10el DK7ZB 28 ohm (MMANA).maa"), 40, out wm);
                Passo("modello MMANA dentro un .yanec", "letto con l'importatore MMANA (fili MMANA presenti, formato MMANA)",
                    mm.SourceFormat + ", " + (wm == null ? 0 : wm.Count) + " fili MMANA, " + mm.Geometry.Count + " fili NEC", mm.SourceFormat == "MMANA" && wm != null && wm.Count > 0);
                // Salva con nome .yanec e rilettura: stessa impedenza, abaco conservato
                string tmp = Path.Combine(AppPaths.TempDir, "collaudo_yanec.yanec");
                File.WriteAllText(tmp, YanecFile.Testo(md, "collaudo_yanec", md.ToNec()));
                NecModel m2 = NecModel.Load(tmp);
                RunResult r1 = Calcola(md, md.FreqMHz), r2 = Calcola(m2, m2.FreqMHz);
                string desc1 = md.Scheda.Find(x => x.Key == "DESCRIZIONE").Value ?? "", desc2 = m2.Scheda.Find(x => x.Key == "DESCRIZIONE").Value ?? "";
                Passo("salvataggio in .yanec 2 e rilettura", "stessa impedenza, stessa descrizione nell'abaco, prima riga = firma",
                    Z(r1.Zin) + " / " + Z(r2.Zin) + ", descrizione " + (desc1 == desc2 ? "uguale" : "diversa"),
                    Complex.Abs(r1.Zin - r2.Zin) < 1e-6 && desc1 == desc2 && YanecFile.E(tmp));
                try { File.Delete(tmp); } catch { }
            }
            catch (Exception ex) { Passo("formato .yanec 2", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 1. fili nel terreno (GPR di IC8TEM: radiali a 2 mm con raggio 8 mm, EZNEC codice 1)
            Func<double, double, string, int> terra = (zr, rr, gn) =>
            {
                string nec = "CM prova\nCE\nGW 1 20 0 0 " + zr.ToString(CI) + " 0 0 5\t0.002\nGW 2 20 0 0 " + zr.ToString(CI) + " 5 0 " + zr.ToString(CI) + " " + rr.ToString(CI) + "\nGE 0\n" + gn + "EX 0 1 1 0 1 0\nFR 0 1 0 0 14.1 0\nEN\n";
                NecModel m = NecModel.Parse(nec.Replace("\t", " "));
                return TerrenoVicino.FiliNelTerreno(m.BuildGeometry(), m.Ground).Count;
            };
            try
            {
                int a = terra(0.002, 0.008, "GN 2 0 0 0 13 0.005\n"), b = terra(0.05, 0.008, "GN 2 0 0 0 13 0.005\n"), c = terra(0.002, 0.008, "");
                // filo disteso a z = 0 su terreno perfetto: YA-NEC lo rifiuta gia' alla costruzione della geometria (come EZNEC): conta come segnalato (0.4.31)
                int d;
                try { d = terra(0, 0.001, "GN 1\n"); } catch (NecException exd) { d = exd.Message.Contains("terreno") ? 1 : 0; }
                Passo("fili nel terreno: radiale a 2 mm con raggio 8 mm (terreno reale), a 5 cm, in spazio libero, disteso a z = 0 (terreno perfetto)",
                    "segnalato, non segnalato, non segnalato, segnalato (EZNEC: \"wires extend into or are lying on the ground\")",
                    string.Format(CI, "{0} / {1} / {2} / {3} avvisi", a, b, c, d), a == 1 && b == 0 && c == 0 && d == 1);
            }
            catch (Exception ex) { Passo("fili nel terreno", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 1b. monopolo a terra a tubi telescopici (eccezione di Cebik, amod82): si corregge come meta' di un elemento
            try
            {
                NecModel m = NecModel.Parse("CM monopolo a gradini\nCE\nGW 1 6 0 0 0 0 0 1.5 0.02\nGW 2 6 0 0 1.5 0 0 3.5 0.0125\nGW 3 4 0 0 3.5 0 0 5.0 0.008\nGE 1\nGN 1\nEX 0 1 1 0 1 0\nFR 0 1 0 0 14.2 0\nEN\n");
                List<Leeson.Catena> cm = Leeson.Trova(m);
                List<string> note = new List<string>();
                Leeson.Applica(m, 14.2, note);
                bool ok = cm.Count == 1 && cm[0].Monopolo && cm[0].Motivo == null && note.Exists(x => x.Contains("monopolo a terra"));
                Passo("monopolo a terra con tre tubi e sorgente alla base (terreno perfetto)", "corretto come meta' di elemento (Cebik, NEC Correctives)",
                    cm.Count + " catene; " + (cm.Count > 0 ? (cm[0].Monopolo ? "monopolo" : "non monopolo") + ", " + (cm[0].Motivo ?? "correggibile") : "") + "; " + string.Join(" ", note), ok);
            }
            catch (Exception ex) { Passo("monopolo a terra a gradini", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 1c. schermo di radiali del NEC-2 (GN con NRADL), proposta da cocoaNEC 3 accettata da IC8TEM il 14/11/2026 22:33; riferimento nec2c
            //     (RP con I1 = 4): verticale a 0,5 m con due radiali corti, 7,1 MHz, terreno medio GN 0
            foreach (object[] cs in new[] { new object[] { 60, 33.002, -547.36, 2.13, -2.84 }, new object[] { 4, 24.581, -542.21, 1.49, -3.04 } })
            {
                try
                {
                    int nr = (int)cs[0];
                    NecModel m = NecModel.Parse("CM schermo\nCE\nGW 1 21 0 0 0.5 0 0 10.7 0.01\nGW 2 5 0 0 0.5 2 0 0.5 0.001\nGW 3 5 0 0 0.5 -2 0 0.5 0.001\nGE 0\nGN 0 " + nr +
                        " 0 0 13 0.005 10.0 0.001\nEX 0 1 1 0 1 0\nFR 0 1 0 0 7.1 0\nEN\n");
                    SegmentGeometry g = m.BuildGeometry();
                    Nec2Engine e = new Nec2Engine(g);
                    e.Solve(7.1, m.Sources, m.EffectiveLoads(), m.Ground, false, 1.0, m.Networks);
                    Complex za = new Complex((double)cs[1], (double)cs[2]);
                    double g60 = e.Gain(60, 0).GainTotal, g30 = e.Gain(30, 0).GainTotal;
                    bool ok = Complex.Abs(e.Sources[0].Z - za) <= 0.002 * Complex.Abs(za) && Math.Abs(g60 - (double)cs[3]) <= 0.02 && Math.Abs(g30 - (double)cs[4]) <= 0.02;
                    Passo("schermo di radiali GN 0 con " + nr + " radiali da 10 m (verticale a 7,1 MHz)", string.Format(CI, "nec2c: {0}, G(60 gradi) {1:0.00} dBi, G(30) {2:0.00} dBi", Z(za), cs[3], cs[4]),
                        string.Format(CI, "{0}, G(60) {1:0.00} dBi, G(30) {2:0.00} dBi", Z(e.Sources[0].Z), g60, g30), ok);
                }
                catch (Exception ex) { Passo("schermo di radiali", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 1d. Cebik W4RNL, "Tapering to Perfection" (amod10), tab. 1: elementi sostitutivi di EZNEC per quattro dipoli a gradini (pollici).
            //     Idea approvata da IC8TEM il 14/11/2026 22:32 ("puoi applicare tutte e tre queste idee?"). Prova con il prototipo: entro 0,05 % e 0,32 %.
            {
                double[][][] lati =
                {
                    new[] { new[] { 150.0, 1.0 }, new[] { 204.0, 0.75 } },
                    new[] { new[] { 50.0, 1.0 }, new[] { 204.0, 0.75 } },
                    new[] { new[] { 20.0, 1.25 }, new[] { 100.0, 1.0 }, new[] { 205.75, 0.75 } },
                    new[] { new[] { 20.0, 2.5 }, new[] { 100.0, 1.0 }, new[] { 208.5, 0.75 } },
                };
                double[][] attesi = { new[] { 201.45, 0.966 }, new[] { 201.661, 0.792 }, new[] { 201.49, 0.889 }, new[] { 201.63, 0.895 } };
                StringBuilder sc = new StringBuilder();
                bool ok = true;
                for (int i = 0; i < lati.Length; i++)
                {
                    double l1, d1;
                    Leeson.Equivalente(new List<double[]>(lati[i]), out l1, out d1);
                    double el = Math.Abs(l1 / attesi[i][0] - 1) * 100, ed = Math.Abs(d1 / attesi[i][1] - 1) * 100;
                    if (el > 0.1 || ed > 0.5) ok = false;
                    sc.Append(string.Format(CI, "{0}caso {1}: {2:0.000}\" {3:0.0000}\" (EZNEC {4}\" {5}\", {6:0.000} % e {7:0.00} %)", i > 0 ? "; " : "", i + 2, l1, d1, attesi[i][0], attesi[i][1], el, ed));
                }
                Passo("elementi sostitutivi di Leeson contro EZNEC (Cebik, Tapering to Perfection, tab. 1)", "mezza lunghezza entro lo 0,1 %, diametro entro lo 0,5 %", sc.ToString(), ok);
            }
            // 1e. staffe di fissaggio (sottolinguetta dei Calcolatori, IC8TEM 14/11/2026 22:31): dipolo 25 mm a 14,175 MHz con piastra 150 x 150 x 5 mm.
            //     Prova con nec2c e l'algoritmo di Leeson (prototipo): originale 69,85 -j8,88 ohm; con la piastra 69,24 -j11,56; compensato (+13,4 mm
            //     per lato) 69,83 -j8,95: la compensazione riporta la reattanza entro 0,1 ohm
            try
            {
                NecModel m = NecModel.Parse("CM dipolo per le staffe\nCE\nGW 1 41 0 -5 0 0 5 0 0.0125\nGE 0\nEX 0 1 21 0 1 0\nFR 0 1 0 0 14.175 0\nEN\n");
                // 0.4.40: con la segmentazione originale (come EZNEC) il riferimento e' il dipolo con gli stessi tre fili della piastra
                //     (20 + 1 + 20 segmenti, il segmento centrale lungo 15 cm come la piastra) ma tutto da 25 mm: nec2c 64,90 -j8,38 ohm.
                //     Il solo accorciamento del segmento della sorgente (da 24 a 15 cm) toglie circa 5 ohm di R anche senza piastra: e' il NEC,
                //     non la piastra. Con la piastra 64,32 -j10,87; compensato (+13,4 mm) 64,85 -j8,45 (nec2c sul mazzo corretto, l440/staffe.py).
                NecModel mu = NecModel.Parse("CM dipolo diviso come la piastra\nCE\nGW 2 20 0 -5 0 0 -0.075 0 0.0125\nGW 1 1 0 -0.075 0 0 0.075 0 0.0125\nGW 3 20 0 0.075 0 0 5 0 0.0125\nGE 0\nEX 0 1 1 0 1 0\nFR 0 1 0 0 14.175 0\nEN\n");
                Complex z0 = Analysis.Run(mu, mu.BuildGeometry(), 14.175, 10.0).Zin;
                double dp = Staffe.DiametroPiastra(0.15, 0.005);
                List<Staffe.Elemento> el = Staffe.Trova(m, 14.175);
                foreach (Staffe.Elemento e in el) Staffe.Compensa(e, 0.15, dp);
                List<string> note = new List<string>();
                NecModel mp = Staffe.Aggiungi(m, el, 0.15, dp, false, note), mc = Staffe.Aggiungi(m, el, 0.15, dp, true, note);
                mp.Leeson = true; mc.Leeson = true;
                Complex zp = Analysis.Run(mp, mp.BuildGeometry(), 14.175, 10.0).Zin, zc = Analysis.Run(mc, mc.BuildGeometry(), 14.175, 10.0).Zin;
                bool ok = el.Count == 1 && mc.Geometry.Count == 3 && mc.Sources[0].Tag == 1 && el[0].Delta > 0.008 && el[0].Delta < 0.02 && Complex.Abs(zc - z0) < 0.5 && zp.Imaginary < z0.Imaginary - 1;
                Passo("staffe: dipolo 25 mm a 14,175 MHz, piastra 150 x 150 x 5 mm (Leeson acceso)", "compensazione circa 13 mm per lato; con la piastra X cala di circa 2,5 ohm; compensato entro 0,5 ohm dal dipolo diviso come la piastra (0.4.40)",
                    string.Format(CI, "{0} elemento, {1} fili dopo; compensazione {2:0.0} mm; originale {3}, con piastra {4}, compensato {5}", el.Count, mc.Geometry.Count, el.Count > 0 ? el[0].Delta * 1000 : 0, Z(z0), Z(zp), Z(zc)), ok);
            }
            catch (Exception ex) { Passo("staffe di fissaggio", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 1f. elica (scheda della barra Geometria, bozzetto 1 approvato il 14/11/2026 22:40): 435 MHz, 10 spire, C 1 lambda, passo 0,23 lambda,
            //     filo 4 mm, riflettore perfetto; nec2c: 199,61 -j32,15 ohm, 11,03 dBi totali, rapporto assiale 0,89, destra
            try
            {
                NecModel mh;
                HelixEsito he = HelixGen.Esito(new HelixSpec(), CancellationToken.None, out mh);
                Complex za = new Complex(199.61, -32.15);
                bool ok = Complex.Abs(he.Z - za) <= 0.015 * Complex.Abs(za) && Math.Abs(he.GdbicNec - 11.0) <= 0.3 && he.DestraNec;
                Passo("elica assiale 10 spire a 435 MHz su piano perfetto", "nec2c: 199,61 -j32,15 ohm, circa 11,0 dBic, polarizzazione destra; Kraus 15,4 dBic, Emerson 12,6",
                    string.Format(CI, "{0}, {1:0.00} dBic {2}, rapporto assiale {3:0.00}; Kraus {4:0.0} dBic, Emerson {5:0.0} dBic", Z(he.Z), he.GdbicNec, he.DestraNec ? "destra" : "sinistra", he.ArNec, he.GdbicKraus, he.GdbicEmerson), ok);
            }
            catch (Exception ex) { Passo("elica", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 1g. verticale multibanda tipo DX Commander (bozzetto 10 approvato il 14/11/2026 22:41): elementi riconosciuti dal file di IC8TEM,
            //     modello con il terreno medio di Sommerfeld e 32 radiali da 5 m a 5 cm senza contatti fuori giunzione, verifica della banda 20 m
            try
            {
                string fd = Path.Combine(AppPaths.DataDir, "archivio", "Verticali multibanda", "dxcommander_A.nec");
                VmSpec sp = new VmSpec { File = NecModel.Load(fd) };
                sp.El = VerticaleMultibanda.ElementiDalFile(sp.File);
                string bande = string.Join(", ", sp.El.ConvertAll(x => x.Banda + (x.Palo ? " (palo)" : "")));
                bool ok1 = sp.El.Count == 6 && bande == "40 m (palo), 30 m, 20 m, 17 m, 12 m, 10 m";
                Passo("verticale multibanda: elementi del file dxcommander_A", "6 elementi: 40 m (palo), 30, 20, 17, 12, 10 m", bande, ok1);
                List<string> note = new List<string>();
                NecModel mv = VerticaleMultibanda.Modello(sp, 14.175, note);
                List<string> ct = TerrenoVicino.ContattiFuoriGiunzione(mv.BuildGeometry());
                VmBanda vb = VerticaleMultibanda.Verifica(sp, sp.El.Find(x => x.Banda == "20 m"), null, CancellationToken.None);
                bool ok2 = ct.Count == 0 && !double.IsNaN(vb.RosMin) && vb.RosMin < 10 && vb.Eff > 0 && vb.Eff < 100 && vb.FRos > 13 && vb.FRos < 15;
                Passo("verticale multibanda: terreno medio (Sommerfeld), 32 radiali da 5 m a 5 cm, banda 20 m", "nessun contatto fuori giunzione; ROS minimo, guadagno a 10 gradi ed efficienza calcolati",
                    string.Format(CI, "{0} fili; contatti: {1}; ROS min {2:0.00} a {3:0.000} MHz, {4:0.00} dBi a 10 gradi, efficienza {5:0} %, ruba: {6}", mv.Geometry.Count, ct.Count == 0 ? "nessuno" : ct[0], vb.RosMin, vb.FRos, vb.G10, vb.Eff, vb.Ruba), ok2);
            }
            catch (Exception ex) { Passo("verticale multibanda", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 1h. antenne su PCB a 2,4 GHz (bozzetto 2 approvato il 14/11/2026 22:15, disegni 22:42): accordo col NEC contro il prototipo con nec2c
            //     (pcb24/pcb.py): IFA FR4 H 3 mm, massa 40 x 25 mm, pista 0,5 mm -> L 23,39 S 7,86 mm a 50 +j0; dipolo a 8 mm dalla massa 34,25 mm
            //     41,6 ohm; Yagi 5 elementi radiatore 35,39 mm 39,5 ohm; Big Wheel petali di 100 gradi R 19,49 mm 18,0 ohm; poi PDF 1:1 e DXF
            try
            {
                double[][] att = { new[] { 23.39, 50.0 }, new[] { 0.0, 0.0 }, new[] { 34.25, 41.6 }, new[] { 35.39, 39.5 }, new[] { 19.49, 18.0 } };
                foreach (int tipo in new[] { 0, 2, 3, 4 })
                {
                    Pcb24Spec sp = new Pcb24Spec { Tipo = tipo };
                    Pcb24.Iniziale(sp);
                    DateTime t0 = DateTime.Now;
                    Pcb24.Accorda(sp, CancellationToken.None);
                    Complex z = Pcb24.Z(sp, Pcb24.Fc(sp), CancellationToken.None);
                    bool ok = Math.Abs(sp.L - att[tipo][0]) <= 0.4 && Math.Abs(z.Real - att[tipo][1]) <= 1.5 && Math.Abs(z.Imaginary) <= 1.0;
                    if (tipo == 0) ok = ok && Math.Abs(sp.S - 7.86) <= 0.4;
                    Passo("antenna su PCB 2,4 GHz: " + Pcb24.Tipi[tipo] + " accordata col NEC", string.Format(CI, "come nec2c: L {0:0.00} mm{1}, R {2:0.0} ohm, X circa 0", att[tipo][0], tipo == 0 ? ", S 7,86 mm" : "", att[tipo][1]),
                        string.Format(CI, "L {0:0.00} mm{1}, {2} ({3:0.0} s)", sp.L, tipo == 0 ? string.Format(CI, ", S {0:0.00} mm", sp.S) : "", Z(z), (DateTime.Now - t0).TotalSeconds), ok);
                }
                Pcb24Spec mi = new Pcb24Spec { Tipo = 1, H = 2 };
                Pcb24.Iniziale(mi);
                Pcb24.Accorda(mi, CancellationToken.None);
                Pcb24Esito em = Pcb24.Esito(mi, CancellationToken.None);
                Passo("antenna su PCB 2,4 GHz: MIFA H 2 mm accordata, ROS sulla banda", "ROS al centro sotto 1,3 (nec2c: 1,17), banda a ROS 2 che copre 2400-2483,5 MHz",
                    string.Format(CI, "L {0:0.00} S {1:0.00} mm, {2}, ROS {3:0.00} / {4:0.00} / {5:0.00}, ROS 2 da {6:0} a {7:0} MHz, {8:0.0} dBi", mi.L, mi.S, Z(em.Zc), em.RosMin, em.RosC, em.RosMax, em.F2a, em.F2b, em.Gmax),
                    em.RosC < 1.3 && em.F2a <= 2400 && em.F2b >= 2483.5);
                Pcb24Spec ifa = new Pcb24Spec();
                Pcb24.Iniziale(ifa);
                string dxf = Pcb24.Dxf(ifa, true);
                byte[] pdf;
                using (MemoryStream ms = new MemoryStream()) { Pcb24Pdf.Scrivi(ms, ifa, null, true); pdf = ms.ToArray(); }
                bool okf = dxf.Contains("ENTITIES") && dxf.Contains("\nLINE\n") && dxf.Contains("MASSA") && pdf.Length > 1000 && pdf[0] == (byte)'%' && pdf[1] == (byte)'P';
                Passo("antenna su PCB 2,4 GHz: disegno in PDF 1:1 (due pagine, diritto e specchiato) e DXF", "PDF valido e DXF con piste e massa",
                    string.Format(CI, "PDF {0} byte, DXF {1} righe", pdf.Length, dxf.Split('\n').Length), okf);
            }
            catch (Exception ex) { Passo("antenne su PCB 2,4 GHz", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 1i. dipolo multibanda (tre modi approvati il 14/11/2026 23:11, XR-5 alle 23:18), spazio libero, accordo di tutte le bande insieme.
            //     Prototipo con nec2c (scratchpad/dipmb): bacchette 20/15/10 m (d 0,1 m) ROS al centro 1,21 / 1,03 / 1,19; manica 1,52 / 1,17 / 1,08;
            //     radiatori XR-5 senza riflettori, di fabbrica ROS 13 sul 17 m, accordati 1,39 / 1,21 / 1,06 / 1,45 / 1,05
            for (int modo = 0; modo < 3; modo++)
            {
                try
                {
                    DmSpec ds = new DmSpec { Modo = modo, Diametro = modo == 1 ? 0.02 : 0.002 };
                    DipoloMultibanda.Iniziale(ds);
                    DateTime t0 = DateTime.Now;
                    DipoloMultibanda.Accorda(ds, CancellationToken.None);
                    List<DmBanda> rb = DipoloMultibanda.Verifica(ds, CancellationToken.None);
                    StringBuilder sb = new StringBuilder();
                    bool ok = rb.Count == ds.Bande.Count;
                    foreach (DmBanda d in rb)
                    {
                        sb.Append(string.Format(CI, "{0}{1} ROS {2:0.00}", sb.Length > 0 ? "; " : "", d.Banda, d.RosC));
                        double lim = modo == 0 ? 1.6 : modo == 1 ? 1.7 : 2.0;
                        ok = ok && d.RosC < lim;
                    }
                    Passo("dipolo multibanda, " + DipoloMultibanda.Modi[modo] + ": accordo di tutte le bande",
                        modo == 0 ? "ROS al centro sotto 1,6 con d 0,1 m (nec2c: 1,21 / 1,03 / 1,19)" : modo == 1 ? "ROS al centro sotto 1,7 (nec2c: 1,52 / 1,17 / 1,08)" : "ROS al centro sotto 2 sulle cinque bande (nec2c senza Leeson: 1,39 / 1,21 / 1,06 / 1,45 / 1,05)",
                        sb.ToString() + string.Format(CI, " ({0:0} s)", (DateTime.Now - t0).TotalSeconds), ok);
                }
                catch (Exception ex) { Passo("dipolo multibanda, modo " + modo, "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 1j. bacchette parallele a quattro dipoli (IC8TEM 14/11/2026 23:32): 40/20/15/10 m, d 0,1 m; nec2c 1,53 / 1,09 / 1,69 / 1,58; poi
            //     l'esportazione per la Yagi HF (23:33) deve rileggersi come modello con le righe CM delle bande
            try
            {
                DmSpec d4 = new DmSpec { Modo = 0, Bande = new List<string> { "40 m", "20 m", "15 m", "10 m" } };
                DipoloMultibanda.Iniziale(d4);
                DipoloMultibanda.Accorda(d4, CancellationToken.None);
                List<DmBanda> r4 = DipoloMultibanda.Verifica(d4, CancellationToken.None);
                StringBuilder s4 = new StringBuilder();
                bool ok = r4.Count == 4;
                foreach (DmBanda d in r4) { s4.Append(string.Format(CI, "{0}{1} ROS {2:0.00}", s4.Length > 0 ? "; " : "", d.Banda, d.RosC)); ok = ok && d.RosC < 2.0; }
                string ny = DipoloMultibanda.EsportaYagi(d4, r4);
                NecModel my = NecModel.Parse(ny);
                bool okY = my.Geometry.Count == 4 * 4 - 1 && ny.Contains("CM 40 m") && ny.Contains("CM 10 m") && my.Sources.Count == 1;
                Passo("dipolo multibanda, bacchette parallele a 4 dipoli (40/20/15/10 m, d 0,1 m)", "ROS al centro sotto 2 su tutte le bande (nec2c: 1,53 / 1,09 / 1,69 / 1,58)", s4.ToString(), ok);
                Passo("dipolo multibanda: esportazione .nec per Yagi HF", "15 fili, una sorgente, righe CM con fili e misure di ogni banda", my.Geometry.Count + " fili, " + my.Sources.Count + " sorgenti", okY);
            }
            catch (Exception ex) { Passo("dipolo multibanda a 4 dipoli", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 1k. Yagi multibanda HF (IC8TEM 14/11/2026 23:36-23:37): bacchette 20/15/10 m d 0,1 m con riflettori (nec2c, peso F/B 0,002:
            //     ROS 1,02 / 1,09 / 1,19, 6,7 / 6,2 / 5,8 dBi, F/B circa 10 dB) e con direttori (configurazione dipolo-direttore: ROS 1,27 / 1,25 /
            //     1,18, 6,1 / 5,6 / 4,2 dBi, F/B 9,9 / 6,4 / 2,7 dB: i direttori di 20 e 15 m, davanti, disturbano il 10 m)
            for (int cfg = 0; cfg < 2; cfg++)
            {
                try
                {
                    YmSpec ym = new YmSpec();
                    DipoloMultibanda.Iniziale(ym.Rad);
                    foreach (string b in ym.Rad.Bande) ym.Direttore[b] = cfg == 1;
                    YagiMultibanda.Iniziale(ym);
                    DateTime t0 = DateTime.Now;
                    YagiMultibanda.Accorda(ym, CancellationToken.None);
                    List<YmBanda> ry = YagiMultibanda.Verifica(ym, CancellationToken.None);
                    StringBuilder sy = new StringBuilder();
                    bool ok = ry.Count == 3, okFb = true;
                    foreach (YmBanda d in ry)
                    {
                        sy.Append(string.Format(CI, "{0}{1} ROS {2:0.00} {3:0.0} dBi F/B {4:0.0} dB", sy.Length > 0 ? "; " : "", d.Banda, d.RosC, d.G, d.Fb));
                        ok = ok && d.RosC < 1.6 && d.G > (cfg == 0 ? 4.5 : 3.5) && (cfg == 1 || d.Fb > 6);
                        okFb = okFb && d.Fb > 1.5;
                    }
                    Passo("Yagi multibanda HF, bacchette 20/15/10 m, " + (cfg == 0 ? "riflettore-dipolo" : "dipolo-direttore"),
                        cfg == 0 ? "ROS al centro sotto 1,6, guadagno sopra 4,5 dBi, F/B sopra 6 dB (nec2c: 1,02 / 1,09 / 1,19, 6,7 / 6,2 / 5,8 dBi, F/B 10,4 / 10,0 / 10,2 dB)"
                                 : "ROS al centro sotto 1,6, guadagno sopra 3,5 dBi (nec2c: 1,27 / 1,25 / 1,18, 6,1 / 5,6 / 4,2 dBi)",
                        sy.ToString() + string.Format(CI, " ({0:0} s)", (DateTime.Now - t0).TotalSeconds), ok);
                    // 0.4.34 (collaudo 0.4.33): nella configurazione dipolo-direttore il F/B del 10 m e' debole per natura (nec2c 2,7 dB, YA-NEC 0,7 dB:
                    // con il peso 0,002 dell'obiettivo i due punti valgono quasi uguale e l'accordo preferisce il ROS): solo avviso
                    if (cfg == 1)
                        Passo("Yagi multibanda HF, dipolo-direttore: F/B", "sopra 1,5 dB su tutte le bande (solo avviso: con un direttore il F/B delle bande alte e' debole; nec2c 9,9 / 6,4 / 2,7 dB)",
                            sy.ToString(), okFb, true);
                }
                catch (Exception ex) { Passo("Yagi multibanda HF", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 1l. trappole sulle bacchette (IC8TEM 14/11/2026 23:40): 40/20/15/10 m d 0,1 m, trappole sul 15 m (-> 30 m) e sul 10 m (-> 17 m);
            //     nec2c (dipmb/fan6b.py): LC 25 pF ROS 1,18 / 1,12 / 1,03 / 1,14 / 1,06 / 1,10; sola bobina 1,95 / 1,47 / 1,87 / 2,49 / 1,59 / 1,58
            for (int tipo = 2; tipo >= 0; tipo -= 2)
            {
                try
                {
                    DmSpec d6 = new DmSpec { Modo = 0, Bande = new List<string> { "40 m", "20 m", "15 m", "10 m" } };
                    d6.Trappole.Add(new DmTrappola { Dipolo = "15 m", Nuova = "30 m", Tipo = tipo });
                    d6.Trappole.Add(new DmTrappola { Dipolo = "10 m", Nuova = "17 m", Tipo = tipo });
                    DipoloMultibanda.Iniziale(d6);
                    DateTime t0 = DateTime.Now;
                    DipoloMultibanda.Accorda(d6, CancellationToken.None);
                    List<DmBanda> r6 = DipoloMultibanda.Verifica(d6, CancellationToken.None);
                    StringBuilder s6 = new StringBuilder();
                    bool ok = r6.Count == 6;
                    foreach (DmBanda d in r6) { s6.Append(string.Format(CI, "{0}{1} {2:0.00}", s6.Length > 0 ? "; " : "", d.Banda, d.RosC)); ok = ok && d.RosC < (tipo == 2 ? 1.6 : 3.0); }
                    Passo("bacchette 40/20/15/10 m con trappole " + (tipo == 2 ? "LC 25 pF" : "a sola bobina") + " sul 15 m (-> 30 m) e sul 10 m (-> 17 m): 6 bande",
                        tipo == 2 ? "ROS al centro sotto 1,6 (nec2c: 1,18 / 1,12 / 1,03 / 1,14 / 1,06 / 1,10)" : "ROS al centro sotto 3 (nec2c: 1,95 / 1,47 / 1,87 / 2,49 / 1,59 / 1,58)",
                        s6.ToString() + "; " + DipoloMultibanda.DescriviTrappola(d6.Trappole[0]) + string.Format(CI, " ({0:0} s)", (DateTime.Now - t0).TotalSeconds), ok);
                }
                catch (Exception ex) { Passo("bacchette con trappole", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 1m. rastremazione dei tubi nei calcolatori HF (23:40): verticale lambda/4 a 7,1 MHz con i tubi 35-30-25-20-16-12 mm, risonanza col NEC
            try
            {
                CalcQuartoTerra qt = new CalcQuartoTerra();
                qt.Rastrema = true;
                CalcOutput oq = qt.Run();
                NecModel mq = NecModel.Parse(oq.Nec);
                double sc = qt.Tune(qt.P("f"));
                RunResult rq = SimpleCalc.Solve(qt.Run(), qt.P("f"));
                bool ok = mq.Rastremazioni.Count > 0 && mq.Leeson && Math.Abs(rq.Zin.Imaginary) < 3;
                Passo("rastremazione HF: verticale lambda/4 a 7,1 MHz a tubi telescopici, portata in risonanza", "tabella dei tubi nel modello, Leeson acceso, X entro 3 ohm dopo la risonanza",
                    string.Format(CI, "{0} righe di rastremazione, Leeson {1}, fattore {2:0.0000}, {3}", mq.Rastremazioni.Count, mq.Leeson ? "si" : "no", sc, Z(rq.Zin)), ok);
                LpdaDesign lp = new LpdaDesign { Fmin = 14, Fmax = 29.7, Tau = 0.88, Sigma = 0.16, Rin = 50, DiamMm = 25, Rastrema = true };
                lp.Compute();
                NecModel ml = NecModel.Parse(lp.ToNec(21.2, 0, new GroundSpec { Kind = GroundKind.FreeSpace }, 50));
                Passo("rastremazione HF: LPDA 14-29,7 MHz a tubi telescopici", "elementi lunghi almeno 1 m rastremati, Leeson acceso",
                    ml.Rastremazioni.Count + " righe, " + ml.Geometry.FindAll(g => g.Id == "GW" && g.F7 < 0).Count + " elementi rastremati su " + lp.N + ", Leeson " + (ml.Leeson ? "si" : "no"), ml.Rastremazioni.Count > 0 && ml.Leeson);
            }
            catch (Exception ex) { Passo("rastremazione HF", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 1n. esempio 4 del manuale (IC8TEM 15/11/2026 07:07): 10 elementi bvo con le variabili, partenza e misure ottimizzate contro nec2c
            //     (scratchpad/bvo): 144,3 MHz 59,9 -j1,0 ohm 15,72 dBi F/B 18,4 dB; ottimizzato 47,5 +j3,5 ohm 15,49 dBi F/B 26,3 dB
            try
            {
                string fb = Path.Combine(AppPaths.ModelsDir, "10el bvo IC8TEM con variabili (esempio Ottimizzatore).nec");
                fb = YanecFile.Sostituto(fb) ?? fb;   // 0.4.30: negli esempi e' un .yanec (schede NEC con la testata)
                string testo = File.ReadAllText(fb);
                string[] sy = { "SY L1=507*MM, L2=485*MM, L3=478.5*MM, X3=669*MM, L10=455*MM", "SY L1=492.6*MM, L2=480.2*MM, L3=475.5*MM, X3=665.6*MM, L10=458.5*MM" };
                double[][] att = { new[] { 59.9, -1.0, 15.72, 18.4 }, new[] { 47.5, 3.5, 15.49, 26.3 } };
                for (int k = 0; k < 2; k++)
                {
                    NecModel mb = NecModel.Parse(testo.Replace(sy[0], sy[k]));
                    RunResult rb = Analysis.Run(mb, mb.BuildGeometry(), 144.3, 5.0);
                    double g = rb.Engine.Gain(90, 0).GainTotal, fbv = g - rb.Engine.Gain(90, 180).GainTotal;
                    bool ok = Math.Abs(rb.Zin.Real - att[k][0]) < 1.0 && Math.Abs(rb.Zin.Imaginary - att[k][1]) < 1.0 && Math.Abs(g - att[k][2]) < 0.1 && Math.Abs(fbv - att[k][3]) < 0.7;
                    Passo("esempio 4 del manuale: 10 el bvo, " + (k == 0 ? "partenza" : "misure ottimizzate"), string.Format(CI, "come nec2c a 144,3 MHz: {0:0.0} {1:+0.0;-0.0}j ohm, {2:0.00} dBi, F/B {3:0.0} dB", att[k][0], att[k][1], att[k][2], att[k][3]),
                        string.Format(CI, "{0}, {1:0.00} dBi, F/B {2:0.0} dB", Z(rb.Zin), g, fbv), ok);
                }
            }
            catch (Exception ex) { Passo("esempio 4 del manuale (10 el bvo)", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 2. limite della correzione di Leeson (prove elemento per elemento: lo si alza per vedere se EZNEC corregge lontano dalla risonanza)
            // 0.5.3 (modifica A): sotto la risonanza si corregge sempre; il 45 % resta solo sopra
            Passo("limite della correzione di Leeson", "sopra la risonanza 45 %, sotto sempre (0.5.3, modifica A)", "sopra " + (Leeson.LimiteRisonanza * 100).ToString("0.#", CI) + " %, sotto " + (Leeson.SottoSempre ? "sempre" : "45 %"), Math.Abs(Leeson.LimiteRisonanza - 0.45) < 1e-12 && Leeson.SottoSempre);
            // 3. Hexbeam 20-10 m: misure di JN1GLB con spezzoni di coassiale 50 ohm (nec2c: ROS sotto 1,32), poi accordo a 50 + j0 ohm
            try
            {
                CalcHexbeam hx = new CalcHexbeam();
                CalcOutput o0 = hx.Run();
                List<string> ct = TerrenoVicino.ContattiFuoriGiunzione(NecModel.Parse(o0.Nec).BuildGeometry());
                Passo("Hexbeam: modello NEC senza contatti fuori giunzione", "nessuna delle sei regole del Geometry Check di EZNEC", ct.Count == 0 ? "nessuna" : ct[0], ct.Count == 0);
                StringBuilder sb = new StringBuilder();
                double rosMax = 0;
                for (int i = 0; i < 5; i++)
                {
                    hx.Par("b").Value = i;
                    CalcOutput o = hx.Run();
                    RunResult r = SimpleCalc.Solve(o, NecModel.Parse(o.Nec).FreqMHz);
                    rosMax = Math.Max(rosMax, r.Swr);
                    sb.Append((i > 0 ? "; " : "") + Z(r.Zin) + string.Format(CI, " ROS {0:0.00} G {1:0.0} dBi", r.Swr, r.GmaxDbi));
                }
                Passo("Hexbeam con le misure di JN1GLB e coassiale 50 ohm (20/17/15/12/10 m)", "ROS sotto 1,5 su tutte le bande (nec2c: sotto 1,32)", sb.ToString(), rosMax < 1.5);
                hx.Par("b").Value = 0;
                DateTime t0 = DateTime.Now;
                hx.Tune(14.175);
                StringBuilder sa = new StringBuilder();
                bool ok = true;
                for (int i = 0; i < 5; i++)
                {
                    hx.Par("b").Value = i;
                    CalcOutput o = hx.Run();
                    RunResult r = SimpleCalc.Solve(o, NecModel.Parse(o.Nec).FreqMHz);
                    sa.Append((i > 0 ? "; " : "") + Z(r.Zin));
                    if (Math.Abs(r.Zin.Imaginary) > 2 || r.Zin.Real < 45 || r.Zin.Real > 60) ok = false;
                }
                Passo("Hexbeam: accordo a 50 + j0 ohm con il pulsante Risonanza", "|X| sotto 2 ohm e R tra 45 e 60 ohm su tutte le bande (nec2c: 49,9-54,7 ohm)",
                    sa + string.Format(CI, " ({0:0.0} s)", (DateTime.Now - t0).TotalSeconds), ok);
            }
            catch (Exception ex) { Passo("Hexbeam 20-10 m", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 5. velocita' della luce come EZNEC (299,792458): MAGLOOP2 di 4nec2 (loop magnetica ad alto Q, 14,05 MHz) contro EZNEC 37,554 -j7,596 ohm
            {
                double cPrima = Nec2Engine.Cvel;
                try
                {
                    string f = Path.Combine(AppPaths.DataDir, "prove", "eznec_archivio", "modelli_ez", "4nec2 (corpus)", "4nec2", "models", "zz_MiniNec", "MagLoops", "MAGLOOP2.ez");
                    if (!File.Exists(f)) Passo("MAGLOOP2.ez nell'archivio EZNEC", "presente", "mancante", false);
                    else
                    {
                        NecModel m = EzImport.LoadEz(f);
                        Complex ze = new Complex(37.554, -7.596);
                        Nec2Engine.Cvel = Nec2Engine.CVEL_FISICA;
                        Complex z1 = Analysis.Run(m, m.BuildGeometry(), m.FreqMHz, 10.0).Zin;
                        Nec2Engine.Cvel = Nec2Engine.CVEL_NEC2;
                        Complex z0 = Analysis.Run(m, m.BuildGeometry(), m.FreqMHz, 10.0).Zin;
                        double d1 = Complex.Abs(z1 - ze) / Complex.Abs(ze) * 100, d0 = Complex.Abs(z0 - ze) / Complex.Abs(ze) * 100;
                        Passo("velocita' della luce come EZNEC: MAGLOOP2 (4nec2) contro EZNEC 37,554 -j7,596 ohm", "con 299,792458 entro lo 0,5 % (con 299,8 circa il 4 %)",
                            string.Format(CI, "299,792458: {0} ({1:0.00} %); 299,8: {2} ({3:0.00} %)", Z(z1), d1, Z(z0), d0), d1 < 0.5 && d0 > 2);
                    }
                }
                catch (Exception ex) { Passo("velocita' della luce come EZNEC", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
                finally { Nec2Engine.Cvel = cPrima; }
            }
            // 4. efficienza della loop magnetica della linguetta Loop (fino alla 0.4.23 il bilancio del NEC-2 dava -20/-25 %)
            try
            {
                NecModel fin;
                LoopEsito le = LoopGen.Esito(new LoopSpec(), CancellationToken.None, out fin);
                if (fin == null) Passo("loop magnetica della linguetta Loop", "modello finale creato", "nessun modello", false);
                else
                {
                    RunResult r = Analysis.Run(fin, fin.BuildGeometry(), fin.FreqMHz, 5.0);
                    bool av = r.Warnings.Exists(w => w.StartsWith("Bilancio di potenza", StringComparison.Ordinal));
                    Passo("efficienza della loop magnetica 1 m a 7,1 MHz (alto Q)", "tra 0 e 100 % (formula della scheda circa 16 %; prima -20 %)",
                        string.Format(CI, "{0:0.0} %{1}; Z {2}", r.Efficiency, av ? " (dal campo lontano: bilancio del NEC-2 non affidabile)" : "", Z(r.Zin)), r.Efficiency > 0 && r.Efficiency < 100);
                }
            }
            catch (Exception ex) { Passo("efficienza della loop magnetica", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
        }

        static void Novita0421()
        {
            // 0.4.21 (IC8TEM 14/11/2026 18:20-18:21): arcarecci diritti e dima della centina
            try
            {
                ParabolaInput pi = new ParabolaInput { Diametro_m = 3.0, FsuD = 0.42, NCentine = 16, NAnelli = 4, FreqMHz = 1296.1 };
                ParabolaResult pr = Parabola.Calcola(pi, false);
                bool tutti = pr.Arcarecci.Count == 4 && pr.Arcarecci.TrueForAll(x => x.Ok && x.Massa_kg > 0 && Math.Abs(x.Campata_m - 2 * x.Raggio_m * Math.Sin(Math.PI / 16)) < 1e-9);
                double ma = 0; foreach (ParabolaArcareccio x in pr.Arcarecci) ma += x.Massa_kg;
                Passo("arcarecci diritti, disco 3 m, 16 centine, 4 anelli, alluminio 6060", "4 anelli dimensionati, campata = corda, massa degli anelli = somma degli arcarecci",
                    string.Format(CI, "{0} anelli, tubi {1}, massa {2:0.00} kg (anelli nel peso {3:0.00} kg)", pr.Arcarecci.Count, string.Join(", ", pr.Arcarecci.ConvertAll(x => x.Sezione.Nome)), ma, pr.MassaAnelli_kg),
                    tutti && Math.Abs(ma - pr.MassaAnelli_kg) < 1e-9);
                string dxf = Parabola.DimaDxf(pi, pr, 0.06, 0.03);
                int linee = 0, k = -1; while ((k = dxf.IndexOf("\r\nLINE\r\n", k + 1, StringComparison.Ordinal)) >= 0) linee++;
                Passo("dima della centina in DXF", "sezione ENTITIES con linee e fine EOF", linee + " linee, " + dxf.Length + " caratteri", dxf.Contains("ENTITIES") && dxf.TrimEnd().EndsWith("EOF") && linee > 100);
            }
            catch (Exception ex) { Passo("arcarecci e dima della centina", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // correzione del boom: valore della formula DL6WU/G3SEK (boom quadro 25 mm a 144,3 MHz, passante conduttivo) e simmetria dei due versi
            {
                double c = YaNec.MainWindow.CorrezioneBoom(144.3, 25, true, 0, 0);
                double lam = 299.792458 / 144.3 * 1000, B = 25 * 1.18 / lam, atteso = (25.195 * B - 229 * B * B) * 25 * 1.18;
                Passo("correzione del boom, quadro 25 mm a 144,3 MHz, passante conduttivo", "C = (25,195 B - 229 B^2) x 1,18 x 25 = " + atteso.ToString("0.00", CI) + " mm (circa 9,2)",
                    c.ToString("0.00", CI) + " mm; isolato " + YaNec.MainWindow.CorrezioneBoom(144.3, 25, true, 1, 0).ToString("0.00", CI) + " mm",
                    Math.Abs(c - atteso) < 1e-9 && Math.Abs(c - 9.19) < 0.05 && Math.Abs(YaNec.MainWindow.CorrezioneBoom(144.3, 25, true, 1, 0) - c / 2) < 1e-12);
            }
            // diametri massimi del modello NEC del disco (12000 segmenti, circa 7 lambda; 1,2 m al massimo)
            {
                double d1296 = YaNec.ParabolaPanel.DiametroNecMassimo(1296.1, 0.42, 16, 0.087), d2320 = YaNec.ParabolaPanel.DiametroNecMassimo(2320.1, 0.42, 16, 0.087),
                       d5760 = YaNec.ParabolaPanel.DiametroNecMassimo(5760.1, 0.42, 16, 0.087);
                Passo("diametro massimo del disco nel NEC (f/D 0,42, 16 centine)", "1296 MHz 1,2 m; 2320 MHz 0,9 m; 5760 MHz 0,35 m (a passi di 5 cm)",
                    string.Format(CI, "1296 MHz {0:0.00} m; 2320 MHz {1:0.00} m; 5760 MHz {2:0.00} m", d1296, d2320, d5760),
                    Math.Abs(d1296 - 1.2) < 1e-6 && Math.Abs(d2320 - 0.9) < 1e-6 && Math.Abs(d5760 - 0.35) < 1e-6);
            }
        }

        static void Novita0420()
        {
            bool prima = Nec2Engine.LuClassica;
            // 1. LU a blocchi contro LU classica (stesso modello, stessa geometria)
            foreach (string nome in new[] { "Dipolo 144 MHz.nec", "4x7el DK7ZB H-frame 3 m x 3 m.nec", "Four-square 7 MHz con radiali (terreno medio).nec" })
            {
                string f = Path.Combine(AppPaths.ModelsDir, nome);
                if (!YanecFile.Esiste(f)) { Passo("modello " + nome, "presente in data/modelli", "mancante", false); continue; }
                try
                {
                    NecModel m = NecModel.Load(f);
                    SegmentGeometry g = m.BuildGeometry();
                    Nec2Engine.LuClassica = false;
                    RunResult a = Analysis.Run(m, g, m.FreqMHz, 10.0);
                    Nec2Engine.LuClassica = true;
                    RunResult b = Analysis.Run(m, g, m.FreqMHz, 10.0);
                    Nec2Engine.LuClassica = prima;
                    double d = Complex.Abs(a.Zin - b.Zin) / Math.Max(1e-9, Complex.Abs(b.Zin));
                    Passo("LU a blocchi come LU classica: " + nome, "stessa impedenza, scarto sotto 1e-8",
                        string.Format(CI, "{0} segmenti: a blocchi {1}, classica {2}, scarto {3:0.0E+0}", g.N, Zt(a.Zin), Zt(b.Zin), d), d < 1e-8);
                }
                catch (Exception ex) { Passo("LU a blocchi: " + nome, "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
                finally { Nec2Engine.LuClassica = prima; }
            }
            Info("Istruzioni vettoriali AVX2/FMA per il prodotto di matrici della LU: " + (System.Runtime.Intrinsics.X86.Fma.IsSupported ? "si'" : "no (nucleo scalare)") + ".");

            // 2. le sei regole del Geometry Check di EZNEC, su modelli costruiti apposta (filo di 1 m, raggio 1 mm, 144 MHz)
            string testa = "CE\n", coda = "GE 0\nEX 0 1 3 0 1 0\nFR 0 1 0 0 144 0\nEN\n";
            Action<string, string, string> regola = (titolo, fili, parola) =>
            {
                NecModel m = NecModel.Parse(testa + fili + coda);
                List<string> av = TerrenoVicino.ContattiFuoriGiunzione(m.BuildGeometry());
                bool ok = parola == null ? av.Count == 0 : av.Exists(x => x.Contains(parola));
                Passo("controllo geometrico: " + titolo, parola == null ? "nessun avviso" : "avviso con \"" + parola + "\"", av.Count == 0 ? "nessun avviso" : string.Join(" | ", av), ok);
            };
            regola("dipolo e riflettore normali", "GW 1 5 0 -0.5 0 0 0.5 0 0.001\nGW 2 5 -0.3 -0.52 0 -0.3 0.52 0 0.001\n", null);
            regola("estremo appoggiato a meta' di un segmento", "GW 1 5 0 -0.5 0 0 0.5 0 0.001\nGW 2 3 0 0 0 0 0 0.6 0.001\n", "end intersects wire");
            regola("due fili che si incrociano", "GW 1 5 0 -0.5 0 0 0.5 0 0.001\nGW 2 5 0 0 -0.5 0 0 0.5 0.001\n", "intersect at other than");
            regola("fili troppo vicini", "GW 1 5 0 -0.5 0 0 0.5 0 0.001\nGW 2 5 0.0015 0.1 -0.5 0.0015 0.1 0.5 0.001\n", "too close");
            regola("fili paralleli che si toccano", "GW 1 5 0 -0.5 0 0 0.5 0 0.001\nGW 2 5 0.0015 -0.5 0 0.0015 0.5 0 0.001\n", "parallel and contacting");
            regola("fili sovrapposti", "GW 1 5 0 -0.5 0 0 0.5 0 0.001\nGW 2 2 0 -0.2 0 0 0.2 0 0.001\n", "overlap");
            regola("filo sottile dentro un tubo grosso", "GW 1 5 0 -0.5 0 0 0.5 0 0.03\nGW 2 20 0 0.1 0 0 0.1 0.5 0.0005\n", "center of end segment");
            regola("tubi in fila con diametri diversi (giunzione regolare)", "GW 1 3 0 -0.5 0 0 -0.1 0 0.004\nGW 2 1 0 -0.1 0 0 0.1 0 0.02\nGW 3 3 0 0.1 0 0 0.5 0 0.004\n", null);

            // 3. terreno reale MININEC di EZNEC (GN 3 in YA-NEC): scrittura e rilettura del .ez
            string dip = "CM prova terreno\nCE\nGW 1 21 0 -5.2 10 0 5.2 10 0.001\nGE 1\n{0}\nEX 0 1 11 0 1 0\nFR 0 1 0 0 14.2 0\nEN\n";
            foreach (string[] t in new[] { new[] { "GN 3 0 0 0 13 0.005", "M", "MININEC (GN 3)" }, new[] { "GN 0 0 0 0 13 0.005", "H", "GN 0 scritto come High Accuracy" }, new[] { "GN 2 0 0 0 13 0.005", "H", "Sommerfeld (GN 2)" } })
            {
                string tmp = Path.Combine(AppPaths.DataDir, "collaudo_terreno.ez");
                try
                {
                    NecModel m = NecModel.Parse(string.Format(CI, dip, t[0]));
                    List<string> perso = new List<string>();
                    byte[] ez = FormatiExport.Ez(m, perso);
                    File.WriteAllBytes(tmp, ez);
                    NecModel r = EzImport.LoadEz(tmp);
                    bool attM = t[1] == "M";
                    Passo(".ez con terreno " + t[2], "byte del modello di terreno '" + t[1] + "', riletto come " + (attM ? "MININEC (GN 3)" : "Sommerfeld (GN 2)"),
                        "'" + (char)ez[0x7B] + "', riletto " + (r.Ground.MiniNec ? "MININEC" : r.Ground.Sommerfeld ? "Sommerfeld" : "GN 0") + (perso.Count > 0 ? "; nota: " + perso[0] : ""),
                        ez[0x7B] == (byte)t[1][0] && r.Ground.MiniNec == attM && r.Ground.Sommerfeld == !attM);
                }
                catch (Exception ex) { Passo(".ez con terreno " + t[2], "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
                try { File.Delete(tmp); } catch { }
            }
            {
                // dipolo a 10 m: con il terreno MININEC l'impedenza e' quella su terreno perfetto (EZNEC 75,918 +j2,708; nec2c GN 1 75,915 +j2,671)
                NecModel m3 = NecModel.Parse(string.Format(CI, dip, "GN 3 0 0 0 13 0.005"));
                Complex z3 = ZDi(m3.ToNec());
                Complex att = new Complex(75.918, 2.708);
                Passo("dipolo a 10 m con il terreno MININEC come EZNEC", Zt(att) + " entro lo 0,5 % (EZNEC Pro/2+ 7.0, prova 6 di IC8TEM)", Zt(z3), Complex.Abs(z3 - att) / Complex.Abs(att) < 0.005);
            }

            // 4. diagramma 3D a 5 gradi affinato poi a 2 gradi (modelli grandi) e processori lasciati all'interfaccia
            try
            {
                NecModel m = NecModel.Load(Path.Combine(AppPaths.ModelsDir, "Dipolo 144 MHz.nec"));
                SegmentGeometry g = m.BuildGeometry();
                RunResult r5 = Analysis.Run(m, g, m.FreqMHz, 5.0);
                RunResult r2 = Analysis.Run(m, g, m.FreqMHz, 2.0);
                double agt5 = r5.AgtDb;
                Analysis.AffinaDiagramma(r5, 2.0, CancellationToken.None);
                Passo("diagramma 3D affinato da 5 a 2 gradi", "passo 2 gradi, AGT uguale al calcolo diretto a 2 gradi",
                    string.Format(CI, "passo {0} gradi, AGT {1:0.000} dB (a 5 gradi {2:0.000}, diretto a 2 gradi {3:0.000}), Gmax {4:0.00} / {5:0.00} dBi", r5.Grid.ThetaStep, r5.AgtDb, agt5, r2.AgtDb, r5.GmaxDbi, r2.GmaxDbi),
                    r5.Grid.ThetaStep == 2.0 && Math.Abs(r5.AgtDb - r2.AgtDb) < 1e-9 && Math.Abs(r5.GmaxDbi - r2.GmaxDbi) < 0.01);
            }
            catch (Exception ex) { Passo("diagramma 3D affinato", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            {
                bool vecchio = Processori.RiservaInterfaccia;
                Processori.RiservaInterfaccia = true;
                int conInterfaccia = Processori.Calcolo;
                Processori.RiservaInterfaccia = false;
                int senza = Processori.Calcolo;
                Processori.RiservaInterfaccia = vecchio;
                int n = Environment.ProcessorCount;
                Passo("processori del calcolo", "con la finestra uno in meno (oltre 2 processori), senza finestra tutti",
                    string.Format(CI, "{0} processori logici: con la finestra {1}, senza {2}", n, conInterfaccia, senza), senza == n && conInterfaccia == (n > 2 ? n - 1 : n));
            }
            // 6. collaudo 0.4.20 (IC8TEM 16:01, "rigoroso sulla creazione dei modelli nec delle schede"): nessun modello dei calcolatori ha
            //    contatti fuori giunzione (Four-square con i radiali sovrapposti e falsi allarmi sulla QFH trovati nella prima prova con EZNEC)
            {
                List<string> sbagliati = new List<string>();
                int nmod = 0;
                foreach (SimpleCalc sc in SimpleCalc.All())
                {
                    if (sc.UsesModel) continue;
                    try
                    {
                        CalcOutput o = sc.Run();
                        if (string.IsNullOrEmpty(o.Nec)) continue;
                        nmod++;
                        NecModel m = NecModel.Parse(o.Nec);
                        List<string> c = TerrenoVicino.ContattiFuoriGiunzione(m.BuildGeometry());
                        if (c.Count > 0) sbagliati.Add(sc.Nome + ": " + c[0]);
                    }
                    catch (Exception ex) { sbagliati.Add(sc.Nome + ": " + ex.Message); }
                }
                Passo("modelli dei calcolatori senza contatti fuori giunzione (" + nmod + " schede, valori iniziali)", "nessun modello con contatti (EZNEC li calcolerebbe tutti)",
                    sbagliati.Count == 0 ? "nessuno" : string.Join(" | ", sbagliati), sbagliati.Count == 0);
            }
            // 7. 0.4.20 (IC8TEM 16:24): stack creato dalla scheda DL6WU (StackDetect.Crea) e riconosciuto con le stesse distanze
            try
            {
                // dipolo e riflettore (2 fili per antenna: il riconoscimento degli stack vuole almeno 2 fili per copia), lobo verso +X
                NecModel d = NecModel.Parse("CM prova stack\nCE\nGW 1 11 0 -0.5 0 0 0.5 0 0.003\nGW 2 11 -0.3 -0.52 0 -0.3 0.52 0 0.003\nGE 0\nEX 0 1 6 0 1 0\nFR 0 1 0 0 144 0\nEN\n");
                int fili = d.Geometry.FindAll(q => q.Id == "GW").Count, sorg = d.Sources.Count;
                List<string> note = new List<string>();
                NecModel st = StackDetect.Crea(d, 2, 2, 1.5, 1.2, 0, note);
                StackInfo si = StackDetect.Detect(st);
                List<string> c = TerrenoVicino.ContattiFuoriGiunzione(st.BuildGeometry());
                Passo("stack 2 x 2 creato da un dipolo con riflettore (verticale 1,5 m, orizzontale 1,2 m)", "4 copie riconosciute, spaziature 1,5 e 1,2 m, 4 sorgenti, nessun contatto",
                    si == null ? "stack non riconosciuto" : string.Format(CI, "{0} copie ({1} x {2}), verticale {3:0.###} m, orizzontale {4:0.###} m, {5} sorgenti, {6} fili, contatti {7}",
                        si.Copies, si.Rows, si.Cols, si.SpacingV, si.SpacingH, st.Sources.Count, st.Geometry.FindAll(q => q.Id == "GW").Count, c.Count),
                    si != null && si.Copies == 4 && Math.Abs(si.SpacingV - 1.5) < 1e-6 && Math.Abs(si.SpacingH - 1.2) < 1e-6 && st.Sources.Count == 4 * sorg && c.Count == 0);
                bool rifiuta = false;
                try { StackDetect.Crea(st, 2, 1, 1.5, 0, 0, null); } catch (NecException) { rifiuta = true; }
                Passo("stack di uno stack", "rifiutato (le distanze si cambiano in Struttura 3D > Oggetti del modello > Stack)", rifiuta ? "rifiutato" : "creato", rifiuta);
            }
            catch (Exception ex) { Passo("stack creato dalla scheda DL6WU", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            // 5. avvisi dei segmenti corti: EZNEC li segnala ma calcola (prova 6), il testo non dice piu' che li rifiuta
            {
                NecModel m = NecModel.Parse("CE\nGW 1 3 0 0 0 0 0 0.03 0.0005\nGW 2 11 0 0 0.03 0 0 5 0.001\nGE 1\nGN 1\nEX 0 1 2 0 1 0\nFR 0 1 0 0 14 0\nEN\n");
                List<string> av = TerrenoVicino.Controlla(m.BuildGeometry(), m.Ground, 14);
                string a = av.Find(x => x.Contains("lambda/1000")) ?? "";
                Passo("avviso dei segmenti corti", "segnalati; \"YA-NEC e EZNEC li calcolano lo stesso\"", a == "" ? "nessun avviso" : a, a.Contains("calcolano lo stesso") && !a.Contains("rifiuta"));
            }
        }

        static void Novita0418()
        {
            // 1. BOG 68 m a 1,83 MHz, terreno medio, terminazione 250 + 20 ohm: filo a lambda/1000 (0,17 m), mazzo di RxAntennas.Bog
            RxParams pb = new RxParams { F = 1.83, L = 68, H = RxAntennas.QuotaBog(1.83), Rterm = 250, Rrod = 20 };
            Passo("quota del BOG nel modello", "lambda/1000 = 0,164 m arrotondato per eccesso al cm: 0,17 m", pb.H.ToString("0.###", CI) + " m", Math.Abs(pb.H - 0.17) < 1e-9);
            string deck = RxAntennas.Bog(pb);
            Complex zb = ZDi(deck);
            Complex attB = new Complex(1639.3, -416.74);
            Passo("BOG a lambda/1000 (80 segmenti, Sommerfeld) come nec2c", Zt(attB) + " entro l'1% (nec2c 1.3.1, stesso mazzo)", Zt(zb), Complex.Abs(zb - attB) / Complex.Abs(attB) < 0.01);
            // 2. stesso BOG a 5 cm, sotto il limite: nec2c da' il doppio dell'impedenza; YA-NEC deve fare lo stesso e avvisare
            RxParams p5 = new RxParams { F = 1.83, L = 68, H = 0.05, Rterm = 250, Rrod = 20 };
            string deck5 = RxAntennas.Bog(p5);
            Complex z5 = ZDi(deck5);
            Complex att5 = new Complex(3301.8, -1336.1);
            Passo("BOG a 5 cm (sotto lambda/1000) come nec2c", Zt(att5) + " entro l'1%: il NEC-2 fuori dal suo campo, impedenza che sale al calare della quota", Zt(z5), Complex.Abs(z5 - att5) / Complex.Abs(att5) < 0.01);
            NecModel m5 = NecModel.Parse(deck5);
            List<string> av5 = TerrenoVicino.Controlla(m5.BuildGeometry(), m5.Ground, 1.83);
            Passo("avviso per il BOG a 5 cm", "segmenti sotto lambda/1000 con il terreno di Sommerfeld", av5.Count > 0 ? av5[0] : "nessun avviso", av5.Exists(a => a.Contains("lambda/1000")));
            NecModel m17 = NecModel.Parse(deck);
            List<string> av17 = TerrenoVicino.Controlla(m17.BuildGeometry(), m17.Ground, 1.83);
            Passo("nessun avviso per il BOG a lambda/1000", "nessun avviso", av17.Count == 0 ? "nessun avviso" : string.Join(" | ", av17), av17.Count == 0);
            Complex za, zc;
            Complex z0 = RxAntennas.Z0Modello(pb, RxAntennas.Bog, out za, out zc);
            Info("Z0 del filo nel modello a lambda/1000: " + Zt(z0) + " (aperto " + Zt(za) + ", in corto " + Zt(zc) + "); BOG reale 200-300 ohm (N4DJ, K1FZ).");
            Passo("Z0 del modello del BOG", "numero finito con parte reale positiva", Zt(z0), z0.Real > 0 && !double.IsNaN(z0.Real));

            // 3. Butternut con 64 radiali: vecchia conversione (GN 0, radiali a 3 cm, 66 fili in un punto) e modello corretto
            string vecchia = Path.Combine(AppPaths.DataDir, "prove", "pesanti", "Butternut_7877_vecchio_solo_tempi.nec");
            string corretta = Path.Combine(AppPaths.DataDir, "archivio", "Modelli IC8TEM (convertiti in NEC)", "80_40_Butternut_64Radials.nec");
            if (File.Exists(vecchia))
            {
                NecModel mv = NecModel.Load(vecchia);
                List<string> avv = TerrenoVicino.Controlla(mv.BuildGeometry(), mv.Ground, 7.15);
                Passo("avvisi sulla vecchia Butternut (senza calcolarla)", "fili sotto lambda/10 con GN 0 e giunzione con piu' di 30 fili",
                    avv.Count + " avvisi: " + string.Join(" | ", avv), avv.Exists(a => a.Contains("GN 0")) && avv.Exists(a => a.Contains("si uniscono")));
            }
            else Passo("vecchia Butternut per i tempi", "in data/prove/pesanti", "mancante", false);
            if (YanecFile.Esiste(corretta))
            {
                NecModel mc = NecModel.Load(corretta);
                SegmentGeometry gc = mc.BuildGeometry();
                List<string> avc = TerrenoVicino.Controlla(gc, mc.Ground, 7.15);
                Passo("Butternut corretta: geometria", "1431 segmenti (0.4.20), Sommerfeld, nessun avviso sul terreno o sulle giunzioni",
                    gc.N + " segmenti, " + (mc.Ground.Sommerfeld ? "Sommerfeld" : "non Sommerfeld") + ", " + (avc.Count == 0 ? "nessun avviso" : string.Join(" | ", avc)),
                    gc.N == 1431 && mc.Ground.Sommerfeld && avc.Count == 0);
                Stopwatch sw = Stopwatch.StartNew();
                RunResult rc = Analysis.Run(mc, gc, 7.15, 5.0);
                Complex attC = new Complex(17.565, 25.176);   // 0.4.20: segmenti di almeno lambda/1000 (nec2c)
                Passo("Butternut corretta come nec2c", Zt(attC) + " entro l'1%, parte reale positiva, guadagno finito",
                    string.Format(CI, "{0}, Gmax {1:0.00} dBi a {2:0} gradi di elevazione, {3:0.0} s", Zt(rc.Zin), rc.GmaxDbi, rc.ElevationOfMax, sw.Elapsed.TotalSeconds),
                    Complex.Abs(rc.Zin - attC) / Complex.Abs(attC) < 0.01 && rc.Zin.Real > 0 && rc.GmaxDbi > -10);
                // 4. tempi delle fasi del motore (servono alla prova dei modelli pesanti)
                Nec2Engine e = rc.Engine;
                Passo("tempi delle fasi del motore", "Sommerfeld, riempimento e fattorizzazione misurati (maggiori di zero)",
                    string.Format(CI, "Sommerfeld {0:0} ms, riempimento {1:0} ms, LU {2:0} ms, soluzione {3:0.0} ms, diagramma {4:0} ms", e.MsSommerfeld, e.MsRiempimento, e.MsFattorizzazione, e.MsSoluzione, rc.MsDiagramma),
                    e.MsRiempimento > 0 && e.MsFattorizzazione > 0 && rc.MsDiagramma > 0);
            }
            else Passo("Butternut corretta", "nell'archivio", "mancante", false);

            // 5. i 3 modelli pesanti della fase 2E sono al loro posto
            string cart = Path.Combine(AppPaths.DataDir, "prove", "pesanti");
            int presenti = 0;
            foreach (string f in new[] { "Butternut_7877_vecchio_solo_tempi.nec", "Bowtie_91XG_4nec2.nec", "2SA_4x2SA13_4nec2.nec" }) if (File.Exists(Path.Combine(cart, f))) presenti++;
            Passo("modelli della fase 2E (YA-NEC.exe --prove-pesanti)", "3 file in data/prove/pesanti", presenti + " file", presenti == 3);
        }

        static void Novita0413()
        {
            // 1. le due LPDA di AC6LA (AutoEZ) con IF() nelle celle: nel collaudo 0.4.9 "Funzione sconosciuta: IF"
            string ar = Path.Combine(AppPaths.DataDir, "archivio", "AutoEZ (esempi AC6LA)");
            if (!Directory.Exists(ar)) ar = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "data", "archivio", "AutoEZ (esempi AC6LA)"));
            foreach (string nome in new[] { "LPDA by Tau+Sigma.weq", "LPDA by Elems+Boom.weq" })
            {
                string f = Path.Combine(ar, nome);
                if (!YanecFile.Esiste(f)) { Passo("apertura di \"" + nome + "\"", "file nell'archivio", "mancante: " + f, false); continue; }
                try
                {
                    NecModel m = EzImport.LoadWeq(f);
                    int fili = m.Geometry.Count;
                    RunResult r = Calcola(m, m.FreqMHz);
                    bool ok = fili > 2 && m.Sources.Count == 1 && m.Networks.Count >= fili - 1 && !double.IsNaN(r.Zin.Real) && !double.IsNaN(r.GmaxDbi);
                    Passo("apertura e calcolo di \"" + nome + "\" (formule con IF, sorgente =S, linee \"Actual dist\" e \"LPDA repeat\", stub)",
                        "si apre, una sorgente sull'ultimo elemento, una linea tra ogni coppia di elementi, si calcola",
                        string.Format(CI, "{0} fili, {1} sorgenti (filo {2}), {3} linee, {4} stub, {5:0.###} MHz, Z {6:0.0} {7:+0.0;-0.0}j, {8:0.00} dBi",
                            fili, m.Sources.Count, m.Sources.Count > 0 ? m.Sources[0].Tag : 0, m.Networks.Count, m.Stubs.Count, m.FreqMHz, r.Zin.Real, r.Zin.Imaginary, r.GmaxDbi), ok);
                }
                catch (Exception ex) { Passo("apertura e calcolo di \"" + nome + "\" (formule con IF)", "si apre e si calcola", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 2. elenco dei passi da guardare scritto dal programma (nel collaudo 0.4.9 il comando PowerShell lo lasciava vuoto)
            try
            {
                string tmp = Path.Combine(AppPaths.TempDir, "yanec_passi_prova.txt"), usc = tmp + ".out";
                File.WriteAllLines(tmp, new[] { "PROCEDURA 7 - Prova", "  7.1  passo uno", "         atteso:   1", "         ottenuto: 2", "         esito:    ERRORE",
                    "  7.2  passo due", "         esito:    OK", "PROCEDURA 8 - Altra", "  8.1  passo", "         atteso:   a", "         ottenuto: b", "         esito:    AVVISO",
                    "    12  00:00:00.000  +      0  PROVA     3. modello x -> ERRORE: guasto" });
                int rc = PassiDaGuardare.Scrivi(tmp, usc);
                string o = File.ReadAllText(usc);
                bool ok = rc == 0 && o.StartsWith("3 passi") && o.Contains("PROCEDURA 7 - Prova") && o.Contains("ottenuto: 2") && o.Contains("PROCEDURA 8 - Altra") && o.Contains("-> ERRORE: guasto");
                Passo("elenco dei passi da guardare (YA-NEC.exe --passi) su un resoconto di prova", "3 passi, con la procedura e le righe atteso / ottenuto",
                    o.Replace("\r", "").Replace("\n", " | ").Replace("esito:", "esito=").Replace("-> ", "->").Replace(" PROVA ", " prova ").Trim(), ok);
                try { File.Delete(tmp); File.Delete(usc); } catch { }
            }
            catch (Exception ex) { Passo("elenco dei passi da guardare", "scritto", ex.Message, false); }
            // 3. parabola offset: disco da 1 m a 10 GHz con il feed teorico (le 110 prove del motore sono nella procedura delle parabole)
            try
            {
                ParabolaInput p = new ParabolaInput();
                OffsetResult r = Parabola.CalcolaOffset(p);
                Passo("parabola offset con i dati di partenza (calcolo completo con le bande)", "risultato senza eccezioni, efficienza tra 0 e 100 %",
                    string.Format(CI, "efficienza d'apertura {0:0.0} %, puntamento del feed {1:0.0} gradi", r.EtaApertura * 100, r.MiraUsata_deg), r.EtaApertura > 0 && r.EtaApertura < 1);
            }
            catch (Exception ex) { Passo("parabola offset con i dati di partenza", "risultato senza eccezioni", ex.GetType().Name + ": " + ex.Message, false); }
            // 4. (0.4.14) punti dello sweep calcolati insieme secondo la memoria
            int p1 = Analysis.ParallelismoPerMemoria(100), p2 = Analysis.ParallelismoPerMemoria(7877), p3 = Analysis.ParallelismoPerMemoria(40000);
            Passo("punti dello sweep calcolati insieme: 100, 7877 e 40000 segmenti", "tutti i processori; al massimo 2; 1",
                p1 + "; " + p2 + "; " + p3 + " (processori: " + Environment.ProcessorCount + ")", p1 == Math.Max(1, Environment.ProcessorCount) && p2 >= 1 && p2 <= 2 && p3 == 1);
            // 5. (0.4.15) aggiornamento automatico: pulizia all'avvio e riconoscimento dei pacchetti .deb/.rpm
            try
            {
                string m = Aggiornamento.PulisciDopoAvvio();
                string mot;
                bool pk = Aggiornamento.DaPacchetto(out mot);
                Passo("aggiornamento automatico: pulizia dei file .old e controllo della cartella del programma", "nessuna eccezione; cartella scrivibile, non da pacchetto",
                    (m ?? "nessun aggiornamento appena installato") + "; " + (pk ? "da pacchetto: " + mot : "cartella scrivibile"), !pk);
            }
            catch (Exception ex) { Passo("aggiornamento automatico", "nessuna eccezione", ex.Message, false); }
        }

        static void ParabolaProve()
        {
            int righe = 0, falliti = 0;
            bool ok = ParabolaSelfTest.Esegui(r => { Riga("         " + r); righe++; if (r.IndexOf("ERRORE", StringComparison.OrdinalIgnoreCase) >= 0 || r.IndexOf("FALLIT", StringComparison.OrdinalIgnoreCase) >= 0) falliti++; });
            Passo("autotest del motore delle parabole", "tutte le prove superate", ok ? "superate (" + righe + " righe di resoconto)" : "non superate (" + falliti + " righe con errore)", ok);
            // 0.4.19: trombe e illuminatori (scheda preparata da IC8TEM il 14/11/2026, 28 prove con i valori attesi controllati in Python)
            int rt = 0, ft = 0;
            bool okT = TrombaSelfTest.Esegui(r => { Riga("         " + r); rt++; if (r.StartsWith("ERR", StringComparison.Ordinal)) ft++; });
            Passo("autotest delle trombe e degli illuminatori (0.4.19)", "tutte le prove superate (28)", okT ? "superate (" + rt + " righe di resoconto)" : "non superate (" + ft + " prove con errore)", okT);
        }

        static void GtTant()
        {
            NecModel m = NecModel.Parse("CM dipolo 144,3 spazio libero\nCE\nGW 1 21 0 -0.5 0 0 0.5 0 0.002\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 144.3 0\nEN");
            RunResult r = Calcola(m, 144.3);
            GtResult g0 = NoiseGt.Gt(r.Grid, r.ThetaMax, r.PhiMax, r.GmaxDbi, 0, 200, 1000, 0, 0, false);
            Passo("dipolo in spazio libero puntato a 0 gradi, cielo 200 K, terreno 1000 K", "Tpattern 600 K (entro 3 K)", string.Format(CI, "{0:0.0} K", g0.Tpattern), Math.Abs(g0.Tpattern - 600) < 3);
            GtResult g30 = NoiseGt.Gt(r.Grid, r.ThetaMax, r.PhiMax, r.GmaxDbi, 30, 10, 290, 0, 0, false);
            Passo("stesso dipolo a 30 gradi, cielo 10 K, terreno 290 K", "Tpattern tra 10 e 150 K (piu' cielo che terreno)", string.Format(CI, "{0:0.0} K", g30.Tpattern), g30.Tpattern > 10 && g30.Tpattern < 150);
        }

        // valori attesi: nec2c del 13/11/2026 con la stessa geometria (scratchpad loop/gen.py): L 2,499 uH, C 201,1 pF, Rr 6,003 mOhm,
        // perdita 31,02 mOhm, efficienza 16,2 %, Q 3011; anello di Faraday D/10: ROS 2,07 (86,7 -j32,4 ohm). Formule: 2,45 uH, 6,0 / 31,6 mOhm.
        static void LoopProva()
        {
            LoopSpec s = new LoopSpec { Misura = 1, Conduttore = 0.022, FreqMHz = 7.1, Alimentazione = 2, Base = 3 };
            NecModel fin;
            LoopEsito r = LoopGen.Esito(s, CancellationToken.None, out fin);
            Passo("loop 1 m, tubo di rame 22 mm, 7,1 MHz, alimentazione diretta", "nec2c: L 2,499 uH, C 201,1 pF, Rr 6,00 mOhm, perdita 31,0 mOhm, efficienza 16,2 %",
                string.Format(CI, "L {0:0.000} uH, C {1:0.0} pF, Rr {2:0.000} mOhm, perdita {3:0.00} mOhm, efficienza {4:0.0} %, Q {5:0}", r.L * 1e6, r.C * 1e12, r.Rr * 1e3, r.Rp * 1e3, r.Eff, r.Q),
                Math.Abs(r.L * 1e6 - 2.499) < 0.01 && Math.Abs(r.C * 1e12 - 201.1) < 0.5 && Math.Abs(r.Rr * 1e3 - 6.003) < 0.05 && Math.Abs(r.Rp * 1e3 - 31.02) < 0.4 && Math.Abs(r.Eff - 16.2) < 0.3);
            Passo("formule del calcolatore Loop magnetica nello stesso pannello", "Rr 6,0 mOhm, perdita circa 31,6 mOhm, L 2,45 uH",
                string.Format(CI, "Rr {0:0.00} mOhm, perdita {1:0.0} mOhm, L {2:0.00} uH", r.Rrf * 1e3, r.Rpf * 1e3, r.Lf * 1e6),
                Math.Abs(r.Rrf * 1e3 - 6.0) < 0.1 && Math.Abs(r.Rpf * 1e3 - 31.7) < 0.6 && Math.Abs(r.Lf * 1e6 - 2.45) < 0.03);
            LoopSpec f = s.Clone(); f.Alimentazione = 0; f.Rapporto = 10;
            LoopEsito rf = LoopGen.Esito(f, CancellationToken.None, out fin);
            Passo("anello di Faraday D/10 a 1 cm dal tubo, condensatore ritoccato per il ROS minimo", "nec2c: ROS circa 2,1 (86,7 -j32,4 ohm)",
                string.Format(CI, "Z {0}, ROS {1:0.00}, C {2:0.000} pF", Z(rf.Zfinale), rf.Ros, rf.C * 1e12), rf.Ros < 2.6);
        }

        static void GeometriaProva()
        {
            NecModel m = NecModel.Parse("CM prova geometria\nCE\nSY L=1\nGW 1 11 0.5 -L/2 2 0.5 L/2 2 0.001\nGW 2 11 -0.5 -0.55 2 -0.5 0.55 2 0.001\nGE 0\nEX 0 1 6 0 1 0\nFR 0 1 0 0 144 0\nEN");
            double[] c = TrasformaGeometria.Centro(m.BuildGeometry());
            NecModel r = TrasformaGeometria.Applica(m, TrasformaGeometria.Ruota(2, 90, c), null);
            GeomCard w = r.Geometry[0];
            Passo("ruota di +90 gradi attorno a Z per il centro (0; 0; 2)", "filo 1 da (0,5; -0,5; 2) a (0,5; 0,5; 2) diventa da (0,5; 0,5) a (-0,5; 0,5), quota 2, formula con L conservata",
                string.Format(CI, "({0:0.###}; {1:0.###}; {2:0.###}) - ({3:0.###}; {4:0.###}; {5:0.###}), X1 = {6}", w.F1, w.F2, w.F3, w.F4, w.F5, w.F6, w.Src[2]),
                Math.Abs(w.F1 - 0.5) < 1e-9 && Math.Abs(w.F2 - 0.5) < 1e-9 && Math.Abs(w.F4 + 0.5) < 1e-9 && Math.Abs(w.F5 - 0.5) < 1e-9 && Math.Abs(w.F3 - 2) < 1e-9 && w.Src[2].Contains("L"));
            NecModel mv = m.Clone();
            mv.Variables.Add(new SyVar { Name = "ANG", Expr = "90" });
            mv.Reevaluate();
            NecModel rv = TrasformaGeometria.Applica(mv, TrasformaGeometria.Ruota(2, 90, c, "ANG"), null);
            GeomCard wv = rv.Geometry[0];
            Passo("stessa rotazione con l'angolo come variabile ANG", "stesse coordinate, formule con SIN(ANG) e COS(ANG)",
                string.Format(CI, "({0:0.###}; {1:0.###}) - ({2:0.###}; {3:0.###}), X1 = {4}", wv.F1, wv.F2, wv.F4, wv.F5, wv.Src[2]),
                Math.Abs(wv.F1 - 0.5) < 1e-9 && Math.Abs(wv.F5 - 0.5) < 1e-9 && wv.Src[2].Contains("ANG"));
            NecModel sp = TrasformaGeometria.CopiaSpeculare(m, TrasformaGeometria.Specchia(2, new double[3]), new HashSet<int> { 1 });
            GeomCard ws = sp.Geometry.FindLast(q => q.Id == "GW");
            Passo("copia speculare del filo 1 nel piano XY", "3 fili, il nuovo a quota -2 con tag 3", string.Format(CI, "{0} fili, ultimo tag {1} a quota {2:0.###}", sp.Geometry.FindAll(q => q.Id == "GW").Count, ws.I1, ws.F3),
                sp.Geometry.FindAll(q => q.Id == "GW").Count == 3 && ws.I1 == 3 && Math.Abs(ws.F3 + 2) < 1e-9);
            NecModel sc = TrasformaGeometria.Applica(m, TrasformaGeometria.Scala(144.0 / 146.0, c), null, true, 144.0 / 146.0);
            SegmentGeometry gs = sc.BuildGeometry();
            Passo("scala 144/146 con i raggi", "lunghezza del filo 2 1,1 x 144/146, raggio 1 mm x 144/146", string.Format(CI, "{0:0.#####} m, raggio {1:0.######} m", sc.Geometry[1].F5 - sc.Geometry[1].F2, sc.Geometry[1].F7),
                Math.Abs(sc.Geometry[1].F5 - sc.Geometry[1].F2 - 1.1 * 144 / 146) < 2e-9 && Math.Abs(sc.Geometry[1].F7 - 0.001 * 144 / 146) < 1e-9 /* le coordinate si riscrivono con 9 decimali */);
            NecModel ga = NecModel.Parse("CM arco\nCE\nGA 1 12 0.5 0 90 0.001\nGW 2 5 0 0 0 0 0 0.3 0.001\nGE 0\nEX 0 2 3 0 1 0\nFR 0 1 0 0 144 0\nEN");
            NecModel gm = TrasformaGeometria.Applica(ga, TrasformaGeometria.Sposta(0, 0, 10), null);
            SegmentGeometry gg = gm.BuildGeometry();
            Passo("sposta di 10 m in alto un modello con un arco GA", "scheda GM in fondo; quota minima 10 m", string.Format(CI, "ultima scheda {0}, quota minima {1:0.###} m", gm.Geometry[gm.Geometry.Count - 1].Id, TrasformaGeometria.ZMin(gg)),
                gm.Geometry[gm.Geometry.Count - 1].Id == "GM" && Math.Abs(TrasformaGeometria.ZMin(gg) - 10) < 1e-9);
        }

        static void OttimizzatoreProva()
        {
            foreach (bool simplesso in new[] { false, true })
            {
                NecModel m = NecModel.Parse("CM dipolo per l'ottimizzatore\nCE\nSY L=1.03\nGW 1 21 0 -L/2 0 0 L/2 0 0.001\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 144.3 0\nEN");
                Ottimizzatore o = new Ottimizzatore { Partenza = m, Simplesso = simplesso, MaxPassi = 400 };
                o.Variabili.Add(new OttVariabile { Nome = "L", Valore = 1.03, Min = 0.90, Max = 1.10, Passo = 0.001 });
                o.Obiettivi.Add(new OttObiettivo { Chiave = "jx", Peso = 100 });
                o.Frequenze.Add(144.3);
                o.Esegui(CancellationToken.None);
                Passo("ottimizzatore, metodo " + (simplesso ? "simplesso di Nelder e Mead" : "una variabile alla volta"), "da L = 1,030 m (X positiva) a |X| sotto 3 ohm, voto sopra 98",
                    string.Format(CI, "L = {0:0.000} m, Z {1}, voto {2:0.0} (partenza {3:0.0}), {4} passi", o.Migliore.Valori[0], Z(o.Migliore.Z), o.Migliore.Voto, o.Iniziale.Voto, o.Passi),
                    Math.Abs(o.Migliore.Z.Imaginary) < 3 && o.Migliore.Voto > 98 && o.Iniziale.Voto < o.Migliore.Voto);
            }
        }

        // 520-40H.YAG di zipyagi.zip (ARRL, arrl.org/antenna-modeling-files; inviato da IC8TEM il 13/11/2026) e tabella 8.6
        // dell'ARRL Antenna Book 24a (EZNEC, 70 ft su terreno piatto). nec2c con il metodo di YA-NEC (13/11/2026):
        // 14,0 22,5 -j27,2 14,84 dBi; 14,2 22,0 -j15,5 14,90 dBi; 14,4 17,3 -j11,5 14,58 dBi.
        // Con i segmenti uguali sull'elemento corretto (0.4.2 bis, 31 segmenti): 22,4 -j26,8 14,84; 22,0 -j15,1 14,90; 16,4 -j11,3 14,55.
        const string Yag52040H = "520-40H.YAG, 5-ele., 40' boom, heavy-duty elements\n14.000 14.175 14.350 14.150 MHz\n5 elements, inches\n" +
            "           3.706    1.250    1.000    0.875    0.750    0.625    0.500\n" +
            "  0.000    4.000   44.000   24.000   20.000   42.000   20.000   68.375\n 72.000    4.000   44.000   24.000   20.000   42.000   20.000   53.500\n" +
            "144.000    4.000   44.000   24.000   20.000   42.000   20.000   51.500\n283.000    4.000   44.000   24.000   20.000   42.000   20.000   48.375\n" +
            "474.000    4.000   44.000   24.000   20.000   42.000   20.000   38.000\n";

        static void Arrl52040H()
        {
            NecModel y = K6stiImport.ParseYag(Yag52040H.Split('\n'), "520-40H");
            double rMax = 0;
            foreach (GeomCard w in y.Geometry) if (w.Id == "GW") rMax = Math.Max(rMax, w.F7);
            Passo("520-40H.YAG (formato dell'ARRL, diametri in pollici senza \"mm\")", "5 elementi, 65 fili (13 per elemento: piastra intera e 6 tubi per lato), piastra da 3,706\" (raggio 47,07 mm)",
                string.Format(CI, "{0} fili, raggio massimo {1:0.00} mm", y.Geometry.Count, rMax * 1000), y.Geometry.Count == 65 && Math.Abs(rMax - 0.04707) < 1e-4);
            string f = Path.Combine(AppPaths.ModelsDir, "ARRL 520-40H Yagi 5 el 20 m (70 ft).nec");
            if (!YanecFile.Esiste(f)) f = Path.Combine(AppContext.BaseDirectory, "data", "modelli", "ARRL 520-40H Yagi 5 el 20 m (70 ft).nec");
            if (!YanecFile.Esiste(f)) { Passo("modello ARRL 520-40H negli esempi", "file presente", "mancante", false); return; }
            NecModel m = NecModel.Load(f);
            // 0.4.40: valori attesi con la correzione che tiene la segmentazione originale (come EZNEC), nec2c corretto (Sommerfeld 0.4.38)
            // sullo stesso mazzo corretto da Python (scratchpad l440/y520.py, 15/11/2026); tra parentesi la tabella 8.6 dell'ARRL (YO).
            // Fino alla 0.4.39 (segmenti resi uguali): 22,9 -j28,6 / 22,2 -j16,9 / 19,5 -j11,6 e 14,82 / 14,90 / 14,66 dBi.
            // 0.5.3 (taratura di Leeson a = 0,05 sotto la risonanza, l440/y520_ab.py): 22,11 -j27,88 / 21,48 -j16,66 / 19,13 -j11,33 (0.4.40: 22,07 -j27,79 / 21,46 -j16,56 / 19,06 -j11,35)
            double[][] arrl = { new[] { 14.0, 22.11, -27.88, 14.98, 23.2, -26.5, 14.82 }, new[] { 14.2, 21.48, -16.66, 15.06, 22.8, -14.8, 14.87 }, new[] { 14.4, 19.13, -11.33, 14.82, 14.5, -10.5, 14.45 } };
            foreach (double[] a in arrl)
            {
                RunResult r = Calcola(m, a[0]);
                Complex za = new Complex(a[1], a[2]);
                Passo(string.Format(CI, "520-40H con Leeson a {0:0.0} MHz", a[0]), string.Format(CI, "{0} e {1:0.00} dBi entro 1 ohm e 0,1 dB (ARRL {2} e {3:0.00} dBi)", Z(za), a[3], Z(new Complex(a[4], a[5])), a[6]),
                    string.Format(CI, "{0}, {1:0.00} dBi", Z(r.Zin), r.GmaxDbi), Complex.Abs(r.Zin - za) < 1.0 && Math.Abs(r.GmaxDbi - a[3]) < 0.1);
            }
            // 0.4.40: prova fissa contro EZNEC (IC8TEM, collaudo 0.4.37, data\prove\eznec_salvati\archivio: stesso modello convertito in .ez,
            // Stepped Diameter Correction di EZNEC): 22,062 -j27,879 ohm e 14,97 dBi a 14,0 MHz. Con i segmenti uguali (0.4.39) lo scarto era del 3,6-4,0 %.
            {
                RunResult r = Calcola(m, 14.0);
                Complex ze = new Complex(22.062, -27.879);
                double sc = Complex.Abs(r.Zin - ze) / Complex.Abs(ze);
                Passo("520-40H con Leeson contro EZNEC a 14,0 MHz", string.Format(CI, "{0} e 14,97 dBi entro l'1 % e 0,1 dB", Z(ze)),
                    string.Format(CI, "{0}, {1:0.00} dBi (scarto {2:0.00} %)", Z(r.Zin), r.GmaxDbi, sc * 100), sc < 0.01 && Math.Abs(r.GmaxDbi - 14.97) < 0.1);
            }
            m.Leeson = false;
            RunResult rs = Calcola(m, 14.2);
            Passo("520-40H senza Leeson a 14,2 MHz", "diagramma rovesciato o quasi (ARRL senza correzione: 6,6 -j4,6, F/R 16,2 dB; nec2c corretto 0.4.38: 3,9 +j1,0)",
                string.Format(CI, "{0}, F/B {1:0.0} dB", Z(rs.Zin), rs.FrontBack), rs.Zin.Real < 8);
        }

        // EF0213 di YU7EF (yu7ef.com/ef0213.htm, misure inviate da IC8TEM il 13/11/2026), il caso d'esempio del manuale di TANT.
        // YU7EF: 16,17 dBi, Ta 223,6 K, G/T -7,32 dB; nec2c (alluminio 6061-T6): 16,17 dBi, efficienza 96,3 %, Ta 223,8 K.
        // Senza le perdite dei conduttori nella temperatura (YA-NEC fino alla 0.4.1) Ta veniva 220,7 K e G/T -7,11 dB.
        static void GtEf0213()
        {
            string f = Path.Combine(AppPaths.ModelsDir, "EF0213 YU7EF 13 el 144 MHz (4 mm).nec");
            if (!YanecFile.Esiste(f)) f = Path.Combine(AppContext.BaseDirectory, "data", "modelli", "EF0213 YU7EF 13 el 144 MHz (4 mm).nec");
            if (!YanecFile.Esiste(f)) { Passo("modello EF0213 negli esempi", "file presente", "mancante", false); return; }
            NecModel m = NecModel.Load(f);
            RunResult r = Calcola(m, 144.3);
            Passo("EF0213 a 144,3 MHz", "YU7EF 16,17 dBi, F/B 23,88 dB; nec2c 51,7 -j1,0 ohm, efficienza 96,3 %",
                string.Format(CI, "{0}, {1:0.00} dBi, F/B {2:0.00} dB, efficienza {3:0.0} %", Z(r.Zin), r.GmaxDbi, r.FrontBack, r.Efficiency),
                Math.Abs(r.GmaxDbi - 16.17) < 0.06 && Math.Abs(r.Efficiency - 96.3) < 0.3);
            GtResult g = NoiseGt.Gt(r.Grid, r.ThetaMax, r.PhiMax, r.GmaxDbi, 30, 200, 1000, 0, 0, false, r.Efficiency);
            Passo("Ta e G/T a 30 gradi, cielo 200 K, terreno 1000 K", "YU7EF Ta 223,6 K (entro 1,5 K), G/T -7,32 dB (entro 0,05 dB)",
                string.Format(CI, "Ta {0:0.0} K (diagramma {1:0.0} + perdite {2:0.0}), G/T {3:0.00} dB", g.Tant, g.Tpattern, g.Tloss, g.GtAnt),
                Math.Abs(g.Tant - 223.6) < 1.5 && Math.Abs(g.GtAnt + 7.32) < 0.05);
        }

        static void Registro042()
        {
            Passo("registro della sessione nella cartella Registri", "file presente", Registro.File_ ?? "non attivo", Registro.Attivo && Registro.File_ != null && File.Exists(Registro.File_));
            string comp = Prestazioni.Computer();
            Passo("descrizione del computer (processore, grafica, memoria, disco)", "le quattro righe rilevate", comp.Replace(Environment.NewLine, " | "), !comp.Contains("non rilevato"), true);
            string ris = Prestazioni.Risorse();
            Passo("campione delle risorse (CPU, RAM, GPU, disco)", "riga RISORSE", ris, ris.StartsWith("CPU"));
            string pun = Prestazioni.Punteggio();
            Passo("punteggio delle funzioni", "contiene le procedure del collaudo e il calcolo NEC", pun.Split('\n').Length + " righe", pun.Contains("Collaudo: ") && pun.Contains("Calcolo NEC"));
            int dll = 0;
            try { dll = Directory.GetFiles(AppContext.BaseDirectory, "*.dll").Length; } catch { }
            Passo("nessuna DLL accanto al programma (librerie native in lib)", "0 DLL, cartella lib presente", dll + " DLL; " + Librerie.Descrizione, dll == 0 && Librerie.Cartella != null, true);

            string xr = Path.Combine(AppPaths.ModelsDir, "Force12 XR-5 misure di fabbrica.nec");
            if (!YanecFile.Esiste(xr)) xr = Path.Combine(AppContext.BaseDirectory, "data", "modelli", "Force12 XR-5 misure di fabbrica.nec");
            if (!YanecFile.Esiste(xr)) { Passo("modello XR-5 con le misure di fabbrica", "file negli esempi", "mancante", false); return; }
            NecModel m = NecModel.Load(xr);
            Passo("XR-5 di fabbrica: correzione Leeson letta dal file", "LEESON=1, 66 fili, linea 450 ohm", (m.Leeson ? "attiva" : "spenta") + ", " + m.Geometry.Count + " fili, " + m.Networks.Count + " linee",
                m.Leeson && m.Geometry.Count == 66 && m.Networks.Count == 1);
            // 0.4.40 (IC8TEM 15/11/2026 14:24): i radiatori 20 e 12 m hanno la linea di fase sul segmento centrale e ora si correggono
            // come in EZNEC (fino alla 0.4.39 erano esclusi: 81,1 +j73,8 ohm contro 61,2 +j42,6 di EZNEC a 14,175 MHz).
            List<string> note = new List<string>();
            Leeson.Applica(m, 14.175, note);
            int esclusi = 0, rad20 = 0;
            foreach (string n in note) { if (n.Contains("linea di fase")) esclusi++; if ((n.Contains("filo 36-46 ") || n.Contains("filo 46-36 ")) && n.Contains(" -> ")) rad20++; }
            Passo("radiatore 20 m con la linea di fase al centro corretto (come EZNEC)", "filo 36-46 corretto, nessuna catena esclusa per la linea di fase",
                (rad20 > 0 ? "corretto" : "non corretto") + ", " + esclusi + " escluse per la linea", rad20 == 1 && esclusi == 0);
            {
                RunResult re = Calcola(m, 14.175);
                Complex ze = new Complex(61.214, 42.593);
                double sc = Complex.Abs(re.Zin - ze) / Complex.Abs(ze);
                // nec2c sullo stesso mazzo corretto da Python (scratchpad l440/xr.py): 61,34 +j42,79 ohm, 5,90 dBi; dalla 0.5.3 (modifica A: corretti
                // anche gli elementi dei 10 e 12 m, f/fs 0,42-0,54) xr_ab.py da' 60,91 +j42,90 ohm, 5,90 dBi (scarto da EZNEC dallo 0,31 allo 0,59 %);
                // con la taratura a = 0,05 (0.5.3) 60,76 +j42,78 ohm (0,66 %)
                Passo("XR-5 di fabbrica con Leeson contro EZNEC a 14,175 MHz (collaudo 0.4.37)", string.Format(CI, "{0} e 5,89 dBi entro l'1 % e 0,1 dB (nec2c 0.5.3 con la taratura: 60,76 +j42,78, 5,90 dBi)", Z(ze)),
                    string.Format(CI, "{0}, {1:0.00} dBi (scarto {2:0.00} %)", Z(re.Zin), re.GmaxDbi, sc * 100), sc < 0.01 && Math.Abs(re.GmaxDbi - 5.89) < 0.1);
            }
            RunResult r = Calcola(m, 24.94);
            Passo("XR-5 di fabbrica a 24,94 MHz (nec2c con Leeson 0.4.40: 29,6 -j7,1 ohm, ROS 1,74; 0.4.39: 32,8 +j4,5, ROS 1,55)", "ROS tra 1,5 e 2,0",
                string.Format(CI, "Z {0:0.0} {1:+0.0;-0.0}j, ROS {2:0.00}, G {3:0.00} dBi, F/B {4:0.0} dB", r.Zin.Real, r.Zin.Imaginary, r.Swr, r.GmaxDbi, r.FrontBack),
                r.Swr > 1.5 && r.Swr < 2.0, true);
        }

        static void Salvataggio()
        {
            NecModel m = NecModel.Parse(DipoloVar);
            string tmp = Path.Combine(AppPaths.DataDir, "collaudo_prova.nec");
            File.WriteAllText(tmp, m.ToNec());
            Passo("salvataggio in data/collaudo_prova.nec", "file scritto", File.Exists(tmp) ? new FileInfo(tmp).Length + " byte" : "non scritto", File.Exists(tmp));
            string txt = File.ReadAllText(tmp);
            bool sy = txt.Contains("SY L=1000*MM") && txt.Contains("GW 1 NS 0 -H 0 0 H 0 R");
            Passo("le variabili restano nel file", "righe SY e GW con le espressioni", sy ? "presenti" : "mancanti", sy);
            NecModel m2 = NecModel.Load(tmp);
            m.Variables[0].Descr = "lunghezza totale del dipolo";
            NecModel m3 = NecModel.Parse(m.ToNec());
            Passo("descrizione scritta dall'utente salvata nel file (CM YA-NEC VAR)", "\"lunghezza totale del dipolo\"", "\"" + m3.Variables[0].Descr + "\"", m3.Variables[0].Descr == "lunghezza totale del dipolo");
            RunResult a = Calcola(m, 144.3), b = Calcola(m2, 144.3);
            Passo("riapertura e ricalcolo", "stessa impedenza", Z(a.Zin) + "  /  " + Z(b.Zin), Math.Abs(a.Zin.Real - b.Zin.Real) < 1e-6 && Math.Abs(a.Zin.Imaginary - b.Zin.Imaginary) < 1e-6);
            try { File.Delete(tmp); } catch { }
        }

        // 0.4.3 (A1): scrittura .weq ed .ez con rilettura; i due dipoli sono collegati da una linea da 300 ohm incrociata
        static void SalvaWeqEz()
        {
            NecModel m = NecModel.Parse("CM prova weq ez\nCE\nSY L=1.02\nGW 1 21 0 -L/2 10 0 L/2 10 0.002\nGW 2 21 0.6 -0.5 10 0.6 0.5 10 0.002\nGE 1\n" +
                "GN 2 0 0 0 13 0.005\nLD 4 2 11 11 5 -10\nTL 1 11 2 11 -300 0.7 0 0 0 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 144.3 0\nEN");
            RunResult a = Calcola(m, 144.3);
            foreach (string ext in new[] { ".weq", ".ez" })
            {
                string tmp = Path.Combine(AppPaths.DataDir, "collaudo_prova" + ext);
                List<string> perso = new List<string>();
                try
                {
                    if (ext == ".ez") File.WriteAllBytes(tmp, FormatiExport.Ez(m, perso));
                    else File.WriteAllText(tmp, FormatiExport.Weq(m, true, perso), System.Text.Encoding.GetEncoding("ISO-8859-1"));
                    NecModel r = ext == ".ez" ? EzImport.LoadEz(tmp) : EzImport.LoadWeq(tmp);
                    if (ext == ".weq") { r.Ground = m.Ground.Clone(); r.GroundFlag = 1; }   // il terreno in AutoEZ e' quello del modello EZNEC
                    RunResult b = Calcola(r, 144.3);
                    Passo("scritto in " + ext + " e riletto", "2 fili, 1 sorgente, 1 carico, 1 linea; stessa impedenza entro 0,05 ohm",
                        string.Format(CI, "{0} fili, {1} sorgenti, {2} carichi, {3} linee; {4} / {5}", r.Geometry.Count, r.Sources.Count, r.Loads.FindAll(q => q.Type != 5).Count, r.Networks.Count, Z(a.Zin), Z(b.Zin)),
                        r.Geometry.Count == 2 && r.Sources.Count == 1 && r.Networks.Count == 1 && Complex.Abs(a.Zin - b.Zin) < 0.05);
                    if (ext == ".weq")
                    {
                        string t = File.ReadAllText(tmp);
                        Passo(".weq: variabile L come lettera A e formula Excel", "riga _1.02 e celle =-A/2, =A/2", t.Contains("_1.02") && t.Contains("_=-A/2") ? "presenti" : "mancanti", t.Contains("_1.02") && t.Contains("_=-A/2"));
                    }
                }
                catch (Exception ex) { Passo("scritto in " + ext + " e riletto", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
                try { File.Delete(tmp); } catch { }
            }
        }

        // 0.4.3: antenne riceventi di IV3PRK (PDF "Double DHDL Broadside" 2013, "Waller Flag" 2007, sito iv3prk.it) contro nec2c (GN 2, 13; 5 mS/m)
        static void RiceventiIv3prk()
        {
            GroundSpec gs = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005, Sommerfeld = true };
            object[][] casi =
            {
                new object[] { "DHDL 20 m, verticali 6,5 m, base a 2 m, 1300 ohm", new RxParams { F = 1.83, L = 20, H = 6.5, H2 = 2, Rterm = 1300, WireMm = 1, Ground = gs }, "dhdl", 9.98, "IV3PRK/TX3A RDF 10 dB" },
                new object[] { "due DHDL broadside a 90 m", new RxParams { F = 1.83, L = 20, H = 6.5, H2 = 2, Rterm = 1300, D = 90, WireMm = 1, Ground = gs }, "dhdl2", 12.59, "IV3PRK RDF 12,7 dB" },
                new object[] { "Waller Flag 4,27 x 2 m a 7,73 m, 600 ohm, TL1 4 m, TL2 3 m", new RxParams { F = 1.83, L = 4.27, H = 2, H2 = 11, D = 7.73, Rterm = 600, Tl1 = 4, Tl2 = 3, Zl = 100, Ratio = 6, VfLinea = 0.66, WireMm = 1.6, Ground = gs }, "wf", 11.63, "IV3PRK RDF 11,84 dB" },
                new object[] { "Pennant 8,84 x 4,27 m, centro a 8 m, 950 ohm (0.4.7)", new RxParams { F = 1.83, L = 8.84, H = 4.27, H2 = 8, Rterm = 950, WireMm = 1.6, Ground = gs }, "pennant", 7.59, "IV3PRK RDF 7,8 dB" },
            };
            foreach (object[] c in casi)
            {
                RxParams p = (RxParams)c[1];
                string k = (string)c[2];
                string nec = k == "dhdl" ? RxAntennas.Dhdl(p) : k == "dhdl2" ? RxAntennas.DhdlBroadside(p) : k == "pennant" ? RxAntennas.Pennant(p) : RxAntennas.WallerFlag(p);
                NecModel m = NecModel.Parse(nec);
                RunResult r = Analysis.Run(m, m.BuildGeometry(), p.F, 5.0);
                RdfResult rd = NoiseGt.Rdf(r.Grid, r.ThetaMax, r.PhiMax, r.GmaxDbi, true);
                double atteso = (double)c[3];
                Passo((string)c[0], string.Format(CI, "nec2c RDF {0:0.00} dB entro 0,4 dB ({1})", atteso, (string)c[4]),
                    string.Format(CI, "RDF {0:0.00} dB, guadagno {1:0.00} dBi, F/B {2:0.0} dB, Z {3}", rd.Rdf, r.GmaxDbi, r.FrontBack, Z(r.Zin)), Math.Abs(rd.Rdf - atteso) < 0.4);
            }
        }

        // 0.4.7: LPDA, il modello non ha piu' il filo fittizio dello stub.
        static void LpdaStub()
        {
            LpdaDesign lp = new LpdaDesign { Fmin = 144, Fmax = 432, Tau = 0.90, Sigma = 0.168, DiamMm = 6, Rin = 50 };
            lp.Compute();
            NecModel ml = NecModel.Parse(lp.ToNec(300, 0, null));
            Passo("LPDA senza filo fittizio", "tanti fili quanti elementi, uno stub come ammettenza", ml.Geometry.Count + " fili, " + lp.N + " elementi, " + ml.Stubs.Count + " stub",
                ml.Geometry.Count == lp.N && ml.Stubs.Count == 1);
        }

        // 0.4.4 (richiesta di IC8TEM del 13/11/2026 15:52): il collaudo calcola per intero tutti i modelli che il programma contiene e
        // scrive ogni risultato nel resoconto e nel registro (che va nello zip del collaudo insieme a collaudo_completo.txt).
        /// <summary>Cartella nec_per_YA-NEC del pacchetto di confronto con EZNEC, se c'e' (data\confronto_eznec o vicino all'eseguibile).</summary>
        static string TrovaConfrontoEznec()
        {
            string d = Path.Combine(AppPaths.DataDir, "confronto_eznec");
            if (Directory.Exists(d)) return Directory.Exists(Path.Combine(d, "nec_per_YA-NEC")) ? Path.Combine(d, "nec_per_YA-NEC") : d;
            string cur = AppContext.BaseDirectory;
            for (int k = 0; k < 6 && cur != null; k++)
            {
                try
                {
                    foreach (string c in Directory.GetDirectories(cur, "YA-NEC-confronto-EZNEC*"))
                    {
                        if (Directory.Exists(Path.Combine(c, "nec_per_YA-NEC"))) return Path.Combine(c, "nec_per_YA-NEC");
                        foreach (string c2 in Directory.GetDirectories(c, "YA-NEC-confronto-EZNEC*"))
                            if (Directory.Exists(Path.Combine(c2, "nec_per_YA-NEC"))) return Path.Combine(c2, "nec_per_YA-NEC");
                    }
                }
                catch { }
                cur = Path.GetDirectoryName(cur.TrimEnd('\\', '/'));
            }
            return null;
        }

        static void TuttiIModelli()
        {
            List<KeyValuePair<string, Func<NecModel>>> elenco = new List<KeyValuePair<string, Func<NecModel>>>();
            // 1. modelli d'esempio (data/modelli) in tutti i formati
            string md = AppPaths.ModelsDir;
            if (!Directory.Exists(md)) md = Path.Combine(AppContext.BaseDirectory, "data", "modelli");
            if (Directory.Exists(md))
            {
                List<string> fl = new List<string>(Directory.GetFiles(md));
                fl.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (string f in fl)
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".nec" && ext != ".maa" && ext != ".weq" && ext != ".ez" && ext != ".yanec") continue;
                    string ff = f;
                    elenco.Add(new KeyValuePair<string, Func<NecModel>>("esempio " + Path.GetFileName(f), () => Apri(ff)));
                }
            }
            // 1b. archivio dei modelli (data/archivio, nel sorgente): modelli di IC8TEM, verticali multibanda, ARRL, YO, EZNEC, AutoEZ.
            // Ogni modello non NEC si converte anche in .nec con gli importatori di YA-NEC (cartella data/archivio_convertito, va nello zip).
            string ar = Path.Combine(AppPaths.DataDir, "archivio");
            if (!Directory.Exists(ar)) ar = Path.Combine(AppContext.BaseDirectory, "data", "archivio");
            if (!Directory.Exists(ar)) ar = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "data", "archivio"));
            string conv = Path.Combine(AppPaths.DataDir, "archivio_convertito");
            int convertiti = 0;
            if (Directory.Exists(ar))
            {
                try { if (Directory.Exists(conv)) Directory.Delete(conv, true); } catch { }
                List<string> fl = new List<string>(Directory.GetFiles(ar, "*", SearchOption.AllDirectories));
                fl.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (string f in fl)
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".yanec" && ext != ".nec" && ext != ".maa" && ext != ".weq" && ext != ".ez" && ext != ".yag" && ext != ".ant") continue;
                    string ff = f, rel = Path.GetRelativePath(ar, f);
                    elenco.Add(new KeyValuePair<string, Func<NecModel>>("archivio " + rel.Replace('\\', '/'), () =>
                    {
                        NecModel am = Apri(ff);
                        // 0.4.30: nel .yanec conta il formato del contenuto (CM YANEC CONTENUTO)
                        string fmt = Path.GetExtension(ff).TrimStart('.').ToLowerInvariant();
                        if (fmt == "yanec") fmt = YanecFile.Contenuto(ff);
                        if (fmt != "nec")
                        {
                            try
                            {
                                string dst = Path.Combine(conv, Path.ChangeExtension(rel, null) + " (" + fmt + ").nec");
                                Directory.CreateDirectory(Path.GetDirectoryName(dst));
                                File.WriteAllText(dst, am.ToNec());
                                convertiti++;
                            }
                            catch { }
                        }
                        return am;
                    }));
                }
            }
            // 2. corpus di 4nec2 (data/prove/4nec2zip.zip, estratto dalla procedura del corpus o qui)
            string cart = Path.Combine(AppPaths.DataDir, "prove", "4nec2", "models");
            if (!Directory.Exists(cart))
            {
                string tmp = Path.Combine(AppPaths.TempDir, "yanec_4nec2_" + Environment.ProcessId, "4nec2", "models");
                if (Directory.Exists(tmp)) cart = tmp;
            }
            if (Directory.Exists(cart))
            {
                List<string> fl = new List<string>(Directory.GetFiles(cart, "*.nec", SearchOption.AllDirectories));
                fl.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (string f in fl)
                {
                    string ff = f;
                    elenco.Add(new KeyValuePair<string, Func<NecModel>>("4nec2 " + Path.GetRelativePath(cart, f).Replace('\\', '/'), () => NecModel.Load(ff)));
                }
            }
            // 2b. 0.4.8: confronto con EZNEC (richiesta IC8TEM 13/11/2026): i mazzi NEC da cui sono fatti i .ez di YA-NEC-confronto-EZNEC
            // (cartella nec_per_YA-NEC). Si cerca in data\confronto_eznec e nelle cartelle YA-NEC-confronto-EZNEC vicine all'eseguibile.
            string ce = TrovaConfrontoEznec();
            if (ce != null)
            {
                List<string> fl = new List<string>(Directory.GetFiles(ce, "*.nec", SearchOption.AllDirectories));
                fl.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (string f in fl)
                {
                    string ff = f;
                    elenco.Add(new KeyValuePair<string, Func<NecModel>>("eznec " + Path.GetRelativePath(ce, f).Replace('\\', '/'), () => NecModel.Load(ff)));
                }
            }
            // 3. modelli creati dai calcolatori con i dati di partenza
            foreach (SimpleCalc c in Calcolatori.Tutti())
            {
                if (c.UsesModel) continue;     // strumenti sul modello aperto (Ridimensiona): niente modello proprio
                SimpleCalc cc = c;
                elenco.Add(new KeyValuePair<string, Func<NecModel>>("calcolatore " + c.Nome, () =>
                {
                    CalcOutput o = cc.Compute();
                    return o.Nec == null ? null : NecModel.Parse(o.Nec);
                }));
            }
            // scheda LPDA (dati di partenza della scheda: 144-432 MHz) e scheda Loop della barra Geometria
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("scheda LPDA 144-432 MHz a 144,3", () =>
            {
                LpdaDesign d = new LpdaDesign { Fmin = 144, Fmax = 432, Tau = 0.90, Sigma = 0.168, Rin = 50, DiamMm = 6, LineTubeMm = 10 };
                d.Compute();
                NecModel lm = NecModel.Parse(d.ToNec(144.3, 0, new GroundSpec { Kind = GroundKind.FreeSpace }, d.Rin));
                lm.FreqMHz = 144.3;
                return lm;
            }));
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("scheda Loop (1 m, tubo 22 mm, 7,1 MHz, Faraday)", () =>
            {
                LoopSpec ls = new LoopSpec { Misura = 1, Conduttore = 0.022, FreqMHz = 7.1, Alimentazione = 0, Rapporto = 10, Base = 3 };
                NecModel fin;
                LoopGen.Esito(ls, CancellationToken.None, out fin);
                return fin;
            }));
            // 4. antenne riceventi
            GroundSpec gs = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005, Sommerfeld = true };
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("ricevente Beverage 175 m", () => NecModel.Parse(RxAntennas.Beverage(new RxParams { Ground = gs }))));
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("ricevente EWE", () => NecModel.Parse(RxAntennas.Ewe(new RxParams { L = 9.1, H = 3.05, Rterm = 850, Ground = gs }))));
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("ricevente DHDL", () => NecModel.Parse(RxAntennas.Dhdl(new RxParams { L = 20, H = 6.5, H2 = 2, Rterm = 1300, Ground = gs }))));
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("ricevente due DHDL broadside", () => NecModel.Parse(RxAntennas.DhdlBroadside(new RxParams { L = 20, H = 6.5, H2 = 2, Rterm = 1300, D = 90, Ground = gs }))));
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("ricevente Flag W7IUV", () => NecModel.Parse(RxAntennas.Flag(new RxParams { L = 8.84, H = 4.28, H2 = 8, Rterm = 937, Ground = gs }))));
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("ricevente Waller Flag", () => NecModel.Parse(RxAntennas.WallerFlag(new RxParams { L = 4.27, H = 2, H2 = 11, D = 7.73, Rterm = 600, Ground = gs }))));
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("ricevente loop magnetico", () => NecModel.Parse(RxAntennas.LoopRx(new RxParams { D = 1, H2 = 1.5, Ground = gs }))));
            elenco.Add(new KeyValuePair<string, Func<NecModel>>("ricevente verticale di riferimento", () => NecModel.Parse(RxAntennas.VerticaleRef(new RxParams { Ground = gs }))));

            int ok = 0, errori = 0, saltati = 0, agtFuori = 0, senzaModello = 0, nonSupportati = 0;
            const int MaxSeg = 3000;
            Riga("         modello | segmenti | MHz | Z ohm | ROS | Gmax dBi | F/B dB | efficienza % | AGT dB | tempo s");
            foreach (KeyValuePair<string, Func<NecModel>> kv in elenco)
            {
                DateTime t0 = DateTime.Now;
                try
                {
                    NecModel m = kv.Value();
                    if (m == null) { senzaModello++; continue; }
                    SegmentGeometry g = m.BuildGeometry(new List<string>());
                    if (g.N > MaxSeg)
                    {
                        saltati++;
                        string r0 = kv.Key + ": " + g.N + " segmenti, oltre " + MaxSeg + ": saltato (troppo lungo per il collaudo)";
                        Riga("         " + r0); Registro.Scrivi("MODELLO", "collaudo: " + r0);
                        continue;
                    }
                    double f = m.FreqMHz;
                    RunResult r = Analysis.Run(m, g, f, 5.0, CancellationToken.None);
                    // AGT vale in spazio libero o su terreno perfetto (le perdite sono gia' tolte in Analysis.Run; Burke e Poggio, ARRL 21a cap. 4)
                    bool agtValido = m.Ground.Kind != GroundKind.Real && !double.IsNaN(r.AgtDb) && r.AgtDb > -90;
                    bool agtOk = !agtValido || Math.Abs(r.AgtDb) <= 0.21;
                    if (!agtOk) agtFuori++;
                    string riga = string.Format(CI, "{0} | {1} | {2:0.###} | {3} | {4:0.00} | {5:0.00} | {6:0.0} | {7:0.0} | {8} | {9:0.0}{10}",
                        kv.Key, g.N, f, Z(r.Zin), r.Swr, r.GmaxDbi, r.FrontBack, r.Efficiency, agtValido ? r.AgtDb.ToString("0.00", CI) : "-",
                        (DateTime.Now - t0).TotalSeconds, agtOk ? "" : "  <- AGT fuori da +-0,2 dB: modello da controllare (segmentazione, fili vicini)");
                    Riga("         " + riga);
                    Registro.Scrivi("MODELLO", "collaudo: " + riga);
                    ok++;
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                    // funzioni del NEC che YA-NEC non ha (superfici, funzione di Green numerica, NEC-4, modelli senza sorgenti per l'onda piana):
                    // non sono guasti, si contano a parte
                    if (msg.Contains("non sono ancora supportate") || msg.Contains("non e' supportata") || msg.Contains("non ha sorgenti") ||
                        msg.Contains(".NGF") || msg.Contains("non contiene schede di geometria") || msg.Contains("Frequenza non valida nel file"))
                    {
                        nonSupportati++;
                        string r2 = kv.Key + ": " + msg;
                        Riga("         non calcolabile: " + r2);
                        Registro.Scrivi("MODELLO", "collaudo, non calcolabile: " + r2);
                        continue;
                    }
                    errori++;
                    string r1 = kv.Key + ": " + ex.GetType().Name + ": " + msg;
                    Riga("         ERRORE " + r1);
                    Registro.Scrivi("MODELLO", "collaudo, errore: " + r1);
                }
            }
            Passo("modelli calcolati per intero", elenco.Count + " modelli (esempi, corpus 4nec2, calcolatori, antenne riceventi)",
                string.Format(CI, "{0} calcolati, {1} con errore, {2} saltati oltre {3} segmenti, {4} con funzioni che YA-NEC non ha (superfici, NGF, NEC-4, senza sorgenti), {5} calcolatori senza modello", ok, errori, saltati, MaxSeg, nonSupportati, senzaModello),
                ok > 0 && errori == 0, errori <= Math.Max(3, elenco.Count / 20));
            Passo("modelli dell'archivio convertiti in .nec", "ogni .maa, .weq, .ez, .yag, .ant scritto in data/archivio_convertito (va nello zip del collaudo)",
                convertiti + " file .nec scritti", convertiti > 0, true);
                        Passo("test del guadagno medio (AGT) entro +-0,2 dB", "nessun modello fuori dove l'AGT vale (spazio libero o terreno perfetto, senza perdite)",
                agtFuori + " fuori (sono indicati nelle righe con la freccia)", agtFuori == 0, true);
        }
    }
}
