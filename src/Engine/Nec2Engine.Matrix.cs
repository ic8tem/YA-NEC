// YA-NEC - matrice di interazione (cmset, cmww) e soluzione (factr, solve), traduzione fedele di matrix.c di nec2c. GPL-3.0.
using System;
using System.Numerics;

namespace YaNec.Engine
{
    public sealed partial class Nec2Engine
    {
        // matrice complessa della struttura (solo fili, senza simmetrie), riempimento trasposto come nec2c
        // copia di lavoro per un thread: stessi dati della geometria e della soluzione (sola lettura), variabili di appoggio proprie
        Nec2Engine Lavoratore()
        {
            Nec2Engine w = (Nec2Engine)MemberwiseClone();
            w.jco = new int[jco.Length]; w.ax = new double[ax.Length]; w.bx = new double[bx.Length]; w.cx = new double[cx.Length];
            // terreno di Sommerfeld (0.4.2, approvato da IC8TEM il 13/11/2026): coefficienti dell'interpolazione bicubica (intrp)
            // copiati insieme agli indici della regione gia' calcolata, cosi' ogni thread riusa o ricalcola i suoi. Le griglie
            // (grid) e le costanti restano in comune perche' durante il riempimento si leggono soltanto; le variabili di gwave
            // e incom sono numeri e il clone le copia.
            w.ip_a = (Complex[,])ip_a.Clone(); w.ip_b = (Complex[,])ip_b.Clone(); w.ip_c = (Complex[,])ip_c.Clone(); w.ip_d = (Complex[,])ip_d.Clone();
            return w;
        }

        // nObs: segmenti di osservazione riempiti (n = matrice intera; con le simmetrie solo la prima copia, Nec2Engine.Simmetria)
        void CmSet(Complex[] cm, double rkhx, int iexkx, int nObs = -1)
        {
            int nrow = n;
            if (nObs < 0) nObs = n;
            rkh = rkhx;
            iexk = iexkx;
            Array.Clear(cm, 0, cm.Length);
            // dalla 0.4.2 (approvato da IC8TEM il 13/11/2026): riempimento in parallelo, ogni thread su un blocco di segmenti di
            // osservazione (righe diverse della matrice, nessuna scrittura in comune) con la sua copia delle variabili di appoggio.
            // Tutti i terreni; con Sommerfeld ogni thread ha le sue tabelle di interpolazione (Lavoratore).
            int nt = Math.Min(Processori.Calcolo, nObs / 64);
            if (nt >= 2)
            {
                System.Threading.Tasks.Parallel.For(0, nt, t =>
                {
                    int i1 = (int)((long)t * nObs / nt) + 1, i2 = (int)((long)(t + 1) * nObs / nt);
                    if (i2 < i1) return;
                    Nec2Engine w = Lavoratore();
                    for (int j = 1; j <= n; j++)
                    {
                        w.Trio(j);
                        w.Cmww(j, i1, i2, cm, nrow, i1 - 1);
                    }
                });
                if (nload == 0) return;
                for (int j = 1; j <= nObs; j++)
                {
                    Complex zaj = zarray[j - 1];
                    if (zaj == Complex.Zero) continue;
                    Trio(j);
                    for (int i = 0; i < jsno; i++)
                    {
                        int jss = jco[i];
                        cm[(jss - 1) + (long)(j - 1) * nrow] -= (ax[i] + cx[i]) * zaj;
                    }
                }
                return;
            }
            for (int j = 1; j <= n; j++)
            {
                Trio(j);
                Cmww(j, 1, nObs, cm, nrow);
                // elementi modificati dai carichi (equazione del segmento j: solo per i segmenti di osservazione)
                if (nload == 0 || j > nObs) continue;
                Complex zaj = zarray[j - 1];
                if (zaj == Complex.Zero) continue;
                for (int i = 0; i < jsno; i++)
                {
                    int jss = jco[i];
                    cm[(jss - 1) + (long)(j - 1) * nrow] -= (ax[i] + cx[i]) * zaj;
                }
            }
        }

