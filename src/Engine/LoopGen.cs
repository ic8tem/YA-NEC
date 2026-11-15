// YA-NEC - generatore di loop per la scheda Loop della barra Geometria (bozzetto approvato doc/bozzetto-loop.png,
// "approvato cosi' com'e', costruisci" di IC8TEM del 13/11/2026). GPL-3.0.
//
// Loop tondo (poligono di N lati), quadrato, ottagono o di N lati; una o piu' spire su un cilindro (elica: le estremita'
// si chiudono con un filo lungo l'asse che porta il condensatore); tubo o fascia larga; rame o alluminio (LD 5);
// piano verticale XZ, verticale YZ od orizzontale, con la base alla quota data; condensatore di accordo (LD 0) calcolato per la
// risonanza; alimentazione con anello di Faraday (diametro D/k), gamma (asta di lunghezza D/k) o diretta.
// Esito: NEC-2 (L dalla reattanza del loop senza condensatore, C di accordo, R di radiazione senza perdite, R di perdita,
// efficienza, guadagno, Q, banda) confrontato con le formule del calcolatore Loop magnetica (W5QJR e AA5TB).
// Fascia larga: raggio equivalente = larghezza / 4 (C. M. Butler, "The equivalent radius of a narrow conducting strip",
// IEEE Trans. AP-30, 1982).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading;

namespace YaNec.Engine
{
    public class LoopSpec
    {
        public int Forma;              // 0 cerchio, 1 quadrato, 2 ottagono, 3 N lati
        public int Lati = 6;           // con Forma = 3
        public double Misura = 1;      // diametro (cerchio) o lato (poligoni), m
        public int Spire = 1;
        public double Passo;           // m per spira (piu' spire)
        public double Conduttore = 0.022;   // diametro del tubo o larghezza della fascia, m
        public bool Fascia;
        public bool Alluminio;
        public int Piano;              // 0 verticale XZ, 1 verticale YZ, 2 orizzontale
        public double Base = 3;        // quota del punto piu' basso, m
        public int Segmenti;           // 0 = automatico (per il cerchio = numero di lati)
        public bool Condensatore = true;
        public double CondensatorePf;  // 0 = automatico (risonanza)
        public int Alimentazione;      // 0 anello di Faraday, 1 gamma, 2 diretta, 3 nessuna (anello passivo, approvata il 13/11/2026)
        public double Rapporto = 5;    // D/k dell'anello o dell'asta
        public double FreqMHz = 7.1;
        public int TagIniziale = 1;
        public LoopSpec Clone() { return (LoopSpec)MemberwiseClone(); }
    }

    public class LoopEsito
    {
        public double L, C, Rr, Rp, Eff, Gdbi, Q, Banda;
        public double Lf, Cf, Rrf, Rpf, Efff, Qf;     // formule
        public Complex Zfinale;
        public double Ros;
        public int Segmenti, MaxSegmenti;
        public double PerimetroLambda;
        public double ScartoL = double.NaN, ScartoRr = double.NaN;   // % con il doppio dei segmenti
        public string Nota = "";
    }

    public static class LoopGen
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static string N(double v) { return Math.Abs(v) < 1e-12 ? "0" : v.ToString("0.#########", CI); }
        const double Mu0 = 4e-7 * Math.PI;

        public static double Raggio(LoopSpec s) { return s.Fascia ? s.Conduttore / 4 : s.Conduttore / 2; }
        public static double Sigma(LoopSpec s) { return s.Alluminio ? 2.5e7 : 5.8e7; }

        static int LatiPoligono(LoopSpec s, int nCerchio) { return s.Forma == 0 ? nCerchio : s.Forma == 1 ? 4 : s.Forma == 2 ? 8 : Math.Max(3, s.Lati); }

        /// <summary>Perimetro e area di una spira.</summary>
        public static void Dimensioni(LoopSpec s, out double perim, out double area, out double rCirc)
        {
            if (s.Forma == 0) { rCirc = s.Misura / 2; perim = Math.PI * s.Misura; area = Math.PI * rCirc * rCirc; return; }
            int n = LatiPoligono(s, 4);
            rCirc = s.Misura / (2 * Math.Sin(Math.PI / n));
            perim = n * s.Misura;
            area = 0.5 * n * rCirc * rCirc * Math.Sin(2 * Math.PI / n);
        }

