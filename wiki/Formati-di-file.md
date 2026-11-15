# Formati di file

| Formato | Apre | Salva | Note |
|---|---|---|---|
| NEC-2 (.nec) | sì | sì | schede GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variabili e formule come 4nec2 e xnec2c) |
| MMANA (.maa) | sì | sì | la segmentazione si ricalcola per il NEC-2 (MMANA usa un motore MININEC) |
| AutoEZ (.weq) | sì | sì | variabili e formule Excel |
| EZNEC (.ez) | sì | sì | fili, sorgenti, carichi, linee, terreno; il terreno MININEC si legge come GN 3 con un avviso |
| YO / AO di K6STI (.yag, .ant) | sì | no | |
| YA-NEC (.yanec) | sì | sì | il modello nel suo formato d'origine, con nome, descrizione e categoria |

Il .yanec contiene il modello **senza conversioni** (le schede NEC così come sono, il testo del .maa, del .weq, del .yag, del .ant; il .ez in base64): YA-NEC lo legge con lo stesso importatore del file d'origine, quindi i risultati sono identici a quelli del file originale.

Tutti i dettagli nel capitolo 10 del manuale.
