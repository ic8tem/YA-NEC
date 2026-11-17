// YA-NEC - motore di calcolo NEC2 (metodo dei momenti, fili sottili) in C#.
// Traduzione fedele delle routine di nec2c (calculations.c, fields.c, matrix.c, radiation.c, network.c)
// di Neoklis Kyriazis 5B4AZ, a sua volta traduzione del NEC2 FORTRAN di G. Burke e A. Poggio (LLNL). GPL-3.0.
// Questa versione tratta: fili (kernel normale ed esteso), spazio libero, terreno perfetto,
// terreno reale con il metodo del coefficiente di riflessione o di Sommerfeld (GN 2), carichi LD 0-5, sorgenti di tensione EX 0.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace YaNec.Engine
{
    public class SourceResult
    {
        public int Segment;          // 1 = primo
        public int Tag;
        public Complex V, I, Z;
        public double Power;
    }

    public sealed partial class Nec2Engine
    {
        // costanti identiche a nec2c
        internal const double PI = 3.141592654;
        internal const double POT = 1.570796327;
        internal const double TP = 6.283185308;
        internal const double TA = 1.745329252E-02;
        internal const double TD = 57.29577951;
        internal const double ETA = 376.73;
        /// <summary>Velocita' della luce del NEC-2 originale e di nec2c (MHz x m).</summary>
        public const double CVEL_NEC2 = 299.8;
        /// <summary>Velocita' della luce esatta (MHz x m), quella di EZNEC.</summary>
        public const double CVEL_FISICA = 299.792458;
        /// <summary>
        /// 0.4.30 (IC8TEM 14/11/2026 22:13, "proviamo ancora a correggere"): EZNEC usa c = 299,792458, il NEC-2 e nec2c 299,8. La differenza
        /// (2,5e-5) spiegava lo scarto di fondo dello 0,15 % con EZNEC e gli scarti del 2-10 % delle loop magnetiche (alto Q: la reattanza
        /// della struttura si sposta rispetto a quella esatta del condensatore). Prova con nec2c sui modelli d'archivio: con le misure scalate
        /// di 299,8/299,792458 lo scarto mediano in spazio libero scende dallo 0,25 % allo 0,002 %, MAGLOOP2 dal 4,0 % allo 0,1 %.
        /// Normale: c esatta (come EZNEC). Le prove che confrontano con nec2c (autotest, collaudo, modelli pesanti e sensibili) usano 299,8.
        /// </summary>
        public static double Cvel = CVEL_FISICA;
        public static double CVEL { get { return Cvel; } }
        /// <summary>Vero durante le prove contro nec2c: le Impostazioni non cambiano Cvel.</summary>
        public static bool CvelBloccata;
        internal const int PCHCON = 100000;
        static readonly Complex CONST1 = new Complex(0.0, 4.771341189);
        static readonly Complex CONST3 = new Complex(0.0, -29.97922085);

        // ---- geometria (in metri) ----
        readonly SegmentGeometry geo;
        readonly int n;
        readonly double[] xm, ym, zm, sim, bim, cab, sab, salp;
        readonly int[] icon1, icon2, itag;

        // ---- geometria in lunghezze d'onda (frequenza corrente) ----
        readonly double[] x, y, z, si, bi;
        double wlam;

        // ---- crnt ----
        readonly double[] air, aii, bir, bii, cir, cii;
        Complex[] cur;

        // ---- segj ----
        int[] jco;
        int jsno;
        double[] ax, bx, cx;

        // ---- dataj ----
        int iexk, ind1, ind2;
        double ds, db, xj, yj, zj, cabj, sabj, salpj, rkh;
        Complex exk, eyk, ezk, exs, eys, ezs, exc, eyc, ezc;

        // ---- gnd ----
        int ksymp = 1, iperf = 0;
        bool miniNec;
        Complex zratiCampo = Complex.One;
        Complex zrati = Complex.One, frati = Complex.One;
        // 0.4.30: schermo di radiali (GN NRADL) come nec2c: lunghezza e raggio in lunghezze d'onda, t1 = j 2367,067 / N, t2 = raggio x N
        int nradl; double scrwl, scrwr, gsT2; Complex gsT1;

        // ---- tmi ----
        int tmi_ij;
        double tmi_zpk, tmi_rkb2;

        // ---- zload ----
        int nload;
        Complex[] zarray;

        // ---- risultati ----
        public double FrequencyMHz { get; private set; }
        public double Wavelength { get { return wlam; } }
        public double InputPower { get; private set; }
        public double StructureLoss { get; private set; }
        public List<SourceResult> Sources { get; private set; }
        public bool Solved { get; private set; }
        public int SegmentCount { get { return n; } }
        public SegmentGeometry Geometry { get { return geo; } }
        public string PivotWarning { get; private set; }
        // 0.4.18 (richiesta di IC8TEM del 14/11/2026 09:17, collaudo dei modelli piu' pesanti): tempi delle fasi dell'ultima Solve, in ms,
        // per capire dove va il tempo e se conviene rifare una parte del motore (tabelle di Sommerfeld, riempimento, fattorizzazione, soluzione)
        public double MsSommerfeld { get; private set; }
        public double MsRiempimento { get; internal set; }
        public double MsFattorizzazione { get; internal set; }
        public double MsSoluzione { get; private set; }

        public Nec2Engine(SegmentGeometry g)
        {
            geo = g;
            n = g.N;
            xm = new double[n]; ym = new double[n]; zm = new double[n]; sim = new double[n]; bim = new double[n];
            cab = new double[n]; sab = new double[n]; salp = new double[n];
            x = new double[n]; y = new double[n]; z = new double[n]; si = new double[n]; bi = new double[n];
            icon1 = (int[])g.Icon1.Clone(); icon2 = (int[])g.Icon2.Clone(); itag = (int[])g.Tag.Clone();
            air = new double[n]; aii = new double[n]; bir = new double[n]; bii = new double[n]; cir = new double[n]; cii = new double[n];
            int maxj = n + 4;
            jco = new int[maxj]; ax = new double[maxj]; bx = new double[maxj]; cx = new double[maxj];

            // dati dei segmenti come nella scheda GE di nec2c
            for (int i = 0; i < n; i++)
            {
                double xw1 = g.X2[i] - g.X1[i];
                double yw1 = g.Y2[i] - g.Y1[i];
                double zw1 = g.Z2[i] - g.Z1[i];
                xm[i] = (g.X1[i] + g.X2[i]) / 2.0;
                ym[i] = (g.Y1[i] + g.Y2[i]) / 2.0;
                zm[i] = (g.Z1[i] + g.Z2[i]) / 2.0;
                double xw2 = xw1 * xw1 + yw1 * yw1 + zw1 * zw1;
                double yw2 = Math.Sqrt(xw2);
                yw2 = (xw2 / yw2 + yw2) * .5;
                sim[i] = yw2;
                cab[i] = xw1 / yw2;
                sab[i] = yw1 / yw2;
                xw2 = zw1 / yw2;
                if (xw2 > 1.0) xw2 = 1.0;
                if (xw2 < -1.0) xw2 = -1.0;
                salp[i] = xw2;
                bim[i] = g.Bi[i];
            }
        }

        /// <summary>Corrente (A) al centro del segmento i (0 = primo) dopo la soluzione.</summary>
        public Complex SegmentCurrent(int i) { return cur[i] * wlam; }

        /// <summary>Calcola le correnti alla frequenza fmhz.</summary>
        public void Solve(double fmhz, IList<SourceSpec> sources, IList<LoadSpec> loads, GroundSpec ground, bool extendedKernel, double rkhIn = 1.0, IList<NetworkSpec> networks = null)
        {
            Solved = false;
            PivotWarning = null;
            MsSommerfeld = MsRiempimento = MsFattorizzazione = MsSoluzione = 0;
            FrequencyMHz = fmhz;
            double fr = fmhz / CVEL;
            wlam = CVEL / fmhz;
            for (int i = 0; i < n; i++)
            {
                x[i] = xm[i] * fr; y[i] = ym[i] * fr; z[i] = zm[i] * fr;
                si[i] = sim[i] * fr; bi[i] = bim[i] * fr;
            }
            PreparaGiunzioniNec4();   // 1.2.0: carica alle giunzioni come il NEC-4 (opzione, spenta di partenza)

            // carichi
            nload = loads == null ? 0 : loads.Count;
            zarray = new Complex[n];
            if (nload > 0) Load(loads);

            // terreno
            ksymp = 1; iperf = 0; zrati = Complex.One; frati = Complex.One; miniNec = false; nradl = 0;
            if (ground != null && ground.Kind != GroundKind.FreeSpace)
            {
                ksymp = 2;
                frati = Complex.One;
                if (ground.Kind == GroundKind.Perfect || ground.MiniNec) iperf = 1;
                if (ground.Kind == GroundKind.Perfect) { }
                else if (ground.MiniNec)
                {
                    // MiniNec ground di 4nec2: le correnti su terreno perfetto; il terreno reale serve solo al campo lontano (fine di Solve)
                    double sigm = ground.Sigma;
                    if (sigm < 0.0) sigm = -sigm / (59.96 * wlam);
                    zratiCampo = 1.0 / Complex.Sqrt(new Complex(ground.Epsr, -sigm * wlam * 59.96));
                    miniNec = true;
                }
                else
                {
                    iperf = ground.Sommerfeld ? 2 : 0;
                    double sig = ground.Sigma;
                    if (sig < 0.0) sig = -sig / (59.96 * wlam);
                    Complex epsc = new Complex(ground.Epsr, -sig * wlam * 59.96);
                    zrati = 1.0 / Complex.Sqrt(epsc);
                    if (iperf == 0 && ground.Nradl > 0 && ground.SchermoL > 0 && ground.SchermoR > 0)
                    {
                        nradl = ground.Nradl;
                        scrwl = ground.SchermoL / wlam;
                        scrwr = ground.SchermoR / wlam;
                        gsT1 = new Complex(0.0, 2367.067 / nradl);
                        gsT2 = scrwr * nradl;
                    }
                    if (iperf == 2)
                    {
                        gw_u = zrati;
                        gw_u2 = gw_u * gw_u;
                        System.Diagnostics.Stopwatch swS = System.Diagnostics.Stopwatch.StartNew();
                        PreparaSommerfeld(ground.Epsr, sig, fmhz);
                        MsSommerfeld = swS.Elapsed.TotalMilliseconds;
                        frati = (epsc - 1.0) / (epsc + 1.0);
                    }
                }
            }

            // sorgenti
            if (sources == null || sources.Count == 0) throw new NecException("Il modello non ha sorgenti (scheda EX).");
            int[] isant = new int[sources.Count];
            Complex[] vsant = new Complex[sources.Count];
            for (int k = 0; k < sources.Count; k++)
            {
                isant[k] = geo.SegNo(sources[k].Tag, sources[k].Seg);
                if (isant[k] < 1 || isant[k] > n) throw new NecException("Sorgente su un segmento inesistente: " + isant[k]);
                vsant[k] = Complex.Abs(sources[k].V) < 1e-20 ? Complex.One : sources[k].V;
            }

            // matrice di interazione (riempimento trasposto come nec2c) e fattorizzazione
            // con le simmetrie GX/GR verificate si risolvono i blocchi della prima copia (Nec2Engine.Simmetria.cs)
            iqds.Clear(); vqds.Clear();
            Action<Complex[]> risolvi;
            if (FattorizzaSimmetrica(rkhIn, extendedKernel ? 1 : 0)) risolvi = RisolviSimmetrica;
            else
            {
                Complex[] cm = new Complex[(long)n * n];
                System.Diagnostics.Stopwatch swF = System.Diagnostics.Stopwatch.StartNew();
                using (Prestazioni.Misura("Motore: riempimento della matrice", null, false)) CmSet(cm, rkhIn, extendedKernel ? 1 : 0);
                MsRiempimento = swF.Elapsed.TotalMilliseconds;
                int[] ip = new int[n];
                swF.Restart();
                using (Prestazioni.Misura("Motore: fattorizzazione LU", null, false)) Factr(n, cm, ip, n);
                MsFattorizzazione = swF.Elapsed.TotalMilliseconds;
                risolvi = b => SolveLu(n, cm, ip, b, n);
            }

            System.Diagnostics.Stopwatch swR = System.Diagnostics.Stopwatch.StartNew();
            if (networks != null && networks.Count > 0)
            {
                SolveWithNetworks(risolvi, isant, vsant, networks);
            }
            else
            {
                // termine noto: campo applicato dalle sorgenti di tensione (etmns)
                Complex[] e = new Complex[n];
                for (int k = 0; k < isant.Length; k++)
                {
                    if (sources[k].Qds) continue;
                    int iss = isant[k] - 1;
                    e[iss] = -vsant[k] / (si[iss] * wlam);
                }
                // sorgenti EX 5 dopo quelle di tensione, come etmns di nec2c
                for (int k = 0; k < isant.Length; k++)
                    if (sources[k].Qds) Qdsrc(isant[k], vsant[k], e);
                risolvi(e);
                cur = e;
                Cabc(cur);

                // parametri d'ingresso
                Sources = new List<SourceResult>();
                double pin = 0;
                for (int k = 0; k < isant.Length; k++)
                {
                    int isc1 = isant[k] - 1;
                    Complex vlt = vsant[k];
                    Complex cux = cur[isc1] * wlam;
                    if (sources[k].Qds)
                    {
                        // EX 5: corrente all'inizio del segmento, I(-s/2) = A - B sin(k s/2) + C cos(k s/2) (netwk di nec2c)
                        double xx = TP * si[isc1] * .5;
                        cux = (new Complex(air[isc1], aii[isc1]) - new Complex(bir[isc1], bii[isc1]) * Math.Sin(xx)
                               + new Complex(cir[isc1], cii[isc1]) * Math.Cos(xx)) * wlam;
                    }
                    SourceResult r = new SourceResult();
                    r.Segment = isc1 + 1;
                    r.Tag = itag[isc1];
                    r.V = vlt;
                    r.I = cux;
                    r.Z = vlt / cux;
                    r.Power = .5 * (vlt * Complex.Conjugate(cux)).Real;
                    pin += r.Power;
                    Sources.Add(r);
                }
                InputPower = pin;
            }
            MsSoluzione = swR.Elapsed.TotalMilliseconds;

            // perdite nella struttura
            double ploss = 0;
            if (nload != 0)
            {
                for (int i = 0; i < n; i++)
                {
                    Complex curi = cur[i] * wlam;
                    double cmag = Complex.Abs(curi);
                    if (Math.Abs(zarray[i].Real) >= 1.0e-20)
                        ploss += 0.5 * cmag * cmag * zarray[i].Real * si[i];
                }
            }
            StructureLoss = ploss;
            if (miniNec) { iperf = 0; zrati = zratiCampo; }   // MiniNec ground: da qui il campo lontano usa il terreno reale
            PreparaCampoLontano();
            Solved = true;
        }

        // ======================= linee di trasmissione e reti a due porte (come NETWK di nec2c) =======================
        // 1) ammettenze di cortocircuito della struttura tra le porte (1 V su una porta, le altre in corto);
        // 2) somma delle ammettenze delle reti; 3) tensioni delle porte libere con le sorgenti fisse;
        // 4) soluzione finale con tutte le tensioni delle porte. Verificato contro nec2c (TL dritte, incrociate,
        //    con ammettenze in parallelo e con lunghezza 0 = distanza tra i centri dei segmenti).
        void SolveWithNetworks(Action<Complex[]> risolvi, int[] isant, Complex[] vsant, IList<NetworkSpec> networks)
        {
            List<int> ports = new List<int>();          // numeri di segmento (1 = primo)
            Func<int, int> portOf = sg =>
            {
                int k = ports.IndexOf(sg);
                if (k < 0) { ports.Add(sg); k = ports.Count - 1; }
                return k;
            };
            foreach (int sg in isant) portOf(sg);
            int[] na = new int[networks.Count], nb = new int[networks.Count];
            for (int k = 0; k < networks.Count; k++)
            {
                NetworkSpec nw = networks[k];
                int s1 = geo.SegNo(nw.Tag1, nw.Seg1), s2 = geo.SegNo(nw.Tag2, nw.Seg2);
                if (s1 < 1 || s1 > n || s2 < 1 || s2 > n) throw new NecException("Linea o rete " + (k + 1) + " collegata a un segmento inesistente.");
                if (s1 == s2) throw new NecException("Linea o rete " + (k + 1) + ": le due estremita' sono sullo stesso segmento.");
                na[k] = portOf(s1);
                nb[k] = portOf(s2);
            }
            int np = ports.Count;

            // 1) ammettenze della struttura tra le porte
            Complex[,] ys = new Complex[np, np];
            for (int p = 0; p < np; p++)
            {
                Complex[] ep = new Complex[n];
                int sp = ports[p] - 1;
                ep[sp] = -1.0 / (si[sp] * wlam);
                risolvi(ep);
                Cabc(ep);
                for (int q = 0; q < np; q++) ys[q, p] = ep[ports[q] - 1] * wlam;
            }

            // 2) ammettenze delle reti
            Complex[,] yn = new Complex[np, np];
            double beta = TP / wlam;
            for (int k = 0; k < networks.Count; k++)
            {
                NetworkSpec nw = networks[k];
                Complex y11, y12, y22;
                if (nw.IsTl)
                {
                    double z0 = Math.Abs(nw.F1);
                    if (z0 < 1e-9) throw new NecException("Linea " + (k + 1) + ": impedenza caratteristica nulla.");
                    double len = nw.F2;
                    if (len <= 0)
                    {
                        int s1 = ports[na[k]] - 1, s2 = ports[nb[k]] - 1;
                        double dx = xm[s1] - xm[s2], dy = ym[s1] - ym[s2], dz = zm[s1] - zm[s2];
                        len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    }
                    double bl = beta * len;
                    // 0.4.8: linea lunga esattamente un multiplo di mezz'onda (hrs_441.nec di 4nec2: TL lunghe WL/2). L'ammettenza e' infinita,
                    // ma il vincolo V2 = -V1 (o V1) resta definito: come nec2c/4nec2 (nessun controllo) si sposta la lunghezza di una parte
                    // su un milione, e l'ammettenza grande (circa 10^6/Z0) fa da vincolo senza rendere la matrice singolare.
                    if (Math.Abs(Math.Sin(bl)) < 1e-6) bl += 1e-6 * Math.Max(1.0, Math.Abs(bl));
                    double sn = Math.Sin(bl), tn = Math.Tan(bl);
                    if (Math.Abs(sn) < 1e-12 || Math.Abs(tn) < 1e-12) throw new NecException("Linea " + (k + 1) + ": lunghezza multipla di mezz'onda, la rete non e' definita.");
                    y11 = new Complex(0, -1.0 / (z0 * tn));
                    y12 = new Complex(0, 1.0 / (z0 * sn));
                    if (nw.F1 < 0) y12 = -y12;      // linea incrociata
                    y22 = y11 + new Complex(nw.F5, nw.F6);
                    y11 = y11 + new Complex(nw.F3, nw.F4);
                }
                else
                {
                    y11 = new Complex(nw.F1, nw.F2);
                    y12 = new Complex(nw.F3, nw.F4);
                    y22 = new Complex(nw.F5, nw.F6);
                }
                yn[na[k], na[k]] += y11;
                yn[na[k], nb[k]] += y12;
                yn[nb[k], na[k]] += y12;
                yn[nb[k], nb[k]] += y22;
            }

            // 3) tensioni delle porte: sorgenti fisse, porte delle reti incognite (le altre restano in corto)
            Complex[] v = new Complex[np];
            bool[] fixedV = new bool[np];
            for (int k = 0; k < isant.Length; k++)
            {
                int p = ports.IndexOf(isant[k]);
                fixedV[p] = true;
                v[p] = vsant[k];
            }
            List<int> free = new List<int>();
            for (int p = 0; p < np; p++) if (!fixedV[p]) free.Add(p);
            int nf = free.Count;
            if (nf > 0)
            {
                Complex[,] a = new Complex[nf, nf + 1];
                for (int r = 0; r < nf; r++)
                {
                    int pr = free[r];
                    for (int c = 0; c < nf; c++) a[r, c] = ys[pr, free[c]] + yn[pr, free[c]];
                    Complex rhs = Complex.Zero;
                    for (int p = 0; p < np; p++) if (fixedV[p]) rhs -= (ys[pr, p] + yn[pr, p]) * v[p];
                    a[r, nf] = rhs;
                }
                for (int c = 0; c < nf; c++)
                {
                    int piv = c;
                    for (int r = c + 1; r < nf; r++) if (Complex.Abs(a[r, c]) > Complex.Abs(a[piv, c])) piv = r;
                    if (Complex.Abs(a[piv, c]) < 1e-300) throw new NecException("Rete di alimentazione singolare (porta senza collegamento).");
                    if (piv != c) for (int k = c; k <= nf; k++) { Complex t = a[c, k]; a[c, k] = a[piv, k]; a[piv, k] = t; }
                    for (int r = 0; r < nf; r++)
                    {
                        if (r == c) continue;
                        Complex f = a[r, c] / a[c, c];
                        if (f == Complex.Zero) continue;
                        for (int k = c; k <= nf; k++) a[r, k] -= f * a[c, k];
                    }
                }
                for (int r = 0; r < nf; r++) v[free[r]] = a[r, nf] / a[r, r];
            }

            // 4) soluzione con tutte le tensioni delle porte
            Complex[] e = new Complex[n];
            for (int p = 0; p < np; p++)
            {
                int sp = ports[p] - 1;
                e[sp] = -v[p] / (si[sp] * wlam);
            }
            risolvi(e);
            cur = e;
            Cabc(cur);

            Sources = new List<SourceResult>();
            double pin = 0;
            for (int k = 0; k < isant.Length; k++)
            {
                int p = ports.IndexOf(isant[k]);
                int isc1 = isant[k] - 1;
                Complex inet = Complex.Zero;
                for (int q = 0; q < np; q++) inet += yn[p, q] * v[q];
                Complex cux = cur[isc1] * wlam + inet;   // corrente fornita dalla sorgente: antenna + rete
                SourceResult r = new SourceResult();
                r.Segment = isc1 + 1;
                r.Tag = itag[isc1];
                r.V = vsant[k];
                r.I = cux;
                r.Z = vsant[k] / cux;
                r.Power = .5 * (vsant[k] * Complex.Conjugate(cux)).Real;
                pin += r.Power;
                Sources.Add(r);
            }
            InputPower = pin;
        }

        // ======================= carichi (load) =======================
        void Load(IList<LoadSpec> loads)
        {
            Complex tpcj = new Complex(0.0, 2.0 * Math.PI * Cvel * 1.0e6);   // 1,883698955e9 con c = 299,8 (nec2c)
            for (int i = 0; i < n; i++) zarray[i] = Complex.Zero;
            foreach (LoadSpec ld in loads)
            {
                if (ld.Type < 0 || ld.Type > 5) throw new NecException("Tipo di carico LD non valido: " + ld.Type);
                int ldtags = ld.Tag;
                int ldtagf = ld.From, ldtagt = ld.To;
                if (ldtagt == 0) ldtagt = ldtagf;
                int jump = ld.Type + 1, ichk = 0, l1 = 1, l2 = n;
                if (ldtags == 0 && (ldtagf != 0 || ldtagt != 0)) { l1 = ldtagf; l2 = ldtagt; }
                if (l1 < 1) l1 = 1;
                if (l2 > n) l2 = n;
                double zlr = ld.F1, zli = ld.F2, zlc = ld.F3;
                for (int i = l1 - 1; i < l2; i++)
                {
                    if (ldtags != 0)
                    {
                        if (ldtags != itag[i]) continue;
                        if (ldtagf != 0)
                        {
                            ichk++;
                            if ((ichk < ldtagf) || (ichk > ldtagt)) continue;
                        }
                        else ichk = 1;
                    }
                    else ichk = 1;

                    Complex zt = Complex.Zero;
                    switch (jump)
                    {
                        case 1:
                            zt = zlr / si[i] + tpcj * zli / (si[i] * wlam);
                            if (Math.Abs(zlc) > 1.0e-20) zt += wlam / (tpcj * si[i] * zlc);
                            break;
                        case 2:
                            zt = tpcj * si[i] * zlc / wlam;
                            if (Math.Abs(zli) > 1.0e-20) zt += si[i] * wlam / (tpcj * zli);
                            if (Math.Abs(zlr) > 1.0e-20) zt += si[i] / zlr;
                            zt = 1.0 / zt;
                            break;
                        case 3:
                            zt = zlr * wlam + tpcj * zli;
                            if (Math.Abs(zlc) > 1.0e-20) zt += 1.0 / (tpcj * si[i] * si[i] * zlc);
                            break;
                        case 4:
                            zt = tpcj * si[i] * si[i] * zlc;
                            if (Math.Abs(zli) > 1.0e-20) zt += 1.0 / (tpcj * zli);
                            if (Math.Abs(zlr) > 1.0e-20) zt += 1.0 / (zlr * wlam);
                            zt = 1.0 / zt;
                            break;
                        case 5:
                            zt = new Complex(zlr, zli) / si[i];
                            break;
                        case 6:
                            zt = Zint(zlr * wlam, bi[i]);
                            break;
                    }
                    zarray[i] += zt;
                }
                if (ichk == 0) throw new NecException("Scheda LD: nessun segmento con tag " + ldtags + ".");
            }
        }

        // impedenza interna di un filo circolare (formula originale NEC2, con i ritorni corretti)
        static Complex Zint(double sigl, double rolam)
        {
            Complex cc1 = new Complex(6.0e-7, 1.9e-6), cc2 = new Complex(-3.4e-6, 5.1e-6), cc3 = new Complex(-2.52e-5, 0.0);
            Complex cc4 = new Complex(-9.06e-5, -9.01e-5), cc5 = new Complex(0.0, -9.765e-4), cc6 = new Complex(.0110486, -0.0110485);
            Complex cc7 = new Complex(0.0, -0.3926991), cc8 = new Complex(1.6e-6, -3.2e-6), cc9 = new Complex(1.17e-5, -2.4e-6);
            Complex cc10 = new Complex(3.46e-5, 3.38e-5), cc11 = new Complex(5.0e-7, 2.452e-4), cc12 = new Complex(-1.3813e-3, 1.3811e-3);
            Complex cc13 = new Complex(-6.25001e-2, -1.0e-7), cc14 = new Complex(.7071068, 0.7071068);
            // 0.4.2 (mining hf-nec2c, approvato da IC8TEM il 13/11/2026): cc5 e CN come nel listato ZINT del NEC-2 (Part III, NTIS
            // ADA956129): nec2c scriveva -j9,765e4 e usava cc14 (7 cifre) al posto di CN. cc5 si semplifica nel rapporto, CN sposta
            // il risultato di circa 5e-10: solo fedelta' all'originale.
            Complex cn = new Complex(0.70710678, 0.70710678);
            double tpcmu = 2.368705e+3, cmotp = 60.00;
            double x = Math.Sqrt(tpcmu * sigl) * rolam;
            Complex j = Complex.ImaginaryOne;
            if (x <= 110.0)
            {
                Complex br1, br2;
                if (x <= 8.0)
                {
                    double yy = x / 8.0;
                    yy = yy * yy;
                    double s = yy * yy;
                    double ber = ((((((-9.01e-6 * s + 1.22552e-3) * s - .08349609) * s + 2.6419140) * s - 32.363456) * s + 113.77778) * s - 64.0) * s + 1.0;
                    double bei = ((((((1.1346e-4 * s - .01103667) * s + .52185615) * s - 10.567658) * s + 72.817777) * s - 113.77778) * s + 16.0) * yy;
                    br1 = new Complex(ber, bei);
                    ber = (((((((-3.94e-6 * s + 4.5957e-4) * s - .02609253) * s + .66047849) * s - 6.0681481) * s + 14.222222) * s - 4.0) * yy) * x;
                    bei = ((((((4.609e-5 * s - 3.79386e-3) * s + .14677204) * s - 2.3116751) * s + 11.377778) * s - 10.666667) * s + .5) * x;
                    br2 = new Complex(ber, bei);
                    br1 = br1 / br2;
                    return j * Math.Sqrt(cmotp / sigl) * br1 / rolam;
                }
                Func<double, Complex> th = d => (((((cc1 * d + cc2) * d + cc3) * d + cc4) * d + cc5) * d + cc6) * d + cc7;
                Func<double, Complex> ph = d => (((((cc8 * d + cc9) * d + cc10) * d + cc11) * d + cc12) * d + cc13) * d + cc14;
                Func<double, Complex> f = d => Complex.Sqrt(POT / d) * Complex.Exp(-cn * d + th(-8.0 / x));
                Func<double, Complex> gfn = d => Complex.Exp(cn * d + th(8.0 / x)) / Complex.Sqrt(TP * d);
                br2 = j * f(x) / PI;
                br1 = gfn(x) + br2;
                br2 = gfn(x) * ph(8.0 / x) - br2 * ph(-8.0 / x);
                br1 = br1 / br2;
                return j * Math.Sqrt(cmotp / sigl) * br1 / rolam;
            }
            Complex b1 = new Complex(.70710678, -.70710678);
            return j * Math.Sqrt(cmotp / sigl) * b1 / rolam;
        }

        // ======================= cabc =======================
        void Cabc(Complex[] curx)
        {
            for (int i = 0; i < n; i++)
            {
                air[i] = 0.0; aii[i] = 0.0; bir[i] = 0.0; bii[i] = 0.0; cir[i] = 0.0; cii[i] = 0.0;
            }
            for (int i = 0; i < n; i++)
            {
                double ar = curx[i].Real, ai = curx[i].Imaginary;
                Tbf(i + 1, 1);
                for (int jx = 0; jx < jsno; jx++)
                {
                    int j = jco[jx] - 1;
                    air[j] += ax[jx] * ar;
                    aii[j] += ax[jx] * ai;
                    bir[j] += bx[jx] * ar;
                    bii[j] += bx[jx] * ai;
                    cir[j] += cx[jx] * ar;
                    cii[j] += cx[jx] * ai;
                }
            }
            // contributo delle sorgenti EX 5 (cabc di nec2c)
            for (int q = 0; q < iqds.Count; q++)
            {
                Complex curd = QdsCurd(iqds[q], vqds[q]);
                for (int jx = 0; jx < jsno; jx++)
                {
                    int j = jco[jx] - 1;
                    air[j] += ax[jx] * curd.Real;
                    aii[j] += ax[jx] * curd.Imaginary;
                    bir[j] += bx[jx] * curd.Real;
                    bii[j] += bx[jx] * curd.Imaginary;
                    cir[j] += cx[jx] * curd.Real;
                    cii[j] += cx[jx] * curd.Imaginary;
                }
            }
            for (int i = 0; i < n; i++)
                curx[i] = new Complex(air[i] + cir[i], aii[i] + cii[i]);
        }

        // ======================= qdsrc: sorgente EX 5 (discontinuita' di pendenza della corrente) =======================
        // 0.4.2: porting di qdsrc() di nec2c (input.c), per gli esempi ufficiali del manuale NEC-2 Part III (proposta 1 del
        // mining, approvata da IC8TEM il 13/11/2026). Il campo della distribuzione di corrente della sorgente si toglie dal
        // termine noto; cabc la aggiunge alla corrente risolta.
        readonly List<int> iqds = new List<int>();
        readonly List<Complex> vqds = new List<Complex>();

        // ampiezza della corrente di sorgente; lascia in jco/ax/bx/cx la funzione di base del segmento con l'inizio libero
        Complex QdsCurd(int iss, Complex v)
        {
            int ix = iss - 1;
            int sav = icon1[ix];
            icon1[ix] = 0;
            Tbf(iss, 0);
            icon1[ix] = sav;
            double sh = si[ix] * .5;
            return new Complex(0, -0.01666666667) * v /
                ((Math.Log(2.0 * sh / bi[ix]) - 1.0) * (bx[jsno - 1] * Math.Cos(TP * sh) + cx[jsno - 1] * Math.Sin(TP * sh)) * wlam);
        }

        void Qdsrc(int iss, Complex v, Complex[] e)
        {
            Complex curd = QdsCurd(iss, v);
            iqds.Add(iss); vqds.Add(v);
            int nj = jsno;
            int[] jc = new int[nj]; double[] a = new double[nj], b = new double[nj], c = new double[nj];
            Array.Copy(jco, jc, nj); Array.Copy(ax, a, nj); Array.Copy(bx, b, nj); Array.Copy(cx, c, nj);
            for (int jx = 0; jx < nj; jx++)
            {
                int j = jc[jx] - 1;
                SegmentoSorgente(j + 1);
                for (int i = 0; i < n; i++)
                {
                    Efld(x[i], y[i], z[i], bi[i], i - j);
                    double cabi = cab[i], sabi = sab[i], salpi = salp[i];
                    Complex etk = exk * cabi + eyk * sabi + ezk * salpi;
                    Complex ets = exs * cabi + eys * sabi + ezs * salpi;
                    Complex etc = exc * cabi + eyc * sabi + ezc * salpi;
                    e[i] -= (etk * a[jx] + ets * b[jx] + etc * c[jx]) * curd;
                }
                // caduta sul carico del segmento (nec2c qdsrc: e[j] += zarray[j]*curd*(ax+cx)); mancava fino alla 0.4.2 e
                // spostava l'es. 2 del manuale con LD 5 (112,107 +j65,581 invece di 112,430 +j65,428)
                if (nload > 0) e[j] += zarray[j] * curd * (a[jx] + c[jx]);
            }
        }

        // ======================= gf =======================
        void Gf(double zk, out double co, out double sii)
        {
            double zdk = zk - tmi_zpk;
            double rk = Math.Sqrt(tmi_rkb2 + zdk * zdk);
            sii = Math.Sin(rk) / rk;
            if (tmi_ij != 0)
            {
                co = Math.Cos(rk) / rk;
                return;
            }
            if (rk >= .2)
            {
                co = (Math.Cos(rk) - 1.0) / rk;
                return;
            }
            double rks = rk * rk;
            co = ((-1.38888889e-3 * rks + 4.16666667e-2) * rks - .5) * rk;
        }

        static void Test(double f1r, double f2r, out double tr, double f1i, double f2i, out double ti, double dmin)
        {
            double den = Math.Abs(f2r);
            tr = Math.Abs(f2i);
            if (den < tr) den = tr;
            if (den < dmin) den = dmin;
            if (den < 1.0e-37)
            {
                tr = 0.0; ti = 0.0;
                return;
            }
            tr = Math.Abs((f1r - f2r) / den);
            ti = Math.Abs((f1i - f2i) / den);
        }

        // ======================= intx (Romberg a passo variabile) =======================
        void Intx(double el1, double el2, double b, int ij, out double sgr, out double sgi)
        {
            int ns, nt;
            int nx = 1, nma = 65536, nts = 4;
            bool flag = true;
            double z0, s, ze, fnm, ep, zend, fns, dz = 0.0, zp, dzot = 0.0, t00r, g1r, g5r = 0.0, t00i;
            double g1i, g5i = 0.0, t01r, g3r = 0.0, t01i, g3i = 0.0, t10r, t10i, te1i, te1r, t02r;
            double g2r, g4r, t02i, g2i, g4i, t11r, t11i, t20r, t20i, te2i, te2r;
            double rx = 1.0e-4;

            z0 = el1;
            ze = el2;
            if (ij == 0) ze = 0.0;
            s = ze - z0;
            fnm = nma;
            ep = s / (10.0 * fnm);
            zend = ze - ep;
            sgr = 0.0;
            sgi = 0.0;
            ns = nx;
            nt = 0;
            Gf(z0, out g1r, out g1i);

            while (true)
            {
                if (flag)
                {
                    fns = ns;
                    dz = s / fns;
                    zp = z0 + dz;
                    if (zp > ze)
                    {
                        dz = ze - z0;
                        if (Math.Abs(dz) <= ep)
                        {
                            if (ij == 0)
                            {
                                sgr = 2.0 * (sgr + Math.Log((Math.Sqrt(b * b + s * s) + s) / b));
                                sgi = 2.0 * sgi;
                            }
                            return;
                        }
                    }
                    dzot = dz * .5;
                    zp = z0 + dzot;
                    Gf(zp, out g3r, out g3i);
                    zp = z0 + dz;
                    Gf(zp, out g5r, out g5i);
                }

                t00r = (g1r + g5r) * dzot;
                t00i = (g1i + g5i) * dzot;
                t01r = (t00r + dz * g3r) * 0.5;
                t01i = (t00i + dz * g3i) * 0.5;
                t10r = (4.0 * t01r - t00r) / 3.0;
                t10i = (4.0 * t01i - t00i) / 3.0;

                Test(t01r, t10r, out te1r, t01i, t10i, out te1i, 0.0);
                if ((te1i <= rx) && (te1r <= rx))
                {
                    sgr = sgr + t10r;
                    sgi = sgi + t10i;
                    nt += 2;
                    z0 += dz;
                    if (z0 >= zend)
                    {
                        if (ij == 0)
                        {
                            sgr = 2.0 * (sgr + Math.Log((Math.Sqrt(b * b + s * s) + s) / b));
                            sgi = 2.0 * sgi;
                        }
                        return;
                    }
                    g1r = g5r;
                    g1i = g5i;
                    if (nt >= nts && ns > nx)
                    {
                        ns = ns / 2;
                        nt = 1;
                    }
                    flag = true;
                    continue;
                }

                zp = z0 + dz * 0.25;
                Gf(zp, out g2r, out g2i);
                zp = z0 + dz * 0.75;
                Gf(zp, out g4r, out g4i);
                t02r = (t01r + dzot * (g2r + g4r)) * 0.5;
                t02i = (t01i + dzot * (g2i + g4i)) * 0.5;
                t11r = (4.0 * t02r - t01r) / 3.0;
                t11i = (4.0 * t02i - t01i) / 3.0;
                t20r = (16.0 * t11r - t10r) / 15.0;
                t20i = (16.0 * t11i - t10i) / 15.0;

                Test(t11r, t20r, out te2r, t11i, t20i, out te2i, 0.0);
                if ((te2i > rx) || (te2r > rx))
                {
                    nt = 0;
                    if (ns < nma)
                    {
                        ns = ns * 2;
                        fns = ns;
                        dz = s / fns;
                        dzot = dz * 0.5;
                        g5r = g3r;
                        g5i = g3i;
                        g3r = g2r;
                        g3i = g2i;
                        flag = false;
                        continue;
                    }
                }

                sgr = sgr + t20r;
                sgi = sgi + t20i;
                nt++;
                z0 += dz;
                if (z0 >= zend)
                {
                    if (ij == 0)
                    {
                        sgr = 2.0 * (sgr + Math.Log((Math.Sqrt(b * b + s * s) + s) / b));
                        sgi = 2.0 * sgi;
                    }
                    return;
                }
                g1r = g5r;
                g1i = g5i;
                if (nt >= nts && ns > nx)
                {
                    ns = ns / 2;
                    nt = 1;
                }
                flag = true;
            }
        }
    }
}
