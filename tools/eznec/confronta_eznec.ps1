# YA-NEC - confronto con EZNEC, seconda versione (Costantino Cerrotta IC8TEM, 13/11/2026)
# Fa calcolare a EZNEC (v. 5.0.60 o successiva, anche Pro/2+ 7.0) ogni modello .ez della cartella "modelli_ez" e raccoglie i risultati
# in "risultati_eznec". Tutti i modelli dell'archivio di YA-NEC sono gia' convertiti in .ez (EZNEC con l'interfaccia apre solo i .ez).
# Interfaccia: quella che usa AutoEZ (AC6LA). EZNEC scrive nel registro (HKCU\Software\VB and VBA Program Settings\EZNEC\AutoEZ) le
# maniglie di due caselle di testo nascoste, "Request" e "Response": il comando si scrive in Request (WM_SETTEXT), la risposta si legge
# in Response (WM_GETTEXT). Sequenza come AutoEZ (modulo EZinterface e RunCalculations): il modello si copia come $AutoEZ$.ez in una
# cartella di lavoro, accanto si scrive $Freqs$.txt, poi "1|<cartella>\$AutoEZ$.ez" (apri, risposta 0), "4|&H2125" (opzioni del calcolo),
# "3|&H2" (calcolo, risposta 0; con 3 si riprova con due frequenze). Risultati: $Sweep$.txt nella cartella di lavoro e LastZ_sorted.txt
# nella cartella dei dati di EZNEC (o nel VirtualStore). Nessun codice di AutoEZ e' copiato: solo il protocollo.
# Novita' di questa versione (prima prova del 13/11 21:36: EZNEC si era bloccato su una finestra e i risultati erano vecchi):
#  - i comandi partono in un thread a parte e intanto lo script guarda le finestre di EZNEC: se compare una finestra di messaggio la
#    scrive nel registro e la chiude (OK / Si' / Continua), cosi' EZNEC non resta bloccato;
#  - il LastZ_sorted.txt si usa solo se e' stato scritto DOPO il comando di calcolo e ha il titolo del modello;
#  - se $Sweep$.txt non e' nella cartella di lavoro lo cerca nelle cartelle dei dati di EZNEC e lo scrive nel registro;
#  - -Prova N: solo i primi N modelli, con il registro dettagliato (per capire subito se qualcosa non va).
# 13/11 22:35: il testo della finestra "Geometry Check" (errori di geometria di EZNEC) si scrive nel registro e la finestra si chiude;
# 13/11 23:05: i .ez vecchi (VerCode 30/31) sono riscritti nello zip; se EZNEC si chiude (errore di run-time) lo script lo riavvia e va avanti.
# 13/11 22:04 (prova di IC8TEM: "Frequency sweep frequency file not found"): EZNEC legge i nomi di $Freqs$.txt e $Sweep$.txt dal blocco 11
# del .ez, come li scrive AutoEZ; ora lo script riscrive quel blocco in ogni copia $AutoEZ$.ez con i file della cartella di lavoro.
# 0.4.20 (compila.bat, fase 2G): -Uscita = cartella dei risultati (lo zip va nella cartella sopra); -Riferimento = csv di YA-NEC da copiare tra i risultati
param([string]$Cartella = "", [int]$Attesa = 300, [int]$Prova = 0, [string]$Lavoro = "C:\YANEC_EZ", [switch]$Stepped, [string]$Uscita = "", [string]$Riferimento = "")

