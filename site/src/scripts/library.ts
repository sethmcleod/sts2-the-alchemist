// Search, filters and sort for the card library. The page works without this: every card is
// listed in the game's compendium order, and the upgrade toggles are CSS. The choices live in the URL, so a reload or a
// shared link shows the same cards.

import { Lang } from '../lib/lang';
import { type CardKeys, type CompendiumKey, compendiumOrder, POOL_RARITIES } from '../lib/mod';

const form = document.querySelector<HTMLFormElement>('#library')!;
const words = JSON.parse(form.dataset.words!);
const l = new Lang(words, words.strings);
const tiles = [...document.querySelectorAll<HTMLElement>('#cards .card-tile')];
const groups = [...document.querySelectorAll<HTMLElement>('[data-card-group]')];
const sorted = document.querySelector<HTMLUListElement>('[data-sorted]')!;
const empty = document.querySelector<HTMLElement>('[data-empty]')!;
const count = document.querySelector<HTMLOutputElement>('[data-count]')!;
const filters = document.querySelector<HTMLElement>('[data-filters]')!;
const drawer = document.querySelector<HTMLDialogElement>('[data-drawer]')!;
const allUpgraded = document.querySelector<HTMLInputElement>('#all-upgraded')!;

const FILTERS = ['type', 'rarity', 'cost', 'keyword', 'from'];
const home = new Map(tiles.map((tile) => [tile, tile.parentElement!]));
// The order the tiles are in, so a render that keeps it moves no tile
let placed = 'rarity';

type State = Record<string, string>;

function read(): State {
  const data = new FormData(form);
  return Object.fromEntries([...data].map(([key, value]) => [key, String(value)]));
}

function matches(tile: HTMLElement, s: State, words: string[]) {
  const d = tile.dataset;
  const cost = d.cost!;
  const from = s.from;
  return (
    (!s.type || d.type === s.type) &&
    (!s.rarity || d.rarity === s.rarity) &&
    (!s.cost || (s.cost === '3' ? Number(cost) >= 3 : cost === s.cost)) &&
    (!s.keyword || d.keywords!.split('|').includes(s.keyword)) &&
    (!from ||
      (from === 'starter' && d.group === 'Basic') ||
      (from === 'pool' && (POOL_RARITIES as readonly string[]).includes(d.group!)) ||
      (from === 'created' && d.group === 'Token') ||
      (from === 'coop' && d.tags!.split('|').includes('Multiplayer')) ||
      from === d.group) &&
    words.every((word) => d.search!.includes(word))
  );
}

const byName = (a: HTMLElement, b: HTMLElement) => a.dataset.name!.localeCompare(b.dataset.name!, l.lang);
const stat = (tile: HTMLElement, key: string) => (tile.dataset[key] ? Number(tile.dataset[key]) : null);

function order(sort: string) {
  if (sort === 'name') return byName;
  if (sort === 'cost' || sort === 'type') {
    const compendium = compendiumOrder(sort as CompendiumKey);
    const keys = (tile: HTMLElement) => tile.dataset as unknown as CardKeys;
    return (a: HTMLElement, b: HTMLElement) => compendium(keys(a), keys(b)) || byName(a, b);
  }
  // A stat puts the highest first and the cards with too few runs last
  return (a: HTMLElement, b: HTMLElement) => {
    const [va, vb] = [stat(a, sort), stat(b, sort)];
    if (va == null || vb == null) return Number(va == null) - Number(vb == null) || byName(a, b);
    return vb - va || byName(a, b);
  };
}

function render() {
  const s = read();
  const words = (s.q ?? '').toLocaleLowerCase(l.lang).split(/\s+/).filter(Boolean);
  const shown = tiles.filter((tile) => {
    const keep = matches(tile, s, words);
    tile.hidden = !keep;
    return keep;
  });

  const sort = s.sort || 'rarity';
  const bySort = sort !== 'rarity';
  if (sort !== placed) {
    if (bySort) sorted.append(...[...tiles].sort(order(sort)));
    else for (const tile of tiles) home.get(tile)!.append(tile);
    placed = sort;
  }
  sorted.hidden = !bySort;
  for (const group of groups) {
    const n = shown.filter((tile) => home.get(tile)!.closest('section') === group).length;
    group.hidden = Boolean(bySort) || n === 0;
    group.querySelector('[data-group-count]')!.textContent = l.n(n, '{n} card', '{n} cards');
  }

  empty.hidden = shown.length > 0;
  count.value =
    shown.length === tiles.length
      ? l.n(tiles.length, '{n} card', '{n} cards')
      : l.n(shown.length, '{n} of {total} card', '{n} of {total} cards', { total: l.num(tiles.length) });
  const active = FILTERS.filter((key) => s[key]).length;
  const badge = document.querySelector<HTMLElement>('[data-filter-count]')!;
  badge.textContent = String(active);
  badge.hidden = !active;
  drawer.querySelector('[data-done]')!.textContent = l.n(shown.length, 'Show {n} card', 'Show {n} cards');
  writeUrl(s);
}

function writeUrl(s: State) {
  // While a card's dialog is open the address is the card's, and the list's own entry sits below it
  if (history.state?.sheet) return;
  const params = new URLSearchParams(
    Object.entries(s).filter(([key, value]) => value && !(key === 'sort' && value === 'rarity')),
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

let typing = 0;
form.addEventListener('input', (e) => {
  clearTimeout(typing);
  if ((e.target as HTMLInputElement).type === 'search') typing = window.setTimeout(render, 120);
  else render();
});
form.addEventListener('submit', (e) => e.preventDefault());

// A card's own toggle flips it against "Show all upgraded", so changing that box resets them all
allUpgraded.addEventListener('change', () => {
  for (const toggle of document.querySelectorAll<HTMLInputElement>('.upgrade-toggle')) toggle.checked = false;
});

// On a narrow screen the filters wait in a drawer, and go back to the toolbar when it closes
document.querySelector('[data-open-filters]')!.addEventListener('click', () => {
  drawer.querySelector('[data-drawer-body]')!.append(filters);
  drawer.showModal();
});
drawer.addEventListener('close', () => form.querySelector('[data-open-filters]')!.after(filters));
// A click on the backdrop closes the drawer, but not the end of a drag that started inside it. The
// drawer's closedby="any" does the same where the browser supports it
let pressedOutside = false;
drawer.addEventListener('pointerdown', (e) => (pressedOutside = e.target === drawer));
drawer.addEventListener('click', (e) => {
  if (e.target === drawer && pressedOutside) drawer.close();
});
drawer.querySelector('[data-done]')!.addEventListener('click', () => drawer.close());
// The drawer's controls sit outside the form, so their changes are read here
drawer.addEventListener('change', render);
for (const button of document.querySelectorAll('[data-clear]')) button.addEventListener('click', clear);

readUrl();
render();
