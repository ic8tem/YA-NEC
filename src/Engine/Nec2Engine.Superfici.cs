// YA-NEC 1.5.9 - superfici del NEC-2 (patch SP/SM/SC): equazione MFIE di Burke e Poggio.
// IC8TEM 23/11/2026 00:47-00:54 («voglio attivare SP/SM/SC ... una simulazione piu' precisa per gli illuminatori»; strada A al posto
// della B per la nuova fonte nec2-rs). Traduzione delle routine di nec2c (N. Kyriazis 5B4AZ, pubblico dominio come il Fortran LLNL):
// cmws, cmsw, cmss (matrix.c), hsfld, hsflx, hfk, gh, hintg, unere, pcint (fields.c), parte delle superfici di cabc (calculations.c)
// e di ffld/fflds (radiation.c). Confronto con nec2-rs (UR3PKI, MIT) e con il Fortran nec2dXS di 4nec2. Differenza voluta da nec2c:
// in cmsw il termine della giunzione filo-superficie usa la funzione di base del segmento appena calcolata da trio (come il Fortran:
// AX(JSNO) dopo CALL TRIO(I)), non quella dell'ultimo segmento sorgente.
// Limiti come il NEC-2: superfici con terreno perfetto o coefficiente di riflessione, non con Sommerfeld; niente simmetrie (matrice intera).
using System;
using System.Numerics;

namespace YaNec.Engine
{
    public sealed partial class Nec2Engine
    {
        const double FPI = 12.56637062, PI8 = 25.13274123, CONST2 = 4.771341188;

        // ---- superfici: in metri (dalla geometria) e in lunghezze d'onda (frequenza corrente) ----
        readonly int m, neq;
        readonly double[] pxm, pym, pzm, pam, t1x, t1y, t1z, t2x, t2y, t2z, psalp;
        readonly double[] px, py, pz, pbi;
        // ---- dataj delle patch ----
        double t1xj, t1yj, t1zj, t2xj, t2yj, t2zj;
        int ipgnd = 1;
        // ---- tmh (hfk, gh) ----
        double tmh_zpka, tmh_rhks;

        /// <summary>Numero di superfici (patch) del modello.</summary>
        public int PatchCount { get { return m; } }

        /// <summary>Densita' di corrente (A/m, componenti x, y, z) della patch k (0 = prima) dopo la soluzione.</summary>
        public Complex[] PatchCurrent(int k)
        {
            if (cur == null || k < 0 || k >= m) return null;
            int b = n + 3 * k;
            return new[] { cur[b] * wlam, cur[b + 1] * wlam, cur[b + 2] * wlam };
        }

        void ScalaSuperfici(double fr)
        {
            for (int i = 0; i < m; i++)
            {
                px[i] = pxm[i] * fr; py[i] = pym[i] * fr; pz[i] = pzm[i] * fr;
                pbi[i] = pam[i] * fr * fr;
            }
        }

        // ======================= matrice: righe e colonne delle superfici (riempimento trasposto) =======================
        void CmSetSuperfici(Complex[] cm, int nrow)
        {
            // sorgenti sui fili, osservazione sulle patch (colonne n .. n+2m-1)
            for (int j = 1; j <= n; j++)
            {
                Trio(j);
                Cmws(j, 1, 2 * m, cm, nrow, n);
            }
            // sorgenti sulle patch: osservazione sui fili, poi sulle patch
            Cmsw(1, m, 1, n, cm, nrow);
            Cmss(1, m, cm, nrow);
        }

        // cmws: campo dei fili sulle patch (equazioni MFIE: prima t2, poi t1 per ogni patch)
        void Cmws(int j, int i1, int i2, Complex[] cm, int nr, int colBase)
        {
            j--;
            ds = si[j]; db = bi[j];
            xj = x[j]; yj = y[j]; zj = z[j];
            cabj = cab[j]; sabj = sab[j]; salpj = salp[j];
            int ipr = -1, js = 0;
            double tx, ty, tz;
            for (int i = i1; i <= i2; i++)
            {
                ipr++;
                int ipatch = (i + 1) / 2;
                int ik = i - (i / 2) * 2;
                if (ik != 0 || ipr == 0)
                {
                    js = ipatch - 1;
                    Hsfld(px[js], py[js], pz[js], 0.0);
                    if (ik != 0) { tx = t2x[js]; ty = t2y[js]; tz = t2z[js]; }
                    else { tx = t1x[js]; ty = t1y[js]; tz = t1z[js]; }
                }
                else { tx = t1x[js]; ty = t1y[js]; tz = t1z[js]; }
                Complex etk = -(exk * tx + eyk * ty + ezk * tz) * psalp[js];
                Complex ets = -(exs * tx + eys * ty + ezs * tz) * psalp[js];
                Complex etc = -(exc * tx + eyc * ty + ezc * tz) * psalp[js];
                long col = (long)(colBase + ipr) * nr;
                for (int ij = 0; ij < jsno; ij++)
                {
                    int jx = jco[ij] - 1;
                    cm[jx + col] += etk * ax[ij] + ets * bx[ij] + etc * cx[ij];
                }
            }
        }

