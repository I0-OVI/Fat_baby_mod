#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_DIR="$ROOT_DIR/workspace/mod"
DIST_DIR="$ROOT_DIR/dist"
STAGING_DIR="$DIST_DIR/staging"
BUILD_DLL="$MOD_DIR/.godot/mono/temp/bin/Debug/fat_baby.dll"
BUILD_PCK="$MOD_DIR/build/mod.pck"
GAME_ROOT="${STS2_GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2}"
BASELIB_SOURCE="$GAME_ROOT/mods/【001】必装前置"

usage() {
  cat <<'EOF'
Usage: scripts/package_release.sh [version] [--skip-build]

  version     Defaults to "version" field in workspace/mod/mod.json.
  --skip-build  Only zip existing build artifacts (do not run sts2_mod_dev.sh all).

Creates: dist/fat-baby-mod-<version>.zip
  fat_baby/mod.json, fat_baby/fat_baby.dll, fat_baby/fat_baby.pck
  【001】必装前置/ (BaseLib)
  INSTALL.md
EOF
}

read_version_from_json() {
  python3 - <<'PY' "$MOD_DIR/mod.json"
import json, sys
with open(sys.argv[1], encoding="utf-8") as f:
    print(json.load(f)["version"])
PY
}

require_built_artifacts() {
  local missing=0
  for path in "$MOD_DIR/mod.json" "$BUILD_DLL" "$BUILD_PCK"; do
    if [[ ! -f "$path" ]]; then
      echo "Missing: $path" >&2
      missing=1
    fi
  done
  if [[ "$missing" -ne 0 ]]; then
    echo "Run: ./scripts/sts2_mod_dev.sh all" >&2
    echo "Or pass --skip-build only when DLL and PCK already exist." >&2
    exit 1
  fi
}

require_baselib() {
  if [[ ! -d "$BASELIB_SOURCE" ]]; then
    echo "Missing BaseLib folder: $BASELIB_SOURCE" >&2
    echo "Set STS2_GAME_DIR or install BaseLib under the game mods directory." >&2
    exit 1
  fi
}

main() {
  local version=""
  local skip_build=0

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --skip-build)
        skip_build=1
        shift
        ;;
      -h|--help|help)
        usage
        exit 0
        ;;
      *)
        version="$1"
        shift
        ;;
    esac
  done

  if [[ -z "$version" ]]; then
    version="$(read_version_from_json)"
  fi

  if [[ "$skip_build" -eq 0 ]]; then
    echo "Building mod for release (test + dll + pck)..."
    "$ROOT_DIR/scripts/sts2_mod_dev.sh" package
  fi

  require_built_artifacts
  require_baselib

  rm -rf "$STAGING_DIR"
  mkdir -p "$STAGING_DIR/fat_baby" "$DIST_DIR"

  cp "$MOD_DIR/mod.json" "$STAGING_DIR/fat_baby/mod.json"
  cp "$BUILD_DLL" "$STAGING_DIR/fat_baby/fat_baby.dll"
  cp "$BUILD_PCK" "$STAGING_DIR/fat_baby/fat_baby.pck"
  cp "$ROOT_DIR/docs/INSTALL.md" "$STAGING_DIR/INSTALL.md"
  rsync -a --exclude='.DS_Store' "$BASELIB_SOURCE/" "$STAGING_DIR/【001】必装前置/"

  local zip_name="fat-baby-mod-${version}.zip"
  local zip_path="$DIST_DIR/$zip_name"

  rm -f "$zip_path"
  # macOS `zip` stores Chinese paths without UTF-8 flags; Windows extractors often
  # skip those entries, so players only see fat_baby/. Use Python with flag 0x800.
  python3 - <<'PY' "$STAGING_DIR" "$zip_path"
import os, sys, zipfile
from pathlib import Path

staging = Path(sys.argv[1])
out = Path(sys.argv[2])

with zipfile.ZipFile(out, "w", compression=zipfile.ZIP_DEFLATED) as zf:
    for root, dirs, files in os.walk(staging):
        dirs.sort()
        for name in sorted(files):
            if name == ".DS_Store":
                continue
            path = Path(root) / name
            arc = path.relative_to(staging).as_posix()
            info = zipfile.ZipInfo(arc)
            info.flag_bits |= 0x800
            info.compress_type = zipfile.ZIP_DEFLATED
            zf.writestr(info, path.read_bytes())
PY

  rm -rf "$STAGING_DIR"
  echo "Created $zip_path"
  ls -lh "$zip_path"
  unzip -l "$zip_path" | tail -3
}

main "$@"
