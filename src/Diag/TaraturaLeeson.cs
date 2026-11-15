// YA-NEC 0.5.3 - prova B della modifica Leeson A/B (via di IC8TEM il 15/11/2026 21:57): taratura della correzione dei tubi telescopici
// sotto la risonanza, sui valori di EZNEC degli elementi a gradini (fasi 2K e 2L del collaudo).
//
// Perche': nel collaudo 0.5.0-0.5.3 gli elementi molto sotto la risonanza (f/fs sotto 0,9: ARRL, Hy-Gain, XR-5, 2el-30-40) scartano ancora
// l'1-3,5 % da EZNEC anche con la correzione. Il cilindro equivalente di Leeson e' calcolato alla risonanza dell'elemento; lontano da
// essa EZNEC lo corregge in modo un po' diverso. Si prova una correzione a due parametri, nulla alla risonanza e crescente sotto:
//   s = 1 - f/fs;  fattore di lunghezza  k' = 1 - (1 - k)(1 + a s);  diametro  D' = D exp(g s)
// (a = Leeson.TaraLunghezza, g = Leeson.TaraDiametro). Si cerca la coppia che avvicina di piu' YA-NEC a EZNEC, prima su una griglia larga e
// poi stretta attorno alla migliore. Il risultato si scrive in taratura_gradini.txt: i valori scelti entrano nel motore solo dopo
// averli visti (Leeson.cs, valori di partenza 0 = Leeson come prima).
//
// YA-NEC.exe --tara-gradini <uscita>: legge <uscita>\riferimento_gradini.csv, <uscita>\modelli_ez\*.ez e
// <uscita>\risultati_eznec\riepilogo_eznec.csv (fase 2K), scrive <uscita>\taratura_gradini.txt.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using YaNec.Engine;

namespace YaNec
{
    public static class TaraturaLeeson
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        class El { public string Nome; public NecModel M; public double F, Ffs; public Complex Ez; }

        static double Scarto(Complex a, Complex b) { return Complex.Abs(a - b) / Math.Max(1e-9, Complex.Abs(a)) * 100; }

        static double Mediana(List<double> v) { if (v.Count == 0) return double.NaN; List<double> c = new List<double>(v); c.Sort(); return c[c.Count / 2]; }

        class Esito { public double A, G, Media, MedTutti, MedSotto09, Max; public int Oltre05, Oltre2; public double[] Sc; }

