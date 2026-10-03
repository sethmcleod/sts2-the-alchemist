import type { Lang } from '../../lib/lang';
import { emptyText } from './empty';

interface Part {
  fill: string;
  label: string;
  value: number;
}

export default function StackBar({ l, parts }: { l: Lang; parts: Part[] }) {
  const total = parts.reduce((sum, part) => sum + part.value, 0);
  if (!total) return <p class="empty">{emptyText(l)}</p>;
  return (
    <div>
      <div aria-hidden="true" class="stack">
        {parts.map((part) => (
          <span style={{ '--fill': part.fill, flexGrow: part.value }} />
        ))}
      </div>
      <ul class="legend">
        {parts.map((part) => (
          <li>
            <i class="dot" style={{ '--dot': part.fill }} />
            {l.t('{label}: {count} ({share})', {
              count: l.num(part.value),
              label: part.label,
              share: l.pct(part.value / total),
            })}
          </li>
        ))}
      </ul>
    </div>
  );
}
