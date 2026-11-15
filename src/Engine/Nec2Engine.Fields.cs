// YA-NEC - campi vicini dei segmenti (efld, eksc, ekscx, gx, gxx), traduzione fedele di fields.c di nec2c. GPL-3.0.
// Terreno reale: coefficiente di riflessione (GN 0) oppure soluzione di Sommerfeld (GN 2, Nec2Engine.Sommerfeld.cs).
using System;
using System.Numerics;

namespace YaNec.Engine
{
    public sealed partial class Nec2Engine
    {
        // campo E vicino di un segmento con correnti seno, coseno e costante; effetto del terreno incluso
        void Efld(double xi, double yi, double zi, double ai, int ij)
        {
            Complex txk, tyk, tzk, txs, tys, tzs, txc, tyc, tzc;
            Complex tezs, ters, tezc, terc, tezk, terk;
            double xij = xi - xj;
            double yij = yi - yj;
            int ijx = ij;
            double rfl = -1.0;

            for (int ip = 0; ip < ksymp; ip++)
            {
                if (ip == 1) ijx = 1;
                rfl = -rfl;
                double salpr = salpj * rfl;
                double zij = zi - rfl * zj;
                double zp = xij * cabj + yij * sabj + zij * salpr;
                double rhox = xij - cabj * zp;
                double rhoy = yij - sabj * zp;
                double rhoz = zij - salpr * zp;
                double rh = Math.Sqrt(rhox * rhox + rhoy * rhoy + rhoz * rhoz + ai * ai);
                if (rh <= 1.0e-10)
                {
                    rhox = 0.0; rhoy = 0.0; rhoz = 0.0;
                }
                else
                {
                    rhox = rhox / rh; rhoy = rhoy / rh; rhoz = rhoz / rh;
                }

                // approssimazione dell'elemento di corrente concentrato per grandi distanze
                double r = Math.Sqrt(zp * zp + rh * rh);
                if (r >= rkh)
                {
                    double rmag = TP * r;
                    double cth = zp / r;
                    double px = rh / r;
                    txk = new Complex(Math.Cos(rmag), -Math.Sin(rmag));
                    double py = TP * r * r;
                    tyk = ETA * cth * txk * new Complex(1.0, -1.0 / rmag) / py;
                    tzk = ETA * px * txk * new Complex(1.0, rmag - 1.0 / rmag) / (2.0 * py);
                    tezk = tyk * cth - tzk * px;
                    terk = tyk * px + tzk * cth;
                    rmag = Math.Sin(PI * ds) / PI;
                    tezc = tezk * rmag;
                    terc = terk * rmag;
                    tezk = tezk * ds;
                    terk = terk * ds;
                    txs = Complex.Zero;
                    tys = Complex.Zero;
                    tzs = Complex.Zero;
                }
                else
                {
                    if (iexk != 1)
                        Eksc(ds, zp, rh, TP, ijx, out tezs, out ters, out tezc, out terc, out tezk, out terk);
                    else
                        Ekscx(db, ds, zp, rh, TP, ijx, ind1, ind2, out tezs, out ters, out tezc, out terc, out tezk, out terk);
                    txs = tezs * cabj + ters * rhox;
                    tys = tezs * sabj + ters * rhoy;
                    tzs = tezs * salpr + ters * rhoz;
                }

                txk = tezk * cabj + terk * rhox;
                tyk = tezk * sabj + terk * rhoy;
                tzk = tezk * salpr + terk * rhoz;
                txc = tezc * cabj + terc * rhox;
                tyc = tezc * sabj + terc * rhoy;
                tzc = tezc * salpr + terc * rhoz;

                if (ip == 1)
                {
                    if (iperf <= 0)
                    {
                        Complex zratx = zrati;
                        // schermo di radiali (nec2c, efld): impedenza superficiale dello schermo nel punto speculare
                        if (nradl != 0)
                        {
                            double xspec = (xi * zj + zi * xj) / (zi + zj), yspec = (yi * zj + zi * yj) / (zi + zj);
                            double rhospc = Math.Sqrt(xspec * xspec + yspec * yspec + gsT2 * gsT2);
                            if (rhospc <= scrwl)
                            {
                                Complex zscrn = gsT1 * rhospc * Math.Log(rhospc / gsT2);
                                zratx = (zscrn * zrati) / (ETA * zrati + zscrn);
                            }
                        }
                        double rmag = r;
                        double xymag = Math.Sqrt(xij * xij + yij * yij);
                        double px, py, cth;
                        Complex zrsin;
                        // coefficienti di riflessione
                        if (xymag <= 1.0e-6)
                        {
                            px = 0.0; py = 0.0; cth = 1.0;
                            zrsin = Complex.One;
                        }
                        else
                        {
                            px = -yij / xymag;
                            py = xij / xymag;
                            cth = zij / rmag;
                            zrsin = Complex.Sqrt(1.0 - zratx * zratx * (1.0 - cth * cth));
                        }
                        Complex refs = (cth - zratx * zrsin) / (cth + zratx * zrsin);
                        Complex refps = -(zratx * cth - zrsin) / (zratx * cth + zrsin);
                        refps = refps - refs;
                        Complex epy = px * txk + py * tyk;
                        Complex epx = px * epy;
                        epy = py * epy;
                        txk = refs * txk + refps * epx;
                        tyk = refs * tyk + refps * epy;
                        tzk = refs * tzk;
                        epy = px * txs + py * tys;
                        epx = px * epy;
                        epy = py * epy;
                        txs = refs * txs + refps * epx;
                        tys = refs * tys + refps * epy;
                        tzs = refs * tzs;
                        epy = px * txc + py * tyc;
                        epx = px * epy;
                        epy = py * epy;
                        txc = refs * txc + refps * epx;
                        tyc = refs * tyc + refps * epy;
                        tzc = refs * tzc;
                    }
                    exk = exk - txk * frati;
                    eyk = eyk - tyk * frati;
                    ezk = ezk - tzk * frati;
                    exs = exs - txs * frati;
                    eys = eys - tys * frati;
                    ezs = ezs - tzs * frati;
                    exc = exc - txc * frati;
                    eyc = eyc - tyc * frati;
                    ezc = ezc - tzc * frati;
                    continue;
                }

                exk = txk; eyk = tyk; ezk = tzk;
                exs = txs; eys = tys; ezs = tzs;
                exc = txc; eyc = tyc; ezc = tzc;
            }

            // terreno di Sommerfeld (GN 2): si aggiunge il campo dovuto al terreno (Sommerfeld/Norton)
            if (iperf == 2) EfldSommerfeld(xi, yi, zi, ai, xij, yij);
        }

