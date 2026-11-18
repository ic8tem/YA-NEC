#!/bin/sh
# YA-NEC - installazione per Linux dal tar.gz nella cartella personale ~/YA-NEC (programma e cartella data insieme),
# con la voce nel menu delle applicazioni. Il tar.gz si puo' anche usare senza installare: ./YA-NEC dalla cartella estratta.
# (1.3.1, come installer/linux-install.sh di YA-MS)
set -e
SRC="$(cd "$(dirname "$0")" && pwd)"
DEST="$HOME/YA-NEC"
echo "Installo YA-NEC in $DEST"
mkdir -p "$DEST"
# copia tutto; i file creati dal programma (data/config.ini, modelli, registri) restano
cp -R "$SRC/." "$DEST/"
rm -f "$DEST/installa.sh"
chmod +x "$DEST/YA-NEC"
mkdir -p "$HOME/.local/share/applications" "$HOME/.local/share/icons/hicolor/256x256/apps"
cp "$DEST/ya-nec.png" "$HOME/.local/share/icons/hicolor/256x256/apps/ya-nec.png"
cat > "$HOME/.local/share/applications/ya-nec.desktop" <<DESK
[Desktop Entry]
Type=Application
Name=YA-NEC
Comment=Wire antenna simulator based on NEC-2 (IC8TEM)
Comment[it]=Simulatore d'antenne a fili basato sul NEC-2 (IC8TEM)
Exec="$DEST/YA-NEC"
Path=$DEST
Icon=ya-nec
Terminal=false
Categories=HamRadio;Science;Engineering;
DESK
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$HOME/.local/share/applications" || true
echo "Fatto. Avvia YA-NEC dal menu delle applicazioni oppure con: $DEST/YA-NEC"
