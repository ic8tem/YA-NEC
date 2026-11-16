#!/bin/bash
# YA-NEC - immagine .dmg per macOS da un pacchetto zip gia' compilato (chiamato da .github/workflows/build.yml, su macOS).
# Uso: dmg.sh <cartella YA-NEC estratta> <versione> <nome: osx-arm64|osx-x64> <cartella di uscita>
# Dentro il .dmg c'e' la cartella "YA-NEC" (da trascinare in Applicazioni) con YA-NEC.app e, accanto, data/ e i documenti:
# impostazioni e modelli restano accanto al programma come negli zip (niente Library/Application Support, scelta di IC8TEM);
# il programma trova data/ accanto a YA-NEC.app (src/UI/AppPaths.cs). Firma ad hoc (nessun certificato Apple):
# la prima volta si apre con tasto destro > Apri.
set -e
SRC="$1"; VER="$2"; NOME="$3"; OUT="$4"
QUI="$(cd "$(dirname "$0")" && pwd)"; RADICE="$(cd "$QUI/../.." && pwd)"
W=$(mktemp -d); C="$W/YA-NEC"; A="$C/YA-NEC.app/Contents"
mkdir -p "$A/MacOS" "$A/Resources"
# eseguibile e librerie dentro il pacchetto .app, dati e documenti accanto
cp -a "$SRC/YA-NEC" "$A/MacOS/"; cp -a "$SRC/lib" "$A/MacOS/" 2>/dev/null || true
for f in "$SRC"/*; do b=$(basename "$f"); case "$b" in YA-NEC|lib) ;; *) cp -a "$f" "$C/";; esac; done
touch "$C/installato-da-pacchetto"
# icona .icns dal PNG 1024
I="$W/ya-nec.iconset"; mkdir -p "$I"
for s in 16 32 128 256 512; do
  sips -z $s $s "$RADICE/assets/ya-nec-1024.png" --out "$I/icon_${s}x${s}.png" >/dev/null
  d=$((s*2)); sips -z $d $d "$RADICE/assets/ya-nec-1024.png" --out "$I/icon_${s}x${s}@2x.png" >/dev/null
done
iconutil -c icns "$I" -o "$A/Resources/ya-nec.icns"
cat > "$A/Info.plist" <<EOP
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleName</key><string>YA-NEC</string>
<key>CFBundleDisplayName</key><string>YA-NEC</string>
<key>CFBundleIdentifier</key><string>com.github.ic8tem.ya-nec</string>
<key>CFBundleVersion</key><string>$VER</string>
<key>CFBundleShortVersionString</key><string>$VER</string>
<key>CFBundleExecutable</key><string>YA-NEC</string>
<key>CFBundleIconFile</key><string>ya-nec</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>NSHighResolutionCapable</key><true/>
<key>LSMinimumSystemVersion</key><string>10.15</string>
</dict></plist>
EOP
chmod +x "$A/MacOS/YA-NEC"
codesign --force --deep -s - "$C/YA-NEC.app"
ln -s /Applications "$W/Applicazioni - Applications"
cat > "$W/LEGGIMI - README.txt" <<EOT
YA-NEC $VER

IT: trascina la cartella YA-NEC in Applicazioni. Al primo avvio: tasto destro su YA-NEC.app > Apri > Apri
(il programma non e' firmato da Apple). Impostazioni e modelli restano nella cartella YA-NEC/data.

EN: drag the YA-NEC folder to Applications. First launch: right-click YA-NEC.app > Open > Open
(the app is not signed by Apple). Settings and models stay in the YA-NEC/data folder.
EOT
mkdir -p "$OUT"
hdiutil create -volname "YA-NEC $VER" -srcfolder "$W" -ov -format UDZO "$OUT/YA-NEC-$VER-$NOME.dmg"
ls -l "$OUT"
