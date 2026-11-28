// YA-NEC 1.9.0 - memoria dopo i calcoli grandi (richiesta di IC8TEM del 28/11/2026 07:35-07:48: «Cosa puoi migliorare a livello di ram
// occupata?»; «Prova prima se un cambio di impostazioni sulla ram infici la qualita' del motore: accetto solo un miglioramento»).
// Dopo un calcolo con la matrice sopra i 50 MB, 2 secondi dopo l'ultimo calcolo (cosi' lo sweep non si rallenta), una raccolta
// completa con la compattazione dei blocchi grandi (LOH): .NET restituisce al sistema la memoria della matrice invece di tenerla.
// Non tocca i numeri: la matrice a quel punto non e' piu' usata. YANEC_RAM_VECCHIA=1 spegne tutto (per i confronti).
#nullable disable
using System;
using System.Runtime;
using System.Threading;

namespace YaNec.Engine
{
    public static class Memoria
    {
        public static readonly bool Attiva = Environment.GetEnvironmentVariable("YANEC_RAM_VECCHIA") != "1";
        public const long Soglia = 50L * 1024 * 1024;
        static Timer timer;
        static readonly object blocco = new object();
        public static int Compattazioni;

        public static void DopoCalcolo(long byteMatrice)
        {
            if (!Attiva || byteMatrice < Soglia) return;
            lock (blocco)
            {
                if (timer == null) timer = new Timer(_ => Compatta(), null, 2000, Timeout.Infinite);
                else timer.Change(2000, Timeout.Infinite);
            }
        }

        public static void Compatta()
        {
            try
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                Interlocked.Increment(ref Compattazioni);
            }
            catch { }
        }
    }
}
