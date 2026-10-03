import type { Lang } from '../../lib/lang';
import { opensInSheet } from '../../lib/links';
import { emptyText } from './empty';

export interface BarItem {
  dot?: string;
  fill?: string;
  hi?: number;
  href?: string;
  icon?: null | string;
  label: string;
  lo?: number;
  note?: null | string;
  ref?: null | number;
  text: string;
  textNote?: null | string;
  value: null | number;
}

interface Props {
  empty?: string;
  items: BarItem[];
  l: Lang;
  labelWidth?: string;
  limit?: number;
  max?: number;
  reference?: null | number;
  referenceLabel?: string;
}

export default function Bars({ empty, items, l, labelWidth, limit, max, reference, referenceLabel }: Props) {
  if (!items.length) return <p class="empty">{empty ?? emptyText(l)}</p>;
  const fullValue = max ?? (Math.max(...items.map((item) => item.value || 0)) || 1);
  const toPercent = (value: number) => `${Math.max(0, Math.min(1, value / fullValue)) * 100}%`;
  const referenceFor = (item: BarItem) => (item.ref === undefined ? reference : item.ref);
  const hasIcons = items.some((item) => item.icon);
  const labelColumnWidth = hasIcons ? `calc(${labelWidth ?? '12rem'} + 32px)` : labelWidth;
  const hiddenCount = limit && items.length > limit ? items.length - limit : 0;

  return (
    <div class="bars-wrap">
      <ol class="bars" style={labelColumnWidth ? { '--label': labelColumnWidth } : undefined}>
        {items.map((item, i) => {
          const rowReference = referenceFor(item);
          const labelContent = (
            <>
              {item.dot && <i class="dot" style={{ '--dot': item.dot }} />}
              {hasIcons && (item.icon ? <img alt="" class="bar-icon" src={item.icon} /> : <span class="bar-icon" />)}
              <span>
                {item.label}
                {item.note && <small>{item.note}</small>}
              </span>
            </>
          );
          return (
            <li class={limit && i >= limit ? 'bar extra' : 'bar'}>
              {item.href ? (
                <a class="bar-label" data-sheet-link={opensInSheet(item.href) || undefined} href={l.href(item.href)}>
                  {labelContent}
                </a>
              ) : (
                <span class="bar-label">{labelContent}</span>
              )}
              <span class="bar-value">
                {item.text}
                {item.textNote && <small>{item.textNote}</small>}
              </span>
              <span aria-hidden="true" class="bar-track">
                <span class="bar-fill" style={{ '--fill': item.fill, '--w': toPercent(item.value || 0) }} />
                {item.lo != null && item.hi != null && (
                  <span class="bar-range" style={{ '--hi': toPercent(item.hi), '--lo': toPercent(item.lo) }} />
                )}
                {rowReference != null && <span class="bar-ref" style={{ '--ref': toPercent(rowReference) }} />}
              </span>
            </li>
          );
        })}
      </ol>
      {referenceLabel && items.some((item) => referenceFor(item) != null) && <p class="ref-key">{referenceLabel}</p>}
      {hiddenCount > 0 && (
        <label class="show-more">
          <input class="sr-only" type="checkbox" />
          <span class="more">{l.t('Show all {count}', { count: l.num(items.length) })}</span>
          <span class="less">{l.t('Show fewer')}</span>
        </label>
      )}
    </div>
  );
}
