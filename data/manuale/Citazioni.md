# Citazioni

## Come leggere le citazioni

Questo documento raccoglie in un solo posto le fonti tecniche di YA-NEC: libri, articoli, norme, programmi e siti. Ogni fonte ha un numero **[C..]**. Il manuale d'uso e il manuale di calcolo e delle fonti lo usano nei rimandi: cliccando un rimando si arriva qui, alla riga della fonte.

Per ogni fonte ci sono le pagine o le parti usate (quando sono note) e il posto di YA-NEC in cui si usano. YA-NEC prende dalle fonti idee, formule pubblicate e numeri di confronto. Non copia codice ne' testo: dove un metodo viene da un programma, e' stato riscritto da capo in C#. Le somiglianze e le differenze tra i calcoli di YA-NEC e le fonti sono spiegate capitolo per capitolo nel manuale di calcolo.

I crediti del software (librerie, caratteri e licenze) restano nella linguetta **Crediti** e nel file CREDITI.txt.

> NOTA: i numeri delle citazioni non cambiano da una versione all'altra. Una fonte nuova prende il primo numero libero; una fonte tolta lascia il suo numero vuoto.

## Libri

| N. | Fonte | Pagine usate | Dove si usa in YA-NEC |
|---|---|---|---|
| C12 | ARRL Antenna Book, 24ª ed. (ARRL 2019), a cura di H. W. Silver N0AX; capitoli di R. D. Straw N6BV, G. Ordy W8WWV, S. Stearns K6OIK, J. Johnson G0KSC, R. Severns N6LF | 8 (modellazione, correzione di Leeson, Tab. 8.6); 10.2 e 10.5 (trappole, Yagi multibanda); 11.9-11.11 (bilanciamento, tolleranze); 14.21-14.28 (terreno reale, HFTA); 22.1 (antenne riceventi, RDF, K9AY); 24.2-24.7 (accordatore, choke, Tab. 24.12); 26 app. A e B (vento, palo) | terreno reale, Calcolatori, Calcoli meccanici, Carichi HF, Cavo e balun, linea aperta e accordatore, stack (calcolo 3, 5, 9, 10, 12, 13, 14) |
| C13 | ARRL Antenna Book, 21ª ed. (ARRL 2007) | cap. 4: prova di convergenza e del guadagno medio (Tab. 5, 6 e 7, Yagi 520-40H) | AGT e convergenza, caso di prova della correzione di Leeson (calcolo 2, 4) |
| C15 | ARRL Antenna Book, 19ª ed. (ARRL 2000) | spaziatura dei parassiti della Yagi a 2 elementi | Calcolatori: Yagi (calcolo 5) |
| C14 | C. A. Balanis, Antenna Theory: Analysis and Design, 4ª ed. (Wiley 2016) | par. 2.12 (polarizzazione, PLF); 2.18 (temperatura d'antenna con la linea); 4.8.4 (fattore di divergenza, eq. 4-132/4-134; rugosita', eq. 4-137); cap. 8 (impedenza propria e mutua); 10 (Tab. 10.1-10.4, Cheng e Chen); 11.4.3 (LPDA, Fig. 11.13); 12 (Tab. 12.2); 13.4.3 e 13.6 (trombe ottime); 15 (illuminatori cos^n) | Rumore e G/T, terreno reale, Calcolatori, Parabola, Trombe (calcolo 3, 5, 6, 8) |
| C40 | J. L. Volakis (a cura di), Antenna Engineering Handbook, 4ª ed. (McGraw-Hill 2007) | cap. 4 (Tai e Long: eq. 4-1, 4-2, Tab. 4-1 impedenza del dipolo; eq. 4-9 – 4-13 dipolo ripiegato); cap. 5 (Smith: eq. 5-7, 5-20, 5-21 loop piccola e accoppiamento capacitivo); cap. 29 (Yagi: 29.2 vento e fatica, 29.4 sensibilita', Tab. 29-2 spaziature di stack, XPD e Yagi incrociate); 52.3 (trasformatori λ/4); 52.7 (balun, Fig. 52-23) | stack, Tolleranze, Vento, Yagi incrociate, Divisori, Cavo e balun; Resistenza di radiazione, Dipolo ripiegato, Loop magnetica (calcolo 6, 9, 11, 12, 13) |
| C18 | J. Devoldere ON4UN, Low-Band DXing, 4ª ed. (ARRL 2005) e 5ª ed. | cap. 7 (DMF, Tab. 7-22, 7-24, 7-25); cap. 8 (8-11/8-12: bobina di carico, linea aperta); cap. 9 (radiali, Tab. 9-1, 9-3, 9-4, misure DJ8WL); p. 167 (bobine come carichi in frequenza) | Antenne riceventi, RDF e DMF, Carichi HF, radiali (calcolo 6, 7, 14) |
| C16 | K. Rothammel Y21BK e A. Krischke DJ0TR, Rothammels Antennenbuch (DARC Verlag) | cap. 24 (Tab. 24.1 e Bild 24.1.3: Yagi lunga DL6WU); diametri equivalenti; stack | Yagi lunga DL6WU (calcolo 5) |
| C17 | D. Leeson W6NL, Physical Design of Yagi Antennas (ARRL 1992) | cap. 8 (diametri a gradini), cap. 9 (staffe) | correzione di Leeson, tubi telescopici (calcolo 4) |
| C19 | J. D. Kraus, Antennas (McGraw-Hill) | elica in modo assiale; resistenza di radiazione del dipolo corto, del monopolo e del loop | Calcolatori: elica, dipoli, loop (calcolo 6) |
| C20 | W. L. Stutzman e G. A. Thiele, Antenna Theory and Design | eq. 9-233 (illuminatore cos^q) | Parabola primo fuoco |
| C21 | J. W. M. Baars, The Paraboloidal Reflector Antenna in Radio Astronomy and Communication (Springer 2007) | cap. 4 | Parabola: taper, HPBW, fuori fuoco, errore di superficie |
| C22 | P.-S. Kildal, Foundations of Antennas | scomposizione dell'efficienza | Parabola offset |
| C23 | T. A. Milligan, Modern Antenna Design, 2ª ed. (Wiley) | definizione di G/T | Rumore e G/T (calcolo 8) |
| C24 | J. Sevick W2FMI, Transmission Line Transformers, 4ª ed. (Noble 2001) | balun a linea di trasmissione | Cavo e balun (calcolo 13) |
| C25 | R. J. Roark e W. C. Young, Roark's Formulas for Stress and Strain | Tab. 8.1 (trave appoggiata con carico uniforme) | centine e arcarecci della parabola, Calcoli meccanici (calcolo 12) |
| C26 | M. Abramowitz e I. A. Stegun, Handbook of Mathematical Functions | integrali di Fresnel | terreno reale: diffrazione (calcolo 3) |
| C28 | I. E. Idelchik, Handbook of Hydraulic Resistance; S. F. Hoerner, Fluid-Dynamic Drag | diagr. 8-6 (rete) | vento sulla rete della parabola (calcolo 12) |
| C29 | E. Anderson e altri, LAPACK Users' Guide, 3ª ed. (SIAM 1999); G. H. Golub e C. F. Van Loan, Matrix Computations, 4ª ed. (2013) | LAPACK 3.4; Golub-Van Loan 3.2.11 | fattorizzazione LU a blocchi del motore (calcolo 1) |
| C30 | J. Meeus, Astronomical Algorithms | cap. 47 (Luna) | Rumore dal cielo vero (calcolo 8) |
| C31 | J. Nielsen, Usability Engineering (1993) | cap. 5 (soglie 0,1 / 1 / 10 s, da R. B. Miller 1968) | Registro delle operazioni e tempi |
| C32 | P. Wade W1GHZ, Online Microwave Antenna Book | cap. 4, 5, 6 (6.3, 6.5), 12; app. 6A | Parabola, Parabola offset, Trombe e illuminatori |
| C37 | L. B. Cebik W4RNL, Cubical Quad Notes, vol. 1-3 (antenneX) | vol. 2 cap. 2: perimetro della spira risonante in funzione di log10(d/λ), programma GW-Basic e Tab. 2-2; vol. 3: conclusioni (quad oltre 4 elementi, multibanda) | Calcolatori: Quad, metodo Cebik e avviso (calcolo 5) |

## Articoli e riviste

| N. | Fonte | Pagine usate | Dove si usa in YA-NEC |
|---|---|---|---|
| C11 | G. Hoch DL6WU, "Extremely Long Yagi Antennas", VHF Communications; distanza di stack dalle aperture a -3 dB | formula della distanza di stack; tabella della Yagi lunga; correzione del boom | scheda Distanze di stack DL6WU, Yagi lunga DL6WU (calcolo 5, 9) |
| C41 | G. H. Brown, R. F. Lewis, J. Epstein, "Ground Systems as a Factor in Antenna Efficiency", Proc. IRE 25 n. 6 (1937) | pp. 753-787, Fig. 25, 28, 36, 37 | radiali, Verticale λ/4 (calcolo 6) |
| C42 | N6MW, "Brown, Lewis and Epstein's 1937 Antenna Ground System Paper - Theoretical Aspects" (2016) | parte teorica | avvertenza nella scheda Verticale λ/4 |
| C43 | P. P. Viezbicke, Yagi Antenna Design, NBS Technical Note 688 (1976) | tabelle delle Yagi | Calcolatori: Yagi (calcolo 5) |
| C44 | D. K. Cheng e C. A. Chen, ottimizzazione delle Yagi, IEEE Trans. AP (1973-1975) | Yagi a 6 elementi (in C14, Tab. 10.1-10.4) | Calcolatori: Yagi |
| C45 | R. L. Carrel (1961); P. C. Butson e G. T. Thompson (1976) | curve di progetto delle LPDA e loro correzione (in C14, Fig. 11.13) | Calcolatori: log-periodica |
| C46 | G. Breed K9AY, "The K9AY Terminated Loop", QST settembre 1997; "Modeling the K9AY Loop", NCJ marzo/aprile 2015 | metodo del modello raddoppiato con l'immagine | Antenne riceventi: K9AY (calcolo 7) |
| C47 | F. Koontz WA2WVL, "Is This EWE for You?", QST febbraio 1995 | EWE | Antenne riceventi |
| C48 | L. Mansutti IV3PRK, "Panoramica sulle antenne per la ricezione in 160 metri", CQ Friul 2/2008 | valori di RDF | Antenne riceventi, RDF di confronto (calcolo 7) |
| C49 | F. Fucelli IK0XBX, "Le mie antenne RX" (ARI Perugia 2018) | Beverage e K9AY | Antenne riceventi |
| C50 | R. Bertelsmeier DJ9BV, Dubus 4/87; L. Edwards VE7BQH, tabelle G/T a 50, 144 e 432 MHz | temperature di cielo e terreno; G/T come criterio EME | Rumore e G/T, criterio dello stack (calcolo 8, 9) |
| C51 | H. A. Wheeler, "Simple Inductance Formulas for Radio Coils", Proc. IRE (1928); R. G. Medhurst, Wireless Engineer (1947) | induttanza e capacita' propria della bobina | Carichi HF, trappole (calcolo 14) |
| C52 | C. E. Smith e E. M. Johnson, "Performance of Short Antennas", Proc. IRE ottobre 1947; R. Severns N6LF, QEX maggio/giugno 2013 | Z0 = 60 (ln(h/r) - 1); Q della bobina ed efficienza | verticali corte, Bobina di carico (calcolo 14) |
| C53 | G. Guanella (1944); W. K. Ruthroff, Proc. IRE agosto 1959 | balun e trasformatori a larga banda | Cavo e balun (calcolo 13) |
| C54 | E. J. Wilkinson, IRE Trans. MTT (1960) | divisore a N vie | Divisori (calcolo 13) |
| C55 | J. Ruze, "Antenna Tolerance Theory", Proc. IEEE (1966) e articoli del 1968-1969 | errore di superficie, blocco, fuori fuoco | Parabola |
| C56 | J. C. Sager G8ONH, skynoise; C. G. T. Haslam e altri, A&AS 47 (1982); M. Remazeilles e altri, MNRAS 451 (2015) | survey a 408 MHz | Rumore dal cielo vero (calcolo 8) |
| C57 | E. K. Miller e G. J. Burke, model-based parameter estimation (MBPE) | funzioni razionali per lo sweep | sweep rapido in frequenza (calcolo 1) |
| C58 | J. A. Nelder e R. Mead, "A simplex method for function minimization", Computer Journal 7 (1965) | metodo del simplesso | Ottimizzatore |
| C59 | C. M. Butler, IEEE Trans. AP-30 (1982); A. C. Ludwig, IEEE Trans. AP-35 (1987); A. Rubinstein e altri, IEEE Trans. EMC 47 (2005) | raggio equivalente di una fascia; griglia di fili per le superfici | loop a fascia, piani di massa e parabole a griglia (calcolo 2) |
| C60 | A. I. Bauerle, "Accurate Modeling of Stepped-Radii Antennas", ACES Journal 10 n. 2 (1995) | diametri a gradini | confronto della correzione di Leeson (calcolo 4) |
| C61 | D. T. Emerson, "The gain of the axial-mode helix antenna", NRAO (1995) | guadagno dell'elica | Calcolatori: elica (calcolo 6) |
| C62 | VK2KU, "Effects of the boom"; G3SEK | correzione del boom | Yagi (calcolo 5) |
| C63 | L. G. Windom W8GZ, QST settembre 1929; F. Spillner DJ2KY, QRV dicembre 1971; Belrose e Bouliane, QST agosto 1990; C. L. Buchanan W3DZZ, QST marzo 1955 | dipoli alimentati fuori centro e trappolati | Calcolatori: dipoli (calcolo 6) |
| C38 | L. B. Cebik W4RNL, "Antenna Options" n. 1-3 e 12-13 (QEX); "The Moxon Rectangle on 2 Meters"; "Some J-Poles That I Have Known"; "What Is a Slim Jim?" | Yagi a 3 elementi e adattamento a forcina; NVIS (altezza 0,175 λ, schermo, 9 fili); Moxon 2 m Tab. 1-2; J-pole standard e non standard (2,46 e 2,56 dBi dopo l'AGT); Slim Jim | Calcolatori: Forcina, NVIS, J-pole / Slim Jim (calcolo 5, 6) |
| C39 | R. D. Straw N6BV, "What's the Deal About NVIS?", QST dicembre 2005 | dipolo NVIS e antenne molto basse | Calcolatori: NVIS (calcolo 6) |

## Norme e rapporti

| N. | Fonte | Pagine usate | Dove si usa in YA-NEC |
|---|---|---|---|
| C3 | G. J. Burke e A. J. Poggio, Numerical Electromagnetics Code (NEC) - Method of Moments, LLNL (1981), parti I, II e III | parte III: schede GN, LD, EX, CP, RP; sez. II "Structure Modeling Guidelines"; esempi 1-3 | motore, terreni, matrice di accoppiamento, avvisi di segmentazione (calcolo 1, 2, 3, 9) |
| C4 | G. J. Burke, NEC-4.2 (LLNL) | giunzioni con raggi diversi | opzione NEC-4 per le giunzioni (calcolo 2) |
| C70 | ITU-R P.372 (rumore radio), P.526 (diffrazione), P.453 (raggio efficace), P.2040 (materiali da costruzione) | - | Rumore, terreno reale, solai (calcolo 3, 8) |
| C71 | NTC 2018 e Circolare 7/2019; CNR-DT 207 R1/2018; EN 1991-1-4 | azione del vento | Calcoli meccanici: Vento (calcolo 12) |
| C72 | EN 755-2, EN 1999-1-1, EN 10025-2, EN 13706-3 | caratteristiche di alluminio, acciaio e vetroresina | Calcoli meccanici, centine (calcolo 12) |
| C73 | Texas Instruments AN043, DN007, DN034 | antenne su PCB a 2,4 GHz | Calcolatori: PCB 2,4 GHz |

## Programmi e codice

| N. | Fonte | Pagine usate | Dove si usa in YA-NEC |
|---|---|---|---|
| C1 | N. Kyriazis 5B4AZ, nec2c (GPL-3), curato da E. Wheeler KJ7LNW | routine di geometria, matrice, campi e diagramma; somnec; qdsrc | motore di YA-NEC, tradotto in C#; riferimento dell'autotest (calcolo 1) |
| C2 | T. Molteno, necpp / nec2++ (GPL-2) | solo i valori dei casi di prova | terzo riferimento numerico dell'autotest (calcolo 1, 3) |
| C5 | R. Lewallen W7EL, EZNEC Pro/2+ 7.0 | help (Stepped Diameter Correction); comportamento di Geometry Check e Segmentation Check | avvisi, fili collegati al terreno, confronti (calcolo 2, 4) |
| C6 | A. Voors, 4nec2 | help (EX 6, LD 6, LD 7, GN 3); Ground.txt, Conduc.txt | terreni tipici, filo isolato (calcolo 2, 3) |
| C7 | YT1NT / VE3EA, TANT; D. Maguire AC6LA, XLGTa; KF2YN | metodo di Tant e G/T, correzione del guadagno medio | Rumore e G/T (calcolo 8) |
| C8 | R. D. Straw N6BV, HFTA | metodo (GO + UTD), formato .PRO | terreno reale (calcolo 3) |
| C9 | M. Mori JE3HHT, MMANA; I. Gontcharenko DL2KQ e A. Schewelev DL1PBD, MMANA-GAL | formato .maa; help (ottimizzatore, stack, Taper wire set) | importazione dei modelli, Rastremazioni |
| C10 | B. Beezley K6STI, YO e ANTFILE.DOC | formati .YAG e .ANT | importatori |
| C27 | YO3IIU, nec2-array (GPL-2 o successiva) | solo il metodo della matrice di accoppiamento | matrice di accoppiamento dello stack, riscritta in C# senza righe copiate (calcolo 9) |
| C33 | L. B. Cebik W4RNL, MOXGEN e articoli (antenna2.github.io/cebik) | regressioni del Moxon; NEC-4 contro NEC-2 con i gradini; terreni S-N, RCA e MININEC | Moxon, correzione di Leeson, avvisi del terreno (calcolo 2, 4, 6) |
| C34 | E. Wheeler KJ7LNW, xnec2c e xnec2c-optimize (GPL-3); P.-T. de Boer PA3FWM, xnecview | interfaccia di riferimento, ottimizzatore | finestre dei diagrammi, Ottimizzatore |
| C35 | J. Coppens, qfhcalc.js (GPL-2); G1OJS, NECBOL (MIT); C. Hoover, tamago (GPL-3) | formule della QFH; geometria dell'elica; eggbeater | Calcolatori per i satelliti |
| C36 | MathWorks Antenna Toolbox | Yagi a 6 elementi, elica, biquad | casi di confronto dell'autotest |

## Siti e misure

| N. | Fonte | Pagine usate | Dove si usa in YA-NEC |
|---|---|---|---|
| C80 | M. Steyer DK7ZB (qsl.net/dk7zb) | Moxon a filo, Yagi lunghe, HB9CV, adattamento 28 ohm | Calcolatori, casi dell'autotest |
| C81 | H. Kluever DG7YBN (dg7ybn.de) | linee di fase, divisori, balun, manuale di TANT | Divisori, Rumore e G/T |
| C82 | T. Rauch W8JI; G. Ordy W8WWV | definizione dell'RDF; LBDXView | RDF (calcolo 7) |
| C83 | J. Brown K9YC, "A New Choke Cookbook for the 160-10M Bands" (2018); S. Hunt G3TXQ | misure dei choke | Cavo e balun (calcolo 13) |
| C84 | I. Pellejero, "Notes on antenna simulation using NEC-2"; R. Fry (2012) | quote minime dei fili sul terreno | avvisi del terreno (calcolo 2, 3) |
| C85 | TINITALY 1.1 (INGV, CC BY 4.0, doi 10.13127/tinitaly/1.1); SRTM1 (NASA / USGS) | modelli del terreno | terreno reale (calcolo 3) |
| C86 | Hi-Z Antennas | tabella RDF | Antenne riceventi |
| C87 | IC8TEM, prove con EZNEC Pro/2+ 7.0 del 14/11/2026 (735 modelli) | - | calibrazione del collaudo |

## Fonti scartate

| N. | Fonte | Motivo |
|---|---|---|
| C90 | Rothammels Antennenbuch, cap. 24, Tab. 24.2 (correzione del boom misurata) | scartata da IC8TEM il 14/11/2026: sono le stesse misure di DL6WU, gia' usate con la formula DL6WU/G3SEK |
| C91 | D. B. Leeson, "A Simple Model of Feedback Oscillator Noise Spectrum", Proc. IEEE 1966 | omonimo: riguarda il rumore di fase degli oscillatori, non la correzione dei tubi. Citato solo per evitare la confusione |
| C92 | progetti senza licenza (antsim, yagi_designer, moxon-antenna-calculator, pymoxon) | solo consultati: nessun codice usato |

> NOTA: la rilettura delle fonti scartate e' in corso; questo elenco si aggiorna con le decisioni di IC8TEM.
