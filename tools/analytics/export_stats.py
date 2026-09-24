"""Turn the uploaded runs into the count tables the dashboard reads (docs/analytics/).

Every run belongs to one group: its mod version, game build, ascension band, card pool, and
whether it was solo or co-op. Each table adds the run's counts to its group's rows. The page
keeps the groups its filters allow and adds their rows up, so every rate it shows is a sum of
counts over a sum of counts. No raw row, deck or player hash leaves this script.

It writes four files to docs/analytics/data/:

    summary.json  meta, groups, run totals, ascensions, days, themes, badges, histograms,
                  counters, acts, death floors
    cards.json    one row per card per group
    relics.json   one row per relic and per potion per group
    fights.json   one row per encounter per group

Each table is {"key": [...], "counts": [...], "rows": [[...], ...]}. A row lists its key values,
then its counts. Run nightly by .github/workflows/analytics.yml, and locally with
`scripts/dev.sh analytics`.

Your own playtests would swamp a small dataset. List your player_hash values, one per line, in
tools/analytics/exclude-players.local.txt (gitignored) or in the ANALYTICS_EXCLUDE_PLAYERS env
var (comma separated), and they are dropped first.
"""

import argparse
import json
import os
import re
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

import common
import mod_meta

OUT_DIR = common.REPO / "docs" / "analytics" / "data"
EXCLUDE_FILE = common.HERE / "exclude-players.local.txt"

# The ascension filter on the page offers these bands: A0, A1 to A4, A5 to A9, A10 and up
ASCENSION_BANDS = [0, 1, 5, 10]

# A run counts toward the theme with the most Alchemist cards in its final deck, duplicates
# included. Fewer than this many is "Unfocused"
THEME_MIN_CARDS = 3

# Histogram bins as (width, last bin). Each badge threshold must sit on a bin edge
HISTOGRAMS = {
    "run_minutes": (10, 180),
    "poison_peak": (5, 60),
    "antitoxin_peak": (10, 100),
    "mixes": (20, 240),
    "ferment_turns": (25, 400),
    "potions_sold": (1, 8),
}

# A newer client uploads the tier each badge earned. For older rows, the export reads the tier from
# the run total below, with the thresholds from the badge classes (mod_meta.py)
BADGE_METRICS = {
    "ALCHEMIST-ANTITOXIN_PEAK": "antitoxin_peak",
    "ALCHEMIST-MIXES": "mixes",
    "ALCHEMIST-FERMENTED": "ferment_turns",
    "ALCHEMIST-POTION_SALE": "potions_sold",
    "ALCHEMIST-CLEAN_RUN": "clean_poison",
}
BADGE_TIERS = {"none": 0, "bronze": 1, "silver": 2, "gold": 3}

# The per-card tally keys, and the cards table column each one fills. One counter per card would
# make summary.json too big for a phone, so these skip the counters table
CARD_KEYS = {"play:": "plays", "fermentplay:": "ferment_plays", "fermentturns:": "ferment_turns"}

# The client's payload schema: `alchemist.schema`, or `mix_keys` before schema 3. Schema 2 re-keyed
# the Mixes. Schema 3 added plays per card, badges, Antitoxin sources and decay, the self-Poison
# peak, Mix losses, enemy Poison and Ferment turns per card
MIX_SCHEMA = 2
DETAIL_SCHEMA = 3


def schema_of(extra: dict) -> int:
    return int(extra.get("schema") or extra.get("mix_keys") or 1)


class Table:
    """Counts keyed by a tuple. Every column after the key is a count, so rows add up."""

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
        # summary.json
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
        "death_floors": Table(g + ["floor"], ["deaths"]),
        # cards.json. The play and Ferment counts come from schema 3 runs only, and cover the cards
        # in the final deck, so held_with_plays is their denominator
        "cards": Table(g + ["card"], [
            "held", "held_wins", "held_twice", "held_twice_wins", "offered", "picked",
            "early_picks", "early_pick_wins", "upgraded", "held_with_plays", "plays",
            "held_never_played", "ferment_plays", "ferment_turns"]),
        # relics.json
        "relics": Table(g + ["relic"], ["held", "held_wins", "bought", "offered", "picked"]),
        "potions": Table(g + ["potion"], ["drunk", "bought", "discarded"]),
        # fights.json
        "encounters": Table(g + ["encounter"], ["fights", "turns", "damage", "deaths"]),
    }