        // campo E di filamenti di corrente seno, coseno e costante: approssimazione del filo sottile
        void Eksc(double s, double zz, double rh, double xk, int ij,
                  out Complex ezs_, out Complex ers_, out Complex ezc_, out Complex erc_, out Complex ezk_, out Complex erk_)
        {
            Complex gz1, gz2, gp1, gp2, gzp1, gzp2;
            double cint, sint;
            tmi_ij = ij;
            tmi_zpk = xk * zz;
            double rhk = xk * rh;
            tmi_rkb2 = rhk * rhk;
            double sh = .5 * s;
            double shk = xk * sh;
            double ss = Math.Sin(shk);
            double cs = Math.Cos(shk);
            double z2a = sh - zz;
            double z1a = -(sh + zz);
            Gx(z1a, rh, xk, out gz1, out gp1);
            Gx(z2a, rh, xk, out gz2, out gp2);
            gzp1 = gp1 * z1a;
            gzp2 = gp2 * z2a;
            ezs_ = CONST1 * ((gz2 - gz1) * cs * xk - (gzp2 + gzp1) * ss);
            ezc_ = -CONST1 * ((gz2 + gz1) * ss * xk + (gzp2 - gzp1) * cs);
            erk_ = CONST1 * (gp2 - gp1) * rh;
            Intx(-shk, shk, rhk, ij, out cint, out sint);
            ezk_ = -CONST1 * (gzp2 - gzp1 + xk * xk * new Complex(cint, -sint));
            gzp1 = gzp1 * z1a;
            gzp2 = gzp2 * z2a;
            if (rh >= 1.0e-10)
            {
                ers_ = -CONST1 * ((gzp2 + gzp1 + gz2 + gz1) * ss - (z2a * gz2 - z1a * gz1) * cs * xk) / rh;
                erc_ = -CONST1 * ((gzp2 - gzp1 + gz2 - gz1) * cs + (z2a * gz2 + z1a * gz1) * ss * xk) / rh;
                return;
            }
            ers_ = Complex.Zero;
            erc_ = Complex.Zero;
        }

