# Installatie

YA-NEC wordt niet geïnstalleerd: je pakt het uit en start het. .NET is niet nodig, het zit al in het pakket. Instellingen, modellen en logboeken blijven in de programmamap (`data`, `Registri`): geen AppData, geen register. Verwijderen = de map wissen.

## Welk pakket

Van de pagina [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Systeem | Pakket |
|---|---|
| Windows 10/11 64-bit | `YA-NEC-<versie>-windows-x64-portatile.zip` |
| Windows 10/11 32-bit | `YA-NEC-<versie>-windows-x86-portatile.zip` |
| Windows op ARM (Snapdragon) | `YA-NEC-<versie>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versie>-windows7-x64-portatile.zip` of `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versie>-linux-x64.tar.gz` |
| Raspberry Pi OS 64-bit en andere aarch64-Linux | `YA-NEC-<versie>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32-bit, armv7l | `YA-NEC-<versie>-linux-armv7l.tar.gz` |
| Alpine en musl-Linux | `YA-NEC-<versie>-alpine-x64.tar.gz` of `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versie>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versie>-mac-intel-app.zip` |

Naast elk zip-bestand staat de controlesom `.sha256` om de download te controleren.

## Windows

Pak het zip-bestand uit in een beschrijfbare map, bijvoorbeeld `C:\YA-NEC` (niet in `C:\Program Files`, waar het programma zijn instellingen niet kan opslaan) en start `YA-NEC.exe`. Bij de eerste start kan Windows SmartScreen om bevestiging vragen: "Meer informatie" > "Toch uitvoeren".

**Windows 7 SP1, 8 en 8.1**: gebruik de `win7`-pakketten, gebouwd met .NET 6 (.NET 8 start niet op Windows 7). Windows 7 heeft Service Pack 1 en de updates KB2533623 en KB2999226 nodig.

## Linux en Raspberry Pi

```
tar -xzf YA-NEC-<versie>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` en `libicu` zijn nodig; die zijn al aanwezig op desktopdistributies. Op Alpine: `apk add icu-libs fontconfig libstdc++`. Op de Raspberry Pi werkt de berekening, maar grote modellen (duizenden segmenten) vragen tijd en geheugen.

## Mac

Pak het zip-bestand uit, bijvoorbeeld in `~/YA-NEC`. Het programma is niet door Apple ondertekend, dus de quarantaine moet één keer worden opgeheven, in Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Kleine schermen

Op lage schermen (bijvoorbeeld 1366 x 768) opent het venster gemaximaliseerd en schakelen de resultaatvakken naar de compacte vorm.
