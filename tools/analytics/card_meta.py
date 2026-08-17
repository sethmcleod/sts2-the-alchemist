"""Card metadata for the dashboard, read straight from the card classes.

Every card class carries a [CardTheme(...)] attribute (AlchemistCode/Cards/CardTheme.cs) and passes
its rarity to the AlchemistCard constructor, so there is nothing to keep in sync by hand. The entry id
follows BaseLib: ALCHEMIST- plus the class name in upper snake case.

    python3 tools/analytics/card_meta.py          # print the table
"""

import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
CARDS = REPO / "AlchemistCode" / "Cards"
PREFIX = "ALCHEMIST-"

THEMES = ["Poison", "Infuse", "Potions", "Antitoxin", "Ferment", "Transform"]

CLASS_RE = re.compile(
    r"\[CardTheme\((?P<themes>[^)]*)\)\]\s*"
    r"public\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(?P<name>\w+)\b", re.S)
RARITY_RE = re.compile(r"base\([^;]*?CardRarity\.(?P<rarity>\w+)", re.S)


def snake(class_name: str) -> str:
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", class_name).upper()


def entry_for(class_name: str) -> str:
    return PREFIX + snake(class_name)


def display_name(class_name: str) -> str:
    name = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", class_name)
    return name.removesuffix(" Alchemist")  # StrikeAlchemist -> Strike


def card_meta() -> dict[str, dict]:
    """entry -> {name, rarity, themes[]} for every card class under AlchemistCode/Cards."""
    meta: dict[str, dict] = {}
    for path in sorted(CARDS.glob("*/*.cs")):
        src = path.read_text(encoding="utf-8")
        m = CLASS_RE.search(src)
        if not m:
            raise SystemExit(f"{path.relative_to(REPO)}: no [CardTheme] attribute before the class")
        r = RARITY_RE.search(src)
        if not r:
            raise SystemExit(f"{path.relative_to(REPO)}: no CardRarity in the base(...) call")
        themes = [t.strip().removeprefix("CardTheme.") for t in m["themes"].split(",") if t.strip()]
        themes = [t for t in themes if t != "None"]
        bad = [t for t in themes if t not in THEMES]
        if bad:
            raise SystemExit(f"{path.relative_to(REPO)}: unknown theme(s) {bad}")
        name = m["name"]
        meta[entry_for(name)] = {"name": display_name(name), "rarity": r["rarity"], "themes": themes}
    return meta


def main() -> int:
    meta = card_meta()
    json.dump(meta, sys.stdout, indent=1, sort_keys=True)
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
