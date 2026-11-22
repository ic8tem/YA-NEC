// YA-NEC - avvio (Windows, Linux, Mac).
// --autotest: confronto del motore con nec2c (data/autotest.txt) e termina.
// --collaudo: tutte le procedure di collaudo senza finestra (data/collaudo.txt) e termina.
// --prove-pesanti (0.4.18): i 3 modelli piu' pesanti senza finestra, tempi delle fasi del motore (data/prove_pesanti.txt) e termina.
// --passi <resoconto> <uscita>: elenco dei passi con AVVISO o ERRORE del resoconto (per compila.bat).
// --prove-sensibili (0.4.20): modelli sensibili e aggressivi, LU a blocchi contro classica, calibrazione con EZNEC (data/prove_sensibili.txt).
// --confronta-eznec [cartella] (0.4.20): confronto dei risultati di EZNEC con il riferimento di YA-NEC (confronto_eznec.txt).
// --confronta-archivio [cartella] (0.4.20): modelli d'archivio ancora oltre lo 0,1 % da EZNEC, riletti e ricalcolati (confronto_archivio.txt).
// --esporta-eznec [cartella] (0.4.20): modelli dei calcolatori in .nec e .ez, riletti e ricalcolati, per le prove con EZNEC (compila.bat).
// --prepara-gradini [archivio] [uscita], --confronta-gradini [uscita] (0.4.30): elementi a gradini uno per uno contro EZNEC (fasi 2K e 2L).
// --tara-gradini [uscita] (0.5.3): taratura di Leeson sotto la risonanza sui valori di EZNEC (fase 2L bis).
// --prepara-varianti [archivio] [uscita], --confronta-varianti [uscita] (0.4.30): varianti dei 7 modelli vicini al terreno contro EZNEC (2M e 2N).
// --registro: avvia il programma normale scrivendo il registro anche in data/registro.txt (per compila.bat).
// Dalla 0.4.2 il registro e' sempre attivo nella cartella Registri accanto al programma (Diag/Registro.cs) e le librerie
// native (DLL, .so, .dylib) stanno nella sottocartella "lib" (Librerie.Prepara, prima di qualunque altra cosa).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;

