# Run analytics

The Alchemist sends an anonymous summary of each finished run to a small database. A nightly
job adds the runs up, and a static page shows the result at
https://sethmcleod.github.io/sts2-the-alchemist/.

Every part is small and has no build step, so another mod can copy the whole pipeline. See
[Make your own](#make-your-own).

## How it works

| Step    | Where                              | What it does                                                                                                                           |
| ------- | ---------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| Upload  | `AlchemistCode/Analytics/`         | When a run ends, the mod rebuilds the game's own run summary, adds its own counters, and posts one row.                                |
| Store   | Supabase, `schema.sql`             | One table, `runs`. The key in the DLL can insert rows and do nothing else.                                                             |
| Export  | `export_stats.py`                  | Reads the rows with the secret key and writes count tables to `docs/analytics/data/`. No raw row, deck or player hash leaves the script. |
| Show    | `docs/analytics/`                  | A static page in plain HTML, CSS and JavaScript. It adds up the count tables for the filters you pick.                                 |
| Publish | `.github/workflows/analytics.yml` | Runs the export every night and deploys the page to GitHub Pages. Nothing is committed.                                                |

## Try it with fake runs

You need Python 3.12 or later. You do not need Supabase or the game.

```bash
scripts/dev.sh analytics seed
```

```bash
scripts/dev.sh analytics serve
```

Then open http://localhost:8765/. `seed` makes 400 runs over four made-up versions with
`seed_runs.py` and exports them. Run `scripts/dev.sh analytics export` to get the real data back.

## When a run uploads

The game decides first. It raises `ModManager.OnMetricsUpload` only when all of these are true:

- The game is a release build.
- The player's Upload Data setting is on.
- The full console is off.
- The player did not abandon the run, and it is not their first run.

For a modded run, the game raises the hook in place of its own upload to MegaCrit. Then the
mod checks its own rules in `AlchemistMetrics.cs`:

- The Share Anonymous Run Analytics mod setting is on.
- The local player is the Alchemist.
- The run is in Standard mode and got past floor 5.

A row never holds a name or a Steam ID. The player hash is the first 16 hex digits of a
SHA-256 of the game's anonymous install id.

> [!NOTE]
> The hook is silent when a gate is closed. After a run, read the game log. `Uploading Alchemist
> run analytics...` then `Analytics for 'Alchemist run' uploaded.` means it worked. `Skipping
> metrics upload` means the game closed a gate. A `401` means the key or the policy is wrong.

## What a row holds

| Column                                                                                   | Contents                                                                                   |
| ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `mod_version`, `game_version`, `victory`, `ascension`, `floor`, `playtime`, `player_hash`, `epochs` | The values the export filters on.                                                          |
| `data`                                                                                   | The vanilla run summary (`VanillaRunMetrics.cs`), in the shape the game sends to MegaCrit. |
| `alchemist`                                                                              | What vanilla cannot see: epochs, Mixes, Poison and Antitoxin, Brews, deck themes, badges, `tally`. |

The `tally` is an open bag of counts (`RunCounters.Tally`). A key with a colon carries a label,
for example `mixsrc:spike` counts the Mixes that Spike created, and `play:spike` counts the times
the run played Spike. `RunCounters.cs` lists every key. Both columns are `jsonb`, so a new field
never needs a migration.

`badges` holds every badge the mod adds, with the tier the run earned (`none`, `bronze`, `silver`
or `gold`). The client applies the same rule as the game over screen. For older rows without it,
the export reads the tier from the counters with the thresholds from the badge classes.

`schema` says which counters the client sends. Every run of one mod version has the same schema,
so the page picks the runs that carry a newer counter by version. The export names the first
version of each schema in `meta.schema_since`.

| Schema | Adds                                                                                                             |
| ------ | ---------------------------------------------------------------------------------------------------------------- |
| 1      | The first client                                                                                                 |
| 2      | Mix keys that follow the card names (`mix_keys: 2`)                                                              |
| 3      | Plays per card, badges, Antitoxin sources and decay, the self-Poison peak, unplayed Mixes, Poison dealt to enemies, Ferment turns per card |

## The export format

Every run belongs to one group: its mod version, game build, ascension band, card pool, and
solo or co-op. Each table adds the counts of a run to the rows of its group. The page keeps the
groups that its filters allow and adds their rows up. So every rate on the page is a sum of
counts over a sum of counts, and every filter works on every table.

A table lists its key columns, its count columns, and then the rows:

```json
{ "key": ["group", "card"], "counts": ["held", "held_wins", "offered", "picked"], "rows": [[12, "ALCHEMIST-THICKEN", 30, 14, 90, 21]] }
```

`group` is a row number in the `groups` table. The files are:

| File           | Tables                                                                                                   |
| -------------- | -------------------------------------------------------------------------------------------------------- |
| `summary.json` | `meta`, `names`, `card_info`, `icons`, `groups`, `totals`, `ascensions`, `days`, `themes`, `badges`, `histograms`, `counters`, `acts`, `death_floors` |
| `cards.json`   | `cards`: held, won, offered, picked, early picks, rest site upgrades, plays and Ferment turns, per card   |
| `relics.json`  | `relics`: held, bought and Ancient offers, per relic. `potions`: drunk, bought and discarded, per potion |
| `fights.json`  | `encounters`: fights, turns, damage and deaths, per encounter                                            |

The page loads `summary.json` first and each other file when its tab first opens. Together they
are about 1 MB over the network.

`card_info` holds each card's type, cost and text from `cards.csv`. The text is in the game's loc
markup: `mod_meta.py` puts back the `[gold]` words from the card's own loc text and marks the
upgraded numbers `[green]`. The images are not copied. `card_info` and `icons` name a path in the
repo, and the page loads it from `meta.assets.base`, which is GitHub's raw file host at the
commit the export ran on. The images only load when the repo is public.

## Add a new stat

Most new stats need one line in the mod and nothing in the export.

1. Count it in the mod with `RunCounters.Tally(player, "my_key")`. The tally is saved with the
   run, so it survives a save and reload.
2. Bump `Schema` in `AlchemistMetrics.cs`, so the page can tell the runs that count it from the
   runs whose client did not.
3. The export copies every tally key into the `counters` table as it is. The per-card keys in
   `CARD_KEYS` are the exception: one counter per card would make `summary.json` too big, so they
   go to the `cards` table.
4. Read it in `app.js` with `counters(on, 'my_key')`, and draw it with a builder from `charts.js`.

A stat from the vanilla summary, or one that needs its own table, needs a change in
`export_stats.py` too. Add the column in `new_tables()` and count it in `add_run()`.

When a key changes meaning, bump `Schema` too, and re-key the old rows in `normalise()`.
`migrate_mix_keys.sql` is the same move done once in the database.

## Set up the real thing

1. Create a Supabase project. Run `schema.sql` in its SQL editor.
2. Put the project URL and the publishable key in `AlchemistCode/Analytics/AnalyticsEndpoint.cs`,
   and the project URL in `common.py`.
3. In the GitHub repository, set Settings, Pages, Source to GitHub Actions.
4. Add the `SUPABASE_READ_KEY` repository secret: the `sb_secret_...` key from API Keys, never the
   publishable one.
5. Optional: set the `ANALYTICS_EXCLUDE_PLAYERS` variable to a comma-separated list of player
   hashes, to keep your own playtests out. Your hash is in the game log on every upload.

For a local export, write the secret key to `tools/analytics/supabase-service-key.local.txt` and
your hashes to `exclude-players.local.txt`. Both files are gitignored.

> [!IMPORTANT]
> The nightly request also keeps a free Supabase project awake. A project with no request for a
> week pauses, and a paused project drops every upload.

## Make your own

The pipeline knows about the Alchemist only in the places below.

1. Copy `AlchemistCode/Analytics/` into your mod. `VanillaRunMetrics.cs`, `RunMetricsUploader.cs`
   and `AnalyticsEndpoint.cs` work as they are. `RunCounters.cs` works once you delete the fixed
   Alchemist keys.
2. In your copy of `AlchemistMetrics.cs`, change the character check and replace `Payload()`
   with your own counters. Call `Initialize()` and `RunCounters.Register()` from your mod
   initializer.
3. Give players their own switch, like `AlchemistModConfig.AnalyticsEnabled`.
4. Copy `tools/analytics/`, `docs/analytics/` and `.github/workflows/analytics.yml`, then follow
   [Set up the real thing](#set-up-the-real-thing).
5. In `mod_meta.py`, change `PREFIX`, `THEMES`, `CHARACTER_ICON` and the source paths. In
   `export_stats.py`, change `BADGE_METRICS` and `HISTOGRAMS`, or empty them.
6. In `docs/analytics/`, keep `data.js` and `charts.js` as they are. Rewrite the text in
   `index.html` and the tabs in `app.js`.

## Commands

```bash
scripts/dev.sh analytics export                 # pull the real rows and export (needs the secret key)
scripts/dev.sh analytics seed                   # fabricate 400 runs and export them, no network
scripts/dev.sh analytics serve                  # serve the page at http://localhost:8765/
python3 tools/analytics/mod_meta.py             # print what the export reads from the mod
python3 tools/analytics/seed_runs.py --key ...  # insert fake rows through the publishable key
```

Inserted fake rows carry `mod_version = seed-test`. The export skips them unless you pass
`--include-seed`, and `delete from runs where mod_version = 'seed-test';` removes them.
