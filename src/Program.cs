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
            Librerie.Prepara();
            List<string> la = new List<string>(args);
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
                if (a == "--prove-interfaccia") MainWindow.ProveAutomatiche = true;   // le prove a mano eseguite dal programma stesso
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
