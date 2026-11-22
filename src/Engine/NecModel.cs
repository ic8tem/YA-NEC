// YA-NEC - modello d'antenna: lettura e scrittura dei file NEC2 (.nec), impostazioni di calcolo.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

namespace YaNec.Engine
{
    public class NecModel
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        public string FileName = "";
        public string SourceFormat = "NEC";        // NEC, MMANA
        public List<string> Comments = new List<string>();
        public List<KeyValuePair<string, string>> Scheda = new List<KeyValuePair<string, string>>();   // 0.4.30: abaco della descrizione del .yanec (righe CM YANEC)
        public List<GeomCard> Geometry = new List<GeomCard>();
        public int GroundFlag;                     // I1 della scheda GE
        public List<SourceSpec> Sources = new List<SourceSpec>();
        public List<LoadSpec> Loads = new List<LoadSpec>();
        public List<NetworkSpec> Networks = new List<NetworkSpec>();
        public GroundSpec Ground = new GroundSpec();
        int calcestruzzoCm;   // 0.5.0: riga CM YA-NEC CALCESTRUZZO=k, applicata al terreno alla fine della lettura
        public bool ExtendedKernel;
        public bool Leeson;            // correzione dei tubi telescopici (diametri a gradini), ricalcolata a ogni frequenza
        public bool GiunzioniNec4;     // 1.2.0: carica alle giunzioni e nucleo come il NEC-4 (opzione, Nec2Engine.Giunzioni.cs)
        public double Rkh = 1.0;
        // frequenze (FR): inizio, passo, numero, passo moltiplicativo
        public double FreqMHz = 144.3;
        public double FreqStep;
        public int FreqCount = 1;
        public bool FreqMultiplicative;
        // impedenza di riferimento per il ROS (non e' una scheda NEC: commento "CM YA-NEC Z0=..." o dal file MMANA)
        public double Z0 = 50.0;
        // materiale dei fili (applicato come LD 5 a tutti i segmenti), conducibilita' S/m, 0 = senza perdite
        public double WireConductivity;
        // sorgenti a corrente (schiere fasate): il valore di EX e' la corrente voluta, le tensioni le ricava il programma
        public bool CurrentSources;
        // "Usa i carichi" (come MMANA): false = i carichi restano nel modello ma non entrano nel calcolo
        public bool UseLoads = true;
        public string WireMaterial = "";
        public List<string> Warnings = new List<string>();
        public List<string> Notes = new List<string>();
        // schede non interpretate, conservate per il salvataggio
        public List<string> OtherCards = new List<string>();
        // variabili SY, nell'ordine di definizione
        public List<SyVar> Variables = new List<SyVar>();
        // oggetti inseriti della 0.4.3 (OggettiExtra.cs): rastremazioni dei tubi, trasformatori, reti L
        public List<Rastremazione> Rastremazioni = new List<Rastremazione>();
        public List<Trasformatore> Trasformatori = new List<Trasformatore>();
        public List<ReteL> RetiL = new List<ReteL>();
        public List<Stub> Stubs = new List<Stub>();          // 0.4.7: stub in parallelo senza filo fittizio (LPDA)
        readonly List<string> righeExtra = new List<string>();
        Dictionary<string, double> varValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> varDescr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public NecModel Clone()
        {
            NecModel m = (NecModel)MemberwiseClone();
            m.Comments = new List<string>(Comments);
            m.Scheda = new List<KeyValuePair<string, string>>(Scheda);
            m.Geometry = new List<GeomCard>();
            foreach (GeomCard g in Geometry) m.Geometry.Add(g.Clone());
            m.Sources = new List<SourceSpec>();
            foreach (SourceSpec s in Sources) m.Sources.Add(s.Clone());
            m.Loads = new List<LoadSpec>();
            foreach (LoadSpec l in Loads) m.Loads.Add(l.Clone());
            m.Networks = new List<NetworkSpec>();
            foreach (NetworkSpec nw in Networks) m.Networks.Add(nw.Clone());
            m.Ground = Ground.Clone();
            m.Warnings = new List<string>(Warnings);
            m.Notes = new List<string>(Notes);
            m.OtherCards = new List<string>(OtherCards);
            m.Variables = new List<SyVar>();
            foreach (SyVar v in Variables) m.Variables.Add(v.Clone());
            m.varValues = new Dictionary<string, double>(varValues, StringComparer.OrdinalIgnoreCase);
            m.varDescr = new Dictionary<string, string>(varDescr, StringComparer.OrdinalIgnoreCase);
            m.Rastremazioni = new List<Rastremazione>();
            foreach (Rastremazione r in Rastremazioni) m.Rastremazioni.Add(r.Clone());
            m.Trasformatori = new List<Trasformatore>();
            foreach (Trasformatore t in Trasformatori) m.Trasformatori.Add(t.Clone());
            m.RetiL = new List<ReteL>();
            foreach (ReteL q in RetiL) m.RetiL.Add(q.Clone());
            m.Stubs = new List<Stub>();
            foreach (Stub st in Stubs) m.Stubs.Add(st.Clone());
            return m;
        }

        public SegmentGeometry BuildGeometry(List<string> notes = null)
        {
            int ignd = GroundFlag;
            // con il terreno i fili che lo toccano vanno collegati alla loro immagine
            if (Ground.Kind != GroundKind.FreeSpace && ignd == 0) ignd = 1;
            if (Ground.Kind == GroundKind.FreeSpace) ignd = 0;
            return GeometryBuilder.Build(OggettiExtra.Espandi(this, Geometry, notes), ignd, notes);
        }

        /// <summary>Linee e reti da usare nel calcolo alla frequenza f: quelle del modello piu' trasformatori e reti L (schede NT).</summary>
        public List<NetworkSpec> EffectiveNetworks(double fMHz)
        {
            if (Trasformatori.Count == 0 && RetiL.Count == 0 && Stubs.Count == 0) return Networks;
            List<NetworkSpec> l = new List<NetworkSpec>(Networks);
            foreach (Trasformatore t in Trasformatori) l.Add(OggettiExtra.Nt(t));
            foreach (ReteL q in RetiL) l.Add(OggettiExtra.Nt(q, fMHz));
            // stub: ammettenza in parallelo all'estremo della prima linea TL che tocca il segmento (copia della scheda, il modello non cambia)
            foreach (Stub st in Stubs)
            {
                int k = l.FindIndex(n => n.IsTl && ((n.Tag1 == st.Tag && n.Seg1 == st.Seg) || (n.Tag2 == st.Tag && n.Seg2 == st.Seg)));
                if (k < 0) continue;
                NetworkSpec c = l[k].Clone();
                System.Numerics.Complex y = st.Y(fMHz);
                if (c.Tag1 == st.Tag && c.Seg1 == st.Seg) { c.F3 += y.Real; c.F4 += y.Imaginary; }
                else { c.F5 += y.Real; c.F6 += y.Imaginary; }
                l[k] = c;
            }
            return l;
        }