        /// <summary>Segmenti massimi per la regola dei 2,5 diametri (ON4UN, MMANA) e numero automatico.</summary>
        public static void Segmentazione(LoopSpec s, out int auto, out int max)
        {
            double perim, area, rc;
            Dimensioni(s, out perim, out area, out rc);
            double d = 2 * Raggio(s);
            max = Math.Max(4, (int)Math.Floor(perim * Math.Max(1, s.Spire) / (2.5 * d)));
            int perSpira = Math.Max(4, (int)Math.Floor(perim / (2.5 * d)));
            auto = Math.Min(36, perSpira);
            if (s.Forma == 0 && auto % 2 == 1) auto--;
            auto = Math.Max(12, auto);
        }

        /// <summary>Scrive il loop in un modello (nuovo o esistente). tagC/segC: dove sta il condensatore.</summary>
        public static NecModel Costruisci(LoopSpec s, NecModel dove, bool conPerdite, double capF, bool conSorgente, out int tagC, out int segC)
        {
            NecModel m;
            if (dove != null) m = dove.Clone();
            else
            {
                // modello vuoto: Parse vuole almeno un filo, quindi se ne mette uno provvisorio e lo si toglie (collaudo 0.4.2, proc. 19)
                m = NecModel.Parse("CM loop\nCE\nGW 1 1 0 0 0 0 0 1 0.001\nGE 0\nFR 0 1 0 0 " + N(s.FreqMHz) + " 0\nEN");
                m.Geometry.Clear();
            }
            int tag = s.TagIniziale;
            foreach (GeomCard g in m.Geometry) tag = Math.Max(tag, g.I1 + 1);
            double a = Raggio(s), perim, area, rc;
            Dimensioni(s, out perim, out area, out rc);
            int auto, max;
            Segmentazione(s, out auto, out max);
            int nCerchio = s.Segmenti > 0 && s.Forma == 0 ? Math.Max(8, s.Segmenti + s.Segmenti % 2) : auto;
            int lati = LatiPoligono(s, nCerchio);
            int segLato = s.Forma == 0 ? 1 : Math.Max(1, (s.Segmenti > 0 ? s.Segmenti : auto) / lati);
            if (segLato % 2 == 0) segLato++;   // sorgente e condensatore al centro di un lato
            int spire = Math.Max(1, s.Spire);
            double passo = spire > 1 ? Math.Max(s.Passo, 4 * a) : 0;
            double apo = rc * Math.Cos(Math.PI / lati);   // il lato inferiore sta alla quota "base"
            // u, v nel piano del loop rispetto al centro (v verso l'alto nei piani verticali), w lungo l'asse del cilindro
            Func<double, double, double, double[]> P3 = (u, v, w) =>
            {
                if (s.Piano == 0) return new[] { u, w, s.Base + apo + v };
                if (s.Piano == 1) return new[] { w, u, s.Base + apo + v };
                return new[] { u, v, s.Base + w };
            };
            List<double[]> pts = new List<double[]>();
            for (int t = 0; t < spire; t++)
                for (int k = 0; k < lati; k++)
                {
                    double ang = -Math.PI / 2 - Math.PI / lati + 2 * Math.PI * k / lati;
                    pts.Add(new[] { rc * Math.Cos(ang), rc * Math.Sin(ang), passo * (t + (double)k / lati) });
                }
            pts.Add(new[] { pts[0][0], pts[0][1], passo * spire });
            List<int> tagsLoop = new List<int>();
            int tagTop = -1, tagBottom = -1;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                double[] p1 = P3(pts[i][0], pts[i][1], pts[i][2]), p2 = P3(pts[i + 1][0], pts[i + 1][1], pts[i + 1][2]);
                m.Geometry.Add(Gw(tag, segLato, p1, p2, a));
                int k = i % lati;
                if (i < lati && k == 0) tagBottom = tag;
                if (i >= (spire - 1) * lati && k == lati / 2) tagTop = tag;
                tagsLoop.Add(tag);
                tag++;
            }
            if (spire > 1)
            {
                // piu' spire: chiusura lungo l'asse, porta il condensatore
                double[] p1 = P3(pts[pts.Count - 1][0], pts[pts.Count - 1][1], pts[pts.Count - 1][2]), p2 = P3(pts[0][0], pts[0][1], pts[0][2]);
                int ns = Math.Max(1, (int)Math.Round(passo * spire / (perim / Math.Max(1, lati * segLato))));
                if (ns % 2 == 0) ns++;
                m.Geometry.Add(Gw(tag, ns, p1, p2, a));
                tagTop = tag; tagsLoop.Add(tag); tag++;
                segC = (ns + 1) / 2;
            }
            else segC = (segLato + 1) / 2;
            tagC = tagTop;
            if (conPerdite) foreach (int t in tagsLoop) m.Loads.Add(new LoadSpec { Type = 5, Tag = t, From = 0, To = 0, F1 = Sigma(s) });
            if (capF > 0 && s.Alimentazione != 3) m.Loads.Add(new LoadSpec { Type = 0, Tag = tagC, From = segC, To = segC, F1 = 0, F2 = 0, F3 = capF });
            int segB = (segLato + 1) / 2;
            double dk = s.Misura * (s.Forma == 0 ? 1 : 1 / Math.Sin(Math.PI / lati)) / Math.Max(1.5, s.Rapporto);   // D/k
            if (s.Alimentazione == 3) { }   // anello passivo (direttore o riflettore): nessuna sorgente e nessun condensatore
            else if (s.Alimentazione == 2)
            {
                if (conSorgente) m.Sources.Add(new SourceSpec { Tag = tagBottom, Seg = segB });
            }
            else if (s.Alimentazione == 0)
            {
                // anello di Faraday nel piano del loop, in basso dentro il loop, filo da 2 mm di rame
                double rcc = dk / 2, ac = 0.001, gap = a + 0.01 + ac;
                double vc = -apo + gap + rcc;
                // collaudo 0.4.20 (prova con EZNEC): con 16 lati l'anello D/5 a 7,1 MHz aveva segmenti di 3,9 cm, sotto lambda/1000 (4,2 cm).
                // Ora i lati sono quanti ne stanno con segmenti di almeno lambda/1000, pari, tra 8 e 16.
                double lamF = 299.792458 / Math.Max(1e-6, s.FreqMHz);
                int nc = (int)Math.Floor(Math.PI * 2 * rcc / (lamF / 1000.0));
                nc = Math.Max(8, Math.Min(16, nc - nc % 2));
                int tagF = -1;
                for (int k = 0; k < nc; k++)
                {
                    double a1 = -Math.PI / 2 - Math.PI / nc + 2 * Math.PI * k / nc, a2 = a1 + 2 * Math.PI / nc;
                    // maglia uniforme, sorgente su un lato normale (tamago, docs/segmentation.md; approvato il 13/11/2026)
                    m.Geometry.Add(Gw(tag, 1, P3(rcc * Math.Cos(a1), vc + rcc * Math.Sin(a1), 0), P3(rcc * Math.Cos(a2), vc + rcc * Math.Sin(a2), 0), ac));
                    if (k == 0) tagF = tag;
                    if (conPerdite) m.Loads.Add(new LoadSpec { Type = 5, Tag = tag, From = 0, To = 0, F1 = 5.8e7 });
                    tag++;
                }
                if (conSorgente) m.Sources.Add(new SourceSpec { Tag = tagF, Seg = 1 });
            }
            else
            {
                // gamma: asta concentrica all'interno, collegata a un vertice del loop in basso; sorgente nel collegamento al vertice
                // all'altro capo (i fili si collegano solo agli estremi dei segmenti, cioe' ai vertici del poligono)
                double sp = Math.Max(0.05 * 2 * rc, 4 * a), rg = rc - sp, ag = Math.Max(0.001, a / 2);
                double passoAng = 2 * Math.PI / lati;
                int nv = Math.Max(1, Math.Min(lati / 2 - 1, (int)Math.Round(dk / Math.Max(rg, 1e-3) / passoAng)));
                Func<double, double, double[]> Q = (r, an) => P3(r * Math.Cos(an), r * Math.Sin(an), 0);
                double a0 = -Math.PI / 2 - Math.PI / lati;     // vertice 0 (inizio del lato inferiore)
                m.Geometry.Add(Gw(tag++, 1, Q(rc, a0), Q(rg, a0), ag));
                for (int k = 0; k < nv; k++) m.Geometry.Add(Gw(tag++, 1, Q(rg, a0 + passoAng * k), Q(rg, a0 + passoAng * (k + 1)), ag));
                int tagS = tag;
                m.Geometry.Add(Gw(tag++, 1, Q(rg, a0 + passoAng * nv), Q(rc, a0 + passoAng * nv), ag));
                if (conSorgente) m.Sources.Add(new SourceSpec { Tag = tagS, Seg = 1 });
            }
            m.FreqMHz = s.FreqMHz;
            List<string> err = m.Reevaluate();
            if (err.Count > 0) throw new NecException(err[0]);
            return m;
        }

        // formule del calcolatore Loop magnetica (W5QJR, AA5TB): area e perimetro della spira, equivalente circolare per L
        static void LoopFormule(LoopSpec s, LoopEsito r)
        {
            double f = s.FreqMHz, w = 2 * Math.PI * f * 1e6;
            double perim, area, rc;
            Dimensioni(s, out perim, out area, out rc);
            double lam = 299.792458 / f, n = Math.Max(1, s.Spire), dCond = 2 * Raggio(s);
            r.Rrf = 31171 * Math.Pow(n * area / (lam * lam), 2);
            double Rs = Math.Sqrt(Math.PI * f * 1e6 * Mu0 / Sigma(s));
            r.Rpf = Rs * perim * n / (Math.PI * dCond);
            double Deq = 2 * Math.Sqrt(area / Math.PI);
            r.Lf = n * n * Mu0 * (Deq / 2) * (Math.Log(8 * Deq / dCond) - 2);
            r.Cf = 1 / (w * w * r.Lf);
            r.Efff = 100 * r.Rrf / (r.Rrf + r.Rpf);
            r.Qf = w * r.Lf / (r.Rrf + r.Rpf);
            if (s.Spire > 1) r.Nota = "Piu' spire: le formule valgono per spire vicine (L con n^2); il NEC tiene conto del passo.";
            if (s.Fascia) r.Nota += (r.Nota.Length > 0 ? " " : "") + "Fascia: raggio equivalente = larghezza/4 (Butler, 1982); perdita con la circonferenza equivalente.";
        }

        static GeomCard Gw(int tag, int ns, double[] p, double[] q, double r) { return Gw(tag, ns, p[0], p[1], p[2], q[0], q[1], q[2], r); }

        static GeomCard Gw(int tag, int ns, double x1, double y1, double z1, double x2, double y2, double z2, double r)
        {
            return new GeomCard
            {
                Id = "GW", I1 = tag, I2 = ns, F1 = x1, F2 = y1, F3 = z1, F4 = x2, F5 = y2, F6 = z2, F7 = r,
                Src = new[] { tag.ToString(CI), ns.ToString(CI), N(x1), N(y1), N(z1), N(x2), N(y2), N(z2), N(r) }
            };
        }

        static Complex Z(NecModel m, double f)
        {
            SegmentGeometry g = m.BuildGeometry();
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            return e.Sources[0].Z;
        }

        /// <summary>Calcolo dell'esito (NEC e formule). Il modello finale e' restituito in "finale".</summary>
        public static LoopEsito Esito(LoopSpec s, CancellationToken ct, out NecModel finale)
        {
            LoopEsito r = new LoopEsito();
            double f = s.FreqMHz, w = 2 * Math.PI * f * 1e6;
            int auto, max;
            Segmentazione(s, out auto, out max);
            r.Segmenti = s.Segmenti > 0 ? s.Segmenti : auto; r.MaxSegmenti = max;
            int tc, sc;
            // loop alimentato direttamente in basso, senza condensatore e senza perdite: L
            LoopSpec d = s.Clone(); d.Alimentazione = 2;
            Complex z0 = Z(Costruisci(d, null, false, 0, true, out tc, out sc), f);
            ct.ThrowIfCancellationRequested();
            r.L = z0.Imaginary / w;
            double cAcc = s.CondensatorePf > 0 ? s.CondensatorePf * 1e-12 : (z0.Imaginary > 0 ? 1 / (w * z0.Imaginary) : 0);
            r.C = cAcc;
            if (cAcc > 0)
            {
                Complex zr = Z(Costruisci(d, null, false, cAcc, true, out tc, out sc), f);
                ct.ThrowIfCancellationRequested();
                Complex zl = Z(Costruisci(d, null, true, cAcc, true, out tc, out sc), f);
                ct.ThrowIfCancellationRequested();
                r.Rr = zr.Real; r.Rp = zl.Real - zr.Real;
                // prova di convergenza (tamago; approvata il 13/11/2026): stesso loop con il doppio dei segmenti
                LoopSpec d2 = d.Clone(); d2.Segmenti = 2 * r.Segmenti;
                Complex z02 = Z(Costruisci(d2, null, false, 0, true, out tc, out sc), f);
                ct.ThrowIfCancellationRequested();
                double c2 = z02.Imaginary > 0 ? 1 / (w * z02.Imaginary) : 0;
                if (c2 > 0)
                {
                    Complex zr2 = Z(Costruisci(d2, null, false, c2, true, out tc, out sc), f);
                    ct.ThrowIfCancellationRequested();
                    r.ScartoL = Math.Abs(z02.Imaginary - z0.Imaginary) / Math.Abs(z0.Imaginary) * 100;
                    r.ScartoRr = Math.Abs(zr2.Real - zr.Real) / Math.Max(Math.Abs(zr.Real), 1e-12) * 100;
                }
                r.Eff = zl.Real > 0 ? 100 * r.Rr / zl.Real : 0;
                r.Q = zl.Real > 0 ? z0.Imaginary / zl.Real : 0;
                r.Banda = r.Q > 0 ? f * 1e6 / r.Q : 0;
            }
            double perimL = 0, areaL, rcL;
            Dimensioni(s, out perimL, out areaL, out rcL);
            r.PerimetroLambda = perimL * Math.Max(1, s.Spire) / (299.792458 / f);
            if (s.Alimentazione == 3)
            {
                // anello passivo: si scrive senza sorgente; l'esito NEC vale per l'anello da solo, alimentato solo per misurarlo
                finale = Costruisci(s, null, true, 0, false, out tc, out sc);
                r.Zfinale = new Complex(double.NaN, double.NaN); r.Ros = double.NaN;
                LoopFormule(s, r);
                r.Nota = (r.Nota.Length > 0 ? r.Nota + " " : "") + string.Format(CultureInfo.CurrentCulture,
                    "Anello passivo: perimetro {0:0.000} lambda, nessuna sorgente e nessun condensatore; l'effetto si vede nel modello completo (direttore o riflettore).", r.PerimetroLambda);
                return r;
            }
            // modello finale con l'alimentazione scelta; con Faraday o gamma il condensatore si ritocca per il ROS minimo
            NecModel fin = Costruisci(s, null, true, s.Condensatore ? cAcc : 0, true, out tc, out sc);
            if (s.Condensatore && s.Alimentazione != 2 && cAcc > 0 && s.CondensatorePf <= 0)
            {
                double z0r = 50, best = double.MaxValue, cBest = cAcc;
                double lo = cAcc * 0.995, hi = cAcc * 1.005;
                // la risonanza e' larga circa 1/Q (0,03 % su C): primo giro fitto (0,006 %), poi due rifiniture
                for (int giro = 0; giro < 3; giro++)
                {
                    int np = giro == 0 ? 160 : 40;
                    for (int k = 0; k <= np; k++)
                    {
                        double c = lo + (hi - lo) * k / np;
                        Complex z = Z(Costruisci(s, null, true, c, true, out tc, out sc), f);
                        double g = Complex.Abs((z - z0r) / (z + z0r));
                        if (g < best) { best = g; cBest = c; }
                        ct.ThrowIfCancellationRequested();
                    }
                    double pas = (hi - lo) / np;
                    lo = cBest - pas; hi = cBest + pas;
                }
                r.C = cBest;
                fin = Costruisci(s, null, true, cBest, true, out tc, out sc);
            }
            RunResult rr = Analysis.Run(fin, fin.BuildGeometry(), f, 5.0, ct);
            r.Zfinale = rr.Zin; r.Ros = Analysis.Swr(rr.Zin, 50); r.Gdbi = rr.GmaxDbi;
            if (s.Alimentazione != 2) r.Eff = rr.Efficiency;
            finale = fin;
            LoopFormule(s, r);
            return r;
        }

    }
}
