// One dot per item on two rate axes, placed by percentage so it fits any width. A dot links to
// its page, and pointing at it shows its numbers. The dots are for pointing; the card table carries
// the same numbers for everyone else.

import { useEffect, useState } from 'preact/hooks';
import type { Lang } from '../../lib/lang';
import { opensInSheet } from '../../lib/links';
import { emptyText } from './empty';

export interface Point {
  x: number;
  y: number;
  /** Radius in pixels */
  r: number;
  fill: string;
  label: string;
  href: string;
  tip: string[];
  /** Heavier dots get their name written first */
  weight: number;
}

interface Props {
  l: Lang;
  points: Point[];
  xLabel: string;
  label: string;
  reference?: number | null;
}

const LETTER = 6.7;
// Chinese, Japanese and Korean characters are about twice as wide
const labelWidth = (label: string) =>
  [...label].reduce((w, ch) => w + (/[\u1100-\u9fff\uac00-\ud7af\uff00-\uffef]/.test(ch) ? 2 : 1) * LETTER, 0);
const LINE = 14;

// Names the heaviest dots first. A label tries four spots around its dot and takes the first one
// that overlaps no dot and no earlier label, at a nominal plot size. Crowded dots stay unnamed
function placeLabels(points: (Point & { px: number; py: number })[], width: number, height: number, count: number) {
  const at = (p: { px: number; py: number }) => ({ x: (p.px / 100) * width, y: (p.py / 100) * height });
  const boxes = points.map((p) => ({ x: at(p).x - p.r, y: at(p).y - p.r, w: p.r * 2, h: p.r * 2 }));
  const clear = (b: (typeof boxes)[number]) =>
    b.x >= 0 &&
    b.x + b.w <= width &&
    b.y >= 0 &&
    b.y + b.h <= height &&
    !boxes.some((o) => b.x < o.x + o.w + 2 && b.x + b.w + 2 > o.x && b.y < o.y + o.h + 2 && b.y + b.h + 2 > o.y);
  const placed = [];
  for (const p of [...points].sort((a, b) => b.weight - a.weight).slice(0, count)) {
    const w = labelWidth(p.label);
    const { x, y } = at(p);
    const spots: [number, number][] = [
      [p.r + 4, -LINE / 2],
      [-p.r - 4 - w, -LINE / 2],
      [-w / 2, -p.r - LINE - 2],
      [-w / 2, p.r + 2],
    ];
    for (const [dx, dy] of spots) {
      const box = { x: x + dx, y: y + dy, w, h: LINE };
      if (!clear(box)) continue;
      boxes.push(box);
      placed.push({ label: p.label, left: p.px, top: p.py, dx, dy });
      break;
    }
  }
  return placed;
}

const step = (span: number) => (span > 0.5 ? 0.2 : 0.1);

export default function Scatter({ l, points, xLabel, label, reference }: Props) {
  const [active, setActive] = useState<number | null>(null);
  // Until the island runs, the browser's own tooltip carries the numbers
  const [live, setLive] = useState(false);
  useEffect(() => setLive(true), []);
  // New filters bring new dots, so the one pointed at before means nothing now
  useEffect(() => setActive(null), [points]);
  if (!points.length) return <p class="empty">{emptyText(l)}</p>;
  const ys = points.map((p) => p.y).concat(reference ?? []);
  const xMax = Math.min(1, Math.ceil((Math.max(...points.map((p) => p.x)) + 0.02) * 10) / 10);
  const yMin = Math.max(0, Math.floor((Math.min(...ys) - 0.02) * 10) / 10);
  const yMax = Math.min(1, Math.ceil((Math.max(...ys) + 0.02) * 10) / 10);
  const left = (x: number) => (x / xMax) * 100;
  const top = (y: number) => (1 - (y - yMin) / (yMax - yMin)) * 100;
  const placed = points.map((p) => ({ ...p, px: left(p.x), py: top(p.y) }));
  const ticks = (from: number, to: number) => {
    const out = [];
    for (let v = from; v <= to + 1e-9; v += step(to - from)) out.push(Math.round(v * 100) / 100);
    return out;
  };
  // A tap opens the dot's page, so only a mouse or a pen shows the tooltip
  const point = (e: PointerEvent) => {
    if (e.pointerType === 'touch') return;
    const index = (e.target as HTMLElement).closest<HTMLElement>('[data-index]')?.dataset.index;
    setActive(index ? Number(index) : null);
  };
  const tip = active === null ? undefined : placed[active];

  return (
    <figure class="scatter" aria-label={label}>
      <div class="scatter-plot" onPointerOver={point} onPointerLeave={() => setActive(null)}>
        {ticks(yMin, yMax).map((v) => (
          <span class="scatter-grid" style={{ '--at': `${top(v)}%` }} aria-hidden="true">
            <span>{l.pct(v)}</span>
          </span>
        ))}
        {ticks(0, xMax).map((v) => (
          <span class="scatter-tick" style={{ '--at': `${left(v)}%` }} aria-hidden="true">
            {l.pct(v)}
          </span>
        ))}
        {reference != null && <span class="scatter-ref" style={{ '--at': `${top(reference)}%` }} aria-hidden="true" />}
        {placed.map((p, i) => (
          <a
            class="scatter-dot"
            href={l.href(p.href)}
            data-sheet-link={opensInSheet(p.href) || undefined}
            data-index={i}
            tabIndex={-1}
            aria-hidden="true"
            title={live ? undefined : p.tip.join('\n')}
            style={{ left: `${p.px}%`, top: `${p.py}%`, '--r': `${p.r}px`, '--fill': p.fill }}
          />
        ))}
        {(['wide', 'narrow'] as const).map((size) =>
          placeLabels(placed, size === 'wide' ? 640 : 300, size === 'wide' ? 380 : 300, size === 'wide' ? 36 : 10).map(
            (l) => (
              <span
                class={`scatter-label ${size}`}
                aria-hidden="true"
                style={{ left: `${l.left}%`, top: `${l.top}%`, '--dx': `${l.dx}px`, '--dy': `${l.dy}px` }}
              >
                {l.label}
              </span>
            ),
          ),
        )}
        {tip && (
          <div
            class={['scatter-tip', tip.py < 25 && 'below', tip.px < 20 ? 'start' : tip.px > 80 && 'end']
              .filter(Boolean)
              .join(' ')}
            style={{ left: `${tip.px}%`, top: `${tip.py}%`, '--r': `${tip.r}px` }}
            aria-hidden="true"
          >
            <strong>{tip.tip[0]}</strong>
            {tip.tip.slice(1).map((line) => (
              <span>{line}</span>
            ))}
          </div>
        )}
      </div>
      <figcaption class="scatter-x">{xLabel}</figcaption>
    </figure>
  );
}