        // elementi della matrice per le interazioni filo-filo (riempimento trasposto, itrp = 1)
        // dati del segmento sorgente j (1-based) e tipo di estremi per il nucleo esteso: intestazione di cmww,
        // usata anche da qdsrc (sorgente EX 5)
        void SegmentoSorgente(int jx)
        {
            int ipr, iprx;
            double xi;
            int j = jx - 1;
            ds = si[j];
            db = bi[j];
            xj = x[j];
            yj = y[j];
            zj = z[j];
            cabj = cab[j];
            sabj = sab[j];
            salpj = salp[j];

            // si puo' usare l'approssimazione estesa?
            if (iexk != 0)
            {
                ipr = icon1[j];
                if (ipr > PCHCON) ind1 = 0;
                else if (ipr < 0)
                {
                    ipr = -ipr;
                    iprx = ipr - 1;
                    if (-icon1[iprx] != jx) ind1 = 2;
                    else
                    {
                        xi = Math.Abs(cabj * cab[iprx] + sabj * sab[iprx] + salpj * salp[iprx]);
                        if ((xi < 0.999999) || (Math.Abs(bi[iprx] / db - 1.0) > 1.0e-6)) ind1 = 2;
                        else ind1 = 0;
                    }
                }
                else
                {
                    iprx = ipr - 1;
                    if (ipr == 0) ind1 = 1;
                    else
                    {
                        if (ipr != jx)
                        {
                            if (icon2[iprx] != jx) ind1 = 2;
                            else
                            {
                                xi = Math.Abs(cabj * cab[iprx] + sabj * sab[iprx] + salpj * salp[iprx]);
                                if ((xi < 0.999999) || (Math.Abs(bi[iprx] / db - 1.0) > 1.0e-6)) ind1 = 2;
                                else ind1 = 0;
                            }
                        }
                        else if (cabj * cabj + sabj * sabj > 1.0e-8) ind1 = 2;
                        else ind1 = 0;
                    }
                }

                ipr = icon2[j];
                if (ipr > PCHCON) ind2 = 2;
                else if (ipr < 0)
                {
                    ipr = -ipr;
                    iprx = ipr - 1;
                    if (-icon2[iprx] != jx) ind2 = 2;
                    else
                    {
                        xi = Math.Abs(cabj * cab[iprx] + sabj * sab[iprx] + salpj * salp[iprx]);
                        if ((xi < 0.999999) || (Math.Abs(bi[iprx] / db - 1.0) > 1.0e-6)) ind2 = 2;
                        else ind2 = 0;
                    }
                }
                else
                {
                    iprx = ipr - 1;
                    if (ipr == 0) ind2 = 1;
                    else
                    {
                        if (ipr != jx)
                        {
                            if (icon1[iprx] != jx) ind2 = 2;
                            else
                            {
                                xi = Math.Abs(cabj * cab[iprx] + sabj * sab[iprx] + salpj * salp[iprx]);
                                if ((xi < 0.999999) || (Math.Abs(bi[iprx] / db - 1.0) > 1.0e-6)) ind2 = 2;
                                else ind2 = 0;
                            }
                        }
                        else if (cabj * cabj + sabj * sabj > 1.0e-8) ind2 = 2;
                        else ind2 = 0;
                    }
                }
            }

        }

        void Cmww(int j, int i1, int i2, Complex[] cm, int nr, int colBase = 0)
        {
            SegmentoSorgente(j);
            j--;
            int ipr;

            // ciclo di osservazione
            ipr = -1;
            for (int i = i1 - 1; i < i2; i++)
            {
                ipr++;
                int ij = i - j;
                Efld(x[i], y[i], z[i], GiunzioniNec4 ? db : bi[i], ij);   // 1.2.0: NEC-4, corrente sulla superficie della sorgente
                double cabi = cab[i], sabi = sab[i], salpi = salp[i];
                Complex etk = exk * cabi + eyk * sabi + ezk * salpi;
                Complex ets = exs * cabi + eys * sabi + ezs * salpi;
                Complex etc = exc * cabi + eyc * sabi + ezc * salpi;
                long col = (long)(ipr + colBase) * nr;   // colBase: primo segmento di osservazione del blocco (riempimento in parallelo)
                for (int k = 0; k < jsno; k++)
                {
                    int jxx = jco[k] - 1;
                    cm[jxx + col] += etk * ax[k] + ets * bx[k] + etc * cx[k];
                }
            }
        }

