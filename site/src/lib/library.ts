import type { Lang } from './lang';
import { plain } from './markup';
import { CARD_GROUPS, cardGroup, compendiumOrder, LAST_IN_GROUP, RARITY_NAME } from './mod';
import { cardRows, DEFAULT_FILTERS, type Runs } from './runs';
import type { CardInfo, Rarity } from './types';

export interface LibraryCard {
  group: string;
  id: string;
  info: CardInfo;
  search: string;
  sort: Record<string, null | number>;
}

export const typeName = (l: Lang, type: string) => l.game?.types[type] ?? l.t(type);
export const rarityName = (l: Lang, rarity: Rarity) => l.game?.rarities[rarity] ?? l.t(RARITY_NAME[rarity]);
export const keywordName = (l: Lang, keyword: string) => l.game?.keywords[keyword] ?? l.t(keyword);

export const cardKind = (l: Lang, info: CardInfo) => l.t(`${RARITY_NAME[info.rarity]} ${info.type}`);

export function libraryCards(l: Lang, runs: Runs, english: Runs = runs) {
  const on = runs.select(DEFAULT_FILTERS);
  const rowsById = cardRows(runs, on, DEFAULT_FILTERS.min);
  const groupOrder = CARD_GROUPS.map((group) => group.id);
  const cards: LibraryCard[] = Object.entries(runs.summary.card_info).map(([id, info]) => {
    const row = rowsById.get(id);
    const counted = row && row.held >= DEFAULT_FILTERS.min ? row : undefined;
    return {
      group: cardGroup(info),
      id,
      info,
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
      sort: {
        held: counted?.held ?? null,
        pickrate: counted?.pickrate ?? null,
        playsPerRun: counted?.playsPerRun ?? null,
        unplayed: counted?.unplayed ?? null,
        vsPeers: counted?.vsPeers ?? null,
        winrate: counted?.winrate ?? null,
      },
    };
  });
  const englishName = (card: LibraryCard) => english.summary.card_info[card.id]?.name ?? card.info.name;
  const compendiumKeys = ({ info }: LibraryCard) => ({ cost: info.costs[0], rarity: info.rarity, type: info.type });
  const isLast = (card: LibraryCard) => Number(LAST_IN_GROUP.has(card.id));
  const byCompendium = compendiumOrder();
  return cards.sort(
    (a, b) =>
      groupOrder.indexOf(a.group) - groupOrder.indexOf(b.group) ||
      isLast(a) - isLast(b) ||
      byCompendium(compendiumKeys(a), compendiumKeys(b)) ||
      englishName(a).localeCompare(englishName(b), 'en'),
  );
}
