import type { APIRoute } from 'astro';
import script from '../scripts/service-worker.js?raw';

// The browser installs a worker again only when its bytes change, so each build signs its own
const built = new Date().toISOString();

export const GET: APIRoute = () =>
  new Response(`${script}\n// Built ${built}\n`, { headers: { 'Content-Type': 'text/javascript' } });
