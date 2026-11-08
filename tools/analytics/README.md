# Run analytics

The mod sends an anonymous summary of each finished run to alchemist.fyi. The site adds them up
once a day and shows the stats next to every card, relic, potion and power.

## How it works

| Step    | Where                      | What it does                                                                 |
| ------- | -------------------------- | ---------------------------------------------------------------------------- |
| Upload  | `AlchemistCode/Analytics/` | When a run ends, posts the game's run summary plus the mod's own counters.   |
| Receive | `site/api/runs.ts`         | Checks the row, stamps an `id` and `created_at`, and queues it in Redis.     |
| Store   | `site/api/pack.ts`         | Once a day, moves the queue into a gzipped file in Vercel Blob and rebuilds. |
| Export  | `export_stats.py`          | Turns the stored runs into count tables in `site/data/`. No raw rows leave.  |
| Show    | `site/`                    | Builds pages from the count tables and sums them for each filter.            |

## Try it with fake runs

No keys or game needed, just Python 3.12+ and Node 22.12+.

```bash
scripts/dev.sh analytics seed
```

```bash
scripts/dev.sh site dev
```

`seed` makes 400 runs across four made-up versions and exports them. Run
`scripts/dev.sh analytics export` to get the real data back (needs the credentials below).

## When a run uploads

The game only raises `ModManager.OnMetricsUpload` when it's a release build, Upload Data is on,
the full console is off, and the run wasn't abandoned or the player's first. Then
`AlchemistMetrics.cs` also checks that the mod's analytics setting is on, the player is the
Alchemist, and the run is Standard mode past floor 5.

A row never holds a name or Steam ID. The player hash is the first 16 hex digits of a SHA-256 of
the game's anonymous install id.

> [!NOTE]
> The mod hears nothing when the game skips a run. After a run, the game log shows
> `Analytics for 'Alchemist run' uploaded.` when it worked, `Skipping metrics upload` when the game
> skipped it, and a `400` with the field the site refused.

## What a row holds

| Column                 | Contents                                                                    |
| ---------------------- | --------------------------------------------------------------------------- |
| Top-level fields       | `mod_version`, `game_version`, `victory`, `ascension`, `floor`, and so on   |
| `data`                 | The vanilla run summary (`VanillaRunMetrics.cs`), as the game sends it      |
| `alchemist`            | Mixes, Poison, Antitoxin, Brews, deck themes, badges, and the `tally`       |
| `id`, `created_at`     | Added by the site                                                           |

The `tally` is an open bag of counts (`RunCounters.Tally`). A key with a colon carries a label:
`play:spike` counts plays of Spike. `RunCounters.cs` lists every key.

`schema` says which counters a client sends, so the site can pick only the runs that have a newer
counter.

| Schema | Adds                                                                                          |
| ------ | --------------------------------------------------------------------------------------------- |
| 1      | The first client                                                                              |
| 2      | Mix keys that follow the card names                                                           |
| 3      | Plays per card, badges, Antitoxin sources and decay, self-Poison peak, unplayed Mixes, Poison dealt, Ferment turns per card |

## The export

Every run belongs to a group (mod version, game build, ascension band, card pool, solo or co-op),
and each table holds counts per group. The site keeps the groups its filters allow and adds them
up, so every rate is a sum of counts over a sum of counts.

```json
{ "key": ["group", "card"], "counts": ["held", "held_wins", "offered", "picked"], "rows": [[12, "ALCHEMIST-THICKEN", 30, 14, 90, 21]] }
```

| File           | Holds                                                         |
| -------------- | ------------------------------------------------------------- |
| `summary.json` | Metadata, the mod's text, groups, totals, badges, counters    |
| `cards.json`   | Held, won, offered, picked, upgraded and played, per card     |
| `relics.json`  | Relics and potions                                            |
| `fights.json`  | Fights, turns, damage and deaths, per encounter               |
| `notes.json`   | The patch notes from `CHANGELOG.md`                           |

## Files from the game

These come from the game files, so they're committed (CI has no game). Rerun them after a game
update.

- `card_frames.py` builds the card frames in `site/src/assets/frames/`. Needs `numpy` and Pillow.

  ```bash
  python3 tools/analytics/card_frames.py --game <recovered project>
  ```

- `game_loc.py` writes `game_loc.json`, the base game's own words in each language.

  ```bash
  python3 tools/analytics/game_loc.py --base <the game's localization folder>
  ```

- `card_fonts.py` cuts the card fonts down to the characters the cards use. Rerun it after a
  localization pass too. Needs `fonttools` and `brotli`.

  ```bash
  python3 tools/analytics/card_fonts.py --game <recovered project>
  ```

`<recovered project>` is the game recovered with GDRE Tools.

## Add a new stat

1. Count it with `RunCounters.Tally(player, "my_key")`. The tally is saved with the run.
2. Bump `Schema` in `AlchemistMetrics.cs`.
3. The export copies every tally key into the `counters` table as is. Per-card keys (`CARD_KEYS`)
   go to the `cards` table instead.
4. Read it on the site with `runs.counters(on)` (see `site/src/lib/details.ts`) and draw it with a
   chart from `site/src/components/charts/`.

A stat that needs its own table also needs a column in `new_tables()` and a count in `add_run()`.
If a key changes meaning, bump `Schema` and re-key the old rows in `normalise()`.

## Setup

1. In the Vercel project's Storage tab, create a private Blob store and an Upstash Redis database,
   and connect both.
2. Add `CRON_SECRET` (a long random string) and `DEPLOY_HOOK_URL` (a deploy hook from the Git
   settings) for Production.
3. Optional: set `ANALYTICS_EXCLUDE_PLAYERS` to a comma-separated list of player hashes to skip.
   Your hash is in the game log on every upload.

For a local export, copy `KV_REST_API_URL`, `KV_REST_API_READ_ONLY_TOKEN` and
`BLOB_READ_WRITE_TOKEN` into `site/.env.local`, and your hashes into `exclude-players.local.txt`.
Both are gitignored.

To test the endpoint end to end, post made-up runs. The export always skips them.

```bash
python3 tools/analytics/seed_runs.py --runs 3 --post https://alchemist.fyi/api/runs
```
