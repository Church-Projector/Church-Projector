#!/usr/bin/env bash

set -euo pipefail

if [[ -z "${VERSION:-}" || -z "${RUNTIME:-}" ]]; then
    echo "Provide VERSION and RUNTIME as environment variables"
    exit 1
fi

case "$RUNTIME" in
    linux-x64)
        SNAP_ARCH="amd64"
        ;;
    linux-arm64)
        SNAP_ARCH="arm64"
        ;;
    *)
        echo "Unsupported Linux runtime: $RUNTIME"
        exit 1
        ;;
esac

SNAP_ROOT="$(mktemp -d)"
trap 'rm -rf "$SNAP_ROOT"' EXIT

# snap pack requires the package root to be traversable by others.
chmod 755 "$SNAP_ROOT"

mkdir -p "$SNAP_ROOT/bin" "$SNAP_ROOT/meta/gui"
cp -r "./publish/$RUNTIME/." "$SNAP_ROOT/bin/"
cp "./ChurchProjector/ChurchProjector/Assets/icon.png" "$SNAP_ROOT/meta/gui/icon.png"
sed \
    -e 's|^Exec=.*|Exec=churchprojector|' \
    -e 's|^Icon=.*|Icon=${SNAP}/meta/gui/icon.png|' \
    "./build/linux/churchprojector.desktop" \
    > "$SNAP_ROOT/meta/gui/churchprojector.desktop"

{
    echo "name: churchprojector"
    echo "version: '$VERSION'"
    echo "summary: ChurchProjector"
    echo "description: A presentation application for churches"
    echo "base: core24"
    echo "grade: stable"
    echo "confinement: classic"
    echo "architectures:"
    echo "  - $SNAP_ARCH"
    echo "apps:"
    echo "  churchprojector:"
    echo "    command: bin/ChurchProjector"
    echo "    desktop: meta/gui/churchprojector.desktop"
} > "$SNAP_ROOT/meta/snap.yaml"

snap pack "$SNAP_ROOT" "churchprojector-${VERSION}-${RUNTIME}.snap"
