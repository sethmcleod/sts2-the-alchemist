#!/usr/bin/env python3
"""Check Ancient dialogue against the rules the game enforces at load.

AncientDialogue.PopulateLines throws when a conversation mixes "r" and non-"r" lines, and
AncientDialogueSet.PopulateLocKeys only assigns a Next button to lines before the last one. A
conversation that breaks either rule shows its first line and then cannot be advanced.
"""
import collections
import json
import io
import re
import sys
from pathlib import Path

ENG = Path(__file__).resolve().parent.parent / "Alchemist/localization/eng/ancients.json"
LINE = re.compile(r"(.+?)\.(\d+)-(\d+)(r?)\.(ancient|char)$")


def main() -> int:
    data = json.load(io.open(ENG, encoding="utf-8"))
    conversations: dict[tuple[str, int], dict[int, str]] = collections.defaultdict(dict)
    for key in data:
        if (m := LINE.match(key)) is not None:
            conversations[(m.group(1), int(m.group(2)))][int(m.group(3))] = m.group(4)

    problems = []
    for (base, index), lines in sorted(conversations.items()):
        found = sorted(lines)
        count = len(found)
        name = f"{base}.{index}"
        if found != list(range(count)):
            problems.append(f"{name}: line indices are {found}, expected 0..{count - 1}")
        first = lines[found[0]]
        for i in found:
            if lines[i] != first:
                problems.append(
                    f"{name} line {i}: 'r' suffix does not match line 0; the game throws on this")
            has_next = f"{base}.{index}-{i}{lines[i]}.next" in data
            if i < count - 1 and not has_next:
                problems.append(f"{name} line {i}: no .next, so the dialogue cannot be advanced")
            if i == count - 1 and has_next:
                problems.append(f"{name} line {i}: last line must not have a .next")

    for p in problems:
        print(f"  \033[31mFAIL\033[0m  {p}")
    print(f"{len(conversations)} conversations: {len(problems)} error(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
