#!/usr/bin/env bash
# Extract still frames from recorded card art mp4s → PNG (raw, no crop).
# Crop to 1000×760 manually afterward.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MAPPING="$ROOT/scripts/card_art_mapping.tsv"
SWIFT_EXTRACT="$ROOT/scripts/extract_frame.swift"
OUT_DIR="$ROOT/assets/card_portraits_src/raw"
PICKS_DIR="$ROOT/assets/card_portraits_src/picks"
TRIM_MARGIN=1
PROBE_STEP=2
FFMPEG="${FFMPEG:-ffmpeg}"
FFPROBE="${FFPROBE:-ffprobe}"

usage() {
  cat <<'EOF'
Usage:
  extract_card_portrait.sh <中文名> [--at SECONDS | --at-pct PERCENT] [--probe]
  extract_card_portrait.sh --all [--probe] [--at-pct PERCENT]
  extract_card_portrait.sh --dir 罕见1 [--probe]   # 处理 mapping 里 source_dir=罕见1 的行
  extract_card_portrait.sh --dir 稀有 [--probe]
  extract_card_portrait.sh --list

Examples:
  extract_card_portrait.sh 攻击
  extract_card_portrait.sh 攻击 --at 1.5
  extract_card_portrait.sh 攻击 --probe          # 去掉首尾 1s，中间段每 2% 一帧 → picks/<stem>/
  extract_card_portrait.sh --all --at-pct 50

Output:
  assets/card_portraits_src/raw/{output_stem}.png
  assets/card_portraits_src/raw/{output_stem}_pct25.png  (with --probe)

Requires ffmpeg/ffprobe, or falls back to macOS AVFoundation via extract_frame.swift.
EOF
}

have_ffmpeg() {
  command -v "$FFMPEG" >/dev/null 2>&1 && command -v "$FFPROBE" >/dev/null 2>&1
}

lookup_row() {
  local name="$1"
  if [[ "$name" != *.mp4 ]]; then
    name="${name}.mp4"
  fi
  awk -F'\t' -v name="$name" '
    /^#/ || NF < 4 { next }
    $1 == name {
      print $0
      found = 1
      exit
    }
    END { if (!found) exit 1 }
  ' "$MAPPING"
}

list_cards() {
  awk -F'\t' '/^#/ || NF < 4 { next } { printf "%-16s  %-22s  %s\n", $1, $3, $4 }' "$MAPPING"
}

video_duration() {
  local video="$1"
  if have_ffmpeg; then
    "$FFPROBE" -v error -show_entries format=duration -of csv=p=0 "$video"
  else
    swift "$SWIFT_EXTRACT" "$video" /dev/null --duration
  fi
}

extract_with_ffmpeg() {
  local video="$1"
  local output="$2"
  local at_arg="$3"

  mkdir -p "$(dirname "$output")"
  "$FFMPEG" -hide_banner -loglevel error -y \
    -ss "$at_arg" -i "$video" \
    -frames:v 1 -q:v 2 \
    "$output"
}

extract_with_swift() {
  local video="$1"
  local output="$2"
  shift 2
  mkdir -p "$(dirname "$output")"
  swift "$SWIFT_EXTRACT" "$video" "$output" "$@"
}

