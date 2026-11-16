# ইনস্টলেশন

YA-NEC ইনস্টল করতে হয় না: আনজিপ করে চালান। .NET লাগবে না, সেটি প্যাকেজেই আছে। সেটিংস, মডেল ও লগ প্রোগ্রামের ফোল্ডারে (`data`, `Registri`) থাকে: কোনো AppData নেই, রেজিস্ট্রি নেই। সরাতে চাইলে শুধু ফোল্ডারটি মুছে দিন।

## কোন প্যাকেজ

[Releases](https://github.com/ic8tem/YA-NEC/releases) পৃষ্ঠা থেকে:

| সিস্টেম | প্যাকেজ |
|---|---|
| Windows 10/11 64-বিট | `YA-NEC-<সংস্করণ>-win-x64.zip` |
| Windows 10/11 32-বিট | `YA-NEC-<সংস্করণ>-win-x86.zip` |
| ARM-এ Windows (Snapdragon) | `YA-NEC-<সংস্করণ>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<সংস্করণ>-win7-x64.zip` বা `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<সংস্করণ>-linux-x64.zip` |
| Raspberry Pi OS 64-বিট ও অন্যান্য aarch64 Linux | `YA-NEC-<সংস্করণ>-linux-arm64.zip` |
| Raspberry Pi OS 32-বিট, armv7l | `YA-NEC-<সংস্করণ>-linux-arm.zip` |
| Alpine ও musl Linux | `YA-NEC-<সংস্করণ>-linux-musl-x64.zip` বা `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<সংস্করণ>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<সংস্করণ>-osx-x64.zip` |

প্রতিটি zip-এর পাশে ডাউনলোড যাচাইয়ের জন্য `.sha256` চেকসাম আছে।

## Windows

zip-টি লেখার অনুমতি আছে এমন ফোল্ডারে আনজিপ করুন, যেমন `C:\YA-NEC` (`C:\Program Files`-এ নয়, যেখানে প্রোগ্রাম তার সেটিংস সংরক্ষণ করতে পারবে না), তারপর `YA-NEC.exe` চালান। প্রথমবার চালালে Windows SmartScreen নিশ্চিতকরণ চাইতে পারে: "More info" > "Run anyway"।

**Windows 7 SP1, 8 ও 8.1**: .NET 6 দিয়ে তৈরি `win7` প্যাকেজ ব্যবহার করুন (.NET 8 Windows 7-এ চলে না)। Windows 7-এর জন্য Service Pack 1 এবং KB2533623 ও KB2999226 আপডেট লাগবে।

## Linux ও Raspberry Pi

```
unzip YA-NEC-<সংস্করণ>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` ও `libicu` লাগবে; ডেস্কটপ ডিস্ট্রিবিউশনে এগুলি আগে থেকেই থাকে। Alpine-এ: `apk add icu-libs fontconfig libstdc++`। Raspberry Pi-তে গণনা চলে, তবে বড় মডেলে (হাজার হাজার সেগমেন্ট) সময় ও মেমরি লাগে।

## Mac

zip-টি আনজিপ করুন, যেমন `~/YA-NEC`-এ। প্রোগ্রামটি Apple-স্বাক্ষরিত নয়, তাই Terminal থেকে একবার কোয়ারেন্টিন সরাতে হবে:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## ছোট পর্দা

কম উচ্চতার পর্দায় (যেমন 1366 x 768) উইন্ডো পূর্ণ আকারে খোলে এবং ফলাফলের বাক্সগুলি সংক্ষিপ্ত রূপে চলে যায়।
