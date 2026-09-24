// Small chart and table builders for the dashboard, in plain DOM and SVG. Charts draw at the width
// of their container, so text stays readable on a phone, and every value a tooltip shows is also
// on the page as text. Nothing here is mod-specific.

// ---------- formatting ----------

export const num = (v) => (v == null ? '–' : Math.round(v).toLocaleString('en-US'));
export const fixed = (v, digits = 1) => (v == null ? '–' : v.toFixed(digits));

export function pct(v) {
  if (v == null || Number.isNaN(v)) return '–';
  const p = v * 100;
  if (p > 0 && p < 1) return '<1%';
  return `${p >= 10 || p === 0 ? Math.round(p) : p.toFixed(1)}%`;
}

export const range = ([lo, hi]) => `${pct(lo)}–${pct(hi)}`;

export function points(delta) {
  if (delta == null) return '–';
  const p = Math.round(delta * 1000) / 10;
  return `${p > 0 ? '+' : ''}${p} pts`;
}

// ---------- DOM ----------

// Builds an element. Text always goes in through textContent, never as HTML
export function el(tag, attrs = {}, ...children) {
  const node = document.createElement(tag);
  for (const [name, value] of Object.entries(attrs)) {
    if (value == null || value === false) continue;
    if (name === 'style') Object.assign(node.style, value);
    else if (name.startsWith('--')) node.style.setProperty(name, value);
    else if (name.startsWith('on')) node.addEventListener(name.slice(2), value);
    else node.setAttribute(name, value === true ? '' : value);
  }
  for (const child of children.flat()) {
    if (child == null || child === false) continue;
    node.append(child instanceof Node ? child : String(child));
  }
  return node;
}

const SVG = 'http://www.w3.org/2000/svg';

function svg(tag, attrs = {}, text) {
  const node = document.createElementNS(SVG, tag);
  for (const [name, value] of Object.entries(attrs)) node.setAttribute(name, value);
  if (text != null) node.textContent = text;
  return node;
}

export function emptyNote(text = 'Not enough runs for these filters yet.') {
  return el('p', { class: 'empty' }, text);
}

// Draws the chart only when its container has a width, which a hidden tab does not
function widthOf(host) {
  return Math.round(host.getBoundingClientRect().width) || 0;
}

// ---------- tooltip ----------

const tip = el('div', { class: 'tip', role: 'status', hidden: true });
document.body.append(tip);

export function showTip(x, y, lines) {
  tip.replaceChildren(...lines.map((line, i) => el(i ? 'div' : 'strong', {}, line)));
  tip.hidden = false;
  const { offsetWidth: w, offsetHeight: h } = tip;
  const below = y + 16 + h < window.innerHeight - 8;
  tip.style.left = `${Math.max(8, Math.min(x - w / 2, window.innerWidth - w - 8))}px`;
  tip.style.top = `${below ? y + 16 : Math.max(8, y - h - 16)}px`;
}

export function hideTip() {
  tip.hidden = true;
}

document.addEventListener('pointerdown', (e) => {
  if (!e.target.closest('.chart')) hideTip();
});
window.addEventListener('scroll', hideTip, { passive: true });

// ---------- stat tiles ----------

// items: [{label, value, note, delta: {text, up}}]
export function tiles(host, items) {
  host.replaceChildren(
    ...items.map((t) =>
      el(
        'div',
        { class: 'tile' },
        el('div', { class: 'tile-label' }, t.label),
        el(
          'div',
          { class: 'tile-value' },
          t.value,
          t.delta && el('span', { class: `delta ${t.delta.up ? 'up' : 'down'}` }, ` ${t.delta.text}`),
        ),
        t.note && el('div', { class: 'tile-note' }, t.note),
      ),
    ),
  );
}

// ---------- bar list ----------

