# Primi passi

1. **Esempi** (barra dei comandi) apre uno dei modelli pronti: dipoli, ground plane, Yagi VHF (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), Yagi HF (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, loop Yagi a 1296 MHz e altri. **Apri modello** apre un file NEC, MMANA, AutoEZ, EZNEC o K6STI; si può anche trascinare il file sulla finestra.
2. Si sceglie la **banda** (da 2200 m a 3 cm, limiti IARU Regione 1) o una frequenza libera, poi **Calcola**.
3. I **riquadri dei risultati** mostrano impedenza, ROS, guadagno, F/B, F/R, aperture a -3 dB, efficienza e AGT (prova di convergenza: vicino a 0 dB il modello è affidabile).
4. **Sweep** calcola la banda intorno alla frequenza: ROS, R/X, guadagno, F/B e carta di Smith.

## Le linguette

- **Struttura 3D**: l'antenna (trascinare = ruota, rotella = zoom, tasto destro = sposta, doppio clic = vista iniziale), con il terreno disegnato secondo il tipo e la quota. Sotto, la **tabella dei fili** in stile foglio di calcolo: le celle accettano numeri, variabili e formule. In basso a destra gli **Oggetti del modello**: sorgenti, carichi, linee, trasformatori.
- **Diagrammi**: orizzontale, verticale e 3D, con l'angolo a -3 dB.
- **Frequenza, Smith, Correnti**: risultati dello sweep e correnti sui fili.
- **Modello NEC**: le schede NEC modificabili a mano.
- **Variabili** e **Ottimizzatore**: si dà un nome alle misure e si lascia che il programma le cambi per raggiungere un obiettivo (ROS, guadagno, F/B, impedenza...). Il manuale ha esempi passo per passo.
- **Calcolatori**: vedi [[Calcolatori]].
- **Antenne riceventi** e **Rumore/GT/Stack**: RDF delle antenne di ricezione, temperatura d'antenna e G/T (metodo di TANT e XLGTa, temperature di riferimento VE7BQH e DG7YBN), stack.
- **Avvisi**: tutto quello che il programma ha trovato di dubbio nel modello (segmenti troppo corti o lunghi, fili vicini al terreno, giunzioni affollate). Il pulsante **Controlla...** nella barra in alto verifica 42 errori tipici di inserimento senza calcolare.
- **Impostazioni**, **Manuale**, **Crediti**. In alto a destra **Lingua**, **Colori** (18 schemi) e **About**.

## Salvare

**Salva** e **Salva con nome** scrivono il modello in NEC, MMANA, AutoEZ, EZNEC o nel formato proprio **.yanec**, che conserva anche la descrizione e il file d'origine.
