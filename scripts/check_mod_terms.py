#!/usr/bin/env python3
"""Check that each language renders the mod's own keywords one way"""
import json
import re
import sys
import unicodedata
from collections import Counter, defaultdict
from pathlib import Path

LOC = Path(__file__).resolve().parent.parent / "Alchemist" / "localization"

TAGGED_RE = re.compile(r"\[(gold|purple|blue|green|red)\](.*?)\[/\1\]")
ANY_TAG_RE = re.compile(r"\[/?[a-z_]+\]")

MOD_TERMS = [
    "Antitoxin", "Ferment", "Brew", "Laced",
    "Mix", "Mixes", "Residue", "Enchanted", "Enchant",
]

STEM = 4


def strip_tags(text: str) -> str:
    return ANY_TAG_RE.sub("", text).strip()


def stem(word: str) -> str:
    decomposed = unicodedata.normalize("NFD", word).casefold()
    folded = "".join(c for c in decomposed if not unicodedata.combining(c))
    return folded[:STEM] if len(folded) > STEM else folded


def load(lang: str) -> dict[str, dict[str, str]]:
    return {
        p.name: json.loads(p.read_text(encoding="utf-8"))
        for p in sorted((LOC / lang).glob("*.json"))
    }


def sites(eng: dict, term: str) -> list[tuple[str, str, int, int]]:
    wanted = term.lower()
    found = []
    for fname, table in eng.items():
        for key, value in table.items():
            if not isinstance(value, str):
                continue
            spans = [strip_tags(s[1]).lower() for s in TAGGED_RE.findall(value)]
            if spans.count(wanted) != 1:
                continue
            found.append((fname, key, spans.index(wanted), len(spans)))
    return found


DISTINCT = [
    ("cards.json", "ALCHEMIST-DOSE.title"),
    ("powers.json", "ALCHEMIST-ANTITOXIN_POWER.title"),
    ("cards.json", "ALCHEMIST-TOXIN_SKIN.title"),
]


def distinct_names(tables: dict[str, dict[str, str]], poison: str) -> list[str]:
    named = []
    for fname, key in DISTINCT:
        value = tables.get(fname, {}).get(key)
        if isinstance(value, str) and value.strip():
            named.append((key.split(".")[0].replace("ALCHEMIST-", ""), value.strip()))
    if poison:
        named.append(("POISON", poison))
    # Compared whole, not by stem: Antidote and Antitoxin share a prefix in most languages
    def norm(s: str) -> str:
        d = unicodedata.normalize("NFD", s).casefold()
        return "".join(c for c in d if not unicodedata.combining(c)).strip()

    problems = []
    for i, (ka, va) in enumerate(named):
        for kb, vb in named[i + 1:]:
            if norm(va) == norm(vb):
                problems.append(
                    f"  {ka} and {kb} both render as {va!r} - players cannot tell them apart"
                )
    return problems


def cross_file_titles(tables: dict[str, dict[str, str]]) -> list[str]:
    seen: dict[str, dict[str, str]] = defaultdict(dict)
    for fname, table in tables.items():
        for key, value in table.items():
            if key.endswith(".title") and isinstance(value, str):
                seen[key][fname] = value
    problems = []
    for key, files in sorted(seen.items()):
        if len(set(files.values())) > 1:
            detail = "; ".join(f"{f}={v!r}" for f, v in sorted(files.items()))
            problems.append(f"  {key}: {detail}")
    return problems


def main() -> None:
    argv = sys.argv[1:]
    glossary_dir = None
    if "--glossary" in argv:
        i = argv.index("--glossary")
        glossary_dir = Path(argv[i + 1])
        del argv[i:i + 2]
    args = [a for a in argv if not a.startswith("--")]

    eng = load("eng")
    langs = args or sorted(
        d.name for d in LOC.iterdir() if d.is_dir() and d.name != "eng"
    )

    term_sites = {t: sites(eng, t) for t in MOD_TERMS}
    failed = False

    for lang in langs:
        tables = load(lang)
        # Word order differs, so a position can land on a neighbouring base-game term
        other_terms: set[str] = set()
        gloss: dict[str, str] = {}
        if glossary_dir and (glossary_dir / f"glossary_{lang}.json").exists():
            gloss = json.loads(
                (glossary_dir / f"glossary_{lang}.json").read_text(encoding="utf-8")
            )
            other_terms = {stem(v) for v in gloss.values() if v}
        problems = []
        for term, places in term_sites.items():
            if len(places) < 2:
                continue
            renderings: dict[str, list[str]] = defaultdict(list)
            for fname, key, index, count in places:
                value = tables.get(fname, {}).get(key)
                if not isinstance(value, str):
                    continue
                spans = TAGGED_RE.findall(value)
                if len(spans) != count:
                    continue
                span = strip_tags(spans[index][1])
                if not span or re.fullmatch(r"[\d\W]*(\{[^}]*\})?[\d\W]*", span):
                    continue
                if stem(span) in other_terms:
                    continue
                renderings[span].append(f"{fname}:{key}")
            # A root seen once is usually reordered tags, so only roots seen more than once count
            roots: dict[str, int] = Counter()
            examples: dict[str, tuple[str, str]] = {}
            for span, where in renderings.items():
                root = stem(span)
                roots[root] += len(where)
                if root not in examples or len(where) > 1:
                    examples[root] = (span, where[0])
            corroborated = {r: n for r, n in roots.items() if n > 1}
            if len(corroborated) > 1:
                detail = "; ".join(
                    f"{examples[r][0]!r} in {n} place(s) e.g. {examples[r][1]}"
                    for r, n in sorted(corroborated.items(), key=lambda kv: -kv[1])
                )
                problems.append(f"  {term}: {detail}")
        problems += cross_file_titles(tables)
        problems += distinct_names(tables, gloss.get("Poison", ""))
        if problems:
            failed = True
            print(f"== {lang}: {len(problems)} inconsistent term(s)")
            print("\n".join(problems))
        else:
            print(f"== {lang}: OK")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
