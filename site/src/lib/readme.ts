import { createHash } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { createSatteriMarkdownProcessor } from '@astrojs/markdown-satteri';

const README = path.join(process.cwd(), '..', 'README.md');

const SITE_END = '<!-- The website shows this section up to here -->';

export function readmeSource(title: string, text = fs.readFileSync(README, 'utf8')) {
  const heading = `\n## ${title}\n`;
  const start = text.indexOf(heading);
  if (start < 0) throw new Error(`README.md has no "## ${title}" section`);
  const body = text.slice(start + heading.length);
  const end = body.search(/^## /m);
  return (end < 0 ? body : body.slice(0, end)).split(SITE_END)[0]!.trim();
}

export const readmeHash = (markdown: string) => createHash('sha256').update(markdown).digest('hex').slice(0, 12);

let renderer: ReturnType<typeof createSatteriMarkdownProcessor> | undefined;

export async function readmeSection(title: string, translations?: Record<string, string>) {
  const english = readmeSource(title);
  const translated = translations?.[readmeHash(english)];
  renderer ??= createSatteriMarkdownProcessor({ syntaxHighlight: false });
  const html = (await (await renderer).render(translated ?? english)).code;
  return { html, lang: translations && !translated ? 'en' : undefined };
}
