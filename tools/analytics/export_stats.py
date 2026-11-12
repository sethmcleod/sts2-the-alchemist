"""Turn the uploaded runs into the count tables the website reads (site/data/).

No raw row, deck or player hash leaves this script. To drop runs by player_hash, list the hashes
in tools/analytics/exclude-players.local.txt or ANALYTICS_EXCLUDE_PLAYERS (comma separated).
"""

import argparse
import json
import os
import re
import sys
from collections import Counter
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

import common
import mod_meta

OUT_DIR = common.REPO / "site" / "data"
EXCLUDE_FILE = common.HERE / "exclude-players.local.txt"

ASCENSION_BANDS = [0, 1, 5, 10]

SKILL_EDGES = [0.15, 0.25, 0.35, 0.5]

RECORD_SIZE = 3

THEME_MIN_CARDS = 3

EXPANSION_TAGS = {"Hero", "Reshuffled Spire", "Ancients Awakened"}

HISTOGRAMS = {
    "run_minutes": (10, 180),
    "poison_peak": (5, 60),
    "antitoxin_peak": (5, 100),
    "mixes": (20, 300),
    "ferment_turns": (25, 400),
    "potions_sold": (1, 8),
}

BADGE_METRICS = {
    "ALCHEMIST-ANTITOXIN_PEAK": "antitoxin_peak",
    "ALCHEMIST-MIXES": "mixes",
    "ALCHEMIST-FERMENTED": "ferment_turns",
    "ALCHEMIST-POTION_SALE": "potions_sold",
    "ALCHEMIST-CLEAN_RUN": "clean_poison",
}
BADGE_TIERS = {"none": 0, "bronze": 1, "silver": 2, "gold": 3}

CARD_KEYS = {"play:": "plays", "fermentplay:": "ferment_plays", "fermentturns:": "ferment_turns"}

MIX_SCHEMA = 2
DETAIL_SCHEMA = 3
REST_SCHEMA = 4


def schema_of(extra: dict) -> int:
    return int(extra.get("schema") or extra.get("mix_keys") or 1)


class Table:
    def __init__(self, key: list[str], counts: list[str]):
        self.key, self.counts = key, counts
        self.index = {name: i for i, name in enumerate(counts)}
        self.rows: dict[tuple, list[int]] = {}

    def add(self, *key, **counts: int) -> None:
        row = self.rows.setdefault(key, [0] * len(self.counts))
        for name, value in counts.items():
            row[self.index[name]] += value

    def to_json(self) -> dict:
        rows = [[*key, *counts] for key, counts in sorted(self.rows.items())]
        return {"key": self.key, "counts": self.counts, "rows": rows}


def new_tables() -> dict[str, Table]:
    g = ["group"]
    return {
        "totals": Table(g, [
            "runs", "wins", "reached_act2", "reached_act3", "seconds", "win_seconds", "deck_size",
            "win_deck_size", "reward_screens", "reward_skips", "brews", "no_brew_runs",
            "potions_sold", "potions_drunk", "runs_with_drinks", "mixes", "runs_with_mixes",
            "poison_gained", "poison_absorbed", "poison_bled", "poison_peak", "runs_with_poison",
            "antitoxin_peak", "runs_with_peak", "runs_with_tally", "wins_with_tally",
            "poison_deaths"]),
        "ascensions": Table(g + ["ascension"], ["runs", "wins"]),
        "days": Table(g + ["day"], ["runs", "wins"]),
        "themes": Table(g + ["theme"], ["runs", "wins"]),
        "badges": Table(g + ["badge", "tier"], ["runs"]),
        "histograms": Table(g + ["metric", "bin"], ["runs", "wins"]),
        "counters": Table(g + ["counter"], ["count", "runs"]),
        "acts": Table(g + ["act"], ["fights", "turns", "damage"]),
        "rest": Table(g + ["act", "choice"], ["picks"]),
        "death_floors": Table(g + ["floor"], ["deaths"]),
        "cards": Table(g + ["card"], [
            "held", "held_wins", "held_twice", "held_twice_wins", "offered", "picked",
            "early_picks", "early_pick_wins", "upgraded", "held_with_plays", "plays",
            "held_never_played", "ferment_plays", "ferment_turns", "held_lift"]),
        "relics": Table(g + ["relic"], ["held", "held_wins", "bought", "offered", "picked"]),
        "potions": Table(g + ["potion"], ["drunk", "bought", "discarded"]),
        "encounters": Table(g + ["encounter"], ["fights", "turns", "damage", "deaths"]),
    }


