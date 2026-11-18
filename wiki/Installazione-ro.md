# Instalare

YA-NEC nu se instalează: se dezarhivează și se pornește. Nu e nevoie de .NET, este deja în pachet. Setările, modelele și jurnalele rămân în dosarul programului (`data`, `Registri`): fără AppData, fără registry. Pentru a-l șterge, ajunge să ștergi dosarul.

## Ce pachet

De pe pagina [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistem | Pachet |
|---|---|
| Windows 10/11 pe 64 de biți | `YA-NEC-<versiune>-windows-x64-portatile.zip` |
| Windows 10/11 pe 32 de biți | `YA-NEC-<versiune>-windows-x86-portatile.zip` |
| Windows pe ARM (Snapdragon) | `YA-NEC-<versiune>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versiune>-windows7-x64-portatile.zip` sau `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versiune>-linux-x64.tar.gz` |
| Raspberry Pi OS pe 64 de biți și alte Linux aarch64 | `YA-NEC-<versiune>-linux-arm64.tar.gz` |
| Raspberry Pi OS pe 32 de biți, armv7l | `YA-NEC-<versiune>-linux-armv7l.tar.gz` |
| Alpine și Linux cu musl | `YA-NEC-<versiune>-alpine-x64.tar.gz` sau `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versiune>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versiune>-mac-intel-app.zip` |

Lângă fiecare zip se află suma de control `.sha256` pentru verificarea descărcării.

## Windows

Dezarhivează zip-ul într-un dosar cu drept de scriere, de exemplu `C:\YA-NEC` (nu în `C:\Program Files`, unde programul nu și-ar putea salva setările) și pornește `YA-NEC.exe`. La prima pornire, Windows SmartScreen poate cere confirmare: „Mai multe informații" > „Rulează oricum".

**Windows 7 SP1, 8 și 8.1**: folosește pachetele `win7`, compilate cu .NET 6 (.NET 8 nu pornește pe Windows 7). Windows 7 are nevoie de Service Pack 1 și de actualizările KB2533623 și KB2999226.

## Linux și Raspberry Pi

```
tar -xzf YA-NEC-<versiune>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Sunt necesare `libfontconfig1` și `libicu`, prezente deja în distribuțiile desktop. Pe Alpine: `apk add icu-libs fontconfig libstdc++`. Pe Raspberry Pi calculul funcționează, dar modelele mari (mii de segmente) cer timp și memorie.

## Mac

Dezarhivează zip-ul, de exemplu în `~/YA-NEC`. Programul nu este semnat de Apple, deci carantina trebuie scoasă o dată, din Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Ecrane mici

Pe ecranele joase (de exemplu 1366 x 768) fereastra se deschide maximizată, iar casetele de rezultate trec la formatul compact.
