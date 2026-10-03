// The run data with the site's filters applied: which groups of runs count, and the numbers more
// than one page reads from them.

import type { Lang } from './lang';
import { ASCENSION_BANDS, inPool, mixKind, POOL_RARITIES, type PoolRarity } from './mod';
import {
  compareVersions,
  type Counts,
  median,
  rate,
  type Row,
  type Selection,
  sum,
  sumBy,
  table,
  type Table,
} from './stats';
import type { AnyTableFile, Meta, Summary, Translation } from './types';

export interface Group {
  ascension: number;
  build: string;
  coop: number;
  pool: string;
  version: string;
}

export interface Filters {
  ascension: string;
  build: string;
  min: number;
  players: 'all' | 'coop' | 'solo';
  pool: string;
  version: string;
}

export const DEFAULT_FILTERS: Filters = {
  ascension: 'all',
  build: 'all',
  min: 10,
  players: 'solo',
  pool: 'all',
  version: 'recent',
};

// The default version filter is the newest versions that together reach this many solo runs
const RECENT_MIN_RUNS = 500;

const SUMMARY_TABLES = [
  'totals',
  'ascensions',
  'days',
  'themes',
  'badges',
  'histograms',
  'counters',
  'acts',
  'death_floors',
] as const;

/** The mod's names in another language, and its badges' tier titles and texts */
export type Names = Pick<Translation, 'badges' | 'names'>;

/** The summary with the mod's names in another language */
export function named(summary: Summary, { badges, names }: Names): Summary {
  const rename = <T extends { name: string }>(infos: Record<string, T>) =>
    Object.fromEntries(Object.entries(infos).map(([id, info]) => [id, { ...info, name: names[id] ?? info.name }]));
  return {
    ...summary,
    card_info: rename(summary.card_info),
    meta: {
      ...summary.meta,
      badges: summary.meta.badges.map((badge) => ({
        ...badge,
        tiers: badge.tiers.map((tier) => ({ ...tier, ...badges[badge.id]?.[tier.tier] })),
      })),
    },
    names: { ...summary.names, ...names },
    potion_info: rename(summary.potion_info),
    power_info: rename(summary.power_info),
    relic_info: rename(summary.relic_info),
  };
}

export type Extra = 'cards' | 'encounters' | 'potions' | 'relics';

/** The data file, and the key in it, that holds each table beyond the summary */
export const EXTRA_FILES: Record<Extra, [file: string, key: string]> = {
  cards: ['cards.json', 'cards'],
  encounters: ['fights.json', 'encounters'],
  potions: ['relics.json', 'potions'],
  relics: ['relics.json', 'relics'],
};

export class Runs {
  readonly meta: Meta;
  readonly groups: Group[];
  readonly tables: Partial<Record<Extra, Table>> & Record<(typeof SUMMARY_TABLES)[number], Table>;
  readonly recentStart: string;

  constructor(
    readonly summary: Summary,
    extra: Partial<Record<Extra, AnyTableFile>> = {},
  ) {
    this.meta = summary.meta;
    this.groups = table(summary.groups).rows as unknown as Group[];
    this.tables = {
      ...(Object.fromEntries(SUMMARY_TABLES.map((name) => [name, table(summary[name] as AnyTableFile)])) as Record<
        (typeof SUMMARY_TABLES)[number],
        Table
      >),
      ...Object.fromEntries(Object.entries(extra).map(([name, file]) => [name, table(file)])),
    };
    this.recentStart = this.findRecentStart();
  }

  /** The same runs, with the mod's names and text from another summary (another language) */
  withSummary(summary: Summary): Runs {
    return Object.assign(Object.create(Runs.prototype), this, { meta: summary.meta, summary });
  }

  /** The mod's own names come from its localization. A base game id only has its words to go on */
  name(id: string) {
    const known = this.summary.names[id] ?? this.summary.card_info[id]?.name;
    if (known) return known;
    return id
      .replace(/^[A-Z0-9]+-/, '')
      .toLowerCase()
      .replace(/_/g, ' ')
      .replace(/\b\w/g, (c) => c.toUpperCase())
      .replace(/(?!^)\b(Of|The|And|In)\b/g, (word) => word.toLowerCase());
  }

