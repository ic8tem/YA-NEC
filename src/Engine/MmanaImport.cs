// YA-NEC - importazione dei modelli MMANA (.maa) con risegmentazione per il NEC2.
// Regole approvate da IC8TEM (12/11/2026): la rastremazione di MMANA (MININEC) non viene copiata;
//  1) segmento massimo lambda/40 (regolabile) alla frequenza del modello;
//  2) lunghezza del segmento / raggio >= 4, altrimenti avviso e meno segmenti;
//  3) numero dispari di segmenti sul filo alimentato (la sorgente cade al centro di un segmento);
//  4) alle giunzioni segmenti adiacenti di lunghezza simile (rapporto massimo 1:2);
//  5) i parametri di segmentazione originali di MMANA restano annotati nel modello.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;

namespace YaNec.Engine
{
    public class MmanaWireInfo
    {
        public int Index;            // 1 = primo filo
        public double X1, Y1, Z1, X2, Y2, Z2, Radius;
        public int MmanaSeg;
        public int NecSeg;
        public bool Fed;
        public double Length { get { double dx = X2 - X1, dy = Y2 - Y1, dz = Z2 - Z1; return Math.Sqrt(dx * dx + dy * dy + dz * dz); } }
        public double SegLength { get { return NecSeg > 0 ? Length / NecSeg : 0; } }
    }

    public static class MmanaImport
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        public const string ImportNote = "Modello MMANA (MININEC) risegmentato per NEC2: impedenza e guadagno possono differire leggermente dall'originale. Eseguire il test di convergenza.";

        public static NecModel Load(string path, double segPerLambda, out List<MmanaWireInfo> wires)
        {
            string y = YanecFile.Sostituto(path);           // 0.4.30: .yanec
            if (y != null) return YanecFile.Carica(y, segPerLambda, out wires);
            NecModel m = Parse(File.ReadAllLines(path), segPerLambda, out wires);
            m.FileName = path;
            return m;
        }

        static string[] Split(string line)
        {
            string[] p = line.Split(new[] { ',' }, StringSplitOptions.None);
            for (int i = 0; i < p.Length; i++) p[i] = p[i].Trim();
            return p;
        }

        static double D(string s)
        {
            double v;
            if (double.TryParse(s, NumberStyles.Float, CI, out v)) return v;
            throw new NecException("Valore numerico non valido nel file MMANA: '" + s + "'");
        }

        static int I(string s)
        {
            double v = D(s);
            return (int)Math.Round(v);
        }

        static readonly Regex WireRef = new Regex(@"^w(\d+)([bce])(\d*)$", RegexOptions.IgnoreCase);

