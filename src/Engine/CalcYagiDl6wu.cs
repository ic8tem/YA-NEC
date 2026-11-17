// YA-NEC 0.5.0 - Yagi lunghe secondo DL6WU e Quagi, con i passi di ottimizzazione (richiesta di IC8TEM del 15/11/2026 15:02:
// "riesci anche a trovare quella routine di calcolo delle yagi secondo dl6wu ... me lo metti in una sottolinguetta di calcolatori e,
// siccome mi ricordo che quella routine arriva ad un risultato poco interessante in termini di prestazioni, mi metti pure dei passaggi
// di ottimizzazione? devi permettere dipolo aperto, dipolo chiuso, LFA, dipolo piegato (dg7ybn) ... medesimo trattamento per la quagi").
//
// Yagi DL6WU: il programma BASIC di J. Haigwood KY4Z e B. Stein W6NBI (1983-86) sulle misure di G. Hoch DL6WU ("Extremely Long Yagi
// Antennas", VHF Communications 3/1982; "Zelf ontwerpen en bouwen van VHF en UHF antennes", con PA0MS), con le modifiche di
// I. White G3SEK (1986), di DL6WU (1988) e i riflettori piu' lunghi (04/1989); letto nella trascrizione in Python di R. Rodrigues
// (github.com/ricardorodrigues-ca/yagi-antenna-calculator, licenza MIT). Formule (tutto in lunghezze d'onda):
//  - riflettore a 0, radiatore a 0,2; spaziature dei direttori 0,075 0,180 0,215 0,250 0,280 0,300 0,315 0,330 0,345 0,360 0,375 0,390 0,400
//    e poi 0,400 (finche' il boom lo permette);
//  - direttore N: (K1 - K2 ln N)(1 - K3 e^(-K4 N)) con K1..K4 interpolati sul diametro (tabella per 0,001 ... 0,02 lambda);
//  - riflettore: ((XR - 40) / (186,8769 ln(2/d) - 320) + 1) / 2 con XR = 20 (DL6WU);
//  - radiatore: (0,4777 - 1,0522 d + 0,43363 d^-0,014891) / 2 x 1,02 (DL6WU);
//  - correzione del boom (G3SEK): BD (733 BD (0,055 - BD) - 504 BD (0,03 - BD)), meta' per gli elementi isolati, zero sopra il boom;
//  - guadagno stimato 9,2 + 3,39 ln(L/lambda) dBd.
// Prova con nec2c (scratchpad yagi5, 15/11/2026, 144,3 MHz, elementi da 4 mm, radiatore da 10 mm, spazio libero): il guadagno del NEC
// coincide con la stima DL6WU entro 0,1-0,3 dB (2,2 lambda 14,01 dBi contro 13,94; 3,6 lambda 15,83 contro 15,71; 5,6 lambda 17,38 contro
// 17,20; 9,6 lambda 19,32 contro 19,02). Quello che il metodo non da' e' l'adattamento (dipolo aperto 51-61 +j6-17 ohm) e un F/B
// uniforme sulla banda: sono questi i passi di ottimizzazione.
//
// Passi di ottimizzazione (stessi del prototipo in Python con nec2c, Nelder-Mead):
//  1. radiatore: lunghezza (e la grandezza propria dell'alimentazione) per l'impedenza voluta;
//  2. riflettore: lunghezza e distanza per F/B e F/R sulla banda;
//  3. direttori: fattore di lunghezza e rastremazione per il guadagno, con i vincoli su ROS e F/B;
//  4. tutto insieme (anche la distanza radiatore - primo direttore).
// Alimentazioni (confermate da IC8TEM il 15/11/2026 15:06): dipolo aperto (50 ohm), dipolo chiuso / folded dipole (200 ohm, balun 4:1),
// LFA di G0KSC (anello piatto nel piano degli elementi, 50 ohm; proporzioni del 6 el. 144 MHz: riflettore - anello 0,052 lambda,
// anello 0,422 x 0,084 lambda, anello - D1 0,089 lambda), radiatore piegato di DG7YBN (parte centrale diritta lunga al massimo 0,1 lambda,
// bracci piegati in avanti; l'angolo regola l'impedenza: DK7ZB, "Design principles OWL", e DG7YBN).
//
// Quagi: N6NB (QST 4/1977 e 2/1978): anello riflettore e anello alimentato (quadrati, alimentazione al centro del lato basso) e direttori
// a bacchetta. Proporzioni dal modello NEC del "Handbook quagi" di L. B. Cebik W4RNL ("Quagi and Yagi on 2 Meters", 144,5 MHz):
// riflettore 1,0548 lambda di perimetro, alimentato 1,0382, primo direttore 0,4390 lambda e poi 0,0023 lambda in meno per ogni direttore;
// spaziature R-A 0,256, A-D1 0,192, D1-D2 0,404, D2-D3 0,214, poi 0,318 lambda (le stesse delle tabelle di N6NB e W3PM, QST 1/1990).
// Con le misure di N6NB/W3PM (anello alimentato 81" a 145,9 MHz, 1,001 lambda) il NEC da' -j90 ohm: per questo si parte da Cebik.
// ARRL Antenna Book 24a, tab. 15.21 (quagi 8 el.): a 144,5 MHz riflettore 86 5/8", alimentato 82" (lunghezze del filo), direttori da
// 35 15/16" a 35" a passi di 3/16", spaziature 21 / 15 3/4 / 33 / 17 1/2 / 26,1": in lambda 1,061 / 1,004 / 0,440 (-0,0023) e
// 0,257 / 0,193 / 0,404 / 0,214 / 0,320, le stesse spaziature usate qui.
// Variante "quad": anche i direttori sono anelli (perimetro 0,975 x alimentato x D/D1).
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    /// <summary>Metodo del simplesso di Nelder e Mead (minimo di una funzione), con i passi iniziali per ogni variabile.</summary>
    public static class NelderMead
    {
        public static double[] Minimo(Func<double[], double> f, double[] x0, double[] passi, int maxValutazioni, out int nVal, out double fMin)
        {
            int n = x0.Length;
            double[][] s = new double[n + 1][];
            double[] fs = new double[n + 1];
            int cont = 0;
            Func<double[], double> F = x => { cont++; double v = f(x); return double.IsNaN(v) ? 1e9 : v; };
            for (int i = 0; i <= n; i++)
            {
                s[i] = (double[])x0.Clone();
                if (i > 0) s[i][i - 1] += passi[i - 1];
                fs[i] = F(s[i]);
            }
            while (cont < maxValutazioni)
            {
                // ordina
                int[] o = new int[n + 1];
                for (int i = 0; i <= n; i++) o[i] = i;
                Array.Sort(o, (a, b) => fs[a].CompareTo(fs[b]));
                double[][] s2 = new double[n + 1][]; double[] f2 = new double[n + 1];
                for (int i = 0; i <= n; i++) { s2[i] = s[o[i]]; f2[i] = fs[o[i]]; }
                s = s2; fs = f2;
                // fine: valori e punti vicini
                double dx = 0;
                for (int i = 1; i <= n; i++) for (int j = 0; j < n; j++) dx = Math.Max(dx, Math.Abs(s[i][j] - s[0][j]));
                if (Math.Abs(fs[n] - fs[0]) < 1e-3 && dx < 1e-4) break;
                double[] c = new double[n];
                for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) c[j] += s[i][j] / n;
                Func<double, double[]> punto = t => { double[] p = new double[n]; for (int j = 0; j < n; j++) p[j] = c[j] + t * (s[n][j] - c[j]); return p; };
                double[] xr = punto(-1); double fr = F(xr);
                if (fr < fs[0])
                {
                    double[] xe = punto(-2); double fe = F(xe);
                    if (fe < fr) { s[n] = xe; fs[n] = fe; } else { s[n] = xr; fs[n] = fr; }
                }
                else if (fr < fs[n - 1]) { s[n] = xr; fs[n] = fr; }
                else
                {
                    bool fuori = fr < fs[n];
                    double[] xc = punto(fuori ? -0.5 : 0.5); double fc = F(xc);
                    if (fc < (fuori ? fr : fs[n])) { s[n] = xc; fs[n] = fc; }
                    else
                        for (int i = 1; i <= n; i++)
                        {
                            for (int j = 0; j < n; j++) s[i][j] = s[0][j] + 0.5 * (s[i][j] - s[0][j]);
                            fs[i] = F(s[i]);
                        }
                }
            }
            int best = 0;
            for (int i = 1; i <= n; i++) if (fs[i] < fs[best]) best = i;
            nVal = cont; fMin = fs[best];
            return s[best];
        }
    }

    /// <summary>Prestazioni su alcune frequenze della banda: impedenza, guadagno in avanti, F/B, F/R e ROS sulla Z voluta.</summary>
    public class PrestazioniBanda
    {
        public double[] F, G, Fb, Fr, Ros;
        public Complex[] Z;
        public double GMedio { get { double s = 0; foreach (double g in G) s += g; return s / G.Length; } }

        /// <summary>Calcolo del modello (spazio libero, lobo verso +x) a ogni frequenza della lista.</summary>
        public static PrestazioniBanda Calcola(string nec, double[] freq, double zRif)
        {
            NecModel m = NecModel.Parse(nec);
            SegmentGeometry g = m.BuildGeometry();
            PrestazioniBanda p = new PrestazioniBanda { F = freq, G = new double[freq.Length], Fb = new double[freq.Length], Fr = new double[freq.Length], Ros = new double[freq.Length], Z = new Complex[freq.Length] };
            for (int i = 0; i < freq.Length; i++)
            {
                double f = freq[i];
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
                Complex z = e.Sources[0].Z;
                double av = e.Gain(90, 0).GainTotal, die = e.Gain(90, 180).GainTotal, peg = -999;
                for (double ph = 90; ph <= 270.001; ph += 3) peg = Math.Max(peg, e.Gain(90, ph).GainTotal);
                double r = Complex.Abs((z - zRif) / (z + zRif));
                p.Z[i] = z; p.G[i] = av; p.Fb[i] = av - die; p.Fr[i] = av - peg; p.Ros[i] = r < 0.999 ? (1 + r) / (1 - r) : 999;
            }
            return p;
        }

        /// <summary>Costo da rendere minimo: meno il guadagno medio, piu' le penalita' su ROS, F/B e F/R fuori dai limiti.</summary>
        public double Costo(Obiettivo o)
        {
            double c = -GMedio;
            for (int i = 0; i < G.Length; i++)
            {
                c += o.PesoRos * Math.Pow(Math.Max(0, Ros[i] - o.Ros), 2) * 10;
                c += o.PesoFb * Math.Max(0, o.Fb - Fb[i]);
                c += o.PesoFr * Math.Max(0, o.Fr - Fr[i]);
            }
            return c;
        }
    }

    public class Obiettivo
    {
        public double PesoRos = 5, Ros = 1.2, PesoFb = 0.1, Fb = 25, PesoFr = 0.1, Fr = 20;
        public static Obiettivo Scelto(int k)
        {
            if (k == 0) return new Obiettivo { PesoFb = 0.02, Fb = 20, PesoFr = 0.02, Fr = 18 };       // guadagno
            if (k == 2) return new Obiettivo { PesoFb = 0.3, Fb = 28, PesoFr = 0.3, Fr = 22 };        // F/B alto
            return new Obiettivo();                                                                     // bilanciato
        }
    }

    /// <summary>Parte comune di Yagi DL6WU e Quagi: geometria in lunghezze d'onda, parametri di ottimizzazione, passi.</summary>
    public abstract class CalcYagiBase : SimpleCalc
    {
        // parametri di ottimizzazione: sr distanza riflettore-radiatore, s1 distanza radiatore-D1, kr kd kdir fattori di lunghezza,
        // tap rastremazione dei direttori, fp grandezza propria dell'alimentazione
        protected class Ott { public double sr, s1, kr = 1, kd = 1, kdir = 1, tap, fp, prog; public Ott Copia() { return (Ott)MemberwiseClone(); } }
        protected Ott ott;            // null = misure del metodo (DL6WU o N6NB/Cebik)
        protected string firma;
        protected List<string[]> passiFatti = new List<string[]>();
        public override string TuneLabel { get { return "Ottimizza (NEC)"; } }

        protected string Firma() { StringBuilder s = new StringBuilder(); foreach (CalcParam p in Params) if (p.Key != "passo") s.Append(p.Key + "=" + p.Value + ";"); return s.ToString(); }
        protected double[] Banda()
        {
            double f = P("f"), bw = Math.Max(0, P("bw")) / 1000;
            return bw > 0 ? new[] { f - bw / 2, f, f + bw / 2 } : new[] { f };
        }

        /// <summary>Misure di partenza del metodo (posizioni e lunghezze in lambda).</summary>
        protected abstract void Progetto(out List<double> pos, out List<double> lens);
        protected abstract Ott Partenza(List<double> pos, List<double> lens);
        protected abstract string Mazzo(Ott p, double f, out double zRif);
        protected abstract string NomeFp { get; }
        protected abstract double PassoFp { get; }
        /// <summary>1.0.15: boom e numero di elementi dati tutti e due (fuori dal metodo): il passo 4 ottimizza anche la progressione delle spaziature.</summary>
        protected virtual bool BoomFisso { get { return false; } }

        protected string Riassunto(PrestazioniBanda r)
        {
            int c = r.G.Length / 2;
            StringBuilder s = new StringBuilder();
            s.Append("G " + Calcolatori.N(r.G[c], 2) + " dBi");
            if (r.G.Length > 1) s.Append(" (" + Calcolatori.N(r.G[0], 2) + " / " + Calcolatori.N(r.G[r.G.Length - 1], 2) + " ai bordi)");
            double fbMin = 999, rosMax = 0;
            for (int i = 0; i < r.G.Length; i++) { fbMin = Math.Min(fbMin, r.Fb[i]); rosMax = Math.Max(rosMax, r.Ros[i]); }
            s.Append(", F/B " + Calcolatori.N(r.Fb[c], 1) + " dB (minimo " + Calcolatori.N(fbMin, 1) + "), F/R " + Calcolatori.N(r.Fr[c], 1) + " dB, ROS massimo " + Calcolatori.N(rosMax, 2) + ", Z " + Calcolatori.Cx(r.Z[c]));
            return s.ToString();
        }

        /// <summary>Passi di ottimizzazione fino a quello scelto; il pulsante "Ottimizza (NEC)" chiama questo metodo.</summary>
        public override double Tune(double fmhz)
        {
            List<double> pos, lens;
            Progetto(out pos, out lens);
            Ott p = Partenza(pos, lens);
            double[] fr = Banda();
            int fino = Math.Max(0, Math.Min(4, I("passo")));
            if (BoomFisso) fino = 4;   // fuori dal metodo l'ottimizzazione e' obbligatoria
            Obiettivo scelto = Obiettivo.Scelto(I("obj"));
            passiFatti = new List<string[]>();
            Func<Ott, PrestazioniBanda> val = q => { double z; string nec = Mazzo(q, P("f"), out z); return PrestazioniBanda.Calcola(nec, fr, z); };
            passiFatti.Add(new[] { "0 - misure del metodo", Riassunto(val(p)) });
            string[] nomi = { "", "1 - radiatore", "2 - riflettore", "3 - direttori", "4 - tutto insieme" };
            string[][] chiavi = { null, new[] { "kd", "fp" }, new[] { "kr", "sr", "kd", "fp" }, new[] { "kdir", "tap", "kd", "fp" },
                BoomFisso ? new[] { "kr", "sr", "kd", "s1", "kdir", "tap", "prog", "fp" } : new[] { "kr", "sr", "kd", "s1", "kdir", "tap", "fp" } };
            int[] maxVal = { 0, 80, 150, 200, 400 };
            for (int passo = 1; passo <= fino; passo++)
            {
                Obiettivo ob = passo == 1 ? new Obiettivo { PesoRos = 50, Ros = 1.0, PesoFb = 0, PesoFr = 0 } : (passo == 2 ? Obiettivo.Scelto(2) : scelto);
                List<string> k = new List<string>(chiavi[passo]);
                if (NomeFp == null) k.Remove("fp");
                Ott base0 = p.Copia();
                Func<double[], Ott> da = x =>
                {
                    Ott q = base0.Copia();
                    for (int i = 0; i < k.Count; i++)
                        switch (k[i]) { case "sr": q.sr = x[i]; break; case "s1": q.s1 = x[i]; break; case "kr": q.kr = x[i]; break; case "kd": q.kd = x[i]; break; case "kdir": q.kdir = x[i]; break; case "tap": q.tap = x[i]; break; case "prog": q.prog = x[i]; break; case "fp": q.fp = x[i]; break; }
                    return q;
                };
                double[] x0 = new double[k.Count], st = new double[k.Count];
                for (int i = 0; i < k.Count; i++)
                    switch (k[i])
                    {
                        case "sr": x0[i] = p.sr; st[i] = 0.02; break; case "s1": x0[i] = p.s1; st[i] = 0.01; break;
                        case "kr": x0[i] = p.kr; st[i] = 0.02; break; case "kd": x0[i] = p.kd; st[i] = 0.02; break;
                        case "kdir": x0[i] = p.kdir; st[i] = 0.015; break; case "tap": x0[i] = p.tap; st[i] = 0.04; break;
                        case "prog": x0[i] = p.prog; st[i] = 0.15; break;
                        default: x0[i] = p.fp; st[i] = PassoFp; break;
                    }
                int nv; double fm;
                double[] xb = NelderMead.Minimo(x =>
                {
                    Ott q = da(x);
                    if (q.sr < 0.03 || q.s1 < 0.03 || q.kr < 0.8 || q.kd < 0.7 || q.kdir < 0.85 || q.kdir > 1.15 || Math.Abs(q.tap) > 0.3 || q.prog < -0.5 || q.prog > 1.2) return 1e3;
                    try { return val(q).Costo(ob); } catch (NecException) { return 1e3; }
                }, x0, st, maxVal[passo], out nv, out fm);
                p = da(xb);
                passiFatti.Add(new[] { nomi[passo], Riassunto(val(p)) + " (" + nv + " calcoli)" });
            }
            ott = p; firma = Firma();
            return 1;
        }

        protected void Passi(CalcOutput o)
        {
            if (ott == null) { o.Oks.Add("misure del metodo; il pulsante \"Ottimizza (NEC)\" esegue i passi fino a quello scelto"); return; }
            foreach (string[] r in passiFatti) o.Add("Passo " + r[0], r[1]);
            o.Oks.Add("misure ottimizzate con il NEC fino al passo scelto; cambiando un dato si torna alle misure del metodo");
        }
    }

    // =============================================================================================
    public class CalcYagiDl6wu : CalcYagiBase
    {
        static readonly double[] Sp = { 0.075, 0.180, 0.215, 0.250, 0.280, 0.300, 0.315, 0.330, 0.345, 0.360, 0.375, 0.390, 0.400, 0.400 };
        static readonly double[][] Kd =
        {
            new[] { 0.001, 0.4711, 0.018, 0.08398, 0.965 }, new[] { 0.003, 0.462, 0.01941, 0.08543, 0.9697 }, new[] { 0.005, 0.4538, 0.02117, 0.0951, 1.007 },
            new[] { 0.007, 0.4491, 0.02274, 0.08801, 0.9004 }, new[] { 0.01, 0.4421, 0.02396, 0.1027, 1.038 }, new[] { 0.015, 0.4358, 0.02558, 0.1149, 1.034 },
            new[] { 0.02, 0.4268, 0.02614, 0.1112, 1.036 }
        };
        public static readonly string[] Alim = { "Dipolo aperto", "Dipolo chiuso (folded)", "LFA (G0KSC)", "Piegato (DG7YBN)" };

        public CalcYagiDl6wu()
        {
            Key = "yagidl6wu"; Title = "Yagi lunga DL6WU"; Family = "Direttive";
            Source = "G. Hoch DL6WU (VHF Communications 3/1982), programma BASIC di KY4Z e W6NBI con le modifiche di G3SEK (1986) e DL6WU (1988); G0KSC (LFA); DG7YBN e DK7ZB (radiatore piegato)";
            Params.Add(new CalcParam("f", "Frequenza di progetto", 144.3, "MHz"));
            Params.Add(new CalcParam("bw", "Banda da coprire (0 = solo la frequenza)", 600, "kHz", "L'ottimizzazione guarda la frequenza di progetto e i due bordi della banda."));
            Params.Add(CalcParam.Scelta("modo", "Dimensione data da", 0, "Numero di elementi oppure lunghezza del boom (DL6WU mette i direttori finche' il boom li contiene).", "Numero di elementi", "Lunghezza del boom", "Tutti e due (fuori DL6WU)"));
            Params.Add(new CalcParam("nel", "Numero di elementi", 10, "", "Riflettore, radiatore e direttori (da 3 a 60)."));
            Params.Add(new CalcParam("boom", "Lunghezza del boom", 4.5, "m", "Dal riflettore all'ultimo direttore; DL6WU vuole da 2,2 a 39 lambda."));
            Params.Add(new CalcParam("ed", "Diametro dei parassiti", 4, "mm", "DL6WU: da 0,001 a 0,02 lambda."));
            Params.Add(new CalcParam("dd", "Diametro del radiatore", 10, "mm"));
            Params.Add(CalcParam.Scelta("mont", "Montaggio degli elementi", 2, "Correzione del boom di G3SEK: piena per gli elementi che attraversano un boom metallico, meta' per quelli isolati dentro il boom, nessuna sopra il boom.", "Attraverso il boom", "Isolati nel boom", "Sopra il boom"));
            Params.Add(new CalcParam("bd", "Diametro del boom", 20, "mm", "Serve solo con gli elementi attraverso il boom o isolati dentro (massimo 0,06 lambda)."));
            Params.Add(CalcParam.Scelta("feed", "Alimentazione", 0, "Dipolo aperto (50 ohm), dipolo chiuso / folded (200 ohm con balun 4:1), anello LFA di G0KSC (50 ohm), radiatore piegato di DG7YBN (50 ohm).", Alim));
            Params.Add(CalcParam.Scelta("obj", "Obiettivo dell'ottimizzazione", 1, "Guadagno: F/B almeno 20 dB; bilanciato: F/B 25 e F/R 20 dB; F/B alto: 28 e 22 dB. Sempre ROS sotto 1,2 sulla banda.", "Guadagno", "Bilanciato", "F/B alto"));
            Params.Add(CalcParam.Scelta("passo", "Ottimizza fino al passo", 4, "0 = misure DL6WU com'e'; 1 radiatore (adattamento); 2 riflettore (F/B); 3 direttori (guadagno); 4 tutto insieme.",
                "0 - DL6WU com'e'", "1 - radiatore", "2 - riflettore", "3 - direttori", "4 - tutto insieme"));
            // 1.0.15 (IC8TEM 17/11/2026 09:52, bozzetto approvato alle 09:57): fumetto fisso sul legame tra elementi e boom
            NotaFissa = "DL6WU lega le due misure. Con \"Numero di elementi\" il boom si calcola dalle spaziature di DL6WU; con \"Lunghezza del boom\" i direttori si aggiungono finche' il boom li contiene. " +
                "Il campo grigio non si scrive: mostra il valore ricavato. Con \"Tutti e due\" si danno entrambi e le spaziature si allungano o si accorciano in proporzione: si esce dal metodo e serve l'ottimizzazione NEC.";
        }

        protected override bool BoomFisso { get { return I("modo") == 2; } }

        static double Kdir(double ed, int n)
        {
            Func<double[], double> L = k => (k[1] - k[2] * Math.Log(n)) * (1 - k[3] * Math.Exp(-k[4] * n));
            if (ed <= Kd[0][0]) return L(Kd[0]);
            if (ed >= Kd[Kd.Length - 1][0]) return L(Kd[Kd.Length - 1]);
            for (int i = 0; i + 1 < Kd.Length; i++)
                if (ed >= Kd[i][0] && ed <= Kd[i + 1][0]) { double j = (ed - Kd[i][0]) / (Kd[i + 1][0] - Kd[i][0]); return L(Kd[i]) + j * (L(Kd[i + 1]) - L(Kd[i])); }
            return L(Kd[0]);
        }

        public static double CorrezioneBoom(double bd, int mont)
        {
            if (mont == 2 || bd <= 0) return 0;
            double bc1 = 733 * bd * (0.055 - bd) - 504 * bd * (0.03 - bd);
            if (mont == 1) bc1 /= 2;
            return bc1 * bd;
        }

        double Lam { get { return C0 / P("f"); } }
        double ED { get { return Math.Max(0.1, P("ed")) / 1000 / Lam; } }
        double DD { get { return Math.Max(0.1, P("dd")) / 1000 / Lam; } }

        protected override void Progetto(out List<double> pos, out List<double> lens)
        {
            pos = new List<double> { 0, 0.2 };
            double t = 0.2, bl = P("boom") / Lam;
            int nel = Math.Max(3, Math.Min(60, I("nel"))), n = 1;
            while (true)
            {
                double s = Sp[Math.Min(n, 14) - 1];
                if (I("modo") != 1) { if (pos.Count >= nel) break; }
                else if (t + s > bl + 1e-9 || pos.Count >= 60) break;
                t += s; pos.Add(t); n++;
            }
            // 1.0.15: tutti e due i dati -> spaziature dei direttori scalate in proporzione fino al boom dato (radiatore a 0,2 come DL6WU)
            FattoreSpaziature = 1;
            if (I("modo") == 2 && pos.Count >= 3)
            {
                double ultimo = pos[pos.Count - 1], k = (Math.Max(bl, 0.3) - 0.2) / (ultimo - 0.2);
                FattoreSpaziature = k;
                for (int i = 2; i < pos.Count; i++) pos[i] = 0.2 + (pos[i] - 0.2) * k;
            }
            double ed = ED, dd = DD;
            lens = new List<double>();
            lens.Add(((20.0 - 40) / (186.8769 * Math.Log(2 / ed) - 320) + 1) / 2);
            lens.Add((0.4777 - 1.0522 * dd + 0.43363 * Math.Pow(dd, -0.014891)) / 2 * 1.02);
            for (int i = 1; i <= pos.Count - 2; i++) lens.Add(Kdir(ed, i));
        }

        List<double> pos0, lens0;
        double FattoreSpaziature = 1;
        protected override Ott Partenza(List<double> pos, List<double> lens)
        {
            pos0 = pos; lens0 = lens;
            int fd = I("feed");
            Ott o = new Ott { sr = pos[1], s1 = pos.Count > 2 ? pos[2] - pos[1] : 0.075 };
            if (fd == 1) o.fp = 0.012;                                       // folded: distanza tra i conduttori
            else if (fd == 2) { o.fp = 0.084; o.sr = 0.052; o.s1 = 0.089; }  // LFA (G0KSC)
            else if (fd == 3) o.fp = 8;                                      // DG7YBN: angolo dei bracci (gradi)
            return o;
        }
        protected override string NomeFp { get { int fd = I("feed"); return fd == 1 ? "distanza dei conduttori" : fd == 2 ? "larghezza dell'anello" : fd == 3 ? "angolo dei bracci" : null; } }
        protected override double PassoFp { get { int fd = I("feed"); return fd == 1 ? 0.005 : fd == 2 ? 0.01 : 4; } }

        /// <summary>Posizioni e lunghezze (lambda) con i parametri di ottimizzazione.</summary>
        void Geometria(Ott p, out double[] pos, out double[] lens)
        {
            int n = pos0.Count, fd = I("feed");
            pos = pos0.ToArray(); lens = lens0.ToArray();
            if (fd == 2) lens[1] = 0.422;                     // LFA: lato lungo dell'anello
            double dR = p.sr - pos0[1];
            for (int i = 1; i < n; i++) pos[i] += dR;
            if (n > 2)
            {
                double w = fd == 2 ? p.fp : 0;
                double d1 = pos[1] + w + p.s1 - pos[2];
                for (int i = 2; i < n; i++) pos[i] += d1;
                if (BoomFisso)
                {
                    // boom fisso: D1 dove lo mettono sr e s1, l'ultimo direttore in fondo al boom; in mezzo le frazioni di DL6WU
                    // mescolate con quelle uniformi: prog 0 = proporzioni DL6WU, 1 = direttori equidistanti (come NBS TN 688), < 0 piu' progressive
                    double fine = pos0[n - 1], p2 = pos[2], c0 = pos0[2];
                    if (n == 3) pos[2] = fine;
                    else
                        for (int i = 2; i < n; i++)
                        {
                            double fr = (pos0[i] - c0) / (fine - c0), u = (double)(i - 2) / (n - 3);
                            double fp2 = fr + p.prog * (u - fr);
                            pos[i] = p2 + (fine - p2) * fp2;
                        }
                }
            }
            lens[0] *= p.kr; lens[1] *= p.kd;
            int m = n - 2;
            for (int i = 0; i < m; i++) { double u = m > 1 ? (double)i / (m - 1) : 0; lens[2 + i] = lens0[2 + i] * p.kdir * (1 + p.tap * (u - 0.5)); }
        }

        protected override string Mazzo(Ott p, double f, out double zRif)
        {
            double lam = C0 / f, rp = Math.Max(0.1, P("ed")) / 2000, rd = Math.Max(0.1, P("dd")) / 2000, seg = lam / 40;
            double[] pos, lens;
            Geometria(p, out pos, out lens);
            int fd = I("feed");
            zRif = fd == 1 ? 200 : 50;
            NecDeck d = new NecDeck(Title + " " + pos.Length + " el. " + Alim[fd]);
            int st = 0, ss = 1;
            for (int i = 0; i < pos.Length; i++)
            {
                double x = pos[i] * lam, h = lens[i] * lam / 2;
                if (i != 1) { d.Ws(seg, x, -h, 0, x, h, 0, rp, true, 3); continue; }
                if (fd == 0) { st = d.Ws(seg, x, -h, 0, x, h, 0, rd, true, 3); ss = (d.LastN + 1) / 2; }
                else if (fd == 1)
                {
                    double s = p.fp * lam;
                    st = d.Ws(seg, x, -h, 0, x, h, 0, rd, true, 3); int nn = d.LastN; ss = (nn + 1) / 2;
                    d.W(nn, x + s, -h, 0, x + s, h, 0, rd); d.W(1, x, -h, 0, x + s, -h, 0, rd); d.W(1, x, h, 0, x + s, h, 0, rd);
                }
                else if (fd == 2)
                {
                    double w = p.fp * lam;
                    st = d.Ws(seg, x, -h, 0, x, h, 0, rd, true, 3); int nn = d.LastN; ss = (nn + 1) / 2;
                    d.W(nn, x + w, -h, 0, x + w, h, 0, rd); d.W(3, x, -h, 0, x + w, -h, 0, rd); d.W(3, x, h, 0, x + w, h, 0, rd);
                }
                else
                {
                    double c = Math.Min(0.1 * lam / 2, h * 0.9), th = p.fp * Math.PI / 180, a = h - c;
                    st = d.Ws(seg, x, -c, 0, x, c, 0, rd, true, 3); ss = (d.LastN + 1) / 2;
                    d.Ws(seg, x, c, 0, x + a * Math.Sin(th), c + a * Math.Cos(th), 0, rd, false, 2);
                    d.Ws(seg, x, -c, 0, x + a * Math.Sin(th), -c - a * Math.Cos(th), 0, rd, false, 2);
                }
            }
            d.Ground(0);
            d.Ex(st, ss);
            return d.End(f);
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            if (firma != null && firma != Firma()) { ott = null; firma = null; passiFatti.Clear(); }
            List<double> pos, lens;
            Progetto(out pos, out lens);
            Ott p = ott ?? Partenza(pos, lens);
            if (ott != null) Partenza(pos, lens);   // pos0, lens0 aggiornati
            double f = P("f"), lam = Lam;
            double[] ps, ls;
            Geometria(p, out ps, out ls);
            int fd = I("feed"), mont = I("mont");
            double bdl = P("bd") / 1000 / lam, bc = CorrezioneBoom(bdl, mont);
            double boomL = ps[ps.Length - 1], gst = 9.2 + 3.39 * Math.Log(Math.Max(0.1, pos[pos.Count - 1]));
            o.Card("Elementi", ps.Length.ToString(), "riflettore, radiatore e " + (ps.Length - 2) + " direttori");
            o.Card("Boom", Calcolatori.N(boomL * lam, 3) + " m", Calcolatori.N(boomL, 2) + " lambda");
            int modo = I("modo");
            if (modo != 2) o.Card("Guadagno stimato DL6WU", Calcolatori.N(gst, 2) + " dBd", Calcolatori.N(gst + 2.15, 2) + " dBi (9,2 + 3,39 ln L)", true);
            else o.Card("Guadagno stimato DL6WU", "non vale", "fuori dal metodo: lo da' il NEC dopo \"Ottimizza (NEC)\"", true);
            // 1.0.15 (bozzetto approvato da IC8TEM il 17/11/2026 09:57): il dato che non si usa e' grigio e mostra il valore ricavato
            Hint("nel", null); Hint("boom", null); Hint("passo", null);
            if (modo == 0)
            {
                Calcolati["boom"] = boomL * lam;
                Hint("boom", "calcolata da DL6WU (" + Calcolatori.N(boomL, 2) + " λ)");
            }
            else if (modo == 1)
            {
                Calcolati["nel"] = ps.Length;
                Hint("nel", "calcolato: ci stanno " + ps.Length + " elementi (boom usato " + Calcolatori.N(boomL * lam, 3) + " m = " + Calcolatori.N(boomL, 2) + " λ)");
            }
            else
            {
                CalcParam pp = Par("passo"); if (pp != null) pp.Value = 4;
                Hint("passo", "passi 0-3 non disponibili: con \"Tutti e due\" l'ottimizzazione e' obbligatoria");
                int nel = ps.Length;
                double nat = 0.2; for (int i = 1; i <= nel - 2; i++) nat += Sp[Math.Min(i, 14) - 1];
                double k = FattoreSpaziature;
                o.AvvisoDati = "Fuori dal metodo DL6WU: " + nel + " elementi stanno su " + Calcolatori.N(nat * lam, 3) + " m (" + Calcolatori.N(nat, 2) + " λ); su " +
                    Calcolatori.N(P("boom"), 3) + " m (" + Calcolatori.N(P("boom") / lam, 2) + " λ) le spaziature dei direttori sono x" + Calcolatori.N(k, 2) + ". " +
                    "Le lunghezze di DL6WU non valgono piu' e nemmeno il guadagno stimato 9,2 + 3,39 ln L: i numeri veri li da' il NEC dopo \"Ottimizza (NEC)\". " +
                    "Al passo 4 si aggiunge la variabile progressione delle spaziature (come crescono lungo il boom, con il boom fisso)" +
                    (ott != null ? ": trovata " + Calcolatori.N(p.prog, 2) + " (0 = proporzioni DL6WU, 1 = direttori equidistanti)." : ".");
                if (k > 1.3) o.Warnings.Add("Direttori molto radi (spaziature x" + Calcolatori.N(k, 2) + " rispetto a DL6WU): a parita' di boom servirebbero piu' elementi; di solito il NEC trova meno guadagno di una DL6WU dello stesso boom. Confronta col modo \"Lunghezza del boom\".");
                else if (k < 0.7) o.Warnings.Add("Direttori molto fitti (spaziature x" + Calcolatori.N(k, 2) + " rispetto a DL6WU): a parita' di boom gli elementi in piu' danno poco guadagno (il guadagno dipende soprattutto dal boom) e stringono la banda.");
                if (P("boom") / lam <= 0.3) o.Warnings.Add("Boom troppo corto per questi elementi: portato a 0,3 lambda.");
            }
            o.Card("Alimentazione", Alim[fd], fd == 1 ? "200 Ω con balun 4:1" : "50 Ω");
            if (mont != 2) o.Card("Correzione del boom", "+" + Calcolatori.N(bc * lam * 1000, 1) + " mm", "per ogni elemento che attraversa il boom (G3SEK)");
            for (int i = 0; i < ps.Length; i++)
            {
                string nome = i == 0 ? "Riflettore" : i == 1 ? "Radiatore" : "D" + (i - 1);
                double lt = ls[i] * lam * 1000, taglio = lt + (i == 1 && fd != 0 ? 0 : bc * lam * 1000);
                string v = "a " + Calcolatori.N(ps[i] * lam * 1000, 0) + " mm, lunghezza " + Calcolatori.N(lt, 1) + " mm";
                if (i == 1 && fd == 1) v += " (folded, conduttori a " + Calcolatori.N(p.fp * lam * 1000, 0) + " mm)";
                else if (i == 1 && fd == 2) v = "anello da " + Calcolatori.N(ps[i] * lam * 1000, 0) + " a " + Calcolatori.N((ps[i] + p.fp) * lam * 1000, 0) + " mm, lati lunghi " + Calcolatori.N(lt, 1) + " mm, alimentato al centro del lato posteriore";
                else if (i == 1 && fd == 3) v += " (centro diritto " + Calcolatori.N(Math.Min(0.1, ls[i]) * lam * 1000, 0) + " mm, bracci piegati in avanti di " + Calcolatori.N(p.fp, 1) + " gradi)";
                if (mont != 2 && !(i == 1 && fd != 0)) v += "; da tagliare " + Calcolatori.N(taglio, 1) + " mm";
                o.Add(nome, v);
            }
            Passi(o);
            if (ED < 0.001 || ED > 0.02) o.Warnings.Add("Diametro dei parassiti fuori dalla tabella di DL6WU (0,001-0,02 lambda): si usa il valore piu' vicino.");
            if (bdl > 0.06 && mont != 2) o.Warnings.Add("Boom piu' grosso di 0,06 lambda: la correzione di G3SEK non vale piu'.");
            if (pos[pos.Count - 1] < 2.2) o.Warnings.Add("Boom sotto 2,2 lambda: DL6WU ha tarato il metodo da 2,2 a 39 lambda; sotto vanno meglio i progetti corti (DK7ZB, YU7EF).");
            o.Warnings.Add("Il modello NEC e' senza boom (elementi isolati in spazio libero): le lunghezze \"da tagliare\" aggiungono la correzione del boom per il montaggio scelto.");
            if (fd == 1) o.Warnings.Add("Folded dipole: impedenza riferita a 200 Ω (balun 4:1 a mezz'onda di coassiale); il ROS della verifica in basso e' su Z0 del programma.");
            o.Formula = "Spaziature DL6WU (lambda): R 0, A 0,2, poi 0,075 0,180 0,215 0,250 0,280 0,300 0,315 0,330 0,345 0,360 0,375 0,390 0,400 ... " +
                "Direttore N: (K1 - K2 ln N)(1 - K3 e^-K4N), K dalla tabella sul diametro. Riflettore ((20 - 40)/(186,88 ln(2/d) - 320) + 1)/2, radiatore (0,4777 - 1,0522 d + 0,43363 d^-0,0149)/2 x 1,02. " +
                "Boom (G3SEK): BD (733 BD (0,055 - BD) - 504 BD (0,03 - BD)). Guadagno 9,2 + 3,39 ln(L/lambda) dBd: ogni raddoppio del boom da' solo 2,35 dB. " +
                "Ottimizzazione: simplesso di Nelder e Mead sul NEC (radiatore, riflettore, direttori, poi tutto). " +
                "Tutti e due i dati (1.0.15): spaziature dei direttori x k = (boom - 0,2) / (boom DL6WU - 0,2); al passo 4 la progressione p mescola le posizioni di DL6WU (p = 0) con direttori equidistanti (p = 1), con D1 e l'ultimo direttore fermi. " +
                "Letteratura sul legame elementi-boom: DL6WU (1982) da' le spaziature che crescono fino a 0,4 lambda e fa dipendere il guadagno dal boom (9,2 + 3,39 ln L); " +
                "NBS TN 688 (Viezbicke 1976) da' progetti ottimizzati per sei lunghezze di boom (da 0,4 a 4,2 lambda) con i direttori equidistanti: simile a DL6WU nel guadagno che cresce col boom, diverso perche' le spaziature non crescono lungo il boom (qui: progressione p = 1); " +
                "Cheng e Chen (IEEE Trans. AP, 1973-75) ottimizzano insieme lunghezze e spaziature di una 6 elementi: come qui le spaziature diventano variabili, ma con un metodo a perturbazioni e senza vincolo sul boom. " +
                "La regola comune (DL6WU, G3SEK) e' che il guadagno dipende soprattutto dalla lunghezza del boom e poco dal numero di elementi, purche' le spaziature siano ragionevoli. Fonti: " + Source + "; P. Viezbicke, NBS Technical Note 688 (1976); D. K. Cheng e C. A. Chen.";
            double z;
            o.Nec = Mazzo(p, f, out z);
            o.Plane = "xy";
            o.HideGround = true;
            // guadagno stimato contro la lunghezza del boom: si vede quanto poco sale allungando
            o.Chart = new CalcChart { Title = "Guadagno DL6WU contro lunghezza del boom", XLabel = "boom (lambda)", YLabel = "dBi" };
            int nPt = 40;
            o.Chart.X = new double[nPt]; o.Chart.Y = new double[nPt];
            for (int i = 0; i < nPt; i++) { double b = 1 + i; o.Chart.X[i] = b; o.Chart.Y[i] = 9.2 + 3.39 * Math.Log(b) + 2.15; }
            o.Chart.MarkX = pos[pos.Count - 1]; o.Chart.MarkText = Calcolatori.N(gst + 2.15, 1) + " dBi";
            return o;
        }
    }

    // =============================================================================================
    public class CalcQuagi : CalcYagiBase
    {
        const double QR = 1.0548, QA = 1.0382, QD1 = 0.4390, QDd = 0.0023;
        static readonly double[] Qs = { 0.256, 0.192, 0.404, 0.214 };
        const double QsN = 0.318;

        public CalcQuagi()
        {
            Key = "quagi"; Title = "Quagi e quad a piu' elementi"; Family = "Direttive";
            Source = "W. Overbeck N6NB (QST 4/1977, 2/1978), modello NEC di L. B. Cebik W4RNL (Quagi and Yagi on 2 Meters), tabelle di W3PM (QST 1/1990)";
            Params.Add(new CalcParam("f", "Frequenza di progetto", 144.3, "MHz"));
            Params.Add(new CalcParam("bw", "Banda da coprire (0 = solo la frequenza)", 600, "kHz"));
            Params.Add(new CalcParam("nel", "Numero di elementi", 8, "", "Anello riflettore, anello alimentato e direttori (da 3 a 40)."));
            Params.Add(CalcParam.Scelta("tipo", "Direttori", 0, "Quagi di N6NB: direttori a bacchetta; quad: anche i direttori sono anelli.", "Bacchette (quagi N6NB)", "Anelli (quad)"));
            Params.Add(new CalcParam("dl", "Diametro del filo degli anelli", 2, "mm", "N6NB: rame da 2 mm (#12)."));
            Params.Add(new CalcParam("dr", "Diametro delle bacchette", 3.2, "mm", "N6NB: 1/8\"."));
            Params.Add(CalcParam.Scelta("obj", "Obiettivo dell'ottimizzazione", 1, "Come nella Yagi DL6WU.", "Guadagno", "Bilanciato", "F/B alto"));
            Params.Add(CalcParam.Scelta("passo", "Ottimizza fino al passo", 4, "0 = misure N6NB/Cebik; 1 anello alimentato (50 ohm); 2 riflettore; 3 direttori; 4 tutto insieme.",
                "0 - N6NB/Cebik com'e'", "1 - anello alimentato", "2 - riflettore", "3 - direttori", "4 - tutto insieme"));
        }

        double Lam { get { return C0 / P("f"); } }
        protected override string NomeFp { get { return null; } }
        protected override double PassoFp { get { return 0; } }

        List<double> pos0, lens0;
        protected override void Progetto(out List<double> pos, out List<double> lens)
        {
            int n = Math.Max(3, Math.Min(40, I("nel")));
            pos = new List<double> { 0 };
            double t = 0;
            for (int i = 0; i < n - 1; i++) { t += i < Qs.Length ? Qs[i] : QsN; pos.Add(t); }
            lens = new List<double> { QR, QA };
            for (int i = 0; i < n - 2; i++) { double l = QD1 - QDd * i; lens.Add(I("tipo") == 1 ? l * QA / QD1 * 0.975 : l); }
        }
        protected override Ott Partenza(List<double> pos, List<double> lens) { pos0 = pos; lens0 = lens; return new Ott { sr = pos[1], s1 = pos.Count > 2 ? pos[2] - pos[1] : 0.19 }; }

        void Geometria(Ott p, out double[] pos, out double[] lens)
        {
            int n = pos0.Count;
            pos = pos0.ToArray(); lens = lens0.ToArray();
            double dR = p.sr - pos0[1];
            for (int i = 1; i < n; i++) pos[i] += dR;
            if (n > 2) { double d1 = pos[1] + p.s1 - pos[2]; for (int i = 2; i < n; i++) pos[i] += d1; }
            lens[0] *= p.kr; lens[1] *= p.kd;
            int m = n - 2;
            for (int i = 0; i < m; i++) { double u = m > 1 ? (double)i / (m - 1) : 0; lens[2 + i] = lens0[2 + i] * p.kdir * (1 + p.tap * (u - 0.5)); }
        }

        protected override string Mazzo(Ott p, double f, out double zRif)
        {
            zRif = 50;
            double lam = C0 / f, rl = Math.Max(0.1, P("dl")) / 2000, rr = Math.Max(0.1, P("dr")) / 2000, seg = lam / 40;
            double[] pos, lens;
            Geometria(p, out pos, out lens);
            bool quad = I("tipo") == 1;
            NecDeck d = new NecDeck(Title + " " + pos.Length + " el.");
            int st = 0, ss = 1;
            for (int i = 0; i < pos.Length; i++)
            {
                double x = pos[i] * lam;
                if (i >= 2 && !quad) { double h = lens[i] * lam / 2; d.Ws(seg, x, -h, 0, x, h, 0, rr, true, 3); continue; }
                double a = lens[i] * lam / 8;   // meta' lato del quadrato
                int t = d.Ws(seg, x, -a, -a, x, a, -a, rl, true, 3); int nn = d.LastN;
                if (i == 1) { st = t; ss = (nn + 1) / 2; }
                d.W(nn, x, a, -a, x, a, a, rl); d.W(nn, x, a, a, x, -a, a, rl); d.W(nn, x, -a, a, x, -a, -a, rl);
            }
            d.Ground(0);
            d.Ex(st, ss);
            return d.End(f);
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            if (firma != null && firma != Firma()) { ott = null; firma = null; passiFatti.Clear(); }
            List<double> pos, lens;
            Progetto(out pos, out lens);
            Ott p0 = Partenza(pos, lens), p = ott ?? p0;
            double f = P("f"), lam = Lam;
            double[] ps, ls;
            Geometria(p, out ps, out ls);
            bool quad = I("tipo") == 1;
            double boomL = ps[ps.Length - 1], gYagi = 9.2 + 3.39 * Math.Log(Math.Max(0.1, boomL)) + 2.15;
            o.Card("Elementi", ps.Length.ToString(), quad ? "tutti anelli" : "2 anelli e " + (ps.Length - 2) + " bacchette");
            o.Card("Boom", Calcolatori.N(boomL * lam, 3) + " m", Calcolatori.N(boomL, 2) + " lambda");
            o.Card("Yagi DL6WU con lo stesso boom", Calcolatori.N(gYagi, 2) + " dBi", "stima; la quagi sta 0,3-0,6 dB sotto (Cebik, prove con nec2c)", true);
            for (int i = 0; i < ps.Length; i++)
            {
                bool anello = i < 2 || quad;
                string nome = i == 0 ? "Anello riflettore" : i == 1 ? "Anello alimentato" : "D" + (i - 1);
                string v = "a " + Calcolatori.N(ps[i] * lam * 1000, 0) + " mm, " + (anello ? "perimetro " + Calcolatori.N(ls[i] * lam * 1000, 0) + " mm (lato " + Calcolatori.N(ls[i] * lam * 250, 1) + " mm)" : "lunghezza " + Calcolatori.N(ls[i] * lam * 1000, 1) + " mm");
                if (i == 1) v += ", alimentato al centro del lato basso (polarizzazione orizzontale)";
                o.Add(nome, v);
            }
            Passi(o);
            o.Warnings.Add("Boom isolante (N6NB usa legno): con un boom metallico tutte le misure cambiano. Il modello NEC e' in spazio libero.");
            o.Formula = "Quagi N6NB: anello riflettore e anello alimentato, direttori a bacchetta. Proporzioni dal modello NEC di Cebik (144,5 MHz): riflettore 1,0548 lambda di perimetro, " +
                "alimentato 1,0382, D1 0,4390 lambda e poi 0,0023 lambda in meno per ognuno; spaziature 0,256 0,192 0,404 0,214 e poi 0,318 lambda. " +
                "Quad: direttori ad anello con perimetro 0,975 x alimentato x D/D1. Ottimizzazione come la Yagi DL6WU. Fonti: " + Source + ".";
            double z;
            o.Nec = Mazzo(p, f, out z);
            o.Plane = "xy";
            o.HideGround = true;
            o.Chart = new CalcChart { Title = "Guadagno della Yagi DL6WU con lo stesso boom (riferimento)", XLabel = "boom (lambda)", YLabel = "dBi" };
            o.Chart.X = new double[30]; o.Chart.Y = new double[30];
            for (int i = 0; i < 30; i++) { double b = 0.5 + 0.25 * i; o.Chart.X[i] = b; o.Chart.Y[i] = 9.2 + 3.39 * Math.Log(b) + 2.15; }
            o.Chart.MarkX = boomL; o.Chart.MarkText = "boom attuale";
            return o;
        }
    }
}

