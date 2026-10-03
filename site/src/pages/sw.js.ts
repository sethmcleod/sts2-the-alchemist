import type { APIRoute } from 'astro';
import script from '../scripts/service-worker.js?raw';

const buildTime = new Date().toISOString();

export const GET: APIRoute = () =>
  new Response(`${script}\n// Built ${buildTime}\n`, { headers: { 'Content-Type': 'text/javascript' } });
