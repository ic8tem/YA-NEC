// YA-NEC - ottimizzatore (fase 5, bozzetto approvato doc/bozzetto-ottimizzatore.png). GPL-3.0.
//
// Obiettivi pesati con soglie (jX, ROS, guadagno, F/B, elevazione, impedenza voluta, corrente in un punto), piu' frequenze,
// variabili della linguetta Variabili con minimo, massimo e passo. Idee: ottimizzatore di MMANA-GAL (obiettivi pesati con
// cursori, "una variabile alla volta"), xnec2c-optimize e AutoEZ (variabili SY); metodo del simplesso di Nelder e Mead (1965).
// Nessun codice preso da quei programmi.
//
// Voto (0-100): per ogni frequenza e ogni obiettivo un punteggio tra 0 e 1, media pesata con i pesi dei cursori, poi media
// sulle frequenze. Raggiunta la soglia il punteggio di quell'obiettivo vale 1 e smette di tirare. Punteggi:
//   jX         1 / (1 + (X / (0,2 Z0))^2)                         soglia: |X| <= valore
//   ROS        1 - |gamma|^2 rispetto a Z0 (potenza adattata)     soglia: ROS <= valore
//   guadagno   1 / (1 + exp(-(G - G0) / 1 dB)), G0 = partenza      soglia: G >= valore (dBi)
//   F/B        1 - exp(-F/B / 15 dB)                               soglia: F/B >= valore
//   elevazione exp(-((el - voluta) / 10 gradi)^2)                  il valore e' l'elevazione voluta del massimo
//   impedenza  1 - |(Z - Zv) / (Z + Zv)|^2                         il valore e' Zv (es. 28 +j0)
//   corrente   1 - |I punto| / |I massima|                         corrente piu' bassa possibile nel punto (trappole, nodi)
// Elevazione e corrente si calcolano alla frequenza centrale con il diagramma completo; gli altri con lo sweep in frequenza
// nella direzione del massimo del modello di partenza.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Threading;

namespace YaNec.Engine
{
    public class OttVariabile
    {
        public string Nome = "";
        public bool Attiva = true;
        public double Valore, Min, Max, Passo;
    }

    public class OttObiettivo
    {
        public string Chiave = "";      // jx, ros, g, fb, el, z, i
        public double Peso;             // 0-100
        public bool HaSoglia;
        public double Soglia;           // jx, ros, g, fb, el
        public Complex Zvoluta = new Complex(50, 0);
        public int Tag, Seg;            // corrente in un punto
    }

    public class OttPasso
    {
        public int N;
        public double[] Valori;
        public double Voto = -1;
        public double G, FB, RosPeggiore, Elev;
        public Complex Z;               // alla frequenza principale
        public bool Valido;
        public string Errore;
        public bool Rispetta = true;    // 1.5: tutte le soglie rispettate a tutte le frequenze
    }

    /// <summary>1.5 (bozzetto "Monte Carlo nell'Ottimizzatore" approvato da IC8TEM il 18/11/2026 19:36): un candidato controllato con le tolleranze.</summary>
    public class OttCandidato
    {
        public string Nome = "";
        public OttPasso Passo;
        public double Percentuale = double.NaN, VotoPeggiore = double.NaN, VotoMedio = double.NaN, FbPeggiore = double.NaN, GPeggiore = double.NaN, RosPeggiore = double.NaN;
        public List<double> FbCopie = new List<double>();
        public bool Proposto;
    }

    public class Ottimizzatore
    {
        public NecModel Partenza;
        public List<OttVariabile> Variabili = new List<OttVariabile>();
        public List<OttObiettivo> Obiettivi = new List<OttObiettivo>();
        public List<double> Frequenze = new List<double>();
        public int Principale;                    // indice della frequenza principale (in grassetto nel bozzetto)
        public bool MantieniBoom;
        public bool Simplesso;                    // falso = una variabile alla volta (come MMANA)
        public int MaxPassi = 3000;
        /// <summary>1.0.17: direzione "avanti" fissata (theta, phi in gradi); NaN = quella del massimo del modello di partenza (come prima).
        /// Serve quando il modello di partenza non ha ancora il lobo dove lo si vuole (Yagi di MathWorks: tutti gli elementi a 0,5 lambda).</summary>
        public double ThetaAvanti = double.NaN, PhiAvanti = double.NaN;

