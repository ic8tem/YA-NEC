# Pag-install

Hindi kailangang i-install ang YA-NEC: i-extract at patakbuhin lang. Hindi kailangan ang .NET, kasama na ito sa package. Ang mga setting, model at log ay nananatili sa folder ng programa (`data`, `Registri`): walang AppData, walang registry. Para alisin ito, burahin lang ang folder.

## Aling package

Mula sa pahinang [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistema | Package |
|---|---|
| Windows 10/11 64-bit | `YA-NEC-<bersyon>-windows-x64-portatile.zip` |
| Windows 10/11 32-bit | `YA-NEC-<bersyon>-windows-x86-portatile.zip` |
| Windows sa ARM (Snapdragon) | `YA-NEC-<bersyon>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<bersyon>-windows7-x64-portatile.zip` o `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<bersyon>-linux-x64.tar.gz` |
| Raspberry Pi OS 64-bit at iba pang aarch64 Linux | `YA-NEC-<bersyon>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32-bit, armv7l | `YA-NEC-<bersyon>-linux-armv7l.tar.gz` |
| Alpine at musl Linux | `YA-NEC-<bersyon>-alpine-x64.tar.gz` o `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<bersyon>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<bersyon>-mac-intel-app.zip` |

Katabi ng bawat zip ang checksum na `.sha256` para masuri ang na-download.

## Windows

I-extract ang zip sa folder na puwedeng sulatan, halimbawa `C:\YA-NEC` (hindi sa `C:\Program Files`, kung saan hindi maise-save ng programa ang mga setting), at patakbuhin ang `YA-NEC.exe`. Sa unang takbo, maaaring humingi ng kumpirmasyon ang Windows SmartScreen: "More info" > "Run anyway".

**Windows 7 SP1, 8 at 8.1**: gamitin ang mga `win7` package, binuo gamit ang .NET 6 (hindi tumatakbo ang .NET 8 sa Windows 7). Kailangan ng Windows 7 ang Service Pack 1 at ang mga update na KB2533623 at KB2999226.

## Linux at Raspberry Pi

```
tar -xzf YA-NEC-<bersyon>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Kailangan ang `libfontconfig1` at `libicu`, na mayroon na sa mga desktop distribution. Sa Alpine: `apk add icu-libs fontconfig libstdc++`. Gumagana ang kalkulasyon sa Raspberry Pi, pero ang malalaking model (libu-libong segment) ay nangangailangan ng oras at memorya.

## Mac

I-extract ang zip, halimbawa sa `~/YA-NEC`. Hindi pinirmahan ng Apple ang programa, kaya kailangang alisin nang isang beses ang quarantine mula sa Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Maliliit na screen

Sa mababang screen (halimbawa 1366 x 768) bumubukas ang window nang naka-maximize at lumilipat sa compact na anyo ang mga kahon ng resulta.
