// YA-NEC 0.4.30 - verticale multibanda a ventaglio tipo DX Commander (dxcommander.com), sottolinguetta dei Calcolatori dal bozzetto 10
// rivisto, approvato da IC8TEM il 14/11/2026 22:41 (richiesta originale del 12/11/2026: "linguetta sperimentale di verifica per le
// verticali multibanda tipo DX Commander"). Modello "dal file" (dxcommander_A di IC8TEM, data/archivio) o "nuovo" (elementi a quarto
// d'onda attorno al palo, il 40 m sul palo con il ripiego in cima). Terreno della scheda (come la finestra o suo, sempre Sommerfeld),
// radiali N x L alla quota scelta. Verifica per banda: sweep di 41 punti, ROS minimo, guadagno a 10 gradi, efficienza (potenza
// irradiata sopra il terreno / potenza d'ingresso) e l'elemento di un'altra banda che "ruba" corrente (come la nota 18 del manuale).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading;

namespace YaNec.Engine
{
    public class VmElemento
    {
        public string Banda = "";
        public double L, Dist;          // lunghezza dal mozzo (m), distanza dal palo (m)
        public bool Attivo = true, Palo;
        public List<int> Tag = new List<int>();   // fili dell'elemento nel modello (dal file)
    }

    public class VmSpec
    {
        public bool DalFile = true;
        public NecModel File;                       // modello dal file (dxcommander_A)
        public List<VmElemento> El = new List<VmElemento>();
        public double H0 = 0.3, AltezzaPalo = 9.0, Raggio = 0.0005;
        public GroundSpec Terreno = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005, Sommerfeld = true };
        public int Radiali = 32;
        public double LRad = 5, Quota = 0.05;
    }

    public class VmBanda
    {
        public string Banda;
        public double RosMin = double.NaN, FRos, G10 = double.NaN, Eff = double.NaN;
        public string Ruba = "";
        public int Fatti, Totale = 41;
    }

    public static class VerticaleMultibanda
    {
        const double C0 = 299.792458;
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        static double Len(GeomCard g) { return Math.Sqrt((g.F4 - g.F1) * (g.F4 - g.F1) + (g.F5 - g.F2) * (g.F5 - g.F2) + (g.F6 - g.F3) * (g.F6 - g.F3)); }
        static bool Vic(double ax, double ay, double az, double bx, double by, double bz) { return Math.Abs(ax - bx) < 1e-4 && Math.Abs(ay - by) < 1e-4 && Math.Abs(az - bz) < 1e-4; }

        /// <summary>Banda radioamatoriale piu' vicina alla risonanza di un quarto d'onda lungo L (accorciamento 0,95).</summary>
        public static string BandaDi(double L)
        {
            double f = 0.95 * C0 / (4 * Math.Max(0.1, L)), best = 1e9; string n = "";
            foreach (Band b in Bands.All)
                if (b.Operating > 1.5 && b.Operating < 60 && Math.Abs(Math.Log(b.Operating / f)) < best) { best = Math.Abs(Math.Log(b.Operating / f)); n = b.Name; }
            return n;
        }

        /// <summary>Elementi del modello del file: rami che partono dal mozzo (cima del filo della sorgente) con un tratto orizzontale.</summary>
        public static List<VmElemento> ElementiDalFile(NecModel m)
        {
            List<VmElemento> l = new List<VmElemento>();
            if (m.Sources.Count == 0) return l;
            GeomCard fe = m.Geometry.Find(g => g.Id == "GW" && g.I1 == m.Sources[0].Tag);
            if (fe == null) return l;
            double hx = fe.F6 > fe.F3 ? fe.F4 : fe.F1, hy = fe.F6 > fe.F3 ? fe.F5 : fe.F2, hz = Math.Max(fe.F3, fe.F6);
            HashSet<int> usati = new HashSet<int> { m.Geometry.IndexOf(fe) };
            for (int i = 0; i < m.Geometry.Count; i++)
            {
                GeomCard g = m.Geometry[i];
                if (g.Id != "GW" || usati.Contains(i)) continue;
                bool da1 = Vic(g.F1, g.F2, g.F3, hx, hy, hz), da2 = Vic(g.F4, g.F5, g.F6, hx, hy, hz);
                if (!da1 && !da2) continue;
                VmElemento e = new VmElemento { Dist = Len(g) };
                e.Tag.Add(g.I1); usati.Add(i);
                double cx = da1 ? g.F4 : g.F1, cy = da1 ? g.F5 : g.F2, cz = da1 ? g.F6 : g.F3;
                bool avanti = true;
                while (avanti)
                {
                    avanti = false;
                    for (int k = 0; k < m.Geometry.Count; k++)
                    {
                        GeomCard q = m.Geometry[k];
                        if (q.Id != "GW" || usati.Contains(k)) continue;
                        bool a1 = Vic(q.F1, q.F2, q.F3, cx, cy, cz), a2 = Vic(q.F4, q.F5, q.F6, cx, cy, cz);
                        if (!a1 && !a2) continue;
                        e.L += Len(q); e.Tag.Add(q.I1); usati.Add(k);
                        cx = a1 ? q.F4 : q.F1; cy = a1 ? q.F5 : q.F2; cz = a1 ? q.F6 : q.F3;
                        avanti = true; break;
                    }
                }
                e.Banda = BandaDi(e.L);
                l.Add(e);
            }
            double lmax = 0; foreach (VmElemento e in l) lmax = Math.Max(lmax, e.L);
            foreach (VmElemento e in l) e.Palo = e.L == lmax;
            return l;
        }

        /// <summary>Elementi del modello nuovo: quarti d'onda (x 0,95) di 40, 20, 17, 15, 12 e 10 m; il 40 m sul palo.</summary>
        public static List<VmElemento> ElementiNuovi()
        {
            List<VmElemento> l = new List<VmElemento>();
            string[] b = { "40 m", "20 m", "17 m", "15 m", "12 m", "10 m" };
            foreach (string n in b)
            {
                Band bb = Bands.All.Find(x => x.Name == n);
                l.Add(new VmElemento { Banda = n, L = 0.95 * C0 / (4 * bb.Operating), Dist = n == "40 m" ? 0.0 : 0.06, Palo = n == "40 m" });
            }
            return l;
        }

        /// <summary>Modello NEC della scheda alla frequenza f, con il terreno e i radiali della scheda. note: avvisi.</summary>
        public static NecModel Modello(VmSpec s, double f, List<string> note)
        {
            bool reale = s.Terreno.Kind == GroundKind.Real, perfetto = s.Terreno.Kind == GroundKind.Perfect;
            double q = reale ? s.Quota : 0;
            if (reale && q < 0.01) { q = 0.01; note?.Add("Radiali interrati o sul terreno: il NEC-2 non li tratta; calcolati a 1 cm dal suolo (quota minima)."); }
            NecModel m;
            if (s.DalFile && s.File != null)
            {
                m = s.File.Clone();
                // tolti i fili degli elementi spenti
                HashSet<int> via = new HashSet<int>();
                foreach (VmElemento e in s.El) if (!e.Attivo) foreach (int t in e.Tag) via.Add(t);
                m.Geometry.RemoveAll(g => g.Id == "GW" && via.Contains(g.I1));
                m.Loads.RemoveAll(ld => via.Contains(ld.Tag));
                if (reale) foreach (GeomCard g in m.Geometry) { g.F3 += q; g.F6 += q; g.Src = null; }
            }
            else
            {
                m = NecModel.Parse("CM verticale multibanda\nCE\nGW 1 1 0 0 0 0 0 1 0.001\nGE 0\nFR 0 1 0 0 7.1 0\nEN");
                m.Geometry.Clear();
                double top = q + s.H0, seg = 0.25;
                m.Geometry.Add(G(1, 3, 0, 0, q, 0, 0, top, s.Raggio));
                int tag = 2, n = s.El.Count, k = 0;
                foreach (VmElemento e in s.El)
                {
                    double an = 2 * Math.PI * (k++) / Math.Max(1, n), d = Math.Max(0.02, e.Dist), x = d * Math.Cos(an), y = d * Math.Sin(an);
                    e.Tag.Clear();
                    if (!e.Attivo) continue;
                    e.Tag.Add(tag); m.Geometry.Add(G(tag++, 1, 0, 0, top, x, y, top, s.Raggio));
                    double su = Math.Min(e.L, s.AltezzaPalo);
                    e.Tag.Add(tag); m.Geometry.Add(G(tag++, Math.Max(3, (int)Math.Ceiling(su / seg)), x, y, top, x, y, top + su, s.Raggio));
                    if (e.L > s.AltezzaPalo + 1e-6)
                    {
                        double x2 = x * 1.8 + (Math.Abs(x) + Math.Abs(y) < 1e-6 ? 0.048 : 0), y2 = y * 1.8, giu = e.L - s.AltezzaPalo;
                        e.Tag.Add(tag); m.Geometry.Add(G(tag++, 1, x, y, top + su, x2, y2, top + su, s.Raggio));
                        e.Tag.Add(tag); m.Geometry.Add(G(tag++, Math.Max(3, (int)Math.Ceiling(giu / seg)), x2, y2, top + su, x2, y2, top + su - giu, s.Raggio));
                    }
                }
                m.Sources.Add(new SourceSpec { Tag = 1, Seg = 1 });
            }
            if (reale)
            {
                int tag = 1; foreach (GeomCard g in m.Geometry) tag = Math.Max(tag, g.I1 + 1);
                int nr = Math.Max(0, s.Radiali);
                for (int k = 0; k < nr; k++)
                {
                    double an = 2 * Math.PI * k / nr;
                    m.Geometry.Add(G(tag++, Math.Max(3, (int)Math.Ceiling(s.LRad / 0.5)), 0, 0, q, s.LRad * Math.Cos(an), s.LRad * Math.Sin(an), q, 0.001));
                }
                if (nr == 0) note?.Add("Nessun radiale: la base del filo della sorgente e' libera sopra il terreno.");
                m.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = s.Terreno.Epsr, Sigma = s.Terreno.Sigma, Sommerfeld = true };
                m.GroundFlag = 0;
            }
            else if (perfetto) { m.Ground = new GroundSpec { Kind = GroundKind.Perfect }; m.GroundFlag = 1; }
            else { m.Ground = new GroundSpec { Kind = GroundKind.FreeSpace }; m.GroundFlag = 0; }
            m.FreqMHz = f;
            List<string> err = m.Reevaluate();
            if (err.Count > 0) throw new NecException(err[0]);
            return m;
        }

        static GeomCard G(int tag, int ns, double x1, double y1, double z1, double x2, double y2, double z2, double r)
        {
            return new GeomCard { Id = "GW", I1 = tag, I2 = ns, F1 = x1, F2 = y1, F3 = z1, F4 = x2, F5 = y2, F6 = z2, F7 = r };
        }

        /// <summary>Verifica di una banda: sweep di 41 punti (banda +-3 %), ROS minimo, guadagno a 10 gradi e efficienza al minimo,
        /// elemento di un'altra banda con la corrente piu' alta rispetto all'elemento della banda.</summary>
        public static VmBanda Verifica(VmSpec s, VmElemento el, Action<VmBanda> avanzamento, CancellationToken ct)
        {
            VmBanda r = new VmBanda { Banda = el.Banda };
            Band b = Bands.All.Find(x => x.Name == el.Banda);
            if (b == null) return r;
            double f1 = b.Low * 0.97, f2 = b.High * 1.03;
            NecModel m = Modello(s, b.Operating, null);
            SegmentGeometry g = m.BuildGeometry();
            double best = double.MaxValue;
            for (int i = 0; i < r.Totale; i++)
            {
                ct.ThrowIfCancellationRequested();
                double f = f1 + (f2 - f1) * i / (r.Totale - 1);
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
                double ros = Analysis.Swr(e.Sources[0].Z, 50);
                if (ros < best) { best = ros; r.RosMin = ros; r.FRos = f; }
                r.Fatti = i + 1;
                avanzamento?.Invoke(r);
            }
            Nec2Engine e0 = new Nec2Engine(g);
            e0.Solve(r.FRos, Analysis.EffectiveSources(m, g, r.FRos), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(r.FRos));
            r.G10 = e0.Gain(80, 0).GainTotal;
            // efficienza: guadagno medio sopra il terreno (potenza irradiata / potenza d'ingresso), griglia di 5 x 15 gradi
            double sum = 0, dth = 5 * Math.PI / 180, dph = 15 * Math.PI / 180;
            for (double th = 2.5; th < 90; th += 5)
                for (double ph = 0; ph < 360; ph += 15)
                {
                    double gt = e0.Gain(th, ph).GainTotal;
                    if (gt > -900) sum += Math.Pow(10, gt / 10) * Math.Sin(th * Math.PI / 180) * dth * dph;
                }
            r.Eff = 100 * sum / (4 * Math.PI);
            // corrente massima su ogni elemento
            Dictionary<int, VmElemento> perTag = new Dictionary<int, VmElemento>();
            foreach (VmElemento x in s.El) foreach (int t in x.Tag) perTag[t] = x;
            Dictionary<VmElemento, double> imax = new Dictionary<VmElemento, double>();
            for (int i = 0; i < g.N; i++)
            {
                VmElemento x;
                if (!perTag.TryGetValue(g.Tag[i], out x)) continue;
                double a = Complex.Abs(e0.SegmentCurrent(i));
                double v; imax.TryGetValue(x, out v); imax[x] = Math.Max(v, a);
            }
            double mio; imax.TryGetValue(el, out mio);
            double peggio = 0; VmElemento ladro = null;
            foreach (KeyValuePair<VmElemento, double> kv in imax) if (kv.Key != el && kv.Value > peggio) { peggio = kv.Value; ladro = kv.Key; }
            if (ladro != null && mio > 0 && peggio > 0.3 * mio) r.Ruba = ladro.Banda + string.Format(CI, " ({0:0}% della corrente)", peggio / mio * 100);
            else r.Ruba = "nessuno";
            return r;
        }
    }
}
