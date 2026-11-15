// YA-NEC 0.5.0 - Calcolatori > "Antenna su tetto" (proposta B approvata da IC8TEM il 15/11/2026 15:32, "si entrambe"): l'antenna del modello
// aperto sopra il solaio di un fabbricato, calcolata in due casi affiancati:
//  - caso HF: il solaio e' sottile rispetto a lambda e quasi trasparente; l'antenna vede il terreno vero sotto il fabbricato, alla quota
//    totale (quota del solaio + quota dell'antenna sopra il solaio), con Sommerfeld;
//  - caso VHF/UHF: il solaio (con la rete elettrosaldata) fa da piano riflettente sotto l'antenna: terreno "solaio in calcestruzzo"
//    (Engine/TerreniEdifici.cs, er e sigma alla frequenza) oppure terreno perfetto se la rete e' fitta rispetto a lambda.
// La verita' sta in mezzo e dipende da frequenza, spessore, rete e dimensioni del tetto: i due casi danno i limiti. Il modello aperto non
// viene toccato; "Apri come modello" apre il caso scelto nelle pillole.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public class CalcTetto : SimpleCalc
    {
        static readonly double[][] Terreni = { new[] { 13, 0.005 }, new[] { 13, 0.002 }, new[] { 3, 0.001 }, new[] { 20, 0.0303 } };
        string[] esito;      // risultati dei due casi (pulsante "Calcola i due casi")
        string firma;
        public override string TuneLabel { get { return "Calcola i due casi (NEC)"; } }

        public CalcTetto()
        {
            Key = "tetto"; Title = "Antenna su tetto (solaio)"; Family = "Strumenti";
            Source = "IC8TEM 15/11/2026 (proposta B); ITU-R P.2040 (calcestruzzo, 1-100 GHz); Perotoni e altri, ACES Journal 34(10), 2019 (calcestruzzo 0,1-2 MHz); terreni tipici dell'ARRL";
            Params.Add(new CalcParam("hs", "Quota del solaio da terra", 15, "m", "Altezza del tetto (del solaio) sopra il terreno vero intorno al fabbricato."));
            Params.Add(new CalcParam("dz", "Quota da aggiungere al modello", 0, "m", "Le quote z del modello aperto si intendono misurate dal solaio. Se il modello e' in spazio libero attorno a z = 0, scrivi qui l'altezza dell'antenna sopra il solaio."));
            Params.Add(CalcParam.Scelta("sol", "Solaio (caso VHF/UHF)", 0, "Calcestruzzo con i valori ITU-R P.2040 o di Perotoni alla frequenza, oppure terreno perfetto se la rete elettrosaldata ha maglie piccole rispetto a lambda (meno di lambda/10).",
                "Calcestruzzo VHF-UHF (ITU)", "Calcestruzzo HF (Perotoni)", "Rete fitta (terreno perfetto)"));
            Params.Add(CalcParam.Scelta("terr", "Terreno sotto il fabbricato (caso HF)", 0, "Terreno vero intorno al fabbricato, con Sommerfeld.", "Medio", "Povero", "Citta'", "Molto buono"));
            Params.Add(CalcParam.Scelta("caso", "Caso nel modello NEC della scheda", 0, "Il caso che il pulsante \"Crea il modello NEC\" e \"Apri come modello\" usano.", "HF: terreno vero", "VHF/UHF: solaio"));
            UsesModel = true;
        }

        string Firma() { string s = Model == null ? "" : Model.ToNec(); foreach (CalcParam p in Params) if (p.Key != "caso") s += p.Key + "=" + p.Value + ";"; return s; }

        double QuotaMinima(NecModel m)
        {
            double zmin = double.MaxValue;
            SegmentGeometry g = m.BuildGeometry();
            for (int i = 0; i < g.N; i++) zmin = Math.Min(zmin, Math.Min(g.Z1[i], g.Z2[i]));
            return zmin;
        }

        NecModel Caso(int caso)
        {
            NecModel m = NecModel.Parse(Model.ToNec());
            double dz = P("dz") + (caso == 0 ? Math.Max(0, P("hs")) : 0);
            if (dz != 0) m.Geometry.Add(new GeomCard { Id = "GM", F6 = dz });
            m.GroundFlag = 0;
            if (caso == 0)
            {
                double[] t = Terreni[Math.Max(0, Math.Min(3, I("terr")))];
                m.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = t[0], Sigma = t[1], Sommerfeld = true, Name = "Terreno sotto il fabbricato" };
            }
            else if (I("sol") == 2) m.Ground = new GroundSpec { Kind = GroundKind.Perfect };
            else
            {
                int k = I("sol") + 1;
                double er, sg;
                TerreniEdifici.Valori(k, m.FreqMHz, out er, out sg);
                m.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = er, Sigma = sg, Sommerfeld = true, Name = TerreniEdifici.Nomi[k], Calcestruzzo = k };
            }
            m.Comments.Add(caso == 0 ? "Antenna su tetto, caso HF: terreno vero alla quota totale (solaio " + P("hs").ToString("0.##", CultureInfo.InvariantCulture) + " m)"
                                     : "Antenna su tetto, caso VHF/UHF: solaio sotto l'antenna");
            return m;
        }

        static string Riassunto(RunResult r)
        {
            return "Z " + Calcolatori.Cx(r.Zin) + ", ROS " + Calcolatori.N(r.Swr, 2) + ", G " + Calcolatori.N(r.GmaxDbi, 2) + " dBi a " + Calcolatori.N(r.ElevationOfMax, 1) + " gradi di elevazione";
        }

        public override double Tune(double fmhz)
        {
            if (Model == null) return 1;
            esito = new string[2];
            for (int c = 0; c < 2; c++)
            {
                NecModel m = Caso(c);
                RunResult r = Analysis.Run(m, m.BuildGeometry(), m.FreqMHz, 2.0);
                esito[c] = Riassunto(r);
            }
            firma = Firma();
            return 1;
        }

        public override CalcOutput Compute()
        {
            CalcOutput o = new CalcOutput();
            if (Model == null) { o.Warnings.Add("Apri prima un modello: lo strumento lavora sul modello aperto (che non viene toccato)."); return o; }
            if (firma != null && firma != Firma()) { esito = null; firma = null; }
            double f = Model.FreqMHz, lam = C0 / Math.Max(1e-6, f);
            double zmin = QuotaMinima(Model) + P("dz"), hs = P("hs");
            o.Card("Antenna sopra il solaio", Calcolatori.N(zmin, 2) + " m", "punto piu' basso, " + Calcolatori.N(zmin / lam, 3) + " lambda", true);
            o.Card("Quota totale da terra", Calcolatori.N(zmin + hs, 2) + " m", Calcolatori.N((zmin + hs) / lam, 2) + " lambda");
            o.Card("Caso HF (terreno vero)", esito != null ? esito[0] : "-", "pulsante \"Calcola i due casi (NEC)\"");
            o.Card("Caso VHF/UHF (solaio)", esito != null ? esito[1] : "-", I("sol") == 2 ? "rete fitta: terreno perfetto" : TerreniEdifici.Nomi[I("sol") + 1]);
            if (zmin <= 0) o.Warnings.Add("L'antenna tocca o attraversa il solaio: alza le quote con \"Quota da aggiungere al modello\".");
            if (Model.Ground.Kind != GroundKind.FreeSpace) o.Warnings.Add("Il modello aperto ha gia' un terreno: qui viene sostituito nei due casi (il modello aperto non cambia).");
            double maglia = 0.2;
            if (I("sol") == 2 && maglia > lam / 10) o.Warnings.Add("Rete elettrosaldata tipica a maglie di 20 cm: a " + Calcolatori.N(f, 1) + " MHz sono " + Calcolatori.N(maglia / lam, 2) + " lambda, non e' fitta: il terreno perfetto esagera il riflesso.");
            o.Add("Come leggere i due casi", "in HF il solaio e' sottile rispetto a lambda e l'antenna vede il terreno sotto il fabbricato (caso HF); in VHF/UHF il solaio con la rete fa da piano sotto l'antenna (caso VHF/UHF). La realta' sta tra i due: piu' la frequenza e' alta e il tetto grande, piu' vale il secondo.");
            o.Formula = "Caso HF: modello alzato di (quota del solaio + quota aggiunta), terreno vero con Sommerfeld. Caso VHF/UHF: modello alzato della sola quota aggiunta, solaio come terreno " +
                "(calcestruzzo ITU-R P.2040: er 5,24, sigma 0,0462 f^0,7822 S/m con f in GHz; calcestruzzo HF di Perotoni: er 20, tan delta 0,012; rete fitta: terreno perfetto). " +
                "Il NEC-2 tratta il solaio come terreno omogeneo e infinito: i bordi del tetto, le stanze sotto e la rete non ci sono.";
            o.Nec = Caso(Math.Max(0, Math.Min(1, I("caso")))).ToNec();
            return o;
        }
    }
}
