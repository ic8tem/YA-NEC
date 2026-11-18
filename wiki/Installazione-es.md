# Instalación

YA-NEC no se instala: se extrae y se ejecuta. No hace falta .NET, ya va dentro del paquete. La configuración, los modelos y los registros quedan en la carpeta del programa (`data`, `Registri`): nada en AppData ni en el registro. Para quitarlo basta con borrar la carpeta.

## Qué paquete

En la página [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistema | Paquete |
|---|---|
| Windows 10/11 64 bits | `YA-NEC-<versión>-windows-x64-portatile.zip` |
| Windows 10/11 32 bits | `YA-NEC-<versión>-windows-x86-portatile.zip` |
| Windows en ARM (Snapdragon) | `YA-NEC-<versión>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<versión>-windows7-x64-portatile.zip` o `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<versión>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bits y otros Linux aarch64 | `YA-NEC-<versión>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bits, armv7l | `YA-NEC-<versión>-linux-armv7l.tar.gz` |
| Alpine y Linux con musl | `YA-NEC-<versión>-alpine-x64.tar.gz` o `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<versión>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<versión>-mac-intel-app.zip` |

Junto a cada zip está la huella `.sha256` para comprobar la descarga.

## Windows

Extraer el zip en una carpeta con permiso de escritura, por ejemplo `C:\YA-NEC` (no en `C:\Archivos de programa`, donde el programa no podría guardar su configuración) y ejecutar `YA-NEC.exe`. En el primer arranque Windows SmartScreen puede pedir confirmación: «Más información» > «Ejecutar de todas formas».

**Windows 7 SP1, 8 y 8.1**: usar los paquetes `win7`, compilados con .NET 6 (.NET 8 no arranca en Windows 7). Windows 7 necesita el Service Pack 1 y las actualizaciones KB2533623 y KB2999226.

## Linux y Raspberry Pi

```
tar -xzf YA-NEC-<versión>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Se necesitan `libfontconfig1` y `libicu`, ya presentes en las distribuciones de escritorio. En Alpine: `apk add icu-libs fontconfig libstdc++`. En la Raspberry Pi el cálculo funciona, pero los modelos grandes (miles de segmentos) requieren tiempo y memoria.

## Mac

Extraer el zip, por ejemplo en `~/YA-NEC`. El programa no está firmado por Apple, así que hay que quitar la cuarentena una vez, desde el Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Pantallas pequeñas

En pantallas bajas (por ejemplo 1366 x 768) la ventana se abre maximizada y los recuadros de resultados pasan al formato compacto.
