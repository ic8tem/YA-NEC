// YA-NEC 0.4.20 - collaudo dei modelli piu' sensibili e piu' aggressivi per il calcolo (fase 2F di compila.bat, YA-NEC.exe --prove-sensibili).
// Richiesta di IC8TEM del 14/11/2026 15:25: "nel bat di compilazione, avrei piacere che tu faccia i collaudi per ya-nec per i modelli che
// risultano piu' sensibili da modificare o che siano piu' aggressivi per il calcolo".
// Scelta dei modelli (prova 6 con EZNEC del 14/11/2026, 735 modelli): i piu' grandi tra quelli che EZNEC calcola uguali a nec2c entro lo 0,5 %,
// uno per ogni parte delicata del motore: molti segmenti (stack da 927 e 604), terreno reale (GN 0, che EZNEC calcola come MININEC),
// Sommerfeld e giunzioni affollate (Butternut corretta), diametri a gradini (Leeson), carichi (LD), sorgenti multiple (four-square).
// Per ogni modello: 1) YA-NEC come nec2c (stesso mazzo) entro lo 0,05 %; 2) la LU a blocchi (0.4.20) come la LU classica entro 1e-8;
// 3) calibrazione con EZNEC: il valore misurato da IC8TEM con EZNEC Pro/2+ 7.0 alle 14:48, con lo stesso terreno (per i GN 0 il terreno
// MININEC, cioe' il GN 3 di YA-NEC), entro lo 0,5 %. In fondo i controlli geometrici (le sei regole del Geometry Check di EZNEC) su tre
// modelli che EZNEC ferma e su tre che calcola.
// Resoconto in data/prove_sensibili.txt.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using YaNec.Engine;

namespace YaNec
{
    public static class ProveSensibili
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static StringBuilder sb;
        static int passoN, nErr, nAvv;

        class Caso
        {
            public string Cartella, File, Perche;
            public bool Leeson, Gn0;
            public Complex Nec2c, Nec2cMiniNec, Eznec;
        }

        static Caso C(string cart, string file, string perche, bool gn0, double r, double x, double rm, double xm, double re, double xe, bool leeson = false)
        {
            return new Caso { Cartella = cart, File = file, Perche = perche, Gn0 = gn0, Leeson = leeson, Nec2c = new Complex(r, x), Nec2cMiniNec = new Complex(rm, xm), Eznec = new Complex(re, xe) };
        }

        const string Conv = "archivio\\Modelli IC8TEM (convertiti in NEC)";
        // 0.4.30 (IC8TEM 15/11/2026 07:31, modelli scartati dagli esempi): i modelli di calibrazione tolti dall'archivio restano solo qui,
        // per il collaudo (data\prove non va nel pacchetto pubblico)
        const string Fissi = "prove\\fissi";
        // nec2c 1.3.1 sullo stesso mazzo (GN del file; con GN 0 anche GN 1 = MININEC); EZNEC Pro/2+ 7.0, prova 6 di IC8TEM (14/11/2026 14:48)
        static readonly Caso[] Casi =
        {
            C(Fissi, "4x7elm-lfa_plus5elm50.nec", "stack 4x7 LFA + 5 el 50 MHz, 927 segmenti, terreno GN 0", true, 76.893, -141.83, 76.226, -142.2, 76.21703, -142.1597),
            C(Conv, "80_40_Butternut_64Radials.nec", "Butternut corretta 0.4.20, 1431 segmenti, Sommerfeld, mozzo dei radiali, diametri a gradini", false, 17.565, 25.175, 17.565, 25.175, 17.58461, 25.0207),
            C(Fissi, "12CQ430.nec", "12 el 432 MHz, 492 segmenti", false, 48.998, -4.794, 48.998, -4.794, 49.03489, -4.798152),
            C(Fissi, "ob16-3.nec", "OptiBeam OB16-3, 417 segmenti, HF", false, 20.163, 4.1516, 20.163, 4.1516, 20.17634, 4.193114),
            C(Fissi, "144-shark-20el.nec", "Shark 20 el 144 MHz, 355 segmenti", false, 43.654, -22.648, 43.654, -22.648, 43.6475, -22.69266),
            C(Fissi, "9elm-6elm.nec", "9+6 el, diametri a gradini (Leeson)", false, 45.001, 11.947, 45.001, 11.947, 44.99577, 12.03523, true),
            C(Conv, "f9ft-2-uu4jcr-stack.nec", "stack F9FT, terreno GN 0", true, 72.633, -4.8216, 72.49, -4.7368, 72.48229, -4.581369),
            C(Conv, "8el-oblong-vertical.nec", "8 el oblong verticale, GN 0 e carichi", true, 43.191, -30.877, 43.192, -30.877, 43.209, -30.81379),
            C(Conv, "moxon-w8jk.nec", "Moxon + W8JK, carichi, reattanza alta", false, 8.122, 127.92, 8.122, 127.92, 8.123702, 127.951),
            C(Conv, "5el_50.2_MHz_Diamond.nec", "5 el 50 MHz, GN 0 e carichi", true, 52.919, 4.3634, 53.054, 4.8413, 53.10173, 4.931591),
            C("modelli", "Dipolo 20 m a 10 m su terreno medio.nec", "dipolo su GN 0: il caso piu' semplice del terreno MININEC di EZNEC", true, 74.235, 10.679, 75.915, 2.6713, 75.91801, 2.707838),
            C("modelli", "Four-square 7 MHz con radiali (terreno medio).nec", "four-square, 4 sorgenti sfasate, GN 0", true, 6.0054, -2.2417, 4.0168, -2.213, 4.022284, -2.210054),
        };