FILES = {
    "summary.json": ["totals", "ascensions", "days", "themes", "badges", "histograms", "counters",
                     "acts", "death_floors", "rest"],
    "cards.json": ["cards"],
    "relics.json": ["relics", "potions"],
    "fights.json": ["encounters"],
}


SCHEMA_1_LABELS = {"fuming": "acrid", "acrid": "poison"}
RENAMED_MIX_LABELS = {"sturdy": "syrupy"}
RETIRED_MIX_LABELS = {"poison"}


def normalise(extra: dict) -> dict:
    old = schema_of(extra) < MIX_SCHEMA

    def label(kind: str) -> str:
        kind = SCHEMA_1_LABELS.get(kind, kind) if old else kind
        return RENAMED_MIX_LABELS.get(kind, kind)

    out = dict(extra)
    if "mixes" in extra:
        mixes = Counter()
        for kind, count in (extra["mixes"] or {}).items():
            mixes[label(kind)] += int(count or 0)
        out["mixes"] = dict(mixes)
    if "tally" in extra:
        tally = Counter()
        for key, count in (extra["tally"] or {}).items():
            prefix, _, rest = key.partition(":")
            if prefix in ("mixmade", "mixplay"):
                if label(rest) in RETIRED_MIX_LABELS:
                    continue
                key = f"{prefix}:{label(rest)}"
            elif prefix == "pair":
                kinds = sorted(label(kind) for kind in rest.split("+"))
                if RETIRED_MIX_LABELS & set(kinds):
                    continue
                key = "pair:" + "+".join(kinds)
            tally[key] += int(count or 0)
        out["tally"] = dict(tally)
    return out


def version_key(version: str) -> tuple:
    parts = re.findall(r"\d+", version or "")
    return (0, *map(int, parts)) if (version or "").startswith("v") and parts else (1, version)


def ascension_band(ascension: int) -> int:
    return max(band for band in ASCENSION_BANDS if ascension >= band)


def is_coop(run: dict) -> bool:
    return ((run.get("data") or {}).get("numPlayers") or 1) > 1


def skill_cell(run: dict) -> tuple:
    skill = (run.get("data") or {}).get("totalWinRate") or 0
    return ascension_band(int(run["ascension"])), sum(skill >= edge for edge in SKILL_EDGES), is_coop(run)


def residuals(runs: list[dict]) -> list[float]:
    cells: dict[tuple, list[int]] = {}
    for run in runs:
        cell = cells.setdefault(skill_cell(run), [0, 0])
        cell[0] += 1
        cell[1] += int(bool(run["victory"]))
    expected = {key: wins / n for key, (n, wins) in cells.items()}
    return [int(bool(run["victory"])) - expected[skill_cell(run)] for run in runs]


def card_pool(run: dict, extra: dict, epochs: int) -> str:
    if (extra.get("config") or {}).get("enable_epochs") is False:
        return "full"
    earned = int(run.get("epochs") or 0)
    return "full" if earned >= epochs else ("partial" if earned > 0 else "starter")


def uses_expansion(run: dict, extra: dict, expansion_cards: set[str]) -> bool:
    deck = (run.get("data") or {}).get("deck") or []
    return any(card in expansion_cards for card in deck) or any((extra.get("expansions") or {}).values())


def group_of(run: dict, extra: dict, epochs: int, expansion_cards: set[str]) -> tuple:
    return (run["mod_version"], run["game_version"], ascension_band(int(run["ascension"])),
            card_pool(run, extra, epochs), int(is_coop(run)), int(uses_expansion(run, extra, expansion_cards)))


