import Columns from '../components/charts/Columns';
import DataTable, { type ColumnDef } from '../components/charts/DataTable';
import type { Lang, LangInit } from '../lib/lang';
import { fights, type EncounterRow, type FightsModel } from '../lib/views/fights';
import Filters, { type FilterOptions } from './Filters';
import { useLang } from './useLang';
import { useStats } from './useStats';
import SectionHeading from '../components/SectionHeading';

const encounterColumns = (l: Lang): ColumnDef<EncounterRow>[] => [
  {
    key: 'name',
    label: l.t('Fight'),
    render: (r) => (
      <>
        {r.name}
        <small>{l.n(r.fights, '{kind}, {n} fight', '{kind}, {n} fights', { kind: r.kind })}</small>
      </>
    ),
  },
  {
    key: 'deaths',
    label: l.t('Runs ended'),
    num: true,
    render: (r) => (
      <>
        {l.num(r.deaths)}
        <small>{l.t('{share} of fights', { share: l.pct(r.lethality) })}</small>
      </>
    ),
  },
  { key: 'lethality', label: l.t('Ends the run'), num: true, wide: true, render: (r) => l.pct(r.lethality) },
  { key: 'damage', label: l.t('Damage'), num: true, wide: true, render: (r) => l.fixed(r.damage, 1) },
  { key: 'turns', label: l.t('Turns'), num: true, wide: true, render: (r) => l.fixed(r.turns, 1) },
];

const actColumns = (l: Lang): ColumnDef<FightsModel['acts'][number]>[] => [
  { key: 'act', label: l.t('Act'), render: (r) => l.t('Act {act}', { act: r.act }) },
  { key: 'fights', label: l.t('Fights'), num: true, render: (r) => l.num(r.fights) },
  { key: 'turns', label: l.t('Turns'), num: true, render: (r) => l.fixed(r.turns, 1) },
  { key: 'damage', label: l.t('Damage'), num: true, render: (r) => l.fixed(r.damage, 1) },
];

interface Props {
  locale: LangInit;
  initial: FightsModel;
  options: FilterOptions;
}

export default function Fights({ locale, initial, options }: Props) {
  const l = useLang(locale);
  const {
    filters,
    model: m,
    status,
    update,
  } = useStats(initial, ['encounters'], options, (runs, f) => fights(l, runs, f), locale);
  return (
    <div class="stats-page" aria-busy={status === 'loading'}>
      <Filters l={l} filters={filters} options={options} runs={m.runs} status={status} onChange={update} />
      <section class="panel p-5">
        <SectionHeading id="where-runs-end">{l.t('Where runs end')}</SectionHeading>
        <p class="note">{l.t('Lost runs by the floor they ended on. The tall bars are usually the act bosses.')}</p>
        <Columns l={l} items={m.floors} label={l.t('Lost runs by the floor they ended on')} />
      </section>
      <section class="panel p-5">
        <SectionHeading id="toughest-fights">{l.t('Toughest fights')}</SectionHeading>
        <p class="note">{l.t('Fights by how many runs they ended. Damage and turns are averages per fight.')}</p>
        <DataTable
          l={l}
          columns={encounterColumns(l)}
          rows={m.encounters}
          sort={{ key: 'deaths', dir: -1 }}
          limit={15}
          caption={l.t('Fights')}
        />
      </section>
      <section class="panel p-5">
        <SectionHeading id="fights-by-act">{l.t('Fights by act')}</SectionHeading>
        <p class="note">{l.t('Average turns and damage taken per fight in each act.')}</p>
        <DataTable
          l={l}
          columns={actColumns(l)}
          rows={m.acts}
          sort={{ key: 'act', dir: 1 }}
          caption={l.t('Fights by act')}
        />
      </section>
    </div>
  );
}
