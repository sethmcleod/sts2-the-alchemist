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

export function fights(l: Lang, runs: Runs, f: Filters): FightsModel {
  const on = runs.select(f);
  const floors = sumBy(runs.tables.death_floors, on, (r) => r.floor as number);
  const deaths = [...floors.values()].reduce((n, c) => n + c.deaths, 0);
  const top = Math.max(0, ...floors.keys());
  const columns: Column[] = [];
  for (let floor = 1; floor <= top; floor++) {
    const n = floors.get(floor)?.deaths || 0;
    columns.push({
      label: l.num(floor),
      tip: [
        l.t('Floor {floor}', { floor }),
        l.n(n, '{n} run ended here', '{n} runs ended here'),
        l.t('{share} of lost runs', { share: l.pct(n / deaths) }),
      ],
      value: n,
    });
  }

  // "KNOWLEDGE_DEMON_BOSS" is the Knowledge Demon, a boss. The game names its own fights
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
      .filter(([, g]) => g.fights >= f.min)
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
