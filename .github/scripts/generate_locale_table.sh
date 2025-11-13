#!/usr/bin/env bash
set -euo pipefail

# Generates the locale table section in docs/LOCALISATION.MD between
# <!-- LOCALE_TABLE_START --> and <!-- LOCALE_TABLE_END --> markers.
# Requires: jq

DOC_FILE="docs/Localisation.md"
LOCALE_DIR="gaseous-server/Support/Localisation"
START_MARKER="<!-- LOCALE_TABLE_START -->"
END_MARKER="<!-- LOCALE_TABLE_END -->"

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required to generate the locale table." >&2
  exit 1
fi

if [ ! -d "$LOCALE_DIR" ]; then
  echo "Locale directory '$LOCALE_DIR' not found." >&2
  exit 1
fi

# Build table header
TABLE=$'| Code | English Name | Native Name | Type | Parent | Pluralisation | Notes |\n'
TABLE+=$'|------|--------------|-------------|------|--------|---------------|-------|\n'

# Iterate over JSON files, sorted for deterministic output
for file in $(ls "$LOCALE_DIR"/*.json | sort); do
  code=$(jq -r '.code // ""' "$file")
  name=$(jq -r '.name // ""' "$file")
  nativeName=$(jq -r '.nativeName // ""' "$file")
  type=$(jq -r '.type // ""' "$file")
  pluralRuleExists=$(jq -r 'has("pluralRule")' "$file")
  pluralRulesExists=$(jq -r 'has("pluralRules")' "$file")
  parent="—"
  if [ "$type" = "Overlay" ]; then
    parent=$(echo "$code" | cut -d'-' -f1)
  fi
  pluralisation=""
  if [ "$pluralRuleExists" = "true" ] && [ "$pluralRulesExists" = "true" ]; then
    pluralisation="pluralRule + pluralRules"
  elif [ "$pluralRulesExists" = "true" ]; then
    pluralisation="pluralRules"
  elif [ "$pluralRuleExists" = "true" ]; then
    pluralisation="pluralRule"
  else
    pluralisation="(none)"
  fi
  notes=""
  # Basic heuristic notes for English regional spelling differences
  if [[ "$code" =~ ^en- ]] && [ "$code" != "en-US" ] && [ "$code" != "en" ]; then
    notes="Regional spelling"
  fi
  if [ "$code" = "en" ]; then
    notes="Provides full master string set & advanced multi-category rules"
  fi
  if [ "$code" = "en-US" ]; then
    notes="Acts as default American variant"
  fi
  TABLE+=$"| $code | $name | $nativeName | $type | $parent | $pluralisation | $notes |"$'\n'
 done

# Escape slashes for sed replacement
ESCAPED_TABLE=$(printf '%s' "$TABLE" | sed 's/\\/\\\\/g; s/\//\\\//g')

# Use perl for multi-line safe replacement between markers
perl -0777 -i -pe "s/${START_MARKER}.*?${END_MARKER}/${START_MARKER}\n${ESCAPED_TABLE}${END_MARKER}/s" "$DOC_FILE"

echo "Locale table updated in $DOC_FILE"
