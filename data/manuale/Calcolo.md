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

### 1.6 Modelli di altri programmi: lettura e prova (dalla 1.5)

**AutoEZ e MultiNEC (.weq).** Il foglio Variables di AutoEZ è una griglia di blocchi di 22 righe (righe 2-23 del file). Il blocco 0 sono le lettere A, B, D, E, G, ..., V, X, Y, Z (mancano C, F, R e W: ricavato dalle formule di OCF Dipole e Fan Dipole di AC6LA). Dal blocco 1 i nomi seguono le colonne di Excel (AA dal blocco 1, AW dal blocco 2, BS dal blocco 3) e contengono anche le «celle calcolate». Nelle formule valgono le funzioni di testo usate da AutoEZ (IF(ISNUMBER(..)), SUBSTITUTE, VALUE per gli angoli scritti «45d»), WL (lunghezza d'onda nell'unità delle coordinate, MultiNEC) e TLParms (Zo e VF nominali; la scheda TL è senza perdite). La 13ª colonna delle tabelle di sorgenti, carichi e linee è la colonna «mark out»: "x" esclude la riga, =IF(condizione su Freq, "", "X") accende la sorgente solo dove la condizione è vera (a ogni frequenza si calcolano solo le sorgenti accese, le altre sono filo continuo, come in EZNEC). I fili virtuali di EZNEC (V1, V2...) diventano fili di 1 segmento (0,02 λ) sopra l'antenna con 1 MΩ di reattanza in serie: la porta resta aperta e il filo non porta corrente. Verifica con nec2c a 14 MHz: dipolo alimentato da 5 m di linea a 50 Ω da un filo virtuale, 30,033 − j14,724 Ω contro 30,032 − j14,723 Ω della formula della linea. La tabella dei trasformatori di EZNEC diventa un trasformatore ideale di YA-NEC con il rapporto delle Rel Z. Restano fuori le reti L scritte da Zmatch (formule nel foglio Calculate, che non è nel .weq) e gli stub con «Open/Short» nella prima colonna senza una linea sullo stesso segmento (avviso).

**.nec per gli altri programmi.** Il NEC-2 non ha le sorgenti di corrente di EZNEC né le sorgenti «mark out»: «Salva con nome» in .nec scrive le tensioni che YA-NEC ricava alla frequenza del modello (V = Z·I) e solo le sorgenti accese, con due righe CM che lo dicono. Il .yanec resta com'era.

**Prova di lettura (riga di comando «YA-NEC --converti <cartella> <uscita>»).** Ogni modello si legge, si calcola con YA-NEC, si scrive in .nec e si ricalcola con nec2c. Novembre 2026: 61 .weq di IC8TEM, 198 modelli degli archivi (AutoEZ, MultiNEC, MMANA, Modeling, mynec4pc) e 116 .yag (zipyagi, Hy-Gain). Entro lo 0,5 % da nec2c tranne: le Yagi con impedenza di pochi ohm (scarti di 0,03-0,07 Ω), i modelli con la correzione dei tubi a gradini (YA-NEC applica Leeson, nec2c no) e i modelli su terreno di Sommerfeld (1,35-2 %; con terreno perfetto, coefficiente di riflessione o spazio libero gli stessi modelli stanno entro lo 0,14 %, quindi la differenza è nel calcolo di Sommerfeld, da studiare).

### 1.7 Come e' stato modificato il motore (dalla 0.4 alla 1.5.9)

Il punto di partenza e' nec2c 1.3 di N. Kyriazis 5B4AZ [C1], tradotto in C# routine per routine (geometria, matrice, campi, campo lontano, SOMNEC). Ogni modifica e' stata confrontata con nec2c, con necpp [C2] e, dove serviva, con il listato Fortran del NEC-2 [C3]; l'autotest (1.4) e il collaudo dei modelli d'archivio la verificano a ogni compilazione. Le modifiche sono di tre tipi.

**A. Difetti di nec2c corretti come nel NEC-2 originale.** I risultati cambiano solo nei casi colpiti dal difetto.

| Routine | Difetto di nec2c | Correzione in YA-NEC | Dalla |
|---|---|---|---|
| zint (perdite del filo, LD 5) | mancano due «return»: vale sempre la terza formula, giusta solo con l'effetto pelle molto marcato; fili sottili o poco conduttivi perdono troppo poco | le tre formule come nel listato; costanti cc5 e CN dal listato ZINT (Part III, via hf-nec2c [C116]) | 0.4.2 |
| evlua (Sommerfeld) | se rmis > 4 rho / zph il cammino sotto i punti di diramazione e' saltato: integrale incompleto | il cammino del Fortran; sul mare, fili a 0,2-1 λ, fino al 5 % di impedenza | 0.4.38 |
| intrp (Sommerfeld) | in uno sweep tiene i coefficienti della frequenza precedente | coefficienti azzerati a ogni frequenza | 0.4.2 |
| rom2 (campo del terreno) | dimezzamenti del passo limitati dal numero di segmenti | limite 65536 come nel Fortran | 0.4.2 |
| qdsrc (sorgente EX 5) | mancava la caduta sul carico del segmento | aggiunta, per gli esempi del manuale NEC-2 Part III | 0.4.2 |
| SC dopo SP (superfici) | il NEC-2 le accetta, nec2c si ferma | YA-NEC non tratta ancora le superfici (lo dice con un errore chiaro) | - |

**B. Parti numeriche riscritte: stesso risultato, piu' veloce o piu' robusto.**

| Parte | Come in nec2c | Come in YA-NEC | Dalla |
|---|---|---|---|
| Soluzione [Z][I] = [V] | LU colonna per colonna | LU a blocchi con pivot parziale, istruzioni vettoriali per i prodotti complessi, in parallelo (idea di E. S. Harriman AE6TY, nec2c-ae6ty [C115]; LAPACK [C29]); la LU classica resta attivabile per i confronti | 0.4.20 |
| Riempimento della matrice | un processore | in parallelo, un blocco di segmenti per thread; vettori di appoggio di efld e rom2 creati una volta per thread (la Butternut da 1453 segmenti allocava 3,1 GB) | 0.4.2, 0.4.20 |
| Simmetrie GX e GR | stesso metodo | soluzione in forma di gruppo; i blocchi grandi si fattorizzano uno dopo l'altro | 0.4.2, 0.4.30 |
| Campo lontano | un processore | una riga di elevazione per processore, seni e coseni per somma degli angoli | 0.4.2 |
| Sweep in frequenza | un calcolo per frequenza | funzioni razionali (MBPE, Burke e Miller) con punti infittiti dove due stime non concordano [C57]; punti in parallelo entro un quarto della memoria | 0.4.2, 0.4.14 |
| Tabelle di Sommerfeld | ricalcolate a ogni modello | conservate per terreno e frequenza (fino a 64) | 0.4.20 |
| Linea TL lunga un multiplo di mezz'onda | ammettenza infinita | lunghezza spostata di una parte su un milione, come nec2c e 4nec2 (hrs_441.nec) | 0.4.8 |

