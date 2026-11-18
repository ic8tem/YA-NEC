# Installasjon

YA-NEC installeres ikke: du pakker det ut og starter det. .NET trengs ikke, det er allerede i pakken. Innstillinger, modeller og logger blir i programmappen (`data`, `Registri`): ingen AppData, intet register. For å fjerne det sletter du bare mappen.

## Hvilken pakke

Fra siden [Releases](https://github.com/ic8tem/YA-NEC/releases):

| System | Pakke |
|---|---|
| Windows 10/11 64-bit | `YA-NEC-<versjon>-windows-x64-portatile.zip` |
| Windows 10/11 32-bit | `YA-NEC-<versjon>-windows-x86-portatile.zip` |
| Windows på ARM (Snapdragon) | `YA-NEC-<versjon>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versjon>-windows7-x64-portatile.zip` eller `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versjon>-linux-x64.tar.gz` |
| Raspberry Pi OS 64-bit og andre aarch64-Linux | `YA-NEC-<versjon>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32-bit, armv7l | `YA-NEC-<versjon>-linux-armv7l.tar.gz` |
| Alpine og musl-Linux | `YA-NEC-<versjon>-alpine-x64.tar.gz` eller `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versjon>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versjon>-mac-intel-app.zip` |

Ved siden av hver zip ligger kontrollsummen `.sha256` for å sjekke nedlastingen.

## Windows

Pakk ut zip-filen i en mappe med skrivetilgang, for eksempel `C:\YA-NEC` (ikke i `C:\Programfiler`, der programmet ikke kan lagre innstillingene), og start `YA-NEC.exe`. Ved første start kan Windows SmartScreen be om bekreftelse: «Mer informasjon» > «Kjør likevel».

**Windows 7 SP1, 8 og 8.1**: bruk `win7`-pakkene, bygget med .NET 6 (.NET 8 starter ikke på Windows 7). Windows 7 trenger Service Pack 1 og oppdateringene KB2533623 og KB2999226.

## Linux og Raspberry Pi

```
tar -xzf YA-NEC-<versjon>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` og `libicu` trengs; de finnes allerede på skrivebordsdistribusjoner. På Alpine: `apk add icu-libs fontconfig libstdc++`. På Raspberry Pi fungerer beregningen, men store modeller (tusenvis av segmenter) krever tid og minne.

## Mac

Pakk ut zip-filen, for eksempel i `~/YA-NEC`. Programmet er ikke signert av Apple, så karantenen må fjernes én gang fra Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Små skjermer

På lave skjermer (for eksempel 1366 x 768) åpnes vinduet maksimert, og resultatfeltene går over til kompakt format.
