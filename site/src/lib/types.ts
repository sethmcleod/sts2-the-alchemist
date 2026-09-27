// The shapes of the files tools/analytics/export_stats.py writes to data/

export type Rarity = 'Basic' | 'Common' | 'Uncommon' | 'Rare' | 'Ancient' | 'Event' | 'Token';
export type CardType = 'Attack' | 'Skill' | 'Power';

export interface CardInfo {
  name: string;
  rarity: Rarity;
  type: CardType;
  themes: string[];
  /** Extra markers from cards.csv: Multiplayer, or the partner mod a card needs */
  tags: string[];
  /** The text as the game shows it, then as its upgrade shows it, in the loc markup */
  texts: [string, string];
  costs: [string, string];
  keywords: string[];
  /** A path in the mod repo */
  art: string | null;
}

export interface ItemInfo {
  name: string;
  rarity: string | null;
  text: string;
  flavor: string | null;
  icon: string | null;
}

/** The base game's own words in one language (tools/analytics/game_loc.json) */
export interface GameWords {
  keywords: Record<string, string>;
  types: Record<string, string>;
  rarities: Record<string, string>;
  potion_rarities: Record<string, string>;
  relic_rarities: Record<string, string>;
  words: Record<string, string>;
  encounters: Record<string, string>;
  period: string;
}

type ItemText = Pick<ItemInfo, 'name' | 'text' | 'flavor'>;

/** The mod's words in one language (data/loc/<game>.json), cards as the game shows them */
export interface Translation {
  names: Record<string, string>;
  cards: Record<string, Pick<CardInfo, 'name' | 'texts'>>;
  relics: Record<string, ItemText>;
  potions: Record<string, ItemText>;
  powers: Record<string, ItemText>;
  badges: Record<string, Partial<Record<BadgeTier['tier'], Pick<BadgeTier, 'title' | 'text'>>>>;
  game: GameWords;
}

export interface BadgeTier {
  tier: 'bronze' | 'silver' | 'gold';
  at: number;
  title: string;
  text: string;
}

export interface Badge {
  id: string;
  icon: string | null;
  needs_win: boolean;
  coop_only: boolean;
  metric: string | null;
  tiers: BadgeTier[];
}

export interface WorkshopItem {
  branch: 'beta' | 'main';
  item: string;
  title: string;
  game_branch: string;
}

export interface Meta {
  generated_at: string;
  total_runs: number;
  players: number;
  first_day: string;
  last_day: string;
  versions: string[];
  builds: Record<string, string>;
  prefix: string;
  themes: string[];
  theme_min_cards: number;
  ascension_bands: number[];
  epochs: number;
  histograms: Record<string, { width: number; last: number }>;
  badges: Badge[];
  schema_since: Record<string, string | null>;
  assets: { character: string; energy: string };
  mod: { name: string; description: string; version: string };
  repo: string | null;
  releases: Record<string, string>;
  workshop: WorkshopItem[];
  previews: { image: string; full: string }[];
}

/** A count table: its key columns, its count columns, then one row per group and key */
export interface TableFile {
  key: string[];
  counts: string[];
  rows: (string | number)[][];
}

/** The same table stored by column, each text key as an index into its list of names. The browser
    gets this form (compact.ts) */
export interface ColumnFile {
  key: string[];
  counts: string[];
  names: Record<string, string[]>;
  columns: number[][];
}

export type AnyTableFile = TableFile | ColumnFile;

export interface Summary {
  meta: Meta;
  names: Record<string, string>;
  card_info: Record<string, CardInfo>;
  relic_info: Record<string, ItemInfo>;
  potion_info: Record<string, ItemInfo>;
  power_info: Record<string, ItemInfo>;
  icons: Record<string, string>;
  groups: AnyTableFile;
  [table: string]: unknown;
}

export interface NoteItem {
  text: string;
  items: NoteItem[];
}

export interface Release {
  version: string;
  date: string | null;
  /** Prose under the version heading, before any list */
  intro: string[];
  /** A list before the first heading has no title */
  sections: { title: string | null; items: NoteItem[] }[];
}
