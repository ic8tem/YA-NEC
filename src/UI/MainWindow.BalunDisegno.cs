// YA-NEC 0.5.3 - Calcolatori > Balun / Un-Un: colonna destra con il disegno di costruzione, i passi e la galleria di tutti i rapporti
// (bozzetto B approvato da IC8TEM il 15/11/2026 19:55). Il disegno e' in Controls/CalcDrawing.cs (Schema "balun").
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Control CostruisciDisegnoBalun(SimpleCalc sc, CalcDrawing dr, Control sinistra, Dictionary<string, ComboBox> tendine, out Action<CalcOutput> mostraDestra, Action ricalcola)
        {
            StackPanel root = new StackPanel { Spacing = 4 };
            TextBlock tit = new TextBlock { FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            root.Children.Add(tit);
            root.Children.Add(dr);
            StackPanel passi = new StackPanel { Spacing = 2 };
            root.Children.Add(passi);
            root.Children.Add(new TextBlock { Text = "Disegni per tutti i rapporti (clic per sceglierlo):", FontSize = 12.5, Foreground = Pal.B(Pal.Muted), Margin = new Thickness(0, 8, 0, 2) });
            WrapPanel gal = new WrapPanel();
            root.Children.Add(gal);
            List<Border> celle = new List<Border>();
            for (int r = 0; r < CalcBalun.Rapporti.Length; r++)
            {
                int rr = r;
                BalunSchema es = null;
                try { es = CalcBalun.Esempio(r); } catch { }
                CalcOutput fo = new CalcOutput { Schema = "balun", HideWires = true, Balun = es };
                CalcDrawing mini = new CalcDrawing { Width = 118, Height = 84, MinHeight = 0, Out = fo, Mini = true };
                StackPanel v = new StackPanel();
                v.Children.Add(mini);
                v.Children.Add(new TextBlock { Text = CalcBalun.Rapporti[r], FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Pal.B(Pal.Fore) });
                Border b = new Border { Child = v, BorderThickness = new Thickness(1), BorderBrush = Pal.B(Pal.Border), CornerRadius = new CornerRadius(6), Padding = new Thickness(3), Margin = new Thickness(0, 0, 6, 6), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
                b.PointerPressed += (s, e) =>
                {
                    CalcParam p = sc.Params.Find(x => x.Key == "r");
                    if (p == null) return;
                    p.Value = rr;
                    // le pillole del rapporto nella colonna sinistra seguono la scelta
                    Control wp = FindTag(sinistra, "pillole-r");
                    if (wp is WrapPanel w) for (int j = 0; j < w.Children.Count; j++) if (w.Children[j] is Button bb) StileScelta(bb, j == rr);
                    ricalcola();
                };
                celle.Add(b);
                gal.Children.Add(b);
            }
            mostraDestra = o =>
            {
                BalunSchema s = o != null ? o.Balun : null;
                tit.Text = s == null ? "Come costruirlo" : "Come costruirlo: " + s.Rapporto + " su " + s.Nucleo;
                passi.Children.Clear();
                int n = 1;
                if (o != null) foreach (string p in o.Passi) passi.Children.Add(new TextBlock { Text = (n++) + ". " + p, TextWrapping = TextWrapping.Wrap, FontSize = 13, Foreground = Pal.B(Pal.Fore) });
                CalcParam pr = sc.Params.Find(x => x.Key == "r");
                int sel = pr != null ? (int)pr.Value : -1;
                for (int i = 0; i < celle.Count; i++) { celle[i].BorderBrush = Pal.B(i == sel ? Pal.Accent : Pal.Border); celle[i].BorderThickness = new Thickness(i == sel ? 2 : 1); }
            };
            return root;
        }
    }
}
