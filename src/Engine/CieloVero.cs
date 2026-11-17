// YA-NEC 1.1.0 - "Rumore dal cielo vero": temperatura d'antenna con la mappa vera del cielo (bozzetto approvato da IC8TEM il 17/11/2026 19:17).
// Calcolo portato in C# da skynoise 0.2.2 di John C. Sager G8ONH (licenza MIT, data/cielo/LICENCE-skynoise.txt): il diagramma 3D
// dell'antenna si ruota sul puntamento (azimut, elevazione); ogni direzione sopra l'orizzonte prende la temperatura della mappa nel punto
// di ascensione retta e declinazione che vede dal QTH all'ora data, sotto l'orizzonte la temperatura del terreno; la media e' pesata sul
// guadagno (somma di T G dOmega / somma di G dOmega, come skynoise e come la correzione KF2YN).
// Somiglianze con skynoise: stesse mappe (Haslam 1982 rielaborata da Remazeilles 2015, scalate da Sager a 144 e 432 MHz con esponente
// 2,56), stesso tempo siderale (Kaplan, USNO Circular 179, eq. 2.11-2.12 con i termini Y0-Y2), stessa media pesata, terreno a 290 K.
// Differenze: l'interpolazione HEALPix e' quella bilineare ad anelli di healpy (get_interp_weights), non quella di astropy-healpix (scarti
// di pochi kelvin dove la mappa cambia in fretta); il diagramma viene dal motore di YA-NEC (griglia di Rumore e G/T), non da un file di
// uscita NEC; alle frequenze diverse da 144 e 432 MHz si scala la mappa piu' vicina con lo stesso esponente; la Luna si calcola con le
// serie ridotte di Meeus (Astronomical Algorithms cap. 47, circa 0,3 gradi), al posto di astronomy-engine usato da moontrack.
using System;
using System.Collections.Generic;
using System.IO;

namespace YaNec.Engine
{
    /// <summary>Mappa HEALPix (RING) della temperatura del cielo in coordinate equatoriali.</summary>
    public sealed class MappaCielo
    {
        public int Nside;
        public float[] T;
        public double FreqMHz;
        public string Origine;

        public static MappaCielo Carica(string path, double freqMHz)
        {
            byte[] b = File.ReadAllBytes(path);
            // intestazione primaria e dell'estensione: blocchi da 2880 byte che finiscono con la scheda END
            int pos = 0, nside = 0, npix = 0;
            for (int hdu = 0; hdu < 2; hdu++)
            {
                bool fine = false;
                while (!fine && pos + 2880 <= b.Length)
                {
                    for (int k = 0; k < 36; k++)
                    {
                        string card = System.Text.Encoding.ASCII.GetString(b, pos + k * 80, 80);
                        if (card.StartsWith("NSIDE ", StringComparison.Ordinal)) nside = int.Parse(card.Substring(10, 20).Trim());
                        if (card.StartsWith("NAXIS2 ", StringComparison.Ordinal) && hdu == 1) npix = int.Parse(card.Substring(10, 20).Trim());
                        if (card.StartsWith("ORDERING", StringComparison.Ordinal) && card.IndexOf("RING", StringComparison.Ordinal) < 0)
                            throw new InvalidDataException("Mappa del cielo non in ordine RING: " + path);
                        if (card.StartsWith("END ", StringComparison.Ordinal) || card.TrimEnd() == "END") fine = true;
                    }
                    pos += 2880;
                }
            }
            if (nside <= 0 || npix != 12 * nside * nside) throw new InvalidDataException("Mappa del cielo non valida: " + path);
            float[] t = new float[npix];
            byte[] q = new byte[4];
            for (int i = 0; i < npix; i++)
            {
                int o = pos + 4 * i;
                q[0] = b[o + 3]; q[1] = b[o + 2]; q[2] = b[o + 1]; q[3] = b[o];   // FITS: big-endian
                t[i] = BitConverter.ToSingle(q, 0);
            }
            return new MappaCielo { Nside = nside, T = t, FreqMHz = freqMHz, Origine = Path.GetFileName(path) };
        }

