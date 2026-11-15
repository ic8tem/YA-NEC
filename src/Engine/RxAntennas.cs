// YA-NEC - antenne riceventi per le bande basse (linguetta "Antenne riceventi"): dimensionamento e modello NEC per l'RDF.
// Fonti:
//  - BOG nel NEC-2 (0.4.18): quota minima del terreno di Sommerfeld lambda/1000 (I. Pellejero, "Notes on antenna simulation using NEC-2"),
//    NEC-2 senza fili interrati o isolati (manuale NEC-2; L. B. Cebik, "S-N, RCA, and MININEC Grounds"); BOG reale 200-250 ohm (K1FZ);
//  - lunghezza della Beverage / BOG per la massima risposta all'angolo theta: L = lambda / (4 (1/VF - cos theta))
//    (onda sul filo e onda nello spazio sfasate di 90 gradi; N4DJ, "Beverage Design and Beverage Basics", 2023:
//    VF 0,80 e 0 gradi -> 1 lambda = 540 piedi a 160 m; BOG VF 0,65 a 20 gradi -> 225 piedi);
//  - impedenza del filo singolo sul terreno Z0 = 138 log10(4h/d) (linea filo-terreno; N4DJ: 400-600 ohm a 2 m, BOG 200-300 ohm);
//  - EWE: K6SE (QST 1995); DHDL: AA7JV (TX3A) con le misure di IV3PRK; Flag W7IUV e Waller Flag (NX4D, N4IS) dai modelli di IV3PRK;
//  - RDF = guadagno - guadagno medio (emisfero superiore / 4 pi), convenzione approvata, valori di confronto IV3PRK e Hi-Z.
// Terreno: soluzione di Sommerfeld (GN 2), indispensabile per fili a pochi metri dal suolo. Verifica contro nec2c GN 2:
// Beverage 175 m a 2,5 m, 520 ohm: Z 701,1 - j146,5, massimo -11,46 dBi a 35 gradi di elevazione verso il carico, RDF 10,55 dB
// (IV3PRK 10,81 dB); verticale lambda/4 con 4 radiali a 0,5 m: Z 37,5 - j8,2, RDF 5,06 dB (IV3PRK 5,06-5,07 dB).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YaNec.Engine
{
    public class RxParams
    {
        public double F = 1.83, L = 175, H = 2.5, Rterm = 500, Rrod = 20, Dir = 0, WireMm = 1.6, Vf = 0.85, Angle = 20, H2 = 1.5, D = 1;
        // Waller Flag: linee di fase (m), Z0 della linea, rapporto del trasformatore (impedenze), VF della linea
        public double Tl1 = 4.0, Tl2 = 3.25, Zl = 100, Ratio = 6, VfLinea = 0.66;
        public GroundSpec Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005 };
    }

    public static class RxAntennas
    {
        const double C0 = 299.792458;
        static string F(double v) { return v != 0 && Math.Abs(v) < 1e-3 ? v.ToString("G9", CultureInfo.InvariantCulture) : v.ToString("0.######", CultureInfo.InvariantCulture); }   // valori piccoli (C in farad, L in henry) in notazione esponenziale

        public static double LengthForAngle(double lambda, double vf, double angleDeg)
        {
            double d = 1 / vf - Math.Cos(angleDeg * Math.PI / 180);
            return d > 1e-6 ? lambda / (4 * d) : double.NaN;
        }

        public static double WireOverGroundZ0(double h, double d) { return 138 * Math.Log10(4 * h / d); }

        static string Ground(GroundSpec g)
        {
            if (g == null || g.Kind == GroundKind.Perfect) return "GN 1";
            return "GN 2 0 0 0 " + F(g.Epsr) + " " + F(g.Sigma);
        }

        // ruota il punto (x, y) dell'angolo az (gradi) attorno all'origine
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

        /// <summary>Beverage: filo a quota H lungo L verso +X (ruotato di Dir), discese a terra alle due estremita';
        /// sorgente in basso nella discesa di partenza (con il picchetto in serie), terminazione + picchetto nell'altra.</summary>
        public static string Beverage(RxParams p)
        {
            double lam = C0 / p.F, r = p.WireMm / 2000, seg = Math.Min(lam / 60, 3.0);
            int n = Math.Max(11, (int)Math.Ceiling(p.L / seg)), nd = Math.Max(2, (int)Math.Ceiling(p.H / 1.0));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM Beverage " + F(p.L) + " m a " + F(p.H) + " m, " + F(p.F) + " MHz, terminazione " + F(p.Rterm) + " ohm (calcolatore antenne riceventi YA-NEC)");
            sb.AppendLine("CM YA-NEC Z0=" + F(Math.Round(WireOverGroundZ0(p.H, p.WireMm / 1000))));
            sb.AppendLine("CE");
            sb.AppendLine(Gw(1, nd, 0, 0, 0, 0, 0, p.H, r, p.Dir));
            sb.AppendLine(Gw(2, n, 0, 0, p.H, p.L, 0, p.H, r, p.Dir));
            sb.AppendLine(Gw(3, nd, p.L, 0, p.H, p.L, 0, 0, r, p.Dir));
            sb.AppendLine("GE 1");
            sb.AppendLine(Ground(p.Ground));
            sb.AppendLine("LD 4 1 1 1 " + F(p.Rrod) + " 0");
            sb.AppendLine("LD 4 3 " + nd + " " + nd + " " + F(p.Rterm + p.Rrod) + " 0");
            sb.AppendLine("EX 0 1 1 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        // ------------------------------------------------------------------------------------------------------------------------
        // 0.4.18 (richiesta di IC8TEM del 14/11/2026 09:17, "un collaudo per le antenne a terra come bog, se il modello di calcolo lo
        // permette"; scelta delle 09:20: approssimazione a pochi cm, avvertenze nella finestra e ampia spiegazione nel manuale).
        // Il BOG vero e' un filo isolato appoggiato al suolo: il NEC-2 non tratta fili sul terreno, interrati o isolati (serve il NEC-4).
        // Approssimazione: filo nudo sollevato alla quota minima che il terreno di Sommerfeld accetta, lambda/1000 (I. Pellejero; altre fonti
        // lambda/200), con le discese di un segmento ai picchetti. Verifica con nec2c (BOG 68 m, 1,83 MHz, terreno medio 13 / 5 mS/m): sotto
        // lambda/1000 l'impedenza cresce senza limite al calare della quota (20 cm 1480 ohm, 5 cm 3300, 1 cm 13800) e il filo diventa una
        // linea cosi' dissipativa che l'estremo lontano quasi non conta (Z aperto = Z in corto): e' il segno che il modello e' fuori campo.
        // Il BOG reale misura 200-300 ohm (N4DJ, K1FZ): il modello serve per lunghezza, direzione, lobo e RDF, non per la terminazione.

        /// <summary>Quota del filo del BOG nel modello: lambda/1000 arrotondato per eccesso al centimetro (minimo 2 cm).</summary>
        public static double QuotaBog(double fmhz)
        {
            double lam = C0 / Math.Max(0.01, fmhz);
            return Math.Max(0.02, Math.Ceiling(lam / 1000 * 100) / 100);
        }

        /// <summary>BOG (Beverage On Ground) approssimato: filo a quota H (di solito QuotaBog), segmenti di circa 5 H vicino al suolo
        /// (rapporto massimo 5 con le discese di un segmento), discese ai picchetti, sorgente in basso nella discesa di partenza,
        /// terminazione + picchetto nell'altra. Sempre Sommerfeld: con il coefficiente di riflessione (GN 0) il risultato non ha senso.</summary>
        public static string Bog(RxParams p)
        {
            double lam = C0 / p.F, r = p.WireMm / 2000, h = Math.Max(p.H, 3 * r);
            double seg = Math.Min(lam / 60, Math.Max(5 * h, 0.05));
            int n = Math.Max(11, (int)Math.Ceiling(p.L / seg));
            GroundSpec g = p.Ground != null && p.Ground.Kind == GroundKind.Real
                ? new GroundSpec { Kind = GroundKind.Real, Epsr = p.Ground.Epsr, Sigma = p.Ground.Sigma, Sommerfeld = true } : p.Ground;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM BOG (Beverage On Ground) " + F(p.L) + " m, " + F(p.F) + " MHz, terminazione " + F(p.Rterm) + " ohm (YA-NEC)");
            sb.AppendLine("CM APPROSSIMAZIONE NEC-2: filo nudo a " + F(h) + " m (lambda/1000 = " + F(Math.Round(lam / 1000, 3)) + " m) invece che isolato sul suolo;");
            sb.AppendLine("CM impedenza e perdite non sono quelle del BOG reale (200-300 ohm misurati). Vedi il manuale.");
            sb.AppendLine("CE");
            sb.AppendLine(Gw(1, 1, 0, 0, 0, 0, 0, h, r, p.Dir));
            sb.AppendLine(Gw(2, n, 0, 0, h, p.L, 0, h, r, p.Dir));
            sb.AppendLine(Gw(3, 1, p.L, 0, h, p.L, 0, 0, r, p.Dir));
            sb.AppendLine("GE 1");
            sb.AppendLine(Ground(g));
            sb.AppendLine("LD 4 1 1 1 " + F(p.Rrod) + " 0");
            sb.AppendLine("LD 4 3 1 1 " + F(p.Rterm + p.Rrod) + " 0");
            sb.AppendLine("EX 0 1 1 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        static System.Numerics.Complex ZSola(string deck, double f)
        {
            NecModel m = NecModel.Parse(deck);
            SegmentGeometry geo = m.BuildGeometry();
            Nec2Engine e = new Nec2Engine(geo);
            e.Solve(f, Analysis.EffectiveSources(m, geo, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            return e.Sources[0].Z;
        }

        /// <summary>Impedenza caratteristica del filo del modello: Z0 = radice(Z aperto x Z in corto), senza picchetti e senza carico
        /// (aperto = senza la discesa lontana). Restituisce anche le due impedenze: se sono quasi uguali la linea perde tanto che l'estremo
        /// lontano non conta piu' (segno che il filo e' troppo vicino al terreno per il NEC-2).</summary>
        public static System.Numerics.Complex Z0Modello(RxParams p, Func<RxParams, string> mazzo, out System.Numerics.Complex zAperto, out System.Numerics.Complex zCorto)
        {
            RxParams q = new RxParams { F = p.F, L = p.L, H = p.H, Rterm = 0, Rrod = 0, Dir = 0, WireMm = p.WireMm, Ground = p.Ground };
            string corto = mazzo(q);
            zCorto = ZSola(corto, p.F);
            StringBuilder sb = new StringBuilder();
            foreach (string riga in corto.Replace("\r", "").Split('\n'))
            {
                if (riga.StartsWith("GW 3 ", StringComparison.Ordinal) || riga.StartsWith("LD 4 3 ", StringComparison.Ordinal)) continue;
                sb.AppendLine(riga);
            }
            zAperto = ZSola(sb.ToString(), p.F);
            return System.Numerics.Complex.Sqrt(zAperto * zCorto);
        }

        /// <summary>EWE di K6SE: due verticali alte H unite da un filo lungo L; sorgente alla base di una, terminazione alla base dell'altra.</summary>
        public static string Ewe(RxParams p)
        {
            double r = p.WireMm / 2000;
            int nv = Math.Max(3, (int)Math.Ceiling(p.H / 0.5)), nh = Math.Max(5, (int)Math.Ceiling(p.L / 0.5));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM EWE (K6SE) " + F(p.L) + " x " + F(p.H) + " m, " + F(p.F) + " MHz, terminazione " + F(p.Rterm) + " ohm (calcolatore antenne riceventi YA-NEC)");
            sb.AppendLine("CM YA-NEC Z0=" + F(p.Rterm));
            sb.AppendLine("CE");
            sb.AppendLine(Gw(1, nv, 0, 0, 0, 0, 0, p.H, r, p.Dir));
            sb.AppendLine(Gw(2, nh, 0, 0, p.H, p.L, 0, p.H, r, p.Dir));
            sb.AppendLine(Gw(3, nv, p.L, 0, p.H, p.L, 0, 0, r, p.Dir));
            sb.AppendLine("GE 1");
            sb.AppendLine(Ground(p.Ground));
            sb.AppendLine("LD 4 1 1 1 " + F(p.Rrod) + " 0");
            sb.AppendLine("LD 4 3 " + nv + " " + nv + " " + F(p.Rterm + p.Rrod) + " 0");
            sb.AppendLine("EX 0 1 1 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        /// <summary>DHDL (AA7JV, TX3A, N4IS) con le misure di IV3PRK ("Double DHDL Broadside", 2013): due verticali lunghi H alle
        /// estremita' (distanza L), base a H2 dal suolo, due fili inclinati dalla cima di ciascun verticale al centro in basso e due fili
        /// orizzontali a H2 dal centro alle basi: un otto con l'incrocio al centro (i fili si incrociano senza toccarsi: 10 cm di distacco).
        /// Resistenza alla base del verticale di sinistra, trasformatore alla base di quello di destra; lobo verso il trasformatore.
        /// IV3PRK: fili 1 e 6 = 6,5 m, fili 3 e 4 = 10 m, a 2 m dal suolo, carico 1250-1300 ohm, Z circa 900 ohm (trasformatore 12:1 su 75),
        /// TX3A: guadagno -28 dB, RDF 10 dB. nec2c con Sommerfeld (13; 5 mS/m) a 1,83 MHz, 1300 ohm: -31,9 dBi, RDF 9,98 dB.</summary>
        public static string Dhdl(RxParams p) { return DhdlDeck(p, false); }

        /// <summary>Due DHDL affiancate (broadside) a distanza D, alimentate in fase con linee uguali (IV3PRK 2013: 90 m, RDF 12,7 dB,
        /// apertura 52 gradi; con 75 ohm fino al T: 46 -j4 ohm). nec2c: 90 m RDF 12,59 dB, 110 m 13,27 dB.</summary>
        public static string DhdlBroadside(RxParams p) { return DhdlDeck(p, true); }

        static void DhdlFili(StringBuilder sb, int t0, double y, RxParams p, double r)
        {
            double a = p.L / 2, h = p.H2, zt = p.H2 + p.H, dy = 0.1;
            double sl = Math.Sqrt(a * a + p.H * p.H);
            int nv = Math.Max(7, (int)Math.Ceiling(p.H / 0.5)) | 1, ns = Math.Max(11, (int)Math.Ceiling(sl / 0.5)) | 1, nh = Math.Max(9, (int)Math.Ceiling(a / 0.5)) | 1;
            sb.AppendLine(Gw(t0, nv, -a, y, h, -a, y, zt, r, p.Dir));
            sb.AppendLine(Gw(t0 + 1, ns, -a, y, zt, 0, y, h, r, p.Dir));
            sb.AppendLine(Gw(t0 + 2, nh, 0, y, h, a, y, h, r, p.Dir));
            sb.AppendLine(Gw(t0 + 3, nv, a, y, h, a, y, zt, r, p.Dir));
            sb.AppendLine(Gw(t0 + 4, ns, a, y, zt, 0, y + dy, h, r, p.Dir));
            sb.AppendLine(Gw(t0 + 5, nh, 0, y + dy, h, -a, y, h, r, p.Dir));
        }

        static string DhdlDeck(RxParams p, bool coppia)
        {
            double r = p.WireMm / 2000;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM " + (coppia ? "Due DHDL broadside a " + F(p.D) + " m" : "DHDL") + " (AA7JV/N4IS, misure IV3PRK): lunghezza " + F(p.L) + " m, verticali " + F(p.H) +
                " m, base a " + F(p.H2) + " m, carico " + F(p.Rterm) + " ohm, " + F(p.F) + " MHz (calcolatore antenne riceventi YA-NEC)");
            sb.AppendLine("CM YA-NEC Z0=" + F(Math.Round(p.Rterm * 0.7)));
            sb.AppendLine("CE");
            DhdlFili(sb, 1, coppia ? -p.D / 2 : 0, p, r);
            if (coppia) DhdlFili(sb, 7, p.D / 2, p, r);
            sb.AppendLine("GE 1");
            sb.AppendLine(Ground(p.Ground));
            // prima tutti i carichi, poi le sorgenti (con un LD in mezzo il NEC ricomincia l'elenco delle sorgenti)
            sb.AppendLine("LD 4 1 1 1 " + F(p.Rterm) + " 0");
            if (coppia) sb.AppendLine("LD 4 7 1 1 " + F(p.Rterm) + " 0");
            sb.AppendLine("EX 0 4 1 0 1 0");
            if (coppia) sb.AppendLine("EX 0 10 1 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        static void Anello(StringBuilder sb, int t0, double xc, double w, double h, double zc, RxParams p, double r)
        {
            double x1 = xc - w / 2, x2 = xc + w / 2, z1 = zc - h / 2, z2 = zc + h / 2;
            int nv = Math.Max(7, (int)Math.Ceiling(h / 0.4)) | 1, nh = Math.Max(9, (int)Math.Ceiling(w / 0.4)) | 1;
            sb.AppendLine(Gw(t0, nv, x1, 0, z1, x1, 0, z2, r, p.Dir));
            sb.AppendLine(Gw(t0 + 1, nh, x1, 0, z2, x2, 0, z2, r, p.Dir));
            sb.AppendLine(Gw(t0 + 2, nv, x2, 0, z2, x2, 0, z1, r, p.Dir));
            sb.AppendLine(Gw(t0 + 3, nh, x2, 0, z1, x1, 0, z1, r, p.Dir));
        }

        static int SegCentro(RxParams p) { return (Math.Max(7, (int)Math.Ceiling(p.H / 0.4)) | 1) / 2 + 1; }

        /// <summary>Flag (K6SE; rotativa di W7IUV come la ricostruita da IV3PRK nel 2007): rettangolo verticale largo L e alto H con il centro
        /// a H2; trasformatore al centro di un lato verticale, resistenza al centro dell'altro, lobo verso il trasformatore.
        /// IV3PRK: 8,84 x 4,28 m, 937 ohm, trasformatore 3+12 spire su BN73-202, guadagno circa -30 dB, RDF circa 8 dB (7,81-7,96 con la torre
        /// disaccordata). La quota della Flag non e' nel testo: 8 m di centro come valore di partenza.</summary>
        public static string Flag(RxParams p)
        {
            double r = p.WireMm / 2000;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM Flag " + F(p.L) + " x " + F(p.H) + " m, centro a " + F(p.H2) + " m, carico " + F(p.Rterm) + " ohm, " + F(p.F) + " MHz (K6SE/W7IUV, misure IV3PRK)");
            sb.AppendLine("CM YA-NEC Z0=" + F(Math.Round(p.Rterm)));
            sb.AppendLine("CE");
            Anello(sb, 1, 0, p.L, p.H, p.H2, p, r);
            sb.AppendLine("GE 1");
            sb.AppendLine(Ground(p.Ground));
            int sc = SegCentro(p);
            sb.AppendLine("LD 4 3 " + sc + " " + sc + " " + F(p.Rterm) + " 0");
            sb.AppendLine("EX 0 1 " + sc + " 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        /// <summary>Pennant (K6SE; pagine di WA1ION e dati IV3PRK): triangolo verticale con il lato verticale alto H a un estremo, il filo
        /// orizzontale in alto lungo L e la diagonale che torna in basso; resistenza al centro del lato verticale, trasformatore sulla punta
        /// (angolo tra il filo alto e la diagonale), lobo verso la punta. Misure classiche 29 x 14 piedi (8,84 x 4,27 m), circa 950 ohm.
        /// Prova con nec2c (13/11/2026, centro a 8 m, Sommerfeld 13 / 5 mS/m, 1,83 MHz): 850 ohm F/B 34,6 dB, RDF 7,59 dB, guadagno -36,7 dBi,
        /// Z 930 +j41 ohm; 950 ohm F/B 22,8 dB, RDF 7,59 dB. IV3PRK: RDF 7,8 dB (graduatoria).</summary>
        public static string Pennant(RxParams p)
        {
            double r = p.WireMm / 2000;
            double x1 = -p.L / 2, x2 = p.L / 2, z1 = p.H2 - p.H / 2, z2 = p.H2 + p.H / 2;
            double diag = Math.Sqrt(p.L * p.L + p.H * p.H);
            int nv = Math.Max(7, (int)Math.Ceiling(p.H / 0.4)) | 1, nh = Math.Max(9, (int)Math.Ceiling(p.L / 0.4)) | 1, nd = Math.Max(9, (int)Math.Ceiling(diag / 0.4)) | 1;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM Pennant " + F(p.L) + " x " + F(p.H) + " m, centro a " + F(p.H2) + " m, carico " + F(p.Rterm) + " ohm, " + F(p.F) + " MHz (K6SE, WA1ION, IV3PRK)");
            sb.AppendLine("CM YA-NEC Z0=" + F(Math.Round(p.Rterm)));
            sb.AppendLine("CE");
            sb.AppendLine(Gw(1, nv, x1, 0, z1, x1, 0, z2, r, p.Dir));
            sb.AppendLine(Gw(2, nh, x1, 0, z2, x2, 0, z2, r, p.Dir));
            sb.AppendLine(Gw(3, nd, x2, 0, z2, x1, 0, z1, r, p.Dir));
            sb.AppendLine("GE 1");
            sb.AppendLine(Ground(p.Ground));
            int sc = nv / 2 + 1;
            sb.AppendLine("LD 4 1 " + sc + " " + sc + " " + F(p.Rterm) + " 0");
            sb.AppendLine("EX 0 2 " + nh + " 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        /// <summary>Waller Flag (NX4D, N4IS; modello di IV3PRK 2007): due Flag L x H nello stesso piano, centri a D m l'uno dall'altro e a H2 dal
        /// suolo, resistenze Rterm, trasformatori Rterm/Zl (600/100), linee di fase Tl1 (anello davanti) e Tl2 (dietro, incrociata) di
        /// impedenza Zl e VF VfLinea fino al punto comune. Il trasformatore ideale si toglie riportando la linea dal lato dell'anello
        /// (Z0 x rapporto, stessa lunghezza elettrica); il punto comune e' un filo corto lontano. Impedenza alla sorgente divisa per il rapporto.
        /// IV3PRK (tab. 1, 4,27 x 2 m, 5 m, TL1 4 m): TL2 3,0 m -58,0 dB RDF 11,84; 4,0 m -56,1 dB RDF 11,74; 5,0 m -54,5 dB RDF 11,36.
        /// nec2c con questo modello: TL2 3,0 m -59,0 dBi RDF 11,63; 4,0 -57,3 RDF 11,63; 5,0 -55,8 RDF 11,33 (stesso andamento).</summary>
        public static string WallerFlag(RxParams p)
        {
            double r = p.WireMm / 2000;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM Waller Flag (NX4D/N4IS, modello IV3PRK): anelli " + F(p.L) + " x " + F(p.H) + " m a " + F(p.D) + " m, centro a " + F(p.H2) + " m, " + F(p.Rterm) +
                " ohm, linee " + F(p.Tl1) + " / " + F(p.Tl2) + " m da " + F(p.Zl) + " ohm VF " + F(p.VfLinea) + ", trasformatori " + F(p.Ratio) + ":1");
            sb.AppendLine("CM YA-NEC Z0=" + F(p.Zl / 2));
            sb.AppendLine("CE");
            Anello(sb, 1, p.D / 2, p.L, p.H, p.H2, p, r);
            Anello(sb, 5, -p.D / 2, p.L, p.H, p.H2, p, r);
            sb.AppendLine(Gw(9, 1, 0, 60, 1, 0, 60, 1.2, 0.001, p.Dir));   // punto comune delle linee (filo corto lontano, come i fili virtuali)
            sb.AppendLine("GE 1");
            sb.AppendLine(Ground(p.Ground));
            int sc = SegCentro(p);
            sb.AppendLine("LD 4 1 " + sc + " " + sc + " " + F(p.Rterm) + " 0");
            sb.AppendLine("LD 4 5 " + sc + " " + sc + " " + F(p.Rterm) + " 0");
            double z = p.Zl * p.Ratio;
            sb.AppendLine("TL 9 1 3 " + sc + " " + F(z) + " " + F(p.Tl1 / p.VfLinea) + " 0 0 0 0");
            sb.AppendLine("TL 9 1 7 " + sc + " " + F(-z) + " " + F(p.Tl2 / p.VfLinea) + " 0 0 0 0");
            sb.AppendLine("EX 0 9 1 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        /// <summary>Loop magnetico ricevente: anello verticale di diametro D con la base a H2, 16 lati, uscita in basso.</summary>
        public static string LoopRx(RxParams p)
        {
            double r = p.WireMm / 2000, R = p.D / 2, zc = p.H2 + R;
            int ns = 16;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM Loop magnetico RX diametro " + F(p.D) + " m, base a " + F(p.H2) + " m, " + F(p.F) + " MHz (calcolatore antenne riceventi YA-NEC)");
            sb.AppendLine("CE");
            for (int k = 0; k < ns; k++)
            {
                double a1 = -Math.PI / 2 + 2 * Math.PI * k / ns + Math.PI / ns, a2 = a1 + 2 * Math.PI / ns;
                if (k == ns - 1) a2 = -Math.PI / 2 + Math.PI / ns;
                sb.AppendLine(Gw(k + 1, 1, R * Math.Cos(a1), 0, zc + R * Math.Sin(a1), R * Math.Cos(a2), 0, zc + R * Math.Sin(a2), r, p.Dir));
            }
            // il lato in basso (tra gli angoli -pi/2 -+ pi/ns) e' l'ultimo: la sorgente va li'
            sb.AppendLine("GE 0");
            sb.AppendLine(Ground(p.Ground));
            sb.AppendLine("EX 0 " + ns + " 1 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        /// <summary>Verticale di riferimento lambda/4 con 4 radiali sopraelevati (caso IV3PRK, 4 radiali a 0,5 m).</summary>
        public static string VerticaleRef(RxParams p)
        {
            double lam = C0 / p.F, h = 71.3 / p.F, rr = 75.0 / p.F, zb = 0.5, r = 0.01;
            int nv = Math.Max(11, (int)Math.Ceiling(h / (lam / 60))), nr = Math.Max(9, (int)Math.Ceiling(rr / (lam / 60)));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CM Verticale TX di riferimento lambda/4, 4 radiali a 0,5 m, " + F(p.F) + " MHz (calcolatore antenne riceventi YA-NEC)");
            sb.AppendLine("CE");
            sb.AppendLine("GW 1 " + nv + " 0 0 " + F(zb) + " 0 0 " + F(zb + h) + " " + F(r));
            for (int k = 0; k < 4; k++)
            {
                double a = k * Math.PI / 2;
                sb.AppendLine("GW " + (k + 2) + " " + nr + " 0 0 " + F(zb) + " " + F(rr * Math.Cos(a)) + " " + F(rr * Math.Sin(a)) + " " + F(zb) + " 0.001");
            }
            sb.AppendLine("GE 0");
            sb.AppendLine(Ground(p.Ground));
            sb.AppendLine("EX 0 1 1 0 1 0");
            sb.AppendLine("FR 0 1 0 0 " + F(p.F) + " 0");
            sb.AppendLine("EN");
            return sb.ToString();
        }

        // valori di confronto per la graduatoria RDF a 160 m (bozzetto approvato; IV3PRK, Hi-Z/W8JI, AA7JV)
        public static readonly Tuple<string, double>[] Graduatoria =
        {
            Tuple.Create("Verticale TX (IV3PRK)", 5.07),
            Tuple.Create("K9AY (Hi-Z)", 7.2),
            Tuple.Create("Dipolo basso (IV3PRK)", 7.3),
            Tuple.Create("Flag (Hi-Z)", 7.4),
            Tuple.Create("Pennant (IV3PRK)", 7.8),
            Tuple.Create("Flag W7IUV (IV3PRK)", 7.9),
            Tuple.Create("BOG 63 m (IV3PRK)", 9.0),
            Tuple.Create("4-square RX (IV3PRK)", 9.4),
            Tuple.Create("DHDL (TX3A, IV3PRK)", 10.0),
            Tuple.Create("Beverage 175 m (IV3PRK)", 10.81),
            Tuple.Create("Waller Flag (IV3PRK, modello)", 11.84),
            Tuple.Create("4-square crossfire (IV3PRK)", 12.02),
            Tuple.Create("Waller Flag (Hi-Z)", 12.2),
            Tuple.Create("Due DHDL broadside 90 m (IV3PRK)", 12.7),
            Tuple.Create("Hi-Z 8 el. cerchio", 13.4),
            Tuple.Create("Beverage 350 m (W8JI/Hi-Z)", 13.66),
        };
    }
}
