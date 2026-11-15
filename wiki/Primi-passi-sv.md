# Kom igång

1. **Exempel** (kommandoraden) öppnar en av de färdiga modellerna: dipoler, groundplane, VHF-Yagis (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), HF-Yagis (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, loop-Yagi för 1296 MHz med flera. **Öppna modell** öppnar en NEC-, MMANA-, AutoEZ-, EZNEC- eller K6STI-fil; du kan också dra filen till fönstret.
2. Välj **bandet** (från 2200 m till 3 cm, gränser för IARU Region 1) eller en fri frekvens, och sedan **Beräkna**.
3. **Resultatrutorna** visar impedans, SWR, förstärkning, F/B, F/R, öppningsvinklar vid -3 dB, verkningsgrad och AGT (konvergenstest: nära 0 dB är modellen pålitlig).
4. **Svep** beräknar bandet runt frekvensen: SWR, R/X, förstärkning, F/B och Smith-diagram.

## Flikarna

- **3D-struktur**: antennen (dra = rotera, hjul = zoom, höger knapp = flytta, dubbelklick = startvy), med marken ritad efter typ och höjd. Nedanför **trådtabellen** i kalkylbladsstil: cellerna tar emot tal, variabler och formler. Nere till höger **modellobjekten**: källor, laster, ledningar, transformatorer.
- **Diagram**: horisontellt, vertikalt och 3D, med -3 dB-vinkeln.
- **Frekvens, Smith, Strömmar**: svepets resultat och strömmarna i trådarna.
- **NEC-modell**: NEC-korten, som kan redigeras för hand.
- **Variabler** och **Optimerare**: ge måtten ett namn och låt programmet ändra dem för att nå ett mål (SWR, förstärkning, F/B, impedans...). Handboken har exempel steg för steg.
- **Kalkylatorer**: se [Kalkylatorer](Calcolatori-sv).
- **Mottagarantenner** och **Brus/GT/Stack**: RDF för mottagarantenner, antenntemperatur och G/T (metoden från TANT och XLGTa, referenstemperaturer från VE7BQH och DG7YBN), stackning.
- **Varningar**: allt tveksamt som programmet hittat i modellen (för korta eller för långa segment, trådar nära marken, fullsatta kopplingspunkter). Knappen **Kontrollera...** i översta raden kontrollerar 42 typiska inmatningsfel utan att räkna.
- **Inställningar**, **Handbok**, **Tack**. Uppe till höger: **Språk**, **Färger** (18 scheman) och **Om**.

## Spara

**Spara** och **Spara som** skriver modellen som NEC, MMANA, AutoEZ, EZNEC eller i det egna **.yanec**-formatet, som också behåller beskrivningen och originalfilen.
