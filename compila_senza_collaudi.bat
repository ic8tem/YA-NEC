@echo off
rem YA-NEC - COMPILAZIONE SENZA COLLAUDI (0.4.35, richiesta di IC8TEM del 15/11/2026 11:36: "preparami la 0.4.35 senza i collaudi per
rem farla provare ad un amico"). Compila la versione autonoma (gira anche senza .NET installato) e prepara il pacchetto
rem pubblicazione\YA-NEC-<versione>-win-x64.zip, senza registri, impostazioni e file di prova: e' lo zip da dare all'amico.
rem Nessuna prova, nessun EZNEC. Durata: qualche minuto.
cd /d "%~dp0"
call compila.bat /distribuzione /solo %*
echo.
if exist "pubblicazione\YA-NEC-*-win-x64.zip" (echo Pacchetto pronto nella cartella pubblicazione.) else echo Pacchetto non creato: guarda compila_log.txt
pause
