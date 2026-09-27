import type { BarItem } from '../../components/charts/Bars';
import type { Column } from '../../components/charts/Columns';
import type { Stat } from '../../components/charts/Stats';
import { rateItem } from '../bars';
import type { Lang } from '../lang';
import { cardHref } from '../links';
import { MIX_KINDS } from '../mod';
import { byPrefix, totalCount, type Filters, type Runs } from '../runs';
import { rate, sumBy, type Counts, type Selection } from '../stats';

const MIX_FIGHT_BUCKETS = ['0', '1', '2', '3', '4', '5-6', '7-9', '10+'];
const TIER_FILL = ['var(--color-bronze)', 'var(--color-silver)', 'var(--color-gold)'];

// What a histogram column covers, in the unit its metric counts: from and to, or from and up
type Span = (l: Lang, from: string, to?: string) => string;
const TURNS: Span = (l, from, to) =>
  to ? l.t('{from} to {to} turns', { from, to }) : l.t('{from} or more turns', { from });
const POISON: Span = (l, from, to) =>
  to ? l.t('{from} to {to} Poison', { from, to }) : l.t('{from} or more Poison', { from });
const ANTITOXIN: Span = (l, from, to) =>
  to ? l.t('{from} to {to} Antitoxin', { from, to }) : l.t('{from} or more Antitoxin', { from });

export interface Histogram {
  columns: Column[];
  markers: { before: number; label: string }[];
  foot: string;
}

export interface MechanicsModel {
  runs: number;
  countedSince: string;
  badges: { id: string; name: string; icon: string | null; text: string; tiers: BarItem[] }[];
  brew: { stats: Stat[]; note: string; picks: BarItem[]; even: number | null };
  mixes: {
    stats: Stat[];
    made: BarItem[];
    played: BarItem[];
    fights: Column[];
    sources: BarItem[];
    lostNote: string;
    lost: BarItem[];
    pairs: BarItem[];
  };
  ferment: { stats: Stat[]; turns: Histogram; cards: BarItem[] };
  poison: { stats: Stat[]; peak: Histogram; split: { label: string; value: number; fill: string }[]; tickFoot: string };
  antitoxin: { stats: Stat[]; peak: Histogram; sources: BarItem[]; decayFoot: string };
}

/** Icon URLs by model id, made at build time */
export type Icons = Record<string, string>;