extract_one() {
  local source_file="$1"
  local output_stem="$2"
  local source_dir="$3"
  local at_seconds="${4:-}"
  local at_pct="${5:-50}"
  local probe="${6:-0}"

  local video="$ROOT/$source_dir/$source_file"
  if [[ ! -f "$video" ]]; then
    echo "error: missing video: $video" >&2
    return 1
  fi

  if [[ "$probe" != "0" ]]; then
    local pick_dir="$PICKS_DIR/${output_stem}"
    mkdir -p "$pick_dir"
    rm -f "$pick_dir"/*.png
    if have_ffmpeg; then
      local dur trim_start trim_end span pct ts out idx=0
      dur="$(video_duration "$video")"
      trim_start="$TRIM_MARGIN"
      trim_end="$(awk -v d="$dur" -v t="$TRIM_MARGIN" 'BEGIN { printf "%.3f", d - t }')"
      span="$(awk -v a="$trim_start" -v b="$trim_end" 'BEGIN { printf "%.3f", b - a }')"
      if ! awk -v span="$span" 'BEGIN { exit (span > 0) ? 0 : 1 }'; then
        echo "error: video too short for ${TRIM_MARGIN}s trim on each end" >&2
        return 1
      fi
      for ((pct = PROBE_STEP; pct <= 96; pct += PROBE_STEP)); do
        idx=$((idx + 1))
        ts="$(awk -v a="$trim_start" -v s="$span" -v p="$pct" 'BEGIN { printf "%.3f", a + s * p / 100 }')"
        out="$pick_dir/$(printf '%02d_pct%02d.png' "$idx" "$pct")"
        extract_with_ffmpeg "$video" "$out" "$ts"
        echo "wrote $out (@ ${ts}s, window ${pct}%)"
      done
    else
      swift "$SWIFT_EXTRACT" "$video" "$pick_dir" --probe-picks 2>/dev/null
    fi
    "$ROOT/scripts/generate_pick_gallery.sh" "$pick_dir" >/dev/null
    echo ""
    echo "Pick frames here: $pick_dir"
    echo "Quick browse: $pick_dir/index.html"
    echo "  open \"$pick_dir/index.html\""
    return 0
  fi

  local out="$OUT_DIR/${output_stem}.png"
  if [[ -n "$at_seconds" ]]; then
    if have_ffmpeg; then
      extract_with_ffmpeg "$video" "$out" "$at_seconds"
      echo "wrote $out (@ ${at_seconds}s, ffmpeg)"
    else
      extract_with_swift "$video" "$out" --at "$at_seconds"
      echo "wrote $out (@ ${at_seconds}s, swift)"
    fi
  else
    if have_ffmpeg; then
      local dur ts
      dur="$(video_duration "$video")"
      ts="$(awk -v d="$dur" -v p="$at_pct" 'BEGIN { printf "%.3f", d * p / 100 }')"
      extract_with_ffmpeg "$video" "$out" "$ts"
      echo "wrote $out (@ ${ts}s, ${at_pct}%, ffmpeg)"
    else
      extract_with_swift "$video" "$out" --at-pct "$at_pct"
      echo "wrote $out (@ ${at_pct}%, swift)"
    fi
  fi
}

main() {
  if [[ ! -f "$MAPPING" ]]; then
    echo "error: mapping not found: $MAPPING" >&2
    exit 1
  fi

  if [[ $# -eq 0 ]]; then
    usage
    exit 1
  fi

  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    --list)
      list_cards
      exit 0
      ;;
    --all|--dir)
      local filter_dir=""
      if [[ "$1" == "--dir" ]]; then
        shift
        filter_dir="${1:-}"
        shift
      fi
      local at_pct="50"
      local probe="0"
      while [[ $# -gt 0 ]]; do
        case "$1" in
          --at-pct) at_pct="$2"; shift 2 ;;
          --probe) probe="1"; shift ;;
          *) echo "unknown option: $1" >&2; exit 1 ;;
        esac
      done
      local ok=0 fail=0
      while IFS=$'\t' read -r source_file _model_id output_stem source_dir; do
        [[ "$source_file" =~ ^# ]] && continue
        [[ -z "$source_file" ]] && continue
        if [[ -n "$filter_dir" && "$source_dir" != "$filter_dir" ]]; then
          continue
        fi
        echo "=== ${source_file%.mp4} → picks/${output_stem} ==="
        if extract_one "$source_file" "$output_stem" "$source_dir" "" "$at_pct" "$probe"; then
          ok=$((ok + 1))
        else
          fail=$((fail + 1))
        fi
        echo ""
      done < "$MAPPING"
      echo "done: ${ok} ok, ${fail} failed"
      exit 0
      ;;
  esac

  local card_name="$1"
  shift

  local at_seconds=""
  local at_pct="50"
  local probe="0"

  while [[ $# -gt 0 ]]; do
    case "$1" in
      --at) at_seconds="$2"; shift 2 ;;
      --at-pct) at_pct="$2"; shift 2 ;;
      --probe) probe="1"; shift ;;
      *) echo "unknown option: $1" >&2; exit 1 ;;
    esac
  done

  local row
  if ! row="$(lookup_row "$card_name")"; then
    echo "error: card not in mapping: $card_name" >&2
    echo "try: $0 --list" >&2
    exit 1
  fi

  IFS=$'\t' read -r source_file _model_id output_stem source_dir <<<"$row"
  extract_one "$source_file" "$output_stem" "$source_dir" "$at_seconds" "$at_pct" "$probe"
}

main "$@"