        // cmsw: campo delle correnti delle patch lungo i fili (righe n + 2(j-1), n + 2(j-1) + 1)
        void Cmsw(int j1, int j2, int i1, int i2, Complex[] cm, int nrow)
        {
            Complex[] emel = new Complex[9];
            int k = -1, icgo = 0;
            double fsign = 1.0;
            for (int i = i1 - 1; i < i2; i++)
            {
                k++;
                double xi = x[i], yi = y[i], zi = z[i], cabi = cab[i], sabi = sab[i], salpi = salp[i];
                int ipch = 0;
                if (icon1[i] >= PCHCON) { ipch = icon1[i] - PCHCON; fsign = -1.0; }
                if (icon2[i] >= PCHCON) { ipch = icon2[i] - PCHCON; fsign = 1.0; }
                long col = (long)k * nrow;
                int jl = -1;
                for (int j = j1; j <= j2; j++)
                {
                    jl += 2;
                    int jsx = j - 1;
                    t1xj = t1x[jsx]; t1yj = t1y[jsx]; t1zj = t1z[jsx];
                    t2xj = t2x[jsx]; t2yj = t2y[jsx]; t2zj = t2z[jsx];
                    xj = px[jsx]; yj = py[jsx]; zj = pz[jsx];
                    ds = pbi[jsx];
                    for (int ip = 1; ip <= ksymp; ip++)
                    {
                        ipgnd = ip;
                        if ((ipch == j || icgo != 0) && ip != 2)
                        {
                            if (icgo <= 0)
                            {
                                Pcint(xi, yi, zi, cabi, sabi, salpi, emel);
                                double pyl = PI * si[i] * fsign;
                                double pxl = Math.Sin(pyl);
                                pyl = Math.Cos(pyl);
                                Complex excl = emel[8] * fsign;
                                Trio(i + 1);
                                int jsnox = jsno - 1;
                                cm[i + col] += excl * (ax[jsnox] + bx[jsnox] * pxl + cx[jsnox] * pyl);
                            }
                            cm[n + (jl - 1) + col] = emel[icgo];
                            cm[n + jl + col] = emel[icgo + 4];
                            icgo++;
                            if (icgo == 4) icgo = 0;
                            continue;
                        }
                        Unere(xi, yi, zi);
                        cm[n + (jl - 1) + col] += exk * cabi + eyk * sabi + ezk * salpi;
                        cm[n + jl + col] += exs * cabi + eys * sabi + ezs * salpi;
                    }
                }
            }
            ipgnd = 1;
        }

        // cmss: interazioni patch-patch (MFIE, con il termine 1/2 sulla diagonale)
        void Cmss(int j1, int j2, Complex[] cm, int nrow)
        {
            for (int il = 0; il < m; il++)
            {
                double t1xi = t1x[il] * psalp[il], t1yi = t1y[il] * psalp[il], t1zi = t1z[il] * psalp[il];
                double t2xi = t2x[il] * psalp[il], t2yi = t2y[il] * psalp[il], t2zi = t2z[il] * psalp[il];
                double xi = px[il], yi = py[il], zi = pz[il];
                long ii1 = n + 2 * il, ii2 = ii1 + 1;
                for (int j = j1; j <= j2; j++)
                {
                    int jl = j - 1;
                    int jj1 = n + 2 * jl, jj2 = jj1 + 1;
                    ds = pbi[jl];
                    xj = px[jl]; yj = py[jl]; zj = pz[jl];
                    t1xj = t1x[jl]; t1yj = t1y[jl]; t1zj = t1z[jl];
                    t2xj = t2x[jl]; t2yj = t2y[jl]; t2zj = t2z[jl];
                    Hintg(xi, yi, zi);
                    Complex g11 = -(t2xi * exk + t2yi * eyk + t2zi * ezk);
                    Complex g12 = -(t2xi * exs + t2yi * eys + t2zi * ezs);
                    Complex g21 = -(t1xi * exk + t1yi * eyk + t1zi * ezk);
                    Complex g22 = -(t1xi * exs + t1yi * eys + t1zi * ezs);
                    if (il == jl) { g11 -= .5; g22 += .5; }
                    cm[jj1 + ii1 * nrow] = g11;
                    cm[jj2 + ii1 * nrow] = g12;
                    cm[jj1 + ii2 * nrow] = g21;
                    cm[jj2 + ii2 * nrow] = g22;
                }
            }
        }

