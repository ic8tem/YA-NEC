# Uppsetning

YA-NEC er ekki sett upp: það er afþjappað og ræst. .NET þarf ekki, það er þegar í pakkanum. Stillingar, líkön og annálar haldast í möppu forritsins (`data`, `Registri`): ekkert AppData, engin skráning. Til að fjarlægja það nægir að eyða möppunni.

## Hvaða pakki

Af síðunni [Releases](https://github.com/ic8tem/YA-NEC/releases):

| Kerfi | Pakki |
|---|---|
| Windows 10/11 64 bita | `YA-NEC-<útgáfa>-win-x64.zip` |
| Windows 10/11 32 bita | `YA-NEC-<útgáfa>-win-x86.zip` |
| Windows á ARM (Snapdragon) | `YA-NEC-<útgáfa>-win-arm64.zip` |
| Windows 7 SP1, 8, 8.1 | `YA-NEC-<útgáfa>-win7-x64.zip` eða `-win7-x86.zip` |
| Linux x86_64 | `YA-NEC-<útgáfa>-linux-x64.zip` |
| Raspberry Pi OS 64 bita og önnur aarch64-Linux | `YA-NEC-<útgáfa>-linux-arm64.zip` |
| Raspberry Pi OS 32 bita, armv7l | `YA-NEC-<útgáfa>-linux-arm.zip` |
| Alpine og musl-Linux | `YA-NEC-<útgáfa>-linux-musl-x64.zip` eða `-linux-musl-arm64.zip` |
| Mac Apple Silicon (M1-M4) | `YA-NEC-<útgáfa>-osx-arm64.zip` |
| Mac Intel | `YA-NEC-<útgáfa>-osx-x64.zip` |

Við hlið hverrar zip-skrár er gátsumman `.sha256` til að sannreyna niðurhalið.

## Windows

Afþjappaðu zip-skrána í möppu með skrifaðgangi, t.d. `C:\YA-NEC` (ekki í `C:\Program Files`, þar sem forritið gæti ekki vistað stillingar sínar), og ræstu `YA-NEC.exe`. Við fyrstu ræsingu gæti Windows SmartScreen beðið um staðfestingu: „More info" > „Run anyway".

**Windows 7 SP1, 8 og 8.1**: notaðu `win7`-pakkana, smíðaða með .NET 6 (.NET 8 ræsist ekki í Windows 7). Windows 7 þarf Service Pack 1 og uppfærslurnar KB2533623 og KB2999226.

## Linux og Raspberry Pi

```
unzip YA-NEC-<útgáfa>-linux-x64.zip -d ~
cd ~/YA-NEC
chmod +x YA-NEC
./YA-NEC
```

`libfontconfig1` og `libicu` þurfa að vera til staðar; þau eru þegar í skjáborðsdreifingum. Í Alpine: `apk add icu-libs fontconfig libstdc++`. Á Raspberry Pi virka útreikningar, en stór líkön (þúsundir búta) þurfa tíma og minni.

## Mac

Afþjappaðu zip-skrána, t.d. í `~/YA-NEC`. Forritið er ekki undirritað af Apple, svo fjarlægja þarf sóttkvína einu sinni í Terminal:

```
xattr -dr com.apple.quarantine ~/YA-NEC
~/YA-NEC/YA-NEC
```

## Litlir skjáir

Á lágum skjám (t.d. 1366 x 768) opnast glugginn í hámarksstærð og niðurstöðureitirnir skipta yfir í þétt snið.
