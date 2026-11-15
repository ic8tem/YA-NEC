# První kroky

1. **Příklady** (příkazový pruh) otevře jeden z hotových modelů: dipóly, ground plane, Yagi pro VKV (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), Yagi pro KV (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, smyčková Yagi pro 1296 MHz a další. **Otevřít model** otevře soubor NEC, MMANA, AutoEZ, EZNEC nebo K6STI; soubor lze také přetáhnout do okna.
2. Zvolte **pásmo** (od 2200 m do 3 cm, hranice IARU Region 1) nebo libovolný kmitočet, pak **Spočítat**.
3. **Pole výsledků** ukazují impedanci, PSV, zisk, F/B, F/R, šířku svazku při -3 dB, účinnost a AGT (test konvergence: blízko 0 dB je model spolehlivý).
4. **Rozmítání** spočítá pásmo kolem kmitočtu: PSV, R/X, zisk, F/B a Smithův diagram.

## Záložky

- **3D struktura**: anténa (tažení = otáčení, kolečko = zoom, pravé tlačítko = posun, dvojklik = výchozí pohled), se zemí kreslenou podle druhu a výšky. Pod ní **tabulka vodičů** ve stylu tabulkového procesoru: buňky přijímají čísla, proměnné a vzorce. Vpravo dole **objekty modelu**: zdroje, zátěže, vedení, transformátory.
- **Diagramy**: vodorovný, svislý a 3D, s úhlem -3 dB.
- **Kmitočet, Smith, Proudy**: výsledky rozmítání a proudy ve vodičích.
- **Model NEC**: karty NEC, upravitelné ručně.
- **Proměnné** a **Optimalizátor**: pojmenujte rozměry a nechte program, ať je mění, dokud nedosáhne cíle (PSV, zisk, F/B, impedance...). Příručka má příklady krok za krokem.
- **Kalkulátory**: viz [Kalkulátory](Calcolatori-cs).
- **Přijímací antény** a **Šum/GT/Stack**: RDF přijímacích antén, teplota antény a G/T (metoda TANT a XLGTa, referenční teploty VE7BQH a DG7YBN), skládání antén.
- **Varování**: vše pochybné, co program v modelu našel (příliš krátké či dlouhé segmenty, vodiče blízko země, přeplněné spoje). Tlačítko **Zkontrolovat...** v horním pruhu zkontroluje 42 typických chyb zadání bez výpočtu.
- **Nastavení**, **Příručka**, **Poděkování**. Vpravo nahoře: **Jazyk**, **Barvy** (18 schémat) a **O programu**.

## Ukládání

**Uložit** a **Uložit jako** zapíší model jako NEC, MMANA, AutoEZ, EZNEC nebo ve vlastním formátu **.yanec**, který uchová i popis a původní soubor.
