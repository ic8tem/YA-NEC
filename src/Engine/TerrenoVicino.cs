// YA-NEC 0.4.18 - controlli dei limiti del NEC-2 per i fili vicini al terreno e per le giunzioni affollate (avvisi del calcolo).
// Nati dalla Butternut con 64 radiali di IC8TEM (registro della 0.4.10: Z = -0,6 +j20 ohm, guadagno -999; nec2c da' lo stesso
// risultato): radiali a 3 cm con il terreno a coefficiente di riflessione (GN 0) e 66 fili in un solo punto. E dal BOG (filo a terra).
// Limiti (fonti in CREDITI.txt):
//  - GN 0, coefficiente di riflessione (RCA): fili orizzontali non sotto lambda/10 (I. Pellejero, "Notes on antenna simulation
//    using NEC-2"); L. B. Cebik ("S-N, RCA, and MININEC Grounds") consiglia di non usarlo sotto 0,2 lambda;
//  - GN 2, Sommerfeld-Norton: fili orizzontali non sotto lambda/1000 (lambda/200 secondo altre fonti, Pellejero);
//  - qualunque terreno: quota di almeno qualche raggio del filo (manuale NEC-2 di Burke e Poggio, riportato da Cebik e da R. Fry);
//  - giunzioni: oltre circa 30 fili in un solo punto il NEC-2 perde precisione (Pellejero).
//  - il NEC-2 non tratta fili interrati ne' isolanti: per i radiali sepolti e per il BOG appoggiato al suolo serve il NEC-4.
//  - 0.4.20 (IC8TEM, 14/11/2026 13:58, dopo la schermata di EZNEC su butternut_hf2v_A): segmenti piu' corti di lambda/1000. La guida
//    del NEC-2 (G. J. Burke, A. J. Poggio, NEC-2 Part III User's Guide, sez. II "Structure Modeling Guidelines"): "Extremely short
//    segments, less than about 10^-3 lambda, should also be avoided". YA-NEC e nec2c li calcolano lo stesso (con meno precisione);
//    EZNEC li segnala nella "Segmentation Check" ("segment length too short"). Correzione dopo la prova 6 (14/11/2026 14:48): e' solo
//    un avviso, EZNEC calcola lo stesso (BetaMatch, Elevrad2); le Butternut erano ferme per il Geometry Check, non per i segmenti corti.
//  - 0.4.20 (prove 5c e 6 con EZNEC, 14/11/2026): le sei regole del "Geometry Check" di EZNEC (CalcolaContatti). Verificate sui 601
//    modelli confrontabili della prova 6: i 26 che EZNEC rifiuta per geometria hanno tutti almeno un avviso, i 575 che calcola nessuno.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace YaNec.Engine
{
    public static class TerrenoVicino
    {
        static readonly CultureInfo CC = CultureInfo.CurrentCulture;
        const double C0 = 299.792458;

        /// <summary>Quota minima (m) dei fili orizzontali consigliata per il terreno indicato alla frequenza f; 0 se non c'e' limite.</summary>
        public static double QuotaMinima(GroundSpec g, double fmhz)
        {
            if (g == null || g.Kind != GroundKind.Real || g.MiniNec || fmhz <= 0) return 0;
            double lam = C0 / fmhz;
            return g.Sommerfeld ? lam / 1000 : lam / 10;
        }

        // 0.4.20 (schermate di EZNEC di IC8TEM del 14/11/2026 14:01-14:02: hex40 e butternut-new2 hanno nella Segmentation Check solo
        // avvisi, ma EZNEC si ferma con "Geometry Error"): fili che si toccano fuori dagli estremi dei segmenti. Nel NEC-2 due fili sono
        // collegati solo se gli estremi dei segmenti coincidono (NEC-2 Part III sez. II: "Segments will be treated as connected if the
        // separation of their ends is less than about 10^-3 times the length of the shortest segment"); un estremo appoggiato a meta' di
        // un segmento, o due fili che si incrociano, NON sono collegati: il NEC-2 (e YA-NEC) calcolano una struttura diversa da quella
        // disegnata, EZNEC la rifiuta. Controllo fatto una volta per geometria (fino a 4000 segmenti) e tenuto in memoria.
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SegmentGeometry, List<string>> cacheGeo =
            new System.Runtime.CompilerServices.ConditionalWeakTable<SegmentGeometry, List<string>>();

        static double DistSeg(SegmentGeometry g, int i, int j, out double s, out double t)
        {
            double p1x = g.X1[i], p1y = g.Y1[i], p1z = g.Z1[i], d1x = g.X2[i] - p1x, d1y = g.Y2[i] - p1y, d1z = g.Z2[i] - p1z;
            double p2x = g.X1[j], p2y = g.Y1[j], p2z = g.Z1[j], d2x = g.X2[j] - p2x, d2y = g.Y2[j] - p2y, d2z = g.Z2[j] - p2z;
            double rx = p1x - p2x, ry = p1y - p2y, rz = p1z - p2z;
            double a = d1x * d1x + d1y * d1y + d1z * d1z, e = d2x * d2x + d2y * d2y + d2z * d2z;
            double f = d2x * rx + d2y * ry + d2z * rz, c = d1x * rx + d1y * ry + d1z * rz, b = d1x * d2x + d1y * d2y + d1z * d2z;
            double den = a * e - b * b;
            s = den > 1e-18 ? Math.Max(0, Math.Min(1, (b * f - c * e) / den)) : 0;
            t = e > 0 ? Math.Max(0, Math.Min(1, (b * s + f) / e)) : 0;
            s = a > 0 ? Math.Max(0, Math.Min(1, (b * t - c) / a)) : 0;
            double dx = p1x + d1x * s - (p2x + d2x * t), dy = p1y + d1y * s - (p2y + d2y * t), dz = p1z + d1z * s - (p2z + d2z * t);
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Fili che si toccano o si incrociano fuori dagli estremi dei segmenti (per il NEC-2 non collegati; EZNEC: Geometry Error).</summary>
        public static List<string> ContattiFuoriGiunzione(SegmentGeometry geo)
        {
            if (geo == null) return new List<string>();
            return cacheGeo.GetValue(geo, CalcolaContatti);   // una volta per geometria, anche con piu' thread (sweep in parallelo)
        }

        // 0.4.20 (prova 5c/6 con EZNEC del 14/11/2026, testo della finestra "Geometry Check" letto dallo script su 735 modelli):
        // le sei regole che fermano il calcolo di EZNEC, con le sue parole tra parentesi, contate e con un esempio ciascuna.
        static List<string> CalcolaContatti(SegmentGeometry geo)
        {
            List<string> av = new List<string>();
            if (geo.N > 4000) return av;
            int n = geo.N;
            // conteggi per coppia di fili (come la finestra Geometry Check di EZNEC), non per coppia di segmenti
            HashSet<long> hA = new HashSet<long>(), hI = new HashSet<long>(), hV = new HashSet<long>(), hP = new HashSet<long>(), hS = new HashSet<long>(), hC = new HashSet<long>();
            Func<int, int, long> cp = (x, y) => (long)geo.Tag[x] * 1000003L + geo.Tag[y];
            string esA = null, esI = null, esV = null, esP = null, esS = null, esC = null;
            double[] lo = new double[n * 3], hi = new double[n * 3];
            bool[] estremo = new bool[n];
            for (int i = 0; i < n; i++)
            {
                double r = geo.Bi[i];
                lo[3 * i] = Math.Min(geo.X1[i], geo.X2[i]) - r; hi[3 * i] = Math.Max(geo.X1[i], geo.X2[i]) + r;
                lo[3 * i + 1] = Math.Min(geo.Y1[i], geo.Y2[i]) - r; hi[3 * i + 1] = Math.Max(geo.Y1[i], geo.Y2[i]) + r;
                lo[3 * i + 2] = Math.Min(geo.Z1[i], geo.Z2[i]) - r; hi[3 * i + 2] = Math.Max(geo.Z1[i], geo.Z2[i]) + r;
                estremo[i] = i == 0 || geo.Tag[i - 1] != geo.Tag[i] || i == n - 1 || geo.Tag[i + 1] != geo.Tag[i] || geo.Tag[i] == 0;
            }
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    if (geo.Tag[i] == geo.Tag[j] && geo.Tag[i] != 0) continue;
                    if (lo[3 * i] > hi[3 * j] || lo[3 * j] > hi[3 * i] || lo[3 * i + 1] > hi[3 * j + 1] || lo[3 * j + 1] > hi[3 * i + 1] || lo[3 * i + 2] > hi[3 * j + 2] || lo[3 * j + 2] > hi[3 * i + 2]) continue;
                    // regola 1: il centro del segmento finale di un filo cade dentro un altro filo ("Center of wire X end segment within
                    // wire Y"): succede dove un filo sottile si attacca a un tubo grosso con un segmento piu' corto del raggio del tubo
                    for (int k = 0; k < 2; k++)
                    {
                        int a = k == 0 ? i : j, b = k == 0 ? j : i;
                        if (!estremo[a]) continue;
                        double cx = geo.CenterX(a), cy = geo.CenterY(a), cz = geo.CenterZ(a);
                        if (DistPunto(geo, b, cx, cy, cz) >= geo.Bi[b]) continue;
                        // collaudo 0.4.20 (QFH del calcolatore: EZNEC la calcola, YA-NEC segnalava 80 casi): se il centro sta oltre l'estremo
                        // del segmento b (un filo che continua un altro, come le curve fatte a fili corti o i tubi in fila) EZNEC segnala solo
                        // quando i due fili formano un angolo di almeno 45 gradi (il filo sottile che entra di fianco nel tubo della Butternut);
                        // verificato di nuovo sui 601 modelli della prova 6: 26 su 26 riconosciuti, nessun falso allarme
                        double bx = geo.X2[b] - geo.X1[b], by = geo.Y2[b] - geo.Y1[b], bz = geo.Z2[b] - geo.Z1[b], l2 = bx * bx + by * by + bz * bz;
                        double tb = l2 > 0 ? ((cx - geo.X1[b]) * bx + (cy - geo.Y1[b]) * by + (cz - geo.Z1[b]) * bz) / l2 : 0;
                        if (!(tb > 1e-9 && tb < 1 - 1e-9))
                        {
                            double ax = geo.X2[a] - geo.X1[a], ay = geo.Y2[a] - geo.Y1[a], az = geo.Z2[a] - geo.Z1[a];
                            double cab = Math.Abs(ax * bx + ay * by + az * bz) / Math.Max(1e-30, Math.Sqrt((ax * ax + ay * ay + az * az) * l2));
                            if (cab > Math.Cos(Math.PI / 4)) continue;
                        }
                        hC.Add(cp(a, b)); if (esC == null) esC = string.Format(CC, "filo {0} nel filo {1}", geo.Tag[a], geo.Tag[b]);
                    }
                    double li = geo.Length(i), lj = geo.Length(j), tol = 1e-3 * Math.Max(1e-9, Math.Min(li, lj));
                    // estremi in comune: giunzione regolare
                    if (Vic(geo.X1[i], geo.Y1[i], geo.Z1[i], geo.X1[j], geo.Y1[j], geo.Z1[j], tol) || Vic(geo.X1[i], geo.Y1[i], geo.Z1[i], geo.X2[j], geo.Y2[j], geo.Z2[j], tol) ||
                        Vic(geo.X2[i], geo.Y2[i], geo.Z2[i], geo.X1[j], geo.Y1[j], geo.Z1[j], tol) || Vic(geo.X2[i], geo.Y2[i], geo.Z2[i], geo.X2[j], geo.Y2[j], geo.Z2[j], tol)) continue;
                    double s, t;
                    double d = DistSeg(geo, i, j, out s, out t);
                    double ux = geo.X2[i] - geo.X1[i], uy = geo.Y2[i] - geo.Y1[i], uz = geo.Z2[i] - geo.Z1[i];
                    double vx = geo.X2[j] - geo.X1[j], vy = geo.Y2[j] - geo.Y1[j], vz = geo.Z2[j] - geo.Z1[j];
                    double cosang = Math.Abs(ux * vx + uy * vy + uz * vz) / Math.Max(1e-30, li * lj);
                    // 0.4.22 (prova 8 con EZNEC, 14/11/2026 20:01: Hex beam JN1GLB, linea di alimentazione di due fili da 1 mm a 1 mm di
                    // distanza): per i fili paralleli EZNEC conta come contatto anche la distanza uguale alla somma dei raggi (i numeri del .ez
                    // sono a 32 bit); per gli altri casi resta il contatto stretto. Verificato sui 601 modelli della prova 6: 26 su 26, nessun nuovo avviso.
                    if (d >= (geo.Bi[i] + geo.Bi[j]) * (cosang > 0.9999 ? 1 + 1e-6 : 1)) continue;
                    string coppia = string.Format(CC, "fili {0} e {1}", geo.Tag[i], geo.Tag[j]);
                    // regole 5 e 6: fili paralleli che si toccano ("parallel and contacting") o uno sull'altro ("overlap")
                    bool sEstremo = s < 1e-6 || s > 1 - 1e-6, tEstremo = t < 1e-6 || t > 1 - 1e-6;
                    if (cosang > 0.9999)
                    {
                        // tubi in fila o sfalsati senza tratto in comune lungo l'asse (estremo contro estremo): EZNEC non li segnala.
                        // Affiancati (con un tratto in comune) si': si proietta il segmento j sull'asse del segmento i.
                        double q1 = ((geo.X1[j] - geo.X1[i]) * ux + (geo.Y1[j] - geo.Y1[i]) * uy + (geo.Z1[j] - geo.Z1[i]) * uz) / (li * li);
                        double q2 = ((geo.X2[j] - geo.X1[i]) * ux + (geo.Y2[j] - geo.Y1[i]) * uy + (geo.Z2[j] - geo.Z1[i]) * uz) / (li * li);
                        double comune = Math.Min(1, Math.Max(q1, q2)) - Math.Max(0, Math.Min(q1, q2));
                        if (comune <= 1e-6 && d > tol) continue;
                        if (d <= tol) { hS.Add(cp(i, j)); if (esS == null) esS = coppia; }
                        else { hP.Add(cp(i, j)); if (esP == null) esP = coppia; }
                        continue;
                    }
                    // regole 2 e 3: estremo appoggiato a meta' di un altro filo, o due fili che si incrociano, a distanza zero
                    if (d <= tol && !(sEstremo && tEstremo))
                    {
                        if (sEstremo || tEstremo) { hA.Add(sEstremo ? cp(i, j) : cp(j, i)); if (esA == null) esA = string.Format(CC, "filo {0} sul filo {1}", sEstremo ? geo.Tag[i] : geo.Tag[j], sEstremo ? geo.Tag[j] : geo.Tag[i]); }
                        else { hI.Add(cp(i, j)); if (esI == null) esI = coppia; }
                        continue;
                    }
                    // regola 4: fili piu' vicini della somma dei raggi senza essere collegati ("contact improperly or are too close"),
                    // compresi due estremi vicini ma non uniti (due tubi in fila con uno spazio piu' piccolo dei raggi). Collaudo 0.4.20: non
                    // se i due estremi sono uniti da un altro segmento (curva fatta di fili corti, come nella QFH: EZNEC non segnala)
                    if (sEstremo && tEstremo)
                    {
                        double pax = s < 0.5 ? geo.X1[i] : geo.X2[i], pay = s < 0.5 ? geo.Y1[i] : geo.Y2[i], paz = s < 0.5 ? geo.Z1[i] : geo.Z2[i];
                        double pbx = t < 0.5 ? geo.X1[j] : geo.X2[j], pby = t < 0.5 ? geo.Y1[j] : geo.Y2[j], pbz = t < 0.5 ? geo.Z1[j] : geo.Z2[j];
                        bool ponte = false;
                        for (int z = 0; z < n && !ponte; z++)
                        {
                            if (z == i || z == j) continue;
                            double tz = 1e-3 * Math.Max(1e-9, Math.Min(geo.Length(z), Math.Min(li, lj)));
                            ponte = (Vic(geo.X1[z], geo.Y1[z], geo.Z1[z], pax, pay, paz, tz) && Vic(geo.X2[z], geo.Y2[z], geo.Z2[z], pbx, pby, pbz, tz)) ||
                                    (Vic(geo.X2[z], geo.Y2[z], geo.Z2[z], pax, pay, paz, tz) && Vic(geo.X1[z], geo.Y1[z], geo.Z1[z], pbx, pby, pbz, tz));
                        }
                        if (ponte) continue;
                    }
                    hV.Add(cp(i, j)); if (esV == null) esV = coppia;
                }
            int nAppoggi = hA.Count, nIncroci = hI.Count, nVicini = hV.Count, nParalleli = hP.Count, nSovrapposti = hS.Count, nCentro = hC.Count;
            if (nAppoggi > 0)
                av.Add(string.Format(CC, "{0} fili con un estremo appoggiato a meta' di un segmento di un altro filo (per esempio {1}): nel NEC-2 i fili si collegano solo negli estremi " +
                    "dei segmenti, quindi qui NON sono collegati e il calcolo e' di un'antenna diversa da quella disegnata. EZNEC lo rifiuta (Geometry Error: \"end intersects wire at other than a segment junction or end\"). Spezzare il filo nel punto di contatto.", nAppoggi, esA));
            if (nIncroci > 0)
                av.Add(string.Format(CC, "{0} coppie di fili che si incrociano fuori dalle giunzioni (per esempio {1}): il NEC-2 non li collega e i loro campi si sovrappongono. " +
                    "EZNEC lo rifiuta (Geometry Error: \"intersect at other than a segment junction or end\"). Spezzare i fili nel punto d'incrocio o allontanarli.", nIncroci, esI));
            if (nVicini > 0)
                av.Add(string.Format(CC, "{0} coppie di fili piu' vicine della somma dei raggi senza essere collegate (per esempio {1}): i due conduttori si compenetrano e il NEC-2 non " +
                    "li tratta come uniti. EZNEC lo rifiuta (Geometry Error: \"contact improperly or are too close\"). Unire gli estremi in un punto o distanziare i fili.", nVicini, esV));
            if (nParalleli > 0)
                av.Add(string.Format(CC, "{0} coppie di fili paralleli che si toccano (per esempio {1}): due conduttori affiancati a distanza minore della somma dei raggi. " +
                    "EZNEC lo rifiuta (Geometry Error: \"parallel and contacting\"). Lasciare un solo filo di raggio equivalente o distanziarli.", nParalleli, esP));
            if (nSovrapposti > 0)
                av.Add(string.Format(CC, "{0} coppie di fili sovrapposti, uno sopra l'altro (per esempio {1}): lo stesso tratto descritto due volte. EZNEC lo rifiuta (Geometry Error: \"overlap\"). " +
                    "Togliere il filo doppio.", nSovrapposti, esS));
            if (nCentro > 0)
                av.Add(string.Format(CC, "{0} fili con il centro del segmento finale dentro un altro filo (per esempio {1}): dove un filo sottile entra in un tubo grosso il suo ultimo segmento " +
                    "e' piu' corto del raggio del tubo e il punto di calcolo cade dentro il conduttore. EZNEC lo rifiuta (Geometry Error: \"center of end segment within wire\"). " +
                    "Allungare il segmento finale (meno segmenti su quel filo) o ridurre il raggio del tubo vicino alla giunzione.", nCentro, esC));
            return av;
        }

        // distanza di un punto dal segmento j
        static double DistPunto(SegmentGeometry g, int j, double px, double py, double pz)
        {
            double ax = g.X1[j], ay = g.Y1[j], az = g.Z1[j], dx = g.X2[j] - ax, dy = g.Y2[j] - ay, dz = g.Z2[j] - az;
            double l2 = dx * dx + dy * dy + dz * dz;
            double t = l2 > 0 ? Math.Max(0, Math.Min(1, ((px - ax) * dx + (py - ay) * dy + (pz - az) * dz) / l2)) : 0;
            double ex = ax + t * dx - px, ey = ay + t * dy - py, ez = az + t * dz - pz;
            return Math.Sqrt(ex * ex + ey * ey + ez * ez);
        }

        static bool Vic(double x1, double y1, double z1, double x2, double y2, double z2, double tol)
        {
            return Math.Abs(x1 - x2) <= tol && Math.Abs(y1 - y2) <= tol && Math.Abs(z1 - z2) <= tol;
        }

        /// <summary>
        /// 0.4.30 (collaudo 0.4.23 di IC8TEM, 14/11/2026 21:50, approvato alle 21:53): fili che entrano nel terreno o ci stanno distesi.
        /// EZNEC si ferma (codice 1) con "One or more wires extend into or are lying on the ground. (Note: Wires within 1/1000 segment
        /// length are considered to be connected.) Wire Z coordinate must be changed or free space analysis selected." Regola ricavata
        /// dal GPR di IC8TEM (radiali a z = 2 mm con raggio 8 mm) e verificata su tutti i modelli d'archivio con terreno: solo i due GPR
        /// segnalati, entrambi fermati da EZNEC, nessun falso allarme. Un estremo di filo e' collegato al terreno se sta a z = 0 entro
        /// 1/1000 della lunghezza del segmento; e' dentro il terreno se sta sotto lo zero, o sopra lo zero ma piu' in basso del raggio
        /// (la superficie del tubo passa sotto il suolo); un filo con i due estremi entro il raggio dal suolo e' disteso sul terreno.
        /// </summary>
        public static List<string> FiliNelTerreno(SegmentGeometry geo, GroundSpec g)
        {
            List<string> av = new List<string>();
            if (geo == null || geo.N == 0 || g == null || g.Kind == GroundKind.FreeSpace) return av;
            int nDentro = 0, nDisteso = 0;
            string es = null;
            int i = 0;
            while (i < geo.N)
            {
                int j = i;
                while (geo.Tag[i] != 0 && j + 1 < geo.N && geo.Tag[j + 1] == geo.Tag[i]) j++;
                double r = geo.Bi[i], z1 = geo.Z1[i], z2 = geo.Z2[j];
                double tol = 1e-3 * geo.Length(i), tol2 = 1e-3 * geo.Length(j);
                bool dentro = z1 < -tol || (z1 > tol && z1 < r) || z2 < -tol2 || (z2 > tol2 && z2 < geo.Bi[j]);
                bool disteso = Math.Abs(z1) <= r && Math.Abs(z2) <= geo.Bi[j];
                if (dentro || disteso)
                {
                    if (disteso) nDisteso++; else nDentro++;
                    if (es == null) es = string.Format(CC, "filo {0}: estremi a z = {1:0.####} e {2:0.####} m con raggio {3:0.####} m", geo.Tag[i], z1, z2, r);
                }
                i = j + 1;
            }
            if (nDentro + nDisteso > 0)
                av.Add(string.Format(CC, "{0} fili dentro il terreno o distesi sul terreno ({1} distesi; per esempio {2}): la superficie del tubo passa sotto il suolo. " +
                    "EZNEC lo rifiuta (\"One or more wires extend into or are lying on the ground\"). Alzare il filo almeno di un raggio sopra il suolo (radiali: qualche centimetro), " +
                    "oppure collegarlo al suolo con un solo estremo esattamente a z = 0.", nDentro + nDisteso, nDisteso, es));
            return av;
        }

        /// <summary>Avvisi per fili orizzontali troppo vicini al terreno, per fili sotto il terreno e per giunzioni con troppi fili.</summary>
        public static List<string> Controlla(SegmentGeometry geo, GroundSpec g, double fmhz)
        {
            List<string> av = new List<string>();
            if (geo == null || geo.N == 0) return av;
            try { av.AddRange(ContattiFuoriGiunzione(geo)); } catch { }
            try { av.AddRange(FiliNelTerreno(geo, g)); } catch { }
            bool terreno = g != null && g.Kind != GroundKind.FreeSpace;
            if (terreno)
            {
                double lam = C0 / Math.Max(1e-9, fmhz);
                double hMinOriz = double.MaxValue, rMin = 0, hSotto = double.MaxValue;
                int nOriz = 0, nRaggio = 0;
                for (int i = 0; i < geo.N; i++)
                {
                    double l = geo.Length(i);
                    if (l <= 0) continue;
                    double zmin = Math.Min(geo.Z1[i], geo.Z2[i]);
                    if (zmin < -1e-9) hSotto = Math.Min(hSotto, zmin);
                    double dz = Math.Abs(geo.Z2[i] - geo.Z1[i]);
                    if (dz > 0.5 * l) continue;   // segmento piu' verticale che orizzontale
                    double h = geo.CenterZ(i);
                    if (h < hMinOriz) { hMinOriz = h; rMin = geo.Bi[i]; }
                    if (h < QuotaMinima(g, fmhz)) nOriz++;
                    if (h > 0 && h < 3 * geo.Bi[i]) nRaggio++;
                }
                if (hSotto < 0)
                    av.Add(string.Format(CC, "Fili sotto il terreno (fino a {0:0.###} m): il NEC-2 non tratta fili interrati; per i radiali sepolti serve il NEC-4.", hSotto));
                double qm = QuotaMinima(g, fmhz);
                if (nOriz > 0 && qm > 0)
                {
                    if (g.Sommerfeld)
                        av.Add(string.Format(CC, "{0} segmenti orizzontali sotto lambda/1000 = {1:0.###} m (il piu' basso a {2:0.###} m): e' sotto il limite del terreno di Sommerfeld del NEC-2; " +
                            "l'impedenza e le perdite diventano sempre meno affidabili man mano che il filo scende (vedi BOG nel manuale). Per un risultato sicuro: almeno lambda/200 = {3:0.###} m.",
                            nOriz, qm, hMinOriz, lam / 200));
                    else
                        av.Add(string.Format(CC, "{0} segmenti orizzontali sotto lambda/10 = {1:0.###} m (il piu' basso a {2:0.###} m) con il terreno a coefficiente di riflessione (GN 0): " +
                            "il NEC-2 non e' affidabile cosi' vicino al suolo (impedenza e guadagno anche impossibili, come una resistenza negativa). Usare il terreno di Sommerfeld (GN 2).",
                            nOriz, qm, hMinOriz));
                }
                if (nRaggio > 0)
                    av.Add(string.Format(CC, "{0} segmenti orizzontali a meno di 3 raggi dal terreno (il piu' basso a {1:0.####} m, raggio {2:0.####} m): l'approssimazione del filo sottile non vale.", nRaggio, hMinOriz, rMin));
            }
            // 0.4.20: segmenti piu' corti di lambda/1000 (NEC-2 Part III sez. II; EZNEC li segnala con un avviso)
            if (fmhz > 0)
            {
                double lmin = C0 / fmhz / 1000.0, lmax = C0 / fmhz / 10.0, lpiu = double.MaxValue, lmagg = 0;
                int nCorti = 0, tagCorto = 0, nLunghi = 0, tagLungo = 0;
                for (int i = 0; i < geo.N; i++)
                {
                    double l = geo.Length(i);
                    if (l > lmax) { nLunghi++; if (l > lmagg) { lmagg = l; tagLungo = geo.Tag[i]; } }
                    if (l <= 0 || l >= lmin) continue;
                    nCorti++;
                    if (l < lpiu) { lpiu = l; tagCorto = geo.Tag[i]; }
                }
                // stessa guida: "Generally, Delta should be less than about 0.1 lambda"; EZNEC lo segnala (avviso, oltre circa 0,12 lambda in rosso)
                if (nLunghi > 0)
                    av.Add(string.Format(CC, "{0} segmenti piu' lunghi di lambda/10 = {1:0.####} m (il piu' lungo {2:0.####} m, filo {3}): la guida del NEC-2 li sconsiglia; " +
                        "EZNEC li segnala come avviso (\"segment length too long\", oltre circa 0,12 lambda in rosso). Servono piu' segmenti.", nLunghi, lmax, lmagg, tagLungo));
                if (nCorti > 0)
                    av.Add(string.Format(CC, "{0} segmenti piu' corti di lambda/1000 = {1:0.####} m (il piu' corto {2:0.####} m, filo {3}): la guida del NEC-2 dice di evitarli " +
                        "perche' il calcolo perde precisione; YA-NEC e EZNEC li calcolano lo stesso, EZNEC con un avviso (\"segment length too short\"). Si allungano i fili corti o si usano meno segmenti.",
                        nCorti, lmin, lpiu, tagCorto));
            }
            // giunzioni: estremi di segmento nello stesso punto (tolleranza 0,1 mm); dentro un filo un punto ha 2 estremi
            Dictionary<long, int> conta = new Dictionary<long, int>();
            Func<double, double, double, long> chiave = (x, y, z) =>
            {
                long a = (long)Math.Round(x * 1e4), b = (long)Math.Round(y * 1e4), c = (long)Math.Round(z * 1e4);
                return (a * 73856093L) ^ (b * 19349663L) ^ (c * 83492791L);
            };
            int maxFili = 0;
            double mx = 0, my = 0, mz = 0;
            for (int i = 0; i < geo.N; i++)
            {
                for (int e = 0; e < 2; e++)
                {
                    double x = e == 0 ? geo.X1[i] : geo.X2[i], y = e == 0 ? geo.Y1[i] : geo.Y2[i], z = e == 0 ? geo.Z1[i] : geo.Z2[i];
                    long k = chiave(x, y, z);
                    int c;
                    conta.TryGetValue(k, out c);
                    conta[k] = ++c;
                    if (c > maxFili) { maxFili = c; mx = x; my = y; mz = z; }
                }
            }
            if (maxFili > 30)
                av.Add(string.Format(CC, "{0} fili si uniscono nel punto ({1:0.###}; {2:0.###}; {3:0.###}) m: oltre circa 30 fili in una giunzione il NEC-2 perde precisione. " +
                    "Si puo' usare un piccolo mozzo (alcuni raggi corti a cui si collegano gruppi di fili).", maxFili, mx, my, mz));
            return av;
        }
    }
}
