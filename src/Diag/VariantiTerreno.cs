// YA-NEC 0.4.30 - fasi 2M e 2N di compila.bat: varianti dei 7 modelli vicini al terreno contro EZNEC (proposta del 14/11/2026 23:11,
// approvata da IC8TEM alle 23:13: "ok approvato"). Su ElevatRad, ELEVRAD1, ELEVRAD2, GndScreen, wa2wvl e i due 80hsbeam YA-NEC da' gli
// stessi valori di nec2c ma EZNEC (terreno "High Accuracy", Sommerfeld) si scosta del 2-6 %. Ogni modello va a EZNEC in quattro varianti:
//   A = originale;
//   B = fili alzati: ogni estremo con 0 < z < lambda/100 portato a lambda/100 (gli estremi uguali restano uguali, le giunzioni restano);
//   C = segmenti raddoppiati su tutti i fili (sorgenti e carichi sono in percentuale del filo, restano al loro posto);
//   D = terreno reale "MININEC" al posto di "High Accuracy" (byte 0x7B del .ez).
// YA-NEC calcola le stesse quattro varianti rileggendo i .ez scritti. Il verdetto dice quale ipotesi spiega lo scarto di A:
// la quota (B porta EZNEC e YA-NEC d'accordo), la segmentazione (C) o il calcolo del terreno di Sommerfeld (D d'accordo, A no).
// YA-NEC.exe --prepara-varianti [archivio] [uscita]; YA-NEC.exe --confronta-varianti [uscita] (legge risultati_eznec\riepilogo_eznec.csv).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using YaNec.Engine;

namespace YaNec
{
    public static class VariantiTerreno
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        const int Testa = 0xBA, Riga = 170;

        public static readonly string[] Modelli =
        {
            @"4nec2 (corpus)\4nec2\models\HFvertical\ElevatRad.ez",
            @"4nec2 (corpus)\4nec2\models\zz_EZnec\v3.0\ELEVRAD1.ez",
            @"4nec2 (corpus)\4nec2\models\zz_EZnec\v3.0\ELEVRAD2.ez",
            @"4nec2 (corpus)\4nec2\models\HFvertical\GndScreen.ez",
            @"4nec2 (corpus)\4nec2\models\HFbeams\wa2wvl.ez",
            @"4nec2 (corpus)\4nec2\models\HFbeams\80hsbeam.ez",
            @"4nec2 (corpus)\4nec2\models\HFvertical\80HSBEAM.ez",
        };
        static readonly string[] NomiVar = { "A originale", "B fili alzati a lambda/100", "C segmenti raddoppiati", "D terreno MININEC" };

        static string Z(Complex z) { return z.Real.ToString("0.###", CI) + ";" + z.Imaginary.ToString("0.###", CI); }

