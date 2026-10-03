import Bars from '../components/charts/Bars';
import Columns from '../components/charts/Columns';
import StackBar from '../components/charts/StackBar';
import Stats from '../components/charts/Stats';
import SectionHeading from '../components/SectionHeading';
import type { Lang, LangInit } from '../lib/lang';
import { type Histogram, type Icons, mechanics, type MechanicsModel } from '../lib/views/mechanics';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';

function HistogramChart({ chart, empty, l, label }: { chart: Histogram; empty?: string; l: Lang; label: string }) {
  return (
    <>
      <Columns empty={empty} items={chart.columns} l={l} label={label} markers={chart.markers} />
      {chart.footnote && <p class="foot">{chart.footnote}</p>}
    </>
  );
}

interface Props {
  icons: Icons;
  initial: MechanicsModel;
  locale: LangInit;
  options: FilterOptions;
}

export default function Mechanics({ icons, initial, locale, options }: Props) {
  const l = useLang(locale);
  const { filters, model, status, update } = useStats(
    initial,
    ['cards'],
    options,
    (runs, f) => mechanics(l, runs, f, icons),
    locale,
  );
  const since = { since: model.countedSince };
  const markedTiers = (chart: Histogram) => ({ tiers: l.list(chart.markers.map((marker) => marker.label)) });
  return (
    <div aria-busy={status === 'loading'} class="stats-page">
      <Filters filters={filters} l={l} onChange={update} options={options} runs={model.runs} status={status} />

      <SectionHeading class="section-title" id="badges">
        {l.t('Badges')}
      </SectionHeading>
      <p class="note">{l.t('How often runs earn each Alchemist badge, and at which tier.')}</p>
      <div class="stats-grid">
        {model.badges.map((badge) => (
          <section class="panel p-5">
            <h3 class="flex items-center gap-2 text-lg">
              {badge.icon && <img alt="" class="size-8" src={badge.icon} />}
              {badge.name}
            </h3>
            <p class="note">{badge.text}</p>
            <Bars items={badge.tiers} l={l} labelWidth="9rem" max={1} />
          </section>
        ))}
      </div>

      <SectionHeading class="section-title" id="brew-and-potions">
        {l.t('Brew and potions')}
      </SectionHeading>
      <Stats items={model.brew.stats} />
      <section class="panel p-5">
        <h3>{l.t('Brew picks')}</h3>
        <p class="note">{model.brew.note}</p>
        <Bars
          items={model.brew.picks}
          l={l}
          labelWidth="11rem"
          max={1}
          reference={model.brew.evenShare}
          referenceLabel={l.t('The gold line is an even share, {rate}', { rate: l.pct(model.brew.evenShare) })}
        />
      </section>

      <SectionHeading class="section-title" id="mixes">
        {l.t('Mixes')}
      </SectionHeading>
      <Stats items={model.mixes.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('Which Mixes get made')}</h3>
          <p class="note">{l.t("Each kind's share of every Mix created.")}</p>
          <Bars items={model.mixes.made} l={l} labelWidth="8rem" />
        </section>
        <section class="panel p-5">
          <h3>{l.t('How many get played')}</h3>
          <p class="note">
            {l.t('The share of each kind that got played. Why Mixes go unplayed, below, shows the rest.')}
          </p>
          <Bars items={model.mixes.played} l={l} labelWidth="8rem" max={1} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Mixes played per fight')}</h3>
          <p class="note">{l.t('Fights by how many Mixes were played in them. A Compound Mix counts as 2.')}</p>
          <Columns items={model.mixes.fights} l={l} label={l.t('Fights by the number of Mixes played in them')} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Where Mixes come from')}</h3>
          <p class="note">{l.t('The card, relic, potion or power that created each Mix.')}</p>
          <Bars items={model.mixes.sources} l={l} labelWidth="11rem" limit={10} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Why Mixes go unplayed')}</h3>
          <p class="note">{model.mixes.lostNote}</p>
          <Bars
            empty={l.t('No runs in these filters count unplayed Mixes yet. Counted {since}.', since)}
            items={model.mixes.lost}
            l={l}
            labelWidth="14rem"
            max={1}
          />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Compound pairings')}</h3>
          <p class="note">{l.t('The two Mixes folded into each Compound Mix.')}</p>
          <Bars items={model.mixes.pairs} l={l} labelWidth="12rem" limit={8} />
        </section>
      </div>

      <SectionHeading class="section-title" id="ferment">
        {l.t('Ferment')}
      </SectionHeading>
      <Stats items={model.ferment.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('Turns fermented per run')}</h3>
          <p class="note">
            {l.t(
              'The turns Fermented on every Ferment card a run played, added up. The lines mark where the {tiers} badges start.',
              markedTiers(model.ferment.turns),
            )}
          </p>
          <HistogramChart
            chart={model.ferment.turns}
            l={l}
            label={l.t('Runs by the turns their Ferment cards fermented')}
          />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Which Ferment cards age')}</h3>
          <p class="note">
            {l.t("The turns Fermented when a card is played, on average, for the Ferment cards in a run's final deck.")}
          </p>
          <Bars
            empty={l.t('No runs in these filters count Ferment turns per card yet. Counted {since}.', since)}
            items={model.ferment.cards}
            l={l}
            labelWidth="11rem"
            limit={10}
          />
        </section>
      </div>

      <SectionHeading class="section-title" id="poison">
        {l.game?.words.Poison ?? l.t('Poison')}
      </SectionHeading>
      <Stats items={model.poison.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('How high self-Poison gets')}</h3>
          <p class="note">{l.t('The most Poison the Alchemist held at once in each run.')}</p>
          <HistogramChart
            chart={model.poison.peak}
            empty={l.t('No runs in these filters count the self-Poison peak yet. Counted {since}.', since)}
            l={l}
            label={l.t('Runs by their self-Poison peak')}
          />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Where self-Poison goes')}</h3>
          <p class="note">
            {l.t('Poison damage the Alchemist took, split by what Antitoxin absorbed and what hit HP.')}
          </p>
          <StackBar l={l} parts={model.poison.split} />
          {model.poison.tickFootnote && <p class="foot">{model.poison.tickFootnote}</p>}
        </section>
      </div>

      <SectionHeading class="section-title" id="antitoxin">
        {l.t('Antitoxin')}
      </SectionHeading>
      <Stats items={model.antitoxin.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('How high Antitoxin gets')}</h3>
          <p class="note">
            {l.t(
              'The most Antitoxin held at once in each run. The lines mark where the {tiers} badges start.',
              markedTiers(model.antitoxin.peak),
            )}
          </p>
          <HistogramChart chart={model.antitoxin.peak} l={l} label={l.t('Runs by their Antitoxin peak')} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Where Antitoxin comes from')}</h3>
          <p class="note">{l.t('The card, relic, potion or power behind each point of Antitoxin gained.')}</p>
          <Bars
            empty={l.t('No runs in these filters count Antitoxin sources yet. Counted {since}.', since)}
            items={model.antitoxin.sources}
            l={l}
            labelWidth="11rem"
            limit={10}
          />
          {model.antitoxin.decayFootnote && <p class="foot">{model.antitoxin.decayFootnote}</p>}
        </section>
      </div>
    </div>
  );
}
