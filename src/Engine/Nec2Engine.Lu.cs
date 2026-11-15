// YA-NEC - fattorizzazione LU a blocchi (0.4.20). GPL-3.0.
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace YaNec.Engine
{
    public sealed partial class Nec2Engine
    {
        /// <summary>
        /// Vero: si usa la LU colonna per colonna di nec2c (FactrColonne/SolveLuColonne), come fino alla 0.4.19. Si attiva con la
        /// variabile d'ambiente YANEC_LU=classica; il collaudo la usa per confrontare i due metodi sugli stessi modelli.
        /// </summary>
        public static bool LuClassica = string.Equals(Environment.GetEnvironmentVariable("YANEC_LU"), "classica", StringComparison.OrdinalIgnoreCase);

        /// <summary>Dimensione del blocco di colonne della LU a blocchi.</summary>
        const int LuBlocco = 64;

        void Factr(int nn, Complex[] a, int[] ip, int ndim)
        {
            if (LuClassica) FactrColonne(nn, a, ip, ndim);
            else FactrBlocchi(nn, a, ip, ndim);
        }

        static void SolveLu(int nn, Complex[] a, int[] ip, Complex[] b, int ndim)
        {
            if (LuClassica) SolveLuColonne(nn, a, ip, b, ndim);
            else SolveLuBlocchi(nn, a, ip, b, ndim);
        }

        // 0.4.20 (scelta di IC8TEM del 14/11/2026, "LU riscritta a blocchi"): eliminazione di Gauss con pivot parziale nella forma
        // a blocchi di LAPACK (zgetrf: Anderson et al., "LAPACK Users' Guide", 3a ed., SIAM 1999, cap. 3.4; Golub & Van Loan,
        // "Matrix Computations", 4a ed., 2013, par. 3.2.11). Per ogni blocco di 64 colonne: 1) il pannello si fattorizza colonna per
        // colonna (pivot, scambio delle righe nel pannello, scala, aggiornamento delle colonne del pannello); 2) gli scambi si
        // applicano alle altre colonne; 3) U12 = L11^-1 A12; 4) A22 -= L21 U12, il prodotto di matrici che fa quasi tutto il conto,
        // a riquadri di 256 righe e 4 colonne tenuti nei registri del processore (AVX2/FMA: due numeri complessi per istruzione).
        // A differenza di nec2c gli scambi di riga valgono per tutta la matrice (L memorizzata gia' permutata): SolveLuBlocchi
        // applica prima tutti gli scambi al termine noto e poi risolve L e U. Stesso risultato matematico di FactrColonne; le
        // ultime cifre possono cambiare per il diverso ordine delle somme (prova nel collaudo: scarto sotto 1e-9 sull'impedenza).
        void FactrBlocchi(int nn, Complex[] a, int[] ip, int ndim)
        {
            ParallelOptions po = Processori.Opzioni();
            // la matrice viene "de-trasposta" per l'eliminazione di Gauss (come nec2c), a riquadri di 32 x 32 in parallelo
            const int T = 32;
            int nt = (nn + T - 1) / T;
            Parallel.For(0, nt, po, I =>
            {
                int i0 = I * T, i1 = Math.Min(nn, i0 + T);
                for (int J = 0; J <= I; J++)
                {
                    int j0 = J * T, j1 = Math.Min(nn, j0 + T);
                    for (int i = i0; i < i1; i++)
                    {
                        int jm = Math.Min(j1, i);
                        for (int j = j0; j < jm; j++)
                        {
                            long ij = i + (long)j * ndim, ji = j + (long)i * ndim;
                            Complex t = a[ij]; a[ij] = a[ji]; a[ji] = t;
                        }
                    }
                }
            });

            bool simd = Fma.IsSupported;
            for (int k0 = 0; k0 < nn; k0 += LuBlocco)
            {
                int k1 = Math.Min(nn, k0 + LuBlocco);
                // 1) pannello
                for (int j = k0; j < k1; j++)
                {
                    long jc = (long)j * ndim;
                    double dmax = -1; int p = j;
                    for (int i = j; i < nn; i++)
                    {
                        Complex v = a[i + jc];
                        double m = v.Real * v.Real + v.Imaginary * v.Imaginary;
                        if (m > dmax) { dmax = m; p = i; }
                    }
                    ip[j] = p + 1;
                    if (dmax < 1.0e-10 && PivotWarning == null)
                        PivotWarning = "Pivot molto piccolo alla riga " + j + " (" + dmax.ToString("E3") + "): matrice quasi singolare, controllare il modello.";
                    if (p != j)
                        for (int c = k0; c < k1; c++)
                        {
                            long cc = (long)c * ndim;
                            Complex t = a[j + cc]; a[j + cc] = a[p + cc]; a[p + cc] = t;
                        }
                    Complex piv = a[j + jc];
                    Complex inv = piv == Complex.Zero ? Complex.Zero : 1.0 / piv;
                    for (int i = j + 1; i < nn; i++) a[i + jc] *= inv;
                    int jj = j;
                    int righe = nn - j - 1, colonne = k1 - j - 1;
                    if (righe <= 0 || colonne <= 0) continue;
                    Action<int, int> agg = (r0, r1) =>
                    {
                        long jcc = (long)jj * ndim;
                        for (int c = jj + 1; c < k1; c++)
                        {
                            long cc = (long)c * ndim;
                            Complex u = a[jj + cc];
                            if (u == Complex.Zero) continue;
                            for (int i = r0; i < r1; i++) a[i + cc] -= a[i + jcc] * u;
                        }
                    };
                    if ((long)righe * colonne < 65536 || Processori.Calcolo < 2) agg(j + 1, nn);
                    else
                    {
                        const int R = 1024;
                        int nr = (righe + R - 1) / R;
                        Parallel.For(0, nr, po, t => agg(jj + 1 + t * R, Math.Min(nn, jj + 1 + (t + 1) * R)));
                    }
                }
                // 2) scambi di riga sulle colonne fuori dal pannello
                int fuori = nn - (k1 - k0);
                if (fuori > 0)
                {
                    int kk0 = k0, kk1 = k1;
                    Parallel.For(0, fuori, po, t =>
                    {
                        int c = t < kk0 ? t : t + (kk1 - kk0);
                        long cc = (long)c * ndim;
                        for (int j = kk0; j < kk1; j++)
                        {
                            int p = ip[j] - 1;
                            if (p != j) { Complex x = a[j + cc]; a[j + cc] = a[p + cc]; a[p + cc] = x; }
                        }
                    });
                }
                if (k1 >= nn) break;
                // 3) U12 = L11^-1 A12 (L11 triangolare inferiore con 1 sulla diagonale)
                {
                    int kk0 = k0, kk1 = k1;
                    Parallel.For(k1, nn, po, c =>
                    {
                        long cc = (long)c * ndim;
                        for (int j = kk0; j < kk1; j++)
                        {
                            Complex u = a[j + cc];
                            if (u == Complex.Zero) continue;
                            long jc = (long)j * ndim;
                            for (int i = j + 1; i < kk1; i++) a[i + cc] -= a[i + jc] * u;
                        }
                    });
                }
                // 4) A22 -= L21 U12, a gruppi di 32 colonne in parallelo
                {
                    const int CW = 32;
                    int kk0 = k0, kk1 = k1;
                    int ng = (nn - k1 + CW - 1) / CW;
                    Parallel.For(0, ng, po, () => new Vector256<double>[2 * LuBlocco * 4], (g, st, buf) =>
                    {
                        int c0 = kk1 + g * CW, c1 = Math.Min(nn, c0 + CW);
                        const int RT = 256;
                        for (int r0 = kk1; r0 < nn; r0 += RT)
                        {
                            int r1 = Math.Min(nn, r0 + RT);
                            if (simd) GemmAvx(a, ndim, kk0, kk1, r0, r1, c0, c1, buf);
                            else GemmScalare(a, ndim, kk0, kk1, r0, r1, c0, c1);
                        }
                        return buf;
                    }, buf => { });
                }
            }
        }

        // A[r0:r1, c0:c1] -= A[r0:r1, k0:k1] * A[k0:k1, c0:c1]
        // 1.0: lettura e scrittura di 4 double; .NET 8 ha Vector256.LoadUnsafe/StoreUnsafe, .NET 6 (Windows 7) no: stesso effetto con Unsafe
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<double> Ld256(ref double d0, nuint e)
        {
#if WINDOWS7
            return Unsafe.ReadUnaligned<Vector256<double>>(ref Unsafe.As<double, byte>(ref Unsafe.Add(ref d0, (nint)e)));
#else
            return Vector256.LoadUnsafe(ref d0, e);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void St256(Vector256<double> v, ref double d0, nuint e)
        {
#if WINDOWS7
            Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref Unsafe.Add(ref d0, (nint)e)), v);
#else
            v.StoreUnsafe(ref d0, e);
#endif
        }

        static void GemmScalare(Complex[] a, int ndim, int k0, int k1, int r0, int r1, int c0, int c1)
        {
            for (int c = c0; c < c1; c++)
            {
                long cc = (long)c * ndim;
                for (int j = k0; j < k1; j++)
                {
                    Complex u = a[j + cc];
                    if (u == Complex.Zero) continue;
                    long jc = (long)j * ndim;
                    for (int i = r0; i < r1; i++) a[i + cc] -= a[i + jc] * u;
                }
            }
        }

        // stesso conto con AVX2/FMA: un Vector256<double> contiene due complessi (re, im, re, im). Prodotto complesso l*u:
        // (lr ur - li ui, li ur + lr ui) = fmaddsub(l, ur, scambia(l) * ui), dove scambia(l) = (li, lr, li, lr) (Avx.Permute 0b0101)
        // e fmaddsub sottrae nelle posizioni pari e somma nelle dispari (istruzione vfmaddsub, Intel 64 SDM vol. 2).
        static void GemmAvx(Complex[] a, int ndim, int k0, int k1, int r0, int r1, int c0, int c1, Vector256<double>[] buf)
        {
            int nb = k1 - k0;
            ref double d0 = ref Unsafe.As<Complex, double>(ref MemoryMarshal.GetArrayDataReference(a));
            int c = c0;
            for (; c + 4 <= c1; c += 4)
            {
                for (int j = 0; j < nb; j++)
                    for (int q = 0; q < 4; q++)
                    {
                        Complex u = a[(k0 + j) + (long)(c + q) * ndim];
                        buf[(j * 4 + q) * 2] = Vector256.Create(u.Real);
                        buf[(j * 4 + q) * 2 + 1] = Vector256.Create(u.Imaginary);
                    }
                long o0 = (long)c * ndim, o1 = o0 + ndim, o2 = o1 + ndim, o3 = o2 + ndim;
                int i = r0;
                for (; i + 2 <= r1; i += 2)
                {
                    nuint e0 = (nuint)((i + o0) * 2), e1 = (nuint)((i + o1) * 2), e2 = (nuint)((i + o2) * 2), e3 = (nuint)((i + o3) * 2);
                    Vector256<double> s0 = Ld256(ref d0, e0), s1 = Ld256(ref d0, e1);
                    Vector256<double> s2 = Ld256(ref d0, e2), s3 = Ld256(ref d0, e3);
                    long lo = (i + (long)k0 * ndim) * 2, passo = (long)ndim * 2;
                    for (int j = 0; j < nb; j++, lo += passo)
                    {
                        Vector256<double> l = Ld256(ref d0, (nuint)lo);
                        Vector256<double> ls = Avx.Permute(l, 0b0101);
                        int b = j * 8;
                        s0 = Avx.Subtract(s0, Fma.MultiplyAddSubtract(l, buf[b], Avx.Multiply(ls, buf[b + 1])));
                        s1 = Avx.Subtract(s1, Fma.MultiplyAddSubtract(l, buf[b + 2], Avx.Multiply(ls, buf[b + 3])));
                        s2 = Avx.Subtract(s2, Fma.MultiplyAddSubtract(l, buf[b + 4], Avx.Multiply(ls, buf[b + 5])));
                        s3 = Avx.Subtract(s3, Fma.MultiplyAddSubtract(l, buf[b + 6], Avx.Multiply(ls, buf[b + 7])));
                    }
                    St256(s0, ref d0, e0); St256(s1, ref d0, e1); St256(s2, ref d0, e2); St256(s3, ref d0, e3);
                }
                if (i < r1) GemmScalare(a, ndim, k0, k1, i, r1, c, c + 4);
            }
            if (c < c1) GemmScalare(a, ndim, k0, k1, r0, r1, c, c1);
        }

        // soluzione di LU x = b per FactrBlocchi: scambi, poi L (diagonale 1) in avanti, poi U all'indietro
        static void SolveLuBlocchi(int nn, Complex[] a, int[] ip, Complex[] b, int ndim)
        {
            for (int i = 0; i < nn; i++)
            {
                int p = ip[i] - 1;
                if (p != i) { Complex t = b[i]; b[i] = b[p]; b[p] = t; }
            }
            for (int j = 0; j < nn; j++)
            {
                Complex bj = b[j];
                if (bj == Complex.Zero) continue;
                long jc = (long)j * ndim;
                for (int i = j + 1; i < nn; i++) b[i] -= a[i + jc] * bj;
            }
            for (int j = nn - 1; j >= 0; j--)
            {
                long jc = (long)j * ndim;
                Complex d = a[j + jc];
                b[j] = d == Complex.Zero ? Complex.Zero : b[j] / d;
                Complex bj = b[j];
                if (bj == Complex.Zero) continue;
                for (int i = 0; i < j; i++) b[i] -= a[i + jc] * bj;
            }
        }
    }
}
