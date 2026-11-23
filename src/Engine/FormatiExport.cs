// YA-NEC 0.4.3 - scrittura dei modelli in AutoEZ / MultiNEC (.weq) ed EZNEC (.ez), per il dialogo "Salva con nome"
// approvato alla lettera (voce A1 della verifica del 13/11/2026).
//  .weq: testo a colonne separate da tabulazioni, righe CRLF, come il file 16el di IC8TEM (MultiNEC, coordinate in m,
//        diametri in mm, variabili nelle righe 2-23 con le lettere A, B, D, E... di AutoEZ, formule Excel quando si puo').
//  .ez : binario sperimentale con la struttura decodificata da Ez2Nec (AG7T, HanleyStrappman/Ez2Nec, src/ez/read.rs):
//        testata di 0xBA byte, una riga di 170 byte per filo/sorgente/carico/linea, 324 byte, blocchi finali. Testata e blocchi
//        non documentati (grafico, programma, motore) sono presi da un file salvato da EZNEC Pro/2+ v7 (lo stack "panino" di IC8TEM).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace YaNec.Engine
{
    public static class FormatiExport
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        const int Colonne = 31;
        static string F(double v) { return Math.Abs(v) < 1e-12 ? "0" : v.ToString("0.#########", CI); }

        static string RigaWeq(params string[] celle)
        {
            string[] r = new string[Colonne];
            for (int i = 0; i < Colonne; i++) r[i] = i < celle.Length && celle[i] != null ? celle[i] : "";
            return string.Join("\t", r) + "\r\n";
        }

        /// <summary>Lettere delle variabili di AutoEZ: A..Z senza C ed R (nomi riservati di Excel).</summary>
        static List<string> Lettere()
        {
            List<string> l = new List<string>();
            for (char c = 'A'; c <= 'Z'; c++) if (c != 'C' && c != 'R') l.Add(c.ToString());
            return l;
        }

        /// <summary>Formula di YA-NEC in formula Excel con le lettere di AutoEZ; null se non si puo' (funzioni, nomi sconosciuti).</summary>
        static string Formula(string expr, Dictionary<string, string> lettera, double scala)
        {
            if (string.IsNullOrWhiteSpace(expr)) return null;
            double d;
            if (double.TryParse(expr, NumberStyles.Float, CI, out d)) return null;      // numero: si scrive il valore
            bool ok = true;
            string t = Regex.Replace(expr, @"[A-Za-z_][A-Za-z0-9_]*(\s*\()?", m =>
            {
                if (m.Groups[1].Success) { ok = false; return m.Value; }             // funzione: in Excel gli angoli sono in radianti
                string n = m.Value;
                foreach (KeyValuePair<string, string> kv in lettera)
                    if (string.Equals(kv.Key, n, StringComparison.OrdinalIgnoreCase)) return kv.Value;
                ok = false; return n;
            });
            if (!ok || !Regex.IsMatch(t, @"^[0-9A-Z.+\-*/^() eE]*$")) return null;
            return scala == 1 ? "=" + t : "=(" + t + ")*" + F(scala);
        }

        static bool SoloGw(NecModel m)
        {
            HashSet<int> tag = new HashSet<int>();
            foreach (GeomCard g in m.Geometry) { if (g.Id != "GW" || g.HasGc || !tag.Add(g.I1)) return false; }
            return true;
        }

        // ------------------------------------------------------------------------------------------- .weq
        /// <summary>Testo .weq. segYaNec = false: segmenti con la formula di AutoEZ =ODD(WireLen / WL * 20).</summary>
        /// <summary>1.5 (z5, IC8TEM 18/11/2026 23:01): .nec per gli altri programmi (nec2c, 4nec2, xnec2c). Le sorgenti di corrente di
        /// EZNEC e le sorgenti accese solo in certe bande (AutoEZ «mark out») non esistono nel NEC-2: si scrivono le tensioni che YA-NEC
        /// ricava alla frequenza del modello (V = Z I, Analysis.EffectiveSources) e solo le sorgenti accese, con una riga CM che lo dice.
        /// Prima la corrente voluta finiva in EX 0 come tensione e nec2c dava un altro risultato con piu' sorgenti (fino al 93 %).</summary>
        public static string NecPerAltri(NecModel m, List<string> perso)
        {
            bool mark = m.Sources.Exists(s => s.AttivaA != null);
            if ((!m.CurrentSources && !mark) || m.Sources.Count == 0 || m.Geometry.Count == 0) return m.ToNec();
            NecModel c = m.Clone();
            SegmentGeometry g = Analysis.GeometriaDiCalcolo(c, c.FreqMHz);
            List<SourceSpec> ss = new List<SourceSpec>();
            foreach (SourceSpec s in Analysis.EffectiveSources(c, g, c.FreqMHz)) { SourceSpec k = s.Clone(); k.AttivaA = null; k.AttivaTesto = null; ss.Add(k); }
            c.Sources = ss;
            bool corr = c.CurrentSources;
            c.CurrentSources = false;
            string f = c.FreqMHz.ToString("0.######", CultureInfo.InvariantCulture);
            // righe CM sotto gli 80 caratteri (nec2c legge 80 colonne)
            string nota = corr ? "CM YA-NEC: sorgenti di corrente scritte come tensioni" + Environment.NewLine + "CM risolte a " + f + " MHz: valgono solo a questa frequenza"
                               : "CM YA-NEC: solo le sorgenti accese a " + f + " MHz (mark out di AutoEZ)";
            if (corr) perso.Add("Sorgenti di corrente: il NEC-2 non le ha, scritte come tensioni calcolate a " + f + " MHz (cambiando frequenza vanno ricalcolate in YA-NEC).");
            if (mark) perso.Add("Sorgenti accese solo in certe bande: scritte solo quelle accese a " + f + " MHz.");
            return nota + Environment.NewLine + c.ToNec();
        }

        public static string Weq(NecModel m, bool segYaNec, List<string> perso)
        {
            SegmentGeometry g = m.BuildGeometry();
            List<MmanaExport.Wire> ws = MmanaExport.Wires(g);
            bool formule = SoloGw(m) && ws.Count == m.Geometry.Count;
            if (!formule && m.Variables.Count > 0) perso.Add("Le schede diverse da GW (GA, GH, GM...) diventano fili diritti: le formule di quei fili diventano numeri.");
            StringBuilder sb = new StringBuilder();
            sb.Append(RigaWeq("MultiNEC", F(m.FreqMHz), "m", "mm", "SLT30", "*** DO NOT MAKE MANUAL EDITS TO THIS FILE ***"));
            // variabili: righe 2-23, valore e descrizione (AutoEZ non ha formule tra le variabili: si scrive il valore)
            List<string> let = Lettere();
            Dictionary<string, string> lettera = new Dictionary<string, string>();
            int nv = Math.Min(22, m.Variables.Count);
            if (m.Variables.Count > 22) perso.Add("AutoEZ ha 22 righe di variabili: le variabili oltre la 22a diventano numeri.");
            for (int i = 0; i < 22; i++)
            {
                if (i < nv)
                {
                    SyVar v = m.Variables[i];
                    lettera[v.Name] = let[i];
                    sb.Append(RigaWeq("_" + F(v.Value), "_" + v.Name + (string.IsNullOrEmpty(v.Descr) ? "" : " - " + v.Descr.Replace("\t", " "))));
                }
                else sb.Append(RigaWeq());
            }
            if (m.Variables.Count > nv) lettera.Clear();   // troppe variabili: tutto in numeri
            string titolo = m.Comments.Count > 0 ? m.Comments[0] : "YA-NEC";
            sb.Append(RigaWeq("_m", "_" + titolo.Replace("\t", " ")));
            int formuleScritte = 0;
            for (int k = 0; k < ws.Count; k++)
            {
                MmanaExport.Wire w = ws[k];
                double[] v = { w.X1, w.Y1, w.Z1, w.X2, w.Y2, w.Z2, w.R * 2000 };
                string[] c = new string[9];
                c[0] = "_m";
                GeomCard card = formule ? m.Geometry[k] : null;
                for (int j = 0; j < 7; j++)
                {
                    string fx = null;
                    if (card != null && card.Src != null && card.Src.Length > j + 2 && lettera.Count > 0) fx = Formula(card.Src[j + 2], lettera, j == 6 ? 2000 : 1);
                    if (fx != null) formuleScritte++;
                    c[j + 1] = "_" + (fx ?? F(v[j]));
                }
                c[8] = "_" + (segYaNec ? w.NSeg.ToString(CI) : "=ODD(WireLen / WL * 20)");
                sb.Append(RigaWeq(c));
            }
            sb.Append(RigaWeq("_m"));
            sb.Append(RigaWeq("_m", "_###Comment###"));
            sb.Append(RigaWeq("_m", "\"_Salvato da YA-NEC " + DateTime.Now.ToString("dd/MM/yyyy HH:mm", CI) + "\""));
            // sorgenti: filo, % da E1 (centro del segmento), ampiezza, fase, tipo
            Func<int, int, int[]> filo = (tag, seg) =>
            {
                for (int k = 0; k < ws.Count; k++)
                    if (ws[k].Tag == tag && seg >= ws[k].FirstSeg && seg < ws[k].FirstSeg + ws[k].NSeg) return new[] { k + 1, seg - ws[k].FirstSeg, ws[k].NSeg };
                return null;
            };
            // tre blocchi di 30 righe dopo i commenti, come nei file di AutoEZ (16el di IC8TEM, esempi 4Square, Fan Dipole, Diamond Quad):
            // sorgenti (filo, % da E1, ampiezza, fase, V/I), carichi (filo, %, R, X, -, -, -, Ser), linee (filo 1, %, filo 2, %,
            // lunghezza, Z0, VF, N/R = normale o incrociata, perdita dB, frequenza della perdita)
            List<string> sorg = new List<string>(), cari = new List<string>(), lin = new List<string>();
            foreach (SourceSpec s in m.Sources)
            {
                int[] f = filo(s.Tag, s.Seg);
                if (f == null) { perso.Add("Sorgente sul filo " + s.Tag + " segmento " + s.Seg + " non trovata: non scritta."); continue; }
                double pct = (f[1] + 0.5) / f[2] * 100;
                Complex v = s.V;
                double amp = Complex.Abs(v), ph = Math.Atan2(v.Imaginary, v.Real) * 180 / Math.PI;
                sorg.Add(RigaWeq("_" + f[0], "_" + F(pct), "_" + F(amp == 0 ? 1 : amp), "_" + F(ph), m.CurrentSources ? "_I" : "_V"));
            }
            double w0 = 2 * Math.PI * m.FreqMHz * 1e6;
            foreach (LoadSpec ld in m.Loads)
            {
                if (ld.Type == 5) { perso.Add("Conducibilita' dei fili (LD 5): in AutoEZ si imposta nel modello EZNEC (Wire Loss)."); continue; }
                int[] f = filo(ld.Tag, Math.Max(1, ld.From));
                if (f == null) { perso.Add("Carico sul filo " + ld.Tag + " non trovato: non scritto."); continue; }
                double r, x;
                if (ld.Type == 4) { r = ld.F1; x = ld.F2; }
                else if (ld.Type == 0)
                {
                    r = ld.F1; x = w0 * ld.F2 - (ld.F3 > 0 ? 1 / (w0 * ld.F3) : 0);
                    perso.Add("Carico RLC serie sul filo " + ld.Tag + " scritto come R+jX a " + F(m.FreqMHz) + " MHz.");
                }
                else { perso.Add("Carico LD " + ld.Type + " sul filo " + ld.Tag + ": tipo non scritto nel .weq."); continue; }
                cari.Add(RigaWeq("_" + f[0], "_" + F((f[1] + 0.5) / f[2] * 100), "_" + F(r), "_" + F(x), "", "", "", "_Ser"));
            }
            foreach (NetworkSpec n in m.Networks)
            {
                if (!n.IsTl) { perso.Add("Rete NT dal filo " + n.Tag1 + " al filo " + n.Tag2 + ": non esiste in AutoEZ, non scritta."); continue; }
                int[] a1 = filo(n.Tag1, n.Seg1), b1 = filo(n.Tag2, n.Seg2);
                if (a1 == null || b1 == null) { perso.Add("Linea dal filo " + n.Tag1 + " al filo " + n.Tag2 + ": estremi non trovati, non scritta."); continue; }
                lin.Add(RigaWeq("_" + a1[0], "_" + F((a1[1] + 0.5) / a1[2] * 100), "_" + b1[0], "_" + F((b1[1] + 0.5) / b1[2] * 100),
                    "_" + F(n.F2 > 0 ? n.F2 : 1e-8), "_" + F(Math.Abs(n.F1)), "_1", n.F1 < 0 ? "_R" : "_N", "_0", "_0"));
            }
            if (m.Ground.Kind != GroundKind.FreeSpace) perso.Add("Terreno: AutoEZ lo prende dal modello EZNEC aperto (impostarlo in EZNEC: er " + F(m.Ground.Epsr) + ", sigma " + F(m.Ground.Sigma) + " S/m).");
            if (formule && formuleScritte == 0 && m.Variables.Count > 0) perso.Add("Formule con funzioni (SIN, COS...) scritte come numeri: in Excel gli angoli sono in radianti.");
            foreach (List<string> blocco in new[] { sorg, cari, lin })
            {
                if (blocco.Count > 30) perso.Add("Piu' di 30 righe in un blocco del .weq: scritte le prime 30.");
                for (int i = 0; i < 30; i++) sb.Append(i < blocco.Count ? blocco[i] : "\r\n");
            }
            sb.Append(RigaWeq("Controls", "FALSE", "TRUE", "0", "1", "2", "0", "0", "1", F(m.Z0), "0", "0", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "FALSE", "TRUE", "_1", "_0").TrimEnd('\r', '\n').TrimEnd('\t') + "\r\n");
            sb.Append("_Freq (MHz)\r\n");
            sb.Append("_" + F(m.FreqMHz) + "\r\n");
            sb.Append("2\r\n");
            return sb.ToString();
        }

        // ------------------------------------------------------------------------------------------- .ez
        const int Testa = 0xBA, Riga = 170, Gap = 324;
        const string TestaModello = "IgABAFJGFthDRQAAAAAAAIA/Q3JlYXRlZCBmcm9tIEF1dG9FWiAgICAgICAgICAgIgACAAAAUgAArCoHPAAAGAVNRlQAAKNEAAAAAAAANENTAAAAAAAAAABSAACAPwAAAAB3zCszAACAPwAAAAAAAJZCMgBMUgEAAABFTSIAAAAiAAAAAAAAAAAAoEAAAAAAgx0sBAAAIEIAAAAAAAAAAAAAAAAAAAAAAABvEoM6AACgQAAAAAAAAAAA";
        const string BlocchiModello = "CwAnAAAAARAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADkwHwAnAAAAAQAAAAABAAAAAAAAAAAAAAAAAAAAAACAPwAAgD8AAAAAZQBYAAAAAgIAAADaS2gvXaDmQAwAAABFWk5FQyBQcm8vMisFAAAANy4wLjODHSwEJAAAAA0KQ2FsYyBFbmdpbmUgPSBFWkNhbGNEXzcwX3g2NC5FWEUNCmYAJQAAAAICAAAAEgAAAEVaQ2FsY0RfNzBfeDY0LkVYRQAAAAA=";

        static void I16(byte[] d, int o, int v) { BitConverter.GetBytes((short)v).CopyTo(d, o); }
        static void I32(byte[] d, int o, int v) { BitConverter.GetBytes(v).CopyTo(d, o); }
        static void F32(byte[] d, int o, double v) { BitConverter.GetBytes((float)v).CopyTo(d, o); }

        /// <summary>File .ez (sperimentale). Le coordinate sono in metri; le unita' mostrate da EZNEC sono i metri.</summary>
        public static byte[] Ez(NecModel m, List<string> perso)
        {
            SegmentGeometry g = m.BuildGeometry();
            List<MmanaExport.Wire> ws = MmanaExport.Wires(g);
            Func<int, int, int[]> filo = (tag, seg) =>
            {
                for (int k = 0; k < ws.Count; k++)
                    if (ws[k].Tag == tag && seg >= ws[k].FirstSeg && seg < ws[k].FirstSeg + ws[k].NSeg) return new[] { k + 1, seg - ws[k].FirstSeg, ws[k].NSeg };
                return null;
            };
            Func<int[], double> pct = f => (f[1] + 0.5) / f[2] * 100;
            // carichi: R+jX (tipo Z) oppure RLC (tipo R); con tutti e due si scrive tutto come RLC
            double sigma = 0;
            List<LoadSpec> car = new List<LoadSpec>();
            bool rlc = false;
            foreach (LoadSpec ld in m.Loads)
            {
                if (ld.Type == 5) { sigma = ld.F1; continue; }
                if (ld.Type == 0 || ld.Type == 1) { rlc = true; car.Add(ld); }
                else if (ld.Type == 4) car.Add(ld);
                else perso.Add("Carico LD " + ld.Type + " sul filo " + ld.Tag + ": tipo non disponibile in EZNEC, non scritto.");
            }
            int nw = ws.Count, ns = m.Sources.Count, nl = car.Count, nt = 0;
            foreach (NetworkSpec n in m.Networks) if (n.IsTl) nt++; else perso.Add("Rete NT dal filo " + n.Tag1 + " al filo " + n.Tag2 + ": EZNEC non ha le reti a due porte generiche, non scritta.");
            // 0.4.20 (collaudo 0.4.20, fase 2G: LPDA HF riletta con il 6,9 % di scarto): le parti che il .ez non contiene si dicono tutte
            if (m.Stubs.Count > 0) perso.Add(m.Stubs.Count + " stub in parallelo (riga YA-NEC STUB, per esempio lo stub in corto della LPDA) non scritti: EZNEC non li ha; in EZNEC si aggiunge una linea in corto a mano.");
            if (m.Trasformatori.Count > 0) perso.Add(m.Trasformatori.Count + " trasformatori (riga YA-NEC TRASF) non scritti.");
            if (m.RetiL.Count > 0) perso.Add(m.RetiL.Count + " reti L (riga YA-NEC RETEL) non scritte.");
            if (m.Laplace.Count > 0) perso.Add(m.Laplace.Count + " carichi di Laplace (riga YA-NEC LAPLACE) non scritti.");
            if (m.LineePerdite.Count > 0) perso.Add(m.LineePerdite.Count + " linee con perdite (riga YA-NEC LINEAP) non scritte.");
            bool reale = m.Ground.Kind == GroundKind.Real;
            int nr = Math.Max(Math.Max(nw, ns), Math.Max(nl, nt));
            byte[] head = Convert.FromBase64String(TestaModello);
            byte[] coda = Convert.FromBase64String(BlocchiModello);
            byte[] d = new byte[Testa + Riga * nr + Gap + coda.Length];
            Array.Copy(head, d, Testa);
            I16(d, 0x00, nr); I16(d, 0x02, reale ? 1 : 0);
            F32(d, 0x05, m.FreqMHz);
            string titolo = m.Comments.Count > 0 ? m.Comments[0] : "Salvato da YA-NEC";
            byte[] tb = Encoding.GetEncoding("ISO-8859-1").GetBytes((titolo + new string(' ', 30)).Substring(0, 30));
            Array.Copy(tb, 0, d, 0x12, 30);
            I16(d, 0x30, nw); I16(d, 0x32, ns); I16(d, 0x34, nl); I16(d, 0x78, nt);
            d[0x36] = (byte)(m.Ground.Kind == GroundKind.FreeSpace ? 'F' : m.Ground.Kind == GroundKind.Perfect ? 'P' : 'R');
            // 0.4.20 (prova 6 con EZNEC): EZNEC ha solo due terreni reali, "MININEC" (impedenza su terreno perfetto, = GN 3) e "High
            // Accuracy" (Sommerfeld, = GN 2). Il GN 0 del NEC-2 (coefficiente di riflessione) non c'e': si scrive High Accuracy, che da'
            // impedenze vicine al GN 0 (dipolo a 10 m: 74,06 +j10,89 contro 74,24 +j10,68 ohm), mentre MININEC le cambierebbe fino al 20 %.
            d[0x7B] = (byte)(m.Ground.MiniNec ? 'M' : 'H');
            if (reale && !m.Ground.Sommerfeld && !m.Ground.MiniNec)
                perso.Add("Terreno GN 0 (coefficiente di riflessione): EZNEC non lo ha, scritto come \"High Accuracy\" (Sommerfeld, GN 2). Differenze di qualche decimo di ohm.");
            d[0x41] = (byte)'M';
            d[0x50] = (byte)(rlc ? 'S' : 'Z');
            d[0x59] = (byte)'Z';
            d[0x75] = (byte)(rlc ? 'R' : 'Z');
            F32(d, 0x62, sigma > 0 ? 1 / sigma : 0);
            F32(d, 0x66, 1);
            I32(d, 0x7C, nr); I32(d, 0x80, nw); I32(d, 0x9C, 0);
            for (int i = 0; i < 16; i++) d[0xAA + i] = 0;
            if (reale) { F32(d, 0xAA, m.Ground.Sigma); F32(d, 0xAE, m.Ground.Epsr); }
            for (int k = 0; k < nr; k++)
            {
                int r = Testa + Riga * k;
                if (k < nw)
                {
                    MmanaExport.Wire w = ws[k];
                    F32(d, r, w.X1); F32(d, r + 4, w.Y1); F32(d, r + 8, w.Z1); F32(d, r + 12, w.X2); F32(d, r + 16, w.Y2); F32(d, r + 20, w.Z2);
                    F32(d, r + 0x18, 2 * w.R); I16(d, r + 0x1C, -1); I16(d, r + 0x1E, w.NSeg);
                }
                if (k < ns)
                {
                    SourceSpec s = m.Sources[k];
                    int[] f = filo(s.Tag, s.Seg);
                    if (f == null) { perso.Add("Sorgente sul filo " + s.Tag + " segmento " + s.Seg + " non trovata: messa sul filo 1."); f = new[] { 1, 0, ws[0].NSeg }; }
                    double amp = Complex.Abs(s.V), ph = Math.Atan2(s.V.Imaginary, s.V.Real) * 180 / Math.PI;
                    I16(d, r + 0x20, f[0]); F32(d, r + 0x22, pct(f)); F32(d, r + 0x26, amp == 0 ? 1 : amp); F32(d, r + 0x2A, ph);
                    d[r + 0x2E] = (byte)(m.CurrentSources ? 'I' : 'V');
                }
                if (k < nl)
                {
                    LoadSpec ld = car[k];
                    int[] f = filo(ld.Tag, Math.Max(1, ld.From));
                    if (f == null) { perso.Add("Carico sul filo " + ld.Tag + " non trovato: messo sul filo 1."); f = new[] { 1, 0, ws[0].NSeg }; }
                    if (ld.To > ld.From) perso.Add("Carico sul filo " + ld.Tag + " segmenti " + ld.From + "-" + ld.To + ": in EZNEC su un solo segmento (" + ld.From + ").");
                    I16(d, r + 0x2F, f[0]); F32(d, r + 0x31, pct(f));
                    double w0 = 2 * Math.PI * m.FreqMHz * 1e6;
                    if (!rlc) { F32(d, r + 0x35, ld.F1); F32(d, r + 0x39, ld.F2); }
                    else
                    {
                        d[r + 0x85] = (byte)'S';
                        if (ld.Type == 4)
                        {
                            // R + jX alla frequenza del modello come RLC serie
                            d[r + 0x86] = (byte)'S';
                            F32(d, r + 0x87, ld.F1);
                            F32(d, r + 0x8B, ld.F2 > 0 ? ld.F2 / w0 : 0);
                            F32(d, r + 0x8F, ld.F2 < 0 ? -1 / (w0 * ld.F2) : 0);
                            perso.Add("Carico R+jX sul filo " + ld.Tag + " scritto come RLC serie valido a " + F(m.FreqMHz) + " MHz.");
                        }
                        else
                        {
                            d[r + 0x86] = (byte)(ld.Type == 1 ? 'P' : 'S');
                            F32(d, r + 0x87, ld.F1); F32(d, r + 0x8B, ld.F2); F32(d, r + 0x8F, ld.F3);
                        }
                    }
                }
            }
            int kt = 0;
            foreach (NetworkSpec n in m.Networks)
            {
                if (!n.IsTl) continue;
                int r = Testa + Riga * kt + 0x6D;
                int[] a = filo(n.Tag1, n.Seg1), b = filo(n.Tag2, n.Seg2);
                if (a == null || b == null) { perso.Add("Linea dal filo " + n.Tag1 + " al filo " + n.Tag2 + ": estremi non trovati."); a = a ?? new[] { 1, 0, 1 }; b = b ?? new[] { 1, 0, 1 }; }
                I16(d, r, a[0]); F32(d, r + 2, pct(a)); I16(d, r + 6, b[0]); F32(d, r + 8, pct(b));
                F32(d, r + 12, n.F1);            // Z0 negativa = linea incrociata (Reversed in EZNEC)
                F32(d, r + 16, n.F2); F32(d, r + 20, 1.0);
                if (n.F3 != 0 || n.F4 != 0 || n.F5 != 0 || n.F6 != 0) perso.Add("Linea dal filo " + n.Tag1 + ": le ammettenze di chiusura della scheda TL non esistono in EZNEC, non scritte.");
                kt++;
            }
            int gap = Testa + Riga * nr;
            d[gap + 0x34] = 0x20;
            Array.Copy(coda, 0, d, gap + Gap, coda.Length);
            if (sigma > 0 && m.Loads.Exists(q => q.Type == 5 && q.Tag != 0)) perso.Add("Conducibilita' per filo (LD 5 con il tag): in EZNEC una sola per tutti i fili.");
            if (m.Leeson) perso.Add("Correzione Leeson: da attivare in EZNEC (Options, Stepped Diameter Correction).");
            perso.Add("Formato .ez sperimentale (struttura da Ez2Nec, AG7T): controllare in EZNEC; per EZNEC Pro e' piu' sicuro il .nec.");
            return d;
        }
    }
}
