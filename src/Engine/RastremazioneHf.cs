// YA-NEC 0.4.30 - rastremazione dei tubi nei calcolatori HF (IC8TEM 14/11/2026 23:40: "devi tenere conto anche della rastremazione degli
// elementi per tutti i design hf, eccetto per i design notoriamente filari. aggiorna le linguette con i calcoli con le rastremazioni").
// Una schema di tubi telescopici scritto come "diametro x lunghezza" per lato, dal centro (o dalla base) verso la punta, separati da ";":
// "35x1; 30x1; 25x1; 20x1; 16x1; 12" = tubo centrale da 35 mm lungo 1 m per lato (2 m in tutto), poi 30, 25, 20, 16 mm da 1 m e la punta
// da 12 mm fino alla fine. Si usa la tabella Rastremazioni gia' presente (come il Taper wire set di MMANA, 0.4.3): il filo prende il
// raggio -N e nel calcolo diventa un tubo per tratto con lo stesso tag e gli stessi segmenti; la correzione di Leeson (come la Stepped
// Diameter Correction di EZNEC) si accende sul modello. Se l'elemento e' piu' corto della schema si usano solo i primi tubi.
// Regola per i modelli dei calcolatori: si rastremano solo i fili col raggio piu' grande del modello (i tubi), lunghi almeno 1 m; un
// filo con un estremo libero e l'altro collegato si rastrema da quello collegato (verticale dalla base, braccio dal centro); un filo
// isolato con la sorgente o parassita si rastrema dal centro ("<>"); un filo collegato ai due estremi resta com'e'.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public static class RastremazioneHf
    {
        public const string Predefinita = "35x1; 30x1; 25x1; 20x1; 16x1; 12";
        static readonly CultureInfo IT = CultureInfo.GetCultureInfo("it-IT"), IV = CultureInfo.InvariantCulture;

        static double Num(string s)
        {
            double v;
            s = s.Trim();
            if (double.TryParse(s.Replace(',', '.'), NumberStyles.Float, IV, out v)) return v;
            throw new NecException("rastremazione: \"" + s + "\" non e' un numero");
        }

        /// <summary>Tubi per lato: {diametro m, lunghezza m (negativa = resto)}, dal centro alla punta.</summary>
        public static List<double[]> Leggi(string schema)
        {
            List<double[]> r = new List<double[]>();
            string[] p = (schema ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string q0 in p)
            {
                string q = q0.Trim().ToLowerInvariant();
                if (q.Length == 0) continue;
                int x = q.IndexOf('x');
                if (x < 0) r.Add(new[] { Num(q) / 1000, -1.0 });
                else r.Add(new[] { Num(q.Substring(0, x)) / 1000, Num(q.Substring(x + 1)) });
            }
            if (r.Count == 0) throw new NecException("rastremazione vuota");
            if (r[r.Count - 1][1] > 0) r.Add(new[] { r[r.Count - 1][0], -1.0 });
            for (int i = 0; i < r.Count - 1; i++) if (r[i][1] < 0) throw new NecException("rastremazione: solo l'ultimo tubo puo' essere senza lunghezza (la punta)");
            return r;
        }

        /// <summary>Riga della tabella Rastremazioni: tipo 0 "<>" dal centro (il primo tubo vale per due lati), 1 ">" da E1, 2 "<" da E2.</summary>
        public static Rastremazione Riga(string schema, int n, int tipo)
        {
            Rastremazione r = new Rastremazione { N = n, Tipo = tipo };
            List<double[]> t = Leggi(schema);
            for (int i = 0; i < t.Count; i++)
            {
                r.R.Add(t[i][0] / 2);
                r.L.Add(t[i][1] < 0 ? -1 : (i == 0 && tipo == 0 ? 2 * t[i][1] : t[i][1]));
            }
            return r;
        }

        public static double DiametroPunta(string schema) { List<double[]> t = Leggi(schema); return t[t.Count - 1][0]; }
        public static double DiametroCentro(string schema) { return Leggi(schema)[0][0]; }

        public static string Descrivi(string schema)
        {
            List<double[]> t = Leggi(schema);
            List<string> s = new List<string>();
            foreach (double[] q in t) s.Add((q[0] * 1000).ToString("0.#", IT) + " mm" + (q[1] < 0 ? " fino alla punta" : " x " + q[1].ToString("0.###", IT) + " m"));
            return string.Join(", ", s);
        }

        static bool Uguali(double ax, double ay, double az, double bx, double by, double bz, double tol)
        {
            return Math.Abs(ax - bx) < tol && Math.Abs(ay - by) < tol && Math.Abs(az - bz) < tol;
        }

        /// <summary>Rastrema i tubi di un modello di calcolatore (regola in testa al file). Restituisce quanti fili ha rastremato.</summary>
        public static int Applica(NecModel m, string schema, List<string> note)
        {
            double rmax = 0;
            foreach (GeomCard g in m.Geometry) if (g.Id == "GW" && g.F7 > 0) rmax = Math.Max(rmax, g.F7);
            if (rmax <= 0) return 0;
            int nBase = 1;
            foreach (Rastremazione q in m.Rastremazioni) nBase = Math.Max(nBase, q.N + 1);
            int n0 = nBase, n1 = nBase + 1, n2 = nBase + 2;
            bool[] usati = new bool[3];
            int conta = 0;
            foreach (GeomCard g in m.Geometry)
            {
                if (g.Id != "GW" || g.F7 <= 0 || g.F7 < rmax * 0.999) continue;
                double len = Math.Sqrt((g.F4 - g.F1) * (g.F4 - g.F1) + (g.F5 - g.F2) * (g.F5 - g.F2) + (g.F6 - g.F3) * (g.F6 - g.F3));
                if (len < 1.0) continue;
                double tol = Math.Max(1e-4, len * 1e-5);
                bool c1 = m.Ground.Kind != GroundKind.FreeSpace && Math.Abs(g.F3) < tol, c2 = m.Ground.Kind != GroundKind.FreeSpace && Math.Abs(g.F6) < tol;
                foreach (GeomCard h in m.Geometry)
                {
                    if (h == g || h.Id != "GW") continue;
                    if (Uguali(g.F1, g.F2, g.F3, h.F1, h.F2, h.F3, tol) || Uguali(g.F1, g.F2, g.F3, h.F4, h.F5, h.F6, tol)) c1 = true;
                    if (Uguali(g.F4, g.F5, g.F6, h.F1, h.F2, h.F3, tol) || Uguali(g.F4, g.F5, g.F6, h.F4, h.F5, h.F6, tol)) c2 = true;
                }
                if (c1 && c2) continue;
                int tipo = c1 ? 1 : c2 ? 2 : 0;
                g.F7 = -(tipo == 0 ? n0 : tipo == 1 ? n1 : n2);
                g.Src = null;
                usati[tipo] = true;
                conta++;
            }
            if (usati[0]) m.Rastremazioni.Add(Riga(schema, n0, 0));
            if (usati[1]) m.Rastremazioni.Add(Riga(schema, n1, 1));
            if (usati[2]) m.Rastremazioni.Add(Riga(schema, n2, 2));
            if (conta > 0)
            {
                m.Leeson = true;
                if (note != null) note.Add("Tubi rastremati (" + conta + " fili): " + Descrivi(schema) + "; correzione di Leeson accesa.");
            }
            return conta;
        }

        /// <summary>Rastrema dal centro ("<>") tutti i fili isolati (nessun estremo collegato) lunghi almeno minimo metri, qualunque sia
        /// il loro raggio: gli elementi di LPDA e Yagi. Restituisce quanti fili ha rastremato.</summary>
        public static int ApplicaIsolati(NecModel m, string schema, double minimo, List<string> note)
        {
            int n = 1;
            foreach (Rastremazione q in m.Rastremazioni) n = Math.Max(n, q.N + 1);
            int conta = 0;
            foreach (GeomCard g in m.Geometry)
            {
                if (g.Id != "GW" || g.F7 <= 0) continue;
                double len = Math.Sqrt((g.F4 - g.F1) * (g.F4 - g.F1) + (g.F5 - g.F2) * (g.F5 - g.F2) + (g.F6 - g.F3) * (g.F6 - g.F3));
                if (len < minimo) continue;
                double tol = Math.Max(1e-4, len * 1e-5);
                bool coll = false;
                foreach (GeomCard h in m.Geometry)
                {
                    if (h == g || h.Id != "GW") continue;
                    if (Uguali(g.F1, g.F2, g.F3, h.F1, h.F2, h.F3, tol) || Uguali(g.F1, g.F2, g.F3, h.F4, h.F5, h.F6, tol) ||
                        Uguali(g.F4, g.F5, g.F6, h.F1, h.F2, h.F3, tol) || Uguali(g.F4, g.F5, g.F6, h.F4, h.F5, h.F6, tol)) { coll = true; break; }
                }
                if (coll) continue;
                g.F7 = -n; g.Src = null; conta++;
            }
            if (conta > 0)
            {
                m.Rastremazioni.Add(Riga(schema, n, 0));
                m.Leeson = true;
                if (note != null) note.Add("Tubi rastremati (" + conta + " elementi): " + Descrivi(schema) + "; correzione di Leeson accesa.");
            }
            return conta;
        }

        /// <summary>Stessa cosa sul testo .nec di un calcolatore.</summary>
        public static string ApplicaTesto(string nec, string schema, List<string> note)
        {
            NecModel m = NecModel.Parse(nec);
            return Applica(m, schema, note) > 0 ? m.ToNec() : nec;
        }
    }
}