**C. Aggiunte e scelte di YA-NEC, sempre dichiarate negli avvisi.**

| Aggiunta | Cosa fa | Dalla |
|---|---|---|
| Velocita' della luce | 299,792458 come EZNEC invece di 299,8 del NEC-2: lo scarto di fondo con EZNEC scende dallo 0,25 % allo 0,002 %, la loop magnetica MAGLOOP2 dal 4 allo 0,1 %; le prove con nec2c usano 299,8 | 0.4.30 |
| Correzione di Leeson | diametri a gradini come la Stepped Diameter Correction di EZNEC (capitolo 4) | 0.4 |
| Schermo di radiali (GN con NRADL) | anche nel campo lontano, come nec2c | 0.4.30 |
| Terreno «MiniNec» (GN 3 di 4nec2) | impedenza su terreno perfetto, diagramma con er e sigma: per leggere i file EZNEC e per la verticale da 43 piedi con i radiali interrati | 1.5, 1.5.9 |
| Carichi e sorgenti di 4nec2 | LD 6 (trappola LC), LD 7 (filo isolato, metodo di Cebik), EX 6 (sorgenti di corrente) | 0.4, 1.5.6 |
| Giunzioni come il NEC-4 (opzione, spenta) | carica alle giunzioni da un piccolo calcolo dei momenti e corrente sulla superficie del segmento sorgente, dal manuale di teoria del NEC-4 (Burke 1992) senza codice NEC-4 | 1.2.0 |
| Efficienza delle strutture ad alto Q | se il bilancio Re(V I*) da' efficienza negativa (loop magnetica) si usa Prad / (Prad + perdite), con un avviso | 0.4.30 |
| Controlli dei dati | quote minime dei fili sul terreno, giunzioni affollate, fili vicini, segmentazione e AGT (capitolo 2) | 0.4.18 |
| Frequenza senza scheda FR | 299,7925 MHz (lambda = 1 m) come il NEC-2, prima 144,3 MHz: i modelli di W1GHZ scritti in lunghezze d'onda coincidono con nec2c | 1.5.9 |

**Cosa non e' cambiato.** Le formule del metodo dei momenti (funzioni di base, nucleo sottile ed esteso, campi vicini e lontani, Sommerfeld-Norton) sono quelle del NEC-2: YA-NEC non ha un motore MININEC (1.5) e non ha il codice del NEC-4. Le superfici (SP, SM, SC) non ci sono ancora.

