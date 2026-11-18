# Telepítés

A YA-NEC-et nem kell telepíteni: kicsomagolod és elindítod. .NET nem kell, már benne van a csomagban. A beállítások, modellek és naplók a program mappájában maradnak (`data`, `Registri`): nincs AppData, nincs rendszerleíró adatbázis. Eltávolításhoz elég törölni a mappát.

## Melyik csomag

A [Releases](https://github.com/ic8tem/YA-NEC/releases) oldalról:

| Rendszer | Csomag |
|---|---|
| Windows 10/11 64 bites | `YA-NEC-<verzió>-windows-x64-portatile.zip` |
| Windows 10/11 32 bites | `YA-NEC-<verzió>-windows-x86-portatile.zip` |
| Windows ARM-on (Snapdragon) | `YA-NEC-<verzió>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<verzió>-windows7-x64-portatile.zip` vagy `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<verzió>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bites és más aarch64 Linux | `YA-NEC-<verzió>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bites, armv7l | `YA-NEC-<verzió>-linux-armv7l.tar.gz` |
| Alpine és musl alapú Linux | `YA-NEC-<verzió>-alpine-x64.tar.gz` vagy `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<verzió>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<verzió>-mac-intel-app.zip` |

Minden zip mellett ott van a `.sha256` ellenőrzőösszeg a letöltés ellenőrzéséhez.

## Windows

Csomagold ki a zipet egy írható mappába, például `C:\YA-NEC` (ne a `C:\Program Files` mappába, ahová a program nem tudná menteni a beállításait), és indítsd el a `YA-NEC.exe` fájlt. Első indításkor a Windows SmartScreen megerősítést kérhet: „További információ" > „Futtatás mindenképp".

**Windows 7 SP1, 8 és 8.1**: a .NET 6-tal készült `win7` csomagokat használd (a .NET 8 nem indul Windows 7-en). Windows 7-hez a Service Pack 1 és a KB2533623, KB2999226 frissítések kellenek.

## Linux és Raspberry Pi

```
tar -xzf YA-NEC-<verzió>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

A `libfontconfig1` és a `libicu` szükséges; az asztali disztribúciókban ezek már megvannak. Alpine-on: `apk add icu-libs fontconfig libstdc++`. Raspberry Pi-n a számítás működik, de a nagy modellek (több ezer szegmens) időt és memóriát igényelnek.

## Mac

Csomagold ki a zipet, például ide: `~/YA-NEC`. A programot nem írta alá az Apple, ezért a karantént egyszer el kell távolítani a Terminálban:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Kis képernyők

Alacsony képernyőn (például 1366 x 768) az ablak teljes méretben nyílik meg, és az eredménymezők tömör formára váltanak.
