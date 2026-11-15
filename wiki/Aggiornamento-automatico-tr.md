# Otomatik güncelleme

YA-NEC her 30 dakikada bir (ve açılıştan 2 dakika sonra) bu GitHub'da yayımlanan en son sürümü sessizce denetler. Daha yeniyse:

1. platformunun zip dosyasını ve `.sha256` sağlama toplamını indirir;
2. sağlama toplamını doğrular: yoksa veya farklıysa hiçbir şey kurulmaz;
3. eski dosyaları yenileriyle değiştirir: yeni sürüm **bir sonraki açılışta** devreye girer, durum çubuğunda bir mesajla.

Önceki sürüm, elle geri dönebilmek için `data/aggiornamenti/precedente-<sürüm>` içinde kalır. Ayarlara (`data/config.ini`), günlüklere ve kullanıcı modellerine dokunulmaz. Birden fazla YA-NEC penceresi açıksa yalnızca ilki denetler.

**Ayarlar > Güncelleme** bölümünden kapatılır; **Şimdi denetle** düğmesi 30 dakikayı beklemez. Her adım günlüğe yazılır (AGGIORNAMENTO kategorisi).

**.deb ve .rpm paketleri**, çıktıklarında: yönetici haklarıyla kurulur ve yalnızca paket yöneticisiyle güncellenir (`apt`, `dnf`, `zypper`). Bu paketlerde otomatik güncelleme kapalıdır ve Ayarlar bunu belirtir.
