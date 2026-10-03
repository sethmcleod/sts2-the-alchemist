import type { NoteItem, Release } from './types';

export interface Change {
  item: NoteItem;
  version: string;
}

interface Alias {
  id: string;
  name: string;
  newest: number;
  oldest: number;
}

const escapeRegExp = (text: string) => text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const ITEM_NAME = String.raw`(?:the )?(.+?)(?: (?:card|relic|potion|power|enchantment|tip|section))?`;
const RENAME = new RegExp(`^${ITEM_NAME} to ${ITEM_NAME}$`);
const RENAME_TAIL = /(?:,? and (?:reworked|tinted)|, reusing|, because|, since|:).*$/;

export function renames(versions: Release[]) {
  const found: { at: number; from: string; to: string }[] = [];
  versions.forEach((release, at) =>
    forEachLine(release, (line) => {
      if (!line.text.startsWith('Renamed ')) return;
      const pairs = line.text.slice('Renamed '.length).replace(RENAME_TAIL, '');
      for (const pair of pairs.split(/,? and |, /)) {
        const match = RENAME.exec(pair.trim());
        if (match) found.push({ at, from: match[1], to: match[2] });
      }
    }),
  );
  return found;
}

function additions(versions: Release[]) {
  const found: { at: number; text: string }[] = [];
  versions.forEach((release, at) =>
    forEachLine(release, (line) => {
      if (line.text.startsWith('Added ')) found.push({ at, text: line.text });
    }),
  );
  return found;
}

function forEachLine(release: Release, visit: (line: NoteItem, parent: NoteItem | null) => void) {
  const walk = (lines: NoteItem[], parent: NoteItem | null) =>
    lines.forEach((line) => {
      visit(line, parent);
      walk(line.items, line);
    });
  release.sections.forEach((section) => walk(section.items, null));
}

function aliases(names: Record<string, string>, versions: Release[]): Alias[] {
  const allRenames = renames(versions);
  const addedLines = additions(versions);
  const oldestRelease = versions.length - 1;
  const found: Alias[] = [];
  const addAlias = (id: string, name: string, newest: number) => {
    const renamedTo = allRenames.find((rename) => rename.to === name && rename.at >= newest);
    const renamedAway = allRenames.find((rename) => rename.from === name && rename.at > newest);
    const addedPattern = new RegExp(`^Added (?:the )?${escapeRegExp(name)}(?![\\w'])`);
    const addedLine = addedLines.find((added) => added.at >= newest && addedPattern.test(added.text));
    const oldest = Math.min(
      renamedTo ? renamedTo.at : oldestRelease,
      renamedAway ? renamedAway.at - 1 : oldestRelease,
      addedLine ? addedLine.at : oldestRelease,
    );
    found.push({ id, name, newest, oldest });
    if (renamedTo && renamedTo.at <= oldest) addAlias(id, renamedTo.from, renamedTo.at);
  };
  for (const [id, name] of Object.entries(names)) addAlias(id, name, 0);
  return found;
}

const LONGER_NAME_BEFORE = /(?:^|[^\w'])[A-Z][\w'-]*(?<!ed) $/;

export function changes(names: Record<string, string>, versions: Release[]): Map<string, Change[]> {
  const longestFirst = aliases(names, versions).sort((a, b) => b.name.length - a.name.length);
  const namePatterns = new Map(
    longestFirst.map((alias) => [
      alias.name,
      new RegExp(`(?<![\\w'])${escapeRegExp(alias.name)}(?!\\w|'(?!s\\b))`, 'g'),
    ]),
  );
  const changesById = new Map<string, Change[]>();
  const addChange = (id: string, change: Change) => {
    const list = changesById.get(id) ?? [];
    if (!list.some((existing) => existing.item === change.item)) list.push(change);
    changesById.set(id, list);
  };

  versions.forEach((release, at) => {
    const idsByLine = new Map<NoteItem, Set<string>>();
    forEachLine(release, (line, parent) => {
      let text = line.text;
      const ids = new Set<string>();
      for (const alias of longestFirst) {
        if (at < alias.newest || at > alias.oldest) continue;
        const pattern = namePatterns.get(alias.name)!;
        const matches = [...text.matchAll(pattern)];
        if (!matches.length) continue;
        if (matches.some((match) => !LONGER_NAME_BEFORE.test(text.slice(0, match.index)))) ids.add(alias.id);
        text = text.replace(pattern, (match) => ' '.repeat(match.length));
      }
      const parentIds = parent ? idsByLine.get(parent) : undefined;
      for (const id of ids) if (!parentIds?.has(id)) addChange(id, { item: line, version: release.version });
      idsByLine.set(line, new Set([...ids, ...(parentIds ?? [])]));
    });
  });
  return changesById;
}