        public static int Tara(string uscita)
        {
            if (string.IsNullOrEmpty(uscita)) uscita = Path.Combine(AppPaths.DataDir, "prove", "eznec_gradini");
            string fRif = Path.Combine(uscita, "riferimento_gradini.csv"), fEz = Path.Combine(uscita, "risultati_eznec", "riepilogo_eznec.csv");
            string dirEz = Path.Combine(uscita, "modelli_ez");
            StringBuilder txt = new StringBuilder();
            txt.AppendLine("TARATURA DELLA CORREZIONE DI LEESON SOTTO LA RISONANZA (prova B) - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CI));
            txt.AppendLine("s = 1 - f/fs; lunghezza k' = 1 - (1 - k)(1 + a s); diametro D' = D exp(g s). a = g = 0: Leeson come nel motore (con la modifica A).");
            if (!File.Exists(fRif) || !File.Exists(fEz))
            {
                txt.AppendLine("Mancano " + (File.Exists(fRif) ? "" : fRif + " ") + (File.Exists(fEz) ? "" : fEz + " (EZNEC non e' stato fatto girare)"));
                File.WriteAllText(Path.Combine(uscita, "taratura_gradini.txt"), txt.ToString(), new UTF8Encoding(true));
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
            List<El> els = new List<El>();
            string[] righe = File.ReadAllLines(fRif);
            for (int k = 1; k < righe.Length; k++)
            {
                string[] c = righe[k].TrimStart('﻿').Split(';');
                if (c.Length < 15) continue;
                string[] e;
                double ffs, er, ex;
                if (!ez.TryGetValue(c[0], out e) || e[0] != "0") continue;
                if (!double.TryParse(c[5], NumberStyles.Float, CI, out ffs) || double.IsNaN(ffs) || ffs >= 1) continue;   // sopra la risonanza la taratura non cambia niente
                if (!double.TryParse(e[1], NumberStyles.Float, CI, out er) || !double.TryParse(e[2], NumberStyles.Float, CI, out ex)) continue;
                string fEl = Path.Combine(dirEz, c[0]);
                if (!File.Exists(fEl)) continue;
                try
                {
                    NecModel me = EzImport.LoadEz(fEl);
                    me.Leeson = true;
                    els.Add(new El { Nome = c[0], M = me, F = me.FreqMHz, Ffs = ffs, Ez = new Complex(er, ex) });
                }
                catch (Exception x) { txt.AppendLine("ERRORE " + c[0] + ": " + x.Message); }
            }
            txt.AppendLine(string.Format(CI, "Elementi sotto la risonanza con il valore di EZNEC: {0} (di cui sotto f/fs 0,9: {1}).", els.Count, els.FindAll(q => q.Ffs < 0.9).Count));
            double a0 = Leeson.TaraLunghezza, g0 = Leeson.TaraDiametro, lim0 = Leeson.LimiteRisonanza;
            bool sotto0 = Leeson.SottoSempre;
            List<Esito> tutti = new List<Esito>();
            int giri = 0;
            Func<double, double, Esito> valuta = (a, g) =>
            {
                foreach (Esito q in tutti) if (Math.Abs(q.A - a) < 1e-9 && Math.Abs(q.G - g) < 1e-9) return q;
                Leeson.TaraLunghezza = a; Leeson.TaraDiametro = g; Leeson.SottoSempre = true; Leeson.LimiteRisonanza = lim0;
                Esito r = new Esito { A = a, G = g, Sc = new double[els.Count] };
                List<double> tot = new List<double>(), s09 = new List<double>();
                for (int i = 0; i < els.Count; i++)
                {
                    double d;
                    try { d = Scarto(els[i].Ez, Analysis.Run(els[i].M, els[i].M.BuildGeometry(), els[i].F, 30.0).Zin); }
                    catch { d = 100; }
                    r.Sc[i] = d; tot.Add(d); if (els[i].Ffs < 0.9) s09.Add(d);
                    r.Media += d; r.Max = Math.Max(r.Max, d);
                    if (d > 0.5) r.Oltre05++;
                    if (d > 2) r.Oltre2++;
                }
                r.Media /= Math.Max(1, els.Count); r.MedTutti = Mediana(tot); r.MedSotto09 = Mediana(s09);
                tutti.Add(r);
                giri++;
                Console.Write("\r  taratura Leeson: " + giri + " prove   ");
                return r;
            };
            // obiettivo: scarto medio (pesa anche gli elementi peggiori), a parita' meno elementi oltre il 2 %
            Comparison<Esito> meglio = (p, q) => p.Media != q.Media ? p.Media.CompareTo(q.Media) : p.Oltre2.CompareTo(q.Oltre2);
            Esito base0 = valuta(0, 0);
            Esito best = base0;
            try
            {
                for (double a = -1.0; a <= 1.0001; a += 0.25)
                    for (double g = -1.0; g <= 1.0001; g += 0.5)
                    {
                        Esito r = valuta(Math.Round(a, 4), Math.Round(g, 4));
                        if (meglio(r, best) < 0) best = r;
                    }
                double ca = best.A, cg = best.G;
                for (double a = ca - 0.2; a <= ca + 0.2001; a += 0.05)
                    for (double g = cg - 0.4; g <= cg + 0.4001; g += 0.1)
                    {
                        Esito r = valuta(Math.Round(a, 4), Math.Round(g, 4));
                        if (meglio(r, best) < 0) best = r;
                    }
                // a sola lunghezza e a solo diametro, per vedere quale dei due conta
                Esito soloA = base0, soloG = base0;
                foreach (Esito r in tutti) { if (Math.Abs(r.G) < 1e-9 && meglio(r, soloA) < 0) soloA = r; if (Math.Abs(r.A) < 1e-9 && meglio(r, soloG) < 0) soloG = r; }
                Func<Esito, string> riga = r => string.Format(CI, "a {0,6:0.00}  g {1,6:0.00}  medio {2,6:0.000} %  mediana {3,6:0.000} %  mediana sotto 0,9 {4,6:0.000} %  oltre 0,5 % {5,4}  oltre 2 % {6,3}  massimo {7,6:0.00} %",
                    r.A, r.G, r.Media, r.MedTutti, r.MedSotto09, r.Oltre05, r.Oltre2, r.Max);
                txt.AppendLine();
                txt.AppendLine("Leeson senza taratura:      " + riga(base0));
                txt.AppendLine("Migliore (lunghezza e diametro): " + riga(best));
                txt.AppendLine("Migliore solo lunghezza:    " + riga(soloA));
                txt.AppendLine("Migliore solo diametro:     " + riga(soloG));
                txt.AppendLine(string.Format(CI, "PROPOSTA: Leeson.TaraLunghezza = {0:0.00}; Leeson.TaraDiametro = {1:0.00}", best.A, best.G));
                txt.AppendLine();
                txt.AppendLine("Elementi sotto f/fs 0,9 (scarto da EZNEC senza taratura -> con la migliore):");
                for (int i = 0; i < els.Count; i++)
                    if (els[i].Ffs < 0.9)
                        txt.AppendLine(string.Format(CI, "  {0,-50} f/fs {1,6:0.000}  {2,7:0.00} % -> {3,7:0.00} %", els[i].Nome.Length > 50 ? els[i].Nome.Substring(0, 50) : els[i].Nome, els[i].Ffs, base0.Sc[i], best.Sc[i]));
                int peggio = 0;
                for (int i = 0; i < els.Count; i++) if (best.Sc[i] > base0.Sc[i] + 0.3) peggio++;
                txt.AppendLine(string.Format(CI, "Elementi peggiorati di oltre 0,3 punti con la migliore: {0}", peggio));
                txt.AppendLine();
                txt.AppendLine("Tutte le prove (ordinate dalla migliore):");
                tutti.Sort(meglio);
                foreach (Esito r in tutti) txt.AppendLine("  " + riga(r));
            }
            finally
            {
                Leeson.TaraLunghezza = a0; Leeson.TaraDiametro = g0; Leeson.LimiteRisonanza = lim0; Leeson.SottoSempre = sotto0;
            }
            Console.WriteLine();
            File.WriteAllText(Path.Combine(uscita, "taratura_gradini.txt"), txt.ToString(), new UTF8Encoding(true));
            Registro.Scrivi("CALCOLO", string.Format(CI, "taratura Leeson: {0} elementi, {1} prove, migliore a {2:0.00} g {3:0.00}, scarto medio {4:0.000} -> {5:0.000} %",
                els.Count, giri, best.A, best.G, base0.Media, best.Media));
            return 0;
        }
    }
}
