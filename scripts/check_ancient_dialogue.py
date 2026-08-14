#!/usr/bin/env python3
"""Check Ancient dialogue against the rules the game enforces at load.

AncientDialogue.PopulateLines throws when a conversation mixes "r" and non-"r" lines, and
AncientDialogueSet.PopulateLocKeys only assigns a Next button to lines before the last one. A
conversation that breaks either rule shows a line and then cannot be advanced.

Line counts are also compared across locales, because BaseLib decides how many lines a
conversation has by probing loc keys. A locale carrying more lines than eng builds a longer
conversation for that player, and its extra line has no Next button.

There is no cap on conversation length. The base game stops at 3 lines, but The Sorceress ships
4- and 5-line conversations, so length alone is not a fault.
"""
import collections
import io
import json
import re
import sys
from pathlib import Path

LOC = Path(__file__).resolve().parent.parent / "Alchemist/localization"
LINE = re.compile(r"(.+?)\.(\d+)-(\d+)(r?)\.(ancient|char)$")


def check(path):
    data = json.load(io.open(path, encoding="utf-8"))
    conversations = collections.defaultdict(dict)
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
    return conversations, problems


def main() -> int:
    total = 0
    failed = 0
    shapes = {}
    for path in sorted(LOC.glob("*/ancients.json")):
        locale = path.parent.name
        conversations, problems = check(path)
        shapes[locale] = {k: len(v) for k, v in conversations.items()}
        total = len(conversations)
        for p in problems:
            print(f"  \033[31mFAIL\033[0m  [{locale}] {p}")
            failed += 1

    eng = shapes.get("eng", {})
    for locale, shape in shapes.items():
        if locale == "eng":
            continue
        for key, n in shape.items():
            if eng.get(key, n) != n:
                print(f"  \033[31mFAIL\033[0m  [{locale}] {key[0]}.{key[1]}: "
                      f"{n} lines but eng has {eng[key]}")
                failed += 1

    print(f"{total} conversations x {len(shapes)} locales: {failed} error(s)")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
