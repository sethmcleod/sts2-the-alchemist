// The languages the site is written in: the mod's own, named the way the game's language menu
// names them (NLanguageDropdown). English lives at the root, every other language under its code.

export interface Locale {
  /** The URL prefix, and the BCP 47 tag the page declares */
  code: string;
  lang: string;
  /** The mod's localization folder */
  game: string;
  name: string;
  /** The label on the language button */
  short: string;
}

export const LOCALES: Locale[] = [
  { code: 'en', lang: 'en', game: 'eng', name: 'English', short: 'EN' },
  { code: 'de', lang: 'de', game: 'deu', name: 'Deutsch', short: 'DE' },
  { code: 'es', lang: 'es-ES', game: 'spa', name: 'Español (Castellano)', short: 'ES' },
  { code: 'es-419', lang: 'es-419', game: 'esp', name: 'Español (Latinoamérica)', short: 'ES-LA' },
  { code: 'fr', lang: 'fr', game: 'fra', name: 'Français', short: 'FR' },
  { code: 'id', lang: 'id', game: 'ind', name: 'Bahasa Indonesia', short: 'ID' },
  { code: 'it', lang: 'it', game: 'ita', name: 'Italiano', short: 'IT' },
  { code: 'ja', lang: 'ja', game: 'jpn', name: '日本語', short: 'JA' },
  { code: 'ko', lang: 'ko', game: 'kor', name: '한국어', short: 'KO' },
  { code: 'pl', lang: 'pl', game: 'pol', name: 'Polski', short: 'PL' },
  { code: 'pt-br', lang: 'pt-BR', game: 'ptb', name: 'Português Brasileiro', short: 'PT-BR' },
  { code: 'ru', lang: 'ru', game: 'rus', name: 'Русский', short: 'RU' },
  { code: 'th', lang: 'th', game: 'tha', name: 'ไทย', short: 'TH' },
  { code: 'tr', lang: 'tr', game: 'tur', name: 'Türkçe', short: 'TR' },
  { code: 'zh-hans', lang: 'zh-Hans', game: 'zhs', name: '中文', short: '简体' },
  { code: 'zh-hant', lang: 'zh-Hant', game: 'zht', name: '繁體中文', short: '繁體' },
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
  LOCALES.map((locale) => ({ locale, lang: locale === DEFAULT_LOCALE ? undefined : locale.code }));

/** getStaticPaths for a page that exists once per language */
export const everyLanguage = () => langParams().map(({ lang }) => ({ params: { lang } }));
