<div dir="rtl">

# التثبيت

لا يحتاج YA-NEC إلى تثبيت: يُفك ضغطه ويُشغَّل. لا حاجة إلى ‎.NET فهو موجود في الحزمة. تبقى الإعدادات والنماذج والسجلات في مجلد البرنامج (`data` و`Registri`): لا AppData ولا سجل النظام. لإزالته يكفي حذف المجلد.

## أي حزمة

من صفحة [Releases](https://github.com/ic8tem/YA-NEC/releases):

| النظام | الحزمة |
|---|---|
| Windows 10/11 بنظام 64 بت | `YA-NEC-<الإصدار>-windows-x64-portatile.zip` |
| Windows 10/11 بنظام 32 بت | `YA-NEC-<الإصدار>-windows-x86-portatile.zip` |
| Windows على ARM (Snapdragon) | `YA-NEC-<الإصدار>-windows-arm64-portatile.zip` |
| Windows 7 SP1 و8 و8.1 | `YA-NEC-<الإصدار>-windows7-x64-portatile.zip` أو `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<الإصدار>-linux-x64.tar.gz` |
| Raspberry Pi OS بنظام 64 بت وأنظمة Linux aarch64 الأخرى | `YA-NEC-<الإصدار>-linux-arm64.tar.gz` |
| Raspberry Pi OS بنظام 32 بت، armv7l | `YA-NEC-<الإصدار>-linux-armv7l.tar.gz` |
| Alpine وأنظمة Linux بمكتبة musl | `YA-NEC-<الإصدار>-alpine-x64.tar.gz` أو `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<الإصدار>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<الإصدار>-mac-intel-app.zip` |

بجانب كل ملف zip توجد بصمة `.sha256` للتحقق من التنزيل.

## Windows

فك ضغط الملف في مجلد قابل للكتابة، مثل `C:\YA-NEC` (وليس في `C:\Program Files` حيث لا يستطيع البرنامج حفظ إعداداته)، ثم شغّل `YA-NEC.exe`. عند التشغيل الأول قد يطلب Windows SmartScreen تأكيدًا: «مزيد من المعلومات» > «التشغيل على أي حال».

**Windows 7 SP1 و8 و8.1**: استخدم حزم `win7` المبنية بـ ‎.NET 6 (لا يعمل ‎.NET 8 على Windows 7). يحتاج Windows 7 إلى Service Pack 1 والتحديثين KB2533623 وKB2999226.

## Linux وRaspberry Pi

```
tar -xzf YA-NEC-<الإصدار>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

يلزم `libfontconfig1` و`libicu`، وهما موجودان أصلًا في توزيعات سطح المكتب. على Alpine: `apk add icu-libs fontconfig libstdc++`. يعمل الحساب على Raspberry Pi، لكن النماذج الكبيرة (آلاف المقاطع) تحتاج إلى وقت وذاكرة.

## Mac

فك ضغط الملف، مثلًا في `~/YA-NEC`. البرنامج غير موقَّع من Apple، لذا يجب إزالة الحجر مرة واحدة من الطرفية:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## الشاشات الصغيرة

على الشاشات المنخفضة (مثل 1366 × 768) تُفتح النافذة مكبّرة وتتحول مربعات النتائج إلى الشكل المضغوط.

</div>
