// YA-NEC 0.4.20 - esportazione dei modelli dei calcolatori per la calibrazione con EZNEC (YA-NEC.exe --esporta-eznec <cartella>).
// Richiesta di IC8TEM del 14/11/2026 15:25: "chiedi di compilare i nec per le sottolinguette di calcolatore. tutti i modelli HF VHF che
// hai come sottolinguette devo poterli provare in eznec per la calibrazione finale. poi nello stesso bat, fai partire anche le prove
// eznec di questi modelli che ti faccio ricavare. mi raccomando al salvataggio ez." Scelta delle 15:25: prove EZNEC sempre in automatico.
// Per ogni sottolinguetta che produce un modello NEC (forme semplici, schiere, satelliti, LPDA, loop magnetica, antenne riceventi) il
// modello con i valori iniziali della scheda si salva in .nec e in .ez; il .ez si rilegge subito (EzImport) e si ricalcola: se il modello
// riletto non da' la stessa impedenza entro lo 0,5 % il salvataggio e' segnalato come ERRORE (controllo del salvataggio .ez chiesto da
// IC8TEM). Le trombe restano fuori; dalla 0.4.22 c'e' il disco a centine della Parabola (1,2 m, ideale e sfaccettato).
// Uscita: <cartella>\modelli_ez\<gruppo>\<nome>.ez (per confronta_eznec.ps1), <cartella>\nec\<gruppo>\<nome>.nec, <cartella>\riferimento_YA-NEC.csv
// e <cartella>\esportazione.txt (resoconto con lo stesso formato dei passi del collaudo).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using YaNec.Engine;

namespace YaNec
{
    public static class EsportaEznec
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static StringBuilder sb, csv;
        static int nOk, nErr, nAvv;

        class Voce
        {
            public string Gruppo, Nome, Nec;
            public bool Correnti;
        }

