[Italiano](README.md) · [English](README.en.md) · [Français](README.fr.md) · **Deutsch** · [Español](README.es.md) · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md)

# YA-NEC

**Drahtantennen-Simulator auf Basis von NEC-2** für Windows, Linux und macOS (C#, .NET 8, Avalonia).
Von Costantino Cerrotta **IC8TEM**.

> *Die Geschichte dieses Programms schreibt ihr: mit euren Projekten, euren Ideen, eurer Genialität, euren Entdeckungen und euren Gewissheiten.*

Die Rechenmaschine ist eine C#-Übersetzung von **nec2c** (dem NEC-2 des Lawrence Livermore National Laboratory) und wird bei jeder Version mit nec2c und EZNEC verglichen. Sie öffnet Modelle in **NEC (.nec)**, **MMANA (.maa)**, **AutoEZ (.weq)**, **EZNEC (.ez)**, **YO/AO von K6STI (.yag, .ant)** und im eigenen **.yanec**-Format.

Funktionen: Impedanz, SWR, Gewinn, 2D- und 3D-Diagramme, Frequenz-Sweep und Smith-Diagramm; Leeson-Korrektur für Teleskoprohre (gestufte Durchmesser) wie in EZNEC; Böden (ideal, Reflexionsfaktor, Sommerfeld, Radialnetz); Variablen, Formeln und Optimierer; mehr als 40 Rechner (Lang-Yagi DL6WU, Quagi, LPDA, Moxon, Hexbeam, Mehrband-Dipole und -Yagis, Four-Square, Vertikale, Parabolspiegel und Erreger, Elementhalterungen und mehr); Empfangsantennen und RDF; Rauschen, G/T und Gruppierung (Stack); Kabeltabelle, Anpassleitungen und Baluns; Prüfung der Eingabedaten. Jede Rechenentscheidung und jedes Kontrollkästchen wird im [Handbuch](doc/Manuale_d_uso.pdf) erklärt, das auch im Programm verfügbar ist (Reiter Handbuch). Mehr im [Wiki](https://github.com/ic8tem/YA-NEC/wiki).

**Sprache:** Die Oberfläche gibt es in 37 Sprachen (Menü Sprache, oben rechts), das Wiki ebenfalls. Das Handbuch ist italienisch.

## Neu in 1.1

> **Anmerkung des Autors.** In dieser Version habe ich lange über die Rauschtemperatur nachgedacht. Man hat mich auf MATLAB-Studien zur Stockung von Richtantennen aufmerksam gemacht, und ich wollte es sofort versuchen: den Nutzern von YA-NEC die Möglichkeit geben, über den Stockungsabstand nach DL6WU (die trigonometrische Formel) hinauszugehen, und sie, wenn sie möchten, bei der Wahl eines eigenen Stockungsabstands begleiten. Dazu eine gründliche Überarbeitung der Oberfläche. — *Costantino IC8TEM*

- **Rauschen vom echten Himmel.** Neue Seite im Obertab **Rauschen/GT/Stock**: Das Diagramm der Antenne oder des Stocks wird über die echte Himmelskarte gelegt (408-MHz-Durchmusterung von Haslam, von Remazeilles neu bearbeitet, von G8ONH auf 144 und 432 MHz umgerechnet), vom eigenen QTH (Locator oder Koordinaten) zur gewählten Zeit, mit beliebiger Ausrichtung, auf den Mond oder auf die kälteste Himmelsstelle. Zwei G/T nebeneinander: mit gleichmäßigem Himmel, vergleichbar mit den Tabellen von VE7BQH (30 Grad, 2 x 2-Stock im DL6WU-Abstand), und mit dem echten Himmel, mit der Kurve der Antennentemperatur über 24 Stunden samt Minimum und Maximum. Die Rechnung ist **skynoise von John C. Sager G8ONH** (MIT-Lizenz), nach C# portiert.
- **Stockungsabstand mit NEC gewählt.** Auf der Seite **Stockungsabstände** (aus den Rechnern verschoben) wird der Stock für eine Reihe von Abständen um den DL6WU-Abstand wirklich berechnet: Gewinn, höchste Nebenkeule, G/T und Kopplungsmatrix zwischen den Ports (S11, S21...). Ein Kriterium wählt den vorgeschlagenen Abstand (höchster Gewinn, niedrigste Nebenkeulen, bestes G/T oder Gewinn mit Nebenkeulen unter einer Schwelle), und eine Schaltfläche erstellt damit den Stock. Auf **KW** gilt die DL6WU-Formel nicht: Der Stock über Grund wird nach den gewünschten Abstrahlwinkeln gewählt, mit Elevationsdiagrammen gleichphasig (BIP), einzeln und gegenphasig (BOP).
- **Rechner nach Familien.** Eine Leiste unter den Tabs teilt die Rechner in **KW**, **UKW**, **Empfangsantennen** (kein eigener Tab mehr), **Werkzeuge**, **Spiegel und Erreger** und **2,4 GHz**; die Liste links zeigt nur die gewählte Familie.
- **Messungen im Diagramm.** Neben der -3-dB-Breite: Breite bis zur ersten Nullstelle (FNBW), höchste Nebenkeule (SLL) und zwei Cursor C1 und C2 per Klick, mit Pegel, Winkel und Differenz.
- **V/R entlang der Achse** korrigiert für Freiraummodelle, deren Keule entlang der Z-Achse zeigt (wie die Yagi der MathWorks Antenna Toolbox).
- **Neue Rechner**: Biquad und Doppel-Biquad (Spiegelerreger), Big Wheel und drei Dipole, horizontale Mehrband-Schleife (Skywire) mit Leitungsverlust auf jedem Band.
- **Übersetzungen**: alle neuen Einträge in den 37 Sprachen des Programms. **Videokurs** mit Lektionen zu den neuen Themen aktualisiert.

## Herunterladen

Auf der Seite **[Releases](https://github.com/ic8tem/YA-NEC/releases)** das Zip für die eigene Plattform herunterladen, in einen **beschreibbaren Ordner** entpacken (z. B. `C:\YA-NEC` oder `~/YA-NEC`) und `YA-NEC.exe` starten (Linux und macOS: `YA-NEC`). Eine .NET-Installation ist nicht nötig. Einstellungen und Daten bleiben im Ordner `data` neben dem Programm: kein AppData, keine Registry.

| System | Paket |
|---|---|
| Windows 10/11 64 Bit | `YA-NEC-<version>-win-x64.zip` |
| Windows 10/11 32 Bit | `YA-NEC-<version>-win-x86.zip` |
| Windows auf ARM (Snapdragon) | `YA-NEC-<version>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 (.NET 6) | `YA-NEC-<version>-win7-x64.zip`, `-win7-x86.zip` (Updates KB2533623 und KB2999226 erforderlich) |
| Linux x86_64 | `YA-NEC-<version>-linux-x64.zip` |
| Linux ARM64 (Raspberry Pi OS 64 Bit, andere aarch64) | `YA-NEC-<version>-linux-arm64.zip` |
| Linux ARM 32 Bit (Raspberry Pi OS 32 Bit, armv7l) | `YA-NEC-<version>-linux-arm.zip` |
| Alpine und andere musl-Linux | `YA-NEC-<version>-linux-musl-x64.zip`, `-linux-musl-arm64.zip` |
| macOS Apple Silicon (M1-M4) | `YA-NEC-<version>-osx-arm64.zip` |
| macOS Intel | `YA-NEC-<version>-osx-x64.zip` |

Zu jedem Zip gehört die Prüfsumme `.sha256`.

- **Linux**: nach dem Entpacken bei Bedarf `chmod +x YA-NEC`; benötigt werden `libfontconfig1` und `libicu` (auf Desktop-Distributionen bereits vorhanden).
- **macOS**: Das Programm ist nicht von Apple signiert. Nach dem Entpacken im Terminal: `xattr -dr com.apple.quarantine ~/YA-NEC` (der entpackte Ordner), dann `~/YA-NEC/YA-NEC`.

## Automatische Aktualisierung

Alle 30 Minuten prüft das Programm im Hintergrund, ob es hier eine neue Version gibt. Falls ja, lädt es das Zip seiner Plattform herunter, prüft die SHA-256-Prüfsumme und installiert es: Die neue Version startet **beim nächsten Programmstart**, mit einer Meldung in der Statusleiste. Die vorherige Version bleibt in `data/aggiornamenti/precedente-<version>`; Einstellungen (`data/config.ini`) und Modelle des Benutzers bleiben unberührt. Ohne Prüfsumme oder mit abweichender Prüfsumme wird nichts installiert. Abschalten unter **Einstellungen > Aktualisierung**.

**Ausnahme: Debian/Ubuntu- (.deb) und Fedora/openSUSE-Pakete (.rpm).** Wenn es sie gibt, installieren sie sich mit Administratorrechten in Systemordner; das Programm kann (und darf) sich daher nicht selbst ändern: Sie werden über die Paketverwaltung aktualisiert. In diesen Paketen ist die automatische Aktualisierung abgeschaltet, und die Einstellungen sagen das.

## Aus dem Quellcode kompilieren

.NET 8 SDK erforderlich. Windows: `compila.bat` (Programm in `build\win-x64`), `compila.bat /distribuzione` für die eigenständige Version, `compila_win7.bat` für Windows 7. Linux und macOS: `./compila.sh`. Einzelheiten in [LEGGIMI.txt](LEGGIMI.txt) (italienisch).

## Danksagung und Lizenz

**GPL-3.0** ([LICENSE](LICENSE), [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)), wie nec2c und xnec2c, von denen die Rechenmaschine abstammt. YA-NEC gibt es dank der Arbeit von Funkamateuren und Forschern: Die vollständige Liste steht in [CREDITI.txt](CREDITI.txt) und im Reiter **Danksagung**. Dank an Luis IV3PRK für die kostenlosen Veröffentlichungen auf seiner Website.

## Kontakt

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [Facebook-Gruppe](https://www.facebook.com/groups/1675310160982493) · Spenden: [ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
