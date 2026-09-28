import type { BarItem } from '../../components/charts/Bars';
import { rateItem } from '../bars';
import type { Lang } from '../lang';
import { potionHref, relicHref } from '../links';
import type { Filters, Runs } from '../runs';
import { median, rate, sumBy } from '../stats';
import type { Icons } from './mechanics';

export interface RelicStatsModel {
  runs: number;
  modRelics: BarItem[];
  ancients: BarItem[];
  even: number | null;
  potions: BarItem[];
  baseRelics: BarItem[];
  middle: number | null;
}

export function relicStats(l: Lang, runs: Runs, f: Filters, icons: Icons): RelicStatsModel {
  const on = runs.select(f);
  const t = runs.totals(on);
  const { relic_info, potion_info } = runs.summary;
  const relics = [...sumBy(runs.table('relics'), on, (r) => r.relic as string)].map(([id, c]) => ({
    id,
    held: c.held,
    held_wins: c.held_wins,
    offered: c.offered,
    picked: c.picked,
  }));
  const middle = median(relics.filter((r) => r.held >= f.min).map((r) => r.held_wins / r.held));

  const offered = relics.filter((r) => r.offered >= f.min).sort((a, b) => b.offered - a.offered);
  const even = rate(
    offered.reduce((n, r) => n + r.picked, 0),
    offered.reduce((n, r) => n + r.offered, 0),
  );

  return {
    runs: t.runs,
    even,
    middle,
    ancients: offered.map((r) =>
      rateItem(l, runs.name(r.id), r.picked, r.offered, {
        note: l.n(r.offered, '{n} offer', '{n} offers'),
        icon: icons[r.id] ?? null,
        href: relic_info[r.id] ? relicHref(r.id) : undefined,
      }),
    ),
    potions: [...sumBy(runs.table('potions'), on, (r) => r.potion as string)]
      .map(([id, c]) => ({ id, drunk: c.drunk, bought: c.bought, per100: (100 * c.drunk) / (t.runs_with_drinks || 1) }))
      .filter((p) => p.drunk >= f.min)
      .sort((a, b) => b.per100 - a.per100)
      .map((p) => ({
        label: runs.name(p.id),
        icon: icons[p.id] ?? null,
        href: potion_info[p.id] ? potionHref(p.id) : undefined,
        value: p.per100,
        text: l.fixed(p.per100, 0),
        textNote: p.bought ? l.t('{count} bought', { count: l.num(p.bought) }) : null,
      })),
    modRelics: relics
      .filter((r) => r.id.startsWith(runs.meta.prefix) && r.held >= f.min)
      .sort((a, b) => b.held - a.held)
      .map((r) =>
        rateItem(l, runs.name(r.id), r.held_wins, r.held, {
          note: l.t('in {share} of runs', { share: l.pct(rate(r.held, t.runs)) }),
          icon: icons[r.id] ?? null,
          href: relic_info[r.id] ? relicHref(r.id) : undefined,
        }),
      ),
    baseRelics: relics
      .filter((r) => !r.id.startsWith(runs.meta.prefix) && r.held >= f.min)
      .sort((a, b) => b.held - a.held)
      .map((r) =>
        rateItem(l, runs.name(r.id), r.held_wins, r.held, {
          note: l.t('in {share} of runs', { share: l.pct(rate(r.held, t.runs)) }),
        }),
      ),
  };
}
