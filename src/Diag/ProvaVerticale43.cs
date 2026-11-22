// YA-NEC 1.5.9 - prova senza finestra della scheda «Verticale non risonante (43 piedi)». Avvio: YA-NEC --prova-v43 <uscita.txt>
// Stima TF5NN e NEC («Calcola le bande (NEC)») con radiali interrati (Rg ON4UN e Rg fissa 10 ohm come AD5X) e rialzati,
// nei tre adattamenti; stampa tabella, riquadri e avvisi per il confronto con i numeri di AD5X.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using YaNec.Engine;

namespace YaNec
{
    static class ProvaVerticale43
    {
        static void Stampa(StringBuilder sb, string titolo, CalcOutput o, long ms)
        {
            sb.AppendLine("== " + titolo + " (" + ms + " ms)");
            foreach (CalcCard c in o.Cards) sb.AppendLine("  [" + c.Title + "] " + c.Value + " - " + c.Sub);
            if (o.Tabella != null) foreach (string[] r in o.Tabella) sb.AppendLine("  " + string.Join(" | ", r));
            foreach (string w in o.Warnings) sb.AppendLine("  ! " + w);
            foreach (string w in o.Oks) sb.AppendLine("  ok " + w);
            foreach (CalcRimando r in o.Rimandi) sb.AppendLine("  > " + r.Testo + " " + r.Voce);
        }

        public static int Esegui(string uscita)
        {
            try { CalcCavi.Lista = Cables.Load(Path.Combine(AppPaths.DataDir, "cavi.csv")); } catch { }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("YA-NEC prova della Verticale non risonante (43 piedi) - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            foreach (int rad in new[] { 0, 1 })
                foreach (int rgm in rad == 0 ? new[] { 0, 1 } : new[] { 0 })
                {
                    CalcVerticale43 c = new CalcVerticale43();
                    c.Par("rad").Value = rad; c.Par("rgm").Value = rgm;
                    string nome = rad == 1 ? "radiali rialzati 2,5 m" : (rgm == 0 ? "interrati, Rg ON4UN" : "interrati, Rg 10 ohm (AD5X)");
                    Stopwatch w = Stopwatch.StartNew();
                    Stampa(sb, nome + ", stima", c.Run(), w.ElapsedMilliseconds);
                    w.Restart();
                    c.Tune(1.85);
                    long t = w.ElapsedMilliseconds;
                    for (int ad = 0; ad <= 2; ad++)
                    {
                        c.Par("ad").Value = ad;
                        Stampa(sb, nome + ", NEC, " + CalcVerticale43.Adattamenti[ad], c.Run(), t);
                    }
                }
            File.WriteAllText(uscita, sb.ToString());
            Console.WriteLine(sb.ToString());
            return 0;
        }
    }
}
