
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

## 1.4.0 – Distanze di stack: ROS attivo e righe "da non scegliere" (18/11/2026, richieste di IC8TEM delle 14:40-14:47)
- StackStudio.RosAttivo: per ogni distanza Γatt,i = somma della riga i della matrice S (alimentazione uguale e in fase), ROS attivo della porta peggiore, perdita per riflessione e G realizzato; per la singola dalla Zin.
- Tabella: colonne "ROS attivo" e "G realizzato"; segni "riflessi minimi" e "ROS attivo alto: da non scegliere". Le righe escluse non entrano nella scelta proposta (se lo sono tutte, si sceglie fra tutte).
- Casella "ROS attivo massimo" (di partenza 1,5); distanze di partenza da 0,5 a 2,5 x DL6WU (prima 2,0).
- Manuale, cap. 16: ROS attivo, G realizzato e la spiegazione delle aree di cattura di DL6WU (Ae = Gλ²/4π contro Dv x Dh, lobi di griglia a sinθ = λ/d).

## 1.4.0 – Finestra di chiusura come LIVETROPOVHF (18/11/2026, richiesta di IC8TEM delle 14:47)
- Nuovo MainWindow.Chiusura.cs: icona, "Vuoi chiudere YA-NEC?", "Grazie per avermi usato: spero di esserti stato utile." in corsivo, barra grigia con Sì e No (Invio = Sì, Esc = No), come AskClose di LIVETROPOVHF.
- Si apre alla chiusura quando il modello e' salvato; con modifiche non salvate resta la finestra "Modello non salvato". Saltata nelle prove automatiche. Voci inglesi in en.txt.

