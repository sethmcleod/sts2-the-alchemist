import type { AnyTableFile } from './types';

export type Counts = Record<string, number>;
export type Row = Record<string, number | string> & { group: number };
export interface Table {
  counts: string[];
  rows: Row[];
}
export type Selection = number[];

export function table(file: AnyTableFile): Table {
  const columnNames = [...file.key, ...file.counts];
  if ('rows' in file) {
    return {
      counts: file.counts,
      rows: file.rows.map((row) => Object.fromEntries(columnNames.map((name, i) => [name, row[i]])) as Row),
    };
  }
  const rows: Row[] = [];
  for (let rowIndex = 0; rowIndex < (file.columns[0]?.length ?? 0); rowIndex++) {
    const row: Record<string, number | string> = {};
    columnNames.forEach((name, columnIndex) => {
      const value = file.columns[columnIndex][rowIndex];
      row[name] = file.names[name]?.[value] ?? value;
    });
    rows.push(row as Row);
  }
  return { counts: file.counts, rows };
}

const zeros = (counts: string[]): Counts => Object.fromEntries(counts.map((name) => [name, 0]));

export function sumBy<K>(source: Table, on: Selection, keyOf: (row: Row) => K) {
  const totals = new Map<K, Counts>();
  for (const row of source.rows) {
    if (!on[row.group]) continue;
    const key = keyOf(row);
    let total = totals.get(key);
    if (!total) totals.set(key, (total = zeros(source.counts)));
    for (const name of source.counts) total[name] += row[name] as number;
  }
  return totals;
}

export function sum(source: Table, on: Selection, where: (row: Row) => boolean = () => true) {
  const total = zeros(source.counts);
  for (const row of source.rows) {
    if (!on[row.group] || !where(row)) continue;
    for (const name of source.counts) total[name] += row[name] as number;
  }
  return total;
}

export const rate = (hits: number, n: number) => (n > 0 ? hits / n : null);

export function wilson(hits: number, n: number): [number, number] {
  if (!n) return [0, 1];
  const z = 1.96;
  const p = hits / n;
  const z2 = z * z;
  const mid = (p + z2 / (2 * n)) / (1 + z2 / n);
  const half = (z * Math.sqrt((p * (1 - p)) / n + z2 / (4 * n * n))) / (1 + z2 / n);
  return [Math.max(0, mid - half), Math.min(1, mid + half)];
}

export function zScore(hitsA: number, nA: number, hitsB: number, nB: number) {
  if (!nA || !nB) return 0;
  const pooled = (hitsA + hitsB) / (nA + nB);
  const standardError = Math.sqrt(pooled * (1 - pooled) * (1 / nA + 1 / nB));
  return standardError > 0 ? (hitsB / nB - hitsA / nA) / standardError : 0;
}

export function median(values: number[]) {
  const sorted = [...values].sort((a, b) => a - b);
  if (!sorted.length) return null;
  const mid = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
}

export function histogramMedian(bins: { bin: number; runs: number }[], width: number) {
  const total = bins.reduce((count, entry) => count + entry.runs, 0);
  if (!total) return null;
  let runsBefore = 0;
  for (const entry of [...bins].sort((a, b) => a.bin - b.bin)) {
    if (runsBefore + entry.runs >= total / 2) return entry.bin + ((total / 2 - runsBefore) / entry.runs) * width;
    runsBefore += entry.runs;
  }
  return null;
}

export function compareVersions(a: string, b: string) {
  const numbers = (version: string) =>
    version
      .replace(/^v/, '')
      .split('.')
      .map((part) => parseInt(part, 10) || 0);
  const [numbersA, numbersB] = [numbers(a), numbers(b)];
  for (let i = 0; i < Math.max(numbersA.length, numbersB.length); i++) {
    const difference = (numbersA[i] || 0) - (numbersB[i] || 0);
    if (difference) return difference;
  }
  return 0;
}
