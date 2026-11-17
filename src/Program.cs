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
    }
}
