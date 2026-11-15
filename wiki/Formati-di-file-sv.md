# Filformat

| Format | Öppnar | Sparar | Anmärkningar |
|---|---|---|---|
| NEC-2 (.nec) | ja | ja | korten GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variabler och formler som i 4nec2 och xnec2c) |
| MMANA (.maa) | ja | ja | segmenteringen beräknas om för NEC-2 (MMANA använder en MININEC-motor) |
| AutoEZ (.weq) | ja | ja | variabler och Excel-formler |
| EZNEC (.ez) | ja | ja | trådar, källor, laster, ledningar, mark; MININEC-mark läses som GN 3 med en varning |
| YO / AO från K6STI (.yag, .ant) | ja | nej | |
| YA-NEC (.yanec) | ja | ja | modellen i sitt ursprungliga format, med namn, beskrivning och kategori |

.yanec-filen innehåller modellen **utan konvertering** (NEC-korten som de är, texten från .maa, .weq, .yag, .ant; .ez i base64): YA-NEC läser den med samma importfunktion som originalfilen, så resultaten är desamma som för originalet.

Alla detaljer i kapitel 10 i handboken.
