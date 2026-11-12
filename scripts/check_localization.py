#!/usr/bin/env python3
"""Check locale files against eng/: keys, placeholders, BBCode tags, and with --glossary the base-game terms"""
import argparse
import json
import re
import sys
import unicodedata
from collections import Counter
from pathlib import Path

LOC_DIR = Path(__file__).resolve().parent.parent / "Alchemist" / "localization"

PLACEHOLDER_RE = re.compile(r"\{([A-Za-z_][A-Za-z0-9_]*)")
TAG_RE = re.compile(r"\[/?([a-z_]+)\]")
PLURAL_RE = re.compile(r"\{[A-Za-z_][A-Za-z0-9_]*:plural(?:\([a-z]+\))?:((?:[^{}]|\{[^{}]*\})*)\}")
TAGGED_RE = re.compile(r"\[(gold|purple|blue|green|red)\](.*?)\[/\1\]")
ANY_TAG_RE = re.compile(r"\[/?[a-z_]+\]")

BASELIB_FILL_VERSION = "3.4.7"
BASELIB_FILLS = {
    "static_hover_tips.json": {
        "BASELIB-SCRY.title": "Scry",
        "BASELIB-SCRY.description": (
            "Look at the top [blue]X[/blue] cards of your [gold]Draw Pile[/gold]. "
            "You may discard any of them."),
        "BASELIB-SCRY.smartDescription": (
            "Look at the top {Scry:plural:card|[blue]{Scry:diff()}[/blue] cards} of your "
            "[gold]Draw Pile[/gold]. You may discard {Scry:plural:it|any of them}."),
    },
}
BASELIB_SHIPS = {"static_hover_tips.json": {"deu", "eng", "ita", "jpn", "kor", "rus", "zhs"}}


def placeholders(text: str) -> list[str]:
    return sorted(PLACEHOLDER_RE.findall(text))


def tags(text: str) -> list[str]:
    return sorted(TAG_RE.findall(text))


def split_plurals(text: str) -> tuple[str, list[str]]:
    branches: list[str] = []

    def collect(match: re.Match) -> str:
        branches.extend(match.group(1).split("|"))
        return ""

    return PLURAL_RE.sub(collect, text), branches


def tag_mismatch(src: str, dst: str) -> str | None:
    """With a plural block only the tag kinds must match, and each branch must close its own tags"""
    src_outside, src_branches = split_plurals(src)
    dst_outside, dst_branches = split_plurals(dst)
    if not src_branches and not dst_branches:
        return (
            None if tags(src) == tags(dst)
            else f"tags differ ({tags(src)} vs {tags(dst)})"
        )
    for fragment in [dst_outside, *dst_branches]:
        counts = Counter(TAG_RE.findall(fragment))
        unclosed = sorted(t for t, n in counts.items() if n % 2)
        if unclosed:
            return f"unclosed {unclosed} in {fragment.strip()!r}"
    if set(tags(src)) != set(tags(dst)):
        return f"uses tags {sorted(set(tags(dst)))}, English uses {sorted(set(tags(src)))}"
    return None


def strip_tags(text: str) -> str:
    return ANY_TAG_RE.sub("", text).strip()


def fold(text: str) -> str:
    """Casefold and drop accents, on Latin letters only (marks on other scripts carry meaning)"""
    kept = []
    for ch in unicodedata.normalize("NFD", text):
        if unicodedata.category(ch) == "Mn" and kept and kept[-1] < "\u0250":
            continue
        kept.append(ch)
    return unicodedata.normalize("NFC", "".join(kept)).casefold()


def stem(word: str) -> str:
    """A short stem, so a glossary noun still matches a verb on the same root"""
    folded = fold(word)
    return folded[: max(3, min(4, int(len(folded) * 0.6)))]


def check_structure(lang: str, eng: dict[str, dict]) -> list[str]:
    errors = []
    for fname, eng_table in eng.items():
        path = LOC_DIR / lang / fname
        if not path.exists():
            errors.append(f"{lang}/{fname}: missing file")
            continue
        try:
            table = json.loads(path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, UnicodeDecodeError) as exc:
            errors.append(f"{lang}/{fname}: invalid JSON: {exc}")
            continue
        for key in sorted(eng_table.keys() - table.keys()):
            errors.append(f"{lang}/{fname}: missing key {key}")
        fills = BASELIB_FILLS.get(fname, {})
        for key in sorted(table.keys() - eng_table.keys()):
            if key.startswith("BASELIB-"):
                if key not in fills:
                    errors.append(f"{lang}/{fname}: unknown BaseLib key {key}; only BASELIB_FILLS are allowed")
                elif lang in BASELIB_SHIPS.get(fname, set()):
                    errors.append(f"{lang}/{fname}: {key} overrides a string BaseLib ships for {lang}; delete the fill")
                else:
                    errors += parity_errors(lang, fname, key, fills[key], table[key])
                continue
            errors.append(f"{lang}/{fname}: extra key {key}")
        if fills and lang not in BASELIB_SHIPS.get(fname, set()):
            missing = fills.keys() - table.keys()
            if missing:
                errors.append(f"{lang}/{fname}: BaseLib fill missing {sorted(missing)}")
        for key in eng_table.keys() & table.keys():
            errors += parity_errors(lang, fname, key, eng_table[key], table[key])
    return errors