        // 1.5: Monte Carlo (versione leggera): partenze a caso e controllo dei migliori con le tolleranze di costruzione
        public int PartenzeCaso = 0;              // partenze a caso oltre al modello aperto (0 = come prima)
        public int NumCandidati = 0, Copie = 50, SceltaMc;   // SceltaMc: 0 percentuale entro le soglie, 1 caso peggiore, 2 voto medio
        public int Seme = 1234;
        public double TolL = 1.0, TolP = 2.0, TolD = 0.1;    // mm, dalla linguetta Tolleranze
        public bool Gauss;
        public readonly List<OttCandidato> Candidati = new List<OttCandidato>();
        public event Action<string> Fase;                    // testo della fase in corso (partenza n di N, controllo...)

        public OttPasso Iniziale, Migliore;
        public readonly List<OttPasso> Registro = new List<OttPasso>();   // ultimi 128 passi che hanno migliorato o richiesto
        public int Passi;
        public event Action<OttPasso, bool> PassoFatto;                  // ogni prova calcolata (non quelle gia' in memoria)

        double thFwd, phFwd, boom0 = double.NaN;
        double[] g0;
        List<OttVariabile> att;
        readonly Dictionary<string, OttPasso> cache = new Dictionary<string, OttPasso>();

        public static double Valuta(string testo)
        {
            string t = Expr.NormalizeDecimal((testo ?? "").Trim().Replace(" ", ""));
            return Expr.Eval(t, null);
        }

        /// <summary>Esegue l'ottimizzazione fino alla fine, a MaxPassi o all'annullamento.</summary>
        public void Esegui(CancellationToken ct)
        {
            if (Frequenze.Count == 0) throw new NecException("Manca la frequenza da rispettare.");
            double somma = 0;
            foreach (OttObiettivo o in Obiettivi) somma += Math.Max(0, o.Peso);
            if (somma <= 0) throw new NecException("Tutti i pesi sono a zero: sposta almeno un cursore a destra.");
            att = Variabili.FindAll(v => v.Attiva && v.Max > v.Min);
            if (att.Count == 0) throw new NecException("Nessuna variabile spuntata con minimo e massimo diversi.");
            Principale = Math.Max(0, Math.Min(Frequenze.Count - 1, Principale));
            // direzione del massimo e guadagni di partenza
            SegmentGeometry g = Partenza.BuildGeometry();
            RunResult r0 = Analysis.Run(Partenza, g, Frequenze[Principale], 2.0, ct);
            thFwd = double.IsNaN(ThetaAvanti) ? r0.ThetaMax : ThetaAvanti; phFwd = double.IsNaN(PhiAvanti) ? r0.PhiMax : PhiAvanti;
            List<SweepPoint> sp0 = Sweep(Partenza, g, ct);
            g0 = new double[Frequenze.Count];
            for (int i = 0; i < g0.Length; i++) g0[i] = sp0[i].Ok ? sp0[i].GainFwd : r0.GmaxDbi;
            if (MantieniBoom) boom0 = Boom(Partenza, g);
            double[] x = new double[att.Count];
            for (int i = 0; i < x.Length; i++) x[i] = Math.Max(att[i].Min, Math.Min(att[i].Max, att[i].Valore));
            Iniziale = Prova(x, ct);
            if (!Iniziale.Valido) throw new NecException("Il modello di partenza non si calcola: " + Iniziale.Errore);
            Migliore = Iniziale;
            Annota(Iniziale, true);
            Candidati.Clear();
            if (PartenzeCaso <= 0 && NumCandidati <= 0) { if (Simplesso) NelderMead(ct); else UnaAllaVolta(ct); return; }
            Random rnd = new Random(Seme);
            List<OttCandidato> fine = new List<OttCandidato>();
            int maxTot = MaxPassi;
            for (int k = 0; k <= Math.Max(0, PartenzeCaso) && !ct.IsCancellationRequested; k++)
            {
                if (k > 0)
                {
                    double[] xr = new double[att.Count];
                    for (int i = 0; i < xr.Length; i++) xr[i] = att[i].Min + rnd.NextDouble() * (att[i].Max - att[i].Min);
                    OttPasso p0 = Prova(xr, ct);
                    if (!p0.Valido) continue;
                    Migliore = p0;
                }
                Fase?.Invoke(k == 0 ? "partenza dal modello aperto" : "partenza a caso " + k + " di " + PartenzeCaso);
                MaxPassi = Passi + maxTot;
                if (Simplesso) NelderMead(ct); else UnaAllaVolta(ct);
                fine.Add(new OttCandidato { Nome = k == 0 ? "modello aperto" : "partenza " + k, Passo = Migliore });
            }
            MaxPassi = maxTot;
            fine.Sort((a, b) => b.Passo.Voto.CompareTo(a.Passo.Voto));
            // doppioni (due partenze finite nello stesso punto): si tiene il primo
            List<OttCandidato> unici = new List<OttCandidato>();
            foreach (OttCandidato c in fine) if (!unici.Exists(u => u.Passo == c.Passo)) unici.Add(c);
            for (int i = 0; i < unici.Count && i < Math.Max(1, NumCandidati) && !ct.IsCancellationRequested; i++)
            {
                Fase?.Invoke("controllo con le tolleranze: candidato " + (i + 1) + " di " + Math.Min(unici.Count, Math.Max(1, NumCandidati)));
                ControlloTolleranze(unici[i], rnd, ct);
                Candidati.Add(unici[i]);
            }
            if (Candidati.Count == 0) return;
            OttCandidato best = Candidati[0];
            foreach (OttCandidato c in Candidati)
            {
                if (double.IsNaN(c.Percentuale)) continue;
                if (SceltaMc == 0 && (c.Percentuale > best.Percentuale + 1e-9 || (Math.Abs(c.Percentuale - best.Percentuale) < 1e-9 && c.Passo.Voto > best.Passo.Voto))) best = c;
                else if (SceltaMc == 1 && c.VotoPeggiore > best.VotoPeggiore) best = c;
                else if (SceltaMc == 2 && c.VotoMedio > best.VotoMedio) best = c;
            }
            best.Proposto = true;
            Migliore = best.Passo;
            Annota(Migliore, true);
        }

