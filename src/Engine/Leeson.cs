// YA-NEC - correzione dei tubi telescopici (elementi a diametri a gradini), sull'idea di Leeson come in EZNEC
// ("Stepped Diameter Correction"): il NEC-2 sbaglia la risonanza degli elementi con diametri a gradini (circa -2 % sul
// riflettore 20 m dell'XR-5); per il calcolo l'elemento viene sostituito da uno di diametro uniforme, con le giunzioni
// avvicinate al centro dello stesso fattore k, ricalcolato a ogni frequenza.
//
// 0.4.8: il cilindro equivalente si calcola con l'algoritmo del libro di Leeson (Equivalente, sez. 8.4); la risonanza a linee
// (punto 1) resta solo per il controllo del 15 %. Metodo della 0.4.7 (ricavato e tarato il 13/11/2026, documentato in doc/riferimenti/leeson):
//  1. ogni lato dell'elemento e' una cascata di linee aperte in punta con l'impedenza media di Schelkunoff
//     Z = 60 (ln(4H/d) - 1) (H = mezza lunghezza, d = diametro del tubo); la risonanza fs e' dove la reattanza al centro si annulla;
//  2. l'elemento uniforme ha il diametro medio pesato sulle lunghezze dei tubi, e il fattore k = 1 - 1,075 (1 - kTL)
//     con kTL = (c/4H)/fs (1,075 tarato sulle sostituzioni di EZNEC dell'XR-5 di Force 12: 8 elementi, scarto 0,1 %);
//  3. si applica solo agli elementi diritti, simmetrici, con sorgenti, carichi e linee solo al centro, e entro il 15 %
//     della loro risonanza (come prescrive EZNEC, L. B. Cebik "Tapering to Perfection").
// Verifica (nec2c, XR-5 con la linea): ROS 2,33 / 1,91 / 3,47 / 1,71 / 1,45 a 14,1 / 18,1 / 21,2 / 24,9 / 28,5 MHz
// contro EZNEC con la sua correzione 2,30 / 1,90 / 3,45 / 1,73 / 1,45 (senza correzione 5,5 a 14,1 MHz).
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public static class Leeson
    {
        public const double Taratura = 1.075;
        /// <summary>0.4.30: scarto massimo dalla risonanza per correggere (0,15 come EZNEC secondo Cebik); le prove elemento per elemento
        /// contro EZNEC (GradiniElementi) lo alzano per vedere se EZNEC corregge anche lontano dalla risonanza.</summary>
        /// 0.4.33 (collaudo 0.4.31, fase 2L, 617 elementi contro EZNEC): EZNEC corregge anche lontano dalla risonanza, almeno fino a
        /// f/fs = 0,58 (XR-5 di fabbrica, 2el-30-40, moxon 24+28+50, 410-14H, Hg155max...: con Leeson forzato lo scarto scende per esempio
        /// dal 30,8 % al 2,9 % e dal 10,6 % all'1,9 %). Limite portato al 45 % (si corregge con f/fs tra 0,55 e 1,45).
        public static double LimiteRisonanza = 0.45;
        /// <summary>
        /// 0.5.3 (modifica A, via di IC8TEM il 15/11/2026 21:57): sotto la risonanza la correzione si applica sempre, a qualunque distanza.
        /// Collaudo 0.5.3, fase 2L (618 elementi contro EZNEC): sotto f/fs 0,55 YA-NEC non correggeva e scartava fino al 14,4 % (yi20_40b),
        /// con la correzione forzata lo scarto massimo scende al 2,8 %; nessun elemento peggiora. Sopra la risonanza resta il limite del 45 %
        /// (nessun dato oltre f/fs 1,05 nel collaudo; l'XR-5 intera a 24,9 e 28,5 MHz, con i radiatori dei 20 m non corretti, coincide con EZNEC).
        /// </summary>
        public static bool SottoSempre = true;
        /// <summary>
        /// 0.5.3 (modifica B, prova di taratura): sotto la risonanza, con s = 1 - f/fs, il fattore di lunghezza diventa 1 - (1 - k)(1 + TaraLunghezza s)
        /// e il diametro D exp(TaraDiametro s). Valori 0 = Leeson come prima. Si tarano con YA-NEC.exe --tara-gradini sui valori di EZNEC della
        /// fase 2K (Diag/TaraturaLeeson.cs); i valori scelti si scrivono qui solo dopo il risultato del collaudo.
        /// </summary>
        /// 0.5.3 (collaudo 15/11/2026 22:21-23:17, fase 2L bis su 508 elementi; scelta di IC8TEM alle 23:2x: "solo lunghezza a = 0,05"):
        /// scarto medio da EZNEC 0,407 -> 0,369 %, mediana 0,36 -> 0,29 %, oltre lo 0,5 % da 118 a 67 elementi (oltre il 2 %: da 6 a 9).
        /// 520-40H a 14,0 MHz piu' vicina a EZNEC (0,26 -> 0,14 %), XR-5 di fabbrica a 14,175 MHz 0,59 -> 0,66 % (prototipo nec2c, l440/y520_ab.py e xr_ab.py).
        public static double TaraLunghezza = 0.05, TaraDiametro = 0;
        const double C = 299.792458;

        public class Catena
        {
            public List<int> Schede = new List<int>();   // indici delle schede GW in ordine lungo l'elemento
            public double Cx, Cy, Cz, Ux, Uy, Uz;         // centro e direzione
            public List<double[]> Lato = new List<double[]>();   // dal centro verso la punta: (distanza di fine tubo, diametro)
            public string Motivo;                         // perche' non si corregge (null = correggibile)
            public double Fres;
            /// <summary>0.4.30: monopolo verticale con la base a terra (eccezione di Cebik / EZNEC): centro = base, un lato solo.</summary>
            public bool Monopolo;
        }

        static double Len(GeomCard c) { return Math.Sqrt((c.F4 - c.F1) * (c.F4 - c.F1) + (c.F5 - c.F2) * (c.F5 - c.F2) + (c.F6 - c.F3) * (c.F6 - c.F3)); }

        /// <summary>Elementi a gradini: catene di fili GW diritti, allineati e collegati, con almeno due diametri diversi.</summary>
        public static List<Catena> Trova(NecModel m)
        {
            List<Catena> l = new List<Catena>();
            List<GeomCard> g = m.Geometry;
            bool[] usato = new bool[g.Count];
            for (int s = 0; s < g.Count; s++)
            {
                if (usato[s] || g[s].Id != "GW" || g[s].HasGc || g[s].F7 <= 0) continue;
                GeomCard a = g[s];
                double L = Len(a);
                if (L <= 0) continue;
                double ux = (a.F4 - a.F1) / L, uy = (a.F5 - a.F2) / L, uz = (a.F6 - a.F3) / L;
                // raccoglie i fili allineati con estremi in comune, in entrambi i versi
                List<int> idx = new List<int> { s };
                usato[s] = true;
                bool trovato = true;
                while (trovato)
                {
                    trovato = false;
                    for (int k = 0; k < g.Count; k++)
                    {
                        if (usato[k] || g[k].Id != "GW" || g[k].HasGc || g[k].F7 <= 0) continue;
                        GeomCard b = g[k];
                        double Lb = Len(b);
                        if (Lb <= 0) continue;
                        double cos = Math.Abs(((b.F4 - b.F1) * ux + (b.F5 - b.F2) * uy + (b.F6 - b.F3) * uz) / Lb);
                        if (cos < 1 - 1e-7) continue;
                        bool tocca = false;
                        foreach (int i in idx)
                        {
                            GeomCard c = g[i];
                            double tol = 1e-5 * Math.Max(1, Math.Max(Len(c), Lb));
                            if (P(c, 0, b, 0, tol) || P(c, 0, b, 1, tol) || P(c, 1, b, 0, tol) || P(c, 1, b, 1, tol)) { tocca = true; break; }
                        }
                        if (!tocca) continue;
                        // deve stare sulla stessa retta
                        double dx = b.F1 - a.F1, dy = b.F2 - a.F2, dz = b.F3 - a.F3;
                        double t = dx * ux + dy * uy + dz * uz;
                        double off = Math.Sqrt(Math.Max(0, dx * dx + dy * dy + dz * dz - t * t));
                        if (off > 1e-5 * Math.Max(1, L)) continue;
                        idx.Add(k); usato[k] = true; trovato = true;
                    }
                }
                if (idx.Count < 2) continue;
                HashSet<double> raggi = new HashSet<double>();
                foreach (int i in idx) raggi.Add(Math.Round(g[i].F7 * 1e7));
                if (raggi.Count < 2) continue;

                // coordinate lungo la retta
                double tmin = 1e30, tmax = -1e30;
                List<double[]> tubi = new List<double[]>();   // t1, t2, diametro
                foreach (int i in idx)
                {
                    GeomCard c = g[i];
                    double t1 = (c.F1 - a.F1) * ux + (c.F2 - a.F2) * uy + (c.F3 - a.F3) * uz;
                    double t2 = (c.F4 - a.F1) * ux + (c.F5 - a.F2) * uy + (c.F6 - a.F3) * uz;
                    tubi.Add(new[] { Math.Min(t1, t2), Math.Max(t1, t2), 2 * c.F7 });
                    tmin = Math.Min(tmin, Math.Min(t1, t2)); tmax = Math.Max(tmax, Math.Max(t1, t2));
                }
                Catena ct = new Catena { Ux = ux, Uy = uy, Uz = uz };
                double tc = (tmin + tmax) / 2, H = (tmax - tmin) / 2;
                // 0.4.30 (L. B. Cebik W4RNL, "NEC Correctives", amod82, segnalato da IC8TEM il 14/11/2026 22:24): la correzione vale per i fili
                // diritti simmetrici rispetto al centro, con un'eccezione: il monopolo a terra con la sorgente vicina al suolo. Lo si tratta
                // come meta' dell'elemento (l'immagine nel terreno fa l'altra meta'): centro alla base, un lato solo dalla base alla punta.
                bool terra = m.Ground != null && m.Ground.Kind != GroundKind.FreeSpace;
                if (terra && Math.Abs(uz) > 0.9999)
                {
                    double zA = a.F3 + uz * (tmin), zB = a.F3 + uz * (tmax);
                    double tolZ = 1e-4 * Math.Max(1, 2 * H);
                    if (Math.Abs(Math.Min(zA, zB)) < tolZ && Math.Max(zA, zB) > tolZ)
                    {
                        ct.Monopolo = true;
                        tc = Math.Abs(zA) < Math.Abs(zB) ? tmin : tmax;
                        H = tmax - tmin;
                        if (tc == tmax) { ct.Ux = -ux; ct.Uy = -uy; ct.Uz = -uz; }   // t cresce dalla base verso la punta
                    }
                }
                ct.Cx = a.F1 + ux * tc; ct.Cy = a.F2 + uy * tc; ct.Cz = a.F3 + uz * tc;
                ct.Schede = idx;
                // lato positivo e negativo dal centro: devono essere uguali (elemento simmetrico)
                List<double[]> pos = new List<double[]>(), neg = new List<double[]>();
                foreach (double[] tb in tubi)
                {
                    double a1 = tb[0] - tc, a2 = tb[1] - tc;
                    if (ct.Monopolo) { double s1 = Math.Abs(a1), s2 = Math.Abs(a2); pos.Add(new[] { Math.Min(s1, s2), Math.Max(s1, s2), tb[2] }); continue; }
                    if (a2 > 1e-9) pos.Add(new[] { Math.Max(0, a1), a2, tb[2] });
                    if (a1 < -1e-9) neg.Add(new[] { Math.Max(0, -a2), -a1, tb[2] });
                }
                pos.Sort((p, q) => p[1].CompareTo(q[1]));
                neg.Sort((p, q) => p[1].CompareTo(q[1]));
                bool sim = ct.Monopolo || pos.Count == neg.Count;
                for (int i = 0; sim && !ct.Monopolo && i < pos.Count; i++)
                    if (Math.Abs(pos[i][1] - neg[i][1]) > 1e-4 * H + 1e-6 || Math.Abs(pos[i][2] - neg[i][2]) > 1e-6) sim = false;
                foreach (double[] p in pos) ct.Lato.Add(new[] { p[1], p[2] });
                if (!sim) ct.Motivo = "elemento non simmetrico";
                // sorgenti, carichi e linee: solo sul segmento centrale
                foreach (int i in idx)
                {
                    GeomCard c = g[i];
                    // piu' fili con lo stesso tag (tubi di un filo rastremato): il segmento si conta dal primo filo del tag
                    int prima = 0;
                    for (int q = 0; q < i; q++) if (g[q].Id == "GW" && g[q].I1 == c.I1) prima += g[q].I2;
                    Func<int, bool> alCentro = seg0 =>
                    {
                        if (seg0 <= 0) return true;
                        int seg = seg0 - prima;
                        if (seg < 1 || seg > c.I2) return true;      // non e' su questo filo
                        double t1 = (c.F1 - a.F1) * ux + (c.F2 - a.F2) * uy + (c.F3 - a.F3) * uz;
                        double t2 = (c.F4 - a.F1) * ux + (c.F5 - a.F2) * uy + (c.F6 - a.F3) * uz;
                        double tm = t1 + (t2 - t1) * (seg - 0.5) / c.I2;
                        // monopolo: la sorgente va sul segmento accanto al suolo (il suo centro dista mezzo segmento dalla base)
                        if (ct.Monopolo) return Math.Abs(Math.Abs(tm - tc) - Math.Abs(t2 - t1) / c.I2 * 0.5) < Math.Abs(t2 - t1) / c.I2 * 0.02;
                        return Math.Abs(tm - tc) < Math.Abs(t2 - t1) / c.I2 * 0.51;
                    };
                    foreach (SourceSpec sp in m.Sources) if (sp.Tag == c.I1 && !alCentro(sp.Seg)) ct.Motivo = "sorgente fuori dal centro";
                    foreach (LoadSpec ld in m.Loads) if (ld.Tag == c.I1 && ld.Type != 5 && !(alCentro(ld.From) && alCentro(ld.To))) ct.Motivo = "carico fuori dal centro";
                    foreach (NetworkSpec n in m.Networks)
                    {
                        if (n.Tag1 == c.I1 && !alCentro(n.Seg1)) ct.Motivo = "linea fuori dal centro";
                        if (n.Tag2 == c.I1 && !alCentro(n.Seg2)) ct.Motivo = "linea fuori dal centro";
                        // 0.4.40: una linea di fase sul segmento centrale (XR-5, HB9CV, log-periodiche) non esclude piu' l'elemento:
                        // EZNEC lo corregge (XR-5 di fabbrica a 14,175 MHz: 81,1 +j73,8 ohm prima, 61,3 +j42,8 ora, EZNEC 61,2 +j42,6).
                        // Restano esclusi gli elementi con la linea fuori dal centro (righe sopra).
                    }
                    // gamma o hairpin: altri fili che toccano l'elemento fuori dalle estremita' della catena (caso escluso n. 3)
                    for (int k = 0; k < g.Count && ct.Motivo == null; k++)
                    {
                        if (idx.Contains(k) || g[k].Id != "GW") continue;
                        GeomCard b = g[k];
                        for (int e2 = 0; e2 < 2; e2++)
                        {
                            double bx = e2 == 0 ? b.F1 : b.F4, by = e2 == 0 ? b.F2 : b.F5, bz = e2 == 0 ? b.F3 : b.F6;
                            double dx = bx - a.F1, dy = by - a.F2, dz = bz - a.F3, t = dx * ux + dy * uy + dz * uz;
                            double off = Math.Sqrt(Math.Max(0, dx * dx + dy * dy + dz * dz - t * t));
                            if (off < 1e-5 * Math.Max(1, L) && t > tmin + 1e-6 && t < tmax - 1e-6 && Math.Abs(t - tc) > 1e-4) ct.Motivo = "filo collegato fuori dal centro (gamma o hairpin)";
                            // 0.4.34 (collaudo 0.4.33, 2J: moxon 24+28+50 dal 16,8 % all'85 %, 2DELTA20 dallo 0 % al 90 % con il limite al 45 %):
                            // un filo attaccato a una punta della catena (code della Moxon, lati di un delta o di una quad) fa dell'elemento un pezzo
                            // di un conduttore piegato: EZNEC non lo corregge (Cebik: solo elementi diritti con le punte libere). Nel monopolo la base
                            // (radiali) non conta.
                            bool punta = off < 1e-5 * Math.Max(1, L) && (Math.Abs(t - tmin) <= 1e-6 * Math.Max(1, L) || Math.Abs(t - tmax) <= 1e-6 * Math.Max(1, L));
                            if (punta && !(ct.Monopolo && Math.Abs(t - tc) <= 1e-6 * Math.Max(1, L))) ct.Motivo = "filo collegato alla punta (elemento piegato: Moxon, delta, quad)";
                            // monopolo: radiali o fili attaccati alla punta o alla base non toccano il tratto in mezzo (gia' controllato sopra)
                        }
                    }
                }
                l.Add(ct);
            }
            // fili non collineari con diametri diversi collegati tra loro (quad, Moxon): caso escluso n. 4, solo avviso
            for (int s = 0; s < g.Count; s++)
            {
                if (usato[s] || g[s].Id != "GW" || g[s].F7 <= 0) continue;
                for (int k = s + 1; k < g.Count; k++)
                {
                    if (g[k].Id != "GW" || g[k].F7 <= 0 || Math.Abs(g[k].F7 - g[s].F7) < 1e-9) continue;
                    double tol = 1e-5 * Math.Max(1, Math.Max(Len(g[s]), Len(g[k])));
                    if (P(g[s], 0, g[k], 0, tol) || P(g[s], 0, g[k], 1, tol) || P(g[s], 1, g[k], 0, tol) || P(g[s], 1, g[k], 1, tol))
                    {
                        Catena ct = new Catena { Motivo = "fili non collineari con diametri diversi (quad, Moxon...)" };
                        ct.Schede.Add(s); ct.Schede.Add(k);
                        l.Add(ct);
                        break;
                    }
                }
            }
            return l;
        }

        static bool P(GeomCard c, int ec, GeomCard b, int eb, double tol)
        {
            double x1 = ec == 0 ? c.F1 : c.F4, y1 = ec == 0 ? c.F2 : c.F5, z1 = ec == 0 ? c.F3 : c.F6;
            double x2 = eb == 0 ? b.F1 : b.F4, y2 = eb == 0 ? b.F2 : b.F5, z2 = eb == 0 ? b.F3 : b.F6;
            return Math.Abs(x1 - x2) < tol && Math.Abs(y1 - y2) < tol && Math.Abs(z1 - z2) < tol;
        }

        // reattanza al centro di un lato (cascata di linee aperte in punta) - modello di Schelkunoff
        public static double Reattanza(List<double[]> lato, double f)
        {
            double lam = C / f, b = 2 * Math.PI / lam, H = lato[lato.Count - 1][0];
            double x = 0;
            bool primo = true;
            for (int i = lato.Count - 1; i >= 0; i--)
            {
                double st = i > 0 ? lato[i - 1][0] : 0, l = lato[i][0] - st;
                double Z = 60 * (Math.Log(4 * H / lato[i][1]) - 1), t = Math.Tan(b * l);
                if (primo) { x = -Z / t; primo = false; }
                else x = Z * (x + Z * t) / (Z - x * t);
            }
            return x;
        }

        /// <summary>Frequenza di risonanza del modello a linee (MHz), cercata attorno a c/4H.</summary>
        public static double Risonanza(List<double[]> lato)
        {
            double H = lato[lato.Count - 1][0], f0 = C / (4 * H);
            double a = f0 * 0.6, fa = Reattanza(lato, a);
            for (int i = 1; i <= 100; i++)
            {
                double bb = f0 * (0.6 + 0.01 * i), fb = Reattanza(lato, bb);
                if (fa < 0 && fb > 0)
                {
                    for (int k = 0; k < 60; k++)
                    {
                        double mm = (a + bb) / 2, fm = Reattanza(lato, mm);
                        if (fm < 0) { a = mm; fa = fm; } else { bb = mm; fb = fm; }
                    }
                    return (a + bb) / 2;
                }
                a = bb; fa = fb;
            }
            return double.NaN;
        }

        /// <summary>Copia del modello con gli elementi a gradini sostituiti alla frequenza f; note = cosa e' stato fatto.</summary>
        public static NecModel Applica(NecModel m, double f, List<string> note)
        {
            // fili rastremati (tabella Rastremazioni, 0.4.3): si corregge l'elemento diviso nei suoi tubi
            if (m.Rastremazioni.Count > 0 && m.Geometry.Exists(q => q.Id == "GW" && q.F7 < 0))
            {
                NecModel e = m.Clone();
                e.Geometry = OggettiExtra.Espandi(m, m.Geometry);
                e.Rastremazioni = new List<Rastremazione>();
                m = e;
            }
            List<Catena> cs = Trova(m);
            if (cs.Count == 0) return m;
            NecModel r = m.Clone();
            CultureInfo cc = CultureInfo.CurrentCulture;
            foreach (Catena c in cs)
            {
                string nome = "filo " + m.Geometry[c.Schede[0]].I1 + (c.Schede.Count > 1 ? "-" + m.Geometry[c.Schede[c.Schede.Count - 1]].I1 : "");
                if (c.Motivo != null) { if (note != null) note.Add("Leeson: " + nome + " non corretto (" + c.Motivo + ")."); continue; }
                double H0 = c.Lato[c.Lato.Count - 1][0];
                double fs = Risonanza(c.Lato);
                c.Fres = fs;
                if (double.IsNaN(fs)) { if (note != null) note.Add("Leeson: " + nome + " senza risonanza calcolabile."); continue; }
                if (f > fs * (1 + LimiteRisonanza) || (!SottoSempre && f < fs * (1 - LimiteRisonanza))) { if (note != null) note.Add("Leeson: " + nome + " lontano dalla risonanza (" + fs.ToString("0.00", cc) + " MHz, oltre il " + (LimiteRisonanza * 100).ToString("0", cc) + " %): non corretto."); continue; }
                // 0.4.8 (approvato da IC8TEM il 13/11/2026 23:50): cilindro equivalente con l'algoritmo del libro di Leeson
                // (Physical Design of Yagi Antennas, ARRL 1992, sez. 8.4, tab. 8-2 e 8-3) al posto del fattore 1,075 tarato a mano.
                // Prova sul giro EZNEC del 13/11: entro il 2 % in 51 modelli contro 37, scarto mediano 3,0 % contro 4,2 %.
                double l1, D;
                Equivalente(c.Lato, out l1, out D);
                double k = l1 / H0;
                double sotto = Math.Max(0, 1 - f / fs);
                if (sotto > 0 && (TaraLunghezza != 0 || TaraDiametro != 0)) { k = 1 - (1 - k) * (1 + TaraLunghezza * sotto); D *= Math.Exp(TaraDiametro * sotto); }
                // 0.4.40 (approvato da IC8TEM il 15/11/2026 14:24, doc/Leeson_contro_EZNEC_0.4.39.md): ogni filo dell'elemento tiene i suoi
                // segmenti e le sue giunzioni, avvicinate al centro dello stesso fattore k (per il monopolo il centro e' la base); tutti i
                // fili prendono il raggio D/2. E' cio' che fa EZNEC: sui 543 elementi a gradini del giro 0.4.39 lo scarto mediano scende
                // dal 3,86 % allo 0,32 % (entro lo 0,5 %: da 55 a 468 elementi); la 520-40H passa dal 3,6-4,0 % allo 0,09-0,19 %.
                // Prima (0.4.3-0.4.39) i segmenti venivano resi tutti uguali (collaudo 0.4.2, proc. 22): era un adattamento al
                // confronto con un elemento uniforme a mano, non alla sostituzione che EZNEC fa davvero.
                foreach (int i in c.Schede)
                {
                    GeomCard g = r.Geometry[i], g0 = m.Geometry[i];
                    double ta = T(g0.F1, g0.F2, g0.F3, c) * k, tb = T(g0.F4, g0.F5, g0.F6, c) * k;
                    g.F1 = c.Cx + c.Ux * ta; g.F2 = c.Cy + c.Uy * ta; g.F3 = c.Cz + c.Uz * ta;
                    g.F4 = c.Cx + c.Ux * tb; g.F5 = c.Cy + c.Uy * tb; g.F6 = c.Cz + c.Uz * tb;
                    g.F7 = D / 2;
                    g.Src = null;
                }
                if (note != null) note.Add("Leeson: " + nome + (c.Monopolo ? " (monopolo a terra)" : "") + " -> diametro uniforme " + (D * 1000).ToString("0.00", cc) + " mm, lunghezza x " + k.ToString("0.0000", cc) +
                    " (risonanza " + fs.ToString("0.00", cc) + " MHz).");
            }
            return r;
        }

        /// <summary>
        /// Cilindro equivalente a un elemento a gradini (Leeson 1992, sez. 8.4, da Schelkunoff): lato = (fine del tratto dal centro, diametro).
        /// Restituisce la mezza lunghezza l1 e il diametro d1 del cilindro con la stessa risonanza e la stessa pendenza della reattanza.
        /// Verifica: esempio del libro (tratti da 50" con 0,8" e 0,4") l1 = 95,70", d1 = 0,594" (tab. 8-3).
        /// </summary>
        public static void Equivalente(List<double[]> lato, out double l1, out double d1)
        {
            int n = lato.Count;
            double l = lato[n - 1][0];
            double[] xs = new double[n + 1], lna = new double[n];
            for (int i = 0; i < n; i++) { xs[i + 1] = lato[i][0]; lna[i] = Math.Log(lato[i][1] / 2); }
            double lae = 0;
            for (int i = 0; i < n; i++) lae += lna[i] * (xs[i + 1] - xs[i]);
            lae /= l;
            double AN = 0, AM = 0, ANp = 0, AMp = 0;
            for (int i = 0; i < n; i++)
            {
                double dd = lna[i] - lae, x0 = xs[i], x1 = xs[i + 1];
                double a0 = Math.PI * x0 / l, a1 = Math.PI * x1 / l;
                AN += 60 * dd * (Math.Sin(a1) - Math.Sin(a0));
                AM += -60 * dd * (Math.Cos(a1) - Math.Cos(a0));
                ANp += 120 * dd * (x1 * Math.Cos(a1) - x0 * Math.Cos(a0)) / l;
                AMp += 120 * dd * (x1 * Math.Sin(a1) - x0 * Math.Sin(a0)) / l;
            }
            double M = -21.13 + AM;
            double Ka = 120 * (Math.Log(2 * l) - lae - 1);
            double Xo = (42.545 - AN) * (1 - M / Ka);
            double Zo = Ka - 2 * M - (1 - M / Ka) * (56.93 + ANp) - (Xo / Ka) * (76.39 + AMp);
            double Xo1 = 42.545 * (1 + 21.13 / Ka);
            double r = 1 - 2 * (Xo - Xo1) / (Math.PI * Zo);       // fo'/fo
            double Zo1 = Zo * r;
            l1 = l / r;
            double Ka1 = Zo1 + 14.67 + 4253 / Ka + 68673 / (Ka * Ka);
            d1 = 4 * l1 * Math.Exp(-(Ka1 / 120 + 1));
        }

        static double T(double x, double y, double z, Catena c) { return (x - c.Cx) * c.Ux + (y - c.Cy) * c.Uy + (z - c.Cz) * c.Uz; }

        static void Scala(ref double x, ref double y, ref double z, Catena c, double k)
        {
            x = c.Cx + (x - c.Cx) * k; y = c.Cy + (y - c.Cy) * k; z = c.Cz + (z - c.Cz) * k;
        }
    }
}
