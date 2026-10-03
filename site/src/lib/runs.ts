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

export type Names = Pick<Translation, 'badges' | 'names'>;

export function withTranslatedNames(summary: Summary, { badges, names }: Names): Summary {
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

  withSummary(summary: Summary): Runs {
    return Object.assign(Object.create(Runs.prototype), this, { meta: summary.meta, summary });
  }

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

  select(filters: Filters, where: (group: Group) => boolean = () => true): Selection {
    const inVersion = this.versionTest(filters.version);
    return this.groups.map((group) =>
      inVersion(group.version) &&
      (filters.ascension === 'all' || group.ascension === Number(filters.ascension)) &&
      (filters.players === 'all' || group.coop === (filters.players === 'coop' ? 1 : 0)) &&
      (filters.pool === 'all' || group.pool === filters.pool) &&
      (filters.build === 'all' || group.build === filters.build) &&
      where(group)
        ? 1
        : 0,
    );
  }

  get latest() {
    return this.meta.versions.at(-1)!;
  }

  versionSpan(choice: string): [string, string] | null {
    if (choice === 'all') return null;
    if (choice === 'recent') return [this.recentStart, this.latest];
    if (choice.startsWith('>=')) return [choice.slice(2), this.latest];
    return [choice, choice];
  }

  private versionTest(choice: string) {
    const span = this.versionSpan(choice);
    return (version: string) =>
      !span || (compareVersions(version, span[0]) >= 0 && compareVersions(version, span[1]) <= 0);
  }

  private findRecentStart() {
    const solo = this.groups.map((group) => (group.coop ? 0 : 1));
    const byVersion = sumBy(this.tables.totals, solo, (row) => this.groups[row.group].version);
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
    return sumBy(this.tables.counters, on, (row) => row.counter as string);
  }

  withDetailedCounters(on: Selection): Selection {
    const since = this.detailSince;
    return on.map((flag, i) => (flag && since && compareVersions(this.groups[i].version, since) >= 0 ? 1 : 0));
  }

  get detailSince() {
    return this.meta.schema_since?.['3'] ?? null;
  }

  countedSince(l: Lang) {
    return this.detailSince ? l.t('from {version} on', { version: this.detailSince }) : l.t('from the next release on');
  }

  histogram(on: Selection, metric: string, column = 'runs') {
    const { last, width } = this.meta.histograms[metric];
    const counts = sumBy(this.tables.histograms, on, (row) => (row.metric === metric ? (row.bin as number) : -1));
    const bins = [];
    for (let bin = 0; bin <= last; bin += width) bins.push({ bin, runs: counts.get(bin)?.[column] || 0 });
    return { bins, last, width };
  }

  badgeShares(on: Selection, badgeId: string) {
    const byTier = sumBy(this.tables.badges, on, (row) => (row.badge === badgeId ? (row.tier as number) : -1));
    byTier.delete(-1);
    const eligible = [...byTier.values()].reduce((total, counts) => total + counts.runs, 0);
    const atLeast = (tier: number) =>
      [...byTier].filter(([rowTier]) => rowTier >= tier).reduce((total, [, counts]) => total + counts.runs, 0);
    return { eligible, share: (tier: number) => rate(atLeast(tier), eligible) };
  }

  breakdown(source: Table, filters: Filters, where: (row: Row) => boolean, winsColumn: string, runsColumn: string) {
    const itemRows = { counts: source.counts, rows: source.rows.filter(where) };
    const allBands = this.select({ ...filters, ascension: 'all' });
    const allVersions = this.select({ ...filters, version: 'all' });
    const byBand = [...sumBy(itemRows, allBands, (row) => this.groups[row.group].ascension)]
      .filter(([, counts]) => counts[runsColumn] > 0)
      .sort((a, b) => a[0] - b[0])
      .map(([band, counts]) => ({ label: ASCENSION_BANDS[band], runs: counts[runsColumn], wins: counts[winsColumn] }));
    const byVersion = [...sumBy(itemRows, allVersions, (row) => this.groups[row.group].version)]
      .filter(([, counts]) => counts[runsColumn] > 0)
      .sort((a, b) => compareVersions(b[0], a[0]))
      .map(([version, counts]) => ({ label: version, runs: counts[runsColumn], wins: counts[winsColumn] }));
    return { byBand, byVersion };
  }
}

export function byPrefix(counters: Map<string, Counts>, prefix: string) {
  const byLabel = new Map<string, Counts>();
  for (const [key, counts] of counters) if (key.startsWith(prefix)) byLabel.set(key.slice(prefix.length), counts);
  return byLabel;
}

export const totalCount = (byKey: Map<string, Counts>) =>
  [...byKey.values()].reduce((total, counts) => total + counts.count, 0);

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
  peer: null | number;
  picked: number;
  pickrate: null | number;
  plays: number;
  playsPerRun: null | number;
  rank?: number;
  ranked?: number;
  rarity: string;
  unplayed: null | number;
  upgraded: number;
  vsPeers: null | number;
  winrate: null | number;
}

export function cardRows(runs: Runs, on: Selection, min: number) {
  const totals = runs.totals(on);
  const info = runs.summary.card_info;
  const rows: CardRow[] = [];
  for (const [id, counts] of sumBy(runs.table('cards'), on, (row) => row.card as string)) {
    rows.push({
      id,
      rarity: info[id]?.rarity ?? 'Retired',
      ...(counts as unknown as Omit<CardRow, 'id' | 'rarity'>),
      deckrate: rate(counts.held, totals.runs),
      peer: null,
      pickrate: rate(counts.picked, counts.offered),
      playsPerRun: rate(counts.plays, counts.held_with_plays),
      unplayed: rate(counts.held_never_played, counts.held_with_plays),
      vsPeers: null,
      winrate: rate(counts.held_wins, counts.held),
    });
  }
  const medianWinrate = {} as Record<PoolRarity, null | number>;
  for (const rarity of POOL_RARITIES) {
    const ranked = rows
      .filter((row) => row.rarity === rarity && row.held >= min)
      .sort((a, b) => (b.winrate ?? 0) - (a.winrate ?? 0));
    ranked.forEach((row, i) => Object.assign(row, { rank: i + 1, ranked: ranked.length }));
    medianWinrate[rarity] = median(ranked.map((row) => row.winrate ?? 0));
  }
  for (const row of rows) {
    row.peer = inPool(row.rarity) ? medianWinrate[row.rarity] : null;
    row.vsPeers = row.peer != null && row.winrate != null ? row.winrate - row.peer : null;
  }
  return new Map(rows.map((row) => [row.id, row]));
}

export interface MixRow {
  perRun: null | number;
  played: null | number;
  share: null | number;
}

export function mixRows(runs: Runs, on: Selection) {
  const counters = runs.counters(on);
  const [made, played] = [byPrefix(counters, 'mixmade:'), byPrefix(counters, 'mixplay:')];
  const totals = runs.totals(on);
  const allMade = totalCount(made);
  const rowsById = new Map<string, MixRow>();
  for (const [id, info] of Object.entries(runs.summary.card_info)) {
    const kind = mixKind(id, info);
    const madeCount = (kind && made.get(kind)?.count) || 0;
    if (kind && madeCount) {
      rowsById.set(id, {
        perRun: rate(madeCount, totals.runs_with_tally),
        played: rate(played.get(kind)?.count || 0, madeCount),
        share: rate(madeCount, allMade),
      });
    }
  }
  return rowsById;
}
