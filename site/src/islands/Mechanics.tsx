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
      {chart.foot && <p class="foot">{chart.foot}</p>}
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
  const {
    filters,
    model: m,
    status,
    update,
  } = useStats(initial, ['cards'], options, (runs, f) => mechanics(l, runs, f, icons), locale);
  const since = { since: m.countedSince };
  // The badge tiers a histogram marks, by the names the mod gives them
  const tiers = (chart: Histogram) => ({ tiers: l.list(chart.markers.map((marker) => marker.label)) });
  return (
    <div aria-busy={status === 'loading'} class="stats-page">
      <Filters filters={filters} l={l} onChange={update} options={options} runs={m.runs} status={status} />

      <SectionHeading class="section-title" id="badges">
        {l.t('Badges')}
      </SectionHeading>
      <p class="note">{l.t('How often runs earn each Alchemist badge, and at which tier.')}</p>
      <div class="stats-grid">
        {m.badges.map((badge) => (
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
      <Stats items={m.brew.stats} />
      <section class="panel p-5">
        <h3>{l.t('Brew picks')}</h3>
        <p class="note">{m.brew.note}</p>
        <Bars
          items={m.brew.picks}
          l={l}
          labelWidth="11rem"
          max={1}
          reference={m.brew.even}
          referenceLabel={l.t('The gold line is an even share, {rate}', { rate: l.pct(m.brew.even) })}
        />
      </section>

      <SectionHeading class="section-title" id="mixes">
        {l.t('Mixes')}
      </SectionHeading>
      <Stats items={m.mixes.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('Which Mixes get made')}</h3>
          <p class="note">{l.t("Each kind's share of every Mix created.")}</p>
          <Bars items={m.mixes.made} l={l} labelWidth="8rem" />
        </section>
        <section class="panel p-5">
          <h3>{l.t('How many get played')}</h3>
          <p class="note">
            {l.t('The share of each kind that got played. Why Mixes go unplayed, below, shows the rest.')}
          </p>
          <Bars items={m.mixes.played} l={l} labelWidth="8rem" max={1} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Mixes played per fight')}</h3>
          <p class="note">{l.t('Fights by how many Mixes were played in them. A Compound Mix counts as 2.')}</p>
          <Columns items={m.mixes.fights} l={l} label={l.t('Fights by the number of Mixes played in them')} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Where Mixes come from')}</h3>
          <p class="note">{l.t('The card, relic, potion or power that created each Mix.')}</p>
          <Bars items={m.mixes.sources} l={l} labelWidth="11rem" limit={10} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Why Mixes go unplayed')}</h3>
          <p class="note">{m.mixes.lostNote}</p>
          <Bars
            empty={l.t('No runs in these filters count unplayed Mixes yet. Counted {since}.', since)}
            items={m.mixes.lost}
            l={l}
            labelWidth="14rem"
            max={1}
          />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Compound pairings')}</h3>
          <p class="note">{l.t('The two Mixes folded into each Compound Mix.')}</p>
          <Bars items={m.mixes.pairs} l={l} labelWidth="12rem" limit={8} />
        </section>
      </div>

      <SectionHeading class="section-title" id="ferment">
        {l.t('Ferment')}
      </SectionHeading>
      <Stats items={m.ferment.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('Turns fermented per run')}</h3>
          <p class="note">
            {l.t(
              'The turns Fermented on every Ferment card a run played, added up. The lines mark where the {tiers} badges start.',
              tiers(m.ferment.turns),
            )}
          </p>
          <HistogramChart
            chart={m.ferment.turns}
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
            items={m.ferment.cards}
            l={l}
            labelWidth="11rem"
            limit={10}
          />
        </section>
      </div>

      <SectionHeading class="section-title" id="poison">
        {l.game?.words.Poison ?? l.t('Poison')}
      </SectionHeading>
      <Stats items={m.poison.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('How high self-Poison gets')}</h3>
          <p class="note">{l.t('The most Poison the Alchemist held at once in each run.')}</p>
          <HistogramChart
            chart={m.poison.peak}
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
          <StackBar l={l} parts={m.poison.split} />
          {m.poison.tickFoot && <p class="foot">{m.poison.tickFoot}</p>}
        </section>
      </div>

      <SectionHeading class="section-title" id="antitoxin">
        {l.t('Antitoxin')}
      </SectionHeading>
      <Stats items={m.antitoxin.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('How high Antitoxin gets')}</h3>
          <p class="note">
            {l.t(
              'The most Antitoxin held at once in each run. The lines mark where the {tiers} badges start.',
              tiers(m.antitoxin.peak),
            )}
          </p>
          <HistogramChart chart={m.antitoxin.peak} l={l} label={l.t('Runs by their Antitoxin peak')} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Where Antitoxin comes from')}</h3>
          <p class="note">{l.t('The card, relic, potion or power behind each point of Antitoxin gained.')}</p>
          <Bars
            empty={l.t('No runs in these filters count Antitoxin sources yet. Counted {since}.', since)}
            items={m.antitoxin.sources}
            l={l}
            labelWidth="11rem"
            limit={10}
          />
          {m.antitoxin.decayFoot && <p class="foot">{m.antitoxin.decayFoot}</p>}
        </section>
      </div>
    </div>
  );
}
