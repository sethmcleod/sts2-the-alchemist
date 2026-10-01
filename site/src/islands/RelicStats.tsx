import Bars from '../components/charts/Bars';
import type { LangInit } from '../lib/lang';
import type { Icons } from '../lib/views/mechanics';
import { relicStats, type RelicStatsModel } from '../lib/views/relics';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';
import SectionHeading from '../components/SectionHeading';

interface Props {
  locale: LangInit;
  initial: RelicStatsModel;
  options: FilterOptions;
  icons: Icons;
}

export default function RelicStats({ locale, initial, options, icons }: Props) {
  const l = useLang(locale);
  const {
    filters,
    model: m,
    status,
    update,
  } = useStats(initial, ['relics', 'potions'], options, (runs, f) => relicStats(l, runs, f, icons), locale);
  const middle = l.t('The gold line is the middle relic, at {rate}', { rate: l.pct(m.middle) });
  return (
    <div class="stats-page" aria-busy={status === 'loading'}>
      <Filters l={l} filters={filters} options={options} runs={m.runs} status={status} onChange={update} />
      <section class="panel p-5">
        <SectionHeading id="alchemist-relics">{l.t('Alchemist relics')}</SectionHeading>
        <p class="note">{l.t('How often the runs that ended with each relic won.')}</p>
        <Bars l={l} items={m.modRelics} max={1} reference={m.middle} referenceLabel={middle} labelWidth="13rem" />
      </section>
      <div class="stats-grid">
        <section class="panel p-5">
          <SectionHeading id="ancient-choices">{l.t('Ancient choices')}</SectionHeading>
          <p class="note">{l.t('How often players take each relic an Ancient offers.')}</p>
          <Bars
            l={l}
            items={m.ancients}
            max={1}
            limit={10}
            reference={m.even}
            referenceLabel={l.t('The gold line is an even share, {rate}', { rate: l.pct(m.even) })}
            labelWidth="11rem"
          />
        </section>
        <section class="panel p-5">
          <SectionHeading id="potions">{l.t('Potions')}</SectionHeading>
          <p class="note">
            {l.t("Potions drunk for every 100 runs. The Alchemist's own potions link to their pages.")}
          </p>
          <Bars l={l} items={m.potions} limit={12} labelWidth="11rem" />
        </section>
      </div>
      <section class="panel p-5">
        <SectionHeading id="base-game-relics">{l.t('Base game relics')}</SectionHeading>
        <p class="note">{l.t('The base game relics Alchemist runs end with most often, and how those runs went.')}</p>
        <Bars
          l={l}
          items={m.baseRelics}
          max={1}
          limit={12}
          reference={m.middle}
          referenceLabel={middle}
          labelWidth="13rem"
        />
      </section>
    </div>
  );
}
