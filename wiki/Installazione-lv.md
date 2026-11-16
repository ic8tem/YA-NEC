# Instalēšana

YA-NEC netiek instalēta: to izpako un palaiž. .NET nav vajadzīgs, tas jau ir pakotnē. Iestatījumi, modeļi un žurnāli paliek programmas mapē (`data`, `Registri`): nekādas AppData, nekāda reģistra. Lai to noņemtu, pietiek izdzēst mapi.

## Kura pakotne

No lapas [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistēma | Pakotne |
|---|---|
| Windows 10/11, 64 bitu | `YA-NEC-<versija>-win-x64.zip` |
| Windows 10/11, 32 bitu | `YA-NEC-<versija>-win-x86.zip` |
| Windows uz ARM (Snapdragon) | `YA-NEC-<versija>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versija>-win7-x64.zip` vai `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<versija>-linux-x64.zip` |
| Raspberry Pi OS 64 bitu un citas aarch64 Linux | `YA-NEC-<versija>-linux-arm64.zip` |
| Raspberry Pi OS 32 bitu, armv7l | `YA-NEC-<versija>-linux-arm.zip` |
| Alpine un musl Linux | `YA-NEC-<versija>-linux-musl-x64.zip` vai `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versija>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<versija>-osx-x64.zip` |

Blakus katram zip ir kontrolsumma `.sha256` lejupielādes pārbaudei.

## Windows

Izpakojiet zip mapē ar rakstīšanas tiesībām, piemēram, `C:\YA-NEC` (nevis `C:\Program Files`, kur programma nevarētu saglabāt iestatījumus), un palaidiet `YA-NEC.exe`. Pirmajā palaišanas reizē Windows SmartScreen var lūgt apstiprinājumu: „Vairāk informācijas" > „Tik un tā palaist".

**Windows 7 SP1, 8 un 8.1**: izmantojiet `win7` pakotnes, kas veidotas ar .NET 6 (.NET 8 nedarbojas Windows 7). Windows 7 vajag Service Pack 1 un atjauninājumus KB2533623 un KB2999226.

## Linux un Raspberry Pi

```
unzip YA-NEC-<versija>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Vajadzīgi `libfontconfig1` un `libicu`, kas darbvirsmas distribūcijās jau ir. Alpine sistēmā: `apk add icu-libs fontconfig libstdc++`. Uz Raspberry Pi aprēķini darbojas, taču lieliem modeļiem (tūkstošiem segmentu) vajag laiku un atmiņu.

## Mac

Izpakojiet zip, piemēram, mapē `~/YA-NEC`. Programmu nav parakstījis Apple, tāpēc karantīna vienreiz jānoņem Terminālī:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Mazi ekrāni

Zemos ekrānos (piemēram, 1366 x 768) logs atveras maksimizēts, un rezultātu lauki pāriet kompaktā formā.
