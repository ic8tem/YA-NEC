# Calcolatori

La linguetta **Calcolatori** raccoglie schede che progettano un'antenna dalle formule e dalle fonti pubblicate, e poi la verificano con il motore NEC. Ogni scheda ha la stessa struttura: dati a sinistra (con un fumetto su ogni casella), formule e fonti sotto i dati, riquadri dei risultati, disegno, avvisi (triangolo arancione) e conferme (spunta verde). I pulsanti:

- **Crea il modello NEC e verificalo**: calcola il modello con il motore e mostra impedenza, ROS e guadagno;
- **Porta in risonanza (NEC)** o **Ottimizza (NEC)**, dove c'è;
- **Salva .nec** e **Apri come modello**, che porta l'antenna nella finestra principale.

Quando il modello è un'approssimazione (per esempio fili molto vicini al terreno) il pulsante ha un fumetto giallo che lo dice.

## Le schede

- **Dipoli**: mezz'onda, V invertita, ripiegato, trappolato (W3DZZ), OCF/Windom, G5RV, T2FD; **dipolo multibanda**.
- **Verticali**: lambda/4 con radiali a terra (ON4UN) e sopraelevati, 5/8, accorciata con bobina, J-pole, **verticale multibanda** tipo DX Commander, **four-square**.
- **Direttive**: HB9CV, Moxon, quad (anche multibanda DK7ZB), delta loop, loop rettangolare, Hentenna, **hexbeam 20-10 m**, **Yagi multibanda**, **Yagi lunga DL6WU** con quattro alimentazioni (dipolo aperto e chiuso, LFA, radiatore piegato DG7YBN), **Quagi**, **loop Yagi 1296 MHz**, **LPDA**.
- **Meccanica e stack**: distanze di stack DL6WU (lo stack si crea nel modello aperto), correzione del boom, **staffe di fissaggio** degli elementi.
- **Satelliti e microonde**: eggbeater, QFH, antenne su PCB per 2,4 GHz, **parabola primo fuoco** (con le centine e la dima stampabile), **trombe e illuminatori**, **parabola offset**.
- **Altro**: loop magnetica, discone, antenna EH, ridimensiona antenna, antenna su un solaio di calcestruzzo, rete LC, resistenza di radiazione.
- **Linee e trasformatori**: cavi RG e abaco dei cavi a 50 e 75 ohm, cambio d'impedenza con un tratto di cavo, toroidi iron powder e ferrite, balun e un-un.

Le fonti di ogni scheda sono scritte sotto i dati e nella linguetta Crediti; dove le fonti non sono d'accordo, le misure discordi sono mostrate affiancate.
