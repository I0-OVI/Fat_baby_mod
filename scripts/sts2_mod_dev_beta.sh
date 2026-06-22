#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_DIR="$ROOT_DIR/workspace/mod-beta"
MOD_ID="fat_baby_beta"
ASSEMBLY_NAME="fat_baby_beta"

GAME_ROOT="${STS2_BETA_GAME_DIR:-${STS2_GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2}}"
GAME_APP="$GAME_ROOT/SlayTheSpire2.app"
GAME_MACOS_DIR="$GAME_APP/Contents/MacOS"
GAME_MODS_DIR="$GAME_MACOS_DIR/mods"
GAME_DISABLED_MODS_DIR="$GAME_MACOS_DIR/mods-disabled"
INSTALLED_MOD_DIR="$GAME_MODS_DIR/$MOD_ID"
CONFLICTING_MOD_ID="fat_baby"

BASELIB_DEST="$GAME_MODS_DIR/BaseLib"

GODOT_BIN="${GODOT_BIN:-$HOME/Downloads/Godot_mono.app/Contents/MacOS/Godot}"
LOG_FILE="$HOME/Library/Application Support/SlayTheSpire2/logs/godot.log"

usage() {
  cat <<'EOF'
Usage: scripts/sts2_mod_dev_beta.sh <command>

Beta-channel dev helper for workspace/mod-beta (mod id: fat_baby_beta).

Commands:
  init      Initialize or refresh beta from stable without overwriting beta-only project files.
  sync      Refresh beta ModCode/assets from stable and patch beta ModId.
  assets    Re-sync shared assets (mod/, scenes/) from stable only.
  test      Run card static tests and build the beta C# DLL.
  code      Build beta DLL and install mod.json + fat_baby_beta.dll.
  pck       Export PCK and install mod.json + fat_baby_beta.pck.
  all       Sync BaseLib, build DLL, export PCK, and install everything.
  package   Test, build DLL, and export PCK (no install; for beta release zip).
  deps      Sync BaseLib into the game mods directory.
  install   Install existing mod.json + DLL + PCK without rebuilding.
  log       Show recent mod-loading lines from godot.log.
  launch    Open Slay the Spire 2.

Environment:
  STS2_BETA_GAME_DIR   Preferred game root when Steam is on beta branch.
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

require_beta_workspace() {
  if [[ ! -d "$MOD_DIR/ModCode" ]]; then
    echo "Beta workspace not initialized. Run: ./scripts/sts2_mod_dev_beta.sh init" >&2
    exit 1
  fi
}

resolve_baselib_source() {
  local candidate
  for candidate in \
    "$GAME_MODS_DIR/BaseLib" \
    "$GAME_ROOT/mods/BaseLib" \
    "$GAME_ROOT/mods/【001】必装前置"/BaseLib*; do
    if [[ -f "$candidate/BaseLib.json" && -f "$candidate/BaseLib.dll" ]]; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done
  return 1
}

sync_deps() {
  local baselib_source
  if ! baselib_source="$(resolve_baselib_source)"; then
    echo "Missing beta-compatible BaseLib (need BaseLib.json + BaseLib.dll)." >&2
    echo "Install BaseLib v3.x as: $BASELIB_DEST" >&2
    exit 1
  fi

  mkdir -p "$BASELIB_DEST"
  rsync -a "$baselib_source/" "$BASELIB_DEST/"
  if [[ -d "$GAME_MODS_DIR/【001】必装前置" ]]; then
    rm -rf "$GAME_MODS_DIR/【001】必装前置"
    echo "Removed legacy 【001】必装前置 bundle to avoid duplicate BaseLib id."
  fi
  echo "Synced BaseLib -> $BASELIB_DEST"
}

build_dll() {
  require_beta_workspace
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
  "$ROOT_DIR/scripts/sync_beta_assets.sh"
  sync_manifest_localization
  (cd "$MOD_DIR" && "$GODOT_BIN" --headless --path . --export-pack mod_pck build/mod.pck)
  "$ROOT_DIR/scripts/patch_pck_offsets.sh" "$MOD_DIR/build/mod.pck"
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
  echo "Disabled conflicting stable mod -> $disabled_dir"
}

install_dll() {
  local dll="$MOD_DIR/.godot/mono/temp/bin/Debug/$ASSEMBLY_NAME.dll"
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
  echo "Installed beta mod -> $INSTALLED_MOD_DIR"
}

show_log() {
  if [[ ! -f "$LOG_FILE" ]]; then
    echo "Missing log file: $LOG_FILE" >&2
    exit 1
  fi

  rg -n "Found mod manifest|Loaded [0-9]+ mods|fat_baby_beta|BaseLib|Exception|Error" "$LOG_FILE" | tail -80 || true
}

command="${1:-}"

case "$command" in
  init)
    "$ROOT_DIR/scripts/init_beta_workspace.sh"
    ;;
  sync)
    "$ROOT_DIR/scripts/sync_beta_workspace.sh"
    ;;
  assets)
    "$ROOT_DIR/scripts/sync_beta_assets.sh"
    ;;
  test)
    require_beta_workspace
    MOD_DIR="$MOD_DIR" MOD_CODE_DIR="$MOD_DIR/ModCode" "$ROOT_DIR/scripts/test_cards.sh"
    build_dll
    ;;
  code)
    build_dll
    install_json
    install_dll
    echo "Beta code install complete. Restart the game to reload the DLL."
    ;;
  pck)
    export_pck
    install_json
    install_pck
    echo "Beta PCK install complete. Restart the game to reload resources."
    ;;
  all)
    sync_deps
    build_dll
    export_pck
    install_all
    echo "Beta full export complete. Restart the game before testing."
    ;;
  package)
    require_beta_workspace
    MOD_DIR="$MOD_DIR" MOD_CODE_DIR="$MOD_DIR/ModCode" "$ROOT_DIR/scripts/test_cards.sh"
    build_dll
    export_pck
    echo "Beta release build complete. Run: ./scripts/package_release_beta.sh"
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
