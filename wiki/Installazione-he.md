<div dir="rtl">

# התקנה

את YA-NEC לא מתקינים: מחלצים ומפעילים. אין צורך ב-.NET, הוא כבר בתוך החבילה. ההגדרות, המודלים והיומנים נשארים בתיקיית התוכנה (`data`, ‏`Registri`): בלי AppData ובלי רישום המערכת. כדי להסיר, מספיק למחוק את התיקייה.

## איזו חבילה

מדף [Releases](https://github.com/ic8tem/YA-NEC/releases):

| מערכת | חבילה |
|---|---|
| Windows 10/11 ‏64 סיביות | `YA-NEC-<גרסה>-win-x64.zip` |
| Windows 10/11 ‏32 סיביות | `YA-NEC-<גרסה>-win-x86.zip` |
| Windows על ARM ‏(Snapdragon) | `YA-NEC-<גרסה>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<גרסה>-win7-x64.zip` או `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<גרסה>-linux-x64.zip` |
| Raspberry Pi OS ‏64 סיביות ו-Linux aarch64 אחרות | `YA-NEC-<גרסה>-linux-arm64.zip` |
| Raspberry Pi OS ‏32 סיביות, armv7l | `YA-NEC-<גרסה>-linux-arm.zip` |
| Alpine ו-Linux עם musl | `YA-NEC-<גרסה>-linux-musl-x64.zip` או `-linux-musl-arm64.zip` |
| Mac Apple Silicon ‏(M1-M4) | `YA-NEC-<גרסה>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<גרסה>-osx-x64.zip` |

ליד כל zip נמצא סכום הביקורת `.sha256` לבדיקת ההורדה.

## Windows

חלצו את ה-zip לתיקייה עם הרשאת כתיבה, למשל `C:\YA-NEC` (לא ל-`C:\Program Files`, שבה התוכנה לא תוכל לשמור את ההגדרות), והפעילו את `YA-NEC.exe`. בהפעלה הראשונה Windows SmartScreen עשוי לבקש אישור: "מידע נוסף" > "הפעל בכל זאת".

**Windows 7 SP1, ‏8 ו-8.1**: השתמשו בחבילות `win7`, שנבנו עם ‎.NET 6 ‏(‎.NET 8 לא עולה ב-Windows 7). ‏Windows 7 צריך את Service Pack 1 ואת העדכונים KB2533623 ו-KB2999226.

## Linux ו-Raspberry Pi

```
unzip YA-NEC-<גרסה>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

נדרשים `libfontconfig1` ו-`libicu`, שכבר קיימים בהפצות שולחן עבודה. ב-Alpine: `apk add icu-libs fontconfig libstdc++`. ב-Raspberry Pi החישוב עובד, אבל מודלים גדולים (אלפי מקטעים) דורשים זמן וזיכרון.

## Mac

חלצו את ה-zip, למשל ל-`~/YA-NEC`. התוכנה אינה חתומה על ידי Apple, ולכן יש להסיר את ההסגר פעם אחת מהמסוף:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## מסכים קטנים

במסכים נמוכים (למשל 1366 x 768) החלון נפתח מוגדל ותיבות התוצאות עוברות לתצוגה דחוסה.

</div>
