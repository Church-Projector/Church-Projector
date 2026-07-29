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

RPM_VERSION="${VERSION#v}"
RPM_BUILD_ROOT="$(pwd)/BuildFolder"
RPM_TOPDIR="$(pwd)/rpmbuild"

mkdir -p "$RPM_BUILD_ROOT/opt/churchprojector"
cp -r "./publish/$RUNTIME/"* "$RPM_BUILD_ROOT/opt/churchprojector/"

mkdir -p "$RPM_BUILD_ROOT/usr/share/applications"
cat <<EOF > "$RPM_BUILD_ROOT/usr/share/applications/churchprojector.desktop"
[Desktop Entry]
Version=$RPM_VERSION
Name=ChurchProjector
Comment=ChurchProjector - A presentation application
Exec=/opt/churchprojector/ChurchProjector
Icon=churchprojector
Terminal=false
Type=Application
Categories=Utility;Presentation;
EOF

mkdir -p "$RPM_BUILD_ROOT/usr/share/icons/hicolor/64x64/apps"
cp "./ChurchProjector/ChurchProjector/Assets/icon.png" "$RPM_BUILD_ROOT/usr/share/icons/hicolor/64x64/apps/churchprojector.png"

mkdir -p "$RPM_TOPDIR/SPECS" "$RPM_TOPDIR/SRPMS" "$RPM_TOPDIR/RPMS" "$RPM_TOPDIR/BUILD" "$RPM_TOPDIR/SOURCES"

cat <<EOF > "$RPM_TOPDIR/SPECS/churchprojector.spec"
Name: churchprojector
Version: $RPM_VERSION
Release: 1%{?dist}
Summary: ChurchProjector - A presentation application
License: MIT
URL: https://github.com/Church-Projector/Church-Projector
BuildArch: $PACKAGE_ARCH
Requires: vlc-devel

%description
ChurchProjector - A presentation application

%install
mkdir -p %{buildroot}
cp -a $RPM_BUILD_ROOT/. %{buildroot}/

%files
/opt/churchprojector
/usr/share/applications/churchprojector.desktop
/usr/share/icons/hicolor/64x64/apps/churchprojector.png
EOF

rpmbuild \
    --define "_topdir $RPM_TOPDIR" \
    --define "_build_id_links none" \
    -bb "$RPM_TOPDIR/SPECS/churchprojector.spec"

cp "$RPM_TOPDIR/RPMS/$PACKAGE_ARCH/"*.rpm .
rm -rf "$RPM_BUILD_ROOT" "$RPM_TOPDIR"
