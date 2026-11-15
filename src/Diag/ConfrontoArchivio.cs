// YA-NEC 0.4.20 - confronto con EZNEC dei modelli d'archivio che ancora si discostano (richiesta di IC8TEM del 14/11/2026 18:54:
// "lo scaricamento sia del programma aggiornato, sia dei soli modelli che hanno ancora discostamenti da eznec oltre lo 0,1%").
// YA-NEC.exe --confronta-archivio <cartella>: nella cartella ci sono modelli_ez\... (i file .ez delle prove con EZNEC) e
// riferimento_EZNEC.csv (file;EZNEC R;EZNEC X;...: i valori di EZNEC Pro/2+ 7.0 delle prove 6 e 7 di IC8TEM). Ogni .ez si legge con il
// lettore di YA-NEC (terreno MININEC come GN 3, diametri a gradini con Leeson come EZNEC) e si calcola alla sua frequenza; lo scarto
// sull'impedenza della prima sorgente va in confronto_archivio.txt e confronto_archivio.csv, con le classi 0,1 / 0,5 / 2 %.
// 0.4.22 (richiesta di IC8TEM del 14/11/2026 20:32, "tutto in un unico bat"): la cartella e' data\prove\eznec_archivio e compila.bat
// fa calcolare prima gli stessi .ez a EZNEC (risultati_eznec\riepilogo_eznec.csv): se ci sono, si confronta con i valori di EZNEC appena
// calcolati, altrimenti con riferimento_EZNEC.csv. I modelli che EZNEC non calcola (Geometry Check) non hanno impedenza: si controlla
// che anche YA-NEC li segnali (contatti fuori giunzione, le sei regole di EZNEC).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using YaNec.Engine;

