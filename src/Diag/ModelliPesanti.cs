// YA-NEC 0.4.18 (0.4.20: confronto LU a blocchi / LU classica) - collaudo dei 3 modelli piu' pesanti (fase 2E di compila.bat, YA-NEC.exe --prove-pesanti), senza finestra.
// Richiesta di IC8TEM del 14/11/2026 09:17: "nella 0.4.18 puoi inserire anche i collaudi dei modelli piu' grandi? vorrei che tu
// controllassi solo i 3 modelli piu' pesanti che hai: mi serve capire se sia possibile razionalizzare il codice". Scelta del 09:20:
// Butternut con 64 radiali (vecchia conversione, 7877 segmenti, tenuta solo per i tempi: il modello in archivio e' stato corretto),
// Bowtie 91XG di 4nec2 (5327 segmenti), 4x2SA13 di 4nec2 (4288 segmenti, simmetria GX). Patch.nec (39020 segmenti) e' escluso:
// la sola matrice chiederebbe circa 24 GB.
// Per ogni modello: impedenza confrontata con nec2c (stesso mazzo), tempi delle fasi del motore (tabelle di Sommerfeld, riempimento
// della matrice, fattorizzazione LU, soluzione, diagramma 3D a 5 gradi), memoria, resa della fattorizzazione in GFLOPS.
// In fondo una prova di riferimento (prodotto di matrici complesse a blocchi con le istruzioni vettoriali del processore) dice quanto
// potrebbe rendere una fattorizzazione riscritta a blocchi: e' il dato per decidere se conviene rifare quella parte del motore.
// Resoconto in data/prove_pesanti.txt, con lo stesso formato dei passi del collaudo (atteso, ottenuto, esito).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using YaNec.Engine;

namespace YaNec
{
    public static class ModelliPesanti
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static StringBuilder sb;
        static int passoN, nErr, nAvv;

        class Caso
        {
            public string Nome, File, Fonte;
            public Complex Atteso;
        }

        class Misura
        {
            public string Nome;
            public int N, Np, Nop;
            public double MsCarica, MsSom, MsRiemp, MsLu, MsSol, MsDiag, MsTot, Gflops, MbMatrice;
        }

        // impedenze di nec2c sullo stesso mazzo (nec2c 1.3.1, calcolate per la 0.4.18; 2SA anche in data/prove/4nec2_attesi.csv)
        static readonly Caso[] Casi =
        {
            new Caso { Nome = "Butternut 64 radiali, vecchia conversione (7877 segmenti, solo per i tempi)", File = "Butternut_7877_vecchio_solo_tempi.nec",
                       Atteso = new Complex(-0.6076, 20.222), Fonte = "nec2c GN 0 (identico); risultato non fisico (radiali a 3 cm con GN 0), tenuto solo come prova di velocita'" },
            new Caso { Nome = "Bowtie Antennas Direct 91XG (4nec2, 5327 segmenti)", File = "Bowtie_91XG_4nec2.nec",
                       Atteso = new Complex(409.36, 296.94), Fonte = "nec2c, 470 MHz" },
            new Caso { Nome = "4x2SA13 con telai e cavi (4nec2, 4288 segmenti, simmetria GX)", File = "2SA_4x2SA13_4nec2.nec",
                       Atteso = new Complex(49.092, -1.9967), Fonte = "nec2c, 144 MHz (data/prove/4nec2_attesi.csv)" },
        };

