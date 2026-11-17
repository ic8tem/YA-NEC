# YA-NEC – specifiche approvate il 17/11/2026 (sessione VHF e meccanica)

Pacchetto da caricare nella chat principale di YA-NEC per la compilazione della prossima versione. I bozzetti PNG approvati (1 rev. 2, 4, 5, 6, 8, 9, 10 rev. 2, 11, 12) sono nella cartella `approvati/` dello zip. Quelli ancora da decidere sono in `da_approvare/` e non vanno compilati.

## Regole comuni a tutti i bozzetti

- I numeri dei bozzetti sono di esempio: il programma li calcola davvero.
- Fascione sinistro a pillole con la selezione blu (stile già approvato).
- Diagrammi dei lobi sempre con il triangolo o il cono a −3 dB.
- Nelle grandezze meccaniche le unità sono sempre affiancate: N e kgf, N·m e kgf·m, N/m² e kgf/m² (1 kgf = 9,80665 N), con la tendina "N e kgf / solo N / solo kgf".
- Ogni nuova casella, linguetta e sottolinguetta entra nel collaudo automatico di compila.bat e nel manuale d'uso.
- Le fonti nuove vanno nei crediti.

## Unificazioni con i bozzetti HF approvati oggi nell'altra chat

- **Terreno reale:** il bozzetto HF 1 rev. 3 (HFTA, angoli di arrivo, direzioni di traffico) e il bozzetto VHF 1 rev. 2 usano **lo stesso motore e la stessa sottolinguetta**. Cambiano solo lo zoom angolare (VHF 0–15°) e le direzioni di traffico (HF).
- **Linea di discesa e modo comune:** il bozzetto HF 5 e il bozzetto VHF 10 diventano **una sola scheda HF–UHF** (bozzetto 10 rev. 2).
- **Meccanica alla YagiStress:** il bozzetto HF 9 entra nella nuova linguetta Calcoli meccanici come sottolinguetta "Sollecitazioni elementi" (bozzetto 13, da approvare).

---

## A. Diagrammi

### 1. Terreno reale – rev. 2 (bozzetto_1_terreno_reale_rev2.png)

- Sottolinguetta **Diagrammi > Terreno reale**.
- Profilo dal .csv di HorizonProfiler, dal DEM (TINITALY / SRTM1) oppure piatto per il confronto.
- Dati del sito: locatore, quota, altezza dell'antenna, azimut, distanza massima.
- Superfici: tipo di terra e di mare.
- Caselle: rugosità del mare, divergenza (k = 4/3), diffrazione sui crinali, rosa 0–360°.
- Metodo come HFTA: il diagramma in spazio libero viene dal NEC; il terreno aggiunge il raggio diretto, i raggi riflessi su ogni tratto e la diffrazione sui crinali.
- Grafico del profilo con l'antenna, il punto di riflessione e la linea d'orizzonte con il suo angolo.
- **Lobo di elevazione polare**, con la stessa scala in dBi e lo stesso orientamento della linguetta Verticale:
  - vista intera 0–180°;
  - zoom 0–15° (×6), oppure 0–30° (×3);
  - curve: profilo reale, terreno piatto (NEC Sommerfeld), spazio libero;
  - triangolo a −3 dB sul primo lobo;
  - settore dell'orizzonte ostruito.
- Gruppo Vista: polare oppure cartesiana; casella "Sovrapponi alla linguetta Verticale".
- Tabella del guadagno in elevazione (piatto, reale, Δ), con primo lobo, primo minimo, punto di riflessione e angolo d'orizzonte.

### 9. Tolleranze costruttive (bozzetto_9_tolleranze.png)

- Sottolinguetta **Diagrammi > Tolleranze**.
- Metodo Monte Carlo (seme fisso, ripetibile) oppure sensibilità rapida, un elemento alla volta.
- Tolleranze su lunghezza, posizione sul boom e diametro; distribuzione uniforme o gaussiana; foratura del boom; errore delle staffe.
- Risultati:
  - guadagno, F/B e ROS nominali e nel 5 % delle prove peggiori;
  - istogramma del F/B;
  - sensibilità per elemento in dB/mm, con i critici in rosso.
- **Lista di taglio** con la tolleranza ammessa per ogni elemento, calcolata dagli obiettivi (calo massimo del F/B, ROS massimo). Si può stampare ed esportare in CSV.

### 11. Misure del VNA (bozzetto_11_misure_vna.png)

- In **Diagrammi > ROS / Z**.
- Legge un file Touchstone .s1p (RI, MA o DB) o un CSV (NanoVNA-Saver e simili).
- De-embedding del cavo tra VNA e antenna (tipo, lunghezza, VF, perdita da tabella).
- ROS calcolato, misurato con il cavo tolto e misurato grezzo; carta di Smith con i marcatori; si possono caricare più misure.
- Tabella di confronto: frequenza del minimo, ROS minimo, banda, Z, scarto in %.
- Interpretazione dello scarto con una **proposta** di correzione: il programma non cambia nulla da solo.

## B. Oggetti inseriti

### 4. Telaio a H e palo nello stack (bozzetto_4_telaio_H_stack.png)

- Nella scheda **Oggetti inseriti > Stack**, casella **"Includi telaio nel calcolo"**.
- Tipo di telaio: H (2 montanti), croce, solo palo, traliccio. Materiale: acciaio zincato, alluminio, vetroresina (la vetroresina non entra nel NEC).
- Caselle: palo sotto il telaio, cavi di fase lungo i montanti (la calza diventa un filo), rotore e controventi.
- Tabella dei pezzi: diametro, lunghezza, collegamento (al boom, a massa, isolato). Montanti, traversa, palo e calze diventano fili NEC.
- Vista frontale con le parti del telaio in rosso e i cavi tratteggiati.
- Tabella di confronto senza e con telaio: guadagno, F/B, F/R medio, lobo laterale peggiore, Z all'ingresso del divisore, Tant e G/T (da TANT).
- Diagramma orizzontale sovrapposto senza e con telaio.
- Fumetto giallo sulla polarizzazione: in orizzontale pesano la traversa e i cavi, in verticale i montanti.

