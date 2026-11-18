#!/bin/bash
# YA-NEC - pacchetti .deb e .rpm da un pacchetto zip gia' compilato (chiamato da .github/workflows/build.yml).
# Uso: pacchetti.sh <cartella YA-NEC estratta> <versione> <arch deb: amd64|arm64|armhf> <arch rpm: x86_64|aarch64|armv7hl> <cartella di uscita>
# Installazione in /opt/YA-NEC; data/ resta scrivibile per tutti (impostazioni e modelli accanto al programma, niente cartelle
# nascoste nella home: scelta di IC8TEM). Il file installato-da-pacchetto spegne l'aggiornamento automatico (si aggiorna con apt/dnf).
set -e
SRC="$1"; VER="$2"; DEB="$3"; RPM="$4"; OUT="$5"
QUI="$(cd "$(dirname "$0")" && pwd)"; RADICE="$(cd "$QUI/../.." && pwd)"
R=$(mktemp -d); chmod 755 "$R"
mkdir -p "$R/opt" "$R/usr/bin" "$R/usr/share/applications" "$R/usr/share/icons/hicolor/256x256/apps" "$R/usr/share/icons/hicolor/512x512/apps"
cp -a "$SRC" "$R/opt/YA-NEC"
touch "$R/opt/YA-NEC/installato-da-pacchetto"
ln -s /opt/YA-NEC/YA-NEC "$R/usr/bin/ya-nec"
cp "$QUI/ya-nec.desktop" "$R/usr/share/applications/"
cp "$RADICE/assets/ya-nec-256.png" "$R/usr/share/icons/hicolor/256x256/apps/ya-nec.png"
cp "$RADICE/assets/ya-nec-512.png" "$R/usr/share/icons/hicolor/512x512/apps/ya-nec.png"
POST='#!/bin/sh
mkdir -p /opt/YA-NEC/data && chmod -R a+rwX /opt/YA-NEC/data
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database -q /usr/share/applications || true
exit 0'
# --- .deb
D=$(mktemp -d); chmod 755 "$D"; cp -a "$R/." "$D/"; mkdir -p "$D/DEBIAN"
KB=$(du -sk "$D" | cut -f1)
cat > "$D/DEBIAN/control" <<EOC
Package: ya-nec
Version: $VER
Section: hamradio
Priority: optional
Architecture: $DEB
Installed-Size: $KB
Depends: libc6, libfontconfig1, libice6, libsm6, libx11-6
Maintainer: Costantino Cerrotta IC8TEM <noreply@github.com>
Homepage: https://github.com/ic8tem/YA-NEC
Description: YA-NEC - NEC-2 wire antenna simulator
 Simulatore d'antenne a fili basato sul NEC-2, a cura di Costantino IC8TEM.
 Self-contained: .NET is included.
EOC
printf '%s\n' "$POST" > "$D/DEBIAN/postinst"; chmod 755 "$D/DEBIAN/postinst"
mkdir -p "$OUT"
dpkg-deb --root-owner-group -Zxz --build "$D" "$OUT/ya-nec_${VER}_${DEB}.deb"
# --- .rpm
T=$(mktemp -d); mkdir -p "$T"/{BUILD,RPMS,SOURCES,SPECS,SRPMS,BUILDROOT}
cat > "$T/SPECS/ya-nec.spec" <<EOS
Name: ya-nec
Version: $VER
Release: 1
Summary: YA-NEC - NEC-2 wire antenna simulator
License: GPL-3.0
URL: https://github.com/ic8tem/YA-NEC
AutoReqProv: no
Requires: fontconfig, libICE, libSM, libX11
%description
Simulatore d'antenne a fili basato sul NEC-2, a cura di Costantino IC8TEM. Self-contained: .NET is included.
%global __os_install_post %{nil}
%global debug_package %{nil}
%install
cp -a $R/. %{buildroot}/
%post
$(printf '%s\n' "$POST" | sed 1d)
%files
/opt/YA-NEC
/usr/bin/ya-nec
/usr/share/applications/ya-nec.desktop
/usr/share/icons/hicolor/256x256/apps/ya-nec.png
/usr/share/icons/hicolor/512x512/apps/ya-nec.png
EOS
rpmbuild --define "_topdir $T" --target "$RPM" -bb "$T/SPECS/ya-nec.spec" >/dev/null
# 1.3.1: nome come in YA-MS (ya-nec-<versione>.<arch>.rpm, senza il "-1" del rilascio)
cp "$T"/RPMS/*/*.rpm "$OUT/ya-nec-${VER}.${RPM}.rpm"
ls -l "$OUT"
