// YA-NEC - generazione della geometria (GW, GA, GH, GM, GR, GS, GX, GC) e connessione dei segmenti.
// Traduzione fedele di geometry.c di nec2c (N. Kyriazis 5B4AZ, dal NEC2 di LLNL), GPL-3.0.
// Simmetrie (GX/GR): dalla 0.4.2 (approvato da IC8TEM il 13/11/2026) la geometria ricorda l'ultima simmetria e il motore
// la sfrutta nella soluzione come nec2c (Nec2Engine.Simmetria.cs), dopo averla verificata segmento per segmento.
using System;
using System.Collections.Generic;

namespace YaNec.Engine
{
    public class GeometryBuilder
    {
        const double PI = 3.141592654, TP = 6.283185308, TA = 1.745329252E-02, SMIN = 1.0e-3;
        const int PCHCON = 100000;

        readonly List<double> x1 = new List<double>(), y1 = new List<double>(), z1 = new List<double>();
        readonly List<double> x2 = new List<double>(), y2 = new List<double>(), z2 = new List<double>();
        readonly List<double> bi = new List<double>();
        readonly List<int> itag = new List<int>();
        public readonly List<string> Notes = new List<string>();
        int symNp, symNop;
        string symPiani = "";

        int N { get { return x1.Count; } }

        void Ensure(int count)
        {
            while (x1.Count < count)
            {
                x1.Add(0); y1.Add(0); z1.Add(0); x2.Add(0); y2.Add(0); z2.Add(0); bi.Add(0); itag.Add(0);
            }
        }

        void Truncate(int count)
        {
            if (x1.Count <= count) return;
            int r = x1.Count - count;
            x1.RemoveRange(count, r); y1.RemoveRange(count, r); z1.RemoveRange(count, r);
            x2.RemoveRange(count, r); y2.RemoveRange(count, r); z2.RemoveRange(count, r);
            bi.RemoveRange(count, r); itag.RemoveRange(count, r);
        }

        /// <summary>Costruisce la geometria a segmenti dalle schede e la connette (scheda GE con flag di terreno ignd).</summary>
        public static SegmentGeometry Build(IList<GeomCard> cards, int ignd, List<string> notes)
        {
            GeometryBuilder g = new GeometryBuilder();
            foreach (GeomCard c in cards) g.Apply(c);
            SegmentGeometry sg = g.Connect(ignd);
            if (notes != null) notes.AddRange(g.Notes);
            return sg;
        }

        public void Apply(GeomCard c)
        {
            switch (c.Id)
            {
                case "GW":
                    {
                        if (c.I2 < 1) throw new NecException("GW con tag " + c.I1 + ": il numero di segmenti deve essere almeno 1.");
                        double rad = c.F7, rdel = 1, rrad = 1;
                        if (rad == 0.0)
                        {
                            if (!c.HasGc) throw new NecException("GW con tag " + c.I1 + " ha raggio 0 ma manca la scheda GC.");
                            if (c.GcRad1 == 0.0 || c.GcRad2 == 0.0) throw new NecException("Scheda GC con raggio nullo (tag " + c.I1 + ").");
                            rdel = c.GcRdel;
                            rad = c.GcRad1;
                            rrad = c.I2 > 1 ? Math.Pow(c.GcRad2 / c.GcRad1, 1.0 / (c.I2 - 1.0)) : 1.0;
                        }
                        Wire(c.F1, c.F2, c.F3, c.F4, c.F5, c.F6, rad, rdel, rrad, c.I2, c.I1);
                        break;
                    }
                case "GA":
                    Arc(c.I1, c.I2, c.F1, c.F2, c.F3, c.F4);
                    break;
                case "GH":
                    Helix(c.F1, c.F2, c.F3, c.F4, c.F5, c.F6, c.F7, c.I2, c.I1);
                    break;
                case "GM":
                    Move(c.F1 * TA, c.F2 * TA, c.F3 * TA, c.F4, c.F5, c.F6, (int)(c.F7 + .5), c.I2, c.I1);
                    break;
                case "GS":
                    Scale(c.F1);
                    break;
                case "GX":
                    {
                        int ns = c.I2;
                        int iy = ns / 10;
                        int iz = ns - iy * 10;
                        int ix = iy / 10;
                        iy = iy - ix * 10;
                        if (ix != 0) ix = 1;
                        if (iy != 0) iy = 1;
                        if (iz != 0) iz = 1;
                        Reflc(ix, iy, iz, c.I1, ns);
                        break;
                    }
                case "GR":
                    Reflc(-1, 0, 0, c.I1, c.I2);
                    break;
                case "SP":
                case "SM":
                case "SC":
                    throw new NecException("Le superfici (schede SP/SM/SC) non sono ancora supportate in questa versione.");
                case "GF":
                    throw new NecException("La scheda GF (funzione di Green numerica) non e' supportata.");
                default:
                    throw new NecException("Scheda di geometria sconosciuta: " + c.Id);
            }
        }

