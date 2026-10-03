// The data files as the browser gets them. A count table is stored by column instead of by row,
// and each text key as an index into its list of names, which halves the download.

import type { ColumnFile, TableFile } from './types';

const isTable = (value: unknown): value is TableFile =>
  typeof value === 'object' && value !== null && 'rows' in value && 'key' in value;

export function toColumns({ counts, key, rows }: TableFile): ColumnFile {
  const names: Record<string, string[]> = {};
  const columns = [...key, ...counts].map((name, i) => {
    const column = rows.map((row) => row[i]);
    if (typeof column[0] !== 'string') return column as number[];
    const list = [...new Set(column as string[])].sort();
    const index = new Map(list.map((value, j) => [value, j]));
    names[name] = list;
    return column.map((value) => index.get(value as string)!);
  });
  return { columns, counts, key, names };
}

/** Every count table in a data file, stored by column */
export const compact = (file: Record<string, unknown>) =>
  Object.fromEntries(Object.entries(file).map(([name, value]) => [name, isTable(value) ? toColumns(value) : value]));
