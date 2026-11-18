<div dir="rtl">

# تنصیب

YA-NEC کو انسٹال نہیں کیا جاتا: اسے ان زِپ کریں اور چلائیں۔ .NET کی ضرورت نہیں، وہ پیکج میں شامل ہے۔ سیٹنگز، ماڈل اور لاگ پروگرام کے فولڈر (`data`، `Registri`) میں رہتے ہیں: نہ AppData، نہ رجسٹری۔ ہٹانے کے لیے صرف فولڈر حذف کر دیں۔

## کون سا پیکج

[Releases](https://github.com/ic8tem/YA-NEC/releases) صفحے سے:

| سسٹم | پیکج |
|---|---|
| Windows 10/11 64 بٹ | `YA-NEC-<ورژن>-windows-x64-portatile.zip` |
| Windows 10/11 32 بٹ | `YA-NEC-<ورژن>-windows-x86-portatile.zip` |
| ARM پر Windows (Snapdragon) | `YA-NEC-<ورژن>-windows-arm64-portatile.zip` |
| Windows 7 SP1، 8، 8.1 | `YA-NEC-<ورژن>-windows7-x64-portatile.zip` یا `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<ورژن>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 بٹ اور دیگر aarch64 Linux | `YA-NEC-<ورژن>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 بٹ، armv7l | `YA-NEC-<ورژن>-linux-armv7l.tar.gz` |
| Alpine اور musl Linux | `YA-NEC-<ورژن>-alpine-x64.tar.gz` یا `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<ورژن>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<ورژن>-mac-intel-app.zip` |

ہر zip کے ساتھ ڈاؤن لوڈ کی جانچ کے لیے `.sha256` چیک سم موجود ہے۔

## Windows

zip کو کسی ایسے فولڈر میں ان زِپ کریں جس میں لکھنے کی اجازت ہو، مثلاً `C:\YA-NEC` (نہ کہ `C:\Program Files` میں، جہاں پروگرام اپنی سیٹنگز محفوظ نہیں کر سکے گا)، اور `YA-NEC.exe` چلائیں۔ پہلی بار چلانے پر Windows SmartScreen تصدیق مانگ سکتا ہے: "More info" > "Run anyway"۔

**Windows 7 SP1، 8 اور 8.1**: .NET 6 سے بنے `win7` پیکج استعمال کریں (.NET 8 Windows 7 پر نہیں چلتا)۔ Windows 7 کو Service Pack 1 اور اپ ڈیٹس KB2533623 اور KB2999226 درکار ہیں۔

## Linux اور Raspberry Pi

```
tar -xzf YA-NEC-<ورژن>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` اور `libicu` درکار ہیں؛ یہ ڈیسک ٹاپ ڈسٹری بیوشنز میں پہلے سے موجود ہوتے ہیں۔ Alpine پر: `apk add icu-libs fontconfig libstdc++`۔ Raspberry Pi پر حساب چلتا ہے، لیکن بڑے ماڈل (ہزاروں سیگمنٹ) وقت اور میموری لیتے ہیں۔

## Mac

zip کو مثلاً `~/YA-NEC` میں ان زِپ کریں۔ پروگرام Apple کا دستخط شدہ نہیں، اس لیے Terminal سے ایک بار قرنطینہ ہٹانا ہوگا:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## چھوٹی اسکرینیں

کم اونچائی والی اسکرینوں (مثلاً 1366 x 768) پر ونڈو مکمل سائز میں کھلتی ہے اور نتائج کے خانے مختصر شکل میں آ جاتے ہیں۔

</div>
