**Italiano** · [English](Novita-1.1-en) · [Deutsch](Novita-1.1-de) · [Nederlands](Novita-1.1-nl) · [Français](Novita-1.1-fr) · [Español](Novita-1.1-es) · [Português](Novita-1.1-pt) · [Português (Brasil)](Novita-1.1-pt-br) · [Dansk](Novita-1.1-da) · [Norsk](Novita-1.1-nb) · [Svenska](Novita-1.1-sv) · [Íslenska](Novita-1.1-is) · [Eesti](Novita-1.1-et) · [Lietuvių](Novita-1.1-lt) · [Latviešu](Novita-1.1-lv) · [Polski](Novita-1.1-pl) · [Čeština](Novita-1.1-cs) · [Slovenčina](Novita-1.1-sk) · [Magyar](Novita-1.1-hu) · [Română](Novita-1.1-ro) · [Ελληνικά](Novita-1.1-el) · [Русский](Novita-1.1-ru) · [Українська](Novita-1.1-uk) · [Türkçe](Novita-1.1-tr) · [Afrikaans](Novita-1.1-af) · [العربية](Novita-1.1-ar) · [עברית](Novita-1.1-he) · [اردو](Novita-1.1-ur) · [हिन्दी](Novita-1.1-hi) · [বাংলা](Novita-1.1-bn) · [ไทย](Novita-1.1-th) · [Bahasa Indonesia](Novita-1.1-id) · [Tagalog](Novita-1.1-tl) · [日本語](Novita-1.1-ja) · [한국어](Novita-1.1-ko) · [中文](Novita-1.1-zh) · [中文 (繁體)](Novita-1.1-zh-tw)

# Novità della 1.1

> **Nota dell'autore.** In questa versione ho ragionato a lungo sulla temperatura di rumore. Mi sono stati segnalati alcuni studi in MATLAB sull'accoppiamento delle antenne direttive e ho voluto provarci subito: offrire a chi usa YA-NEC la possibilità di andare oltre la distanza di accoppiamento di DL6WU (la formula trigonometrica) e accompagnarlo, se lo desidera, nella scelta di una distanza di accoppiamento tutta sua. Insieme a questo, una profonda revisione dell'interfaccia. — *Costantino IC8TEM*

## La sopralinguetta Rumore/GT/Stack

La linguetta **Rumore/GT/Stack** ora ha a sinistra un fascione con tre voci:

| Voce | Cosa fa |
|---|---|
| **Rumore e G/T** | la pagina di sempre: cielo e terreno a temperatura uniforme, come TANT e le tabelle di VE7BQH |
| **Rumore dal cielo vero** | nuova: il diagramma sopra la mappa vera del cielo, dal proprio QTH e all'ora scelta |
| **Distanze di stack** | la scheda DL6WU che stava nei Calcolatori, con sotto lo studio dell'accoppiamento e della distanza migliore |

## Rumore dal cielo vero

Il cielo vero non è uniforme: a 144 MHz va da circa 150-200 K nelle zone fredde a oltre 20 000 K verso il centro della Via Lattea, e cambia con l'ora perché il cielo gira sopra il QTH. Questa pagina mette il diagramma dell'antenna o dello stack sopra la mappa del cielo, nel punto dove la si punta.