        // ======================= campi =======================
        // hsfld: campo H di un segmento con corrente costante, seno e coseno, con il terreno
        void Hsfld(double xi, double yi, double zi, double ai)
        {
            double xij = xi - xj, yij = yi - yj, rfl = -1.0;
            for (int ip = 0; ip < ksymp; ip++)
            {
                rfl = -rfl;
                double salpr = salpj * rfl;
                double zij = zi - rfl * zj;
                double zp = xij * cabj + yij * sabj + zij * salpr;
                double rhox = xij - cabj * zp, rhoy = yij - sabj * zp, rhoz = zij - salpr * zp;
                double rh = Math.Sqrt(rhox * rhox + rhoy * rhoy + rhoz * rhoz + ai * ai);
                if (rh <= 1.0e-10)
                {
                    exk = eyk = ezk = exs = eys = ezs = exc = eyc = ezc = Complex.Zero;
                    continue;
                }
                rhox /= rh; rhoy /= rh; rhoz /= rh;
                double phx = sabj * rhoz - salpr * rhoy;
                double phy = salpr * rhox - cabj * rhoz;
                double phz = cabj * rhoy - sabj * rhox;
                Complex hpk, hps, hpc;
                Hsflx(ds, rh, zp, out hpk, out hps, out hpc);
                if (ip == 1)
                {
                    if (iperf != 1)
                    {
                        Complex zratx = zrati;
                        double rmag = Math.Sqrt(zp * zp + rh * rh);
                        double xymag = Math.Sqrt(xij * xij + yij * yij);
                        Complex rrv, rrh;
                        if (nradl != 0)
                        {
                            double xspec = (xi * zj + zi * xj) / (zi + zj);
                            double yspec = (yi * zj + zi * yj) / (zi + zj);
                            double rhospc = Math.Sqrt(xspec * xspec + yspec * yspec + gsT2 * gsT2);
                            if (rhospc <= scrwl)
                            {
                                rrv = gsT1 * rhospc * Math.Log(rhospc / gsT2);
                                zratx = (rrv * zrati) / (ETA * zrati + rrv);
                            }
                        }
                        double ppx, ppy, cth;
                        if (xymag <= 1.0e-6) { ppx = 0; ppy = 0; cth = 1; rrv = Complex.One; }
                        else
                        {
                            ppx = -yij / xymag; ppy = xij / xymag; cth = zij / rmag;
                            rrv = Complex.Sqrt(1.0 - zratx * zratx * (1.0 - cth * cth));
                        }
                        rrh = zratx * cth;
                        rrh = -(rrh - rrv) / (rrh + rrv);
                        rrv = zratx * rrv;
                        rrv = (cth - rrv) / (cth + rrv);
                        Complex qy = (phx * ppx + phy * ppy) * (rrv - rrh);
                        Complex qx = qy * ppx + phx * rrh;
                        qy = qy * ppy + phy * rrh;
                        Complex qz = phz * rrh;
                        exk -= hpk * qx; eyk -= hpk * qy; ezk -= hpk * qz;
                        exs -= hps * qx; eys -= hps * qy; ezs -= hps * qz;
                        exc -= hpc * qx; eyc -= hpc * qy; ezc -= hpc * qz;
                        continue;
                    }
                    exk -= hpk * phx; eyk -= hpk * phy; ezk -= hpk * phz;
                    exs -= hps * phx; eys -= hps * phy; ezs -= hps * phz;
                    exc -= hpc * phx; eyc -= hpc * phy; ezc -= hpc * phz;
                    continue;
                }
                exk = hpk * phx; eyk = hpk * phy; ezk = hpk * phz;
                exs = hps * phx; eys = hps * phy; ezs = hps * phz;
                exc = hpc * phx; eyc = hpc * phy; ezc = hpc * phz;
            }
        }

