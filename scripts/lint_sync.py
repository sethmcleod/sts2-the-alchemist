#!/usr/bin/env python3
"""Lint the card three-way sync, art on disk, asset paths and Compat/ branch markers"""

import csv
import json
import os
import re
import struct
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
CODE = REPO / "AlchemistCode"
IMG = REPO / "Alchemist" / "images"
CSV = REPO / "cards.csv"
LOC = REPO / "Alchemist" / "localization" / "eng" / "cards.json"

SPECIAL_CLASS = {"Strike": "StrikeAlchemist", "Defend": "DefendAlchemist"}

ASSET_SPECS = [
    ("card", "Cards", "Card", [("portrait", "card_portraits", "{s}.png"),
                               ("big", "card_portraits/big", "{s}.png")]),
    ("power", "Powers", "Power", [("packed", "powers", "{s}.png"),
                                  ("big", "powers/big", "{s}.png")]),
    ("relic", "Relics", "Relic", [("packed", "relics", "{s}.png"),
                                  ("outline", "relics", "{s}_outline.png"),
                                  ("big", "relics/big", "{s}.png")]),
    ("potion", "Potions", "Potion", [("packed", "potions", "{s}.png"),
                                     ("outline", "potions/outlines", "{s}.png")]),
    ("enchantment", "Enchantments", "Enchantment", [("icon", "enchantments", "{s}.png")]),
]

FALLBACK_ART = {"card.png", "power.png", "relic.png", "relic_outline.png", "potion.png"}

BORROWED_ART_BASES = {"CustomTemporaryStrengthPower", "BorrowedIconPower"}


def norm(name: str) -> str:
    return re.sub(r"[^a-z0-9]", "", name.lower())


def snake(class_name: str) -> str:
    s = re.sub(r"(?<=[A-Za-z0-9])(?=[A-Z])", "_", class_name)
    return s.upper()


def load_cards_csv() -> list[dict]:
    rows = []
    with open(CSV, newline="") as f:
        for row in csv.DictReader(f):
            if row.get("Card", "").strip():
                rows.append(row)
    return rows


def entity_classes(subdir: str, base_marker: str, skip_bases: set[str] = frozenset()) -> dict[str, Path]:
    out = {}
    for path in (CODE / subdir).rglob("*.cs"):
        pattern = r"public\s+(?:sealed\s+)?(?:(abstract)\s+|partial\s+|(abstract)\s+partial\s+)?(?:sealed\s+)?class\s+(\w+)\s*:\s*([\w<>, ]+)"
        for m in re.finditer(pattern, path.read_text()):
            abstract_a, abstract_b, name, bases = m.groups()
            if abstract_a or abstract_b or base_marker not in bases:
                continue
            if skip_bases & {re.sub(r"<.*", "", b).strip() for b in bases.split(",")}:
                continue
            out[name] = path
    return out


def card_classes() -> dict[str, Path]:
    return entity_classes("Cards", "Card")


def asset_name(class_name: str) -> str:
    return snake(class_name).lower()


