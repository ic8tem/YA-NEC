# Formáty souborů

| Formát | Otevře | Uloží | Poznámky |
|---|---|---|---|
| NEC-2 (.nec) | ano | ano | karty GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (proměnné a vzorce jako v 4nec2 a xnec2c) |
| MMANA (.maa) | ano | ano | segmentace se přepočítá pro NEC-2 (MMANA používá jádro MININEC) |
| AutoEZ (.weq) | ano | ano | proměnné a vzorce Excelu |
| EZNEC (.ez) | ano | ano | vodiče, zdroje, zátěže, vedení, zem; zem MININEC se čte jako GN 3 s varováním |
| YO / AO od K6STI (.yag, .ant) | ano | ne | |
| YA-NEC (.yanec) | ano | ano | model v původním formátu, s názvem, popisem a kategorií |

Soubor .yanec obsahuje model **bez převodu** (karty NEC beze změny, text .maa, .weq, .yag, .ant; .ez v base64): YA-NEC ho čte stejným importem jako původní soubor, takže výsledky jsou stejné jako u originálu.

Všechny podrobnosti v kapitole 10 příručky.