$ErrorActionPreference = "Stop"
$qui = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($Cartella -eq "") { $Cartella = Join-Path $qui "modelli_ez" }
$out = if ($Uscita -ne "") { $Uscita } else { Join-Path $qui "risultati_eznec" }
$dZip = if ($Uscita -ne "") { Split-Path -Parent $Uscita } else { $qui }
$det = Join-Path $out "dettagli"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force -Path $out, $det | Out-Null
New-Item -ItemType Directory -Force -Path $Lavoro | Out-Null
$env:YANEC_TMP = $Lavoro   # 0.4.30: niente file temporanei in %TEMP% (AppData), richiesta di IC8TEM del 14/11/2026 23:14
$registro = Join-Path $out "registro.txt"
function Scrivi([string]$s) { $r = (Get-Date -Format "HH:mm:ss") + "  " + $s; Write-Host $r; Add-Content -Path $registro -Value $r -Encoding UTF8 }

Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class EzWin {
    [DllImport("user32.dll", CharSet = CharSet.Ansi, EntryPoint = "SendMessageA")] static extern IntPtr SendS(IntPtr h, uint msg, IntPtr w, string l);
    [DllImport("user32.dll", CharSet = CharSet.Ansi, EntryPoint = "SendMessageTimeoutA")] static extern IntPtr SendTB(IntPtr h, uint msg, IntPtr w, StringBuilder l, uint f, uint t, out IntPtr r);
    [DllImport("user32.dll", CharSet = CharSet.Ansi, EntryPoint = "SendMessageTimeoutA")] static extern IntPtr SendTS(IntPtr h, uint msg, IntPtr w, string l, uint f, uint t, out IntPtr r);
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr p, EnumProc f, IntPtr l);
    public static List<string> Log = new List<string>();
    static HashSet<IntPtr> Visti = new HashSet<IntPtr>();
    static void Aggiungi(string x) { Log.Add(x.Length > 1500 ? x.Substring(0, 1500) + " ..." : x); }
    static string Testo(IntPtr h) { StringBuilder s = new StringBuilder(2048); GetWindowText(h, s, 2048); return s.ToString(); }
    static string Classe(IntPtr h) { StringBuilder s = new StringBuilder(256); GetClassName(h, s, 256); return s.ToString(); }
    [DllImport("user32.dll", CharSet = CharSet.Ansi, EntryPoint = "SendMessageTimeoutA")] static extern IntPtr SendTI(IntPtr h, uint msg, IntPtr w, IntPtr l, uint f, uint t, out IntPtr r);
    // prova 5b (14/11/2026 14:03): il testo della "Segmentation Check" e della "Geometry Check" sta in un controllo grafico; la finestra
    // ha i menu File ed Edit: si cercano le voci di copia (Edit > Copy / Select All) e si legge il testo dagli appunti di Windows
    [DllImport("user32.dll")] static extern IntPtr GetMenu(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr h);
    [DllImport("user32.dll")] static extern int GetDlgCtrlID(IntPtr h);
    [DllImport("user32.dll")] static extern int GetMenuItemCount(IntPtr m);
    [DllImport("user32.dll")] static extern IntPtr GetSubMenu(IntPtr m, int pos);
    [DllImport("user32.dll")] static extern uint GetMenuItemID(IntPtr m, int pos);
    [DllImport("user32.dll", CharSet = CharSet.Ansi)] static extern int GetMenuStringA(IntPtr m, uint id, StringBuilder s, int n, uint f);
    [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr h);
    [DllImport("user32.dll")] static extern bool CloseClipboard();
    [DllImport("user32.dll")] static extern bool EmptyClipboard();
    [DllImport("user32.dll")] static extern IntPtr GetClipboardData(uint f);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr h);
    [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr h);
    static string Appunti() {
        string t = "";
        for (int k = 0; k < 10 && !OpenClipboard(IntPtr.Zero); k++) System.Threading.Thread.Sleep(50);
        try { IntPtr h = GetClipboardData(13); if (h != IntPtr.Zero) { IntPtr p = GlobalLock(h); if (p != IntPtr.Zero) { t = Marshal.PtrToStringUni(p); GlobalUnlock(h); } }
              if (t.Length == 0) { h = GetClipboardData(1); if (h != IntPtr.Zero) { IntPtr p = GlobalLock(h); if (p != IntPtr.Zero) { t = Marshal.PtrToStringAnsi(p); GlobalUnlock(h); } } } }
        finally { CloseClipboard(); }
        return t ?? "";
    }
    static string SalvaTesto(IntPtr h, uint idSalva) {
        uint pid; GetWindowThreadProcessId(h, out pid);
        string file = System.IO.Path.Combine(Environment.GetEnvironmentVariable("YANEC_TMP") ?? System.IO.Path.GetTempPath(), "yanec_controllo_" + DateTime.Now.Ticks + ".txt");
        PostMessage(h, 0x0111, (IntPtr)idSalva, IntPtr.Zero);                     // WM_COMMAND: File > Save As (non blocca)
        IntPtr dlg = IntPtr.Zero;
        for (int k = 0; k < 50 && dlg == IntPtr.Zero; k++) {
            System.Threading.Thread.Sleep(100);
            EnumWindows((w, l) => { uint p; GetWindowThreadProcessId(w, out p);
                if (p == pid && w != h && IsWindowVisible(w) && Classe(w) == "#32770") { string t = Testo(w);
                    if (t.IndexOf("Save", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("Salva", StringComparison.OrdinalIgnoreCase) >= 0) dlg = w; }
                return true; }, IntPtr.Zero);
        }
        if (dlg == IntPtr.Zero) return "(finestra Save As non trovata)";
        // casella del nome del file: la Edit dentro la casella combinata con l'identificativo 1148 (cmb13) o 1152 (edt1); altrimenti la prima Edit
        IntPtr ed = IntPtr.Zero, prima = IntPtr.Zero;
        EnumChildWindows(dlg, (c, l) => { if (Classe(c) == "Edit") { if (prima == IntPtr.Zero) prima = c;
                IntPtr p1 = GetParent(c), p2 = p1 == IntPtr.Zero ? IntPtr.Zero : GetParent(p1);
                if (ed == IntPtr.Zero && (GetDlgCtrlID(c) == 1152 || GetDlgCtrlID(p1) == 1148 || GetDlgCtrlID(p2) == 1148)) ed = c; } return true; }, IntPtr.Zero);
        if (ed == IntPtr.Zero) ed = prima;
        if (ed == IntPtr.Zero) { PostMessage(dlg, 0x0111, (IntPtr)2, IntPtr.Zero); return "(casella del nome non trovata)"; }
        SendS(ed, 0x000C, IntPtr.Zero, file);                                      // WM_SETTEXT: nome del file
        System.Threading.Thread.Sleep(200);
        PostMessage(dlg, 0x0111, (IntPtr)1, IntPtr.Zero);                          // IDOK: Salva
        string trovato = null;
        for (int k = 0; k < 50 && trovato == null; k++) {
            System.Threading.Thread.Sleep(100);
            foreach (string c in new[] { file, file + ".txt" }) if (System.IO.File.Exists(c) && new System.IO.FileInfo(c).Length > 0) trovato = c;
        }
        if (trovato == null) { if (IsWindow(dlg)) PostMessage(dlg, 0x0111, (IntPtr)2, IntPtr.Zero); return "(file non salvato)"; }
        System.Threading.Thread.Sleep(200);
        string testo = System.IO.File.ReadAllText(trovato, Encoding.Default);
        try { System.IO.File.Delete(trovato); } catch { }
        return testo.Replace("\r", " ").Replace("\n", " / ");
    }
    // voci dei menu della finestra; esegue "Select All" e poi "Copy" se ci sono, e restituisce il testo copiato (o l'elenco delle voci)
    static string CopiaDalMenu(IntPtr h) {
        IntPtr m = GetMenu(h); if (m == IntPtr.Zero) return "(nessun menu)";
        StringBuilder voci = new StringBuilder(); uint idCopia = 0, idTutto = 0, idSalva = 0;
        int nm = GetMenuItemCount(m);
        for (int i = 0; i < nm; i++) {
            IntPtr sm = GetSubMenu(m, i); if (sm == IntPtr.Zero) continue;
            int ns = GetMenuItemCount(sm);
            for (int j = 0; j < ns; j++) {
                uint id = GetMenuItemID(sm, j); StringBuilder t = new StringBuilder(256); GetMenuStringA(sm, (uint)j, t, 256, 0x400);
                string v = t.ToString().Replace("&", ""); if (v.Length == 0) continue; voci.Append(v + "; ");
                string vl = v.ToLowerInvariant();
                if (vl.StartsWith("copy") || vl.StartsWith("copia")) idCopia = id;
                if (vl.StartsWith("select all") || vl.StartsWith("seleziona tutto")) idTutto = id;
                if (vl.StartsWith("save as") || vl.StartsWith("salva con nome")) idSalva = id;
            }
        }
        // prova 5c (14/11/2026 14:11): la finestra non ha Copy ma ha File > Save As: si salva il testo in un file temporaneo e lo si legge
        if (idCopia == 0 && idSalva != 0) { try { return SalvaTesto(h, idSalva); } catch (Exception ex) { return "(Save As non riuscito: " + ex.Message + "; voci: " + voci.ToString() + ")"; } }
        if (idCopia == 0) return "(voci dei menu: " + voci.ToString() + ")";
        if (OpenClipboard(IntPtr.Zero)) { EmptyClipboard(); CloseClipboard(); }
        if (idTutto != 0) { SendMessage(h, 0x0111, (IntPtr)idTutto, IntPtr.Zero); System.Threading.Thread.Sleep(200); }
        SendMessage(h, 0x0111, (IntPtr)idCopia, IntPtr.Zero); System.Threading.Thread.Sleep(400);
        string c = Appunti().Replace("\r", " ").Replace("\n", " / ");
        return c.Length > 0 ? c : "(copia vuota; voci dei menu: " + voci.ToString() + ")";
    }
    // testo di un controllo di un altro processo (caselle di testo, elenchi): GetWindowText non lo legge, WM_GETTEXT e LB_GETTEXT si'
    static string TestoControllo(IntPtr c) {
        IntPtr r; string cl = Classe(c);
        if (cl.Contains("ListBox")) {
            SendTI(c, 0x018B, IntPtr.Zero, IntPtr.Zero, 2, 2000, out r);          // LB_GETCOUNT
            int n = (int)r; StringBuilder all = new StringBuilder();
            for (int i = 0; i < n && i < 200; i++) { StringBuilder sb = new StringBuilder(1024); SendTB(c, 0x0189, (IntPtr)i, sb, 2, 2000, out r); all.Append(sb.ToString() + " / "); }
            return all.ToString();
        }
        StringBuilder t = new StringBuilder(8192); SendTB(c, 0x000D, (IntPtr)8192, t, 2, 2000, out r);   // WM_GETTEXT
        return t.ToString();
    }
    public static uint Pid(IntPtr h) { uint p; GetWindowThreadProcessId(h, out p); return p; }
    // finestre di dialogo visibili del processo di EZNEC (diverse dalla finestra principale): le scrive nel registro e le chiude
    public static int ChiudiDialoghi(uint pid, IntPtr main) {
        List<IntPtr> dl = new List<IntPtr>();
        EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p);
            if (p == pid && h != main && IsWindowVisible(h)) { string c = Classe(h); if (c == "#32770" || c.StartsWith("ThunderRT6FormDC") || c.StartsWith("ThunderFormDC")) dl.Add(h); }
            return true; }, IntPtr.Zero);
        int n = 0;
        foreach (IntPtr h in dl) {
            string tit = Testo(h); StringBuilder tx = new StringBuilder();
            List<IntPtr> bott = new List<IntPtr>();
            EnumChildWindows(h, (c, l) => { string cl = Classe(c), t = TestoControllo(c);
                if (cl == "Button" || cl.Contains("CommandButton")) bott.Add(c); else if (t.Length > 0) tx.Append(t.Replace("\r", " ").Replace("\n", " ") + " | ");
                return true; }, IntPtr.Zero);
            if (tit == "Segmentation Check" || tit == "Geometry Check" || tit == "Key Error" || tit == "NEC-4 Information") {
                // prova 5 (14/11/2026): nella prova delle 12:34 il testo di queste finestre era vuoto; si aspetta che EZNEC le riempia,
                // si rilegge e si scrivono anche le classi dei controlli, per capire perche' EZNEC risponde 7
                if (Visti.Contains(h) == false) {
                    System.Threading.Thread.Sleep(700);
                    StringBuilder tx2 = new StringBuilder(), cls = new StringBuilder();
                    EnumChildWindows(h, (c, l) => { string cl2 = Classe(c), t2 = TestoControllo(c); cls.Append(cl2 + ","); if (t2.Length > 0) tx2.Append(t2.Replace("\r", " ").Replace("\n", " ") + " | "); return true; }, IntPtr.Zero);
                    if (tx2.Length > tx.Length) tx = tx2;
                    string copiato = "";
                    try { copiato = CopiaDalMenu(h); } catch (Exception ex) { copiato = "(copia non riuscita: " + ex.Message + ")"; }
                    tx.Append(" TESTO: " + copiato);
                }
                if (Visti.Add(h)) Aggiungi("finestra di EZNEC \"" + tit + "\": " + tx.ToString() + " -> chiusa");
                PostMessage(h, 0x0112, (IntPtr)0xF060, IntPtr.Zero); n++; continue;                 // WM_SYSCOMMAND, SC_CLOSE
            }
            if (Classe(h) != "#32770" && bott.Count == 0) continue;   // finestra normale di EZNEC (grafici, tabelle): si lascia
            IntPtr scelto = IntPtr.Zero; string sc = "";
            foreach (string pref in new[] { "OK", "&OK", "Yes", "&Yes", "S\u00EC", "&S\u00EC", "Continue", "&Continue", "Continua", "&Continua", "Close", "&Close", "Chiudi" })
                foreach (IntPtr b in bott) if (scelto == IntPtr.Zero && Testo(b).Replace("&", "").Equals(pref.Replace("&", ""), StringComparison.OrdinalIgnoreCase)) { scelto = b; sc = Testo(b); }
            string bl = ""; foreach (IntPtr b in bott) bl += "[" + Testo(b) + "]";
            // finestre senza OK/Si'/Continua (per esempio l'avanzamento del calcolo con "Cancel"): si lasciano stare, scritte una volta
            if (scelto == IntPtr.Zero) { if (Visti.Add(h)) Aggiungi("finestra di EZNEC \"" + tit + "\": " + tx.ToString() + " pulsanti " + bl + " (lasciata aperta)"); continue; }
            Aggiungi("FINESTRA di EZNEC \"" + tit + "\": " + tx.ToString() + " pulsanti " + bl + " -> premuto " + sc);
            PostMessage(scelto, 0x00F5, IntPtr.Zero, IntPtr.Zero);      // BM_CLICK
            n++;
        }
        return n;
    }
    public static string Leggi(IntPtr resp) { IntPtr r; StringBuilder sb = new StringBuilder(1024); SendTB(resp, 0x000D, (IntPtr)1024, sb, 0, 5000, out r); return sb.ToString(); }
    // comando in un thread a parte; intanto si chiudono le finestre di messaggio. Risposta "TIMEOUT" dopo ms millisecondi.
    public static string Cmd(IntPtr req, IntPtr resp, IntPtr main, string testo, int ms, bool risposta) {
        uint pid = Pid(main);
        bool fatto = false;
        Thread t = new Thread(() => { SendS(req, 0x000C, IntPtr.Zero, testo); fatto = true; });
        t.IsBackground = true; t.Start();
        DateTime fine = DateTime.Now.AddMilliseconds(ms);
        while (!fatto && DateTime.Now < fine) { Thread.Sleep(200); if (!fatto) ChiudiDialoghi(pid, main); }
        if (!fatto) return "TIMEOUT";
        ChiudiDialoghi(pid, main);
        return risposta ? Leggi(resp) : "";
    }
    public static bool Pronto(IntPtr req, IntPtr resp) {
        IntPtr r;
        if (!IsWindow(req)) return false;
        if (SendTS(req, 0x000C, IntPtr.Zero, "99", 0, 5000, out r) == IntPtr.Zero) return false;
        return Leggi(resp) == "9999";
    }
    public static void Chiudi(string titolo) {
        EnumWindows((h, l) => { if (Testo(h) == titolo) PostMessage(h, 0x0112, (IntPtr)0xF060, IntPtr.Zero); return true; }, IntPtr.Zero);
    }
}
"@

