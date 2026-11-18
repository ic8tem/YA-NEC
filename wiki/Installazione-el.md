# Εγκατάσταση

Το YA-NEC δεν εγκαθίσταται: το αποσυμπιέζετε και το εκτελείτε. Δεν χρειάζεται .NET, είναι ήδη μέσα στο πακέτο. Ρυθμίσεις, μοντέλα και αρχεία καταγραφής μένουν στον φάκελο του προγράμματος (`data`, `Registri`): χωρίς AppData, χωρίς μητρώο. Για να το αφαιρέσετε, αρκεί να διαγράψετε τον φάκελο.

## Ποιο πακέτο

Από τη σελίδα [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Σύστημα | Πακέτο |
|---|---|
| Windows 10/11 64 bit | `YA-NEC-<έκδοση>-windows-x64-portatile.zip` |
| Windows 10/11 32 bit | `YA-NEC-<έκδοση>-windows-x86-portatile.zip` |
| Windows σε ARM (Snapdragon) | `YA-NEC-<έκδοση>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<έκδοση>-windows7-x64-portatile.zip` ή `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<έκδοση>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bit και άλλα Linux aarch64 | `YA-NEC-<έκδοση>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bit, armv7l | `YA-NEC-<έκδοση>-linux-armv7l.tar.gz` |
| Alpine και Linux με musl | `YA-NEC-<έκδοση>-alpine-x64.tar.gz` ή `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<έκδοση>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<έκδοση>-mac-intel-app.zip` |

Δίπλα σε κάθε zip υπάρχει το άθροισμα ελέγχου `.sha256` για τον έλεγχο της λήψης.

## Windows

Αποσυμπιέστε το zip σε φάκελο με δικαίωμα εγγραφής, π.χ. `C:\YA-NEC` (όχι στο `C:\Program Files`, όπου το πρόγραμμα δεν θα μπορούσε να αποθηκεύσει τις ρυθμίσεις του), και εκτελέστε το `YA-NEC.exe`. Στην πρώτη εκκίνηση το Windows SmartScreen μπορεί να ζητήσει επιβεβαίωση: «Περισσότερες πληροφορίες» > «Εκτέλεση οπωσδήποτε».

**Windows 7 SP1, 8 και 8.1**: χρησιμοποιήστε τα πακέτα `win7`, χτισμένα με .NET 6 (το .NET 8 δεν ξεκινά στα Windows 7). Τα Windows 7 χρειάζονται το Service Pack 1 και τις ενημερώσεις KB2533623 και KB2999226.

## Linux και Raspberry Pi

```
tar -xzf YA-NEC-<έκδοση>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Χρειάζονται τα `libfontconfig1` και `libicu`, που υπάρχουν ήδη στις διανομές με γραφικό περιβάλλον. Στο Alpine: `apk add icu-libs fontconfig libstdc++`. Στο Raspberry Pi ο υπολογισμός λειτουργεί, αλλά τα μεγάλα μοντέλα (χιλιάδες τμήματα) θέλουν χρόνο και μνήμη.

## Mac

Αποσυμπιέστε το zip, π.χ. στο `~/YA-NEC`. Το πρόγραμμα δεν είναι υπογεγραμμένο από την Apple, οπότε η καραντίνα πρέπει να αφαιρεθεί μία φορά από το Τερματικό:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Μικρές οθόνες

Σε χαμηλές οθόνες (π.χ. 1366 x 768) το παράθυρο ανοίγει μεγιστοποιημένο και τα πλαίσια αποτελεσμάτων περνούν σε συμπαγή μορφή.
