// YA-NEC 1.6.0 - Calcolatori > Suggerimenti (bozzetto «spunti dalla ricerca su GitHub», proposta 1, approvato da IC8TEM il 23/11/2026
// 14:27). Idea di AntAn di sozden (Apache-2.0: dalla frequenza le antenne possibili, ognuna calcolata); nessun codice copiato.
//
// Per la frequenza, l'altezza, il terreno e l'ingombro massimo dati, ogni calcolatore adatto alla banda prepara il suo modello con i
// valori di partenza; il modello si porta all'altezza scelta (centro dell'antenna, o base per le verticali; la V invertita, il ground
// plane e la NVIS usano l'altezza del calcolatore), si mette sul terreno scelto e si calcola con il NEC (un calcolo per antenna, in
// parallelo). Ordine secondo quello che conta: guadagno, uniformita' in azimut, angolo basso, guadagno verso lo zenit (NVIS).
// La polarizzazione si legge dal diagramma (componente piu' forte nella direzione del massimo).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace YaNec.Engine
{
    public class SuggRichiesta
    {
        public double F = 14.175, H = 10, IngombroMax = 12;
        public int Terreno;        // 0 medio, 1 molto povero, 2 molto buono, 3 mare
        public int Obiettivo;      // 0 guadagno in una direzione, 1 tutte le direzioni, 2 angolo basso (DX), 3 NVIS
        public int Pol = 2;        // 0 orizzontale, 1 verticale, 2 indifferente
    }

    public class SuggRiga
    {
        public string Chiave = "", Nome = "";
        public double G = double.NaN, Fb = double.NaN, El = double.NaN, Ros = double.NaN, Zenit = double.NaN, Ondulazione = double.NaN, Z0 = 50;
        public Complex Z;
        public double Largo, Profondo, Alto;     // m: ingombro orizzontale (lato maggiore e minore) e altezza dell'antenna
        public bool Orizzontale, Fuori;
        public string Errore, Nota;
        public Dictionary<string, double> Valori = new Dictionary<string, double>();
        public double Punteggio;
    }

    public static class Suggerimenti
    {
        public static readonly string[] Terreni = { "medio (13 / 0,005)", "molto povero (3 / 0,001)", "molto buono (20 / 0,0303)", "mare (81 / 5)" };
        static readonly double[,] Terr = { { 13, 0.005 }, { 3, 0.001 }, { 20, 0.0303 }, { 81, 5 } };
        public static readonly string[] Obiettivi = { "guadagno in una direzione", "tutte le direzioni", "angolo basso (DX)", "NVIS (verso lo zenit)" };
        public static readonly string[] Polarizzazioni = { "orizzontale", "verticale", "indifferente" };

        // montaggio: 0 centro all'altezza, 1 base all'altezza, 2 com'e' nel calcolatore (a terra o sul piano di massa ideale), 3 altezza del calcolatore
        class Cand
        {
            public string Chiave, Nome; public double FMin, FMax; public int Montaggio; public bool HBasso, SoloNvis;
            public Func<SimpleCalc> Crea; public Action<SimpleCalc, SuggRichiesta> Imposta;
        }

        static void Set(SimpleCalc c, string k, double v) { CalcParam p = c.Par(k); if (p != null) p.Value = v; }

        static int BandaHex(double f)
        {
            double[][] b = { new[] { 14.0, 14.35 }, new[] { 18.068, 18.168 }, new[] { 21.0, 21.45 }, new[] { 24.89, 24.99 }, new[] { 28.0, 29.7 } };
            for (int i = 0; i < b.Length; i++) if (f >= b[i][0] * 0.98 && f <= b[i][1] * 1.02) return i;
            return -1;
        }

        static int ElementiYagi(double f) { return f < 30 ? 3 : f < 100 ? 5 : f < 500 ? 10 : 15; }

        static List<Cand> Candidati()
        {
            return new List<Cand>
            {
                new Cand { Chiave = "dipolo", Nome = "Dipolo λ/2", FMin = 1.8, FMax = 1300, Montaggio = 0, Crea = () => new CalcDipolo(), Imposta = (c, q) => Set(c, "h", 0) },
                new Cand { Chiave = "vinv", Nome = "V invertita", FMin = 1.8, FMax = 60, Montaggio = 3, Crea = () => new CalcVInvertita(), Imposta = (c, q) => Set(c, "h", q.H) },
                new Cand { Chiave = "delta", Nome = "Delta loop", FMin = 3.5, FMax = 150, Montaggio = 0, HBasso = true, Crea = () => new CalcDelta(), Imposta = (c, q) => Set(c, "h", 0) },
                new Cand { Chiave = "quad", Nome = "Quad 2 elementi", FMin = 7, FMax = 450, Montaggio = 0, Crea = () => new CalcQuad(), Imposta = (c, q) => { Set(c, "n", 1); Set(c, "h", 0); } },
                new Cand { Chiave = "moxon", Nome = "Moxon", FMin = 7, FMax = 450, Montaggio = 0, Crea = () => new CalcMoxon(), Imposta = (c, q) => Set(c, "h", 0) },
                new Cand { Chiave = "hb9cv", Nome = "HB9CV", FMin = 14, FMax = 450, Montaggio = 0, Crea = () => new CalcHb9cv(), Imposta = (c, q) => Set(c, "h", 0) },
                new Cand { Chiave = "hexbeam", Nome = "Hex beam", FMin = 13.7, FMax = 30.3, Montaggio = 0, Crea = () => new CalcHexbeam(), Imposta = (c, q) => { Set(c, "b", Math.Max(0, BandaHex(q.F))); Set(c, "h", 0); } },
                new Cand { Chiave = "yagidl6wu", Nome = "Yagi DL6WU", FMin = 14, FMax = 3000, Montaggio = 0, Crea = () => new CalcYagiDl6wu(), Imposta = (c, q) => { Set(c, "modo", 0); Set(c, "nel", ElementiYagi(q.F)); Set(c, "bw", 0); } },
                new Cand { Chiave = "quarto", Nome = "Verticale λ/4 con radiali a terra", FMin = 1.8, FMax = 30, Montaggio = 2, Crea = () => new CalcQuartoTerra(), Imposta = (c, q) => { } },
                new Cand { Chiave = "gp", Nome = "Ground plane", FMin = 7, FMax = 1300, Montaggio = 3, Crea = () => new CalcGroundPlane(), Imposta = (c, q) => Set(c, "h", q.H) },
                new Cand { Chiave = "58", Nome = "Verticale 5/8", FMin = 25, FMax = 1300, Montaggio = 2, Crea = () => new CalcCinqueOttavi(), Imposta = (c, q) => { } },
                new Cand { Chiave = "jpole", Nome = "J-pole", FMin = 25, FMax = 1300, Montaggio = 1, Crea = () => new CalcJpole(), Imposta = (c, q) => { } },
                new Cand { Chiave = "discone", Nome = "Discone", FMin = 25, FMax = 1300, Montaggio = 1, Crea = () => new CalcDiscone(), Imposta = (c, q) => Set(c, "f", q.F * 0.85) },
                new Cand { Chiave = "nvis", Nome = "Dipolo NVIS", FMin = 1.8, FMax = 10, Montaggio = 3, SoloNvis = true, Crea = () => new CalcNvis(), Imposta = (c, q) => { Set(c, "tipo", 0); Set(c, "h", 0); Set(c, "ter", q.Terreno); } },
            };
        }

        public static List<SuggRiga> Calcola(SuggRichiesta q, IProgress<int> prog, CancellationToken ct)
        {
            List<Cand> l = Candidati().Where(c => q.F >= c.FMin && q.F <= c.FMax && (!c.SoloNvis || q.Obiettivo == 3)).ToList();
            if (l.Exists(c => c.Chiave == "hexbeam") && BandaHex(q.F) < 0) l.RemoveAll(c => c.Chiave == "hexbeam");
            SuggRiga[] r = new SuggRiga[l.Count];
            int fatti = 0;
            ParallelOptions po = Processori.Opzioni();
            po.CancellationToken = ct;
            po.MaxDegreeOfParallelism = Math.Max(1, Math.Min(po.MaxDegreeOfParallelism, 4));
            Parallel.For(0, l.Count, po, i =>
            {
                r[i] = Una(l[i], q, ct);
                prog?.Report(Interlocked.Increment(ref fatti));
            });
            List<SuggRiga> ok = r.Where(x => x != null).ToList();
            foreach (SuggRiga x in ok)
            {
                if (x.Errore != null) { x.Punteggio = double.MinValue; continue; }
                bool polOk = q.Pol == 2 || (q.Pol == 0) == x.Orizzontale;
                double p;
                switch (q.Obiettivo)
                {
                    case 1: p = -x.Ondulazione + 0.1 * x.G; break;
                    case 2: p = -x.El + 0.2 * x.G; break;
                    case 3: p = x.Zenit; break;
                    default: p = x.G; break;
                }
                if (!polOk) { p -= 1e6; x.Nota = "polarizzazione " + (x.Orizzontale ? "orizzontale" : "verticale"); }
                if (x.Fuori) p -= 1e7;
                x.Punteggio = p;
            }
            return ok.OrderByDescending(x => x.Punteggio).ToList();
        }

        static SuggRiga Una(Cand c, SuggRichiesta q, CancellationToken ct)
        {
            SuggRiga s = new SuggRiga { Chiave = c.Chiave, Nome = c.Nome };
            try
            {
                ct.ThrowIfCancellationRequested();
                SimpleCalc sc = c.Crea();
                Set(sc, "f", q.F);
                c.Imposta(sc, q);
                if (c.Chiave == "yagidl6wu") s.Nome = "Yagi " + sc.I("nel") + " el DL6WU";
                // risonanza con il NEC come «Porta in risonanza» (non per la Yagi DL6WU, dove e' un'ottimizzazione lunga, ne' per Moxon e
                // hex beam, che hanno gia' le misure dei loro autori)
                if (sc.CanTune && !(sc is CalcYagiDl6wu) && !(sc is CalcMoxon) && !(sc is CalcHexbeam))
                    try { sc.Tune(sc.P("f") > 0 ? sc.P("f") : q.F); } catch (Exception) { sc.Scale = 1; }
                ct.ThrowIfCancellationRequested();
                CalcOutput o = sc.Run();
                if (string.IsNullOrEmpty(o.Nec)) throw new NecException("il calcolatore non da' un modello NEC");
                NecModel m = NecModel.Parse(o.Nec);
                s.Z0 = !double.IsNaN(o.Z0Rif) && o.Z0Rif > 0 ? o.Z0Rif : 50;
                m.Z0 = s.Z0;
                SegmentGeometry g = m.BuildGeometry();
                double zmin, zmax;
                Estremi(g, out zmin, out zmax);
                double dz = 0;
                if (c.Montaggio == 0) dz = q.H - (zmin + zmax) / 2;
                else if (c.Montaggio == 1) dz = q.H - zmin;
                if (Math.Abs(dz) > 1e-9)
                {
                    m.Geometry.Add(new GeomCard { Id = "GM", I1 = 0, I2 = 0, F6 = dz });
                    g = m.BuildGeometry();
                    Estremi(g, out zmin, out zmax);
                }
                double lam = 299.792458 / q.F;
                if (c.Montaggio != 2)
                {
                    if (zmin < 0.05) { s.Errore = "non sta all'altezza scelta (il punto piu' basso va a " + zmin.ToString("0.0") + " m)"; return s; }
                    if (c.Montaggio != 3 || m.Ground.Kind == GroundKind.Real || m.Ground.Kind == GroundKind.FreeSpace)
                    {
                        m.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = Terr[q.Terreno, 0], Sigma = Terr[q.Terreno, 1], Sommerfeld = zmin < lam };
                        m.GroundFlag = 0;
                        g = m.BuildGeometry();
                    }
                }
                s.Valori["f"] = sc.P("f");
                foreach (CalcParam p in sc.Params) if (p.Key != "f" && p.Key != "h") s.Valori[p.Key] = p.Value;
                if (sc.Par("h") != null) s.Valori["h"] = c.Montaggio == 0 ? (c.HBasso ? zmin : q.H) : sc.P("h");
                RunResult rr = Analysis.Run(m, g, q.F, 2.0, ct);
                s.G = rr.GmaxDbi; s.Fb = rr.FrontBack; s.El = rr.ElevationOfMax; s.Z = rr.Zin; s.Ros = Analysis.Swr(rr.Zin, s.Z0);
                if (rr.Grid != null)
                {
                    s.Zenit = rr.Grid.Total[0, 0];
                    int it = Math.Max(0, Math.Min(rr.Grid.NTheta - 1, (int)Math.Round(rr.ThetaMax / rr.Grid.ThetaStep)));
                    int ip = Math.Max(0, Math.Min(rr.Grid.NPhi - 1, (int)Math.Round(rr.PhiMax / rr.Grid.PhiStep)));
                    s.Orizzontale = rr.Grid.Hor[it, ip] >= rr.Grid.Vert[it, ip];
                }
                if (rr.AzCut != null)
                {
                    double mx = double.MinValue, mn = double.MaxValue;
                    foreach (double v in rr.AzCut) if (v > -900) { mx = Math.Max(mx, v); mn = Math.Min(mn, v); }
                    s.Ondulazione = mx > mn ? mx - mn : 0;
                }
                double xmin = double.MaxValue, xmax = double.MinValue, ymin = double.MaxValue, ymax = double.MinValue;
                for (int i = 0; i < g.N; i++)
                {
                    xmin = Math.Min(xmin, Math.Min(g.X1[i], g.X2[i])); xmax = Math.Max(xmax, Math.Max(g.X1[i], g.X2[i]));
                    ymin = Math.Min(ymin, Math.Min(g.Y1[i], g.Y2[i])); ymax = Math.Max(ymax, Math.Max(g.Y1[i], g.Y2[i]));
                }
                double dx = xmax - xmin, dy = ymax - ymin;
                s.Largo = Math.Max(dx, dy); s.Profondo = Math.Min(dx, dy); s.Alto = zmax - zmin;
                s.Fuori = q.IngombroMax > 0 && s.Largo > q.IngombroMax;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { s.Errore = ex.GetBaseException().Message; }
            return s;
        }

        static void Estremi(SegmentGeometry g, out double zmin, out double zmax)
        {
            zmin = double.MaxValue; zmax = double.MinValue;
            for (int i = 0; i < g.N; i++) { zmin = Math.Min(zmin, Math.Min(g.Z1[i], g.Z2[i])); zmax = Math.Max(zmax, Math.Max(g.Z1[i], g.Z2[i])); }
            if (zmin > zmax) { zmin = zmax = 0; }
        }
    }
}
