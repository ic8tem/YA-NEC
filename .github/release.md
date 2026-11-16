**YA-NEC 1.0.1** - simulatore d'antenne a fili basato sul NEC-2, a cura di Costantino IC8TEM.

> *La storia di questo programma la farete voi: con i vostri progetti, con le vostre idee, con la vostra genialità, con le vostre scoperte e con le vostre certezze.*

**Correzione:** in un modello nuovo (Nuovo + Aggiungi filo) ora la vista 3D e la tabella Sorgenti compaiono anche prima del primo calcolo riuscito, così si può aggiungere la sorgente. Motore di calcolo invariato.

## Scarica la versione per il tuo sistema

| Sistema | File |
|---|---|
| Windows 10/11 64 bit | [YA-NEC-1.0.1-win-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-win-x64.zip) |
| Windows 10/11 32 bit | [YA-NEC-1.0.1-win-x86.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-win-x86.zip) |
| Windows su ARM (Snapdragon) | [YA-NEC-1.0.1-win-arm64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-win-arm64.zip) |
| Windows 7 SP1, 8, 8.1 - 64 bit | [YA-NEC-1.0.1-win7-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-win7-x64.zip) |
| Windows 7 SP1, 8, 8.1 - 32 bit | [YA-NEC-1.0.1-win7-x86.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-win7-x86.zip) |
| macOS Apple Silicon (M1-M4) | [YA-NEC-1.0.1-osx-arm64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-osx-arm64.zip) |
| macOS Intel | [YA-NEC-1.0.1-osx-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-osx-x64.zip) |
| Raspberry Pi OS 64 bit (e altri Linux ARM64) | [YA-NEC-1.0.1-linux-arm64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-linux-arm64.zip) |
| Raspberry Pi OS 32 bit (armv7l) | [YA-NEC-1.0.1-linux-arm.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-linux-arm.zip) |
| Linux x86_64 (Debian, Ubuntu, Mint, Fedora...) | [YA-NEC-1.0.1-linux-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-linux-x64.zip) |
| Alpine / musl x64 | [YA-NEC-1.0.1-linux-musl-x64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-linux-musl-x64.zip) |
| Alpine / musl ARM64 | [YA-NEC-1.0.1-linux-musl-arm64.zip](https://github.com/ic8tem/YA-NEC/releases/download/v1.0.1/YA-NEC-1.0.1-linux-musl-arm64.zip) |

Si estrae lo zip in una cartella scrivibile e si avvia `YA-NEC` (Windows: `YA-NEC.exe`). Non serve installare .NET. Ogni zip ha accanto la sua impronta `.sha256` (elenco completo in fondo, sotto "Assets").
macOS: dopo l'estrazione `xattr -dr com.apple.quarantine <cartella>`. Windows 7: servono gli aggiornamenti KB2533623 e KB2999226.

Chi ha la 1.0 riceve la 1.0.1 da solo (aggiornamento automatico, attivo al riavvio; si spegne in Impostazioni > Aggiornamento).
Manuale completo in `doc/Manuale_d_uso.pdf` e nella linguetta Manuale. Licenza GPL-3.0.
