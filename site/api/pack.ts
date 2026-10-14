import { gzipSync } from 'node:zlib';
import { list, put } from '@vercel/blob';
import { INBOX, redis } from './_redis.js';

const FILES = 'runs:files';

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
  if (!queued.length) return Response.json({ packed: 0 });
  const pathname = `runs/${new Date().toISOString().replace(/[:.]/g, '-')}.jsonl.gz`;
  await put(pathname, gzipSync(queued.join('\n') + '\n'), { access: 'private', contentType: 'application/gzip' });
  await redis(['SET', FILES, JSON.stringify(await packedFiles())]);
  await redis(['LTRIM', INBOX, queued.length, -1]);

  const deployHook = process.env.DEPLOY_HOOK_URL;
  if (!deployHook) console.error('DEPLOY_HOOK_URL is not set, so no build was started.');
  const built = deployHook ? (await fetch(deployHook, { method: 'POST' })).ok : false;
  return Response.json({ built, packed: queued.length, pathname });
}
