// YA-NEC - misura delle prestazioni per il registro (richiesta di IC8TEM del 13/11/2026):
//  - descrizione del computer: processore, scheda grafica, memoria, disco (SSD o HDD) del programma;
//  - campioni delle risorse: CPU del programma e del sistema, GPU (solo Windows, contatori PDH), memoria, letture e scritture su disco;
//  - misura delle funzioni (using (Prestazioni.Misura("nome")) { ... }): tempo, tempo proprio (senza le funzioni chiamate dentro),
//    tempo di CPU, memoria allocata; alla fine il punteggio di quanto ogni funzione pesa sulla lentezza.
// Soglie dei tempi di risposta: 0,1 s (immediato), 1 s (il filo del pensiero non si interrompe), 10 s (l'attenzione si perde):
// J. Nielsen, "Usability Engineering" (1993), cap. 5, da R. B. Miller (1968).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace YaNec
{
    public static class Prestazioni
    {
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        // ------------------------------------------------------------------------------------- funzioni misurate
        public class Statistica
        {
            public string Nome;
            public int Chiamate;
            public double TotaleMs, PropriMs, MaxMs, CpuMs, AllocMb;
            public int Lente;          // chiamate oltre 1 s
        }

        static readonly object lk = new object();
        static readonly Dictionary<string, Statistica> stat = new Dictionary<string, Statistica>();
        static readonly Stopwatch orologio = Stopwatch.StartNew();

        public sealed class Scopo : IDisposable
        {
            [ThreadStatic] static Scopo corrente;
            readonly string nome, dettaglio;
            readonly bool scrivi;
            readonly long t0, alloc0;
            readonly TimeSpan cpu0;
            readonly Scopo padre;
            double figliMs;
            bool chiuso;
            /// <summary>Funzione che distribuisce il lavoro su piu' thread (sweep): il suo tempo proprio vale 0, conta il lavoro dentro.</summary>
            public bool Contenitore;

            internal Scopo(string nome, string dettaglio, bool scrivi)
            {
                this.nome = nome; this.dettaglio = dettaglio; this.scrivi = scrivi;
                padre = corrente; corrente = this;
                alloc0 = GC.GetTotalAllocatedBytes(false);
                cpu0 = CpuProcesso();
                t0 = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                if (chiuso) return;
                chiuso = true;
                double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
                double cpu = (CpuProcesso() - cpu0).TotalMilliseconds;
                double mb = (GC.GetTotalAllocatedBytes(false) - alloc0) / 1048576.0;
                if (corrente == this) corrente = padre;
                if (padre != null) padre.figliMs += ms;
                double propri = Contenitore ? 0 : Math.Max(0, ms - figliMs);
                Registra(nome, ms, propri, cpu, mb);
                if (scrivi && Registro.Attivo)
                    Registro.Scrivi("TEMPO", string.Format(CI, "{0}{1}: {2:0} ms (propri {3:0} ms), CPU {4:0} ms, memoria allocata {5:0.0} MB{6}",
                        nome, string.IsNullOrEmpty(dettaglio) ? "" : " (" + dettaglio + ")", ms, propri, cpu, mb, Giudizio(ms)));
            }
        }

        /// <summary>Misura una funzione. scrivi = false: solo nel punteggio (funzioni chiamate molte volte, es. il disegno 3D).</summary>
        public static Scopo Misura(string nome, string dettaglio = null, bool scrivi = true) { return new Scopo(nome, dettaglio, scrivi); }

        /// <summary>Aggiunge al punteggio una durata misurata altrove (es. l'interfaccia ferma).</summary>
        public static void Registra(string nome, double ms, double propri, double cpuMs, double allocMb)
        {
            lock (lk)
            {
                Statistica s;
                if (!stat.TryGetValue(nome, out s)) { s = new Statistica { Nome = nome }; stat[nome] = s; }
                s.Chiamate++; s.TotaleMs += ms; s.PropriMs += propri; s.MaxMs = Math.Max(s.MaxMs, ms); s.CpuMs += cpuMs; s.AllocMb += allocMb;
                if (ms > 1000) s.Lente++;
            }
        }

        public static string Giudizio(double ms)
        {
            if (ms < 100) return "";
            if (ms < 1000) return "  [percepibile]";
            if (ms < 10000) return "  [LENTO: oltre 1 s]";
            return "  [MOLTO LENTO: oltre 10 s]";
        }

        /// <summary>Tabella del punteggio: quanto ogni funzione pesa sulla lentezza (quota del tempo proprio sul totale misurato).</summary>
        public static string Punteggio()
        {
            List<Statistica> l;
            lock (lk) l = stat.Values.Select(x => new Statistica { Nome = x.Nome, Chiamate = x.Chiamate, TotaleMs = x.TotaleMs, PropriMs = x.PropriMs, MaxMs = x.MaxMs, CpuMs = x.CpuMs, AllocMb = x.AllocMb, Lente = x.Lente }).ToList();
            double tot = l.Sum(x => x.PropriMs);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("PUNTEGGIO DELLE FUNZIONI - quanto pesano sulla lentezza (sessione di " + FormatoDurata(orologio.Elapsed.TotalSeconds) + ")");
            sb.AppendLine("Punti = quota del tempo proprio (senza le funzioni chiamate dentro) sul tempo misurato, 0-100. Media e massimo: tempo intero.");
            sb.AppendLine("CPU = tempo di CPU di tutto il programma durante la funzione (piu' alto del tempo se lavora su piu' processori).");
            sb.AppendLine("Peso per chiamata: leggera < 0,1 s, percepibile < 1 s, lenta < 10 s, molto lenta oltre (soglie di Nielsen/Miller).");
            sb.AppendLine(string.Format(CI, "{0,6} {1,-44} {2,8} {3,10} {4,9} {5,9} {6,10} {7,9} {8,6}  {9}", "punti", "funzione", "chiamate", "totale s", "media ms", "max ms", "CPU s", "alloc MB", ">1 s", "peso per chiamata"));
            foreach (Statistica s in l.OrderByDescending(x => x.PropriMs))
            {
                double media = s.TotaleMs / Math.Max(1, s.Chiamate);
                string peso = media < 100 ? "leggera" : media < 1000 ? "percepibile" : media < 10000 ? "lenta" : "molto lenta";
                sb.AppendLine(string.Format(CI, "{0,6:0.0} {1,-44} {2,8} {3,10:0.00} {4,9:0} {5,9:0} {6,10:0.00} {7,9:0.0} {8,6}  {9}",
                    tot > 0 ? 100 * s.PropriMs / tot : 0, s.Nome.Length > 44 ? s.Nome.Substring(0, 44) : s.Nome, s.Chiamate, s.TotaleMs / 1000, media, s.MaxMs, s.CpuMs / 1000, s.AllocMb, s.Lente, peso));
            }
            if (l.Count == 0) sb.AppendLine("(nessuna funzione misurata)");
            return sb.ToString();
        }

        static string FormatoDurata(double s)
        {
            if (s < 120) return s.ToString("0", CI) + " s";
            return ((int)(s / 60)).ToString(CI) + " min " + ((int)(s % 60)).ToString(CI) + " s";
        }

        // ------------------------------------------------------------------------------------- tempo di CPU
        // del processo intero (il motore lavora anche su piu' thread): durante una funzione conta anche quello che fa il resto del programma
        static TimeSpan CpuProcesso()
        {
            try { using (Process p = Process.GetCurrentProcess()) return p.TotalProcessorTime; } catch { return TimeSpan.Zero; }
        }

        [DllImport("kernel32.dll")] static extern IntPtr GetCurrentThread();
        [DllImport("kernel32.dll")] static extern bool GetThreadTimes(IntPtr h, out long creazione, out long fine, out long kernel, out long utente);

        static TimeSpan CpuThread()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    long c, f, k, u;
                    if (GetThreadTimes(GetCurrentThread(), out c, out f, out k, out u)) return TimeSpan.FromTicks(k + u);
                }
                else if (OperatingSystem.IsLinux())
                {
                    // /proc/thread-self/stat: campi 14 e 15 = utime, stime in tick (di solito 100 al secondo)
                    string s = File.ReadAllText("/proc/thread-self/stat");
                    string[] p = s.Substring(s.LastIndexOf(')') + 2).Split(' ');
                    return TimeSpan.FromMilliseconds((long.Parse(p[11], CI) + long.Parse(p[12], CI)) * 10.0);
                }
            }
            catch { }
            return TimeSpan.Zero;
        }

        // ------------------------------------------------------------------------------------- descrizione del computer
        /// <summary>Righe di intestazione del registro: sistema, processore, grafica, memoria, disco.</summary>
        public static string Computer()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Sistema:     " + RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + "), .NET " + Environment.Version);
            sb.AppendLine("Processore:  " + Prova(NomeProcessore) + ", " + Environment.ProcessorCount + " processori logici");
            sb.AppendLine("Grafica:     " + Prova(SchedaGrafica));
            long tot = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            sb.AppendLine("Memoria:     " + (tot / 1073741824.0).ToString("0.0", CI) + " GB totali, " + Prova(() => (RamLibera() / 1073741824.0).ToString("0.0", CI) + " GB liberi all'avvio"));
            sb.AppendLine("Disco:       " + Prova(Disco));
            sb.AppendLine("Programma:   " + AppPaths.AppDir);
            return sb.ToString();
        }

        static string Prova(Func<string> f) { try { string s = f(); return string.IsNullOrWhiteSpace(s) ? "non rilevato" : s.Trim(); } catch (Exception e) { return "non rilevato (" + e.GetType().Name + ")"; } }

        static string NomeProcessore()
        {
            if (OperatingSystem.IsWindows())
                return Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "") as string;
            if (OperatingSystem.IsLinux())
                foreach (string l in File.ReadLines("/proc/cpuinfo"))
                    if (l.StartsWith("model name") || l.StartsWith("Model")) return l.Substring(l.IndexOf(':') + 1);
            if (OperatingSystem.IsMacOS()) return Comando("sysctl", "-n machdep.cpu.brand_string", 3000);
            return null;
        }

        static string SchedaGrafica()
        {
            List<string> l = new List<string>();
            if (OperatingSystem.IsWindows())
            {
                // classe degli adattatori video
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                {
                    if (k != null)
                        foreach (string n in k.GetSubKeyNames())
                        {
                            if (n.Length != 4) continue;
                            using (Microsoft.Win32.RegistryKey a = k.OpenSubKey(n))
                            {
                                if (a == null) continue;
                                string d = a.GetValue("DriverDesc") as string;
                                if (string.IsNullOrEmpty(d)) continue;
                                object mem = a.GetValue("HardwareInformation.qwMemorySize") ?? a.GetValue("HardwareInformation.MemorySize");
                                double gb = 0;
                                if (mem is long) gb = (long)mem / 1073741824.0;
                                else if (mem is int) gb = (uint)(int)mem / 1073741824.0;
                                else if (mem is byte[] && ((byte[])mem).Length >= 4) gb = BitConverter.ToUInt32((byte[])mem, 0) / 1073741824.0;
                                string v = d + (gb > 0 ? " (" + gb.ToString("0.#", CI) + " GB)" : "");
                                if (!l.Contains(v)) l.Add(v);
                            }
                        }
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                string s = Comando("lspci", "", 3000);
                if (s != null)
                    foreach (string r in s.Split('\n'))
                        if (r.Contains("VGA") || r.Contains("3D controller") || r.Contains("Display controller")) l.Add(r.Substring(r.IndexOf(':', r.IndexOf(' ')) + 1).Trim());
            }
            else if (OperatingSystem.IsMacOS())
            {
                string s = Comando("system_profiler", "SPDisplaysDataType", 8000);
                if (s != null) foreach (string r in s.Split('\n')) if (r.Trim().StartsWith("Chipset Model:")) l.Add(r.Trim().Substring(14).Trim());
            }
            return string.Join("; ", l);
        }

        static string Disco()
        {
            string dir = AppPaths.AppDir;
            DriveInfo di = new DriveInfo(Path.GetPathRoot(dir));
            string spazio = di.Name + " " + di.DriveFormat + ", " + (di.TotalSize / 1073741824.0).ToString("0", CI) + " GB, liberi " + (di.AvailableFreeSpace / 1073741824.0).ToString("0", CI) + " GB";
            string tipo = null;
            if (OperatingSystem.IsWindows() && di.Name.Length >= 1 && char.IsLetter(di.Name[0]))
            {
                // MediaType: SSD / HDD / Unspecified; BusType: NVMe, SATA, USB...
                string ps = "$d=Get-Partition -DriveLetter " + di.Name[0] + " | Get-Disk; $p=Get-PhysicalDisk | Where-Object DeviceId -eq $d.Number; " +
                            "Write-Output ($p.MediaType.ToString()+'|'+$p.BusType.ToString()+'|'+$p.FriendlyName)";
                string r = Comando("powershell", "-NoProfile -NonInteractive -Command \"" + ps + "\"", 10000);
                if (r != null && r.Contains("|"))
                {
                    string[] p = r.Trim().Split('|');
                    tipo = (p[0] == "Unspecified" ? (p[1] == "NVMe" ? "SSD" : "tipo non dichiarato") : p[0]) + ", bus " + p[1] + ", " + (p.Length > 2 ? p[2] : "");
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                string src = Comando("df", "--output=source \"" + dir + "\"", 3000);
                if (src != null)
                {
                    string dev = src.Trim().Split('\n').Last().Trim();
                    string r = Comando("lsblk", "-ndo ROTA,TRAN,MODEL -s \"" + dev + "\"", 3000);
                    if (r != null)
                    {
                        string riga = r.Trim().Split('\n').Last().Trim();
                        tipo = (riga.StartsWith("0") ? "SSD" : riga.StartsWith("1") ? "HDD" : "?") + " " + riga.Substring(Math.Min(1, riga.Length)).Trim();
                    }
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                string r = Comando("diskutil", "info /", 5000);
                if (r != null) foreach (string l in r.Split('\n')) if (l.Contains("Solid State:")) tipo = l.Contains("Yes") ? "SSD" : "HDD";
            }
            return (tipo ?? "tipo non rilevato") + " - " + spazio;
        }

        static string Comando(string exe, string arg, int timeoutMs)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, arg) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using (Process p = Process.Start(psi))
                {
                    System.Threading.Tasks.Task<string> t = p.StandardOutput.ReadToEndAsync();
                    if (!t.Wait(timeoutMs) || !p.WaitForExit(timeoutMs)) { try { p.Kill(); } catch { } return null; }
                    return t.Result;
                }
            }
            catch { return null; }
        }

        // ------------------------------------------------------------------------------------- campioni delle risorse
        [StructLayout(LayoutKind.Sequential)]
        struct MEMORYSTATUSEX { public uint dwLength, dwMemoryLoad; public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual; }
        [DllImport("kernel32.dll")] static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX m);
        [StructLayout(LayoutKind.Sequential)]
        struct IO_COUNTERS { public ulong ReadOps, WriteOps, OtherOps, ReadBytes, WriteBytes, OtherBytes; }
        [DllImport("kernel32.dll")] static extern bool GetProcessIoCounters(IntPtr h, out IO_COUNTERS c);
        [DllImport("kernel32.dll")] static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

        static long RamLibera()
        {
            if (OperatingSystem.IsWindows())
            {
                MEMORYSTATUSEX m = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
                if (GlobalMemoryStatusEx(ref m)) return (long)m.ullAvailPhys;
            }
            else if (OperatingSystem.IsLinux())
                foreach (string l in File.ReadLines("/proc/meminfo"))
                    if (l.StartsWith("MemAvailable:")) return long.Parse(l.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1], CI) * 1024;
            return -1;
        }

        static bool IoProcesso(out double letti, out double scritti)
        {
            letti = scritti = 0;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    IO_COUNTERS c;
                    if (GetProcessIoCounters(Process.GetCurrentProcess().Handle, out c)) { letti = c.ReadBytes; scritti = c.WriteBytes; return true; }
                }
                else if (OperatingSystem.IsLinux())
                {
                    foreach (string l in File.ReadLines("/proc/self/io"))
                    {
                        if (l.StartsWith("rchar:")) letti = double.Parse(l.Substring(6).Trim(), CI);
                        if (l.StartsWith("wchar:")) scritti = double.Parse(l.Substring(6).Trim(), CI);
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }

        static bool TempiSistema(out double occupato, out double totale)
        {
            occupato = totale = 0;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    long i, k, u;
                    if (GetSystemTimes(out i, out k, out u)) { totale = k + u; occupato = k + u - i; return true; }   // kernel comprende idle
                }
                else if (OperatingSystem.IsLinux())
                {
                    string[] p = File.ReadLines("/proc/stat").First().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    double t = 0; for (int j = 1; j < p.Length; j++) t += double.Parse(p[j], CI);
                    double idle = double.Parse(p[4], CI) + (p.Length > 5 ? double.Parse(p[5], CI) : 0);
                    totale = t; occupato = t - idle; return true;
                }
            }
            catch { }
            return false;
        }

        // GPU (Windows 10 1709 e successivi): contatori "GPU Engine" con le API PDH, senza pacchetti aggiuntivi
        [DllImport("pdh.dll", CharSet = CharSet.Unicode)] static extern uint PdhOpenQuery(string src, IntPtr user, out IntPtr q);
        [DllImport("pdh.dll", CharSet = CharSet.Unicode)] static extern uint PdhAddEnglishCounter(IntPtr q, string path, IntPtr user, out IntPtr c);
        [DllImport("pdh.dll")] static extern uint PdhCollectQueryData(IntPtr q);
        [DllImport("pdh.dll", CharSet = CharSet.Unicode)] static extern uint PdhGetFormattedCounterArray(IntPtr c, uint fmt, ref uint size, out uint count, IntPtr buf);
        const uint PDH_FMT_DOUBLE = 0x200, PDH_MORE_DATA = 0x800007D2;
        static IntPtr pdhQ, pdhGpu, pdhGpuMem;
        static bool pdhOk;

        static void AvviaGpu()
        {
            if (!OperatingSystem.IsWindows()) return;
            try
            {
                if (PdhOpenQuery(null, IntPtr.Zero, out pdhQ) != 0) return;
                if (PdhAddEnglishCounter(pdhQ, @"\GPU Engine(*)\Utilization Percentage", IntPtr.Zero, out pdhGpu) != 0) return;
                PdhAddEnglishCounter(pdhQ, @"\GPU Process Memory(*)\Dedicated Usage", IntPtr.Zero, out pdhGpuMem);
                PdhCollectQueryData(pdhQ);
                pdhOk = true;
            }
            catch { pdhOk = false; }
        }

        static List<KeyValuePair<string, double>> LeggiArray(IntPtr c)
        {
            List<KeyValuePair<string, double>> l = new List<KeyValuePair<string, double>>();
            if (c == IntPtr.Zero) return l;
            uint size = 0, n;
            uint rc = PdhGetFormattedCounterArray(c, PDH_FMT_DOUBLE, ref size, out n, IntPtr.Zero);
            if (rc != PDH_MORE_DATA || size == 0) return l;
            IntPtr buf = Marshal.AllocHGlobal((int)size);
            try
            {
                if (PdhGetFormattedCounterArray(c, PDH_FMT_DOUBLE, ref size, out n, buf) != 0) return l;
                const int passo = 24;               // PDH_FMT_COUNTERVALUE_ITEM_W: nome (puntatore), CStatus a +8, valore double a +16 (allineato a 8)
                for (int i = 0; i < n; i++)
                {
                    IntPtr p = buf + i * passo;
                    string nome = Marshal.PtrToStringUni(Marshal.ReadIntPtr(p));
                    double v = BitConverter.Int64BitsToDouble(Marshal.ReadInt64(p + 16));
                    l.Add(new KeyValuePair<string, double>(nome ?? "", v));
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
            return l;
        }

        /// <summary>GPU: utilizzo 3D totale e di YA-NEC (%), memoria video di YA-NEC (MB). Valori negativi = non disponibile.</summary>
        static void Gpu(out double tot3d, out double nostro, out double memMb)
        {
            tot3d = nostro = memMb = -1;
            if (!pdhOk) return;
            try
            {
                if (PdhCollectQueryData(pdhQ) != 0) return;
                string pid = "pid_" + Environment.ProcessId + "_";
                tot3d = 0; nostro = 0;
                foreach (KeyValuePair<string, double> kv in LeggiArray(pdhGpu))
                {
                    if (!kv.Key.EndsWith("engtype_3D", StringComparison.OrdinalIgnoreCase)) continue;
                    tot3d += kv.Value;
                    if (kv.Key.StartsWith(pid, StringComparison.OrdinalIgnoreCase)) nostro += kv.Value;
                }
                tot3d = Math.Min(100, tot3d); nostro = Math.Min(100, nostro);
                memMb = 0;
                foreach (KeyValuePair<string, double> kv in LeggiArray(pdhGpuMem))
                    if (kv.Key.StartsWith(pid, StringComparison.OrdinalIgnoreCase)) memMb += kv.Value / 1048576.0;
            }
            catch { tot3d = nostro = memMb = -1; }
        }

        static Timer timer;
        static double ultimoWall, ultimoCpu, ultimoLetti, ultimoScritti, ultimoOcc, ultimoTot;
        static int giri;

        /// <summary>Avvia i campioni periodici (ogni 2 s). Riga RISORSE se il programma lavora (CPU oltre il 5 %) e comunque ogni 30 s.</summary>
        public static void AvviaCampioni()
        {
            if (timer != null) return;
            AvviaGpu();
            Campione(true);
            timer = new Timer(_ => Campione(false), null, 2000, 2000);
        }

        public static void FermaCampioni() { try { timer?.Dispose(); } catch { } timer = null; }

        /// <summary>Una riga con lo stato attuale delle risorse (anche a richiesta, es. alla fine di un calcolo).</summary>
        public static string Risorse() { return Campione(true, false); }

        static readonly object lkc = new object();

        static string Campione(bool forza, bool scrivi = true)
        {
            lock (lkc)
            {
                try
                {
                    Process p = Process.GetCurrentProcess();
                    double wall = orologio.Elapsed.TotalMilliseconds, cpu = p.TotalProcessorTime.TotalMilliseconds;
                    double dw = Math.Max(1, wall - ultimoWall);
                    double cpuPerc = 100 * (cpu - ultimoCpu) / dw / Environment.ProcessorCount, core = (cpu - ultimoCpu) / dw;
                    double occ, tot, sis = -1;
                    if (TempiSistema(out occ, out tot)) { if (tot > ultimoTot && ultimoTot > 0) sis = 100 * (occ - ultimoOcc) / (tot - ultimoTot); ultimoOcc = occ; ultimoTot = tot; }
                    double letti, scritti, dl = -1, ds = -1;
                    if (IoProcesso(out letti, out scritti)) { if (ultimoWall > 0) { dl = (letti - ultimoLetti) / 1048576.0; ds = (scritti - ultimoScritti) / 1048576.0; } ultimoLetti = letti; ultimoScritti = scritti; }
                    double g3, gn, gm;
                    Gpu(out g3, out gn, out gm);
                    bool primo = ultimoWall == 0;
                    ultimoWall = wall; ultimoCpu = cpu;
                    giri++;
                    long lib = -1;
                    try { lib = RamLibera(); } catch { }
                    string r = string.Format(CI, "CPU YA-NEC {0:0}% ({1:0.0} core){2}; RAM YA-NEC {3:0} MB (heap .NET {4:0} MB){5}; GPU {6}; disco YA-NEC {7}; thread {8}",
                        primo ? 0 : cpuPerc, primo ? 0 : core, sis >= 0 ? string.Format(CI, ", sistema {0:0}%", sis) : "",
                        p.WorkingSet64 / 1048576.0, GC.GetTotalMemory(false) / 1048576.0, lib > 0 ? string.Format(CI, ", liberi {0:0.0} GB", lib / 1073741824.0) : "",
                        g3 < 0 ? "non misurata" : string.Format(CI, "3D {0:0}% (YA-NEC {1:0}%), memoria video YA-NEC {2:0} MB", g3, gn, gm),
                        dl < 0 ? "-" : string.Format(CI, "letti {0:0.0} MB, scritti {1:0.0} MB in {2:0.0} s", dl, ds, dw / 1000), p.Threads.Count);
                    if (scrivi && Registro.Attivo && (forza || cpuPerc > 5 || giri % 15 == 0)) Registro.Scrivi("RISORSE", r);
                    return r;
                }
                catch (Exception e) { return "risorse non lette (" + e.GetType().Name + ")"; }
            }
        }

        // ------------------------------------------------------------------------------------- grafica di Avalonia
        /// <summary>Come disegna l'interfaccia: con la scheda grafica (ANGLE/OpenGL) o in software (libreria caricata nel processo).</summary>
        public static string Disegno()
        {
            try
            {
                List<string> m = new List<string>();
                foreach (ProcessModule x in Process.GetCurrentProcess().Modules)
                {
                    string n = x.ModuleName.ToLowerInvariant();
                    if (n.Contains("libglesv2") || n.Contains("libegl") || n.Contains("opengl32") || n.Contains("libgl.") || n.Contains("d3d11") || n.Contains("skiasharp") || n.Contains("metal"))
                        m.Add(x.FileName);
                }
                bool gpu = m.Exists(x => x.ToLowerInvariant().Contains("glesv2") || x.ToLowerInvariant().Contains("libgl.") || x.ToLowerInvariant().Contains("metal"));
                return (gpu ? "con la scheda grafica" : "probabilmente in software (nessuna libreria OpenGL/ANGLE caricata)") + "; librerie: " + (m.Count == 0 ? "-" : string.Join(", ", m));
            }
            catch (Exception e) { return "non rilevato (" + e.GetType().Name + ")"; }
        }
    }
}
