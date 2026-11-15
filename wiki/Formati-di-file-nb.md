# Filformater

| Format | Åpner | Lagrer | Merknader |
|---|---|---|---|
| NEC-2 (.nec) | ja | ja | kortene GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variabler og formler som i 4nec2 og xnec2c) |
| MMANA (.maa) | ja | ja | segmenteringen beregnes på nytt for NEC-2 (MMANA bruker en MININEC-motor) |
| AutoEZ (.weq) | ja | ja | variabler og Excel-formler |
| EZNEC (.ez) | ja | ja | tråder, kilder, laster, linjer, bakke; MININEC-bakke leses som GN 3 med en advarsel |
| YO / AO fra K6STI (.yag, .ant) | ja | nei | |
| YA-NEC (.yanec) | ja | ja | modellen i det opprinnelige formatet, med navn, beskrivelse og kategori |

.yanec-filen inneholder modellen **uten konvertering** (NEC-kortene som de er, teksten fra .maa, .weq, .yag, .ant; .ez i base64): YA-NEC leser den med den samme importfunksjonen som den opprinnelige filen, så resultatene er de samme som for originalen.

Alle detaljer i kapittel 10 i håndboken.
