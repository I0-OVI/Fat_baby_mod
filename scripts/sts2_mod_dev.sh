#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_DIR="$ROOT_DIR/workspace/mod"

GAME_ROOT="${STS2_GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2}"
GAME_APP="$GAME_ROOT/SlayTheSpire2.app"
GAME_MACOS_DIR="$GAME_APP/Contents/MacOS"
GAME_MODS_DIR="$GAME_MACOS_DIR/mods"
INSTALLED_MOD_DIR="$GAME_MODS_DIR/fat_baby"

PREREQ_SOURCE="$GAME_ROOT/mods/【001】必装前置"
PREREQ_DEST="$GAME_MODS_DIR/【001】必装前置"

GODOT_BIN="${GODOT_BIN:-$HOME/Downloads/Godot_mono.app/Contents/MacOS/Godot}"
LOG_FILE="$HOME/Library/Application Support/SlayTheSpire2/logs/godot.log"

usage() {
  cat <<'EOF'
Usage: scripts/sts2_mod_dev.sh <command>

Commands:
  test      Run card static tests and build the C# DLL.
  code      Build C# DLL and install mod.json + mod.dll. Fast path for card logic.
  pck       Export PCK and install mod.json + mod.pck. Use after localization/assets.
  all       Sync BaseLib, build DLL, export PCK, and install everything.
  package   Test, build DLL, and export PCK (no install; for release zip).
  deps      Sync BaseLib into the actual game-scanned mods directory.
  install   Install existing mod.json + mod.dll + mod.pck without rebuilding.
  log       Show recent mod-loading lines from godot.log.
  launch    Open Slay the Spire 2.
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
  if [[ ! -d "$PREREQ_SOURCE" ]]; then
    echo "Missing BaseLib source: $PREREQ_SOURCE" >&2
    exit 1
  fi

  mkdir -p "$PREREQ_DEST"
  rsync -a "$PREREQ_SOURCE/" "$PREREQ_DEST/"
  echo "Synced BaseLib -> $PREREQ_DEST"
}

build_dll() {
  (cd "$MOD_DIR" && dotnet build mod.sln)
}

export_pck() {
  require_file "$GODOT_BIN"
  (cd "$MOD_DIR" && "$GODOT_BIN" --headless --path . --export-pack mod_pck build/mod.pck)
  patch_pck_offsets "$MOD_DIR/build/mod.pck"
}

patch_pck_offsets() {
  local pck="$1"
  python3 - "$pck" <<'PY'
import struct
import sys
from pathlib import Path

pck = Path(sys.argv[1])
data = bytearray(pck.read_bytes())

if data[:4] != b"GDPC":
    raise SystemExit(f"Not a Godot PCK: {pck}")

file_base = struct.unpack_from("<Q", data, 24)[0]
if file_base == 0:
    print(f"PCK offsets already absolute: {pck}")
    raise SystemExit(0)

dir_offset = struct.unpack_from("<Q", data, 32)[0]
file_count = struct.unpack_from("<I", data, dir_offset)[0]
pos = dir_offset + 4

for _ in range(file_count):
    path_len = struct.unpack_from("<I", data, pos)[0]
    pos += 4 + path_len
    offset_pos = pos
    offset = struct.unpack_from("<Q", data, offset_pos)[0]
    struct.pack_into("<Q", data, offset_pos, offset + file_base)
    pos += 8 + 8 + 16 + 4

struct.pack_into("<Q", data, 24, 0)
pck.write_bytes(data)
print(f"Patched PCK offsets to absolute paths: {pck}")
PY
}

install_json() {
  mkdir -p "$INSTALLED_MOD_DIR"
  install -m 0644 "$MOD_DIR/mod.json" "$INSTALLED_MOD_DIR/mod.json"
}

install_dll() {
  local dll="$MOD_DIR/.godot/mono/temp/bin/Debug/mod.dll"
  require_file "$dll"
  install -m 0644 "$dll" "$INSTALLED_MOD_DIR/mod.dll"
}

install_pck() {
  local pck="$MOD_DIR/build/mod.pck"
  require_file "$pck"
  install -m 0644 "$pck" "$INSTALLED_MOD_DIR/mod.pck"
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