# Blocco 11 del .ez (impostazioni del frequency sweep): EZNEC prende da qui il nome del file delle frequenze e del file dei risultati.
# AutoEZ lo scrive cosi' (ReadWriteEZ, WriteEZ): tipo 11, lunghezza 340, rev 1, DataLen 16, lunghezze dei nomi dei file (frequenze,
# -, -, dati, -), flag &H2125, FStart = frequenza, FStop 0, FStep 0, i due nomi in 301 caratteri, 12345 di controllo.
# I .ez dell'archivio hanno il blocco 11 senza nomi di file: per questo la prima prova dava "Frequency sweep frequency file not found".
function Blocco11([string]$fiF, [string]$dfF, [single]$freq) {
    $ms = New-Object IO.MemoryStream; $w = New-Object IO.BinaryWriter($ms)
    $w.Write([int16]11); $w.Write([int32]340); $w.Write([byte]1); $w.Write([int32]16)
    $w.Write([int16]$fiF.Length); $w.Write([int16]0); $w.Write([int16]0); $w.Write([int16]$dfF.Length); $w.Write([int16]0)
    $w.Write([int32]0x2125); $w.Write([single]$freq); $w.Write([single]0); $w.Write([single]0)
    $spec = ($fiF + $dfF).PadRight(301).Substring(0, 301)
    $w.Write([Text.Encoding]::GetEncoding(28591).GetBytes($spec)); $w.Write([int16]12345); $w.Flush()
    return ,$ms.ToArray()
}
function PreparaEz([byte[]]$b, [string]$fiF, [string]$dfF, [single]$freq) {
    $nr = [BitConverter]::ToInt16($b, 0); $p0 = 186 + 170 * $nr + 324; $p = $p0
    $blocchi = New-Object System.Collections.Generic.List[byte[]]; $trovato = $false
    while ($p + 7 -le $b.Length) {
        $t = [BitConverter]::ToInt16($b, $p); $l = [BitConverter]::ToInt32($b, $p + 2)
        if ($l -le 0 -or $p + $l -gt $b.Length) { break }
        if ($t -eq 11 -and -not $trovato) { $blocchi.Add((Blocco11 $fiF $dfF $freq)); $trovato = $true }
        else { $x = New-Object byte[] $l; [Array]::Copy($b, $p, $x, 0, $l); $blocchi.Add($x) }
        $p += $l
    }
    if (-not $trovato) { $blocchi.Insert(0, (Blocco11 $fiF $dfF $freq)) }
    $ms = New-Object IO.MemoryStream
    # (prova del 13/11 22:56: dichiarare versione 50 un .ez vecchio fa cadere EZNEC con l'errore 9: i .ez vecchi ora sono riscritti nello zip)
    $ms.Write($b, 0, [Math]::Min($p0, $b.Length))
    foreach ($x in $blocchi) { $ms.Write($x, 0, $x.Length) }
    if ($p -lt $b.Length -and $p -ge $p0) { $ms.Write($b, $p, $b.Length - $p) }
    return ,$ms.ToArray()
}

