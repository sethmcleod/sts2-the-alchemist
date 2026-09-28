// Changelog text as parts: its markdown links and bare URLs become links, and "->" an arrow. The
// patch notes page and the feed (pages/notes.xml.ts) both show it this way

export type NotePart = { text: string } | { text: string; href: string };

const LINK = /\[([^\]]+)\]\((https?:[^)\s]+)\)|(https?:\/\/[^\s)]*[^\s).,])/g;

export function noteParts(source: string): NotePart[] {
  const text = source.replaceAll('->', '→');
  const parts: NotePart[] = [];
  let last = 0;
  for (const m of text.matchAll(LINK)) {
    parts.push({ text: text.slice(last, m.index) }, { text: m[1] ?? m[3], href: m[2] ?? m[3] });
    last = m.index + m[0].length;
  }
  parts.push({ text: text.slice(last) });
  return parts;
}