        /// <summary>Carichi da usare nel calcolo: quelli del modello piu' il materiale dei fili.</summary>
        public List<LoadSpec> EffectiveLoads()
        {
            List<LoadSpec> l = UseLoads ? new List<LoadSpec>(Loads) : new List<LoadSpec>();
            if (WireConductivity > 0)
                l.Add(new LoadSpec { Type = 5, Tag = 0, From = 0, To = 0, F1 = WireConductivity });
            return l;
        }

        public List<double> Frequencies()
        {
            List<double> f = new List<double>();
            int nf = Math.Max(1, FreqCount);
            double v = FreqMHz;
            for (int i = 0; i < nf; i++)
            {
                f.Add(v);
                if (FreqMultiplicative) v *= FreqStep; else v += FreqStep;
            }
            return f;
        }

        // ---------------------------------------------------------------------------------------------
        // lettura .nec
        // ---------------------------------------------------------------------------------------------
        static string[] Fields(string rest)
        {
            return Expr.SplitFields(rest);
        }

        // campi interi: numero o espressione, arrotondato all'intero piu' vicino (come INT di 4nec2/xnec2c)
        int ParseInt(string[] f, int idx)
        {
            if (idx >= f.Length) return 0;
            string s = f[idx];
            int v;
            if (int.TryParse(s, NumberStyles.Integer, CI, out v)) return v;
            double d;
            if (double.TryParse(s, NumberStyles.Float, CI, out d)) return (int)d;
            return (int)Math.Round(Expr.Eval(s, varValues), MidpointRounding.AwayFromZero);
        }

        double ParseDouble(string[] f, int idx)
        {
            if (idx >= f.Length) return 0;
            return Expr.Eval(f[idx], varValues);
        }

        static string[] Pad(string[] f, int start, int count)
        {
            string[] r = new string[count];
            for (int k = 0; k < count; k++) r[k] = start + k < f.Length ? f[start + k] : "0";
            return r;
        }

        /// <summary>Definisce o ridefinisce una variabile e ne calcola il valore con le variabili gia' note.</summary>
        public void DefineVariable(string name, string expr)
        {
            SyVar v = Variables.Find(q => string.Equals(q.Name, name, StringComparison.OrdinalIgnoreCase));
            if (v == null) { v = new SyVar { Name = name }; Variables.Add(v); }
            v.Expr = expr;
            string ds;
            if (varDescr.TryGetValue(name, out ds)) v.Descr = ds;
            v.Value = Expr.Eval(expr, varValues);
            v.Error = null;
            varValues[name] = v.Value;
        }

        /// <summary>Ricalcola tutte le variabili (nell'ordine) e i campi delle schede di geometria. Restituisce gli errori.</summary>
        public List<string> Reevaluate()
        {
            List<string> err = new List<string>();
            varValues.Clear();
            List<string> bad = new List<string>();
            foreach (SyVar v in Variables)
            {
                // se dipende da una variabile con errore, lo dice invece di "variabile sconosciuta"
                string dep = bad.Find(b => ContainsName(v.Expr, b));
                if (dep != null)
                {
                    v.Error = "dipende da " + dep + ", che ha un errore";
                    err.Add("Variabile " + v.Name + ": " + v.Error);
                    bad.Add(v.Name);
                    continue;
                }
                try
                {
                    if (!Expr.IsValidName(v.Name)) throw new NecException("nome non valido o riservato");
                    v.Value = Expr.Eval(v.Expr, varValues);
                    v.Error = null;
                    varValues[v.Name] = v.Value;
                }
                catch (Exception ex)
                {
                    v.Error = ex.Message;
                    err.Add("Variabile " + v.Name + ": " + ex.Message);
                    bad.Add(v.Name);
                }
            }
            if (err.Count > 0) return err;
            foreach (GeomCard g in Geometry)
            {
                if (g.Src == null) continue;
                try
                {
                    g.I1 = ParseInt(g.Src, 0); g.I2 = ParseInt(g.Src, 1);
                    g.F1 = ParseDouble(g.Src, 2); g.F2 = ParseDouble(g.Src, 3); g.F3 = ParseDouble(g.Src, 4);
                    g.F4 = ParseDouble(g.Src, 5); g.F5 = ParseDouble(g.Src, 6); g.F6 = ParseDouble(g.Src, 7);
                    g.F7 = ParseDouble(g.Src, 8);
                    if (g.HasGc && g.GcSrc != null)
                    {
                        g.GcRdel = ParseDouble(g.GcSrc, 0); g.GcRad1 = ParseDouble(g.GcSrc, 1); g.GcRad2 = ParseDouble(g.GcSrc, 2);
                    }
                }
                catch (Exception ex)
                {
                    err.Add("Scheda " + g.Id + " " + string.Join(" ", g.Src) + ": " + ex.Message);
                }
            }
            return err;
        }

        static readonly string[] GwFieldNames = { "tag", "segmenti", "X1", "Y1", "Z1", "X2", "Y2", "Z2", "raggio" };

