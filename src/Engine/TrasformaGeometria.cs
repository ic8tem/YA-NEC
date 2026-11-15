// YA-NEC - barra Geometria (bozzetto approvato doc/bozzetto-geometria.png): sposta, ruota, scala, specchia, copia.
// GPL-3.0. Approvato da IC8TEM il 13/11/2026 ("approvato procedi").
//
// Ogni operazione e' una trasformazione affine p' = c + M (p - c) + t applicata agli estremi dei fili GW scelti. Se una
// coordinata e' scritta con variabili (o se l'angolo diventa la variabile ANG) il risultato si scrive come formula, cosi'
// le variabili restano vive (sweep, ottimizzatore). Sorgenti, carichi, linee e rastremazioni sono agganciati al tag e al
// segmento: seguono il filo senza modifiche.
// Con "tutta l'antenna" e schede di geometria diverse da GW (GA, GH, GX, GR, GM...) sposta, ruota e scala aggiungono in
// fondo una scheda GM (e GS per la scala), che il NEC applica a tutta la struttura: risultato esatto senza riscrivere archi,
// eliche e simmetrie. Specchia e copia lavorano solo sui fili GW.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public sealed class Affine
    {
        public string[,] M = { { "1", "0", "0" }, { "0", "1", "0" }, { "0", "0", "1" } };   // testo (numeri o formule con ANG)
        public double[,] Mv = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
        public double[] C = new double[3], T = new double[3];
        public bool Riflessione;          // inverte il verso (specchio)
        public string Descrizione = "";

        public double[] Applica(double x, double y, double z)
        {
            double[] p = { x - C[0], y - C[1], z - C[2] }, r = new double[3];
            for (int i = 0; i < 3; i++) r[i] = C[i] + Mv[i, 0] * p[0] + Mv[i, 1] * p[1] + Mv[i, 2] * p[2] + T[i];
            return r;
        }
    }

    public static class TrasformaGeometria
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static string N(double v) { return Math.Abs(v) < 1e-12 ? "0" : v.ToString("0.#########", CI); }

        public static Affine Sposta(double dx, double dy, double dz)
        {
            return new Affine { T = new[] { dx, dy, dz }, Descrizione = string.Format(CI, "sposta di ({0}; {1}; {2}) m", N(dx), N(dy), N(dz)) };
        }

        /// <summary>Rotazione attorno all'asse (0 X, 1 Y, 2 Z) passante per c; gradi positivi = antiorario guardando dall'asse
        /// verso l'origine. varAngolo: nome della variabile da scrivere nelle formule (null = numeri).</summary>
        public static Affine Ruota(int asse, double gradi, double[] c, string varAngolo = null)
        {
            Affine a = new Affine { C = (double[])c.Clone(), Descrizione = string.Format(CI, "ruota di {0} gradi attorno a {1}", N(gradi), "XYZ"[asse]) };
            double r = gradi * Math.PI / 180, co = Math.Cos(r), si = Math.Sin(r);
            string sc = varAngolo != null ? "COS(" + varAngolo + ")" : N(co), ss = varAngolo != null ? "SIN(" + varAngolo + ")" : N(si);
            string ms = varAngolo != null ? "-SIN(" + varAngolo + ")" : N(-si);
            int i = (asse + 1) % 3, j = (asse + 2) % 3;
            a.Mv[i, i] = co; a.Mv[i, j] = -si; a.Mv[j, i] = si; a.Mv[j, j] = co;
            a.M[i, i] = sc; a.M[i, j] = ms; a.M[j, i] = ss; a.M[j, j] = sc;
            return a;
        }

        public static Affine Scala(double k, double[] c)
        {
            Affine a = new Affine { C = (double[])c.Clone(), Descrizione = "scala x " + N(k) };
            for (int i = 0; i < 3; i++) { a.Mv[i, i] = k; a.M[i, i] = N(k); }
            return a;
        }

        /// <summary>Specchio nel piano perpendicolare all'asse dato (0: piano YZ, 1: XZ, 2: XY) passante per c.</summary>
        public static Affine Specchia(int asse, double[] c)
        {
            Affine a = new Affine { C = (double[])c.Clone(), Riflessione = true, Descrizione = "specchia nel piano " + new[] { "YZ", "XZ", "XY" }[asse] };
            a.Mv[asse, asse] = -1; a.M[asse, asse] = "-1";
            return a;
        }

        // ------------------------------------------------------------------------------------------------
        static bool Numero(string s) { double v; return double.TryParse(s, NumberStyles.Float, CI, out v); }

        static string Par(string s) { return Numero(s) && !s.StartsWith("-") ? s : "(" + s + ")"; }

        /// <summary>Coordinata trasformata come testo: numero se tutto e' numerico, altrimenti formula.</summary>
        static string Coord(Affine a, int i, string[] p, double[] pv)
        {
            bool tuttiNumeri = Numero(p[0]) && Numero(p[1]) && Numero(p[2]);
            bool matriceNumeri = true;
            for (int k = 0; k < 3; k++) if (!Numero(a.M[i, k])) matriceNumeri = false;
            if (tuttiNumeri && matriceNumeri)
            {
                double[] r = a.Applica(pv[0], pv[1], pv[2]);
                return N(r[i]);
            }
            List<string> t = new List<string>();
            double cost = a.C[i] + a.T[i];
            for (int k = 0; k < 3; k++)
            {
                string m = a.M[i, k];
                if (m == "0") continue;
                string d = a.C[k] == 0 ? Par(p[k]) : "(" + p[k] + "-" + Par(N(a.C[k])) + ")";
                if (m == "1") t.Add(d);
                else if (m == "-1") t.Add("-" + d);
                else t.Add(Par(m) + "*" + d);
            }
            string s = string.Join("+", t).Replace("+-", "-");
            if (Math.Abs(cost) > 1e-12) s = N(cost) + (s.StartsWith("-") ? "" : "+") + s;
            if (s.Length == 0) s = "0";
            return s;
        }

        static string[] Testi(GeomCard g)
        {
            if (g.Src != null && g.Src.Length >= 9) return (string[])g.Src.Clone();
            return new[] { g.I1.ToString(CI), g.I2.ToString(CI), N(g.F1), N(g.F2), N(g.F3), N(g.F4), N(g.F5), N(g.F6), N(g.F7) };
        }

        static void Trasforma(GeomCard g, Affine a, bool raggio, double kRaggio)
        {
            string[] s = Testi(g);
            string[] e1 = { s[2], s[3], s[4] }, e2 = { s[5], s[6], s[7] };
            double[] v1 = { g.F1, g.F2, g.F3 }, v2 = { g.F4, g.F5, g.F6 };
            string[] n1 = new string[3], n2 = new string[3];
            for (int i = 0; i < 3; i++) { n1[i] = Coord(a, i, e1, v1); n2[i] = Coord(a, i, e2, v2); }
            for (int i = 0; i < 3; i++) { s[2 + i] = n1[i]; s[5 + i] = n2[i]; }
            if (raggio && Math.Abs(kRaggio - 1) > 1e-12) s[8] = Numero(s[8]) ? N(g.F7 * kRaggio) : Par(N(kRaggio)) + "*" + Par(s[8]);
            g.Src = s;
        }

        public static bool SoloFili(NecModel m)
        {
            foreach (GeomCard g in m.Geometry) if (g.Id != "GW" && g.Id != "GE") return false;
            return true;
        }

        /// <summary>Applica la trasformazione al modello (in un clone). tags = null: tutta l'antenna.</summary>
        public static NecModel Applica(NecModel m0, Affine a, HashSet<int> tags, bool scalaRaggi = false, double kScala = 1)
        {
            NecModel m = m0.Clone();
            bool tutta = tags == null;
            if (tutta && !SoloFili(m))
            {
                if (a.Riflessione) throw new NecException("Specchia: nel modello ci sono schede diverse da GW (archi, eliche, simmetrie): si specchiano solo i fili GW scelti.");
                bool haVar = false;
                foreach (string q in a.M) if (!Numero(q)) haVar = true;
                if (haVar) throw new NecException("Angolo come variabile: con archi, eliche o simmetrie nel modello la rotazione si scrive con una scheda GM, che vuole numeri.");
                AggiungiGm(m, a, kScala);
            }
            else
            {
                int n = 0;
                foreach (GeomCard g in m.Geometry)
                {
                    if (g.Id != "GW") continue;
                    if (!tutta && !tags.Contains(g.I1)) continue;
                    Trasforma(g, a, scalaRaggi, kScala);
                    n++;
                }
                if (n == 0) throw new NecException("Nessun filo GW scelto.");
            }
            List<string> err = m.Reevaluate();
            if (err.Count > 0) throw new NecException(err[0]);
            return m;
        }

        // GM (rotazioni attorno a X, Y, Z dell'origine, poi traslazione) e GS per la scala: c + M (p - c) + t = M p + (c - M c + t)
        static void AggiungiGm(NecModel m, Affine a, double kScala)
        {
            double[] off = new double[3];
            for (int i = 0; i < 3; i++)
            {
                off[i] = a.C[i] + a.T[i];
                for (int k = 0; k < 3; k++) off[i] -= a.Mv[i, k] * a.C[k];
            }
            if (Math.Abs(kScala - 1) > 1e-12)
            {
                m.Geometry.Add(new GeomCard { Id = "GS", Src = new[] { "0", "0", N(kScala), "0", "0", "0", "0", "0", "0" }, F1 = kScala });
            }
            // angoli di una sola rotazione attorno a un asse (quelle della barra)
            double rx = 0, ry = 0, rz = 0;
            if (Math.Abs(a.Mv[0, 0] - 1) < 1e-12 && Math.Abs(kScala - 1) < 1e-12) rx = Math.Atan2(a.Mv[2, 1], a.Mv[1, 1]) * 180 / Math.PI;
            else if (Math.Abs(a.Mv[1, 1] - 1) < 1e-12 && Math.Abs(kScala - 1) < 1e-12) ry = Math.Atan2(a.Mv[0, 2], a.Mv[0, 0]) * 180 / Math.PI;
            else if (Math.Abs(kScala - 1) < 1e-12) rz = Math.Atan2(a.Mv[1, 0], a.Mv[0, 0]) * 180 / Math.PI;
            m.Geometry.Add(new GeomCard
            {
                Id = "GM",
                Src = new[] { "0", "0", N(rx), N(ry), N(rz), N(off[0]), N(off[1]), N(off[2]), "0" },
                F1 = rx, F2 = ry, F3 = rz, F4 = off[0], F5 = off[1], F6 = off[2]
            });
        }

        /// <summary>Copie dei fili scelti: ogni copia applica di nuovo la trasformazione all'ultima. Tag nuovi dopo il massimo.</summary>
        public static NecModel Copia(NecModel m0, Affine a, HashSet<int> tags, int copie)
        {
            NecModel m = m0.Clone();
            List<GeomCard> base_ = m.Geometry.FindAll(g => g.Id == "GW" && (tags == null || tags.Contains(g.I1)));
            if (base_.Count == 0) throw new NecException("Copia: nessun filo GW scelto.");
            int maxTag = 0;
            foreach (GeomCard g in m.Geometry) maxTag = Math.Max(maxTag, g.I1);
            List<GeomCard> prec = base_;
            for (int c = 0; c < copie; c++)
            {
                List<GeomCard> nuove = new List<GeomCard>();
                foreach (GeomCard g in prec)
                {
                    GeomCard q = g.Clone();
                    q.Src = Testi(g);
                    Trasforma(q, a, false, 1);
                    q.Src[0] = (++maxTag).ToString(CI);
                    m.Geometry.Add(q);
                    nuove.Add(q);
                }
                List<string> err = m.Reevaluate();
                if (err.Count > 0) throw new NecException(err[0]);
                prec = nuove;
            }
            return m;
        }

        /// <summary>Copia speculare (come GX, ma scritta come fili GW): i fili scelti restano e si aggiungono gli specchiati.</summary>
        public static NecModel CopiaSpeculare(NecModel m0, Affine a, HashSet<int> tags) { return Copia(m0, a, tags, 1); }

        // ------------------------------------------------------------------------------------------------
        // misure dell'antenna per i centri e i comandi rapidi
        // ------------------------------------------------------------------------------------------------
        public static double[] Centro(SegmentGeometry g)
        {
            double[] mn = { double.MaxValue, double.MaxValue, double.MaxValue }, mx = { double.MinValue, double.MinValue, double.MinValue };
            for (int i = 0; i < g.N; i++)
            {
                double[] p = { g.X1[i], g.Y1[i], g.Z1[i], g.X2[i], g.Y2[i], g.Z2[i] };
                for (int k = 0; k < 3; k++) { mn[k] = Math.Min(mn[k], Math.Min(p[k], p[k + 3])); mx[k] = Math.Max(mx[k], Math.Max(p[k], p[k + 3])); }
            }
            if (g.N == 0) return new double[3];
            return new[] { (mn[0] + mx[0]) / 2, (mn[1] + mx[1]) / 2, (mn[2] + mx[2]) / 2 };
        }

        public static double ZMin(SegmentGeometry g)
        {
            double z = double.MaxValue;
            for (int i = 0; i < g.N; i++) z = Math.Min(z, Math.Min(g.Z1[i], g.Z2[i]));
            return g.N == 0 ? 0 : z;
        }

        /// <summary>Asse orizzontale per passare da polarizzazione orizzontale a verticale: il boom (X o Y con i centri dei fili
        /// piu' sparsi); senza boom l'asse orizzontale perpendicolare al filo piu' lungo.</summary>
        public static int AsseOrizzVert(NecModel m, SegmentGeometry g)
        {
            double mnx = double.MaxValue, mxx = double.MinValue, mny = double.MaxValue, mxy = double.MinValue;
            GeomCard lungo = null; double ll = 0;
            foreach (GeomCard c in m.Geometry)
            {
                if (c.Id != "GW") continue;
                double cx = (c.F1 + c.F4) / 2, cy = (c.F2 + c.F5) / 2;
                mnx = Math.Min(mnx, cx); mxx = Math.Max(mxx, cx); mny = Math.Min(mny, cy); mxy = Math.Max(mxy, cy);
                double l = Math.Sqrt((c.F4 - c.F1) * (c.F4 - c.F1) + (c.F5 - c.F2) * (c.F5 - c.F2) + (c.F6 - c.F3) * (c.F6 - c.F3));
                if (l > ll) { ll = l; lungo = c; }
            }
            double ex = mxx - mnx, ey = mxy - mny;
            if (Math.Max(ex, ey) > 1e-6) return ex >= ey ? 0 : 1;
            if (lungo != null && Math.Abs(lungo.F4 - lungo.F1) > Math.Abs(lungo.F5 - lungo.F2)) return 1;
            return 0;
        }
    }
}
