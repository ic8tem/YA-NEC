// YA-NEC - campo lontano (ffld) e guadagni, traduzione fedele di radiation.c di nec2c (solo fili). GPL-3.0.
using System;
using System.Numerics;

namespace YaNec.Engine
{
    public struct FarFieldPoint
    {
        public double ThetaDeg, PhiDeg;
        public double GainTotal, GainVert, GainHor;   // dBi (guadagno di potenza)
        public double AxialRatio, TiltDeg;
        public int Sense;                             // 0 lineare, 1 destra, 2 sinistra
    }

    public sealed partial class Nec2Engine
    {
        double[] sinEl, cosEl;   // sin e cos di pi * lunghezza del segmento (in lunghezze d'onda), preparati alla fine di Solve

        void PreparaCampoLontano()
        {
            sinEl = new double[n]; cosEl = new double[n];
            for (int i = 0; i < n; i++) { sinEl[i] = Math.Sin(PI * si[i]); cosEl[i] = Math.Cos(PI * si[i]); }
        }

        /// <summary>Campi lontani E(theta), E(phi), senza il fattore exp(jkr)/(r/lambda).</summary>
        public void Ffld(double thet, double phi, out Complex eth, out Complex eph) { FfldK(thet, phi, ksymp, out eth, out eph); }
        /// <summary>1.2.0: solo il campo diretto delle correnti (senza l'immagine nel suolo), per il terreno reale.</summary>
        public void FfldDiretto(double thet, double phi, out Complex eth, out Complex eph) { FfldK(thet, phi, 1, out eth, out eph); }

        void FfldK(double thet, double phi, int kmax, out Complex eth, out Complex eph) { FfldK(thet, phi, kmax, null, out eth, out eph); }

        /// <summary>1.6.0 (Cavo e balun, proposta 5): parte della potenza irradiata che viene dai soli segmenti con mask[i] vero: integrale di
        /// |E|^2 del campo delle sole correnti scelte diviso per l'integrale del campo di tutte, sulla sfera (sulla semisfera superiore con il
        /// terreno), passo 5 gradi in theta e 10 in phi, peso sin(theta). Le superfici (patch) non entrano nel campo della maschera.</summary>
        public double FrazioneIrradiata(bool[] mask)
        {
            double tutto = 0, parte = 0;
            int thMax = ksymp == 2 ? 90 : 180;
            for (int it = 0; it < thMax / 5; it++)
            {
                double th = (it * 5 + 2.5) * TA, w = Math.Sin(th);
                for (int ip = 0; ip < 36; ip++)
                {
                    double ph = (ip * 10 + 5) * TA;
                    Complex a, b, c, d;
                    FfldK(th, ph, ksymp, null, out a, out b);
                    FfldK(th, ph, ksymp, mask, out c, out d);
                    tutto += w * ((a * Complex.Conjugate(a)).Real + (b * Complex.Conjugate(b)).Real);
                    parte += w * ((c * Complex.Conjugate(c)).Real + (d * Complex.Conjugate(d)).Real);
                }
            }
            return tutto > 0 ? parte / tutto : 0;
        }

