"""A card's text as the game shows it, and as its upgrade shows it, in any of the mod's languages.

The words come from the card's loc template (eng/cards.json, or another language's). The numbers
come from the card class: each builder call names a template field and gives its base value and its
upgrade (WithDamage(6, 3) fills {Damage}, WithVar("Poison", 2, 1) fills {Poison}). A field no
builder names, such as a calculated amount, is read from cards.csv, which writes each upgradable
number as "6 (9)" in the same order as the English template's fields; every language shares those
fields. Keyword lines come from the card class (WithKeyword, and UpgradeType.Add or Remove), placed
the way CardModel.GetDescriptionForPile places them, in the language's own keyword words.

In English, a card whose text is built in code ({Body}) falls back to the sheet text, with each
"6 (9)" split in two.
"""

import re

TAG_RE = re.compile(r"\[/?\w+\]")
NUMBER = r"(\d+|X)(?: \((\d+|X)\))?"
KEYWORD_RE = re.compile(r"WithKeyword\((?:CardKeyword|AlchemistKeywords)\.(?P<name>\w+)"
                        r"(?:,\s*UpgradeType\.(?P<change>\w+))?\)")
# The builder calls that give a template field its base value and its upgrade
BUILDER_RES = [
    re.compile(r"With(?P<name>Damage|Block|Energy|Cards)\(\s*(?P<base>\d+)\s*,\s*(?P<delta>-?\d+)\s*\)"),
    re.compile(r"With(?:Quiet)?Power<(?P<name>\w+)>\(\s*(?P<base>\d+)\s*,\s*(?P<delta>-?\d+)\s*\)"),
    re.compile(r"WithVar\(\s*\"(?P<name>\w+)\"\s*,\s*(?P<base>\d+)\s*,\s*(?P<delta>-?\d+)\s*\)"),
    # new EnergyVar("Bonus", 1).WithUpgrade(1), new ScryVar(4).WithUpgrade(1)
    re.compile(r"new (?P<kind>\w+)Var\(\s*(?:\"(?P<name>\w+)\"\s*,\s*)?(?P<base>\d+)[^)]*\)"
               r"(?:\s*\.WithUpgrade\(\s*(?P<delta>-?\d+)m?\s*\))?"),
]

# CardKeywordOrder: these go above the text, each inserted at the top in turn, so the last one
# listed ends up first. Laced is a BaseLib keyword that goes above as well
KEYWORDS_ABOVE = ["Ethereal", "Sly", "Retain", "Innate", "Unplayable"]
KEYWORDS_BELOW = ["Exhaust", "Eternal"]
CUSTOM_ABOVE = ["Laced"]
# Fields that only print in combat, or that the card fills from its own state
COMBAT_ONLY = {"InCombat", "OnTable", "IsTargeting", "FormulaDamage", "FormulaHpLoss", "FermentSuffix", "ApplyLine"}


def parts(template: str) -> list[tuple[str, str]]:
    """The template as ("text", ...) and ("field", ...) pieces. A field can hold another field."""
    out, depth, start = [], 0, 0
    for i, ch in enumerate(template):
        if ch == "{":
            if depth == 0:
                out.append(("text", template[start:i]))
                start = i + 1
            depth += 1
        elif ch == "}" and depth:
            depth -= 1
            if depth == 0:
                out.append(("field", template[start:i]))
                start = i + 1
    out.append(("text", template[start:]))
    return out


def options(text: str) -> list[str]:
    """Split "a|b" on the bars that are not inside a nested field."""
    found, depth, start = [], 0, 0
    for i, ch in enumerate(text):
        depth += {"{": 1, "}": -1}.get(ch, 0)
        if ch == "|" and depth == 0:
            found.append(text[start:i])
            start = i + 1
    return found + [text[start:]]


def show_options(fmt: str) -> tuple[str, str]:
    """{IfUpgraded:show:upgraded|base}: the base text can be left out, and then the card shows nothing."""
    upgraded, *base = options(fmt.removeprefix("show:"))
    return upgraded, base[0] if base else ""


