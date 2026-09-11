#!/usr/bin/env python3
"""Shrink the beta card placeholders that final art has replaced.

The placeholder in Alchemist/images/card_portraits/beta/<card>.png is drawn at full size
(1000x760) until the final art lands in card_portraits/<card>.png. After that, only the Use
Beta Art setting shows it, so it is cut to the size of the final art (half, 500x380) and to a
256-color palette. A placeholder that is already at or below that size is left alone, so the
script is safe to run again.

    python3 scripts/shrink_beta_art.py            # only the placeholders that final art replaced
    python3 scripts/shrink_beta_art.py --all      # every placeholder
    python3 scripts/shrink_beta_art.py --dry-run  # report, change nothing

Needs Pillow (pip install Pillow). Godot reimports a changed png on the next
scripts/dev.sh publish; the .import file next to it stays as it is.
"""

import argparse
import sys
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is not installed: pip install Pillow")

REPO = Path(__file__).resolve().parent.parent
PORTRAITS = REPO / "Alchemist" / "images" / "card_portraits"
BETA = PORTRAITS / "beta"
BIG = PORTRAITS / "big"


def has_final_art(name: str) -> bool:
    return (PORTRAITS / name).exists() or (BIG / name).exists()


def target_size(name: str, beta_size: tuple[int, int]) -> tuple[int, int]:
    """The size of the final art when it is on disk, else half the placeholder."""
    small = PORTRAITS / name
    if small.exists():
        with Image.open(small) as im:
            return im.size
    return beta_size[0] // 2, beta_size[1] // 2


def shrink(path: Path, size: tuple[int, int]) -> None:
    with Image.open(path) as im:
        rgba = im.convert("RGBA")
    resized = rgba.resize(size, Image.Resampling.LANCZOS)
    if resized.getchannel("A").getextrema() == (255, 255):
        # Median cut keeps a gradient smoother than the octree does, but it takes RGB only
        out = resized.convert("RGB").quantize(colors=256, method=Image.Quantize.MEDIANCUT,
                                              dither=Image.Dither.NONE)
    else:
        out = resized.quantize(colors=256, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE)
    out.save(path, optimize=True)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--all", action="store_true",
                        help="shrink every placeholder, not only the ones final art replaced")
    parser.add_argument("--dry-run", action="store_true", help="report, change nothing")
    args = parser.parse_args()

    done = at_size = 0
    for path in sorted(BETA.glob("*.png")):
        if not args.all and not has_final_art(path.name):
            continue
        with Image.open(path) as im:
            size = im.size
        target = target_size(path.name, size)
        if size[0] <= target[0] and size[1] <= target[1]:
            at_size += 1
            continue
        before = path.stat().st_size // 1024
        if args.dry_run:
            print(f"{path.name}: {size[0]}x{size[1]} -> {target[0]}x{target[1]} ({before} KB, not changed)")
        else:
            shrink(path, target)
            after = path.stat().st_size // 1024
            print(f"{path.name}: {size[0]}x{size[1]} -> {target[0]}x{target[1]}, {before} KB -> {after} KB")
        done += 1

    verb = "to shrink" if args.dry_run else "shrunk"
    print(f"\n{done} {verb}, {at_size} already at size")
    return 0


if __name__ == "__main__":
    sys.exit(main())
