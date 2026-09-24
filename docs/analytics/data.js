// Loads the tables tools/analytics/export_stats.py writes and adds them up. Every row belongs to
// one group of runs (version, build, ascension band, card pool, solo or co-op). A filter picks
// groups, and a sum adds the counts of the rows in those groups. Nothing here is mod-specific.

const files = new Map();

export function load(file) {
  if (!files.has(file)) {
    const request = fetch(`data/${file}`, { cache: 'no-cache' }).then((response) => {
      if (!response.ok) throw new Error(`${file} (${response.status})`);
      return response.json();
    });
    files.set(file, request);
  }
  return files.get(file);
}

// {key, counts, rows} -> {counts, rows: [{group, card, held, ...}]}
export function table({ key, counts, rows }) {
  const names = [...key, ...counts];
  return {
    counts,
    rows: rows.map((row) => {
      const named = {};
      names.forEach((name, i) => (named[name] = row[i]));
      return named;
    }),
  };
}

// One flag per group: 1 when the group passes the test
export function selectGroups(groups, test) {
  return groups.rows.map((group) => (test(group) ? 1 : 0));
}

const zeros = (counts) => Object.fromEntries(counts.map((name) => [name, 0]));

// Adds up the counts of every row in a selected group, one total per key
export function sumBy(t, selected, keyOf) {
  const totals = new Map();
  for (const row of t.rows) {
    if (!selected[row.group]) continue;
    const key = keyOf(row);
    let total = totals.get(key);
    if (!total) totals.set(key, (total = zeros(t.counts)));
    for (const name of t.counts) total[name] += row[name];
  }
  return totals;
}

export function sum(t, selected, where = () => true) {
  const total = zeros(t.counts);
  for (const row of t.rows) {
    if (!selected[row.group] || !where(row)) continue;
    for (const name of t.counts) total[name] += row[name];
  }
  return total;
}

// ---------- statistics ----------

export const rate = (hits, n) => (n > 0 ? hits / n : null);

// Wilson 95% interval: an honest range for a rate measured on few runs
export function wilson(hits, n) {
  if (!n) return [0, 1];
  const z = 1.96;
  const p = hits / n;
  const z2 = z * z;
  const mid = (p + z2 / (2 * n)) / (1 + z2 / n);
  const half = (z * Math.sqrt((p * (1 - p)) / n + z2 / (4 * n * n))) / (1 + z2 / n);
  return [Math.max(0, mid - half), Math.min(1, mid + half)];
}

// How many standard errors apart two rates are (a two-proportion z-test). Past 1.96 the gap is
// bigger than chance explains 95% of the time, and past 2.58, 99%
export function zScore(hitsA, nA, hitsB, nB) {
  if (!nA || !nB) return 0;
  const pooled = (hitsA + hitsB) / (nA + nB);
  const se = Math.sqrt(pooled * (1 - pooled) * (1 / nA + 1 / nB));
  return se > 0 ? (hitsB / nB - hitsA / nA) / se : 0;
}

export function median(values) {
  const sorted = [...values].sort((a, b) => a - b);
  if (!sorted.length) return null;
  const mid = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
}

// The median of a histogram, read by straight-line interpolation inside the middle bin.
// bins: [{bin, runs}] where bin is the lower edge and every bin is `width` wide
export function histogramMedian(bins, width) {
  const total = bins.reduce((n, b) => n + b.runs, 0);
  if (!total) return null;
  let seen = 0;
  for (const b of [...bins].sort((x, y) => x.bin - y.bin)) {
    if (seen + b.runs >= total / 2) return b.bin + ((total / 2 - seen) / b.runs) * width;
    seen += b.runs;
  }
  return null;
}

// "v0.9.0" sorts before "v0.11.0"
export function compareVersions(a, b) {
  const parts = (v) =>
    String(v)
      .replace(/^v/, '')
      .split('.')
      .map((x) => parseInt(x, 10) || 0);
  const pa = parts(a);
  const pb = parts(b);
  for (let i = 0; i < Math.max(pa.length, pb.length); i++) {
    const d = (pa[i] || 0) - (pb[i] || 0);
    if (d) return d;
  }
  return 0;
}
