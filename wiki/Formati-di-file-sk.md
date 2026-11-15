# Formáty súborov

| Formát | Otvorí | Uloží | Poznámky |
|---|---|---|---|
| NEC-2 (.nec) | áno | áno | karty GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (premenné a vzorce ako v 4nec2 a xnec2c) |
| MMANA (.maa) | áno | áno | segmentácia sa prepočíta pre NEC-2 (MMANA používa jadro MININEC) |
| AutoEZ (.weq) | áno | áno | premenné a vzorce Excelu |
| EZNEC (.ez) | áno | áno | vodiče, zdroje, záťaže, vedenia, zem; zem MININEC sa číta ako GN 3 s upozornením |
| YO / AO od K6STI (.yag, .ant) | áno | nie | |
| YA-NEC (.yanec) | áno | áno | model v pôvodnom formáte, s názvom, popisom a kategóriou |

Súbor .yanec obsahuje model **bez prevodu** (karty NEC bez zmeny, text .maa, .weq, .yag, .ant; .ez v base64): YA-NEC ho číta rovnakým importom ako pôvodný súbor, takže výsledky sú rovnaké ako pri origináli.

Všetky podrobnosti v kapitole 10 príručky.
