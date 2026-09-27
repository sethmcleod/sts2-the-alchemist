"""Cuts the fonts the game draws cards with down to what the website's card faces use.

The game swaps Kreon for another font in some languages (FontManager and FontPathSets): Fira Sans
Extra Condensed for Polish and Russian, Noto Sans CJK for Japanese, Gyeonggi Batang for Korean,
CS ChatThai UI for Thai, and Source Han Serif (titles) with Noto Sans Mono CJK (text) for Chinese.
Most of those fonts also scale their glyphs (a FontVariation transform), which the site copies.

Each font is subset to the characters the mod's cards, keywords and card types use in its
languages and saved as WOFF2 in site/src/assets/fonts/. site/src/lib/card-fonts.json gets what the
build-time text fit needs: each set's languages, line height, scale and character widths.

Run it again after a localization pass; `npm test` in site/ fails while a card uses a character a
subset lacks. The game files are not in the repo: point --game at a project recovered with GDRE
Tools. Needs fontTools with brotli (pip install fonttools brotli).

    python3 tools/analytics/card_fonts.py --game <recovered project>
"""

import argparse
import io
import json
import re
import sys
from pathlib import Path

from fontTools import subset
from fontTools.ttLib import TTFont

REPO = Path(__file__).resolve().parents[2]
LOC = REPO / "Alchemist" / "localization"
GAME_LOC = Path(__file__).with_name("game_loc.json")
OUT = REPO / "site" / "src" / "assets" / "fonts"
TABLE = REPO / "site" / "src" / "lib" / "card-fonts.json"

# Numbers, the upgrade mark and what the site adds around a card's words
ALWAYS = "0123456789+-X?()%/.,:;!' "
# Kreon also sets the site's headings, so its subset keeps all of ASCII and Latin-1 and the
# common punctuation
HEADINGS = "".join(map(chr, [*range(0x20, 0x7F), *range(0xA0, 0x100)])) + \
    "\u2013\u2014\u2018\u2019\u201a\u201c\u201d\u201e\u2020\u2021\u2022\u2026\u2039\u203a\u2212"
# set -> the languages that use it, and per weight the font file and its FontVariation scale.
# Titles and costs use FontType.Bold, card text FontType.Regular.
SETS = {
    "latin": {"langs": ["eng", "deu", "esp", "fra", "ind", "ita", "ptb", "spa", "tur"], "extra": HEADINGS,
              "regular": ("fonts/kreon_regular.ttf", 1.0), "bold": ("fonts/kreon_bold.ttf", 1.0)},
    "fira": {"langs": ["pol", "rus"],
             "regular": ("fonts/rus/FiraSansExtraCondensed-Regular.ttf", 0.95),
             "bold": ("fonts/rus/FiraSansExtraCondensed-Bold.ttf", 1.0)},
    "ja": {"langs": ["jpn"],
           "regular": ("fonts/jpn/NotoSansCJKjp-Regular.otf", 0.95),
           "bold": ("fonts/jpn/NotoSansCJKjp-Bold.otf", 0.95)},
    "ko": {"langs": ["kor"],
           "regular": ("fonts/kor/GyeonggiCheonnyeonBatangBold.ttf", 0.96),
           "bold": ("fonts/kor/GyeonggiCheonnyeonBatangBold.ttf", 0.96), "spacing": 4},
    "th": {"langs": ["tha"],
           "regular": ("fonts/tha/CSChatThaiUI.ttf", 0.95), "bold": ("fonts/tha/CSChatThaiUI.ttf", 0.95), "spacing": 5},
    "zh-hans": {"langs": ["zhs"],
                "regular": ("fonts/zhs/NotoSansMonoCJKsc-Regular.otf", 0.95),
                "bold": ("fonts/zhs/SourceHanSerifSC-Bold.otf", 0.95)},
    "zh-hant": {"langs": ["zht"],
                "regular": ("fonts/zht/NotoSansMonoCJKtc-Regular.otf", 0.95),
                "bold": ("fonts/zht/SourceHanSerifTC-Bold.otf", 0.95)},
}
# The OFL bars a changed font from using a Reserved Font Name, and a subset is a changed font
RESERVED = ("Source", "Fira")
# Tags go; a field's branches stay, since the words in {Cards:plural:card|cards} show on the card
TAG_RE = re.compile(r"\[/?[^\]]*\]")


