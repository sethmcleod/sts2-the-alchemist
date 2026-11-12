#!/usr/bin/env python3
"""Build a per-language glossary of the base-game terms the mod's text tags"""
import argparse
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

MOD_ENG = Path(__file__).resolve().parent.parent / "Alchemist" / "localization" / "eng"

TAGGED_RE = re.compile(r"\[(gold|purple|blue|green|red)\](.*?)\[/\1\]")
ANY_TAG_RE = re.compile(r"\[/?[a-z_]+\]")

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

# No participles: "Poisoned" has a different root from "Poison" in several languages
ALIASES = {
    "potion(s)": "potions", "spire's": "spire", "distillates": "distillate",
    "hands": "hand",
}

# Lowercase gold and poison are the metal and the substance, not the currency and the status
AMBIGUOUS = {"gold", "poison"}

AMBIGUOUS_ANY_CASE = {"discard", "exhausts"}

OVERRIDES = {
    ("Discard Pile", "esp"): "pila de descarte",
    ("Exhaust", "rus"): "Сжигается|сжечь|сожженную",
    ("Fatal", "tur"): "Öldürürse",
}


def strip_tags(text: str) -> str:
    return ANY_TAG_RE.sub("", text).strip()


def tagged_terms(directory: Path) -> Counter:
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
    wanted = term.lower()
    best = None
    for filename, table in eng.items():
        for key, value in table.items():
            if not isinstance(value, str):
                continue
            if strip_tags(value).lower() == wanted:
                rank = (
                    0 if filename in ("static_hover_tips.json", "card_keywords.json")
                    else 1 if key.endswith(".title") else 2
                )
                if best is None or rank < best[0]:
                    best = (rank, filename, key)
    return (best[1], best[2]) if best else None


def resolve_aligned(eng: dict, term: str, limit: int = 60) -> list[tuple[str, str, int, int]]:
    wanted = term.lower()
    hits = []
    for filename, table in eng.items():
        for key, value in table.items():
            if not isinstance(value, str):
                continue
            spans = [strip_tags(s[1]).lower() for s in TAGGED_RE.findall(value)]
            if spans.count(wanted) != 1:
                continue
            hits.append((filename, key, spans.index(wanted), len(spans)))
            if len(hits) >= limit:
                return hits
    return hits


AGREEMENT = 0.6


def translate_aligned(
    tables: dict, sites: list[tuple[str, str, int, int]]
) -> tuple[str, float] | None:
    votes = Counter()
    for filename, key, index, span_count in sites:
        value = tables.get(filename, {}).get(key)
        if not isinstance(value, str):
            continue
        spans = TAGGED_RE.findall(value)
        if len(spans) != span_count:
            continue
        span = strip_tags(spans[index][1])
        if span:
            votes[span] += 1
    if not votes:
        return None
    winner, count = votes.most_common(1)[0]
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
        candidates = [lowered] if lowered != term.lower() else [term]
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
        for (term, lang), wording in OVERRIDES.items():
            if lang == language:
                glossary[term] = wording
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
