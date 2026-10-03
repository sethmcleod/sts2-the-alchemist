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
      {items.map((stat) => (
        <div class="stat">
          <dt class="stat-label">{stat.label}</dt>
          <dd class="stat-value">
            {stat.value}
            {stat.delta && <span class={stat.delta.up ? 'delta up' : 'delta down'}> {stat.delta.text}</span>}
          </dd>
          {stat.note && <dd class="stat-note">{stat.note}</dd>}
        </div>
      ))}
    </dl>
  );
}
