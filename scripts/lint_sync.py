#!/usr/bin/env python3
"""Static three-way-rule linter. Offline, no game required.

Enforces that every card stays in sync across its three homes (CONTRIBUTING.md):
  1. code:  a card class under AlchemistCode/Cards/
  2. loc:   ALCHEMIST-<SNAKE>.title / .description in localization/eng/cards.json
  3. csv:   a row in cards.csv (the design sheet)

FAILs (exit 1) on structural drift: a csv row with no class, a class with no row,
a card missing loc keys, or a cost mismatch. Also does a conservative numeric
cross-check (WithDamage/WithBlock/WithEnergy/WithCards/WithPower literal builders vs
the csv's "N (M)" pairs) and prints those as warnings. Cards using formula builders
(WithCalculated*, computed args) are skipped rather than guessed at.

Separately checks the fourth home a rename has to reach: art on disk. Cards, powers,
relics and potions all resolve their icons from the class name, so renaming a class
without renaming its png leaves the entity with no art and the png orphaned. Missing
art FAILs, orphaned art warns.

Run via `scripts/dev.sh lint`.
"""

import csv
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
CODE = REPO / "AlchemistCode"
IMG = REPO / "Alchemist" / "images"
CSV = REPO / "cards.csv"
LOC = REPO / "Alchemist" / "localization" / "eng" / "cards.json"

# csv display name -> class name, for the two basics that carry an "Alchemist" suffix
SPECIAL_CLASS = {"Strike": "StrikeAlchemist", "Defend": "DefendAlchemist"}

# Every entity resolves its art from its own class name (see AlchemistCode/Extensions/
# StringExtensions.cs), so a rename that misses the images silently orphans them.
# entity label -> (code subdir, base marker, [(variant label, image dir, filename template)])
ASSET_SPECS = [
    # cards ship big portraits only: PortraitPath and BetaPortraitPath fall back to
    # card.png by design, so requiring them here would flag all 95 every run
    ("card", "Cards", "Card", [("portrait", "card_portraits/big", "{s}.png")]),
    ("power", "Powers", "Power", [("packed", "powers", "{s}.png"),
                                  ("big", "powers/big", "{s}.png")]),
    ("relic", "Relics", "Relic", [("packed", "relics", "{s}.png"),
                                  ("outline", "relics", "{s}_outline.png"),
                                  ("big", "relics/big", "{s}.png")]),
    ("potion", "Potions", "Potion", [("packed", "potions", "{s}.png"),
                                     ("outline", "potions/outlines", "{s}.png")]),
]

# generic art the *ImagePath helpers fall back to, claimed by no class
FALLBACK_ART = {"card.png", "power.png", "relic.png", "relic_outline.png", "potion.png"}


def norm(name: str) -> str:
    """Collapse a display or class name to a comparison key."""
    return re.sub(r"[^a-z0-9]", "", name.lower())


def snake(class_name: str) -> str:
    """Card class name -> loc-id suffix (DoubleDose -> DOUBLE_DOSE)."""
    s = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", class_name)
    return s.upper()


def load_cards_csv() -> list[dict]:
    rows = []
    with open(CSV, newline="") as f:
        for row in csv.DictReader(f):
            if row.get("Card", "").strip():
                rows.append(row)
    return rows


def entity_classes(subdir: str, base_marker: str) -> dict[str, Path]:
    """class name -> file, for every concrete class under AlchemistCode/<subdir>.

    base_marker is matched against the base list, so "Card" catches AlchemistCard and
    "Power" catches both AlchemistPower and CustomTemporaryStrengthPower subclasses.
    Abstract classes are skipped: they carry no model id, so they own no assets
    """
    out = {}
    for path in (CODE / subdir).rglob("*.cs"):
        for m in re.finditer(r"public\s+(abstract\s+)?class\s+(\w+)\s*:\s*([\w<>, ]+)", path.read_text()):
            is_abstract, name, bases = m.groups()
            if not is_abstract and base_marker in bases:
                out[name] = path
    return out


def card_classes() -> dict[str, Path]:
    """class name -> file, for every concrete AlchemistCard subclass."""
    return entity_classes("Cards", "Card")


def asset_name(class_name: str) -> str:
    """Entity class name -> icon filename stem (GoldenTouchPower -> golden_touch_power)."""
    return snake(class_name).lower()


def check_assets() -> tuple[list[str], list[str], int]:
    """Every concrete entity has its art on disk, and no art is left unclaimed.

    Returns (errors, warnings, files checked). A missing file is an error: the
    *ImagePath helpers log and fall back to generic art, so the entity still renders
    and the drift is easy to miss. An unclaimed file is only a warning, since it costs
    pck size but nothing else
    """
    errors, warnings = [], []
    claimed: set[Path] = set()

    for label, subdir, marker, variants in ASSET_SPECS:
        for cls in sorted(entity_classes(subdir, marker)):
            for variant, img_dir, template in variants:
                path = IMG / img_dir / template.format(s=asset_name(cls))
                claimed.add(path)
                if not path.exists():
                    errors.append(f"{label} {cls}: missing {variant} art {img_dir}/{path.name}")

    # dedupe: relics keep packed and outline art in one directory
    art_dirs = {img_dir for _, _, _, variants in ASSET_SPECS for _, img_dir, _ in variants}
    for img_dir in sorted(art_dirs):
        for path in sorted((IMG / img_dir).glob("*.png")):
            if path not in claimed and path.name not in FALLBACK_ART:
                warnings.append(f"{img_dir}/{path.name}: no class claims this art")

    return errors, warnings, len(claimed)


