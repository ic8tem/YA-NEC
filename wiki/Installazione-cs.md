# Instalace

YA-NEC se neinstaluje: rozbalí se a spustí. .NET není potřeba, je už v balíčku. Nastavení, modely a protokoly zůstávají ve složce programu (`data`, `Registri`): žádné AppData, žádný registr. Pro odstranění stačí smazat složku.

## Jaký balíček

Ze stránky [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Systém | Balíček |
|---|---|
| Windows 10/11 64bitové | `YA-NEC-<verze>-win-x64.zip` |
| Windows 10/11 32bitové | `YA-NEC-<verze>-win-x86.zip` |
| Windows na ARM (Snapdragon) | `YA-NEC-<verze>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<verze>-win7-x64.zip` nebo `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<verze>-linux-x64.zip` |
| Raspberry Pi OS 64bitový a jiné Linuxy aarch64 | `YA-NEC-<verze>-linux-arm64.zip` |
| Raspberry Pi OS 32bitový, armv7l | `YA-NEC-<verze>-linux-arm.zip` |
| Alpine a Linuxy s musl | `YA-NEC-<verze>-linux-musl-x64.zip` nebo `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<verze>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<verze>-osx-x64.zip` |

Vedle každého zipu je kontrolní součet `.sha256` pro ověření stažení.

## Windows

Rozbalte zip do složky s právem zápisu, např. `C:\YA-NEC` (ne do `C:\Program Files`, kam by program nemohl ukládat nastavení), a spusťte `YA-NEC.exe`. Při prvním spuštění může Windows SmartScreen žádat potvrzení: „Další informace" > „Přesto spustit".

**Windows 7 SP1, 8 a 8.1**: použijte balíčky `win7`, sestavené s .NET 6 (.NET 8 se ve Windows 7 nespustí). Windows 7 potřebují Service Pack 1 a aktualizace KB2533623 a KB2999226.

## Linux a Raspberry Pi

```
unzip YA-NEC-<verze>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Potřebné jsou `libfontconfig1` a `libicu`, které už desktopové distribuce mají. V Alpine: `apk add icu-libs fontconfig libstdc++`. Na Raspberry Pi výpočet funguje, ale velké modely (tisíce segmentů) potřebují čas a paměť.

## Mac

Rozbalte zip, např. do `~/YA-NEC`. Program není podepsán společností Apple, proto je třeba jednou v Terminálu odstranit karanténu:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Malé obrazovky

Na nízkých obrazovkách (např. 1366 x 768) se okno otevře maximalizované a pole výsledků přejdou do kompaktní podoby.
