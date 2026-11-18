# Diegimas

YA-NEC nediegiama: ją išskleidžiate ir paleidžiate. .NET nereikia, jis jau yra pakete. Nustatymai, modeliai ir žurnalai lieka programos aplanke (`data`, `Registri`): jokio AppData ir jokio registro. Norint pašalinti, pakanka ištrinti aplanką.

## Kuris paketas

Iš puslapio [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistema | Paketas |
|---|---|
| Windows 10/11, 64 bitų | `YA-NEC-<versija>-windows-x64-portatile.zip` |
| Windows 10/11, 32 bitų | `YA-NEC-<versija>-windows-x86-portatile.zip` |
| Windows su ARM (Snapdragon) | `YA-NEC-<versija>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versija>-windows7-x64-portatile.zip` arba `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versija>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bitų ir kitos aarch64 Linux | `YA-NEC-<versija>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bitų, armv7l | `YA-NEC-<versija>-linux-armv7l.tar.gz` |
| Alpine ir musl Linux | `YA-NEC-<versija>-alpine-x64.tar.gz` arba `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versija>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versija>-mac-intel-app.zip` |

Šalia kiekvieno zip yra kontrolinė suma `.sha256` atsisiuntimui patikrinti.

## Windows

Išskleiskite zip į aplanką su rašymo teise, pvz., `C:\YA-NEC` (ne į `C:\Program Files`, kur programa negalėtų išsaugoti nustatymų), ir paleiskite `YA-NEC.exe`. Pirmą kartą paleidus, Windows SmartScreen gali paprašyti patvirtinimo: „Daugiau informacijos" > „Vis tiek paleisti".

**Windows 7 SP1, 8 ir 8.1**: naudokite `win7` paketus, sukurtus su .NET 6 (.NET 8 nepasileidžia Windows 7). Windows 7 reikia Service Pack 1 ir naujinimų KB2533623 bei KB2999226.

## Linux ir Raspberry Pi

```
tar -xzf YA-NEC-<versija>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Reikia `libfontconfig1` ir `libicu`; jie jau yra darbalaukio distribucijose. Alpine sistemoje: `apk add icu-libs fontconfig libstdc++`. Raspberry Pi skaičiavimas veikia, bet dideliems modeliams (tūkstančiai segmentų) reikia laiko ir atminties.

## Mac

Išskleiskite zip, pvz., į `~/YA-NEC`. Programa nepasirašyta Apple, todėl karantiną reikia vieną kartą pašalinti Terminale:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Maži ekranai

Žemuose ekranuose (pvz., 1366 x 768) langas atsidaro išdidintas, o rezultatų laukeliai pereina į kompaktišką formą.
