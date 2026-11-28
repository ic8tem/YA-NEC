// YA-NEC 1.8.0 - Calcolatori > Strumenti > Leggi progetto (PDF o immagine): bozzetto approvato da IC8TEM il 28/11/2026 00:55.
// A sinistra il disegno del costruttore (o il testo della pagina del PDF) con i numeri della riga scelta evidenziati, a destra la
// tabella letta, da correggere a mano: righe in giallo = letto male o mancante. Sotto: controlli, confronto con il modello aperto
// e "Crea il modello NEC". Motore: Engine/LetturaProgetto.cs.
#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using YaNec.Engine;
using P = YaNec.ParabolaPanel;

namespace YaNec
{
    public class LetturaProgettoPanel : UserControl
    {
        /// <summary>Impostato da MainWindow: apre il modello NEC creato (testo, nome).</summary>
        public Action<string, string> ApriNec;
        /// <summary>Impostato da MainWindow: geometria del modello aperto, per il confronto.</summary>
        public Func<SegmentGeometry> Geometria;

        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        string percorso, nomeFile;
        List<PaginaLetta> pagine = new List<PaginaLetta>();
        readonly Dictionary<PaginaLetta, PaginaLetta> ocrFatti = new Dictionary<PaginaLetta, PaginaLetta>();
        PaginaLetta pagina;     // pagina analizzata (testo o OCR)
        readonly List<Riga> righe = new List<Riga>();

        ComboBox cbPagine, cbUnita;
        TextBox tbFreq;
        TextBlock tFile, tStato;
        Vista vista;
        StackPanel corpoTabella, pannelloSotto;
        bool caricamento;

        class Riga
        {
            public ElementoLetto E;
            public TextBox Nome, Pos, Tratti; public CheckBox Alim; public Grid G;
        }

        public LetturaProgettoPanel()
        {
            Content = Costruisci();
            // per le prove: YANEC_LEGGI_PROGETTO=<file> apre il file all'apertura della pagina
            string prova = Environment.GetEnvironmentVariable("YANEC_LEGGI_PROGETTO");
            if (!string.IsNullOrEmpty(prova) && File.Exists(prova)) Dispatcher.UIThread.Post(async () => await Carica(prova));
        }

