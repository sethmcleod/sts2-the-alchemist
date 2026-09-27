# alchemist.fyi

The Alchemist's website: every card, relic, potion and power, the patch notes, and the run
stats. It is built with [Astro](https://astro.build) into static pages, with
[Preact](https://preactjs.com) for the stats filters and [Tailwind](https://tailwindcss.com)
for styles. Nothing here is part of the mod: Godot and the C# build both skip this folder.

## Commands

Run these in `site/`. Node 22.12 or newer and Python 3.12 or newer are needed.

| Command              | What it does                                                        |
| -------------------- | ------------------------------------------------------------------- |
| `npm install`        | Installs the dependencies                                           |
| `npm run data`       | Exports the mod's content and the run stats to `data/`              |
| `npm run data:seed`  | The same, from made-up runs (no Supabase key needed)                |
| `npm run dev`        | Serves the site on http://localhost:4321 while you edit             |
| `npm run build`      | Exports the data, then builds the site into `dist/`                 |
| `npm run build:site` | Builds the site from the data already in `data/`                    |
| `npm run preview`    | Serves `dist/`, with the offline cache and the security policy on   |
| `npm test`           | Runs the unit tests (the stats math, the card text fit, formatting) |
| `npm run check`      | Type-checks everything                                              |
| `npm run strings`    | Lists what each language still needs translated (see Languages)     |
| `npm run format`     | Formats everything with Prettier                                    |

`npm run data` reads the runs with the Supabase secret key, from `SUPABASE_READ_KEY` or
`tools/analytics/supabase-service-key.local.txt` (see `tools/analytics/README.md`).

## How it fits together

- `../tools/analytics/export_stats.py` writes the data: card, relic, potion and power text from
  the mod's localization and code, the patch notes from `CHANGELOG.md`, and count tables of every
  shared run.
- `src/lib/` reads that data. `runs.ts` applies the filters, `stats.ts` does the math, and
  `views/` turns them into what each stats page draws. The same code runs at build time and in
  the browser.
- Every page is built ahead of time with the default filters, so the site works without a
  script. A stats page loads the data from `/data/` only when a filter changes; those files are
  the export's, stored by column (`compact.ts`) to halve the download.
- A card, relic, potion or power link opens its page in a dialog (`scripts/sheet.ts`). The card
  faces are drawn from the game's frames (`src/assets/frames`, made by
  `../tools/analytics/card_frames.py`) and fonts, and their text is fitted at build time the way
  the game fits it (`fit.ts`).
- `src/scripts/service-worker.js` (served as `/sw.js`, signed by each build) keeps every page
  and file a visitor opens, so the site works offline too.
- The hero shows the README's Playstyle and Disclaimer sections, so the README stays the one
  description of the mod.

## Languages

The site is in every language the mod is: English at `/`, the rest under their code (`/de/relics`,
`/ja/cards/jab`). `src/lib/i18n.ts` lists them with the names the game's language menu uses.

- Cards, relics, potions, powers and badges read as the game shows them: the export renders each
  language's loc text (`data/loc/<language>.json`), and the base game's own words for card types,
  rarities, keywords and fights come from `tools/analytics/game_loc.json`.
- Card faces use the font the game draws each language with, cut down to the characters the cards
  use by `../tools/analytics/card_fonts.py`. `npm test` fails when a card needs a character a font
  lacks; run that script again then.
- The site's own words go through `l.t('English text')` (`src/lib/lang.ts`). Each build writes the
  site's English to `src/i18n/en.json`: every literal `l.t()` string in the source, and the words
  the pages look up from tables. `src/i18n/<code>.json` holds a language's translations, keyed by
  that English, so changed English shows untranslated until it is translated again. README
  sections are translated whole, as Markdown, keyed by a hash of the English; a section ends early
  for the site at a `<!-- The website shows this section up to here -->` line.
- A language link keeps the reader's place (`src/scripts/place.ts`): the same filters, flipped
  cards, and card or heading at the same height. The library lists cards in English order in every
  language so they line up.
- To translate: build, then `npm run strings -- --todo <folder>` writes the missing English per
  language (with the base game's word filled in where it has one), and
  `npm run strings -- --merge <folder>` takes the translations back. Use the words the mod's own
  loc files use for its keywords, and keep every `{placeholder}` as it is. A counted string, such
  as `{n} cards`, becomes an object keyed by the language's plural forms: `one` and `other`, and
  `few` and `many` as well in Russian and Polish. README sections go back as Markdown. Patch notes
  stay in English.

## Design notes on a card

A Markdown file in `src/content/commentary/` shows on the page with the same path, under
"Design notes": `cards/rolling-boil.md` shows on `/cards/rolling-boil`.

## Hosting

Vercel builds the `site` branch: the newest release, with `site/` and `tools/analytics/` taken
from `beta`. `.github/workflows/site.yml` moves that branch on every release tag and every push
to `beta` that changes the site, and asks Vercel for a fresh build once a day for the run stats.
So a release updates the site with its cards, relics, potions, powers, README text and patch
notes, and nothing needs running by hand. `vercel.json` sets the caching and security headers.
