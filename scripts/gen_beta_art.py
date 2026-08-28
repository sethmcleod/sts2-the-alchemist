#!/usr/bin/env python3
"""Generate the beta card art gradients and the compendium sheet.

Every card gets its own unique placeholder gradient in
Alchemist/images/card_portraits/beta/. The system:

- Each card still on a placeholder has one color pair and one direction in
  the COLORS table. No two cards share a pair, so every image is unique.
- Cards with hand-made art live in ART_DONE instead, and are never written.
  A card graduating to real art must move between the two in one edit.
- Cards in the same mechanic family share the start color (the anchor)
  and vary the end color. A family reads at a glance; a card stays
  unique.
- Card names, rarity, type, cost, and description come from cards.csv.
  The COLORS table holds only what the csv cannot: colors, direction,
  and the family tag.
- Diagonal gradients run corner to corner for the art size.
- The generator interpolates in float and applies ordered dithering
  before it quantizes to 8-bit. This prevents banding and keeps the
  files small.

Usage:
  python3 scripts/gen_beta_art.py            # regenerate ALL placeholder PNGs
  python3 scripts/gen_beta_art.py card NAME  # regenerate just one, the safe
                                             #   default when adding a card
  python3 scripts/gen_beta_art.py sheet      # write beta-art-sheet.html,
                                             #   a compendium-style list
                                             #   (ignored by git)
  python3 scripts/gen_beta_art.py validate   # check COLORS vs cards.csv

To add or rename a card, update cards.csv and the COLORS table. The
validate step reports any mismatch. Output is deterministic: a rerun
produces byte-identical files.
"""
import csv
import html
import math
import os
import re
import struct
import sys
import zlib

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BETA = os.path.join(REPO, "Alchemist/images/card_portraits/beta")
# Half the display size on purpose: these are linear gradients, which
# upscale exactly, and the smaller canvas lets the art import
# LOSSLESS. Lossy WebP bands badly on smooth gradients.
W, H = 500, 380

# Direction -> (sx, sy) unit vector in pixel space (y points down),
# arrow, label. The generator scales diagonals by the art size, so a
# diagonal always runs true corner to corner.
ORIENTS = {
    "BR": (1, 1, "↘", "TL to BR"),
    "TR": (1, -1, "↗", "BL to TR"),
    "BL": (-1, 1, "↙", "TR to BL"),
    "TL": (-1, -1, "↖", "BR to TL"),
    "D": (0, 1, "↓", "top to bottom"),
    "U": (0, -1, "↑", "bottom to top"),
    "R": (1, 0, "→", "left to right"),
    "L": (-1, 0, "←", "right to left"),
}

# Full-art cards use a portrait canvas; every other card uses W x H
SIZES = {"Wormwood": (303, 426), "Panacea": (303, 426)}

# 8x8 Bayer matrix for ordered dithering. A repeating pattern compresses
# far better in PNG than random noise and prevents banding just as well.
BAYER = [
    [0, 32, 8, 40, 2, 34, 10, 42],
    [48, 16, 56, 24, 50, 18, 58, 26],
    [12, 44, 4, 36, 14, 46, 6, 38],
    [60, 28, 52, 20, 62, 30, 54, 22],
    [3, 35, 11, 43, 1, 33, 9, 41],
    [51, 19, 59, 27, 49, 17, 57, 25],
    [15, 47, 7, 39, 13, 45, 5, 37],
    [63, 31, 55, 23, 61, 29, 53, 21],
]

# Card -> (start color, end color, direction, family tag)
# Cards in a family share the start color. Every pair must be unique;
# the validate command enforces this.
# Direction encodes card type: Attack TR, Skill BR, Power R or L, token vertical (U or D)
# Cards whose beta art is hand-made. They are NOT in COLORS and are never generated.
#
# generate() rewrites every file it knows about, so a card graduating from a gradient to real art
# must LEAVE the COLORS table and join this set in the same edit.
ART_DONE = {
    "All At Once",
    "Apothecary",
    "Backfire",
    "Bestow",
    "Bitter Draught",
    "Blend",
    "Bloom",
    "Bonk",
    "Bottoms Up",
    "Brace",
    "Brine",
    "Bursting Mix",
    "Callus",
    "Caustic Strike",
    "Croak",
    "Defend",
    "Distill",
    "Dose",
    "Drench",
    "Effervesce",
    "Eureka",
    "Fallout",
    "Fizz",
    "Flare Up",
    "Forked Tongue",
    "Free Samples",
    "Fresh Batch",
    "Froth",
    "Fumigate",
    "Fuming Mix",
    "Golden Fruit",
    "Grand Batch",
    "Gulp",
    "Harden",
    "Heavy Dose",
    "Inure",
    "Jab",
    "Lash Out",
    "Mash",
    "Mellow",
    "Meltdown",
    "Mercurial Form",
    "Miasma",
    "Mortar",
    "Needle Point",
    "Nightcap",
    "Numb",
    "Osmosis",
    "Overbrew",
    "Overdose",
    "Overspill",
    "Panacea",
    "Pass It On",
    "Patient Strike",
    "Pour Over",
    "Proof",
    "Puff Up",
    "Puncture",
    "Quench",
    "Reclaim",
    "Reflux",
    "Rerun",
    "Resolve",
    "Rolling Boil",
    "Runoff",
    "Smelling Salts",
    "Spike",
    "Strike",
    "Syrupy Mix",
    "Twist",
    "Untended",
    "Upwell",
    "Zesty Mix",
}

