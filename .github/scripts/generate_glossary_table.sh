#!/usr/bin/env bash
set -euo pipefail

# Regenerates the glossary core terms table between markers
# <!-- GLOSSARY_TABLE_START --> and <!-- GLOSSARY_TABLE_END --> in docs/LOCALISATION_GLOSSARY.md
# Terms sourced from a curated list below; translations pulled from base locale JSON files when keys exist.
# Falls back to hard-coded mapping if specific concept not represented as a direct key.

GLOSSARY_FILE="docs/LOCALISATION_GLOSSARY.md"
LOCALE_DIR="gaseous-server/Support/Localisation"
START_MARKER="<!-- GLOSSARY_TABLE_START -->"
END_MARKER="<!-- GLOSSARY_TABLE_END -->"

BASES=(en fr de pt)

TERMS=(
  "settings"
  "preferences"
  "upload"
  "download"
  "library"
  "home"
  "favorites"
  "play"
  "delete"
  "add"
  "emulator"
  "firmware"
  "metadata"
  "signature"
  "rom"
  "media"
  "screenshot"
  "saved_game"
  "user"
  "username"
  "password"
  "two_factor_authentication"
  "error"
  "warning"
  "information"
  "search"
  "filter"
  "platform"
  "genre"
  "theme"
  "rating"
  "player"
  "collection"
  "size"
  "summary"
  "description"
  "link"
  "source"
  "logs"
  "task"
  "maintenance"
  "database"
  "import"
  "export"
  "reset"
  "background"
  "enabled"
  "disabled"
  "pending"
  "processing"
  "complete"
  "failed"
  "session"
  "game"
  "cover_art"
)

# Mapping of concept -> locale key(s) to search.
# Some concepts map to banner.* or generic.* keys; provide ordered candidates per concept.
# Format: concept:key1,key2,...
declare -A CONCEPT_KEYS=(
  [settings]="banner.settings;card.settings.header"
  [preferences]="banner.preferences"
  [upload]="banner.upload;generic.upload_complete"
  [download]="generic.download"
  [library]="banner.library"
  [home]="banner.home"
  [favorites]="home.favourites;home.favorites"
  [play]="generic.play;card.game.play_button_label"
  [delete]="generic.delete;card.management.delete"
  [add]="generic.add"
  [emulator]="card.buttons.emulator"
  [firmware]="card.settings.menu.firmware"
  [metadata]="card.management.metadata"
  [signature]="datasources.signature_source_label"
  [rom]="card.tabs.roms"
  [media]="card.rom.media_prefix"
  [screenshot]="generic.screenshot"
  [saved_game]="home.saved_games"
  [user]="usersettings.new_user_button"
  [username]="accountmodal.section.username_header"
  [password]="accountmodal.current_password_label"
  [two_factor_authentication]="accountmodal.tab.two_factor_authentication"
  [error]="generic.error;console.error_generic"
  [warning]="logs.filter.warning"
  [information]="logs.filter.information"
  [search]="filtering.title_search"
  [filter]="collection.edit.filter_header"
  [platform]="card.settings.menu.platforms"
  [genre]="collection.edit.genres_header"
  [theme]="collection.edit.themes_header"
  [rating]="card.rating.label"
  [player]="usereditmodal.user_role.player_option"
  [collection]="collection.edit.collection_header"
  [size]="card.rom.size_prefix"
  [summary]="card.game.summary_header"
  [description]="card.game.description_prefix"
  [link]="card.metadata.link_label"
  [source]="card.metadata.source_label"
  [logs]="card.settings.menu.logs"
  [task]="task.library_scan"
  [maintenance]="task.weekly_maintenance"
  [database]="homesettings.database_header"
  [import]="banner.upload"
  [export]="platforms.button.export_json"
  [reset]="card.emulator.reset_to_default"
  [background]="process.background"
  [enabled]="generic.enabled"
  [disabled]="generic.disabled"
  [pending]="uploadrommodal.status.pending"
  [processing]="uploadrommodal.status.processing"
  [complete]="uploadrommodal.status.complete"
  [failed]="uploadrommodal.status.failed"
  [session]="console.failed_create_session"
  [game]="first2page.metadata.igdb_option;index.title"
  [cover_art]="card.game.cover_art_alt"
)

