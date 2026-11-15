@echo off
rem ===========================================================================================
rem YA-NEC - COLLAUDO RAPIDO SENZA PRESIDIO (0.5.4, richiesta di IC8TEM del 15/11/2026 23:30: "mi riscrivi una versione 0.5.4 solo
rem con i collaudi necessari?"). Come collaudo_automatico.bat, ma solo le prove che servono quando cambia la correzione dei tubi telescopici:
rem   compilazione, 2A autotest del motore, 2B tutte le procedure (valori fissi di Leeson: 520-40H, XR-5, limite, novita'),
rem   2J confronto d'archivio, 2K-2L elementi a gradini, 2L bis taratura di Leeson, con i valori di EZNEC salvati (EZNEC non si apre).
rem Saltate: 2C interfaccia, 2D Butternut, 2E modelli pesanti, 2F modelli sensibili, 2G-2H calcolatori, 2M-2N varianti.
rem Circa 20 minuti. Alla fine ESITO.txt (anche nello zip) e, se superato, il pacchetto da pubblicare (non pubblicato).
rem Il collaudo completo resta collaudo_automatico.bat (da fare prima di una pubblicazione con modifiche all'interfaccia).
rem   collaudo_rapido.bat            collaudo rapido
rem   collaudo_rapido.bat /spegni    come sopra, poi spegne il PC
rem ===========================================================================================
setlocal EnableExtensions
cd /d "%~dp0"
chcp 65001 >nul
set /p VER=<VERSION
set "SPEGNI="
for %%a in (%*) do if /i "%%~a"=="/spegni" set "SPEGNI=1"
echo.
echo COLLAUDO RAPIDO DI YA-NEC %VER% (autotest, funzioni, archivio e gradini con i valori EZNEC salvati, circa 20 minuti)
echo EZNEC non serve. Disattiva la sospensione del PC.
if defined SPEGNI echo Alla fine il PC si spegne da solo.
echo.
choice /c SN /t 30 /d S /m "Avvio il collaudo senza presidio? S = si', N = no (dopo 30 secondi parte da solo)"
if errorlevel 2 exit /b 0
set "SENZA_PAUSA=1"
if exist "ESITO.txt" del "ESITO.txt"
call compila.bat /completo /rapido
rem esito, riepilogo e passi da guardare in ESITO.txt (anche nello zip del collaudo)
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\esito_collaudo.ps1" -Rep "collaudo_completo.txt" -Uscita "ESITO.txt" -Zip "yanec-collaudo-%VER%.zip" -Versione "%VER%"
set "RCE=%ERRORLEVEL%"
if not "%RCE%"=="0" goto :fine
echo.
echo COLLAUDO SUPERATO: preparo il pacchetto da pubblicare (compilazione autonoma, qualche minuto).
call compila.bat /distribuzione /solo
if exist "pubblicazione\YA-NEC-%VER%-win-x64.zip.sha256" (
  >>"ESITO.txt" echo.
  >>"ESITO.txt" echo Pacchetto pronto: pubblicazione\YA-NEC-%VER%-win-x64.zip e .sha256 ^(non pubblicato^).
) else (
  >>"ESITO.txt" echo.
  >>"ESITO.txt" echo ATTENZIONE: il pacchetto da pubblicare non e' stato creato ^(vedi compila_distribuzione.txt^).
)
powershell -NoProfile -Command "Compress-Archive -LiteralPath 'ESITO.txt','compila_distribuzione.txt' -DestinationPath 'yanec-collaudo-%VER%.zip' -Update -ErrorAction SilentlyContinue" >nul 2>&1
:fine
echo.
type "ESITO.txt"
echo.
echo Mandare il file yanec-collaudo-%VER%.zip
if defined SPEGNI shutdown /s /t 120 /c "YA-NEC: collaudo automatico finito, il PC si spegne tra 2 minuti (shutdown /a per annullare)."
exit /b 0
