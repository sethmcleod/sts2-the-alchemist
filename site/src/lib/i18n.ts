export interface Locale {
  code: string;
  game: string;
  lang: string;
  name: string;
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

export const localeOf = (pathname: string) =>
  BY_CODE.get(pathname.split('/')[1]?.replace(/\.html$/, '') ?? '') ?? DEFAULT_LOCALE;

export function basePath(pathname: string) {
  const locale = localeOf(pathname);
  return locale === DEFAULT_LOCALE ? pathname : pathname.slice(locale.code.length + 1) || '/';
}

export const langParams = () =>
  LOCALES.map((locale) => ({ lang: locale === DEFAULT_LOCALE ? undefined : locale.code, locale }));

export const everyLanguage = () => langParams().map(({ lang }) => ({ params: { lang } }));
