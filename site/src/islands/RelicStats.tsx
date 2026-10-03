import Bars from '../components/charts/Bars';
import SectionHeading from '../components/SectionHeading';
import type { LangInit } from '../lib/lang';
import type { Icons } from '../lib/views/mechanics';
import { relicStats, type RelicStatsModel } from '../lib/views/relics';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';

interface Props {
  icons: Icons;
  initial: RelicStatsModel;
  locale: LangInit;
  options: FilterOptions;
}

export default function RelicStats({ icons, initial, locale, options }: Props) {
  const l = useLang(locale);
  const { filters, model, status, update } = useStats(
    initial,
    ['relics', 'potions'],
    options,
    (runs, f) => relicStats(l, runs, f, icons),
    locale,
  );
  const middleLabel = l.t('The gold line is the middle relic, at {rate}', { rate: l.pct(model.middle) });
  return (
    <div aria-busy={status === 'loading'} class="stats-page">
      <Filters filters={filters} l={l} onChange={update} options={options} runs={model.runs} status={status} />
      <section class="panel p-5">
        <SectionHeading id="alchemist-relics">{l.t('Alchemist relics')}</SectionHeading>
        <p class="note">{l.t('How often the runs that ended with each relic won.')}</p>
        <Bars
          items={model.modRelics}
          l={l}
          labelWidth="13rem"
          max={1}
          reference={model.middle}
          referenceLabel={middleLabel}
        />
      </section>
      <div class="stats-grid">
        <section class="panel p-5">
          <SectionHeading id="ancient-choices">{l.t('Ancient choices')}</SectionHeading>
          <p class="note">{l.t('How often players take each relic an Ancient offers.')}</p>
          <Bars
            items={model.ancients}
            l={l}
            labelWidth="11rem"
            limit={10}
            max={1}
            reference={model.evenShare}
            referenceLabel={l.t('The gold line is an even share, {rate}', { rate: l.pct(model.evenShare) })}
          />
        </section>
        <section class="panel p-5">
          <SectionHeading id="potions">{l.t('Potions')}</SectionHeading>
          <p class="note">
            {l.t("Potions drunk for every 100 runs. The Alchemist's own potions link to their pages.")}
          </p>
          <Bars items={model.potions} l={l} labelWidth="11rem" limit={12} />
        </section>
      </div>
      <section class="panel p-5">
        <SectionHeading id="base-game-relics">{l.t('Base game relics')}</SectionHeading>
        <p class="note">{l.t('The base game relics Alchemist runs end with most often, and how those runs went.')}</p>
        <Bars
          items={model.baseRelics}
          l={l}
          labelWidth="13rem"
          limit={12}
          max={1}
          reference={model.middle}
          referenceLabel={middleLabel}
        />
      </section>
    </div>
  );
}
