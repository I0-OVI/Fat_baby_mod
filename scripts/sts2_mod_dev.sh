#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_DIR="$ROOT_DIR/workspace/mod"
MOD_ID="fat_baby"

GAME_ROOT="${STS2_STABLE_GAME_DIR:-${STS2_GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2}}"
GAME_APP="$GAME_ROOT/SlayTheSpire2.app"
GAME_MACOS_DIR="$GAME_APP/Contents/MacOS"
GAME_MODS_DIR="$GAME_MACOS_DIR/mods"
GAME_DISABLED_MODS_DIR="$GAME_MACOS_DIR/mods-disabled"
INSTALLED_MOD_DIR="$GAME_MODS_DIR/$MOD_ID"
CONFLICTING_MOD_ID="fat_baby_beta"

BASELIB_SOURCE="$GAME_MODS_DIR/BaseLib"
BASELIB_DEST="$GAME_MODS_DIR/BaseLib"

GODOT_BIN="${GODOT_BIN:-$HOME/Downloads/Godot_mono.app/Contents/MacOS/Godot}"
LOG_FILE="$HOME/Library/Application Support/SlayTheSpire2/logs/godot.log"

usage() {
  cat <<'EOF'
Usage: scripts/sts2_mod_dev.sh <command>

Commands:
  test      Run card static tests and build the C# DLL.
  code      Build C# DLL and install mod.json + fat_baby.dll. Fast path for card logic.
  pck       Export PCK and install mod.json + fat_baby.pck. Use after localization/assets.
  all       Sync BaseLib, build DLL, export PCK, and install everything.
  package   Test, build DLL, and export PCK (no install; for release zip).
  deps      Sync BaseLib into the actual game-scanned mods directory.
  install   Install existing mod.json + fat_baby.dll + fat_baby.pck without rebuilding.
  log       Show recent mod-loading lines from godot.log.
  launch    Open Slay the Spire 2.

Environment:
  STS2_STABLE_GAME_DIR Preferred stable-channel game root.
  STS2_GAME_DIR        Fallback game root.
  GODOT_BIN            Godot mono binary for PCK export.
EOF
}

require_file() {
  local path="$1"
  if [[ ! -f "$path" ]]; then
    echo "Missing file: $path" >&2
    exit 1
  fi
}

sync_deps() {
  if [[ ! -f "$BASELIB_SOURCE/BaseLib.json" || ! -f "$BASELIB_SOURCE/BaseLib.dll" ]]; then
    echo "Missing BaseLib source: $BASELIB_SOURCE" >&2
    exit 1
  fi

  mkdir -p "$BASELIB_DEST"
  rsync -a "$BASELIB_SOURCE/" "$BASELIB_DEST/"
  echo "Synced BaseLib -> $BASELIB_DEST"
}

build_dll() {
  (cd "$MOD_DIR" && dotnet build mod.sln)
}

sync_manifest_localization() {
  local src="$MOD_DIR/mod/localization"
  local dest="$MOD_DIR/$MOD_ID/localization"

  rm -rf "$dest"
  mkdir -p "$dest"
  rsync -a "$src/" "$dest/"
  echo "Synced localization alias -> res://$MOD_ID/localization"
}

export_pck() {
  require_file "$GODOT_BIN"
  sync_manifest_localization
  (cd "$MOD_DIR" && "$GODOT_BIN" --headless --path . --export-pack mod_pck build/mod.pck)
  patch_pck_offsets "$MOD_DIR/build/mod.pck"
}

patch_pck_offsets() {
  "$ROOT_DIR/scripts/patch_pck_offsets.sh" "$1"
}

install_json() {
  deactivate_conflicting_mod
  mkdir -p "$INSTALLED_MOD_DIR"
  install -m 0644 "$MOD_DIR/mod.json" "$INSTALLED_MOD_DIR/mod.json"
}

deactivate_conflicting_mod() {
  local conflicting_dir="$GAME_MODS_DIR/$CONFLICTING_MOD_ID"
  local disabled_dir="$GAME_DISABLED_MODS_DIR/$CONFLICTING_MOD_ID"

  if [[ ! -d "$conflicting_dir" ]]; then
    return
  fi

  mkdir -p "$GAME_DISABLED_MODS_DIR"
  rm -rf "$disabled_dir"
  mv "$conflicting_dir" "$disabled_dir"
  echo "Disabled conflicting beta mod -> $disabled_dir"
}

install_dll() {
  local dll="$MOD_DIR/.godot/mono/temp/bin/Debug/fat_baby.dll"
  require_file "$dll"
  install -m 0644 "$dll" "$INSTALLED_MOD_DIR/$MOD_ID.dll"
}

install_pck() {
  local pck="$MOD_DIR/build/mod.pck"
  require_file "$pck"
  install -m 0644 "$pck" "$INSTALLED_MOD_DIR/$MOD_ID.pck"
}

install_all() {
  install_json
  install_dll
  install_pck
  echo "Installed mod -> $INSTALLED_MOD_DIR"
}

show_log() {
  if [[ ! -f "$LOG_FILE" ]]; then
    echo "Missing log file: $LOG_FILE" >&2
    exit 1
  fi

  rg -n "Found mod manifest|Loaded [0-9]+ mods|Tried to load mod|Loading mod|Loaded mod|BaseLib|SCARLET|Exception|Error" "$LOG_FILE" | tail -80 || true
}

command="${1:-}"

case "$command" in
  test)
    "$ROOT_DIR/scripts/test_cards.sh"
    ;;
  code)
    build_dll
    install_json
    install_dll
    echo "Fast code install complete. Restart the game to reload the DLL."
    ;;
  pck)
    export_pck
    install_json
    install_pck
    echo "PCK install complete. Restart the game to reload resources."
    ;;
  all)
    sync_deps
    build_dll
    export_pck
    install_all
    echo "Full export complete. Restart the game, then test with: card SCARLET_COST"
    ;;
  package)
    "$ROOT_DIR/scripts/test_cards.sh"
    build_dll
    export_pck
    echo "Release build complete. Run: ./scripts/package_release.sh"
    ;;
  deps)
    sync_deps
    ;;
  install)
    install_all
    ;;
  log)
    show_log
    ;;
  launch)
    open "$GAME_APP"
    ;;
  ""|-h|--help|help)
    usage
    ;;
  *)
    echo "Unknown command: $command" >&2
    usage >&2
    exit 1
    ;;
esac
