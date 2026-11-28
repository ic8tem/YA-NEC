// YA-NEC 1.9.0 - prova della memoria (YA-NEC --prova-memoria [file o cartelle...]): per ogni modello calcola come l'interfaccia
// (diagramma a 2 gradi, si tiene solo l'ultimo risultato) e scrive i numeri del motore a 17 cifre e la memoria subito dopo e 3 s dopo.
// Si lancia con e senza YANEC_RAM_VECCHIA=1: i numeri devono essere identici, la memoria deve scendere (IC8TEM 28/11/2026 07:48).
#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using YaNec.Engine;

namespace YaNec
{
    public static class ProvaMemoria
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static string R(double v) { return v.ToString("R", CI); }
        static string Mem()
        {
            var info = GC.GetGCMemoryInfo();
            using (var p = Process.GetCurrentProcess())
                return string.Format(CI, "heap {0:0} MB, impegnata {1:0} MB, privata {2:0} MB, working set {3:0} MB",
                    GC.GetTotalMemory(false) / 1048576.0, info.TotalCommittedBytes / 1048576.0, p.PrivateMemorySize64 / 1048576.0, p.WorkingSet64 / 1048576.0);
        }

        public static int Esegui(IEnumerable<string> percorsi)
        {
            Console.WriteLine("YA-NEC " + AppPaths.Version + " - prova della memoria - RAM nuova: " + (Memoria.Attiva ? "si'" : "no (YANEC_RAM_VECCHIA=1)") +
                ", GC: " + (System.Runtime.GCSettings.IsServerGC ? "server" : "workstation") + ", ConserveMemory=" + (AppContext.GetData("GCConserveMemory") ?? Environment.GetEnvironmentVariable("DOTNET_GCConserveMemory") ?? "-"));
            Console.WriteLine("Avvio: " + Mem());
            var modelli = new List<Tuple<string, NecModel>>();
            try { modelli.Add(Tuple.Create("W2IMU 1296 MHz del calcolatore", NecModel.Parse(Tromba.Calcola(new TrombaInput()).Nec))); } catch (Exception ex) { Console.WriteLine("W2IMU: " + ex.Message); }
            modelli.Add(Tuple.Create("8 fili su un anello, simmetria GR (2400 segmenti)", NecModel.Parse(
                "CM prova\nCE\nGW 1 300 1 0 -1 1 0 1 0.002\nGR 0 8\nGE 0\nFR 0 1 0 0 145 0\nEX 0 1 150 0 1 0\nEN\n")));
            foreach (string p in percorsi)
            {
                var files = Directory.Exists(p) ? Directory.GetFiles(p, "*.*", SearchOption.AllDirectories).Where(f => f.EndsWith(".nec", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".yanec", StringComparison.OrdinalIgnoreCase)).OrderBy(f => f).ToArray() : new[] { p };
                foreach (string f in files) { try { modelli.Add(Tuple.Create(Path.GetFileName(f), NecModel.Load(f))); } catch (Exception ex) { Console.WriteLine("NON LETTO " + f + ": " + ex.Message); } }
            }
            RunResult tenuto = null;
            foreach (var t in modelli)
            {
                try
                {
                    var g = t.Item2.BuildGeometry();
                    var sw = Stopwatch.StartNew();
                    var r = Analysis.Run(t.Item2, g, t.Item2.FreqMHz, 2.0);
                    tenuto = r;
                    double somma = 0;
                    if (r.Grid != null) for (int i = 0; i < r.Grid.NTheta; i++) for (int j = 0; j < r.Grid.NPhi; j++) { double v = r.Grid.Total[i, j]; if (!double.IsInfinity(v) && !double.IsNaN(v)) somma += v * (i + 1) * (j + 3); }
                    Console.WriteLine("NUMERI | " + t.Item1 + " | " + g.N + " seg | Z " + R(r.Zin.Real) + " " + R(r.Zin.Imaginary) + " | G " + R(r.GmaxDbi) + " | FB " + R(r.FrontBack) +
                        " | eff " + R(r.Efficiency) + " | P " + R(r.InputPower) + " | diagramma " + R(somma));
                    Console.WriteLine("MEMORIA | " + t.Item1 + " | " + sw.ElapsedMilliseconds + " ms | subito: " + Mem());
                    if ((long)g.N * g.N * 16 >= Memoria.Soglia) { Thread.Sleep(3000); Console.WriteLine("MEMORIA | " + t.Item1 + " | 3 s dopo: " + Mem()); }
                }
                catch (Exception ex) { Console.WriteLine("ERRORE | " + t.Item1 + " | " + ex.Message); }
            }
            Thread.Sleep(3000);
            Console.WriteLine("Fine (tenuto l'ultimo risultato, " + (tenuto != null ? tenuto.Geometry.N + " segmenti" : "-") + "), compattazioni " + Memoria.Compattazioni + ": " + Mem());
            return 0;
        }
    }
}
