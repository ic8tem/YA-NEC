// YA-NEC 1.5.2 - linguette riordinate (IC8TEM 19/11/2026 17:06-17:11, schema «Albero delle linguette» approvato con "corretto"):
// Smith diventa una voce della fascia di Diagrammi; NEC e Correnti vanno nella linguetta "NEC/correnti"; Variabili e Ottimizzatore nella
// linguetta "Variabili e ottimizzazione". Le sottolinguette hanno la stessa fascia dei Diagrammi (blu, voce scelta in arancio).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace YaNec
{
    /// <summary>Fascia di sottolinguette (stile della fascia dei Diagrammi) con le pagine gia' costruite.</summary>
    sealed class SottoLinguette
    {
        readonly Dictionary<string, Button> btn = new Dictionary<string, Button>();
        readonly Dictionary<string, Control> pag = new Dictionary<string, Control>();
        readonly List<string> ordine = new List<string>();
        readonly ContentControl corpo = new ContentControl { Margin = new Thickness(0, 6, 0, 0) };
        readonly string nome;
        public string Voce { get; private set; }
        /// <summary>Chiamata dopo ogni cambio di voce.</summary>
        public Action<string> Aperta;

        public SottoLinguette(string nome) { this.nome = nome; }

        public void Aggiungi(string voce, Control pagina) { ordine.Add(voce); pag[voce] = pagina; }

        public bool Ha(string voce) { return pag.ContainsKey(voce); }

        public Control Costruisci()
        {
            WrapPanel fp = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (string v in ordine)
            {
                string vv = v;
                Button b = new Button { Content = vv, Tag = nome + ":" + vv, Padding = new Thickness(14, 4), Margin = new Thickness(2, 2), CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(1) };
                b.Click += (s, e) => Apri(vv);
                btn[vv] = b;
                fp.Children.Add(b);
            }
            Border fascia = new Border { Background = new SolidColorBrush(Color.Parse("#2F4A66")), BorderBrush = Pal.B(Pal.Accent), BorderThickness = new Thickness(0, 2, 0, 0), Padding = new Thickness(8, 4), Child = fp };
            DockPanel d = new DockPanel();
            DockPanel.SetDock(fascia, Dock.Top);
            d.Children.Add(fascia);
            d.Children.Add(corpo);
            if (ordine.Count > 0) Apri(ordine[0], false);
            return d;
        }

        public void Apri(string v, bool avvisa = true)
        {
            if (!pag.ContainsKey(v)) return;
            Voce = v;
            foreach (KeyValuePair<string, Button> kv in btn)
            {
                bool on = kv.Key == v;
                kv.Value.Background = new SolidColorBrush(on ? Color.Parse("#E08A00") : Color.Parse("#2F4A66"));
                kv.Value.BorderBrush = new SolidColorBrush(on ? Color.Parse("#E08A00") : Color.Parse("#4A6787"));
                kv.Value.Foreground = new SolidColorBrush(Color.Parse(on ? "#FFFFFF" : "#E6EDF5"));
                kv.Value.FontWeight = on ? FontWeight.SemiBold : FontWeight.Normal;
            }
            corpo.Content = pag[v];
            Registro.Scrivi("AZIONE", nome + ": " + v);
            if (avvisa && Aperta != null) Aperta(v);
        }
    }

    public partial class MainWindow
    {
        public const string LinNecCorr = "NEC/correnti", LinVarOtt = "Variabili e ottimizzazione";
        SottoLinguette sottoNec, sottoVarOtt;

        /// <summary>1.5.2: apre una linguetta per nome; accetta anche i nomi delle linguette di prima (Smith, NEC, Correnti, Variabili,
        /// Ottimizzatore), che ora sono sottolinguette. Restituisce false se il nome non c'e'.</summary>
        public bool ApriLinguetta(string nome)
        {
            string princ = nome, voce = null;
            if (nome == "Smith") { princ = "Diagrammi"; voce = "Smith"; }
            else if (nome == "Frequenza" || nome == "Parametri") { princ = "Diagrammi"; voce = "Parametri"; }   // 1.5.3
            else if (nome == "Lobi") { princ = "Diagrammi"; voce = "Lobi"; }
            else if (nome == "NEC" || nome == "Correnti") { princ = LinNecCorr; voce = nome; }
            else if (nome == "Variabili" || nome == "Ottimizzatore") { princ = LinVarOtt; voce = nome; }
            int ti = -1;
            for (int i = 0; i < tabs.Count; i++) if ((tabs[i].Tag as string) == princ) ti = i;
            if (ti < 0) return false;
            if (voce != null)
            {
                if (princ == "Diagrammi") ApriVoceDiagrammi(voce);
                else if (princ == LinNecCorr && sottoNec != null) sottoNec.Apri(voce, false);
                else if (princ == LinVarOtt && sottoVarOtt != null) sottoVarOtt.Apri(voce, false);
            }
            SelectTab(ti);
            return true;
        }

        /// <summary>Le schede dell'ottimizzatore prendono il posto di quelle dei risultati quando la sottolinguetta Ottimizzatore e' aperta.</summary>
        bool OttimizzatoreAperto()
        {
            return curTab >= 0 && curTab < tabs.Count && (tabs[curTab].Tag as string) == LinVarOtt && sottoVarOtt != null && sottoVarOtt.Voce == "Ottimizzatore";
        }

        void AggiornaSchedeOtt()
        {
            bool ottAperto = OttimizzatoreAperto();
            if (resCards != null) resCards.IsVisible = !ottAperto;
            if (ottCards != null) ottCards.IsVisible = ottAperto;
        }

        Control CostruisciNecCorrenti(Control nec, Control correnti)
        {
            sottoNec = new SottoLinguette(LinNecCorr);
            sottoNec.Aggiungi("NEC", nec);
            sottoNec.Aggiungi("Correnti", correnti);
            return sottoNec.Costruisci();
        }

        Control CostruisciVarOtt(Control variabili, Control ottimizzatore)
        {
            sottoVarOtt = new SottoLinguette(LinVarOtt);
            sottoVarOtt.Aggiungi("Variabili", variabili);
            sottoVarOtt.Aggiungi("Ottimizzatore", ottimizzatore);
            sottoVarOtt.Aperta = v =>
            {
                AggiornaSchedeOtt();
                try { if (v == "Ottimizzatore") OttSincronizza(); }
                catch (Exception ex) { Status("Errore nella linguetta Ottimizzatore: " + ex.Message, true); }
            };
            return sottoVarOtt.Costruisci();
        }
    }
}
