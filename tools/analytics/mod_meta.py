"""What the website needs to know about the mod, read from its own source files.

- Cards: rarity from the base(...) call, themes from the [CardTheme(...)] attribute
  (AlchemistCode/Cards/CardTheme.cs), and the type, cost, text and tags such as Multiplayer from
  cards.csv. The text gets the gold words back from the card's loc text.
- Names: the English titles in Alchemist/localization/eng/. translation() gives every name and text
  in another language, the cards rendered as the game shows them there (card_text.py).
- Images: the card art and the relic, potion, power and badge icons, as paths in the repo. The
  export writes page-sized copies next to the data.
- Links: the mod manifest, the GitHub release for each version, and the Steam Workshop items.
- Patch notes: CHANGELOG.md, one entry per version, so a version the beta and public branches both
  released reads once.
- Previews: the Steam Workshop preview images in workshop/previews/.
- Relics, potions and powers: rarity from the class, text from eng/ with its numbers filled in.
- Badges: the tier thresholds from the badge classes, and their titles from eng/badges.json.
- Epochs: how many there are, which is how many a player needs for the full card pool.

Nothing here is kept in sync by hand. An id follows BaseLib: the mod prefix plus the class name
in upper snake case.

    python3 tools/analytics/mod_meta.py       # print it all
"""

import csv
import json
import os
import re
import subprocess
import sys
from pathlib import Path

import card_text

REPO = Path(__file__).resolve().parents[2]
CODE = REPO / "AlchemistCode"
LOC = REPO / "Alchemist" / "localization"
ENG = LOC / "eng"
# The base game's words in every language (game_loc.py)
GAME_LOC = Path(__file__).with_name("game_loc.json")
IMAGES = REPO / "Alchemist" / "images"
PREFIX = "ALCHEMIST-"
CHARACTER_ICON = IMAGES / "charui" / "character_icon_alchemist.png"
ENERGY_ICON = IMAGES / "charui" / "text_energy.png"

# Every value of the CardTheme enum except None. A theme no card uses is left out of the export
THEMES = ["Poison", "Antitoxin", "Ferment", "Mix", "Transform", "Potions"]
# The game prints these keywords on a line of their own, so no loc text wraps them in [gold]. The
# mod's own keywords come from card_keywords.json
BASE_KEYWORDS = ["Exhaust", "Ethereal", "Innate", "Retain", "Unplayable"]
# The git tag suffix of a beta release (workshop/targets.json tagSuffix)
BETA_SUFFIX = "-beta"

CLASS_RE = re.compile(
    r"\[CardTheme\((?P<themes>[^)]*)\)\]\s*"
    r"public\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(?P<name>\w+)\b", re.S)
NOT_A_CARD_RE = re.compile(r"(?:public|internal)\s+(?:abstract|static)\s+class\s+\w+")
RARITY_RE = re.compile(r"base\([^;]*?CardRarity\.(?P<rarity>\w+)", re.S)
BADGE_CLASS_RE = re.compile(r"class\s+(?P<name>\w+)\(\)\s*:\s*CustomBadge\((?P<args>[^)]*)\)")
TIER_RE = re.compile(r"const\s+int\s+(?P<tier>Bronze|Silver|Gold)\w*\s*=\s*(?P<value>\d+)")
SINGLE_TIER_RE = re.compile(r"const\s+int\s+Min\w*\s*=\s*(?P<value>\d+)")
BADGE_ICON_RE = re.compile(r'"(?P<file>[\w.-]+\.png)"\.BadgeImagePath\(\)')
MARKUP_RE = re.compile(r"\[/?\w+\]")
GOLD_RE = re.compile(r"\[gold\](.*?)\[/gold\]")
# "{Cards:plural:Mix+|Mixes+}" or "{IfUpgraded:show:a|b}": one form or the other
CHOICE_RE = re.compile(r"\{\w+:(?:plural|show):([^{}]*)\}")
UPGRADED_NUMBER_RE = re.compile(r"\((\d+)\)")
RARITY_PROP_RE = re.compile(r"override\s+(?:Relic|Potion)Rarity\s+Rarity\s*=>\s*"
                            r"(?:Relic|Potion)Rarity\.(?P<rarity>\w+)")
