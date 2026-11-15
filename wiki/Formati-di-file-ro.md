# Formate de fișiere

| Format | Deschide | Salvează | Note |
|---|---|---|---|
| NEC-2 (.nec) | da | da | cartele GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variabile și formule ca în 4nec2 și xnec2c) |
| MMANA (.maa) | da | da | segmentarea se recalculează pentru NEC-2 (MMANA folosește un motor MININEC) |
| AutoEZ (.weq) | da | da | variabile și formule Excel |
| EZNEC (.ez) | da | da | fire, surse, sarcini, linii, sol; solul MININEC se citește ca GN 3 cu un avertisment |
| YO / AO de la K6STI (.yag, .ant) | da | nu | |
| YA-NEC (.yanec) | da | da | modelul în formatul său original, cu nume, descriere și categorie |

Fișierul .yanec conține modelul **fără conversie** (cartelele NEC așa cum sunt, textul din .maa, .weq, .yag, .ant; .ez în base64): YA-NEC îl citește cu același importator ca fișierul original, deci rezultatele sunt identice cu ale originalului.

Toate detaliile în capitolul 10 al manualului.
