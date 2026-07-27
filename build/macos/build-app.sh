#!/usr/bin/env bash
set -e

RUNTIME="$1"
VERSION="$2"

APP_NAME="ChurchProjector"
PUBLISH_DIR="./publish/${RUNTIME}"
APP_DIR="./${APP_NAME}.app"

echo "Creating macOS app bundle for ${RUNTIME}"

# cleanup
rm -rf "${APP_DIR}"
mkdir -p "${APP_DIR}/Contents/MacOS"
mkdir -p "${APP_DIR}/Contents/Resources"

# copy published files
cp -R "${PUBLISH_DIR}/." "${APP_DIR}/Contents/MacOS/"

# copy plist
cp ./build/macos/Info.plist "${APP_DIR}/Contents/Info.plist"

# copy icon
cp ./ChurchProjector/ChurchProjector/Assets/icon.icns "${APP_DIR}/Contents/Resources/App.icns"

# remove debug symbols
find "${APP_DIR}" -name "*.dSYM" -type d -exec rm -rf {} +

# make executable
chmod +x "${APP_DIR}/Contents/MacOS/${APP_NAME}"

# create zip
zip -r "${RUNTIME}.zip" "${APP_NAME}.app"

echo "Done: ${RUNTIME}.zip"