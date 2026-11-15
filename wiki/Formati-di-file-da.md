# Filformater

| Format | Åbner | Gemmer | Bemærkninger |
|---|---|---|---|
| NEC-2 (.nec) | ja | ja | kort GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variabler og formler som i 4nec2 og xnec2c) |
| MMANA (.maa) | ja | ja | segmenteringen beregnes igen til NEC-2 (MMANA bruger en MININEC-motor) |
| AutoEZ (.weq) | ja | ja | variabler og Excel-formler |
| EZNEC (.ez) | ja | ja | tråde, kilder, belastninger, linjer, jord; MININEC-jord læses som GN 3 med en advarsel |
| YO / AO fra K6STI (.yag, .ant) | ja | nej | |
| YA-NEC (.yanec) | ja | ja | modellen i sit oprindelige format, med navn, beskrivelse og kategori |

.yanec-filen indeholder modellen **uden konvertering** (NEC-kort som de er, teksten fra .maa, .weq, .yag, .ant; .ez i base64): YA-NEC læser den med samme importfunktion som den oprindelige fil, så resultaterne er de samme som for originalen.

Alle detaljer i kapitel 10 i manualen.
