import type { Column } from '../../components/charts/Columns';
import type { Lang } from '../lang';
import { ENCOUNTER_KINDS } from '../mod';
import type { Filters, Runs } from '../runs';
import { rate, sumBy } from '../stats';

export interface EncounterRow {
  id: string;
  name: string;
  kind: string;
  fights: number;
  deaths: number;
  lethality: number | null;
  damage: number;
  turns: number;
}

export interface FightsModel {
  runs: number;
  floors: Column[];
  encounters: EncounterRow[];
  acts: { act: number; fights: number; turns: number; damage: number }[];
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
      value: n,
      tip: [
        l.t('Floor {floor}', { floor }),
        l.n(n, '{n} run ended here', '{n} runs ended here'),
        l.t('{share} of lost runs', { share: l.pct(n / deaths) }),
      ],
    });
  }

  // "KNOWLEDGE_DEMON_BOSS" is the Knowledge Demon, a boss. The game names its own fights
  const encounter = (id: string) => {
    const suffix = id.split('_').at(-1)!;
    const kind = ENCOUNTER_KINDS[suffix];
    return {
      name: l.game?.encounters[id] ?? runs.name(kind ? id.slice(0, -suffix.length - 1) : id),
      kind: kind ? (l.game?.words[kind] ?? l.t(kind)) : l.t('Fight'),
    };
  };

  return {
    runs: runs.totals(on).runs,
    floors: columns,
    encounters: [...sumBy(runs.table('encounters'), on, (r) => r.encounter as string)]
      .filter(([, g]) => g.fights >= f.min)
      .map(([id, g]) => ({
        id,
        ...encounter(id),
        fights: g.fights,
        deaths: g.deaths,
        lethality: rate(g.deaths, g.fights),
        damage: g.damage / g.fights,
        turns: g.turns / g.fights,
      })),
    acts: [...sumBy(runs.tables.acts, on, (r) => r.act as number)]
      .filter(([act, g]) => act > 0 && g.fights)
      .sort((a, b) => a[0] - b[0])
      .map(([act, g]) => ({ act, fights: g.fights, turns: g.turns / g.fights, damage: g.damage / g.fights })),
  };
}
