// A column per item, for counts along an ordered axis: a histogram, or deaths by floor. Drawn in
// HTML so it fits any width without measuring it. Pointing at or tapping a column writes its
// numbers under the chart, and each column carries them as text for a screen reader.

import { useEffect, useState } from 'preact/hooks';
import type { Lang } from '../../lib/lang';
import { emptyText } from './empty';

export interface Column {
  label: string;
  /** The numbers behind the column, one line per entry */
  tip: string[];
  value: number;
}

interface Props {
  empty?: string;
  items: Column[];
  l: Lang;
  label: string;
  /** Vertical lines before a column, such as where a badge tier starts */
  markers?: { before: number; label: string }[];
}

function niceMax(v: number) {
  if (v <= 0) return 1;
  const step = 10 ** Math.floor(Math.log10(v));
  for (const m of [1, 2, 2.5, 5, 10]) if (m * step >= v) return m * step;
  return 10 * step;
}

// About how many characters of axis label fit per column before they collide
const labelEvery = (count: number, longest: number) => Math.max(1, Math.ceil((count * longest) / 48));

const tick = (l: Lang, v: number) => (Number.isInteger(v) ? l.num(v) : l.fixed(v, 1));

export default function Columns({ empty, items, l, label, markers = [] }: Props) {
  const [active, setActive] = useState<null | number>(null);
  // New filters bring new columns, so the one pointed at before means nothing now
  useEffect(() => setActive(null), [items]);
  if (!items.some((i) => i.value)) return <p class="empty">{empty ?? emptyText(l)}</p>;
  const max = niceMax(Math.max(...items.map((i) => i.value)));
  const every = labelEvery(items.length, Math.max(...items.map((i) => i.label.length)));
  const pick = (e: PointerEvent) => {
    const index = (e.target as HTMLElement).closest<HTMLElement>('[data-index]')?.dataset.index;
    if (index) setActive(Number(index));
  };

  return (
    <figure class="columns" style={{ '--n': items.length }}>
      <div aria-hidden="true" class="columns-axis">
        {[1, 0.5, 0].map((t) => (
          <span style={{ '--at': `${(1 - t) * 100}%` }}>{tick(l, t * max)}</span>
        ))}
      </div>
      <ol aria-label={label} class="columns-plot" onPointerDown={pick} onPointerOver={pick}>
        {items.map((item, i) => (
          <li class={i === active ? 'column active' : 'column'} data-index={i} title={item.tip.join('\n')}>
            <span class="column-bar" style={{ '--h': `${(item.value / max) * 100}%` }} />
            <span class="sr-only">{item.tip.join(', ')}</span>
          </li>
        ))}
        {markers.map((m, i) => (
          <li
            aria-hidden="true"
            class="column-marker"
            style={{ '--at': `${(m.before / items.length) * 100}%`, '--row': i % 2 }}
          >
            <span>{m.label}</span>
          </li>
        ))}
      </ol>
      <div aria-hidden="true" class="columns-labels">
        {items.map((item, i) => (
          <span>{i % every === 0 ? item.label : ''}</span>
        ))}
      </div>
      <p aria-hidden="true" class="columns-readout">
        {items[active ?? -1]?.tip.join(' · ')}
      </p>
    </figure>
  );
}
