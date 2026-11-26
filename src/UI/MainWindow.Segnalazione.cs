// YA-NEC 1.5 - Impostazioni > Segnala un problema (bozzetto "Aggiornamento a richiesta, segnalazione dei problemi, stack su due
// frequenze" approvato da IC8TEM il 18/11/2026 19:35). Prepara uno zip in data/segnalazioni con descrizione, versione, sistema,
// modello e registro di oggi, e apre la pagina "nuova segnalazione" del GitHub del programma. Non invia nulla da solo (il 14/11/2026
// IC8TEM aveva scartato l'invio automatico degli errori): niente IP, niente e-mail, percorso della cartella personale sostituito con "~".
// GPL-3.0.
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using YaNec.Engine;

namespace YaNec
{
    public partial class MainWindow
    {
        string ultimaSegnalazione;

        Control PannelloSegnalazione()
        {
            StackPanel sp = new StackPanel { Spacing = 4 };
            sp.Children.Add(new TextBlock { Text = "Cosa e' successo (cosa facevi e cosa ti aspettavi)", Foreground = Pal.B(Pal.Muted), FontSize = 12 });
            TextBox tb = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 90, Watermark = "Apro il modello e il diagramma 3D resta vuoto." };
            sp.Children.Add(tb);
            CheckBox cMod = new CheckBox { Content = "Allega il modello aperto", IsChecked = true };
            CheckBox cReg = new CheckBox { Content = "Allega il registro di oggi", IsChecked = true };
            ToolTip.SetTip(cMod, "Mette nello zip il modello aperto (.nec), anche se non e' salvato.");
            ToolTip.SetTip(cReg, "Mette nello zip il registro di oggi della cartella registri.");
            sp.Children.Add(cMod); sp.Children.Add(cReg);
            TextBlock esito = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Muted), FontSize = 12 };
            Button bPrep = Btn("Prepara la segnalazione", true), bCart = Btn("Apri la cartella", false);
            ToolTip.SetTip(bPrep, "Crea lo zip in data/segnalazioni e apre la pagina «Nuova segnalazione» del GitHub con il testo gia' scritto. Lo zip lo alleghi tu, a mano: il programma non invia nulla da solo.");
            bPrep.Click += (s, e) =>
            {
                try
                {
                    string zip = PreparaSegnalazione(tb.Text ?? "", cMod.IsChecked == true, cReg.IsChecked == true);
                    ultimaSegnalazione = zip;
                    esito.Text = "Pronta: " + Path.GetFileName(zip) + " in data/segnalazioni. Allegala alla pagina di GitHub che si e' aperta (o mandala come preferisci).";
                    ApriSegnalazioneGitHub(tb.Text ?? "");
                    Registro.Scrivi("AZIONE", "segnalazione preparata: " + Path.GetFileName(zip));
                }
                catch (Exception ex) { esito.Text = "Segnalazione non preparata: " + ex.Message; }
            };
            bCart.Click += (s, e) => ApriCartella(Path.Combine(AppPaths.DataDir, "segnalazioni"));
            StackPanel b = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
            b.Children.Add(bPrep); b.Children.Add(bCart);
            sp.Children.Add(b);
            sp.Children.Add(esito);
            sp.Children.Add(new TextBlock { Text = "Niente indirizzo IP, niente e-mail, niente nomi utente nei file: il percorso della cartella personale diventa «~». Senza account GitHub puoi mandare lo zip come preferisci.",
                TextWrapping = TextWrapping.Wrap, Foreground = Pal.B(Pal.Yellow), FontSize = 12 });
            return sp;
        }

        /// <summary>Toglie dal testo i dati personali che i file possono contenere: cartella personale e nome utente.</summary>
        static string Anonimo(string t)
        {
            if (string.IsNullOrEmpty(t)) return t ?? "";
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            // 1.7.2: su Windows i percorsi non distinguono maiuscole e minuscole (c:\users\nome = C:\Users\Nome)
            StringComparison cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!string.IsNullOrEmpty(home)) t = t.Replace(home, "~", cmp);
            string u = Environment.UserName;
            if (!string.IsNullOrEmpty(u) && u.Length > 2) t = t.Replace(u, "utente", cmp);
            return t;
        }

        static string Sistema()
        {
            long ram = 0;
            try { ram = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes; } catch { }
            return AppPaths.AppName + " " + AppPaths.Version + ", " + RuntimeInformation.OSDescription + ", " + RuntimeInformation.OSArchitecture +
                ", .NET " + Environment.Version + (ram > 0 ? ", RAM " + Math.Round(ram / 1073741824.0) + " GB" : "");
        }

        string PreparaSegnalazione(string descrizione, bool modello, bool registro)
        {
            string dir = Path.Combine(AppPaths.DataDir, "segnalazioni");
            Directory.CreateDirectory(dir);
            string zip = Path.Combine(dir, "segnalazione-" + AppPaths.Version + "-" + DateTime.Now.ToString("yyyy-MM-dd-HHmm") + ".zip");
            if (File.Exists(zip)) File.Delete(zip);
            using (ZipArchive z = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Action<string, string> metti = (nome, testo) =>
                {
                    ZipArchiveEntry en = z.CreateEntry(nome);
                    using (StreamWriter w = new StreamWriter(en.Open(), new UTF8Encoding(false))) w.Write(testo);
                };
                metti("descrizione.txt", Anonimo(descrizione));
                metti("sistema.txt", Anonimo(Sistema()));
                if (modello && model != null && model.Geometry.Count > 0) metti("modello.nec", Anonimo(model.ToNec()));
                if (registro && Registro.File_ != null && File.Exists(Registro.File_))
                {
                    string t;
                    using (FileStream fs = new FileStream(Registro.File_, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) using (StreamReader r = new StreamReader(fs)) t = r.ReadToEnd();
                    metti("registro-oggi.txt", Anonimo(t));
                }
            }
            return zip;
        }

        void ApriSegnalazioneGitHub(string descrizione)
        {
            string repo = Aggiornamento.Repository;
            if (string.IsNullOrEmpty(repo) || repo.IndexOf('/') <= 0) return;
            string corpo = Anonimo(descrizione).Trim();
            if (corpo.Length > 1500) corpo = corpo.Substring(0, 1500) + "...";
            corpo += "\n\n---\n" + Anonimo(Sistema()) + "\n(allegare lo zip della segnalazione preparato da YA-NEC)";
            string titolo = corpo.Split('\n')[0];
            if (titolo.Length > 80) titolo = titolo.Substring(0, 80);
            string url = "https://github.com/" + repo + "/issues/new?title=" + Uri.EscapeDataString(titolo) + "&body=" + Uri.EscapeDataString(corpo);
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        void ApriCartella(string d)
        {
            try
            {
                Directory.CreateDirectory(d);
                if (OperatingSystem.IsWindows()) Process.Start(new ProcessStartInfo("explorer.exe", "\"" + d + "\"") { UseShellExecute = false });
                else if (OperatingSystem.IsMacOS()) Process.Start("open", "\"" + d + "\"");
                else Process.Start("xdg-open", "\"" + d + "\"");
            }
            catch (Exception ex) { Status("Cartella: " + d + " (non aperta: " + ex.Message + ")", true); }
        }
    }
}
