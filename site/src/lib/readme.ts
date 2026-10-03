import fs from 'node:fs';
import path from 'node:path';
import { createSatteriMarkdownProcessor } from '@astrojs/markdown-satteri';
import { readmeHash, readmeSource } from '../i18n/catalog.mjs';

const readme = () => fs.readFileSync(path.join(process.cwd(), '..', 'README.md'), 'utf8');

let renderer: ReturnType<typeof createSatteriMarkdownProcessor> | undefined;

export async function readmeSection(title: string, translations?: Record<string, string>) {
  const english = readmeSource(title, readme());
  const translated = translations?.[readmeHash(english)];
  renderer ??= createSatteriMarkdownProcessor({ syntaxHighlight: false });
  const html = (await (await renderer).render(translated ?? english)).code;
  return { html, lang: translations && !translated ? 'en' : undefined };
}
