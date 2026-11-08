"""What the website needs to know about the mod, read from its own source files"""

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
GAME_LOC = Path(__file__).with_name("game_loc.json")
IMAGES = REPO / "Alchemist" / "images"
PREFIX = "ALCHEMIST-"
CHARACTER_ICON = IMAGES / "charui" / "character_icon_alchemist.png"
ENERGY_ICON = IMAGES / "charui" / "text_energy.png"

THEMES = ["Poison", "Antitoxin", "Ferment", "Mix", "Transform", "Potions"]
BASE_KEYWORDS = ["Exhaust", "Ethereal", "Innate", "Retain", "Unplayable"]
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
CHOICE_RE = re.compile(r"\{\w+:(?:plural|show):([^{}]*)\}")
UPGRADED_NUMBER_RE = re.compile(r"\((\d+)\)")
RARITY_PROP_RE = re.compile(r"override\s+(?:Relic|Potion)Rarity\s+Rarity\s*=>\s*"
                            r"(?:Relic|Potion)Rarity\.(?P<rarity>\w+)")
BASE_CLASS_RE = re.compile(r"class\s+(?P<name>\w+)\s*(?:\([^)]*\))?\s*:\s*(?P<base>\w+)")
INT_PROP_RE = re.compile(r"override\s+int\s+(?P<name>\w+)\s*=>\s*(?P<value>\d+)\s*;")


def snake(class_name: str) -> str:
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])", "_", class_name).upper()


def entry_for(class_name: str) -> str:
    return PREFIX + snake(class_name)


def plain(text: str) -> str:
    return MARKUP_RE.sub("", text)


def loc_table(table: str, lang: str = "eng") -> dict[str, str]:
    found = json.loads((ENG / f"{table}.json").read_text(encoding="utf-8"))
    if lang != "eng" and (path := LOC / lang / f"{table}.json").exists():
        found |= json.loads(path.read_text(encoding="utf-8"))
    return found


def titles(*tables: str, lang: str = "eng") -> dict[str, str]:
    names = {}
    for table in tables or sorted(path.stem for path in ENG.glob("*.json")):
        for key, value in loc_table(table, lang).items():
            if key.endswith(".title") and key.startswith(PREFIX):
                names[key.removesuffix(".title")] = plain(value)
    return names


def languages() -> list[str]:
    return ["eng"] + sorted(p.name for p in LOC.iterdir() if p.is_dir() and p.name != "eng")


def card_sheet() -> dict[str, dict]:
    with (REPO / "cards.csv").open(encoding="utf-8") as f:
        return {row["Card"]: row for row in csv.DictReader(f)}


def gold_words(loc_text: str) -> set[str]:
    words = set()
    for span in GOLD_RE.findall(loc_text):
        choice = CHOICE_RE.fullmatch(span)
        words.update(w for w in (choice[1].split("|") if choice else [span]) if "{" not in w)
    return words


def mark_gold(text: str, gold: set[str]) -> str:
    if not gold:
        return text
    words = "|".join(re.escape(w) for w in sorted(gold, key=len, reverse=True))
    return re.sub(rf"(?<![\w+\[])(?:{words})\+?(?![\w\]])", lambda m: f"[gold]{m[0]}[/gold]", text)


def sheet_markup(text: str, gold: set[str]) -> str:
    text = UPGRADED_NUMBER_RE.sub(r"[green](\1)[/green]", mark_gold(text, gold))
    return re.sub(r"\bEnergy\b", "[energy]", text)


def costs(cost: str) -> list[str]:
    m = re.fullmatch(r"(.+?) ?\((.+)\)", cost)
    return [m[1], m[2]] if m else [cost, cost]


def repo_path(path: Path) -> str | None:
    return path.relative_to(REPO).as_posix() if path.exists() else None


def portrait(entry: str) -> str | None:
    file = entry.removeprefix(PREFIX).lower() + ".png"
    return repo_path(IMAGES / "card_portraits" / file) or repo_path(IMAGES / "card_portraits" / "beta" / file)


def beta_portrait(entry: str) -> str | None:
    file = entry.removeprefix(PREFIX).lower() + ".png"
    beta = repo_path(IMAGES / "card_portraits" / "beta" / file)
    return beta if beta != portrait(entry) else None


def icons() -> dict[str, str]:
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
    if slug := os.environ.get("GITHUB_REPOSITORY"):
        return slug
    if os.environ.get("VERCEL_GIT_REPO_OWNER"):
        return f"{os.environ['VERCEL_GIT_REPO_OWNER']}/{os.environ['VERCEL_GIT_REPO_SLUG']}"
    found = re.search(r"github\.com[:/]([^/]+/[^/]+?)(?:\.git)?$", git("remote", "get-url", "origin") or "")
    return found and found[1]


def releases() -> dict[str, str]:
    listing = git("ls-remote", "--tags", "origin") or git("ls-remote", "--tags", f"https://github.com/{repo_slug()}")
    tags = {line.rsplit("/", 1)[1] for line in (listing or "").splitlines() if not line.endswith("^{}")}
    found = {}
    for tag in sorted(tags):
        version = tag.removesuffix(BETA_SUFFIX)
        if tag.endswith(BETA_SUFFIX) or version not in found:
            found[version] = tag
    return found


