# Installation

YA-NEC installeras inte: du packar upp det och startar det. .NET behövs inte, det finns redan i paketet. Inställningar, modeller och loggar stannar i programmappen (`data`, `Registri`): ingen AppData, inget register. För att ta bort det raderar du bara mappen.

## Vilket paket

Från sidan [Releases](https://github.com/ic8tem/YA-NEC/releases):

| System | Paket |
|---|---|
| Windows 10/11 64-bitars | `YA-NEC-<version>-windows-x64-portatile.zip` |
| Windows 10/11 32-bitars | `YA-NEC-<version>-windows-x86-portatile.zip` |
| Windows på ARM (Snapdragon) | `YA-NEC-<version>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<version>-windows7-x64-portatile.zip` eller `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<version>-linux-x64.tar.gz` |
| Raspberry Pi OS 64-bitars och andra aarch64-Linux | `YA-NEC-<version>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32-bitars, armv7l | `YA-NEC-<version>-linux-armv7l.tar.gz` |
| Alpine och musl-Linux | `YA-NEC-<version>-alpine-x64.tar.gz` eller `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<version>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<version>-mac-intel-app.zip` |

Bredvid varje zip finns kontrollsumman `.sha256` för att kontrollera nedladdningen.

## Windows

Packa upp zip-filen i en mapp med skrivrättighet, till exempel `C:\YA-NEC` (inte i `C:\Program Files`, där programmet inte kan spara sina inställningar), och starta `YA-NEC.exe`. Vid första starten kan Windows SmartScreen be om bekräftelse: "Mer information" > "Kör ändå".

**Windows 7 SP1, 8 och 8.1**: använd `win7`-paketen, byggda med .NET 6 (.NET 8 startar inte på Windows 7). Windows 7 behöver Service Pack 1 och uppdateringarna KB2533623 och KB2999226.

## Linux och Raspberry Pi

```
tar -xzf YA-NEC-<version>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` och `libicu` behövs; de finns redan på skrivbordsdistributioner. På Alpine: `apk add icu-libs fontconfig libstdc++`. På Raspberry Pi fungerar beräkningen, men stora modeller (tusentals segment) kräver tid och minne.

## Mac

Packa upp zip-filen, till exempel i `~/YA-NEC`. Programmet är inte signerat av Apple, så karantänen måste tas bort en gång i Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Små skärmar

På låga skärmar (till exempel 1366 x 768) öppnas fönstret maximerat och resultatrutorna växlar till kompakt format.
