// Once a day (vercel.json crons), moves the runs that api/runs.ts queued in Redis into one gzipped
// JSON Lines file in Blob, keeps the list of those files in Redis for the export
// (tools/analytics/common.py), and asks Vercel for a site build. A step that fails leaves the queue
// as it was, so the next day packs the same runs again; the export drops a run it has seen by its id.

import { gzipSync } from 'node:zlib';
import { list, put } from '@vercel/blob';

const INBOX = 'runs:inbox';
const FILES = 'runs:files';

// Mod versions from before this endpoint post to Supabase. Each pack copies the rows it has not
// copied yet, until the project is deleted
const SUPABASE_RUNS = 'https://qgvpsvjvgpfweeouufbk.supabase.co/rest/v1/runs';
const SUPABASE_SEEN = 'runs:supabase-seen';
const SUPABASE_COLUMNS =
  'id,created_at,mod_version,game_version,victory,ascension,floor,playtime,player_hash,epochs,data,alchemist';

async function redis(command: (number | string)[]) {
  const response = await fetch(process.env.KV_REST_API_URL!, {
    body: JSON.stringify(command),
    headers: { Authorization: `Bearer ${process.env.KV_REST_API_TOKEN}` },
    method: 'POST',
  });
  const { error, result } = (await response.json()) as { error?: string; result?: unknown };
  if (!response.ok || error) throw new Error(`Redis ${command[0]}: ${error ?? response.status}`);
  return result;
}

async function fromSupabase(): Promise<{ lines: string[]; seen?: number }> {
  const key = process.env.SUPABASE_READ_KEY;
  if (!key) return { lines: [] };
  let seen = Number((await redis(['GET', SUPABASE_SEEN])) ?? 0);
  const lines: string[] = [];
  for (;;) {
    const url = `${SUPABASE_RUNS}?select=${SUPABASE_COLUMNS}&id=gt.${seen}&order=id.asc&limit=1000`;
    const response = await fetch(url, { headers: { apikey: key, Authorization: `Bearer ${key}` } });
    if (!response.ok) throw new Error(`Supabase: ${response.status} ${await response.text()}`);
    const rows = (await response.json()) as { id: number }[];
    for (const { id, ...row } of rows) lines.push(JSON.stringify({ id: `sb-${id}`, ...row }));
    if (rows.length) seen = rows.at(-1)!.id;
    if (rows.length < 1000) return { lines, seen };
  }
}

async function packedFiles() {
  const files = [];
  let cursor: string | undefined;
  do {
    const page = await list({ cursor, prefix: 'runs/' });
    files.push(...page.blobs.map(({ pathname, size, url }) => ({ pathname, size, url })));
    cursor = page.cursor;
  } while (cursor);
  return files.filter((file) => file.pathname.endsWith('.jsonl.gz'));
}

export async function GET(request: Request) {
  const secret = process.env.CRON_SECRET;
  if (!secret || request.headers.get('authorization') !== `Bearer ${secret}`)
    return new Response('Unauthorized', { status: 401 });

  const queued = (await redis(['LRANGE', INBOX, 0, -1])) as string[];
  let older: Awaited<ReturnType<typeof fromSupabase>> = { lines: [] };
  try {
    older = await fromSupabase();
  } catch (error) {
    console.error(error);
  }

  const lines = [...older.lines, ...queued];
  if (!lines.length) return Response.json({ packed: 0 });
  const pathname = `runs/${new Date().toISOString().replace(/[:.]/g, '-')}.jsonl.gz`;
  await put(pathname, gzipSync(lines.join('\n') + '\n'), { access: 'private', contentType: 'application/gzip' });
  await redis(['SET', FILES, JSON.stringify(await packedFiles())]);
  if (queued.length) await redis(['LTRIM', INBOX, queued.length, -1]);
  if (older.seen) await redis(['SET', SUPABASE_SEEN, older.seen]);

  const hook = process.env.DEPLOY_HOOK_URL;
  if (!hook) console.error('DEPLOY_HOOK_URL is not set, so no build was started.');
  const built = hook ? (await fetch(hook, { method: 'POST' })).ok : false;
  return Response.json({ built, fromSupabase: older.lines.length, packed: lines.length, pathname });
}