def words(lang: str, game: dict) -> str:
    """Every character of the mod's card, relic, potion, power and keyword text in one language,
    and the game's names for the keywords and card types."""
    found = set(ALWAYS)
    for table in ("cards", "relics", "potions", "powers", "card_keywords"):
        path = LOC / lang / f"{table}.json"
        if path.exists():
            for value in json.loads(path.read_text(encoding="utf-8")).values():
                found.update(TAG_RE.sub("", value))
    for group in ("keywords", "types", "rarities"):
        for value in game[lang][group].values():
            found.update(value)
    found.update(game[lang]["period"])
    return "".join(sorted(found - set("\n{}|")))


def rename(font: TTFont, name: str) -> None:
    """Give a subset of a font with a Reserved Font Name a name of its own."""
    table = font["name"]
    for record in table.names:
        if record.nameID in (1, 3, 4, 6, 16, 17, 18, 21, 22):
            text = record.toUnicode()
            if any(reserved in text for reserved in RESERVED):
                style = "Bold" if "Bold" in text else "Regular"
                record.string = f"{name}-{style}" if record.nameID in (3, 6) else (
                    name if record.nameID in (1, 16, 21) else f"{name} {style}")


def cut(source: Path, text: str, name: str) -> tuple[bytes, TTFont]:
    font = TTFont(source)
    options = subset.Options()
    options.flavor = "woff2"
    options.name_IDs = ["*"]
    options.name_languages = ["*"]
    options.notdef_outline = True
    # Hinting is most of a small subset's weight, and browsers draw these sizes well without it
    options.hinting = False
    options.desubroutinize = True
    subsetter = subset.Subsetter(options)
    subsetter.populate(text=text)
    subsetter.subset(font)
    rename(font, name)
    buffer = io.BytesIO()
    subset.save_font(font, buffer, options)
    return buffer.getvalue(), font


def widths(font: TTFont, text: str) -> dict[str, float]:
    """Each character's advance in em, before the FontVariation scale."""
    cmap, advances, units = font.getBestCmap(), font["hmtx"].metrics, font["head"].unitsPerEm
    return {ch: round(advances[cmap[ord(ch)]][0] / units, 4) for ch in text if ord(ch) in cmap}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--game", type=Path, required=True, help="a project recovered with GDRE Tools")
    args = parser.parse_args()
    game_dir = args.game.expanduser()
    game = json.loads(GAME_LOC.read_text(encoding="utf-8"))
    OUT.mkdir(parents=True, exist_ok=True)
    table = {}
    for name, spec in SETS.items():
        text = "".join(sorted(set("".join(words(lang, game) for lang in spec["langs"]) + spec.get("extra", ""))))
        entry = {"langs": spec["langs"], "spacing": spec.get("spacing", 0)}
        for weight in ("regular", "bold"):
            file, scale = spec[weight]
            # Korean and Thai draw everything in one font, so one file serves both weights
            if weight == "bold" and spec["bold"] == spec["regular"]:
                entry["bold"] = entry["regular"]
                continue
            source = game_dir / file
            if not source.exists():
                raise SystemExit(f"{source} is missing: point --game at the recovered project")
            data, font = cut(source, text, f"Alchemist Card {name.title()}")
            out = OUT / f"card-{name}-{weight}.woff2"
            out.write_bytes(data)
            missing = [ch for ch in text if ord(ch) not in font.getBestCmap()]
            if missing:
                print(f"{out.name}: the font has no {''.join(missing)!r}")
            entry[weight] = {"file": out.name, "scale": scale}
            if weight == "regular":
                hhea = font["hhea"]
                entry["line"] = round((hhea.ascent - hhea.descent) / font["head"].unitsPerEm, 4)
                entry["widths"] = widths(font, text)
            print(f"{out.name}: {len(text)} characters, {len(data) // 1024} KB")
        table[name] = entry
    TABLE.write_text(json.dumps(table, ensure_ascii=False, indent=1, sort_keys=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
