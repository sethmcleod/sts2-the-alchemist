import type { Column } from '../../components/charts/Columns';
import type { Lang } from '../lang';
import { ENCOUNTER_KINDS } from '../mod';
import type { Filters, Runs } from '../runs';
import { rate, sumBy } from '../stats';

export interface EncounterRow {
  damage: number;
  deaths: number;
  fights: number;
  id: string;
  kind: string;
  lethality: null | number;
  name: string;
  turns: number;
}

export interface FightsModel {
  acts: { act: number; damage: number; fights: number; turns: number }[];
  encounters: EncounterRow[];
  floors: Column[];
  runs: number;
}

export function fights(l: Lang, runs: Runs, filters: Filters): FightsModel {
  const on = runs.select(filters);
  const floors = sumBy(runs.tables.death_floors, on, (r) => r.floor as number);
  const lostRuns = [...floors.values()].reduce((n, c) => n + c.deaths, 0);
  const lastFloor = Math.max(0, ...floors.keys());
  const columns: Column[] = [];
  for (let floor = 1; floor <= lastFloor; floor++) {
    const ended = floors.get(floor)?.deaths || 0;
    columns.push({
      label: l.num(floor),
      tip: [
        l.t('Floor {floor}', { floor }),
        l.n(ended, '{n} run ended here', '{n} runs ended here'),
        l.t('{share} of lost runs', { share: l.pct(ended / lostRuns) }),
      ],
      value: ended,
    });
  }

  const encounter = (id: string) => {
    const suffix = id.split('_').at(-1)!;
    const kind = ENCOUNTER_KINDS[suffix];
    return {
      kind: kind ? (l.game?.words[kind] ?? l.t(kind)) : l.t('Fight'),
      name: l.game?.encounters[id] ?? runs.name(kind ? id.slice(0, -suffix.length - 1) : id),
    };
  };

  return {
    acts: [...sumBy(runs.tables.acts, on, (r) => r.act as number)]
      .filter(([act, g]) => act > 0 && g.fights)
      .sort((a, b) => a[0] - b[0])
      .map(([act, g]) => ({ act, damage: g.damage / g.fights, fights: g.fights, turns: g.turns / g.fights })),
    encounters: [...sumBy(runs.table('encounters'), on, (r) => r.encounter as string)]
      .filter(([, g]) => g.fights >= filters.min)
      .map(([id, g]) => ({
        id,
        ...encounter(id),
        damage: g.damage / g.fights,
        deaths: g.deaths,
        fights: g.fights,
        lethality: rate(g.deaths, g.fights),
        turns: g.turns / g.fights,
      })),
    floors: columns,
    runs: runs.totals(on).runs,
  };
}