        public static NecModel Parse(string[] lines, double segPerLambda, out List<MmanaWireInfo> wires)
        {
            NecModel m = new NecModel();
            m.SourceFormat = "MMANA";
            wires = new List<MmanaWireInfo>();
            if (segPerLambda < 5) segPerLambda = 5;
            List<string> ls = new List<string>();
            foreach (string l in lines) ls.Add(l.TrimEnd());
            int k = 0;
            // nome, separatore "*", frequenza
            string name = ls.Count > 0 ? ls[0].Trim() : "";
            if (name.Length > 0) m.Comments.Add(name);
            k = 1;
            while (k < ls.Count && ls[k].Trim() != "*" && !ls[k].StartsWith("***")) k++;
            if (k < ls.Count && ls[k].Trim() == "*") k++;
            while (k < ls.Count && ls[k].Trim().Length == 0) k++;
            if (k >= ls.Count) throw new NecException("File MMANA incompleto: manca la frequenza.");
            double fmhz = D(Split(ls[k])[0]);
            if (fmhz <= 0) throw new NecException("Frequenza non valida nel file MMANA.");
            m.FreqMHz = fmhz;
            m.FreqCount = 1;

            Dictionary<string, int> sec = new Dictionary<string, int>();
            for (int i = 0; i < ls.Count; i++)
                if (ls[i].StartsWith("*") && ls[i].Trim().Length > 1 && !sec.ContainsKey(ls[i].Trim().ToLowerInvariant())) sec[ls[i].Trim().ToLowerInvariant()] = i;
            // 0.4.5: le sezioni si trovano anche dalla posizione (file MMANA con le intestazioni in cirillico, "Wires" senza asterischi,
            // "*G/H/M/R/AzEl/X*" con un asterisco solo): dopo la frequenza vengono fili, sorgenti, carichi, segmentazione, terreno
            Func<int, bool> intest = i => i >= 0 && i < ls.Count && ls[i].Trim().Length > 0 && !char.IsDigit(ls[i].Trim()[0]) && ls[i].Trim()[0] != '-' && ls[i].Trim()[0] != '.' &&
                                          !(ls[i].Trim().Length > 1 && (ls[i].Trim()[0] == 'w' || ls[i].Trim()[0] == 'W') && char.IsDigit(ls[i].Trim()[1]));
            Func<string, int> find = key =>
            {
                foreach (KeyValuePair<string, int> kv in sec) if (kv.Key.Contains(key)) return kv.Value;
                return -1;
            };

            // ---- fili ----
            int iw = intest(k + 1) ? k + 1 : find("wires");
            if (iw < 0) throw new NecException("File MMANA senza la sezione ***Wires***.");
            int nw = I(Split(ls[iw + 1])[0]);
            List<string[]> raw = new List<string[]>();
            for (int i = 0; i < nw; i++) raw.Add(Split(ls[iw + 2 + i]));
            double[,] p = new double[nw, 6];
            bool[,] resolved = new bool[nw, 2];
            for (int i = 0; i < nw; i++)
            {
                string[] f = raw[i];
                if (f.Length < 8) throw new NecException("Filo " + (i + 1) + " del file MMANA incompleto.");
                MmanaWireInfo w = new MmanaWireInfo();
                w.Index = i + 1;
                double r = D(f[6]);
                if (r <= 0)
                {
                    m.Warnings.Add("Filo " + (i + 1) + ": raggio " + f[6] + " (diametri a gradini di MMANA) non ancora supportato: usato 1 mm.");
                    r = 0.001;
                }
                w.Radius = r;
                w.MmanaSeg = I(f[7]);
                wires.Add(w);
            }
            // coordinate, anche simboliche (wNb / wNe = inizio / fine del filo N)
            for (int pass = 0; pass < nw + 2; pass++)
            {
                bool all = true;
                for (int i = 0; i < nw; i++)
                    for (int e = 0; e < 2; e++)
                    {
                        if (resolved[i, e]) continue;
                        string[] f = raw[i];
                        string sx = f[e * 3];
                        Match mt = WireRef.Match(sx);
                        if (mt.Success)
                        {
                            int wn = int.Parse(mt.Groups[1].Value, CI) - 1;
                            char be = char.ToLowerInvariant(mt.Groups[2].Value[0]);
                            if (wn < 0 || wn >= nw) throw new NecException("Filo " + (i + 1) + ": riferimento a un filo inesistente (" + sx + ").");
                            if (be == 'c') throw new NecException("Filo " + (i + 1) + ": riferimento al centro di un filo (" + sx + ") non supportato.");
                            int ee = be == 'b' ? 0 : 1;
                            if (!resolved[wn, ee]) { all = false; continue; }
                            p[i, e * 3] = p[wn, ee * 3]; p[i, e * 3 + 1] = p[wn, ee * 3 + 1]; p[i, e * 3 + 2] = p[wn, ee * 3 + 2];
                        }
                        else
                        {
                            p[i, e * 3] = D(f[e * 3]); p[i, e * 3 + 1] = D(f[e * 3 + 1]); p[i, e * 3 + 2] = D(f[e * 3 + 2]);
                        }
                        resolved[i, e] = true;
                    }
                if (all) break;
            }
            for (int i = 0; i < nw; i++)
            {
                if (!resolved[i, 0] || !resolved[i, 1]) throw new NecException("Filo " + (i + 1) + ": riferimenti circolari tra i fili.");
                MmanaWireInfo w = wires[i];
                w.X1 = p[i, 0]; w.Y1 = p[i, 1]; w.Z1 = p[i, 2]; w.X2 = p[i, 3]; w.Y2 = p[i, 4]; w.Z2 = p[i, 5];
                if (w.Length <= 1e-9) throw new NecException("Filo " + (i + 1) + " di lunghezza nulla.");
            }

            // ---- sorgenti ----
            List<Tuple<int, char, Complex>> srcs = new List<Tuple<int, char, Complex>>();
            int isrc = intest(iw + 2 + nw) ? iw + 2 + nw : find("source");
            int nsLette = 0;
            if (isrc >= 0)
            {
                int ns = I(Split(ls[isrc + 1])[0]);
                nsLette = ns;
                for (int i = 0; i < ns; i++)
                {
                    string[] f = Split(ls[isrc + 2 + i]);
                    Match mt = WireRef.Match(f[0]);
                    if (!mt.Success) throw new NecException("Posizione della sorgente non riconosciuta: " + f[0]);
                    int wn = int.Parse(mt.Groups[1].Value, CI) - 1;
                    if (wn < 0 || wn >= nw) throw new NecException("Sorgente su un filo inesistente: " + f[0]);
                    if (mt.Groups[3].Value.Length > 0)
                        m.Warnings.Add("Sorgente " + f[0] + ": lo spostamento dal punto indicato non e' gestito, la sorgente va al punto " + mt.Groups[2].Value + ".");
                    double ph = f.Length > 1 ? D(f[1]) : 0, amp = f.Length > 2 ? D(f[2]) : 1;
                    if (amp == 0) amp = 1;
                    Complex v = Complex.FromPolarCoordinates(amp, ph * Math.PI / 180.0);
                    srcs.Add(Tuple.Create(wn, char.ToLowerInvariant(mt.Groups[2].Value[0]), v));
                    wires[wn].Fed = true;
                }
            }
            if (srcs.Count == 0) throw new NecException("Il file MMANA non ha sorgenti.");

            // ---- carichi ----
            // 0.4.5: carichi MMANA (help di MMANA-GAL: tipo 0 = LC con L in uH, C in pF e Q; tipo 1 = R+jX; tipo 2 = S, Laplace).
            // LC con L e C = trappola parallela; solo L = bobina; solo C = condensatore; Q 0 = senza perdite.
            List<Tuple<int, char, int, double[]>> carichi = new List<Tuple<int, char, int, double[]>>();
            int ild = isrc >= 0 && intest(isrc + 2 + nsLette) ? isrc + 2 + nsLette : find("load");
            int nlLette = 0;
            if (ild >= 0 && ild + 1 < ls.Count)
            {
                int nl = I(Split(ls[ild + 1])[0]);
                nlLette = nl;
                for (int i = 0; i < nl && ild + 2 + i < ls.Count; i++)
                {
                    string[] f = Split(ls[ild + 2 + i]);
                    Match mt = f.Length > 0 ? WireRef.Match(f[0]) : Match.Empty;
                    if (!mt.Success || f.Length < 4) { m.Warnings.Add("Carico MMANA non riconosciuto: " + ls[ild + 2 + i].Trim()); continue; }
                    int wn = int.Parse(mt.Groups[1].Value, CI) - 1;
                    if (wn < 0 || wn >= nw) { m.Warnings.Add("Carico su un filo inesistente: " + f[0]); continue; }
                    int off = mt.Groups[3].Value.Length > 0 ? int.Parse(mt.Groups[3].Value, CI) : 0;
                    double[] v = new double[5];
                    v[0] = I(f[1]);
                    for (int kk = 2; kk < f.Length && kk < 6; kk++) v[kk - 1] = D(f[kk]);
                    carichi.Add(Tuple.Create(wn, char.ToLowerInvariant(mt.Groups[2].Value[0]), off, v));
                }
            }

            // ---- segmentazione originale ----
            int iseg = ild >= 0 && intest(ild + 2 + nlLette) ? ild + 2 + nlLette : find("segmentation");
            if (iseg >= 0 && iseg + 1 < ls.Count)
                m.Notes.Add("Segmentazione MMANA originale (DM1, DM2, SC, EC): " + ls[iseg + 1].Trim() + "; Seg dei fili come nel file.");

            // ---- terreno, altezza, materiale, impedenza di riferimento ----
            int ig = iseg >= 0 && intest(iseg + 2) ? iseg + 2 : find("azel");
            double height = 0;
            if (ig >= 0 && ig + 1 < ls.Count)
            {
                string[] f = Split(ls[ig + 1]);
                int gtype = f.Length > 0 ? I(f[0]) : 0;
                height = f.Length > 1 ? D(f[1]) : 0;
                int mat = f.Length > 2 ? I(f[2]) : 0;
                double r0 = f.Length > 3 ? D(f[3]) : 50;
                double x0 = f.Length > 6 ? D(f[6]) : 0;
                if (r0 > 0) m.Z0 = r0;
                if (Math.Abs(x0) > 1e-9) m.Warnings.Add("Impedenza di riferimento MMANA con parte reattiva (" + x0.ToString(CI) + " ohm): per il ROS si usa solo la parte reale.");
                if (gtype == 0) m.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
                else if (gtype == 1) m.Ground = new GroundSpec { Kind = GroundKind.Perfect };
                else
                {
                    m.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005, Name = "Medio" };
                    m.Warnings.Add("Terreno reale MMANA: impostato il terreno medio (13; 0,005 S/m), da verificare.");
                }
                switch (mat)
                {
                    case 0: m.WireConductivity = 0; m.WireMaterial = "senza perdite"; break;
                    case 1: m.WireConductivity = 5.8e7; m.WireMaterial = "rame"; break;
                    case 2: m.WireConductivity = 3.5e7; m.WireMaterial = "alluminio"; break;
                    default:
                        m.WireConductivity = 0; m.WireMaterial = "senza perdite";
                        m.Warnings.Add("Materiale MMANA M=" + mat + " non riconosciuto: fili senza perdite, scegliere il materiale nelle impostazioni del modello.");
                        break;
                }
                if (gtype != 0 && Math.Abs(height) > 0)
                    m.Notes.Add("Altezza dal terreno di MMANA (" + height.ToString("0.###", CI) + " m) sommata alle quote Z.");
            }
            if (m.Ground.Kind != GroundKind.FreeSpace && height != 0)
                foreach (MmanaWireInfo w in wires) { w.Z1 += height; w.Z2 += height; }

