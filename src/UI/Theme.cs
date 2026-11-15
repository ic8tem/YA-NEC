// YA-NEC - schemi di colori (gli stessi valori di YAEC e YA-MS: Chiaro, Notte blu, Nero).
// 0.5.3 (IC8TEM 15/11/2026 20:20 e 21:03, "come ip-trx-ctrl"): i sei temi di IP-TRX-CTRL (Chiaro, Notte, Nero, Blu, Viola, Verde),
// con i colori di Themes.cs di IP-TRX-CTRL per Blu, Viola e Verde; Chiaro, Notte e Nero restano quelli di prima (sono gli stessi di YAEC).
using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace YaNec
{
    public static class Pal
    {
        public static Color Back, Panel, Panel2, Panel3, Track, Border, Fore, Muted;
        public static Color Accent, AccentSoft, Green, Yellow, Orange, Red, Violet, Cyan;
        public static Color Field, FieldFore, ChartBg, ChartGrid, ChartText;
        public static Color OkBg, WarnBg, WarnText, BarFore, BarMuted, BarHover, AccentFore;
        public static Color Pill, PillFore, PillBorder, PillHover, CodeBack;
        public static int Schema;
        // 0.5.3 (IC8TEM 15/11/2026 21:23, "armonizzare in addizione tutti i programmi"): i 18 schemi di E-LAYER ANALYZER, la somma di
        // YAEC (Chiaro, Notte, Nero, 8 pastelli, Blu, Viola, Verde), YA-MS (Scia meteorica) e IP-TRX-CTRL (Chiaro, Notte, Nero di IP-TRX-CTRL)
        public static readonly string[] SchemaNames = { "Chiaro", "Notte", "Nero",
            "Pastello azzurro", "Pastello arancione", "Pastello acquamarina", "Pastello verde", "Pastello rosa", "Pastello lilla", "Pastello indaco", "Pastello marrone",
            "Scia meteorica", "Chiaro (IP-TRX-CTRL)", "Notte (IP-TRX-CTRL)", "Nero (IP-TRX-CTRL)", "Blu", "Viola", "Verde" };
        static readonly bool[] SchemaScuro = { false, true, true, false, false, false, false, false, false, false, false, true, false, true, true, true, true, true };
        public static bool Dark { get { return SchemaScuro[Math.Max(0, Math.Min(SchemaScuro.Length - 1, Schema))]; } }
        public const int SchemaScia = 11;
        static readonly Color[] PastelloTinta = { Rgb(70, 150, 230), Rgb(240, 140, 50), Rgb(40, 180, 170), Rgb(80, 170, 90), Rgb(230, 110, 160), Rgb(160, 120, 210), Rgb(80, 90, 190), Rgb(150, 105, 70) };
        // temi di IP-TRX-CTRL (UI/Themes.cs): Bg, Panel, Inset, Border, Field, Text, Muted, Btn1, Btn2, BtnText, BtnBorder, Accent, AccentText, QrgBg, Qrg, Ok, Tx
        static readonly string[][] IpColori =
        {
            new[] { "#F5F7FA", "#FFFFFF", "#F0F2F5", "#D5DAE1", "#FFFFFF", "#1B1F24", "#555D68", "#FDFEFF", "#E3E7ED", "#1B1F24", "#B9C0CA", "#1F6FEB", "#FFFFFF", "#141A22", "#F5B942", "#1E7A3C", "#D92D20" },
            new[] { "#0F1A2B", "#15233A", "#0B1526", "#2A3D5C", "#0B1526", "#E6ECF5", "#A9B6C9", "#2E4870", "#1F3354", "#FFFFFF", "#3B5682", "#5AA2FF", "#0B1526", "#070E19", "#F5B942", "#8EE0A6", "#FF4D4F" },
            new[] { "#0B0B0B", "#151515", "#050505", "#303030", "#050505", "#F2F2F2", "#A6A6A6", "#323232", "#1D1D1D", "#FFFFFF", "#444444", "#FF9F1C", "#000000", "#000000", "#FFB000", "#7CE29A", "#FF3B30" },
            new[] { "#12417B", "#16498A", "#0C3162", "#2F64A8", "#0C3162", "#FFFFFF", "#C9D8EC", "#4384D3", "#2B63AD", "#FFFFFF", "#5A93DA", "#FFD24A", "#0C3162", "#0A2A55", "#FFD24A", "#9BF0B5", "#FF5A52" },
            new[] { "#2A1A41", "#33204F", "#22143A", "#573A85", "#3C245D", "#F4EEFB", "#CDBFDF", "#7650AF", "#583789", "#FFFFFF", "#8A66C2", "#E2C6FF", "#2A1A41", "#1E1131", "#FFD98A", "#B9F2A0", "#FF5C7A" },
            new[] { "#12301F", "#173B27", "#0B2215", "#2E6C49", "#0B2215", "#E6F6EC", "#BAD4C5", "#3E8A5D", "#2B6644", "#FFFFFF", "#4F9B6E", "#A6E35F", "#0B2215", "#08180F", "#E8F5A0", "#A6E35F", "#FF5A4E" },
        };
        static Color H(string hex) { return Color.Parse(hex); }
        static Color Mix(Color a, Color b, double t) { return Rgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        static Color Tinta(Color t, double k) { return Rgb((int)(255 + (t.R - 255) * k), (int)(255 + (t.G - 255) * k), (int)(255 + (t.B - 255) * k)); }   // come YAEC: verso il bianco

        public static Color Rgb(int r, int g, int b) { return Color.FromRgb((byte)r, (byte)g, (byte)b); }
        public static Color A(int alpha, Color c) { return Color.FromArgb((byte)alpha, c.R, c.G, c.B); }
        public static IBrush B(Color c) { return new ImmutableSolidColorBrush(c); }
        public static IPen P(Color c, double w = 1) { return new ImmutablePen(new ImmutableSolidColorBrush(c), w); }
        public static IPen P(Color c, double w, ImmutableDashStyle dash) { return new ImmutablePen(new ImmutableSolidColorBrush(c), w, dash); }
        public static readonly ImmutableDashStyle Dash = new ImmutableDashStyle(new double[] { 4, 3 }, 0);
        public static readonly ImmutableDashStyle Dot = new ImmutableDashStyle(new double[] { 1, 2 }, 0);
        public static readonly FontFamily Mono = new FontFamily("Cascadia Mono, Consolas, DejaVu Sans Mono, Menlo, Liberation Mono, monospace");

        // colori delle serie dei grafici (leggibili su fondo chiaro e scuro)
        public static Color Series(int i)
        {
            Color[] light = { Rgb(31, 111, 209), Rgb(217, 83, 25), Rgb(19, 150, 80), Rgb(124, 58, 237), Rgb(181, 131, 0), Rgb(190, 50, 150) };
            Color[] dark = { Rgb(95, 168, 255), Rgb(255, 150, 80), Rgb(61, 220, 132), Rgb(180, 150, 255), Rgb(255, 209, 102), Rgb(240, 171, 252) };
            Color[] a = Dark ? dark : light;
            return a[((i % a.Length) + a.Length) % a.Length];
        }

        static Pal() { Load(0); }

        public static void Load(int schema)
        {
            Schema = Math.Max(0, Math.Min(SchemaNames.Length - 1, schema));
            if (Schema > SchemaScia)
            {
                // i sei temi di IP-TRX-CTRL, come in E-LAYER ANALYZER (LoadIp)
                string[] c = IpColori[Schema - SchemaScia - 1];
                bool dk = Dark;
                Color bg = H(c[0]), pan = H(c[1]), ins = H(c[2]), bor = H(c[3]), fld = H(c[4]), txt = H(c[5]), mut = H(c[6]);
                Color btn1 = H(c[7]), btn2 = H(c[8]), btnTxt = H(c[9]), btnBor = H(c[10]), acc = H(c[11]), accTxt = H(c[12]), qrgBg = H(c[13]), qrg = H(c[14]), ok = H(c[15]), tx = H(c[16]);
                Color white = Rgb(255, 255, 255);
                Back = bg; Panel = pan; Panel2 = ins; Panel3 = btn2; Track = qrgBg; Border = bor; Fore = txt; Muted = mut;
                Accent = acc; AccentSoft = Mix(bg, acc, dk ? 0.18 : 0.12); AccentFore = accTxt;
                Violet = dk ? Rgb(196, 170, 255) : Rgb(124, 58, 237); Green = ok; Yellow = dk ? qrg : Rgb(181, 131, 0);
                Orange = dk ? Rgb(255, 170, 64) : Rgb(217, 123, 0); Red = tx; Cyan = dk ? Rgb(110, 225, 235) : Rgb(0, 140, 160);
                Field = fld; FieldFore = txt; ChartBg = ins; ChartGrid = bor; ChartText = mut;
                OkBg = Mix(bg, ok, dk ? 0.28 : 0.16); WarnBg = dk ? Mix(bg, Orange, 0.22) : Rgb(255, 245, 220); WarnText = dk ? Rgb(254, 215, 170) : Rgb(180, 83, 9);
                BarFore = dk ? txt : Rgb(226, 232, 240); BarMuted = dk ? mut : Rgb(148, 163, 184); BarHover = Mix(qrgBg, white, 0.12);
                Pill = btn2; PillFore = btnTxt; PillBorder = btnBor; PillHover = btn1; CodeBack = fld;
            }
            else if (Schema == SchemaScia)
            {
                // "Scia meteorica": la tavolozza originale di YA-MS
                Back = Rgb(22, 18, 48); Panel = Rgb(28, 23, 64); Panel2 = Rgb(34, 28, 71); Panel3 = Rgb(43, 33, 96);
                Track = Rgb(14, 11, 34); Border = Rgb(58, 50, 112); Fore = Rgb(238, 235, 250); Muted = Rgb(179, 173, 214);
                Accent = Rgb(56, 189, 248); AccentSoft = Rgb(11, 34, 51); AccentFore = Rgb(14, 11, 34);
                Violet = Rgb(167, 139, 250); Green = Rgb(134, 239, 172); Yellow = Rgb(253, 224, 71); Orange = Rgb(251, 146, 60); Red = Rgb(248, 113, 113); Cyan = Rgb(80, 220, 230);
                Field = Back; FieldFore = Fore; ChartBg = Back; ChartGrid = Border; ChartText = Rgb(220, 220, 220);
                OkBg = Rgb(20, 64, 44); WarnBg = Rgb(58, 35, 18); WarnText = Rgb(254, 215, 170);
                BarFore = Fore; BarMuted = Muted; BarHover = Rgb(43, 33, 96);
                Pill = Rgb(43, 33, 96); PillFore = Fore; PillBorder = Rgb(58, 50, 112); PillHover = Rgb(59, 47, 134); CodeBack = Rgb(16, 20, 32);
            }
            else if (Schema == 1 || Schema == 2)
            {
                bool nero = Schema == 2;
                Back = nero ? Rgb(0, 0, 0) : Rgb(11, 18, 32);
                Fore = nero ? Rgb(235, 235, 235) : Rgb(219, 228, 245);
                Muted = nero ? Rgb(140, 140, 140) : Rgb(127, 141, 176);
                Field = nero ? Rgb(18, 18, 18) : Rgb(18, 27, 46);
                Panel2 = nero ? Rgb(34, 34, 34) : Rgb(26, 38, 64);
                Accent = nero ? Rgb(80, 180, 255) : Rgb(95, 168, 255);
                Green = nero ? Rgb(60, 235, 130) : Rgb(61, 220, 132);
                Red = nero ? Rgb(255, 95, 85) : Rgb(255, 110, 100);
                Orange = nero ? Rgb(255, 170, 40) : Rgb(255, 179, 64);
                OkBg = nero ? Rgb(14, 60, 32) : Rgb(20, 64, 44);
                WarnBg = nero ? Rgb(70, 52, 14) : Rgb(74, 58, 26);
                WarnText = Rgb(254, 215, 170);
                AccentSoft = nero ? Rgb(14, 30, 56) : Rgb(22, 40, 70);
                ChartBg = nero ? Rgb(8, 8, 8) : Rgb(14, 22, 40);
                ChartGrid = nero ? Rgb(48, 48, 48) : Rgb(40, 52, 80);
                ChartText = nero ? Rgb(175, 175, 175) : Rgb(160, 175, 200);
                Panel3 = nero ? Rgb(32, 32, 32) : Rgb(28, 40, 66);
                Panel = nero ? Rgb(18, 18, 18) : Rgb(18, 27, 46);
                Border = nero ? Rgb(52, 52, 52) : Rgb(36, 49, 79);
                Yellow = nero ? Rgb(255, 209, 72) : Rgb(255, 209, 102);
                Violet = nero ? Rgb(180, 150, 255) : Rgb(167, 139, 250);
                Cyan = Rgb(80, 220, 230);
                Track = nero ? Rgb(12, 12, 12) : Rgb(6, 11, 22);
                FieldFore = Fore;
                BarFore = Fore; BarMuted = Muted; BarHover = Panel2;
                AccentFore = Back;
                Pill = Panel3; PillFore = Fore; PillBorder = Border; PillHover = nero ? Rgb(52, 52, 52) : Rgb(40, 56, 92);
                CodeBack = Field;
            }
            else
            {
                Back = Rgb(240, 240, 240); Panel = Rgb(255, 255, 255); Panel2 = Rgb(230, 232, 240); Panel3 = Rgb(240, 240, 240);
                Track = Rgb(30, 41, 59); Border = Rgb(213, 220, 234);
                Fore = Rgb(0, 0, 0); Muted = Rgb(109, 109, 109);
                Accent = Rgb(31, 111, 209); AccentSoft = Rgb(235, 245, 255);
                Violet = Rgb(124, 58, 237); Green = Rgb(19, 150, 80); Yellow = Rgb(181, 131, 0); Orange = Rgb(217, 123, 0);
                Red = Rgb(178, 34, 34); Cyan = Rgb(0, 140, 160);
                Field = Rgb(255, 255, 255); FieldFore = Rgb(0, 0, 0);
                ChartBg = Rgb(255, 255, 255); ChartGrid = Rgb(225, 228, 235); ChartText = Rgb(80, 90, 105);
                OkBg = Rgb(214, 245, 214); WarnBg = Rgb(255, 245, 220); WarnText = Rgb(180, 83, 9);
                BarFore = Rgb(226, 232, 240); BarMuted = Rgb(148, 163, 184); BarHover = Rgb(44, 56, 78);
                AccentFore = Rgb(255, 255, 255);
                Pill = Rgb(255, 255, 255); PillFore = Rgb(37, 84, 160); PillBorder = Border; PillHover = AccentSoft;
                CodeBack = Rgb(255, 255, 255);
                if (Schema >= 3 && Schema < 3 + PastelloTinta.Length)
                {
                    // pastello: lo schema chiaro con fondo, riquadri, campi e grafici tinti di un colore tenue (come YAEC)
                    Color t = PastelloTinta[Schema - 3];
                    Back = Tinta(t, 0.24); Panel2 = Tinta(t, 0.34); Field = Tinta(t, 0.05); Panel = Tinta(t, 0.08);
                    ChartBg = Tinta(t, 0.06); ChartGrid = Tinta(t, 0.22); Border = Tinta(t, 0.30); Panel3 = Tinta(t, 0.18);
                    AccentSoft = Tinta(t, 0.12); Pill = Panel; PillBorder = Border; PillHover = AccentSoft; CodeBack = Field;
                }
            }
            try
            {
                if (Application.Current != null)
                    Application.Current.RequestedThemeVariant = Dark ? ThemeVariant.Dark : ThemeVariant.Light;
            }
            catch { }
        }

        // gradiente per l'intensita' di corrente (0 = blu, 1 = rosso)
        public static Color Heat(double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            double r, g, b;
            if (t < 0.25) { r = 0; g = 4 * t; b = 1; }
            else if (t < 0.5) { r = 0; g = 1; b = 1 - 4 * (t - 0.25); }
            else if (t < 0.75) { r = 4 * (t - 0.5); g = 1; b = 0; }
            else { r = 1; g = 1 - 4 * (t - 0.75); b = 0; }
            return Rgb((int)(40 + 215 * r), (int)(40 + 200 * g), (int)(40 + 215 * b));
        }
    }
}
