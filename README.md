**Italiano** · [English](README.en.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md)

# YA-NEC

**Simulatore d'antenne a fili basato sul NEC-2**, per Windows, Linux e macOS (C#, .NET 8, Avalonia).
A cura di Costantino Cerrotta **IC8TEM**.

> *La storia di questo programma la farete voi: con i vostri progetti, con le vostre idee, con la vostra genialità, con le vostre scoperte e con le vostre certezze.*

Il motore è la traduzione in C# di **nec2c** (NEC-2 del Lawrence Livermore National Laboratory), confrontata a ogni collaudo con nec2c ed EZNEC. Apre i modelli **NEC (.nec)**, **MMANA (.maa)**, **AutoEZ (.weq)**, **EZNEC (.ez)**, **YO/AO di K6STI (.yag, .ant)** e il formato proprio **.yanec**.

Cosa c'è: impedenza, ROS, guadagno, diagrammi 2D e 3D, sweep in frequenza e carta di Smith; correzione di Leeson dei tubi telescopici come EZNEC; terreni (perfetto, coefficiente di riflessione, Sommerfeld, radiali); variabili, formule e ottimizzatore; calcolatori (Yagi DL6WU, Quagi, LPDA, Moxon, hexbeam, dipoli e Yagi multibanda, four-square, verticali, parabole e illuminatori, staffe degli elementi e molto altro); antenne riceventi e RDF; rumore, G/T e stack; abaco dei cavi, linee e balun; controllo dei dati inseriti. Ogni scelta di calcolo e ogni casella sono spiegate nel [manuale](doc/Manuale_d_uso.pdf), che è anche dentro il programma (linguetta Manuale). Altre informazioni nel [Wiki](https://github.com/ic8tem/YA-NEC/wiki).

## Immagini del programma

<table><tr><td width="50%" align="center"><a href="docs/screenshots/struttura-3d.png"><img src="docs/screenshots/struttura-3d.png" alt="Struttura 3D con le correnti e il lobo"></a><br><sub>Struttura 3D con le correnti e il lobo</sub></td><td width="50%" align="center"><a href="docs/screenshots/terreno-reale.png"><img src="docs/screenshots/terreno-reale.png" alt="Terreno reale alla HFTA sul profilo DEM"></a><br><sub>Terreno reale alla HFTA sul profilo DEM</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/ricezione-rdf.png"><img src="docs/screenshots/ricezione-rdf.png" alt="Ricezione: RDF, DMF e rumore"></a><br><sub>Ricezione: RDF, DMF e rumore</sub></td><td width="50%" align="center"><a href="docs/screenshots/tolleranze.png"><img src="docs/screenshots/tolleranze.png" alt="Tolleranze costruttive (Monte Carlo)"></a><br><sub>Tolleranze costruttive (Monte Carlo)</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/cavo-balun.png"><img src="docs/screenshots/cavo-balun.png" alt="Cavo e balun: corrente di modo comune"></a><br><sub>Cavo e balun: corrente di modo comune</sub></td><td width="50%" align="center"><a href="docs/screenshots/yagi-incrociate.png"><img src="docs/screenshots/yagi-incrociate.png" alt="Yagi incrociate: RHCP, LHCP, rapporto assiale"></a><br><sub>Yagi incrociate: RHCP, LHCP, rapporto assiale</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/vento.png"><img src="docs/screenshots/vento.png" alt="Calcoli meccanici: vento (NTC 2018)"></a><br><sub>Calcoli meccanici: vento (NTC 2018)</sub></td><td width="50%" align="center"><a href="docs/screenshots/divisori.png"><img src="docs/screenshots/divisori.png" alt="Divisori e cavi di fase"></a><br><sub>Divisori e cavi di fase</sub></td></tr><tr><td width="50%" align="center"><a href="docs/screenshots/carichi-hf.png"><img src="docs/screenshots/carichi-hf.png" alt="Carichi HF: trappole e bobine"></a><br><sub>Carichi HF: trappole e bobine</sub></td><td width="50%" align="center"><a href="docs/screenshots/smith.png"><img src="docs/screenshots/smith.png" alt="Carta di Smith"></a><br><sub>Carta di Smith</sub></td></tr></table>

