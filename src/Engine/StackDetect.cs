// YA-NEC - riconoscimento degli stack: copie perfettamente identiche della stessa antenna, solo spostate.
// I file degli stack (MMANA, AutoEZ/EZNEC, NEC) elencano i fili di ogni copia uno dopo l'altro: si prova ogni divisione
// dei fili in k gruppi uguali (k >= 2, almeno 2 fili per gruppo) e si controlla che ogni gruppo sia il primo spostato
// di un vettore fisso (stesso raggio, stessi segmenti, estremi anche scambiati). Vince il k piu' grande.
// Poi le copie si dispongono in righe (quote) e colonne (posizione orizzontale) e si misurano le spaziature.
// Correzione approvata (scheda Stack, 12/11/2026): DL6WU D = lambda / (2 sin(apertura/2)) con le aperture
// dell'antenna base in spazio libero: stack verticale -> apertura nel piano verticale, affiancato -> nel piano orizzontale.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public class StackInfo
    {
        public int Copies;              // numero di antenne
        public int WiresPerCopy;        // fili dell'antenna base (i primi della lista)
        public List<int> CardIndex = new List<int>();   // indici delle schede GW nella lista Geometry (in ordine)
        public double[][] Offset;       // spostamento di ogni copia rispetto alla prima (x, y, z)
        public int[] Row, Col;          // riga (0 = in basso) e colonna di ogni copia
        public int Rows, Cols;
        public double SpacingV = double.NaN, SpacingH = double.NaN;   // spaziature misurate (m), NaN se una sola riga/colonna
        public double HorizDirX = 1, HorizDirY = 0;    // direzione orizzontale delle colonne
        public double BaseHeight;       // quota media della riga in basso (m)
        public bool HorizontalPol = true;

        public string Describe()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            string s = Copies + " antenne identiche (" + Rows + " x " + Cols + ") da " + WiresPerCopy + " fili ciascuna";
            if (!double.IsNaN(SpacingV)) s += string.Format(ci, ", spaziatura verticale {0:0.000} m", SpacingV);
            if (!double.IsNaN(SpacingH)) s += string.Format(ci, ", orizzontale {0:0.000} m", SpacingH);
            return s;
        }
    }

    public static class StackDetect
    {
        const double Tol = 1e-4;   // metri

        static bool Same(GeomCard a, GeomCard b, double dx, double dy, double dz)
        {
            if (Math.Abs(a.F7 - b.F7) > 1e-9 * Math.Max(1, Math.Abs(a.F7))) return false;
            if (a.I2 != b.I2) return false;
            bool fwd = Math.Abs(a.F1 + dx - b.F1) < Tol && Math.Abs(a.F2 + dy - b.F2) < Tol && Math.Abs(a.F3 + dz - b.F3) < Tol &&
                       Math.Abs(a.F4 + dx - b.F4) < Tol && Math.Abs(a.F5 + dy - b.F5) < Tol && Math.Abs(a.F6 + dz - b.F6) < Tol;
            if (fwd) return true;
            return Math.Abs(a.F1 + dx - b.F4) < Tol && Math.Abs(a.F2 + dy - b.F5) < Tol && Math.Abs(a.F3 + dz - b.F6) < Tol &&
                   Math.Abs(a.F4 + dx - b.F1) < Tol && Math.Abs(a.F5 + dy - b.F2) < Tol && Math.Abs(a.F6 + dz - b.F3) < Tol;
        }

        /// <summary>null se il modello non contiene copie identiche (servono solo schede GW).</summary>
        public static StackInfo Detect(NecModel m)
        {
            if (m == null) return null;
            List<int> idx = new List<int>();
            for (int i = 0; i < m.Geometry.Count; i++)
            {
                string id = m.Geometry[i].Id;
                if (id == "GW") idx.Add(i);
                else if (id == "GE" || id == "GS") continue;
                else return null;     // GM, GR, GA, GH...: non si riconosce (le copie le fa gia' il NEC)
            }
            int n = idx.Count;
            if (n < 4) return null;
            for (int k = n / 2; k >= 2; k--)
            {
                if (n % k != 0) continue;
                int mm = n / k;
                if (mm < 2) continue;
                double[][] off = new double[k][];
                off[0] = new double[3];
                bool ok = true;
                for (int c = 1; c < k && ok; c++)
                {
                    GeomCard a0 = m.Geometry[idx[0]], b0 = m.Geometry[idx[c * mm]];
                    // spostamento dal primo filo (anche con gli estremi scambiati)
                    double[][] cand = {
                        new[] { b0.F1 - a0.F1, b0.F2 - a0.F2, b0.F3 - a0.F3 },
                        new[] { b0.F4 - a0.F1, b0.F5 - a0.F2, b0.F6 - a0.F3 } };
                    bool found = false;
                    foreach (double[] d in cand)
                    {
                        if (Math.Abs(d[0]) + Math.Abs(d[1]) + Math.Abs(d[2]) < 10 * Tol) continue;
                        bool all = true;
                        for (int i = 0; i < mm && all; i++)
                            all = Same(m.Geometry[idx[i]], m.Geometry[idx[c * mm + i]], d[0], d[1], d[2]);
                        if (all) { off[c] = d; found = true; break; }
                    }
                    ok = found;
                }
                if (!ok) continue;
                StackInfo s = new StackInfo { Copies = k, WiresPerCopy = mm, CardIndex = idx, Offset = off };
                Arrange(m, s);
                return s;
            }
            return null;
        }

        static void Arrange(NecModel m, StackInfo s)
        {
            int k = s.Copies;
            // polarizzazione: direzione del filo piu' lungo dell'antenna base
            double best = -1, vz = 0;
            double zsum = 0;
            for (int i = 0; i < s.WiresPerCopy; i++)
            {
                GeomCard g = m.Geometry[s.CardIndex[i]];
                double dx = g.F4 - g.F1, dy = g.F5 - g.F2, dz = g.F6 - g.F3;
                double l = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (l > best) { best = l; vz = l > 0 ? Math.Abs(dz) / l : 0; }
                zsum += (g.F3 + g.F6) / 2;
            }
            s.HorizontalPol = vz < 0.7;
            double zbase = zsum / s.WiresPerCopy;
            // righe: quote distinte; colonne: posizione orizzontale lungo la direzione delle spaziature orizzontali
            List<double> zs = new List<double>();
            foreach (double[] o in s.Offset) AddDistinct(zs, o[2]);
            zs.Sort();
            double hx = 0, hy = 0;
            foreach (double[] o in s.Offset)
            {
                double l = Math.Sqrt(o[0] * o[0] + o[1] * o[1]);
                if (l > 10 * Tol) { hx = o[0] / l; hy = o[1] / l; break; }
            }
            if (hx == 0 && hy == 0) hx = 1;
            s.HorizDirX = hx; s.HorizDirY = hy;
            List<double> hs = new List<double>();
            foreach (double[] o in s.Offset) AddDistinct(hs, o[0] * hx + o[1] * hy);
            hs.Sort();
            s.Rows = zs.Count; s.Cols = hs.Count;
            s.Row = new int[k]; s.Col = new int[k];
            for (int c = 0; c < k; c++)
            {
                s.Row[c] = IndexOf(zs, s.Offset[c][2]);
                s.Col[c] = IndexOf(hs, s.Offset[c][0] * hx + s.Offset[c][1] * hy);
            }
            if (zs.Count > 1) s.SpacingV = (zs[zs.Count - 1] - zs[0]) / (zs.Count - 1);
            if (hs.Count > 1) s.SpacingH = (hs[hs.Count - 1] - hs[0]) / (hs.Count - 1);
            s.BaseHeight = zbase + zs[0];
        }

        static void AddDistinct(List<double> l, double v)
        {
            foreach (double x in l) if (Math.Abs(x - v) < 1e-3) return;
            l.Add(v);
        }

        static int IndexOf(List<double> l, double v)
        {
            for (int i = 0; i < l.Count; i++) if (Math.Abs(l[i] - v) < 1e-3) return i;
            return 0;
        }

        /// <summary>Distanza DL6WU per un'apertura a -3 dB (gradi): D = lambda / (2 sin(a/2)).</summary>
        public static double Dl6wu(double lambda, double apertureDeg)
        {
            double a = apertureDeg * Math.PI / 180.0;
            if (a <= 0 || a >= Math.PI) return double.NaN;
            return lambda / (2 * Math.Sin(a / 2));
        }

        /// <summary>Modello della sola antenna base (i primi fili), in spazio libero, con le sorgenti che stanno sui suoi fili.</summary>
        public static NecModel BaseAntenna(NecModel m, StackInfo s)
        {
            NecModel b = m.Clone();
            HashSet<int> keepTags = new HashSet<int>();
            List<GeomCard> g = new List<GeomCard>();
            HashSet<int> drop = new HashSet<int>();
            for (int i = s.WiresPerCopy; i < s.CardIndex.Count; i++) drop.Add(s.CardIndex[i]);
            for (int i = 0; i < b.Geometry.Count; i++)
            {
                if (drop.Contains(i)) continue;
                g.Add(b.Geometry[i]);
                if (b.Geometry[i].Id == "GW") keepTags.Add(b.Geometry[i].I1);
            }
            b.Geometry = g;
            b.Sources = b.Sources.FindAll(x => keepTags.Contains(x.Tag));
            if (b.Sources.Count == 0 && m.Sources.Count > 0)
            {
                // sorgente solo su una copia diversa dalla prima: si porta sul filo corrispondente della base
                SourceSpec sp = m.Sources[0].Clone();
                for (int i = 0; i < s.CardIndex.Count; i++)
                    if (m.Geometry[s.CardIndex[i]].I1 == sp.Tag) { sp.Tag = m.Geometry[s.CardIndex[i % s.WiresPerCopy]].I1; break; }
                b.Sources.Add(sp);
            }
            b.Loads = b.Loads.FindAll(x => x.Tag == 0 || keepTags.Contains(x.Tag));
            b.Networks = b.Networks.FindAll(x => keepTags.Contains(x.Tag1) && keepTags.Contains(x.Tag2));
            b.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
            b.GroundFlag = 0;
            return b;
        }

        static string Num(double v) { return v.ToString("0.######", CultureInfo.InvariantCulture); }

        static void Shift(GeomCard g, double dx, double dy, double dz)
        {
            g.F1 += dx; g.F2 += dy; g.F3 += dz; g.F4 += dx; g.F5 += dy; g.F6 += dz;
            if (g.Src != null && g.Src.Length >= 8)
            {
                double[] d = { dx, dy, dz, dx, dy, dz };
                for (int k = 0; k < 6; k++)
                {
                    if (Math.Abs(d[k]) < 1e-12) continue;
                    string f = g.Src[2 + k];
                    double v;
                    if (double.TryParse(f, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) g.Src[2 + k] = Num(v + d[k]);
                    else g.Src[2 + k] = "(" + f + ")" + (d[k] >= 0 ? "+" : "-") + Num(Math.Abs(d[k]));
                }
            }
        }

        static double MinOffZ(StackInfo s)
        {
            double z = double.MaxValue;
            foreach (double[] o in s.Offset) z = Math.Min(z, o[2]);
            return z;
        }

        /// <summary>Ridispone le copie: spaziatura verticale e orizzontale (m) e quota della riga in basso (m, NaN = invariata).</summary>
        public static void Apply(NecModel m, StackInfo s, double dv, double dh, double baseHeight)
        {
            bool setV = s.Rows > 1 && !double.IsNaN(dv), setH = s.Cols > 1 && !double.IsNaN(dh);
            // la prima copia puo' non essere in basso a sinistra: gli spostamenti restano relativi a lei
            double h0 = setH ? s.Col[0] * dh : 0, z0 = setV ? s.Row[0] * dv : 0;
            double zMinOff = double.MaxValue;
            for (int c = 0; c < s.Copies; c++) zMinOff = Math.Min(zMinOff, setV ? s.Row[c] * dv - z0 : s.Offset[c][2]);
            double zCopy0 = s.BaseHeight - MinOffZ(s);       // quota media della prima copia
            double lift = double.IsNaN(baseHeight) ? 0 : baseHeight - (zCopy0 + zMinOff);
            for (int c = 0; c < s.Copies; c++)
            {
                double[] o = s.Offset[c];
                double curH = o[0] * s.HorizDirX + o[1] * s.HorizDirY;
                double newH = setH ? s.Col[c] * dh - h0 : curH;
                double newZ = setV ? s.Row[c] * dv - z0 : o[2];
                double ddx = (newH - curH) * s.HorizDirX, ddy = (newH - curH) * s.HorizDirY, ddz = newZ - o[2] + lift;
                for (int i = 0; i < s.WiresPerCopy; i++)
                    Shift(m.Geometry[s.CardIndex[c * s.WiresPerCopy + i]], ddx, ddy, ddz);
            }
        }

        /// <summary>
        /// 0.4.20 (IC8TEM 14/11/2026 16:24: dalla scheda Calcolatori > Distanze di stack DL6WU si deve poter creare lo stack): righe x
        /// colonne copie dell'antenna del modello (solo fili GW), righe in verticale a passo dv dalla quota attuale verso l'alto, colonne in
        /// orizzontale a passo dh, centrate sulla posizione attuale, nella direzione perpendicolare al lobo (azimut phiLoboDeg). Ogni copia
        /// ha tag nuovi e le sue sorgenti (stessa ampiezza e fase: antenne in fase, come con cavi uguali e un divisore), i carichi e le
        /// linee riferiti ai suoi fili. note: cosa e' stato fatto e cosa no.
        /// </summary>
        public static NecModel Crea(NecModel m0, int righe, int colonne, double dv, double dh, double phiLoboDeg, List<string> note)
        {
            if (m0 == null) throw new NecException("Nessun modello aperto.");
            righe = Math.Max(1, Math.Min(8, righe)); colonne = Math.Max(1, Math.Min(8, colonne));
            if (righe * colonne < 2) throw new NecException("Servono almeno 2 antenne (righe x colonne).");
            if (righe > 1 && (double.IsNaN(dv) || dv <= 0)) throw new NecException("Spaziatura verticale non valida.");
            if (colonne > 1 && (double.IsNaN(dh) || dh <= 0)) throw new NecException("Spaziatura orizzontale non valida.");
            foreach (GeomCard g in m0.Geometry)
                if (g.Id != "GW" && g.Id != "GE") throw new NecException("Lo stack si crea solo da modelli fatti di fili GW (scheda " + g.Id + " nel modello): converti prima la geometria in fili.");
            if (Detect(m0) != null) throw new NecException("Il modello e' gia' uno stack: le distanze si cambiano in Struttura 3D > Oggetti del modello > Stack.");
            NecModel m = m0.Clone();
            List<GeomCard> base_ = m.Geometry.FindAll(g => g.Id == "GW");
            int maxTag = 0;
            foreach (GeomCard g in base_) maxTag = Math.Max(maxTag, g.I1);
            double ph = phiLoboDeg * Math.PI / 180.0, hx = -Math.Sin(ph), hy = Math.Cos(ph);
            List<GeomCard> nuova = new List<GeomCard>();
            List<SourceSpec> src = new List<SourceSpec>();
            List<LoadSpec> car = new List<LoadSpec>();
            List<NetworkSpec> lin = new List<NetworkSpec>();
            int k = 0;
            for (int r = 0; r < righe; r++)
                for (int c = 0; c < colonne; c++, k++)
                {
                    double off = colonne > 1 ? (c - (colonne - 1) / 2.0) * dh : 0, dz = righe > 1 ? r * dv : 0;
                    double dx = off * hx, dy = off * hy;
                    int dt = k * maxTag;
                    foreach (GeomCard g in base_)
                    {
                        GeomCard q = g.Clone();
                        q.Src = g.Src != null ? (string[])g.Src.Clone() : null;
                        Shift(q, dx, dy, dz);
                        q.I1 = g.I1 + dt;
                        if (q.Src != null && q.Src.Length > 0) q.Src[0] = q.I1.ToString(CultureInfo.InvariantCulture);
                        nuova.Add(q);
                    }
                    foreach (SourceSpec sp in m0.Sources) { SourceSpec x = sp.Clone(); x.Tag = sp.Tag + dt; src.Add(x); }
                    foreach (LoadSpec ld in m0.Loads) { if (ld.Tag == 0) { if (k == 0) car.Add(ld.Clone()); continue; } LoadSpec x = ld.Clone(); x.Tag = ld.Tag + dt; car.Add(x); }
                    foreach (NetworkSpec n in m0.Networks) { NetworkSpec x = n.Clone(); x.Tag1 = n.Tag1 + dt; x.Tag2 = n.Tag2 + dt; lin.Add(x); }
                }
            List<GeomCard> resto = m.Geometry.FindAll(g => g.Id != "GW");
            m.Geometry = nuova;
            m.Geometry.AddRange(resto);
            m.Sources = src; m.Loads = car; m.Networks = lin;
            if (m0.Stubs.Count + m0.Trasformatori.Count + m0.RetiL.Count > 0 && note != null)
                note.Add("Stub, trasformatori e reti L restano solo sull'antenna base: vanno aggiunti a mano alle copie.");
            List<string> err = m.Reevaluate();
            if (err.Count > 0) throw new NecException(err[0]);
            if (note != null)
            {
                note.Add(string.Format(CultureInfo.CurrentCulture, "Stack {0} x {1} = {2} antenne: verticale {3}, orizzontale {4} (perpendicolare al lobo, azimut {5:0} gradi); sorgenti in fase.",
                    righe, colonne, righe * colonne, righe > 1 ? dv.ToString("0.000", CultureInfo.CurrentCulture) + " m" : "-", colonne > 1 ? dh.ToString("0.000", CultureInfo.CurrentCulture) + " m" : "-", phiLoboDeg));
                note.Add("Le antenne sono alimentate ognuna con la sua sorgente, uguale per ampiezza e fase: e' lo stack con cavi uguali e un divisore ideale; le impedenze delle singole antenne sono impedenze attive.");
            }
            return m;
        }
    }
}