            // ---- risegmentazione NEC ----
            double lambda = Nec2Engine.CVEL_NEC2 / fmhz;   // segmentazione come prima della 0.4.30 (indipendente dalla scelta di c)
            double maxSeg = lambda / segPerLambda;
            foreach (MmanaWireInfo w in wires)
            {
                double L = w.Length;
                int n = Math.Max(1, (int)Math.Ceiling(L / maxSeg - 1e-9));
                if (L / n < 4 * w.Radius) n = Math.Max(1, (int)Math.Floor(L / (4 * w.Radius)));
                w.NecSeg = FixOdd(n, w, L);
            }
            // giunzioni: rapporto massimo 1:2 tra segmenti adiacenti
            double tol = Math.Max(1e-6, lambda * 1e-6);
            for (int iter = 0; iter < 20; iter++)
            {
                bool changed = false;
                for (int a = 0; a < nw; a++)
                    for (int b = 0; b < nw; b++)
                    {
                        if (a == b || !Touch(wires[a], wires[b], tol)) continue;
                        MmanaWireInfo wa = wires[a], wb = wires[b];
                        if (wa.SegLength > 2.0 * wb.SegLength * 1.0001)
                        {
                            int nn = (int)Math.Ceiling(wa.Length / (2.0 * wb.SegLength) - 1e-9);
                            if (wa.Length / nn < 4 * wa.Radius) continue;
                            nn = FixOdd(nn, wa, wa.Length);
                            if (nn > wa.NecSeg) { wa.NecSeg = nn; changed = true; }
                        }
                    }
                if (!changed) break;
            }
            foreach (MmanaWireInfo w in wires)
            {
                double ratio = w.SegLength / w.Radius;
                if (ratio < 4)
                    m.Warnings.Add(string.Format(CI, "Filo {0}: segmento/raggio = {1:0.0} (< 4): risultati NEC poco affidabili su questo filo.", w.Index, ratio));
            }

