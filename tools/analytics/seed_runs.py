"""Fabricate Alchemist runs, so the export and the dashboard can be tried with no real data.

Two modes:
    --local          write the rows to seed-runs.local.json and skip the network (the default)
    --key <anon>     insert them into Supabase through the same insert-only endpoint the DLL uses,
                     which also proves the key and the row level security policy work

`scripts/dev.sh analytics seed` runs the local mode and exports the result. Local rows use made-up
versions (v1.0.0 to v1.0.3) so the version filters have something to show. Inserted rows use
mod_version = "seed-test", which the export skips by default, and this removes them again:

    delete from runs where mod_version = 'seed-test';
"""

import argparse
import hashlib
import json
import random
import sys
from collections import Counter
from datetime import datetime, timedelta, timezone

import common
import export_stats
import mod_meta

LOCAL_OUT = common.HERE / "seed-runs.local.json"

LOCAL_VERSIONS = ["v1.0.0", "v1.0.1", "v1.0.2", "v1.0.3"]
# The fake versions from this one on send the schema 3 counters, like a newer client
SCHEMA_3_FROM = "v1.0.2"
TIER_NAMES = ["none", "bronze", "silver", "gold"]
BUILDS = [("v0.107.1", "public"), ("v0.111.0", "public-beta")]
ENCOUNTERS = {
    1: ["CORPSE_SLUGS_WEAK", "SLUDGE_SPINNER_WEAK", "FUZZY_WURM_NORMAL", "BYRDONIS_ELITE", "CEREMONIAL_BEAST_BOSS"],
    2: ["TERROR_EEL_ELITE", "BOWLBUGS_NORMAL", "DECIMILLIPEDE_ELITE", "KNOWLEDGE_DEMON_BOSS"],
    3: ["SOUL_FYSH_NORMAL", "BYGONE_EFFIGY_ELITE", "AEONGLASS_BOSS"],
}
BASE_RELICS = ["ANCHOR", "BAG_OF_MARBLES", "LANTERN", "PEN_NIB", "ORICHALCUM", "KUNAI", "ICE_CREAM"]
ANCIENT_RELICS = ["NEW_LEAF", "BOOMING_CONCH", "PRECARIOUS_SHEARS", "TOUCH_OF_OROBAS", "LOST_COFFER"]
BASE_POTIONS = ["HEART_OF_IRON", "DEXTERITY_POTION", "SWIFT_POTION", "ENERGY_POTION", "FIRE_POTION"]
MIX_KINDS = ["bursting", "syrupy", "zesty", "fuming", "acrid", "sparkling"]
MIX_BUCKETS = ["0", "1", "2", "3", "4", "5-6", "7-9", "10+"]


def label(entry: str) -> str:
    """The tally label the mod uses for a model: its id without the prefix, lower-cased."""
    return entry.removeprefix(mod_meta.PREFIX).lower()


