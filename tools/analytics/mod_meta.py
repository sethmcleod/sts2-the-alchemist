"""What the dashboard needs to know about the mod, read from its own source files.

- Cards: rarity from the base(...) call, themes from the [CardTheme(...)] attribute
  (AlchemistCode/Cards/CardTheme.cs), and the type, cost, text and tags such as Multiplayer from
  cards.csv. The text gets the gold words back from the card's loc text.
- Names: the English titles in Alchemist/localization/eng/.
- Images: the card art and the relic, potion, power and badge icons, as paths in the repo. The
  page loads them from GitHub's raw file host (asset_base), so nothing is copied.
- Badges: the tier thresholds from the badge classes, and their titles from eng/badges.json.
- Epochs: how many there are, which is how many a player needs for the full card pool.

Nothing here is kept in sync by hand. An id follows BaseLib: the mod prefix plus the class name
in upper snake case.

    python3 tools/analytics/mod_meta.py       # print it all
"""

import csv
import json
import os
import re
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
CODE = REPO / "AlchemistCode"
ENG = REPO / "Alchemist" / "localization" / "eng"
IMAGES = REPO / "Alchemist" / "images"
PREFIX = "ALCHEMIST-"
CHARACTER_ICON = IMAGES / "charui" / "character_icon_alchemist.png"
ENERGY_ICON = IMAGES / "charui" / "text_energy.png"

# Every value of the CardTheme enum except None. A theme no card uses is left out of the export
THEMES = ["Poison", "Antitoxin", "Ferment", "Mix", "Transform", "Potions"]
# The game prints these keywords on a line of their own, so no loc text wraps them in [gold]. The
# mod's own keywords come from card_keywords.json
BASE_KEYWORDS = ["Exhaust", "Ethereal", "Innate", "Retain", "Unplayable"]

CLASS_RE = re.compile(
    r"\[CardTheme\((?P<themes>[^)]*)\)\]\s*"
    r"public\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(?P<name>\w+)\b", re.S)
NOT_A_CARD_RE = re.compile(r"(?:public|internal)\s+(?:abstract|static)\s+class\s+\w+")
RARITY_RE = re.compile(r"base\([^;]*?CardRarity\.(?P<rarity>\w+)", re.S)
BADGE_CLASS_RE = re.compile(r"class\s+(?P<name>\w+)\(\)\s*:\s*CustomBadge\((?P<args>[^)]*)\)")
TIER_RE = re.compile(r"const\s+int\s+(?P<tier>Bronze|Silver|Gold)\w*\s*=\s*(?P<value>\d+)")
SINGLE_TIER_RE = re.compile(r"const\s+int\s+Min\w*\s*=\s*(?P<value>\d+)")
BADGE_ICON_RE = re.compile(r'"(?P<file>[\w.-]+\.png)"\.BadgeImagePath\(\)')
MARKUP_RE = re.compile(r"\[/?\w+\]")
GOLD_RE = re.compile(r"\[gold\](.*?)\[/gold\]")
# "{Cards:plural:Mix+|Mixes+}" or "{IfUpgraded:show:a|b}": one form or the other
CHOICE_RE = re.compile(r"\{\w+:(?:plural|show):([^{}]*)\}")
UPGRADED_NUMBER_RE = re.compile(r"\((\d+)\)")


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


def card_sheet() -> dict[str, dict]:
    """Card title -> its cards.csv row."""
    with (REPO / "cards.csv").open(encoding="utf-8") as f:
        return {row["Card"]: row for row in csv.DictReader(f)}


def gold_words(loc_text: str) -> set[str]:
    """The words a loc string wraps in [gold], with both forms of a plural or an upgrade choice."""
    words = set()
    for span in GOLD_RE.findall(loc_text):
        choice = CHOICE_RE.fullmatch(span)
        words.update(w for w in (choice[1].split("|") if choice else [span]) if "{" not in w)
    return words


