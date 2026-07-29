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
    PACKAGE_ARCH="amd64"
else
    PACKAGE_ARCH="arm64"
fi

PACKAGE_NAME="churchprojector_${VERSION}_${PACKAGE_ARCH}.deb"
BUILD_ROOT="BuildFolder"

# Copy the application
mkdir -p "$BUILD_ROOT/opt/churchprojector/"
cp -r "./publish/$RUNTIME/"* "$BUILD_ROOT/opt/churchprojector/"

# Create control file
mkdir -p "$BUILD_ROOT/DEBIAN"
echo "Package: churchprojector" > "$BUILD_ROOT/DEBIAN/control"
echo "Version: $VERSION" >> "$BUILD_ROOT/DEBIAN/control"
echo "Section: utils" >> "$BUILD_ROOT/DEBIAN/control"
echo "Priority: optional" >> "$BUILD_ROOT/DEBIAN/control"
echo "Architecture: $PACKAGE_ARCH" >> "$BUILD_ROOT/DEBIAN/control"
echo "Maintainer: ChurchProjector <support@church-projector.de>" >> "$BUILD_ROOT/DEBIAN/control"
echo "Depends: libvlc-dev, libvlccore-dev" >> "$BUILD_ROOT/DEBIAN/control"
echo "Description: ChurchProjector - A presentation application" >> "$BUILD_ROOT/DEBIAN/control"

# Create the desktop shortcut
mkdir -p "$BUILD_ROOT/usr/share/applications"
echo "[Desktop Entry]" > "$BUILD_ROOT/usr/share/applications/churchprojector.desktop"
echo "Version=$VERSION" >> "$BUILD_ROOT/usr/share/applications/churchprojector.desktop"
echo "Name=ChurchProjector" >> "$BUILD_ROOT/usr/share/applications/churchprojector.desktop"
echo "Comment=ChurchProjector - A presentation application" >> "$BUILD_ROOT/usr/share/applications/churchprojector.desktop"
echo "Exec=/opt/churchprojector/ChurchProjector" >> "$BUILD_ROOT/usr/share/applications/churchprojector.desktop"
echo "Icon=churchprojector" >> "$BUILD_ROOT/usr/share/applications/churchprojector.desktop"
echo "Terminal=false" >> "$BUILD_ROOT/usr/share/applications/churchprojector.desktop"
echo "Type=Application" >> "$BUILD_ROOT/usr/share/applications/churchprojector.desktop"
echo "Categories=Utility;Presentation;" >> "$BUILD_ROOT/usr/share/applications/churchprojector.desktop"

# Copy the icons
mkdir -p "$BUILD_ROOT/usr/share/icons/hicolor/64x64/apps"
cp "./ChurchProjector/ChurchProjector/Assets/icon.png" "$BUILD_ROOT/usr/share/icons/hicolor/64x64/apps/churchprojector.png"

# Build the application
dpkg-deb --build "$BUILD_ROOT" "$PACKAGE_NAME"
rm -rf "$BUILD_ROOT"
