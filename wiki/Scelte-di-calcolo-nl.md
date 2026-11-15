# Rekenkeuzes

De handleiding (hoofdstuk 21) legt elke keuze uit met haar bronnen. In het kort:

- **Rekenkern**: C#-vertaling van nec2c, de NEC-2 van het Lawrence Livermore National Laboratory. De zelftest vergelijkt hem met nec2c; de acceptatietests vergelijken honderden modellen met EZNEC. De matrixfactorisatie gebeurt in blokken (zoals LAPACK) en gebruikt alle processoren op één na, die voor het venster blijft.
- **Grond**: vrije ruimte, ideaal, reflectiecoëfficiënt, Sommerfeld (met de correctie van een fout van nec2c in de tabellen, gecontroleerd met necpp), radialenscherm; typische en eigen grondsoorten. Automatische waarschuwingen als draden te dicht bij de grond liggen voor NEC-2.
- **Telescoopbuizen**: NEC-2 laat elementen met getrapte diameters te laag resoneren. YA-NEC gebruikt de correctie uit het boek van D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), dezelfde als EZNEC: onder de resonantie altijd, erboven tot 45 %, met een kleine lengte-ijking op waarden van EZNEC.
- **Segmentatie**: controle van de segmenten (tussen lambda/1000 en lambda/10, verhouding lengte/straal) met automatische correctie; oneven aantal segmenten op in het midden gevoede draden.
- **G/T**: integratie van het 3D-diagram zoals XLGTa, elevatiescan zoals TANT, hemel- en grondtemperaturen van VE7BQH (oud en nieuw) en DG7YBN.
- **Gegevenscontrole**: 42 controles die waarschuwen voor foute gegevens zonder het model te veranderen.
