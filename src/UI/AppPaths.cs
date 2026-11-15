// YA-NEC - cartelle del programma e impostazioni: tutto nella cartella "data" accanto al programma (niente AppData)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace YaNec
{
    public static class AppPaths
    {
        public const string AppName = "YA-NEC";
        public const string Author = "a cura di Costantino IC8TEM";
        public static readonly string Version = ReadVersion();

        static string ReadVersion()
        {
            try
            {
                System.Reflection.AssemblyInformationalVersionAttribute a = (System.Reflection.AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                    typeof(AppPaths).Assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute));
                string v = a != null ? a.InformationalVersion : typeof(AppPaths).Assembly.GetName().Version.ToString(3);
                int i = v.IndexOf('+');
                return i > 0 ? v.Substring(0, i) : v;
            }
            catch { return "0.0.0"; }
        }

        public static string Title { get { return AppName + " " + Version + " - " + Author; } }

        // cartella del programma (su Mac quella che contiene YA-NEC.app)
        public static string AppDir
        {
            get
            {
                string b = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                int i = b.IndexOf(".app" + Path.DirectorySeparatorChar + "Contents", StringComparison.OrdinalIgnoreCase);
                if (i > 0) return Path.GetDirectoryName(b.Substring(0, i + 4)) ?? b;
                return b;
            }
        }

        static string dataDir;
        public static string DataDir
        {
            get
            {
                if (dataDir != null) return dataDir;
                dataDir = Path.Combine(AppDir, "data");
                try { Directory.CreateDirectory(dataDir); } catch { }
                return dataDir;
            }
        }

        // 0.4.30 (IC8TEM 14/11/2026 23:14, "per questo applicativo non voglio dati in appdata"): i file temporanei vanno in data\tmp
        // accanto al programma, non in %TEMP% (che su Windows e' dentro AppData\Local)
        public static string TempDir
        {
            get
            {
                string d = Path.Combine(DataDir, "tmp");
                try { Directory.CreateDirectory(d); } catch { }
                return d;
            }
        }

        public static string ModelsDir
        {
            get
            {
                string d = Path.Combine(DataDir, "modelli");
                try { Directory.CreateDirectory(d); } catch { }
                return d;
            }
        }
    }

    // impostazioni chiave=valore in data/config.ini
    public class Config
    {
        readonly string path;
        readonly Dictionary<string, string> v = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Config(string p)
        {
            path = p;
            try
            {
                if (File.Exists(p))
                    foreach (string line in File.ReadAllLines(p))
                    {
                        int eq = line.IndexOf('=');
                        if (eq > 0) v[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                    }
            }
            catch { }
        }

        public string Get(string k, string def) { string s; return v.TryGetValue(k, out s) ? s : def; }

        public double GetD(string k, double def)
        {
            double d;
            return double.TryParse(Get(k, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out d) ? d : def;
        }

        // 0.5.0 (piu' finestre di YA-NEC aperte insieme): prima di scrivere si rilegge il file, cosi' una finestra non cancella le scelte
        // salvate nel frattempo da un'altra; vince l'ultima scelta fatta per ogni chiave
        public void Set(string k, string val)
        {
            try
            {
                if (File.Exists(path))
                    foreach (string line in File.ReadAllLines(path))
                    {
                        int eq = line.IndexOf('=');
                        if (eq > 0) v[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                    }
            }
            catch { }
            v[k] = val;
            Save();
        }
        public void SetD(string k, double d) { Set(k, d.ToString("R", CultureInfo.InvariantCulture)); }

        void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, string> kv in v) sb.AppendLine(kv.Key + "=" + kv.Value);
                File.WriteAllText(path, sb.ToString());
            }
            catch { }
        }
    }
}
