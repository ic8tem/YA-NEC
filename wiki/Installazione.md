# Installazione

YA-NEC non si installa: si estrae e si avvia. Non serve .NET, è già dentro il pacchetto. Impostazioni, modelli e registri restano nella cartella del programma (`data`, `Registri`): niente AppData, niente registro di sistema. Per toglierlo basta cancellare la cartella.

## Quale pacchetto

Dalla pagina [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistema | Pacchetto |
|---|---|
| Windows 10/11 a 64 bit | `YA-NEC-<versione>-windows-x64-portatile.zip` |
| Windows 10/11 a 32 bit | `YA-NEC-<versione>-windows-x86-portatile.zip` |
| Windows su ARM (Snapdragon) | `YA-NEC-<versione>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versione>-windows7-x64-portatile.zip` o `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versione>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bit e altri Linux aarch64 | `YA-NEC-<versione>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bit, armv7l | `YA-NEC-<versione>-linux-armv7l.tar.gz` |
| Alpine e Linux con musl | `YA-NEC-<versione>-alpine-x64.tar.gz` o `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versione>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versione>-mac-intel-app.zip` |

Accanto a ogni zip c'è l'impronta `.sha256` per controllare il file scaricato.

## Windows

Estrarre lo zip in una cartella scrivibile, per esempio `C:\YA-NEC` (non in `C:\Programmi`, dove il programma non potrebbe salvare le impostazioni) e avviare `YA-NEC.exe`. Al primo avvio Windows SmartScreen può chiedere conferma: "Ulteriori informazioni" > "Esegui comunque".

**Windows 7 SP1, 8 e 8.1**: usare i pacchetti `win7`, compilati con .NET 6 (il .NET 8 non parte su Windows 7). Su Windows 7 servono il Service Pack 1 e gli aggiornamenti KB2533623 e KB2999226.

## Linux e Raspberry Pi

```
tar -xzf YA-NEC-<versione>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Servono `libfontconfig1` e `libicu`, già presenti nelle distribuzioni con un desktop. Su Alpine: `apk add icu-libs fontconfig libstdc++`. Sul Raspberry Pi il calcolo funziona, ma i modelli grandi (migliaia di segmenti) richiedono tempo e memoria.

## Mac

Estrarre lo zip, per esempio in `~/YA-NEC`. Il programma non è firmato da Apple, quindi va tolta la quarantena una volta sola, dal Terminale:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Schermi piccoli

Su schermi bassi (per esempio 1366 x 768) la finestra si apre ingrandita e i riquadri dei risultati passano al formato compatto.