// A labelled horizontal bar per item, the value as text beside it. Reads well at any width.
// items: [{label, note, value, text, textNote, lo, hi, ref, dot, fill, onSelect}]
// options: max (the full-bar value, default the largest value), reference (a marker on every
// track, for example the overall win rate; an item's own `ref` wins) with referenceLabel, and
// limit (rows shown before a "Show all" button)
export function barList(host, items, options = {}) {
  const { reference, referenceLabel, limit, labelWidth, empty } = options;
  host.replaceChildren();
  if (!items.length) return host.append(emptyNote(empty));
  const max = options.max ?? (Math.max(...items.map((i) => i.value || 0)) || 1);
  const at = (v) => `${Math.max(0, Math.min(1, v / max)) * 100}%`;
  const refOf = (item) => item.ref ?? reference;
  const list = el('ol', { class: 'bars', '--label-width': labelWidth });
  items.forEach((item, i) => {
    const track = el(
      'span',
      { class: 'bar-track', 'aria-hidden': 'true' },
      el('span', { class: 'bar-fill', '--w': at(item.value || 0), '--fill': item.fill }),
      item.lo != null && el('span', { class: 'bar-range', '--lo': at(item.lo), '--hi': at(item.hi) }),
      refOf(item) != null && el('span', { class: 'bar-ref', '--ref': at(refOf(item)) }),
    );
    const row = el(
      'li',
      { class: 'bar', hidden: limit && i >= limit },
      el(
        'span',
        { class: 'bar-label' },
        item.dot && el('i', { class: 'dot', '--dot': item.dot }),
        item.label,
        item.note && el('small', {}, item.note),
      ),
      el('span', { class: 'bar-value' }, item.text, item.textNote && el('small', {}, item.textNote)),
      track,
    );
    if (item.onSelect) {
      row.classList.add('link');
      row.tabIndex = 0;
      row.setAttribute('role', 'button');
      row.addEventListener('click', item.onSelect);
      row.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          item.onSelect();
        }
      });
    }
    list.append(row);
  });
  host.append(list);
  if (referenceLabel && items.some((item) => refOf(item) != null)) host.append(el('div', { class: 'ref-key' }, referenceLabel));
  if (limit && items.length > limit) {
    const more = el('button', { class: 'show-more', type: 'button' }, `Show all ${items.length}`);
    more.addEventListener('click', () => {
      list.querySelectorAll('.bar[hidden]').forEach((row) => (row.hidden = false));
      more.remove();
    });
    host.append(more);
  }
}

// ---------- stacked bar ----------

// One bar split into parts, with a legend that carries every value as text.
// parts: [{label, value, fill}]
export function stackBar(host, parts, { format = num } = {}) {
  const total = parts.reduce((n, p) => n + p.value, 0);
  host.replaceChildren();
  if (!total) return host.append(emptyNote());
  host.append(
    el(
      'div',
      { class: 'stack', role: 'img', 'aria-label': parts.map((p) => `${p.label} ${pct(p.value / total)}`).join(', ') },
      parts.map((p) => el('span', { style: { flexGrow: p.value }, '--fill': p.fill })),
    ),
    el(
      'div',
      { class: 'legend' },
      parts.map((p) =>
        el('span', {}, el('i', { class: 'dot', '--dot': p.fill }), `${p.label}: ${format(p.value)} (${pct(p.value / total)})`),
      ),
    ),
  );
}

// ---------- column chart ----------

function niceMax(v) {
  if (v <= 0) return 1;
  const step = 10 ** Math.floor(Math.log10(v));
  for (const m of [1, 2, 2.5, 5, 10]) if (m * step >= v) return m * step;
  return 10 * step;
}

