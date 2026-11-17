# YA-NEC – specifiche HF approvate il 17/11/2026 (da compilare nella prossima versione)

Bozzetti approvati da IC8TEM: **1** (terreno reale e angoli, terza versione), **3** (RDF in ricezione), **5** (linea di discesa e modo comune), **7** (carichi HF), **9** (meccanica), **11** (quattro ritocchi).
I PNG approvati sono in questa cartella e vanno copiati in `doc/bozzetti_approvati/`. Vanno applicati **alla lettera**, anche nell'estetica.
Ancora senza bozzetto (da proporre): modello reale del trasformatore 49:1/64:1 con condensatore di compensazione; conversione dei diametri grandi in cilindro a griglia di fili.

Regole di sempre: compila.bat con collaudo automatico completo (tutti i modelli, pagine, funzioni, fumetti) e zip `yanec-collaudo-<versione>.zip`; fumetti gialli per le approssimazioni; tutto spiegato nel manuale d'uso (ogni casella, scelta grafica e di calcolo); crediti di tutte le fonti; nessun dato in AppData.

---

## 1 – Diagrammi › "Terreno reale e angoli" (alla HFTA, N6BV)
PNG: `YA-NEC-bozzetto-01_terreno_reale.png`, `YA-NEC-bozzetto-01b_terreno_reale_mappa.png`

**Fascione di sinistra** (stile a pillole approvato):
- **Posizione dell'antenna**
  - campo **Locatore** da **8 a 12 caratteri**, con controllo (spunta verde / errore); sotto: lat/lon in gradi decimali e quota del suolo dal DEM.
  - con meno di 8 caratteri: fumetto giallo "con 6 caratteri il punto può sbagliare di km: per il profilo servono almeno 8 caratteri".
  - pulsante **Scegli sulla mappa** → finestra (bozzetto 01b):
    - sfondo Foto aerea / OpenStreetMap / Solo griglia; griglia 6 / 8 / 10 caratteri, che si infittisce con lo zoom (fino a 12); bordo spesso = quadrato a 6 caratteri; etichette dei quadrati;
    - ricerca località; "Centra sul locatore";
    - clic = punto dell'antenna; a destra: locatore a 10 caratteri, coordinate, quota del suolo DEM (+ altezza antenna), zoom sul quadrato a 8 caratteri con il sotto-quadrato a 10 in rosso;
    - tabella dei lati dei quadrati alla latitudine (6: ~7,1×4,6 km; 8: ~706×463 m; 10: ~29×19 m; 12: ~2,9×1,9 m a 40,5° N), calcolati, non fissi;
    - pulsanti Annulla (sinistra) / Usa questo punto (destra).
- **Profilo del terreno**: Da HorizonProfiler (.hpr) · DEM TINITALY / SRTM1 · Profilo a mano / piatto.
- **Azimut**: Per direzione di traffico · Tutti (mappa) / azimut libero.
- **Direzione di traffico** (sei voci): USA costa est, USA costa ovest, Giappone, VK4, PY5, Caraibi; ogni pillola mostra **azimut e distanza ortodromici ricalcolati a ogni cambio di locatore**.
  Punti di riferimento (modificabili): New York 40,7 N 74,0 W · San Francisco 37,8 N 122,4 W · Tokyo 35,7 N 139,7 E · Brisbane 27,5 S 153,0 E · Curitiba 25,4 S 49,3 W · Porto Rico 18,2 N 66,5 W.
  Prova di collaudo da JN70CN82WE: 302°/7093 km, 327°/10250 km, 41°/9871 km, 82°/15990 km, 234°/9836 km, 278°/7938 km (tolleranza ±1°, ±10 km).

