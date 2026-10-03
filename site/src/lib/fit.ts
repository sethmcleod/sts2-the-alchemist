import fonts from './card-fonts.json';
import { tokens } from './markup';

const TITLE = { max: 26, min: 12, spacing: 1, width: 210 };
const TEXT = { height: 136, max: 21, min: 12, width: 243 };
const ENERGY_ICON_WIDTH = 24;
const LINE_GAP = -3;

export interface CardFont {
  bold: { file: string; scale: number };
  langs: string[];
  line: number;
  name: string;
  regular: { file: string; scale: number };
  spacing: number;
  widths: Record<string, number>;
}

const CARD_FONTS: CardFont[] = Object.entries(fonts).map(([name, font]) => ({ name, ...font }));
const LATIN = CARD_FONTS.find((font) => font.name === 'latin')!;

export const cardFont = (game = 'eng') => CARD_FONTS.find((font) => font.langs.includes(game)) ?? LATIN;

const WIDE = /[ᄀ-ᇿ⺀-鿿가-힯豈-﫿＀-￯]/;
const THAI = /[\u0e00-\u0e7f]/;
const NO_LINE_START =
  /^[、。，．：；？！）」』】〕〉》〗〙〛’”…ー々ぁぃぅぇぉっゃゅょゎァィゥェォッャュョヮヵヶ%),.:;!?]/;
const NO_LINE_END = /[（「『【〔〈《〖〘〚‘“(]$/;
const thaiWords = new Intl.Segmenter('th', { granularity: 'word' });

const emWidth = (font: CardFont, text: string) =>
  [...text].reduce((width, char) => width + (font.widths[char] ?? (WIDE.test(char) ? 1 : 0.5)), 0);

export const lineHeight = (size: number, font = LATIN) => font.line * size + font.spacing + LINE_GAP;

export function titleSize(name: string, font = LATIN) {
  const letters = [...name].length;
  for (let size = TITLE.max; size > TITLE.min; size--) {
    if (emWidth(font, name) * font.bold.scale * size + TITLE.spacing * letters <= TITLE.width) return size;
  }
  return TITLE.min;
}

function wideParts(chunk: string) {
  const parts: string[] = [];
  for (const char of chunk) {
    const previous = parts.at(-1);
    const joins =
      previous && (NO_LINE_START.test(char) || NO_LINE_END.test(previous) || !WIDE.test(char + previous.at(-1)!));
    if (joins) parts[parts.length - 1] += char;
    else parts.push(char);
  }
  return parts;
}

function breakPieces(text: string) {
  return text.split(' ').flatMap((chunk, chunkIndex) => {
    const parts = THAI.test(chunk)
      ? [...thaiWords.segment(chunk)].map((word) => word.segment)
      : WIDE.test(chunk)
        ? wideParts(chunk)
        : [chunk];
    return parts
      .filter(Boolean)
      .map((part, partIndex) => ({ afterSpace: chunkIndex > 0 && partIndex === 0, text: part }));
  });
}

type Word = { afterSpace: boolean; em: number; lastChar: string; units: number };

function paragraphs(markup: string, font: CardFont) {
  const result: Word[][] = [[]];
  let word: null | Word = null;
  let afterSpace = false;
  const endWord = () => {
    if (word) result.at(-1)!.push(word);
    word = null;
  };
  const openWord = () => {
    word ??= { afterSpace, em: 0, lastChar: '', units: 0 };
    afterSpace = false;
    return word;
  };
  for (const token of tokens(markup)) {
    if (token.kind === 'break') {
      endWord();
      result.push([]);
      afterSpace = false;
    } else if (token.kind === 'energy') {
      openWord().units += ENERGY_ICON_WIDTH * token.count;
    } else {
      breakPieces(token.text).forEach((piece, i) => {
        const first = piece.text[0]!;
        const canBreakBefore =
          word && !NO_LINE_START.test(first) && !NO_LINE_END.test(word.lastChar) && WIDE.test(first + word.lastChar);
        if (i > 0 || piece.afterSpace || canBreakBefore) endWord();
        if (piece.afterSpace || (i === 0 && token.text.startsWith(' '))) afterSpace = true;
        const current = openWord();
        current.em += emWidth(font, piece.text);
        current.lastChar = piece.text.at(-1)!;
      });
      if (token.text.endsWith(' ')) {
        endWord();
        afterSpace = true;
      }
    }
  }
  endWord();
  return result;
}

function lineCount(words: Word[], size: number, font: CardFont) {
  const spaceWidth = emWidth(font, ' ') * font.regular.scale * size;
  let lines = 1;
  let lineWidth = 0;
  for (const word of words) {
    const width = word.em * font.regular.scale * size + word.units;
    const gap = lineWidth > 0 && word.afterSpace ? spaceWidth : 0;
    if (lineWidth > 0 && lineWidth + gap + width > TEXT.width) {
      lines++;
      lineWidth = width;
    } else {
      lineWidth += gap + width;
    }
  }
  return lines;
}

export function textSize(markup: string, font = LATIN) {
  const wordsByParagraph = paragraphs(markup, font);
  for (let size = TEXT.max; size > TEXT.min; size--) {
    const lines = wordsByParagraph.reduce((total, words) => total + lineCount(words, size, font), 0);
    if (lines * lineHeight(size, font) - LINE_GAP <= TEXT.height) return size;
  }
  return TEXT.min;
}
