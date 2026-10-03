import { useEffect, useRef } from 'preact/hooks';
import type { Lang } from '../lib/lang';
import { notesHref } from '../lib/links';
import { ASCENSION_BANDS } from '../lib/mod';
import { DEFAULT_FILTERS, type Filters as FilterValues, type Runs } from '../lib/runs';
import { fillSlots } from './useLang';
import type { Status } from './useStats';

export interface FilterOptions {
  builds: [build: string, label: string][];
  recentStart: string;
  versions: string[];
}

const BRANCH_LABELS: Record<string, (l: Lang, build: string) => string> = {
  public: (l, build) => l.t('{build} (public)', { build }),
  'public-beta': (l, build) => l.t('{build} (beta)', { build }),
};

export function filterOptions(l: Lang, runs: Runs): FilterOptions {
  return {
    builds: Object.entries(runs.meta.builds)
      .reverse()
      .map(([build, branch]) => [build, BRANCH_LABELS[branch]?.(l, build) ?? build]),
    recentStart: runs.recentStart,
    versions: [...runs.meta.versions].reverse(),
  };
}

const PLAYERS: [FilterValues['players'], (l: Lang) => string][] = [
  ['solo', (l) => l.t('Solo')],
  ['coop', (l) => l.t('Multiplayer')],
  ['all', (l) => l.t('Solo and multiplayer')],
];
const POOLS: Record<string, (l: Lang) => [option: string, clause: string]> = {
  all: (l) => [l.t('Any pool'), ''],
  full: (l) => [l.t('Full pool (every epoch)'), l.t(', with the full card pool')],
  partial: (l) => [l.t('Partly unlocked'), l.t(', with a partly unlocked pool')],
  starter: (l) => [l.t('Starting pool (no epochs)'), l.t(', with the starting pool')],
};
const MIN_RUNS = [1, 5, 10, 20, 50];

export function validFilters(filters: FilterValues, options: FilterOptions): FilterValues {
  const defaults = DEFAULT_FILTERS;
  const [latest] = options.versions;
  const since = filters.version.startsWith('>=') ? filters.version.slice(2) : null;
  const version =
    since === latest
      ? latest
      : ['all', 'recent'].includes(filters.version) || options.versions.includes(since ?? filters.version)
        ? filters.version
        : defaults.version;
  return {
    ascension:
      filters.ascension === 'all' || Object.hasOwn(ASCENSION_BANDS, filters.ascension)
        ? filters.ascension
        : defaults.ascension,
    build:
      filters.build === 'all' || options.builds.some(([build]) => build === filters.build)
        ? filters.build
        : defaults.build,
    min: MIN_RUNS.includes(filters.min) ? filters.min : defaults.min,
    players: PLAYERS.some(([players]) => players === filters.players) ? filters.players : defaults.players,
    pool: Object.hasOwn(POOLS, filters.pool) ? filters.pool : defaults.pool,
    version,
  };
}

function VersionRange({ choice, l, options }: { choice: string; l: Lang; options: FilterOptions }) {
  const latest = options.versions[0];
  if (choice === 'all') return <>{l.t('every version')}</>;
  const from = choice === 'recent' ? options.recentStart : choice.replace(/^>=/, '');
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
    filters.build === 'all' ? '' : l.t(', on game build {build}', { build: filters.build }),
    filters.min === DEFAULT_FILTERS.min
      ? ''
      : l.n(filters.min, ', hiding results with fewer than {n} run', ', hiding results with fewer than {n} runs'),
  ].join('');
  const moreFilters = useRef<HTMLDetailsElement>(null);
  const moreFiltersSet =
    filters.pool !== DEFAULT_FILTERS.pool ||
    filters.build !== DEFAULT_FILTERS.build ||
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
              {options.recentStart === latest
                ? l.t('Recent versions ({version})', { version: latest })
                : l.t('Recent versions ({version}+)', { version: options.recentStart })}
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
              <span>{l.t('Card pool')}</span>
              <select class="field-control" onChange={setFilter('pool')} value={filters.pool}>
                {Object.entries(POOLS).map(([value, pool]) => (
                  <option value={value}>{pool(l)[0]}</option>
                ))}
              </select>
            </label>
            <label class="field">
              <span>{l.t('Game build')}</span>
              <select class="field-control" onChange={setFilter('build')} value={filters.build}>
                <option value="all">{l.t('All builds')}</option>
                {options.builds.map(([build, label]) => (
                  <option value={build}>{label}</option>
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
            versions: versions ?? <VersionRange choice={filters.version} l={l} options={options} />,
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
