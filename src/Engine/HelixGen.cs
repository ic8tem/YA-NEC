// YA-NEC 0.4.30 - elica (scheda "Elica" della barra Geometria), bozzetto 1 rivisto approvato da IC8TEM il 14/11/2026 22:40.
// Geometria come NECBOL di G1OJS (MIT): ogni spira e' un poligono di N fili diritti di un segmento, raggio che varia linearmente
// tra base e cima (taper_factor), passo costante; alimentazione con un filo corto dal riflettore all'inizio dell'elica, sul bordo.
// Formule di confronto: J. D. Kraus, Antennas (modo assiale): Z = 140 C/lambda, D = 15 (C/lambda)^2 N S/lambda, rapporto assiale
// (2N + 1)/2N, apertura 52 / ((C/lambda) sqrt(N S/lambda)), banda 0,75-1,33 lambda di circonferenza; D. T. Emerson (NRAO, 1995):
// la formula di Kraus sovrastima il guadagno, G = 10,25 + 1,22 L - 0,0726 L^2 dBic con L = N S in lunghezze d'onda (3-8 lambda).
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace YaNec.Engine
{
    public class HelixSpec
    {
        public int Modo;                    // 0 assiale (Kraus), 1 normale (corta), 2 libera
        public double FreqMHz = 435;
        public int Spire = 10;
        public bool Destra = true;
        public double Circ = 1.0, Passo = 0.23;   // circonferenza e passo in lunghezze d'onda
        public double Rastremazione = 1.0;        // raggio in cima / raggio in basso
        public double Conduttore = 0.004;         // diametro (m)
        public int FiliPerSpira = 24;
        public int Riflettore = 1;                // 0 radiali, 1 perfetto (terreno, come l esito del bozzetto), 2 nessuno
        public double DiamRifl = 0.8;             // diametro del riflettore a radiali (lambda)
        public int Radiali = 16;
        public bool AsseX;
        public double Base;                       // quota della base (m)
        public double Gap = 0.02;                 // filo di alimentazione (m)
        public HelixSpec Clone() { return (HelixSpec)MemberwiseClone(); }
    }

    public class HelixEsito
    {
        public Complex Z, Zbassa, Zalta;
        public double GdbicNec, ArNec, ArDbNec, Swr;
        public bool DestraNec;
        public double Zkraus, GdbicKraus, GdbicEmerson, ArKraus, HpbwKraus, Fbassa, Falta;
        public string Nota = "";
    }

    public static class HelixGen
    {
        const double C0 = 299.792458;

        /// <summary>Valori di partenza del modo: assiale C = 1 lambda, passo 0,23 lambda (12-14 gradi); normale C = 0,1, passo 0,03.</summary>
        public static void Modo(HelixSpec s, int modo)
        {
            s.Modo = modo;
            if (modo == 0) { s.Circ = 1.0; s.Passo = 0.23; if (s.Riflettore == 2) s.Riflettore = 0; }
            else if (modo == 1) { s.Circ = 0.1; s.Passo = 0.03; s.Riflettore = 1; s.FiliPerSpira = Math.Min(s.FiliPerSpira, 12); }
        }

        public static NecModel Costruisci(HelixSpec s, NecModel dove, out int tagSorg, out int segSorg)
        {
            double lam = C0 / s.FreqMHz, R0 = s.Circ * lam / (2 * Math.PI), S = s.Passo * lam, a = s.Conduttore / 2;
            int nf = Math.Max(6, s.FiliPerSpira), ns = Math.Max(1, s.Spire);
            bool perfetto = s.Riflettore == 1, radiali = s.Riflettore == 0;
            double z0 = perfetto ? 0 : s.Base, gap = s.Riflettore == 2 ? 0 : s.Gap;
            NecModel m;
            if (dove != null) m = dove.Clone();
            else { m = NecModel.Parse("CM elica\nCE\nGW 1 1 0 0 0 0 0 1 0.001\nGE 0\nFR 0 1 0 0 " + s.FreqMHz.ToString(System.Globalization.CultureInfo.InvariantCulture) + " 0\nEN"); m.Geometry.Clear(); }
            int tag = 1;
            foreach (GeomCard g in m.Geometry) tag = Math.Max(tag, g.I1 + 1);
            // con l'asse X l'elica giace lungo x a quota "base" (z) e il riflettore e' verticale
            Func<double, double, double, double[]> P = (x, y, z) => s.AsseX ? new[] { z - z0, y, x + s.Base } : new[] { x, y, z };
            double sgn = s.Destra ? 1 : -1;
            // riflettore
            if (radiali)
            {
                double rr = s.DiamRifl * lam / 2;
                int nr = Math.Max(4, s.Radiali);
                for (int k = 0; k < nr; k++)
                {
                    double an = 2 * Math.PI * k / nr, cx = Math.Cos(an), cy = Math.Sin(an);
                    if (k == 0 && R0 < rr)
                    {
                        // il radiale lungo +x passa per il punto di alimentazione: due fili
                        int n1 = Math.Max(1, (int)Math.Ceiling(R0 / (lam / 20))), n2 = Math.Max(1, (int)Math.Ceiling((rr - R0) / (lam / 20)));
                        m.Geometry.Add(Gw(tag++, n1, P(0, 0, z0), P(R0, 0, z0), a));
                        m.Geometry.Add(Gw(tag++, n2, P(R0, 0, z0), P(rr, 0, z0), a));
                    }
                    else m.Geometry.Add(Gw(tag++, Math.Max(1, (int)Math.Ceiling(rr / (lam / 20))), P(0, 0, z0), P(rr * cx, rr * cy, z0), a));
                }
            }
            // filo di alimentazione
            tagSorg = -1; segSorg = 1;
            if (gap > 0) { tagSorg = tag; m.Geometry.Add(Gw(tag++, 1, P(R0, 0, z0), P(R0, 0, z0 + gap), a)); }
            // elica: nf fili per spira
            int tot = nf * ns;
            double zb = z0 + gap;
            double[] prec = P(R0, 0, zb);
            for (int k = 1; k <= tot; k++)
            {
                double t = (double)k / tot, an = sgn * 2 * Math.PI * k / nf, r = R0 * (1 + (s.Rastremazione - 1) * t);
                double[] p = P(r * Math.Cos(an), r * Math.Sin(an), zb + S * k / nf);
                if (tagSorg < 0) tagSorg = tag;
                m.Geometry.Add(Gw(tag++, 1, prec, p, a));
                prec = p;
            }
            m.Sources.Add(new SourceSpec { Tag = tagSorg, Seg = 1 });
            if (dove == null)
            {
                m.FreqMHz = s.FreqMHz;
                m.Ground = perfetto && !s.AsseX ? new GroundSpec { Kind = GroundKind.Perfect } : new GroundSpec { Kind = GroundKind.FreeSpace };
                m.GroundFlag = perfetto && !s.AsseX ? 1 : 0;
            }
            List<string> err = m.Reevaluate();
            if (err.Count > 0) throw new NecException(err[0]);
            return m;
        }

        static GeomCard Gw(int tag, int ns, double[] a, double[] b, double r)
        {
            return new GeomCard { Id = "GW", I1 = tag, I2 = ns, F1 = a[0], F2 = a[1], F3 = a[2], F4 = b[0], F5 = b[1], F6 = b[2], F7 = r };
        }

        /// <summary>Guadagni circolari (dBic) e rapporto assiale lungo l'asse dell'elica (NEC), con il verso della polarizzazione.</summary>
        static void Circolare(Nec2Engine e, bool asseX, out double gR, out double gL)
        {
            double th = asseX ? Math.PI / 2 : 0, ph = 0;
            Complex eth, eph;
            e.Ffld(th, ph, out eth, out eph);
            double gc = e.GainConstant;
            // vista lungo la direzione di propagazione: E_R = (Eth + j Eph)/sqrt2, E_L = (Eth - j Eph)/sqrt2 (convenzione e^{jwt})
            Complex er = (eth + Complex.ImaginaryOne * eph) / Math.Sqrt(2), el = (eth - Complex.ImaginaryOne * eph) / Math.Sqrt(2);
            gR = 10 * Math.Log10(Math.Max(1e-30, gc * er.Magnitude * er.Magnitude));
            gL = 10 * Math.Log10(Math.Max(1e-30, gc * el.Magnitude * el.Magnitude));
        }

        public static HelixEsito Esito(HelixSpec s, CancellationToken ct, out NecModel modello)
        {
            HelixEsito r = new HelixEsito();
            int ts, ss;
            NecModel mod = Costruisci(s, null, out ts, out ss);   // 0.4.31: copia locale, un parametro out non si usa nella lambda (CS1628)
            modello = mod;
            double lam = C0 / s.FreqMHz, N = Math.Max(1, s.Spire), Sl = s.Passo, Cl = s.Circ;
            r.Zkraus = 140 * Cl;
            r.GdbicKraus = 10 * Math.Log10(15 * Cl * Cl * N * Sl);
            double L = N * Sl;
            r.GdbicEmerson = 10.25 + 1.22 * L - 0.0726 * L * L;
            r.ArKraus = (2 * N + 1) / (2 * N);
            r.HpbwKraus = 52 / (Cl * Math.Sqrt(N * Sl));
            r.Fbassa = s.FreqMHz * 0.75 / Cl; r.Falta = s.FreqMHz * 1.33 / Cl;
            Func<double, Nec2Engine> calc = f =>
            {
                ct.ThrowIfCancellationRequested();
                SegmentGeometry g = mod.BuildGeometry();
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(f, Analysis.EffectiveSources(mod, g, f), mod.EffectiveLoads(), mod.Ground, mod.ExtendedKernel, mod.Rkh, mod.EffectiveNetworks(f));
                return e;
            };
            Nec2Engine e0 = calc(s.FreqMHz);
            r.Z = e0.Sources[0].Z;
            r.Swr = Analysis.Swr(r.Z, 50);
            double gR, gL;
            Circolare(e0, s.AsseX, out gR, out gL);
            r.DestraNec = gR >= gL;
            r.GdbicNec = Math.Max(gR, gL);
            double aR = Math.Pow(10, gR / 20), aL = Math.Pow(10, gL / 20);
            r.ArNec = Math.Abs(aR - aL) > 1e-12 ? (aR + aL) / Math.Abs(aR - aL) : 999;
            r.ArDbNec = 20 * Math.Log10(Math.Max(1, r.ArNec));
            r.Zbassa = calc(s.FreqMHz * 0.954).Sources[0].Z;
            r.Zalta = calc(s.FreqMHz * 1.046).Sources[0].Z;
            if (s.Modo == 0 && Math.Abs(r.GdbicKraus - r.GdbicNec) > 1.5) r.Nota = "La formula di Kraus sovrastima il guadagno (Emerson, NRAO 1995): vale il NEC.";
            if (s.Destra != r.DestraNec) r.Nota += (r.Nota.Length > 0 ? " " : "") + "Il NEC trova la polarizzazione opposta al verso scelto: controllare il senso di avvolgimento.";
            if (s.Modo == 0 && (Cl < 0.75 || Cl > 1.33)) r.Nota += (r.Nota.Length > 0 ? " " : "") + "Circonferenza fuori da 0,75-1,33 lambda: non e' piu' il modo assiale di Kraus.";
            return r;
        }
    }
}