        // ---- arc: arco di ns segmenti nel piano XZ ----
        void Arc(int itg, int ns, double rada, double ang1, double ang2, double rad)
        {
            int ist = N;
            if (ns < 1) return;
            if (Math.Abs(ang2 - ang1) >= 360.00001) throw new NecException("GA: l'angolo dell'arco supera 360 gradi.");
            Ensure(ist + ns);
            double ang = ang1 * TA;
            double dang = (ang2 - ang1) * TA / ns;
            double xs1 = rada * Math.Cos(ang);
            double zs1 = rada * Math.Sin(ang);
            for (int i = ist; i < ist + ns; i++)
            {
                ang += dang;
                double xs2 = rada * Math.Cos(ang);
                double zs2 = rada * Math.Sin(ang);
                x1[i] = xs1; y1[i] = 0.0; z1[i] = zs1;
                x2[i] = xs2; y2[i] = 0.0; z2[i] = zs2;
                xs1 = xs2; zs1 = zs2;
                bi[i] = rad;
                itag[i] = itg;
            }
        }

        // ---- helix ----
        void Helix(double s, double hl, double a1, double b1, double a2, double b2, double rad, int ns, int itg)
        {
            int ist = N;
            if (ns < 1) return;
            Ensure(ist + ns);
            double zinc = Math.Abs(hl / ns);
            z1[ist] = 0.0;
            for (int i = ist; i < ist + ns; i++)
            {
                bi[i] = rad;
                itag[i] = itg;
                if (i != ist) z1[i] = z1[i - 1] + zinc;
                z2[i] = z1[i] + zinc;
                if (a2 == a1)
                {
                    if (b1 == 0.0) b1 = a1;
                    x1[i] = a1 * Math.Cos(2.0 * PI * z1[i] / s);
                    y1[i] = b1 * Math.Sin(2.0 * PI * z1[i] / s);
                    x2[i] = a1 * Math.Cos(2.0 * PI * z2[i] / s);
                    y2[i] = b1 * Math.Sin(2.0 * PI * z2[i] / s);
                }
                else
                {
                    if (b2 == 0.0) b2 = a2;
                    x1[i] = (a1 + (a2 - a1) * z1[i] / Math.Abs(hl)) * Math.Cos(2.0 * PI * z1[i] / s);
                    y1[i] = (b1 + (b2 - b1) * z1[i] / Math.Abs(hl)) * Math.Sin(2.0 * PI * z1[i] / s);
                    x2[i] = (a1 + (a2 - a1) * z2[i] / Math.Abs(hl)) * Math.Cos(2.0 * PI * z2[i] / s);
                    y2[i] = (b1 + (b2 - b1) * z2[i] / Math.Abs(hl)) * Math.Sin(2.0 * PI * z2[i] / s);
                }
                if (hl > 0.0) continue;
                double copy = x1[i]; x1[i] = y1[i]; y1[i] = copy;
                copy = x2[i]; x2[i] = y2[i]; y2[i] = copy;
            }
        }

