# Installering

YA-NEC word nie geïnstalleer nie: jy pak dit uit en begin dit. .NET is nie nodig nie, dit is reeds in die pakket. Instellings, modelle en logboeke bly in die programvouer (`data`, `Registri`): geen AppData, geen register nie. Om dit te verwyder, vee jy net die vouer uit.

## Watter pakket

Van die bladsy [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Stelsel | Pakket |
|---|---|
| Windows 10/11 64-bis | `YA-NEC-<weergawe>-win-x64.zip` |
| Windows 10/11 32-bis | `YA-NEC-<weergawe>-win-x86.zip` |
| Windows op ARM (Snapdragon) | `YA-NEC-<weergawe>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<weergawe>-win7-x64.zip` of `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<weergawe>-linux-x64.zip` |
| Raspberry Pi OS 64-bis en ander aarch64-Linux | `YA-NEC-<weergawe>-linux-arm64.zip` |
| Raspberry Pi OS 32-bis, armv7l | `YA-NEC-<weergawe>-linux-arm.zip` |
| Alpine en musl-Linux | `YA-NEC-<weergawe>-linux-musl-x64.zip` of `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<weergawe>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<weergawe>-osx-x64.zip` |

Langs elke zip is die kontrolesom `.sha256` om die aflaai na te gaan.

## Windows

Pak die zip uit in 'n vouer met skryfreg, byvoorbeeld `C:\YA-NEC` (nie in `C:\Program Files` nie, waar die program sy instellings nie kan stoor nie), en begin `YA-NEC.exe`. Met die eerste begin kan Windows SmartScreen om bevestiging vra: "More info" > "Run anyway".

**Windows 7 SP1, 8 en 8.1**: gebruik die `win7`-pakkette, gebou met .NET 6 (.NET 8 begin nie op Windows 7 nie). Windows 7 het Service Pack 1 en die opdaterings KB2533623 en KB2999226 nodig.

## Linux en Raspberry Pi

```
unzip YA-NEC-<weergawe>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` en `libicu` is nodig; hulle is reeds op lessenaarverspreidings. Op Alpine: `apk add icu-libs fontconfig libstdc++`. Op die Raspberry Pi werk die berekening, maar groot modelle (duisende segmente) het tyd en geheue nodig.

## Mac

Pak die zip uit, byvoorbeeld in `~/YA-NEC`. Die program is nie deur Apple onderteken nie, dus moet die kwarantyn een keer in Terminal verwyder word:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Klein skerms

Op lae skerms (byvoorbeeld 1366 x 768) maak die venster gemaksimeer oop en skakel die resultaatblokke na die kompakte formaat.
