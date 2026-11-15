# Beregningsvalg

Manualen (kapitel 21) forklarer hvert valg med kilder. Kort sagt:

- **Motor**: C#-oversættelse af nec2c, NEC-2 fra Lawrence Livermore National Laboratory. Selvtesten sammenligner den med nec2c; godkendelsestestene sammenligner hundredvis af modeller med EZNEC. Matrixfaktoriseringen sker i blokke (som LAPACK) og bruger alle processorer undtagen én, som er til vinduet.
- **Jord**: frit rum, perfekt, refleksionskoefficient, Sommerfeld (med rettelse af en fejl i nec2c's tabeller, kontrolleret med necpp), radialnet; typiske og egne jordtyper. Automatiske advarsler, når tråde er for tæt på jorden for NEC-2.
- **Teleskoprør**: NEC-2 får elementer med trinvise diametre til at resonere for lavt. YA-NEC bruger korrektionen fra bogen af D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), den samme som EZNEC: under resonans altid, over op til 45 %, med en lille længdekalibrering ud fra EZNEC-værdier.
- **Segmentering**: kontrol af segmenterne (mellem lambda/1000 og lambda/10, forholdet længde/radius) med automatisk rettelse; ulige antal segmenter på midtfødede tråde.
- **G/T**: integration af 3D-diagrammet som XLGTa, elevationsskanning som TANT, himmel- og jordtemperaturer fra VE7BQH (gamle og nye) og DG7YBN.
- **Datakontrol**: 42 kontroller, der advarer om forkerte data uden at ændre modellen.
