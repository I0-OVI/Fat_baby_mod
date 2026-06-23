#!/usr/bin/env python3
"""Align a card portrait's color grade to blood_slash.png."""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageEnhance, ImageStat

TARGET = (1000, 760)
TONE_REF = Path(__file__).resolve().parents[1] / "workspace/mod/mod/images/card_portraits/blood_slash.png"


def fit_crop(img: Image.Image) -> Image.Image:
    w, h = img.size
    scale = max(TARGET[0] / w, TARGET[1] / h)
    nw, nh = int(w * scale), int(h * scale)
    resized = img.resize((nw, nh), Image.Resampling.LANCZOS)
    left = (nw - TARGET[0]) // 2
    top = (nh - TARGET[1]) // 2
    return resized.crop((left, top, left + TARGET[0], top + TARGET[1]))


def to_rgb(img: Image.Image) -> Image.Image:
    if img.mode == "RGBA":
        bg = Image.new("RGB", img.size, (86, 78, 67))
        bg.paste(img, mask=img.split()[3])
        return bg
    return img.convert("RGB")


def grade_like_blood_slash(img: Image.Image, ref: Image.Image) -> Image.Image:
    ref_mean = sum(ImageStat.Stat(ref).mean) / 3
    cur_mean = sum(ImageStat.Stat(img).mean) / 3
    brightness = max(0.85, min(1.15, ref_mean / cur_mean if cur_mean else 1.0))

    img = ImageEnhance.Brightness(img).enhance(brightness)
    img = ImageEnhance.Color(img).enhance(0.88)
    img = ImageEnhance.Contrast(img).enhance(1.04)

    r, g, b = img.split()
    r = ImageEnhance.Brightness(r).enhance(1.04)
    b = ImageEnhance.Brightness(b).enhance(0.92)
    return Image.merge("RGB", (r, g, b))


def main() -> int:
    if len(sys.argv) != 3:
        print(f"usage: {Path(sys.argv[0]).name} <input.png> <output.png>", file=sys.stderr)
        return 1

    src_path = Path(sys.argv[1])
    dst_path = Path(sys.argv[2])
    if not TONE_REF.is_file():
        print(f"missing tone reference: {TONE_REF}", file=sys.stderr)
        return 1

    img = to_rgb(fit_crop(Image.open(src_path)))
    ref = Image.open(TONE_REF).convert("RGB")
    out = grade_like_blood_slash(img, ref)
    dst_path.parent.mkdir(parents=True, exist_ok=True)
    out.save(dst_path, "PNG", optimize=True)

    mean = tuple(round(x, 1) for x in ImageStat.Stat(out).mean)
    ref_mean = tuple(round(x, 1) for x in ImageStat.Stat(ref).mean)
    print(f"saved {dst_path}")
    print(f"mean rgb {mean} (blood_slash {ref_mean})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
