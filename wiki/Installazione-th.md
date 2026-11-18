# การติดตั้ง

YA-NEC ไม่ต้องติดตั้ง: แตกไฟล์แล้วเปิดใช้ได้เลย ไม่ต้องใช้ .NET เพราะรวมอยู่ในแพ็กเกจแล้ว การตั้งค่า แบบจำลอง และบันทึก จะอยู่ในโฟลเดอร์ของโปรแกรม (`data`, `Registri`): ไม่ใช้ AppData ไม่ใช้รีจิสทรี หากต้องการลบ ก็แค่ลบโฟลเดอร์ทิ้ง

## แพ็กเกจใด

จากหน้า [Releases](https://github.com/ic8tem/YA-NEC/releases):

| ระบบ | แพ็กเกจ |
|---|---|
| Windows 10/11 64 บิต | `YA-NEC-<เวอร์ชัน>-windows-x64-portatile.zip` |
| Windows 10/11 32 บิต | `YA-NEC-<เวอร์ชัน>-windows-x86-portatile.zip` |
| Windows บน ARM (Snapdragon) | `YA-NEC-<เวอร์ชัน>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<เวอร์ชัน>-windows7-x64-portatile.zip` หรือ `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<เวอร์ชัน>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 บิต และ Linux aarch64 อื่น ๆ | `YA-NEC-<เวอร์ชัน>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 บิต, armv7l | `YA-NEC-<เวอร์ชัน>-linux-armv7l.tar.gz` |
| Alpine และ Linux ที่ใช้ musl | `YA-NEC-<เวอร์ชัน>-alpine-x64.tar.gz` หรือ `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<เวอร์ชัน>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<เวอร์ชัน>-mac-intel-app.zip` |

ข้าง ๆ zip แต่ละไฟล์มีค่าตรวจสอบ `.sha256` ไว้ตรวจไฟล์ที่ดาวน์โหลด

## Windows

แตกไฟล์ zip ไปยังโฟลเดอร์ที่เขียนได้ เช่น `C:\YA-NEC` (ไม่ใช่ `C:\Program Files` ซึ่งโปรแกรมจะบันทึกการตั้งค่าไม่ได้) แล้วเปิด `YA-NEC.exe` ในการเปิดครั้งแรก Windows SmartScreen อาจขอการยืนยัน: "More info" > "Run anyway"

**Windows 7 SP1, 8 และ 8.1**: ใช้แพ็กเกจ `win7` ที่สร้างด้วย .NET 6 (.NET 8 ไม่ทำงานบน Windows 7) Windows 7 ต้องมี Service Pack 1 และอัปเดต KB2533623 กับ KB2999226

## Linux และ Raspberry Pi

```
tar -xzf YA-NEC-<เวอร์ชัน>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

ต้องมี `libfontconfig1` และ `libicu` ซึ่งดิสทริบิวชันแบบเดสก์ท็อปมีอยู่แล้ว บน Alpine: `apk add icu-libs fontconfig libstdc++` บน Raspberry Pi การคำนวณทำงานได้ แต่แบบจำลองขนาดใหญ่ (หลายพันเซกเมนต์) ต้องใช้เวลาและหน่วยความจำ

## Mac

แตกไฟล์ zip เช่น ไปที่ `~/YA-NEC` โปรแกรมไม่ได้รับการลงนามจาก Apple จึงต้องลบการกักกันหนึ่งครั้งจาก Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## หน้าจอขนาดเล็ก

บนหน้าจอที่เตี้ย (เช่น 1366 x 768) หน้าต่างจะเปิดแบบขยายเต็ม และกล่องผลลัพธ์จะเปลี่ยนเป็นแบบกะทัดรัด
