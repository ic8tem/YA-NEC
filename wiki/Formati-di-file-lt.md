# Failų formatai

| Formatas | Atidaro | Įrašo | Pastabos |
|---|---|---|---|
| NEC-2 (.nec) | taip | taip | kortos GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (kintamieji ir formulės kaip 4nec2 ir xnec2c) |
| MMANA (.maa) | taip | taip | segmentavimas perskaičiuojamas NEC-2 (MMANA naudoja MININEC variklį) |
| AutoEZ (.weq) | taip | taip | kintamieji ir Excel formulės |
| EZNEC (.ez) | taip | taip | laidai, šaltiniai, apkrovos, linijos, žemė; MININEC žemė skaitoma kaip GN 3 su įspėjimu |
| K6STI YO / AO (.yag, .ant) | taip | ne | |
| YA-NEC (.yanec) | taip | taip | modelis pradiniu formatu, su pavadinimu, aprašu ir kategorija |

.yanec failas turi modelį **be konvertavimo** (NEC kortos tokios, kokios yra, .maa, .weq, .yag, .ant tekstas; .ez base64 forma): YA-NEC jį skaito tuo pačiu importuokliu kaip pradinį failą, todėl rezultatai sutampa su originalo.

Visos detalės vadovo 10 skyriuje.
