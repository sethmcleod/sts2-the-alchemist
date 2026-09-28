// One bar split into parts, with a legend that carries every value as text

import type { Lang } from '../../lib/lang';
import { emptyText } from './empty';

interface Part {
  label: string;
  value: number;
  fill: string;
}

export default function StackBar({ l, parts }: { l: Lang; parts: Part[] }) {
  const total = parts.reduce((n, p) => n + p.value, 0);
  if (!total) return <p class="empty">{emptyText(l)}</p>;
  return (
    <div>
      <div class="stack" aria-hidden="true">
        {parts.map((p) => (
          <span style={{ flexGrow: p.value, '--fill': p.fill }} />
        ))}
      </div>
      <ul class="legend">
        {parts.map((p) => (
          <li>
            <i class="dot" style={{ '--dot': p.fill }} />
            {l.t('{label}: {count} ({share})', {
              label: p.label,
              count: l.num(p.value),
              share: l.pct(p.value / total),
            })}
          </li>
        ))}
      </ul>
    </div>
  );
}
