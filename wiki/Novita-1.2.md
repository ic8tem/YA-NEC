**Italiano** · [English](Novita-1.2-en) · [Deutsch](Novita-1.2-de) · [Nederlands](Novita-1.2-nl) · [Français](Novita-1.2-fr) · [Español](Novita-1.2-es) · [Português](Novita-1.2-pt) · [Português (Brasil)](Novita-1.2-pt-br) · [Dansk](Novita-1.2-da) · [Norsk](Novita-1.2-nb) · [Svenska](Novita-1.2-sv) · [Íslenska](Novita-1.2-is) · [Eesti](Novita-1.2-et) · [Lietuvių](Novita-1.2-lt) · [Latviešu](Novita-1.2-lv) · [Polski](Novita-1.2-pl) · [Čeština](Novita-1.2-cs) · [Slovenčina](Novita-1.2-sk) · [Magyar](Novita-1.2-hu) · [Română](Novita-1.2-ro) · [Ελληνικά](Novita-1.2-el) · [Русский](Novita-1.2-ru) · [Українська](Novita-1.2-uk) · [Türkçe](Novita-1.2-tr) · [Afrikaans](Novita-1.2-af) · [العربية](Novita-1.2-ar) · [עברית](Novita-1.2-he) · [اردو](Novita-1.2-ur) · [हिन्दी](Novita-1.2-hi) · [বাংলা](Novita-1.2-bn) · [ไทย](Novita-1.2-th) · [Bahasa Indonesia](Novita-1.2-id) · [Tagalog](Novita-1.2-tl) · [日本語](Novita-1.2-ja) · [한국어](Novita-1.2-ko) · [中文](Novita-1.2-zh) · [中文 (繁體)](Novita-1.2-zh-tw)

# Novità della 1.2

