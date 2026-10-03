import type { BarItem } from '../components/charts/Bars';
import type { Lang } from './lang';
import { notesHref } from './links';
import { rate, wilson } from './stats';

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

export const breakdownBars = (l: Lang, { byBand, byVersion }: { byBand: Row[]; byVersion: Row[] }) => ({
  bands: byBand.map((row) => rateItem(l, l.t(row.label), row.wins, row.runs)),
  versions: byVersion.map((row) => rateItem(l, row.label, row.wins, row.runs, { href: notesHref(row.label) })),
});
