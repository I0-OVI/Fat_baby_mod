#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORKSPACE="$ROOT_DIR/workshop/fat_baby_uploader_workspace"
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

STABLE_MOD_DIR="$GAME_MACOS_DIR/mods/fat_baby"
DISABLED_STABLE_MOD_DIR="$GAME_MACOS_DIR/mods-disabled/fat_baby"

DLL="$(find_file fat_baby.dll "$ROOT_DIR/workspace/mod/.godot/mono/temp/bin/Debug" "$STABLE_MOD_DIR" "$DISABLED_STABLE_MOD_DIR")"
PCK="$(find_file mod.pck "$ROOT_DIR/workspace/mod/build")"
MANIFEST="$(find_file mod.json "$STABLE_MOD_DIR" "$DISABLED_STABLE_MOD_DIR" "$ROOT_DIR/workspace/mod")"
PREVIEW="$ROOT_DIR/workspace/mod/mod/images/character_select/fat_baby_cover.png"

if [[ ! -f "$PREVIEW" ]]; then
  echo "Missing Workshop preview image: $PREVIEW" >&2
  exit 1
fi

cp "$DLL" "$CONTENT/fat_baby.dll"
cp "$PCK" "$CONTENT/fat_baby.pck"
cp "$MANIFEST" "$CONTENT/mod.json"
cp "$PREVIEW" "$WORKSPACE/image.png"

echo "Prepared Workshop workspace: $WORKSPACE"
