# İlk adımlar

1. **Örnekler** (komut çubuğu) hazır modellerden birini açar: dipoller, ground plane, VHF Yagi'ler (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), HF Yagi'ler (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, 1296 MHz döngü Yagi ve diğerleri. **Model aç** bir NEC, MMANA, AutoEZ, EZNEC veya K6STI dosyası açar; dosyayı pencereye sürükleyebilirsiniz de.
2. **Bandı** (2200 m'den 3 cm'ye, IARU 1. Bölge sınırları) ya da serbest bir frekansı seçin, sonra **Hesapla**.
3. **Sonuç kutuları** empedansı, SWR'yi, kazancı, F/B, F/R, -3 dB hüzme genişliklerini, verimi ve AGT'yi (yakınsama testi: 0 dB'ye yakınsa model güvenilirdir) gösterir.
4. **Tarama** frekans çevresindeki bandı hesaplar: SWR, R/X, kazanç, F/B ve Smith abağı.

## Sekmeler

- **3D yapı**: anten (sürükle = döndür, tekerlek = yakınlaştır, sağ tuş = kaydır, çift tıklama = ilk görünüm), zemin türüne ve yüksekliğe göre çizilir. Altta hesap tablosu tarzında **tel tablosu**: hücreler sayı, değişken ve formül kabul eder. Sağ altta **model nesneleri**: kaynaklar, yükler, hatlar, transformatörler.
- **Diyagramlar**: yatay, dikey ve 3D, -3 dB açısıyla.
- **Frekans, Smith, Akımlar**: tarama sonuçları ve tellerdeki akımlar.
- **NEC modeli**: elle düzenlenebilen NEC kartları.
- **Değişkenler** ve **Optimizasyon**: ölçülere ad verin ve program bir hedefe ulaşana kadar onları değiştirsin (SWR, kazanç, F/B, empedans...). Kılavuzda adım adım örnekler vardır.
- **Hesaplayıcılar**: bkz. [Hesaplayıcılar](Calcolatori-tr).
- **Alıcı antenler** ve **Gürültü/GT/Yığın**: alıcı antenlerin RDF'si, anten sıcaklığı ve G/T (TANT ve XLGTa yöntemi, VE7BQH ve DG7YBN referans sıcaklıkları), anten yığınları.
- **Uyarılar**: programın modelde bulduğu şüpheli her şey (çok kısa veya çok uzun segmentler, zemine yakın teller, kalabalık bağlantı noktaları). Üst çubuktaki **Denetle...** düğmesi hesap yapmadan 42 tipik giriş hatasını denetler.
- **Ayarlar**, **Kılavuz**, **Teşekkürler**. Sağ üstte: **Dil**, **Renkler** (18 şema) ve **Hakkında**.

## Kaydetme

**Kaydet** ve **Farklı kaydet**, modeli NEC, MMANA, AutoEZ, EZNEC biçiminde ya da açıklamayı ve özgün dosyayı da saklayan kendi **.yanec** biçiminde yazar.
