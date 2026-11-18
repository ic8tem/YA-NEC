# Inštalácia

YA-NEC sa neinštaluje: rozbalí sa a spustí. .NET netreba, je už v balíku. Nastavenia, modely a protokoly zostávajú v priečinku programu (`data`, `Registri`): žiadne AppData, žiadny register. Na odstránenie stačí zmazať priečinok.

## Aký balík

Zo stránky [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Systém | Balík |
|---|---|
| Windows 10/11 64-bitové | `YA-NEC-<verzia>-windows-x64-portatile.zip` |
| Windows 10/11 32-bitové | `YA-NEC-<verzia>-windows-x86-portatile.zip` |
| Windows na ARM (Snapdragon) | `YA-NEC-<verzia>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<verzia>-windows7-x64-portatile.zip` alebo `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<verzia>-linux-x64.tar.gz` |
| Raspberry Pi OS 64-bitový a iné Linuxy aarch64 | `YA-NEC-<verzia>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32-bitový, armv7l | `YA-NEC-<verzia>-linux-armv7l.tar.gz` |
| Alpine a Linuxy s musl | `YA-NEC-<verzia>-alpine-x64.tar.gz` alebo `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<verzia>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<verzia>-mac-intel-app.zip` |

Vedľa každého zipu je kontrolný súčet `.sha256` na overenie stiahnutia.

## Windows

Rozbaľte zip do priečinka s právom zápisu, napr. `C:\YA-NEC` (nie do `C:\Program Files`, kam by program nemohol ukladať nastavenia), a spustite `YA-NEC.exe`. Pri prvom spustení môže Windows SmartScreen žiadať potvrdenie: „Ďalšie informácie" > „Spustiť aj tak".

**Windows 7 SP1, 8 a 8.1**: použite balíky `win7`, zostavené s .NET 6 (.NET 8 sa vo Windows 7 nespustí). Windows 7 potrebuje Service Pack 1 a aktualizácie KB2533623 a KB2999226.

## Linux a Raspberry Pi

```
tar -xzf YA-NEC-<verzia>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Potrebné sú `libfontconfig1` a `libicu`, ktoré desktopové distribúcie už majú. V Alpine: `apk add icu-libs fontconfig libstdc++`. Na Raspberry Pi výpočet funguje, ale veľké modely (tisíce segmentov) potrebujú čas a pamäť.

## Mac

Rozbaľte zip, napr. do `~/YA-NEC`. Program nie je podpísaný spoločnosťou Apple, preto treba raz v Termináli odstrániť karanténu:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Malé obrazovky

Na nízkych obrazovkách (napr. 1366 x 768) sa okno otvorí maximalizované a polia výsledkov prejdú do kompaktnej podoby.
