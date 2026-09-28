// The patch notes as an Atom feed, for a feed reader or a Discord bot to follow. In English, like the
// notes themselves, and with the newest releases only

import type { APIRoute } from 'astro';
import { releases } from '../lib/content';
import { notesHref } from '../lib/links';
import { noteParts } from '../lib/notes';
import type { NoteItem } from '../lib/types';

const NEWEST = 30;

const escape = (text: string) =>
  text.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!);

const line = (text: string) =>
  noteParts(text)
    .map((part) => ('href' in part ? `<a href="${escape(part.href)}">${escape(part.text)}</a>` : escape(part.text)))
    .join('');

const list = (items: NoteItem[]): string =>
  `<ul>${items.map((item) => `<li>${line(item.text)}${item.items.length ? list(item.items) : ''}</li>`).join('')}</ul>`;

export const GET: APIRoute = ({ site }) => {
  const url = (path: string) => new URL(path, site).href;
  const dated = releases().filter((release) => release.date);
  const entries = dated.slice(0, NEWEST).map((release) => {
    const body = [
      ...release.intro.map((paragraph) => `<p>${line(paragraph)}</p>`),
      ...release.sections.map((s) => (s.title ? `<h3>${escape(s.title)}</h3>` : '') + list(s.items)),
    ].join('');
    return `  <entry>
    <title>${escape(release.version)}</title>
    <id>${url(notesHref(release.version))}</id>
    <link href="${url(notesHref(release.version))}"/>
    <updated>${release.date}T00:00:00Z</updated>
    <content type="html">${escape(body)}</content>
  </entry>`;
  });
  const feed = `<?xml version="1.0" encoding="utf-8"?>
<feed xmlns="http://www.w3.org/2005/Atom" xml:lang="en">
  <title>The Alchemist patch notes</title>
  <subtitle>Every release of The Alchemist, a Slay the Spire 2 character mod.</subtitle>
  <id>${url('/notes')}</id>
  <link rel="self" href="${url('/notes.xml')}"/>
  <link rel="alternate" type="text/html" href="${url('/notes')}"/>
  <updated>${dated[0]?.date ?? '2026-01-01'}T00:00:00Z</updated>
  <author><name>Seth</name></author>
${entries.join('\n')}
</feed>
`;
  return new Response(feed, { headers: { 'Content-Type': 'application/atom+xml; charset=utf-8' } });
};
