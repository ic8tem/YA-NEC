// YA-NEC 1.5.6 - pulsanti di rimando tra i Calcolatori (bozzetto Filari approvato da IC8TEM il 21/11/2026 21:12: «la scheda dei balun
// 1 2 4 6 9 16 49 64 la lasci dov'e'. metti una scheda nelle antenne hf dedita alla efhw, e con i bottoni rimandi alla scheda dei
// toroidi. vale anche per delta, ocf, windom, etc.»). Ogni pulsante apre la scheda di destinazione con i valori gia' scritti.
using System;
using System.Collections.Generic;

namespace YaNec.Engine
{
    /// <summary>Pulsante "Testo ›" che apre la voce Voce dei Calcolatori con i parametri Valori.</summary>
    public class CalcRimando
    {
        public string Testo, Voce, Tip;
        public Dictionary<string, double> Valori = new Dictionary<string, double>();
    }

    public static class RimandiCalcolatori
    {
        // indici della scheda Balun / Un-Un (CalcBalun.Rapporti)
        const int B11 = 0, B41 = 2, B61 = 3, B91 = 4, B491 = 7, B641 = 8;

        static CalcRimando Balun(int r, double f, double fh, double pw, string testo, int nucleo = -1, int tipo = 0)
        {
            CalcRimando b = new CalcRimando { Testo = testo, Voce = "balun", Tip = "Apre Strumenti > Balun / Un-Un con rapporto, frequenze e potenza di questa scheda." };
            b.Valori["r"] = r; b.Valori["f"] = Math.Round(f, 3); b.Valori["fh"] = Math.Round(Math.Max(f, fh), 3); b.Valori["p"] = pw; b.Valori["t"] = tipo;
            if (nucleo >= 0) b.Valori["m"] = nucleo;
            return b;
        }

        /// <summary>Choke sul coassiale: FT-240-43 con le spire per 1000 ohm alla frequenza piu' bassa.</summary>
        public static CalcRimando Choke(double f)
        {
            double al = 1075e-9;   // FT-240-43
            double n = Math.Max(4, Math.Min(14, Math.Ceiling(Math.Sqrt(1000 / (2 * Math.PI * f * 1e6 * al)))));
            CalcRimando c = new CalcRimando { Testo = "Choke sul coassiale ›", Voce = "toroidiFT", Tip = "Apre Strumenti > Toroidi FT con un FT-240-43 e le spire per oltre 1000 Ω alla frequenza piu' bassa." };
            c.Valori["m"] = 0; c.Valori["t"] = 5; c.Valori["n"] = n; c.Valori["f"] = Math.Round(f, 3);
            return c;
        }

        static CalcRimando QuartoOnda(double f, double r)
        {
            CalcRimando c = new CalcRimando { Testo = "Trasformatore λ/4 ›", Voce = "cavi", Tip = "Apre Strumenti > Cavi RG con un quarto d'onda (VF 0,66) chiuso sull'impedenza dell'antenna: scegli un cavo da 75 Ω." };
            c.Valori["f"] = Math.Round(f, 3); c.Valori["l"] = Math.Round(299.792458 / f / 4 * 0.66, 3); c.Valori["rl"] = Math.Round(r, 1); c.Valori["xl"] = 0;
            return c;
        }

        public static void Aggiungi(SimpleCalc sc, CalcOutput o)
        {
            List<CalcRimando> r = o.Rimandi;
            double f = sc.P("f");
            switch (sc.Key)
            {
                case "filari":
                    {
                        int tipo = sc.I("tipo");
                        double pw = Math.Max(5, sc.P("pw"));
                        int nucleo = pw <= 15 ? 1 : (pw <= 120 ? 2 : 3);
                        bool conBob = tipo != 2 && sc.I("bob") == 1;   // 22/11/2026: bobina facoltativa nell'EFHW
                        if (tipo == 2)
                        {
                            double fl = Math.Max(1, sc.P("fl"));
                            bool quattro = sc.I("trb") == 1;
                            r.Add(Balun(quattro ? B41 : B91, fl, 30, pw, quattro ? "Costruisci l'un-un 4:1 ›" : "Costruisci l'un-un 9:1 ›", nucleo, 1));
                            r.Add(Choke(fl));
                        }
                        else
                        {
                            double fb = conBob ? Math.Min(sc.P("fb"), f) : f;
                            bool s64 = sc.I("tr") == 1;
                            r.Add(Balun(s64 ? B641 : B491, fb, 30, pw, s64 ? "Costruisci il trasformatore 64:1 ›" : "Costruisci il trasformatore 49:1 ›", nucleo));
                            r.Add(Choke(fb));
                            if (conBob) r.Add(new CalcRimando { Testo = "Bobina di carico ›", Voce = "carichihf", Tip = "Apre HF > Carichi HF (bobine con autorisonanza, Q, tensioni e perdite)." });
                        }
                        break;
                    }
                case "ocf":
                    r.Add(Balun(B41, f, 30, 100, "Balun 4:1 ›"));
                    r.Add(Balun(B61, f, 30, 100, "Balun 6:1 ›"));
                    r.Add(Choke(f));
                    break;
                case "dipolo": case "vinv":
                    r.Add(Balun(B11, f, f, 100, "Balun 1:1 ›"));
                    break;
                case "trappolato":
                    {
                        double fl = sc.I("modo") == 0 ? sc.P("f2") : sc.P("fs"), fh = sc.I("modo") == 0 ? sc.P("f1") : sc.P("fs");
                        r.Add(Balun(B11, fl, fh, 100, "Balun 1:1 ›"));
                        break;
                    }
                case "ripiegato":
                    r.Add(Balun(B41, f, f, 100, "Balun 4:1 ›"));
                    break;
                case "t2fd":
                    r.Add(Balun(B91, f, 30, Math.Max(5, sc.P("pw")), "Balun 9:1 ›"));
                    break;
                case "g5rv":
                    r.Add(Choke(3.5));
                    break;
                case "delta":
                    r.Add(Balun(B11, f, f, 100, "Balun 1:1 ›"));
                    r.Add(QuartoOnda(f, 110));
                    break;
                case "looprett":
                    r.Add(Balun(B41, f, f, 100, "Balun 4:1 ›"));
                    r.Add(Choke(f));
                    break;
                case "skywire":
                    {
                        double fs = 306.3 / Math.Max(1, sc.P("per"));
                        r.Add(Balun(B41, fs, 30, 100, "Balun 4:1 ›"));
                        r.Add(Choke(fs));
                        break;
                    }
                case "quad":
                    r.Add(Balun(B11, f, f, 100, "Balun 1:1 ›"));
                    r.Add(QuartoOnda(f, 110));
                    break;
                case "quadmulti":
                    r.Add(Balun(B11, 14.1, 29, 100, "Balun 1:1 ›"));
                    break;
                case "moxon":
                    r.Add(Choke(f));
                    break;
                case "hexbeam":
                    r.Add(Choke(14.1));
                    break;
            }
        }
    }
}