def parse_number_pairs(desc: str) -> list[tuple[int, int]]:
    """Extract 'N (M)' upgrade pairs from a csv description/cost cell.

    The trailing % is optional so percentage cards ('25% (50%)') pair up like the rest."""
    return [(int(a), int(b)) for a, b in re.findall(r"(\d+)%?\s*\((\d+)%?\)", desc)]


BUILDER = re.compile(
    r"With(?:Damage|Block|Energy|Cards|Power<\w+>|Var\(\s*\"[^\"]+\")\s*"
    r"(?:\([^)]*?|,)\s*(\d+)\s*,\s*(-?\d+)\s*\)")


def parse_builders(text: str) -> list[tuple[int, int]]:
    """Literal (base, delta) builder pairs. Skips cards that compute values.

    A base of 0 is a dynamic placeholder, not a literal amount: the shown value
    comes from a dynamic var or runtime computation (e.g. Albedo's "that much
    Regen", where WithPower<RegenPower>(0, 1) only declares the +1 upgrade tip).
    The csv renders those as "(+ N)", not a literal "0 (N)" pair, so skip them.
    """
    if "WithCalculated" in text:
        return []  # formula damage/block: csv shows a live number, not base(+delta)
    pairs = []
    for m in re.finditer(r"With(?:Damage|Block|Energy|Cards|Power<\w+>)\((\d+)\s*,\s*(-?\d+)\)", text):
        base, delta = int(m.group(1)), int(m.group(2))
        if base != 0:
            pairs.append((base, base + delta))
    for m in re.finditer(r"WithVar\(\s*\"[^\"]+\"\s*,\s*(\d+)\s*,\s*(-?\d+)\)", text):
        base, delta = int(m.group(1)), int(m.group(2))
        if base != 0:
            pairs.append((base, base + delta))
    return pairs


def main() -> int:
    rows = load_cards_csv()
    classes = card_classes()
    loc = LOC.read_text()

    errors: list[str] = []
    warnings: list[str] = []

    csv_by_norm = {norm(SPECIAL_CLASS.get(r["Card"], r["Card"])): r for r in rows}
    class_by_norm = {norm(c): c for c in classes}

    # 1. csv row <-> class file
    for r in rows:
        display = r["Card"]
        expected = SPECIAL_CLASS.get(display, display.replace(" ", "").replace("'", ""))
        if norm(expected) not in class_by_norm:
            errors.append(f"csv row '{display}': no matching card class (expected {expected}.cs)")
    for cls in classes:
        if norm(cls) not in csv_by_norm:
            errors.append(f"class {cls}: no matching row in cards.csv")

    # 2. loc keys per class
    for cls in classes:
        key = f"ALCHEMIST-{snake(cls)}"
        for suffix in (".title", ".description"):
            if f'"{key}{suffix}"' not in loc:
                errors.append(f"class {cls}: missing loc key {key}{suffix} in cards.json")

    # 3. cost + numeric cross-check
    for r in rows:
        display = r["Card"]
        cls = SPECIAL_CLASS.get(display, display.replace(" ", "").replace("'", ""))
        path = classes.get(cls) or classes.get(class_by_norm.get(norm(cls), ""))
        if not path:
            continue
        text = path.read_text()

        # cost: csv "N" or "N (M)" vs ctor base(cost,...) [+ WithCostUpgradeBy]
        cost = r["Cost"].strip()
        cm = re.search(r":\s*base\(\s*(\d+)\s*,", text)
        if cm and cost.isdigit() and int(cost) != int(cm.group(1)):
            errors.append(f"{display}: csv cost {cost} != ctor base cost {cm.group(1)}")

        # numeric pairs: every literal builder pair should show up in the csv text
        csv_pairs = set(parse_number_pairs(r["Description"]) + parse_number_pairs(cost))
        for base, up in parse_builders(text):
            if base != up and (base, up) not in csv_pairs:
                warnings.append(
                    f"{display}: builder produces {base} ({up}) but that pair isn't in the csv row")

    # 4. every entity's art is on disk
    asset_errors, asset_warnings, art_count = check_assets()
    errors += asset_errors
    warnings += asset_warnings

    for w in warnings:
        print(f"\033[33mwarn\033[0m  {w}")
    for e in errors:
        print(f"\033[31mFAIL\033[0m  {e}")
    print(f"\n{len(rows)} cards, {len(classes)} classes, {art_count} art files: "
          f"{len(errors)} error(s), {len(warnings)} warning(s)")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
