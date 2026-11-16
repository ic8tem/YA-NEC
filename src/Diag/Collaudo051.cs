// YA-NEC 0.5.1 - collaudo dei controlli dei dati (controllo z1 approvato da IC8TEM il 15/11/2026 11:35: "procedura di collaudo che
// inserisce dati sbagliati apposta e verifica che ogni avviso compaia"). Per ogni codice un modello con l'errore voluto; poi un dipolo
// giusto senza errori, i modelli di esempio e i calcolatori (gli errori trovati li' sono solo AVVISO: possono essere falsi allarmi).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using YaNec.Engine;

namespace YaNec
{
    public static partial class Collaudo
    {
        static string Mazzo(string gw1 = "GW 1 21 0 -0.5 0 0 0.5 0 0.002", string altri = "", string ge = "GE 0", string ctl = "EX 0 1 11 0 1 0", double f = 144.3)
        {
            return "CM prova dei controlli dei dati\nCE\n" + gw1 + "\n" + altri + ge + "\n" + ctl + "\nFR 0 1 0 0 " + f.ToString("0.######", CI) + " 0\nEN\n";
        }

        static string Yagi3(string dir = "GW 3 21 0.4 -0.45 0 0.4 0.45 0 0.002", string riflettore = "GW 1 21 -0.4 -0.52 0 -0.4 0.52 0 0.002", string altri = "")
        {
            return "CM Yagi 3 el. di prova\nCE\n" + riflettore + "\nGW 2 21 0 -0.49 0 0 0.49 0 0.002\n" + dir + "\n" + altri + "GE 0\nEX 0 2 11 0 1 0\nFR 0 1 0 0 144.3 0\nEN\n";
        }

        static string Codici(List<ControlloDato> l)
        {
            if (l.Count == 0) return "nessun controllo";
            List<string> c = new List<string>();
            foreach (ControlloDato d in l) c.Add((d.Errore ? "E:" : "A:") + d.Codice);
            return string.Join(" ", c);
        }

        static void ProvaCodice(string cod, string cosa, List<ControlloDato> l, bool errore)
        {
            ControlloDato d = l.Find(x => x.Codice == cod);
            bool ok = d != null && (!errore || d.Errore);
            Passo("[" + cod + "] " + cosa, (errore ? "ERRORE " : "avviso ") + cod, Codici(l) + (d != null ? " | " + d.Testo : ""), ok);
        }

        static List<ControlloDato> CtrlNec(string nec, Action<NecModel> ritocco = null)
        {
            NecModel m = NecModel.Parse(nec);
            if (ritocco != null) ritocco(m);
            return ControlliDati.Modello(m);
        }

