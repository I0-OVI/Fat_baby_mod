#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UPLOADER_DIR="$ROOT_DIR/tools/sts2-mod-uploader"
WORKSPACE="$ROOT_DIR/workshop/fat_baby_beta_uploader_workspace"

if [[ ! -f "$UPLOADER_DIR/ModUploader.csproj" ]]; then
  mkdir -p "$ROOT_DIR/tools"
  git clone --depth 1 https://github.com/megacrit/sts2-mod-uploader.git "$UPLOADER_DIR"
fi

"$ROOT_DIR/workshop/sync_fat_baby_beta_workshop_metadata.sh"
"$ROOT_DIR/workshop/prepare_fat_baby_beta_uploader_workspace.sh"
DOTNET_ROLL_FORWARD=Major dotnet run --project "$UPLOADER_DIR/ModUploader.csproj" -- upload -w "$WORKSPACE"
