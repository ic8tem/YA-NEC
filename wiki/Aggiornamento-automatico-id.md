# Pembaruan otomatis

Setiap 30 menit (dan 2 menit setelah dijalankan) YA-NEC diam-diam memeriksa versi terbaru yang diterbitkan di GitHub ini. Jika lebih baru:

1. ia mengunduh zip untuk platformnya beserta checksum `.sha256`;
2. ia memeriksa checksum: jika tidak ada atau berbeda, tidak ada yang diinstal;
3. ia mengganti berkas lama dengan yang baru: versi baru berjalan **pada saat program dibuka berikutnya**, dengan pesan di bilah status.

Versi sebelumnya disimpan di `data/aggiornamenti/precedente-<versi>`, agar bisa kembali secara manual. Pengaturan (`data/config.ini`), log, dan model pengguna tidak disentuh. Jika beberapa jendela YA-NEC terbuka, hanya yang pertama yang memeriksa.

Dapat dimatikan di **Pengaturan > Pembaruan**; tombol **Periksa sekarang** tidak menunggu 30 menit. Setiap langkah dicatat di log (kategori AGGIORNAMENTO).

**Paket .deb dan .rpm**, bila sudah ada: diinstal dengan hak administrator dan hanya diperbarui lewat manajer paket (`apt`, `dnf`, `zypper`). Pada paket tersebut pembaruan otomatis dimatikan, dan Pengaturan menyebutkannya.