BASE_CLASS_RE = re.compile(r"class\s+(?P<name>\w+)\s*(?:\([^)]*\))?\s*:\s*(?P<base>\w+)")


def snake(class_name: str) -> str:
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", class_name).upper()


def entry_for(class_name: str) -> str:
    return PREFIX + snake(class_name)


def plain(text: str) -> str:
    return MARKUP_RE.sub("", text)


def loc_table(table: str, lang: str = "eng") -> dict[str, str]:
    """One loc table in one language. A key the language lacks reads in English, as in the game."""
    found = json.loads((ENG / f"{table}.json").read_text(encoding="utf-8"))
    if lang != "eng" and (path := LOC / lang / f"{table}.json").exists():
        found |= json.loads(path.read_text(encoding="utf-8"))
    return found


def titles(*tables: str, lang: str = "eng") -> dict[str, str]:
    """entry -> title for everything the mod adds, or only for the named loc tables ("relics",
    "potions", ...)."""
    names = {}
    for table in tables or sorted(path.stem for path in ENG.glob("*.json")):
        for key, value in loc_table(table, lang).items():
            if key.endswith(".title") and key.startswith(PREFIX):
                names[key.removesuffix(".title")] = plain(value)
    return names


def languages() -> list[str]:
    """The mod's localization folders, English first."""
    return ["eng"] + sorted(p.name for p in LOC.iterdir() if p.is_dir() and p.name != "eng")


def card_sheet() -> dict[str, dict]:
    """Card title -> its cards.csv row."""
    with (REPO / "cards.csv").open(encoding="utf-8") as f:
        return {row["Card"]: row for row in csv.DictReader(f)}


def gold_words(loc_text: str) -> set[str]:
    """The words a loc string wraps in [gold], with both forms of a plural or an upgrade choice."""
    words = set()
    for span in GOLD_RE.findall(loc_text):
        choice = CHOICE_RE.fullmatch(span)
        words.update(w for w in (choice[1].split("|") if choice else [span]) if "{" not in w)
    return words


def mark_gold(text: str, gold: set[str]) -> str:
    """Wrap the gold words in [gold]. A word also turns gold with a trailing +, as in Mix+."""
    if not gold:
        return text
    words = "|".join(re.escape(w) for w in sorted(gold, key=len, reverse=True))
    return re.sub(rf"(?<![\w+\[])(?:{words})\+?(?![\w\]])", lambda m: f"[gold]{m[0]}[/gold]", text)


def sheet_markup(text: str, gold: set[str]) -> str:
    """cards.csv text in the loc markup: [gold] words, the upgraded number in [green], and [energy]
    for the energy icon."""
    text = UPGRADED_NUMBER_RE.sub(r"[green](\1)[/green]", mark_gold(text, gold))
    return re.sub(r"\bEnergy\b", "[energy]", text)


def costs(cost: str) -> list[str]:
    """ "2 (1)" -> ["2", "1"]: the cost before and after the upgrade."""
    base, _, upgraded = cost.partition(" (")
    return [base, upgraded.rstrip(")") or base]


def repo_path(path: Path) -> str | None:
    return path.relative_to(REPO).as_posix() if path.exists() else None


def portrait(entry: str) -> str | None:
    """The card's art as the game picks it: the final art, else the beta placeholder."""
    file = entry.removeprefix(PREFIX).lower() + ".png"
    return repo_path(IMAGES / "card_portraits" / file) or repo_path(IMAGES / "card_portraits" / "beta" / file)


def icons() -> dict[str, str]:
    """entry -> the icon of every relic, potion and power the mod adds."""
    found = {}
    for folder in ("relics", "potions", "powers"):
        for entry in titles(folder):
            if path := repo_path(IMAGES / folder / (entry.removeprefix(PREFIX).lower() + ".png")):
                found[entry] = path
    return found