namespace YaNec.Engine
{
    // =============================================================================================
    // 0.5.0 (IC8TEM 15/11/2026 15:34 "riusciamo anche a calcolare una loop yagi in 1296 tutti loop?", 15:42 "si"): loop Yagi di
    // C. Angle N6CA (ARRL Antenna Book 24a ed., sez. 15.3.7, fig. 15.47 e 15.48), dal progetto di M. Walters G3JVL (RSGB VHF/UHF Manual,
    // anni '70; programma Loopquad su g3jvl.com). Tutti gli elementi sono anelli circolari di striscia di alluminio da 1/4" x 0,0325"
    // (l'alimentato di rame), due riflettori uguali, boom da 3/4". Misure (pollici, da foro a foro = circonferenza dell'anello):
    //   1296: R1, R2 9,700; alimentato 2 x 4,643; D1-D11 8,250; D12-D17 8,000; D18 e oltre 7,700
    //   1283: 9,829 / 2 x 4,704 / 8,359 / 8,106 / 7,802      1270: 9,929 / 2 x 4,752 / 8,445 / 8,189 / 7,882
    //   spaziature 1296: R1-R2 3,050, R2-A 1,180, A-D1 1,120, D1-D2 0,830, poi 1,780 fino a D6, poi 3,560 (1283 e 1270 nella tabella)
    // ARRL: boom da 6 piedi circa 18 dBi, da 12 piedi circa 20,5 dBi. Prova con nec2c (scratchpad yagi5/loopyagi.py, anelli a poligono di
    // 16 lati con la circonferenza esatta, raggio equivalente della striscia = larghezza/4): 6 piedi (25 el.) 18,60 dBi, 50,0 +j9,5 ohm;
    // 12 piedi (45 el.) 21,21 dBi, 51,8 +j6,5 ohm. Con 12 / 24 lati 18,71 / 18,31 dBi: oltre 16 lati il segmento scende sotto 8 volte il
    // raggio della striscia e il NEC-2 perde precisione (guida del NEC-2), per questo 16 lati.
    public class CalcLoopYagi : CalcYagiBase
    {
        static readonly double[] Ft = { 1296, 1283, 1270 };
        static readonly double[][] Lt = { new[] { 9.700, 9.286, 8.250, 8.000, 7.700 }, new[] { 9.829, 9.408, 8.359, 8.106, 7.802 }, new[] { 9.929, 9.504, 8.445, 8.189, 7.882 } };
        static readonly double[][] St = { new[] { 3.050, 1.180, 1.120, 0.830, 1.780, 3.560 }, new[] { 3.090, 1.193, 1.135, 0.841, 1.804, 3.607 }, new[] { 3.122, 1.202, 1.146, 0.850, 1.822, 3.644 } };
        const int Lati = 16;

