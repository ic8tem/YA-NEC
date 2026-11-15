[Italiano](README.md) · **English** · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · [Português](README.pt.md) · [日本語](README.ja.md) · [한국어](README.ko.md)

# YA-NEC

**NEC-2 wire antenna simulator** for Windows, Linux and macOS (C#, .NET 8, Avalonia).
By Costantino Cerrotta **IC8TEM**.

> *The story of this program will be written by you: with your projects, your ideas, your ingenuity, your discoveries and your certainties.*

The engine is a C# translation of **nec2c** (the NEC-2 of Lawrence Livermore National Laboratory), checked at every release against nec2c and EZNEC. It opens **NEC (.nec)**, **MMANA (.maa)**, **AutoEZ (.weq)**, **EZNEC (.ez)**, **K6STI YO/AO (.yag, .ant)** models and its own **.yanec** format.

Features: impedance, SWR, gain, 2D and 3D patterns, frequency sweep and Smith chart; Leeson correction for telescopic (stepped-diameter) tubing, as in EZNEC; grounds (perfect, reflection coefficient, Sommerfeld, radial screen); variables, formulas and optimizer; more than 40 calculators (DL6WU long Yagi, Quagi, LPDA, Moxon, hexbeam, multiband dipoles and Yagis, four-square, verticals, dishes and feeds, element brackets and more); receiving antennas and RDF; noise, G/T and stacking; coax chart, matching lines and baluns; input data checks. Every calculation choice and every checkbox is explained in the [manual](doc/Manuale_d_uso.pdf), also available inside the program (Manual tab). More in the [Wiki](https://github.com/ic8tem/YA-NEC/wiki).

**Language:** the interface is in Italian, with English partly available (Language menu, top right). The manual and the Wiki are in Italian. More languages will follow.

## Download

From the **[Releases](https://github.com/ic8tem/YA-NEC/releases)** page download the zip for your platform, extract it to a **writable folder** (for example `C:\YA-NEC` or `~/YA-NEC`) and run `YA-NEC.exe` (Linux and macOS: `YA-NEC`). No .NET installation is needed. Settings and data stay in the `data` folder next to the program: no AppData, no registry.

| System | Package |
|---|---|
| Windows 10/11 64-bit | `YA-NEC-<version>-win-x64.zip` |
| Windows 10/11 32-bit | `YA-NEC-<version>-win-x86.zip` |
| Windows on ARM (Snapdragon) | `YA-NEC-<version>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 (.NET 6) | `YA-NEC-<version>-win7-x64.zip`, `-win7-x86.zip` (updates KB2533623 and KB2999226 required) |
| Linux x86_64 | `YA-NEC-<version>-linux-x64.zip` |
| Linux ARM64 (Raspberry Pi OS 64-bit, other aarch64) | `YA-NEC-<version>-linux-arm64.zip` |
| Linux ARM 32-bit (Raspberry Pi OS 32-bit, armv7l) | `YA-NEC-<version>-linux-arm.zip` |
| Alpine and other musl Linux | `YA-NEC-<version>-linux-musl-x64.zip`, `-linux-musl-arm64.zip` |
| macOS Apple Silicon (M1-M4) | `YA-NEC-<version>-osx-arm64.zip` |
| macOS Intel | `YA-NEC-<version>-osx-x64.zip` |

Each zip comes with its `.sha256` checksum.

- **Linux**: after extracting, `chmod +x YA-NEC` if needed; `libfontconfig1` and `libicu` are required (already present on desktop distributions).
- **macOS**: the program is not signed by Apple. After extracting, in Terminal: `xattr -dr com.apple.quarantine ~/YA-NEC` (the extracted folder), then `~/YA-NEC/YA-NEC`.

## Automatic update

Every 30 minutes the program silently checks for a new version here. If there is one, it downloads the zip for its platform, verifies the SHA-256 checksum and installs it: the new version starts **at the next launch**, with a message in the status bar. The previous version is kept in `data/aggiornamenti/precedente-<version>`; settings (`data/config.ini`) and user models are not touched. Without a checksum, or with a different one, nothing is installed. Updates can be turned off in **Settings > Update**.

**Exception: Debian/Ubuntu (.deb) and Fedora/openSUSE (.rpm) packages.** When available, they will install into system folders with administrator rights, so the program cannot (and must not) change itself: they will be updated with the package manager. In those packages automatic update is off, and Settings says so.

## Building from source

.NET 8 SDK required. Windows: `compila.bat` (program in `build\win-x64`), `compila.bat /distribuzione` for the self-contained version, `compila_win7.bat` for Windows 7. Linux and macOS: `./compila.sh`. Details in [LEGGIMI.txt](LEGGIMI.txt) (Italian).

## Credits and license

**GPL-3.0** ([LICENSE](LICENSE), [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)), like nec2c and xnec2c from which the engine derives. YA-NEC exists thanks to the work of fellow radio amateurs and researchers: the full list is in [CREDITI.txt](CREDITI.txt) and in the **Credits** tab. Thanks to Luis IV3PRK for the free publications on his website.

## Contacts

ic8tem@gmail.com · [QRZ](https://www.qrz.com/db/ic8tem) · [Facebook group](https://www.facebook.com/groups/1675310160982493) · donations: [ko-fi.com/ic8tem](https://ko-fi.com/ic8tem)