**Confronto con gli altri motori derivati dal NEC-2.** nec2-rs di UR3PKI [C117] (Rust, licenza MIT) elenca dieci punti in cui nec2c si scosta dal Fortran: zint, evlua, intrp e rom2 YA-NEC li aveva gia' corretti allo stesso modo; restano da valutare il criterio di convergenza di gshank (|re| + |im| nel Fortran), la contabilita' delle perdite di rete con piu' sorgenti, le condizioni del nucleo esteso in qdsrc e nefld, e le superfici. hf-nec2c di R. Sutherland [C116] e nec2c-ae6ty di AE6TY [C115] sono le fonti delle costanti di zint e della LU a blocchi; xnec2c, nec2c-windows e le altre copie di nec2c non hanno differenze di calcolo.

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
| quad (dalla 1.4.2) | perimetro = 8 · (0,0001386 D1⁴ + 0,0026779 D1³ + 0,0195541 D1² + 0,0654053 D1 + 0,2164538) λ, D1 = log10(d/λ), valido da 1E-6 a 1E-2 λ; Automatico: Cebik per 1 elemento fino a 30 MHz; con piu' elementi o sopra i 30 MHz risonanza cercata dal NEC | Cebik [C37] |
| forcina (beta / hairpin) | Xc = √(R(Z0−R)), XL = Z0·√(R/(Z0−R)), Z0f = 120·arccosh(s/d), lunghezza = arctan(XL/Z0f)/β | Cebik [C38] |
| hexbeam | geometria a W e a U di G3TXQ | G3TXQ, M0OXO (nel manuale d'uso) |

### 5.3 Casi di prova

Moxon a filo di DK7ZB per i 6, 10, 12, 15, 17, 20 e 30 m (filo nudo da 1 mm, spazio libero): YA-NEC come nec2c. Delta loop a 14,2 MHz con vertice a 37 gradi: 49,2 - j127 ohm.

Quad di Cebik (dalla 1.4.2): la formula ridà la Tab. 2-2 di [C37] (filo #12: 1,0542 λ a 14,1 MHz, 1,0812 λ a 146 MHz); rispetto all'ARRL il perimetro esce piu' lungo del 3 % in HF e fino al 10,8 % a 144 MHz con tubo da 12 mm, per questo sopra i 30 MHz il metodo Automatico passa al NEC. La formula e' la risonanza della spira da sola: con un riflettore a 0,15 λ il radiatore risuona circa il 4 % piu' in basso (14,2 MHz: ROS sotto 2 solo tra 13,56 e 13,75 MHz), quindi dalla 1.5.3 con 2 o 3 elementi Automatico passa al NEC anche in HF. Moxon da 2 m di Cebik [C38] con nec2c: elementi da 1/4" e 1/2" come la sua Tab. 2 (6,1 e 6,0 dBi, F/B 30 e 36 dB, 48 - j2 e 51 - j2 ohm a 146 MHz); con 1" il NEC-2 non converge (segmenti troppo corti rispetto al raggio).

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
| reattanza del dipolo e del monopolo corti (dalla 1.5) | Z = R(kl) − j[120 (ln(l/a) − 1) cot kl − X(kl)], R(kl) e X(kl) con i polinomi di Tai (entro 0,5 Ω dalla Tab. 4-1), kl fino a π/2; monopolo = metà | AEH 4ª eq. 4-1, 4-2 [C40] |
| dipolo ripiegato in frequenza (dalla 1.5) | raggio equivalente dei due conduttori (eq. 4-9); a = ln(s/r1)/ln(s/r2) (Guertler, uguale al rapporto ARRL); Zf = j Z0 tan kl; Z = 2(1+a)² Zr Zf / ((1+a)² Zr + 2 Zf) | AEH 4ª eq. 4-9 – 4-11 (Uda e Mushiake) [C40] |
| loop magnetica, accoppiamento capacitivo (dalla 1.5) | C2 = √(R0/R − 1)/(ω R0) in parallelo all'ingresso; C1 = 1/(ω(ωL − √(R(R0 − R)))) in serie alla loop | AEH 4ª eq. 5-20, 5-21 (Smith) [C40] |

Casi di prova: ground plane a 4 radiali e monopolo su terreno perfetto come nec2c [C1]; verticale λ/4 con 16 radiali: Tab. 9-1 circa 14,9 ohm di terra (efficienza 71 %), Sevick circa 9 ohm (80 %) [[Calcolo: 3.5 Radiali a terra: confronto delle fonti]].

NVIS (dalla 1.4.2) [C38], [C39]: dipolo a 3,6 MHz su terreno medio con nec2c, guadagno allo zenit 1,23 dBi a 0,05 λ, 6,57 dBi a 0,175 λ (massimo), 6,10 dBi a 0,25 λ; schermo 1 λ x 1 λ: +1,9 dB su terreno molto povero (3 / 0,001), +0,7 dB su terreno medio, +0,2 dB su terreno molto buono, come Cebik. J-pole standard di Cebik (146 MHz, tubo 3/8") con nec2c: 2,45 dBi (Cebik 2,46 dopo l'AGT); Slim Jim: nessun guadagno in piu' [C38].

Somiglianze e differenze (dalla 1.5): la resistenza del dipolo corto dell'AEH (eq. 4-2, 20(kl)²) è la stessa formula di Kraus e Balanis che usavamo; l'AEH aggiunge la reattanza e la resistenza con la corrente sinusoidale (3,18 Ω contro 2,85 Ω per un dipolo di 10 m a 3,6 MHz). Il rapporto del ripiegato di Guertler coincide con quello ARRL; l'AEH aggiunge la formula fuori risonanza, che a risonanza dà meno di 73 Ω × rapporto perché il dipolo equivalente è più grosso. L'accoppiamento capacitivo della loop è la rete L a due condensatori: concorda con le formule di W5QJR e ARRL per L, R e Q, e si aggiunge al loop di Faraday e al gamma.

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

### 8.4 G/T dello stack nell'ottimizzatore (dalla 1.5.8)

La maschera VHF dell'ottimizzatore usa lo stesso G/T di 8.1 (VE7BQH, TANT, correzione KF2YN) su uno stack di righe x colonne alle distanze DL6WU (9.1), ricalcolate a ogni passo dalle aperture a -3 dB della singola antenna. Due modi:

> FORMULA: G_stack(θ, φ) = G_singola(θ, φ) · |Σ e^(j k p_i · u)|² / N        (veloce: antenne uguali in fase, senza accoppiamento)

Nel modo veloce lo stack e' il diagramma NEC della singola per il fattore di schiera; ogni 10 passi e alla fine si calcola lo stack completo con il NEC (StackDetect.Crea, accoppiamento compreso) e la differenza NEC - veloce corregge i passi seguenti. Nel modo NEC lo stack completo si calcola a ogni passo. La catena di ricezione segue Friis:

> FORMULA: Tsys = Tant + Tpre + Tcavo / Gpre        Tcavo = 290 · (10^(L/10) - 1)        G/T = G_stack - 10 · log(Tsys)

Tant comprende il diagramma, le perdite prima del preamplificatore (combinatore) e quelle dei conduttori (efficienza del NEC). Con 20 dB di preamplificatore, 2 dB di cavo dopo di esso aggiungono 1,7 K. Con il **cielo vero** (dalla 1.5.8) la parte del diagramma Tpattern viene da CieloVero.Tpattern (8.3: mappa di Haslam scalata, vista dal QTH all'ora data, puntamento in azimut ed elevazione, 290 K sotto l'orizzonte) al posto delle temperature uniformi; Tloss e la catena di ricezione restano uguali. Il punteggio del voto e' 1 / (1 + e^(-(G/T - G/T0) / 0,5 dB)), G/T0 quello di partenza; con una soglia in dB/K, raggiunta la soglia vale 1.

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

## 16. Filari alimentate in punta

Le formule della scheda Calcolatori > HF > Filari (dalla 1.5.6) e dell'abaco (dalla 1.5.8). Fonti: AA5TB [C88], TF5NN [C89], VE3EED [C93], DG1JAN [C94], I3RKE [C95], OfficinaHF [C96], O. Duffy [C97], W8JI [C82].

### 16.1 Il filo come linea di trasmissione

Il filo alimentato in punta si tratta come una linea aperta la cui perdita e' la potenza irradiata [C89]: verificato con NEC-2, 4 % di errore su |Z| su terra ideale, 11 % con il contrappeso.

> FORMULA: Z0 = 60 · ln(0,22 λ / a) · VF        θ = 2π L / (λ · VF) + e · π        α = atanh(Rrad / Z0)        Z = Z0 · [sinh 2α − j sin 2θ] / [cosh 2α − cos 2θ]

L'effetto d'estremita', in mezze onde, e' e = 0,0464 − 0,003 · ln(λ / a) (minimo 0,004); la mezz'onda vera e' L = (1 − e) · VF · λ/2. Le armoniche cadono un po' corte: l'effetto d'estremita' e' lo stesso in valore assoluto ma pesa meno su piu' mezze onde. Per questo un filo da 40 m va meglio sui 20 e 10 m che sui 15 m, e l'accordo multibanda cerca un compromesso. Per il filo isolato il fattore di velocita' viene dall'induttanza aggiunta di Cebik, la stessa del LD 7 del modello:

> FORMULA: ΔL = 2·10⁻⁷ · (εr R/r)^(1/12) · (1 − 1/εr) · ln(R/r) [H/m]        VF = √(L' / (L' + ΔL)),  L' = Z0 / c

### 16.2 Trasformatore, condensatore e tensione in punta

In punta a una mezz'onda l'impedenza e' di 2000-3500 ohm: il 49:1 (spire 7:1) porta 2450 ohm a 50 ohm, il 64:1 (8:1) porta 3200 ohm. Nel modello il trasformatore e' ideale e il ROS e' riferito a 50 x rapporto. Il condensatore sul primario (100 pF con il 49:1, 150 pF con il 64:1, almeno 1 kV) compensa l'induttanza dispersa del trasformatore e abbassa il ROS sulle bande alte [C94] [C96]; non sostituisce il contrappeso. Il rapporto spire e' √(Za / 50) [C88]: AA5TB trova con misure e modelli che l'8:1 (64:1) da' il ROS piu' stabile al variare del contrappeso.

Il nucleo non e' senza perdite. DG1JAN [C94] le ha misurate con un NanoVNA su un carico da 2400 ohm (perdita del nucleo = perdita di inserzione - disadattamento - carico): FT-82-43 e FT-114-43 intorno all'80 % di efficienza (circa 1 dB), FT-140-43 2/14 intorno al 79 %, FT-240-43 2/15 intorno al 75 % (71 % a 30 MHz, circa 1,2-1,5 dB), due nuclei Fair-Rite accoppiati intorno al 90 %, il Fair-Rite 2643251002 consigliato da MM0OPX per 100 W intorno al 91 %. Nel modello NEC il trasformatore e' ideale; dalla 1.5.8 la scheda Filari ha la scelta del nucleo e mostra le perdite (efficienza di DG1JAN letta sulle sue curve a 3,5, 14 e 28 MHz e interpolata, -10 log η in dB) e la potenza che scalda il nucleo, P (1 - η). Per due FT-240-43 accoppiati DG1JAN non ha misure: la scheda usa quelle di un FT-240-43 solo e lo dice.


Potenza del nucleo (dalla 1.5.9). OH7SV [C114] da' la potenza che regge l'FT-240-43 nel trasformatore 49:1, per numero di nuclei, modo e banda: un nucleo 30 / 60 / 120 W sui 160 m e 50 / 100 / 200 W dagli 80 m in su (continua, cioe' FT8 e RTTY / CW / SSB); due nuclei 120 / 240 / 480 e 200 / 400 / 800 W; tre nuclei 270 / 540 / 1080 e 450 / 900 / 1800 W. Il condensatore di compensazione e' da 47-100 pF, 2 kV. Con «automatico» la scheda usa l'FT-140-43 fino a 15 W, poi il nucleo piu' piccolo che regge la potenza nel modo e sulla banda piu' bassa, nell'ordine 1 x FT-240-43, 1 x FT-290-43, 2 x FT-240-43, 2 x FT-290-43, 3 x FT-240-43. L'FT-290-43 (Fair-Rite 5943011101, 73,7 x 38,9 x 12,7 mm, AL 1300 nH) non e' nella tabella di OH7SV ne' tra le misure di DG1JAN: la sua potenza e' stimata 1,5 volte quella dell'FT-240-43 (sezione 221 contro 162 mm², volume 39 contro 25 cm³) e l'efficienza e' presa uguale.

> FORMULA: nuclei = il piu' piccolo con P(OH7SV, modo, banda) ≥ P trasmessa        P(FT-290-43) ≈ 1,5 · P(FT-240-43) (stima)

Somiglianze e differenze: la regola precedente della scheda («un FT-240-43 a 30-50 W in continua, 100 W in SSB») coincide con la riga degli 80 m di OH7SV; DG1JAN misura le perdite di un solo FT-240-43 e non la potenza; OfficinaHF usa nuclei piu' piccoli a 100 W, che DG1JAN e OH7SV sconsigliano.

> FORMULA: V = √(P · |Z|)        (100 W su 3300 ohm: 574 V efficaci, 812 V di picco)

### 16.3 Contrappeso

La corrente in punta e' piccola: I = √(P / R), cioe' 0,20 A con 100 W su 2450 ohm. Per farla tornare basta un contrappeso corto, 0,05 λ della banda piu' bassa [C88]; in alternativa fa da contrappeso la calza del coassiale fino al choke [C95]. Senza contrappeso il NEC non trova una via di ritorno (con i soli 0,1 m del collegamento il ROS supera 99 a ogni banda): nella pratica fa da contrappeso la calza del coassiale. L'abaco «senza contrappeso» la modella come 10 m di coassiale senza choke che scendono dal trasformatore fino a 0,3 m da terra e vanno verso la radio. Sul terreno medio, a 3 m: la mezz'onda accordata sui 40 m cambia poco di lunghezza (20,27 m contro 19,85 m con il 49:1) ma il ROS alla banda sale da 1,5 a 2,4; sulle bande alte invece il ROS scende quasi ovunque sotto 2 (10 m: 1,1-2,1 con qualunque filo), e persino il quarto d'onda dei 20 m da' 1,4. Non e' un merito dell'antenna: la calza diventa parte del radiatore, irradia e porta la RF in stazione, e il ROS dipende dalla lunghezza del coassiale. Sulla banda piu' bassa il quarto d'onda resta inservibile (160 m: 67,5 con il 49:1, 84 con il 64:1). Per questo la scheda, con «nessuno», chiede la lunghezza del coassiale fino alla radio e lo segnala con un avviso.

Con il 9:1 l'impedenza in punta e' piu' bassa (450 ohm nominali) e la corrente sale a 0,47 A con 100 W: il contrappeso deve essere piu' lungo. L'abaco del 9:1 mostra che il contrappeso migliore dipende dal filo: quasi sempre tra il 5 % (filo da 40 m con 2 m) e il 25 % (25,6 m con 6,4 m; 32,6 m con 8,2 m), con l'eccezione del filo da 21,6 m che vuole 10 m (46 %).

### 16.4 Bobina di carico

Per allungare l'EFHW di una banda piu' bassa si mette una bobina dopo il filo della banda alta, seguita da una coda. La stima tratta il filo come linea: la reattanza della bobina annulla quella dei due tratti alla banda aggiunta.

> FORMULA: X_L = Z0 · (cot θ1 + cot θ2),  θ1 = filo prima della bobina,  θ2 = coda + effetto d'estremita'

Con i dati di OfficinaHF [C96] (20,15 m + coda di 2,39 m per gli 80 m) la formula da' 128 µH contro 110-120 µH dichiarati. Nel NEC, con contrappeso e terreno, la bobina migliore e' piu' piccola (95-101 µH) e sugli 80 m il ROS non scende sotto 1,9 (verticale, 64:1) e arriva a 6 (U rovesciata, 49:1): la sola bobina non annulla la reattanza in punta. Il pulsante «Accorda la bobina (NEC)» cerca l'induttanza con il ROS piu' basso; con la bobina il 64:1 va sempre meglio del 49:1.

### 16.5 Abachi

Calcolati con «YA-NEC --abaco-filari» (filo nudo da 1 mm orizzontale a 3 m, contrappeso orizzontale opposto alla stessa altezza, trasformatore ideale, terreno medio e perfetto; per il 49:1 e il 64:1 anche senza contrappeso, con la calza di 10 m di coassiale al suo posto) e mostrati dal pulsante «Abaco ›» della scheda. Si leggono cinque cose.

- **λ/2 contro λ/4.** Alla banda piu' bassa il quarto d'onda da' ROS oltre 99 con 49:1 e 64:1 su entrambi i terreni: in punta l'impedenza e' bassa (decine di ohm), il contrario di quella per cui e' fatto il trasformatore. La mezz'onda accordata da' ROS tra 1,0 e 1,7 sul terreno medio.
- **Terreno perfetto.** Un filo a 3 m sopra un piano perfetto ha sotto di se' la propria immagine in controfase: la resistenza di radiazione crolla, l'impedenza in punta va molto oltre 3000 ohm e sulle bande basse il ROS resta alto anche alla mezz'onda (40 m: 6-7; 80 e 160 m: oltre 20). Il terreno medio, con le sue perdite, smorza questi picchi ed e' l'ipotesi della scheda.
- **Monobanda e multibanda.** Accordata per una banda, la mezz'onda va benissimo li' e male sulle armoniche (40 m accordata: 40 m 1,5, 15 m 5,6). Accordata come multibanda cede qualcosa sulla fondamentale e guadagna sulle armoniche (20,44 m con 49:1: 40 m 2,4, 20 m 1,2, 15 m 1,9, 10 m 2,0).
- **9:1.** Sul terreno medio a 3 m la lunghezza migliore trovata e' 40 m con 2 m di contrappeso: sei bande con ROS fino a 3 (40, 20, 17, 15, 12 e 10 m). Delle lunghezze di VE3EED [C93] la migliore e' 36,3 m con 5 m di contrappeso (quattro bande). Sui 30, 60 e 80 m il 9:1 resta un caso da accordatore esterno.
- **Senza contrappeso.** La calza del coassiale abbassa il ROS sulle bande alte e lo alza sulla fondamentale (16.3): numeri migliori in apparenza, con il coassiale che irradia.

### 16.6 Casi di prova

60 combinazioni della scheda (3 tipi x 6 disposizioni x 2 trasformatori x bobina si'/no), terreno medio, accordo NEC del pulsante («YA-NEC --prova-filari»). EFHW monobanda 40 m: ROS 1,04-1,68 in tutte le disposizioni. Multibanda 40-10 m: 20, 15 e 10 m tra 1,05 e 2,1 ovunque, 40 m tra 2,1 e 3,3. Bobina per gli 80 m: da 1,94 (verticale, 64:1) a 6,02 (U rovesciata, 49:1). Verticale e inclinata dall'alto sono le disposizioni migliori, L e U rovesciata le peggiori. Il filo non risonante con il valore predefinito di prima (16,2 m, contrappeso 7,5 %) non funzionava: ROS 8-99 su quasi tutte le bande; per questo la scheda propone ora le righe dell'abaco.

### 16.7 Fonti, somiglianze e differenze

| Fonte | Somiglianze | Differenze |
|---|---|---|
| AA5TB [C88] | contrappeso 0,05 λ; corrente piccola in punta; 8:1 piu' stabile; calza come ritorno | modelli con un altro programma, non NEC-2 |
| TF5NN [C89] | modello del filo ed effetto d'estremita' usati da YA-NEC; verificato con NEC-2 | terra ideale: sulle bande basse il NEC con il terreno vero da' impedenze diverse |
| VE3EED [C93] | lunghezze del filo non risonante | nell'abaco a 3 m le migliori sono altre (40 m); dipende da altezza e contrappeso |
| DG1JAN [C94] | 100 pF; perdite misurate dei nuclei (FT-240-43 circa 75 %, Fair-Rite accoppiati circa 90 %) | sconsiglia FT-140-43 e FT-114-43, che OfficinaHF usa a 100 W |
| W8JI [C82], O. Duffy [C97] | avvolgimento e perdite dei trasformatori | - |
| I3RKE [C95] | la calza del coassiale fa parte dell'antenna | nessuna lunghezza ne' trasformatore |
| OfficinaHF [C96] | bobina per gli 80 m, tensione in punta, 64:1 per gli 80 m | contrappeso «non necessario»; «80 mH» per 80 µH; 160 m con 260 µH non ottimale |

## 17. Antenne riceventi per le bande basse: le note di IV3PRK

Approvato da IC8TEM il 22/11/2026 21:34. Fonti: i documenti di Pierluigi "Luis" Mansutti IV3PRK (iv3prk.it, tutti i diritti riservati: qui solo numeri e idee, riassunti con parole nostre e citati), letti il 22/11/2026. Il capitolo 7 da' le definizioni (RDF, DMF, Beverage); qui le esperienze di una stazione reale sui 160 m, con i numeri dei suoi modelli EZNEC e delle sue misure, e quello che ne viene per chi modella con YA-NEC.

### 17.1 In ricezione conta la direttivita', non il guadagno

Sulle bande basse il rumore che arriva dall'esterno supera quello del ricevitore: alzare il segnale alza anche il rumore. Conta il rapporto segnale/rumore, e con l'antenna lo si migliora solo togliendo rumore e QRM dalle direzioni che non servono. Per questo il parametro e' l'RDF (7.1) e non l'efficienza. IV3PRK lo mostra sul suo traliccio: da 4 radiali rialzati a 32 radiali a terra il guadagno sale di 0,4 dB, l'RDF resta 5,06-5,07 dB [C99].

> FORMULA: RDF = G(direzione voluta) - Gmedio        (W8JI; passo del diagramma 5 gradi = 2 592 direzioni, stesso risultato del passo di 1 grado [C99])

Ogni dB di RDF in piu' e' un dB in meno di rumore e QRM dalle altre direzioni. Valori pratici sotto 10 dB; 12-13 dB chiedono ettari di terreno.

| Antenna (EZNEC, 1,835 MHz) | Guadagno | Angolo | Apertura | F/B | Gmedio | RDF |
|---|---|---|---|---|---|---|
| traliccio shunt-fed, 4 radiali rialzati | +1,30 dB | 25 | - | - | -3,76 | 5,06 |
| dipolo basso | -2,70 dB | 90 | - | - | -10,00 | 7,30 |
| Pennant (9 x 4 m, 880-900 ohm) | -35,32 dB | 30 | 147 | 37 dB | -43,11 | 7,79 |
| Flag (950 ohm) | -29,97 dB | 30 | 149 | 30 dB | -37,73 | 7,76 |
| 4-square mini caricata (1994) | -5,86 dB | 20 | 127 | 30 dB | -15,23 | 9,37 |
| Beverage 175 m | -9,82 dB | 36 | 84 | 18 dB | -20,63 | 10,81 |
| Beverage 350 m | -6,43 dB | 24 | 53 | 27 dB | -20,09 | 13,66 |
| due Pennant end-fire a 30 m, 173 gradi | -38,00 dB | 24 | 82 | 40 dB | -49,27 | 11,27 |
| DHDL TX3A (22 m, 1250 ohm) | -27,57 dB | 20 | 106 | 31 dB | -37,52 | 9,95 |
| QDFA (4 Delta Flag, modello da sola) | circa -30 dB | - | - | 60 dB | - | 11,3 |

Fonti: [C99] per le prime righe, [C100] Pennant e Flag, [C102] DHDL, [C104] QDFA. YA-NEC (7.3) calcola per la Beverage da 175 m un RDF di 10,55 dB contro i 10,81 di IV3PRK.

### 17.2 Pennant e Flag

Loop piccoli chiusi su una resistenza dalla parte opposta all'alimentazione: funzionano come due verticali corte end-fire alimentate dai lati orizzontali (stessa famiglia di Flag, Delta, K9AY, EWE). La Pennant di K6SE (QST, luglio 2000) di IV3PRK: filo isolato di 22,5 m in tutto, lato verticale 4,26 m con il fondo a circa 1,8-2 m, due lati da 9,12 m che si uniscono al trasformatore a circa 4 m; resistenza di circa 880-900 ohm a meta' del lato verticale (quattro da 220 ohm a carbone in serie); il lobo va verso l'alimentazione [C100]. Pregio principale: nessun collegamento a terra, quindi meno dipendenza dal terreno di K9AY ed EWE.

Il segnale in uscita e' bassissimo (-35 dB): servono uno o due preamplificatori (IV3PRK: K9AY 10 dB esterno e KD9SV a circa 18 dB). La Flag da' circa 5 dB di segnale in piu' a parita' di RDF e F/B [C101].

Due Pennant end-fire (EZNEC): l'F/B e' massimo a circa 120 gradi di sfasamento, l'RDF cresce fino a circa 180 gradi e non dipende dalla distanza (25-40 m). Con 30 m di distanza e 52 m di RG58 (173 gradi): RDF 11,27 dB. Pennant e Flag insieme danno oltre 1 dB di RDF in meno: meglio due uguali [C101]. Nella realta' IV3PRK non ha mai trovato una prova A/B in cui le due Pennant sfasate battessero la Flag rotativa.

### 17.3 Trasformatori e choke

Il trasformatore e' la parte piu' critica: il segnale e' di decine di dB sotto lo zero e non si puo' perdere altro. Avvolgimenti separati (non autotrasformatori): la calza del coassiale non tocca il lato antenna, cosi' il rumore preso dalla calza non entra nel segnale ("rumore di modo comune") [C103].

> FORMULA: X primario >= 4 · Z antenna        L = X / (2π f)        N = 1000 · √(L[mH] / AL)        N1/N2 = √(Z1/Z2)

Esempio di IV3PRK per la Pennant (900 ohm) su binoculare BN73-202 (materiale 73, AL 8500, permeabilita' circa 1500 a 2 MHz): X = 3600 ohm; L = 3600 / (2π · 1,8 MHz) = 318 µH; N1 = 1000 · √(0,318 / 8500) = 6,1 spire; rapporto spire √(900/50) = 4,24; arrotondato a 8 spire lato antenna e 2 lato 50 ohm (in pratica 12:3 o 8:2) [C103]. Con il materiale 61 o 77 su un FT50 le spire diventerebbero 68:16 o 21:5. Verifica di YA-NEC: 3600 / (2π · 1,8e6) = 318,3 µH; √(0,3183/8500) · 1000 = 6,12; stessi numeri.

Choke sul coassiale: sui 160 m serve un'impedenza resistiva di almeno 1500 ohm. Materiale 75 (J): a 2 MHz µ' circa 300 e µ" circa 1600, quindi impedenza quasi tutta resistiva. 16 spire di RG58 su un FT140-J superano i 2000 ohm (misurate fino a 8 spire, circa 1000 ohm; la regola del quadrato delle spire sovrastima). Spire distanziate, coassiale a terra vicino al choke. Dal 2025 IV3PRK usa l'FT240-31 (materiale 31, consigliato da K9YC) [C103]. Con il DHDL toglie la calza del tutto: 15 m di doppino (circa 100 ohm) tra trasformatori isolati, "meglio di qualsiasi choke" [C102].

### 17.4 Four-square ricevente

Elementi caricati in cima alla W8JI (RXvrhat), modello EZNEC: verticali da 7,5-9,5 m, carico 73 + j296 ohm (Q circa 4), RDF del singolo 4,86 dB. Due elementi end-fire: a λ/4 (42 m) lo sfasamento di 95 gradi da' F/B 51 dB ma un lobo alto; 115 gradi mette il nullo a 30-60 gradi (stazioni europee a 200-800 km) con RDF 8,79. A λ/8 (21 m) il guadagno scende di circa 3 dB ma l'RDF sale (9,04 a 137 gradi, 9,50 a 147) [C105].

Quadrato in crossfire (diagonale): linea di ritardo all'elemento posteriore = diagonale x 0,95 x VF, meta' agli elementi laterali (DX Engineering). Con 29,7 m di diagonale e CATV con VF 0,85: 24 m e 12 m. Con 142 gradi sull'elemento posteriore: RDF 11,40, F/B 38 dB [C105].

> FORMULA: linea di ritardo = diagonale · 0,95 · VF        gradi = lunghezza / VF · 360 / λ

Costruzione (parte 2, agosto 2008) [C112]. Cavo da 75 ohm per interramento; il VF misurato con l'analizzatore su una linea a λ/4 era 0,82 e non 0,81 come da catalogo, quindi a 1,825 MHz il quarto d'onda e' 33,70 m e non 33,27 m: misurare sempre il VF prima di tagliare. Lato del quadrato ridotto a 20 m dal terreno disponibile, verticali di 9,5 m caricate in cima, carico R 74 + j295 ohm. Modello finale:

| Alimentazione | Ritardi (gradi) | Guadagno | Angolo | Apertura -3 dB | F/B | RDF |
|---|---|---|---|---|---|---|
| diagonale (crossfire) | 58 e 29 | -23,51 dBi | 20 gradi | 74 gradi | 29 dB | 12,02 dB |
| lato (end-fire/broadside) | 0 e 29 | -15,48 dBi | 22 gradi | 115 gradi | 13 dB | 9,89 dB |

Le linee di ritardo sono 21,70 m e 10,85 m (diagonale) e 9,85 m (lato), meno 1 m di cavo dentro le scatole; i quarti d'onda 33,20 m (33,70 meno 0,50 interni). Sul lato il segnale e' circa 8 dB piu' forte che in diagonale, quindi un attenuatore a T da 8 dB (21,5 e 47,3 ohm) entra in linea nelle posizioni di lato per avere lo stesso livello. Prova al banco con le linee chiuse su 75 ohm: circa 52 ohm in diagonale e 42 ohm sul lato. Nella scatola degli elementi un'induttanza di 20 spire su un nucleo di ferrite scorrevole (circa 25 µH, 290-295 ohm a 1,825 MHz) e le resistenze di carico su paletti per cambiarle sul campo.

Modo comune e fulmini, sui consigli di K4SAV (e K7TJR): trasformatore d'uscita ad avvolgimenti separati senza collegare la calza; condensatore da 10 nF e diodo inverso su ogni rele'; negativo delle bobine a massa tramite 10 nF; cavo di comando avvolto su due FT140-J; calza del cavo verso la stazione e -12 V del comando su un picchetto di terra separato (obbligatorio per i fulmini; con il trasformatore isolato il choke sul coassiale non serve); scaricatore a gas su ogni verticale (utile solo per le scariche vicine, non per il colpo diretto). Terra: base dei pali in legno avvolta in lamiera di rame per 1 m, poi radiali quanti servono; stessi valori su tutti gli elementi.

Sul campo: F/B 30 dB su segnali forti, ma S/N peggiore delle Pennant (rumore circa 10 dB piu' alto) [C106]. Il rumore veniva dagli elementi (con le linee chiuse su 75 ohm il rumore scende a -122 dBm, con le antenne -103/-110 dBm); tolti i fili di carico, rumore giu' di 10-15 dB [C107]. Poi la 4-square di K7TJR con amplificatori ad alta impedenza: modo A per l'RDF massimo (circa 12 dB, lobo stretto), modo B per l'F/B, con linee di ritardo nel rapporto 0,5 (lato 20 m: circa 48 o 72 gradi) [C108].

### 17.5 Misure di S/N: i modelli non bastano

IV3PRK misura con un oscillatore di prova a 1,843 MHz a circa una lunghezza d'onda e un SDR [C106]:

| Antenna (verso NE) | Segnale | Rumore | S/N |
|---|---|---|---|
| 4-square, diagonale | -61 dBm | -97 dBm | 36 dB |
| Pennant gruppo nord | -60 dBm | -107 dBm | 47 dB |
| Pennant broadside | -59 dBm | -108 dBm | 49 dB |
| DHDL TX3A | -62 dBm | -100 dBm | 38 dB |
| antenna TX (senza preamplificatori) | -50 dBm | -95 dBm | 45 dB |

Tolti i 25 dB di preamplificatori, il fondo di rumore della Pennant e' circa 40 dB sotto quello del traliccio TX. La DHDL ha lo stesso segnale della Pennant vicina ma almeno 5 dB di rumore in piu' (area piu' grande) [C106].

L'ambiente decide piu' del modello:
- un cavo telefonico che attraversava il terreno dava 40 dB di rumore in piu' alla Beverage a due fili verso sud; interrato, la Beverage e la 4-square hanno funzionato [C109];
- il QDFA modellato con i fili vicini: F/B da 60 a 15 dB e RDF da 11,3 a 10,0 con traliccio e radiali; con la recinzione metallica non a terra il diagramma diventa omnidirezionale, a terra l'F/B scende a 7,5 dB: progetto abbandonato [C104];
- il miniloop a tre spire di Lankford: nullo laterale di 30-40 dB sulle onde medie (1 MHz), solo 5 dB sui 160 m, e nessun nullo posteriore [C110].

### 17.6 Beverage e K9AY in pratica: le note di IK0XBX

Francesco Fucelli IK0XBX (Sezione ARI di Perugia, «Le mie antenne RX», 2018) [C111] ha lavorato oltre 200 paesi DXCC sui 160 m con un loop K9AY a quattro direzioni (due loop e due relè, schema di ON4UN), una Beverage in due versi e una DHDL. Le sue regole costruttive:
- **Beverage**: filo da 1 mm a 1,5-2,5 m da terra (1,5 m la migliore per il DX, fino a 6 m accettabile: piu' alta = piu' segnale ma lobi laterali e angolo piu' alti, quindi piu' rumore); resistenza antinduttiva da 470-560 ohm (almeno 2 W se in stazione c'e' un amplificatore) verso un picchetto; trasformatore 9:1 su binoculare BN73-202 con 2 spire lato cavo e 6 lato antenna (poche spire = poco accoppiamento capacitivo);
- **lunghezza**: dai grafici di ON4UN sui 160 m, 89 m danno -14,3 dBi e un lobo di circa 120 gradi, 176 m -9,9 dBi e 60 gradi, 353 m -6,3 dBi e 30 gradi; 90 m sui 40 m si comportano come 353 m sui 160 m;
- **terreno**: piu' e' conduttivo, piu' basso e' il segnale; non cambia molto l'angolo ne' la direttivita'; evitare i radiali della verticale TX e le reti metalliche (le sue passano vicino e funzionano lo stesso);
- **cavo**: 75 ohm TV, calza non collegata alla terra dell'antenna, ferriti a 5-10 m dall'antenna e all'ingresso in casa, nessuna messa a terra del cavo a meno di 5 m dall'antenna; picchetti di rame da 1 m (tubo da 35 mm), anche due o piu'.

Confronto con IV3PRK: la Beverage da 176 m di ON4UN (-9,9 dBi) coincide con quella da 175 m di IV3PRK (-9,82 dBi, RDF 10,81); stesso binoculare BN73-202, 6:2 per i circa 450 ohm della Beverage (IK0XBX) e 8:2 per i 900 ohm della Pennant (IV3PRK), in tutti e due i casi avvolgimenti separati e calza staccata dalla terra dell'antenna contro il rumore di modo comune. Differenza: IK0XBX mette la K9AY al primo posto per l'ascolto in tutte le direzioni in poco spazio, IV3PRK la scarta per la dipendenza dal terreno e preferisce Pennant e Flag (17.2).

### 17.7 Cosa ne viene per YA-NEC

- Confrontare le antenne riceventi con l'RDF (linguetta Rumore/GT/Stack, Calcolatori > Antenne riceventi), non con il guadagno; il passo di 5 gradi basta.
- Mettere nel modello i fili vicini: traliccio, radiali rialzati, recinzioni, linee. YA-NEC li calcola con lo stesso NEC; IV3PRK mostra che sono loro a rovinare l'F/B (il QDFA da 60 a 7,5 dB).
- Il NEC non misura il rumore locale: un RDF alto nel modello non garantisce l'S/N (Pennant contro 4-square, DHDL, Pennant end-fire).
- Per il trasformatore e il choke della ricevente: formule di 17.3 (Calcolatori > Strumenti > Toroidi FT per le spire con l'AL).

### 17.8 Fonti, somiglianze e differenze

| Fonte | Somiglianze | Differenze |
|---|---|---|
| IV3PRK [C99]-[C110] | stessa definizione dell'RDF (W8JI), modelli EZNEC con terreno MININEC | YA-NEC usa il terreno di Sommerfeld; i modelli di IV3PRK comprendono tutto il suo terreno (fino a 1487 segmenti) |
| W8JI [C82] | RDF, verticali caricate in cima, trasformatori su binoculare | - |
| ON4UN [C18] | Beverage, choke, attenuatori a T nella 4-square; grafici della Beverage per lunghezza (anche in IK0XBX) | - |
| IK0XBX [C111] | Beverage 1,5-2,5 m, 470-560 ohm, BN73-202 6:2, calza staccata dalla terra, K9AY a quattro direzioni | preferisce la K9AY, che IV3PRK scarta; nessun modello NEC, solo risultati sul campo (DXCC) |
| K9YC | materiale 31 per i choke sulle bande basse | IV3PRK ha usato il 75 (J) fino al 2025 |
| K6SE, K7TJR, Lankford, N4IS e AA7JV | Pennant, 4-square Hi-Z, QDFA e miniloop, DHDL | progetti loro, IV3PRK riporta le sue prove |

## 18. Verticale non risonante (43 piedi)

Approvata da IC8TEM il 22/11/2026 21:22 (bozzetto). Fonte principale: P. Salas AD5X, «The 43-Foot Vertical», nella presentazione di N0CU (2016) [C113]: qui solo numeri e idee, citati.

### 18.1 Cosa calcola

Una verticale di 13,1 m e' corta sui 160 e sugli 80 m (resistenza di radiazione di pochi ohm e reattanza capacitiva di centinaia di ohm), vicina al quarto d'onda sui 30 m e lunga 5/8 λ o piu' dai 15 m in su. Con un un-un 4:1 alla base il ROS sul coassiale e' accettabile dai 60 ai 10 m; sui 160 e sugli 80 m l'accordatore in stazione accorda il cavo, non l'antenna, e la perdita nel cavo con il ROS diventa di molti dB [C113].

> FORMULA: Z alla base = Z del monopolo (TF5NN v3, terra ideale) + Rg        oppure dal NEC-2 con il terreno di Sommerfeld

> FORMULA: carico del cavo = Z / 4 (un-un 4:1 ideale)        perdita nel cavo = 10 log10(Pin / Pcarico) sulla linea con perdite (gamma = alfa + j beta)

> FORMULA: perdita dell'accordatore = 10 log10[(1 + |X| / (Qu R)) (1 + k Qn / Qu)],  Qn = √(Rmax / Rmin - 1),  Qu = 150

> FORMULA: V alla base = |Z| √(P all'antenna / R)        al trasmettitore = G (NEC) - perdita nel cavo - perdita dell'accordatore

### 18.2 Terra e radiali

Radiali interrati: il NEC-2 non tratta i fili interrati; la loro perdita entra come resistenza di terra in serie alla base, con il terreno «MiniNec» (GN 3: impedenza su terreno perfetto, diagramma con er e sigma del terreno scelto, come EZNEC e AD5X; con il GN 2 il filo che tocca il terreno reale senza schermo di radiali darebbe centinaia di ohm), dalla Tab. 9-1 di ON4UN con la lunghezza in λ di ogni banda (la tabella va da 0,15 a 0,40 λ: sotto 0,15 λ la resistenza vera e' piu' alta), oppure fissa. Radiali rialzati: fili veri nel modello, sopra il terreno di Sommerfeld; le perdite nel terreno stanno nel guadagno del NEC.

### 18.3 Fonti, somiglianze e differenze

| Fonte | Somiglianze | Differenze |
|---|---|---|
| AD5X via N0CU [C113] | Rr 3-4 Ω e circa -j600 Ω sui 160 m, 65 Ω sui 40 m; ROS 324:1 sui 160 m e 41:1 sugli 80 m con il 4:1 e Rg 10 Ω; circa 13 kV alla base a 1500 W sui 160 m; guadagni e angoli su terreno reale | AD5X usa Rg fisso; YA-NEC calcola Rg dai radiali e il diagramma con Sommerfeld, e aggiunge la perdita dell'accordatore |
| ON4UN [C18], W8JI [C82] | la verticale corta vive dei radiali; l'accordatore deve stare alla base | - |
| TF5NN [C89] | impedenza del monopolo per la stima immediata | sostituita dal NEC con «Calcola le bande (NEC)» |

