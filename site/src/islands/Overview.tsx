import Bars from '../components/charts/Bars';
import Columns from '../components/charts/Columns';
import Stats from '../components/charts/Stats';
import type { LangInit } from '../lib/lang';
import { overview, type OverviewModel } from '../lib/views/overview';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';

interface Props {
  locale: LangInit;
  initial: OverviewModel;
  options: FilterOptions;
}

export default function Overview({ locale, initial, options }: Props) {
  const l = useLang(locale);
  const {
    filters,
    model: m,
    status,
    update,
  } = useStats(initial, [], options, (runs, f) => overview(l, runs, f), locale);
  const overall = {
    max: 1,
    reference: m.overall,
    referenceLabel: l.t('All runs together: {rate}', { rate: l.pct(m.overall) }),
  };
  return (
    <div class="stats-page" aria-busy={status === 'loading'}>
      <Filters l={l} filters={filters} options={options} runs={m.runs} status={status} onChange={update} />
      <Stats items={m.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h2>{l.t('How far runs get')}</h2>
          <p class="note">{m.funnelNote}</p>
          <Bars l={l} items={m.funnel} max={1} labelWidth="8rem" />
        </section>
        <section class="panel p-5">
          <h2>{l.t('Playstyles')}</h2>
          <p class="note">{m.themesNote}</p>
          <Bars l={l} items={m.themes} {...overall} labelWidth="8rem" />
        </section>
        <section class="panel p-5">
          <h2>{l.t('Win rate by ascension')}</h2>
          <p class="note">
            {l.t(
              'The thin line on each bar is the range the real win rate most likely falls in. Fewer runs means a wider range.',
            )}
          </p>
          <Bars l={l} items={m.ascensions} {...overall} labelWidth="5rem" />
        </section>
        <section class="panel p-5">
          <h2>{l.t('Win rate by version')}</h2>
          <p class="note">{l.t('Newest first. This ignores the version filter so every release lines up.')}</p>
          <Bars l={l} items={m.versions} max={1} limit={8} labelWidth="6rem" />
        </section>
      </div>
      <section class="panel p-5">
        <h2>{l.t('Runs per day')}</h2>
        <p class="note">{l.t('Runs shared each day over the last month, from every version.')}</p>
        <Columns l={l} items={m.days} label={l.t('Runs shared per day over the last 30 days')} />
      </section>
    </div>
  );
}
