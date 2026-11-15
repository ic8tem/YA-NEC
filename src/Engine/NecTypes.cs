// YA-NEC - tipi del modello NEC2 (schede, sorgenti, carichi, terreno)
// Motore derivato da nec2c di Neoklis Kyriazis 5B4AZ (traduzione in C del NEC2 di LLNL), GPL-3.0.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace YaNec.Engine
{
    public class NecException : Exception
    {
        public NecException(string m) : base(m) { }
    }

    /// <summary>Scheda di geometria (GW, GA, GH, GM, GR, GS, GX, GC...) come letta dal file.</summary>
    public class GeomCard
    {
        public string Id = "GW";
        public int I1, I2;
        public double F1, F2, F3, F4, F5, F6, F7;
        // scheda GC che segue un GW con raggio 0 (filo rastremato)
        public bool HasGc;
        public double GcRdel = 1, GcRad1, GcRad2;
        // testo originale dei campi (puo' contenere variabili ed espressioni SY): I1, I2, F1..F7 e per GC rdel, rad1, rad2
        public string[] Src;
        public string[] GcSrc;

        public GeomCard Clone() { return (GeomCard)MemberwiseClone(); }
    }

    /// <summary>Scheda di controllo (EX, LD, FR, GN, RP, EK, KH, ...).</summary>
    public class ControlCard
    {
        public string Id = "";
        public int I1, I2, I3, I4;
        public double F1, F2, F3, F4, F5, F6;
        public ControlCard Clone() { return (ControlCard)MemberwiseClone(); }
    }

    public enum GroundKind { FreeSpace = 0, Perfect = 1, Real = 2 }

    public class GroundSpec
    {
        public GroundKind Kind = GroundKind.FreeSpace;
        public double Epsr = 13;        // costante dielettrica relativa
        public double Sigma = 0.005;    // conducibilita' S/m
        public bool Sommerfeld;         // true = GN 2 (Sommerfeld/Norton), false = GN 0 (coefficiente di riflessione)
        public bool MiniNec;            // GN 3 di 4nec2 ("MiniNec ground"): correnti e impedenza su terreno perfetto, campo lontano con er e sigma
        // 0.4.30 (proposta da cocoaNEC 3 accettata da IC8TEM il 14/11/2026 22:33): schermo di radiali del NEC-2 (GN con NRADL), solo con
        // il terreno a coefficiente di riflessione (GN 0): numero di radiali, lunghezza (m) e raggio dei fili (m) dello schermo
        public int Nradl;
        public double SchermoL, SchermoR;
        public string Name = "";
        /// <summary>0.5.0: solaio in calcestruzzo (0 no, 1 VHF-UHF ITU-R P.2040, 2 HF Perotoni): er e sigma si ricalcolano a ogni frequenza (Engine/TerreniEdifici.cs).</summary>
        public int Calcestruzzo;
        public GroundSpec Clone() { return (GroundSpec)MemberwiseClone(); }
    }

    /// <summary>Carico LD: tipo 0 RLC serie, 1 RLC parallelo, 2 serie per metro, 3 parallelo per metro, 4 impedenza, 5 conducibilita' del filo.</summary>
    public class LoadSpec
    {
        public int Type, Tag, From, To;
        public double F1, F2, F3;
        public LoadSpec Clone() { return (LoadSpec)MemberwiseClone(); }
    }

    /// <summary>Sorgente di tensione EX 0 sul segmento Seg (1 = primo) dei segmenti con il tag Tag.</summary>
    /// <summary>Linea di trasmissione (TL) o rete a due porte (NT) tra due segmenti.
    /// TL: F1 = Z0 (negativa = linea incrociata), F2 = lunghezza in m (0 = distanza tra i centri dei segmenti),
    /// F3+jF4 e F5+jF6 = ammettenze in parallelo alle due estremita' (S). NT: F1..F6 = Y11, Y12, Y22 (parti reali e immaginarie).</summary>
    public class NetworkSpec
    {
        public bool IsTl = true;
        public int Tag1, Seg1, Tag2, Seg2;
        public double F1, F2, F3, F4, F5, F6;
        public NetworkSpec Clone() { return (NetworkSpec)MemberwiseClone(); }
    }

    public class SourceSpec
    {
        public int Tag, Seg;
        public Complex V = Complex.One;
        public bool Qds;     // EX 5: sorgente di tensione a discontinuita' di pendenza della corrente (qdsrc di NEC-2), dalla 0.4.2
        public SourceSpec Clone() { return (SourceSpec)MemberwiseClone(); }
    }

    /// <summary>Geometria a segmenti (coordinate in metri) dopo la connessione dei fili.</summary>
    public class SegmentGeometry
    {
        public int N;
        public double[] X1, Y1, Z1, X2, Y2, Z2, Bi;
        public int[] Tag, Icon1, Icon2;
        public int GroundFlag;   // I1 della scheda GE
        // simmetria dell'ultima scheda GX o GR (0.4.2): SymNop copie di SymNp segmenti; SymPiani = piani riflessi nell'ordine
        // dei bit della copia ("z", "y", "x" come reflc di nec2c), vuoto per la rotazione attorno a z. Il motore la usa solo
        // dopo averla verificata sui segmenti (Nec2Engine.Simmetria).
        public int SymNp, SymNop;
        public string SymPiani = "";

        public double CenterX(int i) { return (X1[i] + X2[i]) / 2; }
        public double CenterY(int i) { return (Y1[i] + Y2[i]) / 2; }
        public double CenterZ(int i) { return (Z1[i] + Z2[i]) / 2; }
        public double Length(int i)
        {
            double dx = X2[i] - X1[i], dy = Y2[i] - Y1[i], dz = Z2[i] - Z1[i];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Numero del segmento (1 = primo) dell'mx-esimo segmento con tag itag; itag 0 = segmento mx assoluto.</summary>
        public int SegNo(int itagi, int mx)
        {
            if (mx <= 0)
                throw new NecException("Il numero del segmento nel gruppo dello stesso tag non puo' essere zero.");
            if (itagi == 0) return mx;
            int icnt = 0;
            for (int i = 0; i < N; i++)
            {
                if (Tag[i] != itagi) continue;
                icnt++;
                if (icnt == mx) return i + 1;
            }
            throw new NecException("Nessun segmento con tag " + itagi + " e posizione " + mx + ".");
        }

        public int CountTag(int tag)
        {
            int c = 0;
            for (int i = 0; i < N; i++) if (Tag[i] == tag) c++;
            return c;
        }
    }
}