        int SegNoLocal(int itagi, int mx)
        {
            if (mx <= 0) throw new NecException("Posizione del segmento non valida (zero).");
            if (itagi == 0) return mx;
            int icnt = 0;
            for (int i = 0; i < N; i++)
            {
                if (itag[i] != itagi) continue;
                icnt++;
                if (icnt == mx) return i + 1;
            }
            throw new NecException("Nessun segmento ha il tag " + itagi + ".");
        }

        // ---- move: rotazione attorno agli assi x,y,z poi traslazione, con eventuali copie (GM) ----
        void Move(double rox, double roy, double roz, double xs, double ys, double zs, int its, int nrpt, int itgi)
        {
            double sps = Math.Sin(rox), cps = Math.Cos(rox);
            double sth = Math.Sin(roy), cth = Math.Cos(roy);
            double sph = Math.Sin(roz), cph = Math.Cos(roz);
            double xx = cph * cth;
            double xy = cph * sth * sps - sph * cps;
            double xz = cph * sth * cps + sph * sps;
            double yx = sph * cth;
            double yy = sph * sth * sps + cph * cps;
            double yz = sph * sth * cps - cph * sps;
            double zx = -sth;
            double zy = cth * sps;
            double zz = cth * cps;
            int nrp = nrpt == 0 ? 1 : nrpt;
            if (N <= 0) return;
            int i1 = SegNoLocal(its, 1);
            if (i1 < 1) i1 = 1;
            int k = nrpt == 0 ? i1 - 1 : N;
            int n = N;
            for (int ir = 0; ir < nrp; ir++)
            {
                for (int i = i1 - 1; i < n; i++)
                {
                    Ensure(k + 1);
                    double xi = x1[i], yi = y1[i], zi = z1[i];
                    double nx1 = xi * xx + yi * xy + zi * xz + xs;
                    double ny1 = xi * yx + yi * yy + zi * yz + ys;
                    double nz1 = xi * zx + yi * zy + zi * zz + zs;
                    xi = x2[i]; yi = y2[i]; zi = z2[i];
                    double nx2 = xi * xx + yi * xy + zi * xz + xs;
                    double ny2 = xi * yx + yi * yy + zi * yz + ys;
                    double nz2 = xi * zx + yi * zy + zi * zz + zs;
                    x1[k] = nx1; y1[k] = ny1; z1[k] = nz1;
                    x2[k] = nx2; y2[k] = ny2; z2[k] = nz2;
                    bi[k] = bi[i];
                    itag[k] = itag[i];
                    if (itag[i] != 0) itag[k] = itag[i] + itgi;
                    k++;
                }
                i1 = n + 1;
                n = k;
            }
            Truncate(n);
        }

        void Scale(double f)
        {
            for (int i = 0; i < N; i++)
            {
                x1[i] *= f; y1[i] *= f; z1[i] *= f;
                x2[i] *= f; y2[i] *= f; z2[i] *= f;
                bi[i] *= f;
            }
        }

