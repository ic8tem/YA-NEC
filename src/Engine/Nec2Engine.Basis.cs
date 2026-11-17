// YA-NEC - funzioni di base della corrente (sbf, tbf, trio), traduzione fedele di calculations.c di nec2c. GPL-3.0.
using System;

namespace YaNec.Engine
{
    public sealed partial class Nec2Engine
    {
        static double Omc(double d, double cdh, double sdh)
        {
            if (d <= 0.015)
            {
                double omc = 4.0 * d * d;
                return ((1.3888889e-3 * omc - 4.1666666667e-2) * omc + .5) * omc;
            }
            return 1.0 - cdh * cdh + sdh * sdh;
        }

        // componente della funzione di base i sul segmento is
        void Sbf(int i, int iss, out double aa, out double bb, out double cc)
        {
            int ix, jsnoL, june, jcox, jcoxx, jend, iend, njun1 = 0, njun2;
            double d, sig, pp, sdh, cdh, sd, omc, aj, pm = 0, cd, ap, qp, qm, xxi;

            aa = 0.0; bb = 0.0; cc = 0.0;
            june = 0;
            jsnoL = 0;
            pp = 0.0;
            ix = i - 1;

            jcox = icon1[ix];
            if (jcox > PCHCON) jcox = i;
            jend = -1;
            iend = -1;
            sig = -1.0;

            do
            {
                if (jcox != 0)
                {
                    if (jcox < 0) jcox = -jcox;
                    else
                    {
                        sig = -sig;
                        jend = -jend;
                    }
                    jcoxx = jcox - 1;
                    jsnoL++;
                    d = PI * si[jcoxx];
                    sdh = Math.Sin(d);
                    cdh = Math.Cos(d);
                    sd = 2.0 * sdh * cdh;
                    omc = Omc(d, cdh, sdh);
                    { double gx, gy, gz; EstremoSeg(ix, iend, out gx, out gy, out gz); aj = AjAt(jcoxx, gx, gy, gz); }   // 1.2.0: NEC-4 opzionale
                    pp -= omc / sd * aj;

                    if (jcox == iss)
                    {
                        aa = aj / sd * sig;
                        bb = aj / (2.0 * cdh);
                        cc = -aj / (2.0 * sdh) * sig;
                        june = iend;
                    }

                    if (jcox != i)
                    {
                        if (jend != 1) jcox = icon1[jcoxx];
                        else jcox = icon2[jcoxx];
                        if (Math.Abs(jcox) != i)
                        {
                            if (jcox == 0) throw new NecException("SBF - errore di connessione del segmento " + i);
                            else continue;
                        }
                    }
                    else if (jcox == iss) bb = -bb;

                    if (iend == 1) break;
                }

                pm = -pp;
                pp = 0.0;
                njun1 = jsnoL;
                jcox = icon2[ix];
                if (jcox > PCHCON) jcox = i;
                jend = 1;
                iend = 1;
                sig = -1.0;
            } while (jcox != 0);

            njun2 = jsnoL - njun1;
            d = PI * si[ix];
            sdh = Math.Sin(d);
            cdh = Math.Cos(d);
            sd = 2.0 * sdh * cdh;
            cd = cdh * cdh - sdh * sdh;
            if (d <= 0.015)
            {
                omc = 4.0 * d * d;
                omc = ((1.3888889e-3 * omc - 4.1666666667e-2) * omc + .5) * omc;
            }
            else omc = 1.0 - cd;

            if (qGiunz == null) { ap = AjLog(ix); aj = ap; }
            else
            {
                double gx, gy, gz;
                EstremoSeg(ix, 1, out gx, out gy, out gz); ap = AjAt(ix, gx, gy, gz);
                EstremoSeg(ix, -1, out gx, out gy, out gz); aj = AjAt(ix, gx, gy, gz);
            }

            if (njun1 == 0)
            {
                if (njun2 == 0)
                {
                    aa = -1.0;
                    qp = PI * bi[ix];
                    xxi = qp * qp;
                    xxi = qp * (1.0 - .5 * xxi) / (1.0 - xxi);
                    cc = 1.0 / (cdh - xxi * sdh);
                    return;
                }
                qp = PI * bi[ix];
                xxi = qp * qp;
                xxi = qp * (1.0 - .5 * xxi) / (1.0 - xxi);
                qp = -(omc + xxi * sd) / (sd * (ap + xxi * pp) + cd * (xxi * ap - pp));
                if (june == 1)
                {
                    aa = -aa * qp;
                    bb = bb * qp;
                    cc = -cc * qp;
                    if (i != iss) return;
                }
                aa -= 1.0;
                d = cd - xxi * sd;
                bb += (sdh + ap * qp * (cdh - xxi * sdh)) / d;
                cc += (cdh + ap * qp * (sdh + xxi * cdh)) / d;
                return;
            }

            if (njun2 == 0)
            {
                qm = PI * bi[ix];
                xxi = qm * qm;
                xxi = qm * (1.0 - .5 * xxi) / (1.0 - xxi);
                qm = (omc + xxi * sd) / (sd * (aj - xxi * pm) + cd * (pm + xxi * aj));
                if (june == -1)
                {
                    aa = aa * qm;
                    bb = bb * qm;
                    cc = cc * qm;
                    if (i != iss) return;
                }
                aa -= 1.0;
                d = cd - xxi * sd;
                bb += (aj * qm * (cdh - xxi * sdh) - sdh) / d;
                cc += (cdh - aj * qm * (sdh + xxi * cdh)) / d;
                return;
            }

            qp = sd * (pm * pp + aj * ap) + cd * (pm * ap - pp * aj);
            qm = (ap * omc - pp * sd) / qp;
            qp = -(aj * omc + pm * sd) / qp;
            if (june != 0)
            {
                if (june < 0)
                {
                    aa = aa * qm;
                    bb = bb * qm;
                    cc = cc * qm;
                }
                else
                {
                    aa = -aa * qp;
                    bb = bb * qp;
                    cc = -cc * qp;
                }
                if (i != iss) return;
            }
            aa -= 1.0;
            bb += (aj * qm + ap * qp) * sdh / sd;
            cc += (aj * qm - ap * qp) * cdh / sd;
        }