        // --- geometria HEALPix ad anelli (Gorski et al. 2005), come healpy ---
        void InfoAnello(int ir, out int start, out int npr, out double theta, out bool shifted)
        {
            int n = Nside, npix = 12 * n * n;
            double fact2 = 4.0 / npix, fact1 = (n << 1) * fact2;
            int northring = ir > 2 * n ? 4 * n - ir : ir;
            double z;
            if (northring < n)
            {
                double tmp = northring * northring * fact2;
                z = 1 - tmp;
                npr = northring * 4; shifted = true;
                start = 2 * northring * (northring - 1);
            }
            else
            {
                z = (2 * n - northring) * fact1;
                npr = 4 * n; shifted = ((northring - n) & 1) == 0;
                start = 2 * n * (n - 1) + (northring - n) * npr;
            }
            if (northring != ir) { z = -z; start = npix - start - npr; }
            theta = Math.Acos(Math.Max(-1, Math.Min(1, z)));
        }

        int AnelloSopra(double z)
        {
            double az = Math.Abs(z);
            if (az <= 2.0 / 3) return (int)(Nside * (2 - 1.5 * z));
            int ir = (int)(Nside * Math.Sqrt(3 * (1 - az)));
            return z > 0 ? ir : 4 * Nside - ir - 1;
        }

        /// <summary>Temperatura (K) alla frequenza della mappa, interpolata (bilineare ad anelli, healpy) in RA e declinazione (gradi).</summary>
        public double Temperatura(double raDeg, double decDeg)
        {
            double theta = (90 - decDeg) * Math.PI / 180, phi = ((raDeg % 360) + 360) % 360 * Math.PI / 180;
            int n = Nside, npix = 12 * n * n;
            double z = Math.Cos(theta);
            int ir1 = AnelloSopra(z), ir2 = ir1 + 1;
            int[] pix = new int[4];
            double[] w = new double[4];
            double theta1 = 0, theta2 = 0;
            if (ir1 > 0)
            {
                int sp, nr; bool sh;
                InfoAnello(ir1, out sp, out nr, out theta1, out sh);
                double dphi = 2 * Math.PI / nr, tmp = phi / dphi - 0.5 * (sh ? 1 : 0);
                int i1 = tmp < 0 ? (int)tmp - 1 : (int)tmp;
                double ww = tmp - i1;
                int i2 = i1 + 1;
                if (i1 < 0) i1 += nr;
                if (i2 >= nr) i2 -= nr;
                pix[0] = sp + i1; pix[1] = sp + i2; w[0] = 1 - ww; w[1] = ww;
            }
            if (ir2 < 4 * n)
            {
                int sp, nr; bool sh;
                InfoAnello(ir2, out sp, out nr, out theta2, out sh);
                double dphi = 2 * Math.PI / nr, tmp = phi / dphi - 0.5 * (sh ? 1 : 0);
                int i1 = tmp < 0 ? (int)tmp - 1 : (int)tmp;
                double ww = tmp - i1;
                int i2 = i1 + 1;
                if (i1 < 0) i1 += nr;
                if (i2 >= nr) i2 -= nr;
                pix[2] = sp + i1; pix[3] = sp + i2; w[2] = 1 - ww; w[3] = ww;
            }
            if (ir1 == 0)
            {
                double wt = theta / theta2;
                w[2] *= wt; w[3] *= wt;
                double fac = (1 - wt) * 0.25;
                w[0] = fac; w[1] = fac; w[2] += fac; w[3] += fac;
                pix[0] = (pix[2] + 2) & 3; pix[1] = (pix[3] + 2) & 3;
            }
            else if (ir2 == 4 * n)
            {
                double wt = (theta - theta1) / (Math.PI - theta1);
                w[0] *= 1 - wt; w[1] *= 1 - wt;
                double fac = wt * 0.25;
                w[0] += fac; w[1] += fac; w[2] = fac; w[3] = fac;
                pix[2] = ((pix[0] + 2) & 3) + npix - 4; pix[3] = ((pix[1] + 2) & 3) + npix - 4;
            }
            else
            {
                double wt = (theta - theta1) / (theta2 - theta1);
                w[0] *= 1 - wt; w[1] *= 1 - wt; w[2] *= wt; w[3] *= wt;
            }
            double s = 0, sw = 0;
            for (int k = 0; k < 4; k++)
            {
                double v = T[pix[k]];
                if (v < -1e20 || double.IsNaN(v)) continue;   // BAD_DATA
                s += w[k] * v; sw += w[k];
            }
            return sw > 0 ? s / sw : 0;
        }
    }