COLORS = {
    "Aged Batch": ("#3b2c07", "#c9a53a", "BR", "Decant"),
    "Cure": ("#3b2c07", "#7fd4b8", "BR", "Decant"),
    "Digest": ("#2e3d0a", "#8fe06b", "BR", "Mix"),
    "Pelt": ("#3b2c07", "#a8e04a", "TR", "Decant"),
    "Refine": ("#4a1a66", "#257bc3", "BR", "Infuse: heavy"),
    "Spatter": ("#650101", "#7ae801", "BR", "Poison out"),
    "Spit": ("#3d0a02", "#e0d6c2", "TR", "Unique"),
    "Spores": ("#0f3d33", "#4adfc0", "BR", "Poison out"),
    "Sweat It Out": ("#4e8701", "#013161", "BR", "Poison skills"),
    "Taste Test": ("#5f011f", "#c18c0f", "BR", "Unique"),
    "Tempered": ("#1f1723", "#5f7fa8", "BR", "Unique"),
    "Thicken": ("#1f4a2e", "#8fd0e8", "BR", "Mix"),
    "Toughen": ("#2f0170", "#6f9ad8", "BR", "Poison payoffs"),
    "Warm Up": ("#3d1508", "#ffab47", "BR", "Mix"),
    "Transmute": ("#5f011f", "#e3b84a", "BR", "Potion and buff skills"),
    "Uncork": ("#1a2f14", "#d8b04a", "R", "Decant"),
    "Unripe Fruit": ("#26a82b", "#ffef2d", "U", "Unique"),
    "Vent": ("#3d1030", "#e05ab0", "TR", "Poison out"),
    "Vintage": ("#6b2444", "#df9723", "BR", "Ferment: skills and powers"),
    "Vitrify": ("#5b3fc4", "#48495f", "BR", "Enchanted payoffs: Antitoxin"),
    "Wallop": ("#00212a", "#3fb0a0", "TR", "Decant"),
    "Warded": ("#45293f", "#c98fd6", "R", "Unique"),
    "Water Down": ("#7a1c02", "#ffdf8a", "BR", "Poison out"),
    "Wormwood": ("#4a0e2e", "#6ba32c", "TR", "Unique"),
}

# Epoch placeholder gradients: one pair per chapter of the timeline,
# matched to the chapter's story. All seven ascend bottom-left to
# top-right, because the timeline is a climb. 812x500 canvas.
EPOCH_DIR = os.path.join(REPO, "Alchemist/images/epochs")
EPOCHS = {
    "alchemist-alchemist1_epoch": ("#2a0701", "#b8862a"),  # Calcination
    "alchemist-alchemist2_epoch": ("#1d3a02", "#0e7a55"),  # Dissolution
    "alchemist-alchemist3_epoch": ("#163d85", "#4fc98f"),  # Separation
    "alchemist-alchemist4_epoch": ("#5b3fc4", "#e8720c"),  # Conjunction
    "alchemist-alchemist5_epoch": ("#4a0e2e", "#7ac74a"),  # Fermentation
    "alchemist-alchemist6_epoch": ("#1b2a44", "#a8dade"),  # Distillation
    "alchemist-alchemist7_epoch": ("#240147", "#ffc832"),  # Coagulation
}

SPECIAL_FILE = {"Strike": "strike_alchemist", "Defend": "defend_alchemist"}


def card_file(name):
    return SPECIAL_FILE.get(name, name.lower().replace(" ", "_"))


def load_cards():
    with open(os.path.join(REPO, "cards.csv")) as f:
        return list(csv.DictReader(f))


