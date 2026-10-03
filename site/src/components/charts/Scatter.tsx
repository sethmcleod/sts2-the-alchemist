import { useEffect, useState } from 'preact/hooks';
import type { Lang } from '../../lib/lang';
import { opensInSheet } from '../../lib/links';
import { emptyText } from './empty';

export interface Point {
  fill: string;
  href: string;
  label: string;
  r: number;
  tip: string[];
  weight: number;
  x: number;
  y: number;
}

interface Props {
  l: Lang;
  label: string;
  points: Point[];
  reference?: null | number;
  xLabel: string;
}

const LETTER_WIDTH = 6.7;
const labelWidth = (label: string) =>
  [...label].reduce(
    (width, char) => width + (/[\u1100-\u9fff\uac00-\ud7af\uff00-\uffef]/.test(char) ? 2 : 1) * LETTER_WIDTH,
    0,
  );
const LINE_HEIGHT = 14;

function placeLabels(points: (Point & { px: number; py: number })[], width: number, height: number, maxLabels: number) {
  const toPixels = (point: { px: number; py: number }) => ({
    x: (point.px / 100) * width,
    y: (point.py / 100) * height,
  });
  const occupied = points.map((point) => ({
    h: point.r * 2,
    w: point.r * 2,
    x: toPixels(point).x - point.r,
    y: toPixels(point).y - point.r,
  }));
  const isFree = (box: (typeof occupied)[number]) =>
    box.x >= 0 &&
    box.x + box.w <= width &&
    box.y >= 0 &&
    box.y + box.h <= height &&
    !occupied.some(
      (other) =>
        box.x < other.x + other.w + 2 &&
        box.x + box.w + 2 > other.x &&
        box.y < other.y + other.h + 2 &&
        box.y + box.h + 2 > other.y,
    );
  const labels = [];
  for (const point of [...points].sort((a, b) => b.weight - a.weight).slice(0, maxLabels)) {
    const textWidth = labelWidth(point.label);
    const { x, y } = toPixels(point);
    const spots: [number, number][] = [
      [point.r + 4, -LINE_HEIGHT / 2],
      [-point.r - 4 - textWidth, -LINE_HEIGHT / 2],
      [-textWidth / 2, -point.r - LINE_HEIGHT - 2],
      [-textWidth / 2, point.r + 2],
    ];
    for (const [dx, dy] of spots) {
      const box = { h: LINE_HEIGHT, w: textWidth, x: x + dx, y: y + dy };
      if (!isFree(box)) continue;
      occupied.push(box);
      labels.push({ dx, dy, label: point.label, left: point.px, top: point.py });
      break;
    }
  }
  return labels;
}

const tickStep = (span: number) => (span > 0.5 ? 0.2 : 0.1);

export default function Scatter({ l, label, points, reference, xLabel }: Props) {
  const [active, setActive] = useState<null | number>(null);
  const [hydrated, setHydrated] = useState(false);
  useEffect(() => setHydrated(true), []);
  useEffect(() => setActive(null), [points]);
  if (!points.length) return <p class="empty">{emptyText(l)}</p>;
  const yValues = points.map((point) => point.y).concat(reference ?? []);
  const xMax = Math.min(1, Math.ceil((Math.max(...points.map((point) => point.x)) + 0.02) * 10) / 10);
  const yMin = Math.max(0, Math.floor((Math.min(...yValues) - 0.02) * 10) / 10);
  const yMax = Math.min(1, Math.ceil((Math.max(...yValues) + 0.02) * 10) / 10);
  const left = (x: number) => (x / xMax) * 100;
  const top = (y: number) => (1 - (y - yMin) / (yMax - yMin)) * 100;
  const positioned = points.map((point) => ({ ...point, px: left(point.x), py: top(point.y) }));
  const ticks = (from: number, to: number) => {
    const values = [];
    for (let value = from; value <= to + 1e-9; value += tickStep(to - from)) values.push(Math.round(value * 100) / 100);
    return values;
  };
  const hoverDot = (e: PointerEvent) => {
    if (e.pointerType === 'touch') return;
    const index = (e.target as HTMLElement).closest<HTMLElement>('[data-index]')?.dataset.index;
    setActive(index ? Number(index) : null);
  };
  const activeDot = active === null ? undefined : positioned[active];

  return (
    <figure aria-label={label} class="scatter">
      <div class="scatter-plot" onPointerLeave={() => setActive(null)} onPointerOver={hoverDot}>
        {ticks(yMin, yMax).map((value) => (
          <span aria-hidden="true" class="scatter-grid" style={{ '--at': `${top(value)}%` }}>
            <span>{l.pct(value)}</span>
          </span>
        ))}
        {ticks(0, xMax).map((value) => (
          <span aria-hidden="true" class="scatter-tick" style={{ '--at': `${left(value)}%` }}>
            {l.pct(value)}
          </span>
        ))}
        {reference != null && <span aria-hidden="true" class="scatter-ref" style={{ '--at': `${top(reference)}%` }} />}
        {positioned.map((point, i) => (
          <a
            aria-hidden="true"
            class="scatter-dot"
            data-index={i}
            data-sheet-link={opensInSheet(point.href) || undefined}
            href={l.href(point.href)}
            style={{ '--fill': point.fill, '--r': `${point.r}px`, left: `${point.px}%`, top: `${point.py}%` }}
            tabIndex={-1}
            title={hydrated ? undefined : point.tip.join('\n')}
          />
        ))}
        {(['wide', 'narrow'] as const).map((size) =>
          placeLabels(
            positioned,
            size === 'wide' ? 640 : 300,
            size === 'wide' ? 380 : 300,
            size === 'wide' ? 36 : 10,
          ).map((placedLabel) => (
            <span
              aria-hidden="true"
              class={`scatter-label ${size}`}
              style={{
                '--dx': `${placedLabel.dx}px`,
                '--dy': `${placedLabel.dy}px`,
                left: `${placedLabel.left}%`,
                top: `${placedLabel.top}%`,
              }}
            >
              {placedLabel.label}
            </span>
          )),
        )}
        {activeDot && (
          <div
            aria-hidden="true"
            class={[
              'scatter-tip',
              activeDot.py < 25 && 'below',
              activeDot.px < 20 ? 'start' : activeDot.px > 80 && 'end',
            ]
              .filter(Boolean)
              .join(' ')}
            style={{ '--r': `${activeDot.r}px`, left: `${activeDot.px}%`, top: `${activeDot.py}%` }}
          >
            <strong>{activeDot.tip[0]}</strong>
            {activeDot.tip.slice(1).map((line) => (
              <span>{line}</span>
            ))}
          </div>
        )}
      </div>
      <figcaption class="scatter-x">{xLabel}</figcaption>
    </figure>
  );
}
