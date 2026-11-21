// YA-NEC - finestra principale: apertura dei modelli, calcolo, sweep, aggiornamento delle viste
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        // ------------------------------------------------------------------------------------------------
        // esempi nella cartella data/modelli
        // ------------------------------------------------------------------------------------------------
        List<string> ExampleNames()
        {
            List<string> l = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(AppPaths.ModelsDir))
                {
                    string e = Path.GetExtension(f).ToLowerInvariant();
                    if (e == ".yanec" || e == ".nec" || e == ".maa" || e == ".weq" || e == ".ez" || e == ".yag" || e == ".ant")
                    {
                        // 0.4.30: un file nel formato vecchio rimasto accanto al suo .yanec (aggiornamento sopra la cartella vecchia) non si elenca
                        if (e != ".yanec")
                        {
                            string b0 = Path.Combine(Path.GetDirectoryName(f) ?? "", Path.GetFileNameWithoutExtension(f));
                            if (File.Exists(b0 + ".yanec") || File.Exists(b0 + " (" + YanecFile.Etichetta(e) + ").yanec")) continue;
                        }
                        l.Add(Path.GetFileName(f));
                    }
                }
            }
            catch { }
            l.Sort(StringComparer.OrdinalIgnoreCase);
            return l;
        }

        // gli esempi distribuiti con il programma (cartella "modelli" accanto all'eseguibile) vanno in data/modelli se mancano
        void CopyExamples()
        {
            try
            {
                string src = Path.Combine(AppContext.BaseDirectory, "modelli");
                if (Directory.Exists(src))
                    foreach (string f in Directory.GetFiles(src))
                    {
                        string d = Path.Combine(AppPaths.ModelsDir, Path.GetFileName(f));
                        if (!File.Exists(d)) File.Copy(f, d);
                    }
                building = true;
                cmbEsempi.ItemsSource = ExampleNames();
                building = false;
            }
            catch { building = false; }
        }

        static double ParseNum(string s, double def)
        {
            if (string.IsNullOrWhiteSpace(s)) return def;
            double v;
            if (double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
            return def;
        }

        static string Fmt(double v, int dec) { return Draw.N(v, dec); }

        /// <summary>Toglie il "-0,00": i valori piu' piccoli di 0,005 diventano 0.</summary>
        static double Nz(double v) { return Math.Abs(v) < 0.005 ? 0 : v; }

        // ------------------------------------------------------------------------------------------------
        // apertura e salvataggio
        // ------------------------------------------------------------------------------------------------
        async Task OpenModelDialog()
        {
            if (!await ChiediSeNonSalvato("Cosa faccio prima di aprire un altro modello?", "apri")) return;   // 0.5.3
            try
            {
                IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Apri un modello d'antenna",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Modelli NEC, MMANA, AutoEZ, EZNEC, YO, AO") { Patterns = new[] { "*.yanec", "*.nec", "*.NEC", "*.maa", "*.MAA", "*.weq", "*.WEQ", "*.ez", "*.EZ", "*.yag", "*.YAG", "*.ant", "*.ANT" } },
                        new FilePickerFileType("NEC2") { Patterns = new[] { "*.nec", "*.NEC" } },
                        new FilePickerFileType("MMANA") { Patterns = new[] { "*.maa", "*.MAA" } },
                        new FilePickerFileType("AutoEZ / MultiNEC") { Patterns = new[] { "*.weq", "*.WEQ" } },
                        new FilePickerFileType("EZNEC") { Patterns = new[] { "*.ez", "*.EZ" } },
                        new FilePickerFileType("YO di K6STI (.yag)") { Patterns = new[] { "*.yag", "*.YAG" } },
                        new FilePickerFileType("AO / NEC-Wires di K6STI (.ant)") { Patterns = new[] { "*.ant", "*.ANT" } },
                        new FilePickerFileType("Tutti i file") { Patterns = new[] { "*" } }
                    }
                });
                if (files == null || files.Count == 0) return;
                string path = files[0].TryGetLocalPath();
                if (path == null) { Status("Impossibile leggere il file scelto.", true); return; }
                EsempiVuoto();
                LoadModelFile(path);
            }
            catch (Exception ex) { Status("Errore nell'apertura: " + ex.Message, true); }
        }

        // "Salva con nome" in tutti i formati (bozzetto approvato alla lettera)
        async Task SaveModelDialog()
        {
            if (model == null) { Status("Nessun modello da salvare.", true); return; }
            string dir = AppPaths.DataDir;
            try { if (!string.IsNullOrEmpty(model.FileName) && Path.IsPathRooted(model.FileName)) dir = Path.GetDirectoryName(model.FileName); else dir = Path.Combine(AppPaths.DataDir, "modelli"); } catch { }
            string nome = string.IsNullOrEmpty(model.FileName) ? "modello" : Path.GetFileNameWithoutExtension(model.FileName);
            SalvaConNome dlg = new SalvaConNome(model, dir, nome);
            await dlg.ShowDialog(this);
            if (dlg.Risultato == null) return;
            SegnaSalvato();
            Registro.Scrivi("AZIONE", "salvato " + dlg.Risultato);
            string msg = "Modello salvato: " + dlg.Risultato;
            if (dlg.Perdite.Count > 0) msg += " (" + dlg.Perdite.Count + " avvisi: " + string.Join(" ", dlg.Perdite) + ")";
            if (dlg.Riapri)
            {
                // riapre il file scritto e confronta Z e guadagno con il modello aperto
                try
                {
                    NecModel m2;
                    List<MmanaWireInfo> w2;
                    string path = dlg.Risultato;
                    if (path.EndsWith(".maa", StringComparison.OrdinalIgnoreCase)) m2 = MmanaImport.Load(path, (double)(numSegL.Value ?? 40), out w2);
                    else if (path.EndsWith(".weq", StringComparison.OrdinalIgnoreCase)) m2 = EzImport.LoadWeq(path);
                    else if (path.EndsWith(".ez", StringComparison.OrdinalIgnoreCase)) m2 = EzImport.LoadEz(path);
                    else m2 = NecModel.Load(path);
                    NecModel m1 = model;
                    Tuple<RunResult, RunResult> rr = await Task.Run(() => Tuple.Create(Analysis.Run(m1, m1.BuildGeometry(), m1.FreqMHz, 5.0), Analysis.Run(m2, m2.BuildGeometry(), m2.FreqMHz, 5.0)));
                    RunResult a = rr.Item1, b = rr.Item2;
                    double dz = Complex.Abs(a.Zin - b.Zin), dg = Math.Abs(a.GmaxDbi - b.GmaxDbi);
                    msg += "  Confronto dopo la riapertura: Z " + Fmt(a.Zin.Real, 1) + (a.Zin.Imaginary >= 0 ? "+j" : "-j") + Fmt(Math.Abs(a.Zin.Imaginary), 1) +
                        " / " + Fmt(b.Zin.Real, 1) + (b.Zin.Imaginary >= 0 ? "+j" : "-j") + Fmt(Math.Abs(b.Zin.Imaginary), 1) + ", G " + Fmt(a.GmaxDbi, 2) + " / " + Fmt(b.GmaxDbi, 2) + " dBi" +
                        (dz < 0.5 && dg < 0.05 ? ": uguali." : ": diversi (vedi gli avvisi del formato).");
                }
                catch (Exception ex) { msg += "  Confronto non riuscito: " + ex.Message; }
            }
            Status(msg, dlg.Perdite.Count > 0);
        }

        async Task SaveModelDialogNec()
        {
            if (model == null) { Status("Nessun modello da salvare.", true); return; }
            try
            {
                string name = string.IsNullOrEmpty(model.FileName) ? "modello.nec" : Path.GetFileNameWithoutExtension(model.FileName) + ".nec";
                IStorageFile f = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Salva il modello in formato NEC2",
                    SuggestedFileName = name,
                    DefaultExtension = "nec",
                    FileTypeChoices = new[] { new FilePickerFileType("NEC2") { Patterns = new[] { "*.nec" } } }
                });
                if (f == null) return;
                string path = f.TryGetLocalPath();
                if (path == null) { Status("Impossibile scrivere nel percorso scelto.", true); return; }
                using (Prestazioni.Misura("Salvataggio del modello", Path.GetFileName(path))) File.WriteAllText(path, model.ToNec());
                SegnaSalvato();
                Registro.Scrivi("AZIONE", "salvato " + path);
                Status("Modello salvato: " + path);
            }
            catch (Exception ex) { Status("Errore nel salvataggio: " + ex.Message, true); }
        }

        void LoadModelFile(string path)
        {
            try
            {
                Registro.Scrivi("AZIONE", "apertura del modello " + path);
                NecModel m;
                List<MmanaWireInfo> wires = null;
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".yanec" || YanecFile.E(path)) m = YanecFile.Carica(path, (double)(numSegL.Value ?? 40), out wires);   // 0.4.30
                else if (ext == ".maa") m = MmanaImport.Load(path, (double)(numSegL.Value ?? 40), out wires);
                else if (ext == ".weq") m = EzImport.LoadWeq(path);
                else if (ext == ".ez") m = EzImport.LoadEz(path);
                else if (ext == ".yag") m = K6stiImport.LoadYag(path);
                else if (ext == ".ant") m = K6stiImport.LoadAnt(path);
                else
                {
                    using (Prestazioni.Misura("Lettura del modello", Path.GetFileName(path))) m = NecModel.Load(path);
                }
                using (Prestazioni.Misura("Preparazione del modello a video", m.Geometry.Count + " schede")) SetModel(m, wires);
                SegnaSalvato();   // 0.5.3: appena aperto il modello e' uguale al file
                Registro.Scrivi("MODELLO", string.Format(CultureInfo.InvariantCulture, "formato {0}, {1} schede di geometria, {2} variabili, {3} sorgenti, {4} carichi, {5:0.###} MHz, Z0 {6} ohm, terreno {7}",
                    m.SourceFormat, m.Geometry.Count, m.Variables.Count, m.Sources.Count, m.Loads.Count, m.FreqMHz, m.Z0, m.Ground.Kind));
                foreach (string w in m.Warnings) Registro.Scrivi("MODELLO", "avviso: " + w);
                Status((m.SourceFormat == "MMANA" ? MmanaImport.ImportNote + "  " : "") + "Modello caricato: " + Path.GetFileName(path));
                CalcolaAllApertura();
            }
            catch (Exception ex)
            {
                Status("Errore nel modello: " + ex.Message, true);
                lastError = ex.Message;
                UpdateNotes();
            }
        }

        void SetModel(NecModel m, List<MmanaWireInfo> wires)
        {
            model = m;
            mmWires = wires;
            run = null;
            GeoSvuotaStoria();   // Annulla e Ripeti della barra Geometria valgono per il modello aperto
            geoSel.Clear();
            sweep = null;
            lastError = null;
            double f = m.FreqMHz;
            List<double> fl = m.Frequencies();
            building = true;
            txtFreq.Text = Fmt(f, 3);
            if (fl.Count > 1)
            {
                // sweep del file (scheda FR): centro e span in kHz
                double c = (fl[0] + fl[fl.Count - 1]) / 2;
                txtFreq.Text = Fmt(c, 3);
                txtSpan.Text = Fmt((fl[fl.Count - 1] - fl[0]) * 1000.0, 1);
                txtNpts.Text = fl.Count.ToString(CultureInfo.InvariantCulture);
                f = c;
            }
            else
            {
                txtSpan.Text = Fmt(f * 0.02 * 1000.0, 1);   // +-1 %
                txtNpts.Text = "41";
            }
            // la banda che contiene la frequenza del modello viene solo indicata, la frequenza resta quella del file
            int bi = Bands.IndexOf(f);
            cmbBand.SelectedIndex = bi + 1;
            building = false;
            lblFile.Text = Path.GetFileName(m.FileName);
            if (txtNec != null) txtNec.Text = m.ToNec();
            RefreshVarsPanel();
            RefreshWiresTable();
            SyncToolbar();
        }

        // scelta della banda: imposta il centro (operativo o aritmetico) e lo span in kHz, poi ricalcola
        void ApplyBand()
        {
            int i = cmbBand.SelectedIndex - 1;
            Registro.Scrivi("AZIONE", "banda: " + (i < 0 || i >= Bands.All.Count ? "Libera" : Bands.All[i].Name) + ", centro " + (cmbCenter.SelectedIndex == 1 ? "aritmetico" : "operativo"));
            if (i < 0 || i >= Bands.All.Count) return;   // "Libera": frequenza e span restano quelli scritti
            Band bd = Bands.All[i];
            double f = cmbCenter.SelectedIndex == 1 ? bd.Arithmetic : bd.Operating;
            bool old = building;
            building = true;
            txtFreq.Text = Fmt(f, f < 1 ? 5 : (f < 100 ? 4 : 3));
            txtSpan.Text = Fmt(bd.SpanKHz(f), 1);
            building = old;
            if (model != null) Calculate(true);
        }

        // con una frequenza scritta a mano fuori dalla banda scelta, la banda torna "Libera"
        void CheckFreeFrequency(double f)
        {
            int i = cmbBand.SelectedIndex - 1;
            if (i < 0 || i >= Bands.All.Count) return;
            if (Bands.All[i].Contains(f)) return;
            bool old = building;
            building = true;
            int bi = Bands.IndexOf(f);
            cmbBand.SelectedIndex = bi + 1;
            building = old;
        }

        // riporta il modello nei comandi (Z0, terreno, materiale, kernel)
        void SyncToolbar()
        {
            if (model == null || numZ0 == null) return;
            bool old = building;
            building = true;
            numZ0.Value = (decimal)Math.Max(1, Math.Min(10000, model.Z0));
            chkEk.IsChecked = model.ExtendedKernel;
            if (model.Ground.Kind == GroundKind.FreeSpace) cmbGround.SelectedIndex = 0;
            else if (model.Ground.Kind == GroundKind.Perfect) cmbGround.SelectedIndex = 1;
            else
            {
                int idx = model.Ground.Name == "Personalizzato" ? 3 + Grounds.Length : 2;
                for (int i = 0; i < PrimoSolaio; i++)
                    if (Math.Abs(Grounds[i].Item2 - model.Ground.Epsr) < 1e-9 && Math.Abs(Grounds[i].Item3 - model.Ground.Sigma) < 1e-12) idx = 3 + i;
                if (model.Ground.Calcestruzzo > 0) idx = 3 + PrimoSolaio + model.Ground.Calcestruzzo - 1;
                cmbGround.SelectedIndex = idx;
            }
            terrenoPrecedente = cmbGround.SelectedIndex;
            // caselle er e sigma: nascoste con spazio libero e perfetto, modificabili solo con Personalizzato
            if (txtEps != null)
            {
                bool real = model.Ground.Kind == GroundKind.Real;
                bool custom = cmbGround.SelectedIndex == 3 + Grounds.Length;
                lblEps.IsVisible = txtEps.IsVisible = lblSig.IsVisible = txtSig.IsVisible = real;
                txtEps.IsReadOnly = txtSig.IsReadOnly = !custom;
                txtEps.Opacity = txtSig.Opacity = custom ? 1.0 : 0.7;
                if (real)
                {
                    txtEps.Text = model.Ground.Epsr.ToString("0.###", CultureInfo.CurrentCulture);
                    txtSig.Text = model.Ground.Sigma.ToString("0.#####", CultureInfo.CurrentCulture);
                }
            }
            int mi = Materials.Length;
            for (int i = 0; i < Materials.Length; i++)
                if (Math.Abs(Materials[i].Item2 - model.WireConductivity) <= 1e-6 * Math.Max(1, model.WireConductivity)) mi = i;
            cmbMat.SelectedIndex = mi;
            building = old;
        }

        void ApplyGroundChoice()
        {
            if (model == null) return;
            int i = cmbGround.SelectedIndex;
            Registro.Scrivi("AZIONE", "terreno: " + cmbGround.SelectedItem);
            if (i == 0) model.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
            else if (i == 1) model.Ground = new GroundSpec { Kind = GroundKind.Perfect };
            else if (i == 3 + Grounds.Length)
            {
                double er = ParseNum(txtEps.Text, 13), sg = ParseNum(txtSig.Text, 0.005);
                if (er < 1 || er > 100 || sg < 0 || sg > 10)
                {
                    Status("Terreno personalizzato fuori dai valori fisici (er 1-100, sigma 0-10 S/m): controllare le caselle.", true);
                    er = Math.Max(1, Math.Min(100, er)); sg = Math.Max(0, Math.Min(10, sg));
                }
                cfg.SetD("terreno_er", er);
                cfg.SetD("terreno_sigma", sg);
                model.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = er, Sigma = sg, Name = "Personalizzato" };
            }
            else if (i == 2)
            {
                if (model.Ground.Kind != GroundKind.Real) model.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = 13, Sigma = 0.005 };
            }
            else if (i - 3 >= PrimoSolaio)
            {
                // 0.5.0: solaio in calcestruzzo, valori alla frequenza del modello (e ricalcolati dal motore a ogni frequenza)
                int k = i - 3 - PrimoSolaio + 1;
                double er, sg;
                TerreniEdifici.Valori(k, model.FreqMHz, out er, out sg);
                model.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = er, Sigma = sg, Name = TerreniEdifici.Nomi[k], Calcestruzzo = k };
                if (terrenoPrecedente != i && cfg.Get("avviso_solaio", "1") != "0")
                {
                    int prec = terrenoPrecedente;
                    Dispatcher.UIThread.Post(async () => await AvvisoSolaio(prec));
                }
            }
            else
            {
                Tuple<string, double, double> g = Grounds[i - 3];
                model.Ground = new GroundSpec { Kind = GroundKind.Real, Epsr = g.Item2, Sigma = g.Item3, Name = g.Item1 };
            }
            if (model.Ground.Kind == GroundKind.Real) model.Ground.Sommerfeld = cfg.GetD("terreno_sommerfeld", 0) > 0.5;
            if (txtNec != null) txtNec.Text = model.ToNec();
            terrenoPrecedente = i;
            Calculate(true);
        }

        void ApplyMaterialChoice()
        {
            if (model == null) return;
            int i = cmbMat.SelectedIndex;
            Registro.Scrivi("AZIONE", "materiale dei fili: " + cmbMat.SelectedItem);
            if (i < 0 || i >= Materials.Length) return;
            model.WireConductivity = Materials[i].Item2;
            model.WireMaterial = Materials[i].Item1;
            if (txtNec != null) txtNec.Text = model.ToNec();
            Calculate(true);
        }

        void ApplyNecText()
        {
            if (txtNec == null) return;
            try
            {
                NecModel m = NecModel.Parse(txtNec.Text ?? "");
                if (model != null) { m.FileName = model.FileName; }
                SetModel(m, null);
                Calculate(true);
            }
            catch (Exception ex)
            {
                Status("Errore nelle schede NEC: " + ex.Message, true);
                lastError = ex.Message;
                UpdateNotes();
            }
        }

        // ------------------------------------------------------------------------------------------------
        // calcolo a una frequenza (e sweep automatico dopo un nuovo modello)
        // ------------------------------------------------------------------------------------------------
        // finestra "Verifica della segmentazione" (bozzetto approvato): con Applica il modello corretto sostituisce quello aperto
        NecModel segVerificato;
        async Task<bool> ApriVerificaSegmentazione(bool soloSeAvvisi = false)
        {
            if (model == null) { Status("Apri prima un modello.", true); return false; }
            double f0 = ParseNum(txtFreq.Text, model.FreqMHz), span = ParseNum(txtSpan.Text, 0) / 1000.0;
            double fmax = f0 + Math.Max(0, span) / 2;
            if (soloSeAvvisi)
            {
                List<SegIssue> l = SegCheck.Check(model, fmax, new SegOptions());
                if (l.FindAll(i => i.Card >= 0).Count == 0) return false;
            }
            string nome = string.IsNullOrEmpty(model.FileName) ? "modello" : Path.GetFileNameWithoutExtension(model.FileName);
            VerificaSegmentazione.AutoApri = cfg.GetD("seg_auto", 0) > 0.5;
            VerificaSegmentazione w = new VerificaSegmentazione(model, fmax, nome, v => cfg.SetD("seg_auto", v ? 1 : 0));
            await w.ShowDialog(this);
            segVerificato = model;
            if (w.Risultato == null) return false;
            NecModel m = w.Risultato;
            string fn = model.FileName;
            SetModel(m, null);
            model.FileName = fn;
            segVerificato = model;
            Registro.Scrivi("AZIONE", "segmentazione corretta: " + SegCheck.Segmenti(model) + " segmenti");
            Status("Segmentazione corretta: " + SegCheck.Segmenti(model) + " segmenti. Il file su disco non e' cambiato: salvalo se vuoi tenerla.");
            Calculate(true);
            return true;
        }

        async void Calculate(bool thenSweep = false)
        {
            if (model == null) { Status("Apri prima un modello.", true); return; }
            double f = ParseNum(txtFreq.Text, model.FreqMHz);
            if (f <= 0) { Status("Frequenza non valida.", true); return; }
            // "apri questa finestra da sola prima di ogni calcolo se ci sono avvisi" (una volta per modello)
            if (cfg.GetD("seg_auto", 0) > 0.5 && segVerificato != model)
            {
                segVerificato = model;
                if (await ApriVerificaSegmentazione(true)) return;
            }
            CheckFreeFrequency(f);
            try
            {
                geoNotes = new List<string>();
                geo = model.BuildGeometry(geoNotes);
                AggiornaQuota(geo, null);   // 0.5.3
            }
            catch (Exception ex)
            {
                geo = null;
                lastError = ex.Message;
                AggiornaQuota(null, ex.Message);   // 0.5.3: con l'antenna sul suolo compare il riquadro con la quota proposta
                Status("Errore nella geometria: " + ex.Message + " (pulsante Controlla per i dettagli)", true);
                UpdateNotes();
                RefreshAll();
                return;
            }
            if (cts != null) cts.Cancel();
            // 0.4.3 (registro di IC8TEM, 13/11 15:50): lo sweep del modello o della frequenza precedente si ferma, altrimenti
            // gira insieme al nuovo calcolo (stack da 1570 segmenti: CPU al 100 %, 1,4 GB, calcolo da 2 a 7 s)
            if (sweepCts != null) sweepCts.Cancel();
            cts = new CancellationTokenSource();
            CancellationToken tok = cts.Token;
            NecModel m = model;
            SegmentGeometry g = geo;
            Status("Calcolo a " + Fmt(f, 3) + " MHz, " + g.N + " segmenti...");
            prog.IsVisible = true;
            prog.IsIndeterminate = true;
            DateTime t0 = DateTime.Now;
            try
            {
                // 0.4.20: modelli grandi, primo diagramma 3D a 5 gradi (quello a 2 gradi segue in sottofondo)
                bool grande = g.N > SegmentiModelloGrande && !ProveAutomatiche && ModoCalcoloAuto != 0;
                RunResult r = await Task.Run(() => { RunResult rr = Analysis.Run(m, g, f, grande ? 5.0 : 2.0, tok); ControlliDopoCalcolo(m, rr); return rr; });   // 0.5.1: controlli dei dati
                if (tok.IsCancellationRequested || m != model) return;
                run = r;
                lastError = null;
                Registro.Scrivi("CALCOLO", string.Format(CultureInfo.InvariantCulture,
                    "{0:0.000} MHz, {1} segmenti: Z = {2:0.000} {3:+0.000;-0.000}j, ROS {4:0.00} su {5} ohm, Gmax {6:0.00} dBi (az {7:0.0}, el {8:0.0}), F/B {9:0.00} dB, efficienza {10:0.00} %, AGT {11:0.00} dB",
                    f, g.N, r.Zin.Real, r.Zin.Imaginary, r.Swr, r.Z0, r.GmaxDbi, r.PhiMax, r.ElevationOfMax, Nz(r.FrontBack), r.Efficiency, Nz(r.AgtDb)));
                Status(string.Format(CultureInfo.CurrentCulture, "Calcolato a {0:0.000} MHz in {1:0.0} s ({2} segmenti).", f, (DateTime.Now - t0).TotalSeconds, g.N));
                using (Prestazioni.Misura("Aggiornamento delle linguette dopo il calcolo")) RefreshAll();
                Registro.Scrivi("RISORSE", Prestazioni.Risorse());
                nCalcoli++;
                if (grande) AffinaDiagrammaSeServe(r, tok);
                double sec = (DateTime.Now - t0).TotalSeconds;
                if (thenSweep || sweep == null)
                {
                    // 0.4.20 (approvato da IC8TEM il 14/11/2026): con "solo modelli leggeri" lo sweep dei modelli grandi parte a richiesta
                    if (ModoCalcoloAuto == 1 && !ProveAutomatiche && (g.N > SegmentiModelloGrande || sec > 2.0))
                    {
                        Status(string.Format(CultureInfo.CurrentCulture, "Calcolato a {0:0.000} MHz in {1:0.0} s ({2} segmenti). Modello grande: lo sweep non parte da solo, premi Sweep (Impostazioni > Calcolo automatico all'apertura).", f, sec, g.N));
                        Registro.Scrivi("CALCOLO", "sweep automatico non avviato: " + g.N + " segmenti, " + sec.ToString("0.0", CultureInfo.InvariantCulture) + " s");
                    }
                    else DoSweep();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                lastError = ex.Message;
                // 1.5.3: senza sorgente, dopo l'avviso della tabella dei fili, solo la riga di stato (niente seconda finestra)
                bool giaDetto = senzaSorgenteGiaDetto && m.Sources.Count == 0;
                Status("Errore nel calcolo: " + ex.Message, !giaDetto);
                // 1.0.1: anche se il calcolo non riesce (per esempio un modello nuovo ancora senza sorgente) la Struttura 3D
                // e gli Oggetti del modello si ridisegnano, altrimenti la tabella delle Sorgenti non compare e non si puo' aggiungere
                // la sorgente che manca
                if (m == model)
                {
                    run = null;
                    try { RefreshAll(); } catch (Exception ex2) { Registro.Scrivi("ERRORE", "ridisegno dopo l'errore di calcolo: " + ex2.Message); }
                }
                UpdateNotes();
            }
            finally
            {
                senzaSorgenteGiaDetto = false;
                if (!tok.IsCancellationRequested) { prog.IsVisible = false; prog.IsIndeterminate = false; }
            }
        }
        bool senzaSorgenteGiaDetto;   // 1.5.3: la tabella dei fili ha gia' detto «nessuna sorgente»

        CancellationTokenSource sweepCts;

        // 0.4.20: oltre questi segmenti il modello e' "grande" (Butternut corretta 1431, stack 1570; registri della 0.4.19)
        internal const int SegmentiModelloGrande = 1500;

        /// <summary>0 = sempre, 1 = solo modelli leggeri (normale), 2 = mai. Nelle prove automatiche sempre 0.</summary>
        int ModoCalcoloAuto { get { return ProveAutomatiche ? 0 : Math.Max(0, Math.Min(2, (int)Math.Round(cfg.GetD("calc_auto", 1)))); } }

        // all'apertura di un modello (file o calcolatore): calcolo secondo l'impostazione "Calcolo automatico all'apertura"
        void CalcolaAllApertura()
        {
            if (ModoCalcoloAuto != 2) { Calculate(true); return; }
            try { geoNotes = new List<string>(); geo = model.BuildGeometry(geoNotes); } catch (Exception ex) { geo = null; lastError = ex.Message; }
            run = null;
            RefreshAll();
            Status("Modello aperto senza calcolo (Impostazioni > Calcolo automatico all'apertura: mai). Premi Calcola.");
        }

        bool affinando;
        // diagramma 3D a 2 gradi in sottofondo, solo se si vede (Struttura 3D, Diagrammi, Rumore/GT/Stack)
        // 1.5.3: al passo degli angoli scelto (o «auto», adattato al computer); sempre = anche fuori dalle linguette del diagramma
        async void AffinaDiagrammaSeServe(RunResult r, CancellationToken tok, bool sempre = false)
        {
            double passo = PassoDiagramma();
            if (affinando || r == null || r.Grid == null || Math.Abs(r.Grid.ThetaStep - passo) < 1e-9) return;
            if (r.Grid.ThetaStep < passo && !sempre) return;
            string n = curTab >= 0 && curTab < tabs.Count ? tabs[curTab].Tag as string : "";
            if (!sempre && n != "Struttura 3D" && n != "Diagrammi" && n != "Rumore/GT/Stack") return;
            affinando = true;
            try
            {
                RegistraPassoAngoli();
                await Task.Run(() => Analysis.AffinaDiagramma(r, passo, tok));
                if (!tok.IsCancellationRequested && run == r) { Registro.Scrivi("CALCOLO", "diagramma 3D a " + passo.ToString("0.#", CultureInfo.InvariantCulture) + " gradi"); RefreshAll(); AggiornaPassoAngoli(); }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Registro.Scrivi("CALCOLO", "diagramma 3D a 2 gradi non riuscito: " + ex.Message); }
            finally { affinando = false; }
        }

        internal int PuntiSweepForzati;   // 0.4.19: punti dello sweep imposti dalle prove automatiche (0 = quelli della casella)

        async void DoSweep()
        {
            if (model == null || geo == null) { Status("Apri e calcola prima un modello.", true); return; }
            double fc = ParseNum(txtFreq.Text, model.FreqMHz);
            double spanKHz = ParseNum(txtSpan.Text, fc * 20.0);
            if (spanKHz <= 0) { Status("Lo span dello sweep deve essere maggiore di zero (kHz).", true); return; }
            double f1 = fc - spanKHz / 2000.0, f2 = fc + spanKHz / 2000.0;
            int np = (int)ParseNum(txtNpts.Text, 41);
            // 0.4.19 (collaudo 0.4.17): nelle prove dei modelli il numero di punti si impone qui; scritto nella casella prima di aprire
            // il modello veniva sovrascritto dall'apertura (41 punti o quelli della scheda FR): la Butternut faceva 41 punti, 33 minuti
            if (PuntiSweepForzati > 0) np = PuntiSweepForzati;
            if (np < 2) np = 2;
            if (np > 2001) np = 2001;
            if (f1 <= 0) { Status("Lo span e' troppo ampio per questa frequenza.", true); return; }
            List<double> fs = new List<double>();
            for (int i = 0; i < np; i++) fs.Add(f1 + (f2 - f1) * i / (np - 1));
            double th = run != null ? run.ThetaMax : 90, ph = run != null ? run.PhiMax : 0;
            NecModel m = model;
            SegmentGeometry g = geo;
            if (sweepCts != null) sweepCts.Cancel();
            CancellationTokenSource my = new CancellationTokenSource();
            sweepCts = my;
            CancellationToken tok = my.Token;
            prog.IsVisible = true;
            prog.IsIndeterminate = false;
            prog.Maximum = np;
            prog.Value = 0;
            Status("Sweep da " + Fmt(f1, 4) + " a " + Fmt(f2, 4) + " MHz (span " + Fmt(spanKHz, 1) + " kHz), " + np + " punti...");
            Progress<int> pr = new Progress<int>(v => { prog.Value = v; });
            DateTime t0 = DateTime.Now;
            try
            {
                List<SweepPoint> res = await Task.Run(() => Analysis.Sweep(m, g, fs, th, ph, pr, tok));
                if (m != model || tok.IsCancellationRequested) return;
                sweep = res;
                int bad = res.Count(p => !p.Ok);
                Status(Lang.F("Sweep completato in {0} s: {1} punti{2}.", ((DateTime.Now - t0).TotalSeconds).ToString("0.0", CultureInfo.CurrentCulture), res.Count,
                    bad > 0 ? Lang.F(", {0} con errori", bad) : ""), bad > 0);
                ShowSweep();
                nSweep++;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Status("Errore nello sweep: " + ex.Message, true); nSweep++; }
            finally { if (!tok.IsCancellationRequested) prog.IsVisible = false; }
        }

        void RefreshAfterZ0()
        {
            if (run != null)
            {
                run.Z0 = model.Z0;
                run.Swr = Analysis.Swr(run.Zin, model.Z0);
            }
            if (sweep != null)
                foreach (SweepPoint p in sweep) if (p.Ok) p.Swr = Analysis.Swr(p.Z, model.Z0);
            if (txtNec != null) txtNec.Text = model.ToNec();
            RefreshAll();
        }

        // ------------------------------------------------------------------------------------------------
        // aggiornamento delle viste
        // ------------------------------------------------------------------------------------------------
        void RefreshAll()
        {
            if (lblFile != null && model != null) lblFile.Text = Path.GetFileName(model.FileName);
            ShowCards();
            ShowStructure();
            ShowPatterns();
            ShowSweep();
            ShowCurrents();
            UpdateNotes();
            SyncToolbar();
            AggiornaLinguetta();
        }

        void ShowCards()
        {
            if (run == null)
            {
                foreach (string k in new[] { "z", "swr", "g", "dir", "fb", "fr", "bw", "eff", "agt" }) SetCard(k, "-", "");
                SetCard("seg", geo != null ? geo.N.ToString() : "-", geo != null ? "fili: " + model.Geometry.Count : "");
                return;
            }
            Complex z = run.Zin;
            SetCard("z", Fmt(z.Real, 2) + (z.Imaginary >= 0 ? " +j" : " -j") + Fmt(Math.Abs(z.Imaginary), 2), Lang.F("a {0} MHz", Fmt(run.FreqMHz, 3)) + (run.Sources.Count > 1 ? Lang.F(" (sorgente 1 di {0})", run.Sources.Count) : ""));
            Color swrCol = run.Swr <= 1.5 ? Pal.Green : (run.Swr <= 2.0 ? Pal.Fore : (run.Swr <= 3 ? Pal.Orange : Pal.Red));
            SetCard("swr", run.Swr >= 99 ? ">99" : Fmt(run.Swr, 2), Lang.F("su Z0 = {0} ohm", Fmt(run.Z0, 1)), swrCol);
            { double gm = GuadagnoMostrato(run.GmaxDbi); SetCard("g", Fmt(gm, 2) + " dBi", Fmt(gm - 2.15, 2) + " dBd" + (gm != run.GmaxDbi ? " (corretto con l'AGT)" : "")); }
            SetCard("dir", "az " + Fmt(run.PhiMax, 1) + "°", "elevazione " + Fmt(run.ElevationOfMax, 1) + "°");
            SetCard("fb", Fmt(Nz(run.FrontBack), 2) + " dB", "avanti / dietro (180°)");
            SetCard("fr", Fmt(Nz(run.FrontRear), 2) + " dB", "avanti / peggiore posteriore");
            SetCard("bw", Fmt(run.HpbwAz, 1) + "° / " + Fmt(run.HpbwEl, 1) + "°", "orizzontale / verticale");
            SetCard("eff", Fmt(run.Efficiency, 2) + " %", run.StructureLoss > 0 ? "perdite nei fili e nei carichi" : "senza perdite");
            double agt = Nz(run.AgtDb);
            if (model.Ground.Kind == GroundKind.Real)
                SetCard("agt", "n.a.", "valido solo in spazio libero o su terreno perfetto");
            else
            {
                Color agtCol = Math.Abs(agt) <= 0.2 ? Pal.Green : (Math.Abs(agt) <= 0.5 ? Pal.Orange : Pal.Red);
                SetCard("agt", (agt >= 0 ? "+" : "") + Fmt(agt, 2) + " dB", Math.Abs(agt) <= 0.2 ? "modello affidabile" : "controllare la segmentazione", agtCol);
            }
            SetCard("seg", geo != null ? geo.N.ToString() : "-", "fili: " + model.Geometry.Count);
        }

        // aspetto del terreno nella Struttura 3D: tipo scelto nel menu (o riconosciuto dai valori del modello)
        GroundStyle GroundLookFor(GroundSpec g)
        {
            if (g.Kind == GroundKind.Perfect) return GroundPainter.Styles[0];
            if (g.Kind != GroundKind.Real) return null;
            for (int i = 0; i < Grounds.Length; i++)
                if (Math.Abs(Grounds[i].Item2 - g.Epsr) < 1e-9 && Math.Abs(Grounds[i].Item3 - g.Sigma) < 1e-12) return GroundPainter.ByIndex(i + 1);
            return GroundPainter.Neutral;
        }

        /// <summary>1.5.2: linee, carichi, reti, trasformatori, reti L e stub del modello per la Struttura 3D; fili virtuali (1 segmento con LD 4 da 1 Mohm).</summary>
        internal static List<Structure3DView.Oggetto> OggettiPer3D(NecModel m, SegmentGeometry g, out HashSet<int> virt)
        {
            List<Structure3DView.Oggetto> l = new List<Structure3DView.Oggetto>();
            virt = new HashSet<int>();
            if (m == null || g == null) return l;
            Func<int, int, int> seg = (tag, s) => { try { return g.SegNo(tag, s) - 1; } catch { return -1; } };
            Func<int, int> centroFilo = tag =>
            {
                int a = -1, b = -1;
                for (int i = 0; i < g.N; i++) if (g.Tag[i] == tag) { if (a < 0) a = i; b = i; }
                return a < 0 ? -1 : (a + b) / 2;
            };
            CultureInfo ci = CultureInfo.CurrentCulture;
            Func<double, string> n3 = v => (Math.Abs(v) >= 100 ? v.ToString("0", ci) : Math.Abs(v) >= 10 ? v.ToString("0.#", ci) : v.ToString("0.##", ci));
            int ntl = 0;
            for (int i = 0; i < m.Networks.Count; i++)
            {
                NetworkSpec nw = m.Networks[i];
                Structure3DView.Oggetto o = new Structure3DView.Oggetto { Seg1 = seg(nw.Tag1, nw.Seg1), Seg2 = seg(nw.Tag2, nw.Seg2), Riga = i };
                if (nw.IsTl) { o.Tipo = 0; o.Etichetta = "TL" + (++ntl) + " " + n3(Math.Abs(nw.F1)) + " Ω"; o.Incrociata = nw.F1 < 0; }
                else { o.Tipo = 2; o.Etichetta = "NT"; }
                l.Add(o);
            }
            for (int i = 0; i < m.Loads.Count; i++)
            {
                LoadSpec ld = m.Loads[i];
                if (ld.Type == 5 || ld.Type == 7 || ld.Tag <= 0) continue;
                int sg = ld.From > 0 ? seg(ld.Tag, (ld.From + Math.Max(ld.From, ld.To)) / 2) : centroFilo(ld.Tag);
                if (sg < 0) continue;
                // filo virtuale di EZNEC: un segmento solo con 1 Mohm di reattanza
                int nseg = 0; for (int k = 0; k < g.N; k++) if (g.Tag[k] == ld.Tag) nseg++;
                if (ld.Type == 4 && nseg == 1 && Math.Abs(ld.F2) >= 1e5) { virt.Add(sg); continue; }
                Structure3DView.Oggetto o = new Structure3DView.Oggetto { Tipo = 1, Seg1 = sg, Riga = i };
                if (ld.Type == 4) { o.Simbolo = 5; o.Etichetta = n3(ld.F1) + (ld.F2 < 0 ? " -j" : " +j") + n3(Math.Abs(ld.F2)) + " Ω"; }
                else
                {
                    List<string> p = new List<string>();
                    if (ld.F1 != 0) p.Add("R " + n3(ld.F1) + " Ω");
                    if (ld.F2 != 0) p.Add("L " + n3(ld.F2 * 1e6) + " µH");
                    if (ld.F3 != 0) p.Add("C " + n3(ld.F3 * 1e12) + " pF");
                    int cnt = (ld.F1 != 0 ? 1 : 0) + (ld.F2 != 0 ? 1 : 0) + (ld.F3 != 0 ? 1 : 0);
                    bool par = ld.Type == 1 || ld.Type == 3;
                    o.Simbolo = cnt == 1 ? (ld.F1 != 0 ? 0 : ld.F2 != 0 ? 1 : 2) : (par ? 4 : 3);
                    o.Etichetta = (par && cnt > 1 ? "∥ " : "") + string.Join(", ", p);
                }
                l.Add(o);
            }
            for (int i = 0; i < m.Trasformatori.Count; i++)
            {
                Trasformatore t = m.Trasformatori[i];
                l.Add(new Structure3DView.Oggetto { Tipo = 2, Seg1 = seg(t.Tag1, t.Seg1), Seg2 = seg(t.Tag2, t.Seg2), Riga = i, Etichetta = "1:" + n3(t.Z2 / Math.Max(1e-9, t.Z1)) + (t.Inv ? " inv." : "") });
            }
            for (int i = 0; i < m.RetiL.Count; i++)
            {
                ReteL r = m.RetiL[i];
                l.Add(new Structure3DView.Oggetto { Tipo = 2, Seg1 = seg(r.Tag1, r.Seg1), Seg2 = seg(r.Tag2, r.Seg2), Riga = i, Etichetta = Lang.T("rete L") });
            }
            for (int i = 0; i < m.Stubs.Count; i++)
            {
                Stub st = m.Stubs[i];
                l.Add(new Structure3DView.Oggetto { Tipo = 3, Seg1 = seg(st.Tag, st.Seg), Riga = i, Etichetta = "stub " + n3(st.Z0) + " Ω" + (st.Corto ? "" : " " + Lang.T("aperto")) });
            }
            return l;
        }

        void ShowStructure()
        {
            if (view3d == null) return;
            view3d.Geo = geo;
            if (varView != null) VarMostraDove(varScelta);   // 1.5.1: linguetta Variabili (1.5.2: anche senza variabile scelta)
            view3d.Ground = model != null && model.Ground.Kind != GroundKind.FreeSpace;
            view3d.SourceSegs = new HashSet<int>();
            view3d.CurrentNorm = null;
            view3d.Pattern = null;
            view3d.LobeAz = double.NaN;
            if (geo != null && model != null)
            {
                foreach (SourceSpec s in model.Sources)
                {
                    try { view3d.SourceSegs.Add(geo.SegNo(s.Tag, s.Seg) - 1); } catch { }
                }
                GroundStyle gs = GroundLookFor(model.Ground);
                view3d.GroundLook = gs;
                view3d.GroundValues = model.Ground.Kind == GroundKind.Real ? "εr " + Fmt(model.Ground.Epsr, 1) + "   σ " + Fmt(model.Ground.Sigma, 4) + " S/m" : "";
                if (groundLegend != null)
                {
                    groundLegend.SelectedKey = model.Ground.Kind == GroundKind.FreeSpace ? "" : gs.Key;
                    groundLegend.InvalidateVisual();
                }
                // 1.5.2: riga tradotta (prima restava in italiano nelle altre lingue; visto preparando la lezione 28 inglese)
                string gtxt = model.Ground.Kind == GroundKind.FreeSpace ? Lang.T("spazio libero") : (model.Ground.Kind == GroundKind.Perfect ? Lang.T("terreno perfetto") :
                    Lang.F("terreno {0} (εr {1}, σ {2} S/m)", Lang.T(gs == GroundPainter.Neutral ? "personalizzato" : gs.Name), Fmt(model.Ground.Epsr, 0), Fmt(model.Ground.Sigma, 4)));
                view3d.Info = Lang.F("{0} segmenti, {1} schede di geometria, {2}", geo.N, model.Geometry.Count, gtxt);
                // etichette delle sorgenti (A 1∠0°, ...) quando ce n'e' piu' di una
                view3d.SourceLabels = new List<KeyValuePair<int, string>>();
                if (model.Sources.Count > 1)
                    for (int k = 0; k < model.Sources.Count; k++)
                    {
                        SourceSpec ss = model.Sources[k];
                        try
                        {
                            string nome = k < 26 ? ((char)('A' + k)).ToString() : (k + 1).ToString(CultureInfo.InvariantCulture);
                            view3d.SourceLabels.Add(new KeyValuePair<int, string>(geo.SegNo(ss.Tag, ss.Seg) - 1,
                                nome + " " + Fmt(ss.V.Magnitude, 2).TrimEnd('0').TrimEnd(',', '.') + "∠" + Fmt(ss.V.Phase * 180 / Math.PI, 0) + "°"));
                        }
                        catch { }
                    }
                AggiornaRiconoscimentoStack();
                view3d.Oggetti = OggettiPer3D(model, geo, out HashSet<int> virt);
                view3d.SegVirtuali = virt;
            }
            // 1.0.8 (IC8TEM 16/11/2026 20:48 "si correggi"): con la correzione di Leeson (tubi telescopici) il calcolo usa una sua
            // geometria (run.Geometry) diversa da geo: stessi segmenti, raggi equivalenti; prima le correnti non comparivano
            if (run != null && run.Geometry != null && geo != null && (run.Geometry == geo || run.Geometry.N == geo.N))
            {
                double[] c = new double[geo.N];
                double mx = 0;
                for (int i = 0; i < geo.N; i++) { c[i] = Complex.Abs(run.Engine.SegmentCurrent(i)); mx = Math.Max(mx, c[i]); }
                if (mx > 0) for (int i = 0; i < geo.N; i++) c[i] /= mx;
                view3d.CurrentNorm = c;
                view3d.Pattern = run.Grid;
                view3d.PatternMax = run.GmaxDbi;
                view3d.LobeAz = run.PhiMax;
                view3d.LobeEl = run.ElevationOfMax;
            }
            view3d.InvalidateVisual();
        }

        void ShowPatterns()
        {
            if (polAz == null) return;
            polAz.Traces.Clear();
            polEl.Traces.Clear();
            polAz.Notes = new string[0];
            polEl.Notes = new string[0];
            polAz.MarkAngle = double.NaN;
            polEl.MarkAngle = double.NaN;
            bool arrl = cmbScale == null || cmbScale.SelectedIndex != 1;
            polAz.ArrlScale = polEl.ArrlScale = arrl;
            if (run != null)
            {
                bool vh = cmbPol != null && cmbPol.SelectedIndex == 1;
                polAz.RefDb = polEl.RefDb = run.GmaxDbi;
                polAz.Title = Lang.F("Azimut all'elevazione di {0}°", Fmt(run.ElevationOfMax, 1));   // 1.5.2: tradotto (lezione 13 EN)
                polEl.Title = Lang.F("Elevazione nel piano di azimut {0}°", Fmt(run.PhiMax, 1));
                polEl.GroundHalf = model.Ground.Kind != GroundKind.FreeSpace;
                polAz.Traces.Add(new PolarTrace { Db = run.AzCut, Color = Pal.Series(0), Width = 2.4, Name = "totale" });
                polEl.Traces.Add(new PolarTrace { Db = run.ElCut, Color = Pal.Series(0), Width = 2.4, Name = "totale" });
                if (vh)
                {
                    polAz.Traces.Add(new PolarTrace { Db = run.AzCutVert, Color = Pal.Series(1), Width = 1.4, Dashed = true, Name = "verticale" });
                    polAz.Traces.Add(new PolarTrace { Db = run.AzCutHor, Color = Pal.Series(2), Width = 1.4, Dashed = true, Name = "orizzontale" });
                    polEl.Traces.Add(new PolarTrace { Db = run.ElCutVert, Color = Pal.Series(1), Width = 1.4, Dashed = true, Name = "verticale" });
                    polEl.Traces.Add(new PolarTrace { Db = run.ElCutHor, Color = Pal.Series(2), Width = 1.4, Dashed = true, Name = "orizzontale" });
                }
                if (terrSovrapponi && terrEsito != null) polEl.Traces.Add(TerrTraccia());   // 1.2.0: lobo sul terreno reale
                polAz.MarkAngle = run.PhiMax;
                polEl.MarkAngle = run.ElevationOfMax;
                polAz.Notes = new[]
                {
                    Lang.F("Massimo {0} dBi a {1}°;  F/B {2} dB;  F/R {3} dB", Fmt(run.GmaxDbi, 2), Fmt(run.PhiMax, 1), Fmt(run.FrontBack, 2), Fmt(run.FrontRear, 2)),
                    Lang.F("Apertura a -3 dB: {0}°;  {1} MHz", Fmt(run.HpbwAz, 1), Fmt(run.FreqMHz, 3))
                };
                polEl.Notes = new[]
                {
                    Lang.F("Massimo a {0}° di elevazione;  apertura a -3 dB: {1}°", Fmt(run.ElevationOfMax, 1), Fmt(run.HpbwEl, 1)),
                    polEl.GroundHalf ? "Con il terreno solo la semisfera superiore" : "Spazio libero: 0° avanti, 180° dietro, sotto l'orizzonte in basso"
                };
            }
            polAz.InvalidateVisual();
            polEl.InvalidateVisual();
            AggiornaDiagrammi12();
        }

        void ShowSweep()
        {
            if (chSwr == null) return;
            List<SweepPoint> ok = sweep != null ? sweep.Where(p => p.Ok).ToList() : new List<SweepPoint>();
            double[] x = ok.Select(p => p.F).ToArray();
            double mark = run != null ? run.FreqMHz : double.NaN;
            chSwr.X = x; chZ.X = x; chGain.X = x; chFb.X = x;
            chSwr.MarkX = chZ.MarkX = chGain.MarkX = chFb.MarkX = mark;
            chSwr.Series.Clear(); chZ.Series.Clear(); chGain.Series.Clear(); chFb.Series.Clear();
            if (chEff != null) { chEff.X = x; chEff.MarkX = mark; chEff.Series.Clear(); }
            if (ok.Count > 1)
            {
                double z0 = model != null ? model.Z0 : 50;
                chSwr.Title = Lang.F("ROS su Z0 = {0} ohm", Fmt(z0, 1));
                chSwr.Series.Add(new LineSeries { Name = "ROS", Y = ok.Select(p => Math.Min(p.Swr, 10)).ToArray(), Color = Pal.Series(0), Width = 2.4 });
                chSwr.YMinFixed = 1;
                double mx = ok.Max(p => Math.Min(p.Swr, 10));
                chSwr.YMaxFixed = mx < 2.5 ? 3 : (mx < 5 ? 5 : 10);
                chSwr.HLines = new[] { 1.5, 2.0 };
                chZ.Series.Add(new LineSeries { Name = "R", Y = ok.Select(p => p.Z.Real).ToArray(), Color = Pal.Series(0), Width = 2.2 });
                chZ.Series.Add(new LineSeries { Name = "X", Y = ok.Select(p => p.Z.Imaginary).ToArray(), Color = Pal.Series(1), Width = 2.2 });
                chZ.Series.Add(new LineSeries { Name = "|Z|", Y = ok.Select(p => Complex.Abs(p.Z)).ToArray(), Color = Pal.Series(2), Width = 1.4, Dashed = true });
                chZ.HLines = new double[] { 0 };
                chGain.Title = run != null ? Lang.F("Guadagno nella direzione del massimo (az {0}°, el {1}°)", Fmt(run.PhiMax, 0), Fmt(run.ElevationOfMax, 0)) : "Guadagno nella direzione del massimo";
                chFb.Title = run != null ? Lang.F("Rapporti F/B e F/R (az {0}°, el {1}°)", Fmt(run.PhiMax, 0), Fmt(run.ElevationOfMax, 0)) : "Rapporti F/B e F/R";
                chGain.Series.Add(new LineSeries { Name = "Guadagno dBi", Y = ok.Select(p => p.GainFwd).ToArray(), Color = Pal.Series(0), Width = 2.4 });
                chFb.Series.Add(new LineSeries { Name = "F/B", Y = ok.Select(p => p.FrontBack).ToArray(), Color = Pal.Series(1), Width = 2.2, Decimals = 1 });
                chFb.Series.Add(new LineSeries { Name = "F/R", Y = ok.Select(p => p.FrontRear).ToArray(), Color = Pal.Series(3), Width = 1.8, Dashed = true, Decimals = 1 });
                if (chEff != null)
                {
                    double[] ef = ok.Select(p => p.Efficiency).ToArray();
                    chEff.Series.Add(new LineSeries { Name = "Efficienza %", Y = ef, Color = Pal.Series(2), Width = 2.2, Decimals = 1 });
                    double mn = ef.Min();
                    chEff.YMaxFixed = 100;
                    chEff.YMinFixed = mn > 95 ? 90 : (mn > 80 ? 75 : (mn > 50 ? 40 : 0));
                }
            }
            chSwr.InvalidateVisual(); chZ.InvalidateVisual(); chGain.InvalidateVisual(); chFb.InvalidateVisual();
            if (chEff != null) chEff.InvalidateVisual();
            if (smith != null)
            {
                smith.Z0 = model != null ? model.Z0 : 50;
                smith.F = x;
                smith.Z = ok.Select(p => p.Z).ToArray();
                smith.MarkF = mark;
                smith.InvalidateVisual();
            }
            if (diagVoce == "Tabelle") MostraTabelle();
        }

        void ShowCurrents()
        {
            if (txtCurrents == null) return;
            // 1.0.8: la tabella usa la geometria del calcolo (con Leeson e' un'altra geometria: prima si leggeva "Nessun calcolo.")
            if (run == null || geo == null || run.Geometry == null) { txtCurrents.Text = "Nessun calcolo."; return; }
            SegmentGeometry gc = run.Geometry;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Correnti al centro dei segmenti a " + Fmt(run.FreqMHz, 3) + " MHz (1 V sulla sorgente, se non diverso nel modello)");
            sb.AppendLine();
            sb.AppendLine("  seg   tag        X (m)       Y (m)       Z (m)    lung. (mm)   |I| (mA)    fase (°)");
            for (int i = 0; i < gc.N; i++)
            {
                Complex c = run.Engine.SegmentCurrent(i);
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "{0,5} {1,5} {2,12:0.0000} {3,11:0.0000} {4,11:0.0000} {5,12:0.00} {6,10:0.0000} {7,10:0.00}",
                    i + 1, gc.Tag[i], gc.CenterX(i), gc.CenterY(i), gc.CenterZ(i), gc.Length(i) * 1000, Complex.Abs(c) * 1000, Math.Atan2(c.Imaginary, c.Real) * 180 / Math.PI));
            }
            sb.AppendLine();
            sb.AppendLine("Sorgenti:");
            foreach (SourceResult s in run.Sources)
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "  tag {0}, segmento {1}: V = {2:0.###} {3:+0.###;-0.###}j V,  I = {4:0.0000} {5:+0.0000;-0.0000}j A,  Z = {6:0.000} {7:+0.000;-0.000}j ohm,  P = {8:0.000000} W",
                    s.Tag, s.Segment, s.V.Real, s.V.Imaginary, s.I.Real, s.I.Imaginary, s.Z.Real, s.Z.Imaginary, s.Power));
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Potenza d'ingresso {0:0.000000} W, perdite nella struttura {1:0.000000} W, efficienza {2:0.00} %",
                run.InputPower, run.StructureLoss, run.Efficiency));
            txtCurrents.Text = sb.ToString();
        }

        void UpdateNotes()
        {
            if (txtNotes == null) return;
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(lastError)) { sb.AppendLine("ERRORE"); sb.AppendLine("  " + lastError); sb.AppendLine(); }
            if (model == null) { txtNotes.Text = sb.Length > 0 ? sb.ToString() : "Nessun modello aperto."; return; }
            sb.AppendLine("MODELLO: " + model.FileName + "  (formato " + model.SourceFormat + ")");
            foreach (string c in model.Comments) sb.AppendLine("  " + c);
            sb.AppendLine();
            if (model.Notes.Count > 0 || geoNotes.Count > 0)
            {
                sb.AppendLine("NOTE");
                foreach (string n in model.Notes) sb.AppendLine("  - " + n);
                foreach (string n in geoNotes) sb.AppendLine("  - " + n);
                sb.AppendLine();
            }
            List<string> w = new List<string>(model.Warnings);
            if (run != null) w.AddRange(run.Warnings);
            sb.AppendLine(w.Count > 0 ? "AVVISI" : "AVVISI: nessuno");
            foreach (string s in w) sb.AppendLine("  - " + s);
            sb.AppendLine();
            if (mmWires != null && mmWires.Count > 0)
            {
                sb.AppendLine("RISEGMENTAZIONE DEI FILI MMANA (segmento massimo lambda/" + Fmt((double)(numSegL.Value ?? 40), 0) + " alla frequenza del file)");
                sb.AppendLine("  filo   lunghezza (m)   raggio (mm)   segmenti NEC   segmento (mm)   segmento/raggio   alimentato");
                foreach (MmanaWireInfo f in mmWires)
                    sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "  {0,4} {1,15:0.0000} {2,13:0.00} {3,14} {4,15:0.0} {5,17:0.0}   {6}",
                        f.Index, f.Length, f.Radius * 1000, f.NecSeg, f.SegLength * 1000, f.SegLength / f.Radius, f.Fed ? "si'" : ""));
            }
            txtNotes.Text = sb.ToString();
        }

        void RunSelfTest()
        {
            if (txtTest == null) return;
            txtTest.Text = "Autotest in corso...";
            Task.Run(() => SelfTest.Run()).ContinueWith(t =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (t.IsFaulted) { txtTest.Text = "Errore: " + t.Exception?.GetBaseException().Message; return; }
                    StringBuilder sb = new StringBuilder();
                    int bad = 0;
                    foreach (SelfTest.Line l in t.Result)
                    {
                        sb.AppendLine((l.Pass ? "OK      " : "ERRORE  ") + l.Name);
                        sb.AppendLine("        " + l.Detail);
                        if (!l.Pass) bad++;
                    }
                    sb.AppendLine();
                    sb.AppendLine(bad == 0 ? "AUTOTEST SUPERATO: il motore C# riproduce nec2c su tutti i modelli di prova." : "AUTOTEST: " + bad + " prove non superate.");
                    txtTest.Text = sb.ToString();
                    try { File.WriteAllText(Path.Combine(AppPaths.DataDir, "autotest.txt"), sb.ToString()); } catch { }
                    Status(bad == 0 ? "Autotest superato." : "Autotest: " + bad + " prove non superate (dettagli nella linguetta Autotest).", bad > 0);
                });
            });
        }
    }
}
