# Установка

YA-NEC не устанавливается: его распаковывают и запускают. .NET не нужен, он уже в пакете. Настройки, модели и журналы остаются в папке программы (`data`, `Registri`): никакого AppData и реестра. Чтобы удалить программу, достаточно удалить папку.

## Какой пакет

Со страницы [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Система | Пакет |
|---|---|
| Windows 10/11, 64 бита | `YA-NEC-<версия>-windows-x64-portatile.zip` |
| Windows 10/11, 32 бита | `YA-NEC-<версия>-windows-x86-portatile.zip` |
| Windows на ARM (Snapdragon) | `YA-NEC-<версия>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<версия>-windows7-x64-portatile.zip` или `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<версия>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 бита и другие Linux aarch64 | `YA-NEC-<версия>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 бита, armv7l | `YA-NEC-<версия>-linux-armv7l.tar.gz` |
| Alpine и Linux с musl | `YA-NEC-<версия>-alpine-x64.tar.gz` или `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<версия>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<версия>-mac-intel-app.zip` |

Рядом с каждым zip лежит контрольная сумма `.sha256` для проверки загрузки.

## Windows

Распакуйте zip в папку с правом записи, например `C:\YA-NEC` (не в `C:\Program Files`, где программа не сможет сохранять настройки), и запустите `YA-NEC.exe`. При первом запуске Windows SmartScreen может запросить подтверждение: «Подробнее» > «Выполнить в любом случае».

**Windows 7 SP1, 8 и 8.1**: используйте пакеты `win7`, собранные с .NET 6 (.NET 8 не запускается в Windows 7). Для Windows 7 нужны Service Pack 1 и обновления KB2533623 и KB2999226.

## Linux и Raspberry Pi

```
tar -xzf YA-NEC-<версия>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Нужны `libfontconfig1` и `libicu`, они уже есть в дистрибутивах с рабочим столом. В Alpine: `apk add icu-libs fontconfig libstdc++`. На Raspberry Pi расчёт работает, но большие модели (тысячи сегментов) требуют времени и памяти.

## Mac

Распакуйте zip, например в `~/YA-NEC`. Программа не подписана Apple, поэтому один раз нужно снять карантин в Терминале:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Небольшие экраны

На невысоких экранах (например, 1366 x 768) окно открывается развёрнутым, а поля результатов переходят в компактный вид.
