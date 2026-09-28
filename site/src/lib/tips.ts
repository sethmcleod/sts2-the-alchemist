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
// How much of a title a word must share: all of a very short one, most of a longer one
const needed = (title: string) =>
  title.length <= 2 ? title.length : title.length <= 4 ? Math.max(3, title.length - 1) : title.length - 2;
const stemOf = (word: string, title: string): boolean => {
  // A name of several words inflects each of them: Взрывную смесь for Взрывная смесь
  const [words, titles] = [word.split(' '), title.split(' ')];
  if (titles.length > 1) return words.length === titles.length && titles.every((t, i) => stemOf(words[i], t));
  return (
    (shared(word, title) >= needed(title) &&
      word.length - title.length <= (title.length <= 2 ? 2 : 5) &&
      title.length - word.length <= 2) ||
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

export function tipsFor(markups: string[], tips: Tip[], { lang, keywords = [], exclude }: Options): Tip[] {
  const norm = (text: string) => text.trim().toLocaleLowerCase(lang);
  const terms = goldTerms(markups).filter(Boolean);
  const mine = new Set(terms.map(norm));
  const own = new Set(keywords.map((k) => k.toUpperCase()));
  const byTitle = Map.groupBy(tips, (tip) => norm(tip.title));

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

  const lookup = (term: string) => {
    const word = norm(term);
    const exact = best(byTitle.get(word)) ?? best(byTitle.get(word.replace(PLUS, '')));
    if (exact || !INFLECTS.test(word)) return exact;
    const bare = word.replace(PLUS, '');
    // The title that shares the most with the word, then the one closest to its length
    const stem = [...byTitle.keys()]
      .filter((title) => stemOf(bare, title))
      .sort(
        (a, b) =>
          shared(bare, b) - shared(bare, a) || Math.abs(a.length - bare.length) - Math.abs(b.length - bare.length),
      )[0];
    return stem === undefined ? undefined : best(byTitle.get(stem));
  };

  const found = new Map<string, Tip>();
  for (const term of terms) {
    const tip = lookup(term);
    if (tip && !found.has(norm(tip.title)) && norm(tip.title) !== norm(exclude ?? '')) found.set(norm(tip.title), tip);
  }
  return [...found.values()];
}
