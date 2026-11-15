// YA-NEC 0.4.20 - parabola primo fuoco: dimensionamento degli arcarecci DIRITTI tra le centine (scelta di IC8TEM del 14/11/2026 18:20,
// "arcarecci dritti"; 18:21 "cosi' abbiamo i pesi meglio distribuiti") e dima delle centine (18:20, "vorrei calcolare anche le dime delle
// costole"; 18:21 "costole o centine chiamale uguali": nel programma si dice sempre "centine").
//
// Arcarecci: ogni anello e' un poligono di N tratti diritti (N = centine), ognuno da centina a centina.
//   campata L = 2 r sin(pi/N) (corda); freccia della corda r (1 - cos(pi/N)) = errore della rete appoggiata sul poligono.
//   Carichi per metro di arcareccio (a favore di sicurezza: tutta la rete scarica sugli arcarecci, niente sulle centine):
//     vento  q_v = p_din (Cd_rete_disco b + 1,2 d)            b = fascia di rete portata (passo degli anelli; meta' per l'anello di bordo)
//     peso   q_g = g (m_rete b + massa del tubo al metro)
//     ghiaccio (verifica a parte, rete chiusa) q_i = p_din (1,42 b + 1,2 d) + q_g
//   Trave appoggiata sulle due centine: M = q L^2 / 8, sigma = M / W; freccia 5 q L^4 / (384 E I).
//   Criteri: sigma(vento + peso) <= f_y / gamma; freccia col vento <= L/200; freccia permanente (peso) <= lambda/40 alla frequenza
//   del calcolo (errore di superficie che resta, Ruze); col ghiaccio sigma <= f_y (solo avviso).
//   Si sceglie la sezione piu' leggera dell'elenco commerciale che rispetta i tre criteri, non sotto il tubo minimo (0.4.21: 16 x 1,5).
// Fonti: EN 755-2 (6060 T6 e 6082 T6, Rp0,2 150 e 260 MPa) e EN 1999-1-1 (Eurocodice 9, E = 70 GPa, alluminio); EN 10025-2 (S235, 235 MPa,
//   E 210 GPa); EN 13706-3 (profili pultrusi in vetroresina, classe E23: E >= 23 GPa, flessione >= 240 MPa); formule della trave appoggiata
//   (R. J. Roark, W. C. Young, "Roark's Formulas for Stress and Strain", tab. 8.1); Cd del disco come nel calcolo del vento (Hoerner, Idelchik).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YaNec.Engine
{
    public class ArcareccioMateriale
    {
        public string Nome;
        public double E_Pa, Fy_MPa, Gamma, Rho;
        public ArcareccioMateriale(string n, double e, double fy, double g, double rho) { Nome = n; E_Pa = e; Fy_MPa = fy; Gamma = g; Rho = rho; }
    }

    public class ArcareccioSezione
    {
        public bool Quadro;
        public double Lato_mm, Spess_mm;
        public double Area_m2, I_m4, W_m3;
        public string Nome { get { return Quadro ? string.Format(CultureInfo.InvariantCulture, "quadro {0:0.#}x{0:0.#}x{1:0.0#}", Lato_mm, Spess_mm) : string.Format(CultureInfo.InvariantCulture, "tondo {0:0.#}x{1:0.0#}", Lato_mm, Spess_mm); } }
        public static ArcareccioSezione Crea(bool quadro, double lato, double t)
        {
            double b = lato / 1000.0, s = t / 1000.0, bi = Math.Max(0, b - 2 * s);
            ArcareccioSezione x = new ArcareccioSezione { Quadro = quadro, Lato_mm = lato, Spess_mm = t };
            if (quadro) { x.Area_m2 = b * b - bi * bi; x.I_m4 = (Math.Pow(b, 4) - Math.Pow(bi, 4)) / 12.0; }
            else { x.Area_m2 = Math.PI * (b * b - bi * bi) / 4.0; x.I_m4 = Math.PI * (Math.Pow(b, 4) - Math.Pow(bi, 4)) / 64.0; }
            x.W_m3 = 2.0 * x.I_m4 / b;
            return x;
        }
    }

    public class ParabolaArcareccio
    {
        public int Anello;
        public double Raggio_m, Z_m, Campata_m, FrecciaCorda_mm, Fascia_m;
        public int Pezzi;
        public ArcareccioSezione Sezione;
        public double QVento_N_m, QPeso_N_m, QGhiaccio_N_m;
        public double Sigma_MPa, SigmaAmm_MPa, SigmaGhiaccio_MPa, FrecciaVento_mm, FrecciaPermanente_mm, FrecciaVentoMax_mm, FrecciaPermMax_mm;
        public double Massa_kg;
        public bool Ok, OkGhiaccio;
        public string Nota = "";
    }

    public static partial class Parabola
    {
        /// <summary>Materiali degli arcarecci: E, f_y (o resistenza a flessione per la vetroresina), coefficiente di sicurezza, densita'.</summary>
        public static readonly ArcareccioMateriale[] MaterialiArcarecci =
        {
            new ArcareccioMateriale("Alluminio 6060 T6 (EN 755-2)", 70e9, 150, 1.5, 2700),
            new ArcareccioMateriale("Alluminio 6082 T6 (EN 755-2)", 70e9, 260, 1.5, 2700),
            new ArcareccioMateriale("Acciaio S235 zincato (EN 10025-2)", 210e9, 235, 1.5, 7850),
            new ArcareccioMateriale("Vetroresina pultrusa E23 (EN 13706)", 23e9, 240, 3.0, 1900),
        };

        /// <summary>Tubi commerciali (lato o diametro, spessore in mm), dal piu' piccolo.</summary>
        static readonly double[,] TubiTondi = { { 10, 1 }, { 12, 1 }, { 15, 1 }, { 16, 1 }, { 16, 1.5 }, { 20, 1 }, { 20, 1.5 }, { 20, 2 }, { 22, 1.5 }, { 25, 1.5 }, { 25, 2 }, { 30, 2 }, { 32, 2 }, { 35, 2 }, { 40, 2 }, { 40, 3 }, { 50, 2 }, { 50, 3 }, { 60, 3 } };
        static readonly double[,] TubiQuadri = { { 10, 1 }, { 12, 1 }, { 15, 1 }, { 15, 1.5 }, { 20, 1.5 }, { 20, 2 }, { 25, 1.5 }, { 25, 2 }, { 30, 2 }, { 35, 2 }, { 40, 2 }, { 40, 3 }, { 50, 2 }, { 50, 3 }, { 60, 3 } };

        /// <summary>Arcarecci diritti, uno per anello: sezione piu' leggera che regge vento e peso con le frecce ammesse.</summary>
        public static List<ParabolaArcareccio> DimensionaArcarecci(ParabolaInput p, ParabolaResult r, double pDin, double cdRete, double lam_m)
        {
            List<ParabolaArcareccio> lista = new List<ParabolaArcareccio>();
            int na = r.AnelliRaggio_m == null ? 0 : r.AnelliRaggio_m.Length;
            int N = Math.Max(3, p.NCentine);
            if (na == 0) return lista;
            ArcareccioMateriale mat = MaterialiArcarecci[Math.Max(0, Math.Min(MaterialiArcarecci.Length - 1, p.ArcarecciMateriale))];
            double[,] tubi = p.ArcarecciQuadri ? TubiQuadri : TubiTondi;
            double a = p.Diametro_m / 2.0, passo = a / na;
            double mRete = p.ReteMassaSup_kg_m2;
            for (int i = 0; i < na; i++)
            {
                ParabolaArcareccio x = new ParabolaArcareccio { Anello = i + 1, Raggio_m = r.AnelliRaggio_m[i], Z_m = r.AnelliZ_m[i], Pezzi = N };
                double rr = x.Raggio_m;
                x.Campata_m = 2.0 * rr * Math.Sin(Math.PI / N);
                x.FrecciaCorda_mm = rr * (1.0 - Math.Cos(Math.PI / N)) * 1000.0;
                x.Fascia_m = i == na - 1 ? passo / 2.0 : passo;
                double L = x.Campata_m;
                x.FrecciaVentoMax_mm = L / 200.0 * 1000.0;
                x.FrecciaPermMax_mm = lam_m / 40.0 * 1000.0;
                x.SigmaAmm_MPa = mat.Fy_MPa / mat.Gamma;
                ArcareccioSezione scelta = null;
                for (int k = 0; k < tubi.GetLength(0); k++)
                {
                    // 0.4.21 (collaudo del 14/11/2026 20:24: sul disco da 3 m uscivano tubi 10x1 e 12x1, poco realistici per fissare la rete
                    // e troppo leggeri per il contrappeso; scelta di IC8TEM: minimo 16 x 1,5) - si scartano i tubi sotto il minimo
                    if (k < tubi.GetLength(0) - 1 && (tubi[k, 0] < p.ArcarecciLatoMin_mm - 1e-9 || tubi[k, 1] < p.ArcarecciSpessMin_mm - 1e-9)) continue;   // l'ultimo (il piu' grosso) resta sempre
                    ArcareccioSezione s = ArcareccioSezione.Crea(p.ArcarecciQuadri, tubi[k, 0], tubi[k, 1]);
                    double d = s.Lato_mm / 1000.0, mLin = s.Area_m2 * mat.Rho;
                    double qv = pDin * (cdRete * x.Fascia_m + 1.2 * d);
                    double qg = G0 * (mRete * x.Fascia_m + mLin);
                    double qi = pDin * (CdDiscoPieno * x.Fascia_m + 1.2 * d) + qg;
                    double sig = (qv + qg) * L * L / 8.0 / s.W_m3 / 1e6;
                    double fv = 5.0 * (qv + qg) * Math.Pow(L, 4) / (384.0 * mat.E_Pa * s.I_m4) * 1000.0;
                    double fp = 5.0 * qg * Math.Pow(L, 4) / (384.0 * mat.E_Pa * s.I_m4) * 1000.0;
                    double sigI = qi * L * L / 8.0 / s.W_m3 / 1e6;
                    bool ok = sig <= x.SigmaAmm_MPa && fv <= x.FrecciaVentoMax_mm && fp <= x.FrecciaPermMax_mm;
                    // si tiene l'ultima provata (la piu' grossa) se nessuna basta; la prima che basta e' la piu' leggera (elenco in ordine di area)
                    if (scelta == null || ok || k == tubi.GetLength(0) - 1)
                    {
                        scelta = s;
                        x.Sezione = s; x.QVento_N_m = qv; x.QPeso_N_m = qg; x.QGhiaccio_N_m = qi;
                        x.Sigma_MPa = sig; x.SigmaGhiaccio_MPa = sigI; x.FrecciaVento_mm = fv; x.FrecciaPermanente_mm = fp;
                        x.Ok = ok; x.OkGhiaccio = sigI <= mat.Fy_MPa;
                        x.Massa_kg = N * L * mLin;
                    }
                    if (ok) break;
                }
                if (!x.Ok) x.Nota = "nessun tubo dell'elenco basta: servono piu' centine (campata piu' corta) o un materiale piu' rigido";
                else if (!x.OkGhiaccio) x.Nota = "col ghiaccio supera lo snervamento";
                lista.Add(x);
            }
            return lista;
        }

        /// <summary>Elenco di taglio degli arcarecci (testo con tabulazioni, per il foglio di calcolo).</summary>
        public static string ArcarecciCsv(ParabolaResult r, ParabolaInput p, char sep = ';')
        {
            StringBuilder sb = new StringBuilder();
            string s = sep.ToString();
            sb.AppendLine(string.Join(s, "anello", "raggio_mm", "quota_z_mm", "pezzi", "campata_corda_mm", "sezione", "materiale", "massa_anello_kg", "sigma_MPa", "sigma_amm_MPa", "freccia_vento_mm", "freccia_peso_mm", "esito"));
            string mat = MaterialiArcarecci[Math.Max(0, Math.Min(MaterialiArcarecci.Length - 1, p.ArcarecciMateriale))].Nome;
            CultureInfo ci = CultureInfo.InvariantCulture;
            foreach (ParabolaArcareccio x in r.Arcarecci)
                sb.AppendLine(string.Join(s, x.Anello.ToString(ci), (x.Raggio_m * 1000).ToString("0", ci), (x.Z_m * 1000).ToString("0.0", ci), x.Pezzi.ToString(ci),
                    (x.Campata_m * 1000).ToString("0.0", ci), x.Sezione.Nome, mat, x.Massa_kg.ToString("0.00", ci), x.Sigma_MPa.ToString("0.0", ci), x.SigmaAmm_MPa.ToString("0.0", ci),
                    x.FrecciaVento_mm.ToString("0.00", ci), x.FrecciaPermanente_mm.ToString("0.00", ci), x.Ok ? (x.OkGhiaccio ? "OK" : "OK, ghiaccio oltre lo snervamento") : "NON BASTA"));
            return sb.ToString();
        }

        // ------------------------------------------------------------------------------------------------------------------------------
        // dima della centina: profilo z(r) della faccia su cui appoggia la rete e, se la centina ha un'altezza, il bordo dietro
        // (parallelo al profilo, lungo la normale verso l'esterno del disco). Tacche: anelli (arcarecci), attacco dei puntoni, ogni 100 mm.

        /// <summary>Punti della faccia (r, z) e del dorso della centina, in metri, dal centro (r = rMozzo) al bordo.</summary>
        public static void ProfiloDima(ParabolaInput p, double rMozzo_m, double altezza_m, double passo_m, out List<double[]> faccia, out List<double[]> dorso)
        {
            double D = p.Diametro_m, a = D / 2.0, F = p.FsuD * D;
            faccia = new List<double[]>(); dorso = new List<double[]>();
            double r0 = Math.Max(0, Math.Min(a - 1e-3, rMozzo_m));
            int n = Math.Max(20, (int)Math.Ceiling((a - r0) / Math.Max(1e-3, passo_m)));
            for (int i = 0; i <= n; i++)
            {
                double rr = r0 + (a - r0) * i / n, zz = rr * rr / (4.0 * F);
                faccia.Add(new[] { rr, zz });
                if (altezza_m > 0)
                {
                    // normale verso l'esterno (lontano dal fuoco): (r/2F, -1) normalizzata
                    double nx = rr / (2.0 * F), ny = -1.0, l = Math.Sqrt(nx * nx + 1.0);
                    dorso.Add(new[] { rr + altezza_m * nx / l, zz + altezza_m * ny / l });
                }
            }
        }

        /// <summary>Dima in DXF (R12, linee e testi): faccia, dorso, tacche degli anelli, dei puntoni e ogni 100 mm di raggio. Unita': mm.</summary>
        public static string DimaDxf(ParabolaInput p, ParabolaResult r, double rMozzo_m, double altezza_m)
        {
            List<double[]> fa, dor;
            ProfiloDima(p, rMozzo_m, altezza_m, 0.005, out fa, out dor);
            CultureInfo ci = CultureInfo.InvariantCulture;
            StringBuilder sb = new StringBuilder();
            Action<string, string> c = (k, v) => { sb.Append(k).Append("\r\n").Append(v).Append("\r\n"); };
            Action<string, double, double, double, double> linea = (lay, x1, y1, x2, y2) =>
            {
                c("0", "LINE"); c("8", lay);
                c("10", (x1 * 1000).ToString("0.###", ci)); c("20", (y1 * 1000).ToString("0.###", ci)); c("30", "0");
                c("11", (x2 * 1000).ToString("0.###", ci)); c("21", (y2 * 1000).ToString("0.###", ci)); c("31", "0");
            };
            Action<string, double, double, double, string> testo = (lay, x, y, h, t) =>
            {
                c("0", "TEXT"); c("8", lay); c("10", (x * 1000).ToString("0.###", ci)); c("20", (y * 1000).ToString("0.###", ci)); c("30", "0");
                c("40", h.ToString("0.###", ci)); c("1", t);
            };
            c("0", "SECTION"); c("2", "ENTITIES");
            for (int i = 1; i < fa.Count; i++) linea("FACCIA", fa[i - 1][0], fa[i - 1][1], fa[i][0], fa[i][1]);
            for (int i = 1; i < dor.Count; i++) linea("DORSO", dor[i - 1][0], dor[i - 1][1], dor[i][0], dor[i][1]);
            if (dor.Count > 0)
            {
                linea("DORSO", fa[0][0], fa[0][1], dor[0][0], dor[0][1]);
                linea("DORSO", fa[fa.Count - 1][0], fa[fa.Count - 1][1], dor[dor.Count - 1][0], dor[dor.Count - 1][1]);
            }
            double F = p.FsuD * p.Diametro_m, a = p.Diametro_m / 2.0;
            Action<string, double, double, string> tacca = (lay, rr, lung, t) =>
            {
                double zz = rr * rr / (4.0 * F), nx = rr / (2.0 * F), l = Math.Sqrt(nx * nx + 1.0);
                // tacca lungo la normale, verso il fuoco (dentro) e verso il dorso
                linea(lay, rr - lung * nx / l, zz + lung / l, rr + lung * nx / l, zz - lung / l);
                if (t != null) testo(lay, rr + 0.004, zz + lung / l + 0.004, 4, t);
            };
            foreach (double ra in r.AnelliRaggio_m) if (ra >= rMozzo_m) tacca("ANELLI", ra, 0.015, string.Format(ci, "anello r={0:0}", ra * 1000));
            double rs = p.PuntoneAttacco_frac * a;
            if (p.NPuntoni > 0 && rs >= rMozzo_m) tacca("PUNTONI", rs, 0.02, string.Format(ci, "puntone r={0:0}", rs * 1000));
            for (double rr = Math.Ceiling(rMozzo_m * 10) / 10.0; rr <= a + 1e-9; rr += 0.1) tacca("RIGHELLO", rr, 0.006, string.Format(ci, "{0:0}", rr * 1000));
            testo("TESTI", rMozzo_m, -0.03, 6, string.Format(ci, "YA-NEC - dima della centina: D {0:0} mm, f/D {1:0.000}, F {2:0.0} mm, centina alta {3:0} mm; unita' mm, scala 1:1", p.Diametro_m * 1000, p.FsuD, F * 1000, altezza_m * 1000));
            c("0", "ENDSEC"); c("0", "EOF");
            return sb.ToString();
        }
    }
}
