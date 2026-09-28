// The definitions the game shows beside a card for the terms its text marks in gold (Poison,
// Exhaust, Ferment, a Mix). The export lists them per language (data/loc/<language>.json, tips);
// this finds the ones a text uses, in the order it uses them.

import { tokens } from './markup';

export interface Tip {
  /** The loc entry: EXHAUST, POISON_POWER, ALCHEMIST-FERMENT_REF */
  id: string;
  title: string;
  text: string;
}

interface Options {
  /** The page's language, for comparing words */
  lang: string;
  /** The item's own keywords (Exhaust, Ferment), whose tip wins over another with the same title */
  keywords?: string[];
  /** The item's own name, which gets no tip */
  exclude?: string;
  /** Tips in English, for text the mod has not translated yet. Only an exact title counts, since the
   *  page's own words would otherwise pass for inflected English ones */
  fallback?: Tip[];
}

// What a gold term can end in besides the word itself: punctuation in any script, and a Mix's "+".
// Some languages put a Mix's name in quotes
const TRAIL = /[\s.,:;!?。、，：；！？»“”"]+$/u;
const LEAD = /^[\s«„“"]+/u;
const PLUS = /\+$/;
// Languages that inflect their words in Latin or Cyrillic letters: a word counts as a title with
// another ending (Mixes, Fermented, Mixturen, защиты for Защита, яда for Яд, Trucizny for Trucizna).
// Chinese, Japanese, Korean and Thai text uses the title as it is
const INFLECTS = /[\p{Script=Latin}\p{Script=Cyrillic}]/u;
const shared = (a: string, b: string) => {
  let i = 0;
  while (i < a.length && a[i] === b[i]) i++;
  return i;
};
// How much of a title a word must share: all of a very short one, most of a longer one, and the stem
// of a long one, whose ending can change more (Fermente for Fermentation)
const needed = (length: number) =>
  length <= 2 ? length : length <= 4 ? Math.max(3, length - 1) : length <= 7 ? length - 2 : Math.max(5, length - 5);
const stemOf = (word: string, title: string): boolean => {
  // A name of several words inflects each of them (Взрывную смесь for Взрывная смесь), and a word
  // never stands for a name of another length (Exhaust Pile is not Exhaust)
  const [words, titles] = [word.split(' '), title.split(' ')];
  if (words.length !== titles.length) return false;
  if (titles.length > 1) return titles.every((t, i) => stemOf(words[i], t));
  return (
    (shared(word, title) >= needed(title.length) &&
      word.length - title.length <= (title.length <= 2 ? 2 : 5) &&
      title.length - word.length <= (title.length > 7 ? 5 : 2)) ||
    // Or takes a prefix: Berfermentasi for Fermentasi
    (title.length >= 5 && word.includes(title) && word.length - title.length <= 4)
  );
};

const goldTerms = (markups: string[]) =>
  markups.flatMap((markup) =>
    tokens(markup).flatMap((t) =>
      t.kind === 'text' && t.color === 'gold' ? [t.text.replace(TRAIL, '').replace(LEAD, '')] : [],
    ),
  );

export function tipsFor(
  markups: string[],
  tips: Tip[],
  { lang, keywords = [], exclude, fallback = [] }: Options,
): Tip[] {
  const norm = (text: string) => text.trim().toLocaleLowerCase(lang);
  const terms = goldTerms(markups).filter(Boolean);
  const mine = new Set(terms.map(norm));
  const own = new Set(keywords.map((k) => k.toUpperCase()));
  const byTitle = Map.groupBy(tips, (tip) => norm(tip.title));
  const fallbackByTitle = Map.groupBy(fallback, (tip) => norm(tip.title));

  // Several tips can share a title: the item's own keyword first, then the wording about other
  // cards (_REF), then the tip whose text shares the most gold terms with the item (the mod's
  // "Transform into a Mix" beside the game's Transform)
  const score = (tip: Tip) => {
    const id = tip.id.replace(/^[A-Z]+-/, '');
    if (own.has(id)) return 1000;
    const overlap = goldTerms([tip.text]).filter((term) => mine.has(norm(term))).length;
    return (id.endsWith('_REF') ? 100 : 0) + overlap;
  };
  const best = (found: Tip[] | undefined) => found?.reduce((a, b) => (score(b) > score(a) ? b : a), found[0]);

  const exactly = (titles: Map<string, Tip[]>, word: string) =>
    best(titles.get(word)) ?? best(titles.get(word.replace(PLUS, '')));

  const lookup = (term: string) => {
    const word = norm(term);
    const exact = exactly(byTitle, word) ?? exactly(fallbackByTitle, word);
    if (exact || !INFLECTS.test(word)) return exact;
    // Among the titles the word inflects, the best tip, then the title that shares the most with the
    // word, then the one closest to its length
    const bare = word.replace(PLUS, '');
    const rank = (title: string) => [shared(bare, title), -Math.abs(title.length - bare.length)];
    const found = [...byTitle.entries()]
      .filter(([title]) => stemOf(bare, title))
      .flatMap(([title, list]) => list.map((tip) => ({ tip, order: [score(tip), ...rank(title)] })))
      .sort((a, b) => b.order[0] - a.order[0] || b.order[1] - a.order[1] || b.order[2] - a.order[2]);
    return found[0]?.tip;
  };

  const found = new Map<string, Tip>();
  for (const term of terms) {
    const tip = lookup(term);
    if (tip && !found.has(norm(tip.title)) && norm(tip.title) !== norm(exclude ?? '')) found.set(norm(tip.title), tip);
  }
  return [...found.values()];
}
