import type { CardInfo, Rarity } from './types';

export const slug = (id: string) =>
  id
    .replace(/^[A-Z0-9]+-/, '')
    .replace(/_POWER$/, '')
    .toLowerCase()
    .replace(/[_\s]+/g, '-');

export const tallyLabel = (id: string) => id.replace(/^[A-Z0-9]+-/, '').toLowerCase();

export const steamItem = (item: string) => `https://steamcommunity.com/sharedfiles/filedetails/?id=${item}`;

export const RARITY_LOOK: Record<Rarity, { frame: string; outline: string }> = {
  Ancient: { frame: 'ancient', outline: '#4d4b40' },
  Basic: { frame: 'common', outline: '#4d4b40' },
  Common: { frame: 'common', outline: '#4d4b40' },
  Event: { frame: 'event', outline: '#1b6131' },
  Rare: { frame: 'rare', outline: '#6b4b00' },
  Token: { frame: 'common', outline: '#4d4b40' },
  Uncommon: { frame: 'uncommon', outline: '#005c75' },
};

export const RARITIES = ['Basic', 'Common', 'Uncommon', 'Rare', 'Ancient', 'Event', 'Token'] as const;

export const RARITY_NAME: Record<Rarity, string> = {
  Ancient: 'Ancient',
  Basic: 'Starter',
  Common: 'Common',
  Event: 'Event',
  Rare: 'Rare',
  Token: 'Token',
  Uncommon: 'Uncommon',
};

export const POOL_RARITIES = ['Common', 'Uncommon', 'Rare'] as const;
export type PoolRarity = (typeof POOL_RARITIES)[number];
export const inPool = (rarity: string): rarity is PoolRarity => (POOL_RARITIES as readonly string[]).includes(rarity);

export const PARTNER_MODS = [
  { item: '3749294247', name: 'The Hero Expansion', tag: 'Hero' },
  { item: '3747492675', name: 'Ancients Awakened', tag: 'Ancients Awakened' },
];

export const partnerMod = (tag: string) => PARTNER_MODS.find((mod) => mod.tag === tag);

export const CARD_GROUPS: { id: string; note?: string; title: string }[] = [
  { id: 'Basic', title: 'Starting deck' },
  { id: 'Common', title: 'Common' },
  { id: 'Uncommon', title: 'Uncommon' },
  { id: 'Rare', title: 'Rare' },
  { id: 'Ancient', title: 'Ancient' },
  { id: 'Event', title: 'Event' },
  { id: 'Token', note: 'Made by other cards, relics and potions during a fight.', title: 'Created in combat' },
  ...PARTNER_MODS.map((mod) => ({ id: mod.tag, title: mod.name })),
];

export const cardGroup = (info: CardInfo) => info.tags.find((tag) => partnerMod(tag)) ?? info.rarity;

export const LAST_IN_GROUP = new Set(['ALCHEMIST-COMPOUND_MIX']);

export const TYPES = ['Attack', 'Skill', 'Power'] as const;

export interface CardKeys {
  cost: string;
  rarity: string;
  type: string;
}

const COMPENDIUM_RANKS = [
  ['rarity', (card: CardKeys) => (RARITIES as readonly string[]).indexOf(card.rarity)],
  ['type', (card: CardKeys) => (TYPES as readonly string[]).indexOf(card.type)],
  ['cost', (card: CardKeys) => (card.cost === 'X' ? 0 : Number(card.cost))],
] as const;
export type CompendiumKey = (typeof COMPENDIUM_RANKS)[number][0];

export function compendiumOrder(first: CompendiumKey = 'rarity') {
  const ranks = [...COMPENDIUM_RANKS]
    .sort(([a], [b]) => Number(b === first) - Number(a === first))
    .map(([, rank]) => rank);
  return (a: CardKeys, b: CardKeys) => {
    for (const rank of ranks) if (rank(a) !== rank(b)) return rank(a) - rank(b);
    return 0;
  };
}

export const RELIC_ORDER = ['Starter', 'Common', 'Uncommon', 'Rare', 'Shop', 'Ancient', 'Event'];
export const POTION_ORDER = ['Common', 'Uncommon', 'Rare', 'Event'];

export const potionRarity = (rarity: null | string) => (rarity === 'Event' ? 'Brew' : rarity);

export const MIXES = [
  { color: '#f03c3c', kind: 'bursting', name: 'Bursting' },
  { color: '#4a90e8', kind: 'syrupy', name: 'Syrupy' },
  { color: '#c07aff', kind: 'zesty', name: 'Zesty' },
  { color: '#ff9424', kind: 'fuming', name: 'Fuming' },
  { color: '#4fd06a', kind: 'acrid', name: 'Acrid' },
  { color: '#ffe14a', kind: 'sparkling', name: 'Sparkling' },
  { color: '#f4f4f4', kind: 'compound', name: 'Compound' },
];

export const mixByKind = (kind: string) => MIXES.find((mix) => mix.kind === kind);

export function mixKind(id: string, info: CardInfo) {
  const kind = id.match(/^[A-Z0-9]+-(\w+)_MIX$/)?.[1].toLowerCase();
  return info.rarity === 'Token' && kind && mixByKind(kind) ? kind : null;
}

export const ASCENSION_BANDS: Record<number, string> = { 0: 'A0', 1: 'A1 to A4', 5: 'A5 to A9', 10: 'A10 and up' };

export const ENCOUNTER_KINDS: Record<string, string> = {
  BOSS: 'Boss',
  ELITE: 'Elite',
  NORMAL: 'Hallway',
  WEAK: 'Hallway',
};
