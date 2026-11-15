# Rechenentscheidungen

Das Handbuch (Kapitel 21) erklärt jede Entscheidung mit ihren Quellen. Kurz:

- **Rechenkern**: C#-Übersetzung von nec2c, dem NEC-2 des Lawrence Livermore National Laboratory. Der Selbsttest vergleicht ihn mit nec2c; die Abnahmetests vergleichen Hunderte von Modellen mit EZNEC. Die Matrixfaktorisierung erfolgt blockweise (wie LAPACK) und nutzt alle Prozessoren bis auf einen, der für das Fenster bleibt.
- **Böden**: Freiraum, ideal, Reflexionsfaktor, Sommerfeld (mit der Behebung eines Fehlers von nec2c in den Tabellen, geprüft mit necpp), Radialnetz; typische und eigene Böden. Automatische Warnungen, wenn Drähte für NEC-2 zu nahe am Boden liegen.
- **Teleskoprohre**: NEC-2 lässt Elemente mit gestuften Durchmessern zu tief resonieren. YA-NEC nutzt die Korrektur aus dem Buch von D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), dieselbe wie EZNEC: unterhalb der Resonanz immer, oberhalb bis 45 %, mit einer kleinen Längenkalibrierung an EZNEC-Werten.
- **Segmentierung**: Segmentprüfung (zwischen Lambda/1000 und Lambda/10, Verhältnis Länge/Radius) mit automatischer Korrektur; ungerade Segmentzahl auf mittengespeisten Drähten.
- **G/T**: Integration des 3D-Diagramms wie XLGTa, Elevationsabtastung wie TANT, Himmels- und Bodentemperaturen von VE7BQH (alt und neu) und DG7YBN.
- **Datenprüfung**: 42 Prüfungen, die vor falschen Daten warnen, ohne das Modell zu ändern.