        // hsflx: campo H di corrente seno, coseno e costante su un segmento
        void Hsflx(double s, double rh, double zpx, out Complex hpk, out Complex hps, out Complex hpc)
        {
            Complex fjk = new Complex(0.0, -TP);
            if (rh < 1.0e-10) { hpk = hps = hpc = Complex.Zero; return; }
            double zp, hss;
            if (zpx >= 0.0) { zp = zpx; hss = 1.0; } else { zp = -zpx; hss = -1.0; }
            double dh = .5 * s, z1 = zp + dh, z2a = zp - dh;
            double rhz = z2a >= 1.0e-7 ? rh / z2a : 1.0;
            double dk = TP * dh, cdk = Math.Cos(dk), sdk = Math.Sin(dk);
            double hkr, hki;
            Hfk(-dk, dk, rh * TP, zp * TP, out hkr, out hki);
            hpk = new Complex(hkr, hki);
            if (rhz >= 1.0e-3)
            {
                double rh2 = rh * rh;
                double r1 = Math.Sqrt(rh2 + z1 * z1), r2 = Math.Sqrt(rh2 + z2a * z2a);
                Complex ekr1 = Complex.Exp(fjk * r1), ekr2 = Complex.Exp(fjk * r2);
                Complex t1 = z1 * ekr1 / r1, t2 = z2a * ekr2 / r2;
                hps = (cdk * (ekr2 - ekr1) - Complex.ImaginaryOne * sdk * (t2 + t1)) * hss;
                hpc = -sdk * (ekr2 + ekr1) - Complex.ImaginaryOne * cdk * (t2 - t1);
                Complex cons = -Complex.ImaginaryOne / (2.0 * TP * rh);
                hps = cons * hps;
                hpc = cons * hpc;
                return;
            }
            Complex e1 = new Complex(cdk, sdk) / (z2a * z2a), e2 = new Complex(cdk, -sdk) / (z1 * z1);
            double tt1 = TP * (1.0 / z1 - 1.0 / z2a);
            Complex tt2 = Complex.Exp(fjk * zp) * rh / PI8;
            hps = tt2 * (tt1 + (e1 + e2) * sdk) * hss;
            hpc = tt2 * (-Complex.ImaginaryOne * tt1 + (e1 - e2) * cdk);
        }

        // hfk: integrale del campo H a nucleo (Romberg a passo variabile)
        void Hfk(double el1, double el2, double rhk, double zpkx, out double sgr, out double sgi)
        {
            const int nx = 1, nma = 65536, nts = 4;
            const double rx = 1.0e-4;
            int ns, nt;
            bool flag = true;
            double dz = 0, dzot = 0, g1r, g1i, g2r, g2i, g3r = 0, g3i = 0, g4r, g4i, g5r = 0, g5i = 0;
            tmh_zpka = zpkx;
            tmh_rhks = rhk * rhk;
            double zz = el1, ze = el2, s = ze - zz;
            double ep = s / (10.0 * nma), zend = ze - ep;
            sgr = 0; sgi = 0;
            ns = nx; nt = 0;
            Gh(zz, out g1r, out g1i);
            while (true)
            {
                if (flag)
                {
                    dz = s / ns;
                    double zp = zz + dz;
                    if (zp > ze)
                    {
                        dz = ze - zz;
                        if (Math.Abs(dz) <= ep) { sgr *= rhk * .5; sgi *= rhk * .5; return; }
                    }
                    dzot = dz * .5;
                    Gh(zz + dzot, out g3r, out g3i);
                    Gh(zz + dz, out g5r, out g5i);
                }
                double t00r = (g1r + g5r) * dzot, t00i = (g1i + g5i) * dzot;
                double t01r = (t00r + dz * g3r) * 0.5, t01i = (t00i + dz * g3i) * 0.5;
                double t10r = (4.0 * t01r - t00r) / 3.0, t10i = (4.0 * t01i - t00i) / 3.0;
                double te1r, te1i;
                Test(t01r, t10r, out te1r, t01i, t10i, out te1i, 0.0);
                if (te1i <= rx && te1r <= rx)
                {
                    sgr += t10r; sgi += t10i;
                    nt += 2;
                    zz += dz;
                    if (zz >= zend) { sgr *= rhk * .5; sgi *= rhk * .5; return; }
                    g1r = g5r; g1i = g5i;
                    if (nt >= nts && ns > nx) { ns /= 2; nt = 1; }
                    flag = true;
                    continue;
                }
                Gh(zz + dz * 0.25, out g2r, out g2i);
                Gh(zz + dz * 0.75, out g4r, out g4i);
                double t02r = (t01r + dzot * (g2r + g4r)) * 0.5, t02i = (t01i + dzot * (g2i + g4i)) * 0.5;
                double t11r = (4.0 * t02r - t01r) / 3.0, t11i = (4.0 * t02i - t01i) / 3.0;
                double t20r = (16.0 * t11r - t10r) / 15.0, t20i = (16.0 * t11i - t10i) / 15.0;
                double te2r, te2i;
                Test(t11r, t20r, out te2r, t11i, t20i, out te2i, 0.0);
                if (te2i > rx || te2r > rx)
                {
                    nt = 0;
                    if (ns < nma)
                    {
                        ns *= 2;
                        dz = s / ns;
                        dzot = dz * 0.5;
                        g5r = g3r; g5i = g3i;
                        g3r = g2r; g3i = g2i;
                        flag = false;
                        continue;
                    }
                }
                sgr += t20r; sgi += t20i;
                nt++;
                zz += dz;
                if (zz >= zend) { sgr *= rhk * .5; sgi *= rhk * .5; return; }
                g1r = g5r; g1i = g5i;
                if (nt >= nts && ns > nx) { ns /= 2; nt = 1; }
                flag = true;
            }
        }

