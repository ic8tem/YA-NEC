# Manuale di calcolo e delle fonti

## Come e' fatto questo documento

Il **manuale d'uso** dice cosa fare, casella per casella. Questo documento dice **come si calcola**: per ogni calcolo di YA-NEC ci sono le formule, le ipotesi, i limiti, le fonti con le somiglianze e le differenze, e il caso di prova che l'autotest ripete a ogni versione.

I numeri tra parentesi quadre, come [C3], rimandano al documento **Citazioni**, dove ogni fonte ha le pagine usate e il posto in cui si usa in YA-NEC. I rimandi come [[Uso: Distanze di stack: accoppiamento e distanza migliore]] aprono il manuale d'uso al paragrafo.

Le formule sono scritte in testo semplice: `sqrt` e' la radice quadrata, `log` il logaritmo in base 10, `ln` quello naturale, `^` la potenza, λ la lunghezza d'onda, Z0 l'impedenza di riferimento.

> NOTA: YA-NEC prende dalle fonti idee, formule pubblicate e numeri di confronto. Non copia codice ne' testo; dove un metodo viene da un programma, e' riscritto da capo in C# e lo dice la citazione.

## 1. Il motore NEC-2

### 1.1 Il metodo dei momenti

YA-NEC calcola le correnti sui fili con il metodo dei momenti del NEC-2 [C3]: ogni filo e' diviso in segmenti, su ogni segmento la corrente e' la somma di tre termini (costante, seno e coseno) e la condizione al contorno (campo elettrico tangente nullo sulla superficie del conduttore) si impone al centro di ogni segmento. Ne viene un sistema lineare complesso:

> FORMULA: [Z] · [I] = [V]

dove [Z] e' la matrice delle interazioni tra i segmenti, [V] le tensioni delle sorgenti e [I] le correnti cercate. Dalle correnti si calcolano impedenza d'ingresso, campi vicini e lontani, diagramma, guadagno ed efficienza.

### 1.2 Da dove viene il codice

Il motore e' la traduzione in C# di nec2c di N. Kyriazis 5B4AZ [C1] (routine di geometria, matrice, campi e diagramma, SOMNEC per il terreno di Sommerfeld). Cambiano solo le parti di calcolo numerico:

| Parte | YA-NEC | nec2c | Fonte |
|---|---|---|---|
| Soluzione del sistema | fattorizzazione LU a blocchi con pivot parziale, istruzioni vettoriali per i prodotti complessi | LU classica | [C29] |
| Simmetrie GX e GR | soluzione scritta in forma di gruppo (caratteri delle rotazioni e delle riflessioni) | stesso metodo | [C3], [C1] |
| Sweep in frequenza | funzioni razionali (MBPE) con punti infittiti dove due stime non concordano | un calcolo per frequenza | [C57] |
| Terreno di Sommerfeld | routine evlua come il NEC-2 originale (in nec2c un difetto sbaglia fino al 5 % sull'impedenza dei fili a 0,2-1 λ sul mare) | evlua con il difetto | [C1], [C2] |

### 1.3 Somiglianze e differenze con le fonti

| Fonte | Somiglianze | Differenze |
|---|---|---|
| NEC-2, Burke e Poggio [C3] | stesso metodo, stesse schede (GW, GN, EX, LD, TL, NT, CP, RP), stessi limiti dei fili sottili | YA-NEC aggiunge avvisi automatici sui limiti della guida (capitolo 2) |
| nec2c [C1] | stesse routine, tradotte riga per riga e confrontate punto per punto | LU a blocchi, MBPE, correzione di evlua |
| necpp / nec2++ [C2] | terzo riferimento numerico: compilato a parte, solo i valori dei casi di prova | nessun codice usato |

### 1.4 Casi di prova (autotest)

Dipolo a 144,3 MHz in spazio libero (21 segmenti), lo stesso dipolo scritto con le variabili, dipolo con il nucleo esteso, dipolo su terreno medio, ground plane a 4 radiali, perdite del rame e dell'acciaio, carichi LD 0 e LD 4, fili copiati con GM e GX, four-square con tensioni complesse: per ognuno YA-NEC deve coincidere con nec2c entro lo 0,05 %. Gli esempi 1, 2 e 3 della parte III del NEC-2 [C3] si confrontano con i valori pubblicati.

### 1.5 MININEC e NEC-2: commento dall'analisi delle fonti

YA-NEC calcola con il NEC-2 (nec2c) e non ha un motore MININEC. I due metodi risolvono lo stesso problema (correnti su fili sottili, metodo dei momenti) in modo diverso, e molti programmi da radioamatore usano il MININEC (MMANA e MMANA-GAL, ELNEC, il motore MININEC di EZNEC, AO, NEC4WIN). Confronto delle fonti:

| Aspetto | MININEC (J. C. Logan e J. W. Rockway, NOSC, dal 1982) | NEC-2 (G. J. Burke e A. J. Poggio, LLNL, 1981) | Fonte |
|---|---|---|---|
| Dove si calcola la corrente | impulsi di corrente sulle giunzioni tra due segmenti; sorgenti e carichi stanno sulle giunzioni (dipolo alimentato al centro: segmenti pari) | al centro di ogni segmento, con tre termini (costante, seno, coseno); sorgenti e carichi sul segmento (dipolo: segmenti dispari) | Cebik, "Under the Limits: MININEC" (amod2); guida del NEC-2 |
| Terreno reale | impedenza e correnti come su terreno perfetto; costante dielettrica e conducibilita' servono solo al diagramma | Sommerfeld-Norton (GN 2): l'impedenza sente il terreno; radiali anche a pochi cm | Cebik amod2 e amod99 |
| Fili bassi | inaffidabile per fili orizzontali o inclinati sotto circa 0,2 lambda | affidabile molto piu' in basso (limiti al capitolo 3) | Cebik amod2, amod56 |
| Segmentazione | almeno 10 segmenti per mezza lunghezza d'onda, il doppio per antenne complesse; agli angoli servono molti segmenti o la rastremazione (MMANA la fa da se') | segmenti tra lambda/1000 e lambda/10, simili alle giunzioni | Cebik amod2; guida del NEC-2 |
| Frequenza | con il MININEC 3.13 originale la risonanza si sposta verso l'alto con la frequenza (circa 150 kHz, lo 0,5 %, a 28,5 MHz), corretto nelle versioni piu' recenti | riferimento del confronto | Cebik amod2 |
| Diametri a gradini | risultati accurati senza correzioni | serve la correzione di Leeson (YA-NEC la fa come EZNEC, capitolo 4) | Cebik, "When MININEC is Superior to NEC" (amod56) |
| Fili vicini di diametro diverso, giunzioni ad angolo tra diametri diversi | meglio del NEC-2 (folded dipole a salire: MININEC 530 ohm, come atteso; NEC-2 375 ohm) | errori frequenti; YA-NEC li segnala negli avvisi | Cebik amod56 |
| Dimensione dei modelli | storicamente 128-256 impulsi | migliaia di segmenti, superfici (patch), linee e reti (TL, NT), terreni a radiali (GN con NRADL) | Cebik amod2; guida del NEC-2 |

**Somiglianze.** Per una grande classe di antenne (Yagi monobanda alte, dipoli, verticali su terreno perfetto) i due metodi danno gli stessi risultati entro le tolleranze di costruzione: per Cebik "both NEC and MININEC are equally apt". Tutti e due chiedono fili sottili, segmenti non troppo corti e una prova di convergenza.

**Differenze che contano per YA-NEC.** Il MININEC e' piu' robusto dove il NEC-2 e' debole (diametri a gradini, diametri diversi vicini o ad angolo), ma non sa calcolare l'impedenza su terreno reale: per le verticali, i radiali, le antenne basse e le Beverage il NEC-2 con Sommerfeld e' l'unico dei due che risponde. Il NEC-2 ha poi i modelli grandi (parabole a superfici, stack, LPDA con le linee), le reti e il codice pubblico (nec2c).

