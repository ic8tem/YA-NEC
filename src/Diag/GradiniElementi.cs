// YA-NEC 0.4.30 - correzione dei diametri a gradini elemento per elemento contro EZNEC (proposta del 14/11/2026 21:51, approvata da
// IC8TEM alle 21:52: "Gradini, seconda parte: mettere elemento per elemento la correzione di YA-NEC accanto a quella di EZNEC (Stepped
// Diameter Correction) sui 112 modelli rimasti. Poi applicare le stesse regole di EZNEC, cioe' quali elementi corregge e quali salta").
//
// Perche' elemento per elemento: nel collaudo 0.4.23 la correzione di Leeson all'importazione ha portato lo scarto mediano delle Yagi a
// gradini dal 52,8 % al 2,7 %, ma 112 modelli restano oltre il 2 %. Sul modello intero non si vede quale elemento sbaglia. EZNEC non
// mostra l'elemento che sostituisce, quindi ogni elemento a gradini si prova da solo: si toglie dal modello, si mette in spazio libero con
// una sorgente al centro e lo si fa calcolare a EZNEC (con la sua correzione, come nel file d'origine) e a YA-NEC in tre modi:
//   L  = YA-NEC com'e' (Leeson entro il 15 % dalla risonanza);
//   F  = Leeson forzato anche lontano dalla risonanza;
//   0  = senza correzione (NEC-2 puro).
// Il verdetto dice quale dei tre e' piu' vicino a EZNEC: "come YA-NEC", "EZNEC corregge anche lontano dalla risonanza", "EZNEC non
// corregge" oppure "EZNEC corregge diversamente" (allora lo scarto, in funzione di f/fs e del numero dei tubi, serve a tarare la formula).
//
// YA-NEC.exe --prepara-gradini <archivio> <uscita>: scrive <uscita>\modelli_ez\*.ez (un elemento per file, testata del .ez d'origine
// conservata, cosi' la correzione di EZNEC resta quella del modello) e <uscita>\riferimento_gradini.csv con i valori di YA-NEC.
// YA-NEC.exe --confronta-gradini <uscita>: legge <uscita>\risultati_eznec\riepilogo_eznec.csv (fase 2K di compila.bat) e scrive
// confronto_gradini.txt e confronto_gradini.csv.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using YaNec.Engine;

namespace YaNec
{
    public static class GradiniElementi
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        const int Testa = 0xBA, Riga = 170, Gap = 324;

        static void I16(byte[] d, int o, int v) { BitConverter.GetBytes((short)v).CopyTo(d, o); }
        static void I32(byte[] d, int o, int v) { BitConverter.GetBytes(v).CopyTo(d, o); }
        static void F32(byte[] d, int o, double v) { BitConverter.GetBytes((float)v).CopyTo(d, o); }

        static string Pulito(string s)
        {
            StringBuilder b = new StringBuilder();
            char[] no = Path.GetInvalidFileNameChars();
            foreach (char c in s) b.Append(Array.IndexOf(no, c) >= 0 || c == '\'' || c == '\\' || c == '/' ? '_' : c);
            return b.ToString().Trim();
        }

        static string Z(Complex z) { return z.Real.ToString("0.###", CI) + ";" + z.Imaginary.ToString("0.###", CI); }

