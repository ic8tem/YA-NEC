# Számítási döntések

A kézikönyv (21. fejezet) minden döntést forrásaival együtt magyaráz. Röviden:

- **Motor**: az nec2c, a Lawrence Livermore National Laboratory NEC-2-jének C#-fordítása. Az öntesztje az nec2c-vel veti össze; az átvételi tesztek több száz modellt hasonlítanak össze az EZNEC-kel. A mátrix felbontása blokkosan történik (mint a LAPACK-ban), és egy kivételével minden processzort használ; az egyet az ablaknak hagyja.
- **Talaj**: szabad tér, tökéletes, reflexiós tényező, Sommerfeld (az nec2c táblázataiban lévő hiba javításával, necpp-vel ellenőrizve), radiálháló; tipikus és saját talajok. Automatikus figyelmeztetés, ha a huzalok a NEC-2 számára túl közel vannak a talajhoz.
- **Teleszkópos csövek**: a NEC-2 a lépcsős átmérőjű elemek rezonanciáját túl alacsonyra számolja. A YA-NEC D. Leeson W6NL könyvének (*Physical Design of Yagi Antennas*, ARRL 1992) korrekcióját használja, ugyanazt, mint az EZNEC: a rezonancia alatt mindig, felette 45 %-ig, az EZNEC értékein végzett kis hosszkalibrációval.
- **Szegmentálás**: a szegmensek ellenőrzése (lambda/1000 és lambda/10 között, hossz/sugár arány) automatikus javítással; középen táplált huzalokon páratlan számú szegmens.
- **G/T**: a 3D-diagram integrálása az XLGTa szerint, emelkedési szög szerinti pásztázás a TANT szerint, az ég és a talaj hőmérséklete VE7BQH (régi és új) és DG7YBN szerint.
- **Adatellenőrzés**: 42 ellenőrzés, amely a modell megváltoztatása nélkül figyelmeztet a hibás adatokra.