**Centro**:
- **Profilo** verso la direzione scelta: quota s.l.m. vs distanza (0–4 km, scala automatica), antenna con altezza sul suolo, mare in azzurro, raggio a quota antenna.
- **Lobo verticale e angoli di arrivo** (diagramma polare su un quarto di cerchio):
  - lobo col terreno reale (blu pieno) e col terreno piatto NEC (grigio tratteggiato);
  - **angoli di arrivo previsti** come spicchi arancioni dall'origine, uno per grado, lunghezza = % del tempo (statistiche per banda e regione);
  - apertura a -3 dB del lobo con due linee arancioni tratteggiate (regola generale dei diagrammi);
  - **scala angolare doppia 0–45°** come predefinita, casella per tornare a 0–90°;
  - legenda e riassunto: angolo più probabile, intervallo del 50 % dei casi, angolo sotto cui sta il 90 %, fin dove il lobo reale supera il piatto.

**Destra** – Risultati (stile HFTA): figura di merito sulla rotta (dB rispetto al piatto), guadagno a 5°/10° (reale e piatto), primo massimo, angoli coperti a -3 dB con % dei tempi di apertura coperta, altezza migliore per la rotta.
Caselle: Diffrazione sui crinali (UTD) · Superficie del mare (εr 81, σ 5 S/m) · Scansione dell'altezza dell'antenna · Statistiche angoli ARRL (banda e regione). Fumetto: il profilo vale per un solo azimut.

Metodo: diagramma libero dal NEC; riflessioni sul profilo DEM e diffrazione come HFTA; statistiche degli angoli di arrivo per banda/regione (ARRL Antenna Book). Crediti: N6BV (HFTA), ARRL. Collaudo: terreno piatto ⇒ curva reale = curva NEC su suolo piano (entro 0,1 dB).

---

## 3 – Diagrammi › "Ricezione (RDF)"
PNG: `YA-NEC-bozzetto-03_ricezione_rdf.png`

- Sinistra: antenne riceventi (Beverage, BOG, K9AY, Flag/Pennant, EWE, 4-square in ricezione, 8 verticali in cerchio, Modello aperto); rumore Isotropo / Dal piano (ITU-R P.372) / Sorgente locale (azimut). Nota: RDF non dipende dalle perdite.
- Centro: azimut all'elevazione scelta con -3 dB, riferimento verticale λ/4 tratteggiato; direzione voluta (az, el); casella "Mediana sugli angoli di arrivo" collegata alla linguetta Terreno reale.
- Destra: **RDF**, **DMF** (settore ±30° az, 10–40° el), guadagno; F/B e F/R medio; rumore d'antenna ai morsetti; margine sul ricevitore (avviso sotto 6 dB); grafico RDF vs frequenza; tabella di confronto con altre antenne.
- Collaudo: RDF di un'antenna isotropa = 0 dB; verticale λ/4 corta ≈ 4,8 dB (valori di letteratura W8JI/ON4UN).

---

## 5 – Oggetti inseriti › "Linea di discesa (modo comune)"
PNG: `YA-NEC-bozzetto-05_modo_comune.png`

- Sinistra: Calza come filo (modo comune) / Linea ideale (solo TL); percorso del cavo (lungo il palo poi a terra, diritto fino a terra, punti a mano); choke sulla calza (dalla linguetta Balun / R+jX / ideale); fine del cavo (a terra con picchetto R, stazione isolata, stazione a terra di rete). Raggio del filo = diametro esterno della calza dal catalogo cavi; distanze controllate.
- Centro: vista di lato con la calza colorata secondo la corrente; choke disegnati.
- Destra: corrente sulla calza vs distanza (senza choke, choke 1, choke 1+2); confronto senza/con choke: % corrente di modo comune (obiettivo < 5 %), potenza irradiata dal cavo, asimmetria del lobo, potenza scaldata nel choke, ROS in stazione.
- Fumetto giallo: il NEC-2 non tratta fili sotto il suolo; tratto a terra alzato a 5 cm (regola già approvata) e picchetto come carico R.
- Si aggancia alla linguetta Balun approvata (curve di impedenza di modo comune dei choke).

---

## 7 – Calcolatori › "Carichi HF"
PNG: `YA-NEC-bozzetto-07_carichi_hf.png`