        public CalcLoopYagi()
        {
            Key = "loopyagi"; Title = "Loop Yagi (N6CA / G3JVL)"; Family = "Direttive";
            Source = "C. Angle N6CA, ARRL Antenna Book 24a ed. sez. 15.3.7 (fig. 15.47 e 15.48); M. Walters G3JVL (loop Yagi, RSGB VHF/UHF Manual; Loopquad)";
            Params.Add(new CalcParam("f", "Frequenza di progetto", 1296.1, "MHz", "Le misure della tabella scelta si scalano a questa frequenza (in lunghezze d'onda)."));
            Params.Add(new CalcParam("bw", "Banda da coprire (0 = solo la frequenza)", 12000, "kHz"));
            Params.Add(CalcParam.Scelta("tab", "Tabella di N6CA", 0, "1296: segnali deboli; 1283: da 1280 a 1300 MHz; 1270: FM e satelliti (modo L).", "1296 MHz", "1283 MHz", "1270 MHz"));
            Params.Add(new CalcParam("boom", "Lunghezza del boom", 1.83, "m", "N6CA: 6 piedi (1,83 m, circa 18 dBi) o 12 piedi (3,66 m, circa 20,5 dBi); dopo D6 i direttori sono tutti alla stessa distanza."));
            Params.Add(new CalcParam("w", "Larghezza della striscia", 6.35, "mm", "N6CA: 1/4\" (6,35 mm) per 0,0325\"; nel NEC il raggio equivalente e' larghezza/4 (Butler)."));
            Params.Add(CalcParam.Scelta("obj", "Obiettivo dell'ottimizzazione", 1, "Come nella Yagi DL6WU.", "Guadagno", "Bilanciato", "F/B alto"));
            Params.Add(CalcParam.Scelta("passo", "Ottimizza fino al passo", 0, "0 = misure di N6CA; 1 anello alimentato; 2 riflettori; 3 direttori; 4 tutto insieme.",
                "0 - N6CA com'e'", "1 - anello alimentato", "2 - riflettori", "3 - direttori", "4 - tutto insieme"));
        }