// A column per item, for counts along an ordered axis (a histogram, deaths by floor).
// items: [{label, value, tip: [lines], fill}]
// markers: [{before: index, label}] draw a labelled line on the left edge of that column
export function columns(host, items, { height = 200, markers = [], format = num, ariaLabel, empty } = {}) {
  host.replaceChildren();
  if (!items.length || !items.some((i) => i.value)) return host.append(emptyNote(empty));
  const width = widthOf(host);
  if (!width) return;
  const pad = { l: 40, r: 6, t: markers.length ? 24 : 8, b: 26 };
  const max = niceMax(Math.max(...items.map((i) => i.value)));
  const band = (width - pad.l - pad.r) / items.length;
  const barWidth = Math.min(24, Math.max(2, band - 2));
  const y = (v) => pad.t + (1 - v / max) * (height - pad.t - pad.b);
  const root = svg('svg', { viewBox: `0 0 ${width} ${height}`, role: 'img', 'aria-label': ariaLabel || '' });

  for (const t of [0, 0.5, 1]) {
    root.append(svg('line', { class: 'axis', x1: pad.l, x2: width - pad.r, y1: y(t * max), y2: y(t * max) }));
    root.append(svg('text', { x: pad.l - 6, y: y(t * max) + 4, 'text-anchor': 'end' }, format(t * max)));
  }

  const longest = Math.max(...items.map((item) => String(item.label).length));
  const labelEvery = Math.max(1, Math.ceil((longest * 7 + 12) / band));
  items.forEach((item, i) => {
    const cx = pad.l + band * (i + 0.5);
    const top = y(item.value);
    const base = y(0);
    const x = cx - barWidth / 2;
    const r = Math.min(4, barWidth / 2, base - top);
    if (item.value > 0) {
      root.append(
        svg('path', {
          d: `M${x},${base}V${top + r}Q${x},${top} ${x + r},${top}H${x + barWidth - r}Q${x + barWidth},${top} ${x + barWidth},${top + r}V${base}Z`,
          fill: item.fill || 'var(--accent)',
        }),
      );
    }
    if (i % labelEvery === 0) {
      root.append(svg('text', { x: cx, y: height - 8, 'text-anchor': 'middle' }, item.label));
    }
  });

  // A marker label that would run into the one before it moves up a row
  const rowEnds = [];
  for (const m of markers) {
    const x = pad.l + band * m.before;
    let row = rowEnds.findIndex((end) => end < x);
    if (row < 0) row = rowEnds.length;
    rowEnds[row] = x + 8 + m.label.length * 7;
    const labelY = pad.t - 10 - row * 16;
    root.append(svg('line', { x1: x, x2: x, y1: pad.t - 6, y2: y(0), stroke: 'var(--gold)', 'stroke-width': 1.5 }));
    root.append(svg('text', { x: x - 2, y: labelY, fill: 'var(--ink)' }, m.label));
  }
  if (rowEnds.length > 1) {
    const extra = (rowEnds.length - 1) * 16;
    root.setAttribute('viewBox', `0 ${-extra} ${width} ${height + extra}`);
  }

  const hit = svg('rect', { x: pad.l, y: 0, width: width - pad.l - pad.r, height, fill: 'transparent' });
  const pick = (e) => {
    const box = root.getBoundingClientRect();
    const i = Math.floor(((e.clientX - box.left) * (width / box.width) - pad.l) / band);
    const item = items[Math.max(0, Math.min(items.length - 1, i))];
    showTip(e.clientX, e.clientY, item.tip || [item.label, format(item.value)]);
  };
  hit.addEventListener('pointermove', pick);
  hit.addEventListener('pointerdown', pick);
  hit.addEventListener('pointerleave', (e) => e.pointerType === 'mouse' && hideTip());
  root.append(hit);
  host.append(el('div', { class: 'chart' }, root));
}

// ---------- scatter ----------

