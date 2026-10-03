import type { APIRoute, GetStaticPaths } from 'astro';
import { compact } from '../../lib/compact';
import { dataFile, runs } from '../../lib/content';
import { EXTRA_FILES } from '../../lib/runs';

export const getStaticPaths = (() =>
  ['summary.json', ...new Set(Object.values(EXTRA_FILES).map(([file]) => file))].map((file) => ({
    params: { file: file.replace(/\.json$/, '') },
  }))) satisfies GetStaticPaths;

export const GET: APIRoute = ({ params }) =>
  Response.json({ ...compact(dataFile(`${params.file}.json`)), built: runs().meta.generated_at });