def git(*args: str) -> str | None:
    try:
        run = subprocess.run(["git", *args], cwd=REPO, capture_output=True, text=True, check=True)
        return run.stdout.strip() or None
    except (OSError, subprocess.CalledProcessError):
        return None


def repo_slug() -> str | None:
    """owner/name of the GitHub repo, from CI (GitHub Actions or Vercel) or from the origin remote."""
    if slug := os.environ.get("GITHUB_REPOSITORY"):
        return slug
    if os.environ.get("VERCEL_GIT_REPO_OWNER"):
        return f"{os.environ['VERCEL_GIT_REPO_OWNER']}/{os.environ['VERCEL_GIT_REPO_SLUG']}"
    found = re.search(r"github\.com[:/]([^/]+/[^/]+?)(?:\.git)?$", git("remote", "get-url", "origin") or "")
    return found and found[1]


def releases() -> dict[str, str]:
    """Mod version -> the tag of its GitHub release, the beta tag when there is one. Read from the
    remote, so a shallow CI checkout needs no tags."""
    # A build machine can have a checkout with no origin remote, so ask GitHub directly then
    listing = git("ls-remote", "--tags", "origin") or git("ls-remote", "--tags", f"https://github.com/{repo_slug()}")
    tags = {line.rsplit("/", 1)[1] for line in (listing or "").splitlines() if not line.endswith("^{}")}
    found = {}
    for tag in sorted(tags):
        version = tag.removesuffix(BETA_SUFFIX)
        if tag.endswith(BETA_SUFFIX) or version not in found:
            found[version] = tag
    return found


def workshop() -> list[dict]:
    """One Steam Workshop item per game branch, from workshop/targets.json."""
    targets = json.loads((REPO / "workshop" / "targets.json").read_text(encoding="utf-8"))
    return [{"branch": branch, "item": t["itemId"], "title": t["title"], "game_branch": t["gameBranch"]}
            for branch, t in targets.items() if not branch.startswith("_") and t.get("itemId")]


def manifest() -> dict:
    """The mod manifest: name, description and version. The card text is from this version."""
    return json.loads((REPO / "Alchemist.json").read_text(encoding="utf-8-sig"))


def fill_in(text: str) -> str:
    """Loc text with its SmartFormat fields filled in for a page that has no amount to show: an amount
    reads X, a plural or a choice takes its last (many) form, a condition its last (not upgraded)
    branch, and an energy field becomes [energy]."""
    def field(body: str) -> str:
        name, _, rest = body.partition(":")
        if plural := card_text.PLURAL_RE.fullmatch(rest) or card_text.CHOOSE_RE.fullmatch(rest):
            return fill_in(card_text.options(plural["forms"])[-1].replace("{}", "X"))
        if rest.startswith("cond:"):
            options = card_text.options(rest[5:].split("?", 1)[-1])
            return fill_in(options[-1] if len(options) > 1 else "")
        if rest.startswith("energyIcons"):
            count = re.search(r"\((\d*)\)", rest)
            return "[energy]" if count and count[1] == "1" or name == "energyPrefix" else "X [energy]"
        return "X"

    return "".join(field(body) if kind == "field" else body for kind, body in card_text.parts(text))


def class_rarities(folder: str) -> dict[str, str]:
    """Class name -> the Rarity it declares, or the one its base class declares."""
    declared, bases = {}, {}
    for path in (CODE / folder).glob("*.cs"):
        src = path.read_text(encoding="utf-8")
        if cls := BASE_CLASS_RE.search(src):
            bases[cls["name"]] = cls["base"]
            if rarity := RARITY_PROP_RE.search(src):
                declared[cls["name"]] = rarity["rarity"]
    found = {}
    for name in bases:
        klass = name
        while klass in bases and klass not in declared:
            klass = bases[klass]
        if klass in declared:
            found[name] = declared[klass]
    return found


