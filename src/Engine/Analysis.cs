// YA-NEC - calcoli di alto livello: soluzione a una frequenza, diagramma 3D, F/B, F/R, apertura, sweep in frequenza.
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace YaNec.Engine
{
    public class PatternGrid
    {
        public double ThetaStep, PhiStep;
        public int NTheta, NPhi;
        public double[,] Total;      // dBi [itheta, iphi]
        public double[,] Vert, Hor;
        public double ThetaAt(int i) { return i * ThetaStep; }
        public double PhiAt(int j) { return j * PhiStep; }
    }

    public class RunResult
    {
        public NecModel Model;
        public SegmentGeometry Geometry;
        public Nec2Engine Engine;
        public double FreqMHz;
        public List<SourceResult> Sources = new List<SourceResult>();
        public Complex Zin;
        public double Swr, Z0;
        public double InputPower, StructureLoss, Efficiency;
        // direzione e valore del massimo (elevazione = 90 - theta)
        public double GmaxDbi, ThetaMax, PhiMax;
        public double FrontBack, FrontRear;
        public double FrontRearMedio = double.NaN;   // 1.2.0: F/R medio sull'emisfero posteriore (azimut 90-270, elevazioni sopra 0)
        public double HpbwAz, HpbwEl;
        public double AgtDb;   // Average Gain Test (0 dB = modello corretto, senza perdite e spazio libero)
        public PatternGrid Grid;
        public double[] AzCut, ElCut;   // tagli a 1 grado: azimut (phi 0..359 all'elevazione del massimo), elevazione (theta 0..180 / 0..90)
        public double[] AzCutVert, AzCutHor, ElCutVert, ElCutHor;
        public double ElevationOfMax { get { return 90.0 - ThetaMax; } }
        public List<string> Warnings = new List<string>();
        public double MsDiagramma;   // 0.4.18: tempo del diagramma 3D (campo lontano), per il collaudo dei modelli pesanti
    }

    public class SweepPoint
    {
        public double F;
        public Complex Z;
        public double Swr, GainFwd, FrontBack, FrontRear, Gmax, Efficiency;
        public double FrontRearMedio = double.NaN;   // 1.2.0 (bozzetto 11 A): avanti meno media della potenza sull'emisfero posteriore
        public bool Ok;
        public bool Interpolato;   // sweep rapido: punto ricavato dai punti calcolati (MBPE), non da una soluzione
        public string Error;
    }

    public static class Analysis
    {
        /// <summary>1.2.0 (bozzetto 11 A): media della potenza (guadagno lineare) sull'emisfero posteriore rispetto alla direzione (theta, phi):
        /// azimut da 90 a 270 gradi intorno a phi, elevazioni sopra 0 (pesate con il coseno dell'elevazione, angolo solido), passo 5 gradi. In dB.</summary>
        public static double MediaPosteriore(Nec2Engine e, double phiFwd)
        {
            double s = 0, w = 0;
            for (int el = 0; el <= 85; el += 5)
            {
                double c = Math.Cos((el + 2.5) * Math.PI / 180);
                for (int k = 90; k <= 270; k += 5)
                {
                    double g = e.Gain(90 - (el + 2.5), Norm360(phiFwd + k)).GainTotal;
                    double pk = (k == 90 || k == 270) ? 0.5 : 1;
                    s += c * pk * (g < -900 ? 0 : Math.Pow(10, g / 10)); w += c * pk;
                }
            }
            return s > 0 ? 10 * Math.Log10(s / w) : -999.99;
        }

        public static double Swr(Complex z, double z0)
        {
            Complex g = (z - z0) / (z + z0);
            double m = Complex.Abs(g);
            if (m >= 0.999999) return 999;
            return (1 + m) / (1 - m);
        }

        /// <summary>Sorgenti da usare a questa frequenza: con le sorgenti a corrente si ricavano le tensioni (V = Z I).</summary>
        public static IList<SourceSpec> EffectiveSources(NecModel model, SegmentGeometry geo, double fmhz)
        {
            if (!model.CurrentSources || model.Sources.Count == 0) return model.Sources;
            Complex[] want = new Complex[model.Sources.Count];
            for (int k = 0; k < want.Length; k++) want[k] = model.Sources[k].V;
            NecModel c = model.Clone();
            SimpleCalc.SetCurrentDrive(c, geo, fmhz, want);
            return c.Sources;
        }

        public static Complex Reflection(Complex z, double z0) { return (z - z0) / (z + z0); }

        /// <summary>Calcolo completo a una frequenza: correnti, impedenza, diagramma 3D e parametri del lobo.</summary>
        /// <summary>Con la correzione Leeson calcola anche l'impedenza senza correzione (seconda soluzione: raddoppia il tempo).</summary>
        public static bool ConfrontoLeeson;

        /// <summary>0.4.31: geometria da calcolare alla frequenza f, con la correzione di Leeson se il modello la chiede (come in Run).
        /// Gli accordi dei calcolatori la usano, cosi' accordano lo stesso modello che poi Run verifica (collaudo 0.4.30: la verticale
        /// a tubi telescopici accordata senza Leeson e verificata con Leeson restava a X = -11,5 ohm).</summary>
        public static SegmentGeometry GeometriaDiCalcolo(NecModel model, double fmhz)
        {
            if (model.Leeson)
            {
                NecModel ml = Leeson.Applica(model, fmhz, null);
                if (!ReferenceEquals(ml, model)) return ml.BuildGeometry();
            }
            return model.BuildGeometry();
        }

        public static RunResult Run(NecModel model, SegmentGeometry geo, double fmhz, double gridStep = 2.0, CancellationToken ct = default(CancellationToken))
        {
            using Prestazioni.Scopo misura = Prestazioni.Misura("Calcolo NEC", geo.N + " segmenti, " + fmhz.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " MHz, diagramma a " + gridStep.ToString(System.Globalization.CultureInfo.InvariantCulture) + " gradi");
            RunResult r = new RunResult();
            List<string> noteLeeson = null;
            SegmentGeometry r0geo = geo;
            if (model.Leeson)
            {
                // correzione dei diametri a gradini alla frequenza di calcolo (stessi segmenti e stesse etichette)
                noteLeeson = new List<string>();
                NecModel ml = Leeson.Applica(model, fmhz, noteLeeson);
                if (!ReferenceEquals(ml, model)) geo = ml.BuildGeometry();
            }
            r.Model = model;
            r.Geometry = geo;
            r.FreqMHz = fmhz;
            r.Z0 = model.Z0;
            // 0.4.18: limiti del NEC-2 per i fili vicini al terreno e per le giunzioni affollate (Butternut con 64 radiali, BOG)
            try { r.Warnings.AddRange(TerrenoVicino.Controlla(geo, model.Ground, fmhz)); } catch { }
            Nec2Engine e = new Nec2Engine(geo);
            e.Solve(fmhz, EffectiveSources(model, geo, fmhz), model.EffectiveLoads(), TerreniEdifici.Alla(model.Ground, fmhz), model.ExtendedKernel, model.Rkh, model.EffectiveNetworks(fmhz));
            if (e.PivotWarning != null) r.Warnings.Add(e.PivotWarning);
            r.Engine = e;
            r.Sources = e.Sources;
            r.Zin = e.Sources[0].Z;
            r.Swr = Swr(r.Zin, model.Z0);
            { string avc = TerreniEdifici.Avviso(model.Ground, fmhz); if (avc != null) r.Warnings.Add(avc); }
            if (noteLeeson != null)
            {
                r.Warnings.AddRange(noteLeeson);
                // come chiesto per la Yagi 310-08 di Cebik: si mostra anche il risultato senza correzione.
                // Dalla 0.4.2 solo a richiesta (Impostazioni, Tubi telescopici, "Leeson + confronto"): e' una seconda soluzione completa
                if (ConfrontoLeeson && !ReferenceEquals(geo, r0geo))
                {
                    try
                    {
                        Nec2Engine e0 = new Nec2Engine(r0geo);
                        e0.Solve(fmhz, EffectiveSources(model, r0geo, fmhz), model.EffectiveLoads(), TerreniEdifici.Alla(model.Ground, fmhz), model.ExtendedKernel, model.Rkh, model.EffectiveNetworks(fmhz));
                        Complex zSenza = e0.Sources[0].Z;
                        r.Warnings.Add(string.Format(System.Globalization.CultureInfo.CurrentCulture, "Leeson: senza correzione Z = {0:0.0} {1}j{2:0.0} ohm (con la correzione {3:0.0} {4}j{5:0.0} ohm).",
                            zSenza.Real, zSenza.Imaginary >= 0 ? "+" : "-", Math.Abs(zSenza.Imaginary), r.Zin.Real, r.Zin.Imaginary >= 0 ? "+" : "-", Math.Abs(r.Zin.Imaginary)));
                    }
                    catch { }
                }
            }
            r.InputPower = e.InputPower;
            r.StructureLoss = e.StructureLoss;
            r.Efficiency = e.InputPower > 0 ? 100.0 * (e.InputPower - e.StructureLoss) / e.InputPower : 0;
            ct.ThrowIfCancellationRequested();

            // diagramma 3D (0.4.20: in una funzione a parte, per poterlo rifare a passo piu' fine dopo il calcolo)
            bool gnd = e.HasGround;
            double best, bt, bp, msDiag;
            PatternGrid g = Griglia(e, gridStep, ct, out best, out bt, out bp, out msDiag);
            r.MsDiagramma = msDiag;
            r.AgtDb = Agt(g, e);
            // 0.4.30 (punto 8 approvato da IC8TEM il 14/11/2026 22:08, "correggi"): sulla loop magnetica della linguetta Loop (Q di migliaia,
            // anello di Faraday) il bilancio del NEC-2 dava efficienza negativa (-20/-25 %): la potenza d'ingresso Re(V I*)/2 e' la
            // differenza di numeri quasi uguali e la perdita nel filo la supera. Il campo lontano invece viene dalle correnti, che sono
            // giuste: in spazio libero la media del guadagno sulla sfera e' Prad/Pin. Se il bilancio non torna (efficienza negativa o
            // lontana piu' del 25 % dalla media del guadagno: l'AGT normale sta entro +-0,2 dB, cioe' il 5 %) l'efficienza si calcola come Prad / (Prad + Pperdite), con un avviso.
            if (!gnd && e.StructureLoss > 0 && e.InputPower > 0)
            {
                double prad = MediaGuadagno(g) * e.InputPower;
                double effBil = (e.InputPower - e.StructureLoss) / e.InputPower, effCampo = prad / (prad + e.StructureLoss);
                if (prad > 0 && (effBil < 0 || Math.Abs(effBil - prad / e.InputPower) > 0.25 * Math.Max(effBil, prad / e.InputPower)))
                {
                    r.Efficiency = 100.0 * effCampo;
                    r.Warnings.Add(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        "Bilancio di potenza del NEC-2 non affidabile (ingresso {0:0.###E0} W, perdite {1:0.###E0} W: efficienza {2:0.0} %), tipico dei piccoli loop ad alto Q: " +
                        "efficienza calcolata dal campo lontano, Prad / (Prad + perdite) = {3:0.0} %.", e.InputPower, e.StructureLoss, effBil * 100, effCampo * 100));
                }
            }

            // affinamento del massimo
            double step = gridStep / 2;
            for (int it = 0; it < 6; it++)
            {
                double bt0 = bt, bp0 = bp;
                for (int a = -2; a <= 2; a++)
                    for (int b = -2; b <= 2; b++)
                    {
                        double th = bt0 + a * step, ph = bp0 + b * step;
                        if (th < 0 || th > 180 || (gnd && th > 90)) continue;
                        double v = e.Gain(th, ph).GainTotal;
                        if (v > best + 1e-4) { best = v; bt = th; bp = ph; }
                    }
                step /= 2;
            }
            bp = Norm360(bp);
            r.GmaxDbi = best;
            r.ThetaMax = bt;
            r.PhiMax = bp;

            // tagli a 1 grado
            r.AzCut = new double[360]; r.AzCutVert = new double[360]; r.AzCutHor = new double[360];
            for (int k = 0; k < 360; k++)
            {
                FarFieldPoint p = e.Gain(bt, k);
                r.AzCut[k] = p.GainTotal; r.AzCutVert[k] = p.GainVert; r.AzCutHor[k] = p.GainHor;
            }
            int nel = 361;   // elevazione nel piano verticale del massimo: theta da 0 a 180 sul lato phiMax e dall'altro lato
            r.ElCut = new double[nel]; r.ElCutVert = new double[nel]; r.ElCutHor = new double[nel];
            for (int k = 0; k < nel; k++)
            {
                // k = 0..180: elevazione da 0 (orizzonte avanti) a 180 (orizzonte dietro), passando per lo zenit;
                // k = 181..360: sotto l'orizzonte (solo spazio libero)
                double el = k <= 180 ? k : k - 360;    // gradi di elevazione "avvolta"
                double th, ph;
                ElevToThetaPhi(el, bp, out th, out ph);
                if (gnd && th > 90) { r.ElCut[k] = r.ElCutVert[k] = r.ElCutHor[k] = -999.99; continue; }
                FarFieldPoint p = e.Gain(th, ph);
                r.ElCut[k] = p.GainTotal; r.ElCutVert[k] = p.GainVert; r.ElCutHor[k] = p.GainHor;
            }

            // F/B e F/R all'elevazione del massimo
            double back = e.Gain(bt, Norm360(bp + 180)).GainTotal;
            r.FrontBack = best - back;
            double worst = -1e9;
            for (int k = 90; k <= 270; k++)
            {
                double v = e.Gain(bt, Norm360(bp + k)).GainTotal;
                if (v > worst) worst = v;
            }
            r.FrontRear = best - worst;
            r.FrontRearMedio = best - MediaPosteriore(e, bp);
            r.HpbwAz = Beamwidth(a2 => e.Gain(bt, Norm360(bp + a2)).GainTotal, best);
            r.HpbwEl = Beamwidth(a2 =>
            {
                double th, ph;
                ElevToThetaPhi(90 - bt + a2, bp, out th, out ph);
                if (gnd && th > 90) return -999.99;
                return e.Gain(th, ph).GainTotal;
            }, best);
            r.Grid = g;
            return r;
        }

        // elevazione "avvolta" nel piano verticale che contiene la direzione phi0
        public static void ElevToThetaPhi(double el, double phi0, out double theta, out double phi)
        {
            double e = el;
            while (e > 180) e -= 360;
            while (e <= -180) e += 360;
            if (e >= -90 && e <= 90) { theta = 90 - e; phi = phi0; }
            else
            {
                double e2 = e > 0 ? 180 - e : -180 - e;
                theta = 90 - e2;
                phi = Norm360(phi0 + 180);
            }
        }

        public static double Norm360(double a)
        {
            a = a % 360.0;
            if (a < 0) a += 360.0;
            return a;
        }

        // apertura a -3 dB attorno a 0 (gradi), con interpolazione lineare
        static double Beamwidth(Func<double, double> g, double gmax)
        {
            double lim = gmax - 3.0;
            double left = double.NaN, right = double.NaN;
            double prev = g(0);
            for (double a = 0.5; a <= 180; a += 0.5)
            {
                double v = g(a);
                if (v < lim) { right = a - 0.5 + 0.5 * (prev - lim) / Math.Max(1e-9, prev - v); break; }
                prev = v;
            }
            prev = g(0);
            for (double a = 0.5; a <= 180; a += 0.5)
            {
                double v = g(-a);
                if (v < lim) { left = a - 0.5 + 0.5 * (prev - lim) / Math.Max(1e-9, prev - v); break; }
                prev = v;
            }
            if (double.IsNaN(left) || double.IsNaN(right)) return double.NaN;
            return left + right;
        }

        /// <summary>
        /// Diagramma 3D a passo gridStep (gradi) dalla soluzione gia' fatta; best/bt/bp: massimo sulla griglia (theta, phi).
        /// </summary>
        public static PatternGrid Griglia(Nec2Engine e, double gridStep, CancellationToken ct, out double best, out double bt, out double bp, out double ms)
        {
            bool gnd = e.HasGround;
            PatternGrid g = new PatternGrid();
            g.ThetaStep = gridStep;
            g.PhiStep = gridStep;
            g.NTheta = (int)Math.Round((gnd ? 90.0 : 180.0) / gridStep) + 1;
            g.NPhi = (int)Math.Round(360.0 / gridStep);
            g.Total = new double[g.NTheta, g.NPhi];
            g.Vert = new double[g.NTheta, g.NPhi];
            g.Hor = new double[g.NTheta, g.NPhi];
            // a parita' di guadagno (lobi a ciambella degli omnidirezionali) vince l'orizzonte verso +X:
            // si parte da (90, 0) e si cambia direzione solo se il guadagno e' davvero maggiore
            double b0 = e.Gain(90, 0).GainTotal, t0 = 90, p0 = 0;
            Prestazioni.Scopo misuraDiag = Prestazioni.Misura("Diagramma 3D (campo lontano)", gridStep.ToString(System.Globalization.CultureInfo.InvariantCulture) + " gradi", false);
            System.Diagnostics.Stopwatch swDiag = System.Diagnostics.Stopwatch.StartNew();
            // dalla 0.4.2 (approvato da IC8TEM il 13/11/2026): una riga di elevazione per processore. Il campo lontano (Ffld)
            // legge solo i dati della soluzione, quindi i thread non si disturbano; il massimo si cerca dopo, nello stesso
            // ordine di prima, cosi' il risultato e' identico al calcolo su un solo processore.
            ParallelOptions poDiag = new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = Processori.Calcolo };
            Parallel.For(0, g.NTheta, poDiag, i =>
            {
                double th = g.ThetaAt(i);
                for (int j = 0; j < g.NPhi; j++)
                {
                    FarFieldPoint p = e.Gain(th, g.PhiAt(j));
                    g.Total[i, j] = p.GainTotal;
                    g.Vert[i, j] = p.GainVert;
                    g.Hor[i, j] = p.GainHor;
                }
            });
            ct.ThrowIfCancellationRequested();
            for (int i = 0; i < g.NTheta; i++)
                for (int j = 0; j < g.NPhi; j++)
                    if (g.Total[i, j] > b0 + 0.005) { b0 = g.Total[i, j]; t0 = g.ThetaAt(i); p0 = g.PhiAt(j); }
            misuraDiag.Dispose();
            ms = swDiag.Elapsed.TotalMilliseconds;
            best = b0; bt = t0; bp = p0;
            return g;
        }

        /// <summary>0.4.30: media del guadagno (lineare) sulla sfera = potenza irradiata / potenza d'ingresso (spazio libero).</summary>
        public static double MediaGuadagno(PatternGrid g)
        {
            double dth = g.ThetaStep * Math.PI / 180.0, dph = g.PhiStep * Math.PI / 180.0, sum = 0;
            for (int i = 0; i < g.NTheta; i++)
            {
                double th = g.ThetaAt(i) * Math.PI / 180.0;
                double w = Math.Cos(Math.Max(0, th - dth / 2)) - Math.Cos(Math.Min(Math.PI, th + dth / 2));
                for (int j = 0; j < g.NPhi; j++) sum += (g.Total[i, j] < -900 ? 0 : Math.Pow(10, g.Total[i, j] / 10)) * w * dph;
            }
            return sum / (4.0 * Math.PI);
        }

        /// <summary>AGT: media del guadagno sulla sfera (o semisfera con terreno, rapportata a 2 come in EZNEC).</summary>
        public static double Agt(PatternGrid g, Nec2Engine e)
        {
            bool gnd = e.HasGround;
            double gridStep = g.ThetaStep, sum = 0;
            // (con terreno la media sulla semisfera deve valere 2: si divide comunque per 4 pi)
            double dth = gridStep * Math.PI / 180.0, dph = g.PhiStep * Math.PI / 180.0;
            double thMax = (gnd ? 90.0 : 180.0) * Math.PI / 180.0;
            for (int i = 0; i < g.NTheta; i++)
            {
                double th = g.ThetaAt(i) * Math.PI / 180.0;
                double a0 = Math.Max(0, th - dth / 2), b0 = Math.Min(thMax, th + dth / 2);
                double w = Math.Cos(a0) - Math.Cos(b0);   // angolo solido della fascia, per radiante di phi
                for (int j = 0; j < g.NPhi; j++)
                {
                    double lin = g.Total[i, j] < -900 ? 0 : Math.Pow(10, g.Total[i, j] / 10);
                    sum += lin * w * dph;
                }
            }
            double avg = sum / (4.0 * Math.PI);
            double agt = avg > 0 ? 10 * Math.Log10(avg) : -99;
            if (e.StructureLoss > 0 && e.InputPower > e.StructureLoss)
                agt -= 10 * Math.Log10((e.InputPower - e.StructureLoss) / e.InputPower);   // il test misura la correttezza numerica, non le perdite
            return agt;
        }

        /// <summary>
        /// 0.4.20 (approvato da IC8TEM il 14/11/2026): diagramma 3D rifatto a passo piu' fine sulla soluzione gia' calcolata (per i
        /// modelli grandi il primo diagramma si fa a 5 gradi e questo, a 2 gradi, solo se la linguetta del diagramma e' aperta).
        /// Il massimo, i tagli e F/B non cambiano: sono gia' affinati sulla soluzione.
        /// </summary>
        public static void AffinaDiagramma(RunResult r, double gridStep, CancellationToken ct)
        {
            if (r == null || r.Engine == null) return;
            double best, bt, bp, ms;
            PatternGrid g = Griglia(r.Engine, gridStep, ct, out best, out bt, out bp, out ms);
            r.Grid = g;
            r.AgtDb = Agt(g, r.Engine);
            r.MsDiagramma += ms;
        }

        /// <summary>
        /// 0.4.14 (collaudo 0.4.10 del 14/11/2026: Butternut con 64 radiali, 7877 segmenti, 12,5 GB di memoria e interfaccia ferma fino a
        /// 29 s): quanti punti dello sweep calcolare insieme. Ogni punto ha la sua matrice (16 byte x N^2, piu' circa il 20 % di lavoro):
        /// si usa al massimo un quarto della memoria del computer, almeno un punto e non piu' dei processori; oltre 3000 segmenti al
        /// massimo due punti insieme (la fattorizzazione di ogni punto lavora gia' su tutti i processori).
        /// </summary>
        public static int ParallelismoPerMemoria(int nSeg)
        {
            int cpu = Processori.Calcolo;
            try
            {
                double perPunto = 16.0 * nSeg * (double)nSeg * 1.2;
                double disp = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                if (disp <= 0 || perPunto <= 0) return cpu;
                int k = (int)Math.Floor(0.25 * disp / perPunto);
                if (nSeg > 3000) k = Math.Min(k, 2);
                return Math.Max(1, Math.Min(cpu, k));
            }
            catch { return cpu; }
        }

        /// <summary>Sweep in frequenza in parallelo. Il guadagno "avanti" e' nella direzione (theta, phi) data.</summary>
        public static List<SweepPoint> Sweep(NecModel model, SegmentGeometry geo, IList<double> freqs, double thetaFwd, double phiFwd,
                                             IProgress<int> progress, CancellationToken ct)
        {
            using Prestazioni.Scopo misura = Prestazioni.Misura("Sweep in frequenza", freqs.Count + " frequenze, " + geo.N + " segmenti");
            misura.Contenitore = true;
            SweepPoint[] res = new SweepPoint[freqs.Count];
            int done = 0;
            ParallelOptions po = new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = ParallelismoPerMemoria(geo.N) };
            // calcolo completo di un punto (soluzione NEC e guadagni nelle direzioni di riferimento)
            Func<int, SweepPoint> Punto = i =>
            {
                SweepPoint sp = new SweepPoint();
                sp.F = freqs[i];
                try
                {
                    SegmentGeometry gf = geo;
                    if (model.Leeson)
                    {
                        NecModel ml = Leeson.Applica(model, freqs[i], null);
                        if (!ReferenceEquals(ml, model)) gf = ml.BuildGeometry();
                    }
                    Nec2Engine e = new Nec2Engine(gf);
                    e.Solve(freqs[i], EffectiveSources(model, gf, freqs[i]), model.EffectiveLoads(), TerreniEdifici.Alla(model.Ground, freqs[i]), model.ExtendedKernel, model.Rkh, model.EffectiveNetworks(freqs[i]));
                    sp.Z = e.Sources[0].Z;
                    sp.Swr = Swr(sp.Z, model.Z0);
                    sp.Efficiency = e.InputPower > 0 ? 100.0 * (e.InputPower - e.StructureLoss) / e.InputPower : 0;
                    sp.GainFwd = e.Gain(thetaFwd, phiFwd).GainTotal;
                    // 1.0.17 (IC8TEM 17/11/2026 18:44, correzione accettata): con il fascio sull'asse Z (theta < 1 o > 179 gradi, spazio libero)
                    // la direzione a phi + 180 coincide con il davanti e il F/B valeva sempre 0: il dietro diventa la direzione opposta (theta 180 - theta)
                    // e il F/R il peggiore dell'emisfero opposto. Negli altri casi resta come prima (stessa elevazione, azimut + 180).
                    bool asse = (thetaFwd < 1 || thetaFwd > 179) && !e.HasGround;
                    double worst = -1e9;
                    if (asse)
                    {
                        sp.FrontBack = sp.GainFwd - e.Gain(180 - thetaFwd, phiFwd).GainTotal;
                        for (int t = 92; t <= 180; t += 4)
                            for (int k = 0; k < 360; k += 45)
                                worst = Math.Max(worst, e.Gain(thetaFwd < 90 ? t : 180 - t, k).GainTotal);
                    }
                    else
                    {
                        sp.FrontBack = sp.GainFwd - e.Gain(thetaFwd, Norm360(phiFwd + 180)).GainTotal;
                        for (int k = 90; k <= 270; k += 2) worst = Math.Max(worst, e.Gain(thetaFwd, Norm360(phiFwd + k)).GainTotal);
                    }
                    sp.FrontRear = sp.GainFwd - worst;
                    sp.FrontRearMedio = sp.GainFwd - MediaPosteriore(e, phiFwd);
                    // massimo approssimato nell'intorno della direzione di riferimento
                    double gm = sp.GainFwd;
                    for (int a = -20; a <= 20; a += 4)
                        for (int b = -20; b <= 20; b += 4)
                        {
                            double th = thetaFwd + a;
                            if (th < 0 || th > 180 || (e.HasGround && th > 90)) continue;
                            gm = Math.Max(gm, e.Gain(th, Norm360(phiFwd + b)).GainTotal);
                        }
                    sp.Gmax = gm;
                    sp.Ok = true;
                }
                catch (Exception ex)
                {
                    sp.Ok = false;
                    sp.Error = ex.Message;
                }
                return sp;
            };
            Action<IList<int>> Calcola = idx =>
            {
                Parallel.ForEach(idx, po, i =>
                {
                    res[i] = Punto(i);
                    int d = Interlocked.Increment(ref done);
                    if (progress != null) progress.Report(d);
                });
            };
            int n = freqs.Count;
            if (!SweepRapido || n < 9)
            {
                List<int> tutti = new List<int>();
                for (int i = 0; i < n; i++) tutti.Add(i);
                Calcola(tutti);
                return new List<SweepPoint>(res);
            }
            // sweep rapido (0.4.2, approvato da IC8TEM il 13/11/2026): MBPE adattivo (Burke e Miller, "model-based parameter
            // estimation"). Si calcola un punto su 4; per ogni intervallo si stima il punto medio con due funzioni razionali
            // costruite su finestre diverse di punti calcolati; se le due stime di Z, guadagno, guadagno posteriore, F/R,
            // massimo ed efficienza non concordano (0,05 % su Z, 1 % sui guadagni lineari) il punto si calcola davvero e si
            // ripete. Alla fine i punti mancanti si interpolano con i 5 punti calcolati piu' vicini. Verifica (nec2c, 13/11/2026):
            // ROS entro 0,01 %, guadagno e F/B entro 0,07 dB; dove la curva cambia troppo in fretta si calcolano tutti i punti.
            HashSet<int> noti = new HashSet<int>();
            for (int i = 0; i < n; i += 4) noti.Add(i);
            noti.Add(n - 1);
            Calcola(new List<int>(noti));
            for (int giro = 0; giro < 20; giro++)
            {
                ct.ThrowIfCancellationRequested();
                List<int> ks = new List<int>(noti);
                ks.Sort();
                bool tuttiOk = true;
                foreach (int k2 in ks) if (res[k2] == null || !res[k2].Ok) tuttiOk = false;
                List<int> nuovi = new List<int>();
                if (!tuttiOk)
                {
                    for (int i = 0; i < n; i++) if (!noti.Contains(i)) nuovi.Add(i);
                }
                else
                    for (int a = 0; a + 1 < ks.Count; a++)
                    {
                        if (ks[a + 1] - ks[a] <= 1) continue;
                        int mid = (ks[a] + ks[a + 1]) / 2;
                        if (!Concordano(freqs, res, ks, a, freqs[mid])) nuovi.Add(mid);
                    }
                if (nuovi.Count == 0) break;
                Calcola(nuovi);
                foreach (int q in nuovi) noti.Add(q);
            }
            {
                List<int> ks = new List<int>(noti);
                ks.Sort();
                for (int i = 0; i < n; i++)
                {
                    if (noti.Contains(i)) continue;
                    res[i] = Interpola(freqs, res, ks, freqs[i]);
                    res[i].Swr = Swr(res[i].Z, model.Z0);
                    int d = Interlocked.Increment(ref done);
                    if (progress != null) progress.Report(d);
                }
            }
            return new List<SweepPoint>(res);
        }
    
        /// <summary>Sweep rapido con MBPE adattivo (Impostazioni > Motore). Vero di partenza: risultati entro 0,01 % sul ROS.</summary>
        public static bool SweepRapido = true;

        // grandezze del punto per l'interpolazione: Z complessa, poi guadagni come potenze lineari
        static Complex[] Grandezze(SweepPoint p)
        {
            Func<double, Complex> lin = db => new Complex(db < -900 ? 0 : Math.Pow(10, db / 10), 0);
            return new[] { p.Z, lin(p.GainFwd), lin(p.GainFwd - p.FrontBack), lin(p.GainFwd - p.FrontRear), lin(p.Gmax), new Complex(p.Efficiency, 0), lin(double.IsNaN(p.FrontRearMedio) ? -999.99 : p.GainFwd - p.FrontRearMedio) };
        }

        /// <summary>Funzione razionale N(x)/D(x) di grado nd (D con termine noto 1) per minimi quadrati, valutata in f.</summary>
        static Complex Razionale(IList<double> fs, IList<Complex> zs, double f)
        {
            int m = fs.Count, nd = (m - 1) / 2, nc = 2 * nd + 1;
            double x0 = 0, mn = double.MaxValue, mx = double.MinValue;
            foreach (double v in fs) { x0 += v; mn = Math.Min(mn, v); mx = Math.Max(mx, v); }
            x0 /= m;
            double sc = (mx - mn) / 2; if (sc <= 0) sc = 1;
            // sistema normale A^H A c = A^H z
            Complex[,] A = new Complex[m, nc];
            for (int r = 0; r < m; r++)
            {
                double x = (fs[r] - x0) / sc, p = 1;
                for (int k = 0; k <= nd; k++) { A[r, k] = p; p *= x; }
                p = x;
                for (int k = 1; k <= nd; k++) { A[r, nd + k] = -zs[r] * p; p *= x; }
            }
            Complex[,] M = new Complex[nc, nc + 1];
            for (int a = 0; a < nc; a++)
            {
                for (int b = 0; b < nc; b++) { Complex t = Complex.Zero; for (int r = 0; r < m; r++) t += Complex.Conjugate(A[r, a]) * A[r, b]; M[a, b] = t; }
                Complex u = Complex.Zero; for (int r = 0; r < m; r++) u += Complex.Conjugate(A[r, a]) * zs[r]; M[a, nc] = u;
            }
            for (int c = 0; c < nc; c++)
            {
                int pv = c;
                for (int r = c + 1; r < nc; r++) if (Complex.Abs(M[r, c]) > Complex.Abs(M[pv, c])) pv = r;
                if (Complex.Abs(M[pv, c]) < 1e-300) return zs[0];
                if (pv != c) for (int k = 0; k <= nc; k++) { Complex t = M[c, k]; M[c, k] = M[pv, k]; M[pv, k] = t; }
                for (int r = 0; r < nc; r++)
                {
                    if (r == c) continue;
                    Complex q = M[r, c] / M[c, c];
                    for (int k = c; k <= nc; k++) M[r, k] -= q * M[c, k];
                }
            }
            Complex[] cf = new Complex[nc];
            for (int r = 0; r < nc; r++) cf[r] = M[r, nc] / M[r, r];
            double xx = (f - x0) / sc;
            Complex num = Complex.Zero, den = Complex.One, pp = Complex.One;
            for (int k = 0; k <= nd; k++) { num += cf[k] * pp; pp *= xx; }
            pp = xx;
            for (int k = 1; k <= nd; k++) { den += cf[nd + k] * pp; pp *= xx; }
            return num / den;
        }

        static bool Concordano(IList<double> freqs, SweepPoint[] res, List<int> ks, int a, double fm)
        {
            const int m = 5;
            List<int> w1 = new List<int>(), w2 = new List<int>();
            for (int k = a - m + 2; k <= a + 1; k++) if (k >= 0 && k < ks.Count) w1.Add(ks[k]);
            for (int k = a; k < a + m; k++) if (k >= 0 && k < ks.Count) w2.Add(ks[k]);
            if (w1.Count < 3 || w2.Count < 3) return false;
            for (int q = 0; q < 6; q++)
            {
                List<double> f1 = new List<double>(), f2 = new List<double>();
                List<Complex> z1 = new List<Complex>(), z2 = new List<Complex>();
                foreach (int k in w1) { f1.Add(freqs[k]); z1.Add(Grandezze(res[k])[q]); }
                foreach (int k in w2) { f2.Add(freqs[k]); z2.Add(Grandezze(res[k])[q]); }
                Complex p1 = Razionale(f1, z1, fm), p2 = Razionale(f2, z2, fm);
                double tol = q == 0 ? 0.0005 * Math.Max(Complex.Abs(p1), 1) + 0.02 : (q == 5 ? 0.2 : 0.01 * Math.Max(Complex.Abs(p1), 1e-30));
                if (double.IsNaN(p1.Real) || double.IsNaN(p2.Real) || Complex.Abs(p1 - p2) > tol) return false;
            }
            return true;
        }

        static SweepPoint Interpola(IList<double> freqs, SweepPoint[] res, List<int> ks, double f)
        {
            List<int> vicini = new List<int>(ks);
            vicini.Sort((u, v) => Math.Abs(freqs[u] - f).CompareTo(Math.Abs(freqs[v] - f)));
            vicini = vicini.GetRange(0, Math.Min(5, vicini.Count));
            vicini.Sort();
            Complex[] v6 = new Complex[7];
            for (int q = 0; q < 7; q++)
            {
                List<double> fs = new List<double>(); List<Complex> zs = new List<Complex>();
                foreach (int k in vicini) { fs.Add(freqs[k]); zs.Add(Grandezze(res[k])[q]); }
                v6[q] = Razionale(fs, zs, f);
            }
            Func<Complex, double> db = c => c.Real > 1e-30 ? 10 * Math.Log10(c.Real) : -999.99;
            SweepPoint sp = new SweepPoint { F = f, Z = v6[0], Ok = true, Interpolato = true };
            sp.Swr = 0;
            sp.GainFwd = db(v6[1]);
            sp.FrontBack = sp.GainFwd - db(v6[2]);
            sp.FrontRear = sp.GainFwd - db(v6[3]);
            sp.Gmax = db(v6[4]);
            sp.Efficiency = v6[5].Real;
            sp.FrontRearMedio = v6[6].Real > 1e-30 ? sp.GainFwd - db(v6[6]) : double.NaN;
            return sp;
        }
    }
}