        // ---- reflc: riflessione nei piani coordinati (GX) o rotazione attorno a z (GR) ----
        void Reflc(int ix, int iy, int iz, int itx, int nop)
        {
            int iti = itx;
            int n = N;
            int n0 = n;
            if (ix >= 0)
            {
                if (nop == 0) return;
                symNp = n0; symNop = 1 << ((ix != 0 ? 1 : 0) + (iy != 0 ? 1 : 0) + (iz != 0 ? 1 : 0));
                symPiani = (iz != 0 ? "z" : "") + (iy != 0 ? "y" : "") + (ix != 0 ? "x" : "");
                if (iz != 0 && n > 0)
                {
                    Ensure(2 * n);
                    for (int i = 0; i < n; i++)
                    {
                        int nx = i + n;
                        double e1 = z1[i], e2 = z2[i];
                        if ((Math.Abs(e1) + Math.Abs(e2) <= 1.0e-5) || (e1 * e2 < -1.0e-6))
                            throw new NecException("GX: il segmento " + (i + 1) + " giace nel piano di simmetria.");
                        x1[nx] = x1[i]; y1[nx] = y1[i]; z1[nx] = -e1;
                        x2[nx] = x2[i]; y2[nx] = y2[i]; z2[nx] = -e2;
                        int itagi = itag[i];
                        itag[nx] = itagi == 0 ? 0 : itagi + iti;
                        bi[nx] = bi[i];
                    }
                    n = n * 2;
                    iti = iti * 2;
                }
                if (iy != 0 && n > 0)
                {
                    Ensure(2 * n);
                    for (int i = 0; i < n; i++)
                    {
                        int nx = i + n;
                        double e1 = y1[i], e2 = y2[i];
                        if ((Math.Abs(e1) + Math.Abs(e2) <= 1.0e-5) || (e1 * e2 < -1.0e-6))
                            throw new NecException("GX: il segmento " + (i + 1) + " giace nel piano di simmetria.");
                        x1[nx] = x1[i]; y1[nx] = -e1; z1[nx] = z1[i];
                        x2[nx] = x2[i]; y2[nx] = -e2; z2[nx] = z2[i];
                        int itagi = itag[i];
                        itag[nx] = itagi == 0 ? 0 : itagi + iti;
                        bi[nx] = bi[i];
                    }
                    n = n * 2;
                    iti = iti * 2;
                }
                if (ix == 0) return;
                if (n > 0)
                {
                    Ensure(2 * n);
                    for (int i = 0; i < n; i++)
                    {
                        int nx = i + n;
                        double e1 = x1[i], e2 = x2[i];
                        if ((Math.Abs(e1) + Math.Abs(e2) <= 1.0e-5) || (e1 * e2 < -1.0e-6))
                            throw new NecException("GX: il segmento " + (i + 1) + " giace nel piano di simmetria.");
                        x1[nx] = -e1; y1[nx] = y1[i]; z1[nx] = z1[i];
                        x2[nx] = -e2; y2[nx] = y2[i]; z2[nx] = z2[i];
                        int itagi = itag[i];
                        itag[nx] = itagi == 0 ? 0 : itagi + iti;
                        bi[nx] = bi[i];
                    }
                }
                return;
            }

            // rotazione per formare una struttura cilindrica
            if (nop < 1) return;
            symNp = n0; symNop = nop; symPiani = "";
            double sam = TP / nop;
            double cs = Math.Cos(sam), ss = Math.Sin(sam);
            int np = n;
            if (n > 0)
            {
                int ntot = n * nop;
                Ensure(ntot);
                for (int i = np; i < ntot; i++)
                {
                    int k = i - np;
                    double xk = x1[k], yk = y1[k];
                    x1[i] = xk * cs - yk * ss;
                    y1[i] = xk * ss + yk * cs;
                    z1[i] = z1[k];
                    xk = x2[k]; yk = y2[k];
                    x2[i] = xk * cs - yk * ss;
                    y2[i] = xk * ss + yk * cs;
                    z2[i] = z2[k];
                    bi[i] = bi[k];
                    int itagi = itag[k];
                    itag[i] = itagi == 0 ? 0 : itagi + iti;
                }
            }
        }

