const INBOX = 'runs:inbox';
const INBOX_LIMIT = 20_000;
const MAX_BODY = 400_000;

type Check = (value: unknown) => boolean;
const textUpTo =
  (max: number): Check =>
  (value) =>
    typeof value === 'string' && value.length > 0 && value.length <= max;
const wholeUpTo =
  (max: number): Check =>
  (value) =>
    Number.isInteger(value) && (value as number) >= 0 && (value as number) <= max;
const objectUpTo =
  (max: number): Check =>
  (value) =>
    typeof value === 'object' && value !== null && !Array.isArray(value) && JSON.stringify(value).length <= max;

const FIELDS: Record<string, Check> = {
  alchemist: objectUpTo(65_536),
  ascension: wholeUpTo(100),
  data: objectUpTo(262_144),
  epochs: wholeUpTo(1000),
  floor: wholeUpTo(1000),
  game_version: textUpTo(40),
  mod_version: textUpTo(40),
  player_hash: textUpTo(64),
  playtime: wholeUpTo(2 ** 31 - 1),
  victory: (value) => typeof value === 'boolean',
};
const DEFAULTS: Record<string, unknown> = { alchemist: {}, epochs: 0 };

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

export async function POST(request: Request) {
  const body = await request.text();
  if (body.length > MAX_BODY) return new Response('The run is too large.', { status: 413 });
  let run: Record<string, unknown>;
  try {
    run = JSON.parse(body);
  } catch {
    return new Response('The body is not JSON.', { status: 400 });
  }
  if (typeof run !== 'object' || run === null || Array.isArray(run))
    return new Response('The body is not one run.', { status: 400 });

  const row: Record<string, unknown> = { created_at: new Date().toISOString(), id: crypto.randomUUID() };
  for (const [name, check] of Object.entries(FIELDS)) {
    const value = run[name] ?? DEFAULTS[name];
    if (!check(value)) return new Response(`Missing or wrong: ${name}.`, { status: 400 });
    row[name] = value;
  }

  const queueLength = (await redis(['RPUSH', INBOX, JSON.stringify(row)])) as number;
  if (queueLength > INBOX_LIMIT) {
    await redis(['RPOP', INBOX]);
    return new Response('Too many runs are waiting. Try again tomorrow.', { status: 503 });
  }
  return new Response(null, { status: 201 });
}
