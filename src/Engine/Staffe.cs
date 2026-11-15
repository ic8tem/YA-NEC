// YA-NEC 0.4.30 - staffe (piastre) di fissaggio degli elementi al boom: compensazione e modello (richiesta di IC8TEM del 14/11/2026
// 22:31: "questa mi serve come scheda di calcolo aggiuntiva (sottolinguetta di calcolatori): la modellazione proveniente da qua viene
// aggiunta al modello a richiesta dell'utente, non cancella il modello 3d caricato esistente").
// Fonti: ARRL Antenna Book 24a ed., cap. 8 (compensazione delle staffe) e D. Leeson W6NL, Physical Design of Yagi Antennas, cap. 9:
// la piastra al centro dell'elemento si comporta come un tratto corto di diametro grande, cioe' un gradino in piu' dell'elemento, e
// alza la risonanza; si compensa allungando le punte. Diametro equivalente di una sezione rettangolare W x t: 2 x 0,2235 (W + t)
// (Balanis, gia' nel convertitore della tabella dei fili). Calcolo della compensazione con il modello a linee di Schelkunoff degli
// elementi a gradini (Leeson.Risonanza): si cerca l'allungamento delle punte che riporta la risonanza a quella senza piastra.
// Nel modello la piastra diventa un filo corto e grosso al centro dell'elemento; con la correzione di Leeson accesa (tubi a gradini)
// il NEC la tratta come gli altri gradini.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public static class Staffe
    {
        const double C = 299.792458;

        public class Elemento
        {
            public List<int> Schede = new List<int>();     // schede GW dell'elemento (in ordine lungo l'asse)
            public double Cx, Cy, Cz, Ux, Uy, Uz;           // centro e direzione
            public double H;                                // mezza lunghezza (m)
            public List<double[]> Lato = new List<double[]>();   // (fine del tubo dal centro, diametro), dal centro alla punta
            public string Nome, Motivo;                     // Motivo != null: non si puo' modificare
            public bool Sorgente;
            public double Fs0, Fs1, Delta;                  // risonanze (MHz) senza e con piastra, allungamento per lato (m)
        }

        /// <summary>Diametro equivalente (m) di una sezione rettangolare W x t (m): 2 x 0,2235 (W + t) (Balanis).</summary>
        public static double DiametroPiastra(double w, double t) { return 2 * 0.2235 * (Math.Max(0, w) + Math.Max(0, t)); }

        static double Len(GeomCard c) { return Math.Sqrt((c.F4 - c.F1) * (c.F4 - c.F1) + (c.F5 - c.F2) * (c.F5 - c.F2) + (c.F6 - c.F3) * (c.F6 - c.F3)); }

        /// <summary>Elementi diritti del modello lunghi almeno lambda/4: catene di tubi (come la correzione di Leeson) o fili singoli.</summary>
        public static List<Elemento> Trova(NecModel m, double fmhz)
        {
            List<Elemento> l = new List<Elemento>();
            double lam = C / Math.Max(0.001, fmhz);
            HashSet<int> usate = new HashSet<int>();
            foreach (Leeson.Catena c in Leeson.Trova(m))
            {
                if (c.Lato.Count == 0 || c.Monopolo) continue;
                if (c.Motivo != null && c.Motivo.StartsWith("fili non collineari", StringComparison.Ordinal)) continue;
                Elemento e = new Elemento { Cx = c.Cx, Cy = c.Cy, Cz = c.Cz, Ux = c.Ux, Uy = c.Uy, Uz = c.Uz, Lato = c.Lato, H = c.Lato[c.Lato.Count - 1][0] };
                e.Schede.AddRange(c.Schede);
                foreach (int i in c.Schede) usate.Add(i);
                if (c.Motivo == "elemento non simmetrico") e.Motivo = c.Motivo;
                if (2 * e.H >= lam / 4) l.Add(e);
            }
            for (int i = 0; i < m.Geometry.Count; i++)
            {
                GeomCard g = m.Geometry[i];
                if (usate.Contains(i) || g.Id != "GW" || g.F7 <= 0 || g.HasGc) continue;
                double L = Len(g);
                if (L < lam / 4) continue;
                Elemento e = new Elemento { Cx = (g.F1 + g.F4) / 2, Cy = (g.F2 + g.F5) / 2, Cz = (g.F3 + g.F6) / 2, Ux = (g.F4 - g.F1) / L, Uy = (g.F5 - g.F2) / L, Uz = (g.F6 - g.F3) / L, H = L / 2 };
                e.Schede.Add(i);
                e.Lato.Add(new[] { L / 2, 2 * g.F7 });
                l.Add(e);
            }
            foreach (Elemento e in l)
                foreach (SourceSpec s in m.Sources)
                    foreach (int i in e.Schede) if (m.Geometry[i].I1 == s.Tag) e.Sorgente = true;
            return l;
        }

        // lato con la piastra al centro (lunghezza lp, diametro dp) e le punte allungate di delta
        static List<double[]> ConPiastra(List<double[]> lato, double lp, double dp, double delta)
        {
            List<double[]> r = new List<double[]>();
            double mezza = lp / 2;
            r.Add(new[] { mezza, dp });
            foreach (double[] t in lato) if (t[0] > mezza + 1e-9) r.Add(new[] { t[0], t[1] });
            if (r.Count == 1) r.Add(new[] { lato[lato.Count - 1][0], lato[lato.Count - 1][1] });   // piastra lunga quanto l'elemento: resta la punta
            r[r.Count - 1][0] += delta;
            return r;
        }

        /// <summary>Compensazione di un elemento: risonanze senza e con piastra e allungamento per lato (m) che riporta la risonanza.</summary>
        public static void Compensa(Elemento e, double lp, double dp)
        {
            e.Fs0 = Leeson.Risonanza(e.Lato);
            e.Delta = 0; e.Fs1 = e.Fs0;
            double dCentro = e.Lato[0][1];
            if (dp <= dCentro || lp <= 0 || double.IsNaN(e.Fs0)) return;     // piastra piu' sottile del tubo: nessun effetto
            e.Fs1 = Leeson.Risonanza(ConPiastra(e.Lato, lp, dp, 0));
            if (double.IsNaN(e.Fs1)) return;
            double a = 0, fa = e.Fs1 - e.Fs0, b = 0.005 * e.H, fb = Leeson.Risonanza(ConPiastra(e.Lato, lp, dp, b)) - e.Fs0;
            for (int it = 0; it < 40 && Math.Abs(fb) > 1e-7 * e.Fs0; it++)
            {
                if (fb == fa) break;
                double c = b - fb * (b - a) / (fb - fa);
                c = Math.Max(-0.2 * e.H, Math.Min(0.2 * e.H, c));
                a = b; fa = fb; b = c; fb = Leeson.Risonanza(ConPiastra(e.Lato, lp, dp, b)) - e.Fs0;
                if (double.IsNaN(fb)) { b = a; break; }
            }
            e.Delta = b;
        }

        static GeomCard Gw(int tag, int ns, double[] p1, double[] p2, double r)
        {
            return new GeomCard { Id = "GW", I1 = tag, I2 = Math.Max(1, ns), F1 = p1[0], F2 = p1[1], F3 = p1[2], F4 = p2[0], F5 = p2[1], F6 = p2[2], F7 = r };
        }

        /// <summary>
        /// Copia del modello con le piastre aggiunte agli elementi scelti (e, se compensa, le punte allungate di Delta). Il resto del
        /// modello resta com'e'. Il filo dell'elemento che contiene il centro si divide in tre: tubo, piastra (filo corto e grosso con
        /// l'etichetta originale, dove restano sorgente e carichi del centro) e tubo. note: cosa e' stato fatto o perche' no.
        /// </summary>
        public static NecModel Aggiungi(NecModel m0, List<Elemento> scelti, double lp, double dp, bool compensa, List<string> note)
        {
            NecModel m = m0.Clone();
            int tagMax = 0;
            foreach (GeomCard g in m.Geometry) tagMax = Math.Max(tagMax, g.I1);
            CultureInfo cc = CultureInfo.CurrentCulture;
            List<GeomCard> nuove = new List<GeomCard>();
            Dictionary<int, GeomCard[]> sostituisci = new Dictionary<int, GeomCard[]>();
            foreach (Elemento e in scelti)
            {
                string nome = e.Nome ?? ("filo " + m.Geometry[e.Schede[0]].I1);
                if (e.Motivo != null) { note.Add(nome + ": non modificato (" + e.Motivo + ")."); continue; }
                if (dp <= e.Lato[0][1]) { note.Add(nome + ": piastra non piu' grossa del tubo centrale, nessuna modifica."); continue; }
                Func<double, double[]> P = t => new[] { e.Cx + e.Ux * t, e.Cy + e.Uy * t, e.Cz + e.Uz * t };
                Func<double, double, double, double> T = (x, y, z) => (x - e.Cx) * e.Ux + (y - e.Cy) * e.Uy + (z - e.Cz) * e.Uz;
                // filo che contiene il centro
                int ic = -1;
                foreach (int i in e.Schede)
                {
                    GeomCard g = m.Geometry[i];
                    double t1 = T(g.F1, g.F2, g.F3), t2 = T(g.F4, g.F5, g.F6);
                    if (Math.Min(t1, t2) < -lp / 2 + 1e-9 && Math.Max(t1, t2) > lp / 2 - 1e-9) ic = i;
                }
                if (ic < 0) { note.Add(nome + ": il centro non e' su un filo solo lungo almeno la piastra, nessuna modifica."); continue; }
                GeomCard gc = m.Geometry[ic];
                int tag = gc.I1;
                // sorgenti e carichi su questo filo: solo al centro
                double lc = Len(gc), sp = lc / Math.Max(1, gc.I2), ta = T(gc.F1, gc.F2, gc.F3), tb = T(gc.F4, gc.F5, gc.F6);
                bool fuori = false;
                foreach (SourceSpec s in m.Sources)
                    if (s.Tag == tag) { double tm = ta + (tb - ta) * (s.Seg - 0.5) / gc.I2; if (Math.Abs(tm) > Math.Max(sp, lp) * 0.51) fuori = true; }
                foreach (LoadSpec ld in m.Loads)
                    if (ld.Tag == tag && ld.Type != 5) { double tm = ta + (tb - ta) * (Math.Max(1, ld.From) - 0.5) / gc.I2; if (Math.Abs(tm) > Math.Max(sp, lp) * 0.51 || ld.To != ld.From) fuori = true; }
                foreach (NetworkSpec nw in m.Networks)
                    if (nw.Tag1 == tag || nw.Tag2 == tag) fuori = true;
                if (fuori) { note.Add(nome + ": sorgenti, carichi o linee fuori dal centro del filo centrale, nessuna modifica."); continue; }
                double t0 = Math.Min(ta, tb), t3 = Math.Max(ta, tb);
                int nP = Math.Max(1, (int)Math.Round(lp / sp)); if (nP % 2 == 0) nP++;
                int nA = Math.Max(1, (int)Math.Round((-lp / 2 - t0) / sp)), nB = Math.Max(1, (int)Math.Round((t3 - lp / 2) / sp));
                List<GeomCard> pezzi = new List<GeomCard>();
                int tagA = ++tagMax, tagB = ++tagMax;
                if (-lp / 2 - t0 > 1e-6) pezzi.Add(Gw(tagA, nA, P(t0), P(-lp / 2), gc.F7));
                pezzi.Add(Gw(tag, nP, P(-lp / 2), P(lp / 2), dp / 2));
                if (t3 - lp / 2 > 1e-6) pezzi.Add(Gw(tagB, nB, P(lp / 2), P(t3), gc.F7));
                sostituisci[ic] = pezzi.ToArray();
                foreach (SourceSpec s in m.Sources) if (s.Tag == tag) s.Seg = (nP + 1) / 2;
                List<LoadSpec> extra = new List<LoadSpec>();
                foreach (LoadSpec ld in m.Loads)
                {
                    if (ld.Tag != tag) continue;
                    if (ld.Type == 5) { foreach (int t in new[] { tagA, tagB }) { LoadSpec c2 = ld.Clone(); c2.Tag = t; extra.Add(c2); } }
                    else { ld.From = ld.To = (nP + 1) / 2; }
                }
                m.Loads.AddRange(extra);
                // punte allungate di Delta (filo piu' esterno da ogni parte, lungo l'asse)
                if (compensa && Math.Abs(e.Delta) > 1e-6)
                {
                    foreach (int i in e.Schede)
                    {
                        GeomCard[] lst = sostituisci.ContainsKey(i) ? sostituisci[i] : new[] { m.Geometry[i] };
                        foreach (GeomCard g in lst)
                        {
                            double t1 = T(g.F1, g.F2, g.F3), t2 = T(g.F4, g.F5, g.F6);
                            if (Math.Abs(Math.Abs(t1) - e.H) < 1e-6) { double[] q = P(t1 + Math.Sign(t1) * e.Delta); g.F1 = q[0]; g.F2 = q[1]; g.F3 = q[2]; }
                            if (Math.Abs(Math.Abs(t2) - e.H) < 1e-6) { double[] q = P(t2 + Math.Sign(t2) * e.Delta); g.F4 = q[0]; g.F5 = q[1]; g.F6 = q[2]; }
                            g.Src = null;
                        }
                    }
                }
                note.Add(nome + ": piastra " + (lp * 1000).ToString("0", cc) + " mm, diametro equivalente " + (dp * 1000).ToString("0.0", cc) + " mm" +
                    (compensa ? ", punte allungate di " + (e.Delta * 1000).ToString("0.0", cc) + " mm per lato" : "") + ".");
            }
            List<GeomCard> geo = new List<GeomCard>();
            for (int i = 0; i < m.Geometry.Count; i++)
                if (sostituisci.ContainsKey(i)) geo.AddRange(sostituisci[i]); else geo.Add(m.Geometry[i]);
            m.Geometry = geo;
            if (!m.Comments.Exists(x => x.StartsWith("Staffe:", StringComparison.Ordinal))) m.Comments.Add("Staffe: piastre di fissaggio aggiunte dal calcolatore di YA-NEC");
            return m;
        }
    }
}
