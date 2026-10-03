import type { APIRoute } from 'astro';
import { releases } from '../lib/content';
import { notesHref } from '../lib/links';
import { noteParts } from '../lib/notes';
import type { NoteItem } from '../lib/types';

const FEED_LENGTH = 30;

const escape = (text: string) =>
  text.replace(/[&<>"']/g, (char) => ({ '"': '&quot;', '&': '&amp;', "'": '&#39;', '<': '&lt;', '>': '&gt;' })[char]!);

const noteHtml = (text: string) =>
  noteParts(text)
    .map((part) => ('href' in part ? `<a href="${escape(part.href)}">${escape(part.text)}</a>` : escape(part.text)))
    .join('');

const listHtml = (items: NoteItem[]): string =>
  `<ul>${items.map((item) => `<li>${noteHtml(item.text)}${item.items.length ? listHtml(item.items) : ''}</li>`).join('')}</ul>`;

export const GET: APIRoute = ({ site }) => {
  const absolute = (path: string) => new URL(path, site).href;
  const datedReleases = releases().filter((release) => release.date);
  const entries = datedReleases.slice(0, FEED_LENGTH).map((release) => {
    const body = [
      ...release.intro.map((paragraph) => `<p>${noteHtml(paragraph)}</p>`),
      ...release.sections.map(
        (section) => (section.title ? `<h3>${escape(section.title)}</h3>` : '') + listHtml(section.items),
      ),
    ].join('');
    return `  <entry>
    <title>${escape(release.version)}</title>
    <id>${absolute(notesHref(release.version))}</id>
    <link href="${absolute(notesHref(release.version))}"/>
    <updated>${release.date}T00:00:00Z</updated>
    <content type="html">${escape(body)}</content>
  </entry>`;
  });
  const feed = `<?xml version="1.0" encoding="utf-8"?>
<feed xmlns="http://www.w3.org/2005/Atom" xml:lang="en">
  <title>The Alchemist patch notes</title>
  <subtitle>Every release of The Alchemist, a Slay the Spire 2 character mod.</subtitle>
  <id>${absolute('/notes')}</id>
  <link rel="self" href="${absolute('/notes.xml')}"/>
  <link rel="alternate" type="text/html" href="${absolute('/notes')}"/>
  <updated>${datedReleases[0]?.date ?? '2026-01-01'}T00:00:00Z</updated>
  <author><name>The Alchemist</name></author>
${entries.join('\n')}
</feed>
`;
  return new Response(feed, { headers: { 'Content-Type': 'application/atom+xml; charset=utf-8' } });
};
