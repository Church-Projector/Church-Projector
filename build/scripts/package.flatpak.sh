#!/usr/bin/env bash

set -euo pipefail

if [[ -z "${VERSION:-}" || -z "${RUNTIME:-}" ]]; then
    echo "Provide VERSION and RUNTIME as environment variables"
    exit 1
fi

case "$RUNTIME" in
    linux-x64|linux-arm64)
        ;;
    *)
        echo "Unsupported Linux runtime: $RUNTIME"
        exit 1
        ;;
esac

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

mkdir -p "$WORK_DIR/flatpak-source"
cp -r "./publish/$RUNTIME/." "$WORK_DIR/flatpak-source/"
cp "./build/linux/churchprojector.desktop" \
    "$WORK_DIR/flatpak-source/de.churchprojector.ChurchProjector.desktop"
sed -i \
    -e 's|^Icon=.*|Icon=de.churchprojector.ChurchProjector|' \
    "$WORK_DIR/flatpak-source/de.churchprojector.ChurchProjector.desktop"
cp "./ChurchProjector/ChurchProjector/Assets/icon.png" \
    "$WORK_DIR/flatpak-source/de.churchprojector.ChurchProjector.png"
cp "./build/linux/de.churchprojector.ChurchProjector.yml" "$WORK_DIR/"

flatpak-builder \
    --force-clean \
    --user \
    --install-deps-from=flathub \
    --repo="$WORK_DIR/repo" \
    "$WORK_DIR/build" \
    "$WORK_DIR/de.churchprojector.ChurchProjector.yml"

flatpak build-bundle \
    "$WORK_DIR/repo" \
    "churchprojector-${VERSION}-${RUNTIME}.flatpak" \
    de.churchprojector.ChurchProjector \
    --runtime-repo=https://dl.flathub.org/repo/flathub.flatpakrepo
