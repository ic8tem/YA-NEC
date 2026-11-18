**YA-NEC @VER@** - simulatore d'antenne a fili basato sul NEC-2, a cura di Costantino IC8TEM.

> *La storia di questo programma la farete voi: con i vostri progetti, con le vostre idee, con la vostra genialità, con le vostre scoperte e con le vostre certezze.*

**Novità della 1.3.1:**
- **Distanze di stack**: disegno dello stack visto da davanti accanto alla scheda DL6WU (palo, telaio H, antenne, quote, aperture a -3 dB); distanze provate a passi di 0,05 (passo modificabile); in tabella la riga dell'antenna singola, il F/R e la sua variazione; nuovo grafico "Rispetto alla singola antenna" con l'aumento di guadagno e la variazione del F/R su asse rovesciato (G/T con una casella); pulsanti "? Manuale" che aprono il paragrafo giusto del manuale;
- **Distribuzione** come gli altri programmi di IC8TEM: nuovi pacchetti Arch Linux (e Arch Linux ARM), Slackware (x86_64 e ARM64), AppImage (x86_64 e aarch64), archivi tar.gz per Linux, Raspberry Pi e Alpine, zip "-app" per il Mac; nomi dei file come nella tabella qui sotto. L'aggiornamento automatico passa da solo ai nomi nuovi.