  table(name: Extra) {
    const found = this.tables[name];
    if (!found) throw new Error(`${name} is not loaded`);
    return found;
  }

  select(f: Filters, where: (g: Group) => boolean = () => true): Selection {
    const inVersion = this.versionTest(f.version);
    return this.groups.map((g) =>
      inVersion(g.version) &&
      (f.ascension === 'all' || g.ascension === Number(f.ascension)) &&
      (f.players === 'all' || g.coop === (f.players === 'coop' ? 1 : 0)) &&
      (f.pool === 'all' || g.pool === f.pool) &&
      (f.build === 'all' || g.build === f.build) &&
      where(g)
        ? 1
        : 0,
    );
  }

  get latest() {
    return this.meta.versions.at(-1)!;
  }

  /** The first and last version a filter covers */
  versionSpan(choice: string): [string, string] | null {
    if (choice === 'all') return null;
    if (choice === 'recent') return [this.recentStart, this.latest];
    if (choice.startsWith('>=')) return [choice.slice(2), this.latest];
    return [choice, choice];
  }

  private versionTest(choice: string) {
    const span = this.versionSpan(choice);
    return (v: string) => !span || (compareVersions(v, span[0]) >= 0 && compareVersions(v, span[1]) <= 0);
  }

  private findRecentStart() {
    const solo = this.groups.map((g) => (g.coop ? 0 : 1));
    const byVersion = sumBy(this.tables.totals, solo, (r) => this.groups[r.group].version);
    const newestFirst = [...this.meta.versions].reverse();
    let runs = 0;
    for (const version of newestFirst) {
      runs += byVersion.get(version)?.runs || 0;
      if (runs >= RECENT_MIN_RUNS) return version;
    }
    return newestFirst.at(-1)!;
  }

  totals(on: Selection) {
    return sum(this.tables.totals, on);
  }

  counters(on: Selection) {
    return sumBy(this.tables.counters, on, (r) => r.counter as string);
  }

  /** The schema 3 counters start with one version, so the groups that carry them are a version cut */
  withDetail(on: Selection): Selection {
    const since = this.detailSince;
    return on.map((flag, i) => (flag && since && compareVersions(this.groups[i].version, since) >= 0 ? 1 : 0));
  }

  get detailSince() {
    return this.meta.schema_since?.['3'] ?? null;
  }

  /** When the detailed counters start: "from v0.14.18 on" */
  countedSince(l: Lang) {
    return this.detailSince ? l.t('from {version} on', { version: this.detailSince }) : l.t('from the next release on');
  }

  histogram(on: Selection, metric: string, column = 'runs') {
    const { last, width } = this.meta.histograms[metric];
    const counts = sumBy(this.tables.histograms, on, (r) => (r.metric === metric ? (r.bin as number) : -1));
    const bins = [];
    for (let bin = 0; bin <= last; bin += width) bins.push({ bin, runs: counts.get(bin)?.[column] || 0 });
    return { bins, last, width };
  }

  badgeShares(on: Selection, badgeId: string) {
    const byTier = sumBy(this.tables.badges, on, (r) => (r.badge === badgeId ? (r.tier as number) : -1));
    byTier.delete(-1);
    const eligible = [...byTier.values()].reduce((n, t) => n + t.runs, 0);
    const atLeast = (tier: number) => [...byTier].filter(([t]) => t >= tier).reduce((n, [, c]) => n + c.runs, 0);
    return { eligible, share: (tier: number) => rate(atLeast(tier), eligible) };
  }

  /** Win rates of one item's rows by ascension band and by version, ignoring those two filters */
  breakdown(t: Table, f: Filters, where: (row: Row) => boolean, wins: string, runs: string) {
    const mine = { counts: t.counts, rows: t.rows.filter(where) };
    const byBand = [...sumBy(mine, this.select({ ...f, ascension: 'all' }), (r) => this.groups[r.group].ascension)]
      .filter(([, c]) => c[runs] > 0)
      .sort((a, b) => a[0] - b[0])
      .map(([band, c]) => ({ label: ASCENSION_BANDS[band], runs: c[runs], wins: c[wins] }));
    const byVersion = [...sumBy(mine, this.select({ ...f, version: 'all' }), (r) => this.groups[r.group].version)]
      .filter(([, c]) => c[runs] > 0)
      .sort((a, b) => compareVersions(b[0], a[0]))
      .map(([version, c]) => ({ label: version, runs: c[runs], wins: c[wins] }));
    return { byBand, byVersion };
  }
}