Questa pagina riassume le novità della 1.2. Ogni pagina è spiegata per intero, casella per casella, nel **capitolo 23 del manuale** ([doc/Manuale_d_uso.pdf](https://github.com/ic8tem/YA-NEC/blob/main/doc/Manuale_d_uso.pdf) e linguetta Manuale del programma), con le scelte di calcolo, le approssimazioni (i fumetti gialli) e le fonti.

## La fascia dei Diagrammi

Sotto la linguetta **Diagrammi** c'è una fascia con sei voci: **Diagrammi** (azimut ed elevazione, come prima), **Tabelle**, **Misure VNA**, **Terreno reale**, **Ricezione (RDF)**, **Tolleranze**. La voce aperta è in arancio; ogni voce si aggiorna da sola dopo un calcolo o uno sweep.

**Tabelle.** Grafico del F/B e del **F/R medio** su tutta la banda dello sweep e tabella dei valori. F/R medio = guadagno avanti meno la media della potenza sull'emisfero posteriore (azimut da 90 a 270 gradi intorno alla direzione avanti, elevazioni da 2,5 a 87,5 gradi a passi di 5, pesate con il coseno dell'elevazione, cioè con l'angolo solido). È lo stesso denominatore del DMF di ON4UN (Low-Band DXing, capitolo sulle antenne riceventi). Il F/R medio è anche un obiettivo dell'Ottimizzatore ("F/R medio (emisfero posteriore)", per esempio "≥ 18 dB").

| Grandezza | Come si calcola | Perché |
|---|---|---|
| F/B | avanti meno il punto esattamente opposto | è il numero dei cataloghi, ma un nullo stretto proprio dietro lo fa sembrare ottimo |
| F/R (peggiore) | avanti meno il lobo posteriore più alto | il caso peggiore |
| F/R medio | avanti meno la media della potenza dietro | dice quanto rumore e quante stazioni arrivano da dietro; non dipende da un nullo stretto |

## Terreno reale (alla HFTA)

Il diagramma di elevazione su un profilo vero del terreno, come HFTA di N6BV (ARRL Antenna Book). Il diagramma dell'antenna viene dal NEC (le correnti calcolate); il terreno aggiunge i raggi riflessi su ogni tratto del profilo e la diffrazione sui crinali. La stessa pagina vale in HF (direzioni di traffico, scala angolare doppia 0-45 gradi) e in VHF (vista intera 0-180 gradi e zoom 0-15 gradi).

**Posizione dell'antenna.** Il **locatore** da 8 a 12 caratteri (spunta verde se valido); sotto, latitudine e longitudine del centro del quadrato e la quota del suolo dal DEM. Con meno di 8 caratteri compare il fumetto giallo: con 6 caratteri il punto può sbagliare di chilometri. **Scegli sulla mappa** apre la finestra della mappa (sotto).

**Profilo del terreno.**

| Scelta | Cosa fa |
|---|---|
| Da HorizonProfiler (.csv / .hpr) | legge un file di coppie distanza / quota (distanze in m o in km; righe di intestazione ignorate) |
| DEM TINITALY / SRTM1 | campiona il profilo lungo l'azimut fino alla **distanza massima** (400 punti): prima TINITALY 1.1 (Italia, celle di 10 m), poi SRTM1 (tutto il mondo, celle di circa 30 m). Punti senza dato o a quota sotto 0,5 m = mare |
| Terreno piatto (confronto) | profilo piatto alla quota del sito: serve a confrontare |

Il DEM si scarica al primo uso e resta nella cartella **data/dem** accanto al programma (niente in AppData): TINITALY a ritagli di 0,05 gradi dal servizio WCS dell'INGV, SRTM1 a mattonelle di 1 grado. Senza rete e senza file già scaricati il profilo DEM non si può fare: il programma lo dice.

**Sito.** Quota sito (dal DEM o dal file), **altezza antenna** (dal modello: il segmento della sorgente; 10 m se il modello è in spazio libero) e **distanza max** (5 km in HF, 20 km in VHF). Se l'altezza scritta è diversa da quella del modello, il diagramma del NEC si sposta in quota con le stesse correnti (fumetto giallo: impedenza e accoppiamento col suolo restano quelli del modello).

**Azimut.** **Per direzione di traffico**: sei pillole (USA costa est, USA costa ovest, Giappone, VK4, PY5, Caraibi) con azimut e distanza ortodromici ricalcolati a ogni cambio di locatore (sfera di 6371 km; punti di riferimento New York, San Francisco, Tokyo, Brisbane, Curitiba, Porto Rico). **Azimut libero**: si scrive l'azimut. L'antenna è rivolta verso l'azimut scelto (antenna rotativa).

**Superfici.** Terra (dalla tabella dei terreni: quella del modello se c'è), Mare (acqua salata εr 81, σ 5 S/m; acqua dolce εr 80, σ 0,003 S/m), caselle **Superficie del mare**, **Rugosità mare** (fattore di Ament per onde di 0,25 m), **Divergenza** (terra curva con raggio efficace 4/3 e fattore di divergenza), **Diffrazione sui crinali**, **Tutti gli azimut** (rosa della figura di merito ogni 10 gradi, solo con il DEM).

**Vista.** Polare (come la linguetta Verticale) o cartesiana dBi / gradi; **zoom angolare** 0-45 (x2), 0-30 (x3), 0-15 (x6), 0-90, 0-180. In HF un quarto di cerchio con la scala angolare allargata; in VHF il diagramma intero a sinistra e lo zoom a destra. Curve: profilo reale (blu), terreno piatto (arancio tratteggiato), spazio libero (grigio); linee a -3 dB del primo lobo; settore dell'orizzonte ostruito. **Sovrapponi alla linguetta Verticale** aggiunge il lobo reale al diagramma di elevazione dei Diagrammi. **Esporta CSV**: elevazione, reale, piatto, spazio libero.

**Risultati (stile HFTA).** Figura di merito sulla rotta (dB rispetto al piatto), guadagno a 5 / 10 gradi (in VHF a 1 / 3 gradi) reale e piatto, primo massimo, angoli coperti a -3 dB, altezza migliore (con la casella **Scansione dell'altezza**: da 6 a 30 m in HF, da 2 a 30 m in VHF). Tabella del guadagno a 0,5 - 1 - 2 - 3 - 5 - 10 gradi (piatto, reale, differenza), primo lobo, primo minimo, punto di riflessione (sul mare o sul terreno) e angolo dell'orizzonte.

**Metodo di calcolo.**

| Parte | Come | Fonte |
|---|---|---|
| Raggio diretto | campo del NEC senza l'immagine del suolo (le correnti sono quelle calcolate sopra il suolo del modello) | NEC-2 (ffld) |
| Raggi riflessi | per ogni tratto del profilo: immagine dell'antenna nella retta del tratto, coefficiente di Fresnel orizzontale e verticale (le stesse formule del NEC), fase dall'immagine dell'origine; il tratto conta solo se vede l'antenna e il cielo | HFTA (N6BV); NEC-2 per i coefficienti |
| Tratti corti | ogni tratto riflette solo la parte di prima zona di Fresnel che copre: fattore = integrale di Fresnel tra i suoi estremi diviso per l'integrale su tutto il piano; il primo tratto continua sotto l'antenna e l'ultimo oltre il profilo. Su un profilo piatto la somma vale 1 | ottica fisica (zone di Fresnel) |
| Diffrazione | lama di coltello sul crinale più alto (integrale di Fresnel), solo se è un crinale vero | ITU-R P.526; HFTA |
| Mare rugoso, terra curva | fattore di Ament; raggio 4/3 e divergenza | ITU-R P.453 / P.530 (k = 4/3); Ament 1953 |

Collaudo: su un profilo piatto la curva reale deve coincidere con il NEC sul suolo piano entro 0,1 dB (dipolo di 20 m a 10 m, 1-80 gradi: scarto 0,003 dB); integrali di Fresnel contro Abramowitz-Stegun; direzioni di traffico da JN70CN82WE entro 1 grado e 10 km.

**Somiglianze e differenze con HFTA.** Come HFTA: raggi riflessi tratto per tratto, diffrazione sui crinali, direzioni di traffico, figura di merito. Diversamente da HFTA: il diagramma viene dal NEC del modello aperto (HFTA usa diagrammi suoi di dipoli e Yagi); il profilo può venire direttamente dal DEM; le **statistiche degli angoli di arrivo** dell'ARRL (per banda e regione) non ci sono ancora: la casella è grigia finché non arrivano le tabelle, e la figura di merito è la media uniforme su 1-15 gradi in HF (0,5-5 gradi in VHF) invece della media pesata con le percentuali di apertura (fumetto giallo).

**Mappa (Scegli sulla mappa).** Sfondo **Foto aerea** (Esri World Imagery), **OpenStreetMap** o **Solo griglia**; griglia dei locatori a 6, 8 o 10 caratteri, che si infittisce con lo zoom fino a 12 (bordo spesso = quadrato a 6 caratteri). Clic = punto dell'antenna; trascina = sposta; rotella = zoom. **Cerca** accetta un locatore o il nome di una località (Nominatim di OpenStreetMap). A destra: locatore a 10 caratteri con il lato del quadrato, coordinate, quota del suolo dal DEM (+ altezza dell'antenna) e lo zoom sul quadrato a 8 caratteri con il sotto-quadrato a 10 in rosso. Sotto: i lati dei quadrati a 6, 8, 10 e 12 caratteri alla latitudine del punto (calcolati, non fissi). Le mattonelle restano in **data/mappe** per 60 giorni.

## Ricezione (RDF)

Cifre di merito delle antenne riceventi: **RDF** (guadagno nella direzione voluta meno guadagno medio su tutto il semispazio; non dipende dalle perdite), **DMF** (media sul settore avanti meno media sull'emisfero posteriore), guadagno, F/B e F/R medio, rumore d'antenna ai morsetti (ITU-R P.372) e margine sul ricevitore (avviso sotto 6 dB). Antenne: Beverage, BOG, K9AY, Flag / Pennant, EWE, 4-square in ricezione, 8 verticali in cerchio, modello aperto. Rumore: isotropo, dal piano, sorgente locale. Riferimento tratteggiato: verticale lambda/4. Grafico dell'RDF in frequenza e tabella di confronto con i valori di letteratura (W8JI, ON4UN, IV3PRK). Fumetto giallo sul DMF: ON4UN lo definisce con il solo guadagno a 20 gradi; qui si usa la media sul settore avanti. Collaudo: dipolo corto 1,76 dB (teorico), verticale lambda/4 tra 4,6 e 5,4 dB.

## Misure VNA

Confronto tra il calcolo e le misure del VNA. **Misura**: file Touchstone .s1p (formati RI, MA, DB; frequenze in Hz, kHz, MHz o GHz; parametri S, Z o Y) oppure CSV (NanoVNA-Saver e simili: frequenza, parte reale e immaginaria di S11). **Cavo tra VNA e antenna**: con la casella **Togli il cavo dalla misura** la Z misurata si riporta all'antenna togliendo la linea (cavo dall'abaco, lunghezza, VF; perdita dalla tabella dei cavi): è l'inversa esatta della formula dell'impedenza all'ingresso di una linea con perdite. **Mostra**: ROS, carta di Smith (più curve), R e X separate, perdita di ritorno. **Aggiungi altra** sovrappone altre misure.

Il calcolo del NEC si rifà da solo sul campo di frequenze della misura (61 punti). La tabella confronta frequenza del ROS minimo (con l'affinamento parabolico sui tre punti vicini), ROS minimo, banda con ROS sotto la soglia e Z alla frequenza del modello; lo **scarto in %** è in rosso oltre 0,3 %. L'**interpretazione** dice se lo spostamento è uniforme (boom, staffe, dielettrico non modellati) o no (adattamento, balun, cavo) e la **proposta** indica di quanto accorciare o allungare il radiatore per lato, oppure di attivare la correzione del boom: il programma non cambia nulla da solo. Collaudo: lettura RI e DB; un carico 37 + j22 ohm dietro 18,4 m di cavo con perdite torna esatto.

## Tolleranze costruttive

Quanto contano gli errori di costruzione di una Yagi. **Metodo**: **Monte Carlo** (prove con errori casuali; seme fisso = risultati ripetibili) o **Sensibilità rapida** (un elemento alla volta). **Tolleranze**: lunghezza degli elementi, posizione sul boom, diametro (± mm); distribuzione uniforme o gaussiana (la tolleranza = 2 sigma); **foratura del boom** (errore di posizione) e **errore della staffa** (elemento fuori centro lungo il suo asse; fumetto giallo). **Prove**: numero, frequenze (inizio e fine della banda: ogni prova è un calcolo NEC a tre frequenze), seme. **Obiettivo della lista di taglio**: calo massimo del F/B e ROS massimo.

Gli elementi sono i fili diritti (un tag = un elemento), ordinati lungo il boom e chiamati R, DR, D1, D2... Gli errori si applicano alla geometria a segmenti: la lunghezza si allunga o si accorcia in modo simmetrico rispetto al centro. Risultati: guadagno, F/B e ROS nominali e nel 5 % delle prove peggiori, istogramma del F/B con le linee "5 % peggiore" e "nominale", **sensibilità** di ogni elemento (calo del F/B per 1 mm di errore di lunghezza; in rosso i critici) e **lista di taglio** con la **tolleranza ammessa**: l'errore di lunghezza che da solo fa calare il F/B del calo massimo diviso per la radice del numero degli elementi (errori indipendenti) e non porta il ROS sopra l'obiettivo (se il ROS nominale lo supera già, conta solo l'aumento). In rosso gli elementi che chiedono meno della tolleranza di costruzione. **Stampa lista di taglio** (PDF) ed **Esporta CSV**. Collaudo: con tolleranze nulle ogni prova coincide con il nominale; nomi R DR D1 su una Yagi a 3 elementi.

## Avvisi: test AGT

Nella linguetta Avvisi il **test del guadagno medio (AGT)** si fa da sé: il programma toglie perdite e terreno reale e calcola il guadagno medio in spazio libero (atteso 1,000), su terreno perfetto (atteso 2,000) e con i segmenti raddoppiati. Oltre ±0,2 dB l'avviso propone di verificare la segmentazione; il guadagno si può mostrare corretto con l'AGT. Si esegue da solo all'apertura della linguetta fino a 1500 segmenti. Fonte: guida del NEC-2 (Burke), EZNEC (W7EL).

## Oggetti del modello: linea e accordatore, cavo e balun, telaio dello stack

**Linea e accordatore** (nella voce Linee): perdita di una linea con perdite e ROS, come TLW dell'ARRL, più la stima della perdita dell'accordatore.

**Cavo e balun**. La calza del cavo diventa un filo NEC attaccato a un lato del punto di alimentazione (com'è nella realtà senza balun): 5 cm sotto il radiatore, **lungo il boom** fino al palo, **discesa sul palo** e, in HF, **fino alla stazione** (con la **messa a terra all'ingresso**). Con **calza a massa sul palo** la calza è collegata al palo (anche lui un filo). Il balun è un'impedenza di modo comune (scheda LD 4, R + jX alla frequenza) sul primo tratto della calza. L'elenco dei balun cambia con la banda (in grigio quelli dell'altra banda):

| Balun | Modello di modo comune | Approssimazione (fumetto giallo) |
|---|---|---|
| Choke a spire di coax (HF) | 8 spire di RG-213 su 110 mm: L di Wheeler, capacità propria di Medhurst, Q 50 | l'impedenza vera va misurata |
| Toroide 1:1, mix 31/43 (HF) | RLC parallelo, 5 kohm al picco (10 MHz mix 31, 25 MHz mix 43), Q 0,6 | valori tipici di 12 spire su 2 x FT-240 |
| λ/2 coassiale 4:1 (VHF/UHF) | 600 ohm risonanti a f0, Q 3 | dipende dal montaggio |
| Manicotto λ/4 (VHF/UHF), Pawsey stub | linea λ/4 in corto con perdite (Z0 50 o 100 ohm, 0,08 dB) | |
| Ferriti a perline | 10 perline, 40 ohm ciascuna a 144 MHz (mix 61), 100 ohm sopra 30 MHz (mix 43) | valori dei cataloghi |

Confronto: corrente sulla calza (% della corrente al punto di alimentazione), F/B, F/R medio, guadagno, Z e ROS, perdita nel balun e **potenza dissipata a 1 kW**, banda con la calza sotto il 5 %. Vista laterale con la calza colorata secondo la corrente e grafico della corrente lungo la calza. Collaudo: il manicotto a f0 supera 1000 ohm e riduce la corrente sulla calza di un dipolo a 144 MHz di oltre tre volte.

**Telaio a H e palo nello stack** (nella voce Stack). Con **Includi telaio nel calcolo** montanti, traversa, palo e calze dei cavi di fase diventano fili NEC (acciaio zincato o alluminio con la loro conducibilità; la vetroresina non entra nel NEC). Tipi: H (un montante per colonna e una traversa a metà altezza), croce, solo palo, traliccio (palo con il raggio equivalente di un traliccio da 30 cm). I montanti passano per il boom a metà tra due elementi, per non toccarli. I cavi di fase partono da un lato del punto di alimentazione, seguono boom, montante e traversa fino al divisore al centro. Tabella dei pezzi e confronto senza / con telaio: guadagno, F/B, F/R medio, lobo laterale peggiore nel piano E, Z all'ingresso del divisore (impedenze attive in parallelo), Tant e G/T (elevazione 10 gradi, cielo 200 K, suolo 290 K, come la linguetta Rumore/GT). Fumetto giallo sulla polarizzazione: in orizzontale pesano la traversa e i cavi, in verticale i montanti.

## Calcolatori: Carichi HF, Divisori, Yagi incrociate

**Carichi HF** (famiglia HF): trappola LC, trappola coassiale, bobina di carico con l'autorisonanza (Medhurst), linear loading, cappello capacitivo, bobina dalle misure. Per la trappola coassiale: cavo dall'abaco, spire, collegamento **standard** o **W8NX bassa impedenza** (la trappola vista come Z/4: L/4 e C x 4), pulsante "Calcola spire per f0"; grafico di R e X in frequenza con la banda da bloccare; tabella per banda (Z, comportamento, tensione alla potenza scelta, potenza persa). Avvisi gialli: tensione di lavoro del cavo superata (MIL-DTL-17), autorisonanza sotto tre volte la frequenza di lavoro. Collaudo: la trappola W8NX dell'ARRL Antenna Book (8,75 spire di RG-59 su PVC da 60,3 mm, 4 µH, 123 pF, 7,15 MHz) dà 3,97 µH, 123 pF, 7,19 MHz.

**Divisori e cavi di fase** (famiglia VHF). Tipi: λ/4 in tubo quadro, in tubo tondo, in cavo (due cavi da 75 ohm in parallelo), **Wilkinson** (rami λ/4 da Zin · √N e resistenze a stella: isola le uscite anche in 144 MHz), solo cavi di fase.

| Formula | Uso |
|---|---|
| Zcarico = Zantenna / N; Z0 = √(Zin · Zcarico) | trasformatore λ/4 |
| tubo quadro con conduttore tondo: Z0 = 138 · log(1,0787 · D/d) / √εr | diametro del conduttore interno (ARRL, DL6WU) |
| tubo tondo: Z0 = 138 · log(D/d) / √εr | coassiale |
| λ/4 = 299 792 / f / 4 · VF (mm), meno l'accorciamento dei connettori | lunghezza da tagliare |
| cavi di fase: n · λ/2 · VF | l'impedenza attiva si ripete al divisore |
| tilt: allungamento di una riga = dv · sin(tilt) · VF | lobo inclinato |

Accorciamento dei connettori: 4 mm per estremo con i connettori N, 3 BNC, 1,5 SMA (fumetto giallo: tarare con il VNA). Con **Collega allo Stack** il carico è l'insieme delle impedenze attive dello stack aperto e la lunghezza minima dei cavi viene dalle distanze. Banda con ROS sotto 1,1, perdita stimata (alluminio in aria o cavo), tabella dei cavi di fase con la tolleranza di fase. Numero dispari di antenne: avviso con la proposta (due trasformatori in cascata o Wilkinson). **Disegno 1:1 (PDF)**: vista laterale quotata e sezione su fogli A4 orizzontali con 10 mm di sovrapposizione e un segmento di controllo di 50 mm. Collaudo: 4 vie da 50 ohm -> Z0 25 ohm, conduttore 14,2 mm nel tubo quadro da 20 mm, λ/4 519,7 mm a 144,2 MHz.

**Yagi incrociate** (famiglia VHF). Due Yagi uguali (il modello aperto) ruotate di ±45 gradi (X) o 0/90 gradi (+) attorno al boom, sfalsate di λ/4 lungo il boom oppure sullo stesso punto (ritardo con il cavo). Commutazione: orizzontale, verticale, RHCP, LHCP, X-pol (due ricevitori, EME). Per ogni modo il programma prova le quattro fasi della rete sul piano B (0, 90, 180, 270 gradi) e tiene quella con la componente voluta più forte in asse; dice quale fase serve (λ/4 di ritardo, λ/2 in più). Componenti circolari con la convenzione IEEE. Risultati: guadagno in dBic, **rapporto assiale** in asse e a -3 dB, rifiuto della polarizzazione opposta, Z all'ingresso (con il cavo di ritardo), **isolamento tra i due piani** (dalle correnti con un solo piano alimentato, S21 su 50 ohm). Grafici: co-polare e cross-polare, rapporto assiale nel lobo, rapporto assiale e ROS in frequenza. **Includi relè e cavi reali**: perdita del cavo di ritardo e 0,1 dB di relè (fumetto giallo). La stessa rete serve per un turnstile.

## Calcoli meccanici

Nuova linguetta principale dopo i Calcolatori, con le sottolinguette **Vento**, **Bilanciamento**, **Boom e tirante**, **Palo e plinto**, **Sollecitazioni elementi (HF)**; le ultime tre sono previste (in preparazione) e restano spente. Tutte le forze e i momenti si leggono in **N e kgf affiancati**, solo N o solo kgf (scelta **Mostra**; 1 kgf = 9,80665 N).

**Vento.** Norma: NTC 2018 con la Circolare 7/2019 per velocità e pressione; CNR-DT 207 R1/2018 ed EN 1991-1-4 per i coefficienti. TIA-222-H, ASCE 7-22, AS/NZS 1170.2 e AIJ sono visibili ma spente finché non ci sono i testi. **Sito**: locatore (la zona NTC si propone da sola: Capri = zona 9, 31 m/s; confini approssimati, fumetto da verificare sulla tabella 3.3.I), zona, quota del sito (ca), **periodo di ritorno** (cr = 0,75 √(1 - 0,2 ln(-ln(1 - 1/Tr))), NTC 3.3.2), categoria di esposizione (ce alla quota dell'antenna), coefficiente di topografia ct. **Struttura**: antenna da terra, palo sopra il rotore e suo diametro, boom; caselle per usare elementi e boom del modello e per includere telaio, palo e cavi.

| Grandezza | Formula | Fonte |
|---|---|---|
| vb | vb,0 · ca · cr | NTC 2018 3.3.1-3.3.2 |
| qr | ½ · ρ · vb² (ρ = 1,25 kg/m³) | NTC 2018 3.3.6 |
| q | qr · ce · ct | NTC 2018 3.3.4 |
| cf0 dei tubi tondi | dal numero di Reynolds (sotto 2·10^5: 1,2) | EN 1991-1-4 §7.9.2; CNR-DT 207 App. G |
| sezioni quadre | cf0 = 2,1 | EN 1991-1-4 §7.6 |
| ψλ | dalla snellezza | EN 1991-1-4 §7.13 |
| direzione del vento | conta solo la componente perpendicolare all'asse del tubo: forza con il quadrato di quella componente (flusso trasversale) | K5IU; ARRL Antenna Book, appendice sui carichi del vento |

Risultati: tabella delle forze per parte con il vento lungo il boom (gli elementi sono di fianco), forza totale e momento torcente sul rotore in funzione della direzione del vento, forza massima con la sua direzione, momento flettente alla staffa del rotore, momento torcente sul rotore. **Relazione PDF**. Avviso: è un predimensionamento, non sostituisce la verifica di un tecnico abilitato. Collaudo: zona NTC di Capri, Milano, Palermo e Cagliari; un elemento con il vento a 0, 45 e 90 gradi dal boom dà 1200, 600 e 0 N (flusso trasversale).

**Bilanciamento.** **Da bilanciare**: peso e vento insieme (compromesso a metà tra baricentro e centro dell'area), solo peso, solo vento. **Masse dal modello**: elementi (alluminio, tondino pieno o tubo con lo spessore tipico), boom (dalla tabella), staffe e isolatori (g ciascuno), dipolo + scatola, cavo fino al palo (kg/m). **Staffa al palo**: posizione attuale dal riflettore, contrappeso (massa e distanza), vento dalla linguetta Vento (altrimenti 600 N/m²). Vista laterale con baricentro, centro di spinta e staffa; momenti del peso e del vento in funzione della posizione della staffa; tabella di masse e aree; risultati: bilanciamento del peso, del vento, posizione consigliata e torcente sul rotore con la staffa attuale e con quella consigliata. Gli elementi non contano per il torcente: con il vento di traverso sono paralleli al vento (flusso trasversale). **Sposta staffa nel modello** trasla tutto il modello lungo il boom in modo che la staffa consigliata cada nell'origine (si annulla con Annulla della Struttura 3D).

## Filo scelto nella vista 3D

Un clic su un filo nella vista 3D lo sottolinea con un colore velato e porta la tabella dei fili sulla sua riga, che si evidenzia con lo stesso colore (più leggero, per leggere i numeri). Vale anche al contrario: un clic su una riga della tabella sottolinea il filo nella vista 3D. Il colore (arancio, giallo, verde, azzurro, viola, rosso, blu) e la velatura (leggera, media, forte) si scelgono in **Impostazioni > Struttura 3D**. Con una scheda della barra Geometria aperta il clic continua a scegliere i fili da trasformare, come prima.

## Correzione: chiusura con il cursore in un campo

Con il cursore in un campo di un calcolatore, il clic su un'altra voce dell'elenco a sinistra poteva chiudere il programma (registro del 13/11/2026, eccezione di Avalonia "Visual is attached to visual tree but root could not be found"): l'uscita dal campo faceva ricalcolare una pagina che stava già uscendo dalla finestra. Ora l'elenco a sinistra (e le pillole di Parabola e Trombe) chiude il campo prima di cambiare pagina; se l'eccezione dovesse ripresentarsi, si scrive nel registro e il programma resta aperto. Il collaudo automatico prova il caso su tutte le pagine.

## Fonti: somiglianze e differenze

| Fonte | Somiglianze | Differenze |
|---|---|---|
| HFTA di N6BV (ARRL Antenna Book) | raggi riflessi tratto per tratto, diffrazione sui crinali, direzioni di traffico, figura di merito | il diagramma viene dal NEC del modello aperto (HFTA usa diagrammi suoi di dipoli e Yagi); il profilo può venire dal DEM; mancano ancora le statistiche degli angoli di arrivo dell'ARRL: la figura di merito è la media uniforme su 1-15 gradi in HF (0,5-5 in VHF) |
| ON4UN, Low-Band DXing (DMF) | RDF e DMF come cifre di merito delle antenne riceventi | ON4UN definisce il DMF con il solo guadagno a 20 gradi; qui è la media sul settore avanti |
| TLW dell'ARRL | perdita di una linea con perdite e ROS | in più la stima della perdita dell'accordatore |
| W8NX, trappola coassiale (ARRL Antenna Book) | trappola a bassa impedenza vista come Z/4 | collaudo: la trappola del libro (4 µH, 123 pF, 7,15 MHz) qui dà 3,97 µH, 123 pF, 7,19 MHz |
| DL6WU, ARRL (linee in tubo quadro) | Z0 = 138 · log(1,0787 · D/d) / √εr | in più l'accorciamento dei connettori, il Wilkinson e il disegno 1:1 in PDF |
| EZNEC (W7EL), guida del NEC-2 (Burke): test AGT | guadagno medio atteso 1 in spazio libero e 2 su terreno perfetto | qui si esegue da solo all'apertura della linguetta Avvisi, anche con i segmenti raddoppiati |
| K5IU, ARRL Antenna Book (vento) | flusso trasversale: conta solo la componente del vento perpendicolare al tubo | i coefficienti vengono dalle norme NTC 2018, CNR-DT 207 ed EN 1991-1-4 |

## Crediti delle fonti della 1.2

| Fonte | Usata per |
|---|---|
| N6BV (R. D. Straw), HFTA, ARRL Antenna Book | metodo del terreno reale, direzioni di traffico, figura di merito |
| TINITALY 1.1, INGV: Tarquini S., Isola I., Favalli M., Battistini A., Dotta G. (2023), doi 10.13127/tinitaly/1.1, licenza CC BY 4.0 | DEM dell'Italia |
| SRTM1 (NASA / USGS), mattonelle "skadi" di AWS Terrain Tiles (Mapzen) | DEM del mondo |
| Esri World Imagery (Esri, Maxar, Earthstar Geographics e la comunità GIS); OpenStreetMap (© i contributori di OpenStreetMap, ODbL) e Nominatim | sfondi della mappa e ricerca delle località |
| ITU-R P.526, P.372, P.453; Ament (1953) | diffrazione, rumore, raggio efficace, rugosità |
| ON4UN, Low-Band DXing; W8JI; IV3PRK | RDF, DMF, antenne riceventi, ghiaccio |
| W8NX, ARRL Antenna Book 24a ed. | trappola coassiale a bassa impedenza |
| K5IU (flusso trasversale); ARRL Antenna Book, appendici A e B | vento sui tubi |
| NTC 2018 e Circolare 7/2019; CNR-DT 207 R1/2018; EN 1991-1-4 | vento |
| DL6WU, ARRL (linee in tubo quadro); Wilkinson (1960) | divisori |
| VE7BQH (TANT), KF2YN | Tant e G/T nel confronto del telaio |
| G. J. Burke, NEC-2 e NEC-4.2 (LLNL); 4nec2 (A. Voors); L. B. Cebik W4RNL | AGT, giunzioni NEC-4, confronti |
| ARRL, TLW | linea e accordatore |
| Abramowitz e Stegun, Handbook of Mathematical Functions | integrali di Fresnel |

[doc/Manuale_d_uso.pdf](https://github.com/ic8tem/YA-NEC/blob/main/doc/Manuale_d_uso.pdf) (23)
