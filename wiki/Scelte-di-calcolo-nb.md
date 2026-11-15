# Beregningsvalg

Håndboken (kapittel 21) forklarer hvert valg med kilder. Kort fortalt:

- **Motor**: C#-oversettelse av nec2c, NEC-2 fra Lawrence Livermore National Laboratory. Selvtesten sammenligner den med nec2c; godkjenningstestene sammenligner hundrevis av modeller med EZNEC. Matrisefaktoriseringen skjer i blokker (som LAPACK) og bruker alle prosessorer unntatt én, som er til vinduet.
- **Jord**: fritt rom, perfekt, refleksjonskoeffisient, Sommerfeld (med retting av en feil i nec2c-tabellene, kontrollert med necpp), radialnett; typiske og egne jordtyper. Automatiske advarsler når tråder er for nær bakken for NEC-2.
- **Teleskoprør**: NEC-2 får elementer med trinnvise diametre til å resonere for lavt. YA-NEC bruker korreksjonen fra boken til D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), den samme som EZNEC: under resonans alltid, over opptil 45 %, med en liten lengdekalibrering mot EZNEC-verdier.
- **Segmentering**: kontroll av segmentene (mellom lambda/1000 og lambda/10, forholdet lengde/radius) med automatisk retting; oddetall segmenter på midtmatede tråder.
- **G/T**: integrasjon av 3D-diagrammet som XLGTa, elevasjonsskanning som TANT, himmel- og bakketemperaturer fra VE7BQH (gamle og nye) og DG7YBN.
- **Datakontroll**: 42 kontroller som varsler om feil data uten å endre modellen.
