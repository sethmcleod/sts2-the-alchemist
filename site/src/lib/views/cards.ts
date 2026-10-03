import type { BarItem } from '../../components/charts/Bars';
import type { Point } from '../../components/charts/Scatter';
import type { Stat } from '../../components/charts/Stats';
import { rateItem } from '../bars';
import type { Lang } from '../lang';
import { cardHref } from '../links';
import { inPool, type PoolRarity, RARITY_NAME } from '../mod';
import { type CardRow, cardRows, type Filters, type Runs } from '../runs';
import { median, rate, sum, wilson } from '../stats';
import type { Rarity } from '../types';

export const RARITY_FILL: Record<string, string> = {
  Common: 'var(--color-common)',
  Rare: 'var(--color-rare)',
  Uncommon: 'var(--color-uncommon)',
};

export const rarityName = (l: Lang, rarity: string) =>
  l.game?.rarities[rarity] ?? l.t(RARITY_NAME[rarity as Rarity] ?? rarity);

const RANK_AMONG: Record<PoolRarity, (l: Lang, rank: string, count: number) => string> = {
  Common: (l, rank, count) => l.n(count, '{rank} of {n} Common', '{rank} of {n} Commons', { rank }),
  Rare: (l, rank, count) => l.n(count, '{rank} of {n} Rare', '{rank} of {n} Rares', { rank }),
  Uncommon: (l, rank, count) => l.n(count, '{rank} of {n} Uncommon', '{rank} of {n} Uncommons', { rank }),
};

const tagName = (l: Lang, tag: string) => (tag === 'Multiplayer' ? l.t('Multiplayer') : tag);

export interface CardTableRow {
  held: number;
  id: string;
  name: string;
  offered: number;
  pickrate: null | number;
  playsPerRun: null | number;
  range: string;
  rank: null | string;
  rarity: string;
  tags: string[];
  unplayed: null | number;
  vsPeers: null | number;
  winrate: null | number;
}

export interface CardStatsModel {
  countedSince: string;
  down: BarItem[];
  early: BarItem[];
  middle: null | number;
  mostPlayed: BarItem[];
  overall: null | number;
  points: Point[];
  runs: number;
  stats: Stat[];
  table: CardTableRow[];
  unplayed: BarItem[];
  up: BarItem[];
  upgrades: BarItem[];
}

