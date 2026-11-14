import { useEffect, useRef } from 'preact/hooks';
import type { Lang } from '../lib/lang';
import { notesHref } from '../lib/links';
import { ASCENSION_BANDS } from '../lib/mod';
import { DEFAULT_FILTERS, type Filters as FilterValues, patchOf, type Runs } from '../lib/runs';
import { compareVersions } from '../lib/stats';
import { fillSlots } from './useLang';
import type { Status } from './useStats';

export interface FilterOptions {
  defaultPatches: Record<string, string>;
  modes: [mode: string, label: string][];
  patches: [patch: string, label: string][];
  recentStarts: Record<string, string>;
  versions: string[];
}

const BRANCH_LABELS: Record<string, (l: Lang, build: string) => string> = {
  public: (l, build) => l.t('{build} (public)', { build }),
  'public-beta': (l, build) => l.t('{build} (beta)', { build }),
};

const MODE_NAMES: Record<string, string> = { blitz: 'Blitz', dread: 'Dread', standard: 'Standard' };

export function filterOptions(l: Lang, runs: Runs): FilterOptions {
  const branches = new Map<string, string>();
  for (const [build, branch] of Object.entries(runs.meta.builds).sort(([a], [b]) => compareVersions(a, b)))
    branches.set(patchOf(build), branch);
  const modeName = (mode: string) => l.game?.modes?.[MODE_NAMES[mode]] ?? MODE_NAMES[mode] ?? mode;
  const patches = [...branches.keys()].sort((a, b) => compareVersions(b, a));
  const modes = runs.meta.modes ?? [DEFAULT_FILTERS.mode];
  const defaultPatches = Object.fromEntries(modes.map((mode) => [mode, runs.patch(DEFAULT_FILTERS.build, mode)]));
  const recentStarts: Record<string, string> = {};
  for (const mode of modes)
    for (const build of [DEFAULT_FILTERS.build, 'all', ...patches.filter((patch) => patch !== defaultPatches[mode])])
      recentStarts[scopeKey({ build, mode })] = runs.recentStartFor({ build, mode });
  return {
    defaultPatches,
    modes: modes.map((mode) => [mode, modeName(mode)]),
    patches: patches.map((patch) => [patch, BRANCH_LABELS[branches.get(patch)!]?.(l, patch) ?? patch]),
    recentStarts,
    versions: [...runs.meta.versions].reverse(),
  };
}

const scopeKey = ({ build, mode }: Pick<FilterValues, 'build' | 'mode'>) => `${build} ${mode}`;

const recentStart = (options: FilterOptions, filters: FilterValues) =>
  options.recentStarts[scopeKey(filters)] ?? options.versions.at(-1)!;

function validPatch(choice: string, mode: string, options: FilterOptions) {
  const patch = patchOf(choice);
  if (choice === 'all' || patch === options.defaultPatches[mode])
    return choice === 'all' ? 'all' : DEFAULT_FILTERS.build;
  return options.patches.some(([known]) => known === patch) ? patch : DEFAULT_FILTERS.build;
}

const PLAYERS: [FilterValues['players'], (l: Lang) => string][] = [
  ['all', (l) => l.t('Solo and multiplayer')],
  ['solo', (l) => l.t('Solo')],
  ['coop', (l) => l.t('Multiplayer')],
];
const EXPANSIONS: [FilterValues['expansions'], (l: Lang) => [option: string, clause: string]][] = [
  ['all', (l) => [l.t('With or without'), '']],
  ['without', (l) => [l.t('Without expansion mods'), l.t(', without expansion mods')]],
  ['with', (l) => [l.t('With expansion mods'), l.t(', with expansion mods')]],
];
const POOLS: Record<string, (l: Lang) => [option: string, clause: string]> = {
  all: (l) => [l.t('Any pool'), ''],
  full: (l) => [l.t('Full pool (every epoch)'), l.t(', with the full card pool')],
  partial: (l) => [l.t('Partly unlocked'), l.t(', with a partly unlocked pool')],
  starter: (l) => [l.t('Starting pool (no epochs)'), l.t(', with the starting pool')],
};
const MIN_RUNS = [1, 5, 10, 20, 50];