        void Gh(double zk, out double hr, out double hi)
        {
            double rs = zk - tmh_zpka;
            rs = tmh_rhks + rs * rs;
            double r = Math.Sqrt(rs), ckr = Math.Cos(r), skr = Math.Sin(r);
            double rr2 = 1.0 / rs, rr3 = rr2 / r;
            hr = skr * rr2 + ckr * rr3;
            hi = ckr * rr2 - skr * rr3;
        }

        // hintg: campo H di una corrente di patch (sorgente in xj, yj, zj con area ds e versori t1j, t2j)
        void Hintg(double xi, double yi, double zi)
        {
            double rx = xi - xj, ry = yi - yj, rfl = -1.0;
            exk = eyk = ezk = exs = eys = ezs = Complex.Zero;
            for (int ip = 1; ip <= ksymp; ip++)
            {
                rfl = -rfl;
                double rz = zi - zj * rfl;
                double rsq = rx * rx + ry * ry + rz * rz;
                if (rsq < 1.0e-20) continue;
                double r = Math.Sqrt(rsq), rk = TP * r, cr = Math.Cos(rk), sr = Math.Sin(rk);
                Complex gam = -(new Complex(cr, -sr) + rk * new Complex(sr, cr)) / (FPI * rsq * r) * ds;
                Complex excl = gam * rx, eycl = gam * ry, ezcl = gam * rz;
                double t1zr = t1zj * rfl, t2zr = t2zj * rfl;
                Complex f1x = eycl * t1zr - ezcl * t1yj, f1y = ezcl * t1xj - excl * t1zr, f1z = excl * t1yj - eycl * t1xj;
                Complex f2x = eycl * t2zr - ezcl * t2yj, f2y = ezcl * t2xj - excl * t2zr, f2z = excl * t2yj - eycl * t2xj;
                if (ip != 1)
                {
                    if (iperf == 1)
                    {
                        f1x = -f1x; f1y = -f1y; f1z = -f1z; f2x = -f2x; f2y = -f2y; f2z = -f2z;
                    }
                    else
                    {
                        double xymag = Math.Sqrt(rx * rx + ry * ry), pxx, pyy, cth;
                        Complex rrv, rrh;
                        if (xymag <= 1.0e-6) { pxx = 0; pyy = 0; cth = 1; rrv = Complex.One; }
                        else
                        {
                            pxx = -ry / xymag; pyy = rx / xymag; cth = rz / r;
                            rrv = Complex.Sqrt(1.0 - zrati * zrati * (1.0 - cth * cth));
                        }
                        rrh = zrati * cth;
                        rrh = (rrh - rrv) / (rrh + rrv);
                        rrv = zrati * rrv;
                        rrv = -(cth - rrv) / (cth + rrv);
                        Complex g = (f1x * pxx + f1y * pyy) * (rrv - rrh);
                        f1x = f1x * rrh + g * pxx; f1y = f1y * rrh + g * pyy; f1z = f1z * rrh;
                        g = (f2x * pxx + f2y * pyy) * (rrv - rrh);
                        f2x = f2x * rrh + g * pxx; f2y = f2y * rrh + g * pyy; f2z = f2z * rrh;
                    }
                }
                exk += f1x; eyk += f1y; ezk += f1z;
                exs += f2x; eys += f2y; ezs += f2z;
            }
        }