        public static int Esegui(string cartella)
        {
            sb = new StringBuilder(); csv = new StringBuilder();
            nOk = nErr = nAvv = 0;
            if (string.IsNullOrEmpty(cartella)) cartella = Path.Combine(AppPaths.DataDir, "eznec_calcolatori");
            string dEz = Path.Combine(cartella, "modelli_ez"), dNec = Path.Combine(cartella, "nec");
            try { if (Directory.Exists(dEz)) Directory.Delete(dEz, true); if (Directory.Exists(dNec)) Directory.Delete(dNec, true); } catch { }
            Directory.CreateDirectory(dEz); Directory.CreateDirectory(dNec);
            Riga("ESPORTAZIONE DEI MODELLI DEI CALCOLATORI PER EZNEC - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Riga("Cartella: " + cartella);
            Riga("Ogni modello: .nec e .ez con i valori iniziali della scheda; il .ez si rilegge e si ricalcola (controllo del salvataggio).");
            Riga(new string('=', 100));
            csv.AppendLine("modello .ez;gruppo;MHz;segmenti;terreno;YA-NEC R;YA-NEC X;ROS 50;Gmax dBi;elevazione;azimut;F/B dB;efficienza %;.ez riletto R;.ez riletto X;scarto %;perso nel .ez;avvisi");

            List<Voce> voci = Raccogli();
            int k = 0;
            foreach (Voce v in voci)
            {
                k++;
                Riga("");
                Riga(string.Format(CI, "MODELLO {0}/{1} - {2} \\ {3}", k, voci.Count, v.Gruppo, v.Nome));
                Esporta(v, dEz, dNec);
                Registro.Scrivi("COLLAUDO", "esportazione per EZNEC: " + v.Gruppo + " \\ " + v.Nome);
            }
            Riga("");
            Riga(nErr == 0 ? string.Format(CI, "ESPORTAZIONE: SUPERATA ({0} modelli, {1} avvisi).", voci.Count, nAvv) : string.Format(CI, "ESPORTAZIONE: {0} ERRORI su {1} modelli.", nErr, voci.Count));
            try
            {
                File.WriteAllText(Path.Combine(cartella, "esportazione.txt"), sb.ToString(), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(cartella, "riferimento_YA-NEC.csv"), csv.ToString(), new UTF8Encoding(true));
                File.WriteAllText(Path.Combine(cartella, "LEGGIMI.txt"),
                    "YA-NEC " + AppPaths.Version + " - modelli dei calcolatori per EZNEC (prova automatica di compila.bat)\r\n\r\n" +
                    "modelli_ez\\   i modelli in .ez, uno per sottolinguetta (valori iniziali della scheda); EZNEC li calcola con confronta_eznec.ps1\r\n" +
                    "nec\\          gli stessi modelli in NEC-2, aperti in YA-NEC\r\n" +
                    "riferimento_YA-NEC.csv  impedenza, guadagno, F/B ed efficienza di YA-NEC, e l'impedenza del .ez riletto (controllo del salvataggio)\r\n" +
                    "esportazione.txt        resoconto, con gli avvisi del modello e le parti che il formato .ez non contiene\r\n",
                    Encoding.GetEncoding("ISO-8859-1"));
            }
            catch (Exception ex) { Console.WriteLine("Scrittura dei resoconti non riuscita: " + ex.Message); nErr++; }
            return nErr;
        }

        static List<Voce> Raccogli()
        {
            List<Voce> l = new List<Voce>();
            // forme semplici, strumenti con modello, schiere, satelliti: i valori iniziali della scheda
            foreach (SimpleCalc sc in SimpleCalc.All())
            {
                if (sc.UsesModel) continue;
                try
                {
                    CalcOutput o = sc.Run();
                    if (o == null || string.IsNullOrEmpty(o.Nec)) continue;
                    l.Add(new Voce { Gruppo = "Calcolatori - " + sc.Group, Nome = sc.Nome, Nec = o.Nec, Correnti = o.CurrentDrive });
                }
                catch (Exception ex) { Passo("modello di \"" + sc.Nome + "\"", "creato", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 0.5.0 (IC8TEM 15/11/2026 16:00, "voglio collaudare pure i nec che escono dalle schede"): oltre ai dati iniziali, le varianti
            // delle schede nuove: Yagi DL6WU con le altre tre alimentazioni, quad tutta ad anelli, loop Yagi con il boom da 12 piedi
            for (int fd = 1; fd < 4; fd++)
            {
                try
                {
                    CalcYagiDl6wu y = new CalcYagiDl6wu();
                    y.Group = "yagidl6wu"; y.Par("feed").Value = fd;
                    CalcOutput o = y.Run();
                    l.Add(new Voce { Gruppo = "Calcolatori - yagidl6wu", Nome = "Yagi DL6WU 10 el. " + CalcYagiDl6wu.Alim[fd], Nec = o.Nec });
                }
                catch (Exception ex) { Passo("modello della Yagi DL6WU con " + CalcYagiDl6wu.Alim[fd], "creato", ex.GetType().Name + ": " + ex.Message, false); }
            }
            try
            {
                CalcQuagi q = new CalcQuagi(); q.Par("tipo").Value = 1;
                l.Add(new Voce { Gruppo = "Calcolatori - quagi", Nome = "Quad 8 el. tutta ad anelli", Nec = q.Run().Nec });
                CalcLoopYagi ly = new CalcLoopYagi(); ly.Par("boom").Value = 3.66;
                l.Add(new Voce { Gruppo = "Calcolatori - loopyagi", Nome = "Loop Yagi N6CA 1296, boom 12 piedi", Nec = ly.Run().Nec });
            }
            catch (Exception ex) { Passo("modelli di quad e loop Yagi", "creati", ex.GetType().Name + ": " + ex.Message, false); }
            // LPDA: quella della scheda (144-432 MHz) e una HF (13,5-30 MHz), in spazio libero
            foreach (double[] b in new[] { new[] { 144.0, 432.0, 0.90, 0.168, 6.0 }, new[] { 13.5, 30.0, 0.90, 0.168, 25.0 } })
            {
                try
                {
                    LpdaDesign d = new LpdaDesign { Fmin = b[0], Fmax = b[1], Tau = b[2], Sigma = b[3], Rin = 50, DiamMm = b[4], LineTubeMm = 10 };
                    d.Compute();
                    l.Add(new Voce { Gruppo = "Calcolatori - lpda", Nome = string.Format(CI, "LPDA {0:0.#}-{1:0} MHz", b[0], b[1]), Nec = d.ToNec((d.Fmin + d.Fmax) / 2, 0, new GroundSpec { Kind = GroundKind.FreeSpace }, 50) });
                }
                catch (Exception ex) { Passo("modello LPDA", "creato", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // loop magnetica della linguetta Loop (valori iniziali, condensatore di risonanza calcolato)
            try
            {
                NecModel fin;
                LoopEsito e = LoopGen.Esito(new LoopSpec(), CancellationToken.None, out fin);
                // con il condensatore di risonanza il loop ha un Q di migliaia: un millesimo di differenza nell'induttanza sposta molto
                // l'impedenza (avviso fino al 3 %); la stessa loop senza condensatore (solo induttiva) si confronta entro lo 0,5 %
                if (fin != null) l.Add(new Voce { Gruppo = "Loop", Nome = "Loop magnetica 1 m 7,1 MHz (alto Q)", Nec = fin.ToNec() });
                int tc, sgc;
                NecModel senza = LoopGen.Costruisci(new LoopSpec(), null, true, 0, true, out tc, out sgc);
                if (senza.FreqMHz <= 0 || senza.Frequencies().Count == 0) senza.FreqMHz = 7.1;
                l.Add(new Voce { Gruppo = "Loop", Nome = "Loop magnetica 1 m 7,1 MHz senza condensatore", Nec = senza.ToNec() });
            }
            catch (Exception ex) { Passo("modello della loop", "creato", ex.GetType().Name + ": " + ex.Message, false); }
            // 0.4.22 (IC8TEM 14/11/2026 20:32: "sia del YA-NEC con il controllo degli arcarecci e centine delle parabole; poi di questi
            // modelli che maneggi, li converti tutti in .ez per provarli con EZNEC"): il disco a centine della scheda Parabola, 1,2 m a 1296 MHz
            // (il massimo del modello NEC), rete sul paraboloide e rete piana tra le centine (sfaccettata). Circa 7000 segmenti l'uno.
            foreach (bool sfacc in new[] { false, true })
            {
                try
                {
                    int ns;
                    string nec = ParabolaNec.Mazzo(1296.1, 1.2, 0.42, 16, 0.087, sfacc, out ns);
                    l.Add(new Voce { Gruppo = "Parabola - disco a centine", Nome = "Parabola 1,2 m 1296 MHz 16 centine " + (sfacc ? "sfaccettata" : "ideale"), Nec = nec });
                }
                catch (Exception ex) { Passo("modello del disco a centine", "creato", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 0.4.30 (IC8TEM 14/11/2026 22:32, "serve collaudare"): monopolo a tubi telescopici con la base a terra, corretto da YA-NEC come meta'
            // di elemento (Cebik, NEC Correctives); EZNEC lo calcola con la sua Stepped Diameter Correction (se il .ez la tiene accesa)
            l.Add(new Voce { Gruppo = "Tubi telescopici - monopolo", Nome = "Monopolo a gradini 14,2 MHz terreno perfetto",
                Nec = "CM Monopolo a gradini 14,2 MHz terreno perfetto\nCM YA-NEC LEESON=1\nCE\nGW 1 6 0 0 0 0 0 1.5 0.02\nGW 2 6 0 0 1.5 0 0 3.5 0.0125\nGW 3 4 0 0 3.5 0 0 5.0 0.008\nGE 1\nGN 1\nEX 0 1 1 0 1 0\nFR 0 1 0 0 14.2 0\nEN\n" });
            // 0.4.30: elica della barra Geometria (valori di partenza della scheda)
            try { int ts, ss; l.Add(new Voce { Gruppo = "Geometria - elica", Nome = "Elica 10 spire 435 MHz piano perfetto", Nec = HelixGen.Costruisci(new HelixSpec(), null, out ts, out ss).ToNec() }); }
            catch (Exception ex) { Passo("modello dell'elica", "creato", ex.GetType().Name + ": " + ex.Message, false); }
            // 0.4.30 (IC8TEM 14/11/2026 23:13, "si"): verticale multibanda tipo DX Commander della sottolinguetta, terreno medio di Sommerfeld,
            // 32 radiali da 5 m a 5 cm, sulle bande 40 e 20 m
            foreach (double fv in new[] { 7.1, 14.175 })
            {
                try
                {
                    VmSpec vs = new VmSpec { File = NecModel.Load(Path.Combine(AppPaths.DataDir, "archivio", "Verticali multibanda", "dxcommander_A.nec")) };
                    vs.DalFile = true;
                    vs.El = VerticaleMultibanda.ElementiDalFile(vs.File);
                    vs.Terreno = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005, Sommerfeld = true };
                    vs.Radiali = 32; vs.LRad = 5; vs.Quota = 0.05;
                    l.Add(new Voce { Gruppo = "Verticale multibanda (DX Commander)", Nome = "DX Commander " + fv.ToString("0.###", CultureInfo.InvariantCulture) + " MHz terreno medio 32 radiali",
                        Nec = VerticaleMultibanda.Modello(vs, fv, new List<string>()).ToNec() });
                }
                catch (Exception ex) { Passo("modello della verticale multibanda", "creato", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 0.4.30: dipolo multibanda, i tre modi con le misure di partenza, spazio libero, al centro della banda piu' bassa
            for (int dm = 0; dm < 3; dm++)
            {
                try
                {
                    DmSpec ds = new DmSpec { Modo = dm, Diametro = dm == 1 ? 0.02 : 0.002 };
                    DipoloMultibanda.Iniziale(ds);
                    Dictionary<int, string> tbd;
                    l.Add(new Voce { Gruppo = "Dipolo multibanda", Nome = "Dipolo multibanda, " + DipoloMultibanda.Modi[dm] + " (" + string.Join("-", ds.Bande) + ")",
                        Nec = DipoloMultibanda.Modello(ds, DipoloMultibanda.Banda(ds.Bande[0]).Operating, out tbd).ToNec() });
                }
                catch (Exception ex) { Passo("modello del dipolo multibanda " + dm, "creato", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 0.4.30: Yagi multibanda HF (bacchette 20/15/10 m con riflettori e con direttori, misure di partenza)
            for (int cy = 0; cy < 2; cy++)
            {
                try
                {
                    YmSpec ym = new YmSpec();
                    DipoloMultibanda.Iniziale(ym.Rad);
                    foreach (string b in ym.Rad.Bande) ym.Direttore[b] = cy == 1;
                    YagiMultibanda.Iniziale(ym);
                    l.Add(new Voce { Gruppo = "Yagi multibanda HF", Nome = "Yagi multibanda 20-15-10 " + (cy == 0 ? "riflettore-dipolo" : "dipolo-direttore"), Nec = YagiMultibanda.Esporta(ym, null) });
                }
                catch (Exception ex) { Passo("modello della Yagi multibanda " + cy, "creato", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // 0.4.30: antenne su PCB a 2,4 GHz (Calcolatori > 2,4 GHz commerciale), misure di partenza, geometria equivalente in aria (x sqrt eeff)
            for (int tp = 0; tp < 5; tp++)
            {
                try
                {
                    Pcb24Spec ps = new Pcb24Spec { Tipo = tp, H = tp == 1 ? 2 : 3 };
                    Pcb24.Iniziale(ps);
                    l.Add(new Voce { Gruppo = "2,4 GHz - antenne su PCB", Nome = Pcb24.Tipi[tp] + " FR4 2442 MHz (equivalente in aria)", Nec = Pcb24.Modello(ps, true).ToNec() });
                }
                catch (Exception ex) { Passo("modello su PCB " + Pcb24.Tipi[tp], "creato", ex.GetType().Name + ": " + ex.Message, false); }
            }
            // antenne riceventi: le schede con il modello NEC, sulle bande 160 e 80 m
            string[] rx = { "beverage", "bog", "ewe", "dhdl", "dhdl2", "flag", "pennant", "wf", "looprx", "vert" };
            double[] fb = { 1.83, 3.6, 7.05 };
            foreach (string kk in rx)
                for (int banda = 0; banda < 2; banda++)
                {
                    try
                    {
                        MainWindow.RxDefault d0 = MainWindow.RxDefaults(kk, banda);
                        RxParams p = new RxParams
                        {
                            F = fb[banda], L = d0.L, H = d0.H, Rterm = d0.R, Rrod = d0.Rrod, Dir = 0, H2 = d0.H2, D = d0.D, Vf = d0.Vf, Angle = d0.Ang,
                            Tl1 = 4, Tl2 = 3.25, Zl = 100, Ratio = 6, VfLinea = 0.66,
                            Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005 }
                        };
                        string nec = kk == "beverage" ? RxAntennas.Beverage(p) : kk == "bog" ? RxAntennas.Bog(p) : kk == "ewe" ? RxAntennas.Ewe(p) : kk == "dhdl" ? RxAntennas.Dhdl(p) :
                            kk == "looprx" ? RxAntennas.LoopRx(p) : kk == "flag" ? RxAntennas.Flag(p) : kk == "pennant" ? RxAntennas.Pennant(p) : kk == "wf" ? RxAntennas.WallerFlag(p) :
                            kk == "dhdl2" ? RxAntennas.DhdlBroadside(p) : RxAntennas.VerticaleRef(p);
                        l.Add(new Voce { Gruppo = "Antenne riceventi", Nome = kk + string.Format(CI, " {0:0.##} MHz", fb[banda]), Nec = nec });
                    }
                    catch (Exception ex) { Passo("modello ricevente " + kk, "creato", ex.GetType().Name + ": " + ex.Message, false); }
                }
            return l;
        }

        /// <summary>
        /// 0.4.20 (fase 2H di compila.bat): confronto dei risultati di EZNEC (risultati_eznec\riepilogo_eznec.csv, scritto da
        /// confronta_eznec.ps1) con il riferimento di YA-NEC. Esito per modello: OK entro lo 0,5 % sull'impedenza; AVVISO se oltre ma il
        /// .ez non contiene una parte del modello (scritta nel riferimento: il valore da confrontare e' allora quello del .ez riletto);
        /// ERRORE se EZNEC non lo calcola o se differisce senza una ragione nota. Resoconto in confronto_eznec.txt.
        /// </summary>
        public static int Confronta(string cartella)
        {
            sb = new StringBuilder();
            nOk = nErr = nAvv = 0;
            if (string.IsNullOrEmpty(cartella)) cartella = Path.Combine(AppPaths.DataDir, "eznec_calcolatori");
            string fEz = Path.Combine(cartella, "risultati_eznec", "riepilogo_eznec.csv"), fYa = Path.Combine(cartella, "riferimento_YA-NEC.csv");
            Riga("CONFRONTO YA-NEC / EZNEC DEI MODELLI DEI CALCOLATORI - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            if (!File.Exists(fEz) || !File.Exists(fYa))
            {
                Passo("file dei risultati", "riepilogo di EZNEC e riferimento di YA-NEC", (File.Exists(fEz) ? "" : "manca " + fEz + " ") + (File.Exists(fYa) ? "" : "manca " + fYa), false);
                Scrivi(cartella);
                return nErr;
            }
            Dictionary<string, string[]> ez = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            string[] testaEz = null;
            foreach (string l in File.ReadAllLines(fEz))
            {
                string[] c = l.TrimStart('\uFEFF').Split(';');
                if (testaEz == null) { testaEz = c; continue; }
                if (c.Length < 8) continue;
                if (c.Length > 5 && c[5] != "1" && c[5] != "") continue;   // solo la prima sorgente
                if (!ez.ContainsKey(c[0])) ez[c[0]] = c;
            }
            string[] righe = File.ReadAllLines(fYa);
            int nMod = 0;
            for (int k = 1; k < righe.Length; k++)
            {
                string[] y = righe[k].TrimStart('\uFEFF').Split(';');
                if (y.Length < 17) continue;
                nMod++;
                string nome = y[0];
                Riga("");
                Riga("MODELLO " + nome);
                string[] e;
                if (!ez.TryGetValue(nome, out e)) { Passo("risultato di EZNEC", "presente", "manca", false); continue; }
                if (e[2] != "0") { Passo("calcolo di EZNEC", "risposta 0 (calcolato)", "risposta " + e[2] + " " + (e.Length > 12 ? e[12] : ""), false); continue; }
                double er, ex, yr, yx, rr, rx;
                if (!Num(e[6], out er) || !Num(e[7], out ex) || !Num(y[5], out yr) || !Num(y[6], out yx)) { Passo("valori", "numeri", "non leggibili", false); continue; }
                Complex ze = new Complex(er, ex), zy = new Complex(yr, yx);
                double sc = Complex.Abs(ze - zy) / Math.Max(1, Complex.Abs(zy));
                bool perso = y.Length > 16 && y[16].Split('|').Any(x => x.Trim().Length > 0 && !x.Trim().StartsWith("Formato .ez sperimentale", StringComparison.Ordinal));
                if (sc <= 0.005) Passo("impedenza EZNEC / YA-NEC", "entro lo 0,5 %", string.Format(CI, "EZNEC {0}, YA-NEC {1}, scarto {2:0.000} %", Z(ze), Z(zy), sc * 100), true);
                else if (nome.Contains("(alto Q)"))
                    Passo("impedenza EZNEC / YA-NEC (loop accordata, Q di migliaia)", "entro il 3 % (un millesimo di differenza nell'induttanza sposta la risonanza; la stessa loop senza condensatore deve stare entro lo 0,5 %)",
                        string.Format(CI, "EZNEC {0}, YA-NEC {1}, scarto {2:0.000} %", Z(ze), Z(zy), sc * 100), false, sc <= 0.03);
                else if (perso && Num(y[13], out rr) && Num(y[14], out rx))
                {
                    Complex z2 = new Complex(rr, rx);
                    double s2 = Complex.Abs(ze - z2) / Math.Max(1, Complex.Abs(z2));
                    // 0.4.32 (collaudo 0.4.31, 2H: XR-5 35,6 %): il .ez scritto da YA-NEC non accende la Stepped Diameter Correction (il bit della
                    // testata non e' ancora noto: lo cerca la fase 2O), mentre YA-NEC rilegge il .ez con Leeson acceso. Il confronto non e' alla pari: avviso.
                    bool senzaSdc = y[16].Contains("Correzione Leeson");
                    Passo("impedenza EZNEC / YA-NEC (il .ez non contiene tutto il modello)", senzaSdc ? "EZNEC senza la Stepped Diameter Correction (il .ez di YA-NEC non la accende ancora, fase 2O): solo avviso" : "EZNEC uguale al .ez riletto da YA-NEC entro lo 0,5 %",
                        string.Format(CI, "EZNEC {0}, YA-NEC {1} (scarto {2:0.00} %), .ez riletto {3} (scarto {4:0.000} %); manca nel .ez: {5}", Z(ze), Z(zy), sc * 100, Z(z2), s2 * 100, y[16]), false, s2 <= 0.005 || senzaSdc);
                }
                else Passo("impedenza EZNEC / YA-NEC", "entro lo 0,5 %", string.Format(CI, "EZNEC {0}, YA-NEC {1}, scarto {2:0.000} %", Z(ze), Z(zy), sc * 100), false);
            }
            Riga("");
            Riga(nErr == 0 ? string.Format(CI, "CONFRONTO CON EZNEC: SUPERATO ({0} modelli, {1} avvisi).", nMod, nAvv) : string.Format(CI, "CONFRONTO CON EZNEC: {0} ERRORI su {1} modelli.", nErr, nMod));
            Scrivi(cartella);
            return nErr;
        }

        static void Scrivi(string cartella)
        {
            try { File.WriteAllText(Path.Combine(cartella, "confronto_eznec.txt"), sb.ToString(), new UTF8Encoding(false)); } catch { }
        }

        static bool Num(string s, out double v) { return double.TryParse((s ?? "").Trim(), NumberStyles.Float, CI, out v); }

        static string Pulito(string s)
        {
            StringBuilder b = new StringBuilder();
            char[] no = Path.GetInvalidFileNameChars();
            foreach (char c in s) b.Append(Array.IndexOf(no, c) >= 0 || c == '\'' ? '_' : c);
            return b.ToString().Trim();
        }

        static void Esporta(Voce v, string dEz, string dNec)
        {
            string nome = Pulito(v.Nome), gruppo = Pulito(v.Gruppo);
            string fEz = Path.Combine(dEz, gruppo, nome + ".ez"), fNec = Path.Combine(dNec, gruppo, nome + ".nec");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fEz)); Directory.CreateDirectory(Path.GetDirectoryName(fNec));
                NecModel m = NecModel.Parse(v.Nec);
                if (v.Correnti) m.CurrentSources = true;
                if (m.Comments.Count == 0) m.Comments.Add(v.Nome);
                else m.Comments[0] = v.Nome;   // titolo del .ez (30 caratteri): il nome della scheda
                double f = m.FreqMHz;
                File.WriteAllText(fNec, m.ToNec(), new UTF8Encoding(false));
                SegmentGeometry g = m.BuildGeometry(new List<string>());
                RunResult r = Analysis.Run(m, g, f, 5.0, CancellationToken.None);
                List<string> perso = new List<string>();
                byte[] ez = FormatiExport.Ez(m, perso);
                File.WriteAllBytes(fEz, ez);
                // controllo del salvataggio: il .ez riletto deve dare lo stesso modello
                NecModel m2 = EzImport.LoadEz(fEz);
                SegmentGeometry g2 = m2.BuildGeometry(new List<string>());
                RunResult r2 = Analysis.Run(m2, g2, f, 10.0, CancellationToken.None);
                double sc = Complex.Abs(r2.Zin - r.Zin) / Math.Max(1, Complex.Abs(r.Zin));
                string terr = m.Ground.Kind == GroundKind.FreeSpace ? "spazio libero" : m.Ground.Kind == GroundKind.Perfect ? "perfetto" :
                    (m.Ground.MiniNec ? "MININEC" : m.Ground.Sommerfeld ? "Sommerfeld" : "GN 0") + string.Format(CI, " {0:0.##}/{1:0.####}", m.Ground.Epsr, m.Ground.Sigma);
                Passo("modello e calcolo di YA-NEC", "impedenza e guadagno validi",
                    string.Format(CI, "{0:0.###} MHz, {1} segmenti, {2}: Z = {3}, Gmax {4:0.00} dBi, F/B {5:0.00} dB, efficienza {6:0.0} %", f, g.N, terr, Z(r.Zin), r.GmaxDbi, r.FrontBack, r.Efficiency),
                    Finito(r.Zin.Real) && Finito(r.Zin.Imaginary));
                // con GN 0 il .ez ha il terreno High Accuracy (Sommerfeld): differenze di qualche decimo di ohm, si accetta l'1 %
                double tol = m.Ground.Kind == GroundKind.Real && !m.Ground.Sommerfeld && !m.Ground.MiniNec ? 0.01 : 0.005;
                Passo("salvataggio .ez e rilettura", string.Format(CI, "stessi fili e sorgenti, stessa impedenza entro {0:0.#} %", tol * 100),
                    string.Format(CI, "{0} byte; riletto {1} fili, {2} sorgenti: Z = {3} (scarto {4:0.000} %)", ez.Length, m2.Geometry.Count, m2.Sources.Count, Z(r2.Zin), sc * 100),
                    m2.Sources.Count == m.Sources.Count && sc <= tol, perso.Exists(x => !x.StartsWith("Formato .ez sperimentale", StringComparison.Ordinal)));   // con parti che il .ez non ha (sotto) e' un avviso, non un errore
                foreach (string p in perso) Info("non scritto nel .ez: " + p);
                // collaudo 0.4.20 (IC8TEM, 14/11/2026 16:01: "devi essere rigoroso sulla creazione dei modelli nec delle schede"): i modelli
                // creati dai calcolatori non devono avere contatti fuori giunzione (le sei regole del Geometry Check di EZNEC: errore) ne'
                // altri avvisi del calcolo (segmenti fuori misura, terreno: avviso, da spiegare nella scheda e nel manuale)
                List<string> contatti = TerrenoVicino.ContattiFuoriGiunzione(g);
                Passo("geometria del modello del calcolatore", "nessun contatto fuori giunzione (EZNEC lo calcolerebbe senza Geometry Error)",
                    contatti.Count == 0 ? "nessuno" : string.Join(" | ", contatti), contatti.Count == 0);
                List<string> altri = r.Warnings.FindAll(w => !contatti.Contains(w));
                if (altri.Count > 0) Passo("altri avvisi del calcolo", "nessuno, oppure approssimazione dichiarata nella scheda", string.Join(" | ", altri), false, true);
                csv.AppendLine(string.Join(";", new[]
                {
                    Path.Combine(gruppo, nome + ".ez"), v.Gruppo, f.ToString("0.######", CI), g.N.ToString(CI), terr,
                    r.Zin.Real.ToString("0.0000", CI), r.Zin.Imaginary.ToString("0.0000", CI), r.Swr.ToString("0.000", CI), r.GmaxDbi.ToString("0.00", CI),
                    r.ElevationOfMax.ToString("0.0", CI), r.PhiMax.ToString("0.0", CI), Finito(r.FrontBack) ? r.FrontBack.ToString("0.00", CI) : "", r.Efficiency.ToString("0.00", CI),
                    r2.Zin.Real.ToString("0.0000", CI), r2.Zin.Imaginary.ToString("0.0000", CI), (sc * 100).ToString("0.000", CI),
                    string.Join(" | ", perso).Replace(";", ","), string.Join(" | ", r.Warnings).Replace(";", ",")
                }));
            }
            catch (Exception ex)
            {
                Passo("esportazione", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false);
            }
        }

        static bool Finito(double x) { return !double.IsNaN(x) && !double.IsInfinity(x); }
        static string Z(Complex z) { return string.Format(CI, "{0:0.000} {1} j{2:0.000} ohm", z.Real, z.Imaginary < 0 ? "-" : "+", Math.Abs(z.Imaginary)); }
        static void Riga(string s) { sb.AppendLine(s); }
        static void Info(string s) { sb.AppendLine("         nota:     " + s); }

        static void Passo(string cosa, string atteso, string ottenuto, bool ok, bool soloAvviso = false)
        {
            string e = ok ? "OK" : (soloAvviso ? "AVVISO" : "ERRORE");
            if (ok) nOk++; else if (soloAvviso) nAvv++; else nErr++;
            Riga("  " + cosa);
            if (!string.IsNullOrEmpty(atteso)) Riga("         atteso:   " + atteso);
            Riga("         ottenuto: " + ottenuto);
            Riga("         esito:    " + e);
        }
    }
}