export function validFilters(filters: FilterValues, options: FilterOptions): FilterValues {
  const mode = options.modes.some(([known]) => known === filters.mode) ? filters.mode : DEFAULT_FILTERS.mode;
  const [latest] = options.versions;
  const since = filters.version.startsWith('>=') ? filters.version.slice(2) : null;
  const version =
    since === latest
      ? latest
      : ['all', 'recent'].includes(filters.version) || options.versions.includes(since ?? filters.version)
        ? filters.version
        : DEFAULT_FILTERS.version;
  return {
    ascension:
      filters.ascension === 'all' || Object.hasOwn(ASCENSION_BANDS, filters.ascension)
        ? filters.ascension
        : DEFAULT_FILTERS.ascension,
    build: validPatch(filters.build, mode, options),
    expansions: EXPANSIONS.some(([value]) => value === filters.expansions)
      ? filters.expansions
      : DEFAULT_FILTERS.expansions,
    min: MIN_RUNS.includes(filters.min) ? filters.min : DEFAULT_FILTERS.min,
    mode,
    players: PLAYERS.some(([players]) => players === filters.players) ? filters.players : DEFAULT_FILTERS.players,
    pool: Object.hasOwn(POOLS, filters.pool) ? filters.pool : DEFAULT_FILTERS.pool,
    version,
  };
}

function VersionRange({ filters, l, options }: { filters: FilterValues; l: Lang; options: FilterOptions }) {
  const choice = filters.version;
  const latest = options.versions[0];
  if (choice === 'all') return <>{l.t('every version')}</>;
  const from = choice === 'recent' ? recentStart(options, filters) : choice.replace(/^>=/, '');
  const link = (v: string) => <a href={l.href(notesHref(v))}>{v}</a>;
  return from !== latest && (choice === 'recent' || choice.startsWith('>=')) ? (
    <>{fillSlots(l.t('{from} to {to}'), { from: link(from), to: link(latest) })}</>
  ) : (
    link(from)
  );
}

interface Props {
  filters: FilterValues;
  l: Lang;
  onChange: (next: FilterValues) => void;
  options: FilterOptions;
  runs: number;
  status: Status;
  versions?: string;
}