// One dot per item on two rate axes. The pointer picks the nearest dot, so a small dot is
// easy to hit, and a click opens it.
// points: [{x, y, r, fill, label, tip, dim, weight}]
// options: xLabel, reference (a y value, drawn as a gold line; name it in the legend),
// labelCount (how many dots get a name, heaviest first), onSelect
export function scatter(host, pts, options = {}) {
  host.replaceChildren();
  if (!pts.length) return host.append(emptyNote());
  const width = widthOf(host);
  if (!width) return;
  const height = Math.round(Math.max(280, Math.min(460, width * 0.72)));
  const pad = { l: 44, r: 14, t: 14, b: 44 };
  const xs = pts.map((p) => p.x);
  const ys = pts.map((p) => p.y).concat(options.reference ?? []);
  const xMax = Math.min(1, Math.ceil((Math.max(...xs) + 0.02) * 10) / 10);
  const yMin = Math.max(0, Math.floor((Math.min(...ys) - 0.02) * 10) / 10);
  const yMax = Math.min(1, Math.ceil((Math.max(...ys) + 0.02) * 10) / 10);
  const px = (v) => pad.l + (v / xMax) * (width - pad.l - pad.r);
  const py = (v) => pad.t + (1 - (v - yMin) / (yMax - yMin)) * (height - pad.t - pad.b);
  const root = svg('svg', { viewBox: `0 0 ${width} ${height}`, role: 'img', 'aria-label': options.ariaLabel || '' });

  const yStep = yMax - yMin > 0.5 ? 0.2 : 0.1;
  for (let v = yMin; v <= yMax + 1e-9; v += yStep) {
    root.append(svg('line', { class: 'axis', x1: pad.l, x2: width - pad.r, y1: py(v), y2: py(v) }));
    root.append(svg('text', { x: pad.l - 6, y: py(v) + 4, 'text-anchor': 'end' }, pct(v)));
  }
  const xStep = xMax > 0.5 ? 0.2 : 0.1;
  for (let v = 0; v <= xMax + 1e-9; v += xStep) {
    root.append(svg('text', { x: px(v), y: height - 24, 'text-anchor': 'middle' }, pct(v)));
  }
  root.append(svg('text', { x: pad.l + (width - pad.l - pad.r) / 2, y: height - 6, 'text-anchor': 'middle' }, options.xLabel || ''));
  if (options.reference != null) {
    const ry = py(options.reference);
    root.append(svg('line', { x1: pad.l, x2: width - pad.r, y1: ry, y2: ry, stroke: 'var(--gold)', 'stroke-width': 1.5 }));
  }

  const placed = pts.map((p) => ({ ...p, cx: px(p.x), cy: py(p.y) }));
  const dots = svg('g');
  for (const p of [...placed].sort((a, b) => Number(!a.dim) - Number(!b.dim))) {
    dots.append(
      svg('circle', {
        cx: p.cx,
        cy: p.cy,
        r: p.r,
        fill: p.fill,
        stroke: 'var(--panel)',
        'stroke-width': 2,
        opacity: p.dim ? 0.18 : 0.95,
      }),
    );
  }
  root.append(dots);
  placeLabels(root, placed.filter((p) => !p.dim), { width, height, pad }, options.labelCount ?? 30);

  const ring = svg('circle', { r: 0, fill: 'none', stroke: 'var(--ink)', 'stroke-width': 2, 'pointer-events': 'none' });
  root.append(ring);
  const nearest = (e) => {
    const box = root.getBoundingClientRect();
    const x = (e.clientX - box.left) * (width / box.width);
    const y = (e.clientY - box.top) * (height / box.height);
    let best = null;
    let bestDistance = 28;
    for (const p of placed) {
      if (p.dim) continue;
      const d = Math.hypot(p.cx - x, p.cy - y);
      if (d < bestDistance) [best, bestDistance] = [p, d];
    }
    return best;
  };
  const hover = (e) => {
    const p = nearest(e);
    if (!p) {
      ring.setAttribute('r', 0);
      if (e.pointerType === 'mouse') hideTip();
      return;
    }
    ring.setAttribute('cx', p.cx);
    ring.setAttribute('cy', p.cy);
    ring.setAttribute('r', p.r + 3);
    showTip(e.clientX, e.clientY, p.tip);
  };
  const hit = svg('rect', { x: 0, y: 0, width, height, fill: 'transparent', style: 'cursor: pointer' });
  hit.addEventListener('pointermove', hover);
  hit.addEventListener('pointerleave', (e) => {
    ring.setAttribute('r', 0);
    if (e.pointerType === 'mouse') hideTip();
  });
  // A tap first shows the numbers, and a second tap on the same dot opens it
  let tapped = null;
  hit.addEventListener('click', (e) => {
    const p = nearest(e);
    if (!p) return;
    if (e.pointerType === 'mouse' || tapped === p.label) {
      hideTip();
      options.onSelect?.(p);
      tapped = null;
    } else {
      hover(e);
      tapped = p.label;
    }
  });
  root.append(hit);
  host.append(el('div', { class: 'chart' }, root));
}

