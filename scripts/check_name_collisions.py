#!/usr/bin/env python3
"""Check that mod content is not named the same as base-game content.

A translation can be internally consistent and still collide with the game it
sits inside: French renders the mod's Laced as "Imprégné" while the base game
already calls its Imbued enchantment "Imprégnation", so a player meets two
enchantments that read the same. The glossary cannot catch this, because these
are the mod's own terms, and the term checker cannot either, because it only
compares the mod against itself.

Exact matches are reported as collisions. Same-root matches are reported
separately, since sharing a root is sometimes fine and sometimes not.

Usage:
    python3 scripts/check_name_collisions.py --base <extracted>/localization [lang ...]
"""
import argparse
import json
import sys
import unicodedata
from pathlib import Path

LOC = Path(__file__).resolve().parent.parent / "Alchemist" / "localization"

# Only compare like with like: a card sharing a name with an enchantment is far
# less confusing than two enchantments sharing one.
CATEGORIES = ["enchantments.json", "cards.json", "relics.json", "potions.json", "powers.json"]

ROOT = 6

# Every character has a Strike and a Defend, so those share a name by design.
EXPECTED = {"ALCHEMIST-STRIKE_ALCHEMIST.title", "ALCHEMIST-DEFEND_ALCHEMIST.title"}


def norm(text: str) -> str:
    decomposed = unicodedata.normalize("NFD", text).casefold()
    return "".join(c for c in decomposed if not unicodedata.combining(c)).strip()


def root(text: str) -> str:
    folded = norm(text)
    return folded[:ROOT] if len(folded) > ROOT else folded


def titles(path: Path) -> dict[str, str]:
    if not path.exists():
        return {}
    try:
        table = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, UnicodeDecodeError):
        return {}
    return {
        k: v for k, v in table.items()
        if k.endswith(".title") and isinstance(v, str) and v.strip()
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("langs", nargs="*")
    parser.add_argument("--base", required=True, type=Path)
    parser.add_argument("--roots", action="store_true", help="also report shared roots")
    args = parser.parse_args()

    langs = args.langs or sorted(
        d.name for d in LOC.iterdir() if d.is_dir() and d.name != "eng"
    )

    failed = False
    for lang in langs:
        exact, near = [], []
        for category in CATEGORIES:
            mod = titles(LOC / lang / category)
            base = titles(args.base / lang / category)
            # A mod key can share a name with its own power; only base-game
            # names count as foreign, so compare across the two sources.
            base_by_name = {}
            base_by_root = {}
            for key, value in base.items():
                base_by_name.setdefault(norm(value), []).append((key, value))
                base_by_root.setdefault(root(value), []).append((key, value))
            for key, value in mod.items():
                if key in EXPECTED:
                    continue
                for bkey, bvalue in base_by_name.get(norm(value), []):
                    exact.append(f"  {category} {key} = {value!r} collides with base {bkey}")
                if args.roots and norm(value) not in base_by_name:
                    for bkey, bvalue in base_by_root.get(root(value), []):
                        near.append(f"  {category} {key} = {value!r} shares a root with base {bkey} = {bvalue!r}")
        if exact:
            failed = True
            print(f"== {lang}: {len(exact)} name collision(s)")
            print("\n".join(exact))
        elif near:
            print(f"== {lang}: OK ({len(near)} shared root(s))")
        else:
            print(f"== {lang}: OK")
        if near:
            print("\n".join(near))
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
