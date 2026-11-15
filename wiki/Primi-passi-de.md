# Erste Schritte

1. **Beispiele** (Befehlsleiste) öffnet eines der fertigen Modelle: Dipole, Groundplane, VHF-Yagis (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), HF-Yagis (Force 12 XR-5, ARRL 520-40H), Hexbeam, Four-Square, Loop-Yagi für 1296 MHz und weitere. **Modell öffnen** öffnet eine NEC-, MMANA-, AutoEZ-, EZNEC- oder K6STI-Datei; man kann die Datei auch auf das Fenster ziehen.
2. Das **Band** wählen (von 2200 m bis 3 cm, Grenzen der IARU Region 1) oder eine freie Frequenz, dann **Berechnen**.
3. Die **Ergebnisfelder** zeigen Impedanz, SWR, Gewinn, F/B, F/R, Öffnungswinkel bei -3 dB, Wirkungsgrad und AGT (Konvergenztest: nahe 0 dB ist das Modell zuverlässig).
4. **Sweep** berechnet das Band um die Frequenz: SWR, R/X, Gewinn, F/B und Smith-Diagramm.

## Die Reiter

- **3D-Struktur**: die Antenne (ziehen = drehen, Mausrad = Zoom, rechte Taste = verschieben, Doppelklick = Ausgangsansicht), mit dem Boden nach Art und Höhe gezeichnet. Darunter die **Drahttabelle** im Stil einer Tabellenkalkulation: Die Zellen akzeptieren Zahlen, Variablen und Formeln. Unten rechts die **Modellobjekte**: Quellen, Lasten, Leitungen, Transformatoren.
- **Diagramme**: horizontal, vertikal und 3D, mit dem -3-dB-Winkel.
- **Frequenz, Smith, Ströme**: Ergebnisse des Sweeps und Ströme auf den Drähten.
- **NEC-Modell**: die NEC-Karten, von Hand bearbeitbar.
- **Variablen** und **Optimierer**: Man gibt den Maßen einen Namen und lässt das Programm sie ändern, bis ein Ziel erreicht ist (SWR, Gewinn, F/B, Impedanz...). Das Handbuch hat Beispiele Schritt für Schritt.
- **Rechner**: siehe [Rechner](Calcolatori-de).
- **Empfangsantennen** und **Rauschen/GT/Stack**: RDF der Empfangsantennen, Antennentemperatur und G/T (Methode von TANT und XLGTa, Referenztemperaturen von VE7BQH und DG7YBN), Gruppierung.
- **Warnungen**: alles Zweifelhafte, was das Programm im Modell gefunden hat (zu kurze oder zu lange Segmente, Drähte nahe am Boden, überfüllte Verbindungspunkte). Die Schaltfläche **Prüfen...** in der oberen Leiste prüft 42 typische Eingabefehler, ohne zu rechnen.
- **Einstellungen**, **Handbuch**, **Danksagung**. Oben rechts: **Sprache**, **Farben** (18 Schemata) und **Info**.

## Speichern

**Speichern** und **Speichern unter** schreiben das Modell als NEC, MMANA, AutoEZ, EZNEC oder im eigenen **.yanec**-Format, das auch die Beschreibung und die Originaldatei behält.
