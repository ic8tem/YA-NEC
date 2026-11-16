# Format berkas

| Format | Membuka | Menyimpan | Catatan |
|---|---|---|---|
| NEC-2 (.nec) | ya | ya | kartu GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY (variabel dan rumus seperti di 4nec2 dan xnec2c) |
| MMANA (.maa) | ya | ya | segmentasi dihitung ulang untuk NEC-2 (MMANA memakai mesin MININEC) |
| AutoEZ (.weq) | ya | ya | variabel dan rumus Excel |
| EZNEC (.ez) | ya | ya | kawat, sumber, beban, saluran, tanah; tanah MININEC dibaca sebagai GN 3 dengan peringatan |
| YO / AO dari K6STI (.yag, .ant) | ya | tidak | |
| YA-NEC (.yanec) | ya | ya | model dalam format aslinya, dengan nama, deskripsi, dan kategori |

Berkas .yanec berisi model **tanpa konversi** (kartu NEC apa adanya, teks .maa, .weq, .yag, .ant; .ez dalam base64): YA-NEC membacanya dengan pengimpor yang sama dengan berkas aslinya, sehingga hasilnya sama persis dengan berkas asli.

Semua rincian ada di bab 10 manual.
