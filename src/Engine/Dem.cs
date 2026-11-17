// YA-NEC 1.2.0 - Diagrammi > Terreno reale: locatore, ortodromia, modelli digitali del terreno e profilo lungo un azimut.
// Fonti dei dati (scaricati al primo uso nella cartella data/dem del programma, mai in AppData):
//  - TINITALY 1.1, cella 10 m, INGV (S. Tarquini, I. Isola, M. Favalli, A. Battistini, G. Dotta, 2023, doi 10.13127/tinitaly/1.1,
//    licenza CC BY 4.0): servizio OGC WCS 2.0.1 dell'INGV, coverage TINItaly_1_1__tinitaly_dem, ritaglio in latitudine e longitudine;
//  - SRTM1 (NASA / USGS, 1 secondo d'arco, pubblico dominio) dalle mattonelle .hgt.gz del servizio pubblico AWS Terrain Tiles (Mapzen).
// Locatore Maidenhead fino a 12 caratteri (coppie lettere/cifre alternate); distanze e azimut ortodromici sulla sfera di 6371 km.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace YaNec.Engine
{
    public static class Dem
    {
        public const double Rterra = 6371.0;   // km
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        // ------------------------------------------------------------------------------------------------
        // locatore e ortodromia
        // ------------------------------------------------------------------------------------------------
        /// <summary>Centro del quadrato del locatore (4-12 caratteri). false se non valido.</summary>
        public static bool DaLocatore(string loc, out double lat, out double lon)
        {
            lat = lon = 0;
            string l = (loc ?? "").Trim().ToUpperInvariant();
            if (l.Length < 4 || l.Length % 2 != 0 || l.Length > 12) return false;
            if (l[0] < 'A' || l[0] > 'R' || l[1] < 'A' || l[1] > 'R' || !char.IsDigit(l[2]) || !char.IsDigit(l[3])) return false;
            lon = -180 + (l[0] - 'A') * 20; lat = -90 + (l[1] - 'A') * 10;
            lon += (l[2] - '0') * 2; lat += l[3] - '0';
            double w = 2, h = 1;
            for (int i = 4; i < l.Length; i += 2)
            {
                bool lettere = (i / 2) % 2 == 0;
                if (lettere)
                {
                    if (l[i] < 'A' || l[i] > 'X' || l[i + 1] < 'A' || l[i + 1] > 'X') return false;
                    w /= 24; h /= 24; lon += (l[i] - 'A') * w; lat += (l[i + 1] - 'A') * h;
                }
                else
                {
                    if (!char.IsDigit(l[i]) || !char.IsDigit(l[i + 1])) return false;
                    w /= 10; h /= 10; lon += (l[i] - '0') * w; lat += (l[i + 1] - '0') * h;
                }
            }
            lat += h / 2; lon += w / 2;
            return true;
        }

        /// <summary>Locatore di n caratteri (4-12) del punto.</summary>
        public static string Locatore(double lat, double lon, int n = 10)
        {
            double x = lon + 180, y = lat + 90;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int a = (int)(x / 20), b = (int)(y / 10);
            sb.Append((char)('A' + Math.Min(17, a))); sb.Append((char)('A' + Math.Min(17, b)));
            x -= a * 20; y -= b * 10;
            double w = 20, h = 10;
            for (int i = 2; i < n; i += 2)
            {
                bool cifre = (i / 2) % 2 == 1;
                int div = cifre ? 10 : 24;
                w /= div; h /= div;
                int p = Math.Min(div - 1, (int)(x / w)), q = Math.Min(div - 1, (int)(y / h));
                x -= p * w; y -= q * h;
                if (cifre) { sb.Append((char)('0' + p)); sb.Append((char)('0' + q)); }
                else { sb.Append((char)((i == 4 ? 'a' : 'a') + p)); sb.Append((char)('a' + q)); }
            }
            string s = sb.ToString();
            return s.Substring(0, 4) + s.Substring(4).ToLowerInvariant();
        }

        /// <summary>Lati del quadrato del locatore di n caratteri alla latitudine (km, est-ovest e nord-sud).</summary>
        public static void LatoQuadrato(int n, double lat, out double ew, out double ns)
        {
            double w = 20, h = 10;
            for (int i = 2; i < n; i += 2) { int d = (i / 2) % 2 == 1 ? 10 : 24; w /= d; h /= d; }
            double k = Math.PI / 180 * Rterra;
            ew = w * k * Math.Cos(lat * Math.PI / 180); ns = h * k;
        }

        public static void Rotta(double lat1, double lon1, double lat2, double lon2, out double azimut, out double km)
        {
            double p1 = lat1 * Math.PI / 180, p2 = lat2 * Math.PI / 180, dl = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin((p2 - p1) / 2), b = Math.Sin(dl / 2);
            km = 2 * Rterra * Math.Asin(Math.Sqrt(a * a + Math.Cos(p1) * Math.Cos(p2) * b * b));
            azimut = (Math.Atan2(Math.Sin(dl) * Math.Cos(p2), Math.Cos(p1) * Math.Sin(p2) - Math.Sin(p1) * Math.Cos(p2) * Math.Cos(dl)) * 180 / Math.PI + 360) % 360;
        }

        public static void Destinazione(double lat, double lon, double azimut, double km, out double lat2, out double lon2)
        {
            double d = km / Rterra, t = azimut * Math.PI / 180, p1 = lat * Math.PI / 180, l1 = lon * Math.PI / 180;
            double p2 = Math.Asin(Math.Sin(p1) * Math.Cos(d) + Math.Cos(p1) * Math.Sin(d) * Math.Cos(t));
            double l2 = l1 + Math.Atan2(Math.Sin(t) * Math.Sin(d) * Math.Cos(p1), Math.Cos(d) - Math.Sin(p1) * Math.Sin(p2));
            lat2 = p2 * 180 / Math.PI; lon2 = ((l2 * 180 / Math.PI) + 540) % 360 - 180;
        }

        public class Direzione { public string Nome; public double Lat, Lon; }
        /// <summary>Punti di riferimento delle direzioni di traffico del bozzetto 1 (modificabili nel file data/dem/direzioni.txt).</summary>
        public static readonly Direzione[] DirezioniTraffico =
        {
            new Direzione { Nome = "USA costa est", Lat = 40.7, Lon = -74.0 },
            new Direzione { Nome = "USA costa ovest", Lat = 37.8, Lon = -122.4 },
            new Direzione { Nome = "Giappone", Lat = 35.7, Lon = 139.7 },
            new Direzione { Nome = "VK4", Lat = -27.5, Lon = 153.0 },
            new Direzione { Nome = "PY5", Lat = -25.4, Lon = -49.3 },
            new Direzione { Nome = "Caraibi", Lat = 18.2, Lon = -66.5 },
        };

        // ------------------------------------------------------------------------------------------------
        // griglie del terreno
        // ------------------------------------------------------------------------------------------------
        public class Griglia
        {
            public int Nx, Ny; public float[] Z; public double Lon0, Lat0, DLon, DLat; public float NoData = -9999; public string Fonte;
            /// <summary>Quota (m) interpolata; NaN fuori o senza dato.</summary>
            public double Quota(double lat, double lon)
            {
                double fx = (lon - Lon0) / DLon - 0.5, fy = (lat - Lat0) / DLat - 0.5;
                int ix = (int)Math.Floor(fx), iy = (int)Math.Floor(fy);
                if (ix < 0 || iy < 0 || ix + 1 >= Nx || iy + 1 >= Ny) return double.NaN;
                double u = fx - ix, v = fy - iy;
                float a = Z[iy * Nx + ix], b = Z[iy * Nx + ix + 1], c = Z[(iy + 1) * Nx + ix], d = Z[(iy + 1) * Nx + ix + 1];
                if (a <= NoData + 1 || b <= NoData + 1 || c <= NoData + 1 || d <= NoData + 1) return double.NaN;
                return (a * (1 - u) + b * u) * (1 - v) + (c * (1 - u) + d * u) * v;
            }
        }

        static readonly HttpClient Http = CreaHttp();
        static HttpClient CreaHttp()
        {
            HttpClient h = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            h.DefaultRequestHeaders.UserAgent.ParseAdd("YA-NEC/1.2 (+https://github.com/ic8tem/YA-NEC)");
            return h;
        }
        public static string Cartella { get; set; }

        static string Dir(string sotto)
        {
            string d = Path.Combine(Cartella ?? "data", "dem", sotto);
            Directory.CreateDirectory(d);
            return d;
        }

        /// <summary>Mattonella SRTM1 (1 grado) che contiene il punto: dalla cartella, altrimenti scaricata.</summary>
        public static async Task<Griglia> Srtm1(double lat, double lon, CancellationToken ct)
        {
            int la = (int)Math.Floor(lat), lo = (int)Math.Floor(lon);
            string nome = (la >= 0 ? "N" : "S") + Math.Abs(la).ToString("00") + (lo >= 0 ? "E" : "W") + Math.Abs(lo).ToString("000");
            string f = Path.Combine(Dir("srtm1"), nome + ".hgt");
            if (!File.Exists(f))
            {
                string url = "https://elevation-tiles-prod.s3.amazonaws.com/skadi/" + nome.Substring(0, 3) + "/" + nome + ".hgt.gz";
                byte[] gz = await Http.GetByteArrayAsync(url, ct);
                using (GZipStream z = new GZipStream(new MemoryStream(gz), CompressionMode.Decompress))
                using (FileStream o = File.Create(f + ".tmp")) z.CopyTo(o);
                File.Move(f + ".tmp", f, true);
            }
            return LeggiHgt(f, la, lo);
        }

        public static Griglia LeggiHgt(string f, int la, int lo)
        {
            byte[] b = File.ReadAllBytes(f);
            int n = (int)Math.Round(Math.Sqrt(b.Length / 2));
            Griglia g = new Griglia { Nx = n, Ny = n, Z = new float[n * n], Fonte = "SRTM1" };
            double d = 1.0 / (n - 1);
            g.DLon = d; g.DLat = d; g.Lon0 = lo - d / 2; g.Lat0 = la - d / 2;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    short v = (short)((b[2 * (r * n + c)] << 8) | b[2 * (r * n + c) + 1]);
                    // le righe del file vanno da nord a sud: la griglia va da sud a nord
                    g.Z[(n - 1 - r) * n + c] = v == -32768 ? g.NoData : v;
                }
            return g;
        }

        /// <summary>Ritaglio TINITALY 1.1 intorno al punto (mezzo lato in km): dalla cartella, altrimenti dal servizio WCS dell'INGV.</summary>
        public static async Task<Griglia> Tinitaly(double lat, double lon, double raggioKm, CancellationToken ct)
        {
            double dla = raggioKm / 111.2, dlo = raggioKm / (111.2 * Math.Cos(lat * Math.PI / 180));
            // ritaglio allineato a 0,05 gradi per riusare i file
            double la0 = Math.Floor((lat - dla) * 20) / 20, la1 = Math.Ceiling((lat + dla) * 20) / 20;
            double lo0 = Math.Floor((lon - dlo) * 20) / 20, lo1 = Math.Ceiling((lon + dlo) * 20) / 20;
            if (la1 < 35.17 || la0 > 47.16 || lo1 < 6.52 || lo0 > 19.72) return null;   // fuori dall'Italia
            string nome = string.Format(CI, "tinitaly_{0:0.00}_{1:0.00}_{2:0.00}_{3:0.00}.tif", la0, la1, lo0, lo1);
            string f = Path.Combine(Dir("tinitaly"), nome);
            if (!File.Exists(f))
            {
                string url = string.Format(CI, "https://tinitaly.pi.ingv.it/TINItaly_1_1/wcs?service=WCS&version=2.0.1&request=GetCoverage&coverageId=TINItaly_1_1__tinitaly_dem" +
                    "&subsettingCrs=http://www.opengis.net/def/crs/EPSG/0/4326&subset=Lat({0:0.00},{1:0.00})&subset=Long({2:0.00},{3:0.00})&format=image/tiff", la0, la1, lo0, lo1);
                byte[] b = await Http.GetByteArrayAsync(url, ct);
                if (b.Length < 8 || !((b[0] == 'I' && b[1] == 'I') || (b[0] == 'M' && b[1] == 'M'))) throw new IOException("TINITALY: il servizio non ha restituito un GeoTIFF");
                File.WriteAllBytes(f + ".tmp", b);
                File.Move(f + ".tmp", f, true);
            }
            Griglia g = LeggiGeoTiff(File.ReadAllBytes(f));
            g.Fonte = "TINITALY 1.1";
            return g;
        }

        /// <summary>GeoTIFF float32 o int16 non compresso (a strisce o a mattonelle), coordinate geografiche (ModelTransformation o tiepoint + scala).</summary>
        public static Griglia LeggiGeoTiff(byte[] b)
        {
            bool le = b[0] == 'I';
            Func<int, int> U16 = o => le ? b[o] | (b[o + 1] << 8) : (b[o] << 8) | b[o + 1];
            Func<int, uint> U32 = o => le ? (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24)) : (uint)((b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]);
            Func<int, double> F64 = o => { byte[] t = new byte[8]; Array.Copy(b, o, t, 0, 8); if (!le) Array.Reverse(t); return BitConverter.ToDouble(t, 0); };
            int ifd = (int)U32(4), nt = U16(ifd);
            Dictionary<int, long[]> tag = new Dictionary<int, long[]>();
            Dictionary<int, double[]> tagD = new Dictionary<int, double[]>();
            string nodata = null;
            for (int i = 0; i < nt; i++)
            {
                int e = ifd + 2 + 12 * i, id = U16(e), ty = U16(e + 2); int cnt = (int)U32(e + 4);
                int sz = ty == 3 ? 2 : ty == 4 ? 4 : ty == 12 ? 8 : ty == 2 ? 1 : ty == 16 ? 8 : 4;
                int off = cnt * sz <= 4 ? e + 8 : (int)U32(e + 8);
                if (ty == 12) { double[] v = new double[cnt]; for (int k = 0; k < cnt; k++) v[k] = F64(off + 8 * k); tagD[id] = v; }
                else if (ty == 2) nodata = System.Text.Encoding.ASCII.GetString(b, off, cnt).TrimEnd('\0');
                else { long[] v = new long[cnt]; for (int k = 0; k < cnt; k++) v[k] = ty == 3 ? U16(off + 2 * k) : U32(off + 4 * k); tag[id] = v; }
            }
            int w = (int)tag[256][0], h = (int)tag[257][0], bits = (int)tag[258][0];
            if (tag.ContainsKey(259) && tag[259][0] != 1) throw new IOException("GeoTIFF compresso: non supportato");
            int fmt = tag.ContainsKey(339) ? (int)tag[339][0] : 1;
            Griglia g = new Griglia { Nx = w, Ny = h, Z = new float[w * h] };
            if (nodata != null) { float nd; if (float.TryParse(nodata, NumberStyles.Float, CI, out nd)) g.NoData = nd; }
            Func<int, float> Val = o =>
            {
                if (bits == 32 && fmt == 3) { byte[] t = { b[o], b[o + 1], b[o + 2], b[o + 3] }; if (!le) Array.Reverse(t); return BitConverter.ToSingle(t, 0); }
                if (bits == 16) { int v = U16(o); return fmt == 2 ? (short)v : v; }
                if (bits == 32) return (int)U32(o);
                throw new IOException("GeoTIFF: tipo di dato non supportato");
            };
            int bp = bits / 8;
            // righe del file dall'alto (nord): la griglia va da sud
            Action<int, int, int> Put = (r, c, o) => { if (r < h && c < w) g.Z[(h - 1 - r) * w + c] = Val(o); };
            if (tag.ContainsKey(322))
            {
                int tw = (int)tag[322][0], th = (int)tag[323][0];
                long[] offs = tag[324];
                int tx = (w + tw - 1) / tw;
                for (int t = 0; t < offs.Length; t++)
                {
                    int r0 = (t / tx) * th, c0 = (t % tx) * tw;
                    for (int r = 0; r < th; r++) for (int c = 0; c < tw; c++) Put(r0 + r, c0 + c, (int)offs[t] + (r * tw + c) * bp);
                }
            }
            else
            {
                long[] offs = tag[273]; int rps = tag.ContainsKey(278) ? (int)tag[278][0] : h;
                for (int s = 0; s < offs.Length; s++)
                    for (int r = 0; r < rps; r++) for (int c = 0; c < w; c++) Put(s * rps + r, c, (int)offs[s] + (r * w + c) * bp);
            }
            double sx, sy, x0, y0;
            if (tagD.ContainsKey(34264)) { double[] m = tagD[34264]; sx = m[0]; x0 = m[3]; sy = -m[5]; y0 = m[7]; }
            else { double[] tp = tagD[33922], sc = tagD[33550]; sx = sc[0]; sy = sc[1]; x0 = tp[3] - tp[0] * sx; y0 = tp[4] + tp[1] * sy; }
            g.DLon = sx; g.DLat = sy; g.Lon0 = x0; g.Lat0 = y0 - h * sy;
            return g;
        }

        // ------------------------------------------------------------------------------------------------
        // profilo
        // ------------------------------------------------------------------------------------------------
        public class Profilo
        {
            public double[] D, H; public bool[] Mare; public string Fonte = ""; public double QuotaBase;
        }

        /// <summary>Profilo lungo l'azimut fino a dmax km, passo in m: prima TINITALY, poi SRTM1; senza dato o a quota 0 sul bordo = mare.</summary>
        public static Profilo Campiona(double lat, double lon, double azimut, double dmaxKm, double passoM, IList<Griglia> griglie)
        {
            int n = Math.Max(2, (int)(dmaxKm * 1000 / passoM) + 1);
            Profilo p = new Profilo { D = new double[n], H = new double[n], Mare = new bool[n] };
            HashSet<string> fonti = new HashSet<string>();
            for (int i = 0; i < n; i++)
            {
                double d = i * passoM;
                double la, lo; Destinazione(lat, lon, azimut, d / 1000, out la, out lo);
                double q = double.NaN;
                foreach (Griglia g in griglie) { if (g == null) continue; q = g.Quota(la, lo); if (!double.IsNaN(q)) { fonti.Add(g.Fonte); break; } }
                p.D[i] = d;
                if (double.IsNaN(q) || q <= 0.5) { p.H[i] = 0; p.Mare[i] = true; }
                else p.H[i] = q;
            }
            p.QuotaBase = p.H[0];
            p.Fonte = string.Join(" + ", fonti);
            return p;
        }

        /// <summary>Profilo da file: CSV di HorizonProfiler o .hpr (colonne distanza [m o km] e quota [m]; righe di intestazione ignorate).</summary>
        public static Profilo DaFile(string f)
        {
            List<double> d = new List<double>(), h = new List<double>();
            foreach (string r0 in File.ReadAllLines(f))
            {
                string r = r0.Trim(); if (r.Length == 0 || r.StartsWith("#")) continue;
                string[] c = r.Split(new[] { ';', ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (c.Length < 2) continue;
                double a, b;
                if (!double.TryParse(c[0], NumberStyles.Float, CI, out a) || !double.TryParse(c[1], NumberStyles.Float, CI, out b)) continue;
                d.Add(a); h.Add(b);
            }
            if (d.Count < 2) throw new IOException("Il file non contiene coppie distanza / quota.");
            bool km = d[d.Count - 1] < 200;   // distanze in km se l'ultima e' piccola
            Profilo p = new Profilo { D = new double[d.Count], H = h.ToArray(), Mare = new bool[d.Count], Fonte = Path.GetFileName(f) };
            for (int i = 0; i < d.Count; i++) { p.D[i] = km ? d[i] * 1000 : d[i]; p.Mare[i] = p.H[i] <= 0.5; }
            p.QuotaBase = p.H[0];
            return p;
        }
    }
}
