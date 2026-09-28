// Each card's, relic's and potion's own lines from the patch notes. A line counts for an item when it
// names it: "Buffed Wallop card: ...", "Changed All At Once and Wormwood cards: ...", "Quench now
// properly ...", "Buffed Spike's Decant". Renames are followed back, so a card's history includes the
// lines from before it had its name, and a name used again later (by a rename that reuses it, or a new
// item added under a removed one's name) only counts for the item that has it at the time.

import type { NoteItem, Release } from './types';

export interface Change {
  version: string;
  item: NoteItem;
}

interface Alias {
  id: string;
  name: string;
  /** The newest and oldest release it was in use, as positions in the notes (0 is the newest) */
  newest: number;
  oldest: number;
}

const escape = (text: string) => text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const KIND = String.raw`(?:the )?(.+?)(?: (?:card|relic|potion|power|enchantment|tip|section))?`;
const RENAME = new RegExp(`^${KIND} to ${KIND}$`);
// What can follow the names in a rename line: "Renamed Anoint to Spike and reworked it into ..."
const AFTER = /(?:,? and (?:reworked|tinted)|, reusing|, because|, since|:).*$/;

/** The renames in the notes, newest first: "Renamed Purge to Double Dose, Pays Off to Smelling Salts, and Elixir to Panacea" */
export function renames(versions: Release[]) {
  const found: { from: string; to: string; at: number }[] = [];
  versions.forEach((release, at) =>
    each(release, (item) => {
      if (!item.text.startsWith('Renamed ')) return;
      const names = item.text.slice('Renamed '.length).replace(AFTER, '');
      for (const part of names.split(/,? and |, /)) {
        const m = RENAME.exec(part.trim());
        if (m) found.push({ from: m[1], to: m[2], at });
      }
    }),
  );
  return found;
}

/** The lines that add an item, newest first: "Added Toadstone relic (Uncommon): ..." */
function additions(versions: Release[]) {
  const found: { text: string; at: number }[] = [];
  versions.forEach((release, at) =>
    each(release, (item) => {
      if (item.text.startsWith('Added ')) found.push({ text: item.text, at });
    }),
  );
  return found;
}

function each(release: Release, visit: (item: NoteItem, parent: NoteItem | null) => void) {
  const walk = (items: NoteItem[], parent: NoteItem | null) =>
    items.forEach((item) => {
      visit(item, parent);
      walk(item.items, item);
    });
  release.sections.forEach((section) => walk(section.items, null));
}

/** Every name an item has had, and the versions it had each one */
function aliases(names: Record<string, string>, versions: Release[]): Alias[] {
  const all = renames(versions);
  const added = additions(versions);
  const last = versions.length - 1;
  const found: Alias[] = [];
  const add = (id: string, name: string, newest: number) => {
    // A name holds back to the rename that gave it or the line that added the item, and not past an
    // older rename that took the same name away from something else (the name was free again, then
    // reused)
    const given = all.find((r) => r.to === name && r.at >= newest);
    const taken = all.find((r) => r.from === name && r.at > newest);
    const addedAs = new RegExp(`^Added (?:the )?${escape(name)}(?![\\w'])`);
    const born = added.find((a) => a.at >= newest && addedAs.test(a.text));
    const oldest = Math.min(given ? given.at : last, taken ? taken.at - 1 : last, born ? born.at : last);
    found.push({ id, name, newest, oldest });
    if (given && given.at <= oldest) add(id, given.from, given.at);
  };
  for (const [id, name] of Object.entries(names)) add(id, name, 0);
  return found;
}

// A name right after another capitalized word is the end of a longer name (Extra Dose, Potent
// Strike), unless that word is the line's verb (Buffed Dose, Reworked Spike)
const LONGER = /(?:^|[^\w'])[A-Z][\w'-]*(?<!ed) $/;

/** item id -> its lines in the notes, newest first. A line under another line comes with its parent
 *  when the parent names the item */
export function changes(names: Record<string, string>, versions: Release[]): Map<string, Change[]> {
  const known = aliases(names, versions).sort((a, b) => b.name.length - a.name.length);
  // A name ends at a word's end, and may take a possessive: Spike's
  const patterns = new Map(
    known.map((a) => [a.name, new RegExp(`(?<![\\w'])${escape(a.name)}(?!\\w|'(?!s\\b))`, 'g')]),
  );
  const found = new Map<string, Change[]>();
  const add = (id: string, change: Change) => {
    const list = found.get(id) ?? [];
    if (!list.some((c) => c.item === change.item)) list.push(change);
    found.set(id, list);
  };

  versions.forEach((release, at) => {
    const named = new Map<NoteItem, Set<string>>();
    each(release, (item, parent) => {
      // Longer names first, and a longer name hides the shorter ones inside it (Heavy Dose, Dose)
      let text = item.text;
      const ids = new Set<string>();
      for (const alias of known) {
        if (at < alias.newest || at > alias.oldest) continue;
        const pattern = patterns.get(alias.name)!;
        const found = [...text.matchAll(pattern)];
        if (!found.length) continue;
        if (found.some((m) => !LONGER.test(text.slice(0, m.index)))) ids.add(alias.id);
        text = text.replace(pattern, (m) => ' '.repeat(m.length));
      }
      // A line under one that names the item comes with it
      const inherited = parent ? named.get(parent) : undefined;
      for (const id of ids) if (!inherited?.has(id)) add(id, { version: release.version, item });
      named.set(item, new Set([...ids, ...(inherited ?? [])]));
    });
  });
  return found;
}
