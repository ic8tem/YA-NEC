# Scelte di calcolo

Il manuale (capitolo 21) spiega ogni scelta con le sue fonti. In sintesi:

- **Motore**: traduzione in C# di nec2c, il NEC-2 del Lawrence Livermore National Laboratory. L'autotest lo confronta con nec2c; i collaudi confrontano centinaia di modelli con EZNEC. La fattorizzazione della matrice è a blocchi (come LAPACK) e usa tutti i processori meno uno, lasciato alla finestra.
- **Terreni**: spazio libero, perfetto, coefficiente di riflessione, Sommerfeld (con la correzione di un difetto di nec2c nelle tabelle, verificata con necpp), schermo di radiali; terreni tipici e personalizzati. Avvisi automatici quando i fili sono troppo vicini al terreno per il NEC-2.
- **Tubi telescopici**: il NEC-2 fa risuonare troppo in basso gli elementi a diametri diversi. YA-NEC usa la correzione del libro di D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), la stessa di EZNEC: sotto la risonanza si applica sempre, sopra fino al 45 %, con una piccola taratura della lunghezza fatta sui valori di EZNEC.
- **Segmentazione**: verifica dei segmenti (tra lambda/1000 e lambda/10, rapporto lunghezza/raggio) con correzione automatica; numero dispari di segmenti sui fili alimentati al centro.
- **G/T**: integrazione del diagramma 3D come XLGTa, scansione dell'elevazione come TANT, temperature del cielo e del terreno di VE7BQH (vecchie e nuove) e DG7YBN.
- **Controllo dei dati**: 42 controlli che avvisano dei dati sbagliati senza cambiare il modello.
