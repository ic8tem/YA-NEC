// YA-NEC 1.2.0 - telaio a H, palo e cavi di fase nello stack (bozzetto VHF 4). Montanti, traversa, palo e calze dei cavi di fase diventano
// fili NEC aggiunti a una copia del modello; il confronto senza / con telaio usa lo stesso calcolo (guadagno, F/B, F/R medio, lobo
// laterale peggiore nel piano E, Z all'ingresso del divisore, Tant e G/T come la linguetta Rumore/GT).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace YaNec.Engine
{
    public static class TelaioStack
    {
        public static readonly string[] Tipi = { "H (2 montanti)", "croce", "solo palo", "traliccio" };
        public static readonly string[] Materiali = { "Acciaio zincato", "Alluminio", "Vetroresina" };
        public static readonly double[] Sigma = { 1.7e7, 3.5e7, 0 };   // zinco in superficie; alluminio; la vetroresina non entra nel NEC

        public class Opzioni
        {
            public int Tipo, Materiale;
            public bool Palo = true, Cavi = true;
            public double DMontante = 0.040, DTraversa = 0.050, DPalo = 0.060, DCalza = 0.010, LPalo = 3.0, Sporgenza = 0.3;
        }

        public class Pezzo { public string Nome = ""; public double DiamMm, LungM; public string Collegato = ""; public int Quanti = 1; }

        public class Esito
        {
            public NecModel Modello;
            public List<Pezzo> Pezzi = new List<Pezzo>();
            public List<double[][]> Disegno = new List<double[][]>();
            public List<double[]> Fissaggi = new List<double[]>(); public double LElemento = 1;   // segmenti nel piano frontale (u orizzontale, z), con tipo in [2][0]
        }

        static double[] Add(double[] a, double[] b, double k) { return new[] { a[0] + b[0] * k, a[1] + b[1] * k, a[2] + b[2] * k }; }

        public static Esito Costruisci(NecModel m0, SegmentGeometry g, StackInfo si, double f, Opzioni o)
        {
            Esito r = new Esito { Modello = m0.Clone() };
            NecModel m = r.Modello;
            double lam = 299.792458 / f;
            Func<double, int> ns = len => Math.Max(1, (int)Math.Ceiling(len / (lam / 20)));
            int tag = m.Geometry.Where(q => q.I1 > 0).Select(q => q.I1).DefaultIfEmpty(0).Max();
            List<int> nuovi = new List<int>();
            double[] u = { si.HorizDirX, si.HorizDirY, 0 };   // direzione delle colonne
            Action<double[], double[], double, int> filo = (a, b, rad, tipo) =>
            {
                double len = Math.Sqrt(Math.Pow(b[0] - a[0], 2) + Math.Pow(b[1] - a[1], 2) + Math.Pow(b[2] - a[2], 2));
                if (len < 1e-3) return;
                if (o.Materiale != 2 || tipo == 3)
                {
                    m.Geometry.Add(new GeomCard { Id = "GW", I1 = ++tag, I2 = ns(len), F1 = a[0], F2 = a[1], F3 = a[2], F4 = b[0], F5 = b[1], F6 = b[2], F7 = rad });
                    if (tipo != 3) nuovi.Add(tag);
                }
                r.Disegno.Add(new[] { a, b, new double[] { tipo } });
            };
            // punto di fissaggio di ogni antenna: centro degli elementi della copia, spostato a meta' tra due elementi vicini
            int asse; List<Tolleranze.Elemento> el = Tolleranze.Elementi(m0, g, out asse);
            int nBase = el.Count / Math.Max(1, si.Copies);
            List<double[]> fissaggi = new List<double[]>();
            List<double[]> feed = new List<double[]>();
            // geometria della copia 0: centro dei segmenti dei tag della prima copia
            HashSet<int> tag0 = new HashSet<int>();
            for (int k = 0; k < si.WiresPerCopy && k < si.CardIndex.Count; k++) tag0.Add(m0.Geometry[si.CardIndex[k]].I1);
            List<double[]> centri0 = new List<double[]>();
            foreach (int t in tag0)
            {
                double cx = 0, cy = 0, cz = 0; int n = 0;
                for (int i = 0; i < g.N; i++) if (g.Tag[i] == t) { cx += g.CenterX(i); cy += g.CenterY(i); cz += g.CenterZ(i); n++; }
                if (n > 0) centri0.Add(new[] { cx / n, cy / n, cz / n });
            }
            double[] c0 = { centri0.Average(c => c[0]), centri0.Average(c => c[1]), centri0.Average(c => c[2]) };
            List<double> pos = centri0.Select(c => c[asse]).OrderBy(v => v).ToList();
            double best = c0[asse], dist = double.MaxValue;
            for (int i = 0; i + 1 < pos.Count; i++) { double mid = (pos[i] + pos[i + 1]) / 2; if (Math.Abs(mid - c0[asse]) < dist) { dist = Math.Abs(mid - c0[asse]); best = mid; } }
            c0[asse] = best;
            // sorgente della copia 0
            SourceSpec s0 = m0.Sources.FirstOrDefault(q => tag0.Contains(q.Tag)) ?? m0.Sources[0];
            int is0 = -1, cnt = 0; for (int i = 0; i < g.N; i++) if (g.Tag[i] == s0.Tag && ++cnt == s0.Seg) { is0 = i; break; }
            double[] f0p = is0 >= 0 ? new[] { g.X2[is0], g.Y2[is0], g.Z2[is0] } : c0;
            for (int c = 0; c < si.Copies; c++)
            {
                double[] off = si.Offset[c];
                fissaggi.Add(new[] { c0[0] + off[0], c0[1] + off[1], c0[2] + off[2] });
                feed.Add(new[] { f0p[0] + off[0], f0p[1] + off[1], f0p[2] + off[2] });
            }
            r.Fissaggi = fissaggi; r.LElemento = el.Count > 0 ? el.Max(e => e.Lung) : 1;
            double zMin = fissaggi.Min(p => p[2]), zMax = fissaggi.Max(p => p[2]), zMid = (zMin + zMax) / 2;
            // colonne: punti di fissaggio con la stessa coordinata u
            Func<double[], double> U = p => p[0] * u[0] + p[1] * u[1];
            var colonne = fissaggi.GroupBy(p => Math.Round(U(p), 3)).OrderBy(gq => gq.Key).Select(gq => gq.First()).ToList();
            double[] centro = { colonne.Average(p => p[0]), colonne.Average(p => p[1]), zMid };
            double sp = o.Sporgenza;
            if (o.Tipo == 0)   // H: un montante per colonna e una traversa a meta' altezza
            {
                foreach (double[] p in colonne) filo(new[] { p[0], p[1], zMin - sp }, new[] { p[0], p[1], zMax + sp }, o.DMontante / 2, 0);
                double[] a = Add(new[] { colonne[0][0], colonne[0][1], zMid }, u, -0.1), b = Add(new[] { colonne[colonne.Count - 1][0], colonne[colonne.Count - 1][1], zMid }, u, 0.1);
                if (colonne.Count > 1) filo(a, b, o.DTraversa / 2, 1);
                r.Pezzi.Add(new Pezzo { Nome = "Montante sx/dx", DiamMm = o.DMontante * 1000, LungM = zMax - zMin + 2 * sp, Collegato = "al boom", Quanti = colonne.Count });
                if (colonne.Count > 1) r.Pezzi.Add(new Pezzo { Nome = "Traversa", DiamMm = o.DTraversa * 1000, LungM = Math.Abs(U(b) - U(a)), Collegato = "—" });
            }
            else if (o.Tipo == 1)   // croce: palo centrale fino alla riga in alto e una traversa per riga
            {
                filo(new[] { centro[0], centro[1], zMid }, new[] { centro[0], centro[1], zMax + sp }, o.DMontante / 2, 0);
                foreach (double z in fissaggi.Select(p => Math.Round(p[2], 3)).Distinct())
                {
                    double[] a = Add(new[] { colonne[0][0], colonne[0][1], z - 0.03 }, u, -0.1), b = Add(new[] { colonne[colonne.Count - 1][0], colonne[colonne.Count - 1][1], z - 0.03 }, u, 0.1);
                    filo(a, b, o.DTraversa / 2, 1);
                }
                r.Pezzi.Add(new Pezzo { Nome = "Palo centrale", DiamMm = o.DMontante * 1000, LungM = zMax + sp - zMid, Collegato = "al boom" });
                r.Pezzi.Add(new Pezzo { Nome = "Traverse (per riga)", DiamMm = o.DTraversa * 1000, LungM = Math.Abs(U(colonne[colonne.Count - 1]) - U(colonne[0])) + 0.2, Collegato = "—" });
            }
            // palo sotto il telaio (traliccio = palo con il raggio equivalente di un traliccio triangolare da 30 cm)
            double rPalo = o.Tipo == 3 ? 0.15 : o.DPalo / 2;
            if (o.Palo || o.Tipo >= 2)
            {
                double zb = zMid - o.LPalo;
                if (m0.Ground.Kind != GroundKind.FreeSpace) zb = Math.Max(0, zb);
                filo(new[] { centro[0], centro[1], o.Tipo >= 2 ? zMax + sp : zMid }, new[] { centro[0], centro[1], zb }, rPalo, 2);
                r.Pezzi.Add(new Pezzo { Nome = o.Tipo == 3 ? "Traliccio (Ø equivalente)" : "Palo", DiamMm = rPalo * 2000, LungM = (o.Tipo >= 2 ? zMax + sp : zMid) - zb, Collegato = "massa" });
            }
            // cavi di fase: la calza parte da un lato del punto di alimentazione, scende 5 cm, va al montante lungo il boom, segue il montante
            // e la traversa fino al divisore al centro (5 cm davanti ai tubi); le calze sono unite al divisore, fissato al palo
            if (o.Cavi)
            {
                double[] div = { centro[0], centro[1], zMid - 0.05 };
                double tot = 0;
                for (int c = 0; c < si.Copies; c++)
                {
                    double[] a = feed[c], b = new[] { a[0], a[1], a[2] - 0.05 };
                    double[] fx = fissaggi[c];
                    double[] d = { fx[0], fx[1], b[2] }; d[asse] = fx[asse] + 0.05;
                    double[] e = { d[0], d[1], zMid - 0.05 };
                    double[] h = { div[0], div[1], zMid - 0.05 }; h[asse] = d[asse];
                    filo(a, b, o.DCalza / 2, 3); filo(b, d, o.DCalza / 2, 3); filo(d, e, o.DCalza / 2, 3); filo(e, h, o.DCalza / 2, 3);
                    if (Math.Abs(h[asse] - div[asse]) > 1e-3 || Math.Abs(h[0] - div[0]) + Math.Abs(h[1] - div[1]) > 1e-3) filo(h, div, o.DCalza / 2, 3);
                    tot += Math.Abs(a[2] - b[2]) + Math.Abs(fx[asse] + 0.05 - a[asse]) + Math.Abs(b[2] - e[2]) + Math.Abs(U(e) - U(h));
                }
                r.Pezzi.Add(new Pezzo { Nome = "Cavi fase (calza)", DiamMm = o.DCalza * 1000, LungM = tot / si.Copies, Collegato = "al dipolo", Quanti = si.Copies });
            }
            if (Sigma[o.Materiale] > 0)
                foreach (int t in nuovi) m.Loads.Add(new LoadSpec { Type = 5, Tag = t, From = 0, To = 0, F1 = Sigma[o.Materiale] });
            List<string> err = m.Reevaluate();
            if (err.Count > 0) throw new NecException(err[0]);
            return r;
        }

        public class Confronto { public double G, Fb, FrMedio, Lobo, Tant, Gt; public Complex Zdiv; public double[] Az; }

        public static Confronto Valuta(NecModel m, double f, double th, double ph, bool orizz, CancellationToken ct)
        {
            SegmentGeometry g = m.BuildGeometry();
            RunResult r = Analysis.Run(m, g, f, 5, ct);
            Confronto c = new Confronto { G = r.Engine.Gain(th, ph).GainTotal, Fb = r.FrontBack, FrMedio = r.FrontRearMedio };
            Complex y = Complex.Zero; foreach (var s in r.Sources) y += 1 / s.Z;
            c.Zdiv = 1 / y;
            // taglio nel piano E (orizzontale: azimut all'elevazione del massimo; verticale: elevazione)
            double[] cut = new double[360];
            for (int a = 0; a < 360; a++) cut[a] = r.Engine.Gain(th, a).GainTotal;
            c.Az = cut;
            int i0 = ((int)Math.Round(ph) + 360) % 360;
            double gmax = cut[i0];
            int dx = 1; while (dx < 179 && cut[(i0 + dx) % 360] <= cut[(i0 + dx - 1) % 360] + 1e-9) dx++;
            int sxs = 1; while (sxs < 179 && cut[(i0 - sxs + 360) % 360] <= cut[(i0 - sxs + 1 + 360) % 360] + 1e-9) sxs++;
            double lobo = -99;
            for (int a = dx; a <= 360 - sxs; a++) lobo = Math.Max(lobo, cut[(i0 + a) % 360]);
            c.Lobo = lobo - gmax;
            if (r.Grid != null && m.Ground.Kind == GroundKind.FreeSpace)
            {
                GtResult gt = NoiseGt.Gt(r.Grid, r.ThetaMax, r.PhiMax, r.GmaxDbi, 10, 200, 290, 0, 0, true, r.Efficiency > 0 ? r.Efficiency : 100);
                c.Tant = gt.Tant; c.Gt = gt.GtAnt;
            }
            else { c.Tant = double.NaN; c.Gt = double.NaN; }
            return c;
        }
    }
}
