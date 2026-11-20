// YA-NEC 1.5.3 - passo degli angoli del diagramma 3D adattato al computer (proposta del 20/11/2026 12:3x, approvata da IC8TEM alle 12:36:
// «esatto, con una colorazione rossa per la segnalazione più vicina»; «perfetto» per sweep e ottimizzatore a 2 gradi; «lo riporti nel registro»).
// Il costo del campo lontano e' segmenti x direzioni; la velocita' del computer (segmenti*direzioni al secondo, con tutti i processori del
// calcolo) si misura su ogni diagramma calcolato e si tiene come media in config.ini. Al primo avvio, senza misure, una prova di un decimo
// di secondo su un dipolo. Con «auto» si usa il passo piu' fine che costa meno di AutoMax secondi; un passo che costerebbe piu' di
// Limite secondi non si puo' scegliere. Sweep e ottimizzatore restano a 2 gradi (o 5 per i modelli grandi).
using System;
using System.Globalization;

namespace YaNec.Engine
{
    public static class PassoAngoli
    {
        public static readonly double[] Passi = { 0.5, 1, 2, 5 };
        /// <summary>0 = auto, altrimenti il passo scelto in gradi.</summary>
        public static double Scelta = 0;
        /// <summary>Segmenti*direzioni al secondo misurati (NaN = non ancora misurato).</summary>
        public static double Velocita = double.NaN;
        public const double AutoMax = 0.5, Rosso = 1.0, Limite = 10.0;
        /// <summary>Operazioni in virgola mobile per segmento e direzione (esponenziale complesso e somme): stima per la riga del registro.</summary>
        public const double FlopPerPunto = 60;
        /// <summary>Chiamata quando cambia la velocita' (per salvarla in config.ini).</summary>
        public static Action<double> Salva;

        public static int Direzioni(double passo, bool terreno)
        {
            return ((int)Math.Round((terreno ? 90.0 : 180.0) / passo) + 1) * (int)Math.Round(360.0 / passo);
        }

        /// <summary>Tempo previsto in secondi (NaN se la velocita' non e' nota).</summary>
        public static double Previsto(int segmenti, double passo, bool terreno)
        {
            if (double.IsNaN(Velocita) || Velocita <= 0) return double.NaN;
            return (double)segmenti * Direzioni(passo, terreno) / Velocita;
        }

        /// <summary>Misura di un diagramma: aggiorna la media (pesa di piu' i calcoli lunghi, i brevi sono rumorosi).</summary>
        static readonly object blocco = new object();

        public static void Registra(int segmenti, int direzioni, double secondi)
        {
            lock (blocco) RegistraDentro(segmenti, direzioni, secondi);
        }

        static void RegistraDentro(int segmenti, int direzioni, double secondi)
        {
            if (segmenti <= 0 || direzioni <= 0 || secondi < 0.02) return;
            double v = (double)segmenti * direzioni / secondi;
            if (double.IsNaN(Velocita) || Velocita <= 0) Velocita = v;
            else
            {
                double w = Math.Min(0.5, 0.1 + secondi);   // da 0,1 a 0,5
                Velocita = Math.Exp((1 - w) * Math.Log(Velocita) + w * Math.Log(v));
            }
            if (Salva != null) try { Salva(Velocita); } catch { }
        }

        /// <summary>Passo da usare per il diagramma 3D del modello: la scelta fissa, oppure con «auto» il piu' fine sotto AutoMax.</summary>
        public static double Passo(int segmenti, bool terreno)
        {
            if (Scelta > 0)
            {
                double t = Previsto(segmenti, Scelta, terreno);
                return double.IsNaN(t) || t <= Limite ? Scelta : PiuFineEntro(segmenti, terreno, Limite);
            }
            return PiuFineEntro(segmenti, terreno, AutoMax);
        }

        static double PiuFineEntro(int segmenti, bool terreno, double sec)
        {
            if (double.IsNaN(Velocita)) return 2.0;
            foreach (double p in Passi) if (Previsto(segmenti, p, terreno) <= sec) return p;
            return 5.0;
        }

        /// <summary>Riga per il registro: velocita', GFLOPS stimati e tempi previsti per il modello aperto.</summary>
        public static string Descrizione(int segmenti, bool terreno)
        {
            CultureInfo cc = CultureInfo.CurrentCulture;
            if (double.IsNaN(Velocita)) return "velocita' del campo lontano non ancora misurata";
            string s = "campo lontano: " + (Velocita / 1e6).ToString("0.0", cc) + " milioni di segmenti*direzioni al secondo (circa " +
                (Velocita * FlopPerPunto / 1e9).ToString("0.0", cc) + " GFLOPS utili, " + Processori.Calcolo + " processori)";
            if (segmenti > 0)
            {
                s += "; modello di " + segmenti + " segmenti" + (terreno ? " con terreno" : "") + ":";
                foreach (double p in Passi) s += " " + p.ToString("0.#", cc) + " gradi " + Tempo(Previsto(segmenti, p, terreno)) + ";";
                s += " scelta " + (Scelta > 0 ? Scelta.ToString("0.#", cc) + " gradi" : "auto") + " -> " + Passo(segmenti, terreno).ToString("0.#", cc) + " gradi";
            }
            return s;
        }

        public static string Tempo(double t)
        {
            CultureInfo cc = CultureInfo.CurrentCulture;
            if (double.IsNaN(t)) return "?";
            return t < 1 ? (t * 1000).ToString("0", cc) + " ms" : t.ToString(t < 10 ? "0.0" : "0", cc) + " s";
        }
    }
}