# SmartFormat picks a plural form by the rule of the language's culture (LocManager), or of the one
# the field names, as in {Cards:plural(ru):...}. The rules below are SmartFormat's PluralRules
CULTURES = {"eng": "en", "deu": "de", "esp": "es", "fra": "fr", "ind": "id", "ita": "it", "jpn": "ja", "kor": "ko",
            "pol": "pl", "ptb": "pt", "rus": "ru", "spa": "es", "tha": "th", "tur": "tr", "zhs": "zh", "zht": "zh"}
ONE_FORM = {"id", "ja", "ko", "th", "zh"}


def plural_form(lang: str, n: int, count: int) -> int:
    """Which of count forms a language uses for n."""
    if count == 1 or lang in ONE_FORM:
        return 0
    if lang in ("ru", "pl"):
        few = 2 <= n % 10 <= 4 and not 12 <= n % 100 <= 14
        one = n == 1 if lang == "pl" else n % 10 == 1 and n % 100 != 11
        return 0 if one else 1 if few else 2
    if count == 3:
        return 0 if n == 0 else 1 if n == 1 else 2
    return 0 if n == 1 or (lang == "fr" and n == 0) else 1


def literal(text: str) -> str:
    """Template words as a regex that ignores markup, spacing and "a" against "an"."""
    words = re.escape(re.sub(r"\s+", " ", TAG_RE.sub("", text)))
    return re.sub(r"\\ a\\ ", r"\\ an?\\ ", words).replace(r"\ ", r"\s*")


def pattern(template: str) -> tuple[str, list[str]]:
    """A regex for the sheet text of this template, and the field name behind each number group."""
    regex, names = [], []
    for kind, body in parts(template):
        if kind == "text":
            regex.append(literal(body))
            continue
        name, _, fmt = body.partition(":")
        if name in COMBAT_ONLY:
            continue
        if name == "IfUpgraded":
            upgraded, base = (literal(o) for o in show_options(fmt))
            regex.append(f"(?:{base}(?:\\s*\\({upgraded}\\))?|{upgraded})")
        elif fmt.startswith("plural:"):
            forms = [literal(o).replace(r"\{\}", "#").replace(r"\{:diff\(\)\}", "#") for o in options(fmt[7:])]
            numbered = [i for i, form in enumerate(forms) if "#" in form]
            if numbered:
                # One form captures the number, so every field keeps exactly two groups
                names.append(name)
                forms = [f.replace("#", NUMBER if i == numbered[0] else r"(?:\d+|X)") for i, f in enumerate(forms)]
            regex.append("(?:" + "|".join(forms) + ")")
        elif name == "energyPrefix":
            regex.append(r"(?:\s*Energy)?")
        elif fmt.startswith("energyIcons"):
            names.append(name)
            regex.append(f"(?:{NUMBER} )?Energy")
        else:
            names.append(name)
            regex.append(NUMBER)
    return "".join(regex), names


def builder_values(class_src: str) -> dict[str, tuple[str, str]]:
    """Field name -> (base, upgraded) from the card class's builder calls."""
    values = {}
    for regex in BUILDER_RES:
        for m in regex.finditer(class_src):
            name = m["name"] or m.groupdict().get("kind")
            base = int(m["base"])
            values[name] = (str(base), str(base + int(m["delta"] or 0)))
    return values


def fields(template: str) -> set[str]:
    """The fields a template fills from the card's values, nested ones included."""
    names = set()
    for kind, body in parts(template):
        if kind == "text":
            continue
        name, _, fmt = body.partition(":")
        if name == "IfUpgraded":
            for option in show_options(fmt):
                names |= fields(option)
        elif name not in COMBAT_ONLY and name != "energyPrefix":
            names.add(name)
    return names


def sheet_core(text: str, template: str, keywords: set[str]) -> str:
    """The sheet text without what the template does not hold: keyword lines the game adds, and
    upgrade notes such as "(Blend+ makes a basic Mix+)"."""
    plain = TAG_RE.sub("", template)
    kept = [s for s in re.split(r"(?<=\.) ", text)
            if not (re.fullmatch(r"\w+\.", s) and s[:-1] in keywords and s not in plain)]
    core = re.sub(r"\s*\([^()]*\)$", "", " ".join(kept))
    return re.sub(r"(?<=\.)\s+[A-Z][\w' ]*\+ [^.]*\.$", "", core)