    public class CieloRisultato
    {
        public double Tpattern, CieloK, ViaLatteaK, TerrenoK, FrazCielo, FrazTerreno, RaBs, DecBs, Lmst, AzBs, ElBs;
    }

    public static class CieloVero
    {
        public const double EsponenteSpettrale = 2.56;   // G8ONH, dalla 408 MHz
        static readonly Dictionary<double, MappaCielo> mappe = new Dictionary<double, MappaCielo>();
        static readonly object lk = new object();

        /// <summary>Mappa a 144 o 432 MHz (la piu' vicina in scala logaritmica), dalla cartella data/cielo.</summary>
        public static MappaCielo Mappa(string cartella, double fMHz)
        {
            double fm = fMHz < Math.Sqrt(144.0 * 432.0) ? 144 : 432;
            lock (lk)
            {
                MappaCielo m;
                if (!mappe.TryGetValue(fm, out m))
                {
                    m = MappaCielo.Carica(Path.Combine(cartella, fm == 144 ? "SkyTemp144.fits" : "SkyTemp432.fits"), fm);
                    mappe[fm] = m;
                }
                return m;
            }
        }

        /// <summary>Fattore che porta la temperatura della mappa alla frequenza f: (fmappa / f)^2,56.</summary>
        public static double Scala(MappaCielo m, double fMHz) { return Math.Pow(m.FreqMHz / fMHz, EsponenteSpettrale); }

        // --- tempo siderale medio locale (skynoise: Kaplan 2005, USNO Circular 179, eq. 2.11 e 2.12 con Y0-Y2) ---
        static readonly DateTime J2000tt = new DateTime(2000, 1, 1, 11, 58, 55, 816, DateTimeKind.Utc);
        static readonly DateTime J2000u = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        public static double Lmst(DateTime utc, double lonDeg)
        {
            const double X0 = 0.7790572732640, X1 = 0.00273781191135448, Y0 = 0.014506, Y1 = 4612.156534, Y2 = 1.3915817;
            double dtt = (utc - J2000tt).TotalDays, tc = dtt / 36525.0;
            TimeSpan du = utc - J2000u;
            double dut = du.TotalDays;
            double h = dut - Math.Floor(dut);   // frazione di giorno (come skynoise: secondi del timedelta / 86400)
            double thetak = X0 + dut * X1 + h;
            double thetakd = (thetak - Math.Floor(thetak)) * 360.0;
            double ttcorrd = ((Y2 * tc + Y1) * tc + Y0) / 3600.0;
            double lmst = (thetakd + ttcorrd + lonDeg) % 360.0;
            return lmst < 0 ? lmst + 360 : lmst;
        }

        /// <summary>Angolo orario e declinazione (gradi) di una direzione locale (azimut da nord verso est, elevazione).</summary>
        public static void AzElAHaDec(double azDeg, double elDeg, double latDeg, out double haDeg, out double decDeg)
        {
            double az = azDeg * Math.PI / 180, el = elDeg * Math.PI / 180, la = latDeg * Math.PI / 180;
            double sd = Math.Sin(el) * Math.Sin(la) + Math.Cos(el) * Math.Cos(la) * Math.Cos(az);
            sd = Math.Max(-1, Math.Min(1, sd));
            double dec = Math.Asin(sd);
            double den = Math.Cos(la) * Math.Cos(dec);
            double ch = den == 0 ? 1 : (Math.Sin(el) - Math.Sin(la) * sd) / den;
            ch = Math.Max(-1, Math.Min(1, ch));
            double ha = Math.Acos(ch) * 180 / Math.PI;
            if (Math.Sin(az) > 0) ha = 360 - ha;   // verso est: angolo orario negativo
            haDeg = ha; decDeg = dec * 180 / Math.PI;
        }