        static void Novita051()
        {
            Riga("  Codici: E = errore, A = avviso. Ogni passo inserisce apposta un dato sbagliato e verifica che il controllo lo trovi.");
            // modello giusto: nessun errore, nessun avviso, e nessun controllo sui risultati
            NecModel buono = NecModel.Parse(Mazzo());
            List<ControlloDato> lb = ControlliDati.Modello(buono);
            Passo("dipolo 144,3 MHz giusto (21 segmenti, sorgente al centro)", "nessun controllo", Codici(lb), lb.Count == 0);
            RunResult rb = Analysis.Run(buono, buono.BuildGeometry(), 144.3, 2.0);
            List<ControlloDato> lrb = ControlliDati.Risultati(buono, rb);
            Passo("dipolo giusto calcolato: controlli sui risultati", "nessun controllo", Codici(lrb), lrb.Count == 0);

            // c: campo di calcolo
            ProvaCodice("c1", "frequenza 0,005 MHz", CtrlNec(Mazzo(f: 0.005)), true);
            ProvaCodice("u3", "frequenza scritta in kHz (144300)", CtrlNec(Mazzo(f: 144300)), true);
            ProvaCodice("u3", "frequenza scritta in Hz (144300000)", CtrlNec(Mazzo(f: 144300000)), true);
            ProvaCodice("c2", "filo lungo 1 cm con diametro 10 cm", CtrlNec(Mazzo(altri: "GW 2 1 0 2 0 0 2.01 0 0.05\n")), true);
            List<ControlloDato> l3 = CtrlNec(Mazzo(gw1: "GW 1 21 0 -0.5 0 0 0.5 0 3"));
            ProvaCodice("c3", "raggio 3 (m) sul dipolo da 1 m", l3, true);
            ProvaCodice("u2", "raggio 3 (m): sembrano mm", l3, true);
            ProvaCodice("c3", "raggio 1 micron (diametro 0,002 mm)", CtrlNec(Mazzo(gw1: "GW 1 21 0 -0.5 0 0 0.5 0 0.000001")), false);
            ProvaCodice("c4", "terreno con costante dielettrica 0,5", CtrlNec(Mazzo(gw1: "GW 1 21 0 -0.5 10 0 0.5 10 0.002", ge: "GE 1\nGN 2 0 0 0 0.5 0.005")), true);
            ProvaCodice("c4", "terreno con conducibilita' 50 000 S/m", CtrlNec(Mazzo(gw1: "GW 1 21 0 -0.5 10 0 0.5 10 0.002", ge: "GE 1\nGN 2 0 0 0 13 50000")), true);
            ProvaCodice("c5", "dipolo a 30 m (14 lambda) sul terreno perfetto", CtrlNec(Mazzo(gw1: "GW 1 21 0 -0.5 30 0 0.5 30 0.002", ge: "GE 1\nGN 1")), false);
            ProvaCodice("c6", "filo da 30 001 segmenti (matrice di 14 GB)", CtrlNec(Mazzo(gw1: "GW 1 30001 0 -250 0 0 250 0 0.002", ctl: "EX 0 1 15001 0 1 0")), false);
            ProvaCodice("u1", "dipolo scritto in mm al posto dei m (1000 m)", CtrlNec(Mazzo(gw1: "GW 1 21 0 -500 0 0 500 0 0.002")), true);
            ProvaCodice("u1", "dipolo scritto in m al posto dei mm (1 mm)", CtrlNec(Mazzo(gw1: "GW 1 21 0 -0.0005 0 0 0.0005 0 0.000002")), true);
            ProvaCodice("u4", "file importato (MMANA) con le misure in piedi lette come metri", CtrlNec(Mazzo(gw1: "GW 1 21 0 -1.704 0 0 1.704 0 0.002"), m => m.SourceFormat = "MMANA"), false);

            // g: geometria
            ProvaCodice("g1", "filo ripetuto due volte", CtrlNec(Mazzo(altri: "GW 2 21 0 -0.5 0 0 0.5 0 0.002\n")), true);
            ProvaCodice("g2", "filo a lunghezza zero", CtrlNec(Mazzo(altri: "GW 2 1 0 2 0 0 2 0 0.002\n")), true);
            ProvaCodice("g3", "filo che parte a 0,5 mm dalla punta del dipolo", CtrlNec(Mazzo(altri: "GW 2 5 0 0.5005 0 0 0.8 0 0.002\n")), true);
            ProvaCodice("g4", "verticale che scende a -0,3 m sul terreno perfetto", CtrlNec(Mazzo(gw1: "GW 1 21 0 0 -0.3 0 0 0.7 0.002", ge: "GE 1\nGN 1")), true);
            ProvaCodice("g5", "Yagi 3 el. con il direttore spostato di 2 cm da un lato", CtrlNec(Yagi3(dir: "GW 3 21 0.4 -0.43 0 0.4 0.47 0 0.002")), false);
            ProvaCodice("g6", "due direttori nella stessa posizione (x = 0,4 m)", CtrlNec(Yagi3(altri: "GW 4 19 0.4 -0.40 0 0.4 0.40 0 0.002\n")), true);
            List<ControlloDato> ly = CtrlNec(Yagi3());
            Passo("Yagi 3 el. giusta", "nessun errore, nessun g5/g6", Codici(ly), !ly.Exists(x => x.Errore || x.Codice == "g5" || x.Codice == "g6"));

            // k: calcolatori
            ProvaCodice("k2", "riflettore a gradini con le punte piu' grosse del centro", CtrlNec(Yagi3(riflettore: "GW 1 7 -0.4 -0.3 0 -0.4 0.3 0 0.004\nGW 5 3 -0.4 0.3 0 -0.4 0.52 0 0.008\nGW 6 3 -0.4 -0.3 0 -0.4 -0.52 0 0.008")), false);
            List<ControlloDato> lk2b = CtrlNec(Yagi3(riflettore: "GW 1 7 -0.4 -0.3 0 -0.4 0.3 0 0.008\nGW 5 3 -0.4 0.3 0 -0.4 0.52 0 0.004\nGW 6 3 -0.4 -0.3 0 -0.4 -0.52 0 0.004"));
            Passo("riflettore a gradini giusto (grosso al centro, sottile in punta)", "nessun k2", Codici(lk2b), !lk2b.Exists(x => x.Codice == "k2"));
            ProvaCodice("k2", "rastremazione scritta dalla punta al centro", CtrlNec(Mazzo(), m => m.Rastremazioni.Add(new Rastremazione { N = 1, L = new List<double> { 0.2, -1 }, R = new List<double> { 0.003, 0.006 } })), false);
            ProvaCodice("k1", "LPDA con tau 0,70 e sigma 0,30", ControlliDati.Lpda(new LpdaDesign { Fmin = 144, Fmax = 432, Tau = 0.70, Sigma = 0.30 }), false);
            {
                DmSpec ds = new DmSpec { Modo = 0, Bande = new List<string> { "40 m" } };
                ds.Trappole.Add(new DmTrappola { Dipolo = "40 m", Nuova = "15 m" });
                ProvaCodice("k3", "trappola sul dipolo dei 40 m per i 15 m", ControlliDati.DipoloMb(ds), true);
            }
            {
                // 1.0.7: manica aperta 40-20-15-10 m con tubi da 20 mm e il 15 m a 2,1 cm (caso del videocorso, lezione 4)
                DmSpec dm = new DmSpec { Modo = 1, Bande = new List<string> { "40 m", "20 m", "15 m", "10 m" }, Diametro = 0.02 };
                dm.S["40 m"] = 0; dm.S["20 m"] = 0.16; dm.S["15 m"] = 0.021; dm.S["10 m"] = 0.10;
                ProvaCodice("k7", "manica aperta con il 15 m a 2,1 cm (tubi da 20 mm)", ControlliDati.DipoloMb(dm), false);
                dm.S["15 m"] = 0.10; dm.S["10 m"] = 0.30;
                List<ControlloDato> lk7 = ControlliDati.DipoloMb(dm);
                Passo("manica aperta con gli elementi a 10, 16 e 30 cm (tubi da 20 mm)", "nessun k7", Codici(lk7), !lk7.Exists(x => x.Codice == "k7"));
            }
            {
                // 1.0.7: Segmentazione su un dipolo alimentato a uno dei due segmenti centrali di un filo pari: la sorgente torna al centro
                NecModel ms = NecModel.Parse(Mazzo(gw1: "GW 1 4 0 -5.2 10 0 5.2 10 0.001", ctl: "EX 0 1 2 0 1 0", f: 14.2));
                SegOptions so = new SegOptions();
                List<SegIssue> iss = SegCheck.Check(ms, 14.2, so);
                NecModel ms2 = SegCheck.Correct(ms, 14.2, so, iss);
                int nseg = 0; foreach (GeomCard g in ms2.Geometry) if (g.Id == "GW" && g.I1 == 1) nseg = g.I2;
                Passo("Segmentazione: dipolo da 4 segmenti alimentato al 2 (1.0.7)", "sorgente al segmento centrale del filo corretto",
                    "filo da " + nseg + " segmenti, sorgente al " + ms2.Sources[0].Seg, nseg % 2 == 1 && ms2.Sources[0].Seg == (nseg + 1) / 2);
            }
            ProvaCodice("k4", "stack a 0,5 m con la DL6WU a 2 m", ControlliDati.Stack(0.5, double.NaN, 2.0, double.NaN), false);
            {
                // due quadrati verticali di 0,5 m uno sopra l'altro a 0,3 m: si toccano
                string q = "CM stack di due loop\nCE\n";
                for (int k = 0; k < 2; k++)
                {
                    double z = 2 + 0.3 * k;
                    int t = 4 * k;
                    q += string.Format(CI, "GW {0} 5 0 -0.25 {1} 0 0.25 {1} 0.002\nGW {2} 5 0 0.25 {1} 0 0.25 {3} 0.002\nGW {4} 5 0 0.25 {3} 0 -0.25 {3} 0.002\nGW {5} 5 0 -0.25 {3} 0 -0.25 {1} 0.002\n",
                        t + 1, z, t + 2, z + 0.5, t + 3, t + 4);
                }
                q += "GE 0\nEX 0 1 3 0 1 0\nEX 0 5 3 0 1 0\nFR 0 1 0 0 432 0\nEN\n";
                List<ControlloDato> lq = CtrlNec(q);
                Passo("[k4] stack di due loop da 0,5 m a 0,3 m (si toccano)", "ERRORE k4 (se lo stack e' riconosciuto)", Codici(lq), lq.Exists(x => x.Codice == "k4"), true);
            }
            {
                CalcMoxon mx = new CalcMoxon();
                mx.Par("A").Value = 2 * CalcMoxon.Dimensioni(mx.Par("f").Value, mx.Par("d").Value)[0];
                CalcOutput o = mx.Run();
                bool k5 = o.Warnings.Exists(w => w.Contains("[k5]"));
                Passo("[k5] Moxon con la larghezza A doppia di MOXGEN", "avviso k5 nella scheda", k5 ? o.Warnings.Find(w => w.Contains("[k5]")) : string.Join(" | ", o.Warnings), k5);
                CalcQuad qd = new CalcQuad();
                qd.Par("s").Value = 0.05;
                CalcOutput oq = qd.Run();
                bool k5q = oq.Warnings.Exists(w => w.Contains("[k5]"));
                Passo("[k5] quad con spaziatura 0,05 lambda", "avviso k5 nella scheda", k5q ? oq.Warnings.Find(w => w.Contains("[k5]")) : string.Join(" | ", oq.Warnings), k5q);
            }
            {
                ParabolaResult pr = Parabola.Calcola(new ParabolaInput { FsuD = 0.9 }, false);
                ParabolaAvviso a = pr.Avvisi.Find(x => x.Testo.Contains("[k6]"));
                Passo("[k6] parabola con f/D 0,9", "avviso k6", a != null ? a.Testo : (pr.Errore ?? "nessun avviso k6"), a != null);
            }

            // s: sorgenti e carichi
            ProvaCodice("s1", "sorgente sul segmento 30 di un filo da 21", CtrlNec(Mazzo(ctl: "EX 0 1 30 0 1 0")), true);
            ProvaCodice("s1", "sorgente sul filo 9 che non esiste", CtrlNec(Mazzo(ctl: "EX 0 9 1 0 1 0")), true);
            ProvaCodice("s2", "sorgente sul segmento di punta", CtrlNec(Mazzo(ctl: "EX 0 1 1 0 1 0")), false);
            ProvaCodice("s3", "sorgente sul segmento 10 di un filo da 20", CtrlNec(Mazzo(gw1: "GW 1 20 0 -0.5 0 0 0.5 0 0.002", ctl: "EX 0 1 10 0 1 0")), false);
            ProvaCodice("s4", "carico serie con R = -5 ohm", CtrlNec(Mazzo(ctl: "EX 0 1 11 0 1 0\nLD 0 1 11 11 -5 0 0")), true);
            ProvaCodice("s4", "carico sul filo 7 che non esiste", CtrlNec(Mazzo(ctl: "EX 0 1 11 0 1 0\nLD 0 7 1 1 10 0 0")), true);
            ProvaCodice("s5", "trappola LC 1 uH / 4 pF (79,6 MHz, fuori dalle bande)", CtrlNec(Mazzo(ctl: "EX 0 1 11 0 1 0\nLD 1 1 11 11 0 1E-06 4E-12")), false);
            ProvaCodice("s6", "bobina 0,1 uH con 50 ohm (Q 1,8)", CtrlNec(Mazzo(ctl: "EX 0 1 11 0 1 0\nLD 0 1 11 11 50 1E-07 0")), false);
            ProvaCodice("s7", "due sorgenti sullo stesso segmento", CtrlNec(Mazzo(ctl: "EX 0 1 11 0 1 0\nEX 0 1 11 0 1 0")), true);

            // t: linee
            string due = "GW 2 21 1 -0.5 0 1 0.5 0 0.002\n";
            ProvaCodice("t1", "linea con impedenza 0", CtrlNec(Mazzo(altri: due, ctl: "EX 0 1 11 0 1 0\nTL 1 11 2 11 0 0.5 0 0 0 0")), true);
            ProvaCodice("t2", "stub con fattore di velocita' 1,5", CtrlNec(Mazzo(), m => m.Stubs.Add(new Stub { Tag = 1, Seg = 11, Z0 = 300, LenM = 0.3, Vf = 1.5 })), true);
            ProvaCodice("t3", "linea lunga 0,2 m tra punti distanti 1 m", CtrlNec(Mazzo(altri: due, ctl: "EX 0 1 11 0 1 0\nTL 1 11 2 11 300 0.2 0 0 0 0")), false);
            ProvaCodice("t4", "linea verso il filo 5 che non esiste", CtrlNec(Mazzo(altri: due, ctl: "EX 0 1 11 0 1 0\nTL 1 11 5 3 300 1 0 0 0 0")), true);
            List<ControlloDato> lt = CtrlNec(Mazzo(altri: due, ctl: "EX 0 1 11 0 1 0\nTL 1 11 2 11 -300 0 0 0 0 0"));
            Passo("linea incrociata (Z0 negativa) di lunghezza 0 come nelle LPDA", "nessun t1, t3", Codici(lt), !lt.Exists(x => x.Codice == "t1" || x.Codice == "t3"));

            // v: variabili
            ProvaCodice("v1", "A = B + 1 con B non definita", CtrlNec(Mazzo(), m => m.Variables.Add(new SyVar { Name = "A", Expr = "B+1" })), true);
            ProvaCodice("v1", "A = B + 1 con B definita dopo", CtrlNec(Mazzo(), m => { m.Variables.Add(new SyVar { Name = "A", Expr = "B+1" }); m.Variables.Add(new SyVar { Name = "B", Expr = "2" }); }), true);
            ProvaCodice("v2", "A = B + 1 e B = A * 2", CtrlNec(Mazzo(), m => { m.Variables.Add(new SyVar { Name = "A", Expr = "B+1" }); m.Variables.Add(new SyVar { Name = "B", Expr = "A*2" }); }), true);
            ProvaCodice("v3", "Z = 1/(L-L)", CtrlNec(Mazzo(), m => { m.Variables.Add(new SyVar { Name = "L", Expr = "1" }); m.Variables.Add(new SyVar { Name = "Z", Expr = "1/(L-L)" }); }), true);
            ProvaCodice("v3", "Z = SQR(-1)", CtrlNec(Mazzo(), m => m.Variables.Add(new SyVar { Name = "Z", Expr = "SQR(-1)" })), true);
            ProvaCodice("v4", "U = 5 mai usata", CtrlNec(Mazzo(), m => m.Variables.Add(new SyVar { Name = "U", Expr = "5" })), false);

            // r: risultati (valori costruiti apposta, il motore non li darebbe)
            ProvaCodice("r1", "efficienza 120 %", ControlliDati.Risultati(buono, new RunResult { Efficiency = 120, AgtDb = double.NaN }), true);
            ProvaCodice("r2", "AGT +1 dB in spazio libero senza perdite", ControlliDati.Risultati(buono, new RunResult { Efficiency = 100, AgtDb = 1.0 }), false);
            {
                RunResult r3 = new RunResult { Efficiency = 100, AgtDb = double.NaN, Zin = new Complex(-5, 3) };
                r3.Sources.Add(new SourceResult { Tag = 1, Segment = 11, Z = r3.Zin });
                ProvaCodice("r3", "resistenza -5 ohm con una sorgente", ControlliDati.Risultati(buono, r3), true);
            }
            ProvaCodice("r4", "ROS 15 a 144,3 MHz", ControlliDati.Risultati(buono, new RunResult { Efficiency = 100, AgtDb = double.NaN, FreqMHz = 144.3, Swr = 15, Z0 = 50 }), false);
            {
                NecModel y = NecModel.Parse(Yagi3());
                RunResult ry = Analysis.Run(y, y.BuildGeometry(), 144.3, 2.0);
                List<ControlloDato> lry = ControlliDati.Risultati(y, ry);
                Passo("Yagi 3 el. calcolata: guadagno sotto il massimo fisico", string.Format(CI, "nessun r5 (massimo {0:0.0} dBi)", ControlliDati.GuadagnoMassimo(0.8 / (299.792458 / 144.3), false)),
                    string.Format(CI, "{0:0.00} dBi; {1}", ry.GmaxDbi, Codici(lry)), !lry.Exists(x => x.Codice == "r5"));
                ry.GmaxDbi = 30;
                ProvaCodice("r5", "la stessa Yagi con 30 dBi", ControlliDati.Risultati(y, ry), true);
            }

            // modelli di esempio e calcolatori: gli errori qui sono falsi allarmi da guardare (solo avviso)
            int nm = 0, nErr = 0;
            List<string> conErr = new List<string>();
            // 0.5.2 (collaudo 0.5.1): errori veri e attesi negli esempi. L'Hex beam di JN1GLB (MMANA) ha 5 coppie di fili paralleli che si
            // toccano: EZNEC lo ferma ("parallel and contacting", fase 2J), MMANA lo calcola; nell'archivio c'e' la versione corretta (0.4.22).
            Dictionary<string, string> attesi = new Dictionary<string, string> { { "Hex beam 6 bande 20-6 m JN1GLB (MMANA).yanec", "g1" } };
            if (Directory.Exists(AppPaths.ModelsDir))
                foreach (string fpath in Directory.GetFiles(AppPaths.ModelsDir, "*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(fpath).ToLowerInvariant();
                    if (ext != ".yanec" && ext != ".nec" && ext != ".maa" && ext != ".weq" && ext != ".ez") continue;
                    try
                    {
                        NecModel m = Apri(fpath);
                        List<ControlloDato> l = ControlliDati.Modello(m);
                        nm++;
                        string att;
                        bool haAtt = attesi.TryGetValue(Path.GetFileName(fpath), out att);
                        if (haAtt)
                            Passo("esempio con un errore vero: " + Path.GetFileName(fpath), "ERRORE " + att + " (anche EZNEC lo ferma)", Codici(l), l.Exists(x => x.Errore && x.Codice == att));
                        List<ControlloDato> e = l.FindAll(x => x.Errore && !(haAtt && x.Codice == att));
                        if (e.Count > 0) { nErr++; conErr.Add(Path.GetFileName(fpath) + ": " + Codici(e)); }
                        if (l.Count > 0) Info(Path.GetFileName(fpath) + ": " + Codici(l));
                    }
                    catch (Exception ex) { Info(Path.GetFileName(fpath) + ": " + ex.Message); }
                }
            Passo("modelli di esempio: controllo dei dati senza calcolare", "nessun ERRORE (gli avvisi sono elencati sopra)", nm + " modelli, " + nErr + " con errori" + (conErr.Count > 0 ? ": " + string.Join(" | ", conErr) : ""), nErr == 0, true);
            int nc = 0, ncErr = 0;
            List<string> calcErr = new List<string>();
            foreach (SimpleCalc c in Calcolatori.Tutti())
            {
                try
                {
                    CalcOutput o = c.Run();
                    nc++;
                    List<string> e = o.Warnings.FindAll(w => w.StartsWith("ERRORE dei dati", StringComparison.Ordinal));
                    List<string> a = o.Warnings.FindAll(w => w.StartsWith("Controllo dei dati", StringComparison.Ordinal));
                    if (e.Count > 0) { ncErr++; calcErr.Add(c.Title + ": " + string.Join(" | ", e)); }
                    if (e.Count + a.Count > 0) Info(c.Title + ": " + (e.Count + a.Count) + " controlli: " + string.Join(" | ", e) + " " + string.Join(" | ", a));
                }
                catch (Exception ex) { Info(c.Title + ": " + ex.Message); }
            }
            Passo("calcolatori con i valori iniziali: controllo dei dati", "nessun ERRORE", nc + " calcolatori, " + ncErr + " con errori" + (calcErr.Count > 0 ? ": " + string.Join(" || ", calcErr) : ""), ncErr == 0, true);
            {
                CalcOutput oq = new CalcQfh().Run();
                bool c2 = oq.Warnings.Exists(w => w.Contains("[c2]"));
                Passo("QFH dei Calcolatori: elica fatta a fili corti (0.5.2)", "nessun c2 (i fili corti sono uniti in catena)", c2 ? oq.Warnings.Find(w => w.Contains("[c2]")) : "nessun c2", !c2);
                ProvaCodice("c2", "lo stesso filo corto e grosso, isolato", CtrlNec(Mazzo(altri: "GW 2 1 0 2 0 0 2.01 0 0.05\n")), true);
                List<ControlloDato> lg = CtrlNec(Mazzo(altri: "GW 2 1 1 0 -0.01 1 0 0.01 0.001\n", ctl: "EX 0 2 1 0 1 0\nTL 2 1 1 11 450 1 0 0 0 0"));
                Passo("sorgente su un filo di alimentazione collegato a una linea (0.5.2)", "nessun s2", Codici(lg), !lg.Exists(x => x.Codice == "s2"));
            }
            Passo("elenco dei controlli", "43 voci (46 approvate meno z1, z2, z3 che sono collaudo, manuale e pulsante; k7 dalla 1.0.7)", ControlliDati.Elenco.Length + " voci", ControlliDati.Elenco.Length == 43);
        }
    }
}