        // ---- wire: filo dritto di ns segmenti (rdel = rapporto tra lunghezze successive, rrad = rapporto tra raggi) ----
        void Wire(double xw1, double yw1, double zw1, double xw2, double yw2, double zw2,
                  double rad, double rdel, double rrad, int ns, int itg)
        {
            int ist = N;
            if (ns < 1) return;
            Ensure(ist + ns);
            double xd = xw2 - xw1, yd = yw2 - yw1, zd = zw2 - zw1, delz, rd;
            if (Math.Abs(rdel - 1.0) >= 1.0e-6)
            {
                delz = Math.Sqrt(xd * xd + yd * yd + zd * zd);
                xd = xd / delz; yd = yd / delz; zd = zd / delz;
                delz = delz * (1.0 - rdel) / (1.0 - Math.Pow(rdel, ns));
                rd = rdel;
            }
            else
            {
                double fns = ns;
                xd = xd / fns; yd = yd / fns; zd = zd / fns;
                delz = 1.0;
                rd = 1.0;
            }
            double radz = rad, xs1 = xw1, ys1 = yw1, zs1 = zw1;
            int n = ist + ns;
            for (int i = ist; i < n; i++)
            {
                itag[i] = itg;
                double xs2 = xs1 + xd * delz, ys2 = ys1 + yd * delz, zs2 = zs1 + zd * delz;
                x1[i] = xs1; y1[i] = ys1; z1[i] = zs1;
                x2[i] = xs2; y2[i] = ys2; z2[i] = zs2;
                bi[i] = radz;
                delz = delz * rd;
                radz = radz * rrad;
                xs1 = xs2; ys1 = ys2; zs1 = zs2;
            }
            x2[n - 1] = xw2; y2[n - 1] = yw2; z2[n - 1] = zw2;
        }

