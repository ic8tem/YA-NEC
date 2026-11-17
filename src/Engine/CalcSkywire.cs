// YA-NEC 1.0.17 - Loop orizzontale multibanda (Loop Skywire). Bozzetto 3 approvato da IC8TEM il 17/11/2026 18:28 ("BOZZETTO3 OK").
// Fonti: D. Fischer W7FB, "The Loop Skywire" (QST 11/1985): perimetro 1005/f piedi, quadrato orizzontale, alimentato in un angolo,
// usato su tutte le bande sopra la fondamentale con l'accordatore; K. Kleinschmidt NT0Z, loop orizzontale con linea a scala e accordatore
// bilanciato (Table 1: perdita del cavo 8214 contro la scala con il ROS alto); S. Ford WB8IMY, "The Lure of the Ladder Line" (QST 12/1993).
// Somiglianze: perimetro, quota e alimentazione come W7FB. Differenze: la perdita della linea non e' "adattata x fattore del ROS" come
// la tabella di NT0Z ma la formula esatta della linea con perdite e disadattamento (LineCalc.PowerEfficiency, le stesse perdite di data/cavi.csv).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public class CalcSkywire : SimpleCalc
    {
        static readonly double[] BandaF = { 1.83, 3.65, 5.359, 7.1, 10.125, 14.175, 18.118, 21.225, 24.94, 28.5 };
        static readonly string[] BandaN = { "160 m", "80 m", "60 m", "40 m", "30 m", "20 m", "17 m", "15 m", "12 m", "10 m" };
        int bandaPrec = -1;
        string firma;
        public List<string[]> Righe;          // banda, Z al loop, ROS sulla linea, perdita, guadagno, elevazione
        public double[] BF, BRos, BPerd, BG, BEl;

        public CalcSkywire()
        {
            Key = "skywire"; Title = "Loop orizzontale multibanda (Loop Skywire)";
            Source = "D. Fischer W7FB, \"The Loop Skywire\" (QST 11/1985); K. Kleinschmidt NT0Z (loop orizzontale, linea a scala, accordatore bilanciato); S. Ford WB8IMY, \"The Lure of the Ladder Line\" (QST 12/1993); ARRL Antenna Book (perimetro 1005/f piedi)";
            Params.Add(CalcParam.Scelta("banda", "Banda piu' bassa", 1, "La banda per cui il perimetro e' un'onda intera: sopra quella il loop lavora su tutte le bande con l'accordatore.", "160", "80", "40"));
            Params.Add(CalcParam.Scelta("forma", "Forma", 0, "W7FB: quadrato. Il cerchio (poligono di 16 lati) ha l'area piu' grande e il guadagno un po' piu' alto; il triangolo serve con tre sostegni.",
                "quadrato", "triangolo", "rettangolo 2:1", "cerchio (poligono)"));
            Params.Add(new CalcParam("per", "Perimetro", 83.8, "m", "1005/f piedi (W7FB) = 306,3/f metri alla frequenza di centro della banda piu' bassa. Si aggiorna quando cambi la banda."));
            Params.Add(new CalcParam("h", "Altezza", 14, "m", "W7FB: il piu' alto possibile; sotto λ/4 della banda bassa irradia quasi solo verso l'alto (NVIS)."));
            Params.Add(new CalcParam("d", "Diametro del filo", 2, "mm"));
            Params.Add(CalcParam.Scelta("linea", "Discesa", 1, "Coassiale: comodo ma con il ROS alto delle bande armoniche perde molto (NT0Z, WB8IMY). Scala: perde poco anche con il ROS alto, vuole un accordatore bilanciato o un balun 4:1 all'accordatore.",
                "coassiale 50 Ω (RG-213)", "scala 450 Ω", "scala 600 Ω"));
            Params.Add(new CalcParam("lun", "Lunghezza della discesa", 30, "m"));
            Params.Add(CalcParam.Scelta("punto", "Punto di alimentazione", 0, "W7FB alimenta in un angolo; a meta' lato cambia poco l'impedenza ma sposta i lobi sulle bande alte.", "angolo", "meta' lato"));
            NotaFissa = "Il loop e' un'onda intera sulla banda piu' bassa e 2, 3, 4... onde sulle altre: l'impedenza al loop cambia molto da banda a banda. " +
                "Per questo si usa la scala con l'accordatore, o il coassiale con l'accordatore al punto di alimentazione. \"Calcola tutte le bande (NEC)\" calcola ogni banda e la perdita della discesa.";
        }

        public override string TuneLabel { get { return "Calcola tutte le bande (NEC)"; } }

        string Firma()
        {
            StringBuilder s = new StringBuilder();
            foreach (CalcParam p in Params) s.Append(p.Key).Append('=').Append(p.Value.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            return s.ToString();
        }

        int Indice0() { int b = I("banda"); return b == 0 ? 0 : (b == 1 ? 1 : 3); }

        /// <summary>Linea scelta: dall'abaco (data/cavi.csv) se c'e', altrimenti le stesse righe scritte qui.</summary>
        public Cable Linea()
        {
            int l = I("linea");
            string cerca = l == 0 ? "RG-213" : (l == 1 ? "450" : "600");
            foreach (Cable c in CalcCavi.Lista) if (c.Name.StartsWith(cerca == "RG-213" ? "RG-213" : "Linea") && c.Name.Contains(cerca)) return c;
            if (l == 0) return new Cable { Name = "RG-213/U", Z0 = 50, Vf = 0.66, AttDb100m = new[] { 4.31, 8.21, 15.0, 26.0 } };
            if (l == 1) return new Cable { Name = "Linea a finestra 450 Ω", Z0 = 450, Vf = 0.91, AttDb100m = new[] { 1.11, 1.88, 3.26, 5.65 } };
            return new Cable { Name = "Linea aperta 600 Ω", Z0 = 600, Vf = 0.98, AttDb100m = new[] { 0.41, 0.7, 1.21, 2.09 } };
        }

        /// <summary>Vertici del loop (piano orizzontale, quota h), in senso antiorario; il primo lato parte dal primo vertice.</summary>
        public List<double[]> Vertici()
        {
            double per = Math.Max(1, P("per")), h = P("h");
            List<double[]> v = new List<double[]>();
            int forma = I("forma");
            if (forma == 0) { double s = per / 4; v.Add(new[] { -s / 2, -s / 2 }); v.Add(new[] { s / 2, -s / 2 }); v.Add(new[] { s / 2, s / 2 }); v.Add(new[] { -s / 2, s / 2 }); }
            else if (forma == 2) { double w = per / 6, l = 2 * w; v.Add(new[] { -l / 2, -w / 2 }); v.Add(new[] { l / 2, -w / 2 }); v.Add(new[] { l / 2, w / 2 }); v.Add(new[] { -l / 2, w / 2 }); }
            else
            {
                int n = forma == 1 ? 3 : 16;
                double lato = per / n, r = lato / (2 * Math.Sin(Math.PI / n)), a0 = -Math.PI / 2 - Math.PI / n;
                for (int i = 0; i < n; i++) { double a = a0 + 2 * Math.PI * i / n; v.Add(new[] { r * Math.Cos(a), r * Math.Sin(a) }); }
            }
            List<double[]> o = new List<double[]>();
            foreach (double[] p in v) o.Add(new[] { p[0], p[1], h });
            return o;
        }

        public override CalcOutput Compute()
        {
            int i0 = Indice0();
            if (bandaPrec >= 0 && bandaPrec != i0) Par("per").Value = Math.Round(306.3 / BandaF[i0], 1);
            bandaPrec = i0;
            double f0 = BandaF[i0], per = Math.Max(1, P("per")), h = P("h"), a = P("d") / 2000, lam0 = C0 / f0;
            Hint("per", "1005/f piedi: " + Calcolatori.N(306.3 / f0, 1) + " m a " + Calcolatori.N(f0, 3) + " MHz");
            CalcOutput o = new CalcOutput();
            List<double[]> v = Vertici();
            int forma = I("forma");
            double lato = per / v.Count;
            o.Card("Perimetro", Calcolatori.N(per, 1) + " m", Calcolatori.N(per / lam0, 3) + " λ a " + Calcolatori.N(f0, 3) + " MHz", true);
            if (forma == 0) o.Card("Lato", Calcolatori.N(lato, 2) + " m", "quattro lati uguali");
            else if (forma == 1) o.Card("Lato", Calcolatori.N(lato, 2) + " m", "triangolo equilatero, tre sostegni");
            else if (forma == 2) o.Card("Lati", Calcolatori.N(per / 3, 2) + " x " + Calcolatori.N(per / 6, 2) + " m", "rettangolo 2:1");
            else o.Card("Diametro", Calcolatori.N(2 * Math.Sqrt(v[0][0] * v[0][0] + v[0][1] * v[0][1]), 2) + " m", "16 lati da " + Calcolatori.N(lato, 2) + " m");
            o.Card("Altezza", Calcolatori.N(h, 1) + " m", Calcolatori.N(h / lam0, 2) + " λ sulla banda bassa");
            Cable c = Linea();
            o.Card("Discesa", c.Name, Calcolatori.N(P("lun"), 0) + " m, perdita adattata " + Calcolatori.N(c.AttenuationAt(f0) * P("lun") / 100, 2) + " dB a " + Calcolatori.N(f0, 2) + " MHz");

            // ---- modello NEC: lati a segmenti di lambda/20 della banda piu' alta, sorgente nell'angolo o a meta' del primo lato
            NecDeck d = new NecDeck(Title + " " + Calcolatori.N(per, 1) + " m");
            double seg = C0 / 28.5 / 20;
            bool meta = I("punto") == 1;
            int tf = 0, nf = 1;
            for (int i = 0; i < v.Count; i++)
            {
                double[] p = v[i], q = v[(i + 1) % v.Count];
                int t = d.Ws(seg, p[0], p[1], p[2], q[0], q[1], q[2], a, meta && i == 0, 3);
                if (i == 0) { tf = t; nf = d.LastN; }
            }
            d.Ground(h > 0 ? 2 : 0);
            d.Ex(tf, meta ? (nf + 1) / 2 : 1);
            o.Nec = d.End(f0);
            o.Plane = "xy";

            if (h < lam0 / 4) o.Warnings.Add("Sotto λ/4 sulla banda piu' bassa (" + Calcolatori.N(lam0 / 4, 1) + " m) il loop irradia soprattutto verso l'alto: buono per i contatti vicini (NVIS), meno per il DX.");
            if (I("linea") == 0) o.Warnings.Add("Coassiale con il ROS alto delle bande armoniche: la perdita cresce molto (NT0Z, WB8IMY). Meglio l'accordatore al punto di alimentazione, oppure la scala.");

            if (BF != null && firma == Firma())
            {
                for (int k = 0; k < BF.Length; k++)
                    o.Card(Righe[k][0] + " · " + Calcolatori.N(BF[k], 3) + " MHz", "G " + Righe[k][4] + " dBi a " + Righe[k][5] + "°",
                        "Z al loop " + Righe[k][1] + " · ROS " + Righe[k][2] + " sulla linea · perdita " + Righe[k][3] + " dB", BPerd[k] > 3);
                o.Chart = new CalcChart { Title = "Perdita della discesa con il ROS di ogni banda (dB)", XLabel = "MHz", YLabel = "dB", X = BF, Y = BPerd, YMin = 0 };
                double pm = 0; for (int k = 0; k < BPerd.Length; k++) pm = Math.Max(pm, BPerd[k]);
                if (pm > 3) o.Warnings.Add("Su qualche banda la discesa perde piu' di 3 dB (riquadri evidenziati): accorcia la linea, passa alla scala o metti l'accordatore al loop.");
                else o.Oks.Add("Perdita della discesa sotto 3 dB su tutte le bande.");
            }

            o.Formula = "Formule: perimetro 1005/f piedi = 306,3/f m (W7FB, ARRL); il loop e' un'onda sulla banda piu' bassa e un multiplo sulle altre. " +
                "Perdita della discesa: formula della linea con perdite e disadattamento, P al carico / P in ingresso = (1 - |ρ|²) / (e^(2αl) - |ρ|² e^(-2αl)), " +
                "con le perdite dell'abaco cavi. Somiglianze con NT0Z (Table 1): la scala perde molto meno del coassiale con il ROS alto; differenze: NT0Z moltiplica la perdita adattata per un fattore del ROS, qui si usa la formula esatta. " +
                "Fonti: " + Source + ".";
            return o;
        }

        public override double Tune(double fmhz)
        {
            Compute();
            Cable c = Linea();
            double len = Math.Max(0, P("lun"));
            int i0 = Indice0();
            List<double> bf = new List<double>(), ros = new List<double>(), per = new List<double>(), g = new List<double>(), el = new List<double>();
            Righe = new List<string[]>();
            for (int k = i0; k < BandaF.Length; k++)
            {
                double f = BandaF[k];
                CalcOutput o = Compute();
                NecModel m = NecModel.Parse(o.Nec);
                m.Z0 = c.Z0;
                SegmentGeometry geo = m.BuildGeometry();
                RunResult r = Analysis.Run(m, geo, f, 3.0);
                Complex z = r.Zin;
                double att = c.AttenuationAt(f);
                double s = LineCalc.Swr(z, c.Z0);
                double eff = LineCalc.PowerEfficiency(z, c.Z0, f, c.Vf, att, len);
                double perd = eff > 0 ? -10 * Math.Log10(eff) : 99;
                bf.Add(f); ros.Add(s); per.Add(perd); g.Add(r.GmaxDbi); el.Add(r.ElevationOfMax);
                Righe.Add(new[] { BandaN[k], Calcolatori.N(z.Real, 0) + (z.Imaginary < 0 ? " -j" : " +j") + Calcolatori.N(Math.Abs(z.Imaginary), 0) + " Ω",
                    Calcolatori.N(s, 1), Calcolatori.N(perd, 2), Calcolatori.N(r.GmaxDbi, 1), Calcolatori.N(r.ElevationOfMax, 0) });
            }
            BF = bf.ToArray(); BRos = ros.ToArray(); BPerd = per.ToArray(); BG = g.ToArray(); BEl = el.ToArray();
            firma = Firma();
            return Scale;
        }
    }
}