function extract_term() {
  local concept="${1:-}" loc="${2:-}"
  if [ -z "$loc" ]; then echo ""; return; fi
  local file="$LOCALE_DIR/$loc.json"
  if [ ! -f "$file" ]; then echo ""; return; fi
  local keys_string="${CONCEPT_KEYS[$concept]}"
  IFS=';' read -r -a candidates <<< "$keys_string"
  for k in "${candidates[@]}"; do
    # jq path lookup
    local value=$(jq -r --arg key "$k" '.strings[$key] // .serverstrings[$key] // empty' "$file")
    if [ -n "$value" ] && [ "$value" != "null" ]; then
      echo "$value"; return
    fi
  done
  echo "" # fallback blank; script may later substitute manual mapping
}

# Manual fallback map for concepts not present; keyed by concept:en|fr|de|pt
declare -A MANUAL_FALLBACK=(
  [media]="Media|Support|Medien|Média"
  [saved_game]="Saved Games|Jeux sauvegardés|Gespeicherte Spiele|Jogos Gravados"
  [favorites]="Favorites|Favoris|Favoriten|Favoritos"
)

# Build table header
TABLE=$'| Key Concept | English | French | German | Portuguese |\n'
TABLE+=$'|-------------|---------|--------|--------|------------|\n'

for term in "${TERMS[@]}"; do
  en_val=$(extract_term "$term" en)
  fr_val=$(extract_term "$term" fr)
  de_val=$(extract_term "$term" de)
  pt_val=$(extract_term "$term" pt)

  if [ -z "$en_val$fr_val$de_val$pt_val" ] && [ -n "${MANUAL_FALLBACK[$term]:-}" ]; then
    IFS='|' read -r en_val fr_val de_val pt_val <<< "${MANUAL_FALLBACK[$term]}"
  fi

  # Normalise common formatting artifacts (trailing colon for description key etc.)
  en_val=${en_val%:}
  fr_val=${fr_val%:}
  de_val=${de_val%:}
  pt_val=${pt_val%:}

  TABLE+="| ${term//_/ /} | ${en_val:-—} | ${fr_val:-—} | ${de_val:-—} | ${pt_val:-—} |\n"
done

# Safety: ensure markers exist before attempting replacement
if ! grep -q "$START_MARKER" "$GLOSSARY_FILE" || ! grep -q "$END_MARKER" "$GLOSSARY_FILE"; then
  echo "Error: glossary markers not found in $GLOSSARY_FILE. Aborting update." >&2
  exit 1
fi

# Replace section between markers using Perl (non-greedy, DOTALL). Preserve surrounding content.
ESCAPED_TABLE="$TABLE"
if ! perl -0777 -i -pe "s/${START_MARKER}.*?${END_MARKER}/${START_MARKER}\n$ESCAPED_TABLE${END_MARKER}/s" "$GLOSSARY_FILE" 2>/dev/null; then
  echo "Perl replacement failed, attempting awk fallback" >&2
  awk -v start="$START_MARKER" -v end="$END_MARKER" -v repl="$ESCAPED_TABLE" 'BEGIN{infile=""} {infile=infile $0 "\n"} END {
    # Split keeping newlines
    n=split(infile, lines, "\n");
    foundStart=0; foundEnd=0; out=""; inside=0
    for(i=1;i<=n;i++){
      line=lines[i]
      if(line==start){
        foundStart=1
        out=out start "\n" repl end "\n"
        # Skip until end marker encountered
        inside=1; continue
      }
      if(line==end){ foundEnd=1; inside=0; continue }
      if(!inside){ out=out line "\n" }
    }
    if(foundStart && foundEnd){ printf "%s", out } else { printf "%s", infile > "/dev/stderr"; exit 2 }
  }' "$GLOSSARY_FILE" > "$GLOSSARY_FILE.tmp" && mv "$GLOSSARY_FILE.tmp" "$GLOSSARY_FILE"
fi

echo "Glossary table updated successfully in $GLOSSARY_FILE"