        protected override string NomeFp { get { return null; } }
        protected override double PassoFp { get { return 0; } }
        List<double> pos0, lens0;

        protected override void Progetto(out List<double> pos, out List<double> lens)
        {
            int t = Math.Max(0, Math.Min(2, I("tab")));
            double lamIn = 299.792458 / Ft[t] / 0.0254;   // lunghezza d'onda della tabella in pollici
            double boomL = P("boom") / (C0 / P("f"));      // boom in lambda
            pos = new List<double> { 0 };
            for (int i = 0; ; i++)
            {
                double s = (i < 4 ? St[t][i] : (i < 8 ? St[t][4] : St[t][5])) / lamIn;
                if (pos[pos.Count - 1] + s > boomL + 1e-9 || pos.Count >= 120) break;
                pos.Add(pos[pos.Count - 1] + s);
            }
            lens = new List<double>();
            for (int k = 0; k < pos.Count; k++)
            {
                double l = k < 2 ? Lt[t][0] : k == 2 ? Lt[t][1] : (k - 2 <= 11 ? Lt[t][2] : (k - 2 <= 17 ? Lt[t][3] : Lt[t][4]));
                lens.Add(l / lamIn);
            }
        }
        protected override Ott Partenza(List<double> pos, List<double> lens)
        {
            pos0 = pos; lens0 = lens;
            return new Ott { sr = pos.Count > 2 ? pos[2] - pos[1] : 0.13, s1 = pos.Count > 3 ? pos[3] - pos[2] : 0.12 };
        }

