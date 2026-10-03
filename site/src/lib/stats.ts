// Count tables and the statistics read from them. Every row belongs to one group of runs (version,
// build, ascension band, card pool, solo or multiplayer). A filter picks groups, and a sum adds up the
// counts of the rows in those groups.

import type { AnyTableFile } from './types';

export type Counts = Record<string, number>;
export type Row = Record<string, number | string> & { group: number };
export interface Table {
  counts: string[];
  rows: Row[];
}
/** One flag per group: 1 when the filters keep it */
export type Selection = number[];

export function table(file: AnyTableFile): Table {
  const names = [...file.key, ...file.counts];
  if ('rows' in file) {
    return {
      counts: file.counts,
      rows: file.rows.map((row) => Object.fromEntries(names.map((name, i) => [name, row[i]])) as Row),
    };
  }
  const rows: Row[] = [];
  for (let i = 0; i < (file.columns[0]?.length ?? 0); i++) {
    const row: Record<string, number | string> = {};
    names.forEach((name, j) => {
      const value = file.columns[j][i];
      row[name] = file.names[name]?.[value] ?? value;
    });
    rows.push(row as Row);
  }
  return { counts: file.counts, rows };
}

const zeros = (counts: string[]): Counts => Object.fromEntries(counts.map((name) => [name, 0]));

export function sumBy<K>(t: Table, on: Selection, keyOf: (row: Row) => K) {
  const totals = new Map<K, Counts>();
  for (const row of t.rows) {
    if (!on[row.group]) continue;
    const key = keyOf(row);
    let total = totals.get(key);
    if (!total) totals.set(key, (total = zeros(t.counts)));
    for (const name of t.counts) total[name] += row[name] as number;
  }
  return totals;
}

export function sum(t: Table, on: Selection, where: (row: Row) => boolean = () => true) {
  const total = zeros(t.counts);
  for (const row of t.rows) {
    if (!on[row.group] || !where(row)) continue;
    for (const name of t.counts) total[name] += row[name] as number;
  }
  return total;
}

export const rate = (hits: number, n: number) => (n > 0 ? hits / n : null);

/** The Wilson 95% interval: an honest range for a rate measured on few runs */
export function wilson(hits: number, n: number): [number, number] {
  if (!n) return [0, 1];
  const z = 1.96;
  const p = hits / n;
  const z2 = z * z;
  const mid = (p + z2 / (2 * n)) / (1 + z2 / n);
  const half = (z * Math.sqrt((p * (1 - p)) / n + z2 / (4 * n * n))) / (1 + z2 / n);
  return [Math.max(0, mid - half), Math.min(1, mid + half)];
}

/** How many standard errors apart two rates are (a two-proportion z-test) */
export function zScore(hitsA: number, nA: number, hitsB: number, nB: number) {
  if (!nA || !nB) return 0;
  const pooled = (hitsA + hitsB) / (nA + nB);
  const se = Math.sqrt(pooled * (1 - pooled) * (1 / nA + 1 / nB));
  return se > 0 ? (hitsB / nB - hitsA / nA) / se : 0;
}

export function median(values: number[]) {
  const sorted = [...values].sort((a, b) => a - b);
  if (!sorted.length) return null;
  const mid = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
}

/** The median of a histogram, read by straight-line interpolation inside the middle bin */
export function histogramMedian(bins: { bin: number; runs: number }[], width: number) {
  const total = bins.reduce((n, b) => n + b.runs, 0);
  if (!total) return null;
  let seen = 0;
  for (const b of [...bins].sort((x, y) => x.bin - y.bin)) {
    if (seen + b.runs >= total / 2) return b.bin + ((total / 2 - seen) / b.runs) * width;
    seen += b.runs;
  }
  return null;
}

/** "v0.9.0" sorts before "v0.11.0" */
export function compareVersions(a: string, b: string) {
  const parts = (v: string) =>
    v
      .replace(/^v/, '')
      .split('.')
      .map((x) => parseInt(x, 10) || 0);
  const [pa, pb] = [parts(a), parts(b)];
  for (let i = 0; i < Math.max(pa.length, pb.length); i++) {
    const d = (pa[i] || 0) - (pb[i] || 0);
    if (d) return d;
  }
  return 0;
}
