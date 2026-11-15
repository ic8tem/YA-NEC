// YA-NEC 0.4.3 - oggetti inseriti approvati (voci A2, A3, A4 della verifica del 13/11/2026):
//  - Rastremazioni (tubi telescopici) come il "Taper wire set" di MMANA (bozzetto approvato doc/bozzetto-rastremazioni.png): un filo GW
//    con il raggio negativo -N usa la riga N della tabella; nel calcolo il filo diventa un tubo per tratto, con lo stesso tag e lo stesso
//    numero totale di segmenti (le sorgenti e i carichi restano dove sono), segmenti distribuiti in proporzione alle lunghezze.
//    Tipo "<>" simmetrica dal centro (L1 = tratto centrale intero), ">" dall'estremo E1, "<" dall'estremo E2; "resto" = fino alla punta.
//    Nel file .nec si scrivono i tubi veri (letti da tutti i programmi) e una riga "CM YA-NEC RASTRFILO" che permette a YA-NEC di
//    rimettere insieme il filo unico alla riapertura.
//  - Trasformatori ideali tra due punti (porta 1 e porta 2, impedenze Z1 e Z2, polarita' invertita): scheda NT con l'ammettenza di un
//    trasformatore quasi ideale (reattanza magnetizzante 10^4 Z1, resistenze serie 10^-6 Z), come i trasformatori di EZNEC.
//  - Reti L: ramo serie (R, L, C in serie) tra le due porte e ramo parallelo (R, L, C in serie) sulla porta 1 o 2, ricalcolate a ogni
//    frequenza (scheda NT). Il calcolatore delle reti di sfasamento usa le formule di Lahlum (W1MK) per la rete di Lewallen
//    (ON4UN cap. 11, eq. 11-3...11-8, verificate sulle tab. 11-1...11-5) e l'ibrido di Collins (XL = Z0, XC = 2 Z0).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public class Rastremazione
    {
        public int N = 1;                    // il filo la usa con il raggio -N
        public int Tipo;                     // 0 "<>" simmetrica dal centro, 1 ">" da E1, 2 "<" da E2
        public List<double> L = new List<double>();   // lunghezze dei tratti in m (negativa = "resto": fino alla punta)
        public List<double> R = new List<double>();   // raggi in m
        public Rastremazione Clone() { Rastremazione r = (Rastremazione)MemberwiseClone(); r.L = new List<double>(L); r.R = new List<double>(R); return r; }
        public static string TipoTesto(int t) { return t == 1 ? ">" : t == 2 ? "<" : "<>"; }
    }

    public class Trasformatore
    {
        public int Tag1, Seg1, Tag2, Seg2;
        public double Z1 = 50, Z2 = 200;
        public bool Inv;
        public Trasformatore Clone() { return (Trasformatore)MemberwiseClone(); }
    }

    /// <summary>0.4.7 (IC8TEM 13/11/2026 18:25): stub (linea in corto o aperta) in parallelo su un segmento, senza filo fittizio:
    /// diventa l'ammettenza in parallelo all'estremo della linea TL che parte da quel segmento, ricalcolata a ogni frequenza.
    /// Corto: Y = -j / (Z0 tan(beta l)); aperto: Y = j tan(beta l) / Z0; beta = 2 pi f / (c VF).</summary>
    public class Stub
    {
        public int Tag, Seg;
        public double Z0 = 300, LenM, Vf = 1;
        public bool Corto = true;
        public Stub Clone() { return (Stub)MemberwiseClone(); }
        public System.Numerics.Complex Y(double fMHz)
        {
            double b = 2 * Math.PI * fMHz / (299.792458 * Math.Max(0.01, Vf));
            double t = Math.Tan(b * LenM);
            if (Corto) return Math.Abs(t) < 1e-12 ? new System.Numerics.Complex(0, -1e6) : new System.Numerics.Complex(0, -1 / (Z0 * t));
            return new System.Numerics.Complex(0, t / Z0);
        }
        public static string Riga(Stub s)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "YA-NEC STUB {0} {1} {2:R} {3:R} {4:R} {5}", s.Tag, s.Seg, s.Z0, s.LenM, s.Vf, s.Corto ? "corto" : "aperto");
        }
        public static Stub Leggi(string t)
        {
            string[] f = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (f.Length < 4) return null;
            System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
            Stub s = new Stub { Tag = int.Parse(f[0], ci), Seg = int.Parse(f[1], ci), Z0 = double.Parse(f[2], ci), LenM = double.Parse(f[3], ci) };
            if (f.Length > 4) s.Vf = double.Parse(f[4], ci);
            if (f.Length > 5) s.Corto = !f[5].StartsWith("ap", StringComparison.OrdinalIgnoreCase);
            return s;
        }
    }

    public class ReteL
    {
        public int Tag1, Seg1, Tag2, Seg2;
        public double Rs, Ls, Cs;            // ramo serie: ohm, H, F (0 = assente; C 0 = corto)
        public double Rp, Lp, Cp;            // ramo parallelo
        public int LatoParallelo = 2;        // 1 = sulla porta 1, 2 = sulla porta 2
        public ReteL Clone() { return (ReteL)MemberwiseClone(); }
    }

    public static class OggettiExtra
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static string F(double v) { return v != 0 && Math.Abs(v) < 1e-3 ? v.ToString("G9", CI) : v.ToString("0.#########", CI); }

        // ------------------------------------------------------------------------------------------------ rastremazioni
        public class Tubo { public double Da, A, Raggio; public int Seg; }

        /// <summary>Tubi di un filo lungo "lung" con la rastremazione r (posizioni dall'estremo E1). note: avvisi (filo piu' corto o piu' lungo).</summary>
        public static List<Tubo> Tubi(Rastremazione r, double lung, int segTot, List<string> note, string nome)
        {
            List<double[]> lato = new List<double[]>();   // lunghezza, raggio dal punto di partenza verso la punta
            double disp = r.Tipo == 0 ? lung / 2 : lung;
            double usato = 0;
            for (int i = 0; i < r.L.Count && i < r.R.Count; i++)
            {
                double li = r.L[i];
                if (r.Tipo == 0 && i == 0) li = li < 0 ? 2 * disp : li / 2;      // L1 e' il tratto centrale intero
                if (li < 0 || usato + li > disp + 1e-9)
                {
                    if (li >= 0 && note != null && i < r.L.Count - 1) note.Add(nome + ": il filo finisce prima, dei " + r.L.Count + " tratti della rastremazione -" + r.N + " ne usa " + (i + 1) + ".");
                    lato.Add(new[] { disp - usato, r.R[i] }); usato = disp; break;
                }
                lato.Add(new[] { li, r.R[i] }); usato += li;
            }
            if (lato.Count == 0) throw new NecException(nome + ": la rastremazione -" + r.N + " non ha tratti.");
            if (usato < disp - 1e-9)
            {
                lato[lato.Count - 1][0] += disp - usato;
                if (note != null) note.Add(nome + ": il filo e' piu' lungo della rastremazione -" + r.N + ": l'ultimo tratto e' allungato di " + (disp - usato).ToString("0.###", CultureInfo.CurrentCulture) + " m.");
            }
            List<double[]> pezzi = new List<double[]>();   // dall'estremo E1 a E2: lunghezza, raggio
            if (r.Tipo == 0)
            {
                for (int i = lato.Count - 1; i >= 1; i--) pezzi.Add(lato[i]);
                pezzi.Add(new[] { 2 * lato[0][0], lato[0][1] });
                for (int i = 1; i < lato.Count; i++) pezzi.Add(lato[i]);
            }
            else if (r.Tipo == 1) pezzi.AddRange(lato);
            else for (int i = lato.Count - 1; i >= 0; i--) pezzi.Add(lato[i]);
            // segmenti: in proporzione alla lunghezza, almeno 1 per tubo, totale come il filo (simmetrici per "<>")
            int n = pezzi.Count;
            int[] seg = new int[n];
            double tot = 0;
            foreach (double[] q in pezzi) tot += q[0];
            int somma = 0;
            for (int i = 0; i < n; i++) { seg[i] = Math.Max(1, (int)Math.Round(pezzi[i][0] / tot * segTot)); somma += seg[i]; }
            if (r.Tipo == 0)
            {
                int c = n / 2;
                for (int i = 0; i < c; i++) seg[n - 1 - i] = seg[i];
                somma = 0; for (int i = 0; i < n; i++) if (i != c) somma += seg[i];
                seg[c] = segTot - somma;
                while (seg[c] < 1)
                {
                    int k = -1;
                    for (int i = 0; i < c; i++) if (seg[i] > 1 && (k < 0 || seg[i] > seg[k])) k = i;
                    if (k < 0) { seg[c] = 1; break; }
                    seg[k]--; seg[n - 1 - k]--; seg[c] += 2;
                }
            }
            else
            {
                while (somma != segTot)
                {
                    int k = 0;
                    if (somma > segTot) { for (int i = 0; i < n; i++) if (seg[i] > seg[k]) k = i; if (seg[k] <= 1) break; seg[k]--; somma--; }
                    else { double peggio = -1; for (int i = 0; i < n; i++) { double d = pezzi[i][0] / seg[i]; if (d > peggio) { peggio = d; k = i; } } seg[k]++; somma++; }
                }
            }
            int nuovo = 0; foreach (int q in seg) nuovo += q;
            if (nuovo != segTot && note != null) note.Add(nome + ": " + nuovo + " segmenti invece di " + segTot + " (almeno uno per tubo): i numeri dei segmenti del filo cambiano.");
            List<Tubo> l = new List<Tubo>();
            double x = 0;
            for (int i = 0; i < n; i++) { l.Add(new Tubo { Da = x, A = x + pezzi[i][0], Raggio = pezzi[i][1], Seg = seg[i] }); x += pezzi[i][0]; }
            return l;
        }

        static Rastremazione Trova(NecModel m, double raggio)
        {
            int n = (int)Math.Round(-raggio);
            return m.Rastremazioni.Find(q => q.N == n);
        }

        /// <summary>Le schede di geometria con i fili rastremati divisi in tubi (stesso tag). Senza rastremazioni restituisce la lista originale.</summary>
        public static List<GeomCard> Espandi(NecModel m, List<GeomCard> geo, List<string> note = null)
        {
            if (m.Rastremazioni.Count == 0 || !geo.Exists(g => g.Id == "GW" && g.F7 < 0)) return geo;
            List<GeomCard> r = new List<GeomCard>();
            foreach (GeomCard g in geo)
            {
                if (g.Id != "GW" || g.F7 >= 0) { r.Add(g); continue; }
                Rastremazione t = Trova(m, g.F7);
                if (t == null) throw new NecException("Filo con tag " + g.I1 + ": raggio " + g.F7.ToString(CI) + " ma la rastremazione -" + (int)Math.Round(-g.F7) + " non c'e'.");
                double dx = g.F4 - g.F1, dy = g.F5 - g.F2, dz = g.F6 - g.F3, len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (len <= 0) throw new NecException("Filo con tag " + g.I1 + " di lunghezza nulla.");
                foreach (Tubo tb in Tubi(t, len, Math.Max(1, g.I2), note, "Filo con tag " + g.I1))
                {
                    double a = tb.Da / len, b = tb.A / len;
                    r.Add(new GeomCard { Id = "GW", I1 = g.I1, I2 = tb.Seg, F1 = g.F1 + dx * a, F2 = g.F2 + dy * a, F3 = g.F3 + dz * a, F4 = g.F1 + dx * b, F5 = g.F2 + dy * b, F6 = g.F3 + dz * b, F7 = tb.Raggio });
                }
            }
            return r;
        }

        public static string RigaRastremazione(Rastremazione r)
        {
            StringBuilder sb = new StringBuilder("YA-NEC RASTR " + r.N + " " + Rastremazione.TipoTesto(r.Tipo));
            for (int i = 0; i < r.L.Count && i < r.R.Count; i++) sb.Append(" " + (r.L[i] < 0 ? "resto" : F(r.L[i])) + " " + F(r.R[i]));
            return sb.ToString();
        }

        public static Rastremazione LeggiRastremazione(string s)
        {
            string[] p = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 4) return null;
            Rastremazione r = new Rastremazione();
            if (!int.TryParse(p[0], NumberStyles.Integer, CI, out r.N)) return null;
            r.Tipo = p[1] == ">" ? 1 : p[1] == "<" ? 2 : 0;
            for (int i = 2; i + 1 < p.Length; i += 2)
            {
                double l, rr;
                if (string.Equals(p[i], "resto", StringComparison.OrdinalIgnoreCase)) l = -1;
                else if (!double.TryParse(p[i], NumberStyles.Float, CI, out l)) return null;
                if (!double.TryParse(p[i + 1], NumberStyles.Float, CI, out rr) || rr <= 0) return null;
                r.L.Add(l); r.R.Add(rr);
            }
            return r.L.Count > 0 ? r : null;
        }

        /// <summary>Risonanza del filo rastremato da solo (modello a linee di Schelkunoff, come la correzione Leeson): MHz.</summary>
        public static double RisonanzaLinee(Rastremazione r, double lung)
        {
            if (r.Tipo != 0) return double.NaN;
            List<Tubo> tb = Tubi(r, lung, 21, null, "");
            List<double[]> lato = new List<double[]>();
            double c = lung / 2;
            foreach (Tubo t in tb) if (t.A > c + 1e-9) lato.Add(new[] { t.A - c, 2 * t.Raggio });
            return Leeson.Risonanza(lato);
        }

        // ------------------------------------------------------------------------------------------------ trasformatori e reti L
        static void DaZ(Complex z11, Complex z12, Complex z22, NetworkSpec n)
        {
            Complex det = z11 * z22 - z12 * z12;
            Complex y11 = z22 / det, y12 = -z12 / det, y22 = z11 / det;
            n.F1 = y11.Real; n.F2 = y11.Imaginary; n.F3 = y12.Real; n.F4 = y12.Imaginary; n.F5 = y22.Real; n.F6 = y22.Imaginary;
        }

        public static NetworkSpec Nt(Trasformatore t)
        {
            // trasformatore con reattanza magnetizzante Xm sulla porta 1 e rapporto di tensione a = V2/V1 = sqrt(Z2/Z1)
            double z1 = Math.Max(1e-3, Math.Abs(t.Z1)), z2 = Math.Max(1e-3, Math.Abs(t.Z2));
            double a = Math.Sqrt(z2 / z1) * (t.Inv ? -1 : 1), xm = 1e4 * z1;
            Complex jx = new Complex(0, xm);
            NetworkSpec n = new NetworkSpec { IsTl = false, Tag1 = t.Tag1, Seg1 = t.Seg1, Tag2 = t.Tag2, Seg2 = t.Seg2 };
            DaZ(jx + 1e-6 * z1, jx * a, jx * a * a + 1e-6 * z2, n);
            return n;
        }

        public static Complex Ramo(double r, double l, double c, double fMHz)
        {
            double w = 2 * Math.PI * fMHz * 1e6;
            return new Complex(r, w * l - (c > 0 ? 1 / (w * c) : 0));
        }

        public static NetworkSpec Nt(ReteL q, double fMHz)
        {
            NetworkSpec n = new NetworkSpec { IsTl = false, Tag1 = q.Tag1, Seg1 = q.Seg1, Tag2 = q.Tag2, Seg2 = q.Seg2 };
            Complex zs = Ramo(q.Rs, q.Ls, q.Cs, fMHz), zp = Ramo(q.Rp, q.Lp, q.Cp, fMHz);
            if (Complex.Abs(zs) < 1e-6) zs = new Complex(1e-6, 0);
            bool parallelo = q.Rp != 0 || q.Lp != 0 || q.Cp != 0;
            Complex ys = 1 / zs, yp = parallelo ? 1 / (Complex.Abs(zp) < 1e-9 ? new Complex(1e-9, 0) : zp) : Complex.Zero;
            Complex y11 = ys + (q.LatoParallelo == 1 ? yp : Complex.Zero), y22 = ys + (q.LatoParallelo == 2 ? yp : Complex.Zero), y12 = -ys;
            n.F1 = y11.Real; n.F2 = y11.Imaginary; n.F3 = y12.Real; n.F4 = y12.Imaginary; n.F5 = y22.Real; n.F6 = y22.Imaginary;
            return n;
        }

        public static string RigaTrasformatore(Trasformatore t)
        {
            return string.Format(CI, "YA-NEC TRASF {0} {1} {2} {3} {4} {5} {6}", t.Tag1, t.Seg1, t.Tag2, t.Seg2, F(t.Z1), F(t.Z2), t.Inv ? 1 : 0);
        }

        public static string RigaReteL(ReteL q)
        {
            return string.Format(CI, "YA-NEC RETEL {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}", q.Tag1, q.Seg1, q.Tag2, q.Seg2, F(q.Rs), F(q.Ls), F(q.Cs), F(q.Rp), F(q.Lp), F(q.Cp), q.LatoParallelo);
        }

        public static Trasformatore LeggiTrasformatore(string s)
        {
            string[] p = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 7) return null;
            try
            {
                return new Trasformatore { Tag1 = int.Parse(p[0], CI), Seg1 = int.Parse(p[1], CI), Tag2 = int.Parse(p[2], CI), Seg2 = int.Parse(p[3], CI),
                    Z1 = double.Parse(p[4], CI), Z2 = double.Parse(p[5], CI), Inv = p[6] != "0" };
            }
            catch { return null; }
        }

        public static ReteL LeggiReteL(string s)
        {
            string[] p = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 11) return null;
            try
            {
                return new ReteL { Tag1 = int.Parse(p[0], CI), Seg1 = int.Parse(p[1], CI), Tag2 = int.Parse(p[2], CI), Seg2 = int.Parse(p[3], CI),
                    Rs = double.Parse(p[4], CI), Ls = double.Parse(p[5], CI), Cs = double.Parse(p[6], CI), Rp = double.Parse(p[7], CI), Lp = double.Parse(p[8], CI), Cp = double.Parse(p[9], CI),
                    LatoParallelo = int.Parse(p[10], CI) == 1 ? 1 : 2 };
            }
            catch { return null; }
        }

        // ------------------------------------------------------------------------------------------------ calcolatori di sfasamento
        /// <summary>Rete L di Lewallen con le formule di Lahlum (W1MK; ON4UN eq. 11-3...11-8): n elementi alimentati dalla rete, Z0 delle linee
        /// lambda/4, impedenza R + jX dell'elemento, rapporto delle correnti k e sfasamento theta (gradi, negativo = in ritardo).
        /// Restituisce Xs (ramo serie) e Xp (ramo parallelo) in ohm.</summary>
        public static void Lahlum(int n, double z0, double r, double x, double k, double theta, out double xs, out double xp)
        {
            double t = theta * Math.PI / 180;
            xs = -Math.Sin(t) * z0 * z0 / (n * k * r);
            xp = xs / (n * x * xs / (z0 * z0) - 1 + Math.Cos(t) / k);
        }

        /// <summary>Componente da una reattanza: "x,x uH" o "x pF".</summary>
        public static string Componente(double x, double fMHz)
        {
            double w = 2 * Math.PI * fMHz * 1e6;
            CultureInfo cc = CultureInfo.CurrentCulture;
            return x > 0 ? (x / w * 1e6).ToString("0.00", cc) + " uH" : (-1 / (w * x) * 1e12).ToString("0.0", cc) + " pF";
        }
    }
}
