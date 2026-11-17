// YA-NEC 1.2.0 - Avvisi > "Test del guadagno medio (AGT)" (bozzetto HF 11 D approvato da IC8TEM il 17/11/2026): il programma rifa' da se'
// il calcolo togliendo le perdite (resistenze dei carichi, materiale dei fili) e il terreno reale: in spazio libero la media del guadagno
// su tutta la sfera deve valere 1,000; sul terreno perfetto la media sulla semisfera superiore 2,000 (metodo di Burke / Lewallen, EZNEC).
// Oltre +-0,2 dB: avviso e proposta di verifica della segmentazione. Terza riga: stesso test con la segmentazione raddoppiata.
using System;
using System.Collections.Generic;
using System.Threading;

namespace YaNec.Engine
{
    public static class AgtTest
    {
        public class Riga { public string Nome; public double Valore, Atteso, Db; public bool Buono; public string Errore; }

        /// <summary>Copia del modello senza perdite: R dei carichi a zero, niente materiale dei fili e niente carichi di conducibilita'.</summary>
        public static NecModel SenzaPerdite(NecModel m0)
        {
            NecModel m = m0.Clone();
            m.WireConductivity = 0;
            List<LoadSpec> l = new List<LoadSpec>();
            foreach (LoadSpec ld in m.Loads)
            {
                if (ld.Type == 5) continue;
                LoadSpec c = ld.Clone();
                if (c.Type == 0 || c.Type == 1 || c.Type == 4) c.F1 = 0;      // R serie, R parallela (0 = assente), parte reale
                if (c.Type == 2 || c.Type == 3) c.F1 = 0;
                l.Add(c);
            }
            m.Loads = l;
            return m;
        }

        static double Media(NecModel m, double f, bool semisfera, CancellationToken ct)
        {
            RunResult r = Analysis.Run(m, m.BuildGeometry(), f, 3, ct);
            double sum = 0, d = 3 * Math.PI / 180;
            for (double th = 1.5; th < (semisfera ? 90 : 180); th += 3)
            {
                double w = Math.Sin(th * Math.PI / 180) * d * d;
                for (double ph = 0; ph < 360; ph += 3)
                {
                    double g = r.Engine.Gain(th, ph).GainTotal;
                    if (g > -900) sum += Math.Pow(10, g / 10) * w;
                }
            }
            return sum / (semisfera ? 2 * Math.PI : 4 * Math.PI);
        }

        public static List<Riga> Esegui(NecModel m0, CancellationToken ct)
        {
            List<Riga> l = new List<Riga>();
            double f = m0.FreqMHz;
            NecModel a = SenzaPerdite(m0);
            Func<string, double, Func<double>, Riga> prova = (nome, atteso, calc) =>
            {
                Riga r = new Riga { Nome = nome, Atteso = atteso };
                try { r.Valore = calc(); r.Db = 10 * Math.Log10(r.Valore / atteso); r.Buono = Math.Abs(r.Db) <= 0.2; }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { r.Errore = ex.Message; r.Valore = double.NaN; }
                return r;
            };
            NecModel fs = a.Clone(); fs.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
            l.Add(prova("AGT in spazio libero, senza perdite", 1, () => Media(fs, f, false, ct)));
            NecModel pg = a.Clone(); pg.Ground = new GroundSpec { Kind = GroundKind.Perfect };
            l.Add(prova("AGT sul terreno perfetto (atteso 2,000)", 2, () => Media(pg, f, true, ct)));
            NecModel sd = fs.Clone();
            Dictionary<int, int[]> nn = new Dictionary<int, int[]>();   // tag -> segmenti prima e dopo (2n + 1: il centro resta un segmento)
            foreach (GeomCard g in sd.Geometry)
                if (g.Id == "GW" && g.I2 > 0)
                {
                    int n0 = g.I2; g.I2 = 2 * n0 + 1;
                    if (!nn.ContainsKey(g.I1)) nn[g.I1] = new[] { n0, g.I2 };
                    if (g.Src != null && g.Src.Length > 1) g.Src[1] = g.I2.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            Func<int, int, int> nuovo = (tag, s0) =>
            {
                int[] q; if (s0 <= 0 || !nn.TryGetValue(tag, out q)) return s0;
                return Math.Max(1, Math.Min(q[1], (int)Math.Round((s0 - 0.5) / q[0] * q[1] + 0.5)));
            };
            foreach (SourceSpec s in sd.Sources) s.Seg = nuovo(s.Tag, s.Seg);
            foreach (LoadSpec ld in sd.Loads) { ld.From = nuovo(ld.Tag, ld.From); ld.To = nuovo(ld.Tag, ld.To); }
            l.Add(prova("AGT dopo il cambio di segmentazione (segmenti raddoppiati)", 1, () => Media(sd, f, false, ct)));
            return l;
        }
    }
}