def card_text(text: str, gold: set[str]) -> str:
    """cards.csv text in the loc markup: [gold] words, the upgraded number in [green], and [energy]
    for the energy icon. A word also turns gold with a trailing +, as in Mix+."""
    if gold:
        words = "|".join(re.escape(w) for w in sorted(gold, key=len, reverse=True))
        text = re.sub(rf"(?<![\w+])(?:{words})\+?(?!\w)", lambda m: f"[gold]{m[0]}[/gold]", text)
    text = UPGRADED_NUMBER_RE.sub(r"[green](\1)[/green]", text)
    return re.sub(r"\bEnergy\b", "[energy]", text)


def repo_path(path: Path) -> str | None:
    return path.relative_to(REPO).as_posix() if path.exists() else None


def portrait(entry: str) -> str | None:
    """The card's art as the game picks it: the final art, else the beta placeholder."""
    file = entry.removeprefix(PREFIX).lower() + ".png"
    return repo_path(IMAGES / "card_portraits" / file) or repo_path(IMAGES / "card_portraits" / "beta" / file)


def icons() -> dict[str, str]:
    """entry -> the icon of every relic, potion and power the mod adds."""
    found = {}
    for folder in ("relics", "potions", "powers"):
        for entry in titles(folder):
            if path := repo_path(IMAGES / folder / (entry.removeprefix(PREFIX).lower() + ".png")):
                found[entry] = path
    return found


def git(*args: str) -> str | None:
    try:
        run = subprocess.run(["git", *args], cwd=REPO, capture_output=True, text=True, check=True)
        return run.stdout.strip() or None
    except (OSError, subprocess.CalledProcessError):
        return None


def asset_base() -> str | None:
    """The URL the image paths above are relative to: GitHub's raw file host at a commit GitHub has.
    CI names its own commit. A local run takes the upstream branch, because a local commit can be
    unpushed."""
    slug = os.environ.get("GITHUB_REPOSITORY")
    if not slug and (origin := git("remote", "get-url", "origin")):
        found = re.search(r"github\.com[:/]([^/]+/[^/]+?)(?:\.git)?$", origin)
        slug = found and found[1]
    ref = os.environ.get("GITHUB_SHA") or git("rev-parse", "@{upstream}")
    return f"https://raw.githubusercontent.com/{slug}/{ref}/" if slug and ref else None


def mod_version() -> str:
    """The version in the mod manifest, which is the version the card text is from."""
    return json.loads((REPO / "Alchemist.json").read_text(encoding="utf-8-sig"))["version"]


def cards() -> dict[str, dict]:
    """entry -> {name, rarity, themes, tags, type, cost, text, art} for every card class under
    AlchemistCode/Cards."""
    names, sheet = titles(), card_sheet()
    loc = json.loads((ENG / "cards.json").read_text(encoding="utf-8"))
    keywords = set(BASE_KEYWORDS) | set(titles("card_keywords").values())
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
        row = sheet.get(name, {})
        meta[entry] = {
            "name": name,
            "rarity": r["rarity"],
            "themes": themes,
            "tags": [t.strip() for t in row.get("Rarity", "").split(",")[1:]],
            "type": row.get("Type"),
            "cost": card_text(row["Cost"], set()) if row else None,
            "text": card_text(row["Description"], keywords | gold_words(loc.get(f"{entry}.description", "")))
            if row else None,
            "art": portrait(entry),
        }
    return meta


def badges() -> list[dict]:
    """One entry per badge class: its id, its icon, whether it needs a win or a co-op run, and its
    tiers (lowest first).

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
        icon = BADGE_ICON_RE.search(src)
        found.append({
            "id": entry,
            "icon": icon and repo_path(IMAGES / "badges" / icon["file"]),
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
    everything = {"cards": cards(), "badges": badges(), "names": titles(), "epochs": epoch_count(),
                  "icons": icons(), "asset_base": asset_base(), "version": mod_version()}
    json.dump(everything, sys.stdout, indent=1, sort_keys=True)
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
