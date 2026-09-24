"""What the dashboard needs to know about the mod, read from its own source files.

- Cards: rarity from the base(...) call, themes from the [CardTheme(...)] attribute
  (AlchemistCode/Cards/CardTheme.cs), and tags such as Multiplayer from cards.csv.
- Names: the English titles in Alchemist/localization/eng/.
- Badges: the tier thresholds from the badge classes, and their titles from eng/badges.json.
- Epochs: how many there are, which is how many a player needs for the full card pool.

Nothing here is kept in sync by hand. An id follows BaseLib: the mod prefix plus the class name
in upper snake case.

    python3 tools/analytics/mod_meta.py       # print it all
"""

import csv
import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
CODE = REPO / "AlchemistCode"
ENG = REPO / "Alchemist" / "localization" / "eng"
PREFIX = "ALCHEMIST-"

# Every value of the CardTheme enum except None. A theme no card uses is left out of the export
THEMES = ["Poison", "Antitoxin", "Ferment", "Mix", "Transform", "Potions"]

CLASS_RE = re.compile(
    r"\[CardTheme\((?P<themes>[^)]*)\)\]\s*"
    r"public\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(?P<name>\w+)\b", re.S)
NOT_A_CARD_RE = re.compile(r"(?:public|internal)\s+(?:abstract|static)\s+class\s+\w+")
RARITY_RE = re.compile(r"base\([^;]*?CardRarity\.(?P<rarity>\w+)", re.S)
BADGE_CLASS_RE = re.compile(r"class\s+(?P<name>\w+)\(\)\s*:\s*CustomBadge\((?P<args>[^)]*)\)")
TIER_RE = re.compile(r"const\s+int\s+(?P<tier>Bronze|Silver|Gold)\w*\s*=\s*(?P<value>\d+)")
SINGLE_TIER_RE = re.compile(r"const\s+int\s+Min\w*\s*=\s*(?P<value>\d+)")
MARKUP_RE = re.compile(r"\[/?\w+\]")


def snake(class_name: str) -> str:
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", class_name).upper()


def entry_for(class_name: str) -> str:
    return PREFIX + snake(class_name)


def plain(text: str) -> str:
    return MARKUP_RE.sub("", text)


def titles(*tables: str) -> dict[str, str]:
    """entry -> English title for everything the mod adds, or only for the named loc tables
    ("relics", "potions", ...)."""
    names = {}
    paths = [ENG / f"{table}.json" for table in tables] if tables else sorted(ENG.glob("*.json"))
    for path in paths:
        for key, value in json.loads(path.read_text(encoding="utf-8")).items():
            if key.endswith(".title") and key.startswith(PREFIX):
                names[key.removesuffix(".title")] = plain(value)
    return names


def card_tags() -> dict[str, list[str]]:
    """Card title -> the tags after its rarity in cards.csv ("Uncommon, Multiplayer" -> ["Multiplayer"])."""
    with (REPO / "cards.csv").open(encoding="utf-8") as f:
        return {row["Card"]: [t.strip() for t in row["Rarity"].split(",")[1:]] for row in csv.DictReader(f)}


def cards() -> dict[str, dict]:
    """entry -> {name, rarity, themes, tags} for every card class under AlchemistCode/Cards."""
    names, tags = titles(), card_tags()
    meta: dict[str, dict] = {}
    for path in sorted((CODE / "Cards").glob("*/*.cs")):
        src = path.read_text(encoding="utf-8")
        m = CLASS_RE.search(src)
        if not m and NOT_A_CARD_RE.search(src):
            continue  # a shared base class or a helper, not a card
        if not m:
            raise SystemExit(f"{path.relative_to(REPO)}: no [CardTheme] attribute before the class")
        r = RARITY_RE.search(src)
        if not r:
            raise SystemExit(f"{path.relative_to(REPO)}: no CardRarity in the base(...) call")
        themes = [t.strip().removeprefix("CardTheme.") for t in m["themes"].split(",") if t.strip()]
        themes = [t for t in themes if t != "None"]
        if bad := [t for t in themes if t not in THEMES]:
            raise SystemExit(f"{path.relative_to(REPO)}: unknown theme(s) {bad}")
        entry = entry_for(m["name"])
        name = names.get(entry, m["name"])
        meta[entry] = {"name": name, "rarity": r["rarity"], "themes": themes, "tags": tags.get(name, [])}
    return meta


def badges() -> list[dict]:
    """One entry per badge class: its id, whether it needs a win or a co-op run, and its tiers
    (lowest first).

    A tiered badge declares Bronze, Silver and Gold constants. A single-tier badge declares one
    Min constant and counts as Bronze.
    """
    loc = json.loads((ENG / "badges.json").read_text(encoding="utf-8"))
    found = []
    for path in sorted((CODE / "Badges").glob("*.cs")):
        src = path.read_text(encoding="utf-8")
        m = BADGE_CLASS_RE.search(src)
        if not m:
            continue  # a counter or a helper
        entry = entry_for(m["name"])
        tiers = [(t["tier"].lower(), int(t["value"])) for t in TIER_RE.finditer(src)]
        if not tiers and (single := SINGLE_TIER_RE.search(src)):
            tiers = [("bronze", int(single["value"]))]
        if not tiers:
            raise SystemExit(f"{path.relative_to(REPO)}: no Bronze/Silver/Gold or Min threshold")
        found.append({
            "id": entry,
            "needs_win": "requiresWin: true" in m["args"],
            "coop_only": "multiplayerOnly: true" in m["args"],
            "tiers": [{
                "tier": tier,
                "at": value,
                "title": plain(loc.get(f"{entry}.{tier}Title") or loc.get(f"{entry}.title", "")),
                "text": plain(loc.get(f"{entry}.{tier}Description") or loc.get(f"{entry}.description", "")),
            } for tier, value in tiers],
        })
    return found


def epoch_count() -> int:
    """How many epochs unlock the full card pool: the length of EpochRegistration.AlchemistEpochTypes."""
    src = (CODE / "Epochs" / "EpochRegistration.cs").read_text(encoding="utf-8")
    block = re.search(r"AlchemistEpochTypes\s*=\s*\{(?P<body>[^}]*)\}", src)
    if not block:
        raise SystemExit("AlchemistCode/Epochs/EpochRegistration.cs: no AlchemistEpochTypes list")
    return len(re.findall(r"typeof\(", block["body"]))


def main() -> int:
    everything = {"cards": cards(), "badges": badges(), "names": titles(), "epochs": epoch_count()}
    json.dump(everything, sys.stdout, indent=1, sort_keys=True)
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