def parity_errors(lang: str, fname: str, key: str, src: str, dst: str) -> list[str]:
    errors = []
    src_ph, dst_ph = placeholders(src), placeholders(dst)
    if PLURAL_RE.search(src) or PLURAL_RE.search(dst):
        src_ph, dst_ph = sorted(set(src_ph)), sorted(set(dst_ph))
    if src_ph != dst_ph:
        errors.append(f"{lang}/{fname}: {key}: placeholders differ ({src_ph} vs {dst_ph})")
    if (problem := tag_mismatch(src, dst)) is not None:
        errors.append(f"{lang}/{fname}: {key}: {problem}")
    return errors


def check_glossary(lang: str, eng: dict[str, dict], glossary: dict[str, str]) -> list[str]:
    problems = []
    for fname, eng_table in eng.items():
        path = LOC_DIR / lang / fname
        if not path.exists():
            continue
        try:
            table = json.loads(path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, UnicodeDecodeError):
            continue
        for key, src in eng_table.items():
            dst = table.get(key)
            if not isinstance(dst, str):
                continue
            wanted = {
                strip_tags(text) for _tag, text in TAGGED_RE.findall(src)
            } & glossary.keys()
            if not wanted:
                continue
            haystack = fold(dst)
            for term in sorted(wanted):
                official = glossary[term]
                found = False
                for form in official.split("|"):
                    parts = [w for w in re.split(r"\s+", form) if len(w) > 1]
                    if (parts and all(stem(w) in haystack for w in parts)) or (
                        not parts and fold(form) in haystack
                    ):
                        found = True
                        break
                if found:
                    continue
                problems.append(
                    f"{lang}/{fname}: {key}: '{term}' should read "
                    f"'{official}' -- not found in: {strip_tags(dst)[:70]}"
                )
    return problems


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("langs", nargs="*")
    parser.add_argument("--glossary", type=Path, help="directory of glossary_<lang>.json")
    args = parser.parse_args()
    csproj = (LOC_DIR.parent.parent / "Alchemist.csproj").read_text(encoding="utf-8")
    pinned = re.search(r'Include="Alchyr\.Sts2\.BaseLib" Version="([^"]+)"', csproj)
    if pinned and pinned.group(1) != BASELIB_FILL_VERSION:
        print(f"warning: BaseLib is {pinned.group(1)}, BASELIB_FILLS were verified against "
              f"{BASELIB_FILL_VERSION}; re-check which languages BaseLib ships")

    eng = {
        p.name: json.loads(p.read_text(encoding="utf-8"))
        for p in sorted((LOC_DIR / "eng").glob("*.json"))
    }
    langs = args.langs or sorted(
        d.name for d in LOC_DIR.iterdir() if d.is_dir() and d.name != "eng"
    )
    if not langs:
        print("no languages to check")
        return

    failed = False
    for lang in langs:
        errors = check_structure(lang, eng)
        warnings = []
        if args.glossary:
            path = args.glossary / f"glossary_{lang}.json"
            if path.exists():
                warnings = check_glossary(
                    lang, eng, json.loads(path.read_text(encoding="utf-8"))
                )
        if errors:
            failed = True
            print(f"== {lang}: {len(errors)} structural problem(s)")
            for err in errors[:40]:
                print("  " + err)
            if len(errors) > 40:
                print(f"  ... and {len(errors) - 40} more")
        if warnings:
            failed = True
            counts = Counter(w.split("'")[1] for w in warnings)
            print(f"== {lang}: {len(warnings)} terminology problem(s)")
            print("   worst terms: " + ", ".join(f"{t} x{c}" for t, c in counts.most_common(8)))
            for warn in warnings[:15]:
                print("  " + warn)
            if len(warnings) > 15:
                print(f"  ... and {len(warnings) - 15} more")
        if not errors and not warnings:
            print(f"== {lang}: OK")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
