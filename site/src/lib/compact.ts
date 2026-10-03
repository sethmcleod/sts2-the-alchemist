import type { ColumnFile, TableFile } from './types';

const isTable = (value: unknown): value is TableFile =>
  typeof value === 'object' && value !== null && 'rows' in value && 'key' in value;

export function toColumns({ counts, key, rows }: TableFile): ColumnFile {
  const names: Record<string, string[]> = {};
  const columns = [...key, ...counts].map((name, columnIndex) => {
    const column = rows.map((row) => row[columnIndex]);
    if (typeof column[0] !== 'string') return column as number[];
    const distinct = [...new Set(column as string[])].sort();
    const indexOf = new Map(distinct.map((value, index) => [value, index]));
    names[name] = distinct;
    return column.map((value) => indexOf.get(value as string)!);
  });
  return { columns, counts, key, names };
}

export const compact = (file: Record<string, unknown>) =>
  Object.fromEntries(Object.entries(file).map(([name, value]) => [name, isTable(value) ? toColumns(value) : value]));
