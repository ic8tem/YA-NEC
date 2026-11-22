// YA-NEC - abaco delle filari alimentate in punta (IC8TEM 22/11/2026 16:28: «Ipotesi di filo orizzontale sul terreno a 3 mt da terra,
// terreno perfetto. Costruiamo degli abachi (tabelle) di lunghezze predeterminate con 49:1 e con 64:1 ... per lambda/4 della freq piu'
// bassa abbiamo difficolta' d'uso; per lambda/2 andiamo meglio. Poi medesimo abaco ... per il trasformatore 9:1, studiando anche le
// migliori lunghezze di contrappeso»). Avvio: YA-NEC --abaco-filari <uscita.tsv>. Ipotesi: filo nudo da 1 mm orizzontale a 3 m,
// contrappeso orizzontale alla stessa altezza dalla parte opposta, terreno perfetto (GN 1), trasformatore ideale (ROS su 50 x rapporto).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using YaNec.Engine;

namespace YaNec
{
    static class AbacoFilari
    {
        static readonly CultureInfo IC = CultureInfo.InvariantCulture;
        const double C0 = 299.792458, A = 0.0005;
        static double H = 3.0, Passo = 0.5;
        static bool Perfetto = true;   // 22/11/2026 17:30: anche terreno medio (GN 2, 13 / 0,005 S/m) e altre altezze
        static readonly string[] Nomi = CalcFilari.BandeNomi;
        static readonly double[] F = CalcFilari.BandeF;

        static string N(double v, string f = "0.00") { return v.ToString(f, IC); }

        /// <summary>Impedenza in punta: filo L orizzontale a 3 m, contrappeso cp dalla parte opposta, alimentazione su 0,1 m tra i due.</summary>
        static Complex Z(double L, double cp, double f)
        {
            double seg = Math.Min(0.33, C0 / f / 40);
            StringBuilder sb = new StringBuilder("CM abaco filari\nCE\n");
            int tag = 0;
            Func<double, double, int, string> gw = (x1, x2, n) => { tag++; return "GW " + tag + " " + n + " " + N(x1, "0.####") + " 0 " + N(H) + " " + N(x2, "0.####") + " 0 " + N(H) + " " + N(A, "0.######") + "\n"; };
            int nc = Math.Max(1, (int)Math.Ceiling(cp / seg)), nw = Math.Max(3, (int)Math.Ceiling(L / seg));
            if (cp > 0.01) sb.Append(gw(-0.1 - cp, -0.1, nc));
            sb.Append(gw(-0.1, 0, 1)); int feed = tag;
            sb.Append(gw(0, L, nw));
            sb.Append((Perfetto ? "GE 1\nGN 1" : "GE 0\nGN 2 0 0 0 13 0.005") + "\nEX 0 " + feed + " 1 0 1 0\nFR 0 1 0 0 " + N(f, "0.####") + " 0\nEN\n");
            NecModel m = NecModel.Parse(sb.ToString());
            SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, f);
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            return e.Sources[0].Z;
        }

