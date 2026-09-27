// Build-time access to the exported data and to the mod's images. Pages import from here; islands
// fetch the same files from /data instead.

import fs from 'node:fs';
import path from 'node:path';
import type { ImageMetadata } from 'astro';
import { getImage } from 'astro:assets';
import { DEFAULT_LOCALE, type Locale } from './i18n';
import { Lang, type LangInit, type Strings } from './lang';
import { EXTRA_FILES, named, Runs, type Extra } from './runs';
import type { Release, Summary, TableFile, Translation } from './types';

const DATA = path.join(process.cwd(), 'data');
const read = <T>(file: string): T => JSON.parse(fs.readFileSync(path.join(DATA, file), 'utf8'));

export const dataFile = (file: string) => read<Record<string, unknown>>(file);

let loaded: { runs: Runs; stamp: number; locales: Map<string, { runs: Runs; lang: Lang }> } | undefined;

function load() {
  // A new export under a running dev server shows up on the next page load
  const stamp = fs.statSync(path.join(DATA, 'summary.json')).mtimeMs;
  if (loaded?.stamp !== stamp) {
    const extras = Object.entries(EXTRA_FILES).map(([extra, [file, key]]) => [
      extra as Extra,
      read<Record<string, TableFile>>(file)[key],
    ]);
    loaded = { runs: new Runs(read<Summary>('summary.json'), Object.fromEntries(extras)), stamp, locales: new Map() };
    icons = undefined;
  }
  return loaded;
}

/** The mod's words in one language, from the export */
export const translation = (locale: Locale) => read<Translation>(`loc/${locale.game}.json`);

// The site's own words: src/i18n/<code>.json, keyed by the English text. Islands get the stats part
export type StringFile = { site?: Strings; stats?: Strings; readme?: Record<string, string> };
// en.json is the catalog of the English itself (catalog.mjs), not a translation
const stringFiles = import.meta.glob<StringFile>(['../i18n/*.json', '!../i18n/en.json'], {
  eager: true,
  import: 'default',
});
export const strings = (locale: Locale): StringFile => stringFiles[`../i18n/${locale.code}.json`] ?? {};

function localized(locale: Locale) {
  const all = load();
  let found = all.locales.get(locale.code);
  if (!found) {
    const words = translation(locale);
    const { site, stats } = strings(locale);
    const summary = all.runs.summary;
    const each = <T extends object>(infos: Record<string, T>, local: Record<string, Partial<T>>) =>
      Object.fromEntries(Object.entries(infos).map(([id, info]) => [id, { ...info, ...local[id] }]));
    const renamed = named(summary, words);
    const runs =
      locale === DEFAULT_LOCALE
        ? all.runs
        : all.runs.withSummary({
            ...renamed,
            card_info: each(renamed.card_info, words.cards),
            relic_info: each(renamed.relic_info, words.relics),
            potion_info: each(renamed.potion_info, words.potions),
            power_info: each(renamed.power_info, words.powers),
          });
    found = { runs, lang: new Lang(locale, { ...site, ...stats }, words.game) };
    all.locales.set(locale.code, found);
  }
  return found;
}

/** The runs, with the mod's names and text in the page's language */
export const runs = (locale: Locale = DEFAULT_LOCALE) => localized(locale).runs;

/** Words and numbers in a language */
export const language = (locale: Locale) => localized(locale).lang;

/** The language as an island takes it: only the strings the stats islands use, and the mod's names
 *  to put on the data it loads. Only the fights page needs the game's encounter names */
export function islandLang(locale: Locale, { encounters = false } = {}): LangInit {
  const words = translation(locale);
  return {
    code: locale.code,
    lang: locale.lang,
    strings: strings(locale).stats ?? {},
    game: encounters ? words.game : { ...words.game, encounters: {} },
    names: locale === DEFAULT_LOCALE ? undefined : { names: words.names, badges: words.badges },
  };
}

export const releases = () => read<{ versions: Release[] }>('notes.json').versions;

/** The languages the mod is translated into, besides English */
export const translations = () =>
  fs
    .readdirSync(path.join(process.cwd(), '..', 'Alchemist', 'localization'), { withFileTypes: true })
    .filter((entry) => entry.isDirectory() && entry.name !== 'eng').length;

// The data names images by their path in the mod repo
const images = import.meta.glob<{ default: ImageMetadata }>([
  '../../../Alchemist/images/{card_portraits,relics,potions,powers,badges}/**/*.png',
  '../../../workshop/previews/*.png',
]);

export async function image(repoPath: string | null | undefined) {
  const load = repoPath ? images[`../../../${repoPath}`] : undefined;
  return load ? (await load()).default : null;
}

let icons: Promise<Record<string, string>> | undefined;

/** Small icon URLs by model id, for the charts the stats islands draw in the browser */
export const iconUrls = () => (icons ??= makeIconUrls());

async function makeIconUrls() {
  const { summary, meta } = runs();
  const paths = [...Object.entries(summary.icons), ...meta.badges.map((badge) => [badge.id, badge.icon] as const)];
  const urls = await Promise.all(
    paths.map(async ([id, path]) => {
      const src = await image(path);
      return src ? [id, (await getImage({ src, width: 64, format: 'webp' })).src] : null;
    }),
  );
  return Object.fromEntries(urls.filter((url) => url !== null));
}
