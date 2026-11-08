# alchemist.fyi

The Alchemist's website: every card, relic, potion and power, the patch notes, and the run stats.
It's an [Astro](https://astro.build) site of static pages, with [Preact](https://preactjs.com) for
the stats filters and [Tailwind](https://tailwindcss.com) for styles. The mod build skips this
folder.

## Commands

Run these in `site/`. You need Node 22.12+ and Python 3.12+.

| Command              | What it does                                        |
| -------------------- | --------------------------------------------------- |
| `npm install`        | Installs the dependencies                           |
| `npm run data`       | Exports the mod's content and the run stats         |
| `npm run data:seed`  | The same, with made-up runs (no keys needed)        |
| `npm run dev`        | Serves the site on http://localhost:4321            |
| `npm run build`      | Exports the data, then builds the site into `dist/` |
| `npm run build:site` | Builds the site from the data already in `data/`    |
| `npm run preview`    | Serves `dist/` with the offline cache on            |
| `npm test`           | Runs the unit tests                                 |
| `npm run check`      | Type-checks everything                              |
| `npm run lint`       | Checks sort order and the no-comments rule          |
| `npm run strings`    | Lists what each language still needs translated     |
| `npm run format`     | Formats everything with Prettier                    |

`npm run data` needs the Redis and Blob credentials. Vercel builds have them. For a local export,
see [tools/analytics/README.md](../tools/analytics/README.md).

## Layout

- `api/` is the only server code. The mod posts each run to `api/runs.ts`, which queues it in
  Redis. Once a day, `api/pack.ts` moves the queue into Blob and starts a build.
- `../tools/analytics/export_stats.py` writes `data/`: the mod's text, the patch notes from
  `CHANGELOG.md`, and count tables of every run.
- `src/lib/` reads that data. `runs.ts` filters, `stats.ts` does the math, and `views/` shapes it
  for each page. The same code runs at build time and in the browser.
- Pages are built with the default filters, so they work without JavaScript. A filter change
  loads the full data from `/data/`.
- Card faces use the game's frames (`src/assets/frames`) and fonts, and `fit.ts` fits the text
  the way the game does.
- The home page shows the Playstyle and Disclaimer sections of the main README.
- Design notes for a page go in `src/content/commentary/`, at the same path as the page:
  `cards/rolling-boil.md` shows on `/cards/rolling-boil`.

## Languages

The site is in every language the mod is: English at `/`, the rest under their code
(`/de/relics`). `src/lib/i18n.ts` lists them.

- Card, relic and potion text comes from the mod's loc files. The base game's words (card types,
  rarities, keywords) come from `tools/analytics/game_loc.json`.
- The site's own words go through `l.t('English text')`. Each build writes them to
  `src/i18n/en.json`, and `src/i18n/<code>.json` holds the translations, keyed by the English.
- To translate, build first, then run `npm run strings -- --todo <folder>` to write out what's
  missing, and `npm run strings -- --merge <folder>` to bring the translations back. Keep every
  `{placeholder}` as it is. A counted string like `{n} cards` takes one entry per plural form
  (`one`, `other`, plus `few` and `many` in Russian and Polish). Patch notes stay in English.

## Hosting

Vercel builds the `site` branch. `.github/workflows/site.yml` moves it on every release tag and
on every push to `beta` that touches the site, and the daily pack rebuilds it for fresh stats.
Nothing needs running by hand. `vercel.json` sets the headers and the daily schedule.
