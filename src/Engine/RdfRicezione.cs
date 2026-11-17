// YA-NEC 1.2.0 - Diagrammi > "Ricezione (RDF)" (bozzetto HF 3 approvato da IC8TEM il 17/11/2026): cifre di merito in ricezione calcolate
// dal diagramma 3D del NEC.
//  - RDF = guadagno nella direzione voluta meno guadagno medio (emisfero superiore / 4 pi con il terreno, sfera / 4 pi in spazio libero):
//    convenzione gia' approvata (EZNEC, IV3PRK). Non dipende dalle perdite. Antenna isotropa: 0 dB.
//  - DMF: media del guadagno nel settore avanti (azimut +-30 gradi, elevazione 10-40 gradi) meno la media sull'emisfero posteriore
//    (ON4UN, Low-Band DXing cap. 7, esteso dal punto a 20 gradi al settore del bozzetto).
//  - F/B e F/R medio (emisfero posteriore), rumore d'antenna ai morsetti dalla temperatura d'antenna Ta = Text x guadagno medio
//    (ITU-R P.372: Fa sopra kT0), margine sul ricevitore.
//  - Antenne del bozzetto senza un generatore nei Calcolatori: K9AY, 4-square in ricezione e 8 verticali in cerchio (modelli semplificati,
//    fumetto giallo; valori di letteratura W8JI / Hi-Z / ON4UN nella tabella di confronto).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YaNec.Engine
{
    public static class RdfRicezione
    {
        const double C0 = 299.792458;
        static string F(double v) { return v.ToString("0.######", CultureInfo.InvariantCulture); }
        static string Gn(GroundSpec g) { return g == null || g.Kind == GroundKind.Perfect ? "GN 1" : "GN 2 0 0 0 " + F(g.Epsr) + " " + F(g.Sigma); }
        static void Rot(double az, ref double x, ref double y)
        {
            double a = az * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a), xx = x * c - y * s, yy = x * s + y * c;
            x = xx; y = yy;
        }
        static string Gw(int tag, int n, double x1, double y1, double z1, double x2, double y2, double z2, double r, double az)
        {
            Rot(az, ref x1, ref y1); Rot(az, ref x2, ref y2);
            return "GW " + tag + " " + n + " " + F(x1) + " " + F(y1) + " " + F(z1) + " " + F(x2) + " " + F(y2) + " " + F(z2) + " " + F(r);
        }

        /// <summary>K9AY semplificata: un loop a delta verticale (apice 7,6 m, base 9,2 m a 1 m dal suolo), sorgente a un angolo in basso e
        /// resistenza (390 ohm) all'altro; la meta' del K9AY con i due loop a 90 gradi, quella rivolta verso la direzione voluta.</summary>
        public static string K9ay(RxParams p, bool inverti) { return K9ay(p, inverti, 7.6, 4.6, 1.0); }

        /// <summary>1.3.0 (IC8TEM 18/11/2026 00:41, Calcolatori > Antenne riceventi): K9AY con le misure della scheda (apice h, mezza base b,
        /// base a zb dal suolo). Il loop resta chiuso sopra il suolo, come nella pagina Ricezione (RDF).</summary>
        public static string K9ay(RxParams p, bool inverti, double h, double b, double zb)
        {
            double r = 0.0008, lam = C0 / p.F;
            int nd = Math.Max(7, (int)Math.Ceiling(Math.Sqrt(b * b + (h - zb) * (h - zb)) / (lam / 80))), nb = Math.Max(9, (int)Math.Ceiling(2 * b / (lam / 80)));
            if (nb % 2 == 0) nb++;
            double s = inverti ? -1 : 1;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM K9AY (un loop, modello semplificato) " + F(p.F) + " MHz, resistenza " + F(p.Rterm) + " ohm (YA-NEC, Ricezione RDF)");
            sb.AppendLine("CE");
            sb.AppendLine(Gw(1, nd, -s * b, 0, zb, 0, 0, h, r, p.Dir));
            sb.AppendLine(Gw(2, nd, 0, 0, h, s * b, 0, zb, r, p.Dir));
            sb.AppendLine(Gw(3, nb, s * b, 0, zb, -s * b, 0, zb, r, p.Dir));
            sb.AppendLine("GE 0");
            sb.AppendLine(Gn(p.Ground));
            sb.AppendLine("LD 4 3 2 2 " + F(p.Rterm) + " 0");
            sb.AppendLine("EX 0 3 " + (nb - 1) + " 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        /// <summary>Schiera di verticali corte in ricezione con fase per un nullo posteriore esatto: ogni fila avanti di d e' sfasata di pi + beta d.
        /// pos = (x, y) in metri nel sistema dell'antenna (avanti = +x); file = indice della fila (0 = dietro).</summary>
        static string Verticali(RxParams p, List<double[]> pos, double passo, double hv, string nome, bool inverti)
        {
            double lam = C0 / p.F, beta = 2 * Math.PI / lam, r = 0.006;
            int ns = Math.Max(5, (int)Math.Ceiling(hv / (lam / 80)));
            double delta = (double.IsNaN(p.Fase) ? Math.PI + beta * passo : p.Fase * Math.PI / 180) * (inverti ? -1 : 1);   // 1.3.0: fase scelta nella scheda
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM " + nome + " " + F(p.F) + " MHz (verticali corte attive, fase per il nullo posteriore; YA-NEC, Ricezione RDF)");
            sb.AppendLine("CE");
            for (int i = 0; i < pos.Count; i++) sb.AppendLine(Gw(i + 1, ns, pos[i][0], pos[i][1], 0, pos[i][0], pos[i][1], hv, r, p.Dir));
            sb.AppendLine("GE 1");
            sb.AppendLine(Gn(p.Ground));
            double xmin = double.MaxValue; foreach (double[] q in pos) xmin = Math.Min(xmin, q[0]);
            for (int i = 0; i < pos.Count; i++)
            {
                double fila = Math.Round((pos[i][0] - xmin) / passo);
                double ph = fila * delta;
                sb.AppendLine("EX 0 " + (i + 1) + " 1 0 " + F(Math.Cos(ph)) + " " + F(Math.Sin(ph)));
            }
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        /// <summary>4-square in ricezione: quadrato di lato lambda/8, verticali da 7,6 m, fuoco lungo la diagonale (fase dietro / lati / davanti).</summary>
        public static string FourSquareRx(RxParams p, bool inverti) { return FourSquareRx(p, inverti, C0 / p.F / 8, 7.6); }

        /// <summary>1.3.0: 4-square in ricezione con il lato e l'altezza delle verticali della scheda.</summary>
        public static string FourSquareRx(RxParams p, bool inverti, double s, double hv)
        {
            double d = s / Math.Sqrt(2);
            List<double[]> pos = new List<double[]> { new[] { -d, 0.0 }, new[] { 0.0, d }, new[] { 0.0, -d }, new[] { d, 0.0 } };
            return Verticali(p, pos, d, hv, "4-square in ricezione, lato " + F(Math.Round(s, 1)) + " m", inverti);
        }

        /// <summary>1.3.0: due verticali corte in end-fire (cardioide), distanza d lungo la direzione voluta, nullo posteriore esatto.</summary>
        public static string Cardioide2(RxParams p, bool inverti, double d, double hv)
        {
            List<double[]> pos = new List<double[]> { new[] { -d / 2, 0.0 }, new[] { d / 2, 0.0 } };
            return Verticali(p, pos, d, hv, "2 verticali in end-fire (cardioide), distanza " + F(Math.Round(d, 1)) + " m", inverti);
        }

        /// <summary>1.3.0: tre verticali corte ai vertici di un triangolo equilatero di lato s, un vertice verso la direzione voluta:
        /// la coppia dietro in fase, il vertice davanti sfasato per il nullo posteriore.</summary>
        public static string Triangolo3(RxParams p, bool inverti, double s, double hv)
        {
            double hgt = s * Math.Sqrt(3) / 2;
            List<double[]> pos = new List<double[]> { new[] { -hgt / 3, s / 2 }, new[] { -hgt / 3, -s / 2 }, new[] { 2 * hgt / 3, 0.0 } };
            return Verticali(p, pos, hgt, hv, "3 verticali a triangolo, lato " + F(Math.Round(s, 1)) + " m", inverti);
        }

        /// <summary>8 verticali in cerchio (diametro 0,37 lambda): le quattro verso la direzione voluta e quella opposta, coppie broadside in fase
        /// e coppie davanti / dietro in end-fire; le altre quattro non si mettono nel modello (amplificatori ad alta impedenza, scollegate).</summary>
        public static string Cerchio8(RxParams p, bool inverti) { return Cerchio8(p, inverti, 0.37 * C0 / p.F, 7.6); }

        /// <summary>1.3.0: 8 verticali in cerchio con il diametro e l'altezza della scheda.</summary>
        public static string Cerchio8(RxParams p, bool inverti, double diametro, double hv)
        {
            double R = diametro / 2, a = 22.5 * Math.PI / 180;
            List<double[]> pos = new List<double[]>
            {
                new[] { -R * Math.Cos(a), R * Math.Sin(a) }, new[] { -R * Math.Cos(a), -R * Math.Sin(a) },
                new[] { R * Math.Cos(a), R * Math.Sin(a) }, new[] { R * Math.Cos(a), -R * Math.Sin(a) },
            };
            return Verticali(p, pos, 2 * R * Math.Cos(a), hv, "8 verticali in cerchio, diametro " + F(Math.Round(2 * R, 1)) + " m", inverti);
        }

        // ------------------------------------------------------------------------------------------------
        // cifre di merito
        // ------------------------------------------------------------------------------------------------
        public class Esito
        {
            public double G, Avg, Rdf, Dmf, FB, FRmedio, BackAvg, TaRel, PnDbm, Margine, RdfRumore;
            public double[] Az = new double[360];     // guadagno (dBi) in azimut all'elevazione scelta
            public double AzMax;
        }

        /// <summary>Fa (dB sopra kT0) del rumore esterno: 0 rurale tranquillo, 1 rurale, 2 residenziale, 3 citta' (ITU-R P.372, rumore artificiale).</summary>
        public static double Fa(double fmhz, int ambiente)
        {
            double[] c = { 53.6, 67.2, 72.5, 76.8 }, d = { 28.6, 27.7, 27.7, 27.7 };
            int k = Math.Max(0, Math.Min(3, ambiente));
            return c[k] - d[k] * Math.Log10(fmhz);
        }
        public static readonly string[] Ambienti = { "rurale tranquillo", "rurale", "residenziale", "citta'" };

        /// <summary>
        /// Cifre di merito dal motore risolto. az, el = direzione voluta (gradi; azimut come phi del NEC). rumore: 0 isotropo, 1 dal piano
        /// (peso exp(-el/10 gradi): rumore che arriva basso, ITU-R P.372 per il rumore artificiale lontano), 2 sorgente locale all'azimut azLoc
        /// (puntiforme a 5 gradi di elevazione, con la stessa potenza del fondo isotropo).
        /// </summary>
        public static Esito Calcola(Nec2Engine e, double fmhz, double az, double el, int rumore, double azLoc, double bandaHz, int ambiente, double nf, double preamp)
        {
            Esito r = new Esito();
            bool gnd = e.HasGround;
            double dth = 2, dph = 3;
            double sum = 0, sumN = 0, wN = 0, sb = 0, wb = 0, ss = 0, ws = 0;
            int elMin = gnd ? 0 : -90;
            for (double e2 = elMin + dth / 2; e2 < 90; e2 += dth)
            {
                double dOm = Math.Cos(e2 * Math.PI / 180) * (dth * Math.PI / 180) * (dph * Math.PI / 180);
                for (double ph = 0; ph < 360; ph += dph)
                {
                    double g = Lin(e.Gain(90 - e2, ph).GainTotal);
                    sum += g * dOm;
                    double dAz = Math.Abs(((ph - az) % 360 + 540) % 360 - 180);
                    if (e2 > 0)
                    {
                        double wn = rumore == 1 ? Math.Exp(-e2 / 10) : 1;
                        sumN += g * wn * dOm; wN += wn * dOm;
                        if (dAz >= 90) { sb += g * dOm; wb += dOm; }
                        if (dAz <= 30 && e2 >= 10 && e2 <= 40) { ss += g * dOm; ws += dOm; }
                    }
                }
            }
            r.Avg = Db(sum / (4 * Math.PI));
            r.G = e.Gain(90 - el, az).GainTotal;
            r.Rdf = r.G - r.Avg;
            r.BackAvg = Db(wb > 0 ? sb / wb : 0);
            r.Dmf = Db(ws > 0 ? ss / ws : 0) - r.BackAvg;
            r.FB = r.G - e.Gain(90 - el, (az + 180) % 360).GainTotal;
            r.FRmedio = r.G - r.BackAvg;
            for (int k = 0; k < 360; k++) r.Az[k] = e.Gain(90 - el, k).GainTotal;
            r.AzMax = -999; foreach (double v in r.Az) r.AzMax = Math.Max(r.AzMax, v);
            // temperatura d'antenna relativa al rumore esterno (Ta / Text): solo cielo e orizzonte sopra il suolo
            double frazione = gnd ? 0.5 : 1;   // emisfero superiore / sfera
            double ta = wN > 0 ? sumN / wN * frazione : 0;
            if (rumore == 2) ta = (Lin(r.Avg) + Lin(e.Gain(85, azLoc).GainTotal) * frazione) / 2;
            r.TaRel = ta;
            r.RdfRumore = r.G - Db(ta / frazione);
            r.PnDbm = -174 + Fa(fmhz, ambiente) + 10 * Math.Log10(Math.Max(1, bandaHz)) + Db(ta);
            double ric = -174 + nf + 10 * Math.Log10(Math.Max(1, bandaHz));
            r.Margine = r.PnDbm + preamp - ric;
            return r;
        }

        static double Lin(double db) { return db < -900 ? 0 : Math.Pow(10, db / 10); }
        static double Db(double x) { return x > 0 ? 10 * Math.Log10(x) : -999.99; }

        /// <summary>Valori di letteratura per la tabella di confronto (RDF in dB a 1,8 e 3,5 MHz, guadagno a 1,8 MHz, spazio).</summary>
        public static readonly string[][] Letteratura =
        {
            new[] { "Verticale λ/4", "5,0", "5,0", "0,4 dBi", "radiali" },
            new[] { "K9AY", "7,6", "7,4", "-24 dBi", "8 × 8 m" },
            new[] { "Flag / Pennant", "7,8", "7,6", "-30 dBi", "9 × 5 m" },
            new[] { "4-square in ricezione", "9,4", "9,2", "-20 dBi", "λ/8 di lato" },
            new[] { "8 verticali in cerchio", "13,4", "12,8", "-20 dBi", "0,37 λ di diametro" },
            new[] { "Beverage 180 m", "10,6", "12,1", "-11 dBi", "180 m" },
        };
    }
}
