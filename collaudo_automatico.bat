@echo off
rem ===========================================================================================
rem YA-NEC - COLLAUDO AUTOMATICO SENZA PRESIDIO (0.5.1, richiesta di IC8TEM del 15/11/2026 16:34-16:35:
rem "se mi consegni uno zip con un bat, fa tutto lui in automatico?" ... "ok questo procedi gia'").
rem
rem Cosa fa, da solo, dall'inizio alla fine (circa un'ora e mezza):
rem   1. compila e fa il collaudo COMPLETO, come prova_tutto.bat: autotest del motore, tutte le procedure (compresa la nuova
rem      "Novita' della 0.5.1: controllo dei dati"), prove dell'interfaccia, Butternut, modelli pesanti e sensibili, e tutte le fasi
rem      con EZNEC vero (2G-2N: modelli dei calcolatori, archivio, elementi a gradini, varianti vicino al terreno);
rem   2. non si ferma mai: niente pause e niente "premi un tasto", anche se una prova non e' superata (la segna e va avanti);
rem   3. alla fine scrive ESITO.txt: SUPERATO o NON SUPERATO, il riepilogo delle fasi e i passi con ERRORE o AVVISO;
rem      ESITO.txt va anche dentro yanec-collaudo-<versione>.zip;
rem   4. SOLO SE IL COLLAUDO E' SUPERATO prepara il pacchetto da pubblicare (compilazione autonoma, compila.bat /distribuzione /solo):
rem      pubblicazione\YA-NEC-<versione>-win-x64.zip e .sha256. Il pacchetto e' pronto ma NON viene pubblicato: il via lo da' IC8TEM.
rem
rem Prima di partire: chiudi EZNEC (lo apre lo script) e gli altri programmi; disattiva la sospensione del PC.
rem Durante il collaudo: non usare mouse, tastiera e appunti (le fasi con EZNEC li usano).
rem All'avvio chiede una conferma; se nessuno risponde, dopo 30 secondi parte da solo.
rem
rem   collaudo_automatico.bat            collaudo completo, il PC resta acceso alla fine
rem   collaudo_automatico.bat /spegni    come sopra, poi spegne il PC (2 minuti dopo la fine: "shutdown /a" per annullare)
rem
rem Al ritorno: mandare yanec-collaudo-<versione>.zip (con dentro ESITO.txt).
rem ===========================================================================================
setlocal EnableExtensions
cd /d "%~dp0"
chcp 65001 >nul
set /p VER=<VERSION
set "SPEGNI="
for %%a in (%*) do if /i "%%~a"=="/spegni" set "SPEGNI=1"
echo.
echo COLLAUDO AUTOMATICO DI YA-NEC %VER% (completo, con EZNEC, circa un'ora e mezza)
echo Chiudi EZNEC, disattiva la sospensione del PC e non usare mouse e tastiera fino alla fine.
if defined SPEGNI echo Alla fine il PC si spegne da solo.
echo.
choice /c SN /t 30 /d S /m "Avvio il collaudo senza presidio? S = si', N = no (dopo 30 secondi parte da solo)"
if errorlevel 2 exit /b 0
set "SENZA_PAUSA=1"
if exist "ESITO.txt" del "ESITO.txt"
call compila.bat /completo
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
