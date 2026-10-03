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

export const RARITIES: Rarity[] = ['Basic', 'Common', 'Uncommon', 'Rare', 'Ancient', 'Event', 'Token'];

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

export const PARTNER_MODS: Record<string, { item: string; name: string }> = {
  'Ancients Awakened': { item: '3747492675', name: 'Ancients Awakened' },
  Hero: { item: '3749294247', name: 'The Hero Expansion' },
};

export const PARTNER_ORDER = ['Hero', 'Ancients Awakened'];

export const CARD_GROUPS: { id: string; note?: string; title: string }[] = [
  { id: 'Basic', title: 'Starting deck' },
  { id: 'Common', title: 'Common' },
  { id: 'Uncommon', title: 'Uncommon' },
  { id: 'Rare', title: 'Rare' },
  { id: 'Ancient', title: 'Ancient' },
  { id: 'Event', title: 'Event' },
  { id: 'Token', note: 'Made by other cards, relics and potions during a fight.', title: 'Created in combat' },
  ...PARTNER_ORDER.map((id) => ({ id, title: PARTNER_MODS[id].name })),
];

export const cardGroup = (info: CardInfo) => info.tags.find((tag) => tag in PARTNER_MODS) ?? info.rarity;

export const LAST_IN_GROUP = new Set(['ALCHEMIST-COMPOUND_MIX']);

export const TYPES = ['Attack', 'Skill', 'Power'] as const;

export interface CardKeys {
  cost: string;
  rarity: string;
  type: string;
}

const RANKS = {
  cost: (card: CardKeys) => (card.cost === 'X' ? 0 : Number(card.cost)),
  rarity: (card: CardKeys) => (RARITIES as string[]).indexOf(card.rarity),
  type: (card: CardKeys) => (TYPES as readonly string[]).indexOf(card.type),
};
export type CompendiumKey = keyof typeof RANKS;
const COMPENDIUM_KEYS: CompendiumKey[] = ['rarity', 'type', 'cost'];

export function compendiumOrder(first: CompendiumKey = 'rarity') {
  const keys = [first, ...COMPENDIUM_KEYS.filter((key) => key !== first)];
  return (a: CardKeys, b: CardKeys) => {
    for (const key of keys) if (RANKS[key](a) !== RANKS[key](b)) return RANKS[key](a) - RANKS[key](b);
    return 0;
  };
}

export const RELIC_ORDER = ['Starter', 'Common', 'Uncommon', 'Rare', 'Shop', 'Ancient', 'Event'];
export const POTION_ORDER = ['Common', 'Uncommon', 'Rare', 'Event'];

export const potionRarity = (rarity: null | string) => (rarity === 'Event' ? 'Brew' : rarity);

export const MIX_ORDER = ['bursting', 'syrupy', 'zesty', 'fuming', 'acrid', 'sparkling', 'compound'];

export const MIX_KINDS: Record<string, { color: string; name: string }> = {
  acrid: { color: '#4fd06a', name: 'Acrid' },
  bursting: { color: '#f03c3c', name: 'Bursting' },
  compound: { color: '#f4f4f4', name: 'Compound' },
  fuming: { color: '#ff9424', name: 'Fuming' },
  sparkling: { color: '#ffe14a', name: 'Sparkling' },
  syrupy: { color: '#4a90e8', name: 'Syrupy' },
  zesty: { color: '#c07aff', name: 'Zesty' },
};

export function mixKind(id: string, info: CardInfo) {
  const kind = id.match(/^[A-Z0-9]+-(\w+)_MIX$/)?.[1].toLowerCase();
  return info.rarity === 'Token' && kind && kind in MIX_KINDS ? kind : null;
}

export const ASCENSION_BANDS: Record<number, string> = { 0: 'A0', 1: 'A1 to A4', 5: 'A5 to A9', 10: 'A10 and up' };

export const ENCOUNTER_KINDS: Record<string, string> = {
  BOSS: 'Boss',
  ELITE: 'Elite',
  NORMAL: 'Hallway',
  WEAK: 'Hallway',
};
