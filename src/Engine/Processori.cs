// YA-NEC - quanti processori usa il calcolo (0.4.20). GPL-3.0.
using System;
using System.Threading.Tasks;

namespace YaNec.Engine
{
    /// <summary>
    /// 0.4.20 (approvato da IC8TEM il 14/11/2026 dopo i registri della 0.4.19): il calcolo lascia libero un processore logico per
    /// l'interfaccia. Nei registri le finestre ferme fino a 1,2 s capitavano quando calcolo e sweep occupavano tutti gli 8 processori
    /// logici dell'i7-7700; con un processore libero il thread della finestra trova sempre posto. Con 1 o 2 processori si usano tutti.
    /// </summary>
    public static class Processori
    {
        /// <summary>Vero (normale): un processore resta all'interfaccia. Falso nella riga di comando e nel collaudo dei tempi.</summary>
        public static bool RiservaInterfaccia = true;

        public static int Calcolo
        {
            get
            {
                int n = Math.Max(1, Environment.ProcessorCount);
                return RiservaInterfaccia && n > 2 ? n - 1 : n;
            }
        }

        public static ParallelOptions Opzioni() { return new ParallelOptions { MaxDegreeOfParallelism = Calcolo }; }
    }
}