        /// <summary>Azimut ed elevazione della Luna (topocentrica, serie ridotte di Meeus cap. 47, circa 0,3 gradi).</summary>
        public static void Luna(DateTime utc, double latDeg, double lonDeg, out double azDeg, out double elDeg)
        {
            double jd = utc.Subtract(new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)).TotalDays + 2451545.0;
            double T = (jd - 2451545.0) / 36525.0, R = Math.PI / 180;
            double Lp = 218.3164477 + 481267.88123421 * T, D = 297.8501921 + 445267.1114034 * T, M = 357.5291092 + 35999.0502909 * T;
            double Mp = 134.9633964 + 477198.8675055 * T, F = 93.2720950 + 483202.0175233 * T;
            double lon = Lp + 6.289 * Math.Sin(Mp * R) - 1.274 * Math.Sin((2 * D - Mp) * R) + 0.658 * Math.Sin(2 * D * R) + 0.214 * Math.Sin(2 * Mp * R)
                         - 0.186 * Math.Sin(M * R) - 0.114 * Math.Sin(2 * F * R);
            double lat = 5.128 * Math.Sin(F * R) + 0.281 * Math.Sin((Mp + F) * R) + 0.278 * Math.Sin((Mp - F) * R) + 0.173 * Math.Sin((2 * D - F) * R);
            double eps = (23.439291 - 0.0130042 * T) * R;
            double l = lon * R, b = lat * R;
            double ra = Math.Atan2(Math.Sin(l) * Math.Cos(eps) - Math.Tan(b) * Math.Sin(eps), Math.Cos(l)) / R;
            double dec = Math.Asin(Math.Sin(b) * Math.Cos(eps) + Math.Cos(b) * Math.Sin(eps) * Math.Sin(l)) / R;
            double ha = (Lmst(utc, lonDeg) - ra) * R, d = dec * R, la = latDeg * R;
            double se = Math.Sin(la) * Math.Sin(d) + Math.Cos(la) * Math.Cos(d) * Math.Cos(ha);
            double el = Math.Asin(Math.Max(-1, Math.Min(1, se)));
            double az = Math.Atan2(-Math.Cos(d) * Math.Sin(ha), Math.Cos(la) * Math.Sin(d) - Math.Sin(la) * Math.Cos(d) * Math.Cos(ha)) / R;
            azDeg = (az + 360) % 360;
            elDeg = el / R - 0.95 * Math.Cos(el);   // parallasse orizzontale media della Luna (0,95 gradi)
        }

        /// <summary>Latitudine e longitudine (gradi) dal locator Maidenhead (centro del quadrato); falso se il locator non e' valido.</summary>
        public static bool DaLocator(string loc, out double lat, out double lon)
        {
            lat = lon = 0;
            if (string.IsNullOrWhiteSpace(loc)) return false;
            string s = loc.Trim().ToUpperInvariant();
            if (s.Length < 4 || s.Length % 2 != 0) return false;
            if (s[0] < 'A' || s[0] > 'R' || s[1] < 'A' || s[1] > 'R' || !char.IsDigit(s[2]) || !char.IsDigit(s[3])) return false;
            lon = (s[0] - 'A') * 20 - 180 + (s[2] - '0') * 2;
            lat = (s[1] - 'A') * 10 - 90 + (s[3] - '0');
            double dl = 2, db = 1;
            if (s.Length >= 6)
            {
                if (s[4] < 'A' || s[4] > 'X' || s[5] < 'A' || s[5] > 'X') return false;
                lon += (s[4] - 'A') * 5.0 / 60; lat += (s[5] - 'A') * 2.5 / 60; dl = 5.0 / 60; db = 2.5 / 60;
            }
            lon += dl / 2; lat += db / 2;
            return true;
        }

