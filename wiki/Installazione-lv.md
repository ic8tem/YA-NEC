# Instalēšana

YA-NEC netiek instalēta: to izpako un palaiž. .NET nav vajadzīgs, tas jau ir pakotnē. Iestatījumi, modeļi un žurnāli paliek programmas mapē (`data`, `Registri`): nekādas AppData, nekāda reģistra. Lai to noņemtu, pietiek izdzēst mapi.

## Kura pakotne

No lapas [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistēma | Pakotne |
|---|---|
| Windows 10/11, 64 bitu | `YA-NEC-<versija>-windows-x64-portatile.zip` |
| Windows 10/11, 32 bitu | `YA-NEC-<versija>-windows-x86-portatile.zip` |
| Windows uz ARM (Snapdragon) | `YA-NEC-<versija>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versija>-windows7-x64-portatile.zip` vai `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versija>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bitu un citas aarch64 Linux | `YA-NEC-<versija>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bitu, armv7l | `YA-NEC-<versija>-linux-armv7l.tar.gz` |
| Alpine un musl Linux | `YA-NEC-<versija>-alpine-x64.tar.gz` vai `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versija>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versija>-mac-intel-app.zip` |

Blakus katram zip ir kontrolsumma `.sha256` lejupielādes pārbaudei.

## Windows

Izpakojiet zip mapē ar rakstīšanas tiesībām, piemēram, `C:\YA-NEC` (nevis `C:\Program Files`, kur programma nevarētu saglabāt iestatījumus), un palaidiet `YA-NEC.exe`. Pirmajā palaišanas reizē Windows SmartScreen var lūgt apstiprinājumu: „Vairāk informācijas" > „Tik un tā palaist".

**Windows 7 SP1, 8 un 8.1**: izmantojiet `win7` pakotnes, kas veidotas ar .NET 6 (.NET 8 nedarbojas Windows 7). Windows 7 vajag Service Pack 1 un atjauninājumus KB2533623 un KB2999226.

## Linux un Raspberry Pi

```
tar -xzf YA-NEC-<versija>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Vajadzīgi `libfontconfig1` un `libicu`, kas darbvirsmas distribūcijās jau ir. Alpine sistēmā: `apk add icu-libs fontconfig libstdc++`. Uz Raspberry Pi aprēķini darbojas, taču lieliem modeļiem (tūkstošiem segmentu) vajag laiku un atmiņu.

## Mac

Izpakojiet zip, piemēram, mapē `~/YA-NEC`. Programmu nav parakstījis Apple, tāpēc karantīna vienreiz jānoņem Terminālī:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Mazi ekrāni

Zemos ekrānos (piemēram, 1366 x 768) logs atveras maksimizēts, un rezultātu lauki pāriet kompaktā formā.