### 10. Linea di discesa e modo comune – rev. 2, scheda unica HF–UHF (bozzetto_10_modo_comune_HF_UHF_rev2.png)

- Scheda **Oggetti inseriti > Cavo e balun**.
- Banda da 1,8 a 1296 MHz: l'elenco dei balun cambia con la banda.
  - HF: choke a spire di coax, toroide 1:1 mix 31/43.
  - VHF/UHF: λ/2 coassiale 4:1, manicotto λ/4.
  - Sempre disponibili: ferriti a perline, Pawsey stub, nessun balun.
- Linea coassiale o linea aperta (HF), con il percorso lungo il boom, sul palo e fino alla stazione. Calza a massa sul palo o traliccio; messa a terra all'ingresso della stazione.
- Calza e palo diventano fili NEC. Nella vista la calza è colorata secondo la corrente; un grafico mostra la corrente lungo la calza.
- Confronto dei balun: corrente sulla calza, F/B, F/R, guadagno, Z/ROS, perdita, **potenza dissipata a 1 kW**, banda utile.
- Comprende tutto quello che chiedeva il bozzetto HF 5.

## C. Calcolatori

### 5. Divisori di potenza e cavi di fase (bozzetto_5_divisori.png)

- Tipi: λ/4 in tubo quadro, λ/4 in tubo tondo, λ/4 in cavo, **Wilkinson anche in 144 MHz**, solo cavi di fase.
- Wilkinson: resistenze con la potenza dissipata, isolamento tra le uscite.
- Calcoli:
  - Z0 = √(Zin · Zcarico);
  - tubo quadro con conduttore tondo: Z0 = 138 · log(1,0787 · D/d);
  - accorciamento dei connettori;
  - banda;
  - perdite.
- Disegno quotato stampabile 1:1.
- ROS calcolato con le impedenze attive dello Stack.
- Cavi di fase: multipli di λ/2, tolleranza.
- Tilt elettrico.
- Numero dispari di antenne: avviso e proposta di soluzione.

### 6. Yagi incrociate e polarizzazione (bozzetto_6_yagi_incrociate.png)

- Nuova sottolinguetta **Calcolatori > Yagi incrociate**.
- Yagi di base: il modello aperto oppure un calcolatore (es. DL6WU). Disposizione X (45°) oppure + (H/V). Piani sfalsati di λ/4 sul boom oppure sullo stesso piano. Boom metallico con elementi isolati.
- Commutazione: orizzontale, verticale, RHCP, LHCP, X-pol (due ricevitori, EME).
- Rete di fase: cavo di ritardo λ/4 (scelta del cavo); casella per includere relè e cavi reali.
- Risultati: guadagno in dBic, rapporto assiale in asse e a −3 dB, rifiuto della polarizzazione opposta, Z, isolamento tra i due piani.
- Grafici:
  - componenti co-polare e cross-polare (polare);
  - rapporto assiale nel lobo, con le linee a −3 dB;
  - rapporto assiale e ROS in frequenza.
- La rete di fase serve anche per un futuro turnstile (dipoli incrociati a 90°).

## D. Nuova linguetta principale **Calcoli meccanici** (tra Calcolatori e Impostazioni)

Sottolinguette: **Vento · Bilanciamento · Boom e tirante · Palo e plinto · Sollecitazioni elementi**. Le **Staffe** restano nei Calcolatori, perché modificano il modello NEC.

### Vento (bozzetto_8_vento.png)

- Si sposta dai Calcolatori a Calcoli meccanici.
- Norma: NTC 2018 + Circolare n. 7/2019 per velocità e pressione; CNR-DT 207 R1/2018 ed EN 1991-1-4 per i coefficienti (§7.6, §7.9.2, §7.11, §7.13 ψλ).
- TIA-222-H, ASCE 7-22, AS/NZS 1170.2 e AIJ restano visibili ma disattivate finché l'utente non fornisce i testi.
- Zona NTC dal locatore: Capri = zona 9, 31 m/s.
- Forze per parte; forza e momento torcente in funzione della direzione del vento.
- Risultati: forza massima, momento flettente alla staffa, momento torcente sul rotore, tutti in N e kgf.
- Relazione PDF.
- Il momento alla base passa a "Palo e plinto".
- Avviso: è un predimensionamento.

### Bilanciamento (bozzetto_12_bilanciamento_calcoli_meccanici.png)

- Masse dal modello: elementi, boom, staffe, dipolo, cavo.
- Aree al vento dalla sottolinguetta Vento.
- Vista laterale con baricentro, centro di spinta e staffa.
- Grafico del momento del peso e del momento torcente del vento in funzione della posizione della staffa.
- Risultati: punto di bilanciamento del peso, del vento e di compromesso; torcente sul rotore attuale e con la staffa consigliata.
- Contrappeso facoltativo.
- Pulsante "Sposta staffa nel modello".

### Boom e tirante – bozzetto da disegnare

### Palo e plinto – bozzetto da disegnare (collegamento al programma Plinto)

---

## Ancora da approvare (cartella da_approvare/, NON compilare)

- 7 – Rumore del Sole e della Luna atteso (manca la fonte per la conversione del flusso)
- 13 – Sollecitazioni elementi, stile YagiStress (fonti: K5IU sul flusso trasversale, calcolatore "tapers" di hamwaves.com, note sul vento di K7NV)