        public static int Prepara(string archivio, string uscita)
        {
            if (string.IsNullOrEmpty(archivio)) archivio = Path.Combine(AppPaths.DataDir, "prove", "eznec_archivio");
            if (string.IsNullOrEmpty(uscita)) uscita = Path.Combine(AppPaths.DataDir, "prove", "eznec_gradini");
            string dirArc = Path.Combine(archivio, "modelli_ez"), dirEz = Path.Combine(uscita, "modelli_ez");
            try { if (Directory.Exists(dirEz)) Directory.Delete(dirEz, true); } catch { }
            Directory.CreateDirectory(dirEz);
            StringBuilder csv = new StringBuilder(), txt = new StringBuilder();
            csv.AppendLine("file;modello;fili;MHz;risonanza MHz;f/fs;tubi per lato;diametro equivalente mm;fattore di lunghezza;YA-NEC L R;YA-NEC L X;YA-NEC F R;YA-NEC F X;YA-NEC 0 R;YA-NEC 0 X;note");
            txt.AppendLine("ELEMENTI A GRADINI DA SOLI PER EZNEC - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CI));
            if (!Directory.Exists(dirArc)) { txt.AppendLine("ERRORE: manca " + dirArc); File.WriteAllText(Path.Combine(uscita, "preparazione_gradini.txt"), txt.ToString(), new UTF8Encoding(true)); return 1; }
            HashSet<string> visti = new HashSet<string>();
            int nMod = 0, nEl = 0, nDoppi = 0, nErr = 0;
            double limite = Leeson.LimiteRisonanza;
            List<string> files = new List<string>(Directory.GetFiles(dirArc, "*.ez", SearchOption.AllDirectories));
            files.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string f in files)
            {
                string rel = f.Substring(dirArc.Length).TrimStart('\\', '/').Replace('/', '\\');
                try
                {
                    byte[] b = File.ReadAllBytes(f);
                    if (b.Length < Testa + Riga || b[0x40] != 5) continue;
                    NecModel m = EzImport.ParseEz(b);
                    List<Leeson.Catena> cs = Leeson.Trova(m);
                    bool uno = false;
                    int nr0 = BitConverter.ToInt16(b, 0);
                    int p0 = Testa + Riga * nr0;
                    if (p0 + Gap > b.Length) continue;
                    foreach (Leeson.Catena c in cs)
                    {
                        // solo gli elementi che restano tali anche da soli (i motivi legati al resto del modello non hanno senso qui)
                        if (c.Motivo != null && c.Motivo != "elemento non simmetrico") continue;
                        if (c.Monopolo) continue;   // il monopolo a terra non si puo' provare in spazio libero
                        if (c.Lato.Count < 2) continue;
                        StringBuilder chiave = new StringBuilder(m.FreqMHz.ToString("0.####", CI));
                        foreach (double[] l in c.Lato) chiave.Append("|" + l[0].ToString("0.####", CI) + "/" + l[1].ToString("0.#####", CI));
                        if (c.Motivo != null) chiave.Append("|asim" + rel);
                        if (!visti.Add(chiave.ToString())) { nDoppi++; continue; }
                        uno = true;
                        // fili dell'elemento, in ordine lungo l'asse
                        List<GeomCard> cards = new List<GeomCard>();
                        foreach (int i in c.Schede) cards.Add(m.Geometry[i]);
                        Func<double, double, double, double> T = (x, y, z) => (x - c.Cx) * c.Ux + (y - c.Cy) * c.Uy + (z - c.Cz) * c.Uz;
                        cards.Sort((p, q) => Math.Min(T(p.F1, p.F2, p.F3), T(p.F4, p.F5, p.F6)).CompareTo(Math.Min(T(q.F1, q.F2, q.F3), T(q.F4, q.F5, q.F6))));
                        int nw = cards.Count;
                        byte[] d = new byte[Testa + Riga * nw + Gap + (b.Length - p0 - Gap)];
                        Array.Copy(b, d, Testa);
                        int src = 0; double pct = 50;
                        double migliore = double.MaxValue;
                        for (int j = 0; j < nw; j++)
                        {
                            GeomCard g = cards[j];
                            Array.Copy(b, Testa + Riga * (g.I1 - 1), d, Testa + Riga * j, 0x20);
                            double t1 = T(g.F1, g.F2, g.F3), t2 = T(g.F4, g.F5, g.F6);
                            if (Math.Min(t1, t2) < -1e-9 && Math.Max(t1, t2) > 1e-9) { src = j; pct = (0 - t1) / (t2 - t1) * 100; migliore = 0; }
                            else if (migliore > 0)
                            {
                                // il centro sta su una giunzione tra due fili: la sorgente va sul segmento del filo che parte dal centro
                                double a = Math.Abs(t1) < Math.Abs(t2) ? t1 : t2;
                                if (Math.Abs(a) < migliore) { migliore = Math.Abs(a); src = j; pct = Math.Abs(t1) < Math.Abs(t2) ? 0.01 : 99.99; }
                            }
                        }
                        int rs = Testa;
                        I16(d, rs + 0x20, src + 1); F32(d, rs + 0x22, pct); F32(d, rs + 0x26, 1); F32(d, rs + 0x2A, 0); d[rs + 0x2E] = (byte)'V';
                        Array.Copy(b, p0, d, Testa + Riga * nw, b.Length - p0);
                        I16(d, 0x00, nw); I16(d, 0x02, 0); I16(d, 0x30, nw); I16(d, 0x32, 1); I16(d, 0x34, 0); I16(d, 0x78, 0);
                        I32(d, 0x7C, nw); I32(d, 0x80, nw); I32(d, 0x9C, 0);
                        d[0x36] = (byte)'F';
                        nEl++;
                        string nomeMod = Path.GetFileNameWithoutExtension(f);
                        string fili = cards[0].I1 + (nw > 1 ? "-" + cards[nw - 1].I1 : "");
                        string titolo = (nEl.ToString("0000", CI) + " " + nomeMod + " " + fili + new string(' ', 30)).Substring(0, 30);
                        Encoding.GetEncoding("ISO-8859-1").GetBytes(titolo).CopyTo(d, 0x12);
                        string nome = Pulito(nEl.ToString("0000", CI) + " " + nomeMod + " fili " + fili) + ".ez";
                        string fEl = Path.Combine(dirEz, nome);
                        File.WriteAllBytes(fEl, d);
                        // YA-NEC sullo stesso .ez riletto, nei tre modi
                        NecModel me = EzImport.LoadEz(fEl);
                        double fr = me.FreqMHz;
                        List<Leeson.Catena> ce = Leeson.Trova(me);
                        double fs = ce.Count > 0 ? Leeson.Risonanza(ce[0].Lato) : double.NaN, l1, d1;
                        Leeson.Equivalente(c.Lato, out l1, out d1);
                        me.Leeson = true;
                        Leeson.LimiteRisonanza = limite;
                        Complex zL = Analysis.Run(me, me.BuildGeometry(), fr, 30.0).Zin;
                        Leeson.LimiteRisonanza = 1e9;
                        Complex zF = Analysis.Run(me, me.BuildGeometry(), fr, 30.0).Zin;
                        Leeson.LimiteRisonanza = limite;
                        me.Leeson = false;
                        Complex z0 = Analysis.Run(me, me.BuildGeometry(), fr, 30.0).Zin;
                        csv.AppendLine(string.Join(";", nome, rel, fili, fr.ToString("0.####", CI), fs.ToString("0.###", CI), (fr / fs).ToString("0.####", CI),
                            c.Lato.Count.ToString(CI), (d1 * 1000).ToString("0.###", CI), (l1 / c.Lato[c.Lato.Count - 1][0]).ToString("0.#####", CI), Z(zL), Z(zF), Z(z0), c.Motivo ?? ""));
                    }
                    if (uno) nMod++;
                }
                catch (Exception e) { nErr++; txt.AppendLine("ERRORE " + rel + ": " + e.Message); Leeson.LimiteRisonanza = limite; }
                Console.Write("\r  " + nMod + " modelli, " + nEl + " elementi   ");
            }
            Console.WriteLine();
            txt.AppendLine(string.Format(CI, "Modelli con elementi a gradini: {0}; elementi scritti: {1}; elementi uguali saltati: {2}; errori: {3}", nMod, nEl, nDoppi, nErr));
            File.WriteAllText(Path.Combine(uscita, "riferimento_gradini.csv"), csv.ToString(), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(uscita, "preparazione_gradini.txt"), txt.ToString(), new UTF8Encoding(true));
            Console.WriteLine(txt.ToString());
            Registro.Scrivi("CALCOLO", string.Format(CI, "elementi a gradini per EZNEC: {0} modelli, {1} elementi, {2} errori", nMod, nEl, nErr));
            return nErr;
        }

