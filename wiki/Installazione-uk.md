# Встановлення

YA-NEC не встановлюється: його розпаковують і запускають. .NET не потрібен, він уже в пакеті. Налаштування, моделі та журнали залишаються в теці програми (`data`, `Registri`): жодного AppData і реєстру. Щоб видалити програму, досить видалити теку.

## Який пакет

Зі сторінки [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Система | Пакет |
|---|---|
| Windows 10/11, 64 біти | `YA-NEC-<версія>-windows-x64-portatile.zip` |
| Windows 10/11, 32 біти | `YA-NEC-<версія>-windows-x86-portatile.zip` |
| Windows на ARM (Snapdragon) | `YA-NEC-<версія>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<версія>-windows7-x64-portatile.zip` або `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<версія>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 біти та інші Linux aarch64 | `YA-NEC-<версія>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 біти, armv7l | `YA-NEC-<версія>-linux-armv7l.tar.gz` |
| Alpine і Linux з musl | `YA-NEC-<версія>-alpine-x64.tar.gz` або `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<версія>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<версія>-mac-intel-app.zip` |

Поруч із кожним zip є контрольна сума `.sha256` для перевірки завантаження.

## Windows

Розпакуйте zip у теку з правом запису, наприклад `C:\YA-NEC` (не в `C:\Program Files`, де програма не зможе зберігати налаштування), і запустіть `YA-NEC.exe`. Під час першого запуску Windows SmartScreen може попросити підтвердження: «Докладніше» > «Однаково запустити».

**Windows 7 SP1, 8 і 8.1**: використовуйте пакети `win7`, зібрані з .NET 6 (.NET 8 не запускається у Windows 7). Для Windows 7 потрібні Service Pack 1 та оновлення KB2533623 і KB2999226.

## Linux і Raspberry Pi

```
tar -xzf YA-NEC-<версія>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Потрібні `libfontconfig1` і `libicu`, вони вже є в дистрибутивах із робочим столом. В Alpine: `apk add icu-libs fontconfig libstdc++`. На Raspberry Pi розрахунок працює, але великі моделі (тисячі сегментів) потребують часу й пам'яті.

## Mac

Розпакуйте zip, наприклад у `~/YA-NEC`. Програма не підписана Apple, тому один раз треба зняти карантин у Терміналі:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Невеликі екрани

На невисоких екранах (наприклад, 1366 x 768) вікно відкривається розгорнутим, а поля результатів переходять у компактний вигляд.
