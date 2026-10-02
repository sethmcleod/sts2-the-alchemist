import { describe, expect, it } from 'vitest';
import { compendiumOrder, type CardKeys } from './mod';

const card = (name: string, rarity: string, type: string, cost: string) => ({ name, rarity, type, cost });
const CARDS = [
  card('Mud Pack', 'Rare', 'Skill', 'X'),
  card('Double Dip', 'Common', 'Attack', '2'),
  card('Bounce Back', 'Uncommon', 'Skill', '1'),
  card('Snack Attack', 'Common', 'Attack', '1'),
  card('Reconstitute', 'Common', 'Skill', '0'),
  card('Twitch', 'Rare', 'Skill', '1'),
  card('Strike', 'Basic', 'Attack', '1'),
];
const sorted = (first?: Parameters<typeof compendiumOrder>[0]) => {
  const order = compendiumOrder(first);
  return [...CARDS].sort((a: CardKeys, b: CardKeys) => order(a, b)).map((c) => (c as (typeof CARDS)[number]).name);
};

describe('the compendium order', () => {
  it('goes by rarity, then type, then cost, with an X cost as 0', () => {
    expect(sorted()).toEqual([
      'Strike',
      'Snack Attack',
      'Double Dip',
      'Reconstitute',
      'Bounce Back',
      'Mud Pack',
      'Twitch',
    ]);
  });

  it('puts the sort the reader picks first and keeps the rest in order', () => {
    expect(sorted('cost')).toEqual([
      'Reconstitute',
      'Mud Pack',
      'Strike',
      'Snack Attack',
      'Bounce Back',
      'Twitch',
      'Double Dip',
    ]);
    expect(sorted('type').slice(0, 3)).toEqual(['Strike', 'Snack Attack', 'Double Dip']);
  });
});
