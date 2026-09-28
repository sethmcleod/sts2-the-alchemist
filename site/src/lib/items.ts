// Relics, potions and powers, each with the one line of stats its tile shows (default filters)

import type { Lang } from './lang';
import { cardHref, potionHref, powerHref, relicHref } from './links';
import { POTION_ORDER, potionRarity, RELIC_ORDER, tallyLabel } from './mod';
import { byPrefix, DEFAULT_FILTERS, type Runs } from './runs';
import { rate, sumBy } from './stats';
import type { ItemInfo } from './types';

export type ItemKind = 'relic' | 'potion' | 'power';

export interface Item {
  id: string;
  kind: ItemKind;
  info: ItemInfo;
  /** The rarity as a player knows it: a Brew potion, not an Event one */
  rarity: string | null;
  href: string;
  line: string | null;
  /** The card, potion or relic a power comes from */
  source?: Source | null;
}

// A rarity's word agrees with its noun in some languages, so each rarity has a phrase per kind
const KINDS: Record<'relic' | 'potion', Record<string, string>> = {
  relic: {
    Starter: 'Starter relic',
    Common: 'Common relic',
    Uncommon: 'Uncommon relic',
    Rare: 'Rare relic',
    Shop: 'Shop relic',
    Ancient: 'Ancient relic',
    Event: 'Event relic',
  },
  potion: { Common: 'Common potion', Uncommon: 'Uncommon potion', Rare: 'Rare potion', Brew: 'Brew potion' },
};

/** "Common relic", "Brew potion", or "Power" */
export const kindName = (l: Lang, item: Pick<Item, 'kind' | 'rarity'>) =>
  item.kind === 'power' ? l.t('Power') : l.t(KINDS[item.kind][item.rarity ?? ''] ?? `${item.rarity} ${item.kind}`);

export interface Source {
  name: string;
  href: string;
  kind: 'card' | 'potion' | 'relic';
}

const f = DEFAULT_FILTERS;

const byRarity = (l: Lang, order: string[]) => (a: Item, b: Item) =>
  order.indexOf(a.info.rarity ?? '') - order.indexOf(b.info.rarity ?? '') ||
  a.info.name.localeCompare(b.info.name, l.lang);

export function relics(l: Lang, runs: Runs): Item[] {
  const on = runs.select(f);
  const t = runs.totals(on);
  const held = sumBy(runs.table('relics'), on, (r) => r.relic as string);
  return Object.entries(runs.summary.relic_info)
    .map(([id, info]) => {
      const g = held.get(id);
      return {
        id,
        kind: 'relic' as const,
        info,
        rarity: info.rarity,
        href: relicHref(id),
        line:
          g && g.held >= f.min
            ? l.t('Won {rate} · in {share} of runs', {
                rate: l.pct(g.held_wins / g.held),
                share: l.pct(rate(g.held, t.runs)),
              })
            : null,
      };
    })
    .sort(byRarity(l, RELIC_ORDER));
}

export function potions(l: Lang, runs: Runs): Item[] {
  const on = runs.select(f);
  const t = runs.totals(on);
  const drunk = sumBy(runs.table('potions'), on, (r) => r.potion as string);
  const all = runs.counters(on);
  const [offers, picks] = [byPrefix(all, 'brew_offer:'), byPrefix(all, 'brew_pick:')];
  return Object.entries(runs.summary.potion_info)
    .map(([id, info]) => {
      const label = tallyLabel(id);
      const offered = offers.get(label)?.count ?? 0;
      const g = drunk.get(id);
      const line =
        info.rarity === 'Event'
          ? offered >= f.min
            ? l.t('Picked {share} of the time at Brew', { share: l.pct(rate(picks.get(label)?.count ?? 0, offered)) })
            : null
          : g && g.drunk >= f.min
            ? l.n(
                Math.round((100 * g.drunk) / (t.runs_with_drinks || 1)),
                'Drunk {n} time per 100 runs',
                'Drunk {n} times per 100 runs',
              )
            : null;
      return { id, kind: 'potion' as const, info, rarity: potionRarity(info.rarity), href: potionHref(id), line };
    })
    .sort(byRarity(l, POTION_ORDER));
}

/** Where a power comes from: the card, potion or relic whose id is the power's without "_POWER" */
export function powerSource(runs: Runs, id: string): Source | null {
  const base = id.replace(/_POWER$/, '');
  const { card_info, potion_info, relic_info } = runs.summary;
  if (card_info[base]) return { name: card_info[base].name, href: cardHref(base), kind: 'card' };
  if (potion_info[base]) return { name: potion_info[base].name, href: potionHref(base), kind: 'potion' };
  if (relic_info[base]) return { name: relic_info[base].name, href: relicHref(base), kind: 'relic' };
  return null;
}

export function powers(l: Lang, runs: Runs): Item[] {
  const all = runs.counters(runs.select(f));
  const [antitoxin, mixes] = [byPrefix(all, 'atxsrc:'), byPrefix(all, 'mixsrc:')];
  return Object.entries(runs.summary.power_info)
    .map(([id, info]) => ({ id, info, source: powerSource(runs, id) }))
    .filter(({ source }) => source)
    .map(({ id, info, source }) => {
      const label = tallyLabel(id);
      const [gave, made] = [antitoxin.get(label)?.count, mixes.get(label)?.count];
      const counts = [
        gave != null ? l.n(gave, 'gave {n} Antitoxin', 'gave {n} Antitoxin') : null,
        made != null ? l.n(made, 'made {n} Mix', 'made {n} Mixes') : null,
      ].filter((count) => count !== null);
      return {
        id,
        kind: 'power' as const,
        info,
        rarity: null,
        href: powerHref(id),
        line: counts.length ? l.list(counts) : null,
        source,
      };
    })
    .sort((a, b) => a.info.name.localeCompare(b.info.name, l.lang));
}
