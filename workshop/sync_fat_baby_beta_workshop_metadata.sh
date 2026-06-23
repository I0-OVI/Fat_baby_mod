#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORKSPACE="$ROOT_DIR/workshop/fat_baby_beta_uploader_workspace"
CONFIG="$WORKSPACE/workshop.json"
MOD_ID_FILE="$WORKSPACE/mod_id.txt"

if [[ ! -f "$MOD_ID_FILE" ]]; then
  echo "No beta mod_id.txt found; skipping Workshop metadata sync."
  exit 0
fi

python3 - "$CONFIG" "$MOD_ID_FILE" <<'PY'
import json
import sys
import urllib.parse
import urllib.request

config_path, mod_id_path = sys.argv[1], sys.argv[2]
with open(mod_id_path, encoding="utf-8") as file:
    mod_id = file.read().strip()

payload = urllib.parse.urlencode({
    "itemcount": "1",
    "publishedfileids[0]": mod_id,
}).encode("utf-8")

request = urllib.request.Request(
    "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/",
    data=payload,
    method="POST",
)
with urllib.request.urlopen(request, timeout=20) as response:
    details = json.load(response)["response"]["publishedfiledetails"][0]

title = details.get("title")
description = details.get("description")
if not title or description is None:
    raise SystemExit("Steam response did not include title/description.")

with open(config_path, encoding="utf-8") as file:
    config = json.load(file)

config["title"] = title
config["description"] = description.replace("\r\n", "\n")

with open(config_path, "w", encoding="utf-8") as file:
    json.dump(config, file, ensure_ascii=False, indent=2)
    file.write("\n")

print(f"Synced Workshop title/description for item {mod_id}: {title}")
PY
