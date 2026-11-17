// YA-NEC - fascione di sinistra (stile approvato "alla lettera", doc/Stile_fascione_sinistro.md):
// larghezza 200 px, voci a pillola (raggio 14, bordo 1 #D3DBE7, margine interno 10/6, 5 px sotto, carattere 15),
// normale bianco con testo #1D5FD1, selezionata #1D6FE0 con testo bianco grassetto, futura testo #A3ACB9 non cliccabile,
// titolo di famiglia #425066 grassetto con margine interno 2,6,2,2. Nessun cambio al passaggio del mouse, solo la manina.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace YaNec
{
    public class Fascione : StackPanel
    {
        public static readonly Color VoceSfondo = Color.Parse("#FFFFFF");
        public static readonly Color VoceBordo = Color.Parse("#D3DBE7");
        public static readonly Color VoceTesto = Color.Parse("#1D5FD1");
        public static readonly Color SelezSfondo = Color.Parse("#1D6FE0");
        public static readonly Color SelezTesto = Color.Parse("#FFFFFF");
        public static readonly Color FuturaTesto = Color.Parse("#A3ACB9");
        public static readonly Color FamigliaTesto = Color.Parse("#425066");
        public static readonly Color SfondoPagina = Color.Parse("#EEF2F7");
        public const double Larghezza = 200, Raggio = 14, Bordo = 1, MargineOrizz = 10, MargineVert = 6, SpazioSotto = 5, Carattere = 15;

        public event Action<string> Selected;
        readonly Dictionary<string, Border> voci = new Dictionary<string, Border>();
        readonly HashSet<string> future = new HashSet<string>();
        public string Current = "";

        public Fascione()
        {
            Width = Larghezza;
            Orientation = Orientation.Vertical;
        }

        public void Famiglia(string titolo)
        {
            Children.Add(new TextBlock
            {
                Text = titolo, FontSize = 15, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(FamigliaTesto),
                Padding = new Thickness(2, 6, 2, 2), Margin = new Thickness(0, 0, 0, 5), TextWrapping = TextWrapping.Wrap
            });
        }

        readonly Dictionary<string, TextBlock> titoli = new Dictionary<string, TextBlock>(), sotto = new Dictionary<string, TextBlock>();

        public Border Voce(string key, string testo, bool futura = false, string fumetto = null, string sottotitolo = null, bool freccia = false)
        {
            TextBlock t = new TextBlock { Text = testo, FontSize = 15, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(futura ? FuturaTesto : VoceTesto) };
            Control child = t;
            titoli[key] = t;
            if (!string.IsNullOrEmpty(sottotitolo) || freccia)
            {
                // voce con descrizione (bozzetto approvato dei calcolatori delle forme semplici): titolo, descrizione grigia, freccia
                DockPanel dp = new DockPanel();
                if (freccia)
                {
                    TextBlock fr = new TextBlock { Text = "›", FontSize = 18, Foreground = new SolidColorBrush(FuturaTesto), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
                    DockPanel.SetDock(fr, Dock.Right);
                    dp.Children.Add(fr);
                }
                StackPanel st = new StackPanel();
                t.FontWeight = FontWeight.SemiBold;
                st.Children.Add(t);
                if (!string.IsNullOrEmpty(sottotitolo))
                {
                    TextBlock su = new TextBlock { Text = sottotitolo, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(FuturaTesto) };
                    sotto[key] = su;
                    st.Children.Add(su);
                }
                dp.Children.Add(st);
                child = dp;
            }
            Border b = new Border
            {
                Child = child, Background = new SolidColorBrush(VoceSfondo), BorderBrush = new SolidColorBrush(VoceBordo),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(10, 6),
                Margin = new Thickness(0, 0, 0, 5), HorizontalAlignment = HorizontalAlignment.Stretch, Tag = key
            };
            if (!futura)
            {
                b.Cursor = new Cursor(StandardCursorType.Hand);
                b.PointerPressed += (s, e) => Select(key);
            }
            else future.Add(key);
            if (!string.IsNullOrEmpty(fumetto)) ToolTip.SetTip(b, fumetto);
            else if (futura) ToolTip.SetTip(b, "Prevista (bozzetto approvato), non ancora attiva in questa versione.");
            voci[key] = b;
            Children.Add(b);
            return b;
        }

        public bool IsFuture(string key) { return future.Contains(key); }

        /// <summary>Chiavi delle voci attive, nell'ordine in cui sono nel fascione (per il collaudo di tutte le sottolinguette).</summary>
        public List<string> Chiavi()
        {
            List<string> l = new List<string>();
            foreach (Control c in Children)
                if (c is Border b && b.Tag is string k && voci.ContainsKey(k) && !future.Contains(k)) l.Add(k);
            return l;
        }

        /// <summary>Testo della voce (titolo).</summary>
        public string Testo(string key) { TextBlock t; return titoli.TryGetValue(key, out t) ? t.Text : key; }

        public void SetCount(string key, string testo)
        {
            TextBlock t;
            if (titoli.TryGetValue(key, out t)) t.Text = testo;
        }

        /// <summary>1.2.0 (registro di IC8TEM, evento 1026 del 13/11/2026 17:58, AvaloniaInternalException "Visual is attached to visual tree
        /// but root could not be found"): prima di cambiare pagina si toglie il fuoco dal campo in cui si sta scrivendo, mentre la pagina e'
        /// ancora nella finestra. Cosi' il ricalcolo legato all'uscita dal campo avviene prima del cambio e non durante lo smontaggio della
        /// pagina, quando aggiungere elementi a un pannello che sta uscendo dalla finestra chiudeva il programma.</summary>
        public static void TogliFuoco(Visual v)
        {
            try
            {
                TopLevel tl = TopLevel.GetTopLevel(v);
                if (tl != null && tl.FocusManager != null && tl.FocusManager.GetFocusedElement() is TextBox) tl.FocusManager.ClearFocus();
            }
            catch { }
        }

        public void Select(string key, bool notify = true)
        {
            if (!voci.ContainsKey(key) || future.Contains(key)) return;
            if (notify) TogliFuoco(this);
            Current = key;
            foreach (KeyValuePair<string, Border> kv in voci)
            {
                bool on = kv.Key == key;
                bool fut = future.Contains(kv.Key);
                kv.Value.Background = new SolidColorBrush(on ? SelezSfondo : VoceSfondo);
                kv.Value.BorderBrush = new SolidColorBrush(VoceBordo);
                TextBlock t, su;
                if (titoli.TryGetValue(kv.Key, out t))
                {
                    bool desc = sotto.ContainsKey(kv.Key) || !(kv.Value.Child is TextBlock);
                    t.Foreground = new SolidColorBrush(on ? SelezTesto : (fut ? FuturaTesto : VoceTesto));
                    t.FontWeight = on ? FontWeight.Bold : (desc ? FontWeight.SemiBold : FontWeight.Normal);
                }
                if (sotto.TryGetValue(kv.Key, out su)) su.Foreground = new SolidColorBrush(on ? Color.Parse("#DCE8FB") : FuturaTesto);
            }
            if (notify && Selected != null) Selected(key);
        }

        // pagina con il fascione a sinistra (10 px dal bordo, 10 px dal pannello centrale) e il contenuto a destra
        // pagina con il fascione largo una percentuale della finestra (IC8TEM: fino al 30 % per le voci con descrizione)
        // 0.5.3 (IC8TEM 15/11/2026 19:58: "puoi tentare di farmi spostare questa separazione? io avevo chiesto finestra fissa, ma sembra che
        // mobile sia meglio"): tra l'elenco e il contenuto c'e' un separatore che si trascina con il mouse; la larghezza scelta resta finche'
        // il programma e' aperto (per ogni pagina). Doppio clic sul separatore: torna alla larghezza iniziale.
        static readonly System.Collections.Generic.Dictionary<string, double> larghezze = new System.Collections.Generic.Dictionary<string, double>();

        public static Grid PaginaLarga(Fascione f, ContentControl contenuto, double percento, string chiave = null)
        {
            string k = chiave ?? percento.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Grid g = new Grid { Background = new SolidColorBrush(SfondoPagina) };
            double salvata;
            ColumnDefinition c0 = larghezze.TryGetValue(k, out salvata) ? new ColumnDefinition(salvata, GridUnitType.Pixel) : new ColumnDefinition(percento, GridUnitType.Star);
            c0.MinWidth = 200;
            g.ColumnDefinitions.Add(c0);
            g.ColumnDefinitions.Add(new ColumnDefinition(8, GridUnitType.Pixel));
            g.ColumnDefinitions.Add(new ColumnDefinition(100 - percento, GridUnitType.Star) { MinWidth = 420 });
            f.Width = double.NaN;
            f.HorizontalAlignment = HorizontalAlignment.Stretch;
            ScrollViewer sv = new ScrollViewer { Content = f, Margin = new Thickness(10, 10, 2, 10), VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
            g.Children.Add(sv);
            GridSplitter sp = new GridSplitter
            {
                ResizeDirection = GridResizeDirection.Columns, Width = 8, Margin = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromArgb(60, 128, 140, 155)), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.SizeWestEast)
            };
            ToolTip.SetTip(sp, "Trascina per allargare o stringere l'elenco; doppio clic: larghezza iniziale.");
            Grid.SetColumn(sp, 1);
            g.Children.Add(sp);
            sp.DragCompleted += (s, e) => { larghezze[k] = g.ColumnDefinitions[0].ActualWidth; };
            sp.DoubleTapped += (s, e) =>
            {
                larghezze.Remove(k);
                g.ColumnDefinitions[0].Width = new GridLength(percento, GridUnitType.Star);
                g.ColumnDefinitions[2].Width = new GridLength(100 - percento, GridUnitType.Star);
            };
            contenuto.Margin = new Thickness(2, 10, 10, 10);
            Grid.SetColumn(contenuto, 2);
            g.Children.Add(contenuto);
            return g;
        }

        public static DockPanel Pagina(Fascione f, ContentControl contenuto)
        {
            DockPanel d = new DockPanel { Background = new SolidColorBrush(SfondoPagina) };
            ScrollViewer sv = new ScrollViewer { Content = f, Margin = new Thickness(10, 10, 10, 10), VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
            DockPanel.SetDock(sv, Dock.Left);
            d.Children.Add(sv);
            contenuto.Margin = new Thickness(0, 10, 10, 10);
            d.Children.Add(contenuto);
            return d;
        }
    }
}