## Novità della 1.7

- **Nuovi calcolatori**: Yagi HF 2-3 elementi con la guida di taratura, Yagi bibanda interlacciata, verticale trappolata multibanda, L invertita / T per i 160 m, half-sloper sul traliccio, dipolo a banda larga, dipolo multibanda a relè, carico lineare, filo risonante in armonica, gamma match e adattamento a stub con la carta di Smith.
- **Quad multibanda** con le linee delle bande non alimentate e le linee di disaccoppiamento; **loop magnetica** rettangolare, a spire in parallelo, con trasformatore e rete L; **Filari** con il materiale del filo e l'FT-82-43.
- **Stack** con l'area efficace; **ROS** che mostra da che lato sta R; **Terreno reale** con gli angoli di partenza verso un locatore; efficienza nei calcolatori con bobine e trappole; nuova linguetta **Consigli**.

## Novità della 1.6

- **Calcolatori › Suggerimenti**: dalla frequenza, l'altezza, il terreno e l'ingombro, le antenne possibili calcolate con il NEC e ordinate per guadagno, uniformità, angolo basso o NVIS; «apri» porta nel calcolatore.
- **Ottimizzatore**: **adattamento diretto a Z0** di una Yagi (radiatore e primo direttore, senza adattatore) e metodo **evoluzione differenziale** che cerca in tutto l'intervallo delle variabili.
- **Fissa questo risultato** in Diagrammi, Frequenza e Smith (fino a 4 curve tratteggiate per il confronto); Struttura 3D con le **correnti colorate per fase**.
- **Cavo e balun**: la calza fino alla stazione (quota e direzione del tratto), corrente in stazione e potenza irradiata dal cavo.
- **Importatore EZNEC**: carichi di Laplace, stub, linee con perdite e reti L; **superfici SP/SM/SC** del NEC-2, correnti animate, controlli dei dati in ingresso; licenza **GPL-3.0-or-later**.

## Novità della 1.5.8

- **Calcolatori > HF > Filari**: pulsante **Abaco ›** con le tabelle calcolate con il NEC (EFHW 49:1 e 64:1 con e senza contrappeso, 9:1 con il contrappeso migliore, terreno medio o perfetto); una riga si copia nella scheda e il calcolo resta libero. Contrappeso «nessuno» = calza del coassiale fino alla radio.
- **Ottimizzatore**: **maschera VHF ROS + G/T**, che ottimizza il G/T dello stack 2 x 2 a 30 gradi (distanze DL6WU, temperature di VE7BQH, preamplificatore 20 dB, 2 dB di cavo) insieme al ROS.
- **Manuale di calcolo**: capitolo 16 «Filari alimentate in punta» e paragrafo 8.4 sul G/T dello stack; **videocorso**: lezione 32 «Maschera VHF ROS + G/T»; traduzioni dell'Ottimizzatore corrette.

## Novità della 1.5.7

- **Filari**: la bobina di carico è una scelta dell'EFHW (sì / no) e il pulsante **Accorda la bobina (NEC)** cerca l'induttanza giusta con contrappeso e terreno.
- **Traduzioni**: gli avvisi arancio di tutti i calcolatori ora si traducono; scheda Filari completa in inglese; scritte dei disegni e del calcolo in tempo reale tradotte.
- **Struttura 3D**: il filo isolato non si disegna più come una fila di bobine.

## Novità della 1.5.6

