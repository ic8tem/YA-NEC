**YA-NEC 1.0** - simulatore d'antenne a fili basato sul NEC-2, a cura di Costantino IC8TEM.

> *La storia di questo programma la farete voi: con i vostri progetti, con le vostre idee, con la vostra genialità, con le vostre scoperte e con le vostre certezze.*

Prima versione pubblica. Si scarica lo zip della propria piattaforma, lo si estrae in una cartella scrivibile e si avvia `YA-NEC` (Windows: `YA-NEC.exe`). Non serve installare .NET.

- Windows 10/11: `-win-x64`, `-win-x86`, `-win-arm64` (Snapdragon)
- Windows 7 SP1, 8, 8.1: `-win7-x64`, `-win7-x86`
- Linux: `-linux-x64`, `-linux-arm64` (Raspberry Pi OS 64 bit), `-linux-arm` (Raspberry Pi OS 32 bit), `-linux-musl-x64` / `-linux-musl-arm64` (Alpine)
- macOS: `-osx-arm64` (Apple Silicon), `-osx-x64` (Intel); dopo l'estrazione `xattr -dr com.apple.quarantine <cartella>`

Dalle prossime versioni il programma si aggiorna da solo (ogni 30 minuti, impronta SHA-256 verificata, attivo al riavvio; si spegne in Impostazioni > Aggiornamento).
Manuale completo in `doc/Manuale_d_uso.pdf` e nella linguetta Manuale. Licenza GPL-3.0.
