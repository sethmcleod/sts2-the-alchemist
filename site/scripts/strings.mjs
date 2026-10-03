// What each language still needs translated, measured against src/i18n/en.json (the English the
// last build used, see src/i18n/catalog.mjs).
//
//   npm run strings                     how many strings each language is missing
//   npm run strings -- --todo <dir>     writes <dir>/<code>.json: the missing English, to translate.
//                                       A string the base game translates is filled in already.
//   npm run strings -- --merge <dir>    puts the translated <dir>/<code>.json into src/i18n/<code>.json
//
// A language file keeps only strings the site still uses, each in the part the catalog puts it in.

import fs from 'node:fs';
import path from 'node:path';
import { CATALOG, readmeSource } from '../src/i18n/catalog.mjs';

const SITE = path.resolve(import.meta.dirname, '..');
const I18N = path.join(SITE, 'src', 'i18n');
const GAME = JSON.parse(fs.readFileSync(path.join(SITE, '..', 'tools', 'analytics', 'game_loc.json'), 'utf8'));
const LOCALES = [
  ...fs
    .readFileSync(path.join(SITE, 'src', 'lib', 'i18n.ts'), 'utf8')
    .matchAll(/code: '([\w-]+)', lang: '[\w-]+', game: '(\w+)'/g),
]
  .map(([, code, game]) => ({ code, game }))
  .filter((locale) => locale.code !== 'en');

const read = (file) => (fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : {});
const write = (file, data) => fs.writeFileSync(file, `${JSON.stringify(data, null, 2)}\n`);
const catalog = read(CATALOG);
if (!catalog.site) throw new Error('src/i18n/en.json is missing: build the site first');

/** The base game's word for an English one, where the game has both (card types, rarities, ...) */
function donors(game) {
  const found = new Map();
  for (const group of ['types', 'rarities', 'potion_rarities', 'relic_rarities', 'keywords', 'words']) {
    for (const [key, english] of Object.entries(GAME.eng[group])) found.set(english, GAME[game][group][key]);
  }
  return found;
}

/** A language's strings in the catalog's shape: every translation it has that the site still uses */
function tidy(current, extra = {}) {
  const lookup = { ...current.site, ...current.stats, ...extra.site, ...extra.stats };
  const part = (keys) => Object.fromEntries(keys.filter((key) => key in lookup).map((key) => [key, lookup[key]]));
  const readme = { ...current.readme, ...extra.readme };
  return {
    readme: Object.fromEntries(
      Object.keys(catalog.readme)
        .filter((hash) => hash in readme)
        .map((hash) => [hash, readme[hash]]),
    ),
    site: part(catalog.site),
    stats: part(catalog.stats),
  };
}

const [flag, dir] = process.argv.slice(2);
for (const locale of LOCALES) {
  const file = path.join(I18N, `${locale.code}.json`);
  const current = tidy(read(file));
  if (flag === '--merge') {
    const done = read(path.join(dir, `${locale.code}.json`));
    const empty = (value) => value === '' || value == null;
    const clean = (part) => Object.fromEntries(Object.entries(part ?? {}).filter(([, value]) => !empty(value)));
    write(file, tidy(current, { readme: clean(done.readme), site: clean(done.site), stats: clean(done.stats) }));
  }
  const now = flag === '--merge' ? tidy(read(file)) : current;
  const missing = {
    readme: Object.entries(catalog.readme).filter(([hash]) => !(hash in now.readme)),
    site: catalog.site.filter((key) => !(key in now.site)),
    stats: catalog.stats.filter((key) => !(key in now.stats)),
  };
  const count = missing.site.length + missing.stats.length + missing.readme.length;
  console.log(`${locale.code.padEnd(8)} ${count ? `${count} to translate` : 'complete'}`);
  if (flag === '--todo' && count) {
    fs.mkdirSync(dir, { recursive: true });
    const given = donors(locale.game);
    const todo = (keys) => Object.fromEntries(keys.map((key) => [key, given.get(key) ?? '']));
    write(path.join(dir, `${locale.code}.json`), {
      readme: Object.fromEntries(missing.readme.map(([hash, title]) => [hash, readmeSource(title)])),
      site: todo(missing.site),
      stats: todo(missing.stats),
    });
  }
}