**Come si usa.**
1. Si scrive il QTH: locatore (per esempio JN70CN) oppure latitudine e longitudine. Si salva in config.ini.
2. Si sceglie data e ora in UTC (il pulsante **adesso** mette l'ora attuale).
3. Si sceglie il puntamento: azimut ed elevazione, oppure **Luna**, oppure **cielo più freddo**.
4. Si sceglie antenna singola o stack (di partenza 2 x 2 come VE7BQH) e le distanze: DL6WU, quella proposta dalla pagina Distanze di stack, oppure a mano.
5. Si leggono i due G/T: **come VE7BQH** (cielo uniforme, confrontabile con le tabelle) e **con il cielo vero**. Il pulsante **T antenna su 24 ore** dà la curva con il puntamento fisso, con minimo e massimo.

**Da dove viene il calcolo.** È **skynoise 0.2.2 di John C. Sager G8ONH** (licenza MIT), portato in C#: mappa HEALPix (nside 128) con l'interpolazione di healpy; mappe a 144 e 432 MHz di G8ONH ricavate dalla survey a 408 MHz di **Haslam** (A&AS 47, 1982) rielaborata da **Remazeilles** (MNRAS 451, 2015, server LAMBDA della NASA) con l'indice spettrale 2,56; tempo siderale di **Kaplan** (USNO Circular 179); Luna di **Meeus** (Astronomical Algorithms, cap. 47); sotto l'orizzonte 290 K.

**Prove.** Punto freddo della mappa a 144 MHz (RA 142, Dec 30) 165,7 K; centro galattico circa 22 000 K; tempo siderale a J2000 280,461 gradi come l'USNO.

> Fuori da 50-1300 MHz la sola scala con l'indice 2,56 non basta (ionosfera e rumore artificiale sotto, fondo cosmico e atmosfera sopra): la pagina lo scrive e il valore resta indicativo.

## Distanze di stack: accoppiamento e distanza migliore

**VHF/UHF.** La formula di DL6WU dà la distanza per cui i lobi si sovrappongono a circa -3 dB: un ottimo punto di partenza, non sempre il migliore. Ora lo stack si calcola davvero con il NEC per una serie di distanze (di partenza da 0,5 a 2 volte quella di DL6WU, a passi di 0,1) e per ognuna si leggono:

| Risultato | Spiegazione |
|---|---|
| guadagno dello stack | tutte le antenne in fase |
| lobo laterale più alto | nel piano dello stack |
| G/T | con le temperature di Rumore e G/T: per l'EME conta il G/T (VE7BQH) |
| abs(Sij) massimo | potenza che passa da un'antenna all'altra |

La **matrice di accoppiamento** si ottiene alimentando una porta alla volta con le altre in corto (come nec2-array di YO3IIU): Y, poi Z = Y^-1 e S = (Z - Z0)(Z + Z0)^-1. Un **criterio** sceglie la distanza proposta (guadagno massimo, lobi più bassi, G/T migliore, oppure guadagno con i lobi sotto una soglia) e **Usa la distanza proposta per lo stack** la mette nella scheda e nel cielo vero.

**HF.** Qui DL6WU non vale: le antenne sono sopra il terreno e conta l'angolo di partenza. Si danno il numero di antenne (2 o 3), la quota della più bassa, le distanze da provare (in lambda, a passi di 0,125) e gli **angoli di partenza voluti**; la distanza proposta è quella con il guadagno medio più alto in quegli angoli, con i diagrammi in elevazione in fase (BIP), superiore e inferiore da sole e in controfase (BOP).

**Prove (autotest, come nec2c).** Due Yagi MathWorks a 6 elementi sovrapposte alla distanza DL6WU: abs(S21) -27,6 dB, 12,08 dBi. Due Yagi a 3 elementi per i 20 m, la più bassa a 15 m e 0,75 lambda tra le due, suolo medio: BIP 15,63 dBi a 11 gradi.

| Fonte | Somiglianze | Differenze |
|---|---|---|
| DL6WU | la sua distanza resta il riferimento (fattore 1) | dà una sola distanza; qui si prova una serie |
| VE7BQH | il G/T come criterio EME | qui il G/T è sul diagramma NEC dello stack |
| nec2-array, YO3IIU | matrice di accoppiamento con una sorgente per porta | riscritta in C#, nessun codice copiato |
| ARRL Antenna Book, N6BV (HFTA) | stack HF su BIP e BOP e angoli di partenza | terreno piatto (Sommerfeld); il profilo del terreno arriverà in una sottolinguetta dedicata |

## Calcolatori a famiglie

Sotto le linguette c'è una fascia con le famiglie; quella scelta è arancione e il fascione a sinistra mostra solo le sue voci.

| Famiglia | Voci |
|---|---|
| HF | dipoli, dipoli e Yagi multibanda, verticali, four-square, delta loop, loop rettangolare, loop magnetica, quad, EH, Moxon, hexbeam, loop orizzontale multibanda (Skywire), discone |
| VHF | HB9CV, Big Wheel, HEN, LPDA, Yagi lunga DL6WU, Quagi, loop Yagi, boom, eggbeater, QFH |
| Antenne riceventi | Beverage, BOG, EWE, DHDL, loop ricevente, verticale di riferimento (non è più una linguetta) |
| Strumenti | ridimensiona, tetto, rete LC, resistenza di radiazione, cavi, toroidi T e FT, balun, staffe |
| Dischi e illuminatori | parabola primo fuoco, offset, trombe, biquad |
| 2,4 GHz | antenne su PCB |

## Misure sui diagrammi

Oltre all'apertura a -3 dB, sotto ogni diagramma 2D: **apertura al primo nullo** (FNBW, con i raggi viola se si accende "Mostra il primo nullo"), **lobo laterale più alto** (SLL, entro 100 gradi dal massimo, escluso il lobo posteriore) e due **cursori** C1 e C2: con "Cursori" acceso un clic mette C1, il clic dopo C2; sotto il diagramma livello, angolo e differenza. Il doppio clic o "Togli i cursori" li toglie.

## Altre novità

- **F/B lungo l'asse** corretto per i modelli in spazio libero con il fascio lungo l'asse Z (come la Yagi del MathWorks Antenna Toolbox).
- **Nuovi calcolatori**: biquad e doppia biquad (feed per parabole, con la dima 1:1), Big Wheel e tre dipoli (dima del petalo e dettaglio del mozzo), loop orizzontale multibanda (Skywire) con la perdita della linea su ogni banda.
- **Traduzioni** di tutte le voci nuove nelle 37 lingue del programma.
- **Videocorso**: lezioni nuove sugli argomenti della 1.1 e lezioni vecchie rifatte con l'interfaccia nuova (pagina [[Videocorso]]).

Tutti i dettagli, le formule e le fonti sono nel [manuale](https://github.com/ic8tem/YA-NEC/blob/main/doc/Manuale_d_uso.pdf), capitoli 7, 15 e 16.
