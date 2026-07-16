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

Run via `scripts/dev.sh lint`.
"""

import csv
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
CARDS_DIR = REPO / "AlchemistCode" / "Cards"
CSV = REPO / "cards.csv"
LOC = REPO / "Alchemist" / "localization" / "eng" / "cards.json"

# csv display name -> class name, for the two basics that carry an "Alchemist" suffix
SPECIAL_CLASS = {"Strike": "StrikeAlchemist", "Defend": "DefendAlchemist"}


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


def card_classes() -> dict[str, Path]:
    """class name -> file, for every concrete AlchemistCard subclass."""
    out = {}
    for path in CARDS_DIR.rglob("*.cs"):
        text = path.read_text()
        m = re.search(r"public\s+class\s+(\w+)\s*:\s*\w*Card", text)
        if m and "abstract" not in text[:m.start()].split("\n")[-1]:
            out[m.group(1)] = path
    return out


def parse_number_pairs(desc: str) -> list[tuple[int, int]]:
    """Extract 'N (M)' upgrade pairs from a csv description/cost cell."""
    return [(int(a), int(b)) for a, b in re.findall(r"(\d+)\s*\((\d+)\)", desc)]


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

    for w in warnings:
        print(f"\033[33mwarn\033[0m  {w}")
    for e in errors:
        print(f"\033[31mFAIL\033[0m  {e}")
    print(f"\n{len(rows)} cards, {len(classes)} classes: "
          f"{len(errors)} error(s), {len(warnings)} warning(s)")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
