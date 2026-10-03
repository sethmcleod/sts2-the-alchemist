import fs from 'node:fs';
import path from 'node:path';
import { CATALOG_PATH, readmeSource } from '../src/i18n/catalog.mjs';

const SITE = path.resolve(import.meta.dirname, '..');
const I18N = path.join(SITE, 'src', 'i18n');
const GAME_LOC = JSON.parse(fs.readFileSync(path.join(SITE, '..', 'tools', 'analytics', 'game_loc.json'), 'utf8'));
const LOCALES = [
  ...fs.readFileSync(path.join(SITE, 'src', 'lib', 'i18n.ts'), 'utf8').matchAll(/\{[^{}]*\bcode: '([\w-]+)'[^{}]*\}/g),
]
  .map(([locale, code]) => ({ code, game: locale.match(/\bgame: '(\w+)'/)[1] }))
  .filter((locale) => locale.code !== 'en');

const read = (file) => (fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : {});
const write = (file, data) => fs.writeFileSync(file, `${JSON.stringify(data, null, 2)}\n`);
const catalog = read(CATALOG_PATH);
if (!catalog.site) throw new Error('src/i18n/en.json is missing: build the site first');

function gameWordsFor(game) {
  const found = new Map();
  for (const group of ['types', 'rarities', 'potion_rarities', 'relic_rarities', 'keywords', 'words']) {
    for (const [key, english] of Object.entries(GAME_LOC.eng[group])) found.set(english, GAME_LOC[game][group][key]);
  }
  return found;
}

function pruneToCatalog(current, extra = {}) {
  const lookup = { ...current.site, ...current.stats, ...extra.site, ...extra.stats };
  const keepTranslated = (keys) =>
    Object.fromEntries(keys.filter((key) => key in lookup).map((key) => [key, lookup[key]]));
  const readme = { ...current.readme, ...extra.readme };
  return {
    readme: Object.fromEntries(
      Object.keys(catalog.readme)
        .filter((hash) => hash in readme)
        .map((hash) => [hash, readme[hash]]),
    ),
    site: keepTranslated(catalog.site),
    stats: keepTranslated(catalog.stats),
  };
}

const [flag, dir] = process.argv.slice(2);
for (const locale of LOCALES) {
  const file = path.join(I18N, `${locale.code}.json`);
  const current = pruneToCatalog(read(file));
  if (flag === '--merge') {
    const translated = read(path.join(dir, `${locale.code}.json`));
    const isBlank = (value) => value === '' || value == null;
    const dropBlank = (part) => Object.fromEntries(Object.entries(part ?? {}).filter(([, value]) => !isBlank(value)));
    write(
      file,
      pruneToCatalog(current, {
        readme: dropBlank(translated.readme),
        site: dropBlank(translated.site),
        stats: dropBlank(translated.stats),
      }),
    );
  }
  const updated = flag === '--merge' ? pruneToCatalog(read(file)) : current;
  const missing = {
    readme: Object.entries(catalog.readme).filter(([hash]) => !(hash in updated.readme)),
    site: catalog.site.filter((key) => !(key in updated.site)),
    stats: catalog.stats.filter((key) => !(key in updated.stats)),
  };
  const count = missing.site.length + missing.stats.length + missing.readme.length;
  console.log(`${locale.code.padEnd(8)} ${count ? `${count} to translate` : 'complete'}`);
  if (flag === '--todo' && count) {
    fs.mkdirSync(dir, { recursive: true });
    const gameWords = gameWordsFor(locale.game);
    const todo = (keys) => Object.fromEntries(keys.map((key) => [key, gameWords.get(key) ?? '']));
    write(path.join(dir, `${locale.code}.json`), {
      readme: Object.fromEntries(missing.readme.map(([hash, title]) => [hash, readmeSource(title)])),
      site: todo(missing.site),
      stats: todo(missing.stats),
    });
  }
}
