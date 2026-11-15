# Első lépések

1. A **Példák** (parancssáv) megnyitja az egyik kész modellt: dipólok, ground plane, URH Yagik (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), RH Yagik (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, 1296 MHz-es hurok-Yagi és mások. A **Modell megnyitása** NEC, MMANA, AutoEZ, EZNEC vagy K6STI fájlt nyit meg; a fájlt az ablakra is húzhatod.
2. Válaszd ki a **sávot** (2200 m-től 3 cm-ig, az IARU 1. körzet határai) vagy egy szabad frekvenciát, majd **Számítás**.
3. Az **eredménymezők** mutatják az impedanciát, az állóhullámarányt, a nyereséget, az F/B és F/R értéket, a -3 dB-es nyílásszöget, a hatásfokot és az AGT-t (konvergenciapróba: 0 dB közelében a modell megbízható).
4. A **Söprés** kiszámítja a sávot a frekvencia körül: állóhullámarány, R/X, nyereség, F/B és Smith-diagram.

## A fülek

- **3D szerkezet**: az antenna (húzás = forgatás, görgő = nagyítás, jobb gomb = mozgatás, dupla kattintás = kezdőnézet), a talaj fajtája és magassága szerint rajzolva. Alatta a táblázatkezelő stílusú **huzaltáblázat**: a cellák számokat, változókat és képleteket fogadnak. Jobbra lent a **modellobjektumok**: források, terhelések, tápvonalak, transzformátorok.
- **Diagramok**: vízszintes, függőleges és 3D, a -3 dB-es szöggel.
- **Frekvencia, Smith, Áramok**: a söprés eredményei és az áramok a huzalokban.
- **NEC-modell**: a NEC-kártyák, kézzel szerkeszthetők.
- **Változók** és **Optimalizáló**: nevezd el a méreteket, és hagyd, hogy a program addig változtassa őket, amíg el nem ér egy célt (állóhullámarány, nyereség, F/B, impedancia...). A kézikönyvben lépésenkénti példák vannak.
- **Kalkulátorok**: lásd [Kalkulátorok](Calcolatori-hu).
- **Vevőantennák** és **Zaj/GT/Stack**: vevőantennák RDF-je, antennahőmérséklet és G/T (TANT és XLGTa módszere, VE7BQH és DG7YBN referencia-hőmérsékletei), antennák csoportosítása.
- **Figyelmeztetések**: minden, amit a program kétesnek talált a modellben (túl rövid vagy túl hosszú szegmensek, talajhoz közeli huzalok, zsúfolt csatlakozások). A felső sáv **Ellenőrzés...** gombja számítás nélkül ellenőriz 42 tipikus adatbeviteli hibát.
- **Beállítások**, **Kézikönyv**, **Köszönet**. Jobbra fent: **Nyelv**, **Színek** (18 séma) és **Névjegy**.

## Mentés

A **Mentés** és a **Mentés másként** a modellt NEC, MMANA, AutoEZ, EZNEC formátumban vagy a saját **.yanec** formátumban írja ki, amely a leírást és az eredeti fájlt is megőrzi.
