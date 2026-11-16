# Failivormingud

| Vorming | Avab | Salvestab | Märkused |
|---|---|---|---|
| NEC-2 (.nec) | jah | jah | kaardid GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (muutujad ja valemid nagu 4nec2-s ja xnec2c-s) |
| MMANA (.maa) | jah | jah | segmentimine arvutatakse NEC-2 jaoks ümber (MMANA kasutab MININEC-mootorit) |
| AutoEZ (.weq) | jah | jah | muutujad ja Exceli valemid |
| EZNEC (.ez) | jah | jah | juhtmed, allikad, koormused, liinid, maapind; MININEC-maapind loetakse hoiatusega kui GN 3 |
| K6STI YO / AO (.yag, .ant) | jah | ei | |
| YA-NEC (.yanec) | jah | jah | mudel algvormingus, nime, kirjelduse ja kategooriaga |

.yanec-fail sisaldab mudelit **teisendamata** (NEC-kaardid nagu on, .maa, .weq, .yag, .ant tekst; .ez base64-na): YA-NEC loeb seda sama importijaga kui algset faili, seega on tulemused samad mis originaalil.

Kõik üksikasjad kasutusjuhendi 10. peatükis.
