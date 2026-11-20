// YA-NEC 1.5.3 - Variabili e ottimizzazione > Ridimensiona e ricentra (bozzetto «Ridimensiona e ricentra» approvato da IC8TEM il
// 20/11/2026 12:32-12:35, «così lo voglio»). Il calcolo e' in src/Engine/Ricentra.cs; Ridimensiona antenna esce da Calcolatori > Strumenti
// (li' resta un rimando a questa pagina).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        public const string VoceRicentra = "Ridimensiona e ricentra";
        RicentraEsito ricEsito;
        NecModel ricPer;
        Action ricSync;   // all'apertura della voce: frequenze del modello aperto (se non scritte a mano per questo modello)

        /// <summary>Apre Variabili e ottimizzazione > Ridimensiona e ricentra.</summary>
        public void ApriRicentra()
        {
            int ti = -1;
            for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == LinVarOtt) ti = i;
            if (ti < 0) return;
            if (sottoVarOtt != null) sottoVarOtt.Apri(VoceRicentra, false);
            SelectTab(ti);
        }

        static string ZRic(System.Numerics.Complex z)
        {
            CultureInfo cc = CultureInfo.CurrentCulture;
            return z.Real.ToString("0.0", cc) + (z.Imaginary >= 0 ? " +j" : " −j") + Math.Abs(z.Imaginary).ToString("0.0", cc) + " Ω";
        }

        /// <summary>Calcolatori > Strumenti > Ridimensiona antenna: rimando alla pagina nuova (1.5.3).</summary>
        Control BuildRimandoRicentra()
        {
            StackPanel b = new StackPanel { Spacing = 6 };
            b.Children.Add(Nota("Ridimensiona antenna è passato in Variabili e ottimizzazione > Ridimensiona e ricentra: lì fa la stessa cosa sul modello aperto (k = f vecchia / f nuova), con in più il ricentraggio dove l'antenna lavora meglio, i diametri commerciali e il ritocco del radiatore.", Pal.Fore));
            Button v = Btn("Apri Ridimensiona e ricentra", true);
            v.HorizontalAlignment = HorizontalAlignment.Left;
            v.Click += (s, e) => ApriRicentra();
            b.Children.Add(v);
            return Pannello("Ridimensiona antenna", b);
        }

        Control BuildRicentraPage()
        {
            StackPanel body = new StackPanel { Spacing = 4 };
            int modo = 0, diam = 1;
            TextBox tF1 = Tb(FN(model != null ? model.FreqMHz : 144.3, 3), 90), tF2 = Tb(FN(model != null ? model.FreqMHz : 144.3, 3), 90);
            DockPanel rF1 = Riga("Frequenza del modello", tF1, new TextBlock { Text = "MHz", VerticalAlignment = VerticalAlignment.Center });
            DockPanel rMeglio = Riga("Dove lavora meglio adesso", new TextBlock { Text = "ROS minimo su Z0, cercato con uno sweep stretto tra −3 % e +3 % della frequenza voluta", VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), MaxWidth = 640 });
            rF1.IsVisible = false;
            NecModel ricVisto = null;
            ricSync = () =>
            {
                if (model == null || model == ricVisto) return;
                ricVisto = model;
                tF1.Text = FN(model.FreqMHz, 3); tF2.Text = FN(model.FreqMHz, 3);
            };
            body.Children.Add(Riga("Cosa fare", Scelta(new[] { "ricentra: sposta dove lavora meglio adesso", "ridimensiona: da una frequenza all'altra" }, 0, k => { modo = k; rF1.IsVisible = k == 1; rMeglio.IsVisible = k == 0; })));
            body.Children.Add(rF1);
            body.Children.Add(Riga("Frequenza voluta", tF2, new TextBlock { Text = "MHz", VerticalAlignment = VerticalAlignment.Center }));
            body.Children.Add(rMeglio);

            // misure commerciali accese (config.ini)
            HashSet<double> accese = new HashSet<double>();
            string salv = cfg.Get("ricentra_misure", "");
            foreach (string s in salv.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) { double d; if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) accese.Add(d); }
            if (accese.Count == 0) { foreach (double d in Ricentra.Pieni) accese.Add(d); foreach (double d in Ricentra.Tubi) accese.Add(d); }
            Func<double[], WrapPanel> pillole = elenco =>
            {
                WrapPanel w = new WrapPanel { Orientation = Orientation.Horizontal };
                foreach (double d in elenco)
                {
                    double dd = d;
                    Button b = Btn(FN(d, 1), false);
                    b.BorderThickness = new Thickness(1);
                    b.BorderBrush = Pal.B(Pal.PillBorder);
                    b.Margin = new Thickness(0, 0, 3, 3);
                    StileScelta(b, accese.Contains(d));
                    b.Click += (s, e) =>
                    {
                        if (!accese.Remove(dd)) accese.Add(dd);
                        StileScelta(b, accese.Contains(dd));
                        cfg.Set("ricentra_misure", string.Join(" ", accese.OrderBy(x => x).Select(x => x.ToString(CultureInfo.InvariantCulture))));
                    };
                    w.Children.Add(b);
                }
                return w;
            };
            StackPanel comm = new StackPanel { Spacing = 2, IsEnabled = false, Opacity = 0.45 };
            comm.Children.Add(Riga("Tondini pieni (mm)", pillole(Ricentra.Pieni)));
            comm.Children.Add(Riga("Tondini o tubi, da 8 mm (mm)", pillole(Ricentra.Tubi)));
            comm.Children.Add(Riga("", new TextBlock
            {
                Text = "Ogni elemento prende la misura accesa più vicina; la lunghezza si corregge perché l'elemento resti accordato come prima (stessa reattanza, modello a linee di Schelkunoff, lo stesso della correzione di Leeson). Per l'RF un tubo e un tondino dello stesso diametro sono uguali.",
                TextWrapping = TextWrapping.Wrap, MaxWidth = 640, FontSize = 12.5, Foreground = Pal.B(Pal.Muted)
            }));
            body.Children.Add(Riga("Diametri", Scelta(new[] { "in scala", "uguali", "misure commerciali" }, 1, k => { diam = k; comm.IsEnabled = k == 2; comm.Opacity = k == 2 ? 1 : 0.45; })));
            body.Children.Add(comm);
            CheckBox chRit = new CheckBox { Content = "alla fine porta a Z0 +j0 con la sola lunghezza del radiatore", IsChecked = true, Margin = new Thickness(0, 2) };
            body.Children.Add(chRit);

            Button bCerca = Btn("Cerca", false), bApp = Btn("Applica e ricalcola", true);
            bApp.IsEnabled = false;
            TextBlock stato = new TextBlock { Text = "Premi Cerca: il modello aperto non cambia finché non premi Applica.", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, MaxWidth = 560 };
            StackPanel bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 6, 0, 0) };
            bar.Children.Add(bCerca); bar.Children.Add(bApp); bar.Children.Add(stato);
            body.Children.Add(bar);
            StackPanel esito = new StackPanel { Spacing = 4, Margin = new Thickness(0, 8, 0, 0) };
            body.Children.Add(esito);

            CancellationTokenSource ricCts = null;
            bCerca.Click += async (s, e) =>
            {
                if (model == null) { Status("Apri prima un modello: lo strumento lavora sul modello aperto.", true); return; }
                double f2 = ParseNum(tF2.Text, double.NaN), f1 = ParseNum(tF1.Text, double.NaN);
                if (double.IsNaN(f2) || f2 <= 0 || (modo == 1 && (double.IsNaN(f1) || f1 <= 0))) { Status("Frequenza non valida.", true); return; }
                List<double> mis = accese.OrderBy(x => x).ToList();
                if (diam == 2 && mis.Count == 0) { Status("Accendi almeno una misura commerciale.", true); return; }
                if (ricCts != null) ricCts.Cancel();
                ricCts = new CancellationTokenSource();
                CancellationToken tok = ricCts.Token;
                NecModel m = model;
                int md = modo, dm = diam;
                bool rit = chRit.IsChecked == true;
                bApp.IsEnabled = false;
                bCerca.IsEnabled = false;
                esito.Children.Clear();
                stato.Text = md == 0 ? "Sweep stretto attorno a " + FN(f2, 3) + " MHz..." : "Calcolo...";
                // avviso: segmentazione grossa (con 40 per lambda la 11el I4GBZ non era convergente)
                double segLam = double.NaN;
                try
                {
                    SegmentGeometry g = m.BuildGeometry();
                    double maxSeg = 0;
                    for (int i = 0; i < g.N; i++) maxSeg = Math.Max(maxSeg, g.Length(i));
                    if (maxSeg > 0) segLam = 299.792458 / f2 / maxSeg;
                }
                catch { }
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    RicentraEsito r = await Task.Run(() => Ricentra.Calcola(m, md, f1, f2, dm, mis, rit, m.Z0, tok));
                    if (tok.IsCancellationRequested || m != model) return;
                    ricEsito = r; ricPer = m;
                    CultureInfo cc = CultureInfo.CurrentCulture;
                    stato.Text = "Fatto in " + sw.Elapsed.TotalSeconds.ToString("0.0", cc) + " s; il modello aperto non è ancora cambiato.";
                    string testa = md == 0
                        ? "ROS minimo adesso a " + FN(r.FMigliore, 3) + " MHz (ROS " + r.RosMin.ToString("0.00", cc) + ") → fattore " + r.Fattore.ToString("0.0000", cc) + " (" + FN(r.FMigliore, 3) + "/" + FN(f2, 3) + ")"
                        : "Fattore " + r.Fattore.ToString("0.0000", cc) + " (" + FN(f1, 3) + "/" + FN(f2, 3) + ")";
                    if (Math.Abs(r.RitoccoMm) > 0.005) testa += "; radiatore ritoccato di " + r.RitoccoMm.ToString("+0.0;−0.0", cc) + " mm";
                    esito.Children.Add(new TextBlock { Text = testa + ".", FontWeight = FontWeight.SemiBold, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap });
                    if (md == 0 && Math.Abs(r.FMigliore - f2 * 0.97) < f2 * 0.001 || md == 0 && Math.Abs(r.FMigliore - f2 * 1.03) < f2 * 0.001)
                        esito.Children.Add(Nota("Il ROS minimo è sul bordo della ricerca (±3 %): l'antenna è lontana dalla frequenza voluta; usa «ridimensiona» con la frequenza del modello.", Pal.Orange));
                    if (r.Righe.Count > 0)
                    {
                        Grid tb = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto,Auto") };
                        string[] tit = { "Elemento", "Ø ora", "Ø dopo", "Lunghezza ora", "dopo il diametro", "dopo la scala" };
                        int riga = 0;
                        Action<string[], bool> add = (v, h) =>
                        {
                            tb.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                            for (int c = 0; c < v.Length; c++)
                            {
                                TextBlock t = new TextBlock { Text = v[c], Margin = new Thickness(0, 1, 18, 1), Foreground = Pal.B(h ? Pal.Muted : Pal.Fore), FontWeight = h ? FontWeight.SemiBold : FontWeight.Normal, HorizontalAlignment = c == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right };
                                Grid.SetRow(t, riga); Grid.SetColumn(t, c);
                                tb.Children.Add(t);
                            }
                            riga++;
                        };
                        add(tit, true);
                        foreach (RicentraRiga q in r.Righe)
                            add(new[] { q.Nome, FN(q.DPrima, 2) + " mm", FN(q.DDopo, 2) + " mm", FN(q.LPrima, 1) + " mm", FN(q.LDiametro, 1) + " mm", FN(q.LScala, 1) + " mm" }, false);
                        esito.Children.Add(new ScrollViewer { Content = tb, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
                    }
                    StackPanel fin = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                    fin.Children.Add(new TextBlock { Text = "A " + FN(f2, 3) + " MHz: prima " + ZRic(r.ZPrima) + " (ROS " + r.RosPrima.ToString("0.00", cc) + ") → dopo ", Foreground = Pal.B(Pal.Fore) });
                    fin.Children.Add(new TextBlock { Text = ZRic(r.ZDopo) + " (ROS " + r.RosDopo.ToString("0.00", cc) + ")", FontWeight = FontWeight.Bold, Foreground = Pal.B(r.RosDopo <= 1.5 ? Pal.Green : Pal.Orange) });
                    fin.Children.Add(new TextBlock { Text = " · " + r.GDopo.ToString("0.00", cc) + " dBi · F/B " + r.FbDopo.ToString("0.0", cc) + " dB", Foreground = Pal.B(Pal.Fore) });
                    esito.Children.Add(fin);
                    foreach (string n in r.Note) esito.Children.Add(Nota(n, Pal.Orange));
                    if (!double.IsNaN(segLam) && segLam < 100)
                        esito.Children.Add(Nota("Segmenti lunghi: circa " + segLam.ToString("0", cc) + " per λ sul segmento più lungo. Sulle antenne a banda stretta (Yagi lunghe) con meno di 100 per λ il risultato può non essere convergente: alza i segmenti per λ e ripeti il Cerca.", Pal.Orange));
                    bApp.IsEnabled = true;
                }
                catch (OperationCanceledException) { stato.Text = "Interrotto."; }
                catch (Exception ex) { stato.Text = "Errore: " + ex.GetBaseException().Message; Status("Ridimensiona e ricentra: " + ex.GetBaseException().Message, true); }
                finally { bCerca.IsEnabled = true; }
            };
            bApp.Click += (s, e) =>
            {
                if (ricEsito == null || ricEsito.Modello == null || ricPer != model) { Status("Il modello aperto è cambiato: premi di nuovo Cerca.", true); bApp.IsEnabled = false; return; }
                NecModel nm = ricEsito.Modello;
                Registro.Scrivi("AZIONE", "ridimensiona e ricentra: fattore " + ricEsito.Fattore.ToString("0.00000", CultureInfo.InvariantCulture) + ", Z " + ZRic(ricEsito.ZDopo));
                SetModel(nm, null);
                bApp.IsEnabled = false;
                stato.Text = "Applicato: " + nm.FileName + ". Il file su disco non è cambiato: salvalo se vuoi tenerlo.";
                Status("Modello ridimensionato/ricentrato applicato (fattore " + ricEsito.Fattore.ToString("0.0000", CultureInfo.CurrentCulture) + "). Il file su disco non è cambiato.");
                ricEsito = null;
                Calculate(true);
            };
            body.Children.Add(Nota("Geometria > Scala resta lo strumento generico (fattore scritto a mano). Qui il modello aperto si copia, si scala attorno al punto più basso (l'altezza dal suolo resta) e si ricalcola; carichi L e C non si scalano."));
            return new ScrollViewer { Content = Pannello("Ridimensiona e ricentra il modello aperto", body) };
        }
    }
}
