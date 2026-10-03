// The game shrinks a card's title and text until they fit their boxes (NCard). This does the same
// at build time from the card font's glyph widths, so a card never needs a script to lay itself out.
// Sizes are in card units: a card face is 330 units wide. Each language draws cards in the font the
// game picks for it (card-fonts.json, from tools/analytics/card_fonts.py).

import fonts from './card-fonts.json';
import { tokens } from './markup';

const TITLE = { max: 26, min: 12, spacing: 1, width: 210 };
const TEXT = { height: 136, max: 21, min: 12, width: 243 };
// The energy icon is drawn at its own size, whatever the font size
const ENERGY_ICON = 24;
// The description label's line_separation
const LINE_GAP = -3;

export interface CardFont {
  bold: { file: string; scale: number };
  langs: string[];
  /** Ascent plus descent, in em */
  line: number;
  name: string;
  regular: { file: string; scale: number };
  /** Extra space the font adds to each line, in card units */
  spacing: number;
  /** Advances in em, before the scale */
  widths: Record<string, number>;
}

const SETS: CardFont[] = Object.entries(fonts).map(([name, set]) => ({ name, ...set }));
const LATIN = SETS.find((set) => set.name === 'latin')!;

/** The font a language draws its cards in, by the mod's localization folder (eng, jpn, ...) */
export const cardFont = (game = 'eng') => SETS.find((set) => set.langs.includes(game)) ?? LATIN;

// Chinese, Japanese and Korean break between any two characters, Thai between words, which it writes
// without spaces; the rest break at spaces
const WIDE = /[ᄀ-ᇿ⺀-鿿가-힯豈-﫿＀-￯]/;
// No line starts with a closing mark or ends with an opening one
const NO_START = /^[、。，．：；？！）」』】〕〉》〗〙〛’”…ー々ぁぃぅぇぉっゃゅょゎァィゥェォッャュョヮヵヶ%),.:;!?]/;
const NO_END = /[（「『【〔〈《〖〘〚‘“(]$/;
const thai = new Intl.Segmenter('th', { granularity: 'word' });

const em = (font: CardFont, text: string) =>
  [...text].reduce((width, ch) => width + (font.widths[ch] ?? (WIDE.test(ch) ? 1 : 0.5)), 0);

export const lineHeight = (size: number, font = LATIN) => font.line * size + font.spacing + LINE_GAP;

export function titleSize(name: string, font = LATIN) {
  const letters = [...name].length;
  for (let size = TITLE.max; size > TITLE.min; size--) {
    if (em(font, name) * font.bold.scale * size + TITLE.spacing * letters <= TITLE.width) return size;
  }
  return TITLE.min;
}

/** Wide text as the pieces a line may break between: each character, with closing marks kept on
 * the one before them, opening marks on the one after, and runs of Latin letters and digits whole */
function wideParts(chunk: string) {
  const out: string[] = [];
  for (const ch of chunk) {
    const last = out.at(-1);
    if (last && (NO_START.test(ch) || NO_END.test(last) || !WIDE.test(ch + last.at(-1)!))) out[out.length - 1] += ch;
    else out.push(ch);
  }
  return out;
}

/** The pieces of some text a line may break between, each marked when a space comes before it */
function pieces(text: string) {
  return text.split(' ').flatMap((chunk, i) => {
    const parts = /[\u0e00-\u0e7f]/.test(chunk)
      ? [...thai.segment(chunk)].map((s) => s.segment)
      : WIDE.test(chunk)
        ? wideParts(chunk)
        : [chunk];
    return parts.filter(Boolean).map((part, j) => ({ space: i > 0 && j === 0, text: part }));
  });
}

// A word's width is part font (em) and part icon (units), so it can be measured at any size. A word
// after a space puts the space's width before it on its line
type Word = { em: number; last: string; space: boolean; units: number };

function paragraphs(markup: string, font: CardFont) {
  const out: Word[][] = [[]];
  let word: null | Word = null;
  let spaced = false;
  const end = () => {
    if (word) out.at(-1)!.push(word);
    word = null;
  };
  const open = () => {
    word ??= { em: 0, last: '', space: spaced, units: 0 };
    spaced = false;
    return word;
  };
  for (const t of tokens(markup)) {
    if (t.kind === 'break') {
      end();
      out.push([]);
      spaced = false;
    } else if (t.kind === 'energy') {
      open().units += ENERGY_ICON * t.count;
    } else {
      pieces(t.text).forEach((part, i) => {
        const first = part.text[0]!;
        // Each piece is its own word, and so is the first one when wide text breaks before it
        const wideBreak = word && !NO_START.test(first) && !NO_END.test(word.last) && WIDE.test(first + word.last);
        if (i > 0 || part.space || wideBreak) end();
        if (part.space || (i === 0 && t.text.startsWith(' '))) spaced = true;
        const w = open();
        w.em += em(font, part.text);
        w.last = part.text.at(-1)!;
      });
      if (t.text.endsWith(' ')) {
        end();
        spaced = true;
      }
    }
  }
  end();
  return out;
}

function lineCount(words: Word[], size: number, font: CardFont) {
  const space = em(font, ' ') * font.regular.scale * size;
  let lines = 1;
  let used = 0;
  for (const word of words) {
    const width = word.em * font.regular.scale * size + word.units;
    const gap = used > 0 && word.space ? space : 0;
    if (used > 0 && used + gap + width > TEXT.width) {
      lines++;
      used = width;
    } else {
      used += gap + width;
    }
  }
  return lines;
}

export function textSize(markup: string, font = LATIN) {
  const text = paragraphs(markup, font);
  for (let size = TEXT.max; size > TEXT.min; size--) {
    const lines = text.reduce((n, words) => n + lineCount(words, size, font), 0);
    // The gap sits between lines, so there is one fewer than there are lines
    if (lines * lineHeight(size, font) - LINE_GAP <= TEXT.height) return size;
  }
  return TEXT.min;
}
