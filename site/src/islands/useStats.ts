import { useEffect, useRef, useState } from 'preact/hooks';
import { Lang, type LangInit } from '../lib/lang';
import { DEFAULT_FILTERS, type Extra, EXTRA_FILES, type Filters, Runs, withTranslatedNames } from '../lib/runs';
import type { AnyTableFile, Summary } from '../lib/types';
import { type FilterOptions, validFilters } from './Filters';

type DataFile = Record<string, unknown> & { built: string };

const requests = new Map<string, Promise<DataFile>>();

function fetchJson(file: string, reload: boolean) {
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
    const fetchAll = (reload: boolean) => Promise.all(files.map((file) => fetchJson(file, reload)));
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

const FILTER_KEYS = Object.keys(DEFAULT_FILTERS) as (keyof Filters)[];

function readFilters(search: string, options: FilterOptions): Filters {
  const params = new URLSearchParams(search);
  const filters = { ...DEFAULT_FILTERS } as Record<string, number | string>;
  for (const key of FILTER_KEYS) {
    const value = params.get(key);
    if (value) filters[key] = key === 'min' ? Number(value) : value;
  }
  return validFilters(filters as unknown as Filters, options);
}

function filterQuery(filters: Filters) {
  const params = new URLSearchParams();
  for (const key of FILTER_KEYS) if (filters[key] !== DEFAULT_FILTERS[key]) params.set(key, String(filters[key]));
  const query = params.toString();
  return query ? `?${query}` : '';
}

function writeFilters(filters: Filters) {
  if (!history.state?.sheet) {
    const params = new URLSearchParams(location.search);
    for (const key of FILTER_KEYS) {
      if (filters[key] === DEFAULT_FILTERS[key]) params.delete(key);
      else params.set(key, String(filters[key]));
    }
    const query = params.toString();
    history.replaceState(history.state, '', `${location.pathname}${query ? `?${query}` : ''}${location.hash}`);
  }
  for (const link of document.querySelectorAll<HTMLAnchorElement>('a[data-keep-filters]'))
    link.search = filterQuery(filters);
}

export type Status = 'error' | 'loading' | 'ready';

export const statsLang = (init: LangInit) => new Lang(init, init.strings, init.game, 'stats');

export function useStats<M>(
  initial: M,
  extras: Extra[],
  options: FilterOptions,
  compute: (runs: Runs, filters: Filters) => M,
  locale?: LangInit,
) {
  const [filters, setFilters] = useState(DEFAULT_FILTERS);
  const [model, setModel] = useState(initial);
  const [status, setStatus] = useState<Status>('ready');
  const latestTicket = useRef(0);
  const currentFilters = useRef(DEFAULT_FILTERS);

  async function update(next: Filters) {
    const ticket = ++latestTicket.current;
    currentFilters.current = next;
    setFilters(next);
    writeFilters(next);
    setStatus('loading');
    try {
      const runs = await loadRuns(extras);
      if (ticket !== latestTicket.current) return;
      setModel(compute(locale?.names ? runs.withSummary(withTranslatedNames(runs.summary, locale.names)) : runs, next));
      setStatus('ready');
    } catch {
      if (ticket === latestTicket.current) setStatus('error');
    }
  }

  useEffect(() => {
    const fromUrl = readFilters(new URL(history.state?.page ?? location.href).search, options);
    if (filterQuery(fromUrl)) update(fromUrl);
  }, []);

  return { filters, model, refresh: () => update(currentFilters.current), status, update };
}
