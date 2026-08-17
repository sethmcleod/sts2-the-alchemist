# Run analytics

Anonymous run telemetry from players, aggregated nightly into a public dashboard.

## How it flows

1. **Client.** `AlchemistCode/Analytics/AlchemistMetrics.cs` subscribes to the game's own
   `ModManager.OnMetricsUpload` hook. The game raises it only on a release build, with the
   player's "Upload Data" setting on, full console off, the run not abandoned, and not their
   first run. For a modded run it raises the hook *instead of* uploading to MegaCrit. The mod
   adds its own `Share Run Analytics` toggle in the mod config, keeps only Alchemist runs in
   Standard mode past floor 5, rebuilds the vanilla `RunMetrics` payload, and POSTs one row.
2. **Storage.** One Supabase table, `runs` (`schema.sql`). Promoted columns for filters, the
   vanilla payload in `data`, and an `alchemist` object with what vanilla cannot see: unlocked
   epochs, potions sold, Brews taken, deck theme counts, and the gameplay config values. The
   publishable key in the DLL is insert-only under row level security.
3. **Export.** `export_stats.py` pulls the rows with the secret key and writes additive
   count tables to `docs/analytics/data/`. No raw rows, decks, or player hashes leave the
   script.
4. **Dashboard.** `docs/analytics/index.html`, plain HTML and SVG with no dependencies. It
   recomposes every rate client-side by summing the slices the filters select.
5. **Publish.** `.github/workflows/analytics.yml` runs the export nightly and deploys
   `docs/analytics/` to GitHub Pages. Nothing is committed. The nightly request is also what
   keeps the free Supabase project from pausing after a week idle.

## One-time setup

- Supabase: run `schema.sql` in the SQL editor. Copy the publishable key into
  `AlchemistCode/Analytics/AnalyticsEndpoint.cs`.
- Repository: `Settings -> Pages -> Source: GitHub Actions`; add the `SUPABASE_READ_KEY`
  secret (the `sb_secret_…` key from API Keys, or the legacy service_role key; both bypass RLS). Optionally set the `ANALYTICS_EXCLUDE_PLAYERS` variable to a
  comma-separated list of `player_hash` values to keep your own playtests out.
- Locally: write the secret key to `tools/analytics/supabase-service-key.local.txt`
  (gitignored), and your own hashes to `exclude-players.local.txt`. Your hash prints in the
  game log on every upload.

## Commands

```bash
scripts/dev.sh analytics seed     # fabricate 60 runs and export them, no network
scripts/dev.sh analytics serve    # open the dashboard at http://localhost:8765/
scripts/dev.sh analytics export   # pull real rows and export (needs the read key)
python3 tools/analytics/card_meta.py           # print entry, rarity, themes per card
python3 tools/analytics/seed_runs.py --key ...  # insert fabricated rows into Supabase
```

Fabricated rows carry `mod_version = seed-test`. The export drops them unless
`--include-seed` is passed, and `delete from runs where mod_version = 'seed-test';` purges
them.

## Checking that uploads fire

The hook is silent when a gate is closed. Read the game log after a run ends:

- `Skipping metrics upload, user has enabled full console` means the dev console blocks it.
- `Skipping metrics upload, this is a debug build` means a non-release build.
- `Uploading Alchemist run analytics...` then `Analytics for 'Alchemist run' uploaded.` is
  success; a `failed with 401` means the key or the RLS policy is wrong.