namespace YaNec
{
    public static class ConfrontoArchivio
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        public static int Esegui(string cartella)
        {
            if (string.IsNullOrEmpty(cartella)) cartella = Path.Combine(AppPaths.DataDir, "prove", "eznec_archivio");
            string fRif = Path.Combine(cartella, "riferimento_EZNEC.csv"), dirEz = Path.Combine(cartella, "modelli_ez");
            StringBuilder txt = new StringBuilder(), csv = new StringBuilder();
            txt.AppendLine("CONFRONTO YA-NEC / EZNEC DEI MODELLI D'ARCHIVIO OLTRE LO 0,1 % - YA-NEC " + AppPaths.Version + " - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CI));
            txt.AppendLine("Riferimento: " + fRif);
            csv.AppendLine("file;EZNEC R;EZNEC X;YA-NEC R;YA-NEC X;scarto %;classe;secondi;note");
            if (!File.Exists(fRif))
            {
                txt.AppendLine("ERRORE: manca " + fRif);
                File.WriteAllText(Path.Combine(cartella, "confronto_archivio.txt"), txt.ToString(), new UTF8Encoding(true));
                Console.WriteLine(txt.ToString());
                return 1;
            }
            int n = 0, n01 = 0, n05 = 0, n2 = 0, nOltre = 0, nErr = 0, nFermi = 0, nFermiNo = 0;
            string[] righe = File.ReadAllLines(fRif);
            // risultati freschi di EZNEC (fase 2I di compila.bat): file -> (calcolo, R, X)
            Dictionary<string, string[]> fresco = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            string fEz = Path.Combine(cartella, "risultati_eznec", "riepilogo_eznec.csv");
            if (File.Exists(fEz))
            {
                bool testa = true;
                foreach (string l in File.ReadAllLines(fEz))
                {
                    if (testa) { testa = false; continue; }
                    string[] c = l.TrimStart('\uFEFF').Split(';');
                    if (c.Length < 8) continue;
                    if (c[5] != "1" && c[5] != "") continue;
                    if (!fresco.ContainsKey(c[0])) fresco[c[0]] = new[] { c[2], c[6], c[7] };
                }
                txt.AppendLine("Valori di EZNEC: calcolati ora da compila.bat (" + fresco.Count + " modelli, " + fEz + ")");
            }
            else txt.AppendLine("Valori di EZNEC: quelli della prova 6 (riferimento_EZNEC.csv); EZNEC non e' stato fatto girare ora");
            for (int k = 1; k < righe.Length; k++)
            {
                string[] c = righe[k].TrimStart('﻿').Split(';');
                if (c.Length < 3 || c[0].Length == 0) continue;
                double er, ex;
                // 0.4.22: i modelli corretti ("(corretto 0.4.22)") non hanno ancora un valore di EZNEC della prova 6: si usa quello appena calcolato
                bool haRif = double.TryParse(c[1], NumberStyles.Float, CI, out er) & double.TryParse(c[2], NumberStyles.Float, CI, out ex);
                if (!haRif && !fresco.ContainsKey(c[0])) continue;
                string f = Path.Combine(dirEz, c[0].Replace('\\', Path.DirectorySeparatorChar));
                n++;
                string[] fr;
                if (fresco.TryGetValue(c[0], out fr))
                {
                    if (fr[0] != "0")
                    {
                        // EZNEC non lo calcola (Geometry Check): YA-NEC deve segnalarlo con le stesse regole
                        try
                        {
                            NecModel mg = EzImport.LoadEz(f);
                            SegmentGeometry gg = mg.BuildGeometry();
                            List<string> ct = TerrenoVicino.ContattiFuoriGiunzione(gg);
                            // 0.4.30: anche i fili dentro il terreno o distesi sul terreno (EZNEC codice 1)
                            ct.AddRange(TerrenoVicino.FiliNelTerreno(gg, mg.Ground));
                            bool ok = ct.Count > 0;
                            if (ok) nFermi++; else nFermiNo++;
                            csv.AppendLine(string.Join(";", c[0], "", "", "", "", "", ok ? "EZNEC lo ferma, YA-NEC lo segnala" : "EZNEC lo ferma, YA-NEC NO", "", (ct.Count > 0 ? ct[0] : "").Replace(';', ',')));
                            txt.AppendLine((ok ? "FERMATO   " : "AVVISO    ") + c[0] + ": EZNEC non lo calcola (codice " + fr[0] + "); YA-NEC: " + (ok ? ct[0] : "nessun contatto segnalato"));
                        }
                        catch (Exception e) { nErr++; txt.AppendLine("ERRORE    " + c[0] + ": " + e.Message); }
                        continue;
                    }
                    double r2, x2;
                    if (double.TryParse(fr[1], NumberStyles.Float, CI, out r2) && double.TryParse(fr[2], NumberStyles.Float, CI, out x2)) { er = r2; ex = x2; haRif = true; }
                }
                if (!haRif) { n--; continue; }
                DateTime t0 = DateTime.Now;
                try
                {
                    NecModel m = EzImport.LoadEz(f);
                    SegmentGeometry g = m.BuildGeometry();
                    RunResult r = Analysis.Run(m, g, m.FreqMHz, 10.0);
                    Complex ze = new Complex(er, ex), zy = r.Zin;
                    double d = Complex.Abs(ze - zy) / Math.Max(1e-9, Complex.Abs(zy)) * 100.0;
                    string cl = d <= 0.1 ? "entro 0,1 %" : d <= 0.5 ? "0,1-0,5 %" : d <= 2 ? "0,5-2 %" : "oltre 2 %";
                    if (d <= 0.1) n01++; else if (d <= 0.5) n05++; else if (d <= 2) n2++; else nOltre++;
                    string note = string.Join(" | ", m.Warnings);
                    double sec = (DateTime.Now - t0).TotalSeconds;
                    csv.AppendLine(string.Join(";", c[0], er.ToString("0.###", CI), ex.ToString("0.###", CI), zy.Real.ToString("0.###", CI), zy.Imaginary.ToString("0.###", CI), d.ToString("0.###", CI), cl, sec.ToString("0.0", CI), note.Replace(';', ',')));
                    txt.AppendLine(string.Format(CI, "{0,-9} {1,8:0.###} %  {2}  (EZNEC {3:0.###} {4:+0.###;-0.###}j, YA-NEC {5:0.###} {6:+0.###;-0.###}j)", cl.Replace(" ", ""), d, c[0], er, ex, zy.Real, zy.Imaginary));
                }
                catch (Exception e)
                {
                    nErr++;
                    csv.AppendLine(c[0] + ";" + c[1] + ";" + c[2] + ";;;;errore;;" + e.Message.Replace(';', ','));
                    txt.AppendLine("ERRORE    " + c[0] + ": " + e.Message);
                }
                Console.Write("\r  " + k + " / " + (righe.Length - 1) + "   ");
            }
            Console.WriteLine();
            txt.AppendLine();
            txt.AppendLine(string.Format(CI, "Modelli: {0}; entro 0,1 %: {1}; 0,1-0,5 %: {2}; 0,5-2 %: {3}; oltre 2 %: {4}; fermati da EZNEC e segnalati da YA-NEC: {5}, non segnalati: {6}; errori: {7}", n, n01, n05, n2, nOltre, nFermi, nFermiNo, nErr));
            File.WriteAllText(Path.Combine(cartella, "confronto_archivio.txt"), txt.ToString(), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(cartella, "confronto_archivio.csv"), csv.ToString(), new UTF8Encoding(true));
            Console.WriteLine(string.Format(CI, "Confronto d'archivio: {0} modelli, entro 0,1 % {1}, oltre {2}, errori {3}. Resoconto in {4}", n, n01, n - n01 - nErr, nErr, Path.Combine(cartella, "confronto_archivio.txt")));
            Registro.Scrivi("CALCOLO", string.Format(CI, "confronto d'archivio con EZNEC: {0} modelli, entro 0,1 % {1}, 0,1-0,5 % {2}, 0,5-2 % {3}, oltre 2 % {4}, errori {5}", n, n01, n05, n2, nOltre, nErr));
            return nErr + nFermiNo;
        }
    }
}
