// YA-NEC - librerie native in una sottocartella (richiesta di IC8TEM del 13/11/2026: "le DLL non nella stessa cartella dell'EXE").
// Dalla 0.4.2 il programma si pubblica come file unico (PublishSingleFile): le librerie .NET (Avalonia, SkiaSharp...) sono
// dentro YA-NEC.exe; le librerie native che non possono stare dentro (libSkiaSharp, libHarfBuzzSharp, av_libglesv2,
// libAvaloniaNative) vengono spostate dalla compilazione nella cartella "lib" accanto al programma (YA-NEC.csproj,
// obiettivo SpostaLibrerieNative). Qui il programma le ritrova:
//  - Windows: la cartella lib entra nel percorso di ricerca delle DLL (SetDllDirectory) e nel PATH del processo;
//  - tutti i sistemi: AssemblyLoadContext.ResolvingUnmanagedDll carica la libreria da lib quando la ricerca normale fallisce.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace YaNec
{
    public static class Librerie
    {
        public static string Cartella { get; private set; }
        static readonly List<string> caricate = new List<string>();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetDllDirectory(string path);

        public static void Prepara()
        {
            try
            {
                string d = Path.Combine(AppContext.BaseDirectory, "lib");
                if (!Directory.Exists(d)) return;
                Cartella = d;
                if (OperatingSystem.IsWindows())
                {
                    try { SetDllDirectory(d); } catch { }
                    Environment.SetEnvironmentVariable("PATH", d + Path.PathSeparator + (Environment.GetEnvironmentVariable("PATH") ?? ""));
                }
                AssemblyLoadContext.Default.ResolvingUnmanagedDll += Risolvi;
            }
            catch { }
        }

        static IntPtr Risolvi(Assembly a, string nome)
        {
            if (Cartella == null) return IntPtr.Zero;
            string b = Path.GetFileName(nome);
            List<string> prove = new List<string> { b };
            if (OperatingSystem.IsWindows()) { prove.Add(b + ".dll"); }
            else if (OperatingSystem.IsMacOS()) { prove.Add(b + ".dylib"); prove.Add("lib" + b + ".dylib"); }
            else { prove.Add(b + ".so"); prove.Add("lib" + b + ".so"); }
            foreach (string p in prove)
            {
                string f = Path.Combine(Cartella, p);
                IntPtr h;
                if (File.Exists(f) && NativeLibrary.TryLoad(f, out h))
                {
                    lock (caricate) caricate.Add(p);
                    return h;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>Per il registro: dove sono le librerie native e quali sono state caricate da lib.</summary>
        public static string Descrizione
        {
            get
            {
                if (Cartella == null)
                {
                    int n = 0;
                    try { n = Directory.GetFiles(AppContext.BaseDirectory, "*.dll").Length; } catch { }
                    return "cartella lib assente (compilazione vecchia o non a file unico); DLL accanto al programma: " + n;
                }
                string[] f;
                try { f = Directory.GetFiles(Cartella); } catch { f = new string[0]; }
                List<string> nomi = new List<string>();
                foreach (string x in f) nomi.Add(Path.GetFileName(x));
                return "cartella " + Cartella + " (" + string.Join(", ", nomi) + ")";
            }
        }

        public static string Caricate { get { lock (caricate) return caricate.Count == 0 ? "-" : string.Join(", ", caricate); } }
    }
}