        /// <summary>Copie del candidato con le misure spostate a caso entro le tolleranze (lunghezza, posizione sul boom, diametro di ogni elemento).</summary>
        void ControlloTolleranze(OttCandidato c, Random rnd, CancellationToken ct)
        {
            NecModel m = Modello(c.Passo.Valori);
            SegmentGeometry g0 = m.BuildGeometry();
            int asse;
            List<Tolleranze.Elemento> el = Tolleranze.Elementi(m, g0, out asse);
            int ok = 0, n = 0; double somma = 0, peggiore = double.MaxValue, fbP = double.MaxValue, gP = double.MaxValue, rosP = 0;
            for (int i = 0; i < Math.Max(1, Copie) && !ct.IsCancellationRequested; i++)
            {
                SegmentGeometry g = Tolleranze.Copia(g0);
                foreach (Tolleranze.Elemento e in el)
                    Tolleranze.Applica(g, e, asse, Tolleranze.Errore(rnd, TolL, Gauss) / 1000, Tolleranze.Errore(rnd, TolP, Gauss) / 1000, Tolleranze.Errore(rnd, TolD, Gauss) / 1000, 0);
                OttPasso r = new OttPasso { Valori = c.Passo.Valori, N = -1 };
                try { Calcola(m, g, r, ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { r.Valido = false; r.Errore = ex.Message; }
                if (!r.Valido) continue;
                n++;
                bool rispetta = HaSoglie() ? r.Rispetta : r.Voto >= c.Passo.Voto - 5;
                if (rispetta) ok++;
                somma += r.Voto; peggiore = Math.Min(peggiore, r.Voto);
                fbP = Math.Min(fbP, r.FB); gP = Math.Min(gP, r.G); rosP = Math.Max(rosP, r.RosPeggiore);
                c.FbCopie.Add(r.FB);
            }
            if (n == 0) return;
            c.Percentuale = 100.0 * ok / n; c.VotoMedio = somma / n; c.VotoPeggiore = peggiore;
            c.FbPeggiore = fbP; c.GPeggiore = gP; c.RosPeggiore = rosP;
        }

        bool HaSoglie() { return Obiettivi.Exists(o => o.Peso > 0 && o.HaSoglia && o.Chiave != "el" && o.Chiave != "z" && o.Chiave != "i"); }

        // ---------------------------------------------------------------------------------------------
        // metodi
        // ---------------------------------------------------------------------------------------------
        void UnaAllaVolta(CancellationToken ct)
        {
            // passo iniziale: 8 volte il passo dichiarato (al massimo un quarto dell'intervallo), poi si dimezza fino al passo
            double[] st = new double[att.Count];
            for (int i = 0; i < st.Length; i++) st[i] = Math.Max(att[i].Passo, Math.Min(8 * att[i].Passo, (att[i].Max - att[i].Min) / 4));
            while (Passi < MaxPassi && !ct.IsCancellationRequested)
            {
                bool migliorato = false;
                for (int i = 0; i < att.Count && Passi < MaxPassi && !ct.IsCancellationRequested; i++)
                {
                    foreach (int dir in new[] { 1, -1 })
                    {
                        bool avanti = true, mosso = false;
                        while (avanti && Passi < MaxPassi && !ct.IsCancellationRequested)
                        {
                            double[] y = (double[])Migliore.Valori.Clone();
                            y[i] = y[i] + dir * st[i];
                            if (y[i] < att[i].Min - 1e-12 || y[i] > att[i].Max + 1e-12) break;
                            OttPasso p = Prova(y, ct);
                            if (p.Valido && p.Voto > Migliore.Voto + 1e-9) { Migliore = p; Annota(p, true); mosso = migliorato = true; }
                            else avanti = false;
                        }
                        if (mosso) break;
                    }
                }
                if (!migliorato)
                {
                    bool finito = true;
                    for (int i = 0; i < st.Length; i++)
                        if (st[i] > att[i].Passo * 1.0001) { st[i] = Math.Max(att[i].Passo, st[i] / 2); finito = false; }
                    if (finito) break;
                }
            }
        }

        void NelderMead(CancellationToken ct)
        {
            int n = att.Count;
            // coordinate normalizzate 0..1 nell'intervallo di ogni variabile
            Func<double[], double[]> reale = u =>
            {
                double[] y = new double[n];
                for (int i = 0; i < n; i++) y[i] = att[i].Min + Math.Max(0, Math.Min(1, u[i])) * (att[i].Max - att[i].Min);
                return y;
            };
            Func<double[], double> f = u => { OttPasso p = Prova(reale(u), ct); if (p.Valido && p.Voto > Migliore.Voto + 1e-9) { Migliore = p; Annota(p, true); } return p.Valido ? -p.Voto : 1e9; };
            double[][] s = new double[n + 1][];
            double[] fv = new double[n + 1];
            s[0] = new double[n];
            for (int i = 0; i < n; i++) s[0][i] = (Migliore.Valori[i] - att[i].Min) / (att[i].Max - att[i].Min);
            fv[0] = -Migliore.Voto;
            for (int k = 1; k <= n; k++)
            {
                s[k] = (double[])s[0].Clone();
                s[k][k - 1] += s[k][k - 1] < 0.85 ? 0.15 : -0.15;
                fv[k] = f(s[k]);
            }
            int giri = 0;   // le prove gia' in memoria non contano come passi: limite anche sui giri
            while (Passi < MaxPassi && giri++ < 20 * MaxPassi && !ct.IsCancellationRequested)
            {
                Array.Sort(fv, s);
                if (Math.Abs(fv[n] - fv[0]) < 0.01)
                {
                    // simplesso ristretto: basta se e' gia' sotto i passi dichiarati
                    bool piccolo = true;
                    for (int i = 0; i < n; i++)
                        for (int k = 1; k <= n; k++)
                            if (Math.Abs(s[k][i] - s[0][i]) * (att[i].Max - att[i].Min) > att[i].Passo) piccolo = false;
                    if (piccolo) break;
                }
                double[] c = new double[n];
                for (int k = 0; k < n; k++) for (int i = 0; i < n; i++) c[i] += s[k][i] / n;
                Func<double, double[]> lungo = a => { double[] y = new double[n]; for (int i = 0; i < n; i++) y[i] = c[i] + a * (s[n][i] - c[i]); return y; };
                double[] xr = lungo(-1); double fr = f(xr);
                if (fr < fv[0])
                {
                    double[] xe = lungo(-2); double fe = f(xe);
                    if (fe < fr) { s[n] = xe; fv[n] = fe; } else { s[n] = xr; fv[n] = fr; }
                }
                else if (fr < fv[n - 1]) { s[n] = xr; fv[n] = fr; }
                else
                {
                    double[] xc = fr < fv[n] ? lungo(-0.5) : lungo(0.5); double fc = f(xc);
                    if (fc < Math.Min(fr, fv[n])) { s[n] = xc; fv[n] = fc; }
                    else
                        for (int k = 1; k <= n; k++)
                        {
                            for (int i = 0; i < n; i++) s[k][i] = s[0][i] + 0.5 * (s[k][i] - s[0][i]);
                            fv[k] = f(s[k]);
                        }
                }
            }
        }

        // ---------------------------------------------------------------------------------------------
        // valutazione di un punto
        // ---------------------------------------------------------------------------------------------
        OttPasso Prova(double[] x, CancellationToken ct)
        {
            // valori arrotondati al passo: misure costruibili e niente doppioni
            double[] y = new double[x.Length];
            System.Text.StringBuilder k = new System.Text.StringBuilder();
            for (int i = 0; i < x.Length; i++)
            {
                double p = att[i].Passo > 0 ? att[i].Passo : 1e-4;
                y[i] = Math.Max(att[i].Min, Math.Min(att[i].Max, att[i].Min + Math.Round((x[i] - att[i].Min) / p) * p));
                k.Append(y[i].ToString("R", CultureInfo.InvariantCulture)).Append(';');
            }
            OttPasso vecchio;
            if (cache.TryGetValue(k.ToString(), out vecchio)) return vecchio;
            ct.ThrowIfCancellationRequested();
            OttPasso r = new OttPasso { Valori = y, N = Passi++ };
            try
            {
                NecModel m = Modello(y);
                SegmentGeometry g = m.BuildGeometry();
                if (MantieniBoom && Math.Abs(Boom(m, g) - boom0) > 5e-4) throw new NecException("lunghezza del boom cambiata");
                Calcola(m, g, r, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { r.Valido = false; r.Errore = ex.Message; }
            cache[k.ToString()] = r;
            if (PassoFatto != null) PassoFatto(r, false);
            return r;
        }

        /// <summary>Voto e risultati di un modello gia' costruito (anche con la geometria spostata dal Monte Carlo). Rispetta: tutte le soglie a tutte le frequenze.</summary>
        void Calcola(NecModel m, SegmentGeometry g, OttPasso r, CancellationToken ct)
        {
            {
                List<SweepPoint> sp = Sweep(m, g, ct);
                RunResult rc = null;
                foreach (OttObiettivo o in Obiettivi)
                    if (o.Peso > 0 && (o.Chiave == "el" || o.Chiave == "i")) { rc = Analysis.Run(m, g, Frequenze[Principale], 2.0, ct); break; }
                double voto = 0, pesi = 0, rosMax = 0;
                for (int fi = 0; fi < Frequenze.Count; fi++)
                {
                    SweepPoint p = sp[fi];
                    if (!p.Ok) throw new NecException(p.Error ?? "calcolo non riuscito");
                    rosMax = Math.Max(rosMax, p.Swr);
                    foreach (OttObiettivo o in Obiettivi)
                    {
                        if (o.Peso <= 0) continue;
                        double s = Punteggio(o, p, fi, rc, m.Z0);
                        voto += o.Peso * s; pesi += o.Peso;
                        if (o.HaSoglia && o.Chiave != "el" && o.Chiave != "z" && o.Chiave != "i" && s < 1) r.Rispetta = false;
                    }
                }
                SweepPoint pp = sp[Principale];
                r.Voto = 100 * voto / pesi;
                r.G = pp.GainFwd; r.FB = pp.FrontBack; r.Z = pp.Z; r.RosPeggiore = rosMax;
                r.Elev = rc != null ? rc.ElevationOfMax : 90 - thFwd;
                r.Valido = true;
            }
        }

        double Punteggio(OttObiettivo o, SweepPoint p, int fi, RunResult rc, double z0)
        {
            switch (o.Chiave)
            {
                case "jx":
                    if (o.HaSoglia && Math.Abs(p.Z.Imaginary) <= o.Soglia) return 1;
                    { double q = p.Z.Imaginary / (0.2 * z0); return 1 / (1 + q * q); }
                case "ros":
                    if (o.HaSoglia && p.Swr <= o.Soglia) return 1;
                    { Complex gm = (p.Z - z0) / (p.Z + z0); return 1 - gm.Magnitude * gm.Magnitude; }
                case "g":
                    if (o.HaSoglia && p.GainFwd >= o.Soglia) return 1;
                    return 1 / (1 + Math.Exp(-(p.GainFwd - g0[fi])));
                case "fb":
                    if (o.HaSoglia && p.FrontBack >= o.Soglia) return 1;
                    return 1 - Math.Exp(-Math.Max(0, p.FrontBack) / 15);
                case "fr":   // 1.2.0 (bozzetto 11 A): F/R medio sull'emisfero posteriore
                    {
                        double fr = double.IsNaN(p.FrontRearMedio) ? 0 : p.FrontRearMedio;
                        if (o.HaSoglia && fr >= o.Soglia) return 1;
                        return 1 - Math.Exp(-Math.Max(0, fr) / 15);
                    }
                case "el":
                    if (rc == null || !o.HaSoglia) return 1;
                    { double q = (rc.ElevationOfMax - o.Soglia) / 10; return Math.Exp(-q * q); }
                case "z":
                    { Complex gm = (p.Z - o.Zvoluta) / (p.Z + o.Zvoluta); return 1 - Math.Min(1, gm.Magnitude * gm.Magnitude); }
                case "i":
                    {
                        if (rc == null) return 1;
                        double imax = 1e-30, ip = 0;
                        int conta = 0;
                        for (int s = 0; s < rc.Geometry.N; s++)
                        {
                            double a = Complex.Abs(rc.Engine.SegmentCurrent(s));
                            imax = Math.Max(imax, a);
                            if (rc.Geometry.Tag[s] == o.Tag && ++conta == o.Seg) ip = a;
                        }
                        return 1 - Math.Min(1, ip / imax);
                    }
            }
            return 1;
        }

        List<SweepPoint> Sweep(NecModel m, SegmentGeometry g, CancellationToken ct)
        {
            // poche frequenze: lo sweep le calcola tutte (MBPE solo da 9 punti in su)
            return Analysis.Sweep(m, g, Frequenze, thFwd, phFwd, null, ct);
        }

        /// <summary>Modello con i valori dati alle variabili attive (numeri in metri o nell'unita' del valore).</summary>
        public NecModel Modello(double[] y)
        {
            NecModel m = Partenza.Clone();
            for (int i = 0; i < att.Count; i++)
            {
                SyVar v = m.Variables.Find(q => string.Equals(q.Name, att[i].Nome, StringComparison.OrdinalIgnoreCase));
                if (v != null) v.Expr = y[i].ToString("R", CultureInfo.InvariantCulture);
            }
            List<string> err = m.Reevaluate();
            if (err.Count > 0) throw new NecException(err[0]);
            return m;
        }

        // lunghezza del boom: estensione dei centri dei fili lungo l'asse piu' lungo tra quelli perpendicolari al filo alimentato
        public static double Boom(NecModel m, SegmentGeometry g)
        {
            int sTag = m.Sources.Count > 0 ? m.Sources[0].Tag : g.Tag[0];
            double dx = 0, dy = 0, dz = 0;
            for (int i = 0; i < g.N; i++)
                if (g.Tag[i] == sTag) { dx += Math.Abs(g.X2[i] - g.X1[i]); dy += Math.Abs(g.Y2[i] - g.Y1[i]); dz += Math.Abs(g.Z2[i] - g.Z1[i]); }
            int asseFilo = dx >= dy && dx >= dz ? 0 : (dy >= dz ? 1 : 2);
            double best = 0;
            for (int a = 0; a < 3; a++)
            {
                if (a == asseFilo) continue;
                double mn = double.MaxValue, mx = double.MinValue;
                for (int i = 0; i < g.N; i++)
                {
                    double c = a == 0 ? (g.X1[i] + g.X2[i]) / 2 : a == 1 ? (g.Y1[i] + g.Y2[i]) / 2 : (g.Z1[i] + g.Z2[i]) / 2;
                    mn = Math.Min(mn, c); mx = Math.Max(mx, c);
                }
                best = Math.Max(best, mx - mn);
            }
            return best;
        }

        void Annota(OttPasso p, bool migliore)
        {
            lock (Registro)
            {
                Registro.Add(p);
                if (Registro.Count > 128) Registro.RemoveAt(0);
            }
        }
    }
}
