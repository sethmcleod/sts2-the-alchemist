import { gunzipSync } from 'node:zlib';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const blob = vi.hoisted(() => ({ put: vi.fn(), list: vi.fn() }));
vi.mock('@vercel/blob', () => blob);

import { POST } from '../../api/runs';
import { GET } from '../../api/pack';

const RUN = {
  mod_version: '0.14.22-beta',
  game_version: 'v0.99',
  victory: true,
  ascension: 3,
  floor: 51,
  playtime: 2400,
  player_hash: '0123456789abcdef',
  epochs: 4,
  data: { deck: [] },
  alchemist: { schema: 3 },
};

// A fake Redis (a list and keys), Supabase and deploy hook behind fetch
let inbox: string[];
let keys: Map<string, string>;
let supabase: { id: number }[] | Error;
let calls: string[];
function fakeFetch(url: string, init?: RequestInit) {
  calls.push(url);
  if (url === 'https://redis.test') {
    const [name, ...args] = JSON.parse(String(init!.body)) as [string, ...(string | number)[]];
    const result = {
      RPUSH: () => inbox.push(String(args[1])),
      RPOP: () => inbox.pop(),
      LRANGE: () => [...inbox],
      LTRIM: () => void (inbox = inbox.slice(Number(args[1]))),
      GET: () => keys.get(String(args[0])) ?? null,
      SET: () => void keys.set(String(args[0]), String(args[1])),
    }[name]!();
    return Response.json({ result });
  }
  if (url.startsWith('https://qgvpsvjvgpfweeouufbk.supabase.co/')) {
    if (supabase instanceof Error) return new Response('down', { status: 503 });
    const after = Number(new URL(url).searchParams.get('id')!.slice('gt.'.length));
    return Response.json(supabase.filter((row) => row.id > after));
  }
  if (url === 'https://hook.test') return new Response(null, { status: 201 });
  throw new Error(`unexpected fetch ${url}`);
}

beforeEach(() => {
  inbox = [];
  keys = new Map();
  supabase = [];
  calls = [];
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string, init?: RequestInit) => fakeFetch(url, init)),
  );
  vi.stubEnv('KV_REST_API_URL', 'https://redis.test');
  vi.stubEnv('KV_REST_API_TOKEN', 'token');
  vi.stubEnv('CRON_SECRET', 'secret');
  vi.stubEnv('DEPLOY_HOOK_URL', 'https://hook.test');
  vi.stubEnv('SUPABASE_READ_KEY', 'key');
  blob.put.mockReset();
  blob.list.mockReset().mockResolvedValue({ blobs: [], cursor: undefined });
});
afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

const upload = (body: unknown) =>
  POST(new Request('https://alchemist.fyi/api/runs', { method: 'POST', body: JSON.stringify(body) }));
const pack = (secret = 'secret') =>
  GET(new Request('https://alchemist.fyi/api/pack', { headers: { authorization: `Bearer ${secret}` } }));

describe('the upload endpoint', () => {
  it('queues a run with an id and a time, and only the known fields', async () => {
    const response = await upload({ ...RUN, extra: 'dropped' });
    expect(response.status).toBe(201);
    const row = JSON.parse(inbox[0]);
    expect(row).toEqual({ id: expect.any(String), created_at: expect.any(String), ...RUN });
  });

  it('fills in the fields older clients leave out', async () => {
    const { epochs, alchemist, ...older } = RUN;
    expect((await upload(older)).status).toBe(201);
    expect(JSON.parse(inbox[0])).toMatchObject({ epochs: 0, alchemist: {} });
  });

  it('names what is wrong with a run it refuses', async () => {
    const answer = async (body: unknown) => {
      const response = await upload(body);
      return [response.status, await response.text()];
    };
    expect(await answer({ ...RUN, victory: 'yes' })).toEqual([400, 'Missing or wrong: victory.']);
    expect(await answer({ ...RUN, floor: -1 })).toEqual([400, 'Missing or wrong: floor.']);
    expect(await answer([RUN])).toEqual([400, 'The body is not one run.']);
    const notJson = await POST(new Request('https://alchemist.fyi/api/runs', { method: 'POST', body: '{' }));
    expect(notJson.status).toBe(400);
    expect((await upload({ ...RUN, data: { pad: 'x'.repeat(500_000) } })).status).toBe(413);
    expect(inbox).toEqual([]);
  });

  it('turns runs away while the queue is flooded', async () => {
    inbox = Array.from({ length: 20_000 }, () => '{}');
    expect((await upload(RUN)).status).toBe(503);
    expect(inbox.length).toBe(20_000);
  });
});

describe('the daily pack', () => {
  const packed = () => {
    const [pathname, body, options] = blob.put.mock.calls[0];
    return {
      pathname,
      options,
      lines: gunzipSync(body)
        .toString()
        .trim()
        .split('\n')
        .map((line) => JSON.parse(line)),
    };
  };

  it('answers only the cron', async () => {
    expect((await pack('wrong')).status).toBe(401);
    expect(blob.put).not.toHaveBeenCalled();
  });

  it('moves the queue and the new Supabase rows into one private file, then starts a build', async () => {
    inbox = [JSON.stringify({ id: 'a' }), JSON.stringify({ id: 'b' })];
    supabase = [{ id: 1 }, { id: 2 }];
    blob.list.mockResolvedValue({ blobs: [{ pathname: 'runs/x.jsonl.gz', url: 'https://blob/x', size: 9 }] });
    const response = await pack();
    expect(await response.json()).toMatchObject({ packed: 4, fromSupabase: 2, built: true });
    const { pathname, options, lines } = packed();
    expect(pathname).toMatch(/^runs\/.+\.jsonl\.gz$/);
    expect(options).toMatchObject({ access: 'private' });
    expect(lines.map((line) => line.id)).toEqual(['sb-1', 'sb-2', 'a', 'b']);
    expect(inbox).toEqual([]);
    expect(keys.get('runs:supabase-seen')).toBe('2');
    expect(JSON.parse(keys.get('runs:files')!)).toEqual([
      { pathname: 'runs/x.jsonl.gz', url: 'https://blob/x', size: 9 },
    ]);
    expect(calls.at(-1)).toBe('https://hook.test');
  });

  it('copies only the Supabase rows it has not copied', async () => {
    keys.set('runs:supabase-seen', '1');
    supabase = [{ id: 1 }, { id: 2 }];
    await pack();
    expect(packed().lines.map((line) => line.id)).toEqual(['sb-2']);
  });

  it('keeps runs that arrive while it packs', async () => {
    inbox = [JSON.stringify({ id: 'a' })];
    blob.put.mockImplementation(async () => void inbox.push(JSON.stringify({ id: 'late' })));
    await pack();
    expect(inbox.map((line) => JSON.parse(line).id)).toEqual(['late']);
  });

  it('still packs the queue when Supabase is down', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    inbox = [JSON.stringify({ id: 'a' })];
    supabase = new Error('down');
    await pack();
    expect(packed().lines.map((line) => line.id)).toEqual(['a']);
  });

  it('leaves the queue alone when the file cannot be written', async () => {
    inbox = [JSON.stringify({ id: 'a' })];
    blob.put.mockRejectedValue(new Error('Blob is down'));
    await expect(pack()).rejects.toThrow('Blob is down');
    expect(inbox.length).toBe(1);
    expect(calls).not.toContain('https://hook.test');
  });

  it('does nothing when nothing is new', async () => {
    expect(await (await pack()).json()).toEqual({ packed: 0 });
    expect(blob.put).not.toHaveBeenCalled();
    expect(calls).not.toContain('https://hook.test');
  });
});
