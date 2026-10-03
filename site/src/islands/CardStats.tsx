import Bars from '../components/charts/Bars';
import DataTable, { type ColumnDef } from '../components/charts/DataTable';
import Scatter from '../components/charts/Scatter';
import Stats from '../components/charts/Stats';
import SectionHeading from '../components/SectionHeading';
import type { Lang, LangInit } from '../lib/lang';
import { cardHref } from '../lib/links';
import { POOL_RARITIES } from '../lib/mod';
import { cardStats, type CardStatsModel, type CardTableRow, RARITY_FILL, rarityName } from '../lib/views/cards';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';

const columns = (l: Lang): ColumnDef<CardTableRow>[] => [
  {
    key: 'name',
    label: l.t('Card'),
    render: (r) => (
      <>
        <a data-sheet-link href={l.href(cardHref(r.id))}>
          {r.name}
        </a>
        <small>{[r.rarity, ...r.tags].join(', ')}</small>
      </>
    ),
  },
  {
    key: 'winrate',
    label: l.t('Win rate'),
    num: true,
    render: (r) => (
      <>
        {l.pct(r.winrate)}
        {r.rank && <small>{r.rank}</small>}
      </>
    ),
  },
  { key: 'vsPeers', label: l.t('Vs middle card'), num: true, render: (r) => l.change(r.vsPeers) },
  { key: 'range', label: l.t('Likely range'), num: true, sortable: false, wide: true },
  {
    key: 'pickrate',
    label: l.t('Pick rate'),
    num: true,
    render: (r) => (
      <>
        {l.pct(r.pickrate)}
        <small>{r.offered ? l.t('of {count}', { count: l.num(r.offered) }) : l.t('not offered')}</small>
      </>
    ),
  },
  {
    key: 'playsPerRun',
    label: l.t('Plays'),
    num: true,
    render: (r) =>
      r.playsPerRun == null ? (
        '–'
      ) : (
        <>
          {l.fixed(r.playsPerRun, 1)}
          <small>{l.t('{share} unplayed', { share: l.pct(r.unplayed) })}</small>
        </>
      ),
    wide: true,
  },
  { key: 'held', label: l.t('Runs'), num: true, render: (r) => l.num(r.held), wide: true },
];

interface Props {
  initial: CardStatsModel;
  locale: LangInit;
  options: FilterOptions;
}

export default function CardStats({ initial, locale, options }: Props) {
  const l = useLang(locale);
  const {
    filters,
    model: m,
    status,
    update,
  } = useStats(initial, ['cards'], options, (runs, f) => cardStats(l, runs, f), locale);
  const peers = { max: 1, referenceLabel: l.t('The gold line is the middle card of the same rarity') };
  const since = { since: m.countedSince };
  const noPlays = l.t('No runs in these filters count card plays yet. Counted {since}.', since);
  return (
    <div aria-busy={status === 'loading'} class="stats-page">
      <Filters filters={filters} l={l} onChange={update} options={options} runs={m.runs} status={status} />
      <Stats items={m.stats} />
      <div class="stats-grid">
        <section class="panel p-5">
          <SectionHeading id="beating-their-peers">{l.t('Beating their peers')}</SectionHeading>
          <p class="note">{l.t('Cards that win more often than most cards of the same rarity.')}</p>
          <Bars items={m.up} l={l} {...peers} />
        </section>
        <section class="panel p-5">
          <SectionHeading id="trailing-their-peers">{l.t('Trailing their peers')}</SectionHeading>
          <p class="note">{l.t('Cards that win less often than most cards of the same rarity.')}</p>
          <Bars items={m.down} l={l} {...peers} />
        </section>
      </div>
      <section class="panel p-5">
        <SectionHeading id="pick-rate-and-win-rate">{l.t('Pick rate and win rate')}</SectionHeading>
        <p class="note">
          {l.t(
            'Each dot is a card. Further right means players take it more often when it is offered. Higher means the runs that finished with it won more often. The gold line is the middle card, at {rate}. The table below has every number.',
            { rate: l.pct(m.middle) },
          )}
        </p>
        <Scatter
          l={l}
          label={l.t('Pick rate against win rate for each card')}
          points={m.points}
          reference={m.middle}
          xLabel={l.t('Pick rate when offered')}
        />
        <ul class="legend">
          {POOL_RARITIES.map((rarity) => (
            <li>
              <i class="dot" style={{ '--dot': RARITY_FILL[rarity] }} />
              {rarityName(l, rarity)}
            </li>
          ))}
        </ul>
      </section>
      <div class="stats-grid">
        <section class="panel p-5">
          <SectionHeading id="left-unplayed">{l.t('Left unplayed')}</SectionHeading>
          <p class="note">
            {l.t('The share of runs that finished with the card but never played it. Counted {since}.', since)}
          </p>
          <Bars empty={noPlays} items={m.unplayed} l={l} limit={10} />
        </section>
        <section class="panel p-5">
          <SectionHeading id="played-the-most">{l.t('Played the most')}</SectionHeading>
          <p class="note">
            {l.t(
              'How many times a run that finished with the card played it, on average. Starting cards are left out. Counted {since}.',
              since,
            )}
          </p>
          <Bars empty={noPlays} items={m.mostPlayed} l={l} limit={10} />
        </section>
        <section class="panel p-5">
          <SectionHeading id="early-picks">{l.t('Early picks')}</SectionHeading>
          <p class="note">{l.t("Cards taken from a run's first three card rewards, and how those runs went.")}</p>
          <Bars
            items={m.early}
            l={l}
            limit={10}
            max={1}
            reference={m.overall}
            referenceLabel={l.t('The gold line is all runs together, at {rate}', { rate: l.pct(m.overall) })}
          />
        </section>
        <section class="panel p-5">
          <SectionHeading id="upgraded-at-rest-sites">{l.t('Upgraded at rest sites')}</SectionHeading>
          <p class="note">{l.t('Rest site upgrades for every 100 runs that finished with the card.')}</p>
          <Bars items={m.upgrades} l={l} limit={10} />
        </section>
      </div>
      <section class="panel p-5">
        <SectionHeading id="every-card">{l.t('Every card')}</SectionHeading>
        <p class="note">
          {l.n(
            filters.min,
            'Cards with at least {n} run in these filters, ranked against the middle card of their rarity (the gap is in percentage points). Select a column heading to sort by it.',
            'Cards with at least {n} runs in these filters, ranked against the middle card of their rarity (the gap is in percentage points). Select a column heading to sort by it.',
          )}
        </p>
        <DataTable
          caption={l.t('Win rate, pick rate and plays for every card')}
          columns={columns(l)}
          empty={l.t('No card in these filters has enough runs yet.')}
          l={l}
          limit={25}
          rows={m.table}
          sort={{ dir: -1, key: 'vsPeers' }}
        />
      </section>
    </div>
  );
}