        void GrowSegj(int need)
        {
            if (need < jco.Length) return;
            int sz = need + 8;
            Array.Resize(ref jco, sz);
            Array.Resize(ref ax, sz);
            Array.Resize(ref bx, sz);
            Array.Resize(ref cx, sz);
        }

        // funzione di base i
        void Tbf(int i, int icap)
        {
            int ix, jcox, jcoxx, jend, iend, njun1 = 0, njun2, jsnop, jsnox;
            double pp, sdh, cdh, sd, omc, aj, pm = 0, cd, ap, qp, qm, xxi, d, sig;

            jsno = 0;
            pp = 0.0;
            ix = i - 1;
            jcox = icon1[ix];
            if (jcox > PCHCON) jcox = i;
            jend = -1;
            iend = -1;
            sig = -1.0;

            do
            {
                if (jcox != 0)
                {
                    if (jcox < 0) jcox = -jcox;
                    else
                    {
                        sig = -sig;
                        jend = -jend;
                    }
                    jcoxx = jcox - 1;
                    jsno++;
                    GrowSegj(jsno + 1);
                    jsnox = jsno - 1;
                    jco[jsnox] = jcox;
                    d = PI * si[jcoxx];
                    sdh = Math.Sin(d);
                    cdh = Math.Cos(d);
                    sd = 2.0 * sdh * cdh;
                    omc = Omc(d, cdh, sdh);
                    { double gx, gy, gz; EstremoSeg(ix, iend, out gx, out gy, out gz); aj = AjAt(jcoxx, gx, gy, gz); }   // 1.2.0: NEC-4 opzionale
                    pp = pp - omc / sd * aj;
                    ax[jsnox] = aj / sd * sig;
                    bx[jsnox] = aj / (2.0 * cdh);
                    cx[jsnox] = -aj / (2.0 * sdh) * sig;

                    if (jcox != i)
                    {
                        if (jend == 1) jcox = icon2[jcoxx];
                        else jcox = icon1[jcoxx];
                        if (Math.Abs(jcox) != i)
                        {
                            if (jcox != 0) continue;
                            else throw new NecException("TBF - errore di connessione del segmento " + i);
                        }
                    }
                    else bx[jsnox] = -bx[jsnox];

                    if (iend == 1) break;
                }

                pm = -pp;
                pp = 0.0;
                njun1 = jsno;
                jcox = icon2[ix];
                if (jcox > PCHCON) jcox = i;
                jend = 1;
                iend = 1;
                sig = -1.0;
            } while (jcox != 0);

            njun2 = jsno - njun1;
            jsnop = jsno;
            GrowSegj(jsnop + 1);
            jco[jsnop] = i;
            d = PI * si[ix];
            sdh = Math.Sin(d);
            cdh = Math.Cos(d);
            sd = 2.0 * sdh * cdh;
            cd = cdh * cdh - sdh * sdh;
            if (d <= 0.015)
            {
                omc = 4.0 * d * d;
                omc = ((1.3888889e-3 * omc - 4.1666666667e-2) * omc + .5) * omc;
            }
            else omc = 1.0 - cd;

            if (qGiunz == null) { ap = AjLog(ix); aj = ap; }
            else
            {
                double gx, gy, gz;
                EstremoSeg(ix, 1, out gx, out gy, out gz); ap = AjAt(ix, gx, gy, gz);
                EstremoSeg(ix, -1, out gx, out gy, out gz); aj = AjAt(ix, gx, gy, gz);
            }

            if (njun1 == 0)
            {
                if (njun2 == 0)
                {
                    bx[jsnop] = 0.0;
                    if (icap == 0) xxi = 0.0;
                    else
                    {
                        qp = PI * bi[ix];
                        xxi = qp * qp;
                        xxi = qp * (1.0 - .5 * xxi) / (1.0 - xxi);
                    }
                    cx[jsnop] = 1.0 / (cdh - xxi * sdh);
                    jsno = jsnop + 1;
                    ax[jsnop] = -1.0;
                    return;
                }
                if (icap == 0) xxi = 0.0;
                else
                {
                    qp = PI * bi[ix];
                    xxi = qp * qp;
                    xxi = qp * (1.0 - .5 * xxi) / (1.0 - xxi);
                }
                qp = -(omc + xxi * sd) / (sd * (ap + xxi * pp) + cd * (xxi * ap - pp));
                d = cd - xxi * sd;
                bx[jsnop] = (sdh + ap * qp * (cdh - xxi * sdh)) / d;
                cx[jsnop] = (cdh + ap * qp * (sdh + xxi * cdh)) / d;
                for (iend = 0; iend < njun2; iend++)
                {
                    ax[iend] = -ax[iend] * qp;
                    bx[iend] = bx[iend] * qp;
                    cx[iend] = -cx[iend] * qp;
                }
                jsno = jsnop + 1;
                ax[jsnop] = -1.0;
                return;
            }

            if (njun2 == 0)
            {
                if (icap == 0) xxi = 0.0;
                else
                {
                    qm = PI * bi[ix];
                    xxi = qm * qm;
                    xxi = qm * (1.0 - .5 * xxi) / (1.0 - xxi);
                }
                qm = (omc + xxi * sd) / (sd * (aj - xxi * pm) + cd * (pm + xxi * aj));
                d = cd - xxi * sd;
                bx[jsnop] = (aj * qm * (cdh - xxi * sdh) - sdh) / d;
                cx[jsnop] = (cdh - aj * qm * (sdh + xxi * cdh)) / d;
                for (iend = 0; iend < njun1; iend++)
                {
                    ax[iend] = ax[iend] * qm;
                    bx[iend] = bx[iend] * qm;
                    cx[iend] = cx[iend] * qm;
                }
                jsno = jsnop + 1;
                ax[jsnop] = -1.0;
                return;
            }

            qp = sd * (pm * pp + aj * ap) + cd * (pm * ap - pp * aj);
            qm = (ap * omc - pp * sd) / qp;
            qp = -(aj * omc + pm * sd) / qp;
            bx[jsnop] = (aj * qm + ap * qp) * sdh / sd;
            cx[jsnop] = (aj * qm - ap * qp) * cdh / sd;
            for (iend = 0; iend < njun1; iend++)
            {
                ax[iend] = ax[iend] * qm;
                bx[iend] = bx[iend] * qm;
                cx[iend] = cx[iend] * qm;
            }
            jend = njun1;
            for (iend = jend; iend < jsno; iend++)
            {
                ax[iend] = -ax[iend] * qp;
                bx[iend] = bx[iend] * qp;
                cx[iend] = -cx[iend] * qp;
            }
            jsno = jsnop + 1;
            ax[jsnop] = -1.0;
        }

