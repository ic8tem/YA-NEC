// YA-NEC 1.6.0 - Adattamento diretto a Z0 di una Yagi, senza adattatore (bozzetto «spunti dalla ricerca su GitHub», proposta 2,
// approvato da IC8TEM il 23/11/2026 14:27). Idea da AntAn di sozden (Apache-2.0: pulsante "match to 50 ohm"); nessun codice copiato.
//
// Il modello aperto si copia con le misure in numeri (come Ridimensiona e ricentra). Si cercano la lunghezza del radiatore e, se chiesto,
// lunghezza e posizione del primo direttore (D1) che portano il ROS su Z0 al minimo alla frequenza voluta: simplesso di Nelder e Mead su
// tre variabili (radiatore +-5 %, D1 +-5 %, D1 +-60 % della distanza dal radiatore), voto = |gamma|^2 piu' una penalita' se il guadagno
// nella direzione del lobo scende piu' del calo ammesso. Il resto della Yagi non cambia.
// Yagi riconosciuta: una sola sorgente al centro di un filo diritto isolato (non tocca altri fili), almeno altri due fili isolati
// paralleli al radiatore e un direttore dal lato dove ce ne sono di piu' (a pari numero, dal lato dell'elemento piu' corto).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace YaNec.Engine
{
    public class AdattaEsito
    {
        public NecModel Modello;
        public Complex ZPrima, ZDopo;
        public double RosPrima, RosDopo, GPrima, GDopo, FbPrima, FbDopo;
        public double DRadMm, DD1Mm, PD1Mm;   // variazioni: lunghezza del radiatore, lunghezza di D1, posizione di D1 (negativa = verso il radiatore)
        public int Prove;
        public bool SoloRadiatore;
        public List<string> Note = new List<string>();
    }

    public static class AdattaZ0
    {
        class Yagi
        {
            public GeomCard Rad, D1;
            public int Asse;           // asse del boom (0 x, 1 y, 2 z)
            public double Verso;       // +1 / -1: dal radiatore verso D1 lungo l'asse
            public double Spaziatura;  // distanza radiatore - D1 (m)
        }

        static double[] Centro(GeomCard c) { return new[] { (c.F1 + c.F4) / 2, (c.F2 + c.F5) / 2, (c.F3 + c.F6) / 2 }; }
        static double[] Dir(GeomCard c)
        {
            double[] d = { c.F4 - c.F1, c.F5 - c.F2, c.F6 - c.F3 };
            double l = Math.Sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2]);
            return l < 1e-12 ? d : new[] { d[0] / l, d[1] / l, d[2] / l };
        }

        static Yagi Trova(NecModel m, out string perche)
        {
            perche = null;
            GeomCard rad = Ricentra.Alimentato(m);
            if (rad == null) { perche = "serve una sola sorgente al centro di un filo diritto con un numero dispari di segmenti"; return null; }
            List<GeomCard> iso = Ricentra.Isolati(m);
            if (!iso.Contains(rad)) { perche = "il radiatore tocca altri fili (dipolo ripiegato, gamma, anello): la sua lunghezza non si cambia da sola"; return null; }
            double[] dr = Dir(rad), cr = Centro(rad);
            List<GeomCard> par = iso.Where(c => !ReferenceEquals(c, rad) && Math.Abs(Dir(c).Zip(dr, (a, b) => a * b).Sum()) > 0.98).ToList();
            if (par.Count < 2) { perche = "servono almeno tre elementi diritti paralleli (riflettore, radiatore, direttore)"; return null; }
            int asse = -1; double est = -1;
            for (int a = 0; a < 3; a++)
            {
                if (Math.Abs(dr[a]) > 0.7) continue;
                double lo = Math.Min(cr[a], par.Min(c => Centro(c)[a])), hi = Math.Max(cr[a], par.Max(c => Centro(c)[a]));
                if (hi - lo > est) { est = hi - lo; asse = a; }
            }
            if (asse < 0 || est < 1e-6) { perche = "elementi non disposti lungo un boom"; return null; }
            List<GeomCard> avanti = par.Where(c => Centro(c)[asse] > cr[asse] + 1e-6).ToList(), dietro = par.Where(c => Centro(c)[asse] < cr[asse] - 1e-6).ToList();
            Func<List<GeomCard>, GeomCard> vicino = l => l.OrderBy(c => Math.Abs(Centro(c)[asse] - cr[asse])).FirstOrDefault();
            List<GeomCard> lato;
            if (avanti.Count != dietro.Count) lato = avanti.Count > dietro.Count ? avanti : dietro;
            else if (avanti.Count == 0) { perche = "nessun elemento davanti o dietro al radiatore"; return null; }
            else lato = Ricentra.Len(vicino(avanti)) <= Ricentra.Len(vicino(dietro)) ? avanti : dietro;
            GeomCard d1 = vicino(lato);
            if (d1 == null) { perche = "direttore non trovato"; return null; }
            if (Ricentra.Len(d1) > Ricentra.Len(rad) * 1.02) { perche = "l'elemento dopo il radiatore e' piu' lungo del radiatore: non sembra un direttore"; return null; }
            return new Yagi { Rad = rad, D1 = d1, Asse = asse, Verso = Centro(d1)[asse] > cr[asse] ? 1 : -1, Spaziatura = Math.Abs(Centro(d1)[asse] - cr[asse]) };
        }

        /// <summary>Il modello e' una Yagi che si puo' adattare cosi'? (solo geometria, nessun calcolo)</summary>
        public static bool Riconosciuta(NecModel m, out string perche)
        {
            perche = null;
            if (m == null) { perche = "nessun modello"; return false; }
            try { NecModel c = Ricentra.Copia(m); return Trova(c, out perche) != null; }
            catch (Exception ex) { perche = ex.Message; return false; }
        }

        static void Applica(NecModel m, Yagi y, double lr0, double l10, double c10, double dLr, double dL1, double dP1)
        {
            Ricentra.Accorcia(y.Rad, lr0 + dLr);
            Ricentra.Accorcia(y.D1, l10 + dL1);
            double sposta = c10 + y.Verso * dP1 - Centro(y.D1)[y.Asse];
            if (y.Asse == 0) { y.D1.F1 += sposta; y.D1.F4 += sposta; }
            else if (y.Asse == 1) { y.D1.F2 += sposta; y.D1.F5 += sposta; }
            else { y.D1.F3 += sposta; y.D1.F6 += sposta; }
        }

        public static AdattaEsito Calcola(NecModel src, double f, double z0, bool soloRadiatore, double caloGMax, CancellationToken ct)
        {
            AdattaEsito r = new AdattaEsito { SoloRadiatore = soloRadiatore };
            NecModel m = Ricentra.Copia(src);
            m.FreqMHz = f;
            string perche;
            Yagi y = Trova(m, out perche);
            if (y == null) throw new NecException("Adattamento diretto: " + perche + ".");
            RunResult r0 = Analysis.Run(m, m.BuildGeometry(), f, 5.0, ct);
            double th = r0.ThetaMax, ph = r0.PhiMax;
            Func<SweepPoint> misura = () =>
            {
                List<SweepPoint> p = Analysis.Sweep(m, m.BuildGeometry(), new[] { f }, th, ph, null, ct);
                if (p.Count == 0 || !p[0].Ok) throw new NecException(p.Count > 0 && p[0].Error != null ? p[0].Error : "calcolo non riuscito");
                return p[0];
            };
            SweepPoint p0 = misura();
            r.ZPrima = p0.Z; r.RosPrima = Analysis.Swr(p0.Z, z0); r.GPrima = p0.GainFwd; r.FbPrima = p0.FrontBack;
            double lr0 = Ricentra.Len(y.Rad), l10 = Ricentra.Len(y.D1), c10 = Centro(y.D1)[y.Asse];
            int nv = soloRadiatore ? 1 : 3;
            double[] scala = { 0.01 * lr0, 0.01 * l10, 0.1 * y.Spaziatura };
            double[] lim = { 0.05 * lr0, 0.05 * l10, 0.6 * y.Spaziatura };
            Dictionary<string, double> memo = new Dictionary<string, double>();
            double migliore = double.MaxValue; double[] xm = new double[3];
            Func<double[], double> costo = u =>
            {
                ct.ThrowIfCancellationRequested();
                double[] x = new double[3];
                double fuori = 0;
                for (int i = 0; i < nv; i++)
                {
                    x[i] = Math.Round(u[i] * scala[i], 5);   // 0,01 mm
                    if (Math.Abs(x[i]) > lim[i]) { fuori += Math.Abs(x[i]) - lim[i]; x[i] = Math.Sign(x[i]) * lim[i]; }
                }
                string k = x[0] + ";" + x[1] + ";" + x[2];
                double c;
                if (!memo.TryGetValue(k, out c))
                {
                    Applica(m, y, lr0, l10, c10, x[0], x[1], x[2]);
                    r.Prove++;
                    try
                    {
                        SweepPoint p = misura();
                        Complex g = (p.Z - z0) / (p.Z + z0);
                        c = g.Magnitude * g.Magnitude;
                        if (!double.IsInfinity(caloGMax) && p.GainFwd < r.GPrima - caloGMax) c += 0.2 * (r.GPrima - caloGMax - p.GainFwd);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception) { c = 10; }
                    memo[k] = c;
                }
                c += 100 * fuori;
                if (c < migliore) { migliore = c; xm = x; }
                return c;
            };
            // simplesso di Nelder e Mead (coordinate in unita' di scala)
            double[][] s = new double[nv + 1][];
            double[] fv = new double[nv + 1];
            for (int k = 0; k <= nv; k++) { s[k] = new double[nv]; if (k > 0) s[k][k - 1] = 1; fv[k] = costo(s[k]); }
            for (int giro = 0; giro < 200 && r.Prove < 160; giro++)
            {
                Array.Sort(fv, s);
                if (fv[0] < 1e-4) break;   // ROS 1,02
                double ampiezza = 0;
                for (int k = 1; k <= nv; k++) for (int i = 0; i < nv; i++) ampiezza = Math.Max(ampiezza, Math.Abs(s[k][i] - s[0][i]));
                if (ampiezza < 0.005) break;
                double[] c = new double[nv];
                for (int k = 0; k < nv; k++) for (int i = 0; i < nv; i++) c[i] += s[k][i] / nv;
                Func<double, double[]> lungo = a => { double[] q = new double[nv]; for (int i = 0; i < nv; i++) q[i] = c[i] + a * (s[nv][i] - c[i]); return q; };
                double[] xr = lungo(-1); double fr = costo(xr);
                if (fr < fv[0])
                {
                    double[] xe = lungo(-2); double fe = costo(xe);
                    if (fe < fr) { s[nv] = xe; fv[nv] = fe; } else { s[nv] = xr; fv[nv] = fr; }
                }
                else if (nv > 1 && fr < fv[nv - 1] || nv == 1 && fr < fv[nv]) { s[nv] = xr; fv[nv] = fr; }
                else
                {
                    double[] xc = fr < fv[nv] ? lungo(-0.5) : lungo(0.5); double fc = costo(xc);
                    if (fc < Math.Min(fr, fv[nv])) { s[nv] = xc; fv[nv] = fc; }
                    else
                        for (int k = 1; k <= nv; k++)
                        {
                            for (int i = 0; i < nv; i++) s[k][i] = s[0][i] + 0.5 * (s[k][i] - s[0][i]);
                            fv[k] = costo(s[k]);
                        }
                }
            }
            Applica(m, y, lr0, l10, c10, xm[0], xm[1], xm[2]);
            SweepPoint p1 = misura();
            r.ZDopo = p1.Z; r.RosDopo = Analysis.Swr(p1.Z, z0); r.GDopo = p1.GainFwd; r.FbDopo = p1.FrontBack;
            r.DRadMm = 1000 * xm[0]; r.DD1Mm = 1000 * xm[1]; r.PD1Mm = 1000 * xm[2];
            if (r.RosDopo > 1.2) r.Note.Add(soloRadiatore ? "Con il solo radiatore si annulla la reattanza ma la resistenza resta quella della Yagi: per arrivare a Z0 scegli anche D1." :
                                                           "ROS ancora sopra 1,2: la resistenza della Yagi e' lontana da Z0 (allarga il calo di guadagno ammesso o usa un adattatore).");
            if (Math.Abs(xm[0]) >= lim[0] * 0.999 || Math.Abs(xm[1]) >= lim[1] * 0.999 || Math.Abs(xm[2]) >= lim[2] * 0.999)
                r.Note.Add("Una variazione e' arrivata al limite (radiatore e D1 +-5 %, posizione di D1 +-60 % della distanza dal radiatore).");
            m.FileName = string.IsNullOrEmpty(src.FileName) ? "adattato.nec" :
                System.IO.Path.GetFileNameWithoutExtension(src.FileName) + " adattato " + z0.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " ohm.nec";
            r.Modello = m;
            return r;
        }
    }
}
