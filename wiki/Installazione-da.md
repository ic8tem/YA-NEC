# Installation

YA-NEC installeres ikke: man pakker det ud og starter det. .NET er ikke nødvendigt, det er allerede i pakken. Indstillinger, modeller og logfiler bliver i programmappen (`data`, `Registri`): ingen AppData, ingen registreringsdatabase. For at fjerne det sletter man bare mappen.

## Hvilken pakke

Fra siden [Releases](https://github.com/ic8tem/YA-NEC/releases):

| System | Pakke |
|---|---|
| Windows 10/11 64-bit | `YA-NEC-<version>-win-x64.zip` |
| Windows 10/11 32-bit | `YA-NEC-<version>-win-x86.zip` |
| Windows på ARM (Snapdragon) | `YA-NEC-<version>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<version>-win7-x64.zip` eller `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<version>-linux-x64.zip` |
| Raspberry Pi OS 64-bit og andre aarch64-Linux | `YA-NEC-<version>-linux-arm64.zip` |
| Raspberry Pi OS 32-bit, armv7l | `YA-NEC-<version>-linux-arm.zip` |
| Alpine og musl-Linux | `YA-NEC-<version>-linux-musl-x64.zip` eller `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<version>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<version>-osx-x64.zip` |

Ved siden af hver zip ligger kontrolsummen `.sha256` til at kontrollere overførslen.

## Windows

Pak zip-filen ud i en mappe med skriveadgang, f.eks. `C:\YA-NEC` (ikke i `C:\Programmer`, hvor programmet ikke kan gemme sine indstillinger), og start `YA-NEC.exe`. Ved første start kan Windows SmartScreen bede om bekræftelse: "Flere oplysninger" > "Kør alligevel".

**Windows 7 SP1, 8 og 8.1**: brug `win7`-pakkerne, bygget med .NET 6 (.NET 8 starter ikke på Windows 7). Windows 7 kræver Service Pack 1 og opdateringerne KB2533623 og KB2999226.

## Linux og Raspberry Pi

```
unzip YA-NEC-<version>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` og `libicu` er nødvendige; de findes allerede på desktopdistributioner. På Alpine: `apk add icu-libs fontconfig libstdc++`. På Raspberry Pi virker beregningen, men store modeller (tusindvis af segmenter) kræver tid og hukommelse.

## Mac

Pak zip-filen ud, f.eks. i `~/YA-NEC`. Programmet er ikke signeret af Apple, så karantænen skal fjernes én gang fra Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Små skærme

På lave skærme (f.eks. 1366 x 768) åbner vinduet maksimeret, og resultatfelterne skifter til kompakt format.
