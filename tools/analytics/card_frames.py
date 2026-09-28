"""Builds the card frames the stats page draws its cards with, from the game's own card art.

The game draws a card from pieces in its UI atlas: the frame (one per card type), the portrait
border, the title banner and the type plaque. shaders/hsv.gdshader tints each piece. The frame
takes the card pool's tint and the other three take the rarity's. The shader is one 3x3 colour
matrix, so the tint here is the tint in the game.

Each output image is every piece above the portrait, stacked, with the portrait window left clear
and the Alchemist energy orb on top. The site puts the card art under it and the text over it.
An Ancient card has its own layout: full-card art and a dark text panel.

The game files are not in the repo. Point --game at a project recovered with GDRE Tools. Run this
again only when the game changes its card art.

    python3 tools/analytics/card_frames.py --game <recovered project>
"""

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFont

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "site" / "src" / "assets" / "frames"
ENERGY_ORB = REPO / "Alchemist" / "images" / "charui" / "big_energy.png"

# One image pixel per half a card unit. The game's card is 300 x 422 units and its atlas pieces are
# drawn at this scale, so nothing is enlarged
SCALE = 2
# The canvas in card units, around the card's centre: the orb and the banner stick out past the frame
LEFT, TOP, RIGHT, BOTTOM = -166, -227, 164, 211

# (h, s, v) for shaders/hsv.gdshader. Frame: AlchemistCardPool. Rarity: materials/cards/banners/
POOL_TINT = (0.75, 0.4, 0.8)
RARITY_TINTS = {
    "common": (1.0, 0.0, 0.85),  # Basic, Common and Token as well
    "uncommon": (1.0, 1.0, 1.0),
    "rare": (0.563, 1.198, 1.14),
    "event": (0.875, 0.85, 0.9),
}
TYPES = ["attack", "skill", "power"]
TYPE_LABELS = {"attack": "Attack", "skill": "Skill", "power": "Power"}

# Atlas regions: (page, x, y, w, h, margin) from images/atlases/ui_atlas.sprites/card/*.tres
FRAMES = {"attack": (0, 1320, 83, 598, 844), "skill": (0, 1221, 929, 598, 844), "power": (0, 621, 929, 598, 844)}
BORDERS = {"attack": (1, 1329, 1, 551, 420), "skill": (1, 1313, 423, 551, 420), "power": (1, 674, 148, 551, 420)}
BANNER = (1, 674, 1, 653, 145, (1, 23, 2, 25))
PLAQUE = (0, 197, 1948, 123, 75)
ANCIENT_BANNER = (1, 1, 1, 671, 182)
# From images/atlases/compressed.sprites/card_template/ on compressed_0.png
ANCIENT_TEXT_BG = {
    "attack": (824, 510, 676, 499, (10, 10, 17, 34)),
    "skill": (1217, 1150, 674, 427, (8, 106, 19, 106)),
    "power": (824, 1, 677, 507, (8, 0, 16, 26)),
}

YIQ = np.array([[0.2989, 0.5870, 0.1140], [0.5959, -0.2774, -0.3216], [0.2115, -0.5229, 0.3114]])


def hsv_matrix(h: float, s: float, v: float) -> np.ndarray:
    """shaders/hsv.gdshader as one matrix: into YIQ, turn the hue, scale I and Q by s, all by v, back."""
    angle = (1.0 - h) * 2 * np.pi
    turn = np.array([[1, 0, 0], [0, np.cos(angle), -np.sin(angle)], [0, np.sin(angle), np.cos(angle)]])
    return np.linalg.inv(YIQ) @ (v * np.diag([1, s, s]) @ turn @ YIQ)


def tint(img: Image.Image, hsv: tuple) -> Image.Image:
    px = np.asarray(img).astype(np.float64) / 255
    rgb = np.clip(px[..., :3] @ hsv_matrix(*hsv).T, 0, 1)
    return Image.fromarray((np.dstack([rgb, px[..., 3]]) * 255 + 0.5).astype(np.uint8), "RGBA")


def crop(atlas: Image.Image, x, y, w, h, margin=(0, 0, 0, 0)) -> Image.Image:
    """An atlas region, padded back out to the texture's full size when the packer trimmed it."""
    piece = atlas.crop((x, y, x + w, y + h))
    left, top, extra_w, extra_h = margin
    if not any(margin):
        return piece
    full = Image.new("RGBA", (w + extra_w, h + extra_h))
    full.paste(piece, (left, top))
    return full


