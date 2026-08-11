#!/bin/bash
#
# Assembles the Tails bundle: the AppImage, its fingerprint, a checker that refuses to launch a
# file that does not match, and instructions written for someone who does not use a terminal.
#
# Run by both ci.yml and release.yml from the same file, so the artifact a pull request exercises
# and the artifact a release publishes cannot drift apart. That mattered here: the zip is only
# built at release time in the old arrangement, so a defect in it would have been discovered by
# whoever downloaded it.
#
# The assertions at the bottom are the point of doing this in a script rather than inline YAML.
# Two things must hold and neither is obvious:
#
#   1. The zip has to STORE the executable bit on start-here.sh. The zip format keeps Unix modes
#      in the external attributes field, and Info-ZIP writes them, but that is a property of the
#      tool doing the zipping and it is asserted here rather than believed. Whether the extractor
#      on Tails then restores it is a separate question that cannot be tested from CI: Tails has
#      no unzip command, extraction goes through Archive Manager or 7zip, and the answer is
#      unverified. So the instructions tell the user how to set the bit themselves, and this
#      assertion only guarantees that the bit is there to be restored if the extractor cooperates.
#   2. The checker must refuse a file that does not match. A verification that cannot fail is
#      decoration, so a corrupted copy is fed to it here and a zero exit is a build failure.
#
# Usage: build-bundle.sh <path-to-appimage> <version> [output-directory]

set -euo pipefail

appimage_path="${1:?path to the AppImage is required}"
version="${2:?version is required}"
outdir="${3:-.}"

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
appimage_name="$(basename "$appimage_path")"
bundle_name="dice-to-seed-${version}-tails"
staging="$(mktemp -d)"
trap 'rm -rf "$staging"' EXIT

echo "Assembling ${bundle_name}.zip from ${appimage_name}"

# A single top-level folder inside the zip, so extracting produces one folder rather than three
# loose files in whatever directory the user happened to be in. The instructions name that folder,
# so the two have to agree.
work="${staging}/${bundle_name}"
mkdir -p "$work"

cp "$appimage_path" "$work/$appimage_name"
cp "${script_dir}/start-here.sh" "$work/start-here.sh"

# The fingerprint is generated here rather than copied, from the file actually being shipped, and
# with a bare filename so `sha256sum -c` works from inside the folder wherever it is extracted.
( cd "$work" && sha256sum "$appimage_name" > SHA256SUMS )
sha256="$(awk '{print $1}' "$work/SHA256SUMS")"

# The instructions quote the version and the fingerprint. Substituted from the same two values the
# rest of the bundle is built from, so a reader comparing the printed hash against SHA256SUMS can
# never be shown two different numbers.
sed -e "s/@VERSION@/${version}/g" -e "s/@SHA256@/${sha256}/g" \
    "${script_dir}/READ-THIS-FIRST.txt" > "$work/READ-THIS-FIRST.txt"

if grep -q "@VERSION@\|@SHA256@" "$work/READ-THIS-FIRST.txt"; then
    echo "::error::A placeholder survived substitution in READ-THIS-FIRST.txt."
    exit 1
fi

# The AppImage is deliberately NOT executable, and this is the least obvious decision in the
# bundle. Archive Manager on Tails restores stored modes, confirmed on a real session, so an
# AppImage stored 755 arrives ready to double-click. That reads as a convenience and is a hole:
# double-clicking the app is then easier than right-clicking start-here.sh, so the fastest route
# to the application is the one that skips the check, and the check is what this artifact exists
# for. At 644 the app cannot run until start-here.sh has verified it and set the bit. Anyone who
# wants to bypass that can still tick Properties, which is a deliberate act rather than an
# accident.
chmod 755 "$work/start-here.sh"
chmod 644 "$work/$appimage_name" "$work/SHA256SUMS" "$work/READ-THIS-FIRST.txt"

mkdir -p "$outdir"
outdir="$(cd "$outdir" && pwd)"
( cd "$staging" && zip -qr "${outdir}/${bundle_name}.zip" "$bundle_name" )

# ---------------------------------------------------------------------------------------------
# Assertion 1: the executable bit is stored in the archive.
# ---------------------------------------------------------------------------------------------
command -v zipinfo >/dev/null || { echo "::error::zipinfo is needed to verify the bundle."; exit 1; }

echo "--- stored modes"
zipinfo "${outdir}/${bundle_name}.zip"

# Both directions matter, and asserting only one of them would miss the defect that matters more.
# The launcher has to arrive runnable, or nothing can be started at all. The AppImage has to arrive
# NOT runnable, or it can be started without being checked.
if ! zipinfo "${outdir}/${bundle_name}.zip" | grep -qE "^-rwxr-xr-x.* ${bundle_name}/start-here\.sh$"; then
    echo "::error::start-here.sh is not stored as executable, so extracting it cannot produce a runnable file."
    exit 1
fi

if ! zipinfo "${outdir}/${bundle_name}.zip" | grep -qE "^-rw-r--r--.* ${bundle_name}/${appimage_name//./\\.}$"; then
    echo "::error::The AppImage is stored as executable, so it can be launched without being checked first."
    exit 1
fi

# ---------------------------------------------------------------------------------------------
# Assertion 2: the checker refuses a file that does not match, and accepts one that does.
#
# Run against the staged folder rather than an extraction, because Tails has no unzip and the
# runner's unzip is not the extractor that will be used. What is under test is the checker's
# logic, which is the same either way. There is no zenity on the runner, so the script's stderr
# fallback is what runs here, which is worth exercising for its own sake.
# ---------------------------------------------------------------------------------------------
echo "--- the checker accepts the file it shipped with"
( cd "$work" && ./start-here.sh --check-only )

echo "--- the checker refuses a corrupted file"
tampered="${staging}/tampered"
cp -r "$work" "$tampered"
printf 'x' >> "${tampered}/${appimage_name}"

if ( cd "$tampered" && ./start-here.sh --check-only ) 2>/dev/null; then
    echo "::error::The checker accepted a corrupted AppImage. It is decoration, not a check."
    exit 1
fi
echo "It refused, as it must."

echo "--- the checker refuses when the fingerprint is missing"
rm -f "${tampered}/SHA256SUMS"
if ( cd "$tampered" && ./start-here.sh --check-only ) 2>/dev/null; then
    echo "::error::The checker passed with no SHA256SUMS present."
    exit 1
fi
echo "It refused, as it must."

ls -lh "${outdir}/${bundle_name}.zip"
echo "${bundle_name}.zip is assembled and verified."
