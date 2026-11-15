@echo off
rem ===========================================================================================
rem YA-NEC - COLLAUDO MIRATO (0.5.0, richiesta di IC8TEM del 15/11/2026 16:00: "mi fai fare prima un collaudo solo dei modelli piu'
rem discostanti e dei modelli che escono dalle schede del calcolatore")
rem
rem Cosa fa, in ordine (EZNEC vero, non i valori salvati):
rem   1. compila YA-NEC
rem   2G. per ogni sottolinguetta dei Calcolatori crea il modello NEC con i dati iniziali della scheda (anche Yagi DL6WU con dipolo
rem       aperto, chiuso, LFA e piegato DG7YBN, Quagi, quad, loop Yagi 1296), lo salva in .nec e in .ez, rilegge il .ez e lo ricalcola
rem   2H. EZNEC calcola quei .ez: si confronta con YA-NEC, entro lo 0,5 %
rem   2I. EZNEC calcola i 160 modelli d'archivio che nel collaudo 0.4.40 non stavano entro lo 0,1 % (elenco: data\prove\eznec_archivio\mirati.txt)
rem   2J. YA-NEC rilegge quei modelli, li calcola e li confronta con EZNEC (classi degli scarti in fondo)
rem   2K. gli elementi a gradini di quei modelli, uno per uno in .ez, calcolati da EZNEC
rem   2L. YA-NEC li confronta con la sua correzione di Leeson (com'e', forzata, senza)
rem Alla fine: yanec-collaudo-<versione>.zip da mandare.
rem Prima di partire: chiudi EZNEC (lo apre lo script). Durante le prove con EZNEC non usare mouse, tastiera e appunti.
rem Durata: circa 20-25 minuti. Per tutte le prove (autotest, funzioni, interfaccia, Butternut, modelli pesanti e sensibili,
rem varianti e l'archivio intero) c'e' prova_tutto.bat (circa un'ora).
rem ===========================================================================================
cd /d "%~dp0"
call compila.bat /mirato %*