function Maniglie {
    $k = "HKCU:\Software\VB and VBA Program Settings\EZNEC\AutoEZ"
    if (-not (Test-Path $k)) { return $null }
    $p = Get-ItemProperty $k
    return @{ Main = [IntPtr][int64]$p.MainForm; Req = [IntPtr][int64]$p.Request; Resp = [IntPtr][int64]$p.Response }
}
function Pronto($m) { return ($m -ne $null) -and [EzWin]::Pronto($m.Req, $m.Resp) }
function Svuota { foreach ($l in [EzWin]::Log) { Scrivi ("      " + $l) }; [EzWin]::Log.Clear() }

# 1) EZNEC aperto (una sola istanza); se non lo e', lo cerca e lo avvia
$m = Maniglie
if (-not (Pronto $m)) {
    Scrivi "EZNEC non risponde: lo cerco e lo avvio."
    $exe = $null
    foreach ($base in @("C:\", ${env:ProgramFiles}, ${env:ProgramFiles(x86)})) {
        if (-not $base) { continue }
        $exe = Get-ChildItem -Path $base -Filter "EZW*.exe" -Recurse -Depth 2 -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($exe) { break }
    }
    if (-not $exe) { Scrivi "EZNEC non trovato. Avvia EZNEC a mano, lascialo aperto e rilancia."; exit 1 }
    Scrivi ("Avvio " + $exe.FullName)
    Start-Process -FilePath $exe.FullName -WorkingDirectory $exe.DirectoryName
    for ($i = 0; $i -lt 60; $i++) { Start-Sleep -Seconds 1; [EzWin]::Chiudi("Key Error"); $m = Maniglie; if (Pronto $m) { break } }
    if (-not (Pronto $m)) { Scrivi "EZNEC avviato ma non risponde all'interfaccia di AutoEZ (serve la v. 5.0.60 o successiva, non 'come amministratore')."; exit 1 }
}
[EzWin]::Chiudi("NEC-4 Information")
[void][EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "7|1", 10000, $false)
# percorso di EZNEC, per riavviarlo se si chiude (prova del 13/11 22:56: "Errore di run-time 9" su un .ez e EZNEC si e' chiuso)
$ezExe = $null
try { $ezExe = (Get-Process -Id ([EzWin]::Pid($m.Main))).Path } catch { }
$riavvii = 0
function Riavvia {
    if (-not $script:ezExe) { return $false }
    $script:riavvii++
    if ($script:riavvii -gt 10) { return $false }
    Scrivi ("      riavvio EZNEC (" + $script:riavvii + ")")
    try { Get-Process | Where-Object { $_.Path -eq $script:ezExe } | Stop-Process -Force } catch { }
    Start-Sleep -Seconds 2
    Start-Process -FilePath $script:ezExe -WorkingDirectory (Split-Path $script:ezExe)
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 1; [EzWin]::Chiudi("Key Error"); [EzWin]::Chiudi("NEC-4 Information")
        $script:m = Maniglie
        if (Pronto $script:m) { [void][EzWin]::Cmd($script:m.Req, $script:m.Resp, $script:m.Main, "7|1", 10000, $false); return $true }
    }
    return $false
}
$ver = [EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "9", 5000, $true)
$dati = [EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "8", 5000, $true)
Scrivi ("EZNEC risponde. Versione " + $ver + ", cartella dei dati " + $dati + ", cartella di lavoro " + $Lavoro)
$cercaLastz = @((Join-Path $dati "LastZ_sorted.txt"), (Join-Path $env:LOCALAPPDATA ("VirtualStore" + $dati.Substring(2) + "\LastZ_sorted.txt")))
$cercaSweep = @($Lavoro, $dati, (Join-Path $env:LOCALAPPDATA ("VirtualStore" + $dati.Substring(2))), [Environment]::GetFolderPath("MyDocuments"), $env:TEMP)