def dominant_theme(deck_themes: dict[str, int]) -> str:
    order = [t.lower() for t in mod_meta.THEMES]
    rank = lambda theme: order.index(theme) if theme in order else len(order)
    counts = {theme: int(n or 0) for theme, n in deck_themes.items()}
    if not counts:
        return "Unfocused"
    best = max(counts, key=lambda theme: (counts[theme], -rank(theme)))
    return best.capitalize() if counts[best] >= THEME_MIN_CARDS else "Unfocused"


def run_metrics(run: dict, win: int, extra: dict) -> dict[str, int]:
    metrics = {"run_minutes": int(run.get("playtime") or 0) // 60,
               "potions_sold": int(extra.get("potions_sold") or 0)}
    if "mixes" in extra:
        metrics["mixes"] = sum(extra["mixes"].values())
    if (peak := (extra.get("antitoxin") or {}).get("peak")) is not None:
        metrics["antitoxin_peak"] = int(peak)
    if schema_of(extra) >= DETAIL_SCHEMA:
        metrics["poison_peak"] = int((extra.get("poison") or {}).get("peak") or 0)
    if "tally" in extra:
        metrics["ferment_turns"] = int(extra["tally"].get("ferment_turns") or 0)
    if "poison" in extra and win:
        poison = extra["poison"]
        metrics["clean_poison"] = int(poison.get("gained") or 0) if not poison.get("bled") else 0
    return metrics


