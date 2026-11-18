# इंस्टॉलेशन

YA-NEC को इंस्टॉल नहीं किया जाता: उसे अनज़िप करें और चलाएँ। .NET की ज़रूरत नहीं, वह पैकेज में ही है। सेटिंग्स, मॉडल और लॉग प्रोग्राम के फ़ोल्डर (`data`, `Registri`) में रहते हैं: न AppData, न रजिस्ट्री। हटाने के लिए बस फ़ोल्डर मिटा दें।

## कौन-सा पैकेज

[Releases](https://github.com/ic8tem/YA-NEC/releases) पृष्ठ से:

| सिस्टम | पैकेज |
|---|---|
| Windows 10/11 64-बिट | `YA-NEC-<संस्करण>-windows-x64-portatile.zip` |
| Windows 10/11 32-बिट | `YA-NEC-<संस्करण>-windows-x86-portatile.zip` |
| ARM पर Windows (Snapdragon) | `YA-NEC-<संस्करण>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<संस्करण>-windows7-x64-portatile.zip` या `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<संस्करण>-linux-x64.tar.gz` |
| Raspberry Pi OS 64-बिट और अन्य aarch64 Linux | `YA-NEC-<संस्करण>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32-बिट, armv7l | `YA-NEC-<संस्करण>-linux-armv7l.tar.gz` |
| Alpine और musl Linux | `YA-NEC-<संस्करण>-alpine-x64.tar.gz` या `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<संस्करण>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<संस्करण>-mac-intel-app.zip` |

हर zip के पास डाउनलोड जाँचने के लिए `.sha256` चेकसम है।

## Windows

zip को किसी लिखने-योग्य फ़ोल्डर में अनज़िप करें, जैसे `C:\YA-NEC` (`C:\Program Files` में नहीं, जहाँ प्रोग्राम अपनी सेटिंग्स सेव नहीं कर पाएगा), और `YA-NEC.exe` चलाएँ। पहली बार चलाने पर Windows SmartScreen पुष्टि माँग सकता है: "More info" > "Run anyway"।

**Windows 7 SP1, 8 और 8.1**: .NET 6 से बने `win7` पैकेज इस्तेमाल करें (.NET 8 Windows 7 पर नहीं चलता)। Windows 7 को Service Pack 1 और अपडेट KB2533623 तथा KB2999226 चाहिए।

## Linux और Raspberry Pi

```
tar -xzf YA-NEC-<संस्करण>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` और `libicu` चाहिए; डेस्कटॉप वितरणों में ये पहले से होते हैं। Alpine पर: `apk add icu-libs fontconfig libstdc++`। Raspberry Pi पर गणना चलती है, पर बड़े मॉडल (हज़ारों सेगमेंट) समय और मेमोरी लेते हैं।

## Mac

zip को, उदाहरण के लिए, `~/YA-NEC` में अनज़िप करें। प्रोग्राम Apple द्वारा हस्ताक्षरित नहीं है, इसलिए Terminal से एक बार क्वारंटीन हटाना होगा:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## छोटी स्क्रीन

कम ऊँचाई वाली स्क्रीन (जैसे 1366 x 768) पर विंडो बड़ी होकर खुलती है और परिणाम बॉक्स संक्षिप्त रूप में आ जाते हैं।
