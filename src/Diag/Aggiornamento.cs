// Aggiornamento automatico silenzioso dal GitHub (richiesta di IC8TEM del 14/11/2026 08:59, confermata alle 09:03:
// "ti confermo per gli aggiornamenti automatici, eccetto per il debian, e lo spieghiamo nel readme e nel github").
//
// Come funziona:
//  - ogni 3 minuti (e 10 secondi dopo l'avvio; fino alla 1.4.0 ogni 30 minuti, il primo dopo 2) si legge l'ultima versione pubblicata: GET api.github.com/repos/<repo>/releases/latest,
//    senza chiave (repository pubblico; 20 richieste l'ora; dalla 1.7.2 con If-None-Match: la risposta 304 "nessuna novita'" non conta nel limite di 60 l'ora di GitHub);
//  - se la versione e' piu' nuova si scarica in background il pacchetto portatile della piattaforma (dalla 1.3.1 con i nomi dell'elenco
//    di YA-MS: <programma>-<versione>-windows-x64-portatile.zip, -linux-x64.tar.gz, -alpine-x64.tar.gz...; fino alla 1.3.0
//    "<programma>-<versione>-<rid>.zip", che le release continuano a pubblicare per le versioni vecchie) e la sua impronta "<pacchetto>.sha256"; senza impronta, o con impronta diversa, non si
//    installa niente;
//  - 1.7.2 (controllo di sicurezza del 26/11/2026, IC8TEM: "correggi compila e pubblica"): il pacchetto deve avere anche la firma
//    "<pacchetto>.sig" (ECDSA P-256 con SHA-256, base64), fatta nella release con la chiave privata che sta nei segreti di GitHub;
//    la chiave pubblica e' qui sotto. Senza firma, o con firma sbagliata, non si installa niente: un pacchetto messo nella release
//    da altri (con la sua impronta) non basta piu'. Download con tempo massimo di 10 minuti e al massimo 400 MB;
//  - i file nuovi si mettono al posto dei vecchi: un programma in uso non si puo' sovrascrivere, ma si puo' rinominare, quindi il file
//    vecchio diventa "<nome>.old" e quello nuovo prende il suo posto; il programma che sta girando non cambia, la versione nuova parte
//    al prossimo avvio, che cancella i .old e scrive nella barra di stato "aggiornato da ... a ...";
//  - copia della versione precedente in data/aggiornamenti/precedente-<versione> (per tornare indietro a mano);
//  - non si toccano data/config.ini, i registri e i modelli dell'utente che non sono nel pacchetto;
//  - spento durante il collaudo e le prove automatiche (la versione non deve cambiare a meta' prova);
//  - pacchetti Debian/Ubuntu (.deb) e .rpm: installati dal gestore dei pacchetti, con i permessi di amministratore, non si aggiornano
//    da soli; il programma lo dice nelle Impostazioni (si aggiorna con apt / dnf). Si riconoscono dal file "installato-da-pacchetto"
//    accanto al programma (messo dal pacchetto) o dalla cartella non scrivibile.
// Ogni passo va nel registro (categoria AGGIORNAMENTO).
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace YaNec
{
    public static class Aggiornamento
    {
        public const int IntervalloMinuti = 3;   // 1.4.1 (IC8TEM 18/11/2026 16:27, "aggiornamento ogni 3 minuti"; prima 30)
        /// <summary>Repository GitHub "utente/nome" (scelto da IC8TEM il 14/11/2026: ic8tem/YA-NEC; si cambia nelle Impostazioni o in
        /// data/config.ini, chiave aggiornamento_repo).</summary>
        public const string RepoPredefinito = "ic8tem/YA-NEC";

        public static string Stato { get; private set; } = "non ancora controllato";
        public static event Action<string> Cambiato;
        /// <summary>1.4.1 (IC8TEM 18/11/2026 16:18, "poi con un popup avvisa"): versione nuova installata (true) o solo disponibile (Mac, false).</summary>
        public static event Action<string, bool> Pronta;
        static string avvisata = "";
        /// <summary>1.7.2: chiave pubblica (ECDSA P-256, SubjectPublicKeyInfo in base64) delle firme dei pacchetti di aggiornamento.</summary>
        const string ChiavePubblica = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEI08uh0Jh0MEWBXV2y9zYZ4PL4oAna6zbpb6XgNfBREcJUPYNZw+x5SEtVwK6Fwyg6kd1E5KkC2W7S/hqs37OAw==";
        /// <summary>1.7.2: messaggio e firma di prova per l'autotest (controlla che la verifica funzioni su ogni sistema).</summary>
        public const string ProvaMessaggio = "YA-NEC firma di prova 1.7.2\n", ProvaFirma = "MEUCIG99uUanLYOL7avxl0Ch8trflrdQmk+2WA9VLC8FACx8AiEAtkExua/A0jznvWZ/e4Aibi59VQtGrAfl9p009FXiJYo=";
        const long MaxPacchetto = 400L * 1024 * 1024;
        static string etag, ultimoJson;   // 1.7.2: richiesta condizionale a GitHub

        /// <summary>1.7.2: vero se la firma (base64, DER) e' quella della chiave di YA-NEC sui dati.</summary>
        public static bool FirmaValida(Stream dati, string firmaB64)
        {
            try
            {
                byte[] firma = Convert.FromBase64String((firmaB64 ?? "").Trim());
                using (ECDsa ec = ECDsa.Create())
                {
                    ec.ImportSubjectPublicKeyInfo(Convert.FromBase64String(ChiavePubblica), out _);
                    return ec.VerifyData(dati, firma, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
                }
            }
            catch { return false; }
        }
        public static bool FirmaValida(byte[] dati, string firmaB64) { using (MemoryStream ms = new MemoryStream(dati)) return FirmaValida(ms, firmaB64); }
        static Timer timer;
        static Mutex unica;   // 0.5.0: tenuto vivo finche' la finestra e' aperta
        static Config cfg;
        static int inCorso;

        // 0.4.19: un valore vuoto rimasto in config.ini dalle versioni precedenti vale come "repository predefinito"
        public static string Repository { get { string r = cfg == null ? "" : (cfg.Get("aggiornamento_repo", RepoPredefinito) ?? "").Trim(); return r.Length == 0 ? RepoPredefinito : r; } }
        public static bool Attivo { get { return cfg == null || cfg.Get("aggiornamento_auto", "1") != "0"; } }

        static void ImpostaStato(string s, bool registro = true)
        {
            Stato = s;
            if (registro) Registro.Scrivi("AGGIORNAMENTO", s);
            try { Cambiato?.Invoke(s); } catch { }
        }

        /// <summary>Da chiamare all'avvio del programma (anche nelle prove): cancella i file .old e restituisce il messaggio
        /// "aggiornato da ... a ..." se l'ultimo aggiornamento e' appena entrato in funzione, altrimenti null.</summary>
        public static string PulisciDopoAvvio()
        {
            string msg = null;
            try
            {
                foreach (string f in Directory.GetFiles(AppPaths.AppDir, "*.old", SearchOption.AllDirectories))
                    try { File.Delete(f); } catch { }
                string fatto = Path.Combine(AppPaths.DataDir, "aggiornamenti", "applicato.txt");
                if (File.Exists(fatto))
                {
                    msg = File.ReadAllText(fatto).Trim();
                    File.Delete(fatto);
                    Registro.Scrivi("AGGIORNAMENTO", "in funzione: " + msg);
                }
            }
            catch { }
            return msg;
        }

        /// <summary>Installato da un pacchetto Debian/RPM (o cartella non scrivibile): niente aggiornamento automatico.</summary>
        public static bool DaPacchetto(out string motivo)
        {
            motivo = null;
            if (File.Exists(Path.Combine(AppPaths.AppDir, "installato-da-pacchetto")))
            {
                motivo = "installato con un pacchetto (.deb, .rpm, Arch, Slackware, AppImage o .dmg): si aggiorna con il gestore dei pacchetti (apt, dnf, pacman, slackpkg) " +
                    "o scaricando il nuovo AppImage o .dmg, non da solo";
                return true;
            }
            try
            {
                string prova = Path.Combine(AppPaths.AppDir, ".prova_scrittura_" + Environment.ProcessId);
                File.WriteAllText(prova, "");
                File.Delete(prova);
            }
            catch
            {
                motivo = "la cartella del programma non e' scrivibile (installazione di sistema): aggiornare a mano o con il gestore dei pacchetti";
                return true;
            }
            return false;
        }

        public static void Avvia(Config c)
        {
            cfg = c;
            string motivo;
            if (DaPacchetto(out motivo)) { ImpostaStato("spento: " + motivo); return; }
            if (!Attivo) { ImpostaStato("spento nelle Impostazioni"); return; }
            if (Repository.Length == 0) { ImpostaStato("repository GitHub non ancora scelto (Impostazioni > Aggiornamento)", false); }
            // 0.5.0: con piu' finestre aperte insieme solo la prima controlla e installa gli aggiornamenti (due installazioni
            // contemporanee sugli stessi file non devono succedere); le altre lo dicono nello stato
            try
            {
                bool primo;
                unica = new Mutex(true, "YA-NEC-aggiornamento-" + AppPaths.AppName, out primo);
                if (!primo) { ImpostaStato("gestito da un'altra finestra di " + AppPaths.AppName + " gia' aperta"); return; }
            }
            catch { }
            // 1.4.1: primo controllo 10 secondi dopo l'avvio (prima 2 minuti: con le aperture brevi non partiva mai), poi ogni 3 minuti
            timer = new Timer(_ => { var t = Controlla(); }, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(IntervalloMinuti));
        }

        public static void Ferma() { try { timer?.Dispose(); } catch { } timer = null; }

        /// <summary>1.4.1: prima di riavviare il programma si libera il controllo degli aggiornamenti, cosi' lo prende la finestra nuova.</summary>
        public static void Rilascia()
        {
            Ferma();
            try { unica?.ReleaseMutex(); } catch { }
            try { unica?.Dispose(); } catch { }
            unica = null;
        }

        static void AvvisaPronta(string nuova, bool installata)
        {
            if (avvisata == nuova) return;
            avvisata = nuova;
            try { Pronta?.Invoke(nuova, installata); } catch { }
        }

        static string Rid()
        {
#if WINDOWS7
            // 0.5.3: la versione per Windows 7 (.NET 6) si aggiorna con il suo pacchetto, YA-NEC-<versione>-win7-x64.zip (o -win7-x86)
            return RuntimeInformation.ProcessArchitecture == Architecture.X86 ? "win7-x86" : "win7-x64";
#else
            try { return RuntimeInformation.RuntimeIdentifier; } catch { return "win-x64"; }
#endif
        }

        /// <summary>1.3.1 (richiesta di IC8TEM del 18/11/2026 10:57, "armonizzi la distribuzione a questo elenco?", elenco di YA-MS):
        /// nome del pacchetto portatile per questa piattaforma con i nomi nuovi; null sul Mac (si aggiorna con il nuovo .dmg).</summary>
        static string NomePacchetto(Version v)
        {
            string p = AppPaths.AppName + "-" + v + "-";
            switch (Rid())
            {
                case "win-x64": return p + "windows-x64-portatile.zip";
                case "win-x86": return p + "windows-x86-portatile.zip";
                case "win-arm64": return p + "windows-arm64-portatile.zip";
                case "win7-x64": return p + "windows7-x64-portatile.zip";
                case "win7-x86": return p + "windows7-x86-portatile.zip";
                case "linux-x64": return p + "linux-x64.tar.gz";
                case "linux-arm64": return p + "linux-arm64.tar.gz";
                case "linux-arm": return p + "linux-armv7l.tar.gz";
                case "linux-musl-x64": return p + "alpine-x64.tar.gz";
                case "linux-musl-arm64": return p + "alpine-arm64.tar.gz";
            }
            return null;
        }

        static Version Ver(string s)
        {
            s = (s ?? "").Trim().TrimStart('v', 'V');
            int i = s.IndexOfAny(new[] { '-', '+' });
            if (i > 0) s = s.Substring(0, i);
            Version v;
            return Version.TryParse(s, out v) ? v : null;
        }

        /// <summary>Un controllo (chiamato dal timer ogni 3 minuti; anche dal pulsante "Controlla adesso").</summary>
        public static async Task Controlla() { await Controlla(false, false); }

        /// <summary>1.5: a mano = dal pulsante "Forza l'aggiornamento" (vale anche con l'automatico spento); reinstalla = anche la stessa versione.</summary>
        public static async Task Controlla(bool aMano, bool reinstalla)
        {
            if (MainWindow.ProveAutomatiche || (!Attivo && !aMano) || cfg == null) return;
            if (aMano) { string mp; if (DaPacchetto(out mp)) { ImpostaStato("installato con un pacchetto: si aggiorna con il gestore dei pacchetti (su Debian con apt)", false); return; } }
            string repo = Repository;
            if (repo.Length == 0 || repo.IndexOf('/') <= 0) { ImpostaStato("repository GitHub non ancora scelto (Impostazioni > Aggiornamento)", false); return; }
            if (Interlocked.Exchange(ref inCorso, 1) == 1) return;
            try
            {
                using (HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd(AppPaths.AppName + "/" + AppPaths.Version);
                    http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                    if (!string.Equals(repo, RepoPredefinito, StringComparison.OrdinalIgnoreCase))
                        Registro.Scrivi("AGGIORNAMENTO", "attenzione: il repository dell'aggiornamento non e' " + RepoPredefinito + " ma " + repo + " (Impostazioni > Aggiornamento)");
                    string json;
                    using (HttpRequestMessage rq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/" + repo + "/releases/latest"))
                    {
                        if (etag != null && ultimoJson != null && !reinstalla) rq.Headers.TryAddWithoutValidation("If-None-Match", etag);
                        using (HttpResponseMessage rs = await http.SendAsync(rq))
                        {
                            if (rs.StatusCode == System.Net.HttpStatusCode.NotModified) json = ultimoJson;
                            else
                            {
                                rs.EnsureSuccessStatusCode();
                                json = await rs.Content.ReadAsStringAsync();
                                etag = rs.Headers.ETag != null ? rs.Headers.ETag.ToString() : null;
                                ultimoJson = json;
                            }
                        }
                    }
                    string tag; List<KeyValuePair<string, string>> asset = new List<KeyValuePair<string, string>>();
                    using (JsonDocument d = JsonDocument.Parse(json))
                    {
                        tag = d.RootElement.GetProperty("tag_name").GetString();
                        foreach (JsonElement a in d.RootElement.GetProperty("assets").EnumerateArray())
                            asset.Add(new KeyValuePair<string, string>(a.GetProperty("name").GetString(), a.GetProperty("browser_download_url").GetString()));
                    }
                    Version nuova = Ver(tag), attuale = Ver(AppPaths.Version);
                    if (nuova == null || attuale == null) { ImpostaStato("versione pubblicata non leggibile: " + tag); return; }
                    if (nuova < attuale || (nuova == attuale && !reinstalla))
                    {
                        ImpostaStato(aMano ? Lang.F("hai gia' l'ultima versione ({0})", attuale) : Lang.F("aggiornato: l'ultima versione pubblicata e' la {0} ({1})", nuova, DateTime.Now.ToString("HH:mm")), false);   // 1.5.2: tradotti
                        return;
                    }
                    if (nuova == attuale) ImpostaStato("reinstallo la versione " + nuova);
                    string nomeZip = NomePacchetto(nuova);
                    if (nomeZip == null)
                    {
                        // Mac: dal 1.3.1 il pacchetto e' YA-NEC.app (nel .dmg o nello zip -app): non si sostituisce da solo
                        ImpostaStato("versione " + nuova + " disponibile: scarica il .dmg (" + AppPaths.AppName + "-" + nuova + "-mac-" +
                            (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "apple-silicon" : "intel") + ".dmg) da github.com/" + repo + "/releases");
                        AvvisaPronta(nuova.ToString(), false);
                        return;
                    }
                    string urlZip = null, urlSha = null;
                    foreach (var a in asset)
                    {
                        if (a.Key.Equals(nomeZip, StringComparison.OrdinalIgnoreCase)) urlZip = a.Value;
                        if (a.Key.Equals(nomeZip + ".sha256", StringComparison.OrdinalIgnoreCase)) urlSha = a.Value;
                    }
                    if (urlZip == null) { ImpostaStato("versione " + nuova + " pubblicata, ma senza il pacchetto " + nomeZip + " per questa piattaforma"); return; }
                    if (urlSha == null) { ImpostaStato("versione " + nuova + ": manca l'impronta " + nomeZip + ".sha256, non si installa"); return; }
                    string urlSig = null;
                    foreach (var a in asset) if (a.Key.Equals(nomeZip + ".sig", StringComparison.OrdinalIgnoreCase)) urlSig = a.Value;
                    if (urlSig == null) { ImpostaStato("versione " + nuova + ": manca la firma " + nomeZip + ".sig, non si installa"); return; }
                    string dir = Path.Combine(AppPaths.DataDir, "aggiornamenti");
                    Directory.CreateDirectory(dir);
                    string zip = Path.Combine(dir, nomeZip);
                    ImpostaStato("scarico la versione " + nuova + " (" + nomeZip + ")");
                    using (CancellationTokenSource tempo = new CancellationTokenSource(TimeSpan.FromMinutes(10)))
                    {
                        using (HttpResponseMessage rs = await http.GetAsync(urlZip, HttpCompletionOption.ResponseHeadersRead, tempo.Token))
                        {
                            rs.EnsureSuccessStatusCode();
                            if (rs.Content.Headers.ContentLength > MaxPacchetto) { ImpostaStato("versione " + nuova + ": pacchetto troppo grande, non si scarica"); return; }
                            using (Stream s = await rs.Content.ReadAsStreamAsync(tempo.Token)) using (FileStream fs = File.Create(zip))
                            {
                                byte[] buf = new byte[81920]; long tot = 0; int n;
                                while ((n = await s.ReadAsync(buf, 0, buf.Length, tempo.Token)) > 0)
                                {
                                    tot += n;
                                    if (tot > MaxPacchetto) throw new IOException("pacchetto oltre " + (MaxPacchetto >> 20) + " MB");
                                    await fs.WriteAsync(buf, 0, n, tempo.Token);
                                }
                            }
                        }
                    }
                    string attesa = (await http.GetStringAsync(urlSha)).Trim().Split(' ', '\t', '\r', '\n')[0].ToLowerInvariant();
                    string calcolata;
                    using (FileStream fs = File.OpenRead(zip)) using (SHA256 sha = SHA256.Create()) calcolata = Convert.ToHexString(sha.ComputeHash(fs)).ToLowerInvariant();
                    if (calcolata != attesa) { ImpostaStato("versione " + nuova + ": impronta SHA-256 diversa da quella pubblicata, pacchetto scartato"); try { File.Delete(zip); } catch { } return; }
                    string firma = await http.GetStringAsync(urlSig);
                    bool firmato;
                    using (FileStream fs = File.OpenRead(zip)) firmato = FirmaValida(fs, firma);
                    if (!firmato) { ImpostaStato("versione " + nuova + ": firma del pacchetto non valida, pacchetto scartato"); Registro.Scrivi("AGGIORNAMENTO", "FIRMA NON VALIDA per " + nomeZip + ": installazione rifiutata"); try { File.Delete(zip); } catch { } return; }
                    if (MainWindow.ProveAutomatiche) return;
                    Installa(zip, attuale.ToString(), nuova.ToString());
                }
            }
            // registro di IC8TEM del 14/11/2026 17:50: "404 (Not Found)" quando il repository non ha ancora una release pubblicata
            // (o e' privato): non e' un errore del programma, lo si dice in chiaro
            catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            { ImpostaStato("nessuna versione pubblicata su github.com/" + Repository + " (nessuna release, o repository privato): niente da aggiornare"); }
            catch (Exception ex) { ImpostaStato("controllo non riuscito: " + ex.Message + (aMano && Attivo ? " (riprovo da solo fra " + IntervalloMinuti + " minuti)" : "")); }
            finally { Interlocked.Exchange(ref inCorso, 0); }
        }

        static bool DaNonToccare(string rel)
        {
            string r = rel.Replace('\\', '/').ToLowerInvariant();
            return r == "data/config.ini" || r.StartsWith("registri/") || r.StartsWith("data/aggiornamenti/") || r.StartsWith("data/collaudo_immagini/")
                || r == "installato-da-pacchetto";
        }

        static void Installa(string zip, string vecchia, string nuova)
        {
            string dir = Path.GetDirectoryName(zip);
            string est = Path.Combine(dir, nuova);
            if (Directory.Exists(est)) Directory.Delete(est, true);
#if WINDOWS7
            ZipFile.ExtractToDirectory(zip, est);
#else
            if (zip.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                // Linux (1.3.1): tar.gz, che conserva i permessi di esecuzione
                Directory.CreateDirectory(est);
                using (FileStream fs = File.OpenRead(zip)) using (GZipStream gz = new GZipStream(fs, CompressionMode.Decompress))
                    System.Formats.Tar.TarFile.ExtractToDirectory(gz, est, true);
            }
            else ZipFile.ExtractToDirectory(zip, est);
#endif
            // se lo zip ha una sola cartella principale (YA-NEC/...), si parte da li'
            string radice = est;
            string[] dirs = Directory.GetDirectories(est), files = Directory.GetFiles(est);
            if (files.Length == 0 && dirs.Length == 1) radice = dirs[0];
            string precedente = Path.Combine(dir, "precedente-" + vecchia);
            List<string> rinominati = new List<string>();
            List<string> nuovi = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(radice, "*", SearchOption.AllDirectories))
                {
                    string rel = Path.GetRelativePath(radice, f);
                    if (DaNonToccare(rel)) continue;
                    string dest = Path.Combine(AppPaths.AppDir, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    if (File.Exists(dest))
                    {
                        string bk = Path.Combine(precedente, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(bk));
                        File.Copy(dest, bk, true);
                        string old = dest + ".old";
                        if (File.Exists(old)) File.Delete(old);
                        File.Move(dest, old);
                        rinominati.Add(dest);
                    }
                    else nuovi.Add(dest);
                    File.Copy(f, dest, false);
                }
                File.WriteAllText(Path.Combine(dir, "applicato.txt"), "aggiornato da " + vecchia + " a " + nuova + " il " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") +
                    " (copia della " + vecchia + " in data/aggiornamenti/precedente-" + vecchia + ")");
                ImpostaStato("versione " + nuova + " installata: entra in funzione al prossimo avvio del programma");
                Ferma();
                AvvisaPronta(nuova, true);
            }
            catch (Exception ex)
            {
                // si torna come prima: i file nuovi si tolgono, i vecchi riprendono il loro nome
                foreach (string d in nuovi) try { File.Delete(d); } catch { }
                foreach (string d in rinominati) try { if (File.Exists(d)) File.Delete(d); File.Move(d + ".old", d); } catch { }
                ImpostaStato("installazione della " + nuova + " non riuscita, rimessa la " + vecchia + ": " + ex.Message);
            }
            finally { try { Directory.Delete(est, true); } catch { } }
        }
    }
}