Completa il bozzetto Carichi già approvato (Q di L e C, f0, Z per banda, tensione/corrente sul componente), aggiungendo i tipi costruttivi.
- Sinistra: Trappola LC (componenti) · **Trappola coassiale** · **Bobina di carico (autorisonanza)** · **Linear loading** · **Cappello capacitivo** · Bobina dalle misure (L, Q, f0); inserimento nel modello come carico (Z in frequenza) o come fili (linear loading, cappelli).
- Trappola coassiale: forma, cavo da catalogo, spire, passo, collegamento; pulsante "Calcola spire per f0"; risultati f0, L, C (dal cavo), Q a f0 e Q alle altre bande (effetto pelle), lunghezza di cavo (+ margine saldature), accorciamento del dipolo dovuto alla trappola.
- Grafico R e X della trappola in frequenza con la banda da bloccare; tabella per banda: Z, comportamento (induttanza equivalente / blocco), **tensione a 1 kW** (potenza impostabile), potenza persa.
- Fumetti gialli: tensione di lavoro del cavo superata (propone cavo adatto); autorisonanza della bobina (Medhurst) troppo vicina alla frequenza di lavoro (sotto 3×).
- Collaudo: trappola LC ideale con Q infinito ⇒ Z infinita a f0; confronto con la trappola come carico NEC (LD) entro 1 %.

---

## 9 – Calcolatori › "Meccanica" (alla YagiStress, K7NV)
PNG: `YA-NEC-bozzetto-09_meccanica.png`

- Sinistra: parti (Elementi, Boom, Tirante del boom, Staffe e mast, Area al vento totale); vento; lega (6063-T6, 6061-T6, 6082-T6). Tubi presi dalla tabella Rastremazioni (stesse misure del modello NEC).
- **Vento: come la proposta già approvata per il carico del vento** – velocità e pressione da **NTC 2018** (zona, categoria di esposizione, quota), coefficienti da **CNR-DT 207 R1/2018** ed **EN 1991-1-4**; TIA-222-H, ASCE 7-22, AS/NZS 1170.2, AIJ solo citate. La pillola "EN 1991-1-4 (zona, categoria)" del bozzetto diventa "NTC 2018 + CNR-DT 207 / EC1"; restano "Velocità a mano" e "Con ghiaccio".
- Centro: disegno della metà elemento con i tubi telescopici (Ø×spessore) e la freccia; grafico della sollecitazione / snervamento lungo l'elemento con zona di cedimento.
- Destra: vento di sopravvivenza e primo punto di cedimento; vento di progetto del sito con margine; area al vento e peso (lista di taglio); proposta di rinforzo.
- Caselle: Applica la freccia al modello NEC (mostra l'effetto su risonanza e guadagno), Ghiaccio radiale (mm), Spina interna nei giunti. Fumetto: predimensionamento, vale il progetto di un tecnico abilitato.
- Collaudo: trave a sbalzo a sezione costante contro la formula chiusa (momento, freccia) entro 1 %.

---

## 11 – Quattro ritocchi
PNG: `YA-NEC-bozzetto-11_ritocchi.png`

- **A – Diagrammi › Tabelle: F/B e F/R in banda.** F/R = guadagno avanti meno media della potenza sull'emisfero posteriore (azimut 90–270°, elevazioni sopra 0°); grafico su tutta la banda insieme all'F/B; F/R anche tra gli obiettivi dell'ottimizzatore.
- **B – Oggetti inseriti › Linee: linea aperta + accordatore (alla TLW).** Per ogni banda: Z al punto di alimentazione, ROS sulla linea, perdita nella linea, Z all'accordatore, perdita nell'accordatore (tipo e Q impostabili), perdita totale. Avviso quando l'accordatore vede meno di 20 Ω (suggerisce di allungare o accorciare la linea).
- **C – Barra comandi › Terreno: dal locatore (ITU-R P.832).** Pulsante "Cerca nell'atlante ITU" dal locatore; tabella con terraferma vicina e mare (con proposta secondo mezzo GD o mare sotto 5°). Fumetto: l'atlante ha maglie di decine di km.
- **D – Avvisi: test del guadagno medio (AGT).** Calcolato da sé togliendo perdite e terreno reale (spazio libero: atteso 1,000; terreno perfetto: atteso 2,000); oltre ±0,2 dB avviso e proposta di verifica della segmentazione; guadagno mostrabile corretto con l'AGT.
