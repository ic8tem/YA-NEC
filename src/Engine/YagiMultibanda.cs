// YA-NEC 0.4.30 - Yagi multibanda HF a due elementi per banda (sottolinguetta dei Calcolatori), richiesta di IC8TEM del 14/11/2026 23:33
// ("di questi dipoli multibanda mi fai l'esportazione in nec per farne una yagi multibanda hf"), 23:36 ("si come nuova scheda": un
// elemento parassita per banda gia' messo e accordato) e 23:37 ("permetti all'utente di poter usare sia una configurazione
// riflettori-dipoli, sia una configurazione dipoli-direttori"): per ogni banda si sceglie riflettore (dietro, x negativo, piu' lungo) o
// direttore (davanti, x positivo, piu' corto), come la XR-5 che ha i riflettori su 20-12 m e il direttore sul 10 m. Il lobo e' sempre
// verso x positivo.
// I radiatori sono quelli della scheda Dipolo multibanda (bacchette, manica aperta, radiatori XR-5). L'accordo cerca insieme le variabili
// dei radiatori e, per ogni banda, lunghezza e distanza del parassita; obiettivo: somma su tutte le bande di (ROS al centro - 1)^2 +
// peso x (F/B obiettivo - F/B)^2 quando il F/B e' sotto l'obiettivo. Prova con nec2c (scratchpad/dipmb/yagi.py): bacchette 20/15/10 m
// d 0,1 m con riflettori, peso 0,002, F/B obiettivo 20 dB: ROS al centro 1,02 / 1,09 / 1,19, 6,7 / 6,2 / 5,8 dBi, F/B circa 10 dB;
// riflettori 10,66 m a 0,185 lambda, 7,26 m a 0,197 lambda, 5,51 m a 0,222 lambda.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading;

namespace YaNec.Engine
{
    public class YmSpec
    {
        public DmSpec Rad = new DmSpec();
        public Dictionary<string, bool> Direttore = new Dictionary<string, bool>();   // false = riflettore
        public Dictionary<string, double> PL = new Dictionary<string, double>();      // lunghezza del parassita (m)
        public Dictionary<string, double> PS = new Dictionary<string, double>();      // distanza dal radiatore (lambda)
        public double Diametro = 0.02;          // tubo dei parassiti (m)
        public double FbObiettivo = 20, Peso = 0.002;
        public double PSFissa;                  // 0 = distanza accordata; altrimenti distanza fissa in lambda (0,1 / 0,12 / 0,15 / 0,2)
        public YmSpec Clone()
        {
            YmSpec c = (YmSpec)MemberwiseClone();
            c.Rad = Rad.Clone();
            c.Direttore = new Dictionary<string, bool>(Direttore); c.PL = new Dictionary<string, double>(PL); c.PS = new Dictionary<string, double>(PS);
            return c;
        }
    }

    public class YmBanda
    {
        public string Banda, Parassita = "", Ruba = "";
        public Complex Z;
        public double RosLo, RosC, RosHi, G, Fb;
    }

    public static class YagiMultibanda
    {
        static readonly CultureInfo IV = CultureInfo.InvariantCulture, IT = CultureInfo.GetCultureInfo("it-IT");

        /// <summary>Parassiti di partenza: riflettore 0,51 lambda a 0,15 lambda; direttore 0,45 lambda a 0,10 lambda.</summary>
        public static void Iniziale(YmSpec y)
        {
            y.PL.Clear(); y.PS.Clear();
            foreach (string b in y.Rad.Bande)
            {
                bool d;
                if (!y.Direttore.TryGetValue(b, out d)) y.Direttore[b] = d = false;
                double lam = DipoloMultibanda.Lambda(b);
                y.PL[b] = (d ? 0.45 : 0.51) * lam;
                y.PS[b] = y.PSFissa > 0 ? y.PSFissa : d ? 0.10 : 0.15;
            }
        }

