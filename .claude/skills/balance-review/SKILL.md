---
name: balance-review
description: Review Alchemist run data from the Run History save files and suggest balance changes. Use this skill after a playtest run when someone asks for a review, a "take" on the run, or balance suggestions. Also use it for requests to compare the Alchemist against the base characters, to compute pick rates, or to refresh the baseline. The skill reads the .run files directly, filters to relevant runs, and compares against a base-character baseline.
---

# Review run data and suggest balance changes

The game writes one JSON file per finished run. Read these files directly. Do not ask for
screenshots, and do not scrape the in-game Run History screen. The files hold more than
the screen shows.

## Where the data is

```
~/Library/Application Support/SlayTheSpire2/steam/<steamid>/modded/profileN/saves/history/*.run
```

The `read_run_history` MCP tool (sts2-modding-mcp) reads these with filters. If the tool
is unavailable or too coarse, parse the JSON with a small script. The schema is stable.

## Which runs count

- Ask which profile holds base-character runs and which holds Alchemist playtests, if the
  split is not already clear from the data. The base-character runs are the **baseline**.
  Never mix the two sets.
- **Profile 3** is the automation profile for the regression suite. Always exclude it.
- A run is **relevant** when it reached Act 2 or beyond (the run has entries in
  `map_point_history[1]`), was not abandoned (`was_abandoned` false), and is a standard
  run (`game_mode` == `standard` — exclude daily/custom/multiplayer modes).
- For pick-rate work a cheaper floor filter also works: total floors >= 25.

## What each .run file holds

Top level: `seed`, `win`, `ascension`, `run_time` (seconds), `acts`, `build_id`,
`killed_by_encounter`, `players`, `map_point_history` (one list per act, one entry per
floor). Per floor, `player_stats` has `damage_taken`, `hp_healed`, `current_hp`/`max_hp`,
`current_gold`, `cards_gained/removed`, `upgraded_cards`, `cards_enchanted`,
`card_choices` (each option with `was_picked`), `rest_site_choices`, `potion_used`.
Per floor, `rooms` has `model_id`, `room_type`, and `turns_taken`. The player object has
the final `deck` (with `current_upgrade_level` and `enchantment`) and `relics`.

## The metrics that matter

Compute these for the run under review and for the baseline:

- **Damage economy**: total `damage_taken`, total `hp_healed`, net, and damage per
  floor. This is the Alchemist's signature metric. The design target is a damage-taken
  rate that trends toward the base-character baseline while the sustain identity stays
  visible in the healed totals.
- **Combat pace**: `turns_taken` per fight, split monster/elite/boss. Turns matter;
  wall-clock does not (seconds per turn is playstyle).
- **Pick rates**: aggregate `card_choices` across all relevant Alchemist runs, count
  offered vs picked per card. Flag cards at 0% with 5+ offers and cards above ~70% with
  5+ offers. Strip upgrade suffixes before counting.
- **HP posture**: per-floor `current_hp / max_hp`. Gambit (`IsReduced`) needs HP at or
  below half — report how many floors the player entered below 50% and below 65%, since
  full-HP posture disables the whole Gambit package.
- **Rest-site economy**: `rest_site_choices` counts (SMITH/HEAL/BREW). Heavy HEAL means
  sustain is failing; zero HEAL at high ascension means sustain may be too strong.

## Recompute the baseline every review

The samples are small and both sides grow with every play session. Recompute the baseline
from all relevant base-character runs at the start of every review — the pass over the
files is cheap. Report medians per character and pooled, with the run counts, so the
reader sees the sample size. Recompute the cumulative Alchemist pick rates the same way.
Never quote a stat from an earlier session without recomputing it. If a fresh computation
lands far from the previous review without new runs to explain it, suspect the script,
not the data.

## How to judge

1. **Compare to the baseline, not to zero.** A stat is a finding only when it separates
   from the baseline medians by a clear margin. One run is one seed: call a pattern
   confirmed only when 2+ runs agree, and say so when the sample is thin.
2. **Read pick rates as archetype gravity, not card strength.** A 0% card next to a
   dominant archetype is usually crowded out, not weak. Check whether its archetype
   (poison, ferment, exhaust, Gambit) has any picked cards before proposing a number
   change.
3. **Anchor every suggestion to a floor or a pick.** "Zenith+ at floor 21 is where net
   damage flattened" beats "Zenith seems strong". The per-floor table gives this for
   free.
4. **Group findings by mechanic.** Related cards move together (Regen supply, Gambit
   uptime, poison gravity). Propose one coherent batch per mechanic, with concrete
   numbers for every card touched, so the reader can accept or reject a mechanic at a
   time.
5. **Separate verdicts**: what the data confirms, what it suggests, and what needs
   another run. Keep a watchlist for the next run at the end of the review.

## Design intent to respect

- Gambit should reward playing dangerously at low HP as a nice-to-have, not a
  build-around. The intended lever for Gambit uptime is lower Regen supply, not a
  trigger rework.
- Self-Poison is fuel, not a cost. When the poison loop fails, buff the consumers first.
- Powers scale one amount (the stack). Regen large-stack blowups are the known risk
  pattern; watch doubling effects and per-turn drips together.

## Report format

Lead with the verdict on the run (one sentence). Then damage economy vs baseline, pace,
the floor-anchored findings, pick-rate table (only rows with a finding), suggestions
grouped by mechanic with exact numbers, and the next-run watchlist. A review by itself
edits nothing: suggestions wait for approval, and approved changes go through the
three-way rule (see the **card** skill and CONTRIBUTING.md).
