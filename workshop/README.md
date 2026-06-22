# Steam Workshop Upload

This folder prepares a private Steam Workshop upload for the Slay the Spire 2 mod.

Run:

```bash
./workshop/upload_fat_baby_official_uploader.sh
```

Notes:

- This uses Mega Crit's official `sts2-mod-uploader`.
- `fat_baby_uploader_workspace/workshop.json` starts as a private upload.
- `prepare_fat_baby_uploader_workspace.sh` copies the current stable `fat_baby.dll`, `fat_baby.pck`, `mod.json`, and preview image into the ignored upload payload folder before upload.
- BaseLib is listed as Workshop dependency `3737335127`.