def items(table: str, folder: str | None, lang: str = "eng") -> dict[str, dict]:
    """entry -> {name, rarity, text, flavor, icon} for the relics, potions or powers in <table>.json."""
    loc = loc_table(table, lang)
    rarities = class_rarities(folder) if folder else {}
    by_entry = {entry_for(name): rarity for name, rarity in rarities.items()}
    found = {}
    for entry, name in titles(table, lang=lang).items():
        text = loc.get(f"{entry}.description")
        if text is None:
            continue
        found[entry] = {
            "name": name,
            "rarity": by_entry.get(entry),
            "text": fill_in(text),
            "flavor": plain(loc[f"{entry}.flavor"]) if f"{entry}.flavor" in loc else None,
            "icon": repo_path(IMAGES / table / (entry.removeprefix(PREFIX).lower() + ".png")),
        }
    return found


VERSION_HEADING_RE = re.compile(r"^## \[(?P<version>[^\]]+)\](?: - (?P<date>\S+))?")
BULLET_RE = re.compile(r"^(?P<indent>\s*)- (?P<text>.*)$")


def changelog() -> list[dict]:
    """Every released version in CHANGELOG.md, newest first: {version, date, intro: [paragraph],
    sections: [{title, items: [{text, items}]}]}. Bullets before the first "###" heading form a
    section with no title. A bullet's continuation lines join its text, and an indented bullet goes
    under the one above it."""
    versions: list[dict] = []
    current = section = None
    stack: list[tuple[int, dict]] = []
    paragraph = False
    for line in (REPO / "CHANGELOG.md").read_text(encoding="utf-8").splitlines():
        if heading := VERSION_HEADING_RE.match(line):
            released = heading["version"] != "Unreleased"
            current = None
            if released:
                current = {"version": f"v{heading['version']}", "date": heading["date"], "intro": [], "sections": []}
                versions.append(current)
            section, stack, paragraph = None, [], False
        elif current is None:
            continue
        elif line.startswith("### "):
            section, stack = {"title": line[4:].strip(), "items": []}, []
            current["sections"].append(section)
        elif bullet := BULLET_RE.match(line):
            if section is None:
                section = {"title": None, "items": []}
                current["sections"].append(section)
            item = {"text": bullet["text"].strip(), "items": []}
            while stack and stack[-1][0] >= len(bullet["indent"]):
                stack.pop()
            (stack[-1][1]["items"] if stack else section["items"]).append(item)
            stack.append((len(bullet["indent"]), item))
        elif line.strip() and stack:
            stack[-1][1]["text"] += " " + line.strip()
        elif line.strip() and section is None:
            # Prose under the version heading, before any list
            if paragraph:
                current["intro"][-1] += " " + line.strip()
            else:
                current["intro"].append(line.strip())
            paragraph = True
        else:
            paragraph = False
    return versions


def previews() -> list[str]:
    """The Steam Workshop preview images, in their gallery order."""
    return [path.relative_to(REPO).as_posix() for path in
            sorted((REPO / "workshop" / "previews").glob("*.png"), key=lambda p: int(re.sub(r"\D", "", p.stem) or 0))]


def card_classes():
    """(path, source, [CardTheme] match) for every card class under AlchemistCode/Cards."""
    for path in sorted((CODE / "Cards").glob("*/*.cs")):
        src = path.read_text(encoding="utf-8")
        m = CLASS_RE.search(src)
        if not m and NOT_A_CARD_RE.search(src):
            continue  # a shared base class or a helper, not a card
        if not m:
            raise SystemExit(f"{path.relative_to(REPO)}: no [CardTheme] attribute before the class")
        yield path, src, m