FILES = {
    "summary.json": ["totals", "ascensions", "days", "themes", "badges", "histograms", "counters",
                     "acts", "death_floors"],
    "cards.json": ["cards"],
    "relics.json": ["relics", "potions"],
    "fights.json": ["encounters"],
}


# ---------- old rows ----------

# Mix keys follow the card names, so a rename moves counts between keys. Clients before schema 2
# used "fuming" for what is now Acrid Mix and "acrid" for the old Poison Mix
SCHEMA_1_LABELS = {"fuming": "acrid", "acrid": "poison"}
RENAMED_MIX_LABELS = {"sturdy": "syrupy"}
# Mix kinds that no longer exist. The per-kind counts drop them so they never show as a
# current kind, but the run's Mix total keeps them because the Mixes badge counted them
RETIRED_MIX_LABELS = {"poison"}


def normalise(extra: dict) -> dict:
    """Bring an old client's `alchemist` object in line with the current Mix keys, the way
    migrate_mix_keys.sql did for the stored rows. A key the client did not send stays absent."""
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


# ---------- one run ----------

def version_key(version: str) -> tuple:
    """v0.9.0 sorts before v0.11.0. Anything that is not a version sorts last."""
    parts = re.findall(r"\d+", version or "")
    return (0, *map(int, parts)) if (version or "").startswith("v") and parts else (1, version)


def ascension_band(ascension: int) -> int:
    return max(band for band in ASCENSION_BANDS if ascension >= band)


def card_pool(run: dict, extra: dict, epochs: int) -> str:
    """"full" when every epoch is earned or the player turned epochs off (which unlocks everything)."""
    if (extra.get("config") or {}).get("enable_epochs") is False:
        return "full"
    earned = int(run.get("epochs") or 0)
    return "full" if earned >= epochs else ("partial" if earned > 0 else "starter")


def group_of(run: dict, extra: dict, epochs: int) -> tuple:
    coop = 1 if ((run.get("data") or {}).get("numPlayers") or 1) > 1 else 0
    return (run["mod_version"], run["game_version"], ascension_band(int(run["ascension"])),
            card_pool(run, extra, epochs), coop)


def dominant_theme(deck_themes: dict[str, int]) -> str:
    """deck_themes comes from the mod ({"poison": 9, "mix": 2, ...}). Ties go to the theme listed
    first in mod_meta.THEMES. An old run may name a theme no card uses any more, which still counts."""
    order = [t.lower() for t in mod_meta.THEMES]
    rank = lambda theme: order.index(theme) if theme in order else len(order)
    counts = {theme: int(n or 0) for theme, n in deck_themes.items()}
    if not counts:
        return "Unfocused"
    best = max(counts, key=lambda theme: (counts[theme], -rank(theme)))
    return best.capitalize() if counts[best] >= THEME_MIN_CARDS else "Unfocused"


def run_metrics(run: dict, win: int, extra: dict) -> dict[str, int]:
    """The run totals the histograms and badges read. A metric is missing when the client that
    uploaded the run did not record it yet."""
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
    """0 for none, then 1, 2, 3 for bronze, silver, gold."""
    return sum(value >= tier["at"] for tier in badge["tiers"])


def add_run(t: dict[str, Table], group: int, run: dict, extra: dict, badges: list[dict]) -> None:
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
        t["acts"].add(group, int(act.get("act") or 0), fights=int(act.get("fights") or 0),
                      turns=int(act.get("turns") or 0), damage=int(act.get("damage") or 0))

    # Cards: only the mod's own. The first three reward screens the player took a card from
    # are the early picks that shape a run
    mine = lambda card: card.startswith(mod_meta.PREFIX)
    detail = schema_of(extra) >= DETAIL_SCHEMA
    for card, copies in Counter(deck).items():
        if mine(card):
            t["cards"].add(group, card, held=1, held_wins=win, held_twice=int(copies > 1),
                           held_twice_wins=int(copies > 1) * win)
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


# ---------- all runs ----------

