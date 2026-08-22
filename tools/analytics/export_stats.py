"""Export additive aggregate stats for the dashboard (docs/analytics/index.html).

Publishes COUNT tables (runs, wins, offered, picked, ...) keyed by every filter dimension so the
page can recompose any rate client-side by summing. No raw rows, decks, or player hashes leave
this script. Run nightly by .github/workflows/analytics.yml and locally with
`scripts/dev.sh analytics`.

Your own playtest runs would swamp a small dataset. List their player_hash values, one per line, in
tools/analytics/exclude-players.local.txt (gitignored) or the ANALYTICS_EXCLUDE_PLAYERS env var
(comma separated) and they are dropped before aggregation.
"""

import argparse
import json
import os
import sys
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

import common
from card_meta import PREFIX, THEMES, card_meta

OUT_DIR = common.REPO / "docs" / "analytics" / "data"
EXCLUDE_FILE = common.HERE / "exclude-players.local.txt"

# A run commits to the theme with the most Alchemist cards in the final deck (duplicates count).
# Fewer than COMMIT_THRESHOLD such cards is "Unfocused". Ties break in THEMES order
COMMIT_THRESHOLD = 3


def excluded_players() -> set[str]:
    hashes = set()
    if env := os.environ.get("ANALYTICS_EXCLUDE_PLAYERS"):
        hashes |= {h.strip() for h in env.split(",") if h.strip()}
    if EXCLUDE_FILE.exists():
        hashes |= {line.strip() for line in EXCLUDE_FILE.read_text().splitlines()
                   if line.strip() and not line.startswith("#")}
    return hashes


def base_keys(run: dict, day: str, data: dict) -> dict:
    return {"day": day, "mod": run["mod_version"], "game": run["game_version"],
            "asc": int(run["ascension"]), "ep": int(run.get("epochs") or 0),
            "mp": 1 if (data.get("numPlayers") or 1) > 1 else 0}


def aggregate(rows: list[dict], keys: list[str], sums: list[str]) -> list[dict]:
    """Group by keys and sum the measure columns, like a SQL GROUP BY."""
    groups: dict[tuple, dict] = {}
    for row in rows:
        k = tuple(row[key] for key in keys)
        g = groups.get(k)
        if g is None:
            g = groups[k] = dict(zip(keys, k)) | {s: 0 for s in sums}
        for s in sums:
            g[s] += row[s]
    return [groups[k] for k in sorted(groups)]


def copies_bucket(n: int) -> int:
    return min(n, 3)  # 1, 2, 3+ is enough signal and keeps cardinality flat


def dominant_theme(deck_themes: dict[str, int]) -> str:
    """deck_themes comes from the mod itself ({"poison": 9, "infuse": 2, ...})."""
    counts = {t: int(deck_themes.get(t.lower(), 0)) for t in THEMES}
    best = max(THEMES, key=lambda t: (counts[t], -THEMES.index(t)))
    return best if counts[best] >= COMMIT_THRESHOLD else "Unfocused"


