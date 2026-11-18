# Kurulum

YA-NEC kurulmaz: arşivden çıkarılır ve çalıştırılır. .NET gerekmez, pakette zaten vardır. Ayarlar, modeller ve günlükler program klasöründe (`data`, `Registri`) kalır: AppData yok, kayıt defteri yok. Kaldırmak için klasörü silmek yeterlidir.

## Hangi paket

[Releases](https://github.com/ic8tem/YA-NEC/releases) sayfasından:

| Sistem | Paket |
|---|---|
| Windows 10/11 64 bit | `YA-NEC-<sürüm>-windows-x64-portatile.zip` |
| Windows 10/11 32 bit | `YA-NEC-<sürüm>-windows-x86-portatile.zip` |
| ARM üzerinde Windows (Snapdragon) | `YA-NEC-<sürüm>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<sürüm>-windows7-x64-portatile.zip` veya `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<sürüm>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bit ve diğer aarch64 Linux | `YA-NEC-<sürüm>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bit, armv7l | `YA-NEC-<sürüm>-linux-armv7l.tar.gz` |
| Alpine ve musl Linux | `YA-NEC-<sürüm>-alpine-x64.tar.gz` veya `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<sürüm>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<sürüm>-mac-intel-app.zip` |

Her zip dosyasının yanında indirmeyi doğrulamak için `.sha256` sağlama toplamı bulunur.

## Windows

Zip dosyasını yazılabilir bir klasöre, örneğin `C:\YA-NEC` içine çıkarın (programın ayarlarını kaydedemeyeceği `C:\Program Files` içine değil) ve `YA-NEC.exe` dosyasını çalıştırın. İlk açılışta Windows SmartScreen onay isteyebilir: "Ek bilgi" > "Yine de çalıştır".

**Windows 7 SP1, 8 ve 8.1**: .NET 6 ile derlenmiş `win7` paketlerini kullanın (.NET 8 Windows 7'de çalışmaz). Windows 7 için Service Pack 1 ve KB2533623 ile KB2999226 güncellemeleri gerekir.

## Linux ve Raspberry Pi

```
tar -xzf YA-NEC-<sürüm>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` ve `libicu` gerekir; masaüstü dağıtımlarında zaten bulunurlar. Alpine'de: `apk add icu-libs fontconfig libstdc++`. Raspberry Pi'de hesaplama çalışır, ancak büyük modeller (binlerce segment) zaman ve bellek ister.

## Mac

Zip dosyasını örneğin `~/YA-NEC` içine çıkarın. Program Apple tarafından imzalanmadığı için karantinanın Terminal'den bir kez kaldırılması gerekir:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Küçük ekranlar

Alçak ekranlarda (örneğin 1366 x 768) pencere ekranı kaplayarak açılır ve sonuç kutuları kompakt biçime geçer.
