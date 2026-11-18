#!/usr/bin/env bash
# YA-NEC - pacchetto Slackware (.txz), ricavato dal .deb della stessa architettura: stessi file (/opt/YA-NEC, /usr/bin/ya-nec,
# voce di menu e icone). Si installa da root con: installpkg ya-nec-<versione>-<arch>-1_ic8tem.txz
# (1.3.1, come installer/make-txz.sh di YA-MS)
# Uso: make-txz.sh <arch: x86_64|aarch64> <versione> <file .deb> <cartella di uscita>
set -euo pipefail
ARCH="$1"; VERSION="$2"; DEB="$3"; OUT="$4"
T="$(mktemp -d)"
dpkg-deb -x "$DEB" "$T"
mkdir -p "$T/install"
cat > "$T/install/slack-desc" <<'EOF'
      |-----handy-ruler------------------------------------------------------|
ya-nec: ya-nec (simulatore d'antenne a fili basato sul NEC-2)
ya-nec:
ya-nec: YA-NEC - NEC-2 wire antenna simulator: Yagi, dipoli, verticali,
ya-nec: antenne riceventi, terreno reale, ottimizzazione, calcolatori.
ya-nec: Impostazioni e modelli in /opt/YA-NEC/data.
ya-nec: A cura di Costantino Cerrotta IC8TEM.
ya-nec:
ya-nec: https://github.com/ic8tem/YA-NEC
ya-nec:
ya-nec:
ya-nec:
EOF
cat > "$T/install/doinst.sh" <<'EOF'
mkdir -p opt/YA-NEC/data && chmod -R a+rwX opt/YA-NEC/data
if [ -x /usr/bin/update-desktop-database ]; then /usr/bin/update-desktop-database -q usr/share/applications >/dev/null 2>&1; fi
if [ -x /usr/bin/gtk-update-icon-cache ]; then /usr/bin/gtk-update-icon-cache -q usr/share/icons/hicolor >/dev/null 2>&1; fi
EOF
SV="${VERSION//-/_}"
mkdir -p "$OUT"
tar -C "$T" --owner=0 --group=0 -cJf "$OUT/ya-nec-${SV}-${ARCH}-1_ic8tem.txz" install opt usr
ls -la "$OUT/ya-nec-${SV}-${ARCH}-1_ic8tem.txz"