export function mechanics(l: Lang, runs: Runs, f: Filters, icons: Icons): MechanicsModel {
  const on = runs.select(f);
  const t = runs.totals(on);
  const all = runs.counters(on);
  const count = (key: string) => all.get(key)?.count || 0;
  const countedSince = runs.countedSince(l);

  // The schema 3 counters read only the groups whose client sends them. Enemy Poison counts only
  // in solo runs, so it has its own denominator
  const on3 = runs.withDetail(on);
  const runs3 = runs.totals(on3).runs;
  const totals3 = runs.totals(on3);
  const all3 = runs.counters(on3);
  const count3 = (key: string) => all3.get(key)?.count || 0;
  const solo3: Selection = on3.map((flag, i) => (flag && !runs.groups[i].coop ? 1 : 0));
  const soloRuns3 = runs.totals(solo3).runs;
  const detail = (n: number, value: string, note: string) =>
    n ? { value, note } : { value: '–', note: l.t('counted {since}', { since: countedSince }) };

  const idOf = (label: string) => runs.meta.prefix + label.toUpperCase();
  const source = (label: string) => {
    if (label === 'unknown') return { label: l.t('Other'), icon: null };
    const id = idOf(label);
    return {
      label: runs.name(id),
      icon: icons[id] ?? null,
      href: runs.summary.card_info[id] ? cardHref(id) : undefined,
    };
  };
  const shares = (entries: [string, Counts][], labelOf: (label: string) => Partial<BarItem> = source) => {
    const total = entries.reduce((n, [, c]) => n + c.count, 0);
    return entries.map(([label, c]) => ({
      label,
      ...labelOf(label),
      value: c.count / total,
      text: l.pct(c.count / total),
      textNote: l.num(c.count),
    }));
  };
  const mixName = (kind: string) => (MIX_KINDS[kind] ? l.t(MIX_KINDS[kind].name) : kind);

  const histogram = (selection: Selection, metric: string, span: Span, badgeId?: string): Histogram => {
    const { bins, width, last } = runs.histogram(selection, metric);
    const total = bins.reduce((n, b) => n + b.runs, 0);
    const badge = badgeId ? runs.meta.badges.find((b) => b.id === badgeId) : undefined;
    let foot = '';
    if (badge && total) {
      const { share } = runs.badgeShares(on, badge.id);
      const tiers = badge.tiers.map((tier, i) =>
        l.t('{share} {tier}', { share: l.pct(share(i + 1)), tier: tier.title }),
      );
      foot = l.t('Runs that earned each tier or better: {tiers}.', { tiers: l.list(tiers) });
    }
    return {
      columns: bins.map((b) => ({
        label: b.bin === last ? l.t('{n}+', { n: l.num(b.bin) }) : l.num(b.bin),
        value: b.runs,
        tip: [
          b.bin === last ? span(l, l.num(b.bin)) : span(l, l.num(b.bin), l.num(b.bin + width - 1)),
          l.n(b.runs, '{n} run ({share})', '{n} runs ({share})', { share: l.pct(b.runs / total) }),
        ],
      })),
      markers: badge?.tiers.map((tier) => ({ before: tier.at / width, label: tier.title })) ?? [],
      foot,
    };
  };

  // Badges
  const badges = runs.meta.badges.map((badge) => {
    const { eligible, share } = runs.badgeShares(on, badge.id);
    const text = { text: badge.tiers[0].text };
    return {
      id: badge.id,
      name: l.t(
        badge.id
          .slice(runs.meta.prefix.length)
          .replaceAll('_', ' ')
          .toLowerCase()
          .replace(/^\w/, (c) => c.toUpperCase()),
      ),
      icon: icons[badge.id] ?? null,
      text: badge.needs_win
        ? l.n(eligible, '{text} Counted over {n} win.', '{text} Counted over {n} wins.', text)
        : l.n(eligible, '{text} Counted over {n} run.', '{text} Counted over {n} runs.', text),
      tiers: eligible
        ? badge.tiers.map((tier, i) => ({
            label: tier.title,
            note: badge.tiers.length > 1 ? l.t('{n}+', { n: l.num(tier.at) }) : null,
            value: share(i + 1),
            text: l.pct(share(i + 1)),
            fill: TIER_FILL[i],
          }))
        : [],
    };
  });

  // Brew
  const offers = byPrefix(all, 'brew_offer:');
  const picks = byPrefix(all, 'brew_pick:');
  const even = rate(totalCount(picks), totalCount(offers));

  // Mixes
  const made = byPrefix(all, 'mixmade:');
  const played = byPrefix(all, 'mixplay:');
  const madeTotal = totalCount(made);
  const fightCounts = byPrefix(all, 'mixfight:');
  const fightBins = MIX_FIGHT_BUCKETS.map((bucket) => ({ bucket, fights: fightCounts.get(bucket)?.count || 0 }));
  const fightTotal = fightBins.reduce((n, b) => n + b.fights, 0);
  let seen = 0;
  const middleFight = fightTotal ? fightBins.find((b) => (seen += b.fights) >= fightTotal / 2)?.bucket : undefined;
  const kinds = Object.keys(MIX_KINDS).filter((kind) => made.has(kind) || played.has(kind));
  const made3 = totalCount(byPrefix(all3, 'mixmade:'));
  const unplayed = Math.max(0, made3 - totalCount(byPrefix(all3, 'mixplay:')));
  const combined = count3('mixlost:combined');
  const leftover = count3('mixlost:leftover');

  // Ferment
  const fermentPlays = count('ferment_plays');
  const cards = runs.tables.cards;
  const aged = runs.meta.badges.find((b) => b.id === 'ALCHEMIST-FERMENTED')?.tiers[0].title ?? '';

  // Poison and Antitoxin
  const absorbed = t.poison_absorbed;
  const bled = t.poison_bled;
  const lostWithTally = t.runs_with_tally - t.wins_with_tally;
  const ticks = count('tick_covered') + count('tick_bled');
  const antitoxinSources = [...byPrefix(all3, 'atxsrc:')].sort((a, b) => b[1].count - a[1].count);
  const gained = antitoxinSources.reduce((n, [, c]) => n + c.count, 0);
  const decayed = count3('atx_decayed');

  return {
    runs: t.runs,
    countedSince,
    badges,
    brew: {
      stats: [
        { label: l.t('Brews per run'), value: l.fixed(rate(t.brews, t.runs), 1), note: l.t('at rest sites') },
        {
          label: l.t('Never Brewed'),
          value: l.pct(rate(t.no_brew_runs, t.runs)),
          note: l.t('of runs, short ones included'),
        },
        { label: l.t('Potions sold'), value: l.fixed(rate(t.potions_sold, t.runs), 1), note: l.t('per run') },
        {
          label: l.t('Potions drunk'),
          value: l.fixed(rate(t.potions_drunk, t.runs_with_drinks), 1),
          note: l.t('per run'),
        },
      ],
      note: l.t(
        'How often each Brew potion is taken when a rest site offers it. A potion picked evenly sits near {rate}.',
        { rate: l.pct(even) },
      ),
      even,
      picks: [...offers]
        .filter(([, c]) => c.count >= f.min)
        .map(([label, c]) =>
          rateItem(l, runs.name(idOf(label)), picks.get(label)?.count || 0, c.count, {
            note: l.n(c.count, '{n} offer', '{n} offers'),
            icon: icons[idOf(label)] ?? null,
          }),
        )
        .sort((a, b) => (b.value ?? 0) - (a.value ?? 0)),
    },
    mixes: {
      stats: [
        { label: l.t('Mixes per run'), value: l.fixed(rate(t.mixes, t.runs_with_mixes), 0), note: l.t('created') },
        {
          label: l.t('Played'),
          value: l.pct(rate(totalCount(played), madeTotal)),
          note: l.t('of the Mixes created'),
        },
        {
          label: l.t('Compound Mixes'),
          value: l.fixed(rate(made.get('compound')?.count || 0, t.runs_with_tally), 1),
          note: l.t('per run'),
        },
        { label: l.t('Middle fight'), value: middleFight ?? '–', note: l.t('Mixes played') },
      ],
      made: kinds
        .map((kind) => ({ kind, n: made.get(kind)?.count || 0 }))
        .sort((a, b) => b.n - a.n)
        .map(({ kind, n }) => ({
          label: mixName(kind),
          dot: MIX_KINDS[kind].color,
          value: n / madeTotal,
          text: l.pct(n / madeTotal),
          textNote: l.num(n),
        })),
      played: kinds
        .map((kind) => ({ kind, made: made.get(kind)?.count || 0, played: played.get(kind)?.count || 0 }))
        .filter((k) => k.made > 0)
        .sort((a, b) => b.played / b.made - a.played / a.made)
        .map((k) => ({
          label: mixName(k.kind),
          dot: MIX_KINDS[k.kind].color,
          value: Math.min(1, k.played / k.made),
          text: l.pct(Math.min(1, k.played / k.made)),
          textNote: l.t('{count} of {total}', { count: l.num(k.played), total: l.num(k.made) }),
        })),
      // A bucket is a count or a span of counts ("5-6"), and a span takes the plural
      fights: fightBins.map((b) => ({
        label: b.bucket,
        value: b.fights,
        tip: [
          l.n(Number(b.bucket), '{n} Mix played', '{n} Mixes played', { n: b.bucket }),
          l.n(b.fights, '{n} fight ({share})', '{n} fights ({share})', { share: l.pct(b.fights / fightTotal) }),
        ],
      })),
      sources: shares([...byPrefix(all, 'mixsrc:')].sort((a, b) => b[1].count - a[1].count)),
      lostNote: unplayed
        ? l.t('{share} of the Mixes created were never played. This is where they went.', {
            share: l.pct(unplayed / made3),
          })
        : '',
      lost: unplayed
        ? (
            [
              [l.t('Still in a pile when the fight ended'), leftover],
              [l.t('Combined into a Compound Mix'), combined],
              [l.t('Exhausted first, by Ethereal or another card'), Math.max(0, unplayed - combined - leftover)],
            ] as [string, number][]
          )
            .sort((a, b) => b[1] - a[1])
            .map(([label, n]) => ({ label, value: n / unplayed, text: l.pct(n / unplayed), textNote: l.num(n) }))
        : [],
      pairs: shares(
        [...byPrefix(all, 'pair:')].sort((a, b) => b[1].count - a[1].count),
        (pair) => ({
          label: pair
            .split('+')
            .map((kind) => mixName(kind))
            .join(' + '),
          icon: null,
        }),
      ),
    },
    ferment: {
      stats: [
        {
          label: l.t('Turns fermented'),
          value: l.fixed(rate(count('ferment_turns'), fermentPlays), 1),
          note: l.t('when a Ferment card is played, on average'),
        },
        {
          label: l.t('Played unfermented'),
          value: l.pct(rate(count('ferment_zero'), fermentPlays)),
          note: l.t('of Ferment card plays'),
        },
        {
          label: l.t('Ferment plays'),
          value: l.fixed(rate(fermentPlays, t.runs_with_tally), 0),
          note: l.t('per run'),
        },
        {
          label: l.t('Earn {tier}', { tier: aged }),
          value: l.pct(runs.badgeShares(on, 'ALCHEMIST-FERMENTED').share(1)),
          note: l.t('of runs, or a higher tier'),
        },
      ],
      turns: histogram(on, 'ferment_turns', TURNS, 'ALCHEMIST-FERMENTED'),
      cards: cards
        ? [...sumBy(cards, on, (r) => r.card as string)]
            .filter(([, c]) => c.ferment_plays >= f.min)
            .map(([id, c]) => ({ id, turns: c.ferment_turns / c.ferment_plays, plays: c.ferment_plays }))
            .sort((a, b) => b.turns - a.turns)
            .map((r) => ({
              label: runs.name(r.id),
              href: cardHref(r.id),
              value: r.turns,
              text: l.fixed(r.turns, 1),
              textNote: l.n(r.plays, 'over {n} play', 'over {n} plays'),
            }))
        : [],
    },
    poison: {
      stats: [
        {
          label: l.t('Poison per run'),
          value: l.fixed(rate(t.poison_gained, t.runs_with_poison), 0),
          note: l.t('self-Poison gained'),
        },
        {
          label: l.t('Poison peak'),
          ...detail(runs3, l.fixed(rate(totals3.poison_peak, runs3), 0), l.t('the most held at once, on average')),
        },
        {
          label: l.t('Poison dealt'),
          ...detail(
            soloRuns3,
            l.fixed(rate(runs.counters(solo3).get('poison_dealt')?.count || 0, soloRuns3), 0),
            l.t('to enemies per solo run'),
          ),
        },
        {
          label: l.t('Lost to own Poison'),
          value: l.pct(rate(t.poison_deaths, lostWithTally)),
          note: l.t('of lost runs ended on a Poison tick'),
        },
      ],
      peak: histogram(on3, 'poison_peak', POISON),
      split: [
        { label: l.t('Absorbed by Antitoxin'), value: absorbed, fill: 'var(--color-antitoxin)' },
        { label: l.t('Hit HP'), value: bled, fill: 'var(--color-bled)' },
      ],
      tickFoot: ticks
        ? l.t('{share} of the Poison ticks taken while holding Antitoxin were absorbed in full.', {
            share: l.pct(count('tick_covered') / ticks),
          })
        : '',
    },
    antitoxin: {
      stats: [
        {
          label: l.t('Antitoxin peak'),
          value: l.fixed(rate(t.antitoxin_peak, t.runs_with_peak), 0),
          note: l.t('the most held at once, on average'),
        },
        { label: l.t('Gained per run'), ...detail(runs3, l.fixed(rate(gained, runs3), 0), l.t('from every source')) },
        { label: l.t('Lost to decay'), ...detail(runs3, l.pct(rate(decayed, gained)), l.t('of the Antitoxin gained')) },
        { label: l.t('Absorbed'), value: l.pct(rate(absorbed, absorbed + bled)), note: l.t('of self-Poison damage') },
      ],
      peak: histogram(on, 'antitoxin_peak', ANTITOXIN, 'ALCHEMIST-ANTITOXIN_PEAK'),
      sources: shares(antitoxinSources),
      decayFoot: gained
        ? l.t(
            'Of the Antitoxin gained, {share} thinned away at 1 a turn. The rest was still up when each fight ended.',
            { share: l.pct(decayed / gained) },
          )
        : '',
    },
  };
}