        public static int Esegui()
        {
            sb = new StringBuilder();
            passoN = nErr = nAvv = 0;
            Riga("COLLAUDO DEI MODELLI PIU' PESANTI (fase 2E) - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Riga("Senza finestra. Per ogni modello: impedenza come nec2c, tempi delle fasi del motore, memoria, resa della fattorizzazione.");
            Riga("Scopo (IC8TEM, 14/11/2026): capire dove va il tempo sui modelli grandi e se conviene riscrivere una parte del motore.");
            Riga(new string('=', 100));
            Riga(string.Format(CI, "Computer: {0} processori logici; memoria disponibile {1:0} MB; istruzioni vettoriali {2} ({3} double per registro); GC {4}; 64 bit {5}",
                Environment.ProcessorCount, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1048576.0,
                Vector.IsHardwareAccelerated ? "si'" : "no", Vector<double>.Count, GCSettings.IsServerGC ? "server" : "workstation", Environment.Is64BitProcess ? "si'" : "no"));

            string cart = Path.Combine(AppPaths.DataDir, "prove", "pesanti");
            List<Misura> misure = new List<Misura>();
            int k = 0;
            foreach (Caso c in Casi)
            {
                k++;
                Riga("");
                Riga("MODELLO " + k + " - " + c.Nome);
                Misura m = Prova(c, Path.Combine(cart, c.File));
                if (m != null) misure.Add(m);
                Registro.Scrivi("COLLAUDO", "modello pesante " + k + ": " + c.Nome);
            }

            // 0.4.20: la LU a blocchi contro la LU classica di nec2c sullo stesso modello (Bowtie, il piu' pesante senza simmetria
            // che EZNEC calcola): stesso risultato a meno degli arrotondamenti e quanto tempo si guadagna
            Riga("");
            Riga("LU A BLOCCHI (0.4.20) CONTRO LU CLASSICA DI NEC2C (fino alla 0.4.19) - Bowtie 91XG");
            ConfrontoLu(Path.Combine(cart, "Bowtie_91XG_4nec2.nec"), misure.Find(x => x.Nome.StartsWith("Bowtie", StringComparison.Ordinal)));

            Riga("");
            Riga("PROVA DI RIFERIMENTO DEL PROCESSORE");
            double gRif = Riferimento();

            Riga("");
            Riga(new string('=', 100));
            Riga("DOVE VA IL TEMPO (percentuali del calcolo di ogni modello)");
            Riga(string.Format(CI, "  {0,-46} {1,6} {2,8} {3,8} {4,8} {5,8} {6,8} {7,9} {8,8}", "modello", "N", "Sommerf.", "riempim.", "LU", "soluz.", "diagr.", "totale s", "GFLOPS"));
            foreach (Misura m in misure)
            {
                double t = Math.Max(1, m.MsTot);
                Riga(string.Format(CI, "  {0,-46} {1,6} {2,7:0}% {3,7:0}% {4,7:0}% {5,7:0}% {6,7:0}% {7,9:0.0} {8,8:0.0}",
                    Taglia(m.Nome, 46), m.N, 100 * m.MsSom / t, 100 * m.MsRiemp / t, 100 * m.MsLu / t, 100 * m.MsSol / t, 100 * m.MsDiag / t, m.MsTot / 1000, m.Gflops));
            }
            // andamento con N dei modelli senza simmetria: la fattorizzazione dovrebbe crescere come N^3, il riempimento come N^2
            List<Misura> piene = misure.FindAll(x => x.Nop <= 1 && x.MsLu > 0);
            if (piene.Count >= 2)
            {
                piene.Sort((a, b) => b.N.CompareTo(a.N));
                Misura a1 = piene[0], a2 = piene[piene.Count - 1];
                double r = Math.Log((double)a1.N / a2.N);
                if (r > 0.05)
                    Riga(string.Format(CI, "  Crescita con N ({0} -> {1} segmenti): fattorizzazione N^{2:0.00} (teoria 3), riempimento N^{3:0.00} (teoria 2).",
                        a2.N, a1.N, Math.Log(a1.MsLu / a2.MsLu) / r, Math.Log(a1.MsRiemp / Math.Max(1, a2.MsRiemp)) / r));
            }
            double gMax = 0;
            foreach (Misura m in misure) gMax = Math.Max(gMax, m.Gflops);
            if (gRif > 0 && gMax > 0)
                Riga(string.Format(CI, "  Fattorizzazione attuale: al massimo {0:0.0} GFLOPS; prodotto di matrici a blocchi con le istruzioni vettoriali: {1:0.0} GFLOPS (rapporto {2:0.0}).",
                    gMax, gRif, gRif / gMax));
            Riga("  Lettura: se la fattorizzazione prende la gran parte del tempo e il rapporto e' alto, conviene riscriverla a blocchi (stessi");
            Riga("  risultati di nec2c a meno degli arrotondamenti); se domina il riempimento conviene lavorare su Sommerfeld e sul nucleo dei fili.");

            Riga("");
            Riga(nErr == 0 ? (nAvv == 0 ? "MODELLI PESANTI: SUPERATO." : "MODELLI PESANTI: SUPERATO con " + nAvv + " avvisi.") : "MODELLI PESANTI: NON SUPERATO (" + nErr + " errori).");
            string txt = sb.ToString();
            try { File.WriteAllText(Path.Combine(AppPaths.DataDir, "prove_pesanti.txt"), txt, new UTF8Encoding(false)); } catch { }
            return nErr;
        }

        static Misura Prova(Caso c, string file)
        {
            if (!File.Exists(file)) { Passo("aprire " + Path.GetFileName(file), "file presente in data/prove/pesanti", "file mancante", false); return null; }
            try
            {
                long memPrima = GC.GetTotalAllocatedBytes(false);
                Stopwatch sw = Stopwatch.StartNew();
                NecModel model = NecModel.Load(file);
                SegmentGeometry geo = model.BuildGeometry();
                List<double> fr = model.Frequencies();
                double f = fr != null && fr.Count > 0 ? fr[0] : 0;
                double msCarica = sw.Elapsed.TotalMilliseconds;
                int n = geo.N;
                bool sim = geo.SymNop > 1;
                double mb = sim ? 16.0 * n * geo.SymNp / 1048576.0 + geo.SymNop * 16.0 * geo.SymNp * (double)geo.SymNp / 1048576.0 : 16.0 * n * (double)n / 1048576.0;
                Passo("aprire il modello e preparare i segmenti", "", string.Format(CI, "{0} segmenti, {1:0.###} MHz, {2}; matrice {3:0} MB; {4:0} ms",
                    n, f, sim ? "simmetria " + geo.SymNop + " copie da " + geo.SymNp : "nessuna simmetria", mb, msCarica), n > 0 && f > 0);
                sw.Restart();
                RunResult r = Analysis.Run(model, geo, f, 5.0);
                double msTot = sw.Elapsed.TotalMilliseconds;
                Nec2Engine e = r.Engine;
                double rel = Complex.Abs(r.Zin - c.Atteso) / Math.Max(1e-9, Complex.Abs(c.Atteso));
                Passo("impedenza come nec2c", string.Format(CI, "{0} entro l'1% ({1})", Z(c.Atteso), c.Fonte),
                    string.Format(CI, "{0} (scarto {1:0.000}%)", Z(r.Zin), 100 * rel), rel < 0.01);
                Misura m = new Misura { Nome = c.Nome, N = n, Np = sim ? geo.SymNp : n, Nop = sim ? geo.SymNop : 1, MsCarica = msCarica,
                    MsSom = e.MsSommerfeld, MsRiemp = e.MsRiempimento, MsLu = e.MsFattorizzazione, MsSol = e.MsSoluzione, MsDiag = r.MsDiagramma, MsTot = msTot, MbMatrice = mb };
                // operazioni della fattorizzazione LU complessa: N^3/3 moltiplicazioni-somme complesse = 8 N^3 / 3 operazioni reali
                double flop = m.Nop * 8.0 / 3.0 * Math.Pow(m.Np, 3);
                m.Gflops = m.MsLu > 0 ? flop / (m.MsLu * 1e6) : 0;
                double altro = Math.Max(0, msTot - m.MsSom - m.MsRiemp - m.MsLu - m.MsSol - m.MsDiag);
                Passo("tempi delle fasi del motore", "",
                    string.Format(CI, "Sommerfeld {0:0} ms, riempimento {1:0} ms, fattorizzazione LU {2:0} ms ({3:0.0} GFLOPS), soluzione {4:0} ms, diagramma 3D a 5 gradi {5:0} ms, altro {6:0} ms; totale {7:0.0} s{8}",
                        m.MsSom, m.MsRiemp, m.MsLu, m.Gflops, m.MsSol, m.MsDiag, altro, msTot / 1000, string.IsNullOrEmpty(e.SimmetriaUsata) ? "" : " (" + e.SimmetriaUsata + ")"), true);
                long allocate = GC.GetTotalAllocatedBytes(false) - memPrima;
                long picco = 0;
                try { picco = Process.GetCurrentProcess().PeakWorkingSet64; } catch { }
                double disp = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                Passo("memoria", "picco del programma sotto la meta' della RAM del computer",
                    string.Format(CI, "picco {0:0} MB, allocati {1:0} MB, RAM {2:0} MB, sweep in parallelo su {3} punti", picco / 1048576.0, allocate / 1048576.0, disp / 1048576.0, Analysis.ParallelismoPerMemoria(n)),
                    disp <= 0 || picco < disp / 2, true);
                Passo("guadagno e diagramma", "massimo finito", string.Format(CI, "Gmax {0:0.00} dBi a elevazione {1:0} gradi, azimut {2:0}", r.GmaxDbi, r.ElevationOfMax, r.PhiMax),
                    !double.IsNaN(r.GmaxDbi) && r.GmaxDbi > -100 || c.File.StartsWith("Butternut_7877", StringComparison.Ordinal), true);
                Registro.Scrivi("TEMPO", string.Format(CI, "modello pesante {0}: {1} segmenti, riempimento {2:0} ms, LU {3:0} ms ({4:0.0} GFLOPS), diagramma {5:0} ms, totale {6:0} ms",
                    Path.GetFileName(file), n, m.MsRiemp, m.MsLu, m.Gflops, m.MsDiag, msTot));
                model = null; r = null; e = null;
                GC.Collect();
                return m;
            }
            catch (Exception ex)
            {
                Passo("calcolo", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false);
                return null;
            }
        }

        static void ConfrontoLu(string file, Misura blocchi)
        {
            if (!File.Exists(file) || blocchi == null) { Passo("confronto delle due LU", "Bowtie calcolato sopra", "manca", false, true); return; }
            bool prima = Nec2Engine.LuClassica;
            try
            {
                NecModel model = NecModel.Load(file);
                SegmentGeometry geo = model.BuildGeometry();
                double f = model.Frequencies()[0];
                RunResult rb = Analysis.Run(model, geo, f, 10.0);
                Nec2Engine.LuClassica = true;
                RunResult rc = Analysis.Run(model, geo, f, 10.0);
                Nec2Engine.LuClassica = prima;
                double rel = Complex.Abs(rb.Zin - rc.Zin) / Math.Max(1e-9, Complex.Abs(rc.Zin));
                Passo("stessa impedenza con le due LU", "scarto sotto 1e-8 (solo arrotondamenti)",
                    string.Format(CI, "a blocchi {0}, classica {1}, scarto {2:0.0E+0}", Z(rb.Zin), Z(rc.Zin), rel), rel < 1e-8);
                double flop = 8.0 / 3.0 * Math.Pow(geo.N, 3);
                double gb = rb.Engine.MsFattorizzazione > 0 ? flop / (rb.Engine.MsFattorizzazione * 1e6) : 0;
                double gc = rc.Engine.MsFattorizzazione > 0 ? flop / (rc.Engine.MsFattorizzazione * 1e6) : 0;
                Passo("tempo della fattorizzazione", "LU a blocchi piu' veloce della classica",
                    string.Format(CI, "a blocchi {0:0.0} s ({1:0.0} GFLOPS), classica {2:0.0} s ({3:0.0} GFLOPS): {4:0.0} volte; istruzioni AVX2/FMA {5}",
                        rb.Engine.MsFattorizzazione / 1000, gb, rc.Engine.MsFattorizzazione / 1000, gc, rc.Engine.MsFattorizzazione / Math.Max(1, rb.Engine.MsFattorizzazione),
                        System.Runtime.Intrinsics.X86.Fma.IsSupported ? "si'" : "no (nucleo senza istruzioni vettoriali)"),
                    rb.Engine.MsFattorizzazione < rc.Engine.MsFattorizzazione, true);
                Registro.Scrivi("TEMPO", string.Format(CI, "LU a blocchi {0:0} ms, LU classica {1:0} ms (Bowtie, {2} segmenti)", rb.Engine.MsFattorizzazione, rc.Engine.MsFattorizzazione, geo.N));
                rb = null; rc = null; GC.Collect();
            }
            catch (Exception ex) { Passo("confronto delle due LU", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false); }
            finally { Nec2Engine.LuClassica = prima; }
        }

        // prodotto C = A B di matrici complesse n x n (parte reale e immaginaria separate), a blocchi di righe in parallelo e con
        // le istruzioni vettoriali: e' il nucleo di una fattorizzazione LU a blocchi; misura quanti GFLOPS il computer puo' dare
        static double Riferimento()
        {
            try
            {
                int n = 768, vc = Vector<double>.Count;
                double[] ar = new double[n * n], ai = new double[n * n], br = new double[n * n], bi = new double[n * n], cr = new double[n * n], ci = new double[n * n];
                Random rnd = new Random(1);
                for (int i = 0; i < ar.Length; i++) { ar[i] = rnd.NextDouble(); ai[i] = rnd.NextDouble(); br[i] = rnd.NextDouble(); bi[i] = rnd.NextDouble(); }
                Action giro = () =>
                {
                    Array.Clear(cr, 0, cr.Length); Array.Clear(ci, 0, ci.Length);
                    Parallel.For(0, n / 16, blk =>
                    {
                        for (int i = blk * 16; i < blk * 16 + 16; i++)
                            for (int kk = 0; kk < n; kk++)
                            {
                                double xr = ar[i * n + kk], xi = ai[i * n + kk];
                                Vector<double> vxr = new Vector<double>(xr), vxi = new Vector<double>(xi);
                                int rb = kk * n, rc = i * n, j = 0;
                                for (; j <= n - vc; j += vc)
                                {
                                    Vector<double> yr = new Vector<double>(br, rb + j), yi = new Vector<double>(bi, rb + j);
                                    (new Vector<double>(cr, rc + j) + vxr * yr - vxi * yi).CopyTo(cr, rc + j);
                                    (new Vector<double>(ci, rc + j) + vxr * yi + vxi * yr).CopyTo(ci, rc + j);
                                }
                                for (; j < n; j++) { cr[rc + j] += xr * br[rb + j] - xi * bi[rb + j]; ci[rc + j] += xr * bi[rb + j] + xi * br[rb + j]; }
                            }
                    });
                };
                giro();   // riscaldamento (compilazione JIT)
                Stopwatch sw = Stopwatch.StartNew();
                giro();
                double ms = sw.Elapsed.TotalMilliseconds;
                double g = 8.0 * n * (double)n * n / (ms * 1e6);
                Passo("prodotto di matrici complesse 768 x 768 a blocchi, in parallelo e vettoriale", "",
                    string.Format(CI, "{0:0} ms, {1:0.0} GFLOPS su {2} processori logici", ms, g, Environment.ProcessorCount), true);
                return g;
            }
            catch (Exception ex)
            {
                Passo("prova di riferimento", "nessuna eccezione", ex.GetType().Name + ": " + ex.Message, false, true);
                return 0;
            }
        }

        static string Taglia(string s, int n) { return s.Length <= n ? s : s.Substring(0, n - 1) + "."; }
        static string Z(Complex z) { return string.Format(CI, "{0:0.000} {1} j{2:0.000} ohm", z.Real, z.Imaginary < 0 ? "-" : "+", Math.Abs(z.Imaginary)); }
        static void Riga(string s) { sb.AppendLine(s); }

        static void Passo(string cosa, string atteso, string ottenuto, bool ok, bool soloAvviso = false)
        {
            passoN++;
            string e = ok ? "OK" : (soloAvviso ? "AVVISO" : "ERRORE");
            if (!ok) { if (soloAvviso) nAvv++; else nErr++; }
            Riga(string.Format(CI, "  E.{0,-3} {1}", passoN, cosa));
            if (!string.IsNullOrEmpty(atteso)) Riga("         atteso:   " + atteso);
            Riga("         ottenuto: " + ottenuto);
            Riga("         esito:    " + e);
        }
    }
}
