import { describe, expect, it } from 'vitest';
import { cardRows, DEFAULT_FILTERS, Runs } from './runs';
import type { CardInfo, Summary, TableFile } from './types';

const TOTALS = ['runs', 'wins'];
const empty = (key: string[], counts: string[]): TableFile => ({ key, counts, rows: [] });
const card = (rarity: CardInfo['rarity']) => ({ name: rarity, rarity, tags: [] }) as unknown as CardInfo;

// Three versions of solo runs, and one group of multiplayer runs on the newest
function summary(): Summary {
  return {
    meta: {
      versions: ['v0.1.0', 'v0.2.0', 'v0.3.0'],
      prefix: 'X-',
      histograms: {},
      schema_since: {},
    },
    names: {},
    card_info: { 'X-A': card('Common'), 'X-B': card('Common'), 'X-C': card('Common'), 'X-S': card('Basic') },
    relic_info: {},
    potion_info: {},
    power_info: {},
    icons: {},
    groups: {
      key: ['version', 'build', 'ascension', 'pool', 'coop'],
      counts: [],
      rows: [
        ['v0.1.0', 'b1', 0, 'full', 0],
        ['v0.2.0', 'b1', 0, 'full', 0],
        ['v0.3.0', 'b1', 0, 'full', 0],
        ['v0.3.0', 'b1', 0, 'full', 1],
      ],
    },
    totals: {
      key: ['group'],
      counts: TOTALS,
      rows: [
        [0, 400, 100],
        [1, 300, 120],
        [2, 250, 110],
        [3, 900, 300],
      ],
    },
    ascensions: empty(['group', 'ascension'], TOTALS),
    days: empty(['group', 'day'], TOTALS),
    themes: empty(['group', 'theme'], TOTALS),
    badges: empty(['group', 'badge', 'tier'], ['runs']),
    histograms: empty(['group', 'metric', 'bin'], TOTALS),
    counters: empty(['group', 'counter'], ['count', 'runs']),
    acts: empty(['group', 'act'], ['fights', 'turns', 'damage']),
    death_floors: empty(['group', 'floor'], ['deaths']),
  } as unknown as Summary;
}

const CARD_COUNTS = [
  'held',
  'held_wins',
  'held_twice',
  'held_twice_wins',
  'offered',
  'picked',
  'early_picks',
  'early_pick_wins',
  'upgraded',
  'held_with_plays',
  'plays',
  'held_never_played',
  'ferment_plays',
  'ferment_turns',
];
const cardRow = (group: number, id: string, held: number, wins: number) => [
  group,
  id,
  held,
  wins,
  ...Array(CARD_COUNTS.length - 2).fill(0),
];

describe('Runs', () => {
  const runs = new Runs(summary(), {
    cards: {
      key: ['group', 'card'],
      counts: CARD_COUNTS,
      rows: [cardRow(2, 'X-A', 50, 30), cardRow(2, 'X-B', 40, 20), cardRow(2, 'X-C', 5, 5), cardRow(2, 'X-S', 90, 40)],
    },
  });

  it('starts "recent versions" where the newest ones reach 500 solo runs', () => {
    expect(runs.recentStart).toBe('v0.2.0');
    expect(runs.totals(runs.select(DEFAULT_FILTERS)).runs).toBe(550);
  });

  it('filters by players and by one version', () => {
    expect(runs.totals(runs.select({ ...DEFAULT_FILTERS, players: 'coop' })).runs).toBe(900);
    expect(runs.totals(runs.select({ ...DEFAULT_FILTERS, version: 'v0.1.0' })).runs).toBe(400);
    expect(runs.totals(runs.select({ ...DEFAULT_FILTERS, version: '>=v0.3.0', players: 'all' })).runs).toBe(1150);
  });

  it('ranks a card among the cards of its rarity with enough runs', () => {
    const rows = cardRows(runs, runs.select(DEFAULT_FILTERS), 10);
    expect(rows.get('X-A')).toMatchObject({ rank: 1, ranked: 2, winrate: 0.6 });
    expect(rows.get('X-B')).toMatchObject({ rank: 2, ranked: 2 });
    expect(rows.get('X-A')!.peer).toBeCloseTo(0.55);
    // Too few runs to rank, and a starter card has no peers
    expect(rows.get('X-C')!.rank).toBeUndefined();
    expect(rows.get('X-S')!.peer).toBeNull();
  });

  it('names a model it has no words for from its id', () => {
    expect(runs.name('X-KNOWLEDGE_OF_THE_DEEP')).toBe('Knowledge of the Deep');
  });
});
