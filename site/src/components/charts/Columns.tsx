import { useEffect, useState } from 'preact/hooks';
import type { Lang } from '../../lib/lang';
import { emptyText } from './empty';

export interface Column {
  label: string;
  tip: string[];
  value: number;
}

interface Props {
  empty?: string;
  items: Column[];
  l: Lang;
  label: string;
  markers?: { before: number; label: string }[];
}

function niceMax(value: number) {
  if (value <= 0) return 1;
  const powerOfTen = 10 ** Math.floor(Math.log10(value));
  for (const multiple of [1, 2, 2.5, 5, 10]) if (multiple * powerOfTen >= value) return multiple * powerOfTen;
  return 10 * powerOfTen;
}

const labelEvery = (columnCount: number, longestLabel: number) =>
  Math.max(1, Math.ceil((columnCount * longestLabel) / 48));

const tickText = (l: Lang, value: number) => (Number.isInteger(value) ? l.num(value) : l.fixed(value, 1));

export default function Columns({ empty, items, l, label, markers = [] }: Props) {
  const [active, setActive] = useState<null | number>(null);
  useEffect(() => setActive(null), [items]);
  if (!items.some((item) => item.value)) return <p class="empty">{empty ?? emptyText(l)}</p>;
  const max = niceMax(Math.max(...items.map((item) => item.value)));
  const showLabelEvery = labelEvery(items.length, Math.max(...items.map((item) => item.label.length)));
  const selectColumn = (e: PointerEvent) => {
    const index = (e.target as HTMLElement).closest<HTMLElement>('[data-index]')?.dataset.index;
    if (index) setActive(Number(index));
  };

  return (
    <figure class="columns" style={{ '--n': items.length }}>
      <div aria-hidden="true" class="columns-axis">
        {[1, 0.5, 0].map((fraction) => (
          <span style={{ '--at': `${(1 - fraction) * 100}%` }}>{tickText(l, fraction * max)}</span>
        ))}
      </div>
      <ol aria-label={label} class="columns-plot" onPointerDown={selectColumn} onPointerOver={selectColumn}>
        {items.map((item, i) => (
          <li class={i === active ? 'column active' : 'column'} data-index={i} title={item.tip.join('\n')}>
            <span class="column-bar" style={{ '--h': `${(item.value / max) * 100}%` }} />
            <span class="sr-only">{item.tip.join(', ')}</span>
          </li>
        ))}
        {markers.map((marker, i) => (
          <li
            aria-hidden="true"
            class="column-marker"
            style={{ '--at': `${(marker.before / items.length) * 100}%`, '--row': i % 2 }}
          >
            <span>{marker.label}</span>
          </li>
        ))}
      </ol>
      <div aria-hidden="true" class="columns-labels">
        {items.map((item, i) => (
          <span>{i % showLabelEvery === 0 ? item.label : ''}</span>
        ))}
      </div>
      <p aria-hidden="true" class="columns-readout">
        {items[active ?? -1]?.tip.join(' · ')}
      </p>
    </figure>
  );
}