export default function Filters({ filters, l, onChange, options, runs, status, versions }: Props) {
  const setFilter = (key: keyof FilterValues) => (e: Event) => {
    const value = (e.currentTarget as HTMLSelectElement).value;
    onChange({ ...filters, [key]: key === 'min' ? Number(value) : value });
  };
  const [latest, ...older] = options.versions;
  const recent = recentStart(options, filters);
  const defaultPatch = options.defaultPatches[filters.mode];
  const runsShown =
    filters.players === 'solo'
      ? l.n(runs, '{n} solo run', '{n} solo runs')
      : filters.players === 'coop'
        ? l.n(runs, '{n} multiplayer run', '{n} multiplayer runs')
        : l.n(runs, '{n} run', '{n} runs');
  const details = [
    filters.ascension === 'all'
      ? l.t(', at every ascension')
      : l.t(', at {band}', { band: l.t(ASCENSION_BANDS[Number(filters.ascension)]) }),
    POOLS[filters.pool](l)[1],
    EXPANSIONS.find(([value]) => value === filters.expansions)![1](l)[1],
    filters.build === 'all'
      ? ''
      : l.t(', on game build {build}', { build: filters.build === 'latest' ? defaultPatch : filters.build }),
    filters.min === DEFAULT_FILTERS.min
      ? ''
      : l.n(filters.min, ', hiding results with fewer than {n} run', ', hiding results with fewer than {n} runs'),
  ].join('');
  const moreFilters = useRef<HTMLDetailsElement>(null);
  const moreFiltersSet =
    filters.expansions !== DEFAULT_FILTERS.expansions ||
    filters.pool !== DEFAULT_FILTERS.pool ||
    filters.min !== DEFAULT_FILTERS.min;
  useEffect(() => {
    if (moreFiltersSet && moreFilters.current) moreFilters.current.open = true;
  }, [moreFiltersSet]);

  return (
    <div class="stats-filters">
      <form
        aria-label={l.t('Filter the runs')}
        class="needs-js flex flex-wrap items-end gap-3"
        onSubmit={(e) => e.preventDefault()}
      >
        <label class="field" hidden={Boolean(versions)}>
          <span>{l.t('Version')}</span>
          <select class="field-control" onChange={setFilter('version')} value={filters.version}>
            <option value="recent">
              {recent === latest
                ? l.t('Recent versions ({version})', { version: latest })
                : l.t('Recent versions ({version}+)', { version: recent })}
            </option>
            <option value={latest}>{l.t('Latest version ({version})', { version: latest })}</option>
            <option value="all">{l.t('All versions')}</option>
            <optgroup label={l.t('Since a version')}>
              {older.map((v) => (
                <option value={`>=${v}`}>{l.t('{version} and newer', { version: v })}</option>
              ))}
            </optgroup>
            <optgroup label={l.t('One version')}>
              {older.map((v) => (
                <option value={v}>{v}</option>
              ))}
            </optgroup>
          </select>
        </label>
        <label class="field">
          <span>{l.t('Game build')}</span>
          <select class="field-control" onChange={setFilter('build')} value={filters.build}>
            {options.patches.map(([patch, label]) => (
              <option value={patch === defaultPatch ? DEFAULT_FILTERS.build : patch}>{label}</option>
            ))}
            <option value="all">{l.t('All builds')}</option>
          </select>
        </label>
        <label class="field" hidden={options.modes.length < 2}>
          <span>{l.t('Game mode')}</span>
          <select class="field-control" onChange={setFilter('mode')} value={filters.mode}>
            {options.modes.map(([mode, label]) => (
              <option value={mode}>{label}</option>
            ))}
          </select>
        </label>
        <label class="field">
          <span>{l.t('Ascension')}</span>
          <select class="field-control" onChange={setFilter('ascension')} value={filters.ascension}>
            <option value="all">{l.t('All ascensions')}</option>
            {Object.entries(ASCENSION_BANDS).map(([band, name]) => (
              <option value={band}>{l.t(name)}</option>
            ))}
          </select>
        </label>
        <label class="field">
          <span>{l.t('Players')}</span>
          <select class="field-control" onChange={setFilter('players')} value={filters.players}>
            {PLAYERS.map(([value, name]) => (
              <option value={value}>{name(l)}</option>
            ))}
          </select>
        </label>
        <details class="more-filters" ref={moreFilters}>
          <summary>{l.t('More filters')}</summary>
          <div class="flex flex-wrap items-end gap-3">
            <label class="field">
              <span>{l.t('Expansion mods')}</span>
              <select class="field-control" onChange={setFilter('expansions')} value={filters.expansions}>
                {EXPANSIONS.map(([value, name]) => (
                  <option value={value}>{name(l)[0]}</option>
                ))}
              </select>
            </label>
            <label class="field">
              <span>{l.t('Card pool')}</span>
              <select class="field-control" onChange={setFilter('pool')} value={filters.pool}>
                {Object.entries(POOLS).map(([value, pool]) => (
                  <option value={value}>{pool(l)[0]}</option>
                ))}
              </select>
            </label>
            <label class="field">
              <span>{l.t('Hide results with fewer than')}</span>
              <select class="field-control" onChange={setFilter('min')} value={String(filters.min)}>
                {MIN_RUNS.map((n) => (
                  <option value={n}>{l.n(n, '{n} run', '{n} runs')}</option>
                ))}
              </select>
            </label>
          </div>
        </details>
      </form>
      <p aria-live="polite" class="m-0 text-sm text-ink-2">
        {fillSlots(
          versions ? l.t('Showing {runs} on {versions}{details}.') : l.t('Showing {runs} from {versions}{details}.'),
          {
            details,
            runs: <strong class="text-ink">{runsShown}</strong>,
            versions: versions ?? <VersionRange filters={filters} l={l} options={options} />,
          },
        )}
        {status === 'loading' && <span class="text-muted"> {l.t('Updating…')}</span>}
        {status === 'error' && (
          <span class="text-down"> {l.t('The data did not load. Check the connection and try again.')}</span>
        )}
      </p>
    </div>
  );
}
