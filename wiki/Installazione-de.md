# Installation

YA-NEC wird nicht installiert: Man entpackt es und startet es. .NET ist nicht nötig, es steckt bereits im Paket. Einstellungen, Modelle und Protokolle bleiben im Programmordner (`data`, `Registri`): kein AppData, keine Registry. Zum Entfernen genügt es, den Ordner zu löschen.

## Welches Paket

Von der Seite [Releases](https://github.com/ic8tem/YA-NEC/releases):

| System | Paket |
|---|---|
| Windows 10/11 64 Bit | `YA-NEC-<version>-win-x64.zip` |
| Windows 10/11 32 Bit | `YA-NEC-<version>-win-x86.zip` |
| Windows auf ARM (Snapdragon) | `YA-NEC-<version>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<version>-win7-x64.zip` oder `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<version>-linux-x64.zip` |
| Raspberry Pi OS 64 Bit und andere aarch64-Linux | `YA-NEC-<version>-linux-arm64.zip` |
| Raspberry Pi OS 32 Bit, armv7l | `YA-NEC-<version>-linux-arm.zip` |
| Alpine und musl-Linux | `YA-NEC-<version>-linux-musl-x64.zip` oder `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<version>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<version>-osx-x64.zip` |

Neben jedem Zip liegt die Prüfsumme `.sha256`, um den Download zu prüfen.

## Windows

Das Zip in einen beschreibbaren Ordner entpacken, z. B. `C:\YA-NEC` (nicht in `C:\Programme`, wo das Programm seine Einstellungen nicht speichern könnte), und `YA-NEC.exe` starten. Beim ersten Start fragt Windows SmartScreen eventuell nach: „Weitere Informationen" > „Trotzdem ausführen".

**Windows 7 SP1, 8 und 8.1**: die `win7`-Pakete verwenden, gebaut mit .NET 6 (.NET 8 startet nicht unter Windows 7). Windows 7 braucht das Service Pack 1 und die Updates KB2533623 und KB2999226.

## Linux und Raspberry Pi

```
unzip YA-NEC-<version>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Benötigt werden `libfontconfig1` und `libicu`, die auf Desktop-Distributionen bereits vorhanden sind. Unter Alpine: `apk add icu-libs fontconfig libstdc++`. Auf dem Raspberry Pi funktioniert die Berechnung, große Modelle (Tausende Segmente) brauchen aber Zeit und Speicher.

## Mac

Das Zip entpacken, z. B. nach `~/YA-NEC`. Das Programm ist nicht von Apple signiert, daher muss die Quarantäne einmal im Terminal entfernt werden:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Kleine Bildschirme

Auf niedrigen Bildschirmen (z. B. 1366 x 768) öffnet sich das Fenster maximiert und die Ergebnisfelder wechseln ins kompakte Format.
