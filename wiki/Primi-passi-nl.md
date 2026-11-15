# Eerste stappen

1. **Voorbeelden** (opdrachtbalk) opent een van de kant-en-klare modellen: dipolen, groundplane, VHF-Yagi's (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), HF-Yagi's (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, loop-Yagi voor 1296 MHz en meer. **Model openen** opent een NEC-, MMANA-, AutoEZ-, EZNEC- of K6STI-bestand; je kunt het bestand ook op het venster slepen.
2. Kies de **band** (van 2200 m tot 3 cm, grenzen van IARU Regio 1) of een vrije frequentie, en daarna **Berekenen**.
3. De **resultaatvakken** tonen impedantie, SWR, versterking, F/B, F/R, openingshoeken bij -3 dB, rendement en AGT (convergentietest: dicht bij 0 dB is het model betrouwbaar).
4. **Sweep** berekent de band rond de frequentie: SWR, R/X, versterking, F/B en Smithkaart.

## De tabbladen

- **3D-structuur**: de antenne (slepen = draaien, muiswiel = zoomen, rechterknop = verschuiven, dubbelklik = beginbeeld), met de grond getekend volgens soort en hoogte. Daaronder de **draadtabel** in rekenbladstijl: de cellen aanvaarden getallen, variabelen en formules. Rechtsonder de **modelobjecten**: bronnen, belastingen, lijnen, transformatoren.
- **Diagrammen**: horizontaal, verticaal en 3D, met de hoek bij -3 dB.
- **Frequentie, Smith, Stromen**: resultaten van de sweep en stromen in de draden.
- **NEC-model**: de NEC-kaarten, met de hand te bewerken.
- **Variabelen** en **Optimalisator**: geef de maten een naam en laat het programma ze aanpassen tot een doel bereikt is (SWR, versterking, F/B, impedantie...). De handleiding heeft stap-voor-stap voorbeelden.
- **Rekenhulpen**: zie [Rekenhulpen](Calcolatori-nl).
- **Ontvangstantennes** en **Ruis/GT/Stack**: RDF van ontvangstantennes, antennetemperatuur en G/T (methode van TANT en XLGTa, referentietemperaturen van VE7BQH en DG7YBN), stapelen.
- **Waarschuwingen**: alles wat het programma twijfelachtig vond in het model (te korte of te lange segmenten, draden dicht bij de grond, volle knooppunten). De knop **Controleren...** in de bovenste balk controleert 42 typische invoerfouten zonder te rekenen.
- **Instellingen**, **Handleiding**, **Dankwoord**. Rechtsboven: **Taal**, **Kleuren** (18 schema's) en **Over**.

## Opslaan

**Opslaan** en **Opslaan als** schrijven het model als NEC, MMANA, AutoEZ, EZNEC of in het eigen **.yanec**-formaat, dat ook de beschrijving en het oorspronkelijke bestand bewaart.
