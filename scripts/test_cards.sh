#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_DIR="${MOD_DIR:-$ROOT_DIR/workspace/mod}"
MOD_CODE_DIR="${MOD_CODE_DIR:-$MOD_DIR/ModCode}"

cd "$ROOT_DIR"

MOD_DIR="$MOD_DIR" MOD_CODE_DIR="$MOD_CODE_DIR" node scripts/test_cards.mjs
dotnet build "$MOD_DIR/mod.sln"
