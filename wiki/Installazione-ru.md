# Установка

YA-NEC не устанавливается: его распаковывают и запускают. .NET не нужен, он уже в пакете. Настройки, модели и журналы остаются в папке программы (`data`, `Registri`): никакого AppData и реестра. Чтобы удалить программу, достаточно удалить папку.

## Какой пакет

Со страницы [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Система | Пакет |
|---|---|
| Windows 10/11, 64 бита | `YA-NEC-<версия>-win-x64.zip` |
| Windows 10/11, 32 бита | `YA-NEC-<версия>-win-x86.zip` |
| Windows на ARM (Snapdragon) | `YA-NEC-<версия>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<версия>-win7-x64.zip` или `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<версия>-linux-x64.zip` |
| Raspberry Pi OS 64 бита и другие Linux aarch64 | `YA-NEC-<версия>-linux-arm64.zip` |
| Raspberry Pi OS 32 бита, armv7l | `YA-NEC-<версия>-linux-arm.zip` |
| Alpine и Linux с musl | `YA-NEC-<версия>-linux-musl-x64.zip` или `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<версия>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<версия>-osx-x64.zip` |

Рядом с каждым zip лежит контрольная сумма `.sha256` для проверки загрузки.

## Windows

Распакуйте zip в папку с правом записи, например `C:\YA-NEC` (не в `C:\Program Files`, где программа не сможет сохранять настройки), и запустите `YA-NEC.exe`. При первом запуске Windows SmartScreen может запросить подтверждение: «Подробнее» > «Выполнить в любом случае».

**Windows 7 SP1, 8 и 8.1**: используйте пакеты `win7`, собранные с .NET 6 (.NET 8 не запускается в Windows 7). Для Windows 7 нужны Service Pack 1 и обновления KB2533623 и KB2999226.

## Linux и Raspberry Pi

```
unzip YA-NEC-<версия>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Нужны `libfontconfig1` и `libicu`, они уже есть в дистрибутивах с рабочим столом. В Alpine: `apk add icu-libs fontconfig libstdc++`. На Raspberry Pi расчёт работает, но большие модели (тысячи сегментов) требуют времени и памяти.

## Mac

Распакуйте zip, например в `~/YA-NEC`. Программа не подписана Apple, поэтому один раз нужно снять карантин в Терминале:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Небольшие экраны

На невысоких экранах (например, 1366 x 768) окно открывается развёрнутым, а поля результатов переходят в компактный вид.
