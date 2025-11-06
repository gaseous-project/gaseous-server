#!/usr/bin/env bash
# Detect potentially unused files under wwwroot.
# Heuristics:
# 1. Page HTML/JS considered referenced if their base name appears in LoadPageContent('name') or navigateToPage('name') or query parameter 'page=' usage.
# 2. Global scripts considered referenced if listed in scriptLinks array inside index.html, imported via dynamic import(), or mentioned by name in other scripts.
# 3. Stylesheets referenced if linked in <head> or imported via @import in CSS.
# 4. Images referenced if their path appears in any file under wwwroot (src=, url(), etc.).
# Exclusions:
#   - Honors .gitignore: files ignored by git will be skipped.
#   - Skips wwwroot/emulators/EmulatorJS (large third-party bundle).
# Output: JSON summary + plain text sections.
# NOTE: This is a heuristic; manual review required before deletion.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/../../" && pwd)"
WWWROOT="$ROOT_DIR/gaseous-server/wwwroot"
EMULATOR_EXCLUDE_REL="emulators/EmulatorJS"
GITIGNORE_FILE="$ROOT_DIR/.gitignore"

if [[ ! -d "$WWWROOT" ]]; then
  echo "wwwroot directory not found: $WWWROOT" >&2
  exit 1
fi

# Build ignore list from .gitignore (basic lines only; skip patterns with wildcards complexity)
IGNORE_PATTERNS=()
if [[ -f "$GITIGNORE_FILE" ]]; then
  while IFS= read -r line; do
    [[ -z "$line" || "$line" == \#* ]] && continue
    IGNORE_PATTERNS+=("$line")
  done < "$GITIGNORE_FILE"
fi

should_ignore() {
  local path="$1"
  for pat in "${IGNORE_PATTERNS[@]}"; do
    # Basic containment or fnmatch
    if [[ "$pat" == *"*"* || "$pat" == *"?"* || "$pat" == *"["* ]]; then
      if [[ $path == $pat ]]; then return 0; fi
    else
      if [[ "$path" == *"$pat"* ]]; then return 0; fi
    fi
  done
  return 1
}

# Collect all candidate files excluding emulator bundle and .DS_Store
mapfile -t ALL_FILES < <(find "$WWWROOT" -type f \( ! -path "*/$EMULATOR_EXCLUDE_REL/*" \) ! -name '.DS_Store' | sed "s#$WWWROOT/##")

# Filter out ignored files
FILTERED_FILES=()
for f in "${ALL_FILES[@]}"; do
  if should_ignore "$f"; then continue; fi
  FILTERED_FILES+=("$f")
done

# Function to grep safely
grep_ref() {
  local pattern="$1"
  grep -R --no-color -F "$pattern" "$WWWROOT" 2>/dev/null || true
}

# Extract referenced page names via LoadPageContent('name') and navigateToPage('name')
PAGE_NAMES=$(grep -R -E "LoadPageContent\('([a-zA-Z0-9_-]+)'" "$WWWROOT/index.html" "$WWWROOT/scripts" "$WWWROOT/pages" 2>/dev/null | sed -E "s/.*LoadPageContent\('([a-zA-Z0-9_-]+)'.*/\1/" | sort -u)
NAV_PAGE_NAMES=$(grep -R -E "navigateToPage\('([a-zA-Z0-9_-]+)'" "$WWWROOT" 2>/dev/null | sed -E "s/.*navigateToPage\('([a-zA-Z0-9_-]+)'.*/\1/" | sort -u)
QUERY_PAGE_NAMES=$(grep -R -E "page=([a-zA-Z0-9_-]+)" "$WWWROOT" 2>/dev/null | sed -E "s/.*page=([a-zA-Z0-9_-]+).*/\1/" | sort -u)
REFERENCED_PAGES=$(printf "%s\n%s\n%s\n" "$PAGE_NAMES" "$NAV_PAGE_NAMES" "$QUERY_PAGE_NAMES" | sort -u | grep -v '^$' || true)

# All page base names from pages/*.html
ALL_PAGE_BASES=$(find "$WWWROOT/pages" -maxdepth 1 -type f -name '*.html' -printf '%f\n' | sed 's/\.html$//' | sort -u)

# Determine unused pages
UNUSED_PAGES=()
for p in $ALL_PAGE_BASES; do
  if ! echo "$REFERENCED_PAGES" | grep -qx "$p"; then
    UNUSED_PAGES+=("$p")
  fi
done

# Scripts referenced via scriptLinks array plus explicit <script src> tags
# 1. scriptLinks array entries
SCRIPT_LINKS=$(grep -A50 "scriptLinks" "$WWWROOT/index.html" | grep -E '"/scripts/[^" ]+\.js"' | sed -E 's/.*"\/scripts\/([^" ]+\.js)".*/\1/' | sort -u)
# 2. language.js imported dynamically
SCRIPT_LINKS+=$'\n'"language.js"
# 3. <script src="/scripts/..."> tags in index.html and all pages/*.html
HTML_SCRIPT_TAG_SOURCES=$(grep -R -E '<script[^>]+src="/scripts/[^" ]+\.js"' "$WWWROOT/index.html" "$WWWROOT/pages" 2>/dev/null | sed -E 's/.*src="\/scripts\/([^" ]+\.js)".*/\1/' | sort -u || true)
SCRIPT_LINKS+=$'\n'"$HTML_SCRIPT_TAG_SOURCES"
# Consolidate unique referenced scripts
REFERENCED_SCRIPTS=$(echo "$SCRIPT_LINKS" | tr '\n' '\n' | grep -v '^$' | sort -u)

# All top-level scripts
ALL_SCRIPTS=$(find "$WWWROOT/scripts" -maxdepth 1 -type f -name '*.js' -printf '%f\n' | sort -u)
UNUSED_SCRIPTS=()
for s in $ALL_SCRIPTS; do
  if ! echo "$REFERENCED_SCRIPTS" | grep -qx "$s"; then
    # secondary heuristic: if name appears anywhere else it might be dynamic
    if ! grep_ref "$s" | grep -q "$s"; then
      UNUSED_SCRIPTS+=("$s")
    fi
  fi
done

# Stylesheets referenced in index.html
STYLE_LINKS=$(grep -A50 "styleSheets" "$WWWROOT/index.html" | grep -E '"/styles/[^" ]+\.css"' | sed -E 's/.*"\/styles\/([^" ]+\.css)".*/\1/' | sort -u)
ALL_STYLES=$(find "$WWWROOT/styles" -maxdepth 1 -type f -name '*.css' -printf '%f\n' | sort -u)
UNUSED_STYLES=()
for st in $ALL_STYLES; do
  if ! echo "$STYLE_LINKS" | grep -qx "$st"; then
    if ! grep_ref "$st" | grep -q "$st"; then
      UNUSED_STYLES+=("$st")
    fi
  fi
done

# Images heuristic: referenced if literal path appears anywhere. Large; we keep simple.
ALL_IMAGES=$(find "$WWWROOT/images" -type f -printf '%P\n' 2>/dev/null | sort -u || true)
REF_IMAGES=$(grep -R --no-color -E '/images/' "$WWWROOT" 2>/dev/null | sed -E 's/.*\/images\/([^"'\'') ]+).*/\1/' | sort -u || true)
UNUSED_IMAGES=()
for im in $ALL_IMAGES; do
  if ! echo "$REF_IMAGES" | grep -qx "$im"; then
    UNUSED_IMAGES+=("$im")
  fi
