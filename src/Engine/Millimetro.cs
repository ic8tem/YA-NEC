// YA-NEC 1.5.2 - Ottimizzatore, riquadro «4. Misure al millimetro» (bozzetto v2 approvato da IC8TEM il 19/11/2026 14:37,
// doc/bozzetti_approvati/Ottimizzatore_misure_al_millimetro_19-11-2026.html).
// Arrotonda al millimetro le coordinate dei fili scritte come numero e, a scelta, porta i diametri al tubo o al filo commerciale
// piu' vicino. Si arrotondano i punti, non i fili: un estremo in comune a due fili diventa lo stesso numero in entrambi (anche
// se uno dei due fili non e' tra quelli scelti), cosi' i giunti dei tubi telescopici restano collegati. Formule e variabili non si
// toccano; il numero dei segmenti non cambia.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public static class Millimetro
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        /// <summary>Tubi di alluminio commerciali (stessa serie dei Calcoli meccanici), mm.</summary>
        public static readonly double[] Tubi = { 6, 8, 10, 12, 13, 14, 15, 16, 18, 19, 20, 22, 25, 28, 30, 32, 35, 38, 40, 45, 50, 60 };
        /// <summary>Fili e tondi metrici, mm (sotto i 6 mm si guardano anche i diametri AWG 0-20).</summary>
        public static readonly double[] FiliMetrici = { 0.5, 0.6, 0.8, 1.0, 1.2, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0 };

        public class Cambio { public int Tag; public string Campo; public double Prima, Dopo; public bool Diametro; public string Nota = ""; }

        public class Esito
        {
            public NecModel Modello;
            public List<Cambio> Cambi = new List<Cambio>();
            public int Fili, Formule;
            public double SpostamentoMaxMm, DeltaLunghezzaMm;
            public Dictionary<int, double> SpostamentoPerFilo = new Dictionary<int, double>();
        }

        /// <summary>Diametro commerciale piu' vicino (mm) e il suo nome.</summary>
        public static double DiametroCommerciale(double dmm, out string nome)
        {
            double best = double.NaN, err = double.MaxValue; nome = "";
            if (dmm >= 5.5)
            {
                foreach (double t in Tubi) if (Math.Abs(t - dmm) < err) { err = Math.Abs(t - dmm); best = t; nome = "tubo commerciale"; }
                return best;
            }
            foreach (double t in FiliMetrici) if (Math.Abs(t - dmm) < err) { err = Math.Abs(t - dmm); best = t; nome = "filo metrico"; }
            for (int n = 0; n <= 20; n++)
            {
                double d = Expr.RaggioAwg(n) * 2000;
                if (Math.Abs(d - dmm) < err) { err = Math.Abs(d - dmm); best = d; nome = "AWG " + n; }
            }
            return best;
        }

        static bool Numero(GeomCard g, int k)
        {
            if (g.Src == null || k >= g.Src.Length || string.IsNullOrWhiteSpace(g.Src[k])) return true;
            double v; return double.TryParse(g.Src[k].Trim(), NumberStyles.Float, CI, out v);
        }

        static double Get(GeomCard g, int f) { switch (f) { case 1: return g.F1; case 2: return g.F2; case 3: return g.F3; case 4: return g.F4; case 5: return g.F5; case 6: return g.F6; default: return g.F7; } }
        static void Set(GeomCard g, int f, double v)
        {
            switch (f) { case 1: g.F1 = v; break; case 2: g.F2 = v; break; case 3: g.F3 = v; break; case 4: g.F4 = v; break; case 5: g.F5 = v; break; case 6: g.F6 = v; break; default: g.F7 = v; break; }
            if (g.Src != null && f + 1 < g.Src.Length) { g.Src = (string[])g.Src.Clone(); g.Src[f + 1] = v.ToString("0.######", CI); }
        }

        static readonly string[] Nomi = { "", "X1", "Y1", "Z1", "X2", "Y2", "Z2", "raggio" };

        /// <summary>Arrotonda il modello. soloTag = null: tutti i fili GW; altrimenti solo quelli (piu' i punti in comune con loro).</summary>
        public static Esito Arrotonda(NecModel m0, bool diametri, ICollection<int> soloTag)
        {
            Esito e = new Esito();
            NecModel m = m0.Clone();
            e.Modello = m;
            // scala della scheda GS (coordinate scritte in altre unita'): si arrotonda la misura vera in metri
            double sc = 1;
            foreach (GeomCard g in m.Geometry) if (g.Id == "GS" && g.F1 > 0) sc = g.F1;
            Func<double, double> mm = v => Math.Round(v * sc * 1000, MidpointRounding.AwayFromZero) / 1000 / sc;

            // punti dei fili scelti, arrotondati: valgono anche per i fili non scelti che li toccano
            List<double[]> punti = new List<double[]>();
            Func<GeomCard, bool> scelto = g => g.Id == "GW" && (soloTag == null || soloTag.Contains(g.I1));
            foreach (GeomCard g in m.Geometry)
                if (scelto(g)) { punti.Add(new[] { g.F1, g.F2, g.F3 }); punti.Add(new[] { g.F4, g.F5, g.F6 }); }
            Func<double, double, double, bool> toccaScelto = (x, y, z) =>
            {
                foreach (double[] p in punti) if (Math.Abs(p[0] - x) < 1e-7 && Math.Abs(p[1] - y) < 1e-7 && Math.Abs(p[2] - z) < 1e-7) return true;
                return false;
            };

            double lungPrima = 0, lungDopo = 0;
            HashSet<int> filiToccati = new HashSet<int>();
            foreach (GeomCard g in m.Geometry)
            {
                if (g.Id != "GW") continue;
                bool sel = scelto(g);
                bool[] estremo = { sel || toccaScelto(g.F1, g.F2, g.F3), sel || toccaScelto(g.F4, g.F5, g.F6) };
                if (!estremo[0] && !estremo[1]) continue;
                double l0 = Math.Sqrt(Math.Pow(g.F4 - g.F1, 2) + Math.Pow(g.F5 - g.F2, 2) + Math.Pow(g.F6 - g.F3, 2)) * sc;
                double spost = 0;
                for (int f = 1; f <= 6; f++)
                {
                    if (!estremo[f <= 3 ? 0 : 1]) continue;
                    if (!Numero(g, f + 1)) { e.Formule++; continue; }
                    double v = Get(g, f), r = mm(v);
                    if (Math.Abs(r - v) * sc < 1e-9) continue;
                    Set(g, f, r);
                    double d = (r - v) * sc * 1000;
                    spost = Math.Max(spost, Math.Abs(d));
                    e.Cambi.Add(new Cambio { Tag = g.I1, Campo = Nomi[f], Prima = v, Dopo = r });
                    filiToccati.Add(g.I1);
                }
                // diametro: solo per i fili scelti, raggio positivo scritto come numero (il raggio negativo e' una riga delle Rastremazioni)
                if (diametri && sel && g.F7 > 0 && Numero(g, 8))
                {
                    string nome;
                    double dmm = g.F7 * sc * 2000, dc = DiametroCommerciale(dmm, out nome);
                    if (!double.IsNaN(dc) && Math.Abs(dc - dmm) > 1e-6)
                    {
                        double r = dc / 2000 / sc;
                        e.Cambi.Add(new Cambio { Tag = g.I1, Campo = "diametro (mm)", Prima = dmm, Dopo = dc, Diametro = true, Nota = nome });
                        Set(g, 7, r);
                        filiToccati.Add(g.I1);
                    }
                }
                double l1 = Math.Sqrt(Math.Pow(g.F4 - g.F1, 2) + Math.Pow(g.F5 - g.F2, 2) + Math.Pow(g.F6 - g.F3, 2)) * sc;
                lungPrima += l0; lungDopo += l1;
                if (spost > 0)
                {
                    double s0; e.SpostamentoPerFilo.TryGetValue(g.I1, out s0);
                    e.SpostamentoPerFilo[g.I1] = Math.Max(s0, spost);
                    e.SpostamentoMaxMm = Math.Max(e.SpostamentoMaxMm, spost);
                }
            }
            e.Fili = filiToccati.Count;
            e.DeltaLunghezzaMm = (lungDopo - lungPrima) * 1000;
            return e;
        }

        /// <summary>Lista di taglio: un filo per riga, lunghezza al millimetro e diametro.</summary>
        public static string ListaTaglio(NecModel m)
        {
            double sc = 1;
            foreach (GeomCard g in m.Geometry) if (g.Id == "GS" && g.F1 > 0) sc = g.F1;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("filo\tlunghezza (mm)\tdiametro (mm)\tX1 (mm)\tX2 (mm)");
            foreach (GeomCard g in m.Geometry)
            {
                if (g.Id != "GW") continue;
                double l = Math.Sqrt(Math.Pow(g.F4 - g.F1, 2) + Math.Pow(g.F5 - g.F2, 2) + Math.Pow(g.F6 - g.F3, 2)) * sc * 1000;
                string d = g.F7 > 0 ? (g.F7 * sc * 2000).ToString("0.###", CI) : "riga " + (-g.F7).ToString("0", CI) + " delle Rastremazioni";
                sb.AppendLine(g.I1.ToString(CI) + "\t" + Math.Round(l).ToString("0", CI) + "\t" + d + "\t" + (g.F1 * sc * 1000).ToString("0", CI) + "\t" + (g.F4 * sc * 1000).ToString("0", CI));
            }
            return sb.ToString();
        }
    }
}
