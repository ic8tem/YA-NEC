# Paigaldamine

YA-NEC-i ei paigaldata: see pakitakse lahti ja käivitatakse. .NET-i pole vaja, see on juba paketis. Seaded, mudelid ja logid jäävad programmi kausta (`data`, `Registri`): ei mingit AppData-t ega registrit. Eemaldamiseks piisab kausta kustutamisest.

## Milline pakett

Lehelt [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Süsteem | Pakett |
|---|---|
| Windows 10/11 64-bitine | `YA-NEC-<versioon>-windows-x64-portatile.zip` |
| Windows 10/11 32-bitine | `YA-NEC-<versioon>-windows-x86-portatile.zip` |
| Windows ARM-il (Snapdragon) | `YA-NEC-<versioon>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versioon>-windows7-x64-portatile.zip` või `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versioon>-linux-x64.tar.gz` |
| Raspberry Pi OS 64-bitine ja muud aarch64 Linuxid | `YA-NEC-<versioon>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32-bitine, armv7l | `YA-NEC-<versioon>-linux-armv7l.tar.gz` |
| Alpine ja musl-Linuxid | `YA-NEC-<versioon>-alpine-x64.tar.gz` või `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versioon>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versioon>-mac-intel-app.zip` |

Iga zipi kõrval on kontrollsumma `.sha256` allalaadimise kontrollimiseks.

## Windows

Pakkige zip lahti kirjutusõigusega kausta, nt `C:\YA-NEC` (mitte kausta `C:\Program Files`, kuhu programm ei saaks oma seadeid salvestada), ja käivitage `YA-NEC.exe`. Esimesel käivitamisel võib Windows SmartScreen küsida kinnitust: „Lisateave" > „Käivita ikkagi".

**Windows 7 SP1, 8 ja 8.1**: kasutage `win7` pakette, mis on ehitatud .NET 6-ga (.NET 8 ei käivitu Windows 7-s). Windows 7 vajab Service Pack 1-te ning uuendusi KB2533623 ja KB2999226.

## Linux ja Raspberry Pi

```
tar -xzf YA-NEC-<versioon>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Vaja on `libfontconfig1` ja `libicu`, mis on töölauaga distributsioonides juba olemas. Alpine'is: `apk add icu-libs fontconfig libstdc++`. Raspberry Pi-l arvutamine töötab, kuid suured mudelid (tuhanded segmendid) vajavad aega ja mälu.

## Mac

Pakkige zip lahti, nt kausta `~/YA-NEC`. Programm pole Apple'i allkirjastatud, seega tuleb karantiin Terminalis üks kord eemaldada:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Väikesed ekraanid

Madalatel ekraanidel (nt 1366 x 768) avaneb aken maksimeeritult ja tulemuskastid lähevad kompaktsesse vormi.
