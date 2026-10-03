// The languages the site is written in: the mod's own, named the way the game's language menu
// names them (NLanguageDropdown). English lives at the root, every other language under its code.

export interface Locale {
  /** The URL prefix, and the BCP 47 tag the page declares */
  code: string;
  /** The mod's localization folder */
  game: string;
  lang: string;
  name: string;
  /** The label on the language button */
  short: string;
}

export const LOCALES: Locale[] = [
  { code: 'en', game: 'eng', lang: 'en', name: 'English', short: 'EN' },
  { code: 'de', game: 'deu', lang: 'de', name: 'Deutsch', short: 'DE' },
  { code: 'es', game: 'spa', lang: 'es-ES', name: 'Español (Castellano)', short: 'ES' },
  { code: 'es-419', game: 'esp', lang: 'es-419', name: 'Español (Latinoamérica)', short: 'ES-LA' },
  { code: 'fr', game: 'fra', lang: 'fr', name: 'Français', short: 'FR' },
  { code: 'id', game: 'ind', lang: 'id', name: 'Bahasa Indonesia', short: 'ID' },
  { code: 'it', game: 'ita', lang: 'it', name: 'Italiano', short: 'IT' },
  { code: 'ja', game: 'jpn', lang: 'ja', name: '日本語', short: 'JA' },
  { code: 'ko', game: 'kor', lang: 'ko', name: '한국어', short: 'KO' },
  { code: 'pl', game: 'pol', lang: 'pl', name: 'Polski', short: 'PL' },
  { code: 'pt-br', game: 'ptb', lang: 'pt-BR', name: 'Português Brasileiro', short: 'PT-BR' },
  { code: 'ru', game: 'rus', lang: 'ru', name: 'Русский', short: 'RU' },
  { code: 'th', game: 'tha', lang: 'th', name: 'ไทย', short: 'TH' },
  { code: 'tr', game: 'tur', lang: 'tr', name: 'Türkçe', short: 'TR' },
  { code: 'zh-hans', game: 'zhs', lang: 'zh-Hans', name: '中文', short: '简体' },
  { code: 'zh-hant', game: 'zht', lang: 'zh-Hant', name: '繁體中文', short: '繁體' },
];
export const DEFAULT_LOCALE = LOCALES[0];

const BY_CODE = new Map(LOCALES.filter((locale) => locale !== DEFAULT_LOCALE).map((locale) => [locale.code, locale]));

/** The language a path is in: its first segment names it (/de/relics, or /de.html at build), or it is English */
export const localeOf = (pathname: string) =>
  BY_CODE.get(pathname.split('/')[1]?.replace(/\.html$/, '') ?? '') ?? DEFAULT_LOCALE;

/** The path without its language: /de/relics is /relics */
export function basePath(pathname: string) {
  const locale = localeOf(pathname);
  return locale === DEFAULT_LOCALE ? pathname : pathname.slice(locale.code.length + 1) || '/';
}

/** The [...lang] route parameter of every language: none for English, which lives at the root */
export const langParams = () =>
  LOCALES.map((locale) => ({ lang: locale === DEFAULT_LOCALE ? undefined : locale.code, locale }));

/** getStaticPaths for a page that exists once per language */
export const everyLanguage = () => langParams().map(({ lang }) => ({ params: { lang } }));
