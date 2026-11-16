**YA-NEC 1.0.11** - simulatore d'antenne a fili basato sul NEC-2, a cura di Costantino IC8TEM.

> *La storia di questo programma la farete voi: con i vostri progetti, con le vostre idee, con la vostra genialità, con le vostre scoperte e con le vostre certezze.*

**Novita' 1.0.11:**
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

| Sistema | Installatore | Zip (senza installare) |
|---|---|---|
| Windows 10/11 64 bit | [YA-NEC-1.0.10-win-x64-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win-x64-setup.exe) | [YA-NEC-1.0.10-win-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win-x64.zip) |
| Windows 10/11 32 bit | [YA-NEC-1.0.10-win-x86-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win-x86-setup.exe) | [YA-NEC-1.0.10-win-x86.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win-x86.zip) |
| Windows su ARM (Snapdragon) | [YA-NEC-1.0.10-win-arm64-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win-arm64-setup.exe) | [YA-NEC-1.0.10-win-arm64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win-arm64.zip) |
| Windows 7 SP1, 8, 8.1 - 64 bit | [YA-NEC-1.0.10-win7-x64-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win7-x64-setup.exe) | [YA-NEC-1.0.10-win7-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win7-x64.zip) |
| Windows 7 SP1, 8, 8.1 - 32 bit | [YA-NEC-1.0.10-win7-x86-setup.exe](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win7-x86-setup.exe) | [YA-NEC-1.0.10-win7-x86.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-win7-x86.zip) |
| macOS Apple Silicon (M1-M4) | [YA-NEC-1.0.10-osx-arm64.dmg](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-osx-arm64.dmg) | [YA-NEC-1.0.10-osx-arm64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-osx-arm64.zip) |
| macOS Intel | [YA-NEC-1.0.10-osx-x64.dmg](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-osx-x64.dmg) | [YA-NEC-1.0.10-osx-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-osx-x64.zip) |
| Debian, Ubuntu, Mint (x86_64) | [ya-nec_1.0.10_amd64.deb](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/ya-nec_1.0.10_amd64.deb) | [YA-NEC-1.0.10-linux-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-linux-x64.zip) |
| Fedora, openSUSE (x86_64) | [ya-nec-1.0.10-1.x86_64.rpm](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/ya-nec-1.0.10-1.x86_64.rpm) | [YA-NEC-1.0.10-linux-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-linux-x64.zip) |
| Raspberry Pi OS 64 bit (e altri Linux ARM64) | [ya-nec_1.0.10_arm64.deb](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/ya-nec_1.0.10_arm64.deb) | [YA-NEC-1.0.10-linux-arm64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-linux-arm64.zip) |
| Raspberry Pi OS 32 bit (armv7l) | [ya-nec_1.0.10_armhf.deb](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/ya-nec_1.0.10_armhf.deb) | [YA-NEC-1.0.10-linux-arm.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-linux-arm.zip) |
| Linux ARM64 rpm (Fedora ARM...) | [ya-nec-1.0.10-1.aarch64.rpm](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/ya-nec-1.0.10-1.aarch64.rpm) | [YA-NEC-1.0.10-linux-arm64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-linux-arm64.zip) |
| Alpine / musl x64 | - | [YA-NEC-1.0.10-linux-musl-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-linux-musl-x64.zip) |
| Alpine / musl ARM64 | - | [YA-NEC-1.0.10-linux-musl-arm64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.10/YA-NEC-1.0.10-linux-musl-arm64.zip) |

**Installatori (nuovi dalla 1.0.7):** Windows `setup.exe` in `C:\YA-NEC` senza diritti di amministratore (menu Start, desktop, disinstallazione da App installate; impostazioni e modelli restano in `C:\YA-NEC\data`, l'aggiornamento automatico continua a funzionare); `.deb`/`.rpm` in `/opt/YA-NEC` con il comando `ya-nec` e la voce nel menu (si aggiorna con apt/dnf); `.dmg` per Mac: trascina la cartella YA-NEC in Applicazioni.
Il setup non e' firmato: Windows puo' mostrare "PC protetto", si clicca "Ulteriori informazioni" e "Esegui comunque". Sul Mac la prima volta: tasto destro su YA-NEC.app > Apri.

Si estrae lo zip in una cartella scrivibile e si avvia `YA-NEC` (Windows: `YA-NEC.exe`). Non serve installare .NET. Ogni zip ha accanto la sua impronta `.sha256` (elenco completo in fondo, sotto "Assets").
macOS: dopo l'estrazione `xattr -dr com.apple.quarantine <cartella>`. Windows 7: servono gli aggiornamenti KB2533623 e KB2999226.

Chi ha la 1.0 riceve la 1.0.4 da solo (aggiornamento automatico, attivo al riavvio; si spegne in Impostazioni > Aggiornamento).
Manuale completo in `doc/Manuale_d_uso.pdf` e nella linguetta Manuale. Licenza GPL-3.0.
