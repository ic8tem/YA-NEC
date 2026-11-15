@echo off
rem YA-NEC - TUTTE LE PROVE SENZA APRIRE EZNEC (0.4.39, richiesta di IC8TEM del 15/11/2026 13:25)
rem Come prova_tutto.bat, ma le fasi 2H, 2I, 2K e 2M usano i risultati di EZNEC salvati del collaudo 0.4.37
rem (data\prove\eznec_salvati): EZNEC con gli stessi .ez da' gli stessi numeri, cambia solo YA-NEC. Serve quando si tocca il motore.
rem Quando cambiano i modelli o il modo di esportarli in .ez si usa di nuovo prova_tutto.bat. Durata: circa 15-20 minuti.
cd /d "%~dp0"
call compila.bat /completo /senza_eznec %*
