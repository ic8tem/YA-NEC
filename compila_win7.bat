@echo off
rem ===========================================================================================
rem YA-NEC - pacchetti per Windows 7 SP1, 8 e 8.1 (0.5.3, richiesta di IC8TEM del 15/11/2026 22:21)
rem .NET 8 non parte su Windows 7 (manca GetSystemTimePreciseAsFileTime in KERNEL32): come YA-MS 1.8.19 ed E-LAYER ANALYZER 0.5.1
rem si compila con .NET 6, l'ultimo che gira su Windows 7, autonomo (non serve installare .NET sul PC con Windows 7).
rem Basta l'SDK 8 gia' installato: i pacchetti di .NET 6 si scaricano da soli da NuGet la prima volta (serve internet).
rem Risultato: pubblicazione\YA-NEC-<versione>-win7-x64.zip e -win7-x86.zip, con l'impronta .sha256 (per l'aggiornamento automatico).
rem Su Windows 7 servono SP1 e gli aggiornamenti KB2533623 e KB2999226 (come YA-MS). Non e' un collaudo: il collaudo resta compila.bat.
rem ===========================================================================================
setlocal EnableExtensions
cd /d "%~dp0"
chcp 65001 >nul
set /p VER=<VERSION
set "LOG=compila_win7_log.txt"
set "TMPC=%TEMP%\yanec_win7"
if not exist pubblicazione mkdir pubblicazione
echo YA-NEC %VER% per Windows 7, 8 e 8.1 (.NET 6) - %DATE% %TIME%> "%LOG%"
set "ERR=0"
for %%A in (x64 x86) do call :pacchetto %%A
echo.
rem la compilazione normale (.NET 8) ritrova i suoi pacchetti
dotnet restore YA-NEC.csproj >> "%LOG%" 2>&1
if "%ERR%"=="0" (
  echo FATTO: pubblicazione\YA-NEC-%VER%-win7-x64.zip e pubblicazione\YA-NEC-%VER%-win7-x86.zip ^(con .sha256^).
) else (
  echo COMPILAZIONE NON RIUSCITA: righe di errore in %LOG% ^(mandalo a Claude^).
)
pause
exit /b %ERR%

:pacchetto
set "A=%~1"
set "OUT=build\win7-%A%"
echo ----- win7-%A% -----
echo ----- win7-%A% ----->> "%LOG%"
if exist "%OUT%" rmdir /s /q "%OUT%"
dotnet publish YA-NEC.csproj -c Release -r win-%A% --self-contained true -p:Windows7=true -o "%OUT%" >> "%LOG%" 2>&1
if errorlevel 1 (
  set "ERR=1"
  echo *** win7-%A%: compilazione non riuscita. Righe di errore:
  findstr /c:"error " "%LOG%"
  exit /b 0
)
if not exist "%OUT%\YA-NEC.exe" (set "ERR=1" & echo *** win7-%A%: YA-NEC.exe non creato & exit /b 0)
powershell -NoProfile -Command "$s=Join-Path '%TMPC%' 'yanec_w7_%A%'; Remove-Item $s -Recurse -Force -ErrorAction SilentlyContinue; Copy-Item '%OUT%' $s -Recurse; Remove-Item (Join-Path $s 'Registri'),(Join-Path $s 'data\config.ini'),(Join-Path $s 'data\aggiornamenti'),(Join-Path $s 'data\collaudo_immagini'),(Join-Path $s 'data\tmp'),(Join-Path $s 'data\prove'),(Join-Path $s 'data\prove_*.txt'),(Join-Path $s 'data\*.txt') -Recurse -Force -ErrorAction SilentlyContinue; $z='pubblicazione\YA-NEC-%VER%-win7-%A%.zip'; Compress-Archive -Path (Join-Path $s '*') -DestinationPath $z -Force; ((Get-FileHash $z -Algorithm SHA256).Hash.ToLower() + '  YA-NEC-%VER%-win7-%A%.zip') | Set-Content -Encoding ascii ($z + '.sha256')"
if exist "pubblicazione\YA-NEC-%VER%-win7-%A%.zip.sha256" (echo OK  pubblicazione\YA-NEC-%VER%-win7-%A%.zip) else (set "ERR=1" & echo *** win7-%A%: pacchetto non creato)
exit /b 0