        static string Riga(double L, double cp, double z0, int b0, StringBuilder dettagli)
        {
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < F.Length; i++)
            {
                if (i < b0) { s.Append("\t"); continue; }
                Complex z = Z(L, cp, F[i]);
                double r = CalcFilari.Ros(z, z0);
                s.Append("\t" + (r >= 99 ? ">99" : N(r, "0.0")));
                if (dettagli != null) dettagli.Append(Nomi[i] + " " + N(z.Real, "0") + (z.Imaginary >= 0 ? "+j" : "-j") + N(Math.Abs(z.Imaginary), "0") + "; ");
            }
            return s.ToString();
        }

        public static int Esegui(string uscita, string terreno = "perfetto", double altezza = 3, double passo = 0.5)
        {
            Perfetto = terreno != "medio"; H = altezza; Passo = passo;
            StringBuilder o = new StringBuilder();
            o.AppendLine("# YA-NEC abaco filari: filo nudo 1 mm orizzontale a " + N(H, "0.#") + " m, contrappeso orizzontale opposto, terreno " + (Perfetto ? "perfetto" : "medio (13, 0,005 S/m)") + ", trasformatore ideale. " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", IC));
            string intest = "\t" + string.Join("\t", Nomi);
            // ---------------- EFHW 49:1 e 64:1 ----------------
            int[] basse = { 0, 1, 3, 5, 7, 9 };   // 160, 80, 40, 20, 15, 10 m
            foreach (int rapp in new[] { 49, 64 })
            {
                double z0 = 50 * rapp;
                o.AppendLine("## EFHW " + rapp + ":1 (Z0 " + z0 + " ohm), contrappeso 0,05 lambda della banda piu' bassa");
                o.AppendLine("banda bassa\tlunghezza\tfilo m\tcontrappeso m" + intest);
                foreach (int b in basse)
                {
                    double f = F[b], lam = C0 / f, cp = 0.05 * lam;
                    double l0 = (1 - CalcFilari.Estremita(f, A)) * lam / 2;
                    // lunghezza col ROS minimo alla banda piu' bassa (scansione 0,85-1,10 e affinamento)
                    double best = l0, rb = double.MaxValue;
                    for (double k = 0.85; k <= 1.1001; k += 0.01) { double r = CalcFilari.Ros(Z(l0 * k, cp, f), z0); if (r < rb) { rb = r; best = l0 * k; } }
                    for (double d = -0.008; d <= 0.0081; d += 0.002) { double l = best * (1 + d), r = CalcFilari.Ros(Z(l, cp, f), z0); if (r < rb) { rb = r; best = l; } }
                    // multibanda: lunghezza che rende migliore la banda armonica peggiore
                    List<int> arm = new List<int>();
                    for (int i = b; i < F.Length; i++) for (int n = 1; n <= 16; n++) if (Math.Abs(F[i] - n * f) / F[i] < 0.06) { arm.Add(i); break; }
                    double bm = best, rm = double.MaxValue;
                    if (arm.Count > 1)
                        for (double k = 0.90; k <= 1.06; k += 0.005)
                        {
                            double l = best * k, peg = 0;
                            foreach (int i in arm) peg = Math.Max(peg, CalcFilari.Ros(Z(l, cp, F[i]), z0));
                            if (peg < rm) { rm = peg; bm = l; }
                        }
                    o.AppendLine(Nomi[b] + "\tλ/2 formula\t" + N(l0) + "\t" + N(cp) + Riga(l0, cp, z0, b, null));
                    o.AppendLine(Nomi[b] + "\tλ/2 accordata (monobanda)\t" + N(best) + "\t" + N(cp) + Riga(best, cp, z0, b, null));
                    if (arm.Count > 1) o.AppendLine(Nomi[b] + "\tλ/2 multibanda (armoniche: " + string.Join(" ", arm.ConvertAll(i => Nomi[i])) + ")\t" + N(bm) + "\t" + N(cp) + Riga(bm, cp, z0, b, null));
                    o.AppendLine(Nomi[b] + "\tλ/4 (difficile)\t" + N(l0 / 2) + "\t" + N(cp) + Riga(l0 / 2, cp, z0, b, null));
                    Console.WriteLine("EFHW " + rapp + " " + Nomi[b] + " fatto");
                }
                o.AppendLine();
            }
            // ---------------- 9:1: lunghezze e contrappesi ----------------
            double[] ve3eed = { 29, 35.5, 41, 58, 71, 84, 107, 119, 148 };
            double[] cpFissi = Perfetto ? new double[] { 0, 1, 2, 3, 5, 7.6, 10, 15 } : new double[] { 0, 1, 2, 3, 5, 7.6, 10 };
            double[] cpPerc = Perfetto ? new[] { 0.05, 0.075, 0.10, 0.15, 0.25 } : new[] { 0.05, 0.10, 0.25 };
            const double z09 = 450;
            int bandaMin = 1;   // dagli 80 m in su
            Func<double, double, double[]> rosBande = (L, cp) =>
            {
                double[] r = new double[F.Length];
                for (int i = bandaMin; i < F.Length; i++) r[i] = CalcFilari.Ros(Z(L, cp, F[i]), z09);
                return r;
            };
            Func<double[], double> punteggio = r =>
            {
                // bande con ROS <= 3 (accordatore interno) prima, poi media geometrica dei ROS (limitati a 50)
                int buone = 0; double lg = 0; int n = 0;
                for (int i = bandaMin; i < r.Length; i++) { if (r[i] <= 3) buone++; lg += Math.Log(Math.Min(50, r[i])); n++; }
                return buone * 100 - Math.Exp(lg / n);
            };
            o.AppendLine("## 9:1 (Z0 450 ohm): lunghezze VE3EED e contrappesi (dagli 80 m in su)");
            o.AppendLine("filo m\tcontrappeso m\tcontrappeso\tbande ROS<=3" + intest);
            List<double> lunghezze = new List<double>(); foreach (double ft in ve3eed) lunghezze.Add(ft * 0.3048);
            foreach (double L in lunghezze)
            {
                List<double> cps = new List<double>(cpFissi); foreach (double p in cpPerc) cps.Add(L * p);
                double bestCp = 0, bestP = double.MinValue; double[] bestR = null;
                StringBuilder tutte = new StringBuilder();
                foreach (double cp in cps)
                {
                    double[] r = rosBande(L, cp); double p = punteggio(r);
                    int buone = 0; for (int i = bandaMin; i < r.Length; i++) if (r[i] <= 3) buone++;
                    string pc = cp > 0 ? N(cp / L * 100, "0") + " %" : "nessuno";
                    StringBuilder rr = new StringBuilder(); for (int i = 0; i < F.Length; i++) rr.Append("\t" + (i < bandaMin ? "" : (r[i] >= 99 ? ">99" : N(r[i], "0.0"))));
                    tutte.AppendLine(N(L, "0.0") + "\t" + N(cp, "0.0") + "\t" + pc + "\t" + buone + rr);
                    if (p > bestP) { bestP = p; bestCp = cp; bestR = r; }
                }
                o.Append(tutte);
                o.AppendLine("MIGLIORE\t" + N(L, "0.0") + " m con contrappeso " + N(bestCp, "0.0") + " m");
                Console.WriteLine("9:1 " + N(L, "0.0") + " fatto");
            }
            o.AppendLine();
            // scansione delle lunghezze 6-46 m con il contrappeso migliore tra quelli provati
            o.AppendLine("## 9:1 scansione delle lunghezze (passo 0,5 m), contrappeso migliore per ciascuna");
            o.AppendLine("filo m\tcontrappeso m\tcontrappeso\tbande ROS<=3" + intest);
            for (double L = 6; L <= 46.001; L += Passo)
            {
                List<double> cps = new List<double>(cpFissi); foreach (double p in cpPerc) cps.Add(L * p);
                double bestCp = 0, bestP = double.MinValue; double[] bestR = null;
                foreach (double cp in cps) { double[] r = rosBande(L, cp); double p = punteggio(r); if (p > bestP) { bestP = p; bestCp = cp; bestR = r; } }
                int buone = 0; for (int i = bandaMin; i < bestR.Length; i++) if (bestR[i] <= 3) buone++;
                StringBuilder rr = new StringBuilder(); for (int i = 0; i < F.Length; i++) rr.Append("\t" + (i < bandaMin ? "" : (bestR[i] >= 99 ? ">99" : N(bestR[i], "0.0"))));
                o.AppendLine(N(L, "0.0") + "\t" + N(bestCp, "0.0") + "\t" + (bestCp > 0 ? N(bestCp / L * 100, "0") + " %" : "nessuno") + "\t" + buone + rr);
                Console.WriteLine("scansione " + N(L, "0.0"));
            }
            File.WriteAllText(uscita, o.ToString(), new UTF8Encoding(false));
            return 0;
        }
    }
}