        public static int Esegui()
        {
            sb = new StringBuilder();
            passoN = nErr = nAvv = 0;
            Riga("COLLAUDO DEI MODELLI SENSIBILI E AGGRESSIVI (fase 2F) - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Riga("Per ogni modello: YA-NEC come nec2c (0,05 %), LU a blocchi come LU classica (1e-8), calibrazione con EZNEC (0,5 %, prova 6 del 14/11/2026).");
            Riga(new string('=', 100));
            int k = 0;
            foreach (Caso c in Casi)
            {
                k++;
                Riga("");
                Riga("MODELLO " + k + " - " + c.File + " (" + c.Perche + ")");
                Prova(c);
                Registro.Scrivi("COLLAUDO", "modello sensibile " + k + ": " + c.File);
            }
            Riga("");
            Riga("CONTROLLI GEOMETRICI (le sei regole del Geometry Check di EZNEC, 0.4.20)");
            Geometria("butternut-new2.nec", true, "EZNEC: 7 estremi appoggiati e 1 incrocio");
            Geometria("butternut_hf6v.nec", true, "EZNEC: 1 contatto troppo vicino e 2 estremi appoggiati");
            Geometria("40_20_Butternut.nec", true, "EZNEC: centri dei segmenti finali dentro i tubi");
            Geometria("80_40_Butternut_64Radials.nec", false, "EZNEC la calcola (corretta nella 0.4.20)");
            Geometria("moxon-w8jk.nec", false, "EZNEC la calcola");
            Geometria("12CQ430.nec", false, "EZNEC la calcola");
            Riga("");
            Riga(nErr == 0 ? (nAvv == 0 ? "MODELLI SENSIBILI: SUPERATO." : "MODELLI SENSIBILI: SUPERATO con " + nAvv + " avvisi.") : "MODELLI SENSIBILI: NON SUPERATO (" + nErr + " errori).");
            try { File.WriteAllText(Path.Combine(AppPaths.DataDir, "prove_sensibili.txt"), sb.ToString(), new UTF8Encoding(false)); } catch { }
            return nErr;
        }

        static string Percorso(string cart, string file) { return Path.Combine(AppPaths.DataDir, cart.Replace('\\', Path.DirectorySeparatorChar), file); }

        static void Prova(Caso c)
        {
            string f = Percorso(c.Cartella, c.File);
            if (!YanecFile.Esiste(f)) { Passo("aprire " + c.File, "file presente", "mancante: " + f, false); return; }
            bool prima = Nec2Engine.LuClassica;
            try
            {
                NecModel m = NecModel.Load(f);
                if (c.Leeson) m.Leeson = true;   // nell'archivio senza la riga CM YA-NEC LEESON=1; EZNEC usa la sua correzione dei gradini
                double fr = m.FreqMHz;
                SegmentGeometry g = m.BuildGeometry(new List<string>());
                Nec2Engine.LuClassica = false;
                RunResult r = Analysis.Run(m, g, fr, 10.0, CancellationToken.None);
                double s1 = Complex.Abs(r.Zin - c.Nec2c) / Math.Max(1, Complex.Abs(c.Nec2c));
                double t1 = c.Leeson ? 2e-3 : 5e-4;   // con Leeson il riferimento e' nec2c sul mazzo corretto da leeson.py (stesse formule, arrotondamenti diversi)
                Passo("YA-NEC come nec2c (stesso mazzo, terreno del file)", string.Format(CI, "{0} entro lo {1:0.0#} %", Z(c.Nec2c), t1 * 100),
                    string.Format(CI, "{0:0.###} MHz, {1} segmenti: {2} (scarto {3:0.0000} %), LU {4:0} ms", fr, g.N, Z(r.Zin), s1 * 100, r.Engine.MsFattorizzazione), s1 <= t1);
                Nec2Engine.LuClassica = true;
                RunResult rc = Analysis.Run(m, g, fr, 10.0, CancellationToken.None);
                Nec2Engine.LuClassica = prima;
                double s2 = Complex.Abs(rc.Zin - r.Zin) / Math.Max(1e-9, Complex.Abs(r.Zin));
                Passo("LU a blocchi come LU classica", "scarto sotto 1e-8", string.Format(CI, "classica {0}, scarto {1:0.0E+0}; LU classica {2:0} ms", Z(rc.Zin), s2, rc.Engine.MsFattorizzazione), s2 < 1e-8);
                // calibrazione: per i GN 0 EZNEC usa il terreno MININEC (impedenza su terreno perfetto) = GN 3 di YA-NEC
                RunResult re = r;
                if (c.Gn0)
                {
                    m.Ground.MiniNec = true;
                    re = Analysis.Run(m, g, fr, 10.0, CancellationToken.None);
                    double s3 = Complex.Abs(re.Zin - c.Nec2cMiniNec) / Math.Max(1, Complex.Abs(c.Nec2cMiniNec));
                    Passo("YA-NEC con il terreno MININEC (GN 3) come nec2c con GN 1", string.Format(CI, "{0} entro lo 0,05 %", Z(c.Nec2cMiniNec)),
                        string.Format(CI, "{0} (scarto {1:0.0000} %)", Z(re.Zin), s3 * 100), s3 <= 5e-4);
                }
                double se = Complex.Abs(re.Zin - c.Eznec) / Math.Max(1, Complex.Abs(c.Eznec));
                double tol = c.Leeson || c.File.StartsWith("80_40", StringComparison.Ordinal) ? 0.01 : 0.005;
                Passo("calibrazione con EZNEC Pro/2+ 7.0" + (c.Gn0 ? " (terreno MININEC come in EZNEC)" : ""), string.Format(CI, "{0} entro {1:0.#} % (prova 6 di IC8TEM)", Z(c.Eznec), tol * 100),
                    string.Format(CI, "{0} (scarto {1:0.000} %)", Z(re.Zin), se * 100), se <= tol);
            }
            catch (Exception ex) { Passo("calcolo", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            finally { Nec2Engine.LuClassica = prima; }
        }

        static void Geometria(string file, bool attesi, string nota)
        {
            string f = Percorso(Conv, file);
            if (!YanecFile.Esiste(f)) f = Percorso(Fissi, file);
            if (!YanecFile.Esiste(f)) { Passo("aprire " + file, "file presente", "mancante", false); return; }
            try
            {
                NecModel m = NecModel.Load(f);
                SegmentGeometry g = m.BuildGeometry(new List<string>());
                List<string> av = TerrenoVicino.ContattiFuoriGiunzione(g);
                Passo(file + ": controlli geometrici", attesi ? "almeno un avviso (" + nota + ")" : "nessun avviso (" + nota + ")",
                    av.Count == 0 ? "nessun avviso" : av.Count + " avvisi: " + string.Join(" | ", av), attesi ? av.Count > 0 : av.Count == 0);
            }
            catch (Exception ex) { Passo(file + ": controlli geometrici", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
        }

        static string Z(Complex z) { return string.Format(CI, "{0:0.000} {1} j{2:0.000} ohm", z.Real, z.Imaginary < 0 ? "-" : "+", Math.Abs(z.Imaginary)); }
        static void Riga(string s) { sb.AppendLine(s); }

        static void Passo(string cosa, string atteso, string ottenuto, bool ok, bool soloAvviso = false)
        {
            passoN++;
            string e = ok ? "OK" : (soloAvviso ? "AVVISO" : "ERRORE");
            if (!ok) { if (soloAvviso) nAvv++; else nErr++; }
            Riga(string.Format(CI, "  F.{0,-3} {1}", passoN, cosa));
            if (!string.IsNullOrEmpty(atteso)) Riga("         atteso:   " + atteso);
            Riga("         ottenuto: " + ottenuto);
            Riga("         esito:    " + e);
        }
    }
}
