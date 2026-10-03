import type { BarItem } from '../../components/charts/Bars';
import { rateItem } from '../bars';
import type { Lang } from '../lang';
import { potionHref, relicHref } from '../links';
import type { Filters, Runs } from '../runs';
import { median, rate, sumBy } from '../stats';
import type { Icons } from './mechanics';

export interface RelicStatsModel {
  ancients: BarItem[];
  baseRelics: BarItem[];
  even: null | number;
  middle: null | number;
  modRelics: BarItem[];
  potions: BarItem[];
  runs: number;
}

export function relicStats(l: Lang, runs: Runs, f: Filters, icons: Icons): RelicStatsModel {
  const on = runs.select(f);
  const t = runs.totals(on);
  const { potion_info, relic_info } = runs.summary;
  const relics = [...sumBy(runs.table('relics'), on, (r) => r.relic as string)].map(([id, c]) => ({
    held: c.held,
    held_wins: c.held_wins,
    id,
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
    ancients: offered.map((r) =>
      rateItem(l, runs.name(r.id), r.picked, r.offered, {
        href: relic_info[r.id] ? relicHref(r.id) : undefined,
        icon: icons[r.id] ?? null,
        note: l.n(r.offered, '{n} offer', '{n} offers'),
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
    even,
    middle,
    modRelics: relics
      .filter((r) => r.id.startsWith(runs.meta.prefix) && r.held >= f.min)
      .sort((a, b) => b.held - a.held)
      .map((r) =>
        rateItem(l, runs.name(r.id), r.held_wins, r.held, {
          href: relic_info[r.id] ? relicHref(r.id) : undefined,
          icon: icons[r.id] ?? null,
          note: l.t('in {share} of runs', { share: l.pct(rate(r.held, t.runs)) }),
        }),
      ),
    potions: [...sumBy(runs.table('potions'), on, (r) => r.potion as string)]
      .map(([id, c]) => ({ bought: c.bought, drunk: c.drunk, id, per100: (100 * c.drunk) / (t.runs_with_drinks || 1) }))
      .filter((p) => p.drunk >= f.min)
      .sort((a, b) => b.per100 - a.per100)
      .map((p) => ({
        href: potion_info[p.id] ? potionHref(p.id) : undefined,
        icon: icons[p.id] ?? null,
        label: runs.name(p.id),
        text: l.fixed(p.per100, 0),
        textNote: p.bought ? l.t('{count} bought', { count: l.num(p.bought) }) : null,
        value: p.per100,
      })),
    runs: t.runs,
  };
}
