#!/usr/bin/env python3
"""Static linter for the three-way rule.

Each card must stay in sync in its three locations (CONTRIBUTING.md):
  1. code:  a card class under AlchemistCode/Cards/
  2. loc:   ALCHEMIST-<SNAKE>.title / .description in localization/eng/cards.json
  3. csv:   a row in cards.csv (the design sheet)

The linter FAILs (exit 1) on a structural difference: a csv row with no class, a class
with no row, a card with no loc keys, or a cost that does not agree. It also makes a
careful numeric comparison: the literal WithDamage/WithBlock/WithEnergy/WithCards/WithPower
builders against the "N (M)" pairs in the csv. It prints each difference as a warning. It
does not examine a card with a formula builder (WithCalculated*, calculated arguments),
because it cannot know the correct value.

It also checks the fourth location that a rename must reach: the art on disk. Cards,
powers, relics and potions all get their icons from the class name. If you rename a class
but you do not rename its png, the entity has no art and no class uses the png. Art that
is missing is a FAIL. Art that no class uses is a warning.

Run it with `scripts/dev.sh lint`.
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

# csv display name -> class name, for the two basic cards with an "Alchemist" suffix
SPECIAL_CLASS = {"Strike": "StrikeAlchemist", "Defend": "DefendAlchemist"}

# Every entity gets its art from its own class name (see AlchemistCode/Extensions/
# StringExtensions.cs). If a rename does not include the images, no class uses them.
# entity label -> (code subdir, base marker, [(variant label, image dir, filename template)])
ASSET_SPECS = [
    # cards use the base game layout: the real portrait is card_portraits/<s>.png and the beta placeholder
    # is card_portraits/beta/<s>.png (see CardImageOrBetaPath). check_assets accepts either. card.png is the
    # generic fallback, so it is exempt below
    ("card", "Cards", "Card", [("portrait", "card_portraits", "{s}.png")]),
    ("power", "Powers", "Power", [("packed", "powers", "{s}.png"),
                                  ("big", "powers/big", "{s}.png")]),
    ("relic", "Relics", "Relic", [("packed", "relics", "{s}.png"),
                                  ("outline", "relics", "{s}_outline.png"),
                                  ("big", "relics/big", "{s}.png")]),
    ("potion", "Potions", "Potion", [("packed", "potions", "{s}.png"),
                                     ("outline", "potions/outlines", "{s}.png")]),
]

# the default art for the *ImagePath helpers; no class uses it
FALLBACK_ART = {"card.png", "power.png", "relic.png", "relic_outline.png", "potion.png"}


def norm(name: str) -> str:
    """Make a comparison key from a display name or a class name."""
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

    The function compares base_marker with the base list. Thus "Card" matches AlchemistCard,
    and "Power" matches both the AlchemistPower and CustomTemporaryStrengthPower subclasses.
    The function ignores an abstract class: it has no model id, so it has no assets
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
    """Each concrete entity has its art on disk, and every art file belongs to a class.

    It returns (errors, warnings, files checked). A file that is missing is an error. The
    *ImagePath helpers write a log line and use the default art, so the entity still renders
    and you can easily miss the difference. A file that no class uses is only a warning: it
    adds to the pck size, but it has no other effect
    """
    errors, warnings = [], []
    claimed: set[Path] = set()

    for label, subdir, marker, variants in ASSET_SPECS:
        for cls in sorted(entity_classes(subdir, marker)):
            for variant, img_dir, template in variants:
                path = IMG / img_dir / template.format(s=asset_name(cls))
                claimed.add(path)
                # A card portrait is present as the real art in big/ or the beta placeholder in beta/
                beta = None
                if label == "card":
                    beta = IMG / "card_portraits" / "beta" / template.format(s=asset_name(cls))
                    claimed.add(beta)
                if not path.exists() and not (beta and beta.exists()):
                    errors.append(f"{label} {cls}: the {variant} art {img_dir}/{path.name} is missing")

    # remove the duplicates: relics keep the packed art and the outline art in one directory. Also scan the
    # card beta placeholder folder, so an orphaned beta png (no matching card) is reported
    art_dirs = {img_dir for _, _, _, variants in ASSET_SPECS for _, img_dir, _ in variants}
    art_dirs.add("card_portraits/beta")
    for img_dir in sorted(art_dirs):
        for path in sorted((IMG / img_dir).glob("*.png")):
            if path not in claimed and path.name not in FALLBACK_ART:
                warnings.append(f"{img_dir}/{path.name}: no class uses this art")

    return errors, warnings, len(claimed)


def parse_number_pairs(desc: str) -> list[tuple[int, int]]:
    """Get the 'N (M)' upgrade pairs from a csv description cell or cost cell.

    The % at the end is optional, so a percentage card ('25% (50%)') makes a pair like the others."""
    return [(int(a), int(b)) for a, b in re.findall(r"(\d+)%?\s*\((\d+)%?\)", desc)]


BUILDER = re.compile(
    r"With(?:Damage|Block|Energy|Cards|Power<\w+>|Var\(\s*\"[^\"]+\")\s*"
    r"(?:\([^)]*?|,)\s*(\d+)\s*,\s*(-?\d+)\s*\)")


def parse_builders(text: str) -> list[tuple[int, int]]:
    """The literal (base, delta) builder pairs. It ignores a card that calculates its values.

    A base of 0 is a dynamic placeholder, not a literal amount. The value on screen comes
    from a dynamic var or from a calculation at run time. For example, Albedo has "that much
    Regen", and its WithPower<RegenPower>(0, 1) declares only the +1 upgrade tip.
    The csv shows these as "(+ N)", not as a literal "0 (N)" pair, so ignore them.
    """
    if "WithCalculated" in text:
        return []  # formula damage or block: the csv shows a calculated number, not base(+delta)
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
            errors.append(f"csv row '{display}': no card class matches it ({expected}.cs must exist)")
    for cls in classes:
        if norm(cls) not in csv_by_norm:
            errors.append(f"class {cls}: no row in cards.csv matches it")

    # 2. loc keys per class
    for cls in classes:
        key = f"ALCHEMIST-{snake(cls)}"
        for suffix in (".title", ".description"):
            if f'"{key}{suffix}"' not in loc:
                errors.append(f"class {cls}: the loc key {key}{suffix} is missing from cards.json")

    # 3. cost and numeric comparison
    for r in rows:
        display = r["Card"]
        cls = SPECIAL_CLASS.get(display, display.replace(" ", "").replace("'", ""))
        path = classes.get(cls) or classes.get(class_by_norm.get(norm(cls), ""))
        if not path:
            continue
        text = path.read_text()

        # cost: csv "N" or "N (M)" against ctor base(cost,...) [+ WithCostUpgradeBy]
        cost = r["Cost"].strip()
        cm = re.search(r":\s*base\(\s*(\d+)\s*,", text)
        if cm and cost.isdigit() and int(cost) != int(cm.group(1)):
            errors.append(f"{display}: the csv cost {cost} is not the ctor base cost {cm.group(1)}")

        # numeric pairs: the csv text must contain every literal builder pair
        csv_pairs = set(parse_number_pairs(r["Description"]) + parse_number_pairs(cost))
        for base, up in parse_builders(text):
            if base != up and (base, up) not in csv_pairs:
                warnings.append(
                    f"{display}: the builder makes {base} ({up}), but that pair is not in the csv row")

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
