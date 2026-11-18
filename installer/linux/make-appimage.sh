#!/usr/bin/env bash
# YA-NEC - AppImage (un solo file, gira su quasi tutte le distribuzioni Linux con glibc).
# L'AppImage e' in sola lettura: al primo avvio (e a ogni nuova versione) copia il programma in ~/YA-NEC e lo avvia da li',
# cosi' la cartella data resta accanto al programma come negli altri pacchetti. La copia si aggiorna scaricando il nuovo AppImage
# (file installato-da-pacchetto: niente aggiornamento automatico). (1.3.1, come installer/make-appimage.sh di YA-MS)
# Uso: make-appimage.sh <arch: x86_64|aarch64> <versione> <cartella YA-NEC estratta> <cartella di uscita>
set -euo pipefail
ARCH="$1"; VERSION="$2"; SRC="$3"; OUT="$4"
QUI="$(cd "$(dirname "$0")" && pwd)"; RADICE="$(cd "$QUI/../.." && pwd)"
A="$(mktemp -d)/YA-NEC.AppDir"
mkdir -p "$A/opt/YA-NEC" "$A/usr/share/icons/hicolor/256x256/apps"
cp -R "$SRC/." "$A/opt/YA-NEC/"
chmod 755 "$A/opt/YA-NEC/YA-NEC"
echo "$VERSION" > "$A/opt/YA-NEC/VERSIONE_PACCHETTO"
touch "$A/opt/YA-NEC/installato-da-pacchetto"
cp "$RADICE/assets/ya-nec-256.png" "$A/ya-nec.png"
cp "$RADICE/assets/ya-nec-256.png" "$A/usr/share/icons/hicolor/256x256/apps/ya-nec.png"
cat > "$A/ya-nec.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Name=YA-NEC
Comment=Wire antenna simulator based on NEC-2 (IC8TEM)
Exec=AppRun
Icon=ya-nec
Terminal=false
Categories=HamRadio;Science;Engineering;
EOF
cat > "$A/AppRun" <<'EOF'
#!/bin/sh
# YA-NEC: copia il programma in ~/YA-NEC (solo la prima volta o con una versione nuova) e lo avvia.
HERE="$(dirname "$(readlink -f "$0")")"
DEST="$HOME/YA-NEC"
NEW="$(cat "$HERE/opt/YA-NEC/VERSIONE_PACCHETTO" 2>/dev/null)"
OLD="$(cat "$DEST/VERSIONE_PACCHETTO" 2>/dev/null)"
if [ ! -x "$DEST/YA-NEC" ] || [ "$NEW" != "$OLD" ]; then
  mkdir -p "$DEST"
  cp -R "$HERE/opt/YA-NEC/." "$DEST/"
  chmod +x "$DEST/YA-NEC"
fi
cd "$DEST" && exec "$DEST/YA-NEC" "$@"
EOF
chmod 755 "$A/AppRun"
T="$(mktemp -d)"
curl -fsSL -o "$T/appimagetool" "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-$(uname -m).AppImage"
chmod +x "$T/appimagetool"
mkdir -p "$OUT"
ARCH=$ARCH "$T/appimagetool" --appimage-extract-and-run "$A" "$OUT/YA-NEC-${VERSION}-${ARCH}.AppImage"
ls -la "$OUT/YA-NEC-${VERSION}-${ARCH}.AppImage"