/** The counters that start with a prefix, keyed by the label after it */
export function byPrefix(all: Map<string, Counts>, start: string) {
  const out = new Map<string, Counts>();
  for (const [key, total] of all) if (key.startsWith(start)) out.set(key.slice(start.length), total);
  return out;
}

export const totalCount = (byKey: Map<string, Counts>) => [...byKey.values()].reduce((n, c) => n + c.count, 0);

export interface CardRow {
  deckrate: null | number;
  early_pick_wins: number;
  early_picks: number;
  ferment_plays: number;
  ferment_turns: number;
  held: number;
  held_never_played: number;
  held_twice: number;
  held_twice_wins: number;
  held_wins: number;
  held_with_plays: number;
  id: string;
  offered: number;
  /** The win rate of the middle card of its rarity */
  peer: null | number;
  picked: number;
  pickrate: null | number;
  plays: number;
  playsPerRun: null | number;
  /** Its place by win rate among the cards of its rarity with enough runs */
  rank?: number;
  ranked?: number;
  rarity: string;
  unplayed: null | number;
  upgraded: number;
  vsPeers: null | number;
  winrate: null | number;
}

// Long runs finish with more cards, so a card is measured against the cards of its own rarity: its
// place among them by win rate, and the middle one's win rate
export function cardRows(runs: Runs, on: Selection, min: number) {
  const t = runs.totals(on);
  const info = runs.summary.card_info;
  const rows: CardRow[] = [];
  for (const [id, c] of sumBy(runs.table('cards'), on, (r) => r.card as string)) {
    rows.push({
      id,
      rarity: info[id]?.rarity ?? 'Retired',
      ...(c as unknown as Omit<CardRow, 'id' | 'rarity'>),
      deckrate: rate(c.held, t.runs),
      peer: null,
      pickrate: rate(c.picked, c.offered),
      playsPerRun: rate(c.plays, c.held_with_plays),
      unplayed: rate(c.held_never_played, c.held_with_plays),
      vsPeers: null,
      winrate: rate(c.held_wins, c.held),
    });
  }
  const peers = {} as Record<PoolRarity, null | number>;
  for (const rarity of POOL_RARITIES) {
    const ranked = rows
      .filter((r) => r.rarity === rarity && r.held >= min)
      .sort((a, b) => (b.winrate ?? 0) - (a.winrate ?? 0));
    ranked.forEach((r, i) => Object.assign(r, { rank: i + 1, ranked: ranked.length }));
    peers[rarity] = median(ranked.map((r) => r.winrate ?? 0));
  }
  for (const r of rows) {
    r.peer = inPool(r.rarity) ? peers[r.rarity] : null;
    r.vsPeers = r.peer != null && r.winrate != null ? r.winrate - r.peer : null;
  }
  return new Map(rows.map((r) => [r.id, r]));
}

export interface MixRow {
  perRun: null | number;
  played: null | number;
  share: null | number;
}

/** How often a run makes and plays each kind of Mix, from the tally counters */
export function mixRows(runs: Runs, on: Selection) {
  const all = runs.counters(on);
  const [made, played] = [byPrefix(all, 'mixmade:'), byPrefix(all, 'mixplay:')];
  const t = runs.totals(on);
  const total = totalCount(made);
  const out = new Map<string, MixRow>();
  for (const [id, info] of Object.entries(runs.summary.card_info)) {
    const kind = mixKind(id, info);
    const n = (kind && made.get(kind)?.count) || 0;
    if (kind && n) {
      out.set(id, {
        perRun: rate(n, t.runs_with_tally),
        played: rate(played.get(kind)?.count || 0, n),
        share: rate(n, total),
      });
    }
  }
  return out;
}