        // ---- conect: connessioni tra le estremita' dei segmenti (icon1, icon2) e allineamento delle giunzioni ----
        public SegmentGeometry Connect(int ignd)
        {
            int n = N;
            SegmentGeometry g = new SegmentGeometry();
            g.N = n;
            g.GroundFlag = ignd;
            if (symNp > 0 && symNop > 1 && symNp * symNop == n) { g.SymNp = symNp; g.SymNop = symNop; g.SymPiani = symPiani; }
            double[] X1 = x1.ToArray(), Y1 = y1.ToArray(), Z1 = z1.ToArray();
            double[] X2 = x2.ToArray(), Y2 = y2.ToArray(), Z2 = z2.ToArray();
            int[] icon1 = new int[n], icon2 = new int[n];
            if (n == 0) throw new NecException("Il modello non contiene fili.");

            for (int i = 0; i < n; i++)
            {
                icon1[i] = icon2[i] = 0;
                int iz = i + 1;
                double xi1 = X1[i], yi1 = Y1[i], zi1 = Z1[i];
                double xi2 = X2[i], yi2 = Y2[i], zi2 = Z2[i];
                double slen = Math.Sqrt((xi2 - xi1) * (xi2 - xi1) + (yi2 - yi1) * (yi2 - yi1) + (zi2 - zi1) * (zi2 - zi1)) * SMIN;

                // estremo 1
                bool jump = false;
                if (ignd > 0)
                {
                    if (zi1 <= -slen) throw new NecException("Il segmento " + iz + " scende sotto il terreno.");
                    if (zi1 <= slen)
                    {
                        icon1[i] = iz;
                        Z1[i] = 0.0;
                        jump = true;
                    }
                }
                if (!jump)
                {
                    int ic = i;
                    for (int j = 1; j < n; j++)
                    {
                        ic++;
                        if (ic >= n) ic = 0;
                        double sep = Math.Abs(xi1 - X1[ic]) + Math.Abs(yi1 - Y1[ic]) + Math.Abs(zi1 - Z1[ic]);
                        if (sep <= slen) { icon1[i] = -(ic + 1); break; }
                        sep = Math.Abs(xi1 - X2[ic]) + Math.Abs(yi1 - Y2[ic]) + Math.Abs(zi1 - Z2[ic]);
                        if (sep <= slen) { icon1[i] = (ic + 1); break; }
                    }
                }

                // estremo 2
                if ((ignd > 0) || jump)
                {
                    if (zi2 <= -slen) throw new NecException("Il segmento " + iz + " scende sotto il terreno.");
                    if (zi2 <= slen)
                    {
                        if (icon1[i] == iz) throw new NecException("Il segmento " + iz + " giace sul piano del terreno: con il terreno l'antenna va messa sopra lo zero (quote Z maggiori di 0, per esempio con l'altezza dal suolo).");
                        icon2[i] = iz;
                        Z2[i] = 0.0;
                        continue;
                    }
                }
                {
                    int ic = i;
                    for (int j = 1; j < n; j++)
                    {
                        ic++;
                        if (ic >= n) ic = 0;
                        double sep = Math.Abs(xi2 - X1[ic]) + Math.Abs(yi2 - Y1[ic]) + Math.Abs(zi2 - Z1[ic]);
                        if (sep <= slen) { icon2[i] = (ic + 1); break; }
                        sep = Math.Abs(xi2 - X2[ic]) + Math.Abs(yi2 - Y2[ic]) + Math.Abs(zi2 - Z2[ic]);
                        if (sep <= slen) { icon2[i] = -(ic + 1); break; }
                    }
                }
            }

            // estremi connessi resi coincidenti; giunzioni di 3 o piu' segmenti
            int[] jco = new int[n + 2];
            int junctions = 0;
            for (int j = 0; j < n; j++)
            {
                int jx = j + 1, iend = -1, jend = -1, ix = icon1[j], ic = 1;
                jco[0] = -jx;
                double xa = X1[j], ya = Y1[j], za = Z1[j];
                while (true)
                {
                    if ((ix != 0) && (ix != (j + 1)) && (ix <= PCHCON))
                    {
                        bool jump;
                        do
                        {
                            if (ix < 0) ix = -ix;
                            else jend = -jend;
                            jump = false;
                            if (ix == jx) break;
                            if (ix < jx) { jump = true; break; }
                            ic++;
                            if (ic > jco.Length) Array.Resize(ref jco, ic + 8);
                            jco[ic - 1] = ix * jend;
                            int ixx = ix - 1;
                            if (jend != 1)
                            {
                                xa = xa + X1[ixx]; ya = ya + Y1[ixx]; za = za + Z1[ixx];
                                ix = icon1[ixx];
                                continue;
                            }
                            xa = xa + X2[ixx]; ya = ya + Y2[ixx]; za = za + Z2[ixx];
                            ix = icon2[ixx];
                        } while (ix != 0);

                        if (jump && (iend == 1)) break;
                        else if (jump)
                        {
                            iend = 1; jend = 1;
                            ix = icon2[j];
                            ic = 1;
                            jco[0] = jx;
                            xa = X2[j]; ya = Y2[j]; za = Z2[j];
                            continue;
                        }
                        double sep = ic;
                        xa = xa / sep; ya = ya / sep; za = za / sep;
                        for (int i = 0; i < ic; i++)
                        {
                            int ixj = jco[i];
                            if (ixj <= 0)
                            {
                                int ixx = -ixj - 1;
                                X1[ixx] = xa; Y1[ixx] = ya; Z1[ixx] = za;
                                continue;
                            }
                            int ixy = ixj - 1;
                            X2[ixy] = xa; Y2[ixy] = ya; Z2[ixy] = za;
                        }
                        if (ic >= 3) junctions++;
                    }
                    if (iend == 1) break;
                    iend = 1; jend = 1;
                    ix = icon2[j];
                    ic = 1;
                    jco[0] = jx;
                    xa = X2[j]; ya = Y2[j]; za = Z2[j];
                }
            }
            if (junctions > 0) Notes.Add("Giunzioni di 3 o piu' segmenti: " + junctions);

            g.X1 = X1; g.Y1 = Y1; g.Z1 = Z1; g.X2 = X2; g.Y2 = Y2; g.Z2 = Z2;
            g.Bi = bi.ToArray();
            g.Tag = itag.ToArray();
            g.Icon1 = icon1; g.Icon2 = icon2;
            for (int i = 0; i < n; i++)
            {
                if (g.Length(i) <= 1.0e-20) throw new NecException("Il segmento " + (i + 1) + " ha lunghezza nulla.");
                if (g.Bi[i] <= 0.0) throw new NecException("Il segmento " + (i + 1) + " (tag " + g.Tag[i] + ") ha raggio nullo o negativo.");
            }
            ControllaNumeriESovrapposti(g);
            return g;
        }
    
