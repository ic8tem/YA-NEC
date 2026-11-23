// YA-NEC 1.5.3 - Ridimensiona e ricentra (richieste di IC8TEM del 20/11/2026: 12:25 «trovami un modo semplice per ricentrarla a
// 144.300»; 12:29 «si» con la revisione dei diametri commerciali, tondini pieni 3-4-5-6 mm e da 8 mm anche tubi; 12:31 «questo allora
// va qua», cioe' in Variabili e ottimizzazione insieme a Ridimensiona antenna; bozzetto «Ridimensiona e ricentra» approvato alle 12:32).
//
// Ricentra: uno sweep stretto (+-3 % della frequenza voluta) trova dove il modello lavora meglio adesso (ROS minimo su Z0); il modello
// si scala di k = f migliore / f voluta (posizioni e lunghezze; i raggi restano, sono i tubi veri). Ridimensiona: k = f vecchia / f nuova.
// Diametri commerciali: ogni filo diritto isolato (un elemento) prende la misura accesa piu' vicina e la sua lunghezza cambia in modo che
// la reattanza al centro resti quella di prima alla frequenza voluta (modello a linee di Schelkunoff, Leeson.Reattanza). Per l'RF un
// tubo e un tondino dello stesso diametro sono uguali. Ritocco del radiatore: la sola lunghezza del filo alimentato al centro si cerca
// perche' la reattanza all'ingresso sia 0 alla frequenza voluta (secante, pochi calcoli).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace YaNec.Engine
{
    public class RicentraRiga
    {
        public int Tag;
        public string Nome = "";
        public double DPrima, DDopo, LPrima, LDiametro, LScala;   // mm
    }

    public class RicentraEsito
    {
        public NecModel Modello;
        public double FMigliore = double.NaN, Fattore = 1, RosMin = double.NaN;
        public Complex ZPrima, ZDopo;
        public double RosPrima, RosDopo, GDopo = double.NaN, FbDopo = double.NaN, RitoccoMm;
        public List<RicentraRiga> Righe = new List<RicentraRiga>();
        public List<string> Note = new List<string>();
    }

    public static class Ricentra
    {
        public static readonly double[] Pieni = { 3, 4, 5, 6 };
        public static readonly double[] Tubi = { 8, 10, 12, 14, 15, 16, 18, 20, 22, 25, 28, 30, 32, 35, 40 };
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        internal static double Len(GeomCard c)
        {
            return Math.Sqrt((c.F4 - c.F1) * (c.F4 - c.F1) + (c.F5 - c.F2) * (c.F5 - c.F2) + (c.F6 - c.F3) * (c.F6 - c.F3));
        }

        static bool Tocca(double x, double y, double z, GeomCard b, double tol)
        {
            return (Math.Abs(x - b.F1) < tol && Math.Abs(y - b.F2) < tol && Math.Abs(z - b.F3) < tol) ||
                   (Math.Abs(x - b.F4) < tol && Math.Abs(y - b.F5) < tol && Math.Abs(z - b.F6) < tol);
        }

        /// <summary>Fili GW diritti che non toccano altri fili: sono elementi interi (riflettore, direttori, dipolo).</summary>
        public static List<GeomCard> Isolati(NecModel m)
        {
            List<GeomCard> g = m.Geometry.FindAll(c => c.Id == "GW" && c.I2 > 0 && !c.HasGc && c.F7 > 0);
            List<GeomCard> r = new List<GeomCard>();
            foreach (GeomCard a in g)
            {
                double tol = 1e-4 * Math.Max(1, Len(a));
                bool libero = true;
                foreach (GeomCard b in g)
                    if (!ReferenceEquals(a, b) && (Tocca(a.F1, a.F2, a.F3, b, tol) || Tocca(a.F4, a.F5, a.F6, b, tol))) { libero = false; break; }
                if (libero) r.Add(a);
            }
            return r;
        }

        internal static NecModel Copia(NecModel src)
        {
            NecModel m = NecModel.Parse(src.ToNec());
            foreach (GeomCard c in m.Geometry) c.Src = null;
            m.Variables.Clear();
            m.FileName = src.FileName;
            return m;
        }

        /// <summary>Misura commerciale piu' vicina (mm) tra quelle accese.</summary>
        public static double Commerciale(double dmm, IList<double> misure)
        {
            double best = dmm, err = double.MaxValue;
            foreach (double d in misure) { double e = Math.Abs(Math.Log(d / dmm)); if (e < err) { err = e; best = d; } }
            return best;
        }

        /// <summary>Mezza lunghezza con il diametro nuovo che ha al centro la stessa reattanza di prima alla frequenza f (MHz).</summary>
        public static double LunghezzaEquivalente(double h, double d1, double d2, double f)
        {
            if (Math.Abs(d1 - d2) < 1e-12) return h;
            double x0 = Leeson.Reattanza(new List<double[]> { new[] { h, d1 } }, f);
            double a = h * 0.8, b = h * 1.2;
            Func<double, double> fx = hh => Leeson.Reattanza(new List<double[]> { new[] { hh, d2 } }, f) - x0;
            double fa = fx(a), fb = fx(b);
            if (double.IsNaN(fa) || double.IsNaN(fb) || fa * fb > 0) return h;
            for (int k = 0; k < 60; k++)
            {
                double mm = (a + b) / 2, fm = fx(mm);
                if (fa * fm <= 0) { b = mm; fb = fm; } else { a = mm; fa = fm; }
            }
            return (a + b) / 2;
        }

        internal static void Accorcia(GeomCard c, double nuovaLunghezza)
        {
            double L = Len(c), k = nuovaLunghezza / L;
            double xc = (c.F1 + c.F4) / 2, yc = (c.F2 + c.F5) / 2, zc = (c.F3 + c.F6) / 2;
            c.F1 = xc + (c.F1 - xc) * k; c.F2 = yc + (c.F2 - yc) * k; c.F3 = zc + (c.F3 - zc) * k;
            c.F4 = xc + (c.F4 - xc) * k; c.F5 = yc + (c.F5 - yc) * k; c.F6 = zc + (c.F6 - zc) * k;
        }

        /// <summary>Scala posizioni e lunghezze di k attorno a (0, 0, quota piu' bassa): l'altezza dal suolo dell'antenna resta.</summary>
        public static void Scala(NecModel m, double k, bool raggi)
        {
            double z0 = double.MaxValue;
            foreach (GeomCard c in m.Geometry) if (c.Id == "GW") z0 = Math.Min(z0, Math.Min(c.F3, c.F6));
            if (z0 == double.MaxValue) z0 = 0;
            if (m.Ground.Kind == GroundKind.FreeSpace) z0 = 0;
            foreach (GeomCard c in m.Geometry)
            {
                if (c.Id != "GW") continue;
                c.F1 *= k; c.F2 *= k; c.F3 = z0 + (c.F3 - z0) * k;
                c.F4 *= k; c.F5 *= k; c.F6 = z0 + (c.F6 - z0) * k;
                if (raggi) { c.F7 *= k; c.GcRad1 *= k; c.GcRad2 *= k; }
            }
        }

        internal static GeomCard Alimentato(NecModel m)
        {
            if (m.Sources.Count != 1) return null;
            SourceSpec s = m.Sources[0];
            List<GeomCard> l = m.Geometry.FindAll(c => c.Id == "GW" && c.I1 == s.Tag);
            if (l.Count != 1) return null;
            GeomCard g = l[0];
            if (g.I2 % 2 == 1 && s.Seg == (g.I2 + 1) / 2) return g;
            return null;
        }

        static Complex Z(NecModel m, double f, CancellationToken ct)
        {
            List<SweepPoint> p = Analysis.Sweep(m, m.BuildGeometry(), new[] { f }, 90, 0, null, ct);
            if (p.Count == 0 || !p[0].Ok) throw new NecException(p.Count > 0 && p[0].Error != null ? p[0].Error : "calcolo non riuscito");
            return p[0].Z;
        }

        /// <summary>Frequenza di ROS minimo su z0 tra f (1 - 3 %) e f (1 + 3 %): 61 punti, poi 21 punti attorno al minimo.</summary>
        public static double Migliore(NecModel m, double f, double z0, CancellationToken ct, out double rosMin)
        {
            SegmentGeometry g = m.BuildGeometry();
            double a = f * 0.97, b = f * 1.03, best = f;
            rosMin = double.NaN;
            for (int giro = 0; giro < 2; giro++)
            {
                int n = giro == 0 ? 61 : 21;
                List<double> fr = new List<double>();
                for (int i = 0; i < n; i++) fr.Add(a + (b - a) * i / (n - 1));
                List<SweepPoint> p = Analysis.Sweep(m, g, fr, 90, 0, null, ct);
                double r = double.MaxValue;
                foreach (SweepPoint q in p)
                {
                    if (!q.Ok) continue;
                    double s = Analysis.Swr(q.Z, z0);
                    if (s < r) { r = s; best = q.F; }
                }
                if (r == double.MaxValue) throw new NecException("nessun punto dello sweep e' riuscito");
                rosMin = r;
                double passo = (b - a) / (n - 1);
                a = best - passo; b = best + passo;
            }
            return best;
        }

        /// <summary>
        /// modo 0 = ricentra (f migliore -> fVoluta), 1 = ridimensiona (fModello -> fVoluta). diametri: 0 in scala, 1 uguali, 2 commerciali.
        /// </summary>
        public static RicentraEsito Calcola(NecModel src, int modo, double fModello, double fVoluta, int diametri, IList<double> misure,
                                            bool ritocco, double z0, CancellationToken ct)
        {
            RicentraEsito r = new RicentraEsito();
            NecModel m = Copia(src);
            m.FreqMHz = fVoluta;
            r.ZPrima = Z(src, fVoluta, ct);
            r.RosPrima = Analysis.Swr(r.ZPrima, z0);
            double rosMin;
            if (modo == 0)
            {
                r.FMigliore = Migliore(src, fVoluta, z0, ct, out rosMin);
                r.RosMin = rosMin;
                r.Fattore = r.FMigliore / fVoluta;
            }
            else r.Fattore = fModello / fVoluta;

            List<GeomCard> el = Isolati(m);
            Dictionary<GeomCard, RicentraRiga> righe = new Dictionary<GeomCard, RicentraRiga>();
            int nEl = 0;
            foreach (GeomCard c in m.Geometry.Where(c => c.Id == "GW" && el.Contains(c)))
            {
                nEl++;
                RicentraRiga rg = new RicentraRiga { Tag = c.I1, Nome = "filo " + c.I1, DPrima = 2000 * c.F7, LPrima = 1000 * Len(c) };
                righe[c] = rg;
                r.Righe.Add(rg);
            }
            // 1) scala (con "in scala" anche i raggi)
            Scala(m, r.Fattore, diametri == 0);
            // 2) diametri commerciali alla frequenza voluta, con la lunghezza che tiene la reattanza
            foreach (KeyValuePair<GeomCard, RicentraRiga> kv in righe)
            {
                GeomCard c = kv.Key;
                double d1 = 2000 * c.F7, d2 = diametri == 2 && misure != null && misure.Count > 0 ? Commerciale(d1, misure) : d1;
                double L = Len(c);
                kv.Value.LDiametro = 1000 * L;
                if (Math.Abs(d2 - d1) > 1e-9)
                {
                    double h2 = LunghezzaEquivalente(L / 2, d1 / 1000, d2 / 1000, fVoluta);
                    Accorcia(c, 2 * h2);
                    c.F7 = d2 / 2000;
                }
                kv.Value.DDopo = d2;
                kv.Value.LScala = 1000 * Len(c);
            }
            if (diametri == 2 && m.Geometry.Any(c => c.Id == "GW" && !el.Contains(c)))
                r.Note.Add("I fili collegati ad altri fili (elementi a gradini, ripiegati, gamma) hanno tenuto il loro diametro.");
            // 3) ritocco del radiatore: X = 0 alla frequenza voluta
            if (ritocco)
            {
                GeomCard ra = Alimentato(m);
                if (ra == null) r.Note.Add("Ritocco del radiatore non fatto: serve una sola sorgente al centro di un filo con un numero dispari di segmenti.");
                else
                {
                    double L0 = Len(ra), l1 = L0, l2 = L0 * 0.995;
                    Accorcia(ra, l1); double x1 = Z(m, fVoluta, ct).Imaginary;
                    Accorcia(ra, l2); double x2 = Z(m, fVoluta, ct).Imaginary;
                    for (int k = 0; k < 8 && Math.Abs(x2) > 0.05 && Math.Abs(x2 - x1) > 1e-9; k++)
                    {
                        double l3 = l2 - x2 * (l2 - l1) / (x2 - x1);
                        l3 = Math.Max(L0 * 0.9, Math.Min(L0 * 1.1, l3));
                        l1 = l2; x1 = x2; l2 = l3;
                        Accorcia(ra, l2); x2 = Z(m, fVoluta, ct).Imaginary;
                    }
                    r.RitoccoMm = 1000 * (Len(ra) - L0);
                    RicentraRiga rg;
                    if (righe.TryGetValue(ra, out rg)) rg.LScala = 1000 * Len(ra);
                }
            }
            RunResult fin = Analysis.Run(m, m.BuildGeometry(), fVoluta, 5.0, ct);
            r.ZDopo = fin.Zin;
            r.RosDopo = Analysis.Swr(r.ZDopo, z0);
            r.GDopo = fin.GmaxDbi;
            r.FbDopo = fin.FrontBack;
            m.FileName = string.IsNullOrEmpty(src.FileName) ? "ricentrato.nec" :
                System.IO.Path.GetFileNameWithoutExtension(src.FileName) + (modo == 0 ? " ricentrato " : " ridimensionato ") + fVoluta.ToString("0.###", CI) + ".nec";
            r.Modello = m;
            return r;
        }
    }
}
