// YA-NEC - autotest del calcolatore "Parabola primo fuoco".
// Da chiamare dall'autotest esistente (compila.bat): ParabolaSelfTest.Esegui(riga => log.WriteLine(riga)).
// Valori attesi: tabelle pubblicate (Stutzman / GMRT tab. 19.1) e calcolo di riferimento indipendente
// in Python + SciPy (verifica/riferimento_parabola.py, uscita in verifica/riferimento_output.txt).
#nullable disable
using System;
using System.Globalization;

namespace YaNec.Engine
{
    public static class ParabolaSelfTest
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static int ok, ko;

        static void Prova(Action<string> log, string nome, double valore, double atteso, double toll)
        {
            bool pass = !double.IsNaN(valore) && Math.Abs(valore - atteso) <= toll;
            if (pass) ok++; else ko++;
            log(string.Format(CI, "{0}  {1,-58} calcolato {2,12:0.0000}  atteso {3,12:0.0000}  +/- {4}", pass ? "OK  " : "ERR ", nome, valore, atteso, toll));
        }

        public static bool Esegui(Action<string> log)
        {
            ok = 0; ko = 0;
            log("=== Parabola primo fuoco: autotest ===");

            // 1. Bessel J0
            Prova(log, "J0(1)", Parabola.BesselJ0(1.0), 0.7651976866, 1e-6);
            Prova(log, "J0(5)", Parabola.BesselJ0(5.0), -0.1775967713, 1e-6);
            Prova(log, "J0(10)", Parabola.BesselJ0(10.0), -0.2459357645, 1e-6);

            // 1b. rete del disco (Otoshi, foglio W1GHZ 2014) contro la tabella di G3LTF a 5,7 GHz (RSGB 2016): rumore dal terreno
            //     a 290 K; passo da centro a centro 6, 8, 10 mm con filo da 1 e 2 mm (foro = passo - filo)
            {
                double lam57 = Parabola.C0 / 5760e6 * 1000.0;
                double[,] g3 = { { 6, 1, 18 }, { 6, 2, 4 }, { 8, 1, 51 }, { 8, 2, 15 }, { 10, 1, 108 } };
                for (int i = 0; i < g3.GetLength(0); i++)
                {
                    double tK = Parabola.TrasmissioneRete(g3[i, 0] - g3[i, 1], g3[i, 1], lam57) * 290.0;
                    Prova(log, string.Format(CI, "rete {0:0}x{0:0} mm filo {1:0} mm a 5760 MHz (G3LTF {2:0} K)", g3[i, 0], g3[i, 1], g3[i, 2]), tK, g3[i, 2], 3.0);
                }
            }

            // 2. metodo dei lobi contro le tabelle pubblicate
            double hp, sl, et;
            Parabola.ProvaApertura(x => 1.0, out hp, out sl, out et);
            Prova(log, "apertura uniforme: HPBW (lambda/D)", hp, 1.02, 0.01);
            Prova(log, "apertura uniforme: primo lobo (dB)", sl, -17.6, 0.15);
            double[] tap = { 10, 10, 18, 18 }; int[] nn = { 1, 2, 1, 2 };
            double[] hpA = { 1.14, 1.17, 1.20, 1.29 }, slA = { -22.3, -27.0, -24.1, -34.5 }, etA = { 0.917, 0.877, 0.833, 0.719 };
            for (int i = 0; i < 4; i++)
            {
                double C = Math.Pow(10, -tap[i] / 20.0); int n = nn[i];
                Parabola.ProvaApertura(x => C + (1 - C) * Math.Pow(1 - x * x, n), out hp, out sl, out et);
                string tag = string.Format(CI, "pedestal -{0:0} dB n={1}", tap[i], n);
                Prova(log, tag + ": HPBW (lambda/D)", hp, hpA[i], 0.012);
                Prova(log, tag + ": primo lobo (dB)", sl, slA[i], 0.2);
                Prova(log, tag + ": efficienza di taper", et, etA[i], 0.004);
            }

            // 3. caso di riferimento: D 3 m, f/D 0,42, taper 10 dB, 1296,1 MHz (valori Python/SciPy)
            // 0.4.21: i valori di riferimento sono con gli anelli scritti a mano (0,45 kg/m, 15 mm al vento); con gli arcarecci calcolati
            // (di partenza dalla 0.4.21) massa, spinta e contrappeso cambiano: quel caso ha la sua prova nella procedura "Novita' della 0.4.21"
            var p = new ParabolaInput { ArcarecciCalcolati = false };
            var r = Parabola.Calcola(p);
            if (r.Errore != null) { log("ERR  calcolo: " + r.Errore); ko++; }
            else
            {
                Prova(log, "F (m)", r.Fuoco_m, 1.2600, 1e-6);
                Prova(log, "profondita' (m)", r.Profondita_m, 0.4464, 1e-4);
                Prova(log, "angolo di bordo (gradi)", r.Psi0_deg, 61.5254, 1e-3);
                Prova(log, "superficie (m2)", r.AreaSuperficie_m2, 7.6619, 1e-3);
                Prova(log, "lunghezza centina (m)", r.LunghezzaCentina_m, 1.5844, 1e-3);
                Prova(log, "q del feed", r.Q, 1.1448, 1e-3);
                Prova(log, "spillover", r.EtaSpill, 0.9126, 1e-3);
                Prova(log, "taper", r.EtaTaper, 0.9033, 1e-3);
                // 1.0.14: il blocco comprende l'ombra dei puntoni sull'onda sferica (Baars 4.36c-f): 0,9644 -> 0,9359, guadagno -0,13 dB
                Prova(log, "blocco", r.EtaBlocco, 0.9359, 1.5e-3);
                Prova(log, "Ruze (3 mm)", r.EtaRuze, 0.9738, 1e-3);
                // rete con la formula di Otoshi/W1GHZ (prima 0,9966 e 30,246 dBi; a 10 GHz 40,194 dBi)
                Prova(log, "rete 6 mm / 0,8 mm (Otoshi, W1GHZ)", r.EtaRete, 0.9928, 1e-3);
                Prova(log, "guadagno (dBi)", r.Guadagno_dBi, 30.229, 0.05);
                Prova(log, "HPBW (gradi)", r.Hpbw_deg, 5.041, 0.02);
                Prova(log, "primo lobo (dB)", r.PrimoLobo_dB, -23.30, 0.2);
                Prova(log, "fuori fuoco per -0,25 dB (mm)", r.TolleranzaAssiale025_mm, 59.2, 1.0);
                Prova(log, "BDF", r.BDF, 0.8326, 1e-3);
                Prova(log, "lunghezza puntone (m)", r.PuntoneLunghezza_m, 1.4486, 1e-3);
                Prova(log, "massa puntoni (kg)", r.PuntoniMassa_kg, 1.0229, 2e-3);
                Prova(log, "K della rete (Idelchik)", r.ReteK, 0.3688, 1e-3);
                Prova(log, "Cd rete a disco", r.CdReteDisco, 0.3752, 1e-3);
                Prova(log, "spinta del vento (N)", r.ForzaVento_N, 2991.1, 3.0);
                Prova(log, "spinta col ghiaccio (N)", r.ForzaVentoGhiaccio_N, 6904.4, 5.0);
                Prova(log, "massa totale (kg)", r.MassaTotale_kg, 46.316, 0.02);
                Prova(log, "baricentro (m)", r.Baricentro_m, 0.2451, 1e-3);
                Prova(log, "contrappeso (kg)", r.Contrappeso_kg, 21.53, 0.03);
            }

            // 4. stesso disco a 10368,1 MHz (verifica EME)
            p.FreqMHz = 10368.1;
            r = Parabola.Calcola(p);
            if (r.Errore == null)
            {
                Prova(log, "10 GHz: HPBW (gradi)", r.Hpbw_deg, 0.6304, 0.005);
                Prova(log, "10 GHz: guadagno (dBi)", r.Guadagno_dBi, 38.396, 0.05);
                Prova(log, "10 GHz: perdita eco Luna (dB)", r.PerditaEco_dB, 1.888, 0.03);
                Prova(log, "10 GHz: errore ammesso per 0,5 dB (gradi)", r.Tolleranza05_deg, 0.1287, 0.002);
                bool avv = r.Avvisi.Exists(a => a.Livello == 2);
                if (avv) ok++; else ko++;
                log((avv ? "OK  " : "ERR ") + " 10 GHz: compare almeno un avviso critico");
            }

            // 5. feed tabellato (come arriva dal NEC o da un CSV): un cos^q scritto in tabella deve dare gli stessi numeri
            {
                double q = 1.1448;
                var sb = new System.Text.StringBuilder();
                for (int a = 0; a <= 180; a++)
                {
                    double c = Math.Cos(a * Math.PI / 180.0);
                    double dbv = a < 90 ? 20.0 * q * Math.Log10(c) : -80.0;
                    sb.AppendLine(string.Format(CultureInfo.GetCultureInfo("it-IT"), "{0};{1:0.0000}", a, dbv));
                }
                double[] th, db; string err;
                bool letto = Parabola.LeggiFeedCsv(sb.ToString(), out th, out db, out err);
                if (letto) ok++; else ko++;
                log((letto ? "OK  " : "ERR ") + " feed da CSV con la virgola decimale" + (err != null ? ": " + err : ""));
                var pt = new ParabolaInput { FeedTabThetaDeg = th, FeedTabDb = db };
                var rt = Parabola.Calcola(pt, false);
                if (rt.Errore == null)
                {
                    Prova(log, "feed tabellato: spillover", rt.EtaSpill, 0.9126, 0.003);
                    Prova(log, "feed tabellato: taper", rt.EtaTaper, 0.9033, 0.003);
                    Prova(log, "feed tabellato: taper al bordo (dB)", rt.TaperBordoTab_dB, 10.0, 0.1);
                    Prova(log, "feed tabellato: guadagno (dBi)", rt.Guadagno_dBi, 30.229, 0.06);
                    Prova(log, "feed tabellato: HPBW (gradi)", rt.Hpbw_deg, 5.041, 0.03);
                }
                else { ko++; log("ERR  feed tabellato: " + rt.Errore); }
            }

            // 6. lettore dei formati W1GHZ (FEEDPATT/PHASEPAT) e DF3GJ: attenuazione positiva, commenti, fase
            {
                string df3gj = "; this is a comment\n; example in dB attenuation\n 0 0\n 10 0.32\n 20 1 ; you can add a remark\n 30 3\n 60 15\n 90 40\n";
                Parabola.FeedLetto fl; string err;
                bool letto = Parabola.LeggiFeed(df3gj, Parabola.FormatoAuto, out fl, out err);
                bool okF = letto && fl.Theta.Length == 6 && fl.Formato == Parabola.FormatoW1ghz && fl.Attenuazione && fl.Fase == null;
                if (okF) ok++; else ko++;
                log((okF ? "OK  " : "ERR ") + " esempio DF3GJ: 6 righe (commento in coda compreso), attenuazione riconosciuta" + (err != null ? ": " + err : ""));
                if (letto)
                {
                    Prova(log, "esempio DF3GJ: livello a 20 gradi (dB)", fl.DbE[2], -1.0, 1e-9);
                    Prova(log, "esempio DF3GJ: livello a 90 gradi (dB)", fl.DbE[5], -40.0, 1e-9);
                }
                string w1 = "// DIPROD_H.DAT\n0 0 0\n10 0.4 -1.5\n20 1.6 -6.0\n30 3.7 -13.2\n";
                letto = Parabola.LeggiFeed(w1, Parabola.FormatoAuto, out fl, out err);
                bool okW = letto && fl.Formato == Parabola.FormatoW1ghz && fl.Fase != null && fl.Fase.Length == 4 && Math.Abs(fl.Fase[3] + 13.2) < 1e-9 && Math.Abs(fl.DbE[2] + 1.6) < 1e-9;
                if (okW) ok++; else ko++;
                log((okW ? "OK  " : "ERR ") + " file W1GHZ a 3 colonne: la terza e' la fase, non il piano H" + (err != null ? ": " + err : ""));
                string csv = "theta;dB_E;dB_H\n0;0;0\n10;-0,5;-0,3\n20;-2;-1,2\n";
                letto = Parabola.LeggiFeed(csv, Parabola.FormatoAuto, out fl, out err);
                bool okC = letto && fl.Formato == Parabola.FormatoCsv && !fl.Attenuazione && fl.Fase == null && Math.Abs(fl.DbH[2] + 1.2) < 1e-9;
                if (okC) ok++; else ko++;
                log((okC ? "OK  " : "ERR ") + " CSV di YA-NEC con intestazione: terza colonna = piano H");
            }

            // 7. fase e centro di fase (proposta 14/11/2026, prove T1-T4; valori attesi da verifica/riferimento_fase.py)
            {
                Func<double, double[]> cosq = qq =>
                {
                    var v = new double[181];
                    for (int a = 0; a <= 180; a++) v[a] = a < 90 ? 20.0 * qq * Math.Log10(Math.Cos(a * Math.PI / 180.0)) : -80.0;
                    return v;
                };
                var th = new double[181];
                for (int a = 0; a <= 180; a++) th[a] = a;
                double lamMm = Parabola.C0 / 1296.1e6 * 1000.0;
                double[] amp = cosq(1.1448);

                // T1: sorgente spostata di +0,10 lambda verso il disco -> fase 360*0,1*(cos(theta)-1) gradi
                var f1 = new double[181];
                for (int a = 0; a <= 180; a++) f1[a] = 360.0 * 0.1 * (Math.Cos(a * Math.PI / 180.0) - 1.0);
                var p1 = new ParabolaInput { FeedPianiThetaDeg = th, FeedPianoDbE = amp, FeedPianoDbH = amp, FeedPianoFaseE = f1, FeedPianoFaseH = f1 };
                var r1 = Parabola.Calcola(p1, false);
                Prova(log, "T1 centro di fase (mm, atteso 0,1 lambda)", r1.CentroFase_mm, 0.1 * lamMm, 0.05);
                Prova(log, "T1 centro di fase piano E (mm)", r1.CentroFaseE_mm, 0.1 * lamMm, 0.05);
                Prova(log, "T1 centro di fase piano H (mm)", r1.CentroFaseH_mm, 0.1 * lamMm, 0.05);
                Prova(log, "T1 stima di W1GHZ a -10 dB (mm)", r1.CentroFaseStima10_mm, 0.1 * lamMm, 0.2);
                Prova(log, "T1 efficienza di fase dopo la correzione", r1.EtaFase, 1.0, 1e-6);
                Prova(log, "T1 guadagno (dBi, come il cos^q)", r1.Guadagno_dBi, 30.230, 0.06);
                bool fu = r1.FaseUsata; if (fu) ok++; else ko++;
                log((fu ? "OK  " : "ERR ") + " T1 la fase risulta usata");

                // T2: fase -k dz cos(theta) con dz = 0,25 lambda e centro manuale 0 = stessa perdita del feed spostato in asse
                var f2 = new double[181];
                for (int a = 0; a <= 180; a++) f2[a] = -360.0 * 0.25 * Math.Cos(a * Math.PI / 180.0);
                var p2 = new ParabolaInput { FeedPianiThetaDeg = th, FeedPianoDbE = amp, FeedPianoDbH = amp, FeedPianoFaseE = f2, FeedPianoFaseH = f2, CentroFaseAuto = false, CentroFaseManuale_mm = 0 };
                var r2 = Parabola.Calcola(p2, false);
                var r2b = Parabola.Calcola(new ParabolaInput { FeedSpostAssiale_mm = 0.25 * lamMm }, false);
                Prova(log, "T2 perdita di fase (dB)", -10.0 * Math.Log10(r2.EtaFase), 0.2384, 0.005);
                Prova(log, "T2 stessa perdita del feed spostato in asse (dB)", -10.0 * Math.Log10(r2.EtaFase), r2b.PerditaAssiale_dB, 0.005);
                Prova(log, "T2 centro di fase automatico (mm, atteso -0,25 lambda)", r2.CentroFaseAuto_mm, -0.25 * lamMm, 0.05);

                // T3: piani uguali senza fase = risultati di oggi
                var r3 = Parabola.Calcola(new ParabolaInput { FeedPianiThetaDeg = th, FeedPianoDbE = amp, FeedPianoDbH = amp }, false);
                Prova(log, "T3 efficienza di polarizzazione", r3.EtaPol, 1.0, 1e-9);
                Prova(log, "T3 efficienza di fase", r3.EtaFase, 1.0, 1e-9);
                Prova(log, "T3 taper", r3.EtaTaper, 0.9033, 0.001);
                Prova(log, "T3 spillover", r3.EtaSpill, 0.9128, 0.001);

                // T4: piani diversi, cos^2 nel piano E e cos^0,6 nel piano H, senza fase
                var r4 = Parabola.Calcola(new ParabolaInput { FeedPianiThetaDeg = th, FeedPianoDbE = cosq(2.0), FeedPianoDbH = cosq(0.6) }, false);
                Prova(log, "T4 efficienza di polarizzazione", r4.EtaPol, 0.9611, 0.001);
                Prova(log, "T4 spillover", r4.EtaSpill, 0.8568, 0.001);
                Prova(log, "T4 taper (media in campo dei piani)", r4.EtaTaper, 0.9031, 0.001);
            }

            // 8. pagina "Feed": analisi del feed sul caso T1 (valori attesi da verifica/riferimento_analisi_feed.py)
            {
                var th = new double[181]; var amp = new double[181]; var f1 = new double[181];
                for (int a = 0; a <= 180; a++)
                {
                    th[a] = a;
                    amp[a] = a < 90 ? 20.0 * 1.1448 * Math.Log10(Math.Cos(a * Math.PI / 180.0)) : -80.0;
                    f1[a] = 360.0 * 0.1 * (Math.Cos(a * Math.PI / 180.0) - 1.0);
                }
                var rf = Parabola.Calcola(new ParabolaInput { FeedPianiThetaDeg = th, FeedPianoDbE = amp, FeedPianoDbH = amp, FeedPianoFaseE = f1, FeedPianoFaseH = f1 });
                var fa = rf.Feed;
                bool cOk = fa != null && fa.FaseE != null && fa.Fds.Length == 31 && fa.Dz_mm.Length == 81;
                if (cOk) ok++; else ko++;
                log((cOk ? "OK  " : "ERR ") + " Feed: analisi presente con fase, 31 punti in f/D e 81 in posizione");
                if (fa != null)
                {
                    Prova(log, "Feed: efficienza d'apertura del feed a f/D 0,42", fa.TotAttuale, 0.8242, 0.002);
                    Prova(log, "Feed: f/D del massimo", fa.FdsMax, 0.400, 0.001);
                    Prova(log, "Feed: efficienza massima", fa.TotMax, 0.8263, 0.002);
                    Prova(log, "Feed: -0,25 dB, limite inferiore (mm)", fa.TolMin_mm, -59.17, 0.5);
                    Prova(log, "Feed: -0,25 dB, limite superiore (mm)", fa.TolMax_mm, 59.17, 0.5);
                    Prova(log, "Feed: perdita con l'origine del modello nel fuoco (dB)", fa.PerditaOrigine_dB, 0.038, 0.003);
                    Prova(log, "Feed: angolo a -3 dB piano E (gradi)", fa.Theta3E, 42.30, 0.05);
                    Prova(log, "Feed: angolo a -3 dB piano H (gradi)", fa.Theta3H, 42.30, 0.05);
                    Prova(log, "Feed: fase corretta a 60 gradi (gradi, ~0)", fa.FaseE[60], 0.0, 0.5);
                }
            }

            // 9. parabola offset (valori attesi da verifica/riferimento_offset.py)
            {
                // 9a. limite simmetrico (bordo inferiore a -D/2): deve ridare il primo fuoco da 3 m, f/D 0,42, cos^1,1448
                double ts = 2.0 * Math.Atan(1.5 / (2.0 * 1.26));
                double taperFeed = -20.0 * 1.1448 * Math.Log10(Math.Cos(ts));
                var s = Parabola.CalcolaOffset(new ParabolaInput { Diametro_m = 3.0, FsuD = 0.42, FreqMHz = 1296.1, OffsetBordoInferiore_m = -1.5, TaperBordo_dB = taperFeed, FeedDiametro_m = 0.0, OffsetMiraOttima = false }, false);
                Prova(log, "Offset simmetrico: q del feed", s.Q, 1.1448, 1e-3);
                Prova(log, "Offset simmetrico: theta* (gradi)", s.ThetaStar_deg, 61.5254, 1e-3);
                Prova(log, "Offset simmetrico: spillover", s.EtaSpill, 0.9126, 2e-3);
                Prova(log, "Offset simmetrico: taper", s.EtaTaper, 0.9033, 2e-3);
                Prova(log, "Offset simmetrico: efficienza d'apertura", s.EtaApertura, 0.8244, 2e-3);
                Prova(log, "Offset simmetrico: bordo (dB)", s.IllumBordoSup_dB, -10.0, 0.01);
                Prova(log, "Offset simmetrico: profondita' (m)", s.ProfonditaMax_m, 0.4464, 1e-3);
                Prova(log, "Offset simmetrico: HPBW orizzontale (gradi)", s.HpbwH_deg, 5.062, 0.02);
                Prova(log, "Offset simmetrico: HPBW verticale (gradi)", s.HpbwV_deg, 5.062, 0.02);
                Prova(log, "Offset simmetrico: primo lobo (dB)", s.PrimoLoboH_dB, -24.37, 0.3);

                // 9b. OK1DFC 8 m (Trenton 2024): D 7,825 m, f/D 0,457, parabola madre 12,6 m
                var g = Parabola.CalcolaOffset(new ParabolaInput { Diametro_m = 7.825, FsuD = 0.457, FreqMHz = 1296.1, OffsetBordoInferiore_m = 6.3 - 7.825 }, false);
                Prova(log, "OK1DFC: inclinazione del bordo (gradi)", g.Inclinazione_deg, 18.4601, 1e-3);
                Prova(log, "OK1DFC: asse maggiore (m)", g.AsseMaggiore_m, 8.2495, 1e-3);
                Prova(log, "OK1DFC: profondita' dal piano del bordo (m)", g.ProfonditaMax_m, 1.0151, 2e-3);
                Prova(log, "OK1DFC: f/D equivalente per il feed", g.FsuDEquivalente, 0.4969, 1e-3);
                Prova(log, "OK1DFC: parabola madre (m)", g.DiametroMadre_m, 12.6, 1e-6);

                // 9c. offset vero da 1 m a 10 GHz: F 0,6 m, bordo inferiore 0,05 m sopra l'asse, feed a -10 dB a theta*
                var o = Parabola.CalcolaOffset(new ParabolaInput { Diametro_m = 1.0, FsuD = 0.6, FreqMHz = 10368.1, OffsetBordoInferiore_m = 0.05, TaperBordo_dB = 10.0, FeedDiametro_m = 0.0, OffsetMiraOttima = false }, false);
                Prova(log, "Offset 1 m: q del feed", o.Q, 4.6179, 1e-3);
                Prova(log, "Offset 1 m: inclinazione (gradi)", o.Inclinazione_deg, 24.6236, 1e-3);
                Prova(log, "Offset 1 m: efficienza d'apertura (mira sulla bisettrice)", o.EtaApertura, 0.7962, 2e-3);
                Prova(log, "Offset 1 m: bordo inferiore (dB)", o.IllumBordoInf_dB, -8.16, 0.02);
                Prova(log, "Offset 1 m: bordo superiore (dB)", o.IllumBordoSup_dB, -13.08, 0.02);
                Prova(log, "Offset 1 m: HPBW orizzontale (gradi)", o.HpbwH_deg, 1.936, 0.01);
                Prova(log, "Offset 1 m: HPBW verticale (gradi)", o.HpbwV_deg, 1.930, 0.01);
                Prova(log, "Offset 1 m: mira ottima (gradi)", o.MiraOttima_deg, 4.0, 1e-6);
                Prova(log, "Offset 1 m: squint in circolare (gradi)", o.Squint_deg, 0.1515, 1e-3);
                var om = Parabola.CalcolaOffset(new ParabolaInput { Diametro_m = 1.0, FsuD = 0.6, FreqMHz = 10368.1, OffsetBordoInferiore_m = 0.05, TaperBordo_dB = 10.0, FeedDiametro_m = 0.0, OffsetMiraOttima = true }, false);
                Prova(log, "Offset 1 m: efficienza con la mira ottima", om.EtaApertura, 0.8077, 2e-3);
            }

            // 10. 1.0.14: casi di letteratura (Balanis cap. 15, Baars 2007 cap. 4; valori attesi da verifica/riferimento_1014.py)
            {
                // 10a. feed cos^n (potenza): spillover x taper del motore contro Balanis 15-55, integrale indipendente
                double[] fds = { 0.3, 0.42, 0.6 };
                int[] ns = { 2, 4, 6, 8 };
                double[,] attesi = { { 0.7383, 0.5277, 0.3949, 0.3124 }, { 0.8192, 0.7815, 0.6761, 0.5738 }, { 0.6371, 0.7793, 0.8167, 0.8011 } };
                var thT = new double[181];
                for (int a = 0; a <= 180; a++) thT[a] = a;
                for (int i = 0; i < fds.Length; i++)
                    for (int j = 0; j < ns.Length; j++)
                    {
                        int n = ns[j]; double fd = fds[i];
                        var dbT = new double[181];
                        for (int a = 0; a <= 180; a++) dbT[a] = a < 90 ? 10.0 * n * Math.Log10(Math.Cos(a * Math.PI / 180.0)) : -80.0;
                        var rc = Parabola.Calcola(new ParabolaInput { FsuD = fd, FeedTabThetaDeg = thT, FeedTabDb = dbT }, false);
                        double th0 = 2.0 * Math.Atan(1.0 / (4.0 * fd));
                        double bal = Parabola.EtaAperturaBalanis(t => t < Math.PI / 2 ? Math.Pow(Math.Cos(t), n) : 0.0, th0);
                        string tag = string.Format(CI, "cos^{0} f/D {1:0.00}", n, fd);
                        Prova(log, tag + ": Balanis 15-55", bal, attesi[i, j], 2e-3);
                        if (rc.Errore == null) Prova(log, tag + ": motore (spill x taper)", rc.EtaSpill * rc.EtaTaper, attesi[i, j], 5e-3);
                        else { ko++; log("ERR  " + tag + ": " + rc.Errore); }
                    }
                // forma chiusa di Balanis per n = 2 e massimo delle curve (circa 0,82-0,83)
                {
                    double th0 = 2.0 * Math.Atan(1.0 / (4.0 * 0.42)), sh = Math.Sin(th0 / 2), ct = 1.0 / Math.Tan(th0 / 2);
                    double chiusa = 24.0 * Math.Pow(sh * sh + Math.Log(Math.Cos(th0 / 2)), 2) * ct * ct;
                    Prova(log, "cos^2 f/D 0,42: forma chiusa di Balanis", chiusa, 0.8192, 2e-3);
                    double mx = 0;
                    for (double d = 20; d <= 100; d += 0.5) mx = Math.Max(mx, Parabola.EtaAperturaBalanis(t => t < Math.PI / 2 ? Math.Pow(Math.Cos(t), 2) : 0.0, d * Math.PI / 180.0));
                    Prova(log, "cos^2: massimo della curva di Balanis", mx, 0.8290, 2e-3);
                }
                // 10b. distribuzione quadratica su piedistallo a -12 dB (Baars 4.8 e 4.13)
                double C = Math.Pow(10, -12.0 / 20.0);
                Parabola.ProvaApertura(x => C + (1 - C) * (1 - x * x), out hp, out sl, out et);
                Prova(log, "quadratica -12 dB: efficienza (Baars 0,893)", et, 0.8933, 3e-3);
                Prova(log, "quadratica -12 dB: formula di Baars 4.8", Parabola.EtaQuadraticaBaars(C), 0.8933, 1e-4);
                Prova(log, "quadratica -12 dB: HPBW (Baars 66,3 gradi lambda/D)", hp * 180.0 / Math.PI, 66.3, 0.7);
                // 10c. gaussiana troncata a -12 dB (Baars 4.6)
                double ag = 12.0 / 20.0 * Math.Log(10.0);
                Parabola.ProvaApertura(x => Math.Exp(-ag * x * x), out hp, out sl, out et);
                Prova(log, "gaussiana -12 dB: formula di Baars 4.6", Parabola.EtaGaussianaBaars(12.0), 0.8664, 1e-4);
                Prova(log, "gaussiana -12 dB: integrale numerico", et, 0.8664, 3e-3);
                // 10d. free-space taper a f/D 0,3 (Baars 4.3), fuori fuoco beta = 1 (Baars 4.22), Ruze a lambda/20
                var rf = Parabola.Calcola(new ParabolaInput { FsuD = 0.3 }, false);
                Prova(log, "free-space taper f/D 0,3 (Baars 4,58 dB)", rf.TfBaars_dB, 4.58, 0.01);
                Prova(log, "fuori fuoco beta = 1 (Baars 0,919)", Parabola.DefocusUniforme(1.0), 0.9194, 1e-4);
                Prova(log, "Ruze sigma = lambda/20 (dB)", 10.0 * Math.Log10(Math.Exp(-Math.Pow(4.0 * Math.PI / 20.0, 2))), -1.7145, 1e-3);
                // 10e. PA0PLY (misure, 10 GHz, 3 m, f/D 0,3): solo riferimento, il diagramma della choke non e' analitico e va fatto col NEC
                log("INFO  PA0PLY 10 GHz, 3 m, f/D 0,3: guida aperta 13,5 dB contro choke 15,2 dB (sole/rumore); choke avvitata -0,6 dB; cappuccio dell'LNB -0,5 dB.");
            }

            log(string.Format(CI, "=== Parabola: {0} prove superate, {1} fallite ===", ok, ko));
            return ko == 0;
        }
    }
}
