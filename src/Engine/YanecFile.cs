// YA-NEC 0.4.30 - formato .yanec 2 (IC8TEM, 15/11/2026 07:31: "applica gia' le scelte, cancella i modelli che ti ho fatto scartare.
// quelli rimanenti modifica il formato verso il nuovo yanec"; 14/11/2026: formato proprio con tutti i modelli, testo cambiabile
// ma mantenendo l'abaco della descrizione).
//
// Un .yanec e' un file di testo UTF-8 con una testata fissa (l'abaco della descrizione) e il modello:
//
//   CM YA-NEC FORMATO=yanec 2
//   CM YANEC NOME=<nome del modello>
//   CM YANEC DESCRIZIONE=<descrizione libera>
//   CM YANEC CATEGORIA=<cartella o famiglia>
//   CM YANEC FONTE=<autore, libro o programma di origine>
//   CM YANEC ORIGINALE=<nome del file d'origine con l'estensione>
//   CM YANEC CONTENUTO=nec | maa | weq | ez | yag | ant
//   CM YANEC NOTA=<riga di nota>            (zero o piu' righe)
//   <modello>
//
// Il modello resta nel formato d'origine, senza conversioni: schede NEC per "nec" (il file si apre anche con 4nec2 e nec2c,
// le righe CM sono commenti), testo del file MMANA / AutoEZ / YO / AO per "maa", "weq", "yag", "ant", file EZNEC (binario)
// in base64 a righe di 76 caratteri per "ez". Cosi' YA-NEC lo legge con lo stesso importatore e gli stessi risultati del file
// d'origine (il collaudo confronta gli stessi numeri). I valori dell'abaco si possono cambiare; le chiavi no.
// La versione 1 (solo "CM YA-NEC FORMATO=yanec 1" + schede NEC, dalla 0.4.0) si legge ancora.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YaNec.Engine
{
    public static class YanecFile
    {
        public const string Firma = "CM YA-NEC FORMATO=yanec";
        public const string Prefisso = "CM YANEC ";
        public static readonly string[] Chiavi = { "NOME", "DESCRIZIONE", "CATEGORIA", "FONTE", "ORIGINALE", "CONTENUTO", "NOTA" };

        public static string Etichetta(string contenuto)
        {
            switch ((contenuto ?? "").TrimStart('.').ToLowerInvariant())
            {
                case "maa": return "MMANA";
                case "weq": return "AutoEZ";
                case "ez": return "EZNEC";
                case "yag": return "YO";
                case "ant": return "AO";
                default: return "NEC";
            }
        }

        // vero se il file comincia con la firma del formato
        public static bool E(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                using (StreamReader r = new StreamReader(path, new UTF8Encoding(false), true))
                {
                    string l = r.ReadLine();
                    return l != null && l.TrimStart('﻿').StartsWith(Firma, StringComparison.Ordinal);
                }
            }
            catch { return false; }
        }

        public static bool Esiste(string path) { return File.Exists(path) || Sostituto(path) != null; }

        // Il .yanec da leggere al posto di "path": il file stesso se e' un .yanec, altrimenti (file mancante perche' convertito nella 0.4.30)
        // "<nome>.yanec" o "<nome> (<formato>).yanec" nella stessa cartella con il contenuto dell'estensione chiesta. null: si legge il file com'e'.
        public static string Sostituto(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (File.Exists(path)) return E(path) ? path : null;
            try
            {
                string dir = Path.GetDirectoryName(path) ?? "", nome = Path.GetFileNameWithoutExtension(path), ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                if (ext == "yanec") return null;
                string a = Path.Combine(dir, nome + ".yanec"), b = Path.Combine(dir, nome + " (" + Etichetta(ext) + ").yanec");
                if (E(a) && Contenuto(a) == ext) return a;
                if (E(b) && Contenuto(b) == ext) return b;
                if (E(a)) return a;
                return null;
            }
            catch { return null; }
        }

        public static string Contenuto(string path)
        {
            foreach (KeyValuePair<string, string> kv in Testata(path, out _)) if (kv.Key == "CONTENUTO") return kv.Value.ToLowerInvariant();
            return "nec";
        }

        // abaco della descrizione (chiavi nell'ordine del file) e righe del modello
        public static List<KeyValuePair<string, string>> Testata(string path, out List<string> corpo)
        {
            return Dividi(File.ReadAllLines(path, new UTF8Encoding(false)), out corpo, out _);
        }

        static List<KeyValuePair<string, string>> Dividi(string[] righe, out List<string> corpo, out int versione)
        {
            List<KeyValuePair<string, string>> t = new List<KeyValuePair<string, string>>();
            corpo = new List<string>();
            versione = 1;
            int i = 0;
            if (righe.Length > 0)
            {
                string r0 = righe[0].TrimStart('﻿');
                if (r0.StartsWith(Firma, StringComparison.Ordinal))
                {
                    int v;
                    if (int.TryParse(r0.Substring(Firma.Length).Trim(), out v)) versione = v;
                    i = 1;
                }
            }
            for (; i < righe.Length; i++)
            {
                string r = righe[i];
                if (!r.StartsWith(Prefisso, StringComparison.Ordinal)) break;
                string kv = r.Substring(Prefisso.Length);
                int eq = kv.IndexOf('=');
                if (eq <= 0) continue;
                t.Add(new KeyValuePair<string, string>(kv.Substring(0, eq).Trim().ToUpperInvariant(), kv.Substring(eq + 1).Trim()));
            }
            for (; i < righe.Length; i++) corpo.Add(righe[i]);
            return t;
        }

        public static NecModel Carica(string path)
        {
            List<MmanaWireInfo> w;
            return Carica(path, 40, out w);
        }

        public static NecModel Carica(string path, double segPerLambda, out List<MmanaWireInfo> wires)
        {
            wires = null;
            string[] righe = File.ReadAllLines(path, new UTF8Encoding(false));
            List<string> corpo;
            int versione;
            List<KeyValuePair<string, string>> t = Dividi(righe, out corpo, out versione);
            string cont = "nec", orig = Path.GetFileNameWithoutExtension(path);
            foreach (KeyValuePair<string, string> kv in t)
            {
                if (kv.Key == "CONTENUTO") cont = kv.Value.ToLowerInvariant();
                if (kv.Key == "ORIGINALE" && kv.Value.Length > 0) orig = Path.GetFileNameWithoutExtension(kv.Value);
            }
            NecModel m;
            switch (cont)
            {
                case "maa": m = MmanaImport.Parse(corpo.ToArray(), segPerLambda, out wires); break;
                case "weq": m = EzImport.ParseWeq(corpo.ToArray()); break;
                case "yag": m = K6stiImport.ParseYag(corpo.ToArray(), orig); break;
                case "ant": m = K6stiImport.ParseAnt(corpo.ToArray()); break;
                case "ez":
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (string r in corpo) sb.Append(r.Trim());
                        byte[] b;
                        try { b = Convert.FromBase64String(sb.ToString()); }
                        catch (FormatException) { throw new NecException("File .yanec: il modello EZNEC (base64) e' danneggiato."); }
                        m = EzImport.ParseEz(b);
                        break;
                    }
                case "nec":
                    // tutto il testo: le righe dell'abaco sono CM e il lettore NEC le mette nella scheda del modello
                    m = NecModel.Parse(string.Join("\n", righe));
                    break;
                default: throw new NecException("File .yanec: contenuto \"" + cont + "\" sconosciuto (attesi nec, maa, weq, ez, yag, ant).");
            }
            m.Scheda = new List<KeyValuePair<string, string>>(t);
            m.FileName = path;
            return m;
        }

        // testata del .yanec di un modello: l'abaco del modello (se letto da un .yanec) con NOME e CONTENUTO aggiornati
        public static string Testo(NecModel m, string nome, string necTesto)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Firma).Append(" 2\n");
            Dictionary<string, List<string>> v = new Dictionary<string, List<string>>();
            if (m != null && m.Scheda != null)
                foreach (KeyValuePair<string, string> kv in m.Scheda)
                {
                    if (!v.ContainsKey(kv.Key)) v[kv.Key] = new List<string>();
                    v[kv.Key].Add(kv.Value);
                }
            v["NOME"] = new List<string> { nome };
            if (!v.ContainsKey("ORIGINALE")) v["ORIGINALE"] = new List<string> { nome + ".nec" };
            v["CONTENUTO"] = new List<string> { "nec" };
            foreach (string k in Chiavi)
            {
                List<string> l;
                if (!v.TryGetValue(k, out l)) { if (k != "NOTA") sb.Append(Prefisso).Append(k).Append("=\n"); continue; }
                foreach (string x in l) sb.Append(Prefisso).Append(k).Append('=').Append((x ?? "").Replace("\r", " ").Replace("\n", " ")).Append('\n');
            }
            sb.Append(necTesto);
            return sb.ToString();
        }
    }
}
