import type { Stat } from '../../components/charts/Stats';
import type { Lang } from '../lang';
import { inPool, POOL_RARITIES } from '../mod';
import type { Filters, Runs } from '../runs';
import { type Counts, median, rate, sumBy, zScore } from '../stats';

// Comparing many cards at once, a 95% test flags a few by chance, so card changes need 99%
const LIKELY_Z = 2.58;

export interface CardChange {
  confidence: number;
  id: string;
  likely: boolean;
  name: string;
  pickDelta: null | number;
  rarity: string;
  relative: number;
  winA: number;
  winB: number;
}

export interface VersionsModel {
  a: string;
  b: string;
  changes: CardChange[];
  enough: number;
  runs: number;
  stats: Stat[];
}

export function versions(l: Lang, runs: Runs, f: Filters, a: string, b: string): VersionsModel {
  const onA = runs.select({ ...f, version: a });
  const onB = runs.select({ ...f, version: b });
  const [tA, tB] = [runs.totals(onA), runs.totals(onB)];
  const delta = (hitsA: number, nA: number, hitsB: number, nB: number) => {
    const d = (rate(hitsB, nB) ?? NaN) - (rate(hitsA, nA) ?? NaN);
    return Number.isNaN(d) ? null : { text: l.change(d), up: d >= 0 };
  };
  const winZ = zScore(tA.wins, tA.runs, tB.wins, tB.runs);

  // Each side needs enough runs for a rate to mean something. A version that is harder overall
  // drags every card down with it, so each card is measured against the middle card of its rarity
  const enough = Math.max(f.min, 20);
  const cards = runs.table('cards');
  const [cardsA, cardsB] = [onA, onB].map((on) => sumBy(cards, on, (r) => r.card as string));
  const info = runs.summary.card_info;
  const middle = (byCard: Map<string, Counts>, rarity: string) =>
    median(
      [...byCard]
        .filter(([id, c]) => info[id]?.rarity === rarity && c.held >= enough)
        .map(([, c]) => c.held_wins / c.held),
    ) ?? 0;
  const shift = Object.fromEntries(POOL_RARITIES.map((r) => [r, middle(cardsB, r) - middle(cardsA, r)]));

  const changes: CardChange[] = [];
  for (const [id, ca] of cardsA) {
    const cb = cardsB.get(id);
    const rarity = info[id]?.rarity;
    if (!cb || ca.held < enough || cb.held < enough || !rarity || !inPool(rarity)) continue;
    const winA = ca.held_wins / ca.held;
    const winB = cb.held_wins / cb.held;
    const relative = winB - winA - shift[rarity];
    const se = Math.sqrt((winA * (1 - winA)) / ca.held + (winB * (1 - winB)) / cb.held);
    const z = se > 0 ? relative / se : 0;
    changes.push({
      confidence: Math.abs(z),
      id,
      likely: Math.abs(z) > LIKELY_Z,
      name: info[id].name,
      pickDelta: ca.offered && cb.offered ? cb.picked / cb.offered - ca.picked / ca.offered : null,
      rarity,
      relative,
      winA,
      winB,
    });
  }

  return {
    a,
    b,
    changes,
    enough,
    runs: tA.runs + tB.runs,
    stats: [
      {
        label: l.t('Runs in {version}', { version: b }),
        note: l.t('{count} in {version}', { count: l.num(tA.runs), version: a }),
        value: l.num(tB.runs),
      },
      {
        delta: delta(tA.wins, tA.runs, tB.wins, tB.runs),
        label: l.t('Win rate'),
        note:
          Math.abs(winZ) > 1.96
            ? l.t('was {rate}, a likely change', { rate: l.pct(rate(tA.wins, tA.runs)) })
            : l.t('was {rate}, within random swing', { rate: l.pct(rate(tA.wins, tA.runs)) }),
        value: l.pct(rate(tB.wins, tB.runs)),
      },
      {
        delta: delta(tA.reached_act3, tA.runs, tB.reached_act3, tB.runs),
        label: l.t('Reach Act 3'),
        note: l.t('was {rate}', { rate: l.pct(rate(tA.reached_act3, tA.runs)) }),
        value: l.pct(rate(tB.reached_act3, tB.runs)),
      },
      {
        label: l.t('Antitoxin peak'),
        note: l.t('was {count}, on average', { count: l.fixed(rate(tA.antitoxin_peak, tA.runs_with_peak), 0) }),
        value: l.fixed(rate(tB.antitoxin_peak, tB.runs_with_peak), 0),
      },
    ],
  };
}
