import { gzipSync } from 'node:zlib';
import { list, put } from '@vercel/blob';

const INBOX = 'runs:inbox';
const FILES = 'runs:files';

const SUPABASE_RUNS = 'https://qgvpsvjvgpfweeouufbk.supabase.co/rest/v1/runs';
const SUPABASE_LAST_ID = 'runs:supabase-seen';
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

async function newSupabaseRuns(): Promise<{ lastId?: number; lines: string[] }> {
  const key = process.env.SUPABASE_READ_KEY;
  if (!key) return { lines: [] };
  let lastId = Number((await redis(['GET', SUPABASE_LAST_ID])) ?? 0);
  const lines: string[] = [];
  for (;;) {
    const url = `${SUPABASE_RUNS}?select=${SUPABASE_COLUMNS}&id=gt.${lastId}&order=id.asc&limit=1000`;
    const response = await fetch(url, { headers: { apikey: key, Authorization: `Bearer ${key}` } });
    if (!response.ok) throw new Error(`Supabase: ${response.status} ${await response.text()}`);
    const rows = (await response.json()) as { id: number }[];
    for (const { id, ...row } of rows) lines.push(JSON.stringify({ id: `sb-${id}`, ...row }));
    if (rows.length) lastId = rows.at(-1)!.id;
    if (rows.length < 1000) return { lastId, lines };
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
  let supabase: Awaited<ReturnType<typeof newSupabaseRuns>> = { lines: [] };
  try {
    supabase = await newSupabaseRuns();
  } catch (error) {
    console.error(error);
  }

  const lines = [...supabase.lines, ...queued];
  if (!lines.length) return Response.json({ packed: 0 });
  const pathname = `runs/${new Date().toISOString().replace(/[:.]/g, '-')}.jsonl.gz`;
  await put(pathname, gzipSync(lines.join('\n') + '\n'), { access: 'private', contentType: 'application/gzip' });
  await redis(['SET', FILES, JSON.stringify(await packedFiles())]);
  if (queued.length) await redis(['LTRIM', INBOX, queued.length, -1]);
  if (supabase.lastId) await redis(['SET', SUPABASE_LAST_ID, supabase.lastId]);

  const deployHook = process.env.DEPLOY_HOOK_URL;
  if (!deployHook) console.error('DEPLOY_HOOK_URL is not set, so no build was started.');
  const built = deployHook ? (await fetch(deployHook, { method: 'POST' })).ok : false;
  return Response.json({ built, fromSupabase: supabase.lines.length, packed: lines.length, pathname });
}
