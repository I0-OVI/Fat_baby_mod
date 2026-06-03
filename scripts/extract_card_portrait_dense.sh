#!/usr/bin/env bash
# Dense frame extraction: more samples than extract_card_portrait.sh --probe (2% → ~48).
# Default 1% step → ~96 frames in the trimmed window. Output: picks_dense/<stem>/
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MAPPING="$ROOT/scripts/card_art_mapping.tsv"
SWIFT_EXTRACT="$ROOT/scripts/extract_frame.swift"
PICKS_DIR="$ROOT/assets/card_portraits_src/picks_dense"
TRIM_MARGIN=1
PROBE_STEP=1
PROBE_MAX=99
FFMPEG="${FFMPEG:-ffmpeg}"
FFPROBE="${FFPROBE:-ffprobe}"

usage() {
  cat <<EOF
Usage:
  extract_card_portrait_dense.sh <中文名|output_stem> [--step N] [--trim SECONDS]
  extract_card_portrait_dense.sh --dir 普通 [--step N]
  extract_card_portrait_dense.sh --list

Defaults:
  step=1% of trimmed window → frames at 1%, 2%, …, 99% (~99 frames)
  trim=1s off each end (same as standard probe)

Output:
  assets/card_portraits_src/picks_dense/{output_stem}/

Examples:
  extract_card_portrait_dense.sh 致命一击
  extract_card_portrait_dense.sh fatal_strike --step 1
  extract_card_portrait_dense.sh 致命一击 --step 0.5   # requires ffmpeg (~192 frames)
EOF
}

have_ffmpeg() {
  command -v "$FFMPEG" >/dev/null 2>&1 && command -v "$FFPROBE" >/dev/null 2>&1
}

lookup_row() {
  local name="$1"
  local with_mp4="${name%.mp4}.mp4"
  awk -F'\t' -v name="$name" -v mp4="$with_mp4" '
    /^#/ || NF < 4 { next }
    $1 == mp4 || $1 == name || $3 == name {
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

extract_dense() {
  local source_file="$1"
  local output_stem="$2"
  local source_dir="$3"
  local step="$4"
  local trim_margin="$5"

  local video="$ROOT/$source_dir/$source_file"
  if [[ ! -f "$video" ]]; then
    echo "error: missing video: $video" >&2
    return 1
  fi

  local pick_dir="$PICKS_DIR/${output_stem}"
  mkdir -p "$pick_dir"
  rm -f "$pick_dir"/*.png

  local dur trim_start trim_end span pct ts out idx=0
  dur="$(video_duration "$video")"
  trim_start="$trim_margin"
  trim_end="$(awk -v d="$dur" -v t="$trim_margin" 'BEGIN { printf "%.3f", d - t }')"
  span="$(awk -v a="$trim_start" -v b="$trim_end" 'BEGIN { printf "%.3f", b - a }')"
  if ! awk -v span="$span" 'BEGIN { exit (span > 0) ? 0 : 1 }'; then
    echo "error: video too short for ${trim_margin}s trim on each end" >&2
    return 1
  fi

  if have_ffmpeg; then
    pct="$step"
    while awk -v p="$pct" -v m="$PROBE_MAX" 'BEGIN { exit (p <= m + 0.0001) ? 0 : 1 }'; do
      idx=$((idx + 1))
      ts="$(awk -v a="$trim_start" -v s="$span" -v p="$pct" 'BEGIN { printf "%.3f", a + s * p / 100 }')"
      local pct_label
      if awk -v s="$step" 'BEGIN { exit (s >= 1) ? 0 : 1 }'; then
        pct_label="$(printf '%03d' "$(awk -v p="$pct" 'BEGIN { printf "%.0f", p * 10 }')")"
        out="$pick_dir/$(printf '%03d_pct%s.png' "$idx" "$pct_label")"
      else
        pct_label="$(printf '%02d' "$(awk -v p="$pct" 'BEGIN { printf "%.0f", p }')")"
        out="$pick_dir/$(printf '%03d_pct%02d.png' "$idx" "$pct_label")"
      fi
      extract_with_ffmpeg "$video" "$out" "$ts"
      echo "wrote $out (window ${pct}%, ${ts}s)"
      pct="$(awk -v p="$pct" -v s="$step" 'BEGIN { printf "%.3f", p + s }')"
    done
  else
    swift "$SWIFT_EXTRACT" "$video" "$pick_dir" --probe-picks --probe-step "$step" --probe-max "$PROBE_MAX" --trim "$trim_margin"
    idx="$(find "$pick_dir" -maxdepth 1 -name '*.png' | wc -l | tr -d ' ')"
  fi

  if have_ffmpeg; then
    {
      echo "# ${source_file} — dense probe (${idx} frames)"
      echo "# duration: ${dur}s | trim: skip first/last ${trim_margin}s"
      echo "# window: ${trim_start}s – ${trim_end}s | step: ${step}%"
      echo "# output: picks_dense/${output_stem}/"
    } >"$pick_dir/README.txt"
  fi

  "$ROOT/scripts/generate_pick_gallery.sh" "$pick_dir" >/dev/null
  echo ""
  echo "Dense picks (${idx} frames): $pick_dir"
  echo "  open \"$pick_dir/index.html\""
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
    --dir)
      shift
      local filter_dir="${1:-}"
      shift
      local step="$PROBE_STEP"
      local trim_margin="$TRIM_MARGIN"
      while [[ $# -gt 0 ]]; do
        case "$1" in
          --step) step="$2"; shift 2 ;;
          --trim) trim_margin="$2"; shift 2 ;;
          *) echo "unknown option: $1" >&2; exit 1 ;;
        esac
      done
      local ok=0 fail=0
      while IFS=$'\t' read -r source_file _model_id output_stem source_dir; do
        [[ "$source_file" =~ ^# ]] && continue
        [[ -z "$source_file" ]] && continue
        [[ -n "$filter_dir" && "$source_dir" != "$filter_dir" ]] && continue
        echo "=== ${source_file%.mp4} → picks_dense/${output_stem} (step ${step}%) ==="
        if extract_dense "$source_file" "$output_stem" "$source_dir" "$step" "$trim_margin"; then
          ok=$((ok + 1))
        else
          fail=$((fail + 1))
        fi
        echo ""
      done <"$MAPPING"
      echo "done: ${ok} ok, ${fail} failed"
      exit 0
      ;;
  esac

  local card_name="$1"
  shift
  local step="$PROBE_STEP"
  local trim_margin="$TRIM_MARGIN"
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --step) step="$2"; shift 2 ;;
      --trim) trim_margin="$2"; shift 2 ;;
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
  echo "=== ${source_file%.mp4} → picks_dense/${output_stem} (step ${step}%) ==="
  extract_dense "$source_file" "$output_stem" "$source_dir" "$step" "$trim_margin"
}

main "$@"
