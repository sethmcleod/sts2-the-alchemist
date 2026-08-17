"""Fabricate Alchemist runs so the export and the dashboard can be exercised before real data exists.

Two modes:
    --local          write the rows to a JSON file and skip the network (default path below)
    --key <anon>     insert them into Supabase through the same insert-only endpoint the DLL uses

Every seeded row has mod_version = "seed-test", so export_stats.py drops it by default and the
table is purged with:  delete from runs where mod_version = 'seed-test';
"""

import argparse
import hashlib
import json
import random
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

import common
from card_meta import THEMES, card_meta

LOCAL_OUT = common.HERE / "seed-runs.local.json"

STARTERS = {"ALCHEMIST-STRIKE_ALCHEMIST": 4, "ALCHEMIST-DEFEND_ALCHEMIST": 4,
            "ALCHEMIST-JAB": 1, "ALCHEMIST-ANTIDOTE": 1}
ENCOUNTERS = ["CULTIST", "JAW_WORM", "TWO_LOUSE", "GREMLIN_GANG", "LAGAVULIN", "GREMLIN_NOB",
              "SENTRIES", "THREE_SENTRIES", "HEXAGHOST", "SLIME_BOSS", "THE_GUARDIAN",
              "CHOSEN", "SHELLED_PARASITE", "SNAKE_PLANT", "BOOK_OF_STABBING", "THE_CHAMP",
              "THE_COLLECTOR", "AUTOMATON", "SPIRE_GROWTH", "GIANT_HEAD", "TIME_EATER",
              "AWAKENED_ONE", "DONU_AND_DECA"]
RELICS = ["BURNING_BLOOD", "VAJRA", "ANCHOR", "BAG_OF_MARBLES", "LANTERN", "PEN_NIB",
          "ORICHALCUM", "KUNAI", "SHURIKEN", "INK_BOTTLE", "GREMLIN_HORN", "MERCURY_HOURGLASS"]


def fabricate(rng: random.Random, pool: list[str], power: dict[str, float], meta: dict) -> dict:
    deck = [c for c, n in STARTERS.items() for _ in range(n)]
    while len(deck) < rng.randint(24, 40):
        deck.append(rng.choice(pool))

    strength = sum(power.get(c, 0.0) for c in deck) / len(deck)
    victory = rng.random() < min(0.9, max(0.1, 0.45 + strength))
    floor = rng.randint(48, 57) if victory else rng.randint(6, 47)
    ascension = rng.choice([0, 0, 0, 1, 2, 3, 5, 8, 10])
    epochs = rng.choice([0, 1, 2, 3, 4, 5, 6, 7, 7, 7])
    fights = max(3, floor // 3)

    encounters = [{"id": rng.choice(ENCOUNTERS), "damage": rng.randint(0, 30),
                   "turns": rng.randint(2, 8)} for _ in range(fights)]
    choices = []
    for _ in range(rng.randint(5, 16)):
        offered = rng.sample(pool, 3)
        picked = [rng.choice(offered)] if rng.random() < 0.7 else []
        choices.append({"picked": picked, "skipped": [c for c in offered if c not in picked]})

    themes: dict[str, int] = {}
    for c in deck:
        for t in (meta.get(c) or {}).get("themes", []):
            themes[t.lower()] = themes.get(t.lower(), 0) + 1

    return {
        "mod_version": common.SEED_VERSION,
        "game_version": "v0.110.1-seed",
        "victory": victory,
        "ascension": ascension,
        "floor": floor,
        "playtime": rng.randint(1200, 7200),
        "player_hash": hashlib.sha256(f"seed-player-{rng.randint(0, 9)}".encode()).hexdigest()[:16],
        "epochs": epochs,
        "created_at": (datetime.now(timezone.utc) - timedelta(days=rng.randint(0, 20),
                                                              hours=rng.randint(0, 23))).isoformat(),
        "data": {
            "ascension": ascension, "win": victory, "floorReached": floor, "numPlayers": 1,
            "character": "ALCHEMIST-ALCHEMIST", "deck": deck,
            "relics": rng.sample(RELICS, rng.randint(2, 7)),
            "killedByEncounter": None if victory else encounters[-1]["id"],
            "encounters": encounters, "cardChoices": choices,
        },
        "alchemist": {
            "epochs": [f"ALCHEMIST-ALCHEMIST{i + 1}_EPOCH" for i in range(epochs)],
            "potions_sold": rng.randint(0, 6), "brews": rng.randint(0, 5),
            "deck_themes": themes,
            "config": {"enable_epochs": True, "keep_pools_separate": True},
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runs", type=int, default=60)
    parser.add_argument("--seed", type=int, default=7)
    parser.add_argument("--local", action="store_true", help=f"write to {LOCAL_OUT.name}, no network")
    parser.add_argument("--key", default=None, help="publishable key, to insert into Supabase")
    args = parser.parse_args()

    meta = card_meta()
    pool = [e for e, m in meta.items() if m["rarity"] in ("Common", "Uncommon", "Rare")]
    rng = random.Random(args.seed)
    power = {c: rng.uniform(-0.25, 0.25) for c in pool}
    rows = [fabricate(rng, pool, power, meta) for _ in range(args.runs)]

    if args.local or not args.key:
        LOCAL_OUT.write_text(json.dumps(rows, indent=1))
        print(f"wrote {len(rows)} fabricated runs to {LOCAL_OUT.relative_to(common.REPO)}")
        if not args.local:
            print("(pass --key <publishable key> to insert them into Supabase instead)")
        return 0

    import requests
    for row in rows:
        row.pop("created_at")  # let the database stamp it
    resp = requests.post(common.RUNS_URL, json=rows, timeout=60, headers={
        "apikey": args.key, "Authorization": f"Bearer {args.key}",
        "Content-Type": "application/json", "Prefer": "return=minimal"})
    print(resp.status_code, resp.text[:300])
    return 0 if resp.ok else 1


if __name__ == "__main__":
    sys.exit(main())
