// A labelled horizontal bar per item, its value as text beside it. Rows past `limit` wait behind a
// "Show all" box, which works without a script.

import type { Lang } from '../../lib/lang';
import { opensInSheet } from '../../lib/links';
import { emptyText } from './empty';

export interface BarItem {
  label: string;
  href?: string;
  note?: string | null;
  value: number | null;
  text: string;
  textNote?: string | null;
  /** The likely range of a rate */
  lo?: number;
  hi?: number;
  /** This row's own reference mark, which wins over the list's */
  ref?: number | null;
  dot?: string;
  icon?: string | null;
  fill?: string;
}

interface Props {
  l: Lang;
  items: BarItem[];
  /** The value of a full bar, 1 for a rate. Defaults to the largest value */
  max?: number;
  reference?: number | null;
  referenceLabel?: string;
  limit?: number;
  labelWidth?: string;
  empty?: string;
}

export default function Bars({ l, items, max, reference, referenceLabel, limit, labelWidth, empty }: Props) {
  if (!items.length) return <p class="empty">{empty ?? emptyText(l)}</p>;
  const full = max ?? (Math.max(...items.map((i) => i.value || 0)) || 1);
  const at = (v: number) => `${Math.max(0, Math.min(1, v / full)) * 100}%`;
  const refOf = (item: BarItem) => (item.ref === undefined ? reference : item.ref);
  const icons = items.some((item) => item.icon);
  const width = icons ? `calc(${labelWidth ?? '12rem'} + 32px)` : labelWidth;
  const extra = limit && items.length > limit ? items.length - limit : 0;

  return (
    <div class="bars-wrap">
      <ol class="bars" style={width ? { '--label': width } : undefined}>
        {items.map((item, i) => {
          const ref = refOf(item);
          const label = (
            <>
              {item.dot && <i class="dot" style={{ '--dot': item.dot }} />}
              {icons && (item.icon ? <img class="bar-icon" src={item.icon} alt="" /> : <span class="bar-icon" />)}
              <span>
                {item.label}
                {item.note && <small>{item.note}</small>}
              </span>
            </>
          );
          return (
            <li class={limit && i >= limit ? 'bar extra' : 'bar'}>
              {item.href ? (
                <a class="bar-label" href={l.href(item.href)} data-sheet-link={opensInSheet(item.href) || undefined}>
                  {label}
                </a>
              ) : (
                <span class="bar-label">{label}</span>
              )}
              <span class="bar-value">
                {item.text}
                {item.textNote && <small>{item.textNote}</small>}
              </span>
              <span class="bar-track" aria-hidden="true">
                <span class="bar-fill" style={{ '--w': at(item.value || 0), '--fill': item.fill }} />
                {item.lo != null && item.hi != null && (
                  <span class="bar-range" style={{ '--lo': at(item.lo), '--hi': at(item.hi) }} />
                )}
                {ref != null && <span class="bar-ref" style={{ '--ref': at(ref) }} />}
              </span>
            </li>
          );
        })}
      </ol>
      {referenceLabel && items.some((item) => refOf(item) != null) && <p class="ref-key">{referenceLabel}</p>}
      {extra > 0 && (
        <label class="show-more">
          <input type="checkbox" class="sr-only" />
          <span class="more">{l.t('Show all {count}', { count: l.num(items.length) })}</span>
          <span class="less">{l.t('Show fewer')}</span>
        </label>
      )}
    </div>
  );
}
