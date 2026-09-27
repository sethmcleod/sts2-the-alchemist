import { useEffect, useRef } from 'preact/hooks';
import DataTable, { type ColumnDef } from '../components/charts/DataTable';
import Stats from '../components/charts/Stats';
import type { Lang, LangInit } from '../lib/lang';
import { cardHref, notesHref } from '../lib/links';
import { rarityName } from '../lib/views/cards';
import { versions, type CardChange, type VersionsModel } from '../lib/views/versions';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';

const columns = (l: Lang): ColumnDef<CardChange>[] => [
  {
    key: 'name',
    label: l.t('Card'),
    render: (r) => (
      <>
        <a href={l.href(cardHref(r.id))} data-sheet-link>
          {r.name}
        </a>
        <small>
          {l.t('{rarity}, won {from} → {to}', {
            rarity: rarityName(l, r.rarity),
            from: l.pct(r.winA),
            to: l.pct(r.winB),
          })}
        </small>
      </>
    ),
  },
  {
    key: 'confidence',
    label: l.t('Against its peers'),
    num: true,
    render: (r) => (
      <>
        {l.change(r.relative)}
        {r.likely && <small class="text-gold">{l.t('likely')}</small>}
      </>
    ),
  },
  { key: 'pickDelta', label: l.t('Pick rate'), num: true, wide: true, render: (r) => l.change(r.pickDelta) },
];

interface Props {
  locale: LangInit;
  initial: VersionsModel;
  options: FilterOptions;
}

export default function Versions({ locale, initial, options }: Props) {
  const l = useLang(locale);
  const pair = useRef({ a: initial.a, b: initial.b });
  const {
    filters,
    model: m,
    status,
    update,
    refresh,
  } = useStats(initial, ['cards'], options, (runs, f) => versions(l, runs, f, pair.current.a, pair.current.b), locale);
  const pick = (side: 'a' | 'b') => (e: Event) => {
    pair.current = { ...pair.current, [side]: (e.currentTarget as HTMLSelectElement).value };
    // The pair lives in the URL next to the filters, so a shared link opens on the same comparison
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
    const known = (v: string | null) => (v && options.versions.includes(v) ? v : null);
    const [a, b] = [known(params.get('a')), known(params.get('b'))];
    if ((a && a !== pair.current.a) || (b && b !== pair.current.b)) {
      pair.current = { a: a ?? pair.current.a, b: b ?? pair.current.b };
      refresh();
    }
  }, []);

  return (
    <div class="stats-page" aria-busy={status === 'loading'}>
      <Filters
        l={l}
        filters={filters}
        options={options}
        runs={m.runs}
        status={status}
        onChange={update}
        versions={l.list([m.a, m.b])}
      />
      <section class="panel p-5">
        <h2>{l.t('Compare two versions')}</h2>
        <p class="note">{l.t('The filters above apply to both sides.')}</p>
        <form class="needs-js mb-4 flex flex-wrap gap-3" onSubmit={(e) => e.preventDefault()}>
          {(['a', 'b'] as const).map((side) => (
            <label class="field">
              <span>{side === 'a' ? l.t('From') : l.t('To')}</span>
              <select class="field-control" value={pair.current[side]} onChange={pick(side)}>
                {options.versions.map((v) => (
                  <option value={v}>{v}</option>
                ))}
              </select>
            </label>
          ))}
        </form>
        {m.a === m.b ? (
          <p class="empty">{l.t('Pick two different versions.')}</p>
        ) : (
          <>
            <p class="note">
              <a href={l.href(notesHref(m.a))}>{l.t('{version} notes', { version: m.a })}</a> ·{' '}
              <a href={l.href(notesHref(m.b))}>{l.t('{version} notes', { version: m.b })}</a>
            </p>
            <Stats items={m.stats} />
          </>
        )}
      </section>
      <section class="panel p-5">
        <h2>{l.t('Cards that moved the most')}</h2>
        <p class="note">
          {l.n(
            m.enough,
            'Cards with at least {n} run in both versions, the clearest changes first. A change is in percentage points, measured against the middle card of the same rarity, so a version that is harder overall does not drag every card down. A change marked {likely} is too big to be random swing alone.',
            'Cards with at least {n} runs in both versions, the clearest changes first. A change is in percentage points, measured against the middle card of the same rarity, so a version that is harder overall does not drag every card down. A change marked {likely} is too big to be random swing alone.',
            { likely: l.t('likely') },
          )}
        </p>
        <DataTable
          l={l}
          columns={columns(l)}
          rows={m.a === m.b ? [] : m.changes}
          sort={{ key: 'confidence', dir: -1 }}
          limit={15}
          caption={l.t('Card win rate changes between the two versions')}
          empty={l.n(
            m.enough,
            'No card has {n} run in both versions yet.',
            'No card has {n} runs in both versions yet.',
          )}
        />
      </section>
    </div>
  );
}
