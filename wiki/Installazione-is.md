# Uppsetning

YA-NEC er ekki sett upp: það er afþjappað og ræst. .NET þarf ekki, það er þegar í pakkanum. Stillingar, líkön og annálar haldast í möppu forritsins (`data`, `Registri`): ekkert AppData, engin skráning. Til að fjarlægja það nægir að eyða möppunni.

## Hvaða pakki

Af síðunni [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Kerfi | Pakki |
|---|---|
| Windows 10/11 64 bita | `YA-NEC-<útgáfa>-windows-x64-portatile.zip` |
| Windows 10/11 32 bita | `YA-NEC-<útgáfa>-windows-x86-portatile.zip` |
| Windows á ARM (Snapdragon) | `YA-NEC-<útgáfa>-windows-arm64-portatile.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<útgáfa>-windows7-x64-portatile.zip` eða `-windows7-x86-portatile.zip` |
| Linux x86_64 | `YA-NEC-<útgáfa>-linux-x64.tar.gz` |
| Raspberry Pi OS 64 bita og önnur aarch64-Linux | `YA-NEC-<útgáfa>-linux-arm64.tar.gz` |
| Raspberry Pi OS 32 bita, armv7l | `YA-NEC-<útgáfa>-linux-armv7l.tar.gz` |
| Alpine og musl-Linux | `YA-NEC-<útgáfa>-alpine-x64.tar.gz` eða `-alpine-arm64.tar.gz` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<útgáfa>-mac-apple-silicon-app.zip` |
| Mac Intel | `YA-NEC-<útgáfa>-mac-intel-app.zip` |

Við hlið hverrar zip-skrár er gátsumman `.sha256` til að sannreyna niðurhalið.

## Windows

Afþjappaðu zip-skrána í möppu með skrifaðgangi, t.d. `C:\YA-NEC` (ekki í `C:\Program Files`, þar sem forritið gæti ekki vistað stillingar sínar), og ræstu `YA-NEC.exe`. Við fyrstu ræsingu gæti Windows SmartScreen beðið um staðfestingu: „More info" > „Run anyway".

**Windows 7 SP1, 8 og 8.1**: notaðu `win7`-pakkana, smíðaða með .NET 6 (.NET 8 ræsist ekki í Windows 7). Windows 7 þarf Service Pack 1 og uppfærslurnar KB2533623 og KB2999226.

## Linux og Raspberry Pi

```
tar -xzf YA-NEC-<útgáfa>-linux-x64.tar.gz -C ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` og `libicu` þurfa að vera til staðar; þau eru þegar í skjáborðsdreifingum. Í Alpine: `apk add icu-libs fontconfig libstdc++`. Á Raspberry Pi virka útreikningar, en stór líkön (þúsundir búta) þurfa tíma og minni.

## Mac

Afþjappaðu zip-skrána, t.d. í `~/YA-NEC`. Forritið er ekki undirritað af Apple, svo fjarlægja þarf sóttkvína einu sinni í Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
open ~/YA-NEC/YA-NEC.app
```

## Litlir skjáir

Á lágum skjám (t.d. 1366 x 768) opnast glugginn í hámarksstærð og niðurstöðureitirnir skipta yfir í þétt snið.