// Names the heaviest dots first. Each label tries spots around its dot and takes the first one
// that overlaps no dot and no earlier label. A crowded dot stays unnamed, and the tooltip names it
function placeLabels(root, pts, { width, height, pad }, count) {
  const size = 12;
  const boxes = pts.map((p) => ({ x: p.cx - p.r, y: p.cy - p.r, w: p.r * 2, h: p.r * 2 }));
  const clear = (b) =>
    b.x >= pad.l &&
    b.x + b.w <= width - pad.r &&
    b.y >= pad.t &&
    b.y + b.h <= height - pad.b &&
    !boxes.some((o) => b.x < o.x + o.w + 2 && b.x + b.w + 2 > o.x && b.y < o.y + o.h + 2 && b.y + b.h + 2 > o.y);
  const heaviest = [...pts].sort((a, b) => (b.weight || 0) - (a.weight || 0)).slice(0, count);
  for (const p of heaviest) {
    const w = p.label.length * size * 0.56;
    const spots = [
      [p.r + 4, -size / 2],
      [-p.r - 4 - w, -size / 2],
      [-w / 2, -p.r - size - 3],
      [-w / 2, p.r + 3],
    ];
    for (const [dx, dy] of spots) {
      const box = { x: p.cx + dx, y: p.cy + dy, w, h: size + 2 };
      if (!clear(box)) continue;
      boxes.push(box);
      root.append(svg('text', { x: box.x, y: box.y + size - 1, fill: 'var(--ink-2)' }, p.label));
      break;
    }
  }
}

// ---------- table ----------

// A sortable table. Columns marked `wide` hide on a narrow screen.
// columns: [{key, label, num, wide, sortable, render(row), value(row)}]
// sort: {key, dir} with dir 1 (ascending) or -1; onSort(nextSort) is called on a header click
export function dataTable(host, { columns: cols, rows, sort, onSort, onRow, limit, empty }) {
  host.replaceChildren();
  if (!rows.length) return host.append(emptyNote(empty));
  const column = sort && (cols.find((c) => c.key === sort.key) ?? { key: sort.key });
  const valueOf = (c, row) => (c.value ? c.value(row) : row[c.key]);
  const sorted = column
    ? [...rows].sort((a, b) => {
        const va = valueOf(column, a);
        const vb = valueOf(column, b);
        if (va == null) return 1;
        if (vb == null) return -1;
        return (va < vb ? -1 : va > vb ? 1 : 0) * sort.dir;
      })
    : rows;
  const head = el(
    'tr',
    {},
    cols.map((c) => {
      const active = c.key === sort?.key;
      const th = el(
        'th',
        {
          class: [c.num && 'num', c.wide && 'wide'].filter(Boolean).join(' ') || null,
          'aria-sort': active ? (sort.dir > 0 ? 'ascending' : 'descending') : null,
        },
        c.label,
        active ? (sort.dir > 0 ? ' ↑' : ' ↓') : '',
      );
      if (onSort && c.sortable !== false) {
        th.style.cursor = 'pointer';
        th.addEventListener('click', () =>
          onSort({ key: c.key, dir: active ? -sort.dir : c.num ? -1 : 1 }),
        );
      }
      return th;
    }),
  );
  const body = sorted.map((row, i) => {
    const tr = el(
      'tr',
      { class: onRow ? 'link' : null, hidden: limit && i >= limit },
      cols.map((c) => {
        const content = c.render ? c.render(row) : valueOf(c, row);
        return el('td', { class: [c.num && 'num', c.wide && 'wide'].filter(Boolean).join(' ') || null }, content ?? '–');
      }),
    );
    if (onRow) {
      tr.tabIndex = 0;
      tr.addEventListener('click', () => onRow(row));
      tr.addEventListener('keydown', (e) => e.key === 'Enter' && onRow(row));
    }
    return tr;
  });
  const tableNode = el('table', {}, el('thead', {}, head), el('tbody', {}, body));
  host.append(el('div', { class: 'table-wrap' }, tableNode));
  if (limit && rows.length > limit) {
    const more = el('button', { class: 'show-more', type: 'button' }, `Show all ${rows.length}`);
    more.addEventListener('click', () => {
      tableNode.querySelectorAll('tr[hidden]').forEach((tr) => (tr.hidden = false));
      more.remove();
    });
    host.append(more);
  }
}
