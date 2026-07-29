#!/usr/bin/env bash

set -euo pipefail

if [[ -z "${VERSION:-}" || -z "${RUNTIME:-}" ]]; then
    echo "Provide VERSION and RUNTIME as environment variables"
    exit 1
fi

case "$RUNTIME" in
    linux-x64)
        APPIMAGE_ARCH="x86_64"
        ;;
    linux-arm64)
        APPIMAGE_ARCH="aarch64"
        ;;
    *)
        echo "Unsupported Linux runtime: $RUNTIME"
        exit 1
        ;;
esac

APPDIR="$(mktemp -d --suffix=.AppDir)"
TOOL_DIR="$(mktemp -d)"
trap 'rm -rf "$APPDIR" "$TOOL_DIR"' EXIT

mkdir -p \
    "$APPDIR/usr/bin" \
    "$APPDIR/usr/share/applications" \
    "$APPDIR/usr/share/icons/hicolor/16x16/apps"
cp -r "./publish/$RUNTIME/." "$APPDIR/usr/bin/"
cp "./build/linux/AppRun" "$APPDIR/AppRun"
cp "./build/linux/churchprojector.desktop" "$APPDIR/churchprojector.desktop"
cp "./ChurchProjector/ChurchProjector/Assets/icon.png" "$APPDIR/churchprojector.png"
cp "./ChurchProjector/ChurchProjector/Assets/icon.png" \
    "$APPDIR/usr/share/icons/hicolor/16x16/apps/churchprojector.png"
chmod +x "$APPDIR/AppRun" "$APPDIR/usr/bin/ChurchProjector"

APPIMAGETOOL="$TOOL_DIR/appimagetool.AppImage"
curl --fail --location --retry 3 \
    "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-${APPIMAGE_ARCH}.AppImage" \
    --output "$APPIMAGETOOL"
chmod +x "$APPIMAGETOOL"

ARCH="$APPIMAGE_ARCH" "$APPIMAGETOOL" --appimage-extract-and-run \
    "$APPDIR" "churchprojector-${VERSION}-${RUNTIME}.AppImage"