        // unere: campo E di una corrente unitaria t1 e t2 su una patch (ipgnd = 2: immagine nel terreno)
        void Unere(double xob, double yob, double zob)
        {
            double zr = zj, t1zr = t1zj, t2zr = t2zj;
            if (ipgnd == 2) { zr = -zr; t1zr = -t1zr; t2zr = -t2zr; }
            double rx = xob - xj, ry = yob - yj, rz = zob - zr;
            double r2 = rx * rx + ry * ry + rz * rz;
            if (r2 <= 1.0e-20) { exk = eyk = ezk = exs = eys = ezs = Complex.Zero; return; }
            double r = Math.Sqrt(r2), tt1 = -TP * r, tt2 = tt1 * tt1, rt = r2 * r;
            Complex er = new Complex(Math.Sin(tt1), -Math.Cos(tt1)) * (CONST2 * ds);
            Complex q1 = new Complex(tt2 - 1.0, tt1) * er / rt;
            Complex q2 = new Complex(3.0 - tt2, -3.0 * tt1) * er / (rt * r2);
            er = q2 * (t1xj * rx + t1yj * ry + t1zr * rz);
            exk = q1 * t1xj + er * rx; eyk = q1 * t1yj + er * ry; ezk = q1 * t1zr + er * rz;
            er = q2 * (t2xj * rx + t2yj * ry + t2zr * rz);
            exs = q1 * t2xj + er * rx; eys = q1 * t2yj + er * ry; ezs = q1 * t2zr + er * rz;
            if (ipgnd == 1) return;
            if (iperf == 1)
            {
                exk = -exk; eyk = -eyk; ezk = -ezk; exs = -exs; eys = -eys; ezs = -ezs;
                return;
            }
            double xymag = Math.Sqrt(rx * rx + ry * ry), ppx, ppy, cth;
            Complex rrv, rrh;
            if (xymag <= 1.0e-6) { ppx = 0; ppy = 0; cth = 1; rrv = Complex.One; }
            else
            {
                ppx = -ry / xymag; ppy = rx / xymag; cth = rz / Math.Sqrt(xymag * xymag + rz * rz);
                rrv = Complex.Sqrt(1.0 - zrati * zrati * (1.0 - cth * cth));
            }
            rrh = zrati * cth;
            rrh = (rrh - rrv) / (rrh + rrv);
            rrv = zrati * rrv;
            rrv = -(cth - rrv) / (cth + rrv);
            Complex edp = (exk * ppx + eyk * ppy) * (rrh - rrv);
            exk = exk * rrv + edp * ppx; eyk = eyk * rrv + edp * ppy; ezk = ezk * rrv;
            edp = (exs * ppx + eys * ppy) * (rrh - rrv);
            exs = exs * rrv + edp * ppx; eys = eys * rrv + edp * ppy; ezs = ezs * rrv;
        }

