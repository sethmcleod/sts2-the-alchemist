// Words and numbers in the reader's language. The site writes every string through t(), keyed by
// its English text, so a string whose English changes shows in English until it is translated
// again. The translations live in src/i18n/<code>.json; `npm run strings` lists what is missing.

import type { Locale } from './i18n';
import type { GameWords, Translation } from './types';

type Forms = Partial<Record<Intl.LDMLPluralRule, string>>;
export type Strings = Record<string, string | Forms>;
type Vars = Record<string, string | number>;

/** What an island needs to build its Lang: the page's language and the strings the stats use */
export interface LangInit {
  code: string;
  lang: string;
  strings: Strings;
  game?: GameWords;
  /** The mod's names in this language, for data loaded in English */
  names?: Pick<Translation, 'names' | 'badges'>;
}

// While the site builds, each lookup notes its key, and whether a page or an island (which gets only
// the stats strings) asked, for the catalog src/i18n/catalog.mjs writes. A browser skips this
function seen(key: string, scope: Scope) {
  if (!import.meta.env.SSR) return;
  const all: Map<string, Set<Scope>> = ((globalThis as { __siteStrings?: Map<string, Set<Scope>> }).__siteStrings ??=
    new Map());
  if (!all.has(key)) all.set(key, new Set());
  all.get(key)!.add(scope);
}

type Scope = 'site' | 'stats';

const ORDINAL = '{n}th';
const ORDINAL_EN: Forms = { one: '{n}st', two: '{n}nd', few: '{n}rd', other: '{n}th' };
const NONE = '–';

type Maybe = number | null | undefined;
const missing = (v: Maybe): v is null | undefined => v == null || Number.isNaN(v);

export class Lang {
  readonly code: string;
  readonly lang: string;
  private readonly plurals: Intl.PluralRules;
  private readonly ordinals: Intl.PluralRules;
  private readonly whole: Intl.NumberFormat;

  constructor(
    locale: Pick<Locale, 'code' | 'lang'>,
    readonly strings: Strings = {},
    /** The base game's words for this language: card types, rarities, keywords, encounters */
    readonly game?: GameWords,
    /** Who looks strings up: a page, or an island */
    private readonly scope: Scope = 'site',
  ) {
    this.code = locale.code;
    this.lang = locale.lang;
    this.plurals = new Intl.PluralRules(this.lang);
    this.ordinals = new Intl.PluralRules(this.lang, { type: 'ordinal' });
    this.whole = new Intl.NumberFormat(this.lang, { maximumFractionDigits: 0 });
  }

  /** A site path in this language: /relics in German is /de/relics */
  href(path: string) {
    return this.code === 'en' ? path : `/${this.code}${path.replace(/^\/(?=$|[#?])/, '')}`;
  }

  private fill(text: string, vars?: Vars) {
    return vars ? text.replace(/\{(\w+)\}/g, (field, name) => (name in vars ? String(vars[name]) : field)) : text;
  }

  private form(forms: Forms, rule: Intl.LDMLPluralRule) {
    return forms[rule] ?? forms.other;
  }

  /** The English text in the reader's language, with each {name} filled from vars */
  t(en: string, vars?: Vars) {
    seen(en, this.scope);
    const found = this.strings[en];
    return this.fill(typeof found === 'string' ? found : en, vars);
  }

  /** A count and its noun: one and other are the English forms; {n} is the count */
  n(count: number, one: string, other: string, vars?: Vars) {
    seen(other, this.scope);
    const found = this.strings[other];
    const rule = this.plurals.select(count);
    const text =
      typeof found === 'string'
        ? found
        : found
          ? this.form(found, rule)
          : new Intl.PluralRules('en').select(count) === 'one'
            ? one
            : other;
    return this.fill(text ?? other, { n: this.num(count), ...vars });
  }

  num(v: Maybe) {
    return missing(v) ? NONE : this.whole.format(Math.round(v));
  }

  fixed(v: Maybe, digits = 1) {
    return missing(v)
      ? NONE
      : v.toLocaleString(this.lang, { minimumFractionDigits: digits, maximumFractionDigits: digits });
  }

  /** A share as a percentage: whole numbers from 10%, one decimal below, and under 1% as "<1%" */
  pct(v: Maybe) {
    if (missing(v)) return NONE;
    const percent = (share: number, digits: number) =>
      share.toLocaleString(this.lang, {
        style: 'percent',
        minimumFractionDigits: digits,
        maximumFractionDigits: digits,
      });
    if (v > 0 && v < 0.01) return `<${percent(0.01, 0)}`;
    return percent(v, v >= 0.1 || v === 0 ? 0 : 1);
  }

  range([lo, hi]: [number, number]) {
    return this.t('{lo} to {hi}', { lo: this.pct(lo), hi: this.pct(hi) });
  }

  ordinal(n: number) {
    seen(ORDINAL, this.scope);
    const found = this.strings[ORDINAL];
    // Untranslated, it stays English all through: "1st", not "1th"
    const text =
      typeof found === 'string'
        ? found
        : found
          ? this.form(found, this.ordinals.select(n))
          : this.form(ORDINAL_EN, new Intl.PluralRules('en', { type: 'ordinal' }).select(n));
    return this.fill(text ?? ORDINAL, { n });
  }

  /** A change in a rate, in points out of 100: +2.5 */
  change(delta: Maybe) {
    return missing(delta)
      ? NONE
      : (Math.round(delta * 1000) / 10).toLocaleString(this.lang, {
          signDisplay: 'exceptZero',
          maximumFractionDigits: 1,
        });
  }

  minutes(value: Maybe) {
    if (missing(value)) return NONE;
    const m = Math.round(value);
    return m >= 60 ? this.t('{h} h {m} min', { h: Math.floor(m / 60), m: m % 60 }) : this.t('{m} min', { m });
  }

  date(iso: string, style: 'medium' | 'long' = 'medium') {
    return new Date(`${iso.slice(0, 10)}T00:00:00Z`).toLocaleDateString(this.lang, {
      dateStyle: style,
      timeZone: 'UTC',
    });
  }

  /** ["a", "b", "c"] -> "a, b and c" (the site's English has no serial comma, as in British usage) */
  list(words: string[]) {
    return new Intl.ListFormat(this.lang === 'en' ? 'en-GB' : this.lang, { type: 'conjunction' }).format(words);
  }
}
