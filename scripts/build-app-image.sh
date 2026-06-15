#!/bin/bash

NAME="zcli"

BUILD_DIR="build"
DOTNET_PUBLISH_DIR="$BUILD_DIR/app"
DOTNET_PROJECT_NAME="ZuneDeploy.CLI"

APPIMAGE_ROOT="$BUILD_DIR/AppDir"
APPIMAGE_BIN="$APPIMAGE_ROOT/usr/bin"
APPIMAGE_APPRUN="$APPIMAGE_ROOT/AppRun"
APPIMAGE_DESKTOP="$APPIMAGE_ROOT/$NAME.desktop"
APPIMAGE_ICON="$APPIMAGE_ROOT/$NAME.svg"

FINAL_OUTPUT="$BUILD_DIR/$NAME.AppImage"

set -x
set -e

# Clean
rm -rf $BUILD_DIR
rm -rf src/NativeLib/build
mkdir $BUILD_DIR

# Restore and Build
dotnet restore -r linux-x64
dotnet publish -c Release -r linux-x64 --self-contained true -o $DOTNET_PUBLISH_DIR -v:d src/$DOTNET_PROJECT_NAME

# Setup AppImage
mkdir -p $APPIMAGE_BIN
mv $DOTNET_PUBLISH_DIR/* $APPIMAGE_BIN
cp docs/.mtpz-data $APPIMAGE_ROOT

# Run File
cat >$APPIMAGE_APPRUN << 'EOF'
#!/bin/sh
HERE="$(dirname "$(readlink -f "${0}")")"
export PATH="${HERE}"/usr/bin/:"${PATH}"
EOF

cat >>$APPIMAGE_APPRUN << EOF
exec $DOTNET_PROJECT_NAME "\$@"
EOF

chmod 755 $APPIMAGE_APPRUN

# Desktop Entry
cat >$APPIMAGE_DESKTOP << EOF
[Desktop Entry]
Type=Application
Name=$NAME
Comment=Deploy XNA and OpenZDK Applications to your Zune
Icon=$NAME
Exec=$DOTNET_PROJECT_NAME
Path=~
Terminal=true
Categories=Development;
EOF

# Copy Logo
cp docs/logo.svg $APPIMAGE_ICON

# Get AppImage Tool
APPIMG_TOOL="$BUILD_DIR/build.AppImage"
wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage" -O $APPIMG_TOOL
chmod a+x $APPIMG_TOOL

# Build App Image
$APPIMG_TOOL --appimage-extract
mv squashfs-root $BUILD_DIR
$BUILD_DIR/squashfs-root/AppRun $APPIMAGE_ROOT $FINAL_OUTPUT