def sheet_values(template: str, text: str, keywords: set[str]) -> dict[str, tuple[str, str]] | None:
    """Field name -> (base, upgraded) value, read from the sheet text. None when they do not match."""
    regex, names = pattern(template)
    match = re.fullmatch(regex, sheet_core(text, template, keywords), re.I)
    if not match:
        return None
    values: dict[str, tuple[str, str]] = {}
    groups = match.groups()
    for i, name in enumerate(names):
        base, upgraded = groups[2 * i], groups[2 * i + 1]
        if base and name not in values:
            values[name] = (base, upgraded or base)
    return values


def energy(count: str, changed: bool) -> str:
    """EnergyIconsFormatter: one to three icons in a row, otherwise the number and one icon."""
    if count.isdigit() and 0 < int(count) < 4:
        return "[energy]" * int(count)
    return f"{green(count) if changed else count}[energy]"


def green(text: str) -> str:
    return f"[green]{text}[/green]"


PLURAL_RE = re.compile(r"plural(?:\((?P<lang>\w+)\))?:(?P<forms>.*)", re.S)
# {Amount:choose(1):one card|{} cards}: the option of the listed value, else the one after them
CHOOSE_RE = re.compile(r"choose\((?P<values>[^)]*)\):(?P<forms>.*)", re.S)


def render(template: str, values: dict[str, tuple[str, str]], upgraded: bool, lang: str = "en") -> str:
    """Fill the template the way the game does out of combat. The upgraded card is shown the way the
    game previews an upgrade: a number the upgrade changed turns green (the diff() formatter), and so
    does text only the upgrade shows (ShowIfUpgradedFormatter)."""
    out = []
    for kind, body in parts(template):
        if kind == "text":
            out.append(body)
            continue
        name, _, fmt = body.partition(":")
        base, up = values.get(name, ("1", "1"))
        value, changed = (up, up != base) if upgraded else (base, False)
        if name in COMBAT_ONLY:
            continue
        plural, choose = PLURAL_RE.fullmatch(fmt), CHOOSE_RE.fullmatch(fmt)
        if name == "IfUpgraded":
            upgraded_text, base_text = show_options(fmt)
            shown = render(upgraded_text if upgraded else base_text, values, upgraded, lang)
            out.append(green(shown) if upgraded and shown else shown)
        elif plural:
            forms = options(plural["forms"])
            n = int(value) if value.isdigit() else 2
            form = forms[min(plural_form(plural["lang"] or lang, n, len(forms)), len(forms) - 1)]
            shown = green(value) if changed else value
            out.append(render(form.replace("{:diff()}", shown).replace("{}", value), values, upgraded, lang))
        elif choose:
            keys, forms = choose["values"].split("|"), options(choose["forms"])
            form = forms[min(keys.index(value) if value in keys else len(keys), len(forms) - 1)]
            shown = green(value) if changed else value
            out.append(render(form.replace("{:diff()}", shown).replace("{}", value), values, upgraded, lang))
        elif name == "energyPrefix":
            out.append("[energy]")
        elif fmt.startswith("energyIcons"):
            out.append(energy(value, changed))
        else:
            out.append(green(value) if changed and fmt.startswith("diff") else value)
    return "".join(out)


def keyword_sets(class_src: str) -> tuple[set[str], set[str]]:
    """The card's keywords before and after its upgrade."""
    base, upgraded = set(), set()
    for m in KEYWORD_RE.finditer(class_src):
        if m["change"] != "Add":
            base.add(m["name"])
        if m["change"] != "Remove":
            upgraded.add(m["name"])
    return base, upgraded


def ferments(class_src: str) -> bool:
    return re.search(r"override bool Ferments => true", class_src) is not None


def keywords(class_src: str) -> list[str]:
    """Every keyword the card has, before or after its upgrade, Ferment included."""
    base, upgraded = keyword_sets(class_src)
    return sorted(base | upgraded | ({"Ferment"} if ferments(class_src) else set()))


