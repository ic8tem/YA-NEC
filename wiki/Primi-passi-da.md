# Kom i gang

1. **Eksempler** (kommandolinjen) åbner en af de færdige modeller: dipoler, groundplane, VHF-Yagier (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), HF-Yagier (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, loop-Yagi til 1296 MHz og flere. **Åbn model** åbner en NEC-, MMANA-, AutoEZ-, EZNEC- eller K6STI-fil; man kan også trække filen ind i vinduet.
2. Vælg **båndet** (fra 2200 m til 3 cm, IARU Region 1-grænser) eller en fri frekvens, og derefter **Beregn**.
3. **Resultatfelterne** viser impedans, SWR, forstærkning, F/B, F/R, åbningsvinkler ved -3 dB, virkningsgrad og AGT (konvergenstest: tæt på 0 dB er modellen pålidelig).
4. **Sweep** beregner båndet omkring frekvensen: SWR, R/X, forstærkning, F/B og Smith-diagram.

## Fanerne

- **3D-struktur**: antennen (træk = drej, hjul = zoom, højre knap = flyt, dobbeltklik = startvisning), med jorden tegnet efter type og højde. Nedenunder **trådtabellen** i regnearksstil: cellerne accepterer tal, variabler og formler. Nederst til højre **modelobjekterne**: kilder, belastninger, linjer, transformere.
- **Diagrammer**: vandret, lodret og 3D, med -3 dB-vinklen.
- **Frekvens, Smith, Strømme**: resultaterne af sweep og strømmene i trådene.
- **NEC-model**: NEC-kortene, som kan redigeres i hånden.
- **Variabler** og **Optimering**: giv målene et navn, og lad programmet ændre dem for at nå et mål (SWR, forstærkning, F/B, impedans...). Manualen har trin-for-trin-eksempler.
- **Beregnere**: se [Beregnere](Calcolatori-da).
- **Modtageantenner** og **Støj/GT/Stak**: RDF for modtageantenner, antennetemperatur og G/T (metoden fra TANT og XLGTa, referencetemperaturer fra VE7BQH og DG7YBN), stakning.
- **Advarsler**: alt tvivlsomt, programmet har fundet i modellen (for korte eller for lange segmenter, tråde tæt på jorden, overfyldte samlingspunkter). Knappen **Kontrollér...** i den øverste linje kontrollerer 42 typiske indtastningsfejl uden at regne.
- **Indstillinger**, **Manual**, **Tak**. Øverst til højre: **Sprog**, **Farver** (18 skemaer) og **Om**.

## Gemme

**Gem** og **Gem som** skriver modellen som NEC, MMANA, AutoEZ, EZNEC eller i det egne **.yanec**-format, der også gemmer beskrivelsen og den oprindelige fil.