        static double Scarto(Complex a, Complex b) { return Complex.Abs(a - b) / Math.Max(1e-9, Complex.Abs(a)) * 100; }

        public static int Confronta(string uscita)
        {
            if (string.IsNullOrEmpty(uscita)) uscita = Path.Combine(AppPaths.DataDir, "prove", "eznec_gradini");
            string fRif = Path.Combine(uscita, "riferimento_gradini.csv"), fEz = Path.Combine(uscita, "risultati_eznec", "riepilogo_eznec.csv");
            StringBuilder txt = new StringBuilder(), csv = new StringBuilder();
            txt.AppendLine("ELEMENTI A GRADINI: EZNEC (Stepped Diameter Correction) CONTRO YA-NEC - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CI));
            txt.AppendLine("L = YA-NEC com'e' (Leeson entro il 15 % dalla risonanza), F = Leeson forzato, 0 = senza correzione. Scarti sull'impedenza.");
            if (!File.Exists(fRif) || !File.Exists(fEz))
            {
                txt.AppendLine("Mancano " + (File.Exists(fRif) ? "" : fRif + " ") + (File.Exists(fEz) ? "" : fEz + " (EZNEC non e' stato fatto girare)"));
                File.WriteAllText(Path.Combine(uscita, "confronto_gradini.txt"), txt.ToString(), new UTF8Encoding(true));
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
            csv.AppendLine("file;modello;fili;f/fs;tubi;EZNEC R;EZNEC X;scarto L %;scarto F %;scarto 0 %;verdetto;dR L ohm;dX L ohm");
            int n = 0, nCome = 0, nLontano = 0, nNo = 0, nDiv = 0, nFermi = 0;
            // scarti di "corregge diversamente" per fasce di f/fs (sotto 0,9 / 0,9-1,1 / sopra 1,1)
            List<double>[] fasce = { new List<double>(), new List<double>(), new List<double>() };
            Dictionary<string, int[]> perModello = new Dictionary<string, int[]>();
            string[] righe = File.ReadAllLines(fRif);
            for (int k = 1; k < righe.Length; k++)
            {
                string[] c = righe[k].TrimStart('﻿').Split(';');
                if (c.Length < 15) continue;
                string[] e;
                if (!ez.TryGetValue(c[0], out e)) continue;
                n++;
                if (e[0] != "0") { nFermi++; csv.AppendLine(string.Join(";", c[0], c[1], c[2], c[5], c[6], "", "", "", "", "", "EZNEC non lo calcola (codice " + e[0] + ")", "", "")); continue; }
                double er, ex;
                if (!double.TryParse(e[1], NumberStyles.Float, CI, out er) || !double.TryParse(e[2], NumberStyles.Float, CI, out ex)) continue;
                Func<int, Complex> zc = i => new Complex(double.Parse(c[i], CI), double.Parse(c[i + 1], CI));
                Complex ze = new Complex(er, ex), zL = zc(9), zF = zc(11), z0 = zc(13);
                double dL = Scarto(ze, zL), dF = Scarto(ze, zF), d0 = Scarto(ze, z0), ffs;
                double.TryParse(c[5], NumberStyles.Float, CI, out ffs);
                string v;
                if (dL <= 0.5) { v = "come YA-NEC"; nCome++; }
                else if (dF <= 0.5 && dF < d0) { v = "EZNEC corregge anche lontano dalla risonanza"; nLontano++; }
                else if (d0 <= 0.5 && d0 < dF) { v = "EZNEC non corregge"; nNo++; }
                else
                {
                    v = "EZNEC corregge diversamente"; nDiv++;
                    fasce[ffs < 0.9 ? 0 : ffs <= 1.1 ? 1 : 2].Add(Math.Min(dL, dF));
                }
                int[] pm;
                if (!perModello.TryGetValue(c[1], out pm)) perModello[c[1]] = pm = new int[2];
                pm[0]++; if (dL > 0.5) pm[1]++;
                csv.AppendLine(string.Join(";", c[0], c[1], c[2], c[5], c[6], er.ToString("0.###", CI), ex.ToString("0.###", CI), dL.ToString("0.###", CI), dF.ToString("0.###", CI), d0.ToString("0.###", CI), v,
                    (zL.Real - er).ToString("0.###", CI), (zL.Imaginary - ex).ToString("0.###", CI)));
                txt.AppendLine(string.Format(CI, "{0,-46} f/fs {1,6:0.000}  L {2,7:0.00} %  F {3,7:0.00} %  0 {4,7:0.00} %  {5}", c[0].Length > 46 ? c[0].Substring(0, 46) : c[0], ffs, dL, dF, d0, v));
            }
            txt.AppendLine();
            txt.AppendLine(string.Format(CI, "Elementi: {0}; come YA-NEC (entro 0,5 %): {1}; EZNEC corregge anche lontano dalla risonanza: {2}; EZNEC non corregge: {3}; corregge diversamente: {4}; non calcolati da EZNEC: {5}",
                n, nCome, nLontano, nNo, nDiv, nFermi));
            string[] nf = { "f/fs sotto 0,9", "f/fs 0,9-1,1", "f/fs sopra 1,1" };
            for (int i = 0; i < 3; i++)
            {
                if (fasce[i].Count == 0) continue;
                fasce[i].Sort();
                txt.AppendLine(string.Format(CI, "  corregge diversamente, {0}: {1} elementi, scarto mediano {2:0.00} %, massimo {3:0.00} %", nf[i], fasce[i].Count, fasce[i][fasce[i].Count / 2], fasce[i][fasce[i].Count - 1]));
            }
            txt.AppendLine();
            txt.AppendLine("Modelli con elementi oltre lo 0,5 % (elementi oltre / elementi provati):");
            foreach (KeyValuePair<string, int[]> kv in perModello) if (kv.Value[1] > 0) txt.AppendLine("  " + kv.Value[1] + " / " + kv.Value[0] + "  " + kv.Key);
            File.WriteAllText(Path.Combine(uscita, "confronto_gradini.txt"), txt.ToString(), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(uscita, "confronto_gradini.csv"), csv.ToString(), new UTF8Encoding(true));
            Console.WriteLine(string.Format(CI, "Elementi a gradini: {0}; come YA-NEC {1}; lontano dalla risonanza {2}; EZNEC non corregge {3}; diversamente {4}", n, nCome, nLontano, nNo, nDiv));
            Registro.Scrivi("CALCOLO", string.Format(CI, "elementi a gradini contro EZNEC: {0}; come YA-NEC {1}; lontano {2}; non corregge {3}; diversamente {4}", n, nCome, nLontano, nNo, nDiv));
            return 0;
        }
    }
}
