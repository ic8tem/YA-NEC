# Beräkningsval

Handboken (kapitel 21) förklarar varje val med källor. I korthet:

- **Motor**: C#-översättning av nec2c, NEC-2 från Lawrence Livermore National Laboratory. Självtestet jämför den med nec2c; acceptanstesterna jämför hundratals modeller med EZNEC. Matrisfaktoriseringen görs i block (som LAPACK) och använder alla processorer utom en, som lämnas åt fönstret.
- **Mark**: fri rymd, perfekt, reflektionskoefficient, Sommerfeld (med rättning av ett fel i nec2c:s tabeller, kontrollerad med necpp), radialnät; typiska och egna marktyper. Automatiska varningar när trådar ligger för nära marken för NEC-2.
- **Teleskoprör**: NEC-2 får element med stegvisa diametrar att resonera för lågt. YA-NEC använder korrektionen från boken av D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), samma som EZNEC: under resonans alltid, över upp till 45 %, med en liten längdkalibrering mot EZNEC-värden.
- **Segmentering**: kontroll av segmenten (mellan lambda/1000 och lambda/10, förhållandet längd/radie) med automatisk rättning; udda antal segment på mittmatade trådar.
- **G/T**: integrering av 3D-diagrammet som XLGTa, elevationsskanning som TANT, himmels- och marktemperaturer från VE7BQH (gamla och nya) och DG7YBN.
- **Datakontroll**: 42 kontroller som varnar för felaktiga data utan att ändra modellen.