def histogram_bin(metric: str, value: int) -> int:
    width, last = HISTOGRAMS[metric]
    return min(value // width * width, last)


def badge_tier(badge: dict, value: int) -> int:
    return sum(value >= tier["at"] for tier in badge["tiers"])


def add_run(t: dict[str, Table], group: int, run: dict, extra: dict, badges: list[dict], residual: float) -> None:
    data = run.get("data") or {}
    win = int(bool(run["victory"]))
    deck = data.get("deck") or []
    acts_entered = len(data.get("actWins") or [])
    has_tally = "tally" in extra
    tally = extra.get("tally") or {}
    poison = extra.get("poison") or {}
    peak = (extra.get("antitoxin") or {}).get("peak")
    drunk = extra.get("potions_used")
    screens = data.get("cardChoices") or []

    t["totals"].add(
        group,
        runs=1, wins=win,
        reached_act2=int(acts_entered >= 2), reached_act3=int(acts_entered >= 3),
        seconds=int(run.get("playtime") or 0), win_seconds=int(run.get("playtime") or 0) * win,
        deck_size=len(deck), win_deck_size=len(deck) * win,
        reward_screens=len(screens), reward_skips=sum(1 for s in screens if not s.get("picked")),
        brews=int(extra.get("brews") or 0), no_brew_runs=int(not extra.get("brews")),
        potions_sold=int(extra.get("potions_sold") or 0),
        potions_drunk=len(drunk or []), runs_with_drinks=int(drunk is not None),
        mixes=sum(extra.get("mixes", {}).values()), runs_with_mixes=int("mixes" in extra),
        poison_gained=int(poison.get("gained") or 0), poison_absorbed=int(poison.get("absorbed") or 0),
        poison_bled=int(poison.get("bled") or 0), poison_peak=int(poison.get("peak") or 0),
        runs_with_poison=int("poison" in extra),
        antitoxin_peak=int(peak or 0), runs_with_peak=int(peak is not None),
        runs_with_tally=int(has_tally), wins_with_tally=int(has_tally) * win,
        poison_deaths=int(not win and bool(tally.get("poison_death"))),
    )
    t["ascensions"].add(group, int(run["ascension"]), runs=1, wins=win)
    t["days"].add(group, run["created_at"][:10], runs=1, wins=win)
    t["themes"].add(group, dominant_theme(extra.get("deck_themes") or {}), runs=1, wins=win)

    metrics = run_metrics(run, win, extra)
    for metric, value in metrics.items():
        if metric in HISTOGRAMS:
            t["histograms"].add(group, metric, histogram_bin(metric, value), runs=1, wins=win)
    uploaded = extra.get("badges")
    coop = (data.get("numPlayers") or 1) > 1
    for badge in badges:
        if (badge["needs_win"] and not win) or (badge["coop_only"] and not coop):
            continue
        if uploaded is not None and badge["id"] in uploaded:
            tier = BADGE_TIERS.get(str(uploaded[badge["id"]]).lower(), 0)
        elif (metric := BADGE_METRICS.get(badge["id"])) in metrics:
            tier = badge_tier(badge, metrics[metric])
        else:
            continue
        t["badges"].add(group, badge["id"], tier, runs=1)

    per_card = {column: {} for column in CARD_KEYS.values()}
    for key, count in tally.items():
        prefix, colon, label = key.partition(":")
        if column := CARD_KEYS.get(prefix + colon):
            per_card[column][mod_meta.PREFIX + label.upper()] = count
        else:
            t["counters"].add(group, key, count=count, runs=1)
    for act in extra.get("acts") or []:
        number = int(act.get("act") or 0)
        t["acts"].add(group, number, fights=int(act.get("fights") or 0), turns=int(act.get("turns") or 0),
                      damage=int(act.get("damage") or 0))
        for choice, count in (act.get("rest") or {}).items():
            t["rest"].add(group, number, choice, picks=int(count or 0))

    mine = lambda card: card.startswith(mod_meta.PREFIX)
    detail = schema_of(extra) >= DETAIL_SCHEMA
    for card, copies in Counter(deck).items():
        if mine(card):
            t["cards"].add(group, card, held=1, held_wins=win, held_twice=int(copies > 1),
                           held_twice_wins=int(copies > 1) * win, held_lift=round(residual * 1000))
            if detail:
                plays = per_card["plays"].get(card, 0)
                t["cards"].add(group, card, held_with_plays=1, plays=plays, held_never_played=int(not plays),
                               ferment_plays=per_card["ferment_plays"].get(card, 0),
                               ferment_turns=per_card["ferment_turns"].get(card, 0))
    picked_screens = 0
    for screen in screens:
        picked = screen.get("picked") or []
        for card in picked:
            if mine(card):
                t["cards"].add(group, card, offered=1, picked=1)
        for card in screen.get("skipped") or []:
            if mine(card):
                t["cards"].add(group, card, offered=1)
        if picked and picked_screens < 3:
            picked_screens += 1
            for card in picked:
                if mine(card):
                    t["cards"].add(group, card, early_picks=1, early_pick_wins=win)
    for card in data.get("campfireUpgrades") or []:
        if mine(card):
            t["cards"].add(group, card, upgraded=1)

    for relic in set(data.get("relics") or []):
        t["relics"].add(group, relic, held=1, held_wins=win)
    for relic in data.get("relicBuys") or []:
        t["relics"].add(group, relic, bought=1)
    for choice in data.get("ancientChoices") or []:
        if choice.get("picked"):
            t["relics"].add(group, choice["picked"], offered=1, picked=1)
        for relic in choice.get("skipped") or []:
            t["relics"].add(group, relic, offered=1)

    for potion in drunk or []:
        t["potions"].add(group, potion, drunk=1)
    for potion in data.get("potionBuys") or []:
        t["potions"].add(group, potion, bought=1)
    for potion in data.get("potionDiscards") or []:
        t["potions"].add(group, potion, discarded=1)

    killed_by = data.get("killedByEncounter")
    for fight in data.get("encounters") or []:
        t["encounters"].add(group, fight["id"], fights=1, turns=int(fight.get("turns") or 0),
                            damage=int(fight.get("damage") or 0))
    if not win and killed_by:
        t["encounters"].add(group, killed_by, deaths=1)
        t["death_floors"].add(group, int(run["floor"]), deaths=1)


def excluded_players() -> set[str]:
    hashes = set()
    if env := os.environ.get("ANALYTICS_EXCLUDE_PLAYERS"):
        hashes |= {h.strip() for h in env.split(",") if h.strip()}
    if EXCLUDE_FILE.exists():
        hashes |= {line.strip() for line in EXCLUDE_FILE.read_text().splitlines()
                   if line.strip() and not line.startswith("#")}
    return hashes


def secrets_summary(runs: list[dict]) -> str | None:
    unlocked: dict[str, bool] = {}
    for run in runs:
        config = (run.get("alchemist") or {}).get("config") or {}
        if "secrets_unlocked" in config:
            unlocked[run["player_hash"]] = unlocked.get(run["player_hash"], False) or bool(config["secrets_unlocked"])
    if not unlocked:
        return None
    count = sum(unlocked.values())
    return f"Secrets unlocked: {count:,} of {len(unlocked):,} players ({count / len(unlocked):.0%})"


def weekly_players(runs: list[dict]) -> list[dict]:
    weeks: dict[str, set[str]] = {}
    first_week: dict[str, str] = {}
    for run in sorted(runs, key=lambda r: r["created_at"]):
        day = date.fromisoformat(run["created_at"][:10])
        week = (day - timedelta(days=day.weekday())).isoformat()
        weeks.setdefault(week, set()).add(run["player_hash"])
        first_week.setdefault(run["player_hash"], week)
    new = Counter(first_week.values())
    return [{"week": week, "players": len(players), "new": new[week]} for week, players in sorted(weeks.items())]


RECORDS = [
    ("fastest_win", True, False, lambda run, extra: int(run.get("playtime") or 0)),
    ("smallest_winning_deck", True, False, lambda run, extra: len((run.get("data") or {}).get("deck") or [])),
    ("antitoxin_peak", False, True, lambda run, extra: int((extra.get("antitoxin") or {}).get("peak") or 0)),
    ("poison_peak", False, True, lambda run, extra: int((extra.get("poison") or {}).get("peak") or 0)),
    ("mixes", False, True, lambda run, extra: sum((extra.get("mixes") or {}).values())),
    ("ferment_turns", False, True, lambda run, extra: int((extra.get("tally") or {}).get("ferment_turns") or 0)),
    ("potions_sold", False, True, lambda run, extra: int(extra.get("potions_sold") or 0)),
]


def eligible_for_records(run: dict) -> bool:
    data = run.get("data") or {}
    return not is_coop(run) and not data.get("numReloads")


def records(runs: list[dict], extras: list[dict]) -> list[dict]:
    found = []
    pairs = [(run, extra) for run, extra in zip(runs, extras) if eligible_for_records(run)]
    for record, needs_win, highest, value_of in RECORDS:
        entries = []
        for run, extra in pairs:
            if needs_win and not run["victory"]:
                continue
            value = value_of(run, extra)
            if value <= 0:
                continue
            entries.append({"value": value, "version": run["mod_version"], "ascension": int(run["ascension"]),
                            "day": run["created_at"][:10], "won": bool(run["victory"])})
        entries.sort(key=lambda e: (-e["value"] if highest else e["value"], e["day"]))
        found.append({"id": record, "entries": entries[:RECORD_SIZE]})
    return found


def records_summary(runs: list[dict], extras: list[dict]) -> str:
    lines = ["Records (solo runs without a reload):"]
    for record in records(runs, extras):
        entries = ", ".join(f"{e['value']:,} (A{e['ascension']}, {e['version']}, {e['day']}{'' if e['won'] else ', lost'})"
                            for e in record["entries"])
        lines.append(f"  {record['id']}: {entries or 'none'}")
    return "\n".join(lines)


def oldest_version(runs) -> str | None:
    return min((run["mod_version"] for run in runs), key=version_key, default=None)


def build(runs: list[dict], extras: list[dict]) -> dict[str, dict]:
    cards, badges, epochs = mod_meta.cards(), mod_meta.badges(), mod_meta.epoch_count()
    manifest = mod_meta.manifest()
    expansion_cards = {card for card, info in cards.items() if EXPANSION_TAGS & set(info["tags"])}
    keys = [group_of(run, extra, epochs, expansion_cards) for run, extra in zip(runs, extras)]
    groups = sorted(set(keys), key=lambda k: (version_key(k[0]), version_key(k[1]), *k[2:]))
    index = {key: i for i, key in enumerate(groups)}

    tables = new_tables()
    for run, extra, key, residual in zip(runs, extras, keys, residuals(runs)):
        add_run(tables, index[key], run, extra, badges, residual)

    days = sorted({run["created_at"][:10] for run in runs})
    builds = {}
    for run in runs:
        kind = (run.get("data") or {}).get("buildType") or ""
        builds.setdefault(run["game_version"], Counter())[kind] += 1
    meta = {
        "generated_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "total_runs": len(runs),
        "players": len({run["player_hash"] for run in runs}),
        "first_day": days[0],
        "last_day": days[-1],
        "versions": sorted({run["mod_version"] for run in runs}, key=version_key),
        "builds": {build: kinds.most_common(1)[0][0] for build, kinds in
                   sorted(builds.items(), key=lambda kv: version_key(kv[0]))},
        "prefix": mod_meta.PREFIX,
        "themes": [t for t in mod_meta.THEMES if any(t in c["themes"] for c in cards.values())],
        "theme_min_cards": THEME_MIN_CARDS,
        "ascension_bands": ASCENSION_BANDS,
        "epochs": epochs,
        "assets": {
            "character": mod_meta.repo_path(mod_meta.CHARACTER_ICON),
            "energy": mod_meta.repo_path(mod_meta.ENERGY_ICON),
        },
        "mod": {key: manifest[key] for key in ("name", "description", "version")},
        "repo": mod_meta.repo_slug(),
        "releases": mod_meta.releases(),
        "workshop": mod_meta.workshop(),
        "previews": [{"image": path, "full": path} for path in mod_meta.previews()],
        "histograms": {metric: {"width": w, "last": last} for metric, (w, last) in HISTOGRAMS.items()},
        "badges": [b | {"metric": BADGE_METRICS.get(b["id"])} for b in badges],
        "schema_since": {
            str(schema): oldest_version(run for run, extra in zip(runs, extras) if schema_of(extra) >= schema)
            for schema in (DETAIL_SCHEMA, REST_SCHEMA)
        },
        "weekly_players": weekly_players(runs),
    }
    summary = {
        "meta": meta,
        "names": mod_meta.titles(),
        "card_info": cards,
        "relic_info": mod_meta.items("relics", "Relics"),
        "potion_info": mod_meta.items("potions", "Potions"),
        "power_info": mod_meta.items("powers", None),
        "icons": mod_meta.icons(),
        "groups": {"key": ["version", "build", "ascension", "pool", "coop", "expansion"], "counts": [],
                   "rows": [list(key) for key in groups]},
    }
    out = {name: {} for name in FILES}
    out["summary.json"] = summary
    out["notes.json"] = {"versions": mod_meta.changelog()}
    for lang in mod_meta.languages():
        out[f"loc/{lang}.json"] = mod_meta.translation(lang)
    for name, table_names in FILES.items():
        for table in table_names:
            out[name][table] = tables[table].to_json()
    return out


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", type=Path, default=OUT_DIR)
    args = parser.parse_args()

    runs = common.fetch_runs()
    fetched = len(runs)
    if excluded := excluded_players():
        runs = [r for r in runs if r["player_hash"] not in excluded]
    if not runs:
        print(f"No runs to export ({fetched} fetched, all filtered). Leaving the old data.", file=sys.stderr)
        return 1

    args.out.mkdir(parents=True, exist_ok=True)
    shown = lambda path: path.relative_to(common.REPO) if path.is_relative_to(common.REPO) else path
    extras = [normalise(run.get("alchemist") or {}) for run in runs]
    files = build(runs, extras)
    for name, payload in files.items():
        path = args.out / name
        path.parent.mkdir(exist_ok=True)
        path.write_text(json.dumps(payload, separators=(",", ":")) + "\n", encoding="utf-8")
        print(f"wrote {shown(path)} ({path.stat().st_size / 1024:,.0f} KB)")
    print(f"{len(runs):,} runs exported ({fetched:,} fetched)")
    if secrets := secrets_summary(runs):
        print(secrets)
    print(records_summary(runs, extras))
    return 0


if __name__ == "__main__":
    sys.exit(main())
