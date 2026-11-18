// YA-NEC 1.1.0 - Distanze di stack: accoppiamento tra le antenne e distanza migliore col NEC (bozzetto A2 approvato da IC8TEM il 17/11/2026 19:17),
// e stack HF sopra il terreno scelto sugli angoli di partenza voluti (richiesta del 17/11/2026 19:15, bozzetto approvato alle 19:17).
// VHF/UHF (spazio libero): per ogni distanza (fattore x la distanza DL6WU) si crea lo stack dal modello (StackDetect.Crea), lo si calcola, e si
// leggono il guadagno nella direzione del lobo, il lobo laterale piu' alto nel piano dello stack, il G/T con le temperature uniformi di Rumore e
// G/T e l'accoppiamento |Sij| tra le porte (una porta alimentata alla volta, le altre in corto: Y; Z = Y^-1; S = (Z - Z0)(Z + Z0)^-1, come
// la scheda CP del NEC-2 e la matrice di nec2-array di YO3IIU). La distanza proposta segue il criterio scelto.
// HF: l'antenna base va alla quota data, le copie sopra a passo d; terreno del modello (o medio); per ogni passo il diagramma in elevazione
// nell'azimut del lobo; proposta = media del guadagno (dBi) tra gli angoli voluti piu' alta; alla proposta anche sopra da sola, sotto da sola,
// tutte e due in fase (BIP) e, con due antenne, in controfase (BOP).
// Fonti: DL6WU (formula); ARRL Antenna Book 24a ed. (limiti; stack di Yagi HF); VE7BQH (G/T come criterio per gli stack EME); R. D. Straw N6BV
// (HFTA); ON4UN, Low-Band DXing; L. B. Cebik W4RNL; G. Burke e A. Poggio (manuale NEC-2, scheda CP).
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace YaNec.Engine
{
    public class StackPunto
    {
        public double K, Dv, Dh, G, Sll, SllAng, Gt, Tant, S21, Gmedia, Fr;   // Fr: F/R medio (1.3.1)
        public double RosAtt = double.NaN, PerditaRifl, Greal;   // 1.4: ROS attivo (porta peggiore), perdita per riflessione, guadagno realizzato
        public bool Escluso;                                     // 1.4: ROS attivo sopra la soglia: da non scegliere
        public double[] GAng;   // HF: guadagno agli angoli 5, 10, 15, 20
    }

    public class StackStudioRisultato
    {
        public List<StackPunto> Punti = new List<StackPunto>();
        public StackPunto Proposta, Dl6wu, GMax, SllMin, GtMax;
        public double GSingola, SllSingola, GtSingola, FrSingola, RosSingola, PerditaSingola, Dv0, Dh0, Lambda, RosMax = 1.5;
        public StackPunto RifMin;   // 1.4: distanza con il ROS attivo piu' basso
        public Complex[,] Z; public double[,] SdB;   // alla distanza proposta (VHF)
        public string Criterio = "";
        // HF
        public double[] ElAng, ElBip, ElSopra, ElSotto, ElBop;
        public double QuotaBase;
        public List<string> Note = new List<string>();
    }

    public static class StackStudio
    {
        static double Lin(double db) { return db < -900 ? 0 : Math.Pow(10, db / 10); }

        /// <summary>Matrice delle impedenze tra le porte (una sorgente per antenna): Y colonna per colonna, Z = Y^-1.</summary>
        public static Complex[,] MatriceZ(NecModel m, SegmentGeometry g, double f)
        {
            int n = m.Sources.Count;
            Complex[,] y = new Complex[n, n];
            for (int j = 0; j < n; j++)
            {
                List<SourceSpec> src = new List<SourceSpec>();
                for (int k = 0; k < n; k++) { SourceSpec s = m.Sources[k].Clone(); s.V = k == j ? Complex.One : new Complex(1e-15, 0); src.Add(s); }
                Nec2Engine e = new Nec2Engine(g);
                e.Solve(f, src, m.EffectiveLoads(), m.Ground, m.ExtendedKernel, m.Rkh, m.EffectiveNetworks(f));
                for (int k = 0; k < n; k++) y[k, j] = e.Sources[k].I;
            }
            return Inversa(y);
        }

        public static Complex[,] Inversa(Complex[,] a0)
        {
            int n = a0.GetLength(0);
            Complex[,] a = new Complex[n, 2 * n];
            for (int r = 0; r < n; r++) { for (int c = 0; c < n; c++) a[r, c] = a0[r, c]; a[r, n + r] = Complex.One; }
            for (int c = 0; c < n; c++)
            {
                int p = c;
                for (int r = c + 1; r < n; r++) if (Complex.Abs(a[r, c]) > Complex.Abs(a[p, c])) p = r;
                if (p != c) for (int k = 0; k < 2 * n; k++) { Complex t = a[c, k]; a[c, k] = a[p, k]; a[p, k] = t; }
                Complex d = a[c, c];
                if (Complex.Abs(d) < 1e-300) throw new NecException("Matrice delle porte singolare.");
                for (int k = 0; k < 2 * n; k++) a[c, k] /= d;
                for (int r = 0; r < n; r++)
                {
                    if (r == c) continue;
                    Complex f = a[r, c];
                    if (f == Complex.Zero) continue;
                    for (int k = 0; k < 2 * n; k++) a[r, k] -= f * a[c, k];
                }
            }
            Complex[,] o = new Complex[n, n];
            for (int r = 0; r < n; r++) for (int c = 0; c < n; c++) o[r, c] = a[r, n + c];
            return o;
        }

        /// <summary>1.4 (richiesta di IC8TEM del 18/11/2026 14:30): ROS attivo della porta peggiore con tutte le porte alimentate uguali
        /// e in fase: Γ_att,i = somma della riga i della matrice S (S = (Z - Z0)(Z + Z0)^-1), ROS = (1 + |Γ|)/(1 - |Γ|).</summary>
        public static double RosAttivo(Complex[,] z, double z0, out double gammaMax)
        {
            int n = z.GetLength(0);
            Complex[,] zm = new Complex[n, n], zp = new Complex[n, n];
            for (int r = 0; r < n; r++) for (int c = 0; c < n; c++) { zm[r, c] = z[r, c] - (r == c ? z0 : 0); zp[r, c] = z[r, c] + (r == c ? z0 : 0); }
            Complex[,] ip = Inversa(zp);
            gammaMax = 0;
            for (int r = 0; r < n; r++)
            {
                Complex g = Complex.Zero;
                for (int c = 0; c < n; c++) for (int k = 0; k < n; k++) g += zm[r, k] * ip[k, c];
                gammaMax = Math.Max(gammaMax, g.Magnitude);
            }
            double gm = Math.Min(gammaMax, 0.999999);
            return (1 + gm) / (1 - gm);
        }

        static double PerditaDb(double gamma) { return -10 * Math.Log10(Math.Max(1e-12, 1 - Math.Min(gamma * gamma, 0.999999))); }

        /// <summary>|S| in dB dalla matrice Z con l'impedenza di riferimento z0.</summary>
        public static double[,] SdB(Complex[,] z, double z0)
        {
            int n = z.GetLength(0);
            Complex[,] zm = new Complex[n, n], zp = new Complex[n, n];
            for (int r = 0; r < n; r++) for (int c = 0; c < n; c++) { zm[r, c] = z[r, c] - (r == c ? z0 : 0); zp[r, c] = z[r, c] + (r == c ? z0 : 0); }
            Complex[,] ip = Inversa(zp);
            double[,] s = new double[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    Complex v = Complex.Zero;
                    for (int k = 0; k < n; k++) v += zm[r, k] * ip[k, c];
                    s[r, c] = 20 * Math.Log10(Math.Max(1e-12, v.Magnitude));
                }
            return s;
        }

        /// <summary>Lobo laterale piu' alto (dB rispetto al massimo) nel piano che contiene il lobo e l'asse dello stack; fuori dai primi
        /// nulli e lontano oltre 100 gradi dal lobo (il posteriore e' il F/B, non un laterale).</summary>
        public static void LoboLaterale(Nec2Engine e, double thF, double phF, bool verticale, out double sll, out double ang)
        {
            double gm = e.Gain(thF, phF).GainTotal;
            int n = 721;
            double[] g = new double[n], a = new double[n];
            double R = Math.PI / 180, tf = thF * R, pf = phF * R;
            double fx = Math.Sin(tf) * Math.Cos(pf), fy = Math.Sin(tf) * Math.Sin(pf), fz = Math.Cos(tf);
            // asse del piano: lo zenit (stack sovrapposto) o l'orizzontale perpendicolare al lobo (affiancato)
            double qx = verticale ? -fz * fx : -fy, qy = verticale ? -fz * fy : fx, qz = verticale ? 1 - fz * fz : 0;
            double ql = Math.Sqrt(qx * qx + qy * qy + qz * qz);
            if (ql < 1e-9) { qx = 1; qy = 0; qz = 0; ql = 1; }
            qx /= ql; qy /= ql; qz /= ql;
            for (int i = 0; i < n; i++)
            {
                double t = (-180 + 0.5 * i) * R;
                double x = Math.Cos(t) * fx + Math.Sin(t) * qx, y = Math.Cos(t) * fy + Math.Sin(t) * qy, z = Math.Cos(t) * fz + Math.Sin(t) * qz;
                double th = Math.Acos(Math.Max(-1, Math.Min(1, z))) / R, ph = Math.Atan2(y, x) / R;
                if (ph < 0) ph += 360;
                g[i] = e.Gain(th, ph).GainTotal; a[i] = -180 + 0.5 * i;
            }
            // il campionamento a 0,5 gradi puo' mettere il picco vero uno o due punti accanto alla direzione data: si sale prima fino al
            // massimo locale, poi si scende dalle due parti (prima il picco accanto era preso per un lobo laterale a 0 dB)
            int i0 = 360;
            while (i0 < n - 1 && g[i0 + 1] > g[i0]) i0++;
            while (i0 > 0 && g[i0 - 1] > g[i0]) i0--;
            gm = Math.Max(gm, g[i0]);
            int r = i0, l = i0;
            while (r < n - 1 && g[r + 1] <= g[r]) r++;
            while (l > 0 && g[l - 1] <= g[l]) l--;
            sll = -99; ang = double.NaN;
            for (int i = 1; i < n - 1; i++)
            {
                if (i >= l && i <= r) continue;
                if (Math.Abs(a[i]) > 100) continue;
                if (g[i] >= g[i - 1] && g[i] >= g[i + 1] && g[i] - gm > sll) { sll = g[i] - gm; ang = a[i]; }
            }
        }

        /// <summary>Studio VHF/UHF in spazio libero. criterio: 0 guadagno massimo, 1 lobi piu' bassi, 2 G/T migliore, 3 guadagno con lobi sotto la soglia.</summary>
        public static StackStudioRisultato Vhf(NecModel base0, double f, int righe, int colonne, double kMin, double kMax, double z0,
            int criterio, double sogliaSll, double tsky, double tground, double elev, int passiGriglia, CancellationToken ct, double passo = 0.05, double rosMax = 1.5)
        {
            StackStudioRisultato res = new StackStudioRisultato { Lambda = 299.792458 / f };
            NecModel mf = base0.Clone();
            mf.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
            mf.GroundFlag = 0;
            RunResult r1 = Analysis.Run(mf, mf.BuildGeometry(), f, 2.0, ct);
            res.Dv0 = StackDetect.Dl6wu(res.Lambda, r1.HpbwEl);
            res.Dh0 = StackDetect.Dl6wu(res.Lambda, r1.HpbwAz);
            res.GSingola = r1.Engine.Gain(r1.ThetaMax, r1.PhiMax).GainTotal;
            double sl, sa;
            LoboLaterale(r1.Engine, r1.ThetaMax, r1.PhiMax, righe > 1, out sl, out sa);
            res.SllSingola = sl;
            res.FrSingola = r1.FrontRearMedio;
            res.RosMax = rosMax > 1 ? rosMax : 1.5;
            {
                double g1 = ((r1.Zin - z0) / (r1.Zin + z0)).Magnitude;
                res.RosSingola = (1 + Math.Min(g1, 0.999999)) / (1 - Math.Min(g1, 0.999999));
                res.PerditaSingola = PerditaDb(g1);
            }
            res.GtSingola = NoiseGt.Gt(Griglia(r1, passiGriglia), r1.ThetaMax, r1.PhiMax, r1.GmaxDbi, elev, tsky, tground, 0, 0, true).GtAnt;
            // 1.3.1 (bozzetto approvato da IC8TEM il 18/11/2026 13:04): passo scelto, di partenza 0,05 (prima fisso 0,1)
            if (!(passo >= 0.01)) passo = 0.05;
            int nk = Math.Max(2, (int)Math.Round((kMax - kMin) / passo)) + 1;
            for (int i = 0; i < nk; i++)
            {
                ct.ThrowIfCancellationRequested();
                double k = Math.Round(kMin + (kMax - kMin) * i / (nk - 1), 3);
                StackPunto p = new StackPunto { K = k, Dv = k * res.Dv0, Dh = k * res.Dh0 };
                NecModel ms = StackDetect.Crea(mf, righe, colonne, p.Dv, p.Dh, r1.PhiMax, null);
                SegmentGeometry gs = ms.BuildGeometry();
                RunResult rs = Analysis.Run(ms, gs, f, 2.0, ct);
                p.G = rs.Engine.Gain(r1.ThetaMax, r1.PhiMax).GainTotal;
                LoboLaterale(rs.Engine, r1.ThetaMax, r1.PhiMax, righe > 1, out p.Sll, out p.SllAng);
                p.Fr = rs.FrontRearMedio;
                GtResult gt = NoiseGt.Gt(Griglia(rs, passiGriglia), r1.ThetaMax, r1.PhiMax, rs.GmaxDbi, elev, tsky, tground, 0, 0, true);
                p.Gt = gt.GtAnt; p.Tant = gt.Tant;
                Complex[,] z = MatriceZ(ms, gs, f);
                double[,] s = SdB(z, z0);
                double smax = -999;
                for (int a = 0; a < s.GetLength(0); a++) for (int b = 0; b < s.GetLength(0); b++) if (a != b) smax = Math.Max(smax, s[a, b]);
                p.S21 = smax;
                double gam;
                p.RosAtt = RosAttivo(z, z0, out gam);
                p.PerditaRifl = PerditaDb(gam);
                p.Greal = p.G - p.PerditaRifl;
                p.Escluso = p.RosAtt > res.RosMax;
                res.Punti.Add(p);
                if (Math.Abs(k - 1) < 1e-6) res.Dl6wu = p;
            }
            // 1.4 (IC8TEM 18/11/2026 14:47): le distanze con ROS attivo sopra la soglia non entrano nella scelta (se lo sono tutte, si usano tutte)
            List<StackPunto> scegli = res.Punti.FindAll(x => !x.Escluso);
            if (scegli.Count == 0) scegli = res.Punti;
            res.RifMin = res.Punti[0];
            foreach (StackPunto p in res.Punti) if (p.RosAtt < res.RifMin.RosAtt) res.RifMin = p;
            res.GMax = scegli[0]; res.SllMin = scegli[0]; res.GtMax = scegli[0];
            foreach (StackPunto p in scegli)
            {
                if (p.G > res.GMax.G) res.GMax = p;
                if (p.Sll < res.SllMin.Sll) res.SllMin = p;
                if (p.Gt > res.GtMax.Gt) res.GtMax = p;
            }
            if (criterio == 0) { res.Proposta = res.GMax; res.Criterio = "guadagno massimo"; }
            else if (criterio == 1) { res.Proposta = res.SllMin; res.Criterio = "lobi laterali piu' bassi"; }
            else if (criterio == 2) { res.Proposta = res.GtMax; res.Criterio = "G/T migliore"; }
            else
            {
                StackPunto best = null;
                foreach (StackPunto p in scegli) if (p.Sll <= sogliaSll && (best == null || p.G > best.G)) best = p;
                res.Proposta = best ?? res.SllMin;
                res.Criterio = best != null ? "guadagno con lobi sotto " + sogliaSll.ToString("0", System.Globalization.CultureInfo.CurrentCulture) + " dB" : "nessuna distanza con i lobi sotto la soglia: lobi piu' bassi";
            }
            // matrice completa alla distanza proposta
            NecModel mp = StackDetect.Crea(mf, righe, colonne, res.Proposta.Dv, res.Proposta.Dh, r1.PhiMax, null);
            res.Z = MatriceZ(mp, mp.BuildGeometry(), f);
            res.SdB = SdB(res.Z, z0);
            return res;
        }

        static PatternGrid Griglia(RunResult r, int step)
        {
            if (step == (int)Math.Round(r.Grid.ThetaStep)) return r.Grid;
            PatternGrid pg = new PatternGrid { ThetaStep = step, PhiStep = step, NTheta = 180 / step + 1, NPhi = 360 / step };
            pg.Total = new double[pg.NTheta, pg.NPhi];
            System.Threading.Tasks.Parallel.For(0, pg.NTheta, i => { for (int j = 0; j < pg.NPhi; j++) pg.Total[i, j] = r.Engine.Gain(pg.ThetaAt(i), pg.PhiAt(j)).GainTotal; });
            return pg;
        }

        /// <summary>Antenna base portata alla quota h (punto piu' basso) e terreno: quello del modello, o medio se il modello e' in spazio libero.</summary>
        static NecModel BaseHf(NecModel m0, double h, List<string> note)
        {
            NecModel m = m0.Clone();
            double zmin = TrasformaGeometria.ZMin(m.BuildGeometry());
            m = TrasformaGeometria.Applica(m, TrasformaGeometria.Sposta(0, 0, h - zmin), null);
            if (m.Ground == null || m.Ground.Kind == GroundKind.FreeSpace)
            {
                m.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005, Sommerfeld = true, Name = "medio" };
                note.Add("Il modello e' in spazio libero: per lo stack HF si usa il suolo medio (13 / 0,005 S/m, Sommerfeld).");
            }
            m.GroundFlag = 1;
            return m;
        }

        /// <summary>Diagramma in elevazione (0..90 gradi a passo 1) nell'azimut phi.</summary>
        static double[] Elevazione(Nec2Engine e, double phi)
        {
            double[] g = new double[91];
            for (int el = 0; el <= 90; el++) g[el] = e.Gain(90 - el, phi).GainTotal;
            return g;
        }

        /// <summary>Studio HF sopra il terreno: n antenne una sopra l'altra, la piu' bassa alla quota h; passi da dMin a dMax (lambda);
        /// proposta = media del guadagno (dBi) tra angMin e angMax piu' alta.</summary>
        public static StackStudioRisultato Hf(NecModel base0, double f, int n, double h, double dMin, double dMax, double angMin, double angMax, CancellationToken ct)
        {
            StackStudioRisultato res = new StackStudioRisultato { Lambda = 299.792458 / f, QuotaBase = h };
            NecModel mb = BaseHf(base0, h, res.Note);
            RunResult r1 = Analysis.Run(mb, mb.BuildGeometry(), f, 2.0, ct);
            double phi = r1.PhiMax;
            double[] e1 = Elevazione(r1.Engine, phi);
            res.GSingola = Max(e1);
            int nd = Math.Max(2, (int)Math.Round((dMax - dMin) / 0.125)) + 1;
            res.ElAng = new double[91];
            for (int i = 0; i <= 90; i++) res.ElAng[i] = i;
            for (int i = 0; i < nd; i++)
            {
                ct.ThrowIfCancellationRequested();
                double d = Math.Round(dMin + (dMax - dMin) * i / (nd - 1), 3);
                NecModel ms = StackDetect.Crea(mb, n, 1, d * res.Lambda, 0, phi, null);
                RunResult rs = Analysis.Run(ms, ms.BuildGeometry(), f, 2.0, ct);
                double[] el = Elevazione(rs.Engine, phi);
                StackPunto p = new StackPunto { K = d, Dv = d * res.Lambda };
                int imax = 0; for (int a = 1; a <= 90; a++) if (el[a] > el[imax]) imax = a;
                p.G = el[imax]; p.SllAng = imax;
                p.GAng = new[] { el[5], el[10], el[15], el[20] };
                double s = 0; int c = 0;
                for (int a = (int)Math.Ceiling(angMin); a <= (int)Math.Floor(angMax); a++) { s += el[a]; c++; }
                p.Gmedia = c > 0 ? s / c : p.G;
                res.Punti.Add(p);
            }
            res.Proposta = res.Punti[0];
            foreach (StackPunto p in res.Punti) if (p.Gmedia > res.Proposta.Gmedia) res.Proposta = p;
            res.Criterio = "media migliore tra " + angMin.ToString("0", System.Globalization.CultureInfo.CurrentCulture) + " e " + angMax.ToString("0", System.Globalization.CultureInfo.CurrentCulture) + " gradi";
            // alla distanza proposta: tutte in fase (BIP), sopra da sola, sotto da sola, e con due antenne in controfase (BOP)
            NecModel mp = StackDetect.Crea(mb, n, 1, res.Proposta.Dv, 0, phi, null);
            SegmentGeometry gp = mp.BuildGeometry();
            res.ElBip = Elevazione(Analysis.Run(mp, gp, f, 2.0, ct).Engine, phi);
            int ns = mp.Sources.Count, perAnt = Math.Max(1, ns / n);
            Func<Func<int, Complex>, double[]> conV = v =>
            {
                NecModel mx = mp.Clone();
                for (int k = 0; k < mx.Sources.Count; k++) mx.Sources[k].V = v(k / perAnt);
                return Elevazione(Analysis.Run(mx, mx.BuildGeometry(), f, 2.0, ct).Engine, phi);
            };
            res.ElSotto = conV(a => a == 0 ? Complex.One : new Complex(1e-15, 0));
            res.ElSopra = conV(a => a == n - 1 ? Complex.One : new Complex(1e-15, 0));
            if (n == 2) res.ElBop = conV(a => a == 0 ? Complex.One : -Complex.One);
            return res;
        }

        static double Max(double[] a) { double m = double.MinValue; foreach (double v in a) m = Math.Max(m, v); return m; }
    }
}
