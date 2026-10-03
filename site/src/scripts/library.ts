import { Lang } from '../lib/lang';
import { type CardKeys, type CompendiumKey, compendiumOrder, POOL_RARITIES } from '../lib/mod';

const form = document.querySelector<HTMLFormElement>('#library')!;
const langInit = JSON.parse(form.dataset.words!);
const l = new Lang(langInit, langInit.strings);
const tiles = [...document.querySelectorAll<HTMLElement>('#cards .card-tile')];
const groups = [...document.querySelectorAll<HTMLElement>('[data-card-group]')];
const sortedList = document.querySelector<HTMLUListElement>('[data-sorted]')!;
const emptyMessage = document.querySelector<HTMLElement>('[data-empty]')!;
const countOutput = document.querySelector<HTMLOutputElement>('[data-count]')!;
const filters = document.querySelector<HTMLElement>('[data-filters]')!;
const drawer = document.querySelector<HTMLDialogElement>('[data-drawer]')!;
const allUpgraded = document.querySelector<HTMLInputElement>('#all-upgraded')!;

const FILTERS = ['type', 'rarity', 'cost', 'keyword', 'from'];
const homeList = new Map(tiles.map((tile) => [tile, tile.parentElement!]));
let arrangedBy = 'rarity';

type State = Record<string, string>;

function readForm(): State {
  const data = new FormData(form);
  return Object.fromEntries([...data].map(([key, value]) => [key, String(value)]));
}

function matches(tile: HTMLElement, state: State, searchWords: string[]) {
  const card = tile.dataset;
  const cost = card.cost!;
  const from = state.from;
  return (
    (!state.type || card.type === state.type) &&
    (!state.rarity || card.rarity === state.rarity) &&
    (!state.cost || (state.cost === '3' ? Number(cost) >= 3 : cost === state.cost)) &&
    (!state.keyword || card.keywords!.split('|').includes(state.keyword)) &&
    (!from ||
      (from === 'starter' && card.group === 'Basic') ||
      (from === 'pool' && (POOL_RARITIES as readonly string[]).includes(card.group!)) ||
      (from === 'created' && card.group === 'Token') ||
      (from === 'coop' && card.tags!.split('|').includes('Multiplayer')) ||
      from === card.group) &&
    searchWords.every((word) => card.search!.includes(word))
  );
}

const byName = (a: HTMLElement, b: HTMLElement) => a.dataset.name!.localeCompare(b.dataset.name!, l.lang);
const statOf = (tile: HTMLElement, key: string) => (tile.dataset[key] ? Number(tile.dataset[key]) : null);

function compareBy(sort: string) {
  if (sort === 'name') return byName;
  if (sort === 'cost' || sort === 'type') {
    const compendium = compendiumOrder(sort as CompendiumKey);
    const keys = (tile: HTMLElement) => tile.dataset as unknown as CardKeys;
    return (a: HTMLElement, b: HTMLElement) => compendium(keys(a), keys(b)) || byName(a, b);
  }
  return (a: HTMLElement, b: HTMLElement) => {
    const [statA, statB] = [statOf(a, sort), statOf(b, sort)];
    if (statA == null || statB == null) return Number(statA == null) - Number(statB == null) || byName(a, b);
    return statB - statA || byName(a, b);
  };
}

function render() {
  const state = readForm();
  const searchWords = (state.q ?? '').toLocaleLowerCase(l.lang).split(/\s+/).filter(Boolean);
  const shown = tiles.filter((tile) => {
    const keep = matches(tile, state, searchWords);
    tile.hidden = !keep;
    return keep;
  });

  const sort = state.sort || 'rarity';
  const ungrouped = sort !== 'rarity';
  if (sort !== arrangedBy) {
    if (ungrouped) sortedList.append(...[...tiles].sort(compareBy(sort)));
    else for (const tile of tiles) homeList.get(tile)!.append(tile);
    arrangedBy = sort;
  }
  sortedList.hidden = !ungrouped;
  for (const group of groups) {
    const shownInGroup = shown.filter((tile) => homeList.get(tile)!.closest('section') === group).length;
    group.hidden = ungrouped || shownInGroup === 0;
    group.querySelector('[data-group-count]')!.textContent = l.n(shownInGroup, '{n} card', '{n} cards');
  }

  emptyMessage.hidden = shown.length > 0;
  countOutput.value =
    shown.length === tiles.length
      ? l.n(tiles.length, '{n} card', '{n} cards')
      : l.n(shown.length, '{n} of {total} card', '{n} of {total} cards', { total: l.num(tiles.length) });
  const activeFilters = FILTERS.filter((key) => state[key]).length;
  const badge = document.querySelector<HTMLElement>('[data-filter-count]')!;
  badge.textContent = String(activeFilters);
  badge.hidden = !activeFilters;
  drawer.querySelector('[data-done]')!.textContent = l.n(shown.length, 'Show {n} card', 'Show {n} cards');
  writeUrl(state);
}

function writeUrl(state: State) {
  if (history.state?.sheet) return;
  const params = new URLSearchParams(
    Object.entries(state).filter(([key, value]) => value && !(key === 'sort' && value === 'rarity')),
  );
  const query = params.toString();
  history.replaceState(history.state, '', `${location.pathname}${query ? `?${query}` : ''}${location.hash}`);
}

function readUrl() {
  const params = new URLSearchParams(location.search);
  for (const control of [...form.elements] as HTMLInputElement[]) {
    if (!control.name || !params.has(control.name)) continue;
    if (control.type === 'checkbox') control.checked = true;
    else control.value = params.get(control.name)!;
  }
}

function clear() {
  for (const control of [...form.elements] as HTMLSelectElement[]) {
    if (FILTERS.includes(control.name) || control.name === 'q') control.value = '';
  }
  render();
}

let searchTimer = 0;
form.addEventListener('input', (e) => {
  clearTimeout(searchTimer);
  if ((e.target as HTMLInputElement).type === 'search') searchTimer = window.setTimeout(render, 120);
  else render();
});
form.addEventListener('submit', (e) => e.preventDefault());

allUpgraded.addEventListener('change', () => {
  for (const toggle of document.querySelectorAll<HTMLInputElement>('.upgrade-toggle')) toggle.checked = false;
});

document.querySelector('[data-open-filters]')!.addEventListener('click', () => {
  drawer.querySelector('[data-drawer-body]')!.append(filters);
  drawer.showModal();
});
drawer.addEventListener('close', () => form.querySelector('[data-open-filters]')!.after(filters));
let pressedOutside = false;
drawer.addEventListener('pointerdown', (e) => (pressedOutside = e.target === drawer));
drawer.addEventListener('click', (e) => {
  if (e.target === drawer && pressedOutside) drawer.close();
});
drawer.querySelector('[data-done]')!.addEventListener('click', () => drawer.close());
drawer.addEventListener('change', render);
for (const button of document.querySelectorAll('[data-clear]')) button.addEventListener('click', clear);

readUrl();
render();
