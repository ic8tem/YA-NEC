# Dateiformate

| Format | Öffnen | Speichern | Hinweise |
|---|---|---|---|
| NEC-2 (.nec) | ja | ja | Karten GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (Variablen und Formeln wie 4nec2 und xnec2c) |
| MMANA (.maa) | ja | ja | die Segmentierung wird für NEC-2 neu berechnet (MMANA nutzt einen MININEC-Kern) |
| AutoEZ (.weq) | ja | ja | Variablen und Excel-Formeln |
| EZNEC (.ez) | ja | ja | Drähte, Quellen, Lasten, Leitungen, Boden; MININEC-Boden wird mit einer Warnung als GN 3 gelesen |
| YO / AO von K6STI (.yag, .ant) | ja | nein | |
| YA-NEC (.yanec) | ja | ja | das Modell im Originalformat, mit Name, Beschreibung und Kategorie |

Die .yanec-Datei enthält das Modell **ohne Umwandlung** (NEC-Karten unverändert, Text der .maa, .weq, .yag, .ant; die .ez in Base64): YA-NEC liest sie mit demselben Importer wie die Originaldatei, daher sind die Ergebnisse mit denen der Originaldatei identisch.

Alle Einzelheiten in Kapitel 10 des Handbuchs.
