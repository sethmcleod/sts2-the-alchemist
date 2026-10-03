// The game's loc markup: [gold] keywords, [green] upgraded numbers, [energy] icons and line
// breaks. Other tags (the wobble and shake effects) are dropped.

export type Token =
  { color?: string; kind: 'text'; text: string } | { count: number; kind: 'energy' } | { kind: 'break' };

const COLORS = new Set(['gold', 'green', 'blue', 'red', 'purple', 'pink', 'orange', 'aqua']);

export function tokens(markup: string): Token[] {
  const out: Token[] = [];
  // Colours nest: text after an inner [/gold] takes the colour around it again
  const colors: string[] = [];
  for (const part of markup.split(/(\[\/?\w+\]|\n)/)) {
    if (!part) continue;
    const last = out.at(-1);
    if (part === '\n') {
      out.push({ kind: 'break' });
    } else if (part === '[energy]') {
      if (last?.kind === 'energy') last.count++;
      else out.push({ count: 1, kind: 'energy' });
    } else if (/^\[\/?\w+\]$/.test(part)) {
      const name = part.replace(/[[\]/]/g, '');
      if (COLORS.has(name)) {
        if (part.startsWith('[/')) colors.pop();
        else colors.push(name);
      }
    } else {
      out.push({ color: colors.at(-1), kind: 'text', text: part });
    }
  }
  return out;
}

/** Two icons in a row read "2 Energy". One icon follows its number, if it has one. `energy` is the game's word */
export const energyWords = (count: number, energy = 'Energy') => (count > 1 ? ` ${count} ${energy}` : ` ${energy}`);

/** The words alone, for search, alt text and meta descriptions */
export const plain = (markup: string, energy?: string) =>
  tokens(markup)
    .map((t) => (t.kind === 'text' ? t.text : t.kind === 'energy' ? energyWords(t.count, energy) : ' '))
    .join('')
    .replace(/\s+/g, ' ')
    .trim();
