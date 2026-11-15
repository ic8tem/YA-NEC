@echo off
rem YA-NEC - UN SOLO BAT PER TUTTE LE PROVE (0.4.22, richiesta di IC8TEM del 14/11/2026 20:32: "un solo bat con tutte le prove ...
rem tutto in un unico bat, tutta una unica procedura. da ora in poi sempre cosi'").
rem Compila e fa tutto in coda, senza fermarsi se una prova non e' superata:
rem   2A autotest del motore (YA-NEC da solo)            2B collaudo delle funzioni (comprese parabole, arcarecci e centine)
rem   2C prove dell'interfaccia                           2D Butternut   2E modelli pesanti   2F modelli sensibili
rem   2G NEC di tutte le sottolinguette dei calcolatori (e il disco a centine della Parabola) salvati in .ez e riletti
rem   2H EZNEC calcola quei .ez e si confrontano con YA-NEC
rem   2I EZNEC calcola i modelli d'archivio che non combaciavano oltre lo 0,1 %   2J YA-NEC li rilegge e li confronta
rem Alla fine c'e' un solo file da mandare: yanec-collaudo-<versione>.zip. Prima: chiudi EZNEC (lo apre lo script), non usare mouse,
rem tastiera e appunti durante le prove con EZNEC. Durata: circa un'ora.
cd /d "%~dp0"
call compila.bat /completo %*