            // ---- schede NEC ----
            foreach (MmanaWireInfo w in wires)
            {
                GeomCard g = new GeomCard { Id = "GW", I1 = w.Index, I2 = w.NecSeg, F1 = w.X1, F2 = w.Y1, F3 = w.Z1, F4 = w.X2, F5 = w.Y2, F6 = w.Z2, F7 = w.Radius };
                m.Geometry.Add(g);
            }
            m.GroundFlag = m.Ground.Kind == GroundKind.FreeSpace ? 0 : 1;
            foreach (Tuple<int, char, Complex> s in srcs)
            {
                MmanaWireInfo w = wires[s.Item1];
                int seg = s.Item2 == 'b' ? 1 : (s.Item2 == 'e' ? w.NecSeg : (w.NecSeg + 1) / 2);
                m.Sources.Add(new SourceSpec { Tag = w.Index, Seg = seg, V = s.Item3 });
            }
            double w0 = 2 * Math.PI * fmhz * 1e6;
            foreach (Tuple<int, char, int, double[]> c in carichi)
            {
                MmanaWireInfo w = wires[c.Item1];
                int n = w.NecSeg, off = c.Item3;
                int seg = c.Item2 == 'b' ? 1 + off : (c.Item2 == 'e' ? n - off : (n + 1) / 2 + off);
                seg = Math.Max(1, Math.Min(n, seg));
                double[] v = c.Item4;
                int tipo = (int)v[0];
                if (tipo == 1) m.Loads.Add(new LoadSpec { Type = 4, Tag = w.Index, From = seg, To = seg, F1 = v[1], F2 = v[2] });
                else if (tipo == 0)
                {
                    double L = v[1] * 1e-6, C = v[2] * 1e-12, Q = v[3];
                    if (L > 0 && C > 0) m.Loads.Add(new LoadSpec { Type = 1, Tag = w.Index, From = seg, To = seg, F1 = Q > 0 ? Q * w0 * L : 0, F2 = L, F3 = C });
                    else if (L > 0) m.Loads.Add(new LoadSpec { Type = 0, Tag = w.Index, From = seg, To = seg, F1 = Q > 0 ? w0 * L / Q : 0, F2 = L, F3 = 0 });
                    else if (C > 0) m.Loads.Add(new LoadSpec { Type = 0, Tag = w.Index, From = seg, To = seg, F1 = Q > 0 ? 1 / (w0 * C * Q) : 0, F2 = 0, F3 = C });
                    if (Q > 0) m.Notes.Add(string.Format(CI, "Carico MMANA sul filo {0}: Q {1:0.#} convertito in resistenza alla frequenza del modello ({2:0.###} MHz).", w.Index, Q, fmhz));
                }
                else m.Warnings.Add("Carico MMANA di tipo S (Laplace) sul filo " + w.Index + ": non importato.");
            }
            m.Notes.Insert(0, ImportNote);
            return m;
        }

        static int FixOdd(int n, MmanaWireInfo w, double L)
        {
            if (!w.Fed || n % 2 == 1) return n;
            if (L / (n + 1) >= 4 * w.Radius) return n + 1;
            return Math.Max(1, n - 1);
        }

        static bool Same(double x1, double y1, double z1, double x2, double y2, double z2, double tol)
        {
            return Math.Abs(x1 - x2) <= tol && Math.Abs(y1 - y2) <= tol && Math.Abs(z1 - z2) <= tol;
        }

        static bool Touch(MmanaWireInfo a, MmanaWireInfo b, double tol)
        {
            return Same(a.X1, a.Y1, a.Z1, b.X1, b.Y1, b.Z1, tol) || Same(a.X1, a.Y1, a.Z1, b.X2, b.Y2, b.Z2, tol)
                || Same(a.X2, a.Y2, a.Z2, b.X1, b.Y1, b.Z1, tol) || Same(a.X2, a.Y2, a.Z2, b.X2, b.Y2, b.Z2, tol);
        }
    }
}
