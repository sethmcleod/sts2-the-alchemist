import { useEffect, useRef } from 'preact/hooks';
import DataTable, { type ColumnDef } from '../components/charts/DataTable';
import Stats from '../components/charts/Stats';
import SectionHeading from '../components/SectionHeading';
import type { Lang, LangInit } from '../lib/lang';
import { cardHref, notesHref } from '../lib/links';
import { rarityName } from '../lib/views/cards';
import { type CardChange, versions, type VersionsModel } from '../lib/views/versions';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';

const columns = (l: Lang): ColumnDef<CardChange>[] => [
  {
    key: 'name',
    label: l.t('Card'),
    render: (r) => (
      <>
        <a data-sheet-link href={l.href(cardHref(r.id))}>
          {r.name}
        </a>
        <small>
          {l.t('{rarity}, won {from} → {to}', {
            from: l.pct(r.winA),
            rarity: rarityName(l, r.rarity),
            to: l.pct(r.winB),
          })}
        </small>
      </>
    ),
  },
  {
    key: 'zScore',
    label: l.t('Against its peers'),
    num: true,
    render: (r) => (
      <>
        {l.pointChange(r.relative)}
        {r.likely && <small class="text-gold">{l.t('likely')}</small>}
      </>
    ),
  },
  { key: 'pickDelta', label: l.t('Pick rate'), num: true, render: (r) => l.pointChange(r.pickDelta), wide: true },
];

interface Props {
  initial: VersionsModel;
  locale: LangInit;
  options: FilterOptions;
}

export default function Versions({ initial, locale, options }: Props) {
  const l = useLang(locale);
  const pair = useRef({ a: initial.a, b: initial.b });
  const { filters, model, refresh, status, update } = useStats(
    initial,
    ['cards'],
    options,
    (runs, f) => versions(l, runs, f, pair.current.a, pair.current.b),
    locale,
  );
  const pick = (side: 'a' | 'b') => (e: Event) => {
    pair.current = { ...pair.current, [side]: (e.currentTarget as HTMLSelectElement).value };
    if (!history.state?.sheet) {
      const params = new URLSearchParams(location.search);
      params.set('a', pair.current.a);
      params.set('b', pair.current.b);
      history.replaceState(history.state, '', `${location.pathname}?${params}${location.hash}`);
    }
    refresh();
  };

  useEffect(() => {
    const params = new URL(history.state?.page ?? location.href).searchParams;
    const known = (v: null | string) => (v && options.versions.includes(v) ? v : null);
    const [a, b] = [known(params.get('a')), known(params.get('b'))];
    if ((a && a !== pair.current.a) || (b && b !== pair.current.b)) {
      pair.current = { a: a ?? pair.current.a, b: b ?? pair.current.b };
      refresh();
    }
  }, []);

  return (
    <div aria-busy={status === 'loading'} class="stats-page">
      <Filters
        filters={filters}
        l={l}
        onChange={update}
        options={options}
        runs={model.runs}
        status={status}
        versions={l.list([model.a, model.b])}
      />
      <section class="panel p-5">
        <SectionHeading id="compare-two-versions">{l.t('Compare two versions')}</SectionHeading>
        <p class="note">{l.t('The filters above apply to both sides.')}</p>
        <form class="needs-js mb-4 flex flex-wrap gap-3" onSubmit={(e) => e.preventDefault()}>
          {(['a', 'b'] as const).map((side) => (
            <label class="field">
              <span>{side === 'a' ? l.t('From') : l.t('To')}</span>
              <select class="field-control" onChange={pick(side)} value={pair.current[side]}>
                {options.versions.map((v) => (
                  <option value={v}>{v}</option>
                ))}
              </select>
            </label>
          ))}
        </form>
        {model.a === model.b ? (
          <p class="empty">{l.t('Pick two different versions.')}</p>
        ) : (
          <>
            <p class="note">
              <a href={l.href(notesHref(model.a))}>{l.t('{version} notes', { version: model.a })}</a> ·{' '}
              <a href={l.href(notesHref(model.b))}>{l.t('{version} notes', { version: model.b })}</a>
            </p>
            <Stats items={model.stats} />
          </>
        )}
      </section>
      <section class="panel p-5">
        <SectionHeading id="cards-that-moved-the-most">{l.t('Cards that moved the most')}</SectionHeading>
        <p class="note">
          {l.n(
            model.minRuns,
            'Cards with at least {n} run in both versions, the clearest changes first. A change is in percentage points, measured against the middle card of the same rarity, so a version that is harder overall does not drag every card down. A change marked {likely} is too big to be random swing alone.',
            'Cards with at least {n} runs in both versions, the clearest changes first. A change is in percentage points, measured against the middle card of the same rarity, so a version that is harder overall does not drag every card down. A change marked {likely} is too big to be random swing alone.',
            { likely: l.t('likely') },
          )}
        </p>
        <DataTable
          caption={l.t('Card win rate changes between the two versions')}
          columns={columns(l)}
          empty={l.n(
            model.minRuns,
            'No card has {n} run in both versions yet.',
            'No card has {n} runs in both versions yet.',
          )}
          l={l}
          limit={15}
          rows={model.a === model.b ? [] : model.changes}
          sort={{ dir: -1, key: 'zScore' }}
        />
      </section>
    </div>
  );
}
