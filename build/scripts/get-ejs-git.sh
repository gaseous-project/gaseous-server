#!/bin/bash

# This script clones the EmulatorJS repository into the specified directory.
# It is intended to be used in the build process, and run from the project root.
REPO_URL="https://github.com/EmulatorJS/EmulatorJS.git"
REPO_DIR="./gaseous-server/wwwroot/emulators/EmulatorJS"

if [ ! -d "$REPO_DIR/.git" ]; then
	echo "Cloning EmulatorJS repository..."
	rm -rf "$REPO_DIR"
	git clone "$REPO_URL" "$REPO_DIR"
else
	echo "Repository exists. Resetting to origin and pulling latest..."
	pushd "$REPO_DIR" >/dev/null || { echo "Failed to enter repo dir"; exit 1; }
	git fetch origin
	# Prefer main; fall back to master if main not present.
	if git show-ref --verify --quiet refs/remotes/origin/main; then
		TARGET_BRANCH="main"
	else
		TARGET_BRANCH="master"
	fi
	# Ensure local branch exists and is tracking.
	if git rev-parse --verify "$TARGET_BRANCH" >/dev/null 2>&1; then
		git checkout "$TARGET_BRANCH"
	else
		git checkout -b "$TARGET_BRANCH" "origin/$TARGET_BRANCH"
	fi
	git reset --hard "origin/$TARGET_BRANCH"
	git clean -fd
	popd >/dev/null
fi

# Recursively mirror all core files from the CDN into the local cores directory.
# This will overwrite existing files but will not delete extra local files.
# If you want a clean sync, delete the destination directory first.
CORES_URL="https://cdn.emulatorjs.org/latest/data/cores/"
DEST_DIR="./gaseous-server/wwwroot/emulators/EmulatorJS/data/cores"

mkdir -p "$DEST_DIR"

# Use wget recursive download:
# -r        : recursive
# -np       : no parent (stay within cores/)
# -nH       : don't create host directory
# --cut-dirs=3 : strip 'latest/data/cores' from path so deeper structure starts at cores root
# -R "index.html*" : skip auto-generated index listings
# -P DEST_DIR : set destination prefix
# Existing files are overwritten by default.
wget -r -np -nH --cut-dirs=3 -R "index.html*" -P "$DEST_DIR" "$CORES_URL"

echo "EmulatorJS cores download complete into $DEST_DIR"
