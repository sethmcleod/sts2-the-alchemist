import type { BarItem } from '../../components/charts/Bars';
import type { Column } from '../../components/charts/Columns';
import type { Stat } from '../../components/charts/Stats';
import { rateItem } from '../bars';
import type { Lang } from '../lang';
import { cardHref } from '../links';
import { mixByKind, MIXES } from '../mod';
import { byPrefix, type Filters, type Runs, totalCount } from '../runs';
import { type Counts, rate, type Selection, sumBy } from '../stats';

const MIX_FIGHT_BUCKETS = ['0', '1', '2', '3', '4', '5-6', '7-9', '10+'];
const TIER_FILL = ['var(--color-bronze)', 'var(--color-silver)', 'var(--color-gold)'];

type BinRange = (l: Lang, from: string, to?: string) => string;
const TURN_RANGE: BinRange = (l, from, to) =>
  to ? l.t('{from} to {to} turns', { from, to }) : l.t('{from} or more turns', { from });
const POISON_RANGE: BinRange = (l, from, to) =>
  to ? l.t('{from} to {to} Poison', { from, to }) : l.t('{from} or more Poison', { from });
const ANTITOXIN_RANGE: BinRange = (l, from, to) =>
  to ? l.t('{from} to {to} Antitoxin', { from, to }) : l.t('{from} or more Antitoxin', { from });

export interface Histogram {
  columns: Column[];
  footnote: string;
  markers: { before: number; label: string }[];
}

export interface MechanicsModel {
  antitoxin: { decayFootnote: string; peak: Histogram; sources: BarItem[]; stats: Stat[] };
  badges: { icon: null | string; id: string; name: string; text: string; tiers: BarItem[] }[];
  brew: { evenShare: null | number; note: string; picks: BarItem[]; stats: Stat[] };
  countedSince: string;
  ferment: { cards: BarItem[]; stats: Stat[]; turns: Histogram };
  mixes: {
    fights: Column[];
    lost: BarItem[];
    lostNote: string;
    made: BarItem[];
    pairs: BarItem[];
    played: BarItem[];
    sources: BarItem[];
    stats: Stat[];
  };
  poison: {
    peak: Histogram;
    split: { fill: string; label: string; value: number }[];
    stats: Stat[];
    tickFootnote: string;
  };
  runs: number;
}

export type Icons = Record<string, string>;

