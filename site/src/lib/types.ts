// The shapes of the files tools/analytics/export_stats.py writes to data/

import type { Tip } from './tips';

export type Rarity = 'Ancient' | 'Basic' | 'Common' | 'Event' | 'Rare' | 'Token' | 'Uncommon';
export type CardType = 'Attack' | 'Power' | 'Skill';

export interface CardInfo {
  /** A path in the mod repo */
  art: null | string;
  costs: [string, string];
  keywords: string[];
  name: string;
  rarity: Rarity;
  /** Extra markers from cards.csv: Multiplayer, or the partner mod a card needs */
  tags: string[];
  /** The text as the game shows it, then as its upgrade shows it, in the loc markup */
  texts: [string, string];
  themes: string[];
  type: CardType;
}

export interface ItemInfo {
  flavor: null | string;
  icon: null | string;
  name: string;
  rarity: null | string;
  text: string;
}

/** The base game's own words in one language (tools/analytics/game_loc.json) */
export interface GameWords {
  encounters: Record<string, string>;
  keywords: Record<string, string>;
  period: string;
  potion_rarities: Record<string, string>;
  rarities: Record<string, string>;
  relic_rarities: Record<string, string>;
  types: Record<string, string>;
  words: Record<string, string>;
}

type ItemText = Pick<ItemInfo, 'flavor' | 'name' | 'text'>;

/** The mod's words in one language (data/loc/<game>.json), cards as the game shows them */
export interface Translation {
  badges: Record<string, Partial<Record<BadgeTier['tier'], Pick<BadgeTier, 'text' | 'title'>>>>;
  cards: Record<string, Pick<CardInfo, 'name' | 'texts'>>;
  game: GameWords;
  names: Record<string, string>;
  potions: Record<string, ItemText>;
  powers: Record<string, ItemText>;
  relics: Record<string, ItemText>;
  /** The hover tips for the gold terms in the text (lib/tips.ts) */
  tips?: Tip[];
}

export interface BadgeTier {
  at: number;
  text: string;
  tier: 'bronze' | 'gold' | 'silver';
  title: string;
}

export interface Badge {
  coop_only: boolean;
  icon: null | string;
  id: string;
  metric: null | string;
  needs_win: boolean;
  tiers: BadgeTier[];
}

export interface WorkshopItem {
  branch: 'beta' | 'main';
  game_branch: string;
  item: string;
  title: string;
}

export interface Meta {
  ascension_bands: number[];
  assets: { character: string; energy: string };
  badges: Badge[];
  builds: Record<string, string>;
  epochs: number;
  first_day: string;
  generated_at: string;
  histograms: Record<string, { last: number; width: number }>;
  last_day: string;
  mod: { description: string; name: string; version: string };
  players: number;
  prefix: string;
  previews: { full: string; image: string }[];
  releases: Record<string, string>;
  repo: null | string;
  schema_since: Record<string, null | string>;
  theme_min_cards: number;
  themes: string[];
  total_runs: number;
  versions: string[];
  workshop: WorkshopItem[];
}

/** A count table: its key columns, its count columns, then one row per group and key */
export interface TableFile {
  counts: string[];
  key: string[];
  rows: (number | string)[][];
}

/** The same table stored by column, each text key as an index into its list of names. The browser
    gets this form (compact.ts) */
export interface ColumnFile {
  columns: number[][];
  counts: string[];
  key: string[];
  names: Record<string, string[]>;
}

export type AnyTableFile = ColumnFile | TableFile;

export interface Summary {
  [table: string]: unknown;
  card_info: Record<string, CardInfo>;
  groups: AnyTableFile;
  icons: Record<string, string>;
  meta: Meta;
  names: Record<string, string>;
  potion_info: Record<string, ItemInfo>;
  power_info: Record<string, ItemInfo>;
  relic_info: Record<string, ItemInfo>;
}

export interface NoteItem {
  items: NoteItem[];
  text: string;
}

export interface Release {
  date: null | string;
  /** Prose under the version heading, before any list */
  intro: string[];
  /** A list before the first heading has no title */
  sections: { items: NoteItem[]; title: null | string }[];
  version: string;
}