namespace YaNec
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // --passi <resoconto> <uscita>: elenco dei passi da guardare per compila.bat (senza finestra e senza registro)
            if (args.Length >= 3 && args[0] == "--passi") return PassiDaGuardare.Scrivi(args[1], args[2]);
            // 1.5: prova d'importazione dei modelli senza finestra (IC8TEM 18/11/2026)
            if (args.Length >= 3 && args[0] == "--converti") { Librerie.Prepara(); return ConvertiModelli.Esegui(args[1], args[2]); }
            // 1.5.7+: prova di tutte le combinazioni della scheda Filari, senza finestra (IC8TEM 22/11/2026)
            if (args.Length >= 2 && args[0] == "--prova-filari") { Librerie.Prepara(); return ProvaFilari.Esegui(args[1]); }
            Librerie.Prepara();
            List<string> la = new List<string>(args);
            Engine.Dem.Cartella = AppPaths.DataDir;   // 1.2.0: DEM scaricati in data/dem
            string copia = la.Contains("--registro") || la.Contains("--prove-interfaccia") || la.Contains("--prove-butternut") ? Path.Combine(AppPaths.DataDir, "registro.txt") : null;
            string modo = la.Contains("--prepara-varianti") ? "varianti vicino al terreno per EZNEC" : la.Contains("--confronta-varianti") ? "varianti vicino al terreno contro EZNEC" : la.Contains("--prepara-gradini") ? "elementi a gradini per EZNEC" : la.Contains("--confronta-gradini") ? "elementi a gradini contro EZNEC" : la.Contains("--tara-gradini") ? "taratura di Leeson sugli elementi a gradini" : la.Contains("--confronta-archivio") ? "confronto d'archivio con EZNEC" : la.Contains("--confronta-eznec") ? "confronto con EZNEC" : la.Contains("--prove-sensibili") ? "modelli sensibili" : la.Contains("--esporta-eznec") ? "esportazione per EZNEC" : la.Contains("--prove-pesanti") ? "modelli pesanti" : la.Contains("--collaudo") ? "collaudo" : la.Contains("--autotest") ? "autotest" : la.Contains("--prove-interfaccia") ? "prove dell'interfaccia" :
                la.Contains("--prove-butternut") ? "prova della Butternut" : "interfaccia grafica";
            Registro.Avvia(copia, modo);
            Registro.Scrivi("AVVIO", "librerie native: " + Librerie.Descrizione);
            foreach (string a in args) if (a == "--solo-novita") Collaudo.SoloNovita = true;
            // 0.4.30: le prove che confrontano con nec2c usano la sua velocita' della luce (299,8); quelle con EZNEC e il programma normale 299,792458
            if (la.Contains("--collaudo") || la.Contains("--autotest") || la.Contains("--prove-pesanti") || la.Contains("--prove-sensibili") || la.Contains("--prove-interfaccia") || la.Contains("--prove-butternut"))
            {
                Engine.Nec2Engine.Cvel = Engine.Nec2Engine.CVEL_NEC2;
                Engine.Nec2Engine.CvelBloccata = true;
                Registro.Scrivi("AVVIO", "velocita' della luce del motore 299,8 (come nec2c) per le prove di confronto");
            }
            // 0.4.20: senza finestra (collaudo, modelli pesanti, autotest, esportazione) il calcolo usa tutti i processori
            if (la.Contains("--collaudo") || la.Contains("--prove-pesanti") || la.Contains("--autotest") || la.Contains("--esporta-eznec") || la.Contains("--prove-sensibili") || la.Contains("--confronta-archivio") || la.Contains("--prepara-gradini") || la.Contains("--confronta-gradini") || la.Contains("--tara-gradini") || la.Contains("--prepara-varianti") || la.Contains("--confronta-varianti")) Engine.Processori.RiservaInterfaccia = false;
            foreach (string a in args)
            {
                if (a == "--collaudo")
                {
                    int rc;
                    using (Prestazioni.Misura("Collaudo completo")) rc = Collaudo.Esegui();
                    Registro.Scrivi("FINE", "collaudo terminato, codice " + rc);
                    Registro.Chiudi();
                    return rc;
                }
                // 0.4.18 (richiesta di IC8TEM del 14/11/2026 09:17): i 3 modelli piu' pesanti, tempi delle fasi del motore (fase 2E di compila.bat)
                if (a == "--prove-pesanti")
                {
                    int rc;
                    using (Prestazioni.Misura("Collaudo dei modelli pesanti")) rc = ModelliPesanti.Esegui();
                    Registro.Scrivi("FINE", "modelli pesanti terminati, codice " + rc);
                    Registro.Chiudi();
                    return rc;
                }
                // 0.4.20 (richiesta di IC8TEM del 14/11/2026 15:25): i modelli piu' sensibili e aggressivi, con la calibrazione EZNEC (fase 2F)
                if (a == "--prove-sensibili")
                {
                    int rc;
                    using (Prestazioni.Misura("Collaudo dei modelli sensibili")) rc = ProveSensibili.Esegui();
                    Registro.Scrivi("FINE", "modelli sensibili terminati, codice " + rc);
                    Registro.Chiudi();
                    return rc;
                }
                // 0.4.20 (richiesta di IC8TEM del 14/11/2026 15:25): modelli dei calcolatori in .nec e .ez per la calibrazione con EZNEC
                // 0.4.20 (IC8TEM 14/11/2026 18:54): i soli modelli d'archivio con scarti da EZNEC oltre lo 0,1 %
                if (a == "--confronta-archivio")
                {
                    int i = la.IndexOf("--confronta-archivio");
                    string cart = i + 1 < la.Count && !la[i + 1].StartsWith("--", StringComparison.Ordinal) ? la[i + 1] : null;
                    int rc;
                    using (Prestazioni.Misura("Confronto d'archivio con EZNEC")) rc = ConfrontoArchivio.Esegui(cart);
                    Registro.Scrivi("FINE", "confronto d'archivio terminato, codice " + rc);
                    Registro.Chiudi();
                    return rc;
                }
                // 0.4.30 (IC8TEM 14/11/2026 23:13): varianti dei 7 modelli vicini al terreno contro EZNEC (quota, segmenti, terreno)
                if (a == "--prepara-varianti" || a == "--confronta-varianti")
                {
                    int i = la.IndexOf(a);
                    string c1 = i + 1 < la.Count && !la[i + 1].StartsWith("--", StringComparison.Ordinal) ? la[i + 1] : null;
                    string c2 = i + 2 < la.Count && c1 != null && !la[i + 2].StartsWith("--", StringComparison.Ordinal) ? la[i + 2] : null;
                    int rc;
                    using (Prestazioni.Misura("Varianti vicino al terreno contro EZNEC"))
                        rc = a == "--prepara-varianti" ? VariantiTerreno.Prepara(c1, c2) : VariantiTerreno.Confronta(c1);
                    Registro.Scrivi("FINE", "varianti vicino al terreno terminate, codice " + rc);
                    Registro.Chiudi();
                    return rc;
                }
                // 0.4.30 (IC8TEM 14/11/2026 21:52): elementi a gradini uno per uno contro la Stepped Diameter Correction di EZNEC
                if (a == "--prepara-gradini" || a == "--confronta-gradini")
                {
                    int i = la.IndexOf(a);
                    string c1 = i + 1 < la.Count && !la[i + 1].StartsWith("--", StringComparison.Ordinal) ? la[i + 1] : null;
                    string c2 = i + 2 < la.Count && c1 != null && !la[i + 2].StartsWith("--", StringComparison.Ordinal) ? la[i + 2] : null;
                    int rc;
                    using (Prestazioni.Misura("Elementi a gradini contro EZNEC"))
                        rc = a == "--prepara-gradini" ? GradiniElementi.Prepara(c1, c2) : GradiniElementi.Confronta(c1);
                    Registro.Scrivi("FINE", "elementi a gradini terminati, codice " + rc);
                    Registro.Chiudi();
                    return rc;
                }
                // 0.5.3 (modifica Leeson A/B, via di IC8TEM 15/11/2026 21:57): prova B, taratura sotto la risonanza sui valori di EZNEC
                if (a == "--tara-gradini")
                {
                    int i = la.IndexOf(a);
                    string c1 = i + 1 < la.Count && !la[i + 1].StartsWith("--", StringComparison.Ordinal) ? la[i + 1] : null;
                    int rc;
                    using (Prestazioni.Misura("Taratura di Leeson"))
                        rc = TaraturaLeeson.Tara(c1);
                    Registro.Scrivi("FINE", "taratura di Leeson terminata, codice " + rc);
                    Registro.Chiudi();
                    return rc;
                }
                if (a == "--confronta-eznec")
                {
                    int i = la.IndexOf("--confronta-eznec");
                    string cart = i + 1 < la.Count && !la[i + 1].StartsWith("--", StringComparison.Ordinal) ? la[i + 1] : null;
                    int rc = EsportaEznec.Confronta(cart);
                    Registro.Scrivi("FINE", "confronto con EZNEC terminato, codice " + rc);
                    Registro.Chiudi();
                    return rc;
                }
                if (a == "--esporta-eznec")
                {
                    int i = la.IndexOf("--esporta-eznec");
                    string cart = i + 1 < la.Count && !la[i + 1].StartsWith("--", StringComparison.Ordinal) ? la[i + 1] : null;
                    int rc;
                    using (Prestazioni.Misura("Esportazione dei calcolatori per EZNEC")) rc = EsportaEznec.Esegui(cart);
                    Registro.Scrivi("FINE", "esportazione per EZNEC terminata, codice " + rc);
                    Registro.Chiudi();
                    return rc;
                }
                if (a == "--prove-interfaccia") MainWindow.ProveAutomatiche = true;
                if (a == "--schermate") { MainWindow.ProveAutomatiche = true; MainWindow.ProveSchermate = true; }   // 1.2.0: immagini della finestra per README e wiki (data/schermate)   // le prove a mano eseguite dal programma stesso
                // 0.4.17 (richiesta di IC8TEM del 14/11/2026 09:12): la Butternut da 7877 segmenti si prova per ultima, a parte (fase 2D di compila.bat)
                if (a == "--prove-butternut") { MainWindow.ProveAutomatiche = true; MainWindow.ProveSoloButternut = true; }
                // 0.4.13: solo le novita' dopo la 0.4.10 (compila.bat senza /completo)
                if (a == "--solo-novita") { MainWindow.ProveSoloNovita = true; Collaudo.SoloNovita = true; }
            }
            foreach (string a in args)
                if (a == "--autotest")
                {
                    int bad = 0;
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    List<Engine.SelfTest.Line> righe;
                    using (Prestazioni.Misura("Autotest del motore")) righe = new List<Engine.SelfTest.Line>(Engine.SelfTest.Run());
                    foreach (Engine.SelfTest.Line l in righe)
                    {
                        sb.AppendLine((l.Pass ? "OK     " : "ERRORE ") + l.Name + "  " + l.Detail);
                        if (!l.Pass) bad++;
                    }
                    // 1.0.14: anche l'autotest del calcolatore Parabola (casi di Baars e Balanis); le prove fallite si ripetono in fondo
                    var errPar = new List<string>();
                    using (Prestazioni.Misura("Autotest della parabola"))
                        Engine.ParabolaSelfTest.Esegui(r => { sb.AppendLine("       " + r); if (r.StartsWith("ERR", StringComparison.Ordinal)) errPar.Add(r); });
                    // 1.0.16: biquad e doppia biquad a 2442 MHz, adattate col NEC: valori di controllo (fonti: 11-12 dBi la biquad, 13-14 la doppia)
                    using (Prestazioni.Misura("Autotest della biquad"))
                        foreach (int tipo in new[] { 0, 1 })
                        {
                            try
                            {
                                var bq = new Engine.CalcBiquad();
                                bq.Par("tipo").Value = tipo;
                                if (tipo == 1) { bq.Par("rw").Value = 132; bq.Par("rh").Value = 282; bq.Par("dist").Value = 11; bq.Par("lato").Value = 30.9; bq.Par("filo").Value = 1.5; }
                                bq.Compute();
                                bq.Tune(2442);
                                var oq = bq.Compute();
                                var rq = Engine.SimpleCalc.Solve(oq, 2442, 50);
                                string nome = tipo == 0 ? "biquad" : "doppia biquad";
                                string riga = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}: lato {1:0.0} mm, distanza {2:0.0} mm, Z {3:0.0} {4:+0.0;-0.0;+0.0}j, ROS {5:0.00}, G {6:0.00} dBi, F/B {7:0.0} dB, {8} segmenti",
                                    nome, bq.P("lato") * bq.Scale, bq.P("dist"), rq.Zin.Real, rq.Zin.Imaginary, rq.Swr, rq.GmaxDbi, Engine.CalcBiquad.FronteRetro(rq), bq.UltimiSegmenti);
                                bool ok = rq.Swr < 1.6 && rq.GmaxDbi > (tipo == 0 ? 9.5 : 11.5) && rq.GmaxDbi < (tipo == 0 ? 12.8 : 15.0);
                                sb.AppendLine("       " + (ok ? "OK   " : "ERR  ") + riga);
                                if (!ok) errPar.Add(riga);
                            }
                            catch (Exception ex) { errPar.Add("biquad: " + ex.Message); }
                        }
                    // 1.0.17: confronti con casi pubblicati (Yagi a 6 elementi ed elica di MathWorks)
                    using (Prestazioni.Misura("Autotest dei confronti con la letteratura"))
                        try { Engine.ConfrontiLetteratura.Esegui(l => sb.AppendLine("       " + l), errPar); } catch (Exception ex) { errPar.Add("confronti: " + ex.Message); }
                    // 1.0.17: Big Wheel (F1BKM) e tre dipoli (Cebik) adattati col NEC
                    using (Prestazioni.Misura("Autotest della Big Wheel"))
                        foreach (int tipo in new[] { 0, 1, 2 })
                        {
                            try
                            {
                                var bw = new Engine.CalcBigWheel();
                                bw.Compute();
                                bw.Par("tipo").Value = tipo;
                                if (tipo > 0) bw.Par("f").Value = 146;
                                bw.Compute();
                                bw.Tune(bw.P("f"));
                                int c = bw.SweepF.Length / 2;
                                string nome = tipo == 0 ? "Big Wheel F1BKM" : tipo == 1 ? "tre dipoli a triangolo" : "tre dipoli a cerchio";
                                string riga = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    "{0}: {1} {2:0.0} mm, {3} {4:0.0} mm, Z mozzo {5:0.0} {6:+0.0;-0.0;+0.0}j, ROS {7:0.00}, G orizzonte {8:0.00} dBi, circolarita' {9:0.00} dB",
                                    nome, tipo == 0 ? "petalo" : "dipolo", tipo == 0 ? bw.P("lp") : bw.P("ld"), tipo == 0 ? "stub" : "raggio", tipo == 0 ? bw.P("ls") : bw.P("rd"),
                                    bw.Zmozzo.Real, bw.Zmozzo.Imaginary, bw.SweepSwr[c], bw.Gorizz, bw.Circolarita);
                                bool ok = bw.SweepSwr[c] < 2.0 && bw.Circolarita < 3.0 && bw.Gorizz > -2 && bw.Gorizz < 5;
                                sb.AppendLine("       " + (ok ? "OK   " : "ERR  ") + riga);
                                if (!ok) errPar.Add(riga);
                            }
                            catch (Exception ex) { errPar.Add("Big Wheel: " + ex.Message); }
                        }
                    // 1.0.17: Loop Skywire 80 m, quadrato a 14 m, scala 450 ohm da 30 m: tutte le bande col NEC
                    using (Prestazioni.Misura("Autotest del Loop Skywire"))
                        try
                        {
                            var sk = new Engine.CalcSkywire();
                            sk.Compute();
                            sk.Tune(3.65);
                            for (int k = 0; k < sk.BF.Length; k++)
                            {
                                string riga = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Loop Skywire {0} ({1:0.000} MHz): Z {2}, ROS {3} sulla scala 450, perdita {4} dB, G {5} dBi a {6} gradi",
                                    sk.Righe[k][0], sk.BF[k], sk.Righe[k][1], sk.Righe[k][2], sk.Righe[k][3], sk.Righe[k][4], sk.Righe[k][5]);
                                bool okb = sk.BPerd[k] < 3 && sk.BG[k] > 2 && sk.BG[k] < 15;
                                if (k == 0) okb &= sk.BRos[k] < 6;
                                sb.AppendLine("       " + (okb ? "OK   " : "ERR  ") + riga);
                                if (!okb) errPar.Add(riga);
                            }
                        }
                        catch (Exception ex) { errPar.Add("Loop Skywire: " + ex.Message); }
                    // 1.1.0: cielo vero (mappa di G8ONH, tempo siderale, Luna) e studio degli stack (accoppiamento VHF, stack HF sopra il terreno)
                    using (Prestazioni.Misura("Autotest del cielo vero e degli stack"))
                        try
                        {
                            var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                            var mp = Engine.CieloVero.Mappa(System.IO.Path.Combine(AppPaths.DataDir, "cielo"), 144);
                            double tFreddo = mp.Temperatura(142, 30), tCentro = mp.Temperatura(266.4, -28.94);
                            bool okm = tFreddo > 140 && tFreddo < 200 && tCentro > 10000;
                            string rm = string.Format(CI0, "Mappa del cielo 144 MHz (G8ONH): punto freddo RA 142 Dec 30 = {0:0.0} K (skynoise: media 190,7 K sul lobo), centro galattico = {1:0} K", tFreddo, tCentro);
                            sb.AppendLine("       " + (okm ? "OK   " : "ERR  ") + rm); if (!okm) errPar.Add(rm);
                            double lm = Engine.CieloVero.Lmst(new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc), 0);
                            bool okl = Math.Abs(lm - 280.46) < 0.02;
                            string rl = string.Format(CI0, "Tempo siderale a J2000 (Greenwich): {0:0.000} gradi (atteso 280,46)", lm);
                            sb.AppendLine("       " + (okl ? "OK   " : "ERR  ") + rl); if (!okl) errPar.Add(rl);
                            double laz, lel; Engine.CieloVero.Luna(new DateTime(2026, 11, 17, 21, 0, 0, DateTimeKind.Utc), 40.55, 14.21, out laz, out lel);
                            sb.AppendLine(string.Format(CI0, "       INFO Luna da JN70CN il 17/11/2026 21:00 UTC: azimut {0:0.0}, elevazione {1:0.0} gradi (Meeus ridotto)", laz, lel));
                            // stack VHF: 2 Yagi MathWorks sovrapposte, alla distanza DL6WU (nec2c: |S21| -27,6 dB, 12,08 dBi)
                            // stessa Yagi di ConfrontiLetteratura ma con il lobo orizzontale (+Y), elementi lungo X: lo stack sovrapposto (Z) e' nel piano H
                            double[] vy = { 0.90846, 0.84596, 0.72096, 0.65846, 0.72096, 0.65846, 0.42008, 0.36338, 0.23838, 0.51673, 0.53236, 0.016118 };
                            double[] py = { 0, vy[6], vy[6] + vy[7], vy[6] + vy[7] + vy[8], vy[6] + vy[7] + vy[8] + vy[9], vy[6] + vy[7] + vy[8] + vy[9] + vy[10] };
                            double[] ly = { vy[0], vy[1], vy[2], vy[3], vy[4], vy[5] };
                            var dk = new System.Text.StringBuilder("CM Yagi MathWorks con il lobo orizzontale\nCE\n");
                            Func<double, string> fq = x => x.ToString("R", CI0);
                            dk.Append("GW 1 21 " + fq(-ly[0] / 2) + " 0 0 " + fq(ly[0] / 2) + " 0 0 0.00635\n");
                            dk.Append("GW 2 21 " + fq(-ly[1] / 2) + " " + fq(py[1]) + " 0 " + fq(ly[1] / 2) + " " + fq(py[1]) + " 0 0.00635\n");
                            dk.Append("GW 3 21 " + fq(-ly[1] / 2) + " " + fq(py[1]) + " " + fq(vy[11]) + " " + fq(ly[1] / 2) + " " + fq(py[1]) + " " + fq(vy[11]) + " 0.00635\n");
                            dk.Append("GW 4 1 " + fq(-ly[1] / 2) + " " + fq(py[1]) + " 0 " + fq(-ly[1] / 2) + " " + fq(py[1]) + " " + fq(vy[11]) + " 0.00635\n");
                            dk.Append("GW 5 1 " + fq(ly[1] / 2) + " " + fq(py[1]) + " 0 " + fq(ly[1] / 2) + " " + fq(py[1]) + " " + fq(vy[11]) + " 0.00635\n");
                            for (int q = 2; q < 6; q++) dk.Append("GW " + (q + 4) + " 21 " + fq(-ly[q] / 2) + " " + fq(py[q]) + " 0 " + fq(ly[q] / 2) + " " + fq(py[q]) + " 0 0.00635\n");
                            dk.Append("GE 0\nEX 0 2 11 0 1 0\nFR 0 1 0 0 165 0\nEN\n");
                            var my = Engine.NecModel.Parse(dk.ToString());
                            var sv = Engine.StackStudio.Vhf(my, 165, 2, 1, 0.6, 1.0, 300, 3, -13, 200, 1000, 30, 5, System.Threading.CancellationToken.None);
                            var p1 = sv.Dl6wu ?? sv.Punti[sv.Punti.Count - 1];
                            bool oks = Math.Abs(p1.S21 + 27.6) < 1.5 && Math.Abs(p1.G - 12.08) < 0.6 && p1.Sll < -3 && p1.Sll > -15;
                            string rs = string.Format(CI0, "Stack 2 Yagi MathWorks sovrapposte a distanza DL6WU ({0:0.000} m): |S21| {1:0.0} dB, G {2:0.00} dBi, lobo {3:0.0} dB (nec2c: -27,6 dB, 12,08 dBi, -8,3 dB); proposta {4:0.00} x DL6WU ({5})", p1.Dv, p1.S21, p1.G, p1.Sll, sv.Proposta.K, sv.Criterio);
                            sb.AppendLine("       " + (oks ? "OK   " : "ERR  ") + rs); if (!oks) errPar.Add(rs);
                            // stack HF: Yagi 3 elementi 20 m, la piu' bassa a 15 m, suolo medio (nec2c a 0,75 lambda: BIP 15,63 dBi a 11 gradi)
                            string y3 = "CM Yagi 3 el 20 m di prova\nCE\nGW 1 21 -3.17 -5.44 0 -3.17 5.44 0 0.0125\nGW 2 21 0 -5.13 0 0 5.13 0 0.0125\nGW 3 21 3.17 -4.81 0 3.17 4.81 0 0.0125\nGE 0\nEX 0 2 11 0 1 0\nFR 0 1 0 0 14.175 0\nEN\n";
                            var sh = Engine.StackStudio.Hf(Engine.NecModel.Parse(y3), 14.175, 2, 15, 0.75, 0.75, 5, 15, System.Threading.CancellationToken.None);
                            int ib = 0; for (int i = 1; i < sh.ElBip.Length; i++) if (sh.ElBip[i] > sh.ElBip[ib]) ib = i;
                            bool okh = Math.Abs(sh.ElBip[ib] - 15.63) < 0.5 && Math.Abs(ib - 11) <= 1;
                            string rh = string.Format(CI0, "Stack HF 2 Yagi 3 el 20 m (15 m + 0,75 lambda, suolo medio): BIP {0:0.00} dBi a {1} gradi, sopra da sola {2:0.00}, sotto da sola {3:0.00} dBi (nec2c: 15,63 a 11; 13,28; 12,88)", sh.ElBip[ib], ib, Max(sh.ElSopra), Max(sh.ElSotto));
                            sb.AppendLine("       " + (okh ? "OK   " : "ERR  ") + rh); if (!okh) errPar.Add(rh);
                        }
                        catch (Exception ex) { errPar.Add("cielo vero / stack: " + ex.Message); }
                    // 1.2.0: carichi HF (bozzetto 7): trappola LC ideale e confronto con il carico LD del NEC; trappola coassiale RG-58 per 40 m
                    using (Prestazioni.Misura("Autotest dei carichi HF"))
                        try
                        {
                            var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                            var di = new Engine.CarichiHf.Dati { Tipo = 0, LuH = 8.2, CpF = 60, QL = double.PositiveInfinity, QC = double.PositiveInfinity };
                            var ri = Engine.CarichiHf.Calcola(di);
                            double zf0 = ri.Z(ri.F0).Magnitude;
                            bool oki = zf0 > 1e8;
                            string r1 = string.Format(CI0, "Trappola LC ideale 8,2 uH / 60 pF: f0 {0:0.000} MHz, |Z(f0)| = {1:0.0e0} ohm (atteso: infinita)", ri.F0 / 1e6, zf0);
                            sb.AppendLine("       " + (oki ? "OK   " : "ERR  ") + r1); if (!oki) errPar.Add(r1);
                            // stessa trappola con Q 200 come carico LD 1 sul segmento della sorgente: Zin(con) - Zin(senza) = Z della trappola
                            var dq = new Engine.CarichiHf.Dati { Tipo = 0, LuH = 8.2, CpF = 60, QL = 200, QC = double.PositiveInfinity };
                            var rq = Engine.CarichiHf.Calcola(dq);
                            string dip = "CM dipolo 40 m di prova\nCE\nGW 1 21 0 -10 10 0 10 10 0.001\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 7.1 0\nEN\n";
                            double errMax = 0;
                            foreach (double fp in new[] { 3.65, 6.9, 7.3 })
                            {
                                var m0 = Engine.NecModel.Parse(dip);
                                var m1 = Engine.NecModel.Parse(dip);
                                // nel carico NEC la R in parallelo e' fissa (Q x w0 L): il confronto usa la stessa trappola a Q costante
                                double rp = rq.Q0 * 2 * Math.PI * rq.F0 * rq.L;
                                m1.Loads.Add(new Engine.LoadSpec { Type = 1, Tag = 1, From = 11, To = 11, F1 = rp, F2 = rq.L, F3 = rq.C });
                                var z0 = Engine.Analysis.Run(m0, m0.BuildGeometry(), fp, 10).Zin;
                                var z1 = Engine.Analysis.Run(m1, m1.BuildGeometry(), fp, 10).Zin;
                                double w = 2 * Math.PI * fp * 1e6;
                                System.Numerics.Complex zt = 1 / (1 / rp + 1 / new System.Numerics.Complex(0, w * rq.L) + new System.Numerics.Complex(0, w * rq.C));
                                errMax = Math.Max(errMax, ((z1 - z0) - zt).Magnitude / zt.Magnitude);
                            }
                            bool okl = errMax < 0.01;
                            string r2 = string.Format(CI0, "Trappola come carico NEC (LD 1) sul segmento della sorgente, 3,65 / 6,9 / 7,3 MHz: scarto massimo {0:0.000} % (tolleranza 1 %)", errMax * 100);
                            sb.AppendLine("       " + (okl ? "OK   " : "ERR  ") + r2); if (!okl) errPar.Add(r2);
                            var cavi = Engine.Cables.Load(System.IO.Path.Combine(AppPaths.DataDir, "cavi.csv"));
                            var dc = new Engine.CarichiHf.Dati { Tipo = 1, Cavo = cavi.Find(c => c.Name.StartsWith("RG-58")) };
                            dc.Spire = Engine.CarichiHf.SpirePerF0(dc, 7.1e6);
                            var rc = Engine.CarichiHf.Calcola(dc);
                            bool okc = Math.Abs(rc.F0 / 1e6 - 7.1) < 0.005 && rc.Q0 > 80 && rc.Q0 < 400;
                            string r3 = string.Format(CI0, "Trappola coassiale RG-58 su forma 40 mm per 40 m: {0:0.00} spire, f0 {1:0.000} MHz, L {2:0.00} uH, C {3:0} pF, cavo {4:0.00} m, Q {5:0}", dc.Spire, rc.F0 / 1e6, rc.L * 1e6, rc.C * 1e12, rc.LungCavo, rc.Q0);
                            sb.AppendLine("       " + (okc ? "OK   " : "ERR  ") + r3); if (!okc) errPar.Add(r3);
                            // trappola W8NX (ARRL Antenna Book 24a ed., par. 10.2): 8 3/4 spire di RG-59 (Belden 8241) su PVC da 2 3/8" (60,3 mm): 4 uH, 123 pF, 7,15 MHz
                            var dw = new Engine.CarichiHf.Dati { Tipo = 1, FormaMm = 60.3, Spire = 8.75, Blocca = "40 m", Cavo = cavi.Find(c => c.Name.StartsWith("RG-59/U (Belden 8241)")) };
                            var rw = Engine.CarichiHf.Calcola(dw);
                            bool okw = dw.Cavo != null && Math.Abs(rw.F0 / 7.15e6 - 1) < 0.015 && Math.Abs(rw.L / 4e-6 - 1) < 0.03 && Math.Abs(rw.C / 123e-12 - 1) < 0.03;
                            string r4 = string.Format(CI0, "Trappola W8NX (ARRL 24a ed.): 8,75 spire di RG-59 su PVC 60,3 mm: f0 {0:0.000} MHz (W8NX 7,15), L {1:0.00} uH (4), C {2:0} pF (123)", rw.F0 / 1e6, rw.L * 1e6, rw.C * 1e12);
                            sb.AppendLine("       " + (okw ? "OK   " : "ERR  ") + r4); if (!okw) errPar.Add(r4);
                        }
                        catch (Exception ex) { errPar.Add("carichi HF: " + ex.Message); }
                    // 1.2.0: meccanica (bozzetto 9): trave a sbalzo a sezione costante contro le formule chiuse; coefficiente di esposizione NTC 2018
                    using (Prestazioni.Misura("Autotest della meccanica"))
                        try
                        {
                            var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                            var tubi = new List<Engine.Meccanica.Tubo> { new Engine.Meccanica.Tubo { Da = 0, A = 3, D = 0.030, S = 0.002 } };
                            var es = Engine.Meccanica.Elemento(tubi, Engine.Meccanica.Leghe[0], 30, 0, true, 1.2);
                            double w = Engine.Meccanica.Pressione(30) * 1.2 * 0.030, mw = w * 9 / 2;
                            double di = 0.026, gq = Engine.Meccanica.G * Engine.Meccanica.DensAl * Math.PI * (0.030 * 0.030 - di * di) / 4;
                            double fq = Engine.Meccanica.FrecciaChiusa(gq, 3, Engine.Meccanica.E * Engine.Meccanica.Inerzia(0.030, 0.002));
                            double eM = Math.Abs(es.Mw[0] / mw - 1), eF = Math.Abs(es.Freccia[es.Freccia.Length - 1] / fq - 1);
                            bool okm = eM < 0.01 && eF < 0.01;
                            string r1 = string.Format(CI0, "Trave a sbalzo 30x2 mm lunga 3 m: momento al vincolo {0:0.0} N m (wL^2/2 = {1:0.0}), freccia in punta {2:0.00} mm (wL^4/8EI = {3:0.00}); scarti {4:0.00} % e {5:0.00} %", es.Mw[0], mw, es.Freccia[es.Freccia.Length - 1] * 1000, fq * 1000, eM * 100, eF * 100);
                            sb.AppendLine("       " + (okm ? "OK   " : "ERR  ") + r1); if (!okm) errPar.Add(r1);
                            double ce = Engine.Meccanica.Ce(1, 10);
                            bool okc = Math.Abs(ce - 2.35) < 0.02;
                            string r2 = string.Format(CI0, "NTC 2018: coefficiente di esposizione categoria II a 10 m = {0:0.000} (tabella: 2,35); zona 3 a 0 m: vb = {1:0.0} m/s", ce, Engine.Meccanica.Vb(Engine.Meccanica.Zone[2], 0));
                            sb.AppendLine("       " + (okc ? "OK   " : "ERR  ") + r2); if (!okc) errPar.Add(r2);
                        }
                        catch (Exception ex) { errPar.Add("meccanica: " + ex.Message); }
                    // 1.2.0: ricezione RDF (bozzetto 3): antenna isotropa (dipolo corto ideale non serve: si usa un dipolo di Hertz in spazio libero,
                    // RDF teorico 1,76 dB) e verticale lambda/4 con 4 radiali a 0,5 m su terreno medio (IV3PRK 5,07 dB, W8JI / ON4UN circa 4,8-5 dB)
                    using (Prestazioni.Misura("Autotest della ricezione RDF"))
                        try
                        {
                            var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                            var md = Engine.NecModel.Parse("CM dipolo corto\nCE\nGW 1 11 0 0 -0.5 0 0 0.5 0.001\nGE 0\nEX 0 1 6 0 1 0\nFR 0 1 0 0 10 0\nEN\n");
                            var ed = Engine.Analysis.Run(md, md.BuildGeometry(), 10, 10).Engine;
                            var rd = Engine.RdfRicezione.Calcola(ed, 10, 0, 0, 0, 0, 500, 0, 15, 10);
                            bool okd = Math.Abs(rd.Rdf - 1.76) < 0.05;
                            string r1 = string.Format(CI0, "RDF del dipolo corto in spazio libero: {0:0.00} dB (teorico 1,76 dB: direttivita' 1,5); un'antenna isotropa darebbe 0 dB", rd.Rdf);
                            sb.AppendLine("       " + (okd ? "OK   " : "ERR  ") + r1); if (!okd) errPar.Add(r1);
                            var mv = Engine.NecModel.Parse(Engine.RxAntennas.VerticaleRef(new Engine.RxParams { F = 1.83 }));
                            var ev = Engine.Analysis.Run(mv, mv.BuildGeometry(), 1.83, 10).Engine;
                            var rv = Engine.RdfRicezione.Calcola(ev, 1.83, 0, 20, 0, 0, 500, 0, 15, 10);
                            bool okv = rv.Rdf > 4.6 && rv.Rdf < 5.4;
                            string r2 = string.Format(CI0, "RDF della verticale lambda/4 (4 radiali a 0,5 m, terreno medio) a 20 gradi: {0:0.00} dB (IV3PRK 5,07; W8JI / ON4UN circa 4,8-5)", rv.Rdf);
                            sb.AppendLine("       " + (okv ? "OK   " : "ERR  ") + r2); if (!okv) errPar.Add(r2);
                        }
                        catch (Exception ex) { errPar.Add("ricezione RDF: " + ex.Message); }
                    // 1.2.0: test AGT fatto dal programma (bozzetto 11 D): dipolo 20 m a 10 m, attesi 1,000 in spazio libero e 2,000 sul terreno perfetto
                    using (Prestazioni.Misura("Autotest del test AGT"))
                        try
                        {
                            var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                            var ma = Engine.NecModel.Parse("CM dipolo 20 m\nCE\nGW 1 21 0 -5.13 10 0 5.13 10 0.001\nGE 0\nGN 2 0 0 0 13 0.005\nLD 5 0 0 0 5.8e7\nEX 0 1 11 0 1 0\nFR 0 1 0 0 14.175 0\nEN\n");
                            var lagt = Engine.AgtTest.Esegui(ma, System.Threading.CancellationToken.None);
                            bool oka = lagt.Count == 3 && Math.Abs(lagt[0].Valore - 1) < 0.02 && Math.Abs(lagt[1].Valore - 2) < 0.04 && Math.Abs(lagt[2].Valore - 1) < 0.02;
                            string ra = string.Format(CI0, "Test AGT del dipolo 20 m (rame, terreno medio tolti): spazio libero {0:0.000} (atteso 1,000), terreno perfetto {1:0.000} (atteso 2,000), segmenti raddoppiati {2:0.000}", lagt[0].Valore, lagt[1].Valore, lagt[2].Valore);
                            sb.AppendLine("       " + (oka ? "OK   " : "ERR  ") + ra); if (!oka) errPar.Add(ra);
                        }
                        catch (Exception ex) { errPar.Add("test AGT: " + ex.Message); }
                    // 1.2.0: terreno reale (bozzetti 1 HF e VHF): terreno piatto = NEC sul suolo piano entro 0,1 dB; direzioni di traffico da JN70CN82WE
                    using (Prestazioni.Misura("Autotest del terreno reale"))
                        try
                        {
                            var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                            var mt = Engine.NecModel.Parse("CM dipolo 20 m a 10 m\nCE\nGW 1 21 0 -5.13 10 0 5.13 10 0.001\nGE 1\nGN 2 0 0 0 13 0.005\nEX 0 1 11 0 1 0\nFR 0 1 0 0 14.175 0\nEN\n");
                            var rt = Engine.Analysis.Run(mt, mt.BuildGeometry(), 14.175, 5);
                            var eng = rt.Engine;
                            var piatto = new Engine.Dem.Profilo { D = new double[401], H = new double[401], Mare = new bool[401] };
                            for (int i = 0; i < 401; i++) piatto.D[i] = i * 10;
                            var op = new Engine.TerrenoReale.Opzioni { EpsTerra = 13, SigmaTerra = 0.005, Mare = false };
                            var et = Engine.TerrenoReale.Calcola((double el, double ph, out System.Numerics.Complex a, out System.Numerics.Complex b) => eng.FfldDiretto((90 - el) * Math.PI / 180, ph * Math.PI / 180, out a, out b),
                                eng.GainConstant, 14.175, 0, piatto, null, 10, op, null);
                            double dmax = 0;
                            for (int q = 4; q <= 320; q += 4) dmax = Math.Max(dmax, Math.Abs(et.Reale[q] - eng.Gain(90 - et.El[q], 0).GainTotal));
                            bool okt = dmax <= 0.1;
                            string r1 = string.Format(CI0, "Terreno reale su profilo piatto (dipolo 20 m a 10 m, suolo 13 / 5 mS/m, 1-80 gradi): scarto massimo dal NEC {0:0.000} dB (tolleranza 0,1 dB)", dmax);
                            sb.AppendLine("       " + (okt ? "OK   " : "ERR  ") + r1); if (!okt) errPar.Add(r1);
                            double latL, lonL; Engine.Dem.DaLocatore("JN70CN82WE", out latL, out lonL);
                            double[,] att = { { 302, 7093 }, { 327, 10250 }, { 41, 9871 }, { 82, 15990 }, { 234, 9836 }, { 278, 7938 } };
                            double fc1, fs1, fc5, fs5; Engine.TerrenoReale.FresnelCS(1, out fc1, out fs1); Engine.TerrenoReale.FresnelCS(5, out fc5, out fs5);
                            bool okf = Math.Abs(fc1 - 0.7798934) < 1e-6 && Math.Abs(fs1 - 0.4382591) < 1e-6 && Math.Abs(fc5 - 0.5636312) < 1e-6 && Math.Abs(fs5 - 0.4991914) < 1e-6;
                            string rf = string.Format(CI0, "Integrali di Fresnel: C(1) {0:0.0000000} S(1) {1:0.0000000} (Abramowitz-Stegun 0,7798934 / 0,4382591); C(5) {2:0.0000000} S(5) {3:0.0000000} (0,5636312 / 0,4991914)", fc1, fs1, fc5, fs5);
                            sb.AppendLine("       " + (okf ? "OK   " : "ERR  ") + rf); if (!okf) errPar.Add(rf);
                            bool okd = Math.Abs(latL - 40.5508) < 0.0001 && Math.Abs(lonL - 14.2411) < 0.0001;
                            var sbd = new System.Text.StringBuilder();
                            for (int i = 0; i < 6; i++)
                            {
                                var dd = Engine.Dem.DirezioniTraffico[i]; double az, km;
                                Engine.Dem.Rotta(latL, lonL, dd.Lat, dd.Lon, out az, out km);
                                if (Math.Abs(az - att[i, 0]) > 1 || Math.Abs(km - att[i, 1]) > 10) okd = false;
                                sbd.Append(string.Format(CI0, "{0} {1:0}°/{2:0} km; ", dd.Nome, az, km));
                            }
                            string r2 = "Direzioni di traffico da JN70CN82WE (" + latL.ToString("0.0000", CI0) + " N " + lonL.ToString("0.0000", CI0) + " E): " + sbd.ToString();
                            sb.AppendLine("       " + (okd ? "OK   " : "ERR  ") + r2); if (!okd) errPar.Add(r2);
                        }
                        catch (Exception ex) { errPar.Add("terreno reale: " + ex.Message); }
                    // 1.2.0: vento (bozzetto VHF 8): zona NTC dal locatore (Capri = 9, 31 m/s), cf0 subcritico, forza con il flusso trasversale
                    try
                    {
                        var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                        double laC, loC; Engine.Dem.DaLocatore("JN70CN82WE", out laC, out loC);
                        int zc = Engine.MeccanicaVento.ZonaNtc(laC, loC), zn = Engine.MeccanicaVento.ZonaNtc(45.46, 9.19), zp = Engine.MeccanicaVento.ZonaNtc(38.12, 13.36), zcag = Engine.MeccanicaVento.ZonaNtc(39.22, 9.12);
                        double vb = Engine.Meccanica.Vb(Engine.Meccanica.Zone[zc - 1], 62);
                        var pe = new Engine.MeccanicaVento.Parte { Area = 1, Cf = 1.2, Asse = 0 };
                        double f0 = Engine.MeccanicaVento.ForzaXY(pe, 1000, 0)[0], f45 = Engine.MeccanicaVento.ForzaXY(pe, 1000, 45)[0], f90 = Engine.MeccanicaVento.ForzaXY(pe, 1000, 90)[0];
                        bool okv = zc == 9 && Math.Abs(vb - 31) < 1e-9 && zn == 1 && zp == 4 && zcag == 5 && Math.Abs(Engine.MeccanicaVento.Cf0Cilindro(0.008, 31) - 1.2) < 1e-9 && Math.Abs(f0 - 1200) < 1e-6 && Math.Abs(f45 - 600) < 1e-6 && Math.Abs(f90) < 1e-6;
                        string rv = string.Format(CI0, "Vento: zona NTC da JN70CN82WE {0} (Capri, attesa 9), vb {1:0.0} m/s (31); Milano {2} (1), Palermo {3} (4), Cagliari {4} (5); elemento con vento a 0/45/90 gradi dal boom: {5:0}/{6:0}/{7:0} N (flusso trasversale: 1200/600/0)", zc, vb, zn, zp, zcag, f0, f45, f90);
                        sb.AppendLine("       " + (okv ? "OK   " : "ERR  ") + rv); if (!okv) errPar.Add(rv);
                    }
                    catch (Exception ex) { errPar.Add("vento: " + ex.Message); }
                    // 1.2.0: misure del VNA (bozzetto VHF 11): Touchstone RI / MA / DB e de-embedding del cavo (andata e ritorno)
                    try
                    {
                        var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                        var t1 = Engine.MisureVna.LeggiTouchstone(new[] { "! prova", "# MHz S RI R 50", "144.0 0 0", "145.0 0.2 0.1" }, "a.s1p");
                        var t2 = Engine.MisureVna.LeggiTouchstone(new[] { "# GHz S DB R 50", "0.1443 -20 0" }, "b.s1p");
                        var zl = new System.Numerics.Complex[] { new System.Numerics.Complex(37, 22) };
                        var zin = new[] { Engine.LineCalc.InputImpedance(zl[0], 50, 144.3, 0.85, 4.8, 18.4) };
                        var back = Engine.MisureVna.TogliCavo(new[] { 144.3 }, zin, 50, 0.85, f => 4.8, 18.4);
                        double g2 = (t2.Z[0] - 50).Magnitude / (t2.Z[0] + 50).Magnitude;
                        bool okn = Math.Abs(t1.F[0] - 144) < 1e-12 && (t1.Z[0] - 50).Magnitude < 1e-9 && Math.Abs(g2 - 0.1) < 1e-9 && Math.Abs(t2.F[0] - 144.3) < 1e-9 && (back[0] - zl[0]).Magnitude < 1e-9;
                        string rn = string.Format(CI0, "Misure VNA: Touchstone RI e DB letti (S11 0 -> 50 ohm, -20 dB -> |rho| {0:0.0000}); cavo tolto da 37+j22 ohm dietro 18,4 m di cavo: {1:0.000000}{2:+0.000000;-0.000000}j", g2, back[0].Real, back[0].Imaginary);
                        sb.AppendLine("       " + (okn ? "OK   " : "ERR  ") + rn); if (!okn) errPar.Add(rn);
                    }
                    catch (Exception ex) { errPar.Add("misure VNA: " + ex.Message); }
                    // 1.2.0: divisori (bozzetto VHF 5): 4 antenne da 50 ohm su 50 ohm -> Z0 25 ohm; tubo quadro 20 mm -> d 14,2 mm; lambda/4 a 144,2 MHz 519,7 mm
                    try
                    {
                        var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                        double zq = Engine.Divisori.Z0Richiesta(50, 50, 4), dq = Engine.Divisori.DQuadro(zq, 20, 1), lq = Engine.Divisori.QuartoMm(144.2, 1);
                        double zback = Engine.Divisori.Z0Quadro(20, dq, 1);
                        var zi = Engine.Divisori.Zin(12.5, 25, 144.2, 144.2);
                        bool okd2 = Math.Abs(zq - 25) < 1e-9 && Math.Abs(dq - 14.2) < 0.05 && Math.Abs(lq - 519.75) < 0.05 && Math.Abs(zback - 25) < 1e-9 && (zi - 50).Magnitude < 1e-6;
                        string rd2 = string.Format(CI0, "Divisore lambda/4 a 4 vie (50 ohm): Z0 {0:0.0} ohm, conduttore nel tubo quadro da 20 mm {1:0.00} mm (bozzetto 14,2), lambda/4 in aria {2:0.0} mm (519,7), ingresso a f0 {3:0.000} ohm", zq, dq, lq, zi.Real);
                        sb.AppendLine("       " + (okd2 ? "OK   " : "ERR  ") + rd2); if (!okd2) errPar.Add(rd2);
                    }
                    catch (Exception ex) { errPar.Add("divisori: " + ex.Message); }
                    // 1.2.0: cavo e balun (bozzetto VHF 10 rev. 2): il manicotto lambda/4 e' un'alta impedenza a f0; senza balun la calza porta corrente, con il manicotto molto meno
                    using (Prestazioni.Misura("Autotest di cavo e balun"))
                        try
                        {
                            var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                            var md = Engine.NecModel.Parse("CM dipolo 144\nCE\nGW 1 21 0 -0.49 0 0 0.49 0 0.003\nGE 0\nEX 0 1 11 0 1 0\nFR 0 1 0 0 144.3 0\nEN\n");
                            var gd = md.BuildGeometry(); var rd = Engine.Analysis.Run(md, gd, 144.3, 10);
                            var pc = new Engine.CavoBalun.Percorso { LungoBoom = 0.3, DiscesaPalo = 2, PaloPresente = false };
                            var e0 = Engine.CavoBalun.Valuta(md, gd, 144.3, 144.3, pc, "nessuno", rd.ThetaMax, rd.PhiMax, System.Threading.CancellationToken.None);
                            var e1 = Engine.CavoBalun.Valuta(md, gd, 144.3, 144.3, pc, "manicotto", rd.ThetaMax, rd.PhiMax, System.Threading.CancellationToken.None);
                            double zm = Engine.CavoBalun.Zcm("manicotto", 144.3, 144.3).Magnitude;
                            bool okb = zm > 1000 && e0.CorrPerc > 3 * e1.CorrPerc && e1.CorrPerc < 15;
                            string rb = string.Format(CI0, "Cavo e balun (dipolo 144 MHz, calza 0,3 + 2 m): |Z| del manicotto a f0 {0:0} ohm; corrente sulla calza senza balun {1:0.0} %, con il manicotto {2:0.0} %", zm, e0.CorrPerc, e1.CorrPerc);
                            sb.AppendLine("       " + (okb ? "OK   " : "ERR  ") + rb); if (!okb) errPar.Add(rb);
                        }
                        catch (Exception ex) { errPar.Add("cavo e balun: " + ex.Message); }
                    // 1.3.0 (IC8TEM 18/11/2026 00:41): antenne riceventi abilitate (dipolo basso, K9AY, 4-square RX, Hi-Z 2/3/8): il modello si apre,
                    // si calcola, il lobo va dove deve (alto per il dipolo, avanti per le altre) e l'RDF sta in un campo ragionevole
                    using (Prestazioni.Misura("Autotest delle antenne riceventi 1.3"))
                        try
                        {
                            var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                            var gr = new Engine.GroundSpec { Kind = Engine.GroundKind.Real, Epsr = 13, Sigma = 0.005 };
                            double f = 1.83, lam = 299.792458 / f;
                            var casi = new System.Collections.Generic.List<Tuple<string, string, double, double, bool>>
                            {
                                Tuple.Create("dipolo basso 77,8 m a 15 m", Engine.RxAntennas.DipoloBasso(new Engine.RxParams { F = f, L = 77.8, H = 15, Ground = gr }), 6.0, 10.0, false),
                                Tuple.Create("K9AY 7,6 / 9,2 / 1 m, 390 ohm", Engine.RdfRicezione.K9ay(new Engine.RxParams { F = f, Rterm = 390, Ground = gr }, true, 7.6, 4.6, 1.0), 5.5, 8.5, true),
                                Tuple.Create("4-square RX lato lambda/8", Engine.RdfRicezione.FourSquareRx(new Engine.RxParams { F = f, Ground = gr }, false, lam / 8, 7.6), 7.0, 14.0, true),
                                Tuple.Create("Hi-Z 2 elementi lambda/8", Engine.RdfRicezione.Cardioide2(new Engine.RxParams { F = f, Ground = gr }, false, lam / 8, 7.6), 6.0, 12.0, true),
                                Tuple.Create("Hi-Z 3 a triangolo lambda/6", Engine.RdfRicezione.Triangolo3(new Engine.RxParams { F = f, Ground = gr }, false, lam / 6, 7.6), 6.0, 12.0, true),
                                Tuple.Create("Hi-Z 8 in cerchio 0,37 lambda (fase 220 gradi)", Engine.RdfRicezione.Cerchio8(new Engine.RxParams { F = f, Ground = gr, Fase = 220 }, false, 0.37 * lam, 7.6), 6.0, 14.0, true),
                            };
                            foreach (var c in casi)
                            {
                                var mm = Engine.NecModel.Parse(c.Item2);
                                var rr = Engine.Analysis.Run(mm, mm.BuildGeometry(), f, 5.0);
                                var rd = Engine.NoiseGt.Rdf(rr.Grid, rr.ThetaMax, rr.PhiMax, rr.GmaxDbi, true);
                                double dphi = Math.Abs(((rr.PhiMax % 360) + 540) % 360 - 180);
                                bool lobo = c.Item5 ? dphi <= 45 : rr.ElevationOfMax >= 45;
                                bool okr = lobo && rd.Rdf >= c.Item3 && rd.Rdf <= c.Item4;
                                string rx = string.Format(CI0, "Ricevente {0}: RDF {1:0.00} dB (atteso {2:0.#}-{3:0.#}), massimo a {4:0} gradi di elevazione e {5:0} di azimut", c.Item1, rd.Rdf, c.Item3, c.Item4, rr.ElevationOfMax, rr.PhiMax);
                                sb.AppendLine("       " + (okr ? "OK   " : "ERR  ") + rx); if (!okr) errPar.Add(rx);
                            }
                        }
                        catch (Exception ex) { errPar.Add("antenne riceventi 1.3: " + ex.Message); }
                    // 1.2.0: tolleranze (bozzetto VHF 9): nomi degli elementi di una Yagi a 3 elementi; con tolleranze nulle ogni prova = nominale
                    using (Prestazioni.Misura("Autotest delle tolleranze"))
                        try
                        {
                            var CI0 = System.Globalization.CultureInfo.InvariantCulture;
                            var my3 = Engine.NecModel.Parse("CM yagi 3 el 144\nCE\nGW 1 11 0 -0.515 0 0 0.515 0 0.003\nGW 2 11 0.28 -0.49 0 0.28 0.49 0 0.003\nGW 3 11 0.62 -0.47 0 0.62 0.47 0 0.003\nGE 0\nEX 0 2 6 0 1 0\nFR 0 1 0 0 144.3 0\nEN\n");
                            var g3 = my3.BuildGeometry();
                            int asse3; var el3 = Engine.Tolleranze.Elementi(my3, g3, out asse3);
                            var r3 = Engine.Analysis.Run(my3, g3, 144.3, 5);
                            var o3 = new Engine.Tolleranze.Opzioni { TolL = 0, TolP = 0, TolD = 0, Prove = 3, F1 = 144.0, F2 = 144.6 };
                            var mc3 = Engine.Tolleranze.MonteCarlo(my3, g3, el3, asse3, o3, r3.ThetaMax, r3.PhiMax, null, System.Threading.CancellationToken.None);
                            double scarto = mc3.Prove.Max(p => Math.Abs(p.Fb - mc3.Nominale.Fb));
                            string nomi = string.Join(" ", el3.Select(e => e.Nome));
                            bool ok3 = nomi == "R DR D1" && asse3 == 0 && scarto < 1e-6;
                            string r3s = string.Format(CI0, "Tolleranze: elementi della Yagi a 3 elementi {0} (atteso R DR D1), asse del boom x; tolleranze nulle: scarto del F/B dal nominale {1:0.000000} dB", nomi, scarto);
                            sb.AppendLine("       " + (ok3 ? "OK   " : "ERR  ") + r3s); if (!ok3) errPar.Add(r3s);
                        }
                        catch (Exception ex) { errPar.Add("tolleranze: " + ex.Message); }
                    foreach (string e in errPar) sb.AppendLine("ERRORE: " + e);
                    bad += errPar.Count;
                    sb.AppendLine(bad == 0 ? "Autotest superato." : "Autotest: " + bad + " prove non superate.");
                    Console.Write(sb.ToString());
                    // con OutputType WinExe la console di Windows non mostra nulla: il resoconto va anche in data/autotest.txt
                    try { File.WriteAllText(Path.Combine(AppPaths.DataDir, "autotest.txt"), sb.ToString()); } catch { }
                    Registro.Scrivi("FINE", "autotest terminato: " + (bad == 0 ? "superato" : bad + " prove non superate"));
                    Registro.Chiudi();
                    return bad == 0 ? 0 : 1;
                }
            Registro.Scrivi("AVVIO", "interfaccia grafica, argomenti: " + string.Join(" ", args));
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Registro.Scrivi("ECCEZIONE", ex.ToString());
                try { File.WriteAllText(Path.Combine(AppPaths.DataDir, "errore_avvio.txt"), ex.ToString()); } catch { }
                throw;
            }
            Registro.Scrivi("FINE", "programma chiuso normalmente");
            Registro.Chiudi();
            return MainWindow.ProveAutomatiche ? (MainWindow.ProveFinite ? MainWindow.ProveFallite : 10000) : 0;
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
        }
            static double Max(double[] a) { double m = double.MinValue; foreach (double v in a) m = Math.Max(m, v); return m; }
    }
}
