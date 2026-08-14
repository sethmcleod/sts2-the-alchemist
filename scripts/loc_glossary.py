#!/usr/bin/env python3
"""Build a per-language glossary of base-game terms the mod's text references.

The mod wraps game terms in colour tags, e.g. "[gold]Poison[/gold]". Those terms
must read exactly as the base game translates them. Terms are resolved two ways:

1. Direct  - a base-game entry whose whole value is the term (BLOCK.title etc.).
2. Aligned - base-game entries whose only tagged span is the term. Reading the
   same key in another language and taking the span back out gives the official
   wording. This reaches terms that never appear as an entry of their own, such
   as "Hand" or "Exhaust Pile".

Usage:
    python3 scripts/loc_glossary.py --base <dir> [--out <dir>]

<dir> is an extracted copy of the game's res://localization/.
Writes one glossary_<lang>.json per language plus glossary_report.md.
"""
import argparse
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

MOD_ENG = Path(__file__).resolve().parent.parent / "Alchemist" / "localization" / "eng"

TAGGED_RE = re.compile(r"\[(gold|purple|blue|green|red)\](.*?)\[/\1\]")
ANY_TAG_RE = re.compile(r"\[/?[a-z_]+\]")

# Terms the mod invents. They have no base-game translation, so the glossary
# records them for the consistency check only, not as required wording.
MOD_TERMS = {
    "antitoxin", "dosed", "potent", "ferment",
    "reaction", "infuse", "gambit", "brew", "brew potions", "distillate", "distillates",
    "distillate+", "distillates+", "toxic",
    "laced", "fuming", "exalted", "alchemist", "nigredo", "albedo",
    "citrinitas", "rubedo", "nigredo+", "albedo+", "citrinitas+", "rubedo+",
    "golden fruit", "unripe fruit", "work", "journal", "apothecary", "twelve",
    "child", "frog", "frog's", "tincture", "fungi", "laughing", "self",
    "reward", "punish", "patience", "become",
}

# Inflected surface forms that should reuse another term's entry. Participles
# are deliberately absent: "Poisoned" is a different root from "Poison" in
# several languages (ru: Отравленные vs Яд), so aliasing them would demand a
# word the translation is right not to use.
ALIASES = {
    "potion(s)": "potions", "spire's": "spire", "distillates": "distillate",
}

# Terms whose base-game wording depends on context, so no single rendering can
# be enforced. Lowercase gold and poison are the metal and the substance in this
# mod's flavour text, not the currency and the status. "Discard" resolves to
# POTION_POPUP.discard, the button for discarding a potion (de: "Weglegen"),
# while card text uses a different verb for discarding a card (de: "Wirf ... ab").
AMBIGUOUS = {"gold", "poison"}

# The same, but regardless of case. "Gold" the currency is a real glossary term
# while lowercase "gold" is the metal, so that pair stays case-sensitive above;
# "Discard" is context-dependent in every casing.
AMBIGUOUS_ANY_CASE = {"discard"}


def strip_tags(text: str) -> str:
    return ANY_TAG_RE.sub("", text).strip()


def tagged_terms(directory: Path) -> Counter:
    """Every colour-tagged term in a localization directory, with usage counts."""
    found = Counter()
    for path in sorted(directory.glob("*.json")):
        for value in json.loads(path.read_text(encoding="utf-8")).values():
            for _tag, text in TAGGED_RE.findall(value):
                term = strip_tags(text)
                if not term or "{" in term or re.fullmatch(r"[\d\W]+", term):
                    continue
                if len(term.split()) > 3:
                    continue
                found[term] += 1
    return found


def load_lang(base: Path, language: str) -> dict[str, dict[str, str]]:
    tables = {}
    for path in sorted((base / language).glob("*.json")):
        try:
            tables[path.name] = json.loads(path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, UnicodeDecodeError):
            continue
    return tables


def resolve_direct(eng: dict, term: str) -> tuple[str, str] | None:
    """A base-game entry whose entire value is this term."""
    wanted = term.lower()
    best = None
    for filename, table in eng.items():
        for key, value in table.items():
            if not isinstance(value, str):
                continue
            if strip_tags(value).lower() == wanted:
                # Prefer a hover-tip/keyword title over an incidental card name.
                rank = (
                    0 if filename in ("static_hover_tips.json", "card_keywords.json")
                    else 1 if key.endswith(".title") else 2
                )
                if best is None or rank < best[0]:
                    best = (rank, filename, key)
    return (best[1], best[2]) if best else None


def resolve_aligned(eng: dict, term: str, limit: int = 60) -> list[tuple[str, str, int, int]]:
    """Base-game entries where this term is one of the tagged spans.

    Records which span index it occupies and how many spans the entry has, so the
    same position can be read back out of another language.
    """
    wanted = term.lower()
    hits = []
    for filename, table in eng.items():
        for key, value in table.items():
            if not isinstance(value, str):
                continue
            spans = [strip_tags(s[1]).lower() for s in TAGGED_RE.findall(value)]
            # An ambiguous entry (term appears twice) cannot anchor a position.
            if spans.count(wanted) != 1:
                continue
            hits.append((filename, key, spans.index(wanted), len(spans)))
            if len(hits) >= limit:
                return hits
    return hits


