# Arvutusvalikud

Kasutusjuhend (21. peatükk) selgitab iga valikut koos allikatega. Lühidalt:

- **Mootor**: nec2c ehk Lawrence Livermore National Laboratory NEC-2 C#-tõlge. Enesetest võrdleb seda nec2c-ga; vastuvõtutestid võrdlevad sadu mudeleid EZNEC-iga. Maatriksi lahutamine toimub plokkidena (nagu LAPACK-is) ja kasutab kõiki protsessoreid peale ühe, mis jäetakse aknale.
- **Maapind**: vaba ruum, ideaalne, peegeldustegur, Sommerfeld (nec2c tabelite vea parandusega, kontrollitud necpp-ga), radiaalvõrk; tüüpilised ja omad pinnased. Automaatsed hoiatused, kui juhtmed on NEC-2 jaoks maapinnale liiga lähedal.
- **Teleskooptorud**: NEC-2 arvutab astmelise läbimõõduga elementide resonantsi liiga madalaks. YA-NEC kasutab D. Leeson W6NL raamatu (*Physical Design of Yagi Antennas*, ARRL 1992) parandust, sama mis EZNEC: resonantsist allpool alati, ülalpool kuni 45 %, väikese pikkuse kalibreerimisega EZNEC-i väärtuste järgi.
- **Segmentimine**: segmentide kontroll (lambda/1000 ja lambda/10 vahel, pikkuse ja raadiuse suhe) automaatse parandusega; keskelt toidetud juhtmetel paaritu arv segmente.
- **G/T**: 3D-diagrammi integreerimine nagu XLGTa-s, kõrgusnurga skaneerimine nagu TANT-is, taeva ja maa temperatuurid VE7BQH (vanad ja uued) ja DG7YBN järgi.
- **Andmete kontroll**: 42 kontrolli, mis hoiatavad vigaste andmete eest mudelit muutmata.
