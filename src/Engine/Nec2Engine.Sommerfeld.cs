// YA-NEC - terreno reale con la soluzione di Sommerfeld (scheda GN 2), traduzione fedele di nec2c:
// somnec.c (somnec, bessel, evlua, fbar, gshank, hankel, lambda, rom1, saoa), ground.c (rom2, sflds),
// calculations.c (intrp, test) e fields.c (gwave, ramo iperf == 2 di efld).
// Origine: SOMNEC di G. J. Burke (LLNL), traduzione in C di N. Kyriazis 5B4AZ (nec2c). GPL-3.0.
// Le griglie di interpolazione sono calcolate al volo per ogni (frequenza, terreno), come fa nec2c senza il file SOM2D.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace YaNec.Engine
{
    public sealed partial class Nec2Engine
    {
        const double PTP = .6283185308, PI10 = 31.41592654, TOSP = 1.128379167, ACCS = 1.0E-12, SP = 1.772453851, GAMMA = .5772156649;
        const double C1 = -.0245785095,   // 1.5.9 (punto c): valore del Fortran del NEC-2 ((1 - 2 gamma) / 2 pi); nec2c -.02457850915
                      C2 = .3674669052, C3 = .7978845608;
        const double P10 = .0703125, P20 = .1121520996, Q10 = .125, Q20 = .0732421875, P11 = .1171875, P21 = .1441955566, Q11 = .375, Q21 = .1025390625;
        const double POF = .7853981635, CRIT = 1.0E-4;
        const int MAXH = 20, NM = 131072, NTS = 4;
        static readonly Complex CONST4 = new Complex(0.0, 188.365);
        static readonly Complex TPJ = new Complex(0.0, 6.283185308);
        static readonly Complex CPLX_01 = new Complex(0.0, 1.0);

        // ---- ggrid ----
        static readonly int[] nxa = { 11, 17, 9 }, nya = { 10, 5, 8 };
        static readonly double[] dxa = { .02, .05, .1 }, dya = { .1745329252, .0872664626, .1745329252 }, xsa = { 0.0, .2, .2 }, ysa = { 0.0, 0.0, .3490658504 };

        sealed class SomGrid { public int NonConvergenze; public Complex Epscf; public Complex[] Ar1 = new Complex[11 * 10 * 4], Ar2 = new Complex[17 * 5 * 4], Ar3 = new Complex[9 * 8 * 4]; }
        SomGrid grid;
        /// <summary>1.5.9: integrali di Sommerfeld che non sono andati a convergenza (si e' usata l'ultima stima).</summary>
        public int SommerfeldNonConvergenze { get { return grid != null ? grid.NonConvergenze : 0; } }
        // le griglie dipendono solo da frequenza e terreno: si riusano tra un calcolo e l'altro (sweep, ottimizzatore)
        static readonly Dictionary<string, SomGrid> gridCache = new Dictionary<string, SomGrid>();
        static readonly object gridLock = new object();

        // ---- gwav ----
        double gw_r1, gw_r2, gw_zmh, gw_zph;
        Complex gw_u, gw_u2, gw_xx1, gw_xx2;

        // ---- incom ----
        int inc_isnor;
        double inc_xo, inc_yo, inc_zo, inc_sn, inc_xsn, inc_ysn;

        /// <summary>Prepara (o riprende dalla memoria) le griglie di Sommerfeld per il terreno e la frequenza correnti.</summary>
        void PreparaSommerfeld(double epsr, double sig, double fmhz)
        {
            string key = epsr.ToString("R") + "|" + sig.ToString("R") + "|" + fmhz.ToString("R");
            lock (gridLock)
            {
                SomGrid g;
                if (!gridCache.TryGetValue(key, out g))
                {
                    g = new Somnec().Calcola(epsr, sig, fmhz);
                    if (gridCache.Count > 64) gridCache.Clear();
                    gridCache[key] = g;
                }
                grid = g;
            }
            ip_ixs = -10; ip_iys = -10; ip_igrs = -10; ip_ixeg = 0; ip_iyeg = 0;
            ip_dx = 1; ip_dy = 1; ip_xs = 0; ip_ys = 0;
        }

        // ============ ramo iperf == 2 di efld: campo dovuto al terreno (Sommerfeld/Norton) ============
        // 0.4.20 (registri della 0.4.19: 3,1 GB allocati per un calcolo della Butternut da 1453 segmenti, 64 GB nella fase 2D):
        // i vettori di appoggio di efld e rom2 erano creati a ogni coppia di segmenti. Ora ogni thread ha i suoi, creati una volta
        // sola ([ThreadStatic]); i valori sono sempre scritti prima di essere letti, quindi il risultato non cambia.
        [ThreadStatic] static Complex[] tsEgnd, tsG1, tsG2, tsG3, tsG4, tsG5, tsT01, tsT10, tsT20;

        void EfldSommerfeld(double xi, double yi, double zi, double ai, double xij, double yij)
        {
            Complex[] egnd = tsEgnd ?? (tsEgnd = new Complex[9]);
            inc_sn = Math.Sqrt(cabj * cabj + sabj * sabj);
            if (inc_sn >= 1.0e-5)
            {
                inc_xsn = cabj / inc_sn;
                inc_ysn = sabj / inc_sn;
            }
            else
            {
                inc_sn = 0.0; inc_xsn = 1.0; inc_ysn = 0.0;
            }

            // sposta il punto di osservazione per l'approssimazione del filo sottile
            double zij = zi + zj;
            double salpr = -salpj;
            double rhox = sabj * zij - salpr * yij;
            double rhoy = salpr * xij - cabj * zij;
            double rhoz = cabj * yij - sabj * xij;
            double rh = rhox * rhox + rhoy * rhoy + rhoz * rhoz;
            if (rh <= 1.0e-10)
            {
                inc_xo = xi - ai * inc_ysn;
                inc_yo = yi + ai * inc_xsn;
                inc_zo = zi;
            }
            else
            {
                rh = ai / Math.Sqrt(rh);
                if (rhoz < 0.0) rh = -rh;
                inc_xo = xi + rh * rhox;
                inc_yo = yi + rh * rhoy;
                inc_zo = zi + rh * rhoz;
            }

            double r = xij * xij + yij * yij + zij * zij;
            double dmin;
            if (r <= .95)
            {
                // campo dalle tabelle interpolate, integrato sul segmento
                inc_isnor = 1;
                dmin = (exk * Complex.Conjugate(exk)).Real + (eyk * Complex.Conjugate(eyk)).Real + (ezk * Complex.Conjugate(ezk)).Real;
                dmin = .01 * Math.Sqrt(dmin);
                double shaf = .5 * ds;
                Rom2(-shaf, shaf, egnd, dmin);
            }
            else
            {
                // formule di Norton ed elemento di corrente concentrato
                inc_isnor = 2;
                Sflds(0.0, egnd);
            }

            Complex txk = egnd[0], tyk = egnd[1], tzk = egnd[2];
            Complex txs = egnd[3], tys = egnd[4], tzs = egnd[5];
            Complex txc = egnd[6], tyc = egnd[7], tzc = egnd[8];

            if (r > .95)
            {
                double zp = xij * cabj + yij * sabj + zij * salpr;
                rh = r - zp * zp;
                if (rh <= 1.0e-10) dmin = 0.0;
                else dmin = Math.Sqrt(rh / (rh + ai * ai));
                if (dmin <= .95)
                {
                    double px = 1.0 - dmin;
                    Complex terk = (txk * cabj + tyk * sabj + tzk * salpr) * px;
                    txk = dmin * txk + terk * cabj;
                    tyk = dmin * tyk + terk * sabj;
                    tzk = dmin * tzk + terk * salpr;
                    Complex ters = (txs * cabj + tys * sabj + tzs * salpr) * px;
                    txs = dmin * txs + ters * cabj;
                    tys = dmin * tys + ters * sabj;
                    tzs = dmin * tzs + ters * salpr;
                    Complex terc = (txc * cabj + tyc * sabj + tzc * salpr) * px;
                    txc = dmin * txc + terc * cabj;
                    tyc = dmin * tyc + terc * sabj;
                    tzc = dmin * tzc + terc * salpr;
                }
            }

            exk += txk; eyk += tyk; ezk += tzk;
            exs += txs; eys += tys; ezs += tzs;
            exc += txc; eyc += tyc; ezc += tzc;
        }

        // ============ ground.c: rom2 ============
        void Rom2(double a, double b, Complex[] sum, double dmin)
        {
            int i, ns, nt;
            bool flag = true;
            const int nts = 4, nx = 1, nn = 9;
            double dz = 0.0, dzot = 0.0, tmag1, tmag2, tr, ti;
            const double rx = 1.0e-4;
            Complex[] g1 = tsG1 ?? (tsG1 = new Complex[9]), g2 = tsG2 ?? (tsG2 = new Complex[9]), g3 = tsG3 ?? (tsG3 = new Complex[9]);
            Complex[] g4 = tsG4 ?? (tsG4 = new Complex[9]), g5 = tsG5 ?? (tsG5 = new Complex[9]);
            Complex[] t01 = tsT01 ?? (tsT01 = new Complex[9]), t10 = tsT10 ?? (tsT10 = new Complex[9]), t20 = tsT20 ?? (tsT20 = new Complex[9]);
            Complex t00, t02, t11;
            int npm = n;

            double zz = a;
            double ze = b;
            double s = b - a;
            if (s < 0.0) throw new NecException("Errore interno ROM2: B minore di A.");
            double ep = s / (1.0e4 * npm);
            double zend = ze - ep;
            for (i = 0; i < nn; i++) sum[i] = Complex.Zero;
            ns = nx;
            nt = 0;
            Sflds(zz, g1);

            while (true)
            {
                if (flag)
                {
                    dz = s / ns;
                    if (zz + dz > ze)
                    {
                        dz = ze - zz;
                        if (dz <= ep) return;
                    }
                    dzot = dz * .5;
                    Sflds(zz + dzot, g3);
                    Sflds(zz + dz, g5);
                }

                tmag1 = 0.0;
                tmag2 = 0.0;
                // risultato di Romberg a 3 punti e prova di convergenza
                for (i = 0; i < nn; i++)
                {
                    t00 = (g1[i] + g5[i]) * dzot;
                    t01[i] = (t00 + dz * g3[i]) * .5;
                    t10[i] = (4.0 * t01[i] - t00) / 3.0;
                    if (i > 2) continue;
                    tr = t01[i].Real; ti = t01[i].Imaginary;
                    tmag1 += tr * tr + ti * ti;
                    tr = t10[i].Real; ti = t10[i].Imaginary;
                    tmag2 += tr * tr + ti * ti;
                }
                tmag1 = Math.Sqrt(tmag1);
                tmag2 = Math.Sqrt(tmag2);
                Test(tmag1, tmag2, out tr, 0.0, 0.0, out ti, dmin);

                if (tr <= rx)
                {
                    for (i = 0; i < nn; i++) sum[i] += t10[i];
                    nt += 2;
                    zz += dz;
                    if (zz > zend) return;
                    for (i = 0; i < nn; i++) g1[i] = g5[i];
                    if (nt >= nts && ns > nx)
                    {
                        ns = ns / 2;
                        nt = 1;
                    }
                    flag = true;
                    continue;
                }

                Sflds(zz + dz * .25, g2);
                Sflds(zz + dz * .75, g4);
                tmag1 = 0.0;
                tmag2 = 0.0;
                // risultato di Romberg a 5 punti
                for (i = 0; i < nn; i++)
                {
                    t02 = (t01[i] + dzot * (g2[i] + g4[i])) * .5;
                    t11 = (4.0 * t02 - t01[i]) / 3.0;
                    t20[i] = (16.0 * t11 - t10[i]) / 15.0;
                    if (i > 2) continue;
                    tr = t11.Real; ti = t11.Imaginary;
                    tmag1 += tr * tr + ti * ti;
                    tr = t20[i].Real; ti = t20[i].Imaginary;
                    tmag2 += tr * tr + ti * ti;
                }
                tmag1 = Math.Sqrt(tmag1);
                tmag2 = Math.Sqrt(tmag2);
                Test(tmag1, tmag2, out tr, 0.0, 0.0, out ti, dmin);

                if (tr > rx)
                {
                    nt = 0;
                    if (ns < npm)
                    {
                        ns = ns * 2;
                        dz = s / ns;
                        dzot = dz * .5;
                        for (i = 0; i < nn; i++)
                        {
                            g5[i] = g3[i];
                            g3[i] = g2[i];
                        }
                        flag = false;
                        continue;
                    }
                    // nec2c qui scrive "ROM2 -- STEP SIZE LIMITED" e prosegue
                }

                for (i = 0; i < nn; i++) sum[i] += t20[i];
                nt = nt + 1;
                zz = zz + dz;
                if (zz > zend) return;
                for (i = 0; i < nn; i++) g1[i] = g5[i];
                flag = true;
                if (nt < nts || ns <= nx) continue;
                ns = ns / 2;
                nt = 1;
            }
        }

        // ============ ground.c: sflds ============
        void Sflds(double t, Complex[] e)
        {
            double xt = xj + t * cabj;
            double yt = yj + t * sabj;
            double zt = zj + t * salpj;
            double rhx = inc_xo - xt;
            double rhy = inc_yo - yt;
            double rhs = rhx * rhx + rhy * rhy;
            double rho = Math.Sqrt(rhs);
            double phx, phy;
            if (rho <= 0.0)
            {
                rhx = 1.0; rhy = 0.0; phx = 0.0; phy = 1.0;
            }
            else
            {
                rhx = rhx / rho; rhy = rhy / rho;
                phx = -rhy; phy = rhx;
            }

            double cph = rhx * inc_xsn + rhy * inc_ysn;
            double sph = rhy * inc_xsn - rhx * inc_ysn;
            if (Math.Abs(cph) < 1.0e-10) cph = 0.0;
            if (Math.Abs(sph) < 1.0e-10) sph = 0.0;

            gw_zph = inc_zo + zt;
            double zphs = gw_zph * gw_zph;
            double r2s = rhs + zphs;
            gw_r2 = Math.Sqrt(r2s);
            double rk = gw_r2 * TP;
            gw_xx2 = new Complex(Math.Cos(rk), -Math.Sin(rk));
            Complex erv, ezv, erh, ezh, eph, er, et, hrv, hzv, hrh;
            double sfac;

            if (inc_isnor != 1)
            {
                // approssimazione di Norton: corrente concentrata al centro del segmento
                gw_zmh = 1.0;
                gw_r1 = 1.0;
                gw_xx1 = Complex.Zero;
                Gwave(out erv, out ezv, out erh, out ezh, out eph);

                et = -CONST1 * frati * gw_xx2 / (r2s * gw_r2);
                er = 2.0 * et * new Complex(1.0, rk);
                et = et * new Complex(1.0 - rk * rk, rk);
                hrv = (er + et) * rho * gw_zph / r2s;
                hzv = (zphs * er - rhs * et) / r2s;
                hrh = (rhs * er - zphs * et) / r2s;
                erv = erv - hrv;
                ezv = ezv - hzv;
                erh = erh + hrh;
                ezh = ezh + hrv;
                eph = eph + et;
                erv = erv * salpj;
                ezv = ezv * salpj;
                erh = erh * inc_sn * cph;
                ezh = ezh * inc_sn * cph;
                eph = eph * inc_sn * sph;
                erh = erv + erh;
                e[0] = (erh * rhx + eph * phx) * ds;
                e[1] = (erh * rhy + eph * phy) * ds;
                e[2] = (ezv + ezh) * ds;
                e[3] = Complex.Zero;
                e[4] = Complex.Zero;
                e[5] = Complex.Zero;
                sfac = PI * ds;
                sfac = Math.Sin(sfac) / sfac;
                e[6] = e[0] * sfac;
                e[7] = e[1] * sfac;
                e[8] = e[2] * sfac;
                return;
            }

            // interpolazione nelle tabelle di Sommerfeld
            double thet;
            if (rho >= 1.0e-12) thet = Math.Atan(gw_zph / rho);
            else thet = POT;
            Intrp(gw_r2, thet, out erv, out ezv, out erh, out eph);
            gw_xx2 = gw_xx2 / gw_r2;
            sfac = inc_sn * cph;
            erh = gw_xx2 * (salpj * erv + sfac * erh);
            ezh = gw_xx2 * (salpj * ezv - sfac * erv);
            eph = inc_sn * sph * gw_xx2 * eph;
            // corrente costante
            e[0] = erh * rhx + eph * phx;
            e[1] = erh * rhy + eph * phy;
            e[2] = ezh;
            // corrente seno
            rk = TP * t;
            sfac = Math.Sin(rk);
            e[3] = e[0] * sfac;
            e[4] = e[1] * sfac;
            e[5] = e[2] * sfac;
            // corrente coseno
            sfac = Math.Cos(rk);
            e[6] = e[0] * sfac;
            e[7] = e[1] * sfac;
            e[8] = e[2] * sfac;
        }

        // Test (calculations.c) e' gia' in Nec2Engine.cs e serve anche a questo file.

        // ============ calculations.c: intrp (interpolazione cubica bivariata) ============
        int ip_ix, ip_iy, ip_ixs = -10, ip_iys = -10, ip_igrs = -10, ip_ixeg = 0, ip_iyeg = 0;
        int ip_nxm2, ip_nym2, ip_nxms, ip_nyms, ip_nd, ip_ndp;
        double ip_dx = 1.0, ip_dy = 1.0, ip_xs = 0.0, ip_ys = 0.0, ip_xz, ip_yz;
        Complex[,] ip_a = new Complex[4, 4], ip_b = new Complex[4, 4], ip_c = new Complex[4, 4], ip_d = new Complex[4, 4];
        static readonly int[] nda = { 11, 17, 9 }, ndpa = { 110, 85, 72 };

        void Intrp(double xx0, double yy0, out Complex f1, out Complex f2, out Complex f3, out Complex f4)
        {
            Complex p1 = Complex.Zero, p2 = Complex.Zero, p3 = Complex.Zero, p4 = Complex.Zero;
            bool jump = false;
            if (xx0 < ip_xs || yy0 < ip_ys) jump = true;
            else
            {
                ip_ix = (int)((xx0 - ip_xs) / ip_dx) + 1;
                ip_iy = (int)((yy0 - ip_ys) / ip_dy) + 1;
            }

            // se il punto cade nella stessa regione 4x4 del precedente si riusano i coefficienti
            if (ip_ix < ip_ixeg || ip_iy < ip_iyeg || Math.Abs(ip_ix - ip_ixs) >= 2 || Math.Abs(ip_iy - ip_iys) >= 2 || jump)
            {
                int igr;
                if (xx0 <= xsa[1]) igr = 0;
                else igr = yy0 > ysa[2] ? 2 : 1;

                if (igr != ip_igrs)
                {
                    ip_igrs = igr;
                    ip_dx = dxa[ip_igrs];
                    ip_dy = dya[ip_igrs];
                    ip_xs = xsa[ip_igrs];
                    ip_ys = ysa[ip_igrs];
                    ip_nxm2 = nxa[ip_igrs] - 2;
                    ip_nym2 = nya[ip_igrs] - 2;
                    ip_nxms = ((ip_nxm2 + 1) / 3) * 3 + 1;
                    ip_nyms = ((ip_nym2 + 1) / 3) * 3 + 1;
                    ip_nd = nda[ip_igrs];
                    ip_ndp = ndpa[ip_igrs];
                    ip_ix = (int)((xx0 - ip_xs) / ip_dx) + 1;
                    ip_iy = (int)((yy0 - ip_ys) / ip_dy) + 1;
                }

                ip_ixs = ((ip_ix - 1) / 3) * 3 + 2;
                if (ip_ixs < 2) ip_ixs = 2;
                ip_ixeg = -10000;
                if (ip_ixs > ip_nxm2)
                {
                    ip_ixs = ip_nxm2;
                    ip_ixeg = ip_nxms;
                }
                ip_iys = ((ip_iy - 1) / 3) * 3 + 2;
                if (ip_iys < 2) ip_iys = 2;
                ip_iyeg = -10000;
                if (ip_iys > ip_nym2)
                {
                    ip_iys = ip_nym2;
                    ip_iyeg = ip_nyms;
                }

                // coefficienti dei 4 polinomi cubici in x per i 4 valori di griglia in y di ciascuna funzione
                Complex[] ar = ip_igrs == 0 ? grid.Ar1 : (ip_igrs == 1 ? grid.Ar2 : grid.Ar3);
                int iadz = ip_ixs + (ip_iys - 3) * ip_nd - ip_ndp;
                for (int k = 0; k < 4; k++)
                {
                    iadz += ip_ndp;
                    int iadd = iadz;
                    for (int i = 0; i < 4; i++)
                    {
                        iadd += ip_nd;
                        p1 = ar[iadd - 2];
                        p2 = ar[iadd - 1];
                        p3 = ar[iadd];
                        p4 = ar[iadd + 1];
                        ip_a[i, k] = (p4 - p1 + 3.0 * (p2 - p3)) * .1666666667;
                        ip_b[i, k] = (p1 - 2.0 * p2 + p3) * .5;
                        ip_c[i, k] = p3 - (2.0 * p1 + 3.0 * p2 + p4) * .1666666667;
                        ip_d[i, k] = p2;
                    }
                }
                ip_xz = (ip_ixs - 1) * ip_dx + ip_xs;
                ip_yz = (ip_iys - 1) * ip_dy + ip_ys;
            }

            // polinomi in x e interpolazione cubica in y per ciascuna delle 4 funzioni
            double xx = (xx0 - ip_xz) / ip_dx;
            double yy = (yy0 - ip_yz) / ip_dy;
            f1 = IntrpF(0, xx, yy);
            f2 = IntrpF(1, xx, yy);
            f3 = IntrpF(2, xx, yy);
            f4 = IntrpF(3, xx, yy);
        }

        Complex IntrpF(int k, double xx, double yy)
        {
            Complex fx1 = ((ip_a[0, k] * xx + ip_b[0, k]) * xx + ip_c[0, k]) * xx + ip_d[0, k];
            Complex fx2 = ((ip_a[1, k] * xx + ip_b[1, k]) * xx + ip_c[1, k]) * xx + ip_d[1, k];
            Complex fx3 = ((ip_a[2, k] * xx + ip_b[2, k]) * xx + ip_c[2, k]) * xx + ip_d[2, k];
            Complex fx4 = ((ip_a[3, k] * xx + ip_b[3, k]) * xx + ip_c[3, k]) * xx + ip_d[3, k];
            Complex p1 = fx4 - fx1 + 3.0 * (fx2 - fx3);
            Complex p2 = 3.0 * (fx1 - 2.0 * fx2 + fx3);
            Complex p3 = 6.0 * fx3 - 2.0 * fx1 - 3.0 * fx2 - fx4;
            return ((p1 * yy + p2) * yy + p3) * yy * .1666666667 + fx2;
        }

        // ============ fields.c: gwave (onda di terra di Norton) ============
        void Gwave(out Complex erv, out Complex ezv, out Complex erh, out Complex ezh, out Complex eph)
        {
            double sppp = gw_zmh / gw_r1;
            double sppp2 = sppp * sppp;
            double cppp2 = 1.0 - sppp2;
            if (cppp2 < 1.0e-20) cppp2 = 1.0e-20;
            double cppp = Math.Sqrt(cppp2);
            double spp = gw_zph / gw_r2;
            double spp2 = spp * spp;
            double cpp2 = 1.0 - spp2;
            if (cpp2 < 1.0e-20) cpp2 = 1.0e-20;
            double cpp = Math.Sqrt(cpp2);
            Complex rk1 = -TPJ * gw_r1;
            Complex rk2 = -TPJ * gw_r2;
            Complex t1 = 1.0 - gw_u2 * cpp2;
            Complex t2 = Complex.Sqrt(t1);
            Complex t3 = (1.0 - 1.0 / rk1) / rk1;
            Complex t4 = (1.0 - 1.0 / rk2) / rk2;
            Complex p1 = rk2 * gw_u2 * t1 / (2.0 * cpp2);
            Complex rv = (spp - gw_u * t2) / (spp + gw_u * t2);
            Complex omr = 1.0 - rv;
            Complex w = 1.0 / omr;
            w = 4.0 * p1 * w * w;
            Complex f = Fbar(w);
            Complex q1 = rk2 * t1 / (2.0 * gw_u2 * cpp2);
            Complex rh = (t2 - gw_u * spp) / (t2 + gw_u * spp);
            Complex v = 1.0 / (1.0 + rh);
            v = 4.0 * q1 * v * v;
            Complex g = Fbar(v);
            Complex xr1 = gw_xx1 / gw_r1;
            Complex xr2 = gw_xx2 / gw_r2;
            Complex x1 = cppp2 * xr1;
            Complex x2 = rv * cpp2 * xr2;
            Complex x3 = omr * cpp2 * f * xr2;
            Complex x4 = gw_u * t2 * spp * 2.0 * xr2 / rk2;
            Complex x5 = xr1 * t3 * (1.0 - 3.0 * sppp2);
            Complex x6 = xr2 * t4 * (1.0 - 3.0 * spp2);
            Complex x7;
            ezv = (x1 + x2 + x3 - x4 - x5 - x6) * (-CONST4);
            x1 = sppp * cppp * xr1;
            x2 = rv * spp * cpp * xr2;
            x3 = cpp * omr * gw_u * t2 * f * xr2;
            x4 = spp * cpp * omr * xr2 / rk2;
            x5 = 3.0 * sppp * cppp * t3 * xr1;
            x6 = cpp * gw_u * t2 * omr * xr2 / rk2 * .5;
            x7 = 3.0 * spp * cpp * t4 * xr2;
            erv = -(x1 + x2 - x3 + x4 - x5 + x6 - x7) * (-CONST4);
            ezh = -(x1 - x2 + x3 - x4 - x5 - x6 + x7) * (-CONST4);
            x1 = sppp2 * xr1;
            x2 = rv * spp2 * xr2;
            x4 = gw_u2 * t1 * omr * f * xr2;
            x5 = t3 * (1.0 - 3.0 * cppp2) * xr1;
            x6 = t4 * (1.0 - 3.0 * cpp2) * (1.0 - gw_u2 * (1.0 + rv) - gw_u2 * omr * f) * xr2;
            x7 = gw_u2 * cpp2 * omr * (1.0 - 1.0 / rk2) * (f * (gw_u2 * t1 - spp2 - 1.0 / rk2) + 1.0 / rk2) * xr2;
            erh = (x1 - x2 - x4 - x5 + x6 + x7) * (-CONST4);
            x1 = xr1;
            x2 = rh * xr2;
            x3 = (rh + 1.0) * g * xr2;
            x4 = t3 * xr1;
            x5 = t4 * (1.0 - gw_u2 * (1.0 + rv) - gw_u2 * omr * f) * xr2;
            x6 = .5 * gw_u2 * omr * (f * (gw_u2 * t1 - spp2 - 1.0 / rk2) + 1.0 / rk2) * xr2 / rk2;
            eph = -(x1 - x2 + x3 - x4 + x5 + x6) * (-CONST4);
        }

        // ============ somnec.c: fbar (funzione di attenuazione di Sommerfeld) ============
        static Complex Fbar(Complex p)
        {
            Complex zz = CPLX_01 * Complex.Sqrt(p);
            Complex zs, sum, pow, term;
            if (Complex.Abs(zz) <= 3.0)
            {
                // sviluppo in serie
                zs = zz * zz;
                sum = zz;
                pow = zz;
                for (int i = 1; i <= 100; i++)
                {
                    pow = -pow * zs / (double)i;
                    term = pow / (2.0 * i + 1.0);
                    sum = sum + term;
                    double tms = (term * Complex.Conjugate(term)).Real;
                    double sms = (sum * Complex.Conjugate(sum)).Real;
                    if (tms / sms < ACCS) break;
                }
                return 1.0 - (1.0 - sum * TOSP) * zz * Complex.Exp(zs) * SP;
            }
            // sviluppo asintotico
            bool minus = false;
            if (zz.Real < 0.0)
            {
                minus = true;
                zz = -zz;
            }
            zs = .5 / (zz * zz);
            sum = Complex.Zero;
            term = Complex.One;
            for (int i = 1; i <= 6; i++)
            {
                term = -term * (2.0 * i - 1.0) * zs;
                sum += term;
            }
            if (minus) sum -= 2.0 * SP * zz * Complex.Exp(zz * zz);
            return -sum;
        }

        // ============ somnec.c: generazione delle griglie (integrali di Sommerfeld) ============
        sealed class Somnec
        {
            int jh;
            int nonConv;   // 1.5.9: integrali di GSHANK non convergenti in MAXH passi (si usa l'ultima stima, come il Fortran)
            double ck2, ck2sq, tkmag, tsmag, ck1r, zph, rho;
            Complex ct1, ct2, ct3, ck1, ck1sq, cksm;
            Complex a, b;   // cntour

            public SomGrid Calcola(double epr, double sig, double fmhz)
            {
                SomGrid g = new SomGrid();
                if (sig >= 0.0)
                {
                    double wl = CVEL / fmhz;
                    g.Epscf = new Complex(epr, -sig * wl * 59.96);
                }
                else g.Epscf = new Complex(epr, sig);

                ck2 = TP;
                ck2sq = ck2 * ck2;
                // gli integrali usano exp(-jwt), NEC usa exp(+jwt): serve il coniugato di epscf (i campi si coniugano in evlua)
                ck1sq = ck2sq * Complex.Conjugate(g.Epscf);
                ck1 = Complex.Sqrt(ck1sq);
                ck1r = ck1.Real;
                tkmag = 100.0 * Complex.Abs(ck1);
                tsmag = 100.0 * (ck1 * Complex.Conjugate(ck1)).Real;
                cksm = ck2sq / (ck1sq + ck2sq);
                ct1 = .5 * (ck1sq - ck2sq);
                Complex erv = ck1sq * ck1sq;
                Complex ezv = ck2sq * ck2sq;
                ct2 = .125 * (erv - ezv);
                erv *= ck1sq;
                ezv *= ck2sq;
                ct3 = .0625 * (erv - ezv);
                Complex erh, eph;
                double dth = 0.0;

                // le 3 regioni della griglia
                for (int k = 0; k < 3; k++)
                {
                    int nr = nxa[k];
                    int nth = nya[k];
                    double dr = dxa[k];
                    dth = dya[k];
                    double r = xsa[k] - dr;
                    int irs = 1;
                    if (k == 0)
                    {
                        r = xsa[k];
                        irs = 2;
                    }
                    // r = sqrt(rho^2 + (z+h)^2)
                    for (int ir = irs - 1; ir < nr; ir++)
                    {
                        r += dr;
                        double thet = ysa[k] - dth;
                        // theta = atan((z+h)/rho)
                        for (int ith = 0; ith < nth; ith++)
                        {
                            thet += dth;
                            rho = r * Math.Cos(thet);
                            zph = r * Math.Sin(thet);
                            if (rho < 1.0e-7) rho = 1.0e-8;
                            if (zph < 1.0e-7) zph = 0.0;
                            Evlua(out erv, out ezv, out erh, out eph);
                            double rk = ck2 * r;
                            Complex con = -CONST1 * r / new Complex(Math.Cos(rk), -Math.Sin(rk));
                            switch (k)
                            {
                                case 0:
                                    g.Ar1[ir + ith * 11 + 0] = erv * con;
                                    g.Ar1[ir + ith * 11 + 110] = ezv * con;
                                    g.Ar1[ir + ith * 11 + 220] = erh * con;
                                    g.Ar1[ir + ith * 11 + 330] = eph * con;
                                    break;
                                case 1:
                                    g.Ar2[ir + ith * 17 + 0] = erv * con;
                                    g.Ar2[ir + ith * 17 + 85] = ezv * con;
                                    g.Ar2[ir + ith * 17 + 170] = erh * con;
                                    g.Ar2[ir + ith * 17 + 255] = eph * con;
                                    break;
                                default:
                                    g.Ar3[ir + ith * 9 + 0] = erv * con;
                                    g.Ar3[ir + ith * 9 + 72] = ezv * con;
                                    g.Ar3[ir + ith * 9 + 144] = erh * con;
                                    g.Ar3[ir + ith * 9 + 216] = eph * con;
                                    break;
                            }
                        }
                    }
                }

                // griglia 1 per r = 0
                Complex cl2 = -CONST4 * (g.Epscf - 1.0) / (g.Epscf + 1.0);
                Complex cl1 = cl2 / (g.Epscf + 1.0);
                ezv = g.Epscf * cl1;
                double th = -dth;
                int nth0 = nya[0];
                for (int ith = 0; ith < nth0; ith++)
                {
                    th += dth;
                    if (ith + 1 != nth0)
                    {
                        double tfac2 = Math.Cos(th);
                        double tfac1 = (1.0 - Math.Sin(th)) / tfac2;
                        tfac2 = tfac1 / tfac2;
                        erv = g.Epscf * cl1 * tfac1;
                        erh = cl1 * (tfac2 - 1.0) + cl2;
                        eph = cl1 * tfac2 - cl2;
                    }
                    else
                    {
                        erv = Complex.Zero;
                        erh = cl2 - .5 * cl1;
                        eph = -erh;
                    }
                    g.Ar1[0 + ith * 11 + 0] = erv;
                    g.Ar1[0 + ith * 11 + 110] = ezv;
                    g.Ar1[0 + ith * 11 + 220] = erh;
                    g.Ar1[0 + ith * 11 + 330] = eph;
                }
                g.NonConvergenze = nonConv;
                return g;
            }

            // ---- bessel: J0 e derivata per argomento complesso ----
            static readonly int[] bes_m = new int[101];
            static readonly double[] bes_a1 = new double[25], bes_a2 = new double[25];
            static readonly int[] han_m = new int[101];
            static readonly double[] han_a1 = new double[25], han_a2 = new double[25], han_a3 = new double[25], han_a4 = new double[25];

            static Somnec()
            {
                // inizializzazione come in nec2c (compresa la variabile "init" riusata come contatore)
                for (int k = 1; k <= 25; k++)
                {
                    int i = k - 1;
                    bes_a1[i] = -.25 / (k * k);
                    bes_a2[i] = 1.0 / (k + 1.0);
                }
                for (int i = 1; i <= 101; i++)
                {
                    double tst = 1.0;
                    int init = 0;
                    for (int k = 0; k < 24; k++)
                    {
                        init = k;
                        tst *= -i * bes_a1[k];
                        if (tst < 1.0e-6) break;
                    }
                    bes_m[i - 1] = init + 1;
                }

                double psi = -GAMMA;
                for (int k = 1; k <= 25; k++)
                {
                    int i = k - 1;
                    han_a1[i] = -.25 / (k * k);
                    han_a2[i] = 1.0 / (k + 1.0);
                    psi += 1.0 / k;
                    han_a3[i] = psi + psi;
                    han_a4[i] = (psi + psi + 1.0 / (k + 1.0)) / (k + 1.0);
                }
                for (int i = 1; i <= 101; i++)
                {
                    double tst = 1.0;
                    int init = 0;
                    for (int k = 0; k < 24; k++)
                    {
                        init = k;
                        tst *= -i * han_a1[k];
                        if (tst * han_a3[k] < 1.0e-6) break;
                    }
                    han_m[i - 1] = init + 1;
                }
            }

            static void Bessel(Complex zz, out Complex j0, out Complex j0p)
            {
                Complex j0x = Complex.Zero, j0px = Complex.Zero;
                double zms = (zz * Complex.Conjugate(zz)).Real;
                if (zms <= 1.0e-12)
                {
                    j0 = Complex.One;
                    j0p = -.5 * zz;
                    return;
                }
                int ib = 0;
                j0 = Complex.One; j0p = Complex.One;
                if (zms <= 37.21)
                {
                    if (zms > 36.0) ib = 1;
                    // serie
                    int iz = (int)zms;
                    int miz = bes_m[iz];
                    j0 = Complex.One;
                    j0p = j0;
                    Complex zk = j0;
                    Complex zi0 = zz * zz;
                    for (int k = 0; k < miz; k++)
                    {
                        zk *= bes_a1[k] * zi0;
                        j0 += zk;
                        j0p += bes_a2[k] * zk;
                    }
                    j0p *= -.5 * zz;
                    if (ib == 0) return;
                    j0x = j0;
                    j0px = j0p;
                }
                // sviluppo asintotico
                Complex zi = 1.0 / zz;
                Complex zi2 = zi * zi;
                Complex p0z = 1.0 + (P20 * zi2 - P10) * zi2;
                Complex p1z = 1.0 + (P11 - P21 * zi2) * zi2;
                Complex q0z = (Q20 * zi2 - Q10) * zi;
                Complex q1z = (Q11 - Q21 * zi2) * zi;
                Complex zk2 = Complex.Exp(CPLX_01 * (zz - POF));
                zi2 = 1.0 / zk2;
                Complex cz = .5 * (zk2 + zi2);
                Complex sz = CPLX_01 * .5 * (zi2 - zk2);
                zk2 = C3 * Complex.Sqrt(zi);
                j0 = zk2 * (p0z * cz - q0z * sz);
                j0p = -zk2 * (p1z * sz + q1z * cz);
                if (ib == 0) return;
                zms = Math.Cos((Math.Sqrt(zms) - 6.0) * PI10);
                j0 = .5 * (j0x * (1.0 + zms) + j0 * (1.0 - zms));
                j0p = .5 * (j0px * (1.0 + zms) + j0p * (1.0 - zms));
            }

            static void Hankel(Complex zz, out Complex h0, out Complex h0p)
            {
                Complex y0 = Complex.Zero, y0p = Complex.Zero;
                double zms = (zz * Complex.Conjugate(zz)).Real;
                if (zms == 0.0) throw new NecException("Errore interno di Sommerfeld (HANKEL con argomento nullo).");
                int ib = 0;
                h0 = Complex.Zero; h0p = Complex.Zero;
                if (zms <= 16.81)
                {
                    if (zms > 16.0) ib = 1;
                    int iz = (int)zms;
                    int miz = han_m[iz];
                    Complex j0 = Complex.One;
                    Complex j0p = j0;
                    y0 = Complex.Zero;
                    y0p = y0;
                    Complex zk = j0;
                    Complex zi0 = zz * zz;
                    for (int k = 0; k < miz; k++)
                    {
                        zk *= han_a1[k] * zi0;
                        j0 += zk;
                        j0p += han_a2[k] * zk;
                        y0 += han_a3[k] * zk;
                        y0p += han_a4[k] * zk;
                    }
                    j0p *= -.5 * zz;
                    Complex clogz = Complex.Log(.5 * zz);
                    y0 = (2.0 * j0 * clogz - y0) / PI + C2;
                    y0p = (2.0 / zz + 2.0 * j0p * clogz + .5 * y0p * zz) / PI + C1 * zz;
                    h0 = j0 + CPLX_01 * y0;
                    h0p = j0p + CPLX_01 * y0p;
                    if (ib == 0) return;
                    y0 = h0;
                    y0p = h0p;
                }
                Complex zi = 1.0 / zz;
                Complex zi2 = zi * zi;
                Complex p0z = 1.0 + (P20 * zi2 - P10) * zi2;
                Complex p1z = 1.0 + (P11 - P21 * zi2) * zi2;
                Complex q0z = (Q20 * zi2 - Q10) * zi;
                Complex q1z = (Q11 - Q21 * zi2) * zi;
                Complex zk2 = Complex.Exp(CPLX_01 * (zz - POF)) * Complex.Sqrt(zi) * C3;
                h0 = zk2 * (p0z + CPLX_01 * q0z);
                h0p = CPLX_01 * zk2 * (p1z + CPLX_01 * q1z);
                if (ib == 0) return;
                zms = Math.Cos((Math.Sqrt(zms) - 4.0) * 31.41592654);
                h0 = .5 * (y0 * (1.0 + zms) + h0 * (1.0 - zms));
                h0p = .5 * (y0p * (1.0 + zms) + h0p * (1.0 - zms));
            }

            // ---- evlua: percorso di integrazione nel piano complesso lambda ----
            void Evlua(out Complex erv, out Complex ezv, out Complex erh, out Complex eph)
            {
                Complex[] sum = new Complex[6], ans = new Complex[6];
                Complex cp1, cp2, cp3, bk = Complex.Zero, delta, delta2;
                double slope, rmis;
                double del = zph;
                if (rho > del) del = rho;

                if (zph >= 2.0 * rho)
                {
                    // forma con la funzione di Bessel
                    jh = 0;
                    a = Complex.Zero;
                    del = 1.0 / del;
                    if (del > tkmag)
                    {
                        b = new Complex(.1 * tkmag, -.1 * tkmag);
                        Rom1(6, sum, 2);
                        a = b;
                        b = new Complex(del, -del);
                        Rom1(6, ans, 2);
                        for (int i = 0; i < 6; i++) sum[i] += ans[i];
                    }
                    else
                    {
                        b = new Complex(del, -del);
                        Rom1(6, sum, 2);
                    }
                    delta = PTP * del;
                    Gshank(b, delta, ans, 6, sum, 0, b, b);
                    ans[5] *= ck1;
                    // coniugato perche' NEC usa exp(+jwt)
                    erv = Complex.Conjugate(ck1sq * ans[2]);
                    ezv = Complex.Conjugate(ck1sq * (ans[1] + ck2sq * ans[4]));
                    erh = Complex.Conjugate(ck2sq * (ans[0] + ans[5]));
                    eph = -Complex.Conjugate(ck2sq * (ans[3] + ans[5]));
                    return;
                }

                // forma con la funzione di Hankel
                jh = 1;
                cp1 = new Complex(0.0, .4 * ck2);
                cp2 = new Complex(.6 * ck2, -.2 * ck2);
                cp3 = new Complex(1.02 * ck2, -.2 * ck2);
                a = cp1;
                b = cp2;
                Rom1(6, sum, 2);
                a = cp2;
                b = cp3;
                Rom1(6, ans, 2);
                for (int i = 0; i < 6; i++) sum[i] = -(sum[i] + ans[i]);

                // dall'asse immaginario a -infinito
                if (zph > .001 * rho) slope = rho / zph;
                else slope = 1000.0;
                del = PTP / del;
                delta = new Complex(-1.0, slope) * del / Math.Sqrt(1.0 + slope * slope);
                delta2 = -Complex.Conjugate(delta);
                Gshank(cp1, delta, ans, 6, sum, 0, bk, bk);
                rmis = rho * (ck1.Real - ck2);

                bool jump = false;
                if (rmis >= 2.0 * ck2 && rho >= 1.0e-10)
                {
                    if (zph >= 1.0e-10)
                    {
                        bk = new Complex(-zph, rho) * (ck1 - cp3);
                        rmis = -bk.Real / Math.Abs(bk.Imaginary);
                        if (rmis > 4.0 * rho / zph) jump = true;
                    }
                    if (!jump)
                    {
                        // tra i tagli di diramazione, poi a +infinito
                        cp1 = ck1 - new Complex(.1, .2);
                        cp2 = cp1 + .2;
                        bk = new Complex(0.0, del);
                        Gshank(cp1, bk, sum, 6, ans, 0, bk, bk);
                        a = cp1;
                        b = cp2;
                        Rom1(6, ans, 1);
                        for (int i = 0; i < 6; i++) ans[i] -= sum[i];
                        Gshank(cp3, bk, sum, 6, ans, 0, bk, bk);
                        Gshank(cp2, delta2, ans, 6, sum, 0, bk, bk);
                    }
                    // YA-NEC 0.4.38 (fase 1 e 2 del 15/11/2026, via di IC8TEM 13:22): nel NEC-2 originale, se rmis > 4 rho / zph (GO TO 1)
                    // si integra SOTTO i punti di diramazione; nec2c metteva sempre "jump = TRUE" e saltava anche quel cammino, lasciando
                    // un risultato parziale nella tabella 3 di Sommerfeld (fili distanti 0,2-1 lambda, terreni molto conduttivi come il mare:
                    // fino al 5 % di scarto da necpp ed EZNEC). Dopo il cammino tra i tagli si salta; altrimenti si integra sotto (come necpp).
                    jump = !jump;
                }
                else jump = false;

                if (!jump)
                {
                    // sotto i punti di diramazione, poi a +infinito
                    for (int i = 0; i < 6; i++) sum[i] = -ans[i];
                    rmis = ck1.Real * 1.01;
                    if (ck2 + 1.0 > rmis) rmis = ck2 + 1.0;
                    bk = new Complex(rmis, .99 * ck1.Imaginary);
                    delta = bk - cp3;
                    delta *= del / Complex.Abs(delta);
                    Gshank(cp3, delta, ans, 6, sum, 1, bk, delta2);
                }

                ans[5] *= ck1;
                erv = Complex.Conjugate(ck1sq * ans[2]);
                ezv = Complex.Conjugate(ck1sq * (ans[1] + ck2sq * ans[4]));
                erh = Complex.Conjugate(ck2sq * (ans[0] + ans[5]));
                eph = -Complex.Conjugate(ck2sq * (ans[3] + ans[5]));
            }

            // ---- gshank: integrazione fino all'infinito con l'accelerazione di Shanks ----
            void Gshank(Complex start, Complex dela, Complex[] sum, int nans, Complex[] seed, int ibk, Complex bk, Complex delb)
            {
                int ibx, j, i, jm, intx, inx = 0;
                bool brk = false;
                double amg, den, denm;
                Complex a1, a2, as1, as2, del, aa;
                Complex[,] q1 = new Complex[6, 20], q2 = new Complex[6, 20];
                Complex[] ans1 = new Complex[6], ans2 = new Complex[6];

                double rbk = bk.Real;
                del = dela;
                ibx = ibk == 0 ? 1 : 0;
                for (i = 0; i < nans; i++) ans2[i] = seed[i];
                b = start;

                for (intx = 1; intx <= MAXH; intx++)
                {
                    inx = intx - 1;
                    a = b;
                    b += del;
                    if (ibx == 0 && b.Real >= rbk)
                    {
                        // punto di rottura: si riparte
                        ibx = 1;
                        b = bk;
                        del = delb;
                        Rom1(nans, sum, 2);
                        for (i = 0; i < nans; i++) ans2[i] += sum[i];
                        intx = 0;
                        continue;
                    }
                    Rom1(nans, sum, 2);
                    for (i = 0; i < nans; i++) ans1[i] = ans2[i] + sum[i];
                    a = b;
                    b += del;
                    if (ibx == 0 && b.Real >= rbk)
                    {
                        ibx = 2;
                        b = bk;
                        del = delb;
                        Rom1(nans, sum, 2);
                        for (i = 0; i < nans; i++) ans2[i] = ans1[i] + sum[i];
                        intx = 0;
                        continue;
                    }
                    Rom1(nans, sum, 2);
                    for (i = 0; i < nans; i++) ans2[i] = ans1[i] + sum[i];

                    den = 0.0;
                    for (i = 0; i < nans; i++)
                    {
                        as1 = ans1[i];
                        as2 = ans2[i];
                        if (intx >= 2)
                        {
                            for (j = 1; j < intx; j++)
                            {
                                jm = j - 1;
                                aa = q2[i, jm];
                                a1 = q1[i, jm] + as1 - 2.0 * aa;
                                if (a1.Real != 0.0 || a1.Imaginary != 0.0)
                                {
                                    a2 = aa - q1[i, jm];
                                    a1 = q1[i, jm] - a2 * a2 / a1;
                                }
                                else a1 = q1[i, jm];
                                a2 = aa + as2 - 2.0 * as1;
                                if (a2.Real != 0.0 || a2.Imaginary != 0.0)
                                    a2 = aa - (as1 - aa) * (as1 - aa) / a2;
                                else a2 = aa;
                                q1[i, jm] = as1;
                                q2[i, jm] = as2;
                                as1 = a1;
                                as2 = a2;
                            }
                        }
                        q1[i, intx - 1] = as1;
                        q2[i, intx - 1] = as2;
                        amg = Math.Abs(as2.Real) + Math.Abs(as2.Imaginary);
                        if (amg > den) den = amg;
                    }

                    denm = 1.0e-3 * den * CRIT;
                    jm = intx - 3;
                    if (jm < 1) jm = 1;
                    for (j = jm - 1; j < intx; j++)
                    {
                        brk = false;
                        for (i = 0; i < nans; i++)
                        {
                            a1 = q2[i, j];
                            den = (Math.Abs(a1.Real) + Math.Abs(a1.Imaginary)) * CRIT;
                            if (den < denm) den = denm;
                            a1 = q1[i, j] - a1;
                            // 1.5.9 (IC8TEM 22/11/2026 23:12, punto a): |re| + |im| come il Fortran del NEC-2 (nec2c scriveva |re + |im||,
                            // piu' piccolo con la parte reale negativa: l'integrazione poteva fermarsi troppo presto). Fonte: nec2-rs, docs/nec2c-deviations.md
                            amg = Math.Abs(a1.Real) + Math.Abs(a1.Imaginary);
                            if (amg > den)
                            {
                                brk = true;
                                break;
                            }
                        }
                        if (brk) break;
                    }
                    if (!brk)
                    {
                        for (i = 0; i < nans; i++) sum[i] = .5 * (q1[i, inx] + q2[i, inx]);
                        return;
                    }
                }
                // 1.5.9 (punto b): come il Fortran, dopo MAXH passi si usa l'ultima stima (nec2c si fermava); Analysis lo dice negli avvisi
                for (i = 0; i < nans; i++) sum[i] = .5 * (q1[i, inx] + q2[i, inx]);
                nonConv++;
            }

            // ---- rom1: Romberg a passo variabile dei 6 integrali da a a b ----
            void Rom1(int nn, Complex[] sum, int nx)
            {
                bool jump, lstep, nogo;
                int i, ns, nt;
                double zz, ze, s, ep, zend, dz = 0.0, dzot = 0.0, tr, ti;
                Complex t00, t11, t02;
                Complex[] g1 = new Complex[6], g2 = new Complex[6], g3 = new Complex[6], g4 = new Complex[6], g5 = new Complex[6];
                Complex[] t01 = new Complex[6], t10 = new Complex[6], t20 = new Complex[6];

                lstep = false;
                zz = 0.0;
                ze = 1.0;
                s = 1.0;
                ep = s / (1.0e4 * NM);
                zend = ze - ep;
                for (i = 0; i < nn; i++) sum[i] = Complex.Zero;
                ns = nx;
                nt = 0;
                Saoa(zz, g1);

                jump = false;
                while (true)
                {
                    if (!jump)
                    {
                        dz = s / ns;
                        if (zz + dz > ze)
                        {
                            dz = ze - zz;
                            if (dz <= ep) return;
                        }
                        dzot = dz * .5;
                        Saoa(zz + dzot, g3);
                        Saoa(zz + dz, g5);
                    }

                    nogo = false;
                    for (i = 0; i < nn; i++)
                    {
                        t00 = (g1[i] + g5[i]) * dzot;
                        t01[i] = (t00 + dz * g3[i]) * .5;
                        t10[i] = (4.0 * t01[i] - t00) / 3.0;
                        Test(t01[i].Real, t10[i].Real, out tr, t01[i].Imaginary, t10[i].Imaginary, out ti, 0.0);
                        if (tr > CRIT || ti > CRIT) nogo = true;
                    }
                    if (!nogo)
                    {
                        for (i = 0; i < nn; i++) sum[i] += t10[i];
                        nt += 2;
                        zz += dz;
                        if (zz > zend) return;
                        for (i = 0; i < nn; i++) g1[i] = g5[i];
                        if (nt >= NTS && ns > nx)
                        {
                            ns = ns / 2;
                            nt = 1;
                        }
                        jump = false;
                        continue;
                    }

                    Saoa(zz + dz * .25, g2);
                    Saoa(zz + dz * .75, g4);
                    nogo = false;
                    for (i = 0; i < nn; i++)
                    {
                        t02 = (t01[i] + dzot * (g2[i] + g4[i])) * .5;
                        t11 = (4.0 * t02 - t01[i]) / 3.0;
                        t20[i] = (16.0 * t11 - t10[i]) / 15.0;
                        Test(t11.Real, t20[i].Real, out tr, t11.Imaginary, t20[i].Imaginary, out ti, 0.0);
                        if (tr > CRIT || ti > CRIT) nogo = true;
                    }
                    if (!nogo)
                    {
                        for (i = 0; i < nn; i++) sum[i] += t20[i];
                        nt++;
                        zz += dz;
                        if (zz > zend) return;
                        for (i = 0; i < nn; i++) g1[i] = g5[i];
                        if (nt >= NTS && ns > nx)
                        {
                            ns = ns / 2;
                            nt = 1;
                        }
                        jump = false;
                        continue;
                    }

                    nt = 0;
                    if (ns < NM)
                    {
                        ns *= 2;
                        dz = s / ns;
                        dzot = dz * .5;
                        for (i = 0; i < nn; i++)
                        {
                            g5[i] = g3[i];
                            g3[i] = g2[i];
                        }
                        jump = true;
                        continue;
                    }

                    if (!lstep) lstep = true;   // nec2c qui calcola solo lambda per un messaggio diagnostico
                    for (i = 0; i < nn; i++) sum[i] += t20[i];
                    nt++;
                    zz += dz;
                    if (zz > zend) return;
                    for (i = 0; i < nn; i++) g1[i] = g5[i];
                    jump = false;
                }
            }

            // ---- saoa: integrandi dei 6 integrali per sorgente e osservatore sopra il terreno ----
            void Saoa(double t, Complex[] ans)
            {
                Complex dxl = b - a;
                Complex xl = a + dxl * t;
                Complex b0, b0p, cgam1, cgam2, com, dgam;
                if (jh == 0)
                {
                    Bessel(xl * rho, out b0, out b0p);
                    b0 *= 2.0;
                    b0p *= 2.0;
                    cgam1 = Complex.Sqrt(xl * xl - ck1sq);
                    cgam2 = Complex.Sqrt(xl * xl - ck2sq);
                    if (cgam1.Real == 0.0) cgam1 = new Complex(0.0, -Math.Abs(cgam1.Imaginary));
                    if (cgam2.Real == 0.0) cgam2 = new Complex(0.0, -Math.Abs(cgam2.Imaginary));
                }
                else
                {
                    Hankel(xl * rho, out b0, out b0p);
                    com = xl - ck1;
                    cgam1 = Complex.Sqrt(xl + ck1) * Complex.Sqrt(com);
                    if (com.Real < 0.0 && com.Imaginary >= 0.0) cgam1 = -cgam1;
                    com = xl - ck2;
                    cgam2 = Complex.Sqrt(xl + ck2) * Complex.Sqrt(com);
                    if (com.Real < 0.0 && com.Imaginary >= 0.0) cgam2 = -cgam2;
                }

                double xlr = (xl * Complex.Conjugate(xl)).Real;
                if (xlr >= tsmag)
                {
                    double sign;
                    if (xl.Imaginary >= 0.0)
                    {
                        xlr = xl.Real;
                        if (xlr >= ck2)
                        {
                            if (xlr <= ck1r) dgam = cgam2 - cgam1;
                            else
                            {
                                sign = 1.0;
                                dgam = 1.0 / (xl * xl);
                                dgam = sign * ((ct3 * dgam + ct2) * dgam + ct1) / xl;
                            }
                        }
                        else
                        {
                            sign = -1.0;
                            dgam = 1.0 / (xl * xl);
                            dgam = sign * ((ct3 * dgam + ct2) * dgam + ct1) / xl;
                        }
                    }
                    else
                    {
                        sign = 1.0;
                        dgam = 1.0 / (xl * xl);
                        dgam = sign * ((ct3 * dgam + ct2) * dgam + ct1) / xl;
                    }
                }
                else dgam = cgam2 - cgam1;

                Complex den2 = cksm * dgam / (cgam2 * (ck1sq * cgam2 + ck2sq * cgam1));
                Complex den1 = 1.0 / (cgam1 + cgam2) - cksm / cgam2;
                com = dxl * xl * Complex.Exp(-cgam2 * zph);
                ans[5] = com * b0 * den1 / ck1;
                com *= den2;
                if (rho != 0.0)
                {
                    b0p = b0p / rho;
                    ans[0] = -com * xl * (b0p + b0 * xl);
                    ans[3] = com * xl * b0p;
                }
                else
                {
                    ans[0] = -com * xl * xl * .5;
                    ans[3] = ans[0];
                }
                ans[1] = com * cgam2 * cgam2 * b0;
                ans[2] = -ans[3] * cgam2 * rho;
                ans[4] = com * b0;
            }
        }
    }
}
