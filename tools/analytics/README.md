# Run analytics

The mod uploads a summary of each finished run. The site adds them up once a day.

| Step    | Where                      | What                                                   |
| ------- | -------------------------- | ------------------------------------------------------ |
| Upload  | `AlchemistCode/Analytics/` | Posts the game's run summary and the mod's counters    |
| Receive | `site/api/runs.ts`         | Checks the row, adds `id` and `created_at`, queues it  |
| Store   | `site/api/pack.ts`         | Daily: moves the queue to a gzipped file in Vercel Blob |
| Export  | `export_stats.py`          | Turns the runs into count tables in `site/data/`       |
| Show    | `site/`                    | Builds the pages from the count tables                 |

## When a run uploads

The game sends metrics only from a release build with Upload Data on, the full console off, and a
run that is not abandoned and not the player's first. The mod also needs its analytics setting on,
the Alchemist, Standard mode and floor 5 or later.

The game log shows `Analytics for 'Alchemist run' uploaded.`, `Skipping metrics upload`, or a `400`
with the field the site refused.

A row holds no name or Steam ID. The player hash is the first 16 hex digits of a SHA-256 of the
game's anonymous install id.

## A row

| Field              | Holds                                                         |
| ------------------ | ------------------------------------------------------------- |
| Top level          | `mod_version`, `game_version`, `victory`, `ascension`, `floor` |
| `data`             | The game's run summary (`VanillaRunMetrics.cs`)               |
| `alchemist`        | Mixes, Poison, Antitoxin, Brews, deck themes, badges, `tally` |
| `id`, `created_at` | Added by the site                                             |

`tally` is a bag of counts from `RunCounters.Tally`. `play:spike` counts plays of Spike.
`RunCounters.cs` lists every key.

| `schema` | Adds                                                                                  |
| -------- | ------------------------------------------------------------------------------------- |
| 1        | The first client                                                                      |
| 2        | Mix keys that match the card names                                                    |
| 3        | Plays per card, badges, Antitoxin sources, self-Poison peak, Poison dealt, Ferment turns |

## The export

Each run belongs to a group (mod version, game build, ascension band, card pool, solo or
multiplayer). Each table holds counts per group, and the site adds up the groups its filters allow.

```json
{ "key": ["group", "card"], "counts": ["held", "held_wins", "offered", "picked"], "rows": [[12, "ALCHEMIST-THICKEN", 30, 14, 90, 21]] }
```

| File           | Holds                                           |
| -------------- | ----------------------------------------------- |
| `summary.json` | Metadata, mod text, groups, totals, badges, counters |
| `cards.json`   | Per card: held, won, offered, picked, upgraded, played |
| `relics.json`  | Relics and potions                              |
| `fights.json`  | Per encounter: fights, turns, damage, deaths    |
| `notes.json`   | Patch notes from `CHANGELOG.md`                 |

## Add a stat

1. Count it with `RunCounters.Tally(player, "my_key")`.
2. Bump `Schema` in `AlchemistMetrics.cs`.
3. The export puts every tally key in the `counters` table, or in `cards` for `CARD_KEYS`.
4. Read it with `runs.counters(on)` (`site/src/lib/details.ts`) and chart it from
   `site/src/components/charts/`.

A new table needs a column in `new_tables()` and a count in `add_run()`. If a key changes meaning,
bump `Schema` and re-key old rows in `normalise()`.

## Files from the game

Committed, because CI has no game. Rerun after a game update. `<recovered>` is the game recovered
with GDRE Tools.

```bash
python3 tools/analytics/card_frames.py --game <recovered>
python3 tools/analytics/game_loc.py --base <the game's localization folder>
python3 tools/analytics/card_fonts.py --game <recovered>
```

`card_frames.py` needs numpy and Pillow. `card_fonts.py` needs fonttools and brotli, and also
reruns after a localization pass.

## Setup

1. In the Vercel project's Storage tab, create a private Blob store and an Upstash Redis database.
2. Add `CRON_SECRET` and `DEPLOY_HOOK_URL` for Production.
3. Optional: `ANALYTICS_EXCLUDE_PLAYERS`, a comma-separated list of player hashes to skip.

For a local export, put `KV_REST_API_URL`, `KV_REST_API_READ_ONLY_TOKEN` and
`BLOB_READ_WRITE_TOKEN` in `site/.env.local`, and your hashes in `exclude-players.local.txt`.
