# Esimesed sammud

1. **Näited** (käsuriba) avab ühe valmis mudeli: dipoolid, ground plane, VHF Yagid (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), HF Yagid (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, 1296 MHz silmus-Yagi ja muud. **Ava mudel** avab NEC-, MMANA-, AutoEZ-, EZNEC- või K6STI-faili; faili võib ka aknasse lohistada.
2. Valige **sagedusala** (2200 m kuni 3 cm, IARU 1. regiooni piirid) või vaba sagedus, seejärel **Arvuta**.
3. **Tulemuskastid** näitavad impedantsi, SWR-i, võimendust, F/B, F/R, kiire laiust -3 dB juures, kasutegurit ja AGT-d (koonduvustest: 0 dB lähedal on mudel usaldusväärne).
4. **Pühkimine** arvutab sagedusala sageduse ümber: SWR, R/X, võimendus, F/B ja Smithi diagramm.

## Sakid

- **3D-struktuur**: antenn (lohistamine = pööramine, ratas = suum, parem nupp = nihutamine, topeltklõps = algvaade), maapind joonistatakse tüübi ja kõrguse järgi. All on tabelarvutuse stiilis **juhtmete tabel**: lahtrid võtavad vastu arve, muutujaid ja valemeid. Paremal all on **mudeli objektid**: allikad, koormused, liinid, trafod.
- **Diagrammid**: horisontaalne, vertikaalne ja 3D, koos -3 dB nurgaga.
- **Sagedus, Smith, Voolud**: pühkimise tulemused ja voolud juhtmetes.
- **NEC-mudel**: NEC-kaardid, mida saab käsitsi muuta.
- **Muutujad** ja **Optimeerija**: andke mõõtudele nimed ja laske programmil neid muuta, kuni eesmärk on täidetud (SWR, võimendus, F/B, impedants...). Kasutusjuhendis on samm-sammulised näited.
- **Kalkulaatorid**: vt [Kalkulaatorid](Calcolatori-et).
- **Vastuvõtuantennid** ja **Müra/GT/Virn**: vastuvõtuantennide RDF, antenni temperatuur ja G/T (TANT-i ja XLGTa meetod, VE7BQH ja DG7YBN võrdlustemperatuurid), antennide virnastamine.
- **Hoiatused**: kõik kahtlane, mida programm mudelist leidis (liiga lühikesed või pikad segmendid, maapinna lähedal juhtmed, ülerahvastatud ühenduskohad). Ülemise riba nupp **Kontrolli...** kontrollib ilma arvutamata 42 tüüpilist sisestusviga.
- **Seaded**, **Kasutusjuhend**, **Tänuavaldused**. Paremal üleval: **Keel**, **Värvid** (18 skeemi) ja **Teave**.

## Salvestamine

**Salvesta** ja **Salvesta nimega** kirjutavad mudeli NEC-, MMANA-, AutoEZ-, EZNEC-vormingus või omas **.yanec**-vormingus, mis säilitab ka kirjelduse ja algse faili.
