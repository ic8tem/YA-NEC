#!/usr/bin/env bash
# YA-NEC - pacchetto Arch Linux / Manjaro / EndeavourOS / Arch Linux ARM (.pkg.tar.zst), ricavato dal .deb della stessa
# architettura (stessi file). Si costruisce con makepkg in un contenitore Docker archlinux (si impacchettano file gia' compilati,
# l'architettura del contenitore non conta). Si installa con: sudo pacman -U ya-nec-<versione>-1-<arch>.pkg.tar.zst
# (1.3.1, come installer/make-arch.sh di YA-MS)
# Uso: make-arch.sh <arch: x86_64|aarch64> <versione> <file .deb> <cartella di uscita>
set -euo pipefail
ARCH="$1"; VERSION="$2"; DEB="$3"; OUT="$4"
W="$(mktemp -d)"
mkdir -p "$W/albero"
dpkg-deb -x "$DEB" "$W/albero"
PV="${VERSION//-/_}"
cat > "$W/ya-nec.install" <<'EOF'
post_install() {
  mkdir -p /opt/YA-NEC/data && chmod -R a+rwX /opt/YA-NEC/data
  command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database -q /usr/share/applications || true
}
post_upgrade() { post_install; }
EOF
cat > "$W/PKGBUILD" <<EOF
pkgname=ya-nec
pkgver=${PV}
pkgrel=1
pkgdesc="YA-NEC - NEC-2 wire antenna simulator (Costantino Cerrotta IC8TEM)"
arch=('${ARCH}')
url="https://github.com/ic8tem/YA-NEC"
license=('GPL3')
depends=('fontconfig' 'libx11' 'libice' 'libsm')
install=ya-nec.install
options=('!strip' '!debug')
package() {
  cp -a "\$startdir/albero/." "\$pkgdir/"
}
EOF
# il contenitore archlinux esiste solo per x86_64: su un runner ARM gira emulato con qemu
PLAT=linux/amd64
if [ "$(uname -m)" != "x86_64" ]; then docker run --privileged --rm tonistiigi/binfmt --install amd64 >/dev/null; fi
docker run --rm --platform $PLAT -v "$W:/w" archlinux:latest bash -c "
  set -e
  pacman --disable-sandbox -Sy --noconfirm --needed base-devel >/dev/null
  sed -i 's/^CARCH=.*/CARCH=\"${ARCH}\"/' /etc/makepkg.conf
  useradd -m b && chown -R b /w
  su b -c 'cd /w && makepkg --nodeps --skipinteg -f'
  chmod -R a+rwX /w
"
mkdir -p "$OUT"
cp "$W"/ya-nec-*.pkg.tar.zst "$OUT/"
ls -la "$OUT"/ya-nec-*.pkg.tar.zst
