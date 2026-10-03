import type { Tip } from './tips';

export type Rarity = 'Ancient' | 'Basic' | 'Common' | 'Event' | 'Rare' | 'Token' | 'Uncommon';
export type CardType = 'Attack' | 'Power' | 'Skill';

export interface CardInfo {
  art: null | string;
  costs: [base: string, upgraded: string];
  keywords: string[];
  name: string;
  rarity: Rarity;
  tags: string[];
  texts: [base: string, upgraded: string];
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

export interface Translation {
  badges: Record<string, Partial<Record<BadgeTier['tier'], Pick<BadgeTier, 'text' | 'title'>>>>;
  cards: Record<string, Pick<CardInfo, 'name' | 'texts'>>;
  game: GameWords;
  names: Record<string, string>;
  potions: Record<string, ItemText>;
  powers: Record<string, ItemText>;
  relics: Record<string, ItemText>;
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

export interface TableFile {
  counts: string[];
  key: string[];
  rows: (number | string)[][];
}

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
  intro: string[];
  sections: { items: NoteItem[]; title: null | string }[];
  version: string;
}
