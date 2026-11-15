# Alegeri de calcul

Manualul (capitolul 21) explică fiecare alegere cu sursele ei. Pe scurt:

- **Motor**: traducerea în C# a lui nec2c, NEC-2 de la Lawrence Livermore National Laboratory. Autotestul îl compară cu nec2c; testele de recepție compară sute de modele cu EZNEC. Factorizarea matricei se face pe blocuri (ca în LAPACK) și folosește toate procesoarele în afară de unul, lăsat ferestrei.
- **Sol**: spațiu liber, perfect, coeficient de reflexie, Sommerfeld (cu corectarea unui defect al lui nec2c în tabele, verificată cu necpp), rețea de radiali; soluri tipice și personalizate. Avertismente automate când firele sunt prea aproape de sol pentru NEC-2.
- **Țevi telescopice**: NEC-2 face să rezoneze prea jos elementele cu diametre în trepte. YA-NEC folosește corecția din cartea lui D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), aceeași ca EZNEC: sub rezonanță întotdeauna, peste până la 45 %, cu o mică calibrare a lungimii pe valorile EZNEC.
- **Segmentare**: verificarea segmentelor (între lambda/1000 și lambda/10, raportul lungime/rază) cu corectare automată; număr impar de segmente pe firele alimentate la mijloc.
- **G/T**: integrarea diagramei 3D ca în XLGTa, scanarea elevației ca în TANT, temperaturile cerului și solului după VE7BQH (vechi și noi) și DG7YBN.
- **Verificarea datelor**: 42 de verificări care avertizează despre date greșite fără a schimba modelul.
