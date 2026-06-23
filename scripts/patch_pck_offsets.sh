#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: scripts/patch_pck_offsets.sh <path/to/mod.pck>" >&2
  exit 1
fi

python3 - "$1" <<'PY'
import struct
import sys
from pathlib import Path

pck = Path(sys.argv[1])
data = bytearray(pck.read_bytes())

if data[:4] != b"GDPC":
    raise SystemExit(f"Not a Godot PCK: {pck}")

file_base = struct.unpack_from("<Q", data, 24)[0]
if file_base == 0:
    print(f"PCK offsets already absolute: {pck}")
    raise SystemExit(0)

dir_offset = struct.unpack_from("<Q", data, 32)[0]
file_count = struct.unpack_from("<I", data, dir_offset)[0]
pos = dir_offset + 4

for _ in range(file_count):
    path_len = struct.unpack_from("<I", data, pos)[0]
    pos += 4 + path_len
    offset_pos = pos
    offset = struct.unpack_from("<Q", data, offset_pos)[0]
    struct.pack_into("<Q", data, offset_pos, offset + file_base)
    pos += 8 + 8 + 16 + 4

struct.pack_into("<Q", data, 24, 0)
pck.write_bytes(data)
print(f"Patched PCK offsets to absolute paths: {pck}")
PY