def fabricate(rng: random.Random, version: str, pool: list[str], power: dict[str, float],
              cards: dict[str, dict], relics: list[str], potions: list[str], badges: list[dict]) -> dict:
    deck = [e for e, m in cards.items() if m["rarity"] == "Basic" for _ in range(3)]
    for _ in range(rng.randint(12, 24)):
        deck.append(rng.choice(pool))

    skill = sum(power.get(c, 0.0) for c in deck) / len(deck) + 0.04 * LOCAL_VERSIONS.index(version)
    ascension = rng.choice([0, 0, 0, 1, 1, 2, 4, 6, 8, 10, 10])
    victory = rng.random() < min(0.85, max(0.1, 0.5 + skill - ascension * 0.025))
    acts = 3 if victory else rng.choice([1, 1, 2, 2, 3])
    floor = rng.randint(48, 50) if victory else rng.randint(6 + 16 * (acts - 1), 16 * acts)
    build, build_type = rng.choice(BUILDS)
    epochs = rng.choice([0, 2, 4, 5, 7, 7, 7])

    fights = [(act, rng.choice(ENCOUNTERS[act])) for act in range(1, acts + 1) for _ in range(rng.randint(4, 7))]
    encounters = [{"id": enc, "turns": rng.randint(2, 9), "damage": rng.randint(0, 25)} for _, enc in fights]
    choices = []
    for _ in range(rng.randint(6, 18)):
        offered = rng.sample(pool, 3)
        picked = [max(offered, key=lambda c: power[c] + rng.random() * 0.4)] if rng.random() < 0.75 else []
        choices.append({"picked": picked, "skipped": [c for c in offered if c not in picked]})

    themes = Counter(t.lower() for c in deck for t in cards[c]["themes"])
    made = {kind: rng.randint(0, 25) for kind in MIX_KINDS} | {"compound": rng.randint(0, 6)}
    gained = rng.randint(20, 260)
    bled = 0 if rng.random() < 0.3 else rng.randint(1, 30)
    plays = rng.randint(0, 50)
    brews = rng.randint(0, 4)
    offers = [rng.sample(potions, 3) for _ in range(brews)]
    tally = {
        "ferment_plays": plays,
        "ferment_turns": sum(rng.choice([0, 1, 2, 3, 4, 6]) for _ in range(plays)),
        "ferment_zero": rng.randint(0, plays // 3),
        "tick_covered": rng.randint(10, 80),
        "tick_bled": rng.randint(0, 12),
        **({"poison_death": 1} if not victory and rng.random() < 0.08 else {}),
        **{f"mixmade:{k}": n for k, n in made.items() if n},
        **{f"mixplay:{k}": round(n * rng.uniform(0.7, 1.0)) for k, n in made.items() if n},
        **{f"mixsrc:{label(rng.choice(pool))}": rng.randint(1, 12) for _ in range(4)},
        **{f"pair:{'+'.join(sorted(rng.sample(MIX_KINDS, 2)))}": 1 for _ in range(made["compound"])},
        **{f"mixfight:{b}": rng.randint(0, 3) for b in MIX_BUCKETS},
    }
    for offer in offers:
        for potion in offer:
            tally[f"brew_offer:{label(potion)}"] = tally.get(f"brew_offer:{label(potion)}", 0) + 1
        tally[f"brew_pick:{label(offer[0])}"] = tally.get(f"brew_pick:{label(offer[0])}", 0) + 1
    newer = LOCAL_VERSIONS.index(version) >= LOCAL_VERSIONS.index(SCHEMA_3_FROM)
    if newer:
        for card, copies in Counter(deck).items():
            if played := copies * rng.choice([0, 1, 2, 3, 5, 8]):
                tally[f"play:{label(card)}"] = played
                if "Ferment" in cards[card]["themes"]:
                    tally[f"fermentplay:{label(card)}"] = played
                    tally[f"fermentturns:{label(card)}"] = played * rng.randint(0, 4)
        for source in [rng.choice(relics), *rng.sample(pool, 3)]:
            tally[f"atxsrc:{label(source)}"] = rng.randint(3, 30)
        tally["atx_decayed"] = rng.randint(5, 40)
        unplayed = sum(made.values()) - sum(n for key, n in tally.items() if key.startswith("mixplay:"))
        tally["mixlost:combined"] = 2 * made["compound"]
        tally["mixlost:leftover"] = max(0, unplayed // 2)
        tally["poison_dealt"] = rng.randint(20, 400)

    act_rows = []
    for act in range(1, acts + 1):
        mine = [e for (a, _), e in zip(fights, encounters) if a == act]
        act_rows.append({"act": act, "fights": len(mine), "turns": sum(e["turns"] for e in mine),
                         "damage": sum(e["damage"] for e in mine)})

    row = {
        "mod_version": version,
        "game_version": build,
        "victory": victory,
        "ascension": ascension,
        "floor": floor,
        "playtime": rng.randint(1200, 4800) + (1500 if victory else 0),
        "player_hash": hashlib.sha256(f"seed-player-{rng.randint(0, 40)}".encode()).hexdigest()[:16],
        "epochs": epochs,
        "created_at": (datetime.now(timezone.utc) - timedelta(
            days=7 * (len(LOCAL_VERSIONS) - 1 - LOCAL_VERSIONS.index(version)) + rng.randint(0, 6),
            hours=rng.randint(0, 23))).isoformat(),
        "data": {
            "ascension": ascension, "win": victory, "floorReached": floor, "numPlayers": 1 if rng.random() < 0.8 else 2,
            "buildType": build_type, "character": "ALCHEMIST-ALCHEMIST", "deck": deck,
            "relics": [rng.choice(relics)] + rng.sample(BASE_RELICS, rng.randint(1, 5)),
            "killedByEncounter": None if victory else encounters[-1]["id"],
            "encounters": encounters, "cardChoices": choices,
            "actWins": [{"act": f"ACT_{a}", "win": a < acts or victory} for a in range(1, acts + 1)],
            "campfireUpgrades": rng.sample(deck, min(len(deck), rng.randint(0, 4))),
            "relicBuys": rng.sample(BASE_RELICS, rng.randint(0, 1)),
            "potionBuys": rng.sample(BASE_POTIONS, rng.randint(0, 1)),
            "potionDiscards": [],
            "ancientChoices": [{"picked": rng.choice(ANCIENT_RELICS[:3]), "skipped": ANCIENT_RELICS[3:]}],
        },
        "alchemist": {
            "epochs": [f"ALCHEMIST-ALCHEMIST{i + 1}_EPOCH" for i in range(epochs)],
            "potions_sold": rng.choice([0, 0, 0, 1, 2, 3, 5]),
            "potions_used": [rng.choice(potions + BASE_POTIONS) for _ in range(rng.randint(2, 12))],
            "brews": brews,
            "deck_themes": dict(themes),
            **({"schema": 3} if newer else {"mix_keys": 2}),
            "mixes": made,
            "poison": {"gained": gained, "absorbed": max(0, gained * 2 - bled), "bled": bled,
                       **({"peak": rng.randint(4, 40)} if newer else {})},
            "antitoxin": {"peak": int(rng.gammavariate(3, 9))},
            "tally": tally,
            "acts": act_rows,
            "config": {"enable_epochs": True, "keep_pools_separate": True},
        },
    }
    if newer:
        row["alchemist"]["badges"] = earned_badges(row, badges)
    return row


def earned_badges(row: dict, badges: list[dict]) -> dict[str, str]:
    """The tier names a newer client would send, read with the export's own rules."""
    win = int(row["victory"])
    metrics = export_stats.run_metrics(row, win, row["alchemist"])
    tiers = {}
    for badge in badges:
        metric = export_stats.BADGE_METRICS.get(badge["id"])
        earned = (win or not badge["needs_win"]) and metric in metrics
        tiers[badge["id"]] = TIER_NAMES[export_stats.badge_tier(badge, metrics[metric]) if earned else 0]
    return tiers


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--runs", type=int, default=400)
    parser.add_argument("--seed", type=int, default=7)
    parser.add_argument("--local", action="store_true", help=f"write to {LOCAL_OUT.name}, no network")
    parser.add_argument("--key", default=None, help="publishable key, to insert into Supabase")
    args = parser.parse_args()

    cards = mod_meta.cards()
    pool = [e for e, m in cards.items() if m["rarity"] in ("Common", "Uncommon", "Rare") and not m["tags"]]
    relics = list(mod_meta.titles("relics"))
    potions = list(mod_meta.titles("potions"))
    badges = mod_meta.badges()
    rng = random.Random(args.seed)
    power = {c: rng.uniform(-0.15, 0.15) for c in pool}
    rows = [fabricate(rng, LOCAL_VERSIONS[min(3, i * 4 // args.runs)], pool, power, cards, relics, potions, badges)
            for i in range(args.runs)]

    if args.local or not args.key:
        LOCAL_OUT.write_text(json.dumps(rows, indent=1))
        print(f"wrote {len(rows)} fabricated runs to {LOCAL_OUT.relative_to(common.REPO)}")
        if not args.local:
            print("(pass --key <publishable key> to insert them into Supabase instead)")
        return 0

    import requests
    for row in rows:
        row["mod_version"] = common.SEED_VERSION
        row.pop("created_at")  # let the database stamp it
    resp = requests.post(common.RUNS_URL, json=rows, timeout=60, headers={
        "apikey": args.key, "Authorization": f"Bearer {args.key}",
        "Content-Type": "application/json", "Prefer": "return=minimal"})
    print(resp.status_code, resp.text[:300])
    return 0 if resp.ok else 1


if __name__ == "__main__":
    sys.exit(main())
