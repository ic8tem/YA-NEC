# Kurulum

YA-NEC kurulmaz: arşivden çıkarılır ve çalıştırılır. .NET gerekmez, pakette zaten vardır. Ayarlar, modeller ve günlükler program klasöründe (`data`, `Registri`) kalır: AppData yok, kayıt defteri yok. Kaldırmak için klasörü silmek yeterlidir.

## Hangi paket

[Releases](https://github.com/ic8tem/YA-NEC/releases) sayfasından:

| Sistem | Paket |
|---|---|
| Windows 10/11 64 bit | `YA-NEC-<sürüm>-win-x64.zip` |
| Windows 10/11 32 bit | `YA-NEC-<sürüm>-win-x86.zip` |
| ARM üzerinde Windows (Snapdragon) | `YA-NEC-<sürüm>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<sürüm>-win7-x64.zip` veya `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<sürüm>-linux-x64.zip` |
| Raspberry Pi OS 64 bit ve diğer aarch64 Linux | `YA-NEC-<sürüm>-linux-arm64.zip` |
| Raspberry Pi OS 32 bit, armv7l | `YA-NEC-<sürüm>-linux-arm.zip` |
| Alpine ve musl Linux | `YA-NEC-<sürüm>-linux-musl-x64.zip` veya `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<sürüm>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<sürüm>-osx-x64.zip` |

Her zip dosyasının yanında indirmeyi doğrulamak için `.sha256` sağlama toplamı bulunur.

## Windows

Zip dosyasını yazılabilir bir klasöre, örneğin `C:\YA-NEC` içine çıkarın (programın ayarlarını kaydedemeyeceği `C:\Program Files` içine değil) ve `YA-NEC.exe` dosyasını çalıştırın. İlk açılışta Windows SmartScreen onay isteyebilir: "Ek bilgi" > "Yine de çalıştır".

**Windows 7 SP1, 8 ve 8.1**: .NET 6 ile derlenmiş `win7` paketlerini kullanın (.NET 8 Windows 7'de çalışmaz). Windows 7 için Service Pack 1 ve KB2533623 ile KB2999226 güncellemeleri gerekir.

## Linux ve Raspberry Pi

```
unzip YA-NEC-<sürüm>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` ve `libicu` gerekir; masaüstü dağıtımlarında zaten bulunurlar. Alpine'de: `apk add icu-libs fontconfig libstdc++`. Raspberry Pi'de hesaplama çalışır, ancak büyük modeller (binlerce segment) zaman ve bellek ister.

## Mac

Zip dosyasını örneğin `~/YA-NEC` içine çıkarın. Program Apple tarafından imzalanmadığı için karantinanın Terminal'den bir kez kaldırılması gerekir:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Küçük ekranlar

Alçak ekranlarda (örneğin 1366 x 768) pencere ekranı kaplayarak açılır ve sonuç kutuları kompakt biçime geçer.