done

json_escape() { printf '%s' "$1" | sed 's/"/\\"/g'; }

# Output
printf '\n==== UNUSED PAGES (heuristic) ====\n'
for p in "${UNUSED_PAGES[@]}"; do echo "$p.html"; done
printf '\n==== UNUSED SCRIPTS (heuristic) ====\n'
for s in "${UNUSED_SCRIPTS[@]}"; do echo "$s"; done
printf '\n==== UNUSED STYLES (heuristic) ====\n'
for st in "${UNUSED_STYLES[@]}"; do echo "$st"; done
printf '\n==== UNUSED IMAGES (heuristic) ====\n'
for im in "${UNUSED_IMAGES[@]}"; do echo "$im"; done

# JSON summary for GitHub Actions consumption
{
  printf '{"unusedPages":['
  first=1; for p in "${UNUSED_PAGES[@]}"; do [[ $first -eq 0 ]] && printf ','; first=0; printf '"%s"' "$(json_escape "$p.html")"; done
  printf '],"unusedScripts":['
  first=1; for s in "${UNUSED_SCRIPTS[@]}"; do [[ $first -eq 0 ]] && printf ','; first=0; printf '"%s"' "$(json_escape "$s")"; done
  printf '],"unusedStyles":['
  first=1; for st in "${UNUSED_STYLES[@]}"; do [[ $first -eq 0 ]] && printf ','; first=0; printf '"%s"' "$(json_escape "$st")"; done
  printf '],"unusedImages":['
  first=1; for im in "${UNUSED_IMAGES[@]}"; do [[ $first -eq 0 ]] && printf ','; first=0; printf '"%s"' "$(json_escape "$im")"; done
  printf ']}'
}

exit 0
