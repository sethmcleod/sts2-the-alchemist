import Bars from '../components/charts/Bars';
import Columns from '../components/charts/Columns';
import StackBar from '../components/charts/StackBar';
import Stats from '../components/charts/Stats';
import type { Lang, LangInit } from '../lib/lang';
import { mechanics, type Histogram, type Icons, type MechanicsModel } from '../lib/views/mechanics';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';

function HistogramChart({ l, chart, label, empty }: { l: Lang; chart: Histogram; label: string; empty?: string }) {
  return (
    <>
      <Columns l={l} items={chart.columns} markers={chart.markers} label={label} empty={empty} />
      {chart.foot && <p class="foot">{chart.foot}</p>}
    </>
  );
}

interface Props {
  locale: LangInit;
  initial: MechanicsModel;
  options: FilterOptions;
  icons: Icons;
}

export default function Mechanics({ locale, initial, options, icons }: Props) {
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
    <div class="stats-page" aria-busy={status === 'loading'}>
      <Filters l={l} filters={filters} options={options} runs={m.runs} status={status} onChange={update} />

      <h2 class="section-title">{l.t('Badges')}</h2>
      <p class="note">{l.t('How often runs earn each Alchemist badge, and at which tier.')}</p>
      <div class="stats-grid">
        {m.badges.map((badge) => (
          <section class="panel p-5">
            <h3 class="flex items-center gap-2 text-lg">
              {badge.icon && <img src={badge.icon} alt="" class="size-8" />}
              {badge.name}
            </h3>
            <p class="note">{badge.text}</p>
            <Bars l={l} items={badge.tiers} max={1} labelWidth="9rem" />
          </section>
        ))}
      </div>

      <h2 class="section-title">{l.t('Brew and potions')}</h2>
      <Stats items={m.brew.stats} />
      <section class="panel p-5">
        <h3>{l.t('Brew picks')}</h3>
        <p class="note">{m.brew.note}</p>
        <Bars
          l={l}
          items={m.brew.picks}
          max={1}
          reference={m.brew.even}
          referenceLabel={l.t('The gold line is an even share, {rate}', { rate: l.pct(m.brew.even) })}
          labelWidth="11rem"
        />
      </section>

      <h2 class="section-title">{l.t('Mixes')}</h2>
      <Stats items={m.mixes.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('Which Mixes get made')}</h3>
          <p class="note">{l.t("Each kind's share of every Mix created.")}</p>
          <Bars l={l} items={m.mixes.made} labelWidth="8rem" />
        </section>
        <section class="panel p-5">
          <h3>{l.t('How many get played')}</h3>
          <p class="note">
            {l.t('The share of each kind that got played. Why Mixes go unplayed, below, shows the rest.')}
          </p>
          <Bars l={l} items={m.mixes.played} max={1} labelWidth="8rem" />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Mixes played per fight')}</h3>
          <p class="note">{l.t('Fights by how many Mixes were played in them. A Compound Mix counts as 2.')}</p>
          <Columns l={l} items={m.mixes.fights} label={l.t('Fights by the number of Mixes played in them')} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Where Mixes come from')}</h3>
          <p class="note">{l.t('The card, relic, potion or power that created each Mix.')}</p>
          <Bars l={l} items={m.mixes.sources} limit={10} labelWidth="11rem" />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Why Mixes go unplayed')}</h3>
          <p class="note">{m.mixes.lostNote}</p>
          <Bars
            l={l}
            items={m.mixes.lost}
            max={1}
            labelWidth="14rem"
            empty={l.t('No runs in these filters count unplayed Mixes yet. Counted {since}.', since)}
          />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Compound pairings')}</h3>
          <p class="note">{l.t('The two Mixes folded into each Compound Mix.')}</p>
          <Bars l={l} items={m.mixes.pairs} limit={8} labelWidth="12rem" />
        </section>
      </div>

      <h2 class="section-title">{l.t('Ferment')}</h2>
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
            l={l}
            chart={m.ferment.turns}
            label={l.t('Runs by the turns their Ferment cards fermented')}
          />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Which Ferment cards age')}</h3>
          <p class="note">
            {l.t("The turns Fermented when a card is played, on average, for the Ferment cards in a run's final deck.")}
          </p>
          <Bars
            l={l}
            items={m.ferment.cards}
            limit={10}
            labelWidth="11rem"
            empty={l.t('No runs in these filters count Ferment turns per card yet. Counted {since}.', since)}
          />
        </section>
      </div>

      <h2 class="section-title">{l.game?.words.Poison ?? l.t('Poison')}</h2>
      <Stats items={m.poison.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <h3>{l.t('How high self-Poison gets')}</h3>
          <p class="note">{l.t('The most Poison the Alchemist held at once in each run.')}</p>
          <HistogramChart
            l={l}
            chart={m.poison.peak}
            label={l.t('Runs by their self-Poison peak')}
            empty={l.t('No runs in these filters count the self-Poison peak yet. Counted {since}.', since)}
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

      <h2 class="section-title">{l.t('Antitoxin')}</h2>
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
          <HistogramChart l={l} chart={m.antitoxin.peak} label={l.t('Runs by their Antitoxin peak')} />
        </section>
        <section class="panel p-5">
          <h3>{l.t('Where Antitoxin comes from')}</h3>
          <p class="note">{l.t('The card, relic, potion or power behind each point of Antitoxin gained.')}</p>
          <Bars
            l={l}
            items={m.antitoxin.sources}
            limit={10}
            labelWidth="11rem"
            empty={l.t('No runs in these filters count Antitoxin sources yet. Counted {since}.', since)}
          />
          {m.antitoxin.decayFoot && <p class="foot">{m.antitoxin.decayFoot}</p>}
        </section>
      </div>
    </div>
  );
}