        /// <summary>1.5.9 (controlli di ingresso come nec2-rs e momwire): coordinate non finite o fuori scala (GM, GS o GW con
        /// valori enormi davano NaN o bloccavano il calcolo) e segmenti sovrapposti (due fili sullo stesso tratto davano Z = 0
        /// senza nessun avviso).</summary>
        static void ControllaNumeriESovrapposti(SegmentGeometry g)
        {
            int n = g.X1.Length;
            const double MAXC = 1e9;   // un milione di km: oltre e' certamente un errore del file
            for (int i = 0; i < n; i++)
            {
                double[] v = { g.X1[i], g.Y1[i], g.Z1[i], g.X2[i], g.Y2[i], g.Z2[i], g.Bi[i] };
                foreach (double c in v)
                    if (double.IsNaN(c) || double.IsInfinity(c) || Math.Abs(c) > MAXC)
                        throw new NecException("Il segmento " + (i + 1) + " (tag " + g.Tag[i] + ") ha coordinate o raggio non validi o fuori scala (oltre 10^9 m): controlla le schede GW, GM e GS.");
            }
            // segmenti sovrapposti: paralleli, con il centro dell'uno sull'altro; ricerca ordinata per x del centro
            double[] mx = new double[n], len = new double[n];
            int[] ord = new int[n];
            double lmax = 0;
            for (int i = 0; i < n; i++)
            {
                mx[i] = 0.5 * (g.X1[i] + g.X2[i]);
                len[i] = g.Length(i);
                if (len[i] > lmax) lmax = len[i];
                ord[i] = i;
            }
            Array.Sort(mx, ord);
            for (int a = 0; a < n; a++)
            {
                int i = ord[a];
                for (int b = a + 1; b < n && mx[b] - mx[a] <= 0.5 * lmax; b++)
                {
                    int j = ord[b];
                    if (Sovrapposti(g, i, j, len[i], len[j]) || Sovrapposti(g, j, i, len[j], len[i]))
                        throw new NecException("I segmenti " + (Math.Min(i, j) + 1) + " (tag " + g.Tag[Math.Min(i, j)] + ") e " + (Math.Max(i, j) + 1) + " (tag " + g.Tag[Math.Max(i, j)] +
                            ") sono sovrapposti: due fili occupano lo stesso tratto. Togli il filo doppio o spostalo.");
                }
            }
        }

        // vero se il centro del segmento j cade dentro il segmento i (tolleranza 1/1000 della lunghezza) e i due sono paralleli
        static bool Sovrapposti(SegmentGeometry g, int i, int j, double li, double lj)
        {
            double ux = (g.X2[i] - g.X1[i]) / li, uy = (g.Y2[i] - g.Y1[i]) / li, uz = (g.Z2[i] - g.Z1[i]) / li;
            double vx = (g.X2[j] - g.X1[j]) / lj, vy = (g.Y2[j] - g.Y1[j]) / lj, vz = (g.Z2[j] - g.Z1[j]) / lj;
            if (Math.Abs(ux * vx + uy * vy + uz * vz) < 0.9999) return false;
            double cx = 0.5 * (g.X1[j] + g.X2[j]) - g.X1[i], cy = 0.5 * (g.Y1[j] + g.Y2[j]) - g.Y1[i], cz = 0.5 * (g.Z1[j] + g.Z2[j]) - g.Z1[i];
            double t = cx * ux + cy * uy + cz * uz;
            double tol = 1e-3 * Math.Min(li, lj);
            if (t <= tol || t >= li - tol) return false;
            double dx = cx - t * ux, dy = cy - t * uy, dz = cz - t * uz;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz) <= tol;
        }
}
}
