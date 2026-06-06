#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STABLE_DIR="$ROOT_DIR/workspace/mod"
BETA_DIR="$ROOT_DIR/workspace/mod-beta"

if [[ ! -d "$STABLE_DIR/ModCode" ]]; then
  echo "Missing stable workspace: $STABLE_DIR" >&2
  exit 1
fi

mkdir -p "$BETA_DIR"

"$ROOT_DIR/scripts/sync_beta_workspace.sh"

echo "Beta workspace ready at $BETA_DIR"
echo "Next:"
echo "  1. Copy beta game DLLs to deps/game-beta/ (see deps/game-beta/README.md)"
echo "  2. ./scripts/sts2_mod_dev_beta.sh test"