# A term is only mandatory if the aligned sites broadly agree on one wording.
# Terms like "Act" sit next to a varying number ("Act 1", "Act 2") and never
# settle, so they fall out to the review list instead of becoming a rule.
AGREEMENT = 0.6


def translate_aligned(
    tables: dict, sites: list[tuple[str, str, int, int]]
) -> tuple[str, float] | None:
    """Read the tagged span back out of a language's copy of those entries."""
    votes = Counter()
    for filename, key, index, span_count in sites:
        value = tables.get(filename, {}).get(key)
        if not isinstance(value, str):
            continue
        spans = TAGGED_RE.findall(value)
        # Only trust the position when the translation kept every span.
        if len(spans) != span_count:
            continue
        span = strip_tags(spans[index][1])
        if span:
            votes[span] += 1
    if not votes:
        return None
    winner, count = votes.most_common(1)[0]
    # A span that is only digits or punctuation means the translation reordered
    # its spans and the position lookup landed on a number, not the term.
    if re.fullmatch(r"[\d\W]+", winner):
        return None
    return winner, count / sum(votes.values())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", required=True, type=Path)
    parser.add_argument("--out", type=Path, default=Path("."))
    args = parser.parse_args()

    base: Path = args.base
    args.out.mkdir(parents=True, exist_ok=True)

    terms = tagged_terms(MOD_ENG)
    eng = load_lang(base, "eng")
    languages = sorted(d.name for d in base.iterdir() if d.is_dir() and d.name != "eng")

    direct: dict[str, tuple[str, str]] = {}
    aligned: dict[str, list[tuple[str, str]]] = {}
    mod_only: list[str] = []
    unresolved: list[str] = []

    for term in terms:
        lowered = ALIASES.get(term.lower(), term.lower())
        if lowered in MOD_TERMS:
            mod_only.append(term)
            continue
        if term in AMBIGUOUS or term.lower() in AMBIGUOUS_ANY_CASE:
            unresolved.append(term)
            continue
        # Try the term as written before falling back to its base form, so an
        # alias never hides a term that resolves on its own.
        candidates = [term] if lowered == term.lower() else [term, lowered]
        for lookup in candidates:
            hit = resolve_direct(eng, lookup)
            if hit:
                direct[term] = hit
                break
            sites = resolve_aligned(eng, lookup)
            if sites:
                aligned[term] = sites
                break
        else:
            unresolved.append(term)

    per_language_missing: dict[str, list[str]] = defaultdict(list)
    low_confidence: dict[str, set[str]] = defaultdict(set)
    for language in languages:
        tables = load_lang(base, language)
        glossary = {}
        for term, (filename, key) in direct.items():
            value = tables.get(filename, {}).get(key)
            if isinstance(value, str) and strip_tags(value):
                glossary[term] = strip_tags(value)
            else:
                per_language_missing[language].append(term)
        for term, sites in aligned.items():
            result = translate_aligned(tables, sites)
            if result and result[1] >= AGREEMENT:
                glossary[term] = result[0]
            else:
                low_confidence[term].add(language)
                per_language_missing[language].append(term)
        out = args.out / f"glossary_{language}.json"
        out.write_text(
            json.dumps(dict(sorted(glossary.items())), ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    report = [
        "# Base-game glossary",
        "",
        f"{len(direct)} direct, {len(aligned)} aligned, {len(mod_only)} mod-specific, "
        f"{len(unresolved)} unresolved, across {len(languages)} languages.",
        "",
        "## Resolved from a base-game entry",
        "",
    ]
    for term, (filename, key) in sorted(direct.items(), key=lambda kv: -terms[kv[0]]):
        report.append(f"- `{term}` x{terms[term]} -> {filename}:{key}")
    report += ["", "## Resolved by aligning tagged spans", ""]
    for term, sites in sorted(aligned.items(), key=lambda kv: -terms[kv[0]]):
        report.append(f"- `{term}` x{terms[term]} -> {len(sites)} site(s), e.g. {sites[0][0]}:{sites[0][1]}")
    report += ["", "## Mod-specific (translator's choice, must be self-consistent)", ""]
    report += [f"- `{t}` x{terms[t]}" for t in sorted(mod_only, key=lambda t: -terms[t])]
    if unresolved:
        report += ["", "## Unresolved (check by hand)", ""]
        report += [f"- `{t}` x{terms[t]}" for t in sorted(unresolved, key=lambda t: -terms[t])]
    if low_confidence:
        report += ["", "## Left to the translator (no settled base-game wording)", ""]
        for term, langs in sorted(low_confidence.items(), key=lambda kv: -len(kv[1])):
            report.append(f"- `{term}` unsettled in {len(langs)}/{len(languages)} languages")
    if per_language_missing:
        report += ["", "## Gaps per language", ""]
        for language, missing in sorted(per_language_missing.items()):
            report.append(f"- {language}: {', '.join(sorted(set(missing)))}")
    (args.out / "glossary_report.md").write_text("\n".join(report) + "\n", encoding="utf-8")

    print(
        f"{len(direct)} direct, {len(aligned)} aligned, "
        f"{len(mod_only)} mod-specific, {len(unresolved)} unresolved"
    )
    for term in sorted(unresolved, key=lambda t: -terms[t]):
        print(f"  unresolved: {term} (x{terms[term]})")


if __name__ == "__main__":
    main()