**Novità della 1.3** (spiegate nel capitolo 24 del manuale e nella pagina [Novità della 1.3](https://github.com/ic8tem/YA-NEC/wiki/Novita-1.3) del wiki, con le fonti):
- **Antenne riceventi**: si accendono **Dipolo basso**, **K9AY** (terminazione con "ottimizza per F/B"), **4-square RX** e le schiere **Hi-Z** di 2, 3 e 8 verticali, con la fase tra le file e "ottimizza la fase (RDF)"; ogni voce crea il modello NEC e da' RDF, DMF, F/B e posto in graduatoria, con l'avviso sul collegamento a terra del NEC-2;
- **Ricezione (RDF)**: le 8 verticali in cerchio con la fase a 220 gradi e il lobo in avanti;
- **Videocorso**: lezioni 22-25 (Terreno reale, Antenne riceventi, Strumenti VHF, Calcoli meccanici e Carichi HF) in italiano e in inglese con la voce;
- **Lingue**: i testi non ancora tradotti si mostrano in inglese invece che in italiano; altre pagine della 1.2 tradotte in inglese;
- **Correzioni**: Carichi HF non si chiude piu' passando a "Bobina di carico"; gli avvisi delle antenne riceventi non si accumulano; le finestre larghe non escono piu' a destra.

**Dalla 1.2:** terreno reale alla HFTA, Ricezione (RDF), misure VNA, tolleranze, calcoli meccanici, cavo e balun, telaio a H, divisori, Yagi incrociate, carichi HF: vedi [Novità della 1.2](https://github.com/ic8tem/YA-NEC/wiki/Novita-1.2).

**Dalla 1.1:** cielo vero, distanze di stack con il NEC, Calcolatori a famiglie: vedi [Novità della 1.1](https://github.com/ic8tem/YA-NEC/wiki/Novita-1.1).

**Dalla 1.0.13:**
- inglese: tradotti altri 3049 testi dell'interfaccia (schede dei calcolatori, avvisi, fumetti, fonti): da circa 400 a circa 3460;
- tabella dei Fili: Ctrl+Z subito dopo la maniglia di riempimento annulla il riempimento (entra nella storia di "Annulla" della barra Geometria).

**Dalla 1.0.12:**
- Dipolo trappolato a singola banda: disegno piu' grande (senza il terreno) e quote che non si sovrappongono.

**Dalla 1.0.11:**
- Dipolo trappolato: nuovo modo "singola banda (bobina di carico)": scegli la lunghezza del braccio e la posizione della bobina in % del braccio, la scheda calcola la bobina; "Porta in risonanza (NEC)" ritocca la bobina;
- Delta loop: scheda riordinata in sezioni (Dimensioni, Forma, Balun / alimentazione), alimentazione a tessere con la figurina, disegno senza sovrapposizioni;
- tabelle di Fili, Sorgenti, Carichi e Linee: maniglia di riempimento come in Excel (trascina il quadratino blu per copiare il valore nelle celle vicine; Ctrl+Z annulla).

**Dalla 1.0.10:**
- Hexbeam: anche il 6 m (50 MHz) del modello di JN1GLB, a 2 o 3 elementi, sotto il 10 m; si puo' scegliere come banda del calcolo NEC;
- Moxon: profondita' E vincolata e "Fissa A ed E": il NEC ricalcola B, C e D (e A, se libera) in sottofondo per il F/B migliore con ROS sotto 1,5;
- Yagi multibanda e Dipolo multibanda: scheda in sezioni numerate (Radiatori, Trappole, Parassiti, Accordo), bande da 80 a 4 m su due righe (con l'8 m);
- tabella dei Fili: un conto di soli numeri in una cella (1.42+.26) diventa il risultato; barra Geometria: lista a comparsa delle variabili ("Variabili ▾").

**Dalla 1.0.9:**
- Fili: nuovo pulsante "Ins. elemento simmetrico": X sul boom, lunghezza totale, quota, raggio e segmenti, e il filo centrato (Y1 = -L/2, Y2 = +L/2, oppure verticale) va nella tabella; valgono anche variabili e formule;
- Ottimizzazione: dopo "Nuovo" le variabili con lo stesso nome del modello precedente non tengono piu' minimo e massimo vecchi (davano risultati assurdi);
- calcolo in tempo reale sopra 28 MHz: le distanze di stack DL6WU usano sempre le aperture nello spazio libero (sopra un terreno davano 60-77 m).

**Dalla 1.0.8:** due correzioni dell'interfaccia (motore di calcolo invariato):
- linguetta Correnti e colori delle correnti nella vista 3D: ora compaiono anche nei modelli con la correzione di Leeson dei tubi telescopici (prima "Nessun calcolo.");
- "Aggiungi filo" senza un modello aperto crea un modello nuovo e aggiunge il filo (prima chiedeva di aprire un modello).

**Dalla 1.0.7:** tre correzioni (motore di calcolo invariato):
- Segmentazione: un filo alimentato al centro con un numero pari di segmenti, corretto a un numero dispari, ha di nuovo la sorgente sul segmento centrale (prima poteva finire un segmento fuori centro);
- Loop magnetica: il modello NEC del calcolatore usa il materiale scelto (rame, alluminio, ottone) invece dei fili senza perdite, e il nuovo pulsante "Accorda il condensatore (NEC)" porta la loop in risonanza nel modello (prima la verifica dava efficienza 100 % e ROS oltre 500);
- Dipolo multibanda, manica aperta: l'accordo non avvicina piu' gli elementi sotto 3 diametri (almeno 2 cm), e un nuovo controllo (k7) avvisa se la distanza scritta e' piu' piccola: con tubi quasi a contatto il NEC-2 non e' affidabile.

**Dalla 1.0.6:** Cavi RG, il cambio d'impedenza segue la frequenza della scheda; tempo reale, angoli a -3 dB "omni" per le antenne omnidirezionali.

**Dalla 1.0.5:** installatori per Windows, Linux e Mac, icona del programma.

**Correzioni (1.0.1 - 1.0.4), solo interfaccia, motore di calcolo invariato:**
- modello nuovo: vista 3D e tabella Sorgenti compaiono anche prima del primo calcolo riuscito, cosi' si aggiunge la sorgente;
- "Nuovo" toglie dallo schermo struttura, risultati e sorgenti del modello precedente;
- "Aggiungi filo" ripetuto: le righe precedenti della tabella dei fili restano visibili;
- interfaccia in un'altra lingua: il menu Terreno (e gli altri menu tradotti) mostra la voce scelta e non resta fermo sulla prima;
- tabelle Sorgenti, Carichi, Linee, Trasformatori e Reti L: scrivendo nella riga "next" e cliccando subito "Applica e ricalcola", il primo clic non va piu' perso.
- Antenne riceventi: "ottimizza per F/B" funziona (prima si fermava con "Call from invalid thread");
- la EWE e' attribuita al suo autore, Floyd Koontz WA2WVL ("Is This EWE for You?", QST febbraio 1995), e non a K6SE (che resta l'autore di Flag e Pennant).

## Scarica la versione per il tuo sistema

| Sistema | Installatore | Zip / archivio (senza installare) |
|---|---|---|
| Windows 10/11 64 bit | [YA-NEC-@VER@-windows-x64-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows-x64-setup.exe) | [YA-NEC-@VER@-windows-x64-portatile.zip](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows-x64-portatile.zip) |
| Windows 10/11 32 bit | [YA-NEC-@VER@-windows-x86-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows-x86-setup.exe) | [YA-NEC-@VER@-windows-x86-portatile.zip](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows-x86-portatile.zip) |
| Windows su ARM (Snapdragon) | [YA-NEC-@VER@-windows-arm64-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows-arm64-setup.exe) | [YA-NEC-@VER@-windows-arm64-portatile.zip](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows-arm64-portatile.zip) |
| Windows 7 SP1, 8, 8.1 - 64 bit | [YA-NEC-@VER@-windows7-x64-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows7-x64-setup.exe) | [YA-NEC-@VER@-windows7-x64-portatile.zip](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows7-x64-portatile.zip) |
| Windows 7 SP1, 8, 8.1 - 32 bit | [YA-NEC-@VER@-windows7-x86-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows7-x86-setup.exe) | [YA-NEC-@VER@-windows7-x86-portatile.zip](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-windows7-x86-portatile.zip) |
| macOS Apple Silicon (M1-M4) | [YA-NEC-@VER@-mac-apple-silicon.dmg](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-mac-apple-silicon.dmg) | [YA-NEC-@VER@-mac-apple-silicon-app.zip](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-mac-apple-silicon-app.zip) |
| macOS Intel | [YA-NEC-@VER@-mac-intel.dmg](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-mac-intel.dmg) | [YA-NEC-@VER@-mac-intel-app.zip](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-mac-intel-app.zip) |
| Debian, Ubuntu, Mint (x86_64) | [ya-nec_@VER@_amd64.deb](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec_@VER@_amd64.deb) | [YA-NEC-@VER@-linux-x64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-x64.tar.gz) |
| Fedora, openSUSE (x86_64) | [ya-nec-@VER@.x86_64.rpm](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec-@VER@.x86_64.rpm) | [YA-NEC-@VER@-linux-x64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-x64.tar.gz) |
| Arch, Manjaro, EndeavourOS (x86_64) | [ya-nec-@VER@-1-x86_64.pkg.tar.zst](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec-@VER@-1-x86_64.pkg.tar.zst) | [YA-NEC-@VER@-linux-x64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-x64.tar.gz) |
| Slackware (x86_64) | [ya-nec-@VER@-x86_64-1_ic8tem.txz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec-@VER@-x86_64-1_ic8tem.txz) | [YA-NEC-@VER@-linux-x64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-x64.tar.gz) |
| Qualsiasi Linux x86_64 (AppImage) | [YA-NEC-@VER@-x86_64.AppImage](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-x86_64.AppImage) | [YA-NEC-@VER@-linux-x64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-x64.tar.gz) |
| Raspberry Pi OS 64 bit (e altri Linux ARM64) | [ya-nec_@VER@_arm64.deb](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec_@VER@_arm64.deb) | [YA-NEC-@VER@-raspberry-pi-64bit.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-raspberry-pi-64bit.tar.gz) |
| Raspberry Pi OS 32 bit (armv7l) | [ya-nec_@VER@_armhf.deb](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec_@VER@_armhf.deb) | [YA-NEC-@VER@-raspberry-pi-32bit.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-raspberry-pi-32bit.tar.gz) |
| Linux ARM64 rpm (Fedora ARM...) | [ya-nec-@VER@.aarch64.rpm](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec-@VER@.aarch64.rpm) | [YA-NEC-@VER@-linux-arm64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-arm64.tar.gz) |
| Linux ARMv7 rpm (armv7hl) | [ya-nec-@VER@.armv7hl.rpm](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec-@VER@.armv7hl.rpm) | [YA-NEC-@VER@-linux-armv7l.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-armv7l.tar.gz) |
| Arch Linux ARM (aarch64) | [ya-nec-@VER@-1-aarch64.pkg.tar.zst](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec-@VER@-1-aarch64.pkg.tar.zst) | [YA-NEC-@VER@-linux-arm64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-arm64.tar.gz) |
| Slackware ARM64 | [ya-nec-@VER@-aarch64-1_ic8tem.txz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/ya-nec-@VER@-aarch64-1_ic8tem.txz) | [YA-NEC-@VER@-linux-arm64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-arm64.tar.gz) |
| Qualsiasi Linux ARM64 (AppImage) | [YA-NEC-@VER@-aarch64.AppImage](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-aarch64.AppImage) | [YA-NEC-@VER@-linux-arm64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-linux-arm64.tar.gz) |
| Alpine / musl x64 | - | [YA-NEC-@VER@-alpine-x64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-alpine-x64.tar.gz) |
| Alpine / musl ARM64 | - | [YA-NEC-@VER@-alpine-arm64.tar.gz](https://github.com/ic8tem/YA-NEC/releases/download/v@VER@/YA-NEC-@VER@-alpine-arm64.tar.gz) |

**Installatori:** Windows `setup.exe` in `C:\YA-NEC` senza diritti di amministratore (menu Start, desktop, disinstallazione da App installate; impostazioni e modelli restano in `C:\YA-NEC\data`, l'aggiornamento automatico continua a funzionare); `.deb`, `.rpm`, Arch (`sudo pacman -U ./ya-nec-...pkg.tar.zst`) e Slackware (da root `installpkg ya-nec-...txz`) in `/opt/YA-NEC` con il comando `ya-nec` e la voce nel menu (si aggiornano con il gestore dei pacchetti); AppImage: si rende eseguibile e si avvia, al primo avvio copia il programma in `~/YA-NEC` (si aggiorna scaricando il nuovo AppImage); `.dmg` per Mac: trascina la cartella YA-NEC in Applicazioni.
Il setup non e' firmato: Windows puo' mostrare "PC protetto", si clicca "Ulteriori informazioni" e "Esegui comunque". Sul Mac la prima volta: tasto destro su YA-NEC.app > Apri.

Zip e tar.gz: si estraggono in una cartella scrivibile e si avvia `YA-NEC` (Windows: `YA-NEC.exe`); il tar.gz per Linux si puo' anche installare in `~/YA-NEC` con `./installa.sh` (voce nel menu). Alpine: serve `apk add icu-libs fontconfig`. Mac: lo zip `-app` contiene la cartella YA-NEC con YA-NEC.app, da estrarre dove si vuole. Non serve installare .NET. Ogni file ha accanto la sua impronta `.sha256` (elenco completo in fondo, sotto "Assets").
Gli zip `YA-NEC-@VER@-<piattaforma>.zip` con i nomi vecchi (win-x64, linux-x64, osx-arm64...) servono solo all'aggiornamento automatico delle versioni fino alla 1.3.0.
macOS: dopo l'estrazione `xattr -dr com.apple.quarantine <cartella>`. Windows 7: servono gli aggiornamenti KB2533623 e KB2999226.

Chi ha una versione precedente riceve la @VER@ da solo (aggiornamento automatico, attivo al riavvio; si spegne in Impostazioni > Aggiornamento).
Manuale completo in `doc/Manuale_d_uso.pdf` e nella linguetta Manuale. Licenza GPL-3.0.