def excluded_players() -> set[str]:
    hashes = set()
    if env := os.environ.get("ANALYTICS_EXCLUDE_PLAYERS"):
        hashes |= {h.strip() for h in env.split(",") if h.strip()}
    if EXCLUDE_FILE.exists():
        hashes |= {line.strip() for line in EXCLUDE_FILE.read_text().splitlines()
                   if line.strip() and not line.startswith("#")}
    return hashes


def oldest_version(runs) -> str | None:
    return min((run["mod_version"] for run in runs), key=version_key, default=None)


def build(runs: list[dict]) -> dict[str, dict]:
    """The four output files, keyed by file name."""
    cards, badges, epochs = mod_meta.cards(), mod_meta.badges(), mod_meta.epoch_count()
    extras = [normalise(run.get("alchemist") or {}) for run in runs]
    keys = [group_of(run, extra, epochs) for run, extra in zip(runs, extras)]
    groups = sorted(set(keys), key=lambda k: (version_key(k[0]), version_key(k[1]), *k[2:]))
    index = {key: i for i, key in enumerate(groups)}

    tables = new_tables()
    for run, extra, key in zip(runs, extras, keys):
        add_run(tables, index[key], run, extra, badges)

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
        # The Steam branch each game build came from, by the most common build type
        "builds": {build: kinds.most_common(1)[0][0] for build, kinds in
                   sorted(builds.items(), key=lambda kv: version_key(kv[0]))},
        "prefix": mod_meta.PREFIX,
        "themes": [t for t in mod_meta.THEMES if any(t in c["themes"] for c in cards.values())],
        "theme_min_cards": THEME_MIN_CARDS,
        "ascension_bands": ASCENSION_BANDS,
        "epochs": epochs,
        "histograms": {metric: {"width": w, "last": last} for metric, (w, last) in HISTOGRAMS.items()},
        "badges": [b | {"metric": BADGE_METRICS.get(b["id"])} for b in badges],
        # The oldest mod version whose client sends the schema 3 counters. A version's runs all share
        # one schema, so the page selects the runs that carry them by version
        "schema_since": {
            str(DETAIL_SCHEMA): oldest_version(run for run, extra in zip(runs, extras)
                                               if schema_of(extra) >= DETAIL_SCHEMA),
        },
    }
    summary = {
        "meta": meta,
        "names": mod_meta.titles(),
        "card_info": cards,
        "groups": {"key": ["version", "build", "ascension", "pool", "coop"], "counts": [],
                   "rows": [list(key) for key in groups]},
    }
    out = {name: {} for name in FILES}
    out["summary.json"] = summary
    for name, table_names in FILES.items():
        for table in table_names:
            out[name][table] = tables[table].to_json()
    return out


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    common.add_common_args(parser)
    parser.add_argument("--include-seed", action="store_true",
                        help=f"keep fabricated mod_version='{common.SEED_VERSION}' rows")
    parser.add_argument("--out", type=Path, default=OUT_DIR)
    parser.add_argument("--from-file", type=Path, default=None,
                        help="read rows from a JSON file (seed_runs.py --local) instead of Supabase")
    args = parser.parse_args()

    if args.from_file:
        runs = json.loads(args.from_file.read_text())
        args.include_seed = True
    else:
        if not args.key:
            print(common.missing_key_message(), file=sys.stderr)
            return 1
        runs = common.fetch_runs(args.key, args.mod_version, args.game_version, args.days_back)
    fetched = len(runs)
    if not args.include_seed:
        runs = [r for r in runs if r["mod_version"] != common.SEED_VERSION]
    if excluded := excluded_players():
        runs = [r for r in runs if r["player_hash"] not in excluded]
    if not runs:
        print(f"No runs to export ({fetched} fetched, all filtered). Leaving the old data.", file=sys.stderr)
        return 1

    args.out.mkdir(parents=True, exist_ok=True)
    shown = lambda path: path.relative_to(common.REPO) if path.is_relative_to(common.REPO) else path
    for name, payload in build(runs).items():
        path = args.out / name
        path.write_text(json.dumps(payload, separators=(",", ":")) + "\n", encoding="utf-8")
        print(f"wrote {shown(path)} ({path.stat().st_size / 1024:,.0f} KB)")
    print(f"{len(runs):,} runs exported ({fetched:,} fetched)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
