import type { ComponentChildren } from 'preact';
import { useState } from 'preact/hooks';
import type { Lang } from '../../lib/lang';
import { emptyText } from './empty';

export interface ColumnDef<R> {
  key: string;
  label: string;
  num?: boolean;
  render?: (row: R) => ComponentChildren;
  sortable?: boolean;
  wide?: boolean;
}

export type Sort = { dir: -1 | 1; key: string };

interface Props<R> {
  caption?: string;
  columns: ColumnDef<R>[];
  empty?: string;
  l: Lang;
  limit?: number;
  rows: R[];
  sort?: Sort;
}

export default function DataTable<R extends object>({
  caption,
  columns,
  empty,
  l,
  limit,
  rows,
  sort: initialSort,
}: Props<R>) {
  const [sort, setSort] = useState(initialSort);
  if (!rows.length) return <p class="empty">{empty ?? emptyText(l)}</p>;

  const cellValue = (column: ColumnDef<R>, row: R) =>
    (row as Record<string, unknown>)[column.key] as null | number | string;
  const hiddenCount = limit && rows.length > limit ? rows.length - limit : 0;
  const sortColumn = sort && columns.find((column) => column.key === sort.key);
  const sorted = sortColumn
    ? [...rows].sort((a, b) => {
        const [valueA, valueB] = [cellValue(sortColumn, a), cellValue(sortColumn, b)];
        if (valueA == null) return 1;
        if (valueB == null) return -1;
        const order =
          typeof valueA === 'string' && typeof valueB === 'string'
            ? valueA.localeCompare(valueB, l.lang)
            : valueA < valueB
              ? -1
              : valueA > valueB
                ? 1
                : 0;
        return order * sort!.dir;
      })
    : rows;
  const cellClass = (column: ColumnDef<R>) =>
    [column.num && 'num', column.wide && 'wide'].filter(Boolean).join(' ') || undefined;

  return (
    <div class="table-wrap">
      <div class="table-scroll">
        <table>
          {caption && <caption class="sr-only">{caption}</caption>}
          <thead>
            <tr>
              {columns.map((column) => {
                const active = column.key === sort?.key;
                return (
                  <th
                    aria-sort={active ? (sort!.dir > 0 ? 'ascending' : 'descending') : undefined}
                    class={cellClass(column)}
                    scope="col"
                  >
                    {column.sortable === false ? (
                      column.label
                    ) : (
                      <button
                        class="sort"
                        onClick={() =>
                          setSort({ dir: active ? (-sort!.dir as -1 | 1) : column.num ? -1 : 1, key: column.key })
                        }
                        type="button"
                      >
                        {column.label}
                        <span aria-hidden="true">{active ? (sort!.dir > 0 ? ' ↑' : ' ↓') : ''}</span>
                      </button>
                    )}
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {sorted.map((row, i) => (
              <tr class={limit && i >= limit ? 'extra' : undefined}>
                {columns.map((column) => (
                  <td class={cellClass(column)}>
                    {(column.render ? column.render(row) : cellValue(column, row)) ?? '–'}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {hiddenCount > 0 && (
        <label class="show-more">
          <input class="sr-only" type="checkbox" />
          <span class="more">{l.t('Show all {count}', { count: l.num(rows.length) })}</span>
          <span class="less">{l.t('Show fewer')}</span>
        </label>
      )}
    </div>
  );
}
