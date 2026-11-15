# Instalacja

YA-NEC się nie instaluje: rozpakowuje się go i uruchamia. .NET nie jest potrzebny, jest już w pakiecie. Ustawienia, modele i dzienniki zostają w folderze programu (`data`, `Registri`): bez AppData i bez rejestru. Aby go usunąć, wystarczy skasować folder.

## Który pakiet

Ze strony [Releases](https://github.com/ic8tem/YA-NEC/releases):

| System | Pakiet |
|---|---|
| Windows 10/11 64-bit | `YA-NEC-<wersja>-win-x64.zip` |
| Windows 10/11 32-bit | `YA-NEC-<wersja>-win-x86.zip` |
| Windows na ARM (Snapdragon) | `YA-NEC-<wersja>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<wersja>-win7-x64.zip` lub `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<wersja>-linux-x64.zip` |
| Raspberry Pi OS 64-bit i inne Linuksy aarch64 | `YA-NEC-<wersja>-linux-arm64.zip` |
| Raspberry Pi OS 32-bit, armv7l | `YA-NEC-<wersja>-linux-arm.zip` |
| Alpine i Linuksy z musl | `YA-NEC-<wersja>-linux-musl-x64.zip` lub `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<wersja>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<wersja>-osx-x64.zip` |

Obok każdego zip jest suma kontrolna `.sha256` do sprawdzenia pobranego pliku.

## Windows

Rozpakuj zip do folderu z prawem zapisu, np. `C:\YA-NEC` (nie do `C:\Program Files`, gdzie program nie mógłby zapisać ustawień) i uruchom `YA-NEC.exe`. Przy pierwszym uruchomieniu Windows SmartScreen może poprosić o potwierdzenie: „Więcej informacji" > „Uruchom mimo to".

**Windows 7 SP1, 8 i 8.1**: użyj pakietów `win7`, zbudowanych z .NET 6 (.NET 8 nie uruchamia się w Windows 7). Windows 7 wymaga Service Pack 1 oraz aktualizacji KB2533623 i KB2999226.

## Linux i Raspberry Pi

```
unzip YA-NEC-<wersja>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Potrzebne są `libfontconfig1` i `libicu`, obecne już w dystrybucjach z pulpitem. W Alpine: `apk add icu-libs fontconfig libstdc++`. Na Raspberry Pi obliczenia działają, ale duże modele (tysiące segmentów) wymagają czasu i pamięci.

## Mac

Rozpakuj zip, np. do `~/YA-NEC`. Program nie jest podpisany przez Apple, więc trzeba raz zdjąć kwarantannę w Terminalu:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Małe ekrany

Na niskich ekranach (np. 1366 x 768) okno otwiera się zmaksymalizowane, a pola wyników przechodzą w format kompaktowy.
