// YA-NEC 0.5.3 - quota dell'antenna sul terreno (IC8TEM 15/11/2026 19:35: "apro questo e mi si blocca perche' mi dice che l'antenna e'
// sul terreno, ma in realta' non posso impostare io una quota di altezza quando inserisco un modello di terreno?"; bozzetto approvato
// alle 19:40: "si il bozzetto va bene e le quote le devo poter scegliere con centesimi di metro").
//  - casella "Quota" accanto al Terreno (solo con un terreno): punto piu' basso dell'antenna sopra il suolo o il solaio, in metri con
//    due decimali; le frecce cambiano di 0,01 m. Cambiandola tutta l'antenna si sposta in verticale (con Annulla).
//  - riquadro arancione al posto dell'errore quando con il terreno l'antenna e' sul suolo o sotto: proposta 2 m sopra un solaio,
//    10 m sopra un terreno; "Alza a ...", "Scrivo io la quota", "Torna allo spazio libero".
//  - modelli con le variabili: se tutte le quote Z dei fili cominciano con la stessa variabile numerica (Z, Z+..., Z-...), si cambia la
//    variabile; altrimenti si spostano le coordinate come Geometria > Sposta.
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        TextBlock lblQuota;
        TextBox txtQuota;
        Control quotaCtl;
        Border quotaAvviso;
        TextBlock quotaAvvisoTesto;
        Button quotaAlza;
        double quotaProposta = 10;
        bool quotaInAggiorna;

        /// <summary>Etichetta e casella della barra (aggiunte dopo il menu Terreno).</summary>
        void CostruisciQuota(Panel cmd)
        {
            lblQuota = Lbl("Quota");
            txtQuota = Tb("0,00", 64);
            ToolTip.SetTip(txtQuota, "Quota dell'antenna: punto piu' basso sopra il suolo (o sopra il solaio), in metri con due decimali. Cambiandola tutta l'antenna si sposta in verticale (Annulla torna indietro). Le frecce cambiano di 1 cm.");
            txtQuota.KeyDown += (s, e) => { if (e.Key == Key.Enter) { QuotaApplica(); e.Handled = true; } };
            txtQuota.LostFocus += (s, e) => QuotaApplica();
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 3) };
            sp.Children.Add(ConFrecce(txtQuota, x => 0.01, -100000, 100000, 2, QuotaApplica));
            sp.Children.Add(new TextBlock { Text = " m", VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Muted) });
            quotaCtl = sp;
            lblQuota.IsVisible = quotaCtl.IsVisible = false;
            cmd.Children.Add(lblQuota);
            cmd.Children.Add(quotaCtl);
        }

        /// <summary>Riquadro arancione sotto la barra (nascosto finche' non serve).</summary>
        Control CostruisciAvvisoQuota()
        {
            quotaAvvisoTesto = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Foreground = Pal.B(Pal.Fore), MaxWidth = 760 };
            quotaAlza = Btn("Alza a 10 m", true);
            quotaAlza.Click += (s, e) => QuotaPorta(quotaProposta);
            Button scrivo = Btn("Scrivo io la quota", false);
            scrivo.Click += (s, e) => { if (txtQuota != null) { txtQuota.Focus(); txtQuota.SelectAll(); } };
            Button libero = Btn("Torna allo spazio libero", false);
            libero.Click += (s, e) => { if (cmbGround != null) cmbGround.SelectedIndex = 0; };
            WrapPanel wp = new WrapPanel { Orientation = Orientation.Horizontal };
            wp.Children.Add(new Border { Child = quotaAvvisoTesto, Margin = new Thickness(0, 2, 12, 2) });
            foreach (Button b in new[] { quotaAlza, scrivo, libero }) { b.Margin = new Thickness(0, 2, 8, 2); wp.Children.Add(b); }
            quotaAvviso = new Border
            {
                Child = wp, IsVisible = false, Margin = new Thickness(0, 6, 0, 2), Padding = new Thickness(12, 8),
                Background = Pal.B(Pal.WarnBg), BorderBrush = Pal.B(Pal.Orange), BorderThickness = new Thickness(5, 1, 1, 1), CornerRadius = new CornerRadius(6)
            };
            return quotaAvviso;
        }

        static string FQ(double v) { return v.ToString("0.00", CultureInfo.CurrentCulture); }

        /// <summary>Punto piu' basso dell'antenna senza il terreno (la geometria con il terreno non si costruisce se l'antenna e' sul suolo).</summary>
        static double ZMinLibero(NecModel m)
        {
            try
            {
                NecModel c = m.Clone();
                c.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
                c.GroundFlag = 0;
                return TrasformaGeometria.ZMin(c.BuildGeometry());
            }
            catch { return double.NaN; }
        }

        /// <summary>Chiamata da Calculate: g = geometria costruita (null se non si costruisce), errore = messaggio del costruttore.</summary>
        void AggiornaQuota(SegmentGeometry g, string errore)
        {
            if (lblQuota == null) return;
            bool terreno = model != null && model.Ground.Kind != GroundKind.FreeSpace;
            lblQuota.IsVisible = quotaCtl.IsVisible = terreno;
            if (!terreno) { quotaAvviso.IsVisible = false; return; }
            double z = g != null ? TrasformaGeometria.ZMin(g) : ZMinLibero(model);
            if (!txtQuota.IsFocused && !double.IsNaN(z))
            {
                quotaInAggiorna = true;
                txtQuota.Text = FQ(z);
                quotaInAggiorna = false;
            }
            bool sulSuolo = (errore != null && errore.Contains("terreno")) || (!double.IsNaN(z) && z < -1e-6);
            if (!sulSuolo) { quotaAvviso.IsVisible = false; return; }
            bool solaio = model.Ground.Calcestruzzo > 0;
            quotaProposta = solaio ? 2 : 10;
            quotaAvvisoTesto.Text = "L'antenna e' a quota " + (double.IsNaN(z) ? "0" : FQ(z)) + " m: con il terreno va messa sopra il suolo. Quota proposta: " +
                FQ(quotaProposta) + " m sopra " + (solaio ? "il solaio" : "il terreno") + " (si sceglie anche al centimetro nella casella Quota).";
            quotaAlza.Content = "Alza a " + quotaProposta.ToString("0.##", CultureInfo.CurrentCulture) + " m";
            quotaAvviso.IsVisible = true;
        }

        void QuotaApplica()
        {
            if (quotaInAggiorna || building || model == null || txtQuota == null) return;
            if (model.Ground.Kind == GroundKind.FreeSpace) return;
            double q = ParseNum(txtQuota.Text, double.NaN);
            if (double.IsNaN(q)) { Status("Quota non valida: scrivi i metri, per esempio 2,35.", true); return; }
            QuotaPorta(Math.Round(q, 2));
        }

        /// <summary>Sposta l'antenna in verticale finche' il punto piu' basso e' a quota q (m).</summary>
        void QuotaPorta(double q)
        {
            if (model == null) return;
            double z0 = ZMinLibero(model);
            if (double.IsNaN(z0)) { Status("Quota: la geometria non si costruisce (Controlla per i dettagli).", true); return; }
            double dz = q - z0;
            if (Math.Abs(dz) < 0.0005) { quotaInAggiorna = true; txtQuota.Text = FQ(q); quotaInAggiorna = false; return; }
            try
            {
                string var;
                NecModel m = QuotaConVariabile(dz, out var);
                if (m == null) m = TrasformaGeometria.Applica(model, TrasformaGeometria.Sposta(0, 0, dz), null);
                Registro.Scrivi("AZIONE", "quota dell'antenna " + FQ(q) + " m (spostata di " + dz.ToString("0.###", CultureInfo.InvariantCulture) + " m" + (var != null ? ", variabile " + var : "") + ")");
                if (quotaAvviso != null) quotaAvviso.IsVisible = false;
                GeoMetti(m, true);
                Status("Quota: il punto piu' basso dell'antenna e' a " + FQ(q) + " m" + (var != null ? " (cambiata la variabile " + var + ")" : "") + ". \"Annulla\" torna indietro.");
            }
            catch (Exception ex) { Status("Quota: " + ex.Message, true); }
        }

        /// <summary>Se tutte le quote Z dei fili cominciano con la stessa variabile numerica, la cambia di dz; altrimenti null.</summary>
        NecModel QuotaConVariabile(double dz, out string nome)
        {
            nome = null;
            string v = null;
            int n = 0;
            foreach (GeomCard c in model.Geometry)
            {
                if (c.Id != "GW") continue;
                if (c.Src == null || c.Src.Length < 8) return null;
                foreach (int k in new[] { 4, 7 })
                {
                    string s = (c.Src[k] ?? "").Trim();
                    int i = 0;
                    while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    if (i == 0 || !char.IsLetter(s[0])) return null;
                    string t = s.Substring(0, i);
                    string resto = s.Substring(i).TrimStart();
                    if (resto.Length > 0 && resto[0] != '+' && resto[0] != '-') return null;
                    if (v == null) v = t;
                    else if (!string.Equals(v, t, StringComparison.OrdinalIgnoreCase)) return null;
                    n++;
                }
            }
            if (v == null || n == 0) return null;
            SyVar sv = model.Variables.Find(x => string.Equals(x.Name, v, StringComparison.OrdinalIgnoreCase));
            double val;
            if (sv == null || !double.TryParse(sv.Expr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out val)) return null;
            NecModel m = model.Clone();
            SyVar mv = m.Variables.Find(x => string.Equals(x.Name, v, StringComparison.OrdinalIgnoreCase));
            mv.Expr = (val + dz).ToString("0.####", CultureInfo.InvariantCulture);
            List<string> err = m.Reevaluate();
            if (err.Count > 0) return null;
            nome = sv.Name;
            return m;
        }
    }
}
