// The mod describes itself in the repo README, which the Steam Workshop page follows as well. The
// site renders README sections from their Markdown, so an edit there reaches the site on its own. A
// section can end early for the site: at a line "<!-- The website shows this section up to here -->".
// A translation of a section is Markdown, keyed by the hash of the English Markdown it translates:
// after an edit to the English, the section shows in English until it is translated again.

import { createSatteriMarkdownProcessor } from '@astrojs/markdown-satteri';
import { createHash } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';

const README = path.join(process.cwd(), '..', 'README.md');

const SITE_END = '<!-- The website shows this section up to here -->';

/**
 * A section's Markdown: the lines between its "## Title" and the next "## " heading, or the site's
 * end marker. The string catalog (src/i18n/catalog.mjs) reads and hashes a section the same way
 */
export function readmeSource(title: string, text = fs.readFileSync(README, 'utf8')) {
  const start = text.indexOf(`\n## ${title}\n`);
  if (start < 0) throw new Error(`README.md has no "## ${title}" section`);
  const body = text.slice(start + title.length + 5);
  const end = body.search(/^## /m);
  return (end < 0 ? body : body.slice(0, end)).split(SITE_END)[0]!.trim();
}

/** The key of a section's translation: the first 12 hex digits of the SHA-256 of its Markdown */
export const readmeHash = (markdown: string) => createHash('sha256').update(markdown).digest('hex').slice(0, 12);

let renderer: ReturnType<typeof createSatteriMarkdownProcessor> | undefined;

/**
 * A "## Title" section of the README as HTML, without its heading. With `translations` (Markdown by
 * readmeHash), the section's translation, or the English with `lang: 'en'` when there is none
 */
export async function readmeSection(title: string, translations?: Record<string, string>) {
  const english = readmeSource(title);
  const translated = translations?.[readmeHash(english)];
  renderer ??= createSatteriMarkdownProcessor({ syntaxHighlight: false });
  const html = (await (await renderer).render(translated ?? english)).code;
  return { html, lang: translations && !translated ? 'en' : undefined };
}
