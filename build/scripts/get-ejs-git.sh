#!/bin/bash

# This script clones the EmulatorJS repository into the specified directory.
# It is intended to be used in the build process, and run from the project root.
REPO_URL="https://github.com/EmulatorJS/EmulatorJS.git"
SUBREPO_DIR="gaseous-server/wwwroot/emulators/EmulatorJS"
REPO_DIR="./$SUBREPO_DIR"

TARGET_BRANCH="main" # Default target branch

# Recursively mirror all core files from the CDN into the local cores directory.
# This will overwrite existing files but will not delete extra local files.
# If you want a clean sync, delete the destination directory first.
CORES_URL="https://cdn.emulatorjs.org/nightly/data/cores/"
DEST_DIR="./gaseous-server/wwwroot/emulators/EmulatorJS/data/cores"

# Set FORCE_EJS_REFRESH=1 to force a full refresh even if cores already exist.
FORCE_EJS_REFRESH="${FORCE_EJS_REFRESH:-0}"

# If cores are already present, skip network work. This keeps Dockerfile steps
# runnable on every build while avoiding duplicate downloads in CI matrix builds.
if [ "$FORCE_EJS_REFRESH" != "1" ] && [ -d "$DEST_DIR" ] && [ "$(find "$DEST_DIR" -type f -print -quit)" ]; then
	echo "EmulatorJS cores already present in $DEST_DIR; skipping download."
	exit 0
fi

if [ "$FORCE_EJS_REFRESH" = "1" ]; then
	echo "FORCE_EJS_REFRESH=1 set; refreshing EmulatorJS cores."
	rm -rf "$DEST_DIR"
fi

# Refresh submodule only when we actually need to download/update assets.
git submodule update --init --recursive --remote --force "$SUBREPO_DIR"
git submodule set-branch --branch "$TARGET_BRANCH" "$SUBREPO_DIR"
git submodule update --init --recursive --remote --force "$SUBREPO_DIR"

mkdir -p "$DEST_DIR"

# Use wget recursive download:
# -r        : recursive
# -np       : no parent (stay within cores/)
# -nH       : don't create host directory
# --cut-dirs=3 : strip 'nightly/data/cores' from path so deeper structure starts at cores root
# -R "index.html*" : skip auto-generated index listings
# -P DEST_DIR : set destination prefix
# Existing files are overwritten by default.
wget -r -np -nH --cut-dirs=3 -R "index.html*" -P "$DEST_DIR" "$CORES_URL"

echo "EmulatorJS cores download complete into $DEST_DIR"
