// YA-NEC - maniglia di riempimento delle tabelle, come in Excel.
// 1.0.11 (IC8TEM 16/11/2026 23:45 "se io copio un valore da una casella mi devi permettere di trascinare il valore nelle altre caselle";
// bozzetto approvato alle 23:48): la cella attiva ha un quadratino blu nell'angolo in basso a destra; trascinandolo in giu'/su o di lato
// le celle toccate si colorano e al rilascio ricevono lo stesso testo (numero, variabile o formula). Con piu' righe scelte (Maiusc + frecce
// nei fili) si ripete il blocco. Solo copia, niente serie automatiche. Ctrl+Z subito dopo rimette i valori di prima.
// Vale per la tabella dei fili e per le tabelle di sorgenti, carichi e linee.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace YaNec
{
    internal sealed class Maniglia
    {
        readonly Func<List<TextBox[]>> righe;          // celle modificabili per riga (null = colonna senza casella)
        readonly Action<List<TextBox>> dopo;           // dopo il riempimento (o l'annullamento): applica
        readonly Func<int[]> blocco;                   // righe scelte (prima, ultima) o null
        readonly Panel strato = new Panel { IsHitTestVisible = true };
        readonly Border quad;
        TextBox attiva;
        int rA = -1, cA = -1;
        bool trascina, verticale;
        Point inizio;
        readonly List<TextBox> segnate = new List<TextBox>();
        List<KeyValuePair<TextBox, string>> annulla;
        static readonly IBrush Segno = new SolidColorBrush(Color.Parse("#DBE7FB"));
        static readonly IBrush Blu = new SolidColorBrush(Color.Parse("#1D64C8"));

        public Maniglia(Control contenitore, Func<List<TextBox[]>> righe, Action<List<TextBox>> dopo, Func<int[]> blocco = null)
        {
            this.righe = righe; this.dopo = dopo; this.blocco = blocco;
            quad = new Border
            {
                Width = 8, Height = 8, Background = Blu, BorderBrush = Brushes.White, BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 1, 1), Cursor = new Cursor(StandardCursorType.Cross)
            };
            ToolTip.SetTip(quad, "Trascina per copiare il valore nelle celle sotto, sopra o di lato (come la maniglia di Excel). Ctrl+Z subito dopo annulla.");
            strato.Children.Add(quad);
            quad.PointerPressed += Premuto;
            quad.PointerMoved += Mosso;
            quad.PointerReleased += Rilasciato;
            quad.PointerCaptureLost += (s, e) => { if (trascina) { trascina = false; Pulisci(); } };
            // Ctrl+Z prima della TextBox (che ha il suo annulla): rimette i valori di prima del riempimento
            contenitore.AddHandler(InputElement.KeyDownEvent, (s, e) =>
            {
                if (annulla == null) return;
                if (e.Key == Key.Z && (e.KeyModifiers & KeyModifiers.Control) != 0)
                {
                    List<TextBox> toccate = new List<TextBox>();
                    foreach (KeyValuePair<TextBox, string> kv in annulla) { kv.Key.Text = kv.Value; Sincronizza(kv.Key); toccate.Add(kv.Key); }
                    annulla = null;
                    e.Handled = true;
                    dopo?.Invoke(toccate);
                }
                else if (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl && e.Key != Key.LeftShift && e.Key != Key.RightShift) annulla = null;
            }, RoutingStrategies.Tunnel);
        }

        /// <summary>La cella che ha preso il fuoco: la maniglia va nel suo angolo.</summary>
        public void Attiva(TextBox t)
        {
            if (trascina) return;
            if (attiva != null) AdornerLayer.SetAdorner(attiva, null);
            attiva = t; rA = cA = -1;
            if (t == null || t.Classes.Contains("ro") || !t.IsEnabled) return;
            List<TextBox[]> rr = righe();
            for (int r = 0; r < rr.Count && rA < 0; r++)
                for (int c = 0; c < rr[r].Length; c++) if (rr[r][c] == t) { rA = r; cA = c; break; }
            if (rA < 0) { attiva = null; return; }
            AdornerLayer.SetAdorner(t, strato);
        }

        void Premuto(object s, PointerPressedEventArgs e)
        {
            if (attiva == null || !e.GetCurrentPoint(quad).Properties.IsLeftButtonPressed) return;
            TopLevel tl = TopLevel.GetTopLevel(attiva);
            if (tl == null) return;
            trascina = true;
            inizio = e.GetPosition(tl);
            e.Pointer.Capture(quad);
            e.Handled = true;
        }

        void Mosso(object s, PointerEventArgs e)
        {
            if (!trascina || attiva == null) return;
            TopLevel tl = TopLevel.GetTopLevel(attiva);
            if (tl == null) return;
            Point p = e.GetPosition(tl);
            verticale = Math.Abs(p.Y - inizio.Y) >= Math.Abs(p.X - inizio.X);
            Pulisci();
            foreach (TextBox t in Bersagli(p, tl)) { t.Background = Segno; segnate.Add(t); }
        }

        void Rilasciato(object s, PointerReleasedEventArgs e)
        {
            if (!trascina) return;
            trascina = false;
            TopLevel tl = attiva != null ? TopLevel.GetTopLevel(attiva) : null;
            Pulisci();
            e.Pointer.Capture(null);
            if (tl == null) return;
            List<TextBox> dest = Bersagli(e.GetPosition(tl), tl);
            if (dest.Count == 0) return;
            // sorgente: la cella attiva, o il blocco di righe scelte (ripetuto) nella stessa colonna
            List<string> fonte = new List<string>();
            List<TextBox[]> rr = righe();
            int[] bl = blocco?.Invoke();
            if (verticale && bl != null && bl[0] <= rA && rA <= bl[1] && bl[1] > bl[0])
            {
                for (int r = bl[0]; r <= bl[1] && r < rr.Count; r++) if (cA < rr[r].Length && rr[r][cA] != null) fonte.Add(rr[r][cA].Text ?? "");
            }
            if (fonte.Count == 0) fonte.Add(attiva.Text ?? "");
            annulla = new List<KeyValuePair<TextBox, string>>();
            for (int i = 0; i < dest.Count; i++)
            {
                TextBox t = dest[i];
                annulla.Add(new KeyValuePair<TextBox, string>(t, t.Text ?? ""));
                t.Text = fonte[i % fonte.Count];
                Sincronizza(t);
            }
            dopo?.Invoke(dest);
            attiva.Focus();
        }

        // celle tra la cella attiva e il puntatore, nella colonna (verticale) o nella riga (orizzontale), solo quelle disegnate
        List<TextBox> Bersagli(Point p, TopLevel tl)
        {
            List<TextBox> l = new List<TextBox>();
            List<TextBox[]> rr = righe();
            if (rA < 0 || rA >= rr.Count) return l;
            if (verticale)
            {
                Rect? ra = Limiti(attiva, tl);
                if (ra == null) return l;
                bool giu = p.Y > ra.Value.Bottom;
                if (!giu && p.Y >= ra.Value.Top) return l;
                for (int r = rA + (giu ? 1 : -1); r >= 0 && r < rr.Count; r += giu ? 1 : -1)
                {
                    TextBox t = cA < rr[r].Length ? rr[r][cA] : null;
                    if (t == null || !t.IsEnabled || t.Classes.Contains("ro")) continue;
                    Rect? b = Limiti(t, tl);
                    if (b == null) break;
                    if (giu ? b.Value.Top > p.Y : b.Value.Bottom < p.Y) break;
                    l.Add(t);
                }
            }
            else
            {
                TextBox[] riga = rr[rA];
                Rect? ra = Limiti(attiva, tl);
                if (ra == null) return l;
                bool destra = p.X > ra.Value.Right;
                if (!destra && p.X >= ra.Value.Left) return l;
                for (int c = cA + (destra ? 1 : -1); c >= 0 && c < riga.Length; c += destra ? 1 : -1)
                {
                    TextBox t = riga[c];
                    if (t == null || !t.IsEnabled || t.Classes.Contains("ro")) continue;
                    Rect? b = Limiti(t, tl);
                    if (b == null) break;
                    if (destra ? b.Value.Left > p.X : b.Value.Right < p.X) break;
                    l.Add(t);
                }
            }
            return l;
        }

        static Rect? Limiti(Control c, TopLevel tl)
        {
            if (c == null || !c.IsEffectivelyVisible || TopLevel.GetTopLevel(c) != tl) return null;
            Point? a = c.TranslatePoint(new Point(0, 0), tl);
            if (a == null) return null;
            return new Rect(a.Value, c.Bounds.Size);
        }

        void Pulisci()
        {
            foreach (TextBox t in segnate) t.ClearValue(TemplatedControl.BackgroundProperty);
            segnate.Clear();
        }

        // le tabelle degli oggetti leggono le celle quando perdono il fuoco: si simula la perdita del fuoco
        static void Sincronizza(TextBox t)
        {
            t.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));
        }
    }
}
