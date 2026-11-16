# Pilihan perhitungan

Manual (bab 21) menjelaskan setiap pilihan beserta sumbernya. Singkatnya:

- **Mesin**: terjemahan C# dari nec2c, yaitu NEC-2 milik Lawrence Livermore National Laboratory. Uji mandiri membandingkannya dengan nec2c; uji penerimaan membandingkan ratusan model dengan EZNEC. Faktorisasi matriks dilakukan per blok (seperti LAPACK) dan memakai semua prosesor kecuali satu, yang disisakan untuk jendela.
- **Tanah**: ruang bebas, sempurna, koefisien pantul, Sommerfeld (dengan perbaikan cacat nec2c pada tabel, diperiksa dengan necpp), jaringan radial; tanah umum dan tanah khusus. Peringatan otomatis bila kawat terlalu dekat dengan tanah untuk NEC-2.
- **Pipa teleskopik**: NEC-2 membuat elemen berdiameter bertingkat beresonansi terlalu rendah. YA-NEC memakai koreksi dari buku D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), sama seperti EZNEC: di bawah resonansi selalu, di atasnya hingga 45 %, dengan sedikit kalibrasi panjang berdasarkan nilai EZNEC.
- **Segmentasi**: pemeriksaan segmen (antara lambda/1000 dan lambda/10, rasio panjang/jari-jari) dengan perbaikan otomatis; jumlah segmen ganjil pada kawat yang dicatu di tengah.
- **G/T**: integrasi diagram 3D seperti XLGTa, pemindaian elevasi seperti TANT, suhu langit dan tanah dari VE7BQH (lama dan baru) dan DG7YBN.
- **Pemeriksaan data**: 42 pemeriksaan yang memperingatkan data yang salah tanpa mengubah model.
