// YA-NEC 0.5.3 - collaudo delle novita': elenco dei cavi (YAEC + altre impedenze), cambio d'impedenza con un tratto di cavo e linea nel
// modello, balun con tutti i rapporti e i nuclei in polvere di ferro, quota dell'antenna sul terreno.
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using YaNec.Engine;

namespace YaNec
{
    public static partial class Collaudo
    {
        static void Novita053()
        {
            // 1. elenco dei cavi
            List<Cable> cv = Cables.Load(Path.Combine(AppPaths.DataDir, "cavi.csv"));
            int nYaec = cv.FindAll(c => (c.Source ?? "").StartsWith("YAEC")).Count;
            bool zDiverse = cv.Exists(c => c.Z0 == 93) && cv.Exists(c => c.Z0 == 125) && cv.Exists(c => c.Z0 == 450) && cv.Exists(c => c.Z0 == 600) && cv.Exists(c => c.Z0 == 300);
            bool tutti = cv.TrueForAll(c => c.Vf > 0.5 && c.Vf < 1.0 && c.AttenuationAt(14.2) > 0 && c.AttenuationAt(1296) > c.AttenuationAt(50));
            Passo("elenco dei cavi: database di YAEC e altre impedenze", "oltre 80 cavi, 42 da YAEC, 93/125/300/450/600 ohm, VF e perdite valide",
                cv.Count + " cavi, " + nYaec + " da YAEC, altre impedenze " + (zDiverse ? "presenti" : "mancanti") + ", dati " + (tutti ? "validi" : "NON validi"),
                cv.Count >= 80 && nYaec == 42 && zDiverse && tutti);

            // 2. trasformatore a quarto d'onda: delta loop da 100 ohm con RG-59 (esempio di IC8TEM)
            Cable rg59 = cv.Find(c => c.Name.StartsWith("RG-59/U (Belden 8241)"));
            if (rg59 != null)
            {
                double f = 14.2, len = LineCalc.Wavelength(f, rg59.Vf) / 4;
                Complex zin = LineCalc.InputImpedance(new Complex(100, 0), rg59.Z0, f, rg59.Vf, rg59.AttenuationAt(f), len);
                Passo("delta loop 100 ohm + lambda/4 di RG-59 a 14,2 MHz", "circa 75^2/100 = 56,25 ohm, lunghezza 3,48 m",
                    string.Format(CI, "{0}, lunghezza {1:0.000} m, ROS su 50 {2:0.00}", Z(zin), len, LineCalc.Swr(zin, 50)),
                    Math.Abs(zin.Real - 56.25) < 1.0 && Math.Abs(zin.Imaginary) < 1.0 && Math.Abs(len - 3.484) < 0.01);
                double ideale = QuarterWaveMatch.IdealZ0(100, 50);
                Passo("Z0 ideale del trasformatore 100 -> 50 ohm", "70,71 ohm", ideale.ToString("0.00", CI) + " ohm", Math.Abs(ideale - 70.71) < 0.01);
            }
            else Passo("RG-59/U nell'elenco", "presente", "mancante", false);

            // 3. linea nel modello: dipolo + lambda/4 da 75 ohm (TL del NEC) = 75^2 / Z del dipolo
            {
                string nec = "CM dipolo 14,2 MHz\nCE\nGW 1 31 0 -5.15 10 0 5.15 10 0.001\nGE 0\nEX 0 1 16 0 1 0\nFR 0 1 0 0 14.2 0\nEN\n";
                NecModel d = NecModel.Parse(nec);
                RunResult r0 = Analysis.Run(d, d.BuildGeometry(), 14.2, 2.0);
                string err;
                NecModel m = LineaAdattamento.Inserisci(d, 75, LineCalc.C0 / 14.2 / 4, 14.2, out err);
                if (m == null) { Passo("linea TL nel modello", "modello con la linea", err, false); }
                else
                {
                    RunResult r1 = Analysis.Run(m, m.BuildGeometry(), 14.2, 2.0);
                    Complex att = 75.0 * 75.0 / r0.Zin;
                    double scarto = Complex.Abs(r1.Zin - att) / Complex.Abs(att);
                    Passo("dipolo con lambda/4 da 75 ohm messa nel modello (TL + filo corto con la sorgente)", "Z = 75^2 / Zdipolo entro il 3 %",
                        "Zdipolo " + Z(r0.Zin) + ", attesa " + Z(att) + ", ottenuta " + Z(r1.Zin) + string.Format(CI, " (scarto {0:0.0} %)", scarto * 100),
                        scarto < 0.03 && m.Networks.Count == 1 && m.Sources[0].Tag == 2);
                }
                string e2;
                NecModel due = NecModel.Parse(nec.Replace("EX 0 1 16 0 1 0", "EX 0 1 16 0 1 0\nEX 0 1 10 0 1 0"));
                Passo("linea nel modello con due sorgenti", "rifiutata con spiegazione", LineaAdattamento.Inserisci(due, 75, 5, 14.2, out e2) == null ? "rifiutata: " + e2 : "accettata", LineaAdattamento.Inserisci(due, 75, 5, 14.2, out e2) == null);
            }

            // 4. balun
            Func<int, int, double, CalcOutput> balun = (rap, nucleo, fMHz) =>
            {
                CalcBalun b = new CalcBalun();
                b.Par("r").Value = rap; b.Par("m").Value = nucleo; b.Par("f").Value = fMHz;
                return b.Compute();
            };
            CalcOutput o49 = balun(7, 2, 7.0), o64 = balun(8, 2, 7.0), oTr = balun(6, 2, 3.5), o41 = balun(2, 2, 3.5), oT200 = balun(0, 6, 3.5), oT400 = balun(2, 10, 3.5), o91 = balun(4, 2, 3.5);
            Passo("balun 49:1 EFHW su FT-240-43", "secondario 7 volte il primario", o49.Balun.Np + ":" + o49.Balun.Ns, o49.Balun.Ns == 7 * o49.Balun.Np);
            Passo("balun 64:1 EFHW su FT-240-43 (richiesta del 15/11 19:55)", "secondario 8 volte il primario", o64.Balun.Np + ":" + o64.Balun.Ns, o64.Balun.Ns == 8 * o64.Balun.Np);
            Passo("triplo 4 / 9 / 16 su FT-240-43 a 3,5 MHz", "prese a 2, 3 e 4 volte il primario (3: 6 / 9 / 12)", string.Join(" / ", Array.ConvertAll(oTr.Balun.Prese, x => x.ToString(CI))),
                oTr.Balun.Prese.Length == 4 && oTr.Balun.Prese[1] == 2 * oTr.Balun.Prese[0] && oTr.Balun.Prese[2] == 3 * oTr.Balun.Prese[0] && oTr.Balun.Prese[3] == 4 * oTr.Balun.Prese[0] && oTr.Balun.Prese[0] == 3);
            Passo("4:1 Guanella su FT-240-43 a 3,5 MHz, 50 ohm", "2 nuclei, 3 spire (L minima 9,09 uH, AL 1075)", o41.Balun.Nuclei + " nuclei, " + o41.Balun.Np + " spire", o41.Balun.Nuclei == 2 && o41.Balun.Np == 3);
            Passo("1:1 su T-200-2 a 3,5 MHz (polvere di ferro, richiesta del 15/11 19:55)", "28 spire (AL 120 uH/100 spire)", oT200.Balun.Np + " spire", oT200.Balun.Np == 28 && oT200.Balun.Polvere);
            Passo("4:1 su T-400-2 a 3,5 MHz", "23 spire (AL 180 uH/100 spire)", oT400.Balun.Np + " spire", oT400.Balun.Np == 23);
            Passo("9:1 trifilare su FT-240-43", "impedenza lato antenna 450 ohm", o91.Cards.Count > 0 ? o91.Cards[0].Value : "-", o91.Cards.Count > 0 && o91.Cards[0].Value.StartsWith("450"));
            CalcOutput oEfT = balun(7, 6, 7.0);
            Passo("EFHW su polvere di ferro", "avviso: per l'EFHW si usa la ferrite 43", oEfT.Warnings.Count > 0 ? oEfT.Warnings[0] : "nessun avviso", oEfT.Warnings.Exists(w => w.Contains("ferrite 43")));
            int disegni = 0;
            for (int i = 0; i < CalcBalun.Rapporti.Length; i++) { BalunSchema s = CalcBalun.Esempio(i); if (s != null && s.Np > 0 && s.Tipo != null) disegni++; }
            Passo("disegni di costruzione di tutti i rapporti", CalcBalun.Rapporti.Length + " disegni", disegni + " disegni", disegni == CalcBalun.Rapporti.Length);

            // 6. lingue e colori (0.5.3)
            {
                string prima = Lang.Code;
                Lang.Load("en");
                string t1 = Lang.T("Calcola"), t2 = Lang.T("testo che non e' una chiave");
                Lang.Load(prima);
                Passo("traduzione inglese (data/lingue/en.txt)", "\"Calcola\" -> \"Calculate\"; un testo senza traduzione resta in italiano", t1 + " / " + t2, t1 == "Calculate" && t2 == "testo che non e' una chiave");
                Passo("elenco delle lingue", "37 lingue (come YAEC: le 36 di YA-MS, E-LAYER ANALYZER, LIVETROPOVHF e lo slovacco)", Lang.Languages.Length + " lingue", Lang.Languages.Length == 37);
                int prev = Pal.Schema;
                bool okT = true;
                for (int i = 0; i < Pal.SchemaNames.Length; i++) { Pal.Load(i); okT &= Pal.Schema == i && Pal.Dark == (i == 1 || i == 2 || i == 11 || i >= 13); }
                Pal.Load(prev);
                Passo("schemi di colore armonizzati", "18 schemi come E-LAYER ANALYZER (Chiaro, Notte, Nero, 8 pastelli, Scia meteorica, 3 di IP-TRX-CTRL, Blu, Viola, Verde); chiari e scuri giusti", Pal.SchemaNames.Length + " schemi: " + string.Join(", ", Pal.SchemaNames), okT && Pal.SchemaNames.Length == 18);
            }

            // 7. modifica Leeson A/B (via di IC8TEM il 15/11/2026 21:57)
            {
                // elemento a due diametri (12 e 25 mm, 10,4 m, risonanza circa 14 MHz) calcolato a 7 MHz: f/fs circa 0,5
                string nec = "CM gradini lontano dalla risonanza\nCE\nGW 1 7 0 -5.2 10 0 -2.5 10 0.006\nGW 2 13 0 -2.5 10 0 2.5 10 0.0125\nGW 3 7 0 2.5 10 0 5.2 10 0.006\nGE 0\nEX 0 2 7 0 1 0\nFR 0 1 0 0 7.0 0\nEN\n";
                NecModel g = NecModel.Parse(nec);
                List<string> note = new List<string>();
                Leeson.Applica(g, 7.0, note);
                bool corretto = note.Exists(n => n.Contains("diametro uniforme"));
                Passo("Leeson A: elemento a gradini a meta' della sua risonanza (f/fs circa 0,5)", "corretto (sotto la risonanza si corregge sempre; prima: 'lontano dalla risonanza')",
                    note.Count > 0 ? note[0] : "nessuna nota", corretto);
                Passo("Leeson B: taratura sotto la risonanza", "lunghezza 0,05, diametro 0 (fase 2L bis del collaudo 0.5.3, scelta di IC8TEM: solo lunghezza)",
                    string.Format(CI, "lunghezza {0:0.00}, diametro {1:0.00}", Leeson.TaraLunghezza, Leeson.TaraDiametro), Math.Abs(Leeson.TaraLunghezza - 0.05) < 1e-12 && Leeson.TaraDiametro == 0);
            }

            // 5. quota: l'antenna si sposta in verticale al centimetro
            {
                NecModel q = NecModel.Parse("CM dipolo a terra\nCE\nGW 1 21 0 -0.5 0 0 0.5 0 0.002\nGE 1\nGN 2 0 0 0 13 0.005\nEX 0 1 11 0 1 0\nFR 0 1 0 0 144.3 0\nEN\n");
                NecModel q2 = TrasformaGeometria.Applica(q, TrasformaGeometria.Sposta(0, 0, 2.35), null);
                double z = TrasformaGeometria.ZMin(q2.BuildGeometry());
                Passo("quota dell'antenna: dipolo dal suolo a 2,35 m", "punto piu' basso a 2,35 m", z.ToString("0.000", CI) + " m", Math.Abs(z - 2.35) < 1e-6);
            }
        }
    }
}
