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
powers, relics, potions and enchantments all get their icons from the class name. If you
rename a class but you do not rename its png, the entity has no art and no class uses the
png. Art that is missing is a FAIL. Art that no class uses is a warning.

Two further checks guard the case that froze a run: a class that hardcodes an asset
filename passes the class-name check while asking the game for a file that is not there,
and a miss that lands on an absent fallback throws the same way. So every asset literal in
the code must resolve, and every fallback a *ImagePath helper uses must itself exist.

A last check guards the case that broke a publish: every file in AlchemistCode/Compat/ is
written against one game branch, and a merge from beta silently carries beta's copies onto
main. The mismatch only shows up as a wall of CS0115 errors that name no branch, so each
file states its branch in a COMPAT-BRANCH marker and this checks it against the branch you
are on.

Run it with `scripts/dev.sh lint`.
"""

import csv
import os
import re
import subprocess
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
    # cards use the base game layout: the real portrait is card_portraits/<s>.png and the beta
    # placeholder is card_portraits/beta/<s>.png, and check_assets accepts either
    ("card", "Cards", "Card", [("portrait", "card_portraits", "{s}.png")]),
    ("power", "Powers", "Power", [("packed", "powers", "{s}.png"),
                                  ("big", "powers/big", "{s}.png")]),
    ("relic", "Relics", "Relic", [("packed", "relics", "{s}.png"),
                                  ("outline", "relics", "{s}_outline.png"),
                                  ("big", "relics/big", "{s}.png")]),
    ("potion", "Potions", "Potion", [("packed", "potions", "{s}.png"),
                                     ("outline", "potions/outlines", "{s}.png")]),
    ("enchantment", "Enchantments", "Enchantment", [("icon", "enchantments", "{s}.png")]),
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
        # "partial" appears when a class is split across the branch-specific Compat files, and it
        # can sit on either side of "abstract". Only the half that names the base is matched, so a
        # partial still resolves to exactly one file
        pattern = r"public\s+(?:sealed\s+)?(?:(abstract)\s+|partial\s+|(abstract)\s+partial\s+)?(?:sealed\s+)?class\s+(\w+)\s*:\s*([\w<>, ]+)"
        for m in re.finditer(pattern, path.read_text()):
            abstract_a, abstract_b, name, bases = m.groups()
            if not (abstract_a or abstract_b) and base_marker in bases:
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


def check_asset_literals() -> list[str]:
    """No model may name an asset file that is not on disk.

    check_assets derives the filename from the class name, so it only proves the *convention* is
    satisfied. A class that hardcodes a different filename passes that check while asking the game
    for a file that does not exist, and a missing texture throws inside the effect handler and
    freezes the run. That is how the Dosed and Potent icons shipped naming the pre-rename art
    """
    errors = []
    for path in sorted(CODE.rglob("*.cs")):
        for m in re.finditer(r'"([A-Za-z0-9_/\.-]+\.(?:png|tscn|tres|wav|ogg|ttf|gdshader))"', path.read_text()):
            ref = m.group(1)
            if ref.startswith("res://"):
                cand = [REPO / ref[len("res://"):]]
            else:
                stem = ref.rsplit("/", 1)[-1]
                cand = list(IMG.rglob(stem)) + list((REPO / "Alchemist").rglob(stem))
            if not any(c.exists() for c in cand):
                rel = path.relative_to(REPO)
                errors.append(f"{rel}: names the asset '{ref}', which is not on disk")
    return errors


def check_game_scene_paths() -> tuple[list[str], list[str]]:
    """Every "vfx/..." the mod names must be a scene the game actually ships.

    check_asset_literals only matches strings that carry a file extension, so an extensionless
    game scene path slips past it. A path the game cannot resolve makes VfxCmd.PlayVfx throw
    inside the attack command, which kills PlayCardAction: the card sticks in the middle of the
    screen, deals no damage and never leaves play. That is how Slow Burn shipped naming
    vfx/vfx_attack_fire, which does not exist. Skipped when the game is not installed, so CI,
    which has no game, still passes
    """
    game_dir = os.environ.get("STS2_GAME_DIR")
    if not game_dir:
        return [], ["game scene paths: STS2_GAME_DIR is not set, skipping the vfx check"]
    pcks = list(Path(game_dir).rglob("Slay the Spire 2.pck"))
    if not pcks:
        return [], ["game scene paths: no game pck found, skipping the vfx check"]

    refs = set()
    for path in sorted(CODE.rglob("*.cs")):
        refs.update(re.findall(r'"(vfx/[a-z0-9_/]+)"', path.read_text()))
    if not refs:
        return [], []

    blob = pcks[0].read_bytes()
    missing = [r for r in sorted(refs) if (r + ".tscn").encode() not in blob]
    return [f"the game ships no scene for '{r}', which the mod names" for r in missing], []


def check_res_path_separator() -> list[str]:
    """A res:// path must be built with forward slashes on every platform.

    Path.Join and Path.Combine join with the platform separator, which is a backslash on Windows.
    The pack index is keyed by forward slashes only, so such a path loads on macOS and Linux and
    fails on Windows. The failure is invisible here: it shipped once as an empty Compendium for
    every character, because one missing character icon throws out of NCardLibrary._Ready
    """
    errors = []
    for path in sorted(CODE.rglob("*.cs")):
        for n, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if line.lstrip().startswith("//"):
                continue
            if re.search(r"\bPath\.(Join|Combine)\(", line):
                errors.append(f"{path.name}:{n} builds a path with Path.Join/Combine; join "
                              "res:// paths with '/' so they also load on Windows")
    return errors


def check_fallback_art() -> tuple[list[str], list[str]]:
    """Every fallback a *ImagePath helper falls back to must itself exist.

    A miss that lands on an absent fallback throws exactly like the original miss did, and a
    missing texture inside an effect handler freezes the run. A helper with no fallback at all
    only gets a warning: it needs art before it can have one
    """
    errors, warnings = [], []
    src = (CODE / "Extensions" / "StringExtensions.cs").read_text()
    for m in re.finditer(r"public static string (\w+)\(this string path\)\s*\{(.*?)\n    \}", src, re.S):
        name, body = m.groups()
        if "ResourceLoader.Exists" not in body:
            continue
        tail = body[body.index("ResourceLoader.Exists"):]
        # A fallback may point at a base game asset, which ships in the game pck and is not ours to check
        if re.search(r'return "res://', tail):
            continue
        join = re.search(r'return Res\(MainFile\.ResPath,\s*(.*?)\);', tail, re.S)
        if not join:
            warnings.append(f"{name}: no fallback art, so a miss returns a path that will throw")
            continue
        parts = [q.strip('" ') for q in re.findall(r'"([^"]+)"', join.group(1))]
        rel = Path(*parts[1:]) if parts and parts[0] == "images" else Path(*parts)
        if not (IMG / rel).exists():
            errors.append(f"{name}: its fallback art images/{rel} is missing")
    return errors, warnings


COMPAT = CODE / "Compat"
COMPAT_MARKER = re.compile(r"^//\s*COMPAT-BRANCH:\s*(main|beta|any)\s*$", re.M)


def git_branch() -> str | None:
    """The checked-out branch, or None in a detached HEAD or outside a work tree."""
    try:
        out = subprocess.run(["git", "-C", str(REPO), "symbolic-ref", "--short", "-q", "HEAD"],
                             capture_output=True, text=True).stdout.strip()
    except OSError:
        return None
    return out or None


def check_compat_branch() -> list[str]:
    """Every file in Compat/ is written against one game branch, so it must match the branch.

    beta targets the game's public-beta branch and main targets the default branch, and the two
    spell a handful of APIs differently. A merge from beta carries beta's copies of these files
    across, which then fails to build against the other game with CS0115 errors that never say
    "branch". Marking the file and checking it here names the real problem in one line
    """
    errors = []
    branch = git_branch()
    # A feature branch is cut from one of the two and carries that one's copies. Only the two
    # release branches have a game branch of their own to be checked against
    if branch not in (None, "main", "beta"):
        return errors
    for path in sorted(COMPAT.glob("*.cs")):
        m = COMPAT_MARKER.search(path.read_text(encoding="utf-8"))
        if m is None:
            errors.append(f"Compat/{path.name}: no '// COMPAT-BRANCH: main|beta|any' marker; "
                          "say which game branch it is written against")
        elif m.group(1) != "any" and branch is not None and m.group(1) != branch:
            errors.append(f"Compat/{path.name}: holds the {m.group(1)} implementation, but you are "
                          f"on {branch}. A merge took the wrong side; restore {branch}'s copy")
    return errors


def check_card_themes(classes: dict[str, Path]) -> list[str]:
    """Every card class must carry [CardTheme(...)] on the line before its declaration. Neutral cards
    say CardTheme.None explicitly, so a missing attribute is always an oversight."""
    theme_re = re.compile(r"\[CardTheme\((?P<themes>[^)]*)\)\]\s*public\s+(?:\w+\s+)*class\s+(?P<name>\w+)")
    enum_src = (CODE / "Cards" / "CardTheme.cs").read_text()
    valid = set(re.findall(r"^\s+(\w+),\s*$", enum_src, re.M))
    errors = []
    for cls, path in classes.items():
        m = theme_re.search(path.read_text())
        if not m or m["name"] != cls:
            errors.append(f"class {cls}: no [CardTheme(...)] attribute before the class declaration")
            continue
        themes = [t.strip().removeprefix("CardTheme.") for t in m["themes"].split(",") if t.strip()]
        if not themes:
            errors.append(f"class {cls}: [CardTheme()] names no theme (use CardTheme.None for a neutral card)")
        for t in themes:
            if t not in valid:
                errors.append(f"class {cls}: unknown theme CardTheme.{t}")
        if "None" in themes and len(themes) > 1:
            errors.append(f"class {cls}: CardTheme.None cannot be combined with another theme")
    return errors


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

    # 5. no class names an asset that is not there, and the fallbacks themselves exist
    errors += check_asset_literals()
    fb_errors, fb_warnings = check_fallback_art()
    errors += fb_errors
    warnings += fb_warnings
    errors += check_res_path_separator()

    # 6. every Compat/ file is the copy for the branch you are on
    errors += check_compat_branch()

    # 7. every card class carries a [CardTheme] attribute (the analytics dashboard groups by it)
    errors += check_card_themes(classes)

    scene_errors, scene_warnings = check_game_scene_paths()
    errors += scene_errors
    warnings += scene_warnings

    for w in warnings:
        print(f"\033[33mwarn\033[0m  {w}")
    for e in errors:
        print(f"\033[31mFAIL\033[0m  {e}")
    print(f"\n{len(rows)} cards, {len(classes)} classes, {art_count} art files: "
          f"{len(errors)} error(s), {len(warnings)} warning(s)")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
