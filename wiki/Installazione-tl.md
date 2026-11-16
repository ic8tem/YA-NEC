# Pag-install

Hindi kailangang i-install ang YA-NEC: i-extract at patakbuhin lang. Hindi kailangan ang .NET, kasama na ito sa package. Ang mga setting, model at log ay nananatili sa folder ng programa (`data`, `Registri`): walang AppData, walang registry. Para alisin ito, burahin lang ang folder.

## Aling package

Mula sa pahinang [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Sistema | Package |
|---|---|
| Windows 10/11 64-bit | `YA-NEC-<bersyon>-win-x64.zip` |
| Windows 10/11 32-bit | `YA-NEC-<bersyon>-win-x86.zip` |
| Windows sa ARM (Snapdragon) | `YA-NEC-<bersyon>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<bersyon>-win7-x64.zip` o `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<bersyon>-linux-x64.zip` |
| Raspberry Pi OS 64-bit at iba pang aarch64 Linux | `YA-NEC-<bersyon>-linux-arm64.zip` |
| Raspberry Pi OS 32-bit, armv7l | `YA-NEC-<bersyon>-linux-arm.zip` |
| Alpine at musl Linux | `YA-NEC-<bersyon>-linux-musl-x64.zip` o `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<bersyon>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<bersyon>-osx-x64.zip` |

Katabi ng bawat zip ang checksum na `.sha256` para masuri ang na-download.

## Windows

I-extract ang zip sa folder na puwedeng sulatan, halimbawa `C:\YA-NEC` (hindi sa `C:\Program Files`, kung saan hindi maise-save ng programa ang mga setting), at patakbuhin ang `YA-NEC.exe`. Sa unang takbo, maaaring humingi ng kumpirmasyon ang Windows SmartScreen: "More info" > "Run anyway".

**Windows 7 SP1, 8 at 8.1**: gamitin ang mga `win7` package, binuo gamit ang .NET 6 (hindi tumatakbo ang .NET 8 sa Windows 7). Kailangan ng Windows 7 ang Service Pack 1 at ang mga update na KB2533623 at KB2999226.

## Linux at Raspberry Pi

```
unzip YA-NEC-<bersyon>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

Kailangan ang `libfontconfig1` at `libicu`, na mayroon na sa mga desktop distribution. Sa Alpine: `apk add icu-libs fontconfig libstdc++`. Gumagana ang kalkulasyon sa Raspberry Pi, pero ang malalaking model (libu-libong segment) ay nangangailangan ng oras at memorya.

## Mac

I-extract ang zip, halimbawa sa `~/YA-NEC`. Hindi pinirmahan ng Apple ang programa, kaya kailangang alisin nang isang beses ang quarantine mula sa Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Maliliit na screen

Sa mababang screen (halimbawa 1366 x 768) bumubukas ang window nang naka-maximize at lumilipat sa compact na anyo ang mga kahon ng resulta.
