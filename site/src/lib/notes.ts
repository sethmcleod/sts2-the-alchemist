export type NotePart = { href: string; text: string } | { text: string };

const LINK = /\[([^\]]+)\]\((https?:[^)\s]+)\)|(https?:\/\/[^\s)]*[^\s).,])/g;

export function noteParts(source: string): NotePart[] {
  const text = source.replaceAll('->', '→');
  const parts: NotePart[] = [];
  let textStart = 0;
  for (const match of text.matchAll(LINK)) {
    parts.push(
      { text: text.slice(textStart, match.index) },
      { href: match[2] ?? match[3], text: match[1] ?? match[3] },
    );
    textStart = match.index + match[0].length;
  }
  parts.push({ text: text.slice(textStart) });
  return parts;
}
