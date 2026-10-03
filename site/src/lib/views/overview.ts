import type { BarItem } from '../../components/charts/Bars';
import type { Column } from '../../components/charts/Columns';
import type { Stat } from '../../components/charts/Stats';
import { rateItem } from '../bars';
import type { Lang } from '../lang';
import { notesHref } from '../links';
import type { Filters, Runs } from '../runs';
import { compareVersions, histogramMedian, rate, sumBy } from '../stats';

export interface OverviewModel {
  ascensions: BarItem[];
  days: Column[];
  funnel: BarItem[];
  funnelNote: string;
  overall: null | number;
  runs: number;
  stats: Stat[];
  themes: BarItem[];
  themesNote: string;
  versions: BarItem[];
}

export function overview(l: Lang, runs: Runs, filters: Filters): OverviewModel {
  const on = runs.select(filters);
  const totals = runs.totals(on);
  const overall = rate(totals.wins, totals.runs);
  const runLength = runs.histogram(on, 'run_minutes');
  const winLength = runs.histogram(on, 'run_minutes', 'wins');
  const per100 = (n: number) => Math.round((100 * n) / totals.runs);

  const stages: [string, number][] = [
    [l.t('Reach Act 2'), totals.reached_act2],
    [l.t('Reach Act 3'), totals.reached_act3],
    [l.t('Win'), totals.wins],
  ];

  const ascensions = [...sumBy(runs.tables.ascensions, on, (r) => r.ascension as number)]
    .sort((a, b) => a[0] - b[0])
    .filter(([, g]) => g.runs >= filters.min)
    .map(([ascension, g]) => rateItem(l, l.t('A{n}', { n: ascension }), g.wins, g.runs));

  const themeNames = new Map(runs.meta.themes.map((theme) => [theme, l.game?.words[theme] ?? l.t(theme)]));
  const themes = [...sumBy(runs.tables.themes, on, (r) => r.theme as string)]
    .filter(([, g]) => g.runs >= filters.min)
    .sort((a, b) => b[1].runs - a[1].runs)
    .map(([theme, g]) =>
      rateItem(l, themeNames.get(theme) ?? theme, g.wins, g.runs, {
        note: l.t('{share} of runs', { share: l.pct(rate(g.runs, totals.runs)) }),
      }),
    );

  const everyVersion = runs.select({ ...filters, version: 'all' });
  const versions = [...sumBy(runs.tables.totals, everyVersion, (r) => runs.groups[r.group].version)]
    .filter(([, g]) => g.runs >= filters.min)
    .sort((a, b) => compareVersions(b[0], a[0]))
    .map(([version, g]) => rateItem(l, version, g.wins, g.runs, { href: notesHref(version) }));

  const byDay = sumBy(runs.tables.days, everyVersion, (r) => r.day as string);
  const lastDay = new Date(`${runs.meta.last_day}T00:00:00Z`);
  const days: Column[] = [];
  for (let i = 29; i >= 0; i--) {
    const day = new Date(lastDay);
    day.setUTCDate(lastDay.getUTCDate() - i);
    const counts = byDay.get(day.toISOString().slice(0, 10)) ?? { runs: 0, wins: 0 };
    const label = day.toLocaleDateString(l.lang, { day: 'numeric', month: 'short', timeZone: 'UTC' });
    days.push({
      label,
      tip: [
        label,
        l.n(counts.runs, '{n} run', '{n} runs'),
        counts.runs ? l.t('{share} won', { share: l.pct(counts.wins / counts.runs) }) : l.t('no runs'),
      ],
      value: counts.runs,
    });
  }

  return {
    ascensions,
    days,
    funnel: totals.runs
      ? stages.map(([label, n], i) => ({
          fill: `var(--stage-${i + 1})`,
          label,
          text: l.pct(n / totals.runs),
          textNote: l.num(n),
          value: n / totals.runs,
        }))
      : [],
    funnelNote: totals.runs
      ? l.t('Out of every 100 runs, {act2} reach Act 2, {act3} reach Act 3 and {wins} win.', {
          act2: l.num(per100(totals.reached_act2)),
          act3: l.num(per100(totals.reached_act3)),
          wins: l.num(per100(totals.wins)),
        })
      : '',
    overall,
    runs: totals.runs,
    stats: [
      {
        label: l.t('Win rate'),
        note: l.n(totals.runs, '{wins} from {n} run', '{wins} from {n} runs', {
          wins: l.n(totals.wins, '{n} win', '{n} wins'),
        }),
        value: l.pct(overall),
      },
      {
        label: l.t('Reach Act 3'),
        note: l.t('{share} reach Act 2', { share: l.pct(rate(totals.reached_act2, totals.runs)) }),
        value: l.pct(rate(totals.reached_act3, totals.runs)),
      },
      {
        label: l.t('Run length'),
        note: l.t('for the middle run. The middle win takes {time}', {
          time: l.minutes(histogramMedian(winLength.bins, winLength.width)),
        }),
        value: l.minutes(histogramMedian(runLength.bins, runLength.width)),
      },
      {
        label: l.t('Antitoxin peak'),
        note: l.t('the most held at once, on average'),
        value: l.fixed(rate(totals.antitoxin_peak, totals.runs_with_peak), 0),
      },
    ],
    themes,
    themesNote: l.n(
      runs.meta.theme_min_cards,
      'Each run counts toward the theme with the most cards in its final deck. A deck with fewer than {n} card of any one theme counts as {unfocused}.',
      'Each run counts toward the theme with the most cards in its final deck. A deck with fewer than {n} cards of any one theme counts as {unfocused}.',
      { unfocused: l.t('Unfocused') },
    ),
    versions,
  };
}
