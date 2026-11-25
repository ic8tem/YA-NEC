// YA-NEC 1.7.0 - zona «Consigli» (bozzetto «mining riviste» U, approvato da IC8TEM il 25/11/2026 20:41).
// Schede brevi: titolo, 3-4 righe, fonte e voce dei Calcolatori che mostra i numeri. Le stesse schede compaiono in fondo ai
// calcolatori che riguardano. Tutte riassunte dalle fonti gia' citate (Citazioni.md), mai copiate.
using System.Collections.Generic;

namespace YaNec.Engine
{
    public class Consiglio
    {
        public string Titolo, Testo, Fonte;
        public string[] Voci;          // chiavi dei calcolatori (SimpleCalc.Key) in cui la scheda compare; la prima e' quella del pulsante «apri»
        public string[] VociNomi;      // nomi mostrati sui pulsanti
    }

    public static class Consigli
    {
        public static readonly List<Consiglio> Tutti = new List<Consiglio>
        {
            new Consiglio
            {
                Titolo = "ROS basso non vuol dire antenna efficiente",
                Testo = "La resistenza vista dal trasmettitore e' radiazione piu' perdite (conduttore, bobine, terreno): un carico fittizio ha ROS 1. " +
                        "Sotto 0,2 λ un dipolo orizzontale perde molto nel terreno; su un'antenna accorciata il cappello in testa rende piu' della bobina alla base.",
                Fonte = "I5WHC, RadioRivista 4/2025; ON4UN, Low-Band DXing; DJ8WL",
                Voci = new[] { "caricata", "gp", "caricolin", "quarto" }, VociNomi = new[] { "Verticale accorciata", "Ground plane", "Carico lineare", "λ/4 e radiali" }
            },
            new Consiglio
            {
                Titolo = "ROS 2 non e' sempre uguale",
                Testo = "Lo stesso ROS si ha con R sotto o sopra Z0. Con R bassa (per esempio 25 Ω su 50) a pari potenza la corrente nei finali sale di circa il 40-50 %: " +
                        "un finale si e' guastato a 26-27 Ω con l'allarme fermo a ROS 2. Nel grafico del ROS la curva e' piena dove R < Z0.",
                Fonte = "I5WHC, RadioRivista 9/2026",
                Voci = new[] { "*frequenza" }, VociNomi = new[] { "Diagrammi › Parametri" }
            },
            new Consiglio
            {
                Titolo = "Il nucleo del trasformatore EFHW scalda",
                Testo = "Il trasformatore 49:1 non e' ideale: un FT-240-43 ha circa il 75 % di efficienza, quindi a 100 W in FT8 dissipa una ventina di watt. " +
                        "I nuclei piccoli vanno bene solo in QRP.",
                Fonte = "DG1JAN; OH7SV; RadioKit 6/2023",
                Voci = new[] { "filari" }, VociNomi = new[] { "Filari" }
            },
            new Consiglio
            {
                Titolo = "Il fattore di accorciamento di un filo si misura sul posto",
                Testo = "Altezza, alberi, tetto e inclinazione cambiano la velocita' dell'onda sul filo: il valore giusto si trova con il ROS, accorciando a piccoli passi " +
                        "(nell'esempio dell'articolo 0,93). Il calcolo da' il punto di partenza.",
                Fonte = "RadioKit Elettronica 1/2024 (EFLW)",
                Voci = new[] { "eflw", "filari" }, VociNomi = new[] { "Filari › risonanze in armonica", "Filari" }
            },
        };

        /// <summary>Schede che riguardano il calcolatore con questa chiave.</summary>
        public static List<Consiglio> Per(string key)
        {
            List<Consiglio> l = new List<Consiglio>();
            foreach (Consiglio c in Tutti) if (System.Array.IndexOf(c.Voci, key) >= 0) l.Add(c);
            return l;
        }
    }
}
