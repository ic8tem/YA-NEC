// YA-NEC - dialogo "Salva modello con nome" in tutti i formati (bozzetto approvato alla lettera,
// doc/bozzetto-salva-formati-approvato.png): cartella e nome, formato a schede (.yanec completo, .nec universale, .maa,
// .weq, .ez sperimentale), tabella "Cosa entra nel file", avviso giallo su cio' che si perde, opzioni, Annulla / Salva.
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using YaNec.Engine;

namespace YaNec
{
    public class SalvaConNome : Window
    {
        readonly NecModel model;
        int formato = 1;                                   // 0 yanec, 1 nec, 2 maa, 3 weq, 4 ez
        static readonly string[] Ext = { ".yanec", ".nec", ".maa", ".weq", ".ez" };
        readonly TextBox tCartella, tNome;
        readonly TextBlock lExt, lTitoloTab;
        readonly StackPanel righe, avviso, schede;
        readonly Border avvisoBox;
        readonly Button bSalva;
        readonly CheckBox cTubi, cFormule, cPunto, cRiapri;
        int segmenti;                                       // 0 quelli di YA-NEC, 1 automatici
        public string Risultato;                            // percorso scritto
        public bool Riapri;

        static Color C(string h) { return Color.Parse(h); }

        public SalvaConNome(NecModel m, string cartella, string nome)
        {
            model = m;
            Title = "Salva modello con nome";
            Width = 1180; Height = 820;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Pal.B(Pal.Back);

            DockPanel root = new DockPanel();
            Border head = new Border { Background = new SolidColorBrush(C("#1E293B")), Padding = new Thickness(18, 12), Child = new TextBlock { Text = "Salva modello con nome", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Brushes.White } };
            DockPanel.SetDock(head, Dock.Top);
            root.Children.Add(head);

            // piede
            DockPanel foot = new DockPanel { Margin = new Thickness(18, 10), LastChildFill = true };
            Button bAnn = new Button { Content = "Annulla", Padding = new Thickness(18, 6), CornerRadius = new CornerRadius(16), Margin = new Thickness(0, 0, 10, 0) };
            bSalva = new Button { Content = "Salva .nec", Padding = new Thickness(18, 6), CornerRadius = new CornerRadius(16), Background = Pal.B(Pal.Accent), Foreground = Brushes.White, FontWeight = FontWeight.SemiBold };
            StackPanel fb = new StackPanel { Orientation = Orientation.Horizontal };
            fb.Children.Add(bAnn); fb.Children.Add(bSalva);
            DockPanel.SetDock(fb, Dock.Right);
            foot.Children.Add(fb);
            string orig = string.IsNullOrEmpty(m.FileName) ? "" : Path.GetExtension(m.FileName);
            foot.Children.Add(new TextBlock { Text = "Il modello aperto resta quello di partenza" + (orig.Length > 0 ? " (" + orig + ")" : "") + ": il salvataggio crea un file nuovo.", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) });
            Border footB = new Border { Child = foot, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 1, 0, 0) };
            DockPanel.SetDock(footB, Dock.Bottom);
            root.Children.Add(footB);

