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
  wide?: boolean;
  sortable?: boolean;
  render?: (row: R) => ComponentChildren;
}

export type Sort = { key: string; dir: 1 | -1 };

interface Props<R> {
  l: Lang;
  columns: ColumnDef<R>[];
  rows: R[];
  sort?: Sort;
  limit?: number;
  empty?: string;
  caption?: string;
}

export default function DataTable<R extends object>({
  l,
  columns,
  rows,
  sort: initial,
  limit,
  empty,
  caption,
}: Props<R>) {
  const [sort, setSort] = useState(initial);
  if (!rows.length) return <p class="empty">{empty ?? emptyText(l)}</p>;

  const valueOf = (c: ColumnDef<R>, row: R) => (row as Record<string, unknown>)[c.key] as number | string | null;
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
                    scope="col"
                    class={classOf(c)}
                    aria-sort={active ? (sort!.dir > 0 ? 'ascending' : 'descending') : undefined}
                  >
                    {c.sortable === false ? (
                      c.label
                    ) : (
                      <button
                        type="button"
                        class="sort"
                        onClick={() => setSort({ key: c.key, dir: active ? (-sort!.dir as 1 | -1) : c.num ? -1 : 1 })}
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
          <input type="checkbox" class="sr-only" />
          <span class="more">{l.t('Show all {count}', { count: l.num(rows.length) })}</span>
          <span class="less">{l.t('Show fewer')}</span>
        </label>
      )}
    </div>
  );
}
