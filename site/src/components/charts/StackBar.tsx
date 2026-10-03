// One bar split into parts, with a legend that carries every value as text

import type { Lang } from '../../lib/lang';
import { emptyText } from './empty';

interface Part {
  fill: string;
  label: string;
  value: number;
}

export default function StackBar({ l, parts }: { l: Lang; parts: Part[] }) {
  const total = parts.reduce((n, p) => n + p.value, 0);
  if (!total) return <p class="empty">{emptyText(l)}</p>;
  return (
    <div>
      <div aria-hidden="true" class="stack">
        {parts.map((p) => (
          <span style={{ '--fill': p.fill, flexGrow: p.value }} />
        ))}
      </div>
      <ul class="legend">
        {parts.map((p) => (
          <li>
            <i class="dot" style={{ '--dot': p.fill }} />
            {l.t('{label}: {count} ({share})', {
              count: l.num(p.value),
              label: p.label,
              share: l.pct(p.value / total),
            })}
          </li>
        ))}
      </ul>
    </div>
  );
}
