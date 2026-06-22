#!/usr/bin/env python3
"""Composite DS3 covenant items onto onion_knight_magic_cast_v1.png."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
COMPOSITE_DIR = ROOT / ".tmp/composite"
BASE_PATH = ROOT / "onion_knight_magic_cast_v1.before_items.png"
EAR_PATH = ROOT / "workspace/mod/mod/images/card_portraits/proof_of_a_concord_kept.png"
VERTEBRA_PATH = COMPOSITE_DIR / "vertebra_shackle.png"
OUT_PATH = ROOT / "onion_knight_magic_cast_v1.png"


def average_corner_rgb(image: Image.Image) -> tuple[int, int, int]:
    px = image.load()
    w, h = image.size
    samples = [px[0, 0], px[w - 1, 0], px[0, h - 1], px[w - 1, h - 1]]
    return (
        sum(color[0] for color in samples) // 4,
        sum(color[1] for color in samples) // 4,
        sum(color[2] for color in samples) // 4,
    )


def remove_dark_background(image: Image.Image, tolerance: int = 42) -> Image.Image:
    rgba = image.convert("RGBA")
    bg = average_corner_rgb(rgba)
    px = rgba.load()
    w, h = rgba.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            dist = abs(r - bg[0]) + abs(g - bg[1]) + abs(b - bg[2])
            if dist <= tolerance:
                px[x, y] = (r, g, b, 0)
            elif max(r, g, b) < 28:
                px[x, y] = (r, g, b, 0)
    return rgba


def trim_alpha(image: Image.Image, pad: int = 8) -> Image.Image:
    bbox = image.getbbox()
    if bbox is None:
        return image
    left, top, right, bottom = bbox
    left = max(0, left - pad)
    top = max(0, top - pad)
    right = min(image.width, right + pad)
    bottom = min(image.height, bottom + pad)
    return image.crop((left, top, right, bottom))


def add_purple_tint(image: Image.Image, strength: float = 0.10) -> Image.Image:
    rgba = image.convert("RGBA")
    overlay = Image.new("RGBA", rgba.size, (170, 70, 255, int(255 * strength)))
    tinted = Image.alpha_composite(rgba, overlay)
    alpha = tinted.split()[3]
    alpha = ImageEnhance.Brightness(alpha).enhance(1.15)
    tinted.putalpha(alpha)
    return tinted


def paste_item(
    base: Image.Image,
    item: Image.Image,
    center: tuple[int, int],
    scale: float,
    rotation_deg: float,
) -> None:
    width = max(1, int(item.width * scale))
    height = max(1, int(item.height * scale))
    resized = item.resize((width, height), Image.Resampling.LANCZOS)
    if rotation_deg:
        resized = resized.rotate(rotation_deg, expand=True, resample=Image.Resampling.BICUBIC)
    tinted = add_purple_tint(resized)
    tinted = tinted.filter(ImageFilter.GaussianBlur(radius=0.2))
    x = int(center[0] - tinted.width / 2)
    y = int(center[1] - tinted.height / 2)
    base.alpha_composite(tinted, (x, y))


def main() -> None:
    base = Image.open(BASE_PATH).convert("RGBA")
    ear = trim_alpha(remove_dark_background(Image.open(EAR_PATH)))
    vertebra = trim_alpha(remove_dark_background(Image.open(VERTEBRA_PATH)))

    hand_center = (1045, 455)
    paste_item(base, ear, (hand_center[0] - 60, hand_center[1] - 5), scale=0.33, rotation_deg=-12)
    paste_item(base, vertebra, (hand_center[0] + 95, hand_center[1] + 25), scale=1.0, rotation_deg=18)

    base.convert("RGB").save(OUT_PATH, quality=95)
    print(f"Wrote {OUT_PATH}")


if __name__ == "__main__":
    main()
