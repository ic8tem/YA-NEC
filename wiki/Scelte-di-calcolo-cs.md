# Výpočetní volby

Příručka (kapitola 21) vysvětluje každou volbu i s prameny. Stručně:

- **Jádro**: převod programu nec2c do C#, tedy NEC-2 z Lawrence Livermore National Laboratory. Autotest ho porovnává s nec2c; přejímací testy porovnávají stovky modelů s EZNEC. Rozklad matice probíhá po blocích (jako LAPACK) a používá všechny procesory kromě jednoho, který zůstává oknu.
- **Zem**: volný prostor, dokonalá, koeficient odrazu, Sommerfeld (s opravou chyby nec2c v tabulkách, ověřenou s necpp), síť radiálů; typické a vlastní druhy země. Automatická varování, když jsou vodiče pro NEC-2 příliš blízko země.
- **Teleskopické trubky**: NEC-2 dává prvkům se stupňovitým průměrem příliš nízkou rezonanci. YA-NEC používá korekci z knihy D. Leesona W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), stejnou jako EZNEC: pod rezonancí vždy, nad ní do 45 %, s malou kalibrací délky podle hodnot z EZNEC.
- **Segmentace**: kontrola segmentů (mezi lambda/1000 a lambda/10, poměr délky a poloměru) s automatickou opravou; lichý počet segmentů na vodičích napájených uprostřed.
- **G/T**: integrace 3D diagramu jako XLGTa, skenování elevace jako TANT, teploty oblohy a země podle VE7BQH (staré i nové) a DG7YBN.
- **Kontrola dat**: 42 kontrol, které upozorní na chybná data bez změny modelu.
