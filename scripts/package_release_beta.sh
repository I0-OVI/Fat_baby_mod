#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_DIR="$ROOT_DIR/workspace/mod-beta"
DIST_DIR="$ROOT_DIR/dist"
STAGING_DIR="$DIST_DIR/staging-beta"
BUILD_DLL="$MOD_DIR/.godot/mono/temp/bin/Debug/fat_baby_beta.dll"
BUILD_PCK="$MOD_DIR/build/mod.pck"
MOD_ID="fat_baby_beta"

usage() {
  cat <<'EOF'
Usage: scripts/package_release_beta.sh [version] [--skip-build]

  version       Defaults to "version" in workspace/mod-beta/mod.json.
  --skip-build  Zip existing beta artifacts without rebuilding.

Creates: dist/fat-baby-mod-beta-<version>.zip
  fat_baby_beta/mod.json, fat_baby_beta/fat_baby_beta.dll, fat_baby_beta/fat_baby_beta.pck
  INSTALL-beta.md

Does NOT bundle BaseLib. Players must install beta-compatible BaseLib separately.
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
    echo "Run: ./scripts/sts2_mod_dev_beta.sh package" >&2
    exit 1
  fi
}

write_install_doc() {
  cat > "$STAGING_DIR/INSTALL-beta.md" <<'EOF'
# Fat Baby Beta 安装说明

本包仅适用于 **Slay the Spire 2 Steam beta 测试分支**。

## 安装

1. Steam 中将游戏切换到 beta 分支。
2. 单独安装与 beta 游戏版本匹配的 **BaseLib**（不要混用稳定版 zip 里的旧 BaseLib）。
3. 将 `fat_baby_beta/` 复制到游戏 `mods/` 目录。
4. 重启游戏，在 Mod 列表启用 **Fat Baby (Beta)**。

## 注意

- 不要与稳定版 `fat_baby` 同时用于生产测试；beta 与稳定应使用各自 BaseLib 与 DLL。
- 若日志出现 `HarmonyException` / `Undefined target method`，先更新 BaseLib 并重新编译本 mod。
EOF
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
    echo "Building beta mod for release..."
    "$ROOT_DIR/scripts/sts2_mod_dev_beta.sh" package
  fi

  require_built_artifacts

  rm -rf "$STAGING_DIR"
  mkdir -p "$STAGING_DIR/$MOD_ID" "$DIST_DIR"

  cp "$MOD_DIR/mod.json" "$STAGING_DIR/$MOD_ID/mod.json"
  cp "$BUILD_DLL" "$STAGING_DIR/$MOD_ID/$MOD_ID.dll"
  cp "$BUILD_PCK" "$STAGING_DIR/$MOD_ID/$MOD_ID.pck"
  write_install_doc

  local zip_name="fat-baby-mod-beta-${version}.zip"
  local zip_path="$DIST_DIR/$zip_name"

  rm -f "$zip_path"
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
}

main "$@"
