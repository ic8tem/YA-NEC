// YA-NEC - tempi di reazione dell'interfaccia per il registro (richiesta di IC8TEM del 13/11/2026).
//  - RISPOSTA: dopo ogni azione dell'utente, il tempo finche' l'interfaccia torna libera (disegno e impaginazione compresi);
//  - BLOCCO: un orologio ogni 50 ms sul thread dell'interfaccia; se arriva con piu' di 250 ms di ritardo l'interfaccia era ferma.
// Entrambi entrano anche nel punteggio delle funzioni ("Risposta dell'interfaccia", "Interfaccia ferma").
using System;
using System.Diagnostics;
using Avalonia.Threading;

namespace YaNec
{
    public static class Interfaccia
    {
        static DispatcherTimer guardia;
        static Stopwatch sw;
        static double atteso;
        const double Periodo = 50, Soglia = 250;

        /// <summary>Da chiamare sul thread dell'interfaccia, all'apertura della finestra principale.</summary>
        public static void AvviaGuardia()
        {
            if (guardia != null || !Registro.Attivo) return;
            sw = Stopwatch.StartNew();
            atteso = Periodo;
            guardia = new DispatcherTimer(TimeSpan.FromMilliseconds(Periodo), DispatcherPriority.Background, (s, e) =>
            {
                double ora = sw.Elapsed.TotalMilliseconds, ritardo = ora - atteso;
                atteso = ora + Periodo;
                if (ritardo > Soglia)
                {
                    Prestazioni.Registra("Interfaccia ferma", ritardo, ritardo, 0, 0);
                    Registro.Scrivi("BLOCCO", string.Format(System.Globalization.CultureInfo.InvariantCulture, "interfaccia ferma per {0:0} ms{1} - ultima azione: {2}",
                        ritardo, Prestazioni.Giudizio(ritardo), Registro.UltimaAzione ?? "-"));
                }
            });
            guardia.Start();
        }

        /// <summary>Dopo un'azione: misura quando l'interfaccia torna libera (priorita' ContextIdle, dopo disegno e impaginazione).</summary>
        public static void MisuraRisposta(string azione)
        {
            try
            {
                if (!Dispatcher.UIThread.CheckAccess()) return;
                long t0 = Stopwatch.GetTimestamp();
                Dispatcher.UIThread.Post(() =>
                {
                    double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
                    Prestazioni.Registra("Risposta dell'interfaccia", ms, ms, 0, 0);
                    Registro.Scrivi("RISPOSTA", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0} ms{1} ({2})", ms, Prestazioni.Giudizio(ms),
                        azione.Length > 80 ? azione.Substring(0, 80) + "..." : azione));
                }, DispatcherPriority.ContextIdle);
            }
            catch { }
        }
    }
}
