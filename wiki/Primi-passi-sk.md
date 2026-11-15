# Prvé kroky

1. **Príklady** (príkazový pruh) otvorí jeden z hotových modelov: dipóly, ground plane, Yagi pre VKV (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), Yagi pre KV (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, slučková Yagi pre 1296 MHz a ďalšie. **Otvoriť model** otvorí súbor NEC, MMANA, AutoEZ, EZNEC alebo K6STI; súbor možno aj pretiahnuť do okna.
2. Zvoľte **pásmo** (od 2200 m do 3 cm, hranice IARU Región 1) alebo ľubovoľný kmitočet, potom **Vypočítať**.
3. **Polia výsledkov** ukazujú impedanciu, PSV, zisk, F/B, F/R, šírku zväzku pri -3 dB, účinnosť a AGT (test konvergencie: blízko 0 dB je model spoľahlivý).
4. **Rozmietanie** vypočíta pásmo okolo kmitočtu: PSV, R/X, zisk, F/B a Smithov diagram.

## Záložky

- **3D štruktúra**: anténa (ťahanie = otáčanie, koliesko = zoom, pravé tlačidlo = posun, dvojklik = východiskový pohľad), so zemou kreslenou podľa druhu a výšky. Pod ňou **tabuľka vodičov** v štýle tabuľkového procesora: bunky prijímajú čísla, premenné a vzorce. Vpravo dole **objekty modelu**: zdroje, záťaže, vedenia, transformátory.
- **Diagramy**: vodorovný, zvislý a 3D, s uhlom -3 dB.
- **Kmitočet, Smith, Prúdy**: výsledky rozmietania a prúdy vo vodičoch.
- **Model NEC**: karty NEC, upraviteľné ručne.
- **Premenné** a **Optimalizátor**: pomenujte rozmery a nechajte program, nech ich mení, kým nedosiahne cieľ (PSV, zisk, F/B, impedancia...). Príručka má príklady krok za krokom.
- **Kalkulátory**: pozri [Kalkulátory](Calcolatori-sk).
- **Prijímacie antény** a **Šum/GT/Stack**: RDF prijímacích antén, teplota antény a G/T (metóda TANT a XLGTa, referenčné teploty VE7BQH a DG7YBN), skladanie antén.
- **Upozornenia**: všetko pochybné, čo program v modeli našiel (príliš krátke či dlhé segmenty, vodiče blízko zeme, preplnené spoje). Tlačidlo **Skontrolovať...** v hornom pruhu skontroluje 42 typických chýb zadania bez výpočtu.
- **Nastavenia**, **Príručka**, **Poďakovanie**. Vpravo hore: **Jazyk**, **Farby** (18 schém) a **O programe**.

## Ukladanie

**Uložiť** a **Uložiť ako** zapíšu model ako NEC, MMANA, AutoEZ, EZNEC alebo vo vlastnom formáte **.yanec**, ktorý zachová aj popis a pôvodný súbor.