        /// <summary>Descrizione automatica di una variabile, ricavata da dove e' usata (per la colonna Descrizione).</summary>
        public string DescribeUse(string name)
        {
            // campo -> elenco dei fili (tag) in cui compare
            SortedDictionary<int, List<string>> perField = new SortedDictionary<int, List<string>>();
            List<string> other = new List<string>();
            int wire = 0;
            foreach (GeomCard g in Geometry)
            {
                if (g.Id == "GW") wire++;
                if (g.Src == null) continue;
                for (int k = 0; k < g.Src.Length; k++)
                {
                    if (!ContainsName(g.Src[k], name)) continue;
                    if (g.Id == "GW" && k < GwFieldNames.Length)
                    {
                        List<string> l;
                        if (!perField.TryGetValue(k, out l)) perField[k] = l = new List<string>();
                        string w = wire.ToString(CI);
                        if (!l.Contains(w)) l.Add(w);
                    }
                    else if (!other.Contains(g.Id)) other.Add(g.Id);
                }
                if (g.GcSrc != null) foreach (string f in g.GcSrc) if (ContainsName(f, name) && !other.Contains("GC")) other.Add("GC");
            }
            List<string> parts = new List<string>();
            // raggruppa i campi che compaiono negli stessi fili: "Y1, Y2 del filo 1"
            List<KeyValuePair<string, string>> groups = new List<KeyValuePair<string, string>>();
            foreach (KeyValuePair<int, List<string>> kv in perField)
            {
                string wires = string.Join(", ", kv.Value);
                int gi = groups.FindIndex(x => x.Value == wires);
                if (gi >= 0) groups[gi] = new KeyValuePair<string, string>(groups[gi].Key + ", " + GwFieldNames[kv.Key], wires);
                else groups.Add(new KeyValuePair<string, string>(GwFieldNames[kv.Key], wires));
            }
            foreach (KeyValuePair<string, string> gr in groups)
                parts.Add(gr.Key + (gr.Value.Contains(",") ? " dei fili " : " del filo ") + gr.Value);
            if (other.Count > 0) parts.Add("schede " + string.Join(", ", other));
            List<string> users = new List<string>();
            foreach (SyVar v in Variables)
                if (!string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase) && ContainsName(v.Expr, name)) users.Add(v.Name);
            if (users.Count > 0) parts.Add("serve a calcolare " + string.Join(", ", users));
            return parts.Count == 0 ? "non usata nel modello" : string.Join("; ", parts);
        }

        public bool UsesVariable(string name)
        {
            foreach (GeomCard g in Geometry)
            {
                if (g.Src == null) continue;
                foreach (string f in g.Src)
                    if (ContainsName(f, name)) return true;
                if (g.GcSrc != null) foreach (string f in g.GcSrc) if (ContainsName(f, name)) return true;
            }
            foreach (SyVar v in Variables) if (!string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase) && ContainsName(v.Expr, name)) return true;
            return false;
        }

        public static bool ContainsName(string text, string name)
        {
            if (string.IsNullOrEmpty(text)) return false;
            int i = 0;
            while ((i = text.IndexOf(name, i, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                bool before = i == 0 || !(char.IsLetterOrDigit(text[i - 1]) || text[i - 1] == '_');
                int e = i + name.Length;
                bool after = e >= text.Length || !(char.IsLetterOrDigit(text[e]) || text[e] == '_');
                if (before && after) return true;
                i = e;
            }
            return false;
        }

        public static NecModel Load(string path)
        {
            string y = YanecFile.Sostituto(path);           // 0.4.30: .yanec (anche al posto di un file convertito)
            if (y != null) return YanecFile.Carica(y);
            NecModel m = Parse(File.ReadAllText(path));
            m.FileName = path;
            return m;
        }

        public static NecModel Parse(string text)
        {
            NecModel m = new NecModel();
            m.FreqCount = 1;
            bool geometryDone = false;
            bool haveFr = false;
            GeomCard lastGw = null;
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int lineNo = 0;
            foreach (string raw in lines)
            {
                lineNo++;
                string line = raw.TrimEnd();
                if (line.Trim().Length == 0) continue;
                string t = line.TrimStart();
                if (t.StartsWith("'") || t.StartsWith("#") || t.StartsWith("!")) continue;
                if (t.Length < 2) continue;
                string id = t.Substring(0, 2).ToUpperInvariant();
                string rest = t.Length > 2 ? t.Substring(2) : "";
                // commento in fondo alla riga come in 4nec2: "EX 0 4 1 0 -0.992 -0.128 'fase 187,4 gradi"
                if (id != "CM" && id != "CE") { int q = rest.IndexOf('\''); if (q >= 0) rest = rest.Substring(0, q); }
                try
                {
                    switch (id)
                    {
                        case "CM":
                        case "CE":
                            {
                                string c = rest.Trim();
                                // impostazioni di YA-NEC nei commenti
                                if (c.StartsWith("YA-NEC+ ", StringComparison.Ordinal))
                                {
                                    if (m.righeExtra.Count > 0) m.righeExtra[m.righeExtra.Count - 1] += " " + c.Substring(8).Trim();
                                }
                                else if (c.StartsWith("YA-NEC RASTR", StringComparison.Ordinal) || c.StartsWith("YA-NEC TRASF ", StringComparison.Ordinal) || c.StartsWith("YA-NEC RETEL ", StringComparison.Ordinal) || c.StartsWith("YA-NEC STUB ", StringComparison.Ordinal))
                                    m.righeExtra.Add(c.Substring(7).Trim());
                                else if (c.StartsWith("YANEC ", StringComparison.Ordinal))
                                {
                                    // 0.4.30: abaco della descrizione del .yanec (CM YANEC CHIAVE=valore): non e' un commento del modello
                                    string kv = c.Substring(6); int eqk = kv.IndexOf('=');
                                    if (eqk > 0) m.Scheda.Add(new KeyValuePair<string, string>(kv.Substring(0, eqk).Trim().ToUpperInvariant(), kv.Substring(eqk + 1).Trim()));
                                }
                                else if (c.StartsWith("YA-NEC FORMATO=", StringComparison.OrdinalIgnoreCase)) { }
                                else if (c.StartsWith("YA-NEC VAR ", StringComparison.OrdinalIgnoreCase))
                                {
                                    // descrizione di una variabile: CM YA-NEC VAR L: lunghezza totale
                                    string dv = c.Substring(11);
                                    int colon = dv.IndexOf(':');
                                    if (colon > 0) m.varDescr[dv.Substring(0, colon).Trim()] = dv.Substring(colon + 1).Trim();
                                }
                                else if (c.StartsWith("YA-NEC", StringComparison.OrdinalIgnoreCase)) m.ReadSetting(c.Substring(6).Trim());
                                else if (c.Length > 0) m.Comments.Add(c);
                                break;
                            }
                        case "GW":
                        case "GA":
                        case "GH":
                        case "GM":
                        case "GR":
                        case "GS":
                        case "GX":
                        case "SP":
                        case "SM":
                        case "SC":
                        case "GF":
                            {
                                string[] f = Fields(rest);
                                // 0.4.8: unita' come campo a se' (2lsloper.nec: "GW 1 11 0 -68 ft X Y -68 ft Z #12") solo se i campi sono troppi:
                                // "GS 0 0 ft" resta il fattore di scala ft
                                if (id == "GW" && f.Length > 9) f = Expr.UnisciUnita(f);
                                GeomCard g = new GeomCard();
                                g.Id = id;
                                g.Src = Pad(f, 0, 9);
                                g.I1 = m.ParseInt(f, 0);
                                g.I2 = m.ParseInt(f, 1);
                                g.F1 = m.ParseDouble(f, 2); g.F2 = m.ParseDouble(f, 3); g.F3 = m.ParseDouble(f, 4);
                                g.F4 = m.ParseDouble(f, 5); g.F5 = m.ParseDouble(f, 6); g.F6 = m.ParseDouble(f, 7);
                                g.F7 = m.ParseDouble(f, 8);
                                m.Geometry.Add(g);
                                lastGw = id == "GW" ? g : null;
                                break;
                            }
                        case "GC":
                            {
                                string[] f = Fields(rest);
                                if (lastGw == null) throw new NecException("Scheda GC senza un GW con raggio 0 prima.");
                                lastGw.HasGc = true;
                                lastGw.GcSrc = Pad(f, 2, 3);
                                lastGw.GcRdel = m.ParseDouble(f, 2);
                                lastGw.GcRad1 = m.ParseDouble(f, 3);
                                lastGw.GcRad2 = m.ParseDouble(f, 4);
                                lastGw = null;
                                break;
                            }
                        case "GE":
                            {
                                string[] f = Fields(rest);
                                m.GroundFlag = m.ParseInt(f, 0);
                                geometryDone = true;
                                break;
                            }
                        case "SY":
                            foreach (KeyValuePair<string, string> d in Expr.SplitDefinitions(rest))
                            {
                                if (d.Key.StartsWith("="))
                                {
                                    // "SY R-2=18": a sinistra un'espressione (4nec2). Va bene solo se vale gia' quanto scritto a destra.
                                    string sx = d.Key.Substring(1);
                                    double a, b;
                                    try { a = Expr.Eval(sx, m.varValues); b = Expr.Eval(d.Value, m.varValues); }
                                    catch (NecException) { throw new NecException("Scheda SY: nome di variabile non valido o riservato: '" + sx + "'"); }
                                    if (Math.Abs(a - b) > 1e-9 * Math.Max(1.0, Math.Abs(b)))
                                        throw new NecException("Scheda SY: '" + sx + "' non e' un nome di variabile e vale " + a.ToString("0.######", CI) + ", non " + b.ToString("0.######", CI) + ".");
                                    m.Warnings.Add("Riga " + lineNo + ": 'SY " + sx + "=" + d.Value + "' ignorata: a sinistra c'e' un'espressione, che vale gia' " + b.ToString("0.######", CI) + ".");
                                    continue;
                                }
                                m.DefineVariable(d.Key, d.Value);
                            }
                            break;
                        default:
                            {
                                if (!geometryDone && id != "EN")
                                    m.Warnings.Add("Riga " + lineNo + ": scheda " + id + " prima della fine della geometria (GE).");
                                string[] f = Fields(rest);
                                ControlCard c = new ControlCard();
                                c.Id = id;
                                // 4nec2 (Example3.nec): segmento della sorgente o del carico in percentuale del filo, "EX 0 2 50% 0 1 0"
                                if ((id == "EX" || id == "LD") && f.Length > 3)
                                    for (int q = 2; q <= 3; q++)
                                        if (f[q].EndsWith("%")) f[q] = m.SegDaPercento(m.ParseInt(f, 1), f[q]).ToString(CI);
                                c.I1 = m.ParseInt(f, 0); c.I2 = m.ParseInt(f, 1); c.I3 = m.ParseInt(f, 2); c.I4 = m.ParseInt(f, 3);
                                c.F1 = m.ParseDouble(f, 4); c.F2 = m.ParseDouble(f, 5); c.F3 = m.ParseDouble(f, 6);
                                c.F4 = m.ParseDouble(f, 7); c.F5 = m.ParseDouble(f, 8); c.F6 = m.ParseDouble(f, 9);
                                m.ApplyControl(c, lineNo, ref haveFr, line);
                                break;
                            }
                    }
                }
                catch (NecException ex)
                {
                    throw new NecException("Riga " + lineNo + " (" + line.Trim() + "): " + ex.Message);
                }
            }
            if (m.Geometry.Count == 0) throw new NecException("Il file non contiene schede di geometria.");
            m.LeggiRigheExtra();
            // 1.5.9 (IC8TEM 22/11/2026 22:31 «approvato»): senza scheda FR il NEC-2 originale calcola a 299,8 MHz (lambda = 1 m), come serve
            // ai modelli scritti in lunghezze d'onda (W1GHZ, DIPROD e DIPSPLSH); prima YA-NEC usava 144,3 MHz
            if (!haveFr)
            {
                m.FreqMHz = 299.7925;
                m.Warnings.Add("Nessuna scheda FR: frequenza 299,7925 MHz (lambda = 1 m) come il NEC-2; per un'altra frequenza aggiungi la scheda FR.");
            }
            foreach (double[] tr in m.trappole4nec2)
            {
                LoadSpec ld = m.Loads[(int)tr[0]];
                double f = tr[2] > 0 ? tr[2] : m.FreqMHz;
                ld.F1 = tr[1] * 2 * Math.PI * f * 1e6 * ld.F2;
                m.Warnings.Add(string.Format(CI, "LD 6 di 4nec2 (trappola LC sul filo {0}): RLC parallelo con R = Q x XL = {1:0.#} ohm a {2:0.###} MHz.", ld.Tag, ld.F1, f));
            }
            Isolati4nec2(m);
            if (m.calcestruzzoCm > 0 && m.Ground.Kind == GroundKind.Real)
            {
                m.Ground.Calcestruzzo = Math.Min(2, m.calcestruzzoCm);
                m.Ground.Name = TerreniEdifici.Nomi[m.Ground.Calcestruzzo];
            }
            // 1.5.9: se tutte le schede EX sono state scartate lo si dice subito, invece del generico "non ha sorgenti"
            if (m.Sources.Count == 0 && m.exIgnorate > 0)
                throw new NecException("Nessuna sorgente utilizzabile: le schede EX del file sono di un tipo che YA-NEC non calcola (" + m.exIgnorate + (m.exIgnorate == 1 ? " scheda" : " schede") + "). Usa EX 0 (tensione) o EX 5.");
            if (m.Networks.Count > 0 && m.Sources.Exists(q => q.Qds))
                m.Warnings.Add("EX 5 insieme a linee o reti (TL/NT): in questo caso la sorgente e' calcolata come EX 0, come tensione al centro del segmento.");
            if (m.ex6 > 0)
            {
                if (m.ex6 == m.Sources.Count) { m.CurrentSources = true; m.Warnings.Add("EX 6 di 4nec2 (sorgenti di corrente): lette come \"Sorgenti a corrente\" di YA-NEC."); }
                else m.Warnings.Add("EX 6 di 4nec2 insieme a sorgenti di tensione: YA-NEC non le mescola, tutte trattate come sorgenti di tensione.");
            }
            return m;
        }

        int ex6;   // sorgenti EX 6 di 4nec2 lette
        int exIgnorate;   // 1.5.9: schede EX scartate (tipo non calcolato o non valido)
        readonly List<double[]> trappole4nec2 = new List<double[]>();   // LD 6 di 4nec2: indice del carico, Q, frequenza
        readonly List<double[]> isolati4nec2 = new List<double[]>();    // LD 7 di 4nec2: tag, primo, ultimo, er, raggio esterno, riga

        // LD 7 di 4nec2 (0.4.2, approvato da IC8TEM il 13/11/2026): filo isolato con il metodo di L. B. Cebik (amod83) come lo
        // applica 4nec2 (help, "Wire coating"): induttanza distribuita LD 2 con
        //   L = 2e-7 (er R/r)^(1/12) (1 - 1/er) ln(R/r)  H/m,  r = raggio del filo, R = raggio con l'isolante.
        // Formula adatta a er tra 2 e 3,5 (plastiche). Un carico LD 2 per ogni tratto di segmenti consecutivi con lo stesso raggio.
        static void Isolati4nec2(NecModel m)
        {
            if (m.isolati4nec2.Count == 0) return;
            SegmentGeometry g;
            try { g = m.BuildGeometry(); }
            catch (NecException ex) { m.Warnings.Add("LD 7 di 4nec2 non convertito: " + ex.Message); return; }
            foreach (double[] d in m.isolati4nec2)
            {
                int tag = (int)d[0], da = (int)d[1], a = (int)d[2], riga = (int)d[5];
                double er = d[3], R = d[4];
                if (a == 0) a = da;
                if (er <= 1 || R <= 0)
                {
                    m.Warnings.Add("Riga " + riga + ": LD 7 con costante dielettrica <= 1 o raggio nullo: nessun effetto, il filo e' nudo.");
                    continue;
                }
                Dictionary<int, int> conta = new Dictionary<int, int>();
                int nCarichi = 0, troppoSottile = 0;
                LoadSpec cur = null; double rCur = -1; int segCur = -9, tagCur = -1;
                for (int i = 0; i < g.N; i++)
                {
                    int t = g.Tag[i];
                    int k; conta.TryGetValue(t, out k); k++; conta[t] = k;
                    bool scelto = tag == 0 ? (da == 0 || (i + 1 >= da && i + 1 <= a)) : (t == tag && (da == 0 || (k >= da && k <= a)));
                    if (!scelto) { cur = null; continue; }
                    double r = g.Bi[i];
                    if (t == 0) k = i + 1;   // fili senza tag: numero assoluto del segmento
                    if (R <= r) { troppoSottile++; cur = null; continue; }
                    if (cur != null && t == tagCur && k == segCur + 1 && Math.Abs(r - rCur) <= 1e-12 * r) { cur.To = k; segCur = k; continue; }
                    double L = 2e-7 * Math.Pow(er * R / r, 1.0 / 12) * (1 - 1 / er) * Math.Log(R / r);
                    cur = new LoadSpec { Type = 2, Tag = t, From = k, To = k, F1 = 0, F2 = L, F3 = 0, DaIsolante = true };
                    m.Loads.Add(cur); nCarichi++;
                    rCur = r; segCur = k; tagCur = t;
                }
                if (troppoSottile > 0)
                    m.Warnings.Add(string.Format(CI, "Riga {0}: LD 7 con raggio esterno {1:0.####} m non maggiore del raggio del filo su {2} segmenti: li lascio nudi (in 4nec2 forse e' il diametro).", riga, R, troppoSottile));
                if (nCarichi > 0)
                    m.Warnings.Add(string.Format(CI, "Riga {0}: LD 7 di 4nec2 (filo isolato, er {1:0.##}, raggio esterno {2:0.####} m, metodo di Cebik): {3} carichi LD 2 di induttanza distribuita.", riga, er, R, nCarichi));
            }
        }

        // 1.5.9 (IC8TEM 23/11/2026 00:56 «si procedi»): il NEC calcola ogni esecuzione (XQ, RP, NE, NH) con le schede lette fino a li';
        // YA-NEC ha un solo calcolo e prende la prima esecuzione. Prima le schede dopo l'esecuzione si sommavano (l'ultima FR, tutte
        // le EX): nei file NEC-4.2 di prova di momwire 5 su 99 davano un'impedenza diversa da quella stampata dal NEC.
        bool esecuzioneLetta, avvisoEsecuzioni;
        static readonly HashSet<string> SchedeDiEsecuzione = new HashSet<string> { "FR", "EX", "LD", "GN", "GD", "EK", "KH", "NT", "TL", "CP" };

        void ApplyControl(ControlCard c, int lineNo, ref bool haveFr, string line)
        {
            if (esecuzioneLetta && SchedeDiEsecuzione.Contains(c.Id))
            {
                if (!avvisoEsecuzioni)
                {
                    avvisoEsecuzioni = true;
                    Warnings.Add("Riga " + lineNo + ": il file ha piu' esecuzioni (schede dopo XQ, RP, NE o NH): YA-NEC calcola la prima, come la prima stampa del NEC; da qui le schede " +
                        "FR, EX, LD, GN, EK, TL e NT sono ignorate.");
                }
                return;
            }
            if (c.Id == "XQ" || c.Id == "RP" || c.Id == "NE" || c.Id == "NH") esecuzioneLetta = true;
            switch (c.Id)
            {
                case "FR":
                    FreqMultiplicative = c.I1 == 1;
                    FreqCount = c.I2 <= 0 ? 1 : c.I2;
                    FreqMHz = c.F1;
                    FreqStep = c.F2;
                    if (FreqMHz <= 0) throw new NecException("Frequenza non valida.");
                    haveFr = true;
                    break;
                case "EX":
                    if (c.I1 == 6)
                    {
                        // 4nec2: EX 6 = sorgente di corrente (4nec2 la converte per il NEC-2); in YA-NEC: "Sorgenti a corrente"
                        ex6++;
                        Sources.Add(new SourceSpec { Tag = c.I2, Seg = c.I3, V = new Complex(c.F1, c.F2) });
                        break;
                    }
                    if (c.I1 != 0 && c.I1 != 5)
                    {
                        exIgnorate++;
                        if (c.I1 >= 1 && c.I1 <= 4) Warnings.Add("Riga " + lineNo + ": eccitazione EX tipo " + c.I1 + " (onda piana o dipolo elementare) non ancora supportata: ignorata.");
                        else Warnings.Add("Riga " + lineNo + ": tipo di eccitazione EX " + c.I1 + " non valido (il NEC-2 usa da 0 a 5): ignorata.");
                        break;
                    }
                    Sources.Add(new SourceSpec { Tag = c.I2, Seg = c.I3, V = new Complex(c.F1, c.F2), Qds = c.I1 == 5 });
                    break;
                case "LD":
                    if (c.I1 == -1) { Loads.Clear(); WireConductivity = 0; break; }
                    // LD 5 su tutta la struttura = materiale dei fili
                    if (c.I1 == 5 && c.I2 == 0 && c.I3 == 0 && c.I4 == 0 && WireConductivity == 0 && c.F1 > 0)
                    {
                        WireConductivity = c.F1;
                        WireMaterial = "dal file";
                        break;
                    }
                    if (c.I1 == 5 && c.F1 <= 0)
                    {
                        // 1.5.9: conducibilita' nulla o negativa = resistenza infinita, il calcolo da' NaN
                        Warnings.Add("Riga " + lineNo + ": LD 5 con conducibilita' nulla o negativa (" + c.F1.ToString(CI) + " S/m): ignorata.");
                        break;
                    }
                    if (c.I1 == 6)
                    {
                        // 4nec2 LD 6 = trappola LC: F1 = Q della bobina (0 -> 100), F2 = L in H, F3 = C in F, F4 = frequenza per la perdita
                        // (0 -> prima scheda FR). 4nec2 la converte in RLC parallelo (LD 1) con R = Q * 2 pi f L: qui alla fine della lettura.
                        trappole4nec2.Add(new double[] { Loads.Count, c.F1 > 0 ? c.F1 : 100, c.F4 });
                        Loads.Add(new LoadSpec { Type = 1, Tag = c.I2, From = c.I3, To = c.I4 == 0 ? c.I3 : c.I4, F1 = 0, F2 = c.F2, F3 = c.F3 });
                        break;
                    }
                    if (c.I1 == 7)
                    {
                        // 4nec2 LD 7 = filo isolato: F1 = costante dielettrica, F2 = raggio esterno (filo + isolante) in m.
                        // Convertito alla fine della lettura, quando si conoscono i raggi dei segmenti (Isolati4nec2).
                        isolati4nec2.Add(new double[] { c.I2, c.I3, c.I4, c.F1, c.F2, lineNo });
                        break;
                    }
                    Loads.Add(new LoadSpec { Type = c.I1, Tag = c.I2, From = c.I3, To = c.I4 == 0 ? c.I3 : c.I4, F1 = c.F1, F2 = c.F2, F3 = c.F3 });
                    break;
                case "GN":
                    if (c.I1 == -1) { Ground = new GroundSpec { Kind = GroundKind.FreeSpace }; break; }
                    if (c.I1 == 1) Ground = new GroundSpec { Kind = GroundKind.Perfect };
                    else
                    {
                        // GN 3 di 4nec2 = "MiniNec ground" (help di 4nec2): correnti e impedenza su terreno perfetto (GN 1), campo lontano
                        // con il terreno reale (4nec2 aggiunge GD 0 0 0 0 er sigma); valido per le verticali, non per fili orizzontali sotto 0,2 lambda
                        Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = c.F1, Sigma = c.F2, Sommerfeld = c.I1 == 2, MiniNec = c.I1 == 3 };
                        // 1.5.9: con er nullo o negativo il calcolo da' NaN (controllo come nec2-rs)
                        if (c.F1 <= 0) throw new NecException("Costante dielettrica del terreno non valida (" + c.F1.ToString(CI) + "): deve essere maggiore di 0.");
                        if (c.I1 == 3) Warnings.Add("Riga " + lineNo + ": GN 3 di 4nec2 (MiniNec ground): impedenza su terreno perfetto, diagramma con er " + c.F1.ToString(CI) + " e sigma " + c.F2.ToString(CI) + " S/m.");
                    }
                    // 0.4.30: schermo di radiali del NEC-2 (GN con NRADL: F3 lunghezza, F4 raggio dei fili), solo con il coefficiente di riflessione
                    if (c.I2 != 0)
                    {
                        if (Ground.Kind == GroundKind.Real && !Ground.Sommerfeld && !Ground.MiniNec && c.F3 > 0 && c.F4 > 0)
                        {
                            Ground.Nradl = c.I2; Ground.SchermoL = c.F3; Ground.SchermoR = c.F4;
                            Warnings.Add(string.Format(CI, "Riga {0}: schermo di radiali del NEC-2: {1} fili lunghi {2} m, raggio {3} m (approssimazione di impedenza superficiale; i radiali non sono modellati come fili).", lineNo, c.I2, c.F3, c.F4));
                        }
                        else Warnings.Add("Riga " + lineNo + ": schermo di radiali (GN con NRADL) valido solo con il terreno a coefficiente di riflessione (GN 0) e con lunghezza e raggio maggiori di zero: ignorato (come nec2c con Sommerfeld).");
                    }
                    break;
                case "EK":
                    ExtendedKernel = c.I1 != -1;
                    break;
                case "KH":
                    Rkh = c.F1;
                    break;
                case "RP":
                case "XQ":
                case "EN":
                case "PT":
                case "PQ":
                case "NX":
                case "PL":
                    OtherCards.Add(line.Trim());
                    break;
                case "NE":
                case "NH":
                    Warnings.Add("Riga " + lineNo + ": campi vicini (" + c.Id + ") non ancora calcolati: scheda ignorata.");
                    break;
                case "NT":
                case "TL":
                    // 0.4.0: linee di trasmissione e reti a due porte calcolate dal motore (come NETWK di nec2c)
                    if (c.I1 == -1) { Networks.Clear(); break; }
                    Networks.Add(new NetworkSpec { IsTl = c.Id == "TL", Tag1 = c.I1, Seg1 = c.I2, Tag2 = c.I3, Seg2 = c.I4, F1 = c.F1, F2 = c.F2, F3 = c.F3, F4 = c.F4, F5 = c.F5, F6 = c.F6 });
                    break;
                case "GD":
                case "CP":
                case "WG":
                    Warnings.Add("Riga " + lineNo + ": scheda " + c.Id + " non supportata: ignorata.");
                    break;
                default:
                    Warnings.Add("Riga " + lineNo + ": scheda sconosciuta " + c.Id + ": ignorata.");
                    break;
            }
        }

        // righe "CM YA-NEC RASTR / RASTRFILO / TRASF / RETEL" lette alla fine (servono le variabili e le schede gia' lette)
        void LeggiRigheExtra()
        {
            foreach (string e in righeExtra)
            {
                try
                {
                    if (e.StartsWith("RASTRFILO ", StringComparison.Ordinal))
                    {
                        string[] f = Fields(e.Substring(10));
                        GeomCard g = new GeomCard { Id = "GW", Src = Pad(f, 0, 9) };
                        g.I1 = ParseInt(f, 0); g.I2 = ParseInt(f, 1);
                        g.F1 = ParseDouble(f, 2); g.F2 = ParseDouble(f, 3); g.F3 = ParseDouble(f, 4); g.F4 = ParseDouble(f, 5); g.F5 = ParseDouble(f, 6); g.F6 = ParseDouble(f, 7); g.F7 = ParseDouble(f, 8);
                        int primo = Geometry.FindIndex(q => q.Id == "GW" && q.I1 == g.I1);
                        if (primo < 0) { Warnings.Add("Riga RASTRFILO per il tag " + g.I1 + ": tubi non trovati nel file."); continue; }
                        Geometry.RemoveAll(q => q.Id == "GW" && q.I1 == g.I1);
                        Geometry.Insert(Math.Min(primo, Geometry.Count), g);
                    }
                    else if (e.StartsWith("RASTR ", StringComparison.Ordinal))
                    {
                        Rastremazione r = OggettiExtra.LeggiRastremazione(e.Substring(6));
                        if (r != null) Rastremazioni.Add(r); else Warnings.Add("Riga di rastremazione non valida: " + e);
                    }
                    else if (e.StartsWith("STUB ", StringComparison.Ordinal))
                    {
                        Stub st = Stub.Leggi(e.Substring(5));
                        if (st == null) { Warnings.Add("Riga di stub non valida: " + e); continue; }
                        // la TL scritta per gli altri programmi ha gia' l'ammettenza dello stub alla frequenza del file: si toglie, YA-NEC la ricalcola
                        foreach (NetworkSpec n in Networks)
                        {
                            if (!n.IsTl) continue;
                            if (n.Tag1 == st.Tag && n.Seg1 == st.Seg) { n.F3 = 0; n.F4 = 0; break; }
                            if (n.Tag2 == st.Tag && n.Seg2 == st.Seg) { n.F5 = 0; n.F6 = 0; break; }
                        }
                        Stubs.Add(st);
                    }
                    else if (e.StartsWith("TRASF ", StringComparison.Ordinal) || e.StartsWith("RETEL ", StringComparison.Ordinal))
                    {
                        bool tr = e.StartsWith("TRASF ", StringComparison.Ordinal);
                        Trasformatore t = tr ? OggettiExtra.LeggiTrasformatore(e.Substring(6)) : null;
                        ReteL q = tr ? null : OggettiExtra.LeggiReteL(e.Substring(6));
                        if (t == null && q == null) { Warnings.Add("Riga non valida: " + e); continue; }
                        int t1 = tr ? t.Tag1 : q.Tag1, s1 = tr ? t.Seg1 : q.Seg1, t2 = tr ? t.Tag2 : q.Tag2, s2 = tr ? t.Seg2 : q.Seg2;
                        // la scheda NT scritta per gli altri programmi si toglie: YA-NEC la ricalcola
                        int k = Networks.FindIndex(n => !n.IsTl && n.Tag1 == t1 && n.Seg1 == s1 && n.Tag2 == t2 && n.Seg2 == s2);
                        if (k >= 0) Networks.RemoveAt(k);
                        if (tr) Trasformatori.Add(t); else RetiL.Add(q);
                    }
                }
                catch (Exception ex) { Warnings.Add("Riga \"" + e + "\": " + ex.Message); }
            }
            righeExtra.Clear();
        }

        /// <summary>Segmento al p % del filo con il tag dato (come EZNEC e Ez2Nec: floor(p n / 100) + 1).</summary>
        int SegDaPercento(int tag, string testo)
        {
            double p = Expr.Eval(testo.TrimEnd('%'), varValues);
            GeomCard g = Geometry.Find(q => q.Id == "GW" && q.I1 == tag);
            if (g == null) throw new NecException("percentuale " + testo + " su un filo (tag " + tag + ") che non c'e'.");
            int n = Math.Max(1, g.I2);
            return Math.Max(1, Math.Min(n, (int)Math.Floor(p * n / 100.0) + 1));
        }

        void ReadSetting(string s)
        {
            foreach (string part in s.Split(new[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string k = part.Substring(0, eq).ToUpperInvariant();
                string v = part.Substring(eq + 1);
                double d;
                if (!double.TryParse(v, NumberStyles.Float, CI, out d)) continue;
                if (k == "Z0" && d > 0) Z0 = d;
                if (k == "SIGMA" && d >= 0) WireConductivity = d;
                if (k == "CORRENTI") CurrentSources = d != 0;
                if (k == "CARICHI") UseLoads = d != 0;
                if (k == "LEESON") Leeson = d != 0;
                if (k == "GIUNZIONI_NEC4") GiunzioniNec4 = d != 0;
                if (k == "CALCESTRUZZO") calcestruzzoCm = (int)d;
            }
        }

        // ---------------------------------------------------------------------------------------------
        // scrittura .nec
        // ---------------------------------------------------------------------------------------------
        static string F(double v) { return v != 0 && Math.Abs(v) < 1e-3 ? v.ToString("G9", CI) : v.ToString("0.######", CI); }   // valori piccoli (C in farad, L in henry) in notazione esponenziale

        static List<string> A80(string c)
        {
            List<string> r = new List<string>();
            string t = c.Trim();
            while (t.Length > 77)
            {
                int k = t.LastIndexOf(' ', 77);
                if (k < 20) k = 77;
                r.Add(t.Substring(0, k).TrimEnd());
                t = t.Substring(k).TrimStart();
            }
            r.Add(t);
            return r;
        }

        public string ToNec()
        {
            StringBuilder sb = new StringBuilder();
            if (Comments.Count == 0) sb.AppendLine("CM YA-NEC");
            // righe CM al massimo di 80 caratteri: nec2c (e il NEC-2 originale) leggono 80 colonne e il resto diventa una scheda sbagliata
            foreach (string c in Comments) foreach (string pezzo in A80(c)) sb.AppendLine("CM " + pezzo);
            sb.AppendLine(string.Format(CI, "CM YA-NEC Z0={0}", F(Z0)));
            if (CurrentSources) sb.AppendLine("CM YA-NEC CORRENTI=1");
            if (!UseLoads) sb.AppendLine("CM YA-NEC CARICHI=0");
            if (Leeson) sb.AppendLine("CM YA-NEC LEESON=1");
            if (GiunzioniNec4) sb.AppendLine("CM YA-NEC GIUNZIONI_NEC4=1");
            if (Ground.Kind == GroundKind.Real && Ground.Calcestruzzo > 0) sb.AppendLine("CM YA-NEC CALCESTRUZZO=" + Ground.Calcestruzzo);
            foreach (SyVar v in Variables)
                if (!string.IsNullOrWhiteSpace(v.Descr)) sb.AppendLine("CM YA-NEC VAR " + v.Name + ": " + v.Descr.Replace("\r", " ").Replace("\n", " ").Trim());
            List<string> extra = new List<string>();
            foreach (Rastremazione r in Rastremazioni) extra.Add(OggettiExtra.RigaRastremazione(r));
            foreach (GeomCard g in Geometry)
                if (g.Id == "GW" && g.F7 < 0 && Rastremazioni.Count > 0)
                    extra.Add("YA-NEC RASTRFILO " + (g.Src != null ? string.Join(" ", g.Src) : string.Format(CI, "{0} {1} {2} {3} {4} {5} {6} {7} {8}", g.I1, g.I2, F(g.F1), F(g.F2), F(g.F3), F(g.F4), F(g.F5), F(g.F6), F(g.F7))));
            foreach (Trasformatore t in Trasformatori) extra.Add(OggettiExtra.RigaTrasformatore(t));
            foreach (ReteL q in RetiL) extra.Add(OggettiExtra.RigaReteL(q));
            foreach (Stub st in Stubs) extra.Add(Stub.Riga(st));
            foreach (string e in extra)
            {
                // righe CM di 80 caratteri al massimo: il seguito va su righe "CM YA-NEC+"
                string t = e;
                bool prima = true;
                while (t.Length > 0)
                {
                    int max = prima ? 77 : 69;
                    string pezzo = t;
                    if (t.Length > max) { int k = t.LastIndexOf(' ', max); if (k < 10) k = max; pezzo = t.Substring(0, k); }
                    sb.AppendLine((prima ? "CM " : "CM YA-NEC+ ") + pezzo.Trim());
                    t = t.Substring(pezzo.Length).Trim();
                    prima = false;
                }
            }
            sb.AppendLine("CE");
            // variabili (una per riga, come in 4nec2 e xnec2c)
            foreach (SyVar v in Variables) sb.AppendLine("SY " + v.Name + "=" + v.Expr);
            foreach (GeomCard g in Geometry)
            {
                if (g.Id == "GW" && g.F7 < 0 && Rastremazioni.Count > 0)
                {
                    // filo rastremato: nel file i tubi veri, letti da tutti i programmi (YA-NEC lo ricompone con la riga RASTRFILO)
                    try
                    {
                        foreach (GeomCard q in OggettiExtra.Espandi(this, new List<GeomCard> { g }))
                            sb.AppendLine(string.Format(CI, "GW {0} {1} {2} {3} {4} {5} {6} {7} {8}", q.I1, q.I2, F(q.F1), F(q.F2), F(q.F3), F(q.F4), F(q.F5), F(q.F6), F(q.F7)));
                        continue;
                    }
                    catch (NecException) { }
                }
                // 1.5.3: un campo di testo vuoto (null) spostava tutti i campi dopo di lui (segmenti letti come 0): allora si scrivono i numeri
                if (g.Src != null && Array.TrueForAll(g.Src, x => !string.IsNullOrWhiteSpace(x))) sb.AppendLine(g.Id + " " + string.Join(" ", g.Src));
                else
                    sb.AppendLine(string.Format(CI, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}", g.Id, g.I1, g.I2,
                        F(g.F1), F(g.F2), F(g.F3), F(g.F4), F(g.F5), F(g.F6), F(g.F7)));
                if (g.HasGc)
                {
                    if (g.GcSrc != null) sb.AppendLine("GC 0 0 " + string.Join(" ", g.GcSrc));
                    else sb.AppendLine(string.Format(CI, "GC 0 0 {0} {1} {2}", F(g.GcRdel), F(g.GcRad1), F(g.GcRad2)));
                }
            }
            sb.AppendLine("GE " + GroundFlag);
            if (ExtendedKernel) sb.AppendLine("EK");
            if (Math.Abs(Rkh - 1.0) > 1e-12) sb.AppendLine("KH 0 0 0 0 " + F(Rkh));
            if (WireConductivity > 0) sb.AppendLine("LD 5 0 0 0 " + WireConductivity.ToString("0.###E+0", CI));
            foreach (LoadSpec l in Loads)
                sb.AppendLine(string.Format(CI, "LD {0} {1} {2} {3} {4} {5} {6}", l.Type, l.Tag, l.From, l.To, F(l.F1), F(l.F2), F(l.F3)));
            if (Ground.Kind == GroundKind.Perfect) sb.AppendLine("GN 1");
            else if (Ground.Kind == GroundKind.Real && Ground.Nradl > 0 && !Ground.Sommerfeld && !Ground.MiniNec)
                sb.AppendLine(string.Format(CI, "GN 0 {0} 0 0 {1} {2} {3} {4}", Ground.Nradl, F(Ground.Epsr), F(Ground.Sigma), F(Ground.SchermoL), F(Ground.SchermoR)));
            else if (Ground.Kind == GroundKind.Real) sb.AppendLine(string.Format(CI, "GN {2} 0 0 0 {0} {1}", F(Ground.Epsr), F(Ground.Sigma), Ground.MiniNec ? 3 : (Ground.Sommerfeld ? 2 : 0)));
            foreach (NetworkSpec nw in EffectiveNetworks(FreqMHz))
                sb.AppendLine(string.Format(CI, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}", nw.IsTl ? "TL" : "NT", nw.Tag1, nw.Seg1, nw.Tag2, nw.Seg2,
                    nw.IsTl ? F(nw.F1) : nw.F1.ToString("G10", CI), nw.IsTl ? F(nw.F2) : nw.F2.ToString("G10", CI), nw.IsTl ? F(nw.F3) : nw.F3.ToString("G10", CI),
                    nw.IsTl ? F(nw.F4) : nw.F4.ToString("G10", CI), nw.IsTl ? F(nw.F5) : nw.F5.ToString("G10", CI), nw.IsTl ? F(nw.F6) : nw.F6.ToString("G10", CI)));
            foreach (SourceSpec s in Sources)
                sb.AppendLine(string.Format(CI, "EX {4} {0} {1} 0 {2} {3}", s.Tag, s.Seg, F(s.V.Real), F(s.V.Imaginary), s.Qds ? 5 : 0));
            sb.AppendLine(string.Format(CI, "FR {0} {1} 0 0 {2} {3}", FreqMultiplicative ? 1 : 0, Math.Max(1, FreqCount), F(FreqMHz), F(FreqStep)));
            bool hasRp = false;
            foreach (string o in OtherCards)
            {
                if (o.StartsWith("EN", StringComparison.OrdinalIgnoreCase)) continue;
                if (o.StartsWith("RP", StringComparison.OrdinalIgnoreCase)) hasRp = true;
                sb.AppendLine(o);
            }
            if (!hasRp) sb.AppendLine(Ground.Kind == GroundKind.FreeSpace ? "RP 0 37 73 1000 0 0 5 5" : "RP 0 19 73 1000 0 0 5 5");
            sb.AppendLine("EN");
            return sb.ToString();
        }
    }
}
