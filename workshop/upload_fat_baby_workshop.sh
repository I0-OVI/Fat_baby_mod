#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STEAMCMD="$ROOT_DIR/sdk/tools/ContentBuilder/builder_osx/steamcmd.sh"
VDF="$ROOT_DIR/workshop/workshop_fat_baby.vdf"

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <steam_login_name>"
  echo "SteamCMD will ask for your password and Steam Guard code if needed."
  exit 2
fi

chmod +x "$STEAMCMD" "$ROOT_DIR/sdk/tools/ContentBuilder/builder_osx/steamcmd"
"$STEAMCMD" +login "$1" +workshop_build_item "$VDF" +quit