def cards() -> dict[str, dict]:
    """entry -> {name, rarity, themes, tags, type, cost, text, texts, costs, keywords, art} for every
    card class under AlchemistCode/Cards."""
    names, sheet = titles(), card_sheet()
    loc = json.loads((ENG / "cards.json").read_text(encoding="utf-8"))
    keywords = set(BASE_KEYWORDS) | set(titles("card_keywords").values())
    meta: dict[str, dict] = {}
    for path, src, m in card_classes():
        r = RARITY_RE.search(src)
        if not r:
            raise SystemExit(f"{path.relative_to(REPO)}: no CardRarity in the base(...) call")
        themes = [t.strip().removeprefix("CardTheme.") for t in m["themes"].split(",") if t.strip()]
        themes = [t for t in themes if t != "None"]
        if bad := [t for t in themes if t not in THEMES]:
            raise SystemExit(f"{path.relative_to(REPO)}: unknown theme(s) {bad}")
        entry = entry_for(m["name"])
        name = names.get(entry, m["name"])
        # lint_sync.py matches rows loosely; the export needs the exact title
        if not (row := sheet.get(name)):
            raise SystemExit(f"{path.relative_to(REPO)}: no cards.csv row is titled '{name}'")
        gold = keywords | gold_words(loc.get(f"{entry}.description", ""))
        face = card_text.game_text(loc.get(f"{entry}.description", ""), row["Description"], src, keywords)
        if not face["exact"]:
            face["text"] = [re.sub(r"\bEnergy\b", "[energy]", mark_gold(t, gold)) for t in face["text"]]
        meta[entry] = {
            "name": name,
            "rarity": r["rarity"],
            "themes": themes,
            "tags": [t.strip() for t in row.get("Rarity", "").split(",")[1:]],
            "type": row["Type"],
            "cost": sheet_markup(row["Cost"], set()),
            "text": sheet_markup(row["Description"], gold),
            # As the game shows the card and its upgrade
            "texts": face["text"],
            "costs": costs(row["Cost"]),
            "keywords": card_text.keywords(src),
            "art": portrait(entry),
        }
    return meta


def keyword_words(lang: str, game: dict) -> dict:
    """The keyword titles and the period a card's keyword lines use in one language."""
    mine = {name: title for entry, title in titles("card_keywords", lang=lang).items()
            for name in [entry.removeprefix(PREFIX).title()]}
    return {"titles": {**game["keywords"], **mine}, "period": game["period"]}


def localized_cards(lang: str, game: dict, english_cards: dict[str, dict]) -> dict[str, dict]:
    """entry -> {name, texts} as the game shows each card in one language. The numbers come from the
    English template's fields, which every language shares. A card whose text cannot be filled in
    keeps its English text (english_cards is cards())."""
    names, eng_names, sheet = titles(lang=lang), titles(), card_sheet()
    english, loc = loc_table("cards"), loc_table("cards", lang)
    keywords = set(BASE_KEYWORDS) | set(titles("card_keywords").values())
    words, culture = keyword_words(lang, game), card_text.CULTURES[lang]
    found = {}
    for _, src, m in card_classes():
        entry = entry_for(m["name"])
        row = sheet[eng_names.get(entry, m["name"])]
        values = card_text.card_values(english.get(f"{entry}.description", ""), row["Description"], src, keywords)
        template = loc.get(f"{entry}.description", "")
        if values is None and f"{entry}.part_draw" in loc:
            # Compound Mix builds its text in code; an unmade one shows its draw line and "??????"
            template, values = loc[f"{entry}.part_draw"] + "\n??????", card_text.builder_values(src)
        texts = (card_text.texts(template, values, src, culture, words) if values is not None
                 else english_cards[entry]["texts"])
        found[entry] = {"name": names.get(entry, m["name"]), "texts": texts}
    return found


def tips(lang: str, base: dict[str, dict]) -> list[dict]:
    """The definitions the game shows beside a card for the terms its text marks in gold, in one
    language: {id, title, text}. The base game's come from game_loc.json; the mod adds its keywords,
    its Mixes and Brew, and Antitoxin. Ferment has two: one for a card with the keyword, and one
    (_REF) for text about other cards."""
    found = [{"id": key, **tip} for key, tip in base.items()]
    for table in ("card_keywords", "static_hover_tips", "enchantments"):
        loc = loc_table(table, lang)
        found += [{"id": entry, "title": title, "text": loc[f"{entry}.description"]}
                  for entry, title in titles(table, lang=lang).items() if f"{entry}.description" in loc]
    antitoxin = f"{PREFIX}ANTITOXIN_POWER"
    found.append({"id": antitoxin, "title": titles("powers", lang=lang)[antitoxin],
                  "text": loc_table("powers", lang)[f"{antitoxin}.description"]})
    unique: dict[tuple[str, str], dict] = {}
    for tip in found:
        tip = {**tip, "title": plain(tip["title"]), "text": fill_in(tip["text"])}
        unique.setdefault((tip["title"], tip["text"]), tip)
    return list(unique.values())