        void Geometria(Ott p, out double[] pos, out double[] lens)
        {
            int n = pos0.Count;
            pos = pos0.ToArray(); lens = lens0.ToArray();
            if (n > 2) { double d = p.sr - (pos0[2] - pos0[1]); for (int i = 2; i < n; i++) pos[i] += d; }
            if (n > 3) { double d = pos[2] + p.s1 - pos[3]; for (int i = 3; i < n; i++) pos[i] += d; }
            lens[0] *= p.kr; if (n > 1) lens[1] *= p.kr; if (n > 2) lens[2] *= p.kd;
            int m = n - 3;
            for (int i = 0; i < m; i++) { double u = m > 1 ? (double)i / (m - 1) : 0; lens[3 + i] = lens0[3 + i] * p.kdir * (1 + p.tap * (u - 0.5)); }
        }

        protected override string Mazzo(Ott p, double f, out double zRif)
        {
            zRif = 50;
            double lam = C0 / f, r = Math.Max(0.1, P("w")) / 4000;
            double[] pos, lens;
            Geometria(p, out pos, out lens);
            NecDeck d = new NecDeck(Title + " " + pos.Length + " el.");
            int st = 0;
            for (int k = 0; k < pos.Length; k++)
            {
                double x = pos[k] * lam, per = lens[k] * lam, R = per / (2 * Lati * Math.Sin(Math.PI / Lati));
                for (int j = 0; j < Lati; j++)
                {
                    double a1 = -Math.PI / 2 - Math.PI / Lati + 2 * Math.PI * j / Lati, a2 = a1 + 2 * Math.PI / Lati;
                    bool alim = k == 2 && j == 0;   // lato basso orizzontale dell'anello alimentato: polarizzazione orizzontale
                    int t = d.W(alim ? 3 : 1, x, R * Math.Cos(a1), R * Math.Sin(a1), x, R * Math.Cos(a2), R * Math.Sin(a2), r);
                    if (alim) st = t;
                }
            }
            d.Ground(0);
            d.Ex(st, 2);
            return d.End(f);
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            if (firma != null && firma != Firma()) { ott = null; firma = null; passiFatti.Clear(); }
            List<double> pos, lens;
            Progetto(out pos, out lens);
            Ott p0 = Partenza(pos, lens), p = ott ?? p0;
            double f = P("f"), lam = C0 / f;
            double[] ps, ls;
            Geometria(p, out ps, out ls);
            o.Card("Elementi", ps.Length.ToString(), "2 riflettori, l'anello alimentato e " + Math.Max(0, ps.Length - 3) + " direttori", true);
            o.Card("Boom usato", Calcolatori.N(ps[ps.Length - 1] * lam, 3) + " m", Calcolatori.N(ps[ps.Length - 1], 2) + " lambda");
            o.Card("ARRL (N6CA)", "6 piedi circa 18 dBi, 12 piedi circa 20,5 dBi", "nec2c: 18,60 e 21,21 dBi con le misure 1296");
            for (int k = 0; k < ps.Length; k++)
            {
                string nome = k == 0 ? "Riflettore 1" : k == 1 ? "Riflettore 2" : k == 2 ? "Anello alimentato" : "D" + (k - 2);
                o.Add(nome, "a " + Calcolatori.N(ps[k] * lam * 1000, 1) + " mm, circonferenza " + Calcolatori.N(ls[k] * lam * 1000, 1) + " mm (striscia " +
                    Calcolatori.N(ls[k] * lam * 1000 + 6.35, 1) + " mm con i fori a 3,2 mm dalle punte), diametro " + Calcolatori.N(ls[k] * lam * 1000 / Math.PI, 1) + " mm");
            }
            Passi(o);
            o.Warnings.Add("Il modello NEC e' senza boom: N6CA fissa gli anelli su un boom di alluminio da 3/4\" e avverte che con un altro boom le misure cambiano.");
            o.Warnings.Add("Anelli fatti a poligono di " + Lati + " lati con la circonferenza esatta; con piu' lati il segmento scende sotto 8 volte il raggio della striscia e il NEC-2 perde precisione.");
            if (Math.Abs(f / Ft[I("tab")] - 1) > 0.02) o.Warnings.Add("Frequenza lontana da quella della tabella: le misure sono scalate in lunghezze d'onda, ma la larghezza della striscia e il boom no.");
            o.Formula = "Misure di N6CA (ARRL 24a ed., fig. 15.47-15.48) in lunghezze d'onda della tabella scelta, scalate alla frequenza di progetto; circonferenza = distanza tra i fori. " +
                "Raggio equivalente della striscia = larghezza/4. Ottimizzazione come la Yagi DL6WU (riflettori insieme, alimentato, direttori). Fonti: " + Source + ".";
            double z;
            o.Nec = Mazzo(p, f, out z);
            o.Plane = "xz";
            o.HideGround = true;
            o.Chart = new CalcChart { Title = "Guadagno della Yagi DL6WU con lo stesso boom (riferimento)", XLabel = "boom (lambda)", YLabel = "dBi" };
            o.Chart.X = new double[40]; o.Chart.Y = new double[40];
            for (int i = 0; i < 40; i++) { double b = 1 + 0.5 * i; o.Chart.X[i] = b; o.Chart.Y[i] = 9.2 + 3.39 * Math.Log(b) + 2.15; }
            o.Chart.MarkX = ps[ps.Length - 1]; o.Chart.MarkText = "boom attuale";
            return o;
        }
    }
}