def place(canvas: Image.Image, piece: Image.Image, left, top, right, bottom, fit="contain"):
    """Draw a piece into a box given in card units, as a TextureRect's stretch mode does: contain
    keeps the aspect inside the box (mode 5), cover keeps it and crops the overflow (mode 6), fill
    stretches."""
    box = [round((v - o) * SCALE) for v, o in ((left, LEFT), (top, TOP), (right, LEFT), (bottom, TOP))]
    w, h = box[2] - box[0], box[3] - box[1]
    if fit == "fill":
        piece, x, y = piece.resize((w, h), Image.LANCZOS), box[0], box[1]
    else:
        scale = (max if fit == "cover" else min)(w / piece.width, h / piece.height)
        piece = piece.resize((round(piece.width * scale), round(piece.height * scale)), Image.LANCZOS)
        if fit == "cover":
            cx, cy = (piece.width - w) // 2, (piece.height - h) // 2
            piece = piece.crop((cx, cy, cx + w, cy + h))
        x, y = box[0] + (w - piece.width) // 2, box[1] + (h - piece.height) // 2
    canvas.alpha_composite(piece, (x, y))


def nine_patch(piece: Image.Image, width: int, left: int, right: int) -> Image.Image:
    """Stretch the middle of a horizontal nine-patch to a new width, keeping the end caps."""
    mid = piece.crop((left, 0, piece.width - right, piece.height))
    mid = mid.resize((width - left - right, piece.height), Image.LANCZOS)
    out = Image.new("RGBA", (width, piece.height))
    out.paste(piece.crop((0, 0, left, piece.height)), (0, 0))
    out.paste(mid, (left, 0))
    out.paste(piece.crop((piece.width - right, 0, piece.width, piece.height)), (width - right, 0))
    return out


def plaque_width(game: Path, card_type: str) -> float:
    """The game sizes the plaque to its label: 16 px Kreon Bold plus 17, at least 61 (NCard)."""
    font = ImageFont.truetype(str(game / "fonts" / "kreon_bold.ttf"), 16)
    return max(max(font.getlength(TYPE_LABELS[card_type]), 44) + 17, 61)


def canvas() -> Image.Image:
    return Image.new("RGBA", ((RIGHT - LEFT) * SCALE, (BOTTOM - TOP) * SCALE))


def add_plaque_and_orb(card: Image.Image, plaque: Image.Image, width: float, orb: Image.Image):
    half = width / 2
    place(card, nine_patch(plaque, round(width * SCALE), 26, 24), -half, 1, half, 38, fit="fill")
    place(card, orb, -166, -227, -102, -163)


def build(game: Path, out: Path) -> list[Path]:
    atlases = [Image.open(game / "images" / "atlases" / f"ui_atlas_{i}.png").convert("RGBA") for i in (0, 1)]
    compressed = Image.open(game / "images" / "atlases" / "compressed_0.png").convert("RGBA")
    region = lambda spec: crop(atlases[spec[0]], *spec[1:])
    orb = Image.open(ENERGY_ORB).convert("RGBA")
    plaque, banner = region(PLAQUE), region(BANNER)
    out.mkdir(parents=True, exist_ok=True)
    written = []

    def save(img: Image.Image, name: str):
        path = out / f"{name}.webp"
        img.save(path, "WEBP", quality=88, method=6)
        written.append(path)

    for card_type in TYPES:
        frame = tint(region(FRAMES[card_type]), POOL_TINT)
        for rarity, hsv in RARITY_TINTS.items():
            card = canvas()
            place(card, frame, -150, -211, 150, 211)
            place(card, tint(region(BORDERS[card_type]), hsv), -137.5, -164, 137.5, 46)
            place(card, tint(banner, hsv), -163, -207, 164, -124, fit="cover")
            add_plaque_and_orb(card, tint(plaque, hsv), plaque_width(game, card_type), orb)
            save(card, f"{card_type}_{rarity}")

        card = canvas()
        text_bg = crop(compressed, *ANCIENT_TEXT_BG[card_type])
        alpha = text_bg.getchannel("A").point(lambda a: round(a * 0.66))
        text_bg = Image.merge("RGBA", (*Image.new("RGB", text_bg.size).split(), alpha))
        place(card, text_bg, -133, -22, 131, 181)
        place(card, region(ANCIENT_BANNER), -163, -207, 164, -124, fit="cover")
        add_plaque_and_orb(card, tint(plaque, (0.0, 0.2, 0.9)), plaque_width(game, card_type), orb)
        save(card, f"{card_type}_ancient")
    return written


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--game", type=Path, required=True, help="a GDRE-recovered Slay the Spire 2 project")
    parser.add_argument("--out", type=Path, default=OUT)
    args = parser.parse_args()
    args.game = args.game.expanduser()
    if not (args.game / "images" / "atlases" / "ui_atlas_0.png").exists():
        print(f"{args.game}: no images/atlases/ui_atlas_0.png. Recover the game project with GDRE Tools first.",
              file=sys.stderr)
        return 1
    written = build(args.game, args.out)
    total = sum(p.stat().st_size for p in written)
    print(f"wrote {len(written)} images to {args.out.relative_to(REPO) if args.out.is_relative_to(REPO) else args.out}"
          f" ({total / 1024:,.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
