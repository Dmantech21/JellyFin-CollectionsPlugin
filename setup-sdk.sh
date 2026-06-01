#!/usr/bin/env bash
# Downloads Jellyfin server DLLs needed to compile the plugin locally.
# MediaBrowser.Common and MediaBrowser.Controller are not on NuGet.org —
# they ship with the Jellyfin server binary.
#
# Usage:
#   ./setup-sdk.sh          # uses default version (10.9.0)
#   ./setup-sdk.sh 10.9.11  # specify a version

set -euo pipefail

JF_VERSION="${1:-10.9.0}"
DEST="$(cd "$(dirname "$0")" && pwd)/.jellyfin-sdk"
TMP_TAR="/tmp/jellyfin-${JF_VERSION}.tar.gz"
TMP_EXT="/tmp/jellyfin-extract-${JF_VERSION}"

echo "Downloading Jellyfin ${JF_VERSION} release..."
wget -q --show-progress \
  "https://github.com/jellyfin/jellyfin/releases/download/v${JF_VERSION}/jellyfin_${JF_VERSION}_linux-amd64.tar.gz" \
  -O "$TMP_TAR"

rm -rf "$TMP_EXT"
mkdir -p "$TMP_EXT" "$DEST"
tar -xzf "$TMP_TAR" -C "$TMP_EXT/"

DLL_DIR=$(find "$TMP_EXT" -name "MediaBrowser.Common.dll" -exec dirname {} \; | head -1)
if [ -z "$DLL_DIR" ]; then
  echo "ERROR: Could not find MediaBrowser.Common.dll in release archive."
  exit 1
fi

echo "Copying DLLs from ${DLL_DIR}..."
cp "$DLL_DIR"/*.dll "$DEST/"

echo ""
echo "Done! DLLs are in .jellyfin-sdk/"
echo "You can now build normally: dotnet build"
echo "(JellyfinSdkDir defaults to .jellyfin-sdk/ in the csproj)"
