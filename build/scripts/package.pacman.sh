#!/usr/bin/env bash

set -euo pipefail

if [[ -z "${VERSION:-}" ]]; then
    echo "Provide the version as environment variable VERSION"
    exit 1
fi

if [[ -z "${RUNTIME:-}" ]]; then
    echo "Provide the runtime as environment variable RUNTIME"
    exit 1
fi

if [[ "$RUNTIME" == "linux-x64" ]]; then
    PACKAGE_ARCH="x86_64"
else
    PACKAGE_ARCH="aarch64"
fi

PACKAGE_VERSION="${VERSION#v}"
PACKAGE_NAME="churchprojector-${PACKAGE_VERSION}-1-${PACKAGE_ARCH}.pkg.tar.zst"
BUILD_ROOT="$(pwd)/BuildFolder"
PACKAGE_ROOT="$BUILD_ROOT/pkgroot"

mkdir -p "$PACKAGE_ROOT/opt/churchprojector"
cp -r "./publish/$RUNTIME/"* "$PACKAGE_ROOT/opt/churchprojector/"

mkdir -p "$PACKAGE_ROOT/usr/share/applications"
cat <<EOF > "$PACKAGE_ROOT/usr/share/applications/churchprojector.desktop"
[Desktop Entry]
Version=$PACKAGE_VERSION
Name=ChurchProjector
Comment=ChurchProjector - A presentation application
Exec=/opt/churchprojector/ChurchProjector
Icon=churchprojector
Terminal=false
Type=Application
Categories=Utility;Presentation;
EOF

mkdir -p "$PACKAGE_ROOT/usr/share/icons/hicolor/64x64/apps"
cp "./ChurchProjector/ChurchProjector/Assets/icon.png" "$PACKAGE_ROOT/usr/share/icons/hicolor/64x64/apps/churchprojector.png"

cat <<EOF > "$PACKAGE_ROOT/.PKGINFO"
pkgname = churchprojector
pkgbase = churchprojector
pkgver = $PACKAGE_VERSION-1
pkgdesc = ChurchProjector - A presentation application
url = https://github.com/Church-Projector/Church-Projector
builddate = $(date +%s)
packager = ChurchProjector <support@church-projector.de>
size = $(du -sb "$PACKAGE_ROOT" | awk '{print $1}')
arch = $PACKAGE_ARCH
license = MIT
depend = vlc
EOF

(
    cd "$PACKAGE_ROOT"
    tar --sort=name \
        --mtime='UTC 1970-01-01' \
        --owner=0 \
        --group=0 \
        --numeric-owner \
        -I 'zstd -19 -T0' \
        -cf "../../$PACKAGE_NAME" \
        .
)

rm -rf "$BUILD_ROOT"
