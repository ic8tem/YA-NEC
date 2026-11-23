// YA-NEC 1.2.0 - Oggetti inseriti > Cavo e balun (bozzetto VHF 10 rev. 2, comprende il bozzetto HF 5): la calza del cavo diventa un filo NEC
// attaccato a un lato del punto di alimentazione (com'e' nella realta' senza balun), lungo il boom (5 cm sotto), giu' per il palo e fino
// alla stazione; il balun e' un'impedenza di modo comune (scheda LD 4, R + jX alla frequenza) sul primo segmento della calza.
// Modelli di modo comune (approssimati, fumetti gialli): choke a spire di coax (L di Wheeler, C di Medhurst, Q 50), toroide 1:1 in ferrite
// (RLC parallelo a Q basso: mix 31 a 10 MHz, mix 43 a 25 MHz), lambda/2 coassiale 4:1, manicotto e Pawsey stub (linea lambda/4 in corto
// con perdite), ferriti a perline (mix 61 in VHF, 43 in HF).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace YaNec.Engine
{
    public static class CavoBalun
    {
        public class Tipo { public string Chiave, Nome; public bool Hf, Vhf; }
        public static readonly Tipo[] Tipi =
        {
            new Tipo { Chiave = "nessuno", Nome = "Nessuno", Hf = true, Vhf = true },
            new Tipo { Chiave = "choke", Nome = "Choke a spire di coax (HF)", Hf = true },
            new Tipo { Chiave = "toroide", Nome = "Toroide 1:1, mix 31/43 (HF)", Hf = true },
            new Tipo { Chiave = "mezzaonda", Nome = "λ/2 coassiale 4:1 (VHF/UHF)", Vhf = true },
            new Tipo { Chiave = "manicotto", Nome = "Manicotto λ/4 (VHF/UHF)", Vhf = true },
            new Tipo { Chiave = "ferriti", Nome = "Ferriti a perline", Hf = true, Vhf = true },
            new Tipo { Chiave = "pawsey", Nome = "Pawsey stub", Hf = true, Vhf = true },
        };

        /// <summary>Impedenza di modo comune del balun alla frequenza f (progettato per f0).</summary>
        public static Complex Zcm(string k, double f, double f0)
        {
            double w = 2 * Math.PI * f * 1e6;
            Func<double, double, double, Complex> parRlc = (r, fr, q) =>
            {
                double wr = 2 * Math.PI * fr * 1e6, l = r / (q * wr), c = 1 / (wr * wr * l);
                Complex y = 1 / r + 1 / new Complex(0, w * l) + new Complex(0, w * c);
                return 1 / y;
            };
            switch (k)
            {
                case "choke":
                    {
                        // 8 spire di RG-213 su 110 mm: L di Wheeler, capacita' propria di Medhurst, Q 50
                        double l = CarichiHf.Wheeler(8, 0.110, 0.085), cs = CarichiHf.CsMedhurst(0.110, 0.085);
                        double fr = 1 / (2 * Math.PI * Math.Sqrt(l * cs)) / 1e6;
                        return parRlc(50 * w * l * Math.Min(1, fr / f), fr, 50);
                    }
                case "toroide": return parRlc(5000, f < 15 ? 10 : 25, 0.6);
                case "mezzaonda": return parRlc(600, f0, 3);
                case "manicotto":
                case "pawsey":
                    {
                        double zs = k == "manicotto" ? 50 : 100, len = 299.792458 / f0 / 4;
                        Complex g = LineCalc.Gamma(f, 1, 0.08 * Math.Sqrt(f / 144.0) * 100 / Math.Max(0.05, len)) * len;   // circa 0,08 dB sul quarto d'onda
                        return zs * Complex.Tanh(g);
                    }
                case "ferriti":
                    {
                        // 10 perline: mix 61 sopra 50 MHz (piu' resistive verso 300 MHz), mix 43 sotto
                        double r1 = f >= 50 ? 40 * Math.Min(2, Math.Sqrt(f / 144.0)) : 100 * Math.Min(1, f / 30.0);
                        return new Complex(10 * r1, 10 * r1 * 0.3);
                    }
                default: return Complex.Zero;
            }
        }

        public class Percorso
        {
            public double LungoBoom = 0.5, DiscesaPalo = 3, FinoStazione = 0, RaggioCalza = 0.005, RaggioPalo = 0.025;
            /// <summary>1.6.0 (proposta 5): quota del tratto verso la stazione (0 = alla fine della discesa; con la quota la discesa si ferma li')
            /// e direzione del tratto rispetto al boom, in gradi attorno alla verticale (0 = lungo il boom, 90 = sotto un braccio).</summary>
            public double QuotaOrizzontale, Direzione;
            public bool PaloPresente = true, CalzaAMassa = true, TerraStazione = true;
        }

        public class Esito
        {
            public string Chiave = "", Nome = "";
            public double CorrPerc, Fb, FrMedio, G, Ros, PerditaDb, WattA1kW, BandaDa = double.NaN, BandaA = double.NaN;
            public Complex Z;
            public double[] Dist, Corr;   // corrente lungo la calza (% della corrente al punto di alimentazione)
            /// <summary>1.6.0: corrente sulla calza all'ingresso della stazione (% del punto di alimentazione), potenza irradiata dalla sola
            /// calza (% della potenza irradiata in tutto: integrale del campo delle correnti della calza sulla sfera, o sulla semisfera con il
            /// terreno) e ROS cambiato rispetto al modello senza cavo.</summary>
            public double CorrStazione = double.NaN, PotCalzaPerc = double.NaN, DeltaRos = double.NaN;
        }

        /// <summary>Modello con la calza (e il palo): restituisce il modello, il tag del primo filo della calza e i tag della calza in ordine.</summary>
        public static NecModel ConCalza(NecModel m0, SegmentGeometry g, double f, Percorso p, string balun, double f0, out List<int> tagsCalza)
        {
            int ts;
            return ConCalza(m0, g, f, p, balun, f0, out tagsCalza, out ts);
        }

        /// <summary>tagStazione: tag del tratto che arriva alla stazione (o dell'ultimo tratto della calza se non c'e').</summary>
        public static NecModel ConCalza(NecModel m0, SegmentGeometry g, double f, Percorso p, string balun, double f0, out List<int> tagsCalza, out int tagStazione)
        {
            tagStazione = -1;
            NecModel m = m0.Clone();
            tagsCalza = new List<int>();
            SourceSpec s = m.Sources[0];
            int iSeg = -1, cnt = 0;
            for (int i = 0; i < g.N; i++) if (g.Tag[i] == s.Tag && ++cnt == s.Seg) { iSeg = i; break; }
            if (iSeg < 0) throw new NecException("Cavo e balun: segmento della sorgente non trovato.");
            double lam = 299.792458 / f;
            double[] a = { g.X2[iSeg], g.Y2[iSeg], g.Z2[iSeg] };          // la calza va al lato "fine" del segmento alimentato
            double[] dirEl = Unit(new[] { g.X2[iSeg] - g.X1[iSeg], g.Y2[iSeg] - g.Y1[iSeg], g.Z2[iSeg] - g.Z1[iSeg] });
            // asse del boom: orizzontale e perpendicolare all'elemento (verso il palo: indietro, dal radiatore al riflettore se piu' lungo)
            int asse; var el = Tolleranze.Elementi(m0, g, out asse);
            double[] boom = { 0, 0, 0 }; boom[asse] = 1;
            if (Math.Abs(Dot(boom, dirEl)) > 0.9) boom = Unit(Cross(dirEl, new double[] { 0, 0, 1 }));
            // verso il palo: il centro dei centri degli elementi
            double[] cg = TrasformaGeometria.Centro(g);
            double[] verso = Sub(cg, a); double proj = Dot(verso, boom);
            double[] dirBoom = proj >= 0 ? boom : Mul(boom, -1);
            bool vert = Math.Abs(dirEl[2]) > 0.7;   // dipolo verticale: la calza esce orizzontale e scende dopo
            double[] giu = { 0, 0, -1 };
            int tag = m.Geometry.Where(q => q.I1 > 0).Select(q => q.I1).DefaultIfEmpty(0).Max();
            Func<double, int> ns = len => Math.Max(2, (int)Math.Ceiling(len / (lam / 20)));
            double rc = p.RaggioCalza;
            List<double[]> punti = new List<double[]> { a };
            double[] b1 = Add(a, vert ? dirBoom : giu, 0.05);                 // 5 cm sotto (o di lato) per non toccare gli elementi
            punti.Add(b1);
            double[] b2 = Add(b1, dirBoom, Math.Max(0.02, p.LungoBoom));
            punti.Add(b2);
            double[] b3 = Add(b2, giu, Math.Max(0.1, p.DiscesaPalo));
            if (p.QuotaOrizzontale > 0) b3[2] = Math.Min(b2[2] - 0.1, p.QuotaOrizzontale);   // 1.6.0: la discesa si ferma alla quota del tratto
            if (m0.Ground.Kind != GroundKind.FreeSpace) b3[2] = Math.Max(0.1, b3[2]);
            punti.Add(b3);
            int kStazione = -1;
            if (p.FinoStazione > 0.05)
            {
                // 1.6.0: il tratto verso la stazione gira attorno alla verticale di Direzione gradi rispetto al boom
                double a = p.Direzione * Math.PI / 180;
                double[] h = Unit(new[] { dirBoom[0], dirBoom[1], 0 });
                if (Norm(h) < 0.5) h = new double[] { 1, 0, 0 };
                double[] dirSt = { h[0] * Math.Cos(a) - h[1] * Math.Sin(a), h[0] * Math.Sin(a) + h[1] * Math.Cos(a), 0 };
                double[] b4 = Add(b3, dirSt, p.FinoStazione);
                punti.Add(b4);
                kStazione = punti.Count - 2;
                if (p.TerraStazione && m0.Ground.Kind != GroundKind.FreeSpace) punti.Add(new[] { b4[0], b4[1], 0.0 });
            }
            for (int k = 0; k + 1 < punti.Count; k++)
            {
                double len = Norm(Sub(punti[k + 1], punti[k]));
                if (len < 1e-4) continue;
                m.Geometry.Add(new GeomCard { Id = "GW", I1 = ++tag, I2 = k == 0 ? 1 : ns(len), F1 = punti[k][0], F2 = punti[k][1], F3 = punti[k][2], F4 = punti[k + 1][0], F5 = punti[k + 1][1], F6 = punti[k + 1][2], F7 = rc });
                tagsCalza.Add(tag);
                if (k == kStazione || (kStazione < 0 && k == punti.Count - 2)) tagStazione = tag;
            }
            // palo: dal boom (2 cm sotto) a terra o per la discesa, 5 cm dietro la calza; collegato alla calza in basso se richiesto
            if (p.PaloPresente)
            {
                double[] t0 = Add(Add(b2, dirBoom, 0.05), giu, -0.03);
                double[] t1 = Add(Add(b3, dirBoom, 0.05), giu, 0);
                if (m0.Ground.Kind != GroundKind.FreeSpace) t1[2] = 0;
                double len = Norm(Sub(t1, t0));
                m.Geometry.Add(new GeomCard { Id = "GW", I1 = ++tag, I2 = ns(len), F1 = t0[0], F2 = t0[1], F3 = t0[2], F4 = t1[0], F5 = t1[1], F6 = t1[2], F7 = p.RaggioPalo });
                if (p.CalzaAMassa)
                {
                    double[] q0 = b3, q1 = Add(b3, dirBoom, 0.05);
                    if (m0.Ground.Kind != GroundKind.FreeSpace) q1[2] = Math.Max(q1[2], 0.1);
                    // il ponticello arriva al palo all'altezza dell'estremo della calza
                    m.Geometry.Add(new GeomCard { Id = "GW", I1 = ++tag, I2 = 1, F1 = q0[0], F2 = q0[1], F3 = q0[2], F4 = q1[0], F5 = q1[1], F6 = q1[2], F7 = rc });
                }
            }
            if (balun != "nessuno" && tagsCalza.Count > 0)
            {
                Complex z = Zcm(balun, f, f0);
                m.Loads.Add(new LoadSpec { Type = 4, Tag = tagsCalza[0], From = 1, To = 1, F1 = z.Real, F2 = z.Imaginary });
            }
            List<string> err = m.Reevaluate();
            if (err.Count > 0) throw new NecException(err[0]);
            return m;
        }

        public static Esito Valuta(NecModel m0, SegmentGeometry g, double f, double f0, Percorso p, string balun, double th, double ph, CancellationToken ct)
        {
            List<int> tags; int tagSt;
            NecModel m = ConCalza(m0, g, f, p, balun, f0, out tags, out tagSt);
            SegmentGeometry gg = m.BuildGeometry();
            RunResult r = Analysis.Run(m, gg, f, 5, ct);
            Esito e = new Esito { Chiave = balun, Nome = Tipi.First(t => t.Chiave == balun).Nome, Z = r.Zin, Ros = r.Swr, G = r.Engine.Gain(th, ph).GainTotal, Fb = r.FrontBack, FrMedio = r.FrontRearMedio };
            int iS = -1, c = 0;
            for (int i = 0; i < gg.N; i++) if (gg.Tag[i] == m.Sources[0].Tag && ++c == m.Sources[0].Seg) { iS = i; break; }
            double iFeed = Math.Max(1e-12, r.Engine.SegmentCurrent(iS).Magnitude);
            List<double> dist = new List<double>(), corr = new List<double>();
            double d = 0, mx = 0;
            foreach (int t in tags)
                for (int i = 0; i < gg.N; i++)
                {
                    if (gg.Tag[i] != t) continue;
                    double len = gg.Length(i);
                    double pc = r.Engine.SegmentCurrent(i).Magnitude / iFeed * 100;
                    dist.Add(d + len / 2); corr.Add(pc); d += len; mx = Math.Max(mx, pc);
                }
            e.Dist = dist.ToArray(); e.Corr = corr.ToArray(); e.CorrPerc = mx;
            // 1.6.0: corrente all'ingresso della stazione (ultimo segmento del tratto verso la stazione)
            for (int i = gg.N - 1; i >= 0; i--)
                if (gg.Tag[i] == tagSt) { e.CorrStazione = r.Engine.SegmentCurrent(i).Magnitude / iFeed * 100; break; }
            // 1.6.0: potenza irradiata dalla sola calza rispetto a tutta la potenza irradiata
            try
            {
                bool[] mask = new bool[gg.N];
                HashSet<int> ht = new HashSet<int>(tags);
                for (int i = 0; i < gg.N; i++) mask[i] = ht.Contains(gg.Tag[i]);
                e.PotCalzaPerc = 100 * r.Engine.FrazioneIrradiata(mask);
            }
            catch (Exception) { }
            if (balun != "nessuno")
            {
                int i0 = -1; for (int i = 0; i < gg.N; i++) if (gg.Tag[i] == tags[0]) { i0 = i; break; }
                Complex z = Zcm(balun, f, f0); Complex ib = r.Engine.SegmentCurrent(i0);
                double pb = 0.5 * ib.Magnitude * ib.Magnitude * z.Real;
                double pin = Math.Max(1e-30, r.InputPower);
                e.WattA1kW = 1000 * pb / pin;
                e.PerditaDb = -10 * Math.Log10(Math.Max(1e-12, 1 - pb / pin));
            }
            return e;
        }

        static double[] Add(double[] a, double[] b, double k) { return new[] { a[0] + b[0] * k, a[1] + b[1] * k, a[2] + b[2] * k }; }
        static double[] Sub(double[] a, double[] b) { return new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] }; }
        static double[] Mul(double[] a, double k) { return new[] { a[0] * k, a[1] * k, a[2] * k }; }
        static double Norm(double[] a) { return Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]); }
        static double[] Unit(double[] a) { double n = Norm(a); return n < 1e-12 ? new double[3] : new[] { a[0] / n, a[1] / n, a[2] / n }; }
        static double[] Cross(double[] a, double[] b) { return new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] }; }
        static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
    }
}