## 1.4.0 – Documenti tecnici (18/11/2026, bozzetto approvato da IC8TEM alle 14:54)
- La linguetta Manuale diventa **Documenti tecnici**: tre pillole (Manuale d'uso, Manuale di calcolo e delle fonti, Citazioni), ognuna con il suo indice; casella Cerca su tutti e tre i documenti (risultati cliccabili, fino a 60); Apri il PDF del documento scelto (quello in data/manuale se c'e', altrimenti doc/ su GitHub). I pulsanti "? Manuale" della 1.3.1 aprono il manuale d'uso al paragrafo.
- MainWindow.Manual.cs: BuildDocumento generico; nuove righe "> FORMULA:"; rimandi [C12] cliccabili verso le Citazioni; rimandi [[Calcolo: titolo]] / [[Uso: titolo]] cliccabili tra i documenti; le celle della prima colonna delle tabelle hanno il Tag (si arriva alla riga della citazione, evidenziata per 2 secondi).
- Nuovo data/manuale/Calcolo.md (15 capitoli: motore, segmentazione e AGT, terreni, Leeson, Yagi e quad, dipoli e verticali, RDF, rumore e G/T, stack VHF/UHF, stack HF, tolleranze, vento, divisori e balun, carichi HF, parabole). Il capitolo 15 Parabole e illuminatori e' in piu' rispetto al bozzetto.
- Nuovo data/manuale/Citazioni.md: bibliografia unica numerata [C..] in libri, articoli, norme, programmi, siti, fonti scartate (con il motivo), con le pagine usate e il posto in YA-NEC; i numeri non cambiano tra le versioni.
- Manuale d'uso: spostate nel manuale di calcolo le sezioni MININEC e NEC-2, Modelli tipicamente critici, Radiali a terra, Tubi telescopici (Leeson), Parabola: efficienza, le tabelle di calcolo e delle fonti del Rumore dal cielo vero, matrice di accoppiamento, ROS attivo, aree di cattura ed esempi dello stack; al loro posto un rimando cliccabile. Le tabelle dei crediti 1.2 e 1.3 rimandano alle Citazioni.
- Tre PDF separati in doc/: Manuale_d_uso.pdf (102 pagine), Manuale_di_calcolo_e_delle_fonti.pdf (23), Citazioni.pdf (7). Generatore md2html.py con #### , FORMULA e rimandi.
- Voci inglesi in en.txt; prove dell'interfaccia sulla linguetta "Documenti tecnici".

## 1.4.0 – Uscita (18/11/2026, "una sola versione 1.4.0 ... vai diretto con tutte le novita'" di IC8TEM alle 15:09)
- VERSION 1.4.0; novita' in .github/release.md; PDF dei tre documenti rigenerati con la versione 1.4.0.

## 1.4.1 – Aggiornamento ogni 5 minuti e finestra "Riavvia adesso" (18/11/2026, richiesta di IC8TEM delle 16:18: "ogni 5min deve controllare. poi con un popup avvisa")
- Dai registri dei due PC: l'aggiornamento funzionava, ma il primo controllo partiva 2 minuti dopo l'avvio e poi ogni 30; con aperture di pochi secondi (1.0.15: 18 s, 79 s, 7 s, 6 s) non partiva mai.
- Aggiornamento.cs: primo controllo 10 secondi dopo l'avvio, poi ogni 5 minuti (IntervalloMinuti 5); evento Pronta(versione, installata), una volta per versione; Rilascia() libera il timer e il mutex prima del riavvio.
- MainWindow.Chiusura.cs: finestra "È pronta la versione X di YA-NEC" con la grafica della finestra di chiusura: Riavvia adesso / Più tardi (Mac: Apri la pagina delle versioni / Più tardi). Riavvia: chiede del modello non salvato, avvia di nuovo il programma e chiude senza la conferma di chiusura.
- Testi aggiornati (Impostazioni, manuale cap. 21, README), voci inglesi.

## 1.4.1 – Ogni 3 minuti e riavvio da solo (18/11/2026, IC8TEM alle 16:27: "aggiornamento ogni 3 minuti, con popup di avviso. l'utente non potra' interagire"; alla domanda: "Solo avviso, poi riavvio")
- IntervalloMinuti 3 (primo controllo dopo 10 s).
- La finestra non ha piu' pulsanti ne' la x: conta 10 secondi con una barra e riavvia YA-NEC (sul Mac avvisa del .dmg e si chiude dopo 20 s). Sostituisce Riavvia adesso / Piu' tardi della voce precedente.
- Prima del riavvio: il modello salvato si annota in data/aggiornamenti/riapri.txt; quello non salvato si copia in data/aggiornamenti/"modello non salvato <data>.nec". All'avvio successivo RiapriDopoRiavvio lo riapre (il non salvato resta da salvare, con il nome di prima).
- Testi aggiornati (Impostazioni, manuale, README, voci inglesi).

## 1.4.1 – Uscita (18/11/2026, "pubblica" di IC8TEM alle 18:34)
- VERSION 1.4.1; novita' in .github/release.md e nel capitolo 25 del manuale; PDF rigenerati.

## 1.4.2 – Impostazioni: perche' alcune caselle sono grigie (18/11/2026, IC8TEM alle 18:55: "mica ho capito perche' alcune di queste opzioni sono grigie. serve scriverlo nei manuali")
- Manuale, capitolo 21 "Impostazioni: ogni casella": spiegati i tre casi (grigia e spuntata = sempre attiva; grigia non spuntata = non ancora disponibile; pillole grigie = scelta fissa) e rifatta la tabella, voce per voce, con dove la funzione si trova davvero.
- Impostazioni: il fumetto di ogni casella grigia comincia con il suo caso; corretti i fumetti rimasti a "prossima versione" per funzioni gia' presenti (controlli geometrici, AGT, verifica della segmentazione, impedenza attiva, mare lontano nel Terreno reale, perdite dei cavi); la nota verde in fondo spiega i tre casi. Tre caselle non disponibili ora appaiono non spuntate (proponi sorgenti a corrente, avviso lobi dello stack, confronto NEC / fattore di schiera). Solo testi, nessun cambiamento di impaginazione.

## 1.4.2 (uscita il 18/11/2026; i lavori approvati per la 1.5 restano: OCR delle tabelle, Confronto, Terreno reale 2 con OpenHFTA, secondo mezzo di terreno, loop a più spire)

- 18/11/2026 19:30: approvato il bozzetto «Calcolatori 1.5» (doc/bozzetti_approvati/Calcolatori_1.5_18-11-2026.html): loop a più spire (l1), metodo della Quad (q1), Slim Jim (j2), forcina (y1), NVIS (n1, n2).
- Quad (q1, q2): nuova scelta «Metodo»: Automatico (Cebik fino a 30 MHz, sopra il motore NEC cerca la risonanza partendo da Cebik, come chiesto da IC8TEM: «solo per HF, per VHF usi altro metodo»), ARRL (1005/f), Cebik, NEC. Formula di Cebik dal programma GW-Basic di Cubical Quad Notes vol. 2, cap. 2; riflettore e direttore nei rapporti ARRL. Ricerca NEC in sottofondo come la Moxon con E vincolata. Nota di Cebik (vol. 3) sulle quad oltre 4 elementi nelle formule.
- Casi prova verificati con nec2c prima di scriverli: Moxon 2 m di Cebik 1/4" e 1/2" (Z, guadagno e F/B come la sua Tab. 2; quella da 1" esce dal campo affidabile del NEC-2 e resta solo come nota); J-pole standard (2,45 dBi contro 2,46 di Cebik dopo l'AGT) e non standard (2,57 contro 2,56); regressione della quad uguale alla Tab. 2-2 di Cebik (1,0542 λ a 14,1 MHz con filo #12).
- 18/11/2026 19:35: approvato il bozzetto «Aggiornamento a richiesta, segnalazione dei problemi, stack su due frequenze» (doc/bozzetti_approvati/Aggiornamento_segnalazioni_stack2f_18-11-2026.html). IC8TEM 19:35: approvato anche il metodo Automatico della Quad.
- Impostazioni > Aggiornamento: «Controlla adesso» diventa «Forza l'aggiornamento» (funziona anche con l'automatico spento; sui pacchetti .deb/.rpm rimanda al gestore dei pacchetti) con la casella «Reinstalla la versione attuale»; se GitHub non risponde scrive che riprova da solo fra 3 minuti.
- IC8TEM 19:36, «solo tuo autotest»: i casi prova di Cebik e K1FO restano nelle mie verifiche con nec2c, non entrano nel collaudo del programma.
- J-pole (j2): scelta «Tipo» J-pole / Slim Jim (filo ripiegato fino a 0,012 λ sopra lo stub; con nec2c a 145 MHz la presa va a circa 50 mm: 48 + j7 Ω); nota di Cebik sul guadagno (circa 2,5 dBi, la Slim Jim non guadagna di più).
- Nuovo calcolatore Strumenti > Forcina (beta / hairpin, y1): Xc = √(R(Z0−R)), XL = Z0·√(R/(Z0−R)), Z0 della forcina 120·arccosh(s/d), lunghezza come linea in corto, stima dell'accorciamento del pilotato.
- Nuovo calcolatore HF > NVIS (n1, n2): dipolo, V invertito, loop di 1 λ; altezza 0,175 λ (o scansione NEC 0,10–0,25 λ col pulsante «Altezza ottima»); schermo 1 λ × 1 λ, 9 fili o filo riflettore a 0,001 λ; quattro terreni. Verifica con nec2c a 3,6 MHz, dipolo su terreno medio: massimo allo zenit a 0,175 λ (6,57 dBi; 1,23 dBi a 0,05 λ); schermo +1,9 dB su terreno molto povero e +0,2 dB su terreno molto buono, come Cebik.
- Distanze di stack (VHF): scelta «Frequenze: una / due» con le due frequenze e «Distanza proposta: caso peggiore / media delle due». Alla seconda frequenza le distanze in metri restano quelle della prima; ogni distanza prende il valore peggiore (o medio) di guadagno, lobi, G/T, F/R e ROS attivo, poi la scelta si rifà con il criterio (StackStudio.Scegli e DueFrequenze). Pannello in più con le due tabelle affiancate e le due curve del guadagno.
- Impostazioni > Segnala un problema: zip in data/segnalazioni (descrizione, sistema, modello, registro di oggi; cartella personale sostituita con «~», nome utente con «utente») e pagina «nuova segnalazione» del GitHub con il testo già scritto. Non invia nulla da solo.
- 18/11/2026 19:36: approvato il bozzetto «Monte Carlo nell'Ottimizzatore» (doc/bozzetti_approvati/Ottimizzatore_MonteCarlo_18-11-2026.html). Riquadro «Monte Carlo» nella linguetta Ottimizzatore: partenze a caso (di partenza 8), candidati da controllare (3), copie per candidato (50), criterio di scelta (percentuale di copie entro le soglie, caso peggiore, voto medio), seme. Le copie spostano lunghezza, posizione e diametro di ogni elemento entro le tolleranze della linguetta Tolleranze (Tolleranze.Applica); il candidato proposto diventa il migliore e la tabella dei candidati permette di riprenderne un altro. Senza la scheda (collaudi) il motore resta come prima (0 partenze, 0 candidati).

- 18/11/2026 19:55, IC8TEM: «compila e pubblica». Versione 1.4.2: VERSION, note di rilascio, README in 8 lingue, capitolo 25 del manuale, Calcolo (5.2, 6) e Citazioni (C37 Cebik Cubical Quad Notes, C38 Cebik QEX e articoli, C39 N6BV NVIS); PDF rigenerati. Prima compilazione di controllo fallita: in CalcQuad una variabile si chiamava «base» (parola riservata del C#), corretta.
- 18/11/2026 21:10, IC8TEM: «si per cortesia» alla compilazione di voacapl su GitHub. Nuovo lavoro .github/workflows/angoli.yml (si lancia a mano): compila voacapl (commit c12a98b) e lancia tools/angoli/angoli_voacap.py (metodo di W7IV, MIT): origini Capri JN70CN, I0-I8, IS0, IT9; regioni EU, Nord America est e ovest, Caraibi, Sud America, Africa australe, Giappone, Asia, Oceania; un .PRN per SSN 10, 70, 150 e uno complessivo; artefatto «angoli-voacap».
- 18/11/2026 21:18, IC8TEM: «si» - vale il SI a h1 (diffrazione completa di OpenHFTA nel Terreno reale 2) e non il NO della mattina a t1 (diffrazione su piu' creste).

### Risposte di IC8TEM alle domande in sospeso (18/11/2026 21:22)
SI: p1 (via al motore OpenHFTA), p2 (limiti HFTA nel manuale), t8 (0,6 F1), p3 (72 profili dentro il programma), p4 («solo tuo autotest» inteso bene), v1, v2, v3, k1, p5 (bozzetti Sollecitazioni elementi, Boom e tirante, Palo e plinto), y2, p6 (bozzetto rumore Sole e Luna), x1 (corner reflector), x2 (verticale ad elica 160 m), x3 (EFHW), x4 (SLT), x6 (capacita' parassite VNA), x7 (KB2SA solo come dato: «il calcolo e' gia' sufficiente», niente strumento f/D), x8 (accordatori bilanciati), x9 (citazione W7SX), x10 (toroide dalle misure). NO: l1 (niente fattore di Smith: loop a piu' spire con il solo effetto pelle e l'avviso), x5 (WSPR: «non mi serve»). Senza risposta: b3 (K9YC).
- Loop magnetica: campi «Numero di spire» e «Passo tra le spire»; Rr con (N·A)², perdita con N, induttanza con N², avviso sulla prossimita' non calcolata (esempio ARRL 15ª), modello NEC a una spira.
- 18/11/2026 21:27, IC8TEM: SI a s1 (bande diverse sullo stesso palo, IZ5IUY), s2 (commutatore di polarizzazione per Yagi a X, citando SM5BSZ), s3 (efficienza delle verticali come somma delle perdite nella scheda delle verticali; la soluzione del costruttore Butternut non va citata). Aggiunti i tre pannelli al bozzetto delle risposte del 18/11 sera.
- 18/11/2026 21:28, IC8TEM: approvato il bozzetto «Pannelli per le risposte del 18/11 sera» (doc/bozzetti_approvati/Risposte_18-11-2026_sera.html) tranne la scheda Palo e plinto, tolta: «la faro' io» (v1 e v2 restano a lui). Approvati: Sollecitazioni elementi, Boom e tirante, Bilanciamento (compensatore e contrappeso), Corner reflector, Verticale ad elica, EFHW, Trasformatore a gradini, Accordatori bilanciati, Toroidi dalle misure, Terreno reale 2 (profili, angoli, 0,6 F1, esportazioni), Rumore di Sole e Luna, Bande diverse sullo stesso palo, Commutatore di polarizzazione, Efficienza delle verticali.
- 18/11/2026 21:28, IC8TEM: «calcoli meccanici entra nella linguetta calcolatori». La linguetta principale Calcoli meccanici diventa la famiglia «Calcoli meccanici» dei Calcolatori (tra Strumenti e Dischi), con la sua fascia Vento / Bilanciamento / Boom e tirante / Palo e plinto / Sollecitazioni elementi. Sollecitazioni elementi si accende (pagina Meccanica alla YagiStress); Boom e tirante resta spenta finche' non e' pronta; Palo e plinto spenta con il fumetto «La prepara IC8TEM».
- 18/11/2026 21:29, IC8TEM: lo stack HF va valutato in una sottolinguetta nuova. Nel fascione Rumore, G/T e stack la voce «Distanze di stack» resta per VHF/UHF (senza la scelta VHF/HF) e nasce «Distanze di stack HF» (sopra il terreno) con la sua pagina, il suo stato e i suoi risultati.
- 18/11/2026 21:30, IC8TEM: «gradirei che sia inserito anche il bozzetto ocr che ho approvato». Copiato il bozzetto approvato in doc/bozzetti_approvati/Tabella_da_immagine_OCR_18-11-2026.html; l'OCR delle tabelle (8 lingue) entra nei lavori della prossima versione.
- 18/11/2026 21:55-21:56, IC8TEM: il grafico dell'elevazione del Terreno reale 2 deve essere «paragonabile al lobo di elevazione di una antenna» e la voce «Terreno reale» diventa «Terreno reale HFTA». Bozzetto rev. 2 approvato (doc/bozzetti_approvati/Terreno_reale_HFTA_rev2_18-11-2026.html): lobo di elevazione polare (orizzonte a destra, zenit in alto) con le curve sovrapposte, viste 0-90° intera e 0-35° / 0-15° ingrandite, spicchi arancioni degli angoli di arrivo VOACAP (esempio vero: Capri → Giappone, 20 m). Sostituisce il grafico cartesiano del bozzetto approvato al pomeriggio.
- 18/11/2026 21:58, IC8TEM: «esatto, questo volevo» sulla proposta di mettere voacapl dentro YA-NEC (compilato su GitHub per Windows, Linux e Mac) per calcolare sul PC dell'utente le tabelle degli angoli di arrivo di qualunque locatore, una volta sola, salvate in data/angoli (mai in AppData).
- 18/11/2026 21:58, IC8TEM: «esatto questo volevo» anche sulle regioni di arrivo relative a chi usa il programma: regioni fisse per continente, Europa e USA divisi in parti come l'ARRL, esclusa da sola la regione in cui si trova l'utente, piu' un punto qualunque scelto sulla mappa.
- 18/11/2026 21:58, IC8TEM: «esatto» su Copernicus GLO-30 (30 m, tutta la Terra, AWS) accanto a SRTM1 per il terreno fuori da 60° N - 56° S, citato nei crediti.