**Scelta di YA-NEC.** Il motore resta il NEC-2; le sue debolezze si coprono come fa EZNEC: correzione di Leeson per i tubi telescopici, avvisi sulle giunzioni e sui fili vicini, verifica della segmentazione, AGT. Il "terreno MININEC" (GN 3, capitolo 3) c'e' solo per leggere e confrontare i file di EZNEC con quel terreno: le correnti restano quelle del NEC-2. I file MMANA si leggono risegmentati per il NEC-2 (manuale d'uso, capitolo 10).

## 2. Segmentazione, AGT, convergenza

### 2.1 Limiti della guida del NEC-2

La guida per l'utente del NEC-2 [C3] (parte III, sez. II "Structure Modeling Guidelines") fissa i limiti che YA-NEC controlla a ogni calcolo:

| Grandezza | Limite | Avviso di YA-NEC |
|---|---|---|
| lunghezza del segmento Δ | λ/1000 < Δ < λ/10 | segmenti troppo corti o troppo lunghi |
| rapporto lunghezza/raggio del segmento | oltre 8 (oltre 2 con il nucleo esteso EK) | verifica della segmentazione |
| fili in una giunzione | non oltre circa 30 [C84] | troppi fili in un punto |
| contatti fuori dalle giunzioni | le sei regole del Geometry Check di EZNEC [C5] | estremo a meta' segmento, fili incrociati, vicini, paralleli, sovrapposti, centro del segmento finale dentro un altro filo |

### 2.2 AGT (Average Gain Test)

In spazio libero e senza perdite la potenza irradiata e' uguale a quella d'ingresso, quindi la media del guadagno sulla sfera vale 1 (0 dB):

> FORMULA: AGT = (1 / 4π) · ∫∫ G(θ, φ) sinθ dθ dφ        AGT dB = 10 · log(AGT)

Entro ±0,2 dB il modello e' affidabile, oltre 0,5 dB va rivista la segmentazione [C13]. Con il terreno reale l'AGT non ha senso (si scrive n.a.).

**Efficienza.** Di norma si usa il bilancio del NEC-2: potenza d'ingresso meno perdite. Sui piccoli loop ad alto Q questa e' la differenza di due numeri quasi uguali e da' efficienze negative: se il bilancio non torna (efficienza negativa, o lontana piu' del 25 % dalla media del guadagno) YA-NEC usa

> FORMULA: η = Prad / (Prad + Pperdite)

con Prad dal campo lontano, e mette un avviso. E' una scelta di YA-NEC: il campo lontano viene dalle correnti, che il NEC calcola bene anche dove il bilancio e' impreciso.

### 2.3 Convergenza

L'AGT da solo non basta: nel collaudo la Yagi DK7ZB ha AGT a 0 dB con 20, 40 e 80 segmenti per λ, ma l'impedenza passa da 26,0 a 28,1 a 30,4 ohm. Per l'impedenza si confrontano almeno due segmentazioni (prova di convergenza dell'ARRL [C13], cap. 4, Tab. 5 e 6).

| Fonte | Somiglianze | Differenze |
|---|---|---|
| ARRL Antenna Book 21ª ed. [C13] | AGT e prova di convergenza raddoppiando i segmenti | YA-NEC li fa da solo e li mostra negli Avvisi |
| EZNEC [C5] | Segmentation Check e Geometry Check | EZNEC si ferma per il Geometry Check; YA-NEC calcola lo stesso e avvisa con le parole di EZNEC |
| Pellejero [C84], Cebik [C33] | quote minime dei fili sul terreno, giunzioni affollate | soglie riportate negli avvisi (capitolo 3) |

### 2.4 Casi di prova

Prova con EZNEC Pro/2+ 7.0 [C87]: dei 601 modelli confrontabili, i 26 che EZNEC ferma per geometria hanno tutti almeno un avviso in YA-NEC, i 575 che EZNEC calcola nessuno.

### 2.5 Modelli tipicamente critici: analisi delle fonti

Quali modelli mettono in difficolta' il NEC-2 (e in genere i programmi a fili), cosa dicono le fonti e cosa fa YA-NEC. Gli scarti citati vengono dai confronti con EZNEC del collaudo.

