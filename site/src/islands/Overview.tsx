import Bars from '../components/charts/Bars';
import Columns from '../components/charts/Columns';
import Stats from '../components/charts/Stats';
import SectionHeading from '../components/SectionHeading';
import type { LangInit } from '../lib/lang';
import { overview, type OverviewModel } from '../lib/views/overview';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';

interface Props {
  initial: OverviewModel;
  locale: LangInit;
  options: FilterOptions;
}

export default function Overview({ initial, locale, options }: Props) {
  const l = useLang(locale);
  const { filters, model, status, update } = useStats(initial, [], options, (runs, f) => overview(l, runs, f), locale);
  const overallReference = {
    max: 1,
    reference: model.overall,
    referenceLabel: l.t('All runs together: {rate}', { rate: l.pct(model.overall) }),
  };
  return (
    <div aria-busy={status === 'loading'} class="stats-page">
      <Filters filters={filters} l={l} onChange={update} options={options} runs={model.runs} status={status} />
      <Stats items={model.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <SectionHeading id="how-far-runs-get">{l.t('How far runs get')}</SectionHeading>
          <p class="note">{model.funnelNote}</p>
          <Bars items={model.funnel} l={l} labelWidth="8rem" max={1} />
        </section>
        <section class="panel p-5">
          <SectionHeading id="playstyles">{l.t('Playstyles')}</SectionHeading>
          <p class="note">{model.themesNote}</p>
          <Bars items={model.themes} l={l} {...overallReference} labelWidth="8rem" />
        </section>
        <section class="panel p-5">
          <SectionHeading id="win-rate-by-ascension">{l.t('Win rate by ascension')}</SectionHeading>
          <p class="note">
            {l.t(
              'The thin line on each bar is the range the real win rate most likely falls in. Fewer runs means a wider range.',
            )}
          </p>
          <Bars items={model.ascensions} l={l} {...overallReference} labelWidth="5rem" />
        </section>
        <section class="panel p-5">
          <SectionHeading id="win-rate-by-version">{l.t('Win rate by version')}</SectionHeading>
          <p class="note">{l.t('Newest first. This ignores the version filter so every release lines up.')}</p>
          <Bars items={model.versions} l={l} labelWidth="6rem" limit={8} max={1} />
        </section>
      </div>
      <section class="panel p-5">
        <SectionHeading id="runs-per-day">{l.t('Runs per day')}</SectionHeading>
        <p class="note">{l.t('Runs shared each day over the last month, from every version.')}</p>
        <Columns items={model.days} l={l} label={l.t('Runs shared per day over the last 30 days')} />
      </section>
    </div>
  );
}
