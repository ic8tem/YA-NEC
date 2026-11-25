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
| C125 | M. Lisi IZ0FNO, «Spaziatura degli elementi di un array e area efficace», RadioRivista 9/2026 | area efficace Ae = G λ²/4π, cella dello stack, curva NBS e simulazione di I0KPT | Distanze di stack DL6WU e studio dello stack, 1.7 (manuale 28.1) |
| C126 | P. Musacchio I5WHC, PA da 700 W per i 160 m, RadioRivista 9/2026; «Adattare necesse est», RadioRivista 4/2025 | ROS 2 con R tra 25 e 100 Ω, corrente dei MOSFET; resistenza di radiazione e perdite, cappello contro bobina alla base | ROS da che lato, Consigli, efficienza nei calcolatori, 1.7 (manuale 28.2, 28.12) |
| C127 | M. Dose, EFHW QRP con FT-82-43 e cavetto d'acciaio, RadioKit 6/2023 | nucleo FT-82-43, 20,5 m di cavetto da 0,6 mm | Filari, 1.7 (manuale 28.3) |
| C128 | ARRL Handbook, cap. 20 «Antenne e progetti» (edizione spagnola) | dipolo a banda larga con risonatore coassiale, L invertita e T per i 160 m, half-sloper di VE2CV, gamma e delta match (solo le idee) | nuovi calcolatori HF, 1.7 (manuale 28.4) |
| C129 | D. J. Healey W3PVZ, «An Examination of the Gamma Match», QST 4/1969 | fattore di divisione della corrente, rapporto (1 + α)², linea in corto in parallelo | Gamma match, 1.7 (manuale 28.4) |
| C130 | M. Filippi I4MFA, RadioRivista 5/2023 (rimanda a RR 3/1992 per le linee di disaccoppiamento), 9/2024, 11/2024, 9/2025, 11/2025 | linee di disaccoppiamento nella quad multibanda; propagazione e angoli di partenza deterministici; Yagi HF 2-3 elementi e regole di taratura; spider quad 10/15 m con linee separate | Quad multibanda, Terreno reale, Yagi HF, 1.7 (manuale 28.5, 28.8, 28.9) |
| C131 | S. Stefani I3STS e IZ3LJH, «La mia antenna loop magnetica», RadioRivista 6/2024 | loop rettangolare, spire in parallelo, trasformatore e rete L, Q misurato | Loop magnetica, 1.7 (manuale 28.6) |
| C132 | IW2FND, adattamento a stub, RadioRivista 10/2023 | stub singolo e doppio (formule classiche delle linee) | Adattamento a stub, 1.7 (manuale 28.7) |
| C133 | IK4PKK e IK4CIE, dipolo multibanda a rele', RadioRivista 9/2025 | tratti a lunghezza piena uniti da rele', corrente dei rele' | Dipolo multibanda a rele', 1.7 (manuale 28.10) |
| C134 | IZ2IAM, Cushcraft AV-5, RadioRivista 11/2025; Grazioli, verticale MV7, RadioKit Elettronica 11/2025 | verticale trappolata a 5 bande; bobina con cappello (il contrappeso brevettato non si usa) | Verticale trappolata, 1.7 (manuale 28.11) |
| C135 | J. Reisert W1JR, «12- and 17-Meter Antenna Design», QST 2/2022 | Yagi bibanda sullo stesso boom con una linea, elementi isolati, interazione tra le bande (solo riassunto) | Yagi bibanda interlacciata, 1.7 (manuale 28.12) |
| C136 | I3RKE, Yagi 4 elementi per gli 8 m con radiatore ripiegato verticale, RadioRivista 3/2025 | idea del radiatore ripiegato alimentato direttamente a 50 Ω | modello d'esempio degli 8 m, 1.7 (misure proprie) |
| C137 | M. Diana IU5HIV, «Carico lineare per antenne a 1/4», RadioKit Elettronica 1/2024 | carico lineare (linea bifilare in corto) al posto della bobina | Carico lineare, 1.7 (manuale 28.13) |
| C138 | «EFLW», RadioKit Elettronica 1/2024 | filo di 21,12 m risonante in armonica su 40, 20, 15 e 10 m; fattore di accorciamento misurato (0,93) | Filari › risonanze in armonica, Consigli, 1.7 (manuale 28.14) |

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
| C115 | E. S. Harriman AE6TY, nec2c-ae6ty (github.com/ae6ty/nec2c-ae6ty, licenza MIT) | fattorizzazione a blocchi | LU a blocchi (calcolo 1.7), solo l'idea |
| C116 | R. Sutherland, hf-nec2c (github.com/sutherlandryan/hf-nec2c, licenza BSD-2) | costanti di zint dal listato del NEC-2 | calcolo 1.7 |
| C117 | UR3PKI, nec2-rs (github.com/CyberDev-space/nec2-rs, licenza MIT), docs/nec2c-deviations.md, letto il 22/11/2026 | i dieci punti in cui nec2c si scosta dal Fortran del NEC-2 | confronto del calcolo 1.7 |
| C118 | S. M. Burns, momwire (github.com/stevenmburns/momwire, licenza MIT), tests/fixtures/refusal_coverage_0800 e docs/deck-dialect.md, letto il 22/11/2026 | 33 file NEC volutamente sbagliati e le regole con cui vanno rifiutati | controlli di ingresso della 1.5.9 (calcolo 1.7); nessun codice copiato |
| C119 | EA1FUO, AntennaSim (github.com/EA1FUO/AntennaSim, licenza GPL-3), letto il 22/11/2026 | correnti animate lungo i fili e valori al passaggio del mouse nella vista 3D | solo l'idea per la Struttura 3D della 1.5.9 (manuale, «Struttura 3D: caselle della vista»); nessun codice usato |
| C120 | sozden, AntAn (github.com/sozden/AntAn, licenza Apache-2.0), letto il 23/11/2026 | dalla frequenza le antenne possibili, ognuna calcolata; pulsante di adattamento a 50 ohm | solo l'idea per Calcolatori › Suggerimenti e per l'Adattamento diretto a Z0 della 1.6 (manuale 27.1, 27.2); nessun codice usato |
| C121 | R. Storn e K. Price, «Differential Evolution - A Simple and Efficient Heuristic for Global Optimization over Continuous Spaces», Journal of Global Optimization 11 (1997) 341-359 | schema DE/rand/1/bin, valori tipici F 0,5-1 e CR 0,9 | Ottimizzatore, evoluzione differenziale della 1.6 (manuale 27.3) |
| C122 | NEC-Workbench (licenza GPL-3) e 45south (senza licenza), letti il 23/11/2026 | ottimizzatore a evoluzione differenziale; risultati precedenti sovrapposti; correnti colorate per fase | solo le idee per la 1.6 (manuale 27.3, 27.4); nessun codice usato |
| C123 | lrizio, AntennaCalc (licenza MIT), letto il 23/11/2026 | corrente di modo comune lungo il cavo fino alla stazione | solo l'idea per Cavo e balun della 1.6 (manuale 27.5); nessun codice usato |
| C124 | H. Strappman AG7T, Ez2Nec (github.com/HanleyStrappman/Ez2Nec, licenza Apache-2.0), src/ez/read.rs e src/nec/elements.rs, letti il 23/11/2026 | posizione nel .ez dei carichi di Laplace, delle perdite delle linee (blocco 0x0e) e delle reti L (blocco 0x11); formule della linea con perdite e dei rami RLC | Importatore EZNEC della 1.6 (manuale 27.6); formule riscritte in C#, nessun codice copiato |
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
| C88 | S. Yates AA5TB, "The End-Fed Half-Wave Antenna" (aa5tb.com/efha.html, 1998-2019, opera protetta: solo citata, letta il 22/11/2026) | contrappeso 0,05 λ (trovato con misure e modelli; piu' lungo non migliora), impedenza in punta 1800-5000 ohm, rapporto spire = √(Za/50) (7:1 per 2450, 8:1 per 3200; l'8:1 da' il ROS piu' stabile al variare del contrappeso), calza del coassiale come ritorno, RF in stazione con lunghezze critiche | Filari (calcolo 16) |
| C89 | G. B. Gudlaugsson TF5NN, HF Antenna Impedance Visualizer v3 (github.com/TF5NN, CC BY 4.0) | modello del filo come linea con perdita, effetto d'estremita', confronto con NEC-2 | Filari (calcolo 16) |
| C93 | VE3EED, lunghezze del filo non risonante per l'un-un 9:1 (ve3ips.wordpress.com, letto il 22/11/2026) | tabella delle lunghezze: 29, 35,5, 41, 58, 71, 84, 107, 119, 148 piedi (8,8-45,1 m), e piu' lunghe fino a 423 piedi | Filari (calcolo 16) |
| C94 | DG1JAN, xOTA-Antenna (github.com/DG1JAN, CC BY-SA 4.0) e "Comparing the efficiency of Ferrite Toroids for EFHW-Antennas" (16/06/2023, codeberg.org/dg1jan/UniBalun, letto il 22/11/2026) | 100 pF; efficienza misurata dei nuclei con NanoVNA su carico da 2400 ohm (perdita = S21 - disadattamento - carico): FT-82 e FT-114 circa 80 %, FT-140-43 2/14 circa 79 %, FT-240-43 2/15 circa 75 % (71 % a 30 MHz), nuclei Fair-Rite accoppiati circa 90 %, 2643251002 (MM0OPX) 2/15 circa 91 % | Filari (calcolo 16) |
| C95 | L. I3RKE, "Conoscere le antenne End Fed" (ARI Vittorio Veneto, 2020) | la calza del coassiale come parte dell'antenna, choke | Filari (calcolo 16) |
| C96 | OfficinaHF (officinahf.jimdofree.com), pagine End Fed, balun e un-un, long wire, Windom, Delta | bobina per gli 80 m, tensione in punta, 64:1 | Filari (calcolo 16) |
| C97 | O. Duffy VK1OD (owenduffy.net) | perdite dei trasformatori EFHW | Filari (calcolo 16) |
| C99 | P. Mansutti IV3PRK, «160 m. receiving antennas overview and evaluation» (gennaio 2008) (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | RDF con passo di 5 gradi, confronto traliccio / dipolo / Pennant / Flag / 4-square / Beverage, i radiali non cambiano l'RDF | Antenne riceventi (calcolo 17) |
| C100 | IV3PRK, «The Pennant antenna» (gennaio 2010) (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | Pennant 9 x 4 m, 880-900 ohm, resistenza a carbone | Antenne riceventi (calcolo 17) |
| C101 | IV3PRK, «End-Fire PENNANTS» (ottobre 2004) e «Pennants revisited» (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | Pennant end-fire e broadside, Flag contro Pennant | Antenne riceventi (calcolo 17) |
| C102 | IV3PRK, «the TX3A DHDL» (novembre 2009), «DHDL TX3A 2» e «DHDL broadside» (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | DHDL, doppino tra trasformatori isolati | Antenne riceventi (calcolo 17) |
| C103 | IV3PRK, «Transformers and Chokes for 160 m. receiving antennas» (gennaio 2019, P.S. 2025) (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | trasformatori su binoculare, choke 75 (J) e 31 (K9YC) | Antenne riceventi (calcolo 17) |
| C104 | IV3PRK, «the QDFA project» (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | QDFA rovinato dai fili vicini (traliccio, radiali, recinzione) | Antenne riceventi (calcolo 17) |
| C105 | IV3PRK, «the 4-Square Rx Vertical Array, Part 1: EZNEC modeling and the array design» (marzo 2008) (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | elementi caricati in cima, linee di ritardo crossfire | Antenne riceventi (calcolo 17) |
| C106 | IV3PRK, «Signal/Noise Tests Using an improved 1.843 Test Oscillator and the SDR-IQ receiver» (marzo 2010) (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | misure di S/N delle antenne riceventi | Antenne riceventi (calcolo 17) |
| C107 | IV3PRK, «again Noise Tests» (sn tests 3) (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | il rumore della 4-square veniva dai fili di carico | Antenne riceventi (calcolo 17) |
| C108 | IV3PRK, «the 4-Square Rx Vertical Array», Part 3 (ottobre 2008), Part 4 e Part 6 (K7TJR Hi-Z) (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | installazione, prove, 4-square con amplificatori ad alta impedenza | Antenne riceventi (calcolo 17) |
| C109 | IV3PRK, «the two-wire Beverage and the NOISE mystery to be solved» (dicembre 2008) e «follow up» (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | rumore da un cavo telefonico interrato | Antenne riceventi (calcolo 17) |
| C110 | IV3PRK, «the 3 wires MiniLoop» (progetto di D. Lankford) (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | nulli del miniloop sulle onde medie e sui 160 m | Antenne riceventi (calcolo 17) |
| C111 | F. Fucelli IK0XBX, «Le mie antenne RX» (Sezione ARI di Perugia, 18/12/2018) (presentazione allegata da IC8TEM; solo citata) | Beverage, K9AY a quattro direzioni, cavo e terra | Antenne riceventi (calcolo 17) |
| C112 | IV3PRK, «the 4-Square Rx Vertical Array, Part 2: putting all stuff together and construction details» (agosto 2008) (iv3prk.it, tutti i diritti riservati: solo citato, letto il 22/11/2026) | VF misurato, linee di ritardo, attenuatore a T, scatole, modo comune e fulmini (K4SAV, K7TJR) | Antenne riceventi (calcolo 17) |
| C113 | P. Salas AD5X, «The 43-Foot Vertical», nella presentazione di N0CU (2016, allegata da IC8TEM; solo citata) | Rr e reattanza sui 160 m, ROS con il 4:1, perdite nel coassiale, guadagni e angoli per banda, tensione alla base | Verticale non risonante (calcolo 18) |
| C114 | OH7SV, trasformatore EFHW 49:1 su FT-240-43 (jumaradio, letto il 22/11/2026; solo citato) | potenza per 1, 2 e 3 nuclei, per modo (continua, CW, SSB) e banda (160 m, 80 m e sopra); condensatore 47-100 pF 2 kV | Filari, nucleo automatico (calcolo 16) |

## Fonti scartate

| N. | Fonte | Motivo |
|---|---|---|
| C90 | Rothammels Antennenbuch, cap. 24, Tab. 24.2 (correzione del boom misurata) | scartata da IC8TEM il 14/11/2026: sono le stesse misure di DL6WU, gia' usate con la formula DL6WU/G3SEK |
| C91 | D. B. Leeson, "A Simple Model of Feedback Oscillator Noise Spectrum", Proc. IEEE 1966 | omonimo: riguarda il rumore di fase degli oscillatori, non la correzione dei tubi. Citato solo per evitare la confusione |
| C92 | progetti senza licenza (antsim, yagi_designer, moxon-antenna-calculator, pymoxon) | solo consultati: nessun codice usato |
| C98 | progetti EFHW senza licenza (cheenle/efhw-knowledge, evil-lair/EFHW-antennas, ben-kuhn/BaitcasterEFHW, aussiegeek/portable-efhw) e ckowals/EFHW4 | i primi solo consultati per il confronto delle fonti, nessun testo ne' codice usato; ckowals/EFHW4 non riguarda le antenne |

> NOTA: la rilettura delle fonti scartate e' in corso; questo elenco si aggiorna con le decisioni di IC8TEM.
