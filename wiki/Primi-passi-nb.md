# Kom i gang

1. **Eksempler** (kommandolinjen) åpner en av de ferdige modellene: dipoler, groundplane, VHF-Yagier (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), HF-Yagier (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, loop-Yagi for 1296 MHz med flere. **Åpne modell** åpner en NEC-, MMANA-, AutoEZ-, EZNEC- eller K6STI-fil; du kan også dra filen inn i vinduet.
2. Velg **båndet** (fra 2200 m til 3 cm, grensene for IARU Region 1) eller en fri frekvens, og deretter **Beregn**.
3. **Resultatfeltene** viser impedans, SWR, forsterkning, F/B, F/R, åpningsvinkler ved -3 dB, virkningsgrad og AGT (konvergenstest: nær 0 dB er modellen pålitelig).
4. **Sveip** beregner båndet rundt frekvensen: SWR, R/X, forsterkning, F/B og Smith-diagram.

## Fanene

- **3D-struktur**: antennen (dra = roter, hjul = zoom, høyre knapp = flytt, dobbeltklikk = startvisning), med bakken tegnet etter type og høyde. Under ligger **trådtabellen** i regnearkstil: cellene tar imot tall, variabler og formler. Nederst til høyre **modellobjektene**: kilder, laster, linjer, transformatorer.
- **Diagrammer**: horisontalt, vertikalt og 3D, med -3 dB-vinkelen.
- **Frekvens, Smith, Strømmer**: resultatene av sveipet og strømmene i trådene.
- **NEC-modell**: NEC-kortene, som kan redigeres for hånd.
- **Variabler** og **Optimalisering**: gi målene et navn og la programmet endre dem for å nå et mål (SWR, forsterkning, F/B, impedans...). Håndboken har eksempler trinn for trinn.
- **Kalkulatorer**: se [Kalkulatorer](Calcolatori-nb).
- **Mottakerantenner** og **Støy/GT/Stabling**: RDF for mottakerantenner, antennetemperatur og G/T (metoden til TANT og XLGTa, referansetemperaturer fra VE7BQH og DG7YBN), stabling.
- **Advarsler**: alt tvilsomt programmet har funnet i modellen (for korte eller for lange segmenter, tråder nær bakken, overfylte koblingspunkter). Knappen **Kontroller...** i den øverste linjen sjekker 42 typiske inntastingsfeil uten å regne.
- **Innstillinger**, **Håndbok**, **Takk**. Øverst til høyre: **Språk**, **Farger** (18 skjemaer) og **Om**.

## Lagre

**Lagre** og **Lagre som** skriver modellen som NEC, MMANA, AutoEZ, EZNEC eller i det egne **.yanec**-formatet, som også tar vare på beskrivelsen og den opprinnelige filen.