def png_size(path: Path) -> tuple[int, int] | None:
    with open(path, "rb") as f:
        head = f.read(24)
    if len(head) < 24 or head[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", head[16:24])


def check_card_art(cls: str, claimed: set[Path]) -> tuple[list[str], list[str]]:
    errors, warnings = [], []
    name = f"{asset_name(cls)}.png"
    small = IMG / "card_portraits" / name
    big = IMG / "card_portraits" / "big" / name
    beta = IMG / "card_portraits" / "beta" / name
    claimed.update((small, big, beta))
    has_final = small.exists() or big.exists()
    if not has_final and not beta.exists():
        errors.append(f"card {cls}: no portrait, neither card_portraits/{name} nor card_portraits/beta/{name}")
        return errors, warnings
    if small.exists() != big.exists():
        missing = f"card_portraits/{'big/' if small.exists() else ''}{name}"
        warnings.append(f"card {cls}: the final art needs both sizes, {missing} is missing")
    if not has_final:
        return errors, warnings
    if not beta.exists():
        warnings.append(f"card {cls}: no placeholder card_portraits/beta/{name}, so Use Beta Art shows the generic back")
        return errors, warnings
    target = png_size(small) if small.exists() else None
    if target is None:
        full = png_size(big)
        target = (full[0] // 2, full[1] // 2) if full else None
    actual = png_size(beta)
    if target and actual and (actual[0] > target[0] or actual[1] > target[1]):
        warnings.append(f"card {cls}: the placeholder is {actual[0]}x{actual[1]}, the final art is "
                        f"{target[0]}x{target[1]}; shrink the placeholder to that size")
    return errors, warnings


def check_assets() -> tuple[list[str], list[str], int]:
    errors, warnings = [], []
    claimed: set[Path] = set()

    for label, subdir, marker, variants in ASSET_SPECS:
        for cls in sorted(entity_classes(subdir, marker, BORROWED_ART_BASES)):
            if label == "card":
                card_errors, card_warnings = check_card_art(cls, claimed)
                errors += card_errors
                warnings += card_warnings
                continue
            for variant, img_dir, template in variants:
                path = IMG / img_dir / template.format(s=asset_name(cls))
                claimed.add(path)
                if not path.exists():
                    errors.append(f"{label} {cls}: the {variant} art {img_dir}/{path.name} is missing")

    art_dirs = {img_dir for _, _, _, variants in ASSET_SPECS for _, img_dir, _ in variants}
    art_dirs.add("card_portraits/beta")
    literal_names = set(re.findall(r'"([A-Za-z0-9_.-]+\.png)"',
                                   "\n".join(p.read_text() for p in CODE.rglob("*.cs"))))
    for img_dir in sorted(art_dirs):
        for path in sorted((IMG / img_dir).glob("*.png")):
            if path.name.endswith("_outline.png") and (path.parent / path.name.replace("_outline", "")) in claimed:
                continue
            if path not in claimed and path.name not in FALLBACK_ART and path.name not in literal_names:
                warnings.append(f"{img_dir}/{path.name}: no class uses this art")

    return errors, warnings, len(claimed)


def check_asset_literals() -> list[str]:
    errors = []
    for path in sorted(CODE.rglob("*.cs")):
        text = "\n".join(l for l in path.read_text().splitlines() if "// os path" not in l)
        for m in re.finditer(r'"([A-Za-z0-9_/\.-]+\.(?:png|tscn|tres|wav|ogg|ttf|gdshader))"', text):
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
    errors, warnings = [], []
    src = (CODE / "Extensions" / "StringExtensions.cs").read_text()
    for m in re.finditer(r"public static string (\w+)\(this string path\)\s*\{(.*?)\n    \}", src, re.S):
        name, body = m.groups()
        if "ResourceLoader.Exists" not in body:
            continue
        tail = body[body.index("ResourceLoader.Exists"):]
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
    try:
        out = subprocess.run(["git", "-C", str(REPO), "symbolic-ref", "--short", "-q", "HEAD"],
                             capture_output=True, text=True).stdout.strip()
    except OSError:
        return None
    return out or None


def check_compat_branch() -> list[str]:
    errors = []
    branch = git_branch()
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


NAME_TAGS = ("Strike", "Defend")


def check_name_tags(classes: dict[str, Path]) -> list[str]:
    titles = json.loads(LOC.read_text())
    errors = []
    for cls, path in classes.items():
        title = titles.get(f"ALCHEMIST-{snake(cls)}.title", "")
        text = path.read_text()
        for tag in NAME_TAGS:
            if re.search(rf"\b{tag}\b", title) and f"CardTag.{tag}" not in text:
                errors.append(f"class {cls}: the title \"{title}\" contains {tag}, but the class has no "
                              f"CardTag.{tag}, so base effects that read the tag skip it")
    return errors


COMMENT_RE = re.compile(r"(?<!:)//(?!/)(.*)$")

COMMENT_BANNED = [
    ("a version number", re.compile(r"\bv?\d+\.\d+\.\d+\b")),
    ("a date", re.compile(
        r"\b20\d\d([-./]\d{1,2}){1,2}\b"
        r"|\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]* 20\d\d\b")),
    ("process or history", re.compile(
        r"\b(the user|user'?s?\b|playtest|feedback|communit|approved|decision|proposal"
        r"|doctrine|rework|redesign|retired|shipped|changelog|wave \d)", re.I)),
    ("an in-progress marker", re.compile(r"\b(TODO|FIXME|HACK|XXX)\b")),
]


def check_comment_hygiene() -> list[str]:
    errors = []
    for path in sorted(CODE.rglob("*.cs")):
        for n, line in enumerate(path.read_text().splitlines(), 1):
            m = COMMENT_RE.search(line)
            if not m:
                continue
            for label, pat in COMMENT_BANNED:
                if pat.search(m.group(1)):
                    rel = path.relative_to(REPO)
                    errors.append(f"{rel}:{n}: the comment carries {label}; comments describe "
                                  f"complex code only ({m.group(1).strip()[:60]!r})")
    return errors


def check_badge_bins() -> list[str]:
    sys.path.insert(0, str(REPO / "tools" / "analytics"))
    import export_stats
    import mod_meta
    errors = []
    for badge in mod_meta.badges():
        metric = export_stats.BADGE_METRICS.get(badge["id"])
        if metric not in export_stats.HISTOGRAMS:
            continue
        width, last = export_stats.HISTOGRAMS[metric]
        for tier in badge["tiers"]:
            if tier["at"] % width or tier["at"] > last:
                errors.append(f"badge {badge['id']}: {tier['tier']} {tier['at']} is off the site's {metric} "
                              f"bins (width {width}, last {last})")
    return errors


def parse_number_pairs(desc: str) -> list[tuple[int, int]]:
    return [(int(a), int(b)) for a, b in re.findall(r"(\d+)%?\s*\((\d+)%?\)", desc)]


def parse_builders(text: str) -> list[tuple[int, int]]:
    """A base of 0 is a dynamic placeholder, which the csv shows as "(+ N)", so it is skipped"""
    pairs = []
    for m in re.finditer(r"With(?:Damage|Block|Energy|Cards|(?:Quiet)?Power<\w+>)\((\d+)\s*,\s*(-?\d+)\)", text):
        base, delta = int(m.group(1)), int(m.group(2))
        if base != 0:
            pairs.append((base, base + delta))
    for m in re.finditer(r"WithVar\(\s*\"[^\"]+\"\s*,\s*(\d+)\s*,\s*(-?\d+)\)", text):
        base, delta = int(m.group(1)), int(m.group(2))
        if base != 0:
            pairs.append((base, base + delta))
    for m in re.finditer(
            r"new \w+Var\(\s*(?:\"[^\"]+\"\s*,\s*)?(\d+)[^)]*\)"
            r"\s*(?:\.WithUpgrade\(\s*(-?\d+)m?\s*\))?", text):
        base, delta = int(m.group(1)), int(m.group(2) or 0)
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

    for r in rows:
        display = r["Card"]
        expected = SPECIAL_CLASS.get(display, display.replace(" ", "").replace("'", ""))
        if norm(expected) not in class_by_norm:
            errors.append(f"csv row '{display}': no card class matches it ({expected}.cs must exist)")
    for cls in classes:
        if norm(cls) not in csv_by_norm:
            errors.append(f"class {cls}: no row in cards.csv matches it")

    for cls in classes:
        key = f"ALCHEMIST-{snake(cls)}"
        for suffix in (".title", ".description"):
            if f'"{key}{suffix}"' not in loc:
                errors.append(f"class {cls}: the loc key {key}{suffix} is missing from cards.json")

    for r in rows:
        display = r["Card"]
        cls = SPECIAL_CLASS.get(display, display.replace(" ", "").replace("'", ""))
        path = classes.get(cls) or classes.get(class_by_norm.get(norm(cls), ""))
        if not path:
            continue
        text = path.read_text()

        cost = r["Cost"].strip()
        cm = re.search(r":\s*base\(\s*(\d+)\s*,", text)
        if cm and cost.isdigit() and int(cost) != int(cm.group(1)):
            errors.append(f"{display}: the csv cost {cost} is not the ctor base cost {cm.group(1)}")

        csv_pairs = set(parse_number_pairs(r["Description"]) + parse_number_pairs(cost))
        for base, up in parse_builders(text):
            if base != up and (base, up) not in csv_pairs:
                warnings.append(
                    f"{display}: the builder makes {base} ({up}), but that pair is not in the csv row")

    asset_errors, asset_warnings, art_count = check_assets()
    errors += asset_errors
    warnings += asset_warnings

    errors += check_asset_literals()
    fb_errors, fb_warnings = check_fallback_art()
    errors += fb_errors
    warnings += fb_warnings
    errors += check_res_path_separator()

    errors += check_compat_branch()

    errors += check_card_themes(classes)
    errors += check_name_tags(classes)

    errors += check_comment_hygiene()
    errors += check_badge_bins()

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
