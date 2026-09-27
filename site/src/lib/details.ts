// The numbers on a card, relic, potion or power page, under the default filters

import type { Stat } from '../components/charts/Stats';
import { breakdownBars } from './bars';
import type { Lang } from './lang';
import { inPool, mixKind, tallyLabel, type PoolRarity } from './mod';
import { byPrefix, cardRows, DEFAULT_FILTERS, mixRows, type Runs } from './runs';
import { rate, sumBy, wilson } from './stats';

const f = DEFAULT_FILTERS;

// The rarity's word can change with the words around it, so each rarity has its own key
const AMONG: Record<PoolRarity, string> = {
  Common: 'Among Common cards',
  Uncommon: 'Among Uncommon cards',
  Rare: 'Among Rare cards',
};

/** "12 runs, likely 40% to 55%" */
const winNote = (l: Lang, wins: number, runs: number) =>
  l.n(runs, '{n} run, likely {range}', '{n} runs, likely {range}', { range: l.range(wilson(wins, runs)) });

export function cardDetail(l: Lang, runs: Runs, id: string) {
  const on = runs.select(f);
  const info = runs.summary.card_info[id];
  const r = cardRows(runs, on, f.min).get(id);
  const mix = mixKind(id, info) ? mixRows(runs, on).get(id) : undefined;
  let stats: Stat[] = [];

  if (mix) {
    stats = [
      { label: l.t('Made per run'), value: l.fixed(mix.perRun, 1), note: l.t('by cards, relics, potions and powers') },
      { label: l.t('Played'), value: l.pct(mix.played), note: l.t('of the ones made') },
      { label: l.t('Share of Mixes'), value: l.pct(mix.share), note: l.t('of every Mix made') },
    ];
  } else if (r && r.held > 0) {
    stats = [
      { label: l.t('Win rate'), value: l.pct(r.winrate), note: winNote(l, r.held_wins, r.held) },
      r.rank && inPool(r.rarity)
        ? {
            label: l.t(AMONG[r.rarity]),
            value: l.t('{rank} of {count}', { rank: l.ordinal(r.rank), count: l.num(r.ranked) }),
            note: l.t('by win rate. The middle one wins {rate}', { rate: l.pct(r.peer) }),
          }
        : { label: l.t('In final decks'), value: l.pct(r.deckrate), note: l.t('of runs') },
      {
        label: l.t('Pick rate'),
        value: l.pct(r.pickrate),
        note: r.offered
          ? l.n(r.offered, 'picked {picked} of {n} time offered', 'picked {picked} of {n} times offered', {
              picked: l.num(r.picked),
            })
          : l.t('never offered as a card reward'),
      },
      {
        label: l.t('As an early pick'),
        value: l.pct(rate(r.early_pick_wins, r.early_picks)),
        note: r.early_picks
          ? l.n(r.early_picks, 'win rate over {n} early pick', 'win rate over {n} early picks')
          : l.t('never an early pick'),
      },
      {
        label: l.t('Plays per run'),
        value: l.fixed(r.playsPerRun, 1),
        note: r.held_with_plays
          ? l.n(r.held_with_plays, 'never played in {share} of {n} run', 'never played in {share} of {n} runs', {
              share: l.pct(r.unplayed),
            })
          : l.t('counted {since}', { since: runs.countedSince(l) }),
      },
      {
        label: l.t('Two or more copies'),
        value: l.pct(rate(r.held_twice_wins, r.held_twice)),
        note: r.held_twice
          ? l.n(r.held_twice, 'win rate over {n} run', 'win rate over {n} runs')
          : l.t('no run held two copies'),
      },
    ];
  }

  const cards = runs.table('cards');
  const breakdown = r?.held
    ? breakdownBars(
        l,
        runs.breakdown(cards, f, (row) => row.card === id, 'held_wins', 'held'),
      )
    : null;
  return { stats, breakdown, runs: runs.totals(on).runs };
}

export function relicDetail(l: Lang, runs: Runs, id: string) {
  const on = runs.select(f);
  const info = runs.summary.relic_info[id];
  const relics = runs.table('relics');
  const g = sumBy(relics, on, (r) => r.relic as string).get(id);
  const t = runs.totals(on);
  const held = (g?.held ?? 0) > 0;
  const stats: Stat[] = held
    ? [
        { label: l.t('Win rate'), value: l.pct(g!.held_wins / g!.held), note: winNote(l, g!.held_wins, g!.held) },
        { label: l.t('In runs'), value: l.pct(rate(g!.held, t.runs)), note: l.t('of runs ended with it') },
        ...(info.rarity === 'Starter'
          ? []
          : [
              {
                label: l.t('Bought'),
                value: l.num(g!.bought),
                note: l.n(g!.bought, 'time from the Merchant', 'times from the Merchant'),
              },
            ]),
      ]
    : [];
  const breakdown = held
    ? breakdownBars(
        l,
        runs.breakdown(relics, f, (row) => row.relic === id, 'held_wins', 'held'),
      )
    : null;
  return { stats, breakdown, runs: t.runs };
}

export function potionDetail(l: Lang, runs: Runs, id: string) {
  const on = runs.select(f);
  const info = runs.summary.potion_info[id];
  const g = sumBy(runs.table('potions'), on, (r) => r.potion as string).get(id) ?? {
    drunk: 0,
    bought: 0,
    discarded: 0,
  };
  const t = runs.totals(on);
  const label = tallyLabel(id);
  const all = runs.counters(on);
  const offered = all.get(`brew_offer:${label}`)?.count || 0;
  const drunk = (100 * g.drunk) / (t.runs_with_drinks || 1);
  const stats: Stat[] = [
    ...(info.rarity === 'Event'
      ? [
          {
            label: l.t('Picked at Brew'),
            value: l.pct(rate(all.get(`brew_pick:${label}`)?.count || 0, offered)),
            note: offered
              ? l.n(offered, 'of {n} time it was offered', 'of {n} times it was offered')
              : l.t('not offered yet'),
          },
        ]
      : []),
    {
      label: l.t('Drunk'),
      value: l.fixed(drunk, 0),
      note: l.n(Math.round(drunk), 'time per 100 runs', 'times per 100 runs'),
    },
    { label: l.t('Bought'), value: l.num(g.bought), note: l.t('from the Merchant') },
    { label: l.t('Thrown away'), value: l.num(g.discarded), note: l.t('to make room') },
  ];
  return { stats, breakdown: null, runs: t.runs };
}

export function powerDetail(l: Lang, runs: Runs, id: string) {
  const on = runs.select(f);
  const all = runs.counters(on);
  const label = tallyLabel(id);
  const [antitoxin, mixes] = [byPrefix(all, 'atxsrc:').get(label)?.count, byPrefix(all, 'mixsrc:').get(label)?.count];
  const stats: Stat[] = [
    ...(antitoxin ? [{ label: l.t('Antitoxin gained'), value: l.num(antitoxin), note: l.t('from this power') }] : []),
    ...(mixes ? [{ label: l.t('Mixes made'), value: l.num(mixes), note: l.t('by this power') }] : []),
  ];
  return { stats, breakdown: null, runs: runs.totals(on).runs };
}
