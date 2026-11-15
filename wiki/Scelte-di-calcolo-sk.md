# Výpočtové voľby

Príručka (kapitola 21) vysvetľuje každú voľbu aj s prameňmi. Stručne:

- **Jadro**: prevod programu nec2c do C#, teda NEC-2 z Lawrence Livermore National Laboratory. Autotest ho porovnáva s nec2c; preberacie testy porovnávajú stovky modelov s EZNEC. Rozklad matice prebieha po blokoch (ako LAPACK) a používa všetky procesory okrem jedného, ktorý zostáva oknu.
- **Zem**: voľný priestor, dokonalá, koeficient odrazu, Sommerfeld (s opravou chyby nec2c v tabuľkách, overenou s necpp), sieť radiálov; typické a vlastné druhy zeme. Automatické upozornenia, keď sú vodiče pre NEC-2 príliš blízko zeme.
- **Teleskopické rúrky**: NEC-2 dáva prvkom so stupňovitým priemerom príliš nízku rezonanciu. YA-NEC používa korekciu z knihy D. Leesona W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), rovnakú ako EZNEC: pod rezonanciou vždy, nad ňou do 45 %, s malou kalibráciou dĺžky podľa hodnôt z EZNEC.
- **Segmentácia**: kontrola segmentov (medzi lambda/1000 a lambda/10, pomer dĺžky a polomeru) s automatickou opravou; nepárny počet segmentov na vodičoch napájaných v strede.
- **G/T**: integrácia 3D diagramu ako XLGTa, skenovanie elevácie ako TANT, teploty oblohy a zeme podľa VE7BQH (staré aj nové) a DG7YBN.
- **Kontrola dát**: 42 kontrol, ktoré upozornia na chybné dáta bez zmeny modelu.
