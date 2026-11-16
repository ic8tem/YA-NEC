# Langkah pertama

1. **Contoh** (bilah perintah) membuka salah satu model siap pakai: dipol, ground plane, Yagi VHF (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), Yagi HF (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, Yagi loop 1296 MHz, dan lainnya. **Buka model** membuka berkas NEC, MMANA, AutoEZ, EZNEC, atau K6STI; berkas juga bisa diseret ke jendela.
2. Pilih **band** (dari 2200 m sampai 3 cm, batas IARU Region 1) atau frekuensi bebas, lalu **Hitung**.
3. **Kotak hasil** menampilkan impedansi, SWR, gain, F/B, F/R, lebar berkas -3 dB, efisiensi, dan AGT (uji konvergensi: mendekati 0 dB berarti model dapat dipercaya).
4. **Sapuan** menghitung band di sekitar frekuensi: SWR, R/X, gain, F/B, dan diagram Smith.

## Tab-tabnya

- **Struktur 3D**: antena (seret = putar, roda = zoom, tombol kanan = geser, klik ganda = tampilan awal), dengan tanah digambar sesuai jenis dan ketinggian. Di bawahnya **tabel kawat** bergaya lembar kerja: sel menerima angka, variabel, dan rumus. Di kanan bawah **objek model**: sumber, beban, saluran, transformator.
- **Diagram**: horizontal, vertikal, dan 3D, dengan sudut -3 dB.
- **Frekuensi, Smith, Arus**: hasil sapuan dan arus pada kawat.
- **Model NEC**: kartu NEC, bisa disunting manual.
- **Variabel** dan **Pengoptimal**: beri nama pada ukuran dan biarkan program mengubahnya sampai tujuan tercapai (SWR, gain, F/B, impedansi...). Manual memuat contoh langkah demi langkah.
- **Kalkulator**: lihat [Kalkulator](Calcolatori-id).
- **Antena penerima** dan **Derau/GT/Stack**: RDF antena penerima, suhu antena dan G/T (metode TANT dan XLGTa, suhu acuan VE7BQH dan DG7YBN), penumpukan antena.
- **Peringatan**: semua hal meragukan yang ditemukan program dalam model (segmen terlalu pendek atau panjang, kawat dekat tanah, sambungan padat). Tombol **Periksa...** di bilah atas memeriksa 42 kesalahan masukan umum tanpa menghitung.
- **Pengaturan**, **Manual**, **Terima kasih**. Kanan atas: **Bahasa**, **Warna** (18 skema), dan **Tentang**.

## Menyimpan

**Simpan** dan **Simpan sebagai** menulis model sebagai NEC, MMANA, AutoEZ, EZNEC, atau dalam format sendiri **.yanec**, yang juga menyimpan deskripsi dan berkas aslinya.
