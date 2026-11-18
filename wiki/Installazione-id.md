# Instalasi

YA-NEC tidak perlu diinstal: cukup diekstrak lalu dijalankan. .NET tidak diperlukan, sudah ada di dalam paket. Pengaturan, model, dan log tetap di folder program (`data`, `Registri`): tanpa AppData, tanpa registry. Untuk menghapusnya, cukup hapus foldernya.

## Paket mana

Dari halaman [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistem | Paket |
|---|---|
| Windows 10/11 64-bit | `YA-NEC-<versi>-windows-x64-portatile.zip` |
| Windows 10/11 32-bit | `YA-NEC-<versi>-windows-x86-portatile.zip` |
| Windows di ARM (Snapdragon) | `YA-NEC-<versi>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versi>-windows7-x64-portatile.zip` atau `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versi>-linux-x64.tar.gz` |
| Raspberry Pi OS 64-bit dan Linux aarch64 lain | `YA-NEC-<versi>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32-bit, armv7l | `YA-NEC-<versi>-linux-armv7l.tar.gz` |
| Alpine dan Linux musl | `YA-NEC-<versi>-alpine-x64.tar.gz` atau `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versi>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versi>-mac-intel-app.zip` |

Di samping setiap zip ada checksum `.sha256` untuk memeriksa unduhan.

## Windows

Ekstrak zip ke folder yang bisa ditulisi, misalnya `C:\YA-NEC` (jangan di `C:\Program Files`, tempat program tidak bisa menyimpan pengaturannya), lalu jalankan `YA-NEC.exe`. Saat pertama kali dijalankan, Windows SmartScreen mungkin meminta konfirmasi: "Info selengkapnya" > "Tetap jalankan".

**Windows 7 SP1, 8, dan 8.1**: gunakan paket `win7`, dibangun dengan .NET 6 (.NET 8 tidak berjalan di Windows 7). Windows 7 memerlukan Service Pack 1 serta pembaruan KB2533623 dan KB2999226.

## Linux dan Raspberry Pi

```
tar -xzf YA-NEC-<versi>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Diperlukan `libfontconfig1` dan `libicu`, yang sudah ada di distribusi desktop. Di Alpine: `apk add icu-libs fontconfig libstdc++`. Di Raspberry Pi perhitungan berjalan, tetapi model besar (ribuan segmen) butuh waktu dan memori.

## Mac

Ekstrak zip, misalnya ke `~/YA-NEC`. Program ini tidak ditandatangani oleh Apple, jadi karantina harus dicabut sekali dari Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Layar kecil

Pada layar yang pendek (misalnya 1366 x 768) jendela terbuka dalam ukuran maksimal dan kotak hasil beralih ke format ringkas.
