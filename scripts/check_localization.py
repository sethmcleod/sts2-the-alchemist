#!/usr/bin/env python3
"""Check mod localization files against the English reference.

Structural checks, per language:
  - every file present in eng/ exists and parses
  - key sets match eng exactly
  - SmartFormat placeholder names match per string
  - BBCode tag multisets match per string

With --glossary, also checks terminology: whenever an English string tags a
base-game term ("[gold]Poison[/gold]"), the translation must contain the base
game's own word for it. Matching is on a stem, so inflected forms still pass.

Usage:
    python3 scripts/check_localization.py [lang ...]
    python3 scripts/check_localization.py --glossary <dir> [lang ...]
"""
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
# A {Name:plural:a|b|c} block, allowing one level of nested braces in a branch.
PLURAL_RE = re.compile(r"\{[A-Za-z_][A-Za-z0-9_]*:plural:((?:[^{}]|\{[^{}]*\})*)\}")
TAGGED_RE = re.compile(r"\[(gold|purple|blue|green|red)\](.*?)\[/\1\]")
ANY_TAG_RE = re.compile(r"\[/?[a-z_]+\]")


def placeholders(text: str) -> list[str]:
    return sorted(PLACEHOLDER_RE.findall(text))


def tags(text: str) -> list[str]:
    return sorted(TAG_RE.findall(text))


def split_plurals(text: str) -> tuple[str, list[str]]:
    """Separate a string into what sits outside plural blocks, and each branch."""
    branches: list[str] = []

    def collect(match: re.Match) -> str:
        branches.extend(match.group(1).split("|"))
        return ""

    return PLURAL_RE.sub(collect, text), branches


def tag_mismatch(src: str, dst: str) -> str | None:
    """Describe how two strings' tags differ, or None if they are compatible.

    Without a plural block a translation cannot restructure, so the tags must
    match exactly. With one it can: a language may need the tagged term repeated
    across more plural forms than English has, or may hoist the count out of the
    block entirely. Then the requirement is only that the same kinds of tag are
    used and that every branch stands on its own with its tags closed.
    """
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


def stem(word: str) -> str:
    """Leading portion of a word, enough to survive inflection.

    Capped short because the glossary holds nouns while card text often needs a
    verb built on the same root ("Aturdimiento" vs "aturdes"). A longer stem
    would reject those, and they are correct.
    """
    folded = unicodedata.normalize("NFC", word).casefold()
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
        for key in sorted(table.keys() - eng_table.keys()):
            errors.append(f"{lang}/{fname}: extra key {key}")
        for key in eng_table.keys() & table.keys():
            src, dst = eng_table[key], table[key]
            # Same restructuring latitude the tag check grants: with a plural block in
            # play, a translation may repeat a placeholder across more branches than
            # English has or collapse identical branches to one use. The base game does
            # both (CHARGE: Japanese folds {IfUpgraded} to a single use, Russian to
            # three), so the requirement drops to the same set of names.
            src_ph, dst_ph = placeholders(src), placeholders(dst)
            if PLURAL_RE.search(src) or PLURAL_RE.search(dst):
                src_ph, dst_ph = sorted(set(src_ph)), sorted(set(dst_ph))
            if src_ph != dst_ph:
                errors.append(
                    f"{lang}/{fname}: {key}: placeholders differ "
                    f"({src_ph} vs {dst_ph})"
                )
            if (problem := tag_mismatch(src, dst)) is not None:
                errors.append(f"{lang}/{fname}: {key}: {problem}")
    return errors


def check_glossary(lang: str, eng: dict[str, dict], glossary: dict[str, str]) -> list[str]:
    """Flag strings that tag a base-game term but never use its official word."""
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
            haystack = unicodedata.normalize("NFC", dst).casefold()
            for term in sorted(wanted):
                official = glossary[term]
                # Every word of the official term must show up, by stem.
                parts = [w for w in re.split(r"\s+", official) if len(w) > 1]
                if parts and all(stem(w) in haystack for w in parts):
                    continue
                if not parts and official.casefold() in haystack:
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
