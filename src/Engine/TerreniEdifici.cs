// YA-NEC 0.5.0 - solaio in calcestruzzo come terreno (IC8TEM 15/11/2026 15:12: "hai mancato il calcestruzzo come terreno: per i solai
// dei fabbricati cosa possiamo fare?"; 15:32 "si entrambe" alle proposte A e B).
// Il NEC-2 vuole un terreno omogeneo e infinito: un solaio di 20-30 cm con la rete elettrosaldata e le stanze sotto non lo e'.
// Per questo i due valori servono solo come lastra sotto un'antenna bassa sul tetto, con un avviso. Fonti (e differenze tra loro):
//  1 "VHF-UHF": ITU-R P.2040 (tabella dei materiali da costruzione): er = 5,24, sigma = 0,0462 f^0,7822 S/m con f in GHz, tarata da
//    1 a 100 GHz; sotto 1 GHz e' un'estrapolazione (144 MHz: 0,0101 S/m; 432 MHz: 0,0240 S/m).
//  2 "HF": Perotoni, Vieira, dos Santos, Almeida, "Electrical Characterization of Concrete Using the Parallel Plate Capacitor Method",
//    ACES Journal 34(10), 2019: misure da 0,1 a 2 MHz, er 17,7-28,3 (fresco) e 19,7-27,3 (asciutto), tangente di perdita 0,008-0,016;
//    qui er = 20 e tan delta = 0,012, sigma = 2 pi f eps0 er tan delta (0,0013 S/m a 1,8 MHz... cresce con la frequenza).
// Le due fonti non si raccordano: a 1 GHz ITU da' er 5,24, a 2 MHz Perotoni circa 20 (il calcestruzzo e' dispersivo, l'acqua nei pori
// pesa di piu' in basso). Tra 2 MHz e 1 GHz non ho trovato misure pubblicate: per questo i terreni sono due, uno per banda.
using System;

namespace YaNec.Engine
{
    public static class TerreniEdifici
    {
        public static readonly string[] Nomi = { "", "Solaio in calcestruzzo, VHF-UHF (ITU-R P.2040)", "Solaio in calcestruzzo, HF (Perotoni 2019)" };

        /// <summary>Costante dielettrica e conducibilita' (S/m) del solaio k alla frequenza f (MHz).</summary>
        public static void Valori(int k, double fmhz, out double er, out double sigma)
        {
            if (k == 2)
            {
                er = 20; const double tanDelta = 0.012;
                sigma = 2 * Math.PI * fmhz * 1e6 * 8.8541878128e-12 * er * tanDelta;
            }
            else
            {
                er = 5.24;
                sigma = 0.0462 * Math.Pow(Math.Max(1e-6, fmhz) / 1000.0, 0.7822);
            }
        }

        /// <summary>Il terreno da usare alla frequenza f: se e' un solaio, con i valori a quella frequenza; altrimenti quello del modello.</summary>
        public static GroundSpec Alla(GroundSpec g, double fmhz)
        {
            if (g == null || g.Kind != GroundKind.Real || g.Calcestruzzo <= 0) return g;
            GroundSpec c = g.Clone();
            double er, s;
            Valori(g.Calcestruzzo, fmhz, out er, out s);
            c.Epsr = er; c.Sigma = s;
            return c;
        }

        public static string Avviso(GroundSpec g, double fmhz)
        {
            if (g == null || g.Kind != GroundKind.Real || g.Calcestruzzo <= 0) return null;
            double er, s;
            Valori(g.Calcestruzzo, fmhz, out er, out s);
            string fuori = g.Calcestruzzo == 1 ? (fmhz < 1000 ? " La formula ITU-R P.2040 e' tarata da 1 a 100 GHz: qui e' estrapolata." : "")
                                                : (fmhz > 2 ? " Le misure di Perotoni arrivano a 2 MHz: qui sono estrapolate." : "");
            return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "Terreno: {0}, er {1:0.##} e sigma {2:0.#####} S/m a {3:0.###} MHz.{4} Il NEC-2 lo tratta come terreno omogeneo e infinito: " +
                "vale solo come lastra sotto un'antenna bassa sul tetto; la rete elettrosaldata e le stanze sotto non ci sono. Per l'antenna alta " +
                "o in HF conviene il terreno vero alla quota totale (Calcolatori > Antenna su tetto).", Nomi[g.Calcestruzzo], er, s, fmhz, fuori);
        }
    }
}
