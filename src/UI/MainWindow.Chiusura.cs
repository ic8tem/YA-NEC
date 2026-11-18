// YA-NEC 1.4 - finestra di chiusura come in LIVETROPOVHF (richiesta di IC8TEM del 18/11/2026 14:47, "inseriamo anche questo"):
// icona, domanda, ringraziamento in corsivo, barra grigia con Si' e No. Invio = Si', Esc = No.
using System;
using System.Diagnostics;
using Avalonia.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace YaNec
{
    public partial class MainWindow
    {
        async Task<bool> ChiediChiusura()
        {
            Window w = new Window
            {
                Title = "Chiusura del programma", Width = 480, SizeToContent = SizeToContent.Height, CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel), ShowInTaskbar = false, Icon = Icon
            };
            bool si = false;
            DockPanel top = new DockPanel { Margin = new Thickness(20, 20, 20, 16) };
            try
            {
                Bitmap img = new Bitmap(Avalonia.Platform.AssetLoader.Open(new System.Uri("avares://YA-NEC/assets/ya-nec-256.png")));
                Border pic = new Border { Width = 64, Height = 64, CornerRadius = new CornerRadius(10), ClipToBounds = true, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Top, Child = new Image { Source = img, Stretch = Stretch.UniformToFill } };
                DockPanel.SetDock(pic, Dock.Left);
                top.Children.Add(pic);
            }
            catch { }
            TextBlock q = new TextBlock { Text = "Vuoi chiudere YA-NEC?", FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            TextBlock grazie = new TextBlock { Text = "Grazie per avermi usato: spero di esserti stato utile.", FontSize = 14, FontStyle = FontStyle.Italic, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
            top.Children.Add(new StackPanel { Children = { q, grazie }, VerticalAlignment = VerticalAlignment.Center });
            Button bSi = Btn("Sì", true), bNo = Btn("No", false);
            bSi.MinWidth = 90; bNo.MinWidth = 90;
            bSi.Click += (s, e) => { si = true; w.Close(); };
            bNo.Click += (s, e) => w.Close();
            Border barra = new Border
            {
                Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(16, 10),
                Child = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right, Children = { bSi, bNo } }
            };
            w.Content = new StackPanel { Children = { top, barra } };
            w.KeyDown += (s, e) => { if (e.Key == Avalonia.Input.Key.Escape) w.Close(); if (e.Key == Avalonia.Input.Key.Enter) { si = true; w.Close(); } };
            await w.ShowDialog(this);
            Registro.Scrivi("AZIONE", "chiusura del programma: " + (si ? "si'" : "no"));
            return si;
        }

        // 1.4.1 (IC8TEM 18/11/2026 16:18 "poi con un popup avvisa"; 16:27 "l'utente non potra' interagire" = solo avviso, poi riavvio):
        // finestra senza pulsanti, con la grafica della finestra di chiusura, che conta 10 secondi e riavvia YA-NEC.
        // Il modello aperto si riapre dopo il riavvio; se non era salvato se ne salva prima una copia in data/aggiornamenti.
        bool avvisoAggAperto;
        public const int SecondiRiavvio = 10;

        async void AvvisoAggiornamento(string nuova, bool installata)
        {
            if (avvisoAggAperto || ProveAutomatiche) return;
            avvisoAggAperto = true;
            bool puoChiudere = false;
            Window w = new Window
            {
                Title = Lang.T("Aggiornamento di YA-NEC"), Width = 500, SizeToContent = SizeToContent.Height, CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Pal.B(Pal.Panel), ShowInTaskbar = false, Icon = Icon,
                SystemDecorations = SystemDecorations.BorderOnly, Topmost = true
            };
            w.Closing += (s, e) => { if (!puoChiudere) e.Cancel = true; };
            DockPanel top = new DockPanel { Margin = new Thickness(20, 20, 20, 16) };
            try
            {
                Bitmap img = new Bitmap(Avalonia.Platform.AssetLoader.Open(new System.Uri("avares://YA-NEC/assets/ya-nec-256.png")));
                Border pic = new Border { Width = 64, Height = 64, CornerRadius = new CornerRadius(10), ClipToBounds = true, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Top, Child = new Image { Source = img, Stretch = Stretch.UniformToFill } };
                DockPanel.SetDock(pic, Dock.Left);
                top.Children.Add(pic);
            }
            catch { }
            TextBlock q = new TextBlock { Text = Lang.F("È pronta la versione {0} di YA-NEC", nuova), FontSize = 17, FontWeight = FontWeight.Bold, Foreground = Pal.B(Pal.Fore), TextWrapping = TextWrapping.Wrap };
            TextBlock t2 = new TextBlock
            {
                Text = installata ? Lang.T("È già scaricata e installata. Il programma si riavvia da solo e riapre il modello aperto.")
                                  : Lang.T("Sul Mac non si installa da sola: scarica il nuovo .dmg dalla pagina delle versioni su GitHub."),
                FontSize = 14, Foreground = Pal.B(Pal.Muted), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0)
            };
            top.Children.Add(new StackPanel { Children = { q, t2 }, VerticalAlignment = VerticalAlignment.Center });
            int sec = installata ? SecondiRiavvio : 20;
            TextBlock conto = new TextBlock { FontStyle = FontStyle.Italic, Foreground = Pal.B(Pal.Fore), HorizontalAlignment = HorizontalAlignment.Right };
            ProgressBar pb = new ProgressBar { Minimum = 0, Maximum = sec, Value = 0, Height = 6, Margin = new Thickness(0, 6, 0, 0) };
            Action testo = () => conto.Text = installata ? Lang.F("Riavvio tra {0} s", sec) : Lang.F("Questo avviso si chiude tra {0} s", sec);
            testo();
            w.Content = new StackPanel
            {
                Children = { top, new Border { Background = Pal.B(Pal.Panel2), BorderBrush = Pal.B(Pal.Border), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(16, 10), Child = new StackPanel { Children = { conto, pb } } } }
            };
            Registro.Scrivi("AGGIORNAMENTO", installata ? "avviso: versione " + nuova + " pronta, riavvio tra " + sec + " s" : "avviso: versione " + nuova + " disponibile (Mac)");
            DispatcherTimer tm = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            tm.Tick += (s, e) =>
            {
                sec--; pb.Value = pb.Maximum - sec; testo();
                if (sec <= 0) { tm.Stop(); puoChiudere = true; w.Close(); }
            };
            tm.Start();
            await w.ShowDialog(this);
            avvisoAggAperto = false;
            if (installata) Riavvia();
        }

        static string FileRiapri { get { return System.IO.Path.Combine(AppPaths.DataDir, "aggiornamenti", "riapri.txt"); } }

        /// <summary>1.4.1: chiude e riapre YA-NEC dopo l'aggiornamento, senza domande. Il modello aperto si riapre; se non era salvato
        /// si riapre la sua copia salvata in data/aggiornamenti (e resta "da salvare").</summary>
        void Riavvia()
        {
            try
            {
                string exe = System.Environment.ProcessPath;
                if (string.IsNullOrEmpty(exe)) { Status("Riavvio non riuscito: chiudi e riapri YA-NEC a mano.", true); return; }
                try
                {
                    if (model != null && model.Geometry.Count > 0)
                    {
                        string dir = System.IO.Path.GetDirectoryName(FileRiapri) ?? "";
                        System.IO.Directory.CreateDirectory(dir);
                        if (ModelloNonSalvato() || string.IsNullOrEmpty(model.FileName) || !System.IO.File.Exists(model.FileName))
                        {
                            string copia = System.IO.Path.Combine(dir, "modello non salvato " + DateTime.Now.ToString("yyyy-MM-dd HH-mm") + ".nec");
                            System.IO.File.WriteAllText(copia, model.ToNec());
                            System.IO.File.WriteAllText(FileRiapri, copia + "\nnonsalvato\n" + (model.FileName ?? ""));
                            Registro.Scrivi("AGGIORNAMENTO", "modello non salvato copiato in " + copia);
                        }
                        else System.IO.File.WriteAllText(FileRiapri, model.FileName + "\nsalvato\n");
                    }
                }
                catch (System.Exception ex) { Registro.Scrivi("ERRORE", "copia del modello prima del riavvio: " + ex.Message); }
                Aggiornamento.Rilascia();
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = System.IO.Path.GetDirectoryName(exe) ?? "" });
                Registro.Scrivi("AZIONE", "riavvio del programma per l'aggiornamento");
                chiusuraConfermata = true;
                Close();
            }
            catch (System.Exception ex) { Status("Riavvio non riuscito (" + ex.Message + "): chiudi e riapri YA-NEC a mano.", true); }
        }

        /// <summary>1.4.1: all'avvio dopo un riavvio per l'aggiornamento riapre il modello che era aperto.</summary>
        void RiapriDopoRiavvio()
        {
            try
            {
                if (!System.IO.File.Exists(FileRiapri)) return;
                string[] r = System.IO.File.ReadAllLines(FileRiapri);
                System.IO.File.Delete(FileRiapri);
                if (r.Length < 2 || !System.IO.File.Exists(r[0])) return;
                LoadModelFile(r[0]);
                if (r[1] == "nonsalvato")
                {
                    model.FileName = r.Length > 2 ? r[2] : "";
                    testoSalvato = null;   // resta "da salvare"
                    Status(Lang.T("Riaperto dopo l'aggiornamento il modello che non era salvato: salvalo quando vuoi (copia in data/aggiornamenti)."));
                }
                else Status(Lang.T("Riaperto dopo l'aggiornamento il modello che era aperto."));
            }
            catch (System.Exception ex) { Registro.Scrivi("ERRORE", "riapertura dopo l'aggiornamento: " + ex.Message); }
        }
    }
}
