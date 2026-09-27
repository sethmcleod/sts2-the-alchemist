// The state every stats page shares: the filters, which live in the URL, and the numbers computed
// from them. A page arrives with its numbers for the default filters already drawn; the data files
// load only once a filter changes.

import { useEffect, useRef, useState } from 'preact/hooks';
import { Lang, type LangInit } from '../lib/lang';
import { DEFAULT_FILTERS, EXTRA_FILES, named, Runs, type Extra, type Filters } from '../lib/runs';
import type { AnyTableFile, Summary } from '../lib/types';
import { validFilters, type FilterOptions } from './Filters';

type DataFile = Record<string, unknown> & { built: string };

const requests = new Map<string, Promise<DataFile>>();

// Every load asks the server whether the file changed, so the files all come from one build
function json(file: string, reload: boolean) {
  if (reload || !requests.has(file)) {
    const request = fetch(`/data/${file}`, { cache: reload ? 'reload' : 'no-cache' }).then((response) => {
      if (!response.ok) throw new Error(`${file} answered ${response.status}`);
      return response.json() as Promise<DataFile>;
    });
    request.catch(() => requests.delete(file));
    requests.set(file, request);
  }
  return requests.get(file)!;
}

const datasets = new Map<string, Promise<Runs>>();

function loadRuns(extras: Extra[]) {
  const key = extras.join();
  if (!datasets.has(key)) {
    const files = ['summary.json', ...new Set(extras.map((extra) => EXTRA_FILES[extra][0]))];
    const fetchAll = (reload: boolean) => Promise.all(files.map((file) => json(file, reload)));
    // A daily rebuild can land between two loads, and rows only make sense with their own build's groups
    const dataset = fetchAll(false)
      .then((loaded) => (loaded.every((file) => file.built === loaded[0].built) ? loaded : fetchAll(true)))
      .then((loaded) => {
        const byName = Object.fromEntries(files.map((file, i) => [file, loaded[i]]));
        const tables = Object.fromEntries(
          extras.map((extra) => [extra, byName[EXTRA_FILES[extra][0]][EXTRA_FILES[extra][1]] as AnyTableFile]),
        );
        return new Runs(byName['summary.json'] as unknown as Summary, tables);
      });
    dataset.catch(() => datasets.delete(key));
    datasets.set(key, dataset);
  }
  return datasets.get(key)!;
}

const KEYS = Object.keys(DEFAULT_FILTERS) as (keyof Filters)[];

function readFilters(search: string, options: FilterOptions): Filters {
  const params = new URLSearchParams(search);
  const f = { ...DEFAULT_FILTERS } as Record<string, string | number>;
  for (const key of KEYS) {
    const value = params.get(key);
    if (value) f[key] = key === 'min' ? Number(value) : value;
  }
  return validFilters(f as unknown as Filters, options);
}

function filterQuery(f: Filters) {
  const params = new URLSearchParams();
  for (const key of KEYS) if (f[key] !== DEFAULT_FILTERS[key]) params.set(key, String(f[key]));
  const query = params.toString();
  return query ? `?${query}` : '';
}

/** Keeps the filters in the URL, beside any other parameter the page keeps there, and in the links
    to the other stats pages */
function writeFilters(f: Filters) {
  // While a card's dialog is open the address is the card's, and the page's own entry sits below it
  if (!history.state?.sheet) {
    const params = new URLSearchParams(location.search);
    for (const key of KEYS) {
      if (f[key] === DEFAULT_FILTERS[key]) params.delete(key);
      else params.set(key, String(f[key]));
    }
    const query = params.toString();
    history.replaceState(history.state, '', `${location.pathname}${query ? `?${query}` : ''}${location.hash}`);
  }
  for (const link of document.querySelectorAll<HTMLAnchorElement>('a[data-keep-filters]')) link.search = filterQuery(f);
}

export type Status = 'ready' | 'loading' | 'error';

/** The words an island draws with, for the page that computes the island's first numbers at build */
export const statsLang = (init: LangInit) => new Lang(init, init.strings, init.game, 'stats');

export function useStats<M>(
  initial: M,
  extras: Extra[],
  options: FilterOptions,
  compute: (runs: Runs, f: Filters) => M,
  /** The page's language, whose names go on the data, which comes in English */
  locale?: LangInit,
) {
  const [filters, setFilters] = useState(DEFAULT_FILTERS);
  const [model, setModel] = useState(initial);
  const [status, setStatus] = useState<Status>('ready');
  const latest = useRef(0);
  const current = useRef(DEFAULT_FILTERS);

  async function update(next: Filters) {
    const ticket = ++latest.current;
    current.current = next;
    setFilters(next);
    writeFilters(next);
    setStatus('loading');
    try {
      const runs = await loadRuns(extras);
      if (ticket !== latest.current) return;
      setModel(compute(locale?.names ? runs.withSummary(named(runs.summary, locale.names)) : runs, next));
      setStatus('ready');
    } catch {
      if (ticket === latest.current) setStatus('error');
    }
  }

  // A link with filters in it opens on those filters. A card dialog opened before this ran has
  // put its own address in the URL, so the page's is read from the history entry below it
  useEffect(() => {
    const fromUrl = readFilters(new URL(history.state?.page ?? location.href).search, options);
    if (filterQuery(fromUrl)) update(fromUrl);
  }, []);

  return { filters, model, status, update, refresh: () => update(current.current) };
}