        // come eksc ma con l'approssimazione estesa del filo sottile
        void Ekscx(double bx_, double s, double zz, double rhx, double xk, int ij, int inx1, int inx2,
                   out Complex ezs_, out Complex ers_, out Complex ezc_, out Complex erc_, out Complex ezk_, out Complex erk_)
        {
            int ira;
            double b, rh;
            Complex gz1, gz2, gzp1, gzp2, gr1, gr2, grp1, grp2, grk1, grk2, gzz1, gzz2;
            double cint, sint;
            if (rhx >= bx_)
            {
                rh = rhx; b = bx_; ira = 0;
            }
            else
            {
                rh = bx_; b = rhx; ira = 1;
            }
            double sh = .5 * s;
            tmi_ij = ij;
            tmi_zpk = xk * zz;
            double rhk = xk * rh;
            tmi_rkb2 = rhk * rhk;
            double shk = xk * sh;
            double ss = Math.Sin(shk);
            double cs = Math.Cos(shk);
            double z2a = sh - zz;
            double z1a = -(sh + zz);
            double a2 = b * b;

            if (inx1 != 2)
                Gxx(z1a, rh, b, a2, xk, ira, out gz1, out gzp1, out gr1, out grp1, out grk1, out gzz1);
            else
            {
                Gx(z1a, rhx, xk, out gz1, out grk1);
                gzp1 = grk1 * z1a;
                gr1 = gz1 / rhx;
                grp1 = gzp1 / rhx;
                grk1 = grk1 * rhx;
                gzz1 = Complex.Zero;
            }
            if (inx2 != 2)
                Gxx(z2a, rh, b, a2, xk, ira, out gz2, out gzp2, out gr2, out grp2, out grk2, out gzz2);
            else
            {
                Gx(z2a, rhx, xk, out gz2, out grk2);
                gzp2 = grk2 * z2a;
                gr2 = gz2 / rhx;
                grp2 = gzp2 / rhx;
                grk2 = grk2 * rhx;
                gzz2 = Complex.Zero;
            }

            ezs_ = CONST1 * ((gz2 - gz1) * cs * xk - (gzp2 + gzp1) * ss);
            ezc_ = -CONST1 * ((gz2 + gz1) * ss * xk + (gzp2 - gzp1) * cs);
            ers_ = -CONST1 * ((z2a * grp2 + z1a * grp1 + gr2 + gr1) * ss - (z2a * gr2 - z1a * gr1) * cs * xk);
            erc_ = -CONST1 * ((z2a * grp2 - z1a * grp1 + gr2 - gr1) * cs + (z2a * gr2 + z1a * gr1) * ss * xk);
            erk_ = CONST1 * (grk2 - grk1);
            Intx(-shk, shk, rhk, ij, out cint, out sint);
            double bk = b * xk;
            double bk2 = bk * bk * .25;
            ezk_ = -CONST1 * (gzp2 - gzp1 + xk * xk * (1.0 - bk2) * new Complex(cint, -sint) - bk2 * (gzz2 - gzz1));
        }

        // contributi degli estremi del segmento, filo sottile
        static void Gx(double zz, double rh, double xk, out Complex gz, out Complex gzp)
        {
            double r2 = zz * zz + rh * rh;
            double r = Math.Sqrt(r2);
            double rkz = xk * r;
            gz = new Complex(Math.Cos(rkz), -Math.Sin(rkz)) / r;
            gzp = -new Complex(1.0, rkz) * gz / r2;
        }

        // contributi degli estremi del segmento, filo sottile esteso
        static void Gxx(double zz, double rh, double a, double a2, double xk, int ira,
                        out Complex g1, out Complex g1p, out Complex g2, out Complex g2p, out Complex g3, out Complex gzp)
        {
            double r2 = zz * zz + rh * rh;
            double r = Math.Sqrt(r2);
            double r4 = r2 * r2;
            double rk = xk * r;
            double rk2 = rk * rk;
            double rh2 = rh * rh;
            double t1 = .25 * a2 * rh2 / r4;
            double t2 = .5 * a2 / r2;
            Complex c1 = new Complex(1.0, rk);
            Complex c2 = 3.0 * c1 - rk2;
            Complex c3 = new Complex(6.0, rk) * rk2 - 15.0 * c1;
            Complex gz = new Complex(Math.Cos(rk), -Math.Sin(rk)) / r;
            g2 = gz * (1.0 + t1 * c2);
            g1 = g2 - t2 * c1 * gz;
            gz = gz / r2;
            g2p = gz * (t1 * c3 - c1);
            gzp = t2 * c2 * gz;
            g3 = g2p + gzp;
            g1p = g3 * zz;

            if (ira != 1)
            {
                g3 = (g3 + gzp) * rh;
                gzp = -zz * c1 * gz;
                if (rh <= 1.0e-10)
                {
                    g2 = Complex.Zero;
                    g2p = Complex.Zero;
                    return;
                }
                g2 = g2 / rh;
                g2p = g2p * zz / rh;
                return;
            }
            t2 = .5 * a;
            g2 = -t2 * c1 * gz;
            g2p = t2 * gz * c2 / r2;
            g3 = rh2 * g2p - a * gz * c1;
            g2p = g2p * zz;
            gzp = -zz * c1 * gz;
        }
    }
}
