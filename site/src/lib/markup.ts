export type Token =
  { color?: string; kind: 'text'; text: string } | { count: number; kind: 'energy' } | { kind: 'break' };

const COLORS = new Set(['gold', 'green', 'blue', 'red', 'purple', 'pink', 'orange', 'aqua']);

export function tokens(markup: string): Token[] {
  const result: Token[] = [];
  const openColors: string[] = [];
  for (const part of markup.split(/(\[\/?\w+\]|\n)/)) {
    if (!part) continue;
    const previous = result.at(-1);
    if (part === '\n') {
      result.push({ kind: 'break' });
    } else if (part === '[energy]') {
      if (previous?.kind === 'energy') previous.count++;
      else result.push({ count: 1, kind: 'energy' });
    } else if (/^\[\/?\w+\]$/.test(part)) {
      const tag = part.replace(/[[\]/]/g, '');
      if (COLORS.has(tag)) {
        if (part.startsWith('[/')) openColors.pop();
        else openColors.push(tag);
      }
    } else {
      result.push({ color: openColors.at(-1), kind: 'text', text: part });
    }
  }
  return result;
}

export const energyWords = (count: number, energy = 'Energy') => (count > 1 ? ` ${count} ${energy}` : ` ${energy}`);

export const plain = (markup: string, energy?: string) =>
  tokens(markup)
    .map((token) =>
      token.kind === 'text' ? token.text : token.kind === 'energy' ? energyWords(token.count, energy) : ' ',
    )
    .join('')
    .replace(/\s+/g, ' ')
    .trim();
