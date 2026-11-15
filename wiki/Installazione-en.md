# Installation

YA-NEC is not installed: you extract it and run it. No .NET is needed, it is already in the package. Settings, models and logs stay in the program folder (`data`, `Registri`): no AppData, no registry. To remove it, just delete the folder.

## Which package

From the [Releases](https://github.com/ic8tem/YA-NEC/releases) page:

| System | Package |
|---|---|
| Windows 10/11 64-bit | `YA-NEC-<version>-win-x64.zip` |
| Windows 10/11 32-bit | `YA-NEC-<version>-win-x86.zip` |
| Windows on ARM (Snapdragon) | `YA-NEC-<version>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<version>-win7-x64.zip` or `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<version>-linux-x64.zip` |
| Raspberry Pi OS 64-bit and other aarch64 Linux | `YA-NEC-<version>-linux-arm64.zip` |
| Raspberry Pi OS 32-bit, armv7l | `YA-NEC-<version>-linux-arm.zip` |
| Alpine and musl Linux | `YA-NEC-<version>-linux-musl-x64.zip` or `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<version>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<version>-osx-x64.zip` |

Next to each zip there is the `.sha256` checksum to verify the download.

## Windows

Extract the zip into a writable folder, for example `C:\YA-NEC` (not `C:\Program Files`, where the program could not save its settings) and run `YA-NEC.exe`. On first start Windows SmartScreen may ask for confirmation: "More info" > "Run anyway".

**Windows 7 SP1, 8 and 8.1**: use the `win7` packages, built with .NET 6 (.NET 8 does not start on Windows 7). Windows 7 needs Service Pack 1 and updates KB2533623 and KB2999226.

## Linux and Raspberry Pi

```
unzip YA-NEC-<version>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` and `libicu` are required; they are already present on desktop distributions. On Alpine: `apk add icu-libs fontconfig libstdc++`. On the Raspberry Pi the calculation works, but large models (thousands of segments) need time and memory.

## Mac

Extract the zip, for example into `~/YA-NEC`. The program is not signed by Apple, so the quarantine must be removed once, from Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Small screens

On low screens (for example 1366 x 768) the window opens maximized and the result boxes switch to the compact format.
