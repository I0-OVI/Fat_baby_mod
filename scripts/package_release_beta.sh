#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_DIR="$ROOT_DIR/workspace/mod-beta"
DIST_DIR="$ROOT_DIR/dist"
STAGING_DIR="$DIST_DIR/staging-beta"
BUILD_DLL="$MOD_DIR/.godot/mono/temp/bin/Debug/fat_baby_beta.dll"
BUILD_PCK="$MOD_DIR/build/mod.pck"
MOD_ID="fat_baby_beta"
GAME_ROOT="${STS2_BETA_GAME_DIR:-${STS2_GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2}}"
GAME_MACOS_DIR="$GAME_ROOT/SlayTheSpire2.app/Contents/MacOS"
GAME_MODS_DIR="$GAME_MACOS_DIR/mods"

usage() {
  cat <<'EOF'
Usage: scripts/package_release_beta.sh [version] [--skip-build]

  version       Defaults to "version" in workspace/mod-beta/mod.json.
  --skip-build  Zip existing beta artifacts without rebuilding.

Creates: dist/fat-baby-mod-beta-<version>.zip
  fat_baby_beta/mod.json, fat_baby_beta/fat_baby_beta.dll, fat_baby_beta/fat_baby_beta.pck
  BaseLib/ (beta-compatible BaseLib v3.x from the local game install)
  INSTALL-beta.md

Environment:
  STS2_BETA_GAME_DIR   Preferred beta game root when resolving BaseLib.
  STS2_GAME_DIR        Fallback game root.
EOF
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

require_baselib() {
  if ! BASELIB_SOURCE="$(resolve_baselib_source)"; then
    echo "Missing beta-compatible BaseLib (need BaseLib.json + BaseLib.dll)." >&2
    echo "Install BaseLib v3.x under: $GAME_MODS_DIR/BaseLib" >&2
    echo "Or set STS2_BETA_GAME_DIR / STS2_GAME_DIR to your beta game install." >&2
    exit 1
  fi
  echo "Using BaseLib: $BASELIB_SOURCE"
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
2. 解压后会看到 `fat_baby_beta/` 与 `BaseLib/` 两个文件夹。
3. 将这两个文件夹整体复制到游戏 `mods/` 目录（macOS 常见路径见下）。
4. 重启游戏，在 Mod 列表启用 **BaseLib** 与 **Fat Baby (Beta)**。

**macOS**

```text
.../Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/BaseLib/
.../Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/fat_baby_beta/
```

**Windows**

```text
.../Steam/steamapps/common/Slay the Spire 2/mods/BaseLib/
.../Steam/steamapps/common/Slay the Spire 2/mods/fat_baby_beta/
```

## 注意

- 本包内的 `BaseLib/` 为 beta 分支配套版本；不要与稳定版 zip 里的 `【001】必装前置` 混用。
- 不要同时启用稳定版 `fat_baby` 与本包 `fat_baby_beta`。
- 若日志出现 `HarmonyException` / `Undefined target method`，先更新游戏 beta 分支与 BaseLib，再重新安装本包。
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
  require_baselib

  rm -rf "$STAGING_DIR"
  mkdir -p "$STAGING_DIR/$MOD_ID" "$STAGING_DIR/BaseLib" "$DIST_DIR"

  cp "$MOD_DIR/mod.json" "$STAGING_DIR/$MOD_ID/mod.json"
  cp "$BUILD_DLL" "$STAGING_DIR/$MOD_ID/$MOD_ID.dll"
  cp "$BUILD_PCK" "$STAGING_DIR/$MOD_ID/$MOD_ID.pck"
  rsync -a --exclude='.DS_Store' "$BASELIB_SOURCE/" "$STAGING_DIR/BaseLib/"
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
