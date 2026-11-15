# Hesaplama tercihleri

Kılavuz (21. bölüm) her tercihi kaynaklarıyla açıklar. Kısaca:

- **Motor**: Lawrence Livermore Ulusal Laboratuvarı'nın NEC-2'si olan nec2c'nin C# çevirisi. Öz test onu nec2c ile karşılaştırır; kabul testleri yüzlerce modeli EZNEC ile karşılaştırır. Matris ayrıştırması bloklar hâlinde yapılır (LAPACK gibi) ve pencereye bırakılan biri dışında tüm işlemcileri kullanır.
- **Zemin**: serbest uzay, mükemmel, yansıma katsayısı, Sommerfeld (nec2c tablolarındaki bir hatanın düzeltilmesiyle, necpp ile doğrulandı), radyal ağı; tipik ve özel zeminler. Teller NEC-2 için zemine fazla yakınsa otomatik uyarılar.
- **Teleskopik borular**: NEC-2, kademeli çaplı elemanların rezonansını fazla düşük hesaplar. YA-NEC, D. Leeson W6NL'nin kitabındaki (*Physical Design of Yagi Antennas*, ARRL 1992) düzeltmeyi kullanır, EZNEC ile aynısı: rezonansın altında her zaman, üstünde %45'e kadar; EZNEC değerlerine göre küçük bir uzunluk kalibrasyonuyla.
- **Segmentasyon**: segment denetimi (lambda/1000 ile lambda/10 arası, uzunluk/yarıçap oranı) ve otomatik düzeltme; ortadan beslenen tellerde tek sayıda segment.
- **G/T**: XLGTa gibi 3D diyagram entegrasyonu, TANT gibi yükseliş taraması, VE7BQH (eski ve yeni) ve DG7YBN gökyüzü ve zemin sıcaklıkları.
- **Veri denetimi**: modeli değiştirmeden yanlış veriler için uyaran 42 denetim.
