// A table whose column headers sort it. Columns marked `wide` hide on a narrow screen, and rows
// past `limit` wait behind a "Show all" box.

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
  sort: initial,
}: Props<R>) {
  const [sort, setSort] = useState(initial);
  if (!rows.length) return <p class="empty">{empty ?? emptyText(l)}</p>;

  const valueOf = (c: ColumnDef<R>, row: R) => (row as Record<string, unknown>)[c.key] as null | number | string;
  const column = sort && columns.find((c) => c.key === sort.key);
  const sorted = column
    ? [...rows].sort((a, b) => {
        const [va, vb] = [valueOf(column, a), valueOf(column, b)];
        if (va == null) return 1;
        if (vb == null) return -1;
        const order =
          typeof va === 'string' && typeof vb === 'string'
            ? va.localeCompare(vb, l.lang)
            : va < vb
              ? -1
              : va > vb
                ? 1
                : 0;
        return order * sort!.dir;
      })
    : rows;
  const classOf = (c: ColumnDef<R>) => [c.num && 'num', c.wide && 'wide'].filter(Boolean).join(' ') || undefined;

  return (
    <div class="table-wrap">
      <div class="table-scroll">
        <table>
          {caption && <caption class="sr-only">{caption}</caption>}
          <thead>
            <tr>
              {columns.map((c) => {
                const active = c.key === sort?.key;
                return (
                  <th
                    aria-sort={active ? (sort!.dir > 0 ? 'ascending' : 'descending') : undefined}
                    class={classOf(c)}
                    scope="col"
                  >
                    {c.sortable === false ? (
                      c.label
                    ) : (
                      <button
                        class="sort"
                        onClick={() => setSort({ dir: active ? (-sort!.dir as -1 | 1) : c.num ? -1 : 1, key: c.key })}
                        type="button"
                      >
                        {c.label}
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
                {columns.map((c) => (
                  <td class={classOf(c)}>{(c.render ? c.render(row) : valueOf(c, row)) ?? '–'}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {limit && rows.length > limit && (
        <label class="show-more">
          <input class="sr-only" type="checkbox" />
          <span class="more">{l.t('Show all {count}', { count: l.num(rows.length) })}</span>
          <span class="less">{l.t('Show fewer')}</span>
        </label>
      )}
    </div>
  );
}