def validate():
    """Check that COLORS plus ART_DONE covers cards.csv exactly."""
    names = {r["Card"] for r in load_cards()}
    problems = []
    missing = names - set(COLORS) - ART_DONE
    if missing:
        problems.append(f"cards with no colors: {sorted(missing)}")
    unknown = set(COLORS) - names
    if unknown:
        problems.append(f"colored cards not in cards.csv: {sorted(unknown)}")
    stale = ART_DONE - names
    if stale:
        problems.append(f"ART_DONE cards not in cards.csv: {sorted(stale)}")
    both = set(COLORS) & ART_DONE
    if both:
        problems.append(f"in COLORS and ART_DONE at once, generate would "
                        f"overwrite the real art: {sorted(both)}")
    pairs = {}
    for name, (c0, c1, orient, _fam) in COLORS.items():
        if orient not in ORIENTS:
            problems.append(f"{name}: unknown direction {orient}")
        other = pairs.setdefault((c0, c1), name)
        if other != name:
            problems.append(f"{name} and {other} share the pair {c0} {c1}")
    if problems:
        raise SystemExit("spec error:\n  " + "\n  ".join(problems))


def hex_rgb(s):
    return tuple(int(s[i:i + 2], 16) for i in (1, 3, 5))


def write_png(path, w, h, rgb_rows):
    def chunk(ctype, data):
        c = ctype + data
        return struct.pack(">I", len(data)) + c + struct.pack(
            ">I", zlib.crc32(c) & 0xFFFFFFFF)
    # Sub filter: each pixel stores the difference from its left
    # neighbor. With ordered dithering the residuals repeat every 8
    # pixels, which deflate compresses very well.
    parts = []
    for row in rgb_rows:
        out = bytearray(row)
        for i in range(len(row) - 1, 2, -1):
            out[i] = (out[i] - row[i - 3]) & 0xFF
        parts.append(b"\x01" + bytes(out))
    raw = b"".join(parts)
    png = (b"\x89PNG\r\n\x1a\n"
           + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
           + chunk(b"IDAT", zlib.compress(raw, 9))
           + chunk(b"IEND", b""))
    with open(path, "wb") as f:
        f.write(png)


def force_lossless(png_path):
    """Keep the .import for a gradient on lossless compression.

    Lossy WebP bands badly on a smooth gradient: it flattens the ramp
    into wide plateaus with visible steps. The project default is lossy,
    so each gradient opts out. Godot writes the .import on first import,
    so this is a no-op until then and self-heals on the next run.
    """
    imp = png_path + ".import"
    if not os.path.exists(imp):
        return
    text = open(imp).read()
    fixed = text.replace("compress/mode=1", "compress/mode=0")
    if fixed != text:
        open(imp, "w").write(fixed)


def gen_gradient(path, c0, c1, orient, w=W, h=H):
    sx, sy = ORIENTS[orient][:2]
    dx, dy = sx * w, sy * h
    r0, g0, b0 = hex_rgb(c0)
    r1, g1, b1 = hex_rgb(c1)
    dr, dg, db = r1 - r0, g1 - g0, b1 - b0
    # Projection extremes over the four corners
    projs = [x * dx + y * dy for x in (0, w - 1) for y in (0, h - 1)]
    lo, span = min(projs), max(projs) - min(projs)
    tx = [x * dx for x in range(w)]
    rows = []
    for y in range(h):
        ty = y * dy - lo
        bay = BAYER[y & 7]
        row = bytearray(w * 3)
        i = 0
        for x in range(w):
            t = (tx[x] + ty) / span
            # Ordered dither before the int() floor prevents banding
            n = (bay[x & 7] + 0.5) / 64
            row[i] = int(r0 + dr * t + n)
            row[i + 1] = int(g0 + dg * t + n)
            row[i + 2] = int(b0 + db * t + n)
            i += 3
        rows.append(row)
    write_png(path, w, h, rows)
    force_lossless(path)


def generate_one(name):
    """Write one gradient. Use this for a single new card: a full generate() is a rewrite of
    every file, which is only safe when nothing on disk is hand-made."""
    validate()
    if name in ART_DONE:
        raise SystemExit(f"{name} has hand-made art and is not generated")
    if name not in COLORS:
        raise SystemExit(f"{name} has no COLORS entry")
    c0, c1, orient, _fam = COLORS[name]
    w, h = SIZES.get(name, (W, H))
    gen_gradient(os.path.join(BETA, card_file(name) + ".png"), c0, c1, orient, w, h)
    print(f"{card_file(name)}.png {c0} {c1} {orient}", flush=True)