export function cardStats(l: Lang, runs: Runs, filters: Filters): CardStatsModel {
  const on = runs.select(filters);
  const totals = runs.totals(on);
  const info = runs.summary.card_info;
  const named = (r: CardRow) => ({ ...r, name: info[r.id]?.name ?? r.id });
  const rows = [...cardRows(runs, on, filters.min).values()].filter((r) => info[r.id]).map(named);
  const measured = rows.filter((r) => inPool(r.rarity) && r.held >= filters.min);
  const overall = rate(totals.wins, totals.runs);
  const link = (r: CardRow) => ({ href: cardHref(r.id), note: rarityName(l, r.rarity) });

  const standout = (r: CardRow & { name: string }) =>
    rateItem(l, r.name, r.held_wins, r.held, {
      ...link(r),
      ref: r.peer,
      textNote: l.t('{rank} of {count}', { count: l.num(r.ranked), rank: l.ordinal(r.rank!) }),
    });
  const byGap = [...measured].sort((a, b) => (b.vsPeers ?? 0) - (a.vsPeers ?? 0));
  const middle = median(measured.map((r) => r.winrate ?? 0));

  const tracked = rows.filter((r) => r.held_with_plays >= filters.min && r.rarity !== 'Token');
  const early = rows.filter((r) => r.early_picks >= filters.min).sort((a, b) => b.early_picks - a.early_picks);
  const deckSize = Math.round(rate(totals.deck_size, totals.runs) ?? NaN);
  const upgraded = rows
    .filter((r) => r.held >= filters.min && r.upgraded > 0)
    .map((r) => ({ ...r, per100: (100 * r.upgraded) / r.held }))
    .sort((a, b) => b.per100 - a.per100);

  return {
    countedSince: runs.countedSince(l),
    down: byGap
      .filter((r) => (r.vsPeers ?? 0) < 0)
      .slice(-6)
      .reverse()
      .map(standout),
    early: early.map((r) =>
      rateItem(l, r.name, r.early_pick_wins, r.early_picks, {
        href: cardHref(r.id),
        note: l.n(r.early_picks, '{n} pick', '{n} picks'),
      }),
    ),
    middle,
    mostPlayed: tracked
      .filter((r) => r.rarity !== 'Basic')
      .sort((a, b) => (b.playsPerRun ?? 0) - (a.playsPerRun ?? 0))
      .map((r) => ({
        label: r.name,
        ...link(r),
        text: l.fixed(r.playsPerRun, 1),
        textNote: l.t('a run'),
        value: r.playsPerRun,
      })),
    overall,
    points: measured
      .filter((r) => r.offered > 0)
      .map((r) => ({
        fill: RARITY_FILL[r.rarity],
        href: cardHref(r.id),
        label: r.name,
        r: Math.max(4, Math.min(9, 3 + Math.sqrt(r.held) / 3)),
        tip: [
          l.t('{name} ({rarity})', { name: r.name, rarity: rarityName(l, r.rarity) }),
          l.n(r.held, 'Wins {rate} of {n} run', 'Wins {rate} of {n} runs', { rate: l.pct(r.winrate) }),
          l.n(r.offered, 'Picked {rate} of {n} time offered', 'Picked {rate} of {n} times offered', {
            rate: l.pct(r.pickrate),
          }),
        ],
        weight: r.held,
        x: r.pickrate ?? 0,
        y: r.winrate ?? 0,
      })),
    runs: totals.runs,
    stats: [
      {
        label: l.t('Final deck'),
        note: l.t('{count} in a winning run', { count: l.fixed(rate(totals.win_deck_size, totals.wins), 0) }),
        value: l.n(deckSize, '{n} card', '{n} cards'),
      },
      {
        label: l.t('Card choices'),
        note: l.t('per run, rewards and events'),
        value: l.fixed(rate(totals.reward_screens, totals.runs), 0),
      },
      {
        label: l.t('Skipped'),
        note: l.t('of card choices'),
        value: l.pct(rate(totals.reward_skips, totals.reward_screens)),
      },
      {
        label: l.t('Upgrades'),
        note: l.t('per run, at rest sites'),
        value: l.fixed(rate(sum(runs.table('cards'), on).upgraded, totals.runs), 1),
      },
    ],
    table: rows
      .filter((r) => r.held >= filters.min)
      .map((r) => ({
        held: r.held,
        id: r.id,
        name: r.name,
        offered: r.offered,
        pickrate: r.pickrate,
        playsPerRun: r.playsPerRun,
        range: l.range(wilson(r.held_wins, r.held)),
        rank: r.rank ? RANK_AMONG[r.rarity as PoolRarity](l, l.ordinal(r.rank), r.ranked!) : null,
        rarity: rarityName(l, r.rarity),
        tags: info[r.id].tags.map((tag) => tagName(l, tag)),
        unplayed: r.unplayed,
        vsPeers: r.vsPeers,
        winrate: r.winrate,
      })),
    unplayed: [...tracked]
      .sort((a, b) => (b.unplayed ?? 0) - (a.unplayed ?? 0))
      .map((r) => ({
        label: r.name,
        ...link(r),
        text: l.pct(r.unplayed),
        textNote: l.n(r.held_with_plays, 'of {n} run', 'of {n} runs'),
        value: r.unplayed,
      })),
    up: byGap
      .filter((r) => (r.vsPeers ?? 0) > 0)
      .slice(0, 6)
      .map(standout),
    upgrades: upgraded.map((r) => ({
      label: r.name,
      ...link(r),
      text: l.fixed(r.per100, 0),
      textNote: l.t('per 100'),
      value: r.per100,
    })),
  };
}
