import { describe, expect, it } from 'vitest';
import { toColumns } from './compact';
import { compareVersions, histogramMedian, median, rate, sum, sumBy, table, wilson, zScore } from './stats';

describe('rates', () => {
  it('has no rate without runs', () => {
    expect(rate(3, 0)).toBeNull();
    expect(rate(1, 4)).toBe(0.25);
  });

  it('gives a wide Wilson range for few runs and a narrow one for many', () => {
    const [lo, hi] = wilson(5, 10);
    expect(lo).toBeCloseTo(0.237, 3);
    expect(hi).toBeCloseTo(0.763, 3);
    const [lo2, hi2] = wilson(500, 1000);
    expect(hi2 - lo2).toBeLessThan(0.07);
    expect(wilson(0, 0)).toEqual([0, 1]);
  });

  it('scores a real gap higher than a small one', () => {
    expect(Math.abs(zScore(40, 100, 60, 100))).toBeGreaterThan(2.58);
    expect(Math.abs(zScore(50, 100, 52, 100))).toBeLessThan(1.96);
  });
});

describe('medians', () => {
  it('takes the middle value, or the mean of the middle two', () => {
    expect(median([3, 1, 2])).toBe(2);
    expect(median([4, 1, 3, 2])).toBe(2.5);
    expect(median([])).toBeNull();
  });

  it('reads a histogram median inside its middle bin', () => {
    const bins = [
      { bin: 0, runs: 10 },
      { bin: 10, runs: 10 },
      { bin: 20, runs: 0 },
    ];
    expect(histogramMedian(bins, 10)).toBe(10);
    expect(histogramMedian([{ bin: 0, runs: 0 }], 10)).toBeNull();
  });
});

describe('versions', () => {
  it('sorts by number, not by text', () => {
    expect(compareVersions('v0.9.0', 'v0.11.0')).toBeLessThan(0);
    expect(compareVersions('v0.14.21', 'v0.14.3')).toBeGreaterThan(0);
    expect(compareVersions('v1.0', 'v1.0.0')).toBe(0);
  });
});

describe('count tables', () => {
  const t = table({
    counts: ['held', 'wins'],
    key: ['group', 'card'],
    rows: [
      [0, 'A', 2, 1],
      [1, 'A', 3, 3],
      [1, 'B', 1, 0],
    ],
  });

  it('adds up only the selected groups', () => {
    expect(sum(t, [1, 1])).toEqual({ held: 6, wins: 4 });
    expect(sum(t, [0, 1])).toEqual({ held: 4, wins: 3 });
  });

  it('adds up per key', () => {
    const byCard = sumBy(t, [1, 1], (row) => row.card);
    expect(byCard.get('A')).toEqual({ held: 5, wins: 4 });
    expect(byCard.get('B')).toEqual({ held: 1, wins: 0 });
  });
});

describe('the compact form', () => {
  it('reads back the same rows', () => {
    const file = {
      counts: ['held'],
      key: ['group', 'card'],
      rows: [
        [0, 'B', 2],
        [1, 'A', 3],
        [1, 'B', 4],
      ],
    };
    const columns = toColumns(file);
    expect(columns.names.card).toEqual(['A', 'B']);
    expect(columns.columns[1]).toEqual([1, 0, 1]);
    expect(table(columns)).toEqual(table(file));
  });
});
