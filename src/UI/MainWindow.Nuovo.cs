// YA-NEC 0.5.3 - pulsante "Nuovo" (IC8TEM 15/11/2026 20:26: "un pulsante per 'crea nuovo', così azzero quello che ho. se ho un modello
// non salvato mi avvisi"; bozzetto approvato alle 20:27). Il modello e' "non salvato" se il suo testo NEC e' diverso da quello dell'apertura
// o dell'ultimo salvataggio; i modelli creati dai calcolatori, dagli stack o dalla linea di adattamento non sono mai stati salvati.
using System;
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
        string testoSalvato;

        void SegnaSalvato() { testoSalvato = model != null ? model.ToNec() : null; }

        bool ModelloNonSalvato()
        {
            if (model == null || model.Geometry.Count == 0) return false;
            string t;
            try { t = model.ToNec(); } catch { return true; }
            return t != testoSalvato;
        }

        // 0.5.3 (IC8TEM 15/11/2026 21:00, "si" alla proposta): lo stesso avviso anche per Apri modello, Esempi e chiusura del programma.
        // Ritorna true se si puo' andare avanti (modello salvato, salvato adesso o "senza salvare"), false se l'utente annulla.
        bool chiusuraConfermata;

        async Task<bool> ChiediSeNonSalvato(string domanda, string senza)
        {
            if (!ModelloNonSalvato() || ProveAutomatiche) return true;
            string nome = string.IsNullOrEmpty(model.FileName) ? "Il modello aperto" : "\"" + System.IO.Path.GetFileName(model.FileName) + "\"";
            Window w = new Window { Title = "Modello non salvato", Width = 600, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel) };
            StackPanel sp = new StackPanel { Margin = new Thickness(20, 16), Spacing = 10 };
            sp.Children.Add(new TextBlock { Text = "Modello non salvato", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore) });
            sp.Children.Add(new Border
            {
                Background = Pal.B(Pal.WarnBg), BorderBrush = Pal.B(Pal.Orange), BorderThickness = new Thickness(5, 0, 0, 0), CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 8),
                Child = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Fore), Text = nome + " ha modifiche non salvate (fili, sorgenti, carichi, linee, variabili o terreno cambiati dopo l'apertura o l'ultimo salvataggio)." }
            });
            sp.Children.Add(new TextBlock { Text = domanda, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap });
            WrapPanel bt = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
            Button bS = Btn("Salva e poi " + senza, true), bN = Btn(char.ToUpperInvariant(senza[0]) + senza.Substring(1) + " senza salvare", false), bA = Btn("Annulla", false);
            foreach (Button b in new[] { bS, bN, bA }) { b.Margin = new Thickness(8, 0, 0, 0); bt.Children.Add(b); }
            sp.Children.Add(bt);
            w.Content = sp;
            int scelta = 2;
            bS.Click += (s, e) => { scelta = 0; w.Close(); };
            bN.Click += (s, e) => { scelta = 1; w.Close(); };
            bA.Click += (s, e) => { scelta = 2; w.Close(); };
            await w.ShowDialog(this);
            Registro.Scrivi("AZIONE", "modello non salvato (" + senza + "): " + (scelta == 0 ? "salva e poi " + senza : scelta == 1 ? senza + " senza salvare" : "annullato"));
            if (scelta == 2) return false;
            if (scelta == 0)
            {
                await SaveModelDialog();
                if (ModelloNonSalvato()) { Status("Salvataggio non fatto: il modello aperto resta com'e'.", true); return false; }
            }
            return true;
        }

        // 1.5.2 (IC8TEM 19/11/2026 11:15 "si"): dopo Nuovo, o aprendo un file, la tendina Esempi torna a "Esempi..."
        // (prima restava sul nome dell'ultimo esempio aperto anche con un altro modello)
        void EsempiVuoto()
        {
            if (cmbEsempi == null || cmbEsempi.SelectedItem == null) return;
            bool old = building; building = true; cmbEsempi.SelectedItem = null; building = old;
        }

        async Task NuovoModello()
        {
            if (!await ChiediSeNonSalvato("Cosa faccio prima di cominciare un modello nuovo?", "nuovo")) return;
            double f = ParseNum(txtFreq != null ? txtFreq.Text : "", 144.3);
            if (!(f > 0)) f = 144.3;
            // NecModel.Parse non accetta un file senza geometria: il modello vuoto si costruisce direttamente
            NecModel m = new NecModel { FreqMHz = f, FreqCount = 1, FileName = "nuovo.nec" };
            m.Ground = new GroundSpec { Kind = GroundKind.FreeSpace };
            m.Comments.Add("modello nuovo (YA-NEC)");
            SetModel(m, null);
            // 1.0.2: il modello nuovo e' vuoto, quindi via anche la struttura, i risultati, le sorgenti e la quota del modello precedente
            // (prima restavano a schermo finche' non si calcolava di nuovo)
            geo = null;
            try { RefreshAll(); AggiornaQuota(null, null); AggiornaOggetti(); }
            catch (Exception ex) { Registro.Scrivi("ERRORE", "ridisegno del modello nuovo: " + ex.Message); }
            SegnaSalvato();
            EsempiVuoto();
            Registro.Scrivi("AZIONE", "nuovo modello vuoto a " + f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " MHz");
            Status("Modello nuovo, vuoto: aggiungi i fili nella tabella dei Fili o con la barra Geometria, oppure parti da un calcolatore.");
            SelectTab(0);
        }
    }
}
