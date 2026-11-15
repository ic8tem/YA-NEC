@echo off
rem ===========================================================================================
rem YA-NEC - COMPILAZIONE E COLLAUDO per Windows (dalla 0.3.3: 3 fasi, nessuna pausa intermedia)
rem   compila.bat                 compilazione veloce (usa il .NET gia' installato) + collaudo completo
rem   compila.bat /distribuzione  compilazione autonoma (gira anche senza .NET) + collaudo completo
rem   compila.bat /distribuzione /solo  solo compilazione e pacchetto da dare ad altri, senza collaudo (0.4.35, compila_senza_collaudi.bat)
rem   compila.bat /completo /senza_eznec  tutte le prove, ma i valori di EZNEC sono quelli salvati (data\prove\eznec_salvati, collaudo 0.4.37): EZNEC non si apre (0.4.39, prova_senza_eznec.bat)
rem   compila.bat /completo /rapido  (0.5.4, collaudo_rapido.bat) solo i collaudi che servono quando cambia il motore: 2A, 2B completo, 2J archivio, 2K-2L-2L bis
rem                        gradini e taratura di Leeson, con i valori di EZNEC salvati (EZNEC non si apre); saltate 2C-2H e 2M-2N (circa 20 minuti)
rem   compila.bat /mirato  (0.5.0, prova_mirata.bat) solo i modelli dei calcolatori e i modelli d'archivio che si discostano da EZNEC
rem                        (elenco in data\prove\eznec_archivio\mirati.txt), con EZNEC vero: fasi 2G, 2H, 2I, 2J, 2K, 2L
rem   collaudo_automatico.bat (0.5.1) = compila.bat /completo senza la pausa finale, poi ESITO.txt e, se superato, il pacchetto /distribuzione
rem   dalla 0.4.13 il collaudo prova solo le novita' dopo la 0.4.10; con /completo prova tutto (anche: compila.bat /distribuzione /completo)
rem Fasi: 1 compilazione  2 collaudo (2A autotest, 2B funzioni, 2C interfaccia, 2D Butternut, 2E modelli pesanti, 2F modelli sensibili,
rem        2G modelli dei calcolatori in .nec/.ez, 2H prove con EZNEC, 2I EZNEC sui modelli d'archivio, 2J confronto d'archivio, 2K-2L elementi a gradini, 2M-2N varianti vicino al terreno)  3 riepilogo
rem 0.4.22 (IC8TEM, 14/11/2026 20:32, "un solo bat con tutte le prove ... tutto in un unico bat, tutta una unica procedura. da ora in poi
rem sempre cosi'"): tutte le prove in coda, sempre, anche se una prima non e' superata; prova_tutto.bat = compila.bat /completo.
rem Tutto va anche in collaudo_completo.txt; alla fine si manda yanec-collaudo-<versione>.zip (resoconto e registri).
rem ===========================================================================================
setlocal EnableExtensions
cd /d "%~dp0"
chcp 65001 >nul
set "MODO=veloce"
set "AMPIEZZA=--solo-novita"
set "DESCR=solo le novita' dopo la 0.4.10"
for %%a in (%*) do (
  if /i "%%~a"=="/distribuzione" set "MODO=distribuzione"
  if /i "%%~a"=="/completo" (set "AMPIEZZA=" & set "DESCR=completo")
  if /i "%%~a"=="/solo" set "SOLO=1"
  if /i "%%~a"=="/senza_eznec" set "SENZAEZ=1"
  if /i "%%~a"=="/rapido" (set "RAPIDO=1" & set "SENZAEZ=1" & set "AMPIEZZA=" & set "DESCR=rapido: autotest, tutte le funzioni, archivio e gradini con i valori EZNEC salvati")
  if /i "%%~a"=="/mirato" (set "MIRATO=1" & set "DESCR=mirato: calcolatori e modelli che si discostano da EZNEC")
)
if "%MODO%"=="veloce" (set "OUT=build\win-x64") else set "OUT=pubblicazione\win-x64"
set "EXE=%OUT%\YA-NEC.exe"
set "REP=collaudo_completo.txt"
rem 0.5.1: la compilazione /solo (pacchetto da dare) ha il suo resoconto, cosi' non cancella quello del collaudo
if defined SOLO set "REP=compila_distribuzione.txt"
set /p VER=<VERSION
set "ESITO_COMP=non eseguita"
set "ESITO_AUTO=non eseguito"
set "ESITO_COLL=non eseguito"
set "ESITO_REG=non eseguite"
set "ESITO_BUT=non eseguita"
set "ESITO_PES=non eseguiti"
set "ESITO_SEN=non eseguiti"
set "ESITO_EZE=non eseguita"
set "ESITO_EZN=non eseguite"
set "ESITO_EZA=non eseguite"
set "ESITO_ARC=non eseguito"
set "ESITO_STP=non eseguita"
set "ESITO_EZG=non eseguite"
set "ESITO_GRA=non eseguito"
set "ESITO_TAR=non eseguita"
set "ESITO_EZV=non eseguite"
set "ESITO_VAR=non eseguito"
set "T0=%TIME%"

> "%REP%" echo COLLAUDO DI YA-NEC %VER% - compilazione %MODO% - collaudo %DESCR% - %DATE% %TIME%
>>"%REP%" echo Computer: %COMPUTERNAME%   Sistema: %OS%   Processore: %PROCESSOR_ARCHITECTURE%
>>"%REP%" echo ===========================================================================================

rem 0.4.30 (IC8TEM 14/11/2026 23:14, "per questo applicativo non voglio dati in appdata"): file di lavoro in tmp_compila accanto al bat, non nella cartella temporanea di Windows
set "TMPC=%CD%\tmp_compila"
if not exist "%TMPC%" mkdir "%TMPC%"
rem 0.4.30 (IC8TEM 15/11/2026 07:31): esempi in .yanec e modelli scartati. Uno zip estratto sopra la cartella vecchia lascerebbe i file
rem nel formato vecchio e quelli tolti: si cancellano (elenco in tools\file_tolti_0.4.30.txt) nel sorgente e nella cartella del programma.
if exist "tools\file_tolti_0.4.30.txt" powershell -NoProfile -Command "$n=0; Get-Content -LiteralPath 'tools\file_tolti_0.4.30.txt' -Encoding UTF8 | ForEach-Object { $r=$_.Trim(); if ($r) { foreach ($b in @('data', '%OUT%\data')) { $p=Join-Path $b $r; if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Force; $n++ } } } }; Add-Content -LiteralPath '%REP%' -Value ('Pulizia dei file vecchi (formato .yanec e modelli scartati, 0.4.30): ' + $n + ' file tolti')"
rem ----------------------------------------------------------------------------- FASE 1
call :titolo 1 "COMPILAZIONE (%MODO%)"
where dotnet >nul 2>&1
if errorlevel 1 (
  call :scrivi "ERRORE: il comando dotnet non esiste. Installare .NET 8 SDK da https://dotnet.microsoft.com/download"
  goto :riepilogo
)
dotnet --list-sdks > "%TMPC%\yanec_sdk.txt" 2>&1
call :scrivi "SDK installati (serve almeno uno 8.x o successivo):"
type "%TMPC%\yanec_sdk.txt"
type "%TMPC%\yanec_sdk.txt" >> "%REP%"
rem dalla 0.4.2: file unico (YA-NEC.exe con dentro le librerie .NET), librerie native nella sottocartella lib
rem toglie le librerie delle compilazioni vecchie (non i dati: modelli e impostazioni restano)
if exist "%OUT%" del /q "%OUT%\*.dll" "%OUT%\*.pdb" "%OUT%\*.deps.json" "%OUT%\*.runtimeconfig.json" >nul 2>&1
if "%MODO%"=="veloce" (
  call :scrivi "Comando: dotnet publish YA-NEC.csproj -c Release -r win-x64 --self-contained false -o %OUT%"
  dotnet publish YA-NEC.csproj -c Release -r win-x64 --self-contained false -o "%OUT%" > compila_log.txt 2>&1
) else (
  call :scrivi "Comando: dotnet publish YA-NEC.csproj -c Release -r win-x64 --self-contained true -o %OUT%"
  call :scrivi "Attendere: la compilazione autonoma richiede qualche minuto."
  dotnet publish YA-NEC.csproj -c Release -r win-x64 --self-contained true -o "%OUT%" > compila_log.txt 2>&1
)
if errorlevel 1 (
  set "ESITO_COMP=NON RIUSCITA"
  call :scrivi "*** COMPILAZIONE NON RIUSCITA. Righe di errore:"
  findstr /i /c:": error" compila_log.txt
  findstr /i /c:": error" compila_log.txt >> "%REP%"
  call :scrivi "Il resoconto completo e' in compila_log.txt"
  goto :riepilogo
)
set "ESITO_COMP=riuscita"
rem avvisi del compilatore: righe con ": warning" (in italiano ": avviso"), senza doppioni
set "NWARN=0"
findstr /i /c:": warning" /c:": avviso" compila_log.txt > "%TMPC%\yanec_warn.txt" 2>nul
for /f %%n in ('type "%TMPC%\yanec_warn.txt" ^| sort /unique 2^>nul ^| find /c /v ""') do set "NWARN=%%n"
call :scrivi "Compilazione riuscita. Avvisi del compilatore: %NWARN% (non bloccano)."
if not "%NWARN%"=="0" (
  type "%TMPC%\yanec_warn.txt" | sort /unique
  type "%TMPC%\yanec_warn.txt" | sort /unique >> "%REP%"
)
call :controlla "%EXE%" "programma YA-NEC.exe"
call :controlla "%OUT%\data\modelli" "cartella dei modelli di esempio"
call :controlla "%OUT%\data\cavi.csv" "abaco dei cavi"
call :controlla "%OUT%\data\manuale\Manuale.md" "manuale"
call :controlla "%OUT%\lib" "cartella lib delle librerie native"
rem nessuna DLL accanto a YA-NEC.exe (richiesta del 13/11/2026)
set "NDLL=0"
for %%d in ("%OUT%\*.dll") do set /a NDLL+=1
if "%NDLL%"=="0" (call :scrivi "OK      nessuna DLL nella cartella del programma") else (
  call :scrivi "AVVISO  DLL ancora nella cartella del programma: %NDLL%"
  dir /b "%OUT%\*.dll" 2>nul
  dir /b "%OUT%\*.dll" >> "%REP%" 2>nul
)
if exist "%OUT%\lib" (
  call :scrivi "Contenuto di lib:"
  dir /b "%OUT%\lib" 2>nul
  dir /b "%OUT%\lib" >> "%REP%" 2>nul
)
rem 0.4.15: con /distribuzione si prepara anche il pacchetto per la release del GitHub (aggiornamento automatico):
rem pubblicazione\YA-NEC-<versione>-win-x64.zip e la sua impronta .sha256, senza config.ini, registri e file del collaudo
if "%MODO%"=="distribuzione" if exist "%EXE%" (
  powershell -NoProfile -Command "$s=Join-Path '%TMPC%' 'yanec_rel'; Remove-Item $s -Recurse -Force -ErrorAction SilentlyContinue; Copy-Item '%OUT%' $s -Recurse; Remove-Item (Join-Path $s 'Registri'),(Join-Path $s 'data\config.ini'),(Join-Path $s 'data\aggiornamenti'),(Join-Path $s 'data\collaudo_immagini'),(Join-Path $s 'data\tmp'),(Join-Path $s 'data\prove'),(Join-Path $s 'data\prove_*.txt'),(Join-Path $s 'data\*.txt') -Recurse -Force -ErrorAction SilentlyContinue; $z='pubblicazione\YA-NEC-%VER%-win-x64.zip'; Compress-Archive -Path (Join-Path $s '*') -DestinationPath $z -Force; ((Get-FileHash $z -Algorithm SHA256).Hash.ToLower() + '  YA-NEC-%VER%-win-x64.zip') | Set-Content -Encoding ascii ($z + '.sha256')"
  if exist "pubblicazione\YA-NEC-%VER%-win-x64.zip.sha256" (call :scrivi "OK      pacchetto per il GitHub: pubblicazione\YA-NEC-%VER%-win-x64.zip e .sha256 (da allegare alla release v%VER%)") else call :scrivi "AVVISO  pacchetto per il GitHub non creato"
)
rem 0.4.37 (collaudo 0.4.36: un commento mio chiudeva una graffa dello script EZNEC e tutte le fasi 2H-2O sono saltate): controllo della
rem sintassi degli script PowerShell prima delle prove, con le righe sbagliate nel resoconto
powershell -NoProfile -Command "$e=$null; $t=$null; $n=0; Get-ChildItem tools\eznec\*.ps1 | ForEach-Object { [void][System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$t, [ref]$e); foreach ($x in $e) { $n++; Write-Output ('ERRORE  ' + $_.Name + ' riga ' + $x.Extent.StartLineNumber + ': ' + $x.Message) } }; if ($n -eq 0) { Write-Output 'OK      sintassi degli script EZNEC' }" > "%TMPC%\yanec_ps1.txt" 2>&1
type "%TMPC%\yanec_ps1.txt"
type "%TMPC%\yanec_ps1.txt" >> "%REP%"
if not exist "%EXE%" goto :riepilogo
rem 0.4.35 (IC8TEM 15/11/2026 11:36, "preparami la 0.4.35 senza i collaudi per farla provare ad un amico"): con /solo niente collaudo
if defined SOLO (
  call :scrivi "Senza collaudo (/solo): il programma e' in %OUT%; il pacchetto da dare e' pubblicazione\YA-NEC-%VER%-win-x64.zip"
  goto :riepilogo
)

rem ----------------------------------------------------------------------------- FASE 2
rem 0.4.18 (richiesta di IC8TEM del 14/11/2026 09:17, "razionalizzare"): ogni prova passa da un solo sottoprogramma (:prova) che
rem avvia YA-NEC, accoda il resoconto e scrive l'esito; prima ogni prova ripeteva le stesse righe.
rem   2A autotest del motore   2B collaudo delle funzioni   2C prove dell'interfaccia
rem   2D Butternut (YA-NEC si riapre e la carica)   2E i 3 modelli piu' pesanti (tempi del motore)   2D e 2E solo se 2A, 2B e 2C sono superate
if defined MIRATO goto :mirato
call :titolo 2 "COLLAUDO: autotest del motore, funzioni, interfaccia, Butternut, modelli pesanti"
call :scrivi "Tutto automatico. Durante le prove dell'interfaccia la finestra di YA-NEC si apre e si chiude da sola: non toccare mouse e tastiera."
if defined AMPIEZZA call :scrivi "Si provano solo le novita' dopo la 0.4.10 (pochi minuti); compila.bat /completo prova tutto (piu' di mezz'ora)."
if not defined AMPIEZZA call :scrivi "Collaudo completo: ogni linguetta, ogni fumetto, ogni pulsante, ogni casella e tutti i modelli (piu' di mezz'ora)."
call :scrivi "Poi, sempre (dalla 0.4.22 tutte in coda): 2D Butternut, 2E modelli pesanti, 2F modelli sensibili, 2G calcolatori in .nec/.ez, 2H EZNEC sui calcolatori, 2I EZNEC sull'archivio, 2J confronto d'archivio, 2K EZNEC sugli elementi a gradini, 2L confronto dei gradini, 2M EZNEC sulle varianti vicino al terreno, 2N confronto delle varianti."
for %%f in (autotest.txt collaudo.txt prove_interfaccia.txt prove_butternut.txt prove_pesanti.txt prove_sensibili.txt fumetti.txt registro.txt) do if exist "%OUT%\data\%%f" del "%OUT%\data\%%f"
if exist "%OUT%\Registri" rmdir /s /q "%OUT%\Registri"

call :prova 2A "AUTOTEST DEL MOTORE (confronto con nec2c)" "--autotest" autotest.txt ESITO_AUTO superato "NON SUPERATO"
call :prova 2B "COLLAUDO DELLE FUNZIONI (senza finestra)" "--collaudo %AMPIEZZA%" collaudo.txt ESITO_COLL superato "NON SUPERATO"
if defined RAPIDO goto :rapido
call :prova 2C "PROVE DELL'INTERFACCIA (il programma si usa da solo)" "--prove-interfaccia %AMPIEZZA%" prove_interfaccia.txt ESITO_REG superate "NON SUPERATE"
if exist "%OUT%\data\registro.txt" (
  >>"%REP%" echo.
  >>"%REP%" echo ----- REGISTRO DELLE OPERAZIONI -----
  type "%OUT%\data\registro.txt" >> "%REP%"
)
rem 0.4.17 (IC8TEM, 14/11/2026 09:12): la Butternut: YA-NEC si riapre e la carica.
rem 0.4.18 (IC8TEM, 14/11/2026 09:17): poi i 3 modelli piu' pesanti, senza finestra, con i tempi di ogni fase del motore.
rem 0.4.22: 2D, 2E e 2F partono sempre (prima solo con 2A, 2B e 2C superate): un solo giro da tutti i risultati
call :prova 2D "BUTTERNUT CON 64 RADIALI (modello corretto nella 0.4.18), YA-NEC si riapre e la carica" "--prove-butternut" prove_butternut.txt ESITO_BUT superata "NON SUPERATA"
call :prova 2E "I 3 MODELLI PIU' PESANTI (tempi del motore, LU a blocchi contro LU classica, senza finestra)" "--prove-pesanti" prove_pesanti.txt ESITO_PES superati "NON SUPERATI"
rem 0.4.20 (IC8TEM, 14/11/2026 15:25): i modelli piu' sensibili e aggressivi per il calcolo, con la calibrazione su EZNEC (prova 6)
call :prova 2F "MODELLI SENSIBILI E AGGRESSIVI (YA-NEC come nec2c, LU a blocchi, calibrazione con EZNEC)" "--prove-sensibili" prove_sensibili.txt ESITO_SEN superati "NON SUPERATI"
:dopo_ultime
rem 0.4.20 (IC8TEM, 14/11/2026 15:25; scelta delle 15:25: prove EZNEC sempre in automatico): i modelli di tutte le sottolinguette dei
rem calcolatori in .nec e .ez (il .ez si rilegge e si ricalcola), poi EZNEC li calcola con lo script del confronto. Sempre, anche se
rem le altre prove non sono superate: servono alla calibrazione finale.
call :prova 2G "MODELLI DEI CALCOLATORI IN .NEC E .EZ (salvataggio .ez riletto e ricalcolato)" "--esporta-eznec" "eznec_calcolatori\esportazione.txt" ESITO_EZE riuscita "CON ERRORI"
call :eznec
call :archivio
call :gradini
call :varianti
:rapido_fine
:dopo_prove
rem 0.4.38: fase 2O tolta. Il collaudo 0.4.37 ha mostrato che nessun bit della testata del .ez accende la Stepped Diameter Correction:
rem e' un'impostazione di EZNEC (accesa per tutti i modelli). Il sottoprogramma :stepped resta qui sotto per una nuova ricerca.
rem tempi: il punteggio delle funzioni dell'ultima sessione (prove dell'interfaccia) e i blocchi dell'interfaccia
if exist "%OUT%\Registri\punteggio_funzioni.txt" (
  >>"%REP%" echo.
  >>"%REP%" echo ----- TEMPI: PUNTEGGIO DELLE FUNZIONI -----
  type "%OUT%\Registri\punteggio_funzioni.txt" >> "%REP%"
)
>>"%REP%" echo.
>>"%REP%" echo ----- TEMPI: COMPUTER, BLOCCHI E FUNZIONI LENTE DI TUTTI I REGISTRI -----
findstr /c:"COMPUTER " /c:"BLOCCO " /c:"[LENTO" /c:"[MOLTO LENTO" /c:"librerie native" "%OUT%\Registri\YA-NEC_registro_*.txt" >> "%REP%" 2>nul
rem i passi con AVVISO o ERRORE, raccolti in fondo per leggerli subito
>>"%REP%" echo.
>>"%REP%" echo ----- PASSI DA GUARDARE (AVVISO o ERRORE) -----
rem 0.4.11: l'elenco lo scrive il programma (il comando PowerShell della 0.4.8 dava un elenco vuoto)
if exist "%EXE%" (start "" /wait "%EXE%" --passi "%REP%" "%REP%.tmp") else findstr /c:"esito:    ERRORE" /c:"esito:    AVVISO" /c:"-> ERRORE" "%REP%" >> "%REP%.tmp" 2>nul
if exist "%REP%.tmp" (type "%REP%.tmp" >> "%REP%" & del "%REP%.tmp")

rem ----------------------------------------------------------------------------- FASE 3
:riepilogo
call :titolo 3 "RIEPILOGO"
call :scrivi "Compilazione ............ %ESITO_COMP% (%MODO%)"
call :scrivi "Autotest del motore ..... %ESITO_AUTO%"
call :scrivi "Collaudo delle funzioni . %ESITO_COLL%"
call :scrivi "Prove dell'interfaccia .. %ESITO_REG%"
call :scrivi "Butternut (2D) .......... %ESITO_BUT%"
call :scrivi "Modelli pesanti (2E) .... %ESITO_PES%"
call :scrivi "Modelli sensibili (2F) .. %ESITO_SEN%"
call :scrivi "Calcolatori .nec/.ez (2G) %ESITO_EZE%"
call :scrivi "Prove con EZNEC (2H) .... %ESITO_EZN%"
call :scrivi "EZNEC archivio (2I) ..... %ESITO_EZA%"
call :scrivi "Confronto archivio (2J) . %ESITO_ARC%"
call :scrivi "EZNEC gradini (2K) ...... %ESITO_EZG%"
call :scrivi "Confronto gradini (2L) .. %ESITO_GRA%"
call :scrivi "Taratura Leeson (2L bis)  %ESITO_TAR%"
call :scrivi "EZNEC varianti (2M) .... %ESITO_EZV%"
call :scrivi "Confronto varianti (2N)  %ESITO_VAR%"
call :scrivi "Inizio %T0% - fine %TIME%"
call :scrivi ""
rem 0.5.1: con /solo non c'e' collaudo e lo zip del collaudo non si tocca (collaudo_automatico.bat compila /distribuzione /solo dopo il collaudo)
if defined SOLO goto :senza_zip
if exist "yanec-collaudo-%VER%.zip" del "yanec-collaudo-%VER%.zip"
rem dalla 0.4.4: nello zip sempre il resoconto e i registri (anche senza immagini: prima, senza immagini, lo zip non si creava)
rem dalla 0.4.9 (richiesta di IC8TEM del 14/11/2026 00:30): yanec-collaudo-<versione>.zip con il resoconto, compila_log.txt e tutti i registri
powershell -NoProfile -Command "$f=@((Resolve-Path '%REP%').Path); $f+=@(Get-ChildItem -Path '%OUT%\Registri\*.txt','%OUT%\data\registro.txt','%OUT%\data\collaudo.txt','%OUT%\data\autotest.txt','%OUT%\data\prove_interfaccia.txt','%OUT%\data\prove_butternut.txt','%OUT%\data\prove_pesanti.txt','%OUT%\data\prove_sensibili.txt','%OUT%\data\eznec_calcolatori\esportazione.txt','%OUT%\data\eznec_calcolatori\riferimento_YA-NEC.csv','%OUT%\data\eznec_calcolatori\confronto_eznec.txt','%OUT%\data\eznec_calcolatori\eznec-confronto-*.zip','%OUT%\data\prove\eznec_archivio\confronto_archivio.txt','%OUT%\data\prove\eznec_archivio\confronto_archivio.csv','%OUT%\data\prove\eznec_archivio\eznec-confronto-*.zip','%OUT%\data\prove\eznec_gradini\preparazione_gradini.txt','%OUT%\data\prove\eznec_gradini\riferimento_gradini.csv','%OUT%\data\prove\eznec_gradini\confronto_gradini.txt','%OUT%\data\prove\eznec_gradini\confronto_gradini.csv','%OUT%\data\prove\eznec_gradini\taratura_gradini.txt','%OUT%\data\prove\eznec_gradini\eznec-confronto-*.zip','%OUT%\data\prove\eznec_varianti\preparazione_varianti.txt','%OUT%\data\prove\eznec_varianti\riferimento_varianti.csv','%OUT%\data\prove\eznec_varianti\confronto_varianti.txt','%OUT%\data\prove\eznec_varianti\confronto_varianti.csv','%OUT%\data\prove\eznec_varianti\eznec-confronto-*.zip','%OUT%\data\prove\eznec_mirati\confronto_archivio.txt','%OUT%\data\prove\eznec_mirati\confronto_archivio.csv','%OUT%\data\prove\eznec_mirati\eznec-confronto-*.zip','%OUT%\data\prove\eznec_gradini_mirati\preparazione_gradini.txt','%OUT%\data\prove\eznec_gradini_mirati\confronto_gradini.txt','%OUT%\data\prove\eznec_gradini_mirati\confronto_gradini.csv','%OUT%\data\prove\eznec_gradini_mirati\eznec-confronto-*.zip','%OUT%\data\prove\eznec_stepped\stepped.csv','%OUT%\data\prove\eznec_stepped\registro.txt','%OUT%\data\fumetti.txt','compila_log.txt' -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName }); Compress-Archive -Path $f -DestinationPath 'yanec-collaudo-%VER%.zip' -Force" >nul 2>&1
if exist "yanec-collaudo-%VER%.zip" (
  call :scrivi "Mandare il file yanec-collaudo-%VER%.zip (resoconto collaudo_completo.txt, compila_log.txt e tutti i registri di performance)."
) else call :scrivi "Mandare il file %REP% e la cartella %OUT%\Registri (lo zip non e' stato creato)."
:senza_zip
echo.
rem 0.5.1: collaudo_automatico.bat (SENZA_PAUSA=1) non si ferma alla fine
if not defined SENZA_PAUSA pause
exit /b 0

rem ----------------------------------------------------------------------------- funzioni
:stepped
rem 0.4.32 (collaudo 0.4.31, 2H: radiatori XR-5 esportati in .ez, EZNEC 35,6 %% lontano): il .ez scritto da YA-NEC non accende la Stepped
rem Diameter Correction di EZNEC. Fase 2O: lo script cerca il bit della testata che la comanda (520-40H e 620-80H dell'ARRL, ogni bit
rem di MiscFlags e i byte vicini cambiati uno alla volta, circa 110 calcoli). Il risultato (stepped.csv) va nello zip del collaudo.
set "EZS=%CD%\%OUT%\data\prove\eznec_stepped"
set "EZSA=%CD%\%OUT%\data\prove\eznec_archivio\modelli_ez"
if not exist "%EZSA%\ARRL Antenna Book\520-40H.ez" (set "ESITO_STP=non eseguita: manca 520-40H.ez" & exit /b 0)
>>"%REP%" echo.
>>"%REP%" echo ----- 2O. RICERCA DEL BIT DELLA STEPPED DIAMETER CORRECTION NEL .EZ -----
call :scrivi "2O: EZNEC calcola due Yagi ARRL con ogni bit della testata cambiato (circa 110 calcoli). Non usare mouse e tastiera."
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\eznec\confronta_eznec.ps1" -Stepped -Cartella "%EZSA%" -Uscita "%EZS%" -Attesa 120 > "%CD%\%OUT%\data\prove\stepped_console.txt" 2>&1
if exist "%OUT%\data\prove\stepped_console.txt" type "%OUT%\data\prove\stepped_console.txt" >> "%REP%"
if exist "%EZS%\stepped.csv" (type "%EZS%\stepped.csv" >> "%REP%" & set "ESITO_STP=eseguita (stepped.csv nello zip)") else set "ESITO_STP=non riuscita (manca stepped.csv)"
call :scrivi "2O ...... %ESITO_STP%"
exit /b 0

:titolo
echo.
echo ===========================================================================================
echo FASE %~1 di 3 - %~2
echo ===========================================================================================
>>"%REP%" echo.
>>"%REP%" echo ===== FASE %~1 di 3 - %~2 =====
exit /b 0

:scrivi
echo(%~1
>>"%REP%" echo(%~1
exit /b 0

rem 0.4.18: una prova della fase 2 (sottoprogramma: dentro un blocco tra parentesi ERRORLEVEL non si aggiorna)
rem   %1 sigla  %2 titolo  %3 argomenti di YA-NEC.exe  %4 resoconto in data  %5 variabile dell'esito  %6 esito buono  %7 esito cattivo
rem   codice di uscita: 0 superato, 10000 finestra chiusa prima della fine, altro numero di passi o procedure con errore
:prova
>>"%REP%" echo.
>>"%REP%" echo ----- %~1. %~2 -----
if exist "%OUT%\data\%~4" del "%OUT%\data\%~4"
start "" /wait "%EXE%" %~3
set "RC=%ERRORLEVEL%"
if exist "%OUT%\data\%~4" (type "%OUT%\data\%~4" >> "%REP%") else call :scrivi "ERRORE: %~4 non creato"
if "%RC%"=="0" (set "%~5=%~6") else set "%~5=%~7 (codice %RC%: procedure o passi con errore)"
if "%RC%"=="10000" set "%~5=INTERROTTO (la finestra si e' chiusa prima della fine: resoconto parziale)"
call set "ESITO_ORA=%%%~5%%"
call :scrivi "%~1 ...... %ESITO_ORA%"
exit /b 0

:rapido
rem 0.5.4 (IC8TEM 15/11/2026 23:30, "mi riscrivi una versione 0.5.4 solo con i collaudi necessari?"): dopo 2A e 2B solo le prove del motore
rem che cambiano con la correzione dei tubi telescopici (2J archivio, 2K-2L elementi a gradini, 2L bis taratura), con i valori di EZNEC salvati.
set "ESITO_REG=saltate (collaudo rapido)"
set "ESITO_BUT=saltata (collaudo rapido)"
set "ESITO_PES=saltati (collaudo rapido)"
set "ESITO_SEN=saltati (collaudo rapido)"
set "ESITO_EZE=saltata (collaudo rapido)"
set "ESITO_EZN=saltate (collaudo rapido)"
set "ESITO_EZV=saltate (collaudo rapido)"
set "ESITO_VAR=saltato (collaudo rapido)"
call :scrivi "Collaudo rapido: saltate 2C interfaccia, 2D Butternut, 2E pesanti, 2F sensibili, 2G-2H calcolatori, 2M-2N varianti; 2I-2K con i valori EZNEC salvati."
call :archivio
call :gradini
goto :rapido_fine

:eznec
rem 0.4.20, fase 2H: EZNEC calcola i modelli dei calcolatori (lo script lo cerca e lo avvia se non e' aperto; si ferma se non lo trova)
set "EZD=%CD%\%OUT%\data\eznec_calcolatori"
if not exist "%EZD%\modelli_ez" (set "ESITO_EZN=non eseguite: mancano i modelli .ez (2G)" & exit /b 0)
>>"%REP%" echo.
>>"%REP%" echo ----- 2H. PROVE CON EZNEC DEI MODELLI DEI CALCOLATORI -----
call :scrivi "2H: EZNEC calcola i modelli dei calcolatori (si apre da solo). Non usare mouse, tastiera e copia/incolla fino alla fine."
del "%EZD%\eznec-confronto-*.zip" >nul 2>&1
if defined SENZAEZ call :salvati calcolatori "%EZD%" & goto :dopo_ez
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\eznec\confronta_eznec.ps1" -Cartella "%EZD%\modelli_ez" -Uscita "%EZD%\risultati_eznec" -Riferimento "%EZD%\riferimento_YA-NEC.csv" -Attesa 1800
:dopo_ez
set "RC=%ERRORLEVEL%"
if exist "%EZD%\risultati_eznec\registro.txt" type "%EZD%\risultati_eznec\registro.txt" >> "%REP%"
set "NZ=0"
for %%z in ("%EZD%\eznec-confronto-*.zip") do set /a NZ+=1
if defined SENZAEZ set "NZ=1"
if "%NZ%"=="0" (set "ESITO_EZN=NON ESEGUITE (codice %RC%: EZNEC non trovato o non risponde; vedi il registro sopra)" & goto :eznec_fine)
rem collaudo 0.4.20: il confronto EZNEC / YA-NEC fa parte dell'esito (OK entro lo 0,5 %, AVVISO se manca una parte nel .ez, ERRORE altrimenti)
if exist "%EZD%\confronto_eznec.txt" del "%EZD%\confronto_eznec.txt"
start "" /wait "%EXE%" --confronta-eznec
set "RC=%ERRORLEVEL%"
if exist "%EZD%\confronto_eznec.txt" type "%EZD%\confronto_eznec.txt" >> "%REP%"
if "%RC%"=="0" (set "ESITO_EZN=superate: EZNEC come YA-NEC entro lo 0,5 %% (avvisi solo per le parti che il .ez non contiene)") else set "ESITO_EZN=NON SUPERATE (%RC% modelli con errore: vedi il confronto)"
:eznec_fine
call :scrivi "2H ...... %ESITO_EZN%"
exit /b 0

:archivio
rem 0.4.22 (IC8TEM, 14/11/2026 18:54 e 20:32): i modelli d'archivio che non combaciavano con EZNEC oltre lo 0,1 % (prova 8, 460 .ez):
rem 2I EZNEC li calcola di nuovo con lo stesso script; 2J YA-NEC li rilegge con il suo lettore .ez, li calcola e li confronta con i valori
rem di EZNEC appena calcolati (se EZNEC non c'e', con quelli della prova 6). I modelli che EZNEC ferma per geometria: YA-NEC deve segnalarli.
if not defined ARCHNOME set "ARCHNOME=eznec_archivio"
set "EZA=%CD%\%OUT%\data\prove\%ARCHNOME%"
if not exist "%EZA%\modelli_ez" (set "ESITO_EZA=non eseguite: manca data\prove\eznec_archivio" & set "ESITO_ARC=non eseguito" & exit /b 0)
>>"%REP%" echo.
>>"%REP%" echo ----- 2I. EZNEC SUI MODELLI D'ARCHIVIO OLTRE LO 0,1 %% -----
if defined MIRATO (call :scrivi "2I: EZNEC calcola i modelli d'archivio che si discostano (160, circa 6 minuti). Non usare mouse, tastiera e copia/incolla fino alla fine.") else call :scrivi "2I: EZNEC calcola i modelli d'archivio (411 dalla 0.4.30, circa 15 minuti). Non usare mouse, tastiera e copia/incolla fino alla fine."
del "%EZA%\eznec-confronto-*.zip" >nul 2>&1
rem 0.4.30 (IC8TEM 14/11/2026 22:08): pulizia automatica. Lo zip nuovo estratto sopra la cartella vecchia lascia i modelli tolti (dscn2):
rem si cancellano i .ez che non sono in elenco_modelli.txt (l'elenco dei modelli del sorgente), cosi' EZNEC e YA-NEC provano solo quelli.
if exist "%EZA%\elenco_modelli.txt" powershell -NoProfile -Command "$e=@{}; Get-Content -LiteralPath '%EZA%\elenco_modelli.txt' | ForEach-Object { $e[$_.Trim()]=1 }; $b=(Resolve-Path -LiteralPath '%EZA%\modelli_ez').Path; $n=0; Get-ChildItem -LiteralPath $b -Recurse -File -Filter *.ez | ForEach-Object { $r=$_.FullName.Substring($b.Length).TrimStart([char]92); if (-not $e.ContainsKey($r)) { Remove-Item -LiteralPath $_.FullName -Force; $n++; Add-Content -LiteralPath '%REP%' -Value ('  tolto dall archivio (non piu nel sorgente): ' + $r) } }; Add-Content -LiteralPath '%REP%' -Value ('Pulizia dell archivio: ' + $n + ' modelli tolti')"
if exist "%EZA%\risultati_eznec" rmdir /s /q "%EZA%\risultati_eznec"
if defined SENZAEZ call :salvati archivio "%EZA%" & goto :dopo_ar
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\eznec\confronta_eznec.ps1" -Cartella "%EZA%\modelli_ez" -Uscita "%EZA%\risultati_eznec" -Attesa 600
:dopo_ar
set "RC=%ERRORLEVEL%"
set "NZ=0"
for %%z in ("%EZA%\eznec-confronto-*.zip") do set /a NZ+=1
if defined SENZAEZ set "NZ=1"
if "%NZ%"=="0" (set "ESITO_EZA=NON ESEGUITE (codice %RC%: EZNEC non trovato o non risponde; il confronto usa i valori della prova 6)") else set "ESITO_EZA=eseguite"
call :scrivi "2I ...... %ESITO_EZA%"
>>"%REP%" echo.
>>"%REP%" echo ----- 2J. CONFRONTO D'ARCHIVIO: YA-NEC RILEGGE I .EZ E LI CONFRONTA CON EZNEC -----
if exist "%EZA%\confronto_archivio.txt" del "%EZA%\confronto_archivio.txt"
start "" /wait "%EXE%" --confronta-archivio "%EZA%"
set "RC=%ERRORLEVEL%"
if exist "%EZA%\confronto_archivio.txt" (type "%EZA%\confronto_archivio.txt" >> "%REP%") else call :scrivi "ERRORE: confronto_archivio.txt non creato"
if "%RC%"=="0" (set "ESITO_ARC=eseguito (classi degli scarti in fondo al resoconto)") else set "ESITO_ARC=eseguito con %RC% modelli da guardare (errori o fermati da EZNEC non segnalati da YA-NEC)"
call :scrivi "2J ...... %ESITO_ARC%"
exit /b 0

:gradini
rem 0.4.30 (IC8TEM, 14/11/2026 21:52: "Gradini, seconda parte: mettere elemento per elemento la correzione di YA-NEC accanto a quella di
rem EZNEC"): ogni elemento a gradini dei modelli d'archivio si scrive da solo in un .ez (spazio libero, sorgente al centro, testata del
rem file d'origine); 2K EZNEC li calcola con la sua Stepped Diameter Correction, 2L YA-NEC li confronta con Leeson, Leeson forzato e senza.
if not defined GRADNOME set "GRADNOME=eznec_gradini"
set "EZG=%CD%\%OUT%\data\prove\%GRADNOME%"
>>"%REP%" echo.
>>"%REP%" echo ----- 2K. ELEMENTI A GRADINI DA SOLI: YA-NEC LI PREPARA, EZNEC LI CALCOLA -----
if not exist "%EZG%" mkdir "%EZG%"
start "" /wait "%EXE%" --prepara-gradini "%EZA%" "%EZG%"
if exist "%EZG%\preparazione_gradini.txt" (type "%EZG%\preparazione_gradini.txt" >> "%REP%") else (set "ESITO_EZG=NON ESEGUITE: preparazione non riuscita" & set "ESITO_GRA=non eseguito" & exit /b 0)
call :scrivi "2K: EZNEC calcola gli elementi a gradini uno per uno (circa 30 minuti). Non usare mouse, tastiera e copia/incolla fino alla fine."
del "%EZG%\eznec-confronto-*.zip" >nul 2>&1
if exist "%EZG%\risultati_eznec" rmdir /s /q "%EZG%\risultati_eznec"
if defined SENZAEZ call :salvati gradini "%EZG%" & goto :dopo_gr
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\eznec\confronta_eznec.ps1" -Cartella "%EZG%\modelli_ez" -Uscita "%EZG%\risultati_eznec" -Attesa 300
:dopo_gr
set "RC=%ERRORLEVEL%"
set "NZ=0"
for %%z in ("%EZG%\eznec-confronto-*.zip") do set /a NZ+=1
if defined SENZAEZ set "NZ=1"
if "%NZ%"=="0" (set "ESITO_EZG=NON ESEGUITE (codice %RC%: EZNEC non trovato o non risponde)") else set "ESITO_EZG=eseguite"
call :scrivi "2K ...... %ESITO_EZG%"
>>"%REP%" echo.
>>"%REP%" echo ----- 2L. ELEMENTI A GRADINI: EZNEC CONTRO YA-NEC (L = com'e', F = Leeson forzato, 0 = senza correzione) -----
if exist "%EZG%\confronto_gradini.txt" del "%EZG%\confronto_gradini.txt"
start "" /wait "%EXE%" --confronta-gradini "%EZG%"
if exist "%EZG%\confronto_gradini.txt" (type "%EZG%\confronto_gradini.txt" >> "%REP%" & set "ESITO_GRA=eseguito (verdetti in fondo al resoconto)") else (set "ESITO_GRA=ERRORE: confronto_gradini.txt non creato" & call :scrivi "ERRORE: confronto_gradini.txt non creato")
call :scrivi "2L ...... %ESITO_GRA%"
rem 0.5.3 (modifica Leeson A/B, via di IC8TEM 15/11/2026 21:57): prova B, taratura di Leeson sotto la risonanza sugli stessi valori di EZNEC
>>"%REP%" echo.
>>"%REP%" echo ----- 2L bis. TARATURA DI LEESON SOTTO LA RISONANZA (prova B: proposta, il motore non cambia) -----
if exist "%EZG%\taratura_gradini.txt" del "%EZG%\taratura_gradini.txt"
start "" /wait "%EXE%" --tara-gradini "%EZG%"
if exist "%EZG%\taratura_gradini.txt" (type "%EZG%\taratura_gradini.txt" >> "%REP%" & set "ESITO_TAR=eseguita (proposta in fondo al resoconto)") else (set "ESITO_TAR=ERRORE: taratura_gradini.txt non creato" & call :scrivi "ERRORE: taratura_gradini.txt non creato")
call :scrivi "2L bis .. %ESITO_TAR%"
exit /b 0

:varianti
rem 0.4.30 (IC8TEM, 14/11/2026 23:13, "ok approvato"): i 7 modelli vicini al terreno dove EZNEC si scosta del 2-6 % da YA-NEC e nec2c
rem (ElevatRad, ELEVRAD1, ELEVRAD2, GndScreen, wa2wvl, 80hsbeam x2) in quattro varianti: A originale, B fili alzati a lambda/100,
rem C segmenti raddoppiati, D terreno MININEC; 2M EZNEC le calcola, 2N il verdetto (quota, segmentazione o terreno).
set "EZV=%CD%\%OUT%\data\prove\eznec_varianti"
>>"%REP%" echo.
>>"%REP%" echo ----- 2M. VARIANTI VICINO AL TERRENO: YA-NEC LE PREPARA, EZNEC LE CALCOLA -----
if not exist "%EZV%" mkdir "%EZV%"
start "" /wait "%EXE%" --prepara-varianti "%EZA%" "%EZV%"
if exist "%EZV%\preparazione_varianti.txt" (type "%EZV%\preparazione_varianti.txt" >> "%REP%") else (set "ESITO_EZV=NON ESEGUITE: preparazione non riuscita" & set "ESITO_VAR=non eseguito" & exit /b 0)
call :scrivi "2M: EZNEC calcola le varianti (pochi minuti). Non usare mouse, tastiera e copia/incolla fino alla fine."
del "%EZV%\eznec-confronto-*.zip" >nul 2>&1
if exist "%EZV%\risultati_eznec" rmdir /s /q "%EZV%\risultati_eznec"
if defined SENZAEZ call :salvati varianti "%EZV%" & goto :dopo_va
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\eznec\confronta_eznec.ps1" -Cartella "%EZV%\modelli_ez" -Uscita "%EZV%\risultati_eznec" -Attesa 300
:dopo_va
set "RC=%ERRORLEVEL%"
set "NZ=0"
for %%z in ("%EZV%\eznec-confronto-*.zip") do set /a NZ+=1
if defined SENZAEZ set "NZ=1"
if "%NZ%"=="0" (set "ESITO_EZV=NON ESEGUITE (codice %RC%: EZNEC non trovato o non risponde)") else set "ESITO_EZV=eseguite"
call :scrivi "2M ...... %ESITO_EZV%"
>>"%REP%" echo.
>>"%REP%" echo ----- 2N. VARIANTI VICINO AL TERRENO: EZNEC CONTRO YA-NEC E VERDETTO -----
if exist "%EZV%\confronto_varianti.txt" del "%EZV%\confronto_varianti.txt"
start "" /wait "%EXE%" --confronta-varianti "%EZV%"
if exist "%EZV%\confronto_varianti.txt" (type "%EZV%\confronto_varianti.txt" >> "%REP%" & set "ESITO_VAR=eseguito (verdetti in fondo al resoconto)") else (set "ESITO_VAR=ERRORE: confronto_varianti.txt non creato" & call :scrivi "ERRORE: confronto_varianti.txt non creato")
call :scrivi "2N ...... %ESITO_VAR%"
exit /b 0

:mirato
rem ---------------------------------------------------------------------------------------------------------------------------
rem 0.5.0 (IC8TEM 15/11/2026 16:00: "mi fai fare prima un collaudo solo dei modelli piu' discostanti e dei modelli che escono dalle
rem schede del calcolatore"). COLLAUDO MIRATO, con EZNEC vero (non i valori salvati):
rem   2G  YA-NEC crea il modello NEC di ogni sottolinguetta dei Calcolatori (comprese Yagi DL6WU con le quattro alimentazioni, Quagi,
rem       quad, loop Yagi), lo salva in .nec e in .ez, rilegge il .ez e lo ricalcola
rem   2H  EZNEC calcola quei .ez e YA-NEC confronta (entro lo 0,5 %%)
rem   2I  EZNEC calcola i 160 modelli d'archivio che nel collaudo 0.4.40 non erano entro lo 0,1 %% (data\prove\eznec_archivio\mirati.txt)
rem   2J  YA-NEC rilegge quei .ez, li calcola e li confronta con EZNEC
rem   2K  YA-NEC prende gli elementi a gradini di quei modelli e li scrive uno per uno in .ez; EZNEC li calcola
rem   2L  YA-NEC li confronta (Leeson come in YA-NEC, Leeson forzato, senza correzione)
rem Non si fanno: autotest, collaudo delle funzioni, interfaccia, Butternut, modelli pesanti e sensibili, varianti vicino al terreno
rem (per quelli c'e' prova_tutto.bat). Durata: circa 20-25 minuti. Alla fine si manda yanec-collaudo-<versione>.zip.
call :titolo 2 "COLLAUDO MIRATO: modelli dei calcolatori e modelli d'archivio che si discostano da EZNEC (con EZNEC)"
call :scrivi "2G e 2H: i modelli NEC di tutte le sottolinguette dei Calcolatori, in .nec e .ez, calcolati da YA-NEC e da EZNEC."
call :scrivi "2I e 2J: i 160 modelli d'archivio che nel collaudo 0.4.40 non stavano entro lo 0,1 per cento da EZNEC."
call :scrivi "2K e 2L: gli elementi a gradini di quei modelli, uno per uno."
call :scrivi "Chiudi EZNEC prima di partire (lo apre lo script) e non usare mouse, tastiera e appunti durante le prove con EZNEC."
if exist "%OUT%\Registri" rmdir /s /q "%OUT%\Registri"
call :prova 2G "MODELLI DEI CALCOLATORI IN .NEC E .EZ (salvataggio .ez riletto e ricalcolato)" "--esporta-eznec" "eznec_calcolatori\esportazione.txt" ESITO_EZE riuscita "CON ERRORI"
call :eznec
rem cartella dell'archivio ridotta ai modelli dell'elenco mirati.txt (modelli .ez, elenco e valori di riferimento)
set "SRCA=%CD%\%OUT%\data\prove\eznec_archivio"
set "DSTA=%CD%\%OUT%\data\prove\eznec_mirati"
if exist "%DSTA%" rmdir /s /q "%DSTA%"
powershell -NoProfile -Command "$s='%SRCA%'; $d='%DSTA%'; $l=Get-Content -LiteralPath (Join-Path $s 'mirati.txt') -Encoding UTF8 | ForEach-Object { $_.Trim() } | Where-Object { $_ }; $n=0; foreach ($r in $l) { $a=Join-Path (Join-Path $s 'modelli_ez') $r; $b=Join-Path (Join-Path $d 'modelli_ez') $r; if (Test-Path -LiteralPath $a) { New-Item -ItemType Directory -Force -Path (Split-Path $b) | Out-Null; Copy-Item -LiteralPath $a -Destination $b -Force; $n++ } }; $l | Set-Content -LiteralPath (Join-Path $d 'elenco_modelli.txt') -Encoding UTF8; $h=@{}; foreach ($r in $l) { $h[$r]=1 }; $rif=Get-Content -LiteralPath (Join-Path $s 'riferimento_EZNEC.csv') -Encoding UTF8; $o=@($rif[0]) + @($rif | Select-Object -Skip 1 | Where-Object { $h.ContainsKey(($_ -split ';')[0]) }); $o | Set-Content -LiteralPath (Join-Path $d 'riferimento_EZNEC.csv') -Encoding UTF8; Add-Content -LiteralPath '%REP%' -Value ('Collaudo mirato: ' + $n + ' modelli d archivio copiati in eznec_mirati')"
set "ARCHNOME=eznec_mirati"
set "GRADNOME=eznec_gradini_mirati"
call :archivio
call :gradini
goto :dopo_prove

:salvati
rem 0.4.39 (IC8TEM 15/11/2026 13:25, "procedi" su prova_senza_eznec.bat): al posto di EZNEC i suoi risultati salvati del collaudo 0.4.37
rem (stessi .ez, stessi numeri); YA-NEC ricalcola la sua parte e confronta. Da rifare con EZNEC quando cambiano i modelli o l'esportazione.
if not exist "data\prove\eznec_salvati\%~1\riepilogo_eznec.csv" (call :scrivi "MANCANO i valori EZNEC salvati di %~1 (data\prove\eznec_salvati)" & exit /b 0)
if not exist "%~2\risultati_eznec" mkdir "%~2\risultati_eznec"
copy /y "data\prove\eznec_salvati\%~1\riepilogo_eznec.csv" "%~2\risultati_eznec\riepilogo_eznec.csv" >nul
call :scrivi "Senza EZNEC: valori salvati per %~1 (archivio e gradini del collaudo 0.5.3 del 15/11/2026 23:17, calcolatori e varianti del 0.4.37; EZNEC non si apre)"
exit /b 0

:controlla
if exist "%~1" (call :scrivi "OK      %~2  (%~1)") else call :scrivi "MANCA   %~2  (%~1)"
exit /b 0