def build_tables(runs: list[dict], meta: dict[str, dict]) -> dict[str, list[dict]]:
    run_rows, card_rows, choice_rows, relic_rows = [], [], [], []
    death_rows, floor_rows, enc_rows, theme_rows = [], [], [], []
    brew_rows, first_rows, potion_use_rows = [], [], []

    for run in runs:
        day = run["created_at"][:10]  # ISO timestamp, the date is the first ten characters
        win = int(bool(run["victory"]))
        data = run.get("data") or {}
        extra = run.get("alchemist") or {}
        keys = base_keys(run, day, data)
        deck = data.get("deck") or []

        run_rows.append(keys | {"runs": 1, "wins": win})
        theme_rows.append(keys | {"theme": dominant_theme(extra.get("deck_themes") or {}),
                                  "runs": 1, "wins": win})
        # Reward-screen behaviour comes from the vanilla-shaped cardChoices history, so it
        # covers every run ever uploaded. A screen with no pick is a skip; the first three
        # screens the player picked from are the "first picks" that shape the early game
        screens = data.get("cardChoices") or []
        picked_screens = 0
        skips = 0
        for screen in screens:
            picks = screen.get("picked") or []
            if not picks:
                skips += 1
                continue
            if picked_screens < 3:
                picked_screens += 1
                for card in picks:
                    first_rows.append(keys | {"card": card, "order": picked_screens,
                                              "picked": 1, "wins": win})

        for potion in extra.get("potions_used") or []:
            potion_use_rows.append(keys | {"potion": potion, "uses": 1})

        mixes = extra.get("mixes") or {}
        poison = extra.get("poison") or {}
        brew_rows.append(keys | {"brews": int(extra.get("brews") or 0),
                                 "reward_screens": len(screens),
                                 "reward_skips": skips,
                                 "potions_used": len(extra.get("potions_used") or []),
                                 "sold": int(extra.get("potions_sold") or 0),
                                 "mix_bursting": int(mixes.get("bursting") or 0),
                                 "mix_fuming": int(mixes.get("fuming") or 0),
                                 "mix_syrupy": int(mixes.get("syrupy") or 0) + int(mixes.get("sturdy") or 0),
                                 "mix_zesty": int(mixes.get("zesty") or 0),
                                 "poison_gained": int(poison.get("gained") or 0),
                                 "poison_absorbed": int(poison.get("absorbed") or 0),
                                 "poison_bled": int(poison.get("bled") or 0),
                                 "runs": 1, "wins": win})

        for card, copies in Counter(deck).items():
            if (meta.get(card) or {}).get("rarity") == "Basic":  # forced picks carry no signal
                continue
            card_rows.append(keys | {"card": card, "copies": copies_bucket(copies),
                                     "runs": 1, "wins": win})

        for screen in data.get("cardChoices") or []:
            for card in screen.get("picked") or []:
                choice_rows.append(keys | {"card": card, "offered": 1, "picked": 1})
            for card in screen.get("skipped") or []:
                choice_rows.append(keys | {"card": card, "offered": 1, "picked": 0})

        for relic in set(data.get("relics") or []):
            relic_rows.append(keys | {"relic": relic, "runs": 1, "wins": win})

        killed_by = data.get("killedByEncounter")
        if not win and killed_by:
            death_rows.append(keys | {"enc": killed_by, "deaths": 1})
            floor_rows.append(keys | {"floor": int(run["floor"]), "deaths": 1})

        for enc in data.get("encounters") or []:
            enc_rows.append(keys | {"enc": enc["id"], "fights": 1,
                                    "dmg": int(enc.get("damage") or 0),
                                    "turns": int(enc.get("turns") or 0)})

    keys = ["day", "mod", "game", "asc", "ep", "mp"]
    return {
        "runs_daily": aggregate(run_rows, keys, ["runs", "wins"]),
        "cards_daily": aggregate(card_rows, keys + ["card", "copies"], ["runs", "wins"]),
        "choices_daily": aggregate(choice_rows, keys + ["card"], ["offered", "picked"]),
        "relics_daily": aggregate(relic_rows, keys + ["relic"], ["runs", "wins"]),
        "deaths_daily": aggregate(death_rows, keys + ["enc"], ["deaths"]),
        "death_floors_daily": aggregate(floor_rows, keys + ["floor"], ["deaths"]),
        "encounters_daily": aggregate(enc_rows, keys + ["enc"], ["fights", "dmg", "turns"]),
        "themes_daily": aggregate(theme_rows, keys + ["theme"], ["runs", "wins"]),
        "economy_daily": aggregate(brew_rows, keys, ["brews", "sold", "reward_screens", "reward_skips", "potions_used", "mix_bursting", "mix_fuming", "mix_syrupy", "mix_zesty", "poison_gained", "poison_absorbed", "poison_bled", "runs", "wins"]),
        "first_picks_daily": aggregate(first_rows, keys + ["card", "order"], ["picked", "wins"]),
        "potion_uses_daily": aggregate(potion_use_rows, keys + ["potion"], ["uses"]),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
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
    total_fetched = len(runs)
    if not args.include_seed:
        runs = [r for r in runs if r["mod_version"] != common.SEED_VERSION]
    if excluded := excluded_players():
        runs = [r for r in runs if r["player_hash"] not in excluded]
    if not runs:
        print(f"No runs to export ({total_fetched} fetched, all filtered). Leaving existing data.",
              file=sys.stderr)
        return 1

    meta = card_meta()
    tables = build_tables(runs, meta)
    days = sorted({r["day"] for r in tables["runs_daily"]})
    outputs: dict[str, object] = {f"{name}.json": rows for name, rows in tables.items()}
    outputs["meta.json"] = {
        "generated_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "total_runs": int(len(runs)),
        "players": len({r["player_hash"] for r in runs}),
        "mod_versions": sorted({r["mod_version"] for r in runs}),
        "game_versions": sorted({r["game_version"] for r in runs}),
        "first_day": days[0],
        "last_day": days[-1],
        "themes": THEMES,
        "prefix": PREFIX,
    }
    outputs["cards_meta.json"] = meta

    args.out.mkdir(parents=True, exist_ok=True)
    for name, payload in outputs.items():
        path = args.out / name
        path.write_text(json.dumps(payload, separators=(",", ":"), sort_keys=True) + "\n",
                        encoding="utf-8")
        print(f"wrote {path.relative_to(common.REPO)} ({len(payload)} entries)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
