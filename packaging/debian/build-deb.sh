#!/usr/bin/env bash
set -euo pipefail

# Build a self-contained Debian package for a 64-bit Linux system.
# Usage: ./packaging/debian/build-deb.sh [version]

version="${1:-0.1.0}"
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
package_root="$project_root/artifacts/deb/medlen_${version}_amd64"
publish_root="$package_root/usr/lib/medlen"

rm -rf "$package_root"
mkdir -p "$publish_root" "$package_root/usr/bin" "$package_root/usr/share/man/man1" "$package_root/DEBIAN"

dotnet restore "$project_root/Medlen.csproj" --runtime linux-x64

dotnet publish "$project_root/Medlen.csproj" \
  --configuration Release \
  --runtime linux-x64 \
  --no-restore \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:InvariantGlobalization=true \
  --output "$publish_root"

ln -s ../lib/medlen/medlen "$package_root/usr/bin/medlen"
install -m 0644 "$project_root/man/medlen.1" "$package_root/usr/share/man/man1/medlen.1"

cat > "$package_root/DEBIAN/control" << EOF
Package: medlen
Version: $version
Section: utils
Priority: optional
Architecture: amd64
Maintainer: Medlen contributors <maintainers@example.com>
Depends: libc6, libgcc-s1, libssl3, libstdc++6, zlib1g
Description: Lightweight media duration command-line tool
 Recursively find common media files and report individual, total, average,
 longest, and shortest durations without requiring FFmpeg.
EOF

mkdir -p "$project_root/artifacts/deb"
dpkg-deb --build --root-owner-group "$package_root" "$project_root/artifacts/deb"
echo "Package created: $project_root/artifacts/deb/medlen_${version}_amd64.deb"