# -Stepped (13/11 22:40, "vedere se puoi lavorare con le tabelle di stepped"): la correzione dei diametri a gradini di EZNEC non ha un
# comando nell'interfaccia di AutoEZ. Si cerca il bit della testata del .ez che la comanda: per due Yagi a tubi telescopici dell'ARRL si
# calcola il modello cosi' com'e' e poi con ciascuno dei 32 bit di MiscFlags (0x8C) e dei byte vicini cambiati; un'impedenza che diventa
# quella senza correzione (520-40H: circa 5,5 -j3 ohm invece di 21 -j18) indica il bit. Risultati in risultati_eznec\stepped.csv.
if ($Stepped) {
  # 0.4.34 (collaudo 0.4.33: stepped.csv con la sola intestazione e nessuna riga nel registro): tutto dentro try, con la riga dell'errore
  try {
    $sc = Join-Path $out "stepped.csv"
    "modello;prova;byte;bit;calcolo;R ohm;X ohm;note" | Set-Content -Path $sc -Encoding UTF8
    $dest = Join-Path $Lavoro '$AutoEZ$.ez'; $fq = Join-Path $Lavoro '$Freqs$.txt'; $sw = Join-Path $Lavoro '$Sweep$.txt'
    $cult = [System.Globalization.CultureInfo]::CurrentCulture
    foreach ($nome in @("ARRL Antenna Book\520-40H.ez", "ARRL Antenna Book\620-80H.ez")) {
        $fz = Join-Path $Cartella $nome
        Scrivi ("stepped: " + $fz + $(if (Test-Path -LiteralPath $fz) { " presente" } else { " MANCANTE" }))
        if (-not (Test-Path -LiteralPath $fz)) { Scrivi ("manca " + $fz); continue }
        $b0 = [IO.File]::ReadAllBytes($fz); $freq = [BitConverter]::ToSingle($b0, 5)
        $prove = New-Object System.Collections.Generic.List[object]
        $prove.Add(@("originale", (-1), (-1)))
        for ($bit = 0; $bit -lt 32; $bit++) { $prove.Add(@("MiscFlags", (0x8C + [int][Math]::Floor($bit / 8)), ($bit % 8))) }   # 0.4.37: parentesi (in PowerShell la virgola lega piu' di + e %); il commento ora sta dopo la graffa (0.4.36: la chiudeva)
        foreach ($o in @(0x88, 0x89, 0x8A, 0x8B, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9)) { $prove.Add(@("byte", $o, 0)) }
        foreach ($p in $prove) {
            $b = [byte[]]$b0.Clone()
            if ($p[0] -eq "MiscFlags") { $b[$p[1]] = $b[$p[1]] -bxor [byte](1 -shl $p[2]) }
            elseif ($p[0] -eq "byte") { $b[$p[1]] = [byte](1 - [Math]::Min(1, $b[$p[1]])) }
            try {
                [IO.File]::WriteAllBytes($dest, (PreparaEz $b $fq $sw $freq))
                Set-Content -LiteralPath $fq -Value (" " + ([double]$freq).ToString("0.######", $cult) + " ") -Encoding Default
                $t0 = Get-Date
                $ap = [EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "1|" + $dest, 60000, $true); Svuota
                if ($ap -ne "0") { "$nome;" + $p[0] + ";" + $p[1] + ";" + $p[2] + ";apertura $ap;;;" | Add-Content -Path $sc -Encoding UTF8; continue }
                [void][EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "4|&H2125", 10000, $false)
                $rc = [EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "3|&H2", $Attesa * 1000, $true); Svuota
                $z = ""
                foreach ($c in $cercaLastz) { if ((Test-Path -LiteralPath $c) -and (Get-Item -LiteralPath $c).LastWriteTime -ge $t0.AddSeconds(-2)) { $z = (Get-Content -LiteralPath $c | Select-Object -Skip 3 -First 1); break } }
                $zz = $z -split ','
                $riga = "$nome;" + $p[0] + ";0x" + ('{0:X2}' -f [int]$p[1]) + ";" + $p[2] + ";$rc;" + $(if ($zz.Count -ge 4) { $zz[2] + ";" + $zz[3] } else { ";" }) + ";"
                $riga | Add-Content -Path $sc -Encoding UTF8
                Scrivi ("stepped " + $riga)
            } catch { Scrivi ("stepped errore " + $_.Exception.Message) }
        }
    }
    $zip = Join-Path $qui ("eznec-stepped-" + (Get-Date -Format "yyyy-MM-dd_HH-mm") + ".zip")
    Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip -Force
    Scrivi ("Fatto. Zip da mandare: " + $zip)
  } catch { Scrivi ("stepped: ERRORE alla riga " + $_.InvocationInfo.ScriptLineNumber + ": " + $_.Exception.Message + " | " + $_.InvocationInfo.Line.Trim()) }
    exit 0
}

