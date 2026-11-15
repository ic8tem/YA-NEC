// YA-NEC - scrittura dei modelli in formato MMANA-GAL (.maa), per il dialogo "Salva con nome" approvato alla lettera.
// I fili si ricavano dalla geometria gia' calcolata (le schede GA, GH, GM, GR, GX diventano fili diritti): i segmenti consecutivi
// e allineati dello stesso filo NEC formano un filo MMANA. Le sorgenti diventano riferimenti wNb / wNc / wNe; i carichi R+jX
// (LD 4) e RLC serie (LD 0) diventano carichi MMANA; le linee di trasmissione non esistono in MMANA e vanno perse (avviso).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public static class MmanaExport
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static string F(double v) { return v != 0 && Math.Abs(v) < 1e-3 ? v.ToString("G9", CI) : v.ToString("0.######", CI); }   // valori piccoli (C in farad, L in henry) in notazione esponenziale

        public class Wire { public double X1, Y1, Z1, X2, Y2, Z2, R; public int Tag, FirstSeg, NSeg; }

        /// <summary>Fili MMANA ricavati dalla geometria a segmenti (raggruppa i segmenti allineati dello stesso tag).</summary>
        public static List<Wire> Wires(SegmentGeometry g)
        {
            List<Wire> l = new List<Wire>();
            Wire cur = null;
            int segInTag = 0, lastTag = int.MinValue;
            for (int i = 0; i < g.N; i++)
            {
                if (g.Tag[i] != lastTag) { segInTag = 0; lastTag = g.Tag[i]; }
                segInTag++;
                double dx = g.X2[i] - g.X1[i], dy = g.Y2[i] - g.Y1[i], dz = g.Z2[i] - g.Z1[i];
                bool cont = false;
                if (cur != null && cur.Tag == g.Tag[i] && Math.Abs(cur.R - g.Bi[i]) < 1e-12)
                {
                    double ex = g.X1[i] - cur.X2, ey = g.Y1[i] - cur.Y2, ez = g.Z1[i] - cur.Z2;
                    double cx = cur.X2 - cur.X1, cy = cur.Y2 - cur.Y1, cz = cur.Z2 - cur.Z1;
                    double lc = Math.Sqrt(cx * cx + cy * cy + cz * cz), ls = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    double cos = (cx * dx + cy * dy + cz * dz) / Math.Max(1e-15, lc * ls);
                    cont = Math.Sqrt(ex * ex + ey * ey + ez * ez) < 1e-6 * Math.Max(1, lc) && cos > 1 - 1e-9;
                }
                if (cont)
                {
                    cur.X2 = g.X2[i]; cur.Y2 = g.Y2[i]; cur.Z2 = g.Z2[i]; cur.NSeg++;
                }
                else
                {
                    cur = new Wire { X1 = g.X1[i], Y1 = g.Y1[i], Z1 = g.Z1[i], X2 = g.X2[i], Y2 = g.Y2[i], Z2 = g.Z2[i], R = g.Bi[i], Tag = g.Tag[i], FirstSeg = segInTag, NSeg = 1 };
                    l.Add(cur);
                }
            }
            return l;
        }

        /// <summary>Testo .maa. segYaNec = true: il numero di segmenti di YA-NEC; false: -1 (MMANA risegmenta da solo).</summary>
        public static string Write(NecModel m, bool segYaNec, List<string> perso)
        {
            SegmentGeometry g = m.BuildGeometry();
            List<Wire> ws = Wires(g);
            StringBuilder sb = new StringBuilder();
            string nome = m.Comments.Count > 0 ? m.Comments[0] : (string.IsNullOrEmpty(m.FileName) ? "YA-NEC" : System.IO.Path.GetFileNameWithoutExtension(m.FileName));
            sb.AppendLine(nome.Replace(",", " "));
            sb.AppendLine("*");
            sb.AppendLine(F(m.FreqMHz));
            sb.AppendLine("***Wires***");
            sb.AppendLine(ws.Count + ",\t1,\t2,\t2.0,\t2.0,\t1");
            foreach (Wire w in ws)
                sb.AppendLine(F(w.X1) + ",\t" + F(w.Y1) + ",\t" + F(w.Z1) + ",\t" + F(w.X2) + ",\t" + F(w.Y2) + ",\t" + F(w.Z2) + ",\t" + F(w.R) + ",\t" + (segYaNec ? w.NSeg.ToString(CI) : "-1"));

            // riferimento MMANA del segmento (tag, seg): filo e punto b/c/e
            Func<int, int, string> rif = (tag, seg) =>
            {
                for (int k = 0; k < ws.Count; k++)
                {
                    Wire w = ws[k];
                    if (w.Tag != tag || seg < w.FirstSeg || seg >= w.FirstSeg + w.NSeg) continue;
                    int rel = seg - w.FirstSeg;               // 0 = primo segmento del filo MMANA
                    string p = rel == 0 && w.NSeg > 1 ? "b" : (rel == w.NSeg - 1 && w.NSeg > 1 ? "e" : "c");
                    if (p == "c" && w.NSeg % 2 == 0 || (p == "c" && rel != w.NSeg / 2)) perso.Add("Sorgente/carico sul filo " + tag + " segmento " + seg + ": in MMANA va al centro del filo " + (k + 1) + ".");
                    return "w" + (k + 1) + p;
                }
                perso.Add("Riferimento al filo " + tag + " segmento " + seg + " non trovato: messo sul filo 1.");
                return "w1c";
            };

            sb.AppendLine("***Source***");
            sb.AppendLine(m.Sources.Count + ",\t" + (m.CurrentSources ? "1" : "0"));
            foreach (SourceSpec s in m.Sources)
            {
                Complex v = s.V;
                double amp = Complex.Abs(v), ph = Math.Atan2(v.Imaginary, v.Real) * 180 / Math.PI;
                sb.AppendLine(rif(s.Tag, s.Seg) + ",\t" + F(ph) + ",\t" + F(amp == 0 ? 1 : amp));
            }

            List<string> carichi = new List<string>();
            foreach (LoadSpec ld in m.Loads)
            {
                // 0.4.5: tipi di MMANA come nel suo help: 0 = LC (L uH, C pF, Q; L e C insieme = trappola parallela), 1 = R+jX
                double w0 = 2 * Math.PI * m.FreqMHz * 1e6;
                if (ld.Type == 4) carichi.Add(rif(ld.Tag, ld.From) + ",\t1,\t" + F(ld.F1) + ",\t" + F(ld.F2));
                else if (ld.Type == 0 && ld.F2 > 0 && ld.F3 <= 0) carichi.Add(rif(ld.Tag, ld.From) + ",\t0,\t" + F(ld.F2 * 1e6) + ",\t0.0,\t" + F(ld.F1 > 0 ? w0 * ld.F2 / ld.F1 : 0));
                else if (ld.Type == 0 && ld.F3 > 0 && ld.F2 <= 0) carichi.Add(rif(ld.Tag, ld.From) + ",\t0,\t0.0,\t" + F(ld.F3 * 1e12) + ",\t" + F(ld.F1 > 0 ? 1 / (w0 * ld.F3 * ld.F1) : 0));
                else if (ld.Type == 1 && ld.F2 > 0 && ld.F3 > 0) carichi.Add(rif(ld.Tag, ld.From) + ",\t0,\t" + F(ld.F2 * 1e6) + ",\t" + F(ld.F3 * 1e12) + ",\t" + F(ld.F1 > 0 ? ld.F1 / (w0 * ld.F2) : 0));
                else if (ld.Type == 0 || ld.Type == 1)
                {
                    // RLC che MMANA non ha: R+jX alla frequenza del modello
                    System.Numerics.Complex zl = ld.Type == 0
                        ? new System.Numerics.Complex(ld.F1, w0 * ld.F2 - (ld.F3 > 0 ? 1 / (w0 * ld.F3) : 0))
                        : 1 / ((ld.F1 > 0 ? 1 / ld.F1 : 0) + (ld.F2 > 0 ? new System.Numerics.Complex(0, -1 / (w0 * ld.F2)) : 0) + (ld.F3 > 0 ? new System.Numerics.Complex(0, w0 * ld.F3) : 0));
                    carichi.Add(rif(ld.Tag, ld.From) + ",\t1,\t" + F(zl.Real) + ",\t" + F(zl.Imaginary));
                    perso.Add("Carico RLC sul filo " + ld.Tag + ": in MMANA come R+jX valido a " + F(m.FreqMHz) + " MHz.");
                }
                else if (ld.Type != 5) perso.Add("Carico LD " + ld.Type + " sul filo " + ld.Tag + ": tipo non disponibile in MMANA, non scritto.");
            }
            sb.AppendLine("***Load***");
            sb.AppendLine(carichi.Count + ",\t1");
            foreach (string c in carichi) sb.AppendLine(c);
            foreach (NetworkSpec n in m.Networks)
                perso.Add((n.IsTl ? "Linea di trasmissione" : "Rete") + " dal filo " + n.Tag1 + " al filo " + n.Tag2 + ": MMANA non la gestisce, va persa.");
            sb.AppendLine("***Segmentation***");
            sb.AppendLine("800,\t80,\t2.0,\t2");
            sb.AppendLine("***G/H/M/R/AzEl/X***");
            int gt = m.Ground.Kind == GroundKind.FreeSpace ? 0 : (m.Ground.Kind == GroundKind.Perfect ? 1 : 2);
            sb.AppendLine(gt + ",\t0.0,\t0,\t" + F(m.Z0) + ",\t120,\t60,\t0.0");
            if (gt == 2) perso.Add("Terreno reale: MMANA usa i propri parametri del terreno (impostali in MMANA: er " + F(m.Ground.Epsr) + ", sigma " + F(m.Ground.Sigma) + " S/m).");
            return sb.ToString();
        }
    }
}
