import locales from './locales.json';

export interface Locale {
  code: string;
  game: string;
  lang: string;
  name: string;
  short: string;
}

export const LOCALES: Locale[] = locales;
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
