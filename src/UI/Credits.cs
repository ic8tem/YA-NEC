// YA-NEC - crediti: i colleghi e i progetti da cui YA-NEC deriva (motore, interfaccia di riferimento, librerie)
using System.Collections.Generic;

namespace YaNec
{
    public class CreditEntry
    {
        public string Title, Who, What, Link, License;
    }

    public static class Credits
    {
        public static readonly List<CreditEntry> Entries = new List<CreditEntry>
        {
            // 0.5.3 (IC8TEM 15/11/2026 22:30): ringraziamento a Luis IV3PRK
            new CreditEntry
            {
                Title = "Ringraziamenti",
                Who = "Luis Mansutti IV3PRK",
                What = "Per le pubblicazioni gratuite sul suo sito (iv3prk.it): antenne riceventi, misure e modelli che YA-NEC usa nella linguetta Antenne riceventi e nei confronti di RDF.",
                Link = "iv3prk.it",
                License = "pubblicazioni gratuite dell'autore, citate"
            },
            // 1.0 (IC8TEM 16/11/2026 00:08): modelli MMANA aggiunti agli esempi
            new CreditEntry
            {
                Title = "Esempi Yagi 144 MHz (MMANA)",
                Who = "I4GBZ (11 el), Rainer Bertelsmeier DJ9BV (BVO2-3WL, 10 el), Martin Steyer DK7ZB (7 el EME), Makoto Kasahara JN1GLB (hexbeam 6 bande)",
                What = "Modelli MMANA forniti da IC8TEM e distribuiti tra gli esempi di YA-NEC, nel formato originale dentro il .yanec.",
                Link = "",
                License = "modelli degli autori, citati"
            },
            new CreditEntry
            {
                Title = "NEC2 - Numerical Electromagnetics Code",
                Who = "Gerald J. Burke e Andrew J. Poggio, Lawrence Livermore National Laboratory (1981)",
                What = "Il metodo dei momenti per antenne a fili su cui si basa tutto il calcolo di YA-NEC.",
                Link = "",
                License = "di pubblico dominio"
            },
            new CreditEntry
            {
                Title = "nec2c",
                Who = "Neoklis Kyriazis 5B4AZ (traduzione in C del NEC2); repository curato da Eric KJ7LNW",
                What = "Il motore di YA-NEC e' la traduzione in C# delle sue routine (geometria, matrice, campi, diagramma). " +
                       "L'autotest confronta YA-NEC con nec2c sugli stessi modelli.",
                Link = "github.com/KJ7LNW/nec2c",
                License = "GPL-3.0"
            },
            new CreditEntry
            {
                Title = "xnec2c",
                Who = "Neoklis Kyriazis 5B4AZ (autore), Eric KJ7LNW (manutenzione e sviluppo attuale)",
                What = "L'interfaccia grafica di riferimento: finestre della struttura, dei diagrammi e dei dati in frequenza, carta di Smith.",
                Link = "github.com/KJ7LNW/xnec2c - www.xnec2c.org",
                License = "GPL-3.0"
            },
            new CreditEntry
            {
                Title = "xnec2c-optimize",
                Who = "Eric KJ7LNW",
                What = "L'ottimizzatore simplex delle geometrie d'antenna: base della funzione di ottimizzazione prevista nella fase 5.",
                Link = "github.com/KJ7LNW/xnec2c-optimize",
                License = "GPL-3.0"
            },
            new CreditEntry
            {
                Title = "xnecview",
                Who = "Pieter-Tjerk de Boer PA3FWM",
                What = "Il visualizzatore da cui xnec2c ha preso parte del codice e l'impostazione grafica.",
                Link = "",
                License = "GPL"
            },
            new CreditEntry
            {
                Title = "AutoEZ",
                Who = "Dan Maguire AC6LA",
                What = "Ispirazione per le funzioni delle prossime fasi (variabili, sweep su variabili, Resonate, AutoSeg, linee e reti, " +
                       "snapshot, terreni tipici). Nessun codice di AutoEZ e' usato in YA-NEC.",
                Link = "ac6la.com",
                License = "programma commerciale, solo idee dalla documentazione pubblica"
            },
            new CreditEntry
            {
                Title = "MMANA / MMANA-GAL",
                Who = "Makoto Mori JE3HHT (MMANA, titolare del copyright); Igor Gontcharenko DL2KQ e Alexandr Schewelev DL1PBD (MMANA-GAL, con JE3HHT)",
                What = "Il formato dei file .maa che YA-NEC apre (risegmentati per il NEC2); i file .maa usati come esempi e casi di studio, che restano dei loro autori; " +
                       "una libera interpretazione delle schede di MMANA: tabella dei fili sotto la struttura 3D (una riga per filo), posizione di sorgenti e carichi " +
                       "come filo e punto del filo (notazione w4c) elencati accanto ai fili; dall'help idee per ottimizzatore, stack, Taper wire set e strumenti. " +
                       "Nessuna riga di codice di MMANA: il motore e' NEC-2, non MININEC.",
                Link = "https://hamsoft.ca/pages/mmana-gal.php",
                License = "MMANA-GAL basic: freeware, copyright Makoto Mori JE3HHT; copia e distribuzione libere del programma non modificato riconoscendo il copyright"
            },
            new CreditEntry
            {
                Title = "Letteratura della 0.4.0 (LPDA, stack, multibanda)",
                Who = "K. Rothammel Y21BK e A. Krischke DJ0TR; C. A. Balanis; R. Carrel; Butson e Thompson; G. Hoch DL6WU; P. Viezbicke (NBS TN 688); Cheng e Chen; WA2PHW; DL4AKK",
                What = "Calcolatore LPDA (metodo di Carrel con le curve corrette, Balanis 11.4.3), distanze di stack DL6WU con le aperture dell'antenna base, diametri equivalenti.",
                Link = "",
                License = "formule della letteratura, citate"
            },
            new CreditEntry
            {
                Title = "DK7ZB - Yagi per VHF/UHF",
                Who = "Martin Steyer DK7ZB (progetti fatti con YO di Brian Beezley K6STI e controllati con EZNEC)",
                What = "Longyagi construction principles, Construction details, 7-El. Yagi stacked (caso di studio 7 el. / 2x7 / 4x7 nell'autotest e negli esempi), Duoband-Yagis 2m/70cm principles.",
                Link = "www.qsl.net/dk7zb",
                License = "fonte citata"
            },
            new CreditEntry
            {
                Title = "Yagi multibanda e G/T",
                Who = "ARRL Antenna Book 24a (K9AY, K5RR, Force 12, W3DZZ, W1FBY, W6NL, N6BV, K1VR); SQ9OUB; YT1NT/VE3EA (TANT); AC6LA (XLGTa); KF2YN; VE7BQH; DG7YBN",
                What = "Nota del manuale sulle Yagi multibanda dai 28 MHz in su; metodo della linguetta Rumore/GT/Stack; temperature di riferimento delle tabelle G/T di VE7BQH (vecchio 200 K / 1000 K come TANT, nuovo 290 K con terreno 1600 / 5400 / 14 550 K; 432 MHz 20 K / 350 K da DG7YBN, Dubus 2/2012).",
                Link = "sq9oub.pl.tl",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Terreno di Sommerfeld (GN 2)",
                Who = "G. J. Burke (LLNL), programma SOMNEC; traduzione in C di N. Kyriazis 5B4AZ (nec2c: somnec.c, ground.c)",
                What = "Integrali di Sommerfeld, griglie di interpolazione, onda di terra di Norton: tradotti in C# e verificati con nec2c " +
                       "(Beverage 175 m: Z 701,1 - j146,5 ohm, verticale con radiali a 0,5 m: Z 37,5 - j8,2 ohm, identici).",
                Link = "github.com/KJ7LNW/nec2c",
                License = "GPL-3.0"
            },
            new CreditEntry
            {
                Title = "Calcolatori delle forme semplici (0.4.1)",
                Who = "ARRL Antenna Book; W3DZZ; L. Varney G5RV; G. Countryman W3HH (T2FD); W5QJR e AA5TB (loop magnetica); " +
                      "Martin Steyer DK7ZB (HB9CV, quad); L. B. Cebik W4RNL (MOXGEN); hamuniverse.com (Hentenna); J. Kraus e C. A. Balanis " +
                      "(resistenza di radiazione); ON4UN (four-square); Amidon (AL dei toroidi, amidoncorp.com, tabella ok1dxd); " +
                      "N4DJ, WA2WVL, K6SE, AA7JV, IV3PRK (antenne riceventi e RDF)",
                What = "Formule, misure di partenza e casi di prova dei calcolatori e della linguetta Antenne riceventi; le fonti sono scritte in ogni calcolatore.",
                Link = "",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Novita' della 0.4.3",
                Who = "Luis Mansutti IV3PRK (DHDL e DHDL broadside 2013, Waller Flag e Flag W7IUV 2007, BOG, iv3prk.it); George AA7JV e TX3A (DHDL); " +
                      "Jose Carlos N4IS e Doug Waller NX4D (Waller Flag); Larry W7IUV e Earl Cunningham K6SE (Flag); Tobias DH1TW (descrizione della DHDL); " +
                      "Hanley Strappman AG7T (Ez2Nec: struttura dei file .ez, lettura e scrittura); Dan Maguire AC6LA (formato .weq di AutoEZ, file d'esempio); " +
                      "Frank Lahlum W1MK e Roy Lewallen W7EL (reti L di sfasamento); ON4UN (cap. 11); MMANA-GAL (Taper wire set)",
                What = "Antenne riceventi di IV3PRK nella linguetta Antenne riceventi; Salva con nome in .weq ed .ez; lettura dei carichi, delle linee e del terreno dei .ez; " +
                       "tabella Rastremazioni, Trasformatori, Reti L e calcolatore delle reti di sfasamento negli Oggetti inseriti.",
                Link = "iv3prk.it; github.com/HanleyStrappman/Ez2Nec",
                License = "fonti citate; Ez2Nec: solo la struttura del formato (nessun codice copiato)"
            },
            new CreditEntry
            {
                Title = "Novita' della 0.4.7",
                Who = "Earl Cunningham K6SE e WA1ION (Pennant); IV3PRK (RDF della Pennant); G. J. Burke, LLNL (manuale NEC-4, teoria, 1992; manuale d'uso NEC-4.2, 2011); D. B. Leeson W6NL (Physical Design of Yagi Antennas, 1992); R. Severns N6LF (QEX 2009); Fartookzadeh e altri (Radioengineering 2016); K1GQ (cocoaNEC, pagina NEC4); " +
                      "hf-nec2c (costanti di zint), A. Voors (nec2dxs), T. Molteno (necpp, riferimento per la correzione del terreno di Sommerfeld nella 0.4.38), G1OJS (NECBOL), D. Custura (antsim): teorie riabilitate; Dan Maguire AC6LA (AutoEZ)",
                What = "Calcolatore Pennant nelle Antenne riceventi; lettura dei .weq con fili, sorgenti e carichi esclusi, diametri AWG, funzioni di Excel e carichi RLC; " +
                       "correnti a colori o come curva nella Struttura 3D; promemoria nel salvataggio dai Calcolatori.",
                Link = "qsl.net/wa1ion; k1gq.net",
                License = "fonti citate (solo idee, nessun codice copiato)"
            },
            new CreditEntry
            {
                Title = "Fonti approvate il 13/11/2026",
                Who = "R. Lewallen W7EL (help di EZNEC 7, Stepped Diameter Correction); W8JI e G. Ordy W8WWV (RDF); ON4UN (DMF, cap. 7 e 9, misure DJ8WL); " +
                      "IV3PRK; IK0XBX; ARRL Antenna Book 21a e 24a ed.; K. Rothammel (cap. 24); D. K. Cheng e C. A. Chen; P. P. Viezbicke (NBS TN 688); " +
                      "R. L. Carrel con P. C. Butson e G. T. Thompson; C. A. Balanis (cap. 8, 10, 11); T. A. Milligan",
                What = "Regole della correzione dei diametri a gradini, RDF e DMF, tabelle delle antenne riceventi e delle verticali, test di convergenza e del guadagno medio, " +
                       "spaziature e stack DL6WU, casi di prova delle Yagi e della LPDA, impedenze mutue, bande LF/MF, definizione di G/T. Elenco con l'uso in doc/Fonti_approvate_13-11-2026.md.",
                Link = "",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Registro dei tempi e caso di studio XR-5 (0.4.2)",
                Who = "J. Nielsen (Usability Engineering, 1993) e R. B. Miller (1968); Microsoft (contatori PDH); Force 12 e T. Schiller N6BT (manuale XR-5); A. Voors (4nec2); B. Beezley K6STI (AO, NEC/Wires, YO); YT1NT/VE3EA (TANT)",
                What = "Soglie dei tempi di risposta 0,1 / 1 / 10 s nel punteggio delle funzioni; uso della GPU nel registro (Windows); misure di fabbrica della XR-5 (disegno dwxr5.007); " +
                       "estensioni EX 6, LD 6 e GN 3 e terreni di 4nec2, corpus dei modelli di 4nec2 nel collaudo; formati .yag e .ant di K6STI; regola dell'orizzonte di TANT nel G/T.",
                Link = "",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Sweep rapido, simmetrie, filo isolato, G/T e ottimizzatore (0.4.2)",
                Who = "E. K. Miller e G. J. Burke (MBPE); NEC-2 LLNL e N. Kyriazis 5B4AZ (nec2c); A. Voors (4nec2) e L. B. Cebik W4RNL (amod83); Dragoslav Dobricic YU7EF (EF0213); J. A. Nelder e R. Mead (simplesso, 1965); ARRL N6BV (520-40H.YAG); C. M. Butler (raggio equivalente di una fascia, 1982)",
                What = "Sweep in frequenza con funzioni razionali e punti infittiti dove serve; simmetrie GX e GR sfruttate nella soluzione; LD 7 di 4nec2 (filo isolato) " +
                       "come induttanza distribuita; EF0213 come prova del G/T, con le perdite dei conduttori nella temperatura d'antenna; " +
                       "metodo del simplesso nell'ottimizzatore; formato .YAG dell'ARRL e caso di prova della correzione Leeson; fascia larga nella scheda Loop (larghezza/4).",
                Link = "https://yu7ef.com/ef0213.htm",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Idee dai progetti GitHub attorno a nec2c (0.4.2, mining del 13/11/2026)",
                Who = "G. J. Burke e A. J. Poggio (NEC-2 Part III, NTIS ADA956129); R. Sutherland (hf-nec2c); E. Wheeler KJ7LNW (nec2c); E. S. Harriman AE6TY (nec2c-ae6ty, SimSmith); " +
                      "N. Mladenov (nec2c_opt); Dorin Custura (antsim); SA6MWA (nec2-gnuplot-average-power-gain); C. Hoover (tamago); olgierd (yagi_designer)",
                What = "Esempi ufficiali del manuale NEC-2 nel collaudo e sorgente EX 5 (qdsrc); costanti di zint() come nell'originale; fbar() di Sommerfeld; " +
                       "fattorizzazione LU a blocchi in parallelo; distanza minima superficie-superficie di 5 mm tra fili non collegati; avviso per estremi " +
                       "quasi coincidenti; efficienza in funzione della frequenza; anello di Faraday a maglia uniforme e prova di convergenza nella scheda Loop; " +
                       "rotella del mouse sulle celle numeriche. Solo idee e dati di prova, riscritti da capo: nessuna riga di codice copiata.",
                Link = "https://github.com/sutherlandryan/hf-nec2c",
                License = "BSD-2 (hf-nec2c), GPL-3 (nec2c, tamago), MIT (nec2c-ae6ty, sa6mwa), GPL (nec2c_opt); antsim e yagi_designer senza licenza: solo idee"
            },
            new CreditEntry
            {
                Title = "Tubi, staffe e radiali: fonti approvate il 13/11/2026",
                Who = "D. Leeson W6NL (Physical Design of Yagi Antennas, ARRL 1992; SCALE / EFFLEN.FOR); ARRL Antenna Book 21a ed. (cap. 4, Tab. 7) e 24a ed. (cap. 8); " +
                      "G. H. Brown, R. F. Lewis e J. Epstein (Proc. IRE 1937); J. Sevick W2FMI",
                What = "Correzione dei diametri a gradini e compensazione delle staffe secondo l'algoritmo di Leeson; Yagi 520-40H con e senza correzione come " +
                       "caso di prova; tabelle dei radiali verificate alla fonte (1937) e confrontate nel manuale. Tab. 24.2 del Rothammel scartata il 14/11/2026 (stesse misure di DL6WU, gia' in uso).",
                Link = "https://www.arrl.org/antenna-modeling-files",
                License = "libri e articoli citati"
            },
            new CreditEntry
            {
                Title = "Eggbeater e QFH (calcolatori per i satelliti, 0.4.2)",
                Who = "ON6WG e F5VIF; K5OE; C. Hoover (tamago); J. Coppens (calcolatore QFH); helix2nec (uuki.kapsi.fi); oceantrotter (QFH2nec)",
                What = "Eggbeater: perimetro 1005/f piedi, riflettore a radiali a 1/8 lambda, linea di fase lambda/4, spire accordate in quadratura col NEC e " +
                       "avviso sull'impedenza delle spire che dipende dall'altezza sul riflettore. QFH: formule e tabella del conduttore di Coppens, geometria " +
                       "come helix2nec e QFH2nec. Formule e idee riscritte da capo.",
                Link = "https://www.jcoppens.com/ant/qfh/calc.en.php",
                License = "GPL-3 (tamago, QFH2nec), GPL-2 (qfhcalc.js); articoli citati"
            },
            new CreditEntry
            {
                Title = "Calcolatore Parabola primo fuoco (0.4.2)",
                Who = "Stutzman e Thiele; S. Sengupta; GMRT/NCRA; J. Baars; C.A. Balanis; P. Wade W1GHZ; PA0PLY; J. Ruze; W.W. Mumford; I.E. Idelchik; S.F. Hoerner; Lamb e Olver; Bob Atkins; OE2IGL; DF3GJ; UA3AVR; DJ7FJ; OK1DFC; RF HamDesign",
                What = "Integrazione d'apertura con feed cos^q o calcolato dal NEC, efficienze (spillover, taper, blocco, Ruze, rete), meccanica e vento della rete, contrappeso, bande ed EME. Elenco completo nella pagina Fonti del calcolatore e in CREDITI.txt.",
                Link = "",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Pagina Feed e calcolatore Parabola offset (0.4.12)",
                Who = "P. Wade W1GHZ; W2IMU; OK1DFC; OE5JFL; OK2AQ; RW3BP; Bob Atkins; OE2IGL; VA3RR; A.W. Rudge e N.A. Adatia; JPL TDA 42-128; T.S. Chu e R.H. Turrin; P.-S. Kildal; OM6AA e M. Hazdra; Pawlan e Galuscak; N2UO; SM6FHZ",
                What = "Fase del feed e centro di fase, efficienza in funzione di f/D e della posizione del feed; geometria offset, dime, illuminazione asimmetrica, lobi H e V, squint in circolare, EME. Elenco completo nelle pagine Fonti e in CREDITI.txt.",
                Link = "",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Motore, controlli geometrici, terreni di EZNEC e centine (0.4.20)",
                Who = "E. Anderson e altri (LAPACK Users' Guide, SIAM 1999, zgetrf); G. H. Golub e C. F. Van Loan (Matrix Computations, 2013); Intel (Software Developer's Manual, VFMADDSUB); " +
                      "Roy Lewallen W7EL (EZNEC Pro/2+ 7.0: Geometry Check e terreni, solo il comportamento); A. Voors (4nec2, GN 3); A. Rubinstein, F. Rachidi, M. Rubinstein (IEEE Trans. EMC 2005); P. Wade W1GHZ; prove con EZNEC di IC8TEM",
                What = "Fattorizzazione LU a blocchi con le istruzioni vettoriali; sei controlli geometrici con le regole e le parole di EZNEC; terreno MININEC di EZNEC letto come GN 3 e GN 0 scritto come High Accuracy; " +
                       "parabola a centine come griglia di fili e bordo a poligono nel calcolo d'apertura; calibrazione del collaudo sulle prove EZNEC del 14/11/2026.",
                Link = "netlib.org/lapack/lug",
                License = "fonti citate (solo algoritmi e regole, nessun codice copiato)"
            },
            new CreditEntry
            {
                Title = "Hexbeam 20-10 m (0.4.30)",
                Who = "Steve Hunt G3TXQ (broadband hexbeam); Makoto Kasahara JN1GLB (modello MMANA 6BandBBHEX, 2008); M0OXO (The G3TXQ Broadband Hexagonal Beam); Don Keith N4KC e K4KIO (A Practical 5-Band Wire Beam); MM0OPX-Projects (ScotHex)",
                What = "Geometria a W e a U per banda, misure di JN1GLB, alimentazione in alto con spezzoni di linea lungo il palo; accordo a 50 + j0 ohm banda per banda. Via di IC8TEM del 14/11/2026.",
                Link = "m0oxo.com",
                License = "fonti citate (solo misure e geometria, nessun file copiato)"
            },
            new CreditEntry
            {
                Title = "Elica, verticale e dipolo multibanda, antenne su PCB a 2,4 GHz (0.4.30)",
                Who = "J. D. Kraus (Antennas); D. T. Emerson (NRAO, 1995); G1OJS (NECBOL, MIT); DX Commander (dxcommander.com); J. Devoldere ON4UN (Low-Band DXing); " +
                      "Force 12 e T. Schiller N6BT (manuale XR-5: open sleeve a 3 bande); ARRL Antenna Book (dipoli multipli, manica aperta); H. A. Wheeler (Proc. IRE 1928, bobine) e R. G. Medhurst (Wireless Engineer 1947, capacita' propria) per le trappole; Texas Instruments (AN043, DN007, DN034 / SWRA350); WA5VJB Kent Electronics; W1FVY e W1IJD (Big Wheel, QST 1961); A. C. Ludwig (IEEE Trans. AP-35, 1987); C. A. Balanis; Autodesk (DXF R12)",
                What = "Scheda Elica con esito NEC contro Kraus ed Emerson; verticale a ventaglio con verifica per banda; IFA, MIFA, dipolo, Yagi e Big Wheel su PCB con permittivita' efficace, " +
                       "piano di massa a griglia, accordo col NEC, disegno quotato stampabile a scala 1:1 (anche specchiato) e DXF. Bozzetti 1, 2 e 10 approvati da IC8TEM il 14/11/2026.",
                Link = "dxcommander.com; ti.com; wa5vjb.com",
                License = "fonti citate (solo formule, tipi e regole; disegni e misure calcolati da YA-NEC, nessuna figura copiata)"
            },
            new CreditEntry
            {
                Title = "Fonti aggiunte ai calcolatori (0.4.30)",
                Who = "L. G. Windom W8GZ (QST 1929); F. Spillner DJ2KY (FD4, QRV 1971); Belrose e Bouliane (QST 1990); C. L. Buchanan W3DZZ (QST 1955); Y. Someya JE1DEU (Hentenna, 1972); " +
                      "C. E. Smith e E. M. Johnson (Proc. IRE 1947); R. Severns N6LF (QEX 2013); G. Guanella (1944), W. K. Ruthroff (1959), J. Sevick (Transmission Line Transformers); ARRL Antenna Book 19ª e 24ª ed. (spaziatura dei parassiti)",
                What = "OCF/Windom, dipolo trappolato, Hentenna, verticali accorciata e 5/8, balun, spaziatura della Yagi a due elementi. Ricerca in rete del 15/11/2026 chiesta da IC8TEM.",
                Link = "hamwaves.com/cl-ocfd/history.html; highfrequencyelectronics.com; antennasbyn6lf.com",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Arcarecci diritti e dima delle centine della parabola (0.4.21)",
                Who = "Norme EN 755-2, EN 1999-1-1 (Eurocodice 9), EN 10025-2, EN 13706-3; R. J. Roark e W. C. Young (Roark's Formulas for Stress and Strain, tab. 8.1); Autodesk (formato DXF R12)",
                What = "Scelta del tubo piu' leggero per ogni anello di arcarecci diritti (vento, peso, frecce, ghiaccio), masse distribuite nel baricentro; dima della centina a scala 1:1 in PDF a fogli A4 e in DXF. Richieste di IC8TEM del 14/11/2026.",
                Link = "",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Trombe e illuminatori (0.4.19)",
                Who = "P. Wade W1GHZ (Online Microwave Antenna Book cap. 6.3, 6.5, 12; VE4MA and Chaparral feeds with Septum Polarizers, misure WD5AGO); C. A. Balanis (Antenna Theory, 13.4.3 e 13.6); P. D. Potter (dual-mode, 1963); Silver e Chu; VA3RR (W2IMU in 4nec2)",
                What = "Dimensionamento per banda di coffee-can, VE4MA, Super-VE4MA, Chaparral, W2IMU, conica e piramidale; diagramma d'apertura; efficienza sulla parabola; modello NEC a griglia di fili. Scheda preparata da IC8TEM il 14/11/2026.",
                Link = "w1ghz.org",
                License = "fonti citate"
            },
            new CreditEntry
            {
                Title = "Fili vicini al terreno, BOG e modelli pesanti (0.4.18)",
                Who = "I. Pellejero, \"Notes on antenna simulation using NEC-2\" (ipellejero.es); L. B. Cebik W4RNL, \"S-N, RCA, and MININEC Grounds\" e \"A Ground Is Just a Ground\" (antenna2.github.io/cebik); " +
                      "R. Fry (NEC-2 User's Guide sui terreni, radiodiscussions.com, 2012); G. J. Burke e A. J. Poggio, manuale NEC-2; K1FZ (note sul BOG, qsl.net/k1fz); N4DJ; IV3PRK (BOG 63 m); " +
                      "Larry KE5KJD (Butternut HF2V con 64 radiali, MMANA); Ken Nist e holl_ands (Bowtie 91XG) e il modello 4x2SA13, raccolta di 4nec2 (A. Voors); nec2c (N. Kyriazis 5B4AZ) come riferimento",
                What = "Quote minime dei fili sul terreno (lambda/10 con GN 0, lambda/1000 con Sommerfeld), massimo di circa 30 fili per giunzione, NEC-2 senza fili interrati e isolanti; " +
                       "BOG approssimato a lambda/1000 con le avvertenze; impedenza reale del BOG 200-300 ohm; Butternut corretta; i tre modelli pesanti della fase 2E confrontati con nec2c.",
                Link = "ipellejero.es/tecnico/nec-2/english.php; antenna2.github.io/cebik/content/amod/amod99.html; qsl.net/k1fz/bognotes.html",
                License = "fonti citate (solo dati e regole, nessun codice copiato)"
            },
            new CreditEntry
            {
                Title = "Avalonia UI 11.2.3 e .NET 8",
                Who = "Team di Avalonia UI; Microsoft e .NET Foundation",
                What = "L'interfaccia multipiattaforma (Windows, Linux, Mac) e l'ambiente di esecuzione.",
                Link = "github.com/AvaloniaUI/Avalonia - dotnet.microsoft.com",
                License = "MIT"
            },
            new CreditEntry
            {
                Title = "Carattere Inter",
                Who = "Rasmus Andersson",
                What = "Il carattere dell'interfaccia.",
                Link = "rsms.me/inter",
                License = "SIL Open Font License 1.1"
            },
            new CreditEntry
            {
                Title = "Yagi lunga DL6WU, Quagi, file di K6STI (0.5.0)",
                Who = "G. Hoch DL6WU (\"Extremely Long Yagi Antennas\", VHF Communications 3/1982; con PA0MS, VERON); J. Haigwood KY4Z e B. Stein W6NBI (programma BASIC 1983-86), " +
                      "I. White G3SEK (modifiche 1986 e correzione del boom); R. Rodrigues (trascrizione in Python, github.com/ricardorodrigues-ca/yagi-antenna-calculator, MIT); " +
                      "W. Overbeck N6NB (Quagi, QST 4/1977 e 2/1978); L. B. Cebik W4RNL (\"Quagi and Yagi on 2 Meters\", \"Under the Limits: MININEC\", \"When MININEC is Superior to NEC\"); " +
                      "G. Marcus W3PM (QST 1/1990, tabelle delle quagi); C. Angle N6CA e M. Walters G3JVL (loop Yagi 1296 MHz, ARRL Antenna Book 24a ed. sez. 15.3.7); J. Johnson G0KSC (LFA, misure del 6 el. 144 MHz); DG7YBN e M. Steyer DK7ZB (radiatore piegato, \"Design principles OWL\"); " +
                      "B. Beezley K6STI (ANTFILE.DOC, 1995); ITU-R P.2040 e Perotoni, Vieira, dos Santos e Almeida (ACES Journal 34(10), 2019) per i solai in calcestruzzo",
                What = "Calcolatori Yagi lunga DL6WU e Quagi con l'ottimizzazione a passi (formule riscritte in C#, nessun codice copiato); lettore .ANT completo; paragrafo MININEC e NEC-2 del manuale.",
                Link = "github.com/ricardorodrigues-ca/yagi-antenna-calculator; antenna2.github.io/cebik; qsl.net/dk7zb",
                License = "formule e misure pubblicate (citate); trascrizione Python con licenza MIT, usata solo come lettura delle formule"
            },
            new CreditEntry
            {
                Title = "Controllo dei dati (0.5.1)",
                Who = "W. W. Hansen e J. R. Woodyard (\"A New Principle in Directional Antenna Design\", Proc. IRE 26(3), 1938: massimo di una schiera end-fire); " +
                      "G. J. Burke e A. J. Poggio (NEC-2 Part III, sez. II); R. Lewallen W7EL (EZNEC: Geometry Check, AGT); R. L. Carrel e C. A. Balanis (tau e sigma delle LPDA); " +
                      "L. B. Cebik W4RNL (MOXGEN); M. Kasahara JN1GLB (hexbeam); ARRL Antenna Book (quad); G. Hoch DL6WU (stack); IARU Regione 1 (bande)",
                What = "45 controlli dei dati approvati da IC8TEM il 15/11/2026: soglie e spiegazioni nel manuale, capitolo 11.",
                Link = "",
                License = "soglie e formule pubblicate (citate), nessun codice copiato"
            },
            new CreditEntry
            {
                Title = "Cavi, cambio d'impedenza e balun (0.5.3)",
                Who = "C. Cerrotta IC8TEM (database dei cavi di YAEC, con le schede di SSB-Electronic, Messi & Paoloni, Times Microwave, CommScope Andrew, RFS, Belden, Prospecta, Qaxial, Siva Cavi, Tasker, Caledonian, SV1BSX, DD1US/EA5IP); " +
                      "YB0EMJ e W4ZT (tabelle dei coassiali da 75, 93, 95, 125, 185 ohm); DX Engineering (linea LL300); Electronics World 1965 via RF Cafe (piattina 300 ohm); KV5R (VF della linea a finestra); ARRL Antenna Book; " +
                      "G. Guanella (1944), W. K. Ruthroff (1959), J. Sevick (Transmission Line Transformers); Amidon e OK1DXD (AL dei nuclei)",
                What = "Elenco dei cavi da 29 a 84 linee, cambio d'impedenza con un tratto di cavo nella scheda Cavi RG, balun con tutti i rapporti e il disegno di costruzione.",
                Link = "github.com/ic8tem/yaec; qsl.net/yb0emj/coax.html; w4zt.wa4dsy.com/coax.html; dxengineering.com; qsl.net/ok1dxd/amidon.htm; amidoncorp.com",
                License = "valori e formule pubblicati (citati); YAEC e' dello stesso autore di YA-NEC"
            },
        };

        public const string Intro =
            "YA-NEC esiste grazie al lavoro di colleghi radioamatori e ricercatori che hanno reso pubblico il loro codice. " +
            "Un ringraziamento sentito a ciascuno di loro.";
    }
}
