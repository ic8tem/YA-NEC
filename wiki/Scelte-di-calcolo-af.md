# Berekeningskeuses

Die handleiding (hoofstuk 21) verduidelik elke keuse met sy bronne. Kortliks:

- **Enjin**: C#-vertaling van nec2c, die NEC-2 van Lawrence Livermore National Laboratory. Die selftoets vergelyk dit met nec2c; die aanvaardingstoetse vergelyk honderde modelle met EZNEC. Die matriksfaktorisering gebeur in blokke (soos LAPACK) en gebruik al die verwerkers behalwe een, wat vir die venster bly.
- **Grond**: vrye ruimte, perfek, refleksiekoëffisiënt, Sommerfeld (met die regstelling van 'n fout in nec2c se tabelle, nagegaan met necpp), radiaalnet; tipiese en eie grondsoorte. Outomatiese waarskuwings wanneer drade vir NEC-2 te naby die grond is.
- **Teleskoopbuise**: NEC-2 laat elemente met getrapte deursnee te laag resoneer. YA-NEC gebruik die korreksie uit die boek van D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), dieselfde as EZNEC: onder resonansie altyd, daarbo tot 45 %, met 'n klein lengtekalibrasie op EZNEC-waardes.
- **Segmentering**: kontrole van die segmente (tussen lambda/1000 en lambda/10, verhouding lengte/radius) met outomatiese regstelling; onewe aantal segmente op drade wat in die middel gevoer word.
- **G/T**: integrasie van die 3D-diagram soos XLGTa, hoogteskandering soos TANT, lug- en grondtemperature van VE7BQH (oud en nuut) en DG7YBN.
- **Datakontrole**: 42 kontroles wat oor verkeerde data waarsku sonder om die model te verander.
