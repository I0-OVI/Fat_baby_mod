#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORKSPACE="$ROOT_DIR/workshop/fat_baby_beta_uploader_workspace"
CONTENT="$WORKSPACE/content"
GAME_MACOS_DIR="$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS"

mkdir -p "$CONTENT"

find_file() {
  local rel="$1"
  shift
  for dir in "$@"; do
    if [[ -f "$dir/$rel" ]]; then
      printf '%s\n' "$dir/$rel"
      return 0
    fi
  done
  return 1
}

BETA_MOD_DIR="$GAME_MACOS_DIR/mods/fat_baby_beta"
DISABLED_BETA_MOD_DIR="$GAME_MACOS_DIR/mods-disabled/fat_baby_beta"
WORKSHOP_BETA_DIR="$HOME/Library/Application Support/Steam/steamapps/workshop/content/2868840/3749546498"

DLL="$(find_file fat_baby_beta.dll "$ROOT_DIR/workspace/mod-beta/.godot/mono/temp/bin/Debug" "$BETA_MOD_DIR" "$DISABLED_BETA_MOD_DIR" "$WORKSHOP_BETA_DIR")"
PCK="$(find_file mod.pck "$ROOT_DIR/workspace/mod-beta/build")"
MANIFEST="$(find_file mod.json "$BETA_MOD_DIR" "$DISABLED_BETA_MOD_DIR" "$WORKSHOP_BETA_DIR" "$ROOT_DIR/workspace/mod-beta")"
PREVIEW="$ROOT_DIR/workspace/mod-beta/mod/images/character_select/fat_baby_cover.png"

if [[ ! -f "$PREVIEW" ]]; then
  echo "Missing Workshop preview image: $PREVIEW" >&2
  exit 1
fi

cp "$DLL" "$CONTENT/fat_baby_beta.dll"
cp "$PCK" "$CONTENT/fat_baby_beta.pck"
cp "$MANIFEST" "$CONTENT/mod.json"
cp "$PREVIEW" "$WORKSPACE/image.png"

echo "Prepared beta Workshop workspace: $WORKSPACE"
