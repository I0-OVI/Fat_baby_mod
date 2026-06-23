# Steam Workshop Upload

This folder prepares Steam Workshop uploads for the Slay the Spire 2 mod.

## Stable

```bash
./workshop/upload_fat_baby_official_uploader.sh
```

Uploads `workshop/fat_baby_uploader_workspace` as a private Workshop item unless
`mod_id.txt` is added to that workspace.

## Beta

```bash
./workshop/upload_fat_baby_beta_official_uploader.sh
```

Uploads `workshop/fat_baby_beta_uploader_workspace`. This workspace already has
`mod_id.txt` for Workshop item `3749546498`, so it updates that existing item.
Before preparing the payload, the upload script syncs the current Steam title
and description back into local `workshop.json` so page edits are preserved.

Notes:

- This uses Mega Crit's official `sts2-mod-uploader`.
- `prepare_fat_baby_uploader_workspace.sh` copies the current stable `fat_baby.dll`, `fat_baby.pck`, `mod.json`, and preview image into the ignored upload payload folder before upload.
- `prepare_fat_baby_beta_uploader_workspace.sh` copies the current beta `fat_baby_beta.dll`, `fat_baby_beta.pck`, `mod.json`, and preview image into the beta upload payload folder before upload.
- BaseLib is listed as Workshop dependency `3737335127`.
