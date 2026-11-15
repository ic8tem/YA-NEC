// Elenco "PASSI DA GUARDARE" del resoconto di compila.bat (avvio con --passi <resoconto> <uscita>).
// 0.4.11 (collaudo 0.4.9 del 14/11/2026): il comando PowerShell della 0.4.8 non scriveva niente (elenco vuoto anche con due ERRORE
// e tre AVVISO nel resoconto). Ora lo fa il programma: per ogni riga con "esito:    ERRORE", "esito:    AVVISO" o "-> ERRORE"
// scrive la procedura (o la sezione) a cui appartiene, le 4 righe prima (passo, atteso, ottenuto) e la riga stessa.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YaNec
{
    static class PassiDaGuardare
    {
        public static int Scrivi(string resoconto, string uscita)
        {
            try
            {
                // il resoconto e' scritto da cmd nella codifica della console: Latin1 legge qualunque byte senza errori
                string[] righe = File.ReadAllLines(resoconto, Encoding.Latin1);
                StringBuilder sb = new StringBuilder();
                Queue<string> prima = new Queue<string>();
                string proc = "", sezione = "";
                int n = 0;
                foreach (string r in righe)
                {
                    if (r.StartsWith("PROCEDURA ")) proc = r.Trim();
                    if (r.StartsWith("----- ") || r.StartsWith("===== ")) { sezione = r.Trim(); proc = ""; }
                    // 0.4.16: solo le righe che cominciano con "esito:" (non quelle che lo citano dentro un "ottenuto") e le righe PROVA del registro
                    string rt = r.TrimStart();
                    bool trovato = rt.StartsWith("esito:    ERRORE") || rt.StartsWith("esito:    AVVISO") ||
                        (r.Contains(" PROVA ") && (r.Contains("-> ERRORE") || r.Contains("-> AVVISO")));
                    if (trovato)
                    {
                        n++;
                        sb.AppendLine();
                        sb.AppendLine(proc.Length > 0 ? proc : sezione);
                        foreach (string p in prima) sb.AppendLine(p);
                        sb.AppendLine(r);
                    }
                    prima.Enqueue(r);
                    while (prima.Count > 4) prima.Dequeue();
                }
                sb.Insert(0, n == 0 ? "Nessun passo con AVVISO o ERRORE." + Environment.NewLine : n + " passi con AVVISO o ERRORE:" + Environment.NewLine);
                File.WriteAllText(uscita, sb.ToString(), Encoding.Latin1);
                return 0;
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(uscita, "Elenco dei passi non creato: " + ex.Message + Environment.NewLine); } catch { }
                return 1;
            }
        }
    }
}
