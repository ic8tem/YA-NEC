# Dosya biçimleri

| Biçim | Açar | Kaydeder | Notlar |
|---|---|---|---|
| NEC-2 (.nec) | evet | evet | GW, GA, GH, GM, GR, GS, GX, GC, GN, LD, EX, TL, NT, FR, SY kartları (değişken ve formüller 4nec2 ve xnec2c'deki gibi) |
| MMANA (.maa) | evet | evet | segmentasyon NEC-2 için yeniden hesaplanır (MMANA bir MININEC motoru kullanır) |
| AutoEZ (.weq) | evet | evet | değişkenler ve Excel formülleri |
| EZNEC (.ez) | evet | evet | teller, kaynaklar, yükler, hatlar, zemin; MININEC zemini bir uyarıyla GN 3 olarak okunur |
| K6STI'nin YO / AO dosyaları (.yag, .ant) | evet | hayır | |
| YA-NEC (.yanec) | evet | evet | özgün biçimindeki model; ad, açıklama ve kategoriyle |

.yanec dosyası modeli **dönüştürmeden** içerir (NEC kartları olduğu gibi, .maa, .weq, .yag, .ant metni; .ez base64 ile): YA-NEC onu özgün dosyayla aynı içe aktarıcıyla okur, bu yüzden sonuçlar özgün dosyayla aynıdır.

Tüm ayrıntılar kılavuzun 10. bölümünde.