def translation(lang: str) -> dict:
    """The mod's words in one language: every name, the cards as the game shows them, the relic,
    potion, power and badge text, the hover tips for the gold terms in them, and the base game's own
    words the site uses (game_loc.json)."""
    game = json.loads(GAME_LOC.read_text(encoding="utf-8"))[lang]
    base_tips = game.pop("tips")
    english_cards = cards()
    found = {
        "names": titles(lang=lang),
        "cards": ({entry: {"name": c["name"], "texts": c["texts"]} for entry, c in english_cards.items()}
                  if lang == "eng" else localized_cards(lang, game, english_cards)),
        "badges": {b["id"]: {t["tier"]: {"title": t["title"], "text": t["text"]} for t in b["tiers"]}
                   for b in badges(lang)},
        "game": game,
        "tips": tips(lang, base_tips),
    }
    for table, folder in (("relics", "Relics"), ("potions", "Potions"), ("powers", None)):
        found[table] = {entry: {key: item[key] for key in ("name", "text", "flavor")}
                        for entry, item in items(table, folder, lang).items()}
    return found


def badges(lang: str = "eng") -> list[dict]:
    """One entry per badge class: its id, its icon, whether it needs a win or a co-op run, and its
    tiers (lowest first).

    A tiered badge declares Bronze, Silver and Gold constants. A single-tier badge declares one
    Min constant and counts as Bronze.
    """
    loc = loc_table("badges", lang)
    found = []
    for path in sorted((CODE / "Badges").glob("*.cs")):
        src = path.read_text(encoding="utf-8")
        m = BADGE_CLASS_RE.search(src)
        if not m:
            continue  # a counter or a helper
        entry = entry_for(m["name"])
        tiers = [(t["tier"].lower(), int(t["value"])) for t in TIER_RE.finditer(src)]
        if not tiers and (single := SINGLE_TIER_RE.search(src)):
            tiers = [("bronze", int(single["value"]))]
        if not tiers:
            raise SystemExit(f"{path.relative_to(REPO)}: no Bronze/Silver/Gold or Min threshold")
        icon = BADGE_ICON_RE.search(src)
        found.append({
            "id": entry,
            "icon": icon and repo_path(IMAGES / "badges" / icon["file"]),
            "needs_win": "requiresWin: true" in m["args"],
            "coop_only": "multiplayerOnly: true" in m["args"],
            "tiers": [{
                "tier": tier,
                "at": value,
                "title": plain(loc.get(f"{entry}.{tier}Title") or loc.get(f"{entry}.title", "")),
                "text": plain(loc.get(f"{entry}.{tier}Description") or loc.get(f"{entry}.description", "")),
            } for tier, value in tiers],
        })
    return found


def epoch_count() -> int:
    """How many epochs unlock the full card pool: the length of EpochRegistration.AlchemistEpochTypes."""
    src = (CODE / "Epochs" / "EpochRegistration.cs").read_text(encoding="utf-8")
    block = re.search(r"AlchemistEpochTypes\s*=\s*\{(?P<body>[^}]*)\}", src)
    if not block:
        raise SystemExit("AlchemistCode/Epochs/EpochRegistration.cs: no AlchemistEpochTypes list")
    return len(re.findall(r"typeof\(", block["body"]))


def main() -> int:
    everything = {"cards": cards(), "badges": badges(), "names": titles(), "epochs": epoch_count(),
                  "icons": icons(), "repo": repo_slug(), "releases": releases(), "workshop": workshop(),
                  "relics": items("relics", "Relics"), "potions": items("potions", "Potions"),
                  "powers": items("powers", None),
                  "manifest": manifest()}
    json.dump(everything, sys.stdout, indent=1, sort_keys=True)
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
