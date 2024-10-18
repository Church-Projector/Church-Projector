#!/usr/bin/env bash

if [[ -z "$VERSION" ]]; then
    echo "Provide the version as environment variable VERSION"
    exit 1
fi

if [[ -z "$RUNTIME" ]]; then
    echo "Provide the runtime as environment variable RUNTIME"
    exit 1
fi

# Copy the application
mkdir -p BuildFolder/opt/churchprojector/
cp -r ./publish/$RUNTIME/* BuildFolder/opt/churchprojector/

# Create control file
mkdir -p BuildFolder/DEBIAN
echo "Package: churchprojector" > BuildFolder/DEBIAN/control
echo "Version: $VERSION" >> BuildFolder/DEBIAN/control
echo "Section: utils" >> BuildFolder/DEBIAN/control
echo "Priority: optional" >> BuildFolder/DEBIAN/control
if [[ "$RUNTIME" == "linux-x64" ]]; then
  echo "Architecture: amd64" >> BuildFolder/DEBIAN/control
else
  echo "Architecture: arm64" >> BuildFolder/DEBIAN/control
fi
echo "Maintainer: ChurchProjector <support@church-projector.de>" >> BuildFolder/DEBIAN/control
echo "Depends: libvlc-dev, libvlccore-dev" >> BuildFolder/DEBIAN/control
echo "Description: ChurchProjector - A presentation application" >> BuildFolder/DEBIAN/control

# Create the desktop shortcut
mkdir -p BuildFolder/usr/share/applications
echo "[Desktop Entry]" > BuildFolder/usr/share/applications/churchprojector.desktop
echo "Version=$VERSION" >> BuildFolder/usr/share/applications/churchprojector.desktop
echo "Name=ChurchProjector" >> BuildFolder/usr/share/applications/churchprojector.desktop
echo "Comment=ChurchProjector - A presentation application" >> BuildFolder/usr/share/applications/churchprojector.desktop
echo "Exec=/opt/churchprojector/ChurchProjector" >> BuildFolder/usr/share/applications/churchprojector.desktop
echo "Icon=churchprojector" >> BuildFolder/usr/share/applications/churchprojector.desktop
echo "Terminal=false" >> BuildFolder/usr/share/applications/churchprojector.desktop
echo "Type=Application" >> BuildFolder/usr/share/applications/churchprojector.desktop
echo "Categories=Utility;Presentation;" >> BuildFolder/usr/share/applications/churchprojector.desktop

# Copy the icons
mkdir -p BuildFolder/usr/share/icons/hicolor/64x64/apps
cp ./ChurchProjector/Assets/icon.png BuildFolder/usr/share/icons/hicolor/64x64/apps/churchprojector.png

# Build the application
dpkg-deb --build BuildFolder $RUNTIME.deb
rm -rf BuildFolder