        /// <summary>Variante k del .ez d'origine; null se non si applica (B senza fili sotto lambda/100, D senza terreno reale). nota = cosa e' cambiato.</summary>
        static byte[] Variante(byte[] b, int k, out string nota)
        {
            nota = "";
            byte[] d = (byte[])b.Clone();
            int nw = BitConverter.ToInt16(d, 0x30);
            double f = BitConverter.ToSingle(d, 5), lam = 299.792458 / f, h = lam / 100;
            if (k == 1)
            {
                int n = 0;
                for (int w = 0; w < nw; w++)
                    foreach (int o in new[] { 8, 20 })
                    {
                        int p = Testa + Riga * w + o;
                        double z = BitConverter.ToSingle(d, p);
                        if (z > 0 && z < h) { BitConverter.GetBytes((float)h).CopyTo(d, p); n++; }
                    }
                if (n == 0) { nota = "nessun estremo sotto lambda/100 (" + h.ToString("0.###", CI) + " m): variante non applicabile"; return null; }
                nota = n + " estremi portati a " + h.ToString("0.###", CI) + " m";
                // 0.4.36 (collaudo 0.4.34, 2M: ElevatRad_B e ELEVRAD2_B "segmento 1 di lunghezza nulla"): un filo con tutti e due gli estremi
                // sotto lambda/100 diventerebbe lungo zero. In quel caso si alza tutta l'antenna (gli estremi con z > 0) dello stesso tratto,
                // cosi' il piu' basso arriva a lambda/100; gli estremi a z = 0 (collegati al terreno) restano dove sono.
                bool nullo = false;
                for (int w = 0; w < nw && !nullo; w++)
                {
                    int p = Testa + Riga * w;
                    double dx = BitConverter.ToSingle(d, p + 12) - BitConverter.ToSingle(d, p + 0), dy = BitConverter.ToSingle(d, p + 16) - BitConverter.ToSingle(d, p + 4), dz = BitConverter.ToSingle(d, p + 20) - BitConverter.ToSingle(d, p + 8);
                    if (Math.Sqrt(dx * dx + dy * dy + dz * dz) < 1e-5) nullo = true;
                }
                if (nullo)
                {
                    d = (byte[])b.Clone();
                    double zmin = double.MaxValue;
                    for (int w = 0; w < nw; w++)
                        foreach (int o in new[] { 8, 20 }) { double z = BitConverter.ToSingle(d, Testa + Riga * w + o); if (z > 0 && z < zmin) zmin = z; }
                    double sale = h - zmin;
                    for (int w = 0; w < nw; w++)
                        foreach (int o in new[] { 8, 20 })
                        {
                            int p = Testa + Riga * w + o;
                            double z = BitConverter.ToSingle(d, p);
                            if (z > 0) BitConverter.GetBytes((float)(z + sale)).CopyTo(d, p);
                        }
                    nota = "antenna alzata di " + sale.ToString("0.####", CI) + " m (un filo tutto sotto lambda/100 sarebbe diventato lungo zero): estremo piu' basso a " + h.ToString("0.###", CI) + " m";
                }
            }
            else if (k == 2)
            {
                int tot = 0;
                for (int w = 0; w < nw; w++)
                {
                    int p = Testa + Riga * w + 30;
                    int ns = BitConverter.ToInt16(d, p) * 2;
                    BitConverter.GetBytes((short)ns).CopyTo(d, p);
                    tot += ns;
                }
                nota = tot + " segmenti";
            }
            else if (k == 3)
            {
                if (d[0x36] != (byte)'R') { nota = "terreno non reale: variante non applicabile"; return null; }
                if (d[0x7B] == (byte)'M') nota = "gia' MININEC";
                d[0x7B] = (byte)'M';
                nota = nota.Length > 0 ? nota : "High Accuracy -> MININEC";
            }
            else nota = "come il file d'archivio";
            return d;
        }