            StackPanel body = new StackPanel { Margin = new Thickness(18, 12), Spacing = 8 };
            tCartella = new TextBox { Text = cartella, FontFamily = Pal.Mono, Width = 760 };
            Button bSfoglia = new Button { Content = "Sfoglia...", CornerRadius = new CornerRadius(14), Padding = new Thickness(14, 4) };
            body.Children.Add(Riga("Cartella", tCartella, bSfoglia));
            tNome = new TextBox { Text = nome, FontFamily = Pal.Mono, Width = 640 };
            lExt = new TextBlock { FontFamily = Pal.Mono, Foreground = Pal.B(Pal.Accent), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0) };
            body.Children.Add(Riga("Nome file", tNome, new Border { Child = lExt, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 4) },
                new TextBlock { Text = "l'estensione segue il formato scelto", Foreground = Pal.B(Pal.Muted), VerticalAlignment = VerticalAlignment.Center }));

            Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("43*,14,57*") };
            // formato
            schede = new StackPanel { Spacing = 8 };
            StackPanel fmt = new StackPanel();
            fmt.Children.Add(new TextBlock { Text = "Formato", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 8) });
            fmt.Children.Add(schede);
            fmt.Children.Add(new TextBlock { Text = "Solo risultati (in \"Esporta risultati\"): tabella CSV, Touchstone .s1p, diagramma per VOACAP, immagini PNG.", TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 8, 0, 0) });
            Border fmtB = Box(fmt);
            g.Children.Add(fmtB);

            StackPanel dx = new StackPanel { Spacing = 10 };
            StackPanel tab = new StackPanel();
            lTitoloTab = new TextBlock { FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 8) };
            tab.Children.Add(lTitoloTab);
            righe = new StackPanel();
            tab.Children.Add(righe);
            avviso = new StackPanel();
            avvisoBox = new Border { Background = new SolidColorBrush(C("#FFF8E6")), BorderBrush = new SolidColorBrush(C("#E8B44A")), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8), Margin = new Thickness(0, 10, 0, 0), Child = avviso };
            tab.Children.Add(avvisoBox);
            dx.Children.Add(Box(tab));
            StackPanel opz = new StackPanel { Spacing = 4 };
            opz.Children.Add(new TextBlock { Text = "Opzioni", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 4) });
            cTubi = new CheckBox { Content = "Unisci i tubi in linea in un filo con rastremazione (solo .maa)", IsChecked = true };
            ToolTip.SetTip(cTubi, "I tubi allineati dello stesso filo NEC diventano un solo filo MMANA; il Taper wire set con raggi diversi arriva con la prossima versione.");
            cFormule = new CheckBox { Content = "Scrivi anche le formule come commento (per non perderle)", IsChecked = false };
            cPunto = new CheckBox { Content = "Separatore decimale con il punto (letto da tutti i programmi)", IsChecked = true, IsEnabled = false };
            cRiapri = new CheckBox { Content = "Dopo il salvataggio riapri il file e confronta i risultati (controllo che non si sia perso niente)", IsChecked = true };
            opz.Children.Add(cTubi); opz.Children.Add(cFormule); opz.Children.Add(cPunto);
            StackPanel segRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            segRow.Children.Add(new TextBlock { Text = "Segmenti:", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore) });
            Button s0 = new Button { Content = "quelli di YA-NEC" }, s1 = new Button { Content = "automatici del programma di destinazione" };
            Action segStile = () =>
            {
                s0.Background = Pal.B(segmenti == 0 ? Pal.Accent : Pal.Pill); s0.Foreground = segmenti == 0 ? Brushes.White : Pal.B(Pal.PillFore);
                s1.Background = Pal.B(segmenti == 1 ? Pal.Accent : Pal.Pill); s1.Foreground = segmenti == 1 ? Brushes.White : Pal.B(Pal.PillFore);
            };
            s0.Click += (s, e) => { segmenti = 0; segStile(); Aggiorna(); };
            s1.Click += (s, e) => { segmenti = 1; segStile(); Aggiorna(); };
            segStile();
            segRow.Children.Add(s0); segRow.Children.Add(s1);
            opz.Children.Add(segRow);
            opz.Children.Add(cRiapri);
            dx.Children.Add(Box(opz));
            Grid.SetColumn(dx, 2);
            g.Children.Add(dx);
            body.Children.Add(g);
            root.Children.Add(new ScrollViewer { Content = body });
            Content = root;

            string[] tit = { "Progetto YA-NEC", "NEC-2", "MMANA-GAL", "AutoEZ / MultiNEC", "EZNEC" };
            string[] eti = { "completo", "universale", "", "", "sperimentale" };
            string[] des =
            {
                "Tutto: modello, variabili e formule, rastremazioni, oggetti inseriti, confronti e istantanee, impostazioni di calcolo.",
                "Letto da 4nec2, xnec2c, EZNEC Pro, nec2c. Variabili come schede SY, linee come TL, rastremazioni divise in fili.",
                "Fili, Taper wire set, sorgenti, carichi, terreno, quota, Z0. MMANA non ha linee di trasmissione.",
                "Variabili (lettere A, B, D...) e formule, fili, sorgenti, carichi R+jX, tabella Transmission Lines; il terreno lo da' il modello EZNEC.",
                "Formato binario non documentato (struttura da Ez2Nec, AG7T): fili, sorgenti, carichi, linee, terreno. Per EZNEC Pro e' piu' sicuro il .nec."
            };
            for (int i = 0; i < 5; i++)
            {
                int k = i;
                bool attivo = true;
                StackPanel t = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                t.Children.Add(new TextBlock { Text = tit[i], FontWeight = FontWeight.Bold, FontSize = 15, Foreground = Pal.B(attivo ? Pal.Fore : Pal.Muted) });
                t.Children.Add(new TextBlock { Text = Ext[i], FontFamily = Pal.Mono, FontSize = 15, Foreground = Pal.B(Pal.Accent) });
                if (eti[i].Length > 0)
                    t.Children.Add(new Border { Background = new SolidColorBrush(C(i == 4 ? "#FFF3D6" : "#E3F5EA")), CornerRadius = new CornerRadius(8), Padding = new Thickness(7, 1),
                        Child = new TextBlock { Text = eti[i], FontSize = 12, Foreground = new SolidColorBrush(C(i == 4 ? "#9A6B00" : "#1E7F45")) } });
                StackPanel sp = new StackPanel();
                sp.Children.Add(t);
                sp.Children.Add(new TextBlock { Text = des[i], TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 2, 0, 0) });
                RadioButton rb = new RadioButton { Content = sp, GroupName = "fmt", IsChecked = i == formato, IsEnabled = attivo };
                Border card = new Border { Child = rb, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8), Tag = i };
                rb.IsCheckedChanged += (s, e) => { if (rb.IsChecked == true) { formato = k; Aggiorna(); } };
                schede.Children.Add(card);
            }

            bSfoglia.Click += async (s, e) =>
            {
                IReadOnlyList<IStorageFolder> d = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Cartella del modello" });
                if (d != null && d.Count > 0) { string p = d[0].TryGetLocalPath(); if (p != null) tCartella.Text = p; }
            };
            bAnn.Click += (s, e) => Close();
            bSalva.Click += (s, e) => Salva();
            Aggiorna();
        }

        static DockPanel Riga(string et, params Control[] c)
        {
            DockPanel d = new DockPanel();
            TextBlock l = new TextBlock { Text = et, Width = 140, VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), FontSize = 15 };
            DockPanel.SetDock(l, Dock.Left);
            d.Children.Add(l);
            StackPanel h = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            foreach (Control x in c) h.Children.Add(x);
            d.Children.Add(h);
            return d;
        }

        static Border Box(Control c)
        {
            return new Border { Child = c, Background = Pal.B(Pal.Panel), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 12) };
        }

        void Riga3(string parte, string come, string esito)
        {
            Grid r = new Grid { ColumnDefinitions = new ColumnDefinitions("230,70,*"), Margin = new Thickness(0, 3) };
            Color c = esito == "si'" ? C("#1E7F45") : esito == "no" ? C("#C0392B") : C("#B26A00");
            TextBlock a = new TextBlock { Text = parte, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            TextBlock b = new TextBlock { Text = esito, Foreground = new SolidColorBrush(c), FontWeight = FontWeight.SemiBold };
            TextBlock d = new TextBlock { Text = come, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(b, 1); Grid.SetColumn(d, 2);
            r.Children.Add(a); r.Children.Add(b); r.Children.Add(d);
            righe.Children.Add(new Border { Child = r, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 0, 1) });
        }

        void Aggiorna()
        {
            string ext = Ext[formato];
            lExt.Text = ext;
            bSalva.Content = "Salva " + ext;
            lTitoloTab.Text = "Cosa entra nel file " + ext;
            foreach (Control c in schede.Children)
            {
                Border b = (Border)c;
                bool on = (int)b.Tag == formato;
                b.BorderBrush = Pal.B(on ? Pal.Accent : Pal.Border);
                b.BorderThickness = new Thickness(on ? 2 : 1);
                b.Background = Pal.B(on ? Pal.AccentSoft : Pal.Panel);
            }
            righe.Children.Clear();
            Grid h = new Grid { ColumnDefinitions = new ColumnDefinitions("300,*") };
            h.Children.Add(new TextBlock { Text = "Parte del modello", Foreground = Pal.B(Pal.Muted) });
            TextBlock h2 = new TextBlock { Text = "Come viene scritta", Foreground = Pal.B(Pal.Muted) };
            Grid.SetColumn(h2, 1); h.Children.Add(h2);
            righe.Children.Add(h);
            int nf = model.Geometry.Count, ns = model.Sources.Count, nl = model.Loads.Count, nt = model.Networks.Count, nv = model.Variables.Count;
            string terr = model.Ground.Kind == GroundKind.FreeSpace ? "spazio libero" : model.Ground.Kind == GroundKind.Perfect ? "perfetto" : "reale (er " + model.Ground.Epsr + ", sigma " + model.Ground.Sigma + ")";
            switch (formato)
            {
                case 0:
                case 1:
                    Riga3("Fili (" + nf + ")", "schede di geometria come nel modello (GW, GA, GH, GM...)", "si'");
                    Riga3("Variabili e formule (" + nv + ")", "schede SY (lette da 4nec2 e nec2c)", "si'");
                    Riga3("Sorgenti (" + ns + ")", model.CurrentSources ? "EX, con la riga CM YA-NEC CORRENTI=1" : "schede EX", "si'");
                    Riga3("Carichi (" + nl + ")", "schede LD", "si'");
                    Riga3("Linee di trasmissione (" + nt + ")", "schede TL / NT", "si'");
                    Riga3("Terreno e quota", terr + (model.Ground.Sommerfeld ? ", Sommerfeld (GN 2)" : ""), "si'");
                    Riga3("Frequenza, Z0", model.FreqMHz + " MHz, Z0 " + model.Z0 + " ohm (riga CM YA-NEC)", "si'");
                    Riga3("Segmentazione", "segmenti di YA-NEC nelle schede", "si'");
                    Riga3("Impostazioni YA-NEC", formato == 0 ? "righe CM YA-NEC (correnti, carichi, kernel, note)" : "righe CM ignorate dagli altri programmi", formato == 0 ? "si'" : "valori");
                    break;
                case 2:
                    Riga3("Fili (" + nf + ")", "coordinate in m, raggio come MMANA; segmenti allineati riuniti", "si'");
                    Riga3("Variabili e formule", "MMANA non ha formule: vengono scritti i numeri calcolati", "valori");
                    Riga3("Sorgenti (" + ns + ")", "riferimento wNb / wNc / wNe sul filo MMANA", "si'");
                    Riga3("Carichi (" + nl + ")", "R+jX e RLC serie come tipi MMANA; gli altri persi", nl == 0 ? "si'" : "valori");
                    Riga3("Linee di trasmissione (" + nt + ")", "MMANA non le gestisce: " + (nt > 0 ? "vanno perse" : "nessuna nel modello"), nt > 0 ? "no" : "si'");
                    Riga3("Terreno e quota", terr + ", quota 0 (le Z sono gia' assolute)", "si'");
                    Riga3("Frequenza, Z0", model.FreqMHz + " MHz, Z0 " + model.Z0 + " ohm", "si'");
                    Riga3("Segmentazione", segmenti == 0 ? "numero di segmenti di YA-NEC" : "MMANA risegmenta da solo (DM1 800, DM2 80, SC 2, EC 2)", "auto");
                    Riga3("Correzione Leeson", "non serve: MMANA (MININEC) non ha l'errore dei diametri a gradini", "no");
                    break;
                case 3:
                    Riga3("Fili (" + nf + ")", "righe dei fili in m, diametro in mm; GA/GH/GM diventano fili diritti", "si'");
                    Riga3("Variabili e formule (" + nv + ")", "righe 2-23 con le lettere di AutoEZ (A, B, D, E...); formule Excel dove non ci sono funzioni", nv == 0 ? "si'" : "valori");
                    Riga3("Sorgenti (" + ns + ")", "filo, % da E1, ampiezza, fase", "si'");
                    Riga3("Carichi (" + nl + ")", "R+jX (RLC alla frequenza del modello); LD 5 nel modello EZNEC", nl == 0 ? "si'" : "valori");
                    Riga3("Linee di trasmissione (" + nt + ")", "tabella Transmission Lines (N normale, R incrociata)", "si'");
                    Riga3("Terreno e quota", "AutoEZ usa il terreno del modello EZNEC aperto", model.Ground.Kind == GroundKind.FreeSpace ? "si'" : "no");
                    Riga3("Frequenza, Z0", model.FreqMHz + " MHz, Z0 " + model.Z0 + " ohm (riga Controls)", "si'");
                    Riga3("Segmentazione", segmenti == 0 ? "numero di segmenti di YA-NEC" : "formula di AutoEZ =ODD(WireLen / WL * 20)", "auto");
                    break;
                case 4:
                    Riga3("Fili (" + nf + ")", "righe di 170 byte in metri; GA/GH/GM diventano fili diritti", "si'");
                    Riga3("Variabili e formule", "EZNEC non ha formule: vengono scritti i numeri calcolati", nv == 0 ? "si'" : "valori");
                    Riga3("Sorgenti (" + ns + ")", "filo, % da E1, ampiezza, fase, V o I", "si'");
                    Riga3("Carichi (" + nl + ")", "R+jX, oppure RLC serie/parallelo; LD 5 come resistivita' dei fili", "si'");
                    Riga3("Linee di trasmissione (" + nt + ")", "Z0, lunghezza, VF 1; Z0 negativa = incrociata", "si'");
                    Riga3("Terreno e quota", terr + (model.Ground.Kind == GroundKind.Real ? (model.Ground.Sommerfeld ? ", High Accuracy" : ", MININEC") : ""), "si'");
                    Riga3("Frequenza, Z0", model.FreqMHz + " MHz", "si'");
                    Riga3("Segmentazione", "segmenti di YA-NEC", "si'");
                    Riga3("Correzione Leeson", model.Leeson ? "da riattivare in EZNEC (Stepped Diameter Correction)" : "non usata", model.Leeson ? "no" : "si'");
                    break;
            }
            avviso.Children.Clear();
            if (formato == 2 && (nt > 0 || nv > 0))
            {
                if (nt > 0)
                {
                    NetworkSpec n = model.Networks[0];
                    avviso.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(C("#5A4300")),
                        Text = "Attenzione: il modello ha " + nt + (nt == 1 ? " linea di trasmissione" : " linee di trasmissione") + " (filo " + n.Tag1 + " -> filo " + n.Tag2 + ", " + n.F1 + " ohm, " + n.F2 + " m). " +
                        "In MMANA i risultati saranno quelli dell'antenna senza la linea. Formati che la conservano: .yanec, .nec, .weq, .ez." });
                }
                if (nv > 0)
                    avviso.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(C("#5A4300")), Text = "Le " + nv + " variabili diventano numeri: le formule restano solo nel .yanec e nel .nec." });
            }
            avvisoBox.IsVisible = avviso.Children.Count > 0;
            cTubi.IsEnabled = formato == 2;
        }

        void Salva()
        {
            try
            {
                string dir = tCartella.Text.Trim(), nome = tNome.Text.Trim();
                if (nome.Length == 0) return;
                foreach (string e0 in Ext) if (nome.EndsWith(e0, StringComparison.OrdinalIgnoreCase)) nome = nome.Substring(0, nome.Length - e0.Length);
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, nome + Ext[formato]);
                string testo;
                List<string> perso = new List<string>();
                if (formato == 4)
                {
                    File.WriteAllBytes(path, FormatiExport.Ez(model, perso));
                    Risultato = path; Riapri = cRiapri.IsChecked == true; Perdite = perso; Close();
                    return;
                }
                if (formato == 2) testo = MmanaExport.Write(model, segmenti == 0, perso);
                else if (formato == 3) testo = FormatiExport.Weq(model, segmenti == 0, perso);
                else
                {
                    testo = formato == 1 ? FormatiExport.NecPerAltri(model, perso) : model.ToNec();
                    if (cFormule.IsChecked == true && model.Variables.Count > 0)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        foreach (SyVar v in model.Variables) sb.AppendLine("CM formula: " + v.Name + " = " + v.Expr);
                        testo = sb.ToString() + testo;
                    }
                    // 0.4.30: formato .yanec 2, la testata con l'abaco della descrizione va in cima (prima riga = firma del formato)
                    if (formato == 0) testo = YanecFile.Testo(model, Path.GetFileNameWithoutExtension(path), testo);
                }
                if (formato == 3) File.WriteAllText(path, testo, System.Text.Encoding.GetEncoding("ISO-8859-1"));
                else File.WriteAllText(path, testo);
                Risultato = path;
                Riapri = cRiapri.IsChecked == true;
                Perdite = perso;
                Close();
            }
            catch (Exception ex)
            {
                avviso.Children.Clear();
                avviso.Children.Add(new TextBlock { Text = "Errore nel salvataggio: " + ex.Message, Foreground = Pal.B(Pal.Red), TextWrapping = TextWrapping.Wrap });
                avvisoBox.IsVisible = true;
            }
        }

        public List<string> Perdite = new List<string>();
    }
}
