
## 1.3.0 – Antenne riceventi: avvisi che si accumulavano (18/11/2026)
- Dopo "Crea il modello" e poi "ottimizza la fase", l'avviso giallo "RDF ... contro ... della fonte" compariva due volte (uno per calcolo).
- Ora gli avvisi legati al risultato (RDF contro la fonte, BOG con Z aperto/corto, modello approssimato del BOG) hanno l'etichetta "ris" e si tolgono all'inizio di ogni calcolo (`Ris()` in MainWindow.Oggetti.cs).

## 1.3.0 – Traduzioni dei diagrammi polari e della pagina delle antenne riceventi (18/11/2026)
- PolarPlot: titolo, nomi delle tracce, riga FNBW/lobo laterale e scala ora passano da Lang.T/Lang.F (prima restavano in italiano in tutte le lingue).
- Antenne riceventi: "azimut a {0}°" composto con Lang.F; aggiunte in en.txt le voci "Cos'e' l'RDF" e il suo testo, elevazione, azimut, totale, scala lineare, FNBW.
- Barra in alto: "Didascalie {0} %" composto con Lang.F; "ROS" diventa "SWR" in inglese.
- Antenne riceventi: avviso "RDF ... contro ... della fonte" composto con Lang.F; il nome del modello creato si traduce (es. "4-square for receiving.nec").
- Ricezione (RDF): titolo "Azimut a {0}° · {1} MHz" e famiglia "RUMORE" tradotti; Calcolatori semplici: "Azimut a {0}° di elevazione".
- Oggetti del modello: etichette con il conteggio (Sorgenti 1, Carichi 0...) tradotte; "Apri la scheda {0}" e "caso mostrato: {0}" con Lang.F; AvvisoGiallo passa il testo da Lang.T (gli avvisi gialli delle schede 1.2 ora si traducono); voci inglesi per Cavo e balun e Telaio a H.

## 1.3.0 – Carichi HF: crash passando da Trappola coassiale a Bobina di carico (18/11/2026)
- Trovato registrando il videocorso: "The control TextBox already has a visual parent WrapPanel". I campi comuni (Blocca, dipolo con, Filo Ø, pulsante) restavano figli delle righe vecchie dopo datiCorpo.Children.Clear().
- Correzione: rigaW stacca ogni controllo dal pannello precedente prima di aggiungerlo alla riga nuova.

## 1.3.0 – Uscita (18/11/2026)
- VERSION 1.3.0; manuale: 24.2 con le correzioni della notte e 24.2a Videocorso; PDF rigenerato (111 pagine).
- .github/release.md con le novita' della 1.3; README in 8 lingue con la sezione 1.3; wiki Novita-1.3 in 37 lingue, Home e barra laterale.

## 1.3.1 – Distribuzione armonizzata con l'elenco di YA-MS (18/11/2026, richiesta di IC8TEM delle 10:57)
- Nomi dei pacchetti come YA-MS: -windows-x64/x86/arm64-setup.exe e -portatile.zip, -windows7-x64/x86-, -mac-apple-silicon.dmg / -mac-intel.dmg con lo zip -app (cartella YA-NEC con YA-NEC.app), ya-nec_<v>_amd64/arm64/armhf.deb, ya-nec-<v>.x86_64/aarch64/armv7hl.rpm (senza il "-1"), -linux-x64/-linux-arm64/-linux-armv7l.tar.gz, -raspberry-pi-64bit/32bit.tar.gz, -alpine-x64/arm64.tar.gz.
- Nuovi pacchetti: Arch / Manjaro / EndeavourOS e Arch Linux ARM (.pkg.tar.zst), Slackware x86_64 e ARM64 (.txz), AppImage x86_64 e aarch64 (copia il programma in ~/YA-NEC al primo avvio). Script in installer/linux (make-arch.sh, make-txz.sh, make-appimage.sh, installa.sh per il tar.gz), ricavati da quelli di YA-MS; Arch e Slackware dal .deb, con /opt/YA-NEC/data scrivibile come nel .deb.
- Workflow: installatori Linux su due runner (x86_64; ARM64 per l'AppImage aarch64); il .dmg produce anche lo zip -app; .sha256 per ogni file. Gli zip YA-NEC-<v>-<rid>.zip con i nomi vecchi restano nella release: li cercano le versioni fino alla 1.3.0 per l'aggiornamento automatico.
- Aggiornamento automatico (src/Diag/Aggiornamento.cs): cerca i nomi nuovi (portatile.zip su Windows, tar.gz su Linux, estratto con System.Formats.Tar che conserva i permessi); sul Mac non sostituisce piu' i file (il pacchetto e' YA-NEC.app) e avvisa che c'e' il nuovo .dmg; Arch, Slackware e AppImage contano come pacchetti (file installato-da-pacchetto).
- winget, Scoop e Chocolatey (installer/gestori/genera.py) e Inno Setup con i nomi nuovi; tabella dei download in .github/release.md con le 21 righe dell'elenco; nomi nuovi nella pagina Installazione del wiki (37 lingue) e nei README (comando tar per Linux, open YA-NEC.app per il Mac).
- Non ancora compilato ne' pubblicato (si fa quando lo dice IC8TEM).

## 1.3.1 – Distanze di stack: disegno, passi di 0,05, grafico rispetto alla singola, pulsanti del manuale (18/11/2026, bozzetto approvato da IC8TEM alle 13:04)
- Scheda Distanze di stack DL6WU: a destra il disegno dello stack visto da davanti (palo, telaio H, antenne con elemento e boom, quote in m e lambda, aperture a -3 dB tratteggiate; polarizzazione dal modello); la scheda "Stack da creare" diventa "Antenne" (righe x colonne), cosi' le schede sono piu' strette. CalcDrawing: cerchio tratteggiato.
- Accoppiamento e distanza migliore (NEC): passo delle distanze scelto con una casella, di partenza 0,05 (prima fisso 0,1); StackStudio.Vhf calcola anche il F/R medio della singola e di ogni distanza.
- Tabella delle distanze: prima riga con l'antenna singola, colonne F/R e ΔF/R, segno "F/R max". Nuovo grafico "Rispetto alla singola antenna": aumento di guadagno da 0 in su (con le N antenne ideali), variazione del F/R su asse rovesciato con lo 0 in alto; G/T solo con la casella.
- Pulsante "? Manuale" sulla scheda DL6WU e sui riquadri Accoppiamento, Matrice e Distanze provate: apre il manuale al paragrafo "Distanze di stack: accoppiamento e distanza migliore" (i titoli #### ora si impaginano come paragrafi).
- Prova con nec2c (10 el. BVO IC8TEM, 2 x 2, 0,5-2,0 a passi di 0,05): guadagno, |Sij| e lobi coincidono con la tabella di YA-NEC 1.3.0 entro 0,1-0,3 dB.
