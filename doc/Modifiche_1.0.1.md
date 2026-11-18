
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
