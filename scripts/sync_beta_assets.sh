#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STABLE_DIR="$ROOT_DIR/workspace/mod"
BETA_DIR="$ROOT_DIR/workspace/mod-beta"

if [[ ! -d "$STABLE_DIR/mod" ]]; then
  echo "Missing stable assets: $STABLE_DIR/mod" >&2
  exit 1
fi

mkdir -p "$BETA_DIR"

rsync -a --delete "$STABLE_DIR/mod/" "$BETA_DIR/mod/"

if [[ -d "$STABLE_DIR/scenes" ]]; then
  rsync -a --delete "$STABLE_DIR/scenes/" "$BETA_DIR/scenes/"
fi

echo "Synced shared assets -> $BETA_DIR"
