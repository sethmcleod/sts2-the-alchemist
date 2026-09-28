import { describe, expect, it } from 'vitest';
import { changes, renames } from './history';
import type { NoteItem, Release } from './types';

const note = (text: string, items: NoteItem[] = []): NoteItem => ({ text, items });
const release = (version: string, ...items: NoteItem[]): Release => ({
  version,
  date: null,
  intro: [],
  sections: [{ title: null, items }],
});

// Newest first, like notes.json
const NOTES = [
  release('v4', note('Buffed Heavy Dose card: damage increased from 26 -> 28'), note('Changed Spike and Dose cards')),
  release('v3', note('Renamed Anoint to Spike and reworked it into a Decant card: "Deal 9 damage."')),
  release('v2', note('Renamed Next Up to Anoint, and Delayed Reaction to Unstable Compound, reusing the name')),
  release(
    'v1',
    note('Buffed Next Up card'),
    note('Removed Unstable Compound card'),
    note('Updated cards:', [note('Dose')]),
    note('Removed Extra Dose relic'),
  ),
];
const NAMES = { SPIKE: 'Spike', DOSE: 'Dose', HEAVY: 'Heavy Dose', UNSTABLE: 'Unstable Compound' };
const history = (id: string) => (changes(NAMES, NOTES).get(id) ?? []).map((c) => `${c.version}: ${c.item.text}`);

describe('history', () => {
  it('reads every rename in a line', () => {
    expect(renames(NOTES).map((r) => `${r.from} -> ${r.to}`)).toEqual([
      'Anoint -> Spike',
      'Next Up -> Anoint',
      'Delayed Reaction -> Unstable Compound',
    ]);
  });

  it('follows a card back through its old names', () => {
    expect(history('SPIKE')).toEqual([
      'v4: Changed Spike and Dose cards',
      'v3: Renamed Anoint to Spike and reworked it into a Decant card: "Deal 9 damage."',
      'v2: Renamed Next Up to Anoint, and Delayed Reaction to Unstable Compound, reusing the name',
      'v1: Buffed Next Up card',
    ]);
  });

  it('lets a longer name hide a shorter one inside it, known or not', () => {
    expect(history('HEAVY')).toEqual(['v4: Buffed Heavy Dose card: damage increased from 26 -> 28']);
    expect(history('DOSE')).toEqual(['v4: Changed Spike and Dose cards', 'v1: Dose']);
  });

  it('keeps a reused name for the item that has it at the time', () => {
    expect(history('UNSTABLE')).toEqual([
      'v2: Renamed Next Up to Anoint, and Delayed Reaction to Unstable Compound, reusing the name',
    ]);
  });
});