def generate():
    validate()
    total = len(COLORS)
    for done, (name, (c0, c1, orient, _fam)) in enumerate(
            sorted(COLORS.items()), 1):
        w, h = SIZES.get(name, (W, H))
        gen_gradient(os.path.join(BETA, card_file(name) + ".png"),
                     c0, c1, orient, w, h)
        print(f"[{done}/{total}] {card_file(name)}.png {c0} {c1} {orient}",
              flush=True)
    for name, (c0, c1) in EPOCHS.items():
        gen_gradient(os.path.join(EPOCH_DIR, name + ".png"),
                     c0, c1, "TR", 406, 250)
        print(f"[epoch] {name}.png {c0} {c1} TR", flush=True)


def css_angle(orient, w=W, h=H):
    sx, sy = ORIENTS[orient][:2]
    return round(math.degrees(math.atan2(sx * w, -(sy * h))) % 360, 1)


# ------------------------------------------------------------- sheet
RARITY_ORDER = ["Basic", "Common", "Uncommon", "Rare", "Ancient",
                "Quest", "Token"]
TYPE_ORDER = ["Attack", "Skill", "Power"]
BANNERS = {
    "Basic": "#b9bec6", "Common": "#b9bec6", "Uncommon": "#7adcec",
    "Rare": "#f2b441", "Ancient": "#ffd76a", "Quest": "#a8d96a",
    "Token": "#9aa0a8",
}

# Terms the game renders in gold. Longest first so multi-word terms win.
KEYWORDS = [
    "Exhaust Pile", "Draw Pile", "Discard Pile", "Golden Fruit",
    "Enchanted", "Vulnerable", "Fermented", "Residue",
    "Multiplayer", "Dexterity", "Reaction", "Strength", "Infused",
    "Antitoxin", "Procure", "Exhaust", "Ferment",
    "Poison", "Potion", "Retain", "Innate",
    "Infuse", "Energy", "Block", "Toxic", "Weak", "Stun", "Hand", "Mixes", "Mix",
]
KEYWORD_RE = re.compile(
    r"\b(" + "|".join(re.escape(k) for k in KEYWORDS) + r")\b")
UPGRADE_RE = re.compile(r"\((\+?\d+%?|X(?:\+\d+)?|doesn't)\)")


def highlight(desc):
    s = html.escape(desc)
    s = KEYWORD_RE.sub(r'<span class="kw">\1</span>', s)
    s = UPGRADE_RE.sub(r'<span class="up">(\1)</span>', s)
    return s


def rarity_bucket(rarity):
    first = rarity.split(",")[0].strip()
    return first if first in RARITY_ORDER else "Token"


def sort_key(row):
    rarity = rarity_bucket(row["Rarity"])
    ctype = row["Type"] if row["Type"] in TYPE_ORDER else "zz"
    cost = row["Cost"].strip()
    m = re.match(r"\d+", cost)
    cost_n = int(m.group()) if m else (90 if cost.startswith("X") else 99)
    t = TYPE_ORDER.index(ctype) if ctype in TYPE_ORDER else 9
    return (RARITY_ORDER.index(rarity), t, cost_n, row["Card"])


def _meta_html(name):
    if name in ART_DONE:
        return '<div class="meta"><span class="mono">hand-made art</span></div>'
    c0, c1, orient, _fam = COLORS[name]
    arrow = ORIENTS[orient][2]
    return (f'<div class="meta">'
            f'<span class="mono"><i class="swz" style="background:{c0}">'
            f'</i>{c0} <i class="swz" style="background:{c1}"></i>{c1}'
            f'</span><span>{arrow}</span></div>')


def _plain_card(row):
    name = row["Card"]
    # The sheet is written to the repo root, so the real art is one relative hop away. Showing it
    # rather than a swatch is the point of the sheet: it is what the card actually looks like
    if name in ART_DONE:
        art = (f'background-image:url(Alchemist/images/card_portraits/beta/'
               f'{card_file(name)}.png);background-size:cover')
    else:
        c0, c1, orient, _fam = COLORS[name]
        art = f'background:linear-gradient({css_angle(orient)}deg,{c0},{c1})'
    b = rarity_bucket(row["Rarity"])
    banner = BANNERS[b]
    sub = " &middot; Multiplayer" if "Multiplayer" in row["Rarity"] else ""
    return (
        f'<div class="cell"><div class="card"><div class="cost0">'
        f'{html.escape(row["Cost"].split("(")[0].strip())}</div>'
        f'<div class="banner" style="background:{banner}">'
        f'{html.escape(name)}</div>'
        f'<div class="art0" style="{art};aspect-ratio:{W}/{H}"></div>'
        f'<div class="tpill" style="background:{banner}">'
        f'{html.escape(row["Type"])}{sub}</div>'
        f'<div class="desc">{highlight(row["Description"])}</div></div>'
        f'{_meta_html(name)}</div>')


