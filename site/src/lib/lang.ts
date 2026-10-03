import type { Locale } from './i18n';
import type { GameWords, Translation } from './types';

type Forms = Partial<Record<Intl.LDMLPluralRule, string>>;
export type Strings = Record<string, Forms | string>;
type Vars = Record<string, number | string>;

export interface LangInit {
  code: string;
  game?: GameWords;
  lang: string;
  names?: Pick<Translation, 'badges' | 'names'>;
  strings: Strings;
}

function recordUse(key: string, scope: Scope) {
  if (!import.meta.env.SSR) return;
  const used: Map<string, Set<Scope>> = ((globalThis as { __siteStrings?: Map<string, Set<Scope>> }).__siteStrings ??=
    new Map());
  if (!used.has(key)) used.set(key, new Set());
  used.get(key)!.add(scope);
}

type Scope = 'site' | 'stats';

const ORDINAL = '{n}th';
const ORDINAL_EN: Forms = { few: '{n}rd', one: '{n}st', other: '{n}th', two: '{n}nd' };
const NONE = '–';

type Maybe = null | number | undefined;
const missing = (value: Maybe): value is null | undefined => value == null || Number.isNaN(value);

export class Lang {
  readonly code: string;
  readonly lang: string;
  private readonly plurals: Intl.PluralRules;
  private readonly ordinals: Intl.PluralRules;
  private readonly whole: Intl.NumberFormat;

  constructor(
    locale: Pick<Locale, 'code' | 'lang'>,
    readonly strings: Strings = {},
    readonly game?: GameWords,
    private readonly scope: Scope = 'site',
  ) {
    this.code = locale.code;
    this.lang = locale.lang;
    this.plurals = new Intl.PluralRules(this.lang);
    this.ordinals = new Intl.PluralRules(this.lang, { type: 'ordinal' });
    this.whole = new Intl.NumberFormat(this.lang, { maximumFractionDigits: 0 });
  }

  href(path: string) {
    return this.code === 'en' ? path : `/${this.code}${path.replace(/^\/(?=$|[#?])/, '')}`;
  }

  private fill(text: string, vars?: Vars) {
    return vars ? text.replace(/\{(\w+)\}/g, (field, name) => (name in vars ? String(vars[name]) : field)) : text;
  }

  private form(forms: Forms, rule: Intl.LDMLPluralRule) {
    return forms[rule] ?? forms.other;
  }

  t(en: string, vars?: Vars) {
    recordUse(en, this.scope);
    const found = this.strings[en];
    return this.fill(typeof found === 'string' ? found : en, vars);
  }

  n(count: number, one: string, other: string, vars?: Vars) {
    recordUse(other, this.scope);
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

  num(value: Maybe) {
    return missing(value) ? NONE : this.whole.format(Math.round(value));
  }

  fixed(value: Maybe, digits = 1) {
    return missing(value)
      ? NONE
      : value.toLocaleString(this.lang, { maximumFractionDigits: digits, minimumFractionDigits: digits });
  }

  pct(value: Maybe) {
    if (missing(value)) return NONE;
    const percent = (share: number, digits: number) =>
      share.toLocaleString(this.lang, {
        maximumFractionDigits: digits,
        minimumFractionDigits: digits,
        style: 'percent',
      });
    if (value > 0 && value < 0.01) return `<${percent(0.01, 0)}`;
    return percent(value, value >= 0.1 || value === 0 ? 0 : 1);
  }

  range([lo, hi]: [number, number]) {
    return this.t('{lo} to {hi}', { hi: this.pct(hi), lo: this.pct(lo) });
  }

  ordinal(n: number) {
    recordUse(ORDINAL, this.scope);
    const found = this.strings[ORDINAL];
    const text =
      typeof found === 'string'
        ? found
        : found
          ? this.form(found, this.ordinals.select(n))
          : this.form(ORDINAL_EN, new Intl.PluralRules('en', { type: 'ordinal' }).select(n));
    return this.fill(text ?? ORDINAL, { n });
  }

  pointChange(delta: Maybe) {
    return missing(delta)
      ? NONE
      : (Math.round(delta * 1000) / 10).toLocaleString(this.lang, {
          maximumFractionDigits: 1,
          signDisplay: 'exceptZero',
        });
  }

  minutes(value: Maybe) {
    if (missing(value)) return NONE;
    const m = Math.round(value);
    return m >= 60 ? this.t('{h} h {m} min', { h: Math.floor(m / 60), m: m % 60 }) : this.t('{m} min', { m });
  }

  date(iso: string, style: 'long' | 'medium' = 'medium') {
    return new Date(`${iso.slice(0, 10)}T00:00:00Z`).toLocaleDateString(this.lang, {
      dateStyle: style,
      timeZone: 'UTC',
    });
  }

  list(words: string[]) {
    return new Intl.ListFormat(this.lang === 'en' ? 'en-GB' : this.lang, { type: 'conjunction' }).format(words);
  }
}
