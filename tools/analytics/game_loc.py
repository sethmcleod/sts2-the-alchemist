"""The base game's own words that the website and the card text need in every language: keyword
names, card types, rarities, encounter names, a few labels, and the definitions the game shows
beside a card for the terms its text marks in gold. They are read from a copy of the
game's res://localization/ and written to game_loc.json next to this file, which is committed,
because a build machine has no copy of the game.

Run it again after a game update, or when the site needs another word:

    python3 tools/analytics/game_loc.py --base <game localization folder>
"""

import argparse
import json
import sys
from pathlib import Path

OUT = Path(__file__).with_name("game_loc.json")

# group -> table -> {our name: the game's key}
WORDS = {
    "keywords": {"card_keywords": {k.capitalize(): f"{k}.title" for k in
                                   ("ETERNAL", "ETHEREAL", "EXHAUST", "INNATE", "RETAIN", "SLY", "UNPLAYABLE")}},
    "types": {"gameplay_ui": {t.capitalize(): f"CARD_TYPE.{t}" for t in
                              ("ATTACK", "SKILL", "POWER", "STATUS", "CURSE")}},
    # The card library's names, where Basic reads "Starter"
    "rarities": {
        "main_menu_ui": {r.capitalize(): f"CARD_LIBRARY_RARITY_{r}" for r in
                         ("BASIC", "COMMON", "UNCOMMON", "RARE", "ANCIENT")},
        "gameplay_ui": {"Event": "CARD_RARITY.EVENT", "Token": "CARD_RARITY.TOKEN"},
    },
    "potion_rarities": {"gameplay_ui": {r.capitalize(): f"POTION_RARITY.{r}" for r in
                                        ("COMMON", "UNCOMMON", "RARE", "EVENT")}},
    "relic_rarities": {"gameplay_ui": {r.capitalize(): f"RELIC_RARITY.{r}" for r in
                                       ("STARTER", "COMMON", "UNCOMMON", "RARE", "SHOP", "EVENT", "ANCIENT")}},
    "words": {
        "static_hover_tips": {"Boss": "ROOM_BOSS.title", "Elite": "ROOM_ELITE.title", "Floor": "FLOOR.title",
                              "Energy": "ENERGY.title", "Block": "BLOCK.title"},
        "main_menu_ui": {"Compendium": "COMPENDIUM", "Card Library": "COMPENDIUM_CARD_LIBRARY.title",
                         "Relic Collection": "COMPENDIUM_RELIC_COLLECTION.title",
                         "Potion Lab": "COMPENDIUM_POTION_LAB.title", "Run History": "RUN_HISTORY.title",
                         "Timeline": "TIMELINE", "Rarity": "CARD_LIBRARY_RARITY"},
        "settings_ui": {"Language": "LANGUAGE"},
        "gameplay_ui": {"Cost": "SORT_COST"},
        "game_over_screen": {"Victory": "BANNER.trueWin", "Defeat": "BANNER.lose2"},
        "powers": {"Poison": "POISON_POWER.title", "Weak": "WEAK_POWER.title", "Vulnerable": "VULNERABLE_POWER.title",
                   "Strength": "STRENGTH_POWER.title", "Dexterity": "DEXTERITY_POWER.title"},
    },
}


# table -> the entries whose title and description the site shows as a card's hover tips
TIPS = {
    "card_keywords": ("ETERNAL", "ETHEREAL", "EXHAUST", "INNATE", "RETAIN", "SLY", "UNPLAYABLE"),
    "powers": tuple(f"{p}_POWER" for p in ("POISON", "WEAK", "VULNERABLE", "STRENGTH", "DEXTERITY", "ARTIFACT",
                                           "PLATING", "FRAIL", "THORNS", "INTANGIBLE", "TAINTED")),
    "static_hover_tips": ("BLOCK", "TRANSFORM", "FATAL", "REPLAY_STATIC"),
}


def read(base: Path, lang: str, table: str) -> dict[str, str]:
    path = base / lang / f"{table}.json"
    if not path.exists():
        raise SystemExit(f"{path} is missing: point --base at the game's localization folder")
    return json.loads(path.read_text(encoding="utf-8"))


def language(base: Path, lang: str) -> dict:
    """The words in one language. A key the language lacks falls back to English, as in the game."""
    def table(name: str) -> dict[str, str]:
        return {**read(base, "eng", name), **read(base, lang, name)}

    found: dict = {}
    for group, tables in WORDS.items():
        found[group] = {}
        for name, keys in tables.items():
            loc = table(name)
            for word, key in keys.items():
                if key not in loc:
                    raise SystemExit(f"eng/{name}.json has no {key}")
                found[group][word] = loc[key]
    found["period"] = table("card_keywords")["PERIOD"]
    found["tips"] = {}
    for name, keys in TIPS.items():
        loc = table(name)
        for key in keys:
            if f"{key}.description" not in loc:
                raise SystemExit(f"eng/{name}.json has no {key}.description")
            found["tips"][key] = {"title": loc[f"{key}.title"], "text": loc[f"{key}.description"]}
    found["encounters"] = {key.removesuffix(".title"): value
                           for key, value in sorted(table("encounters").items()) if key.endswith(".title")}
    return found


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--base", type=Path, required=True, help="the game's localization folder (eng, deu, ...)")
    args = parser.parse_args()
    base = args.base.expanduser()
    langs = sorted(p.name for p in base.iterdir() if (p / "card_keywords.json").exists())
    if "eng" not in langs:
        raise SystemExit(f"{base} has no eng folder")
    OUT.write_text(json.dumps({lang: language(base, lang) for lang in langs}, ensure_ascii=False, indent=1,
                              sort_keys=True) + "\n", encoding="utf-8")
    print(f"{OUT.name}: {len(langs)} languages")
    return 0


if __name__ == "__main__":
    sys.exit(main())
