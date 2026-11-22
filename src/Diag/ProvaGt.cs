// YA-NEC 1.5.8 - prova senza finestra del G/T dello stack della maschera VHF dell'ottimizzatore (Engine/OttGt.cs).
// Avvio: YA-NEC --prova-gt <uscita.txt> <modello> [modello con variabili]
// 1) G/T della singola e dello stack 2 x 2 (veloce e NEC) del modello; con la EF0213 di YU7EF si confronta con TANT (G/T -7,32 dB a 30 gradi).
// 2) se c'e' il secondo modello: ottimizzazione breve con la maschera VHF (ROS 60 con soglia 1,3, G/T 100) sulle sue variabili (+-3 %).
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using YaNec.Engine;

namespace YaNec
{
    static class ProvaGt
    {
        static readonly CultureInfo IC = CultureInfo.InvariantCulture;
        static string N(double v, int d) { return double.IsNaN(v) ? "-" : v.ToString("F" + d, IC); }

        static void Riga(StringBuilder sb, string nome, OttGtRisultato r, long ms)
        {
            sb.AppendLine(nome + ": G/T sistema " + N(r.GtSys, 2) + " dB/K, G/T antenna " + N(r.GtAnt, 2) + ", G/T singola " + N(r.GtSingola, 2) +
                ", Tant " + N(r.Tant, 1) + " K, G stack " + N(r.Gstack, 2) + " dBi, G singola " + N(r.Gsingola, 2) + " dBi, DL6WU V " + N(r.Dv, 3) + " m H " + N(r.Dh, 3) +
                " m (aperture " + N(r.HpbwEl, 1) + " / " + N(r.HpbwAz, 1) + " gradi), " + ms + " ms");
        }

        public static int Esegui(string uscita, string modello, string conVariabili)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("YA-NEC prova del G/T dello stack (maschera VHF) - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", IC));
            NecModel m = YanecFile.Carica(modello);
            double f = m.Frequencies()[0];
            OttGtImpostazioni s = new OttGtImpostazioni();
            OttGtImpostazioni.Temperature(f, out s.Tcielo, out s.Tterreno);
            sb.AppendLine(Path.GetFileName(modello) + " a " + N(f, 3) + " MHz; cielo " + s.Tcielo + " K, terreno " + s.Tterreno + " K, 30 gradi, NF 0,25 dB, preamp 20 dB, cavo 2 dB, combinatore 0,2 dB");
            SegmentGeometry g = m.BuildGeometry();
            foreach (int k in new[] { 3, 2 })
            {
                s.PassoGradi = k;
                Stopwatch w = Stopwatch.StartNew();
                OttGtRisultato a = OttGt.Calcola(m, g, f, s, false, CancellationToken.None);
                Riga(sb, "passo " + k + " gradi, veloce (fattore di schiera)", a, w.ElapsedMilliseconds);
                w.Restart();
                OttGtRisultato b = OttGt.Calcola(m, g, f, s, true, CancellationToken.None);
                Riga(sb, "passo " + k + " gradi, NEC dello stack", b, w.ElapsedMilliseconds);
                sb.AppendLine("  differenza NEC - veloce: " + N(b.GtSys - a.GtSys, 3) + " dB");
            }
            s.PassoGradi = 3;
            foreach (int[] rc in new[] { new[] { 1, 2 }, new[] { 2, 1 }, new[] { 1, 1 } })
            {
                s.Righe = rc[0]; s.Colonne = rc[1];
                Riga(sb, rc[0] + " x " + rc[1] + " NEC", OttGt.Calcola(m, g, f, s, true, CancellationToken.None), 0);
            }
            if (!string.IsNullOrEmpty(conVariabili))
            {
                sb.AppendLine();
                NecModel mv = YanecFile.Carica(conVariabili);
                Ottimizzatore o = new Ottimizzatore { Partenza = mv, Simplesso = true, MaxPassi = 40 };
                OttGtImpostazioni.Temperature(mv.Frequencies()[0], out o.Gt.Tcielo, out o.Gt.Tterreno);
                o.Obiettivi.Add(new OttObiettivo { Chiave = "ros", Peso = 60, HaSoglia = true, Soglia = 1.3 });
                o.Obiettivi.Add(new OttObiettivo { Chiave = "gt", Peso = 100 });
                foreach (SyVar v in mv.Variables)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(v.Expr.Replace(" ", ""), @"^[\d.]+(\*MM)?$") || v.Value == 0) continue;
                    double x = v.Value;
                    double a = Math.Abs(x);
                    o.Variabili.Add(new OttVariabile { Nome = v.Name, Valore = x, Min = x - 0.03 * a, Max = x + 0.03 * a, Passo = Math.Max(1e-4, 0.002 * a) });
                }
                o.Frequenze.Add(mv.Frequencies()[0]);
                sb.AppendLine(Path.GetFileName(conVariabili) + ": " + o.Variabili.Count + " variabili, maschera VHF, simplesso, 40 passi");
                Stopwatch w = Stopwatch.StartNew();
                o.Esegui(CancellationToken.None);
                sb.AppendLine("partenza: voto " + N(o.Iniziale.Voto, 1) + ", G/T " + N(o.Iniziale.GtVoto, 2) + " (NEC " + N(o.Iniziale.GtNec, 2) + "), ROS " + N(o.Iniziale.RosPeggiore, 2));
                sb.AppendLine("migliore: voto " + N(o.Migliore.Voto, 1) + ", G/T " + N(o.Migliore.GtVoto, 2) + " (NEC " + N(o.Migliore.GtNec, 2) + "), ROS " + N(o.Migliore.RosPeggiore, 2) +
                    ", passi " + o.Passi + ", controlli NEC " + o.GtControlli + ", " + w.ElapsedMilliseconds / 1000 + " s");
                for (int i = 0; i < o.Variabili.Count; i++) sb.AppendLine("  " + o.Variabili[i].Nome + " " + N(o.Variabili[i].Valore, 4) + " -> " + N(o.Migliore.Valori[i], 4));
            }
            File.WriteAllText(uscita, sb.ToString());
            Console.WriteLine(sb.ToString());
            return 0;
        }
    }
}
