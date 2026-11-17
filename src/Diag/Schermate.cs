// YA-NEC 1.2.0 - immagini della finestra per il README e la wiki (avvio con --schermate): apre i modelli d'esempio, va sulle pagine
// nuove, aspetta i calcoli e salva le immagini in data/schermate. Non tocca il registro delle prove; alla fine si chiude.
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace YaNec
{
    public partial class MainWindow
    {
        public static bool ProveSchermate;

        async Task Schermate()
        {
            prRep = new System.Text.StringBuilder();
            prFile = "schermate.txt";
            string dir = Path.Combine(AppPaths.DataDir, "schermate");
            Directory.CreateDirectory(dir);
            Width = 1600; Height = 1000;
            await Task.Delay(1500);
            Func<string, Task> foto = async nome =>
            {
                await Task.Delay(600);
                PixelSize ps = new PixelSize(Math.Max(1, (int)Bounds.Width), Math.Max(1, (int)Bounds.Height));
                using (RenderTargetBitmap bmp = new RenderTargetBitmap(ps, new Vector(96, 96)))
                {
                    bmp.Render(this);
                    bmp.Save(Path.Combine(dir, nome + ".png"));
                }
                PrRiga("immagine " + nome + ".png");
            };
            Func<string, Task<bool>> apri = async nome =>
            {
                string f = Path.Combine(AppPaths.DataDir, "modelli", nome);
                if (!File.Exists(f)) { PrRiga("manca " + f); return false; }
                int c0 = nCalcoli, s0 = nSweep; LoadModelFile(f);
                return await AttendiCalcolo(c0, s0);
            };
            Func<string, bool> premi = testo =>
            {
                Button b = this.GetVisualDescendants().OfType<Button>().FirstOrDefault(x => x.IsEffectivelyVisible && TestoDi(x.Content).Trim().StartsWith(testo));
                if (b == null) { PrRiga("pulsante non trovato: " + testo); return false; }
                b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return true;
            };
            try
            {
                // HF: Yagi 5 elementi ARRL a 70 piedi, terreno reale verso gli USA da JN70CN82WE (Capri)
                if (await apri("ARRL 520-40H Yagi 5 el 20 m (70 ft).yanec"))
                {
                    SelectTab(IndiceLinguetta("Struttura 3D")); await foto("struttura-3d");
                    SelectTab(IndiceLinguetta("Diagrammi")); ApriVoceDiagrammi("Diagrammi"); await foto("diagrammi");
                    ApriVoceDiagrammi("Terreno reale"); await Attendi(() => terrEsito != null, 90000); await Task.Delay(2500); await foto("terreno-reale");
                    ApriVoceDiagrammi("Tabelle"); await foto("tabelle-fb-fr");
                    SelectTab(IndiceLinguetta("Smith")); await foto("smith");
                    SelectTab(IndiceLinguetta("Avvisi")); await Task.Delay(4000); await foto("avvisi-agt");
                }
                SelectTab(IndiceLinguetta("Diagrammi")); ApriVoceDiagrammi("Ricezione (RDF)"); await Task.Delay(20000); await foto("ricezione-rdf");
                ApriCalcolatore("carichihf"); await Task.Delay(1500); await foto("carichi-hf");
                // VHF: Yagi 10 elementi DJ9BV, tolleranze con 100 prove
                if (await apri("10 el DJ9BV BVO2-3WL 144 MHz (MMANA).yanec"))
                {
                    SelectTab(IndiceLinguetta("Rumore/GT/Stack")); await foto("rumore-gt");
                    SelectTab(IndiceLinguetta("Diagrammi")); ApriVoceDiagrammi("Tolleranze"); await Task.Delay(500);
                    TextBox tn = this.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.IsEffectivelyVisible && t.Text == "500");
                    if (tn != null) tn.Text = "100";
                    if (premi("Avvia")) await Attendi(() => this.GetVisualDescendants().OfType<Button>().Any(x => x.IsEffectivelyVisible && TestoDi(x.Content).StartsWith("Avvia") && x.IsEnabled), 240000);
                    await Task.Delay(1500); await foto("tolleranze");
                    ApriVoceDiagrammi("Terreno reale"); terrEsito = null; await Attendi(() => terrEsito != null, 90000); await Task.Delay(2500); await foto("terreno-reale-vhf");
                }
            }
            catch (Exception ex) { PrRiga("interrotte: " + ex); }
            File.WriteAllText(Path.Combine(dir, "schermate.txt"), prRep.ToString());
        }
    }
}
