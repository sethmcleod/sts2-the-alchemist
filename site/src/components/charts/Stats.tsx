// A row of headline numbers, each with what it counts

import type { ComponentChildren } from 'preact';

export interface Stat {
  delta?: null | { text: string; up: boolean };
  label: ComponentChildren;
  note?: ComponentChildren;
  value: ComponentChildren;
}

export default function Stats({ items }: { items: Stat[] }) {
  return (
    <dl class="stats">
      {items.map((s) => (
        <div class="stat">
          <dt class="stat-label">{s.label}</dt>
          <dd class="stat-value">
            {s.value}
            {s.delta && <span class={s.delta.up ? 'delta up' : 'delta down'}> {s.delta.text}</span>}
          </dd>
          {s.note && <dd class="stat-note">{s.note}</dd>}
        </div>
      ))}
    </dl>
  );
}
