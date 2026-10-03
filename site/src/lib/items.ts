// Relics, potions and powers, each with the one line of stats its tile shows (default filters)

import type { Lang } from './lang';
import { cardHref, potionHref, powerHref, relicHref } from './links';
import { POTION_ORDER, potionRarity, RELIC_ORDER, tallyLabel } from './mod';
import { byPrefix, DEFAULT_FILTERS, type Runs } from './runs';
import { rate, sumBy } from './stats';
import type { ItemInfo } from './types';

export type ItemKind = 'potion' | 'power' | 'relic';

export interface Item {
  href: string;
  id: string;
  info: ItemInfo;
  kind: ItemKind;
  line: null | string;
  /** The rarity as a player knows it: a Brew potion, not an Event one */
  rarity: null | string;
  /** The card, potion or relic a power comes from */
  source?: null | Source;
}

// A rarity's word agrees with its noun in some languages, so each rarity has a phrase per kind
const KINDS: Record<'potion' | 'relic', Record<string, string>> = {
  potion: { Brew: 'Brew potion', Common: 'Common potion', Rare: 'Rare potion', Uncommon: 'Uncommon potion' },
  relic: {
    Ancient: 'Ancient relic',
    Common: 'Common relic',
    Event: 'Event relic',
    Rare: 'Rare relic',
    Shop: 'Shop relic',
    Starter: 'Starter relic',
    Uncommon: 'Uncommon relic',
  },
};

/** "Common relic", "Brew potion", or "Power" */
export const kindName = (l: Lang, item: Pick<Item, 'kind' | 'rarity'>) =>
  item.kind === 'power' ? l.t('Power') : l.t(KINDS[item.kind][item.rarity ?? ''] ?? `${item.rarity} ${item.kind}`);

export interface Source {
  href: string;
  kind: 'card' | 'potion' | 'relic';
  name: string;
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
        href: relicHref(id),
        id,
        info,
        kind: 'relic' as const,
        line:
          g && g.held >= f.min
            ? l.t('Won {rate} · in {share} of runs', {
                rate: l.pct(g.held_wins / g.held),
                share: l.pct(rate(g.held, t.runs)),
              })
            : null,
        rarity: info.rarity,
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
      return { href: potionHref(id), id, info, kind: 'potion' as const, line, rarity: potionRarity(info.rarity) };
    })
    .sort(byRarity(l, POTION_ORDER));
}

/** Where a power comes from: the card, potion or relic whose id is the power's without "_POWER" */
export function powerSource(runs: Runs, id: string): null | Source {
  const base = id.replace(/_POWER$/, '');
  const { card_info, potion_info, relic_info } = runs.summary;
  if (card_info[base]) return { href: cardHref(base), kind: 'card', name: card_info[base].name };
  if (potion_info[base]) return { href: potionHref(base), kind: 'potion', name: potion_info[base].name };
  if (relic_info[base]) return { href: relicHref(base), kind: 'relic', name: relic_info[base].name };
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
        href: powerHref(id),
        id,
        info,
        kind: 'power' as const,
        line: counts.length ? l.list(counts) : null,
        rarity: null,
        source,
      };
    })
    .sort((a, b) => a.info.name.localeCompare(b.info.name, l.lang));
}