        Control Costruisci()
        {
            var root = new DockPanel { Background = new SolidColorBrush(P.CSfondo), Margin = new Thickness(0) };
            var testa = new StackPanel { Spacing = 6, Margin = new Thickness(8, 8, 8, 4) };
            testa.Children.Add(new TextBlock { Text = "Leggi progetto (PDF o immagine)", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(P.CFam) });
            var barra = new WrapPanel { Orientation = Orientation.Horizontal };
            var bApri = new Button { Content = "Apri PDF o immagine…", Margin = new Thickness(0, 0, 8, 4) };
            bApri.Click += async (s, e) => await Apri();
            barra.Children.Add(bApri);
            tFile = new TextBlock { Text = "nessun file", VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(P.CGrigio), Margin = new Thickness(0, 0, 12, 4), MaxWidth = 320, TextTrimming = TextTrimming.CharacterEllipsis };
            barra.Children.Add(tFile);
            cbPagine = new ComboBox { MinWidth = 260, Margin = new Thickness(0, 0, 12, 4) };
            ToolTip.SetTip(cbPagine, "Pagine con le misure: testo del PDF (numeri esatti) oppure immagine (disegno del costruttore, letta con l'OCR).");
            cbPagine.SelectionChanged += async (s, e) => { if (!caricamento) await Analizza(); };
            barra.Children.Add(cbPagine);
            barra.Children.Add(new TextBlock { Text = "unità", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 4) });
            cbUnita = new ComboBox { ItemsSource = LetturaProgetto.Unita, SelectedIndex = 0, Margin = new Thickness(0, 0, 12, 4) };
            cbUnita.SelectionChanged += (s, e) => { if (!caricamento && pagina != null) Riempi(LetturaProgetto.Analizza(pagina, cbUnita.SelectedIndex)); };
            barra.Children.Add(cbUnita);
            barra.Children.Add(new TextBlock { Text = "frequenza più alta", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 4) });
            tbFreq = new TextBox { Text = "30", Width = 70, Margin = new Thickness(0, 0, 4, 4) };
            ToolTip.SetTip(tbFreq, "Per i segmenti del modello (circa lambda/40). Con una multibanda: la banda più alta.");
            barra.Children.Add(tbFreq);
            barra.Children.Add(new TextBlock { Text = "MHz", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 4) });
            testa.Children.Add(barra);
            tStato = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(P.CGrigio), FontSize = 12.5,
                Text = "Apri il manuale o il disegno della direttiva: YA-NEC legge le posizioni sul boom e i tubi (diametro × lunghezza) di ogni elemento. Il file resta tuo: YA-NEC non lo copia." };
            testa.Children.Add(tStato);
            DockPanel.SetDock(testa, Dock.Top);
            root.Children.Add(testa);

            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,1.25*"), Margin = new Thickness(8, 4, 8, 8) };
            vista = new Vista();
            var sv = new ScrollViewer { Content = vista, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
            var bVista = new Border { Background = Pal.B(Pal.Panel), BorderBrush = new SolidColorBrush(P.CBordo), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(6), Margin = new Thickness(0, 0, 8, 0), Child = sv };
            g.Children.Add(bVista);

            var dx = new StackPanel { Spacing = 6 };
            var intest = RigaGriglia();
            intest.Children.Add(Testo("Elemento", 0)); intest.Children.Add(Testo("Sul boom", 1)); intest.Children.Add(Testo("Tratti dal centro alla punta: Ø × L", 2)); intest.Children.Add(Testo("Alim.", 3));
            corpoTabella = new StackPanel { Spacing = 2 };
            var tab = new StackPanel { Spacing = 2 }; tab.Children.Add(intest); tab.Children.Add(corpoTabella);
            dx.Children.Add(P.Carta(tab, "Tabella letta"));
            var bott = new WrapPanel { Orientation = Orientation.Horizontal };
            var bAgg = new Button { Content = "Aggiungi elemento", Margin = new Thickness(0, 0, 8, 4) };
            bAgg.Click += (s, e) => { AggiungiRiga(new ElementoLetto { Nome = "elemento " + (righe.Count + 1) }); };
            var bConf = new Button { Content = "Confronta con il modello aperto", Margin = new Thickness(0, 0, 8, 4) };
            bConf.Click += (s, e) => Confronta();
            var bCrea = new Button { Content = "Crea il modello NEC", Margin = new Thickness(0, 0, 8, 4), Background = new SolidColorBrush(P.CBlu), Foreground = Brushes.White };
            bCrea.Click += (s, e) => Crea();
            bott.Children.Add(bAgg); bott.Children.Add(bConf); bott.Children.Add(bCrea);
            dx.Children.Add(bott);
            pannelloSotto = new StackPanel { Spacing = 6 };
            dx.Children.Add(pannelloSotto);
            dx.Children.Add(P.Nota("Giallo = letto male o mancante: si corregge a mano. Controlli: posizioni crescenti lungo il boom, diametri che calano verso la punta. "
                + "Le lunghezze sono i tratti visibili di ogni tubo (senza la parte infilata nel tubo più grosso), come nei disegni dei costruttori. "
                + "OCR: Tesseract (Apache 2.0); PDF: PdfPig (Apache 2.0)."));
            var svDx = new ScrollViewer { Content = dx };
            Grid.SetColumn(svDx, 1);
            g.Children.Add(svDx);
            root.Children.Add(g);
            return root;
        }

        static Grid RigaGriglia() { return new Grid { ColumnDefinitions = new ColumnDefinitions("150,80,*,44") }; }
        static TextBlock Testo(string t, int col)
        {
            var tb = new TextBlock { Text = t, FontSize = 12.5, Foreground = new SolidColorBrush(P.CGrigio), Margin = new Thickness(2, 0, 4, 2), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(tb, col); return tb;
        }

        // ------------------------------------------------------------------------------------------ file
        async Task Apri()
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top == null) return;
                var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Progetto della direttiva: PDF o immagine",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("PDF e immagini") { Patterns = new[] { "*.pdf", "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.tif", "*.tiff", "*.webp" } } }
                });
                if (files == null || files.Count == 0) return;
                string p = files[0].TryGetLocalPath();
                if (p == null) { tStato.Text = "File non raggiungibile."; return; }
                await Carica(p);
            }
            catch (Exception ex) { tStato.Text = "Errore: " + ex.Message; }
        }

        /// <summary>Carica un file (anche dal collaudo): PDF o immagine.</summary>
        public async Task Carica(string p)
        {
            percorso = p; nomeFile = Path.GetFileName(p);
            tFile.Text = nomeFile;
            tStato.Text = "Lettura di " + nomeFile + "…";
            Registro.Scrivi("AZIONE", "leggi progetto: " + nomeFile);
            List<PaginaLetta> l; string err = null;
            try
            {
                l = await Task.Run(() =>
                {
                    if (p.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return LetturaProgetto.LeggiPdf(p);
                    return new List<PaginaLetta> { new PaginaLetta { Pagina = 0, DaOcr = true, Png = File.ReadAllBytes(p), Descrizione = "immagine " + Path.GetFileName(p) } };
                });
            }
            catch (Exception ex) { l = new List<PaginaLetta>(); err = ex.Message; }
            pagine = l; ocrFatti.Clear();
            caricamento = true;
            cbPagine.ItemsSource = pagine.Select(q => q.Descrizione).ToList();
            // pagina proposta: l'immagine grande con piu' area, altrimenti il testo con piu' misure in pollici
            int sc = LetturaProgetto.Proposta(pagine);
            cbPagine.SelectedIndex = sc;
            caricamento = false;
            if (err != null) { tStato.Text = "File non leggibile: " + err; return; }
            if (pagine.Count == 0) { tStato.Text = "Nel file non ci sono pagine con misure né immagini grandi."; return; }
            await Analizza();
        }

        async Task Analizza()
        {
            int i = cbPagine.SelectedIndex;
            if (i < 0 || i >= pagine.Count) return;
            var pg = pagine[i];
            if (pg.DaOcr)
            {
                PaginaLetta letta;
                if (!ocrFatti.TryGetValue(pg, out letta))
                {
                    tStato.Text = "OCR di " + pg.Descrizione + " in corso…";
                    string err = null;
                    letta = await Task.Run(() => LetturaProgetto.Ocr(pg.Png, pg.Descrizione, out err));
                    if (letta == null) { tStato.Text = err; vista.Imposta(pg, null); Riempi(new List<ElementoLetto>()); return; }
                    ocrFatti[pg] = letta;
                }
                pagina = letta;
            }
            else pagina = pg;
            caricamento = true;
            cbUnita.SelectedIndex = LetturaProgetto.UnitaProbabile(pagina);
            caricamento = false;
            double f = LetturaProgetto.FrequenzaDalTesto(pagine.SelectMany(q => q.Parole.Select(w => w.Testo)).Concat(pagina.Parole.Select(w => w.Testo)));
            if (f > 0) tbFreq.Text = f.ToString("0.##", CI);
            vista.Imposta(pagina, null);
            var el = LetturaProgetto.Analizza(pagina, cbUnita.SelectedIndex);
            Riempi(el);
            int dubbi = el.Count(e => e.Dubbi.Count > 0);
            tStato.Text = (pagina.DaOcr ? "Letto con l'OCR: " : "Letto dal testo del PDF: ") + el.Count + " elementi" + (dubbi > 0 ? ", " + dubbi + " da controllare (in giallo)" : "") + ". Clic su una riga: i suoi numeri si accendono nel disegno.";
            Registro.Scrivi("AZIONE", "leggi progetto: " + pagina.Descrizione + ", " + el.Count + " elementi, " + dubbi + " da controllare");
        }

        // ------------------------------------------------------------------------------------------ tabella
        void Riempi(List<ElementoLetto> el)
        {
            corpoTabella.Children.Clear(); righe.Clear(); pannelloSotto.Children.Clear();
            foreach (var e in el) AggiungiRiga(e);
        }

        void AggiungiRiga(ElementoLetto e)
        {
            var r = new Riga { E = e, G = RigaGriglia() };
            r.Nome = new TextBox { Text = e.Nome, FontSize = 12.5, Margin = new Thickness(0, 0, 4, 0) };
            r.Pos = new TextBox { Text = double.IsNaN(e.Pos) ? "" : LetturaProgetto.Fmt(e.Pos), FontSize = 12.5, Margin = new Thickness(0, 0, 4, 0) };
            r.Tratti = new TextBox { Text = LetturaProgetto.TestoSpezzoni(e.Spezzoni), FontSize = 12.5, Margin = new Thickness(0, 0, 4, 0) };
            r.Alim = new CheckBox { IsChecked = e.Alimentato, HorizontalAlignment = HorizontalAlignment.Center };
            ToolTip.SetTip(r.Alim, "Elemento alimentato: sorgente al centro.");
            Grid.SetColumn(r.Pos, 1); Grid.SetColumn(r.Tratti, 2); Grid.SetColumn(r.Alim, 3);
            r.G.Children.Add(r.Nome); r.G.Children.Add(r.Pos); r.G.Children.Add(r.Tratti); r.G.Children.Add(r.Alim);
            Colora(r);
            foreach (var c in new Control[] { r.Nome, r.Pos, r.Tratti }) c.GotFocus += (s, a) => vista.Imposta(pagina, e.Fonti);
            r.Pos.LostFocus += (s, a) => { e.DubbioPos = false; e.Dubbi.RemoveAll(d => d.StartsWith("posizione")); Colora(r); };
            r.Tratti.LostFocus += (s, a) =>
            {
                List<SpezzoneLetto> sp; string err;
                e.Dubbi.Clear();
                if (LetturaProgetto.LeggiSpezzoni(r.Tratti.Text, out sp, out err)) { e.Spezzoni = sp; LetturaProgetto.Controlla(e); } else e.Dubbi.Add(err);
                Colora(r);
            };
            righe.Add(r);
            corpoTabella.Children.Add(r.G);
        }

        void Colora(Riga r)
        {
            var giallo = new SolidColorBrush(Color.Parse(Pal.Dark ? "#5A4A12" : "#FFF1B8"));
            bool dP = r.E.DubbioPos, dT = r.E.Dubbi.Any(d => !d.StartsWith("posizione"));
            r.Pos.Background = dP ? giallo : null;
            r.Tratti.Background = dT ? giallo : null;
            ToolTip.SetTip(r.Tratti, r.E.Dubbi.Count > 0 ? string.Join("\n", r.E.Dubbi) : "Tratti dal centro alla punta (meta' elemento), diametro×lunghezza, separati da · o ;");
        }

        /// <summary>Tabella come elementi (dopo le correzioni a mano); errore = prima riga sbagliata.</summary>
        List<ElementoLetto> Leggi(out string errore)
        {
            errore = null;
            var l = new List<ElementoLetto>();
            foreach (var r in righe)
            {
                double pos;
                if (!double.TryParse((r.Pos.Text ?? "").Replace(',', '.').Trim().TrimEnd('"'), NumberStyles.Float, CI, out pos)) { errore = r.Nome.Text + ": posizione non valida"; return null; }
                List<SpezzoneLetto> sp; string err;
                if (!LetturaProgetto.LeggiSpezzoni(r.Tratti.Text, out sp, out err)) { errore = r.Nome.Text + ": " + err; return null; }
                l.Add(new ElementoLetto { Nome = r.Nome.Text, Pos = pos, Spezzoni = sp, Alimentato = r.Alim.IsChecked == true });
            }
            if (l.Count == 0) errore = "Tabella vuota: apri un file o aggiungi gli elementi a mano.";
            return l;
        }

        void Confronta()
        {
            pannelloSotto.Children.Clear();
            string err; var l = Leggi(out err);
            if (l == null || err != null) { pannelloSotto.Children.Add(P.Avviso(2, err)); return; }
            var g = Geometria?.Invoke();
            var conf = LetturaProgetto.Confronta(l, cbUnita.SelectedIndex, g);
            if (conf.Count == 0) { pannelloSotto.Children.Add(P.Avviso(1, "Nessun modello aperto da confrontare (o modello senza elementi diritti).")); return; }
            var t = new List<string[]> { new[] { "elemento", "posizione", "lunghezza", "" } };
            t.AddRange(conf);
            int uguali = conf.Count(c => c[3] == "uguale");
            pannelloSotto.Children.Add(P.Carta(P.Tabella(t, "Auto,Auto,Auto,*"), "Confronto con il modello aperto: " + uguali + " su " + conf.Count + " elementi uguali (entro 5 mm di posizione e 10 mm di lunghezza)"));
        }

        void Crea()
        {
            pannelloSotto.Children.Clear();
            string err; var l = Leggi(out err);
            if (l == null || err != null) { pannelloSotto.Children.Add(P.Avviso(2, err)); return; }
            double f;
            if (!double.TryParse((tbFreq.Text ?? "").Replace(',', '.'), NumberStyles.Float, CI, out f) || f <= 0) { pannelloSotto.Children.Add(P.Avviso(2, "Frequenza non valida.")); return; }
            string nome = Path.GetFileNameWithoutExtension(nomeFile ?? "progetto") + " (letto)";
            string nec = LetturaProgetto.CreaNec(l, cbUnita.SelectedIndex, f, nome, (nomeFile ?? "") + (pagina != null ? ", " + pagina.Descrizione : ""));
            if (!l.Any(e => e.Alimentato)) pannelloSotto.Children.Add(P.Avviso(1, "Nessun elemento alimentato: il modello si apre senza sorgente."));
            Registro.Scrivi("AZIONE", "leggi progetto: modello creato con " + l.Count + " elementi");
            ApriNec?.Invoke(nec, nome + ".nec");
        }

        // ------------------------------------------------------------------------------------------ disegno
        class Vista : Control
        {
            PaginaLetta pg; Bitmap bmp; List<ParolaLetta> evid;
            public void Imposta(PaginaLetta p, List<ParolaLetta> ev)
            {
                if (p != pg)
                {
                    bmp?.Dispose(); bmp = null;
                    if (p != null && p.Png != null) { try { using (var ms = new MemoryStream(p.Png)) bmp = new Bitmap(ms); } catch { bmp = null; } }
                }
                pg = p; evid = ev;
                InvalidateMeasure(); InvalidateVisual();
            }
            protected override Size MeasureOverride(Size a)
            {
                double w = double.IsInfinity(a.Width) ? 500 : a.Width;
                if (pg == null || pg.Larghezza <= 0) return new Size(w, 300);
                return new Size(w, w * pg.Altezza / pg.Larghezza);
            }
            public override void Render(DrawingContext ctx)
            {
                double w = Bounds.Width, h = Bounds.Height;
                ctx.FillRectangle(Brushes.White, new Rect(0, 0, w, h));
                if (pg == null)
                {
                    var ft0 = new FormattedText("Il disegno del costruttore compare qui.", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 13, Brushes.Gray);
                    ctx.DrawText(ft0, new Point(12, 12)); return;
                }
                double s = w / pg.Larghezza;
                if (bmp != null) ctx.DrawImage(bmp, new Rect(0, 0, bmp.Size.Width, bmp.Size.Height), new Rect(0, 0, w, h));
                else
                    foreach (var p in pg.Parole)
                    {
                        var ft = new FormattedText(p.Testo, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, Math.Max(6, p.H * s * 0.9), Brushes.Black);
                        ctx.DrawText(ft, new Point(p.X * s, p.Y * s));
                    }
                if (evid != null)
                {
                    var pen = new Pen(new SolidColorBrush(Color.Parse("#1F6FEB")), 1.5);
                    var fondo = new SolidColorBrush(Color.FromArgb(60, 31, 111, 235));
                    foreach (var p in evid) ctx.DrawRectangle(fondo, pen, new Rect(p.X * s - 2, p.Y * s - 2, p.W * s + 4, p.H * s + 4), 3, 3);
                }
            }
        }
    }
}
