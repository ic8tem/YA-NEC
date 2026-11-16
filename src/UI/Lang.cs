// YA-NEC 0.5.3 - traduzione dell'interfaccia (stesso sistema di YA-MS, E-LAYER ANALYZER e LIVETROPOVHF, le stesse 36 lingue;
// IC8TEM 15/11/2026 20:20 "prepara anche le traduzioni", 21:03 "come ip-trx-ctrl"). L'italiano e' la lingua di partenza: le chiavi sono i testi italiani.
// File di traduzione: data/lingue/<codice>.txt (UTF-8), una voce per riga: <chiave italiana><TAB><traduzione>.
// Le righe che iniziano con # sono commenti; in entrambe le colonne la sequenza \n indica un a capo.
// Voce o file mancante: si mostra il testo italiano.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace YaNec
{
    public static class Lang
    {
        public static string Code = "it";

        // codice, nome nella lingua stessa
        public static readonly string[][] Languages =
        {
            new[] { "it", "Italiano" },
            new[] { "en", "English" },
            new[] { "de", "Deutsch" },
            new[] { "fr", "Français" },
            new[] { "es", "Español" },
            new[] { "pt", "Português" },
            new[] { "pt-br", "Português (Brasil)" },
            new[] { "nl", "Nederlands" },
            new[] { "sv", "Svenska" },
            new[] { "nb", "Norsk (bokmål)" },
            new[] { "da", "Dansk" },
            new[] { "is", "Íslenska" },
            new[] { "et", "Eesti" },
            new[] { "lv", "Latviešu" },
            new[] { "lt", "Lietuvių" },
            new[] { "pl", "Polski" },
            new[] { "cs", "Čeština" },
            new[] { "hu", "Magyar" },
            new[] { "ro", "Română" },
            new[] { "el", "Ελληνικά" },
            new[] { "ru", "Русский" },
            new[] { "uk", "Українська" },
            new[] { "tr", "Türkçe" },
            new[] { "af", "Afrikaans" },
            new[] { "ar", "العربية" },
            new[] { "he", "עברית" },
            new[] { "ur", "اردو" },
            new[] { "hi", "हिन्दी" },
            new[] { "bn", "বাংলা" },
            new[] { "id", "Bahasa Indonesia" },
            new[] { "tl", "Tagalog" },
            new[] { "zh", "中文 (简体)" },
            new[] { "zh-tw", "中文 (繁體)" },
            new[] { "th", "ไทย" },
            new[] { "ja", "日本語" },
            new[] { "ko", "한국어" },
            new[] { "sk", "Slovenčina" }   // 0.5.3: la 37a lingua, come YAEC
        };

        // cultura per numeri e date di ogni lingua
        static readonly string[][] Cultures =
        {
            new[] { "it", "it-IT" },
            new[] { "en", "en-GB" },
            new[] { "de", "de-DE" },
            new[] { "fr", "fr-FR" },
            new[] { "es", "es-ES" },
            new[] { "pt", "pt-PT" },
            new[] { "pt-br", "pt-BR" },
            new[] { "nl", "nl-NL" },
            new[] { "sv", "sv-SE" },
            new[] { "nb", "nb-NO" },
            new[] { "da", "da-DK" },
            new[] { "is", "is-IS" },
            new[] { "et", "et-EE" },
            new[] { "lv", "lv-LV" },
            new[] { "lt", "lt-LT" },
            new[] { "pl", "pl-PL" },
            new[] { "cs", "cs-CZ" },
            new[] { "hu", "hu-HU" },
            new[] { "ro", "ro-RO" },
            new[] { "el", "el-GR" },
            new[] { "ru", "ru-RU" },
            new[] { "uk", "uk-UA" },
            new[] { "tr", "tr-TR" },
            new[] { "af", "af-ZA" },
            new[] { "ar", "ar-EG" },
            new[] { "he", "he-IL" },
            new[] { "ur", "ur-PK" },
            new[] { "hi", "hi-IN" },
            new[] { "bn", "bn-BD" },
            new[] { "id", "id-ID" },
            new[] { "tl", "fil-PH" },
            new[] { "zh", "zh-CN" },
            new[] { "zh-tw", "zh-TW" },
            new[] { "th", "th-TH" },
            new[] { "ja", "ja-JP" },
            new[] { "ko", "ko-KR" },
            new[] { "sk", "sk-SK" }
        };

        // il dizionario viene sostituito per intero (lettura sicura anche dai thread secondari)
        static Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);

        public static int IndexOf(string code)
        {
            for (int i = 0; i < Languages.Length; i++) if (string.Equals(Languages[i][0], code, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        // codice canonico (pt-BR, zh-TW con le maiuscole giuste) o null
        static string Canonical(string code) { int i = IndexOf(code); return i < 0 ? null : Languages[i][0]; }

        // Lingua all'avvio: quella salvata in config.ini (chiave "lingua"); al primo avvio quella del sistema, se prevista, altrimenti l'italiano
        public static void Init(string saved)
        {
            string code = Canonical((saved ?? "").Trim());
            if (code == null)
            {
                code = "it";
                try
                {
                    CultureInfo ui = CultureInfo.CurrentUICulture;
                    string name = ui.Name, os = ui.TwoLetterISOLanguageName.ToLowerInvariant();
                    // varianti: portoghese del Brasile, cinese tradizionale (Taiwan, Hong Kong, Macao, Hant)
                    if (name.StartsWith("pt-BR", StringComparison.OrdinalIgnoreCase)) os = "pt-br";
                    else if (os == "zh" && (name.Contains("TW") || name.Contains("HK") || name.Contains("MO") || name.Contains("Hant"))) os = "zh-tw";
                    if (Canonical(os) != null) code = Canonical(os);
                }
                catch { }
            }
            Load(code);
            ApplyCulture();
        }

        // Carica la lingua (per "it" nessun file: si usano le chiavi)
        public static void Load(string code)
        {
            code = Canonical(code) ?? "it";
            Dictionary<string, string> m = new Dictionary<string, string>(StringComparer.Ordinal);
            if (code != "it")
            {
                foreach (string file in new[] { FindFile(code) })
                if (file != null)
                {
                    try
                    {
                        foreach (string raw in File.ReadAllLines(file, Encoding.UTF8))
                        {
                            string l = raw.TrimEnd('\r');
                            if (l.Length > 0 && l[0] == '﻿') l = l.Substring(1);
                            if (l.Length == 0 || l.StartsWith("#")) continue;
                            int tab = l.IndexOf('\t');
                            if (tab <= 0) continue;
                            string key = Unescape(l.Substring(0, tab));
                            string val = Unescape(l.Substring(tab + 1));
                            if (val.Trim().Length == 0) continue;   // traduzione vuota: resta l'italiano
                            m[key] = val;
                        }
                    }
                    catch { }
                }
            }
            map = m;
            Code = code;
        }

        static string FindFile(string code)
        {
            string[] dirs =
            {
                Path.Combine(AppPaths.DataDir, "lingue"),
                Path.Combine(AppContext.BaseDirectory, "data", "lingue"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "data", "lingue"))
            };
            foreach (string d in dirs)
            {
                try
                {
                    string f = Path.Combine(d, code + ".txt");
                    if (File.Exists(f)) return f;
                }
                catch { }
            }
            return null;
        }

        // la sequenza di due caratteri \n diventa un a capo
        public static string Unescape(string s) { return s.Replace("\\n", "\n"); }
        public static string Escape(string s) { return s.Replace("\r\n", "\n").Replace("\n", "\\n"); }

        // Testo tradotto (o l'italiano se manca la traduzione)
        public static string T(string it)
        {
            if (string.IsNullOrEmpty(it)) return it;
            string t;
            Dictionary<string, string> m = map;
            return m.TryGetValue(it, out t) ? t : it;
        }

        // Testo tradotto con segnaposto {0}, {1}, ... (numeri e date secondo la cultura della lingua scelta)
        public static string F(string it, params object[] args)
        {
            string t = T(it);
            try { return string.Format(CultureInfo.CurrentCulture, t, args); }
            catch (FormatException)
            {
                // traduzione con segnaposto sbagliati: si usa il testo italiano
                try { return string.Format(CultureInfo.CurrentCulture, it, args); } catch (FormatException) { return it; }
            }
        }

        // Cultura dell'interfaccia della lingua scelta (anche per i thread secondari).
        public static void ApplyCulture()
        {
            string name = "it-IT";
            foreach (string[] c in Cultures) if (c[0] == Code) name = c[1];
            try
            {
                CultureInfo ci = CultureInfo.GetCultureInfo(name);
                // YA-NEC: cambia solo la cultura dell'interfaccia; i numeri restano nel formato di prima (molte caselle e file
                // si leggono con la virgola o il punto: cambiare anche CurrentCulture spezzerebbe la lettura)
                CultureInfo.CurrentUICulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;
            }
            catch { }
        }

        // ------------------------------------------------------------------------------------------------
        // YA-NEC: traduzione automatica delle scritte dell'interfaccia. Invece di cambiare migliaia di righe, il programma traduce ogni
        // testo nel momento in cui una casella, un pulsante, un fumetto o un titolo lo riceve: se il testo italiano e' una chiave del file
        // della lingua, al suo posto va la traduzione. I testi composti con numeri (stato, risultati) restano in italiano finche' non
        // vengono scritti con Lang.F. I disegni (grafici, diagrammi) passano da Draw.Ft, che traduce anch'esso.
        [ThreadStatic] static bool inTraduzione;
        static bool installata;

        public static bool Attiva { get { return Code != "it" && map.Count > 0; } }

        public static void Installa()
        {
            if (installata) return;
            installata = true;
            // 0.5.3 (collaudo 22:13, CS1061): AddClassHandler e' un metodo di estensione di Avalonia (AvaloniaObjectExtensions) sulla forma
            // non generica degli eventi; HeaderedContentControl sta in Avalonia.Controls.Primitives
            Aggancia<Avalonia.Controls.TextBlock>(Avalonia.Controls.TextBlock.TextProperty.Changed, (t, v) => Sostituisci(t, Avalonia.Controls.TextBlock.TextProperty, v as string));
            Aggancia<Avalonia.Controls.ContentControl>(Avalonia.Controls.ContentControl.ContentProperty.Changed, (c, v) =>
            {
                if (v is string s && !(c is Avalonia.Controls.Window)) Sostituisci(c, Avalonia.Controls.ContentControl.ContentProperty, s);
            });
            Aggancia<Avalonia.Controls.Primitives.HeaderedContentControl>(Avalonia.Controls.Primitives.HeaderedContentControl.HeaderProperty.Changed, (c, v) => { if (v is string s) Sostituisci(c, Avalonia.Controls.Primitives.HeaderedContentControl.HeaderProperty, s); });
            Aggancia<Avalonia.Controls.Window>(Avalonia.Controls.Window.TitleProperty.Changed, (w, v) => Sostituisci(w, Avalonia.Controls.Window.TitleProperty, v as string));
            Aggancia<Avalonia.Controls.TextBox>(Avalonia.Controls.TextBox.WatermarkProperty.Changed, (t, v) => Sostituisci(t, Avalonia.Controls.TextBox.WatermarkProperty, v as string));
            Aggancia<Avalonia.Controls.ComboBox>(Avalonia.Controls.ComboBox.PlaceholderTextProperty.Changed, (c, v) => Sostituisci(c, Avalonia.Controls.ComboBox.PlaceholderTextProperty, v as string));
            Aggancia<Avalonia.Controls.Control>(Avalonia.Controls.ToolTip.TipProperty.Changed, (c, v) => { if (v is string s) Sostituisci(c, Avalonia.Controls.ToolTip.TipProperty, s); });
        }

        // gestore di classe sulla forma non generica dell'evento (IObservable e' covariante): v = nuovo valore come object
        static void Aggancia<T>(IObservable<Avalonia.AvaloniaPropertyChangedEventArgs> evento, Action<T, object> azione) where T : Avalonia.AvaloniaObject
        {
            Avalonia.AvaloniaObjectExtensions.AddClassHandler<T>(evento, (o, e) => azione(o, e.NewValue));
        }

        static void Sostituisci(Avalonia.AvaloniaObject o, Avalonia.AvaloniaProperty p, string testo)
        {
            if (inTraduzione || string.IsNullOrEmpty(testo) || !Attiva) return;
            string t;
            if (!map.TryGetValue(testo, out t) || t == testo) return;
            inTraduzione = true;
            // 1.0.2: SetCurrentValue e non SetValue: il testo tradotto non stacca il collegamento (binding) della scritta, cosi' la casella
            // di un menu a tendina (es. Terreno) segue la scelta successiva invece di restare ferma sulla prima voce tradotta
            try { o.SetCurrentValue(p, t); }
            catch { }
            finally { inTraduzione = false; }
        }
    }
}
