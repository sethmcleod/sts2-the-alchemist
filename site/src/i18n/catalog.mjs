import { createHash } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const SITE = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const SRC = path.join(SITE, 'src');
const README = path.join(SITE, '..', 'README.md');
export const CATALOG_PATH = path.join(SRC, 'i18n', 'en.json');
export const README_SECTIONS = ['Playstyle', 'Disclaimer'];
const BROWSER_CODE = /^(islands|components\/charts|lib\/views)\/|^lib\/(bars|runs|lang)\.ts$/;

const SITE_END = '<!-- The website shows this section up to here -->';

export function readmeSource(title, text = fs.readFileSync(README, 'utf8')) {
  const start = text.indexOf(`\n## ${title}\n`);
  if (start < 0) throw new Error(`README.md has no "## ${title}" section`);
  const body = text.slice(start + title.length + 5);
  const end = body.search(/^## /m);
  return (end < 0 ? body : body.slice(0, end)).split(SITE_END)[0].trim();
}

export const readmeHash = (markdown) => createHash('sha256').update(markdown).digest('hex').slice(0, 12);

function literalKeys(code) {
  const found = [];
  const stringLiteral = /\s*(['"`])((?:\\.|(?!\1)[^\\])*)\1/y;
  const readLiteral = (at) => {
    stringLiteral.lastIndex = at;
    const match = stringLiteral.exec(code);
    return match && !(match[1] === '`' && match[2].includes('${'))
      ? { end: stringLiteral.lastIndex, text: match[2].replace(/\\(.)/g, '$1') }
      : null;
  };
  for (const call of code.matchAll(/\.(t|n)\(/g)) {
    let at = call.index + call[0].length;
    if (call[1] === 'n') {
      let depth = 0;
      for (; at < code.length; at++) {
        const char = code[at];
        if ('([{'.includes(char)) depth++;
        else if (')]}'.includes(char)) depth--;
        else if (char === ',' && depth === 0) break;
      }
      const one = readLiteral(at + 1);
      const other = one && code[one.end] === ',' ? readLiteral(one.end + 1) : null;
      if (other) found.push(other.text);
    } else {
      const key = readLiteral(at);
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

export function buildCatalog(lookedUp = new Map()) {
  const scopes = new Map([...lookedUp].map(([key, where]) => [key, new Set(where)]));
  for (const file of sourceFiles(SRC)) {
    const relative = path.relative(SRC, file);
    const scope = BROWSER_CODE.test(relative) ? 'stats' : 'site';
    for (const key of literalKeys(fs.readFileSync(file, 'utf8'))) {
      if (!scopes.has(key)) scopes.set(key, new Set());
      scopes.get(key).add(scope);
    }
  }
  const keysIn = (scope) =>
    [...scopes]
      .filter(([, where]) => (scope === 'stats' ? where.has('stats') : !where.has('stats')))
      .map(([key]) => key)
      .sort();
  return {
    readme: Object.fromEntries(README_SECTIONS.map((title) => [readmeHash(readmeSource(title)), title])),
    site: keysIn('site'),
    stats: keysIn('stats'),
  };
}

export const catalog = () => ({
  hooks: {
    'astro:build:done': ({ logger }) => {
      const found = buildCatalog(globalThis.__siteStrings);
      const text = `${JSON.stringify(found, null, 2)}\n`;
      if (!fs.existsSync(CATALOG_PATH) || fs.readFileSync(CATALOG_PATH, 'utf8') !== text)
        fs.writeFileSync(CATALOG_PATH, text);
      logger.info(`${found.site.length + found.stats.length} strings in src/i18n/en.json`);
    },
  },
  name: 'site-strings',
});
