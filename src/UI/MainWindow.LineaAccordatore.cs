// YA-NEC 1.2.0 - Oggetti inseriti > Linee: linea aperta + accordatore alla TLW (bozzetto HF 11 B). Calcolo in Engine/LineaAccordatore.cs.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        string laCavo = "Linea a finestra 450", laFreq = "3,650; 7,100; 14,150; 28,500";
        double laLen = 22, laQ = 200; int laTipo = 0;
        List<LineaAccordatore.Riga> laRighe;
        NecModel laModello;

        Control PannelloLineaAccordatore()
        {
            List<Cable> cavi = CalcCavi.Lista ?? new List<Cable>();
            ComboBox cC = new ComboBox { ItemsSource = cavi, MinWidth = 260 };
            cC.SelectedItem = cavi.Find(c => c.Name.StartsWith(laCavo, StringComparison.Ordinal)) ?? (cavi.Count > 0 ? cavi[0] : null);
            TextBox tL = Tb(FN(laLen, 2), 60), tF = Tb(laFreq, 230), tQ = Tb(FN(laQ, 0), 50);
            ComboBox cT = new ComboBox { ItemsSource = LineaAccordatore.Tipi, SelectedIndex = laTipo };
            Button b = Btn("Calcola", true);
            Grid tab = new Grid();
            StackPanel avv = new StackPanel();
            TextBlock stato = Nota("");
            Action mostra = () =>
            {
                tab.Children.Clear(); tab.RowDefinitions.Clear(); tab.ColumnDefinitions.Clear(); avv.Children.Clear();
                if (laRighe == null || laModello != model) { stato.Text = "Premi Calcola: il NEC calcola l'impedenza del modello aperto a ogni frequenza."; return; }
                tab.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                foreach (var _ in laRighe) tab.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                Cable cv = cC.SelectedItem as Cable;
                string[] nomi = { "", "Z al punto di alimentazione (Ω)", "Linea " + (cv != null ? FN(cv.Z0, 0) + " Ω" : "") + ", " + FN(laLen, 1) + " m: ROS sulla linea", "Perdita nella linea",
                    "Z all'accordatore (Ω)", "Perdita nell'accordatore (" + LineaAccordatore.Tipi[laTipo].Split(' ')[0] + ", Q " + FN(laQ, 0) + ")", "Perdita totale" };
                Func<Complex, string> zs = z => FN(z.Real, z.Real >= 100 ? 0 : 1) + (z.Imaginary >= 0 ? " + j " : " - j ") + FN(Math.Abs(z.Imaginary), Math.Abs(z.Imaginary) >= 100 ? 0 : 1);
                for (int r = 0; r < nomi.Length; r++)
                {
                    tab.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    for (int c = 0; c <= laRighe.Count; c++)
                    {
                        string t; bool rosso = false;
                        if (c == 0) t = nomi[r];
                        else
                        {
                            LineaAccordatore.Riga x = laRighe[c - 1];
                            if (x.Errore != null) t = r == 0 ? FN(x.F, 3) : r == 1 ? x.Errore : "-";
                            else switch (r)
                            {
                                case 0: t = FN(x.F, 3); break;
                                case 1: t = zs(x.Zant); break;
                                case 2: t = FN(x.RosLinea, 1); break;
                                case 3: t = FN(x.PerditaLinea, 1) + " dB"; break;
                                case 4: t = zs(x.Zacc); rosso = x.Basso; break;
                                case 5: t = FN(x.PerditaAcc, 1) + " dB"; rosso = x.PerditaAcc > 1; break;
                                default: t = FN(x.Totale, 1) + " dB"; rosso = x.Totale > 2; break;
                            }
                        }
                        Border bd = new Border
                        {
                            Child = new TextBlock { Text = t, FontWeight = r == 0 || r == 6 ? FontWeight.SemiBold : FontWeight.Normal, Foreground = Pal.B(rosso ? Pal.Red : Pal.Fore) },
                            Padding = new Thickness(8, 4), Background = r == 0 ? Pal.B(Pal.Panel2) : null, BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 0, 1, 1)
                        };
                        Grid.SetRow(bd, r); Grid.SetColumn(bd, c); tab.Children.Add(bd);
                    }
                }
                foreach (LineaAccordatore.Riga x in laRighe)
                    if (x.Errore == null && x.Basso)
                        avv.Children.Add(AvvisoGiallo("A " + FN(x.F, 3) + " MHz l'accordatore vede " + FN(x.Zacc.Real, 1) + " Ω: perdite alte. Allunga o accorcia la linea (prova " + FN(laLen + 2, 1) + " o " + FN(Math.Max(1, laLen - 2), 1) + " m) per portare la Z sopra 20 Ω."));
                stato.Text = "";
            };
            b.Click += async (s, e) =>
            {
                if (model == null) return;
                laLen = Math.Max(0.1, ParseNum(tL.Text, 22)); laQ = Math.Max(10, ParseNum(tQ.Text, 200)); laTipo = Math.Max(0, cT.SelectedIndex); laFreq = tF.Text;
                Cable cv = cC.SelectedItem as Cable; if (cv == null) return; laCavo = cv.Name;
                List<double> fs = new List<double>();
                foreach (string p in (tF.Text ?? "").Split(';')) { double f = ParseNum(p, -1); if (f > 0) fs.Add(f); }
                NecModel m = model; double len = laLen, q = laQ; int tipo = laTipo;
                stato.Text = "calcolo in corso…";
                List<LineaAccordatore.Riga> l = await Task.Run(() =>
                {
                    List<LineaAccordatore.Riga> rr = new List<LineaAccordatore.Riga>();
                    SegmentGeometry g = m.BuildGeometry();
                    foreach (double f in fs)
                    {
                        LineaAccordatore.Riga x = new LineaAccordatore.Riga { F = f };
                        try
                        {
                            x.Zant = Analysis.Run(m, g, f, 15).Zin;
                            double pl;
                            x.RosLinea = LineaAccordatore.Ros(x.Zant, cv.Z0);
                            x.Zacc = LineaAccordatore.Trasforma(x.Zant, cv.Z0, cv.Vf, cv.AttenuationAt(f) / 100, f, len, out pl);
                            x.PerditaLinea = pl;
                            x.PerditaAcc = LineaAccordatore.PerditaAccordatore(x.Zacc, 50, tipo, q);
                            x.Totale = x.PerditaLinea + x.PerditaAcc;
                            x.Basso = x.Zacc.Real < 20;
                        }
                        catch (Exception ex) { x.Errore = ex.Message; }
                        rr.Add(x);
                    }
                    return rr;
                });
                laRighe = l; laModello = m;
                mostra();
            };
            StackPanel sp = new StackPanel { Spacing = 4 };
            WrapPanel w = new WrapPanel();
            w.Children.Add(Et("Linea")); w.Children.Add(cC); w.Children.Add(Et("lunga")); w.Children.Add(tL); w.Children.Add(Et("m  frequenze (MHz)")); w.Children.Add(tF);
            w.Children.Add(Et("accordatore")); w.Children.Add(cT); w.Children.Add(Et("Q della bobina")); w.Children.Add(tQ); w.Children.Add(b);
            FumettoGiallo(cT, "Tipo di accordatore tra la linea e il trasmettitore (50 Ω).", "perdita stimata dal Q caricato della rete (Qn = sqrt(Rmax/Rmin - 1), due sezioni per T e Pi-greco) e dal Q della bobina; TLW di N6BV calcola i componenti uno per uno e puo' dare qualche decimo di dB diverso.", "capitolo 14 (Linea aperta + accordatore)");
            sp.Children.Add(w);
            sp.Children.Add(tab);
            sp.Children.Add(Nota("Esempio: doppietto 2 × 20 m (ZS6BKW/G5RV). Avviso quando l'accordatore vede meno di 20 Ω: perdite alte, si suggerisce di allungare o accorciare la linea. Linea con perdite come TLW (N6BV, ARRL)."));
            sp.Children.Add(avv); sp.Children.Add(stato);
            mostra();
            return Pannello("Linea aperta + accordatore (alla TLW)", sp);
        }
    }
}