        // fattorizzazione LU (Ralston), matrice trasposta come in nec2c. Dalla 0.4.20 resta come "LU classica" (variabile d'ambiente
        // YANEC_LU=classica, e confronto nel collaudo): il calcolo normale usa FactrBlocchi in Nec2Engine.Lu.cs.
        void FactrColonne(int nn, Complex[] a, int[] ip, int ndim)
        {
            Complex[] scm = new Complex[nn];
            Complex arj;
            // la matrice viene "de-trasposta" per l'eliminazione di Gauss
            for (int i = 1; i < nn; i++)
                for (int j = 0; j < i; j++)
                {
                    long ij = i + (long)j * ndim, ji = j + (long)i * ndim;
                    arj = a[ij];
                    a[ij] = a[ji];
                    a[ji] = arj;
                }

            // 0.4.2 (idea di E. S. Harriman AE6TY, nec2c-ae6ty; approvata da IC8TEM il 13/11/2026): fattorizzazione a blocchi di
            // colonne. L'algoritmo di nec2c lavora colonna per colonna applicando a ognuna gli scambi e le eliminazioni delle colonne
            // gia' finite: per un blocco di colonne la parte che usa le colonne finite prima del blocco e' indipendente per colonna e
            // va in parallelo; dentro il blocco si prosegue in ordine. Ogni colonna fa le stesse operazioni nello stesso ordine:
            // risultato identico bit per bit a quello sequenziale.
            int nb = nn >= 256 && Processori.Calcolo > 1 ? Math.Max(16, Math.Min(64, nn / (2 * Processori.Calcolo))) : 1;
            Complex[][] col = new Complex[nb][];
            for (int k = 0; k < nb; k++) col[k] = k == 0 ? scm : new Complex[nn];
            for (int r0 = 0; r0 < nn; r0 += nb)
            {
                int r1 = Math.Min(nn, r0 + nb);
                Action<int> prima = c =>
                {
                    Complex[] sc = col[c - r0];
                    long cc = (long)c * ndim;
                    for (int k = 0; k < nn; k++) sc[k] = a[k + cc];
                    for (int j = 0; j < r0; j++)
                    {
                        int pj = ip[j] - 1;
                        Complex av = sc[pj];
                        a[j + cc] = av;
                        sc[pj] = sc[j];
                        long jc = (long)j * ndim;
                        for (int i = j + 1; i < nn; i++) sc[i] -= a[i + jc] * av;
                    }
                };
                if (r1 - r0 > 1 && r0 > 0) System.Threading.Tasks.Parallel.For(r0, r1, Processori.Opzioni(), prima);
                else for (int c = r0; c < r1; c++) prima(c);
                for (int r = r0; r < r1; r++)
                {
                    Complex[] sc = col[r - r0];
                    long rc = (long)r * ndim;
                    for (int j = r0; j < r; j++)
                    {
                        int pj = ip[j] - 1;
                        arj = sc[pj];
                        a[j + rc] = arj;
                        sc[pj] = sc[j];
                        long jc = (long)j * ndim;
                        for (int i = j + 1; i < nn; i++) sc[i] -= a[i + jc] * arj;
                    }

                    double dmax = (sc[r] * Complex.Conjugate(sc[r])).Real;
                    int rp1 = r + 1;
                    ip[r] = rp1;
                    for (int i = rp1; i < nn; i++)
                    {
                        double elmag = (sc[i] * Complex.Conjugate(sc[i])).Real;
                        if (elmag >= dmax)
                        {
                            dmax = elmag;
                            ip[r] = i + 1;
                        }
                    }
                    if (dmax < 1.0e-10 && PivotWarning == null)
                        PivotWarning = "Pivot molto piccolo alla riga " + r + " (" + dmax.ToString("E3") + "): matrice quasi singolare, controllare il modello.";

                    int pr = ip[r] - 1;
                    a[r + rc] = sc[pr];
                    sc[pr] = sc[r];
                    if (rp1 < nn)
                    {
                        arj = 1.0 / a[r + rc];
                        for (int i = rp1; i < nn; i++) a[i + rc] = sc[i] * arj;
                    }
                }
            }
        }

        // soluzione di LU x = b (b in ingresso, soluzione in uscita), per FactrColonne
        static void SolveLuColonne(int nn, Complex[] a, int[] ip, Complex[] b, int ndim)
        {
            Complex[] scm = new Complex[nn];
            for (int i = 0; i < nn; i++)
            {
                int pia = ip[i] - 1;
                scm[i] = b[pia];
                b[pia] = b[i];
                long ic = (long)i * ndim;
                for (int j = i + 1; j < nn; j++) b[j] -= a[j + ic] * scm[i];
            }
            for (int k = 0; k < nn; k++)
            {
                int i = nn - k - 1;
                Complex sum = Complex.Zero;
                for (int j = i + 1; j < nn; j++) sum += a[i + (long)j * ndim] * b[j];
                b[i] = (scm[i] - sum) / a[i + (long)i * ndim];
            }
        }
    }
}
