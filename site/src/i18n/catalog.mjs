// The site's English, as the list src/i18n/<code>.json translates. Each build writes it to
// src/i18n/en.json: every literal l.t() and l.n() string in the source, every other string the pages
// looked up (Lang records them while it renders, such as names from a table), and the README
// sections the home page shows. `npm run strings` compares a language with it.
//
// "stats" holds the strings the islands use (they get only that part), "site" the rest, and
// "readme" each README section by the hash of its Markdown (src/lib/readme.ts hashes it the same way).

import { createHash } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const SITE = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const SRC = path.join(SITE, 'src');
const README = path.join(SITE, '..', 'README.md');
export const CATALOG = path.join(SRC, 'i18n', 'en.json');
// The home page shows these README sections
export const README_SECTIONS = ['Playstyle', 'Disclaimer'];
// Code that runs in the browser, where only the stats strings exist
const ISLAND_CODE = /^(islands|components\/charts|lib\/views)\/|^lib\/(bars|runs|lang)\.ts$/;

const SITE_END = '<!-- The website shows this section up to here -->';

/** A README section's Markdown: the lines between its "## Title" and the next "## " heading, or the
 *  site's end marker */
export function readmeSource(title, text = fs.readFileSync(README, 'utf8')) {
  const start = text.indexOf(`\n## ${title}\n`);
  if (start < 0) throw new Error(`README.md has no "## ${title}" section`);
  const body = text.slice(start + title.length + 5);
  const end = body.search(/^## /m);
  return (end < 0 ? body : body.slice(0, end)).split(SITE_END)[0].trim();
}

export const readmeHash = (markdown) => createHash('sha256').update(markdown).digest('hex').slice(0, 12);

/** Every string literal passed to .t(...) or as the plural forms of .n(count, one, other) */
function literals(code) {
  const found = [];
  const string = /\s*(['"`])((?:\\.|(?!\1)[^\\])*)\1/y;
  const read = (at) => {
    string.lastIndex = at;
    const m = string.exec(code);
    return m && !(m[1] === '`' && m[2].includes('${'))
      ? { text: m[2].replace(/\\(.)/g, '$1'), end: string.lastIndex }
      : null;
  };
  for (const call of code.matchAll(/\.(t|n)\(/g)) {
    let at = call.index + call[0].length;
    if (call[1] === 'n') {
      // Skip the count: everything up to the first comma outside brackets
      let depth = 0;
      for (; at < code.length; at++) {
        const ch = code[at];
        if ('([{'.includes(ch)) depth++;
        else if (')]}'.includes(ch)) depth--;
        else if (ch === ',' && depth === 0) break;
      }
      const one = read(at + 1);
      const other = one && code[one.end] === ',' ? read(one.end + 1) : null;
      if (other) found.push(other.text);
    } else {
      const key = read(at);
      if (key) found.push(key.text);
    }
  }
  return found;
}

function sourceFiles(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return sourceFiles(full);
    return /\.(astro|ts|tsx|mjs)$/.test(entry.name) && !entry.name.endsWith('.test.ts') ? [full] : [];
  });
}

/** The catalog from what the build looked up, as { key: Set of "site" | "stats" } */
export function buildCatalog(seen = new Map()) {
  const scopes = new Map([...seen].map(([key, where]) => [key, new Set(where)]));
  for (const file of sourceFiles(SRC)) {
    const relative = path.relative(SRC, file);
    const scope = ISLAND_CODE.test(relative) ? 'stats' : 'site';
    for (const key of literals(fs.readFileSync(file, 'utf8'))) {
      if (!scopes.has(key)) scopes.set(key, new Set());
      scopes.get(key).add(scope);
    }
  }
  const sorted = (scope) =>
    [...scopes]
      .filter(([, where]) => (scope === 'stats' ? where.has('stats') : !where.has('stats')))
      .map(([key]) => key)
      .sort();
  return {
    site: sorted('site'),
    stats: sorted('stats'),
    readme: Object.fromEntries(README_SECTIONS.map((title) => [readmeHash(readmeSource(title)), title])),
  };
}

/** An Astro integration that writes the catalog after each build */
export const catalog = () => ({
  name: 'site-strings',
  hooks: {
    'astro:build:done': ({ logger }) => {
      const found = buildCatalog(globalThis.__siteStrings);
      const text = `${JSON.stringify(found, null, 2)}\n`;
      if (!fs.existsSync(CATALOG) || fs.readFileSync(CATALOG, 'utf8') !== text) fs.writeFileSync(CATALOG, text);
      logger.info(`${found.site.length + found.stats.length} strings in src/i18n/en.json`);
    },
  },
});
