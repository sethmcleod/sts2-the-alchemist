// The card library: every card with the values the page sorts, filters and searches by. The stats it
// sorts by use the default filters; the cards themselves show none, since the dialog has them all.

import type { Lang } from './lang';
import { plain } from './markup';
import { cardGroup, CARD_GROUPS, RARITY_NAME } from './mod';
import { cardRows, DEFAULT_FILTERS, type Runs } from './runs';
import type { CardInfo, Rarity } from './types';

export interface LibraryCard {
  id: string;
  info: CardInfo;
  group: string;
  sort: Record<string, number | null>;
  search: string;
}

// A card's type, rarity and keywords in the game's own words, where the game has them
export const typeName = (l: Lang, type: string) => l.game?.types[type] ?? l.t(type);
export const rarityName = (l: Lang, rarity: Rarity) => l.game?.rarities[rarity] ?? l.t(RARITY_NAME[rarity]);
export const keywordName = (l: Lang, keyword: string) => l.game?.keywords[keyword] ?? l.t(keyword);

/** "Common Attack": one phrase per pair, since the rarity's word agrees with the type's in many languages */
export const cardKind = (l: Lang, info: CardInfo) => l.t(`${RARITY_NAME[info.rarity]} ${info.type}`);

/** The library's cards in their order: by group, then by English name, so a card keeps its place in
 *  every language. english holds the English names; without it the runs' own names are used */
export function libraryCards(l: Lang, runs: Runs, english: Runs = runs) {
  const on = runs.select(DEFAULT_FILTERS);
  const stats = cardRows(runs, on, DEFAULT_FILTERS.min);
  const order = CARD_GROUPS.map((g) => g.id);
  const cards: LibraryCard[] = Object.entries(runs.summary.card_info).map(([id, info]) => {
    const r = stats.get(id);
    const enough = r && r.held >= DEFAULT_FILTERS.min ? r : undefined;
    return {
      id,
      info,
      group: cardGroup(info),
      sort: {
        vsPeers: enough?.vsPeers ?? null,
        winrate: enough?.winrate ?? null,
        pickrate: enough?.pickrate ?? null,
        playsPerRun: enough?.playsPerRun ?? null,
        unplayed: enough?.unplayed ?? null,
        held: enough?.held ?? null,
      },
      search: [
        info.name,
        rarityName(l, info.rarity),
        typeName(l, info.type),
        ...info.keywords.map((keyword) => keywordName(l, keyword)),
        ...info.themes.map((theme) => l.t(theme)),
        ...info.texts.map((text) => plain(text, l.game?.words.Energy)),
      ]
        .join(' ')
        .toLocaleLowerCase(l.lang),
    };
  });
  const name = (card: LibraryCard) => english.summary.card_info[card.id]?.name ?? card.info.name;
  return cards.sort((a, b) => order.indexOf(a.group) - order.indexOf(b.group) || name(a).localeCompare(name(b), 'en'));
}