        public static NecModel Modello(YmSpec y, double f, out Dictionary<int, string> tb)
        {
            NecModel m = DipoloMultibanda.Modello(y.Rad, f, out tb);
            int tag = 0;
            foreach (GeomCard g in m.Geometry) tag = Math.Max(tag, g.I1);
            double fmax = 0;
            foreach (string b in y.Rad.Bande) fmax = Math.Max(fmax, DipoloMultibanda.Banda(b).High);
            double seg = 299.792458 / fmax / 20;
            foreach (string b in y.Rad.Bande)
            {
                double lam = DipoloMultibanda.Lambda(b), L = y.PL[b], x = DipoloMultibanda.XRadiatore(y.Rad, b) + (y.Direttore[b] ? 1 : -1) * y.PS[b] * lam, z = DipoloMultibanda.ZRadiatore(y.Rad, b);
                int n = Math.Max(3, (int)Math.Ceiling(L / seg)); if (n % 2 == 0) n++;
                tag++;
                m.Geometry.Add(new GeomCard { Id = "GW", I1 = tag, I2 = n, F1 = x, F2 = -L / 2, F3 = z, F4 = x, F5 = L / 2, F6 = z, F7 = y.Rad.Rastrema && y.Rad.Modo != 2 ? -3 : y.Diametro / 2 });   // -3 = tubi "<>" dal centro (riga del Dipolo multibanda)
                tb[tag] = b;
            }
            m.Comments.Insert(0, "YA-NEC - Yagi multibanda HF, due elementi per banda: " + string.Join(", ", y.Rad.Bande.ConvertAll(b => b + (y.Direttore[b] ? " radiatore-direttore" : " riflettore-radiatore"))));
            List<string> err = m.Reevaluate();
            if (err.Count > 0) throw new NecException(err[0]);
            return m;
        }

        static double Theta(YmSpec y) { return y.Rad.Terreno ? 70 : 90; }   // con il terreno il F/B si misura a 20 gradi di elevazione

        static void Punto(YmSpec y, string b, CancellationToken ct, out Complex z, out double fb)
        {
            ct.ThrowIfCancellationRequested();
            double f = DipoloMultibanda.Banda(b).Operating;
            Dictionary<int, string> tb;
            NecModel m = Modello(y, f, out tb);
            SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, f);   // 0.4.31: con Leeson come la verifica
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            z = e.Sources[0].Z;
            fb = e.Gain(Theta(y), 0).GainTotal - e.Gain(Theta(y), 180).GainTotal;
        }

