import type { Stat } from '../../components/charts/Stats';
import type { Lang } from '../lang';
import { inPool, POOL_RARITIES } from '../mod';
import type { Filters, Runs } from '../runs';
import { type Counts, median, rate, sumBy, zScore } from '../stats';

const LIKELY_Z = 2.58;

export interface CardChange {
  id: string;
  likely: boolean;
  name: string;
  pickDelta: null | number;
  rarity: string;
  relative: number;
  winA: number;
  winB: number;
  zScore: number;
}

export interface VersionsModel {
  a: string;
  b: string;
  changes: CardChange[];
  minRuns: number;
  runs: number;
  stats: Stat[];
}

export function versions(l: Lang, runs: Runs, filters: Filters, versionA: string, versionB: string): VersionsModel {
  const onA = runs.select({ ...filters, version: versionA });
  const onB = runs.select({ ...filters, version: versionB });
  const [totalsA, totalsB] = [runs.totals(onA), runs.totals(onB)];
  const delta = (hitsA: number, nA: number, hitsB: number, nB: number) => {
    const change = (rate(hitsB, nB) ?? NaN) - (rate(hitsA, nA) ?? NaN);
    return Number.isNaN(change) ? null : { text: l.pointChange(change), up: change >= 0 };
  };
  const winZ = zScore(totalsA.wins, totalsA.runs, totalsB.wins, totalsB.runs);

  const minRuns = Math.max(filters.min, 20);
  const cards = runs.table('cards');
  const [cardsA, cardsB] = [onA, onB].map((on) => sumBy(cards, on, (r) => r.card as string));
  const info = runs.summary.card_info;
  const middle = (byCard: Map<string, Counts>, rarity: string) =>
    median(
      [...byCard]
        .filter(([id, c]) => info[id]?.rarity === rarity && c.held >= minRuns)
        .map(([, c]) => c.held_wins / c.held),
    ) ?? 0;
  const rarityShift = Object.fromEntries(POOL_RARITIES.map((r) => [r, middle(cardsB, r) - middle(cardsA, r)]));

  const changes: CardChange[] = [];
  for (const [id, countsA] of cardsA) {
    const countsB = cardsB.get(id);
    const rarity = info[id]?.rarity;
    if (!countsB || countsA.held < minRuns || countsB.held < minRuns || !rarity || !inPool(rarity)) continue;
    const winA = countsA.held_wins / countsA.held;
    const winB = countsB.held_wins / countsB.held;
    const relative = winB - winA - rarityShift[rarity];
    const standardError = Math.sqrt((winA * (1 - winA)) / countsA.held + (winB * (1 - winB)) / countsB.held);
    const z = standardError > 0 ? relative / standardError : 0;
    changes.push({
      id,
      likely: Math.abs(z) > LIKELY_Z,
      name: info[id].name,
      pickDelta:
        countsA.offered && countsB.offered ? countsB.picked / countsB.offered - countsA.picked / countsA.offered : null,
      rarity,
      relative,
      winA,
      winB,
      zScore: Math.abs(z),
    });
  }

  return {
    a: versionA,
    b: versionB,
    changes,
    minRuns,
    runs: totalsA.runs + totalsB.runs,
    stats: [
      {
        label: l.t('Runs in {version}', { version: versionB }),
        note: l.t('{count} in {version}', { count: l.num(totalsA.runs), version: versionA }),
        value: l.num(totalsB.runs),
      },
      {
        delta: delta(totalsA.wins, totalsA.runs, totalsB.wins, totalsB.runs),
        label: l.t('Win rate'),
        note:
          Math.abs(winZ) > 1.96
            ? l.t('was {rate}, a likely change', { rate: l.pct(rate(totalsA.wins, totalsA.runs)) })
            : l.t('was {rate}, within random swing', { rate: l.pct(rate(totalsA.wins, totalsA.runs)) }),
        value: l.pct(rate(totalsB.wins, totalsB.runs)),
      },
      {
        delta: delta(totalsA.reached_act3, totalsA.runs, totalsB.reached_act3, totalsB.runs),
        label: l.t('Reach Act 3'),
        note: l.t('was {rate}', { rate: l.pct(rate(totalsA.reached_act3, totalsA.runs)) }),
        value: l.pct(rate(totalsB.reached_act3, totalsB.runs)),
      },
      {
        label: l.t('Antitoxin peak'),
        note: l.t('was {count}, on average', {
          count: l.fixed(rate(totalsA.antitoxin_peak, totalsA.runs_with_peak), 0),
        }),
        value: l.fixed(rate(totalsB.antitoxin_peak, totalsB.runs_with_peak), 0),
      },
    ],
  };
}