        public static int Prepara(string archivio, string uscita)
        {
            if (string.IsNullOrEmpty(archivio)) archivio = Path.Combine(AppPaths.DataDir, "prove", "eznec_archivio");
            if (string.IsNullOrEmpty(uscita)) uscita = Path.Combine(AppPaths.DataDir, "prove", "eznec_varianti");
            string dirArc = Path.Combine(archivio, "modelli_ez"), dirEz = Path.Combine(uscita, "modelli_ez");
            try { if (Directory.Exists(dirEz)) Directory.Delete(dirEz, true); } catch { }
            Directory.CreateDirectory(dirEz);
            StringBuilder csv = new StringBuilder(), txt = new StringBuilder();
            csv.AppendLine("file;modello;variante;MHz;YA-NEC R;YA-NEC X;nota");
            txt.AppendLine("VARIANTI DEI 7 MODELLI VICINI AL TERRENO PER EZNEC - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CI));
            txt.AppendLine("A originale, B estremi sotto lambda/100 alzati a lambda/100, C segmenti raddoppiati, D terreno MININEC al posto di High Accuracy.");
            int nErr = 0, nFile = 0;
            double cv = Nec2Engine.Cvel;
            Nec2Engine.Cvel = Nec2Engine.CVEL_FISICA;   // come EZNEC (299,792458)
            try
            {
                foreach (string rel in Modelli)
                {
                    string f = Path.Combine(dirArc, rel.Replace('\\', Path.DirectorySeparatorChar));
                    string nomeMod = Path.GetFileNameWithoutExtension(f) + (rel.Contains("HFvertical") && rel.EndsWith("80HSBEAM.ez") ? "_vert" : "");
                    if (!File.Exists(f)) { nErr++; txt.AppendLine("MANCA " + rel); continue; }
                    byte[] b = File.ReadAllBytes(f);
                    for (int k = 0; k < 4; k++)
                    {
                        string nota;
                        byte[] d;
                        try { d = Variante(b, k, out nota); }
                        catch (Exception ex) { nErr++; txt.AppendLine("ERRORE " + nomeMod + " " + NomiVar[k] + ": " + ex.Message); continue; }
                        if (d == null) { txt.AppendLine(string.Format(CI, "{0,-14} {1,-28} {2}", nomeMod, NomiVar[k], nota)); csv.AppendLine(string.Join(";", "", rel, NomiVar[k], "", "", "", nota)); continue; }
                        string nome = nomeMod + "_" + "ABCD"[k] + ".ez";
                        string fv = Path.Combine(dirEz, nome);
                        File.WriteAllBytes(fv, d);
                        try
                        {
                            NecModel m = EzImport.LoadEz(fv);
                            Complex z = Analysis.Run(m, m.BuildGeometry(), m.FreqMHz, 30.0).Zin;
                            csv.AppendLine(string.Join(";", nome, rel, NomiVar[k], m.FreqMHz.ToString("0.####", CI), Z(z), nota));
                            txt.AppendLine(string.Format(CI, "{0,-14} {1,-28} YA-NEC {2,9:0.00} {3,9:+0.00;-0.00}j  {4}", nomeMod, NomiVar[k], z.Real, z.Imaginary, nota));
                            nFile++;
                        }
                        catch (Exception ex) { nErr++; txt.AppendLine("ERRORE YA-NEC " + nome + ": " + ex.Message); csv.AppendLine(string.Join(";", nome, rel, NomiVar[k], "", "", "", "errore YA-NEC: " + ex.Message)); }
                    }
                }
            }
            finally { Nec2Engine.Cvel = cv; }
            txt.AppendLine(string.Format(CI, "File scritti per EZNEC: {0}; errori: {1}", nFile, nErr));
            File.WriteAllText(Path.Combine(uscita, "riferimento_varianti.csv"), csv.ToString(), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(uscita, "preparazione_varianti.txt"), txt.ToString(), new UTF8Encoding(true));
            Console.WriteLine(txt.ToString());
            Registro.Scrivi("CALCOLO", string.Format(CI, "varianti vicino al terreno per EZNEC: {0} file, {1} errori", nFile, nErr));
            return nErr;
        }

        static double Scarto(Complex a, Complex b) { return Complex.Abs(a - b) / Math.Max(1e-9, Complex.Abs(a)) * 100; }

        public static int Confronta(string uscita)
        {
            if (string.IsNullOrEmpty(uscita)) uscita = Path.Combine(AppPaths.DataDir, "prove", "eznec_varianti");
            string fRif = Path.Combine(uscita, "riferimento_varianti.csv"), fEz = Path.Combine(uscita, "risultati_eznec", "riepilogo_eznec.csv");
            StringBuilder txt = new StringBuilder(), csv = new StringBuilder();
            txt.AppendLine("VARIANTI VICINO AL TERRENO: EZNEC CONTRO YA-NEC - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CI));
            txt.AppendLine("Scarto = |Z EZNEC - Z YA-NEC| / |Z EZNEC|. Variazione = quanto cambia Z passando da A alla variante, in EZNEC e in YA-NEC.");
            if (!File.Exists(fRif) || !File.Exists(fEz))
            {
                txt.AppendLine("Mancano " + (File.Exists(fRif) ? "" : fRif + " ") + (File.Exists(fEz) ? "" : fEz + " (EZNEC non e' stato fatto girare)"));
                File.WriteAllText(Path.Combine(uscita, "confronto_varianti.txt"), txt.ToString(), new UTF8Encoding(true));
                Console.WriteLine(txt.ToString());
                return File.Exists(fRif) ? 0 : 1;
            }
            Dictionary<string, string[]> ez = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            bool testa = true;
            foreach (string l in File.ReadAllLines(fEz))
            {
                if (testa) { testa = false; continue; }
                string[] c = l.TrimStart('﻿').Split(';');
                if (c.Length < 8 || ez.ContainsKey(c[0])) continue;
                ez[c[0]] = new[] { c[2], c[6], c[7] };
            }
            // modello -> variante (0..3) -> {Z EZNEC, Z YA-NEC}
            Dictionary<string, Complex?[,]> tab = new Dictionary<string, Complex?[,]>();
            List<string> ordine = new List<string>();
            string[] righe = File.ReadAllLines(fRif);
            for (int k = 1; k < righe.Length; k++)
            {
                string[] c = righe[k].TrimStart('﻿').Split(';');
                if (c.Length < 7 || c[0].Length == 0 || c[4].Length == 0) continue;
                int v = Array.IndexOf(NomiVar, c[2]);
                if (v < 0) continue;
                string mod = c[0].Substring(0, c[0].Length - 5);
                Complex?[,] t;
                if (!tab.TryGetValue(mod, out t)) { tab[mod] = t = new Complex?[4, 2]; ordine.Add(mod); }
                t[v, 1] = new Complex(double.Parse(c[4], CI), double.Parse(c[5], CI));
                string[] e;
                double er, ex;
                if (ez.TryGetValue(c[0], out e) && e[0] == "0" && double.TryParse(e[1], NumberStyles.Float, CI, out er) && double.TryParse(e[2], NumberStyles.Float, CI, out ex))
                    t[v, 0] = new Complex(er, ex);
            }
            csv.AppendLine("modello;variante;EZNEC R;EZNEC X;YA-NEC R;YA-NEC X;scarto %;variazione EZNEC %;variazione YA-NEC %");
            int nQuota = 0, nSeg = 0, nTerr = 0, nAltro = 0;
            foreach (string mod in ordine)
            {
                Complex?[,] t = tab[mod];
                txt.AppendLine();
                txt.AppendLine(mod);
                double[] sc = new double[4];
                for (int v = 0; v < 4; v++)
                {
                    sc[v] = double.NaN;
                    if (t[v, 1] == null) { txt.AppendLine("  " + NomiVar[v].PadRight(28) + " non applicabile"); continue; }
                    if (t[v, 0] == null) { txt.AppendLine("  " + NomiVar[v].PadRight(28) + " EZNEC non l'ha calcolata (YA-NEC " + Z(t[v, 1].Value) + ")"); continue; }
                    Complex ze = t[v, 0].Value, zy = t[v, 1].Value;
                    sc[v] = Scarto(ze, zy);
                    double ve = v > 0 && t[0, 0] != null ? Scarto(t[0, 0].Value, ze) : double.NaN, vy = v > 0 && t[0, 1] != null ? Scarto(t[0, 1].Value, zy) : double.NaN;
                    txt.AppendLine(string.Format(CI, "  {0,-28} EZNEC {1,9:0.00} {2,9:+0.00;-0.00}j  YA-NEC {3,9:0.00} {4,9:+0.00;-0.00}j  scarto {5,6:0.00} %{6}", NomiVar[v], ze.Real, ze.Imaginary, zy.Real, zy.Imaginary, sc[v],
                        v > 0 ? string.Format(CI, "  (da A: EZNEC {0:0.0} %, YA-NEC {1:0.0} %)", ve, vy) : ""));
                    csv.AppendLine(string.Join(";", mod, NomiVar[v], ze.Real.ToString("0.###", CI), ze.Imaginary.ToString("0.###", CI), zy.Real.ToString("0.###", CI), zy.Imaginary.ToString("0.###", CI),
                        sc[v].ToString("0.###", CI), double.IsNaN(ve) ? "" : ve.ToString("0.###", CI), double.IsNaN(vy) ? "" : vy.ToString("0.###", CI)));
                }
                string verdetto;
                if (double.IsNaN(sc[0])) verdetto = "originale non calcolato da EZNEC: nessun verdetto";
                else if (sc[0] <= 1) verdetto = "originale gia' entro l'1 %: niente da spiegare";
                else
                {
                    List<string> p = new List<string>();
                    if (!double.IsNaN(sc[1]) && sc[1] <= 1) { p.Add("QUOTA: con i fili a lambda/100 EZNEC e YA-NEC concordano, lo scarto viene dai fili vicinissimi al terreno"); nQuota++; }
                    if (!double.IsNaN(sc[2]) && sc[2] <= sc[0] / 2) { p.Add("SEGMENTAZIONE: raddoppiando i segmenti lo scarto almeno si dimezza"); nSeg++; }
                    if (!double.IsNaN(sc[3]) && sc[3] <= 1) { p.Add("TERRENO: con MININEC concordano, la differenza e' nel calcolo di Sommerfeld"); nTerr++; }
                    if (p.Count == 0) { p.Add("nessuna delle tre ipotesi spiega lo scarto: serve un'altra variante"); nAltro++; }
                    verdetto = string.Join("; ", p);
                }
                txt.AppendLine("  verdetto: " + verdetto);
            }
            txt.AppendLine();
            txt.AppendLine(string.Format(CI, "Modelli: {0}; spiegati dalla quota: {1}; dalla segmentazione: {2}; dal terreno: {3}; non spiegati: {4}", ordine.Count, nQuota, nSeg, nTerr, nAltro));
            File.WriteAllText(Path.Combine(uscita, "confronto_varianti.txt"), txt.ToString(), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(uscita, "confronto_varianti.csv"), csv.ToString(), new UTF8Encoding(true));
            Console.WriteLine(txt.ToString());
            Registro.Scrivi("CALCOLO", string.Format(CI, "varianti vicino al terreno contro EZNEC: {0} modelli; quota {1}, segmenti {2}, terreno {3}, non spiegati {4}", ordine.Count, nQuota, nSeg, nTerr, nAltro));
            return 0;
        }
    }
}