SHEET_CSS = """
:root{--bg:#f5f4f0;--ink:#22261f;--mut:#6d7264;--line:#dcdad2}
@media (prefers-color-scheme: dark){:root{--bg:#131511;--ink:#e6e8e0;
  --mut:#969b8b;--line:#2e322a}}
body{background:var(--bg);color:var(--ink);margin:0;
  font:15px/1.5 -apple-system,'Segoe UI',system-ui,sans-serif;
  padding:2rem 1rem 4rem}
main{max-width:1280px;margin:0 auto}
h1{font-size:1.5rem}
h2{font-size:1.05rem;color:var(--mut);text-transform:uppercase;
  letter-spacing:.08em;margin:2.2rem 0 .8rem;border-bottom:1px solid
  var(--line);padding-bottom:.3rem}
.intro{color:var(--mut);font-size:.85rem;max-width:110ch}
.mono{font-family:ui-monospace,Menlo,monospace;font-size:.75rem}
.grid{display:grid;grid-template-columns:repeat(auto-fill,
  minmax(230px,1fr));gap:1.4rem 1.1rem}
.cell{display:flex;flex-direction:column}
.cell .card{flex:1}
.meta{display:flex;align-items:center;gap:.6rem;
  font-size:.66rem;color:var(--mut);padding:.3rem .15rem 0}
.meta .mono{font-size:.62rem}
.swz{display:inline-block;width:.72em;height:.72em;border-radius:3px;
  margin-right:.28em;vertical-align:-1px;
  border:1px solid rgba(128,128,128,.5)}
.kw{color:#f0c95c}
.up{color:#8f93a0}
.card{background:linear-gradient(160deg,#5b4c96,#473a75);
  border-radius:12px;padding:.5rem;color:#e8e8ea;position:relative;
  border:1px solid #6c5cab;display:flex;flex-direction:column;gap:.4rem}
.banner{border-radius:6px;text-align:center;font-weight:700;
  color:#23262e;padding:.16rem .9rem;font-size:.9rem}
.cost0{position:absolute;top:-.5rem;left:-.5rem;width:1.8rem;
  height:1.8rem;border-radius:50%;background:radial-gradient(circle at
  35% 30%,#ffd985,#c98a1a);color:#23262e;font-weight:800;display:flex;
  align-items:center;justify-content:center;z-index:1}
.art0{border-radius:4px;border:3px solid #b9bec6}
.tpill{align-self:center;border-radius:4px;font-size:.68rem;
  font-weight:600;padding:.02rem .55rem;color:#23262e;
  margin-top:-1.15rem;position:relative;z-index:1}
.desc{font-size:.76rem;line-height:1.5;text-align:center;
  color:#e2e4ea;flex:1;background:#2b2d37;border-radius:6px;
  padding:.85rem .7rem}
"""


def sheet(path):
    """Write a compendium-style list of every card with its beta art."""
    validate()
    rows = ['<!doctype html><meta charset="utf-8">\n'
            '<title>Alchemist beta card compendium</title>\n'
            f'<style>{SHEET_CSS}</style><main>\n'
            '<h1>Beta card compendium</h1>\n'
            '<p class="intro">Every card with its generated beta art, in '
            'compendium order. Regenerate art with <span class="mono">'
            'python3 scripts/gen_beta_art.py</span>; colors live in the '
            'COLORS table and everything else comes from cards.csv.</p>']
    cards = sorted(load_cards(), key=sort_key)
    bucket = None
    for row in cards:
        b = rarity_bucket(row["Rarity"])
        if b != bucket:
            if bucket is not None:
                rows.append("</div>")
            bucket = b
            rows.append(f"<h2>{b}</h2><div class='grid'>")
        rows.append(_plain_card(row))
    rows.append("</div></main>")
    with open(path, "w") as f:
        f.write("\n".join(rows))
    print(f"wrote {path}")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "generate"
    if cmd == "generate":
        generate()
    elif cmd == "card":
        if len(sys.argv) < 3:
            raise SystemExit("usage: gen_beta_art.py card \"Card Name\"")
        generate_one(" ".join(sys.argv[2:]))
    elif cmd == "sheet":
        sheet(os.path.join(REPO, "beta-art-sheet.html"))
    elif cmd == "validate":
        validate()
        print("spec ok")
    else:
        raise SystemExit(f"unknown command: {cmd} "
                         f"(use generate, card <name>, sheet, or validate)")