        static double Ros(YmSpec y, double f, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Dictionary<int, string> tb;
            NecModel m = Modello(y, f, out tb);
            SegmentGeometry g = Analysis.GeometriaDiCalcolo(m, f);   // 0.4.31: con Leeson come la verifica
            Nec2Engine e = new Nec2Engine(g);
            e.Solve(f, Analysis.EffectiveSources(m, g, f), m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
            return Analysis.Swr(e.Sources[0].Z, 50);
        }

        static double Obiettivo(YmSpec y, CancellationToken ct)
        {
            double o = 0;
            foreach (string b in DipoloMultibanda.TutteBande(y.Rad))
            {
                Complex z; double fb;
                Punto(y, b, ct, out z, out fb);
                double r = Analysis.Swr(z, 50), manca = Math.Max(0, y.FbObiettivo - fb);
                o += (r - 1) * (r - 1) + y.Peso * manca * manca;
            }
            return o;
        }

        /// <summary>Accordo di radiatori e parassiti insieme (discesa per coordinate, 3 giri, ricerca aurea; si tiene solo cio' che migliora).</summary>
        public static void Accorda(YmSpec y, CancellationToken ct, Action<string> avanzamento = null)
        {
            // 0.4.31 (collaudo 0.4.30: dipolo-direttore F/B 0,7 dB sul 10 m): prima i radiatori da soli, poi radiatori e parassiti insieme,
            // come nella prova con nec2c (dipmb/yagi.py parte dalle lunghezze dei radiatori gia' accordati)
            DipoloMultibanda.Accorda(y.Rad, ct, avanzamento);
            List<Tuple<string, Func<double>, Action<double>, double, double>> v = DipoloMultibanda.Variabili(y.Rad);
            foreach (string b0 in y.Rad.Bande)
            {
                string b = b0;
                double lam = DipoloMultibanda.Lambda(b);
                bool d = y.Direttore[b];
                string nome = d ? "direttore " : "riflettore ";
                v.Add(Tuple.Create<string, Func<double>, Action<double>, double, double>(nome + b, () => y.PL[b], x => y.PL[b] = x, (d ? 0.40 : 0.48) * lam, (d ? 0.48 : 0.56) * lam));
                if (y.PSFissa <= 0) v.Add(Tuple.Create<string, Func<double>, Action<double>, double, double>("distanza del " + nome + b, () => y.PS[b], x => y.PS[b] = x, d ? 0.07 : 0.08, d ? 0.20 : 0.25));
            }
            double best = Obiettivo(y, ct);
            for (int giro = 0; giro < 3; giro++)
                foreach (Tuple<string, Func<double>, Action<double>, double, double> t in v)
                {
                    double vecchio = t.Item2();
                    double nuovo = DipoloMultibanda.Aurea(x => { t.Item3(x); double o = Obiettivo(y, ct); t.Item3(vecchio); return o; }, t.Item4, t.Item5, 12);
                    t.Item3(nuovo);
                    double o2 = Obiettivo(y, ct);
                    if (o2 < best) best = o2; else t.Item3(vecchio);
                    if (avanzamento != null) avanzamento(string.Format(IT, "giro {0}/3, {1}: obiettivo {2:0.000}", giro + 1, t.Item1, best));
                }
        }

        public static List<YmBanda> Verifica(YmSpec y, CancellationToken ct)
        {
            List<YmBanda> r = new List<YmBanda>();
            foreach (string b in DipoloMultibanda.TutteBande(y.Rad))
            {
                Band bb = DipoloMultibanda.Banda(b);
                Dictionary<int, string> tb;
                NecModel m = Modello(y, bb.Operating, out tb);
                SegmentGeometry g = m.BuildGeometry();
                RunResult rr = Analysis.Run(m, g, bb.Operating, 5.0, ct);
                YmBanda d = new YmBanda { Banda = b, Z = rr.Zin, RosC = Analysis.Swr(rr.Zin, 50), G = rr.GmaxDbi };
                d.Fb = rr.Engine.Gain(Theta(y), 0).GainTotal - rr.Engine.Gain(Theta(y), 180).GainTotal;
                d.RosLo = Ros(y, bb.Low, ct); d.RosHi = Ros(y, bb.High, ct);
                d.Parassita = !y.PL.ContainsKey(b) ? "nessuno (banda della trappola)" :
                    string.Format(IT, "{0} {1:0.000} m a {2:0.000} lambda ({3:0.000} m)", y.Direttore[b] ? "direttore" : "riflettore", y.PL[b], y.PS[b], y.PS[b] * DipoloMultibanda.Lambda(b));
                Dictionary<string, double> imax = new Dictionary<string, double>();
                for (int i = 0; i < g.N; i++)
                {
                    string eb;
                    if (!tb.TryGetValue(g.Tag[i], out eb)) continue;
                    double a = Complex.Abs(rr.Engine.SegmentCurrent(i)), v;
                    imax.TryGetValue(eb, out v); imax[eb] = Math.Max(v, a);
                }
                double mio; imax.TryGetValue(b, out mio);
                string ladro = null; double peggio = 0;
                foreach (KeyValuePair<string, double> kv in imax) if (kv.Key != b && kv.Value > peggio) { peggio = kv.Value; ladro = kv.Key; }
                d.Ruba = ladro != null && mio > 0 && peggio > 0.3 * mio ? ladro + string.Format(IT, " ({0:0} % della corrente)", peggio / mio * 100) : "nessuno";
                r.Add(d);
            }
            return r;
        }

        /// <summary>Il modello in .nec con le righe CM delle bande (fili, misure, esito), per l'Ottimizzatore o per altri programmi.</summary>
        public static string Esporta(YmSpec y, List<YmBanda> esito)
        {
            Dictionary<int, string> tb;
            NecModel m = Modello(y, DipoloMultibanda.Banda(y.Rad.Bande[0]).Operating, out tb);
            StringBuilder c = new StringBuilder();
            c.Append("CM YA-NEC " + AppPaths.Version + " - Yagi multibanda HF (Calcolatori > Yagi multibanda HF), radiatori: " + DipoloMultibanda.Modi[y.Rad.Modo] + "\n");
            c.Append("CM boom = asse X, lobo verso x positivo; " + (y.Rad.Terreno ? string.Format(IT, "terreno medio, quota {0:0.##} m", y.Rad.Quota) : "spazio libero") + "\n");
            foreach (string b in DipoloMultibanda.TutteBande(y.Rad))
            {
                List<string> tags = new List<string>();
                foreach (KeyValuePair<int, string> kv in tb) if (kv.Value == b) tags.Add(kv.Key.ToString(IV));
                YmBanda d = esito != null ? esito.Find(x => x.Banda == b) : null;
                c.Append("CM " + b + ": fili " + string.Join(" ", tags) + (!y.PL.ContainsKey(b) ? "; banda della trappola, senza parassita" : string.Format(IT, "; {0} {1:0.000} m a {2:0.000} m", y.Direttore[b] ? "direttore" : "riflettore", y.PL[b], y.PS[b] * DipoloMultibanda.Lambda(b))) +
                         (d != null ? string.Format(IT, "; ROS {0:0.00}, {1:0.0} dBi, F/B {2:0.0} dB", d.RosC, d.G, d.Fb) : "; misure non accordate") + "\n");
            }
            return c.ToString() + m.ToNec();
        }
    }
}
