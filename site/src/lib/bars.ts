import type { BarItem } from '../components/charts/Bars';
import type { Lang } from './lang';
import { notesHref } from './links';
import { rate, wilson } from './stats';

/** A win rate bar: the rate, its likely range, and the run count */
export function rateItem(l: Lang, label: string, wins: number, runs: number, extra: Partial<BarItem> = {}): BarItem {
  const [lo, hi] = wilson(wins, runs);
  return {
    hi,
    label,
    lo,
    note: l.n(runs, '{n} run', '{n} runs'),
    text: l.pct(rate(wins, runs)),
    value: rate(wins, runs),
    ...extra,
  };
}

type Row = { label: string; runs: number; wins: number };

/** Win rate bars by ascension band and by version, each version linked to its patch notes */
export const breakdownBars = (l: Lang, { byBand, byVersion }: { byBand: Row[]; byVersion: Row[] }) => ({
  bands: byBand.map((r) => rateItem(l, l.t(r.label), r.wins, r.runs)),
  versions: byVersion.map((r) => rateItem(l, r.label, r.wins, r.runs, { href: notesHref(r.label) })),
});
