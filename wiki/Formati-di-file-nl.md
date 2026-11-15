# Bestandsformaten

| Formaat | Openen | Opslaan | Opmerkingen |
|---|---|---|---|
| NEC-2 (.nec) | ja | ja | kaarten GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variabelen en formules zoals in 4nec2 en xnec2c) |
| MMANA (.maa) | ja | ja | de segmentatie wordt opnieuw berekend voor NEC-2 (MMANA gebruikt een MININEC-kern) |
| AutoEZ (.weq) | ja | ja | variabelen en Excel-formules |
| EZNEC (.ez) | ja | ja | draden, bronnen, belastingen, lijnen, grond; MININEC-grond wordt met een waarschuwing als GN 3 gelezen |
| YO / AO van K6STI (.yag, .ant) | ja | nee | |
| YA-NEC (.yanec) | ja | ja | het model in zijn oorspronkelijke formaat, met naam, beschrijving en categorie |

Het .yanec-bestand bevat het model **zonder omzetting** (NEC-kaarten zoals ze zijn, de tekst van de .maa, .weq, .yag, .ant; de .ez in base64): YA-NEC leest het met dezelfde importfunctie als het oorspronkelijke bestand, dus de resultaten zijn gelijk aan die van het origineel.

Alle details in hoofdstuk 10 van de handleiding.
