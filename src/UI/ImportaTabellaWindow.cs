// YA-NEC 1.9.0 - finestra "Importa tabella da immagine (OCR)": bozzetto approvato da IC8TEM il 18/11/2026 21:30
// (doc/bozzetti_approvati/Tabella_da_immagine_OCR_18-11-2026.html). Tre passi affiancati: 1 sorgente (immagine, PDF, incolla, cattura
// dello schermo; pagina, lingua, raddrizza, contrasto, ritaglio trascinando sull'anteprima), 2 tabella letta da correggere (ruolo delle
// colonne, unita', decimali, righe e colonne, copia per Excel; giallo = cifra incerta, rosso = controllo di plausibilita'),
// 3 dove mandarla (modello nuovo, tabella dei Fili, calcolatore con il confronto). Motore: Engine/TabellaOcr.cs.
#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public class ImportaTabellaWindow : Window
    {
        public Action<string, string> ApriNec;                    // modello nuovo (testo NEC, nome)
        public Func<List<GeomCard>, bool, string> InserisciFili;  // fili, al posto dei scelti -> messaggio
        public double FreqBarra = 144.3;

        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        byte[] originale;               // immagine (o null per un PDF con il testo)
        string nomeFonte = "";
        List<PaginaLetta> pagine = new List<PaginaLetta>();
        double angolo; double[] ritaglio;
        TabellaLetta tab = new TabellaLetta();
        List<int> ruoli = new List<int>();
        int cellaR = -1, cellaC = -1;
        bool carica;

        ComboBox cbPagina, cbLingua, cbUnita, cbDecimali, cbTipo, cbPol, cbAlim, cbCalc;
        RadioButton rbRaddAuto, rbRaddMano, rbContrNorm, rbContrForte, rbFondo, rbPosto;
        TextBox tbAngolo, tbFreq;
        TextBlock tStatoLettura, tSorgente, tRiassunto, tNoteControlli;
        CheckBox chkLeeson;
        Anteprima ante;
        StackPanel pTabella, pModello, pFili, pCalc, pConfronto;
        Button[] pill = new Button[3]; int dest;

        public ImportaTabellaWindow()
        {
            Title = Lang.T("Importa tabella da immagine (OCR)") + " - YA-NEC";
            Width = 1480; Height = 860; MinWidth = 1000; MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Pal.B(Pal.Panel);
            Content = Costruisci();
            KeyDown += async (s, e) => { if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control) && !(FocusManager?.GetFocusedElement() is TextBox)) { e.Handled = true; await Incolla(); } };
        }

        static TextBlock T(string s, double size = 12.5, bool muted = true) { return new TextBlock { Text = s, FontSize = size, Foreground = Pal.B(muted ? Pal.Muted : Pal.Fore), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center }; }
        static TextBlock Titolo(string n, string s)
        {
            var tb = new TextBlock { FontSize = 15, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), Margin = new Thickness(0, 0, 0, 6) };
            tb.Inlines = new Avalonia.Controls.Documents.InlineCollection { new Avalonia.Controls.Documents.Run { Text = n + "  ", Foreground = Pal.B(Pal.Accent) }, new Avalonia.Controls.Documents.Run { Text = s } };
            return tb;
        }
        static Border Riquadro(Control c) { return new Border { Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 0, 8, 0), Child = c }; }
        static StackPanel Riga(params Control[] c) { var s = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 2, 0, 2) }; foreach (var x in c) s.Children.Add(x); return s; }

        Control Costruisci()
        {
            var root = new DockPanel { Margin = new Thickness(12) };
            var testa = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 8) };
            testa.Children.Add(new TextBlock { Text = "Importa tabella da immagine (OCR)", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            testa.Children.Add(T("Tre passi: la sorgente (scansione, PDF, foto o schermata), la tabella letta da correggere, e dove mandarla (modello nuovo, tabella dei fili o calcolatore)."));
            DockPanel.SetDock(testa, Dock.Top); root.Children.Add(testa);

            var piede = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            var bAnn = new Button { Content = "Annulla" }; bAnn.Click += (s, e) => Close();
            var bCrea = new Button { Content = "Crea", Background = Pal.B(Pal.Accent), Foreground = Pal.B(Pal.AccentFore), MinWidth = 90 }; bCrea.Click += (s, e) => Crea();
            var come = T("OCR: Tesseract (Apache 2.0) con i dati di otto lingue (tessdata_fast); tutto sul PC, senza rete. Un PDF con testo vero si legge senza OCR. Le cifre incerte (O/0, l/1, S/5, B/8) si correggono da sole e restano gialle; quello che non torna è rosso ma non blocca.");
            come.MaxWidth = 900;
            piede.Children.Add(come); piede.Children.Add(bAnn); piede.Children.Add(bCrea);
            DockPanel.SetDock(piede, Dock.Bottom); root.Children.Add(piede);

            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("1*,1.35*,1*") };
            // ---------------------------------------------------------------- 1 sorgente
            var s1 = new StackPanel { Spacing = 6 };
            s1.Children.Add(Titolo("1", "Sorgente"));
            var bApri = new Button { Content = "Apri immagine o PDF…" }; bApri.Click += async (s, e) => await Apri();
            var bInc = new Button { Content = "Incolla (Ctrl+V)" }; bInc.Click += async (s, e) => await Incolla();
            var bCatt = new Button { Content = "Cattura schermo" }; bCatt.Click += async (s, e) => await Cattura();
            ToolTip.SetTip(bCatt, "La finestra si nasconde, si fotografa lo schermo e poi si ritaglia la tabella trascinando sull'anteprima.");
            var wb = new WrapPanel(); foreach (var b in new[] { bApri, bInc, bCatt }) { b.Margin = new Thickness(0, 0, 6, 4); wb.Children.Add(b); }
            s1.Children.Add(wb);
            s1.Children.Add(T("Formati: PNG, JPG, TIFF, BMP, PDF (pagina scelta). Un PDF con testo vero si legge senza OCR, senza errori."));
            cbPagina = new ComboBox { MinWidth = 150 };
            cbPagina.SelectionChanged += (s, e) => { if (!carica) ScegliPagina(); };
            cbLingua = new ComboBox { ItemsSource = TabellaOcr.Lingue, SelectedIndex = LinguaIniziale() };
            s1.Children.Add(Riga(T("Pagina"), cbPagina, T("Lingua"), cbLingua));
            tSorgente = T("", 12);
            s1.Children.Add(tSorgente);
            ante = new Anteprima { Height = 330 };
            ante.Ritagliato += r => { ritaglio = r; tStatoLettura.Text = "Ritaglio: trascina di nuovo per cambiarlo, doppio clic per toglierlo."; };
            ante.Tolto += () => { ritaglio = null; tStatoLettura.Text = "Ritaglio tolto."; };
            s1.Children.Add(new Border { BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(1), Child = ante });
            s1.Children.Add(T("Trascina sull'anteprima per ritagliare la tabella (doppio clic: tutta la pagina).", 11.5));
            rbRaddAuto = new RadioButton { Content = "automatico", GroupName = "radd", IsChecked = true };
            rbRaddMano = new RadioButton { Content = "manuale", GroupName = "radd" };
            tbAngolo = new TextBox { Text = "0", Width = 60 };
            rbRaddAuto.IsCheckedChanged += (s, e) => { if (rbRaddAuto.IsChecked == true) Raddrizza(); };
            tbAngolo.LostFocus += (s, e) => { double a; if (double.TryParse((tbAngolo.Text ?? "").Replace(',', '.'), NumberStyles.Float, CI, out a)) { rbRaddMano.IsChecked = true; angolo = a; MostraAnteprima(); } };
            s1.Children.Add(Riga(T("Raddrizza"), rbRaddAuto, rbRaddMano, tbAngolo, T("gradi")));
            rbContrNorm = new RadioButton { Content = "automatico", GroupName = "contr", IsChecked = true };
            rbContrForte = new RadioButton { Content = "forte (foto)", GroupName = "contr" };
            s1.Children.Add(Riga(T("Contrasto"), rbContrNorm, rbContrForte));
            var bLeggi = new Button { Content = "Leggi la tabella", Background = Pal.B(Pal.Accent), Foreground = Pal.B(Pal.AccentFore) };
            bLeggi.Click += async (s, e) => await Leggi();
            tStatoLettura = T("");
            s1.Children.Add(Riga(bLeggi, tStatoLettura));
            var c1 = Riquadro(new ScrollViewer { Content = s1 });
            g.Children.Add(c1);

            // ---------------------------------------------------------------- 2 tabella letta
            var s2 = new StackPanel { Spacing = 6 };
            s2.Children.Add(Titolo("2", "Tabella letta (si corregge qui)"));
            pTabella = new StackPanel { Spacing = 2 };
            s2.Children.Add(new ScrollViewer { Content = pTabella, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
            var leg = new WrapPanel();
            leg.Children.Add(new Border { Background = new SolidColorBrush(Giallo()), Padding = new Thickness(6, 1), Margin = new Thickness(0, 0, 6, 0), Child = T("letto con poca sicurezza (il fumetto dice cosa ha letto)", 11.5, false) });
            leg.Children.Add(new Border { Background = new SolidColorBrush(Rosso()), Padding = new Thickness(6, 1), Child = T("valore da controllare", 11.5, false) });
            s2.Children.Add(leg);
            cbUnita = new ComboBox { ItemsSource = TabellaOcr.Unita, SelectedIndex = 0 };
            cbUnita.SelectionChanged += (s, e) => { if (!carica) { tab.Unita = cbUnita.SelectedIndex; Ricontrolla(); } };
            cbDecimali = new ComboBox { ItemsSource = new[] { "virgola", "punto" }, SelectedIndex = 0 };
            cbDecimali.SelectionChanged += (s, e) => { if (!carica) { tab.Virgola = cbDecimali.SelectedIndex == 0; Ricontrolla(); } };
            s2.Children.Add(Riga(T("Unità"), cbUnita, T("Decimali"), cbDecimali));
            var bPiu = new Button { Content = "+ riga" }; bPiu.Click += (s, e) => { SincronizzaDaCelle(); var r = new List<CellaLetta>(); for (int k = 0; k < tab.Colonne; k++) r.Add(new CellaLetta()); tab.Righe.Insert(cellaR >= 0 ? cellaR + 1 : tab.Righe.Count, r); Disegna(); };
            var bMeno = new Button { Content = "− riga" }; bMeno.Click += (s, e) => { SincronizzaDaCelle(); if (tab.Righe.Count == 0) return; tab.Righe.RemoveAt(cellaR >= 0 && cellaR < tab.Righe.Count ? cellaR : tab.Righe.Count - 1); cellaR = -1; Disegna(); };
            var bUni = new Button { Content = "Unisci colonne" }; bUni.Click += (s, e) => Unisci();
            ToolTip.SetTip(bUni, "Unisce la colonna della cella scelta con quella a destra.");
            var bDiv = new Button { Content = "Dividi colonna" }; bDiv.Click += (s, e) => Dividi();
            ToolTip.SetTip(bDiv, "Divide la colonna della cella scelta al primo spazio di ogni cella.");
            var bCopia = new Button { Content = "Copia (per Excel)" }; bCopia.Click += async (s, e) => await Copia();
            var wb2 = new WrapPanel(); foreach (var b in new[] { bPiu, bMeno, bUni, bDiv, bCopia }) { b.Margin = new Thickness(0, 0, 6, 4); wb2.Children.Add(b); }
            s2.Children.Add(wb2);
            tNoteControlli = T("");
            s2.Children.Add(tNoteControlli);
            var c2 = Riquadro(new ScrollViewer { Content = s2 });
            Grid.SetColumn(c2, 1); g.Children.Add(c2);

            // ---------------------------------------------------------------- 3 dove la mando
            var s3 = new StackPanel { Spacing = 6 };
            s3.Children.Add(Titolo("3", "Dove la mando"));
            var pp = new WrapPanel();
            string[] np = { "Modello nuovo", "Tabella dei fili", "Calcolatore" };
            for (int k = 0; k < 3; k++) { int kk = k; pill[k] = new Button { Content = np[k], Margin = new Thickness(0, 0, 6, 4), CornerRadius = new CornerRadius(12) }; pill[k].Click += (s, e) => Destinazione(kk); pp.Children.Add(pill[k]); }
            s3.Children.Add(pp);
            pModello = new StackPanel { Spacing = 6 };
            cbTipo = new ComboBox { ItemsSource = TabellaOcr.Tipi, SelectedIndex = 0, MinWidth = 260 };
            cbTipo.SelectionChanged += (s, e) => { if (!carica) Ricontrolla(); };
            pModello.Children.Add(Riga(T("Tipo"), cbTipo));
            tbFreq = new TextBox { Text = FreqBarra.ToString("0.###", CI), Width = 80 };
            tbFreq.LostFocus += (s, e) => Ricontrolla();
            cbPol = new ComboBox { ItemsSource = new[] { "orizzontale", "verticale" }, SelectedIndex = 0 };
            pModello.Children.Add(Riga(T("Frequenza"), tbFreq, T("MHz"), T("Polarizzazione"), cbPol));
            cbAlim = new ComboBox { MinWidth = 160 };
            cbAlim.SelectionChanged += (s, e) => { if (!carica) Riassumi(); };
            pModello.Children.Add(Riga(T("Alimentato"), cbAlim, T("Segmenti automatici (λ/20)")));
            tRiassunto = T("", 12.5, false);
            pModello.Children.Add(tRiassunto);
            pModello.Children.Add(T("Dopo «Crea» si calcola subito: guadagno, F/B e ROS compaiono come per un modello aperto."));
            chkLeeson = new CheckBox { Content = "correzione di Leeson se gli elementi sono a tubi telescopici" };
            pModello.Children.Add(chkLeeson);
            s3.Children.Add(pModello);
            pFili = new StackPanel { Spacing = 6, IsVisible = false };
            rbFondo = new RadioButton { Content = "Inserisci in fondo alla tabella dei Fili", GroupName = "fili", IsChecked = true };
            rbPosto = new RadioButton { Content = "al posto dei fili scelti", GroupName = "fili" };
            pFili.Children.Add(rbFondo); pFili.Children.Add(rbPosto);
            pFili.Children.Add(T("Ogni riga diventa un filo (estremi simmetrici sull'asse Y, alla posizione X sul boom). Si annulla con «Annulla» come le altre modifiche della geometria. Con «Fili liberi» le colonne X1…Z2, raggio e segmenti si copiano così come sono. Tipo, frequenza e polarizzazione sono quelli di «Modello nuovo»."));
            s3.Children.Add(pFili);
            pCalc = new StackPanel { Spacing = 6, IsVisible = false };
            cbCalc = new ComboBox { ItemsSource = new[] { "Yagi lunga DL6WU (confronto)", "Tubi telescopici (rastremazioni)", "Quad / loop" }, SelectedIndex = 0 };
            pCalc.Children.Add(Riga(T("Calcolatore"), cbCalc));
            pCalc.Children.Add(T("Le misure lette vanno accanto a quelle calcolate dal calcolatore, colonna per colonna, con la differenza in mm: serve a confrontare un progetto pubblicato con il calcolo. Frequenza e diametro sono quelli della tabella."));
            pConfronto = new StackPanel { Spacing = 2 };
            pCalc.Children.Add(pConfronto);
            s3.Children.Add(pCalc);
            s3.Children.Add(T("Si salvano insieme al modello l'immagine ritagliata e la tabella corretta (righe CM con la fonte: titolo, pagina), così si sa sempre da dove vengono le misure."));
            var c3 = Riquadro(new ScrollViewer { Content = s3 });
            c3.Margin = new Thickness(0);
            Grid.SetColumn(c3, 2); g.Children.Add(c3);
            root.Children.Add(g);
            Destinazione(0);
            Disegna();
            return root;
        }

        static Color Giallo() { return Color.Parse(Pal.Dark ? "#5A4A12" : "#FFF1B8"); }
        static Color Rosso() { return Color.Parse(Pal.Dark ? "#5C2323" : "#FFD5D0"); }

        static int LinguaIniziale()
        {
            string[] ui = { "it", "en", "de", "ru", "ja", "fr", "pt", "es" };
            int k = Array.IndexOf(ui, (Lang.Code ?? "it").Split('-')[0]);
            return k < 0 ? 0 : k;
        }

        void Destinazione(int k)
        {
            dest = k;
            for (int i = 0; i < 3; i++)
            {
                pill[i].Background = Pal.B(i == k ? Pal.Accent : Pal.Pill);
                pill[i].Foreground = Pal.B(i == k ? Pal.AccentFore : Pal.PillFore);
            }
            pModello.IsVisible = k != 2; pFili.IsVisible = k == 1; pCalc.IsVisible = k == 2;
        }

        // ------------------------------------------------------------------------------------------ sorgente
        async Task Apri()
        {
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Tabella delle misure: immagine o PDF", AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("Immagini e PDF") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.tif", "*.tiff", "*.bmp", "*.gif", "*.webp", "*.pdf" } } }
                });
                if (files == null || files.Count == 0) return;
                string p = files[0].TryGetLocalPath();
                if (p != null) await Carica(p);
            }
            catch (Exception ex) { tStatoLettura.Text = "Errore: " + ex.Message; }
        }

        /// <summary>Carica un file (anche dalle prove: YANEC_IMPORTA_TABELLA=file).</summary>
        public async Task Carica(string p)
        {
            nomeFonte = Path.GetFileName(p);
            Registro.Scrivi("AZIONE", "importa tabella: " + nomeFonte);
            if (p.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                tSorgente.Text = "Lettura del PDF…";
                try { pagine = await Task.Run(() => LetturaProgetto.LeggiPdf(p)); }
                catch (Exception ex) { tSorgente.Text = "PDF non leggibile: " + ex.Message; return; }
                if (pagine.Count == 0) { tSorgente.Text = "Nel PDF non ci sono pagine con numeri né immagini grandi."; return; }
                carica = true;
                cbPagina.ItemsSource = pagine.Select(q => q.Descrizione).ToList();
                cbPagina.SelectedIndex = Math.Max(0, LetturaProgetto.Proposta(pagine));
                carica = false;
                ScegliPagina();
            }
            else
            {
                pagine = new List<PaginaLetta>();
                carica = true; cbPagina.ItemsSource = null; carica = false;
                Immagine(File.ReadAllBytes(p), nomeFonte);
            }
        }

        void ScegliPagina()
        {
            int i = cbPagina.SelectedIndex;
            if (i < 0 || i >= pagine.Count) return;
            var pg = pagine[i];
            if (pg.DaOcr) Immagine(pg.Png, nomeFonte + ", " + pg.Descrizione);
            else
            {
                originale = null; ritaglio = null;
                ante.Imposta(null, pg);
                tSorgente.Text = nomeFonte + ", " + pg.Descrizione + ": testo vero, si legge senza OCR.";
            }
        }

        void Immagine(byte[] png, string descr)
        {
            originale = png; ritaglio = null; angolo = 0;
            tSorgente.Text = descr;
            nomeFonteCompleta = descr;
            if (rbRaddAuto.IsChecked == true) Raddrizza(); else MostraAnteprima();
        }
        string nomeFonteCompleta = "";

        void Raddrizza()
        {
            if (originale == null) return;
            try { angolo = TabellaOcr.AngoloRaddrizza(originale); } catch { angolo = 0; }
            tbAngolo.Text = angolo.ToString("0.0", CI);
            MostraAnteprima();
        }

        void MostraAnteprima()
        {
            if (originale == null) return;
            try { ante.Imposta(Math.Abs(angolo) > 0.05 ? TabellaOcr.Prepara(originale, angolo, false, null) : originale, null); }
            catch { ante.Imposta(originale, null); }
            ritaglio = null;
        }

        async Task Incolla()
        {
            try
            {
                var cb = Clipboard;
                if (cb == null) return;
                var formati = await cb.GetFormatsAsync();
                foreach (string f in formati ?? new string[0])
                {
                    string fl = f.ToLowerInvariant();
                    if (!(fl.Contains("png") || fl.Contains("bitmap") || fl.Contains("image") || fl.Contains("dib"))) continue;
                    var dati = await cb.GetDataAsync(f);
                    byte[] b = dati as byte[];
                    if (b == null && dati is Stream st) { using (var ms = new MemoryStream()) { st.CopyTo(ms); b = ms.ToArray(); } }
                    if (b == null || b.Length < 40) continue;
                    if (fl.Contains("dib") || fl == "bitmap") b = BmpDaDib(b);
                    using (var test = SkiaSharp.SKBitmap.Decode(b)) if (test == null) continue;
                    pagine = new List<PaginaLetta>(); carica = true; cbPagina.ItemsSource = null; carica = false;
                    nomeFonte = "appunti";
                    Immagine(b, "immagine incollata dagli appunti");
                    return;
                }
                tStatoLettura.Text = "Negli appunti non c'è un'immagine.";
            }
            catch (Exception ex) { tStatoLettura.Text = "Incolla non riuscito: " + ex.Message; }
        }

        // immagine DIB di Windows (appunti) -> file BMP
        static byte[] BmpDaDib(byte[] dib)
        {
            if (dib.Length > 2 && dib[0] == 'B' && dib[1] == 'M') return dib;
            int hdr = BitConverter.ToInt32(dib, 0), bpp = BitConverter.ToInt16(dib, 14), clr = BitConverter.ToInt32(dib, 32), compr = BitConverter.ToInt32(dib, 16);
            int pal = (clr == 0 && bpp <= 8 ? (1 << bpp) : clr) * 4 + (compr == 3 && hdr == 40 ? 12 : 0);
            var o = new byte[14 + dib.Length];
            o[0] = (byte)'B'; o[1] = (byte)'M';
            BitConverter.GetBytes(o.Length).CopyTo(o, 2);
            BitConverter.GetBytes(14 + hdr + pal).CopyTo(o, 10);
            Array.Copy(dib, 0, o, 14, dib.Length);
            return o;
        }

        async Task Cattura()
        {
            string dir = Path.Combine(AppPaths.TempDir, "cattura");
            Directory.CreateDirectory(dir);
            string f = Path.Combine(dir, "schermo.png");
            try { if (File.Exists(f)) File.Delete(f); } catch { }
            var stato = WindowState;
            var princ = Owner as Window; var statoP = princ?.WindowState ?? WindowState.Normal;
            WindowState = WindowState.Minimized; if (princ != null) princ.WindowState = WindowState.Minimized;
            await Task.Delay(700);
            string err = await Task.Run(() => Fotografa(f));
            WindowState = stato; if (princ != null) princ.WindowState = statoP;
            Activate();
            if (err != null || !File.Exists(f)) { tStatoLettura.Text = "Cattura non riuscita: " + (err ?? "nessun file") + ". Si può usare lo strumento del sistema e poi «Incolla»."; return; }
            pagine = new List<PaginaLetta>(); carica = true; cbPagina.ItemsSource = null; carica = false;
            nomeFonte = "cattura dello schermo";
            Immagine(File.ReadAllBytes(f), "cattura dello schermo: trascina per ritagliare la tabella");
            try { File.Delete(f); } catch { }
        }

        static string Fotografa(string f)
        {
            var prove = new List<string[]>();
            if (OperatingSystem.IsWindows())
                prove.Add(new[] { "powershell", "-NoProfile", "-Command", "Add-Type -AssemblyName System.Windows.Forms,System.Drawing; $b=[System.Windows.Forms.SystemInformation]::VirtualScreen; $i=New-Object System.Drawing.Bitmap $b.Width,$b.Height; $g=[System.Drawing.Graphics]::FromImage($i); $g.CopyFromScreen($b.Left,$b.Top,0,0,$i.Size); $i.Save('" + f.Replace("'", "''") + "',[System.Drawing.Imaging.ImageFormat]::Png)" });
            else if (OperatingSystem.IsMacOS()) prove.Add(new[] { "screencapture", "-x", f });
            else
            {
                prove.Add(new[] { "gnome-screenshot", "-f", f }); prove.Add(new[] { "spectacle", "-b", "-n", "-o", f }); prove.Add(new[] { "grim", f });
                prove.Add(new[] { "scrot", "-o", f }); prove.Add(new[] { "import", "-window", "root", f });
            }
            string ultimo = "nessun programma di cattura trovato";
            foreach (var p in prove)
            {
                try
                {
                    var psi = new ProcessStartInfo { FileName = p[0], UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
                    for (int k = 1; k < p.Length; k++) psi.ArgumentList.Add(p[k]);
                    using (var pr = Process.Start(psi)) { pr.StandardOutput.ReadToEnd(); pr.WaitForExit(20000); }
                    if (File.Exists(f) && new FileInfo(f).Length > 100) return null;
                }
                catch (Exception ex) { ultimo = p[0] + ": " + ex.Message; }
            }
            return ultimo;
        }

        // ------------------------------------------------------------------------------------------ lettura
        async Task Leggi()
        {
            var sw = Stopwatch.StartNew();
            PaginaLetta pg = null; string err = null;
            int i = cbPagina.SelectedIndex;
            if (originale == null && i >= 0 && i < pagine.Count && !pagine[i].DaOcr) pg = pagine[i];
            else if (originale != null)
            {
                string lingua = TabellaOcr.CodiciLingua[Math.Max(0, cbLingua.SelectedIndex)];
                if (!TabellaOcr.LinguaPresente(lingua)) lingua = "eng";
                if (lingua == "jpn") lingua = "jpn+eng";
                tStatoLettura.Text = "OCR in corso…";
                double a = angolo; bool forte = rbContrForte.IsChecked == true; var rit = ritaglio; byte[] img = originale;
                pg = await Task.Run(() =>
                {
                    byte[] pr = TabellaOcr.Prepara(img, a, forte, rit);
                    string e2; var r = LetturaProgetto.Ocr(pr, nomeFonteCompleta, out e2, lingua, 6);
                    err = e2; return r;
                });
            }
            else { tStatoLettura.Text = "Prima apri, incolla o cattura una tabella."; return; }
            if (pg == null) { tStatoLettura.Text = err; return; }
            tab = TabellaOcr.Riconosci(pg);
            double f = LetturaProgetto.FrequenzaDalTesto(new[] { tab.Titolo });
            if (f > 0) tbFreq.Text = f.ToString("0.###", CI);
            ruoli = TabellaOcr.RuoliProbabili(tab, Freq());
            carica = true; cbUnita.SelectedIndex = tab.Unita; cbDecimali.SelectedIndex = tab.Virgola ? 0 : 1; carica = false;
            tStatoLettura.Text = tab.Righe.Count == 0 ? "Nessuna tabella trovata: ritaglia la tabella o prova il contrasto forte." :
                tab.Righe.Count + " righe × " + tab.Colonne + " colonne, " + (sw.ElapsedMilliseconds / 1000.0).ToString("0.0", CultureInfo.CurrentCulture) + " s";
            Registro.Scrivi("AZIONE", "importa tabella: " + tStatoLettura.Text);
            Disegna();
        }

        double Freq() { double f; return double.TryParse((tbFreq?.Text ?? "").Replace(',', '.'), NumberStyles.Float, CI, out f) && f > 0 ? f : FreqBarra; }

        // ------------------------------------------------------------------------------------------ tabella
        readonly List<List<TextBox>> celle = new List<List<TextBox>>();
        readonly List<TextBox> intestazioni = new List<TextBox>();

        void Disegna()
        {
            pTabella.Children.Clear(); celle.Clear(); intestazioni.Clear();
            tab.Pareggia();
            int n = tab.Colonne;
            while (ruoli.Count < n) ruoli.Add(TabellaOcr.RIgnora);
            while (ruoli.Count > n) ruoli.RemoveAt(ruoli.Count - 1);
            if (n == 0) { pTabella.Children.Add(T("La tabella letta compare qui: apri un'immagine o un PDF e premi «Leggi la tabella».")); AggiornaAlim(); return; }
            if (tab.Titolo.Length > 0) pTabella.Children.Add(T(tab.Titolo, 12, false));
            string cols = string.Join(",", Enumerable.Repeat("140", n));
            var gr = new Grid { ColumnDefinitions = new ColumnDefinitions(cols) };
            for (int k = 0; k < n; k++)
            {
                int kk = k;
                var cb = new ComboBox { ItemsSource = TabellaOcr.Ruoli, SelectedIndex = ruoli[k], FontSize = 11.5, Margin = new Thickness(0, 0, 2, 2), HorizontalAlignment = HorizontalAlignment.Stretch };
                cb.SelectionChanged += (s, e) => { ruoli[kk] = cb.SelectedIndex; Ricontrolla(); };
                Grid.SetColumn(cb, k); gr.Children.Add(cb);
            }
            pTabella.Children.Add(gr);
            var gh = new Grid { ColumnDefinitions = new ColumnDefinitions(cols) };
            for (int k = 0; k < n; k++)
            {
                var tb = new TextBox { Text = tab.Intestazioni[k], FontWeight = FontWeight.SemiBold, FontSize = 12, Margin = new Thickness(0, 0, 2, 2) };
                int kk = k; tb.LostFocus += (s, e) => tab.Intestazioni[kk] = tb.Text ?? "";
                intestazioni.Add(tb); Grid.SetColumn(tb, k); gh.Children.Add(tb);
            }
            pTabella.Children.Add(gh);
            for (int i = 0; i < tab.Righe.Count; i++)
            {
                var gg = new Grid { ColumnDefinitions = new ColumnDefinitions(cols) };
                var riga = new List<TextBox>();
                for (int k = 0; k < n; k++)
                {
                    int ii = i, kk = k;
                    var tb = new TextBox { Text = tab.Righe[i][k].Testo, FontSize = 12.5, Margin = new Thickness(0, 0, 2, 1) };
                    tb.GotFocus += (s, e) => { cellaR = ii; cellaC = kk; };
                    tb.LostFocus += (s, e) =>
                    {
                        if (ii < tab.Righe.Count && kk < tab.Righe[ii].Count && tab.Righe[ii][kk].Testo != (tb.Text ?? ""))
                        { tab.Righe[ii][kk].Testo = tb.Text ?? ""; tab.Righe[ii][kk].Incerta = false; Ricontrolla(); }
                    };
                    riga.Add(tb); Grid.SetColumn(tb, k); gg.Children.Add(tb);
                }
                celle.Add(riga);
                pTabella.Children.Add(gg);
            }
            Ricontrolla();
        }

        void SincronizzaDaCelle()
        {
            for (int i = 0; i < celle.Count && i < tab.Righe.Count; i++)
                for (int k = 0; k < celle[i].Count && k < tab.Righe[i].Count; k++)
                    if (tab.Righe[i][k].Testo != (celle[i][k].Text ?? "")) { tab.Righe[i][k].Testo = celle[i][k].Text ?? ""; tab.Righe[i][k].Incerta = false; }
            for (int k = 0; k < intestazioni.Count && k < tab.Intestazioni.Count; k++) tab.Intestazioni[k] = intestazioni[k].Text ?? "";
        }

        void Ricontrolla()
        {
            if (tab == null) return;
            var note = TabellaOcr.Controlla(tab, ruoli, Freq(), Math.Max(0, cbTipo?.SelectedIndex ?? 0));
            for (int i = 0; i < celle.Count && i < tab.Righe.Count; i++)
                for (int k = 0; k < celle[i].Count && k < tab.Righe[i].Count; k++)
                {
                    var c = tab.Righe[i][k];
                    celle[i][k].Background = c.Rossa ? new SolidColorBrush(Rosso()) : c.Incerta ? new SolidColorBrush(Giallo()) : null;
                    string tip = c.Rossa ? c.Nota : c.Incerta ? (c.Letto != null ? "letto: \"" + c.Letto + "\"" : "letto con poca sicurezza") : null;
                    ToolTip.SetTip(celle[i][k], tip);
                }
            tNoteControlli.Text = note.Count > 0 ? "Controlli (non bloccano): " + string.Join("; ", note) + "." : "";
            AggiornaAlim();
        }

        void AggiornaAlim()
        {
            int kn = ruoli.IndexOf(TabellaOcr.RNome);
            var nomi = tab.Righe.Select((r, i) => kn >= 0 && r[kn].Testo.Length > 0 ? r[kn].Testo + (i == 1 ? " (secondo elemento)" : "") : "riga " + (i + 1)).ToList();
            int sel = cbAlim.SelectedIndex;
            if (sel < 0 || sel >= nomi.Count)
            {
                sel = nomi.FindIndex(x => System.Text.RegularExpressions.Regex.IsMatch(x, @"^(DR|DE|A|RAD|DIP|DRIV|FED|ALIM)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
                if (sel < 0) sel = cbTipo.SelectedIndex == 2 ? 0 : Math.Min(1, nomi.Count - 1);
            }
            carica = true; cbAlim.ItemsSource = nomi; cbAlim.SelectedIndex = sel; carica = false;
            Riassumi();
        }

        void Riassumi()
        {
            if (tRiassunto == null) return;
            if (tab.Righe.Count == 0) { tRiassunto.Text = ""; return; }
            string err; var el = TabellaOcr.Elementi(tab, ruoli, Freq(), 0.006, out err);
            if (err != null) { tRiassunto.Text = "Da sistemare: " + err + "."; return; }
            string nec = TabellaOcr.Nec(el, cbTipo.SelectedIndex, Freq(), cbPol.SelectedIndex == 1, cbAlim.SelectedIndex, false, tab.Titolo, nomeFonteCompleta, tab, ruoli);
            tRiassunto.Text = TabellaOcr.Riassunto(el, cbTipo.SelectedIndex, Freq(), nec) + (ruoli.Contains(TabellaOcr.RDiam) || ruoli.Contains(TabellaOcr.RRaggio) ? "" : " Senza colonna del diametro: 6 mm.");
        }

        void Unisci()
        {
            SincronizzaDaCelle();
            int c = cellaC; if (c < 0 || c + 1 >= tab.Colonne) { tStatoLettura.Text = "Scegli una cella della colonna da unire con quella a destra."; return; }
            foreach (var r in tab.Righe) { r[c].Testo = (r[c].Testo + " " + r[c + 1].Testo).Trim(); r.RemoveAt(c + 1); }
            tab.Intestazioni[c] = (tab.Intestazioni[c] + " " + tab.Intestazioni[c + 1]).Trim(); tab.Intestazioni.RemoveAt(c + 1);
            ruoli.RemoveAt(c + 1);
            Disegna();
        }

        void Dividi()
        {
            SincronizzaDaCelle();
            int c = cellaC; if (c < 0 || c >= tab.Colonne) { tStatoLettura.Text = "Scegli una cella della colonna da dividere."; return; }
            foreach (var r in tab.Righe)
            {
                string t = r[c].Testo.Trim(); int sp = t.IndexOf(' ');
                r[c].Testo = sp < 0 ? t : t.Substring(0, sp);
                r.Insert(c + 1, new CellaLetta { Testo = sp < 0 ? "" : t.Substring(sp + 1).Trim() });
            }
            tab.Intestazioni.Insert(c + 1, ""); ruoli.Insert(c + 1, TabellaOcr.RIgnora);
            Disegna();
        }

        async Task Copia()
        {
            SincronizzaDaCelle();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join("\t", tab.Intestazioni));
            foreach (var r in tab.Righe) sb.AppendLine(string.Join("\t", r.Select(c => c.Testo)));
            try { if (Clipboard != null) await Clipboard.SetTextAsync(sb.ToString()); tStatoLettura.Text = "Tabella copiata: si incolla in Excel o nel foglio di calcolo."; }
            catch (Exception ex) { tStatoLettura.Text = "Copia non riuscita: " + ex.Message; }
        }

        // ------------------------------------------------------------------------------------------ crea
        void Crea()
        {
            SincronizzaDaCelle();
            Ricontrolla();
            if (tab.Righe.Count == 0) { tStatoLettura.Text = "Nessuna tabella da mandare: prima «Leggi la tabella»."; return; }
            string err; var el = TabellaOcr.Elementi(tab, ruoli, Freq(), 0.006, out err);
            if (err != null) { tStatoLettura.Text = "Da sistemare: " + err + "."; return; }
            int tipo = cbTipo.SelectedIndex; double f = Freq();
            if (dest == 2) { Confronta(el); return; }
            string fonte = nomeFonteCompleta + FonteImmagine();
            string nec = TabellaOcr.Nec(el, tipo, f, cbPol.SelectedIndex == 1, cbAlim.SelectedIndex, chkLeeson.IsChecked == true, tab.Titolo, fonte, tab, ruoli);
            if (dest == 0)
            {
                string nome = (string.IsNullOrWhiteSpace(tab.Titolo) ? "Tabella importata" : Pulito(tab.Titolo)) + ".nec";
                Registro.Scrivi("AZIONE", "importa tabella: modello nuovo, " + el.Count + " righe");
                ApriNec?.Invoke(nec, nome);
                Close();
            }
            else
            {
                try
                {
                    var m = NecModel.Parse(nec);
                    string msg = InserisciFili?.Invoke(m.Geometry, rbPosto.IsChecked == true);
                    Registro.Scrivi("AZIONE", "importa tabella: " + m.Geometry.Count + " fili nella tabella dei Fili");
                    if (msg != null) { tStatoLettura.Text = msg; return; }
                    Close();
                }
                catch (Exception ex) { tStatoLettura.Text = "Fili non inseriti: " + ex.Message; }
            }
        }

        static string Pulito(string s) { foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, ' '); s = s.Trim(); return s.Length > 60 ? s.Substring(0, 60).Trim() : s; }

        // immagine ritagliata salvata accanto ai dati (data/fonti_tabelle), citata nelle righe CM del modello
        string FonteImmagine()
        {
            if (originale == null) return "";
            try
            {
                string dir = Path.Combine(AppPaths.DataDir, "fonti_tabelle"); Directory.CreateDirectory(dir);
                string f = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CI) + ".png");
                File.WriteAllBytes(f, TabellaOcr.Prepara(originale, angolo, false, ritaglio));
                return "; immagine: data/fonti_tabelle/" + Path.GetFileName(f);
            }
            catch { return ""; }
        }

        void Confronta(List<ElementoTab> el)
        {
            pConfronto.Children.Clear();
            int c = cbCalc.SelectedIndex; double f = Freq();
            try
            {
                string necCalc; bool perimetro = false; string nota;
                var ord = el.OrderBy(e => e.Pos).ToList();
                double dPar = ord.Count > 2 ? ord[2].Diam : ord[0].Diam, dRad = ord.Count > 1 ? ord[1].Diam : dPar;
                if (c == 0)
                {
                    var calc = new CalcYagiDl6wu();
                    Imposta(calc, "f", f); Imposta(calc, "modo", 0); Imposta(calc, "nel", el.Count); Imposta(calc, "ed", dPar * 1000); Imposta(calc, "dd", dRad * 1000);
                    Imposta(calc, "mont", 2); Imposta(calc, "passo", 0); Imposta(calc, "bw", 0);
                    necCalc = calc.Compute().Nec; nota = "Yagi lunga DL6WU com'è (passo 0, elementi sopra il boom), " + el.Count + " elementi a " + f.ToString("0.###", CI) + " MHz";
                }
                else if (c == 2)
                {
                    var calc = new CalcQuagi();
                    Imposta(calc, "f", f); Imposta(calc, "nel", el.Count); Imposta(calc, "tipo", 1); Imposta(calc, "dl", dPar * 1000); Imposta(calc, "passo", 0); Imposta(calc, "bw", 0);
                    necCalc = calc.Compute().Nec; perimetro = true; nota = "Quad (N6NB/Cebik com'è, anelli anche per i direttori), lunghezza = perimetro";
                }
                else
                {
                    string nec = TabellaOcr.Nec(el, 0, f, false, cbAlim.SelectedIndex, false, tab.Titolo, nomeFonteCompleta, tab, ruoli);
                    var note = new List<string>();
                    necCalc = RastremazioneHf.ApplicaTesto(nec, RastremazioneHf.Predefinita, note);
                    nota = "Tubi telescopici " + RastremazioneHf.Predefinita + " mm (Leeson): lunghezza da punta a punta equivalente alla tabella";
                }
                if (string.IsNullOrEmpty(necCalc)) { pConfronto.Children.Add(T("Il calcolatore non ha dato un modello.")); return; }
                var righe = TabellaOcr.Confronto(el, necCalc, perimetro);
                pConfronto.Children.Add(T(nota + ". Valori in mm.", 12, false));
                var intest = new[] { "elemento", "pos. letta", "pos. calc.", "diff.", "lungh. letta", "lungh. calc.", "diff." };
                var g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto") };
                for (int r = 0; r <= righe.Count; r++)
                {
                    g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    var v = r == 0 ? intest : righe[r - 1];
                    for (int k = 0; k < 7; k++)
                    {
                        var tb = new TextBlock { Text = v[k], FontSize = 12, Margin = new Thickness(0, 1, 10, 1), Foreground = Pal.B(r == 0 ? Pal.Muted : k == 3 || k == 6 ? Pal.Accent : Pal.Fore), HorizontalAlignment = k == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                        Grid.SetRow(tb, r); Grid.SetColumn(tb, k); g.Children.Add(tb);
                    }
                }
                pConfronto.Children.Add(g);
                Registro.Scrivi("AZIONE", "importa tabella: confronto con " + cbCalc.SelectedItem);
            }
            catch (Exception ex) { pConfronto.Children.Add(T("Confronto non riuscito: " + ex.Message)); }
        }

        static void Imposta(SimpleCalc c, string k, double v) { foreach (var p in c.Params) if (p.Key == k) p.Value = v; }

        // ------------------------------------------------------------------------------------------ anteprima con ritaglio
        class Anteprima : Control
        {
            Bitmap bmp; PaginaLetta testo; double iw, ih;
            Point? da; Point a; Rect? sel;
            public event Action<double[]> Ritagliato;
            public event Action Tolto;
            public void Imposta(byte[] png, PaginaLetta pg)
            {
                bmp?.Dispose(); bmp = null; testo = pg; sel = null;
                if (png != null) { try { using (var ms = new MemoryStream(png)) bmp = new Bitmap(ms); iw = bmp.PixelSize.Width; ih = bmp.PixelSize.Height; } catch { bmp = null; } }
                else if (pg != null) { iw = pg.Larghezza; ih = pg.Altezza; }
                InvalidateVisual();
            }
            Rect Area()
            {
                double w = Bounds.Width, h = Bounds.Height;
                if (iw <= 0 || ih <= 0) return new Rect(0, 0, w, h);
                double s = Math.Min(w / iw, h / ih);
                return new Rect((w - iw * s) / 2, (h - ih * s) / 2, iw * s, ih * s);
            }
            protected override void OnPointerPressed(PointerPressedEventArgs e)
            {
                base.OnPointerPressed(e);
                if (e.ClickCount == 2) { sel = null; InvalidateVisual(); Tolto?.Invoke(); return; }
                if (bmp == null) return;
                da = e.GetPosition(this); a = da.Value; e.Pointer.Capture(this);
            }
            protected override void OnPointerMoved(PointerEventArgs e)
            {
                base.OnPointerMoved(e);
                if (da == null) return;
                a = e.GetPosition(this); sel = new Rect(da.Value, a).Normalize(); InvalidateVisual();
            }
            protected override void OnPointerReleased(PointerReleasedEventArgs e)
            {
                base.OnPointerReleased(e);
                if (da == null) return;
                da = null; e.Pointer.Capture(null);
                if (sel == null || sel.Value.Width < 8 || sel.Value.Height < 8) { sel = null; InvalidateVisual(); return; }
                var r = Area(); double s = iw / r.Width;
                Ritagliato?.Invoke(new[] { (sel.Value.X - r.X) * s, (sel.Value.Y - r.Y) * s, (sel.Value.Right - r.X) * s, (sel.Value.Bottom - r.Y) * s });
            }
            public override void Render(DrawingContext ctx)
            {
                ctx.FillRectangle(Brushes.White, new Rect(Bounds.Size));
                var r = Area();
                if (bmp != null) ctx.DrawImage(bmp, new Rect(0, 0, iw, ih), r);
                else if (testo != null)
                {
                    double s = r.Width / iw;
                    foreach (var p in testo.Parole)
                        ctx.DrawText(new FormattedText(p.Testo, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, Math.Max(4, p.H * s * 0.9), Brushes.Black), new Point(r.X + p.X * s, r.Y + p.Y * s));
                }
                else ctx.DrawText(new FormattedText("Anteprima della tabella", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 13, Brushes.Gray), new Point(10, 10));
                if (sel != null) ctx.DrawRectangle(new SolidColorBrush(Color.FromArgb(40, 31, 111, 235)), new Pen(new SolidColorBrush(Color.Parse("#1F6FEB")), 1.5), sel.Value);
            }
        }
    }
}
