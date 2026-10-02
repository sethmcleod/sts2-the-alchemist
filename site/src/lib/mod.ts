// What the site knows about The Alchemist that the data does not say

import type { CardInfo, Rarity } from './types';

/** The URL name of a model: ALCHEMIST-ROLLING_BOIL -> rolling-boil, ALCHEMIST-FRESH_BATCH_POWER -> fresh-batch */
export const slug = (id: string) =>
  id
    .replace(/^[A-Z0-9]+-/, '')
    .replace(/_POWER$/, '')
    .toLowerCase()
    .replace(/[_\s]+/g, '-');

/** The label run tallies use for a model: ALCHEMIST-FRESH_BATCH_POWER -> fresh_batch_power */
export const tallyLabel = (id: string) => id.replace(/^[A-Z0-9]+-/, '').toLowerCase();

export const steamItem = (item: string) => `https://steamcommunity.com/sharedfiles/filedetails/?id=${item}`;

// How the game draws each rarity: the frame's banner tint and the title outline (StsColors)
export const RARITY_LOOK: Record<Rarity, { frame: string; outline: string }> = {
  Basic: { frame: 'common', outline: '#4d4b40' },
  Common: { frame: 'common', outline: '#4d4b40' },
  Token: { frame: 'common', outline: '#4d4b40' },
  Uncommon: { frame: 'uncommon', outline: '#005c75' },
  Rare: { frame: 'rare', outline: '#6b4b00' },
  Event: { frame: 'event', outline: '#1b6131' },
  Ancient: { frame: 'ancient', outline: '#4d4b40' },
};

export const RARITY_NAME: Record<Rarity, string> = {
  Basic: 'Starter',
  Common: 'Common',
  Uncommon: 'Uncommon',
  Rare: 'Rare',
  Ancient: 'Ancient',
  Event: 'Event',
  Token: 'Token',
};

/** The rarities a card reward offers, where cards are ranked against their peers */
export const POOL_RARITIES = ['Common', 'Uncommon', 'Rare'] as const;
export type PoolRarity = (typeof POOL_RARITIES)[number];
export const inPool = (rarity: string): rarity is PoolRarity => (POOL_RARITIES as readonly string[]).includes(rarity);

// The cards.csv tag of each partner mod
export const PARTNER_MODS: Record<string, { name: string; item: string }> = {
  Hero: { name: 'The Hero Expansion', item: '3749294247' },
  'Ancients Awakened': { name: 'Ancients Awakened', item: '3747492675' },
};

// The library's groups, in order. A card a partner mod unlocks goes in that mod's group
export const CARD_GROUPS: { id: string; title: string; note?: string }[] = [
  { id: 'Basic', title: 'Starting deck' },
  { id: 'Common', title: 'Common' },
  { id: 'Uncommon', title: 'Uncommon' },
  { id: 'Rare', title: 'Rare' },
  { id: 'Ancient', title: 'Ancient' },
  { id: 'Event', title: 'Event' },
  { id: 'Token', title: 'Created in combat', note: 'Made by other cards, relics and potions during a fight.' },
  ...Object.entries(PARTNER_MODS).map(([id, mod]) => ({ id, title: mod.name })),
];

export const cardGroup = (info: CardInfo) => info.tags.find((tag) => tag in PARTNER_MODS) ?? info.rarity;

export const LAST_IN_GROUP = new Set(['ALCHEMIST-COMPOUND_MIX']);

export const TYPES = ['Attack', 'Skill', 'Power'] as const;

export interface CardKeys {
  rarity: string;
  type: string;
  cost: string;
}

const RANKS = {
  rarity: (card: CardKeys) => (Object.keys(RARITY_NAME) as string[]).indexOf(card.rarity),
  type: (card: CardKeys) => (TYPES as readonly string[]).indexOf(card.type),
  cost: (card: CardKeys) => (card.cost === 'X' ? 0 : Number(card.cost)),
};
export type CompendiumKey = keyof typeof RANKS;

export function compendiumOrder(first: CompendiumKey = 'rarity') {
  const keys = [first, ...(Object.keys(RANKS) as CompendiumKey[]).filter((key) => key !== first)];
  return (a: CardKeys, b: CardKeys) => {
    for (const key of keys) if (RANKS[key](a) !== RANKS[key](b)) return RANKS[key](a) - RANKS[key](b);
    return 0;
  };
}

export const RELIC_ORDER = ['Starter', 'Common', 'Uncommon', 'Rare', 'Shop', 'Ancient', 'Event'];
export const POTION_ORDER = ['Common', 'Uncommon', 'Rare', 'Event'];
// The mod's Event potions are the ones only Brew makes
export const potionRarity = (rarity: string | null) => (rarity === 'Event' ? 'Brew' : rarity);

export const MIX_KINDS: Record<string, { name: string; color: string }> = {
  bursting: { name: 'Bursting', color: '#f03c3c' },
  syrupy: { name: 'Syrupy', color: '#4a90e8' },
  zesty: { name: 'Zesty', color: '#c07aff' },
  fuming: { name: 'Fuming', color: '#ff9424' },
  acrid: { name: 'Acrid', color: '#4fd06a' },
  sparkling: { name: 'Sparkling', color: '#ffe14a' },
  compound: { name: 'Compound', color: '#f4f4f4' },
};

/** A Mix card's kind, the label its tally counters use: ALCHEMIST-BURSTING_MIX -> "bursting" */
export function mixKind(id: string, info: CardInfo) {
  const kind = id.match(/^[A-Z0-9]+-(\w+)_MIX$/)?.[1].toLowerCase();
  return info.rarity === 'Token' && kind && kind in MIX_KINDS ? kind : null;
}

// The export's ascension bands, by their lowest ascension
export const ASCENSION_BANDS: Record<number, string> = { 0: 'A0', 1: 'A1 to A4', 5: 'A5 to A9', 10: 'A10 and up' };

export const ENCOUNTER_KINDS: Record<string, string> = {
  BOSS: 'Boss',
  ELITE: 'Elite',
  WEAK: 'Hallway',
  NORMAL: 'Hallway',
};