def workshop() -> list[dict]:
    targets = json.loads((REPO / "workshop" / "targets.json").read_text(encoding="utf-8"))
    return [{"branch": branch, "item": t["itemId"], "title": t["title"], "game_branch": t["gameBranch"]}
            for branch, t in targets.items() if not branch.startswith("_") and t.get("itemId")]


def manifest() -> dict:
    return json.loads((REPO / "Alchemist.json").read_text(encoding="utf-8-sig"))


def fill_in(text: str, values: dict[str, str] | None = None) -> str:
    def field(body: str) -> str:
        name, _, rest = body.partition(":")
        if not rest and values and name in values:
            return values[name]
        if plural := card_text.PLURAL_RE.fullmatch(rest) or card_text.CHOOSE_RE.fullmatch(rest):
            return fill_in(card_text.options(plural["forms"])[-1].replace("{}", "X"), values)
        if rest.startswith("cond:"):
            options = card_text.options(rest[5:].split("?", 1)[-1])
            return fill_in(options[-1] if len(options) > 1 else "", values)
        if rest.startswith("energyIcons"):
            count = re.search(r"\((\d*)\)", rest)
            return "[energy]" if count and count[1] == "1" or name == "energyPrefix" else "X [energy]"
        return "X"

    return "".join(field(body) if kind == "field" else body for kind, body in card_text.parts(text))


def class_int_values(folder: str) -> dict[str, dict[str, str]]:
    return {entry_for(path.stem): {m["name"]: m["value"] for m in INT_PROP_RE.finditer(path.read_text(encoding="utf-8"))}
            for path in (CODE / folder).glob("*.cs")}


def class_rarities(folder: str) -> dict[str, str]:
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
    loc = loc_table(table, lang)
    rarities = class_rarities(folder) if folder else {}
    by_entry = {entry_for(name): rarity for name, rarity in rarities.items()}
    values = class_int_values(folder) if folder else {}
    found = {}
    for entry, name in titles(table, lang=lang).items():
        text = loc.get(f"{entry}.description")
        if text is None:
            continue
        found[entry] = {
            "name": name,
            "rarity": by_entry.get(entry),
            "text": fill_in(text, values.get(entry)),
            "flavor": plain(loc[f"{entry}.flavor"]) if f"{entry}.flavor" in loc else None,
            "icon": repo_path(IMAGES / table / (entry.removeprefix(PREFIX).lower() + ".png")),
        }
    return found


VERSION_HEADING_RE = re.compile(r"^## \[(?P<version>[^\]]+)\](?: - (?P<date>\S+))?")
BULLET_RE = re.compile(r"^(?P<indent>\s*)- (?P<text>.*)$")


def changelog() -> list[dict]:
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
            if paragraph:
                current["intro"][-1] += " " + line.strip()
            else:
                current["intro"].append(line.strip())
            paragraph = True
        else:
            paragraph = False
    return versions


def previews() -> list[str]:
    return [path.relative_to(REPO).as_posix() for path in
            sorted((REPO / "workshop" / "previews").glob("*.png"), key=lambda p: int(re.sub(r"\D", "", p.stem) or 0))]


def card_classes():
    for path in sorted((CODE / "Cards").glob("*/*.cs")):
        src = path.read_text(encoding="utf-8")
        m = CLASS_RE.search(src)
        if not m and NOT_A_CARD_RE.search(src):
            continue
        if not m:
            raise SystemExit(f"{path.relative_to(REPO)}: no [CardTheme] attribute before the class")
        yield path, src, m


def cards() -> dict[str, dict]:
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
            "texts": face["text"],
            "costs": costs(row["Cost"]),
            "keywords": card_text.keywords(src),
            "art": portrait(entry),
            "betaArt": beta_portrait(entry),
        }
    return meta


def keyword_words(lang: str, game: dict) -> dict:
    mine = {name: title for entry, title in titles("card_keywords", lang=lang).items()
            for name in [entry.removeprefix(PREFIX).title()]}
    return {"titles": {**game["keywords"], **mine}, "period": game["period"]}


def localized_cards(lang: str, game: dict, english_cards: dict[str, dict]) -> dict[str, dict]:
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


MOD_WORDS = {"Antitoxin": f"{PREFIX}ANTITOXIN_POWER", "Brew": f"{PREFIX}BREW", "Ferment": f"{PREFIX}FERMENT",
             "Mix": f"{PREFIX}MIX", "Use Beta Art": f"{PREFIX}USE_BETA_ART"}


def translation(lang: str) -> dict:
    game = json.loads(GAME_LOC.read_text(encoding="utf-8"))[lang]
    base_tips = game.pop("tips")
    english_cards = cards()
    names = titles(lang=lang)
    game["words"].update({word: names[entry] for word, entry in MOD_WORDS.items()})
    found = {
        "names": names,
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
    loc = loc_table("badges", lang)
    found = []
    for path in sorted((CODE / "Badges").glob("*.cs")):
        src = path.read_text(encoding="utf-8")
        m = BADGE_CLASS_RE.search(src)
        if not m:
            continue
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