        void FfldK(double thet, double phi, int kmax, bool[] mask, out Complex eth, out Complex eph)
        {
            double phx = -Math.Sin(phi);
            double phy = Math.Cos(phi);
            double roz = Math.Cos(thet);
            double thx = roz * phy;
            double thy = -roz * phx;
            double thz = -Math.Sin(thet);
            double rox = -thz * phy;
            double roy = thz * phx;
            Complex cix = Complex.Zero, ciy = Complex.Zero, ciz = Complex.Zero;
            Complex ccx = Complex.Zero, ccy = Complex.Zero, ccz = Complex.Zero;
            Complex rrv = Complex.Zero, rrh = Complex.Zero, rrv1 = Complex.Zero, rrh1 = Complex.Zero;
            // 0.4.30: schermo di radiali nel campo lontano (RP con I1 = 4 di nec2c): coefficiente dello schermo dentro il cerchio,
            // del terreno fuori; il punto speculare di ogni segmento immagine decide
            bool schermo = nradl != 0 && iperf != 1;
            double tthet = Math.Tan(thet);

            for (int k = 0; k < kmax; k++)
            {
                if (k != 0)
                {
                    if (iperf == 1)
                    {
                        rrv = -Complex.One;
                        rrh = -Complex.One;
                    }
                    else
                    {
                        Complex zrsin = Complex.Sqrt(1.0 - zrati * zrati * thz * thz);
                        rrv = -(roz - zrati * zrsin) / (roz + zrati * zrsin);
                        rrh = (zrati * roz - zrsin) / (zrati * roz + zrsin);
                    }
                    rrv1 = rrv; rrh1 = rrh;
                    roz = -roz;
                    ccx = cix; ccy = ciy; ccz = ciz;
                }
                cix = Complex.Zero; ciy = Complex.Zero; ciz = Complex.Zero;

                double[] sEl = sinEl, cEl = cosEl;
                for (int i = 0; i < n; i++)
                {
                    if (mask != null && (i >= mask.Length || !mask[i])) continue;
                    double omega = -(rox * cab[i] + roy * sab[i] + roz * salp[i]);
                    double el = PI * si[i];
                    double sill = omega * el;
                    double top = el + sill;
                    double bot = el - sill;
                    double a, too, boo;
                    // dalla 0.4.2 (approvato il 13/11/2026): sin(el +- sill) = sin el cos sill +- cos el sin sill, con sin el e cos el
                    // calcolati una volta per segmento: due funzioni trigonometriche per punto invece di quattro (stessi valori
                    // entro gli arrotondamenti; le formule per argomenti piccoli restano quelle di nec2c)
                    (double ssill, double csill) = Math.SinCos(sill);
                    if (Math.Abs(omega) >= 1.0e-7) a = 2.0 * ssill / omega;
                    else a = (2.0 - omega * omega * el * el / 3.0) * el;
                    if (Math.Abs(top) >= 1.0e-7) too = (sEl[i] * csill + cEl[i] * ssill) / top;
                    else too = 1.0 - top * top / 6.0;
                    if (Math.Abs(bot) >= 1.0e-7) boo = (sEl[i] * csill - cEl[i] * ssill) / bot;
                    else boo = 1.0 - bot * bot / 6.0;
                    double b = el * (boo - too);
                    double c = el * (boo + too);
                    double rr = a * air[i] + b * bii[i] + c * cir[i];
                    double ri = a * aii[i] - b * bir[i] + c * cii[i];
                    double arg = TP * (x[i] * rox + y[i] * roy + z[i] * roz);
                    (double sarg, double carg) = Math.SinCos(arg);
                    Complex exa = new Complex(carg, sarg) * new Complex(rr, ri);
                    if (k == 1 && schermo)
                    {
                        double dr = z[i] * tthet, d = dr * phy + x[i];
                        d = Math.Sqrt(d * d + (y[i] - dr * phx) * (y[i] - dr * phx));
                        if (scrwl - d >= 0.0)
                        {
                            d = d + gsT2;
                            Complex zscrn = gsT1 * d * Math.Log(d / gsT2);
                            zscrn = (zscrn * zrati) / (ETA * zrati + zscrn);
                            Complex zrs = Complex.Sqrt(1.0 - zscrn * zscrn * thz * thz);
                            rrv = (roz + zscrn * zrs) / (-roz + zscrn * zrs);
                            rrh = (zscrn * roz + zrs) / (zscrn * roz - zrs);
                        }
                        else { rrv = rrv1; rrh = rrh1; }
                        Complex tix = exa * cab[i], tiy = exa * sab[i], tiz = exa * salp[i];
                        Complex cdq = (tix * phx + tiy * phy) * (rrh - rrv);
                        cix = cix + tix * rrv + cdq * phx;
                        ciy = ciy + tiy * rrv + cdq * phy;
                        ciz = ciz - tiz * rrv;
                        continue;
                    }
                    cix = cix + exa * cab[i];
                    ciy = ciy + exa * sab[i];
                    ciz = ciz + exa * salp[i];
                }

                if (k == 0) continue;
                if (schermo) { cix = cix + ccx; ciy = ciy + ccy; ciz = ciz + ccz; continue; }
                // contributo dell'immagine per terreno infinito
                Complex cdp = (cix * phx + ciy * phy) * (rrh - rrv);
                cix = ccx + cix * rrv + cdp * phx;
                ciy = ccy + ciy * rrv + cdp * phy;
                ciz = ccz - ciz * rrv;
            }
            eth = (cix * thx + ciy * thy + ciz * thz) * CONST3;
            eph = (cix * phx + ciy * phy) * CONST3;
            if (m > 0 && mask == null)
            {
                // 1.5.9: campo delle superfici (fflds di nec2c)
                Complex sth, sph;
                CampoSuperfici(thx, thy, thz, phx, phy, rox, roy, roz, kmax, out sth, out sph);
                eth += sth; eph += sph;
            }
        }