        // componenti di tutte le funzioni di base sul segmento j
        void Trio(int j)
        {
            int jcox, jcoxx, jsnox, jx, jend = 0, iend = 0;
            double a, b, c;

            jsno = 0;
            jx = j - 1;
            jcox = icon1[jx];
            if (jcox <= PCHCON)
            {
                jend = -1;
                iend = -1;
            }
            if ((jcox == 0) || (jcox > PCHCON))
            {
                jcox = icon2[jx];
                if (jcox <= PCHCON)
                {
                    jend = 1;
                    iend = 1;
                }
                if (jcox == 0 || (jcox > PCHCON))
                {
                    jsnox = jsno;
                    jsno++;
                    GrowSegj(jsno + 1);
                    Sbf(j, j, out a, out b, out c);
                    ax[jsnox] = a; bx[jsnox] = b; cx[jsnox] = c;
                    jco[jsnox] = j;
                    return;
                }
            }

            do
            {
                if (jcox < 0) jcox = -jcox;
                else jend = -jend;
                jcoxx = jcox - 1;

                if (jcox != j)
                {
                    jsnox = jsno;
                    jsno++;
                    GrowSegj(jsno + 1);
                    Sbf(jcox, j, out a, out b, out c);
                    ax[jsnox] = a; bx[jsnox] = b; cx[jsnox] = c;
                    jco[jsnox] = jcox;
                    if (jend != 1) jcox = icon1[jcoxx];
                    else jcox = icon2[jcoxx];
                    if (jcox == 0) throw new NecException("TRIO - errore di connessione del segmento " + j);
                    else continue;
                }

                if (iend == 1) break;
                jcox = icon2[jx];
                if (jcox > PCHCON) break;
                jend = 1;
                iend = 1;
            } while (jcox != 0);

            jsnox = jsno;
            jsno++;
            GrowSegj(jsno + 1);
            Sbf(j, j, out a, out b, out c);
            ax[jsnox] = a; bx[jsnox] = b; cx[jsnox] = c;
            jco[jsnox] = j;
        }
    }
}
