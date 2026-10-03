import fs from 'node:fs';
import path from 'node:path';
import type { ImageMetadata } from 'astro';
import { getImage } from 'astro:assets';
import { type Change, changes } from './history';
import { DEFAULT_LOCALE, type Locale } from './i18n';
import { Lang, type LangInit, type Strings } from './lang';
import { type Extra, EXTRA_FILES, Runs, withTranslatedNames } from './runs';
import type { Release, Summary, TableFile, Translation } from './types';

const DATA = path.join(process.cwd(), 'data');
const read = <T>(file: string): T => JSON.parse(fs.readFileSync(path.join(DATA, file), 'utf8'));

export const dataFile = (file: string) => read<Record<string, unknown>>(file);

let loaded:
  undefined | { locales: Map<string, { lang: Lang; runs: Runs; words: Translation }>; runs: Runs; stamp: number };

function load() {
  const stamp = fs.statSync(path.join(DATA, 'summary.json')).mtimeMs;
  if (loaded?.stamp !== stamp) {
    const extras = Object.entries(EXTRA_FILES).map(([extra, [file, key]]) => [
      extra as Extra,
      read<Record<string, TableFile>>(file)[key],
    ]);
    loaded = { locales: new Map(), runs: new Runs(read<Summary>('summary.json'), Object.fromEntries(extras)), stamp };
    icons = undefined;
  }
  return loaded;
}

export const translation = (locale: Locale) => read<Translation>(`loc/${locale.game}.json`);

export type StringFile = { readme?: Record<string, string>; site?: Strings; stats?: Strings };
const stringFiles = import.meta.glob<StringFile>(['../i18n/*.json', '!../i18n/en.json'], {
  eager: true,
  import: 'default',
});
export const strings = (locale: Locale): StringFile => stringFiles[`../i18n/${locale.code}.json`] ?? {};

function localized(locale: Locale) {
  const cache = load();
  let found = cache.locales.get(locale.code);
  if (!found) {
    const words = translation(locale);
    const { site, stats } = strings(locale);
    const summary = cache.runs.summary;
    const withLocalText = <T extends object>(infos: Record<string, T>, local: Record<string, Partial<T>>) =>
      Object.fromEntries(Object.entries(infos).map(([id, info]) => [id, { ...info, ...local[id] }]));
    const renamed = withTranslatedNames(summary, words);
    const runs =
      locale === DEFAULT_LOCALE
        ? cache.runs
        : cache.runs.withSummary({
            ...renamed,
            card_info: withLocalText(renamed.card_info, words.cards),
            potion_info: withLocalText(renamed.potion_info, words.potions),
            power_info: withLocalText(renamed.power_info, words.powers),
            relic_info: withLocalText(renamed.relic_info, words.relics),
          });
    found = { lang: new Lang(locale, { ...site, ...stats }, words.game), runs, words };
    cache.locales.set(locale.code, found);
  }
  return found;
}

export const runs = (locale: Locale = DEFAULT_LOCALE) => localized(locale).runs;

export const language = (locale: Locale) => localized(locale).lang;

export const tips = (locale: Locale) => localized(locale).words.tips ?? [];

export function islandLang(locale: Locale, { encounters = false } = {}): LangInit {
  const words = translation(locale);
  return {
    code: locale.code,
    game: encounters ? words.game : { ...words.game, encounters: {} },
    lang: locale.lang,
    names: locale === DEFAULT_LOCALE ? undefined : { badges: words.badges, names: words.names },
    strings: strings(locale).stats ?? {},
  };
}

export const releases = () => read<{ versions: Release[] }>('notes.json').versions;

let history: undefined | { changes: Map<string, Change[]>; from: Runs };

export function itemChanges(id: string) {
  const current = load().runs;
  if (history?.from !== current) {
    const { card_info, potion_info, relic_info } = current.summary;
    const names = Object.fromEntries(
      [card_info, relic_info, potion_info].flatMap((infos) =>
        Object.entries(infos).map(([itemId, info]) => [itemId, info.name]),
      ),
    );
    history = { changes: changes(names, releases()), from: current };
  }
  return history.changes.get(id) ?? [];
}

export const translationCount = () =>
  fs
    .readdirSync(path.join(process.cwd(), '..', 'Alchemist', 'localization'), { withFileTypes: true })
    .filter((entry) => entry.isDirectory() && entry.name !== 'eng').length;

const images = import.meta.glob<{ default: ImageMetadata }>([
  '../../../Alchemist/images/{card_portraits,relics,potions,powers,badges}/**/*.png',
  '../../../workshop/previews/*.png',
]);

export async function image(repoPath: null | string | undefined) {
  const importImage = repoPath ? images[`../../../${repoPath}`] : undefined;
  return importImage ? (await importImage()).default : null;
}

let icons: Promise<Record<string, string>> | undefined;

export const iconUrls = () => (icons ??= makeIconUrls());

async function makeIconUrls() {
  const { meta, summary } = runs();
  const iconPaths = [...Object.entries(summary.icons), ...meta.badges.map((badge) => [badge.id, badge.icon] as const)];
  const entries = await Promise.all(
    iconPaths.map(async ([id, iconPath]) => {
      const src = await image(iconPath);
      return src ? [id, (await getImage({ format: 'webp', src, width: 64 })).src] : null;
    }),
  );
  return Object.fromEntries(entries.filter((entry) => entry !== null));
}
