// YA-NEC - registro delle operazioni, in testo semplice (dalla 0.4.2 sempre attivo, richiesta di IC8TEM del 13/11/2026).
// File: cartella "Registri" accanto al programma, un file per sessione: YA-NEC_registro_AAAA-MM-GG_hh-mm-ss.txt
// (se la cartella del programma non e' scrivibile: Documenti\YA-NEC\Registri). Si tengono gli ultimi 30 registri.
// Con --registro e --prove-interfaccia (compila.bat) il registro va anche in data/registro.txt come prima.
// Ogni riga: numero, ora, millisecondi dalla riga precedente, categoria, messaggio.
// Categorie: AVVIO, COMPUTER, AZIONE (cosa ha fatto l'utente), RISPOSTA (quanto ci ha messo l'interfaccia a tornare libera),
// TEMPO (durata di una funzione), RISORSE (CPU, GPU, RAM, disco), BLOCCO (interfaccia ferma), STATO, ERRORE, CALCOLO,
// MODELLO, PROVA, ECCEZIONE, FINE. In fondo al file: il punteggio delle funzioni (anche in Registri/punteggio_funzioni.txt).
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace YaNec
{
    public static class Registro
    {
        static readonly object lk = new object();
        static StreamWriter w, wCopia;
        static string file;
        static int passo;
        static long ultimo;
        static bool chiuso;

        public static bool Attivo { get { return w != null; } }
        /// <summary>Il file del registro di questa sessione (per la linguetta Registro e il pulsante "Apri la cartella").</summary>
        public static string File_ { get { return file; } }
        public static string Cartella { get; private set; }
        /// <summary>Ultima azione dell'utente: compare nelle righe BLOCCO.</summary>
        public static string UltimaAzione { get; private set; }

        static string ScegliCartella()
        {
            foreach (string d in new[] { Path.Combine(AppPaths.AppDir, "Registri"),
                                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YA-NEC", "Registri") })
            {
                try
                {
                    Directory.CreateDirectory(d);
                    string prova = Path.Combine(d, ".prova");
                    File.WriteAllText(prova, "");
                    File.Delete(prova);
                    return d;
                }
                catch { }
            }
            return null;
        }

        /// <summary>Avvia il registro della sessione. copia: un secondo file (data/registro.txt per il collaudo), riscritto da capo.</summary>
        static bool avviato;

        public static void Avvia(string copia = null, string modo = "interfaccia grafica")
        {
            if (avviato) return;
            avviato = true;
            lock (lk)
            {
                try
                {
                    if (w == null)
                    {
                        Cartella = ScegliCartella();
                        if (Cartella != null)
                        {
                            file = Path.Combine(Cartella, "YA-NEC_registro_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + Environment.ProcessId + ".txt");   // 0.5.0: piu' finestre aperte insieme
                            w = Apri(file);
                            Pulisci();
                        }
                    }
                    if (copia != null && wCopia == null) wCopia = Apri(copia);
                    if (w == null && wCopia != null) { w = wCopia; wCopia = null; file = copia; }
                }
                catch { }
            }
            if (w == null) return;
            Intestazione("REGISTRO DI YA-NEC " + AppPaths.Version + " - " + modo + " - avviato " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                "Cartella dati: " + AppPaths.DataDir + Environment.NewLine +
                "Colonne: numero, ora, millisecondi dalla riga precedente, categoria, messaggio." + Environment.NewLine + new string('-', 100));
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Scrivi("ECCEZIONE", e.ExceptionObject.ToString());
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) => Scrivi("ECCEZIONE", e.Exception.ToString());
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Chiudi();
            // la descrizione del computer (il tipo di disco puo' chiedere qualche secondo) e i campioni delle risorse: in sottofondo
            System.Threading.Tasks.Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                foreach (string r in Prestazioni.Computer().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) Scrivi("COMPUTER", r);
                Scrivi("COMPUTER", "descrizione del computer letta in " + sw.ElapsedMilliseconds + " ms");
                Prestazioni.AvviaCampioni();
            });
        }

        static StreamWriter Apri(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            return new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
        }

        static void Intestazione(string t)
        {
            lock (lk)
            {
                try { w?.WriteLine(t); wCopia?.WriteLine(t); } catch { }
            }
        }

        // tiene gli ultimi 30 registri
        static void Pulisci()
        {
            try
            {
                FileInfo[] f = new DirectoryInfo(Cartella).GetFiles("YA-NEC_registro_*.txt").OrderByDescending(x => x.Name).ToArray();
                for (int i = 30; i < f.Length; i++) { try { f[i].Delete(); } catch { } }
            }
            catch { }
        }

        /// <summary>Scrive una riga numerata, con i millisecondi trascorsi dalla riga precedente.</summary>
        public static void Scrivi(string categoria, string msg)
        {
            if (w == null || chiuso) return;
            lock (lk)
            {
                try
                {
                    passo++;
                    long ora = Stopwatch.GetTimestamp();
                    double dt = ultimo == 0 ? 0 : (ora - ultimo) * 1000.0 / Stopwatch.Frequency;
                    ultimo = ora;
                    string riga = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,6}  {1:HH:mm:ss.fff}  +{2,7:0}  {3,-9} {4}", passo, DateTime.Now, dt, categoria, msg);
                    w.WriteLine(riga);
                    wCopia?.WriteLine(riga);
                }
                catch { }
            }
            if (categoria == "AZIONE") { UltimaAzione = msg; Interfaccia.MisuraRisposta(msg); }
        }

        /// <summary>Punteggio delle funzioni in fondo al registro e in Registri/punteggio_funzioni.txt.</summary>
        public static void ScriviPunteggio()
        {
            if (w == null) return;
            string p = Prestazioni.Punteggio();
            Scrivi("RISORSE", Prestazioni.Risorse());
            Intestazione(new string('-', 100) + Environment.NewLine + p);
            try { if (Cartella != null) File.WriteAllText(Path.Combine(Cartella, "punteggio_funzioni.txt"), "YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + p, new UTF8Encoding(false)); } catch { }
        }

        public static void Chiudi()
        {
            if (w == null || chiuso) return;
            ScriviPunteggio();
            Prestazioni.FermaCampioni();
            lock (lk)
            {
                chiuso = true;
                try { w.Dispose(); } catch { }
                try { wCopia?.Dispose(); } catch { }
            }
        }
    }
}
