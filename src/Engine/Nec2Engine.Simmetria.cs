// YA-NEC - soluzione con le simmetrie GX (riflessioni) e GR (rotazione attorno a z), come cmset/factrs/solves di nec2c
// (NEC-2, LLNL: matrice a blocchi trasformata), scritta in forma di gruppo. GPL-3.0.
//
// 0.4.2, approvato da IC8TEM il 13/11/2026. Se la struttura e' fatta di nop copie uguali della prima (np segmenti), il
// blocco della matrice tra la copia p e la copia q dipende solo da g_p^-1 g_q: Z(p,q) = A(g_p^-1 g_q), con A(h) = Z(0,h).
// Basta quindi riempire le righe della prima copia (1/nop del riempimento) e, con i caratteri chi_k del gruppo
// (rotazione: exp(j 2 pi k p / nop); riflessioni: (-1)^(bit comuni di k e p)), la matrice si separa in nop blocchi
// indipendenti Zk = somma_h chi_k(h) A(h) di dimensione np: fattorizzazione nop^2 volte piu' veloce.
// Termine noto: ek = (1/nop) somma_p conj(chi_k(p)) E(p); soluzione I(p) = somma_k chi_k(p) Zk^-1 ek.
// La simmetria si usa solo se: la geometria la rispetta segmento per segmento (estremi e raggi), i carichi sono uguali
// sulle copie, non c'e' riflessione nel piano z con il terreno. Altrimenti si risolve la matrice intera (risultati uguali).
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace YaNec.Engine
{
    public sealed partial class Nec2Engine
    {
        /// <summary>Sfrutta le simmetrie GX/GR nella soluzione (vero di partenza; falso solo per i confronti del collaudo).</summary>
        public static bool UsaSimmetrie = true;

        /// <summary>Simmetria usata nell'ultima soluzione ("" = matrice intera).</summary>
        public string SimmetriaUsata { get; private set; } = "";

        int sNp, sNop;
        Complex[,] sChi;          // sChi[k, p] = chi_k(copia p)
        Complex[][] sZk;          // blocchi fattorizzati
        int[][] sIp;

        bool SimmetriaUsabile()
        {
            int np = geo.SymNp, nop = geo.SymNop;
            if (!UsaSimmetrie || nop < 2 || np < 1 || np * nop != n || m > 0) return false;
            string piani = geo.SymPiani ?? "";
            if (ksymp == 2 && piani.Contains("z")) return false;       // riflessione nel piano del terreno: non ammessa
            // estensione del modello per la tolleranza
            double sc = 0;
            for (int i = 0; i < n; i++)
                sc = Math.Max(sc, Math.Max(Math.Max(Math.Abs(geo.X1[i]), Math.Abs(geo.Y1[i])), Math.Max(Math.Abs(geo.Z1[i]), Math.Max(Math.Abs(geo.X2[i]), Math.Max(Math.Abs(geo.Y2[i]), Math.Abs(geo.Z2[i]))))));
            double tol = 1e-7 * Math.Max(sc, 1e-3);
            for (int c = 1; c < nop; c++)
            {
                double ca = 1, sa = 0;
                if (piani.Length == 0) { ca = Math.Cos(2 * Math.PI * c / nop); sa = Math.Sin(2 * Math.PI * c / nop); }
                for (int i = 0; i < np; i++)
                {
                    int s = c * np + i;
                    double ax1 = geo.X1[i], ay1 = geo.Y1[i], az1 = geo.Z1[i], ax2 = geo.X2[i], ay2 = geo.Y2[i], az2 = geo.Z2[i];
                    if (piani.Length == 0)
                    {
                        double t1 = ax1 * ca - ay1 * sa, t2 = ax2 * ca - ay2 * sa;
                        ay1 = ax1 * sa + ay1 * ca; ay2 = ax2 * sa + ay2 * ca; ax1 = t1; ax2 = t2;
                    }
                    else
                        for (int b = 0; b < piani.Length; b++)
                        {
                            if ((c & (1 << b)) == 0) continue;
                            if (piani[b] == 'x') { ax1 = -ax1; ax2 = -ax2; }
                            else if (piani[b] == 'y') { ay1 = -ay1; ay2 = -ay2; }
                            else { az1 = -az1; az2 = -az2; }
                        }
                    if (Math.Abs(geo.X1[s] - ax1) > tol || Math.Abs(geo.Y1[s] - ay1) > tol || Math.Abs(geo.Z1[s] - az1) > tol ||
                        Math.Abs(geo.X2[s] - ax2) > tol || Math.Abs(geo.Y2[s] - ay2) > tol || Math.Abs(geo.Z2[s] - az2) > tol) return false;
                    if (Math.Abs(geo.Bi[s] - geo.Bi[i]) > 1e-9 * Math.Max(geo.Bi[i], 1e-12)) return false;
                    if (nload > 0 && Complex.Abs(zarray[s] - zarray[i]) > 1e-9 * Complex.Abs(zarray[i]) + 1e-15) return false;
                }
            }
            return true;
        }

        /// <summary>Riempie le righe della prima copia e fattorizza i nop blocchi; restituisce falso se la simmetria non e' usabile.</summary>
        bool FattorizzaSimmetrica(double rkhx, int iexkx)
        {
            SimmetriaUsata = "";
            if (!SimmetriaUsabile()) return false;
            int np = geo.SymNp, nop = geo.SymNop;
            string piani = geo.SymPiani ?? "";
            Complex[] cm = new Complex[(long)n * np];
            System.Diagnostics.Stopwatch swF = System.Diagnostics.Stopwatch.StartNew();   // 0.4.18: tempi delle fasi
            using (Prestazioni.Misura("Motore: riempimento della matrice", "simmetria, " + nop + " copie", false)) CmSet(cm, rkhx, iexkx, np);
            MsRiempimento = swF.Elapsed.TotalMilliseconds;
            swF.Restart();
            Complex[,] chi = new Complex[nop, nop];
            for (int k = 0; k < nop; k++)
                for (int p = 0; p < nop; p++)
                {
                    if (piani.Length == 0) chi[k, p] = Complex.FromPolarCoordinates(1, 2 * Math.PI * ((long)k * p % nop) / nop);
                    else
                    {
                        int b = k & p, c = 0;
                        while (b != 0) { c += b & 1; b >>= 1; }
                        chi[k, p] = (c & 1) == 0 ? Complex.One : -Complex.One;
                    }
                }
            Complex[][] zk = new Complex[nop][];
            int[][] ipk = new int[nop][];
            // 0.4.20: con la LU a blocchi (gia' in parallelo al suo interno) i blocchi grandi si fattorizzano uno dopo l'altro.
            // 0.4.30 (punto 8 approvato da IC8TEM il 14/11/2026 22:08): sul 4x2SA13 (4 blocchi da 1072) la LU rendeva 2,7 GFLOPS contro
            // i 40 della Bowtie: su blocchi di un migliaio di righe la LU a blocchi ha pannelli seriali e poco lavoro da dividere tra i
            // processori. Ora i blocchi sotto le 2048 righe si fattorizzano tutti insieme (uno per processore, fino a nop), e la LU di
            // ognuno usa i processori rimasti; solo i blocchi grandi restano uno dopo l'altro.
            ParallelOptions poBlocchi = new ParallelOptions { MaxDegreeOfParallelism = np >= 2048 && !LuClassica ? 1 : Math.Max(1, Math.Min(nop, Processori.Calcolo)) };
            using (Prestazioni.Misura("Motore: fattorizzazione LU", "simmetria, " + nop + " blocchi di " + np, false))
                Parallel.For(0, nop, poBlocchi, k =>
                {
                    // ingresso di Factr trasposto come nec2c: a[unk + eq * np]; cm[unk + oss * n] (oss nella prima copia)
                    Complex[] a = new Complex[(long)np * np];
                    for (int h = 0; h < nop; h++)
                    {
                        Complex c = chi[k, h];
                        int off = h * np;
                        for (int eq = 0; eq < np; eq++)
                        {
                            long rs = (long)eq * n + off, rd = (long)eq * np;
                            for (int u = 0; u < np; u++) a[rd + u] += c * cm[rs + u];
                        }
                    }
                    int[] ip = new int[np];
                    Factr(np, a, ip, np);
                    zk[k] = a; ipk[k] = ip;
                });
            MsFattorizzazione = swF.Elapsed.TotalMilliseconds;
            sNp = np; sNop = nop; sChi = chi; sZk = zk; sIp = ipk;
            SimmetriaUsata = piani.Length == 0 ? "rotazione GR, " + nop + " copie" : "riflessione GX nei piani " + piani + ", " + nop + " copie";
            return true;
        }

        /// <summary>Soluzione con i blocchi della simmetria: e (campo applicato) in ingresso, correnti in uscita.</summary>
        void RisolviSimmetrica(Complex[] e)
        {
            int np = sNp, nop = sNop;
            Complex[][] x = new Complex[nop][];
            Parallel.For(0, nop, k =>
            {
                Complex[] ek = new Complex[np];
                for (int p = 0; p < nop; p++)
                {
                    Complex c = Complex.Conjugate(sChi[k, p]) / nop;
                    int off = p * np;
                    for (int a = 0; a < np; a++) ek[a] += c * e[off + a];
                }
                SolveLu(np, sZk[k], sIp[k], ek, np);
                x[k] = ek;
            });
            for (int p = 0; p < nop; p++)
            {
                int off = p * np;
                for (int a = 0; a < np; a++)
                {
                    Complex t = Complex.Zero;
                    for (int k = 0; k < nop; k++) t += sChi[k, p] * x[k][a];
                    e[off + a] = t;
                }
            }
        }
    }
}
