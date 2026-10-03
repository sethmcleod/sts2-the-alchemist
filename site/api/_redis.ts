export const INBOX = 'runs:inbox';

export async function redis(command: (number | string)[]) {
  const response = await fetch(process.env.KV_REST_API_URL!, {
    body: JSON.stringify(command),
    headers: { Authorization: `Bearer ${process.env.KV_REST_API_TOKEN}` },
    method: 'POST',
  });
  const { error, result } = (await response.json()) as { error?: string; result?: unknown };
  if (!response.ok || error) throw new Error(`Redis ${command[0]}: ${error ?? response.status}`);
  return result;
}