        // pcint: campo delle 4 patch di una giunzione filo-superficie, integrato su 10 x 10 punti
        void Pcint(double xi, double yi, double zi, double cabi, double sabi, double salpi, Complex[] e)
        {
            const int nint = 10;
            double d = Math.Sqrt(ds) * .5, dsl = 4.0 * d / nint, da = dsl * dsl;
            double gcon = 1.0 / ds, fcon = 1.0 / (2.0 * TP * d);
            double xxj = xj, xyj = yj, xzj = zj, xs = ds;
            ds = da;
            double s1 = d + dsl * .5;
            double xss = xj + s1 * (t1xj + t2xj), yss = yj + s1 * (t1yj + t2yj), zss = zj + s1 * (t1zj + t2zj);
            s1 = s1 + d;
            double s2x = s1;
            Complex e1 = 0, e2 = 0, e3 = 0, e4 = 0, e5 = 0, e6 = 0, e7 = 0, e8 = 0, e9 = 0;
            for (int i1 = 0; i1 < nint; i1++)
            {
                s1 -= dsl;
                double s2 = s2x;
                xss -= dsl * t1xj; yss -= dsl * t1yj; zss -= dsl * t1zj;
                xj = xss; yj = yss; zj = zss;
                for (int i2 = 0; i2 < nint; i2++)
                {
                    s2 -= dsl;
                    xj -= dsl * t2xj; yj -= dsl * t2yj; zj -= dsl * t2zj;
                    Unere(xi, yi, zi);
                    Complex ek = exk * cabi + eyk * sabi + ezk * salpi;
                    Complex es = exs * cabi + eys * sabi + ezs * salpi;
                    double g1 = (d + s1) * (d + s2) * gcon, g2 = (d - s1) * (d + s2) * gcon;
                    double g3 = (d - s1) * (d - s2) * gcon, g4 = (d + s1) * (d - s2) * gcon;
                    double f2 = (s1 * s1 + s2 * s2) * TP;
                    double f1 = s1 / f2 - (g1 - g2 - g3 + g4) * fcon;
                    f2 = s2 / f2 - (g1 + g2 - g3 - g4) * fcon;
                    e1 += ek * g1; e2 += ek * g2; e3 += ek * g3; e4 += ek * g4;
                    e5 += es * g1; e6 += es * g2; e7 += es * g3; e8 += es * g4;
                    e9 += ek * f1 + es * f2;
                }
            }
            e[0] = e1; e[1] = e2; e[2] = e3; e[3] = e4; e[4] = e5; e[5] = e6; e[6] = e7; e[7] = e8; e[8] = e9;
            xj = xxj; yj = xyj; zj = xzj; ds = xs;
        }

        // cabc, parte delle superfici: correnti t1, t2 -> componenti x, y, z (in place, dalla fine come nec2c)
        void CabcSuperfici(Complex[] curx)
        {
            int jco1 = n + 2 * m, jco2 = jco1 + m;
            for (int i = 1; i <= m; i++)
            {
                jco1 -= 2;
                jco2 -= 3;
                Complex cs1 = curx[jco1], cs2 = curx[jco1 + 1];
                int k = m - i;
                curx[jco2] = cs1 * t1x[k] + cs2 * t2x[k];
                curx[jco2 + 1] = cs1 * t1y[k] + cs2 * t2y[k];
                curx[jco2 + 2] = cs1 * t1z[k] + cs2 * t2z[k];
            }
        }

        // fflds e parte delle superfici di ffld: campo lontano delle patch (con l'immagine nel terreno)
        void CampoSuperfici(double thx, double thy, double thz, double phx, double phy, double rox, double roy, double roz, int kmax,
                            out Complex sth, out Complex sph)
        {
            Complex ex = 0, ey = 0, ez = 0;
            double rfl = -1.0;
            for (int ip = 0; ip < kmax; ip++)
            {
                rfl = -rfl;
                double rrz = roz * rfl;
                Complex gx = 0, gy = 0, gz = 0;
                for (int j = 0; j < m; j++)
                {
                    double arg = TP * (rox * px[j] + roy * py[j] + rrz * pz[j]);
                    Complex ct = new Complex(Math.Cos(arg) * pbi[j], Math.Sin(arg) * pbi[j]);
                    int k = n + 3 * j;
                    gx += cur[k] * ct; gy += cur[k + 1] * ct; gz += cur[k + 2] * ct;
                }
                Complex cdot = rox * gx + roy * gy + rrz * gz;
                gx = CONST4 * (cdot * rox - gx); gy = CONST4 * (cdot * roy - gy); gz = CONST4 * (cdot * rrz - gz);
                if (ip != 1) { ex = gx; ey = gy; ez = gz; continue; }
                if (iperf == 1) { gx = -gx; gy = -gy; gz = -gz; }
                else
                {
                    Complex rrv = Complex.Sqrt(1.0 - zrati * zrati * thz * thz);
                    Complex rrh = zrati * roz;
                    rrh = (rrh - rrv) / (rrh + rrv);
                    rrv = zrati * rrv;
                    rrv = -(roz - rrv) / (roz + rrv);
                    Complex et = (gx * phx + gy * phy) * (rrh - rrv);
                    gx = gx * rrv + et * phx; gy = gy * rrv + et * phy; gz = gz * rrv;
                }
                ex += gx; ey += gy; ez -= gz;
            }
            sth = ex * thx + ey * thy + ez * thz;
            sph = ex * phx + ey * phy;
        }
    }
}