def with_keywords(text: str, keywords: set[str], ferments: bool, words: dict | None = None) -> str:
    """Put the keyword lines around the text, each the keyword's title in gold and the language's
    period (CardKeywordExtensions.GetCardText). A Ferment card's Retain shares the "Ferment." line
    (FermentInlineRetainPatch), and Laced keeps its period in gold (LacedKeywordPeriodPatch).
    words holds the titles and the period in another language: {"titles": {...}, "period": "."}."""
    titles, period = (words or {}).get("titles", {}), (words or {}).get("period", ".")
    title = lambda keyword: titles.get(keyword, keyword)
    lines = [text] if text else []
    for keyword in KEYWORDS_ABOVE:
        if keyword in keywords:
            lines.insert(0, f"[gold]{title(keyword)}[/gold]{period}")
    for keyword in CUSTOM_ABOVE:
        if keyword in keywords:
            lines.insert(0, f"[gold]{title(keyword)}{period}[/gold]")
    lines += [f"[gold]{title(k)}[/gold]{period}" for k in KEYWORDS_BELOW if k in keywords]
    joined = "\n".join(lines)
    retain = title("Retain")
    return joined.replace(f"[gold]{retain}[/gold]{period}\n", f"[gold]{retain}{period}[/gold] ") if ferments else joined


def split_sheet(text: str, keywords: set[str]) -> tuple[str, str]:
    """The fallback: the sheet text with each "6 (9)" or "Mix (Mix+)" split into base and upgrade,
    its upgrade notes dropped, and its keyword sentences on lines of their own."""
    text = re.sub(r"\s*\([^()]*\+[^()]*\)$|(?<=\.)\s+[A-Z][\w' ]*\+ [^.]*\.$", "", text)
    number = re.compile(r"(\d+|X) \((\d+|X)\)")
    base, upgraded = number.sub(r"\1", text), number.sub(lambda m: green(m[2]), text)

    # "Acrid Mix (Acrid Mix+)": the words in the brackets replace as many words before them
    def swap_words(line: str, pick_upgrade: bool) -> str:
        def one(m: re.Match) -> str:
            before, after = m[1].split(" "), m[2].split(" ")
            kept = " ".join(before[:-len(after)] + (after if pick_upgrade else before[-len(after):]))
            return kept
        return re.sub(r"((?:\S+ )*?\S+) \(([^()\d][^()]*)\)", one, line)

    fix_plural = lambda line: re.sub(r"\b1 (card|turn|time)s\b", r"1 \1", line)

    def keyword_lines(line: str) -> str:
        sentences = re.split(r"(?<=\.) ", line)
        is_keyword = [re.fullmatch(r"\w+\.", x) is not None and x[:-1] in keywords for x in sentences]
        lines, prose = [], []
        for sentence, keyword in zip(sentences, is_keyword):
            if keyword:
                if prose:
                    lines.append(" ".join(prose))
                    prose = []
                lines.append(sentence)
            else:
                prose.append(sentence)
        return "\n".join(lines + ([" ".join(prose)] if prose else []))

    return keyword_lines(fix_plural(swap_words(base, False))), keyword_lines(swap_words(upgraded, True))


def card_values(template: str, sheet_text: str, class_src: str, keywords: set[str]) -> dict | None:
    """Field name -> (base, upgraded) for every field the English template fills, or None when the
    text is built in code ({Body}) or a field has no value."""
    values = {**(sheet_values(template, sheet_text, keywords) or {}), **builder_values(class_src)}
    if template.strip().startswith("{Body}") or not fields(template) <= values.keys():
        return None
    return values


def texts(template: str, values: dict, class_src: str, lang: str = "en", words: dict | None = None) -> list[str]:
    """The card's text and its upgrade's, keyword lines included, in the template's language."""
    base_keywords, upgraded_keywords = keyword_sets(class_src)
    return [with_keywords(render(template, values, False, lang), base_keywords, ferments(class_src), words),
            with_keywords(render(template, values, True, lang), upgraded_keywords, ferments(class_src), words)]


def game_text(template: str, sheet_text: str, class_src: str, keywords: set[str]) -> dict:
    """{"text": [base, upgraded], "exact": bool} for one card in English."""
    values = card_values(template, sheet_text, class_src, keywords)
    if values is None:
        return {"text": list(split_sheet(sheet_text, keywords)), "exact": False}
    return {"text": texts(template, values, class_src), "exact": True}
