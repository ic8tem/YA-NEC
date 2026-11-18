// YA-NEC 1.5 - prova d'importazione dei modelli (richiesta di IC8TEM del 18/11/2026 19:33, eseguita alle 22:03):
// YA-NEC --converti <cartella dei modelli> <cartella di uscita>
// Ogni .weq, .ez, .maa, .yag, .ant, .nec della cartella si legge con gli stessi lettori dell'interfaccia, si riscrive come .nec
// (quello che YA-NEC ha capito del file) e si calcola alla sua frequenza; il riepilogo va in converti.csv (senza finestra).
using System;
using System.Globalization;
using System.IO;
using System.Text;
using YaNec.Engine;

namespace YaNec
{
    public static class ConvertiModelli
    {
        public static int Esegui(string dirIn, string dirOut)
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            Directory.CreateDirectory(dirOut);
            StringBuilder csv = new StringBuilder("file;esito;formato;schede;segmenti;sorgenti;carichi;MHz;terreno;R;X;Gmax dBi;avvisi;errore\n");
            foreach (string f in Directory.GetFiles(dirIn))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant(), nome = Path.GetFileName(f);
                if (ext != ".weq" && ext != ".ez" && ext != ".maa" && ext != ".yag" && ext != ".ant" && ext != ".nec") continue;
                try
                {
                    NecModel m;
                    if (ext == ".maa") { System.Collections.Generic.List<MmanaWireInfo> w; m = MmanaImport.Load(f, 40, out w); }
                    else if (ext == ".weq") m = EzImport.LoadWeq(f);
                    else if (ext == ".ez") m = EzImport.LoadEz(f);
                    else if (ext == ".yag") m = K6stiImport.LoadYag(f);
                    else if (ext == ".ant") m = K6stiImport.LoadAnt(f);
                    else m = NecModel.Load(f);
                    File.WriteAllText(Path.Combine(dirOut, Path.GetFileNameWithoutExtension(f) + ".nec"), FormatiExport.NecPerAltri(m, new System.Collections.Generic.List<string>()));
                    SegmentGeometry g = m.BuildGeometry();
                    RunResult r = Analysis.Run(m, g, m.FreqMHz, 5.0);
                    csv.AppendLine(string.Join(";", nome, "ok", m.SourceFormat, m.Geometry.Count.ToString(ci), g.N.ToString(ci), m.Sources.Count.ToString(ci), m.Loads.Count.ToString(ci),
                        m.FreqMHz.ToString("0.####", ci), m.Ground.Kind.ToString(), r.Zin.Real.ToString("0.###", ci), r.Zin.Imaginary.ToString("0.###", ci), r.GmaxDbi.ToString("0.##", ci),
                        string.Join(" | ", m.Warnings).Replace(";", ","), ""));
                }
                catch (Exception ex) { csv.AppendLine(nome + ";errore;;;;;;;;;;;;" + ex.GetBaseException().Message.Replace(";", ",").Replace("\n", " ")); }
                Console.WriteLine(nome);
            }
            File.WriteAllText(Path.Combine(dirOut, "converti.csv"), csv.ToString(), new UTF8Encoding(true));
            return 0;
        }
    }
}
