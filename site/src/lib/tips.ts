import { tokens } from './markup';

export interface Tip {
  id: string;
  text: string;
  title: string;
}

interface Options {
  fallback?: Tip[];
  keywords?: string[];
  lang: string;
  ownName?: string;
}

const TRAILING_PUNCTUATION = /[\s.,:;!?。、，：；！？»“”"]+$/u;
const LEADING_QUOTES = /^[\s«„“"]+/u;
const TRAILING_PLUS = /\+$/;
const INFLECTING_SCRIPT = /[\p{Script=Latin}\p{Script=Cyrillic}]/u;
const commonPrefixLength = (a: string, b: string) => {
  let i = 0;
  while (i < a.length && a[i] === b[i]) i++;
  return i;
};
const requiredPrefixLength = (length: number) =>
  length <= 2 ? length : length <= 4 ? Math.max(3, length - 1) : length <= 7 ? length - 2 : Math.max(5, length - 5);
const isInflectionOf = (word: string, title: string): boolean => {
  const [words, titleWords] = [word.split(' '), title.split(' ')];
  if (words.length !== titleWords.length) return false;
  if (titleWords.length > 1) return titleWords.every((titleWord, i) => isInflectionOf(words[i], titleWord));
  return (
    (commonPrefixLength(word, title) >= requiredPrefixLength(title.length) &&
      word.length - title.length <= (title.length <= 2 ? 2 : 5) &&
      title.length - word.length <= (title.length > 7 ? 5 : 2)) ||
    (title.length >= 5 && word.includes(title) && word.length - title.length <= 4)
  );
};

const goldTerms = (markups: string[]) =>
  markups.flatMap((markup) =>
    tokens(markup).flatMap((token) =>
      token.kind === 'text' && token.color === 'gold'
        ? [token.text.replace(TRAILING_PUNCTUATION, '').replace(LEADING_QUOTES, '')]
        : [],
    ),
  );

export function tipsFor(
  markups: string[],
  tips: Tip[],
  { fallback = [], keywords = [], lang, ownName }: Options,
): Tip[] {
  const normalize = (text: string) => text.trim().toLocaleLowerCase(lang);
  const terms = goldTerms(markups).filter(Boolean);
  const textTerms = new Set(terms.map(normalize));
  const ownKeywords = new Set(keywords.map((keyword) => keyword.toUpperCase()));
  const byTitle = Map.groupBy(tips, (tip) => normalize(tip.title));
  const fallbackByTitle = Map.groupBy(fallback, (tip) => normalize(tip.title));

  const score = (tip: Tip) => {
    const id = tip.id.replace(/^[A-Z]+-/, '');
    if (ownKeywords.has(id)) return 1000;
    const overlap = goldTerms([tip.text]).filter((term) => textTerms.has(normalize(term))).length;
    return (id.endsWith('_REF') ? 100 : 0) + overlap;
  };
  const bestTip = (candidates: Tip[] | undefined) =>
    candidates?.reduce((a, b) => (score(b) > score(a) ? b : a), candidates[0]);

  const exactMatch = (titles: Map<string, Tip[]>, word: string) =>
    bestTip(titles.get(word)) ?? bestTip(titles.get(word.replace(TRAILING_PLUS, '')));

  const lookup = (term: string) => {
    const word = normalize(term);
    const exact = exactMatch(byTitle, word) ?? exactMatch(fallbackByTitle, word);
    if (exact || !INFLECTING_SCRIPT.test(word)) return exact;
    const bare = word.replace(TRAILING_PLUS, '');
    const closeness = (title: string) => [commonPrefixLength(bare, title), -Math.abs(title.length - bare.length)];
    const candidates = [...byTitle.entries()]
      .filter(([title]) => isInflectionOf(bare, title))
      .flatMap(([title, list]) => list.map((tip) => ({ order: [score(tip), ...closeness(title)], tip })))
      .sort((a, b) => b.order[0] - a.order[0] || b.order[1] - a.order[1] || b.order[2] - a.order[2]);
    return candidates[0]?.tip;
  };

  const chosen = new Map<string, Tip>();
  for (const term of terms) {
    const tip = lookup(term);
    if (tip && !chosen.has(normalize(tip.title)) && normalize(tip.title) !== normalize(ownName ?? '')) {
      chosen.set(normalize(tip.title), tip);
    }
  }
  return [...chosen.values()];
}