export function mechanics(l: Lang, runs: Runs, filters: Filters, icons: Icons): MechanicsModel {
  const on = runs.select(filters);
  const totals = runs.totals(on);
  const counters = runs.counters(on);
  const count = (key: string) => counters.get(key)?.count || 0;
  const countedSince = runs.countedSince(l);

  const detailed = runs.withDetailedCounters(on);
  const detailedRuns = runs.totals(detailed).runs;
  const detailedTotals = runs.totals(detailed);
  const detailedCounters = runs.counters(detailed);
  const detailedCount = (key: string) => detailedCounters.get(key)?.count || 0;
  const soloDetailed: Selection = detailed.map((flag, i) => (flag && !runs.groups[i].coop ? 1 : 0));
  const soloDetailedRuns = runs.totals(soloDetailed).runs;
  const countedStat = (countedRuns: number, value: string, note: string) =>
    countedRuns ? { note, value } : { note: l.t('counted {since}', { since: countedSince }), value: '–' };

  const idOf = (label: string) => runs.meta.prefix + label.toUpperCase();
  const source = (label: string) => {
    if (label === 'unknown') return { icon: null, label: l.t('Other') };
    const id = idOf(label);
    return {
      href: runs.summary.card_info[id] ? cardHref(id) : undefined,
      icon: icons[id] ?? null,
      label: runs.name(id),
    };
  };
  const shares = (entries: [string, Counts][], labelOf: (label: string) => Partial<BarItem> = source) => {
    const total = entries.reduce((n, [, c]) => n + c.count, 0);
    return entries.map(([label, c]) => ({
      label,
      ...labelOf(label),
      text: l.pct(c.count / total),
      textNote: l.num(c.count),
      value: c.count / total,
    }));
  };
  const mixName = (kind: string) => {
    const mix = mixByKind(kind);
    return mix ? l.t(mix.name) : kind;
  };

  const histogram = (selection: Selection, metric: string, binRange: BinRange, badgeId?: string): Histogram => {
    const { bins, last, width } = runs.histogram(selection, metric);
    const total = bins.reduce((n, b) => n + b.runs, 0);
    const badge = badgeId ? runs.meta.badges.find((b) => b.id === badgeId) : undefined;
    let footnote = '';
    if (badge && total) {
      const { share } = runs.badgeShares(on, badge.id);
      const tiers = badge.tiers.map((tier, i) =>
        l.t('{share} {tier}', { share: l.pct(share(i + 1)), tier: tier.title }),
      );
      footnote = l.t('Runs that earned each tier or better: {tiers}.', { tiers: l.list(tiers) });
    }
    return {
      columns: bins.map((b) => ({
        label: b.bin === last ? l.t('{n}+', { n: l.num(b.bin) }) : l.num(b.bin),
        tip: [
          b.bin === last ? binRange(l, l.num(b.bin)) : binRange(l, l.num(b.bin), l.num(b.bin + width - 1)),
          l.n(b.runs, '{n} run ({share})', '{n} runs ({share})', { share: l.pct(b.runs / total) }),
        ],
        value: b.runs,
      })),
      footnote,
      markers: badge?.tiers.map((tier) => ({ before: tier.at / width, label: tier.title })) ?? [],
    };
  };

  const badges = runs.meta.badges.map((badge) => {
    const { eligible, share } = runs.badgeShares(on, badge.id);
    const firstTierText = { text: badge.tiers[0].text };
    return {
      icon: icons[badge.id] ?? null,
      id: badge.id,
      name: l.t(
        badge.id
          .slice(runs.meta.prefix.length)
          .replaceAll('_', ' ')
          .toLowerCase()
          .replace(/^\w/, (c) => c.toUpperCase()),
      ),
      text: badge.needs_win
        ? l.n(eligible, '{text} Counted over {n} win.', '{text} Counted over {n} wins.', firstTierText)
        : l.n(eligible, '{text} Counted over {n} run.', '{text} Counted over {n} runs.', firstTierText),
      tiers: eligible
        ? badge.tiers.map((tier, i) => ({
            fill: TIER_FILL[i],
            label: tier.title,
            note: badge.tiers.length > 1 ? l.t('{n}+', { n: l.num(tier.at) }) : null,
            text: l.pct(share(i + 1)),
            value: share(i + 1),
          }))
        : [],
    };
  });

  const offers = byPrefix(counters, 'brew_offer:');
  const picks = byPrefix(counters, 'brew_pick:');
  const evenShare = rate(totalCount(picks), totalCount(offers));

  const made = byPrefix(counters, 'mixmade:');
  const played = byPrefix(counters, 'mixplay:');
  const madeTotal = totalCount(made);
  const fightCounts = byPrefix(counters, 'mixfight:');
  const fightBins = MIX_FIGHT_BUCKETS.map((bucket) => ({ bucket, fights: fightCounts.get(bucket)?.count || 0 }));
  const fightTotal = fightBins.reduce((n, b) => n + b.fights, 0);
  let fightsSoFar = 0;
  const middleFight = fightTotal
    ? fightBins.find((b) => (fightsSoFar += b.fights) >= fightTotal / 2)?.bucket
    : undefined;
  const kinds = MIXES.map((mix) => mix.kind).filter((kind) => made.has(kind) || played.has(kind));
  const detailedMade = totalCount(byPrefix(detailedCounters, 'mixmade:'));
  const unplayed = Math.max(0, detailedMade - totalCount(byPrefix(detailedCounters, 'mixplay:')));
  const combined = detailedCount('mixlost:combined');
  const leftover = detailedCount('mixlost:leftover');

  const fermentPlays = count('ferment_plays');
  const cards = runs.tables.cards;
  const fermentedTier = runs.meta.badges.find((b) => b.id === 'ALCHEMIST-FERMENTED')?.tiers[0].title ?? '';

  const absorbed = totals.poison_absorbed;
  const bled = totals.poison_bled;
  const lostWithTally = totals.runs_with_tally - totals.wins_with_tally;
  const ticks = count('tick_covered') + count('tick_bled');
  const antitoxinSources = [...byPrefix(detailedCounters, 'atxsrc:')].sort((a, b) => b[1].count - a[1].count);
  const gained = antitoxinSources.reduce((n, [, c]) => n + c.count, 0);
  const decayed = detailedCount('atx_decayed');

  return {
    antitoxin: {
      decayFootnote: gained
        ? l.t(
            'Of the Antitoxin gained, {share} thinned away at 1 a turn. The rest was still up when each fight ended.',
            { share: l.pct(decayed / gained) },
          )
        : '',
      peak: histogram(on, 'antitoxin_peak', ANTITOXIN_RANGE, 'ALCHEMIST-ANTITOXIN_PEAK'),
      sources: shares(antitoxinSources),
      stats: [
        {
          label: l.t('Antitoxin peak'),
          note: l.t('the most held at once, on average'),
          value: l.fixed(rate(totals.antitoxin_peak, totals.runs_with_peak), 0),
        },
        {
          label: l.t('Gained per run'),
          ...countedStat(detailedRuns, l.fixed(rate(gained, detailedRuns), 0), l.t('from every source')),
        },
        {
          label: l.t('Lost to decay'),
          ...countedStat(detailedRuns, l.pct(rate(decayed, gained)), l.t('of the Antitoxin gained')),
        },
        { label: l.t('Absorbed'), note: l.t('of self-Poison damage'), value: l.pct(rate(absorbed, absorbed + bled)) },
      ],
    },
    badges,
    brew: {
      evenShare,
      note: l.t(
        'How often each Brew potion is taken when a rest site offers it. A potion picked evenly sits near {rate}.',
        { rate: l.pct(evenShare) },
      ),
      picks: [...offers]
        .filter(([, c]) => c.count >= filters.min)
        .map(([label, c]) =>
          rateItem(l, runs.name(idOf(label)), picks.get(label)?.count || 0, c.count, {
            icon: icons[idOf(label)] ?? null,
            note: l.n(c.count, '{n} offer', '{n} offers'),
          }),
        )
        .sort((a, b) => (b.value ?? 0) - (a.value ?? 0)),
      stats: [
        { label: l.t('Brews per run'), note: l.t('at rest sites'), value: l.fixed(rate(totals.brews, totals.runs), 1) },
        {
          label: l.t('Never Brewed'),
          note: l.t('of runs, short ones included'),
          value: l.pct(rate(totals.no_brew_runs, totals.runs)),
        },
        { label: l.t('Potions sold'), note: l.t('per run'), value: l.fixed(rate(totals.potions_sold, totals.runs), 1) },
        {
          label: l.t('Potions drunk'),
          note: l.t('per run'),
          value: l.fixed(rate(totals.potions_drunk, totals.runs_with_drinks), 1),
        },
      ],
    },
    countedSince,
    ferment: {
      cards: cards
        ? [...sumBy(cards, on, (r) => r.card as string)]
            .filter(([, c]) => c.ferment_plays >= filters.min)
            .map(([id, c]) => ({ id, plays: c.ferment_plays, turns: c.ferment_turns / c.ferment_plays }))
            .sort((a, b) => b.turns - a.turns)
            .map((r) => ({
              href: cardHref(r.id),
              label: runs.name(r.id),
              text: l.fixed(r.turns, 1),
              textNote: l.n(r.plays, 'over {n} play', 'over {n} plays'),
              value: r.turns,
            }))
        : [],
      stats: [
        {
          label: l.t('Turns fermented'),
          note: l.t('when a Ferment card is played, on average'),
          value: l.fixed(rate(count('ferment_turns'), fermentPlays), 1),
        },
        {
          label: l.t('Played unfermented'),
          note: l.t('of Ferment card plays'),
          value: l.pct(rate(count('ferment_zero'), fermentPlays)),
        },
        {
          label: l.t('Ferment plays'),
          note: l.t('per run'),
          value: l.fixed(rate(fermentPlays, totals.runs_with_tally), 0),
        },
        {
          label: l.t('Earn {tier}', { tier: fermentedTier }),
          note: l.t('of runs, or a higher tier'),
          value: l.pct(runs.badgeShares(on, 'ALCHEMIST-FERMENTED').share(1)),
        },
      ],
      turns: histogram(on, 'ferment_turns', TURN_RANGE, 'ALCHEMIST-FERMENTED'),
    },
    mixes: {
      fights: fightBins.map((b) => ({
        label: b.bucket,
        tip: [
          l.n(Number(b.bucket), '{n} Mix played', '{n} Mixes played', { n: b.bucket }),
          l.n(b.fights, '{n} fight ({share})', '{n} fights ({share})', { share: l.pct(b.fights / fightTotal) }),
        ],
        value: b.fights,
      })),
      lost: unplayed
        ? (
            [
              [l.t('Still in a pile when the fight ended'), leftover],
              [l.t('Combined into a Compound Mix'), combined],
              [l.t('Exhausted first, by Ethereal or another card'), Math.max(0, unplayed - combined - leftover)],
            ] as [string, number][]
          )
            .sort((a, b) => b[1] - a[1])
            .map(([label, n]) => ({ label, text: l.pct(n / unplayed), textNote: l.num(n), value: n / unplayed }))
        : [],
      lostNote: unplayed
        ? l.t('{share} of the Mixes created were never played. This is where they went.', {
            share: l.pct(unplayed / detailedMade),
          })
        : '',
      made: kinds
        .map((kind) => ({ kind, n: made.get(kind)?.count || 0 }))
        .sort((a, b) => b.n - a.n)
        .map(({ kind, n }) => ({
          dot: mixByKind(kind)!.color,
          label: mixName(kind),
          text: l.pct(n / madeTotal),
          textNote: l.num(n),
          value: n / madeTotal,
        })),
      pairs: shares(
        [...byPrefix(counters, 'pair:')].sort((a, b) => b[1].count - a[1].count),
        (pair) => ({
          icon: null,
          label: pair
            .split('+')
            .map((kind) => mixName(kind))
            .join(' + '),
        }),
      ),
      played: kinds
        .map((kind) => ({ kind, made: made.get(kind)?.count || 0, played: played.get(kind)?.count || 0 }))
        .filter((mix) => mix.made > 0)
        .sort((a, b) => b.played / b.made - a.played / a.made)
        .map((mix) => ({
          dot: mixByKind(mix.kind)!.color,
          label: mixName(mix.kind),
          text: l.pct(Math.min(1, mix.played / mix.made)),
          textNote: l.t('{count} of {total}', { count: l.num(mix.played), total: l.num(mix.made) }),
          value: Math.min(1, mix.played / mix.made),
        })),
      sources: shares([...byPrefix(counters, 'mixsrc:')].sort((a, b) => b[1].count - a[1].count)),
      stats: [
        {
          label: l.t('Mixes per run'),
          note: l.t('created'),
          value: l.fixed(rate(totals.mixes, totals.runs_with_mixes), 0),
        },
        {
          label: l.t('Played'),
          note: l.t('of the Mixes created'),
          value: l.pct(rate(totalCount(played), madeTotal)),
        },
        {
          label: l.t('Compound Mixes'),
          note: l.t('per run'),
          value: l.fixed(rate(made.get('compound')?.count || 0, totals.runs_with_tally), 1),
        },
        { label: l.t('Middle fight'), note: l.t('Mixes played'), value: middleFight ?? '–' },
      ],
    },
    poison: {
      peak: histogram(detailed, 'poison_peak', POISON_RANGE),
      split: [
        { fill: 'var(--color-antitoxin)', label: l.t('Absorbed by Antitoxin'), value: absorbed },
        { fill: 'var(--color-bled)', label: l.t('Hit HP'), value: bled },
      ],
      stats: [
        {
          label: l.t('Poison per run'),
          note: l.t('self-Poison gained'),
          value: l.fixed(rate(totals.poison_gained, totals.runs_with_poison), 0),
        },
        {
          label: l.t('Poison peak'),
          ...countedStat(
            detailedRuns,
            l.fixed(rate(detailedTotals.poison_peak, detailedRuns), 0),
            l.t('the most held at once, on average'),
          ),
        },
        {
          label: l.t('Poison dealt'),
          ...countedStat(
            soloDetailedRuns,
            l.fixed(rate(runs.counters(soloDetailed).get('poison_dealt')?.count || 0, soloDetailedRuns), 0),
            l.t('to enemies per solo run'),
          ),
        },
        {
          label: l.t('Lost to own Poison'),
          note: l.t('of lost runs ended on a Poison tick'),
          value: l.pct(rate(totals.poison_deaths, lostWithTally)),
        },
      ],
      tickFootnote: ticks
        ? l.t('{share} of the Poison ticks taken while holding Antitoxin were absorbed in full.', {
            share: l.pct(count('tick_covered') / ticks),
          })
        : '',
    },
    runs: totals.runs,
  };
}
