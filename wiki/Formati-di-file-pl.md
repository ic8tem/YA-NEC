# Formaty plików

| Format | Otwiera | Zapisuje | Uwagi |
|---|---|---|---|
| NEC-2 (.nec) | tak | tak | karty GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (zmienne i wzory jak w 4nec2 i xnec2c) |
| MMANA (.maa) | tak | tak | segmentacja jest liczona na nowo dla NEC-2 (MMANA używa silnika MININEC) |
| AutoEZ (.weq) | tak | tak | zmienne i wzory Excela |
| EZNEC (.ez) | tak | tak | przewody, źródła, obciążenia, linie, grunt; grunt MININEC jest czytany jako GN 3 z ostrzeżeniem |
| YO / AO K6STI (.yag, .ant) | tak | nie | |
| YA-NEC (.yanec) | tak | tak | model w oryginalnym formacie, z nazwą, opisem i kategorią |

Plik .yanec zawiera model **bez konwersji** (karty NEC bez zmian, tekst .maa, .weq, .yag, .ant; .ez w base64): YA-NEC czyta go tym samym importerem co plik oryginalny, więc wyniki są takie same jak dla oryginału.

Wszystkie szczegóły w rozdziale 10 instrukcji.