- **Calcolatori > HF > Filari** (nuova): antenne filari alimentate in punta. EFHW multibanda, monobanda e con bobina (49:1 o 64:1), filo non risonante con 9:1 o 4:1; sei disposizioni (inclinata dal basso o dall'alto, verticale, L e U rovesciata, tutto dritto); contrappeso 0,05 λ o calza del coassiale, condensatore, tensione in punta, ROS per banda con il modello TF5NN e con il NEC; **Accorda la lunghezza (NEC)**.
- **Pulsanti di rimando**: dai Filari e dalle altre schede filari (dipoli, OCF/Windom, T2FD, G5RV, Delta, Quad, Loop, Skywire, Moxon, Hexbeam) si apre già compilata la scheda del balun, del choke (Toroidi FT), del quarto d'onda (Cavi RG) o dei carichi, con «‹ Torna».
- **Discone**: passata dalla famiglia HF alla famiglia VHF.

## Novità della 1.5.5

- **Calcolatori > HEN**: la scheda indica la polarizzazione orizzontale, come la verifica NEC (prima diceva verticale).
- **Misure VNA**: titoli, legenda e proposta di correzione tradotti (prima restavano in italiano).
- **Videocorso**: 32 lezioni, con le nuove 30 «Cavi e misure col VNA» e 31 «Altri calcolatori».

## Novità della 1.5.4

- **Modello scritto a mano**: i fili applicati prima della sorgente danno un avviso arancione nella barra di stato, senza finestra; le finestre di errore vere prendono il fuoco (Invio o Esc le chiudono).
- **Traduzioni**: pagina Boom (dal rilievo al modello elettrico) tradotta in inglese.
- **Videocorso completo**: 30 lezioni girate con la 1.5.3-1.5.4, in italiano e in inglese, con la nuova lezione 29 «Ridimensiona e ricentra».

## Novità della 1.5.3

- **Ridimensiona e ricentra** (Variabili e ottimizzazione): un modello MMANA o un progetto portato alla frequenza voluta in un clic; diametri in scala, uguali o commerciali (tondini pieni 3-6 mm, tondini o tubi 8-40 mm); il ritocco del radiatore azzera la reattanza.
- **Diagrammi**: la prima voce si chiama **Lobi**, la linguetta Frequenza diventa **Parametri**; **passo degli angoli** adattato al computer; **Ricezione (RDF)** sul modello aperto.
- **Correzioni**: quad a 2-3 elementi in Automatico portata in risonanza dal NEC (Cebik vale per la spira da sola); un solo avviso per un modello senza sorgente; videocorso rigirato con la 1.5.3.

## Novità della 1.5.2

- **Linguette riordinate** (da 16 a 13): la carta di Smith nella fascia dei Diagrammi, **NEC/correnti**, **Variabili e ottimizzazione**; Distanze di stack VHF e HF + HFTA; nuovo ordine delle famiglie dei Calcolatori.
- **Struttura 3D**: linee, carichi e reti disegnati sui fili; la legenda del terreno si apre da **Mostra terreno** nella barra di stato; ogni errore in grassetto e in una finestra con OK. **Frequenza**: bande radioamatoriali sul fondo dei grafici.
- **Terreno reale**: stato del calcolo e quota del sito a mano. **Ottimizzatore**: **Misure al millimetro** (coordinate e diametri commerciali, verifica NEC, lista di taglio). Diagrammi, grafici e riquadri tradotti in 36 lingue.

## Novità della 1.5.1

- **Ottimizzatore**: «+ aggiungi variabile» crea una variabile dal modello (lunghezza o perimetro, posizione, raggio di ogni elemento, anche dei loop; quota dell'antenna), con l'anteprima in ocra.
- **Variabili**: a destra la struttura 3D mostra in ocra dove agisce la variabile scelta.
- Correzioni (loop capacitiva, stack senza distanze valide, Vento alla prima apertura) e traduzione inglese completata.

## Novità della 1.5

- **Calcolatori**: Calcoli meccanici dentro i Calcolatori; reattanza del dipolo corto, dipolo ripiegato in frequenza, loop magnetica a più spire e con accoppiamento capacitivo (Antenna Engineering Handbook, 4ª ed.). **Distanze di stack HF** in una voce a sé.
- **Modelli AutoEZ, MultiNEC, EZNEC**: sorgenti «mark out», fili virtuali, trasformatori, celle calcolate; il .nec salvato riporta le sorgenti di corrente come tensioni calcolate.

## Novità della 1.4.2

- **Calcolatori**: NVIS (altezza ottima e schermo a terra, da Cebik), Forcina (beta / hairpin), Quad con il metodo di Cebik e la risonanza NEC in VHF, J-pole con la Slim Jim.
- **Ottimizzatore**: Monte Carlo con le tolleranze di costruzione. **Stack**: distanze su due frequenze.
- **Impostazioni**: Forza l'aggiornamento, Segnala un problema.

## Novità della 1.4

- **Documenti tecnici**: manuale d'uso, nuovo manuale di calcolo e delle fonti (formule, ipotesi, limiti, fonti con somiglianze e differenze, casi di prova) e citazioni numerate, ognuno con indice e PDF; ricerca in tutti e tre.
- **Distanze di stack**: ROS attivo e guadagno realizzato; le distanze con ROS attivo alto sono segnate "da non scegliere".
- **Chiusura**: finestra di conferma con il ringraziamento.

## Novità della 1.3

- **Antenne riceventi**: nei Calcolatori si accendono **Dipolo basso**, **K9AY** (terminazione con "ottimizza per F/B"), **4-square RX** e le schiere **Hi-Z** di 2, 3 e 8 verticali, con la **fase tra le file** e il pulsante "ottimizza la fase (RDF)". Ogni voce crea il modello NEC e da' RDF, DMF, F/B e posto in graduatoria; un avviso giallo ricorda il limite del NEC-2 con i fili collegati al terreno reale.
- **Ricezione (RDF)**: le 8 verticali in cerchio hanno la fase a 220 gradi e il lobo in avanti.
- **Videocorso**: lezioni 22 (Terreno reale), 23 (Antenne riceventi), 24 (Strumenti VHF) e 25 (Calcoli meccanici e Carichi HF), in italiano e in inglese con la voce.
- **Lingue**: i testi non ancora tradotti si mostrano in inglese invece che in italiano; tradotti in inglese diagrammi polari, oggetti del modello, Cavo e balun, Telaio a H, Divisori e avvisi gialli.
- **Correzioni**: Carichi HF non si chiude piu' passando a "Bobina di carico"; il clic su un'altra voce con il cursore in un campo non chiude piu' il programma; gli avvisi delle antenne riceventi non si accumulano; le finestre larghe non escono piu' a destra.
- Tutto nel capitolo 24 del manuale, con le fonti (K9AY, Hi-Z Antennas, IV3PRK, ON4UN, W7EL).

## Novità della 1.2

- **Terreno reale** (alla HFTA di N6BV): diagramma di elevazione sul profilo vero del terreno, dal DEM **TINITALY** (Italia, 10 m) o **SRTM1** (mondo), da un file di HorizonProfiler o piatto. Riflessioni tratto per tratto pesate con le zone di Fresnel, diffrazione sui crinali, mare, terra curva; direzioni di traffico dal locatore (fino a 12 caratteri) e **mappa** con foto aerea, OpenStreetMap e griglia dei locatori. In VHF vista intera e zoom 0-15 gradi.
- **Ricezione (RDF)**: RDF, DMF, F/R medio, rumore d'antenna (ITU-R P.372) e margine sul ricevitore per Beverage, BOG, K9AY, Flag, EWE, 4-square e 8 verticali in cerchio.
- **Misure VNA**: file .s1p e CSV sovrapposti al calcolo, con il cavo tolto (de-embedding), carta di Smith e proposta di correzione.
- **Tolleranze costruttive**: Monte Carlo ripetibile, sensibilità per elemento e lista di taglio con la tolleranza ammessa.
- **Calcoli meccanici** (nuova linguetta): **vento** con NTC 2018, CNR-DT 207 ed EN 1991-1-4 (zona dal locatore, flusso trasversale) e **bilanciamento** sul palo; forze in N e kgf.
- **Cavo e balun** da 1,8 a 1296 MHz (la calza diventa un filo NEC), **telaio a H e palo** nello stack, **divisori** λ/4 e Wilkinson con disegno 1:1, **Yagi incrociate** (RHCP, LHCP, X-pol), **carichi HF** (trappole coassiali W8NX, bobine, linear loading), **F/R medio** anche nell'ottimizzatore e **test AGT** automatico.
- Tutto spiegato nel capitolo 23 del manuale, con le fonti; voci tradotte nelle 37 lingue.

## Novità della 1.1

> **Nota dell'autore.** In questa versione ho ragionato a lungo sulla temperatura di rumore. Mi sono stati segnalati alcuni studi in MATLAB sull'accoppiamento delle antenne direttive e ho voluto provarci subito: offrire a chi usa YA-NEC la possibilità di andare oltre la distanza di accoppiamento di DL6WU (la formula trigonometrica) e accompagnarlo, se lo desidera, nella scelta di una distanza di accoppiamento tutta sua. Insieme a questo, una profonda revisione dell'interfaccia. — *Costantino IC8TEM*

- **Rumore dal cielo vero.** Nuova pagina nella sopralinguetta **Rumore/GT/Stack**: il diagramma dell'antenna o dello stack viene messo sopra la mappa vera del cielo (survey a 408 MHz di Haslam, rielaborata da Remazeilles, portata a 144 e 432 MHz da G8ONH), dal proprio QTH (locatore o coordinate) e all'ora scelta, puntando dove si vuole, sulla Luna o sul punto di cielo più freddo. Si leggono due G/T affiancati: quello con il cielo uniforme, confrontabile con le tabelle di VE7BQH (30 gradi, stack 2 x 2 alla distanza DL6WU), e quello con il cielo vero, con la curva della temperatura d'antenna sulle 24 ore e il suo minimo e massimo. Il calcolo è quello di **skynoise di John C. Sager G8ONH** (licenza MIT), portato in C#.
- **Distanza di stack scelta col NEC.** Nella pagina **Distanze di stack** (spostata dai Calcolatori) lo stack si calcola davvero per una serie di distanze attorno a quella di DL6WU: guadagno, lobo laterale più alto, G/T e matrice di accoppiamento tra le porte (S11, S21...). Un criterio sceglie la distanza proposta (guadagno massimo, lobi più bassi, G/T migliore o guadagno con i lobi sotto una soglia) e un pulsante la usa per creare lo stack. In **HF** la formula di DL6WU non vale: lo stack sopra il terreno si sceglie sugli angoli di partenza voluti, con i diagrammi in elevazione in fase (BIP), da sole e in controfase (BOP).
- **Calcolatori a famiglie.** Una fascia sotto le linguette divide i calcolatori in **HF**, **VHF**, **Antenne riceventi** (che non è più una linguetta a parte), **Strumenti**, **Dischi e illuminatori** e **2,4 GHz**; il fascione a sinistra mostra solo le voci della famiglia scelta.
- **Misure sui diagrammi.** Oltre all'apertura a -3 dB: apertura al primo nullo (FNBW), lobo laterale più alto (SLL) e due cursori C1 e C2 da mettere con un clic, con livello, angolo e differenza tra i due.
- **F/B lungo l'asse** corretto per i modelli in spazio libero con il fascio lungo l'asse Z (come la Yagi del MathWorks Antenna Toolbox).
- **Nuovi calcolatori**: Biquad e doppia biquad (feed per parabole), Big Wheel e tre dipoli, Loop orizzontale multibanda (Skywire) con la perdita della linea su ogni banda.
- **Traduzioni**: tutte le voci nuove nelle 37 lingue del programma. **Videocorso** aggiornato con le lezioni sui nuovi argomenti.

## Scaricare

Dalla pagina **[Releases](https://github.com/ic8tem/YA-NEC/releases)** si scarica lo zip della propria piattaforma, lo si estrae in una **cartella scrivibile** (per esempio `C:\YA-NEC` o `~/YA-NEC`) e si avvia `YA-NEC.exe` (Linux e macOS: `YA-NEC`). Non serve installare .NET. Le impostazioni e i dati restano nella cartella `data` accanto al programma: niente AppData, niente registro di sistema.

| Sistema | Pacchetto |
|---|---|
| Windows 10/11 a 64 bit | `YA-NEC-<versione>-windows-x64-portatile.zip` |
| Windows 10/11 a 32 bit | `YA-NEC-<versione>-windows-x86-portatile.zip` |
| Windows su ARM (Snapdragon) | `YA-NEC-<versione>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 (.NET 6) | `YA-NEC-<versione>-windows7-x64-portatile.zip`, `-windows7-x86-portatile.zip` (servono gli aggiornamenti KB2533623 e KB2999226) |
| Linux x86_64 (Debian, Ubuntu, Mint, Fedora, openSUSE, Arch...) | `YA-NEC-<versione>-linux-x64.tar.gz` |
| Linux ARM64 (Raspberry Pi OS 64 bit, altri aarch64) | `YA-NEC-<versione>-linux-arm64.tar.gz` |
| Linux ARM 32 bit (Raspberry Pi OS 32 bit, armv7l) | `YA-NEC-<versione>-linux-armv7l.tar.gz` |
| Alpine e altri Linux con musl | `YA-NEC-<versione>-alpine-x64.tar.gz`, `-alpine-arm64.tar.gz` |
| macOS Apple Silicon (M1-M4) | `YA-NEC-<versione>-mac-apple-silicon-app.zip` |
| macOS Intel | `YA-NEC-<versione>-mac-intel-app.zip` |

Ogni zip ha accanto la sua impronta `.sha256`.

- **Linux**: dopo l'estrazione `chmod +x YA-NEC` se serve; occorrono `libfontconfig1` e `libicu` (già presenti nelle distribuzioni con desktop).
- **macOS**: il programma non è firmato da Apple. Dopo l'estrazione, nel Terminale: `xattr -dr com.apple.quarantine ~/YA-NEC` (la cartella estratta), poi `~/YA-NEC/YA-NEC`.

## Aggiornamento automatico

Il programma controlla 10 secondi dopo l'avvio e poi ogni 3 minuti se qui c'è una versione nuova. Se c'è, scarica lo zip della sua piattaforma, ne verifica l'impronta SHA-256 e lo installa: la versione nuova entra in funzione **al prossimo avvio**, una finestra lo avvisa e il programma si riavvia da solo dopo 10 secondi, riaprendo il modello. La versione precedente resta in `data/aggiornamenti/precedente-<versione>`; le impostazioni (`data/config.ini`) e i modelli dell'utente non si toccano. Senza impronta, o con un'impronta diversa, non si installa niente. L'aggiornamento si spegne in **Impostazioni > Aggiornamento**.

**Eccezione: pacchetti Debian/Ubuntu (.deb) e Fedora/openSUSE (.rpm).** Quando ci saranno, si installeranno nelle cartelle di sistema con i permessi di amministratore: il programma non può (e non deve) cambiarsi da solo, quindi si aggiorneranno con il gestore dei pacchetti (`sudo apt update && sudo apt upgrade`, `sudo dnf upgrade`, `sudo zypper update`). In quei pacchetti l'aggiornamento automatico è spento e le Impostazioni lo dicono.

## Compilare dal sorgente

Serve il .NET 8 SDK. Windows: `compila.bat` (programma in `build\win-x64`), `compila.bat /distribuzione` per la versione autonoma, `compila_win7.bat` per Windows 7. Linux e macOS: `./compila.sh`. Dettagli in [LEGGIMI.txt](LEGGIMI.txt).

## Crediti e licenza

**GPL-3.0-or-later** (file [LICENSE](LICENSE); note di terze parti in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)), come nec2c e xnec2c da cui deriva il motore. YA-NEC esiste grazie al lavoro di colleghi radioamatori e ricercatori: l'elenco completo è in [CREDITI.txt](CREDITI.txt) e nella linguetta **Crediti** del programma. Un ringraziamento a Luis IV3PRK per le pubblicazioni gratuite sul suo sito.

## Contatti

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [gruppo Facebook](https://www.facebook.com/groups/1675310160982493) · donazioni: [ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)

