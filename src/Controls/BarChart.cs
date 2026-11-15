// YA-NEC - grafico a barre orizzontali (graduatoria RDF delle antenne riceventi)
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace YaNec
{
    public class BarItem
    {
        public string Label;
        public double Value;
        public Color Color;
        public BarItem(string l, double v, Color c) { Label = l; Value = v; Color = c; }
    }

    public class BarChart : Control
    {
        public List<BarItem> Items = new List<BarItem>();
        public double LabelWidth = 230;

        public override void Render(DrawingContext ctx)
        {
            double W = Bounds.Width, H = Bounds.Height;
            ctx.DrawRectangle(Pal.B(Pal.ChartBg), null, new Rect(0, 0, W, H));
            if (Items.Count == 0) return;
            double max = 0;
            foreach (BarItem b in Items) max = Math.Max(max, b.Value);
            if (max <= 0) max = 1;
            double rowH = Math.Min(30, (H - 8) / Items.Count), bw = Math.Max(40, W - LabelWidth - 70);
            for (int i = 0; i < Items.Count; i++)
            {
                BarItem b = Items[i];
                double y = 4 + i * rowH;
                Draw.Text(ctx, b.Label, 6, y + rowH / 2 - 9, 13, Pal.ChartText);
                double w = bw * Math.Max(0, b.Value) / max;
                ctx.DrawRectangle(Pal.B(b.Color), null, new Rect(LabelWidth, y + rowH * 0.2, w, rowH * 0.6));
                Draw.Text(ctx, Draw.N(b.Value, 1), LabelWidth + w + 6, y + rowH / 2 - 9, 13, Pal.ChartText);
            }
        }
    }
}
