// YA-NEC 1.0.9 - Fili: pulsante "Ins. elemento simmetrico" (IC8TEM 16/11/2026 20:51 "qua voglio un pulsante 'ins elemento simmetrico'";
// bozzetto approvato alle 21:36 "ok procedi"). Riquadro sotto la barra dei Fili: X sul boom, lunghezza totale, quota, raggio, segmenti
// (dispari), orientamento orizzontale (Y) o verticale (Z), inserimento in fondo o dopo la riga selezionata; anteprima della riga.
// Il filo e' diritto e centrato: Y1 = -L/2, Y2 = +L/2 (o Z per il verticale). Valgono numeri, variabili e formule.
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        Border simPannello;
        Action simPrecompila;

        Control BuildPannelloSimmetrico()
        {
            StackPanel sp = new StackPanel { Spacing = 6 };
            sp.Children.Add(new TextBlock { Text = "Elemento simmetrico  (filo diritto centrato sul boom; X e Z del centro)", FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            TextBox tX = Tb("0", 80), tL = Tb("1", 80), tZ = Tb("0", 70), tR = Tb("5", 60), tS = Tb("21", 60);
            foreach (TextBox t in new[] { tX, tL, tZ, tR, tS }) t.FontFamily = Pal.Mono;
            WrapPanel r1 = new WrapPanel();
            void Campo(string n, TextBox t, string u) { StackPanel c = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 0, 14, 4) }; c.Children.Add(Lbl(n)); c.Children.Add(t); if (u != null) c.Children.Add(Lbl(u)); r1.Children.Add(c); }
            Campo("X sul boom", tX, "m"); Campo("Lunghezza totale", tL, "m"); Campo("Z (quota)", tZ, "m"); Campo("Raggio", tR, "mm"); Campo("Segmenti", tS, "(dispari)");
            sp.Children.Add(r1);
            int orient = 0, dove = 0;
            TextBlock anteprima = new TextBlock { FontFamily = Pal.Mono, Foreground = Pal.B(Pal.Fore), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            Func<string[]> riga = null;
            Action aggiorna = () => { try { string[] v = riga(); anteprima.Text = "X1 " + v[0] + "  Y1 " + v[1] + "  Z1 " + v[2] + "  X2 " + v[3] + "  Y2 " + v[4] + "  Z2 " + v[5] + "  r " + v[6] + " mm  " + v[7] + " segm."; } catch (Exception ex) { anteprima.Text = ex.Message; } };
            WrapPanel r2 = new WrapPanel();
            r2.Children.Add(Lbl("Orientamento: "));
            r2.Children.Add(Scelta(new[] { "orizzontale (Y)", "verticale (Z)" }, 0, k => { orient = k; aggiorna(); }));
            r2.Children.Add(new TextBlock { Text = "   Inserisci: ", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore) });
            r2.Children.Add(Scelta(new[] { "in fondo", "dopo la riga selezionata" }, 0, k => dove = k));
            sp.Children.Add(r2);
            riga = () =>
            {
                string x = Pulito(tX.Text, "0"), z = Pulito(tZ.Text, "0"), l = Pulito(tL.Text, "1"), r = Pulito(tR.Text, "5");
                int ns; if (!int.TryParse((tS.Text ?? "").Trim(), out ns) || ns < 1) ns = 21;
                if (ns % 2 == 0) ns++;
                string meno, piu;
                double lv;
                if (double.TryParse(Expr.NormalizeDecimal(l), NumberStyles.Float, CultureInfo.InvariantCulture, out lv)) { meno = FormatVal(-lv / 2); piu = FormatVal(lv / 2); }
                else { meno = "-(" + l + ")/2"; piu = "(" + l + ")/2"; }
                if (orient == 0) return new[] { x, meno, z, x, piu, z, r, ns.ToString(CultureInfo.InvariantCulture) };
                // verticale: centro a quota Z
                double zv;
                string zb, za;
                if (double.TryParse(Expr.NormalizeDecimal(z), NumberStyles.Float, CultureInfo.InvariantCulture, out zv) && double.TryParse(Expr.NormalizeDecimal(l), NumberStyles.Float, CultureInfo.InvariantCulture, out lv))
                { zb = FormatVal(zv - lv / 2); za = FormatVal(zv + lv / 2); }
                else { zb = z + "-(" + l + ")/2"; za = z + "+(" + l + ")/2"; }
                return new[] { x, "0", zb, x, "0", za, r, ns.ToString(CultureInfo.InvariantCulture) };
            };
            foreach (TextBox t in new[] { tX, tL, tZ, tR, tS }) t.TextChanged += (s, e) => aggiorna();
            WrapPanel r3 = new WrapPanel();
            r3.Children.Add(new TextBlock { Text = "Anteprima riga:  ", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore) });
            r3.Children.Add(anteprima);
            Button bIns = Btn("Inserisci", true), bChiudi = Btn("Chiudi", false);
            bIns.Margin = new Thickness(12, 0, 6, 0);
            r3.Children.Add(bIns); r3.Children.Add(bChiudi);
            sp.Children.Add(r3);
            sp.Children.Add(Nota("Valgono numeri, variabili e formule (es. Lunghezza = LD2, X = XD2). Con una riga selezionata i campi partono dai suoi valori. Il filo va nella tabella: \"Applica e ricalcola\" lo mette nel modello."));
            simPrecompila = () =>
            {
                WireRow a = activeRow;
                if (a == null || a.Cells == null || a.ReadOnly) { aggiorna(); return; }
                double x1, y1, z1, y2, z2;
                bool n = Num(a.Cells[1].Text, out x1) & Num(a.Cells[2].Text, out y1) & Num(a.Cells[3].Text, out z1) & Num(a.Cells[5].Text, out y2) & Num(a.Cells[6].Text, out z2);
                if (n)
                {
                    tX.Text = FormatVal(x1);
                    if (Math.Abs(z2 - z1) > Math.Abs(y2 - y1)) { tL.Text = FormatVal(Math.Abs(z2 - z1)); tZ.Text = FormatVal((z1 + z2) / 2); }
                    else { tL.Text = FormatVal(Math.Abs(y2 - y1)); tZ.Text = FormatVal(z1); }
                }
                if (!string.IsNullOrWhiteSpace(a.Cells[7].Text)) tR.Text = a.Cells[7].Text;
                if (!string.IsNullOrWhiteSpace(a.Cells[8].Text)) tS.Text = a.Cells[8].Text;
                aggiorna();
            };
            bChiudi.Click += (s, e) => simPannello.IsVisible = false;
            bIns.Click += async (s, e) =>
            {
                try
                {
                    if (model == null) { await NuovoModello(); if (model == null) return; }
                    string[] v = riga();
                    int dopo = wireRows.Count - 1;
                    if (dove == 1 && activeRow != null) { int i = wireRows.IndexOf(activeRow); if (i >= 0) dopo = i; }
                    WireRow r = MakeWireRow(null);
                    for (int k = 0; k < 8; k++) r.Cells[1 + k].Text = v[k];
                    wireRows.Insert(Math.Min(dopo + 1, wireRows.Count), r);
                    RenumberWires();
                    wiresPanel.Mostra(r.Row, () => r.Cells[1].Focus());
                    Registro.Scrivi("AZIONE", "Fili: elemento simmetrico inserito (" + string.Join(" ", v) + ")");
                    Status("Elemento simmetrico inserito nella tabella dei Fili: \"Applica e ricalcola\" lo mette nel modello.");
                }
                catch (Exception ex) { Status("Elemento simmetrico: " + ex.Message, true); }
            };
            aggiorna();
            simPannello = new Border
            {
                Child = sp, IsVisible = false, Background = Pal.B(Pal.WarnBg), BorderBrush = Pal.B(Pal.Orange), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 6)
            };
            return simPannello;
        }

        static string Pulito(string t, string def) { t = (t ?? "").Trim(); return t.Length == 0 ? def : t; }
        static bool Num(string t, out double v) { return double.TryParse(Expr.NormalizeDecimal((t ?? "").Trim()), NumberStyles.Float, CultureInfo.InvariantCulture, out v); }
    }
}
