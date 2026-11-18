# Instalace

YA-NEC se neinstaluje: rozbalí se a spustí. .NET není potřeba, je už v balíčku. Nastavení, modely a protokoly zůstávají ve složce programu (`data`, `Registri`): žádné AppData, žádný registr. Pro odstranění stačí smazat složku.

## Jaký balíček

Ze stránky [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Systém | Balíček |
|---|---|
| Windows 10/11 64bitové | `YA-NEC-<verze>-windows-x64-portatile.zip` |
| Windows 10/11 32bitové | `YA-NEC-<verze>-windows-x86-portatile.zip` |
| Windows na ARM (Snapdragon) | `YA-NEC-<verze>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<verze>-windows7-x64-portatile.zip` nebo `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<verze>-linux-x64.tar.gz` |
| Raspberry Pi OS 64bitový a jiné Linuxy aarch64 | `YA-NEC-<verze>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32bitový, armv7l | `YA-NEC-<verze>-linux-armv7l.tar.gz` |
| Alpine a Linuxy s musl | `YA-NEC-<verze>-alpine-x64.tar.gz` nebo `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<verze>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<verze>-mac-intel-app.zip` |

Vedle každého zipu je kontrolní součet `.sha256` pro ověření stažení.

## Windows

Rozbalte zip do složky s právem zápisu, např. `C:\YA-NEC` (ne do `C:\Program Files`, kam by program nemohl ukládat nastavení), a spusťte `YA-NEC.exe`. Při prvním spuštění může Windows SmartScreen žádat potvrzení: „Další informace" > „Přesto spustit".

**Windows 7 SP1, 8 a 8.1**: použijte balíčky `win7`, sestavené s .NET 6 (.NET 8 se ve Windows 7 nespustí). Windows 7 potřebují Service Pack 1 a aktualizace KB2533623 a KB2999226.

## Linux a Raspberry Pi

```
tar -xzf YA-NEC-<verze>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Potřebné jsou `libfontconfig1` a `libicu`, které už desktopové distribuce mají. V Alpine: `apk add icu-libs fontconfig libstdc++`. Na Raspberry Pi výpočet funguje, ale velké modely (tisíce segmentů) potřebují čas a paměť.

## Mac

Rozbalte zip, např. do `~/YA-NEC`. Program není podepsán společností Apple, proto je třeba jednou v Terminálu odstranit karanténu:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Malé obrazovky

Na nízkých obrazovkách (např. 1366 x 768) se okno otevře maximalizované a pole výsledků přejdou do kompaktní podoby.