        /// <summary>Costante di guadagno di potenza (rdpat: gcop) = lambda^2 * 2pi / (376.73 * Pin).</summary>
        public double GainConstant
        {
            get { return InputPower > 0 ? wlam * wlam * 2.0 * PI / (376.73 * InputPower) : 0; }
        }

        static double Db10(double v) { return v < 1.0e-20 ? -999.99 : 10.0 * Math.Log10(v); }

        /// <summary>Guadagno di potenza (dBi) totale, verticale (theta) e orizzontale (phi) in una direzione.</summary>
        public FarFieldPoint Gain(double thetaDeg, double phiDeg)
        {
            Complex eth, eph;
            Ffld(thetaDeg * TA, phiDeg * TA, out eth, out eph);
            double gcon = GainConstant;
            double ethm2 = (eth * Complex.Conjugate(eth)).Real;
            double ephm2 = (eph * Complex.Conjugate(eph)).Real;
            FarFieldPoint p = new FarFieldPoint();
            p.ThetaDeg = thetaDeg;
            p.PhiDeg = phiDeg;
            p.GainVert = Db10(gcon * ethm2);
            p.GainHor = Db10(gcon * ephm2);
            p.GainTotal = Db10(gcon * (ethm2 + ephm2));
            // polarizzazione ellittica (come rdpat)
            if (ethm2 > 1.0e-20 || ephm2 > 1.0e-20)
            {
                double ethm = Math.Sqrt(ethm2), ephm = Math.Sqrt(ephm2);
                double etha = Math.Atan2(eth.Imaginary, eth.Real) * TD, epha = Math.Atan2(eph.Imaginary, eph.Real) * TD;
                double dfaz = epha - etha;
                double dfaz2 = epha >= 0.0 ? dfaz - 360.0 : dfaz + 360.0;
                if (Math.Abs(dfaz) > Math.Abs(dfaz2)) dfaz = dfaz2;
                double cdfaz = Math.Cos(dfaz * TA);
                double tstor1 = ethm2 - ephm2;
                double tstor2 = 2.0 * ephm * ethm * cdfaz;
                double tilta = .5 * Math.Atan2(tstor2, tstor1);
                double stilta = Math.Sin(tilta);
                tstor1 = tstor1 * stilta * stilta;
                tstor2 = tstor2 * stilta * Math.Cos(tilta);
                double emajr2 = -tstor1 + tstor2 + ethm2;
                double eminr2 = tstor1 - tstor2 + ephm2;
                if (eminr2 < 0.0) eminr2 = 0.0;
                p.AxialRatio = emajr2 > 0 ? Math.Sqrt(eminr2 / emajr2) : 0;
                p.TiltDeg = tilta * TD;
                p.Sense = p.AxialRatio <= 1.0e-5 ? 0 : (dfaz <= 0.0 ? 1 : 2);
            }
            return p;
        }

        /// <summary>Guadagno totale di potenza come rapporto lineare (non in dB) in una direzione.</summary>
        public double GainLinear(double thetaDeg, double phiDeg)
        {
            Complex eth, eph;
            Ffld(thetaDeg * TA, phiDeg * TA, out eth, out eph);
            return GainConstant * ((eth * Complex.Conjugate(eth)).Real + (eph * Complex.Conjugate(eph)).Real);
        }

        public bool HasGround { get { return ksymp == 2; } }
    }
}