$csv = Join-Path $out "riepilogo_eznec.csv"
"file;apertura;calcolo;secondi;frequenza MHz;sorgente;R ohm;X ohm;ROS 50;guadagno max dBi;angolo max;tipo diagramma;note" | Set-Content -Path $csv -Encoding UTF8
$cult = [System.Globalization.CultureInfo]::CurrentCulture
$inv = [System.Globalization.CultureInfo]::InvariantCulture
$files = @(Get-ChildItem -Path $Cartella -Recurse -File | Where-Object { $_.Extension -match '^\.ez$' } | Sort-Object FullName)
if ($Prova -gt 0) { $files = @($files | Select-Object -First $Prova) }
Scrivi ("Modelli da calcolare: " + $files.Count + " (cartella " + $Cartella + ")")
$dest = Join-Path $Lavoro '$AutoEZ$.ez'
$fq = Join-Path $Lavoro '$Freqs$.txt'
$sw = Join-Path $Lavoro '$Sweep$.txt'
$n = 0; $bloccati = 0; $prec = ""
foreach ($f in $files) {
    $n++
    $rel = $f.FullName.Substring($Cartella.Length).TrimStart('\')
    $nomeOut = ($rel -replace '[\\/:*?"<>|]', '_')
    $t0 = Get-Date
    try {
        if (-not (Pronto $m)) {
            [void][EzWin]::ChiudiDialoghi([EzWin]::Pid($m.Main), $m.Main); Svuota; Start-Sleep -Seconds 2
            if (-not (Pronto $m)) {
                Scrivi ("[" + $n + "] EZNEC non risponde piu' (dopo " + $prec + ").")
                "$prec;EZNEC si e' chiuso o bloccato dopo questo modello;;;;;;;;;;;" | Add-Content -Path $csv -Encoding UTF8
                if (-not (Riavvia)) { Scrivi "EZNEC non si riavvia: mi fermo. Chiudi EZNEC, riaprilo e rilancia."; break }
            }
        }
        $prec = $rel
        $b = [IO.File]::ReadAllBytes($f.FullName)
        $freq = [BitConverter]::ToSingle($b, 5)
        $titolo = [Text.Encoding]::GetEncoding(28591).GetString($b, 0x12, 30).Trim([char]0, ' ')
        [IO.File]::WriteAllBytes($dest, (PreparaEz $b $fq $sw $freq))
        foreach ($x in @($sw)) { if (Test-Path -LiteralPath $x) { Remove-Item -LiteralPath $x -Force } }
        # $Freqs$.txt come la scrive AutoEZ (Print # di VBA: spazio davanti, separatore decimale di Windows)
        Set-Content -LiteralPath $fq -Value (" " + ([double]$freq).ToString("0.######", $cult) + " ") -Encoding Default
        $ap = [EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "1|" + $dest, 60000, $true); Svuota
        if ($ap -ne "0") {
            "$rel;risposta $ap;-;;;;;;;;;;EZNEC non apre il file" | Add-Content -Path $csv -Encoding UTF8
            Scrivi ("[" + $n + "/" + $files.Count + "] " + $rel + ": apertura -> " + $ap); continue
        }
        $tCalc = Get-Date
        [void][EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "4|&H2125", 10000, $false)
        $rc = [EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "3|&H2", $Attesa * 1000, $true); Svuota
        if ($rc -eq "3") {
            # come AutoEZ: con una sola frequenza a volte serve un secondo tentativo con due frequenze
            Set-Content -LiteralPath $fq -Value @((" " + ([double]$freq).ToString("0.######", $cult) + " "), (" " + ([double]$freq + 0.1).ToString("0.######", $cult) + " ")) -Encoding Default
            [void][EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "4|&H2125", 10000, $false)
            $rc = [EzWin]::Cmd($m.Req, $m.Resp, $m.Main, "3|&H2", $Attesa * 1000, $true); Svuota
            $rc = "3 poi " + $rc
        }
        $sec = ((Get-Date) - $t0).TotalSeconds.ToString("0.0", $inv)
        # $Sweep$.txt: nella cartella di lavoro; se no lo si cerca nelle altre (e lo si scrive nel registro)
        $swTrovato = $null
        for ($i = 0; $i -lt 10 -and -not $swTrovato; $i++) {
            foreach ($c in $cercaSweep) { $p = Join-Path $c '$Sweep$.txt'; if ((Test-Path -LiteralPath $p) -and (Get-Item -LiteralPath $p).LastWriteTime -ge $tCalc.AddSeconds(-2)) { $swTrovato = $p; break } }
            if (-not $swTrovato) { Start-Sleep -Milliseconds 300 }
        }
        if ($swTrovato -and $swTrovato -ne $sw -and $n -le 3) { Scrivi ("      `$Sweep`$.txt trovato in " + $swTrovato) }
        $guad = ""; $ang = ""; $tipo = ""; $fs = ""
        if ($swTrovato) {
            Copy-Item -LiteralPath $swTrovato (Join-Path $det ($nomeOut + ".sweep.txt")) -Force
            $righe = Get-Content -LiteralPath $swTrovato
            $max = -999.0
            for ($i = 0; $i -lt $righe.Count; $i++) {
                $r = $righe[$i]
                if ($r -match 'Frequency = ([\d.,]+)' -and $fs -eq "") { $fs = $Matches[1] }
                if ($r -match '(Elevation|Azimuth|Elev|Azim)' -and $tipo -eq "") { $tipo = $r.Trim() }
                if ($r -match '^\s*-?\d+(?:[.,]\d+)?\s+-?\d') {
                    $c = $r.Trim() -split '\s+'
                    if ($c.Count -ge 4) {
                        $v = 0.0
                        if ([double]::TryParse($c[3], [Globalization.NumberStyles]::Float, $cult, [ref]$v) -and $v -gt $max) { $max = $v; $ang = $c[0] }
                    }
                }
            }
            if ($max -gt -999) { $guad = $max.ToString("0.00", $inv) }
        }
        # LastZ_sorted.txt: valido solo se scritto dopo il comando e con il titolo del modello
        $zr = @(); $nota = ""
        $lz = $null
        foreach ($c in $cercaLastz) { if ((Test-Path -LiteralPath $c) -and (Get-Item -LiteralPath $c).LastWriteTime -ge $tCalc.AddSeconds(-2)) { $lz = $c; break } }
        if ($lz) {
            Copy-Item -LiteralPath $lz (Join-Path $det ($nomeOut + ".lastz.txt")) -Force
            $tutte = Get-Content -LiteralPath $lz
            if ($tutte.Count -ge 2 -and $titolo.Length -gt 0 -and -not $tutte[1].Contains($titolo.Substring(0, [Math]::Min(10, $titolo.Length)))) { $nota = "LastZ con un altro titolo: " + $tutte[1] }
            $zr = $tutte | Select-Object -Skip 3
        } else { $nota = "LastZ_sorted.txt non aggiornato da EZNEC" }
        if (-not $swTrovato) { $nota = ($nota + " | `$Sweep`$.txt non scritto").Trim(" |") }
        if ($zr.Count -eq 0) { "$rel;0;$rc;$sec;$fs;;;;;$guad;$ang;$tipo;$nota" | Add-Content -Path $csv -Encoding UTF8 }
        foreach ($z in $zr) {
            $c = $z -split ','
            if ($c.Count -ge 5) { "$rel;0;$rc;$sec;" + $c[1].Trim() + ";" + $c[0].Trim() + ";" + $c[2].Trim() + ";" + $c[3].Trim() + ";" + $c[4].Trim() + ";$guad;$ang;$tipo;$nota" | Add-Content -Path $csv -Encoding UTF8 }
        }
        Scrivi ("[" + $n + "/" + $files.Count + "] " + $rel + ": calcolo -> " + $rc + " in " + $sec + " s, Z " + ($(if ($zr.Count) { ($zr[0] -split ',')[2] + " " + ($zr[0] -split ',')[3] } else { "-" })) + ", Gmax " + $guad + " dBi " + $nota)
    }
    catch { "$rel;errore;;;;;;;;;;;$($_.Exception.Message)" | Add-Content -Path $csv -Encoding UTF8; Scrivi ("[" + $n + "] " + $rel + ": errore " + $_.Exception.Message) }
}
Copy-Item (Join-Path $qui "elenco_conversione.csv") $out -ErrorAction SilentlyContinue
if ($Riferimento -ne "" -and (Test-Path $Riferimento)) { Copy-Item $Riferimento $out -ErrorAction SilentlyContinue }
$zip = Join-Path $dZip ("eznec-confronto-" + (Get-Date -Format "yyyy-MM-dd_HH-mm") + ".zip")
Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip -Force
Scrivi ("Fatto. Zip da mandare: " + $zip)