        /// <summary>Temperatura dal diagramma con il cielo vero. Diagramma in spazio libero con il lobo verso (thetaFwd, phiFwd), puntato in
        /// azimut ed elevazione dal QTH all'ora data; sotto l'orizzonte tground. Via Lattea = parte dovuta alle zone piu' calde di
        /// 1000 K a 144 MHz (scalati alla frequenza).</summary>
        public static CieloRisultato Tpattern(PatternGrid g, double thetaFwd, double phiFwd, MappaCielo mappa, double fMHz,
            double latDeg, double lonDeg, DateTime utc, double azDeg, double elDeg, double tground)
        {
            double R = Math.PI / 180;
            double tf = thetaFwd * R, pf = phiFwd * R;
            double fx = Math.Sin(tf) * Math.Cos(pf), fy = Math.Sin(tf) * Math.Sin(pf), fz = Math.Cos(tf);
            double ux = -fz * fx, uy = -fz * fy, uz = 1 - fz * fz, ul = Math.Sqrt(ux * ux + uy * uy + uz * uz);
            if (ul < 1e-6) { ux = 1; uy = 0; uz = 0; ul = 1; }
            ux /= ul; uy /= ul; uz /= ul;
            double rx = fy * uz - fz * uy, ry = fz * ux - fx * uz, rz = fx * uy - fy * ux;   // destra = avanti x su
            // stessa terna nel cielo locale (est, nord, zenit)
            double a = azDeg * R, e = elDeg * R;
            double Fe = Math.Sin(a) * Math.Cos(e), Fn = Math.Cos(a) * Math.Cos(e), Fu = Math.Sin(e);
            double Ue = -Math.Sin(a) * Math.Sin(e), Un = -Math.Cos(a) * Math.Sin(e), Uu = Math.Cos(e);
            double Re = Fn * Uu - Fu * Un, Rn = Fu * Ue - Fe * Uu, Ru = Fe * Un - Fn * Ue;
            double lmst = Lmst(utc, lonDeg), k = Scala(mappa, fMHz), soglia = 1000 * Math.Pow(144.0 / fMHz, EsponenteSpettrale);
            double sum = 0, sumT = 0, sky = 0, gnd = 0, milky = 0, tSky = 0;
            double step = g.ThetaStep * R, dph = g.PhiStep * R;
            for (int i = 0; i < g.NTheta; i++)
            {
                double th = g.ThetaAt(i) * R;
                double a0 = Math.Max(0, th - step / 2), b0 = Math.Min(Math.PI, th + step / 2), w = (Math.Cos(a0) - Math.Cos(b0)) * dph;
                for (int j = 0; j < g.NPhi; j++)
                {
                    double gv = g.Total[i, j];
                    double gw = (gv < -900 ? 0 : Math.Pow(10, gv / 10)) * w;
                    if (gw <= 0) continue;
                    double ph = g.PhiAt(j) * R;
                    double x = Math.Sin(th) * Math.Cos(ph), y = Math.Sin(th) * Math.Sin(ph), z = Math.Cos(th);
                    double cf = x * fx + y * fy + z * fz, cu = x * ux + y * uy + z * uz, cr = x * rx + y * ry + z * rz;
                    double de = cf * Fe + cu * Ue + cr * Re, dn = cf * Fn + cu * Un + cr * Rn, du = cf * Fu + cu * Uu + cr * Ru;
                    sum += gw;
                    if (du <= 0) { sumT += gw * tground; gnd += gw; continue; }
                    double el = Math.Asin(Math.Min(1, du)) / R, az = Math.Atan2(de, dn) / R;
                    double ha, dec;
                    AzElAHaDec(az, el, latDeg, out ha, out dec);
                    double t = mappa.Temperatura(lmst - ha, dec) * k;
                    sumT += gw * t; sky += gw; tSky += gw * t;
                    if (t > soglia) milky += gw * t;
                }
            }
            CieloRisultato r = new CieloRisultato();
            if (sum <= 0) return r;
            r.Tpattern = sumT / sum;
            r.FrazCielo = sky / sum; r.FrazTerreno = gnd / sum;
            r.ViaLatteaK = milky / sum; r.CieloK = (tSky - milky) / sum; r.TerrenoK = gnd * tground / sum;
            double hb, db;
            AzElAHaDec(azDeg, elDeg, latDeg, out hb, out db);
            r.RaBs = ((lmst - hb) % 360 + 360) % 360; r.DecBs = db; r.Lmst = lmst; r.AzBs = azDeg; r.ElBs = elDeg;
            return r;
        }
    }
}
