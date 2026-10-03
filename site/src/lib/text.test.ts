import fs from 'node:fs';
import { describe, expect, it } from 'vitest';
import { readmeHash as catalogHash, readmeSource as catalogSource, README_SECTIONS } from '../i18n/catalog.mjs';
import { cardFont, lineHeight, textSize, titleSize } from './fit';
import { LOCALES } from './i18n';
import { Lang } from './lang';
import { plain, tokens } from './markup';
import { readmeHash, readmeSource } from './readme';

describe('markup', () => {
  it('colours text and drops the effect tags', () => {
    expect(tokens('Gain 3 [gold]Poison[/gold].[sine]x[/sine]')).toEqual([
      { color: undefined, kind: 'text', text: 'Gain 3 ' },
      { color: 'gold', kind: 'text', text: 'Poison' },
      { color: undefined, kind: 'text', text: '.' },
      { color: undefined, kind: 'text', text: 'x' },
    ]);
  });

  it('counts energy icons in a row and keeps line breaks', () => {
    expect(tokens('Gain [energy][energy].\nDraw 1.')).toEqual([
      { color: undefined, kind: 'text', text: 'Gain ' },
      { count: 2, kind: 'energy' },
      { color: undefined, kind: 'text', text: '.' },
      { kind: 'break' },
      { color: undefined, kind: 'text', text: 'Draw 1.' },
    ]);
  });

  it('reads energy icons as words', () => {
    expect(plain('Gain [energy][energy].')).toBe('Gain 2 Energy.');
    expect(plain('Costs 4[energy].')).toBe('Costs 4 Energy.');
    expect(plain('[gold]Retain.[/gold]\nDeal 8 damage.')).toBe('Retain. Deal 8 damage.');
  });

  it("reads energy icons in the language's word", () => {
    expect(plain('Gain [energy][energy].', 'Energie')).toBe('Gain 2 Energie.');
  });
});

describe('README translations', () => {
  it('key a section the way the string catalog does', () => {
    for (const title of README_SECTIONS) {
      expect(readmeHash(readmeSource(title))).toBe(catalogHash(catalogSource(title)));
    }
  });
});

describe('fitting text the way the game does', () => {
  it('keeps a short title and text at full size', () => {
    expect(titleSize('Jab')).toBe(26);
    expect(textSize('Deal 6 damage.')).toBe(21);
  });

  it('shrinks a long title', () => {
    expect(titleSize('A Much Longer Card Name Than Fits')).toBeLessThan(26);
  });

  it('shrinks text until its lines fit the box', () => {
    const long = Array.from({ length: 8 }, () => 'Apply 2 [gold]Poison[/gold] to ALL enemies.').join('\n');
    const size = textSize(long);
    expect(size).toBeLessThan(21);
    expect(size).toBeGreaterThanOrEqual(12);
  });

  it('uses the label line height, less its 3 unit gap', () => {
    expect(lineHeight(21)).toBeCloseTo(23.46, 2);
  });
});

describe('card fonts', () => {
  const exported = LOCALES.filter((locale) => fs.existsSync(`data/loc/${locale.game}.json`));

  it.each(exported.map((locale) => locale.game))('has every character %s cards use', (game) => {
    const { cards } = JSON.parse(fs.readFileSync(`data/loc/${game}.json`, 'utf8'));
    const font = cardFont(game);
    const used = new Set(
      Object.values<{ name: string; texts: string[] }>(cards).flatMap((card) =>
        [card.name, ...card.texts].flatMap((text) =>
          tokens(text).flatMap((t) => (t.kind === 'text' ? [...t.text] : [])),
        ),
      ),
    );
    const missing = [...used].filter((ch) => ch.trim() && !(ch in font.widths));
    expect(missing.join('')).toBe('');
  });

  it('breaks wide text between characters and Latin text at spaces', () => {
    const ja = cardFont('jpn');
    expect(textSize('カードを1枚引く。', ja)).toBe(21);
    expect(lineHeight(20, ja)).toBeCloseTo(1.48 * 20 - 3);
  });
});

describe('formatting', () => {
  const l = new Lang({ code: 'en', lang: 'en' });

  it('rounds percentages the way the page shows them', () => {
    expect(l.pct(0.4567)).toBe('46%');
    expect(l.pct(0.0567)).toBe('5.7%');
    expect(l.pct(0.004)).toBe('<1%');
    expect(l.pct(0)).toBe('0%');
    expect(l.pct(null)).toBe('–');
  });

  it('writes ordinals', () => {
    expect([1, 2, 3, 4, 11, 12, 13, 21, 22, 101].map((n) => l.ordinal(n))).toEqual([
      '1st',
      '2nd',
      '3rd',
      '4th',
      '11th',
      '12th',
      '13th',
      '21st',
      '22nd',
      '101st',
    ]);
  });

  it('joins a list like a sentence', () => {
    expect(l.list(['a'])).toBe('a');
    expect(l.list(['a', 'b', 'c'])).toBe('a, b and c');
  });
});
