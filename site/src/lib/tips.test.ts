import { describe, expect, it } from 'vitest';
import { type Tip, tipsFor } from './tips';

const tip = (id: string, title: string, text = '') => ({ id, text, title });
const TIPS: Tip[] = [
  tip('POISON_POWER', 'Poison'),
  tip('EXHAUST', 'Exhaust'),
  tip('TRANSFORM', 'Transform', 'Becomes a random card.'),
  tip('ALCHEMIST-FERMENT', 'Ferment', 'This card grows.'),
  tip('ALCHEMIST-FERMENT_REF', 'Ferment', 'These cards grow.'),
  tip('ALCHEMIST-MIX', 'Mix'),
  tip('ALCHEMIST-BURSTING_MIX_PLUS', 'Bursting Mix+'),
  tip('ALCHEMIST-TRANSFORM_MIX', 'Transform', 'Becomes a random [gold]Mix[/gold].'),
];
const titles = (markup: string, options: { keywords?: string[]; lang?: string; ownName?: string } = {}) =>
  tipsFor([markup], TIPS, { lang: 'en', ...options }).map((t) => t.id);

describe('tips', () => {
  it('lists each gold term once, in the order the text uses them', () => {
    expect(titles('[gold]Exhaust[/gold]. Gain [gold]Poison[/gold], then more [gold]Poison[/gold].')).toEqual([
      'EXHAUST',
      'POISON_POWER',
    ]);
  });

  it('reads an ending as the title: Mixes, Exhausted, Mix+', () => {
    expect(titles('Add 2 [gold]Mixes[/gold].')).toEqual(['ALCHEMIST-MIX']);
    expect(titles('It is [gold]Exhausted[/gold].')).toEqual(['EXHAUST']);
    expect(titles('Add a [gold]Mix+[/gold].')).toEqual(['ALCHEMIST-MIX']);
  });

  it('prefers the exact title, "+" and all', () => {
    expect(titles('Add a [gold]Bursting Mix+[/gold].')).toEqual(['ALCHEMIST-BURSTING_MIX_PLUS']);
  });

  it("gives a card its own keyword's wording, and other text the wording about other cards", () => {
    expect(titles('[gold]Ferment[/gold].', { keywords: ['Ferment'] })).toEqual(['ALCHEMIST-FERMENT']);
    expect(titles('Your [gold]Ferment[/gold] cards grow faster.')).toEqual(['ALCHEMIST-FERMENT_REF']);
  });

  it('picks the tip that shares terms with the text', () => {
    expect(titles('[gold]Transform[/gold] a card into a [gold]Mix[/gold].')[0]).toBe('ALCHEMIST-TRANSFORM_MIX');
    expect(titles('[gold]Transform[/gold] a card.')).toEqual(['TRANSFORM']);
  });

  it('skips terms with no tip and the item itself', () => {
    expect(titles('Put it in your [gold]Hand[/gold].')).toEqual([]);
    expect(titles('Add a [gold]Mix[/gold].', { ownName: 'Mix' })).toEqual([]);
  });

  it('reads inflected words in languages that inflect them', () => {
    const ru = [tip('POISON_POWER', 'Яд'), tip('BLOCK', 'Защита'), tip('STRENGTH_POWER', 'Сила')];
    const found = (text: string, list: Tip[], lang: string) => tipsFor([text], list, { lang }).map((t) => t.id);
    expect(found('[gold]яда[/gold] и [gold]защиты[/gold], [gold]силы[/gold]', ru, 'ru')).toEqual([
      'POISON_POWER',
      'BLOCK',
      'STRENGTH_POWER',
    ]);
    expect(found('[gold]Trucizny[/gold]', [tip('POISON_POWER', 'Trucizna')], 'pl')).toEqual(['POISON_POWER']);
    expect(found('[gold]«Взрывную смесь»[/gold]', [tip('BURSTING_MIX', 'Взрывная смесь')], 'ru')).toEqual([
      'BURSTING_MIX',
    ]);
    expect(found('[gold]Berfermentasi[/gold]', [tip('FERMENT', 'Fermentasi')], 'id')).toEqual(['FERMENT']);
  });

  it('reads the stem of a long title, and picks the tip that fits the text across titles', () => {
    const fr = [tip('ALCHEMIST-FERMENT', 'Fermentation'), tip('TRANSFORM', 'Transformer', 'Au hasard.')];
    const mix = tip('ALCHEMIST-TRANSFORM_MIX', 'Transformation', 'Une [gold]Mixture[/gold] au hasard.');
    const found = (text: string, list: Tip[]) => tipsFor([text], list, { lang: 'fr' }).map((t) => t.id);
    expect(found('Elle [gold]Fermente[/gold].', fr)).toEqual(['ALCHEMIST-FERMENT']);
    expect(found('[gold]Transformez[/gold] une carte en [gold]Mixture[/gold].', [...fr, mix])).toEqual([
      'ALCHEMIST-TRANSFORM_MIX',
    ]);
    expect(found('[gold]Transformez[/gold] une carte.', [...fr, mix])).toEqual(['TRANSFORM']);
  });

  it('uses the English tips only for English words', () => {
    const local = [tip('ALCHEMIST-FERMENT', 'Fermentation')];
    const english = [tip('ALCHEMIST-FERMENT', 'Ferment'), tip('POISON_POWER', 'Poison')];
    const found = (text: string) => tipsFor([text], local, { fallback: english, lang: 'fr' }).map((t) => t.title);
    expect(found('[gold]Fermente[/gold] et [gold]Poison[/gold]')).toEqual(['Fermentation', 'Poison']);
  });

  it('never reads a name of several words as a one-word title', () => {
    expect(titles('Your [gold]Exhaust Pile[/gold].')).toEqual([]);
  });

  it('keeps unrelated words and unspaced scripts apart', () => {
    expect(titles('[gold]Poisonousness[/gold]')).toEqual([]);
    expect(titles('[gold]Mind[/gold]')).toEqual([]);
    expect(tipsFor(['[gold]毒素[/gold]'], [tip('POISON_POWER', '毒')], { lang: 'ja' })).toEqual([]);
  });
});