| Tipo di modello | Perche' e' critico | Cosa fa YA-NEC | Fonti |
|---|---|---|---|
| Elementi a tubi telescopici (diametri a gradini): Yagi HF, verticali a tubi | il NEC-2 fa risuonare l'elemento piu' in basso del vero (circa il 2 % sul riflettore 20 m della Force 12 XR-5); senza correzione il diagramma di una Yagi puo' rovesciarsi | correzione di Leeson come EZNEC (capitolo 4); senza, lo scarto mediano da EZNEC delle Yagi a gradini e' del 52,8 %, con la correzione lo 0,32 % sugli elementi | Leeson, Physical Design of Yagi Antennas (ARRL 1992), sez. 8.4; Cebik, "NEC Correctives" e "Tapering to Perfection"; EZNEC (Stepped Diameter Correction) |
| Fili molto vicini al terreno, radiali sul suolo, Beverage | sotto circa lambda/100 il NEC-2 perde precisione; con il terreno a coefficiente di riflessione (GN 0) l'impedenza delle antenne basse e' sbagliata; il MININEC sotto 0,2 lambda e' inaffidabile | avvisi automatici (capitolo 3 e manuale d'uso, capitolo 11), Sommerfeld consigliato; nel collaudo i soli modelli d'archivio ancora lontani da EZNEC (2-6 %) hanno fili entro lambda/100 dal suolo | guida del NEC-2 (Burke e Poggio); Cebik, "S-N, RCA, and MININEC Grounds" (amod99) e "Under the Limits: MININEC" (amod2) |
| Fili a 0,2-1 lambda su terreni molto conduttivi (mare) con Sommerfeld | il difetto di nec2c (corretto in YA-NEC) sbaglia proprio questa zona delle tabelle di Sommerfeld (fino al 5 % sull'impedenza) | evlua come il NEC-2 originale; 6 prove fisse contro necpp | doc/Sommerfeld_difetto_nec2c.md; necpp (T. Molteno) |
| Giunzioni ad angolo tra fili di diametro diverso; fili paralleli vicini di diametro diverso (folded dipole a salire, open-sleeve, hairpin, gamma) | il NEC-2 sbaglia spesso questi casi: folded dipole a salire 375 ohm contro 530 attesi; il MININEC li tratta meglio | avvisi delle giunzioni e dei fili vicini; verifica della segmentazione; con fili vicini segmenti allineati | Cebik, "When MININEC is Superior to NEC" (amod56); EZNEC, Geometry Check |
| Angoli acuti e loop (delta, quad, V invertite, Moxon) | agli angoli i centri dei segmenti si avvicinano all'altro filo; servono segmenti piu' corti vicino al vertice | regole del Geometry Check di EZNEC (centri dei segmenti dentro l'altro filo); la correzione di Leeson esclude gli elementi con fili attaccati alla punta | guida del NEC-2; EZNEC; Cebik amod2 (MININEC: angoli, rastremazione) |
| Segmenti troppo corti o troppo lunghi, rapporto lunghezza/raggio basso, segmenti diversi ai lati della sorgente | fuori da lambda/1000 - lambda/10 e con lunghezza/raggio sotto 8 (2 con il kernel esteso) il calcolo perde precisione; un segmento della sorgente piu' corto dei vicini cambia la resistenza (dipolo con la piastra centrale: circa 5 ohm) | verifica della segmentazione con correzione; kernel esteso a scelta | guida del NEC-2; prove del collaudo (staffe) |
| Piccoli loop magnetici e antenne molto corte ad alto Q | il bilancio di potenza del NEC-2 (ingresso meno perdite) diventa la differenza di due numeri quasi uguali | efficienza dal campo lontano quando il bilancio non torna, con un avviso; AGT | guida del NEC-2 (AGT); scelta di YA-NEC (capitolo 2) |
| Multibanda con elementi intercalati o con un solo punto di alimentazione | gli elementi delle altre bande disturbano; piu' generatori uguali danno resistenze negative su alcuni radiatori | modellare tutti gli elementi; una sorgente per banda o il collegamento vero (linea TL); trappole come carichi | ARRL Antenna Book 24a, 11.5; casi di studio del manuale d'uso, capitolo 18 |
| Trappole e carichi validi a una sola frequenza (trappole RLC e carichi di Laplace di AO, impedenze LD 4) | il valore e' giusto solo alla frequenza a cui e' stato calcolato | avviso all'importazione; negli sweep va rifatto il carico o usato l'RLC | ANTFILE.DOC (K6STI); guida del NEC-2 (LD) |
| Modelli molto grandi (stack, parabole a superfici, migliaia di segmenti) | tempi di calcolo e memoria; convergenza da verificare | LU a blocchi con le istruzioni vettoriali, AGT e prova di convergenza, collaudo dei modelli pesanti | guida del NEC-2; collaudo (fase 2E) |

**Somiglianze tra le fonti.** Tutte indicano gli stessi punti deboli dei programmi a fili sottili: salti di diametro, giunzioni, fili vicini tra loro o al suolo, segmentazione. Tutte chiedono una prova di convergenza (raddoppiare i segmenti) e l'AGT prima di fidarsi del risultato.

**Differenze.** La guida del NEC-2 da' limiti numerici generali (segmenti, raggi, kernel). Cebik confronta i motori sugli stessi modelli e mostra che il MININEC e' migliore sui salti di diametro e sulle giunzioni, il NEC-2 sul terreno reale. EZNEC traduce questi limiti in controlli automatici (Geometry Check, Segmentation Check), senza numeri sugli scarti. Il collaudo di YA-NEC aggiunge gli scarti misurati contro EZNEC sui modelli d'archivio.

## 3. Terreni e terreno reale

### 3.1 I due terreni del NEC-2

| Modello | Scheda | Come funziona | Limite |
|---|---|---|---|
| coefficiente di riflessione | GN 0 | il terreno e' uno specchio con il coefficiente di Fresnel dell'onda piana | non sotto λ/10 per i fili orizzontali (Cebik: 0,2 λ) [C84], [C33] |
| Sommerfeld-Norton | GN 2 | integrali di Sommerfeld e onda di terra di Norton, con le tabelle di interpolazione di SOMNEC | non sotto λ/1000 (λ/200 per i piu' prudenti) [C84]; niente fili interrati (serve il NEC-4 [C4]) |
| "MiniNec ground" di 4nec2 | GN 3 | correnti e impedenza su terreno perfetto, εr e σ solo nel diagramma | solo per leggere i file di EZNEC con il terreno MININEC [C6], [C5] |

Coefficienti di Fresnel per la riflessione sul terreno (ψ angolo sopra l'orizzonte, εc = εr - j·60·σ·λ):

> FORMULA: Rv = (εc·sinψ - sqrt(εc - cos²ψ)) / (εc·sinψ + sqrt(εc - cos²ψ))        Rh = (sinψ - sqrt(εc - cos²ψ)) / (sinψ + sqrt(εc - cos²ψ))

### 3.2 Solai in calcestruzzo

> FORMULA: VHF-UHF (ITU-R P.2040): εr = 5,24   σ = 0,0462 · f^0,7822 S/m (f in GHz)
> FORMULA: HF (Perotoni 2019): εr = 20   tanδ = 0,012   σ = 2π · f · ε0 · εr · tanδ

Fonti, somiglianze e differenze: [[Uso: 11. Terreni]] (le due fonti non si raccordano tra 2 MHz e 1 GHz) [C70].

### 3.3 Terreno reale alla HFTA

Il diagramma in elevazione su un profilo vero del terreno segue il metodo di HFTA di N6BV [C8], [C12] (14.21-14.28): ottica geometrica piu' la teoria uniforme della diffrazione (UTD), con la funzione di transizione data dagli integrali di Fresnel [C26]. Il diagramma dell'antenna e' quello del NEC (correnti calcolate); il terreno aggiunge i raggi riflessi su ogni tratto del profilo e la diffrazione sui crinali. Profili dal DEM TINITALY o SRTM1 [C85].

| Effetto | Formula | Fonte |
|---|---|---|
| terra curva | raggio efficace 4/3 · 6371 km | ITU-R P.453 [C70] |
| divergenza sulla terra curva | fattore di divergenza (eq. 4-132/4-134) | Balanis 4.8.4 [C14] |
| rugosita' del mare | R · exp(-2 (k · h0 · sinψ)²) (fattore di Ament; criterio di Rayleigh h < λ / (8 sinψ)) | Balanis eq. 4-137 [C14]; ITU-R P.526 [C70] |
| diffrazione sul crinale | UTD con funzione di transizione di Fresnel | Kouyoumjian-Pathak tramite HFTA [C8]; [C26] |

| Fonte | Somiglianze | Differenze |
|---|---|---|
| HFTA, N6BV [C8], [C12] | stessi cinque meccanismi (riflessione, diffrazione diretta, diffratto poi riflesso, riflesso poi diffratto, diffrazione multipla), profilo distanza/quota | HFTA usa una sorgente puntiforme (coseno²) e non tiene gli accoppiamenti; YA-NEC usa il diagramma NEC del modello. HFTA non fa la polarizzazione verticale |
| Balanis [C14] | divergenza e rugosita' | Balanis usa εr 81 e σ 4,64 S/m per il mare, YA-NEC 81 e 5 S/m |

### 3.4 Verifica contro nec2c

Beverage 175 m a 2,5 m, 1,83 MHz, terreno medio di Sommerfeld: Z = 701,09 - j146,50 ohm; verticale λ/4 con 4 radiali a 0,5 m: Z = 37,498 - j8,229 ohm. Identici a nec2c [C1].

### 3.5 Radiali a terra: confronto delle fonti

La scheda "Verticale lambda/4 con radiali a terra" usa la Tab. 9-1 di ON4UN; le altre fonti si mostrano accanto per giudicarne l'attendibilita'.

| Fonte | Che cosa e' | Somiglianze | Differenze |
|---|---|---|---|
| G. H. Brown, R. F. Lewis, J. Epstein, "Ground Systems as a Factor in Antenna Efficiency", Proc. IRE, 1937 (RCA) | misure originali: campo a distanza fissa con 2-113 radiali interrati, lunghezze circa 0,14-0,41 lambda, circa 3 MHz, un solo terreno | e' la base comune: ON4UN e la scheda di YA-NEC ne derivano | un solo terreno e una sola frequenza; la parte teorica (rapporto tra corrente nel terreno e nei radiali) non concorda con i calcoli moderni (N6MW, 2016); le misure restano valide |
| ON4UN, Low-Band DXing, cap. 9, Tab. 9-1 e 9-3 | resistenza equivalente di terra per numero e lunghezza dei radiali; lunghezza ottima per numero | ricavata dalla Fig. 25 di Brown-Lewis-Epstein: verificata alla fonte, a 88 gradi coincide entro 0,2 ohm con 15 e 60 radiali (incertezza di lettura 2-4 ohm) | forma tabellare, valori interpolati da YA-NEC; Tab. 9-3 e' una regola pratica (0,10 lambda con 4 radiali ... 0,50 lambda con 120) |
| J. Sevick W2FMI (ARRL, misure su verticali in HF) | resistenza di terra con radiali sul suolo: circa 29 / 18 / 9 / 0 ohm con 4 / 8 / 16 / infiniti radiali | stessa tendenza: la resistenza cala con il numero di radiali e si stabilizza oltre qualche decina | con 16 radiali da 0,25 lambda da' circa 9 ohm, la Tab. 9-1 circa 15 ohm: altro terreno, altra frequenza, radiali sul suolo invece che interrati. Per questo la scheda la mostra solo come confronto |
| DJ8WL (misure riportate da ON4UN) | verticale con 8 radiali: "50 ohm perfetti" ma efficienza sotto il 50 % | conferma la Tab. 9-1: con pochi radiali la resistenza di terra e' grande quanto quella di radiazione | non e' una tabella: e' un caso misurato, usato nell'avviso della scheda |

Esempio con i numeri della scheda (16 radiali da 0,25 lambda, 36 ohm di radiazione): Tab. 9-1 circa 14,9 ohm, efficienza 71 %; Sevick circa 9 ohm, efficienza 80 %. La differenza (circa 1 dB) e' il margine da aspettarsi tra terreni diversi. Scelta di calcolo: YA-NEC usa la fonte con le misure originali verificabili (Brown-Lewis-Epstein attraverso ON4UN) e mostra Sevick accanto; il NEC-2 non tratta i radiali interrati (serve il NEC-4), quindi il modello NEC usa il terreno perfetto con la resistenza di terra in serie.

## 4. Correzione di Leeson

### 4.1 Il problema

Il NEC-2 tratta male i salti di diametro: un elemento a tubi telescopici risuona piu' in basso del vero (circa il 2 % sul riflettore 20 m della Force 12 XR-5), e una Yagi a gradini puo' rovesciare il diagramma. La correzione sostituisce ogni elemento a gradini con un elemento di diametro uniforme che risuona alla stessa frequenza, come fa EZNEC [C5] con la Stepped Diameter Correction.

### 4.2 Il metodo

L'algoritmo e' quello del libro di D. Leeson W6NL [C17] (cap. 8, diametri a gradini), basato sull'impedenza caratteristica media di Schelkunoff di ogni tratto. Le condizioni sono quelle di Cebik [C33]: fili diritti, simmetrici rispetto al centro, punte libere, sorgenti e carichi sul segmento centrale, entro circa il 15 % dalla risonanza.

### 4.3 Tubi telescopici: perche' l'algoritmo del libro di Leeson

**Regole della correzione (Cebik, "NEC Correctives", amod82).** La correzione si applica solo a fili diritti, simmetrici rispetto al centro, con le punte libere (un filo attaccato a una punta, come le code della Moxon o i lati di un delta, esclude l'elemento), con sorgenti e carichi sul segmento centrale, entro circa il 15 % dalla risonanza; EZNEC e NEC-Win Plus non la applicano se l'elemento non rispetta una delle condizioni. Eccezione: il monopolo verticale con la base a terra e la sorgente accanto al suolo, che YA-NEC corregge come meta' di elemento (l'immagine nel terreno fa l'altra meta'). **Segmentazione dell'elemento sostituito (scelta di calcolo).** Ogni filo dell'elemento tiene il suo numero di segmenti e le sue giunzioni, avvicinate al centro dello stesso fattore k = l'/l (per il monopolo il centro e' la base); tutti i fili prendono il diametro equivalente. E' quello che fa EZNEC: su 543 elementi a gradini lo scarto mediano da EZNEC e' dello 0,32 %, contro il 3,86 % con tutti i segmenti resi uguali (entro lo 0,5 %: 468 elementi contro 55; ARRL 0,34 % contro 3,94 %, Hy-Gain 0,26 % contro 1,84 %). Cebik ricorda che il risultato dipende dalla segmentazione: con i segmenti uguali la resistenza viene circa il 4 % piu' alta di EZNEC negli elementi ARRL con molti tubi corti. Conseguenza da sapere: un segmento della sorgente piu' corto dei vicini (per esempio la piastra centrale) cambia la resistenza anche di qualche ohm, esattamente come in EZNEC; e' un effetto del NEC-2, non della correzione.

**Linee di fase.** Un elemento con una linea (TL o NT) sul segmento centrale si corregge, come in EZNEC (XR-5 di fabbrica a 14,175 MHz: 61,3 +j42,8 ohm, EZNEC 61,2 +j42,6; senza correggere quell'elemento 81,1 +j73,8). Restano esclusi gli elementi con linee, sorgenti o carichi fuori dal centro. Nota sul nome: l'"equazione di Leeson" che si trova in rete (per esempio su Wikipedia) e' un'altra cosa, il rumore di fase degli oscillatori (D. B. Leeson, Proc. IEEE 1966); la correzione dei tubi viene dal libro di D. Leeson W6NL sulle Yagi (ARRL 1992).

**NEC-4 al posto della correzione?** Il NEC-4 di Burke (LLNL, 1992) tratta i salti di diametro direttamente, senza sostituire l'elemento. Confronto delle fonti:

| Fonte | Somiglianze | Differenze |
|---|---|---|
| L. B. Cebik W4RNL, "NEC-4 vs. NEC-2 With Stepped-Diameter Correction" | NEC-4 e NEC-2 con la correzione danno risultati ragionevoli; NEC-2 senza correzione e' inaffidabile (come nel nostro confronto d'archivio: mediana 52,8 % senza, 2,7 % con Leeson) | il NEC-4 converge male con salti grandi dove la corrente e' alta: 40 m 2 el. 6,23 contro 7,13 dBi, 20 m 3 el. 7,74 contro 10,23 dBi; servono modelli molto grandi |
| A. I. Bauerle, "Accurate Modeling of Stepped-Radii Antennas", ACES Journal 1995 (LP8 10-30 MHz) | NEC-2 con lunghezze equivalenti e NEC-4 diretto concordano entrambi con il ROS misurato | per il NEC-2 le lunghezze equivalenti venivano da un altro programma (ELNEC); nessun numero dell'errore nel riassunto |
| D. Leeson W6NL (1992), EZNEC (Stepped Diameter Correction) | e' la correzione che YA-NEC applica, la stessa di EZNEC, il nostro riferimento | lavora sul singolo elemento e solo vicino alla risonanza; le fasi 2K e 2L del collaudo ne verificano le regole |

Scelta: il codice del NEC-4 non e' pubblico (licenza LLNL) e, secondo Cebik, sui salti di diametro grandi non e' piu' affidabile della correzione. YA-NEC resta con il NEC-2 e la correzione di Leeson come EZNEC; la teoria R9 e' chiusa con questo confronto.


Il limite di correzione e' una variabile del motore. Cebik indica per EZNEC il 15 % dalla risonanza, ma le prove del collaudo (617 elementi a gradini calcolati da soli in EZNEC con la Stepped Diameter Correction, fasi 2K/2L) mostrano che EZNEC corregge anche molto piu' lontano, almeno fino a f/fs = 0,58: per gli elementi tra 0,58 e 0,85 la correzione porta lo scarto mediano dal 9,8 % al 3,0 % (410-14H dell'ARRL dal 30,8 % al 2,9 %, radiatori XR-5 dal 10,6 % all'1,9 %). **Scelta di calcolo:** sotto la risonanza la correzione si applica sempre, a qualunque distanza; sopra la risonanza il limite e' il 45 % (fino a f/fs 1,45). Motivo: gli elementi sotto f/fs 0,55 non corretti scartano da EZNEC fino al 14,4 %, corretti al massimo il 2,8 %, e nessun elemento peggiora; sopra la risonanza non ci sono dati (nessun elemento oltre f/fs 1,05) e l'XR-5 intera a 24,9 e 28,5 MHz, con i radiatori dei 20 m non corretti, coincide con EZNEC. La differenza sistematica sulla resistenza negli elementi ARRL e Hy-Gain con molti tubi si spiega con la segmentazione (vedi sopra, "Segmentazione dell'elemento sostituito"). **Taratura sotto la risonanza.** Tra f/fs 0,55 e 0,9 EZNEC corregge un po' diversamente dal libro di Leeson. Con s = 1 - f/fs (nulla alla risonanza, crescente sotto) YA-NEC corregge la lunghezza equivalente: k' = 1 - (1 - k)(1 + a s); il diametro si potrebbe correggere con D' = D exp(g s). La fase 2L bis del collaudo (`YA-NEC.exe --tara-gradini`) cerca a e g sui valori di EZNEC e scrive la proposta in taratura_gradini.txt. **Scelta di calcolo (IC8TEM):** a = 0,05 sulla lunghezza, nessuna correzione del diametro (g = 0). Sui 508 elementi sotto la risonanza lo scarto medio da EZNEC scende da 0,41 a 0,37 %, la mediana da 0,36 a 0,29 % e gli elementi oltre lo 0,5 % da 118 a 67 (oltre il 2 %: da 6 a 9). Con anche il diametro (a = g = 0,10) 30 elementi peggioravano di oltre 0,3 punti. La 520-40H a 14,0 MHz va piu' vicina a EZNEC (0,14 % invece di 0,26 %), l'XR-5 a 14,175 MHz un po' meno (0,66 % invece di 0,59 %).

Il NEC-2 con diametri a gradini fa risuonare l'elemento piu' in basso della realta'. YA-NEC usa l'algoritmo del libro di D. Leeson W6NL (Physical Design of Yagi Antennas, ARRL 1992, sez. 8.4): per ogni elemento si calcolano il logaritmo del raggio medio pesato sulle lunghezze, le somme AN, AM, AN', AM' sulle sezioni, poi Ka = 120 (ln 2l - ln ae - 1), Xo, Zo, il rapporto r = 1 - 2 (Xo - Xo') / (pi Zo), la lunghezza equivalente l' = l / r e il diametro equivalente d' = 4 l' e^-(Ka'/120 + 1). Prova: esempio del libro 95,70" / 0,594"; riflettore 20 m della XR-5 5,3234 m e 18,58 mm contro EZNEC 5,3243 m e 18,60 mm. Nel confronto con EZNEC su 916 modelli l'algoritmo del libro sta entro il 2 % in 51 casi contro 37 di un fattore fisso 1,075 sul raggio medio (errore mediano 3,0 % contro 4,2 %): EZNEC usa proprio questo algoritmo.

### 4.4 Casi di prova

Yagi 520-40H dell'ARRL (5 el. 20 m a 70 ft) con e senza correzione, dalla Tab. 7 della 21ª ed. [C13] e dalla Tab. 8.6 della 24ª [C12]. Senza correzione lo scarto mediano da EZNEC delle Yagi a gradini e' del 52,8 %, con la correzione lo 0,32 % sugli elementi.

## 5. Calcolatori: Yagi e quad

### 5.1 Yagi

| Calcolo | Formula o metodo | Fonte |
|---|---|---|
| Yagi lunga DL6WU | tabella delle lunghezze e delle spaziature di DL6WU, correzione del boom DL6WU/G3SEK | [C11], [C16] cap. 24 Tab. 24.1; [C62] |
| Yagi corte | tabelle NBS | Viezbicke [C43] |
| Yagi a 6 elementi ottimizzate | Cheng e Chen | [C44] in [C14] Tab. 10.1-10.4 |
| Yagi a 2 elementi | spaziatura dei parassiti | ARRL 19ª e 24ª [C15], [C12] |
| guadagno atteso | confronto con WA2PHW e con la Tab. 29-2 dell'AEH | [C40] |

### 5.2 Quad, Moxon e hexbeam

| Calcolo | Formula o metodo | Fonte |
|---|---|---|
| Moxon | regressioni di MOXGEN | Cebik [C33]; casi DK7ZB [C80] |
| quad e delta | perimetro 1005/f piedi | ARRL [C12] |
| hexbeam | geometria a W e a U di G3TXQ | G3TXQ, M0OXO (nel manuale d'uso) |

### 5.3 Casi di prova

Moxon a filo di DK7ZB per i 6, 10, 12, 15, 17, 20 e 30 m (filo nudo da 1 mm, spazio libero): YA-NEC come nec2c. Delta loop a 14,2 MHz con vertice a 37 gradi: 49,2 - j127 ohm.

## 6. Calcolatori: dipoli, verticali, loop

| Calcolo | Formula | Fonte |
|---|---|---|
| dipolo corto e monopolo | Rrad = 20 π² (l/λ)² (dipolo corto); 40 π² (h/λ)² per il monopolo alto h | Kraus, Balanis [C19], [C14] |
| verticale corta | Z0 = 60 · (ln(h/r) - 1)   X = Z0 · cot(β h) | Smith e Johnson [C52] |
| loop piccolo | Rrad = 31 171 · (N · A / λ²)² ohm (N spire) | Kraus [C19] |
| fascia larga w | raggio equivalente w/4 | Butler [C59] |
| elica in modo assiale | Z = 140 C/λ; D = 15 C² N S / λ³ (sovrastima: Emerson) | Kraus [C19], Emerson [C61] |
| OCF, Windom, trappolati | punto di alimentazione e lunghezze dalle fonti originali | [C63] |
| radiali | resistenza di terra dalla Tab. 9-1 di ON4UN (da Brown-Lewis-Epstein) | [C18], [C41], [C42] |

Casi di prova: ground plane a 4 radiali e monopolo su terreno perfetto come nec2c [C1]; verticale λ/4 con 16 radiali: Tab. 9-1 circa 14,9 ohm di terra (efficienza 71 %), Sevick circa 9 ohm (80 %) [[Calcolo: 3.5 Radiali a terra: confronto delle fonti]].

## 7. Ricezione, RDF e DMF

### 7.1 Definizioni

> FORMULA: RDF = G(direzione voluta) - Gmedio        Gmedio = Pemisfero superiore / 4π (con il terreno), media sulla sfera (spazio libero)

L'RDF non dipende dalle perdite: e' la cifra giusta per confrontare antenne riceventi poco efficienti [C82]. Il DMF di ON4UN [C18] (cap. 7) confronta il guadagno avanti con la media dell'emisfero posteriore; YA-NEC usa la media sul settore avanti al posto del solo guadagno a 20 gradi (fumetto giallo nella scheda).

> FORMULA: Beverage: L = λ / (4 · (1/VF - cosθ))        Z0 = 138 · log(4h/d)

### 7.2 Fonti, somiglianze e differenze

| Fonte | Somiglianze | Differenze |
|---|---|---|
| W8JI, W8WWV [C82] | stessa definizione dell'RDF, media sull'emisfero | non vale se rumore e segnale hanno polarizzazione diversa o con il rumore d'onda di terra [C12] 22.1.1 |
| ON4UN [C18] | DMF, Beverage, loop, four-square | DMF con il settore avanti (vedi sopra) |
| IV3PRK [C48], Hi-Z [C86] | valori di RDF di confronto | misure e modelli loro; YA-NEC calcola dal NEC del modello |
| K9AY [C46] | il NEC sbaglia le antenne collegate direttamente a un terreno con perdite | K9AY usa il modello raddoppiato con l'immagine e una resistenza di terra di 100-150 ohm; YA-NEC usa i radiali rialzati e un avviso |

### 7.3 Casi di prova

Dipolo corto: RDF 1,76 dB (teorico). Verticale λ/4: tra 4,6 e 5,4 dB. Beverage 175 m: RDF 10,55 dB (IV3PRK 10,81 [C48]).

## 8. Rumore e G/T

### 8.1 Temperatura d'antenna

> FORMULA: Tant = ∫∫ G(θ, φ) · T(θ, φ) dΩ / ∫∫ G(θ, φ) dΩ        G/T = G - 10 · log(Tant + Trx)

Il cielo e il terreno hanno temperature diverse; il diagramma pesa le due parti. Metodo di TANT [C7] (con la correzione KF2YN sul guadagno medio) e delle tabelle di VE7BQH [C50]. Con il cavo o il divisore tra antenna e ricevitore (Balanis 2.18 [C14]):

> FORMULA: Ta,rx = Tant · e^(-2αl) + Tfisica · (1 - e^(-2αl))

Temperature di partenza: cielo 200 K e terreno 1000 K (vecchio riferimento di TANT), 290 K e i valori nuovi di VE7BQH (1600 / 5400 / 14 550 K a seconda della banda) [C50]; a 432 MHz 20 K / 350 K (DG7YBN [C81]). Rumore esterno in HF: ITU-R P.372 [C70].

### 8.2 Fonti, somiglianze e differenze

| Fonte | Somiglianze | Differenze |
|---|---|---|
| TANT, YT1NT e VE3EA [C7] | stessa media pesata; a 0 gradi Tpattern = (Tcielo + Tterreno)/2 per diagrammi simmetrici | TANT legge un diagramma da file; YA-NEC usa quello calcolato |
| VE7BQH [C50] | G/T a 30 gradi, stack 2 x 2 alla distanza DL6WU | VE7BQH usa il cielo uniforme |
| Milligan [C23] | definizione di G/T | - |
| YU7EF, EF0213 | caso di prova | ha mostrato che la temperatura deve comprendere le perdite dei conduttori |

### 8.3 Rumore dal cielo vero

**Come si calcola.**

| Passo | Cosa fa | Fonte |
|---|---|---|
| Mappa | HEALPix RING con nside 128 (196 608 pixel, circa 0,46 gradi), valori in kelvin; tra i pixel si interpola sui quattro vicini come fa healpy (get_interp_weights) | G8ONH, Haslam, Remazeilles |
| Frequenza | si sceglie la mappa piu' vicina (144 o 432 MHz) e si scala con T = T_mappa (f_mappa / f)^2,56 | G8ONH |
| Tempo siderale | tempo siderale medio locale dalla data UTC con l'espressione dell'USNO | G. H. Kaplan, USNO Circular 179 (2005) |
| Coordinate | ogni direzione del diagramma (azimut, elevazione) diventa angolo orario e declinazione, poi ascensione retta | trigonometria sferica classica |
| Luna | posizione della Luna con la teoria ridotta di Meeus, per puntarla | J. Meeus, Astronomical Algorithms, cap. 47 |
| Sotto l'orizzonte | ogni direzione sotto l'orizzonte vede il terreno, 290 K come in skynoise (modificabile) | G8ONH |
| T antenna | media della temperatura pesata sul diagramma di guadagno su tutta la sfera: T = somma(G T dOmega) / somma(G dOmega) | TANT, skynoise |

| Fonte | Somiglianze | Differenze |
|---|---|---|
| skynoise 0.2.2, G8ONH | stessa mappa, stessa interpolazione, stesso tempo siderale, stessa scala in frequenza e 290 K sotto l'orizzonte | skynoise usa un diagramma di Yagi da file o un modello gaussiano; qui il diagramma e' quello calcolato dal NEC del modello aperto o dello stack |
| VE7BQH, tabelle G/T | stesso G/T a 30 gradi, stack di 4 alla distanza DL6WU | VE7BQH usa il cielo uniforme (200 K): e' il primo dei due G/T della pagina |
| TANT (YT1NT, VE3EA; manuale DG7YBN) | temperatura dell'antenna dal diagramma pesato su cielo e terreno | TANT usa temperature uniformi e una legge di elevazione del cielo; non conosce l'ora ne' il QTH |
| KF2YN | correzione sul rumore di terreno vicino all'orizzonte | resta nella pagina Rumore e G/T |

Fonti: skynoise di G8ONH, Haslam, Remazeilles [C56]; Kaplan (USNO Circular 179); Meeus [C30].

Prove (autotest): mappa 144 MHz, punto freddo (RA 142, Dec 30) 165,7 K, centro galattico circa 22 000 K; tempo siderale all'epoca J2000 = 18,697 h come l'USNO.

## 9. Stack VHF/UHF

### 9.1 Distanza DL6WU

> FORMULA: D = λ / (2 · sin(B/2))

B e' l'apertura a -3 dB nel piano dello stack, presa dal diagramma NEC dell'antenna singola in spazio libero. Ipotesi: i lobi delle antenne si toccano a -3 dB. Limiti: nel piano E vale per boom oltre circa 0,7 λ, nel piano H oltre 2 λ [C12]. Fonte: DL6WU [C11]; anche Rothammel cap. 24 [C16] e DG7YBN [C81].

### 9.2 Matrice di accoppiamento

Si alimenta una porta alla volta con 1 V (le altre con una sorgente quasi nulla, cioe' in corto) e si leggono le correnti di tutte le porte: e' una colonna della matrice delle ammettenze Y. Poi:

> FORMULA: Z = Y^-1        S = (Z - Z0·I) · (Z + Z0·I)^-1

E' la scheda CP del NEC-2 [C3] e il metodo di nec2-array di YO3IIU [C27], riscritto in C# senza righe copiate. Sulla diagonale c'e' il disadattamento di ogni antenna con le altre chiuse su Z0, fuori diagonale la potenza che passa da una porta all'altra.

### 9.3 ROS attivo e guadagno realizzato

**ROS attivo e "da non scegliere"** (dalla 1.4, richiesta di IC8TEM del 18/11/2026). Con le antenne alimentate tutte insieme, in fase e con la stessa ampiezza, la potenza riflessa su ogni porta non e' solo il suo S11: e' la somma della riga della matrice, Γatt,i = Si1 + Si2 + ... + SiN. Il **ROS attivo** e' (1 + |Γatt|)/(1 - |Γatt|) della porta peggiore; il **G realizzato** toglie al guadagno la perdita per riflessione -10 log(1 - |Γatt|²). Per l'antenna singola si usa la sua impedenza d'ingresso. Le distanze con ROS attivo sopra la soglia della casella **ROS attivo massimo** (di partenza 1,5) hanno il commento **"ROS attivo alto: da non scegliere"** e restano fuori dalla scelta proposta (se lo fossero tutte, si sceglie fra tutte); la riga con il ROS attivo piu' basso e' segnata **"riflessi minimi"**.

Perche' DL6WU parla di aree di cattura che si toccano. L'area efficace di un'antenna e' Ae = Gλ²/4π (per la 10 el. BVO a 144,3 MHz circa 12,8 m²). La "piastrella" di DL6WU, Dv x Dh, vale circa λ²/(Bv·Bh) con le larghezze a -3 dB in radianti: per la stessa antenna circa 15,3 m², quasi la stessa area. Alla distanza DL6WU le aree di cattura sono quindi affiancate: lo stack raccoglie quasi tutta l'energia senza che due antenne si contendano lo stesso fronte d'onda. Piu' vicino le aree si sovrappongono (il guadagno cresce meno di +3 dB per raddoppio e l'accoppiamento sale, quindi il ROS attivo); piu' lontano compaiono i lobi di griglia, a sinθ = λ/d. La regola di DL6WU dice quindi soprattutto dove non stare; il ROS attivo segna in tabella il lato delle distanze troppo corte.

> FORMULA: Γatt,i = Σj Sij        ROS = (1 + |Γatt|) / (1 - |Γatt|)        G realizzato = G + 10 · log(1 - |Γatt|²)

### 9.4 F/R medio

F/R medio = guadagno avanti meno la media della potenza sull'emisfero posteriore (azimut 90-270 gradi, elevazioni sopra 0). La variazione rispetto all'antenna singola e' il ΔF/R della tabella.

### 9.5 Casi di prova

Esempio (10 el. BVO di IC8TEM a 144,3 MHz, stack 2 x 2, nec2c): singola 15,72 dBi, F/R 26,4 dB; il F/R migliora di piu' a 0,90 x DL6WU (33,9 dB, +7,4 dB), con un secondo minimo a 1,40 (+7,1 dB); il guadagno e' gia' a +6 dB da 1,0 in poi, massimo 21,90 dBi a 1,35; lobi laterali piu' bassi a 0,85 (-14,4 dB).

Prova (autotest): due Yagi a 6 elementi del MathWorks Antenna Toolbox (misure ottimizzate da MathWorks) una sopra l'altra alla distanza DL6WU: abs(S21) circa -27,6 dB e 12,08 dBi, come nec2c.

### 9.6 Fonti: somiglianze e differenze

| Fonte | Somiglianze | Differenze |
|---|---|---|
| DL6WU [C11] | distanza dalle aperture a -3 dB (fattore 1 nella tabella) | una sola distanza; qui una serie e un criterio |
| AEH Tab. 29-2 [C40] | spaziature tipiche 1,0-2,0 λ per 4-18 elementi | valori tipici, non calcolati sull'antenna |
| nec2-array, YO3IIU [C27] | matrice di accoppiamento con una porta alimentata alla volta | YO3IIU lo usa in ricezione; qui anche ROS attivo e G realizzato |
| VE7BQH [C50] | G/T come criterio per l'EME | - |
| MMANA-GAL [C9] | stack calcolato dal modello | MMANA-GAL non da' la matrice S |

## 10. Stack HF sopra il terreno

In HF la formula di DL6WU non vale: le antenne sono sopra il terreno e il diagramma in elevazione e' fatto dalle riflessioni sul suolo. Per ogni distanza si calcola con il NEC il diagramma in elevazione con tutte le antenne in fase (BIP) nell'azimut del lobo; la distanza proposta e' quella con la media del guadagno piu' alta tra gli angoli di partenza voluti. Per quella si calcolano anche la superiore da sola, l'inferiore da sola e, con due antenne, la controfase (BOP).

| Fonte | Somiglianze | Differenze |
|---|---|---|
| ARRL Antenna Book, stack HF [C12] | BIP, BOP, angoli di partenza verso le zone da collegare | YA-NEC calcola gli accoppiamenti con il NEC |
| HFTA, N6BV [C8] | antenne separate, stesso confronto per angoli | HFTA non tiene gli accoppiamenti e chiede spaziature oltre 0,5 λ |
| ON4UN [C18], Cebik [C33] | stack sulle bande basse | - |

## 11. Tolleranze

Monte Carlo: prove con errori casuali su lunghezza, posizione sul boom e diametro degli elementi (distribuzione uniforme, o gaussiana con tolleranza = 2σ); seme fisso per risultati ripetibili. Sensibilita': un elemento alla volta, calo del F/B per 1 mm di errore. Tolleranza ammessa della lista di taglio:

> FORMULA: errore ammesso = errore che da solo fa calare il F/B di (calo massimo / sqrt(numero degli elementi))

con errori indipendenti, e che non porta il ROS sopra l'obiettivo.

| Fonte | Somiglianze | Differenze |
|---|---|---|
| AEH 29.4 [C40] | analisi di sensibilita' per assegnare le tolleranze strette solo ai parametri critici | - |
| ARRL 24ª, 11.10 [C12] | in HF fino a 30 MHz costruire a 3 mm da' prestazioni coerenti con il calcolo | YA-NEC lo calcola per la Yagi aperta |

Caso di prova: con tolleranze nulle ogni prova coincide con il nominale.

## 12. Vento, bilanciamento, palo

### 12.1 Vento

| Grandezza | Formula | Fonte |
|---|---|---|
| velocita' di riferimento | vb = vb,0 · ca · cr | NTC 2018 3.3.1-3.3.2 [C71] |
| periodo di ritorno | cr = 0,75 · sqrt(1 - 0,2 · ln(-ln(1 - 1/Tr))) | NTC 2018 3.3.2 [C71] |
| pressione cinetica | qr = ½ · ρ · vb²  (ρ = 1,25 kg/m³) | NTC 2018 3.3.6 [C71] |
| pressione | q = qr · ce · ct | NTC 2018 3.3.4 [C71] |
| tubi tondi | cf0 dal numero di Reynolds (sotto 2·10^5: 1,2) | EN 1991-1-4 §7.9.2; CNR-DT 207 App. G [C71] |
| sezioni quadre | cf0 = 2,1 | EN 1991-1-4 §7.6 [C71] |
| snellezza | ψλ | EN 1991-1-4 §7.13 [C71] |
| direzione del vento | conta la componente perpendicolare all'asse del tubo, al quadrato (flusso trasversale) | K5IU; ARRL 26 app. A [C12] |

Caso di prova: un elemento con il vento a 0, 45 e 90 gradi dal boom da' 1200, 600 e 0 N.

| Fonte | Somiglianze | Differenze |
|---|---|---|
| ARRL 24ª, cap. 26 app. A e B [C12] | area proiettata, flusso trasversale, sollecitazione del palo M·c/I | P = 0,00256 V² senza quota, esposizione e raffica: YA-NEC usa la NTC |
| AEH 29.2 [C40] | fatica dell'alluminio, risonanza, turbolenza vicino agli edifici | nella scheda come avviso |

### 12.2 Bilanciamento e boom

Baricentro del peso, centro di spinta del vento e loro compromesso; trave appoggiata con carico uniforme (momento qL²/8, freccia 5qL⁴/384EI) da Roark [C25]; materiali EN 755-2, EN 1999-1-1, EN 10025-2, EN 13706-3 [C72]. ARRL 11.9-11.11 [C12]: compensatore di coppia in PVC dietro l'ultimo direttore.

## 13. Divisori, cavo e balun

### 13.1 Divisori

| Formula | Uso | Fonte |
|---|---|---|
| Zcarico = Zantenna / N; Z0 = sqrt(Zin · Zcarico) | trasformatore λ/4 | [C12], AEH 52.3 [C40] |
| Z0 = 138 · log(1,0787 · D/d) / sqrt(εr) | tubo quadro con conduttore tondo | ARRL, DL6WU [C12], [C11] |
| Z0 = 138 · log(D/d) / sqrt(εr) | tubo tondo (coassiale) | [C12] |
| λ/4 = 299 792 / f / 4 · VF (mm), meno l'accorciamento dei connettori | lunghezza da tagliare | - |
| rami λ/4 da Zin · sqrt(N), resistenze a stella | Wilkinson | [C54] |
| cavi di fase: n · λ/2 · VF | l'impedenza attiva si ripete al divisore | DG7YBN [C81] |

Caso di prova: 4 vie da 50 ohm, Z0 25 ohm, conduttore 14,2 mm nel tubo quadro da 20 mm, λ/4 di 519,7 mm a 144,2 MHz.

### 13.2 Cavo e balun

La calza del cavo diventa un filo NEC attaccato a un lato del punto di alimentazione; il balun e' un'impedenza di modo comune (LD 4, R + jX) sul primo tratto della calza.

| Fonte | Somiglianze | Differenze |
|---|---|---|
| AEH 52.7, Fig. 52-23 [C40] | balun di corrente e di tensione; il manicotto λ/4 singolo non e' bilanciato fuori risonanza | - |
| K9YC, G3TXQ [C83]; ARRL 24.7 Tab. 24.12 [C12] | choke: almeno 1000 ohm in aria, 5000 ohm in ferrite in trasmissione, 500-1000 ohm in ricezione | valori tipici nella scheda; quelli veri vanno misurati |
| Guanella, Ruthroff, Sevick [C53], [C24] | balun a linea di trasmissione | - |
| Wheeler, Medhurst [C51] | L e capacita' propria del choke a spire di coax | - |

Caso di prova: il manicotto a f0 supera 1000 ohm e riduce la corrente sulla calza di un dipolo a 144 MHz di oltre tre volte.

## 14. Carichi HF, linea aperta

### 14.1 Bobine e trappole

> FORMULA: L (µH) = r² · n² / (9r + 10l)   (Wheeler, r e l in pollici)        Cpropria = H · D   (Medhurst)

Trappola coassiale W8NX a bassa impedenza: la trappola vista come Z/4 (L/4, C x 4) [C12]. Bobina di carico: X = 1122 ohm con Q 300 da' 3,74 ohm di perdita (ON4UN 8-11/8-12 [C18]). Efficienza delle verticali corte: Severns [C52].

Caso di prova: la trappola W8NX dell'ARRL Antenna Book (8,75 spire di RG-59 su PVC da 60,3 mm, 4 µH, 123 pF, 7,15 MHz) da' 3,97 µH, 123 pF, 7,19 MHz.

### 14.2 Linea con perdite e accordatore

Perdita della linea con il ROS come TLW dell'ARRL [C12]; accordatore con Qn = sqrt(Rmax/Rmin - 1), due sezioni per T e pi greco.

| Fonte | Somiglianze | Differenze |
|---|---|---|
| ARRL 24ª, 24.2.3 Fig. 24.4 [C12] | rete L e pi greco a Q minimo: perdita circa 1,8 % a 1,8 MHz su 5 ohm | il T reale con C2 = 500 pF perde il 22,4 %: la stima di YA-NEC sta sul caso a Q minimo |
| ON4UN cap. 8 [C18] | dipolo da 160 m a meta' misura: Z = 11,5 - j1122 ohm, feeder aperto con ROS 280:1 e perdita totale sotto 2 dB | - |
| NT0Z, WB8IMY | linea a scala con l'accordatore | NT0Z moltiplica la perdita adattata per un fattore del ROS; YA-NEC usa la formula esatta della linea |

## 15. Parabole e illuminatori

### 15.1 Parabola: come si calcola l'efficienza

Il guadagno di una parabola e' quello di un'apertura circolare uniforme moltiplicato per l'efficienza totale:

G = (pi D / lambda)^2 x eta_totale, in dBi: G = 10 log10[(pi D / lambda)^2] + 10 log10(eta_totale)

YA-NEC calcola eta_totale come prodotto di sei fattori, ognuno nella pagina Riepilogo con la sua perdita in dB:

eta_totale = eta_apertura x eta_blocco x eta_superficie x eta_rete x eta_bordo x eta_varie

**1. Apertura (illuminazione del feed): eta_apertura = eta_taper x eta_fase x eta_spillover x eta_pol.** Il feed illumina il disco con il suo diagramma; sulla superficie si aggiunge l'attenuazione spaziale (1 + cos theta)/2, perche' il bordo e' piu' lontano dal fuoco del centro (distanza 2F/(1 + cos theta)). Senza diagramma del feed, YA-NEC usa il feed cos^q(theta) e ricava q dal taper al bordo scritto (feed + attenuazione spaziale, 10 dB come consiglia W1GHZ): q = ln(10^(-taper/20) / s) / ln(cos psi0), con s = (1 + cos psi0)/2 e psi0 = 2 atan(1 / (4 f/D)) il semiangolo del bordo visto dal fuoco.

| Fattore | Formula | Cosa misura |
|---|---|---|
| taper (illuminazione) | eta_taper = (integrale di E rho drho)^2 / [(a^2/2) x integrale di E^2 rho drho], a = raggio del disco | quanto l'illuminazione si allontana da quella uniforme: bordo troppo scuro = disco usato in parte |
| spillover | feed cos^q: 1 - cos^(2q+1)(psi0); feed dal NEC o tabellato: potenza entro psi0 / potenza totale | la potenza del feed che passa oltre il bordo e non arriva al disco (e vede il terreno caldo) |
| fase | modulo dell'integrale del campo complesso al quadrato / (integrale del modulo)^2 | il feed non ha un centro di fase unico: solo con la fase dal NEC o da file W1GHZ, altrimenti 1 |
| polarizzazione | potenza del termine medio (E + H)/2 entro psi0 / potenza entro psi0 (Kildal 1985) | asimmetria tra i piani E e H del feed (polarizzazione incrociata): solo con i due piani, altrimenti 1 |

Taper e spillover tirano in direzioni opposte: un bordo poco illuminato riduce lo spillover ma spreca il disco. Il massimo di eta_taper x eta_spillover si ha intorno a 10-12 dB di taper totale al bordo, circa 82 %: e' la figura della pagina Feed (efficienza in funzione di f/D).

**2. Blocco: eta_blocco = |1 - b_c - b_p|^2.** Il feed e i puntoni fanno ombra sul disco. Il blocco non si conta come area tolta, ma come campo tolto pesato con l'illuminazione: b_c = campo sotto l'ombra del feed (raggio pari a meta' del suo diametro) diviso il campo totale; b_p = N puntoni x larghezza / (2 pi) x campo lungo l'ombra dei puntoni, dal bordo del feed al punto d'attacco sulle centine, diviso il campo totale. Il centro e' la zona piu' illuminata, per questo un feed piccolo pesa piu' della sua area (Galt Telescope; Lamb e Olver; Ruze per i puntoni). Dalla 1.0.14 b_p comprende anche l'ombra sull'onda sferica (Baars 2007, eq. 4.36c-f): l'onda che va dal fuoco al disco incontra i puntoni anche oltre il punto d'attacco, fino al bordo; per ogni raggio si trova dove taglia il puntone e l'ombra sul disco vale larghezza x (distanza fuoco-disco / distanza fuoco-puntone). Con i puntoni attaccati al 65 % del raggio questa parte e' la maggiore (esempio sotto: 1,45 % del campo contro 1,05 % dell'onda piana). La pagina Riepilogo la indica a parte.

**3. Superficie (Ruze): eta_superficie = exp[-(4 pi sigma / lambda)^2].** sigma e' lo scostamento RMS della superficie dal paraboloide (centine, rete, montaggio). lambda/40 costa 0,4 dB, lambda/20 costa 1,7 dB, lambda/16 circa 2,7 dB. Con la casella "Aggiungi la sfaccettatura della rete piana" si somma (in quadratura) l'errore della rete tesa piatta tra due centine: freccia della corda k2 s^2/8 (s = corda tra le centine, k2 = curvatura del paraboloide in quel punto), RMS circa 0,298 volte la freccia, media pesata con l'illuminazione. Dalla 1.0.14 conta solo la componente assiale dell'errore, eps_assiale = eps_normale / sqrt(1 + (r/2F)^2) (Baars, eq. 4.43): verso il bordo la superficie e' inclinata e lo stesso errore normale sposta meno la fase. La pagina Meccanica mostra tutti e due i valori.

**4. Rete (Mumford): eta_rete = 1 - T.** Una parte dell'onda passa attraverso i fori della rete: T = 1 / (1 + x^2), x = lambda / [2 s ln(s / (pi d))], con s = passo (foro + filo) e d = diametro del filo. La potenza trasmessa va dietro il disco e porta rumore del terreno (T x 290 K, pagina Riepilogo). Con "Superficie piena" il fattore vale 1.

**5. Bordo a poligono: eta_bordo = N sin(2 pi / N) / (2 pi).** Solo con la sfaccettatura accesa: con la rete piana tra N centine il bordo e' un poligono, che ha meno area del cerchio (16 centine: 0,975, -0,11 dB).

**6. Perdite varie: eta_varie = 10^(-L/10).** Una perdita fissa scritta a mano (di partenza 0,7 dB): ROS del feed, linea fino al punto di misura, polarizzazione incrociata, piccoli errori di fase e di montaggio. W1GHZ osserva che i dischi reali rendono almeno il 15 % meno del calcolo: questa casella serve a tenerne conto.

**Esempio con i valori di partenza del calcolatore** (disco da 3 m, 1296,1 MHz, f/D 0,42, taper totale 10 dB, feed cos^q, ombra del feed 0,20 m, 3 puntoni da 20 mm attaccati al 65 % del raggio, rete 6 mm con filo 0,8 mm, errore RMS 3 mm, perdite varie 0,7 dB):

| Passo | Valore | Fattore | Perdita |
|---|---|---|---|
| lambda, semiangolo del bordo psi0 | 231,3 mm, 61,5 gradi | | |
| attenuazione spaziale al bordo, taper del solo feed, q | 2,63 dB, 7,37 dB, q = 1,14 | | |
| taper (illuminazione) | | 90,3 % | -0,44 dB |
| spillover | | 91,3 % | -0,40 dB |
| apertura (taper x spillover; fase e polarizzazione = 1 con il feed cos^q) | | 82,4 % | -0,84 dB |
| blocco (feed 0,75 % del campo, puntoni 1,05 % onda piana + 1,45 % onda sferica) | | 93,6 % | -0,29 dB |
| superficie, 3 mm = lambda/77 (Ruze) | | 97,4 % | -0,12 dB |
| rete 6 mm = lambda/38 (Mumford, T = 0,34 %) | | 99,7 % | -0,01 dB |
| perdite varie | | 85,1 % | -0,70 dB |
| **totale** | | **63,8 %** | **-1,95 dB** |
| guadagno: apertura uniforme 32,20 dBi - 1,95 dB | **30,25 dBi** | | |

Si vede che a 1296 MHz rete e superficie quasi non contano: pesano illuminazione e perdite varie. Alle bande alte i numeri cambiano: con la stessa rete e lo stesso errore RMS, a 10368 MHz (lambda 28,9 mm) la superficie da 3 mm (lambda/10) costa circa 7 dB e la rete 6 mm diventa trasparente per una parte importante della potenza; la pagina Bande ed EME fa lo stesso conto per le quattro bande.

Con il diagramma del feed dal modello NEC (pulsante "Feed dal modello NEC") o da un file W1GHZ, taper e spillover si calcolano sul diagramma vero, anche diverso nei piani E e H; con la fase si aggiungono eta_fase ed eta_pol, e il centro di fase si cerca dove l'efficienza e' massima (criterio di W1GHZ).

**Controlli di letteratura (dalla 1.0.14).** Accanto al calcolo, la scheda mostra i valori delle formule chiuse di Baars e Balanis, per vedere quanto il disco si scosta dai casi ideali:

| Dove | Cosa | Formula e fonte | Somiglianze e differenze con il calcolo |
|---|---|---|---|
| pagina Feed, nota | free-space taper | Tf = 20 log[1 + (D/4f)^2] (Baars 4.3): 2,63 dB a f/D 0,42, 4,58 dB a f/D 0,3 | e' la stessa attenuazione spaziale gia' calcolata; la nota da' anche il taper totale al bordo |
| pagina Feed, grafico 4 | guadagno contro fuori fuoco | eta = [sin(beta/2) / (beta/2)]^2, beta = 2 pi delta / lambda x (1 - cos psi0) (Baars 4.22) | Baars vale per l'apertura uniforme, il calcolo usa il feed vero: la curva tratteggiata e' un controllo, non la sostituisce. Il Riepilogo da' anche la perdita per l'apertura quadratica (Baars 4.23) |
| Riepilogo, sotto la fase | limite inferiore dell'efficienza di fase | (1 - m^2/2)^2, m = scarto massimo di fase sull'apertura (Balanis cap. 15) | e' un minimo garantito: il valore calcolato sta sempre sopra. Serve la fase del feed |
| Riepilogo | guadagno massimo con questa superficie | lambda_max = 4 pi sigma, G_max = 20 log10(D/sigma) - 16,38 dB + perdite non di superficie (Balanis 15-67...71) | oltre quella frequenza il guadagno cala anche se la frequenza sale; c'e' un avviso quando la frequenza scelta e' gia' oltre |
| pagina Lobi, tabella | apertura a -3 dB e primo lobo | HPBW = b lambda/D, b = 1,269 - 0,566 t + 0,534 t^2 - 0,208 t^3; primo lobo -24,6 + 7,37 t dB; t = campo al bordo (feed + spazio) (Baars 4.13 e 4.14a) | distribuzione quadratica su piedistallo, senza blocco: il primo lobo calcolato con il blocco e' di solito piu' alto |
| pagina Lobi, grafico | fascio d'errore | larghezza 1,06 lambda/c, potenza circa 1,1 (e^(s^2) - 1), s = 4 pi sigma / lambda, c = distanza tra le centine a 2/3 del raggio (Baars 4.44-4.46) | e' una stima (curva tratteggiata arancio), non un calcolo: dice dove va l'energia che Ruze toglie al lobo principale |

L'autotest ha la sezione 10 con i casi della letteratura: feed cos^2...cos^8 a f/D 0,3, 0,42 e 0,6 contro l'integrale di Balanis (15-55), fatto con un integrale indipendente, e contro il motore; forma chiusa per cos^2 e massimo della curva (0,829); distribuzione quadratica a -12 dB (0,893, HPBW 66,3 gradi lambda/D); gaussiana a -12 dB (0,866); Tf 4,58 dB a f/D 0,3; fuori fuoco con beta = 1 (0,919); Ruze con sigma = lambda/20 (-1,71 dB). Le misure di PA0PLY a 10 GHz (disco da 3 m, f/D 0,3: guida aperta 13,5 dB contro choke 15,2 dB di rapporto sole/rumore; choke avvitata -0,6 dB; cappuccio dell'LNB -0,5 dB) sono riportate come riferimento: il diagramma di quei feed non ha una formula e va calcolato col NEC.

Fonti: Stutzman e Thiele, Antenna Theory and Design, eq. 9-233 (feed cos^q, spillover, efficienza d'illuminazione); P. Wade W1GHZ, Online Microwave Antenna Book, cap. 4 e 6 (taper ottimo, fase, efficienza reale); P.-S. Kildal, IEEE Trans. AP-33, 1985 (fattori dell'efficienza del feed); J. Ruze, Proc. IEEE 1966 (errori di superficie) e Microwave Journal 1968 (puntoni); W.W. Mumford, Proc. IRE 1961 (trasmissione della rete); Galt Telescope, arXiv 1607.06436, e Lamb e Olver (blocco pesato); J.W.M. Baars, The Paraboloidal Reflector Antenna in Radio Astronomy and Communication, Springer 2007, cap. 4 (controlli, ombra sferica dei puntoni, componente assiale, fascio d'errore); C.A. Balanis, Antenna Theory, cap. 15 (feed cos^n, limite di fase, guadagno massimo); PA0PLY (misure di feed a 10 GHz).

### 15.2 Altre fonti

Baars [C21], Ruze [C55], Kildal [C22], Stutzman e Thiele [C20], Wade W1GHZ [C32], Balanis cap. 12, 13 e 15 [C14], Idelchik e Hoerner per il vento sulla rete [C28].
