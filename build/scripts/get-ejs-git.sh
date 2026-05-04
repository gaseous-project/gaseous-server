#!/bin/bash

# This script clones the EmulatorJS repository into the specified directory.
# It is intended to be used in the build process, and run from the project root.
REPO_URL="https://github.com/EmulatorJS/EmulatorJS.git"
SUBREPO_DIR="gaseous-server/wwwroot/emulators/EmulatorJS"
REPO_DIR="./$SUBREPO_DIR"

TARGET_BRANCH="download-file-fixes" # Default target branch

# Reset submodule to ensure a clean state
git submodule update --init --recursive --remote --force "$SUBREPO_DIR"
git submodule set-branch --branch "$TARGET_BRANCH" "$SUBREPO_DIR"
git submodule update --init --recursive --remote --force "$SUBREPO_DIR"

# Recursively mirror all core files from the CDN into the local cores directory.
# This will overwrite existing files but will not delete extra local files.
# If you want a clean sync, delete the destination directory first.
CORES_URL="https://cdn.emulatorjs.org/nightly/data/cores/"
DEST_DIR="./gaseous-server/wwwroot/emulators/EmulatorJS/data/cores"

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
