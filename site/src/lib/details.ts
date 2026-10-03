import type { Stat } from '../components/charts/Stats';
import { breakdownBars } from './bars';
import type { Lang } from './lang';
import { inPool, mixKind, type PoolRarity, tallyLabel } from './mod';
import { byPrefix, cardRows, DEFAULT_FILTERS, mixRows, type Runs } from './runs';
import { rate, sumBy, wilson } from './stats';

const filters = DEFAULT_FILTERS;

const AMONG: Record<PoolRarity, string> = {
  Common: 'Among Common cards',
  Rare: 'Among Rare cards',
  Uncommon: 'Among Uncommon cards',
};

const winNote = (l: Lang, wins: number, runs: number) =>
  l.n(runs, '{n} run, likely {range}', '{n} runs, likely {range}', { range: l.range(wilson(wins, runs)) });

export function cardDetail(l: Lang, runs: Runs, id: string) {
  const on = runs.select(filters);
  const info = runs.summary.card_info[id];
  const row = cardRows(runs, on, filters.min).get(id);
  const mix = mixKind(id, info) ? mixRows(runs, on).get(id) : undefined;
  let stats: Stat[] = [];

  if (mix) {
    stats = [
      { label: l.t('Made per run'), note: l.t('by cards, relics, potions and powers'), value: l.fixed(mix.perRun, 1) },
      { label: l.t('Played'), note: l.t('of the ones made'), value: l.pct(mix.played) },
      { label: l.t('Share of Mixes'), note: l.t('of every Mix made'), value: l.pct(mix.share) },
    ];
  } else if (row && row.held > 0) {
    stats = [
      { label: l.t('Win rate'), note: winNote(l, row.held_wins, row.held), value: l.pct(row.winrate) },
      row.rank && inPool(row.rarity)
        ? {
            label: l.t(AMONG[row.rarity]),
            note: l.t('by win rate. The middle one wins {rate}', { rate: l.pct(row.peer) }),
            value: l.t('{rank} of {count}', { count: l.num(row.ranked), rank: l.ordinal(row.rank) }),
          }
        : { label: l.t('In final decks'), note: l.t('of runs'), value: l.pct(row.deckrate) },
      {
        label: l.t('Pick rate'),
        note: row.offered
          ? l.n(row.offered, 'picked {picked} of {n} time offered', 'picked {picked} of {n} times offered', {
              picked: l.num(row.picked),
            })
          : l.t('never offered as a card reward'),
        value: l.pct(row.pickrate),
      },
      {
        label: l.t('As an early pick'),
        note: row.early_picks
          ? l.n(row.early_picks, 'win rate over {n} early pick', 'win rate over {n} early picks')
          : l.t('never an early pick'),
        value: l.pct(rate(row.early_pick_wins, row.early_picks)),
      },
      {
        label: l.t('Plays per run'),
        note: row.held_with_plays
          ? l.n(row.held_with_plays, 'never played in {share} of {n} run', 'never played in {share} of {n} runs', {
              share: l.pct(row.unplayed),
            })
          : l.t('counted {since}', { since: runs.countedSince(l) }),
        value: l.fixed(row.playsPerRun, 1),
      },
      {
        label: l.t('Two or more copies'),
        note: row.held_twice
          ? l.n(row.held_twice, 'win rate over {n} run', 'win rate over {n} runs')
          : l.t('no run held two copies'),
        value: l.pct(rate(row.held_twice_wins, row.held_twice)),
      },
    ];
  }

  const cards = runs.table('cards');
  const breakdown = row?.held
    ? breakdownBars(
        l,
        runs.breakdown(cards, filters, (entry) => entry.card === id, 'held_wins', 'held'),
      )
    : null;
  return { breakdown, runs: runs.totals(on).runs, stats };
}

export function relicDetail(l: Lang, runs: Runs, id: string) {
  const on = runs.select(filters);
  const info = runs.summary.relic_info[id];
  const relics = runs.table('relics');
  const counts = sumBy(relics, on, (row) => row.relic as string).get(id);
  const totals = runs.totals(on);
  const held = (counts?.held ?? 0) > 0;
  const stats: Stat[] = held
    ? [
        {
          label: l.t('Win rate'),
          note: winNote(l, counts!.held_wins, counts!.held),
          value: l.pct(counts!.held_wins / counts!.held),
        },
        { label: l.t('In runs'), note: l.t('of runs ended with it'), value: l.pct(rate(counts!.held, totals.runs)) },
        ...(info.rarity === 'Starter'
          ? []
          : [
              {
                label: l.t('Bought'),
                note: l.n(counts!.bought, 'time from the Merchant', 'times from the Merchant'),
                value: l.num(counts!.bought),
              },
            ]),
      ]
    : [];
  const breakdown = held
    ? breakdownBars(
        l,
        runs.breakdown(relics, filters, (row) => row.relic === id, 'held_wins', 'held'),
      )
    : null;
  return { breakdown, runs: totals.runs, stats };
}

export function potionDetail(l: Lang, runs: Runs, id: string) {
  const on = runs.select(filters);
  const info = runs.summary.potion_info[id];
  const counts = sumBy(runs.table('potions'), on, (row) => row.potion as string).get(id) ?? {
    bought: 0,
    discarded: 0,
    drunk: 0,
  };
  const totals = runs.totals(on);
  const label = tallyLabel(id);
  const counters = runs.counters(on);
  const offered = counters.get(`brew_offer:${label}`)?.count || 0;
  const drunkPer100Runs = (100 * counts.drunk) / (totals.runs_with_drinks || 1);
  const stats: Stat[] = [
    ...(info.rarity === 'Event'
      ? [
          {
            label: l.t('Picked at Brew'),
            note: offered
              ? l.n(offered, 'of {n} time it was offered', 'of {n} times it was offered')
              : l.t('not offered yet'),
            value: l.pct(rate(counters.get(`brew_pick:${label}`)?.count || 0, offered)),
          },
        ]
      : []),
    {
      label: l.t('Drunk'),
      note: l.n(Math.round(drunkPer100Runs), 'time per 100 runs', 'times per 100 runs'),
      value: l.fixed(drunkPer100Runs, 0),
    },
    { label: l.t('Bought'), note: l.t('from the Merchant'), value: l.num(counts.bought) },
    { label: l.t('Thrown away'), note: l.t('to make room'), value: l.num(counts.discarded) },
  ];
  return { breakdown: null, runs: totals.runs, stats };
}

export function powerDetail(l: Lang, runs: Runs, id: string) {
  const on = runs.select(filters);
  const counters = runs.counters(on);
  const label = tallyLabel(id);
  const [antitoxin, mixes] = [
    byPrefix(counters, 'atxsrc:').get(label)?.count,
    byPrefix(counters, 'mixsrc:').get(label)?.count,
  ];
  const stats: Stat[] = [
    ...(antitoxin ? [{ label: l.t('Antitoxin gained'), note: l.t('from this power'), value: l.num(antitoxin) }] : []),
    ...(mixes ? [{ label: l.t('Mixes made'), note: l.t('by this power'), value: l.num(mixes) }] : []),
  ];
  return { breakdown: null, runs: runs.totals(on).runs, stats };
}
