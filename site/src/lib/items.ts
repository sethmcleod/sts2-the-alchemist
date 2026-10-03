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
  rarity: null | string;
  source?: null | Source;
}

const KIND_PHRASES: Record<'potion' | 'relic', Record<string, string>> = {
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

export const kindName = (l: Lang, item: Pick<Item, 'kind' | 'rarity'>) =>
  item.kind === 'power'
    ? l.t('Power')
    : l.t(KIND_PHRASES[item.kind][item.rarity ?? ''] ?? `${item.rarity} ${item.kind}`);

export interface Source {
  href: string;
  kind: 'card' | 'potion' | 'relic';
  name: string;
}

const filters = DEFAULT_FILTERS;

const byRarity = (l: Lang, order: string[]) => (a: Item, b: Item) =>
  order.indexOf(a.info.rarity ?? '') - order.indexOf(b.info.rarity ?? '') ||
  a.info.name.localeCompare(b.info.name, l.lang);

export function relics(l: Lang, runs: Runs): Item[] {
  const on = runs.select(filters);
  const totals = runs.totals(on);
  const byRelic = sumBy(runs.table('relics'), on, (row) => row.relic as string);
  return Object.entries(runs.summary.relic_info)
    .map(([id, info]) => {
      const counts = byRelic.get(id);
      return {
        href: relicHref(id),
        id,
        info,
        kind: 'relic' as const,
        line:
          counts && counts.held >= filters.min
            ? l.t('Won {rate} · in {share} of runs', {
                rate: l.pct(counts.held_wins / counts.held),
                share: l.pct(rate(counts.held, totals.runs)),
              })
            : null,
        rarity: info.rarity,
      };
    })
    .sort(byRarity(l, RELIC_ORDER));
}

export function potions(l: Lang, runs: Runs): Item[] {
  const on = runs.select(filters);
  const totals = runs.totals(on);
  const byPotion = sumBy(runs.table('potions'), on, (row) => row.potion as string);
  const counters = runs.counters(on);
  const [offers, picks] = [byPrefix(counters, 'brew_offer:'), byPrefix(counters, 'brew_pick:')];
  return Object.entries(runs.summary.potion_info)
    .map(([id, info]) => {
      const label = tallyLabel(id);
      const offered = offers.get(label)?.count ?? 0;
      const counts = byPotion.get(id);
      const line =
        info.rarity === 'Event'
          ? offered >= filters.min
            ? l.t('Picked {share} of the time at Brew', { share: l.pct(rate(picks.get(label)?.count ?? 0, offered)) })
            : null
          : counts && counts.drunk >= filters.min
            ? l.n(
                Math.round((100 * counts.drunk) / (totals.runs_with_drinks || 1)),
                'Drunk {n} time per 100 runs',
                'Drunk {n} times per 100 runs',
              )
            : null;
      return { href: potionHref(id), id, info, kind: 'potion' as const, line, rarity: potionRarity(info.rarity) };
    })
    .sort(byRarity(l, POTION_ORDER));
}

export function powerSource(runs: Runs, id: string): null | Source {
  const sourceId = id.replace(/_POWER$/, '');
  const { card_info, potion_info, relic_info } = runs.summary;
  if (card_info[sourceId]) return { href: cardHref(sourceId), kind: 'card', name: card_info[sourceId].name };
  if (potion_info[sourceId]) return { href: potionHref(sourceId), kind: 'potion', name: potion_info[sourceId].name };
  if (relic_info[sourceId]) return { href: relicHref(sourceId), kind: 'relic', name: relic_info[sourceId].name };
  return null;
}

export function powers(l: Lang, runs: Runs): Item[] {
  return Object.entries(runs.summary.power_info)
    .map(([id, info]) => ({
      href: powerHref(id),
      id,
      info,
      kind: 'power' as const,
      line: null,
      rarity: null,
      source: powerSource(runs, id),
    }))
    .filter(({ source }) => source)
    .sort((a, b) => a.info.name.localeCompare(b.info.name, l.lang));
}
